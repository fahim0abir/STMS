import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ApiResponse } from '../../shared/models/api-response.model';
import { PagedResult } from '../../shared/models/paged-result.model';
import { AiInteractionLog, AiInteractionLogQuery } from '../../shared/models/ai-log.model';

@Injectable({ providedIn: 'root' })
export class AiLogsService {
  private readonly http = inject(HttpClient);
  private readonly logsUrl = `${API_BASE_URL}/ailogs`;

  getPaged(query: AiInteractionLogQuery): Observable<PagedResult<AiInteractionLog>> {
    return this.http
      .get<ApiResponse<PagedResult<AiInteractionLog>>>(`${this.logsUrl}/paged`, { params: this.toParams(query) })
      .pipe(map((res) => res.data));
  }

  private toParams(query: AiInteractionLogQuery): HttpParams {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }
}
