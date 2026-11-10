import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MastersService, ProductDto, CreateOrUpdateProductRequest } from '../../core/services/masters.service';
import {
  BmButtonDirective,
  BmPanelDirective,
  BmPanelHeaderDirective,
  BmPanelBodyDirective,
  BmPanelFooterDirective,
  BmTableDirective,
  BmTableHeaderCellDirective,
  BmTableRowDirective,
  BmTableCellDirective,
  BmInputDirective,
  BmBadgeDirective
} from '../../shared/directives';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BmButtonDirective,
    BmPanelDirective,
    BmPanelHeaderDirective,
    BmPanelBodyDirective,
    BmPanelFooterDirective,
    BmTableDirective,
    BmTableHeaderCellDirective,
    BmTableRowDirective,
    BmTableCellDirective,
    BmInputDirective,
    BmBadgeDirective
  ],
  template: `
    <div class="master-container">
      <!-- Header Actions -->
      <div class="master-header">
        <div>
          <h2 class="master-title"><i class="pi pi-box mr-2"></i> Product & Service Catalogue</h2>
          <p class="master-subtitle">Manage items, HSN/SAC classification, GST tax rates, units, and stock tracking</p>
        </div>
        <div class="master-actions">
          <button bmButton bmVariant="outline" bmSize="md" (click)="openQuickSeed()">
            <i class="pi pi-plus-circle"></i> Seed Sample Items
          </button>
          <button bmButton bmVariant="primary" bmSize="md" (click)="openCreateModal()">
            <i class="pi pi-plus"></i> Add New Product (F3)
          </button>
        </div>
      </div>

      <!-- Search & Filters Toolbar -->
      <div bmPanel bmElevation="sm" class="toolbar-panel mb-3">
        <div class="toolbar-grid">
          <div class="search-wrap">
            <input bmInput type="text" [(ngModel)]="searchQuery" (ngModelChange)="onSearchChange($event)" placeholder="Search by Item Code, Name, or HSN Code..." />
          </div>
          <div class="filter-wrap">
            <select bmInput [(ngModel)]="selectedCategory" (ngModelChange)="loadProducts()">
              <option value="">All Categories</option>
              @for (cat of mastersService.categories(); track cat.id) {
                <option [value]="cat.id">{{ cat.name }}</option>
              }
            </select>
          </div>
          <div class="filter-count">
            <span bmBadge="info">{{ mastersService.products().length }} Items Active</span>
          </div>
        </div>
      </div>

      <!-- Main Products Table -->
      <div bmPanel bmElevation="sm">
        <div bmPanelBody class="p-0">
          <table bmTable [bmStriped]="true" [bmHover]="true">
            <thead>
              <tr>
                <th bmTableHeaderCell>Item Code / SKU</th>
                <th bmTableHeaderCell>Product / Service Name</th>
                <th bmTableHeaderCell>HSN / SAC</th>
                <th bmTableHeaderCell>UOM</th>
                <th bmTableHeaderCell bmAlign="right">Sale Price</th>
                <th bmTableHeaderCell bmAlign="right">Purchase Price</th>
                <th bmTableHeaderCell bmAlign="center">GST %</th>
                <th bmTableHeaderCell bmAlign="right">Current Stock</th>
                <th bmTableHeaderCell bmAlign="center">Actions</th>
              </tr>
            </thead>
            <tbody>
              @if (mastersService.products().length === 0) {
                <tr bmTableRow>
                  <td bmTableCell colspan="9" class="text-center py-5">
                    <div class="empty-state">
                      <i class="pi pi-box empty-icon"></i>
                      <h4>No Products Found</h4>
                      <p>Add your first product or click "Seed Sample Items" to load realistic Indian GST items.</p>
                      <button bmButton bmVariant="primary" bmSize="sm" (click)="openCreateModal()">
                        <i class="pi pi-plus"></i> Add Product
                      </button>
                    </div>
                  </td>
                </tr>
              } @else {
                @for (item of mastersService.products(); track item.id) {
                  <tr bmTableRow>
                    <td bmTableCell><strong>{{ item.itemCode }}</strong></td>
                    <td bmTableCell>
                      <div>{{ item.name }}</div>
                      @if (item.categoryName) {
                        <small class="text-muted">{{ item.categoryName }}</small>
                      }
                    </td>
                    <td bmTableCell><code>{{ item.hsnSacCode || 'N/A' }}</code></td>
                    <td bmTableCell><span bmBadge="neutral">{{ item.unitCode }}</span></td>
                    <td bmTableCell bmAlign="right"><strong>₹ {{ item.salePrice | number:'1.2-2' }}</strong></td>
                    <td bmTableCell bmAlign="right" class="text-muted">₹ {{ item.purchasePrice | number:'1.2-2' }}</td>
                    <td bmTableCell bmAlign="center">
                      <span bmBadge="info">{{ item.gstRatePercent }}%</span>
                    </td>
                    <td bmTableCell bmAlign="right">
                      <span [class.text-danger]="item.currentStock <= item.minStockAlert" [class.text-success]="item.currentStock > item.minStockAlert">
                        <strong>{{ item.currentStock }} {{ item.unitCode }}</strong>
                      </span>
                    </td>
                    <td bmTableCell bmAlign="center">
                      <div class="btn-group">
                        <button bmButton bmVariant="outline" bmSize="sm" (click)="editProduct(item)" title="Edit Item">
                          <i class="pi pi-pencil"></i>
                        </button>
                        <button bmButton bmVariant="danger" bmSize="sm" (click)="deleteProduct(item.id)" title="Deactivate Item">
                          <i class="pi pi-trash"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Add/Edit Modal Dialog -->
      @if (showModal()) {
        <div class="modal-backdrop">
          <div bmPanel class="modal-dialog">
            <div bmPanelHeader>
              <span><i class="pi pi-box mr-2"></i> {{ editingId() ? 'Edit Product Master' : 'Add New Product Master' }}</span>
              <button class="close-btn" (click)="closeModal()"><i class="pi pi-times"></i></button>
            </div>
            <div bmPanelBody>
              @if (modalError()) {
                <div class="alert-box error mb-3">
                  <i class="pi pi-exclamation-triangle"></i>
                  <span>{{ modalError() }}</span>
                </div>
              }
              <div class="form-grid">
                <div class="col-6">
                  <label class="form-label required">Product / Item Name</label>
                  <input bmInput type="text" [(ngModel)]="form.name" placeholder="e.g. Basmati Rice 10kg Bag" />
                </div>
                <div class="col-6">
                  <label class="form-label required">Item Code / SKU</label>
                  <input bmInput type="text" [(ngModel)]="form.itemCode" placeholder="e.g. ITEM-001" />
                </div>
                <div class="col-6">
                  <label class="form-label required">HSN / SAC Code (4, 6 or 8 digits)</label>
                  <input bmInput type="text" maxlength="8" [(ngModel)]="form.hsnSacCode" placeholder="e.g. 100630" />
                </div>
                <div class="col-6">
                  <label class="form-label required">Unit of Measurement (UOM)</label>
                  <select bmInput [(ngModel)]="form.unitCode">
                    @for (u of mastersService.units(); track u.code) {
                      <option [value]="u.code">{{ u.code }} - {{ u.name }}</option>
                    }
                  </select>
                </div>
                <div class="col-6">
                  <label class="form-label required">Sale Price (₹)</label>
                  <input bmInput type="number" step="0.01" [(ngModel)]="form.salePrice" />
                </div>
                <div class="col-6">
                  <label class="form-label">Purchase Price (₹)</label>
                  <input bmInput type="number" step="0.01" [(ngModel)]="form.purchasePrice" />
                </div>
                <div class="col-6">
                  <label class="form-label required">GST Rate Tier</label>
                  <select bmInput [(ngModel)]="form.gstRatePercent">
                    <option [ngValue]="0">0% (Nil / Exempt)</option>
                    <option [ngValue]="0.1">0.10%</option>
                    <option [ngValue]="0.25">0.25%</option>
                    <option [ngValue]="1.5">1.50%</option>
                    <option [ngValue]="3">3.00% (Gold/Jewellery)</option>
                    <option [ngValue]="5">5.00%</option>
                    <option [ngValue]="12">12.00%</option>
                    <option [ngValue]="18">18.00% (Standard Goods & Services)</option>
                    <option [ngValue]="28">28.00% (Luxury / De-merit)</option>
                  </select>
                </div>
                <div class="col-6">
                  <label class="form-label">Opening Stock Quantity</label>
                  <input bmInput type="number" step="1" [(ngModel)]="form.openingStock" />
                </div>
              </div>
            </div>
            <div bmPanelFooter>
              <div class="modal-footer-btns">
                <button bmButton bmVariant="secondary" (click)="closeModal()">Cancel</button>
                <button bmButton bmVariant="primary" (click)="saveProduct()">
                  <i class="pi pi-check"></i> Save Product
                </button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .master-container {
      padding: 1.5rem;
    }
    .master-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.25rem;
    }
    .master-title {
      font-size: 1.35rem;
      font-weight: 700;
      color: var(--bm-text-primary);
      margin: 0;
    }
    .master-subtitle {
      font-size: 0.85rem;
      color: var(--bm-text-secondary);
      margin-top: 0.2rem;
    }
    .master-actions {
      display: flex;
      gap: 0.75rem;
    }
    .toolbar-panel {
      padding: 0.75rem 1rem;
    }
    .toolbar-grid {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .search-wrap { flex: 1; }
    .filter-wrap { width: 220px; }
    .filter-count { font-weight: 600; }
    .p-0 { padding: 0 !important; }
    .mb-3 { margin-bottom: 1rem; }
    .mr-2 { margin-right: 0.5rem; }
    .btn-group { display: flex; gap: 0.35rem; justify-content: center; }
    .empty-state { text-align: center; padding: 3rem 1rem; }
    .empty-icon { font-size: 3rem; color: #94a3b8; margin-bottom: 1rem; display: block; }
    
    .modal-backdrop {
      position: fixed;
      top: 0; left: 0; width: 100%; height: 100%;
      background: rgba(15, 23, 42, 0.6);
      display: flex; align-items: center; justify-content: center;
      z-index: 1000;
    }
    .modal-dialog {
      width: 100%;
      max-width: 680px;
      background: #ffffff;
      border-radius: var(--bm-radius-lg);
      box-shadow: 0 20px 25px -5px rgba(0,0,0,0.2);
    }
    .close-btn {
      background: transparent; border: none; font-size: 1.1rem; cursor: pointer; color: #64748b;
    }
    .modal-footer-btns { display: flex; justify-content: flex-end; gap: 0.75rem; }
    .form-grid { display: grid; grid-template-columns: repeat(12, 1fr); gap: 1rem; }
    .col-6 { grid-column: span 6; }
    .form-label { display: block; font-size: 0.8rem; font-weight: 600; margin-bottom: 0.35rem; color: #334155; }
    .form-label.required::after { content: ' *'; color: #ef4444; }
    .alert-box.error { background: #fef2f2; border: 1px solid #fecaca; color: #b91c1c; padding: 0.75rem; border-radius: 6px; }
  `]
})
export class ProductListComponent implements OnInit {
  protected readonly mastersService = inject(MastersService);

