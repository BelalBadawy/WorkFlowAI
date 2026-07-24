import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { categoriesApi } from '../lib/categories-api';
import type { 
  PagedFilterRequest, 
  CreateCategoryRequest, 
  UpdateCategoryRequest,
  CategoryResponse,
  PagedResult
} from '../lib/categories-api';
import { useToast } from '../components/ui/toast';

export function useCategoryList(params: PagedFilterRequest) {
  return useQuery({
    queryKey: [
      'categories', 
      'list', 
      params.pageNumber, 
      params.pageSize, 
      params.searchTerm, 
      params.sortBy, 
      params.sortDirection, 
      params.isActive
    ],
    queryFn: async () => {
      const response = await categoriesApi.getPagedList(params);
      if (!response.isSuccessful) {
        throw new Error(response.messages[0] || 'Failed to retrieve categories.');
      }
      return response.data;
    },
    placeholderData: (previousData) => previousData,
  });
}

export function useCategoryLookups() {
  return useQuery({
    queryKey: ['categories', 'lookups'],
    queryFn: async () => {
      const response = await categoriesApi.getForList();
      if (!response.isSuccessful) {
        throw new Error(response.messages[0] || 'Failed to load parent category lookups.');
      }
      return response.data || [];
    },
  });
}

export function useCreateCategory() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: CreateCategoryRequest) => categoriesApi.create(data),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Category created successfully!');
        queryClient.invalidateQueries({ queryKey: ['categories'] });
      } else {
        toast.error(response.messages[0] || 'Failed to create category.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during save.');
    },
  });
}

export function useUpdateCategory() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: UpdateCategoryRequest) => categoriesApi.update(data),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Category updated successfully!');
        queryClient.invalidateQueries({ queryKey: ['categories'] });
      } else {
        toast.error(response.messages[0] || 'Failed to update category.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during save.');
    },
  });
}

export function useDeleteCategory() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: number) => categoriesApi.delete(id),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Category deleted successfully.');
        queryClient.invalidateQueries({ queryKey: ['categories'] });
      } else {
        toast.error(response.messages[0] || 'Failed to delete category.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during deletion.');
    },
  });
}

export function useRestoreCategory() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: number) => categoriesApi.restore(id),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Category restored successfully!');
        queryClient.invalidateQueries({ queryKey: ['categories'] });
      } else {
        toast.error(response.messages[0] || 'Failed to restore category.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during restoration.');
    },
  });
}

export function useChangeCategoryStatus() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: { id: number; isActive: boolean }) =>
      categoriesApi.changeStatus(data.id, data.isActive),
    onMutate: async ({ id, isActive }) => {
      await queryClient.cancelQueries({ queryKey: ['categories'] });

      const previousQueries = queryClient.getQueriesData<PagedResult<CategoryResponse>>({
        queryKey: ['categories', 'list']
      });

      previousQueries.forEach(([queryKey]) => {
        queryClient.setQueryData<PagedResult<CategoryResponse>>(queryKey, (old) => {
          if (!old) return old;
          return {
            ...old,
            data: old.data.map((cat) =>
              cat.id === id ? { ...cat, isActive } : cat
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
      toast.error(err.message || 'Failed to update category status.');
    },
    onSuccess: (response, variables) => {
      if (response.isSuccessful) {
        toast.success(
          `Category ${variables.isActive ? 'activated' : 'deactivated'} successfully.`
        );
      } else {
        toast.error(response.messages[0] || 'Failed to update category status.');
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
    },
  });
}