import { Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { SalesService } from '../../core/services/sales.service';
import { DialogService } from '../../shared/services/dialog.service';
import { BmButtonDirective } from '../../shared/directives/bm-button.directive';
import { BmSelectDirective } from '../../shared/directives/bm-controls.directive';

@Component({
  selector: 'app-document-preview-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, BmButtonDirective, BmSelectDirective],
  template: `
    @if (isOpen) {
      <div class="bm-preview-overlay" (click)="onBackdropClick($event)">
        <div class="bm-preview-modal" role="dialog" aria-modal="true">
          <!-- Header Bar -->
          <div class="bm-preview-header">
            <div class="header-left">
              <i class="pi pi-print header-icon"></i>
              <div>
                <h3 class="header-title">Document Print & Preview</h3>
                <span class="header-subtitle">Doc: {{ documentNumber || 'Draft Document' }}</span>
              </div>
            </div>

            <!-- Toolbar Controls -->
            <div class="header-toolbar">
              <!-- Format Selector -->
              <div class="toolbar-item">
                <label>Format:</label>
                <select bmSelect class="toolbar-select" [ngModel]="selectedFormat()" (ngModelChange)="onFormatChange($event)">
                  <option value="a4">Standard A4</option>
                  <option value="a5">Compact A5 Landscape</option>
                  <option value="thermal">80mm POS Thermal</option>
                </select>
              </div>

              <!-- Letterhead Mode (only for standard A4/A5) -->
              @if (selectedFormat() !== 'thermal') {
                <div class="toolbar-item">
                  <label>Stationery:</label>
                  <select bmSelect class="toolbar-select" [ngModel]="letterheadMode()" (ngModelChange)="onLetterheadModeChange($event)">
                    <option [value]="'with'">With Digital Letterhead</option>
                    <option [value]="'preprinted'">Pre-Printed Stationery</option>
                  </select>
                </div>
              }

              <!-- Actions -->
              <button
                bmButton
                bmVariant="primary"
                bmSize="sm"
                class="action-btn"
                (click)="printDirect()"
                [disabled]="isLoading() || !pdfUrl()"
                title="Print directly to connected printer without downloading"
              >
                <i class="pi pi-print"></i>
                <span>Direct Print</span>
              </button>

              <button
                bmButton
                bmVariant="secondary"
                bmSize="sm"
                class="action-btn"
                (click)="downloadPdf()"
                [disabled]="isLoading() || !pdfBlob"
                title="Save PDF file to disk"
              >
                <i class="pi pi-download"></i>
                <span>Download</span>
              </button>

              <button class="close-btn" (click)="close()" title="Close Preview">
                <i class="pi pi-times"></i>
              </button>
            </div>
          </div>

          <!-- Body / Viewer Container -->
          <div class="bm-preview-body">
            @if (isLoading()) {
              <div class="loading-state">
                <i class="pi pi-spin pi-spinner spinner-icon"></i>
                <p>Generating document layout...</p>
              </div>
            }

            @if (errorMessage()) {
              <div class="error-state">
                <i class="pi pi-exclamation-triangle"></i>
                <p>{{ errorMessage() }}</p>
                <button bmButton bmVariant="secondary" bmSize="sm" (click)="loadPdfPreview()">Try Again</button>
              </div>
            }

            <iframe
              #pdfFrame
              [src]="pdfUrl()"
              class="preview-iframe"
              [class.hidden]="isLoading() || !!errorMessage()"
              title="Document Print Preview"
            ></iframe>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .bm-preview-overlay {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.7);
      backdrop-filter: blur(4px);
      z-index: 1100;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1.5rem;
      animation: fadeIn 0.15s ease-out;
    }

    .bm-preview-modal {
      background: #ffffff;
      border-radius: 12px;
      width: 100%;
      max-width: 1050px;
      height: 90vh;
      display: flex;
      flex-direction: column;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
      overflow: hidden;
      border: 1px solid #e2e8f0;
    }

    .bm-preview-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.875rem 1.25rem;
      background: #f8fafc;
      border-bottom: 1px solid #e2e8f0;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .header-icon {
      font-size: 1.5rem;
      color: #1e3a8a;
      background: #eff6ff;
      padding: 0.5rem;
      border-radius: 8px;
    }

    .header-title {
      margin: 0;
      font-size: 1.05rem;
      font-weight: 700;
      color: #0f172a;
    }

    .header-subtitle {
      font-size: 0.8rem;
      color: #64748b;
      font-family: monospace;
    }

    .header-toolbar {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .toolbar-item {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.825rem;
      font-weight: 600;
      color: #475569;
    }

    .toolbar-select {
      padding: 0.35rem 0.65rem;
      font-size: 0.825rem;
      border-radius: 6px;
      border: 1px solid #cbd5e1;
      background: #ffffff;
      color: #1e293b;
      cursor: pointer;
    }

    .action-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
    }

    .close-btn {
      background: none;
      border: none;
      color: #64748b;
      cursor: pointer;
      padding: 0.4rem;
      border-radius: 6px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1rem;
      transition: background 0.15s, color 0.15s;

      &:hover {
        background: #e2e8f0;
        color: #0f172a;
      }
    }

    .bm-preview-body {
      flex: 1;
      position: relative;
      background: #525659;
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: hidden;
    }

    .preview-iframe {
      width: 100%;
      height: 100%;
      border: none;
      background: #525659;
    }

    .preview-iframe.hidden {
      display: none;
    }

    .loading-state, .error-state {
      position: absolute;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      color: #f8fafc;
      gap: 0.75rem;
      font-size: 0.95rem;
    }

    .spinner-icon {
      font-size: 2rem;
      color: #38bdf8;
    }

    .error-state {
      color: #fca5a5;
      text-align: center;
      padding: 1.5rem;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: scale(0.99); }
      to { opacity: 1; transform: scale(1); }
    }
  `]
})
export class DocumentPreviewDialogComponent implements OnChanges, OnDestroy {
  @Input() documentId: string = '';
  @Input() documentNumber: string = '';
  @Input() isOpen: boolean = false;
  @Output() closed = new EventEmitter<void>();

