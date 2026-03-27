<script lang="ts">
  import type { ResolvedApplication, ApplicationSnapshot, CompatibilityResult } from '@datarizen/contracts';
  import { resolveLoader, structureLoader, compatibilityLoader, initialViewHtmlLoader } from '$lib/loaders';
  import { RuntimeRenderer } from '$lib/renderer';
  import { isSchemaVersionSupported } from '$lib/versioning/schemaVersionAdapter';

  interface Props {
    data: { tenantSlug: string; appSlug: string; environment: string };
  }
  let { data }: Props = $props();

  let resolved = $state<ResolvedApplication | null>(null);
  let snapshot = $state<ApplicationSnapshot | null>(null);
  let compatibility = $state<CompatibilityResult | null>(null);
  /** When set, use server-rendered initial view HTML for first paint; otherwise fall back to JSON snapshot render. */
  let initialViewHtml = $state<string | null>(null);
  let loading = $state(true);
  let error = $state<string | null>(null);

  $effect(() => {
    const slug = data.tenantSlug;
    const app = data.appSlug;
    const env = data.environment ?? 'production';
    if (!slug || !app) {
      loading = false;
      return;
    }

    loading = true;
    error = null;
    resolved = null;
    snapshot = null;
    compatibility = null;
    initialViewHtml = null;

    (async () => {
      try {
        const r = await resolveLoader(slug, app, env);
        if (!r?.applicationReleaseId) {
          error = 'App not found';
          loading = false;
          return;
        }
        resolved = r;
        const releaseId = r.applicationReleaseId;
        const [snap, compat, html] = await Promise.all([
          structureLoader(releaseId),
          compatibilityLoader(releaseId),
          initialViewHtmlLoader(releaseId)
        ]);
        snapshot = snap;
        compatibility = compat;
        initialViewHtml = html;
        console.info('[Runtime page] Loaded:', {
          hasResolved: !!resolved,
          releaseId: resolved?.applicationReleaseId,
          hasSnapshot: !!snap,
          snapshotNavCount: Array.isArray(snap?.navigation) ? snap.navigation.length : snap?.navigation ? 1 : 0,
          snapshotNavDefined: snap?.navigation != null,
          initialViewHtmlLength: typeof html === 'string' ? html.length : 0,
          initialViewHtmlFalsy: !html
        });
        if (compatibility && !compatibility.isCompatible) {
          error = compatibility.errorMessage ?? 'This app is not compatible with the current runtime.';
        } else if (
          snapshot &&
          compatibility &&
          !isSchemaVersionSupported(snapshot.schemaVersion, compatibility.supportedSchemaVersions)
        ) {
          error = `Unsupported schema version (${snapshot.schemaVersion ?? 'unknown'}). Please upgrade the runtime client.`;
        }
      } catch (e) {
        error = e instanceof Error ? e.message : 'Failed to load app';
      } finally {
        loading = false;
      }
    })();
  });
</script>

{#if loading}
  <p class="loading">Loading app…</p>
{:else if error}
  <p class="error">{error}</p>
{:else if resolved && snapshot && compatibility?.isCompatible && isSchemaVersionSupported(snapshot.schemaVersion, compatibility.supportedSchemaVersions)}
  <RuntimeRenderer
    {snapshot}
    config={resolved}
    applicationReleaseId={resolved?.applicationReleaseId ?? null}
    {initialViewHtml}
  />
{:else}
  <p>Unable to load app.</p>
{/if}

<style>
  .loading, .error {
    padding: var(--datarizen-spacing-lg, 1.5rem);
  }
  .error {
    color: #dc2626;
  }
</style>
