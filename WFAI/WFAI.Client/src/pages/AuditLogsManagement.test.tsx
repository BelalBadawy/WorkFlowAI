import { render, screen, fireEvent } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import React from 'react'
import { MemoryRouter, Route, Routes, useSearchParams } from 'react-router-dom'
import AuditLogsManagement from './AuditLogsManagement'
import { useAuditLogs } from '../hooks/useAuditLogs'
import { useUserLookups } from '../hooks/useUsers'

vi.mock('../hooks/useAuditLogs', () => ({
  useAuditLogs: vi.fn(),
}))

vi.mock('../hooks/useUsers', () => ({
  useUserLookups: vi.fn(),
}))

const mockToast = {
  success: vi.fn(),
  error: vi.fn(),
  warning: vi.fn(),
}
vi.mock('../components/ui/toast', () => ({
  useToast: () => mockToast,
}))

vi.mock('../components/ui/date-picker', () => ({
  DatePicker: ({ date, setDate, placeholder }: { date?: Date; setDate: (d?: Date) => void; placeholder: string }) => (
    <input 
      type="text" 
      placeholder={placeholder}
      value={date ? date.toISOString().split('T')[0] : ''} 
      onChange={(e) => setDate(e.target.value ? new Date(e.target.value) : undefined)} 
      data-testid={`date-picker-${placeholder.toLowerCase().replace(' ', '-')}`}
    />
  )
}))

vi.mock('../components/ui/select', () => ({
  Select: ({ children, value, onValueChange, 'data-testid': testId }: { children: React.ReactNode; value: string; onValueChange: (val: string) => void; 'data-testid'?: string }) => (
    <select 
      value={value} 
      onChange={(e) => onValueChange(e.target.value)} 
      data-testid={testId || "select-mock"}
    >
      {children}
    </select>
  ),
  SelectTrigger: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SelectValue: ({ placeholder }: { placeholder?: string }) => <>{placeholder}</>,
  SelectContent: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SelectItem: ({ value, children }: { value: string; children: React.ReactNode }) => (
    <option value={value}>{children}</option>
  ),
}))

