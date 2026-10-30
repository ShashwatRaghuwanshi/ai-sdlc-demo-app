import { expect, test } from '@playwright/test';
import { InvoiceApprovalPage } from '../pages/InvoiceApprovalPage';

test('manager final approval for an over-limit invoice is rejected without mutation', async ({ page }) => {
  const app = new InvoiceApprovalPage(page);
  const customerName = `Reject Manager ${Date.now()}`;
  const amount = 75000;

  await app.goto();
  await app.login('manager.demo', 'ChangeMe123!');

  const { id } = await app.createInvoice(customerName, amount);
  await app.submitForApproval(id);
  await app.openPendingApprovals();
  await app.approveInvoice(id);

  await expect(app.decisionError).toBeVisible();
  await expect(app.decisionError).toContainText('Manager final approval is only allowed for invoices up to 50,000');

  const invoice = await app.getInvoiceState(id);
  expect(invoice.status).toBe('PendingApproval');
  expect(invoice.decidedBy).toBeNull();
  expect(invoice.decidedAt).toBeNull();
  expect(invoice.decisionNote).toBeNull();
  expect(invoice.managerApproval).toBeNull();
  expect(invoice.directorApproval).toBeNull();
});
