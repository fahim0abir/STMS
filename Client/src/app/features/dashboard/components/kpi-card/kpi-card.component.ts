import { Component, Input } from '@angular/core';

import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'app-kpi-card',
  standalone: true,
  template: `
    <div class="kpi kpi--{{ tone }}">
      <span class="kpi__label">{{ label }}</span>
      <span class="kpi__value">{{ value }}</span>
      <span *ngIf="hint" class="kpi__hint">{{ hint }}</span>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .kpi {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        background: #fff;
        border: 1px solid #e5e7eb;
        border-left: 4px solid #9ca3af;
        border-radius: 0.75rem;
        padding: 1rem 1.25rem;
        box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
      }
      .kpi--primary { border-left-color: #2563eb; }
      .kpi--success { border-left-color: #16a34a; }
      .kpi--warning { border-left-color: #d97706; }
      .kpi--danger { border-left-color: #dc2626; }
      .kpi__label { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; color: #6b7280; }
      .kpi__value { font-size: 1.75rem; font-weight: 700; color: #111827; line-height: 1.1; }
      .kpi__hint { font-size: 0.75rem; color: #9ca3af; }
    `,
  ],
})
export class KpiCardComponent {
  @Input() label = '';
  @Input() value: number | string = 0;
  @Input() hint = '';
  @Input() tone: 'primary' | 'success' | 'warning' | 'danger' | 'neutral' = 'neutral';
}

