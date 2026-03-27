<script lang="ts">
  import type { ApplicationSnapshot, NavigationNode, ResolvedApplication } from '@datarizen/contracts';
  import { executeDatasource } from '$lib/execution/datasourceExecution';

  interface Props {
    snapshot: ApplicationSnapshot;
    /** Optional config from resolve (tenant, environment) */
    config?: ResolvedApplication | null;
    /** Application release ID for execution (datasource run). */
    applicationReleaseId?: string | null;
    /** When set, render server-generated initial view HTML for first paint; otherwise render from snapshot. */
    initialViewHtml?: string | null;
  }
  let { snapshot, config, applicationReleaseId, initialViewHtml = null }: Props = $props();

  const navNodes = $derived(normalizeNavigation(snapshot.navigation));
  const dataSources = $derived(snapshot.dataSources ?? []);

  let executionResult = $state<unknown>(null);
  let executionError = $state<string | null>(null);
  let executing = $state(false);

  function normalizeNavigation(nav: ApplicationSnapshot['navigation']): NavigationNode[] {
    if (nav == null) return [];
    return Array.isArray(nav) ? nav : [nav];
  }

  $effect(() => {
    const useHtml = !!initialViewHtml;
    const count = navNodes.length;
    const htmlLen = typeof initialViewHtml === 'string' ? initialViewHtml.length : 0;
    const snippet = typeof initialViewHtml === 'string' && initialViewHtml.length > 0
      ? initialViewHtml.slice(0, 500).replace(/\s+/g, ' ')
      : '';
    console.info('[RuntimeRenderer]', {
      useInitialViewHtml: useHtml,
      initialViewHtmlLength: htmlLen,
      navNodesFromSnapshot: count,
      snapshotNavigationType: Array.isArray(snapshot.navigation) ? 'array' : snapshot.navigation ? 'single' : 'undefined',
      ...(useHtml && htmlLen > 0 ? {
        htmlSnippet: snippet + (initialViewHtml!.length > 500 ? '...' : ''),
        hasNavigationRoot: initialViewHtml!.includes('navigation-root'),
        hasDataLabel: initialViewHtml!.includes('data-label')
      } : {})
    });
    if (!useHtml && count === 0 && snapshot.navigation != null) {
      console.warn('[RuntimeRenderer] Snapshot has navigation but navNodes is empty. snapshot.navigation:', snapshot.navigation);
    }
  });

  async function runDatasource(id: string) {
    if (!applicationReleaseId) return;
    executing = true;
    executionResult = null;
    executionError = null;
    try {
      const res = await executeDatasource(applicationReleaseId, id);
      if (res.ok) executionResult = res.data;
      else executionError = `Request failed: ${res.status}`;
    } catch (e) {
      executionError = e instanceof Error ? e.message : 'Execution failed';
    } finally {
      executing = false;
    }
  }
</script>

