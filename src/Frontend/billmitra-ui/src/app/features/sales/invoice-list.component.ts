import { Component, OnInit, output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SalesService, SalesDocumentDto, DocumentType, DocumentStatus } from '../../core/services/sales.service';
import { DialogService } from '../../shared/services/dialog.service';
import { DocumentPreviewDialogComponent } from './document-preview-dialog.component';
import { BmPaginationComponent } from '../../shared/components';
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

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DocumentPreviewDialogComponent,
    BmPaginationComponent,
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
        <div>
          <h1 class="bm-page-title">Sales Documents & GST Invoices</h1>
          <p class="bm-page-subtitle">Create, finalize, track, and print CBIC Rule 46 compliant GST tax invoices, bills of supply, and quotations</p>
        </div>
        <div class="bm-page-actions">
          <button bmButton="primary" (click)="createNew.emit()">
            <i class="pi pi-plus"></i> Create New Invoice
          </button>
        </div>
      </div>

      <div bmPanel [bmBordered]="true" class="bm-list-panel">
        <div bmPanelHeader class="bm-filter-bar">
          <div class="bm-search-group">
            <i class="pi pi-search bm-search-icon"></i>
            <input
              bmInput
              type="text"
              placeholder="Search by customer, invoice no, or draft..."
              [(ngModel)]="searchQuery"
              (keyup.enter)="loadDocuments()"
              class="bm-search-input"
            />
          </div>

          <div class="bm-filters">
            <select bmSelect [(ngModel)]="selectedType" (ngModelChange)="loadDocuments()">
              <option [ngValue]="null">All Document Types</option>
              <option [ngValue]="1">Tax Invoice</option>
              <option [ngValue]="2">Bill of Supply</option>
              <option [ngValue]="3">Quotation / Estimate</option>
              <option [ngValue]="4">Proforma Invoice</option>
              <option [ngValue]="5">Credit Note</option>
              <option [ngValue]="6">Debit Note</option>
            </select>

            <select bmSelect [(ngModel)]="selectedStatus" (ngModelChange)="loadDocuments()">
              <option [ngValue]="null">All Statuses</option>
              <option [ngValue]="1">Drafts Only</option>
              <option [ngValue]="2">Finalized / Issued</option>
              <option [ngValue]="3">Cancelled</option>
            </select>

            <button bmButton="secondary" bmSize="sm" (click)="loadDocuments()">
              <i class="pi pi-refresh"></i> Refresh
            </button>
          </div>
        </div>

        <div bmPanelBody class="bm-table-container">
          <table bmTable [bmStriped]="true" [bmHover]="true">
            <thead>
              <tr>
                <th bmHeaderCell>Doc No / Ref</th>
                <th bmHeaderCell>Type</th>
                <th bmHeaderCell>Date</th>
                <th bmHeaderCell>Customer / Party</th>
                <th bmHeaderCell>POS</th>
                <th bmHeaderCell class="bm-align-right">Taxable (₹)</th>
                <th bmHeaderCell class="bm-align-right">Tax (₹)</th>
                <th bmHeaderCell class="bm-align-right">Grand Total (₹)</th>
                <th bmHeaderCell class="bm-align-center">Status</th>
                <th bmHeaderCell class="bm-align-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              @if (salesService.isLoading()) {
                <tr>
                  <td colspan="10" class="bm-loading-cell">
                    <i class="pi pi-spin pi-spinner"></i> Loading sales documents...
                  </td>
                </tr>
              } @else if (salesService.documents().length === 0) {
                <tr>
                  <td colspan="10" class="bm-empty-cell">
                    <div class="bm-empty-state">
                      <i class="pi pi-file-edit bm-empty-icon"></i>
                      <h3>No sales documents found</h3>
                      <p>Start billing your customers by creating a new GST tax invoice or quotation.</p>
                      <button bmButton="primary" (click)="createNew.emit()">
                        <i class="pi pi-plus"></i> Create First Invoice
                      </button>
                    </div>
                  </td>
                </tr>
              } @else {
                @for (doc of paginatedDocuments(); track doc.id) {
                  <tr>
                    <td bmCell>
                      <strong class="bm-doc-number">{{ doc.documentNumber || doc.draftNumber }}</strong>
                      @if (doc.documentNumber && doc.draftNumber) {
                        <div class="bm-subtext">Ref: {{ doc.draftNumber }}</div>
                      }
                    </td>
                    <td bmCell>
                      <span class="bm-type-pill">{{ getDocTypeName(doc.documentType) }}</span>
                    </td>
                    <td bmCell>{{ doc.documentDate | date:'dd-MMM-yyyy' }}</td>
                    <td bmCell>
                      <strong>{{ doc.customerName }}</strong>
                      @if (doc.customerGstin) {
                        <div class="bm-gstin-code">{{ doc.customerGstin }}</div>
                      }
                    </td>
                    <td bmCell>
                      <span class="bm-pos-tag">{{ doc.placeOfSupplyStateCode }}</span>
                    </td>
                    <td bmCell class="bm-align-right">₹{{ doc.subTotalTaxable | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ doc.totalTax | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">
                      <strong class="bm-grand-total">₹{{ doc.grandTotal | number:'1.2-2' }}</strong>
                    </td>
                    <td bmCell class="bm-align-center">
                      @if (doc.documentStatus === 1) {
                        <span bmBadge="warning">Draft</span>
                      } @else if (doc.documentStatus === 2) {
                        <span bmBadge="success">Finalized</span>
                      } @else if (doc.documentStatus === 3) {
                        <span bmBadge="primary">Partially Paid</span>
                      } @else if (doc.documentStatus === 4) {
                        <span bmBadge="success">Paid</span>
                      } @else if (doc.documentStatus === 5) {
                        <span bmBadge="danger">Cancelled</span>
                      } @else if (doc.documentStatus === 6) {
                        <span bmBadge="secondary">Converted</span>
                      }
                    </td>
                    <td bmCell class="bm-align-center">
                      <div class="bm-action-buttons">
                        <!-- In-App Print & Preview Dialog -->
                        <button
                          bmButton="outline"
                          bmSize="sm"
                          (click)="openPreview(doc)"
                          title="Preview, format switcher, and direct print"
                        >
                          <i class="pi pi-print"></i> Preview
                        </button>

                        <!-- Convert Proforma/Quotation to Tax Invoice -->
                        @if (canConvertToInvoice(doc)) {
                          <button
                            bmButton="primary"
                            bmSize="sm"
                            (click)="promptConvertToInvoice(doc)"
                            title="Convert to GST Tax Invoice"
                          >
                            <i class="pi pi-sync"></i> Convert
                          </button>
                        }

                        @if (doc.documentStatus === 1) {
                          <button
                            bmButton="secondary"
                            bmSize="sm"
                            (click)="editInvoice.emit(doc.id)"
                            title="Edit Draft"
                          >
                            <i class="pi pi-pencil"></i>
                          </button>
                          <button
                            bmButton="primary"
                            bmSize="sm"
                            (click)="finalizeInvoice(doc.id)"
                            title="Finalize & Allocate Statutory Number"
                          >
                            <i class="pi pi-check"></i> Finalize
                          </button>
                        } @else if (doc.documentStatus === 2 || doc.documentStatus === 3) {
                          <button
                            bmButton="danger"
                            bmSize="sm"
                            (click)="promptCancel(doc)"
                            title="Cancel Invoice"
                          >
                            <i class="pi pi-ban"></i>
                          </button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
        <bm-pagination
          [totalItems]="salesService.documents().length"
          [(currentPage)]="currentPage"
          [(pageSize)]="pageSize"
          itemName="invoices">
        </bm-pagination>
      </div>

      <!-- Cancel Confirmation Modal -->
      @if (showCancelModal()) {
        <div class="bm-modal-backdrop">
          <div bmPanel [bmBordered]="true" class="bm-modal-panel">
            <div bmPanelHeader>
              <span>Cancel Invoice: {{ cancelTarget()?.documentNumber }}</span>
              <button class="bm-close-btn" (click)="showCancelModal.set(false)">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <div bmPanelBody>
              <div class="bm-alert-warning">
                <i class="pi pi-exclamation-triangle"></i>
                <div>
                  <strong>Statutory Notice:</strong> In accordance with GST statutory recordkeeping, finalized invoice numbers are permanent and can never be reused or deleted. A cancellation audit record will be logged.
                </div>
              </div>
              <div class="bm-form-group" style="margin-top: 1rem;">
                <label class="bm-form-label">Reason for Cancellation *</label>
                <textarea
                  bmInput
                  rows="3"
                  [(ngModel)]="cancelReason"
                  placeholder="e.g. Order cancelled by buyer / incorrect item quantity"
                ></textarea>
              </div>
            </div>
            <div class="bm-modal-footer">
              <button bmButton="secondary" (click)="showCancelModal.set(false)">Close</button>
              <button bmButton="danger" [disabled]="!cancelReason.trim()" (click)="confirmCancel()">
                <i class="pi pi-ban"></i> Confirm Cancellation
              </button>
            </div>
          </div>
        </div>
      }

      <!-- Document Preview & Direct Print Dialog -->
      <app-document-preview-dialog
        [isOpen]="previewOpen()"
        [documentId]="previewDocId()"
        [documentNumber]="previewDocNumber()"
        (closed)="previewOpen.set(false)"
      ></app-document-preview-dialog>
    </div>
  `,
  styles: [`
    .bm-page-container {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
      height: 100%;
    }
    .bm-page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .bm-page-title {
      font-size: 1.35rem;
      font-weight: 700;
      color: var(--bm-text-primary);
    }
    .bm-page-subtitle {
      font-size: 0.85rem;
      color: var(--bm-text-secondary);
      margin-top: 0.2rem;
    }
    .bm-page-actions {
      display: flex;
      gap: 0.75rem;
    }
    .bm-filter-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }
    .bm-search-group {
      position: relative;
      flex: 1;
      max-width: 380px;
    }
    .bm-search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--bm-text-muted);
    }
    .bm-search-input {
      padding-left: 2.25rem !important;
    }
    .bm-filters {
      display: flex;
      gap: 0.6rem;
      align-items: center;
    }
    .bm-table-container {
      overflow-y: auto;
      max-height: calc(100vh - 270px);
      padding: 0 !important;
    }
    .bm-doc-number {
      color: var(--bm-primary);
      font-size: 0.9rem;
    }
    .bm-subtext {
      font-size: 0.75rem;
      color: var(--bm-text-muted);
    }
    .bm-type-pill {
      font-size: 0.75rem;
      font-weight: 500;
      background: #F1F5F9;
      color: #334155;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
    }
    .bm-gstin-code {
      font-size: 0.75rem;
      font-family: monospace;
      color: var(--bm-text-secondary);
    }
    .bm-pos-tag {
      font-weight: 600;
      font-size: 0.8rem;
      background: #EFF6FF;
      color: #1E3A8A;
      padding: 0.15rem 0.4rem;
      border-radius: 4px;
    }
    .bm-grand-total {
      color: #0F172A;
      font-size: 0.95rem;
    }
    .bm-action-buttons {
      display: flex;
      gap: 0.35rem;
      justify-content: center;
    }
    .bm-loading-cell, .bm-empty-cell {
      text-align: center;
      padding: 3rem 1rem !important;
      color: var(--bm-text-secondary);
    }
    .bm-empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
    }
    .bm-empty-icon {
      font-size: 2.5rem;
      color: var(--bm-text-muted);
    }
    .bm-modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.45);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      backdrop-filter: blur(2px);
    }
    .bm-modal-panel {
      width: 520px;
      max-width: 95vw;
      background: #FFFFFF;
      border-radius: 8px;
    }
    .bm-close-btn {
      background: transparent;
      border: none;
      cursor: pointer;
      color: var(--bm-text-secondary);
    }
    .bm-alert-warning {
      background-color: #FFFBEB;
      border: 1px solid #FDE68A;
      color: #92400E;
      padding: 0.75rem;
      border-radius: 6px;
      display: flex;
      gap: 0.75rem;
      font-size: 0.825rem;
      line-height: 1.4;
    }
    .bm-modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 0.5rem;
      padding: 0.75rem 1.25rem;
      border-top: 1px solid var(--bm-border-subtle);
      background-color: #F8FAFC;
    }
  `]
})
export class InvoiceListComponent implements OnInit {
  createNew = output<void>();
  editInvoice = output<string>();

  searchQuery = '';
  selectedType: number | null = null;
  selectedStatus: number | null = null;

  currentPage = signal<number>(1);
  pageSize = signal<number>(10);

  paginatedDocuments = computed(() => {
    const docs = this.salesService.documents();
    const start = (this.currentPage() - 1) * this.pageSize();
    return docs.slice(start, start + this.pageSize());
  });

  showCancelModal = signal(false);
  cancelTarget = signal<SalesDocumentDto | null>(null);
  cancelReason = '';

  previewOpen = signal<boolean>(false);
  previewDocId = signal<string>('');
  previewDocNumber = signal<string>('');

  constructor(
    public salesService: SalesService,
    private dialogService: DialogService
  ) {}

  openPreview(doc: SalesDocumentDto): void {
    this.previewDocId.set(doc.id);
    this.previewDocNumber.set(doc.documentNumber || doc.draftNumber);
    this.previewOpen.set(true);
  }

  canConvertToInvoice(doc: SalesDocumentDto): boolean {
    return (doc.documentType === DocumentType.Quotation || doc.documentType === DocumentType.ProformaInvoice) &&
           doc.documentStatus !== DocumentStatus.Cancelled &&
           doc.documentStatus !== DocumentStatus.Converted;
  }

  async promptConvertToInvoice(doc: SalesDocumentDto): Promise<void> {
    const docLabel = doc.documentNumber || doc.draftNumber;
    const ok = await this.dialogService.confirm(
      'Convert to Tax Invoice',
      `Are you sure you want to convert ${this.getDocTypeName(doc.documentType)} "${docLabel}" to an official GST Tax Invoice? All customer details, items, discounts, and taxes will be copied, and "${docLabel}" will be marked as Converted.`,
      'Convert Now',
      false,
      'info'
    );
    if (!ok) return;

    this.salesService.convertToTaxInvoice(doc.id, false).subscribe({
      next: (res) => {
        if (res.success) {
          this.dialogService.toast(`Successfully converted to Tax Invoice: ${res.data.draftNumber || res.data.documentNumber}`, 'success');
          this.loadDocuments();
        }
      },
      error: (err) => {
        this.dialogService.alert('Conversion Failed', err?.error?.message || 'Failed to convert document to invoice.', 'danger');
      }
    });
  }

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.currentPage.set(1);
    this.salesService.loadDocuments(
      this.selectedType ?? undefined,
      this.selectedStatus ?? undefined,
      this.searchQuery.trim() || undefined
    ).subscribe();
  }

  getDocTypeName(type: DocumentType): string {
    switch (type) {
      case DocumentType.TaxInvoice: return 'Tax Invoice';
      case DocumentType.BillOfSupply: return 'Bill of Supply';
      case DocumentType.Quotation: return 'Quotation';
      case DocumentType.ProformaInvoice: return 'Proforma';
      case DocumentType.CreditNote: return 'Credit Note';
      case DocumentType.DebitNote: return 'Debit Note';
      case DocumentType.DeliveryChallan: return 'Delivery Challan';
      default: return 'Sales Document';
    }
  }

  async finalizeInvoice(id: string): Promise<void> {
    const ok = await this.dialogService.confirm(
      'Finalize Invoice',
      'Finalizing will permanently assign a statutory consecutive invoice number (Rule 46) and lock the document. Proceed?',
      'Finalize & Issue',
      false,
      'warning'
    );
    if (!ok) return;

    this.salesService.finalizeDocument(id).subscribe({
      next: (res) => {
        if (res.success) {
          this.dialogService.toast(`Invoice ${res.data.documentNumber} finalized successfully!`, 'success');
          this.loadDocuments();
        }
      },
      error: (err) => this.dialogService.alert('Finalization Failed', err.error?.message || 'Failed to finalize invoice.', 'danger')
    });
  }

  promptCancel(doc: SalesDocumentDto): void {
    this.cancelTarget.set(doc);
    this.cancelReason = '';
    this.showCancelModal.set(true);
  }

  confirmCancel(): void {
    const doc = this.cancelTarget();
    if (!doc || !this.cancelReason.trim()) return;

    this.salesService.cancelDocument(doc.id, this.cancelReason.trim()).subscribe({
      next: (res) => {
        if (res.success) {
          this.showCancelModal.set(false);
          this.dialogService.toast(`Document ${doc.documentNumber} cancelled.`, 'warning');
          this.loadDocuments();
        }
      },
      error: (err) => this.dialogService.alert('Cancellation Error', err.error?.message || 'Failed to cancel invoice.', 'danger')
    });
  }

  printPdf(id: string, format: string): void {
    this.salesService.downloadPdf(id, format).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Invoice_${id.substring(0, 8)}_${format}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => this.dialogService.alert('Download Error', 'Failed to generate or download the invoice PDF.', 'danger')
    });
  }
}

