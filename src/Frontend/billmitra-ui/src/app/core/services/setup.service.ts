import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string;
  errors: string[];
  statusCode: number;
}

export interface SetupStatus {
  isCompleted: boolean;
  companyName?: string;
  tradeName?: string;
  gstin?: string;
  stateCode?: string;
  stateName?: string;
  currentFinancialYear?: string;
  initializedAtUtc?: string;
}

export interface InitialSetupRequest {
  legalName: string;
  tradeName: string;
  gstin?: string;
  gstRegistrationType: number;
  stateCode: string;
  stateName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  pincode: string;
  phone: string;
  email?: string;
  bankName?: string;
  bankAccountNumber?: string;
  bankIfsc?: string;
  bankBranch?: string;
  upiId?: string;
  financialYearStartYear: number;
  ownerFullName: string;
  ownerUsername: string;
  ownerEmail: string;
  ownerPassword: string;
}

export interface CompanyProfileDto {
  id: string;
  legalName: string;
  tradeName: string;
  gstin?: string;
  pan?: string;
  gstRegistrationType: number;
  stateCode: string;
  stateName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  pincode: string;
  phone: string;
  email?: string;
  bankName?: string;
  bankAccountNumber?: string;
  bankIfsc?: string;
  bankBranch?: string;
  upiId?: string;
  invoiceFooterNotes?: string;
  termsAndConditions?: string;
  letterheadImagePath?: string;
  defaultLetterheadMode: number;
  preprintedHeaderMarginMm: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UpdateLetterheadSettingsRequest {
  defaultLetterheadMode: number;
  preprintedHeaderMarginMm: number;
}

export const INDIAN_STATES: { code: string; name: string }[] = [
  { code: '01', name: 'Jammu and Kashmir' },
  { code: '02', name: 'Himachal Pradesh' },
  { code: '03', name: 'Punjab' },
  { code: '04', name: 'Chandigarh' },
  { code: '05', name: 'Uttarakhand' },
  { code: '06', name: 'Haryana' },
  { code: '07', name: 'Delhi' },
  { code: '08', name: 'Rajasthan' },
  { code: '09', name: 'Uttar Pradesh' },
  { code: '10', name: 'Bihar' },
  { code: '11', name: 'Sikkim' },
  { code: '12', name: 'Arunachal Pradesh' },
  { code: '13', name: 'Nagaland' },
  { code: '14', name: 'Manipur' },
  { code: '15', name: 'Mizoram' },
  { code: '16', name: 'Tripura' },
  { code: '17', name: 'Meghalaya' },
  { code: '18', name: 'Assam' },
  { code: '19', name: 'West Bengal' },
  { code: '20', name: 'Jharkhand' },
  { code: '21', name: 'Odisha' },
  { code: '22', name: 'Chhattisgarh' },
  { code: '23', name: 'Madhya Pradesh' },
  { code: '24', name: 'Gujarat' },
  { code: '26', name: 'Dadra and Nagar Haveli and Daman and Diu' },
  { code: '27', name: 'Maharashtra' },
  { code: '28', name: 'Andhra Pradesh (Old)' },
  { code: '29', name: 'Karnataka' },
  { code: '30', name: 'Goa' },
  { code: '31', name: 'Lakshadweep' },
  { code: '32', name: 'Kerala' },
  { code: '33', name: 'Tamil Nadu' },
  { code: '34', name: 'Puducherry' },
  { code: '35', name: 'Andaman and Nicobar Islands' },
  { code: '36', name: 'Telangana' },
  { code: '37', name: 'Andhra Pradesh (New)' },
  { code: '38', name: 'Ladakh' },
  { code: '97', name: 'Other Territory' }
];

@Injectable({
  providedIn: 'root'
})
export class SetupService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://127.0.0.1:5000/api/v1';

  // Signals
  public setupStatus = signal<SetupStatus | null>(null);
  public isLoading = signal<boolean>(false);
  public errorMessage = signal<string | null>(null);

  public checkStatus(): Observable<ApiResponse<SetupStatus>> {
    this.isLoading.set(true);
    return this.http.get<ApiResponse<SetupStatus>>(`${this.baseUrl}/setup/status`).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.setupStatus.set(res.data);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err?.message || 'Failed to check setup status.');
        }
      })
    );
  }

  public completeSetup(request: InitialSetupRequest): Observable<ApiResponse<SetupStatus>> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    return this.http.post<ApiResponse<SetupStatus>>(`${this.baseUrl}/setup/initialize`, request).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.setupStatus.set(res.data);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err?.error?.message || err?.message || 'Failed to complete setup.');
        }
      })
    );
  }

  public getCompanyProfile(): Observable<ApiResponse<CompanyProfileDto>> {
    return this.http.get<ApiResponse<CompanyProfileDto>>(`${this.baseUrl}/company/profile`);
  }

  public updateLetterheadSettings(req: UpdateLetterheadSettingsRequest): Observable<ApiResponse<CompanyProfileDto>> {
    return this.http.put<ApiResponse<CompanyProfileDto>>(`${this.baseUrl}/company/letterhead-settings`, req);
  }

  public uploadLetterhead(base64Data: string, fileName: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/company/letterhead`, { base64Data, fileName });
  }

  public removeLetterhead(): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/company/letterhead`);
  }
}

