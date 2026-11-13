import { Component, EventEmitter, OnInit, Output, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentService, PaymentDto } from '../../core/services/payment.service';
import { DialogService } from '../../shared/services/dialog.service';
import { PaymentModalComponent } from './payment-modal.component';
import { BmPaginationComponent, BmGridToolbarComponent } from '../../shared/components';
import { GridColumnDef } from '../../shared/models';
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
  selector: 'app-payment-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PaymentModalComponent,
    BmPaginationComponent,
    BmGridToolbarComponent,
    BmButtonDirective,
    BmPanelDirective,
    BmPanelBodyDirective,
    BmTableDirective,
    BmTableHeaderCellDirective,
    BmTableRowDirective,
    BmTableCellDirective,
    BmBadgeDirective
  ],
  template: `
    <div class="bm-payments-container">
      <!-- Header -->
      <div class="bm-content-header">
        <div>
          <h1 class="bm-page-title">Payments & Receipts Register</h1>
          <p class="bm-page-subtitle">Track Collections, Multi-Invoice Settle, Advance Balances & Reversals</p>
        </div>
        <div class="bm-header-buttons">
          <button bmButton bmVariant="outline" bmSize="md" (click)="openPartyLedger.emit()">
            <i class="pi pi-book"></i> Party Ledger & Aging
          </button>
          <button bmButton bmVariant="outline" bmSize="md" (click)="openCreateModal(2)">
            <i class="pi pi-arrow-up-right text-warning"></i> Record Supplier Payment
          </button>
          <button bmButton bmVariant="primary" bmSize="md" (click)="openCreateModal(1)">
            <i class="pi pi-arrow-down-left text-success"></i> + Record Customer Receipt
          </button>
        </div>
      </div>

      <!-- KPI Summary Cards -->
      <div class="bm-kpi-grid">
        <div class="bm-kpi-card border-success">
          <div class="bm-kpi-icon bg-success-light text-success">
            <i class="pi pi-arrow-down-left"></i>
          </div>
          <div class="bm-kpi-info">
            <span class="bm-kpi-label">Total Customer Receipts</span>
            <span class="bm-kpi-value text-success">₹{{ totalReceipts() | number: '1.2-2' }}</span>
          </div>
        </div>

        <div class="bm-kpi-card border-warning">
          <div class="bm-kpi-icon bg-warning-light text-warning">
            <i class="pi pi-arrow-up-right"></i>
          </div>
          <div class="bm-kpi-info">
            <span class="bm-kpi-label">Total Supplier Payments</span>
            <span class="bm-kpi-value text-warning">₹{{ totalPayments() | number: '1.2-2' }}</span>
          </div>
        </div>

        <div class="bm-kpi-card border-info">
          <div class="bm-kpi-icon bg-info-light text-primary">
            <i class="pi pi-wallet"></i>
          </div>
          <div class="bm-kpi-info">
            <span class="bm-kpi-label">Unallocated Advances Held</span>
            <span class="bm-kpi-value">₹{{ totalUnallocated() | number: '1.2-2' }}</span>
          </div>
        </div>
      </div>

      <!-- Search, Export & Column Customization Toolbar -->
      <bm-grid-toolbar
        gridId="payment_receipt_register"
        [columns]="paymentColumns"
        [data]="paymentService.payments()"
        title="Payment_Receipt_Register"
        [totalCount]="filteredPayments().length"
        itemName="Vouchers"
        searchPlaceholder="Search by Voucher #, Party, Reference..."
        [(searchTerm)]="searchQuery"
        (searchTermChange)="currentPage.set(1)">
        <div toolbar-actions class="bm-tabs">
          <button class="bm-tab-btn" [class.active]="selectedTab === 'all'" (click)="setTab('all')">
            All ({{ paymentService.payments().length }})
          </button>
          <button class="bm-tab-btn" [class.active]="selectedTab === 'receipts'" (click)="setTab('receipts')">
            Receipts ({{ countReceipts() }})
          </button>
          <button class="bm-tab-btn" [class.active]="selectedTab === 'payments'" (click)="setTab('payments')">
            Payments ({{ countPayments() }})
          </button>
          <button class="bm-tab-btn" [class.active]="selectedTab === 'reversed'" (click)="setTab('reversed')">
            Reversed ({{ countReversed() }})
          </button>
        </div>
      </bm-grid-toolbar>

      <!-- Data Table Panel -->
      <div bmPanel>
        <div bmPanelBody class="p-0">
          @if (paymentService.loading()) {
            <div class="bm-loading-placeholder p-4">
              <i class="pi pi-spin pi-spinner mr-2"></i> Loading payment records...
            </div>
          } @else if (filteredPayments().length === 0) {
            <div class="bm-empty-placeholder p-5 text-center">
              <i class="pi pi-credit-card text-muted mb-2" style="font-size: 2.2rem;"></i>
              <p class="text-muted">No payment or receipt vouchers found.</p>
              <button bmButton bmVariant="primary" bmSize="sm" class="mt-2" (click)="openCreateModal(1)">
                Record First Receipt
              </button>
            </div>
          } @else {
            <div class="bm-table-responsive">
              <table bmTable class="bm-payments-table">
                <thead>
                  <tr>
                    <th bmHeaderCell *ngIf="isColVisible('paymentDate')">Date</th>
                    <th bmHeaderCell *ngIf="isColVisible('voucherNumber')">Voucher #</th>
                    <th bmHeaderCell *ngIf="isColVisible('partyName')">Party Name</th>
                    <th bmHeaderCell *ngIf="isColVisible('voucherType')">Type</th>
                    <th bmHeaderCell *ngIf="isColVisible('paymentMode')">Mode</th>
                    <th bmHeaderCell *ngIf="isColVisible('referenceNumber')">Reference / Bank</th>
                    <th bmHeaderCell class="text-right" *ngIf="isColVisible('amount')">Amount (₹)</th>
                    <th bmHeaderCell class="text-right" *ngIf="isColVisible('allocatedAmount')">Allocated (₹)</th>
                    <th bmHeaderCell class="text-right" *ngIf="isColVisible('unallocatedAmount')">Advance (₹)</th>
                    <th bmHeaderCell *ngIf="isColVisible('isReversed')">Status</th>
                    <th bmHeaderCell class="text-center" style="width: 110px;" *ngIf="isColVisible('actions')">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  @for (p of paginatedPayments(); track p.id) {
                    <tr bmTableRow>
                      <td bmTableCell *ngIf="isColVisible('paymentDate')">{{ p.paymentDate | date: 'dd-MMM-yyyy' }}</td>
                      <td bmTableCell *ngIf="isColVisible('voucherNumber')"><strong>{{ p.paymentNumber }}</strong></td>
                      <td bmTableCell *ngIf="isColVisible('partyName')">
                        <div class="font-semibold">{{ p.partyName }}</div>
                        @if (p.partyGstin) {
                          <div class="text-xs text-muted">{{ p.partyGstin }}</div>
                        }
                      </td>
                      <td bmTableCell *ngIf="isColVisible('voucherType')">
                        <span [bmBadge]="p.paymentType === 1 ? 'success' : 'warning'">
                          {{ p.paymentType === 1 ? 'Receipt' : 'Payment' }}
                        </span>
                      </td>
                      <td bmTableCell *ngIf="isColVisible('paymentMode')">{{ getModeLabel(p.paymentMode) }}</td>
                      <td bmTableCell *ngIf="isColVisible('referenceNumber')">
                        <div>{{ p.referenceNumber || '-' }}</div>
                        @if (p.bankName) {
                          <div class="text-xs text-muted">{{ p.bankName }}</div>
                        }
                      </td>
                      <td bmTableCell class="text-right font-mono font-bold" *ngIf="isColVisible('amount')">
                        ₹{{ p.amount | number: '1.2-2' }}
                      </td>
                      <td bmTableCell class="text-right font-mono text-muted" *ngIf="isColVisible('allocatedAmount')">
                        ₹{{ p.allocatedAmount | number: '1.2-2' }}
                      </td>
                      <td bmTableCell class="text-right font-mono font-semibold" [ngClass]="p.unallocatedAmount > 0 ? 'text-success' : 'text-muted'" *ngIf="isColVisible('unallocatedAmount')">
                        ₹{{ p.unallocatedAmount | number: '1.2-2' }}
                      </td>
                      <td bmTableCell *ngIf="isColVisible('isReversed')">
                        <span [bmBadge]="p.status === 1 ? 'success' : 'danger'">
                          {{ p.status === 1 ? 'Active' : 'Reversed' }}
                        </span>
                      </td>
                      <td bmTableCell class="text-center" *ngIf="isColVisible('actions')">
                        <div class="bm-actions-group">
                          @if (p.status === 1) {
                            <button
                              class="bm-btn-icon text-danger"
                              title="Reverse Voucher"
                              (click)="promptReverse(p)"
                            >
                              <i class="pi pi-replay"></i>
                            </button>
                          } @else {
                            <span class="text-xs text-muted">Reversed</span>
                          }
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
        <bm-pagination
          [totalItems]="filteredPayments().length"
          [(currentPage)]="currentPage"
          [(pageSize)]="pageSize"
          itemName="vouchers">
        </bm-pagination>
      </div>

      <!-- Record Modal -->
      @if (showCreateModal) {
        <app-payment-modal
          [paymentType]="modalType"
          (close)="showCreateModal = false"
          (saved)="onPaymentSaved()"
        ></app-payment-modal>
      }
    </div>
  `,
  styles: [`
    .bm-payments-container {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .bm-kpi-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1.25rem;
    }

    .bm-kpi-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 1.15rem;
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .border-success { border-left: 4px solid #16a34a; }
    .border-warning { border-left: 4px solid #d97706; }
    .border-info { border-left: 4px solid #0284c7; }

    .bm-kpi-icon {
      width: 44px;
      height: 44px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.25rem;
    }

    .bg-success-light { background: #dcfce7; }
    .bg-warning-light { background: #fef3c7; }
    .bg-info-light { background: #e0f2fe; }

    .bm-kpi-info {
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
    }

    .bm-kpi-label {
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      color: #64748b;
    }

    .bm-kpi-value {
      font-size: 1.35rem;
      font-weight: 700;
      color: #0f172a;
    }

    .bm-filter-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
    }

    .bm-tabs {
      display: flex;
      gap: 0.5rem;
      background: #f1f5f9;
      padding: 0.25rem;
      border-radius: 6px;
    }

    .bm-tab-btn {
      background: none;
      border: none;
      padding: 0.4rem 0.85rem;
      font-size: 0.825rem;
      font-weight: 600;
      color: #64748b;
      cursor: pointer;
      border-radius: 4px;
      transition: all 0.15s ease;
    }

    .bm-tab-btn.active {
      background: #ffffff;
      color: #0f172a;
      box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
    }

    .bm-search-wrap {
      position: relative;
      width: 320px;
    }

    .bm-search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: #94a3b8;
    }

    .bm-search-input {
      width: 100%;
      padding-left: 2.25rem;
    }

    .font-mono {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    }

    .bm-actions-group {
      display: flex;
      justify-content: center;
      gap: 0.5rem;
    }

    .bm-btn-icon {
      background: none;
      border: none;
      padding: 0.35rem;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.95rem;
    }

    .bm-btn-icon:hover {
      background: #f1f5f9;
    }
  `]
})
export class PaymentListComponent implements OnInit {
  @Output() openPartyLedger = new EventEmitter<void>();

