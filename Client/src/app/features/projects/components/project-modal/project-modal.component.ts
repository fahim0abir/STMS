import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PROJECT_STATUS_LABELS, Project, ProjectStatus, ProjectUpsert } from '../../../../shared/models/project.model';
import { User } from '../../../../shared/models/user.model';
import { ButtonComponent } from '../../../../shared/ui/button.component';

@Component({
  selector: 'app-project-modal',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent],
  template: `
    @if (open) {
    <div class="modal">
      <div class="modal__backdrop" (click)="close.emit()"></div>
      <div class="modal__panel" role="dialog" aria-modal="true">
        <h2 class="modal__title">{{ project ? 'Edit Project' : 'New Project' }}</h2>

        <form [formGroup]="form" (ngSubmit)="submit()" class="modal__form">
          <label class="modal__field">
            <span>Name *</span>
            <input type="text" formControlName="name" maxlength="200" />
            @if (form.controls.name.touched && form.controls.name.invalid) {
              <small class="modal__error">Name is required (max 200 characters).</small>
            }
          </label>

          <label class="modal__field">
            <span>Description</span>
            <textarea formControlName="description" rows="4" maxlength="1000"></textarea>
            @if (form.controls.description.touched && form.controls.description.invalid) {
              <small class="modal__error">Description must be at most 1000 characters.</small>
            }
          </label>

          <div class="modal__row">
            <label class="modal__field">
              <span>Start date *</span>
              <input type="date" formControlName="startDate" />
              @if (form.controls.startDate.touched && form.controls.startDate.invalid) {
                <small class="modal__error">Start date is required.</small>
              }
            </label>

            <label class="modal__field">
              <span>End date</span>
              <input type="date" formControlName="endDate" />
              @if (form.hasError('dateRange') && form.controls.endDate.touched) {
                <small class="modal__error">End date must be on or after the start date.</small>
              }
            </label>
          </div>

          <label class="modal__field">
            <span>Tech stack *</span>
            <input type="text" formControlName="techStack" maxlength="200" placeholder="e.g. Angular 18, ASP.NET Core 9, MSSQL" />
            @if (form.controls.techStack.touched && form.controls.techStack.invalid) {
              <small class="modal__error">Tech stack is required (max 200 characters).</small>
            }
          </label>

          <label class="modal__field">
            <span>Client</span>
            <input type="text" formControlName="clientName" maxlength="200" />
            @if (form.controls.clientName.touched && form.controls.clientName.invalid) {
              <small class="modal__error">Client name must be at most 200 characters.</small>
            }
          </label>

          <label class="modal__field">
            <span>Status *</span>
            <select formControlName="status">
              @for (option of statusOptions; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
          </label>

          @if (canAssignManager) {
            <label class="modal__field">
              <span>Project manager</span>
              <select formControlName="managerId">
                <option value="">Unassigned</option>
                @for (manager of managers; track manager.id) {
                  <option [value]="manager.id">{{ manager.name || manager.email }}</option>
                }
              </select>
            </label>
          }

          @if (errorMessage) {
            <p class="modal__error">{{ errorMessage }}</p>
          }

          <div class="modal__actions">
            <app-btn variant="ghost" (click)="close.emit()">Cancel</app-btn>
            <app-btn variant="primary" [loading]="saving" (click)="submit()">
              {{ project ? 'Save Changes' : 'Create Project' }}
            </app-btn>
          </div>
        </form>
      </div>
    </div>
    }
  `,
  styles: [
    `
      :host { display: contents; }
      .modal { position: fixed; inset: 0; z-index: 60; display: flex; align-items: center; justify-content: center; padding: 1rem; }
      .modal__backdrop { position: absolute; inset: 0; background: rgb(17 24 39 / 0.5); }
      .modal__panel { position: relative; background: #fff; border-radius: 0.75rem; padding: 1.5rem; width: 100%; max-width: 480px; box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.2); }
      .modal__title { margin: 0 0 1rem; font-size: 1.125rem; font-weight: 600; color: #111827; }
      .modal__form { display: flex; flex-direction: column; gap: 1rem; }
      .modal__row { display: flex; gap: 0.75rem; }
      .modal__row .modal__field { flex: 1; }
      .modal__field { display: flex; flex-direction: column; gap: 0.375rem; font-size: 0.8rem; color: #374151; }
      .modal__field input, .modal__field textarea, .modal__field select {
        border: 1px solid #d1d5db;
        border-radius: 0.5rem;
        padding: 0.5rem 0.75rem;
        font-size: 0.875rem;
        font-family: inherit;
        outline: none;
        background: #fff;
      }
      .modal__field input:focus, .modal__field textarea:focus, .modal__field select:focus { border-color: #2563eb; box-shadow: 0 0 0 3px rgb(37 99 235 / 0.15); }
      .modal__error { color: #b91c1c; font-size: 0.75rem; margin: 0; }
      .modal__actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.5rem; }
    `,
  ],
})
export class ProjectModalComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  @Input() open = false;
  @Input() project: Project | null = null;
  @Input() saving = false;
  @Input() errorMessage = '';
  @Input() managers: User[] = [];
  @Input() canAssignManager = false;

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<ProjectUpsert>();

  readonly statusOptions = Object.entries(PROJECT_STATUS_LABELS).map(([value, label]) => ({ value, label }));

  private readonly dateRangeValidator = (group: AbstractControl) => {
    const start = group.get('startDate')?.value;
    const end = group.get('endDate')?.value;
    if (!start || !end) return null;
    return new Date(end) >= new Date(start) ? null : { dateRange: true };
  };

  readonly form = this.fb.nonNullable.group(
    {
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(1000)]],
      startDate: ['', [Validators.required]],
      endDate: [''],
      techStack: ['', [Validators.required, Validators.maxLength(200)]],
      clientName: ['', [Validators.maxLength(200)]],
      status: [String(ProjectStatus.Active), [Validators.required]],
      managerId: [''],
    },
    { validators: this.dateRangeValidator },
  );

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']?.currentValue === true || changes['project']) {
      this.form.reset({
        name: this.project?.name ?? '',
        description: this.project?.description ?? '',
        startDate: this.toDateInput(this.project?.startDate),
        endDate: this.toDateInput(this.project?.endDate),
        techStack: this.project?.techStack ?? '',
        clientName: this.project?.clientName ?? '',
        status: String(this.project?.status ?? ProjectStatus.Active),
        managerId: this.project?.managerId ?? '',
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.saving) return;
    const raw = this.form.getRawValue();
    this.save.emit({
      name: raw.name.trim(),
      description: raw.description?.trim() || null,
      startDate: new Date(raw.startDate).toISOString(),
      endDate: raw.endDate ? new Date(raw.endDate).toISOString() : null,
      techStack: raw.techStack.trim(),
      clientName: raw.clientName?.trim() || null,
      status: Number(raw.status) as ProjectStatus,
      managerId: this.canAssignManager && raw.managerId ? raw.managerId : null,
    });
  }

  private toDateInput(value?: string | null): string {
    if (!value) return '';
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? '' : parsed.toISOString().slice(0, 10);
  }
}

