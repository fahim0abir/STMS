import { expect, test } from '@playwright/test';
import { login, uniqueName, users } from './support/api';

test.describe('Project management', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.getByRole('link', { name: 'Projects' }).click();
    await expect(page).toHaveURL(/\/projects$/);
    await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible();
  });

  test('admin can create, search, edit and delete a project', async ({ page }) => {
    const name = uniqueName('E2E Project');

    await page.getByRole('button', { name: 'New Project' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await dialog.locator('input[type="text"]').fill(name);
    await dialog.locator('textarea').fill('Created by Playwright');
    await dialog.getByRole('button', { name: 'Create Project' }).click();

    await expect(dialog).toBeHidden();
    const row = page.getByRole('row', { name: new RegExp(name) });
    await expect(row).toBeVisible();

    await page.getByPlaceholder(/Search projects by name/).fill(name);
    await expect(page.getByRole('row', { name: new RegExp(name) })).toBeVisible();

    const updated = `${name} updated`;
    await page.getByRole('row', { name: new RegExp(name) }).getByRole('button', { name: 'Edit' }).click();
    const editDialog = page.getByRole('dialog');
    await editDialog.locator('input[type="text"]').fill(updated);
    await editDialog.getByRole('button', { name: 'Save Changes' }).click();
    await expect(editDialog).toBeHidden();

    await page.getByPlaceholder(/Search projects by name/).fill(updated);
    await expect(page.getByRole('row', { name: new RegExp(updated) })).toBeVisible();

    page.once('dialog', (confirmDialog) => confirmDialog.accept());
    await page.getByRole('row', { name: new RegExp(updated) }).getByRole('button', { name: 'Delete' }).click();
    await expect(page.getByRole('row', { name: new RegExp(updated) })).toHaveCount(0);
  });

  test('project manager can create a project', async ({ page }) => {
    await login(page, users.manager);
    await page.getByRole('link', { name: 'Projects' }).click();
    await expect(page).toHaveURL(/\/projects$/);

    const name = uniqueName('Manager Project');

    await page.getByRole('button', { name: 'New Project' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.locator('input[type="text"]').fill(name);
    await dialog.getByRole('button', { name: 'Create Project' }).click();

    await page.getByPlaceholder(/Search projects by name/).fill(name);
    await expect(page.getByRole('row', { name: new RegExp(name) })).toBeVisible();
  });
});
