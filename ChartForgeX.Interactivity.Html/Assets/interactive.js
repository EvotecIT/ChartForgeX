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