/**
 * Shared API client for gateway/backend calls.
 * - When VITE_API_BASE_URL is set (e.g. by Aspire), requests go to that URL.
 * - When not set (e.g. dashboard run standalone), use relative paths so Vite proxy forwards /api to the gateway.
 * Uses credentials: 'include' so auth cookies are sent when calling the gateway.
 * Sends X-Client-App: dashboard so backend (Aspire logs) can attribute requests to this client.
 */

const CLIENT_APP = 'dashboard';

export function getApiBaseUrl(): string {
  if (typeof import.meta.env !== 'undefined' && import.meta.env.VITE_API_BASE_URL) {
    return (import.meta.env.VITE_API_BASE_URL as string).replace(/\/$/, '');
  }
  return '';
}

function buildUrl(path: string): string {
  const base = getApiBaseUrl();
  if (path.startsWith('http')) return path;
  return base ? `${base}${path.startsWith('/') ? '' : '/'}${path}` : path.startsWith('/') ? path : `/${path}`;
}

function defaultHeaders(method: string): Record<string, string> {
  const h: Record<string, string> = { 'X-Client-App': CLIENT_APP };
  if (method !== 'GET') h['Content-Type'] = 'application/json';
  return h;
}

function logRequest(method: string, url: string): void {
  console.info(`[Dashboard API] ${method} ${url}`);
}

function logResponse(method: string, url: string, status: number, ok: boolean): void {
  console.info(`[Dashboard API] ${method} ${url} → ${status} ${ok ? 'OK' : 'FAIL'}`);
}

function logError(method: string, url: string, err: unknown): void {
  console.error(`[Dashboard API] ${method} ${url} → Failed to fetch:`, err);
}

async function parseJsonOrEmpty<T>(res: Response): Promise<T | null> {
  const text = await res.text();
  if (!text || res.status === 204) return null as T;
  try {
    return JSON.parse(text) as T;
  } catch {
    return null as T;
  }
}

export async function apiGet<T = unknown>(path: string): Promise<{ ok: true; data: T } | { ok: false; status: number }> {
  const url = buildUrl(path);
  logRequest('GET', url);
  try {
    const res = await fetch(url, {
      credentials: 'include',
      headers: defaultHeaders('GET')
    });
    logResponse('GET', url, res.status, res.ok);
    if (!res.ok) return { ok: false, status: res.status };
    const data = (await parseJsonOrEmpty<T>(res)) ?? ({} as T);
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
      body: JSON.stringify(body),
      credentials: 'include'
    });
    logResponse('POST', url, res.status, res.ok);
    if (!res.ok) return { ok: false, status: res.status };
    const data = (await parseJsonOrEmpty<T>(res)) ?? ({} as T);
    return { ok: true, data };
  } catch (e) {
    logError('POST', url, e);
    throw e;
  }
}

/** Full HTTP response (status, headers, body as text) for debugging and user feedback. */
export interface FullHttpResponse {
  status: number;
  statusText: string;
  headers: Record<string, string>;
  body: string;
}

/**
 * POST and return the full response (status, headers, body) so the UI can show exactly what the gateway returned.
 * Use for flows like tenant create where 204 or other codes need to be explained to the user.
 */
export async function apiPostFullResponse(path: string, body: unknown): Promise<FullHttpResponse> {
  const url = buildUrl(path);
  logRequest('POST', url);
  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: defaultHeaders('POST'),
      body: JSON.stringify(body),
      credentials: 'include'
    });
    logResponse('POST', url, res.status, res.ok);
    const headers: Record<string, string> = {};
    res.headers.forEach((value, key) => {
      headers[key] = value;
    });
    const bodyText = await res.text();
    return {
      status: res.status,
      statusText: res.statusText,
      headers,
      body: bodyText || '(empty body)'
    };
  } catch (e) {
    logError('POST', url, e);
    throw e;
  }
}
