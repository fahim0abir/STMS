import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Project } from '../../../../shared/models/project.model';
import { ButtonComponent } from '../../../../shared/ui/button.component';

@Component({
  selector: 'app-project-table',
  standalone: true,
  imports: [ButtonComponent],
  template: `
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th class="table__sortable" (click)="sort('Name')">
              Name
              @if (sortBy === 'Name') {
                <span>{{ sortOrder === 'asc' ? '↑' : '↓' }}</span>
              }
            </th>
            <th>Description</th>
            <th>Manager</th>
            @if (canManage) {
              <th class="table__actions-head">Actions</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (project of projects; track project.id) {
            <tr>
              <td class="table__name">{{ project.name }}</td>
              <td class="table__muted">{{ project.description || '—' }}</td>
              <td class="table__muted">{{ project.managerName || project.managerEmail || 'Unassigned' }}</td>
              @if (canManage) {
                <td class="table__actions">
                  <app-btn size="sm" variant="outline" (click)="edit.emit(project)">Edit</app-btn>
                  <app-btn size="sm" variant="danger" (click)="delete.emit(project)">Delete</app-btn>
                </td>
              }
            </tr>
          }
          @if (!projects.length) {
            <tr>
              <td [attr.colspan]="canManage ? 4 : 3" class="table__empty">No projects found.</td>
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
      }
      .table td { padding: 0.75rem 1rem; border-bottom: 1px solid #f3f4f6; color: #111827; }
      .table__sortable { cursor: pointer; user-select: none; }
      .table__name { font-weight: 500; }
      .table__muted { color: #6b7280; }
      .table__actions { display: flex; gap: 0.5rem; }
      .table__empty { text-align: center; color: #9ca3af; padding: 2rem 1rem; }
    `,
  ],
})
export class ProjectTableComponent {
  @Input() projects: Project[] = [];
  @Input() canManage = false;
  @Input() sortBy = 'Name';
  @Input() sortOrder: 'asc' | 'desc' = 'asc';

  @Output() sortChange = new EventEmitter<string>();
  @Output() edit = new EventEmitter<Project>();
  @Output() delete = new EventEmitter<Project>();

  sort(field: string): void {
    this.sortChange.emit(field);
  }
}

