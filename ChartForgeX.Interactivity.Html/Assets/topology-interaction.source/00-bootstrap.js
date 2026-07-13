(() => {
  for (const wrapper of document.querySelectorAll('.cfx-topology-wrapper[data-cfx-interactive="true"]')) {
    if (wrapper.getAttribute('data-cfx-runtime-bound') === 'true') continue;
    wrapper.setAttribute('data-cfx-runtime-bound', 'true');
    const selectables = '[data-cfx-role="topology-node"],[data-cfx-role="topology-edge"],[data-cfx-role="topology-group"]';
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
    let applyingSync = false;
    let scenarioPlayback = null;
    let forceGraphMotionTimer = null;
    const scenarioUrlKey = name => {
      const chartId = attr(wrapper, 'data-chart-id').replace(/[^a-z0-9_.-]+/gi, '-').replace(/^-+|-+$/g, '') || 'topology';
      return 'cfx-' + chartId + '-' + name;
    };
    const scenarioUrlParam = name => scenarioUrlState ? (() => { try { const params = new URL(window.location.href).searchParams; return params.get(scenarioUrlKey(name)) || params.get(name) || ''; } catch { return ''; } })() : '';
    const syncScenarioUrl = (scenarioId, stepIndex) => {
      if (!scenarioUrlState || !window.history || !window.history.replaceState) return;
      try { const url = new URL(window.location.href); const scenarioKey = scenarioUrlKey('scenario'); const stepKey = scenarioUrlKey('scenarioStep'); scenarioId ? url.searchParams.set(scenarioKey, scenarioId) : url.searchParams.delete(scenarioKey); stepIndex === undefined || stepIndex === null || stepIndex === '' ? url.searchParams.delete(stepKey) : url.searchParams.set(stepKey, stepIndex); url.searchParams.delete('scenario'); url.searchParams.delete('scenarioStep'); window.history.replaceState(window.history.state, '', url); } catch { }
    };
    const attr = (element, name) => element.getAttribute(name) || '';
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
