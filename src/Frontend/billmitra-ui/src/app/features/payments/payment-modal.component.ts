import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentService, CreatePaymentRequest, OpenInvoiceDto } from '../../core/services/payment.service';
import { MastersService, PartyDto } from '../../core/services/masters.service';
import { DialogService } from '../../shared/services/dialog.service';
import {
  BmButtonDirective,
  BmPanelDirective,
  BmPanelHeaderDirective,
  BmPanelBodyDirective,
  BmPanelFooterDirective,
  BmInputDirective,
  BmSelectDirective,
  BmTableDirective,
  BmTableHeaderCellDirective,
  BmTableRowDirective,
  BmTableCellDirective,
  BmBadgeDirective
} from '../../shared/directives';

@Component({
  selector: 'app-payment-modal',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BmButtonDirective,
    BmPanelDirective,
    BmPanelHeaderDirective,
    BmPanelBodyDirective,
    BmPanelFooterDirective,
    BmInputDirective,
    BmSelectDirective,
    BmTableDirective,
    BmTableHeaderCellDirective,
    BmTableRowDirective,
    BmTableCellDirective
  ],
  template: `
    <div class="bm-modal-backdrop" (click)="onBackdropClick($event)">
      <div class="bm-modal-dialog bm-modal-lg" role="dialog" aria-modal="true">
        <div bmPanel class="bm-payment-card">
          <div bmPanelHeader>
            <div class="bm-card-header-wrap">
              <div class="bm-header-title">
                <i class="pi" [ngClass]="paymentType === 1 ? 'pi-arrow-down-left text-success' : 'pi-arrow-up-right text-warning'"></i>
                <div>
                  <h3 class="bm-title-text">{{ paymentType === 1 ? 'Record Customer Receipt (Inward)' : 'Record Supplier Payment (Outward)' }}</h3>
                  <p class="bm-subtitle-text">Multi-Invoice Allocation, Advance Balances & Real-Time Statutory Traceability</p>
                </div>
              </div>
              <button class="bm-btn-close" (click)="closeModal()">
                <i class="pi pi-times"></i>
              </button>
            </div>
          </div>

          <div bmPanelBody class="bm-modal-content">
            <!-- Form Grid -->
            <div class="bm-form-grid">
              <!-- Party Selection -->
              <div class="bm-form-group col-span-2">
                <label class="bm-label">Select Party (Customer / Supplier) *</label>
                <select bmSelect class="bm-select-full" [(ngModel)]="selectedPartyId" (change)="onPartyChange()">
                  <option [ngValue]="''">-- Select Account --</option>
                  @for (party of parties(); track party.id) {
                    <option [value]="party.id">{{ party.name }} (GSTIN: {{ party.gstin || 'Unregistered' }})</option>
                  }
                </select>
              </div>

              <!-- Payment Type Switch -->
              <div class="bm-form-group">
                <label class="bm-label">Voucher Type *</label>
                <select bmSelect class="bm-select-full" [(ngModel)]="paymentType" (change)="onTypeChange()">
                  <option [ngValue]="1">Receipt (Money Received)</option>
                  <option [ngValue]="2">Payment (Money Paid)</option>
                </select>
              </div>

              <!-- Payment Mode -->
              <div class="bm-form-group">
                <label class="bm-label">Payment Mode *</label>
                <select bmSelect class="bm-select-full" [(ngModel)]="paymentMode">
                  <option [ngValue]="1">Cash</option>
                  <option [ngValue]="2">Bank Transfer (NEFT/RTGS/IMPS)</option>
                  <option [ngValue]="3">Cheque / Demand Draft</option>
                  <option [ngValue]="4">UPI (GooglePay / PhonePe / Paytm)</option>
                  <option [ngValue]="5">Credit Card</option>
                  <option [ngValue]="6">Debit Card</option>
                </select>
              </div>

              <!-- Payment Date -->
              <div class="bm-form-group">
                <label class="bm-label">Payment Date *</label>
                <input bmInput type="date" [(ngModel)]="paymentDate" />
              </div>

              <!-- Amount -->
              <div class="bm-form-group">
                <label class="bm-label">Total Amount (₹) *</label>
                <input
                  bmInput
                  type="number"
                  step="0.01"
                  min="0.01"
                  [(ngModel)]="amount"
                  (ngModelChange)="onAmountChange()"
                  placeholder="0.00"
                />
              </div>

              <!-- Reference Number -->
              <div class="bm-form-group">
                <label class="bm-label">Reference # (UTR / Cheque / UPI ID)</label>
                <input bmInput type="text" [(ngModel)]="referenceNumber" placeholder="e.g. UTR12345678" />
              </div>

              <!-- Bank Name -->
              <div class="bm-form-group">
                <label class="bm-label">Bank Name</label>
                <input bmInput type="text" [(ngModel)]="bankName" placeholder="e.g. State Bank of India" />
              </div>

              <!-- Notes -->
              <div class="bm-form-group col-span-2">
                <label class="bm-label">Narration / Notes</label>
                <input bmInput type="text" [(ngModel)]="notes" placeholder="Optional notes for this voucher" />
              </div>
            </div>

            <!-- Open Invoices Allocation Section -->
            @if (selectedPartyId) {
              <div class="bm-allocations-section mt-4">
                <div class="bm-section-title-wrap">
                  <div>
                    <h4 class="bm-section-title">Invoice Settle & Multi-Allocation</h4>
                    <p class="bm-section-subtitle">Allocate this payment across specific invoices or leave as Advance Credit</p>
                  </div>
                  @if (openInvoices().length > 0 && amount > 0) {
                    <button bmButton bmVariant="outline" bmSize="sm" (click)="autoAllocateFifo()">
                      <i class="pi pi-bolt"></i> Auto-Allocate (FIFO)
                    </button>
                  }
                </div>

                @if (loadingInvoices()) {
                  <div class="bm-loading-placeholder">
                    <i class="pi pi-spin pi-spinner mr-2"></i> Loading pending invoices...
                  </div>
                } @else if (openInvoices().length === 0) {
                  <div class="bm-empty-banner">
                    <i class="pi pi-check-circle text-success"></i>
                    <span>No open unpaid invoices found for this party. The full amount will be recorded as an <strong>Advance Balance</strong>.</span>
                  </div>
                } @else {
                  <div class="bm-table-responsive mt-2">
                    <table bmTable class="bm-alloc-table">
                      <thead>
                        <tr>
                          <th bmHeaderCell>Invoice #</th>
                          <th bmHeaderCell>Date</th>
                          <th bmHeaderCell>Due Date</th>
                          <th bmHeaderCell class="text-right">Grand Total</th>
                          <th bmHeaderCell class="text-right">Paid</th>
                          <th bmHeaderCell class="text-right">Balance Due</th>
                          <th bmHeaderCell class="text-right" style="width: 160px;">Allocate (₹)</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (inv of openInvoices(); track inv.salesDocumentId) {
                          <tr bmTableRow>
                            <td bmTableCell><strong>{{ inv.documentNumber }}</strong></td>
                            <td bmTableCell>{{ inv.documentDate | date: 'dd-MMM-yyyy' }}</td>
                            <td bmTableCell>{{ inv.dueDate ? (inv.dueDate | date: 'dd-MMM-yyyy') : '-' }}</td>
                            <td bmTableCell class="text-right">₹{{ inv.grandTotal | number: '1.2-2' }}</td>
                            <td bmTableCell class="text-right text-muted">₹{{ inv.paidAmount | number: '1.2-2' }}</td>
                            <td bmTableCell class="text-right text-danger font-semibold">₹{{ inv.outstandingBalance | number: '1.2-2' }}</td>
                            <td bmTableCell class="text-right">
                              <input
                                bmInput
                                type="number"
                                step="0.01"
                                min="0"
                                [max]="inv.outstandingBalance"
                                [(ngModel)]="allocationsMap[inv.salesDocumentId]"
                                (ngModelChange)="onAllocationChange()"
                                class="text-right"
                                placeholder="0.00"
                              />
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                }

                <!-- Allocation Summary Banner -->
                <div class="bm-allocation-summary mt-3">
                  <div class="bm-summary-metric">
                    <span class="bm-metric-label">Voucher Amount</span>
                    <span class="bm-metric-val">₹{{ amount | number: '1.2-2' }}</span>
                  </div>
                  <div class="bm-summary-metric">
                    <span class="bm-metric-label">Total Allocated</span>
                    <span class="bm-metric-val text-primary font-bold">₹{{ totalAllocated() | number: '1.2-2' }}</span>
                  </div>
                  <div class="bm-summary-metric">
                    <span class="bm-metric-label">Unallocated (Advance)</span>
                    <span class="bm-metric-val font-bold" [ngClass]="unallocatedAmount() >= 0 ? 'text-success' : 'text-danger'">
                      ₹{{ unallocatedAmount() | number: '1.2-2' }}
                    </span>
                  </div>
                </div>

                @if (unallocatedAmount() < 0) {
                  <div class="bm-alert-error mt-2">
                    <i class="pi pi-exclamation-triangle mr-1"></i>
                    <strong>Error:</strong> Total allocated amount cannot exceed the voucher total of ₹{{ amount | number: '1.2-2' }}.
                  </div>
                }
              </div>
            }
          </div>

          <div bmPanelFooter>
            <div class="bm-footer-actions">
              <button bmButton bmVariant="secondary" bmSize="md" (click)="closeModal()">
                Cancel
              </button>
              <button
                bmButton
                bmVariant="primary"
                bmSize="md"
                [disabled]="isSaving() || !isValid()"
                (click)="savePayment()"
              >
                @if (isSaving()) {
                  <i class="pi pi-spin pi-spinner mr-1"></i> Saving...
                } @else {
                  <i class="pi pi-check mr-1"></i> Confirm & Save Voucher
                }
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bm-modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100vw;
      height: 100vh;
      background: rgba(15, 23, 42, 0.6);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1.5rem;
    }

    .bm-modal-dialog {
      width: 100%;
      max-width: 900px;
      max-height: 90vh;
      display: flex;
      flex-direction: column;
    }

    .bm-payment-card {
      display: flex;
      flex-direction: column;
      max-height: 90vh;
      border: 1px solid var(--bm-border-color, #e2e8f0);
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      background: #ffffff;
      border-radius: 8px;
    }

    .bm-card-header-wrap {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
    }

    .bm-header-title {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .bm-title-text {
      margin: 0;
      font-size: 1.15rem;
      font-weight: 700;
      color: #0f172a;
    }

    .bm-subtitle-text {
      margin: 0;
      font-size: 0.8rem;
      color: #64748b;
    }

    .bm-btn-close {
      background: none;
      border: none;
      font-size: 1.1rem;
      color: #64748b;
      cursor: pointer;
      padding: 0.35rem;
      border-radius: 4px;
    }

    .bm-btn-close:hover {
      background: #f1f5f9;
      color: #0f172a;
    }

    .bm-modal-content {
      overflow-y: auto;
      padding: 1.25rem;
      max-height: calc(90vh - 140px);
    }

    .bm-form-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 1rem;
    }

    .col-span-2 {
      grid-column: span 2;
    }

    .bm-form-group {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }

    .bm-label {
      font-size: 0.8rem;
      font-weight: 600;
      color: #334155;
    }

    .bm-select-full {
      width: 100%;
    }

    .bm-allocations-section {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 1rem;
    }

    .bm-section-title-wrap {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.75rem;
    }

    .bm-section-title {
      margin: 0;
      font-size: 0.95rem;
      font-weight: 600;
      color: #0f172a;
    }

    .bm-section-subtitle {
      margin: 0;
      font-size: 0.75rem;
      color: #64748b;
    }

    .bm-empty-banner {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      background: #f0fdf4;
      border: 1px solid #bbf7d0;
      border-radius: 6px;
      font-size: 0.85rem;
      color: #166534;
    }

    .bm-allocation-summary {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1rem;
      background: #ffffff;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      padding: 0.75rem 1rem;
    }

    .bm-summary-metric {
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
    }

    .bm-metric-label {
      font-size: 0.75rem;
      text-transform: uppercase;
      font-weight: 600;
      color: #64748b;
    }

    .bm-metric-val {
      font-size: 1.1rem;
    }

    .bm-alert-error {
      padding: 0.6rem 0.85rem;
      background: #fef2f2;
      border: 1px solid #fecaca;
      border-radius: 6px;
      color: #991b1b;
      font-size: 0.8rem;
    }

    .bm-footer-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      width: 100%;
    }
  `]
})
export class PaymentModalComponent implements OnInit {
  @Input() paymentType: number = 1; // 1 = Receipt, 2 = Payment
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  private readonly paymentService = inject(PaymentService);
  private readonly mastersService = inject(MastersService);
  private readonly dialogService = inject(DialogService);

