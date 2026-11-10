import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, of, tap } from 'rxjs';

export interface SystemHealthDto {
  status: string;
  appName: string;
  version: string;
  environment: string;
  databaseConnected: boolean;
  databaseProvider: string;
  serverTimeUtc: string;
  uptime: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors: string[];
  statusCode: number;
}

@Injectable({
  providedIn: 'root'
})
export class SystemService {
  // Use relative path by default; if running in development WebView2, can fall back to localhost:5000
  private readonly apiUrl = 'http://127.0.0.1:5000/api/v1/system/health';

  health = signal<SystemHealthDto | null>(null);
  isLoading = signal<boolean>(false);
  isApiConnected = signal<boolean>(false);
  lastChecked = signal<Date | null>(null);
  errorMessage = signal<string | null>(null);

  constructor(private http: HttpClient) {}

  checkHealth(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.http.get<ApiResponse<SystemHealthDto>>(this.apiUrl)
      .pipe(
        tap(res => {
          if (res.success && res.data) {
            this.health.set(res.data);
            this.isApiConnected.set(true);
            this.lastChecked.set(new Date());
          }
          this.isLoading.set(false);
        }),
        catchError(err => {
          this.isApiConnected.set(false);
          this.errorMessage.set('Cannot connect to local BillMitra API service.');
          this.isLoading.set(false);
          return of(null);
        })
      )
      .subscribe();
  }
}
