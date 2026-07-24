import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { Button } from './button';
import { Loader2 } from 'lucide-react';

interface DataTableExportProps {
  /**
   * Callback invoked when the user selects an export format.
   * Should return a promise that resolves when the export is complete.
   */
  onExport: (format: 'excel' | 'pdf') => Promise<void> | void;
  /**
   * Indicates whether an export operation is currently in progress.
   * When true, the dropdown trigger and items are disabled and a spinner is shown.
   */
  isExporting?: boolean;
}

/**
 * Reusable dropdown component for exporting a data table.
 *
 * It uses Radix UI's {@code DropdownMenu} primitives to ensure proper
 * accessibility semantics and keyboard navigation. The UI follows the
 * project's design system: the trigger is a small {@code Button} styled
 * with the "outline" variant, and each menu item calls {@code onExport}
 * with the corresponding format.
 */
export default function DataTableExport({
  onExport,
  isExporting = false,
}: DataTableExportProps) {
  const handleExport = async (format: 'excel' | 'pdf') => {
    if (isExporting) return;
    await onExport(format);
  };

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild disabled={isExporting}>
        <Button variant="outline" size="sm" className="h-8 flex items-center gap-1">
          {isExporting ? (
            <Loader2 className="w-4 h-4 animate-spin" />
          ) : (
            'Export'
          )}
        </Button>
      </DropdownMenu.Trigger>

      <DropdownMenu.Content
        sideOffset={4}
        align="end"
        className="z-50 min-w-[200px] rounded-md border bg-popover p-1 shadow-md outline-none"
      >
        <DropdownMenu.Item
          onSelect={(e) => {
            e.preventDefault();
            void handleExport('excel');
          }}
          disabled={isExporting}
          className="flex cursor-pointer select-none items-center rounded-sm px-2 py-1 text-sm outline-none focus:bg-accent focus:text-accent-foreground"
        >
          Export to Excel (.xlsx)
        </DropdownMenu.Item>
        <DropdownMenu.Item
          onSelect={(e) => {
            e.preventDefault();
            void handleExport('pdf');
          }}
          disabled={isExporting}
          className="flex cursor-pointer select-none items-center rounded-sm px-2 py-1 text-sm outline-none focus:bg-accent focus:text-accent-foreground"
        >
          Export to PDF (.pdf)
        </DropdownMenu.Item>
        <DropdownMenu.Arrow className="fill-popover" />
      </DropdownMenu.Content>
    </DropdownMenu.Root>
  );
}