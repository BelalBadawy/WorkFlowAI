import type { Table } from "@tanstack/react-table";
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
} from "lucide-react";

import { Button } from "./button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./select";

interface DataTablePaginationProps<TData> {
  table: Table<TData>;
}

export function DataTablePagination<TData>({
  table,
}: DataTablePaginationProps<TData>) {
  const pageIndex = table.getState().pagination.pageIndex;
  const pageCount = table.getPageCount();

  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    const siblingCount = 1;

    if (pageCount <= 5) {
      for (let i = 0; i < pageCount; i++) {
        pages.push(i);
      }
      return pages;
    }

    pages.push(0);

    const leftSiblingIndex = Math.max(pageIndex - siblingCount, 1);
    const rightSiblingIndex = Math.min(pageIndex + siblingCount, pageCount - 2);

    const shouldShowLeftDots = leftSiblingIndex > 1;
    const shouldShowRightDots = rightSiblingIndex < pageCount - 2;

    if (shouldShowLeftDots) {
      pages.push("ellipsis-left");
    }

    for (let i = leftSiblingIndex; i <= rightSiblingIndex; i++) {
      pages.push(i);
    }

    if (shouldShowRightDots) {
      pages.push("ellipsis-right");
    }

    pages.push(pageCount - 1);

    return pages;
  };

  const pages = getPageNumbers();

  return (
    <div className="flex items-center justify-between px-2">
      <div className="flex-1 text-sm text-neutral-500">
        {table.getFilteredSelectedRowModel().rows.length > 0 && (
          <>
            {table.getFilteredSelectedRowModel().rows.length} of{" "}
            {table.getFilteredRowModel().rows.length} row(s) selected.
          </>
        )}
      </div>
      <div className="flex items-center space-x-6 lg:space-x-8">
        <div className="flex items-center space-x-2">
          <p className="text-sm font-medium">Rows per page</p>
          <Select
            value={`${table.getState().pagination.pageSize}`}
            onValueChange={(value) => {
              table.setPageSize(Number(value));
            }}
          >
            <SelectTrigger className="h-8 w-[70px]">
              <SelectValue placeholder={table.getState().pagination.pageSize} />
            </SelectTrigger>
            <SelectContent side="top">
              {[10, 20, 30, 40, 50].map((pageSize) => (
                <SelectItem key={pageSize} value={`${pageSize}`}>
                  {pageSize}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex w-[100px] items-center justify-center text-sm font-medium">
          Page {pageIndex + 1} of {pageCount || 1}
        </div>
        <div className="flex items-center space-x-2">
          <Button
            variant="outline"
            className="hidden h-8 w-8 p-0 lg:flex"
            onClick={() => table.setPageIndex(0)}
            disabled={!table.getCanPreviousPage()}
            aria-label="Go to first page"
          >
            <span className="sr-only">Go to first page</span>
            <ChevronsLeft className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            className="h-8 w-8 p-0"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
            aria-label="Go to previous page"
          >
            <span className="sr-only">Go to previous page</span>
            <ChevronLeft className="h-4 w-4" />
          </Button>

          {/* Render Page Numbers */}
          <div className="hidden sm:flex items-center space-x-1">
            {pages.map((page, idx) => {
              if (typeof page === "string") {
                return (
                  <span
                    key={`${page}-${idx}`}
                    className="flex h-8 w-8 items-center justify-center text-sm text-neutral-400"
                    data-testid={page}
                  >
                    ...
                  </span>
                );
              }
              return (
                <Button
                  key={page}
                  variant={pageIndex === page ? "default" : "outline"}
                  className="h-8 w-8 p-0 text-sm"
                  onClick={() => table.setPageIndex(page)}
                  aria-label={`Go to page ${page + 1}`}
                  aria-current={pageIndex === page ? "page" : undefined}
                >
                  {page + 1}
                </Button>
              );
            })}
          </div>

          <Button
            variant="outline"
            className="h-8 w-8 p-0"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
            aria-label="Go to next page"
          >
            <span className="sr-only">Go to next page</span>
            <ChevronRight className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            className="hidden h-8 w-8 p-0 lg:flex"
            onClick={() => table.setPageIndex(pageCount - 1)}
            disabled={!table.getCanNextPage()}
            aria-label="Go to last page"
          >
            <span className="sr-only">Go to last page</span>
            <ChevronsRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}