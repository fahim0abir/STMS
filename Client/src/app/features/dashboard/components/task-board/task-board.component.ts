import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TaskItem } from '../../../../shared/models/task.model';
import {
  TaskPriority,
  TaskStatus,
  TASK_PRIORITY_LABELS,
  TASK_STATUS_LABELS,
} from '../../../../shared/models/enums';
import { BadgeComponent } from '../../../../shared/ui/badge.component';
import { ButtonComponent } from '../../../../shared/ui/button.component';

interface BoardColumn {
  status: TaskStatus;
  label: string;
}

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [BadgeComponent, ButtonComponent],
  template: `
    <div class="board">
      <div class="board__head">
        <h3 class="board__title">My Task Board</h3>
        <span class="board__meta">{{ tasks.length }} assigned task{{ tasks.length === 1 ? '' : 's' }}</span>
      </div>

      <div class="board__columns">
        @for (column of columns; track column.status) {
          <div class="board__column">
            <div class="board__column-head">
              <span>{{ column.label }}</span>
              <span class="board__count">{{ tasksIn(column.status).length }}</span>
            </div>

            <div class="board__cards">
              @for (task of tasksIn(column.status); track task.id) {
                <article class="board__card">
                  <div class="board__card-top">
                    <span class="board__task-title">{{ task.title }}</span>
                    <app-badge
                      [variant]="priorityVariant(task.priority)"
                      [label]="priorityLabel(task)"
                    ></app-badge>
                  </div>
                  <div class="board__card-meta">
                    <span [class.board__overdue]="isOverdue(task)">{{ dueLabel(task) }}</span>
                    @if (task.projectName) {
                      <span> · {{ task.projectName }}</span>
                    }
                  </div>
                  <div class="board__card-actions">
                    @if (nextStatus(task.status); as next) {
                      <app-btn
                        size="sm"
                        variant="outline"
                        [loading]="busyId === task.id"
                        (click)="move(task, next.status)"
                        >{{ next.label }}</app-btn
                      >
                    }
                    @if (reopenStatus(task.status); as reopen) {
                      <app-btn
                        size="sm"
                        variant="ghost"
                        [loading]="busyId === task.id"
                        (click)="move(task, reopen.status)"
                        >{{ reopen.label }}</app-btn
                      >
                    }
                  </div>
                </article>
              }
              @if (!tasksIn(column.status).length) {
                <p class="board__empty">Nothing here.</p>
              }
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .board { background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; padding: 1.25rem; margin-bottom: 1.5rem; }
      .board__head { display: flex; align-items: baseline; justify-content: space-between; gap: 0.75rem; margin-bottom: 1rem; }
      .board__title { margin: 0; font-size: 1rem; font-weight: 600; color: #111827; }
      .board__meta { font-size: 0.75rem; color: #9ca3af; }
      .board__columns { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 0.75rem; }
      .board__column { background: #f9fafb; border: 1px solid #f3f4f6; border-radius: 0.5rem; padding: 0.75rem; }
      .board__column-head { display: flex; align-items: center; justify-content: space-between; font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.03em; color: #6b7280; margin-bottom: 0.625rem; }
      .board__count { background: #e5e7eb; color: #374151; border-radius: 999px; padding: 0 0.4rem; font-size: 0.7rem; }
      .board__cards { display: flex; flex-direction: column; gap: 0.5rem; }
      .board__card { background: #fff; border: 1px solid #e5e7eb; border-radius: 0.5rem; padding: 0.625rem; display: flex; flex-direction: column; gap: 0.375rem; }
      .board__card-top { display: flex; align-items: flex-start; justify-content: space-between; gap: 0.5rem; }
      .board__task-title { font-size: 0.85rem; font-weight: 500; color: #111827; }
      .board__card-meta { font-size: 0.72rem; color: #9ca3af; }
      .board__overdue { color: #dc2626; font-weight: 500; }
      .board__card-actions { display: flex; flex-wrap: wrap; gap: 0.375rem; }
      .board__empty { margin: 0; font-size: 0.75rem; color: #9ca3af; }
    `,
  ],
})
export class TaskBoardComponent {
  @Input() tasks: TaskItem[] = [];
  @Input() busyId: string | null = null;

  @Output() statusChange = new EventEmitter<{ task: TaskItem; status: TaskStatus }>();

  readonly columns: BoardColumn[] = [
    { status: TaskStatus.ToDo, label: TASK_STATUS_LABELS[TaskStatus.ToDo] },
    { status: TaskStatus.InProgress, label: TASK_STATUS_LABELS[TaskStatus.InProgress] },
    { status: TaskStatus.Completed, label: TASK_STATUS_LABELS[TaskStatus.Completed] },
    { status: TaskStatus.Cancelled, label: TASK_STATUS_LABELS[TaskStatus.Cancelled] },
  ];

  tasksIn(status: TaskStatus): TaskItem[] {
    return this.tasks.filter((task) => task.status === status);
  }

  move(task: TaskItem, status: TaskStatus): void {
    this.statusChange.emit({ task, status });
  }

  nextStatus(status: TaskStatus): { status: TaskStatus; label: string } | null {
    switch (status) {
      case TaskStatus.ToDo:
        return { status: TaskStatus.InProgress, label: 'Start' };
      case TaskStatus.InProgress:
        return { status: TaskStatus.Completed, label: 'Complete' };
      default:
        return null;
    }
  }

  reopenStatus(status: TaskStatus): { status: TaskStatus; label: string } | null {
    switch (status) {
      case TaskStatus.InProgress:
        return { status: TaskStatus.ToDo, label: 'Pause' };
      case TaskStatus.Completed:
      case TaskStatus.Cancelled:
        return { status: TaskStatus.ToDo, label: 'Reopen' };
      default:
        return { status: TaskStatus.Cancelled, label: 'Cancel' };
    }
  }

  priorityLabel(task: TaskItem): string {
    return TASK_PRIORITY_LABELS[task.priority];
  }

  priorityVariant(priority: TaskPriority): 'success' | 'warning' | 'danger' | 'secondary' {
    switch (priority) {
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

  dueLabel(task: TaskItem): string {
    if (!task.dueAt) return 'No due date';
    const due = new Date(task.dueAt);
    if (Number.isNaN(due.getTime())) return 'No due date';
    return `Due ${due.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}`;
  }

  isOverdue(task: TaskItem): boolean {
    if (!task.dueAt) return false;
    const due = new Date(task.dueAt);
    return !Number.isNaN(due.getTime()) && due.getTime() < Date.now() && task.status !== TaskStatus.Completed;
  }
}
