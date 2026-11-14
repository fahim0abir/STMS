import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ApiResponse } from '../../shared/models/api-response.model';
import { AiEnhanceRequest, AiEnhanceResponse } from '../../shared/models/ai.model';
import { TaskStatus } from '../../shared/models/enums';
import { PagedResult } from '../../shared/models/paged-result.model';
import {
  TaskItem,
  TaskItemQuery,
  TaskItemUpsert,
  normalizeTask,
} from '../../shared/models/task.model';

@Injectable({ providedIn: 'root' })
export class TasksService {
  private readonly http = inject(HttpClient);
  private readonly tasksUrl = `${API_BASE_URL}/taskitems`;
  private readonly enhanceUrl = `${API_BASE_URL}/tasks/enhance-description`;

  getPaged(query: TaskItemQuery): Observable<PagedResult<TaskItem>> {
    return this.http
      .get<ApiResponse<PagedResult<TaskItem>>>(`${this.tasksUrl}/paged`, { params: this.toParams(query) })
      .pipe(
        map((res) => ({
          ...res.data,
          items: (res.data.items ?? []).map((item) => normalizeTask(item as never)),
        })),
      );
  }

  getById(id: string): Observable<TaskItem> {
    return this.http
      .get<ApiResponse<TaskItem>>(`${this.tasksUrl}/${id}`)
      .pipe(map((res) => normalizeTask(res.data as never)));
  }

  getByProject(projectId: string): Observable<TaskItem[]> {
    const params = new HttpParams().set('projectId', projectId);
    return this.http
      .get<ApiResponse<TaskItem[]>>(this.tasksUrl, { params })
      .pipe(map((res) => (res.data ?? []).map((item) => normalizeTask(item as never))));
  }

  create(projectId: string, dto: TaskItemUpsert): Observable<TaskItem> {
    const params = new HttpParams().set('projectId', projectId);
    return this.http
      .post<ApiResponse<TaskItem>>(this.tasksUrl, dto, { params })
      .pipe(map((res) => normalizeTask(res.data as never)));
  }

  update(id: string, dto: TaskItemUpsert): Observable<TaskItem | null> {
    return this.http
      .put<ApiResponse<TaskItem>>(`${this.tasksUrl}/${id}`, dto)
      .pipe(map((res) => (res.data ? normalizeTask(res.data as never) : null)));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<ApiResponse<object>>(`${this.tasksUrl}/${id}`).pipe(map(() => void 0));
  }

  assign(id: string, assigneeId: string | null): Observable<void> {
    return this.http
      .patch<ApiResponse<object>>(`${this.tasksUrl}/${id}/assignee`, { assigneeId })
      .pipe(map(() => void 0));
  }

  updateStatus(id: string, status: TaskStatus): Observable<void> {
    return this.http
      .patch<ApiResponse<object>>(`${this.tasksUrl}/${id}/status`, { status })
      .pipe(map(() => void 0));
  }

  enhanceDescription(request: AiEnhanceRequest): Observable<AiEnhanceResponse> {
    return this.http
      .post<ApiResponse<AiEnhanceResponse>>(this.enhanceUrl, request)
      .pipe(map((res) => res.data));
  }

  private toParams(query: TaskItemQuery): HttpParams {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }
}
