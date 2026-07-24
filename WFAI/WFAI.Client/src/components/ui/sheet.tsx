import * as React from "react"
import { X } from "lucide-react"
import { cn } from "@/lib/utils"

export interface SheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  children: React.ReactNode;
}

export function Sheet({ open, onOpenChange, children }: SheetProps) {
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
    <div className="fixed inset-0 z-50 flex justify-end">
      {/* Backdrop */}
      <div 
        className="fixed inset-0 bg-black/50 backdrop-blur-sm transition-opacity duration-300"
        onClick={() => onOpenChange(false)}
      />
      {/* Content */}
      <div className={cn(
        "relative w-full max-w-lg h-full bg-white p-6 shadow-2xl transition-all duration-300 dark:bg-neutral-900 border-l border-neutral-200 dark:border-neutral-800 overflow-y-auto",
        "animate-in slide-in-from-right duration-300"
      )}>
        <button
          onClick={() => onOpenChange(false)}
          className="absolute right-4 top-4 rounded-sm opacity-70 ring-offset-white transition-opacity hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-neutral-950 focus:ring-offset-2 disabled:pointer-events-none dark:ring-offset-neutral-950 dark:focus:ring-neutral-300"
        >
          <X className="h-5 w-5 text-neutral-500" />
          <span className="sr-only">Close</span>
        </button>
        {children}
      </div>
    </div>
  )
}

export function SheetHeader({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn("flex flex-col space-y-2 text-left mb-6", className)}>{children}</div>
}

export function SheetTitle({ children, className }: { children: React.ReactNode; className?: string }) {
  return <h2 className={cn("text-lg font-semibold text-neutral-950 dark:text-neutral-50", className)}>{children}</h2>
}

export function SheetDescription({ children, className }: { children: React.ReactNode; className?: string }) {
  return <p className={cn("text-sm text-neutral-500 dark:text-neutral-400", className)}>{children}</p>
}

export function SheetFooter({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn("flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-2 gap-2 mt-6", className)}>{children}</div>
}