  @ViewChild('pdfFrame') iframeRef?: ElementRef<HTMLIFrameElement>;

  private readonly salesService = inject(SalesService);
  private readonly dialogService = inject(DialogService);
  private readonly sanitizer = inject(DomSanitizer);

  selectedFormat = signal<'a4' | 'a5' | 'thermal'>('a4');
  letterheadMode = signal<'with' | 'preprinted'>('with');
  pdfUrl = signal<SafeResourceUrl | null>(null);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  pdfBlob: Blob | null = null;
  private currentObjectUrl: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      if (this.documentId) {
        this.loadPdfPreview();
      }
    }
  }

  ngOnDestroy(): void {
    this.revokeObjectUrl();
  }

  onFormatChange(format: 'a4' | 'a5' | 'thermal'): void {
    this.selectedFormat.set(format);
    this.loadPdfPreview();
  }

  onLetterheadModeChange(mode: 'with' | 'preprinted'): void {
    this.letterheadMode.set(mode);
    this.loadPdfPreview();
  }

  loadPdfPreview(): void {
    if (!this.documentId) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.revokeObjectUrl();

    const withLetterhead = this.selectedFormat() === 'thermal' ? true : (this.letterheadMode() === 'with');

    this.salesService.getPdfBlob(this.documentId, this.selectedFormat(), withLetterhead).subscribe({
      next: (blob) => {
        this.pdfBlob = blob;
        this.currentObjectUrl = URL.createObjectURL(blob);
        this.pdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(this.currentObjectUrl));
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err?.error?.message || err?.message || 'Failed to generate document preview.');
      }
    });
  }

  printDirect(): void {
    if (!this.iframeRef?.nativeElement) {
      window.print();
      return;
    }

    try {
      const iframe = this.iframeRef.nativeElement;
      if (iframe.contentWindow) {
        iframe.contentWindow.focus();
        iframe.contentWindow.print();
      } else {
        window.print();
      }
    } catch {
      // Cross-origin fallback
      window.print();
    }
  }

  downloadPdf(): void {
    if (!this.pdfBlob) return;

    const safeNumber = (this.documentNumber || 'document').replace(/[\/\\:]/g, '-');
    const suffix = this.letterheadMode() === 'preprinted' && this.selectedFormat() !== 'thermal' ? '_preprinted' : '';
    const fileName = `${safeNumber}_${this.selectedFormat()}${suffix}.pdf`;

    const link = document.createElement('a');
    link.href = URL.createObjectURL(this.pdfBlob);
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    this.dialogService.toast(`Document downloaded: ${fileName}`, 'success');
  }

  close(): void {
    this.revokeObjectUrl();
    this.closed.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('bm-preview-overlay')) {
      this.close();
    }
  }

  private revokeObjectUrl(): void {
    if (this.currentObjectUrl) {
      URL.revokeObjectURL(this.currentObjectUrl);
      this.currentObjectUrl = null;
      this.pdfUrl.set(null);
    }
  }
}
