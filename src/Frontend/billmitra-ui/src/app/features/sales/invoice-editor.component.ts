import { Component, Input, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  SalesService,
  CreateOrUpdateSalesDocumentRequest,
  SalesDocumentItemInput,
  CalculateGstRequest,
  CalculateGstResponse,
  GstSupplyType,
  DocumentType
} from '../../core/services/sales.service';
import { MastersService, PartyDto, ProductDto } from '../../core/services/masters.service';
import { SetupService } from '../../core/services/setup.service';
import { DialogService } from '../../shared/services/dialog.service';
import {
  BmButtonDirective,
  BmPanelDirective,
  BmPanelHeaderDirective,
  BmPanelBodyDirective,
  BmTableDirective,
  BmTableHeaderCellDirective,
  BmTableCellDirective,
  BmInputDirective,
  BmSelectDirective,
  BmBadgeDirective
} from '../../shared/directives';

interface InvoiceLineItem extends SalesDocumentItemInput {
  taxableAmount?: number;
  taxAmount?: number;
  totalAmount?: number;
}

@Component({
  selector: 'app-invoice-editor',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BmButtonDirective,
    BmPanelDirective,
    BmPanelHeaderDirective,
    BmPanelBodyDirective,
    BmTableDirective,
    BmTableHeaderCellDirective,
    BmTableCellDirective,
    BmInputDirective,
    BmSelectDirective,
    BmBadgeDirective
  ],
  template: `
    <div class="bm-page-container">
      <div class="bm-page-header">
        <div class="bm-header-left">
          <button bmButton="outline" bmSize="sm" (click)="navigateBack.emit()">
            <i class="pi pi-arrow-left"></i> Back to Invoices
          </button>
          <div>
            <h1 class="bm-page-title">
              {{ editingId ? 'Edit Draft Invoice' : 'New Sales Invoice / Billing' }}
            </h1>
            <p class="bm-page-subtitle">Rule 46 compliant statutory billing for Goods and Services</p>
          </div>
        </div>
        <div class="bm-header-actions">
          <button bmButton="secondary" [disabled]="isSaving()" (click)="saveDraft()">
            <i class="pi pi-save"></i> Save Draft
          </button>
          <button bmButton="primary" [disabled]="isSaving() || items.length === 0" (click)="finalizeAndIssue()">
            <i class="pi pi-check-circle"></i> Finalize & Issue
          </button>
        </div>
      </div>

      <!-- Main Billing Form -->
      <div class="bm-editor-grid">
        <!-- Customer & Document Header Details -->
        <div bmPanel [bmBordered]="true" class="bm-form-panel">
          <div bmPanelHeader>
            <span><i class="pi pi-user"></i> Customer & Invoice Particulars</span>
            <span bmBadge="info">
              {{ calculation?.supplyType === 1 ? 'Intra-State (CGST + SGST)' : 'Inter-State (IGST)' }}
            </span>
          </div>
          <div bmPanelBody class="bm-details-grid">
            <!-- Customer Picker -->
            <div class="bm-form-group">
              <label class="bm-form-label">Select Customer / Party *</label>
              <select bmSelect [(ngModel)]="selectedPartyId" (ngModelChange)="onPartySelected($event)">
                <option [ngValue]="null">-- Select Existing Customer or Type Below --</option>
                @for (party of mastersService.parties(); track party.id) {
                  <option [ngValue]="party.id">{{ party.name }} ({{ party.city }})</option>
                }
              </select>
            </div>

            <!-- Customer Legal Name -->
            <div class="bm-form-group">
              <label class="bm-form-label">Customer Legal Name *</label>
              <input bmInput type="text" [(ngModel)]="form.customerName" placeholder="Recipient Business Name" />
            </div>

            <!-- Customer GSTIN -->
            <div class="bm-form-group">
              <label class="bm-form-label">Customer GSTIN (15 Chars)</label>
              <input
                bmInput
                type="text"
                maxlength="15"
                [(ngModel)]="form.customerGstin"
                (ngModelChange)="onGstinChanged($event)"
                placeholder="24AAACR5055K1Z8"
                style="text-transform: uppercase;"
              />
            </div>

            <!-- Document Type -->
            <div class="bm-form-group">
              <label class="bm-form-label">Document Type *</label>
              <select bmSelect [(ngModel)]="form.documentType" (ngModelChange)="recalculate()">
                <option [ngValue]="1">Tax Invoice (B2B / B2C)</option>
                <option [ngValue]="2">Bill of Supply (Exempt / Composition)</option>
                <option [ngValue]="3">Quotation / Estimate</option>
                <option [ngValue]="4">Proforma Invoice</option>
                <option [ngValue]="5">Credit Note</option>
                <option [ngValue]="6">Debit Note</option>
              </select>
            </div>

            <!-- Document Date -->
            <div class="bm-form-group">
              <label class="bm-form-label">Document Date *</label>
              <input bmInput type="date" [(ngModel)]="form.documentDate" />
            </div>

            <!-- Due Date -->
            <div class="bm-form-group">
              <label class="bm-form-label">Payment Due Date</label>
              <input bmInput type="date" [(ngModel)]="form.dueDate" />
            </div>

            <!-- Place of Supply -->
            <div class="bm-form-group">
              <label class="bm-form-label">Place of Supply (State Code) *</label>
              <select bmSelect [(ngModel)]="form.placeOfSupplyStateCode" (ngModelChange)="recalculate()">
                <option *ngFor="let s of stateOptions" [value]="s.code">
                  {{ s.code }} - {{ s.name }}
                </option>
              </select>
            </div>

            <!-- Billing Address -->
            <div class="bm-form-group" style="grid-column: span 2;">
              <label class="bm-form-label">Billing Address</label>
              <input bmInput type="text" [(ngModel)]="form.billingAddress" placeholder="Street, Area, City, Pin" />
            </div>

            <!-- Reverse Charge (RCM) -->
            <div class="bm-form-group-checkbox">
              <label class="bm-checkbox-label">
                <input type="checkbox" [(ngModel)]="form.isReverseCharge" (ngModelChange)="recalculate()" />
                <span>Reverse Charge Mechanism (RCM) Applicable</span>
              </label>
            </div>
          </div>
        </div>

        <!-- Line Items Table -->
        <div bmPanel [bmBordered]="true" class="bm-items-panel">
          <div bmPanelHeader>
            <span><i class="pi pi-list"></i> Invoice Line Items</span>
            <button bmButton="secondary" bmSize="sm" (click)="addLineItem()">
              <i class="pi pi-plus"></i> Add Item Row
            </button>
          </div>
          <div bmPanelBody class="bm-items-table-container">
            <table bmTable [bmCompact]="true">
              <thead>
                <tr>
                  <th bmHeaderCell style="width: 30px;">#</th>
                  <th bmHeaderCell style="width: 200px;">Product / Quick Pick</th>
                  <th bmHeaderCell style="width: 220px;">Item Description *</th>
                  <th bmHeaderCell style="width: 100px;">HSN/SAC *</th>
                  <th bmHeaderCell style="width: 80px;" class="bm-align-right">Qty *</th>
                  <th bmHeaderCell style="width: 90px;">Unit</th>
                  <th bmHeaderCell style="width: 110px;" class="bm-align-right">Rate (₹) *</th>
                  <th bmHeaderCell style="width: 80px;" class="bm-align-right">Disc%</th>
                  <th bmHeaderCell style="width: 110px;">GST Rate</th>
                  <th bmHeaderCell style="width: 110px;" class="bm-align-right">Taxable (₹)</th>
                  <th bmHeaderCell style="width: 100px;" class="bm-align-right">Tax (₹)</th>
                  <th bmHeaderCell style="width: 120px;" class="bm-align-right">Total (₹)</th>
                  <th bmHeaderCell style="width: 40px;"></th>
                </tr>
              </thead>
              <tbody>
                @if (items.length === 0) {
                  <tr>
                    <td colspan="13" class="bm-empty-cell">
                      No items added yet. Click <strong>"Add Item Row"</strong> or select a product above.
                    </td>
                  </tr>
                } @else {
                  @for (item of items; track $index; let idx = $index) {
                    <tr>
                      <td bmCell>{{ idx + 1 }}</td>
                      <td bmCell>
                        <select
                          bmSelect
                          bmSize="sm"
                          [ngModel]="item.productId"
                          (ngModelChange)="onProductPicked(idx, $event)"
                        >
                          <option [ngValue]="null">-- Pick Master --</option>
                          @for (p of mastersService.products(); track p.id) {
                            <option [ngValue]="p.id">{{ p.name }} (₹{{ p.salePrice }})</option>
                          }
                        </select>
                      </td>
                      <td bmCell>
                        <input
                          bmInput
                          bmSize="sm"
                          type="text"
                          [(ngModel)]="item.itemName"
                          (ngModelChange)="recalculate()"
                          placeholder="Item Name"
                        />
                      </td>
                      <td bmCell>
                        <input
                          bmInput
                          bmSize="sm"
                          type="text"
                          [(ngModel)]="item.hsnSacCode"
                          placeholder="HSN"
                        />
                      </td>
                      <td bmCell class="bm-align-right">
                        <input
                          bmInput
                          bmSize="sm"
                          type="number"
                          min="0.001"
                          [(ngModel)]="item.quantity"
                          (ngModelChange)="recalculate()"
                          style="text-align: right;"
                        />
                      </td>
                      <td bmCell>
                        <select bmSelect bmSize="sm" [(ngModel)]="item.unitCode">
                          <option value="PCS">PCS</option>
                          <option value="BOX">BOX</option>
                          <option value="KGS">KGS</option>
                          <option value="MTR">MTR</option>
                          <option value="NOS">NOS</option>
                          <option value="BAG">BAG</option>
                          <option value="SET">SET</option>
                          <option value="LTR">LTR</option>
                        </select>
                      </td>
                      <td bmCell class="bm-align-right">
                        <input
                          bmInput
                          bmSize="sm"
                          type="number"
                          min="0"
                          [(ngModel)]="item.unitPrice"
                          (ngModelChange)="recalculate()"
                          style="text-align: right;"
                        />
                      </td>
                      <td bmCell class="bm-align-right">
                        <input
                          bmInput
                          bmSize="sm"
                          type="number"
                          min="0"
                          max="100"
                          [(ngModel)]="item.discountPercent"
                          (ngModelChange)="recalculate()"
                          style="text-align: right;"
                        />
                      </td>
                      <td bmCell>
                        <select
                          bmSelect
                          bmSize="sm"
                          [(ngModel)]="item.gstRatePercent"
                          (ngModelChange)="recalculate()"
                        >
                          <option [ngValue]="0">0%</option>
                          <option [ngValue]="0.1">0.1%</option>
                          <option [ngValue]="0.25">0.25%</option>
                          <option [ngValue]="1.5">1.5%</option>
                          <option [ngValue]="3">3%</option>
                          <option [ngValue]="5">5%</option>
                          <option [ngValue]="12">12%</option>
                          <option [ngValue]="18">18%</option>
                          <option [ngValue]="28">28%</option>
                        </select>
                      </td>
                      <td bmCell class="bm-align-right">
                        ₹{{ item.taxableAmount || 0 | number:'1.2-2' }}
                      </td>
                      <td bmCell class="bm-align-right">
                        ₹{{ item.taxAmount || 0 | number:'1.2-2' }}
                      </td>
                      <td bmCell class="bm-align-right">
                        <strong>₹{{ item.totalAmount || 0 | number:'1.2-2' }}</strong>
                      </td>
                      <td bmCell>
                        <button
                          class="bm-remove-row-btn"
                          (click)="removeLineItem(idx)"
                          title="Remove Row"
                        >
                          <i class="pi pi-trash"></i>
                        </button>
                      </td>
                    </tr>
                  }
                }
              </tbody>
            </table>
          </div>
        </div>

        <!-- Notes, Terms & Real-Time Financial Totals Breakdown -->
        <div class="bm-totals-grid">
          <!-- Notes & Terms -->
          <div bmPanel [bmBordered]="true" class="bm-notes-panel">
            <div bmPanelHeader>
              <span>Notes & Terms</span>
            </div>
            <div bmPanelBody>
              <div class="bm-form-group">
                <label class="bm-form-label">Customer Notes</label>
                <input bmInput type="text" [(ngModel)]="form.notes" placeholder="e.g. Thanks for your business" />
              </div>
              <div class="bm-form-group" style="margin-top: 0.75rem;">
                <label class="bm-form-label">Terms & Conditions</label>
                <textarea bmInput rows="2" [(ngModel)]="form.termsAndConditions"></textarea>
              </div>
            </div>
          </div>

          <!-- Live Financial Totals Card -->
          <div bmPanel [bmBordered]="true" class="bm-summary-card">
            <div bmPanelHeader>
              <span>Invoice Financial Summary</span>
            </div>
            <div bmPanelBody class="bm-summary-body">
              <div class="bm-summary-row">
                <span>Taxable Subtotal:</span>
                <span>₹{{ calculation?.subTotalTaxable || 0 | number:'1.2-2' }}</span>
              </div>

              @if (calculation && calculation.supplyType === 1) {
                <div class="bm-summary-row">
                  <span>CGST Total:</span>
                  <span>₹{{ calculation.cgstTotal | number:'1.2-2' }}</span>
                </div>
                <div class="bm-summary-row">
                  <span>SGST Total:</span>
                  <span>₹{{ calculation.sgstTotal | number:'1.2-2' }}</span>
                </div>
              } @else if (calculation && calculation.supplyType === 2) {
                <div class="bm-summary-row">
                  <span>IGST Total:</span>
                  <span>₹{{ calculation.igstTotal | number:'1.2-2' }}</span>
                </div>
              }

              @if (calculation && calculation.roundOffAmount !== 0) {
                <div class="bm-summary-row">
                  <span>Round Off:</span>
                  <span>₹{{ calculation.roundOffAmount | number:'1.2-2' }}</span>
                </div>
              }

              <div class="bm-grand-total-row">
                <div class="bm-grand-label">GRAND TOTAL:</div>
                <div class="bm-grand-amount">₹{{ calculation?.grandTotal || 0 | number:'1.2-2' }}</div>
              </div>

              <div class="bm-words-row">
                <strong>In Words:</strong> {{ calculation?.totalAmountInWords || 'Rupees Zero Only' }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bm-page-container {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
      height: 100%;
      overflow-y: auto;
      padding-bottom: 2rem;
    }
    .bm-page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .bm-header-left {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .bm-header-actions {
      display: flex;
      gap: 0.75rem;
    }
    .bm-page-title {
      font-size: 1.35rem;
      font-weight: 700;
      color: var(--bm-text-primary);
    }
    .bm-page-subtitle {
      font-size: 0.85rem;
      color: var(--bm-text-secondary);
    }
    .bm-editor-grid {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }
    .bm-details-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
    }
    .bm-form-group {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }
    .bm-form-label {
      font-size: 0.8rem;
      font-weight: 600;
      color: var(--bm-text-secondary);
    }
    .bm-form-group-checkbox {
      grid-column: span 2;
      display: flex;
      align-items: center;
      padding-top: 1.25rem;
    }
    .bm-checkbox-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.85rem;
      cursor: pointer;
    }
    .bm-items-table-container {
      overflow-x: auto;
      padding: 0 !important;
    }
    .bm-remove-row-btn {
      background: transparent;
      border: none;
      color: var(--bm-danger);
      cursor: pointer;
      font-size: 0.9rem;
      padding: 0.25rem;
      border-radius: 4px;
    }
    .bm-remove-row-btn:hover {
      background: #FEE2E2;
    }
    .bm-empty-cell {
      text-align: center;
      padding: 2rem !important;
      color: var(--bm-text-muted);
    }
    .bm-totals-grid {
      display: grid;
      grid-template-columns: 1fr 420px;
      gap: 1.25rem;
    }
    .bm-summary-body {
      display: flex;
      flex-direction: column;
      gap: 0.6rem;
    }
    .bm-summary-row {
      display: flex;
      justify-content: space-between;
      font-size: 0.875rem;
      color: var(--bm-text-secondary);
      padding-bottom: 0.35rem;
      border-bottom: 1px dashed var(--bm-border-subtle);
    }
    .bm-grand-total-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: #EFF6FF;
      border: 1px solid #BFDBFE;
      padding: 0.75rem 1rem;
      border-radius: 6px;
      margin-top: 0.5rem;
    }
    .bm-grand-label {
      font-weight: 700;
      font-size: 1rem;
      color: #1E3A8A;
    }
    .bm-grand-amount {
      font-weight: 800;
      font-size: 1.25rem;
      color: #1E3A8A;
    }
    .bm-words-row {
      font-size: 0.8rem;
      color: #475569;
      background: #F8FAFC;
      padding: 0.5rem 0.75rem;
      border-radius: 4px;
      line-height: 1.4;
    }
  `]
})
export class InvoiceEditorComponent implements OnInit {
  @Input() editingId: string | null = null;
  navigateBack = output<void>();
  invoiceSaved = output<void>();

