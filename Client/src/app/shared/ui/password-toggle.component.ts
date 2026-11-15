import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-password-toggle',
  standalone: true,
  template: `
    <button
      type="button"
      class="password-toggle"
      [attr.aria-label]="visible ? 'Hide password' : 'Show password'"
      [attr.aria-pressed]="visible"
      (click)="toggled.emit()"
    >
      @if (visible) {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M3 3l18 18" />
          <path d="M10.584 10.587a2 2 0 0 0 2.828 2.83" />
          <path d="M9.363 5.365A9.466 9.466 0 0 1 12 5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639a10.7 10.7 0 0 1-1.53 2.706" />
          <path d="M6.228 6.228A10.7 10.7 0 0 0 2.037 11.68a1.012 1.012 0 0 0 0 .64C3.423 16.49 7.36 19.5 12 19.5c1.05 0 2.06-.156 3.01-.44" />
        </svg>
      } @else {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
          <circle cx="12" cy="12" r="3" />
        </svg>
      }
    </button>
  `,
  styles: [
    `
      :host { display: inline-flex; }
      .password-toggle {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 1.75rem;
        height: 1.75rem;
        padding: 0;
        background: transparent;
        border: none;
        border-radius: 0.375rem;
        color: #6b7280;
        cursor: pointer;
      }
      .password-toggle:hover { color: #111827; }
      .password-toggle:focus-visible { outline: 2px solid #2563eb; outline-offset: 1px; }
      .password-toggle svg { width: 1.125rem; height: 1.125rem; }
    `,
  ],
})
export class PasswordToggleComponent {
  @Input() visible = false;
  @Output() toggled = new EventEmitter<void>();
}
