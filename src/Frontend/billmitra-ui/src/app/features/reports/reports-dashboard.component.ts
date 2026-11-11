import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ReportService,
  ComprehensiveReportsBundle,
  FinancialOverviewReport,
  MonthlyFinancialTrend,
  GstLiabilitySummary,
  AgingBucket,
  PartyAgingDetail,
  ProductMovementVelocity
} from '../../core/services/report.service';
import { DialogService } from '../../shared/services/dialog.service';
import { BmPaginationComponent } from '../../shared/components';

@Component({
  selector: 'app-reports-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, BmPaginationComponent],
  template: `
    <div class="space-y-6">
      <!-- Header with Filters & Print -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <div>
          <h1 class="text-2xl font-bold text-slate-800 tracking-tight flex items-center gap-3">
            <span class="w-10 h-10 rounded-xl bg-gradient-to-tr from-indigo-600 to-violet-600 text-white flex items-center justify-center text-lg shadow-md shadow-indigo-100">
              📊
            </span>
            Executive Financial & Analytics Dashboard
          </h1>
          <p class="text-sm text-slate-500 mt-1">Real-time business performance, GST liabilities, debt aging, and stock movement velocities</p>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <!-- Date Range Picker -->
          <div class="flex items-center gap-2 bg-slate-50 p-1.5 rounded-xl border border-slate-200">
            <input type="date" [(ngModel)]="fromDate" class="bg-white px-3 py-1.5 text-xs font-medium rounded-lg border border-slate-200 text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            <span class="text-slate-400 text-xs">to</span>
            <input type="date" [(ngModel)]="toDate" class="bg-white px-3 py-1.5 text-xs font-medium rounded-lg border border-slate-200 text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500" />
          </div>

          <button (click)="loadReports()" class="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 text-white text-xs font-semibold rounded-xl shadow-sm transition flex items-center gap-1.5">
            <span>🔄</span> Refresh
          </button>

          <button (click)="printReport()" class="px-4 py-2 bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 text-xs font-semibold rounded-xl shadow-sm transition flex items-center gap-1.5">
            <span>🖨️</span> Print Report
          </button>
        </div>
      </div>

      <!-- Quick Preset Filter Buttons -->
      <div class="flex items-center gap-2">
        <span class="text-xs font-semibold text-slate-500 mr-1">Quick Select:</span>
        <button (click)="applyQuickFilter('thisMonth')" [class.bg-indigo-100]="activeFilter === 'thisMonth'" [class.text-indigo-700]="activeFilter === 'thisMonth'" class="px-3 py-1 text-xs font-medium rounded-lg bg-white border border-slate-200 hover:bg-slate-50 text-slate-600 transition">
          This Month
        </button>
        <button (click)="applyQuickFilter('lastMonth')" [class.bg-indigo-100]="activeFilter === 'lastMonth'" [class.text-indigo-700]="activeFilter === 'lastMonth'" class="px-3 py-1 text-xs font-medium rounded-lg bg-white border border-slate-200 hover:bg-slate-50 text-slate-600 transition">
          Last Month
        </button>
        <button (click)="applyQuickFilter('thisQuarter')" [class.bg-indigo-100]="activeFilter === 'thisQuarter'" [class.text-indigo-700]="activeFilter === 'thisQuarter'" class="px-3 py-1 text-xs font-medium rounded-lg bg-white border border-slate-200 hover:bg-slate-50 text-slate-600 transition">
          This Quarter
        </button>
        <button (click)="applyQuickFilter('financialYear')" [class.bg-indigo-100]="activeFilter === 'financialYear'" [class.text-indigo-700]="activeFilter === 'financialYear'" class="px-3 py-1 text-xs font-medium rounded-lg bg-white border border-slate-200 hover:bg-slate-50 text-slate-600 transition">
          FY 2026-27 (YTD)
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="p-16 text-center bg-white rounded-2xl border border-slate-200">
        <div class="inline-block animate-spin text-3xl mb-3">🔄</div>
        <p class="text-slate-600 font-medium">Aggregating real-time financial metrics and analytics...</p>
      </div>

      <div *ngIf="!isLoading && bundle" class="space-y-6">
        <!-- 1. Executive KPI Cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <!-- Total Sales -->
          <div class="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-100 p-5 rounded-2xl shadow-sm">
            <div class="flex items-center justify-between mb-2">
              <span class="text-xs font-bold text-blue-700 uppercase tracking-wider">Total Sales (Revenue)</span>
              <span class="text-lg">📈</span>
            </div>
            <div class="text-2xl font-black text-blue-950">₹{{ bundle.overview.totalSales | number:'1.2-2' }}</div>
            <div class="text-xs text-blue-600 mt-1 font-medium">Gross revenue generated</div>
          </div>

          <!-- Total Purchases & Direct Expenses -->
          <div class="bg-gradient-to-br from-slate-50 to-slate-100 border border-slate-200 p-5 rounded-2xl shadow-sm">
            <div class="flex items-center justify-between mb-2">
              <span class="text-xs font-bold text-slate-600 uppercase tracking-wider">Purchases & Expenses</span>
              <span class="text-lg">🧾</span>
            </div>
            <div class="text-2xl font-black text-slate-900">₹{{ (bundle.overview.totalPurchases + bundle.overview.totalExpenses) | number:'1.2-2' }}</div>
            <div class="text-xs text-slate-500 mt-1">Purchases: ₹{{ bundle.overview.totalPurchases | number:'1.0-0' }} | Exp: ₹{{ bundle.overview.totalExpenses | number:'1.0-0' }}</div>
          </div>

          <!-- Net Profit & Margin -->
          <div [class.from-emerald-50]="bundle.overview.netProfit >= 0" [class.to-teal-50]="bundle.overview.netProfit >= 0" [class.border-emerald-100]="bundle.overview.netProfit >= 0"
               [class.from-rose-50]="bundle.overview.netProfit < 0" [class.to-red-50]="bundle.overview.netProfit < 0" [class.border-rose-100]="bundle.overview.netProfit < 0"
               class="bg-gradient-to-br border p-5 rounded-2xl shadow-sm">
            <div class="flex items-center justify-between mb-2">
              <span [class.text-emerald-700]="bundle.overview.netProfit >= 0" [class.text-rose-700]="bundle.overview.netProfit < 0" class="text-xs font-bold uppercase tracking-wider">
                Net Profit
              </span>
              <span class="text-lg">{{ bundle.overview.netProfit >= 0 ? '💰' : '⚠️' }}</span>
            </div>
            <div [class.text-emerald-950]="bundle.overview.netProfit >= 0" [class.text-rose-950]="bundle.overview.netProfit < 0" class="text-2xl font-black">
              ₹{{ bundle.overview.netProfit | number:'1.2-2' }}
            </div>
            <div class="text-xs font-semibold mt-1" [class.text-emerald-600]="bundle.overview.netProfit >= 0" [class.text-rose-600]="bundle.overview.netProfit < 0">
              Margin: {{ bundle.overview.profitMarginPercent }}%
            </div>
          </div>

          <!-- Receivables vs Payables -->
          <div class="bg-gradient-to-br from-amber-50 to-orange-50 border border-amber-100 p-5 rounded-2xl shadow-sm">
            <div class="flex items-center justify-between mb-2">
              <span class="text-xs font-bold text-amber-800 uppercase tracking-wider">Receivables / Payables</span>
              <span class="text-lg">⚖️</span>
            </div>
            <div class="flex items-baseline justify-between">
              <div>
                <span class="text-xs font-bold text-emerald-700">Rec: </span>
                <span class="text-lg font-black text-emerald-900">₹{{ bundle.overview.totalReceivables | number:'1.0-0' }}</span>
              </div>
              <div>
                <span class="text-xs font-bold text-rose-700">Pay: </span>
                <span class="text-lg font-black text-rose-900">₹{{ bundle.overview.totalPayables | number:'1.0-0' }}</span>
              </div>
            </div>
            <div class="text-xs text-amber-700 mt-1 font-medium">Stock Value: ₹{{ bundle.overview.totalStockValuation | number:'1.0-0' }}</div>
          </div>
        </div>

        <!-- 2. Trajectory & Monthly Financial Trends -->
        <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
          <div class="flex items-center justify-between mb-4">
            <div>
              <h2 class="text-lg font-bold text-slate-800">6-Month Financial Trend & Net Margin</h2>
              <p class="text-xs text-slate-500">Historical performance trajectory of Sales, Purchases, Direct Expenses, and Net Profits</p>
            </div>
            <span class="text-xs font-bold px-2.5 py-1 bg-indigo-50 text-indigo-700 rounded-lg border border-indigo-100">Rolling 6 Months</span>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 text-slate-600 font-bold border-b border-slate-200">
                <tr>
                  <th class="py-3 px-4">Month</th>
                  <th class="py-3 px-4 text-right">Sales</th>
                  <th class="py-3 px-4 text-right">Purchases</th>
                  <th class="py-3 px-4 text-right">Expenses</th>
                  <th class="py-3 px-4 text-right">Net Profit</th>
                  <th class="py-3 px-4">Profit Trend</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 text-slate-700">
                <tr *ngFor="let t of bundle.monthlyTrends" class="hover:bg-slate-50 transition">
                  <td class="py-3 px-4 font-bold text-slate-800">{{ t.monthLabel }}</td>
                  <td class="py-3 px-4 text-right font-medium text-blue-700">₹{{ t.salesAmount | number:'1.2-2' }}</td>
                  <td class="py-3 px-4 text-right font-medium text-slate-600">₹{{ t.purchaseAmount | number:'1.2-2' }}</td>
                  <td class="py-3 px-4 text-right font-medium text-slate-600">₹{{ t.expenseAmount | number:'1.2-2' }}</td>
                  <td class="py-3 px-4 text-right font-bold" [class.text-emerald-600]="t.netProfit >= 0" [class.text-rose-600]="t.netProfit < 0">
                    ₹{{ t.netProfit | number:'1.2-2' }}
                  </td>
                  <td class="py-3 px-4">
                    <div class="w-full bg-slate-100 rounded-full h-2.5 overflow-hidden">
                      <div class="h-2.5 rounded-full" 
                           [class.bg-emerald-500]="t.netProfit >= 0" 
                           [class.bg-rose-500]="t.netProfit < 0"
                           [style.width.%]="getTrendPercent(t)"></div>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- 3. GST 3B & ITC Liability Summary Card -->
        <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
          <div class="flex items-center justify-between mb-4">
            <div>
              <h2 class="text-lg font-bold text-slate-800 flex items-center gap-2">
                <span>🏛️</span> GST Liability & ITC Setoff (GSTR-3B Estimate)
              </h2>
              <p class="text-xs text-slate-500">Outward output tax liability offset against eligible Inward Input Tax Credit (ITC)</p>
            </div>
            <div class="text-right">
              <div class="text-xs font-bold text-slate-500">Net Tax Payable</div>
              <div class="text-xl font-black text-rose-600">₹{{ bundle.gstSummary.totalNetGstPayable | number:'1.2-2' }}</div>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mt-2">
            <!-- Outward Liability -->
            <div class="p-4 bg-blue-50/50 rounded-xl border border-blue-100">
              <div class="text-xs font-bold text-blue-800 uppercase mb-2">Outward Taxable & Output Tax</div>
              <div class="text-sm font-semibold text-slate-800 mb-1">Taxable: ₹{{ bundle.gstSummary.totalOutwardTaxable | number:'1.2-2' }}</div>
              <div class="space-y-1 text-xs text-slate-600">
                <div class="flex justify-between"><span>CGST:</span> <span class="font-bold">₹{{ bundle.gstSummary.outwardCgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between"><span>SGST:</span> <span class="font-bold">₹{{ bundle.gstSummary.outwardSgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between"><span>IGST:</span> <span class="font-bold">₹{{ bundle.gstSummary.outwardIgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between border-t border-blue-200 pt-1 font-bold text-blue-900">
                  <span>Total Output Tax:</span> <span>₹{{ bundle.gstSummary.totalOutwardTax | number:'1.2-2' }}</span>
                </div>
              </div>
            </div>

            <!-- Inward Eligible ITC -->
            <div class="p-4 bg-emerald-50/50 rounded-xl border border-emerald-100">
              <div class="text-xs font-bold text-emerald-800 uppercase mb-2">Inward Purchases & Eligible ITC</div>
              <div class="text-sm font-semibold text-slate-800 mb-1">Taxable: ₹{{ bundle.gstSummary.totalInwardTaxable | number:'1.2-2' }}</div>
              <div class="space-y-1 text-xs text-slate-600">
                <div class="flex justify-between"><span>ITC CGST:</span> <span class="font-bold">₹{{ bundle.gstSummary.inwardEligibleCgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between"><span>ITC SGST:</span> <span class="font-bold">₹{{ bundle.gstSummary.inwardEligibleSgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between"><span>ITC IGST:</span> <span class="font-bold">₹{{ bundle.gstSummary.inwardEligibleIgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between border-t border-emerald-200 pt-1 font-bold text-emerald-900">
                  <span>Total Eligible ITC:</span> <span>₹{{ bundle.gstSummary.totalEligibleItc | number:'1.2-2' }}</span>
                </div>
              </div>
            </div>

            <!-- Net Payable (Cash Ledger) -->
            <div class="p-4 bg-rose-50/50 rounded-xl border border-rose-100">
              <div class="text-xs font-bold text-rose-800 uppercase mb-2">Net Cash Liability to Pay</div>
              <div class="text-xs text-slate-500 mb-2">After adjusting eligible input tax credit:</div>
              <div class="space-y-1 text-xs text-slate-600">
                <div class="flex justify-between"><span>Cash CGST:</span> <span class="font-bold text-rose-700">₹{{ bundle.gstSummary.netPayableCgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between"><span>Cash SGST:</span> <span class="font-bold text-rose-700">₹{{ bundle.gstSummary.netPayableSgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between"><span>Cash IGST:</span> <span class="font-bold text-rose-700">₹{{ bundle.gstSummary.netPayableIgst | number:'1.2-2' }}</span></div>
                <div class="flex justify-between border-t border-rose-200 pt-1 font-bold text-rose-900">
                  <span>Net Payable:</span> <span>₹{{ bundle.gstSummary.totalNetGstPayable | number:'1.2-2' }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 4. Debt Aging Matrix & Top Overdue Parties -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <!-- Aging Brackets -->
          <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
            <h2 class="text-lg font-bold text-slate-800 mb-1">Aging Matrix (Receivables & Payables)</h2>
            <p class="text-xs text-slate-500 mb-4">Maturity buckets for outstanding customer invoices and vendor bills</p>

            <div class="space-y-3">
              <div *ngFor="let b of bundle.agingBuckets" class="p-3 bg-slate-50 rounded-xl border border-slate-200 flex items-center justify-between">
                <div>
                  <div class="font-bold text-xs text-slate-800">{{ b.bucketName }}</div>
                  <div class="text-[11px] text-slate-500 mt-0.5">
                    {{ b.invoicesCount }} Invoices | {{ b.billsCount }} Bills
                  </div>
                </div>
                <div class="text-right">
                  <div class="text-xs font-bold text-emerald-700">Rec: ₹{{ b.totalReceivableAmount | number:'1.2-2' }}</div>
                  <div class="text-xs font-bold text-rose-700">Pay: ₹{{ b.totalPayableAmount | number:'1.2-2' }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Top Overdue Parties -->
          <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
            <h2 class="text-lg font-bold text-slate-800 mb-1">Top Overdue Accounts</h2>
            <p class="text-xs text-slate-500 mb-4">Highest pending balances with aging breakdown</p>

            <div class="overflow-y-auto max-h-72">
              <table class="w-full text-left text-xs">
                <thead class="bg-slate-50 text-slate-600 font-bold sticky top-0">
                  <tr>
                    <th class="py-2 px-3">Party Name</th>
                    <th class="py-2 px-3 text-right">Balance</th>
                    <th class="py-2 px-3 text-right">0-30d</th>
                    <th class="py-2 px-3 text-right">31-60d</th>
                    <th class="py-2 px-3 text-right">60d+</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100 text-slate-700">
                  <tr *ngFor="let p of paginatedOverdueParties" class="hover:bg-slate-50 transition">
                    <td class="py-2 px-3">
                      <div class="font-bold text-slate-800">{{ p.partyName }}</div>
                      <div class="text-[10px] text-slate-400 font-mono">{{ p.gstin || 'Unregistered' }}</div>
                    </td>
                    <td class="py-2 px-3 text-right font-black text-rose-600">₹{{ p.currentBalance | number:'1.0-0' }}</td>
                    <td class="py-2 px-3 text-right text-slate-600">₹{{ p.days0To30 | number:'1.0-0' }}</td>
                    <td class="py-2 px-3 text-right text-amber-600">₹{{ p.days31To60 | number:'1.0-0' }}</td>
                    <td class="py-2 px-3 text-right font-bold text-rose-600">₹{{ (p.days61To90 + p.days90Plus) | number:'1.0-0' }}</td>
                  </tr>
                  <tr *ngIf="bundle.topOverdueParties.length === 0">
                    <td colspan="5" class="py-6 text-center text-slate-400 font-medium">No overdue customer accounts found 🎉</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <bm-pagination
              *ngIf="bundle.topOverdueParties.length > 0"
              [totalItems]="bundle.topOverdueParties.length"
              [(currentPage)]="overduePage"
              [(pageSize)]="overduePageSize"
              [pageSizeOptions]="[5, 10, 20]"
              itemName="parties">
            </bm-pagination>
          </div>
        </div>

        <!-- 5. Product Movement Velocities & Inventory Optimization -->
        <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
          <div class="flex items-center justify-between mb-4">
            <div>
              <h2 class="text-lg font-bold text-slate-800">Product Turnover Velocity & Dead Stock Analysis</h2>
              <p class="text-xs text-slate-500">Classifies fast-moving inventory vs idle capital locked in slow-moving items</p>
            </div>
            <span class="text-xs font-medium text-slate-500">{{ bundle.productVelocities.length }} Catalog Items Tracked</span>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 text-slate-600 font-bold border-b border-slate-200">
                <tr>
                  <th class="py-3 px-4">Product Name & SKU</th>
                  <th class="py-3 px-4 text-center">Velocity Classification</th>
                  <th class="py-3 px-4 text-right">Units Sold</th>
                  <th class="py-3 px-4 text-right">Revenue Generated</th>
                  <th class="py-3 px-4 text-right">Current Stock</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 text-slate-700">
                <tr *ngFor="let item of paginatedProductVelocities" class="hover:bg-slate-50 transition">
                  <td class="py-3 px-4">
                    <div class="font-bold text-slate-800">{{ item.productName }}</div>
                    <div class="text-[11px] text-slate-400 font-mono">{{ item.sku || 'No SKU' }}</div>
                  </td>
                  <td class="py-3 px-4 text-center">
                    <span [ngClass]="{
                      'bg-emerald-100 text-emerald-800 border-emerald-200': item.velocityClassification === 'Fast Moving',
                      'bg-blue-100 text-blue-800 border-blue-200': item.velocityClassification === 'Moderate',
                      'bg-amber-100 text-amber-800 border-amber-200': item.velocityClassification === 'Slow Moving / Dead Stock'
                    }" class="px-2.5 py-1 rounded-full text-[11px] font-bold border">
                      {{ item.velocityClassification }}
                    </span>
                  </td>
                  <td class="py-3 px-4 text-right font-bold text-slate-800">{{ item.totalSoldQuantity }}</td>
                  <td class="py-3 px-4 text-right font-medium text-blue-700">₹{{ item.totalRevenue | number:'1.2-2' }}</td>
                  <td class="py-3 px-4 text-right font-bold" [class.text-rose-600]="item.currentStock <= 5" [class.text-slate-800]="item.currentStock > 5">
                    {{ item.currentStock }}
                  </td>
                </tr>
                <tr *ngIf="bundle.productVelocities.length === 0">
                  <td colspan="5" class="py-8 text-center text-slate-400 font-medium">No product turnover records recorded for this duration.</td>
                </tr>
              </tbody>
            </table>
          </div>
          <bm-pagination
            *ngIf="bundle.productVelocities.length > 0"
            [totalItems]="bundle.productVelocities.length"
            [(currentPage)]="velocityPage"
            [(pageSize)]="velocityPageSize"
            itemName="products">
          </bm-pagination>
        </div>
      </div>
    </div>
  `
})
export class ReportsDashboardComponent implements OnInit {
  private reportService = inject(ReportService);
  private dialog = inject(DialogService);

