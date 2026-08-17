import { expect, test } from '@playwright/test';

const apiUrl = 'http://127.0.0.1:5000';

test.beforeEach(async ({ request }) => {
  const response = await request.get(`${apiUrl}/health/db`);
  expect(response.ok(), 'Start the API and its PostgreSQL database before running e2e tests.').toBeTruthy();
});

test('navigates, creates and deletes a folder, then searches across the filesystem', async ({ page }) => {
  const folderName = `e2e-${Date.now()}`;

  await page.goto('/');
  await expect(page.getByLabel('breadcrumb')).toHaveText('home');

  await page.getByRole('button', { name: /documents/i }).dblclick();
  await expect(page.getByLabel('breadcrumb')).toHaveText('home/documents');
  await page.getByRole('button', { name: 'Parent' }).click();
  await expect(page.getByLabel('breadcrumb')).toHaveText('home');

  await page.getByRole('button', { name: 'New' }).click();
  const createDialog = page.getByRole('dialog');
  await createDialog.getByLabel('Entry name').fill(folderName);
  await createDialog.getByRole('button', { name: 'Create' }).click();
  await expect(createDialog).toBeHidden();

  const createdFolder = page.getByRole('button', { name: new RegExp(folderName, 'i') });
  await expect(createdFolder).toBeVisible();
  await createdFolder.click();
  await page.getByRole('button', { name: 'Delete' }).click();

  const deleteDialog = page.getByRole('dialog');
  await expect(deleteDialog).toContainText('Everything inside this folder will also be deleted.');
  await deleteDialog.getByRole('button', { name: 'Yes' }).click();
  await expect(deleteDialog).toBeHidden();
  await expect(createdFolder).toBeHidden();

  await page.getByLabel('Search scope').selectOption('all');
  await page.getByLabel('Search files').fill('f24');
  const result = page.getByRole('button', { name: /f24-notes\.md/i });
  await expect(result).toBeVisible();
  await expect(result).toContainText('home/documents/projects/f24-notes.md');
  await result.click();
  await expect(page.getByLabel('breadcrumb')).toHaveText('home/documents/projects');
});
