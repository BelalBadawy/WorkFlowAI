import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../components/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Badge } from '../components/ui/badge';
import { Sheet, SheetHeader, SheetTitle, SheetDescription, SheetFooter } from '../components/ui/sheet';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from '../components/ui/dialog';
import { categoriesApi } from '../lib/categories-api';
import type { CategoryResponse } from '../lib/categories-api';
import { 
  Plus, Edit2, Trash2, ShieldCheck, AlertTriangle, 
  Search, RotateCcw, Loader2
} from 'lucide-react';
import { 
  useReactTable, 
  getCoreRowModel, 
} from '@tanstack/react-table';
import type { ColumnDef } from '@tanstack/react-table';
import { 
  useCategoryList, 
  useCategoryLookups, 
  useCreateCategory, 
  useUpdateCategory, 
  useDeleteCategory,
  useChangeCategoryStatus,
  useRestoreCategory
} from '../hooks/useCategories';
import { DataTablePagination } from '../components/ui/DataTablePagination';
import { useToast } from '../components/ui/toast';
import DataTableExport from '../components/ui/DataTableExport';
import { StatusSwitch } from '../components/shared/StatusSwitch';
import { StatusConfirmationDialog } from '../components/shared/StatusConfirmationDialog';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '../components/ui/tooltip';

const columns: ColumnDef<CategoryResponse>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Category Name' },
  { accessorKey: 'slug', header: 'Slug' },
  { accessorKey: 'parentId', header: 'Parent Category' },
  { accessorKey: 'sortOrder', header: 'Sort Order' },
  { accessorKey: 'isActive', header: 'Status' },
];

