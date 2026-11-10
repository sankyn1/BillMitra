import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  PurchaseExpenseService,
  ExpenseDto,
  CreateExpenseRequest
} from '../../core/services/purchase-expense.service';
import { DialogService } from '../../shared/services/dialog.service';

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900">Expenses & Cash Outflows</h1>
          <p class="text-sm text-slate-500">Record business operating expenses, maintain petty cash vouchers, and track claimable GST inputs</p>
        </div>
        <div class="flex items-center gap-3">
          <button (click)="openAddCategoryModal()" class="rounded-lg border border-slate-300 bg-white px-3.5 py-2.5 text-sm font-semibold text-slate-700 shadow-xs hover:bg-slate-50">
            + New Category
          </button>
          <button (click)="openCreateModal()" class="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-emerald-500">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Record Expense Voucher
          </button>
        </div>
      </div>

      <!-- KPI Metrics -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-4">
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-slate-500">Total Expenses</div>
          <div class="mt-2 text-2xl font-bold text-slate-900">₹{{ totalExpenses() | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-slate-500">{{ expenses().length }} expense entries</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-emerald-600">Claimable ITC on Expenses</div>
          <div class="mt-2 text-2xl font-bold text-emerald-700">₹{{ claimableGst() | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-emerald-600">Valid GST tax invoices</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-blue-600">Bank / Digital Outflow</div>
          <div class="mt-2 text-2xl font-bold text-blue-700">₹{{ digitalExpenses() | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-blue-600">UPI, NetBanking & Cards</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-amber-600">Cash / Petty Cash</div>
          <div class="mt-2 text-2xl font-bold text-amber-700">₹{{ cashExpenses() | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-amber-600">Direct cash payments</div>
        </div>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
        <div class="flex flex-wrap items-center gap-3">
          <div class="w-64">
            <input type="text" [(ngModel)]="searchQuery" placeholder="Search payee, voucher or notes..."
                   class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
          </div>
          <select [(ngModel)]="categoryFilter"
                  class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-hidden">
            <option value="">All Categories</option>
            <option *ngFor="let cat of categories()" [value]="cat.id">{{ cat.name }}</option>
          </select>
        </div>
      </div>

      <!-- Expense Vouchers Table -->
      <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm text-slate-600">
            <thead class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-500">
              <tr>
                <th class="px-4 py-3.5">Voucher #</th>
                <th class="px-4 py-3.5">Date</th>
                <th class="px-4 py-3.5">Category</th>
                <th class="px-4 py-3.5">Payee / Vendor</th>
                <th class="px-4 py-3.5">Payment Mode</th>
                <th class="px-4 py-3.5 text-right">Net Amount</th>
                <th class="px-4 py-3.5 text-right">GST</th>
                <th class="px-4 py-3.5 text-right">Total Outflow</th>
                <th class="px-4 py-3.5 text-center">ITC</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngFor="let exp of filteredExpenses()" class="hover:bg-slate-50/75">
                <td class="px-4 py-3 font-mono font-medium text-slate-900">{{ exp.voucherNumber }}</td>
                <td class="px-4 py-3 text-slate-600">{{ exp.expenseDate | date:'mediumDate' }}</td>
                <td class="px-4 py-3">
                  <span class="inline-flex items-center rounded-md bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
                    {{ exp.expenseCategoryName }}
                  </span>
                </td>
                <td class="px-4 py-3 font-medium text-slate-800">{{ exp.payeeName || 'Cash Voucher' }}</td>
                <td class="px-4 py-3">
                  <span class="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium"
                        [ngClass]="exp.paymentMode === 1 ? 'bg-amber-50 text-amber-700' : 'bg-blue-50 text-blue-700'">
                    {{ exp.paymentModeName }}
                  </span>
                  <div *ngIf="exp.referenceNumber" class="text-xs text-slate-400 font-mono">{{ exp.referenceNumber }}</div>
                </td>
                <td class="px-4 py-3 text-right font-medium text-slate-700">₹{{ exp.amount | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-right text-slate-600">
                  <span *ngIf="exp.gstAmount > 0">₹{{ exp.gstAmount | number:'1.2-2' }} ({{ exp.gstRate }}%)</span>
                  <span *ngIf="exp.gstAmount === 0" class="text-slate-400">-</span>
                </td>
                <td class="px-4 py-3 text-right font-semibold text-slate-900">₹{{ exp.totalAmount | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-center">
                  <span *ngIf="exp.isItcClaimable" class="inline-flex items-center rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-semibold text-emerald-700">
                    Eligible
                  </span>
                  <span *ngIf="!exp.isItcClaimable" class="text-xs text-slate-400">
                    None
                  </span>
                </td>
              </tr>
              <tr *ngIf="filteredExpenses().length === 0">
                <td colspan="9" class="px-4 py-12 text-center text-slate-400">
                  No expenses recorded matching the current filter.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Record Expense Modal -->
      <div *ngIf="isModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
        <div class="w-full max-w-xl rounded-2xl bg-white p-6 shadow-xl">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <h3 class="text-lg font-bold text-slate-900">Record Expense Voucher</h3>
            <button (click)="closeModal()" class="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="mt-4 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">Expense Category *</label>
                <select [(ngModel)]="newExpense.expenseCategoryId"
                        class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                  <option value="">Select Category</option>
                  <option *ngFor="let cat of categories()" [value]="cat.id">{{ cat.name }}</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Expense Date *</label>
                <input type="date" [(ngModel)]="newExpense.expenseDate"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">Net Amount (₹) *</label>
                <input type="number" [(ngModel)]="newExpense.amount" min="1" placeholder="0.00"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">GST Rate (%)</label>
                <select [(ngModel)]="newExpense.gstRate"
                        class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                  <option [value]="0">0% (Nil / Exempt)</option>
                  <option [value]="5">5%</option>
                  <option [value]="12">12%</option>
                  <option [value]="18">18%</option>
                  <option [value]="28">28%</option>
                </select>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">Payment Mode</label>
                <select [(ngModel)]="newExpense.paymentMode"
                        class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                  <option [value]="1">Cash</option>
                  <option [value]="2">Bank Transfer (NEFT/RTGS)</option>
                  <option [value]="3">Cheque</option>
                  <option [value]="4">UPI</option>
                  <option [value]="5">Credit Card</option>
                  <option [value]="6">Debit Card</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Reference / Txn #</label>
                <input type="text" [(ngModel)]="newExpense.referenceNumber" placeholder="UPI-TXN or Cheque No"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
            </div>

            <div>
              <label class="block text-xs font-medium text-slate-700">Payee / Vendor Name</label>
              <input type="text" [(ngModel)]="newExpense.payeeName" placeholder="e.g. Reliance Fresh, Local Electrician"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>

            <div>
              <label class="block text-xs font-medium text-slate-700">Notes / Remarks</label>
              <input type="text" [(ngModel)]="newExpense.notes" placeholder="e.g. Office tea supplies and snacks"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>

            <div class="flex items-center gap-2 pt-2">
              <input type="checkbox" id="itcCheck" [(ngModel)]="newExpense.isItcClaimable" class="h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500" />
              <label for="itcCheck" class="text-xs font-medium text-slate-700">ITC Claimable on GST Return (Valid GST invoice available)</label>
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button (click)="closeModal()" class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Cancel</button>
            <button (click)="saveExpense()" class="rounded-lg bg-emerald-600 px-5 py-2 text-sm font-semibold text-white hover:bg-emerald-500">Record Voucher</button>
          </div>
        </div>
      </div>

      <!-- Add Category Modal -->
      <div *ngIf="isCategoryModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
        <div class="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <h3 class="text-lg font-bold text-slate-900">Add Expense Category</h3>
            <button (click)="closeCategoryModal()" class="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="mt-4 space-y-4">
            <div>
              <label class="block text-xs font-medium text-slate-700">Category Name *</label>
              <input type="text" [(ngModel)]="newCategoryName" placeholder="e.g. Legal & Professional Fees"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-700">Description</label>
              <input type="text" [(ngModel)]="newCategoryDescription" placeholder="Optional notes"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button (click)="closeCategoryModal()" class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Cancel</button>
            <button (click)="saveCategory()" class="rounded-lg bg-emerald-600 px-5 py-2 text-sm font-semibold text-white hover:bg-emerald-500">Create Category</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ExpenseListComponent implements OnInit {
  private expenseService = inject(PurchaseExpenseService);
  private dialogService = inject(DialogService);

  expenses = this.expenseService.expenses;
  categories = this.expenseService.categories;
  searchQuery = '';
  categoryFilter = '';

  isModalOpen = false;
  isCategoryModalOpen = false;

  newCategoryName = '';
  newCategoryDescription = '';

  newExpense: CreateExpenseRequest = {
    expenseCategoryId: '',
    expenseDate: new Date().toISOString().split('T')[0],
    amount: 0,
    gstRate: 0,
    isItcClaimable: false,
    paymentMode: 1,
    referenceNumber: '',
    payeeName: '',
    notes: ''
  };

  ngOnInit() {
    this.expenseService.loadCategories().subscribe();
    this.expenseService.loadExpenses().subscribe();
  }

  totalExpenses = computed(() => this.expenses().reduce((acc, e) => acc + (e.totalAmount || 0), 0));
  claimableGst = computed(() => this.expenses().reduce((acc, e) => acc + (e.isItcClaimable ? (e.gstAmount || 0) : 0), 0));
  cashExpenses = computed(() => this.expenses().reduce((acc, e) => acc + (e.paymentMode === 1 ? (e.totalAmount || 0) : 0), 0));
  digitalExpenses = computed(() => this.expenses().reduce((acc, e) => acc + (e.paymentMode !== 1 ? (e.totalAmount || 0) : 0), 0));

  filteredExpenses = computed(() => {
    let list = this.expenses();
    if (this.categoryFilter) {
      list = list.filter(e => e.expenseCategoryId === this.categoryFilter);
    }
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      list = list.filter(e =>
        e.voucherNumber.toLowerCase().includes(q) ||
        (e.payeeName && e.payeeName.toLowerCase().includes(q)) ||
        (e.notes && e.notes.toLowerCase().includes(q)) ||
        e.expenseCategoryName.toLowerCase().includes(q)
      );
    }
    return list;
  });

  openCreateModal() {
    if (this.categories().length > 0 && !this.newExpense.expenseCategoryId) {
      this.newExpense.expenseCategoryId = this.categories()[0].id;
    }
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
  }

  openAddCategoryModal() {
    this.newCategoryName = '';
    this.newCategoryDescription = '';
    this.isCategoryModalOpen = true;
  }

  closeCategoryModal() {
    this.isCategoryModalOpen = false;
  }

  async saveCategory() {
    if (!this.newCategoryName.trim()) {
      await this.dialogService.alert('Missing Field', 'Category name is required.', 'warning');
      return;
    }

    this.expenseService.createCategory(this.newCategoryName, this.newCategoryDescription).subscribe({
      next: () => {
        this.closeCategoryModal();
        this.dialogService.toast('Category created successfully.', 'success');
      },
      error: (err) => {
        this.dialogService.alert('Error', err?.error?.message || 'Could not create category.', 'danger');
      }
    });
  }

  async saveExpense() {
    if (!this.newExpense.expenseCategoryId) {
      await this.dialogService.alert('Missing Field', 'Please select an expense category.', 'warning');
      return;
    }
    if (this.newExpense.amount <= 0) {
      await this.dialogService.alert('Invalid Amount', 'Expense amount must be greater than zero.', 'warning');
      return;
    }

    this.expenseService.createExpense(this.newExpense).subscribe({
      next: () => {
        this.closeModal();
        this.dialogService.toast('Expense voucher recorded successfully.', 'success');
      },
      error: (err) => {
        this.dialogService.alert('Error', err?.error?.message || 'Could not record expense.', 'danger');
      }
    });
  }
}
