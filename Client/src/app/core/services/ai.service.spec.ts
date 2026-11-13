import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AI_MAX_LENGTH, AI_MIN_LENGTH, AiService } from './ai.service';
import { TasksService } from './tasks.service';

const enhanceResponse = {
  enhancedDescription: '  Improved description  ',
  suggestedTitle: '  Title  ',
  acceptanceCriteria: ['criterion'],
  subtasks: ['subtask'],
  suggestedPriority: 2,
  estimatedHours: 4,
  model: 'gpt-4o-mini',
  usedFallback: false,
};

describe('AiService', () => {
  let service: AiService;
  let enhanceDescription: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    enhanceDescription = vi.fn().mockReturnValue(of(enhanceResponse));
    TestBed.configureTestingModule({
      providers: [{ provide: TasksService, useValue: { enhanceDescription } }],
    });
    service = TestBed.inject(AiService);
  });

  it('accepts descriptions inside the allowed length window', () => {
    expect(service.validate('a'.repeat(AI_MIN_LENGTH))).toBeNull();
    expect(service.validate('a'.repeat(AI_MAX_LENGTH))).toBeNull();
  });

  it('rejects descriptions that are too short', () => {
    expect(service.validate('short')).toBe(`Description must be at least ${AI_MIN_LENGTH} characters.`);
  });

  it('rejects descriptions that are too long', () => {
    expect(service.validate('a'.repeat(AI_MAX_LENGTH + 1))).toBe(
      `Description must be at most ${AI_MAX_LENGTH} characters.`,
    );
  });

  it('fails without calling the API when validation fails', async () => {
    await expect(firstValueFrom(service.enhance({ description: 'too short' }))).rejects.toThrow();
    expect(enhanceDescription).not.toHaveBeenCalled();
  });

  it('trims the request and normalizes the response', async () => {
    const result = await firstValueFrom(
      service.enhance({ description: '  Please improve this task description  ', title: '  Feature  ', context: '   ' }),
    );

    expect(enhanceDescription).toHaveBeenCalledWith({
      description: 'Please improve this task description',
      title: 'Feature',
      context: null,
    });
    expect(result.enhancedDescription).toBe('Improved description');
    expect(result.suggestedTitle).toBe('Title');
    expect(result.model).toBe('gpt-4o-mini');
    expect(result.usedFallback).toBe(false);
  });

  it('falls back to a local suggestion when the API fails', async () => {
    enhanceDescription.mockReturnValue(throwError(() => new Error('provider down')));

    const result = await firstValueFrom(
      service.enhance({ description: 'Implement token rotation for refresh flows.' }),
    );

    expect(result.usedFallback).toBe(true);
    expect(result.model).toBe('fallback');
    expect(result.enhancedDescription).toContain('Implement token rotation for refresh flows');
    expect(result.acceptanceCriteria.length).toBeGreaterThan(0);
    expect(result.message).toContain('local suggestion');
  });

  it('marks provider fallbacks coming from the backend', async () => {
    enhanceDescription.mockReturnValue(
      of({ ...enhanceResponse, model: 'fallback', usedFallback: true, enhancedDescription: '' }),
    );

    const result = await firstValueFrom(
      service.enhance({ description: 'Original description that is long enough.' }),
    );

    expect(result.enhancedDescription).toBe('Original description that is long enough.');
    expect(result.usedFallback).toBe(true);
    expect(result.message).toContain('Local fallback suggestion');
  });
});
