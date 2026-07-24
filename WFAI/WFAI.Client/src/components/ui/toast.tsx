import React, { createContext, useContext, useState, useCallback, useEffect, useMemo } from 'react'
import { CheckCircle2, AlertCircle, Info, AlertTriangle, X } from 'lucide-react'

export type ToastType = 'success' | 'error' | 'info' | 'warning'

export interface ToastMessage {
  id: string
  type: ToastType
  message: string
}

interface ToastContextType {
  toast: {
    success: (message: string) => void
    error: (message: string) => void
    info: (message: string) => void
    warning: (message: string) => void
  }
}

const ToastContext = createContext<ToastContextType | undefined>(undefined)

export const useToast = () => {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return context.toast
}

export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [toasts, setToasts] = useState<ToastMessage[]>([])

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const addToast = useCallback((type: ToastType, message: string) => {
    const id = Math.random().toString(36).substring(2, 9)
    setToasts((prev) => [...prev, { id, type, message }])
  }, [])

  const toast = useMemo(() => ({
    success: (msg: string) => addToast('success', msg),
    error: (msg: string) => addToast('error', msg),
    info: (msg: string) => addToast('info', msg),
    warning: (msg: string) => addToast('warning', msg),
  }), [addToast])

  return (
    <ToastContext.Provider value={{ toast }}>
      {children}
      
      {/* Toast Portal/Container */}
      <div className="fixed top-5 right-5 z-[9999] flex flex-col gap-3 w-full max-w-[400px] pointer-events-none">
        {toasts.map((t) => (
          <ToastItem key={t.id} toast={t} onClose={removeToast} />
        ))}
      </div>
    </ToastContext.Provider>
  )
}

const ToastItem: React.FC<{ toast: ToastMessage; onClose: (id: string) => void }> = ({
  toast,
  onClose,
}) => {
  const [isExiting, setIsExiting] = useState(false)

  useEffect(() => {
    const autoDismissTimer = setTimeout(() => {
      handleClose()
    }, 4000)

    return () => clearTimeout(autoDismissTimer)
  }, [])

  const handleClose = () => {
    setIsExiting(true)
    setTimeout(() => {
      onClose(toast.id)
    }, 300) // matches animation duration
  }

  const icons = {
    success: <CheckCircle2 className="w-5 h-5 text-emerald-500 shrink-0" />,
    error: <AlertCircle className="w-5 h-5 text-rose-500 shrink-0" />,
    info: <Info className="w-5 h-5 text-blue-500 shrink-0" />,
    warning: <AlertTriangle className="w-5 h-5 text-amber-500 shrink-0" />,
  }

  const borderColors = {
    success: 'border-emerald-500/25 bg-emerald-50/90 text-emerald-950',
    error: 'border-rose-500/25 bg-rose-50/90 text-rose-950',
    info: 'border-blue-500/25 bg-blue-50/90 text-blue-950',
    warning: 'border-amber-500/25 bg-amber-50/90 text-amber-950',
  }

  return (
    <div
      className={`pointer-events-auto flex items-start gap-3 p-4 bg-white/80 backdrop-blur-md rounded-2xl border shadow-[0_8px_30px_rgba(0,0,0,0.06)] transition-all duration-300 ${
        borderColors[toast.type]
      } ${
        isExiting
          ? 'animate-out fade-out slide-out-to-right-10 duration-300'
          : 'animate-in fade-in slide-in-from-right-10 duration-300'
      }`}
      role="alert"
    >
      {icons[toast.type]}
      <div className="flex-grow text-xs md:text-sm font-medium leading-relaxed">
        {toast.message}
      </div>
      <button
        onClick={handleClose}
        className="text-neutral-400 hover:text-neutral-600 transition-colors p-0.5 shrink-0"
      >
        <X className="w-4 h-4" />
      </button>
    </div>
  )
}