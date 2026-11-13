import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
  ContentChild,
  TemplateRef,
  inject,
  HostListener,
  ElementRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GridColumnDef, GridSortState, GridExportFormat } from '../models/grid-column.model';
import { GridExportService } from '../services/grid-export.service';
import { BmPaginationComponent } from './bm-pagination.component';

@Component({
  selector: 'bm-data-grid',
  standalone: true,
  imports: [CommonModule, FormsModule, BmPaginationComponent],
  template: `
    <div class="bm-grid-wrapper" [class.bm-grid-compact]="isCompact">
      <!-- Grid Top Toolbar -->
      <div class="bm-grid-toolbar">
        <!-- Left: Search and Filter -->
        <div class="bm-grid-toolbar-left">
          <div class="bm-grid-search-wrap" *ngIf="showSearch">
            <i class="pi pi-search bm-search-icon"></i>
            <input
              type="text"
              class="bm-grid-search-input"
              [placeholder]="searchPlaceholder"
              [(ngModel)]="searchTerm"
              (ngModelChange)="onSearchInput($event)" />
            <button
              *ngIf="searchTerm"
              type="button"
              class="bm-search-clear-btn"
              (click)="clearSearch()"
              title="Clear Search">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <div class="bm-grid-count-badge" *ngIf="displayTotalCount > 0">
            <span>{{ displayTotalCount }} {{ itemName }}</span>
          </div>
        </div>

        <!-- Right: Actions, Column Chooser, Export, Density -->
        <div class="bm-grid-toolbar-right">
          <!-- Content Projected Custom Action Buttons (e.g. + Add New) -->
          <ng-content select="[toolbar-actions]"></ng-content>

          <!-- Density Toggle -->
          <button
            *ngIf="showDensityToggle"
            type="button"
            class="bm-grid-btn bm-density-btn"
            [class.active]="isCompact"
            (click)="toggleDensity()"
            title="Toggle Row Compactness">
            <i class="pi" [ngClass]="isCompact ? 'pi-bars' : 'pi-align-justify'"></i>
          </button>

          <!-- Column Customization Chooser Dropdown -->
          <div class="bm-grid-dropdown-wrap" *ngIf="showColumnChooser">
            <button
              type="button"
              class="bm-grid-btn"
              [class.open]="isColumnMenuOpen"
              (click)="toggleColumnMenu($event)"
              title="Customize Columns">
              <i class="pi pi-table"></i>
              <span class="bm-btn-label">Columns</span>
              <span class="bm-col-badge">{{ visibleColumnCount }}/{{ columns.length }}</span>
              <i class="pi pi-chevron-down bm-chevron"></i>
            </button>

            <!-- Columns Dropdown Menu -->
            <div class="bm-dropdown-panel bm-column-panel" *ngIf="isColumnMenuOpen" (click)="$event.stopPropagation()">
              <div class="bm-panel-header">
                <span class="bm-panel-title">Visible Columns</span>
                <button type="button" class="bm-link-btn" (click)="resetColumnsToDefault()">Reset</button>
              </div>

              <!-- Quick Column Search if > 6 columns -->
              <div class="bm-column-search-wrap" *ngIf="columns.length > 6">
                <input
                  type="text"
                  class="bm-column-search-input"
                  placeholder="Filter columns..."
                  [(ngModel)]="columnSearchTerm" />
              </div>

              <div class="bm-column-list">
                @for (col of filteredColumnDefs; track col.key) {
                  <label class="bm-column-item" [class.locked]="col.locked">
                    <input
                      type="checkbox"
                      [checked]="col.visible !== false"
                      [disabled]="col.locked"
                      (change)="toggleColumnVisibility(col, $event)" />
                    <span class="bm-col-name">{{ col.header }}</span>
                    <span *ngIf="col.locked" class="bm-locked-tag">Required</span>
                  </label>
                }
              </div>

              <div class="bm-panel-footer">
                <button type="button" class="bm-btn-subtle" (click)="showAllColumns()">Show All</button>
                <button type="button" class="bm-btn-primary-sm" (click)="isColumnMenuOpen = false">Done</button>
              </div>
            </div>
          </div>

          <!-- Centralized Export Dropdown Menu -->
          <div class="bm-grid-dropdown-wrap" *ngIf="showExport">
            <button
              type="button"
              class="bm-grid-btn bm-btn-export"
              [class.open]="isExportMenuOpen"
              (click)="toggleExportMenu($event)"
              title="Export Grid Data">
              <i class="pi pi-download"></i>
              <span class="bm-btn-label">Export</span>
              <i class="pi pi-chevron-down bm-chevron"></i>
            </button>

            <!-- Export Options Panel -->
            <div class="bm-dropdown-panel bm-export-panel" *ngIf="isExportMenuOpen" (click)="$event.stopPropagation()">
              <div class="bm-panel-header">
                <span class="bm-panel-title">Export Records</span>
              </div>
              <div class="bm-export-list">
                <button type="button" class="bm-export-option" (click)="triggerExport('csv')">
                  <i class="pi pi-file-excel text-emerald"></i>
                  <div>
                    <div class="bm-opt-title">Export as CSV (.csv)</div>
                    <div class="bm-opt-sub">Universal format for Excel & Sheets</div>
                  </div>
                </button>

                <button type="button" class="bm-export-option" (click)="triggerExport('excel')">
                  <i class="pi pi-file text-primary"></i>
                  <div>
                    <div class="bm-opt-title">Export as Excel (.xls)</div>
                    <div class="bm-opt-sub">Formatted XML Spreadsheet</div>
                  </div>
                </button>

                <button type="button" class="bm-export-option" (click)="triggerExport('print')">
                  <i class="pi pi-print text-indigo"></i>
                  <div>
                    <div class="bm-opt-title">Print View / PDF</div>
                    <div class="bm-opt-sub">Printer-optimized clean layout</div>
                  </div>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Main Data Table Container -->
      <div class="bm-grid-table-container">
        <!-- Loading Overlay -->
        <div class="bm-grid-loading-overlay" *ngIf="loading">
          <div class="bm-loading-spinner">
            <i class="pi pi-spin pi-spinner text-primary"></i>
            <span>Loading {{ itemName }}...</span>
          </div>
        </div>

        <table class="bm-grid-table">
          <thead>
            <tr>
              @for (col of activeColumns; track col.key) {
                <th
                  class="bm-grid-th"
                  [ngClass]="'bm-align-' + (col.align || 'left')"
                  [style.width]="col.width || 'auto'"
                  [class.sortable]="col.sortable !== false"
                  (click)="onHeaderSort(col)">
                  <div class="bm-th-content">
                    <span>{{ col.header }}</span>
                    @if (col.sortable !== false) {
                      <span class="bm-sort-indicator">
                        @if (sortState.column === col.key) {
                          <i class="pi" [ngClass]="sortState.direction === 'asc' ? 'pi-sort-amount-up-alt text-primary' : 'pi-sort-amount-down text-primary'"></i>
                        } @else {
                          <i class="pi pi-sort-alt bm-sort-inactive"></i>
                        }
                      </span>
                    }
                  </div>
                </th>
              }
            </tr>
          </thead>

          <tbody>
            @if (displayedRows.length === 0 && !loading) {
              <tr>
                <td [attr.colspan]="activeColumns.length" class="bm-empty-cell">
                  <div class="bm-empty-state">
                    <i class="pi pi-inbox bm-empty-icon"></i>
                    <div class="bm-empty-title">{{ emptyMessage || 'No records found' }}</div>
                    <div class="bm-empty-subtitle" *ngIf="searchTerm">
                      No results match your search "{{ searchTerm }}".
                    </div>
                  </div>
                </td>
              </tr>
            } @else {
              @for (row of displayedRows; track trackByFn(row, $index)) {
                <tr
                  class="bm-grid-tr"
                  [class.clickable]="isRowClickable"
                  (click)="onRowClick(row)">
                  @for (col of activeColumns; track col.key) {
                    <td
                      class="bm-grid-td"
                      [ngClass]="'bm-align-' + (col.align || 'left')">
                      <!-- Custom Cell Template If Provided -->
                      @if (cellTemplate) {
                        <ng-container
                          *ngTemplateOutlet="cellTemplate; context: { $implicit: row, row: row, column: col, value: row[col.key] }">
                        </ng-container>
                      } @else {
                        <span>{{ getFormattedValue(row, col) }}</span>
                      }
                    </td>
                  }
                </tr>
              }
            }
          </tbody>
        </table>
      </div>

      <!-- Pagination Footer -->
      <div class="bm-grid-footer">
        <bm-pagination
          [totalItems]="displayTotalCount"
          [(currentPage)]="currentPage"
          [(pageSize)]="pageSize"
          [pageSizeOptions]="pageSizeOptions"
          [itemName]="itemName"
          (currentPageChange)="onPageChange($event)"
          (pageSizeChange)="onPageSizeChange($event)">
        </bm-pagination>
      </div>
    </div>
  `,
  styles: [`
    .bm-grid-wrapper {
      display: flex;
      flex-direction: column;
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      overflow: visible;
      box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.05);
      position: relative;
    }

    /* Toolbar */
    .bm-grid-toolbar {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: center;
      gap: 0.75rem;
      padding: 0.85rem 1rem;
      background: #ffffff;
      border-bottom: 1px solid #e2e8f0;
      border-radius: 8px 8px 0 0;
    }

    .bm-grid-toolbar-left {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex: 1;
      min-width: 240px;
    }

    .bm-grid-search-wrap {
      position: relative;
      display: flex;
      align-items: center;
      width: 100%;
      max-width: 320px;

      .bm-search-icon {
        position: absolute;
        left: 0.75rem;
        font-size: 0.85rem;
        color: #94a3b8;
        pointer-events: none;
      }

      .bm-grid-search-input {
        width: 100%;
        padding: 0.45rem 2rem 0.45rem 2.2rem;
        font-size: 0.825rem;
        border: 1px solid #cbd5e1;
        border-radius: 6px;
        background: #f8fafc;
        color: #1e293b;
        transition: all 0.15s ease;

        &:focus {
          outline: none;
          background: #ffffff;
          border-color: #2563eb;
          box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.12);
        }
      }

      .bm-search-clear-btn {
        position: absolute;
        right: 0.5rem;
        background: transparent;
        border: none;
        color: #94a3b8;
        padding: 0.2rem;
        cursor: pointer;
        font-size: 0.75rem;

        &:hover { color: #475569; }
      }
    }

    .bm-grid-count-badge {
      font-size: 0.75rem;
      font-weight: 600;
      color: #64748b;
      background: #f1f5f9;
      padding: 0.25rem 0.6rem;
      border-radius: 12px;
      white-space: nowrap;
    }

    .bm-grid-toolbar-right {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      position: relative;
    }

    /* Buttons */
    .bm-grid-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      background: #ffffff;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      padding: 0.45rem 0.75rem;
      font-size: 0.825rem;
      font-weight: 600;
      color: #334155;
      cursor: pointer;
      transition: all 0.15s ease;

      &:hover {
        background: #f8fafc;
        border-color: #94a3b8;
        color: #0f172a;
      }

      &.open {
        background: #eff6ff;
        border-color: #3b82f6;
        color: #1e3a8a;
      }

      .bm-chevron {
        font-size: 0.65rem;
        margin-left: 0.15rem;
        transition: transform 0.15s ease;
      }

      &.open .bm-chevron {
        transform: rotate(180deg);
      }
    }

    .bm-col-badge {
      background: #e2e8f0;
      color: #334155;
      font-size: 0.7rem;
      font-weight: 700;
      padding: 0.1rem 0.35rem;
      border-radius: 4px;
    }

    .bm-density-btn {
      padding: 0.45rem 0.6rem;
      &.active {
        background: #f1f5f9;
        color: #1e3a8a;
        border-color: #3b82f6;
      }
    }

    /* Dropdown Menus */
    .bm-grid-dropdown-wrap {
      position: relative;
    }

    .bm-dropdown-panel {
      position: absolute;
      top: calc(100% + 6px);
      right: 0;
      z-index: 1050;
      background: #ffffff;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.15), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
      width: 280px;
      animation: bmDropdownIn 0.15s ease;
    }

    @keyframes bmDropdownIn {
      from { opacity: 0; transform: translateY(-4px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .bm-panel-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.65rem 0.9rem;
      border-bottom: 1px solid #e2e8f0;
      background: #f8fafc;
      border-radius: 8px 8px 0 0;

      .bm-panel-title {
        font-size: 0.8rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: #475569;
      }

      .bm-link-btn {
        background: none;
        border: none;
        font-size: 0.75rem;
        color: #2563eb;
        font-weight: 600;
        cursor: pointer;
        padding: 0;

        &:hover { text-decoration: underline; }
      }
    }

    /* Column Panel Specifics */
    .bm-column-search-wrap {
      padding: 0.5rem 0.75rem;
      border-bottom: 1px solid #f1f5f9;

      .bm-column-search-input {
        width: 100%;
        padding: 0.35rem 0.5rem;
        font-size: 0.775rem;
        border: 1px solid #e2e8f0;
        border-radius: 4px;
        outline: none;

        &:focus { border-color: #3b82f6; }
      }
    }

    .bm-column-list {
      max-height: 240px;
      overflow-y: auto;
      padding: 0.35rem 0;
    }

    .bm-column-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.4rem 0.9rem;
      font-size: 0.825rem;
      color: #1e293b;
      cursor: pointer;
      transition: background 0.1s ease;

      &:hover { background: #f8fafc; }

      &.locked {
        opacity: 0.75;
        cursor: not-allowed;
      }

      input[type="checkbox"] {
        cursor: pointer;
        accent-color: #1e3a8a;
      }

      .bm-col-name { flex: 1; }

      .bm-locked-tag {
        font-size: 0.65rem;
        font-weight: 700;
        text-transform: uppercase;
        color: #94a3b8;
        background: #f1f5f9;
        padding: 0.1rem 0.3rem;
        border-radius: 3px;
      }
    }

    .bm-panel-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.5rem 0.9rem;
      border-top: 1px solid #e2e8f0;
      background: #f8fafc;
      border-radius: 0 0 8px 8px;

      .bm-btn-subtle {
        background: transparent;
        border: none;
        font-size: 0.75rem;
        color: #64748b;
        font-weight: 600;
        cursor: pointer;
        &:hover { color: #0f172a; }
      }

      .bm-btn-primary-sm {
        background: #1e3a8a;
        color: #ffffff;
        border: none;
        border-radius: 4px;
        padding: 0.3rem 0.75rem;
        font-size: 0.75rem;
        font-weight: 600;
        cursor: pointer;
        &:hover { background: #1d4ed8; }
      }
    }

    /* Export Panel */
    .bm-export-panel {
      width: 250px;
    }

    .bm-export-list {
      padding: 0.4rem 0;
    }

    .bm-export-option {
      width: 100%;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.6rem 0.9rem;
      border: none;
      background: transparent;
      text-align: left;
      cursor: pointer;
      transition: background 0.1s ease;

      &:hover { background: #f1f5f9; }

      i { font-size: 1.15rem; }

      .bm-opt-title {
        font-size: 0.825rem;
        font-weight: 600;
        color: #0f172a;
      }

      .bm-opt-sub {
        font-size: 0.7rem;
        color: #64748b;
      }
    }

    /* Table Container */
    .bm-grid-table-container {
      overflow-x: auto;
      position: relative;
      min-height: 180px;
    }

    .bm-grid-loading-overlay {
      position: absolute;
      inset: 0;
      background: rgba(255, 255, 255, 0.75);
      backdrop-filter: blur(2px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 20;

      .bm-loading-spinner {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.85rem;
        font-weight: 600;
        color: #1e3a8a;

        i { font-size: 1.75rem; }
      }
    }

    .bm-grid-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.85rem;
    }

    .bm-grid-th {
      background: #f8fafc;
      color: #475569;
      font-weight: 700;
      font-size: 0.775rem;
      text-transform: uppercase;
      letter-spacing: 0.03em;
      padding: 0.75rem 1rem;
      border-bottom: 2px solid #e2e8f0;
      white-space: nowrap;
      user-select: none;

      &.sortable {
        cursor: pointer;
        &:hover {
          background: #f1f5f9;
          color: #1e3a8a;
        }
      }

      .bm-th-content {
        display: flex;
        align-items: center;
        gap: 0.35rem;
      }

      .bm-sort-indicator {
        font-size: 0.75rem;
        display: inline-flex;
        align-items: center;

        .bm-sort-inactive {
          color: #cbd5e1;
        }
      }
    }

    .bm-grid-tr {
      border-bottom: 1px solid #f1f5f9;
      transition: background 0.1s ease;

      &:nth-child(even) {
        background: #fcfcfd;
      }

      &:hover {
        background: #f8fafc;
      }

      &.clickable {
        cursor: pointer;
      }
    }

    .bm-grid-td {
      padding: 0.75rem 1rem;
      color: #1e293b;
      vertical-align: middle;
    }

    /* Compact View */
    .bm-grid-compact {
      .bm-grid-th {
        padding: 0.45rem 0.75rem;
        font-size: 0.725rem;
      }
      .bm-grid-td {
        padding: 0.45rem 0.75rem;
        font-size: 0.8rem;
      }
    }

    /* Alignment utility */
    .bm-align-left {
      text-align: left;
      .bm-th-content { justify-content: flex-start; }
    }
    .bm-align-center {
      text-align: center;
      .bm-th-content { justify-content: center; }
    }
    .bm-align-right {
      text-align: right;
      .bm-th-content { justify-content: flex-end; }
    }

    /* Empty state */
    .bm-empty-cell {
      padding: 3rem 1rem;
      text-align: center;
    }

    .bm-empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 0.35rem;

      .bm-empty-icon {
        font-size: 2.25rem;
        color: #94a3b8;
        margin-bottom: 0.5rem;
      }

      .bm-empty-title {
        font-size: 0.95rem;
        font-weight: 700;
        color: #334155;
      }

      .bm-empty-subtitle {
        font-size: 0.8rem;
        color: #64748b;
      }
    }

    /* Color helpers */
    .text-emerald { color: #059669; }
    .text-primary { color: #1e3a8a; }
    .text-indigo { color: #4f46e5; }

    /* Responsive */
    @media (max-width: 640px) {
      .bm-grid-toolbar {
        flex-direction: column;
        align-items: stretch;
      }
      .bm-grid-toolbar-left, .bm-grid-toolbar-right {
        width: 100%;
        justify-content: space-between;
      }
      .bm-grid-search-wrap {
        max-width: none;
      }
      .bm-btn-label {
        display: none;
      }
    }
  `]
})
export class BmDataGridComponent implements OnInit, OnChanges {
  private exportService = inject(GridExportService);
  private elementRef = inject(ElementRef);