  isSaving = signal(false);
  selectedPartyId: string | null = null;
  calculation: CalculateGstResponse | null = null;

  supplierStateCode = '24'; // Default to Gujarat, loaded from company

  form: CreateOrUpdateSalesDocumentRequest = {
    documentType: DocumentType.TaxInvoice,
    documentDate: new Date().toISOString().substring(0, 10),
    dueDate: new Date(Date.now() + 30 * 86400000).toISOString().substring(0, 10),
    partyId: null,
    customerName: '',
    customerGstin: '',
    billingAddress: '',
    shippingAddress: '',
    placeOfSupplyStateCode: '24',
    isReverseCharge: false,
    notes: 'Thank you for your business.',
    termsAndConditions: 'Payment due within 30 days. Late interest @ 18% p.a.',
    items: []
  };

  items: InvoiceLineItem[] = [];

  stateOptions = [
    { code: '01', name: 'Jammu & Kashmir' },
    { code: '02', name: 'Himachal Pradesh' },
    { code: '03', name: 'Punjab' },
    { code: '04', name: 'Chandigarh' },
    { code: '05', name: 'Uttarakhand' },
    { code: '06', name: 'Haryana' },
    { code: '07', name: 'Delhi' },
    { code: '08', name: 'Rajasthan' },
    { code: '09', name: 'Uttar Pradesh' },
    { code: '10', name: 'Bihar' },
    { code: '19', name: 'West Bengal' },
    { code: '23', name: 'Madhya Pradesh' },
    { code: '24', name: 'Gujarat' },
    { code: '27', name: 'Maharashtra' },
    { code: '29', name: 'Karnataka' },
    { code: '32', name: 'Kerala' },
    { code: '33', name: 'Tamil Nadu' },
    { code: '36', name: 'Telangana' }
  ];

