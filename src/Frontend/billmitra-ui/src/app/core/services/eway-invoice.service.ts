import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface EWayBillDto {
  id: string;
  salesDocumentId: string;
  ewayBillNumber: string;
  ewayBillDate: string;
  validUntil: string;
  status: number;
  statusName: string;
  docType: string;
  docNumber: string;
  docDate: string;
  fromGstin: string;
  fromName: string;
  toGstin: string;
  toName: string;
  totalInvoiceValue: number;
  transportationMode: number;
  transporterId?: string;
  transporterName?: string;
  vehicleNo?: string;
  approximateDistanceKm: number;
  canCancel: boolean;
  cancelReason?: string;
  cancelRemarks?: string;
  cancelledAt?: string;
}

export interface EInvoiceDto {
  id: string;
  salesDocumentId: string;
  irn: string;
  ackNo: string;
  ackDate: string;
  signedQrCode: string;
  status: number;
  statusName: string;
  canCancel: boolean;
  cancelReason?: number;
  cancelRemarks?: string;
  cancelledAt?: string;
}

export interface DocumentEWayInvoiceSummaryDto {
  documentId: string;
  documentNumber: string;
  grandTotal: number;
  isEWayBillMandatory: boolean;
  isEInvoiceEligible: boolean;
  ewayBill?: EWayBillDto;
  eInvoice?: EInvoiceDto;
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
export class EWayInvoiceService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5000/api/v1';

  isLoading = signal(false);
  ewayBills = signal<EWayBillDto[]>([]);
  einvoices = signal<EInvoiceDto[]>([]);

  listEWayBills(page = 1, pageSize = 50): Observable<ApiResponse<EWayBillDto[]>> {
    this.isLoading.set(true);
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<EWayBillDto[]>>(`${this.baseUrl}/ewaybill/list`, { params }).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.ewayBills.set(res.data);
          }
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  generateEWayBill(req: {
    salesDocumentId: string;
    transportationMode: number;
    transporterId?: string;
    transporterName?: string;
    transportDocNo?: string;
    transportDocDate?: string;
    vehicleNo?: string;
    vehicleType: number;
    approximateDistanceKm: number;
  }): Observable<ApiResponse<EWayBillDto>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<EWayBillDto>>(`${this.baseUrl}/ewaybill/generate`, req).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }

  updateVehicle(req: {
    ewayBillNumber: string;
    vehicleNo: string;
    fromPlace: string;
    fromStateCode: string;
    reasonCode: string;
    remarks?: string;
  }): Observable<ApiResponse<EWayBillDto>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<EWayBillDto>>(`${this.baseUrl}/ewaybill/update-vehicle`, req).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }

  cancelEWayBill(req: {
    ewayBillNumber: string;
    cancelReasonCode: string;
    cancelRemarks: string;
  }): Observable<ApiResponse<EWayBillDto>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<EWayBillDto>>(`${this.baseUrl}/ewaybill/cancel`, req).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }

  listEInvoices(page = 1, pageSize = 50): Observable<ApiResponse<EInvoiceDto[]>> {
    this.isLoading.set(true);
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<EInvoiceDto[]>>(`${this.baseUrl}/einvoice/list`, { params }).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.einvoices.set(res.data);
          }
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  generateEInvoice(salesDocumentId: string): Observable<ApiResponse<EInvoiceDto>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<EInvoiceDto>>(`${this.baseUrl}/einvoice/generate`, { salesDocumentId }).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }

  cancelEInvoice(req: {
    irn: string;
    cancelReason: number;
    cancelRemarks: string;
  }): Observable<ApiResponse<EInvoiceDto>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<EInvoiceDto>>(`${this.baseUrl}/einvoice/cancel`, req).pipe(
      tap({
        next: () => this.isLoading.set(false),
        error: () => this.isLoading.set(false)
      })
    );
  }

  getDocumentSummary(documentId: string): Observable<ApiResponse<DocumentEWayInvoiceSummaryDto>> {
    return this.http.get<ApiResponse<DocumentEWayInvoiceSummaryDto>>(`${this.baseUrl}/einvoice/document/${documentId}/summary`);
  }
}
