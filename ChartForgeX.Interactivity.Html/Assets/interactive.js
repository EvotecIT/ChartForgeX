(() => {
  const hasFeature = (root, name) => (root.dataset.cfxInteractionFeatures || '').toLowerCase().includes(name.toLowerCase());
  const text = (node) => {
    const data = node.dataset || {};
    return node.getAttribute('aria-label') || data.cfxLabel || data.cfxValue || data.cfxRole || '';
  };
  const showTip = (root, tip, node, event) => {
    if (!hasFeature(root, 'Tooltips')) return;
    const value = text(node);
    if (!value) return;
    tip.textContent = value;
    tip.hidden = false;
    moveTip(tip, event);
  };
  const moveTip = (tip, event) => {
    if (!event || tip.hidden) return;
    const x = Math.min(window.innerWidth - 24, event.clientX + 14);
    const y = Math.min(window.innerHeight - 24, event.clientY + 14);
    tip.style.left = x + 'px';
    tip.style.top = y + 'px';
  };
  const hideTip = (tip) => { tip.hidden = true; };
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
  const setMode = (root, mode) => {
    root.dataset.cfxMode = root.dataset.cfxMode === mode ? '' : mode;
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      button.setAttribute('aria-pressed', button.dataset.cfxModeButton === root.dataset.cfxMode ? 'true' : 'false');
    });
  };
  const resetViewport = (root) => {
    applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
    root.dataset.cfxBrush = '';
    root.dataset.cfxMode = '';
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => button.setAttribute('aria-pressed', 'false'));
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
  const setSeriesMuted = (root, series, muted) => {
    root.querySelectorAll('[data-cfx-series]').forEach((node) => {
      if ((node.dataset ? node.dataset.cfxSeries : undefined) !== series) return;
      const role = node.dataset ? node.dataset.cfxRole || '' : '';
      if (role.indexOf('legend') === 0) {
        node.dataset.cfxMuted = muted ? 'true' : 'false';
        return;
      }
      node.classList.toggle('cfx-series-muted', muted);
    });
  };
  const toggleSeries = (root, item) => {
    if (!hasFeature(root, 'LegendToggles')) return;
    const series = item.dataset.cfxSeries;
    if (series === undefined) return;
    const muted = item.dataset.cfxMuted !== 'true';
    setSeriesMuted(root, series, muted);
    emitHostEvent(root, 'cfxseries', { series, muted });
    emitSync(root, { action: 'series', series, muted });
  };
  const toggleSelection = (root, node) => {
    if (!hasFeature(root, 'Selection')) return;
    const selected = !node.classList.contains('cfx-selected');
    setNodeSelected(node, selected);
    emitHostEvent(root, 'cfxselect', { label: text(node), selected });
    emitSync(root, { action: 'selection', label: text(node), selected });
  };
  const setNodeSelected = (node, selected) => {
    node.classList.toggle('cfx-selected', selected);
    node.setAttribute('aria-selected', selected ? 'true' : 'false');
  };
  const applySelectionByLabel = (root, label, selected) => {
    if (!label) return;
    root.querySelectorAll('.cfx-interactive-region,[data-cfx-label]').forEach((node) => {
      if (text(node) === label) setNodeSelected(node, selected);
    });
  };
  const applySync = (root, detail) => {
    if (!detail || detail.chartId === root.dataset.cfxChartId) return;
    if (detail.action === 'viewport' && detail.state) applyViewport(root, detail.state);
    else if (detail.action === 'brush') root.dataset.cfxBrush = detail.bounds || '';
    else if (detail.action === 'selection') applySelectionByLabel(root, detail.label || '', detail.selected === true);
    else if (detail.action === 'series' && detail.series !== undefined) setSeriesMuted(root, detail.series, detail.muted === true);
    else if (detail.action === 'scenario') setScenario(root, detail.scenarioId || '', false, false);
    else if (detail.action === 'scenario-step') {
      if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, false, false);
      setScenarioStep(root, detail.index, false, false);
    }
  };
  const readJson = (value, fallback) => {
    try {
      const parsed = JSON.parse(value || '');
      return parsed === null || parsed === undefined ? fallback : parsed;
    } catch {
      return fallback;
    }
  };
  const scenarioUrlParam = (root, name) => {
    if (root.dataset.cfxDeepLinkState !== 'true') return '';
    try {
      const params = new URL(window.location.href).searchParams;
      return params.get(scenarioUrlKey(root, name)) || params.get(name) || '';
    } catch { return ''; }
  };
  const scenarioUrlKey = (root, name) => {
    const chartId = (root.dataset.cfxChartId || 'chart').replace(/[^a-z0-9_.-]+/gi, '-').replace(/^-+|-+$/g, '') || 'chart';
    return 'cfx-' + chartId + '-' + name;
  };
  const syncScenarioUrl = (root, scenarioId, stepIndex) => {
    if (root.dataset.cfxDeepLinkState !== 'true' || !window.history || !window.history.replaceState) return;
    try {
      const url = new URL(window.location.href);
      const scenarioKey = scenarioUrlKey(root, 'scenario');
      const stepKey = scenarioUrlKey(root, 'scenarioStep');
      scenarioId ? url.searchParams.set(scenarioKey, scenarioId) : url.searchParams.delete(scenarioKey);
      stepIndex === undefined || stepIndex === null || stepIndex === '' ? url.searchParams.delete(stepKey) : url.searchParams.set(stepKey, stepIndex);
      url.searchParams.delete('scenario');
      url.searchParams.delete('scenarioStep');
      window.history.replaceState(window.history.state, '', url);
    } catch { }
  };
  const scenarioButton = (root, scenarioId) => Array.from(root.querySelectorAll('[data-cfx-scenario]')).find((button) => (button.dataset.cfxScenario || '') === scenarioId) || null;
  const scenarioRoute = (root, scenarioId) => {
    const button = scenarioButton(root, scenarioId);
    if (!button || !scenarioId) return null;
    return {
      id: scenarioId,
      label: button.dataset.cfxScenarioLabel || button.textContent.trim(),
      description: button.dataset.cfxScenarioDescription || '',
      color: button.dataset.cfxScenarioColor || '',
      steps: readJson(button.dataset.cfxScenarioSteps || '[]', []),
      metadata: readJson(button.dataset.cfxScenarioMetadata || '{}', {})
    };
  };
  const scenarioTargetMatches = (node, step) => {
    const data = node.dataset || {};
    if ((step.targetKind || '') === 'series' && data.cfxSeries === step.targetId) return true;
    if ((step.targetKind || '') === 'element' && (data.cfxId === step.targetId || node.id === step.targetId)) return true;
    if (data.cfxRole === step.targetKind && (data.cfxLabel === step.targetId || data.cfxValue === step.targetId)) return true;
    return false;
  };
  const scenarioTargetCandidates = (root, route) => {
    const candidates = new Set(root.querySelectorAll('.cfx-interactive-region,[data-cfx-label],[data-cfx-role],[data-cfx-series],[data-cfx-id]'));
    const elementIds = new Set((route ? route.steps : [])
      .filter((step) => (step.targetKind || '') === 'element' && step.targetId)
      .map((step) => step.targetId));
    if (elementIds.size) {
      root.querySelectorAll('[id]').forEach((node) => {
        if (!elementIds.has(node.id) || node.closest('defs')) return;
        if (node.querySelector('.cfx-interactive-region,[data-cfx-label],[data-cfx-role],[data-cfx-series],[data-cfx-id]')) return;
        candidates.add(node);
      });
    }
    return Array.from(candidates);
  };
  const scenarioTargets = (root, route, stepIndex) => {
    const matches = new Set();
    if (!route) return matches;
    const steps = stepIndex === undefined || stepIndex === null ? route.steps : route.steps.filter((_, index) => index === Number(stepIndex));
    scenarioTargetCandidates(root, route).forEach((node) => {
      if (steps.some((step) => scenarioTargetMatches(node, step))) matches.add(node);
    });
    return matches;
  };
  const renderScenarioPanel = (root, route) => {
    const panel = root.querySelector('[data-cfx-scenario-panel]');
    if (!panel) return;
    const title = panel.querySelector('[data-cfx-scenario-panel-title]');
    const meta = panel.querySelector('[data-cfx-scenario-panel-meta]');
    const steps = panel.querySelector('[data-cfx-scenario-panel-steps]');
    if (!route) {
      panel.removeAttribute('data-cfx-panel-active-scenario');
      panel.style.removeProperty('--cfx-scenario-color');
      if (title) title.textContent = 'All';
      if (meta) meta.textContent = 'All chart data visible';
      if (steps) steps.replaceChildren();
      return;
    }
    panel.setAttribute('data-cfx-panel-active-scenario', route.id);
    if (route.color) panel.style.setProperty('--cfx-scenario-color', route.color);
    else panel.style.removeProperty('--cfx-scenario-color');
    if (title) title.textContent = route.label || route.id;
    if (meta) {
      const pairs = Object.entries(route.metadata || {}).slice(0, 2).map(([key, value]) => key + ': ' + value);
      meta.textContent = [route.description, route.steps.length ? route.steps.length + ' steps' : '', ...pairs].filter(Boolean).join(' / ');
    }
    if (steps) {
      steps.replaceChildren();
      route.steps.forEach((step, index) => {
        const item = document.createElement('li');
        item.setAttribute('data-cfx-scenario-step-index', String(index + 1));
        item.setAttribute('data-cfx-scenario-target-kind', step.targetKind || '');
        item.setAttribute('data-cfx-scenario-target-id', step.targetId || '');
        item.setAttribute('role', 'button');
        item.setAttribute('tabindex', '0');
        item.textContent = step.label || step.targetId || '';
        steps.appendChild(item);
      });
    }
  };
  const clearScenarioStep = (root) => {
    root.removeAttribute('data-cfx-active-scenario-step');
    root.querySelectorAll('.cfx-scenario-step-active,.cfx-scenario-step-muted').forEach((node) => node.classList.remove('cfx-scenario-step-active', 'cfx-scenario-step-muted'));
    root.querySelectorAll('[data-cfx-scenario-step-index]').forEach((node) => node.removeAttribute('aria-current'));
  };
  const setScenarioStep = (root, stepIndex, emit, sync) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    const index = clamp(Number(stepIndex) || 0, 0, route.steps.length - 1);
    const targets = scenarioTargets(root, route, index);
    root.dataset.cfxActiveScenarioStep = String(index);
    root.querySelectorAll('.cfx-scenario-active').forEach((node) => {
      const active = targets.has(node);
      node.classList.toggle('cfx-scenario-step-active', active);
      node.classList.toggle('cfx-scenario-step-muted', !active);
    });
    root.querySelectorAll('[data-cfx-scenario-step-index]').forEach((node) => {
      node.getAttribute('data-cfx-scenario-step-index') === String(index + 1) ? node.setAttribute('aria-current', 'step') : node.removeAttribute('aria-current');
    });
    syncScenarioUrl(root, route.id, index);
    if (emit !== false) emitHostEvent(root, 'cfxscenariostep', { scenarioId: route.id, index, step: route.steps[index] });
    if (sync !== false) emitSync(root, { action: 'scenario-step', scenarioId: route.id, index });
  };
  const setScenario = (root, scenarioId, emit, sync) => {
    const route = scenarioRoute(root, scenarioId);
    stopScenarioPlayback(root);
    clearScenarioStep(root);
    root.querySelectorAll('.cfx-scenario-active,.cfx-scenario-muted').forEach((node) => node.classList.remove('cfx-scenario-active', 'cfx-scenario-muted'));
    root.querySelectorAll('[data-cfx-scenario]').forEach((button) => button.setAttribute('aria-pressed', (button.dataset.cfxScenario || '') === (route ? route.id : '') ? 'true' : 'false'));
    if (!route) {
      root.removeAttribute('data-cfx-active-scenario');
      renderScenarioPanel(root, null);
      syncScenarioUrl(root, '', '');
      if (emit !== false) emitHostEvent(root, 'cfxscenarioclear', {});
      if (sync !== false) emitSync(root, { action: 'scenario', scenarioId: '' });
      return;
    }
    root.dataset.cfxActiveScenario = route.id;
    if (route.color) root.style.setProperty('--cfx-scenario-color', route.color);
    else root.style.removeProperty('--cfx-scenario-color');
    const targets = scenarioTargets(root, route, null);
    scenarioTargetCandidates(root, route).forEach((node) => {
      const active = targets.has(node);
      node.classList.toggle('cfx-scenario-active', active);
      node.classList.toggle('cfx-scenario-muted', !active);
    });
    renderScenarioPanel(root, route);
    syncScenarioUrl(root, route.id, '');
    if (emit !== false) emitHostEvent(root, 'cfxscenario', { scenarioId: route.id, label: route.label, description: route.description, color: route.color, steps: route.steps, metadata: route.metadata });
    if (sync !== false) emitSync(root, { action: 'scenario', scenarioId: route.id });
  };
  const stepScenario = (root, delta) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    const current = Number(root.dataset.cfxActiveScenarioStep || (delta > 0 ? '-1' : route.steps.length));
    setScenarioStep(root, current + delta, true, true);
  };
  const stopScenarioPlayback = (root) => {
    if (root._cfxScenarioPlayback) window.clearInterval(root._cfxScenarioPlayback);
    root._cfxScenarioPlayback = null;
    root.querySelectorAll('[data-cfx-scenario-step-control="play"]').forEach((button) => button.setAttribute('aria-pressed', 'false'));
  };
  const playScenario = (root) => {
    const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
    if (!route || !route.steps.length) return;
    if (root._cfxScenarioPlayback) {
      stopScenarioPlayback(root);
      return;
    }
    root.querySelectorAll('[data-cfx-scenario-step-control="play"]').forEach((button) => button.setAttribute('aria-pressed', 'true'));
    let index = Number(root.dataset.cfxActiveScenarioStep || '-1') + 1;
    if (!Number.isFinite(index) || index >= route.steps.length) index = 0;
    root._cfxScenarioPlayback = window.setInterval(() => {
      if (index >= route.steps.length) {
        stopScenarioPlayback(root);
        return;
      }
      setScenarioStep(root, index++, true, true);
    }, 900);
    setScenarioStep(root, index++, true, true);
  };
  const copyScenarioLink = (root) => {
    syncScenarioUrl(root, root.dataset.cfxActiveScenario || '', root.dataset.cfxActiveScenarioStep || '');
    const url = window.location.href;
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(url).catch(() => {});
    emitHostEvent(root, 'cfxscenariolink', { url });
  };
  document.querySelectorAll('.cfx-interactive-chart').forEach((root) => {
    const tip = root.querySelector('.cfx-tooltip');
    const stage = root.querySelector('.cfx-stage');
    const brush = root.querySelector('.cfx-brush-box');
    if (!tip) return;
    applyViewport(root, getState(root));
    const targets = root.querySelectorAll('.cfx-interactive-region,[data-cfx-label],[data-cfx-role="legend-item"]');
    targets.forEach((node) => {
      if (!node.hasAttribute('tabindex')) node.setAttribute('tabindex', '0');
      node.addEventListener('pointerenter', (event) => showTip(root, tip, node, event));
      node.addEventListener('pointermove', (event) => moveTip(tip, event));
      node.addEventListener('pointerleave', () => hideTip(tip));
      node.addEventListener('focus', (event) => showTip(root, tip, node, event));
      node.addEventListener('blur', () => hideTip(tip));
      node.addEventListener('click', () => {
        if ((node.dataset ? node.dataset.cfxRole : '') === 'legend-item') toggleSeries(root, node);
        else toggleSelection(root, node);
      });
      node.addEventListener('keydown', (event) => {
        if (!hasFeature(root, 'KeyboardNavigation')) return;
        if (event.key !== 'Enter' && event.key !== ' ') return;
        event.preventDefault();
        node.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      });
    });
    root.querySelectorAll('[data-cfx-zoom]').forEach((button) => {
      button.addEventListener('click', () => zoomBy(root, button.dataset.cfxZoom === 'in' ? 1.25 : 0.8));
    });
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      button.addEventListener('click', () => setMode(root, button.dataset.cfxModeButton || ''));
    });
    root.querySelectorAll('[data-cfx-export]').forEach((button) => {
      button.addEventListener('click', () => {
        if (button.dataset.cfxExport === 'png') exportPng(root);
        else exportSvg(root);
      });
    });
    root.querySelectorAll('[data-cfx-scenario]').forEach((button) => {
      button.addEventListener('click', () => setScenario(root, button.dataset.cfxScenario || '', true));
    });
    if (hasFeature(root, 'StepPlayback')) {
      root.querySelectorAll('[data-cfx-scenario-step-control]').forEach((button) => {
        button.addEventListener('click', () => {
          const action = button.dataset.cfxScenarioStepControl || '';
          if (action === 'previous') stepScenario(root, -1);
          else if (action === 'next') stepScenario(root, 1);
          else if (action === 'play') playScenario(root);
          else if (action === 'reset') { stopScenarioPlayback(root); clearScenarioStep(root); }
          else if (action === 'link') copyScenarioLink(root);
        });
      });
      const stepsList = root.querySelector('[data-cfx-scenario-panel-steps]');
      if (stepsList) {
        const focusStep = (target) => {
          const item = target instanceof Element ? target.closest('[data-cfx-scenario-step-index]') : null;
          if (item && stepsList.contains(item)) setScenarioStep(root, Number(item.getAttribute('data-cfx-scenario-step-index')) - 1, true, true);
        };
        stepsList.addEventListener('click', (event) => focusStep(event.target));
        stepsList.addEventListener('keydown', (event) => {
          if (event.key !== 'Enter' && event.key !== ' ') return;
          event.preventDefault();
          focusStep(event.target);
        });
      }
    }
    if (hasFeature(root, 'Scenarios')) {
      const initialScenario = scenarioUrlParam(root, 'scenario') || root.dataset.cfxActiveScenario || '';
      const initialScenarioStep = scenarioUrlParam(root, 'scenarioStep');
      setScenario(root, initialScenario, false, false);
      if (hasFeature(root, 'StepPlayback') && initialScenarioStep) setScenarioStep(root, initialScenarioStep, false, false);
    }
    if (stage) {
      stage.addEventListener('wheel', (event) => {
        if (!hasFeature(root, 'Zoom')) return;
        event.preventDefault();
        zoomBy(root, event.deltaY < 0 ? 1.08 : 0.92);
      }, { passive: false });
      let drag = null;
      stage.addEventListener('pointerdown', (event) => {
        if (event.button !== 0) return;
        if (root.dataset.cfxMode === 'pan' && hasFeature(root, 'Pan')) {
          const state = getState(root);
          drag = { mode: 'pan', id: event.pointerId, x: event.clientX, y: event.clientY, panX: state.panX, panY: state.panY };
          stage.setPointerCapture(event.pointerId);
        } else if (root.dataset.cfxMode === 'brush' && hasFeature(root, 'Brush') && brush) {
          const rect = stage.getBoundingClientRect();
          drag = { mode: 'brush', id: event.pointerId, left: event.clientX - rect.left, top: event.clientY - rect.top };
          brush.hidden = false;
          brush.style.left = drag.left + 'px';
          brush.style.top = drag.top + 'px';
          brush.style.width = '0px';
          brush.style.height = '0px';
          stage.setPointerCapture(event.pointerId);
        }
      });
      stage.addEventListener('pointermove', (event) => {
        if (!drag || drag.id !== event.pointerId) return;
        if (drag.mode === 'pan') {
          applyViewport(root, { zoom: getState(root).zoom, panX: drag.panX + event.clientX - drag.x, panY: drag.panY + event.clientY - drag.y });
        } else if (drag.mode === 'brush' && brush) {
          const rect = stage.getBoundingClientRect();
          const x = clamp(event.clientX - rect.left, 0, rect.width);
          const y = clamp(event.clientY - rect.top, 0, rect.height);
          const left = Math.min(drag.left, x);
          const top = Math.min(drag.top, y);
          brush.style.left = left + 'px';
          brush.style.top = top + 'px';
          brush.style.width = Math.abs(x - drag.left) + 'px';
          brush.style.height = Math.abs(y - drag.top) + 'px';
        }
      });
      stage.addEventListener('pointerup', (event) => {
        if (!drag || drag.id !== event.pointerId) return;
        if (drag.mode === 'brush' && brush) {
          root.dataset.cfxBrush = [brush.style.left, brush.style.top, brush.style.width, brush.style.height].join(' ');
          emitHostEvent(root, 'cfxbrush', { bounds: root.dataset.cfxBrush });
          emitSync(root, { action: 'brush', bounds: root.dataset.cfxBrush });
        } else if (drag.mode === 'pan') {
          emitHostEvent(root, 'cfxviewport', { state: getState(root) });
          emitSync(root, { action: 'viewport', state: getState(root) });
        }
        stage.releasePointerCapture(event.pointerId);
        drag = null;
      });
      stage.addEventListener('pointercancel', () => { drag = null; });
    }
    const reset = root.querySelector('[data-cfx-reset]');
    if (reset) reset.addEventListener('click', () => {
      resetViewport(root);
      emitHostEvent(root, 'cfxreset', {});
      emitHostEvent(root, 'cfxviewport', { state: getState(root) });
      emitSync(root, { action: 'viewport', state: getState(root) });
      root.querySelectorAll('.cfx-selected').forEach((node) => {
        node.classList.remove('cfx-selected');
        node.removeAttribute('aria-selected');
      });
      root.querySelectorAll('.cfx-series-muted').forEach((node) => node.classList.remove('cfx-series-muted'));
      root.querySelectorAll('[data-cfx-muted]').forEach((node) => node.removeAttribute('data-cfx-muted'));
      if (brush) brush.hidden = true;
      hideTip(tip);
    });
  });
})();
