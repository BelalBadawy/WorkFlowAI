import * as React from "react"
import { cn } from "@/lib/utils"

export interface DialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  children: React.ReactNode;
}

export function Dialog({ open, onOpenChange, children }: DialogProps) {
  React.useEffect(() => {
    if (open) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [open]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div 
        className="fixed inset-0 bg-black/50 backdrop-blur-sm transition-opacity duration-300"
        onClick={() => onOpenChange(false)}
      />
      {/* Content */}
      <div className="relative w-full max-w-lg transform rounded-2xl bg-white p-6 shadow-2xl transition-all duration-300 animate-in fade-in-0 zoom-in-95 dark:bg-neutral-900 border border-neutral-200 dark:border-neutral-800">
        {children}
      </div>
    </div>
  )
}

export function DialogContent({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn("space-y-4", className)}>{children}</div>
}

export function DialogHeader({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn("flex flex-col space-y-1.5 text-center sm:text-left", className)}>{children}</div>
}

export function DialogTitle({ children, className }: { children: React.ReactNode; className?: string }) {
  return <h2 className={cn("text-lg font-semibold leading-none tracking-tight text-neutral-950 dark:text-neutral-50", className)}>{children}</h2>
}

export function DialogDescription({ children, className }: { children: React.ReactNode; className?: string }) {
  return <p className={cn("text-sm text-neutral-500 dark:text-neutral-400", className)}>{children}</p>
}

export function DialogFooter({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn("flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-2 gap-2 mt-6", className)}>{children}</div>
}

export function DialogClose({ onClick, children, className }: { onClick: () => void; children: React.ReactNode; className?: string }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn("px-4 py-2 border border-neutral-200 rounded-xl text-sm font-semibold hover:bg-neutral-50 focus:outline-none transition-colors", className)}
    >
      {children}
    </button>
  )
}