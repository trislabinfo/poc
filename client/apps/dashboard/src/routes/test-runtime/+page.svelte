<script lang="ts">
  import { apiPostFullResponse, apiPost } from '$lib/shared/apiClient';

  /** Flow state: IDs and slugs from previous steps for prefilling and building paths */
  interface FlowState {
    tenantId: string;
    tenantSlug: string;
    applicationDefinitionId: string;
    platformReleaseId: string;
    tenantApplicationId: string;
    appSlug: string;
    tenantReleaseId: string;
    environmentId: string;
    environmentName: string;
  }

  function emptyFlowState(): FlowState {
    return {
      tenantId: '',
      tenantSlug: '',
      applicationDefinitionId: '',
      platformReleaseId: '',
      tenantApplicationId: '',
      appSlug: '',
      tenantReleaseId: '',
      environmentId: '',
      environmentName: ''
    };
  }

  /** Get string from response; backend may return camelCase or PascalCase */
  function getStr(obj: unknown, ...keys: string[]): string {
    if (obj == null || typeof obj !== 'object') return '';
    const o = obj as Record<string, unknown>;
    for (const k of keys) {
      const v = o[k];
      if (typeof v === 'string') return v;
    }
    return '';
  }

  let flow = $state(emptyFlowState());
  let expandedStep = $state<number | null>(1);
  let stepResponses = $state<Record<number, { status: number; body: string }>>({});
  let stepLoading = $state<Record<number, boolean>>({});

  // Step 1 payload (create tenant)
  let step1Body = $state(
    JSON.stringify(
      {
        Name: 'DataRizenTenant',
        Slug: 'DRSlug',
        Users: [
          {
            Email: 'hello@dr.com',
            DisplayName: 'Mr. Hello',
            Password: 'NotSavedIdDb!1',
            IsTenantOwner: true
          }
        ]
      },
      null,
      2
    )
  );

  // Step 2: application definition + editable entities, properties, relations, navigation
  let step2Body = $state(
    JSON.stringify(
      {
        name: 'crm',
        description: 'crm desc',
        slug: 'crm',
        ispublic: true
      },
      null,
      2
    )
  );
  const defaultEntities = [
    { name: 'Customer', displayName: 'Customer' },
    { name: 'Order', displayName: 'Order' }
  ];
  let step2EntitiesBody = $state(JSON.stringify(defaultEntities, null, 2));
  const defaultProperties = [
    { entityName: 'Customer', name: 'Name', displayName: 'Name', dataType: 0, isRequired: true, order: 0 },
    { entityName: 'Customer', name: 'Email', displayName: 'Email', dataType: 0, isRequired: false, order: 1 },
    { entityName: 'Customer', name: 'CreatedAt', displayName: 'Created At', dataType: 3, isRequired: false, order: 2 },
    { entityName: 'Order', name: 'OrderNumber', displayName: 'Order Number', dataType: 0, isRequired: true, order: 0 },
    { entityName: 'Order', name: 'OrderDate', displayName: 'Order Date', dataType: 3, isRequired: false, order: 1 }
  ];
  let step2PropertiesBody = $state(JSON.stringify(defaultProperties, null, 2));
  const defaultRelations = [
    { sourceEntity: 'Order', targetEntity: 'Customer', name: 'Customer', relationType: 1, cascadeDelete: false }
  ];
  let step2RelationsBody = $state(JSON.stringify(defaultRelations, null, 2));
  const defaultNavigation = {
    children: [
      { label: 'Customer', children: [{ label: 'Search' }, { label: 'Add' }] },
      { label: 'Order', children: [{ label: 'Search' }, { label: 'Add' }] }
    ]
  };
  let step2NavigationBody = $state(JSON.stringify(defaultNavigation, null, 2));

  // Step 4: install – path params prefilled from flow; body
  let step4Name = $state('tenant-crm');
  let step4Slug = $state('crm');
  let step7EnvName = $state('Development');
  let step8Version = $state('0.0.0');

  /** Runtime app base URL for Step 9 link. Set VITE_RUNTIME_BASE_URL or defaults to runtime dev server (e.g. 5175). */
  const runtimeBaseUrl =
    typeof import.meta.env?.VITE_RUNTIME_BASE_URL === 'string'
      ? (import.meta.env.VITE_RUNTIME_BASE_URL as string).replace(/\/$/, '')
      : 'http://localhost:5175';

  function setResponse(step: number, status: number, body: unknown) {
    stepResponses = {
      ...stepResponses,
      [step]: { status, body: typeof body === 'string' ? body : JSON.stringify(body, null, 2) }
    };
  }

  function setLoading(step: number, value: boolean) {
    stepLoading = { ...stepLoading, [step]: value };
  }

  function formatFullResponse(res: { status: number; statusText: string; headers: Record<string, string>; body: string }): string {
    const lines = [
      `HTTP ${res.status} ${res.statusText}`,
      '',
      'Response headers:',
      ...Object.entries(res.headers).map(([k, v]) => `  ${k}: ${v}`),
      '',
      'Response body:',
      res.body
    ];
    return lines.join('\n');
  }

  async function runStep1() {
    setLoading(1, true);
    try {
      let body: unknown;
      try {
        body = JSON.parse(step1Body);
      } catch {
        setResponse(1, 0, 'Invalid JSON in request body');
        return;
      }
      const res = await apiPostFullResponse('/api/tenant/with-users', body);
      const fullDisplay = formatFullResponse(res);
      setResponse(1, res.status, fullDisplay);
      if (res.status >= 200 && res.status < 300 && res.body && res.body !== '(empty body)') {
        try {
          const data = JSON.parse(res.body) as Record<string, unknown>;
          flow = {
            ...flow,
            tenantId: getStr(data, 'id', 'tenantId', 'Id', 'TenantId'),
            tenantSlug: getStr(data, 'slug', 'Slug') || (body as Record<string, unknown>)?.Slug as string || (body as Record<string, unknown>)?.slug as string || ''
          };
        } catch {
          // keep flow unchanged if body wasn't JSON
        }
      }
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      setResponse(1, 0, `Request failed (e.g. network or CORS): ${msg}`);
    } finally {
      setLoading(1, false);
    }
  }

  async function runStep2() {
    setLoading(2, true);
    const log: string[] = [];
    try {
      let body: unknown;
      try {
        body = JSON.parse(step2Body);
      } catch {
        setResponse(2, 0, 'Invalid JSON in request body');
        return;
      }
      const res = await apiPostFullResponse('/api/appbuilder/application-definitions', body);
      log.push(`Application definition: HTTP ${res.status}`);
      if (res.status < 200 || res.status >= 300) {
        setResponse(2, res.status, log.join('\n\n') + '\n\n' + formatFullResponse(res));
        return;
      }
      let appId = '';
      try {
        const parsed = JSON.parse(res.body) as Record<string, unknown>;
        // API may return { id: "..." } at root or { data: { id: "..." } }
        appId = getStr(parsed, 'id', 'applicationDefinitionId', 'Id', 'ApplicationDefinitionId');
        if (!appId && parsed.data && typeof parsed.data === 'object') {
          appId = getStr(parsed.data as Record<string, unknown>, 'id', 'applicationDefinitionId', 'Id', 'ApplicationDefinitionId');
        }
      } catch {
        setResponse(2, res.status, log.join('\n\n') + '\n\n' + formatFullResponse(res));
        return;
      }
      if (!appId) {
        setResponse(2, res.status, log.join('\n\n') + '\n\nNo application definition ID in response.');
        return;
      }
      flow = { ...flow, applicationDefinitionId: appId };
      log.push(`Application definition ID (copied to Step 3): ${appId}`);

      const createEntity = (name: string, displayName: string) =>
        apiPost<Record<string, unknown>>('/api/appbuilder/entities', {
          applicationDefinitionId: appId,
          name,
          displayName
        });
      const createProperty = (entityId: string, name: string, displayName: string, dataType: number, isRequired: boolean, order: number) =>
        apiPost<Record<string, unknown>>(`/api/appbuilder/entities/${entityId}/properties`, {
          entityDefinitionId: entityId,
          name,
          displayName,
          dataType,
          isRequired,
          order
        });
      const createRelation = (sourceEntityId: string, targetEntityId: string, name: string, relationType: number, cascadeDelete: boolean) =>
        apiPost<Record<string, unknown>>('/api/appbuilder/relations', {
          sourceEntityId,
          targetEntityId,
          name,
          relationType,
          cascadeDelete
        });

      let entities: Array<{ name: string; displayName: string }> = [];
      try {
        entities = JSON.parse(step2EntitiesBody) as Array<{ name: string; displayName: string }>;
        if (!Array.isArray(entities)) entities = [];
      } catch {
        log.push('Entities: invalid JSON, skipped');
      }
      const entityIdByName: Record<string, string> = {};
      for (const e of entities) {
        const n = e?.name?.trim();
        const d = e?.displayName?.trim() || n;
        if (!n) continue;
        const resE = await createEntity(n, d);
        if (!resE.ok) {
          log.push(`Entity ${n}: failed (${resE.status})`);
          continue;
        }
        const id = getStr(resE.data as Record<string, unknown>, 'id');
        entityIdByName[n] = id;
        log.push(`Entity ${n}: created (id: ${id})`);
      }

      let properties: Array<{ entityName: string; name: string; displayName: string; dataType: number; isRequired: boolean; order: number }> = [];
      try {
        properties = JSON.parse(step2PropertiesBody) as typeof properties;
        if (!Array.isArray(properties)) properties = [];
      } catch {
        log.push('Properties: invalid JSON, skipped');
      }
      for (const p of properties) {
        const eid = entityIdByName[p?.entityName?.trim() ?? ''];
        if (!eid) continue;
        const name = p?.name?.trim() ?? 'Property';
        const displayName = p?.displayName?.trim() || name;
        const dataType = typeof p?.dataType === 'number' ? p.dataType : 0;
        const isRequired = !!p?.isRequired;
        const order = typeof p?.order === 'number' ? p.order : 0;
        await createProperty(eid, name, displayName, dataType, isRequired, order);
      }
      log.push(`Properties: ${properties.length} created`);

      let relations: Array<{ sourceEntity: string; targetEntity: string; name: string; relationType: number; cascadeDelete: boolean }> = [];
      try {
        relations = JSON.parse(step2RelationsBody) as typeof relations;
        if (!Array.isArray(relations)) relations = [];
      } catch {
        log.push('Relations: invalid JSON, skipped');
      }
      for (const r of relations) {
        const srcId = entityIdByName[r?.sourceEntity?.trim() ?? ''];
        const tgtId = entityIdByName[r?.targetEntity?.trim() ?? ''];
        if (!srcId || !tgtId) continue;
        const relRes = await createRelation(srcId, tgtId, r?.name?.trim() ?? 'Relation', r?.relationType ?? 1, !!r?.cascadeDelete);
        if (!relRes.ok) log.push(`Relation ${r.sourceEntity}->${r.targetEntity}: failed (${relRes.status})`);
        else log.push(`Relation ${r.sourceEntity}->${r.targetEntity}: created`);
      }

      let navConfig: { children?: Array<{ label: string; children?: Array<{ label: string }> }> } = {};
      try {
        navConfig = JSON.parse(step2NavigationBody) as typeof navConfig;
      } catch {
        navConfig = { children: [{ label: 'Customer', children: [{ label: 'Search' }, { label: 'Add' }] }, { label: 'Order', children: [{ label: 'Search' }, { label: 'Add' }] }] };
      }
      const navRes = await apiPost<Record<string, unknown>>('/api/appbuilder/navigations', {
        applicationDefinitionId: appId,
        name: 'Main',
        configurationJson: JSON.stringify(navConfig)
      });
      if (!navRes.ok) log.push(`Navigation: failed (${navRes.status})`);
      else log.push('Navigation: Main created');

      setResponse(2, 200, log.join('\n\n') + '\n\n' + formatFullResponse(res));
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      setResponse(2, 0, (log.length ? log.join('\n\n') + '\n\n' : '') + `Request failed: ${msg}`);
    } finally {
      setLoading(2, false);
    }
  }

  async function runStep3() {
    const appDefId = flow.applicationDefinitionId.trim();
    if (!appDefId) {
      setResponse(3, 0, 'Enter Application definition ID (from Step 2 response or paste here).');
      return;
    }
    setLoading(3, true);
    try {
      const res = await apiPostFullResponse(`/api/appbuilder/applications/${appDefId}/releases`, {});
      setResponse(3, res.status, formatFullResponse(res));
      if (res.status >= 200 && res.status < 300 && res.body && res.body !== '(empty body)') {
        try {
          const parsed = JSON.parse(res.body) as Record<string, unknown>;
          let releaseId = getStr(parsed, 'id', 'releaseId', 'Id', 'ReleaseId');
          if (!releaseId && parsed.data && typeof parsed.data === 'object')
            releaseId = getStr(parsed.data as Record<string, unknown>, 'id', 'releaseId', 'Id', 'ReleaseId');
          if (releaseId) flow = { ...flow, platformReleaseId: releaseId };
        } catch { /* ignore */ }
      }
    } catch (e) {
      setResponse(3, 0, `Request failed: ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      setLoading(3, false);
    }
  }

  async function runStep4() {
    const tenantId = flow.tenantId.trim();
    const platformReleaseId = flow.platformReleaseId.trim();
    if (!tenantId) {
      setResponse(4, 0, 'Enter Tenant ID (from Step 1 or paste here).');
      return;
    }
    if (!platformReleaseId) {
      setResponse(4, 0, 'Enter Platform release ID (from Step 3 or paste here).');
      return;
    }
    setLoading(4, true);
    try {
      const body = { ApplicationReleaseId: platformReleaseId, Name: step4Name, Slug: step4Slug };
      const res = await apiPostFullResponse(`/api/tenantapplication/tenants/${tenantId}/applications/install`, body);
      setResponse(4, res.status, formatFullResponse(res));
      if (res.status >= 200 && res.status < 300 && res.body && res.body !== '(empty body)') {
        try {
          const data = JSON.parse(res.body) as Record<string, unknown>;
          flow = { ...flow, tenantApplicationId: getStr(data, 'id', 'tenantApplicationId', 'Id', 'TenantApplicationId'), appSlug: step4Slug };
        } catch { /* ignore */ }
      }
    } catch (e) {
      setResponse(4, 0, `Request failed: ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      setLoading(4, false);
    }
  }

  async function runStep5() {
    const tenantId = flow.tenantId.trim();
    const appId = flow.tenantApplicationId.trim();
    if (!tenantId || !appId) {
      setResponse(5, 0, 'Enter Tenant ID and Tenant application ID (from Steps 1 & 4 or paste here).');
      return;
    }
    setLoading(5, true);
    try {
      const res = await apiPostFullResponse(`/api/tenantapplication/tenants/${tenantId}/applications/${appId}/releases`, {});
      setResponse(5, res.status, formatFullResponse(res));
      if (res.status >= 200 && res.status < 300 && res.body && res.body !== '(empty body)') {
        try {
          const data = JSON.parse(res.body) as Record<string, unknown>;
          flow = { ...flow, tenantReleaseId: getStr(data, 'id', 'tenantReleaseId', 'Id', 'ReleaseId') };
        } catch { /* ignore */ }
      }
    } catch (e) {
      setResponse(5, 0, `Request failed: ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      setLoading(5, false);
    }
  }

  async function runStep6() {
    const tenantId = flow.tenantId.trim();
    const appId = flow.tenantApplicationId.trim();
    const releaseId = flow.tenantReleaseId.trim();
    if (!tenantId || !appId || !releaseId) {
      setResponse(6, 0, 'Enter Tenant ID, Tenant application ID, and Tenant release ID (from Steps 1, 4, 5 or paste here).');
      return;
    }
    setLoading(6, true);
    try {
      const res = await apiPostFullResponse(
        `/api/tenantapplication/tenants/${tenantId}/applications/${appId}/releases/${releaseId}/approve`,
        {}
      );
      setResponse(6, res.status, formatFullResponse(res));
    } catch (e) {
      setResponse(6, 0, `Request failed: ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      setLoading(6, false);
    }
  }

  async function runStep7() {
    const tenantId = flow.tenantId.trim();
    const appId = flow.tenantApplicationId.trim();
    if (!tenantId || !appId) {
      setResponse(7, 0, 'Enter Tenant ID (Step 1) and Tenant application ID (Step 4 – the installed app Id, not the release Id).');
      return;
    }
    setLoading(7, true);
    try {
      const body = { Name: step7EnvName, EnvironmentType: 0 };
      const res = await apiPostFullResponse(
        `/api/tenantapplication/tenants/${tenantId}/applications/${appId}/environments`,
        body
      );
      setResponse(7, res.status, formatFullResponse(res));
      if (res.status >= 200 && res.status < 300 && res.body && res.body !== '(empty body)') {
        try {
          const data = JSON.parse(res.body) as Record<string, unknown>;
          flow = { ...flow, environmentId: getStr(data, 'id', 'environmentId', 'Id', 'EnvironmentId'), environmentName: step7EnvName };
        } catch { /* ignore */ }
      }
    } catch (e) {
      setResponse(7, 0, `Request failed: ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      setLoading(7, false);
    }
  }

  async function runStep8() {
    const tenantId = flow.tenantId.trim();
    const appId = flow.tenantApplicationId.trim();
    const envId = flow.environmentId.trim();
    const releaseId = flow.tenantReleaseId.trim();
    if (!tenantId || !appId || !envId || !releaseId) {
      setResponse(8, 0, 'Enter Tenant ID (Step 1), Tenant application ID (Step 4), Environment ID (Step 7), and Release ID (Step 5 – approved tenant release).');
      return;
    }
    setLoading(8, true);
    try {
      const body = { ReleaseId: releaseId, Version: step8Version };
      const res = await apiPostFullResponse(
        `/api/tenantapplication/tenants/${tenantId}/applications/${appId}/environments/${envId}/deploy`,
        body
      );
      setResponse(8, res.status, formatFullResponse(res));
    } catch (e) {
      setResponse(8, 0, `Request failed: ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      setLoading(8, false);
    }
  }

  const runtimePath = $derived(
    flow.tenantSlug && flow.appSlug && flow.environmentName
      ? `/${flow.tenantSlug}/${flow.appSlug}/${flow.environmentName}`
      : ''
  );
  const runtimeUrl = $derived(runtimePath ? `${runtimeBaseUrl}${runtimePath}` : '');
</script>

<main class="test-runtime">
  <h1>Runtime testing flow</h1>
  <p class="description">
    Run each step in order to create a tenant, application definition, release, install the app, approve, create an environment, and deploy. Then open the runtime URL to verify the app (including HTML initial view and JSON fallback).
  </p>

  <div class="cards">
    <!-- Step 1 -->
    <div class="card" class:expanded={expandedStep === 1}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 1 ? null : 1)}>
        <span class="card-title">Step 1 – Create tenant</span>
        <span class="card-toggle">{expandedStep === 1 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 1}
        <div class="card-body">
          <label for="step1-body">Request body (JSON)</label>
          <textarea id="step1-body" bind:value={step1Body} rows="12" spellcheck="false"></textarea>
          <p class="hint">Response will fill Tenant ID and Tenant slug below; you can also paste them into Step 4.</p>
          <button type="button" class="btn-execute" onclick={runStep1} disabled={stepLoading[1]}>
            {stepLoading[1] ? 'Executing…' : 'Execute'}
          </button>
          <label for="step1-response">Response</label>
          <textarea id="step1-response" class="response" readonly rows="8" spellcheck="false">{stepResponses[1]?.body ?? ''}</textarea>
          {#if stepResponses[1]}<span class="status">Status: {stepResponses[1].status}</span>{/if}
        </div>
      {/if}
    </div>

    <!-- Step 2 -->
    <div class="card" class:expanded={expandedStep === 2}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 2 ? null : 2)}>
        <span class="card-title">Step 2 – Create application definition</span>
        <span class="card-toggle">{expandedStep === 2 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 2}
        <div class="card-body">
          <label for="step2-body">Application definition (JSON)</label>
          <textarea id="step2-body" bind:value={step2Body} rows="6" spellcheck="false"></textarea>

          <label for="step2-entities">Entities definition</label>
          <textarea id="step2-entities" bind:value={step2EntitiesBody} rows="6" spellcheck="false"></textarea>

          <label for="step2-properties">Properties definition</label>
          <textarea id="step2-properties" bind:value={step2PropertiesBody} rows="10" spellcheck="false"></textarea>

          <label for="step2-relations">Relations definition</label>
          <textarea id="step2-relations" bind:value={step2RelationsBody} rows="5" spellcheck="false"></textarea>

          <label for="step2-navigation">Navigation definition</label>
          <textarea id="step2-navigation" bind:value={step2NavigationBody} rows="10" spellcheck="false"></textarea>

          <p class="hint">Edit the JSON above if needed, then Execute. Application definition ID from the response is copied to Step 3.</p>
          <button type="button" class="btn-execute" onclick={runStep2} disabled={stepLoading[2]}>
            {stepLoading[2] ? 'Executing…' : 'Execute'}
          </button>
          <label for="step2-response">Response</label>
          <textarea id="step2-response" class="response" readonly rows="8" spellcheck="false">{stepResponses[2]?.body ?? ''}</textarea>
          {#if stepResponses[2]}<span class="status">Status: {stepResponses[2].status}</span>{/if}
          {#if flow.applicationDefinitionId}
            <p class="hint">Application definition ID for Step 3: <code>{flow.applicationDefinitionId}</code></p>
          {/if}
        </div>
      {/if}
    </div>

    <!-- Step 3 -->
    <div class="card" class:expanded={expandedStep === 3}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 3 ? null : 3)}>
        <span class="card-title">Step 3 – Create (platform) release</span>
        <span class="card-toggle">{expandedStep === 3 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 3}
        <div class="card-body">
          <label for="step3-app-def-id">Application definition ID (prefilled from Step 2)</label>
          <input id="step3-app-def-id" type="text" bind:value={flow.applicationDefinitionId} placeholder="From Step 2 response (id) or paste here" />
          <button type="button" class="btn-execute" onclick={runStep3} disabled={stepLoading[3]}>
            {stepLoading[3] ? 'Executing…' : 'Execute'}
          </button>
          <label for="step3-response">Response</label>
          <textarea id="step3-response" class="response" readonly rows="8" spellcheck="false">{stepResponses[3]?.body ?? ''}</textarea>
          {#if stepResponses[3]}<span class="status">Status: {stepResponses[3].status}</span>{/if}
        </div>
      {/if}
    </div>

    <!-- Step 4 -->
    <div class="card" class:expanded={expandedStep === 4}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 4 ? null : 4)}>
        <span class="card-title">Step 4 – Install tenant application</span>
        <span class="card-toggle">{expandedStep === 4 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 4}
        <div class="card-body">
          <label for="step4-tenant-id">Tenant ID</label>
          <input id="step4-tenant-id" type="text" bind:value={flow.tenantId} placeholder="From Step 1 response or paste here" />
          <label for="step4-platform-release-id">Platform release ID</label>
          <input id="step4-platform-release-id" type="text" bind:value={flow.platformReleaseId} placeholder="From Step 3 response or paste here" />
          <label for="step4-name">Name</label>
          <input id="step4-name" type="text" bind:value={step4Name} />
          <label for="step4-slug">Slug (used in runtime URL)</label>
          <input id="step4-slug" type="text" bind:value={step4Slug} />
          <button type="button" class="btn-execute" onclick={runStep4} disabled={stepLoading[4]}>
            {stepLoading[4] ? 'Executing…' : 'Execute'}
          </button>
          <label for="step4-response">Response</label>
          <textarea id="step4-response" class="response" readonly rows="8" spellcheck="false">{stepResponses[4]?.body ?? ''}</textarea>
          {#if stepResponses[4]}<span class="status">Status: {stepResponses[4].status}</span>{/if}
        </div>
      {/if}
    </div>

    <!-- Step 5 -->
    <div class="card" class:expanded={expandedStep === 5}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 5 ? null : 5)}>
        <span class="card-title">Step 5 – Create tenant application release</span>
        <span class="card-toggle">{expandedStep === 5 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 5}
        <div class="card-body">
          <label for="step5-tenant-id">Tenant ID</label>
          <input id="step5-tenant-id" type="text" bind:value={flow.tenantId} placeholder="From Step 1" />
          <label for="step5-tenant-app-id">Tenant application ID</label>
          <input id="step5-tenant-app-id" type="text" bind:value={flow.tenantApplicationId} placeholder="From Step 4 – Id of the installed app (not a release Id)" />
          <button type="button" class="btn-execute" onclick={runStep5} disabled={stepLoading[5]}>
            {stepLoading[5] ? 'Executing…' : 'Execute'}
          </button>
          <label for="step5-response">Response</label>
          <textarea id="step5-response" class="response" readonly rows="8" spellcheck="false">{stepResponses[5]?.body ?? ''}</textarea>
          {#if stepResponses[5]}<span class="status">Status: {stepResponses[5].status}</span>{/if}
        </div>
      {/if}
    </div>

    <!-- Step 6 -->
    <div class="card" class:expanded={expandedStep === 6}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 6 ? null : 6)}>
        <span class="card-title">Step 6 – Approve tenant release</span>
        <span class="card-toggle">{expandedStep === 6 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 6}
        <div class="card-body">
          <label for="step6-tenant-id">Tenant ID</label>
          <input id="step6-tenant-id" type="text" bind:value={flow.tenantId} placeholder="From Step 1" />
          <label for="step6-tenant-app-id">Tenant application ID</label>
          <input id="step6-tenant-app-id" type="text" bind:value={flow.tenantApplicationId} placeholder="From Step 4 – installed app Id" />
          <label for="step6-tenant-release-id">Tenant release ID (to approve)</label>
          <input id="step6-tenant-release-id" type="text" bind:value={flow.tenantReleaseId} placeholder="From Step 5 response – release Id" />
          <button type="button" class="btn-execute" onclick={runStep6} disabled={stepLoading[6]}>
            {stepLoading[6] ? 'Executing…' : 'Execute'}
          </button>
          <label for="step6-response">Response</label>
          <textarea id="step6-response" class="response" readonly rows="6" spellcheck="false">{stepResponses[6]?.body ?? ''}</textarea>
          {#if stepResponses[6]}<span class="status">Status: {stepResponses[6].status}</span>{/if}
        </div>
      {/if}
    </div>

    <!-- Step 7 -->
    <div class="card" class:expanded={expandedStep === 7}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 7 ? null : 7)}>
        <span class="card-title">Step 7 – Create environment</span>
        <span class="card-toggle">{expandedStep === 7 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 7}
        <div class="card-body">
          <p class="hint">Use <strong>Tenant application ID from Step 4</strong> (the installed app Id). Do not use the release Id from Step 5.</p>
          <label for="step7-tenant-id">Tenant ID</label>
          <input id="step7-tenant-id" type="text" bind:value={flow.tenantId} placeholder="From Step 1" />
          <label for="step7-tenant-app-id">Tenant application ID</label>
          <input id="step7-tenant-app-id" type="text" bind:value={flow.tenantApplicationId} placeholder="From Step 4 – installed app Id (not release Id)" />
          <label for="step7-name">Environment name</label>
          <input id="step7-name" type="text" bind:value={step7EnvName} />
          <button type="button" class="btn-execute" onclick={runStep7} disabled={stepLoading[7]}>
            {stepLoading[7] ? 'Executing…' : 'Execute'}
          </button>
          <label for="step7-response">Response</label>
          <textarea id="step7-response" class="response" readonly rows="6" spellcheck="false">{stepResponses[7]?.body ?? ''}</textarea>
          {#if stepResponses[7]}<span class="status">Status: {stepResponses[7].status}</span>{/if}
        </div>
      {/if}
    </div>

    <!-- Step 8 -->
    <div class="card" class:expanded={expandedStep === 8}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 8 ? null : 8)}>
        <span class="card-title">Step 8 – Deploy</span>
        <span class="card-toggle">{expandedStep === 8 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 8}
        <div class="card-body">
          <p class="hint">Tenant application ID = from Step 4 (installed app). Release ID = from Step 5 (the approved tenant release to deploy).</p>
          <label for="step8-tenant-id">Tenant ID</label>
          <input id="step8-tenant-id" type="text" bind:value={flow.tenantId} placeholder="From Step 1" />
          <label for="step8-tenant-app-id">Tenant application ID</label>
          <input id="step8-tenant-app-id" type="text" bind:value={flow.tenantApplicationId} placeholder="From Step 4 – installed app Id" />
          <label for="step8-environment-id">Environment ID</label>
          <input id="step8-environment-id" type="text" bind:value={flow.environmentId} placeholder="From Step 7 response" />
          <label for="step8-release-id">Release ID (approved tenant release)</label>
          <input id="step8-release-id" type="text" bind:value={flow.tenantReleaseId} placeholder="From Step 5 response – same Id as in Step 6" />
          <label for="step8-version">Version</label>
          <input id="step8-version" type="text" bind:value={step8Version} />
          <button type="button" class="btn-execute" onclick={runStep8} disabled={stepLoading[8]}>
            {stepLoading[8] ? 'Executing…' : 'Execute'}
          </button>
          <label for="step8-response">Response</label>
          <textarea id="step8-response" class="response" readonly rows="6" spellcheck="false">{stepResponses[8]?.body ?? ''}</textarea>
          {#if stepResponses[8]}<span class="status">Status: {stepResponses[8].status}</span>{/if}
        </div>
      {/if}
    </div>

    <!-- Step 9 -->
    <div class="card" class:expanded={expandedStep === 9}>
      <button type="button" class="card-header" onclick={() => (expandedStep = expandedStep === 9 ? null : 9)}>
        <span class="card-title">Step 9 – Open runtime</span>
        <span class="card-toggle">{expandedStep === 9 ? '▼' : '▶'}</span>
      </button>
      {#if expandedStep === 9}
        <div class="card-body">
          <p class="hint">Ensure the runtime client is running (e.g. <code>pnpm run dev --filter runtime</code>) and <code>VITE_API_BASE_URL</code> points at the gateway.</p>
          <label for="step9-tenant-slug">Tenant slug (for URL path)</label>
          <input id="step9-tenant-slug" type="text" bind:value={flow.tenantSlug} placeholder="From Step 1 or enter manually" />
          <label for="step9-app-slug">App slug (for URL path)</label>
          <input id="step9-app-slug" type="text" bind:value={flow.appSlug} placeholder="From Step 4 or enter manually" />
          <label for="step9-env-name">Environment name (for URL path)</label>
          <input id="step9-env-name" type="text" bind:value={flow.environmentName} placeholder="From Step 7 or enter manually" />
          {#if runtimePath}
            <p>Runtime path: <code>{runtimePath}</code></p>
            <a href={runtimeUrl} target="_blank" rel="noopener noreferrer" class="link-runtime">Open runtime</a>
          {:else}
            <p>Fill tenant slug, app slug, and environment name above to build the runtime URL.</p>
          {/if}
        </div>
      {/if}
    </div>
  </div>
</main>

<style>
  .test-runtime {
    padding: var(--datarizen-spacing-lg, 1.5rem);
    max-width: 56rem;
    margin: 0 auto;
  }
  .test-runtime h1 {
    color: var(--datarizen-primary, #2563eb);
    margin-bottom: var(--datarizen-spacing-sm, 0.5rem);
  }
  .description {
    color: var(--datarizen-text-muted, #6b7280);
    margin-bottom: var(--datarizen-spacing-xl, 2rem);
  }
  .cards {
    display: flex;
    flex-direction: column;
    gap: var(--datarizen-spacing-md, 1rem);
  }
  .card {
    border: 1px solid var(--datarizen-border, #e5e7eb);
    border-radius: var(--datarizen-radius-lg, 0.5rem);
    background: var(--datarizen-bg, #fff);
    overflow: hidden;
  }
  .card-header {
    width: 100%;
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: var(--datarizen-spacing-md, 1rem);
    background: var(--datarizen-bg-muted, #f9fafb);
    border: none;
    cursor: pointer;
    font-size: 1rem;
    text-align: left;
  }
  .card-header:hover {
    background: var(--datarizen-border, #e5e7eb);
  }
  .card-title {
    font-weight: 600;
    color: var(--datarizen-text, #1f2937);
  }
  .card-toggle {
    color: var(--datarizen-text-muted, #6b7280);
  }
  .card-body {
    padding: var(--datarizen-spacing-lg, 1.5rem);
    display: flex;
    flex-direction: column;
    gap: var(--datarizen-spacing-sm, 0.5rem);
  }
  .card-body label {
    font-weight: 500;
    color: var(--datarizen-text, #1f2937);
  }
  .card-body textarea,
  .card-body input[type='text'] {
    width: 100%;
    box-sizing: border-box;
    padding: var(--datarizen-spacing-sm, 0.5rem);
    border: 1px solid var(--datarizen-border, #e5e7eb);
    border-radius: var(--datarizen-radius-md, 0.375rem);
    font-family: ui-monospace, monospace;
    font-size: 0.875rem;
  }
  .card-body .response {
    background: var(--datarizen-bg-muted, #f9fafb);
    resize: vertical;
  }
  .hint {
    font-size: 0.875rem;
    color: var(--datarizen-text-muted, #6b7280);
    margin: 0 0 var(--datarizen-spacing-sm, 0.5rem) 0;
  }
  .hint code {
    background: var(--datarizen-bg-muted, #f9fafb);
    padding: 0.1em 0.4em;
    border-radius: var(--datarizen-radius-sm, 0.25rem);
  }
  .btn-execute {
    align-self: flex-start;
    padding: var(--datarizen-spacing-sm, 0.5rem) var(--datarizen-spacing-md, 1rem);
    background: var(--datarizen-primary, #2563eb);
    color: white;
    border: none;
    border-radius: var(--datarizen-radius-md, 0.375rem);
    cursor: pointer;
    font-weight: 500;
  }
  .btn-execute:hover:not(:disabled) {
    background: var(--datarizen-primary-hover, #1d4ed8);
  }
  .btn-execute:disabled {
    opacity: 0.7;
    cursor: not-allowed;
  }
  .status {
    font-size: 0.875rem;
    color: var(--datarizen-text-muted, #6b7280);
  }
  .link-runtime {
    display: inline-block;
    margin-top: var(--datarizen-spacing-sm, 0.5rem);
    padding: var(--datarizen-spacing-sm, 0.5rem) var(--datarizen-spacing-md, 1rem);
    background: var(--datarizen-primary, #2563eb);
    color: white;
    text-decoration: none;
    border-radius: var(--datarizen-radius-md, 0.375rem);
    font-weight: 500;
  }
  .link-runtime:hover {
    background: var(--datarizen-primary-hover, #1d4ed8);
  }
</style>
