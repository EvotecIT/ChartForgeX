  const chartRoot = target => typeof target === 'string' ? document.querySelector(target) : target;
  const mountChart = target => {
    const root = chartRoot(target);
    const existing = root && chartLifetimes.get(root);
    if (existing && !root.isConnected) { existing.controller.dispose(); throw new TypeError('Mount: connected section required.'); }
    if (existing && !existing.disposed) return existing.controller;
    readChartSection(root);
    if (!root.isConnected) throw new TypeError('Mount: connected section required.');
    const lifetime = { cleanups: new Set(), disposed: false, controller: null, preparationConnections: {},
      preparationPending: null, preparationSession: null, sceneRevision: nextChartSceneRevision() };
    lifetime.adaptive = readAdaptiveLayout(root);
    const requireMounted = () => {
      if (lifetime.disposed || !root.isConnected) { controller.dispose(); throw new Error('Controller disposed; remount section.'); }
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
        if (options.transition != null && options.sceneRevision !== lifetime.sceneRevision)
          throw new TypeError('Native transition: current source sceneRevision required.');
        const next = readChartSection(section, true).cloneNode(true), adaptive = readAdaptiveLayout(next);
        if (nativeLegendConnected(root) && (adaptive ? [adaptive.wide, adaptive.narrow] : [next]).some(drawing => !capturedNativeVisibility(drawing)))
          throw new TypeError('Update: native legend requires a retained native visibility view.');
        const captured = captureNativeValueMotion(root, next, options.transition, null, adaptive);
        resetAnnotationEditor(root);
        replaceChartSection(root, lifetime, next, '', '', false, nativeState, motion, adaptive, captured);
        emitHostEvent(root, 'cfxupdated', {});
        return controller;
      },
      presentInitial: (transition, options = {}) => {
        requireMounted();
        const revision = lifetime.sceneRevision;
        if (options.sceneRevision !== revision) throw new TypeError('Native initial presentation: current sceneRevision required.');
        const motion = captureChartUpdateMotion(options.motion), captured = captureInitialNativeTransition(root, transition);
        if (lifetime.sceneRevision !== revision || chartLifetimes.get(root) !== lifetime || lifetime.disposed)
          throw new TypeError('Native initial presentation: source changed during capture.');
        beginChartUpdateMotion(root, motion, captured); return controller;
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
        requireNativePreparationIdle(root);
        if (!requestAnnotationChange(root, change)) throw new TypeError('Annotation: request unavailable.');
        return controller;
      },
      editAnnotations: options => { requireMounted(); return createAnnotationEditor(root, requireMounted, options); },
      connectXViewport: connection => {
        requireMounted();
        if (connection !== null && typeof connection !== 'function') throw new TypeError('X viewport: callback or null required.');
        setNativePreparationConnection(root, 'viewport', connection, { axes: Object.freeze(['x']), legacy: true });
        if (connection) applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
        syncXViewportControls(root);
        return controller;
      },
      connectViewport: (connection, options = {}) => {
        requireMounted();
        const axes = nativeViewportAxes(options.axes, ['x', 'y']);
        if (connection !== null && axes.some(name => !chartAxisProjection(root, name)))
          throw new TypeError('Axis projection unavailable.');
        setNativePreparationConnection(root, 'viewport', connection, { axes, legacy: false });
        if (connection) applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
        syncXViewportControls(root); return controller;
      },
      connectPolarViewport: (connection, options = {}) => {
        requireMounted(); connectPolarViewport(root, connection, options); return controller;
      },
      getPolarViewport: () => { requireMounted(); return copyHostValue(nativePolarViewport(root)); },
      setPolarViewport: viewport => {
        requireMounted();
        if (!requestNativePolarWindow(root, viewport)) throw new TypeError('Polar viewport: connected axes/valid windows required.');
        return controller;
      },
      setProgressValue: (match, value) => {
        requireMounted(); requireNativePreparationIdle(root); requestProgressValue(root, match, value); return controller;
      },
      getHierarchyView: () => { requireMounted(); return copyHostValue(hierarchySnapshot(root)); },
      setHierarchyView: view => {
        requireMounted();
        if (view !== null) requireNativePreparationIdle(root);
        if (!requestNativeHierarchy(root, view)) throw new TypeError('Hierarchy: supported connection and exact source view or null required.');
        return controller;
      },
      getViewport: () => { requireMounted(); return copyHostValue(nativeViewportWindows(root)); },
      setViewport: viewport => {
        requireMounted();
        if (!requestNativeCartesianWindow(root, viewport)) throw new TypeError('Viewport: connected axes/finite min<max required.');
        return controller;
      },
      getYWindow: () => { requireMounted(); return copyHostValue(nativeYWindow(root)); },
      setYWindow: (minimum, maximum) => {
        requireMounted();
        if (!requestNativeCartesianWindow(root, { y: { minimum, maximum } })) throw new TypeError('Y viewport: connected primary Y/finite min<max required.');
        return controller;
      },
      getXWindow: () => { requireMounted(); return copyHostValue(nativeXWindow(root)); },
      setXWindow: (minimum, maximum) => {
        requireMounted();
        if (!requestNativeXWindow(root, { minimum, maximum }))
          throw new TypeError('X viewport: supported connection/finite min<max required.');
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
    try { rememberNativeDrawingSnapshot(root, lifetime, root); prepareAdaptiveRoot(root, lifetime); bindChart(root); window[Symbol.for('ChartForgeX.nativeTextExport')].mount(root, release => ownChartWork(root, release)); bindAdaptiveLayout(root); watchChartLifetimes(); } catch (error) { releaseChartLifetime(root); throw error; }
    return controller;
  };

  window.ChartForgeXCharts = Object.freeze({ mount: mountChart,
    get: target => { const root = chartRoot(target), lifetime = root?.isConnected && chartLifetimes.get(root); return lifetime && !lifetime.disposed ? lifetime.controller : null; } });
  document.querySelectorAll('.cfx-interactive-chart').forEach(root => mountChart(root));
})();
