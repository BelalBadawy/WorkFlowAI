import { useState, useMemo } from 'react'
import { 
  Search, 
  Copy, 
  Check, 
  Info,
  SlidersHorizontal,
  FileCode,
  TableProperties
} from 'lucide-react'

interface EntityDiffViewerProps {
  oldValues: string | null
  newValues: string | null
}

interface DiffItem {
  key: string
  oldVal: any
  newVal: any
  type: 'added' | 'deleted' | 'modified' | 'unchanged'
}

// Helper to safely parse JSON
const parseJsonSafe = (jsonStr: string | null): Record<string, any> | null => {
  if (!jsonStr) return null
  try {
    const parsed = JSON.parse(jsonStr)
    return typeof parsed === 'object' && parsed !== null ? parsed : null
  } catch (e) {
    return null
  }
}

// Helper to format values for tabular display
const formatValue = (val: any): string => {
  if (val === null || val === undefined) return '-'
  if (typeof val === 'boolean') return val ? 'True' : 'False'
  if (typeof val === 'object') {
    try {
      return JSON.stringify(val)
    } catch {
      return String(val)
    }
  }
  return String(val)
}

// Copy to Clipboard Button Component
const CopyButton = ({ text }: { text: string }) => {
  const [copied, setCopied] = useState(false)

  const handleCopy = async (e: React.MouseEvent) => {
    e.stopPropagation()
    try {
      await navigator.clipboard.writeText(text)
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    } catch (err) {
      // Fallback if clipboard API fails
    }
  }

  return (
    <button
      onClick={handleCopy}
      className="p-1 text-neutral-400 hover:text-neutral-700 hover:bg-neutral-100 rounded-md transition-all ml-1 opacity-0 group-hover:opacity-100 focus:opacity-100 focus:outline-none"
      title="Copy value to clipboard"
    >
      {copied ? (
        <Check className="w-3.5 h-3.5 text-emerald-600" />
      ) : (
        <Copy className="w-3.5 h-3.5" />
      )}
    </button>
  )
}

