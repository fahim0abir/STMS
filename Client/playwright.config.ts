import { defineConfig, devices } from '@playwright/test';

const API_URL = 'http://localhost:5195';
const APP_URL = 'http://localhost:4200';

export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: !!process.env.CI,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    baseURL: APP_URL,
    headless: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [
    {
      name: 'chrome',
      use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project ../Server/SmartTask.API/SmartTask.API.csproj --no-launch-profile --urls http://localhost:5195',
      url: `${API_URL}/health`,
      cwd: __dirname,
      timeout: 240_000,
      reuseExistingServer: !process.env.CI,
      stdout: 'ignore',
      stderr: 'pipe',
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'E2E',
        DOTNET_ENVIRONMENT: 'E2E',
      },
    },
    {
      command: 'npm start -- --port 4200',
      url: APP_URL,
      timeout: 240_000,
      reuseExistingServer: !process.env.CI,
      stdout: 'ignore',
      stderr: 'pipe',
    },
  ],
});