  public readonly paymentService = inject(PaymentService);
  private readonly dialogService = inject(DialogService);

  public selectedTab: 'all' | 'receipts' | 'payments' | 'reversed' = 'all';
  public searchQuery: string = '';
  public showCreateModal: boolean = false;
  public modalType: number = 1; // 1 = Receipt, 2 = Payment

  public currentPage = signal<number>(1);
  public pageSize = signal<number>(10);

  public paymentColumns: GridColumnDef[] = [
    { key: 'paymentDate', header: 'Date', formatter: (p) => new Date(p.paymentDate).toLocaleDateString() },
    { key: 'voucherNumber', header: 'Voucher #', locked: true, formatter: (p) => p.paymentNumber },
    { key: 'partyName', header: 'Party Name', locked: true, formatter: (p) => `${p.partyName}${p.partyGstin ? ' (' + p.partyGstin + ')' : ''}` },
    { key: 'voucherType', header: 'Type', formatter: (p) => p.paymentType === 1 ? 'Receipt' : 'Payment' },
    { key: 'paymentMode', header: 'Mode', formatter: (p) => this.getModeLabel(p.paymentMode) },
    { key: 'referenceNumber', header: 'Reference / Bank', formatter: (p) => `${p.referenceNumber || '-'}${p.bankName ? ' (' + p.bankName + ')' : ''}` },
    { key: 'amount', header: 'Amount (₹)', align: 'right', formatter: (p) => `₹ ${p.amount}` },
    { key: 'allocatedAmount', header: 'Allocated (₹)', align: 'right', formatter: (p) => `₹ ${p.allocatedAmount}` },
    { key: 'unallocatedAmount', header: 'Advance (₹)', align: 'right', formatter: (p) => `₹ ${p.unallocatedAmount}` },
    { key: 'isReversed', header: 'Status', align: 'center', formatter: (p) => p.status === 1 ? 'Active' : 'Reversed' },
    { key: 'actions', header: 'Actions', exportable: false, sortable: false, locked: true }
  ];

