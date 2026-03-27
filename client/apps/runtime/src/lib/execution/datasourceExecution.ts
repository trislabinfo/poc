import { apiPost } from '$lib/shared';

export interface DatasourceExecuteRequest {
  applicationReleaseId: string;
  datasourceId: string;
}

export interface DatasourceExecuteResult {
  data?: unknown;
  schemaVersion?: string;
}

/**
 * Execute a datasource via BFF. POST /api/runtime/datasource/execute
 */
export async function executeDatasource(
  applicationReleaseId: string,
  datasourceId: string
): Promise<{ ok: true; data: DatasourceExecuteResult } | { ok: false; status: number }> {
  return apiPost<DatasourceExecuteResult, DatasourceExecuteRequest>(
    '/api/runtime/datasource/execute',
    { applicationReleaseId, datasourceId }
  );
}
