import { Component, Input, Output, EventEmitter } from '@angular/core';
import { PROJECT_STATUS_LABELS, Project, ProjectStatus } from '../../../../shared/models/project.model';
import { BadgeComponent } from '../../../../shared/ui/badge.component';
import { ButtonComponent } from '../../../../shared/ui/button.component';

@Component({
  selector: 'app-project-table',
  standalone: true,
  imports: [BadgeComponent, ButtonComponent],
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
            <th class="table__sortable" (click)="sort('StartDate')">
              Start
              @if (sortBy === 'StartDate') {
                <span>{{ sortOrder === 'asc' ? '↑' : '↓' }}</span>
              }
            </th>
            <th>End</th>
            <th>Tech stack</th>
            <th>Client</th>
            <th>Status</th>
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
              <td class="table__muted">{{ formatDate(project.startDate) }}</td>
              <td class="table__muted">{{ project.endDate ? formatDate(project.endDate) : '—' }}</td>
              <td class="table__muted">{{ project.techStack || '—' }}</td>
              <td class="table__muted">{{ project.clientName || '—' }}</td>
              <td><app-badge [variant]="statusVariant(project)" [label]="statusLabel(project)"></app-badge></td>
              <td class="table__muted">{{ project.managerName || project.managerEmail || 'Unassigned' }}</td>
              @if (canManage) {
                <td class="table__actions">
                  <app-btn size="sm" variant="outline" (click)="edit.emit(project)">Edit</app-btn>
                  @if (isArchived(project)) {
                    <app-btn size="sm" variant="primary" (click)="restore.emit(project)">Restore</app-btn>
                  } @else {
                    <app-btn size="sm" variant="danger" (click)="archive.emit(project)">Archive</app-btn>
                  }
                </td>
              }
            </tr>
          }
          @if (!projects.length) {
            <tr>
              <td [attr.colspan]="canManage ? 8 : 7" class="table__empty">No projects found.</td>
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
  @Output() archive = new EventEmitter<Project>();
  @Output() restore = new EventEmitter<Project>();

  sort(field: string): void {
    this.sortChange.emit(field);
  }

  formatDate(value?: string | null): string {
    if (!value) return '—';
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString();
  }

  isArchived(project: Project): boolean {
    return project.status === ProjectStatus.Archived;
  }

  statusLabel(project: Project): string {
    return PROJECT_STATUS_LABELS[project.status] ?? 'Active';
  }

  statusVariant(project: Project): 'secondary' | 'success' | 'warning' | 'danger' | 'primary' {
    switch (project.status) {
      case ProjectStatus.Completed:
        return 'success';
      case ProjectStatus.OnHold:
        return 'warning';
      case ProjectStatus.Archived:
        return 'danger';
      default:
        return 'primary';
    }
  }
}

