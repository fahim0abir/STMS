import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { Params } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { DashboardService } from '../../core/services/dashboard.service';
import { TasksService } from '../../core/services/tasks.service';
import { AuthService } from '../../core/auth/auth.service';
import { DashboardStats } from '../../shared/models/dashboard.model';
import { TaskItem } from '../../shared/models/task.model';
import { TaskStatus } from '../../shared/models/enums';
import { SpinnerComponent } from '../../shared/ui/spinner.component';
import { KpiCardComponent } from './components/kpi-card/kpi-card.component';
import { StatBreakdownComponent, BreakdownRow } from './components/stat-breakdown/stat-breakdown.component';
import { UpcomingDueListComponent } from './components/upcoming-due-list/upcoming-due-list.component';
import { CompletionRingComponent } from './components/completion-ring/completion-ring.component';
import { TaskBoardComponent } from './components/task-board/task-board.component';

const STATUS_LABELS: Record<string, string> = {
  ToDo: 'To Do',
  InProgress: 'In Progress',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

const STATUS_COLORS: Record<string, string> = {
  ToDo: '#6b7280',
  InProgress: '#2563eb',
  Completed: '#16a34a',
  Cancelled: '#dc2626',
};

const PRIORITY_COLORS: Record<string, string> = {
  Low: '#16a34a',
  Medium: '#2563eb',
  High: '#d97706',
  Critical: '#dc2626',
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    SpinnerComponent,
    KpiCardComponent,
    StatBreakdownComponent,
    UpcomingDueListComponent,
    CompletionRingComponent,
    TaskBoardComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  private readonly dashboardService = inject(DashboardService);
  private readonly tasksService = inject(TasksService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly authService = inject(AuthService);

  readonly canViewProjects = this.authService.hasRole('Admin', 'ProjectManager');
  readonly currentUserId = this.authService.currentUserValue?.id ?? '';
  readonly boardAssigneeId: string | undefined = this.authService.hasRole('TeamMember')
    ? this.currentUserId
    : undefined;

  readonly completedQuery: Params = { status: 'Completed' };
  readonly pendingQuery: Params = { pending: '1' };
  readonly dueSoonQuery: Params = { dueFrom: 'now', pending: '1', sortBy: 'DueAt' };

  readonly loading = signal(true);
  readonly error = signal('');
  readonly stats = signal<DashboardStats | null>(null);
  readonly upcoming = signal<TaskItem[]>([]);
  readonly myTasks = signal<TaskItem[]>([]);
  readonly boardBusyId = signal<string | null>(null);
  readonly boardError = signal('');

  readonly statusRows = computed<BreakdownRow[]>(() => this.toRows(this.stats()?.tasksByStatus, STATUS_LABELS, STATUS_COLORS));
  readonly priorityRows = computed<BreakdownRow[]>(() => this.toRows(this.stats()?.tasksByPriority, STATUS_LABELS, PRIORITY_COLORS));

  constructor() {
    this.load();
  }

  load(silent = false): void {
    if (!silent) this.loading.set(true);
    this.error.set('');

    const dueFrom = new Date().toISOString();
    forkJoin({
      myTasks: this.tasksService.getPaged({
        assigneeId: this.boardAssigneeId,
        sortBy: 'DueAt',
        sortOrder: 'asc',
        pageNumber: 1,
        pageSize: 50,
      }),
      stats: this.dashboardService.getStats(),
      upcoming: this.tasksService.getPaged({
        dueFrom,
        pendingOnly: true,
        sortBy: 'DueAt',
        sortOrder: 'asc',
        pageNumber: 1,
        pageSize: 5,
      }),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ stats, upcoming, myTasks }) => {
          this.stats.set(stats);
          this.upcoming.set(upcoming.items);
          this.myTasks.set(myTasks.items);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load dashboard.');
          this.loading.set(false);
        },
      });
  }

  onBoardStatusChange(event: { task: TaskItem; status: TaskStatus }): void {
    if (this.boardBusyId()) return;

    this.boardBusyId.set(event.task.id);
    this.boardError.set('');

    this.tasksService
      .updateStatus(event.task.id, event.status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.boardBusyId.set(null);
          this.load(true);
        },
        error: (err) => {
          this.boardBusyId.set(null);
          this.boardError.set(err?.error?.message || 'Failed to update task status.');
        },
      });
  }

  private toRows(
    source: Record<string, number> | undefined,
    labels: Record<string, string>,
    colors: Record<string, string>,
  ): BreakdownRow[] {
    if (!source) return [];
    return Object.entries(source).map(([key, value]) => ({
      label: labels[key] ?? key,
      value,
      color: colors[key],
    }));
  }
}

