import { Component, Input, Output, EventEmitter } from '@angular/core';
import { TaskItem } from '../../../../shared/models/task.model';
import {
  TASK_PRIORITY_LABELS,
  TASK_STATUS_LABELS,
  TaskPriority,
  TaskStatus,
} from '../../../../shared/models/enums';
import { BadgeComponent } from '../../../../shared/ui/badge.component';
import { ButtonComponent } from '../../../../shared/ui/button.component';

import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-task-table',
  standalone: true,
  imports: [CommonModule, BadgeComponent, ButtonComponent],
  template: `
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th class="table__sortable" (click)="sortChange.emit('Title')">
              Title <span *ngIf="sortBy === 'Title'">{{ sortOrder === 'asc' ? '↑' : '↓' }}</span>
            </th>
            <th>Status</th>
            <th>Priority</th>
            <th>Assignee</th>
            <th *ngIf="canManage" class="table__actions-head">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let task of tasks">
            <td class="table__title">{{ task.title }}</td>
            <td><app-badge [variant]="statusVariant(task)" [label]="statusLabel(task)"></app-badge></td>
            <td><app-badge [variant]="priorityVariant(task)" [label]="priorityLabel(task)"></app-badge></td>
            <td class="table__muted">{{ task.assigneeEmail || 'Unassigned' }}</td>
            <td *ngIf="canManage" class="table__actions">
              <app-btn size="sm" variant="outline" (click)="edit.emit(task)">Edit</app-btn>
              <app-btn *ngIf="canDelete" size="sm" variant="danger" (click)="delete.emit(task)">Delete</app-btn>
            </td>
          </tr>
          <tr *ngIf="!tasks.length">
            <td [attr.colspan]="canManage ? 5 : 4" class="table__empty">No tasks found.</td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .table-wrap { overflow-x: auto; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; }
      .table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
      .table th {
        text-align: left;
        padding: 0.75rem 1rem;
        background: #f9fafb;
        color: #6b7280;
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        border-bottom: 1px solid #e5e7eb;
        white-space: nowrap;
      }
      .table td { padding: 0.75rem 1rem; border-bottom: 1px solid #f3f4f6; color: #111827; }
      .table__sortable { cursor: pointer; user-select: none; }
      .table__title { font-weight: 500; }
      .table__muted { color: #6b7280; }
      .table__actions { display: flex; gap: 0.5rem; }
      .table__empty { text-align: center; color: #9ca3af; padding: 2rem 1rem; }
    `,
  ],
})
export class TaskTableComponent {
  @Input() tasks: TaskItem[] = [];
  @Input() canManage = false;
  @Input() canDelete = false;
  @Input() sortBy = 'CreatedAt';
  @Input() sortOrder: 'asc' | 'desc' = 'desc';

  @Output() sortChange = new EventEmitter<string>();
  @Output() edit = new EventEmitter<TaskItem>();
  @Output() delete = new EventEmitter<TaskItem>();

  statusLabel(task: TaskItem): string {
    return TASK_STATUS_LABELS[task.status];
  }

  priorityLabel(task: TaskItem): string {
    return TASK_PRIORITY_LABELS[task.priority];
  }

  statusVariant(task: TaskItem): 'secondary' | 'primary' | 'success' | 'warning' | 'danger' {
    switch (task.status) {
      case TaskStatus.InProgress:
        return 'primary';
      case TaskStatus.Completed:
        return 'success';
      case TaskStatus.Cancelled:
        return 'danger';
      default:
        return 'secondary';
    }
  }

  priorityVariant(task: TaskItem): 'secondary' | 'success' | 'warning' | 'danger' {
    switch (task.priority) {
      case TaskPriority.Critical:
        return 'danger';
      case TaskPriority.High:
        return 'warning';
      case TaskPriority.Low:
        return 'success';
      default:
        return 'secondary';
    }
  }
}

