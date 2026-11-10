import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { ApiResponse } from './setup.service';

export interface UnitDto {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}

export interface ProductCategoryDto {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface ProductDto {
  id: string;
  itemCode: string;
  name: string;
  description?: string;
  itemType: number;
  categoryId?: string;
  categoryName?: string;
  hsnSacCode: string;
  unitCode: string;
  purchasePrice: number;
  salePrice: number;
  gstRatePercent: number;
  cessPercent: number;
  isTaxInclusive: boolean;
  openingStock: number;
  currentStock: number;
  minStockAlert: number;
  isActive: boolean;
}

export interface CreateOrUpdateProductRequest {
  itemCode: string;
  name: string;
  description?: string;
  itemType: number;
  categoryId?: string;
  hsnSacCode: string;
  unitCode: string;
  purchasePrice: number;
  salePrice: number;
  gstRatePercent: number;
  cessPercent: number;
  isTaxInclusive: boolean;
  openingStock: number;
  minStockAlert: number;
}

export interface PartyDto {
  id: string;
  partyType: number; // 1 = Customer, 2 = Supplier, 3 = Both
  name: string;
  tradeName?: string;
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
  creditLimit: number;
  currentBalance: number;
  isActive: boolean;
}

export interface CreateOrUpdatePartyRequest {
  partyType: number;
  name: string;
  tradeName?: string;
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
  creditLimit: number;
}

@Injectable({
  providedIn: 'root'
})
export class MastersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://127.0.0.1:5000/api/v1';

  public products = signal<ProductDto[]>([]);
  public parties = signal<PartyDto[]>([]);
  public units = signal<UnitDto[]>([]);
  public categories = signal<ProductCategoryDto[]>([]);
  public isLoading = signal<boolean>(false);
  public errorMessage = signal<string | null>(null);

  // Products API
  public loadProducts(search?: string, categoryId?: string): Observable<ApiResponse<ProductDto[]>> {
    this.isLoading.set(true);
    let url = `${this.baseUrl}/products`;
    const params: string[] = [];
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    if (categoryId) params.push(`categoryId=${encodeURIComponent(categoryId)}`);
    if (params.length > 0) url += `?${params.join('&')}`;

    return this.http.get<ApiResponse<ProductDto[]>>(url).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.products.set(res.data);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err?.message || 'Failed to load products.');
        }
      })
    );
  }

  public createProduct(req: CreateOrUpdateProductRequest): Observable<ApiResponse<ProductDto>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<ProductDto>>(`${this.baseUrl}/products`, req).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.products.update(list => [...list, res.data!]);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err?.error?.message || err?.message || 'Error saving product.');
        }
      })
    );
  }

  public updateProduct(id: string, req: CreateOrUpdateProductRequest): Observable<ApiResponse<ProductDto>> {
    this.isLoading.set(true);
    return this.http.put<ApiResponse<ProductDto>>(`${this.baseUrl}/products/${id}`, req).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.products.update(list => list.map(p => p.id === id ? res.data! : p));
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err?.error?.message || err?.message || 'Error updating product.');
        }
      })
    );
  }

  public deleteProduct(id: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/products/${id}`).pipe(
      tap({
        next: (res) => {
          if (res.success) {
            this.products.update(list => list.filter(p => p.id !== id));
          }
        }
      })
    );
  }

  public loadUnits(): Observable<ApiResponse<UnitDto[]>> {
    return this.http.get<ApiResponse<UnitDto[]>>(`${this.baseUrl}/products/units`).pipe(
      tap(res => {
        if (res.success && res.data) this.units.set(res.data);
      })
    );
  }

  public loadCategories(): Observable<ApiResponse<ProductCategoryDto[]>> {
    return this.http.get<ApiResponse<ProductCategoryDto[]>>(`${this.baseUrl}/products/categories`).pipe(
      tap(res => {
        if (res.success && res.data) this.categories.set(res.data);
      })
    );
  }

  // Parties API
  public loadParties(partyType?: number, search?: string): Observable<ApiResponse<PartyDto[]>> {
    this.isLoading.set(true);
    let url = `${this.baseUrl}/parties`;
    const params: string[] = [];
    if (partyType) params.push(`partyType=${partyType}`);
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    if (params.length > 0) url += `?${params.join('&')}`;

    return this.http.get<ApiResponse<PartyDto[]>>(url).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.parties.set(res.data);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err?.message || 'Failed to load parties.');
        }
      })
    );
  }

  public createParty(req: CreateOrUpdatePartyRequest): Observable<ApiResponse<PartyDto>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<PartyDto>>(`${this.baseUrl}/parties`, req).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.parties.update(list => [...list, res.data!]);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err?.error?.message || err?.message || 'Error saving party.');
        }
      })
    );
  }

  public updateParty(id: string, req: CreateOrUpdatePartyRequest): Observable<ApiResponse<PartyDto>> {
    this.isLoading.set(true);
    return this.http.put<ApiResponse<PartyDto>>(`${this.baseUrl}/parties/${id}`, req).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.parties.update(list => list.map(p => p.id === id ? res.data! : p));
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err?.error?.message || err?.message || 'Error updating party.');
        }
      })
    );
  }

  public deleteParty(id: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/parties/${id}`).pipe(
      tap({
        next: (res) => {
          if (res.success) {
            this.parties.update(list => list.filter(p => p.id !== id));
          }
        }
      })
    );
  }
}

