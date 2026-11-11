import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  GstService,
  Gstr1SummaryDto,
  Gstr3bSummaryDto,
  HsnSummaryItemDto,
  GstValidationResultDto,
  GstReturnFilingDto
} from '../../core/services/gst.service';
import { DialogService } from '../../shared/services/dialog.service';
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
  selector: 'app-gst-returns',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
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
    <div class="bm-gst-container">
      <!-- Page Header -->
      <div class="bm-page-header">
        <div>
          <h1 class="bm-page-title">GST Returns & Statutory Compliance</h1>
          <p class="bm-page-subtitle">Prepare, validate, export, and submit CBIC-compliant GSTR-1, GSTR-3B, and Table 12 HSN registers</p>
        </div>

        <!-- Period Selector & Global Actions -->
        <div class="bm-page-actions">
          <div class="period-picker-group">
            <span class="period-label">Tax Period:</span>
            <select bmSelect [(ngModel)]="selectedMonth" (ngModelChange)="onPeriodChange()" class="month-select">
              <option value="04">April (04)</option>
              <option value="05">May (05)</option>
              <option value="06">June (06)</option>
              <option value="07">July (07)</option>
              <option value="08">August (08)</option>
              <option value="09">September (09)</option>
              <option value="10">October (10)</option>
              <option value="11">November (11)</option>
              <option value="12">December (12)</option>
              <option value="01">January (01)</option>
              <option value="02">February (02)</option>
              <option value="03">March (03)</option>
            </select>

            <select bmSelect [(ngModel)]="selectedYear" (ngModelChange)="onPeriodChange()" class="year-select">
              <option value="2026">2026</option>
              <option value="2027">2027</option>
            </select>
          </div>

          <button bmButton bmVariant="secondary" bmSize="md" (click)="loadAllData()" [disabled]="isLoading()">
            <i class="pi pi-refresh" [class.pi-spin]="isLoading()"></i>
            <span>Refresh</span>
          </button>

          <button
            bmButton
            bmVariant="primary"
            bmSize="md"
            (click)="downloadPortalJson()"
            [disabled]="isLoading() || !gstr1Summary()"
            title="Download GST Portal Offline Tool JSON File"
          >
            <i class="pi pi-download"></i>
            <span>Download Portal JSON</span>
          </button>

          <button
            bmButton
            bmVariant="outline"
            bmSize="md"
            (click)="submitGstr1()"
            [disabled]="isSubmitting() || isLoading()"
            title="Submit to GSTN via Sandboxed GSP Interface"
          >
            <i class="pi pi-send"></i>
            <span>{{ isSubmitting() ? 'Submitting...' : 'File via GSP Sandbox' }}</span>
          </button>
        </div>
      </div>

      <!-- Compliance & Statutory Validation Box -->
      @if (validationResult()) {
        <div class="validation-banner" [ngClass]="validationResult()?.isValid ? 'banner-success' : 'banner-warning'">
          <div class="banner-icon">
            <i class="pi" [ngClass]="validationResult()?.isValid ? 'pi-check-circle' : 'pi-exclamation-triangle'"></i>
          </div>
          <div class="banner-content">
            <h4 class="banner-title">
              {{ validationResult()?.isValid ? 'Statutory Rule Compliance Verified' : 'Compliance Validation Alerts' }}
            </h4>
            @if (validationResult()?.isValid) {
              <p class="banner-text">All B2B customer GSTINs, HSN codes, and interstate tax thresholds are valid for period {{ currentTaxPeriod }}. Return payload is ready for portal upload or submission.</p>
            } @else {
              <ul class="banner-list">
                @for (err of validationResult()?.errors; track err) {
                  <li class="error-item"><i class="pi pi-times-circle text-danger"></i> {{ err }}</li>
                }
                @for (warn of validationResult()?.warnings; track warn) {
                  <li class="warn-item"><i class="pi pi-info-circle text-warning"></i> {{ warn }}</li>
                }
              </ul>
            }
          </div>
        </div>
      }

      <!-- Main Navigation Tabs -->
      <div class="bm-tabs-header">
        <button
          class="bm-tab-btn"
          [class.active]="activeTab === 'gstr1'"
          (click)="activeTab = 'gstr1'"
        >
          <i class="pi pi-file-edit"></i>
          <span>GSTR-1 Outward Supplies</span>
        </button>

        <button
          class="bm-tab-btn"
          [class.active]="activeTab === 'gstr3b'"
          (click)="activeTab = 'gstr3b'"
        >
          <i class="pi pi-chart-pie"></i>
          <span>GSTR-3B Monthly Return</span>
        </button>

        <button
          class="bm-tab-btn"
          [class.active]="activeTab === 'hsn'"
          (click)="activeTab = 'hsn'"
        >
          <i class="pi pi-box"></i>
          <span>Table 12 HSN Summary</span>
        </button>

        <button
          class="bm-tab-btn"
          [class.active]="activeTab === 'filings'"
          (click)="activeTab = 'filings'"
        >
          <i class="pi pi-history"></i>
          <span>Filing Audit Tracker</span>
        </button>
      </div>

      <!-- TAB 1: GSTR-1 OUTWARD SUPPLIES -->
      @if (activeTab === 'gstr1') {
        <!-- KPI Metric Cards -->
        <div class="bm-kpi-grid">
          <div bmPanel bmElevation="sm" class="bm-kpi-card">
            <div class="kpi-top">
              <span class="kpi-label">Table 4A: B2B Invoices</span>
              <span bmBadge="primary">{{ gstr1Summary()?.b2bCount || 0 }} Docs</span>
            </div>
            <div class="kpi-main-val">₹{{ (gstr1Summary()?.b2bTotalTaxable || 0) | number:'1.2-2' }}</div>
            <div class="kpi-meta">Tax: ₹{{ (gstr1Summary()?.b2bTotalTax || 0) | number:'1.2-2' }}</div>
          </div>

          <div bmPanel bmElevation="sm" class="bm-kpi-card">
            <div class="kpi-top">
              <span class="kpi-label">Table 5A: B2C Large (> ₹2.5L)</span>
              <span bmBadge="secondary">{{ gstr1Summary()?.b2clCount || 0 }} Docs</span>
            </div>
            <div class="kpi-main-val">₹{{ (gstr1Summary()?.b2clTotalTaxable || 0) | number:'1.2-2' }}</div>
            <div class="kpi-meta">Tax: ₹{{ (gstr1Summary()?.b2clTotalTax || 0) | number:'1.2-2' }}</div>
          </div>

          <div bmPanel bmElevation="sm" class="bm-kpi-card">
            <div class="kpi-top">
              <span class="kpi-label">Table 7: B2C Small Supplies</span>
              <span bmBadge="secondary">{{ gstr1Summary()?.b2csCount || 0 }} Docs</span>
            </div>
            <div class="kpi-main-val">₹{{ (gstr1Summary()?.b2csTotalTaxable || 0) | number:'1.2-2' }}</div>
            <div class="kpi-meta">Tax: ₹{{ (gstr1Summary()?.b2csTotalTax || 0) | number:'1.2-2' }}</div>
          </div>

          <div bmPanel bmElevation="sm" class="bm-kpi-card highlight-card">
            <div class="kpi-top">
              <span class="kpi-label font-bold text-primary">Total Tax Liability</span>
              <span bmBadge="success">Period: {{ currentTaxPeriod }}</span>
            </div>
            <div class="kpi-main-val text-primary">₹{{ (gstr1Summary()?.totalTaxLiability || 0) | number:'1.2-2' }}</div>
            <div class="kpi-meta">Taxable: ₹{{ (gstr1Summary()?.totalTaxableValue || 0) | number:'1.2-2' }}</div>
          </div>
        </div>

        <!-- Section Details -->
        <div class="gstr1-tables-stack">
          <!-- B2B Invoices Section -->
          <div bmPanel [bmBordered]="true" class="section-panel">
            <div bmPanelHeader class="section-header">
              <div class="section-title-wrap">
                <i class="pi pi-building text-primary"></i>
                <span class="section-title">Table 4A - Supplies to Registered Taxpayers (B2B)</span>
              </div>
              <span bmBadge="primary">{{ gstr1Summary()?.b2bInvoices?.length || 0 }} Invoices</span>
            </div>

            <div bmPanelBody class="table-container">
              <table bmTable [bmStriped]="true" [bmHover]="true">
                <thead>
                  <tr>
                    <th bmHeaderCell>Customer GSTIN</th>
                    <th bmHeaderCell>Customer Name</th>
                    <th bmHeaderCell>Invoice No</th>
                    <th bmHeaderCell>Date</th>
                    <th bmHeaderCell>POS</th>
                    <th bmHeaderCell class="bm-align-right">Invoice Total (₹)</th>
                    <th bmHeaderCell class="bm-align-right">Taxable (₹)</th>
                    <th bmHeaderCell class="bm-align-right">CGST (₹)</th>
                    <th bmHeaderCell class="bm-align-right">SGST (₹)</th>
                    <th bmHeaderCell class="bm-align-right">IGST (₹)</th>
                  </tr>
                </thead>
                <tbody>
                  @if (!gstr1Summary()?.b2bInvoices?.length) {
                    <tr><td colspan="10" class="empty-cell">No B2B invoices recorded in period {{ currentTaxPeriod }}.</td></tr>
                  } @else {
                    @for (inv of paginatedB2b(); track inv.invoiceNumber) {
                      <tr>
                        <td bmCell><span class="font-mono font-bold">{{ inv.customerGstin }}</span></td>
                        <td bmCell>{{ inv.customerName }}</td>
                        <td bmCell><strong>{{ inv.invoiceNumber }}</strong></td>
                        <td bmCell>{{ inv.invoiceDate | date:'dd-MMM-yyyy' }}</td>
                        <td bmCell><span class="pos-badge">{{ inv.placeOfSupply }}</span></td>
                        <td bmCell class="bm-align-right font-bold">₹{{ inv.invoiceValue | number:'1.2-2' }}</td>
                        <td bmCell class="bm-align-right">₹{{ inv.taxableValue | number:'1.2-2' }}</td>
                        <td bmCell class="bm-align-right">₹{{ inv.cgstAmount | number:'1.2-2' }}</td>
                        <td bmCell class="bm-align-right">₹{{ inv.sgstAmount | number:'1.2-2' }}</td>
                        <td bmCell class="bm-align-right">₹{{ inv.igstAmount | number:'1.2-2' }}</td>
                      </tr>
                    }
                  }
                </tbody>
              </table>
              <bm-pagination
                *ngIf="(gstr1Summary()?.b2bInvoices?.length || 0) > 0"
                [totalItems]="gstr1Summary()?.b2bInvoices?.length || 0"
                [(currentPage)]="b2bPage"
                [(pageSize)]="b2bPageSize"
                itemName="B2B invoices">
              </bm-pagination>
            </div>
          </div>

          <!-- B2CS & B2CL Breakdown -->
          <div class="two-column-grid">
            <!-- B2CS Small Consumer -->
            <div bmPanel [bmBordered]="true" class="section-panel">
              <div bmPanelHeader class="section-header">
                <div class="section-title-wrap">
                  <i class="pi pi-users text-primary"></i>
                  <span class="section-title">Table 7 - B2C Small Consumer Supplies</span>
                </div>
              </div>
              <div bmPanelBody class="table-container">
                <table bmTable [bmStriped]="true">
                  <thead>
                    <tr>
                      <th bmHeaderCell>Type</th>
                      <th bmHeaderCell>POS</th>
                      <th bmHeaderCell class="bm-align-right">Rate (%)</th>
                      <th bmHeaderCell class="bm-align-right">Taxable (₹)</th>
                      <th bmHeaderCell class="bm-align-right">Total Tax (₹)</th>
                    </tr>
                  </thead>
                  <tbody>
                    @if (!gstr1Summary()?.b2csSections?.length) {
                      <tr><td colspan="5" class="empty-cell">No B2C small consumer supplies in this period.</td></tr>
                    } @else {
                      @for (sec of gstr1Summary()?.b2csSections; track sec.placeOfSupply + sec.gstRatePercent) {
                        <tr>
                          <td bmCell><span bmBadge="secondary">{{ sec.supplyType }}</span></td>
                          <td bmCell>{{ sec.placeOfSupply }}</td>
                          <td bmCell class="bm-align-right font-bold">{{ sec.gstRatePercent }}%</td>
                          <td bmCell class="bm-align-right">₹{{ sec.taxableValue | number:'1.2-2' }}</td>
                          <td bmCell class="bm-align-right font-bold">₹{{ (sec.cgstAmount + sec.sgstAmount + sec.igstAmount + sec.cessAmount) | number:'1.2-2' }}</td>
                        </tr>
                      }
                    }
                  </tbody>
                </table>
              </div>
            </div>

            <!-- Table 13 Document Sequence Summary -->
            <div bmPanel [bmBordered]="true" class="section-panel">
              <div bmPanelHeader class="section-header">
                <div class="section-title-wrap">
                  <i class="pi pi-list text-primary"></i>
                  <span class="section-title">Table 13 - Documents Issued Summary</span>
                </div>
              </div>
              <div bmPanelBody class="table-container">
                <table bmTable [bmStriped]="true">
                  <thead>
                    <tr>
                      <th bmHeaderCell>Nature of Document</th>
                      <th bmHeaderCell>From Serial</th>
                      <th bmHeaderCell>To Serial</th>
                      <th bmHeaderCell class="bm-align-right">Total</th>
                      <th bmHeaderCell class="bm-align-right">Cancelled</th>
                      <th bmHeaderCell class="bm-align-right">Net Issued</th>
                    </tr>
                  </thead>
                  <tbody>
                    @if (!gstr1Summary()?.documentsSummary?.length) {
                      <tr><td colspan="6" class="empty-cell">No documents issued in this period.</td></tr>
                    } @else {
                      @for (doc of gstr1Summary()?.documentsSummary; track doc.documentType) {
                        <tr>
                          <td bmCell><strong>{{ doc.documentName }}</strong></td>
                          <td bmCell class="font-mono">{{ doc.fromSerial }}</td>
                          <td bmCell class="font-mono">{{ doc.toSerial }}</td>
                          <td bmCell class="bm-align-right">{{ doc.totalCount }}</td>
                          <td bmCell class="bm-align-right text-danger">{{ doc.cancelledCount }}</td>
                          <td bmCell class="bm-align-right font-bold text-success">{{ doc.netIssuedCount }}</td>
                        </tr>
                      }
                    }
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      }

      <!-- TAB 2: GSTR-3B MONTHLY RETURN -->
      @if (activeTab === 'gstr3b') {
        <div class="gstr3b-container">
          <!-- Table 3.1: Outward Supplies & Reverse Charge -->
          <div bmPanel [bmBordered]="true" class="section-panel">
            <div bmPanelHeader class="section-header">
              <div class="section-title-wrap">
                <i class="pi pi-file-excel text-primary"></i>
                <span class="section-title">3.1 Details of Outward Supplies & Inward Supplies Liable to Reverse Charge</span>
              </div>
            </div>
            <div bmPanelBody class="table-container">
              <table bmTable [bmStriped]="true" [bmHover]="true">
                <thead>
                  <tr>
                    <th bmHeaderCell style="width: 45%;">Nature of Supply</th>
                    <th bmHeaderCell class="bm-align-right">Total Taxable Value (₹)</th>
                    <th bmHeaderCell class="bm-align-right">Integrated Tax (₹)</th>
                    <th bmHeaderCell class="bm-align-right">Central Tax (₹)</th>
                    <th bmHeaderCell class="bm-align-right">State/UT Tax (₹)</th>
                    <th bmHeaderCell class="bm-align-right">Cess (₹)</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td bmCell><strong>(a) Outward taxable supplies</strong> (other than zero rated, nil rated, exempted)</td>
                    <td bmCell class="bm-align-right font-bold">₹{{ gstr3bSummary()?.table31?.outwardTaxableSupplies?.taxableValue || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.outwardTaxableSupplies?.integratedTax || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.outwardTaxableSupplies?.centralTax || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.outwardTaxableSupplies?.stateTax || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.outwardTaxableSupplies?.cess || 0 | number:'1.2-2' }}</td>
                  </tr>
                  <tr>
                    <td bmCell><strong>(b) Outward taxable supplies (zero rated)</strong> (Exports / SEZ)</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.zeroRatedSupplies?.taxableValue || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.zeroRatedSupplies?.integratedTax || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹0.00</td>
                    <td bmCell class="bm-align-right">₹0.00</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.zeroRatedSupplies?.cess || 0 | number:'1.2-2' }}</td>
                  </tr>
                  <tr>
                    <td bmCell><strong>(c) Other outward supplies</strong> (Nil rated, exempted)</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.otherOutwardNilExempt || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹0.00</td>
                    <td bmCell class="bm-align-right">₹0.00</td>
                    <td bmCell class="bm-align-right">₹0.00</td>
                    <td bmCell class="bm-align-right">₹0.00</td>
                  </tr>
                  <tr>
                    <td bmCell><strong>(d) Inward supplies</strong> (liable to reverse charge)</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.inwardReverseChargeSupplies?.taxableValue || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.inwardReverseChargeSupplies?.integratedTax || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.inwardReverseChargeSupplies?.centralTax || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.inwardReverseChargeSupplies?.stateTax || 0 | number:'1.2-2' }}</td>
                    <td bmCell class="bm-align-right">₹{{ gstr3bSummary()?.table31?.inwardReverseChargeSupplies?.cess || 0 | number:'1.2-2' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Total Tax Liability Banner -->
          <div bmPanel class="tax-summary-panel">
            <div class="tax-summary-wrap">
              <div class="summary-left">
                <h3>Total Tax Liability for Period {{ currentTaxPeriod }}</h3>
                <p>Calculated statutory payment obligation across CGST, SGST, IGST, and Compensation Cess.</p>
              </div>
              <div class="summary-right">
                <div class="tax-breakdown-chip">
                  <span>IGST:</span>
                  <strong>₹{{ (gstr3bSummary()?.totalLiability?.integratedTax || 0) | number:'1.2-2' }}</strong>
                </div>
                <div class="tax-breakdown-chip">
                  <span>CGST:</span>
                  <strong>₹{{ (gstr3bSummary()?.totalLiability?.centralTax || 0) | number:'1.2-2' }}</strong>
                </div>
                <div class="tax-breakdown-chip">
                  <span>SGST:</span>
                  <strong>₹{{ (gstr3bSummary()?.totalLiability?.stateTax || 0) | number:'1.2-2' }}</strong>
                </div>
                <div class="total-badge-block">
                  <span class="total-title">Total Tax Payable</span>
                  <span class="total-amount">₹{{ (gstr3bSummary()?.totalLiability?.totalTax || 0) | number:'1.2-2' }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      }

      <!-- TAB 3: TABLE 12 HSN SUMMARY -->
      @if (activeTab === 'hsn') {
        <div bmPanel [bmBordered]="true" class="section-panel">
          <div bmPanelHeader class="section-header">
            <div class="section-title-wrap">
              <i class="pi pi-box text-primary"></i>
              <span class="section-title">Table 12 - HSN/SAC Summary of Outward Supplies</span>
            </div>
            <span bmBadge="primary">{{ hsnList().length }} HSN Codes</span>
          </div>

          <div bmPanelBody class="table-container">
            <table bmTable [bmStriped]="true" [bmHover]="true">
              <thead>
                <tr>
                  <th bmHeaderCell>HSN/SAC Code</th>
                  <th bmHeaderCell>Description</th>
                  <th bmHeaderCell>UQC</th>
                  <th bmHeaderCell class="bm-align-right">Total Qty</th>
                  <th bmHeaderCell class="bm-align-right">Total Value (₹)</th>
                  <th bmHeaderCell class="bm-align-right">Taxable Value (₹)</th>
                  <th bmHeaderCell class="bm-align-right">Rate (%)</th>
                  <th bmHeaderCell class="bm-align-right">CGST (₹)</th>
                  <th bmHeaderCell class="bm-align-right">SGST (₹)</th>
                  <th bmHeaderCell class="bm-align-right">IGST (₹)</th>
                </tr>
              </thead>
              <tbody>
                @if (hsnList().length === 0) {
                  <tr><td colspan="10" class="empty-cell">No HSN entries recorded in period {{ currentTaxPeriod }}.</td></tr>
                } @else {
                  @for (item of paginatedHsn(); track item.hsnSacCode + item.gstRatePercent) {
                    <tr>
                      <td bmCell><strong class="font-mono text-primary">{{ item.hsnSacCode }}</strong></td>
                      <td bmCell>{{ item.description }}</td>
                      <td bmCell><span class="uqc-tag">{{ item.unitCode }}</span></td>
                      <td bmCell class="bm-align-right font-bold">{{ item.totalQuantity }}</td>
                      <td bmCell class="bm-align-right">₹{{ item.totalValue | number:'1.2-2' }}</td>
                      <td bmCell class="bm-align-right font-bold">₹{{ item.taxableAmount | number:'1.2-2' }}</td>
                      <td bmCell class="bm-align-right">{{ item.gstRatePercent }}%</td>
                      <td bmCell class="bm-align-right">₹{{ item.cgstAmount | number:'1.2-2' }}</td>
                      <td bmCell class="bm-align-right">₹{{ item.sgstAmount | number:'1.2-2' }}</td>
                      <td bmCell class="bm-align-right">₹{{ item.igstAmount | number:'1.2-2' }}</td>
                    </tr>
                  }
                }
              </tbody>
            </table>
            <bm-pagination
              *ngIf="hsnList().length > 0"
              [totalItems]="hsnList().length"
              [(currentPage)]="hsnPage"
              [(pageSize)]="hsnPageSize"
              itemName="HSN lines">
            </bm-pagination>
          </div>
        </div>
      }

      <!-- TAB 4: FILING AUDIT TRACKER -->
      @if (activeTab === 'filings') {
        <div bmPanel [bmBordered]="true" class="section-panel">
          <div bmPanelHeader class="section-header">
            <div class="section-title-wrap">
              <i class="pi pi-history text-primary"></i>
              <span class="section-title">GST Filing Audit Trail & Portal Acknowledgements</span>
            </div>
          </div>

          <div bmPanelBody class="table-container">
            <table bmTable [bmStriped]="true" [bmHover]="true">
              <thead>
                <tr>
                  <th bmHeaderCell>Return Type</th>
                  <th bmHeaderCell>Tax Period</th>
                  <th bmHeaderCell>FY</th>
                  <th bmHeaderCell>Status</th>
                  <th bmHeaderCell>ARN (Ack No.)</th>
                  <th bmHeaderCell>ARN Date</th>
                  <th bmHeaderCell class="bm-align-right">Taxable (₹)</th>
                  <th bmHeaderCell class="bm-align-right">Liability (₹)</th>
                  <th bmHeaderCell>Recorded At</th>
                </tr>
              </thead>
              <tbody>
                @if (filingHistory().length === 0) {
                  <tr><td colspan="9" class="empty-cell">No historical GST return filings recorded.</td></tr>
                } @else {
                  @for (f of paginatedFilings(); track f.id) {
                    <tr>
                      <td bmCell><strong>{{ f.returnType === 1 ? 'GSTR-1' : 'GSTR-3B' }}</strong></td>
                      <td bmCell><span class="font-mono">{{ f.taxPeriod }}</span></td>
                      <td bmCell>{{ f.financialYear }}</td>
                      <td bmCell>
                        @if (f.filingStatus === 8) {
                          <span bmBadge="success">Filed</span>
                        } @else if (f.filingStatus === 4) {
                          <span bmBadge="primary">Submitted</span>
                        } @else if (f.filingStatus === 2) {
                          <span bmBadge="danger">Validation Failed</span>
                        } @else {
                          <span bmBadge="warning">Prepared</span>
                        }
                      </td>
                      <td bmCell>
                        @if (f.arn) {
                          <span class="font-mono font-bold text-success">{{ f.arn }}</span>
                        } @else {
                          <span class="text-muted">Pending</span>
                        }
                      </td>
                      <td bmCell>{{ f.arnDate ? (f.arnDate | date:'dd-MMM-yyyy HH:mm') : '-' }}</td>
                      <td bmCell class="bm-align-right font-bold">₹{{ f.totalTaxableValue | number:'1.2-2' }}</td>
                      <td bmCell class="bm-align-right font-bold">₹{{ f.totalTaxLiability | number:'1.2-2' }}</td>
                      <td bmCell>{{ f.createdAtUtc | date:'dd-MMM-yyyy HH:mm' }}</td>
                    </tr>
                  }
                }
              </tbody>
            </table>
            <bm-pagination
              *ngIf="filingHistory().length > 0"
              [totalItems]="filingHistory().length"
              [(currentPage)]="filingPage"
              [(pageSize)]="filingPageSize"
              itemName="filings">
            </bm-pagination>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .bm-gst-container {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
      padding-bottom: 2rem;
    }

    .bm-page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .bm-page-title {
      font-size: 1.35rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0;
    }

    .bm-page-subtitle {
      font-size: 0.85rem;
      color: #64748b;
      margin: 0.25rem 0 0 0;
    }

    .bm-page-actions {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .period-picker-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      background: #ffffff;
      padding: 0.25rem 0.65rem;
      border-radius: 8px;
      border: 1px solid #cbd5e1;
    }

    .period-label {
      font-size: 0.825rem;
      font-weight: 700;
      color: #334155;
    }

    .month-select, .year-select {
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 0.35rem 0.5rem;
      font-size: 0.825rem;
      font-weight: 600;
      color: #1e293b;
      cursor: pointer;
    }

    .validation-banner {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      padding: 1rem 1.25rem;
      border-radius: 8px;
      border: 1px solid;
    }

    .banner-success {
      background: #f0fdf4;
      border-color: #bbf7d0;
      color: #166534;

      .banner-icon { font-size: 1.35rem; color: #16a34a; }
    }

    .banner-warning {
      background: #fffbeb;
      border-color: #fde68a;
      color: #92400e;

      .banner-icon { font-size: 1.35rem; color: #d97706; }
    }

    .banner-title {
      margin: 0 0 0.25rem 0;
      font-size: 0.95rem;
      font-weight: 700;
    }

    .banner-text {
      margin: 0;
      font-size: 0.85rem;
      line-height: 1.4;
    }

    .banner-list {
      margin: 0.35rem 0 0 0;
      padding-left: 1.25rem;
      font-size: 0.825rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .bm-tabs-header {
      display: flex;
      gap: 0.5rem;
      border-bottom: 2px solid #e2e8f0;
      padding-bottom: 0.25rem;
    }

    .bm-tab-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: transparent;
      border: none;
      padding: 0.65rem 1.15rem;
      font-size: 0.9rem;
      font-weight: 600;
      color: #64748b;
      border-radius: 8px 8px 0 0;
      cursor: pointer;
      transition: all 0.15s ease;

      &:hover {
        color: #1e3a8a;
        background: #f1f5f9;
      }

      &.active {
        color: #1e3a8a;
        font-weight: 700;
        background: #eff6ff;
        border-bottom: 3px solid #2563eb;
      }
    }

    .bm-kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1rem;
    }

    .bm-kpi-card {
      padding: 1.1rem;
      border-radius: 8px;
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }

    .highlight-card {
      background: #eff6ff;
      border: 1px solid #bfdbfe;
    }

    .kpi-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .kpi-label {
      font-size: 0.775rem;
      text-transform: uppercase;
      font-weight: 700;
      color: #64748b;
    }

    .kpi-main-val {
      font-size: 1.45rem;
      font-weight: 800;
      color: #0f172a;
    }

    .kpi-meta {
      font-size: 0.8rem;
      color: #64748b;
    }

    .section-panel {
      background: #ffffff;
      border-radius: 8px;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .section-title-wrap {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .section-title {
      font-size: 0.95rem;
      font-weight: 700;
      color: #1e293b;
    }

    .table-container {
      overflow-x: auto;
    }

    .empty-cell {
      text-align: center;
      padding: 2rem;
      color: #94a3b8;
      font-style: italic;
    }

    .pos-badge, .uqc-tag {
      background: #f1f5f9;
      border: 1px solid #cbd5e1;
      padding: 0.15rem 0.45rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 700;
      color: #475569;
    }

    .two-column-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(450px, 1fr));
      gap: 1.25rem;
      margin-top: 1.25rem;
    }

    .gstr1-tables-stack {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .tax-summary-panel {
      background: #1e3a8a;
      color: #ffffff;
      border-radius: 10px;
      padding: 1.5rem;
      margin-top: 1rem;
    }

    .tax-summary-wrap {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1.5rem;
      flex-wrap: wrap;
    }

    .summary-left {
      h3 { margin: 0; font-size: 1.15rem; font-weight: 700; }
      p { margin: 0.35rem 0 0 0; font-size: 0.85rem; color: #93c5fd; }
    }

    .summary-right {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .tax-breakdown-chip {
      background: rgba(255, 255, 255, 0.12);
      border: 1px solid rgba(255, 255, 255, 0.2);
      border-radius: 6px;
      padding: 0.4rem 0.85rem;
      display: flex;
      flex-direction: column;
      font-size: 0.8rem;

      span { color: #bfdbfe; font-size: 0.725rem; }
      strong { font-size: 0.95rem; color: #ffffff; }
    }

    .total-badge-block {
      background: #ffffff;
      color: #1e3a8a;
      border-radius: 8px;
      padding: 0.5rem 1.15rem;
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.2);

      .total-title { font-size: 0.75rem; text-transform: uppercase; font-weight: 700; color: #64748b; }
      .total-amount { font-size: 1.35rem; font-weight: 800; color: #1e3a8a; }
    }
  `]
})
export class GstReturnsComponent implements OnInit {
  private readonly gstService = inject(GstService);
  private readonly dialogService = inject(DialogService);

  activeTab: 'gstr1' | 'gstr3b' | 'hsn' | 'filings' = 'gstr1';

  selectedMonth: string = `${DateTimeTodayMonth()}`;
  selectedYear: string = '2026';

  gstr1Summary = signal<Gstr1SummaryDto | null>(null);
  gstr3bSummary = signal<Gstr3bSummaryDto | null>(null);
  hsnList = signal<HsnSummaryItemDto[]>([]);
  validationResult = signal<GstValidationResultDto | null>(null);
  filingHistory = signal<GstReturnFilingDto[]>([]);

  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);

  b2bPage = signal<number>(1);
  b2bPageSize = signal<number>(10);
  paginatedB2b = computed(() => {
    const list = this.gstr1Summary()?.b2bInvoices || [];
    const start = (this.b2bPage() - 1) * this.b2bPageSize();
    return list.slice(start, start + this.b2bPageSize());
  });

  hsnPage = signal<number>(1);
  hsnPageSize = signal<number>(10);
  paginatedHsn = computed(() => {
    const list = this.hsnList();
    const start = (this.hsnPage() - 1) * this.hsnPageSize();
    return list.slice(start, start + this.hsnPageSize());
  });

  filingPage = signal<number>(1);
  filingPageSize = signal<number>(10);
  paginatedFilings = computed(() => {
    const list = this.filingHistory();
    const start = (this.filingPage() - 1) * this.filingPageSize();
    return list.slice(start, start + this.filingPageSize());
  });

  get currentTaxPeriod(): string {
    return `${this.selectedMonth}${this.selectedYear}`;
  }

  ngOnInit(): void {
    const today = new Date();
    const mm = (today.getMonth() + 1).toString().padStart(2, '0');
    this.selectedMonth = mm;
    this.selectedYear = today.getFullYear().toString();

    this.loadAllData();
  }

  onPeriodChange(): void {
    this.b2bPage.set(1);
    this.hsnPage.set(1);
    this.filingPage.set(1);
    this.loadAllData();
  }

  loadAllData(): void {
    this.isLoading.set(true);
    const period = this.currentTaxPeriod;

    // 1. Validation
    this.gstService.validateGstr1(period).subscribe({
      next: (res) => { if (res.success && res.data) this.validationResult.set(res.data); }
    });

    // 2. GSTR-1 Summary
    this.gstService.getGstr1Summary(period).subscribe({
      next: (res) => {
        if (res.success && res.data) this.gstr1Summary.set(res.data);
      }
    });

    // 3. GSTR-3B Summary
    this.gstService.getGstr3bSummary(period).subscribe({
      next: (res) => {
        if (res.success && res.data) this.gstr3bSummary.set(res.data);
      }
    });

    // 4. HSN Summary
    this.gstService.getHsnSummary(period).subscribe({
      next: (res) => {
        if (res.success && res.data) this.hsnList.set(res.data);
      }
    });

    // 5. Filing History
    this.gstService.getFilingHistory().subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.success && res.data) this.filingHistory.set(res.data);
      },
      error: () => this.isLoading.set(false)
    });
  }

  downloadPortalJson(): void {
    const period = this.currentTaxPeriod;
    this.gstService.downloadGstr1Json(period).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `GSTR1_${period}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.dialogService.toast(`GSTR-1 JSON downloaded for upload to GST Offline Utility: GSTR1_${period}.json`, 'success');
      },
      error: (err) => {
        this.dialogService.alert('Export Failed', err?.error?.message || 'Failed to export GSTR-1 JSON.', 'danger');
      }
    });
  }

  async submitGstr1(): Promise<void> {
    const period = this.currentTaxPeriod;
    const ok = await this.dialogService.confirm(
      'File GSTR-1 to Sandbox GSP',
      `Submit GSTR-1 return for tax period ${period} to government sandbox GSP gateway? This will validate compliance and log official Application Reference Number (ARN).`,
      'Submit Return',
      false,
      'info'
    );
    if (!ok) return;

    this.isSubmitting.set(true);
    this.gstService.submitGstr1(period, true).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.success && res.data) {
          this.dialogService.toast(`GSTR-1 successfully filed! Application Reference Number (ARN): ${res.data.arn}`, 'success');
          this.loadAllData();
        }
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.dialogService.alert('Submission Failed', err?.error?.message || 'Failed to submit GSTR-1 return.', 'danger');
      }
    });
  }
}

function DateTimeTodayMonth(): string {
  const m = new Date().getMonth() + 1;
  return m < 10 ? `0${m}` : `${m}`;
}

