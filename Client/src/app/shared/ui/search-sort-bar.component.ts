import { Component, Input, Output, EventEmitter } from '@angular/core';

export interface SortOption {
  value: string;
  label: string;
}

import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'app-search-sort-bar',
  standalone: true,
  template: `
    <div class="toolbar">
      <input
        class="toolbar__search"
        type="search"
        [placeholder]="searchPlaceholder"
        [value]="searchValue"
        (input)="onSearch($event)"
      />
      <div class="toolbar__right">
        <select class="toolbar__select" [value]="sortBy" (change)="onSort($event)" *ngIf="sortOptions.length">
          <option *ngFor="let option of sortOptions" [value]="option.value">{{ option.label }}</option>
        </select>
        <button class="toolbar__order" type="button" (click)="toggleOrder()">
          {{ sortOrder === 'asc' ? 'ASC â†‘' : 'DESC â†“' }}
        </button>
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .toolbar { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
      .toolbar__search {
        flex: 1;
        min-width: 200px;
        border: 1px solid #d1d5db;
        border-radius: 0.5rem;
        padding: 0.5rem 0.75rem;
        font-size: 0.875rem;
        outline: none;
      }
      .toolbar__search:focus { border-color: #2563eb; box-shadow: 0 0 0 3px rgb(37 99 235 / 0.15); }
      .toolbar__right { display: flex; align-items: center; gap: 0.5rem; }
      .toolbar__select, .toolbar__order {
        border: 1px solid #d1d5db;
        border-radius: 0.5rem;
        background: #fff;
        color: #374151;
        padding: 0.45rem 0.6rem;
        font-size: 0.8rem;
        cursor: pointer;
      }
    `,
  ],
})
export class SearchSortBarComponent {
  @Input() searchPlaceholder = 'Searchâ€¦';
  @Input() searchValue = '';
  @Input() sortBy = '';
  @Input() sortOrder: 'asc' | 'desc' = 'asc';
  @Input() sortOptions: SortOption[] = [];

  @Output() searchChange = new EventEmitter<string>();
  @Output() sortChange = new EventEmitter<string>();
  @Output() sortOrderChange = new EventEmitter<'asc' | 'desc'>();

  onSearch(event: Event): void {
    this.searchChange.emit((event.target as HTMLInputElement).value);
  }

  onSort(event: Event): void {
    this.sortChange.emit((event.target as HTMLSelectElement).value);
  }

  toggleOrder(): void {
    this.sortOrderChange.emit(this.sortOrder === 'asc' ? 'desc' : 'asc');
  }
}

