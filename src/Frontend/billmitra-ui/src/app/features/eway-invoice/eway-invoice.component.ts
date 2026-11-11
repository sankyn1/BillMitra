import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EWayInvoiceService, EWayBillDto, EInvoiceDto } from '../../core/services/eway-invoice.service';
import { SalesService, SalesDocumentDto } from '../../core/services/sales.service';
import { DialogService } from '../../shared/services/dialog.service';
import { BmPaginationComponent } from '../../shared/components';

@Component({
  selector: 'app-eway-invoice',
  standalone: true,
  imports: [CommonModule, FormsModule, BmPaginationComponent],
  template: `
    <div class="eway-container">
      <!-- Header -->
      <div class="eway-header">
        <div>
          <h2>E-Way Bill & E-Invoice Management</h2>
          <p class="subtitle">Statutory transport permits, 12-digit E-Way bills, 64-character IRN generation & NIC IRP integration</p>
        </div>
        <div class="actions">
          <button class="btn btn-outline" (click)="refreshAll()" [disabled]="ewayService.isLoading()">
            <i class="pi pi-refresh" [class.pi-spin]="ewayService.isLoading()"></i>
            <span>Refresh</span>
          </button>
          <button class="btn btn-primary" (click)="activeTab = 'generate'">
            <i class="pi pi-plus"></i>
            <span>Generate New Permit</span>
          </button>
        </div>
      </div>

      <!-- Quick Metrics -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-icon"><i class="pi pi-truck"></i></div>
          <div class="kpi-data">
            <span class="kpi-val">{{ ewayService.ewayBills().length }}</span>
            <span class="kpi-label">Active E-Way Bills</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-icon" style="color: #6366f1; background: #e0e7ff;"><i class="pi pi-qrcode"></i></div>
          <div class="kpi-data">
            <span class="kpi-val">{{ ewayService.einvoices().length }}</span>
            <span class="kpi-label">Generated IRNs</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-icon" style="color: #10b981; background: #d1fae5;"><i class="pi pi-shield"></i></div>
          <div class="kpi-data">
            <span class="kpi-val">₹ 50,000+</span>
            <span class="kpi-label">Mandatory E-Way Threshold</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-icon" style="color: #f59e0b; background: #fef3c7;"><i class="pi pi-clock"></i></div>
          <div class="kpi-data">
            <span class="kpi-val">24 Hours</span>
            <span class="kpi-label">Statutory Cancel Window</span>
          </div>
        </div>
      </div>

      <!-- Tabs Navigation -->
      <div class="tabs-bar">
        <button class="tab-btn" [class.active]="activeTab === 'eway'" (click)="activeTab = 'eway'">
          <i class="pi pi-truck"></i>
          <span>E-Way Bills Register ({{ ewayService.ewayBills().length }})</span>
        </button>
        <button class="tab-btn" [class.active]="activeTab === 'einvoice'" (click)="activeTab = 'einvoice'">
          <i class="pi pi-qrcode"></i>
          <span>E-Invoices (IRN) Register ({{ ewayService.einvoices().length }})</span>
        </button>
        <button class="tab-btn" [class.active]="activeTab === 'generate'" (click)="activeTab = 'generate'">
          <i class="pi pi-file-edit"></i>
          <span>Eligible Sales Invoices</span>
        </button>
      </div>

      <!-- TAB 1: E-Way Bills Register -->
      <div *ngIf="activeTab === 'eway'" class="tab-content">
        <div class="table-card">
          <table class="data-table" *ngIf="ewayService.ewayBills().length > 0; else emptyEwb">
            <thead>
              <tr>
                <th>E-Way Bill #</th>
                <th>Doc #</th>
                <th>Customer / Consignee</th>
                <th>Consignment Val</th>
                <th>Vehicle #</th>
                <th>Valid Until</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let b of paginatedEwb">
                <td class="font-mono font-bold">{{ b.ewayBillNumber }}</td>
                <td class="font-mono">{{ b.docNumber }}</td>
                <td>
                  <div class="fw-600">{{ b.toName }}</div>
                  <div class="sub-text">GSTIN: {{ b.toGstin || 'URP' }}</div>
                </td>
                <td class="font-bold">₹ {{ b.totalInvoiceValue | number:'1.2-2' }}</td>
                <td>
                  <span class="badge" *ngIf="b.vehicleNo; else noVeh">{{ b.vehicleNo }}</span>
                  <ng-template #noVeh><span class="badge badge-warning">Part-A Only</span></ng-template>
                </td>
                <td>{{ b.validUntil | date:'dd-MMM-yyyy HH:mm' }}</td>
                <td>
                  <span class="badge" [class.badge-success]="b.status === 1 || b.status === 2" [class.badge-danger]="b.status === 3">
                    {{ b.statusName }}
                  </span>
                </td>
                <td>
                  <div class="action-btn-group">
                    <button class="btn-icon" title="Update Vehicle / Part-B" *ngIf="b.status !== 3" (click)="openUpdateVehicleDialog(b)">
                      <i class="pi pi-car"></i>
                    </button>
                    <button class="btn-icon text-danger" title="Cancel E-Way Bill" *ngIf="b.canCancel" (click)="openCancelEwbDialog(b)">
                      <i class="pi pi-times-circle"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
          <bm-pagination
            *ngIf="ewayService.ewayBills().length > 0"
            [totalItems]="ewayService.ewayBills().length"
            [(currentPage)]="ewbPage"
            [(pageSize)]="ewbPageSize"
            itemName="e-way bills">
          </bm-pagination>
          <ng-template #emptyEwb>
            <div class="empty-state">
              <i class="pi pi-truck" style="font-size: 3rem; color: #94a3b8;"></i>
              <p>No E-Way Bills generated yet. Choose an invoice under "Eligible Sales Invoices" to generate your first permit.</p>
            </div>
          </ng-template>
        </div>
      </div>

      <!-- TAB 2: E-Invoices Register -->
      <div *ngIf="activeTab === 'einvoice'" class="tab-content">
        <div class="table-card">
          <table class="data-table" *ngIf="ewayService.einvoices().length > 0; else emptyEinv">
            <thead>
              <tr>
                <th>Ack #</th>
                <th>Ack Date</th>
                <th>Invoice Ref</th>
                <th>IRN (64-Char Hash)</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let inv of paginatedEinv">
                <td class="font-mono font-bold">{{ inv.ackNo }}</td>
                <td>{{ inv.ackDate | date:'dd-MMM-yyyy HH:mm' }}</td>
                <td class="font-mono">Document {{ inv.salesDocumentId | slice:0:8 }}</td>
                <td class="font-mono text-break" style="max-width: 280px; font-size: 0.75rem;">
                  {{ inv.irn }}
                </td>
                <td>
                  <span class="badge" [class.badge-success]="inv.status === 1" [class.badge-danger]="inv.status === 2">
                    {{ inv.statusName }}
                  </span>
                </td>
                <td>
                  <div class="action-btn-group">
                    <button class="btn-icon" title="Copy IRN" (click)="copyIrn(inv.irn)">
                      <i class="pi pi-copy"></i>
                    </button>
                    <button class="btn-icon text-danger" title="Cancel IRN" *ngIf="inv.canCancel" (click)="openCancelEinvDialog(inv)">
                      <i class="pi pi-times-circle"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
          <bm-pagination
            *ngIf="ewayService.einvoices().length > 0"
            [totalItems]="ewayService.einvoices().length"
            [(currentPage)]="einvPage"
            [(pageSize)]="einvPageSize"
            itemName="e-invoices">
          </bm-pagination>
          <ng-template #emptyEinv>
            <div class="empty-state">
              <i class="pi pi-qrcode" style="font-size: 3rem; color: #94a3b8;"></i>
              <p>No E-Invoices generated yet. Select a finalized B2B invoice to generate an official IRN.</p>
            </div>
          </ng-template>
        </div>
      </div>

      <!-- TAB 3: Eligible Sales Invoices -->
      <div *ngIf="activeTab === 'generate'" class="tab-content">
        <div class="table-card">
          <table class="data-table">
            <thead>
              <tr>
                <th>Invoice #</th>
                <th>Date</th>
                <th>Customer Name</th>
                <th>GSTIN</th>
                <th>Grand Total</th>
                <th>Permit Eligibility</th>
                <th>Quick Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let doc of paginatedEligibleInvoices">
                <td class="font-mono font-bold">{{ doc.documentNumber || doc.draftNumber }}</td>
                <td>{{ doc.documentDate | date:'dd-MMM-yyyy' }}</td>
                <td class="fw-600">{{ doc.customerName }}</td>
                <td>
                  <span class="badge badge-b2b" *ngIf="doc.customerGstin; else urp">
                    {{ doc.customerGstin }}
                  </span>
                  <ng-template #urp><span class="badge">Unregistered</span></ng-template>
                </td>
                <td class="font-bold">₹ {{ doc.grandTotal | number:'1.2-2' }}</td>
                <td>
                  <span class="badge badge-warning" *ngIf="doc.grandTotal > 50000">E-Way Required (&gt; ₹50k)</span>
                  <span class="badge badge-success" *ngIf="doc.customerGstin">E-Invoice Eligible</span>
                </td>
                <td>
                  <div class="action-btn-group">
                    <button class="btn-sm btn-outline" (click)="openGenerateEwbModal(doc)">
                      <i class="pi pi-truck"></i> E-Way Bill
                    </button>
                    <button class="btn-sm btn-primary" *ngIf="doc.customerGstin" (click)="generateEInvoiceDirect(doc)">
                      <i class="pi pi-qrcode"></i> Generate IRN
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
          <bm-pagination
            *ngIf="finalizedInvoices.length > 0"
            [totalItems]="finalizedInvoices.length"
            [(currentPage)]="eligiblePage"
            [(pageSize)]="eligiblePageSize"
            itemName="eligible invoices">
          </bm-pagination>
        </div>
      </div>
    </div>

    <!-- Generate E-Way Bill Modal -->
    <div class="modal-backdrop" *ngIf="showGenerateEwbModal">
      <div class="modal-card">
        <div class="modal-header">
          <h3>Generate E-Way Bill</h3>
          <button class="close-btn" (click)="showGenerateEwbModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <p class="mb-4"><strong>Invoice:</strong> {{ selectedInvoice?.documentNumber }} | <strong>Value:</strong> ₹ {{ selectedInvoice?.grandTotal | number:'1.2-2' }}</p>
          
          <div class="form-grid">
            <div class="form-group">
              <label>Transportation Mode</label>
              <select class="form-control" [(ngModel)]="ewbForm.transportationMode">
                <option [value]="1">Road</option>
                <option [value]="2">Rail</option>
                <option [value]="3">Air</option>
                <option [value]="4">Ship</option>
              </select>
            </div>

            <div class="form-group">
              <label>Vehicle Number</label>
              <input type="text" class="form-control" placeholder="e.g. GJ01AB1234" [(ngModel)]="ewbForm.vehicleNo" />
            </div>

            <div class="form-group">
              <label>Approximate Distance (in KM)</label>
              <input type="number" class="form-control" [(ngModel)]="ewbForm.approximateDistanceKm" />
            </div>

            <div class="form-group">
              <label>Transporter ID / GSTIN</label>
              <input type="text" class="form-control" placeholder="15-digit GSTIN or TRANSIN" [(ngModel)]="ewbForm.transporterId" />
            </div>

            <div class="form-group" style="grid-column: span 2;">
              <label>Transporter Name</label>
              <input type="text" class="form-control" placeholder="Logistics partner name" [(ngModel)]="ewbForm.transporterName" />
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-outline" (click)="showGenerateEwbModal = false">Cancel</button>
          <button class="btn btn-primary" (click)="confirmGenerateEwb()" [disabled]="ewayService.isLoading()">
            <i class="pi pi-check"></i> Generate E-Way Bill
          </button>
        </div>
      </div>
    </div>

    <!-- Update Vehicle Modal -->
    <div class="modal-backdrop" *ngIf="showUpdateVehicleModal">
      <div class="modal-card">
        <div class="modal-header">
          <h3>Update Vehicle / Part-B</h3>
          <button class="close-btn" (click)="showUpdateVehicleModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group mb-3">
            <label>E-Way Bill Number</label>
            <input type="text" class="form-control font-mono" [value]="selectedEwb?.ewayBillNumber" disabled />
          </div>
          <div class="form-group mb-3">
            <label>New Vehicle Number</label>
            <input type="text" class="form-control" placeholder="e.g. MH02CD5678" [(ngModel)]="updateVehForm.vehicleNo" />
          </div>
          <div class="form-group mb-3">
            <label>From Place</label>
            <input type="text" class="form-control" [(ngModel)]="updateVehForm.fromPlace" />
          </div>
          <div class="form-group mb-3">
            <label>Reason</label>
            <select class="form-control" [(ngModel)]="updateVehForm.reasonCode">
              <option value="1">Due to Breakdown</option>
              <option value="2">Due to Transhipment</option>
              <option value="3">Others</option>
            </select>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-outline" (click)="showUpdateVehicleModal = false">Cancel</button>
          <button class="btn btn-primary" (click)="confirmUpdateVehicle()" [disabled]="ewayService.isLoading()">
            Update Vehicle
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .eway-container { padding: 24px; max-width: 1400px; margin: 0 auto; }
    .eway-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
    .eway-header h2 { font-size: 1.5rem; font-weight: 800; color: #0f172a; margin: 0; }
    .subtitle { font-size: 0.88rem; color: #64748b; margin-top: 4px; }
    .actions { display: flex; gap: 12px; }

    .kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px; }
    .kpi-card { background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 18px; display: flex; align-items: center; gap: 16px; }
    .kpi-icon { width: 44px; height: 44px; border-radius: 10px; background: #dbeafe; color: #2563eb; display: flex; align-items: center; justify-content: center; font-size: 1.25rem; }
    .kpi-data { display: flex; flex-direction: column; }
    .kpi-val { font-size: 1.35rem; font-weight: 800; color: #0f172a; }
    .kpi-label { font-size: 0.75rem; text-transform: uppercase; color: #64748b; font-weight: 700; margin-top: 2px; }

    .tabs-bar { display: flex; gap: 12px; border-bottom: 2px solid #e2e8f0; margin-bottom: 20px; }
    .tab-btn { display: inline-flex; align-items: center; gap: 8px; padding: 12px 18px; font-weight: 700; font-size: 0.92rem; color: #64748b; background: transparent; border: none; border-bottom: 2px solid transparent; margin-bottom: -2px; cursor: pointer; transition: all 0.2s ease; }
    .tab-btn:hover { color: #2563eb; }
    .tab-btn.active { color: #2563eb; border-bottom-color: #2563eb; }

    .table-card { background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .data-table { width: 100%; border-collapse: collapse; text-align: left; font-size: 0.88rem; }
    .data-table th { background: #f8fafc; padding: 12px 16px; font-weight: 700; color: #475569; border-bottom: 1px solid #e2e8f0; }
    .data-table td { padding: 14px 16px; border-bottom: 1px solid #f1f5f9; color: #334155; }
    .data-table tr:hover { background: #f8faff; }

    .font-mono { font-family: 'JetBrains Mono', monospace; }
    .font-bold { font-weight: 700; }
    .fw-600 { font-weight: 600; }
    .sub-text { font-size: 0.78rem; color: #64748b; margin-top: 2px; }

    .badge { display: inline-block; padding: 3px 8px; border-radius: 20px; font-size: 0.72rem; font-weight: 700; background: #e2e8f0; color: #475569; }
    .badge-success { background: #dcfce7; color: #166534; }
    .badge-warning { background: #fef3c7; color: #92400e; }
    .badge-danger { background: #fee2e2; color: #991b1b; }
    .badge-b2b { background: #dbeafe; color: #1e40af; }

    .action-btn-group { display: flex; gap: 8px; align-items: center; }
    .btn-icon { width: 32px; height: 32px; border-radius: 8px; border: 1px solid #e2e8f0; background: #fff; display: flex; align-items: center; justify-content: center; cursor: pointer; color: #475569; }
    .btn-icon:hover { background: #f1f5f9; color: #0f172a; }
    .btn-icon.text-danger:hover { background: #fee2e2; color: #dc2626; border-color: #fca5a5; }

    .btn { display: inline-flex; align-items: center; gap: 8px; padding: 8px 16px; border-radius: 8px; font-weight: 600; font-size: 0.88rem; cursor: pointer; border: 1px solid transparent; }
    .btn-primary { background: #2563eb; color: #fff; }
    .btn-primary:hover { background: #1d4ed8; }
    .btn-outline { background: #fff; border-color: #cbd5e1; color: #334155; }
    .btn-outline:hover { background: #f8fafc; }
    .btn-sm { padding: 4px 10px; font-size: 0.8rem; }

    .empty-state { text-align: center; padding: 60px 20px; color: #64748b; }
    .empty-state p { margin-top: 12px; }

    .modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,0.5); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card { background: #fff; border-radius: 16px; width: 560px; max-width: 95vw; box-shadow: 0 20px 25px -5px rgba(0,0,0,0.1); overflow: hidden; }
    .modal-header { padding: 18px 24px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; }
    .modal-header h3 { font-size: 1.15rem; font-weight: 700; margin: 0; }
    .close-btn { background: transparent; border: none; font-size: 1.5rem; cursor: pointer; color: #94a3b8; }
    .modal-body { padding: 24px; }
    .modal-footer { padding: 16px 24px; border-top: 1px solid #e2e8f0; display: flex; justify-content: flex-end; gap: 12px; background: #f8fafc; }

    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .form-group { display: flex; flex-direction: column; gap: 6px; }
    .form-group label { font-size: 0.82rem; font-weight: 700; color: #475569; }
    .form-control { padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 0.9rem; }
    .form-control:focus { outline: none; border-color: #2563eb; box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.1); }
    .mb-3 { margin-bottom: 12px; }
    .mb-4 { margin-bottom: 16px; }
  `]
})
export class EWayInvoiceComponent implements OnInit {
  ewayService = inject(EWayInvoiceService);
  salesService = inject(SalesService);
  dialogService = inject(DialogService);

