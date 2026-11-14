import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ApiResponse } from '../../shared/models/api-response.model';
import { Role } from '../../shared/models/enums';
import {
  CreateUserRequest,
  UpdateProfileRequest,
  User,
  normalizeUser,
} from '../../shared/models/user.model';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/users`;

  getAssignableUsers(role?: Role): Observable<User[]> {
    let params = new HttpParams();
    if (role !== undefined && role !== null) {
      params = params.set('role', String(role));
    }

    return this.http
      .get<ApiResponse<User[]>>(this.baseUrl, { params })
      .pipe(map((res) => (res.data ?? []).map((user) => normalizeUser(user) as User)));
  }

  createUser(request: CreateUserRequest): Observable<User> {
    return this.http
      .post<ApiResponse<User>>(this.baseUrl, request)
      .pipe(map((res) => normalizeUser(res.data) as User));
  }

  updateProfile(request: UpdateProfileRequest): Observable<User> {
    return this.http
      .put<ApiResponse<User>>(`${this.baseUrl}/me`, request)
      .pipe(map((res) => normalizeUser(res.data) as User));
  }
}