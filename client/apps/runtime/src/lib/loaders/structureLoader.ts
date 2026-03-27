import type { ApplicationSnapshot, NavigationNode, PageDefinition, DataSourceDefinition, EntityDefinition } from '@datarizen/contracts';
import { apiGet } from '$lib/shared';

/** Backend DTO shape (camelCase from BFF): snapshot fields are JSON strings */
interface ApplicationSnapshotDto {
  navigationJson?: string;
  pageJson?: string;
  dataSourceJson?: string;
  entityJson?: string;
  schemaVersion?: string | null;
}

/** Server may send navigation as entities; support camelCase and PascalCase from .NET JSON. */
interface ServerNavItem {
  id?: string;
  Id?: string;
  label?: string;
  name?: string;
  Name?: string;
  path?: string;
  Path?: string;
  configurationJson?: string;
  ConfigurationJson?: string;
  children?: ServerNavItem[];
  Children?: ServerNavItem[];
  [key: string]: unknown;
}

function parseJson<T>(json: string | undefined, fallback: T): T {
  if (json == null || json === '') return fallback;
  try {
    const parsed = JSON.parse(json) as T;
    return parsed ?? fallback;
  } catch {
    return fallback;
  }
}

function getPathFromConfig(configurationJson: string | undefined): string | undefined {
  if (!configurationJson) return undefined;
  try {
    const c = JSON.parse(configurationJson) as { path?: string; Path?: string };
    return c?.path ?? c?.Path;
  } catch {
    return undefined;
  }
}

function getStr(item: ServerNavItem, ...keys: string[]): string | undefined {
  for (const k of keys) {
    const v = item[k];
    if (typeof v === 'string' && v) return v;
  }
  return undefined;
}

function toNavigationNode(item: ServerNavItem): NavigationNode {
  const idVal = getStr(item, 'id', 'Id') ?? (item.id != null ? String(item.id) : item.Id != null ? String(item.Id) : '');
  const label = getStr(item, 'label', 'name', 'Name') ?? (idVal || undefined);
  const path = getStr(item, 'path', 'Path') ?? getPathFromConfig(getStr(item, 'configurationJson', 'ConfigurationJson'));
  const rawChildren = item.children ?? item.Children;
  const children = Array.isArray(rawChildren)
    ? rawChildren.map(toNavigationNode)
    : undefined;
  return {
    id: idVal || 'unknown',
    ...(label != null && { label }),
    ...(path != null && { path }),
    ...(children != null && children.length > 0 && { children })
  };
}

function normalizeSnapshotNavigation(raw: NavigationNode | NavigationNode[] | ServerNavItem | ServerNavItem[]): NavigationNode[] {
  if (raw == null) return [];
  const arr = Array.isArray(raw) ? raw : [raw];
  if (arr.length === 0) return [];
  return arr.map((item) => {
    const node = item as ServerNavItem;
    if (node.label != null && node.path != null) return item as NavigationNode;
    return toNavigationNode(node);
  });
}

const LOG = '[Runtime snapshot]';

export async function structureLoader(applicationReleaseId: string): Promise<ApplicationSnapshot | null> {
  const url = `/api/runtime/snapshot?applicationReleaseId=${encodeURIComponent(applicationReleaseId)}`;
  const result = await apiGet<ApplicationSnapshotDto>(url);
  if (!result.ok) return null;
  const dto = result.data;
  const rawNav = parseJson<NavigationNode | NavigationNode[] | ServerNavItem | ServerNavItem[]>(dto.navigationJson, []);
  const navigation = normalizeSnapshotNavigation(rawNav);
  const navJsonLen = dto.navigationJson?.length ?? 0;
  const rawIsArray = Array.isArray(rawNav);
  const rawArr = rawIsArray ? (rawNav as unknown[]) : rawNav ? [rawNav] : [];
  const rawLen = rawArr.length;
  const firstRaw = rawArr[0] as Record<string, unknown> | undefined;
  const rawKeys = firstRaw ? Object.keys(firstRaw) : [];
  console.info(LOG, `navigationJson length=${navJsonLen}, rawLen=${rawLen}, normalized nodes=${navigation.length}. Raw first item keys: [${rawKeys.join(', ')}]`);
  if (firstRaw) {
    console.info(LOG, 'Raw first item sample:', rawKeys.slice(0, 8).reduce((acc, k) => ({ ...acc, [k]: firstRaw[k] }), {}));
  }
  if (navigation.length > 0) {
    const first = navigation[0];
    console.info(LOG, 'Normalized first nav node:', { id: first?.id, label: (first as NavigationNode)?.label, path: (first as NavigationNode)?.path });
  } else if (navJsonLen > 0) {
    console.warn(LOG, 'Normalized to 0 nodes. navigationJson preview:', dto.navigationJson?.slice(0, 300));
  }
  const pages = parseJson<PageDefinition[]>(dto.pageJson, []);
  const dataSources = parseJson<DataSourceDefinition[]>(dto.dataSourceJson, []);
  const entities = parseJson<EntityDefinition[]>(dto.entityJson, []);
  return {
    navigation: navigation.length > 0 ? navigation : undefined,
    pages,
    dataSources,
    entities,
    schemaVersion: dto.schemaVersion ?? undefined,
  };
}
