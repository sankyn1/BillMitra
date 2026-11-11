import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
  inject,
  ElementRef,
  HostListener
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GridColumnDef, GridExportFormat } from '../models/grid-column.model';
import { GridExportService } from '../services/grid-export.service';

@Component({
  selector: 'bm-grid-toolbar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bm-grid-toolbar">
      <!-- Left: Search and Total Count -->
      <div class="bm-grid-toolbar-left">
        <div class="bm-grid-search-wrap" *ngIf="showSearch">
          <i class="pi pi-search bm-search-icon"></i>
          <input
            type="text"
            class="bm-grid-search-input"
            [placeholder]="searchPlaceholder"
            [(ngModel)]="searchTerm"
            (ngModelChange)="onSearchChange($event)" />
          <button
            *ngIf="searchTerm"
            type="button"
            class="bm-search-clear-btn"
            (click)="clearSearch()"
            title="Clear Search">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <div class="bm-grid-count-badge" *ngIf="totalCount !== undefined">
          <span>{{ totalCount }} {{ itemName }}</span>
        </div>
      </div>

      <!-- Right: Action Buttons, Column Customizer, Centralized Export -->
      <div class="bm-grid-toolbar-right">
        <!-- Content Projected Custom Action Buttons -->
        <ng-content select="[toolbar-actions]"></ng-content>

        <!-- Column Customization Chooser Dropdown -->
        <div class="bm-grid-dropdown-wrap" *ngIf="showColumnChooser && columns.length > 0">
          <button
            type="button"
            class="bm-grid-btn"
            [class.open]="isColumnMenuOpen"
            (click)="toggleColumnMenu($event)"
            title="Customize Visible Columns">
            <i class="pi pi-table"></i>
            <span class="bm-btn-label">Columns</span>
            <span class="bm-col-badge">{{ visibleCount }}/{{ columns.length }}</span>
            <i class="pi pi-chevron-down bm-chevron"></i>
          </button>

          <!-- Columns Dropdown Menu -->
          <div class="bm-dropdown-panel bm-column-panel" *ngIf="isColumnMenuOpen" (click)="$event.stopPropagation()">
            <div class="bm-panel-header">
              <span class="bm-panel-title">Visible Columns</span>
              <button type="button" class="bm-link-btn" (click)="resetColumns()">Reset</button>
            </div>

            <!-- Search filter if columns > 6 -->
            <div class="bm-column-search-wrap" *ngIf="columns.length > 6">
              <input
                type="text"
                class="bm-column-search-input"
                placeholder="Filter columns..."
                [(ngModel)]="columnSearchTerm" />
            </div>

            <div class="bm-column-list">
              @for (col of filteredColumns; track col.key) {
                <label class="bm-column-item" [class.locked]="col.locked">
                  <input
                    type="checkbox"
                    [checked]="col.visible !== false"
                    [disabled]="col.locked"
                    (change)="toggleColumn(col, $event)" />
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

        <!-- Centralized Export Menu -->
        <div class="bm-grid-dropdown-wrap" *ngIf="showExport">
          <button
            type="button"
            class="bm-grid-btn bm-btn-export"
            [class.open]="isExportMenuOpen"
            (click)="toggleExportMenu($event)"
            title="Export Records">
            <i class="pi pi-download"></i>
            <span class="bm-btn-label">Export</span>
            <i class="pi pi-chevron-down bm-chevron"></i>
          </button>

          <!-- Export Options Menu -->
          <div class="bm-dropdown-panel bm-export-panel" *ngIf="isExportMenuOpen" (click)="$event.stopPropagation()">
            <div class="bm-panel-header">
              <span class="bm-panel-title">Export Options</span>
            </div>
            <div class="bm-export-list">
              <button type="button" class="bm-export-option" (click)="triggerExport('csv')">
                <i class="pi pi-file-excel text-emerald"></i>
                <div>
                  <div class="bm-opt-title">Export as CSV (.csv)</div>
                  <div class="bm-opt-sub">Compatible with Excel & Sheets</div>
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
                  <div class="bm-opt-sub">Clean printer-friendly page</div>
                </div>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bm-grid-toolbar {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      margin-bottom: 0.85rem;
      box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.04);
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

      &.open .bm-chevron { transform: rotate(180deg); }
    }

    .bm-col-badge {
      background: #e2e8f0;
      color: #334155;
      font-size: 0.7rem;
      font-weight: 700;
      padding: 0.1rem 0.35rem;
      border-radius: 4px;
    }

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

    .bm-export-panel { width: 250px; }
    .bm-export-list { padding: 0.4rem 0; }

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

      .bm-opt-title { font-size: 0.825rem; font-weight: 600; color: #0f172a; }
      .bm-opt-sub { font-size: 0.7rem; color: #64748b; }
    }

    .text-emerald { color: #059669; }
    .text-primary { color: #1e3a8a; }
    .text-indigo { color: #4f46e5; }

    @media (max-width: 640px) {
      .bm-grid-toolbar { flex-direction: column; align-items: stretch; }
      .bm-grid-toolbar-left, .bm-grid-toolbar-right { width: 100%; justify-content: space-between; }
      .bm-grid-search-wrap { max-width: none; }
      .bm-btn-label { display: none; }
    }
  `]
})
export class BmGridToolbarComponent implements OnInit, OnChanges {
  private exportService = inject(GridExportService);
  private elementRef = inject(ElementRef);

  @Input({ required: true }) gridId!: string;
  @Input({ required: true }) columns: GridColumnDef[] = [];
  @Input() data: any[] = [];
  @Input() title = 'Export';
  @Input() totalCount?: number;
  @Input() itemName = 'records';
  @Input() showSearch = true;
  @Input() searchPlaceholder = 'Search records...';
  @Input() searchTerm = '';
  @Input() showExport = true;
  @Input() showColumnChooser = true;

  @Output() searchTermChange = new EventEmitter<string>();
  @Output() columnsChange = new EventEmitter<GridColumnDef[]>();
  @Output() exportTriggered = new EventEmitter<GridExportFormat>();

  isColumnMenuOpen = false;
  isExportMenuOpen = false;
  columnSearchTerm = '';

  ngOnInit(): void {
    this.loadPersistedColumnSettings();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['columns']) {
      this.loadPersistedColumnSettings();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isColumnMenuOpen = false;
      this.isExportMenuOpen = false;
    }
  }

  get visibleCount(): number {
    return this.columns.filter(c => c.visible !== false).length;
  }

  get filteredColumns(): GridColumnDef[] {
    if (!this.columnSearchTerm) return this.columns;
    const term = this.columnSearchTerm.toLowerCase();
    return this.columns.filter(c => c.header.toLowerCase().includes(term));
  }

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

  toggleColumn(col: GridColumnDef, event: Event): void {
    if (col.locked) return;
    const target = event.target as HTMLInputElement;
    col.visible = target.checked;
    this.savePersistedColumnSettings();
    this.columnsChange.emit(this.columns);
  }

  showAllColumns(): void {
    this.columns.forEach(c => c.visible = true);
    this.savePersistedColumnSettings();
    this.columnsChange.emit(this.columns);
  }

  resetColumns(): void {
    try {
      localStorage.removeItem(`bm_grid_cols_${this.gridId}`);
    } catch {}
    this.columns.forEach(c => c.visible = true);
    this.columnsChange.emit(this.columns);
  }

  onSearchChange(val: string): void {
    this.searchTermChange.emit(val);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.searchTermChange.emit('');
  }

  triggerExport(format: GridExportFormat): void {
    this.isExportMenuOpen = false;
    this.exportTriggered.emit(format);

    switch (format) {
      case 'csv':
        this.exportService.exportToCsv(this.title, this.columns, this.data);
        break;
      case 'excel':
        this.exportService.exportToExcel(this.title, this.columns, this.data);
        break;
      case 'print':
        this.exportService.printGrid(this.title, this.columns, this.data);
        break;
    }
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
}
