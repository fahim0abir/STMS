import { Component, Input, Output, EventEmitter } from '@angular/core';

import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'app-pagination',
  standalone: true,
  template: `
    <div class="pagination">
      <span class="pagination__summary">
        Page {{ pageNumber }} of {{ totalPages || 1 }} Â· {{ totalCount }} item{{ totalCount === 1 ? '' : 's' }}
      </span>
      <div class="pagination__controls">
        <button class="pagination__btn" [disabled]="pageNumber <= 1" (click)="go(pageNumber - 1)">Prev</button>
        <button class="pagination__btn" [disabled]="pageNumber >= totalPages" (click)="go(pageNumber + 1)">
          Next
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .pagination { display: flex; align-items: center; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
      .pagination__summary { font-size: 0.8rem; color: #6b7280; }
      .pagination__controls { display: flex; gap: 0.5rem; }
      .pagination__btn {
        border: 1px solid #d1d5db;
        background: #fff;
        color: #374151;
        border-radius: 0.375rem;
        padding: 0.375rem 0.75rem;
        font-size: 0.8rem;
        cursor: pointer;
      }
      .pagination__btn:disabled { opacity: 0.5; cursor: not-allowed; }
      .pagination__btn:not(:disabled):hover { background: #f3f4f6; }
    `,
  ],
})
export class PaginationComponent {
  @Input() pageNumber = 1;
  @Input() pageSize = 20;
  @Input() totalCount = 0;
  @Input() totalPages = 0;
  @Output() pageChange = new EventEmitter<number>();

  go(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.pageNumber) return;
    this.pageChange.emit(page);
  }
}

