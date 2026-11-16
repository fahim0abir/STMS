import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AiLogsService } from '../../core/services/ai-logs.service';
import { AiInteractionLog } from '../../shared/models/ai-log.model';
import { SpinnerComponent } from '../../shared/ui/spinner.component';
import { BadgeComponent } from '../../shared/ui/badge.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { PaginationComponent } from '../../shared/ui/pagination.component';

@Component({
  selector: 'app-ai-logs',
  standalone: true,
  imports: [SpinnerComponent, BadgeComponent, ButtonComponent, PaginationComponent],
  template: `
    <div class="ai-logs">
      <div class="ai-logs__header">
        <div>
          <h1>AI Call Logs</h1>
          <p>Successful AI provider calls with the caller, model, prompt and raw output.</p>
        </div>
        <app-btn size="sm" variant="outline" (click)="load()">Refresh</app-btn>
      </div>

      <div class="ai-logs__filters">
        <label class="ai-logs__field">
          <span>Search</span>
          <input
            type="search"
            placeholder="User, prompt or output…"
            [value]="keyword()"
            (input)="onKeyword($event)"
          />
        </label>
        <label class="ai-logs__field">
          <span>Model</span>
          <input
            type="text"
            placeholder="e.g. gemini-3.1-flash-lite"
            [value]="model()"
            (input)="onModel($event)"
          />
        </label>
      </div>

      @if (loading()) {
        <div class="ai-logs__state"><app-spinner></app-spinner><span>Loading AI logs…</span></div>
      }

      @if (!loading() && error()) {
        <div class="ai-logs__state ai-logs__state--error">
          <span>{{ error() }}</span>
          <app-btn size="sm" variant="outline" (click)="load()">Retry</app-btn>
        </div>
      }
      @if (!loading() && !error()) {
        <div class="ai-logs__table-wrap">
          <table class="ai-logs__table">
            <thead>
              <tr>
                <th>When</th>
                <th>User</th>
                <th>Model</th>
                <th>Duration</th>
                <th>Prompt</th>
                <th>Output</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (log of logs(); track log.id) {
                <tr>
                  <td class="ai-logs__muted">{{ when(log) }}</td>
                  <td>
                    <div class="ai-logs__user">{{ log.userName || log.userEmail }}</div>
                    <div class="ai-logs__muted">{{ log.userEmail }}</div>
                  </td>
                  <td><app-badge variant="secondary" [label]="log.model"></app-badge></td>
                  <td class="ai-logs__muted">{{ log.durationMs }} ms</td>
                  <td class="ai-logs__preview">{{ preview(log.prompt) }}</td>
                  <td class="ai-logs__preview">{{ preview(log.outputJson) }}</td>
                  <td><app-btn size="sm" variant="ghost" (click)="inspect(log)">View</app-btn></td>
                </tr>
              }
              @if (!logs().length) {
                <tr>
                  <td colspan="7" class="ai-logs__empty">No AI calls recorded yet.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <app-pagination
          [pageNumber]="pageNumber()"
          [pageSize]="pageSize"
          [totalCount]="totalCount()"
          [totalPages]="totalPages()"
          (pageChange)="onPageChange($event)"
        ></app-pagination>
      }

      @if (selected(); as log) {
        <div class="ai-modal">
          <div class="ai-modal__backdrop" (click)="inspect(null)"></div>
          <div class="ai-modal__panel" role="dialog" aria-modal="true">
            <h2 class="ai-modal__title">AI call · {{ log.model }}</h2>
            <p class="ai-modal__meta">
              {{ log.userName || log.userEmail }} · {{ log.userRole || 'n/a' }} · {{ when(log) }} ·
              {{ log.durationMs }} ms
            </p>
            <h3 class="ai-modal__label">Prompt</h3>
            <pre class="ai-modal__pre">{{ log.prompt }}</pre>
            <h3 class="ai-modal__label">Output</h3>
            <pre class="ai-modal__pre">{{ log.outputJson }}</pre>
            <div class="ai-modal__actions">
              <app-btn size="sm" variant="ghost" (click)="inspect(null)">Close</app-btn>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .ai-logs__header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 1rem; }
      .ai-logs__header h1 { margin: 0 0 0.25rem; font-size: 1.5rem; color: #111827; }
      .ai-logs__header p { margin: 0; font-size: 0.9rem; color: #6b7280; }
      .ai-logs__filters { display: flex; flex-wrap: wrap; gap: 0.75rem; margin-bottom: 1rem; }
      .ai-logs__field { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.8rem; color: #374151; }
      .ai-logs__field input { border: 1px solid #d1d5db; border-radius: 0.5rem; padding: 0.5rem 0.75rem; font-size: 0.875rem; min-width: 240px; }
      .ai-logs__field input:focus { outline: none; border-color: #2563eb; box-shadow: 0 0 0 3px rgb(37 99 235 / 0.15); }
      .ai-logs__state { display: flex; align-items: center; gap: 0.75rem; padding: 2rem; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; color: #4b5563; font-size: 0.9rem; }
      .ai-logs__state--error { color: #b91c1c; border-color: #fecaca; background: #fef2f2; }
      .ai-logs__table-wrap { overflow-x: auto; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; margin-bottom: 1rem; }
      .ai-logs__table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
      .ai-logs__table th { text-align: left; padding: 0.75rem; background: #f9fafb; color: #6b7280; font-weight: 600; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.03em; }
      .ai-logs__table td { padding: 0.75rem; border-top: 1px solid #f3f4f6; vertical-align: top; color: #111827; }
      .ai-logs__muted { color: #6b7280; font-size: 0.8rem; }
      .ai-logs__user { font-weight: 500; }
      .ai-logs__preview { max-width: 260px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #4b5563; }
      .ai-logs__empty { text-align: center; color: #9ca3af; padding: 2rem; }
      .ai-modal { position: fixed; inset: 0; z-index: 60; display: flex; align-items: center; justify-content: center; padding: 1rem; }
      .ai-modal__backdrop { position: absolute; inset: 0; background: rgb(17 24 39 / 0.5); }
      .ai-modal__panel { position: relative; background: #fff; border-radius: 0.75rem; padding: 1.5rem; width: 100%; max-width: 720px; max-height: 85dvh; overflow-y: auto; box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.2); }
      .ai-modal__title { margin: 0 0 0.25rem; font-size: 1.1rem; color: #111827; }
      .ai-modal__meta { margin: 0 0 1rem; font-size: 0.8rem; color: #6b7280; }
      .ai-modal__label { margin: 0 0 0.375rem; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.03em; color: #6b7280; }
      .ai-modal__pre { margin: 0 0 1rem; padding: 0.75rem; background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 0.5rem; font-size: 0.8rem; white-space: pre-wrap; word-break: break-word; max-height: 240px; overflow-y: auto; }
      .ai-modal__actions { display: flex; justify-content: flex-end; }
    `,
  ],
})
export class AiLogsComponent {
  private readonly aiLogsService = inject(AiLogsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly keywordInput$ = new Subject<string>();

  readonly logs = signal<AiInteractionLog[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly keyword = signal('');
  readonly model = signal('');
  readonly pageNumber = signal(1);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly selected = signal<AiInteractionLog | null>(null);
  readonly pageSize = 20;

  constructor() {
    this.keywordInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.keyword.set(value);
        this.pageNumber.set(1);
        this.load();
      });
    this.load();
  }

  onKeyword(event: Event): void {
    this.keywordInput$.next((event.target as HTMLInputElement).value);
  }

  onModel(event: Event): void {
    this.model.set((event.target as HTMLInputElement).value.trim());
    this.pageNumber.set(1);
    this.load();
  }

  onPageChange(page: number): void {
    this.pageNumber.set(page);
    this.load();
  }

  inspect(log: AiInteractionLog | null): void {
    this.selected.set(log);
  }

  when(log: AiInteractionLog): string {
    const parsed = new Date(log.createdAt);
    return Number.isNaN(parsed.getTime()) ? log.createdAt : parsed.toLocaleString();
  }

  preview(value: string, max = 90): string {
    const text = (value ?? '').replace(/\s+/g, ' ').trim();
    return text.length <= max ? text : `${text.slice(0, max)}…`;
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.aiLogsService
      .getPaged({
        keyword: this.keyword() || undefined,
        model: this.model() || undefined,
        pageNumber: this.pageNumber(),
        pageSize: this.pageSize,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.logs.set(result.items ?? []);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load AI logs.');
          this.loading.set(false);
        },
      });
  }
}
