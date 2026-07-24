import { useQuery } from '@tanstack/react-query';
import { auditLogsApi } from '../lib/audit-logs-api';
import type { AuditLogsFilterRequest } from '../lib/audit-logs-api';

export function useAuditLogs(params: AuditLogsFilterRequest) {
  return useQuery({
    queryKey: [
      'audit-logs',
      'list',
      params.pageNumber,
      params.pageSize,
      params.searchTerm,
      params.sortBy,
      params.sortDirection,
      params.tableName,
      params.entityId,
      params.actionTypes,
      params.fromDate,
      params.toDate,
      params.userId,
    ],
    queryFn: async () => {
      const response = await auditLogsApi.getPagedList(params);
      if (!response.isSuccessful) {
        throw new Error(response.messages[0] || 'Failed to retrieve audit logs.');
      }
      return response.data;
    },
    placeholderData: (previousData) => previousData,
  });
}