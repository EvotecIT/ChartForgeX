  const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
  const getState = (root) => ({
    zoom: Number(root.dataset.cfxZoom || '1'),
    panX: Number(root.dataset.cfxPanX || '0'),
    panY: Number(root.dataset.cfxPanY || '0')
  });
  const applyViewport = (root, state) => {
    state.zoom = clamp(state.zoom, 0.5, 6);
    state.panX = clamp(state.panX, -4000, 4000);
    state.panY = clamp(state.panY, -4000, 4000);
    root.dataset.cfxZoom = state.zoom.toFixed(3);
    root.dataset.cfxPanX = state.panX.toFixed(1);
    root.dataset.cfxPanY = state.panY.toFixed(1);
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    stage.style.setProperty('--cfx-zoom', state.zoom);
    stage.style.setProperty('--cfx-pan-x', state.panX + 'px');
    stage.style.setProperty('--cfx-pan-y', state.panY + 'px');
  };
  const sameGroup = (root, peer) => root !== peer && root.dataset.cfxInteractionGroup && root.dataset.cfxInteractionGroup === peer.dataset.cfxInteractionGroup;
  const emitHostEvent = (root, name, detail) => {
    root.dispatchEvent(new CustomEvent(name, { detail: Object.assign({ chartId: root.dataset.cfxChartId || '' }, detail || {}) }));
  };
  const emitSync = (root, payload) => {
    if (!hasFeature(root, 'SynchronizedCharts') || !root.dataset.cfxInteractionGroup) return;
    const detail = Object.assign({ chartId: root.dataset.cfxChartId || '', group: root.dataset.cfxInteractionGroup }, payload);
    root.dispatchEvent(new CustomEvent('cfxsync', { detail }));
    document.querySelectorAll('.cfx-interactive-chart').forEach((peer) => {
      if (sameGroup(root, peer) && hasFeature(peer, 'SynchronizedCharts')) applySync(peer, detail);
    });
  };
  const applyMode = (root, mode) => {
    root.dataset.cfxMode = mode || '';
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      button.setAttribute('aria-pressed', button.dataset.cfxModeButton === root.dataset.cfxMode ? 'true' : 'false');
    });
  };
  const setMode = (root, mode) => applyMode(root, root.dataset.cfxMode === mode ? '' : mode);
  const resetViewport = (root) => {
    applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
    root.dataset.cfxBrush = '';
    root.dataset.cfxMode = '';
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => button.setAttribute('aria-pressed', 'false'));
  };
  const storeInteractionState = (root, snapshot) => {
    try {
      root.dataset.cfxInteractionSnapshot = JSON.stringify(snapshot);
    } catch {
      root.removeAttribute('data-cfx-interaction-snapshot');
    }
  };
  const captureInteractionState = (root, source) => {
    const snapshot = {
      version: 1,
      chartId: root.dataset.cfxChartId || '',
      source: source || '',
      viewport: getState(root),
      mode: root.dataset.cfxMode || '',
      brush: root.dataset.cfxBrush || '',
      selectedTargets: selectedTargets(root),
      compareCount: Number(root.dataset.cfxCompareCount || '0'),
      scenario: {
        id: root.dataset.cfxActiveScenario || '',
        stepIndex: root.dataset.cfxActiveScenarioStep || '',
        progress: root.dataset.cfxScenarioProgress || '',
        playback: root.dataset.cfxScenarioPlayback || ''
      }
    };
    storeInteractionState(root, snapshot);
    return snapshot;
  };
  const publishInteractionState = (root, source, sync) => {
    if (!hasFeature(root, 'StateBookmarks')) return null;
    const snapshot = captureInteractionState(root, source || 'host');
    emitHostEvent(root, 'cfxstate', { source: source || 'host', snapshot });
    if (sync !== false) emitSync(root, { action: 'state', state: snapshot });
    return snapshot;
  };
  const applyInteractionState = (root, snapshot, emit, sync) => {
    if (!snapshot || !hasFeature(root, 'StateBookmarks')) return;
    if (snapshot.viewport) applyViewport(root, Object.assign({}, snapshot.viewport));
    if (snapshot.mode !== undefined) applyMode(root, snapshot.mode || '');
    if (snapshot.brush !== undefined) root.dataset.cfxBrush = snapshot.brush || '';
    const scenario = snapshot.scenario || {};
    if (scenario.id !== undefined) {
      setScenario(root, scenario.id || '', false, false);
      if (scenario.id && scenario.stepIndex !== undefined && scenario.stepIndex !== '') setScenarioStep(root, scenario.stepIndex, false, false);
      const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
      if (scenario.playback === 'playing' && route && route.steps.length) {
        const current = Number(root.dataset.cfxActiveScenarioStep || '-1');
        const next = Number.isFinite(current) ? current + 1 : 0;
        startScenarioPlayback(root, route, next, false, false);
      } else if (scenario.playback) {
        setScenarioPlaybackState(root, scenario.playback, route, false);
      }
    }
    applySelectionSetByTargets(root, snapshot.selectedTargets || [], true);
    renderCompare(root);
    storeInteractionState(root, snapshot);
    if (emit !== false) emitHostEvent(root, 'cfxstateapplied', { snapshot });
    if (sync !== false) emitSync(root, { action: 'state', state: snapshot });
  };
  const zoomBy = (root, factor) => {
    if (!hasFeature(root, 'Zoom')) return;
    const state = getState(root);
    state.zoom *= factor;
    applyViewport(root, state);
    emitHostEvent(root, 'cfxviewport', { state: getState(root) });
    emitSync(root, { action: 'viewport', state: getState(root) });
  };
  const serializeSvg = (root) => {
    const svg = root.querySelector('.cfx-stage svg');
    return svg ? { svg, data: new XMLSerializer().serializeToString(svg) } : null;
  };
  const downloadBlob = (root, blob, extension, format) => {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = (root.dataset.cfxChartId || 'chart') + '.' + extension;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    emitHostEvent(root, 'cfxexport', { format });
  };
  const svgSize = (svg) => {
    const viewBox = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
    const rect = svg.getBoundingClientRect ? svg.getBoundingClientRect() : { width: 0, height: 0 };
    const width = Math.max(1, Math.round((viewBox && viewBox.width) || (svg.width && svg.width.baseVal ? svg.width.baseVal.value : 0) || rect.width || 800));
    const height = Math.max(1, Math.round((viewBox && viewBox.height) || (svg.height && svg.height.baseVal ? svg.height.baseVal.value : 0) || rect.height || 450));
    return { width, height };
  };
  const exportSvg = (root) => {
    if (!hasFeature(root, 'Export')) return;
    const serialized = serializeSvg(root);
    if (!serialized) return;
    downloadBlob(root, new Blob([serialized.data], { type: 'image/svg+xml;charset=utf-8' }), 'svg', 'svg');
  };
  const exportPng = (root) => {
    if (!hasFeature(root, 'Export')) return;
    const serialized = serializeSvg(root);
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
      canvas.toBlob((blob) => {
        URL.revokeObjectURL(sourceUrl);
        if (blob) downloadBlob(root, blob, 'png', 'png');
      }, 'image/png');
    };
    image.onerror = () => URL.revokeObjectURL(sourceUrl);
    image.src = sourceUrl;
  };
