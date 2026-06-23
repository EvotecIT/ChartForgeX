  const bindCanvasHitTesting = (root) => {
    const canvas = root.querySelector('[data-cfx-role="graph-canvas"]');
    if (!canvas) return;
    const selectCanvasNode = (event) => {
      if (!root.classList.contains('cfx-graph-render-canvas')) return;
      const best = hitNodeAt(root, scenePoint(root, event));
      if (!best) return false;
      select(root, best.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
      root.__cfxGraphCanvasSelectionTick = Date.now();
      return true;
    };
    canvas.addEventListener('mousedown', event => {
      selectCanvasNode(event);
    });
    canvas.addEventListener('click', event => {
      if (Date.now() - (root.__cfxGraphCanvasSelectionTick || 0) < 250) return;
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
      root.dataset.cfxGraphLastPointerX = point.x.toFixed(3);
      root.dataset.cfxGraphLastPointerY = point.y.toFixed(3);
      root.dataset.cfxGraphLastPointerHit = node?.id || '';
      if (node && hasFeature(root, 'DragNodes')) {
        event.preventDefault();
        stage.setPointerCapture?.(event.pointerId);
        root.dataset.cfxGraphPhysicsState = 'paused';
        stopWorkerPhysics(root, true);
        root.classList.add('cfx-graph-dragging-node');
        root.dataset.cfxGraphLastPointerMode = 'node';
        node.el.setAttribute('data-node-fixed', 'true');
        active = { mode: 'node', pointerId: event.pointerId, nodeId: node.id };
        select(root, node.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
        root.__cfxGraphPointerSelectionTick = Date.now();
        root.__cfxGraphPointerSelectionId = node.id;
        emit(root, 'cfxgraphdragstart', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: node.id, x: node.x, y: node.y });
      } else if (hasFeature(root, 'Viewport')) {
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
        node.x = Math.max(24, Math.min(936, point.x));
        node.y = Math.max(24, Math.min(536, point.y));
        applyLayout(root, state);
        emit(root, 'cfxgraphdrag', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: node.id, x: node.x, y: node.y });
      } else if (active.mode === 'pan') {
        setViewport(root, { x: active.view.x + point.screenX - active.screenX, y: active.view.y + point.screenY - active.screenY, scale: active.view.scale });
      }
    });
    const finish = (event) => {
      if (!active || active.pointerId !== event.pointerId) return;
      stage.releasePointerCapture?.(event.pointerId);
      if (active.mode === 'node') emit(root, 'cfxgraphdragend', { graphId: attr(root, 'data-cfx-graph-id'), nodeId: active.nodeId });
      root.classList.remove('cfx-graph-dragging-node', 'cfx-graph-panning');
      active = null;
    };
    stage.addEventListener('pointerup', finish);
    stage.addEventListener('pointercancel', finish);
    stage.addEventListener('wheel', event => {
      if (!hasFeature(root, 'Viewport')) return;
      event.preventDefault();
      const point = scenePoint(root, event);
      zoomViewport(root, event.deltaY < 0 ? 1.12 : 0.88, { x: point.screenX, y: point.screenY });
    }, { passive: false });
  };
  const exportGraph = (root, format) => {
    const name = `${attr(root, 'data-cfx-graph-id') || 'graph'}.${format}`;
    let content = '';
    let mime = 'application/octet-stream';
    if (format === 'svg') {
      const svg = root.querySelector('[data-cfx-role="graph-scene"]');
      content = svg ? new XMLSerializer().serializeToString(svg) : '';
      mime = 'image/svg+xml';
    } else if (format === 'json') {
      content = JSON.stringify(exportGraphJson(root), null, 2);
      mime = 'application/json';
    } else if (format === 'png') {
      const canvas = root.querySelector('[data-cfx-role="graph-canvas"]');
      drawCanvas(root, graphState(root), { force: true });
      content = canvas ? canvas.toDataURL('image/png') : '';
      mime = 'image/png';
    }
    if (!content) return;
    root.dataset.cfxGraphLastExport = format;
    if (!emit(root, 'cfxgraphexport', { graphId: attr(root, 'data-cfx-graph-id'), format, fileName: name, mimeType: mime, content }, { cancelable: true })) return;
    downloadExport(name, mime, content);
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
      sampleTicks: Number(root.dataset.cfxGraphPerformanceSampleTicks || 0),
      sampleBudgetMs: Number(root.dataset.cfxGraphPerformanceSampleBudgetMs || 0),
      thread: root.dataset.cfxGraphPerformanceThread || '',
      acceleration: root.dataset.cfxGraphPerformanceAcceleration || ''
    },
    nodes: graphState(root).nodes.map(node => ({ id: node.id, x: Number(node.x.toFixed(3)), y: Number(node.y.toFixed(3)), fixed: attr(node.el, 'data-node-fixed') === 'true', kind: attr(node.el, 'data-node-kind'), status: attr(node.el, 'data-cfx-status') })),
    edges: items(root, '[data-cfx-role="graph-edge"]').map(edge => ({ id: attr(edge, 'data-edge-id'), source: attr(edge, 'data-source-node-id'), target: attr(edge, 'data-target-node-id'), label: attr(edge, 'data-edge-label'), directed: attr(edge, 'data-edge-directed') === 'true' }))
  });
  const downloadExport = (name, mime, content) => {
    const isDataUrl = content.startsWith('data:');
    const url = isDataUrl ? content : URL.createObjectURL(new Blob([content], { type: mime }));
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    link.click();
    if (!isDataUrl) setTimeout(() => URL.revokeObjectURL(url), 500);
  };
  const bind = (root) => {
    root.setAttribute('data-cfx-graph-bound', 'true');
    indexHitTesting(root, graphState(root));
    applyLod(root);
    performanceGate(root);
    if (attr(root, 'data-cfx-lod-collapse-clusters') === 'true') applyClusterState(root, true);
    else applyClusterState(root, undefined);
    bindCanvasHitTesting(root);
    bindPointerInteractions(root);
    items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]').forEach(item => {
      item.addEventListener('click', event => {
        const id = attr(item, 'data-node-id') || attr(item, 'data-edge-id') || attr(item, 'data-cluster-id');
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
        if (action === 'fit') fitViewport(root);
        if (action === 'zoom-in') zoomViewport(root, 1.18);
        if (action === 'zoom-out') zoomViewport(root, 0.84);
        if (action === 'export-svg') exportGraph(root, 'svg');
        if (action === 'export-png') exportGraph(root, 'png');
        if (action === 'export-json') exportGraph(root, 'json');
        if (action === 'physics') {
          const running = root.dataset.cfxGraphPhysicsState === 'running';
          root.dataset.cfxGraphPhysicsState = running ? 'paused' : 'running';
          if (running) stopWorkerPhysics(root, true);
          if (!running) startPhysics(root);
        }
        if (action === 'stabilize') startPhysics(root);
        button.setAttribute('aria-pressed', attr(button, 'aria-pressed') === 'true' ? 'false' : 'true');
        emit(root, 'cfxgraphaction', { graphId: attr(root, 'data-cfx-graph-id'), action, physics: attr(root, 'data-cfx-graph-physics') });
      });
    });
    updateSelectionState(root);
    if (hasFeature(root, 'RuntimePhysics') && hasFeature(root, 'Stabilization')) startPhysics(root);
    emit(root, 'cfxgraphready', {
      graphId: attr(root, 'data-cfx-graph-id'),
      renderer: attr(root, 'data-cfx-graph-renderer'),
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

