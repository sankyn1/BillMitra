import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export interface BackupRecord {
  id: string;
  fileName: string;
  filePath: string;
  fileSizeBytes: number;
  backupType: string;
  status: string;
  createdAtUtc: string;
  checksum?: string;
  notes?: string;
}

export interface CreateBackupRequest {
  backupType?: string;
  notes?: string;
}

export interface RestoreBackupRequest {
  backupId: string;
  confirmSafetySnapshot: boolean;
}

export interface RestoreBackupResult {
  success: boolean;
  message: string;
  safetySnapshotFileName?: string;
  restoredAtUtc: string;
}

export interface BackupConfiguration {
  autoBackupOnStartup: boolean;
  backupFolderPath: string;
  retentionCount: number;
  lastBackupAtUtc?: string;
}

export interface UpdateBackupConfigurationRequest {
  autoBackupOnStartup: boolean;
  backupFolderPath?: string;
  retentionCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class BackupService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5000/api/v1/backups';

  createBackup(request: CreateBackupRequest): Observable<BackupRecord> {
    return this.http.post<ApiResponse<BackupRecord>>(this.baseUrl, request)
      .pipe(map(r => r.data));
  }

  getHistory(): Observable<BackupRecord[]> {
    return this.http.get<ApiResponse<BackupRecord[]>>(this.baseUrl)
      .pipe(map(r => r.data));
  }

  verifyIntegrity(id: string): Observable<boolean> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${id}/verify`, null)
      .pipe(map(r => r.data));
  }

  restore(request: RestoreBackupRequest): Observable<RestoreBackupResult> {
    return this.http.post<ApiResponse<RestoreBackupResult>>(`${this.baseUrl}/restore`, request)
      .pipe(map(r => r.data));
  }

  deleteBackup(id: string): Observable<boolean> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  getConfig(): Observable<BackupConfiguration> {
    return this.http.get<ApiResponse<BackupConfiguration>>(`${this.baseUrl}/config`)
      .pipe(map(r => r.data));
  }

  updateConfig(request: UpdateBackupConfigurationRequest): Observable<BackupConfiguration> {
    return this.http.put<ApiResponse<BackupConfiguration>>(`${this.baseUrl}/config`, request)
      .pipe(map(r => r.data));
  }
}