<div class="runtime-app">
  {#if initialViewHtml}
    <!-- Server-rendered initial view (navigation + main content); semantic HTML from BFF -->
    <div class="runtime-initial-html" data-initial-view="server">
      {@html initialViewHtml}
    </div>
  {:else}
  <!-- Left sidebar: application navigation (Gmail-style) from navigation entity -->
  {#if navNodes.length > 0}
    <aside class="runtime-nav-sidebar" aria-label="Application navigation">
      <nav class="runtime-nav">
        <ul class="runtime-nav-list">
          {#each navNodes as node (node.id)}
            <li class="runtime-nav-item">
              {#if node.path}
                <a href={node.path} class="runtime-nav-link">{node.label ?? node.id}</a>
              {:else}
                <span class="runtime-nav-label">{node.label ?? node.id}</span>
              {/if}
              {#if node.children && node.children.length > 0}
                <ul class="runtime-nav-children">
                  {#each node.children as child (child.id)}
                    <li class="runtime-nav-item">
                      {#if child.path}
                        <a href={child.path} class="runtime-nav-link">{child.label ?? child.id}</a>
                      {:else}
                        <span class="runtime-nav-label">{child.label ?? child.id}</span>
                      {/if}
                    </li>
                  {/each}
                </ul>
              {/if}
            </li>
          {/each}
        </ul>
      </nav>
    </aside>
  {/if}
  <main class="runtime-main">
    <p class="runtime-main-placeholder">Page content will render here.</p>
    {#if snapshot.pages && snapshot.pages.length > 0}
      <p class="runtime-main-meta">{snapshot.pages.length} page(s) defined.</p>
    {/if}
    {#if applicationReleaseId && dataSources.length > 0}
      <section class="runtime-datasources" aria-label="Datasource execution">
        <p class="runtime-datasources-title">Datasources (run via BFF)</p>
        <ul class="runtime-datasources-list">
          {#each dataSources as ds (ds.id)}
            <li class="runtime-datasources-item">
              <span class="runtime-datasources-id">{ds.id}</span>
              <button
                type="button"
                class="runtime-datasources-run"
                disabled={executing}
                onclick={() => runDatasource(ds.id)}
              >
                {executing ? 'Running…' : 'Run'}
              </button>
            </li>
          {/each}
        </ul>
        {#if executionError}
          <p class="runtime-execution-error">{executionError}</p>
        {/if}
        {#if executionResult != null}
          <pre class="runtime-execution-result">{JSON.stringify(executionResult, null, 2)}</pre>
        {/if}
      </section>
    {/if}
  </main>
  {/if}
</div>

<style>
  .runtime-app {
    display: flex;
    flex-direction: row;
    min-height: 100%;
  }
  .runtime-initial-html {
    flex: 1;
    min-width: 0;
  }
  /* Make server-rendered semantic nav visible (labels from data-label); :global() so {@html} content is styled */
  .runtime-initial-html :global([data-component="navigation-root"]),
  .runtime-initial-html :global([data-component="navigation-sub"]) {
    display: block;
    margin: 0.25rem 0;
  }
  .runtime-initial-html :global([data-component="navigation-root"]:not([data-label=""]))::before,
  .runtime-initial-html :global([data-component="navigation-sub"]:not([data-label=""]))::before {
    content: attr(data-label);
    display: inline-block;
  }
  .runtime-initial-html :global([data-component="navigation-root"] a),
  .runtime-initial-html :global([data-component="navigation-sub"] a) {
    color: var(--datarizen-color-link, #2563eb);
  }
  .runtime-nav-sidebar {
    flex-shrink: 0;
    width: 12rem;
    min-height: 100%;
    background: var(--datarizen-color-surface, #f3f4f6);
    border-right: 1px solid var(--datarizen-color-border, #e5e7eb);
  }
  .runtime-nav {
    padding: var(--datarizen-spacing-md, 1rem);
    position: sticky;
    top: 0;
  }
  .runtime-nav-list,
  .runtime-nav-children {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }
  .runtime-nav-children {
    margin-top: 0.25rem;
    padding-left: 1rem;
    border-left: 1px solid var(--datarizen-color-border, #e5e7eb);
  }
  .runtime-nav-item {
    margin: 0;
  }
  .runtime-nav-link {
    color: var(--datarizen-color-primary, #2563eb);
    text-decoration: none;
  }
  .runtime-nav-link:hover {
    text-decoration: underline;
  }
  .runtime-nav-label {
    color: var(--datarizen-color-text, #374151);
  }
  .runtime-main {
    flex: 1;
    min-width: 0;
    padding: var(--datarizen-spacing-lg, 1.5rem);
  }
  .runtime-main-placeholder {
    color: var(--datarizen-color-text-muted, #6b7280);
  }
  .runtime-main-meta {
    margin-top: 0.5rem;
    font-size: 0.875rem;
    color: var(--datarizen-color-text-muted, #6b7280);
  }
  .runtime-datasources {
    margin-top: 1.5rem;
    padding-top: 1rem;
    border-top: 1px solid var(--datarizen-color-border, #e5e7eb);
  }
  .runtime-datasources-title {
    font-weight: 600;
    margin-bottom: 0.5rem;
  }
  .runtime-datasources-list {
    list-style: none;
    margin: 0;
    padding: 0;
  }
  .runtime-datasources-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.25rem;
  }
  .runtime-datasources-id {
    font-family: monospace;
    font-size: 0.875rem;
  }
  .runtime-datasources-run {
    padding: 0.25rem 0.5rem;
    font-size: 0.875rem;
  }
  .runtime-execution-error {
    color: #dc2626;
    margin-top: 0.5rem;
  }
  .runtime-execution-result {
    margin-top: 0.5rem;
    padding: 0.75rem;
    background: var(--datarizen-color-surface, #f3f4f6);
    border-radius: 4px;
    font-size: 0.8125rem;
    overflow: auto;
  }
</style>
