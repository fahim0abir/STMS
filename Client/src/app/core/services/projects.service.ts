import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ApiResponse } from '../../shared/models/api-response.model';
import { PagedResult } from '../../shared/models/paged-result.model';
import { Project, ProjectQuery, ProjectUpsert } from '../../shared/models/project.model';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/projects`;

  getPaged(query: ProjectQuery): Observable<PagedResult<Project>> {
    return this.http
      .get<ApiResponse<PagedResult<Project>>>(`${this.baseUrl}/paged`, { params: this.toParams(query) })
      .pipe(map((res) => res.data));
  }

  getById(id: string): Observable<Project> {
    return this.http.get<ApiResponse<Project>>(`${this.baseUrl}/${id}`).pipe(map((res) => res.data));
  }

  create(dto: ProjectUpsert): Observable<Project> {
    return this.http.post<ApiResponse<Project>>(this.baseUrl, dto).pipe(map((res) => res.data));
  }

  update(id: string, dto: ProjectUpsert): Observable<Project | null> {
    return this.http.put<ApiResponse<Project>>(`${this.baseUrl}/${id}`, dto).pipe(map((res) => res.data));
  }

  assignManager(id: string, managerId: string): Observable<void> {
    return this.http
      .patch<ApiResponse<object>>(`${this.baseUrl}/${id}/manager`, { managerId })
      .pipe(map(() => void 0));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<ApiResponse<object>>(`${this.baseUrl}/${id}`).pipe(map(() => void 0));
  }

  private toParams(query: ProjectQuery): HttpParams {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }
}
