import { api } from './api-client';
import type { ApiResponse } from './api-client';

export interface RoleResponse {
  id: number;
  name: string;
  description: string;
}

export interface CreateRoleRequest {
  name: string;
  description: string;
}

export interface UpdateRoleRequest {
  roleId: number;
  name: string;
  description: string;
}

export interface RoleClaimViewModel {
  claimType: string;
  claimValue: string;
  description: string;
  selected?: boolean;
}

export interface RoleClaimResponse {
  role: RoleResponse;
  roleClaims: RoleClaimViewModel[];
}

export interface UpdateRoleClaimsRequest {
  roleId: number;
  roleClaims: RoleClaimViewModel[];
}

export const rolesApi = {
  getAll: (): Promise<ApiResponse<RoleResponse[]>> => {
    return api.get('api/v1/roles/all');
  },

  getById: (roleId: number): Promise<ApiResponse<RoleResponse>> => {
    return api.get(`api/v1/roles/${roleId}`);
  },

  create: (data: CreateRoleRequest): Promise<ApiResponse> => {
    return api.post('api/v1/roles', data);
  },

  update: (data: UpdateRoleRequest): Promise<ApiResponse> => {
    return api.put('api/v1/roles', data);
  },

  delete: (roleId: number): Promise<ApiResponse> => {
    return api.delete(`api/v1/roles/${roleId}`);
  },

  getPermissions: (roleId: number): Promise<ApiResponse<RoleClaimResponse>> => {
    return api.get(`api/v1/roles/permissions/${roleId}`);
  },

  updatePermissions: (data: UpdateRoleClaimsRequest): Promise<ApiResponse> => {
    return api.put('api/v1/roles/update-permissions', data);
  },
};