  public searchQuery = '';
  public selectedCategory = '';
  public showModal = signal<boolean>(false);
  public editingId = signal<string | null>(null);
  public modalError = signal<string | null>(null);

  public form: CreateOrUpdateProductRequest = this.getEmptyForm();

  ngOnInit(): void {
    this.mastersService.loadUnits().subscribe();
    this.mastersService.loadCategories().subscribe();
    this.loadProducts();
  }

  loadProducts(): void {
    this.mastersService.loadProducts(this.searchQuery, this.selectedCategory).subscribe();
  }

  onSearchChange(val: string): void {
    this.searchQuery = val;
    this.loadProducts();
  }

  openCreateModal(): void {
    this.editingId.set(null);
    this.form = this.getEmptyForm();
    this.modalError.set(null);
    this.showModal.set(true);
  }

  editProduct(item: ProductDto): void {
    this.editingId.set(item.id);
    this.form = {
      itemCode: item.itemCode,
      name: item.name,
      description: item.description,
      itemType: item.itemType,
      categoryId: item.categoryId,
      hsnSacCode: item.hsnSacCode,
      unitCode: item.unitCode,
      purchasePrice: item.purchasePrice,
      salePrice: item.salePrice,
      gstRatePercent: item.gstRatePercent,
      cessPercent: item.cessPercent,
      isTaxInclusive: item.isTaxInclusive,
      openingStock: item.openingStock,
      minStockAlert: item.minStockAlert
    };
    this.modalError.set(null);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
  }

