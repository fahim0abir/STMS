import { Role, toRoleName } from './enums';

export interface User {
  id: string;
  email: string;
  name?: string | null;
  role: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  name?: string | null;
  role: Role;
}

export interface UpdateProfileRequest {
  email: string;
  name?: string | null;
  currentPassword?: string | null;
  newPassword?: string | null;
  confirmPassword?: string | null;
}

export interface UpdateUserRoleRequest {
  role: Role;
}

export interface AuthResult {
  user: User;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
}

export function normalizeUser(raw: Partial<User> & { role?: number | string | null } | null | undefined): User | null {
  if (!raw) return null;
  return {
    id: raw.id ?? '',
    email: raw.email ?? '',
    name: raw.name ?? null,
    role: toRoleName(raw.role),
  };
}
