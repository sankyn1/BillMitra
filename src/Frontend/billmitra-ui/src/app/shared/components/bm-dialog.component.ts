import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogService, DialogType } from '../services/dialog.service';
import { BmButtonDirective } from '../directives/bm-button.directive';
import { BmBadgeDirective } from '../directives/bm-controls.directive';

@Component({
  selector: 'app-bm-dialog',
  standalone: true,
  imports: [CommonModule, BmButtonDirective],
  template: `
    <!-- Modal Dialog Backdrop & Card -->
    @if (dialogService.dialogState().isOpen) {
      <div class="bm-dialog-overlay" (click)="onBackdropClick($event)">
        <div class="bm-dialog-modal" role="dialog" aria-modal="true">
          <div class="bm-dialog-header" [ngClass]="'bm-header-' + dialogService.dialogState().type">
            <div class="bm-header-title-wrap">
              <i class="pi" [ngClass]="getIcon(dialogService.dialogState().type)"></i>
              <h3>{{ dialogService.dialogState().title }}</h3>
            </div>
            <button class="bm-dialog-close" (click)="dialogService.handleCancel()">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <div class="bm-dialog-body">
            <p>{{ dialogService.dialogState().message }}</p>
          </div>

          <div class="bm-dialog-footer">
            @if (dialogService.dialogState().isConfirm) {
              <button bmButton bmVariant="secondary" (click)="dialogService.handleCancel()">
                {{ dialogService.dialogState().cancelText }}
              </button>
              <button
                bmButton
                [bmVariant]="dialogService.dialogState().isDestructive ? 'danger' : 'primary'"
                (click)="dialogService.handleConfirm()"
              >
                {{ dialogService.dialogState().confirmText }}
              </button>
            } @else {
              <button bmButton bmVariant="primary" (click)="dialogService.handleConfirm()">
                {{ dialogService.dialogState().confirmText }}
              </button>
            }
          </div>
        </div>
      </div>
    }

    <!-- Toast Notification Container (Bottom Right) -->
    @if (dialogService.toasts().length > 0) {
      <div class="bm-toast-stack">
        @for (toast of dialogService.toasts(); track toast.id) {
          <div class="bm-toast-item" [ngClass]="'bm-toast-' + toast.type">
            <i class="pi" [ngClass]="getIcon(toast.type)"></i>
            <div class="bm-toast-message">{{ toast.message }}</div>
            <button class="bm-toast-close" (click)="dialogService.removeToast(toast.id)">
              <i class="pi pi-times"></i>
            </button>
          </div>
        }
      </div>
    }
  `,
  styles: [`
    .bm-dialog-overlay {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.45);
      backdrop-filter: blur(3px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 9999;
      animation: bmFadeIn 0.15s ease-out;
    }

    .bm-dialog-modal {
      background: #FFFFFF;
      border-radius: var(--bm-radius-lg);
      width: 480px;
      max-width: 92vw;
      box-shadow: var(--bm-shadow-lg), 0 20px 25px -5px rgba(0, 0, 0, 0.15);
      border: 1px solid var(--bm-border-subtle);
      overflow: hidden;
      animation: bmScaleIn 0.18s cubic-bezier(0.16, 1, 0.3, 1);
    }

    .bm-dialog-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1.1rem 1.25rem;
      border-bottom: 1px solid var(--bm-border-subtle);
    }

    .bm-header-title-wrap {
      display: flex;
      align-items: center;
      gap: 0.65rem;
    }

    .bm-header-title-wrap h3 {
      font-size: 1.05rem;
      font-weight: 600;
      color: var(--bm-text-primary);
      margin: 0;
    }

    .bm-header-title-wrap i {
      font-size: 1.25rem;
    }

    .bm-header-info i { color: var(--bm-primary); }
    .bm-header-success i { color: var(--bm-success); }
    .bm-header-warning i { color: var(--bm-warning); }
    .bm-header-danger i { color: var(--bm-danger); }

    .bm-dialog-close {
      background: transparent;
      border: none;
      color: var(--bm-text-muted);
      cursor: pointer;
      font-size: 0.95rem;
      padding: 0.25rem;
      border-radius: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .bm-dialog-close:hover {
      color: var(--bm-text-primary);
      background: #F1F5F9;
    }

    .bm-dialog-body {
      padding: 1.25rem;
      font-size: 0.9rem;
      line-height: 1.55;
      color: var(--bm-text-secondary);
    }

    .bm-dialog-footer {
      display: flex;
      justify-content: flex-end;
      gap: 0.65rem;
      padding: 0.85rem 1.25rem;
      background: #F8FAFC;
      border-top: 1px solid var(--bm-border-subtle);
    }

    /* Toast Container */
    .bm-toast-stack {
      position: fixed;
      bottom: 1.5rem;
      right: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 0.65rem;
      z-index: 10000;
      pointer-events: none;
    }

    .bm-toast-item {
      pointer-events: auto;
      min-width: 300px;
      max-width: 420px;
      background: #FFFFFF;
      border-radius: var(--bm-radius-md);
      box-shadow: var(--bm-shadow-lg);
      padding: 0.75rem 1rem;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      border-left: 4px solid;
      animation: bmSlideIn 0.2s ease-out;
      font-size: 0.875rem;
    }

    .bm-toast-info { border-left-color: var(--bm-info); }
    .bm-toast-info i { color: var(--bm-info); }

    .bm-toast-success { border-left-color: var(--bm-success); }
    .bm-toast-success i { color: var(--bm-success); }

    .bm-toast-warning { border-left-color: var(--bm-warning); }
    .bm-toast-warning i { color: var(--bm-warning); }

    .bm-toast-danger { border-left-color: var(--bm-danger); }
    .bm-toast-danger i { color: var(--bm-danger); }

    .bm-toast-message {
      flex: 1;
      color: var(--bm-text-primary);
    }

    .bm-toast-close {
      background: transparent;
      border: none;
      color: var(--bm-text-muted);
      cursor: pointer;
    }
    .bm-toast-close:hover {
      color: var(--bm-text-primary);
    }

    @keyframes bmFadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes bmScaleIn {
      from { transform: scale(0.95); opacity: 0; }
      to { transform: scale(1); opacity: 1; }
    }
    @keyframes bmSlideIn {
      from { transform: translateX(20px); opacity: 0; }
      to { transform: translateX(0); opacity: 1; }
    }
  `]
})
export class BmDialogComponent {
  constructor(public dialogService: DialogService) {}

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('bm-dialog-overlay')) {
      this.dialogService.handleCancel();
    }
  }

  getIcon(type: DialogType): string {
    switch (type) {
      case 'success': return 'pi-check-circle';
      case 'danger': return 'pi-times-circle';
      case 'warning': return 'pi-exclamation-triangle';
      default: return 'pi-info-circle';
    }
  }
}
