import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import type { Table } from '@tanstack/react-table';
import { DataTablePagination } from './DataTablePagination';

interface MockTable {
  getState: () => { pagination: { pageIndex: number; pageSize: number } };
  getPageCount: () => number;
  getFilteredSelectedRowModel: () => { rows: { length: number } };
  getFilteredRowModel: () => { rows: { length: number } };
  getCanPreviousPage: () => boolean;
  getCanNextPage: () => boolean;
  setPageIndex: (index: number) => void;
  setPageSize: (size: number) => void;
  previousPage: () => void;
  nextPage: () => void;
}

describe('DataTablePagination', () => {
  let tableMock: MockTable;

  beforeEach(() => {
    tableMock = {
      getState: vi.fn().mockReturnValue({
        pagination: {
          pageIndex: 1, // Page 2
          pageSize: 10,
        },
      }),
      getPageCount: vi.fn().mockReturnValue(5),
      getFilteredSelectedRowModel: vi.fn().mockReturnValue({
        rows: { length: 2 },
      }),
      getFilteredRowModel: vi.fn().mockReturnValue({
        rows: { length: 50 },
      }),
      getCanPreviousPage: vi.fn().mockReturnValue(true),
      getCanNextPage: vi.fn().mockReturnValue(true),
      setPageIndex: vi.fn(),
      setPageSize: vi.fn(),
      previousPage: vi.fn(),
      nextPage: vi.fn(),
    };
  });

  it('renders selection count and pagination status correctly', () => {
    render(<DataTablePagination table={tableMock as unknown as Table<unknown>} />);

    expect(screen.getByText('2 of 50 row(s) selected.')).toBeInTheDocument();
    expect(screen.getByText('Page 2 of 5')).toBeInTheDocument();
  });

  it('renders numeric page buttons with correct active state', () => {
    render(<DataTablePagination table={tableMock as unknown as Table<unknown>} />);

    const page2Button = screen.getByRole('button', { name: 'Go to page 2' });
    expect(page2Button).toBeInTheDocument();
    expect(page2Button).toHaveAttribute('aria-current', 'page');

    const page1Button = screen.getByRole('button', { name: 'Go to page 1' });
    expect(page1Button).toBeInTheDocument();
    expect(page1Button).not.toHaveAttribute('aria-current');
  });

  it('calls setPageIndex when page number is clicked', async () => {
    render(<DataTablePagination table={tableMock as unknown as Table<unknown>} />);

    const page3Button = screen.getByRole('button', { name: 'Go to page 3' });
    await userEvent.click(page3Button);

    expect(tableMock.setPageIndex).toHaveBeenCalledWith(2);
  });

  it('calls nextPage and previousPage when chevrons are clicked', async () => {
    render(<DataTablePagination table={tableMock as unknown as Table<unknown>} />);

    const nextButton = screen.getByRole('button', { name: 'Go to next page' });
    const prevButton = screen.getByRole('button', { name: 'Go to previous page' });

    await userEvent.click(nextButton);
    expect(tableMock.nextPage).toHaveBeenCalled();

    await userEvent.click(prevButton);
    expect(tableMock.previousPage).toHaveBeenCalled();
  });

  it('calls setPageIndex to 0 and pageCount-1 when first and last page buttons are clicked', async () => {
    render(<DataTablePagination table={tableMock as unknown as Table<unknown>} />);

    const firstButton = screen.getByRole('button', { name: 'Go to first page' });
    const lastButton = screen.getByRole('button', { name: 'Go to last page' });

    await userEvent.click(firstButton);
    expect(tableMock.setPageIndex).toHaveBeenCalledWith(0);

    await userEvent.click(lastButton);
    expect(tableMock.setPageIndex).toHaveBeenCalledWith(4);
  });
});