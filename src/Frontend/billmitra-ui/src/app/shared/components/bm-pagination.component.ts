import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'bm-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bm-pagination-container">
      <!-- Left: Item counts & Page size selector -->
      <div class="bm-pagination-info">
        <span class="bm-pagination-range">
          Showing <strong>{{ startItem }}</strong> to <strong>{{ endItem }}</strong> of <strong>{{ totalItems }}</strong> {{ itemName }}
        </span>
        <div class="bm-pagination-size-wrap">
          <label class="bm-pagination-size-label">Per page:</label>
          <select
            class="bm-pagination-select"
            [ngModel]="pageSize"
            (ngModelChange)="onPageSizeSelect($event)">
            @for (opt of pageSizeOptions; track opt) {
              <option [value]="opt">{{ opt }}</option>
            }
          </select>
        </div>
      </div>

      <!-- Right: Navigation buttons -->
      <div class="bm-pagination-controls" *ngIf="totalPages > 1 || totalItems > pageSize">
        <!-- First Page -->
        <button
          type="button"
          class="bm-page-btn bm-nav-btn"
          [disabled]="currentPage <= 1"
          (click)="setPage(1)"
          title="First Page">
          <i class="pi pi-angle-double-left"></i>
        </button>

        <!-- Previous Page -->
        <button
          type="button"
          class="bm-page-btn bm-nav-btn"
          [disabled]="currentPage <= 1"
          (click)="setPage(currentPage - 1)"
          title="Previous Page">
          <i class="pi pi-angle-left"></i>
        </button>

        <!-- Page Numbers -->
        <div class="bm-page-numbers">
          @for (p of pagesList; track $index) {
            @if (p === -1) {
              <span class="bm-page-ellipsis">…</span>
            } @else {
              <button
                type="button"
                class="bm-page-btn bm-number-btn"
                [class.active]="p === currentPage"
                (click)="setPage(p)">
                {{ p }}
              </button>
            }
          }
        </div>

        <!-- Next Page -->
        <button
          type="button"
          class="bm-page-btn bm-nav-btn"
          [disabled]="currentPage >= totalPages"
          (click)="setPage(currentPage + 1)"
          title="Next Page">
          <i class="pi pi-angle-right"></i>
        </button>

        <!-- Last Page -->
        <button
          type="button"
          class="bm-page-btn bm-nav-btn"
          [disabled]="currentPage >= totalPages"
          (click)="setPage(totalPages)"
          title="Last Page">
          <i class="pi pi-angle-double-right"></i>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .bm-pagination-container {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      padding: 0.85rem 1.25rem;
      border-top: 1px solid var(--bm-border-subtle, #E2E8F0);
      background-color: #FFFFFF;
      border-bottom-left-radius: inherit;
      border-bottom-right-radius: inherit;
      font-size: 0.8125rem;
      color: var(--bm-text-secondary, #475569);
      user-select: none;
    }

    @media (min-width: 640px) {
      .bm-pagination-container {
        flex-direction: row;
        align-items: center;
        justify-content: space-between;
      }
    }

    .bm-pagination-info {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 1rem;
    }

    .bm-pagination-range strong {
      color: var(--bm-text-primary, #0F172A);
      font-weight: 600;
    }

    .bm-pagination-size-wrap {
      display: flex;
      align-items: center;
      gap: 0.4rem;
    }

    .bm-pagination-size-label {
      font-size: 0.75rem;
      color: var(--bm-text-muted, #64748B);
    }

    .bm-pagination-select {
      height: 28px !important;
      padding: 0.15rem 1.75rem 0.15rem 0.5rem !important;
      font-size: 0.75rem !important;
      border-radius: 4px !important;
      border: 1px solid var(--bm-border-strong, #CBD5E1) !important;
      background-position: right 0.4rem center !important;
      background-size: 0.75rem 0.75rem !important;
    }

    .bm-pagination-controls {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      flex-wrap: wrap;
    }

    .bm-page-numbers {
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }

    .bm-page-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 32px;
      height: 32px;
      padding: 0 0.35rem;
      border-radius: var(--bm-radius-md, 6px);
      border: 1px solid var(--bm-border-subtle, #E2E8F0);
      background-color: #FFFFFF;
      color: var(--bm-text-primary, #1E293B);
      font-size: 0.8125rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s ease-in-out;
    }

    .bm-page-btn:hover:not(:disabled) {
      background-color: var(--bm-secondary-light, #F1F5F9);
      border-color: var(--bm-border-strong, #CBD5E1);
      color: var(--bm-primary, #1E3A8A);
    }

    .bm-page-btn:disabled {
      opacity: 0.4;
      cursor: not-allowed;
      background-color: #F8FAFC;
    }

    .bm-page-btn.active {
      background-color: var(--bm-primary, #1E3A8A);
      border-color: var(--bm-primary, #1E3A8A);
      color: #FFFFFF;
      font-weight: 700;
      box-shadow: 0 1px 3px rgba(30, 58, 138, 0.25);
    }

    .bm-page-ellipsis {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      color: var(--bm-text-muted, #94A3B8);
      font-weight: bold;
    }

    @media (max-width: 480px) {
      .bm-number-btn:not(.active) {
        display: none;
      }
      .bm-page-ellipsis {
        display: none;
      }
    }
  `]
})
export class BmPaginationComponent {
  @Input() totalItems = 0;
  @Input() currentPage = 1;
  @Input() pageSize = 10;
  @Input() pageSizeOptions = [10, 25, 50, 100];
  @Input() itemName = 'records';

  @Output() currentPageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();
  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalItems / this.pageSize));
  }

  get startItem(): number {
    if (this.totalItems === 0) return 0;
    return (this.currentPage - 1) * this.pageSize + 1;
  }

  get endItem(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalItems);
  }

  get pagesList(): number[] {
    const total = this.totalPages;
    const current = this.currentPage;

    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current <= 4) {
      return [1, 2, 3, 4, 5, -1, total];
    }

    if (current >= total - 3) {
      return [1, -1, total - 4, total - 3, total - 2, total - 1, total];
    }

    return [1, -1, current - 1, current, current + 1, -1, total];
  }

  setPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) return;
    this.currentPage = page;
    this.currentPageChange.emit(page);
    this.pageChange.emit(page);
  }

  onPageSizeSelect(newSize: string | number): void {
    const size = Number(newSize);
    if (size > 0 && size !== this.pageSize) {
      this.pageSize = size;
      this.pageSizeChange.emit(size);
      this.currentPage = 1;
      this.currentPageChange.emit(1);
      this.pageChange.emit(1);
    }
  }
}
