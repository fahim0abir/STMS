import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError, timeout } from 'rxjs';
import { TasksService } from './tasks.service';
import { AiEnhanceRequest, AiEnhanceResponse } from '../../shared/models/ai.model';

export const AI_MIN_LENGTH = 20;
export const AI_MAX_LENGTH = 4000;
const AI_TIMEOUT_MS = 30000;

export interface AiEnhanceResult extends AiEnhanceResponse {
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

  enhance(request: AiEnhanceRequest): Observable<AiEnhanceResult> {
    const description = (request.description ?? '').trim();
    const validationError = this.validate(description);
    if (validationError) return throwError(() => new Error(validationError));

    return this.tasksService
      .enhanceDescription({
        description,
        title: request.title?.trim() || null,
        context: request.context?.trim() || null,
      })
      .pipe(
        timeout(AI_TIMEOUT_MS),
        map((res) => this.normalize(res, description)),
        catchError(() =>
          of({
            ...this.fallback(description),
            message: 'AI enhancement is unavailable right now. A local suggestion was generated instead.',
          }),
        ),
      );
  }

  private normalize(res: AiEnhanceResponse, original: string): AiEnhanceResult {
    return {
      enhancedDescription: (res?.enhancedDescription ?? '').trim() || original,
      suggestedTitle: (res?.suggestedTitle ?? '').trim(),
      acceptanceCriteria: res?.acceptanceCriteria ?? [],
      subtasks: res?.subtasks ?? [],
      suggestedPriority: res?.suggestedPriority ?? 1,
      estimatedHours: res?.estimatedHours ?? null,
      model: res?.model ?? 'unknown',
      usedFallback: !!res?.usedFallback,
      message: res?.usedFallback ? 'Local fallback suggestion (AI provider unavailable).' : '',
    };
  }

  private fallback(description: string): AiEnhanceResult {
    const sentence = description.split(/(?<=[.!?])\s+/)[0] ?? description;
    return {
      enhancedDescription: `- ${sentence.replace(/\.$/, '')}.`,
      suggestedTitle: description.split(/\s+/).slice(0, 12).join(' '),
      acceptanceCriteria: ['Behaviour described in the task is implemented and verified.'],
      subtasks: ['Break the description into verifiable steps.'],
      suggestedPriority: 1,
      estimatedHours: null,
      model: 'fallback',
      usedFallback: true,
    };
  }
}
