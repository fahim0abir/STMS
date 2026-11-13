import { expect, test } from '@playwright/test';
import { apiLogin, createProject, createTask, login, uniqueName, users } from './support/api';

test.describe('Role-based access control', () => {
  test('team member does not see the projects navigation entry', async ({ page }) => {
    await login(page, users.member);
    await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Tasks' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Projects' })).toBeHidden();
  });

  test('team member is blocked from the projects route', async ({ page }) => {
    await login(page, users.member);
    await page.goto('/projects');
    await expect(page).toHaveURL(/\/(login|unauthorized)$/);
    await expect(page.getByRole('heading', { name: 'Projects' })).toBeHidden();
  });

  test('project manager sees the projects navigation entry', async ({ page }) => {
    await login(page, users.manager);
    await expect(page.getByRole('link', { name: 'Projects' })).toBeVisible();
  });

  test('team member only sees tasks assigned to them', async ({ page, request }) => {
    const token = await apiLogin(request);
    const project = await createProject(request, token, uniqueName('RBAC Project'));
    const unassigned = await createTask(request, token, project.id, { title: uniqueName('Unassigned task') });

    await login(page, users.member);
    await page.getByRole('link', { name: 'Tasks' }).click();
    await expect(page).toHaveURL(/\/tasks$/);
    await expect(page.getByRole('cell', { name: unassigned.title })).toHaveCount(0);
  });
});
