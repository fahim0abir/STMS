import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BadgeComponent } from '../../../../shared/ui/badge.component';
import { TaskItem } from '../../../../shared/models/task.model';
import { TASK_PRIORITY_LABELS, TASK_STATUS_LABELS } from '../../../../shared/models/enums';

@Component({
  selector: 'app-upcoming-due-list',
  standalone: true,
  imports: [BadgeComponent, RouterLink],
  template: `
    <div class="upcoming">
      <h3 class="upcoming__title">Upcoming Due</h3>
      @if (!tasks.length) {
        <p class="upcoming__empty">Nothing due soon.</p>
      }
      @if (tasks.length) {
        <ul class="upcoming__list">
          @for (task of tasks; track task.id) {
            <li class="upcoming__item">
              <div class="upcoming__info">
                <a
                  class="upcoming__name"
                  routerLink="/tasks"
                  [queryParams]="{ projectId: task.projectId, sortBy: 'DueAt' }"
                  >{{ task.title }}</a
                >
                <span class="upcoming__meta">
                  <span class="upcoming__due" [class.upcoming__due--overdue]="isOverdue(task)">{{
                    dueLabel(task)
                  }}</span>
                  · {{ task.assigneeEmail || 'Unassigned' }}
                </span>
              </div>
              <div class="upcoming__badges">
                <app-badge variant="secondary" [label]="statusLabel(task)"></app-badge>
                <app-badge [variant]="priorityVariant(task)" [label]="priorityLabel(task)"></app-badge>
              </div>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .upcoming { background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; padding: 1.25rem; }
      .upcoming__title { margin: 0 0 1rem; font-size: 1rem; font-weight: 600; color: #111827; }
      .upcoming__empty { color: #9ca3af; font-size: 0.875rem; margin: 0; }
      .upcoming__list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.75rem; }
      .upcoming__item { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; padding-bottom: 0.75rem; border-bottom: 1px solid #f3f4f6; }
      .upcoming__item:last-child { border-bottom: none; padding-bottom: 0; }
      .upcoming__info { display: flex; flex-direction: column; gap: 0.125rem; min-width: 0; }
      .upcoming__name { font-size: 0.9rem; font-weight: 500; color: #111827; text-decoration: none; }
      .upcoming__name:hover, .upcoming__name:focus-visible { color: #2563eb; text-decoration: underline; }
      .upcoming__meta { font-size: 0.75rem; color: #9ca3af; }
      .upcoming__due { color: #374151; font-weight: 500; }
      .upcoming__due--overdue { color: #dc2626; }
      .upcoming__badges { display: flex; gap: 0.375rem; flex-shrink: 0; }
    `,
  ],
})
export class UpcomingDueListComponent {
  @Input() tasks: TaskItem[] = [];

  statusLabel(task: TaskItem): string {
    return TASK_STATUS_LABELS[task.status];
  }

  dueLabel(task: TaskItem): string {
    if (!task.dueAt) return 'No due date';
    const due = new Date(task.dueAt);
    if (Number.isNaN(due.getTime())) return 'No due date';

    const formatted = due.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
    const days = this.daysUntil(due);
    if (days < 0) return `Due ${formatted} · overdue by ${Math.abs(days)}d`;
    if (days === 0) return `Due ${formatted} · today`;
    return `Due ${formatted} · in ${days}d`;
  }

  isOverdue(task: TaskItem): boolean {
    if (!task.dueAt) return false;
    const due = new Date(task.dueAt);
    return !Number.isNaN(due.getTime()) && this.daysUntil(due) < 0;
  }

  private daysUntil(due: Date): number {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const target = new Date(due);
    target.setHours(0, 0, 0, 0);
    return Math.round((target.getTime() - today.getTime()) / 86_400_000);
  }

  priorityLabel(task: TaskItem): string {
    return TASK_PRIORITY_LABELS[task.priority];
  }

  priorityVariant(task: TaskItem): 'success' | 'warning' | 'danger' | 'secondary' {
    switch (task.priority) {
      case 3:
        return 'danger';
      case 2:
        return 'warning';
      case 0:
        return 'success';
      default:
        return 'secondary';
    }
  }
}

