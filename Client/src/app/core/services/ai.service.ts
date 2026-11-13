import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError, timeout } from 'rxjs';
import { TasksService } from './tasks.service';

export const AI_MIN_LENGTH = 5;
export const AI_MAX_LENGTH = 1000;
const AI_TIMEOUT_MS = 20000;

export interface AiEnhanceResult {
  text: string;
  usedFallback: boolean;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly tasksService = inject(TasksService);

  validate(description: string): string | null {
    const value = (description ?? '').trim();
    if (value.length < AI_MIN_LENGTH) return `Description must be at least ${AI_MIN_LENGTH} characters.`;
    if (value.length > AI_MAX_LENGTH) return `Description must be at most ${AI_MAX_LENGTH} characters.`;
    return null;
  }

  enhance(description: string): Observable<AiEnhanceResult> {
    const validationError = this.validate(description);
    if (validationError) return throwError(() => new Error(validationError));

    const original = description.trim();
    return this.tasksService.enhanceDescription(original).pipe(
      timeout(AI_TIMEOUT_MS),
      map((text) => ({ text: text?.trim() || original, usedFallback: false })),
      catchError(() =>
        of({
          text: original,
          usedFallback: true,
          message: 'AI enhancement is unavailable right now. Your original description was kept.',
        }),
      ),
    );
  }
}
