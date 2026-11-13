import { APIRequestContext, Page, expect } from '@playwright/test';

export const API_BASE_URL = 'http://localhost:5195/api';

export const users = {
  admin: { email: 'admin@smarttask.dev', password: 'Admin123!' },
  manager: { email: 'projectmanager@smarttask.dev', password: 'Pm123456!' },
  member: { email: 'teammember@smarttask.dev', password: 'Tm123456!' },
};

export interface Credentials {
  email: string;
  password: string;
}

export async function login(page: Page, user: Credentials = users.admin): Promise<void> {
  await page.goto('/login');
  await page.getByPlaceholder('Email', { exact: true }).fill(user.email);
  await page.getByPlaceholder('Password', { exact: true }).fill(user.password);
  await page.getByRole('button', { name: 'Login', exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

export async function apiLogin(request: APIRequestContext, user: Credentials = users.admin): Promise<string> {
  const response = await request.post(`${API_BASE_URL}/auth/login`, { data: user });
  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return body.data.accessToken as string;
}

export async function createProject(
  request: APIRequestContext,
  token: string,
  name: string,
  description = 'Created by the Playwright suite',
): Promise<{ id: string; name: string }> {
  const response = await request.post(`${API_BASE_URL}/projects`, {
    data: { name, description },
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return body.data;
}

export async function createTask(
  request: APIRequestContext,
  token: string,
  projectId: string,
  task: { title: string; status?: number; priority?: number; description?: string },
): Promise<{ id: string; title: string }> {
  const response = await request.post(`${API_BASE_URL}/taskitems?projectId=${projectId}`, {
    data: {
      title: task.title,
      description: task.description ?? 'Created by the Playwright suite',
      status: task.status ?? 0,
      priority: task.priority ?? 1,
      assigneeId: null,
      dueAt: new Date(Date.now() + 86_400_000).toISOString(),
    },
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return body.data;
}

export function uniqueName(prefix: string): string {
  return `${prefix} ${Date.now()}-${Math.floor(Math.random() * 1000)}`;
}