  activeTab: 'eway' | 'einvoice' | 'generate' = 'eway';
  finalizedInvoices: SalesDocumentDto[] = [];

  ewbPage = 1;
  ewbPageSize = 10;
  get paginatedEwb() {
    const start = (this.ewbPage - 1) * this.ewbPageSize;
    return this.ewayService.ewayBills().slice(start, start + this.ewbPageSize);
  }

  einvPage = 1;
  einvPageSize = 10;
  get paginatedEinv() {
    const start = (this.einvPage - 1) * this.einvPageSize;
    return this.ewayService.einvoices().slice(start, start + this.einvPageSize);
  }

  eligiblePage = 1;
  eligiblePageSize = 10;
  get paginatedEligibleInvoices() {
    const start = (this.eligiblePage - 1) * this.eligiblePageSize;
    return this.finalizedInvoices.slice(start, start + this.eligiblePageSize);
  }

  showGenerateEwbModal = false;
  selectedInvoice: SalesDocumentDto | null = null;
  ewbForm = {
    transportationMode: 1,
    vehicleNo: '',
    approximateDistanceKm: 25,
    transporterId: '',
    transporterName: ''
  };

  showUpdateVehicleModal = false;
  selectedEwb: EWayBillDto | null = null;
  updateVehForm = {
    vehicleNo: '',
    fromPlace: 'Ahmedabad',
    fromStateCode: '24',
    reasonCode: '1',
    remarks: 'Vehicle replacement'
  };

