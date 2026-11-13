import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SetupService, CompanyProfileDto } from '../../core/services/setup.service';
import { DialogService } from '../../shared/services/dialog.service';
import {
  BmButtonDirective,
  BmPanelDirective,
  BmPanelHeaderDirective,
  BmPanelBodyDirective,
  BmPanelFooterDirective,
  BmInputDirective,
  BmSelectDirective,
  BmBadgeDirective
} from '../../shared/directives';

@Component({
  selector: 'app-company-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BmButtonDirective,
    BmPanelDirective,
    BmPanelHeaderDirective,
    BmPanelBodyDirective,
    BmPanelFooterDirective,
    BmInputDirective,
    BmBadgeDirective
  ],
  template: `
    <div class="bm-settings-container">
      <div class="bm-page-header">
        <div>
          <h1 class="bm-page-title">Company Profile & Invoice Stationery Settings</h1>
          <p class="bm-page-subtitle">Configure business statutory credentials, invoice templates, digital letterhead banners, and pre-printed stationery margins</p>
        </div>
        <div class="bm-page-actions">
          <button bmButton bmVariant="danger" bmSize="md" (click)="confirmResetAllData()" title="Clear all invoices, purchases, payments, expenses, parties, items and reset sequences back to 0">
            <i class="pi pi-trash"></i>
            <span>Reset All Data & Sequences</span>
          </button>
          <button bmButton bmVariant="secondary" bmSize="md" (click)="loadProfile()">
            <i class="pi pi-refresh"></i>
            <span>Reload</span>
          </button>
        </div>
      </div>

      <div class="bm-settings-grid">
        <!-- Section 1: Business Identity & GST Details -->
        <div bmPanel bmElevation="sm" class="settings-panel">
          <div bmPanelHeader>
            <div class="panel-header-wrap">
              <i class="pi pi-building text-primary"></i>
              <h3>Business Identity & GST Registration</h3>
            </div>
            <span bmBadge="success">Verified Profile</span>
          </div>

          <div bmPanelBody class="panel-form-body">
            @if (isLoading()) {
              <div class="loading-wrap">
                <i class="pi pi-spin pi-spinner"></i>
                <span>Loading profile...</span>
              </div>
            } @else if (profile()) {
              <div class="info-grid">
                <div class="info-item">
                  <span class="info-label">Legal Name:</span>
                  <span class="info-value font-bold">{{ profile()?.legalName }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">Trade Name:</span>
                  <span class="info-value">{{ profile()?.tradeName || 'Same as Legal' }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">GSTIN:</span>
                  <span class="info-value font-mono font-bold text-primary">{{ profile()?.gstin || 'Unregistered' }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">PAN:</span>
                  <span class="info-value font-mono">{{ profile()?.pan || 'N/A' }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">State / Jurisdiction:</span>
                  <span class="info-value">{{ profile()?.stateName }} (Code: {{ profile()?.stateCode }})</span>
                </div>
                <div class="info-item">
                  <span class="info-label">Registered Address:</span>
                  <span class="info-value">{{ profile()?.addressLine1 }}, {{ profile()?.city }} - {{ profile()?.pincode }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">Contact:</span>
                  <span class="info-value">{{ profile()?.phone }} | {{ profile()?.email || 'N/A' }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">Primary Bank / UPI:</span>
                  <span class="info-value">{{ profile()?.bankName || 'Not configured' }} {{ profile()?.bankAccountNumber ? '(A/C: ' + profile()?.bankAccountNumber + ')' : '' }}</span>
                </div>
              </div>
            }
          </div>
        </div>

        <!-- Section 2: Invoice Letterhead & Stationery Configuration -->
        <div bmPanel bmElevation="sm" class="settings-panel">
          <div bmPanelHeader>
            <div class="panel-header-wrap">
              <i class="pi pi-print text-primary"></i>
              <h3>Invoice Stationery & Letterhead Management</h3>
            </div>
            <span bmBadge="primary">Print Engine</span>
          </div>

          <div bmPanelBody class="panel-form-body">
            <!-- Mode Selection -->
            <div class="bm-form-row">
              <label class="bm-label">Default Invoice Printing Mode:</label>
              <div class="radio-cards">
                <label class="radio-card" [class.selected]="defaultMode === 1">
                  <input type="radio" name="letterheadMode" [value]="1" [(ngModel)]="defaultMode" />
                  <div class="radio-card-content">
                    <div class="radio-card-title">
                      <i class="pi pi-file"></i> With Digital Letterhead (Plain Paper)
                    </div>
                    <div class="radio-card-desc">
                      Prints complete company legal name, logo/banner, address, and GSTIN header on standard blank paper.
                    </div>
                  </div>
                </label>

                <label class="radio-card" [class.selected]="defaultMode === 2">
                  <input type="radio" name="letterheadMode" [value]="2" [(ngModel)]="defaultMode" />
                  <div class="radio-card-content">
                    <div class="radio-card-title">
                      <i class="pi pi-align-justify"></i> Pre-Printed Stationery Mode
                    </div>
                    <div class="radio-card-desc">
                      Suppresses company header and leaves a top margin so the invoice prints neatly on offset-printed business letterhead.
                    </div>
                  </div>
                </label>
              </div>
            </div>

            <!-- Pre-printed Stationery Top Margin -->
            @if (defaultMode === 2) {
              <div class="margin-config-box">
                <div class="bm-form-row">
                  <label class="bm-label">Pre-Printed Header Margin (mm):</label>
                  <div class="margin-input-group">
                    <input
                      bmInput
                      type="number"
                      min="20"
                      max="100"
                      step="5"
                      [(ngModel)]="preprintedMarginMm"
                      class="margin-input"
                    />
                    <span class="unit-text">millimeters (mm)</span>
                  </div>
                  <span class="bm-help-text">Standard pre-printed letterheads typically require 45mm to 60mm blank top space.</span>
                </div>
              </div>
            }

            <!-- Custom Letterhead Banner Graphic Upload -->
            <div class="letterhead-upload-section">
              <label class="bm-label">Custom Digital Letterhead Graphic (Optional):</label>
              <p class="bm-help-text">
                Upload a header banner graphic (recommended: 1800 x 300 px, PNG or JPEG). When present, this graphic replaces the standard text header on plain paper invoices.
              </p>

              @if (currentLetterheadPath()) {
                <div class="current-letterhead-card">
                  <div class="letterhead-status">
                    <i class="pi pi-check-circle text-success"></i>
                    <span>Custom Letterhead Active: <strong>{{ getLetterheadFileName() }}</strong></span>
                  </div>
                  <div class="letterhead-actions">
                    <button bmButton bmVariant="danger" bmSize="sm" (click)="removeLetterhead()">
                      <i class="pi pi-trash"></i>
                      <span>Remove</span>
                    </button>
                  </div>
                </div>
              }

              <div class="upload-controls">
                <input
                  type="file"
                  #fileInput
                  accept="image/png, image/jpeg, image/webp"
                  (change)="onFileSelected($event)"
                  class="hidden-file-input"
                />
                <button bmButton bmVariant="outline" bmSize="sm" (click)="fileInput.click()" [disabled]="isUploading()">
                  <i class="pi pi-upload"></i>
                  <span>{{ isUploading() ? 'Uploading...' : 'Upload Header Image' }}</span>
                </button>
                <span class="supported-formats">PNG, JPEG, WebP up to 5MB</span>
              </div>
            </div>
          </div>

          <div bmPanelFooter class="panel-footer-actions">
            <button
              bmButton
              bmVariant="primary"
              bmSize="md"
              (click)="saveLetterheadSettings()"
              [disabled]="isSaving()"
            >
              <i class="pi pi-save"></i>
              <span>{{ isSaving() ? 'Saving Settings...' : 'Save Stationery Settings' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bm-settings-container {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
      padding-bottom: 2rem;
    }

    .bm-page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
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

    .bm-settings-grid {
      display: grid;
      grid-template-columns: 1fr;
      gap: 1.5rem;
    }

    .settings-panel {
      background: #ffffff;
      border-radius: 8px;
    }

    .panel-header-wrap {
      display: flex;
      align-items: center;
      gap: 0.65rem;

      h3 {
        margin: 0;
        font-size: 1rem;
        font-weight: 700;
        color: #1e293b;
      }
    }

    .panel-form-body {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1rem;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
      background: #f8fafc;
      padding: 0.75rem 1rem;
      border-radius: 6px;
      border: 1px solid #e2e8f0;
    }

    .info-label {
      font-size: 0.75rem;
      font-weight: 600;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }

    .info-value {
      font-size: 0.9rem;
      color: #1e293b;
    }

    .radio-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 1rem;
      margin-top: 0.5rem;
    }

    .radio-card {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 1rem;
      border: 2px solid #e2e8f0;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.15s ease;

      input[type="radio"] {
        margin-top: 0.25rem;
      }

      &.selected {
        border-color: #2563eb;
        background: #eff6ff;
      }
    }

    .radio-card-title {
      font-weight: 700;
      font-size: 0.9rem;
      color: #0f172a;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .radio-card-desc {
      font-size: 0.8rem;
      color: #64748b;
      margin-top: 0.25rem;
      line-height: 1.4;
    }

    .margin-config-box {
      background: #fefce8;
      border: 1px solid #fef08a;
      padding: 1rem;
      border-radius: 8px;
    }

    .margin-input-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-top: 0.35rem;
    }

    .margin-input {
      width: 100px;
    }

    .unit-text {
      font-size: 0.85rem;
      font-weight: 600;
      color: #475569;
    }

    .letterhead-upload-section {
      border-top: 1px solid #e2e8f0;
      padding-top: 1.25rem;
    }

    .current-letterhead-card {
      display: flex;
      align-items: center;
      justify-content: space-between;
      background: #f0fdf4;
      border: 1px solid #bbf7d0;
      padding: 0.75rem 1rem;
      border-radius: 6px;
      margin: 0.75rem 0;
    }

    .letterhead-status {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.85rem;
      color: #166534;
    }

    .upload-controls {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-top: 0.5rem;
    }

    .hidden-file-input {
      display: none;
    }

    .supported-formats {
      font-size: 0.8rem;
      color: #94a3b8;
    }

    .panel-footer-actions {
      display: flex;
      justify-content: flex-end;
      padding: 0.875rem 1.25rem;
      background: #f8fafc;
      border-top: 1px solid #e2e8f0;
    }

    .bm-label {
      font-weight: 700;
      font-size: 0.875rem;
      color: #1e293b;
    }

    .bm-help-text {
      font-size: 0.8rem;
      color: #64748b;
      margin-top: 0.25rem;
    }

    .loading-wrap {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      color: #64748b;
      padding: 1.5rem;
    }
  `]
})
export class CompanySettingsComponent implements OnInit {
  private readonly setupService = inject(SetupService);
  private readonly dialogService = inject(DialogService);

