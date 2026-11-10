import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string;
  errors: string[];
  statusCode: number;
}

export interface Gstr1SummaryDto {
  taxPeriod: string;
  financialYear: string;
  b2bTotalTaxable: number;
  b2bTotalTax: number;
  b2bCount: number;
  b2clTotalTaxable: number;
  b2clTotalTax: number;
  b2clCount: number;
  b2csTotalTaxable: number;
  b2csTotalTax: number;
  b2csCount: number;
  cdnrTotalTaxable: number;
  cdnrTotalTax: number;
  cdnrCount: number;
  cdnurTotalTaxable: number;
  cdnurTotalTax: number;
  cdnurCount: number;
  hsnCount: number;
  totalInvoicesCount: number;
  totalCancelledCount: number;
  totalTaxableValue: number;
  totalCgst: number;
  totalSgst: number;
  totalIgst: number;
  totalCess: number;
  totalTaxLiability: number;
  grandTotal: number;
  b2bInvoices: Gstr1B2bInvoiceDto[];
  b2clInvoices: Gstr1B2clInvoiceDto[];
  b2csSections: Gstr1B2csSectionDto[];
  cdnrNotes: Gstr1CdnrNoteDto[];
  documentsSummary: Gstr1DocSummaryDto[];
}

export interface Gstr1B2bInvoiceDto {
  customerGstin: string;
  customerName: string;
  invoiceNumber: string;
  invoiceDate: string;
  invoiceValue: number;
  placeOfSupply: string;
  reverseCharge: boolean;
  invoiceType: string;
  taxableValue: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  cessAmount: number;
  items: Gstr1ItemDetailDto[];
}

export interface Gstr1B2clInvoiceDto {
  invoiceNumber: string;
  invoiceDate: string;
  invoiceValue: number;
  placeOfSupply: string;
  taxableValue: number;
  igstAmount: number;
  cessAmount: number;
  items: Gstr1ItemDetailDto[];
}

export interface Gstr1B2csSectionDto {
  supplyType: string;
  placeOfSupply: string;
  gstRatePercent: number;
  taxableValue: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  cessAmount: number;
}

export interface Gstr1CdnrNoteDto {
  customerGstin: string;
  customerName: string;
  noteNumber: string;
  noteDate: string;
  noteType: string;
  originalInvoiceNumber: string;
  originalInvoiceDate?: string;
  noteValue: number;
  taxableValue: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  cessAmount: number;
}

export interface Gstr1DocSummaryDto {
  documentType: number;
  documentName: string;
  fromSerial: string;
  toSerial: string;
  totalCount: number;
  cancelledCount: number;
  netIssuedCount: number;
}

export interface Gstr1ItemDetailDto {
  ratePercent: number;
  taxableValue: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  cessAmount: number;
}

export interface HsnSummaryItemDto {
  hsnSacCode: string;
  description: string;
  unitCode: string;
  totalQuantity: number;
  totalValue: number;
  taxableAmount: number;
  gstRatePercent: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  cessAmount: number;
}

export interface Gstr3bSummaryDto {
  taxPeriod: string;
  financialYear: string;
  table31: Gstr3bTable31Dto;
  table32: Gstr3bTable32RowDto[];
  table4: Gstr3bTable4Dto;
  totalLiability: Gstr3bTaxLiabilityDto;
}

export interface Gstr3bTable31Dto {
  outwardTaxableSupplies: Gstr3bTaxRowDto;
  zeroRatedSupplies: Gstr3bTaxRowDto;
  otherOutwardNilExempt: number;
  inwardReverseChargeSupplies: Gstr3bTaxRowDto;
  nonGstOutwardSupplies: number;
}

export interface Gstr3bTable32RowDto {
  placeOfSupplyStateCode: string;
  stateName: string;
  taxableValue: number;
  integratedTaxAmount: number;
}

export interface Gstr3bTable4Dto {
  inwardReverseChargeItc: number;
  allOtherItc: number;
  netItcAvailable: number;
}

export interface Gstr3bTaxRowDto {
  taxableValue: number;
  integratedTax: number;
  centralTax: number;
  stateTax: number;
  cess: number;
}

export interface Gstr3bTaxLiabilityDto {
  taxableValue: number;
  integratedTax: number;
  centralTax: number;
  stateTax: number;
  cess: number;
  totalTax: number;
}

export interface GstValidationResultDto {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

export interface GstReturnFilingDto {
  id: string;
  companyId: string;
  returnType: number;
  financialYear: string;
  taxPeriod: string;
  filingStatus: number;
  arn?: string;
  arnDate?: string;
  validationErrors: string[];
  providerName?: string;
  providerReference?: string;
  totalTaxableValue: number;
  totalTaxLiability: number;
  createdAtUtc: string;
  filedAtUtc?: string;
}

@Injectable({
  providedIn: 'root'
})
export class GstService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://127.0.0.1:5000/api/v1/gst';

  getGstr1Summary(taxPeriod: string): Observable<ApiResponse<Gstr1SummaryDto>> {
    const params = new HttpParams().set('taxPeriod', taxPeriod);
    return this.http.get<ApiResponse<Gstr1SummaryDto>>(`${this.baseUrl}/gstr1/summary`, { params });
  }

  validateGstr1(taxPeriod: string): Observable<ApiResponse<GstValidationResultDto>> {
    const params = new HttpParams().set('taxPeriod', taxPeriod);
    return this.http.get<ApiResponse<GstValidationResultDto>>(`${this.baseUrl}/gstr1/validate`, { params });
  }

  downloadGstr1Json(taxPeriod: string): Observable<Blob> {
    const params = new HttpParams().set('taxPeriod', taxPeriod);
    return this.http.get(`${this.baseUrl}/gstr1/json`, {
      params,
      responseType: 'blob'
    });
  }

  getGstr3bSummary(taxPeriod: string): Observable<ApiResponse<Gstr3bSummaryDto>> {
    const params = new HttpParams().set('taxPeriod', taxPeriod);
    return this.http.get<ApiResponse<Gstr3bSummaryDto>>(`${this.baseUrl}/gstr3b/summary`, { params });
  }

  getHsnSummary(taxPeriod: string): Observable<ApiResponse<HsnSummaryItemDto[]>> {
    const params = new HttpParams().set('taxPeriod', taxPeriod);
    return this.http.get<ApiResponse<HsnSummaryItemDto[]>>(`${this.baseUrl}/hsn-summary`, { params });
  }

  getFilingHistory(): Observable<ApiResponse<GstReturnFilingDto[]>> {
    return this.http.get<ApiResponse<GstReturnFilingDto[]>>(`${this.baseUrl}/filings`);
  }

  submitGstr1(taxPeriod: string, simulateProvider: boolean = true): Observable<ApiResponse<GstReturnFilingDto>> {
    return this.http.post<ApiResponse<GstReturnFilingDto>>(`${this.baseUrl}/gstr1/submit`, {
      taxPeriod,
      simulateProvider
    });
  }

  markAsFiled(filingId: string, arn: string, arnDate: string): Observable<ApiResponse<GstReturnFilingDto>> {
    return this.http.post<ApiResponse<GstReturnFilingDto>>(`${this.baseUrl}/filings/${filingId}/mark-filed`, {
      arn,
      arnDate
    });
  }
}
