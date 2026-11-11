import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export interface FinancialOverviewReport {
  totalSales: number;
  totalPurchases: number;
  totalExpenses: number;
  grossProfit: number;
  netProfit: number;
  profitMarginPercent: number;
  totalReceivables: number;
  totalPayables: number;
  totalStockValuation: number;
  fromDate: string;
  toDate: string;
}

export interface MonthlyFinancialTrend {
  monthLabel: string;
  year: number;
  month: number;
  salesAmount: number;
  purchaseAmount: number;
  expenseAmount: number;
  netProfit: number;
}

export interface GstLiabilitySummary {
  totalOutwardTaxable: number;
  outwardCgst: number;
  outwardSgst: number;
  outwardIgst: number;
  totalOutwardTax: number;
  totalInwardTaxable: number;
  inwardEligibleCgst: number;
  inwardEligibleSgst: number;
  inwardEligibleIgst: number;
  totalEligibleItc: number;
  netPayableCgst: number;
  netPayableSgst: number;
  netPayableIgst: number;
  totalNetGstPayable: number;
}

export interface AgingBucket {
  bucketName: string;
  totalReceivableAmount: number;
  invoicesCount: number;
  totalPayableAmount: number;
  billsCount: number;
}

export interface PartyAgingDetail {
  partyId: string;
  partyName: string;
  gstin?: string;
  isCustomer: boolean;
  currentBalance: number;
  days0To30: number;
  days31To60: number;
  days61To90: number;
  days90Plus: number;
}

export interface ProductMovementVelocity {
  productId: string;
  productName: string;
  sku: string;
  totalSoldQuantity: number;
  totalRevenue: number;
  currentStock: number;
  velocityClassification: string;
}

export interface ComprehensiveReportsBundle {
  overview: FinancialOverviewReport;
  monthlyTrends: MonthlyFinancialTrend[];
  gstSummary: GstLiabilitySummary;
  agingBuckets: AgingBucket[];
  topOverdueParties: PartyAgingDetail[];
  productVelocities: ProductMovementVelocity[];
}

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5000/api/v1/reports';

  getComprehensiveReports(fromDate?: string, toDate?: string): Observable<ComprehensiveReportsBundle> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<ApiResponse<ComprehensiveReportsBundle>>(`${this.baseUrl}/comprehensive`, { params })
      .pipe(map(r => r.data));
  }

  getFinancialOverview(fromDate?: string, toDate?: string): Observable<FinancialOverviewReport> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<ApiResponse<FinancialOverviewReport>>(`${this.baseUrl}/financial-overview`, { params })
      .pipe(map(r => r.data));
  }

  getGstSummary(fromDate?: string, toDate?: string): Observable<GstLiabilitySummary> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<ApiResponse<GstLiabilitySummary>>(`${this.baseUrl}/gst-summary`, { params })
      .pipe(map(r => r.data));
  }

  getAgingBuckets(): Observable<AgingBucket[]> {
    return this.http.get<ApiResponse<AgingBucket[]>>(`${this.baseUrl}/aging-buckets`)
      .pipe(map(r => r.data));
  }

  getProductVelocities(fromDate?: string, toDate?: string): Observable<ProductMovementVelocity[]> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<ApiResponse<ProductMovementVelocity[]>>(`${this.baseUrl}/product-velocity`, { params })
      .pipe(map(r => r.data));
  }
}

