  const chartRoot = target => typeof target === 'string' ? document.querySelector(target) : target;
  const mountChart = target => {
    const root = chartRoot(target);
    const existing = root && chartLifetimes.get(root);
    if (existing && !root.isConnected) { existing.controller.dispose(); throw new TypeError('Mount the chart after inserting its section into the document.'); }
    if (existing && !existing.disposed) return existing.controller;
    readChartSection(root);
    if (!root.isConnected) throw new TypeError('Mount the chart after inserting its section into the document.');
    const lifetime = { cleanups: new Set(), disposed: false, controller: null, preparationConnections: {},
      preparationPending: null, preparationSession: null, sceneRevision: nextChartSceneRevision() };
    lifetime.adaptive = readAdaptiveLayout(root);
    const requireMounted = () => {
      if (lifetime.disposed || !root.isConnected) { controller.dispose(); throw new Error('The chart controller is disposed; mount the connected section again.'); }
    };
    const controller = Object.freeze({
      root,
      get sceneRevision() { return lifetime.sceneRevision; },
      select: (match, options) => { requireMounted(); runSelectionCommand(root, match, options); return controller; },
      clearSelection: options => { requireMounted(); clearSelectionCommand(root, options); return controller; },
      clearBrush: options => { requireMounted(); clearBrushCommand(root, options); return controller; },
      getSelection: () => { requireMounted(); return copyHostValue(selectedTargets(root)); },
      resolveTargets: match => { requireMounted(); return resolveHostTargets(root, match); },
      highlight: (match, options) => { requireMounted(); runHighlightCommand(root, match, options); return controller; },
      clearHighlight: options => { requireMounted(); clearHighlightCommand(root, options); return controller; },
      unpinTooltip: () => { requireMounted(); unpinTooltip(root); return controller; },
      update: (section, options = {}) => {
        requireMounted();
        const motion = captureChartUpdateMotion(options.motion);
        const nativeState = lifetime.preparationSession
          ? { session: lifetime.preparationSession, basis: nativePreparationBasis(options.basis) } : null;
        const next = readChartSection(section, true).cloneNode(true);
        resetAnnotationEditor(root);
        replaceChartSection(root, lifetime, next, '', '', false, nativeState, motion);
        emitHostEvent(root, 'cfxupdated', {});
        return controller;
      },
      connectNative: (connection, options = {}) => {
        requireMounted();
        connectNativePreparation(root, connection, options);
        return controller;
      },
      connectAnnotations: connection => {
        requireMounted();
        setNativePreparationConnection(root, 'annotation', connection);
        return controller;
      },
      applyAnnotationChange: change => {
        requireMounted();
        if (!requestAnnotationChange(root, change)) throw new TypeError('The native annotation request cannot start on this scene.');
        return controller;
      },
      editAnnotations: options => { requireMounted(); return createAnnotationEditor(root, requireMounted, options); },
      connectXViewport: connection => {
        requireMounted();
        if (connection !== null && typeof connection !== 'function') throw new TypeError('Expected an X viewport callback or null to disconnect.');
        setNativePreparationConnection(root, 'viewport', connection, { axes: Object.freeze(['x']), legacy: true });
        if (connection) applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
        syncXViewportControls(root);
        return controller;
      },
      connectViewport: (connection, options = {}) => {
        requireMounted();
        const axes = nativeViewportAxes(options.axes, ['x', 'y']);
        if (connection !== null && axes.some(name => !chartAxisProjection(root, name)))
          throw new TypeError('This chart has no native projection for every requested physical axis.');
        setNativePreparationConnection(root, 'viewport', connection, { axes, legacy: false });
        if (connection) applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
        syncXViewportControls(root); return controller;
      },
      getHierarchyView: () => { requireMounted(); return copyHostValue(hierarchySnapshot(root)); },
      setHierarchyView: view => {
        requireMounted();
        if (!requestNativeHierarchy(root, view)) throw new TypeError('A hierarchy view requires a connected supported hierarchy and an existing source root, or null to reset.');
        return controller;
      },
      getViewport: () => { requireMounted(); return copyHostValue(nativeViewportWindows(root)); },
      setViewport: viewport => {
        requireMounted();
        if (!requestNativeCartesianWindow(root, viewport)) throw new TypeError('A native viewport requires connected supported axes and finite increasing source windows.');
        return controller;
      },
      getYWindow: () => { requireMounted(); return copyHostValue(nativeYWindow(root)); },
      setYWindow: (minimum, maximum) => {
        requireMounted();
        if (!requestNativeCartesianWindow(root, { y: { minimum, maximum } })) throw new TypeError('A native Y window requires a connected primary vertical axis and finite increasing source bounds.');
        return controller;
      },
      getXWindow: () => { requireMounted(); return copyHostValue(nativeXWindow(root)); },
      setXWindow: (minimum, maximum) => {
        requireMounted();
        if (!requestNativeXWindow(root, { minimum, maximum }))
          throw new TypeError('A native X window requires a connected supported chart and finite increasing source bounds.');
        return controller;
      },
      resize: () => {
        requireMounted(); settleChartUpdateMotion(root); hideCrosshair(root, root.querySelector('.cfx-crosshair'));
        refreshKeyboardNavigation(root); refreshTooltipPosition(root); syncResetControl(root);
        return controller;
      },
      dispose: () => { if (!lifetime.disposed) releaseChartLifetime(root); }
    });
    lifetime.controller = controller; chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    try { prepareAdaptiveRoot(root, lifetime); bindChart(root); bindAdaptiveLayout(root); watchChartLifetimes(); } catch (error) { releaseChartLifetime(root); throw error; }
    return controller;
  };
  // Fragments share one runtime/controller.
  window.ChartForgeXCharts = Object.freeze({ mount: mountChart,
    get: target => { const root = chartRoot(target), lifetime = root?.isConnected && chartLifetimes.get(root); return lifetime && !lifetime.disposed ? lifetime.controller : null; } });
  document.querySelectorAll('.cfx-interactive-chart').forEach(root => mountChart(root));
})();