  /** Unique ID to store column configurations in localStorage */
  @Input({ required: true }) gridId!: string;

  /** Column configuration definitions */
  @Input({ required: true }) columns: GridColumnDef[] = [];

  /** Full dataset (in client-side mode) or current page dataset (in server-side mode) */
  @Input() data: any[] = [];

  /** Name of item being displayed (e.g. 'parties', 'products', 'invoices') */
  @Input() itemName = 'items';

  /** Title used for exported file name or print document */
  @Input() exportTitle = 'Export';

  /** Whether the grid is in loading state */
  @Input() loading = false;

  /** Empty state message */
  @Input() emptyMessage = 'No records found.';

  /** Search input placeholder */
  @Input() searchPlaceholder = 'Search records...';

  /** Whether to show the top search bar */
  @Input() showSearch = true;

  /** Whether to show centralized export buttons */
  @Input() showExport = true;

  /** Whether to show column chooser */
  @Input() showColumnChooser = true;

  /** Whether to show density toggle */
  @Input() showDensityToggle = true;

  /** Server-side pagination mode toggle */
  @Input() serverSide = false;

  /** Total records count in server-side mode */
  @Input() totalRecords = 0;

  /** Current page index (1-based) */
  @Input() currentPage = 1;

  /** Page size */
  @Input() pageSize = 10;

