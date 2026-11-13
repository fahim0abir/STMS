import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../core/auth/auth.service';
import { passwordsMatch } from '../../../shared/passwords-match.validator';

import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly error = signal<string>('');
  readonly isLoading = signal<boolean>(false);

  readonly form = this.fb.group(
    {
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch('password', 'confirmPassword') },
  );

  onSubmit(): void {
    if (this.form.invalid) return;
    this.isLoading.set(true);
    this.error.set('');
    this.authService
      .register(this.form.value as never)
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: () => this.router.navigate(['/login']),
        error: (err) => {
          this.error.set(err?.error?.message || 'Registration failed');
          this.isLoading.set(false);
        },
      });
  }
}

