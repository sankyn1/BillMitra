import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  BackupService,
  BackupRecord,
  BackupConfiguration
} from '../../core/services/backup.service';
import { DialogService } from '../../shared/services/dialog.service';

@Component({
  selector: 'app-backup-restore',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6">
      <!-- Header & Quick Actions -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <div>
          <h1 class="text-2xl font-bold text-slate-800 tracking-tight flex items-center gap-3">
            <span class="w-10 h-10 rounded-xl bg-gradient-to-tr from-emerald-600 to-teal-600 text-white flex items-center justify-center text-lg shadow-md shadow-emerald-100">
              🛡️
            </span>
            Local Backups & Disaster Recovery
          </h1>
          <p class="text-sm text-slate-500 mt-1">Automated SQLite/SQL Server snapshots, WAL checkpoints, SHA256 integrity verification, and safe point-in-time restore</p>
        </div>

        <div class="flex items-center gap-3">
          <button (click)="openManualBackupModal()" class="px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white text-xs font-bold rounded-xl shadow-sm transition flex items-center gap-2">
            <span>💾</span> Create Instant Backup
          </button>
          <button (click)="loadAll()" class="px-4 py-2.5 bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 text-xs font-semibold rounded-xl shadow-sm transition flex items-center gap-1.5">
            <span>🔄</span> Refresh
          </button>
        </div>
      </div>

      <!-- Overview Stats Cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4" *ngIf="config">
        <div class="bg-gradient-to-br from-emerald-50 to-teal-50 border border-emerald-100 p-5 rounded-2xl shadow-sm">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-bold text-emerald-800 uppercase tracking-wider">Last Verified Backup</span>
            <span class="text-lg">⏰</span>
          </div>
          <div class="text-xl font-black text-emerald-950">
            {{ config.lastBackupAtUtc ? (config.lastBackupAtUtc | date:'medium') : 'Never' }}
          </div>
          <div class="text-xs text-emerald-700 mt-1">Automatic recovery point active</div>
        </div>

        <div class="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-100 p-5 rounded-2xl shadow-sm">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-bold text-blue-800 uppercase tracking-wider">Available Snapshots</span>
            <span class="text-lg">📦</span>
          </div>
          <div class="text-2xl font-black text-blue-950">{{ backups.length }} Backups</div>
          <div class="text-xs text-blue-600 mt-1">Total Size: {{ getTotalBackupSize() }}</div>
        </div>

        <div class="bg-gradient-to-br from-amber-50 to-orange-50 border border-amber-100 p-5 rounded-2xl shadow-sm">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-bold text-amber-800 uppercase tracking-wider">Retention Policy</span>
            <span class="text-lg">⚙️</span>
          </div>
          <div class="text-2xl font-black text-amber-950">Last {{ config.retentionCount }} Snapshots</div>
          <div class="text-xs text-amber-700 mt-1">Startup Auto-Backup: {{ config.autoBackupOnStartup ? 'Enabled' : 'Disabled' }}</div>
        </div>
      </div>

      <!-- Backups List Table -->
      <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-bold text-slate-800">Available Restore Points & Snapshots</h2>
            <p class="text-xs text-slate-500">Every snapshot is defragmented and contains the full transactional state of your business</p>
          </div>
          <span class="text-xs font-medium text-slate-400">Total: {{ backups.length }} snapshots</span>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs">
            <thead class="bg-slate-50 text-slate-600 font-bold border-b border-slate-200">
              <tr>
                <th class="py-3 px-4">Backup File & Type</th>
                <th class="py-3 px-4">Timestamp</th>
                <th class="py-3 px-4 text-right">Size</th>
                <th class="py-3 px-4 text-center">Integrity Status</th>
                <th class="py-3 px-4">SHA256 Checksum</th>
                <th class="py-3 px-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-slate-700">
              <tr *ngFor="let b of backups" class="hover:bg-slate-50 transition">
                <td class="py-3 px-4">
                  <div class="font-bold text-slate-800 flex items-center gap-1.5">
                    <span>{{ getBackupTypeIcon(b.backupType) }}</span>
                    <span>{{ b.fileName }}</span>
                  </div>
                  <div class="text-[11px] text-slate-400 font-mono mt-0.5">{{ b.notes || b.backupType }}</div>
                </td>
                <td class="py-3 px-4 text-slate-600">
                  {{ b.createdAtUtc | date:'medium' }}
                </td>
                <td class="py-3 px-4 text-right font-mono font-medium text-slate-800">
                  {{ formatBytes(b.fileSizeBytes) }}
                </td>
                <td class="py-3 px-4 text-center">
                  <span [ngClass]="{
                    'bg-emerald-100 text-emerald-800 border-emerald-200': b.status === 'Verified' || b.status === 'Completed',
                    'bg-rose-100 text-rose-800 border-rose-200': b.status === 'Failed'
                  }" class="px-2.5 py-0.5 rounded-full text-[11px] font-bold border">
                    {{ b.status === 'Verified' ? '✓ Verified' : b.status }}
                  </span>
                </td>
                <td class="py-3 px-4 font-mono text-[10px] text-slate-400 max-w-xs truncate" [title]="b.checksum || ''">
                  {{ b.checksum ? (b.checksum | slice:0:16) + '...' : 'N/A' }}
                </td>
                <td class="py-3 px-4 text-right space-x-2">
                  <button (click)="verifyBackup(b)" [disabled]="isActionBusy" class="px-2.5 py-1 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-xs font-semibold transition" title="Test PRAGMA integrity check">
                    🔍 Verify
                  </button>
                  <button (click)="openRestoreModal(b)" [disabled]="isActionBusy" class="px-2.5 py-1 bg-amber-50 hover:bg-amber-100 text-amber-800 border border-amber-200 rounded-lg text-xs font-semibold transition" title="Restore this backup">
                    ⚡ Restore
                  </button>
                  <button (click)="deleteBackup(b)" [disabled]="isActionBusy" class="px-2.5 py-1 bg-rose-50 hover:bg-rose-100 text-rose-700 rounded-lg text-xs font-semibold transition" title="Delete snapshot">
                    🗑️
                  </button>
                </td>
              </tr>
              <tr *ngIf="backups.length === 0">
                <td colspan="6" class="py-12 text-center text-slate-400 font-medium">
                  No backup archives created yet. Click "Create Instant Backup" above to secure your database!
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Automated Retention & Schedule Policy Configuration -->
      <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm" *ngIf="config">
        <h2 class="text-lg font-bold text-slate-800 mb-1">Automated Disaster Recovery & Retention Settings</h2>
        <p class="text-xs text-slate-500 mb-4">Configure background startup backups and automatic cleanup of aging snapshots</p>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div class="space-y-2">
            <label class="text-xs font-bold text-slate-700 flex items-center gap-2">
              <input type="checkbox" [(ngModel)]="config.autoBackupOnStartup" class="w-4 h-4 rounded text-emerald-600 focus:ring-emerald-500" />
              Auto-Backup on Application Startup
            </label>
            <p class="text-[11px] text-slate-500">Automatically creates a fresh backup when BillMitra launches once every 24 hours.</p>
          </div>

          <div class="space-y-2">
            <label class="text-xs font-bold text-slate-700">Snapshot Retention Limit</label>
            <input type="number" [(ngModel)]="config.retentionCount" min="3" max="100" class="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:ring-2 focus:ring-emerald-500" />
            <p class="text-[11px] text-slate-500">Retain newest N snapshots; older archives are automatically pruned.</p>
          </div>

          <div class="space-y-2">
            <label class="text-xs font-bold text-slate-700">Target Backup Directory</label>
            <input type="text" [(ngModel)]="config.backupFolderPath" class="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono focus:outline-none focus:ring-2 focus:ring-emerald-500" />
            <p class="text-[11px] text-slate-500">Local or external drive path for storing backup files.</p>
          </div>
        </div>

        <div class="mt-6 flex justify-end">
          <button (click)="savePolicy()" [disabled]="isActionBusy" class="px-5 py-2 bg-slate-800 hover:bg-slate-900 active:bg-black text-white text-xs font-bold rounded-xl shadow-sm transition">
            Save Policy Settings
          </button>
        </div>
      </div>
    </div>

    <!-- Modal: Instant Backup -->
    <div *ngIf="showManualModal" class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
        <h3 class="text-lg font-bold text-slate-800 flex items-center gap-2">
          <span>💾</span> Create Immediate Database Backup
        </h3>
        <p class="text-xs text-slate-500">Creates a point-in-time snapshot of all invoices, parties, items, batches, and ledger balances.</p>

        <div>
          <label class="block text-xs font-bold text-slate-700 mb-1">Optional Notes / Reason</label>
          <input type="text" [(ngModel)]="manualNotes" placeholder="e.g. Month-end closing backup, Pre-audit snapshot" class="w-full px-3 py-2 border border-slate-200 rounded-xl text-xs focus:ring-2 focus:ring-emerald-500" />
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button (click)="showManualModal = false" class="px-4 py-2 border border-slate-200 rounded-xl text-xs font-semibold hover:bg-slate-50 text-slate-700">
            Cancel
          </button>
          <button (click)="submitManualBackup()" [disabled]="isActionBusy" class="px-5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold shadow-sm">
            {{ isActionBusy ? 'Creating Backup...' : 'Create Snapshot' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Modal: Restore Database Confirmation -->
    <div *ngIf="showRestoreModal && selectedBackup" class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
        <h3 class="text-lg font-bold text-rose-700 flex items-center gap-2">
          <span>⚠️</span> Restore Database From Backup
        </h3>
        <p class="text-xs text-slate-600">
          You are about to roll back the active database to the state recorded in <strong class="text-slate-800">{{ selectedBackup.fileName }}</strong> ({{ selectedBackup.createdAtUtc | date:'medium' }}).
        </p>

        <div class="p-3 bg-amber-50 rounded-xl border border-amber-200 text-amber-900 text-xs">
          <strong>Safety Protection:</strong> A pre-restore safety snapshot will be taken automatically before replacing the current database so no data is permanently lost.
        </div>

        <div class="flex items-center gap-2 text-xs">
          <input type="checkbox" [(ngModel)]="confirmSafetySnapshot" id="safetyCheck" class="w-4 h-4 rounded text-emerald-600" />
          <label for="safetyCheck" class="text-slate-700 font-medium">Create automatic pre-restore safety snapshot (Recommended)</label>
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button (click)="showRestoreModal = false" class="px-4 py-2 border border-slate-200 rounded-xl text-xs font-semibold hover:bg-slate-50 text-slate-700">
            Cancel
          </button>
          <button (click)="submitRestore()" [disabled]="isActionBusy" class="px-5 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-xl text-xs font-bold shadow-sm">
            {{ isActionBusy ? 'Restoring Database...' : 'Confirm & Restore' }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class BackupRestoreComponent implements OnInit {
  private backupService = inject(BackupService);
  private dialog = inject(DialogService);

  backups: BackupRecord[] = [];
  config: BackupConfiguration | null = null;
  isActionBusy = false;

  showManualModal = false;
  manualNotes = '';

  showRestoreModal = false;
  selectedBackup: BackupRecord | null = null;
  confirmSafetySnapshot = true;

  ngOnInit() {
    this.loadAll();
  }

  loadAll() {
    this.backupService.getConfig().subscribe({
      next: (cfg) => (this.config = cfg),
      error: (err: any) => this.dialog.alert('Error', err?.error?.message || 'Failed to load backup configuration.', 'danger')
    });

    this.backupService.getHistory().subscribe({
      next: (list) => (this.backups = list),
      error: (err: any) => this.dialog.alert('Error', err?.error?.message || 'Failed to load backup history.', 'danger')
    });
  }

  openManualBackupModal() {
    this.manualNotes = '';
    this.showManualModal = true;
  }

  submitManualBackup() {
    this.isActionBusy = true;
    this.backupService.createBackup({ backupType: 'Manual', notes: this.manualNotes }).subscribe({
      next: (record) => {
        this.isActionBusy = false;
        this.showManualModal = false;
        this.dialog.toast(`Backup '${record.fileName}' created successfully!`, 'success');
        this.loadAll();
      },
      error: (err: any) => {
        this.isActionBusy = false;
        this.dialog.alert('Error', err?.error?.message || 'Failed to generate database backup.', 'danger');
      }
    });
  }

  verifyBackup(b: BackupRecord) {
    this.isActionBusy = true;
    this.backupService.verifyIntegrity(b.id).subscribe({
      next: (valid) => {
        this.isActionBusy = false;
        if (valid) {
          this.dialog.alert('Integrity Verified', `Backup ${b.fileName} passed PRAGMA integrity checks without error.`, 'success');
        } else {
          this.dialog.alert('Verification Failed', `Backup ${b.fileName} failed integrity verification or is missing.`, 'danger');
        }
        this.loadAll();
      },
      error: (err: any) => {
        this.isActionBusy = false;
        this.dialog.alert('Error', err?.error?.message || 'Verification process failed.', 'danger');
      }
    });
  }

  openRestoreModal(b: BackupRecord) {
    this.selectedBackup = b;
    this.confirmSafetySnapshot = true;
    this.showRestoreModal = true;
  }

  submitRestore() {
    if (!this.selectedBackup) return;

    this.isActionBusy = true;
    this.backupService.restore({ backupId: this.selectedBackup.id, confirmSafetySnapshot: this.confirmSafetySnapshot }).subscribe({
      next: (res) => {
        this.isActionBusy = false;
        this.showRestoreModal = false;
        this.dialog.alert('Restore Complete', res.message + (res.safetySnapshotFileName ? ` (Pre-restore safety snapshot saved as ${res.safetySnapshotFileName})` : ''), 'success');
        this.loadAll();
      },
      error: (err: any) => {
        this.isActionBusy = false;
        this.dialog.alert('Restore Error', err?.error?.message || 'Failed to restore database from backup.', 'danger');
      }
    });
  }

  async deleteBackup(b: BackupRecord) {
    const confirmed = await this.dialog.confirm(
      'Delete Backup Archive',
      `Are you sure you want to permanently delete backup '${b.fileName}'?`,
      'Delete',
      true
    );

    if (!confirmed) return;

    this.isActionBusy = true;
    this.backupService.deleteBackup(b.id).subscribe({
      next: () => {
        this.isActionBusy = false;
        this.dialog.toast('Backup archive deleted.', 'success');
        this.loadAll();
      },
      error: (err: any) => {
        this.isActionBusy = false;
        this.dialog.alert('Error', err?.error?.message || 'Could not delete backup.', 'danger');
      }
    });
  }

  savePolicy() {
    if (!this.config) return;

    this.isActionBusy = true;
    this.backupService.updateConfig({
      autoBackupOnStartup: this.config.autoBackupOnStartup,
      backupFolderPath: this.config.backupFolderPath,
      retentionCount: this.config.retentionCount
    }).subscribe({
      next: (updated) => {
        this.isActionBusy = false;
        this.config = updated;
        this.dialog.toast('Disaster recovery settings saved successfully.', 'success');
      },
      error: (err: any) => {
        this.isActionBusy = false;
        this.dialog.alert('Error', err?.error?.message || 'Failed to save settings.', 'danger');
      }
    });
  }

  getBackupTypeIcon(type: string): string {
    switch (type) {
      case 'Manual': return '👤';
      case 'ScheduledStartup': return '⏰';
      case 'PreRestoreSnapshot': return '🛡️';
      default: return '💾';
    }
  }

  formatBytes(bytes: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  getTotalBackupSize(): string {
    const sum = this.backups.reduce((acc, curr) => acc + (curr.fileSizeBytes || 0), 0);
    return this.formatBytes(sum);
  }
}
