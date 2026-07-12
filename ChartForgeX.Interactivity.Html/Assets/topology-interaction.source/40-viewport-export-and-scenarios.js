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
