namespace ChartForgeX.Topology;

public sealed partial class TopologyHtmlRenderer
{
    private static string InteractionScriptPart1() => """
<script>
(() => {
  for (const wrapper of document.querySelectorAll('.cfx-topology-wrapper[data-cfx-interactive="true"]')) {
    const selectables = '[data-cfx-role="topology-node"],[data-cfx-role="topology-edge"],[data-cfx-role="topology-group"]';
    const viewportControls = wrapper.getAttribute('data-cfx-viewport-controls') === 'true';
    const exportControls = wrapper.getAttribute('data-cfx-export-controls') === 'true';
    const scenarioControls = wrapper.getAttribute('data-cfx-scenario-controls') === 'true';
    const scenarioUrlState = wrapper.getAttribute('data-cfx-scenario-url-state') === 'true';
    const scenarioPanel = wrapper.querySelector('[data-cfx-topology-scenario-panel]');
    const syncEnabled = wrapper.getAttribute('data-cfx-sync-enabled') === 'true';
    const syncGroup = wrapper.getAttribute('data-cfx-sync-group') || '';
    const viewport = wrapper.querySelector('.cfx-topology-viewport') || wrapper;
    const topologyRoot = wrapper.querySelector('[data-cfx-role="topology"]');
    let applyingSync = false;
    let scenarioPlayback = null;
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
    const scenarioIds = element => (attr(element, 'data-scenario-ids') || '').split(/\s+/).filter(value => value);
    const scenarioStepIndices = (element, scenarioId) => (attr(element, 'data-scenario-step-indices') || '').split(/\s+/).filter(value => value.startsWith(scenarioId + ':')).flatMap(value => value.slice(scenarioId.length + 1).split(',').filter(index => index));
    const findScenarioButton = scenarioId => Array.from(wrapper.querySelectorAll('[data-cfx-topology-scenario]')).find(button => attr(button, 'data-cfx-topology-scenario') === scenarioId) || null;
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
    const copyScenarioLink = () => { syncScenarioUrl(attr(wrapper, 'data-cfx-active-scenario'), wrapper.getAttribute('data-cfx-active-scenario-step') || ''); const url = window.location.href; if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(url).catch(() => {}); wrapper.dispatchEvent(new CustomEvent('cfx-topology-scenario-link', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), url } })); };
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
    const emitViewport = () => {
      wrapper.dispatchEvent(new CustomEvent('cfx-topology-viewport', { bubbles: true, detail: { chartId: attr(wrapper, 'data-chart-id'), state: viewportState() } }));
      emitSync('viewport', { state: viewportState() });
    };
    const zoomBy = factor => {
      const state = viewportState();
      applyViewport({ zoom: state.zoom * factor, panX: state.panX, panY: state.panY });
      emitViewport();
    };
    const resetViewport = () => {
      applyViewport({ zoom: 1, panX: 0, panY: 0 });
      wrapper.removeAttribute('data-cfx-topology-mode');
      wrapper.querySelectorAll('[data-cfx-topology-mode]').forEach(button => button.setAttribute('aria-pressed', 'false'));
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
      wrapper.style.removeProperty('--cfx-topology-active-scenario-color');
      wrapper.querySelectorAll('.cfx-topology-html-scenario-active,.cfx-topology-html-scenario-muted').forEach(item => {
        item.classList.remove('cfx-topology-html-scenario-active', 'cfx-topology-html-scenario-muted');
        item.removeAttribute('data-cfx-active-scenario-step-indices');
      });
      wrapper.querySelectorAll('[data-cfx-topology-scenario]').forEach(button => button.setAttribute('aria-pressed', attr(button, 'data-cfx-topology-scenario') ? 'false' : 'true'));
      updateScenarioStepControls(false);
      renderScenarioPanel(null);
      syncScenarioUrl('', '');
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
    const svgElement = () => wrapper.querySelector('.cfx-topology-viewport svg') || wrapper.querySelector('svg');
    const exportName = () => (attr(wrapper, 'data-chart-id') || 'topology').replace(/[^a-z0-9_.-]+/gi, '-').replace(/^-+|-+$/g, '') || 'topology';
    const serializeSvg = () => {
      const svg = svgElement();
      return svg ? { svg, data: new XMLSerializer().serializeToString(svg) } : null;
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
""";
}