describe('AuditLogsManagement Component', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    vi.mocked(useAuditLogs).mockReturnValue({
      data: {
        data: [
          {
            id: 1,
            userId: 100,
            userEmail: 'actor@domain.com',
            ipAddress: '127.0.0.1',
            type: 'Create',
            tableName: 'Category',
            dateTime: '2026-06-05T12:00:00Z',
            oldValues: null,
            newValues: '{"Name":"Test"}',
            affectedColumns: '["Name"]',
            primaryKey: '{"Id":5}',
          }
        ],
        totalCount: 1,
        currentPage: 1,
        pageSize: 10,
      },
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useAuditLogs>)

    vi.mocked(useUserLookups).mockReturnValue({
      data: [
        { id: 10, fullName: 'User Ten', email: 'ten@domain.com' },
        { id: 11, fullName: 'User Eleven', email: 'eleven@domain.com' },
      ],
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useUserLookups>)
  })

  it('hydrates initial search state from URL query parameters', async () => {
    render(
      <MemoryRouter initialEntries={['/admin/audit-logs?search=test-query']}>
        <Routes>
          <Route path="/admin/audit-logs" element={<AuditLogsManagement />} />
        </Routes>
      </MemoryRouter>
    )

    const input = screen.getByPlaceholderText('Search Actor Email or IP Address...') as HTMLInputElement
    expect(input.value).toBe('test-query')

    expect(useAuditLogs).toHaveBeenCalledWith(
      expect.objectContaining({ searchTerm: 'test-query' })
    )
  })

  it('updates local input immediately but does NOT update URL until Apply Filters is clicked', async () => {
    let urlSearch = ''
    const handleUrlChange = vi.fn((val) => {
      urlSearch = val
    })

    const UrlObserver = () => {
      const [searchParams] = useSearchParams()
      React.useEffect(() => {
        handleUrlChange(searchParams.get('search') || '')
      }, [searchParams])
      return null
    }

    render(
      <MemoryRouter initialEntries={['/admin/audit-logs']}>
        <UrlObserver />
        <Routes>
          <Route path="/admin/audit-logs" element={<AuditLogsManagement />} />
        </Routes>
      </MemoryRouter>
    )

    const input = screen.getByPlaceholderText('Search Actor Email or IP Address...') as HTMLInputElement
    expect(input.value).toBe('')

    // Simulate typing
    fireEvent.change(input, { target: { value: 'audit' } })

    // Input state changes immediately
    expect(input.value).toBe('audit')

    // URL parameter is not updated instantly
    expect(urlSearch).toBe('')

    // Click Apply Filters button
    const applyButton = screen.getByRole('button', { name: /Apply Filters/i })
    fireEvent.click(applyButton)

    // URL parameter is now updated
    expect(urlSearch).toBe('audit')
  })

  it('updates URL parameters only when Apply Filters is clicked for table name, action types, or dates', async () => {
    let urlParams: Record<string, string> = {}
    const handleUrlChange = vi.fn((params) => {
      urlParams = params
    })

    const UrlObserver = () => {
      const [searchParams] = useSearchParams()
      React.useEffect(() => {
        const params: Record<string, string> = {}
        searchParams.forEach((val, key) => {
          params[key] = val
        })
        handleUrlChange(params)
      }, [searchParams])
      return null
    }

    render(
      <MemoryRouter initialEntries={['/admin/audit-logs']}>
        <UrlObserver />
        <Routes>
          <Route path="/admin/audit-logs" element={<AuditLogsManagement />} />
        </Routes>
      </MemoryRouter>
    )

    // 1. Table Name Select
    const select = screen.getByTestId('table-name-select')
    fireEvent.change(select, { target: { value: 'Category' } })
    expect(urlParams.tableName).toBeUndefined() // Should not update immediately

    // 2. Action Types (Create, Update, Delete buttons)
    const createButton = screen.getByRole('button', { name: 'Create' })
    fireEvent.click(createButton)
    expect(urlParams.actionTypes).toBeUndefined() // Should not update immediately

    // 3. Date Inputs (using mocked DatePicker text inputs)
    const fromInput = screen.getByTestId('date-picker-from-date')
    const toInput = screen.getByTestId('date-picker-to-date')

    // Change From Date
    fireEvent.change(fromInput, { target: { value: '2026-06-01' } })
    expect(urlParams.fromDate).toBeUndefined()

    // Change To Date
    fireEvent.change(toInput, { target: { value: '2026-06-05' } })
    expect(urlParams.toDate).toBeUndefined()

    // Click Apply Filters
    const applyButton = screen.getByRole('button', { name: /Apply Filters/i })
    fireEvent.click(applyButton)

    // Verify all URL parameters updated together
    expect(urlParams.tableName).toBe('Category')
    expect(urlParams.actionTypes).toBe('Create')
    expect(urlParams.fromDate).toBe('2026/06/01')
    expect(urlParams.toDate).toBe('2026/06/05')
  })

  it('updates Entity ID input locally immediately and propagates to URL only on Apply click', async () => {
    let urlEntityId = ''
    const handleUrlChange = vi.fn((val) => {
      urlEntityId = val
    })

    const UrlObserver = () => {
      const [searchParams] = useSearchParams()
      React.useEffect(() => {
        handleUrlChange(searchParams.get('entityId') || '')
      }, [searchParams])
      return null
    }

    render(
      <MemoryRouter initialEntries={['/admin/audit-logs']}>
        <UrlObserver />
        <Routes>
          <Route path="/admin/audit-logs" element={<AuditLogsManagement />} />
        </Routes>
      </MemoryRouter>
    )

    const input = screen.getByPlaceholderText('Search by Entity ID...') as HTMLInputElement
    expect(input.value).toBe('')

    // Simulate typing
    fireEvent.change(input, { target: { value: '5' } })

    // Input changes immediately
    expect(input.value).toBe('5')

    // URL parameter is not updated instantly
    expect(urlEntityId).toBe('')

    // Click Apply Filters
    const applyButton = screen.getByRole('button', { name: /Apply Filters/i })
    fireEvent.click(applyButton)

    expect(urlEntityId).toBe('5')
  })

  it('hydrates initial Entity ID state from URL query parameters', async () => {
    render(
      <MemoryRouter initialEntries={['/admin/audit-logs?entityId=123']}>
        <Routes>
          <Route path="/admin/audit-logs" element={<AuditLogsManagement />} />
        </Routes>
      </MemoryRouter>
    )

    const input = screen.getByPlaceholderText('Search by Entity ID...') as HTMLInputElement
    expect(input.value).toBe('123')

    expect(useAuditLogs).toHaveBeenCalledWith(
      expect.objectContaining({ entityId: '123' })
    )
  })

  it('updates URL parameter only on Apply when a user is selected from dropdown', async () => {
    let urlParams: Record<string, string> = {}
    const handleUrlChange = vi.fn((params) => {
      urlParams = params
    })

    const UrlObserver = () => {
      const [searchParams] = useSearchParams()
      React.useEffect(() => {
        const params: Record<string, string> = {}
        searchParams.forEach((val, key) => {
          params[key] = val
        })
        handleUrlChange(params)
      }, [searchParams])
      return null
    }

    render(
      <MemoryRouter initialEntries={['/admin/audit-logs']}>
        <UrlObserver />
        <Routes>
          <Route path="/admin/audit-logs" element={<AuditLogsManagement />} />
        </Routes>
      </MemoryRouter>
    )

    const select = screen.getByTestId('user-select')
    fireEvent.change(select, { target: { value: '11' } })
    expect(urlParams.userId).toBeUndefined()

    // Click Apply Filters
    const applyButton = screen.getByRole('button', { name: /Apply Filters/i })
    fireEvent.click(applyButton)

    expect(urlParams.userId).toBe('11')
  })

  it('resets all filters when the Reset Filters button is clicked and updates URL immediately', async () => {
    let urlParams: Record<string, string> = {}
    const handleUrlChange = vi.fn((params) => {
      urlParams = params
    })

    const UrlObserver = () => {
      const [searchParams] = useSearchParams()
      React.useEffect(() => {
        const params: Record<string, string> = {}
        searchParams.forEach((val, key) => {
          params[key] = val
        })
        handleUrlChange(params)
      }, [searchParams])
      return null
    }

    render(
      <MemoryRouter initialEntries={['/admin/audit-logs?search=test&entityId=5&tableName=Category&actionTypes=Create&fromDate=2026/06/01&userId=10']}>
        <UrlObserver />
        <Routes>
          <Route path="/admin/audit-logs" element={<AuditLogsManagement />} />
        </Routes>
      </MemoryRouter>
    )

    // Verify initial values
    const searchInput = screen.getByPlaceholderText('Search Actor Email or IP Address...') as HTMLInputElement
    const entityInput = screen.getByPlaceholderText('Search by Entity ID...') as HTMLInputElement
    expect(searchInput.value).toBe('test')
    expect(entityInput.value).toBe('5')

    // Click Reset
    const resetButton = screen.getByRole('button', { name: /Reset Filters/i })
    fireEvent.click(resetButton)

    // Check that state resets
    expect(searchInput.value).toBe('')
    expect(entityInput.value).toBe('')
    expect(urlParams).toEqual({})
  })

  it('shows an error toast and prevents applying when To Date is earlier than From Date', async () => {
    let urlParams: Record<string, string> = {}
    const handleUrlChange = vi.fn((params) => {
      urlParams = params
    })

    const UrlObserver = () => {
      const [searchParams] = useSearchParams()
      React.useEffect(() => {
        const params: Record<string, string> = {}
        searchParams.forEach((val, key) => {
          params[key] = val
        })
        handleUrlChange(params)
      }, [searchParams])
      return null
    }

    render(
      <MemoryRouter initialEntries={['/admin/audit-logs']}>
        <UrlObserver />
        <Routes>
          <Route path="/admin/audit-logs" element={<AuditLogsManagement />} />
        </Routes>
      </MemoryRouter>
    )

    const fromInput = screen.getByTestId('date-picker-from-date')
    const toInput = screen.getByTestId('date-picker-to-date')

    // Change From Date to 2026-06-05
    fireEvent.change(fromInput, { target: { value: '2026-06-05' } })
    // Change To Date to 2026-06-01 (earlier than from date)
    fireEvent.change(toInput, { target: { value: '2026-06-01' } })

    // Reset calls on mockToast
    mockToast.error.mockClear()

    // Click Apply Filters
    const applyButton = screen.getByRole('button', { name: /Apply Filters/i })
    fireEvent.click(applyButton)

    // Should call toast error
    expect(mockToast.error).toHaveBeenCalledWith('To Date cannot be before From Date')
    // Should NOT update URL
    expect(urlParams.fromDate).toBeUndefined()
    expect(urlParams.toDate).toBeUndefined()
  })
})