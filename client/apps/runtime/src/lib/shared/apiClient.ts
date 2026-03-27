/**
 * Shared API client for BFF calls. Reads base URL from env.
 * Sends X-Client-App: runtime so backend (Aspire logs) can attribute requests to this client.
 */

const CLIENT_APP = 'runtime';

export function getApiBaseUrl(): string {
  if (typeof import.meta.env !== 'undefined' && import.meta.env.VITE_API_BASE_URL) {
    return (import.meta.env.VITE_API_BASE_URL as string).replace(/\/$/, '');
  }
  return '';
}

function buildUrl(path: string): string {
  const base = getApiBaseUrl();
  return path.startsWith('http') ? path : `${base}${path.startsWith('/') ? '' : '/'}${path}`;
}

function defaultHeaders(method: string): Record<string, string> {
  const h: Record<string, string> = { 'X-Client-App': CLIENT_APP };
  if (method !== 'GET') h['Content-Type'] = 'application/json';
  return h;
}

function logRequest(method: string, url: string): void {
  console.info(`[Runtime API] ${method} ${url}`);
}

function logResponse(method: string, url: string, status: number, ok: boolean): void {
  console.info(`[Runtime API] ${method} ${url} → ${status} ${ok ? 'OK' : 'FAIL'}`);
}

function logError(method: string, url: string, err: unknown): void {
  console.error(`[Runtime API] ${method} ${url} → Failed to fetch:`, err);
}

export async function apiGet<T = unknown>(path: string): Promise<{ ok: true; data: T } | { ok: false; status: number }> {
  const url = buildUrl(path);
  logRequest('GET', url);
  try {
    const res = await fetch(url, { headers: defaultHeaders('GET') });
    logResponse('GET', url, res.status, res.ok);
    if (!res.ok) return { ok: false, status: res.status };
    const data = (await res.json()) as T;
    return { ok: true, data };
  } catch (e) {
    logError('GET', url, e);
    throw e;
  }
}

export async function apiPost<T = unknown, B = unknown>(
  path: string,
  body: B
): Promise<{ ok: true; data: T } | { ok: false; status: number }> {
  const url = buildUrl(path);
  logRequest('POST', url);
  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: defaultHeaders('POST'),
      body: JSON.stringify(body)
    });
    logResponse('POST', url, res.status, res.ok);
    if (!res.ok) return { ok: false, status: res.status };
    const data = (await res.json()) as T;
    return { ok: true, data };
  } catch (e) {
    logError('POST', url, e);
    throw e;
  }
}
