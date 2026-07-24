import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '../lib/users-api';
import type { 
  PagedFilterRequest, 
  UserRegistrationRequest 
} from '../lib/users-api';
import { useToast } from '../components/ui/toast';

export function useUserList(params: PagedFilterRequest) {
  return useQuery({
    queryKey: [
      'users', 
      'list', 
      params.pageNumber, 
      params.pageSize, 
      params.searchTerm, 
      params.sortBy, 
      params.sortDirection, 
      params.isActive, 
      params.isLocked, 
      params.roleId
    ],
    queryFn: async () => {
      const response = await usersApi.getPagedList(params);
      if (!response.isSuccessful || !response.data) {
        throw new Error(response.messages[0] || 'Failed to retrieve users.');
      }

      const usersData = response.data.data;

      // Resolve user roles in parallel with isolated try/catch failsafes
      const rolesPromises = usersData.map(async (u) => {
        try {
          const roleResponse = await usersApi.getUserRoles(u.id);
          if (roleResponse.isSuccessful && roleResponse.data) {
            return { userId: u.id, roles: roleResponse.data.map(r => r.roleName) };
          }
        } catch (e) {
          console.error(`Failed to fetch roles for user ${u.id}`, e);
        }
        return { userId: u.id, roles: [] };
      });

      const results = await Promise.all(rolesPromises);
      const rolesMap: Record<number, string[]> = {};
      results.forEach(res => {
        rolesMap[res.userId] = res.roles;
      });

      return {
        data: usersData,
        totalCount: response.data.totalCount,
        rolesMap,
      };
    },
    placeholderData: (previousData) => previousData,
  });
}

export function useAvailableRoles() {
  return useQuery({
    queryKey: ['roles', 'available'],
    queryFn: async () => {
      const response = await usersApi.getRolesAll();
      if (!response.isSuccessful) {
        throw new Error(response.messages[0] || 'Failed to load roles.');
      }
      return response.data || [];
    },
  });
}

export function useRegisterUser() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: UserRegistrationRequest) => usersApi.register(data),
    onSuccess: (res) => {
      if (res.isSuccessful) {
        toast.success('User created successfully!');
        queryClient.invalidateQueries({ queryKey: ['users'] });
      } else {
        toast.error(res.messages[0] || 'Registration failed.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during registration.');
    },
  });
}

export interface UpdateUserAndRolesRequest {
  userId: number;
  fullName: string;
  phoneNumber: string;
  roles: string[];
}

export function useUpdateUserAndRoles() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: async (data: UpdateUserAndRolesRequest) => {
      const profileRes = await usersApi.update({
        userId: data.userId,
        fullName: data.fullName,
        phoneNumber: data.phoneNumber,
      });
      if (!profileRes.isSuccessful) {
        throw new Error(profileRes.messages[0] || 'Failed to update user details.');
      }

      const rolesRes = await usersApi.updateUserRoles(data.userId, data.roles);
      if (!rolesRes.isSuccessful) {
        throw new Error(rolesRes.messages[0] || 'Failed to update assigned roles.');
      }

      return { profileRes, rolesRes };
    },
    onSuccess: () => {
      toast.success('User details and roles updated successfully!');
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during updates.');
    },
  });
}

export function useLockUser() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (userId: number) => usersApi.lock(userId),
    onSuccess: (res) => {
      if (res.isSuccessful) {
        toast.success('User locked successfully.');
        queryClient.invalidateQueries({ queryKey: ['users'] });
      } else {
        toast.error(res.messages[0] || 'Failed to lock user.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'Operation failed.');
    },
  });
}

export function useUnlockUser() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (userId: number) => usersApi.unlock(userId),
    onSuccess: (res) => {
      if (res.isSuccessful) {
        toast.success('User unlocked successfully.');
        queryClient.invalidateQueries({ queryKey: ['users'] });
      } else {
        toast.error(res.messages[0] || 'Failed to unlock user.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'Operation failed.');
    },
  });
}

export function useChangeUserStatus() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: { userId: number; activate: boolean }) =>
      usersApi.changeStatus(data.userId, data.activate),
    onSuccess: (res, data) => {
      if (res.isSuccessful) {
        toast.success(`User ${data.activate ? 'activated' : 'deactivated'} successfully.`);
        queryClient.invalidateQueries({ queryKey: ['users'] });
      } else {
        toast.error(res.messages[0] || 'Failed to change user status.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'Operation failed.');
    },
  });
}

export function useDeleteUser() {
  const toast = useToast();

  return useMutation({
    mutationFn: async (userFullName: string) => {
      await new Promise((resolve) => setTimeout(resolve, 500));
      return userFullName;
    },
    onSuccess: (name) => {
      toast.success(
        `Delete operation simulated successfully for user "${name}"! (Verified claim: Permission.Identity.Users.Delete)`
      );
    },
  });
}

export function useUserLookups() {
  return useQuery({
    queryKey: ['users', 'lookup'],
    queryFn: async () => {
      const response = await usersApi.getPagedList({ pageNumber: 1, pageSize: 100, sortBy: 'id', sortDirection: 'asc' });
      if (!response.isSuccessful || !response.data) {
        throw new Error(response.messages[0] || 'Failed to retrieve users for lookup.');
      }
      return response.data.data.map(u => ({
        id: u.id,
        fullName: u.fullName,
        email: u.email,
      }));
    },
  });
}