export default function CategoriesManagement() {
  const { hasPermission } = useAuth();

  const [searchParams, setSearchParams] = useSearchParams();

  // Query parameters from URL
  const pageNumber = Number(searchParams.get('page') || '1');
  const pageSize = Number(searchParams.get('size') || '10');
  const searchTerm = searchParams.get('search') || '';
  const sortBy = searchParams.get('sortBy') || 'sortorder';
  const sortDirection = (searchParams.get('sortDir') || 'asc') as 'asc' | 'desc';
  const statusFilter = (searchParams.get('status') || 'all') as 'all' | 'active' | 'inactive';
  const includeDeletedParam = searchParams.get('includeDeleted') === 'true';

  // Local filter states
  const [localSearch, setLocalSearch] = useState(searchTerm);
  const [localStatus, setLocalStatus] = useState(statusFilter);
  const [localIncludeDeleted, setLocalIncludeDeleted] = useState(includeDeletedParam);

  // Synchronize local states with URL search params (supporting Back/Forward navigation & hydration)
  const [prevParams, setPrevParams] = useState({
    search: searchTerm,
    status: statusFilter,
    includeDeleted: includeDeletedParam,
  });

  if (
    searchTerm !== prevParams.search ||
    statusFilter !== prevParams.status ||
    includeDeletedParam !== prevParams.includeDeleted
  ) {
    setPrevParams({
      search: searchTerm,
      status: statusFilter,
      includeDeleted: includeDeletedParam,
    });
    setLocalSearch(searchTerm);
    setLocalStatus(statusFilter);
    setLocalIncludeDeleted(includeDeletedParam);
  }

  const isDirty =
    localSearch.trim() !== searchTerm ||
    localStatus !== statusFilter ||
    localIncludeDeleted !== includeDeletedParam;

  // Dialog & Sheet States
  const [isFormSheetOpen, setIsFormSheetOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [isStatusDialogOpen, setIsStatusDialogOpen] = useState(false);
  const [statusAction, setStatusAction] = useState<'activate' | 'deactivate' | null>(null);
  const [targetCategory, setTargetCategory] = useState<CategoryResponse | null>(null);

  // Form States
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create');
  const [formName, setFormName] = useState('');
  const [formSlug, setFormSlug] = useState('');
  const [formParentId, setFormParentId] = useState<number | null>(null);
  const [formSortOrder, setFormSortOrder] = useState<number>(0);
  const [formIsActive, setFormIsActive] = useState(true);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isExporting, setIsExporting] = useState(false);
  const toast = useToast();

  // API Hooks
  const isActiveParam = statusFilter === 'active' ? true : statusFilter === 'inactive' ? false : null;
  const { data: pagedData, isLoading: loading } = useCategoryList({
    pageNumber,
    pageSize,
    searchTerm,
    sortBy,
    sortDirection,
    isActive: isActiveParam,
    includeDeleted: includeDeletedParam,
  });

  const { data: parentLookups = [] } = useCategoryLookups();

  const createMutation = useCreateCategory();
  const updateMutation = useUpdateCategory();
  const deleteMutation = useDeleteCategory();
  const changeStatusMutation = useChangeCategoryStatus();
  const restoreMutation = useRestoreCategory();

  const categories = pagedData?.data || [];
  const totalCount = pagedData?.totalCount || 0;

  const formSubmitting = createMutation.isPending || updateMutation.isPending;

  // React Table Instance
  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: categories,
    columns,
    pageCount: Math.ceil(totalCount / pageSize),
    state: {
      pagination: {
        pageIndex: pageNumber - 1,
        pageSize,
      },
      sorting: [{ id: sortBy, desc: sortDirection === 'desc' }],
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function' 
        ? updater({ pageIndex: pageNumber - 1, pageSize }) 
        : updater;
      setSearchParams(prev => {
        prev.set('page', String(next.pageIndex + 1));
        prev.set('size', String(next.pageSize));
        return prev;
      });
    },
    onSortingChange: (updater) => {
      const current = [{ id: sortBy, desc: sortDirection === 'desc' }];
      const next = typeof updater === 'function' ? updater(current) : updater;
      if (next && next.length > 0) {
        setSearchParams(prev => {
          prev.set('sortBy', next[0].id);
          prev.set('sortDir', next[0].desc ? 'desc' : 'asc');
          prev.set('page', '1');
          return prev;
        });
      }
    },
    manualPagination: true,
    manualSorting: true,
    getCoreRowModel: getCoreRowModel(),
  });

  // Helper to dynamically slugify text
  const generateSlug = (val: string) => {
    return val
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9\s-]/g, '') // remove invalid chars
      .replace(/[\s-]+/g, '-')     // replace spaces/hyphens with a single hyphen
      .replace(/^-+|-+$/g, '');    // trim hyphens
  };

  // Handle name input change to auto-suggest slug
  const handleNameChange = (val: string) => {
    setFormName(val);
    if (formMode === 'create') {
      setFormSlug(generateSlug(val));
    }
  };

  const handleApplyFilters = (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setSearchParams(prev => {
      const next = new URLSearchParams(prev);
      if (localSearch.trim()) {
        next.set('search', localSearch.trim());
      } else {
        next.delete('search');
      }

      if (localStatus && localStatus !== 'all') {
        next.set('status', localStatus);
      } else {
        next.delete('status');
      }

      if (localIncludeDeleted) {
        next.set('includeDeleted', 'true');
      } else {
        next.delete('includeDeleted');
      }

      next.set('page', '1');
      return next;
    });
  };

  const handleResetFilters = () => {
    setLocalSearch('');
    setLocalStatus('all');
    setLocalIncludeDeleted(false);
    setSearchParams(new URLSearchParams());
  };

  const toggleSort = (field: string) => {
    setSearchParams(prev => {
      const currentSortBy = prev.get('sortBy') || 'sortorder';
      const currentSortDir = prev.get('sortDir') || 'asc';
      if (currentSortBy === field) {
        prev.set('sortDir', currentSortDir === 'asc' ? 'desc' : 'asc');
      } else {
        prev.set('sortBy', field);
        prev.set('sortDir', 'asc');
      }
      prev.set('page', '1');
      return prev;
    });
  };

  // Form Validation
  const validateForm = () => {
    const nextErrors: Record<string, string> = {};

    if (!formName.trim()) {
      nextErrors.name = 'Category Name is required';
    } else if (formName.trim().length < 2) {
      nextErrors.name = 'Category Name must be at least 2 characters';
    }

    if (!formSlug.trim()) {
      nextErrors.slug = 'Slug is required';
    } else if (!/^[a-z0-9-]+$/.test(formSlug)) {
      nextErrors.slug = 'Slug must only contain lowercase alphanumeric characters and hyphens';
    }

    if (formSortOrder < 0) {
      nextErrors.sortOrder = 'Sort order must be non-negative';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const openCreateSheet = () => {
    setFormMode('create');
    setFormName('');
    setFormSlug('');
    setFormParentId(null);
    setFormSortOrder(0);
    setFormIsActive(true);
    setErrors({});
    setIsFormSheetOpen(true);
  };

  const openEditSheet = (cat: CategoryResponse) => {
    setFormMode('edit');
    setTargetCategory(cat);
    setFormName(cat.name);
    setFormSlug(cat.slug);
    setFormParentId(cat.parentId);
    setFormSortOrder(cat.sortOrder);
    setFormIsActive(cat.isActive);
    setErrors({});
    setIsFormSheetOpen(true);
  };

  const handleSubmitCategory = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    if (formMode === 'create') {
      createMutation.mutate({
        name: formName,
        slug: formSlug,
        parentId: formParentId,
        isActive: formIsActive,
        sortOrder: formSortOrder,
      }, {
        onSuccess: (response) => {
          if (response.isSuccessful) {
            setIsFormSheetOpen(false);
          }
        }
      });
    } else {
      if (!targetCategory) return;
      updateMutation.mutate({
        id: targetCategory.id,
        name: formName,
        slug: formSlug,
        parentId: formParentId,
        isActive: formIsActive,
        sortOrder: formSortOrder,
        rowVersion: targetCategory.rowVersion,
      }, {
        onSuccess: (response) => {
          if (response.isSuccessful) {
            setIsFormSheetOpen(false);
          }
        }
      });
    }
  };

  const requestDelete = (cat: CategoryResponse) => {
    setTargetCategory(cat);
    setIsDeleteDialogOpen(true);
  };

  const executeDeleteCategory = async () => {
    if (!targetCategory) return;
    deleteMutation.mutate(targetCategory.id, {
      onSuccess: (response) => {
        if (response.isSuccessful) {
          setIsDeleteDialogOpen(false);
          setTargetCategory(null);
        }
      }
    });
  };

  const requestChangeStatus = (action: 'activate' | 'deactivate', cat: CategoryResponse) => {
    setStatusAction(action);
    setTargetCategory(cat);
    setIsStatusDialogOpen(true);
  };

  const executeChangeStatus = async () => {
    if (!targetCategory || !statusAction) return;
    changeStatusMutation.mutate({
      id: targetCategory.id,
      isActive: statusAction === 'activate'
    }, {
      onSuccess: (response) => {
        if (response.isSuccessful) {
          setIsStatusDialogOpen(false);
        }
      }
    });
  };

  // Find parent category name from lists
  const getParentCategoryName = (parentId: number | null) => {
    if (parentId === null) return 'None (Root)';
    const lookup = parentLookups.find(p => p.id === parentId);
    return lookup ? lookup.name : `Category #${parentId}`;
  };

  return (
    <TooltipProvider>
      <div className="space-y-6">
      
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight text-neutral-900">Product Categories</h1>
          <p className="text-sm text-neutral-500 mt-1">Configure hierarchical classification catalogs for products.</p>
        </div>

        {hasPermission('Permission.Product.Categories.Create') ? (
          <Button onClick={openCreateSheet} className="bg-[#4285F4] hover:bg-[#3273DC] text-white font-bold py-2.5 px-5 rounded-xl shadow-sm flex items-center gap-2 transition-all">
            <Plus className="w-4 h-4" />
            Create Category
          </Button>
        ) : (
          <div className="text-xs text-amber-600 bg-amber-50 border border-amber-100 p-2.5 rounded-xl font-medium flex items-center gap-1.5 max-w-xs leading-tight">
            <AlertTriangle className="w-4 h-4 shrink-0 text-amber-500" />
            <span>Creation disabled due to insufficient permissions.</span>
          </div>
        )}
      </div>

      {/* Search & Filters */}
      <Card className="bg-white border-neutral-200 shadow-sm rounded-xl">
        <CardContent className="p-4">
          <form onSubmit={handleApplyFilters} className="flex flex-col md:flex-row gap-3">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-neutral-400" />
              <input
                type="text"
                placeholder="Search categories by name or slug..."
                value={localSearch}
                onChange={(e) => setLocalSearch(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-neutral-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/30 focus:border-[#4285F4] bg-neutral-50/50"
              />
            </div>
            
            <div className="flex gap-2 shrink-0">
              <select
                value={localStatus}
                onChange={(e) => setLocalStatus(e.target.value as 'all' | 'active' | 'inactive')}
                className="border border-neutral-200 rounded-xl px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-[#4285F4]/30 font-medium text-neutral-700"
              >
                <option value="all">All Statuses</option>
                <option value="active">Active Only</option>
                <option value="inactive">Inactive Only</option>
              </select>

              <label className="flex items-center gap-2 border border-neutral-200 rounded-xl px-3 py-2 text-sm bg-white cursor-pointer select-none font-medium text-neutral-700">
                <input
                  type="checkbox"
                  checked={localIncludeDeleted}
                  onChange={(e) => setLocalIncludeDeleted(e.target.checked)}
                  className="w-4 h-4 text-[#4285F4] border-neutral-300 rounded focus:ring-[#4285F4]"
                />
                Include Deleted
              </label>

              <Button type="submit" variant="default" disabled={!isDirty} className="rounded-xl px-5 disabled:opacity-50 disabled:pointer-events-none">
                Apply Filters
              </Button>
              <Button type="button" variant="outline" onClick={handleResetFilters} className="rounded-xl px-4 flex items-center gap-1">
                <RotateCcw className="w-3.5 h-3.5" />
                Reset Filters
              </Button>
              <DataTableExport
                isExporting={isExporting}
                onExport={async (format) => {
                  try {
                    setIsExporting(true);
                    await categoriesApi.exportCategories({
                      searchTerm: searchTerm || undefined,
                      isActive: isActiveParam,
                      sortBy: sortBy || undefined,
                      sortDirection: sortDirection || undefined,
                      exportFormat: format
                    });
                  } catch (err) {
                    toast.error(err instanceof Error ? err.message : String(err));
                  } finally {
                    setIsExporting(false);
                  }
                }}
              />
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Main Table Card */}
      <Card className="bg-white border border-neutral-200 shadow-xl rounded-2xl overflow-hidden">
        <CardHeader className="border-b border-neutral-100 pb-4 bg-neutral-50/30">
          <div className="flex justify-between items-center">
            <div>
              <CardTitle className="text-lg font-bold text-neutral-900">Categories Directory</CardTitle>
              <CardDescription>A paginated overview of inventory and classification nodes.</CardDescription>
            </div>
            <div className="text-xs font-semibold text-neutral-400">
              Total Records: {totalCount}
            </div>
          </div>
        </CardHeader>

        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-neutral-200 bg-neutral-50/50 text-neutral-500 text-xs font-bold uppercase tracking-wider">
                  <th onClick={() => toggleSort('id')} className="px-6 py-4 cursor-pointer hover:bg-neutral-100 select-none transition-colors">
                    ID {sortBy === 'id' && (sortDirection === 'asc' ? 'â–²' : 'â–¼')}
                  </th>
                  <th onClick={() => toggleSort('name')} className="px-6 py-4 cursor-pointer hover:bg-neutral-100 select-none transition-colors">
                    Category Name {sortBy === 'name' && (sortDirection === 'asc' ? 'â–²' : 'â–¼')}
                  </th>
                  <th onClick={() => toggleSort('slug')} className="px-6 py-4 cursor-pointer hover:bg-neutral-100 select-none transition-colors">
                    Slug {sortBy === 'slug' && (sortDirection === 'asc' ? 'â–²' : 'â–¼')}
                  </th>
                  <th className="px-6 py-4">Parent Category</th>
                  <th onClick={() => toggleSort('sortorder')} className="px-6 py-4 cursor-pointer hover:bg-neutral-100 select-none transition-colors">
                    Sort Order {sortBy === 'sortorder' && (sortDirection === 'asc' ? 'â–²' : 'â–¼')}
                  </th>
                  <th className="px-6 py-4">Status</th>
                  <th className="px-6 py-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-100 text-sm text-neutral-800">
                {loading ? (
                  <tr>
                    <td colSpan={7} className="text-center py-12 text-neutral-400">
                      <div className="flex items-center justify-center gap-2">
                        <Loader2 className="w-5 h-5 animate-spin text-[#4285F4]" />
                        <span>Loading category database...</span>
                      </div>
                    </td>
                  </tr>
                ) : categories.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="text-center py-12 text-neutral-400">
                      No matching classification categories found.
                    </td>
                  </tr>
                ) : (
                  categories.map((cat) => (
                    <tr key={cat.id} className="hover:bg-neutral-50/50 transition-colors">
                      <td className="px-6 py-4 font-bold text-neutral-400">
                        #{cat.id}
                      </td>
                      <td className="px-6 py-4">
                        <span className="font-extrabold text-neutral-900">{cat.name}</span>
                      </td>
                      <td className="px-6 py-4 font-mono text-xs text-neutral-500">
                        {cat.slug}
                      </td>
                      <td className="px-6 py-4 text-neutral-600 font-medium">
                        {getParentCategoryName(cat.parentId)}
                      </td>
                      <td className="px-6 py-4 font-semibold text-neutral-500">
                        {cat.sortOrder}
                      </td>
                      <td className="px-6 py-4">
                        {cat.softDeleted ? (
                          <Badge variant="outline" className="border-rose-200 bg-rose-50 text-rose-700 font-bold">
                            Deleted
                          </Badge>
                        ) : hasPermission('Permission.Product.Categories.Update') ? (
                          <StatusSwitch
                            isActive={cat.isActive}
                            onToggle={() => requestChangeStatus(cat.isActive ? 'deactivate' : 'activate', cat)}
                            entityName={cat.name}
                            isLoading={changeStatusMutation.isPending && targetCategory?.id === cat.id}
                          />
                        ) : (
                          <Badge 
                             variant="outline" 
                             className={
                               cat.isActive 
                                 ? 'border-emerald-200 bg-emerald-50 text-emerald-700 font-bold' 
                                 : 'border-neutral-200 bg-neutral-50 text-neutral-600 font-bold'
                             }
                          >
                            {cat.isActive ? 'Active' : 'Inactive'}
                          </Badge>
                        )}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          {cat.softDeleted ? (
                            hasPermission('Permission.Product.Categories.Update') && (
                              <Tooltip delayDuration={300}>
                                <TooltipTrigger asChild>
                                  <Button 
                                    variant="ghost" 
                                    size="icon" 
                                    onClick={() => restoreMutation.mutate(cat.id)}
                                    disabled={restoreMutation.isPending}
                                    className="h-8 w-8 text-emerald-600 hover:text-emerald-900 hover:bg-emerald-50 rounded-lg"
                                  >
                                    {restoreMutation.isPending && targetCategory?.id === cat.id ? (
                                      <Loader2 className="w-3.5 h-3.5 animate-spin" />
                                    ) : (
                                      <RotateCcw className="w-3.5 h-3.5" />
                                    )}
                                  </Button>
                                </TooltipTrigger>
                                <TooltipContent>Restore Category</TooltipContent>
                              </Tooltip>
                            )
                          ) : (
                            <>
                              {hasPermission('Permission.Product.Categories.Update') && (
                                <Tooltip delayDuration={300}>
                                  <TooltipTrigger asChild>
                                    <Button 
                                      variant="ghost" 
                                      size="icon" 
                                      onClick={() => openEditSheet(cat)}
                                      className="h-8 w-8 text-neutral-500 hover:text-neutral-900 hover:bg-neutral-100 rounded-lg"
                                    >
                                      <Edit2 className="w-3.5 h-3.5" />
                                    </Button>
                                  </TooltipTrigger>
                                  <TooltipContent>Edit Details</TooltipContent>
                                </Tooltip>
                              )}

                              {hasPermission('Permission.Product.Categories.Delete') && (
                                <Tooltip delayDuration={300}>
                                  <TooltipTrigger asChild>
                                    <Button 
                                      variant="ghost" 
                                      size="icon" 
                                      onClick={() => requestDelete(cat)}
                                      className="h-8 w-8 text-rose-500 hover:text-rose-900 hover:bg-rose-50 rounded-lg"
                                    >
                                      <Trash2 className="w-3.5 h-3.5" />
                                    </Button>
                                  </TooltipTrigger>
                                  <TooltipContent>Delete Category</TooltipContent>
                                </Tooltip>
                              )}
                            </>
                          )}

                          {!hasPermission('Permission.Product.Categories.Update') && 
                           !hasPermission('Permission.Product.Categories.Delete') && (
                            <span className="text-xs text-neutral-400 font-medium italic">Read-Only</span>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {!loading && totalCount > 0 && (
            <div className="px-6 py-4 border-t border-neutral-100 bg-neutral-50/50">
              <DataTablePagination table={table} />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Security Info Notice */}
      <div className="p-4 bg-[#4285F4]/5 border border-[#4285F4]/10 rounded-2xl flex gap-3 text-[#4285F4] text-xs max-w-2xl leading-relaxed">
        <ShieldCheck className="w-5 h-5 shrink-0 mt-0.5" />
        <div className="space-y-1">
          <span className="font-bold block">Dynamic Roles Authorization Guards Active</span>
          Security controls are applied dynamically at page-load and transaction boundaries based on identity permission claims.
        </div>
      </div>

      {/* CREATE / EDIT SHEET PANEL */}
      <Sheet open={isFormSheetOpen} onOpenChange={setIsFormSheetOpen}>
        <SheetHeader>
          <SheetTitle>{formMode === 'create' ? 'Create New Category' : 'Modify Category Details'}</SheetTitle>
          <SheetDescription>
            {formMode === 'create'
              ? 'Configure name, url slug, display priority, and status details.'
              : 'Edit category names, hierarchical parents, and priority rankings.'}
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmitCategory} className="space-y-4 mt-4">
          
          {/* Category Name */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-neutral-600 block">Category Name</label>
            <input
              type="text"
              value={formName}
              onChange={(e) => handleNameChange(e.target.value)}
              placeholder="e.g. Science Fiction"
              className={`w-full p-2.5 border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20 ${
                errors.name ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200 focus:border-[#4285F4]'
              }`}
            />
            {errors.name && <p className="text-rose-500 text-[11px] font-medium">{errors.name}</p>}
          </div>

          {/* Slug */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-neutral-600 block">URL Slug</label>
            <input
              type="text"
              value={formSlug}
              onChange={(e) => setFormSlug(e.target.value)}
              placeholder="e.g. science-fiction"
              className={`w-full p-2.5 border rounded-xl text-sm font-mono focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20 ${
                errors.slug ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200 focus:border-[#4285F4]'
              }`}
            />
            {errors.slug && <p className="text-rose-500 text-[11px] font-medium">{errors.slug}</p>}
          </div>

          {/* Parent Category Selector */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-neutral-600 block">Parent Category</label>
            <select
              value={formParentId === null ? '' : formParentId}
              onChange={(e) => setFormParentId(e.target.value ? Number(e.target.value) : null)}
              className="w-full p-2.5 border border-neutral-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20"
            >
              <option value="">None (Root Category)</option>
              {parentLookups
                .filter(p => formMode === 'create' || p.id !== targetCategory?.id) // exclude self to prevent cycle
                .map(p => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
            </select>
          </div>

          {/* Sort Order */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-neutral-600 block">Sort Order / Priority</label>
            <input
              type="number"
              min="0"
              value={formSortOrder}
              onChange={(e) => setFormSortOrder(Number(e.target.value))}
              placeholder="0"
              className={`w-full p-2.5 border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20 ${
                errors.sortOrder ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200 focus:border-[#4285F4]'
              }`}
            />
            {errors.sortOrder && <p className="text-rose-500 text-[11px] font-medium">{errors.sortOrder}</p>}
          </div>

          {/* Status Active switch */}
          <div className="flex items-center justify-between p-3 border border-neutral-100 rounded-xl bg-neutral-50/50">
            <div>
              <span className="text-xs font-bold text-neutral-800 block">Publish Status</span>
              <span className="text-[10px] text-neutral-400 font-medium">Toggle active status visibility in client catalogs.</span>
            </div>
            <input
              type="checkbox"
              checked={formIsActive}
              onChange={(e) => setFormIsActive(e.target.checked)}
              className="w-4 h-4 text-[#4285F4] border-neutral-300 rounded focus:ring-[#4285F4]"
            />
          </div>

          <SheetFooter>
            <Button
              type="submit"
              disabled={formSubmitting}
              className="bg-[#4285F4] hover:bg-[#3273DC] text-white font-bold py-2.5 px-6 rounded-xl flex items-center gap-2 justify-center w-full sm:w-auto"
            >
              {formSubmitting && <Loader2 className="w-4 h-4 animate-spin" />}
              {formMode === 'create' ? 'Create Category' : 'Save Changes'}
            </Button>
          </SheetFooter>
        </form>
      </Sheet>

      {/* DELETE CONFIRMATION DIALOG */}
      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <DialogContent className="max-w-md bg-white p-6 rounded-2xl border border-neutral-200 shadow-2xl">
          <DialogHeader>
            <DialogTitle className="text-neutral-900 font-extrabold flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-rose-500" /> Confirm Classification Deletion
            </DialogTitle>
            <DialogDescription className="text-neutral-500 text-sm mt-2">
              Are you sure you want to delete the product category "{targetCategory?.name}"? Any products mapped to this category may lose their categorization hierarchy. This action is irreversible.
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="flex justify-end gap-2 mt-6">
            <DialogClose onClick={() => setIsDeleteDialogOpen(false)} className="border-neutral-200 text-neutral-600 hover:bg-neutral-100 font-bold">
              Cancel
            </DialogClose>
            <Button
              type="button"
              disabled={deleteMutation.isPending}
              onClick={executeDeleteCategory}
              className="bg-rose-600 hover:bg-rose-700 text-white font-bold px-5 py-2 rounded-xl border-transparent"
            >
              {deleteMutation.isPending && <Loader2 className="w-4 h-4 animate-spin mr-1.5 inline" />}
              Delete Permanently
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* STATUS CONFIRMATION DIALOG */}
      <StatusConfirmationDialog
        isOpen={isStatusDialogOpen}
        onClose={() => setIsStatusDialogOpen(false)}
        onConfirm={executeChangeStatus}
        entityName={targetCategory?.name || ''}
        entityType="category"
        action={statusAction || 'activate'}
        isLoading={changeStatusMutation.isPending}
      />
    </div>
    </TooltipProvider>
  );
}