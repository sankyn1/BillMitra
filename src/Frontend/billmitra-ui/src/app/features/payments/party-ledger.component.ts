import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentService, LedgerEntryDto, PartyAgingDto } from '../../core/services/payment.service';
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
  selector: 'app-party-ledger',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BmButtonDirective,
    BmPanelDirective,
    BmPanelHeaderDirective,
    BmPanelBodyDirective,
    BmInputDirective,
    BmSelectDirective,
    BmTableDirective,
    BmTableHeaderCellDirective,
    BmTableRowDirective,
    BmTableCellDirective,
    BmBadgeDirective
  ],
  template: `
    <div class="bm-ledger-container">
      <!-- Header with Filters -->
      <div class="bm-content-header">
        <div>
          <h1 class="bm-page-title">Customer & Supplier Statement of Accounts</h1>
          <p class="bm-page-subtitle">Chronological Ledger Running Balance, Overdue Aging & Audit Trails</p>
        </div>
        <div class="bm-header-buttons">
          <button bmButton bmVariant="outline" bmSize="md" (click)="printStatement()">
            <i class="pi pi-print"></i> Print Statement
          </button>
          <button bmButton bmVariant="secondary" bmSize="md" (click)="navigateBack.emit()">
            <i class="pi pi-arrow-left"></i> Back to Payments
          </button>
        </div>
      </div>

      <!-- Controls Panel -->
      <div bmPanel class="mb-4">
        <div bmPanelBody>
          <div class="bm-filter-grid">
            <div class="bm-form-group col-party">
              <label class="bm-label">Select Account *</label>
              <select bmSelect class="bm-select-full" [(ngModel)]="selectedPartyId" (change)="loadLedgerAndAging()">
                <option [ngValue]="''">-- Choose Customer or Supplier --</option>
                @for (p of parties(); track p.id) {
                  <option [value]="p.id">{{ p.name }} ({{ p.partyType === 1 ? 'Customer' : p.partyType === 2 ? 'Supplier' : 'Both' }})</option>
                }
              </select>
            </div>

            <div class="bm-form-group">
              <label class="bm-label">From Date</label>
              <input bmInput type="date" [(ngModel)]="fromDate" />
            </div>

            <div class="bm-form-group">
              <label class="bm-label">To Date</label>
              <input bmInput type="date" [(ngModel)]="toDate" />
            </div>

            <div class="bm-form-group flex-end">
              <button bmButton bmVariant="primary" bmSize="md" (click)="loadLedgerAndAging()" [disabled]="!selectedPartyId || loading()">
                <i class="pi pi-search"></i> Generate Statement
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Aging Summary Cards -->
      @if (aging(); as ag) {
        <div class="bm-aging-grid mb-4">
          <div class="bm-aging-card card-total">
            <span class="bm-aging-label">Net Outstanding Balance</span>
            <span class="bm-aging-val">₹{{ ag.currentOutstanding | number: '1.2-2' }}</span>
            <span class="bm-aging-sub">Total Invoiced: ₹{{ ag.totalInvoiced | number: '1.2-2' }}</span>
          </div>

          <div class="bm-aging-card card-bucket">
            <span class="bm-aging-label">Current (0 - 30 Days)</span>
            <span class="bm-aging-val text-success">₹{{ ag.days0To30 | number: '1.2-2' }}</span>
            <span class="bm-aging-sub">Within normal terms</span>
          </div>

          <div class="bm-aging-card card-bucket">
            <span class="bm-aging-label">31 - 60 Days</span>
            <span class="bm-aging-val text-warning">₹{{ ag.days31To60 | number: '1.2-2' }}</span>
            <span class="bm-aging-sub">Moderate delay</span>
          </div>

          <div class="bm-aging-card card-bucket">
            <span class="bm-aging-label">61 - 90 Days</span>
            <span class="bm-aging-val text-warning">₹{{ ag.days61To90 | number: '1.2-2' }}</span>
            <span class="bm-aging-sub">Attention required</span>
          </div>

          <div class="bm-aging-card card-bucket">
            <span class="bm-aging-label">Overdue (90+ Days)</span>
            <span class="bm-aging-val text-danger">₹{{ ag.days90Plus | number: '1.2-2' }}</span>
            <span class="bm-aging-sub">Critical collection</span>
          </div>
        </div>
      }

      <!-- Statement Ledger Table -->
      <div bmPanel>
        <div bmPanelHeader>
          <div class="bm-statement-header">
            <h3 class="bm-panel-title">Transaction Ledger & Running Balance</h3>
            <span class="text-sm text-secondary">{{ entries().length }} total entries in period</span>
          </div>
        </div>

        <div bmPanelBody class="p-0">
          @if (loading()) {
            <div class="bm-loading-placeholder p-4">
              <i class="pi pi-spin pi-spinner mr-2"></i> Computing running balance...
            </div>
          } @else if (entries().length === 0) {
            <div class="bm-empty-placeholder p-5 text-center">
              <i class="pi pi-book text-muted mb-2" style="font-size: 2rem;"></i>
              <p class="text-muted">{{ selectedPartyId ? 'No transactions found for this account in the specified date range.' : 'Select an account above to generate its transaction statement.' }}</p>
            </div>
          } @else {
            <div class="bm-table-responsive">
              <table bmTable class="bm-ledger-table">
                <thead>
                  <tr>
                    <th bmHeaderCell>Date</th>
                    <th bmHeaderCell>Voucher Type</th>
                    <th bmHeaderCell>Voucher #</th>
                    <th bmHeaderCell>Reference / Details</th>
                    <th bmHeaderCell class="text-right">Debit (₹)</th>
                    <th bmHeaderCell class="text-right">Credit (₹)</th>
                    <th bmHeaderCell class="text-right">Running Balance (₹)</th>
                  </tr>
                </thead>
                <tbody>
                  @for (e of entries(); track $index) {
                    <tr bmTableRow>
                      <td bmTableCell>{{ e.date | date: 'dd-MMM-yyyy' }}</td>
                      <td bmTableCell>
                        <span [bmBadge]="getBadgeVariant(e.voucherType)">
                          {{ e.voucherType }}
                        </span>
                      </td>
                      <td bmTableCell><strong>{{ e.voucherNumber }}</strong></td>
                      <td bmTableCell>
                        <div>{{ e.reference || '-' }}</div>
                        @if (e.notes) {
                          <div class="text-xs text-muted">{{ e.notes }}</div>
                        }
                      </td>
                      <td bmTableCell class="text-right font-mono">
                        {{ e.debitAmount > 0 ? (e.debitAmount | number: '1.2-2') : '-' }}
                      </td>
                      <td bmTableCell class="text-right font-mono">
                        {{ e.creditAmount > 0 ? (e.creditAmount | number: '1.2-2') : '-' }}
                      </td>
                      <td bmTableCell class="text-right font-mono font-bold" [ngClass]="e.runningBalance >= 0 ? 'text-primary' : 'text-danger'">
                        ₹{{ Math.abs(e.runningBalance) | number: '1.2-2' }}
                        <small class="text-xs">{{ e.runningBalance >= 0 ? 'Dr' : 'Cr' }}</small>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bm-ledger-container {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .bm-filter-grid {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr auto;
      gap: 1rem;
      align-items: flex-end;
    }

    .bm-form-group {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }

    .flex-end {
      align-self: flex-end;
    }

    .bm-label {
      font-size: 0.8rem;
      font-weight: 600;
      color: #334155;
    }

    .bm-select-full {
      width: 100%;
    }

    .bm-aging-grid {
      display: grid;
      grid-template-columns: 1.5fr repeat(4, 1fr);
      gap: 1rem;
    }

    .bm-aging-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .card-total {
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
      border-left: 4px solid var(--bm-primary, #0284c7);
    }

    .bm-aging-label {
      font-size: 0.75rem;
      text-transform: uppercase;
      font-weight: 600;
      color: #64748b;
    }

    .bm-aging-val {
      font-size: 1.3rem;
      font-weight: 700;
      color: #0f172a;
    }

    .bm-aging-sub {
      font-size: 0.75rem;
      color: #94a3b8;
    }

    .bm-statement-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
    }

    .bm-panel-title {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
      color: #0f172a;
    }

    .font-mono {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    }
  `]
})
export class PartyLedgerComponent implements OnInit {
  @Input() initialPartyId?: string;
  @Output() navigateBack = new EventEmitter<void>();

