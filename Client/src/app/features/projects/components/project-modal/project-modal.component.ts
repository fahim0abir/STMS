import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Project, ProjectUpsert } from '../../../../shared/models/project.model';
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
      .modal__field { display: flex; flex-direction: column; gap: 0.375rem; font-size: 0.8rem; color: #374151; }
      .modal__field input, .modal__field textarea {
        border: 1px solid #d1d5db;
        border-radius: 0.5rem;
        padding: 0.5rem 0.75rem;
        font-size: 0.875rem;
        font-family: inherit;
        outline: none;
      }
      .modal__field input:focus, .modal__field textarea:focus { border-color: #2563eb; box-shadow: 0 0 0 3px rgb(37 99 235 / 0.15); }
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

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<ProjectUpsert>();

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']?.currentValue === true || changes['project']) {
      this.form.reset({
        name: this.project?.name ?? '',
        description: this.project?.description ?? '',
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.saving) return;
    this.save.emit(this.form.getRawValue());
  }
}

