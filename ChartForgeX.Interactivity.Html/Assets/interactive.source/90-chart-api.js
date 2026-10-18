  const chartRoot = target => typeof target === 'string' ? document.querySelector(target) : target;
  const mountChart = target => {
    const root = chartRoot(target);
    const existing = root && chartLifetimes.get(root);
    if (existing && !root.isConnected) { existing.controller.dispose(); throw new TypeError('Mount the chart after inserting its section into the document.'); }
    if (existing && !existing.disposed) return existing.controller;
    readChartSection(root);
    if (!root.isConnected) throw new TypeError('Mount the chart after inserting its section into the document.');
    const lifetime = { cleanups: new Set(), disposed: false, controller: null, xViewportConnection: null,
      xViewportPending: null, sceneRevision: nextChartSceneRevision() };
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
      highlight: (match, options) => { requireMounted(); runHighlightCommand(root, match, options); return controller; },
      clearHighlight: options => { requireMounted(); clearHighlightCommand(root, options); return controller; },
      unpinTooltip: () => { requireMounted(); unpinTooltip(root); return controller; },
      update: section => {
        requireMounted();
        const next = readChartSection(section, true).cloneNode(true);
        replaceChartSection(root, lifetime, next);
        emitHostEvent(root, 'cfxupdated', {});
        return controller;
      },
      connectXViewport: connection => {
        requireMounted();
        if (connection !== null && typeof connection !== 'function') throw new TypeError('Expected an X viewport callback or null to disconnect.');
        cancelXViewportRequest(lifetime);
        lifetime.xViewportConnection = connection;
        if (connection) applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
        syncXViewportControls(root);
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
        requireMounted(); hideCrosshair(root, root.querySelector('.cfx-crosshair'));
        refreshKeyboardNavigation(root); refreshTooltipPosition(root); syncResetControl(root);
        return controller;
      },
      dispose: () => { if (!lifetime.disposed) releaseChartLifetime(root); }
    });
    lifetime.controller = controller; chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    try { prepareAdaptiveRoot(root, lifetime); bindChart(root); bindAdaptiveLayout(root); watchChartLifetimes(); } catch (error) { releaseChartLifetime(root); throw error; }
    return controller;
  };
  // Shared assets may be registered by more than one fragment. One installed runtime owns all controllers.
  window.ChartForgeXCharts = Object.freeze({ mount: mountChart,
    get: target => { const root = chartRoot(target), lifetime = root?.isConnected && chartLifetimes.get(root); return lifetime && !lifetime.disposed ? lifetime.controller : null; } });
  document.querySelectorAll('.cfx-interactive-chart').forEach(root => mountChart(root));
})();
