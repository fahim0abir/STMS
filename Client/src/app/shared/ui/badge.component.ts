import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-badge',
  standalone: true,
  template: `<span class="badge badge--{{ variant }}">{{ label }}</span>`,
  styles: [
    `
      :host { display: contents; }
      .badge {
        display: inline-flex;
        align-items: center;
        padding: 0.25rem 0.625rem;
        border-radius: 9999px;
        font-size: 0.75rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }
      .badge--primary { background: #dbeafe; color: #1d4ed8; }
      .badge--secondary { background: #f3f4f6; color: #374151; }
      .badge--success { background: #dcfce7; color: #15803d; }
      .badge--warning { background: #fef3c7; color: #92400e; }
      .badge--danger { background: #fee2e2; color: #b91c1c; }
      .badge--info { background: #dbeafe; color: #1d4ed8; }
    `,
  ],
})
export class BadgeComponent {
  @Input() variant: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info' = 'secondary';
  @Input() label = '';
}
