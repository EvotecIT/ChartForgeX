  // A saved reader has no native callback. Keep these connection boundaries closed
  // so the linker can omit host preparation, reply validation and transition code.
  const nativePreparationBinding = () => null;
  const nativePreparationBackpressureBusy = () => false;
  const nativeLegendConnected = () => false;
  const nativeSeriesVisibilityChanged = () => false;
  const hierarchyConnected = () => false;
  const polarViewportConnected = () => false;
  const cancelNativePreparation = () => {};
  const requestNativePreparation = () => false;
  const readerRuntimeKey = Symbol.for('ChartForgeX.chartRuntime');
  const mountStandaloneChart = root => {
    if (root.dataset.cfxRuntimeBound === 'true') {
      if (root[readerRuntimeKey]?.kind !== 'core') return;
      root[readerRuntimeKey].dispose();
    }
    readChartSection(root);
    const lifetime = { cleanups: new Set(), disposed: false, preparationConnections: {},
      preparationPending: null, preparationSession: null, sceneRevision: nextChartSceneRevision() };
    lifetime.controller = { dispose: () => releaseChartLifetime(root) };
    lifetime.adaptive = null;
    chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    const ownership = { kind: 'standalone', dispose: lifetime.controller.dispose };
    root[readerRuntimeKey] = ownership;
    ownChartWork(root, () => { if (root[readerRuntimeKey] === ownership) delete root[readerRuntimeKey]; });
    try {
      bindChart(root);
      window[Symbol.for('ChartForgeX.nativeTextExport')].mount(root, release => ownChartWork(root, release));
      watchChartLifetimes();
    } catch (error) { releaseChartLifetime(root); throw error; }
  };
  document.querySelectorAll('.cfx-interactive-chart').forEach(mountStandaloneChart);
