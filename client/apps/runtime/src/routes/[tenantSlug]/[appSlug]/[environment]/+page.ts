import type { LayoutLoad } from './$types';

export const load: LayoutLoad = ({ params }) => ({
  tenantSlug: params.tenantSlug ?? '',
  appSlug: params.appSlug ?? '',
  environment: params.environment ?? 'production',
});
