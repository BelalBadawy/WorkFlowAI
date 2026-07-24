import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { parse, format } from 'date-fns'
import { useAuditLogs } from '../hooks/useAuditLogs'
import { useUserLookups } from '../hooks/useUsers'
import { auditLogsApi, type AuditTrailResponse } from '../lib/audit-logs-api'
import { useToast } from '../components/ui/toast'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { Button } from '../components/ui/button'
import { DatePicker } from '../components/ui/date-picker'
import { Badge } from '../components/ui/badge'
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '../components/ui/select'
import DataTableExport from '../components/ui/DataTableExport'
import { 
  FileText, 
  Search, 
  ChevronLeft, 
  ChevronRight, 
  ArrowUpDown,
  Calendar,
  Layers,
  Globe,
  Mail,
  SlidersHorizontal,
  X
} from 'lucide-react'
import EntityDiffViewer from '../components/EntityDiffViewer'

export default function AuditLogsManagement() {
  const [searchParams, setSearchParams] = useSearchParams()
  const toast = useToast()

  // Details Sheet State
  const [selectedLog, setSelectedLog] = useState<AuditTrailResponse | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)

  // Query Params Synchronized State
  const page = parseInt(searchParams.get('page') || '1', 10)
  const pageSize = 10
  const search = searchParams.get('search') || ''
  const sortBy = searchParams.get('sortBy') || 'datetime'
  const sortDirection = (searchParams.get('sortDirection') || 'desc') as 'asc' | 'desc'
  const tableName = searchParams.get('tableName') || 'all'
  const entityId = searchParams.get('entityId') || ''
  const actionTypes = searchParams.get('actionTypes') || ''
  const fromDate = searchParams.get('fromDate') || ''
  const toDate = searchParams.get('toDate') || ''
  const userId = searchParams.get('userId') || 'all'

  // Local filter states (bound to form controls)
  const [localSearch, setLocalSearch] = useState(search)
  const [localEntityId, setLocalEntityId] = useState(entityId)
  const [localTableName, setLocalTableName] = useState(tableName)
  const [localActionTypes, setLocalActionTypes] = useState(actionTypes)
  const [localUserId, setLocalUserId] = useState(userId)
  const [localFromDate, setLocalFromDate] = useState(fromDate)
  const [localToDate, setLocalToDate] = useState(toDate)
  // Export loading state
  const [isExporting, setIsExporting] = useState(false)
  // Synchronize local states with URL search params (supporting Back/Forward navigation & hydration)
  const [prevParams, setPrevParams] = useState({
    search,
    entityId,
    tableName,
    actionTypes,
    userId,
    fromDate,
    toDate,
  })

  if (
    search !== prevParams.search ||
    entityId !== prevParams.entityId ||
    tableName !== prevParams.tableName ||
    actionTypes !== prevParams.actionTypes ||
    userId !== prevParams.userId ||
    fromDate !== prevParams.fromDate ||
    toDate !== prevParams.toDate
  ) {
    setPrevParams({
      search,
      entityId,
      tableName,
      actionTypes,
      userId,
      fromDate,
      toDate,
    })
    setLocalSearch(search)
    setLocalEntityId(entityId)
    setLocalTableName(tableName)
    setLocalActionTypes(actionTypes)
    setLocalUserId(userId)
    setLocalFromDate(fromDate)
    setLocalToDate(toDate)
  }
  // Helper to parse date string safely from yyyy/MM/dd to Date
  const parseDateString = (dateStr: string): Date | undefined => {
    if (!dateStr) return undefined
    try {
      const parsed = parse(dateStr, 'yyyy/MM/dd', new Date())
      return isNaN(parsed.getTime()) ? undefined : parsed
    } catch {
      return undefined
    }
  }

  const handleFromDateChange = (date?: Date) => {
    setLocalFromDate(date ? format(date, 'yyyy/MM/dd') : '')
  }

  const handleToDateChange = (date?: Date) => {
    setLocalToDate(date ? format(date, 'yyyy/MM/dd') : '')
  }

  // Fetch users lookup for dropdown filter
  const { data: userLookups = [] } = useUserLookups()

  // Fetch Audit Logs using TanStack Query custom hook
  const { data: queryData, isLoading: loading, error } = useAuditLogs({
    pageNumber: page,
    pageSize,
    searchTerm: search || undefined,
    sortBy,
    sortDirection,
    tableName: tableName !== 'all' ? tableName : undefined,
    entityId: entityId || undefined,
    actionTypes: actionTypes || undefined,
    fromDate: fromDate || undefined,
    toDate: toDate || undefined,
    userId: userId !== 'all' ? parseInt(userId, 10) : undefined,
  })

  const logs = queryData?.data || []
  const totalCount = queryData?.totalCount || 0

  // Display toast error on query failures
  useEffect(() => {
    if (error) {
      toast.error(error.message || 'Failed to retrieve audit logs.')
    }
  }, [error, toast])

  // Sync controls with URL search params (supporting multiple key-value pairs)
  const updateUrlParams = (params: Record<string, string | null>) => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      Object.entries(params).forEach(([key, value]) => {
        if (value) {
          next.set(key, value)
        } else {
          next.delete(key)
        }
      })
      return next
    })
  }

  // Backwards compatibility helper
  const updateUrlParam = (key: string, value: string | null) => {
    updateUrlParams({ [key]: value })
  }

  // Sorting Handler
  const handleSort = (field: string) => {
    const isAsc = sortBy === field && sortDirection === 'asc'
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      next.set('sortBy', field)
      next.set('sortDirection', isAsc ? 'desc' : 'asc')
      next.set('page', '1') // Reset to first page
      return next
    })
  }

  // Dirty check to enable/disable the "Apply Filters" button
  const isDirty = 
    localSearch.trim() !== search ||
    localEntityId.trim() !== entityId ||
    localTableName !== tableName ||
    localActionTypes !== actionTypes ||
    localUserId !== userId ||
    localFromDate !== fromDate ||
    localToDate !== toDate

  // Batch apply filters to URL
  const handleApplyFilters = (e?: React.FormEvent) => {
    if (e) e.preventDefault()
    if (localFromDate && localToDate) {
    const from = parseDateString(localFromDate)
    const to = parseDateString(localToDate)
    if (from && to && to < from) {
        toast.error("To Date cannot be before From Date")
        return
      }
    }

    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)

      if (localSearch.trim()) {
        next.set('search', localSearch.trim())
      } else {
        next.delete('search')
      }

      if (localEntityId.trim()) {
        next.set('entityId', localEntityId.trim())
      } else {
        next.delete('entityId')
      }

      if (localTableName && localTableName !== 'all') {
        next.set('tableName', localTableName)
      } else {
        next.delete('tableName')
      }

      if (localActionTypes) {
        next.set('actionTypes', localActionTypes)
      } else {
        next.delete('actionTypes')
      }

      if (localUserId && localUserId !== 'all') {
        next.set('userId', localUserId)
      } else {
        next.delete('userId')
      }

      if (localFromDate) {
        next.set('fromDate', localFromDate)
      } else {
        next.delete('fromDate')
      }

      if (localToDate) {
        next.set('toDate', localToDate)
      } else {
        next.delete('toDate')
      }

      next.set('page', '1')
      return next
    })
  }

  // Reset all advanced and basic filters
  const handleResetFilters = () => {
    setLocalSearch('')
    setLocalEntityId('')
    setLocalTableName('all')
    setLocalActionTypes('')
    setLocalUserId('all')
    setLocalFromDate('')
    setLocalToDate('')
    setSearchParams(new URLSearchParams())
  }

  // Helper: Audit Type Badge styling
  const renderTypeBadge = (type: string) => {
    switch (type.toLowerCase()) {
      case 'create':
        return <Badge className="bg-emerald-100 hover:bg-emerald-200 text-emerald-800 border-emerald-200 font-bold uppercase text-xs rounded-lg py-1 px-2.5">Create</Badge>
      case 'update':
        return <Badge className="bg-amber-100 hover:bg-amber-200 text-amber-800 border-amber-200 font-bold uppercase text-xs rounded-lg py-1 px-2.5">Update</Badge>
      case 'delete':
        return <Badge className="bg-rose-100 hover:bg-rose-200 text-rose-800 border-rose-200 font-bold uppercase text-xs rounded-lg py-1 px-2.5">Delete</Badge>
      default:
        return <Badge className="bg-blue-100 hover:bg-blue-200 text-blue-800 border-blue-200 font-bold uppercase text-xs rounded-lg py-1 px-2.5">{type}</Badge>
    }
  }

  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight text-neutral-900 flex items-center gap-2">
            <FileText className="w-8 h-8 text-[#4285F4]" /> Audit Logs Directory
          </h1>
          <p className="text-neutral-500 mt-1 text-sm">
            Monitor and audit all creation, update, and deletion actions captured globally.
          </p>
        </div>
      </div>

      {/* Advanced Filters Container */}
      <form onSubmit={handleApplyFilters} className="bg-white p-5 rounded-2xl border border-neutral-200 shadow-sm space-y-4">
        
        {/* Row 1: Search, Table Name Select, Entity ID Input, User Lookup */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          
          {/* General Search */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-neutral-400" />
            <input
              type="text"
              value={localSearch}
              onChange={(e) => setLocalSearch(e.target.value)}
              placeholder="Search Actor Email or IP Address..."
              className="w-full pl-9 pr-4 py-2 border border-neutral-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/30 focus:border-[#4285F4] transition-all bg-neutral-50/50"
            />
          </div>

          {/* Table Name Select */}
          <div>
            <Select 
              data-testid="table-name-select"
              value={localTableName} 
              onValueChange={(val) => setLocalTableName(val)}
            >
              <SelectTrigger className="w-full rounded-xl text-sm border-neutral-300 bg-neutral-50/50">
                <SelectValue placeholder="All Tables" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Tables</SelectItem>
                <SelectItem value="Category">Category</SelectItem>
                <SelectItem value="ApplicationUser">ApplicationUser</SelectItem>
                <SelectItem value="ApplicationRole">ApplicationRole</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Entity ID Search */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-neutral-400" />
            <input
              type="text"
              value={localEntityId}
              onChange={(e) => setLocalEntityId(e.target.value)}
              placeholder="Search by Entity ID..."
              className="w-full pl-9 pr-4 py-2 border border-neutral-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#4285F4]/30 focus:border-[#4285F4] transition-all bg-neutral-50/50"
            />
          </div>

          {/* User Select Dropdown */}
          <div>
            <Select 
              data-testid="user-select"
              value={localUserId} 
              onValueChange={(val) => setLocalUserId(val)}
            >
              <SelectTrigger className="w-full rounded-xl text-sm border-neutral-300 bg-neutral-50/50">
                <SelectValue placeholder="All Users" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Users</SelectItem>
                {userLookups.map((u) => (
                  <SelectItem key={u.id} value={String(u.id)}>
                    {u.fullName} ({u.email})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Row 2: Action Types Badges, Date Range Pickers & Reset Button */}
        <div className="flex flex-col xl:flex-row xl:items-center xl:justify-between gap-4 pt-2 border-t border-neutral-100">
          
          {/* Action Types Filter Badges */}
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-xs font-semibold text-neutral-500 mr-1">Event Type:</span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                const active = localActionTypes ? localActionTypes.split(',') : [];
                const nextTypes = active.includes('Create') 
                  ? active.filter(t => t !== 'Create') 
                  : [...active, 'Create'];
                setLocalActionTypes(nextTypes.join(','));
              }}
              className={`h-8 rounded-lg text-xs font-bold transition-all px-3 ${
                (localActionTypes ? localActionTypes.split(',') : []).includes('Create')
                  ? 'bg-emerald-100 border-emerald-300 text-emerald-800 hover:bg-emerald-200'
                  : 'bg-neutral-50 border-neutral-200 text-neutral-600 hover:bg-neutral-100'
              }`}
            >
              Create
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                const active = localActionTypes ? localActionTypes.split(',') : [];
                const nextTypes = active.includes('Update') 
                  ? active.filter(t => t !== 'Update') 
                  : [...active, 'Update'];
                setLocalActionTypes(nextTypes.join(','));
              }}
              className={`h-8 rounded-lg text-xs font-bold transition-all px-3 ${
                (localActionTypes ? localActionTypes.split(',') : []).includes('Update')
                  ? 'bg-amber-100 border-amber-300 text-amber-800 hover:bg-amber-200'
                  : 'bg-neutral-50 border-neutral-200 text-neutral-600 hover:bg-neutral-100'
              }`}
            >
              Update
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                const active = localActionTypes ? localActionTypes.split(',') : [];
                const nextTypes = active.includes('Delete') 
                  ? active.filter(t => t !== 'Delete') 
                  : [...active, 'Delete'];
                setLocalActionTypes(nextTypes.join(','));
              }}
              className={`h-8 rounded-lg text-xs font-bold transition-all px-3 ${
                (localActionTypes ? localActionTypes.split(',') : []).includes('Delete')
                  ? 'bg-rose-100 border-rose-300 text-rose-800 hover:bg-rose-200'
                  : 'bg-neutral-50 border-neutral-200 text-neutral-600 hover:bg-neutral-100'
              }`}
            >
              Delete
            </Button>
          </div>

          {/* Date Picker Bounds & Reset Button */}
          <div className="flex flex-wrap items-center gap-3">
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold text-neutral-500">From:</span>
              <DatePicker 
                date={parseDateString(localFromDate)}
                setDate={handleFromDateChange}
                placeholder="From Date"
                className="w-36"
              />
            </div>
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold text-neutral-500">To:</span>
              <DatePicker 
                date={parseDateString(localToDate)}
                setDate={handleToDateChange}
                placeholder="To Date"
                className="w-36"
              />
            </div>

            <Button
              type="submit"
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
              <X className="w-3.5 h-3.5" />
              Reset Filters
            </Button>
            <DataTableExport
              isExporting={isExporting}
              onExport={async (format) => {
                try {
                  setIsExporting(true);
                  await auditLogsApi.exportAuditLogs({
                    // pageNumber and pageSize are not needed for export
                    exportFormat: format,
                    searchTerm: search || undefined,
                    tableName: tableName !== 'all' ? tableName : undefined,
                    entityId: entityId || undefined,
                    actionTypes: actionTypes || undefined,
                    fromDate: fromDate || undefined,
                    toDate: toDate || undefined,
                    userId: userId !== 'all' ? parseInt(userId, 10) : undefined,
                  });
                } catch (err) {
                  const message = err instanceof Error ? err.message : String(err);
                  toast.error(message);
                } finally {
                  setIsExporting(false);
                }
              }}
            />
          </div>

        </div>

        {/* Dynamic Result Count Label */}
        <div className="flex justify-end text-xs text-neutral-400 pt-1">
          <SlidersHorizontal className="w-3.5 h-3.5 mr-1" />
          <span>Showing {logs.length} of {totalCount} records</span>
        </div>
      </form>

      {/* Main Grid table */}
      <Card className="rounded-2xl border border-neutral-200 shadow-sm overflow-hidden bg-white">
        <CardHeader className="border-b border-neutral-100 py-4 px-6">
          <CardTitle className="text-lg font-bold text-neutral-900">Activity Log</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-neutral-50/80 border-b border-neutral-200 text-xs font-bold text-neutral-600 uppercase tracking-wider">
                  <th className="py-4 px-6">
                    <button 
                      onClick={() => handleSort('id')} 
                      className="flex items-center gap-1 hover:text-neutral-900 transition-colors"
                    >
                      Log ID <ArrowUpDown className="w-3 h-3" />
                    </button>
                  </th>
                  <th className="py-4 px-6">
                    <button 
                      onClick={() => handleSort('tablename')} 
                      className="flex items-center gap-1 hover:text-neutral-900 transition-colors"
                    >
                      Affected Table <ArrowUpDown className="w-3 h-3" />
                    </button>
                  </th>
                  <th className="py-4 px-6">
                    <button 
                      onClick={() => handleSort('type')} 
                      className="flex items-center gap-1 hover:text-neutral-900 transition-colors"
                    >
                      Event Type <ArrowUpDown className="w-3 h-3" />
                    </button>
                  </th>
                  <th className="py-4 px-6">Actor Email</th>
                  <th className="py-4 px-6">IP Address</th>
                  <th className="py-4 px-6">
                    <button 
                      onClick={() => handleSort('datetime')} 
                      className="flex items-center gap-1 hover:text-neutral-900 transition-colors"
                    >
                      Timestamp <ArrowUpDown className="w-3 h-3" />
                    </button>
                  </th>
                  <th className="py-4 px-6 text-center">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-100 text-sm text-neutral-700">
                {loading ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-neutral-400">
                      <div className="flex flex-col items-center gap-2">
                        <div className="w-6 h-6 border-2 border-[#4285F4] border-t-transparent rounded-full animate-spin"></div>
                        <span>Loading audit logs...</span>
                      </div>
                    </td>
                  </tr>
                ) : logs.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-neutral-400">
                      No matching audit trails found.
                    </td>
                  </tr>
                ) : (
                  logs.map((log) => (
                    <tr key={log.id} className="hover:bg-neutral-50/50 transition-colors">
                      <td className="py-4 px-6 font-mono text-xs font-semibold text-[#4285F4]">#{log.id}</td>
                      <td className="py-4 px-6 font-bold text-neutral-950 flex items-center gap-1.5">
                        <Layers className="w-3.5 h-3.5 text-neutral-400" />
                        {log.tableName}
                      </td>
                      <td className="py-4 px-6">{renderTypeBadge(log.type)}</td>
                      <td className="py-4 px-6">
                        {log.userEmail ? (
                          <span className="flex items-center gap-1">
                            <Mail className="w-3.5 h-3.5 text-neutral-400" />
                            {log.userEmail}
                          </span>
                        ) : (
                          <span className="text-neutral-400 italic">System / Anonymous</span>
                        )}
                      </td>
                      <td className="py-4 px-6 font-mono text-xs text-neutral-600">
                        {log.ipAddress ? (
                          <span className="flex items-center gap-1">
                            <Globe className="w-3.5 h-3.5 text-neutral-400" />
                            {log.ipAddress}
                          </span>
                        ) : (
                          <span className="text-neutral-400">-</span>
                        )}
                      </td>
                      <td className="py-4 px-6">
                        <span className="flex items-center gap-1 text-neutral-600 text-xs">
                          <Calendar className="w-3.5 h-3.5 text-neutral-400" />
                          {new Date(log.dateTime).toLocaleString()}
                        </span>
                      </td>
                      <td className="py-4 px-6 text-center">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setSelectedLog(log)
                            setSheetOpen(true)
                          }}
                          className="h-8 border-[#4285F4]/30 text-[#4285F4] hover:bg-[#4285F4]/5 font-semibold text-xs rounded-xl"
                        >
                          View Details
                        </Button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-neutral-100 px-6 py-4 bg-neutral-50/50">
              <span className="text-xs text-neutral-400">
                Page {page} of {totalPages} ({totalCount} total logs)
              </span>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="icon"
                  disabled={page <= 1}
                  onClick={() => updateUrlParam('page', String(page - 1))}
                  className="w-8 h-8 rounded-lg"
                >
                  <ChevronLeft className="w-4 h-4" />
                </Button>
                {Array.from({ length: totalPages }, (_, i) => i + 1)
                  .filter((p) => Math.abs(p - page) <= 1 || p === 1 || p === totalPages)
                  .map((p, idx, arr) => {
                    const prev = arr[idx - 1]
                    const showEllipsis = prev && p - prev > 1
                    return (
                      <div key={p} className="flex items-center gap-1">
                        {showEllipsis && <span className="text-neutral-400 text-xs px-1">...</span>}
                        <Button
                          variant={page === p ? 'default' : 'outline'}
                          onClick={() => updateUrlParam('page', String(p))}
                          className={`w-8 h-8 rounded-lg text-xs font-bold ${page === p ? 'bg-[#4285F4] hover:bg-[#3273DC]' : ''}`}
                        >
                          {p}
                        </Button>
                      </div>
                    )
                  })}
                <Button
                  variant="outline"
                  size="icon"
                  disabled={page >= totalPages}
                  onClick={() => updateUrlParam('page', String(page + 1))}
                  className="w-8 h-8 rounded-lg"
                >
                  <ChevronRight className="w-4 h-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* --- DETAILS SIDE SHEET PANEL --- */}
      {sheetOpen && selectedLog && (
        <div className="fixed inset-0 z-50 animate-in fade-in duration-200">
          {/* Backdrop */}
          <div 
            className="fixed inset-0 bg-black/40 backdrop-blur-xs" 
            onClick={() => setSheetOpen(false)} 
          />
          {/* Content Pane */}
          <div className="fixed top-0 right-0 bottom-0 w-full md:w-3/5 max-w-3xl bg-white border-l border-neutral-200 shadow-2xl p-6 flex flex-col z-50 animate-in slide-in-from-right duration-250">
            
            {/* Header */}
            <div className="flex items-center justify-between pb-4 border-b border-neutral-100">
              <div>
                <h2 className="text-xl font-extrabold text-neutral-900 flex items-center gap-2">
                  <FileText className="w-5 h-5 text-[#4285F4]" /> Audit Log details
                </h2>
                <p className="text-xs text-[#4285F4] font-mono mt-1 font-semibold">Log ID: #{selectedLog.id}</p>
              </div>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setSheetOpen(false)}
                className="h-9 w-9 text-neutral-500 hover:bg-neutral-100 rounded-lg"
              >
                <X className="w-5 h-5" />
              </Button>
            </div>

            {/* Scrollable details */}
            <div className="flex-1 overflow-y-auto py-6 space-y-6 pr-1">
              
              {/* Log Metadata Grid */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 bg-neutral-50 p-4 rounded-2xl border border-neutral-200 text-sm">
                <div>
                  <span className="text-xs text-neutral-400 uppercase tracking-wider block">Affected Table</span>
                  <span className="font-bold text-neutral-900">{selectedLog.tableName}</span>
                </div>
                <div>
                  <span className="text-xs text-neutral-400 uppercase tracking-wider block">Event Type</span>
                  <span className="mt-0.5 inline-block">{renderTypeBadge(selectedLog.type)}</span>
                </div>
                <div>
                  <span className="text-xs text-neutral-400 uppercase tracking-wider block">Actor Email</span>
                  <span className="font-medium text-neutral-900">
                    {selectedLog.userEmail || 'System / Anonymous'}
                  </span>
                </div>
                <div>
                  <span className="text-xs text-neutral-400 uppercase tracking-wider block">IP Address</span>
                  <span className="font-mono text-xs font-medium text-neutral-900">
                    {selectedLog.ipAddress || '-'}
                  </span>
                </div>
                <div className="sm:col-span-2">
                  <span className="text-xs text-neutral-400 uppercase tracking-wider block">Primary Key (Identifier)</span>
                  <span className="font-mono text-xs text-neutral-800 break-all">
                    {selectedLog.primaryKey || 'None'}
                  </span>
                </div>
              </div>

              {/* Old vs New Values JSON Viewers */}
              <div className="space-y-4">
                {selectedLog.affectedColumns && (
                  <div>
                    <h3 className="text-xs font-bold text-neutral-400 uppercase tracking-wider mb-2">Affected Columns</h3>
                    <div className="flex flex-wrap gap-1.5">
                      {JSON.parse(selectedLog.affectedColumns).map((col: string) => (
                        <Badge key={col} variant="outline" className="text-xs bg-neutral-50 font-semibold">{col}</Badge>
                      ))}
                    </div>
                  </div>
                )}

                <EntityDiffViewer 
                  oldValues={selectedLog.oldValues} 
                  newValues={selectedLog.newValues} 
                />
              </div>

            </div>

            {/* Footer */}
            <div className="pt-4 border-t border-neutral-100 flex justify-end">
              <Button
                onClick={() => setSheetOpen(false)}
                className="bg-neutral-900 hover:bg-neutral-800 text-white rounded-xl px-6"
              >
                Close Panel
              </Button>
            </div>

          </div>
        </div>
      )}
    </div>
  )
}