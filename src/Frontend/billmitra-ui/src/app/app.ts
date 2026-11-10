import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SystemService } from './core/services/system.service';
import {
  BmButtonDirective,
  BmPanelDirective,
  BmPanelHeaderDirective,
  BmPanelBodyDirective,
  BmPanelFooterDirective,
  BmTableDirective,
  BmTableRowDirective,
  BmTableCellDirective,
  BmInputDirective,
  BmBadgeDirective
} from './shared/directives';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    BmButtonDirective,
    BmPanelDirective,
    BmPanelHeaderDirective,
    BmPanelBodyDirective,
    BmPanelFooterDirective,
    BmTableDirective,
    BmTableRowDirective,
    BmTableCellDirective,
    BmBadgeDirective
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly systemService = inject(SystemService);
  protected isSidebarCollapsed = false;
  protected activeMenu = 'dashboard';

  ngOnInit(): void {
    // Check initial health on load
    this.systemService.checkHealth();
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  setActiveMenu(menu: string): void {
    this.activeMenu = menu;
  }
}
