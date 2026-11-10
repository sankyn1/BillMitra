import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  InventoryService,
  ProductStockSummaryDto,
  StockItemBatchDto,
  StockMovementDto,
  WarehouseDto,
  StockAdjustmentRequest,
  InterWarehouseTransferRequest,
  CreateStockBatchRequest,
  CreateWarehouseRequest
} from '../../core/services/inventory.service';
import { DialogService } from '../../shared/services/dialog.service';

@Component({
  selector: 'app-inventory-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900">Inventory & Warehouse Management</h1>
          <p class="text-sm text-slate-500">Multi-location stock balances, batch & expiry compliance, FIFO valuation, and stock transfers</p>
        </div>
        <div class="flex flex-wrap items-center gap-2">
          <button (click)="openTransferModal()" class="rounded-lg border border-slate-300 bg-white px-3.5 py-2 text-sm font-semibold text-slate-700 shadow-xs hover:bg-slate-50">
            Transfer Stock
          </button>
          <button (click)="openAdjustmentModal()" class="rounded-lg border border-slate-300 bg-white px-3.5 py-2 text-sm font-semibold text-slate-700 shadow-xs hover:bg-slate-50">
            Reconcile Stock
          </button>
          <button (click)="openAddBatchModal()" class="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-emerald-500">
            + New Batch
          </button>
        </div>
      </div>

      <!-- KPI Summary Cards -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-4">
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-slate-500">Total Stock Valuation</div>
          <div class="mt-2 text-2xl font-bold text-slate-900">₹{{ (metrics()?.totalStockValuation || 0) | number:'1.2-2' }}</div>
          <div class="mt-1 text-xs text-slate-500">FIFO / Purchase cost basis</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-emerald-600">Active SKUs / Items</div>
          <div class="mt-2 text-2xl font-bold text-emerald-700">{{ metrics()?.totalActiveProducts || stockSummaries().length }}</div>
          <div class="mt-1 text-xs text-emerald-600">Across {{ warehouses().length }} location(s)</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-amber-600">Low Stock Reorders</div>
          <div class="mt-2 text-2xl font-bold text-amber-700">{{ metrics()?.lowStockAlertCount || 0 }}</div>
          <div class="mt-1 text-xs text-amber-600">At or below reorder level</div>
        </div>

        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
          <div class="text-xs font-medium uppercase tracking-wider text-rose-600">Expired / At Risk Batches</div>
          <div class="mt-2 text-2xl font-bold text-rose-700">{{ (metrics()?.expiredBatchesCount || 0) + (metrics()?.expiringSoonBatches?.length || 0) }}</div>
          <div class="mt-1 text-xs text-rose-600">{{ metrics()?.expiredBatchesCount || 0 }} expired, {{ metrics()?.expiringSoonBatches?.length || 0 }} expiring soon</div>
        </div>
      </div>

      <!-- Tabs Navigation -->
      <div class="border-b border-slate-200">
        <nav class="-mb-px flex space-x-6 text-sm font-medium">
          <button (click)="activeTab = 'balances'"
                  [ngClass]="activeTab === 'balances' ? 'border-emerald-500 text-emerald-600' : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'"
                  class="whitespace-nowrap border-b-2 py-3 px-1">
            Stock Balances ({{ stockSummaries().length }})
          </button>
          <button (click)="activeTab = 'batches'"
                  [ngClass]="activeTab === 'batches' ? 'border-emerald-500 text-emerald-600' : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'"
                  class="whitespace-nowrap border-b-2 py-3 px-1">
            Batches & Expiry ({{ batches().length }})
          </button>
          <button (click)="activeTab = 'movements'"
                  [ngClass]="activeTab === 'movements' ? 'border-emerald-500 text-emerald-600' : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'"
                  class="whitespace-nowrap border-b-2 py-3 px-1">
            Movement Ledger ({{ movements().length }})
          </button>
          <button (click)="activeTab = 'warehouses'"
                  [ngClass]="activeTab === 'warehouses' ? 'border-emerald-500 text-emerald-600' : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'"
                  class="whitespace-nowrap border-b-2 py-3 px-1">
            Warehouses & Locations ({{ warehouses().length }})
          </button>
        </nav>
      </div>

      <!-- TAB 1: Stock Balances -->
      <div *ngIf="activeTab === 'balances'" class="space-y-4">
        <div class="flex items-center justify-between gap-4">
          <input type="text" [(ngModel)]="stockSearchQuery" placeholder="Search item name, SKU or HSN..."
                 class="w-72 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
          <div class="flex items-center gap-2">
            <label class="flex items-center gap-2 text-xs font-medium text-slate-600 cursor-pointer">
              <input type="checkbox" [(ngModel)]="onlyLowStockFilter" class="rounded border-slate-300 text-emerald-600 focus:ring-emerald-500" />
              Show Low Stock Only
            </label>
          </div>
        </div>

        <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
          <table class="w-full text-left text-sm text-slate-600">
            <thead class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-500">
              <tr>
                <th class="px-4 py-3.5">Product & SKU</th>
                <th class="px-4 py-3.5">HSN / SAC</th>
                <th class="px-4 py-3.5 text-right">Available Qty</th>
                <th class="px-4 py-3.5 text-right">Cost Price</th>
                <th class="px-4 py-3.5 text-right">Selling Price</th>
                <th class="px-4 py-3.5 text-right">Stock Valuation</th>
                <th class="px-4 py-3.5 text-center">Batches</th>
                <th class="px-4 py-3.5 text-center">Status</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngFor="let item of filteredStockSummaries()" class="hover:bg-slate-50/75">
                <td class="px-4 py-3 font-medium text-slate-900">
                  {{ item.productName }}
                  <div class="text-xs text-slate-400 font-mono">{{ item.sku || 'No SKU' }}</div>
                </td>
                <td class="px-4 py-3 font-mono text-slate-600">{{ item.hsnCode }}</td>
                <td class="px-4 py-3 text-right font-semibold text-slate-900">
                  {{ item.totalStockQuantity }} {{ item.unit }}
                </td>
                <td class="px-4 py-3 text-right text-slate-600">₹{{ item.purchasePrice | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-right font-medium text-slate-800">₹{{ item.sellingPrice | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-right font-semibold text-emerald-700">₹{{ item.totalStockValuation | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-center">
                  <span class="inline-flex items-center rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
                    {{ item.activeBatchesCount }} active
                  </span>
                </td>
                <td class="px-4 py-3 text-center">
                  <span *ngIf="item.isLowStock" class="inline-flex items-center rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-800">
                    Low Stock (≤ {{ item.minStockAlert }})
                  </span>
                  <span *ngIf="!item.isLowStock" class="inline-flex items-center rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-800">
                    Adequate
                  </span>
                </td>
              </tr>
              <tr *ngIf="filteredStockSummaries().length === 0">
                <td colspan="8" class="px-4 py-12 text-center text-slate-400">
                  No products found matching current criteria.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- TAB 2: Batches & Expiry -->
      <div *ngIf="activeTab === 'batches'" class="space-y-4">
        <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
          <table class="w-full text-left text-sm text-slate-600">
            <thead class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-500">
              <tr>
                <th class="px-4 py-3.5">Batch #</th>
                <th class="px-4 py-3.5">Product</th>
                <th class="px-4 py-3.5">Warehouse</th>
                <th class="px-4 py-3.5">Mfg Date</th>
                <th class="px-4 py-3.5">Expiry Date</th>
                <th class="px-4 py-3.5 text-right">Qty on Hand</th>
                <th class="px-4 py-3.5 text-right">Cost (₹)</th>
                <th class="px-4 py-3.5 text-right">MRP (₹)</th>
                <th class="px-4 py-3.5 text-center">Compliance</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngFor="let b of batches()" class="hover:bg-slate-50/75">
                <td class="px-4 py-3 font-mono font-bold text-slate-900">{{ b.batchNumber }}</td>
                <td class="px-4 py-3 font-medium text-slate-800">{{ b.productName }}</td>
                <td class="px-4 py-3 text-slate-600">{{ b.warehouseName }}</td>
                <td class="px-4 py-3 text-slate-500">{{ b.manufacturingDate ? (b.manufacturingDate | date:'mediumDate') : '-' }}</td>
                <td class="px-4 py-3 text-slate-600">{{ b.expiryDate ? (b.expiryDate | date:'mediumDate') : 'No Expiry' }}</td>
                <td class="px-4 py-3 text-right font-semibold text-slate-900">{{ b.quantityOnHand }}</td>
                <td class="px-4 py-3 text-right text-slate-600">₹{{ b.purchasePrice | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-right font-medium text-slate-800">₹{{ b.mrp | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-center">
                  <span *ngIf="b.isExpired" class="inline-flex items-center rounded-full bg-rose-100 px-2 py-0.5 text-xs font-semibold text-rose-800">
                    Expired
                  </span>
                  <span *ngIf="b.isExpiringSoon && !b.isExpired" class="inline-flex items-center rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-800">
                    Expiring Soon
                  </span>
                  <span *ngIf="!b.isExpired && !b.isExpiringSoon" class="inline-flex items-center rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-800">
                    Valid
                  </span>
                </td>
              </tr>
              <tr *ngIf="batches().length === 0">
                <td colspan="9" class="px-4 py-12 text-center text-slate-400">
                  No batch records registered yet. Use "+ New Batch" to add lot/batch tracking.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- TAB 3: Movement Ledger -->
      <div *ngIf="activeTab === 'movements'" class="space-y-4">
        <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
          <table class="w-full text-left text-sm text-slate-600">
            <thead class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-500">
              <tr>
                <th class="px-4 py-3.5">Date</th>
                <th class="px-4 py-3.5">Product</th>
                <th class="px-4 py-3.5">Warehouse</th>
                <th class="px-4 py-3.5">Movement Type</th>
                <th class="px-4 py-3.5 text-right">Quantity</th>
                <th class="px-4 py-3.5 text-right">Unit Cost</th>
                <th class="px-4 py-3.5 text-right">Balance After</th>
                <th class="px-4 py-3.5">Reference / Doc #</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr *ngFor="let m of movements()" class="hover:bg-slate-50/75">
                <td class="px-4 py-3 text-slate-600">{{ m.movementDate | date:'short' }}</td>
                <td class="px-4 py-3 font-medium text-slate-900">{{ m.productName }}</td>
                <td class="px-4 py-3 text-slate-600">{{ m.warehouseName }}</td>
                <td class="px-4 py-3">
                  <span class="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-semibold"
                        [ngClass]="m.quantity > 0 ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'">
                    {{ m.movementTypeName }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right font-semibold"
                    [ngClass]="m.quantity > 0 ? 'text-emerald-700' : 'text-rose-700'">
                  {{ m.quantity > 0 ? '+' : '' }}{{ m.quantity }}
                </td>
                <td class="px-4 py-3 text-right text-slate-600">₹{{ m.unitCost | number:'1.2-2' }}</td>
                <td class="px-4 py-3 text-right font-medium text-slate-900">{{ m.balanceQuantityAfter }}</td>
                <td class="px-4 py-3 font-mono text-xs text-slate-500">
                  {{ m.referenceDocumentNumber || m.remarks || '-' }}
                </td>
              </tr>
              <tr *ngIf="movements().length === 0">
                <td colspan="8" class="px-4 py-12 text-center text-slate-400">
                  No stock movements recorded yet.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- TAB 4: Warehouses -->
      <div *ngIf="activeTab === 'warehouses'" class="space-y-4">
        <div class="flex justify-end">
          <button (click)="openAddWarehouseModal()" class="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-emerald-500">
            + Add Warehouse Location
          </button>
        </div>

        <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div *ngFor="let w of warehouses()" class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
            <div class="flex items-center justify-between">
              <span class="font-mono text-xs font-bold uppercase tracking-wider text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded">
                {{ w.code }}
              </span>
              <span *ngIf="w.isDefault" class="text-xs font-medium text-slate-500 bg-slate-100 px-2 py-0.5 rounded">
                Default Store
              </span>
            </div>
            <h3 class="mt-3 text-base font-bold text-slate-900">{{ w.name }}</h3>
            <p class="mt-1 text-xs text-slate-500">{{ w.address || 'No street address' }}</p>
            <p class="text-xs text-slate-500">{{ w.city || 'Primary City' }}, State {{ w.stateCode || '24' }}</p>
          </div>
        </div>
      </div>

      <!-- MODAL: Reconcile Physical Stock -->
      <div *ngIf="isAdjustmentModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
        <div class="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <h3 class="text-lg font-bold text-slate-900">Reconcile Physical Stock Count</h3>
            <button (click)="closeAdjustmentModal()" class="text-slate-400 hover:text-slate-600">✕</button>
          </div>

          <div class="mt-4 space-y-4">
            <div>
              <label class="block text-xs font-medium text-slate-700">Select Product *</label>
              <select [(ngModel)]="adjRequest.productId"
                      class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                <option value="">Select Item</option>
                <option *ngFor="let item of stockSummaries()" [value]="item.productId">{{ item.productName }} (System: {{ item.totalStockQuantity }} {{ item.unit }})</option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-medium text-slate-700">Warehouse Location *</label>
              <select [(ngModel)]="adjRequest.warehouseId"
                      class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                <option *ngFor="let w of warehouses()" [value]="w.id">{{ w.name }} ({{ w.code }})</option>
              </select>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">Adjustment Reason</label>
                <select [(ngModel)]="adjRequest.reason"
                        class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                  <option [value]="1">Physical Count Reconciliation</option>
                  <option [value]="2">Damaged or Broken</option>
                  <option [value]="3">Expired Goods Write-Off</option>
                  <option [value]="4">Wastage or Theft</option>
                  <option [value]="5">Initial Stock Take</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Actual Physical Quantity *</label>
                <input type="number" [(ngModel)]="adjRequest.physicalQuantity" min="0" placeholder="0"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
            </div>

            <div>
              <label class="block text-xs font-medium text-slate-700">Remarks / Audit Note</label>
              <input type="text" [(ngModel)]="adjRequest.remarks" placeholder="e.g. Monthly stock take physical audit"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button (click)="closeAdjustmentModal()" class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Cancel</button>
            <button (click)="saveAdjustment()" class="rounded-lg bg-emerald-600 px-5 py-2 text-sm font-semibold text-white hover:bg-emerald-500">Apply Reconciliation</button>
          </div>
        </div>
      </div>

      <!-- MODAL: Inter-Warehouse Transfer -->
      <div *ngIf="isTransferModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
        <div class="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <h3 class="text-lg font-bold text-slate-900">Inter-Warehouse Stock Transfer</h3>
            <button (click)="closeTransferModal()" class="text-slate-400 hover:text-slate-600">✕</button>
          </div>

          <div class="mt-4 space-y-4">
            <div>
              <label class="block text-xs font-medium text-slate-700">Select Product to Move *</label>
              <select [(ngModel)]="transferRequest.productId"
                      class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                <option value="">Select Item</option>
                <option *ngFor="let item of stockSummaries()" [value]="item.productId">{{ item.productName }}</option>
              </select>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">From (Source) *</label>
                <select [(ngModel)]="transferRequest.sourceWarehouseId"
                        class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                  <option *ngFor="let w of warehouses()" [value]="w.id">{{ w.name }}</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">To (Destination) *</label>
                <select [(ngModel)]="transferRequest.destinationWarehouseId"
                        class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                  <option *ngFor="let w of warehouses()" [value]="w.id">{{ w.name }}</option>
                </select>
              </div>
            </div>

            <div>
              <label class="block text-xs font-medium text-slate-700">Transfer Quantity *</label>
              <input type="number" [(ngModel)]="transferRequest.quantity" min="1" placeholder="Quantity"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>

            <div>
              <label class="block text-xs font-medium text-slate-700">Remarks / Waybill Reference</label>
              <input type="text" [(ngModel)]="transferRequest.remarks" placeholder="e.g. Branch stock replenishment"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button (click)="closeTransferModal()" class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Cancel</button>
            <button (click)="saveTransfer()" class="rounded-lg bg-emerald-600 px-5 py-2 text-sm font-semibold text-white hover:bg-emerald-500">Execute Transfer</button>
          </div>
        </div>
      </div>

      <!-- MODAL: Add Batch -->
      <div *ngIf="isBatchModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
        <div class="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <h3 class="text-lg font-bold text-slate-900">Register Product Lot / Batch</h3>
            <button (click)="closeAddBatchModal()" class="text-slate-400 hover:text-slate-600">✕</button>
          </div>

          <div class="mt-4 space-y-4">
            <div>
              <label class="block text-xs font-medium text-slate-700">Product *</label>
              <select [(ngModel)]="newBatch.productId"
                      class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                <option value="">Select Item</option>
                <option *ngFor="let item of stockSummaries()" [value]="item.productId">{{ item.productName }}</option>
              </select>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">Batch / Lot Number *</label>
                <input type="text" [(ngModel)]="newBatch.batchNumber" placeholder="e.g. LOT-2026-X1"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm font-mono focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Warehouse Location</label>
                <select [(ngModel)]="newBatch.warehouseId"
                        class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden">
                  <option *ngFor="let w of warehouses()" [value]="w.id">{{ w.name }}</option>
                </select>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">Manufacturing Date</label>
                <input type="date" [(ngModel)]="newBatch.manufacturingDate"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Expiry Date</label>
                <input type="date" [(ngModel)]="newBatch.expiryDate"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
            </div>

            <div class="grid grid-cols-3 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">Quantity *</label>
                <input type="number" [(ngModel)]="newBatch.quantityOnHand" min="1" placeholder="0"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">Purchase Cost (₹)</label>
                <input type="number" [(ngModel)]="newBatch.purchasePrice" min="0" placeholder="0.00"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">MRP (₹)</label>
                <input type="number" [(ngModel)]="newBatch.mrp" min="0" placeholder="0.00"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button (click)="closeAddBatchModal()" class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Cancel</button>
            <button (click)="saveBatch()" class="rounded-lg bg-emerald-600 px-5 py-2 text-sm font-semibold text-white hover:bg-emerald-500">Save Batch</button>
          </div>
        </div>
      </div>

      <!-- MODAL: Add Warehouse -->
      <div *ngIf="isWarehouseModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
        <div class="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
          <div class="flex items-center justify-between border-b border-slate-100 pb-4">
            <h3 class="text-lg font-bold text-slate-900">Add Warehouse / Storage Location</h3>
            <button (click)="closeAddWarehouseModal()" class="text-slate-400 hover:text-slate-600">✕</button>
          </div>

          <div class="mt-4 space-y-4">
            <div>
              <label class="block text-xs font-medium text-slate-700">Location Name *</label>
              <input type="text" [(ngModel)]="newWarehouse.name" placeholder="e.g. City Central Godown"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-700">Location Code *</label>
              <input type="text" [(ngModel)]="newWarehouse.code" placeholder="e.g. GODOWN-2"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm font-mono uppercase focus:border-emerald-500 focus:outline-hidden" />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-700">Address</label>
              <input type="text" [(ngModel)]="newWarehouse.address" placeholder="Premises / Street address"
                     class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-slate-700">City</label>
                <input type="text" [(ngModel)]="newWarehouse.city" placeholder="City"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
              <div>
                <label class="block text-xs font-medium text-slate-700">State Code</label>
                <input type="text" [(ngModel)]="newWarehouse.stateCode" placeholder="24" maxlength="2"
                       class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-hidden" />
              </div>
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button (click)="closeAddWarehouseModal()" class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Cancel</button>
            <button (click)="saveWarehouse()" class="rounded-lg bg-emerald-600 px-5 py-2 text-sm font-semibold text-white hover:bg-emerald-500">Create Location</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class InventoryDashboardComponent implements OnInit {
  private invService = inject(InventoryService);
  private dialogService = inject(DialogService);

  activeTab: 'balances' | 'batches' | 'movements' | 'warehouses' = 'balances';
  metrics = this.invService.metrics;
  stockSummaries = this.invService.stockSummaries;
  batches = this.invService.batches;
  movements = this.invService.movements;
  warehouses = this.invService.warehouses;

  stockSearchQuery = '';
  onlyLowStockFilter = false;

  isAdjustmentModalOpen = false;
  isTransferModalOpen = false;
  isBatchModalOpen = false;
  isWarehouseModalOpen = false;

  adjRequest: StockAdjustmentRequest = {
    productId: '',
    warehouseId: '',
    reason: 1,
    physicalQuantity: 0,
    remarks: ''
  };

  transferRequest: InterWarehouseTransferRequest = {
    sourceWarehouseId: '',
    destinationWarehouseId: '',
    productId: '',
    quantity: 1,
    remarks: ''
  };

  newBatch: CreateStockBatchRequest = {
    productId: '',
    warehouseId: '',
    batchNumber: '',
    quantityOnHand: 0,
    purchasePrice: 0,
    mrp: 0
  };

  newWarehouse: CreateWarehouseRequest = {
    name: '',
    code: '',
    address: '',
    city: '',
    stateCode: '24',
    isDefault: false
  };

  ngOnInit() {
    this.invService.loadMetrics().subscribe();
    this.invService.loadStockSummaries().subscribe();
    this.invService.loadBatches().subscribe();
    this.invService.loadMovements().subscribe();
    this.invService.loadWarehouses().subscribe();
  }

  filteredStockSummaries = computed(() => {
    let list = this.stockSummaries();
    if (this.onlyLowStockFilter) {
      list = list.filter(s => s.isLowStock);
    }
    if (this.stockSearchQuery) {
      const q = this.stockSearchQuery.toLowerCase();
      list = list.filter(s =>
        s.productName.toLowerCase().includes(q) ||
        (s.sku && s.sku.toLowerCase().includes(q)) ||
        (s.hsnCode && s.hsnCode.toLowerCase().includes(q))
      );
    }
    return list;
  });

  openAdjustmentModal() {
    if (this.warehouses().length > 0) {
      this.adjRequest.warehouseId = this.warehouses()[0].id;
    }
    this.isAdjustmentModalOpen = true;
  }

  closeAdjustmentModal() {
    this.isAdjustmentModalOpen = false;
  }

  async saveAdjustment() {
    if (!this.adjRequest.productId) {
      await this.dialogService.alert('Missing Field', 'Please select a product to adjust.', 'warning');
      return;
    }
    if (this.adjRequest.physicalQuantity < 0) {
      await this.dialogService.alert('Invalid Quantity', 'Physical count cannot be negative.', 'warning');
      return;
    }

    this.invService.recordAdjustment(this.adjRequest).subscribe({
      next: () => {
        this.closeAdjustmentModal();
        this.dialogService.toast('Stock physical reconciliation applied successfully.', 'success');
      },
      error: (err) => {
        this.dialogService.alert('Error', err?.error?.message || 'Could not reconcile stock.', 'danger');
      }
    });
  }

  openTransferModal() {
    if (this.warehouses().length >= 2) {
      this.transferRequest.sourceWarehouseId = this.warehouses()[0].id;
      this.transferRequest.destinationWarehouseId = this.warehouses()[1].id;
    } else if (this.warehouses().length === 1) {
      this.transferRequest.sourceWarehouseId = this.warehouses()[0].id;
    }
    this.isTransferModalOpen = true;
  }

  closeTransferModal() {
    this.isTransferModalOpen = false;
  }

  async saveTransfer() {
    if (!this.transferRequest.productId) {
      await this.dialogService.alert('Missing Field', 'Please select a product to transfer.', 'warning');
      return;
    }
    if (this.transferRequest.sourceWarehouseId === this.transferRequest.destinationWarehouseId) {
      await this.dialogService.alert('Invalid Selection', 'Source and destination warehouses must be different.', 'warning');
      return;
    }
    if (this.transferRequest.quantity <= 0) {
      await this.dialogService.alert('Invalid Quantity', 'Transfer quantity must be greater than zero.', 'warning');
      return;
    }

    this.invService.recordTransfer(this.transferRequest).subscribe({
      next: () => {
        this.closeTransferModal();
        this.dialogService.toast('Inter-warehouse stock transfer executed.', 'success');
      },
      error: (err) => {
        this.dialogService.alert('Error', err?.error?.message || 'Could not execute stock transfer.', 'danger');
      }
    });
  }

  openAddBatchModal() {
    if (this.warehouses().length > 0) {
      this.newBatch.warehouseId = this.warehouses()[0].id;
    }
    this.isBatchModalOpen = true;
  }

  closeAddBatchModal() {
    this.isBatchModalOpen = false;
  }

  async saveBatch() {
    if (!this.newBatch.productId) {
      await this.dialogService.alert('Missing Field', 'Please select a product.', 'warning');
      return;
    }
    if (!this.newBatch.batchNumber.trim()) {
      await this.dialogService.alert('Missing Field', 'Batch or lot number is required.', 'warning');
      return;
    }

    this.invService.createBatch(this.newBatch).subscribe({
      next: () => {
        this.closeAddBatchModal();
        this.dialogService.toast('Stock batch registered successfully.', 'success');
      },
      error: (err) => {
        this.dialogService.alert('Error', err?.error?.message || 'Could not register batch.', 'danger');
      }
    });
  }

  openAddWarehouseModal() {
    this.newWarehouse = {
      name: '',
      code: '',
      address: '',
      city: '',
      stateCode: '24',
      isDefault: false
    };
    this.isWarehouseModalOpen = true;
  }

  closeAddWarehouseModal() {
    this.isWarehouseModalOpen = false;
  }

  async saveWarehouse() {
    if (!this.newWarehouse.name.trim() || !this.newWarehouse.code.trim()) {
      await this.dialogService.alert('Missing Field', 'Warehouse name and code are required.', 'warning');
      return;
    }

    this.invService.createWarehouse(this.newWarehouse).subscribe({
      next: () => {
        this.closeAddWarehouseModal();
        this.dialogService.toast('Warehouse location created successfully.', 'success');
      },
      error: (err) => {
        this.dialogService.alert('Error', err?.error?.message || 'Could not create warehouse.', 'danger');
      }
    });
  }
}

