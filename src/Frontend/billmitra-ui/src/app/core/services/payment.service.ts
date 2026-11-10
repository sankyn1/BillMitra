import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface PaymentAllocationDto {
  id: string;
  paymentId: string;
  salesDocumentId: string;
  documentNumber: string;
  documentDate: string;
  invoiceGrandTotal: number;
  previouslyPaid: number;
  outstandingBeforeAllocation: number;
  allocatedAmount: number;
  allocationDate: string;
  notes?: string;
}

export interface PaymentDto {
  id: string;
  paymentNumber: string;
  partyId: string;
  partyName: string;
  partyGstin?: string;
  paymentType: number; // 1 = Receipt, 2 = Payment
  paymentMode: number; // 1 = Cash, 2 = Bank, 3 = Cheque, 4 = UPI, 5 = CC, 6 = DC
  paymentDate: string;
  amount: number;
  allocatedAmount: number;
  unallocatedAmount: number;
  referenceNumber?: string;
  bankName?: string;
  chequeDate?: string;
  notes?: string;
  status: number; // 1 = Active, 2 = Reversed
  createdAtUtc: string;
  createdBy?: string;
  allocations: PaymentAllocationDto[];
}

export interface CreatePaymentAllocationItem {
  salesDocumentId: string;
  allocatedAmount: number;
  notes?: string;
}

export interface CreatePaymentRequest {
  partyId: string;
  paymentType: number;
  paymentMode: number;
  paymentDate: string;
  amount: number;
  referenceNumber?: string;
  bankName?: string;
  chequeDate?: string;
  notes?: string;
  allocations?: CreatePaymentAllocationItem[];
}

export interface OpenInvoiceDto {
  salesDocumentId: string;
  documentNumber: string;
  documentDate: string;
  dueDate?: string;
  grandTotal: number;
  paidAmount: number;
  outstandingBalance: number;
}

export interface LedgerEntryDto {
  date: string;
  voucherType: string;
  voucherNumber: string;
  reference?: string;
  debitAmount: number;
  creditAmount: number;
  runningBalance: number;
  notes?: string;
}

export interface PartyAgingDto {
  partyId: string;
  partyName: string;
  totalInvoiced: number;
  totalPaid: number;
  currentOutstanding: number;
  days0To30: number;
  days31To60: number;
  days61To90: number;
  days90Plus: number;
}

interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data: T;
  errors?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://127.0.0.1:5000/api/v1/payments';

  public readonly payments = signal<PaymentDto[]>([]);
  public readonly loading = signal<boolean>(false);

  loadPayments(type?: number, status?: number, partyId?: string): Observable<ApiResponse<PaymentDto[]>> {
    this.loading.set(true);
    let params = new HttpParams();
    if (type !== undefined && type !== null) params = params.set('type', type);
    if (status !== undefined && status !== null) params = params.set('status', status);
    if (partyId) params = params.set('partyId', partyId);

    return this.http.get<ApiResponse<PaymentDto[]>>(this.baseUrl, { params }).pipe(
      tap({
        next: (res) => {
          if (res.success && res.data) {
            this.payments.set(res.data);
          }
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      })
    );
  }

  getPaymentById(id: string): Observable<ApiResponse<PaymentDto>> {
    return this.http.get<ApiResponse<PaymentDto>>(`${this.baseUrl}/${id}`);
  }

  recordPayment(request: CreatePaymentRequest): Observable<ApiResponse<PaymentDto>> {
    return this.http.post<ApiResponse<PaymentDto>>(this.baseUrl, request).pipe(
      tap((res) => {
        if (res.success && res.data) {
          this.payments.update((current) => [res.data, ...current]);
        }
      })
    );
  }

  reversePayment(id: string, reason: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${id}/reverse`, { reason });
  }

  getOpenInvoices(partyId: string): Observable<ApiResponse<OpenInvoiceDto[]>> {
    return this.http.get<ApiResponse<OpenInvoiceDto[]>>(`${this.baseUrl}/parties/${partyId}/open-invoices`);
  }

  getPartyLedger(partyId: string, fromDate?: string, toDate?: string): Observable<ApiResponse<LedgerEntryDto[]>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<ApiResponse<LedgerEntryDto[]>>(`${this.baseUrl}/parties/${partyId}/ledger`, { params });
  }

  getPartyAging(partyId: string): Observable<ApiResponse<PartyAgingDto>> {
    return this.http.get<ApiResponse<PartyAgingDto>>(`${this.baseUrl}/parties/${partyId}/aging`);
  }
}
