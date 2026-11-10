import { Component, inject, signal, computed, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SetupService, InitialSetupRequest, INDIAN_STATES } from '../../core/services/setup.service';
import { BmButtonDirective } from '../../shared/directives/bm-button.directive';
import { BmPanelDirective } from '../../shared/directives/bm-panel.directive';
import { BmInputDirective, BmBadgeDirective } from '../../shared/directives/bm-controls.directive';

@Component({
  selector: 'app-setup-wizard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BmButtonDirective,
    BmPanelDirective,
    BmInputDirective
  ],
  template: `
    <div class="setup-container">
      <div bmPanel class="setup-card">
        <!-- Wizard Header -->
        <div class="setup-header">
          <div class="brand-row">
            <div class="brand-badge"><i class="pi pi-bolt"></i></div>
            <div>
              <h2 class="title">BillMitra Business Setup</h2>
              <p class="subtitle">Quick First-Run Configuration for Indian GST & Billing Operations</p>
            </div>
          </div>
          
          <!-- Stepper Progress Bar -->
          <div class="steps-nav">
            <div class="step-item" [class.active]="currentStep() === 1" [class.completed]="currentStep() > 1">
              <span class="step-num">1</span>
              <span class="step-label">Identity & GST</span>
            </div>
            <div class="step-divider"></div>
            <div class="step-item" [class.active]="currentStep() === 2" [class.completed]="currentStep() > 2">
              <span class="step-num">2</span>
              <span class="step-label">Address & Contact</span>
            </div>
            <div class="step-divider"></div>
            <div class="step-item" [class.active]="currentStep() === 3" [class.completed]="currentStep() > 3">
              <span class="step-num">3</span>
              <span class="step-label">Banking & FY</span>
            </div>
            <div class="step-divider"></div>
            <div class="step-item" [class.active]="currentStep() === 4" [class.completed]="currentStep() > 4">
              <span class="step-num">4</span>
              <span class="step-label">Admin Account</span>
            </div>
          </div>
        </div>

        <!-- Wizard Body -->
        <div class="setup-body">
          @if (errorMessage()) {
            <div class="alert-box error">
              <i class="pi pi-exclamation-triangle"></i>
              <span>{{ errorMessage() }}</span>
            </div>
          }

          <!-- STEP 1: Business Identity & GST -->
          @if (currentStep() === 1) {
            <div class="form-grid">
              <div class="col-12">
                <h3 class="section-title">1. Business Identity</h3>
              </div>

              <div class="col-6">
                <label class="form-label required">Legal Entity Name (as per PAN/GST)</label>
                <input bmInput type="text" [(ngModel)]="form.legalName" placeholder="e.g. Acme Enterprises Private Limited" />
              </div>

              <div class="col-6">
                <label class="form-label">Trade / Brand Name (Optional)</label>
                <input bmInput type="text" [(ngModel)]="form.tradeName" placeholder="e.g. Acme Tech Solutions" />
              </div>

              <div class="col-6">
                <label class="form-label">GSTIN (15 Alphanumeric)</label>
                <input bmInput type="text" maxlength="15" [(ngModel)]="form.gstin" (ngModelChange)="onGstinChange($event)" placeholder="e.g. 24ABCDE1234F1Z5" />
                @if (extractedPan()) {
                  <span class="helper-text"><i class="pi pi-id-card"></i> Auto-Extracted PAN: <strong>{{ extractedPan() }}</strong></span>
                }
              </div>

              <div class="col-6">
                <label class="form-label required">GST Registration State</label>
                <select bmInput [(ngModel)]="form.stateCode" (ngModelChange)="onStateChange($event)">
                  <option value="" disabled>Select State / UT</option>
                  @for (state of states; track state.code) {
                    <option [value]="state.code">{{ state.code }} - {{ state.name }}</option>
                  }
                </select>
              </div>

              <div class="col-6">
                <label class="form-label">GST Registration Category</label>
                <select bmInput [(ngModel)]="form.gstRegistrationType">
                  <option [ngValue]="1">Regular Registered Taxpayer</option>
                  <option [ngValue]="2">Composition Scheme Dealer</option>
                  <option [ngValue]="3">Unregistered SMB</option>
                  <option [ngValue]="4">Overseas / SEZ Unit</option>
                </select>
              </div>
            </div>
          }

          <!-- STEP 2: Address & Contact -->
          @if (currentStep() === 2) {
            <div class="form-grid">
              <div class="col-12">
                <h3 class="section-title">2. Registered Place of Business</h3>
              </div>

              <div class="col-12">
                <label class="form-label required">Address Line 1</label>
                <input bmInput type="text" [(ngModel)]="form.addressLine1" placeholder="Shop / Flat No., Building, Street Name" />
              </div>

              <div class="col-12">
                <label class="form-label">Address Line 2 (Optional)</label>
                <input bmInput type="text" [(ngModel)]="form.addressLine2" placeholder="Locality, Landmark, Area" />
              </div>

              <div class="col-6">
                <label class="form-label required">City / District</label>
                <input bmInput type="text" [(ngModel)]="form.city" placeholder="e.g. Ahmedabad" />
              </div>

              <div class="col-6">
                <label class="form-label required">Postal PIN Code (6 Digits)</label>
                <input bmInput type="text" maxlength="6" [(ngModel)]="form.pincode" placeholder="e.g. 380015" />
              </div>

              <div class="col-6">
                <label class="form-label required">Primary Mobile Number</label>
                <input bmInput type="tel" maxlength="15" [(ngModel)]="form.phone" placeholder="e.g. 9876543210" />
              </div>

              <div class="col-6">
                <label class="form-label">Official Email</label>
                <input bmInput type="email" [(ngModel)]="form.email" placeholder="e.g. accounts@acme.com" />
              </div>
            </div>
          }

          <!-- STEP 3: Banking & Financial Year -->
          @if (currentStep() === 3) {
            <div class="form-grid">
              <div class="col-12">
                <h3 class="section-title">3. Settlement Bank & Accounting Period</h3>
              </div>

              <div class="col-6">
                <label class="form-label">Initial Financial Year</label>
                <select bmInput [(ngModel)]="form.financialYearStartYear">
                  <option [ngValue]="2026">FY 2026-27 (Current Cycle)</option>
                  <option [ngValue]="2025">FY 2025-26 (Previous Cycle)</option>
                </select>
                <span class="helper-text"><i class="pi pi-calendar"></i> Indian FY runs April 01 to March 31</span>
              </div>

              <div class="col-6">
                <label class="form-label">Settlement UPI VPA (Optional)</label>
                <input bmInput type="text" [(ngModel)]="form.upiId" placeholder="e.g. acme@okhdfcbank" />
              </div>

              <div class="col-6">
                <label class="form-label">Bank Name</label>
                <input bmInput type="text" [(ngModel)]="form.bankName" placeholder="e.g. State Bank of India" />
              </div>

              <div class="col-6">
                <label class="form-label">Bank Account Number</label>
                <input bmInput type="text" [(ngModel)]="form.bankAccountNumber" placeholder="Account Number" />
              </div>

              <div class="col-6">
                <label class="form-label">IFSC Code</label>
                <input bmInput type="text" maxlength="11" [(ngModel)]="form.bankIfsc" placeholder="e.g. SBIN0001234" />
              </div>

              <div class="col-6">
                <label class="form-label">Bank Branch Name</label>
                <input bmInput type="text" [(ngModel)]="form.bankBranch" placeholder="e.g. Main Commercial Branch" />
              </div>
            </div>
          }

          <!-- STEP 4: Administrator Account -->
          @if (currentStep() === 4) {
            <div class="form-grid">
              <div class="col-12">
                <h3 class="section-title">4. Master Administrator Credential</h3>
                <p class="section-desc">Create your local administrator profile to safeguard invoicing, audit records, and operational settings.</p>
              </div>

              <div class="col-6">
                <label class="form-label required">Administrator Full Name</label>
                <input bmInput type="text" [(ngModel)]="form.ownerFullName" placeholder="e.g. Rajesh Sharma" />
              </div>

              <div class="col-6">
                <label class="form-label required">Username</label>
                <input bmInput type="text" [(ngModel)]="form.ownerUsername" placeholder="e.g. admin" />
              </div>

              <div class="col-6">
                <label class="form-label required">Administrator Email</label>
                <input bmInput type="email" [(ngModel)]="form.ownerEmail" placeholder="e.g. admin@billmitra.local" />
              </div>

              <div class="col-6">
                <label class="form-label required">Password (Min 6 Characters)</label>
                <input bmInput type="password" [(ngModel)]="form.ownerPassword" placeholder="••••••••" />
              </div>

              <!-- Setup Summary Card -->
              <div class="col-12 summary-card">
                <h4 class="summary-title"><i class="pi pi-check-circle"></i> Verification Checklist</h4>
                <div class="summary-grid">
                  <div><strong>Legal Entity:</strong> {{ form.legalName || 'Not Set' }}</div>
                  <div><strong>State:</strong> {{ form.stateName || 'Not Set' }} ({{ form.stateCode }})</div>
                  <div><strong>GSTIN:</strong> {{ form.gstin || 'Unregistered' }}</div>
                  <div><strong>Accounting FY:</strong> FY {{ form.financialYearStartYear }}-{{ (form.financialYearStartYear + 1) % 100 }}</div>
                  <div><strong>Super Admin:</strong> {{ form.ownerUsername || 'Not Set' }}</div>
                </div>
              </div>
            </div>
          }
        </div>

        <!-- Wizard Footer Navigation -->
        <div class="setup-footer">
          <div>
            @if (currentStep() > 1) {
              <button bmButton bmVariant="secondary" (click)="prevStep()" [disabled]="isSubmitting()">
                <i class="pi pi-arrow-left"></i> Previous
              </button>
            }
          </div>

          <div>
            @if (currentStep() < 4) {
              <button bmButton bmVariant="primary" (click)="nextStep()">
                Continue <i class="pi pi-arrow-right"></i>
              </button>
            } @else {
              <button bmButton bmVariant="success" (click)="submitSetup()" [disabled]="isSubmitting()">
                @if (isSubmitting()) {
                  <i class="pi pi-spin pi-spinner"></i> Initializing System...
                } @else {
                  <i class="pi pi-check"></i> Complete Setup & Open BillMitra
                }
              </button>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .setup-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 90vh;
      padding: 1.5rem;
    }

    .setup-card {
      width: 100%;
      max-width: 860px;
      padding: 0;
      border-radius: var(--bm-radius-lg);
      background: #ffffff;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .setup-header {
      padding: 1.5rem 2rem;
      background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
      color: #ffffff;
    }

    .brand-row {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .brand-badge {
      width: 44px;
      height: 44px;
      border-radius: 10px;
      background: var(--bm-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.4rem;
      color: #ffffff;
    }

    .title {
      font-size: 1.35rem;
      font-weight: 700;
      letter-spacing: -0.02em;
      margin: 0;
    }

    .subtitle {
      font-size: 0.85rem;
      color: #94a3b8;
      margin: 0.25rem 0 0 0;
    }

    .steps-nav {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-top: 1rem;
      border-top: 1px solid rgba(255, 255, 255, 0.1);
    }

    .step-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      opacity: 0.6;
      transition: all 0.2s ease;
    }

    .step-item.active {
      opacity: 1;
      font-weight: 600;
    }

    .step-item.completed {
      opacity: 0.9;
    }

    .step-num {
      width: 24px;
      height: 24px;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.2);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.75rem;
    }

    .step-item.active .step-num {
      background: var(--bm-primary);
      color: #ffffff;
    }

    .step-item.completed .step-num {
      background: #10b981;
      color: #ffffff;
    }

    .step-label {
      font-size: 0.8rem;
    }

    .step-divider {
      flex: 1;
      height: 1px;
      background: rgba(255, 255, 255, 0.15);
      margin: 0 0.75rem;
    }

    .setup-body {
      padding: 2rem;
      min-height: 380px;
    }

    .section-title {
      font-size: 1.1rem;
      font-weight: 700;
      color: #0f172a;
      margin-top: 0;
      margin-bottom: 0.25rem;
    }

    .section-desc {
      font-size: 0.85rem;
      color: #64748b;
      margin-bottom: 1.25rem;
    }

    .form-grid {
      display: grid;
      grid-template-columns: repeat(12, 1fr);
      gap: 1rem;
    }

    .col-12 { grid-column: span 12; }
    .col-6 { grid-column: span 6; }

    .form-label {
      display: block;
      font-size: 0.8rem;
      font-weight: 600;
      color: #334155;
      margin-bottom: 0.35rem;
    }

    .form-label.required::after {
      content: ' *';
      color: #ef4444;
    }

    .helper-text {
      display: block;
      font-size: 0.75rem;
      color: #0284c7;
      margin-top: 0.35rem;
    }

    .alert-box {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.85rem 1rem;
      border-radius: var(--bm-radius-md);
      margin-bottom: 1.25rem;
      font-size: 0.875rem;
    }

    .alert-box.error {
      background: #fef2f2;
      border: 1px solid #fecaca;
      color: #b91c1c;
    }

    .summary-card {
      margin-top: 1rem;
      padding: 1.25rem;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: var(--bm-radius-md);
    }

    .summary-title {
      font-size: 0.95rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0 0 0.75rem 0;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 0.5rem;
      font-size: 0.85rem;
      color: #475569;
    }

    .setup-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.25rem 2rem;
      background: #f8fafc;
      border-top: 1px solid #e2e8f0;
    }
  `]
})
export class SetupWizardComponent {
  private readonly setupService = inject(SetupService);

