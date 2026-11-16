import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { ProjectsService } from '../../core/services/projects.service';
import { UsersService } from '../../core/services/users.service';
import { AuthService } from '../../core/auth/auth.service';
import { Project, ProjectUpsert } from '../../shared/models/project.model';
import { User } from '../../shared/models/user.model';
import { Role } from '../../shared/models/enums';
import { PaginationComponent } from '../../shared/ui/pagination.component';
import { SearchSortBarComponent, SortOption } from '../../shared/ui/search-sort-bar.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { SpinnerComponent } from '../../shared/ui/spinner.component';
import { ProjectTableComponent } from './components/project-table/project-table.component';
import { ProjectModalComponent } from './components/project-modal/project-modal.component';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    PaginationComponent,
    SearchSortBarComponent,
    ButtonComponent,
    SpinnerComponent,
    ProjectTableComponent,
    ProjectModalComponent,
  ],
  templateUrl: './projects.component.html',
  styleUrl: './projects.component.scss',
})
export class ProjectsComponent {
  private readonly projectsService = inject(ProjectsService);
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly managers = signal<User[]>([]);

  readonly projects = signal<Project[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  readonly sortBy = signal('Name');
  readonly sortOrder = signal<'asc' | 'desc'>('asc');
  readonly search = signal('');
  readonly includeArchived = signal(false);

  readonly canManage = this.authService.hasRole('Admin', 'ProjectManager');
  readonly canAssignManager = this.authService.hasRole('Admin');

  readonly modalOpen = signal(false);
  readonly editing = signal<Project | null>(null);
  readonly saving = signal(false);
  readonly modalError = signal('');

  readonly sortOptions: SortOption[] = [
    { value: 'Name', label: 'Name' },
    { value: 'StartDate', label: 'Start date' },
    { value: 'CreatedAt', label: 'Created' },
  ];

  private readonly searchInput$ = new Subject<string>();

  constructor() {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.search.set(value);
        this.pageNumber.set(1);
        this.load();
      });
    this.load();
  }

  onSearch(value: string): void {
    this.searchInput$.next(value);
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

  onPageChange(page: number): void {
    this.pageNumber.set(page);
    this.load();
  }

  openCreate(): void {
    this.editing.set(null);
    this.modalError.set('');
    this.modalOpen.set(true);
    this.ensureManagers();
  }

  openEdit(project: Project): void {
    this.editing.set(project);
    this.modalError.set('');
    this.modalOpen.set(true);
    this.ensureManagers();
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.saving.set(false);
  }

  save(dto: ProjectUpsert): void {
    if (this.saving()) return;

    const editing = this.editing();
    this.saving.set(true);
    this.modalError.set('');
    const request$ = editing
      ? this.projectsService.update(editing.id, dto)
      : this.projectsService.create(dto);

    request$
      .pipe(
        switchMap(() => {
          const shouldAssignManager =
            !!editing && this.canAssignManager && !!dto.managerId && dto.managerId !== editing.managerId;
          return shouldAssignManager
            ? this.projectsService.assignManager(editing!.id, dto.managerId!)
            : of(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.modalOpen.set(false);
          this.load();
        },
        error: (err) => {
          this.saving.set(false);
          this.modalError.set(err?.error?.message || 'Failed to save project.');
        },
      });
  }

  private ensureManagers(): void {
    if (!this.canAssignManager || this.managers().length) return;
    this.usersService
      .getAssignableUsers(Role.ProjectManager)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => this.managers.set(users),
        error: () => this.modalError.set('Failed to load project managers.'),
      });
  }

  onIncludeArchivedChange(event: Event): void {
    this.includeArchived.set((event.target as HTMLInputElement).checked);
    this.pageNumber.set(1);
    this.load();
  }

  archive(project: Project): void {
    if (!this.canManage) return;
    if (!confirm(`Archive project "${project.name}"? It will be hidden from active lists.`)) return;

    this.projectsService.archive(project.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err?.error?.message || 'Failed to archive project.'),
    });
  }

  restore(project: Project): void {
    if (!this.canManage) return;

    this.projectsService.unarchive(project.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err?.error?.message || 'Failed to restore project.'),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.projectsService
      .getPaged({
        keyword: this.search() || undefined,
        includeArchived: this.includeArchived() || undefined,
        sortBy: this.sortBy(),
        sortOrder: this.sortOrder(),
        pageNumber: this.pageNumber(),
        pageSize: this.pageSize,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.projects.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load projects.');
          this.loading.set(false);
        },
      });
  }
}