  ngOnInit() {
    this.refreshAll();
  }

  refreshAll() {
    this.ewayService.listEWayBills().subscribe();
    this.ewayService.listEInvoices().subscribe();
    this.salesService.loadDocuments(undefined, 2).subscribe(res => {
      if (res.success && res.data) {
        this.finalizedInvoices = res.data;
      }
    });
  }

  openGenerateEwbModal(doc: SalesDocumentDto) {
    this.selectedInvoice = doc;
    this.showGenerateEwbModal = true;
  }

  confirmGenerateEwb() {
    if (!this.selectedInvoice) return;

    this.ewayService.generateEWayBill({
      salesDocumentId: this.selectedInvoice.id,
      transportationMode: Number(this.ewbForm.transportationMode),
      vehicleNo: this.ewbForm.vehicleNo,
      approximateDistanceKm: Number(this.ewbForm.approximateDistanceKm),
      transporterId: this.ewbForm.transporterId,
      transporterName: this.ewbForm.transporterName,
      vehicleType: 1
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.dialogService.toast(`E-Way Bill ${res.data.ewayBillNumber} generated successfully!`, 'success');
          this.showGenerateEwbModal = false;
          this.refreshAll();
          this.activeTab = 'eway';
        }
      },
      error: (err) => {
        this.dialogService.alert('E-Way Bill Generation Error', err.error?.message || 'Failed to generate E-Way bill');
      }
    });
  }

  generateEInvoiceDirect(doc: SalesDocumentDto) {
    this.dialogService.confirm('Generate IRN', `Generate official E-Invoice (IRN) for ${doc.documentNumber}?`).then(confirmed => {
      if (confirmed) {
        this.ewayService.generateEInvoice(doc.id).subscribe({
          next: (res) => {
            if (res.success) {
              this.dialogService.toast(`IRN generated successfully! Ack: ${res.data.ackNo}`, 'success');
              this.refreshAll();
              this.activeTab = 'einvoice';
            }
          },
          error: (err) => {
            this.dialogService.alert('E-Invoice Error', err.error?.message || 'Failed to generate IRN');
          }
        });
      }
    });
  }

  openUpdateVehicleDialog(b: EWayBillDto) {
    this.selectedEwb = b;
    this.updateVehForm.vehicleNo = b.vehicleNo || '';
    this.showUpdateVehicleModal = true;
  }

  confirmUpdateVehicle() {
    if (!this.selectedEwb) return;

    this.ewayService.updateVehicle({
      ewayBillNumber: this.selectedEwb.ewayBillNumber,
      vehicleNo: this.updateVehForm.vehicleNo,
      fromPlace: this.updateVehForm.fromPlace,
      fromStateCode: this.updateVehForm.fromStateCode,
      reasonCode: this.updateVehForm.reasonCode,
      remarks: this.updateVehForm.remarks
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.dialogService.toast('Vehicle updated successfully!', 'success');
          this.showUpdateVehicleModal = false;
          this.refreshAll();
        }
      },
      error: (err) => {
        this.dialogService.alert('Update Failed', err.error?.message || 'Failed to update vehicle');
      }
    });
  }

  openCancelEwbDialog(b: EWayBillDto) {
    this.dialogService.confirm('Cancel E-Way Bill', `Are you sure you want to cancel E-Way Bill ${b.ewayBillNumber}?`).then(confirmed => {
      if (confirmed) {
        this.ewayService.cancelEWayBill({
          ewayBillNumber: b.ewayBillNumber,
          cancelReasonCode: '1',
          cancelRemarks: 'Order cancelled by customer'
        }).subscribe({
          next: (res) => {
            if (res.success) {
              this.dialogService.toast(`E-Way Bill ${b.ewayBillNumber} cancelled`, 'success');
              this.refreshAll();
            }
          },
          error: (err) => {
            this.dialogService.alert('Cancellation Failed', err.error?.message || 'Failed to cancel E-Way Bill');
          }
        });
      }
    });
  }

  openCancelEinvDialog(inv: EInvoiceDto) {
    this.dialogService.confirm('Cancel IRN', `Are you sure you want to cancel IRN ${inv.irn.substring(0, 16)}...?`).then(confirmed => {
      if (confirmed) {
        this.ewayService.cancelEInvoice({
          irn: inv.irn,
          cancelReason: 1,
          cancelRemarks: 'Customer order cancelled'
        }).subscribe({
          next: (res) => {
            if (res.success) {
              this.dialogService.toast('IRN cancelled successfully', 'success');
              this.refreshAll();
            }
          },
          error: (err) => {
            this.dialogService.alert('Cancellation Failed', err.error?.message || 'Failed to cancel IRN');
          }
        });
      }
    });
  }

  copyIrn(irn: string) {
    navigator.clipboard.writeText(irn).then(() => {
      this.dialogService.toast('IRN copied to clipboard!', 'info');
    });
  }
}
