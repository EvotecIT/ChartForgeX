  const chartRoot = target => typeof target === 'string' ? document.querySelector(target) : target;
  const mountChart = target => {
    const root = chartRoot(target);
    const existing = root && chartLifetimes.get(root);
    if (existing && !root.isConnected) { existing.controller.dispose(); throw new TypeError('Mount the chart after inserting its section into the document.'); }
    if (existing && !existing.disposed) return existing.controller;
    readChartSection(root);
    if (!root.isConnected) throw new TypeError('Mount the chart after inserting its section into the document.');
    const lifetime = { cleanups: new Set(), disposed: false, controller: null };
    const requireMounted = () => {
      if (lifetime.disposed || !root.isConnected) { controller.dispose(); throw new Error('The chart controller is disposed; mount the connected section again.'); }
    };
    const controller = Object.freeze({
      root,
      update: section => {
        requireMounted();
        const next = readChartSection(section, true).cloneNode(true);
        const state = chartUpdateState(root);
        releaseChartLifetime(root);
        installChartSection(root, next);
        // Keep this controller and the host root stable while replacing all owned browser work.
        lifetime.disposed = false; chartLifetimes.set(root, lifetime); mountedCharts.add(root);
        bindChart(root); watchChartLifetimes(); restoreChartUpdateState(root, state);
        emitHostEvent(root, 'cfxupdated', {});
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
    try { bindChart(root); watchChartLifetimes(); } catch (error) { releaseChartLifetime(root); throw error; }
    return controller;
  };
  // Shared assets may be registered by more than one fragment. One installed runtime owns all controllers.
  window.ChartForgeXCharts = Object.freeze({ mount: mountChart });
  document.querySelectorAll('.cfx-interactive-chart').forEach(root => mountChart(root));
})();
