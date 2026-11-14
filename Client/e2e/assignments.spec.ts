import { expect, test } from '@playwright/test';
import { API_BASE_URL, apiLogin, createProject, createTask, login, uniqueName, users } from './support/api';

test.describe('Assignments', () => {
  test('admin can assign a project manager from the project modal', async ({ page }) => {
    const name = uniqueName('Assignable Project');

    await login(page);
    await page.getByRole('link', { name: 'Projects' }).click();
    await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible();

    await page.getByRole('button', { name: 'New Project' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.locator('input[type="text"]').fill(name);

    const managerSelect = dialog.locator('select[formcontrolname="managerId"]');
    await expect(managerSelect).toBeVisible();
    await expect(managerSelect.locator('option')).not.toHaveCount(1);
    await managerSelect.selectOption({ index: 1 });
    await dialog.getByRole('button', { name: 'Create Project' }).click();
    await expect(dialog).toBeHidden();

    await page.getByPlaceholder(/Search projects by name/).fill(name);
    const row = page.getByRole('row', { name: new RegExp(name) });
    await expect(row).toBeVisible();
    await expect(row).not.toContainText('Unassigned');
  });

  test('project manager can assign a task to a team member', async ({ page }) => {
    const projectName = uniqueName('Manager Owned Project');
    const taskTitle = uniqueName('Assignable Task');

    await login(page, users.manager);
    await page.getByRole('link', { name: 'Projects' }).click();

    await page.getByRole('button', { name: 'New Project' }).click();
    const projectDialog = page.getByRole('dialog');
    await projectDialog.locator('input[type="text"]').fill(projectName);
    await projectDialog.getByRole('button', { name: 'Create Project' }).click();
    await expect(projectDialog).toBeHidden();

    await page.getByRole('link', { name: 'Tasks' }).click();
    await page.getByRole('button', { name: 'New Task' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.locator('select[formcontrolname="projectId"]').selectOption({ label: projectName });
    await dialog.locator('input[formcontrolname="title"]').fill(taskTitle);

    const assigneeSelect = dialog.locator('select[formcontrolname="assigneeId"]');
    await expect(assigneeSelect.locator('option')).not.toHaveCount(1);
    await assigneeSelect.selectOption({ index: 1 });

    await dialog.getByRole('button', { name: 'Create Task' }).click();
    await expect(dialog).toBeHidden();

    const row = page.getByRole('row', { name: new RegExp(taskTitle) });
    await expect(row).toBeVisible();
    await expect(row).not.toContainText('Unassigned');
  });

  test('team member can change the status of an assigned task', async ({ page, request }) => {
    const token = await apiLogin(request);
    const project = await createProject(request, token, uniqueName('Member Project'));
    const task = await createTask(request, token, project.id, { title: uniqueName('Member task') });

    const membersResponse = await request.get(`${API_BASE_URL}/users?role=2`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(membersResponse.ok()).toBeTruthy();
    const membersBody = await membersResponse.json();
    const memberId = membersBody.data[0].id as string;

    const assignResponse = await request.patch(`${API_BASE_URL}/taskitems/${task.id}/assignee`, {
      data: { assigneeId: memberId },
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(assignResponse.ok()).toBeTruthy();

    await login(page, users.member);
    await page.getByRole('link', { name: 'Tasks' }).click();
    await expect(page.getByRole('heading', { name: 'Tasks', exact: true })).toBeVisible();

    const row = page.getByRole('row', { name: new RegExp(task.title) });
    const status = row.locator('select.table__status');
    await expect(status).toBeVisible();
    await status.selectOption({ label: 'In Progress' });
    await expect(status).toHaveValue('1');
  });
});