import { Component, Input, Output, EventEmitter } from '@angular/core';

export interface FilterChip<T> {
  value: T | undefined;
  label: string;
}

import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'app-task-filter-chips',
  standalone: true,
  template: `
    <div class="chips">
      <span class="chips__label">{{ label }}</span>
      <button
        *ngFor="let chip of chips"
        type="button"
        class="chips__chip"
        [class.chips__chip--active]="chip.value === active"
        (click)="select(chip.value)"
      >
        {{ chip.label }}
      </button>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .chips { display: flex; align-items: center; gap: 0.375rem; flex-wrap: wrap; }
      .chips__label { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: #6b7280; margin-right: 0.25rem; }
      .chips__chip {
        border: 1px solid #d1d5db;
        background: #fff;
        color: #374151;
        border-radius: 9999px;
        padding: 0.25rem 0.75rem;
        font-size: 0.75rem;
        cursor: pointer;
        transition: all 0.15s;
      }
      .chips__chip:hover { background: #f3f4f6; }
      .chips__chip--active { background: #2563eb; border-color: #2563eb; color: #fff; }
    `,
  ],
})
export class TaskFilterChipsComponent<T = number> {
  @Input() label = '';
  @Input() chips: FilterChip<T>[] = [];
  @Input() active: T | undefined = undefined;

  @Output() activeChange = new EventEmitter<T | undefined>();

  select(value: T | undefined): void {
    this.activeChange.emit(value);
  }
}

