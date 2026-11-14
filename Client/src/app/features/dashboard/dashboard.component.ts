import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { DashboardService } from '../../core/services/dashboard.service';
import { TasksService } from '../../core/services/tasks.service';
import { DashboardStats } from '../../shared/models/dashboard.model';
import { TaskItem } from '../../shared/models/task.model';
import { SpinnerComponent } from '../../shared/ui/spinner.component';
import { KpiCardComponent } from './components/kpi-card/kpi-card.component';
import { StatBreakdownComponent, BreakdownRow } from './components/stat-breakdown/stat-breakdown.component';
import { UpcomingDueListComponent } from './components/upcoming-due-list/upcoming-due-list.component';
import { CompletionRingComponent } from './components/completion-ring/completion-ring.component';

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
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  private readonly dashboardService = inject(DashboardService);
  private readonly tasksService = inject(TasksService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly stats = signal<DashboardStats | null>(null);
  readonly upcoming = signal<TaskItem[]>([]);

  readonly statusRows = computed<BreakdownRow[]>(() => this.toRows(this.stats()?.tasksByStatus, STATUS_LABELS, STATUS_COLORS));
  readonly priorityRows = computed<BreakdownRow[]>(() => this.toRows(this.stats()?.tasksByPriority, STATUS_LABELS, PRIORITY_COLORS));

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    const dueFrom = new Date().toISOString();
    forkJoin({
      stats: this.dashboardService.getStats(),
      upcoming: this.tasksService.getPaged({
        dueFrom,
        sortBy: 'DueAt',
        sortOrder: 'asc',
        pageNumber: 1,
        pageSize: 5,
      }),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ stats, upcoming }) => {
          this.stats.set(stats);
          this.upcoming.set(upcoming.items);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load dashboard.');
          this.loading.set(false);
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

