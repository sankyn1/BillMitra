import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface PurchaseDocumentItemInput {
  productId?: string;
  itemName: string;
  hsnCode: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  discountPercent: number;
  gstRate: number;
  itcEligibility: number;
}

export interface CreateOrUpdatePurchaseRequest {
  documentType: number;
  supplierId?: string;
  supplierName: string;
  supplierGstin?: string;
  supplierAddress?: string;
  supplierStateCode: string;
  placeOfSupplyStateCode: string;
  supplierInvoiceNumber: string;
  supplierInvoiceDate: string;
  documentDate: string;
  dueDate?: string;
  isReverseCharge: boolean;
  defaultItcEligibility: number;
  notes?: string;
  items: PurchaseDocumentItemInput[];
}

export interface PurchaseDocumentItemDto {
  id: string;
  productId?: string;
  itemName: string;
  hsnCode: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  discountPercent: number;
  discountAmount: number;
  gstRate: number;
  taxableAmount: number;
  cgstRate: number;
  cgstAmount: number;
  sgstRate: number;
  sgstAmount: number;
  igstRate: number;
  igstAmount: number;
  cessRate: number;
  cessAmount: number;
  totalAmount: number;
  itcEligibility: number;
  itcEligibilityName: string;
}

export interface PurchaseDocumentDto {
  id: string;
  documentType: number;
  documentTypeName: string;
  documentStatus: number;
  documentStatusName: string;
  supplierId?: string;
  supplierName: string;
  supplierGstin?: string;
  supplierAddress?: string;
  supplierStateCode: string;
  placeOfSupplyStateCode: string;
  supplierInvoiceNumber: string;
  supplierInvoiceDate: string;
  documentDate: string;
  dueDate?: string;
  isReverseCharge: boolean;
  defaultItcEligibility: number;
  subTotalTaxable: number;
  cgstTotal: number;
  sgstTotal: number;
  igstTotal: number;
  cessTotal: number;
  totalTax: number;
  roundOffAmount: number;
  grandTotal: number;
  paidAmount: number;
  outstandingBalance: number;
  notes?: string;
  createdAtUtc: string;
  finalizedAtUtc?: string;
  items: PurchaseDocumentItemDto[];
}

export interface ExpenseCategoryDto {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface CreateExpenseRequest {
  expenseCategoryId: string;
  expenseDate: string;
  amount: number;
  gstRate: number;
  isItcClaimable: boolean;
  paymentMode: number;
  referenceNumber?: string;
  payeeName?: string;
  notes?: string;
}

export interface ExpenseDto {
  id: string;
  expenseCategoryId: string;
  expenseCategoryName: string;
  voucherNumber: string;
  expenseDate: string;
  amount: number;
  gstRate: number;
  gstAmount: number;
  totalAmount: number;
  isItcClaimable: boolean;
  paymentMode: number;
  paymentModeName: string;
  referenceNumber?: string;
  payeeName?: string;
  notes?: string;
  createdAtUtc: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class PurchaseExpenseService {
  private http = inject(HttpClient);
  private purchaseBaseUrl = '/api/v1/purchases';
  private expenseBaseUrl = '/api/v1/expenses';

  purchases = signal<PurchaseDocumentDto[]>([]);
  expenses = signal<ExpenseDto[]>([]);
  categories = signal<ExpenseCategoryDto[]>([]);
  loading = signal<boolean>(false);

  loadPurchases(docType?: number, status?: number): Observable<ApiResponse<PurchaseDocumentDto[]>> {
    this.loading.set(true);
    let params = new HttpParams();
    if (docType !== undefined) params = params.set('documentType', docType);
    if (status !== undefined) params = params.set('status', status);

    return this.http.get<ApiResponse<PurchaseDocumentDto[]>>(this.purchaseBaseUrl, { params }).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.purchases.set(res.data);
        }
        this.loading.set(false);
      })
    );
  }

  createPurchase(req: CreateOrUpdatePurchaseRequest): Observable<ApiResponse<PurchaseDocumentDto>> {
    return this.http.post<ApiResponse<PurchaseDocumentDto>>(this.purchaseBaseUrl, req).pipe(
      tap(() => this.loadPurchases().subscribe())
    );
  }

  finalizePurchase(id: string, notes?: string): Observable<ApiResponse<PurchaseDocumentDto>> {
    return this.http.post<ApiResponse<PurchaseDocumentDto>>(`${this.purchaseBaseUrl}/${id}/finalize`, { notes }).pipe(
      tap(() => this.loadPurchases().subscribe())
    );
  }

  cancelPurchase(id: string, reason: string): Observable<ApiResponse<PurchaseDocumentDto>> {
    return this.http.post<ApiResponse<PurchaseDocumentDto>>(`${this.purchaseBaseUrl}/${id}/cancel?reason=${encodeURIComponent(reason)}`, {}).pipe(
      tap(() => this.loadPurchases().subscribe())
    );
  }

  loadCategories(): Observable<ApiResponse<ExpenseCategoryDto[]>> {
    return this.http.get<ApiResponse<ExpenseCategoryDto[]>>(`${this.expenseBaseUrl}/categories`).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.categories.set(res.data);
        }
      })
    );
  }

  createCategory(name: string, description?: string): Observable<ApiResponse<ExpenseCategoryDto>> {
    return this.http.post<ApiResponse<ExpenseCategoryDto>>(`${this.expenseBaseUrl}/categories`, { name, description }).pipe(
      tap(() => this.loadCategories().subscribe())
    );
  }

  loadExpenses(): Observable<ApiResponse<ExpenseDto[]>> {
    this.loading.set(true);
    return this.http.get<ApiResponse<ExpenseDto[]>>(this.expenseBaseUrl).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.expenses.set(res.data);
        }
        this.loading.set(false);
      })
    );
  }

  createExpense(req: CreateExpenseRequest): Observable<ApiResponse<ExpenseDto>> {
    return this.http.post<ApiResponse<ExpenseDto>>(this.expenseBaseUrl, req).pipe(
      tap(() => this.loadExpenses().subscribe())
    );
  }
}