  profile = signal<CompanyProfileDto | null>(null);
  isLoading = signal<boolean>(false);
  isSaving = signal<boolean>(false);
  isUploading = signal<boolean>(false);

  defaultMode: number = 1;
  preprintedMarginMm: number = 50;
  currentLetterheadPath = signal<string | null>(null);

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.setupService.getCompanyProfile().subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.success && res.data) {
          this.profile.set(res.data);
          this.defaultMode = res.data.defaultLetterheadMode || 1;
          this.preprintedMarginMm = res.data.preprintedHeaderMarginMm || 50;
          this.currentLetterheadPath.set(res.data.letterheadImagePath || null);
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.dialogService.alert('Error', 'Failed to load company profile settings.', 'danger');
      }
    });
  }

  saveLetterheadSettings(): void {
    this.isSaving.set(true);
    this.setupService.updateLetterheadSettings({
      defaultLetterheadMode: this.defaultMode,
      preprintedHeaderMarginMm: Number(this.preprintedMarginMm) || 50
    }).subscribe({
      next: (res) => {
        this.isSaving.set(false);
        if (res.success) {
          this.dialogService.toast('Stationery & letterhead preferences updated successfully!', 'success');
          if (res.data) {
            this.profile.set(res.data);
          }
        }
      },
      error: (err) => {
        this.isSaving.set(false);
        this.dialogService.alert('Update Failed', err?.error?.message || 'Failed to update letterhead settings.', 'danger');
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (file.size > 5 * 1024 * 1024) {
      this.dialogService.alert('File Too Large', 'Please select a letterhead graphic under 5MB.', 'warning');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const base64Data = reader.result as string;
      this.isUploading.set(true);
      this.setupService.uploadLetterhead(base64Data, file.name).subscribe({
        next: (res) => {
          this.isUploading.set(false);
          if (res.success && res.data) {
            this.currentLetterheadPath.set(res.data);
            this.dialogService.toast('Custom letterhead graphic uploaded successfully!', 'success');
            this.loadProfile();
          }
        },
        error: (err) => {
          this.isUploading.set(false);
          this.dialogService.alert('Upload Failed', err?.error?.message || 'Failed to upload letterhead image.', 'danger');
        }
      });
    };
    reader.readAsDataURL(file);
  }

  async removeLetterhead(): Promise<void> {
    const ok = await this.dialogService.confirm(
      'Remove Custom Letterhead',
      'Are you sure you want to remove the custom letterhead graphic? Standard legal details will be printed instead.',
      'Remove',
      true,
      'warning'
    );
    if (!ok) return;

    this.setupService.removeLetterhead().subscribe({
      next: (res) => {
        if (res.success) {
          this.currentLetterheadPath.set(null);
          this.dialogService.toast('Custom letterhead removed.', 'warning');
          this.loadProfile();
        }
      },
      error: () => this.dialogService.alert('Error', 'Failed to remove letterhead.', 'danger')
    });
  }

  getLetterheadFileName(): string {
    const p = this.currentLetterheadPath();
    if (!p) return '';
    return p.split(/[\/\\]/).pop() || 'letterhead.png';
  }

  async confirmResetAllData(): Promise<void> {
    const ok = await this.dialogService.confirm(
      'Wipe All Test Data & Reset Counters',
      'WARNING: This will permanently delete all sales invoices, purchases, expenses, payment vouchers, stock movements, parties, and products. Document numbering sequence counters will be reset to 0 so you can test starting from #1. Company registration remains intact. Do you wish to proceed?',
      'Yes, Wipe Clean',
      true,
      'danger'
    );
    if (!ok) return;

    this.setupService.resetData().subscribe({
      next: (res) => {
        if (res.success) {
          this.dialogService.toast('All transactional and master records wiped. Sequences reset to 0.', 'success');
        }
      },
      error: (err) => {
        this.dialogService.alert('Reset Failed', err?.error?.message || 'Failed to reset database.', 'danger');
      }
    });
  }
}
