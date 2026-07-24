import { api, BASE_URL } from './api-client';
import type { ApiResponse } from './api-client';

export interface UserResponse {
  id: number;
  fullName: string;
  email: string;
  userName: string;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumber: string;
  isLocked: boolean;
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
  isLocked?: boolean | null;
  roleId?: number | null;
}

export interface UserRegistrationRequest {
  fullName: string;
  email: string;
  password?: string;
  confirmPassword?: string;
  phoneNumber: string;
  autoConfirmEmail: boolean;
  activateUser: boolean;
}

export interface UpdateUserRequest {
  userId: number;
  fullName: string;
  phoneNumber: string;
}

export interface UserRoleViewModel {
  roleName: string;
  roleDescription: string;
}

export interface RoleResponse {
  id: number;
  name: string;
  description: string;
}

export const usersApi = {
  getPagedList: (params: PagedFilterRequest): Promise<ApiResponse<PagedResult<UserResponse>>> => {
    const query = new URLSearchParams({
      pageNumber: String(params.pageNumber),
      pageSize: String(params.pageSize),
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.sortBy && { sortBy: params.sortBy }),
      ...(params.sortDirection && { sortDirection: params.sortDirection }),
      ...(params.isActive !== undefined && params.isActive !== null && { isActive: String(params.isActive) }),
      ...(params.isLocked !== undefined && params.isLocked !== null && { isLocked: String(params.isLocked) }),
      ...(params.roleId !== undefined && params.roleId !== null && { roleId: String(params.roleId) }),
    });
    return api.get(`api/v1/users/paged?${query.toString()}`);
  },

  register: (data: UserRegistrationRequest): Promise<ApiResponse> => {
    return api.post('api/v1/users/register', data);
  },

  update: (data: UpdateUserRequest): Promise<ApiResponse> => {
    return api.put('api/v1/users/update', data);
  },

  changeStatus: (userId: number, activate: boolean): Promise<ApiResponse> => {
    return api.put('api/v1/users/change-status', {
      userId,
      activateOrDeactivate: activate,
    });
  },

  lock: (userId: number): Promise<ApiResponse> => {
    return api.put('api/v1/users/lock-user', { userId });
  },

  unlock: (userId: number): Promise<ApiResponse> => {
    return api.put('api/v1/users/unlock-user', { userId });
  },

  getUserRoles: (userId: number): Promise<ApiResponse<UserRoleViewModel[]>> => {
    return api.get(`api/v1/users/roles/${userId}`);
  },

  updateUserRoles: (userId: number, roles: string[]): Promise<ApiResponse> => {
    return api.put('api/v1/users/user-roles', { userId, roles });
  },

  getRolesAll: (): Promise<ApiResponse<RoleResponse[]>> => {
    return api.get('api/v1/roles/all');
  },

  exportUsers: async (params: Omit<PagedFilterRequest, 'pageNumber' | 'pageSize'> & { exportFormat: 'excel' | 'pdf' }): Promise<void> => {
    const queryParams: Record<string, string> = {
      exportFormat: params.exportFormat
    };
    if (params.searchTerm) queryParams.searchTerm = params.searchTerm;
    if (params.sortBy) queryParams.sortBy = params.sortBy;
    if (params.sortDirection) queryParams.sortDirection = params.sortDirection;
    if (params.isActive !== undefined && params.isActive !== null) {
      queryParams.isActive = String(params.isActive);
    }
    if (params.isLocked !== undefined && params.isLocked !== null) {
      queryParams.isLocked = String(params.isLocked);
    }
    if (params.roleId !== undefined && params.roleId !== null) {
      queryParams.roleId = String(params.roleId);
    }

    const query = new URLSearchParams(queryParams);
    const token = localStorage.getItem('token');
    const headers: Record<string, string> = {};
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const url = `${BASE_URL.replace(/\/$/, '')}/api/v1/users/export?${query.toString()}`;
    const response = await fetch(url, {
      method: 'GET',
      headers
    });

    if (!response.ok) {
      let errorMessage = 'Failed to export users.';
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
    let fileName = `Users_${new Date().toISOString().slice(0, 19).replace(/[-T:]/g, '')}.${params.exportFormat === 'pdf' ? 'pdf' : 'xlsx'}`;

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