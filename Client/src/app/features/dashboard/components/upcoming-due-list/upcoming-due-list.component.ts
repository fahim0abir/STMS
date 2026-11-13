import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BadgeComponent } from '../../../../shared/ui/badge.component';
import { TaskItem } from '../../../../shared/models/task.model';
import { TASK_PRIORITY_LABELS, TASK_STATUS_LABELS } from '../../../../shared/models/enums';

@Component({
  selector: 'app-upcoming-due-list',
  standalone: true,
  imports: [CommonModule, BadgeComponent],
  template: `
    <div class="upcoming">
      <h3 class="upcoming__title">Upcoming Due</h3>
      <p *ngIf="!tasks.length" class="upcoming__empty">Nothing due soon.</p>
      <ul *ngIf="tasks.length" class="upcoming__list">
        <li *ngFor="let task of tasks" class="upcoming__item">
          <div class="upcoming__info">
            <span class="upcoming__name">{{ task.title }}</span>
            <span class="upcoming__meta">{{ task.assigneeEmail || 'Unassigned' }}</span>
          </div>
          <div class="upcoming__badges">
            <app-badge variant="secondary" [label]="statusLabel(task)"></app-badge>
            <app-badge [variant]="priorityVariant(task)" [label]="priorityLabel(task)"></app-badge>
          </div>
        </li>
      </ul>
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
      .upcoming__name { font-size: 0.9rem; font-weight: 500; color: #111827; }
      .upcoming__meta { font-size: 0.75rem; color: #9ca3af; }
      .upcoming__badges { display: flex; gap: 0.375rem; flex-shrink: 0; }
    `,
  ],
})
export class UpcomingDueListComponent {
  @Input() tasks: TaskItem[] = [];

  statusLabel(task: TaskItem): string {
    return TASK_STATUS_LABELS[task.status];
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

