import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../components/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Badge } from '../components/ui/badge';
import { Sheet, SheetHeader, SheetTitle, SheetDescription, SheetFooter } from '../components/ui/sheet';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from '../components/ui/dialog';
import { phasesApi } from '../lib/phases-api';
import type { PhaseResponse } from '../lib/phases-api';
import { 
  Plus, Edit2, Trash2, ShieldCheck, AlertTriangle, 
  Search, RotateCcw, Loader2, Layers
} from 'lucide-react';
import { 
  useReactTable, 
  getCoreRowModel, 
} from '@tanstack/react-table';
import type { ColumnDef } from '@tanstack/react-table';
import { 
  usePhaseList, 
  useCreatePhase, 
  useUpdatePhase, 
  useDeletePhase,
  useChangePhaseStatus,
  useRestorePhase
} from '../hooks/usePhases';
import { DataTablePagination } from '../components/ui/DataTablePagination';
import { useToast } from '../components/ui/toast';
import DataTableExport from '../components/ui/DataTableExport';
import { StatusSwitch } from '../components/shared/StatusSwitch';
import { StatusConfirmationDialog } from '../components/shared/StatusConfirmationDialog';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '../components/ui/tooltip';

const columns: ColumnDef<PhaseResponse>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'title', header: 'Title' },
  { accessorKey: 'description', header: 'Description' },
  { accessorKey: 'sortOrder', header: 'Sort Order' },
  { accessorKey: 'isActive', header: 'Status' },
];