  public readonly completed = output<void>();

  public currentStep = signal<number>(1);
  public isSubmitting = signal<boolean>(false);
  public errorMessage = signal<string | null>(null);
  public extractedPan = signal<string | null>(null);

  public states = INDIAN_STATES;

  public form: InitialSetupRequest = {
    legalName: '',
    tradeName: '',
    gstin: '',
    gstRegistrationType: 1,
    stateCode: '24',
    stateName: 'Gujarat',
    addressLine1: '',
    addressLine2: '',
    city: 'Ahmedabad',
    pincode: '380015',
    phone: '',
    email: '',
    bankName: '',
    bankAccountNumber: '',
    bankIfsc: '',
    bankBranch: '',
    upiId: '',
    financialYearStartYear: 2026,
    ownerFullName: 'Rajesh Sharma',
    ownerUsername: 'admin',
    ownerEmail: 'admin@billmitra.local',
    ownerPassword: ''
  };

  public onGstinChange(val: string): void {
    if (!val) {
      this.extractedPan.set(null);
      return;
    }
    const clean = val.trim().toUpperCase();
    this.form.gstin = clean;

    if (clean.length >= 2) {
      const statePrefix = clean.substring(0, 2);
      const match = this.states.find(s => s.code === statePrefix);
      if (match) {
        this.form.stateCode = match.code;
        this.form.stateName = match.name;
      }
    }

    if (clean.length >= 12) {
      this.extractedPan.set(clean.substring(2, 10));
    } else {
      this.extractedPan.set(null);
    }
  }