  bundle: ComprehensiveReportsBundle | null = null;
  isLoading = false;
  activeFilter = 'thisMonth';

  fromDate: string = '';
  toDate: string = '';

  overduePage = 1;
  overduePageSize = 5;

  velocityPage = 1;
  velocityPageSize = 10;

  get paginatedOverdueParties(): PartyAgingDetail[] {
    if (!this.bundle) return [];
    const start = (this.overduePage - 1) * this.overduePageSize;
    return this.bundle.topOverdueParties.slice(start, start + this.overduePageSize);
  }

  get paginatedProductVelocities(): ProductMovementVelocity[] {
    if (!this.bundle) return [];
    const start = (this.velocityPage - 1) * this.velocityPageSize;
    return this.bundle.productVelocities.slice(start, start + this.velocityPageSize);
  }

  ngOnInit() {
    this.applyQuickFilter('thisMonth');
  }

  applyQuickFilter(filter: string) {
    this.activeFilter = filter;
    const now = new Date();

    if (filter === 'thisMonth') {
      const start = new Date(now.getFullYear(), now.getMonth(), 1);
      this.fromDate = this.formatDate(start);
      this.toDate = this.formatDate(now);
    } else if (filter === 'lastMonth') {
      const start = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      const end = new Date(now.getFullYear(), now.getMonth(), 0);
      this.fromDate = this.formatDate(start);
      this.toDate = this.formatDate(end);
    } else if (filter === 'thisQuarter') {
      const quarter = Math.floor(now.getMonth() / 3);
      const start = new Date(now.getFullYear(), quarter * 3, 1);
      this.fromDate = this.formatDate(start);
      this.toDate = this.formatDate(now);
    } else if (filter === 'financialYear') {
      // Indian Financial Year starts April 1
      const fyYear = now.getMonth() >= 3 ? now.getFullYear() : now.getFullYear() - 1;
      const start = new Date(fyYear, 3, 1);
      this.fromDate = this.formatDate(start);
      this.toDate = this.formatDate(now);
    }

    this.loadReports();
  }

  loadReports() {
    this.overduePage = 1;
    this.velocityPage = 1;
    this.isLoading = true;
    this.reportService.getComprehensiveReports(this.fromDate, this.toDate).subscribe({
      next: (data) => {
        this.bundle = data;
        this.isLoading = false;
      },
      error: (err: any) => {
        this.isLoading = false;
        this.dialog.alert('Error', err?.error?.message || 'Failed to aggregate business reports.', 'danger');
      }
    });
  }

  getTrendPercent(t: MonthlyFinancialTrend): number {
    if (!this.bundle || this.bundle.monthlyTrends.length === 0) return 0;
    const maxVal = Math.max(...this.bundle.monthlyTrends.map(x => Math.max(Math.abs(x.salesAmount), Math.abs(x.netProfit))));
    if (maxVal <= 0) return 0;
    return Math.min(100, Math.round((Math.abs(t.netProfit) / maxVal) * 100));
  }

  printReport() {
    window.print();
  }

  private formatDate(d: Date): string {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
