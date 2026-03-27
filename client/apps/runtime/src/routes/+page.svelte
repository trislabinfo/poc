<script lang="ts">
  // Runtime — redirect or landing when no tenant/app in path
</script>

<main>
  <h1>Runtime</h1>
  <p>End-user app renderer. Open a URL like <code>/&lt;tenantSlug&gt;/&lt;appSlug&gt;/production</code> to load an app.</p>
</main>

<style>
  main {
    padding: var(--datarizen-spacing-lg, 1.5rem);
    max-width: 60rem;
    margin: 0 auto;
  }
  h1 {
    color: var(--datarizen-primary, #2563eb);
  }
  code {
    background: var(--datarizen-bg-muted, #f9fafb);
    padding: 0.125rem 0.375rem;
    border-radius: var(--datarizen-radius-sm, 0.25rem);
  }
</style>
