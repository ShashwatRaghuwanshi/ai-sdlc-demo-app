import { type Locator, type Page } from '@playwright/test';

export class InvoiceApprovalPage {
  readonly page: Page;
  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly signInButton: Locator;
  readonly approvalTab: Locator;
  readonly decisionError: Locator;

  constructor(page: Page) {
    this.page = page;
    this.usernameInput = page.locator('#username');
    this.passwordInput = page.locator('#password');
    this.signInButton = page.getByRole('button', { name: 'Sign in' });
    this.approvalTab = page.getByRole('tab', { name: 'Approvals' });
    this.decisionError = page.locator('#decision-error');
  }

  async goto() {
    await this.page.goto('/');
  }

  async login(username: string, password: string) {
    await this.usernameInput.fill(username);
    await this.passwordInput.fill(password);
    await this.signInButton.click();
    await this.page.getByText(username).waitFor();
  }

  async createInvoice(customerName: string, amount: number) {
    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount').fill(String(amount));
    await this.page.getByRole('button', { name: 'Create invoice' }).click();

    const row = this.page.locator('tbody tr').filter({ hasText: customerName }).first();
    await row.waitFor();

    const id = (await row.locator('td').first().textContent())?.trim();
    if (!id) {
      throw new Error(`Invoice for ${customerName} was not created.`);
    }

    return { id };
  }

  async submitForApproval(invoiceId: string) {
    await this.page.getByRole('button', { name: `Submit invoice ${invoiceId} for approval` }).click();
  }

  async openPendingApprovals() {
    await this.approvalTab.click();
  }

  async approveInvoice(invoiceId: string) {
    await this.page.getByRole('button', { name: `Approve invoice ${invoiceId}` }).click();
  }

  async getInvoiceState(invoiceId: string) {
    const session = await this.page.evaluate(() => JSON.parse(sessionStorage.getItem('session') || 'null'));
    if (!session?.token) {
      throw new Error('No active session token available for invoice state check.');
    }

    return this.page.evaluate(async ({ invoiceId, token }) => {
      const response = await fetch(`/api/invoices/${invoiceId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!response.ok) {
        throw new Error(`Invoice lookup failed with status ${response.status}`);
      }

      return response.json();
    }, { invoiceId, token: session.token });
  }
}