  private readonly paymentService = inject(PaymentService);
  private readonly mastersService = inject(MastersService);
  private readonly dialogService = inject(DialogService);

  public readonly parties = signal<PartyDto[]>([]);
  public readonly entries = signal<LedgerEntryDto[]>([]);
  public readonly aging = signal<PartyAgingDto | null>(null);
  public readonly loading = signal<boolean>(false);

  public selectedPartyId: string = '';
  public fromDate: string = '';
  public toDate: string = '';

  protected readonly Math = Math;

  ngOnInit(): void {
    this.loadParties();
    if (this.initialPartyId) {
      this.selectedPartyId = this.initialPartyId;
      this.loadLedgerAndAging();
    }
  }

  loadParties(): void {
    this.mastersService.loadParties().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.parties.set(res.data);
        }
      }
    });
  }

  loadLedgerAndAging(): void {
    if (!this.selectedPartyId) {
      this.entries.set([]);
      this.aging.set(null);
      return;
    }

    this.loading.set(true);

    this.paymentService.getPartyAging(this.selectedPartyId).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.aging.set(res.data);
        }
      }
    });

    this.paymentService.getPartyLedger(this.selectedPartyId, this.fromDate || undefined, this.toDate || undefined).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.entries.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  getBadgeVariant(voucherType: string): 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info' {
    switch (voucherType.toLowerCase()) {
      case 'invoice':
        return 'primary';
      case 'receipt':
        return 'success';
      case 'payment':
        return 'warning';
      case 'reversal':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  printStatement(): void {
    window.print();
  }
}
