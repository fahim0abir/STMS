import { Component, Input } from '@angular/core';

import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'app-btn',
  standalone: true,
  template: `
    <button
      type="button"
      [class]="btnClass"
      [disabled]="disabled || loading"
    >
      <span *ngIf="loading" class="btn__spinner"></span>
      <ng-content></ng-content>
    </button>
  `,
  styles: [
    `
      :host { display: contents; }
      .btn {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        border: none;
        border-radius: 0.5rem;
        cursor: pointer;
        font-weight: 500;
        transition: all 0.2s;
        font-family: inherit;
      }
      .btn:disabled { opacity: 0.6; cursor: not-allowed; }
      .btn-primary { background: #2563eb; color: #fff; }
      .btn-secondary { background: #e5e7eb; color: #111; }
      .btn-danger { background: #dc2626; color: #fff; }
      .btn-ghost { background: transparent; color: #374151; }
      .btn-outline { background: transparent; color: #2563eb; border: 1px solid #2563eb; }
      .btn-sm { padding: 0.375rem 0.75rem; font-size: 0.875rem; }
      .btn-md { padding: 0.5rem 1rem; font-size: 1rem; }
      .btn-lg { padding: 0.75rem 1.5rem; font-size: 1.125rem; }
      .btn__spinner {
        width: 1rem;
        height: 1rem;
        border: 2px solid transparent;
        border-top-color: currentColor;
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
      }
      @keyframes spin { to { transform: rotate(360deg); } }
    `,
  ],
})
export class ButtonComponent {
  @Input() variant: 'primary' | 'secondary' | 'danger' | 'ghost' | 'outline' = 'primary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() disabled = false;
  @Input() loading = false;

  get btnClass(): string {
    return `btn btn-${this.variant} btn-${this.size}`;
  }
}
