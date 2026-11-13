import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { TasksService } from './tasks.service';
import { API_BASE_URL } from '../api.config';
import { TaskPriority, TaskStatus } from '../../shared/models/enums';
import { TaskItem } from '../../shared/models/task.model';

const rawTask = {
  id: 't1',
  projectId: 'p1',
  title: 'Fix login',
  status: 'Completed',
  priority: 2,
  assigneeEmail: 'dev@example.com',
};

describe('TasksService', () => {
  let service: TasksService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TasksService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('maps the paged payload and normalizes each task', () => {
    let result: TaskItem[] = [];
    let totalPages = 0;

    service
      .getPaged({ keyword: 'login', pageNumber: 2, pageSize: 10, status: TaskStatus.ToDo })
      .subscribe((page) => {
        result = page.items;
        totalPages = page.totalPages;
      });

    const req = httpMock.expectOne((request) => request.url === `${API_BASE_URL}/taskitems/paged`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('keyword')).toBe('login');
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('status')).toBe('0');

    req.flush({
      success: true,
      statusCode: 200,
      data: { items: [rawTask], totalCount: 1, pageNumber: 2, pageSize: 10, totalPages: 3 },
    });

    expect(result[0].status).toBe(TaskStatus.Completed);
    expect(result[0].priority).toBe(TaskPriority.High);
    expect(totalPages).toBe(3);
  });

  it('omits empty query values from the request', () => {
    service.getPaged({ keyword: '', pageNumber: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((request) => request.url === `${API_BASE_URL}/taskitems/paged`);
    expect(req.request.params.has('keyword')).toBe(false);
    expect(req.request.params.get('pageNumber')).toBe('1');
    req.flush({ success: true, statusCode: 200, data: { items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 } });
  });

  it('creates a task with the project id as a query parameter', () => {
    service
      .create('p1', { title: 'New', status: TaskStatus.ToDo, priority: TaskPriority.Low })
      .subscribe();

    const req = httpMock.expectOne((request) => request.url === `${API_BASE_URL}/taskitems`);
    expect(req.request.method).toBe('POST');
    expect(req.request.params.get('projectId')).toBe('p1');
    req.flush({ success: true, statusCode: 201, data: rawTask });
  });

  it('returns null when an update response carries no data', () => {
    let updated: TaskItem | null = null;

    service
      .update('t1', { title: 'New', status: TaskStatus.ToDo, priority: TaskPriority.Low })
      .subscribe((value) => (updated = value));

    const req = httpMock.expectOne(`${API_BASE_URL}/taskitems/t1`);
    expect(req.request.method).toBe('PUT');
    req.flush({ success: true, statusCode: 200, data: null });

    expect(updated).toBeNull();
  });

  it('resolves delete to void', () => {
    let completed = false;

    service.delete('t1').subscribe(() => (completed = true));

    const req = httpMock.expectOne(`${API_BASE_URL}/taskitems/t1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true, statusCode: 204, data: {} });

    expect(completed).toBe(true);
  });

  it('unwraps the AI enhancement payload', () => {
    let model = '';

    service.enhanceDescription({ description: 'Improve this description please' }).subscribe((res) => (model = res.model));

    const req = httpMock.expectOne(`${API_BASE_URL}/tasks/enhance-description`);
    expect(req.request.method).toBe('POST');
    req.flush({
      success: true,
      statusCode: 200,
      data: {
        enhancedDescription: 'Improved',
        suggestedTitle: 'Title',
        acceptanceCriteria: [],
        subtasks: [],
        suggestedPriority: 1,
        model: 'gpt-4o-mini',
        usedFallback: false,
      },
    });

    expect(model).toBe('gpt-4o-mini');
  });
});