  public onStateChange(code: string): void {
    const match = this.states.find(s => s.code === code);
    if (match) {
      this.form.stateName = match.name;
    }
  }

  public nextStep(): void {
    this.errorMessage.set(null);
    const step = this.currentStep();

    if (step === 1) {
      if (!this.form.legalName.trim()) {
        this.errorMessage.set('Please enter the Legal Entity Name.');
        return;
      }
      if (!this.form.stateCode) {
        this.errorMessage.set('Please select the GST Registration State.');
        return;
      }
      if (this.form.gstin) {
        const cleanGstin = this.form.gstin.trim();
        const gstinRegex = /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/;
        if (!gstinRegex.test(cleanGstin)) {
          this.errorMessage.set('GSTIN format is invalid. Must be 15 alphanumeric characters.');
          return;
        }
      }
    } else if (step === 2) {
      if (!this.form.addressLine1.trim()) {
        this.errorMessage.set('Address Line 1 is required.');
        return;
      }
      if (!this.form.city.trim()) {
        this.errorMessage.set('City name is required.');
        return;
      }
      if (!this.form.pincode.trim() || this.form.pincode.trim().length !== 6) {
        this.errorMessage.set('Please provide a valid 6-digit Indian PIN code.');
        return;
      }
      if (!this.form.phone.trim()) {
        this.errorMessage.set('Primary Contact Phone number is required.');
        return;
      }
    }

    this.currentStep.set(step + 1);
  }

  public prevStep(): void {
    this.errorMessage.set(null);
    if (this.currentStep() > 1) {
      this.currentStep.set(this.currentStep() - 1);
    }
  }

  public submitSetup(): void {
    this.errorMessage.set(null);

    if (!this.form.ownerUsername.trim()) {
      this.errorMessage.set('Administrator Username is required.');
      return;
    }
    if (!this.form.ownerPassword || this.form.ownerPassword.length < 6) {
      this.errorMessage.set('Password must be at least 6 characters.');
      return;
    }

    this.isSubmitting.set(true);

    this.setupService.completeSetup(this.form).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.success) {
          this.completed.emit();
        } else {
          this.errorMessage.set(res.message || 'Initialization failed.');
        }
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err?.error?.message || err?.message || 'Error executing setup request.');
      }
    });
  }
}

