import { expect, test } from '@playwright/test';
import { login, users, uniqueName } from './support/api';

test.describe('Authentication flows', () => {
  test('unauthenticated visitor is redirected to login', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  });

  test('admin can sign in and sees the dashboard shell', async ({ page }) => {
    await login(page);
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Projects' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Tasks' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Logout' })).toBeVisible();
  });

  test('invalid credentials show an error and stay on login', async ({ page }) => {
    await page.goto('/login');
    await page.getByPlaceholder('Email', { exact: true }).fill(users.admin.email);
    await page.getByPlaceholder('Password', { exact: true }).fill('WrongPassword!1');
    await page.getByRole('button', { name: 'Login', exact: true }).click();

    await expect(page.locator('.error')).toContainText('Invalid email or password');
    await expect(page).toHaveURL(/\/login$/);
  });

  test('new user can register and then sign in', async ({ page }) => {
    const email = `${uniqueName('e2e.user').replace(/\s+/g, '.').toLowerCase()}@smarttask.dev`;
    const password = 'Passw0rd!23';

    await page.goto('/register');
    await page.getByPlaceholder('Full name').fill('E2E UI User');
    await page.getByPlaceholder('Email', { exact: true }).fill(email);
    await page.getByPlaceholder('Password', { exact: true }).fill(password);
    await page.getByPlaceholder('Confirm password').fill(password);
    await page.getByRole('button', { name: 'Register', exact: true }).click();

    await expect(page).toHaveURL(/\/login$/);
    await login(page, { email, password });
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  });

  test('logout returns the user to the login page', async ({ page }) => {
    await login(page);
    await page.getByRole('button', { name: 'Logout' }).click();
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  });
});
