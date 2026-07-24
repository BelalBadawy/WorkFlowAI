import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from "../ui/dialog"
import { Button } from "../ui/button"
import { AlertTriangle, Loader2 } from "lucide-react"

interface StatusConfirmationDialogProps {
  readonly isOpen: boolean;
  readonly onClose: () => void;
  readonly onConfirm: () => void;
  readonly entityName: string;
  readonly entityType: string; // "user" or "category"
  readonly action: 'activate' | 'deactivate';
  readonly isLoading?: boolean;
}

export function StatusConfirmationDialog({
  isOpen,
  onClose,
  onConfirm,
  entityName,
  entityType,
  action,
  isLoading = false,
}: StatusConfirmationDialogProps) {
  const capitalize = (str: string) => str.charAt(0).toUpperCase() + str.slice(1);
  const actionText = capitalize(action);
  const entityTypeText = capitalize(entityType);

  const getConsequenceText = () => {
    if (entityType.toLowerCase() === 'user') {
      return action === 'deactivate' 
        ? "They will no longer be able to perform operations."
        : "";
    }
    if (entityType.toLowerCase() === 'category') {
      return action === 'deactivate'
        ? "Products mapped to this category may hide or lose visibility in catalogs."
        : "It will become visible in product catalogs.";
    }
    return "";
  };

  const confirmButtonClass = action === 'activate'
    ? "bg-emerald-600 hover:bg-emerald-700 text-white font-bold px-5 py-2 rounded-xl border-transparent"
    : "bg-rose-600 hover:bg-rose-700 text-white font-bold px-5 py-2 rounded-xl border-transparent";

  const consequence = getConsequenceText();

  return (
    <Dialog open={isOpen} onOpenChange={(open) => { if (!open) onClose(); }}>
      <DialogContent className="max-w-md bg-white p-6 rounded-2xl border border-neutral-200 shadow-2xl">
        <DialogHeader>
          <DialogTitle className="text-neutral-900 font-extrabold flex items-center gap-2 capitalize">
            <AlertTriangle className="w-5 h-5 text-amber-500" /> {actionText} {entityTypeText}
          </DialogTitle>
          <DialogDescription className="text-neutral-500 text-sm mt-2">
            Are you sure you want to {action} the {entityType} '{entityName}'?{consequence ? ` ${consequence}` : ""}
          </DialogDescription>
        </DialogHeader>

        <DialogFooter className="flex justify-end gap-2 mt-6">
          <DialogClose onClick={onClose} className="border-neutral-200 text-neutral-600 hover:bg-neutral-100 font-bold">
            Cancel
          </DialogClose>
          <Button
            type="button"
            disabled={isLoading}
            onClick={onConfirm}
            className={confirmButtonClass}
          >
            {isLoading && <Loader2 className="w-4 h-4 animate-spin mr-1.5 inline" />}
            {actionText} {entityTypeText}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}