  saveProduct(): void {
    this.modalError.set(null);
    if (!this.form.name.trim()) {
      this.modalError.set('Product name is required.');
      return;
    }

    const id = this.editingId();
    if (id) {
      this.mastersService.updateProduct(id, this.form).subscribe({
        next: () => this.closeModal(),
        error: (err) => this.modalError.set(err?.error?.message || err?.message || 'Update failed.')
      });
    } else {
      this.mastersService.createProduct(this.form).subscribe({
        next: () => this.closeModal(),
        error: (err) => this.modalError.set(err?.error?.message || err?.message || 'Creation failed.')
      });
    }
  }

  deleteProduct(id: string): void {
    if (confirm('Are you sure you want to mark this item as inactive?')) {
      this.mastersService.deleteProduct(id).subscribe();
    }
  }

  openQuickSeed(): void {
    const sampleItems: CreateOrUpdateProductRequest[] = [
      { itemCode: 'ITEM-1001', name: 'Alphonso Mango Pulp (850g Tin)', hsnSacCode: '200899', unitCode: 'BOX', salePrice: 220.00, purchasePrice: 165.00, gstRatePercent: 12.00, cessPercent: 0, isTaxInclusive: false, openingStock: 50, minStockAlert: 10, itemType: 1 },
      { itemCode: 'ITEM-1002', name: 'Cotton Bed Sheet Double King', hsnSacCode: '630231', unitCode: 'PCS', salePrice: 1250.00, purchasePrice: 850.00, gstRatePercent: 5.00, cessPercent: 0, isTaxInclusive: false, openingStock: 30, minStockAlert: 5, itemType: 1 },
      { itemCode: 'ITEM-1003', name: 'Stainless Steel Water Bottle 1L', hsnSacCode: '732393', unitCode: 'PCS', salePrice: 450.00, purchasePrice: 280.00, gstRatePercent: 18.00, cessPercent: 0, isTaxInclusive: false, openingStock: 100, minStockAlert: 20, itemType: 1 },
      { itemCode: 'ITEM-1004', name: 'Annual Software Maintenance Service', hsnSacCode: '998314', unitCode: 'NOS', salePrice: 15000.00, purchasePrice: 0.00, gstRatePercent: 18.00, cessPercent: 0, isTaxInclusive: false, openingStock: 0, minStockAlert: 0, itemType: 2 }
    ];

    sampleItems.forEach(item => {
      this.mastersService.createProduct(item).subscribe();
    });
  }

  private getEmptyForm(): CreateOrUpdateProductRequest {
    return {
      itemCode: `ITEM-${Math.floor(1000 + Math.random() * 9000)}`,
      name: '',
      description: '',
      itemType: 1,
      hsnSacCode: '847130',
      unitCode: 'PCS',
      purchasePrice: 0,
      salePrice: 0,
      gstRatePercent: 18.00,
      cessPercent: 0,
      isTaxInclusive: false,
      openingStock: 0,
      minStockAlert: 5
    };
  }
}