export default function PhasesManagement() {
  const { hasPermission } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();

  const pageNumber = Number(searchParams.get('page') || '1');
  const pageSize = Number(searchParams.get('size') || '10');
  const searchTerm = searchParams.get('search') || '';
  const sortBy = searchParams.get('sortBy') || 'sortorder';
  const sortDirection = (searchParams.get('sortDir') || 'asc') as 'asc' | 'desc';
  const statusFilter = (searchParams.get('status') || 'all') as 'all' | 'active' | 'inactive';
  const includeDeletedParam = searchParams.get('includeDeleted') === 'true';

  const [localSearch, setLocalSearch] = useState(searchTerm);

  const isActiveParam = statusFilter === 'active' ? true : statusFilter === 'inactive' ? false : null;

  const { data: pagedData, isLoading, isError, error, isFetching } = usePhaseList({
    pageNumber,
    pageSize,
    searchTerm,
    sortBy,
    sortDirection,
    isActive: isActiveParam,
    includeDeleted: includeDeletedParam,
  });

  const createPhase = useCreatePhase();
  const updatePhase = useUpdatePhase();
  const deletePhase = useDeletePhase();
  const changePhaseStatus = useChangePhaseStatus();
  const restorePhase = useRestorePhase();
  const toast = useToast();

  const [isSheetOpen, setIsSheetOpen] = useState(false);
  const [editingPhase, setEditingPhase] = useState<PhaseResponse | null>(null);

  const [formTitle, setFormTitle] = useState('');
  const [formDescription, setFormDescription] = useState('');
  const [formIsActive, setFormIsActive] = useState(true);
  const [formSortOrder, setFormSortOrder] = useState<number>(0);

  const [deleteTarget, setDeleteTarget] = useState<PhaseResponse | null>(null);
  const [restoreTarget, setRestoreTarget] = useState<PhaseResponse | null>(null);
  const [statusTarget, setStatusTarget] = useState<{ id: number; title: string; currentStatus: boolean; nextStatus: boolean } | null>(null);

  const canCreate = hasPermission('Permission.Product.Phases.Create');
  const canUpdate = hasPermission('Permission.Product.Phases.Update');
  const canDelete = hasPermission('Permission.Product.Phases.Delete');
  const canExport = hasPermission('Permission.Product.Phases.Read');

  const updateUrlParams = (newParams: Record<string, string | null>) => {
    const current = new URLSearchParams(searchParams);
    Object.entries(newParams).forEach(([key, value]) => {
      if (value === null || value === '') {
        current.delete(key);
      } else {
        current.set(key, value);
      }
    });
    setSearchParams(current);
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateUrlParams({ search: localSearch.trim() || null, page: '1' });
  };

  const handleOpenCreate = () => {
    setEditingPhase(null);
    setFormTitle('');
    setFormDescription('');
    setFormIsActive(true);
    setFormSortOrder(0);
    setIsSheetOpen(true);
  };

  const handleOpenEdit = (item: PhaseResponse) => {
    setEditingPhase(item);
    setFormTitle(item.title);
    setFormDescription(item.description || '');
    setFormIsActive(item.isActive);
    setFormSortOrder(item.sortOrder);
    setIsSheetOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formTitle.trim()) {
      toast.error('Title is required');
      return;
    }

    if (editingPhase) {
      updatePhase.mutate(
        {
          id: editingPhase.id,
          title: formTitle.trim(),
          description: formDescription.trim() || undefined,
          isActive: formIsActive,
          sortOrder: formSortOrder,
          rowVersion: editingPhase.rowVersion,
        },
        {
          onSuccess: (res) => {
            if (res.isSuccessful) {
              setIsSheetOpen(false);
            }
          },
        }
      );
    } else {
      createPhase.mutate(
        {
          title: formTitle.trim(),
          description: formDescription.trim() || undefined,
          isActive: formIsActive,
          sortOrder: formSortOrder,
        },
        {
          onSuccess: (res) => {
            if (res.isSuccessful) {
              setIsSheetOpen(false);
            }
          },
        }
      );
    }
  };

  const handleConfirmStatusChange = () => {
    if (!statusTarget) return;
    changePhaseStatus.mutate(
      { id: statusTarget.id, isActive: statusTarget.nextStatus },
      {
        onSettled: () => setStatusTarget(null),
      }
    );
  };

  const handleConfirmDelete = () => {
    if (!deleteTarget) return;
    deletePhase.mutate(deleteTarget.id, {
      onSettled: () => setDeleteTarget(null),
    });
  };

  const handleConfirmRestore = () => {
    if (!restoreTarget) return;
    restorePhase.mutate(restoreTarget.id, {
      onSettled: () => setRestoreTarget(null),
    });
  };

  const table = useReactTable({
    data: pagedData?.data || [],
    columns,
    pageCount: pagedData ? Math.ceil(pagedData.totalCount / pageSize) : -1,
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
      updateUrlParams({ page: String(next.pageIndex + 1), size: String(next.pageSize) });
    },
    onSortingChange: (updater) => {
      const current = [{ id: sortBy, desc: sortDirection === 'desc' }];
      const next = typeof updater === 'function' ? updater(current) : updater;
      if (next && next.length > 0) {
        updateUrlParams({ sortBy: next[0].id, sortDir: next[0].desc ? 'desc' : 'asc' });
      }
    },
    manualPagination: true,
    manualSorting: true,
    getCoreRowModel: getCoreRowModel(),
  });

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <Layers className="h-8 w-8 text-indigo-600" />
            Phase Management
          </h1>
          <p className="text-slate-500 text-sm mt-1">
            Configure system execution phases, order sequence, and activity states.
          </p>
        </div>

        <div className="flex items-center gap-2">
          {canExport && (
            <DataTableExport
              onExport={(format) =>
                phasesApi.exportPhases({
                  searchTerm,
                  sortBy,
                  sortDirection,
                  isActive: isActiveParam,
                  exportFormat: format,
                })
              }
            />
          )}

          {canCreate && (
            <Button onClick={handleOpenCreate} className="gap-2 bg-indigo-600 hover:bg-indigo-700">
              <Plus className="h-4 w-4" />
              New Phase
            </Button>
          )}
        </div>
      </div>

      <Card className="shadow-sm border-slate-200">
        <CardHeader className="pb-4">
          <CardTitle className="text-lg font-semibold text-slate-800">Filter & Search</CardTitle>
          <CardDescription>Search phases by title or description</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col md:flex-row items-center gap-4">
            <form onSubmit={handleSearchSubmit} className="flex-1 flex gap-2 w-full">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
                <input
                  type="text"
                  placeholder="Search phases..."
                  className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  value={localSearch}
                  onChange={(e) => setLocalSearch(e.target.value)}
                />
              </div>
              <Button type="submit" variant="secondary" size="sm">
                Search
              </Button>
            </form>

            <div className="flex items-center gap-3 w-full md:w-auto">
              <select
                className="py-2 px-3 text-sm border border-slate-200 rounded-md bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={statusFilter}
                onChange={(e) => updateUrlParams({ status: e.target.value, page: '1' })}
              >
                <option value="all">All Statuses</option>
                <option value="active">Active Only</option>
                <option value="inactive">Inactive Only</option>
              </select>

              <label className="flex items-center gap-2 text-xs text-slate-600 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={includeDeletedParam}
                  onChange={(e) => updateUrlParams({ includeDeleted: e.target.checked ? 'true' : null, page: '1' })}
                  className="rounded text-indigo-600 focus:ring-indigo-500"
                />
                Include Deleted
              </label>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="shadow-sm border-slate-200">
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-12 text-center text-slate-500 flex flex-col items-center gap-2">
              <Loader2 className="h-8 w-8 animate-spin text-indigo-600" />
              <p>Loading phases...</p>
            </div>
          ) : isError ? (
            <div className="p-12 text-center text-rose-500 flex flex-col items-center gap-2">
              <AlertTriangle className="h-8 w-8" />
              <p>{error?.message || 'Error loading phases'}</p>
            </div>
          ) : pagedData?.data.length === 0 ? (
            <div className="p-12 text-center text-slate-500">
              <p className="text-lg font-medium text-slate-700">No phases found</p>
              <p className="text-sm text-slate-400 mt-1">Try adjusting your filters or search criteria.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm text-slate-600">
                <thead className="bg-slate-50 text-slate-700 font-semibold border-b border-slate-200">
                  <tr>
                    <th className="py-3 px-4">ID</th>
                    <th className="py-3 px-4">Title</th>
                    <th className="py-3 px-4">Description</th>
                    <th className="py-3 px-4 text-center">Sort Order</th>
                    <th className="py-3 px-4 text-center">Status</th>
                    <th className="py-3 px-4 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {table.getRowModel().rows.map((row) => {
                    const item = row.original;
                    return (
                      <tr key={item.id} className={`hover:bg-slate-50/80 transition-colors ${item.softDeleted ? 'bg-rose-50/30' : ''}`}>
                        <td className="py-3 px-4 font-mono text-xs text-slate-500">{item.id}</td>
                        <td className="py-3 px-4 font-medium text-slate-900">
                          {item.title}
                          {item.softDeleted && (
                            <Badge variant="destructive" className="ml-2 text-[10px]">
                              Deleted
                            </Badge>
                          )}
                        </td>
                        <td className="py-3 px-4 text-slate-500 max-w-xs truncate">{item.description || '—'}</td>
                        <td className="py-3 px-4 text-center font-mono">{item.sortOrder}</td>
                        <td className="py-3 px-4 text-center">
                          <StatusSwitch
                            isActive={item.isActive}
                            onToggle={() =>
                              setStatusTarget({
                                id: item.id,
                                title: item.title,
                                currentStatus: item.isActive,
                                nextStatus: !item.isActive,
                              })
                            }
                            entityName={item.title}
                            disabled={!canUpdate || item.softDeleted}
                          />
                        </td>
                        <td className="py-3 px-4 text-right">
                          <div className="flex items-center justify-end gap-2">
                            {item.softDeleted ? (
                              canUpdate && (
                                <TooltipProvider>
                                  <Tooltip>
                                    <TooltipTrigger asChild>
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        className="h-8 w-8 p-0 text-amber-600 hover:text-amber-700 hover:bg-amber-50"
                                        onClick={() => setRestoreTarget(item)}
                                      >
                                        <RotateCcw className="h-4 w-4" />
                                      </Button>
                                    </TooltipTrigger>
                                    <TooltipContent>Restore Phase</TooltipContent>
                                  </Tooltip>
                                </TooltipProvider>
                              )
                            ) : (
                              <>
                                {canUpdate && (
                                  <Button
                                    variant="outline"
                                    size="sm"
                                    className="h-8 w-8 p-0 text-slate-600 hover:text-slate-900"
                                    onClick={() => handleOpenEdit(item)}
                                  >
                                    <Edit2 className="h-4 w-4" />
                                  </Button>
                                )}
                                {canDelete && (
                                  <Button
                                    variant="outline"
                                    size="sm"
                                    className="h-8 w-8 p-0 text-rose-600 hover:text-rose-700 hover:bg-rose-50"
                                    onClick={() => setDeleteTarget(item)}
                                  >
                                    <Trash2 className="h-4 w-4" />
                                  </Button>
                                )}
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

          {!isLoading && (pagedData?.totalCount ?? 0) > 0 && (
            <div className="px-6 py-4 border-t border-slate-100 bg-slate-50/50">
              <DataTablePagination table={table} />
            </div>
          )}

      {/* Create / Edit Sheet Modal */}
      <Sheet open={isSheetOpen} onOpenChange={setIsSheetOpen}>
        <div className="p-6 space-y-6">
          <SheetHeader>
            <SheetTitle>{editingPhase ? 'Edit Phase' : 'Create New Phase'}</SheetTitle>
            <SheetDescription>
              {editingPhase
                ? 'Update phase parameters and status.'
                : 'Define a new process phase and ordering sequence.'}
            </SheetDescription>
          </SheetHeader>

          <form onSubmit={handleSave} className="space-y-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-slate-700">Title *</label>
              <input
                type="text"
                required
                className="w-full px-3 py-2 text-sm border border-slate-200 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={formTitle}
                onChange={(e) => setFormTitle(e.target.value)}
                placeholder="Phase title e.g. Pre-boarding"
              />
            </div>

            <div className="space-y-1">
              <label className="text-sm font-medium text-slate-700">Description</label>
              <textarea
                rows={3}
                className="w-full px-3 py-2 text-sm border border-slate-200 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={formDescription}
                onChange={(e) => setFormDescription(e.target.value)}
                placeholder="Optional description of this phase..."
              />
            </div>

            <div className="space-y-1">
              <label className="text-sm font-medium text-slate-700">Sort Order</label>
              <input
                type="number"
                min={0}
                className="w-full px-3 py-2 text-sm border border-slate-200 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={formSortOrder}
                onChange={(e) => setFormSortOrder(Number(e.target.value))}
              />
            </div>

            <div className="flex items-center gap-3 pt-2">
              <StatusSwitch
                isActive={formIsActive}
                onToggle={() => setFormIsActive((prev) => !prev)}
                entityName="Phase"
              />
              <span className="text-sm font-medium text-slate-700">
                {formIsActive ? 'Active' : 'Inactive'}
              </span>
            </div>

            <SheetFooter className="pt-4">
              <Button type="button" variant="outline" onClick={() => setIsSheetOpen(false)}>
                Cancel
              </Button>
              <Button type="submit" className="bg-indigo-600 hover:bg-indigo-700">
                {editingPhase ? 'Save Changes' : 'Create Phase'}
              </Button>
            </SheetFooter>
          </form>
        </div>
      </Sheet>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-rose-600">
              <AlertTriangle className="h-5 w-5" />
              Delete Phase
            </DialogTitle>
            <DialogDescription>
              Are you sure you want to soft-delete the phase{' '}
              <strong className="text-slate-900">{deleteTarget?.title}</strong>? You can restore it later if needed.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button variant="outline">Cancel</Button>
            </DialogClose>
            <Button variant="destructive" onClick={handleConfirmDelete}>
              Delete Phase
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Restore Confirmation Dialog */}
      <Dialog open={!!restoreTarget} onOpenChange={(open) => !open && setRestoreTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-amber-600">
              <RotateCcw className="h-5 w-5" />
              Restore Phase
            </DialogTitle>
            <DialogDescription>
              Are you sure you want to restore the phase{' '}
              <strong className="text-slate-900">{restoreTarget?.title}</strong> back to active management?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button variant="outline">Cancel</Button>
            </DialogClose>
            <Button className="bg-amber-600 hover:bg-amber-700 text-white" onClick={handleConfirmRestore}>
              Restore Phase
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Status Toggle Confirmation */}
      {statusTarget && (
        <StatusConfirmationDialog
          isOpen={!!statusTarget}
          onClose={() => setStatusTarget(null)}
          onConfirm={handleConfirmStatusChange}
          entityName={statusTarget.title}
          entityType="Phase"
          action={statusTarget.nextStatus ? 'activate' : 'deactivate'}
          isLoading={changePhaseStatus.isPending}
        />
      )}
    </div>
  );
}
