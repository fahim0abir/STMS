import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { TokenService } from './token.service';
import { User } from '../../shared/models/user.model';

function createJwt(expiresInSeconds: number): string {
  const payload = { sub: 'user-1', exp: Math.floor(Date.now() / 1000) + expiresInSeconds };
  const encode = (value: object) => btoa(JSON.stringify(value));
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.signature`;
}

const user: User = { id: 'user-1', email: 'user@example.com', name: 'Jane', role: 'Admin' };

describe('TokenService', () => {
  let service: TokenService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenService);
  });

  it('returns null when nothing has been stored', () => {
    expect(service.getToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
    expect(service.getUser()).toBeNull();
    expect(service.getExpiration()).toBeNull();
  });

  it('persists the full session', () => {
    service.setSession('access', 'refresh', user);

    expect(service.getToken()).toBe('access');
    expect(service.getRefreshToken()).toBe('refresh');
    expect(service.getUser()).toEqual(user);
  });

  it('parses the JWT expiration and reports an expiring token as expired', () => {
    service.setToken(createJwt(-60));

    expect(service.getExpiration()).not.toBeNull();
    expect(service.isExpired()).toBe(true);
  });

  it('reports a future token as not expired', () => {
    service.setToken(createJwt(3600));

    expect(service.isExpired()).toBe(false);
  });

  it('treats malformed tokens as expired', () => {
    service.setToken('not-a-jwt');

    expect(service.getExpiration()).toBeNull();
    expect(service.isExpired()).toBe(true);
  });

  it('clears every stored key on removeToken', () => {
    service.setSession('access', 'refresh', user);

    service.removeToken();

    expect(service.getToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
    expect(service.getUser()).toBeNull();
  });

  it('returns null when the stored user payload is corrupt', () => {
    localStorage.setItem('current_user', '{not-json');

    expect(service.getUser()).toBeNull();
  });
});
