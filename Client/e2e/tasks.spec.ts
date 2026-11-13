import { expect, test } from '@playwright/test';
import { apiLogin, createProject, login, uniqueName } from './support/api';

test.describe('Task management', () => {
  let projectName: string;

  test.beforeEach(async ({ page, request }) => {
    const token = await apiLogin(request);
    const project = await createProject(request, token, uniqueName('Task Project'));
    projectName = project.name;

    await login(page);
    await page.getByRole('link', { name: 'Tasks' }).click();
    await expect(page).toHaveURL(/\/tasks$/);
    await expect(page.getByRole('heading', { name: 'Tasks' })).toBeVisible();
  });

  async function createTaskThroughUi(page: import('@playwright/test').Page, title: string, priority = 'High') {
    await page.getByRole('button', { name: 'New Task' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await dialog.locator('select[formcontrolname="projectId"]').selectOption({ label: projectName });
    await dialog.locator('input[formcontrolname="title"]').fill(title);
    await dialog.locator('select[formcontrolname="priority"]').selectOption({ label: priority });
    await dialog.getByRole('button', { name: 'Create Task' }).click();
    await expect(dialog).toBeHidden();
    await expect(page.getByRole('cell', { name: title })).toBeVisible();
  }

  test('admin can create a task and change its status', async ({ page }) => {
    const title = uniqueName('E2E task');
    await createTaskThroughUi(page, title);

    const row = page.getByRole('row', { name: new RegExp(title) });
    await expect(row.getByText('To Do')).toBeVisible();

    await row.getByRole('button', { name: 'Edit' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.locator('select[formcontrolname="status"]').selectOption({ label: 'In Progress' });
    await dialog.getByRole('button', { name: 'Save Changes' }).click();
    await expect(dialog).toBeHidden();

    await expect(page.getByRole('row', { name: new RegExp(title) }).getByText('In Progress')).toBeVisible();
  });

  test('filters hide tasks that do not match the selected status', async ({ page }) => {
    const title = uniqueName('Filterable task');
    await createTaskThroughUi(page, title);

    await page.getByRole('button', { name: 'Completed', exact: true }).click();
    await expect(page.getByRole('cell', { name: title })).toHaveCount(0);

    await page.getByRole('button', { name: 'To Do', exact: true }).click();
    await expect(page.getByRole('cell', { name: title })).toBeVisible();
  });

  test('AI enhancement produces a suggestion that can be accepted', async ({ page }) => {
    await page.getByRole('button', { name: 'New Task' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.locator('select[formcontrolname="projectId"]').selectOption({ label: projectName });
    await dialog.locator('input[formcontrolname="title"]').fill(uniqueName('AI task'));
    await dialog.locator('textarea').fill('fix login page failing when password has spaces');

    await dialog.getByRole('button', { name: 'Improve with AI' }).click();
    await expect(dialog.locator('.modal__ai-message')).toContainText(/fallback|enhanced/i);
    await expect(dialog.locator('.modal__suggestion')).toBeVisible();
    await expect(dialog.getByRole('button', { name: 'Accept' })).toBeVisible();

    await dialog.getByRole('button', { name: 'Accept' }).click();
    await expect(dialog.locator('textarea')).not.toHaveValue('fix login page failing when password has spaces');

    await dialog.getByRole('button', { name: 'Cancel' }).click();
    await expect(dialog).toBeHidden();
  });
});
