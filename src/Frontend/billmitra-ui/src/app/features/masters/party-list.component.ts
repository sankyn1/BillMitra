import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MastersService, PartyDto, CreateOrUpdatePartyRequest } from '../../core/services/masters.service';
import { INDIAN_STATES } from '../../core/services/setup.service';
import { DialogService } from '../../shared/services/dialog.service';
import { BmPaginationComponent } from '../../shared/components';
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
  selector: 'app-party-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BmPaginationComponent,
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
          <h2 class="master-title"><i class="pi pi-users mr-2"></i> Customers & Suppliers Directory</h2>
          <p class="master-subtitle">Manage B2B/B2C parties, GSTIN registration, billing/shipping states, and ledger balances</p>
        </div>
        <div class="master-actions">
          <button bmButton bmVariant="outline" bmSize="md" (click)="openQuickSeed()">
            <i class="pi pi-user-plus"></i> Seed Sample Parties
          </button>
          <button bmButton bmVariant="primary" bmSize="md" (click)="openCreateModal()">
            <i class="pi pi-plus"></i> Add New Party (F4)
          </button>
        </div>
      </div>

      <!-- Search & Filters Toolbar -->
      <div bmPanel bmElevation="sm" class="toolbar-panel mb-3">
        <div class="toolbar-grid">
          <div class="search-wrap">
            <input bmInput type="text" [(ngModel)]="searchQuery" (ngModelChange)="onSearchChange($event)" placeholder="Search by Party Name, GSTIN, Phone, or City..." />
          </div>
          <div class="filter-wrap">
            <select bmInput [(ngModel)]="selectedType" (ngModelChange)="loadParties()">
              <option [ngValue]="null">All Parties</option>
              <option [ngValue]="1">Customers Only</option>
              <option [ngValue]="2">Suppliers Only</option>
            </select>
          </div>
          <div class="filter-count">
            <span bmBadge="info">{{ mastersService.parties().length }} Records</span>
          </div>
        </div>
      </div>

      <!-- Main Parties Table -->
      <div bmPanel bmElevation="sm">
        <div bmPanelBody class="p-0">
          <table bmTable [bmStriped]="true" [bmHover]="true">
            <thead>
              <tr>
                <th bmTableHeaderCell>Party Name / Trade Name</th>
                <th bmTableHeaderCell>Category</th>
                <th bmTableHeaderCell>GSTIN / PAN</th>
                <th bmTableHeaderCell>State of Supply</th>
                <th bmTableHeaderCell>Contact & City</th>
                <th bmTableHeaderCell bmAlign="right">Credit Limit</th>
                <th bmTableHeaderCell bmAlign="right">Ledger Balance</th>
                <th bmTableHeaderCell bmAlign="center">Actions</th>
              </tr>
            </thead>
            <tbody>
              @if (mastersService.parties().length === 0) {
                <tr bmTableRow>
                  <td bmTableCell colspan="8" class="text-center py-5">
                    <div class="empty-state">
                      <i class="pi pi-users empty-icon"></i>
                      <h4>No Customers or Suppliers Found</h4>
                      <p>Register customer accounts for GST invoicing or click "Seed Sample Parties" to populate demo records.</p>
                      <button bmButton bmVariant="primary" bmSize="sm" (click)="openCreateModal()">
                        <i class="pi pi-plus"></i> Add Party
                      </button>
                    </div>
                  </td>
                </tr>
              } @else {
                @for (party of paginatedParties(); track party.id) {
                  <tr bmTableRow>
                    <td bmTableCell>
                      <strong>{{ party.name }}</strong>
                      @if (party.tradeName) {
                        <div class="text-muted text-sm">{{ party.tradeName }}</div>
                      }
                    </td>
                    <td bmTableCell>
                      @if (party.partyType === 1) {
                        <span bmBadge="info">Customer</span>
                      } @else if (party.partyType === 2) {
                        <span bmBadge="warning">Supplier</span>
                      } @else {
                        <span bmBadge="neutral">Both</span>
                      }
                    </td>
                    <td bmTableCell>
                      @if (party.gstin) {
                        <code>{{ party.gstin }}</code>
                      } @else {
                        <span class="text-muted">Unregistered (B2C)</span>
                      }
                    </td>
                    <td bmTableCell>{{ party.stateName }} ({{ party.stateCode }})</td>
                    <td bmTableCell>
                      <div><i class="pi pi-phone text-muted mr-1"></i> {{ party.phone }}</div>
                      <small class="text-muted">{{ party.city }}</small>
                    </td>
                    <td bmTableCell bmAlign="right">₹ {{ party.creditLimit | number:'1.2-2' }}</td>
                    <td bmTableCell bmAlign="right">
                      <span [class.text-danger]="party.currentBalance > 0" [class.text-success]="party.currentBalance <= 0">
                        <strong>₹ {{ party.currentBalance | number:'1.2-2' }}</strong>
                      </span>
                    </td>
                    <td bmTableCell bmAlign="center">
                      <div class="btn-group">
                        <button bmButton bmVariant="outline" bmSize="sm" (click)="editParty(party)" title="Edit Party">
                          <i class="pi pi-pencil"></i>
                        </button>
                        <button bmButton bmVariant="danger" bmSize="sm" (click)="deleteParty(party.id)" title="Deactivate Party">
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
        <bm-pagination
          [totalItems]="mastersService.parties().length"
          [(currentPage)]="currentPage"
          [(pageSize)]="pageSize"
          itemName="parties">
        </bm-pagination>
      </div>

      <!-- Add/Edit Modal Dialog -->
      @if (showModal()) {
        <div class="modal-backdrop">
          <div bmPanel class="modal-dialog">
            <div bmPanelHeader>
              <span><i class="pi pi-user mr-2"></i> {{ editingId() ? 'Edit Party Details' : 'Add Customer / Supplier' }}</span>
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
                  <label class="form-label required">Relationship Type</label>
                  <select bmInput [(ngModel)]="form.partyType">
                    <option [ngValue]="1">Customer (Debtor / Client)</option>
                    <option [ngValue]="2">Supplier (Creditor / Vendor)</option>
                    <option [ngValue]="3">Both (Customer & Supplier)</option>
                  </select>
                </div>
                <div class="col-6">
                  <label class="form-label required">Party Legal / Trade Name</label>
                  <input bmInput type="text" [(ngModel)]="form.name" placeholder="e.g. Reliance Retail Limited" />
                </div>
                <div class="col-6">
                  <label class="form-label">GSTIN (Optional for B2C)</label>
                  <input bmInput type="text" maxlength="15" [(ngModel)]="form.gstin" (ngModelChange)="onGstinChange($event)" placeholder="e.g. 24AABCR1234F1Z1" />
                </div>
                <div class="col-6">
                  <label class="form-label required">Place of Supply State</label>
                  <select bmInput [(ngModel)]="form.stateCode" (ngModelChange)="onStateChange($event)">
                    @for (s of states; track s.code) {
                      <option [value]="s.code">{{ s.code }} - {{ s.name }}</option>
                    }
                  </select>
                </div>
                <div class="col-12">
                  <label class="form-label required">Billing Address Line 1</label>
                  <input bmInput type="text" [(ngModel)]="form.addressLine1" placeholder="Building, Street, Area" />
                </div>
                <div class="col-4">
                  <label class="form-label required">City</label>
                  <input bmInput type="text" [(ngModel)]="form.city" placeholder="City" />
                </div>
                <div class="col-4">
                  <label class="form-label required">PIN Code (6 Digits)</label>
                  <input bmInput type="text" maxlength="6" [(ngModel)]="form.pincode" placeholder="380001" />
                </div>
                <div class="col-4">
                  <label class="form-label required">Mobile Number</label>
                  <input bmInput type="tel" maxlength="15" [(ngModel)]="form.phone" placeholder="9876543210" />
                </div>
                <div class="col-6">
                  <label class="form-label">Credit Limit (₹)</label>
                  <input bmInput type="number" [(ngModel)]="form.creditLimit" placeholder="0.00" />
                </div>
                <div class="col-6">
                  <label class="form-label">Official Email</label>
                  <input bmInput type="email" [(ngModel)]="form.email" placeholder="billing@party.com" />
                </div>
              </div>
            </div>
            <div bmPanelFooter>
              <div class="modal-footer-btns">
                <button bmButton bmVariant="secondary" (click)="closeModal()">Cancel</button>
                <button bmButton bmVariant="primary" (click)="saveParty()">
                  <i class="pi pi-check"></i> Save Party
                </button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .master-container { padding: 1.5rem; }
    .master-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; }
    .master-title { font-size: 1.35rem; font-weight: 700; color: var(--bm-text-primary); margin: 0; }
    .master-subtitle { font-size: 0.85rem; color: var(--bm-text-secondary); margin-top: 0.2rem; }
    .master-actions { display: flex; gap: 0.75rem; }
    .toolbar-panel { padding: 0.75rem 1rem; }
    .toolbar-grid { display: flex; align-items: center; gap: 1rem; }
    .search-wrap { flex: 1; }
    .filter-wrap { width: 200px; }
    .filter-count { font-weight: 600; }
    .p-0 { padding: 0 !important; }
    .mb-3 { margin-bottom: 1rem; }
    .mr-1 { margin-right: 0.25rem; }
    .mr-2 { margin-right: 0.5rem; }
    .btn-group { display: flex; gap: 0.35rem; justify-content: center; }
    .empty-state { text-align: center; padding: 3rem 1rem; }
    .empty-icon { font-size: 3rem; color: #94a3b8; margin-bottom: 1rem; display: block; }

    .modal-backdrop {
      position: fixed; top: 0; left: 0; width: 100%; height: 100%;
      background: rgba(15, 23, 42, 0.6);
      display: flex; align-items: center; justify-content: center; z-index: 1000;
    }
    .modal-dialog {
      width: 100%; max-width: 680px; background: #ffffff;
      border-radius: var(--bm-radius-lg); box-shadow: 0 20px 25px -5px rgba(0,0,0,0.2);
    }
    .close-btn { background: transparent; border: none; font-size: 1.1rem; cursor: pointer; color: #64748b; }
    .modal-footer-btns { display: flex; justify-content: flex-end; gap: 0.75rem; }
    .form-grid { display: grid; grid-template-columns: repeat(12, 1fr); gap: 1rem; }
    .col-12 { grid-column: span 12; }
    .col-6 { grid-column: span 6; }
    .col-4 { grid-column: span 4; }
    .form-label { display: block; font-size: 0.8rem; font-weight: 600; margin-bottom: 0.35rem; color: #334155; }
    .form-label.required::after { content: ' *'; color: #ef4444; }
    .alert-box.error { background: #fef2f2; border: 1px solid #fecaca; color: #b91c1c; padding: 0.75rem; border-radius: 6px; }
  `]
})
export class PartyListComponent implements OnInit {
  protected readonly mastersService = inject(MastersService);
  protected readonly dialogService = inject(DialogService);

  public states = INDIAN_STATES;
  public searchQuery = '';
  public selectedType: number | null = null;
  public showModal = signal<boolean>(false);
  public editingId = signal<string | null>(null);
  public modalError = signal<string | null>(null);

  public currentPage = signal<number>(1);
  public pageSize = signal<number>(10);

  public paginatedParties = computed(() => {
    const parties = this.mastersService.parties();
    const start = (this.currentPage() - 1) * this.pageSize();
    return parties.slice(start, start + this.pageSize());
  });

  public form: CreateOrUpdatePartyRequest = this.getEmptyForm();

  ngOnInit(): void {
    this.loadParties();
  }

  loadParties(): void {
    this.currentPage.set(1);
    this.mastersService.loadParties(this.selectedType ?? undefined, this.searchQuery).subscribe();
  }

  onSearchChange(val: string): void {
    this.searchQuery = val;
    this.currentPage.set(1);
    this.loadParties();
  }

  onGstinChange(val: string): void {
    if (!val) return;
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
  }

  onStateChange(code: string): void {
    const match = this.states.find(s => s.code === code);
    if (match) {
      this.form.stateName = match.name;
    }
  }

  openCreateModal(): void {
    this.editingId.set(null);
    this.form = this.getEmptyForm();
    this.modalError.set(null);
    this.showModal.set(true);
  }

  editParty(party: PartyDto): void {
    this.editingId.set(party.id);
    this.form = {
      partyType: party.partyType,
      name: party.name,
      tradeName: party.tradeName,
      gstin: party.gstin,
      gstRegistrationType: party.gstRegistrationType,
      stateCode: party.stateCode,
      stateName: party.stateName,
      addressLine1: party.addressLine1,
      addressLine2: party.addressLine2,
      city: party.city,
      pincode: party.pincode,
      phone: party.phone,
      email: party.email,
      creditLimit: party.creditLimit
    };
    this.modalError.set(null);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
  }

  saveParty(): void {
    this.modalError.set(null);
    if (!this.form.name.trim()) {
      this.modalError.set('Party name is required.');
      return;
    }
    if (!this.form.phone.trim()) {
      this.modalError.set('Phone number is required.');
      return;
    }

    const id = this.editingId();
    if (id) {
      this.mastersService.updateParty(id, this.form).subscribe({
        next: () => this.closeModal(),
        error: (err) => this.modalError.set(err?.error?.message || err?.message || 'Update failed.')
      });
    } else {
      this.mastersService.createParty(this.form).subscribe({
        next: () => this.closeModal(),
        error: (err) => this.modalError.set(err?.error?.message || err?.message || 'Creation failed.')
      });
    }
  }

  async deleteParty(id: string): Promise<void> {
    const confirmed = await this.dialogService.confirm(
      'Deactivate Party',
      'Are you sure you want to mark this customer/supplier as inactive?',
      'Deactivate',
      true
    );
    if (confirmed) {
      this.mastersService.deleteParty(id).subscribe({
        next: () => this.dialogService.toast('Party marked as inactive.', 'info')
      });
    }
  }

  openQuickSeed(): void {
    const sampleParties: CreateOrUpdatePartyRequest[] = [
      { partyType: 1, name: 'Metro Cash & Carry India Pvt Ltd', tradeName: 'Metro Wholesale', gstin: '24AAACM1234F1Z2', gstRegistrationType: 1, stateCode: '24', stateName: 'Gujarat', addressLine1: 'Plot 12, GIDC Estate', city: 'Ahmedabad', pincode: '382445', phone: '9825012345', email: 'accounts@metroindia.com', creditLimit: 250000 },
      { partyType: 1, name: 'Sharma Kirana Store', tradeName: 'Sharma General', gstin: '', gstRegistrationType: 3, stateCode: '24', stateName: 'Gujarat', addressLine1: 'Shop 4, Anand Nagar', city: 'Surat', pincode: '395001', phone: '9426098765', email: 'sharma.kirana@gmail.com', creditLimit: 50000 },
      { partyType: 2, name: 'Godrej Agrovet Agro Supplies', tradeName: 'Godrej Agro', gstin: '27AAACG0000A1Z5', gstRegistrationType: 1, stateCode: '27', stateName: 'Maharashtra', addressLine1: 'Eastern Express Highway, Vikhroli', city: 'Mumbai', pincode: '400079', phone: '9820011223', email: 'vendorcare@godrejagro.com', creditLimit: 500000 }
    ];

    sampleParties.forEach(p => {
      this.mastersService.createParty(p).subscribe();
    });
  }

  private getEmptyForm(): CreateOrUpdatePartyRequest {
    return {
      partyType: 1,
      name: '',
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
      creditLimit: 0
    };
  }
}

