import type { CompatibilityResult } from '@datarizen/contracts';
import { apiGet } from '$lib/shared';

/** Backend DTO shape (camelCase from BFF) */
interface CompatibilityCheckResultDto {
  isCompatible: boolean;
  missingComponentTypes?: string[];
  incompatibleVersions?: string[] | Record<string, string>;
  errorMessage?: string | null;
  supportedSchemaVersions?: string[];
}

function mapToCompatibilityResult(dto: CompatibilityCheckResultDto): CompatibilityResult {
  let incompatibleVersions: Record<string, string> | undefined;
  if (Array.isArray(dto.incompatibleVersions)) {
    incompatibleVersions = {};
    dto.incompatibleVersions.forEach((v, i) => {
      incompatibleVersions![`item${i}`] = v;
    });
  } else if (dto.incompatibleVersions && typeof dto.incompatibleVersions === 'object') {
    incompatibleVersions = dto.incompatibleVersions as Record<string, string>;
  }
  return {
    isCompatible: dto.isCompatible,
    missingComponentTypes: dto.missingComponentTypes,
    incompatibleVersions,
    errorMessage: dto.errorMessage ?? undefined,
    supportedSchemaVersions: dto.supportedSchemaVersions,
  };
}

export async function compatibilityLoader(
  applicationReleaseId: string,
  runtimeVersionId?: string
): Promise<CompatibilityResult | null> {
  let url = `/api/runtime/compatibility?applicationReleaseId=${encodeURIComponent(applicationReleaseId)}`;
  if (runtimeVersionId) url += `&runtimeVersionId=${encodeURIComponent(runtimeVersionId)}`;
  const result = await apiGet<CompatibilityCheckResultDto>(url);
  if (!result.ok) return null;
  return mapToCompatibilityResult(result.data);
}
