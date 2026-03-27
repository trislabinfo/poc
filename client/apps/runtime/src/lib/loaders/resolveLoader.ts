import type { ResolvedApplication } from '@datarizen/contracts';
import { apiGet } from '$lib/shared';

/** Backend DTO shape (camelCase from BFF) */
interface ResolvedApplicationDto {
  tenantId: string;
  tenantSlug: string;
  tenantApplicationId?: string;
  appSlug?: string;
  applicationReleaseId: string;
  environmentConfiguration?: string;
  isTenantRelease?: boolean;
}

function mapToResolvedApplication(dto: ResolvedApplicationDto): ResolvedApplication {
  let environmentConfiguration: Record<string, unknown> | undefined;
  if (typeof dto.environmentConfiguration === 'string' && dto.environmentConfiguration) {
    try {
      environmentConfiguration = JSON.parse(dto.environmentConfiguration) as Record<string, unknown>;
    } catch {
      environmentConfiguration = undefined;
    }
  }
  return {
    applicationReleaseId: String(dto.applicationReleaseId),
    tenantId: String(dto.tenantId),
    tenantSlug: dto.tenantSlug ?? '',
    environmentConfiguration,
    isTenantRelease: dto.isTenantRelease,
  };
}

export async function resolveLoader(
  tenantSlug: string,
  appSlug: string,
  environment: string
): Promise<ResolvedApplication | null> {
  const url = `/api/runtime/resolve?tenantSlug=${encodeURIComponent(tenantSlug)}&appSlug=${encodeURIComponent(appSlug)}&environment=${encodeURIComponent(environment)}`;
  const result = await apiGet<ResolvedApplicationDto>(url);
  if (!result.ok) return null;
  return mapToResolvedApplication(result.data);
}
