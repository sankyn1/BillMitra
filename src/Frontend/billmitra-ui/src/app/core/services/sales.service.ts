import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export enum DocumentType {
  TaxInvoice = 1,
  BillOfSupply = 2,
  Quotation = 3,
  ProformaInvoice = 4,
  CreditNote = 5,
  DebitNote = 6,
  DeliveryChallan = 7
}

export enum DocumentStatus {
  Draft = 1,
  Finalized = 2,
  Cancelled = 3
}

export enum GstSupplyType {
  IntraState = 1,
  InterState = 2
}

export interface SalesDocumentItemDto {
  id: string;
  productId?: string;
  itemName: string;
  hsnSacCode: string;
  quantity: number;
  unitCode: string;
  unitPrice: number;
  discountPercent: number;
  discountAmount: number;
  taxableAmount: number;
  gstRatePercent: number;
  cgstRatePercent: number;
  cgstAmount: number;
  sgstRatePercent: number;
  sgstAmount: number;
  igstRatePercent: number;
  igstAmount: number;
  cessRatePercent: number;
  cessAmount: number;
  totalAmount: number;
}

export interface SalesDocumentDto {
  id: string;
  documentType: DocumentType;
  documentStatus: DocumentStatus;
  documentNumber?: string;
  draftNumber: string;
  documentDate: string;
  dueDate?: string;
  companyId: string;
  partyId?: string;
  customerName: string;
  customerGstin?: string;
  billingAddress?: string;
  shippingAddress?: string;
  placeOfSupplyStateCode: string;
  supplierStateCode: string;
  isReverseCharge: boolean;
  supplyType: GstSupplyType;
  subTotalTaxable: number;
  cgstTotal: number;
  sgstTotal: number;
  igstTotal: number;
  cessTotal: number;
  totalTax: number;
  roundOffAmount: number;
  grandTotal: number;
  totalAmountInWords: string;
  notes?: string;
  termsAndConditions?: string;
  createdAtUtc: string;
  finalizedAtUtc?: string;
  cancelledAtUtc?: string;
  cancellationReason?: string;
  items: SalesDocumentItemDto[];
}

export interface SalesDocumentItemInput {
  productId?: string | null;
  itemName: string;
  hsnSacCode: string;
  quantity: number;
  unitCode: string;
  unitPrice: number;
  discountPercent: number;
  gstRatePercent: number;
  cessRatePercent?: number;
}

export interface CreateOrUpdateSalesDocumentRequest {
  documentType: DocumentType;
  documentDate: string;
  dueDate?: string | null;
  partyId?: string | null;
  customerName: string;
  customerGstin?: string | null;
  billingAddress?: string | null;
  shippingAddress?: string | null;
  placeOfSupplyStateCode: string;
  isReverseCharge: boolean;
  notes?: string | null;
  termsAndConditions?: string | null;
  items: SalesDocumentItemInput[];
}

export interface CalculateGstRequest {
  supplierStateCode: string;
  placeOfSupplyStateCode: string;
  isReverseCharge: boolean;
  items: SalesDocumentItemInput[];
}

export interface CalculateGstResponse {
  supplyType: GstSupplyType;
  subTotalTaxable: number;
  cgstTotal: number;
  sgstTotal: number;
  igstTotal: number;
  cessTotal: number;
  totalTax: number;
  roundOffAmount: number;
  grandTotal: number;
  totalAmountInWords: string;
  items: any[];
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class SalesService {
  private readonly baseUrl = 'http://127.0.0.1:5000/api/v1/sales';

  documents = signal<SalesDocumentDto[]>([]);
  activeDocument = signal<SalesDocumentDto | null>(null);
  isLoading = signal<boolean>(false);

  constructor(private http: HttpClient) {}

  loadDocuments(documentType?: DocumentType, status?: DocumentStatus, search?: string): Observable<ApiResponse<SalesDocumentDto[]>> {
    this.isLoading.set(true);
    let params = new HttpParams();
    if (documentType) params = params.set('documentType', documentType.toString());
    if (status) params = params.set('status', status.toString());
    if (search) params = params.set('search', search);

    return this.http.get<ApiResponse<SalesDocumentDto[]>>(this.baseUrl, { params }).pipe(
      tap({
        next: (res) => {
          if (res.success) {
            this.documents.set(res.data);
          }
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  getDocumentById(id: string): Observable<ApiResponse<SalesDocumentDto>> {
    return this.http.get<ApiResponse<SalesDocumentDto>>(`${this.baseUrl}/${id}`);
  }

  calculateGst(req: CalculateGstRequest): Observable<ApiResponse<CalculateGstResponse>> {
    return this.http.post<ApiResponse<CalculateGstResponse>>(`${this.baseUrl}/calculate`, req);
  }

  createDraft(req: CreateOrUpdateSalesDocumentRequest): Observable<ApiResponse<SalesDocumentDto>> {
    return this.http.post<ApiResponse<SalesDocumentDto>>(`${this.baseUrl}/draft`, req);
  }

  updateDraft(id: string, req: CreateOrUpdateSalesDocumentRequest): Observable<ApiResponse<SalesDocumentDto>> {
    return this.http.put<ApiResponse<SalesDocumentDto>>(`${this.baseUrl}/draft/${id}`, req);
  }

  finalizeDocument(id: string, idempotencyKey?: string): Observable<ApiResponse<SalesDocumentDto>> {
    return this.http.post<ApiResponse<SalesDocumentDto>>(`${this.baseUrl}/${id}/finalize`, { idempotencyKey });
  }

  cancelDocument(id: string, cancellationReason: string): Observable<ApiResponse<SalesDocumentDto>> {
    return this.http.post<ApiResponse<SalesDocumentDto>>(`${this.baseUrl}/${id}/cancel`, { cancellationReason });
  }

  downloadPdf(id: string, format: string = 'a4'): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/pdf?format=${format}`, {
      responseType: 'blob'
    });
  }
}
