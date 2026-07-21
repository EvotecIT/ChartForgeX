  const bindCanvasHitTesting = (root) => {
    const canvas = root.querySelector('[data-cfx-role="graph-canvas"]');
    if (!canvas) return;
    const selectCanvasNode = (event, best) => {
      if (!root.classList.contains('cfx-graph-render-canvas')) return;
      best = best || hitGraphItemAt(root, scenePoint(root, event));
      if (!best) return false;
      select(root, best.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
      root.__cfxGraphCanvasSelectionTick = Date.now();
      return true;
    };
    canvas.addEventListener('click', event => {
      const best = hitGraphItemAt(root, scenePoint(root, event));
      const bestId = best ? attr(best.el, 'data-node-id') || attr(best.el, 'data-edge-id') || attr(best.el, 'data-cluster-id') : '';
      if (bestId && root.__cfxGraphSuppressClickId === bestId) {
        root.__cfxGraphSuppressClickId = '';
        event.preventDefault();
        return;
      }
      if (Date.now() - (root.__cfxGraphCanvasSelectionTick || 0) < 250) return;
      if (Date.now() - (root.__cfxGraphPointerSelectionTick || 0) < 250) return;
      selectCanvasNode(event, best);
    });
    canvas.addEventListener('keydown', event => {
      if (!root.classList.contains('cfx-graph-render-canvas') || !hasFeature(root, 'Selection')) return;
      if (moveAcceleratedGraphSelection(root, event)) return;
      if (event.key !== 'Enter' && event.key !== ' ') return;
      const state = (root.__cfxGraphState || graphState(root));
      const best = state.byId.get(root.dataset.cfxGraphSelectionPrimary || '') || state.nodes.find(node => visible(node.el)) || state.clusters.find(cluster => visible(cluster.el)) || state.edges.find(edge => visible(edge.el));
      if (!best) return;
      event.preventDefault(); select(root, best.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
    });
  };
  const bindAcceleratedSvgKeyboard = (root) => {
    const scene = root.querySelector('[data-cfx-role="graph-scene"]');
    if (!scene) return;
    scene.addEventListener('keydown', event => {
      if (root.dataset.cfxGraphRendererActive !== 'svg' || attr(root, 'data-cfx-graph-accelerated-markup') !== 'true' || !hasFeature(root, 'Selection')) return;
      if (moveAcceleratedGraphSelection(root, event)) return;
      if (event.key !== 'Enter' && event.key !== ' ') return;
      const state = root.__cfxGraphState || graphState(root);
      const selected = state.byId.get(root.dataset.cfxGraphSelectionPrimary || '') || state.nodes.find(node => visible(node.el));
      if (!selected) return;
      event.preventDefault();
      select(root, selected.el, { additive: event.ctrlKey || event.metaKey || event.shiftKey, toggle: event.ctrlKey || event.metaKey || event.shiftKey });
    });
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
      const state = (root.__cfxGraphState || graphState(root));
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
  const preloadCanvasImages = (root, state) => Promise.all(state.nodes.filter(node => (node.shape === 'image' || node.shape === 'imageRect') && node.imageUrl).map(node => new Promise(resolve => {
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
    const state = (root.__cfxGraphState || graphState(root));
    syncSvgLayout(root, state); const clone = svg.cloneNode(true);
    materializeAcceleratedSvg(root, clone, state);
    const computed = root.ownerDocument.defaultView?.getComputedStyle(root);
    if (computed) {
      for (let index = 0; index < computed.length; index++) {
        const name = computed[index];
        if (name.startsWith('--cfx-')) clone.style.setProperty(name, computed.getPropertyValue(name));
      }
    }
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
    items(root, "[data-cfx-graph-action='physics']").forEach(button => {
      button.setAttribute('aria-pressed', pressed);
      button.setAttribute('aria-label', pressed === 'true' ? 'Pause physics' : 'Start physics');
      button.setAttribute('data-cfx-tooltip', pressed === 'true' ? 'Pause physics' : 'Start physics');
      const label = button.querySelector('.cfx-graph-tool-label');
      if (label) label.textContent = pressed === 'true' ? 'Pause' : 'Physics';
    });
  };
  const exportGraphJson = (root) => ({
    graphId: attr(root, 'data-cfx-graph-id'),
    metadata: metadataDetail(root),
    renderer: root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer'),
    viewport: viewport(root),
    selection: {
      count: Number(root.dataset.cfxGraphSelectionCount || 0),
      ids: (root.dataset.cfxGraphSelectionIds || '').split(',').filter(Boolean),
      roles: (root.dataset.cfxGraphSelectionRoles || '').split(',').filter(Boolean),
      primary: root.dataset.cfxGraphSelectionPrimary || ''
    },
    focus: { active: root.dataset.cfxGraphFocus === 'active', nodeId: root.dataset.cfxGraphFocusNode || '' }, hierarchy: { rootNodeId: root.dataset.cfxGraphHierarchyRoot || '', depth: Number(root.dataset.cfxGraphHierarchyDepth || attr(root, 'data-cfx-graph-hierarchy-depth') || 0), visibleNodeCount: Number(root.dataset.cfxGraphHierarchyVisibleNodes || attr(root, 'data-cfx-graph-node-count') || 0) }, clustering: { mode: attr(root, 'data-cfx-graph-cluster-mode'), adaptive: attr(root, 'data-cfx-graph-cluster-adaptive') === 'true', minimumClusterSize: Number(attr(root, 'data-cfx-graph-cluster-min-size') || 0), targetClusterSize: Number(attr(root, 'data-cfx-graph-cluster-target-size') || 0), collapseOnLoad: attr(root, 'data-cfx-lod-collapse-clusters') === 'true' || attr(root, 'data-cfx-graph-cluster-collapse-on-load') === 'true', lod: root.dataset.cfxGraphClusterLod || '', state: root.dataset.cfxGraphClusters || '', count: Number(attr(root, 'data-cfx-graph-cluster-count') || 0) }, manipulation: { enabled: attr(root, 'data-cfx-graph-manipulation') === 'true', capabilities: (attr(root, 'data-cfx-graph-manipulation-capabilities') || '').split(',').filter(Boolean) },
    performance: {
      state: root.dataset.cfxGraphPerformance || '', budget: root.dataset.cfxGraphPerformanceBudget || '',
      samples: Number(root.dataset.cfxGraphPerformanceSamples || 0),
      frameSamples: Number(root.dataset.cfxGraphPerformanceFrameSamples || 0), maxFrameMs: Number(root.dataset.cfxGraphPerformanceMaxFrameMs || 0), maxRenderMs: Number(root.dataset.cfxGraphPerformanceMaxRenderMs || 0),
      warmupFrameSamples: Number(root.dataset.cfxGraphPerformanceWarmupFrameSamples || 0), maxWarmupFrameMs: Number(root.dataset.cfxGraphPerformanceMaxWarmupFrameMs || 0), maxWarmupRenderMs: Number(root.dataset.cfxGraphPerformanceMaxWarmupRenderMs || 0),
      physicsSamples: Number(root.dataset.cfxGraphPerformancePhysicsSamples || 0), physicsBudgetMisses: Number(root.dataset.cfxGraphPerformancePhysicsBudgetMisses || 0),
      budgetMisses: Number(root.dataset.cfxGraphPerformanceBudgetMisses || 0), budgetMissRate: Number(root.dataset.cfxGraphPerformanceBudgetMissRate || 0),
      cadenceBudgetMisses: Number(root.dataset.cfxGraphPerformanceCadenceBudgetMisses || 0), cadenceBudgetMissRate: Number(root.dataset.cfxGraphPerformanceCadenceBudgetMissRate || 0),
      maxSampleMs: Number(root.dataset.cfxGraphPerformanceMaxSampleMs || 0),
      lastSampleMs: Number(root.dataset.cfxGraphPerformanceLastSampleMs || 0),
      overlapPressureEvents: Number(root.dataset.cfxGraphPerformanceOverlapPressureEvents || 0), communityPackingEvents: Number(root.dataset.cfxGraphPerformanceCommunityPackingEvents || 0),
      sampleTicks: Number(root.dataset.cfxGraphPerformanceSampleTicks || 0),
      sampleBudgetMs: Number(root.dataset.cfxGraphPerformanceSampleBudgetMs || 0),
      thread: root.dataset.cfxGraphPerformanceThread || '', acceleration: root.dataset.cfxGraphPerformanceAcceleration || ''
    },
    nodes: (root.__cfxGraphState || graphState(root)).nodes.map(node => ({ id: node.id, label: attr(node.el, 'data-node-label'), secondaryLabel: attr(node.el, 'data-node-secondary-label'), badge: attr(node.el, 'data-node-badge'), parentId: attr(node.el, 'data-node-parent'), x: Number(node.x.toFixed(3)), y: Number(node.y.toFixed(3)), fixed: attr(node.el, 'data-node-fixed') === 'true', level: attr(node.el, 'data-node-level') === '' ? null : Number(attr(node.el, 'data-node-level')), kind: attr(node.el, 'data-node-kind'), groupId: attr(node.el, 'data-node-group'), clusterId: attr(node.el, 'data-node-cluster'), status: attr(node.el, 'data-cfx-status'), size: Number(attr(node.el, 'data-node-size') || 0), shape: attr(node.el, 'data-node-shape'), icon: attr(node.el, 'data-node-icon'), imageUrl: attr(node.el, 'data-node-image-url'), imageAlt: attr(node.el, 'data-node-image-alt') || attr(node.el.querySelector('image'), 'aria-label'), style: { backgroundColor: attr(node.el, 'data-node-background-color'), borderColor: attr(node.el, 'data-node-border-color'), labelColor: attr(node.el, 'data-node-label-color'), labelBackgroundColor: attr(node.el, 'data-node-label-background-color'), shadow: attr(node.el, 'data-node-shadow') === 'true' }, hidden: attr(node.el, 'data-node-hidden') === 'true', search: attr(node.el, 'data-cfx-search'), metadata: metadataDetail(node.el) })),
    edges: items(root, '[data-cfx-role="graph-edge"]').map(edge => ({ id: attr(edge, 'data-edge-id'), source: attr(edge, 'data-source-node-id'), target: attr(edge, 'data-target-node-id'), label: attr(edge, 'data-edge-label'), kind: attr(edge, 'data-edge-kind'), status: attr(edge, 'data-cfx-status'), weight: Number(attr(edge, 'data-edge-weight') || 0), length: Number(attr(edge, 'data-edge-length') || 0), shape: attr(edge, 'data-edge-shape'), routePoints: routePoints(attr(edge, 'data-edge-route-points')), curvature: Number(attr(edge, 'data-edge-curvature') || 0), dashed: attr(edge, 'data-edge-dashed') === 'true', dashPattern: attr(edge, 'data-edge-dash-pattern'), showLabel: attr(edge, 'data-edge-show-label') !== 'false', directed: attr(edge, 'data-edge-directed') === 'true', sourceArrow: attr(edge, 'data-edge-source-arrow') === 'true', targetArrow: attr(edge, 'data-edge-target-arrow') === 'true', physics: attr(edge, 'data-edge-physics') !== 'false', style: { color: attr(edge, 'data-edge-color'), labelColor: attr(edge, 'data-edge-label-color'), width: Number(attr(edge, 'data-edge-width') || 0) }, hidden: attr(edge, 'data-edge-hidden') === 'true', search: attr(edge, 'data-cfx-search'), metadata: metadataDetail(edge) })),
    clusters: items(root, '[data-cfx-role="graph-cluster"]').map(cluster => ({ id: attr(cluster, 'data-cluster-id'), label: attr(cluster, 'data-cluster-label'), kind: attr(cluster, 'data-cluster-kind'), parentClusterId: attr(cluster, 'data-cluster-parent'), nodeIds: idList(attr(cluster, 'data-cluster-node-ids')), collapsed: attr(cluster, 'data-cluster-collapsed') === 'true', hidden: false, search: attr(cluster, 'data-cfx-search'), metadata: metadataDetail(cluster) }))
  });
  const bindCommandMenus = (root) => {
    const menus = Array.from(root.querySelectorAll('.cfx-graph-command-menu'));
    if (!menus.length) return;
    root.addEventListener('pointerdown', event => menus.forEach(menu => {
      if (menu.open && !menu.contains(event.target)) menu.removeAttribute('open');
    }));
    menus.forEach(menu => {
      const summary = menu.querySelector('summary');
      const sync = () => summary?.setAttribute('aria-expanded', menu.open ? 'true' : 'false');
      menu.addEventListener('toggle', sync);
      sync();
      menu.addEventListener('keydown', event => {
        if (event.key !== 'Escape' || !menu.open) return;
        event.preventDefault();
        menu.removeAttribute('open');
        summary?.focus();
      });
    });
  };
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
    bindGraphTheme(root);
    indexHitTesting(root, graphState(root));
    if (hasFeature(root, 'LevelOfDetail')) applyLod(root);
    else {
      const configured = attr(root, 'data-cfx-graph-renderer');
      const renderer = configured === 'webgl' && webGlAvailable(root) ? 'webgl' : configured === 'canvas' || configured === 'webgl' ? 'canvas' : 'svg';
      root.classList.toggle('cfx-graph-render-canvas', renderer === 'canvas');
      root.classList.toggle('cfx-graph-render-webgl', renderer === 'webgl');
      root.classList.toggle('cfx-graph-render-svg', renderer === 'svg');
      root.dataset.cfxGraphRendererActive = renderer; syncRendererAccessibility(root, renderer);
      syncGraphItemTabStops(root);
    }
    applySemanticZoom(root, viewport(root).scale);
    performanceGate(root);
    if (hasFeature(root, 'Clustering')) {
      if (attr(root, 'data-cfx-graph-cluster-collapse-on-load') === 'true' || (hasFeature(root, 'LevelOfDetail') && attr(root, 'data-cfx-lod-collapse-clusters') === 'true')) applyClusterState(root, true, undefined, { reheat: false });
      else applyClusterState(root, undefined, undefined, { reheat: false });
    } else {
      items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => {
        cluster.classList.add('cfx-graph-cluster-expanded');
        cluster.setAttribute('data-cluster-collapsed', 'false');
      });
      items(root, '.cfx-graph-cluster-collapsed-member').forEach(item => item.classList.remove('cfx-graph-cluster-collapsed-member'));
      root.dataset.cfxGraphClusters = 'disabled';
      applyFilters(root);
    }
    syncClusterControls(root);
    syncFocusControls(root);
    if (hasFeature(root, 'HierarchyNavigation')) applyHierarchyView(root, attr(root, 'data-cfx-graph-hierarchy-root'), num(root, 'data-cfx-graph-hierarchy-depth', 2), { fit: false, restartPhysics: false });
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
    bindWebGlHitTesting(root);
    bindAcceleratedSvgKeyboard(root);
    bindPointerInteractions(root);
    bindGraphBoxSelection(root);
    bindOverview(root);
    bindHierarchyInteractions(root);
    bindPhysicsConfigurator(root);
    bindGraphManipulation(root);
    bindCommandMenus(root);
    items(root, '[data-cfx-role="graph-node"],[data-cfx-role="graph-edge"],[data-cfx-role="graph-cluster"]').forEach(item => bindGraphItemSelection(root, item));
    bindGraphSearch(root);
    items(root, '[data-cfx-graph-filter]').forEach(filter => filter.addEventListener('change', () => applyFilters(root)));
    items(root, '[data-cfx-graph-action]').forEach(button => {
      button.addEventListener('click', () => {
        const action = attr(button, 'data-cfx-graph-action');
        if (action === 'clusters') applyClusterState(root, root.dataset.cfxGraphClusters !== 'collapsed');
        if (action === 'hierarchy-home') applyHierarchyView(root, '', Number(root.dataset.cfxGraphHierarchyDepth || num(root, 'data-cfx-graph-hierarchy-depth', 2)));
        if (action === 'hierarchy-up') navigateHierarchyUp(root);
        if (action === 'focus') toggleNeighborhoodFocus(root);
        if (action === 'clear-selection') clearSelection(root);
        if (action === 'box-select') setGraphBoxSelectionMode(root, root.dataset.cfxGraphPointerMode !== 'box-select');
        if (action === 'edit') {
          const editor = graphEditor(root), open = editor?.hasAttribute('hidden');
          if (editor) { if (open) editor.removeAttribute('hidden'); else editor.setAttribute('hidden', ''); }
          button.setAttribute('aria-pressed', open ? 'true' : 'false');
        }
        if (action === 'undo') traverseGraphHistory(root, 'undo');
        if (action === 'redo') traverseGraphHistory(root, 'redo');
        if (action === 'save-positions') {
          const state = persistGraphInteractionState(root, 'positions');
          emit(root, 'cfxgraphpositions', { graphId: attr(root, 'data-cfx-graph-id'), positions: state.positions });
        }
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
        if (action.startsWith('export-')) button.closest('details')?.removeAttribute('open');
        if (action === 'physics') {
          const running = root.dataset.cfxGraphPhysicsState === 'running';
          if (running) pausePhysics(root);
          if (!running) startPhysics(root);
          syncPhysicsControls(root);
        }
        if (action === 'stabilize' && hasFeature(root, 'Stabilization') && startPhysics(root)) syncPhysicsControls(root);
        if (action === 'physics') syncPhysicsControls(root);
        emit(root, 'cfxgraphaction', { graphId: attr(root, 'data-cfx-graph-id'), action, physics: attr(root, 'data-cfx-graph-physics') });
      });
    });
    updateSelectionState(root);
    const restoredInteractionState = initializeGraphInteractionState(root);
    bindGraphStatePersistence(root);
    if (!restoredInteractionState && hasFeature(root, 'RuntimePhysics') && hasFeature(root, 'Stabilization') && attr(root, 'data-cfx-physics-stabilization-enabled') !== 'false') startPhysics(root, { reason: 'initial-stabilization' });
    syncPhysicsControls(root);
    syncPhysicsConfigurator(root);
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