  /** Page size options */
  @Input() pageSizeOptions = [10, 25, 50, 100];

  /** Whether rows can be clicked */
  @Input() isRowClickable = false;

  /** Template to override cell rendering */
  @ContentChild('cellTemplate') cellTemplate?: TemplateRef<any>;

  /** Outputs */
  @Output() currentPageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();
  @Output() searchChange = new EventEmitter<string>();
  @Output() sortChange = new EventEmitter<GridSortState>();
  @Output() rowClick = new EventEmitter<any>();

  // State
  searchTerm = '';
  columnSearchTerm = '';
  isCompact = false;
  isColumnMenuOpen = false;
  isExportMenuOpen = false;

  sortState: GridSortState = { column: '', direction: null };

  ngOnInit(): void {
    this.loadPersistedColumnSettings();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['columns']) {
      this.loadPersistedColumnSettings();
    }
  }

  // Close menus on outside click
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isColumnMenuOpen = false;
      this.isExportMenuOpen = false;
    }
  }

  // Active columns (visible only)
  get activeColumns(): GridColumnDef[] {
    return this.columns.filter(c => c.visible !== false);
  }

  get visibleColumnCount(): number {
    return this.activeColumns.length;
  }

  get filteredColumnDefs(): GridColumnDef[] {
    if (!this.columnSearchTerm) return this.columns;
    const term = this.columnSearchTerm.toLowerCase();
    return this.columns.filter(c => c.header.toLowerCase().includes(term));
  }

  // Client-side processed rows
  get displayedRows(): any[] {
    if (this.serverSide) {
      return this.data || [];
    }

    let result = [...(this.data || [])];

    // 1. Client-side search
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.trim().toLowerCase();
      const searchableCols = this.columns.filter(c => c.searchable !== false);
      result = result.filter(row => {
        return searchableCols.some(col => {
          const val = this.exportService.extractCellValue(row, col);
          return val !== null && val !== undefined && String(val).toLowerCase().includes(term);
        });
      });
    }

    // 2. Client-side sort
    if (this.sortState.column && this.sortState.direction) {
      const col = this.columns.find(c => c.key === this.sortState.column);
      const dir = this.sortState.direction === 'asc' ? 1 : -1;
      result.sort((a, b) => {
        const valA = col ? this.exportService.extractCellValue(a, col) : a[this.sortState.column];
        const valB = col ? this.exportService.extractCellValue(b, col) : b[this.sortState.column];

        if (valA === valB) return 0;
        if (valA === null || valA === undefined) return 1;
        if (valB === null || valB === undefined) return -1;

        if (typeof valA === 'number' && typeof valB === 'number') {
          return (valA - valB) * dir;
        }

        return String(valA).localeCompare(String(valB)) * dir;
      });
    }

    // 3. Client-side pagination
    const start = (this.currentPage - 1) * this.pageSize;
    return result.slice(start, start + this.pageSize);
  }

  get displayTotalCount(): number {
    if (this.serverSide) return this.totalRecords;
    if (!this.searchTerm.trim()) return this.data ? this.data.length : 0;

    const term = this.searchTerm.trim().toLowerCase();
    const searchableCols = this.columns.filter(c => c.searchable !== false);
    return (this.data || []).filter(row => {
      return searchableCols.some(col => {
        const val = this.exportService.extractCellValue(row, col);
        return val !== null && val !== undefined && String(val).toLowerCase().includes(term);
      });
    }).length;
  }

  // Formatting
  getFormattedValue(row: any, col: GridColumnDef): string {
    return this.exportService.extractCellValue(row, col);
  }

  trackByFn(item: any, index: number): any {
    return item?.id || item?.invoiceNumber || item?.code || index;
  }

  // Column Visibility Handlers
  toggleColumnMenu(event: Event): void {
    event.stopPropagation();
    this.isColumnMenuOpen = !this.isColumnMenuOpen;
    this.isExportMenuOpen = false;
  }

  toggleExportMenu(event: Event): void {
    event.stopPropagation();
    this.isExportMenuOpen = !this.isExportMenuOpen;
    this.isColumnMenuOpen = false;
  }

  toggleColumnVisibility(col: GridColumnDef, event: Event): void {
    if (col.locked) return;
    const target = event.target as HTMLInputElement;
    col.visible = target.checked;
    this.savePersistedColumnSettings();
  }

  showAllColumns(): void {
    this.columns.forEach(c => c.visible = true);
    this.savePersistedColumnSettings();
  }

  resetColumnsToDefault(): void {
    try {
      localStorage.removeItem(`bm_grid_cols_${this.gridId}`);
    } catch {}
    this.columns.forEach(c => c.visible = true);
  }

  private savePersistedColumnSettings(): void {
    if (!this.gridId) return;
    try {
      const visibilityMap: Record<string, boolean> = {};
      this.columns.forEach(c => {
        visibilityMap[c.key] = c.visible !== false;
      });
      localStorage.setItem(`bm_grid_cols_${this.gridId}`, JSON.stringify(visibilityMap));
    } catch {}
  }

  private loadPersistedColumnSettings(): void {
    if (!this.gridId) return;
    try {
      const raw = localStorage.getItem(`bm_grid_cols_${this.gridId}`);
      if (raw) {
        const visibilityMap = JSON.parse(raw);
        this.columns.forEach(c => {
          if (!c.locked && visibilityMap[c.key] !== undefined) {
            c.visible = Boolean(visibilityMap[c.key]);
          }
        });
      }
    } catch {}
  }

  // Search & Sort Handlers
  onSearchInput(val: string): void {
    this.currentPage = 1;
    this.currentPageChange.emit(1);
    this.searchChange.emit(val);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.onSearchInput('');
  }

  onHeaderSort(col: GridColumnDef): void {
    if (col.sortable === false) return;

    if (this.sortState.column !== col.key) {
      this.sortState = { column: col.key, direction: 'asc' };
    } else if (this.sortState.direction === 'asc') {
      this.sortState = { column: col.key, direction: 'desc' };
    } else {
      this.sortState = { column: '', direction: null };
    }

    this.currentPage = 1;
    this.currentPageChange.emit(1);
    this.sortChange.emit(this.sortState);
  }

  // Density Toggle
  toggleDensity(): void {
    this.isCompact = !this.isCompact;
  }

  // Centralized Export Trigger
  triggerExport(format: GridExportFormat): void {
    this.isExportMenuOpen = false;
    const title = this.exportTitle || this.itemName || 'Export';
    const rowsToExport = this.serverSide ? this.data : this.data;

    switch (format) {
      case 'csv':
        this.exportService.exportToCsv(title, this.columns, rowsToExport);
        break;
      case 'excel':
        this.exportService.exportToExcel(title, this.columns, rowsToExport);
        break;
      case 'print':
        this.exportService.printGrid(title, this.columns, rowsToExport);
        break;
    }
  }

  // Pagination Handlers
  onPageChange(page: number): void {
    this.currentPage = page;
    this.currentPageChange.emit(page);
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.pageSizeChange.emit(size);
  }

  onRowClick(row: any): void {
    if (this.isRowClickable) {
      this.rowClick.emit(row);
    }
  }
}

