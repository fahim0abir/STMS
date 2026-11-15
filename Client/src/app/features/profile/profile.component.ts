import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { UsersService } from '../../core/services/users.service';
import { passwordsMatch } from '../../shared/passwords-match.validator';
import { ButtonComponent } from '../../shared/ui/button.component';
import { PasswordToggleComponent } from '../../shared/ui/password-toggle.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent, PasswordToggleComponent],
  template: `
    <div class="profile">
      <div class="profile__header">
        <h1>My Profile</h1>
        <p>Update your name, email and password</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="save()" class="profile__form">
        <label class="profile__field">
          <span>Full name</span>
          <input type="text" formControlName="name" maxlength="100" />
        </label>

        <label class="profile__field">
          <span>Email *</span>
          <input type="email" formControlName="email" maxlength="256" />
          @if (form.controls.email.touched && form.controls.email.invalid) {
            <small class="profile__error">A valid email is required.</small>
          }
        </label>

        <div class="profile__field">
          <label for="currentPassword">Current password</label>
          <div class="profile__password">
            <input
              id="currentPassword"
              formControlName="currentPassword"
              maxlength="100"
              autocomplete="current-password"
              [type]="showCurrentPassword() ? 'text' : 'password'"
            />
            <app-password-toggle [visible]="showCurrentPassword()" (toggled)="showCurrentPassword.set(!showCurrentPassword())" />
          </div>
        </div>

        <div class="profile__field">
          <label for="newPassword">New password</label>
          <div class="profile__password">
            <input
              id="newPassword"
              formControlName="newPassword"
              maxlength="100"
              autocomplete="new-password"
              [type]="showNewPassword() ? 'text' : 'password'"
            />
            <app-password-toggle [visible]="showNewPassword()" (toggled)="showNewPassword.set(!showNewPassword())" />
          </div>
          <small class="profile__hint">Leave blank to keep your current password.</small>
        </div>

        <div class="profile__field">
          <label for="confirmPassword">Confirm new password</label>
          <div class="profile__password">
            <input
              id="confirmPassword"
              formControlName="confirmPassword"
              maxlength="100"
              autocomplete="new-password"
              [type]="showConfirmPassword() ? 'text' : 'password'"
            />
            <app-password-toggle [visible]="showConfirmPassword()" (toggled)="showConfirmPassword.set(!showConfirmPassword())" />
          </div>
          @if (form.controls.confirmPassword.touched && form.errors?.['passwordsMismatch']) {
            <small class="profile__error">Passwords do not match.</small>
          }
        </div>

        @if (error()) {
          <p class="profile__error">{{ error() }}</p>
        }
        @if (message()) {
          <p class="profile__success">{{ message() }}</p>
        }

        <div class="profile__actions">
          <app-btn variant="primary" [loading]="saving()" (click)="save()">Save Profile</app-btn>
        </div>
      </form>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .profile__header { margin-bottom: 1rem; }
      .profile__header h1 { margin: 0; font-size: 1.5rem; color: #111827; }
      .profile__header p { margin: 0.25rem 0 0; color: #6b7280; font-size: 0.875rem; }
      .profile__form { display: flex; flex-direction: column; gap: 1rem; max-width: 480px; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; padding: 1.5rem; }
      .profile__field { display: flex; flex-direction: column; gap: 0.375rem; font-size: 0.8rem; color: #374151; }
      .profile__field input { border: 1px solid #d1d5db; border-radius: 0.5rem; padding: 0.5rem 0.75rem; font-size: 0.875rem; font-family: inherit; outline: none; }
      .profile__field input:focus { border-color: #2563eb; box-shadow: 0 0 0 3px rgb(37 99 235 / 0.15); }
      .profile__password { position: relative; display: block; }
      .profile__password input { width: 100%; box-sizing: border-box; padding-right: 2.5rem; }
      .profile__password app-password-toggle { position: absolute; top: 50%; right: 6px; transform: translateY(-50%); }
      .profile__hint { color: #6b7280; font-size: 0.75rem; }
      .profile__error { color: #b91c1c; font-size: 0.8125rem; margin: 0; }
      .profile__success { color: #15803d; font-size: 0.8125rem; margin: 0; }
      .profile__actions { display: flex; justify-content: flex-end; }
    `,
  ],
})
export class ProfileComponent {
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly saving = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly showCurrentPassword = signal(false);
  readonly showNewPassword = signal(false);
  readonly showConfirmPassword = signal(false);

  readonly form = this.fb.nonNullable.group(
    {
      name: [this.authService.currentUserValue?.name ?? '', [Validators.maxLength(100)]],
      email: [this.authService.currentUserValue?.email ?? '', [Validators.required, Validators.email]],
      currentPassword: [''],
      newPassword: [''],
      confirmPassword: [''],
    },
    { validators: passwordsMatch('newPassword', 'confirmPassword') },
  );

  save(): void {
    if (this.saving()) return;
    this.form.markAllAsTouched();
    const raw = this.form.getRawValue();
    const newPassword = raw.newPassword?.trim() ? raw.newPassword : null;
    const confirmPassword = raw.confirmPassword?.trim() ? raw.confirmPassword : null;

    if (newPassword && !raw.currentPassword) {
      this.error.set('Enter your current password to set a new password.');
      return;
    }
    if (newPassword && newPassword.length < 8) {
      this.error.set('New password must be at least 8 characters.');
      return;
    }
    if (newPassword && !confirmPassword) {
      this.error.set('Confirm your new password.');
      return;
    }
    if (newPassword !== confirmPassword) {
      this.error.set('New passwords do not match.');
      return;
    }
    if (this.form.controls.email.invalid || this.form.controls.name.invalid) return;

    this.saving.set(true);
    this.error.set('');
    this.message.set('');
    this.usersService
      .updateProfile({
        email: raw.email.trim(),
        name: raw.name?.trim() || null,
        currentPassword: raw.currentPassword || null,
        newPassword,
        confirmPassword,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (user) => {
          this.saving.set(false);
          this.authService.updateCurrentUser(user);
          this.form.controls.currentPassword.setValue('');
          this.form.controls.newPassword.setValue('');
          this.form.controls.confirmPassword.setValue('');
          this.message.set('Profile updated.');
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(err?.error?.message || 'Failed to update profile.');
        },
      });
  }
}