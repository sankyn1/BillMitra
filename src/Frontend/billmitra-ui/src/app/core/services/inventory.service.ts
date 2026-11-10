import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface WarehouseDto {
  id: string;
  name: string;
  code: string;
  address?: string;
  city?: string;
  stateCode?: string;
  isDefault: boolean;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateWarehouseRequest {
  name: string;
  code: string;
  address?: string;
  city?: string;
  stateCode?: string;
  isDefault: boolean;
}

export interface StockItemBatchDto {
  id: string;
  productId: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  batchNumber: string;
  manufacturingDate?: string;
  expiryDate?: string;
  quantityOnHand: number;
  purchasePrice: number;
  mrp: number;
  isExpired: boolean;
  isExpiringSoon: boolean;
  isActive: boolean;
}

export interface CreateStockBatchRequest {
  productId: string;
  warehouseId: string;
  batchNumber: string;
  manufacturingDate?: string;
  expiryDate?: string;
  quantityOnHand: number;
  purchasePrice: number;
  mrp: number;
}

export interface StockMovementDto {
  id: string;
  productId: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  batchId?: string;
  batchNumber?: string;
  movementType: number;
  movementTypeName: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  balanceQuantityAfter: number;
  referenceDocumentNumber?: string;
  referenceDocumentId?: string;
  remarks?: string;
  movementDate: string;
}

export interface StockAdjustmentRequest {
  productId: string;
  warehouseId: string;
  batchId?: string;
  reason: number;
  physicalQuantity: number;
  remarks?: string;
}

export interface StockAdjustmentDto {
  id: string;
  productId: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  batchId?: string;
  batchNumber?: string;
  reason: number;
  reasonName: string;
  systemQuantity: number;
  physicalQuantity: number;
  adjustmentQuantity: number;
  unitCost: number;
  impactValue: number;
  remarks?: string;
  adjustmentDate: string;
}

export interface InterWarehouseTransferRequest {
  sourceWarehouseId: string;
  destinationWarehouseId: string;
  productId: string;
  quantity: number;
  remarks?: string;
}

export interface StockTransferDto {
  id: string;
  transferNumber: string;
  sourceWarehouseId: string;
  sourceWarehouseName: string;
  destinationWarehouseId: string;
  destinationWarehouseName: string;
  productId: string;
  productName: string;
  quantity: number;
  remarks?: string;
  transferDate: string;
}

export interface ProductStockSummaryDto {
  productId: string;
  productName: string;
  sku: string;
  hsnCode: string;
  unit: string;
  totalStockQuantity: number;
  purchasePrice: number;
  sellingPrice: number;
  totalStockValuation: number;
  minStockAlert: number;
  isLowStock: boolean;
  activeBatchesCount: number;
  expiredBatchesCount: number;
}

export interface InventoryDashboardMetricsDto {
  totalStockValuation: number;
  totalActiveProducts: number;
  lowStockAlertCount: number;
  expiredBatchesCount: number;
  warehousesCount: number;
  lowStockItems: ProductStockSummaryDto[];
  expiringSoonBatches: StockItemBatchDto[];
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
export class InventoryService {
  private http = inject(HttpClient);
  private invBaseUrl = '/api/v1/inventory';
  private whBaseUrl = '/api/v1/warehouses';

  metrics = signal<InventoryDashboardMetricsDto | null>(null);
  stockSummaries = signal<ProductStockSummaryDto[]>([]);
  movements = signal<StockMovementDto[]>([]);
  batches = signal<StockItemBatchDto[]>([]);
  warehouses = signal<WarehouseDto[]>([]);
  loading = signal<boolean>(false);

  loadMetrics(): Observable<ApiResponse<InventoryDashboardMetricsDto>> {
    this.loading.set(true);
    return this.http.get<ApiResponse<InventoryDashboardMetricsDto>>(`${this.invBaseUrl}/metrics`).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.metrics.set(res.data);
        }
        this.loading.set(false);
      })
    );
  }

  loadStockSummaries(): Observable<ApiResponse<ProductStockSummaryDto[]>> {
    return this.http.get<ApiResponse<ProductStockSummaryDto[]>>(`${this.invBaseUrl}/stock-summary`).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.stockSummaries.set(res.data);
        }
      })
    );
  }

  loadMovements(productId?: string, warehouseId?: string): Observable<ApiResponse<StockMovementDto[]>> {
    let params = new HttpParams();
    if (productId) params = params.set('productId', productId);
    if (warehouseId) params = params.set('warehouseId', warehouseId);

    return this.http.get<ApiResponse<StockMovementDto[]>>(`${this.invBaseUrl}/movements`, { params }).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.movements.set(res.data);
        }
      })
    );
  }

  loadBatches(productId?: string, warehouseId?: string): Observable<ApiResponse<StockItemBatchDto[]>> {
    let params = new HttpParams();
    if (productId) params = params.set('productId', productId);
    if (warehouseId) params = params.set('warehouseId', warehouseId);

    return this.http.get<ApiResponse<StockItemBatchDto[]>>(`${this.invBaseUrl}/batches`, { params }).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.batches.set(res.data);
        }
      })
    );
  }

  createBatch(req: CreateStockBatchRequest): Observable<ApiResponse<StockItemBatchDto>> {
    return this.http.post<ApiResponse<StockItemBatchDto>>(`${this.invBaseUrl}/batches`, req).pipe(
      tap(() => {
        this.loadMetrics().subscribe();
        this.loadBatches().subscribe();
        this.loadStockSummaries().subscribe();
      })
    );
  }

  recordAdjustment(req: StockAdjustmentRequest): Observable<ApiResponse<StockAdjustmentDto>> {
    return this.http.post<ApiResponse<StockAdjustmentDto>>(`${this.invBaseUrl}/adjustments`, req).pipe(
      tap(() => {
        this.loadMetrics().subscribe();
        this.loadStockSummaries().subscribe();
        this.loadMovements().subscribe();
      })
    );
  }

  recordTransfer(req: InterWarehouseTransferRequest): Observable<ApiResponse<StockTransferDto>> {
    return this.http.post<ApiResponse<StockTransferDto>>(`${this.invBaseUrl}/transfers`, req).pipe(
      tap(() => {
        this.loadMetrics().subscribe();
        this.loadStockSummaries().subscribe();
        this.loadMovements().subscribe();
      })
    );
  }

  loadWarehouses(): Observable<ApiResponse<WarehouseDto[]>> {
    return this.http.get<ApiResponse<WarehouseDto[]>>(this.whBaseUrl).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.warehouses.set(res.data);
        }
      })
    );
  }

  createWarehouse(req: CreateWarehouseRequest): Observable<ApiResponse<WarehouseDto>> {
    return this.http.post<ApiResponse<WarehouseDto>>(this.whBaseUrl, req).pipe(
      tap(() => this.loadWarehouses().subscribe())
    );
  }
}

