import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { TaskPriority, TaskStatus, TASK_PRIORITY_LABELS, TASK_STATUS_LABELS } from '../../shared/models/enums';
import { Project } from '../../shared/models/project.model';
import { TaskItem, TaskItemQuery } from '../../shared/models/task.model';
import { TasksService } from '../../core/services/tasks.service';
import { ProjectsService } from '../../core/services/projects.service';
import { UsersService } from '../../core/services/users.service';
import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../shared/models/enums';
import { User } from '../../shared/models/user.model';
import { PaginationComponent } from '../../shared/ui/pagination.component';
import { SearchSortBarComponent, SortOption } from '../../shared/ui/search-sort-bar.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { SpinnerComponent } from '../../shared/ui/spinner.component';
import { TaskTableComponent } from './components/task-table/task-table.component';
import { TaskModalComponent, TaskModalSaveEvent } from './components/task-modal/task-modal.component';
import { FilterChip, TaskFilterChipsComponent } from './components/task-filter-chips/task-filter-chips.component';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [
    PaginationComponent,
    SearchSortBarComponent,
    ButtonComponent,
    SpinnerComponent,
    TaskTableComponent,
    TaskModalComponent,
    TaskFilterChipsComponent,
  ],
  templateUrl: './tasks.component.html',
  styleUrl: './tasks.component.scss',
})
export class TasksComponent {
  private readonly tasksService = inject(TasksService);
  private readonly projectsService = inject(ProjectsService);
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);

  readonly currentUserId = this.authService.currentUserValue?.id ?? '';
  readonly assignees = signal<User[]>([]);

  readonly tasks = signal<TaskItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  readonly sortBy = signal('CreatedAt');
  readonly sortOrder = signal<'asc' | 'desc'>('desc');
  readonly search = signal('');

  readonly statusFilter = signal<TaskStatus | undefined>(undefined);
  readonly priorityFilter = signal<TaskPriority | undefined>(undefined);
  readonly mineOnly = signal(false);
  readonly pendingOnly = signal(false);
  readonly projectFilter = signal<string | undefined>(undefined);
  readonly dueFrom = signal('');
  readonly dueTo = signal('');

  readonly canManage = this.authService.hasRole('Admin', 'ProjectManager');
  readonly canDelete = this.authService.hasRole('Admin');

  readonly projects = signal<Project[]>([]);
  readonly modalOpen = signal(false);
  readonly editing = signal<TaskItem | null>(null);
  readonly saving = signal(false);
  readonly modalError = signal('');

  readonly sortOptions: SortOption[] = [
    { value: 'CreatedAt', label: 'Created' },
    { value: 'Title', label: 'Title' },
    { value: 'Status', label: 'Status' },
    { value: 'Priority', label: 'Priority' },
    { value: 'DueAt', label: 'Due date' },
  ];

  readonly statusChips = computed<FilterChip<TaskStatus>[]>(() => [
    { value: undefined, label: 'All' },
    ...Object.entries(TASK_STATUS_LABELS).map(([value, label]) => ({
      value: Number(value) as TaskStatus,
      label,
    })),
  ]);

  readonly priorityChips = computed<FilterChip<TaskPriority>[]>(() => [
    { value: undefined, label: 'All' },
    ...Object.entries(TASK_PRIORITY_LABELS).map(([value, label]) => ({
      value: Number(value) as TaskPriority,
      label,
    })),
  ]);

  private readonly searchInput$ = new Subject<string>();

  constructor() {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.search.set(value);
        this.pageNumber.set(1);
        this.load();
      });
    this.applyQueryParams();
    this.ensureProjects();
    if (this.canManage) {
      this.ensureAssignees();
    }
    this.load();
  }

  private applyQueryParams(): void {
    const params = this.route.snapshot.queryParamMap;
    const status = params.get('status');
    if (status && status in TaskStatus) {
      this.statusFilter.set(TaskStatus[status as keyof typeof TaskStatus]);
    }
    const priority = params.get('priority');
    if (priority && priority in TaskPriority) {
      this.priorityFilter.set(TaskPriority[priority as keyof typeof TaskPriority]);
    }
    const projectId = params.get('projectId');
    if (projectId) {
      this.projectFilter.set(projectId);
    }
    const dueFrom = params.get('dueFrom');
    if (dueFrom) {
      this.dueFrom.set(dueFrom === 'now' ? new Date().toISOString().slice(0, 10) : dueFrom);
    }
    if (params.get('pending') === '1') {
      this.pendingOnly.set(true);
    }
    const sortBy = params.get('sortBy');
    if (sortBy) {
      this.sortBy.set(sortBy);
      this.sortOrder.set('asc');
    }
  }

  onSearch(value: string): void {
    this.searchInput$.next(value);
  }

  onSortField(field: string): void {
    this.sortBy.set(field);
    this.pageNumber.set(1);
    this.load();
  }

  onSortOrderChange(order: 'asc' | 'desc'): void {
    this.sortOrder.set(order);
    this.pageNumber.set(1);
    this.load();
  }

  onSort(field: string): void {
    if (this.sortBy() === field) {
      this.sortOrder.update((order) => (order === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      this.sortOrder.set('asc');
    }
    this.pageNumber.set(1);
    this.load();
  }

  onStatusChange(status: TaskStatus | undefined): void {
    this.statusFilter.set(status);
    this.pageNumber.set(1);
    this.load();
  }

  onPriorityChange(priority: TaskPriority | undefined): void {
    this.priorityFilter.set(priority);
    this.pageNumber.set(1);
    this.load();
  }

  onProjectChange(event: Event): void {
    this.projectFilter.set((event.target as HTMLSelectElement).value || undefined);
    this.pageNumber.set(1);
    this.load();
  }

  onMineChange(event: Event): void {
    this.mineOnly.set((event.target as HTMLInputElement).checked);
    this.pageNumber.set(1);
    this.load();
  }

  onDueFromChange(event: Event): void {
    this.dueFrom.set((event.target as HTMLInputElement).value);
    this.pageNumber.set(1);
    this.load();
  }

  onDueToChange(event: Event): void {
    this.dueTo.set((event.target as HTMLInputElement).value);
    this.pageNumber.set(1);
    this.load();
  }

  onPageChange(page: number): void {
    this.pageNumber.set(page);
    this.load();
  }

  openCreate(): void {
    this.editing.set(null);
    this.modalError.set('');
    this.modalOpen.set(true);
    this.ensureProjects();
    this.ensureAssignees();
  }

  openEdit(task: TaskItem): void {
    this.editing.set(task);
    this.modalError.set('');
    this.modalOpen.set(true);
    this.ensureAssignees();
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.saving.set(false);
  }

  save(event: TaskModalSaveEvent): void {
    if (this.saving()) return;

    const editing = this.editing();
    this.saving.set(true);
    this.modalError.set('');

    const request$ = editing
      ? this.tasksService.update(editing.id, event.dto)
      : this.tasksService.create(event.projectId!, event.dto);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.modalOpen.set(false);
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.modalError.set(err?.error?.message || 'Failed to save task.');
      },
    });
  }

  remove(task: TaskItem): void {
    if (!this.canDelete) return;
    if (!confirm(`Delete task "${task.title}"?`)) return;

    this.tasksService.delete(task.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        if (this.tasks().length === 1 && this.pageNumber() > 1) {
          this.pageNumber.update((p) => p - 1);
        }
        this.load();
      },
      error: (err) => this.error.set(err?.error?.message || 'Failed to delete task.'),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    const query: TaskItemQuery = {
      keyword: this.search() || undefined,
      status: this.statusFilter(),
      priority: this.priorityFilter(),
      projectId: this.projectFilter(),
      pendingOnly: this.pendingOnly() || undefined,
      assigneeId: this.mineOnly() ? this.authService.currentUserValue?.id : undefined,
      dueFrom: this.toIsoStart(this.dueFrom()),
      dueTo: this.toIsoEnd(this.dueTo()),
      sortBy: this.sortBy(),
      sortOrder: this.sortOrder(),
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize,
    };

    this.tasksService
      .getPaged(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.tasks.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load tasks.');
          this.loading.set(false);
        },
      });
  }

  onTaskStatusChange(event: { task: TaskItem; status: TaskStatus }): void {
    this.tasksService
      .updateStatus(event.task.id, event.status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.load(),
        error: (err) => this.error.set(err?.error?.message || 'Failed to update task status.'),
      });
  }

  onTaskAssign(event: { task: TaskItem; assigneeId: string | null }): void {
    this.tasksService
      .assign(event.task.id, event.assigneeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.load(),
        error: (err) => this.error.set(err?.error?.message || 'Failed to assign task.'),
      });
  }

  private ensureAssignees(): void {
    if (!this.canManage || this.assignees().length) return;
    this.usersService
      .getAssignableUsers(Role.TeamMember)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => this.assignees.set(users),
        error: () => this.modalError.set('Failed to load team members.'),
      });
  }

  private ensureProjects(): void {
    if (this.projects().length) return;
    this.projectsService
      .getPaged({ pageSize: 100, sortBy: 'Name', sortOrder: 'asc' })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => this.projects.set(result.items),
        error: () => this.modalError.set('Failed to load projects for task creation.'),
      });
  }

  private toIsoStart(value: string): string | undefined {
    return value ? new Date(value + 'T00:00:00').toISOString() : undefined;
  }

  private toIsoEnd(value: string): string | undefined {
    return value ? new Date(value + 'T23:59:59').toISOString() : undefined;
  }
}

