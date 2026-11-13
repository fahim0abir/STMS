import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ApiResponse } from '../../shared/models/api-response.model';
import { DashboardStats } from '../../shared/models/dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getStats(): Observable<DashboardStats> {
    return this.http
      .get<ApiResponse<DashboardStats>>(`${API_BASE_URL}/dashboard/stats`)
      .pipe(map((res) => res.data));
  }
}
