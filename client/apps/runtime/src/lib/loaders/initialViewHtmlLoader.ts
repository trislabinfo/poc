import { getApiBaseUrl } from '$lib/shared';

const LOG = '[Runtime initial-view]';

/**
 * Fetches initial view HTML from BFF (GET /api/runtime/initial-view).
 * Returns HTML string when available; null on 404 or error (client falls back to JSON snapshot).
 */
export async function initialViewHtmlLoader(
  applicationReleaseId: string
): Promise<string | null> {
  if (!applicationReleaseId) {
    console.info(LOG, 'Skipped: no applicationReleaseId');
    return null;
  }
  const base = getApiBaseUrl();
  const path = `/api/runtime/initial-view?applicationReleaseId=${encodeURIComponent(applicationReleaseId)}`;
  const url = path.startsWith('http') ? path : `${base}${path.startsWith('/') ? '' : '/'}${path}`;
  console.info(LOG, 'GET', url);
  try {
    const res = await fetch(url, {
      headers: { Accept: 'text/html' }
    });
    const contentType = res.headers.get('Content-Type') ?? '';
    console.info(LOG, `Response ${res.status} Content-Type: ${contentType}`);
    if (!res.ok) {
      console.warn(LOG, 'Non-OK status, will fall back to snapshot');
      return null;
    }
    if (!contentType.includes('text/html')) {
      console.warn(LOG, 'Response is not text/html, will fall back to snapshot');
      return null;
    }
    const html = await res.text();
    console.info(LOG, `Got HTML length=${html?.length ?? 0} (${html?.length ? 'will render server HTML' : 'empty, will fall back to snapshot'})`);
    return html || null;
  } catch (e) {
    console.error(LOG, 'Fetch failed:', e);
    return null;
  }
}