export default function EntityDiffViewer({ oldValues, newValues }: EntityDiffViewerProps) {
  const [activeTab, setActiveTab] = useState<'visual' | 'raw'>('visual')
  const [searchQuery, setSearchQuery] = useState('')
  const [showChangesOnly, setShowChangesOnly] = useState(false)

  // Compute property changes dynamically
  const diffItems = useMemo<DiffItem[]>(() => {
    const oldObj = parseJsonSafe(oldValues)
    const newObj = parseJsonSafe(newValues)

    if (!oldObj && !newObj) return []

    const allKeys = Array.from(
      new Set([
        ...Object.keys(oldObj || {}),
        ...Object.keys(newObj || {})
      ])
    ).sort()

    return allKeys.map((key) => {
      const hasOld = oldObj !== null && key in oldObj
      const hasNew = newObj !== null && key in newObj

      const oldVal = hasOld ? oldObj[key] : undefined
      const newVal = hasNew ? newObj[key] : undefined

      let type: 'added' | 'deleted' | 'modified' | 'unchanged' = 'unchanged'

      if (hasOld && !hasNew) {
        type = 'deleted'
      } else if (!hasOld && hasNew) {
        type = 'added'
      } else {
        // Compare values by stringifying to catch object/array discrepancies
        const oldStr = JSON.stringify(oldVal)
        const newStr = JSON.stringify(newVal)
        if (oldStr !== newStr) {
          type = 'modified'
        }
      }

      return { key, oldVal, newVal, type }
    })
  }, [oldValues, newValues])

  // Filter diff items based on search query and checkbox toggle
  const filteredItems = useMemo(() => {
    return diffItems.filter((item) => {
      const matchesSearch = item.key.toLowerCase().includes(searchQuery.toLowerCase())
      const matchesChangesOnly = !showChangesOnly || item.type !== 'unchanged'
      return matchesSearch && matchesChangesOnly
    })
  }, [diffItems, searchQuery, showChangesOnly])

  // Determine mutation helper context message
  const getContextMessage = () => {
    const hasOld = !!oldValues && parseJsonSafe(oldValues) !== null
    const hasNew = !!newValues && parseJsonSafe(newValues) !== null

    if (hasOld && !hasNew) {
      return 'Entity was Deleted. All values displayed are pre-mutation states.'
    }
    if (!hasOld && hasNew) {
      return 'Entity was Created. All values displayed are post-mutation states.'
    }
    return 'Entity was Updated. Displaying differences between pre and post-mutation states.'
  }

  // Pretty prints JSON strings
  const formatJson = (jsonStr: string | null) => {
    if (!jsonStr) return 'None'
    try {
      const parsed = JSON.parse(jsonStr)
      return JSON.stringify(parsed, null, 2)
    } catch (e) {
      return jsonStr
    }
  }

  return (
    <div className="space-y-4">
      {/* Sleek Glassmorphic Tab Selector */}
      <div className="flex border-b border-neutral-200">
        <button
          onClick={() => setActiveTab('visual')}
          className={`flex items-center gap-2 py-2.5 px-4 text-sm font-bold border-b-2 transition-all duration-200 -mb-[2px] ${
            activeTab === 'visual'
              ? 'border-[#4285F4] text-[#4285F4]'
              : 'border-transparent text-neutral-500 hover:text-neutral-800'
          }`}
        >
          <TableProperties className="w-4 h-4" />
          Interactive Table Diff
        </button>
        <button
          onClick={() => setActiveTab('raw')}
          className={`flex items-center gap-2 py-2.5 px-4 text-sm font-bold border-b-2 transition-all duration-200 -mb-[2px] ${
            activeTab === 'raw'
              ? 'border-[#4285F4] text-[#4285F4]'
              : 'border-transparent text-neutral-500 hover:text-neutral-800'
          }`}
        >
          <FileCode className="w-4 h-4" />
          Raw JSON View
        </button>
      </div>

      {activeTab === 'visual' ? (
        <div className="space-y-4 animate-in fade-in duration-200">
          
          {/* Metadata context banner */}
          <div className="flex items-start gap-2 bg-blue-50/50 border border-blue-100 text-blue-800 text-xs p-3 rounded-xl">
            <Info className="w-4 h-4 text-blue-500 shrink-0 mt-0.5" />
            <span>{getContextMessage()}</span>
          </div>

          {/* Search and Filters Controls */}
          {diffItems.length > 0 && (
            <div className="flex flex-col sm:flex-row gap-3 items-center justify-between bg-neutral-50/60 p-3 rounded-xl border border-neutral-200">
              <div className="relative w-full sm:max-w-xs">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-neutral-400" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Search properties..."
                  className="w-full pl-9 pr-3 py-1.5 border border-neutral-300 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-[#4285F4]/30 focus:border-[#4285F4] bg-white transition-all"
                />
              </div>

              <div className="flex items-center gap-2 shrink-0">
                <label className="flex items-center gap-2 text-xs font-semibold text-neutral-600 cursor-pointer select-none">
                  <input
                    type="checkbox"
                    checked={showChangesOnly}
                    onChange={(e) => setShowChangesOnly(e.target.checked)}
                    className="w-4 h-4 text-[#4285F4] border-neutral-300 rounded-sm focus:ring-[#4285F4]"
                  />
                  Show modified properties only
                </label>
                <SlidersHorizontal className="w-3.5 h-3.5 text-neutral-400" />
              </div>
            </div>
          )}

          {/* Main Visual Diff Table */}
          <div className="border border-neutral-200 rounded-xl overflow-hidden shadow-xs bg-white">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse table-fixed">
                <thead>
                  <tr className="bg-neutral-50/80 border-b border-neutral-200 text-xs font-bold text-neutral-500 uppercase tracking-wider">
                    <th className="py-3 px-4 w-1/3">Property</th>
                    <th className="py-3 px-4 w-1/3">Old Value (Before)</th>
                    <th className="py-3 px-4 w-1/3">New Value (After)</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-neutral-100 text-xs text-neutral-700">
                  {filteredItems.length === 0 ? (
                    <tr>
                      <td colSpan={3} className="py-8 text-center text-neutral-400 italic">
                        {diffItems.length === 0 
                          ? 'No properties captured.' 
                          : 'No properties match the current filters.'}
                      </td>
                    </tr>
                  ) : (
                    filteredItems.map((item) => {
                      const oldText = formatValue(item.oldVal)
                      const newText = formatValue(item.newVal)

                      // Custom row styling based on modification state
                      let rowBg = 'hover:bg-neutral-50/40 transition-colors group'
                      let statusBadge = null

                      if (item.type === 'added') {
                        rowBg = 'bg-emerald-50/20 hover:bg-emerald-50/40 transition-colors group'
                        statusBadge = (
                          <span className="ml-2 inline-flex items-center px-1.5 py-0.5 rounded text-[9px] font-bold uppercase tracking-wider bg-emerald-100 text-emerald-800 border border-emerald-200">
                            Added
                          </span>
                        )
                      } else if (item.type === 'deleted') {
                        rowBg = 'bg-rose-50/20 hover:bg-rose-50/40 transition-colors group'
                        statusBadge = (
                          <span className="ml-2 inline-flex items-center px-1.5 py-0.5 rounded text-[9px] font-bold uppercase tracking-wider bg-rose-100 text-rose-800 border border-rose-200">
                            Deleted
                          </span>
                        )
                      } else if (item.type === 'modified') {
                        rowBg = 'bg-amber-50/20 hover:bg-amber-50/40 transition-colors group'
                        statusBadge = (
                          <span className="ml-2 inline-flex items-center px-1.5 py-0.5 rounded text-[9px] font-bold uppercase tracking-wider bg-amber-100 text-amber-800 border border-amber-200">
                            Modified
                          </span>
                        )
                      }

                      return (
                        <tr key={item.key} className={rowBg}>
                          {/* Property Name */}
                          <td className="py-3 px-4 font-semibold text-neutral-900 break-words flex items-center flex-wrap gap-y-1">
                            <span className="font-mono">{item.key}</span>
                            {statusBadge}
                          </td>

                          {/* Old Value */}
                          <td className="py-3 px-4 break-all leading-normal">
                            {item.type === 'added' ? (
                              <span className="text-neutral-400 italic">None</span>
                            ) : item.type === 'deleted' ? (
                              <span className="text-rose-700 font-medium line-through bg-rose-100/50 px-1.5 py-0.5 rounded border border-rose-200/40 font-mono inline-flex items-center">
                                {oldText}
                                <CopyButton text={oldText} />
                              </span>
                            ) : item.type === 'modified' ? (
                              <span className="text-rose-600 bg-rose-50/60 px-2 py-0.5 rounded border border-rose-100 font-mono inline-flex items-center">
                                {oldText}
                                <CopyButton text={oldText} />
                              </span>
                            ) : (
                              <span className="font-mono text-neutral-600 inline-flex items-center">
                                {oldText}
                                <CopyButton text={oldText} />
                              </span>
                            )}
                          </td>

                          {/* New Value */}
                          <td className="py-3 px-4 break-all leading-normal">
                            {item.type === 'deleted' ? (
                              <span className="text-neutral-400 italic">None</span>
                            ) : item.type === 'added' ? (
                              <span className="text-emerald-700 font-semibold bg-emerald-100/50 px-1.5 py-0.5 rounded border border-emerald-200/40 font-mono inline-flex items-center">
                                {newText}
                                <CopyButton text={newText} />
                              </span>
                            ) : item.type === 'modified' ? (
                              <span className="text-emerald-700 font-semibold bg-emerald-50/60 px-2 py-0.5 rounded border border-emerald-100 font-mono inline-flex items-center">
                                {newText}
                                <CopyButton text={newText} />
                              </span>
                            ) : (
                              <span className="font-mono text-neutral-600 inline-flex items-center">
                                {newText}
                                <CopyButton text={newText} />
                              </span>
                            )}
                          </td>
                        </tr>
                      )
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 animate-in fade-in duration-200">
          {/* Old Values JSON */}
          <div className="space-y-2 group">
            <h3 className="text-xs font-bold text-neutral-500 uppercase tracking-wider flex items-center justify-between">
              <span className="flex items-center gap-1">
                <span className="w-1.5 h-1.5 rounded-full bg-rose-500"></span> Old Values (Pre-mutation)
              </span>
              {oldValues && <CopyButton text={formatJson(oldValues)} />}
            </h3>
            <pre className="bg-rose-50/10 text-rose-950 p-4 rounded-xl overflow-x-auto text-xs font-mono border border-rose-100/60 max-h-[400px] leading-relaxed">
              {formatJson(oldValues)}
            </pre>
          </div>

          {/* New Values JSON */}
          <div className="space-y-2 group">
            <h3 className="text-xs font-bold text-neutral-500 uppercase tracking-wider flex items-center justify-between">
              <span className="flex items-center gap-1">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span> New Values (Post-mutation)
              </span>
              {newValues && <CopyButton text={formatJson(newValues)} />}
            </h3>
            <pre className="bg-emerald-50/10 text-emerald-950 p-4 rounded-xl overflow-x-auto text-xs font-mono border border-emerald-100/60 max-h-[400px] leading-relaxed">
              {formatJson(newValues)}
            </pre>
          </div>
        </div>
      )}
    </div>
  )
}