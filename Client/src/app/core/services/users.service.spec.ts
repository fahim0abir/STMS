import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { UsersService } from './users.service';
import { API_BASE_URL } from '../api.config';
import { Role } from '../../shared/models/enums';
import { User } from '../../shared/models/user.model';

describe('UsersService', () => {
  let service: UsersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UsersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests assignable users with a role filter and normalizes them', () => {
    let result: User[] = [];

    service.getAssignableUsers(Role.TeamMember).subscribe((users) => (result = users));

    const req = httpMock.expectOne((r) => r.url === `${API_BASE_URL}/users`);
    expect(req.request.params.get('role')).toBe(String(Role.TeamMember));
    req.flush({
      success: true,
      data: [{ id: 'u1', email: 'dev@example.com', name: 'Dev', role: 2 }],
      message: '',
      statusCode: 200,
    });

    expect(result).toHaveLength(1);
    expect(result[0].role).toBe('TeamMember');
  });

  it('creates a user with the requested role', () => {
    let created: User | null = null;

    service
      .createUser({ email: 'pm@example.com', password: 'Passw0rd!', name: 'PM', role: Role.ProjectManager })
      .subscribe((user) => (created = user));

    const req = httpMock.expectOne(`${API_BASE_URL}/users`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.role).toBe(Role.ProjectManager);
    req.flush({
      success: true,
      data: { id: 'u2', email: 'pm@example.com', name: 'PM', role: 1 },
      message: 'User created.',
      statusCode: 200,
    });

    expect(created!.role).toBe('ProjectManager');
  });

  it('updates the current profile through /users/me', () => {
    let updated: User | null = null;

    service.updateProfile({ email: 'me@example.com', name: 'New Name' }).subscribe((user) => (updated = user));

    const req = httpMock.expectOne(`${API_BASE_URL}/users/me`);
    expect(req.request.method).toBe('PUT');
    req.flush({
      success: true,
      data: { id: 'u1', email: 'me@example.com', name: 'New Name', role: 2 },
      message: 'Profile updated.',
      statusCode: 200,
    });

    expect(updated!.name).toBe('New Name');
  });
});