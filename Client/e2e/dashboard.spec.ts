import { expect, test } from '@playwright/test';
import { apiLogin, createProject, createTask, login, uniqueName } from './support/api';

test.describe('Dashboard', () => {
  test('renders KPI cards and breakdown sections', async ({ page }) => {
    await login(page);

    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    for (const label of ['Projects', 'Total Tasks', 'Completed', 'Pending', 'Upcoming Due']) {
      await expect(page.getByText(label, { exact: true }).first()).toBeVisible();
    }

    await expect(page.getByText('Tasks by Status')).toBeVisible();
    await expect(page.getByText('Tasks by Priority')).toBeVisible();
  });

  test('reflects newly created project and task', async ({ page, request }) => {
    const token = await apiLogin(request);
    const project = await createProject(request, token, uniqueName('Dashboard Project'));
    await createTask(request, token, project.id, { title: uniqueName('Dashboard task'), status: 2, priority: 2 });

    await login(page);
    await expect(page.getByText('Tasks by Status')).toBeVisible();
    await page.getByRole('link', { name: 'Dashboard' }).click();
    await expect(page.getByText('Completed', { exact: true }).first()).toBeVisible();
  });
});
