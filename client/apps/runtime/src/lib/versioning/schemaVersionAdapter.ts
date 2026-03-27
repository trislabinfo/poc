/**
 * Schema version support for runtime adapter selection.
 * Per Compatibility and Versioning Framework: client uses snapshot.schemaVersion and
 * compatibility.supportedSchemaVersions to decide whether to render or show "unsupported".
 */

const DEFAULT_SCHEMA_VERSION = '1.0';

/**
 * Returns whether the snapshot's schema version is supported by the runtime/client.
 * When backend does not send supportedSchemaVersions, we treat default "1.0" as supported.
 */
export function isSchemaVersionSupported(
  snapshotSchemaVersion: string | undefined,
  supportedSchemaVersions: string[] | undefined
): boolean {
  const version = snapshotSchemaVersion ?? DEFAULT_SCHEMA_VERSION;
  const supported = supportedSchemaVersions?.length ? supportedSchemaVersions : [DEFAULT_SCHEMA_VERSION];
  return supported.includes(version);
}

export { DEFAULT_SCHEMA_VERSION };