  public isColVisible(key: string): boolean {
    const col = this.paymentColumns.find(c => c.key === key);
    return col ? col.visible !== false : true;
  }

  public get visibleColsCount(): number {
    return this.paymentColumns.filter(c => c.visible !== false).length;
  }

  public paginatedPayments = computed(() => {
    const list = this.filteredPayments();
    const start = (this.currentPage() - 1) * this.pageSize();
    return list.slice(start, start + this.pageSize());
  });

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.currentPage.set(1);
    this.paymentService.loadPayments().subscribe();
  }

  setTab(tab: 'all' | 'receipts' | 'payments' | 'reversed'): void {
    this.selectedTab = tab;
    this.currentPage.set(1);
  }

  openCreateModal(type: number): void {
    this.modalType = type;
    this.showCreateModal = true;
  }

  onPaymentSaved(): void {
    this.refresh();
  }

  promptReverse(payment: PaymentDto): void {
    this.dialogService.confirm(
      'Reverse Payment Voucher',
      `Are you sure you want to reverse voucher ${payment.paymentNumber} of ₹${payment.amount}? All allocated invoice balances will be restored immediately.`,
      'Confirm Reversal',
      true,
      'warning'
    ).then((confirmed) => {
      if (confirmed) {
        this.paymentService.reversePayment(payment.id, 'User initiated reversal').subscribe({
          next: (res) => {
            if (res.success) {
              this.dialogService.toast(`Voucher ${payment.paymentNumber} reversed successfully.`, 'success');
              this.refresh();
            } else {
              this.dialogService.alert('Reversal Error', res.message || 'Failed to reverse voucher.', 'danger');
            }
          },
          error: (err) => {
            this.dialogService.alert('Error', err.error?.message || err.message || 'Failed to reverse voucher.', 'danger');
          }
        });
      }
    });
  }

  filteredPayments(): PaymentDto[] {
    let list = this.paymentService.payments();

    if (this.selectedTab === 'receipts') {
      list = list.filter((p) => p.paymentType === 1 && p.status === 1);
    } else if (this.selectedTab === 'payments') {
      list = list.filter((p) => p.paymentType === 2 && p.status === 1);
    } else if (this.selectedTab === 'reversed') {
      list = list.filter((p) => p.status === 2);
    }

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      list = list.filter((p) =>
        p.paymentNumber.toLowerCase().includes(q) ||
        p.partyName.toLowerCase().includes(q) ||
        (p.referenceNumber && p.referenceNumber.toLowerCase().includes(q))
      );
    }

    return list;
  }

  totalReceipts(): number {
    return this.paymentService.payments()
      .filter((p) => p.paymentType === 1 && p.status === 1)
      .reduce((sum, p) => sum + p.amount, 0);
  }

  totalPayments(): number {
    return this.paymentService.payments()
      .filter((p) => p.paymentType === 2 && p.status === 1)
      .reduce((sum, p) => sum + p.amount, 0);
  }

  totalUnallocated(): number {
    return this.paymentService.payments()
      .filter((p) => p.status === 1)
      .reduce((sum, p) => sum + p.unallocatedAmount, 0);
  }

  countReceipts(): number {
    return this.paymentService.payments().filter((p) => p.paymentType === 1 && p.status === 1).length;
  }

  countPayments(): number {
    return this.paymentService.payments().filter((p) => p.paymentType === 2 && p.status === 1).length;
  }

  countReversed(): number {
    return this.paymentService.payments().filter((p) => p.status === 2).length;
  }

  getModeLabel(mode: number): string {
    switch (mode) {
      case 1: return 'Cash';
      case 2: return 'Bank Transfer';
      case 3: return 'Cheque';
      case 4: return 'UPI';
      case 5: return 'Credit Card';
      case 6: return 'Debit Card';
      default: return 'Other';
    }
  }
}
