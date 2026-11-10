import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SystemService } from './core/services/system.service';
import { SetupService } from './core/services/setup.service';
import { SetupWizardComponent } from './features/setup/setup-wizard.component';
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
  }

  onSetupCompleted(): void {
    this.refreshAll();
  }
}
