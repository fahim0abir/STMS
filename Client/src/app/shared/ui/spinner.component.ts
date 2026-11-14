import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  standalone: true,
  template: `<div class="spinner spinner--{{ size }}"></div>`,
  styles: [
    `
      :host { display: contents; }
      .spinner {
        width: 1.5rem;
        height: 1.5rem;
        border: 3px solid #e5e7eb;
        border-top-color: #2563eb;
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
      }
      .spinner--sm { width: 1rem; height: 1rem; border-width: 2px; }
      .spinner--lg { width: 2rem; height: 2rem; border-width: 4px; }
      @keyframes spin { to { transform: rotate(360deg); } }
    `,
  ],
})
export class SpinnerComponent {
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
}
