import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsersService } from '../../core/services/users.service';
import { AuthService } from '../../core/auth/auth.service';
import { uniqueEmail } from '../../shared/unique-email.validator';
import { Role, ROLE_LABELS } from '../../shared/models/enums';
import { User } from '../../shared/models/user.model';
import { BadgeComponent } from '../../shared/ui/badge.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { SpinnerComponent } from '../../shared/ui/spinner.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [ReactiveFormsModule, BadgeComponent, ButtonComponent, SpinnerComponent],
  template: `
    <div class="users">
      <div class="users__header">
        <div>
          <h1>Users</h1>
          <p>Create project managers and team members</p>
        </div>
        <app-btn variant="primary" (click)="openCreate()">New User</app-btn>
      </div>

      @if (loading()) {
        <div class="users__state"><app-spinner></app-spinner><span>Loading users…</span></div>
      }

      @if (!loading() && error()) {
        <div class="users__state users__state--error">
          <span>{{ error() }}</span>
          <app-btn size="sm" variant="outline" (click)="load()">Retry</app-btn>
        </div>
      }

      @if (!loading() && actionError()) {
        <p class="users__action-error">{{ actionError() }}</p>
      }

      @if (!loading() && !error()) {
        <div class="users__table-wrap">
          <table class="users__table">
            <thead>
              <tr><th>Name</th><th>Email</th><th>Role</th><th>Actions</th></tr>
            </thead>
            <tbody>
              @for (user of users(); track user.id) {
                <tr>
                  <td>{{ user.name || '—' }}</td>
                  <td class="users__muted">{{ user.email }}</td>
                  <td><app-badge variant="secondary" [label]="user.role"></app-badge></td>
                  <td>
                    @if (user.role === 'TeamMember') {
                      <app-btn
                        size="sm"
                        variant="outline"
                        [loading]="updatingId() === user.id"
                        [attr.data-testid]="'promote-user-' + user.id"
                        (click)="promote(user)"
                      >Make PM</app-btn>
                    }
                  </td>
                </tr>
              }
              @if (!users().length) {
                <tr><td colspan="4" class="users__empty">No users found.</td></tr>
              }
            </tbody>
          </table>
        </div>
      }

      @if (modalOpen()) {
        <div class="modal">
          <div class="modal__backdrop" (click)="closeModal()"></div>
          <div class="modal__panel" role="dialog" aria-modal="true">
            <h2 class="modal__title">New User</h2>
            <form [formGroup]="form" (ngSubmit)="save()" class="modal__form">
              <label class="modal__field">
                <span>Email *</span>
                <input type="email" formControlName="email" maxlength="256" />
                @if (form.controls.email.touched && form.controls.email.hasError('emailTaken')) {
                  <small class="modal__error">This email is already registered.</small>
                } @else if (form.controls.email.touched && form.controls.email.invalid) {
                  <small class="modal__error">A valid email is required.</small>
                }
              </label>
              <label class="modal__field">
                <span>Full name</span>
                <input type="text" formControlName="name" maxlength="100" />
              </label>
              <label class="modal__field">
                <span>Password *</span>
                <input type="password" formControlName="password" maxlength="100" />
                @if (form.controls.password.touched && form.controls.password.invalid) {
                  <small class="modal__error">Password must be 8-100 characters.</small>
                }
              </label>
              <label class="modal__field">
                <span>Role *</span>
                <select formControlName="role">
                  @for (option of roleOptions; track option.value) {
                    <option [value]="option.value">{{ option.label }}</option>
                  }
                </select>
              </label>

              @if (modalError()) {
                <p class="modal__error">{{ modalError() }}</p>
              }

              <div class="modal__actions">
                <app-btn variant="ghost" (click)="closeModal()">Cancel</app-btn>
                <app-btn variant="primary" [loading]="saving()" (click)="save()">Create User</app-btn>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .users__header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
      .users__header h1 { margin: 0; font-size: 1.5rem; color: #111827; }
      .users__header p { margin: 0.25rem 0 0; color: #6b7280; font-size: 0.875rem; }
      .users__state { display: flex; align-items: center; gap: 0.5rem; color: #6b7280; padding: 1rem 0; }
      .users__state--error { color: #b91c1c; }
      .users__table-wrap { overflow-x: auto; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; }
      .users__table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
      .users__table th { text-align: left; padding: 0.75rem 1rem; background: #f9fafb; color: #6b7280; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e5e7eb; }
      .users__table td { padding: 0.75rem 1rem; border-bottom: 1px solid #f3f4f6; color: #111827; }
      .users__muted { color: #6b7280; }
      .users__action-error { margin: 0 0 0.75rem; color: #b91c1c; font-size: 0.875rem; }
      .users__empty { text-align: center; color: #9ca3af; padding: 2rem 1rem; }
      .modal { position: fixed; inset: 0; z-index: 60; display: flex; align-items: center; justify-content: center; padding: 1rem; }
      .modal__backdrop { position: absolute; inset: 0; background: rgb(17 24 39 / 0.5); }
      .modal__panel { position: relative; background: #fff; border-radius: 0.75rem; padding: 1.5rem; width: 100%; max-width: 440px; box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.2); }
      .modal__title { margin: 0 0 1rem; font-size: 1.125rem; font-weight: 600; color: #111827; }
      .modal__form { display: flex; flex-direction: column; gap: 1rem; }
      .modal__field { display: flex; flex-direction: column; gap: 0.375rem; font-size: 0.8rem; color: #374151; }
      .modal__field input, .modal__field select { border: 1px solid #d1d5db; border-radius: 0.5rem; padding: 0.5rem 0.75rem; font-size: 0.875rem; font-family: inherit; outline: none; background: #fff; }
      .modal__field input:focus, .modal__field select:focus { border-color: #2563eb; box-shadow: 0 0 0 3px rgb(37 99 235 / 0.15); }
      .modal__error { color: #b91c1c; font-size: 0.75rem; margin: 0; }
      .modal__actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.5rem; }
    `,
  ],
})
export class UsersComponent {
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly users = signal<User[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly modalOpen = signal(false);
  readonly saving = signal(false);
  readonly modalError = signal('');
  readonly updatingId = signal<string | null>(null);
  readonly actionError = signal('');

  readonly roleOptions = [
    { value: Role.ProjectManager, label: ROLE_LABELS[Role.ProjectManager] },
    { value: Role.TeamMember, label: ROLE_LABELS[Role.TeamMember] },
  ];

  readonly form = this.fb.nonNullable.group({
    email: [
      '',
      [Validators.required, Validators.email, Validators.maxLength(256)],
      uniqueEmail(this.authService),
    ],
    name: ['', [Validators.maxLength(100)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    role: [Role.TeamMember as Role, [Validators.required]],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.usersService
      .getAssignableUsers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => {
          this.users.set(users);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load users.');
          this.loading.set(false);
        },
      });
  }

  openCreate(): void {
    this.modalError.set('');
    this.form.reset({ email: '', name: '', password: '', role: Role.TeamMember });
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.saving.set(false);
  }

  promote(user: User): void {
    if (this.updatingId()) return;

    this.updatingId.set(user.id);
    this.actionError.set('');
    this.usersService
      .updateUserRole(user.id, Role.ProjectManager)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.updatingId.set(null);
          this.load();
        },
        error: (err) => {
          this.updatingId.set(null);
          this.actionError.set(err?.error?.message || 'Failed to update role.');
        },
      });
  }

  save(): void {
    if (this.saving()) return;
    if (this.form.invalid || this.form.pending) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.saving.set(true);
    this.modalError.set('');
    this.usersService
      .createUser({
        email: raw.email.trim(),
        name: raw.name?.trim() || null,
        password: raw.password,
        role: Number(raw.role) as Role,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.modalOpen.set(false);
          this.load();
        },
        error: (err) => {
          this.saving.set(false);
          this.modalError.set(err?.error?.message || 'Failed to create user.');
        },
      });
  }
}