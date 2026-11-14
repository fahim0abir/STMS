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

@Component({
  selector: 'app-task-table',
  standalone: true,
  imports: [BadgeComponent, ButtonComponent],
  template: `
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th class="table__sortable" (click)="sortChange.emit('Title')">
              Title
              @if (sortBy === 'Title') {
                <span>{{ sortOrder === 'asc' ? '↑' : '↓' }}</span>
              }
            </th>
            <th>Status</th>
            <th>Priority</th>
            <th>Assignee</th>
            @if (canManage) {
              <th class="table__actions-head">Actions</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (task of tasks; track task.id) {
            <tr>
              <td class="table__title">{{ task.title }}</td>
              <td>
                @if (canUpdateStatus(task)) {
                  <select class="table__status" (change)="onStatusChange(task, $event)">
                    @for (option of statusOptions; track option.value) {
                      <option [value]="option.value" [selected]="option.value === task.status">{{ option.label }}</option>
                    }
                  </select>
                } @else {
                  <app-badge [variant]="statusVariant(task)" [label]="statusLabel(task)"></app-badge>
                }
              </td>
              <td><app-badge [variant]="priorityVariant(task)" [label]="priorityLabel(task)"></app-badge></td>
              <td class="table__muted">{{ task.assigneeName || task.assigneeEmail || 'Unassigned' }}</td>
              @if (canManage) {
                <td class="table__actions">
                  <app-btn size="sm" variant="outline" (click)="edit.emit(task)">Edit</app-btn>
                  @if (canDelete) {
                    <app-btn size="sm" variant="danger" (click)="delete.emit(task)">Delete</app-btn>
                  }
                </td>
              }
            </tr>
          }
          @if (!tasks.length) {
            <tr>
              <td [attr.colspan]="canManage ? 5 : 4" class="table__empty">No tasks found.</td>
            </tr>
          }
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
      .table__status {
        border: 1px solid #d1d5db;
        border-radius: 0.5rem;
        padding: 0.25rem 0.5rem;
        font-size: 0.8125rem;
        background: #fff;
        color: #111827;
        font-family: inherit;
      }
      .table__actions { display: flex; gap: 0.5rem; }
      .table__empty { text-align: center; color: #9ca3af; padding: 2rem 1rem; }
    `,
  ],
})
export class TaskTableComponent {
  @Input() tasks: TaskItem[] = [];
  @Input() canManage = false;
  @Input() canDelete = false;
  @Input() currentUserId = '';
  @Input() sortBy = 'CreatedAt';
  @Input() sortOrder: 'asc' | 'desc' = 'desc';

  @Output() sortChange = new EventEmitter<string>();
  @Output() edit = new EventEmitter<TaskItem>();
  @Output() delete = new EventEmitter<TaskItem>();
  @Output() statusChange = new EventEmitter<{ task: TaskItem; status: TaskStatus }>();

  readonly statusOptions = Object.entries(TASK_STATUS_LABELS).map(([value, label]) => ({
    value: Number(value) as TaskStatus,
    label,
  }));

  canUpdateStatus(task: TaskItem): boolean {
    return this.canManage || (!!this.currentUserId && task.assigneeId === this.currentUserId);
  }

  onStatusChange(task: TaskItem, event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value) as TaskStatus;
    if (value === task.status) return;
    this.statusChange.emit({ task, status: value });
  }

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

