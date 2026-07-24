import { api, BASE_URL } from './api-client';
import type { ApiResponse } from './api-client';

export interface CategoryResponse {
  id: number;
  name: string;
  slug: string;
  parentId: number | null;
  sortOrder: number;
  isActive: boolean;
  softDeleted: boolean;
  rowVersion: string;
}

export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
}

export interface PagedFilterRequest {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  isActive?: boolean | null;
  includeDeleted?: boolean;
}

export interface CreateCategoryRequest {
  name: string;
  slug: string;
  parentId: number | null;
  isActive: boolean;
  sortOrder: number;
}

export interface UpdateCategoryRequest {
  id: number;
  name: string;
  slug: string;
  parentId: number | null;
  isActive: boolean;
  sortOrder: number;
  rowVersion: string;
}

export interface CategoryLookupDto {
  id: number;
  name: string;
  parentId: number | null;
}

export const categoriesApi = {
  getPagedList: (params: PagedFilterRequest): Promise<ApiResponse<PagedResult<CategoryResponse>>> => {
    const query = new URLSearchParams({
      pageNumber: String(params.pageNumber),
      pageSize: String(params.pageSize),
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.sortBy && { sortBy: params.sortBy }),
      ...(params.sortDirection && { sortDirection: params.sortDirection }),
      ...(params.isActive !== undefined && params.isActive !== null && { isActive: String(params.isActive) }),
      ...(params.includeDeleted !== undefined && { includeDeleted: String(params.includeDeleted) }),
    });
    return api.get(`api/v1/categories/paged?${query.toString()}`);
  },

  getAll: (isActive?: boolean): Promise<ApiResponse<CategoryResponse[]>> => {
    const query = isActive !== undefined ? `?isActive=${isActive}` : '';
    return api.get(`api/v1/categories${query}`);
  },

  getForList: (): Promise<ApiResponse<CategoryLookupDto[]>> => {
    return api.get('api/v1/categories/for-list');
  },

  getById: (id: number): Promise<ApiResponse<CategoryResponse>> => {
    return api.get(`api/v1/categories/${id}`);
  },

  create: (data: CreateCategoryRequest): Promise<ApiResponse<number>> => {
    return api.post('api/v1/categories', data);
  },

  update: (data: UpdateCategoryRequest): Promise<ApiResponse> => {
    return api.put('api/v1/categories', data);
  },

  changeStatus: (id: number, isActive: boolean): Promise<ApiResponse<number>> => {
    return api.put(`api/v1/categories/${id}/status?isActive=${isActive}`);
  },

  restore: (id: number): Promise<ApiResponse<number>> => {
    return api.post(`api/v1/categories/${id}/restore`);
  },

  delete: (id: number): Promise<ApiResponse> => {
    return api.delete(`api/v1/categories/${id}`);
  },

  exportCategories: async (params: Omit<PagedFilterRequest, 'pageNumber' | 'pageSize'> & { exportFormat: 'excel' | 'pdf' }): Promise<void> => {
    const query = new URLSearchParams({
      exportFormat: params.exportFormat,
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.sortBy && { sortBy: params.sortBy }),
      ...(params.sortDirection && { sortDirection: params.sortDirection }),
      ...(params.isActive !== undefined && params.isActive !== null && { isActive: String(params.isActive) }),
    });
    const token = localStorage.getItem('token');
    const headers: Record<string, string> = {};
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const url = `${BASE_URL.replace(/\/$/, '')}/api/v1/categories/export?${query.toString()}`;
    const response = await fetch(url, {
      method: 'GET',
      headers
    });

    if (!response.ok) {
      let errorMessage = 'Failed to export categories.';
      try {
        const data = await response.json();
        if (data?.messages && data.messages.length > 0) {
          errorMessage = data.messages[0];
        }
      } catch {
        errorMessage = response.statusText || errorMessage;
      }
      throw new Error(errorMessage);
    }

    const blob = await response.blob();
    const contentDisposition = response.headers.get('Content-Disposition');
    let fileName = `Categories_${new Date().toISOString().slice(0, 19).replace(/[-T:]/g, '')}.${params.exportFormat === 'pdf' ? 'pdf' : 'xlsx'}`;

    if (contentDisposition) {
      const match = contentDisposition.match(/filename\*?=(?:UTF-8'')?([^;\n]+)/i) || contentDisposition.match(/filename="?([^";\n]+)"?/i);
      if (match && match[1]) {
        fileName = decodeURIComponent(match[1]);
      }
    }

    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
  }
};