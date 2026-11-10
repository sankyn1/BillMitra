import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SystemService } from './core/services/system.service';
import { SetupService } from './core/services/setup.service';
import { SetupWizardComponent } from './features/setup/setup-wizard.component';
import { ProductListComponent } from './features/masters/product-list.component';
import { PartyListComponent } from './features/masters/party-list.component';
import { InvoiceListComponent } from './features/sales/invoice-list.component';
import { InvoiceEditorComponent } from './features/sales/invoice-editor.component';
import { PaymentListComponent } from './features/payments/payment-list.component';
import { PartyLedgerComponent } from './features/payments/party-ledger.component';
import { BmDialogComponent } from './shared/components/bm-dialog.component';
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
  BmBadgeDirective
} from './shared/directives';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    SetupWizardComponent,
    ProductListComponent,
    PartyListComponent,
    InvoiceListComponent,
    InvoiceEditorComponent,
    PaymentListComponent,
    PartyLedgerComponent,
    BmDialogComponent,
    BmButtonDirective,
    BmPanelDirective,
    BmPanelHeaderDirective,
    BmPanelBodyDirective,
    BmPanelFooterDirective,
    BmTableDirective,
    BmTableHeaderCellDirective,
    BmTableRowDirective,
    BmTableCellDirective,
    BmBadgeDirective
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly systemService = inject(SystemService);
  protected readonly setupService = inject(SetupService);

  protected isSidebarCollapsed = false;
  protected activeMenu = 'dashboard';
  protected activeInvoiceId: string | null = null;

  ngOnInit(): void {
    this.refreshAll();
  }

  refreshAll(): void {
    this.systemService.checkHealth();
    this.setupService.checkStatus().subscribe();
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  setActiveMenu(menu: string): void {
    this.activeMenu = menu;
    if (menu !== 'new-invoice') {
      this.activeInvoiceId = null;
    }
  }

  openNewInvoice(): void {
    this.activeInvoiceId = null;
    this.activeMenu = 'new-invoice';
  }

  openEditInvoice(id: string): void {
    this.activeInvoiceId = id;
    this.activeMenu = 'new-invoice';
  }

  onSetupCompleted(): void {
    this.refreshAll();
  }
}
