import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../components/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Badge } from '../components/ui/badge';
import { Sheet, SheetHeader, SheetTitle, SheetDescription, SheetFooter } from '../components/ui/sheet';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from '../components/ui/dialog';
import { PasswordStrengthMeter } from '../components/PasswordStrengthMeter';
import { usersApi } from '../lib/users-api';
import type { UserResponse } from '../lib/users-api';
import { 
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue 
} from '../components/ui/select';
import { 
  Plus, Edit2, Trash2, UserCheck, AlertTriangle, 
  Search, RotateCcw, Lock, Unlock, Loader2 
} from 'lucide-react';
import { useToast } from '../components/ui/toast';
import DataTableExport from '../components/ui/DataTableExport';
import { StatusSwitch } from '../components/shared/StatusSwitch';
import { StatusConfirmationDialog } from '../components/shared/StatusConfirmationDialog';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '../components/ui/tooltip';
import { 
  useReactTable, 
  getCoreRowModel, 
} from '@tanstack/react-table';
import type { ColumnDef } from '@tanstack/react-table';
import {
  useUserList,
  useAvailableRoles,
  useRegisterUser,
  useUpdateUserAndRoles,
  useLockUser,
  useUnlockUser,
  useChangeUserStatus,
  useDeleteUser,
} from '../hooks/useUsers';
import { DataTablePagination } from '../components/ui/DataTablePagination';

const columns: ColumnDef<UserResponse>[] = [
  { accessorKey: 'fullName', header: 'User' },
  { accessorKey: 'email', header: 'Email' },
  { accessorKey: 'roles', header: 'Assigned Roles' },
  { accessorKey: 'status', header: 'Status' },
];

