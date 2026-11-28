(() => {
  const selector = '.cfx-topology-wrapper[data-cfx-interactive="true"]';
  const mountForPrefix = root => {
    const wrappers = root?.matches?.(selector) ? [root] : Array.from((root || document).querySelectorAll(selector));
    for (const wrapper of wrappers) {
    if (wrapper.getAttribute('data-cfx-runtime-bound') === 'true') continue;
    window[Symbol.for('ChartForgeX.strictStyles')]?.(wrapper);
    const lifetime = window[Symbol.for('ChartForgeX.adapterLifetimes')].create(wrapper, 'topology:' + selector);
    window[Symbol.for('ChartForgeX.nativeTextExport')].mount(wrapper, release => lifetime.own(release));
    const listen = (target, name, listener, options) => lifetime.listen(target, name, listener, options);
    lifetime.own(() => wrapper.removeAttribute('data-cfx-runtime-bound'));
    wrapper.setAttribute('data-cfx-runtime-bound', 'true');
    if (wrapper.hasAttribute('data-cfx-region-chart')) {
      window[Symbol.for('ChartForgeX.topologyRegions')](wrapper, lifetime);
      continue;
    }
    const uiText = (message, fallback, values) => window[Symbol.for('ChartForgeX.uiText')](wrapper, message, fallback, values);
    const selectables = '[data-cfx-role="topology-node"],[data-cfx-role="topology-edge"],[data-cfx-role="topology-group"]';
    // Edge labels and value chips are painted apart from their route but follow its visibility and emphasis.
    const edgeAnnotationSelector = '[data-cfx-role="topology-edge-label"],[data-cfx-role="topology-edge-chip"]';
    const viewportControls = wrapper.getAttribute('data-cfx-viewport-controls') === 'true';
    const exportControls = wrapper.getAttribute('data-cfx-export-controls') === 'true';
    const fullscreenControl = wrapper.getAttribute('data-cfx-fullscreen-control') === 'true';
    const scenarioControls = wrapper.getAttribute('data-cfx-scenario-controls') === 'true';
    const scenarioControlMode = wrapper.getAttribute('data-cfx-scenario-control-mode') || 'buttons';
    const forceGraphControls = wrapper.getAttribute('data-cfx-force-graph-controls') === 'true';
    const scenarioUrlState = wrapper.getAttribute('data-cfx-scenario-url-state') === 'true';
    const scenarioPanel = wrapper.querySelector('[data-cfx-topology-scenario-panel]');
    const selectionPanel = wrapper.querySelector('[data-cfx-topology-selection-panel]');
    const forceGraphPanel = wrapper.querySelector('[data-cfx-force-graph-panel]');
    const syncEnabled = wrapper.getAttribute('data-cfx-sync-enabled') === 'true';
    const syncGroup = wrapper.getAttribute('data-cfx-sync-group') || '';
    const viewport = wrapper.querySelector('.cfx-topology-viewport') || wrapper;
    const topologyRoot = wrapper.querySelector('[data-cfx-role="topology"]');
    const topologySvg = () => {
      const root = wrapper.querySelector('[data-cfx-role="topology"]');
      return root ? root.closest('svg') : (viewport.querySelector(':scope > svg') || wrapper.querySelector('svg'));
    };
    const narrowSvg = topologySvg();
    if (narrowSvg?.viewBox?.baseVal?.height > 0) wrapper.style.setProperty('--cfx-topology-aspect', String(narrowSvg.viewBox.baseVal.width / narrowSvg.viewBox.baseVal.height));
    let applyingSync = false;
    let scenarioPlayback = null;
    let forceGraphMotionTimer = null;
    // A fragment rendered before placements had keys of their own keeps the chart id's key as its placement key.
    const placementFamily = wrapper.hasAttribute('data-cfx-key') ? 'data-cfx-key' : 'data-chart-id';
    const scenarioUrlKey = (name, from = placementFamily) => {
      const chartId = attr(wrapper, from).replace(/[^a-z0-9_.-]+/gi, '-').replace(/^-+|-+$/g, '') || 'topology';
      return (from === 'data-cfx-key' ? 'cfxk-' : 'cfx-') + chartId + '-' + name;
    };
    // Scenario and step come from one set of keys: the placement's, else an older link's keyed by the chart id, else plain.
    // An older link, keyed by the chart id or plain, stays in the address for every placement that reads it; a placement
    // that clears its scenario keeps an empty key of its own, so the older link no longer speaks for it.
    const scenarioUrlName = (params, name) => { const from = ['data-cfx-key', 'data-chart-id'].filter(f => wrapper.hasAttribute(f)).find(f => params.has(scenarioUrlKey('scenario', f)) || params.has(scenarioUrlKey('scenarioStep', f))); return from ? scenarioUrlKey(name, from) : name; };
    // null when the address says nothing; an empty value is a selection the reader cleared.
    const scenarioUrlParam = name => scenarioUrlState ? (() => { try { const params = new URL(window.location.href).searchParams; const key = scenarioUrlName(params, name); return params.has(key) ? params.get(key) : null; } catch { return null; } })() : null;
    const syncScenarioUrl = (scenarioId, stepIndex) => {
      if (!scenarioUrlState || !window.history || !window.history.replaceState) return;
      try { const url = new URL(window.location.href); const scenarioKey = scenarioUrlKey('scenario'); const stepKey = scenarioUrlKey('scenarioStep'); const older = (placementFamily !== 'data-chart-id' && (url.searchParams.has(scenarioUrlKey('scenario', 'data-chart-id')) || url.searchParams.has(scenarioUrlKey('scenarioStep', 'data-chart-id')))) || url.searchParams.has('scenario') || url.searchParams.has('scenarioStep'); scenarioId || older ? url.searchParams.set(scenarioKey, scenarioId || '') : url.searchParams.delete(scenarioKey); stepIndex === undefined || stepIndex === null || stepIndex === '' ? url.searchParams.delete(stepKey) : url.searchParams.set(stepKey, stepIndex); window.history.replaceState(window.history.state, '', url); } catch { }
    };
    const attr = (element, name) => element.getAttribute(name) || '';
    // A linked node's own link is its tab stop and keeps its link semantics; focus, clicks and keys on the link act for
    // the node inside it.
    const nodeLink = element => { const host = element?.closest?.('a[data-cfx-role="topology-node-host"]'); return host && wrapper.contains(host) ? host : null; };
    const ownerOf = target => !(target instanceof Element) ? null
      : target.closest(selectables) || (target.matches('a[data-cfx-role="topology-node-host"]') ? target.querySelector(selectables) : null);
    // A node a filter hides can't take focus, as on a hidden element; its link, still in the document, mustn't either.
    const focusNode = element => {
      const link = nodeLink(element);
      if (link && nodeHidden(element)) return;
      const target = link || element;
      if (target?.focus) target.focus({ preventScroll: true });
    };
    const toCamel = value => value.replace(/-([a-z0-9])/g, (_, ch) => ch.toUpperCase());
    const collectPrefixed = (element, prefix) => {
      const values = {};
      for (const attribute of element.attributes) {
        if (attribute.name.startsWith(prefix)) values[toCamel(attribute.name.slice(prefix.length))] = attribute.value;
      }
      return values;
    };
    const unique = values => Array.from(new Set(values.filter(value => value)));
    const scenarioIdTokens = value => unique(String(value || '').split(/[,\s]+/).map(item => item.trim()));
    const scenarioIds = element => (attr(element, 'data-scenario-ids') || '').split(/\s+/).filter(value => value);
    const scenarioStepIndices = (element, scenarioId) => (attr(element, 'data-scenario-step-indices') || '').split(/\s+/).filter(value => value.startsWith(scenarioId + ':')).flatMap(value => value.slice(scenarioId.length + 1).split(',').filter(index => index));
    const findScenarioButton = scenarioId => Array.from(wrapper.querySelectorAll('[data-cfx-topology-scenario],[data-cfx-topology-scenario-toggle]')).find(button => (attr(button, 'data-cfx-topology-scenario') || attr(button, 'data-cfx-topology-scenario-toggle')) === scenarioId) || null;
    const scenarioSummaries = () => {
      try {
        const summaries = JSON.parse(attr(topologyRoot || wrapper, 'data-cfx-scenarios') || '[]');
        return Array.isArray(summaries) ? summaries : [];
      } catch {
        return [];
      }
    };
    const findScenarioSummary = scenarioId => scenarioSummaries().find(scenario => scenario && scenario.id === scenarioId) || null;
    const scenarioSteps = button => {
      try {
        const steps = JSON.parse(attr(button, 'data-cfx-scenario-steps') || '[]');
        return Array.isArray(steps) ? steps : [];
      } catch {
        return [];
      }
    };
    const scenarioMetadata = button => {
      try {
        const metadata = JSON.parse(attr(button, 'data-cfx-scenario-metadata') || '{}');
        return metadata && typeof metadata === 'object' && !Array.isArray(metadata) ? metadata : {};
      } catch {
        return {};
      }
    };
