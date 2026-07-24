import React, { useEffect, useState } from 'react';
import { useAuth } from '../components/AuthContext';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { useToast } from '../components/ui/toast';
import { Sheet, SheetHeader, SheetTitle, SheetDescription, SheetFooter } from '../components/ui/sheet';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from '../components/ui/dialog';
import { rolesApi } from '../lib/roles-api';
import type { RoleResponse } from '../lib/roles-api';
import { 
  Plus, Edit2, Trash2, Search, RotateCcw, 
  Loader2, AlertTriangle, ShieldCheck, ChevronDown, ChevronRight 
} from 'lucide-react';

interface ParsedPermission {
  claimValue: string;
  description: string;
  service: string;
  feature: string;
  action: string;
  selected: boolean;
}

export default function RoleManagement() {
  const { hasPermission } = useAuth();
  const toast = useToast();

  // List States
  const [roles, setRoles] = useState<RoleResponse[]>([]);
  const [loading, setLoading] = useState(true);
  
  // Search & Filter
  const [searchQuery, setSearchQuery] = useState('');

  // Dialog & Sheet States
  const [isFormSheetOpen, setIsFormSheetOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [targetRole, setTargetRole] = useState<RoleResponse | null>(null);

  // Form States
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create');
  const [formName, setFormName] = useState('');
  const [formDescription, setFormDescription] = useState('');
  const [formSubmitting, setFormSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Permissions Matrix States
  const [permissions, setPermissions] = useState<ParsedPermission[]>([]);
  const [permissionsLoading, setPermissionsLoading] = useState(false);
  const [expandedCategories, setExpandedCategories] = useState<Record<string, boolean>>({});

  // Fetch all roles
  const fetchRoles = async () => {
    setLoading(true);
    try {
      const response = await rolesApi.getAll();
      if (response.isSuccessful && response.data) {
        setRoles(response.data);
      } else {
        toast.error(response.messages[0] || 'Failed to retrieve roles.');
      }
    } catch (err) {
      console.error(err);
      toast.error('An error occurred while loading roles.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRoles();
  }, []);

  // Form Validation
  const validateForm = () => {
    const nextErrors: Record<string, string> = {};

    if (!formName.trim()) {
      nextErrors.name = 'Role Name is required';
    } else if (formName.trim().length < 3) {
      nextErrors.name = 'Role Name must be at least 3 characters';
    }

    if (!formDescription.trim()) {
      nextErrors.description = 'Description is required';
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  // Helper to parse flat permissions list into a structured format
  const parsePermissions = (claims: { claimValue: string; description: string; selected?: boolean }[]): ParsedPermission[] => {
    return claims.map(c => {
      const parts = c.claimValue.split('.');
      // Format is: Permission.{Service}.{Feature}.{Action}
      const service = parts[1] || 'General';
      const feature = parts[2] || 'System';
      const action = parts[3] || 'Execute';

      return {
        claimValue: c.claimValue,
        description: c.description,
        service,
        feature,
        action,
        selected: c.selected || false,
      };
    });
  };

  // Load permissions list
  const loadPermissionsMatrix = async (roleId: number | null) => {
    setPermissionsLoading(true);
    try {
      let targetId = roleId;
      
      // If creating a role, fetch permissions from first available role and uncheck all
      if (targetId === null) {
        if (roles.length > 0) {
          targetId = roles[0].id;
        } else {
          // If no roles exist, we can't fetch permissions layout from DB. Fallback to empty.
          setPermissions([]);
          setPermissionsLoading(false);
          return;
        }
      }

      const res = await rolesApi.getPermissions(targetId);
      if (res.isSuccessful && res.data) {
        const parsed = parsePermissions(res.data.roleClaims);
        
        // If creating, reset all selection flags to false
        if (roleId === null) {
          parsed.forEach(p => p.selected = false);
        }
        
        setPermissions(parsed);

        // Expand all categories by default
        const categories: Record<string, boolean> = {};
        parsed.forEach(p => {
          categories[`${p.service}.${p.feature}`] = true;
        });
        setExpandedCategories(categories);
      } else {
        toast.error('Failed to load system permissions list.');
      }
    } catch (err) {
      console.error(err);
      toast.error('Error loading permissions matrix.');
    } finally {
      setPermissionsLoading(false);
    }
  };

  // Open Create Role
  const openCreateSheet = () => {
    setFormMode('create');
    setFormName('');
    setFormDescription('');
    setPermissions([]);
    setErrors({});
    setIsFormSheetOpen(true);
    loadPermissionsMatrix(null);
  };

  // Open Edit Role
  const openEditSheet = (role: RoleResponse) => {
    setFormMode('edit');
    setTargetRole(role);
    setFormName(role.name);
    setFormDescription(role.description);
    setPermissions([]);
    setErrors({});
    setIsFormSheetOpen(true);
    loadPermissionsMatrix(role.id);
  };

  // Toggle individual permission checkbox
  const handleTogglePermission = (claimValue: string) => {
    setPermissions(prev =>
      prev.map(p => p.claimValue === claimValue ? { ...p, selected: !p.selected } : p)
    );
  };

  // Toggle all permissions under a specific Service.Feature category
  const handleToggleCategory = (categoryKey: string, checked: boolean) => {
    setPermissions(prev =>
      prev.map(p => {
        const key = `${p.service}.${p.feature}`;
        return key === categoryKey ? { ...p, selected: checked } : p;
      })
    );
  };

  // Toggle all permissions globally
  const handleToggleAll = (checked: boolean) => {
    setPermissions(prev => prev.map(p => ({ ...p, selected: checked })));
  };

  // Toggle category collapse/expand state
  const toggleCategoryExpand = (categoryKey: string) => {
    setExpandedCategories(prev => ({
      ...prev,
      [categoryKey]: !prev[categoryKey],
    }));
  };

  // Submit Create/Edit Role + Permissions Matrix
  const handleSubmitRole = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setFormSubmitting(true);
    try {
      let savedRoleId = targetRole?.id || 0;
      let success = false;

      if (formMode === 'create') {
        const response = await rolesApi.create({
          name: formName,
          description: formDescription,
        });

        if (response.isSuccessful) {
          // Fetch newly created role ID to bind claims
          const allRolesRes = await rolesApi.getAll();
          if (allRolesRes.isSuccessful && allRolesRes.data) {
            const newlyCreated = allRolesRes.data.find(r => r.name === formName);
            if (newlyCreated) {
              savedRoleId = newlyCreated.id;
              success = true;
            }
          }
        } else {
          toast.error(response.messages[0] || 'Failed to create role.');
        }
      } else {
        if (!targetRole) return;
        const response = await rolesApi.update({
          roleId: targetRole.id,
          name: formName,
          description: formDescription,
        });

        if (response.isSuccessful) {
          success = true;
        } else {
          toast.error(response.messages[0] || 'Failed to update role details.');
        }
      }

      // If metadata saved successfully, update role permissions matrix
      if (success && savedRoleId > 0) {
        const claimsPayload = permissions.map(p => ({
          claimType: 'Permission',
          claimValue: p.claimValue,
          description: p.description,
          selected: p.selected,
        })).filter(c => c.selected); // Backend expects list of enabled claims

        const permissionsRes = await rolesApi.updatePermissions({
          roleId: savedRoleId,
          roleName: formName,
          roleClaims: claimsPayload,
        } as any);

        if (permissionsRes.isSuccessful) {
          toast.success(`Role and permissions saved successfully!`);
          setIsFormSheetOpen(false);
          fetchRoles();
        } else {
          toast.warning(`Role info saved, but permissions update failed: ${permissionsRes.messages[0]}`);
        }
      }
    } catch (err) {
      console.error(err);
      toast.error('An error occurred during form submission.');
    } finally {
      setFormSubmitting(false);
    }
  };

  // Delete Role confirmation
  const requestDelete = (role: RoleResponse) => {
    setTargetRole(role);
    setIsDeleteDialogOpen(true);
  };

  const executeDeleteRole = async () => {
    if (!targetRole) return;
    try {
      const response = await rolesApi.delete(targetRole.id);
      if (response.isSuccessful) {
        toast.success(`Role "${targetRole.name}" deleted successfully.`);
        fetchRoles();
      } else {
        toast.error(response.messages[0] || 'Failed to delete role.');
      }
    } catch (err) {
      console.error(err);
      toast.error('An error occurred during deletion.');
    } finally {
      setIsDeleteDialogOpen(false);
      setTargetRole(null);
    }
  };

  // Filter roles client-side by query
  const filteredRoles = roles.filter(role => 
    role.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    role.description.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Group permissions state for visual matrix rendering
  const groupedPermissions: Record<string, ParsedPermission[]> = {};
  permissions.forEach(p => {
    const key = `${p.service}.${p.feature}`;
    if (!groupedPermissions[key]) {
      groupedPermissions[key] = [];
    }
    groupedPermissions[key].push(p);
  });

  const allSelected = permissions.length > 0 && permissions.every(p => p.selected);
  const someSelected = permissions.some(p => p.selected) && !allSelected;

  return (
    <div className="space-y-6">
      
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight text-neutral-900">Role Management</h1>
          <p className="text-sm text-neutral-500 mt-1">Manage global security roles, access classifications, and user permissions.</p>
        </div>

        {/* Create Role Button Guarded by hasPermission */}
        {hasPermission('Permission.Identity.Roles.Create') ? (
          <Button onClick={openCreateSheet} className="bg-[#4285F4] hover:bg-[#3273DC] text-white font-bold py-2.5 px-5 rounded-xl shadow-sm flex items-center gap-2 transition-all">
            <Plus className="w-4 h-4" />
            Create Role
          </Button>
        ) : (
          <div className="text-xs text-amber-600 bg-amber-50 border border-amber-100 p-2.5 rounded-xl font-medium flex items-center gap-1.5 max-w-xs leading-tight">
            <AlertTriangle className="w-4 h-4 shrink-0 text-amber-500" />
            <span>Role creation disabled due to insufficient permissions.</span>
          </div>
        )}
      </div>

      {/* Search Bar */}
      <Card className="bg-white border-neutral-200 shadow-sm rounded-xl">
        <CardContent className="p-4">
          <div className="flex gap-3">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-neutral-400" />
              <input
                type="text"
                placeholder="Search roles by name or description..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-neutral-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/30 focus:border-[#4285F4] bg-neutral-50/50"
              />
            </div>
            {searchQuery && (
              <Button type="button" variant="outline" onClick={() => setSearchQuery('')} className="rounded-xl px-4 flex items-center gap-1">
                <RotateCcw className="w-3.5 h-3.5" />
                Clear
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Roles List Card */}
      <Card className="bg-white border border-neutral-200 shadow-xl rounded-2xl overflow-hidden">
        <CardHeader className="border-b border-neutral-100 pb-4 bg-neutral-50/30">
          <div className="flex justify-between items-center">
            <div>
              <CardTitle className="text-lg font-bold text-neutral-900">Security Roles</CardTitle>
              <CardDescription>A list of roles currently defined in the Identity system.</CardDescription>
            </div>
            <div className="text-xs font-semibold text-neutral-400">
              Total Roles: {filteredRoles.length}
            </div>
          </div>
        </CardHeader>

        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-neutral-200 bg-neutral-50/50 text-neutral-500 text-xs font-bold uppercase tracking-wider">
                  <th className="px-6 py-4">ID</th>
                  <th className="px-6 py-4">Role Name</th>
                  <th className="px-6 py-4">Description</th>
                  <th className="px-6 py-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-100 text-sm text-neutral-800">
                {loading ? (
                  <tr>
                    <td colSpan={4} className="text-center py-12 text-neutral-400">
                      <div className="flex items-center justify-center gap-2">
                        <Loader2 className="w-5 h-5 animate-spin text-[#4285F4]" />
                        <span>Retrieving roles list...</span>
                      </div>
                    </td>
                  </tr>
                ) : filteredRoles.length === 0 ? (
                  <tr>
                    <td colSpan={4} className="text-center py-12 text-neutral-400">
                      No matching roles found.
                    </td>
                  </tr>
                ) : (
                  filteredRoles.map((role) => (
                    <tr key={role.id} className="hover:bg-neutral-50/50 transition-colors">
                      <td className="px-6 py-4 font-bold text-neutral-400">
                        #{role.id}
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <span className="font-extrabold text-neutral-950">{role.name}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4 text-neutral-500 max-w-sm truncate">
                        {role.description}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          {/* Edit Role Button - Guarded */}
                          {hasPermission('Permission.Identity.Roles.Update') && (
                            <Button 
                              variant="ghost" 
                              size="icon" 
                              onClick={() => openEditSheet(role)}
                              className="h-8 w-8 text-neutral-500 hover:text-neutral-900 hover:bg-neutral-100 rounded-lg"
                              title="Edit Details"
                            >
                              <Edit2 className="w-3.5 h-3.5" />
                            </Button>
                          )}

                          {/* Delete Role Button - Guarded */}
                          {hasPermission('Permission.Identity.Roles.Delete') && role.name.toLowerCase() !== 'admin' && (
                            <Button 
                              variant="ghost" 
                              size="icon" 
                              onClick={() => requestDelete(role)}
                              className="h-8 w-8 text-rose-500 hover:text-rose-900 hover:bg-rose-50 rounded-lg"
                              title="Delete Role"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                            </Button>
                          )}

                          {!hasPermission('Permission.Identity.Roles.Update') && 
                           !hasPermission('Permission.Identity.Roles.Delete') && (
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
        </CardContent>
      </Card>

      {/* Info Alert Box */}
      <div className="p-4 bg-[#4285F4]/5 border border-[#4285F4]/10 rounded-2xl flex gap-3 text-[#4285F4] text-xs max-w-2xl leading-relaxed">
        <ShieldCheck className="w-5 h-5 shrink-0 mt-0.5" />
        <div className="space-y-1">
          <span className="font-bold block">Dynamic Roles Authorization Guards Active</span>
          Access settings, roles editing and removal features are guarded dynamically in accordance with your security claims.
        </div>
      </div>

      {/* ROLE FORM SHEET (WITH DYNAMIC PERMISSIONS MATRIX) */}
      <Sheet open={isFormSheetOpen} onOpenChange={setIsFormSheetOpen}>
        <SheetHeader>
          <SheetTitle>{formMode === 'create' ? 'Create New Security Role' : 'Modify Role details'}</SheetTitle>
          <SheetDescription>
            Specify the name, description, and permission overrides of the role.
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmitRole} className="space-y-4 mt-4 flex flex-col h-[calc(100vh-140px)]">
          <div className="flex-1 overflow-y-auto pr-1 space-y-4">
            {/* Role Name */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-neutral-600 block">Role Name</label>
              <input
                type="text"
                value={formName}
                onChange={(e) => setFormName(e.target.value)}
                placeholder="e.g. Moderator"
                className={`w-full p-2.5 border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20 ${
                  errors.name ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200 focus:border-[#4285F4]'
                }`}
              />
              {errors.name && <p className="text-rose-500 text-[11px] font-medium">{errors.name}</p>}
            </div>

            {/* Description */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-neutral-600 block">Description</label>
              <textarea
                value={formDescription}
                onChange={(e) => setFormDescription(e.target.value)}
                placeholder="Describe the responsibilities and scope of this role..."
                rows={2}
                className={`w-full p-2.5 border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/20 ${
                  errors.description ? 'border-rose-500 focus:border-rose-500' : 'border-neutral-200 focus:border-[#4285F4]'
                }`}
              />
              {errors.description && <p className="text-rose-500 text-[11px] font-medium">{errors.description}</p>}
            </div>

            {/* Permissions Matrix */}
            <div className="border-t border-neutral-100 pt-4 space-y-3">
              <div className="flex items-center justify-between">
                <div>
                  <span className="text-xs font-bold text-neutral-800 block">Permissions Authorization Matrix</span>
                  <span className="text-[10px] text-neutral-400 font-medium block">Configure granular resource access claims.</span>
                </div>

                {/* Global Toggles */}
                {permissions.length > 0 && (
                  <div className="flex items-center gap-2">
                    <label className="text-[10px] font-bold text-neutral-500 cursor-pointer" htmlFor="global-toggle">
                      Select All
                    </label>
                    <input
                      id="global-toggle"
                      type="checkbox"
                      checked={allSelected}
                      ref={el => {
                        if (el) el.indeterminate = someSelected;
                      }}
                      onChange={(e) => handleToggleAll(e.target.checked)}
                      className="w-3.5 h-3.5 accent-[#4285F4] rounded cursor-pointer"
                    />
                  </div>
                )}
              </div>

              {permissionsLoading ? (
                <div className="flex items-center justify-center py-10 gap-2 text-xs text-neutral-400">
                  <Loader2 className="w-4 h-4 animate-spin text-[#4285F4]" />
                  <span>Loading permissions schema...</span>
                </div>
              ) : (
                <div className="space-y-3 mt-2">
                  {Object.entries(groupedPermissions).map(([categoryKey, list]) => {
                    const [service, feature] = categoryKey.split('.');
                    const isExpanded = expandedCategories[categoryKey];
                    
                    const catAllSelected = list.every(p => p.selected);
                    const catSomeSelected = list.some(p => p.selected) && !catAllSelected;

                    return (
                      <div key={categoryKey} className="border border-neutral-100 rounded-xl overflow-hidden bg-neutral-50/30">
                        {/* Accordion Category Header */}
                        <div className="flex items-center justify-between p-3 bg-neutral-50 border-b border-neutral-100 select-none">
                          <div 
                            className="flex items-center gap-1.5 cursor-pointer flex-1"
                            onClick={() => toggleCategoryExpand(categoryKey)}
                          >
                            {isExpanded ? (
                              <ChevronDown className="w-4 h-4 text-neutral-500" />
                            ) : (
                              <ChevronRight className="w-4 h-4 text-neutral-500" />
                            )}
                            <div className="text-xs">
                              <span className="font-extrabold text-neutral-900 block">{feature}</span>
                              <span className="text-[9px] text-neutral-400 font-bold uppercase tracking-wider block">{service} Module</span>
                            </div>
                          </div>

                          {/* Category Bulk Toggle */}
                          <div className="flex items-center gap-1.5 pr-1">
                            <input
                              type="checkbox"
                              checked={catAllSelected}
                              ref={el => {
                                if (el) el.indeterminate = catSomeSelected;
                              }}
                              onChange={(e) => handleToggleCategory(categoryKey, e.target.checked)}
                              className="w-3.5 h-3.5 accent-[#4285F4] rounded cursor-pointer"
                            />
                          </div>
                        </div>

                        {/* Accordion Body */}
                        {isExpanded && (
                          <div className="p-3 bg-white grid grid-cols-1 sm:grid-cols-2 gap-2">
                            {list.map(p => (
                              <div
                                key={p.claimValue}
                                onClick={() => handleTogglePermission(p.claimValue)}
                                className={`p-2 rounded-lg border text-left flex items-center justify-between cursor-pointer select-none transition-all ${
                                  p.selected
                                    ? 'border-[#4285F4] bg-[#4285F4]/5 text-neutral-950'
                                    : 'border-neutral-200 bg-white hover:bg-neutral-50 text-neutral-500'
                                }`}
                              >
                                <div className="pr-2 leading-tight">
                                  <span className="text-[11px] font-bold block">{p.description}</span>
                                  <span className="text-[9px] text-neutral-400 font-medium block truncate max-w-[160px]" title={p.claimValue}>
                                    {p.action}
                                  </span>
                                </div>
                                <input
                                  type="checkbox"
                                  checked={p.selected}
                                  readOnly
                                  className="w-3 h-3 accent-[#4285F4] shrink-0"
                                />
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </div>

          <SheetFooter className="border-t border-neutral-100 pt-3 bg-white">
            <div className="flex w-full gap-2">
              <Button 
                type="submit" 
                disabled={formSubmitting} 
                className="flex-1 bg-[#4285F4] hover:bg-[#3273DC] text-white rounded-xl"
              >
                {formSubmitting ? 'Saving changes...' : 'Save Role & Permissions'}
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

      {/* DELETE CONFIRMATION DIALOG */}
      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        {targetRole && (
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Delete Security Role</DialogTitle>
              <DialogDescription>
                Are you sure you want to permanently delete the role "{targetRole.name}"? This operation cannot be undone.
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <Button 
                variant="destructive"
                onClick={executeDeleteRole}
                className="rounded-xl px-5"
              >
                Delete Role
              </Button>
              <DialogClose onClick={() => setIsDeleteDialogOpen(false)}>
                Cancel
              </DialogClose>
            </DialogFooter>
          </DialogContent>
        )}
      </Dialog>
    </div>
  );
}