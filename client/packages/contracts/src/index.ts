/**
 * @datarizen/contracts
 * Shared types for application structure, snapshot, compatibility.
 * Align with backend DTOs (ResolvedApplicationDto, ApplicationSnapshotDto, CompatibilityCheckResultDto).
 * See Compatibility and Versioning Framework for schema version usage.
 */

// --- Resolved application (from BFF resolve) ---
export interface ResolvedApplication {
  applicationReleaseId: string;
  tenantId: string;
  tenantSlug: string;
  environmentConfiguration?: Record<string, unknown>;
  /** When true, BFF should call TenantApplication for snapshot; otherwise AppBuilder */
  isTenantRelease?: boolean;
}

// --- Application snapshot (from BFF snapshot) ---
export interface ApplicationSnapshot {
  navigation?: NavigationNode | NavigationNode[];
  pages?: PageDefinition[];
  dataSources?: DataSourceDefinition[];
  entities?: EntityDefinition[];
  /** Per Compatibility and Versioning Framework; used by client adapters */
  schemaVersion?: string;
}

export interface NavigationNode {
  id: string;
  label?: string;
  path?: string;
  children?: NavigationNode[];
  [key: string]: unknown;
}

export interface PageDefinition {
  id: string;
  name?: string;
  layout?: Record<string, unknown>;
  components?: ComponentDefinition[];
  [key: string]: unknown;
}

export interface ComponentDefinition {
  id: string;
  type: string;
  props?: Record<string, unknown>;
  children?: ComponentDefinition[];
  [key: string]: unknown;
}

export interface DataSourceDefinition {
  id: string;
  type?: string;
  config?: Record<string, unknown>;
  [key: string]: unknown;
}

export interface EntityDefinition {
  id: string;
  name?: string;
  properties?: Record<string, unknown>;
  [key: string]: unknown;
}

// --- Semantic HTML (initial-view generation and runtime rendering) ---
export {
  HTML_ATTR_COMPONENT,
  HTML_ATTR_SLOT,
  SemanticComponentType,
  SEMANTIC_HTML_ATTRIBUTES,
} from './semantic-html.js';
export type { SemanticComponentTypeValue } from './semantic-html.js';

// --- Compatibility result (from BFF compatibility) ---
export interface CompatibilityResult {
  isCompatible: boolean;
  missingComponentTypes?: string[];
  incompatibleVersions?: Record<string, string>;
  errorMessage?: string;
  /** Supported schema versions for client adapter selection */
  supportedSchemaVersions?: string[];
}
