import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { phasesApi } from '../lib/phases-api';
import type { 
  PagedFilterRequest, 
  CreatePhaseRequest, 
  UpdatePhaseRequest,
  PhaseResponse,
  PagedResult
} from '../lib/phases-api';
import { useToast } from '../components/ui/toast';

export function usePhaseList(params: PagedFilterRequest) {
  return useQuery({
    queryKey: [
      'phases', 
      'list', 
      params.pageNumber, 
      params.pageSize, 
      params.searchTerm, 
      params.sortBy, 
      params.sortDirection, 
      params.isActive
    ],
    queryFn: async () => {
      const response = await phasesApi.getPagedList(params);
      if (!response.isSuccessful) {
        throw new Error(response.messages[0] || 'Failed to retrieve phases.');
      }
      return response.data;
    },
    placeholderData: (previousData) => previousData,
  });
}

export function useCreatePhase() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: CreatePhaseRequest) => phasesApi.create(data),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Phase created successfully!');
        queryClient.invalidateQueries({ queryKey: ['phases'] });
      } else {
        toast.error(response.messages[0] || 'Failed to create phase.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during save.');
    },
  });
}

export function useUpdatePhase() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: UpdatePhaseRequest) => phasesApi.update(data),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Phase updated successfully!');
        queryClient.invalidateQueries({ queryKey: ['phases'] });
      } else {
        toast.error(response.messages[0] || 'Failed to update phase.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during save.');
    },
  });
}

export function useDeletePhase() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: number) => phasesApi.delete(id),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Phase deleted successfully.');
        queryClient.invalidateQueries({ queryKey: ['phases'] });
      } else {
        toast.error(response.messages[0] || 'Failed to delete phase.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during deletion.');
    },
  });
}

export function useRestorePhase() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: number) => phasesApi.restore(id),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Phase restored successfully!');
        queryClient.invalidateQueries({ queryKey: ['phases'] });
      } else {
        toast.error(response.messages[0] || 'Failed to restore phase.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during restoration.');
    },
  });
}

export function useChangePhaseStatus() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: { id: number; isActive: boolean }) =>
      phasesApi.changeStatus(data.id, data.isActive),
    onMutate: async ({ id, isActive }) => {
      await queryClient.cancelQueries({ queryKey: ['phases'] });

      const previousQueries = queryClient.getQueriesData<PagedResult<PhaseResponse>>({
        queryKey: ['phases', 'list']
      });

      previousQueries.forEach(([queryKey]) => {
        queryClient.setQueryData<PagedResult<PhaseResponse>>(queryKey, (old) => {
          if (!old) return old;
          return {
            ...old,
            data: old.data.map((item) =>
              item.id === id ? { ...item, isActive } : item
            )
          };
        });
      });

      return { previousQueries };
    },
    onError: (err: Error, _variables, context) => {
      if (context?.previousQueries) {
        context.previousQueries.forEach(([queryKey, queryData]) => {
          queryClient.setQueryData(queryKey, queryData);
        });
      }
      toast.error(err.message || 'Failed to update phase status.');
    },
    onSuccess: (response, variables) => {
      if (response.isSuccessful) {
        toast.success(
          `Phase ${variables.isActive ? 'activated' : 'deactivated'} successfully.`
        );
      } else {
        toast.error(response.messages[0] || 'Failed to update phase status.');
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['phases'] });
    },
  });
}
