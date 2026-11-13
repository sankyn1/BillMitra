import { Component, HostListener, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SetupService, SelectiveResetRequest } from '../../core/services/setup.service';
import { DialogService } from '../../shared/services/dialog.service';

@Component({
  selector: 'app-secret-reset-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (isOpen()) {
      <div class="secret-modal-backdrop" (click)="onBackdropClick($event)">
        <div class="secret-modal-card" (click)="$event.stopPropagation()">
          <div class="secret-modal-header">
            <div class="header-title">
              <i class="pi pi-shield text-danger"></i>
              <div>
                <h3>Developer & Admin Secret Data Reset Console</h3>
                <span class="sub-title">Triggered via secret shortcut <code>Ctrl + Shift + Alt + D</code> or 5x brand click</span>
              </div>
            </div>
            <button class="close-btn" (click)="close()">&times;</button>
          </div>

          <div class="secret-modal-body">
            <div class="notice-box">
              <i class="pi pi-exclamation-triangle"></i>
              <div>
                <strong>Restricted Area:</strong> Select exactly which modules you want to wipe clean. All checked items will be permanently erased. Document numbering sequences will reset back to 0 (so your next invoice starts with #1).
              </div>
            </div>

            <div class="form-section">
              <label class="section-label">1. Enter Secret Reset Password</label>
              <div class="password-input-wrap">
                <i class="pi pi-lock"></i>
                <input
                  type="password"
                  [(ngModel)]="password"
                  placeholder="Enter secret reset password or admin password"
                  class="secret-input"
                  autofocus
                />
              </div>
              <span class="hint-text">Master Default Secret: <code>BillMitraReset@2026</code> (or your Admin password)</span>
            </div>

            <div class="form-section">
              <div class="section-header-flex">
                <label class="section-label">2. Select Modules to Reset</label>
                <div class="quick-toggle-actions">
                  <button type="button" class="btn-link" (click)="selectAll(true)">Select All</button>
                  <span class="sep">|</span>
                  <button type="button" class="btn-link" (click)="selectTransactionsOnly()">Transactions Only</button>
                  <span class="sep">|</span>
                  <button type="button" class="btn-link" (click)="selectAll(false)">Clear All</button>
                </div>
              </div>

              <div class="reset-checklist">
                <label class="check-item highlight">
                  <input type="checkbox" [(ngModel)]="options.resetDocumentSequences" />
                  <div class="check-content">
                    <span class="item-title">Reset Document Sequence Counters to 0</span>
                    <span class="item-desc">Resets invoice, quotation, payment, and voucher sequences so new ones start from #1</span>
                  </div>
                </label>

                <label class="check-item">
                  <input type="checkbox" [(ngModel)]="options.resetSalesInvoices" />
                  <div class="check-content">
                    <span class="item-title">Sales Invoices & Line Items</span>
                    <span class="item-desc">All tax invoices, credit/debit notes, and items</span>
                  </div>
                </label>

                <label class="check-item">
                  <input type="checkbox" [(ngModel)]="options.resetPurchases" />
                  <div class="check-content">
                    <span class="item-title">Purchase Bills & Goods Receipt</span>
                    <span class="item-desc">Inward vendor bills and purchase item entries</span>
                  </div>
                </label>

                <label class="check-item">
                  <input type="checkbox" [(ngModel)]="options.resetPaymentsAndLedgers" />
                  <div class="check-content">
                    <span class="item-title">Payments & Party Ledger Accounts</span>
                    <span class="item-desc">Receipts, payments, bill allocations, and ledger postings</span>
                  </div>
                </label>

                <label class="check-item">
                  <input type="checkbox" [(ngModel)]="options.resetExpenses" />
                  <div class="check-content">
                    <span class="item-title">Direct & Indirect Expenses</span>
                    <span class="item-desc">Expense vouchers, petty cash, and payment records</span>
                  </div>
                </label>

                <label class="check-item">
                  <input type="checkbox" [(ngModel)]="options.resetEWayAndEInvoices" />
                  <div class="check-content">
                    <span class="item-title">E-Way Bills & E-Invoices (IRN)</span>
                    <span class="item-desc">Generated permits, Part-A/Part-B data, and QR codes</span>
                  </div>
                </label>

                <label class="check-item">
                  <input type="checkbox" [(ngModel)]="options.resetStockAndBatches" />
                  <div class="check-content">
                    <span class="item-title">Inventory Stock Movements & Batches</span>
                    <span class="item-desc">Clears stock ledger, lot numbers, and current warehouse levels</span>
                  </div>
                </label>

                <label class="check-item">
                  <input type="checkbox" [(ngModel)]="options.resetGstFilings" />
                  <div class="check-content">
                    <span class="item-title">GST Return Filing Records</span>
                    <span class="item-desc">Clears local GSTR-1, GSTR-3B snapshots and filing logs</span>
                  </div>
                </label>

                <label class="check-item danger-item">
                  <input type="checkbox" [(ngModel)]="options.resetParties" />
                  <div class="check-content">
                    <span class="item-title">Parties Master (Customers & Suppliers)</span>
                    <span class="item-desc">Permanently erases all saved customer/supplier profiles</span>
                  </div>
                </label>

                <label class="check-item danger-item">
                  <input type="checkbox" [(ngModel)]="options.resetProducts" />
                  <div class="check-content">
                    <span class="item-title">Products Master (Items & SKUs)</span>
                    <span class="item-desc">Permanently erases all inventory items, rates, and HSN codes</span>
                  </div>
                </label>
              </div>
            </div>
          </div>

          <div class="secret-modal-footer">
            <button type="button" class="btn btn-secondary" (click)="close()" [disabled]="isLoading()">
              Cancel
            </button>
            <button
              type="button"
              class="btn btn-danger"
              (click)="executeReset()"
              [disabled]="isLoading() || !password || countSelected() === 0"
            >
              <i class="pi pi-trash" [class.pi-spin]="isLoading()"></i>
              <span>{{ isLoading() ? 'Wiping Clean...' : 'Execute Secret Reset (' + countSelected() + ' selected)' }}</span>
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .secret-modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.75);
      backdrop-filter: blur(6px);
      z-index: 99999;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 16px;
      animation: fadeIn 0.2s ease-out;
    }

    .secret-modal-card {
      background: #ffffff;
      border-radius: 16px;
      width: 680px;
      max-width: 95vw;
      max-height: 90vh;
      display: flex;
      flex-direction: column;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.35);
      border: 1px solid #fee2e2;
      overflow: hidden;
    }

    .secret-modal-header {
      padding: 18px 24px;
      background: #fef2f2;
      border-bottom: 1px solid #fee2e2;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .header-title {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .header-title i {
      font-size: 1.8rem;
    }

    .header-title h3 {
      margin: 0;
      font-size: 1.15rem;
      font-weight: 700;
      color: #991b1b;
    }

    .sub-title {
      font-size: 0.75rem;
      color: #7f1d1d;
    }

    .sub-title code {
      background: #fca5a5;
      color: #450a0a;
      padding: 2px 6px;
      border-radius: 4px;
      font-weight: 600;
    }

    .close-btn {
      background: transparent;
      border: none;
      font-size: 1.6rem;
      color: #991b1b;
      cursor: pointer;
      line-height: 1;
    }

    .secret-modal-body {
      padding: 24px;
      overflow-y: auto;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .notice-box {
      background: #fff1f2;
      border: 1px solid #fecdd3;
      border-radius: 10px;
      padding: 12px 16px;
      display: flex;
      gap: 12px;
      align-items: flex-start;
      font-size: 0.85rem;
      color: #881337;
    }

    .notice-box i {
      font-size: 1.25rem;
      color: #e11d48;
      margin-top: 2px;
    }

    .form-section {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .section-label {
      font-size: 0.85rem;
      font-weight: 700;
      color: #1e293b;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }

    .section-header-flex {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .quick-toggle-actions {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .btn-link {
      background: transparent;
      border: none;
      color: #2563eb;
      font-size: 0.78rem;
      font-weight: 600;
      cursor: pointer;
      padding: 0;
    }

    .btn-link:hover {
      text-decoration: underline;
    }

    .sep {
      color: #cbd5e1;
      font-size: 0.75rem;
    }

    .password-input-wrap {
      position: relative;
      display: flex;
      align-items: center;
    }

    .password-input-wrap i {
      position: absolute;
      left: 14px;
      color: #64748b;
    }

    .secret-input {
      width: 100%;
      padding: 10px 14px 10px 40px;
      border: 2px solid #cbd5e1;
      border-radius: 10px;
      font-size: 0.95rem;
      outline: none;
      transition: all 0.2s;
    }

    .secret-input:focus {
      border-color: #dc2626;
      box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.15);
    }

    .hint-text {
      font-size: 0.75rem;
      color: #64748b;
    }

    .hint-text code {
      background: #f1f5f9;
      padding: 2px 6px;
      border-radius: 4px;
      font-weight: 600;
      color: #0f172a;
    }

    .reset-checklist {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
      max-height: 320px;
      overflow-y: auto;
      padding-right: 4px;
    }

    .check-item {
      display: flex;
      gap: 10px;
      align-items: flex-start;
      padding: 10px 12px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.15s;
    }

    .check-item:hover {
      background: #f1f5f9;
      border-color: #cbd5e1;
    }

    .check-item.highlight {
      background: #eff6ff;
      border-color: #bfdbfe;
      grid-column: span 2;
    }

    .check-item.danger-item {
      border-left: 3px solid #f87171;
    }

    .check-item input[type="checkbox"] {
      margin-top: 3px;
      width: 16px;
      height: 16px;
      accent-color: #dc2626;
      cursor: pointer;
    }

    .check-content {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .item-title {
      font-size: 0.82rem;
      font-weight: 700;
      color: #1e293b;
    }

    .item-desc {
      font-size: 0.72rem;
      color: #64748b;
      line-height: 1.25;
    }

    .secret-modal-footer {
      padding: 16px 24px;
      background: #f8fafc;
      border-top: 1px solid #e2e8f0;
      display: flex;
      justify-content: flex-end;
      gap: 12px;
    }

    .btn {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 9px 20px;
      border-radius: 8px;
      font-weight: 600;
      font-size: 0.88rem;
      cursor: pointer;
      border: 1px solid transparent;
      transition: all 0.15s;
    }

    .btn-secondary {
      background: #ffffff;
      border-color: #cbd5e1;
      color: #334155;
    }

    .btn-secondary:hover {
      background: #f1f5f9;
    }

    .btn-danger {
      background: #dc2626;
      color: #ffffff;
    }

    .btn-danger:hover:not(:disabled) {
      background: #b91c1c;
    }

    .btn:disabled {
      opacity: 0.55;
      cursor: not-allowed;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
  `]
})
export class SecretResetModalComponent {
  private readonly setupService = inject(SetupService);
  private readonly dialogService = inject(DialogService);

  isOpen = signal<boolean>(false);
  isLoading = signal<boolean>(false);
  password = '';

  options: SelectiveResetRequest = {
    password: '',
    resetSalesInvoices: true,
    resetPurchases: true,
    resetExpenses: true,
    resetPaymentsAndLedgers: true,
    resetEWayAndEInvoices: true,
    resetStockAndBatches: true,
    resetGstFilings: true,
    resetDocumentSequences: true,
    resetParties: false,
    resetProducts: false
  };

  // Secret shortcut listener: Ctrl + Shift + Alt + D
  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {
    if (event.ctrlKey && event.shiftKey && event.altKey && (event.key === 'D' || event.key === 'd')) {
      event.preventDefault();
      this.open();
    }
  }

  open(): void {
    this.password = '';
    this.selectTransactionsOnly();
    this.isOpen.set(true);
  }

  close(): void {
    if (this.isLoading()) return;
    this.isOpen.set(false);
  }

  onBackdropClick(event: MouseEvent): void {
    this.close();
  }

  selectAll(val: boolean): void {
    this.options.resetSalesInvoices = val;
    this.options.resetPurchases = val;
    this.options.resetExpenses = val;
    this.options.resetPaymentsAndLedgers = val;
    this.options.resetEWayAndEInvoices = val;
    this.options.resetStockAndBatches = val;
    this.options.resetGstFilings = val;
    this.options.resetDocumentSequences = val;
    this.options.resetParties = val;
    this.options.resetProducts = val;
  }

  selectTransactionsOnly(): void {
    this.options.resetSalesInvoices = true;
    this.options.resetPurchases = true;
    this.options.resetExpenses = true;
    this.options.resetPaymentsAndLedgers = true;
    this.options.resetEWayAndEInvoices = true;
    this.options.resetStockAndBatches = true;
    this.options.resetGstFilings = true;
    this.options.resetDocumentSequences = true;
    this.options.resetParties = false;
    this.options.resetProducts = false;
  }

  countSelected(): number {
    return [
      this.options.resetSalesInvoices,
      this.options.resetPurchases,
      this.options.resetExpenses,
      this.options.resetPaymentsAndLedgers,
      this.options.resetEWayAndEInvoices,
      this.options.resetStockAndBatches,
      this.options.resetGstFilings,
      this.options.resetDocumentSequences,
      this.options.resetParties,
      this.options.resetProducts
    ].filter(Boolean).length;
  }

  executeReset(): void {
    if (!this.password.trim()) {
      this.dialogService.toast('Please enter the secret password.', 'warning');
      return;
    }

    this.options.password = this.password.trim();
    this.isLoading.set(true);

    this.setupService.resetSelectiveData(this.options).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.success) {
          this.dialogService.alert(
            'Reset Completed Successfully',
            'The selected data has been completely erased, and document sequences have been reset to 0. You can now test from invoice #1.',
            'success'
          );
          this.close();
          // Reload page to refresh all active grids and state
          setTimeout(() => window.location.reload(), 1200);
        } else {
          this.dialogService.alert('Reset Failed', res.message || 'Operation failed', 'danger');
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.status === 401 ? 'Invalid secret password! Access denied.' : (err?.error?.message || 'Failed to execute reset.');
        this.dialogService.alert('Reset Denied', msg, 'danger');
      }
    });
  }
}

