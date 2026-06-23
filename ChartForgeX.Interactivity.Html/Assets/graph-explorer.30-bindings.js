  const bindCanvasHitTesting = (root) => {
    const canvas = root.querySelector('[data-cfx-role="graph-canvas"]');
    if (!canvas) return;
    const selectCanvasNode = (event) => {
      if (!root.classList.contains('cfx-graph-render-canvas')) return;
      const best = hitGraphItemAt(root, scenePoint(root, event));
      if (!best) return false;
      select(root, best.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
      root.__cfxGraphCanvasSelectionTick = Date.now();
      return true;
    };
    canvas.addEventListener('click', event => {
      if (Date.now() - (root.__cfxGraphCanvasSelectionTick || 0) < 250) return;
      if (Date.now() - (root.__cfxGraphPointerSelectionTick || 0) < 250) return;
      selectCanvasNode(event);
    });
  };
  const bindPointerInteractions = (root) => {
    const stage = root.querySelector('.cfx-graph-stage');
    if (!stage) return;
    let active = null;
    stage.addEventListener('pointerdown', event => {
      if (event.button !== 0) return;
      const point = scenePoint(root, event);
      const targetNode = event.target.closest ? event.target.closest('[data-cfx-role="graph-node"]') : null;
      const node = targetNode ? graphState(root).nodes.find(item => item.el === targetNode) : hitNodeAt(root, point);
      const graphItem = event.target.closest ? event.target.closest('[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-edge-label"],[data-cfx-role="graph-cluster"]') : null;
      const hitItem = node || graphItem || hitGraphItemAt(root, point);
      root.dataset.cfxGraphLastPointerX = point.x.toFixed(3);
      root.dataset.cfxGraphLastPointerY = point.y.toFixed(3);
      root.dataset.cfxGraphLastPointerHit = node?.id || '';
      if (node && hasFeature(root, 'DragNodes')) {
        event.preventDefault();
        stage.setPointerCapture?.(event.pointerId);
        root.dataset.cfxGraphLastPointerMode = 'node';
        active = { mode: 'node', pointerId: event.pointerId, nodeId: node.id, startX: point.x, startY: point.y, fixed: attr(node.el, 'data-node-fixed'), moved: false };
        select(root, node.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
        root.__cfxGraphPointerSelectionTick = Date.now();
        root.__cfxGraphPointerSelectionId = node.id;
      } else if (hasFeature(root, 'Viewport') && !hitItem) {
        event.preventDefault();
        stage.setPointerCapture?.(event.pointerId);
        root.classList.add('cfx-graph-panning');
        root.dataset.cfxGraphLastPointerMode = 'pan';
        active = { mode: 'pan', pointerId: event.pointerId, screenX: point.screenX, screenY: point.screenY, view: viewport(root) };
      } else {
        root.dataset.cfxGraphLastPointerMode = 'none';
      }
    });
    stage.addEventListener('pointermove', event => {
      if (!active || active.pointerId !== event.pointerId) return;
      const point = scenePoint(root, event);
      if (active.mode === 'node') {
        const state = graphState(root);
        const node = state.nodes.find(item => item.id === active.nodeId);
        if (!node) return;
        const dragThreshold = 3;
        if (!active.moved && Math.hypot(point.x - active.startX, point.y - active.startY) < dragThreshold) return;
        if (!active.moved) {
          active.moved = true;
          root.dataset.cfxGraphPhysicsState = 'paused';
          stopWorkerPhysics(root, true);
          stopMainPhysics(root, true);
          root.classList.add('cfx-graph-dragging-node');
          emit(root, 'cfxgraphdragstart', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: node.id, x: node.x, y: node.y });
        }
        node.el.setAttribute('data-node-fixed', 'true');
        node.x = point.x;
        node.y = point.y;
        applyLayout(root, state);
        emit(root, 'cfxgraphdrag', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: node.id, x: node.x, y: node.y });
      } else if (active.mode === 'pan') {
        root.__cfxGraphViewportTouched = true;
        setViewport(root, { x: active.view.x + point.screenX - active.screenX, y: active.view.y + point.screenY - active.screenY, scale: active.view.scale });
      }
    });
    const finish = (event) => {
      if (!active || active.pointerId !== event.pointerId) return;
      stage.releasePointerCapture?.(event.pointerId);
      if (active.mode === 'node' && active.moved) {
        root.__cfxGraphSuppressClickId = active.nodeId;
        emit(root, 'cfxgraphdragend', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: active.nodeId });
      }
      if (active.mode === 'node' && !active.moved) {
        const node = graphState(root).nodes.find(item => item.id === active.nodeId);
        if (node) node.el.setAttribute('data-node-fixed', active.fixed || 'false');
      }
      root.classList.remove('cfx-graph-dragging-node', 'cfx-graph-panning');
      active = null;
    };
    stage.addEventListener('pointerup', finish);
    stage.addEventListener('pointercancel', finish);
    stage.addEventListener('wheel', event => {
      if (!hasFeature(root, 'Viewport')) return;
      event.preventDefault();
      root.__cfxGraphViewportTouched = true;
      const point = scenePoint(root, event);
      zoomViewport(root, event.deltaY < 0 ? 1.12 : 0.88, { x: point.screenX, y: point.screenY });
    }, { passive: false });
  };
  const exportGraph = async (root, format) => {
    const name = `${attr(root, 'data-cfx-graph-id') || 'graph'}.${format}`;
    let content = '';
    let mime = 'application/octet-stream';
    if (format === 'svg') {
      content = exportSvgContent(root);
      mime = 'image/svg+xml';
    } else if (format === 'json') {
      content = JSON.stringify(exportGraphJson(root), null, 2);
      mime = 'application/json';
    } else if (format === 'png') {
      const canvas = root.querySelector('[data-cfx-role="graph-canvas"]');
      const state = graphState(root);
      await preloadCanvasImages(root, state);
      drawCanvas(root, state, { force: true });
      try {
        content = canvas ? canvas.toDataURL('image/png') : '';
      } catch (error) {
        root.dataset.cfxGraphLastExportError = error?.name || 'export-error';
        emit(root, 'cfxgraphexporterror', { graphId: attr(root, 'data-cfx-graph-id'), format, fileName: name, error: root.dataset.cfxGraphLastExportError });
        return;
      }
      mime = 'image/png';
    }
    if (!content) return;
    root.dataset.cfxGraphLastExport = format;
    if (!emit(root, 'cfxgraphexport', { graphId: attr(root, 'data-cfx-graph-id'), format, fileName: name, mimeType: mime, content }, { cancelable: true })) return;
    downloadExport(name, mime, content);
  };
  const preloadCanvasImages = (root, state) => Promise.all(state.nodes.filter(node => node.shape === 'image' && node.imageUrl).map(node => new Promise(resolve => {
    const image = graphImage(node.imageUrl, () => resolve());
    if (!image || image.complete) {
      resolve();
      return;
    }
    let settled = false;
    const done = () => {
      if (settled) return;
      settled = true;
      resolve();
    };
    image.addEventListener?.('load', done, { once: true });
    image.addEventListener?.('error', done, { once: true });
    setTimeout(done, 1500);
  })));
  const exportSvgContent = (root) => {
    const svg = root.querySelector('[data-cfx-role="graph-scene"]');
    if (!svg) return '';
    const clone = svg.cloneNode(true);
    ['cfx-graph-lod-compact', 'cfx-graph-lod-hide-edge-labels', 'cfx-graph-neighborhood-active', 'cfx-graph-performance-gated'].forEach(name => {
      if (root.classList.contains(name)) clone.classList.add(name);
    });
    const styleSource = root.ownerDocument.querySelector('style[data-cfx-graph-assets="true"]')
      || Array.from(root.ownerDocument.querySelectorAll('style')).find(style => (style.textContent || '').includes('.cfx-graph-explorer'));
    if (styleSource?.textContent) {
      const style = root.ownerDocument.createElementNS('http:' + '//www.w3.org/2000/svg', 'style');
      style.setAttribute('data-cfx-export-style', 'true');
      style.textContent = styleSource.textContent;
      const defs = clone.querySelector('defs');
      if (defs) defs.insertBefore(style, defs.firstChild);
      else clone.insertBefore(style, clone.firstChild);
    }
    return new XMLSerializer().serializeToString(clone);
  };
  const syncPhysicsControls = (root) => {
    const pressed = root.dataset.cfxGraphPhysicsState === 'running' ? 'true' : 'false';
    items(root, '[data-cfx-graph-action="physics"]').forEach(button => button.setAttribute('aria-pressed', pressed));
  };
  const exportGraphJson = (root) => ({
    graphId: attr(root, 'data-cfx-graph-id'),
    renderer: root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer'),
    viewport: viewport(root),
    selection: {
      count: Number(root.dataset.cfxGraphSelectionCount || 0),
      ids: (root.dataset.cfxGraphSelectionIds || '').split(',').filter(Boolean),
      roles: (root.dataset.cfxGraphSelectionRoles || '').split(',').filter(Boolean),
      primary: root.dataset.cfxGraphSelectionPrimary || ''
    },
    focus: { active: root.dataset.cfxGraphFocus === 'active', nodeId: root.dataset.cfxGraphFocusNode || '' },
    performance: {
      state: root.dataset.cfxGraphPerformance || '',
      budget: root.dataset.cfxGraphPerformanceBudget || '',
      samples: Number(root.dataset.cfxGraphPerformanceSamples || 0),
      budgetMisses: Number(root.dataset.cfxGraphPerformanceBudgetMisses || 0),
      maxSampleMs: Number(root.dataset.cfxGraphPerformanceMaxSampleMs || 0),
      lastSampleMs: Number(root.dataset.cfxGraphPerformanceLastSampleMs || 0),
      overlapPressureEvents: Number(root.dataset.cfxGraphPerformanceOverlapPressureEvents || 0),
      sampleTicks: Number(root.dataset.cfxGraphPerformanceSampleTicks || 0),
      sampleBudgetMs: Number(root.dataset.cfxGraphPerformanceSampleBudgetMs || 0),
      thread: root.dataset.cfxGraphPerformanceThread || '',
      acceleration: root.dataset.cfxGraphPerformanceAcceleration || ''
    },
    nodes: graphState(root).nodes.map(node => ({ id: node.id, label: attr(node.el, 'data-node-label'), x: Number(node.x.toFixed(3)), y: Number(node.y.toFixed(3)), fixed: attr(node.el, 'data-node-fixed') === 'true', kind: attr(node.el, 'data-node-kind'), groupId: attr(node.el, 'data-node-group'), clusterId: attr(node.el, 'data-node-cluster'), status: attr(node.el, 'data-cfx-status'), size: Number(attr(node.el, 'data-node-size') || 0), shape: attr(node.el, 'data-node-shape'), icon: attr(node.el, 'data-node-icon'), imageUrl: attr(node.el, 'data-node-image-url'), imageAlt: attr(node.el.querySelector('image'), 'aria-label'), hidden: node.el.classList.contains('cfx-graph-hidden') || node.el.classList.contains('cfx-graph-cluster-collapsed-member'), search: attr(node.el, 'data-cfx-search'), metadata: metadataDetail(node.el) })),
    edges: items(root, '[data-cfx-role="graph-edge"]').map(edge => ({ id: attr(edge, 'data-edge-id'), source: attr(edge, 'data-source-node-id'), target: attr(edge, 'data-target-node-id'), label: attr(edge, 'data-edge-label'), kind: attr(edge, 'data-edge-kind'), status: attr(edge, 'data-cfx-status'), weight: Number(attr(edge, 'data-edge-weight') || 0), length: Number(attr(edge, 'data-edge-length') || 0), shape: attr(edge, 'data-edge-shape'), curvature: Number(attr(edge, 'data-edge-curvature') || 0), dashed: attr(edge, 'data-edge-dashed') === 'true', showLabel: attr(edge, 'data-edge-show-label') !== 'false', directed: attr(edge, 'data-edge-directed') === 'true', hidden: edge.classList.contains('cfx-graph-hidden') || edge.classList.contains('cfx-graph-cluster-collapsed-member'), search: attr(edge, 'data-cfx-search'), metadata: metadataDetail(edge) })),
    clusters: items(root, '[data-cfx-role="graph-cluster"]').map(cluster => ({ id: attr(cluster, 'data-cluster-id'), label: attr(cluster, 'data-cluster-label'), kind: attr(cluster, 'data-cluster-kind'), nodeIds: idList(attr(cluster, 'data-cluster-node-ids')), collapsed: attr(cluster, 'data-cluster-collapsed') === 'true', hidden: cluster.classList.contains('cfx-graph-hidden'), search: attr(cluster, 'data-cfx-search'), metadata: metadataDetail(cluster) }))
  });
  const downloadExport = (name, mime, content) => {
    const isDataUrl = content.startsWith('data:');
    const url = isDataUrl ? content : URL.createObjectURL(new Blob([content], { type: mime }));
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    link.style.display = 'none';
    const parent = document.body || document.documentElement;
    if (parent) parent.appendChild(link);
    link.click();
    link.remove();
    if (!isDataUrl) setTimeout(() => URL.revokeObjectURL(url), 500);
  };
  const bind = (root) => {
    root.setAttribute('data-cfx-graph-bound', 'true');
    indexHitTesting(root, graphState(root));
    if (hasFeature(root, 'LevelOfDetail')) applyLod(root);
    else {
      const useCanvas = attr(root, 'data-cfx-graph-renderer') === 'canvas';
      root.classList.toggle('cfx-graph-render-canvas', useCanvas);
      root.classList.toggle('cfx-graph-render-svg', !useCanvas);
      root.dataset.cfxGraphRendererActive = useCanvas ? 'canvas' : 'svg';
    }
    performanceGate(root);
    if (hasFeature(root, 'Clustering')) {
      if (attr(root, 'data-cfx-lod-collapse-clusters') === 'true') applyClusterState(root, true);
      else applyClusterState(root, undefined);
    } else {
      items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => {
        cluster.classList.add('cfx-graph-cluster-expanded');
        cluster.setAttribute('data-cluster-collapsed', 'false');
      });
      items(root, '.cfx-graph-cluster-collapsed-member').forEach(item => item.classList.remove('cfx-graph-cluster-collapsed-member'));
      root.dataset.cfxGraphClusters = 'disabled';
      applyFilters(root);
      drawCanvas(root, graphState(root));
    }
    if (hasFeature(root, 'Viewport')) {
      fitViewport(root);
      const stage = root.querySelector('.cfx-graph-stage');
      if (stage && typeof ResizeObserver !== 'undefined') {
        let frame = 0;
        const observer = new ResizeObserver(() => {
          if (root.__cfxGraphViewportTouched) return;
          if (frame) cancelAnimationFrame(frame);
          frame = requestAnimationFrame(() => {
            frame = 0;
            fitViewport(root);
          });
        });
        observer.observe(stage);
        root.__cfxGraphResizeObserver = observer;
      }
    }
    bindCanvasHitTesting(root);
    bindPointerInteractions(root);
    items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]').forEach(item => {
      item.addEventListener('click', event => {
        const id = attr(item, 'data-node-id') || attr(item, 'data-edge-id') || attr(item, 'data-cluster-id');
        if (id && root.__cfxGraphSuppressClickId === id) {
          root.__cfxGraphSuppressClickId = '';
          event.preventDefault();
          return;
        }
        if (id && root.__cfxGraphPointerSelectionId === id && Date.now() - (root.__cfxGraphPointerSelectionTick || 0) < 250) return;
        select(root, item, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
      });
      item.addEventListener('keydown', event => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          select(root, item, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
        }
      });
    });
    const search = root.querySelector('[data-cfx-graph-search]');
    if (search) search.addEventListener('input', () => applyFilters(root));
    items(root, '[data-cfx-graph-filter]').forEach(filter => filter.addEventListener('change', () => applyFilters(root)));
    items(root, '[data-cfx-graph-action]').forEach(button => {
      button.addEventListener('click', () => {
        const action = attr(button, 'data-cfx-graph-action');
        if (action === 'clusters') applyClusterState(root, root.dataset.cfxGraphClusters !== 'collapsed');
        if (action === 'focus') toggleNeighborhoodFocus(root);
        if (action === 'clear-selection') clearSelection(root);
        if (action === 'fit' && hasFeature(root, 'Viewport')) {
          root.__cfxGraphViewportTouched = false;
          fitViewport(root);
        }
        if (action === 'zoom-in' && hasFeature(root, 'Viewport')) {
          root.__cfxGraphViewportTouched = true;
          zoomViewport(root, 1.18);
        }
        if (action === 'zoom-out' && hasFeature(root, 'Viewport')) {
          root.__cfxGraphViewportTouched = true;
          zoomViewport(root, 0.84);
        }
        if (action === 'export-svg') void exportGraph(root, 'svg');
        if (action === 'export-png') void exportGraph(root, 'png');
        if (action === 'export-json') void exportGraph(root, 'json');
        if (action === 'physics') {
          const running = root.dataset.cfxGraphPhysicsState === 'running';
          if (running) {
            root.dataset.cfxGraphPhysicsState = 'paused';
            stopWorkerPhysics(root, true);
            stopMainPhysics(root, true);
          }
          if (!running) startPhysics(root);
          syncPhysicsControls(root);
        }
        if (action === 'stabilize' && hasFeature(root, 'Stabilization')) startPhysics(root);
        if (action === 'physics') syncPhysicsControls(root);
        else button.setAttribute('aria-pressed', attr(button, 'aria-pressed') === 'true' ? 'false' : 'true');
        emit(root, 'cfxgraphaction', { graphId: attr(root, 'data-cfx-graph-id'), action, physics: attr(root, 'data-cfx-graph-physics') });
      });
    });
    updateSelectionState(root);
    if (hasFeature(root, 'RuntimePhysics') && hasFeature(root, 'Stabilization')) startPhysics(root);
    syncPhysicsControls(root);
    emit(root, 'cfxgraphready', {
      graphId: attr(root, 'data-cfx-graph-id'),
      renderer: root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer'),
      physics: attr(root, 'data-cfx-graph-physics'),
      nodeCount: Number(attr(root, 'data-cfx-graph-node-count')),
      edgeCount: Number(attr(root, 'data-cfx-graph-edge-count')),
      selectionCount: Number(root.dataset.cfxGraphSelectionCount || 0),
      performance: root.dataset.cfxGraphPerformance,
      performanceBudget: root.dataset.cfxGraphPerformanceBudget,
      lod: root.dataset.cfxGraphLod
    });
  };
  const start = () => roots().forEach(bind);
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
  else start();

