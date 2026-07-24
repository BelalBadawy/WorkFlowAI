import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { useUserList, useAvailableRoles } from './useUsers';
import { usersApi } from '../lib/users-api';

vi.mock('../lib/users-api', () => ({
  usersApi: {
    getPagedList: vi.fn(),
    getUserRoles: vi.fn(),
    getRolesAll: vi.fn(),
    register: vi.fn(),
    update: vi.fn(),
    updateUserRoles: vi.fn(),
    lock: vi.fn(),
    unlock: vi.fn(),
    changeStatus: vi.fn(),
  },
}));

vi.mock('../components/ui/toast', () => ({
  useToast: () => ({
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
  }),
}));

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

describe('useUsers Hooks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useUserList', () => {
    it('successfully fetches users list and fetches user roles in parallel', async () => {
      const mockUsers = [
        { id: 1, fullName: 'John Doe', email: 'john@example.com', isActive: true, emailConfirmed: true, phoneNumber: '123', isLocked: false, userName: 'john@example.com' },
        { id: 2, fullName: 'Jane Doe', email: 'jane@example.com', isActive: true, emailConfirmed: true, phoneNumber: '456', isLocked: false, userName: 'jane@example.com' },
      ];

      vi.mocked(usersApi.getPagedList).mockResolvedValue({
        isSuccessful: true,
        messages: [],
        statusCode: 200,
        data: {
          data: mockUsers,
          totalCount: 2,
          currentPage: 1,
          pageSize: 10,
        },
      });

      vi.mocked(usersApi.getUserRoles).mockImplementation(async (userId) => {
        if (userId === 1) {
          return {
            isSuccessful: true,
            messages: [],
            statusCode: 200,
            data: [{ roleName: 'Admin', roleDescription: 'Administrator' }],
          };
        }
        return {
          isSuccessful: true,
          messages: [],
          statusCode: 200,
          data: [{ roleName: 'Basic', roleDescription: 'Basic User' }],
        };
      });

      const { result } = renderHook(
        () => useUserList({ pageNumber: 1, pageSize: 10, searchTerm: '' }),
        { wrapper: createWrapper() }
      );

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.data?.data).toEqual(mockUsers);
      expect(result.current.data?.totalCount).toBe(2);
      expect(result.current.data?.rolesMap).toEqual({
        1: ['Admin'],
        2: ['Basic'],
      });

      expect(usersApi.getPagedList).toHaveBeenCalledTimes(1);
      expect(usersApi.getUserRoles).toHaveBeenCalledTimes(2);
    });

    it('handles user roles fetch failures gracefully without crashing the list query', async () => {
      const mockUsers = [
        { id: 1, fullName: 'John Doe', email: 'john@example.com', isActive: true, emailConfirmed: true, phoneNumber: '123', isLocked: false, userName: 'john@example.com' },
      ];

      vi.mocked(usersApi.getPagedList).mockResolvedValue({
        isSuccessful: true,
        messages: [],
        statusCode: 200,
        data: {
          data: mockUsers,
          totalCount: 1,
          currentPage: 1,
          pageSize: 10,
        },
      });

      vi.mocked(usersApi.getUserRoles).mockRejectedValue(new Error('Network error'));

      const { result } = renderHook(
        () => useUserList({ pageNumber: 1, pageSize: 10, searchTerm: '' }),
        { wrapper: createWrapper() }
      );

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.data?.data).toEqual(mockUsers);
      expect(result.current.data?.rolesMap).toEqual({
        1: [],
      });
      expect(usersApi.getUserRoles).toHaveBeenCalledTimes(1);
    });
  });

  describe('useAvailableRoles', () => {
    it('successfully retrieves available roles list', async () => {
      const mockRoles = [
        { id: 1, name: 'Admin', description: 'Admin role' },
        { id: 2, name: 'Basic', description: 'Basic role' },
      ];

      vi.mocked(usersApi.getRolesAll).mockResolvedValue({
        isSuccessful: true,
        messages: [],
        statusCode: 200,
        data: mockRoles,
      });

      const { result } = renderHook(() => useAvailableRoles(), { wrapper: createWrapper() });

      await waitFor(() => expect(result.current.isSuccess).toBe(true));
      expect(result.current.data).toEqual(mockRoles);
    });
  });
});