import { Component, Input } from '@angular/core';

import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'app-completion-ring',
  standalone: true,
  template: `
    <div class="ring">
      <h3 class="ring__title">Completed vs Pending</h3>
      <div class="ring__body">
        <div
          class="ring__circle"
          [style.background]="'conic-gradient(#16a34a 0% ' + percent + '%, #e5e7eb ' + percent + '% 100%)'"
        >
          <span class="ring__percent">{{ percent }}%</span>
        </div>
        <ul class="ring__legend">
          <li><span class="ring__dot ring__dot--done"></span>Completed: {{ completed }}</li>
          <li><span class="ring__dot ring__dot--pending"></span>Pending: {{ pending }}</li>
        </ul>
      </div>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .ring { background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; padding: 1.25rem; }
      .ring__title { margin: 0 0 1rem; font-size: 1rem; font-weight: 600; color: #111827; }
      .ring__body { display: flex; align-items: center; gap: 1.5rem; }
      .ring__circle {
        width: 7rem;
        height: 7rem;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
      }
      .ring__percent {
        width: 5rem;
        height: 5rem;
        border-radius: 50%;
        background: #fff;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.125rem;
        font-weight: 700;
        color: #111827;
      }
      .ring__legend { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.5rem; font-size: 0.875rem; color: #4b5563; }
      .ring__dot { display: inline-block; width: 0.625rem; height: 0.625rem; border-radius: 50%; margin-right: 0.5rem; }
      .ring__dot--done { background: #16a34a; }
      .ring__dot--pending { background: #e5e7eb; }
    `,
  ],
})
export class CompletionRingComponent {
  @Input() completed = 0;
  @Input() pending = 0;

  get percent(): number {
    const total = this.completed + this.pending;
    return total ? Math.round((this.completed / total) * 100) : 0;
  }
}