  constructor(
    private salesService: SalesService,
    public mastersService: MastersService,
    private setupService: SetupService,
    private dialogService: DialogService
  ) {}

  ngOnInit(): void {
    this.mastersService.loadParties(1).subscribe(); // Load Customers
    this.mastersService.loadProducts().subscribe();

    const status = this.setupService.setupStatus();
    if (status?.stateCode) {
      this.supplierStateCode = status.stateCode;
      if (!this.editingId) {
        this.form.placeOfSupplyStateCode = status.stateCode;
      }
    }

    if (this.editingId) {
      this.loadExistingDocument(this.editingId);
    } else {
      this.addLineItem(); // Initial line item row
    }
  }

  loadExistingDocument(id: string): void {
    this.salesService.getDocumentById(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          const doc = res.data;
          this.form = {
            documentType: doc.documentType,
            documentDate: doc.documentDate.substring(0, 10),
            dueDate: doc.dueDate ? doc.dueDate.substring(0, 10) : null,
            partyId: doc.partyId,
            customerName: doc.customerName,
            customerGstin: doc.customerGstin,
            billingAddress: doc.billingAddress,
            shippingAddress: doc.shippingAddress,
            placeOfSupplyStateCode: doc.placeOfSupplyStateCode,
            isReverseCharge: doc.isReverseCharge,
            notes: doc.notes,
            termsAndConditions: doc.termsAndConditions,
            items: []
          };
          this.selectedPartyId = doc.partyId || null;

          this.items = doc.items.map(i => ({
            productId: i.productId,
            itemName: i.itemName,
            hsnSacCode: i.hsnSacCode,
            quantity: i.quantity,
            unitCode: i.unitCode,
            unitPrice: i.unitPrice,
            discountPercent: i.discountPercent,
            gstRatePercent: i.gstRatePercent,
            cessRatePercent: i.cessRatePercent,
            taxableAmount: i.taxableAmount,
            taxAmount: i.cgstAmount + i.sgstAmount + i.igstAmount + i.cessAmount,
            totalAmount: i.totalAmount
          }));

          this.recalculate();
        }
      }
    });
  }

  addLineItem(): void {
    this.items.push({
      productId: null,
      itemName: '',
      hsnSacCode: '',
      quantity: 1,
      unitCode: 'PCS',
      unitPrice: 0,
      discountPercent: 0,
      gstRatePercent: 18,
      taxableAmount: 0,
      taxAmount: 0,
      totalAmount: 0
    });
  }

  removeLineItem(index: number): void {
    this.items.splice(index, 1);
    this.recalculate();
  }

  onProductPicked(lineIndex: number, productId: string | null): void {
    if (!productId) return;
    const prod = this.mastersService.products().find(p => p.id === productId);
    if (!prod) return;

    const line = this.items[lineIndex];
    line.productId = prod.id;
    line.itemName = prod.name;
    line.hsnSacCode = prod.hsnSacCode;
    line.unitCode = prod.unitCode;
    line.unitPrice = prod.salePrice;
    line.gstRatePercent = prod.gstRatePercent;

    this.recalculate();
  }

  onPartySelected(partyId: string | null): void {
    if (!partyId) return;
    const party = this.mastersService.parties().find(p => p.id === partyId);
    if (!party) return;

    this.form.partyId = party.id;
    this.form.customerName = party.name;
    this.form.customerGstin = party.gstin || '';
    this.form.billingAddress = `${party.addressLine1}, ${party.city} - ${party.pincode}`;
    this.form.shippingAddress = this.form.billingAddress;
    this.form.placeOfSupplyStateCode = party.stateCode;

    this.recalculate();
  }

  onGstinChanged(gstin: string): void {
    if (gstin && gstin.length >= 2) {
      const code = gstin.substring(0, 2);
      if (this.stateOptions.some(s => s.code === code)) {
        this.form.placeOfSupplyStateCode = code;
      }
    }
    this.recalculate();
  }

  recalculate(): void {
    if (this.items.length === 0) {
      this.calculation = null;
      return;
    }

    const calcReq: CalculateGstRequest = {
      supplierStateCode: this.supplierStateCode,
      placeOfSupplyStateCode: this.form.placeOfSupplyStateCode,
      isReverseCharge: this.form.isReverseCharge,
      items: this.items.map(i => ({
        productId: i.productId,
        itemName: i.itemName,
        hsnSacCode: i.hsnSacCode,
        quantity: i.quantity || 0,
        unitCode: i.unitCode,
        unitPrice: i.unitPrice || 0,
        discountPercent: i.discountPercent || 0,
        gstRatePercent: i.gstRatePercent || 0
      }))
    };

    this.salesService.calculateGst(calcReq).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.calculation = res.data;
          // Sync lines
          res.data.items.forEach((ci, idx) => {
            if (this.items[idx]) {
              this.items[idx].taxableAmount = ci.taxableAmount;
              this.items[idx].taxAmount = ci.cgstAmount + ci.sgstAmount + ci.igstAmount + ci.cessAmount;
              this.items[idx].totalAmount = ci.totalAmount;
            }
          });
        }
      }
    });
  }

  async saveDraft(): Promise<void> {
    if (!(await this.validateForm())) return;

    this.isSaving.set(true);
    this.form.items = this.items.map(i => ({
      productId: i.productId,
      itemName: i.itemName,
      hsnSacCode: i.hsnSacCode,
      quantity: i.quantity,
      unitCode: i.unitCode,
      unitPrice: i.unitPrice,
      discountPercent: i.discountPercent,
      gstRatePercent: i.gstRatePercent
    }));

    const obs$ = this.editingId
      ? this.salesService.updateDraft(this.editingId, this.form)
      : this.salesService.createDraft(this.form);

    obs$.subscribe({
      next: (res) => {
        this.isSaving.set(false);
        if (res.success) {
          this.dialogService.toast('Draft invoice saved successfully.', 'success');
          this.invoiceSaved.emit();
        }
      },
      error: (err) => {
        this.isSaving.set(false);
        this.dialogService.alert('Save Failed', err.error?.message || 'Failed to save draft invoice.', 'danger');
      }
    });
  }

  async finalizeAndIssue(): Promise<void> {
    if (!(await this.validateForm())) return;

    const confirmed = await this.dialogService.confirm(
      'Finalize Invoice',
      'Statutory Notice: Finalizing will permanently allocate a consecutive invoice number (Rule 46) and create an immutable record. Proceed?',
      'Finalize & Issue',
      false,
      'warning'
    );
    if (!confirmed) {
      return;
    }

    this.isSaving.set(true);
    this.form.items = this.items.map(i => ({
      productId: i.productId,
      itemName: i.itemName,
      hsnSacCode: i.hsnSacCode,
      quantity: i.quantity,
      unitCode: i.unitCode,
      unitPrice: i.unitPrice,
      discountPercent: i.discountPercent,
      gstRatePercent: i.gstRatePercent
    }));

    // First ensure draft is saved, then finalize
    const save$ = this.editingId
      ? this.salesService.updateDraft(this.editingId, this.form)
      : this.salesService.createDraft(this.form);

    save$.subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.salesService.finalizeDocument(res.data.id).subscribe({
            next: async (finRes) => {
              this.isSaving.set(false);
              if (finRes.success) {
                await this.dialogService.alert(
                  'Invoice Issued',
                  `Invoice successfully finalized and issued! Statutory Number: ${finRes.data.documentNumber}`,
                  'success'
                );
                this.invoiceSaved.emit();
              }
            },
            error: (finErr) => {
              this.isSaving.set(false);
              this.dialogService.alert('Finalization Failed', finErr.error?.message || 'Failed to finalize invoice.', 'danger');
            }
          });
        }
      },
      error: (err) => {
        this.isSaving.set(false);
        this.dialogService.alert('Save Failed', err.error?.message || 'Failed to save draft before finalization.', 'danger');
      }
    });
  }

  private async validateForm(): Promise<boolean> {
    if (!this.form.customerName.trim()) {
      await this.dialogService.alert('Missing Customer', 'Please enter or select a customer name.', 'warning');
      return false;
    }
    if (!this.form.placeOfSupplyStateCode) {
      await this.dialogService.alert('Missing Place of Supply', 'Place of Supply state code is mandatory for GST calculation.', 'warning');
      return false;
    }
    if (this.items.length === 0) {
      await this.dialogService.alert('No Items', 'An invoice must contain at least one line item.', 'warning');
      return false;
    }
    for (let i = 0; i < this.items.length; i++) {
      const itm = this.items[i];
      if (!itm.itemName.trim()) {
        await this.dialogService.alert('Missing Description', `Line ${i + 1}: Item description cannot be empty.`, 'warning');
        return false;
      }
      if (itm.quantity <= 0) {
        await this.dialogService.alert('Invalid Quantity', `Line ${i + 1}: Quantity must be greater than zero.`, 'warning');
        return false;
      }
      if (itm.unitPrice < 0) {
        await this.dialogService.alert('Invalid Unit Price', `Line ${i + 1}: Unit price cannot be negative.`, 'warning');
        return false;
      }
    }
    return true;
  }
}