export default function UsersManagement() {
  const { hasPermission } = useAuth();

  // Query / Filter / Pagination States from URL
  const [searchParams, setSearchParams] = useSearchParams();

  const pageNumber = parseInt(searchParams.get('page') || '1', 10);
  const pageSize = parseInt(searchParams.get('size') || '10', 10);
  const searchTerm = searchParams.get('search') || '';
  const activeParam = searchParams.get('active') || 'all';
  const lockedParam = searchParams.get('locked') || 'all';
  const roleParam = searchParams.get('role') || 'all';
  const sortBy = searchParams.get('sortBy') || 'fullname';
  const sortDirection = (searchParams.get('sortDir') || 'asc') as 'asc' | 'desc';

  // Local filter states
  const [localSearch, setLocalSearch] = useState(searchTerm);
  const [localActive, setLocalActive] = useState(activeParam);
  const [localLocked, setLocalLocked] = useState(lockedParam);
  const [localRole, setLocalRole] = useState(roleParam);

  // Synchronize local states with URL search params (supporting Back/Forward navigation & hydration)
  const [prevParams, setPrevParams] = useState({
    search: searchTerm,
    active: activeParam,
    locked: lockedParam,
    role: roleParam,
  });

  if (
    searchTerm !== prevParams.search ||
    activeParam !== prevParams.active ||
    lockedParam !== prevParams.locked ||
    roleParam !== prevParams.role
  ) {
    setPrevParams({
      search: searchTerm,
      active: activeParam,
      locked: lockedParam,
      role: roleParam,
    });
    setLocalSearch(searchTerm);
    setLocalActive(activeParam);
    setLocalLocked(lockedParam);
    setLocalRole(roleParam);
  }

  const isDirty =
    localSearch.trim() !== searchTerm ||
    localActive !== activeParam ||
    localLocked !== lockedParam ||
    localRole !== roleParam;

  const handleApplyFilters = (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    const nextParams = new URLSearchParams(searchParams);
    nextParams.set('page', '1');

    const newParams = {
      search: localSearch.trim(),
      active: localActive,
      locked: localLocked,
      role: localRole,
    };

    Object.entries(newParams).forEach(([key, val]) => {
      if (!val || val === 'all' || val === '') {
        nextParams.delete(key);
      } else {
        nextParams.set(key, val);
      }
    });

    setSearchParams(nextParams);
  };

  const handleResetFilters = () => {
    setLocalSearch('');
    setLocalActive('all');
    setLocalLocked('all');
    setLocalRole('all');
    setSearchParams(new URLSearchParams());
  };

  const updateFilters = (newParams: Record<string, string | null>) => {
    const nextParams = new URLSearchParams(searchParams);
    
    // Changing page/size/sortBy/sortDir resets to page 1 if applicable
    if (newParams.page === undefined && (newParams.sortBy !== undefined || newParams.sortDir !== undefined)) {
      nextParams.set('page', '1');
    }

    Object.entries(newParams).forEach(([key, val]) => {
      if (val === null || val === 'all' || (key === 'page' && val === '1') || (key === 'search' && val === '') || (key === 'sortBy' && val === 'fullname') || (key === 'sortDir' && val === 'asc') || (key === 'size' && val === '10')) {
        nextParams.delete(key);
      } else {
        nextParams.set(key, val);
      }
    });

    setSearchParams(nextParams);
  };

  // Dialog & Sheet States
  const [isFormSheetOpen, setIsFormSheetOpen] = useState(false);
  const [isConfirmDialogOpen, setIsConfirmDialogOpen] = useState(false);
  const [confirmAction, setConfirmAction] = useState<'lock' | 'unlock' | 'delete' | 'activate' | 'deactivate' | null>(null);
  const [targetUser, setTargetUser] = useState<UserResponse | null>(null);
  
  // Form States
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create');
  const [formFullName, setFormFullName] = useState('');
  const [formEmail, setFormEmail] = useState('');
  const [formPhoneNumber, setFormPhoneNumber] = useState('');
  const [formPassword, setFormPassword] = useState('');
  const [formConfirmPassword, setFormConfirmPassword] = useState('');
  const [formActivateUser, setFormActivateUser] = useState(true);
  const [formAutoConfirmEmail, setFormAutoConfirmEmail] = useState(false);
  const [formRoles, setFormRoles] = useState<string[]>([]);

  // Validation States
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isExporting, setIsExporting] = useState(false);
  const toast = useToast();

  // Query & Mutation Hooks
  const isActiveParam = activeParam === 'active' ? true : activeParam === 'inactive' ? false : null;
  const isLockedParam = lockedParam === 'locked' ? true : lockedParam === 'unlocked' ? false : null;
  const roleIdParam = roleParam !== 'all' ? parseInt(roleParam, 10) : null;

  const { data: pagedData, isLoading: loading } = useUserList({
    pageNumber,
    pageSize,
    searchTerm,
    sortBy,
    sortDirection,
    isActive: isActiveParam,
    isLocked: isLockedParam,
    roleId: roleIdParam,
  });

  const { data: availableRoles = [] } = useAvailableRoles();

  const registerMutation = useRegisterUser();
  const updateMutation = useUpdateUserAndRoles();
  const lockMutation = useLockUser();
  const unlockMutation = useUnlockUser();
  const changeStatusMutation = useChangeUserStatus();
  const deleteMutation = useDeleteUser();

  const users = pagedData?.data || [];
  const totalCount = pagedData?.totalCount || 0;
  const userRolesMap = pagedData?.rolesMap || {};

  const formSubmitting = registerMutation.isPending || updateMutation.isPending;
  const confirmPending = 
    lockMutation.isPending || 
    unlockMutation.isPending || 
    changeStatusMutation.isPending || 
    deleteMutation.isPending;

  // React Table Instance
  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: users,
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
      updateFilters({
        page: String(next.pageIndex + 1),
        size: String(next.pageSize),
      });
    },
    onSortingChange: (updater) => {
      const current = [{ id: sortBy, desc: sortDirection === 'desc' }];
      const next = typeof updater === 'function' ? updater(current) : updater;
      if (next && next.length > 0) {
        updateFilters({
          sortBy: next[0].id,
          sortDir: next[0].desc ? 'desc' : 'asc',
          page: '1',
        });
      }
    },
    manualPagination: true,
    manualSorting: true,
    getCoreRowModel: getCoreRowModel(),
  });



  // Toggle Sorting
  const toggleSort = (field: string) => {
    const dir = sortBy === field && sortDirection === 'asc' ? 'desc' : 'asc';
    updateFilters({
      sortBy: field,
      sortDir: dir,
      page: '1',
    });
  };

  // Form Validation
  const validateForm = () => {
    const nextErrors: Record<string, string> = {};

    if (!formFullName.trim()) {
      nextErrors.fullName = 'Full Name is required';
    } else if (formFullName.trim().length < 3) {
      nextErrors.fullName = 'Full Name must be at least 3 characters';
    }

    if (formMode === 'create') {
      if (!formEmail.trim()) {
        nextErrors.email = 'Email is required';
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formEmail)) {
        nextErrors.email = 'Please enter a valid email address';
      }

      if (!formPassword) {
        nextErrors.password = 'Password is required';
      } else {
        // Password strength complexity validation
        const hasMinLength = formPassword.length >= 6;
        const hasUpper = /[A-Z]/.test(formPassword);
        const hasLower = /[a-z]/.test(formPassword);
        const hasNumber = /\d/.test(formPassword);
        const hasSpecial = /[^a-zA-Z0-9]/.test(formPassword);

        if (!hasMinLength || !hasUpper || !hasLower || !hasNumber || !hasSpecial) {
          nextErrors.password = 'Password is too weak';
        }
      }

      if (formPassword !== formConfirmPassword) {
        nextErrors.confirmPassword = 'Passwords do not match';
      }
    }

    if (!formPhoneNumber.trim()) {
      nextErrors.phoneNumber = 'Phone Number is required';
    } else if (!/^[0-9+\s-]{7,15}$/.test(formPhoneNumber)) {
      nextErrors.phoneNumber = 'Invalid phone number format';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  // Open Create Form
  const openCreateSheet = () => {
    setFormMode('create');
    setFormFullName('');
    setFormEmail('');
    setFormPhoneNumber('');
    setFormPassword('');
    setFormConfirmPassword('');
    setFormActivateUser(true);
    setFormAutoConfirmEmail(false);
    setFormRoles(['Basic']);
    setErrors({});
    setIsFormSheetOpen(true);
  };

  // Open Edit Form
  const openEditSheet = async (user: UserResponse) => {
    setFormMode('edit');
    setTargetUser(user);
    setFormFullName(user.fullName);
    setFormEmail(user.email);
    setFormPhoneNumber(user.phoneNumber);
    setFormRoles(userRolesMap[user.id] || []);
    setErrors({});
    setIsFormSheetOpen(true);
  };

  // Submit User creation/edit Form
  const handleSubmitUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    if (formMode === 'create') {
      registerMutation.mutate({
        fullName: formFullName,
        email: formEmail,
        password: formPassword,
        confirmPassword: formConfirmPassword,
        phoneNumber: formPhoneNumber,
        activateUser: formActivateUser,
        autoConfirmEmail: formAutoConfirmEmail,
      }, {
        onSuccess: (response) => {
          if (response.isSuccessful) {
            setIsFormSheetOpen(false);
          }
        }
      });
    } else {
      if (!targetUser) return;
      updateMutation.mutate({
        userId: targetUser.id,
        fullName: formFullName,
        phoneNumber: formPhoneNumber,
        roles: formRoles,
      }, {
        onSuccess: (response) => {
          if (response.profileRes.isSuccessful && response.rolesRes.isSuccessful) {
            setIsFormSheetOpen(false);
          }
        }
      });
    }
  };

  // Role selections toggle helper
  const handleRoleToggle = (roleName: string) => {
    setFormRoles(prev => 
      prev.includes(roleName) 
        ? prev.filter(r => r !== roleName) 
        : [...prev, roleName]
    );
  };

  // Open Actions Confirmation Modal
  const requestConfirm = (action: 'lock' | 'unlock' | 'delete' | 'activate' | 'deactivate', user: UserResponse) => {
    setConfirmAction(action);
    setTargetUser(user);
    setIsConfirmDialogOpen(true);
  };

  // Execute lock/unlock/delete/activate/deactivate operations
  const executeConfirmAction = async () => {
    if (!targetUser || !confirmAction) return;

    if (confirmAction === 'lock') {
      lockMutation.mutate(targetUser.id, {
        onSuccess: (res) => {
          if (res.isSuccessful) setIsConfirmDialogOpen(false);
        }
      });
    } else if (confirmAction === 'unlock') {
      unlockMutation.mutate(targetUser.id, {
        onSuccess: (res) => {
          if (res.isSuccessful) setIsConfirmDialogOpen(false);
        }
      });
    } else if (confirmAction === 'activate') {
      changeStatusMutation.mutate({ userId: targetUser.id, activate: true }, {
        onSuccess: (res) => {
          if (res.isSuccessful) setIsConfirmDialogOpen(false);
        }
      });
    } else if (confirmAction === 'deactivate') {
      changeStatusMutation.mutate({ userId: targetUser.id, activate: false }, {
        onSuccess: (res) => {
          if (res.isSuccessful) setIsConfirmDialogOpen(false);
        }
      });
    } else if (confirmAction === 'delete') {
      deleteMutation.mutate(targetUser.fullName, {
        onSuccess: () => {
          setIsConfirmDialogOpen(false);
        }
      });
    }
  };

  return (
    <TooltipProvider>
      <div className="space-y-6">
      
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight text-neutral-900">User Management</h1>
          <p className="text-sm text-neutral-500 mt-1">Search, lock, unlock, and manage system user records.</p>
        </div>

        {/* Create User Button Guarded by hasPermission */}
        {hasPermission('Permission.Identity.Users.Create') ? (
          <Button onClick={openCreateSheet} className="bg-[#4285F4] hover:bg-[#3273DC] text-white font-bold py-2.5 px-5 rounded-xl shadow-sm flex items-center gap-2 transition-all">
            <Plus className="w-4 h-4" />
            Create User
          </Button>
        ) : (
          <div className="text-xs text-amber-600 bg-amber-50 border border-amber-100 p-2.5 rounded-xl font-medium flex items-center gap-1.5 max-w-xs leading-tight">
            <AlertTriangle className="w-4 h-4 shrink-0 text-amber-500" />
            <span>Creation disabled due to insufficient permissions.</span>
          </div>
        )}
      </div>

      {/* Consolidated Filters Container */}
      <div className="bg-white p-5 rounded-2xl border border-neutral-200 shadow-sm space-y-4">
        {/* Row 1: Controls */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          {/* General Search */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-neutral-400" />
            <input
              type="text"
              placeholder="Search by full name or email..."
              value={localSearch}
              onChange={(e) => setLocalSearch(e.target.value)}
              className="w-full pl-9 pr-4 py-2 border border-neutral-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/30 focus:border-[#4285F4] transition-all bg-neutral-50/50"
            />
          </div>

          {/* Status Filter */}
          <div>
            <Select value={localActive} onValueChange={(val) => setLocalActive(val)}>
              <SelectTrigger className="w-full h-[38px] border-neutral-300 rounded-xl bg-neutral-50/50 focus:ring-[#4285F4]/30">
                <SelectValue placeholder="All Statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Statuses</SelectItem>
                <SelectItem value="active">Active Only</SelectItem>
                <SelectItem value="inactive">Inactive Only</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Lockout Filter */}
          <div>
            <Select value={localLocked} onValueChange={(val) => setLocalLocked(val)}>
              <SelectTrigger className="w-full h-[38px] border-neutral-300 rounded-xl bg-neutral-50/50 focus:ring-[#4285F4]/30">
                <SelectValue placeholder="All Lockouts" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Lockouts</SelectItem>
                <SelectItem value="locked">Locked Only</SelectItem>
                <SelectItem value="unlocked">Unlocked Only</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Role Filter */}
          <div>
            <Select value={localRole} onValueChange={(val) => setLocalRole(val)}>
              <SelectTrigger className="w-full h-[38px] border-neutral-300 rounded-xl bg-neutral-50/50 focus:ring-[#4285F4]/30">
                <SelectValue placeholder="All Roles" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Roles</SelectItem>
                {availableRoles.map((role) => (
                  <SelectItem key={role.id} value={String(role.id)}>
                    {role.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Row 2: Action Buttons */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 pt-2 border-t border-neutral-100">
          <div className="text-xs text-neutral-400">
            Showing {users.length} of {totalCount} records
          </div>
          <div className="flex items-center gap-3 w-full sm:w-auto justify-end">
            <Button
              type="button"
              onClick={() => handleApplyFilters()}
              disabled={!isDirty}
              className="h-8 bg-[#4285F4] hover:bg-[#3273DC] text-white font-semibold text-xs rounded-xl flex items-center gap-1 disabled:opacity-50 disabled:pointer-events-none"
            >
              Apply Filters
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleResetFilters}
              className="h-8 border-neutral-300 text-neutral-600 hover:bg-neutral-100 font-semibold text-xs rounded-xl flex items-center gap-1"
            >
              <RotateCcw className="w-3.5 h-3.5" />
              Reset Filters
            </Button>
            <DataTableExport
              isExporting={isExporting}
              onExport={async (format) => {
                try {
                  setIsExporting(true);
                  await usersApi.exportUsers({
                    searchTerm: searchTerm || undefined,
                    isActive: isActiveParam,
                    isLocked: isLockedParam,
                    roleId: roleIdParam,
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
        </div>
      </div>

      {/* Main Table Card */}
      <Card className="bg-white border border-neutral-200 shadow-xl rounded-2xl overflow-hidden">
        <CardHeader className="border-b border-neutral-100 pb-4 bg-neutral-50/30">
          <div className="flex justify-between items-center">
            <div>
              <CardTitle className="text-lg font-bold text-neutral-900">System Users</CardTitle>
              <CardDescription>A paginated overview of system accounts.</CardDescription>
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
                  <th onClick={() => toggleSort('fullname')} className="px-6 py-4 cursor-pointer hover:bg-neutral-100 select-none transition-colors">
                    User {sortBy === 'fullname' && (sortDirection === 'asc' ? 'â–²' : 'â–¼')}
                  </th>
                  <th onClick={() => toggleSort('email')} className="px-6 py-4 cursor-pointer hover:bg-neutral-100 select-none transition-colors">
                    Email {sortBy === 'email' && (sortDirection === 'asc' ? 'â–²' : 'â–¼')}
                  </th>
                  <th className="px-6 py-4">Assigned Roles</th>
                  <th className="px-6 py-4">Status</th>
                  <th className="px-6 py-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-100 text-sm text-neutral-800">
                {loading ? (
                  <tr>
                    <td colSpan={5} className="text-center py-12 text-neutral-400">
                      <div className="flex items-center justify-center gap-2">
                        <Loader2 className="w-5 h-5 animate-spin text-[#4285F4]" />
                        <span>Loading user directory...</span>
                      </div>
                    </td>
                  </tr>
                ) : users.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="text-center py-12 text-neutral-400">
                      No matching user accounts found.
                    </td>
                  </tr>
                ) : (
                  users.map((usr) => {
                    const isUserAdmin = userRolesMap[usr.id]?.some(r => r.toLowerCase() === 'admin');
                    return (
                      <tr key={usr.id} className="hover:bg-neutral-50/50 transition-colors">
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-3">
                            <div className="w-9 h-9 rounded-full bg-neutral-100 text-neutral-600 font-bold flex items-center justify-center text-xs border border-neutral-200">
                              {usr.fullName[0]?.toUpperCase() || '?'}
                            </div>
                            <div>
                              <div className="font-bold text-neutral-900">{usr.fullName}</div>
                              <div className="text-xs text-neutral-400 font-medium">#{usr.id} Â· {usr.phoneNumber || 'No phone'}</div>
                            </div>
                          </div>
                        </td>
                        <td className="px-6 py-4 font-medium text-neutral-600">
                          {usr.email}
                        </td>
                        <td className="px-6 py-4">
                          <div className="flex flex-wrap gap-1">
                            {userRolesMap[usr.id]?.length ? (
                              userRolesMap[usr.id].map(r => (
                                <Badge key={r} variant="secondary" className="bg-[#4285F4]/10 text-[#4285F4] hover:bg-[#4285F4]/20 border-transparent text-xs font-semibold px-2 py-0.5 rounded">
                                  {r}
                                </Badge>
                              ))
                            ) : (
                              <span className="text-xs text-neutral-400 italic">None</span>
                            )}
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <div className="flex flex-col gap-1 items-start">
                            <div className="flex flex-wrap gap-1">
                              <Badge 
                                variant={usr.isActive ? "default" : "secondary"}
                                className={
                                  usr.isActive 
                                    ? 'bg-emerald-500 hover:bg-emerald-600 text-white font-bold' 
                                    : 'bg-neutral-200 text-neutral-600 hover:bg-neutral-300 font-bold'
                                }
                              >
                                {usr.isActive ? 'Active' : 'Inactive'}
                              </Badge>
                              {usr.isLocked && (
                                <Badge 
                                  className="bg-amber-500 hover:bg-amber-600 text-white font-bold border-transparent"
                                >
                                  Locked
                                </Badge>
                              )}
                            </div>
                            {usr.emailConfirmed && (
                              <span className="text-[10px] text-emerald-600 font-semibold bg-emerald-50 border border-emerald-100 rounded px-1">Email Verified</span>
                            )}
                          </div>
                        </td>
                        <td className="px-6 py-4 text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            {/* Lock / Unlock Toggle buttons */}
                            {usr.isLocked ? (
                              hasPermission('Permission.Identity.Users.Unlock') && !isUserAdmin && (
                                <Tooltip delayDuration={300}>
                                  <TooltipTrigger asChild>
                                    <Button 
                                      variant="ghost" 
                                      size="icon" 
                                      onClick={() => requestConfirm('unlock', usr)}
                                      className="h-8 w-8 text-emerald-500 hover:text-emerald-700 hover:bg-emerald-50 rounded-lg"
                                    >
                                      <Unlock className="w-3.5 h-3.5" />
                                    </Button>
                                  </TooltipTrigger>
                                  <TooltipContent>Unlock User Account</TooltipContent>
                                </Tooltip>
                              )
                            ) : (
                              hasPermission('Permission.Identity.Users.Lock') && !isUserAdmin && (
                                <Tooltip delayDuration={300}>
                                  <TooltipTrigger asChild>
                                    <Button 
                                      variant="ghost" 
                                      size="icon" 
                                      onClick={() => requestConfirm('lock', usr)}
                                      className="h-8 w-8 text-amber-500 hover:text-amber-700 hover:bg-amber-50 rounded-lg"
                                    >
                                      <Lock className="w-3.5 h-3.5" />
                                    </Button>
                                  </TooltipTrigger>
                                  <TooltipContent>Lock User Account</TooltipContent>
                                </Tooltip>
                              )
                            )}

                            {/* Active / Inactive Status Toggle buttons */}
                            {hasPermission('Permission.Identity.Users.Update') && !isUserAdmin && (
                              <StatusSwitch
                                isActive={usr.isActive}
                                onToggle={() => requestConfirm(usr.isActive ? 'deactivate' : 'activate', usr)}
                                entityName={usr.fullName}
                                isLoading={changeStatusMutation.isPending && targetUser?.id === usr.id}
                              />
                            )}

                            {/* Edit User Button - Guarded */}
                            {hasPermission('Permission.Identity.Users.Update') && (
                              <Tooltip delayDuration={300}>
                                <TooltipTrigger asChild>
                                  <Button 
                                    variant="ghost" 
                                    size="icon" 
                                    onClick={() => openEditSheet(usr)}
                                    className="h-8 w-8 text-neutral-500 hover:text-neutral-900 hover:bg-neutral-100 rounded-lg"
                                  >
                                    <Edit2 className="w-3.5 h-3.5" />
                                  </Button>
                                </TooltipTrigger>
                                <TooltipContent>Edit Details & Roles</TooltipContent>
                              </Tooltip>
                            )}

                            {/* Delete User Button - Guarded */}
                            {hasPermission('Permission.Identity.Users.Delete') && !isUserAdmin && (
                              <Tooltip delayDuration={300}>
                                <TooltipTrigger asChild>
                                  <Button 
                                    variant="ghost" 
                                    size="icon" 
                                    onClick={() => requestConfirm('delete', usr)}
                                    className="h-8 w-8 text-rose-500 hover:text-rose-900 hover:bg-rose-50 rounded-lg"
                                  >
                                    <Trash2 className="w-3.5 h-3.5" />
                                  </Button>
                                </TooltipTrigger>
                                <TooltipContent>Delete User</TooltipContent>
                              </Tooltip>
                            )}

                            {!hasPermission('Permission.Identity.Users.Update') && 
                             !hasPermission('Permission.Identity.Users.Delete') && 
                             !hasPermission('Permission.Identity.Users.Lock') && 
                             !hasPermission('Permission.Identity.Users.Unlock') && (
                              <span className="text-xs text-neutral-400 font-medium italic">Read-Only</span>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
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

      {/* Informative Security Panel */}
      <div className="p-4 bg-[#4285F4]/5 border border-[#4285F4]/10 rounded-2xl flex gap-3 text-[#4285F4] text-xs max-w-2xl leading-relaxed">
        <UserCheck className="w-5 h-5 shrink-0 mt-0.5" />
        <div className="space-y-1">
          <span className="font-bold block">Dynamic Action Authorization Guard Active</span>
          Admin panel buttons such as Create, Edit, Lock, and Unlock are controlled dynamically based on identity claim validation.
        </div>
      </div>

      {/* CREATE / EDIT SHEET PANEL */}
      <Sheet open={isFormSheetOpen} onOpenChange={setIsFormSheetOpen}>
        <SheetHeader>
          <SheetTitle>{formMode === 'create' ? 'Create New System Account' : 'Modify User details'}</SheetTitle>
          <SheetDescription>
            {formMode === 'create'
              ? 'Provide profile information and assign initial privileges.'
              : 'Edit name, phone number, and system access groups.'}
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmitUser} className="space-y-4 mt-4">
          
          {/* Full Name */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-neutral-600 block">Full Name</label>
            <input
              type="text"
              value={formFullName}
              onChange={(e) => setFormFullName(e.target.value)}
              placeholder="e.g. Belal Badawy"
              className={`w-full p-2.5 border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20 ${
                errors.fullName ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200 focus:border-[#4285F4]'
              }`}
            />
            {errors.fullName && <p className="text-rose-500 text-[11px] font-medium">{errors.fullName}</p>}
          </div>

          {/* Email (Creation Mode Only) */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-neutral-600 block">Email Address</label>
            <input
              type="email"
              value={formEmail}
              disabled={formMode === 'edit'}
              onChange={(e) => setFormEmail(e.target.value)}
              placeholder="e.g. user@domain.com"
              className="w-full p-2.5 border border-neutral-200 rounded-xl text-sm disabled:bg-neutral-100 disabled:text-neutral-500 focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20"
            />
            {errors.email && <p className="text-rose-500 text-[11px] font-medium">{errors.email}</p>}
          </div>

          {/* Phone Number */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-neutral-600 block">Phone Number</label>
            <input
              type="text"
              value={formPhoneNumber}
              onChange={(e) => setFormPhoneNumber(e.target.value)}
              placeholder="e.g. +1 555-0199"
              className={`w-full p-2.5 border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20 ${
                errors.phoneNumber ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200 focus:border-[#4285F4]'
              }`}
            />
            {errors.phoneNumber && <p className="text-rose-500 text-[11px] font-medium">{errors.phoneNumber}</p>}
          </div>

          {/* Password Fields (Creation Mode Only) */}
          {formMode === 'create' && (
            <>
              <div className="space-y-1">
                <label className="text-xs font-bold text-neutral-600 block">Password</label>
                <input
                  type="password"
                  value={formPassword}
                  onChange={(e) => setFormPassword(e.target.value)}
                  placeholder="Enter strong password"
                  className="w-full p-2.5 border border-neutral-200 rounded-xl text-sm focus:outline-none focus:ring-2"
                />
                <PasswordStrengthMeter password={formPassword} />
              </div>

              <div className="space-y-1">
                <label className="text-xs font-bold text-neutral-600 block">Confirm Password</label>
                <input
                  type="password"
                  value={formConfirmPassword}
                  onChange={(e) => setFormConfirmPassword(e.target.value)}
                  placeholder="Confirm password"
                  className={`w-full p-2.5 border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20 ${
                    errors.confirmPassword ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200 focus:border-[#4285F4]'
                  }`}
                />
                {errors.confirmPassword && <p className="text-rose-500 text-[11px] font-medium">{errors.confirmPassword}</p>}
              </div>

              {/* Status Toggles */}
              <div className="flex flex-col gap-2 p-3 bg-neutral-50 rounded-xl border border-neutral-100">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold text-neutral-700">Activate Account Instantly</span>
                  <input 
                    type="checkbox" 
                    checked={formActivateUser} 
                    onChange={(e) => setFormActivateUser(e.target.checked)}
                    className="w-4 h-4 accent-[#4285F4]"
                  />
                </div>
                <div className="flex items-center justify-between border-t border-neutral-200/50 pt-2 mt-1">
                  <span className="text-xs font-bold text-neutral-700">Auto Confirm Email</span>
                  <input 
                    type="checkbox" 
                    checked={formAutoConfirmEmail} 
                    onChange={(e) => setFormAutoConfirmEmail(e.target.checked)}
                    className="w-4 h-4 accent-[#4285F4]"
                  />
                </div>
              </div>
            </>
          )}

          {/* User Roles Selection */}
          <div className="space-y-2 border-t border-neutral-100 pt-4 mt-2">
            <span className="text-xs font-bold text-neutral-600 block">Assigned Roles / Access Groups</span>
            <div className="grid grid-cols-2 gap-2">
              {availableRoles.map((role) => (
                <div 
                  key={role.id} 
                  onClick={() => handleRoleToggle(role.name)}
                  className={`p-2.5 rounded-xl border flex items-center justify-between cursor-pointer select-none transition-all ${
                    formRoles.includes(role.name) 
                      ? 'border-[#4285F4] bg-[#4285F4]/5 text-neutral-900' 
                      : 'border-neutral-200 bg-white hover:bg-neutral-50 text-neutral-500'
                  }`}
                >
                  <div className="text-xs">
                    <span className="font-bold block">{role.name}</span>
                    <span className="text-[10px] text-neutral-400 font-medium block leading-tight">{role.description}</span>
                  </div>
                  <input 
                    type="checkbox" 
                    checked={formRoles.includes(role.name)}
                    readOnly
                    className="w-3.5 h-3.5 accent-[#4285F4]"
                  />
                </div>
              ))}
            </div>
          </div>

          <SheetFooter>
            <div className="flex w-full gap-2 mt-4">
              <Button 
                type="submit" 
                disabled={formSubmitting} 
                className="flex-1 bg-[#4285F4] hover:bg-[#3273DC] text-white"
              >
                {formSubmitting ? 'Saving changes...' : 'Save User'}
              </Button>
              <Button 
                type="button" 
                variant="outline" 
                onClick={() => setIsFormSheetOpen(false)}
                className="flex-1 rounded-xl"
              >
                Cancel
              </Button>
            </div>
          </SheetFooter>
        </form>
      </Sheet>

      {/* STATUS CONFIRMATION DIALOG */}
      <StatusConfirmationDialog
        isOpen={isConfirmDialogOpen && (confirmAction === 'activate' || confirmAction === 'deactivate')}
        onClose={() => setIsConfirmDialogOpen(false)}
        onConfirm={executeConfirmAction}
        entityName={targetUser?.fullName || ''}
        entityType="user"
        action={(confirmAction === 'activate' || confirmAction === 'deactivate') ? confirmAction : 'activate'}
        isLoading={confirmPending}
      />

      {/* CONFIRMATION DIALOG */}
      <Dialog open={isConfirmDialogOpen && confirmAction !== 'activate' && confirmAction !== 'deactivate'} onOpenChange={setIsConfirmDialogOpen}>
        {targetUser && confirmAction && confirmAction !== 'activate' && confirmAction !== 'deactivate' && (
          <DialogContent>
            <DialogHeader>
              <DialogTitle className="capitalize">{confirmAction} Account</DialogTitle>
              <DialogDescription>
                {confirmAction === 'lock' && `Are you sure you want to lock the account of ${targetUser.fullName}? They will be blocked from logging into the application.`}
                {confirmAction === 'unlock' && `Are you sure you want to unlock the account of ${targetUser.fullName}?`}
                {confirmAction === 'delete' && `Are you sure you want to delete ${targetUser.fullName}? This operation will delete their account data.`}
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <Button 
                variant={confirmAction === 'delete' ? 'destructive' : 'default'}
                disabled={confirmPending}
                onClick={executeConfirmAction}
                className="rounded-xl px-5"
              >
                {confirmPending && <Loader2 className="w-4 h-4 animate-spin mr-1.5 inline" />}
                Confirm
              </Button>
              <DialogClose onClick={() => setIsConfirmDialogOpen(false)} className="border-neutral-200 text-neutral-600 hover:bg-neutral-100 font-bold">
                Cancel
              </DialogClose>
            </DialogFooter>
          </DialogContent>
        )}
      </Dialog>
    </div>
    </TooltipProvider>
  );
}