import { Component, Input } from '@angular/core';

export interface BreakdownRow {
  label: string;
  value: number;
  color?: string;
}

@Component({
  selector: 'app-stat-breakdown',
  standalone: true,
  template: `
    <div class="breakdown">
      <h3 class="breakdown__title">{{ title }}</h3>
      @if (!rows.length) {
        <p class="breakdown__empty">No data yet.</p>
      }
      @if (rows.length) {
        <ul class="breakdown__list">
          @for (row of rows; track row.label) {
            <li class="breakdown__row">
              <span class="breakdown__label">{{ row.label }}</span>
              <span class="breakdown__bar">
                <span
                  class="breakdown__fill"
                  [style.width.%]="percent(row.value)"
                  [style.background]="row.color || '#2563eb'"
                ></span>
              </span>
              <span class="breakdown__value">{{ row.value }}</span>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .breakdown { background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; padding: 1.25rem; }
      .breakdown__title { margin: 0 0 1rem; font-size: 1rem; font-weight: 600; color: #111827; }
      .breakdown__empty { color: #9ca3af; font-size: 0.875rem; margin: 0; }
      .breakdown__list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.625rem; }
      .breakdown__row { display: grid; grid-template-columns: 6rem 1fr 2rem; align-items: center; gap: 0.5rem; }
      .breakdown__label { font-size: 0.8rem; color: #4b5563; }
      .breakdown__bar { display: block; height: 0.5rem; background: #f3f4f6; border-radius: 9999px; overflow: hidden; }
      .breakdown__fill { display: block; height: 100%; border-radius: 9999px; transition: width 0.3s ease; }
      .breakdown__value { font-size: 0.8rem; font-weight: 600; color: #111827; text-align: right; }
    `,
  ],
})
export class StatBreakdownComponent {
  @Input() title = '';
  @Input() rows: BreakdownRow[] = [];

  private get max(): number {
    return this.rows.reduce((m, r) => Math.max(m, r.value), 0) || 1;
  }

  percent(value: number): number {
    return Math.round((value / this.max) * 100);
  }
}