  public readonly parties = signal<PartyDto[]>([]);
  public readonly openInvoices = signal<OpenInvoiceDto[]>([]);
  public readonly loadingInvoices = signal<boolean>(false);
  public readonly isSaving = signal<boolean>(false);

  public selectedPartyId: string = '';
  public paymentMode: number = 4; // UPI by default
  public paymentDate: string = new Date().toISOString().substring(0, 10);
  public amount: number = 0;
  public referenceNumber: string = '';
  public bankName: string = '';
  public notes: string = '';

  public allocationsMap: { [key: string]: number } = {};

  ngOnInit(): void {
    this.loadParties();
  }

  loadParties(): void {
    // 1 = Customer, 2 = Supplier
    const typeFilter = this.paymentType === 1 ? 1 : 2;
    this.mastersService.loadParties(typeFilter).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.parties.set(res.data);
        }
      }
    });
  }

  onTypeChange(): void {
    this.loadParties();
    this.selectedPartyId = '';
    this.openInvoices.set([]);
    this.allocationsMap = {};
  }

  onPartyChange(): void {
    this.allocationsMap = {};
    if (!this.selectedPartyId) {
      this.openInvoices.set([]);
      return;
    }

    this.loadingInvoices.set(true);
    this.paymentService.getOpenInvoices(this.selectedPartyId).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.openInvoices.set(res.data);
        }
        this.loadingInvoices.set(false);
      },
      error: () => this.loadingInvoices.set(false)
    });
  }

  onAmountChange(): void {
    // trigger recalculation
  }

  onAllocationChange(): void {
    // trigger recalculation
  }

  autoAllocateFifo(): void {
    let remainingToAllocate = this.amount;
    this.allocationsMap = {};

    for (const inv of this.openInvoices()) {
      if (remainingToAllocate <= 0) break;

      const alloc = Math.min(remainingToAllocate, inv.outstandingBalance);
      this.allocationsMap[inv.salesDocumentId] = alloc;
      remainingToAllocate -= alloc;
    }
  }

  totalAllocated(): number {
    let total = 0;
    for (const key of Object.keys(this.allocationsMap)) {
      const val = Number(this.allocationsMap[key]);
      if (!isNaN(val) && val > 0) {
        total += val;
      }
    }
    return Math.round(total * 100) / 100;
  }

  unallocatedAmount(): number {
    return Math.round((this.amount - this.totalAllocated()) * 100) / 100;
  }

  isValid(): boolean {
    if (!this.selectedPartyId) return false;
    if (this.amount <= 0) return false;
    if (this.unallocatedAmount() < 0) return false;
    return true;
  }

  savePayment(): void {
    if (!this.isValid()) return;

    const allocations = Object.keys(this.allocationsMap)
      .filter((k) => this.allocationsMap[k] > 0)
      .map((k) => ({
        salesDocumentId: k,
        allocatedAmount: this.allocationsMap[k]
      }));

    const req: CreatePaymentRequest = {
      partyId: this.selectedPartyId,
      paymentType: this.paymentType,
      paymentMode: this.paymentMode,
      paymentDate: this.paymentDate,
      amount: this.amount,
      referenceNumber: this.referenceNumber || undefined,
      bankName: this.bankName || undefined,
      notes: this.notes || undefined,
      allocations: allocations.length > 0 ? allocations : undefined
    };

    this.isSaving.set(true);
    this.paymentService.recordPayment(req).subscribe({
      next: (res) => {
        this.isSaving.set(false);
        if (res.success) {
          this.dialogService.toast(`Payment voucher ${res.data?.paymentNumber || ''} recorded successfully!`, 'success');
          this.saved.emit();
          this.closeModal();
        } else {
          this.dialogService.alert('Payment Error', res.message || 'Failed to record payment.', 'danger');
        }
      },
      error: (err) => {
        this.isSaving.set(false);
        this.dialogService.alert('Submission Error', err.error?.message || err.message || 'An unexpected error occurred.', 'danger');
      }
    });
  }

  closeModal(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('bm-modal-backdrop')) {
      this.closeModal();
    }
  }
}
