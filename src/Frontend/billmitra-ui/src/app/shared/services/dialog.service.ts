import { Injectable, signal } from '@angular/core';

export type DialogType = 'info' | 'success' | 'warning' | 'danger';

export interface DialogState {
  isOpen: boolean;
  title: string;
  message: string;
  type: DialogType;
  confirmText: string;
  cancelText: string;
  isConfirm: boolean;
  isDestructive: boolean;
  resolve?: (value: boolean) => void;
}

export interface ToastItem {
  id: string;
  message: string;
  type: DialogType;
}

@Injectable({
  providedIn: 'root'
})
export class DialogService {
  dialogState = signal<DialogState>({
    isOpen: false,
    title: '',
    message: '',
    type: 'info',
    confirmText: 'OK',
    cancelText: 'Cancel',
    isConfirm: false,
    isDestructive: false
  });

  toasts = signal<ToastItem[]>([]);

  alert(title: string, message: string, type: DialogType = 'info'): Promise<void> {
    return new Promise((resolve) => {
      this.dialogState.set({
        isOpen: true,
        title,
        message,
        type,
        confirmText: 'OK',
        cancelText: '',
        isConfirm: false,
        isDestructive: false,
        resolve: () => resolve()
      });
    });
  }

  confirm(
    title: string,
    message: string,
    confirmText: string = 'Confirm',
    isDestructive: boolean = false,
    type: DialogType = isDestructive ? 'danger' : 'warning'
  ): Promise<boolean> {
    return new Promise((resolve) => {
      this.dialogState.set({
        isOpen: true,
        title,
        message,
        type,
        confirmText,
        cancelText: 'Cancel',
        isConfirm: true,
        isDestructive,
        resolve
      });
    });
  }

  handleConfirm(): void {
    const state = this.dialogState();
    if (state.resolve) {
      state.resolve(true);
    }
    this.closeDialog();
  }

  handleCancel(): void {
    const state = this.dialogState();
    if (state.resolve) {
      state.resolve(false);
    }
    this.closeDialog();
  }

  private closeDialog(): void {
    this.dialogState.update((s) => ({ ...s, isOpen: false, resolve: undefined }));
  }

  toast(message: string, type: DialogType = 'info', durationMs: number = 3500): void {
    const id = Math.random().toString(36).substring(2, 9);
    const item: ToastItem = { id, message, type };
    this.toasts.update((list) => [...list, item]);

    setTimeout(() => {
      this.removeToast(id);
    }, durationMs);
  }

  removeToast(id: string): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }
}

