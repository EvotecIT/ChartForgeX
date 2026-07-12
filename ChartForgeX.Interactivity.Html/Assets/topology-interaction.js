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
    const scenarioRoute = scenarioId => {
      if (!scenarioId) return null;
      const scenarioButton = findScenarioButton(scenarioId);
      const scenarioSummary = scenarioButton ? null : findScenarioSummary(scenarioId);
      if (!scenarioButton && !scenarioSummary) return null;
      const steps = scenarioButton ? scenarioSteps(scenarioButton) : (Array.isArray(scenarioSummary.steps) ? scenarioSummary.steps : []);
      const metadata = scenarioButton ? scenarioMetadata(scenarioButton) : (scenarioSummary.metadata && typeof scenarioSummary.metadata === 'object' && !Array.isArray(scenarioSummary.metadata) ? scenarioSummary.metadata : {});
      const nodeIds = new Set();
      const edgeIds = new Set();
      const groupIds = new Set();
      wrapper.querySelectorAll('[data-cfx-role="topology-node"][data-scenario-ids]').forEach(node => {
        if (!scenarioIds(node).includes(scenarioId)) return;
        nodeIds.add(attr(node, 'data-node-id'));
        groupIds.add(attr(node, 'data-group-id'));
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"][data-scenario-ids]').forEach(edge => {
        if (!scenarioIds(edge).includes(scenarioId)) return;
        edgeIds.add(attr(edge, 'data-edge-id'));
        nodeIds.add(attr(edge, 'data-source-node-id'));
        nodeIds.add(attr(edge, 'data-target-node-id'));
        groupIds.add(attr(edge, 'data-source-group-id'));
        groupIds.add(attr(edge, 'data-target-group-id'));
      });
      return {
        scenarioId,
        button: scenarioButton,
        label: scenarioButton ? attr(scenarioButton, 'data-cfx-scenario-label') || scenarioButton.textContent.trim() : scenarioSummary.label || scenarioId,
        description: scenarioButton ? attr(scenarioButton, 'data-cfx-scenario-description') : scenarioSummary.description || '',
        color: scenarioButton ? attr(scenarioButton, 'data-cfx-scenario-color') : scenarioSummary.color || '',
        stepCount: scenarioButton ? Number(attr(scenarioButton, 'data-cfx-scenario-step-count') || '0') : Number(scenarioSummary.stepCount || steps.length || 0),
        metadata,
        steps,
        nodeIds,
        edgeIds,
        groupIds
      };
    };
    const renderScenarioPanel = route => {
      if (!scenarioPanel) return;
      const title = scenarioPanel.querySelector('[data-cfx-scenario-panel-title]');
      const meta = scenarioPanel.querySelector('[data-cfx-scenario-panel-meta]');
      const stepsList = scenarioPanel.querySelector('[data-cfx-scenario-panel-steps]');
      if (!route) {
        scenarioPanel.removeAttribute('data-cfx-panel-active-scenario');
        scenarioPanel.style.removeProperty('--cfx-topology-scenario-color');
        if (title) title.textContent = 'All';
        if (meta) meta.textContent = 'All topology routes visible';
        if (stepsList) stepsList.replaceChildren();
        return;
      }

      scenarioPanel.setAttribute('data-cfx-panel-active-scenario', route.scenarioId);
      if (route.color) scenarioPanel.style.setProperty('--cfx-topology-scenario-color', route.color);
      else scenarioPanel.style.removeProperty('--cfx-topology-scenario-color');
      if (title) title.textContent = route.label || route.scenarioId;
      if (meta) {
        const metadata = Object.entries(route.metadata).slice(0, 2).map(([key, value]) => key + ': ' + value);
        const summary = [route.description, route.stepCount ? route.stepCount + ' steps' : '', ...metadata].filter(value => value);
        meta.textContent = summary.join(' / ');
      }
      if (stepsList) {
        stepsList.replaceChildren();
        route.steps.forEach(step => {
          const item = document.createElement('li');
          item.setAttribute('data-cfx-scenario-step-kind', step.kind || '');
          item.setAttribute('data-cfx-scenario-step-id', step.id || '');
          item.setAttribute('data-cfx-scenario-step-index', String((Number(step.index) || 0) + 1));
          item.setAttribute('role', 'button');
          item.setAttribute('tabindex', '0');
          item.textContent = step.label || step.id || '';
          stepsList.appendChild(item);
        });
      }
    };
    const clearScenarioPreview = () => {
      wrapper.style.removeProperty('--cfx-topology-preview-scenario-color');
      wrapper.querySelectorAll('.cfx-topology-html-scenario-preview,.cfx-topology-html-scenario-preview-muted').forEach(item => {
        item.classList.remove('cfx-topology-html-scenario-preview', 'cfx-topology-html-scenario-preview-muted');
      });
    };
    const previewScenario = scenarioId => {
      clearScenarioPreview();
      if (!scenarioId) return;
      const route = scenarioRoute(scenarioId);
      if (!route) return;
      if (route.color) wrapper.style.setProperty('--cfx-topology-preview-scenario-color', route.color);
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        const active = route.edgeIds.has(attr(edge, 'data-edge-id'));
        edge.classList.toggle('cfx-topology-html-scenario-preview', active);
        edge.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const active = route.edgeIds.has(attr(label, 'data-edge-id'));
        label.classList.toggle('cfx-topology-html-scenario-preview', active);
        label.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const active = route.nodeIds.has(attr(node, 'data-node-id'));
        node.classList.toggle('cfx-topology-html-scenario-preview', active);
        node.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node-status"]').forEach(status => {
        const active = route.nodeIds.has(attr(status, 'data-node-id'));
        status.classList.toggle('cfx-topology-html-scenario-preview', active);
        status.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
        const active = route.groupIds.has(attr(group, 'data-group-id'));
        group.classList.toggle('cfx-topology-html-scenario-preview', active);
        group.classList.toggle('cfx-topology-html-scenario-preview-muted', !active);
      });
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-preview', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), scenarioId, label: route.label, description: route.description, color: route.color, stepCount: route.stepCount, metadata: route.metadata, steps: route.steps, nodeIds: Array.from(route.nodeIds), edgeIds: Array.from(route.edgeIds), groupIds: Array.from(route.groupIds) } }));
    };
    const updateScenarioStepControls = playing => {
      const hasRoute = !!scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      wrapper.querySelectorAll('[data-cfx-scenario-step-control]').forEach(button => {
        button.disabled = !hasRoute;
        if (attr(button, 'data-cfx-scenario-step-control') === 'play') button.setAttribute('aria-pressed', playing ? 'true' : 'false');
      });
    };
    const clearScenarioStep = () => {
      wrapper.removeAttribute('data-cfx-active-scenario-step');
      wrapper.querySelectorAll('.cfx-topology-html-scenario-step-active,.cfx-topology-html-scenario-step-muted').forEach(item => item.classList.remove('cfx-topology-html-scenario-step-active', 'cfx-topology-html-scenario-step-muted'));
      if (scenarioPanel) scenarioPanel.querySelectorAll('[data-cfx-scenario-step-id]').forEach(item => item.removeAttribute('aria-current'));
      updateScenarioStepControls(false);
    };
    const stopScenarioPlayback = () => {
      if (scenarioPlayback) window.clearInterval(scenarioPlayback);
      scenarioPlayback = null;
      updateScenarioStepControls(false);
    };
    const setScenarioStep = (stepIndex, keepPlaying, emit = true, sync = true) => {
      const route = scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      if (!route || !route.steps.length) return;
      const requested = Number(stepIndex);
      const index = clamp(Number.isFinite(requested) ? requested : 0, 0, route.steps.length - 1);
      wrapper.setAttribute('data-cfx-active-scenario-step', String(index));
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"],[data-cfx-role="topology-node"]').forEach(item => {
        const routeMember = item.classList.contains('cfx-topology-html-scenario-active');
        const current = routeMember && scenarioStepIndices(item, route.scenarioId).includes(String(index));
        item.classList.toggle('cfx-topology-html-scenario-step-active', current);
        item.classList.toggle('cfx-topology-html-scenario-step-muted', routeMember && !current);
      });
      if (scenarioPanel) scenarioPanel.querySelectorAll('[data-cfx-scenario-step-id]').forEach(item => {
        if (attr(item, 'data-cfx-scenario-step-index') === String(index + 1)) item.setAttribute('aria-current', 'step');
        else item.removeAttribute('aria-current');
      });
      if (!keepPlaying) stopScenarioPlayback();
      if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-step', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), scenarioId: route.scenarioId, index, step: route.steps[index] } }));
      syncScenarioUrl(route.scenarioId, index);
      if (sync) emitSync('scenario-step', { scenarioId: route.scenarioId, index });
    };
    const stepScenario = delta => {
      const route = scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      if (!route) return;
      const current = Number(wrapper.getAttribute('data-cfx-active-scenario-step') || (delta > 0 ? '-1' : route.steps.length));
      setScenarioStep(current + delta, false);
    };
    const playScenario = () => {
      const route = scenarioRoute(attr(wrapper, 'data-cfx-active-scenario'));
      if (!route || !route.steps.length) return;
      if (scenarioPlayback) {
        stopScenarioPlayback();
        return;
      }
      updateScenarioStepControls(true);
      let index = Number(wrapper.getAttribute('data-cfx-active-scenario-step') || '-1') + 1;
      if (!Number.isFinite(index) || index >= route.steps.length) index = 0;
      scenarioPlayback = window.setInterval(() => {
        if (index >= route.steps.length) {
          stopScenarioPlayback();
          return;
        }
        setScenarioStep(index++, true);
      }, 900);
      setScenarioStep(index++, true);
    };
    const copyScenarioLink = () => { syncScenarioUrl(attr(wrapper, 'data-cfx-active-scenario') || attr(wrapper, 'data-cfx-active-scenarios'), wrapper.getAttribute('data-cfx-active-scenario-step') || ''); const url = window.location.href; if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(url).catch(() => {}); wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-link', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), url } })); };
    const emitFullscreenState = () => wrapper.dispatchEvent(new CustomEvent('cfx-topology-fullscreen', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), fullscreen: document.fullscreenElement === wrapper } }));
    const setFullscreenState = () => { wrapper.setAttribute('data-cfx-topology-fullscreen', document.fullscreenElement === wrapper ? 'true' : 'false'); emitFullscreenState(); };
    const toggleFullscreen = () => {
      if (!document.fullscreenElement && wrapper.requestFullscreen) wrapper.requestFullscreen().catch(() => {});
      else if (document.fullscreenElement === wrapper && document.exitFullscreen) document.exitFullscreen().catch(() => {});
    };
    const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
    const numberAttr = (name, fallback) => {
      const value = Number(wrapper.getAttribute(name) || '');
      return Number.isFinite(value) ? value : fallback;
    };
    const viewportState = () => ({
      zoom: numberAttr('data-cfx-topology-zoom', 1),
      panX: numberAttr('data-cfx-topology-pan-x', 0),
      panY: numberAttr('data-cfx-topology-pan-y', 0)
    });
    const emitSync = (action, payload) => {
      if (!syncEnabled || !syncGroup || applyingSync) return;
      const detail = Object.assign({ chartId: attr(wrapper, 'data-chart-id'), group: syncGroup, action }, payload || {});
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-sync', { bubbles: true, detail }));
      document.querySelectorAll('.cfx-topology-wrapper[data-cfx-interactive="true"][data-cfx-sync-enabled="true"]').forEach(peer => {
        if (peer === wrapper || peer.getAttribute('data-cfx-sync-group') !== syncGroup) return;
        peer.dispatchEvent(new CustomEvent('cfx-topology-apply-sync', { detail }));
      });
    };
    const applyViewport = state => {
      if (!viewportControls) return;
      const next = {
        zoom: clamp(Number(state.zoom) || 1, 0.5, 4),
        panX: clamp(Number(state.panX) || 0, -4000, 4000),
        panY: clamp(Number(state.panY) || 0, -4000, 4000)
      };
      wrapper.setAttribute('data-cfx-topology-zoom', next.zoom.toFixed(3));
      wrapper.setAttribute('data-cfx-topology-pan-x', next.panX.toFixed(1));
      wrapper.setAttribute('data-cfx-topology-pan-y', next.panY.toFixed(1));
      wrapper.style.setProperty('--cfx-topology-zoom', next.zoom.toFixed(3));
      wrapper.style.setProperty('--cfx-topology-pan-x', next.panX.toFixed(1) + 'px');
      wrapper.style.setProperty('--cfx-topology-pan-y', next.panY.toFixed(1) + 'px');
    };
    const viewportMetrics = () => {
      const svg = topologySvg();
      const viewBox = svg && svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
      const rect = viewport.getBoundingClientRect ? viewport.getBoundingClientRect() : { width: 0, height: 0 };
      const styles = window.getComputedStyle ? window.getComputedStyle(viewport) : null;
      const paddingTop = styles ? Number.parseFloat(styles.paddingTop || '0') || 0 : 0;
      const paddingLeft = styles ? Number.parseFloat(styles.paddingLeft || '0') || 0 : 0;
      const paddingRight = styles ? Number.parseFloat(styles.paddingRight || '0') || 0 : 0;
      const paddingBottom = styles ? Number.parseFloat(styles.paddingBottom || '0') || 0 : 0;
      const width = Math.max(1, rect.width - paddingLeft - paddingRight);
      const availableHeight = Math.max(1, rect.height - paddingTop - paddingBottom);
      const svgWidth = Math.max(1, width);
      const svgHeight = viewBox && viewBox.width > 0 && viewBox.height > 0 ? svgWidth * (viewBox.height / viewBox.width) : Math.max(1, svg ? svg.getBoundingClientRect().height : availableHeight);
      return { rect, paddingTop, paddingLeft, width, availableHeight, svgWidth, svgHeight };
    };
    const emitViewport = () => {
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-viewport', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState() } }));
      emitSync('viewport', { state: viewportState() });
    };
    const zoomBy = (factor, origin) => {
      const state = viewportState();
      const nextZoom = clamp(state.zoom * factor, 0.5, 4);
      const ratio = nextZoom / state.zoom;
      const metrics = viewportMetrics();
      const localX = origin && Number.isFinite(origin.clientX) ? origin.clientX - metrics.rect.left : metrics.rect.width / 2;
      const localY = origin && Number.isFinite(origin.clientY) ? origin.clientY - metrics.rect.top : metrics.rect.height / 2;
      const centerX = metrics.rect.width / 2;
      const centerY = metrics.rect.height / 2;
      const panX = localX - centerX - ratio * (localX - centerX - state.panX);
      const panY = localY - centerY - ratio * (localY - centerY - state.panY);
      markForceGraphMoving();
      applyViewport({ zoom: nextZoom, panX, panY });
      emitViewport();
    };
    const fitViewport = () => {
      const metrics = viewportMetrics();
      const scale = clamp(Math.min(1, metrics.width / metrics.svgWidth, metrics.availableHeight / metrics.svgHeight), 0.5, 1);
      applyViewport({ zoom: scale, panX: 0, panY: 0 });
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-fit', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState() } }));
      emitViewport();
    };
    const isViewportVisible = element => {
      if (!element || !(element instanceof Element)) return false;
      if (element.classList.contains('cfx-topology-html-filter-hidden') || element.classList.contains('cfx-topology-html-force-hidden')) return false;
      if (element.closest('.cfx-topology-html-filter-hidden,.cfx-topology-html-force-hidden')) return false;
      return true;
    };
    const elementBounds = element => {
      try {
        if (element && element.getBBox) {
          const box = element.getBBox();
          if (box && Number.isFinite(box.x) && Number.isFinite(box.y) && box.width > 0 && box.height > 0) return box;
        }
      } catch { }
      return null;
    };
    const fitViewportToElements = (elements, emit = true) => {
      if (!viewportControls) return false;
      const svg = topologySvg();
      const viewBox = svg && svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
      if (!svg || !viewBox || viewBox.width <= 0 || viewBox.height <= 0) return false;
      const boxes = Array.from(elements || []).filter(isViewportVisible).map(elementBounds).filter(box => box);
      if (!boxes.length) return false;
      const bounds = boxes.reduce((current, box) => ({
        x: Math.min(current.x, box.x),
        y: Math.min(current.y, box.y),
        right: Math.max(current.right, box.x + box.width),
        bottom: Math.max(current.bottom, box.y + box.height)
      }), { x: boxes[0].x, y: boxes[0].y, right: boxes[0].x + boxes[0].width, bottom: boxes[0].y + boxes[0].height });
      const metrics = viewportMetrics();
      const unitX = metrics.svgWidth / viewBox.width;
      const unitY = metrics.svgHeight / viewBox.height;
      const boundsWidth = Math.max(1, (bounds.right - bounds.x) * unitX);
      const boundsHeight = Math.max(1, (bounds.bottom - bounds.y) * unitY);
      const padding = 44;
      const zoom = clamp(Math.min(metrics.width / (boundsWidth + padding * 2), metrics.availableHeight / (boundsHeight + padding * 2)), 0.5, 3.2);
      const centerX = (bounds.x + (bounds.right - bounds.x) / 2 - viewBox.x) * unitX;
      const centerY = (bounds.y + (bounds.bottom - bounds.y) / 2 - viewBox.y) * unitY;
      const originX = metrics.svgWidth / 2;
      const originY = metrics.svgHeight / 2;
      const targetX = metrics.width / 2;
      const targetY = metrics.availableHeight / 2;
      const panX = targetX - originX - zoom * (centerX - originX);
      const panY = targetY - originY - zoom * (centerY - originY);
      applyViewport({ zoom, panX, panY });
      if (emit) {
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-fit', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState(), mode: 'visible', count: boxes.length } }));
        emitViewport();
      }
      return true;
    };
    const fitVisibleTopology = (emit = true) => fitViewportToElements(wrapper.querySelectorAll('[data-cfx-role="topology-node"]:not(.cfx-topology-html-filter-hidden):not(.cfx-topology-html-force-hidden)'), emit);
    const markForceGraphMoving = () => {
      if (!forceGraphControls || wrapper.getAttribute('data-cfx-force-hide-moving-edges') !== 'true') return;
      wrapper.setAttribute('data-cfx-force-moving-edges', 'true');
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => edge.classList.add('cfx-topology-html-force-moving'));
      if (forceGraphMotionTimer) window.clearTimeout(forceGraphMotionTimer);
      forceGraphMotionTimer = window.setTimeout(() => {
        wrapper.removeAttribute('data-cfx-force-moving-edges');
        wrapper.querySelectorAll('.cfx-topology-html-force-moving').forEach(edge => edge.classList.remove('cfx-topology-html-force-moving'));
      }, 280);
    };
    const resetViewport = () => {
      applyViewport({ zoom: 1, panX: 0, panY: 0 });
      wrapper.removeAttribute('data-cfx-topology-dragging');
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-reset', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id') } }));
      emitViewport();
    };
    const setViewportMode = mode => {
      const next = wrapper.getAttribute('data-cfx-topology-mode') === mode ? '' : mode;
      if (next) wrapper.setAttribute('data-cfx-topology-mode', next);
      else wrapper.removeAttribute('data-cfx-topology-mode');
      wrapper.querySelectorAll('[data-cfx-topology-mode]').forEach(button => button.setAttribute('aria-pressed', attr(button, 'data-cfx-topology-mode') === next ? 'true' : 'false'));
    };
    const clearScenario = () => {
      stopScenarioPlayback();
      clearScenarioStep();
      clearScenarioPreview();
      wrapper.removeAttribute('data-cfx-active-scenario');
      wrapper.removeAttribute('data-cfx-active-scenarios');
      wrapper.style.removeProperty('--cfx-topology-active-scenario-color');
      wrapper.querySelectorAll('.cfx-topology-html-scenario-active,.cfx-topology-html-scenario-muted').forEach(item => {
        item.classList.remove('cfx-topology-html-scenario-active', 'cfx-topology-html-scenario-muted');
        item.removeAttribute('data-cfx-active-scenario-step-indices');
      });
      wrapper.querySelectorAll('[data-cfx-topology-scenario]').forEach(button => button.setAttribute('aria-pressed', attr(button, 'data-cfx-topology-scenario') ? 'false' : 'true'));
      wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]').forEach(input => input.checked = false);
      updateScenarioStepControls(false);
      renderScenarioPanel(null);
      syncScenarioUrl('', '');
    };
    const setScenarioFilters = (scenarioIds, emit = true, sync = true) => {
      clearScenario();
      const ids = unique((scenarioIds || []).map(value => String(value || '').trim()));
      if (!ids.length) {
        if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-clear', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id') } }));
        if (sync) emitSync('scenario-filters', { scenarioIds: [] });
        return;
      }

      const routes = ids.map(scenarioRoute).filter(route => route);
      if (!routes.length) {
        if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-clear', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id') } }));
        if (sync) emitSync('scenario-filters', { scenarioIds: [] });
        return;
      }

      const nodeIds = new Set();
      const edgeIds = new Set();
      const groupIds = new Set();
      routes.forEach(route => {
        route.nodeIds.forEach(id => nodeIds.add(id));
        route.edgeIds.forEach(id => edgeIds.add(id));
        route.groupIds.forEach(id => groupIds.add(id));
      });
      wrapper.setAttribute('data-cfx-active-scenarios', routes.map(route => route.scenarioId).join(' '));
      if (routes.length === 1) {
        wrapper.setAttribute('data-cfx-active-scenario', routes[0].scenarioId);
        if (routes[0].color) wrapper.style.setProperty('--cfx-topology-active-scenario-color', routes[0].color);
        else wrapper.style.removeProperty('--cfx-topology-active-scenario-color');
      } else {
        wrapper.removeAttribute('data-cfx-active-scenario');
        wrapper.style.removeProperty('--cfx-topology-active-scenario-color');
      }
      wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]').forEach(input => input.checked = routes.some(route => route.scenarioId === attr(input, 'data-cfx-topology-scenario-toggle')));
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        const active = edgeIds.has(attr(edge, 'data-edge-id'));
        edge.classList.toggle('cfx-topology-html-scenario-active', active);
        edge.classList.toggle('cfx-topology-html-scenario-muted', !active);
        if (active && routes.length === 1) edge.setAttribute('data-cfx-active-scenario-step-indices', scenarioStepIndices(edge, routes[0].scenarioId).join(' '));
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const active = nodeIds.has(attr(node, 'data-node-id'));
        node.classList.toggle('cfx-topology-html-scenario-active', active);
        node.classList.toggle('cfx-topology-html-scenario-muted', !active);
        if (active && routes.length === 1) node.setAttribute('data-cfx-active-scenario-step-indices', scenarioStepIndices(node, routes[0].scenarioId).join(' '));
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const active = edgeIds.has(attr(label, 'data-edge-id'));
        label.classList.toggle('cfx-topology-html-scenario-active', active);
        label.classList.toggle('cfx-topology-html-scenario-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node-status"]').forEach(status => {
        const active = nodeIds.has(attr(status, 'data-node-id'));
        status.classList.toggle('cfx-topology-html-scenario-active', active);
        status.classList.toggle('cfx-topology-html-scenario-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
        const active = groupIds.has(attr(group, 'data-group-id'));
        group.classList.toggle('cfx-topology-html-scenario-active', active);
        group.classList.toggle('cfx-topology-html-scenario-muted', !active);
      });
      renderScenarioPanel(routes.length === 1 ? routes[0] : { ...routes[0], label: routes.length + ' routes enabled', description: routes.map(route => route.label).join(' / '), stepCount: routes.reduce((sum, route) => sum + route.stepCount, 0), steps: routes.flatMap(route => route.steps), metadata: {} });
      updateScenarioStepControls(false);
      syncScenarioUrl(routes.map(route => route.scenarioId).join(','), '');
      if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-filter', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), scenarioIds: routes.map(route => route.scenarioId), routes: routes.map(route => ({ scenarioId: route.scenarioId, label: route.label, description: route.description, color: route.color, stepCount: route.stepCount })), nodeIds: Array.from(nodeIds), edgeIds: Array.from(edgeIds), groupIds: Array.from(groupIds) } }));
      if (sync) emitSync('scenario-filters', { scenarioIds: routes.map(route => route.scenarioId) });
    };
    const setScenario = (scenarioId, emit = true, sync = true) => {
      clearScenario();
      if (!scenarioId) {
        if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-clear', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id') } }));
        if (sync) emitSync('scenario', { scenarioId: '' });
        return;
      }
      const route = scenarioRoute(scenarioId);
      if (!route) {
        if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-clear', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id') } }));
        if (sync) emitSync('scenario', { scenarioId: '' });
        return;
      }

      wrapper.setAttribute('data-cfx-active-scenario', scenarioId);
      if (scenarioControlMode === 'checkboxes') {
        wrapper.setAttribute('data-cfx-active-scenarios', scenarioId);
        wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]').forEach(input => input.checked = attr(input, 'data-cfx-topology-scenario-toggle') === scenarioId);
      }
      if (route.color) wrapper.style.setProperty('--cfx-topology-active-scenario-color', route.color);
      else wrapper.style.removeProperty('--cfx-topology-active-scenario-color');
      wrapper.querySelectorAll('[data-cfx-topology-scenario]').forEach(button => button.setAttribute('aria-pressed', attr(button, 'data-cfx-topology-scenario') === scenarioId ? 'true' : 'false'));
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        const active = route.edgeIds.has(attr(edge, 'data-edge-id'));
        edge.classList.toggle('cfx-topology-html-scenario-active', active);
        edge.classList.toggle('cfx-topology-html-scenario-muted', !active);
        if (active) edge.setAttribute('data-cfx-active-scenario-step-indices', scenarioStepIndices(edge, scenarioId).join(' '));
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const active = route.nodeIds.has(attr(node, 'data-node-id'));
        node.classList.toggle('cfx-topology-html-scenario-active', active);
        node.classList.toggle('cfx-topology-html-scenario-muted', !active);
        if (active) node.setAttribute('data-cfx-active-scenario-step-indices', scenarioStepIndices(node, scenarioId).join(' '));
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const active = route.edgeIds.has(attr(label, 'data-edge-id'));
        label.classList.toggle('cfx-topology-html-scenario-active', active);
        label.classList.toggle('cfx-topology-html-scenario-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node-status"]').forEach(status => {
        const active = route.nodeIds.has(attr(status, 'data-node-id'));
        status.classList.toggle('cfx-topology-html-scenario-active', active);
        status.classList.toggle('cfx-topology-html-scenario-muted', !active);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
        const active = route.groupIds.has(attr(group, 'data-group-id'));
        group.classList.toggle('cfx-topology-html-scenario-active', active);
        group.classList.toggle('cfx-topology-html-scenario-muted', !active);
      });
      renderScenarioPanel(route);
      clearScenarioStep();
      updateScenarioStepControls(false);
      syncScenarioUrl(scenarioId, '');
      if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), scenarioId, label: route.label, description: route.description, color: route.color, stepCount: route.stepCount, metadata: route.metadata, steps: route.steps, nodeIds: Array.from(route.nodeIds), edgeIds: Array.from(route.edgeIds), groupIds: Array.from(route.groupIds) } }));
      if (sync) emitSync('scenario', { scenarioId });
    };
    const svgElement = () => topologySvg();
    const exportName = () => (attr(wrapper, 'data-chart-id') || 'topology').replace(/[^a-z0-9_.-]+/gi, '-').replace(/^-+|-+$/g, '') || 'topology';
    const serializeSvg = () => {
      const svg = svgElement();
      if (!svg) return null;
      const clone = svg.cloneNode(true);
      const defs = clone.querySelector('defs') || clone.insertBefore(document.createElementNS('http://www.w3.org/2000/svg', 'defs'), clone.firstChild);
      const style = document.createElementNS('http://www.w3.org/2000/svg', 'style');
      style.setAttribute('data-cfx-export-style', 'force-filters');
      style.textContent = '.cfx-topology-html-force-hidden{display:none!important}';
      defs.appendChild(style);
      return { svg, data: new XMLSerializer().serializeToString(clone) };
    };
    const emitExport = format => {
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-export', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), format } }));
    };
    const downloadBlob = (blob, extension, format) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = exportName() + '.' + extension;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      emitExport(format);
    };
    const svgSize = svg => {
      const viewBox = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
      const rect = svg.getBoundingClientRect ? svg.getBoundingClientRect() : { width: 0, height: 0 };
      const width = Math.max(1, Math.round((viewBox && viewBox.width) || (svg.width && svg.width.baseVal ? svg.width.baseVal.value : 0) || rect.width || 800));
      const height = Math.max(1, Math.round((viewBox && viewBox.height) || (svg.height && svg.height.baseVal ? svg.height.baseVal.value : 0) || rect.height || 450));
      return { width, height };
    };
    const exportSvg = () => {
      const serialized = serializeSvg();
      if (!serialized) return;
      downloadBlob(new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' }), 'svg', 'svg');
    };
    const exportPng = () => {
      const serialized = serializeSvg();
      if (!serialized) return;
      const source = new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' });
      const sourceUrl = URL.createObjectURL(source);
      const image = new Image();
      image.onload = () => {
        const size = svgSize(serialized.svg);
        const scale = Math.max(1, Math.ceil(window.devicePixelRatio || 1));
        const canvas = document.createElement('canvas');
        canvas.width = size.width * scale;
        canvas.height = size.height * scale;
        const context = canvas.getContext('2d');
        if (!context) {
          URL.revokeObjectURL(sourceUrl);
          return;
        }
        context.setTransform(scale, 0, 0, scale, 0, 0);
        context.clearRect(0, 0, size.width, size.height);
        context.drawImage(image, 0, 0, size.width, size.height);
        canvas.toBlob(blob => {
          URL.revokeObjectURL(sourceUrl);
          if (blob) downloadBlob(blob, 'png', 'png');
        }, 'image/png');
      };
      image.onerror = () => URL.revokeObjectURL(sourceUrl);
      image.src = sourceUrl;
    };
    const edgeIdentity = edge => ({
      id: attr(edge, 'data-edge-id'),
      kind: attr(edge, 'data-edge-kind'),
      status: attr(edge, 'data-cfx-status'),
      sourceNodeId: attr(edge, 'data-source-node-id'),
      targetNodeId: attr(edge, 'data-target-node-id'),
      sourceGroupId: attr(edge, 'data-source-group-id'),
      targetGroupId: attr(edge, 'data-target-group-id')
    });
    const related = detail => {
      if (detail.kind === 'node') {
        const edges = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-edge"]'))
          .filter(edge => attr(edge, 'data-source-node-id') === detail.id || attr(edge, 'data-target-node-id') === detail.id);
        return {
          nodeIds: unique(edges.flatMap(edge => [attr(edge, 'data-source-node-id'), attr(edge, 'data-target-node-id')]).filter(id => id !== detail.id)),
          edgeIds: edges.map(edge => attr(edge, 'data-edge-id')),
          groupIds: unique([detail.groupId].concat(edges.flatMap(edge => [attr(edge, 'data-source-group-id'), attr(edge, 'data-target-group-id')]))),
          edges: edges.map(edgeIdentity)
        };
      }
      if (detail.kind === 'edge') {
        return {
          nodeIds: unique([detail.sourceNodeId, detail.targetNodeId]),
          edgeIds: [detail.id],
          groupIds: unique([detail.sourceGroupId, detail.targetGroupId]),
          edges: [edgeIdentity(detail.element)]
        };
      }
      if (detail.kind === 'group') {
        const nodes = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-node"]'))
          .filter(node => attr(node, 'data-group-id') === detail.id);
        const edges = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-edge"]'))
          .filter(edge => attr(edge, 'data-source-group-id') === detail.id || attr(edge, 'data-target-group-id') === detail.id);
        return {
          nodeIds: nodes.map(node => attr(node, 'data-node-id')),
          edgeIds: edges.map(edge => attr(edge, 'data-edge-id')),
          groupIds: unique(edges.flatMap(edge => [attr(edge, 'data-source-group-id'), attr(edge, 'data-target-group-id')]).filter(id => id !== detail.id)),
          edges: edges.map(edgeIdentity)
        };
      }
      return { nodeIds: [], edgeIds: [], groupIds: [], edges: [] };
    };
    const statusColor = status => {
      const key = String(status || '').toLowerCase();
      if (key === 'healthy') return 'var(--cfx-topology-healthy,#16a34a)';
      if (key === 'warning') return 'var(--cfx-topology-warning,#f97316)';
      if (key === 'critical') return 'var(--cfx-topology-critical,#ef4444)';
      if (key === 'disabled') return 'var(--cfx-topology-disabled,#94a3b8)';
      return 'var(--cfx-topology-unknown,#64748b)';
    };
    const panelRowClass = () => {
      if (!selectionPanel) return 'cfx-topology-selection-panel__row';
      const base = (selectionPanel.getAttribute('class') || 'cfx-topology-selection-panel').split(/\s+/)[0] || 'cfx-topology-selection-panel';
      return base + '__row';
    };
    const addSelectionRow = (container, label, value) => {
      if (!container || value === undefined || value === null || value === '') return;
      const row = document.createElement('div');
      row.className = panelRowClass();
      const left = document.createElement('span');
      const right = document.createElement('span');
      left.textContent = label;
      right.textContent = String(value);
      row.append(left, right);
      container.appendChild(row);
    };
    const setSelectionFacts = (container, rows) => {
      if (!container) return;
      container.replaceChildren();
      rows.forEach(row => {
        if (!row || row[1] === undefined || row[1] === null || row[1] === '') return;
        const dt = document.createElement('dt');
        const dd = document.createElement('dd');
        dt.textContent = row[0];
        dd.textContent = String(row[1]);
        container.append(dt, dd);
      });
    };
    const renderSelectionPanel = detail => {
      if (!selectionPanel) return;
      const title = selectionPanel.querySelector('[data-cfx-selection-title]');
      const kind = selectionPanel.querySelector('[data-cfx-selection-kind]');
      const status = selectionPanel.querySelector('[data-cfx-selection-status]');
      const facts = selectionPanel.querySelector('[data-cfx-selection-facts]');
      const meta = selectionPanel.querySelector('[data-cfx-selection-meta]');
      const relatedPanel = selectionPanel.querySelector('[data-cfx-selection-related]');
      if (!detail) {
        selectionPanel.hidden = true;
        selectionPanel.style.removeProperty('--cfx-topology-selection-status-color');
        if (title) title.textContent = 'No item selected';
        if (kind) kind.textContent = 'Selection';
        if (status) status.textContent = '';
        if (facts) facts.replaceChildren();
        if (meta) meta.replaceChildren();
        if (relatedPanel) relatedPanel.replaceChildren();
        return;
      }

      selectionPanel.hidden = false;
      selectionPanel.style.setProperty('--cfx-topology-selection-status-color', statusColor(detail.status));
      const displayTitle = detail.kind === 'node'
        ? (detail.metadata && detail.metadata.name) || (detail.element ? attr(detail.element, 'data-node-label') : '') || detail.id
        : detail.kind === 'edge'
          ? (detail.element ? attr(detail.element, 'data-edge-label') : '') || detail.id
          : (detail.element ? attr(detail.element, 'data-group-label') : '') || detail.id;
      if (title) title.textContent = displayTitle || detail.id || 'Selection';
      if (kind) kind.textContent = detail.kind === 'node' ? (detail.nodeKind || 'Node') : detail.kind === 'edge' ? (detail.edgeKind || 'Edge') : 'Group';
      if (status) status.textContent = detail.status || '';
      setSelectionFacts(facts, [
        ['ID', detail.id],
        ['Status', detail.status],
        ['Group', detail.groupId || detail.sourceGroupId || detail.targetGroupId],
        ['Related nodes', (detail.related && detail.related.nodeIds || []).length],
        ['Related edges', (detail.related && detail.related.edgeIds || []).length]
      ]);
      if (meta) {
        meta.replaceChildren();
        Object.entries(detail.metadata || {}).slice(0, 6).forEach(([key, value]) => addSelectionRow(meta, key, value));
        Object.entries(detail.metrics || {}).slice(0, 4).forEach(([key, value]) => addSelectionRow(meta, key, value));
      }
      if (relatedPanel) {
        relatedPanel.replaceChildren();
        (detail.related && detail.related.edges || []).slice(0, 5).forEach(edge => {
          const label = edge.kind || 'Edge';
          const value = [edge.sourceNodeId, edge.targetNodeId].filter(Boolean).join(' -> ');
          addSelectionRow(relatedPanel, label, value || edge.id);
        });
      }
    };
    const clear = () => {
      wrapper.removeAttribute('data-cfx-selection-kind');
      wrapper.removeAttribute('data-cfx-selection-id');
      wrapper.removeAttribute('data-cfx-selection-status');
      wrapper.querySelectorAll('.cfx-topology-html-selected,.cfx-topology-html-muted,.cfx-topology-html-related').forEach(item => {
        item.classList.remove('cfx-topology-html-selected', 'cfx-topology-html-muted', 'cfx-topology-html-related');
        item.removeAttribute('aria-selected');
      });
      clearForceFocusLabels();
      renderSelectionPanel(null);
    };
    const clearHover = () => {
      wrapper.removeAttribute('data-cfx-hover-kind');
      wrapper.removeAttribute('data-cfx-hover-id');
      wrapper.querySelectorAll('.cfx-topology-html-hovered,.cfx-topology-html-hover-muted,.cfx-topology-html-hover-related').forEach(item => {
        item.classList.remove('cfx-topology-html-hovered', 'cfx-topology-html-hover-muted', 'cfx-topology-html-hover-related');
      });
      restoreForceFocusLabels();
    };
    const identity = element => {
      const role = element.getAttribute('data-cfx-role') || '';
      const base = {
        role,
        elementId: attr(element, 'id'),
        status: attr(element, 'data-cfx-status'),
        selected: attr(element, 'data-cfx-selected') === 'true',
        metadata: collectPrefixed(element, 'data-cfx-meta-'),
        metrics: collectPrefixed(element, 'data-cfx-metric-'),
        element
      };
      if (role === 'topology-node') {
        return {
          ...base,
          kind: 'node',
          id: attr(element, 'data-node-id'),
          label: attr(element, 'data-node-label'),
          nodeKind: attr(element, 'data-node-kind'),
          groupId: attr(element, 'data-group-id'),
          displayMode: attr(element, 'data-node-display-mode'),
          badge: attr(element, 'data-node-badge'),
          color: attr(element, 'data-node-color'),
          iconId: attr(element, 'data-node-icon-id'),
          iconPack: attr(element, 'data-node-icon-pack'),
          iconLabel: attr(element, 'data-node-icon-label'),
          iconShape: attr(element, 'data-node-icon-shape'),
          iconArtwork: attr(element, 'data-node-icon-artwork'),
          artworkSource: attr(element, 'data-node-artwork-source'),
          longitude: attr(element, 'data-node-longitude'),
          latitude: attr(element, 'data-node-latitude'),
          geoVisible: attr(element, 'data-node-geo-visible')
        };
      }
      if (role === 'topology-edge') {
        return {
          ...base,
          kind: 'edge',
          id: attr(element, 'data-edge-id'),
          label: attr(element, 'data-edge-label'),
          edgeKind: attr(element, 'data-edge-kind'),
          sourceNodeId: attr(element, 'data-source-node-id'),
          targetNodeId: attr(element, 'data-target-node-id'),
          sourceGroupId: attr(element, 'data-source-group-id'),
          targetGroupId: attr(element, 'data-target-group-id'),
          sourcePort: attr(element, 'data-source-port'),
          targetPort: attr(element, 'data-target-port'),
          routeLane: attr(element, 'data-route-lane'),
          layoutInference: attr(element, 'data-edge-layout-inference'),
          muted: attr(element, 'data-edge-muted') === 'true',
          lineStyle: attr(element, 'data-edge-line-style'),
          route: {
            strategy: attr(element, 'data-route-strategy'),
            curve: attr(element, 'data-route-curve'),
            controlX: attr(element, 'data-route-control-x'),
            controlY: attr(element, 'data-route-control-y'),
            corridor: attr(element, 'data-route-corridor'),
            candidateCount: attr(element, 'data-route-candidate-count'),
            fallbackReason: attr(element, 'data-route-fallback-reason'),
            segmentCount: attr(element, 'data-route-segment-count'),
            obstacleCount: attr(element, 'data-route-obstacle-count'),
            obstacleHits: attr(element, 'data-route-obstacle-hits'),
            labelObstacleHits: attr(element, 'data-route-label-obstacle-hits'),
            overlapScore: attr(element, 'data-route-overlap-score'),
            offset: attr(element, 'data-route-offset'),
            waypointCount: attr(element, 'data-waypoint-count')
          }
        };
      }
      if (role === 'topology-group') {
        return {
          ...base,
          kind: 'group',
          id: attr(element, 'data-group-id'),
          label: attr(element, 'data-group-label'),
          symbol: attr(element, 'data-group-symbol'),
          color: attr(element, 'data-group-color'),
          iconId: attr(element, 'data-group-icon-id'),
          iconPack: attr(element, 'data-group-icon-pack'),
          iconLabel: attr(element, 'data-group-icon-label'),
          iconShape: attr(element, 'data-group-icon-shape'),
          iconArtwork: attr(element, 'data-group-icon-artwork'),
          longitude: attr(element, 'data-group-longitude'),
          latitude: attr(element, 'data-group-latitude'),
          geoVisible: attr(element, 'data-group-geo-visible'),
          layoutPolicy: attr(element, 'data-group-layout-policy'),
          appliedLayoutPolicy: attr(element, 'data-group-applied-layout-policy'),
          callout: {
            visualRole: attr(element, 'data-cfx-visual-role'),
            placement: attr(element, 'data-callout-placement'),
            anchorX: attr(element, 'data-callout-anchor-x'),
            anchorY: attr(element, 'data-callout-anchor-y'),
            nodeCount: attr(element, 'data-callout-node-count'),
            healthyCount: attr(element, 'data-callout-healthy-count'),
            warningCount: attr(element, 'data-callout-warning-count'),
            criticalCount: attr(element, 'data-callout-critical-count'),
            unknownCount: attr(element, 'data-callout-unknown-count'),
            disabledCount: attr(element, 'data-callout-disabled-count')
          }
        };
      }
      return { ...base, kind: 'unknown', id: '' };
    };
    const findSelection = request => {
      if (!request || !request.kind || !request.id) return null;
      return Array.from(wrapper.querySelectorAll(selectables)).find(element => {
        const detail = identity(element);
        return detail.kind === request.kind && detail.id === request.id;
      }) || null;
    };
    const elementKey = element => {
      const detail = identity(element);
      return detail.kind + ':' + detail.id;
    };
    const publicDetail = element => {
      const detail = identity(element);
      detail.related = related(detail);
      delete detail.element;
      return detail;
    };
    const relatedElements = detail => {
      const items = [];
      const seen = new Set();
      const add = (kind, id) => {
        if (!id) return;
        const key = kind + ':' + id;
        if (seen.has(key) || (detail.kind === kind && detail.id === id)) return;
        const element = findSelection({ kind, id });
        if (!element) return;
        seen.add(key);
        items.push(element);
      };
      (detail.related.nodeIds || []).forEach(id => add('node', id));
      (detail.related.edgeIds || []).forEach(id => add('edge', id));
      (detail.related.groupIds || []).forEach(id => add('group', id));
      return items;
    };
    const focusRelated = (element, offset) => {
      const detail = identity(element);
      detail.related = related(detail);
      const candidates = relatedElements(detail);
      if (!candidates.length) return false;
      const sourceKey = detail.kind + ':' + detail.id;
      const previousTarget = wrapper.getAttribute('data-cfx-navigation-source') === sourceKey ? wrapper.getAttribute('data-cfx-navigation-target') || '' : '';
      const previousIndex = candidates.findIndex(candidate => elementKey(candidate) === previousTarget);
      const nextIndex = previousIndex >= 0 ? (previousIndex + offset + candidates.length) % candidates.length : (offset > 0 ? 0 : candidates.length - 1);
      const target = candidates[nextIndex];
      const targetKey = elementKey(target);
      wrapper.setAttribute('data-cfx-navigation-source', sourceKey);
      wrapper.setAttribute('data-cfx-navigation-target', targetKey);
      if (target.focus) target.focus({ preventScroll: true });
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-navigate', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), from: publicDetail(element), to: publicDetail(target) } }));
      return true;
    };
    const applyRelatedClasses = (detail, classPrefix, selfClass) => {
      const relatedEdges = new Set(detail.related.edgeIds || []);
      const relatedNodes = new Set(detail.related.nodeIds || []);
      const relatedGroups = new Set(detail.related.groupIds || []);
      const relatedClass = classPrefix + 'related';
      const mutedClass = classPrefix + 'muted';
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
        const isSelf = detail.kind === 'edge' && attr(edge, 'data-edge-id') === detail.id;
        const isRelated = isSelf || relatedEdges.has(attr(edge, 'data-edge-id'));
        edge.classList.toggle(relatedClass, isRelated);
        edge.classList.toggle(mutedClass, !isRelated);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const isSelf = detail.kind === 'edge' && attr(label, 'data-edge-id') === detail.id;
        const isRelated = isSelf || relatedEdges.has(attr(label, 'data-edge-id'));
        label.classList.toggle(relatedClass, isRelated);
        label.classList.toggle(mutedClass, !isRelated);
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const isSelf = detail.kind === 'node' && attr(node, 'data-node-id') === detail.id;
        const isRelated = isSelf || relatedNodes.has(attr(node, 'data-node-id'));
        node.classList.toggle(relatedClass, isRelated);
        node.classList.toggle(mutedClass, !isRelated && detail.kind !== 'group');
      });
      wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
        const isSelf = detail.kind === 'group' && attr(group, 'data-group-id') === detail.id;
        const isRelated = isSelf || relatedGroups.has(attr(group, 'data-group-id'));
        group.classList.toggle(relatedClass, isRelated);
      });
      if (detail.element && selfClass) detail.element.classList.add(selfClass);
    };
    const hover = element => {
      const detail = identity(element);
      detail.related = related(detail);
      clearHover();
      wrapper.setAttribute('data-cfx-hover-kind', detail.kind);
      wrapper.setAttribute('data-cfx-hover-id', detail.id);
      applyRelatedClasses(detail, 'cfx-topology-html-hover-', 'cfx-topology-html-hovered');
      applyForceFocusLabels(detail);
      delete detail.element;
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover', { bubbles: true, detail }));
    };
    const select = (element, emit = true, sync = true) => {
      const detail = identity(element);
      detail.related = related(detail);
      clear();
      wrapper.setAttribute('data-cfx-selection-kind', detail.kind);
      wrapper.setAttribute('data-cfx-selection-id', detail.id);
      wrapper.setAttribute('data-cfx-selection-status', detail.status);
      element.classList.add('cfx-topology-html-selected');
      element.setAttribute('aria-selected', 'true');
      applyRelatedClasses({ ...detail, element }, 'cfx-topology-html-', '');
      applyForceFocusLabels(detail);
      renderSelectionPanel({ ...detail, element });
      delete detail.element;
      if (emit) wrapper.dispatchEvent(new CustomEvent('cfx-topology-select', { bubbles: true, detail }));
      if (sync) emitSync('selection', { selection: { kind: detail.kind, id: detail.id, status: detail.status } });
    };
    const hydrateSelected = element => {
      const detail = identity(element);
      detail.related = related(detail);
      wrapper.setAttribute('data-cfx-selection-kind', detail.kind);
      wrapper.setAttribute('data-cfx-selection-id', detail.id);
      wrapper.setAttribute('data-cfx-selection-status', detail.status);
      element.setAttribute('aria-selected', 'true');
      renderSelectionPanel({ ...detail, element });
      delete detail.element;
    };
    const topologyFilterSearchText = element => [
      attr(element, 'data-node-id'), attr(element, 'data-node-label'), attr(element, 'data-node-kind'), attr(element, 'data-group-id'),
      attr(element, 'data-edge-id'), attr(element, 'data-edge-label'), attr(element, 'data-edge-secondary-label'), attr(element, 'data-edge-tertiary-label'), attr(element, 'data-edge-kind'), attr(element, 'data-source-node-id'), attr(element, 'data-target-node-id'),
      attr(element, 'data-group-label'), attr(element, 'data-group-symbol'), attr(element, 'data-cfx-status'), element.textContent || ''
    ].join(' ').toLowerCase();
    const topologyFilterEscape = value => window.CSS && CSS.escape ? CSS.escape(value) : String(value).replace(/["\\]/g, '\\$&');
    let topologyFilterState = { query: '', status: '', group: '', kind: '', edges: true, labels: true, groups: true };
    const setTopologyFilterHidden = (selector, predicate) => {
      wrapper.querySelectorAll(selector).forEach(element => element.classList.toggle('cfx-topology-html-filter-hidden', predicate(element)));
    };
    const setTopologyFilterAttributes = detail => {
      wrapper.setAttribute('data-cfx-filter-query', detail.query || '');
      wrapper.setAttribute('data-cfx-filter-status', detail.status || '');
      wrapper.setAttribute('data-cfx-filter-group', detail.group || '');
      wrapper.setAttribute('data-cfx-filter-kind', detail.kind || '');
      wrapper.setAttribute('data-cfx-filter-active', detail.active ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-visible-nodes', String(detail.nodes));
      wrapper.setAttribute('data-cfx-visible-edges', String(detail.edges));
    };
    const applyTopologyFilter = state => {
      topologyFilterState = Object.assign({}, topologyFilterState, state || {});
      const query = String(topologyFilterState.query || '').trim().toLowerCase();
      const status = String(topologyFilterState.status || '').trim();
      const group = String(topologyFilterState.group || '').trim();
      const kind = String(topologyFilterState.kind || '').trim();
      const showEdges = topologyFilterState.edges !== false;
      const showLabels = topologyFilterState.labels !== false;
      const showGroups = topologyFilterState.groups !== false;
      const active = !!(query || status || group || kind || !showEdges || !showLabels || !showGroups);
      const visibleNodes = new Set();
      const queryNodes = new Set();
      const statusNodes = new Set();
      if (query) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (!topologyFilterSearchText(edge).includes(query)) return;
          queryNodes.add(attr(edge, 'data-source-node-id'));
          queryNodes.add(attr(edge, 'data-target-node-id'));
        });
        wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(groupElement => {
          if (!topologyFilterSearchText(groupElement).includes(query)) return;
          const groupId = attr(groupElement, 'data-group-id');
          wrapper.querySelectorAll('[data-cfx-role="topology-node"][data-group-id="' + topologyFilterEscape(groupId) + '"]').forEach(node => queryNodes.add(attr(node, 'data-node-id')));
        });
      }
      if (status) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (attr(edge, 'data-cfx-status') !== status) return;
          statusNodes.add(attr(edge, 'data-source-node-id'));
          statusNodes.add(attr(edge, 'data-target-node-id'));
        });
      }
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const nodeId = attr(node, 'data-node-id');
        const groupOk = !group || attr(node, 'data-group-id') === group;
        const statusOk = !status || attr(node, 'data-cfx-status') === status || statusNodes.has(nodeId);
        const kindOk = !kind || attr(node, 'data-node-kind') === kind;
        const queryOk = !query || topologyFilterSearchText(node).includes(query) || queryNodes.has(nodeId);
        const visible = groupOk && statusOk && kindOk && queryOk;
        node.classList.toggle('cfx-topology-html-filter-hidden', !visible);
        wrapper.querySelectorAll('[data-cfx-role="topology-node-status"][data-node-id="' + topologyFilterEscape(nodeId) + '"]').forEach(statusBadge => statusBadge.classList.toggle('cfx-topology-html-filter-hidden', !visible));
        if (visible) visibleNodes.add(nodeId);
      });
      const nodeById = id => wrapper.querySelector('[data-cfx-role="topology-node"][data-node-id="' + topologyFilterEscape(id) + '"]');
      setTopologyFilterHidden('[data-cfx-role="topology-edge"]', edge => {
        const sourceId = attr(edge, 'data-source-node-id');
        const targetId = attr(edge, 'data-target-node-id');
        const source = nodeById(sourceId);
        const target = nodeById(targetId);
        const endpointsVisible = visibleNodes.has(sourceId) && visibleNodes.has(targetId);
        const edgeQueryOk = !query || topologyFilterSearchText(edge).includes(query) || (source && topologyFilterSearchText(source).includes(query)) || (target && topologyFilterSearchText(target).includes(query));
        const edgeStatusOk = !status || attr(edge, 'data-cfx-status') === status || (source && attr(source, 'data-cfx-status') === status) || (target && attr(target, 'data-cfx-status') === status);
        return !showEdges || !endpointsVisible || !edgeQueryOk || !edgeStatusOk;
      });
      setTopologyFilterHidden('[data-cfx-role="topology-edge-label"]', label => !showLabels || !showEdges || !wrapper.querySelector('[data-cfx-role="topology-edge"][data-edge-id="' + topologyFilterEscape(attr(label, 'data-edge-id')) + '"]:not(.cfx-topology-html-filter-hidden):not(.cfx-topology-html-force-hidden)'));
      setTopologyFilterHidden('[data-cfx-role="topology-group"]', groupElement => {
        const groupId = attr(groupElement, 'data-group-id');
        const hasVisibleNodes = !!wrapper.querySelector('[data-cfx-role="topology-node"][data-group-id="' + topologyFilterEscape(groupId) + '"]:not(.cfx-topology-html-filter-hidden):not(.cfx-topology-html-force-hidden)');
        return !showGroups || !hasVisibleNodes || (group && groupId !== group);
      });
      const detail = {
        chartId: attr(wrapper, 'data-chart-id'),
        nodes: visibleNodes.size,
        edges: wrapper.querySelectorAll('[data-cfx-role="topology-edge"]:not(.cfx-topology-html-filter-hidden):not(.cfx-topology-html-force-hidden)').length,
        query: topologyFilterState.query || '',
        status,
        group,
        kind,
        active
      };
      setTopologyFilterAttributes(detail);
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-filter', { bubbles: true, detail }));
      if (active && visibleNodes.size) fitVisibleTopology(false);
      restoreForceFocusLabels();
    };
    const clearTopologyFilter = () => applyTopologyFilter({ query: '', status: '', group: '', kind: '', edges: true, labels: true, groups: true });
    const forceSearchText = element => [
      attr(element, 'data-node-id'), attr(element, 'data-node-label'), attr(element, 'data-node-kind'), attr(element, 'data-group-id'),
      attr(element, 'data-edge-id'), attr(element, 'data-edge-label'), attr(element, 'data-edge-secondary-label'), attr(element, 'data-edge-tertiary-label'), attr(element, 'data-edge-kind'), attr(element, 'data-source-node-id'), attr(element, 'data-target-node-id'),
      attr(element, 'data-group-label'), attr(element, 'data-group-symbol'), element.textContent || ''
    ].join(' ').toLowerCase();
    const cssEscape = value => window.CSS && CSS.escape ? CSS.escape(value) : String(value).replace(/["\\]/g, '\\$&');
    const forceGraphState = () => ({
      query: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-search]')) || {}).value || '',
      status: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-status]')) || {}).value || '',
      group: ((forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-group]')) || {}).value || '',
      edges: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="edges"]:checked')),
      focus: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="focus"]:checked')),
      labels: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="edge-labels"]:checked')),
      groups: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="groups"]:checked')),
      hideMovingEdges: !!(forceGraphPanel && forceGraphPanel.querySelector('[data-cfx-force-toggle="hide-moving-edges"]:checked'))
    });
    const setForceHidden = (selector, predicate) => {
      wrapper.querySelectorAll(selector).forEach(element => element.classList.toggle('cfx-topology-html-force-hidden', predicate(element)));
    };
    const applyForceGraphFilters = () => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const state = forceGraphState();
      const query = state.query.trim().toLowerCase();
      const visibleNodes = new Set();
      const queryNodes = new Set();
      const statusNodes = new Set();
      if (query) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (!forceSearchText(edge).includes(query)) return;
          queryNodes.add(attr(edge, 'data-source-node-id'));
          queryNodes.add(attr(edge, 'data-target-node-id'));
        });
        wrapper.querySelectorAll('[data-cfx-role="topology-group"]').forEach(group => {
          if (!forceSearchText(group).includes(query)) return;
          const groupId = attr(group, 'data-group-id');
          wrapper.querySelectorAll('[data-cfx-role="topology-node"][data-group-id="' + cssEscape(groupId) + '"]').forEach(node => queryNodes.add(attr(node, 'data-node-id')));
        });
      }
      if (state.status) {
        wrapper.querySelectorAll('[data-cfx-role="topology-edge"]').forEach(edge => {
          if (attr(edge, 'data-cfx-status') !== state.status) return;
          statusNodes.add(attr(edge, 'data-source-node-id'));
          statusNodes.add(attr(edge, 'data-target-node-id'));
        });
      }
      wrapper.setAttribute('data-cfx-force-edges-visible', state.edges ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-focus', state.focus ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-edge-labels-visible', state.labels ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-groups-visible', state.groups ? 'true' : 'false');
      wrapper.setAttribute('data-cfx-force-hide-moving-edges', state.hideMovingEdges ? 'true' : 'false');
      wrapper.querySelectorAll('[data-cfx-role="topology-node"]').forEach(node => {
        const groupOk = !state.group || attr(node, 'data-group-id') === state.group;
        const statusOk = !state.status || attr(node, 'data-cfx-status') === state.status || statusNodes.has(attr(node, 'data-node-id'));
        const queryOk = !query || forceSearchText(node).includes(query) || queryNodes.has(attr(node, 'data-node-id'));
        const visible = groupOk && statusOk && queryOk;
        node.classList.toggle('cfx-topology-html-force-hidden', !visible);
        wrapper.querySelectorAll('[data-cfx-role="topology-node-status"][data-node-id="' + cssEscape(attr(node, 'data-node-id')) + '"]').forEach(status => status.classList.toggle('cfx-topology-html-force-hidden', !visible));
        if (visible) visibleNodes.add(attr(node, 'data-node-id'));
      });
      const nodeById = id => wrapper.querySelector('[data-cfx-role="topology-node"][data-node-id="' + cssEscape(id) + '"]');
      setForceHidden('[data-cfx-role="topology-edge"]', edge => {
        const sourceId = attr(edge, 'data-source-node-id');
        const targetId = attr(edge, 'data-target-node-id');
        const source = nodeById(sourceId);
        const target = nodeById(targetId);
        const endpointsVisible = visibleNodes.has(sourceId) && visibleNodes.has(targetId);
        const edgeQueryOk = !query || forceSearchText(edge).includes(query) || (source && forceSearchText(source).includes(query)) || (target && forceSearchText(target).includes(query));
        const edgeStatusOk = !state.status || attr(edge, 'data-cfx-status') === state.status || (source && attr(source, 'data-cfx-status') === state.status) || (target && attr(target, 'data-cfx-status') === state.status);
        return !state.edges || !endpointsVisible || !edgeQueryOk || !edgeStatusOk;
      });
      setForceHidden('[data-cfx-role="topology-edge-label"]', label => !state.labels || !state.edges || !wrapper.querySelector('[data-cfx-role="topology-edge"][data-edge-id="' + cssEscape(attr(label, 'data-edge-id')) + '"]:not(.cfx-topology-html-force-hidden)'));
      setForceHidden('[data-cfx-role="topology-group"]', group => {
        const groupId = attr(group, 'data-group-id');
        const hasVisibleNodes = !!wrapper.querySelector('[data-cfx-role="topology-node"][data-group-id="' + cssEscape(groupId) + '"]:not(.cfx-topology-html-force-hidden)');
        return !state.groups || !hasVisibleNodes || (state.group && groupId !== state.group);
      });
      const nodeCount = visibleNodes.size;
      const edgeCount = wrapper.querySelectorAll('[data-cfx-role="topology-edge"]:not(.cfx-topology-html-force-hidden)').length;
      const summary = forceGraphPanel.querySelector('[data-cfx-force-summary]');
      if (summary) summary.textContent = nodeCount + ' nodes / ' + edgeCount + ' edges visible';
      const detail = { chartId: attr(wrapper, 'data-chart-id'), nodes: nodeCount, edges: edgeCount, query: state.query, status: state.status, group: state.group, kind: '', active: !!(state.query || state.status || state.group || !state.edges || !state.labels || !state.groups) };
      setTopologyFilterAttributes(detail);
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-filter', { bubbles: true, detail }));
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-force-filter', { bubbles: true, detail }));
      restoreForceFocusLabels();
    };
    const clearForceFocusLabels = () => {
      if (!forceGraphControls) return;
      const state = forceGraphPanel ? forceGraphState() : { labels: false };
      wrapper.querySelectorAll('.cfx-topology-html-force-focus-label').forEach(label => {
        label.classList.remove('cfx-topology-html-force-focus-label');
        if (!state.labels) label.classList.add('cfx-topology-html-force-hidden');
      });
    };
    const selectedFocusElement = () => {
      const kind = wrapper.getAttribute('data-cfx-selection-kind') || '';
      const id = wrapper.getAttribute('data-cfx-selection-id') || '';
      return kind && id ? findSelection({ kind, id }) : null;
    };
    const restoreForceFocusLabels = () => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const selected = selectedFocusElement();
      if (selected) applyForceFocusLabels(identity(selected));
      else clearForceFocusLabels();
    };
    const applyForceFocusLabels = detail => {
      if (!forceGraphControls || !forceGraphPanel) return;
      const state = forceGraphState();
      if (!state.focus || !state.edges) {
        clearForceFocusLabels();
        return;
      }
      detail.related = detail.related || related(detail);
      const edgeIds = new Set(detail.related.edgeIds || []);
      if (detail.kind === 'edge') edgeIds.add(detail.id);
      wrapper.querySelectorAll('[data-cfx-role="topology-edge-label"]').forEach(label => {
        const edgeId = attr(label, 'data-edge-id');
        const edge = wrapper.querySelector('[data-cfx-role="topology-edge"][data-edge-id="' + cssEscape(edgeId) + '"]:not(.cfx-topology-html-force-hidden)');
        const active = edgeIds.has(edgeId) && !!edge;
        label.classList.toggle('cfx-topology-html-force-focus-label', active);
        if (active) label.classList.remove('cfx-topology-html-force-hidden');
        else if (!state.labels) label.classList.add('cfx-topology-html-force-hidden');
      });
      const summary = forceGraphPanel.querySelector('[data-cfx-force-summary]');
      if (summary && detail.kind === 'node') summary.textContent = detail.id + ': ' + (detail.related.nodeIds || []).length + ' neighbors / ' + edgeIds.size + ' edges';
    };
    wrapper.querySelectorAll(selectables).forEach(element => {
      if (!element.hasAttribute('tabindex')) element.setAttribute('tabindex', '0');
      if (!element.hasAttribute('role')) element.setAttribute('role', 'button');
      element.addEventListener('pointerenter', () => hover(element));
      element.addEventListener('pointerleave', () => {
        clearHover();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover-clear', { bubbles: true }));
      });
      element.addEventListener('focus', () => hover(element));
      element.addEventListener('blur', () => {
        clearHover();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-hover-clear', { bubbles: true }));
      });
    });
    const initiallySelected = Array.from(wrapper.querySelectorAll(selectables)).find(element => attr(element, 'data-cfx-selected') === 'true');
    if (initiallySelected) hydrateSelected(initiallySelected);
    else renderSelectionPanel(null);
    if (viewportControls) {
      applyViewport(viewportState());
      wrapper.querySelectorAll('[data-cfx-topology-zoom]').forEach(button => {
        button.addEventListener('click', () => zoomBy(attr(button, 'data-cfx-topology-zoom') === 'in' ? 1.2 : 0.8333333333));
      });
      wrapper.querySelectorAll('[data-cfx-topology-mode]').forEach(button => {
        button.addEventListener('click', () => setViewportMode(attr(button, 'data-cfx-topology-mode')));
      });
      wrapper.querySelectorAll('[data-cfx-topology-fit]').forEach(button => {
        button.addEventListener('click', () => fitViewport());
      });
      wrapper.querySelectorAll('[data-cfx-topology-reset]').forEach(button => {
        button.addEventListener('click', () => resetViewport());
      });
      let drag = null;
      let suppressClick = false;
      const isViewportChrome = target => target instanceof Element && target.closest('.cfx-topology-controls,.cfx-topology-scenarios,.cfx-topology-scenario-panel,.cfx-topology-selection-panel,.cfx-topology-force-controls');
      viewport.addEventListener('wheel', event => {
        if (isViewportChrome(event.target)) return;
        event.preventDefault();
        zoomBy(event.deltaY < 0 ? 1.08 : 0.92, event);
      }, { passive: false });
      viewport.addEventListener('pointerdown', event => {
        if (event.button !== 0) return;
        if (isViewportChrome(event.target)) return;
        const state = viewportState();
        drag = { id: event.pointerId, x: event.clientX, y: event.clientY, panX: state.panX, panY: state.panY, moved: false };
        wrapper.setAttribute('data-cfx-topology-dragging', 'true');
        try { if (viewport.setPointerCapture) viewport.setPointerCapture(event.pointerId); } catch { }
      });
      viewport.addEventListener('pointermove', event => {
        if (!drag || drag.id !== event.pointerId) return;
        const dx = event.clientX - drag.x;
        const dy = event.clientY - drag.y;
        if (Math.abs(dx) > 2 || Math.abs(dy) > 2) {
          drag.moved = true;
          suppressClick = true;
          markForceGraphMoving();
        }
        applyViewport({ zoom: viewportState().zoom, panX: drag.panX + dx, panY: drag.panY + dy });
      });
      viewport.addEventListener('pointerup', event => {
        if (!drag || drag.id !== event.pointerId) return;
        try { if (viewport.releasePointerCapture) viewport.releasePointerCapture(event.pointerId); } catch { }
        if (drag.moved) emitViewport();
        wrapper.removeAttribute('data-cfx-force-moving-edges');
        wrapper.removeAttribute('data-cfx-topology-dragging');
        wrapper.querySelectorAll('.cfx-topology-html-force-moving').forEach(edge => edge.classList.remove('cfx-topology-html-force-moving'));
        drag = null;
      });
      viewport.addEventListener('pointercancel', event => {
        if (drag && drag.id === event.pointerId) {
          wrapper.removeAttribute('data-cfx-topology-dragging');
          drag = null;
        }
      });
      wrapper.addEventListener('cfx-topology-set-viewport', event => {
        applyViewport(event.detail || {});
        emitViewport();
      });
      wrapper.addEventListener('cfx-topology-reset-viewport', () => resetViewport());
      wrapper.addEventListener('cfx-topology-fit-visible', event => {
        const detail = event.detail || {};
        if (detail.kind && detail.id) {
          const element = findSelection(detail);
          if (element) {
            fitViewportToElements([element]);
            return;
          }
        }

        fitVisibleTopology();
      });
      wrapper.addEventListener('click', event => {
        if (isViewportChrome(event.target)) return;
        if (!suppressClick) return;
        suppressClick = false;
        event.preventDefault();
        event.stopPropagation();
      }, true);
    }
    if (exportControls) {
      wrapper.querySelectorAll('[data-cfx-topology-export]').forEach(button => {
        button.addEventListener('click', () => {
          if (attr(button, 'data-cfx-topology-export') === 'png') exportPng();
          else exportSvg();
        });
      });
    }
    if (fullscreenControl) {
      wrapper.querySelectorAll('[data-cfx-topology-fullscreen]').forEach(button => {
        button.addEventListener('click', () => toggleFullscreen());
      });
      document.addEventListener('fullscreenchange', setFullscreenState);
      setFullscreenState();
    }
    if (forceGraphControls && forceGraphPanel) {
      forceGraphPanel.querySelectorAll('input,select').forEach(control => {
        control.addEventListener('input', applyForceGraphFilters);
        control.addEventListener('change', applyForceGraphFilters);
      });
      wrapper.addEventListener('cfx-topology-force-filter-set', event => {
        const detail = event.detail || {};
        if (forceGraphPanel.querySelector('[data-cfx-force-search]') && detail.query !== undefined) forceGraphPanel.querySelector('[data-cfx-force-search]').value = detail.query || '';
        if (forceGraphPanel.querySelector('[data-cfx-force-status]') && detail.status !== undefined) forceGraphPanel.querySelector('[data-cfx-force-status]').value = detail.status || '';
        if (forceGraphPanel.querySelector('[data-cfx-force-group]') && detail.group !== undefined) forceGraphPanel.querySelector('[data-cfx-force-group]').value = detail.group || '';
        applyForceGraphFilters();
      });
      applyForceGraphFilters();
    }
    wrapper.addEventListener('cfx-topology-set-filter', event => applyTopologyFilter(event.detail || {}));
    wrapper.addEventListener('cfx-topology-clear-filter', () => clearTopologyFilter());
    if (scenarioControls) {
      if (scenarioControlMode === 'checkboxes') {
        wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]').forEach(input => {
          const scenarioId = attr(input, 'data-cfx-topology-scenario-toggle');
          input.addEventListener('change', () => setScenarioFilters(Array.from(wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]:checked')).map(item => attr(item, 'data-cfx-topology-scenario-toggle'))));
          input.addEventListener('pointerenter', () => previewScenario(scenarioId));
          input.addEventListener('pointerleave', clearScenarioPreview);
          input.addEventListener('focus', () => previewScenario(scenarioId));
          input.addEventListener('blur', clearScenarioPreview);
        });
      } else {
        wrapper.querySelectorAll('[data-cfx-topology-scenario]').forEach(button => {
          const scenarioId = attr(button, 'data-cfx-topology-scenario');
          button.addEventListener('click', () => setScenario(scenarioId));
          button.addEventListener('pointerenter', () => previewScenario(scenarioId));
          button.addEventListener('pointerleave', clearScenarioPreview);
          button.addEventListener('focus', () => previewScenario(scenarioId));
          button.addEventListener('blur', clearScenarioPreview);
        });
      }
      wrapper.addEventListener('cfx-topology-set-scenario', event => setScenario((event.detail || {}).scenarioId || ''));
      wrapper.addEventListener('cfx-topology-set-scenario-filters', event => setScenarioFilters((event.detail || {}).scenarioIds || []));
      wrapper.addEventListener('cfx-topology-clear-scenario', () => setScenario(''));
      wrapper.addEventListener('cfx-topology-set-scenario-step', event => setScenarioStep((event.detail || {}).index, false));
      wrapper.querySelectorAll('[data-cfx-scenario-step-control]').forEach(button => {
        button.addEventListener('click', () => {
          const action = attr(button, 'data-cfx-scenario-step-control');
          if (action === 'previous') stepScenario(-1);
          else if (action === 'next') stepScenario(1);
          else if (action === 'play') playScenario();
          else if (action === 'reset') { stopScenarioPlayback(); clearScenarioStep(); }
          else if (action === 'link') copyScenarioLink();
        });
      });
      const stepsList = scenarioPanel ? scenarioPanel.querySelector('[data-cfx-scenario-panel-steps]') : null;
      if (stepsList) {
        const focusStep = target => { const item = target instanceof Element ? target.closest('[data-cfx-scenario-step-index]') : null; if (item && stepsList.contains(item)) setScenarioStep(Number(attr(item, 'data-cfx-scenario-step-index')) - 1, false); };
        stepsList.addEventListener('click', event => focusStep(event.target));
        stepsList.addEventListener('keydown', event => { if (event.key !== 'Enter' && event.key !== ' ') return; event.preventDefault(); focusStep(event.target); });
      }
      const initialScenario = scenarioUrlParam('scenario') || attr(wrapper, 'data-cfx-active-scenario');
      const initialScenarioStep = scenarioUrlParam('scenarioStep');
      if (scenarioControlMode === 'checkboxes') setScenarioFilters(initialScenario ? scenarioIdTokens(initialScenario) : Array.from(wrapper.querySelectorAll('[data-cfx-topology-scenario-toggle]:checked')).map(item => attr(item, 'data-cfx-topology-scenario-toggle')), false, false);
      else setScenario(initialScenario, false, false);
      if (initialScenarioStep && (scenarioControlMode !== 'checkboxes' || attr(wrapper, 'data-cfx-active-scenario'))) setScenarioStep(initialScenarioStep, false, false, false);
    }
    wrapper.addEventListener('click', event => {
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    wrapper.addEventListener('keydown', event => {
      if (event.key === 'Escape') {
        clear();
        wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
        emitSync('clear-selection', {});
        return;
      }
      if (event.key === 'ArrowRight' || event.key === 'ArrowDown' || event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
        const element = event.target instanceof Element ? event.target.closest(selectables) : null;
        if (!element || !wrapper.contains(element)) return;
        if (focusRelated(element, event.key === 'ArrowRight' || event.key === 'ArrowDown' ? 1 : -1)) event.preventDefault();
        return;
      }
      if (event.key !== 'Enter' && event.key !== ' ') return;
      const element = event.target instanceof Element ? event.target.closest(selectables) : null;
      if (!element || !wrapper.contains(element)) return;
      event.preventDefault();
      select(element);
    });
    wrapper.addEventListener('cfx-topology-set-selection', event => {
      const element = findSelection(event.detail);
      if (element) select(element);
    });
    wrapper.addEventListener('cfx-topology-clear-selection', () => {
      clear();
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
      emitSync('clear-selection', {});
    });
    wrapper.addEventListener('cfx-topology-apply-sync', event => {
      const detail = event.detail || {};
      if (!syncEnabled || !syncGroup || detail.group !== syncGroup) return;
      applyingSync = true;
      try {
        if (detail.action === 'selection' && detail.selection) {
          const element = findSelection(detail.selection);
          if (element) select(element);
        } else if (detail.action === 'clear-selection') {
          clear();
          wrapper.dispatchEvent(new CustomEvent('cfx-topology-clear', { bubbles: true }));
        } else if (detail.action === 'viewport' && detail.state) {
          applyViewport(detail.state);
          wrapper.dispatchEvent(new CustomEvent('cfx-topology-viewport', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState(), sourceChartId: detail.chartId } }));
        } else if (detail.action === 'scenario') {
          setScenario(detail.scenarioId || '');
        } else if (detail.action === 'scenario-filters') {
          setScenarioFilters(detail.scenarioIds || []);
        } else if (detail.action === 'scenario-step') {
          if (detail.scenarioId && attr(wrapper, 'data-cfx-active-scenario') !== detail.scenarioId) setScenario(detail.scenarioId);
          setScenarioStep(detail.index, false);
        }
      } finally {
        applyingSync = false;
      }
    });
  }
})();
