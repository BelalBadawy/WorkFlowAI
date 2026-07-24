import { api, BASE_URL } from './api-client';
import type { ApiResponse } from './api-client';
import type { PagedResult } from './categories-api';

export interface AuditTrailResponse {
  id: number;
  userId: number | null;
  userEmail: string | null;
  ipAddress: string | null;
  type: string;
  tableName: string | null;
  dateTime: string;
  oldValues: string | null;
  newValues: string | null;
  affectedColumns: string | null;
  primaryKey: string | null;
}

export interface AuditLogsFilterRequest {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  tableName?: string;
  entityId?: string;
  actionTypes?: string;
  fromDate?: string;
  toDate?: string;
  userId?: number;
}

export const auditLogsApi = {
  getPagedList: (params: AuditLogsFilterRequest): Promise<ApiResponse<PagedResult<AuditTrailResponse>>> => {
    const query = new URLSearchParams({
      pageNumber: String(params.pageNumber),
      pageSize: String(params.pageSize),
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.sortBy && { sortBy: params.sortBy }),
      ...(params.sortDirection && { sortDirection: params.sortDirection }),
      ...(params.tableName && { tableName: params.tableName }),
      ...(params.entityId && { entityId: params.entityId }),
      ...(params.actionTypes && { actionTypes: params.actionTypes }),
      ...(params.fromDate && { fromDate: params.fromDate }),
      ...(params.toDate && { toDate: params.toDate }),
      ...(params.userId !== undefined && params.userId !== null && { userId: String(params.userId) }),
    });
    return api.get(`api/v1/audit-logs/paged?${query.toString()}`);
  },

  exportAuditLogs: async (params: Omit<AuditLogsFilterRequest, 'pageNumber' | 'pageSize'> & { exportFormat: 'excel' | 'pdf' }): Promise<void> => {
    const query = new URLSearchParams({
      exportFormat: params.exportFormat,
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.tableName && { tableName: params.tableName }),
      ...(params.entityId && { entityId: params.entityId }),
      ...(params.actionTypes && { actionTypes: params.actionTypes }),
      ...(params.fromDate && { fromDate: params.fromDate }),
      ...(params.toDate && { toDate: params.toDate }),
      ...(params.userId !== undefined && params.userId !== null && { userId: String(params.userId) }),
    });
    const token = localStorage.getItem('token');
    const headers: Record<string, string> = {};
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const url = `${BASE_URL.replace(/\/$/, '')}/api/v1/audit-logs/export?${query.toString()}`;
    const response = await fetch(url, {
      method: 'GET',
      headers
    });

    if (!response.ok) {
      let errorMessage = 'Failed to export audit logs.';
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
    let fileName = `AuditLogs_${new Date().toISOString().slice(0, 19).replace(/[-T:]/g, '')}.${params.exportFormat === 'pdf' ? 'pdf' : 'xlsx'}`;
    
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