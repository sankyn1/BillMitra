import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  PurchaseExpenseService,
  PurchaseDocumentDto,
  CreateOrUpdatePurchaseRequest,
  PurchaseDocumentItemInput
} from '../../core/services/purchase-expense.service';
import { DialogService } from '../../shared/services/dialog.service';
import { BmPaginationComponent } from '../../shared/components';

@Component({
  selector: 'app-purchase-list',
  standalone: true,
  imports: [CommonModule, FormsModule, BmPaginationComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900">Inward Purchases & Bills</h1>
          <p class="text-sm text-slate-500">Track inward supplier invoices, verify Input Tax Credit (ITC) eligibility, and manage accounts payable</p>
        </div>
        <div class="flex items-center gap-3">
          <button (click)="openCreateModal()" class="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-emerald-500">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Record Inward Purchase
          </button>
        </div>
      </div>

      <!-- KPI Metrics -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-4">
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-slate-500">Total Inward Purchases</div>
          <div class="mt-2 text-2xl font-bold text-slate-900">₹{{ totalPurchases() | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-slate-500">{{ purchases().length }} invoices recorded</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-emerald-600">Eligible ITC Claim</div>
          <div class="mt-2 text-2xl font-bold text-emerald-700">₹{{ totalItc() | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-emerald-600">Inputs & Services (Table 4A)</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-amber-600">Outstanding Balance</div>
          <div class="mt-2 text-2xl font-bold text-amber-700">₹{{ totalOutstanding() | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-amber-600">Accounts payable pending</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-indigo-600">Total GST Paid</div>
          <div class="mt-2 text-2xl font-bold text-indigo-700">₹{{ totalGstPaid() | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-indigo-600">CGST + SGST + IGST</div>
        </div>
      </div>

      <!-- Filter Bar -->
      <div class="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
        <div class="flex flex-wrap items-center gap-3">
          <div class="w-64">
            <input type="text" [(ngModel)]="searchQuery" placeholder="Search supplier or bill no..."
                   class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
          </div>
          <select [(ngModel)]="statusFilter" (change)="applyFilters()"
                  class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-hidden">
            <option value="">All Statuses</option>
            <option value="1">Draft</option>
            <option value="2">Finalized</option>
            <option value="3">Paid</option>
            <option value="4">Partially Paid</option>
            <option value="5">Cancelled</option>
          </select>
        </div>
      </div>

      <!-- Inward Purchases Table -->
      <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm text-slate-600">
            <thead class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-500">
              <tr>
                <th class="px-4 py-3.5">Bill / Invoice #</th>
                <th class="px-4 py-3.5">Supplier Details</th>
                <th class="px-4 py-3.5">Bill Date</th>
                <th class="px-4 py-3.5">Place of Supply</th>
                <th class="px-4 py-3.5 text-right">Taxable</th>
                <th class="px-4 py-3.5 text-right">GST</th>
                <th class="px-4 py-3.5 text-right">Grand Total</th>
                <th class="px-4 py-3.5 text-center">Status</th>
                <th class="px-4 py-3.5 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngFor="let doc of paginatedPurchases()" class="hover:bg-slate-50/75">
                <td class="px-4 py-3 font-medium text-slate-900">
                  {{ doc.supplierInvoiceNumber }}
                  <div class="text-xs text-slate-400">Created: {{ doc.createdAtUtc | date:'shortDate' }}</div>
                </td>
                <td class="px-4 py-3">
                  <div class="font-medium text-slate-800">{{ doc.supplierName }}</div>
                  <div class="text-xs text-slate-400 font-mono">{{ doc.supplierGstin || 'Unregistered' }}</div>
                </td>
                <td class="px-4 py-3 text-slate-600">{{ doc.supplierInvoiceDate | date:'mediumDate' }}</td>
                <td class="px-4 py-3">
                  <span class="inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium"
                        [ngClass]="doc.supplierStateCode === doc.placeOfSupplyStateCode ? 'bg-blue-50 text-blue-700' : 'bg-purple-50 text-purple-700'">
                    State {{ doc.supplierStateCode }} {{ doc.supplierStateCode === doc.placeOfSupplyStateCode ? '(Intra)' : '(Inter)' }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right font-medium text-slate-700">₹{{ doc.subTotalTaxable | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-right text-slate-600">₹{{ doc.totalTax | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-right font-semibold text-slate-900">₹{{ doc.grandTotal | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-center">
                  <span class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold"
                        [ngClass]="{
                          'bg-amber-100 text-amber-800': doc.documentStatus === 1,
                          'bg-emerald-100 text-emerald-800': doc.documentStatus === 2,
                          'bg-blue-100 text-blue-800': doc.documentStatus === 3,
                          'bg-rose-100 text-rose-800': doc.documentStatus === 5
                        }">
                    {{ doc.documentStatusName }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button *ngIf="doc.documentStatus === 1" (click)="finalizeDoc(doc)"
                            class="rounded-md bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-700 hover:bg-emerald-100">
                      Finalize
                    </button>
                    <button *ngIf="doc.documentStatus === 2 && doc.paidAmount === 0" (click)="cancelDoc(doc)"
                            class="rounded-md bg-rose-50 px-2.5 py-1 text-xs font-semibold text-rose-700 hover:bg-rose-100">
                      Cancel
                    </button>
                  </div>
                </td>
              </tr>
              <tr *ngIf="filteredPurchases().length === 0">
                <td colspan="9" class="px-4 py-12 text-center text-slate-400">
                  No inward purchase bills found matching current filter.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <bm-pagination
          [totalItems]="filteredPurchases().length"
          [(currentPage)]="currentPage"
          [(pageSize)]="pageSize"
          itemName="inward bills">
        </bm-pagination>
      </div>

      <!-- Record Purchase Modal -->
      <div *ngIf="isModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
        <div class="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-2xl bg-white p-6 shadow-xl">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <h3 class="text-lg font-bold text-slate-900">Record Inward Supplier Purchase</h3>
            <button (click)="closeModal()" class="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="mt-4 space-y-4">
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <div>
                <label class="block text-xs font-medium text-slate-700">Supplier Name *</label>
                <input type="text" [(ngModel)]="newPurchase.supplierName" placeholder="e.g. Tata Steel BSL"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Supplier GSTIN</label>
                <input type="text" [(ngModel)]="newPurchase.supplierGstin" placeholder="24AAAAA0000A1Z5"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm font-mono uppercase focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Supplier State Code (2-digit)</label>
                <input type="text" [(ngModel)]="newPurchase.supplierStateCode" placeholder="24" maxlength="2"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
            </div>

            <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <div>
                <label class="block text-xs font-medium text-slate-700">Supplier Bill / Invoice # *</label>
                <input type="text" [(ngModel)]="newPurchase.supplierInvoiceNumber" placeholder="INV-2026-901"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Supplier Invoice Date</label>
                <input type="date" [(ngModel)]="newPurchase.supplierInvoiceDate"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">ITC Eligibility</label>
                <select [(ngModel)]="newPurchase.defaultItcEligibility"
                        class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                  <option [value]="1">Eligible - Inputs & Services (Table 4A(5))</option>
                  <option [value]="2">Eligible - Capital Goods (Table 4A(4))</option>
                  <option [value]="3">Ineligible - Section 17(5) Blocked</option>
                  <option [value]="4">Inward Reverse Charge (Table 4A(3))</option>
                </select>
              </div>
            </div>

            <!-- Items Table -->
            <div class="mt-4">
              <div class="flex items-center justify-between pb-2">
                <span class="text-xs font-bold uppercase text-slate-500">Bill Items</span>
                <button (click)="addItemRow()" class="text-xs font-semibold text-emerald-600 hover:text-emerald-700">+ Add Item</button>
              </div>
              <table class="w-full text-xs">
                <thead class="border-y border-slate-200 bg-slate-50 text-slate-500">
                  <tr>
                    <th class="py-2 text-left">Item Name</th>
                    <th class="py-2 text-left">HSN</th>
                    <th class="py-2 text-right">Qty</th>
                    <th class="py-2 text-left">Unit</th>
                    <th class="py-2 text-right">Price (₹)</th>
                    <th class="py-2 text-right">Disc %</th>
                    <th class="py-2 text-right">GST %</th>
                    <th class="py-2 text-center">Action</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  <tr *ngFor="let item of newPurchase.items; let i = index">
                    <td class="py-1 pr-2"><input type="text" [(ngModel)]="item.itemName" placeholder="Item description" class="w-full rounded border border-slate-300 px-2 py-1 text-xs" /></td>
                    <td class="py-1 pr-2"><input type="text" [(ngModel)]="item.hsnCode" placeholder="HSN" class="w-20 rounded border border-slate-300 px-2 py-1 text-xs font-mono" /></td>
                    <td class="py-1 pr-2"><input type="number" [(ngModel)]="item.quantity" min="1" class="w-16 rounded border border-slate-300 px-2 py-1 text-xs text-right" /></td>
                    <td class="py-1 pr-2"><input type="text" [(ngModel)]="item.unit" class="w-14 rounded border border-slate-300 px-2 py-1 text-xs" /></td>
                    <td class="py-1 pr-2"><input type="number" [(ngModel)]="item.unitPrice" min="0" class="w-24 rounded border border-slate-300 px-2 py-1 text-xs text-right" /></td>
                    <td class="py-1 pr-2"><input type="number" [(ngModel)]="item.discountPercent" min="0" max="100" class="w-16 rounded border border-slate-300 px-2 py-1 text-xs text-right" /></td>
                    <td class="py-1 pr-2">
                      <select [(ngModel)]="item.gstRate" class="w-16 rounded border border-slate-300 px-2 py-1 text-xs">
                        <option [value]="0">0%</option>
                        <option [value]="5">5%</option>
                        <option [value]="12">12%</option>
                        <option [value]="18">18%</option>
                        <option [value]="28">28%</option>
                      </select>
                    </td>
                    <td class="py-1 text-center">
                      <button *ngIf="newPurchase.items.length > 1" (click)="removeItemRow(i)" class="text-rose-500 hover:text-rose-700">✕</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button (click)="closeModal()" class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Cancel</button>
            <button (click)="savePurchase()" class="rounded-lg bg-emerald-600 px-5 py-2 text-sm font-semibold text-white hover:bg-emerald-500">Record Inward Bill</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class PurchaseListComponent implements OnInit {
  private purchaseService = inject(PurchaseExpenseService);
  private dialogService = inject(DialogService);

  purchases = this.purchaseService.purchases;
  searchQuery = '';
  statusFilter = '';
  isModalOpen = false;

  currentPage = signal<number>(1);
  pageSize = signal<number>(10);

  paginatedPurchases = computed(() => {
    const list = this.filteredPurchases();
    const start = (this.currentPage() - 1) * this.pageSize();
    return list.slice(start, start + this.pageSize());
  });

  newPurchase: CreateOrUpdatePurchaseRequest = {
    documentType: 1,
    supplierName: '',
    supplierGstin: '',
    supplierAddress: '',
    supplierStateCode: '24',
    placeOfSupplyStateCode: '24',
    supplierInvoiceNumber: '',
    supplierInvoiceDate: new Date().toISOString().split('T')[0],
    documentDate: new Date().toISOString().split('T')[0],
    isReverseCharge: false,
    defaultItcEligibility: 1,
    items: [
      {
        itemName: '',
        hsnCode: '',
        quantity: 1,
        unit: 'NOS',
        unitPrice: 0,
        discountPercent: 0,
        gstRate: 18,
        itcEligibility: 1
      }
    ]
  };

  ngOnInit() {
    this.purchaseService.loadPurchases().subscribe();
  }

  totalPurchases = computed(() => this.purchases().reduce((acc, p) => acc + (p.grandTotal || 0), 0));
  totalItc = computed(() => this.purchases().reduce((acc, p) => acc + (p.defaultItcEligibility === 1 || p.defaultItcEligibility === 2 ? (p.totalTax || 0) : 0), 0));
  totalOutstanding = computed(() => this.purchases().reduce((acc, p) => acc + (p.outstandingBalance || 0), 0));
  totalGstPaid = computed(() => this.purchases().reduce((acc, p) => acc + (p.totalTax || 0), 0));

  filteredPurchases = computed(() => {
    let list = this.purchases();
    if (this.statusFilter) {
      list = list.filter(p => p.documentStatus.toString() === this.statusFilter);
    }
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      list = list.filter(p =>
        p.supplierName.toLowerCase().includes(q) ||
        p.supplierInvoiceNumber.toLowerCase().includes(q) ||
        (p.supplierGstin && p.supplierGstin.toLowerCase().includes(q))
      );
    }
    return list;
  });

  applyFilters() {
    this.currentPage.set(1);
  }

  openCreateModal() {
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
  }

  addItemRow() {
    this.newPurchase.items.push({
      itemName: '',
      hsnCode: '',
      quantity: 1,
      unit: 'NOS',
      unitPrice: 0,
      discountPercent: 0,
      gstRate: 18,
      itcEligibility: this.newPurchase.defaultItcEligibility
    });
  }

  removeItemRow(index: number) {
    this.newPurchase.items.splice(index, 1);
  }

  async savePurchase() {
    if (!this.newPurchase.supplierName.trim()) {
      await this.dialogService.alert('Missing Field', 'Supplier name is required.', 'warning');
      return;
    }
    if (!this.newPurchase.supplierInvoiceNumber.trim()) {
      await this.dialogService.alert('Missing Field', 'Supplier bill / invoice number is required.', 'warning');
      return;
    }
    if (this.newPurchase.items.some(i => !i.itemName.trim() || i.unitPrice <= 0)) {
      await this.dialogService.alert('Invalid Items', 'Each item must have a valid name and price greater than 0.', 'warning');
      return;
    }

    this.purchaseService.createPurchase(this.newPurchase).subscribe({
      next: async () => {
        this.closeModal();
        this.dialogService.toast('Inward purchase invoice recorded successfully.', 'success');
      },
      error: async (err) => {
        await this.dialogService.alert('Failed to Record', err?.error?.message || 'Error recording purchase.', 'danger');
      }
    });
  }

  async finalizeDoc(doc: PurchaseDocumentDto) {
    const confirmed = await this.dialogService.confirm('Finalize Purchase', `Are you sure you want to finalize bill ${doc.supplierInvoiceNumber}? This locks the GST amounts for GSTR-2B ITC matching.`);
    if (confirmed) {
      this.purchaseService.finalizePurchase(doc.id).subscribe({
        next: () => this.dialogService.toast('Purchase invoice finalized successfully.', 'success'),
        error: (err) => this.dialogService.alert('Error', err?.error?.message || 'Could not finalize invoice.', 'danger')
      });
    }
  }

  async cancelDoc(doc: PurchaseDocumentDto) {
    const confirmed = await this.dialogService.confirm('Cancel Purchase', `Are you sure you want to cancel bill ${doc.supplierInvoiceNumber}?`, 'danger');
    if (confirmed) {
      this.purchaseService.cancelPurchase(doc.id, 'Cancelled by user').subscribe({
        next: () => this.dialogService.toast('Purchase bill cancelled.', 'success'),
        error: (err) => this.dialogService.alert('Error', err?.error?.message || 'Could not cancel invoice.', 'danger')
      });
    }
  }
}
