  // Optional behaviour ships in separate feature bundles that register themselves here.
  // Inline pages embed only the bundles their options declare. Pages with shared assets
  // name each bundle's file on the runtime script, and the runtime fetches it when the
  // browser is idle after the first paint or when a reader first needs it.
  const chartFeatureRegistry = () => window[Symbol.for('ChartForgeX.chartFeatures')] ||= {};
  const chartFeature = name => chartFeatureRegistry()[name] || null;
  const chartFeatureSourceScript = document.currentScript;
  const chartFeatureLoads = new Map();
  const chartFeatureSources = () => {
    try { return JSON.parse(chartFeatureSourceScript?.dataset.cfxFeatures || '{}') || {}; } catch (_) { return {}; }
  };
  const chartFeatureSource = name => chartFeatureSources()[name]?.src ? chartFeatureSources()[name] : null;
  const loadChartFeature = name => {
    const ready = chartFeature(name);
    if (ready) return Promise.resolve(ready);
    const source = chartFeatureSource(name);
    if (!source) return Promise.resolve(null);
    if (!chartFeatureLoads.has(name)) chartFeatureLoads.set(name, new Promise(resolve => {
      const script = document.createElement('script');
      script.src = source.src; script.async = true; script.dataset.cfxFeature = name;
      if (source.integrity) { script.integrity = source.integrity; script.crossOrigin = 'anonymous'; }
      if (chartFeatureSourceScript?.nonce) script.nonce = chartFeatureSourceScript.nonce;
      script.onload = () => resolve(chartFeature(name));
      script.onerror = () => { chartFeatureLoads.delete(name); resolve(null); };
      document.head.appendChild(script);
    }));
    return chartFeatureLoads.get(name);
  };
  const prefetchChartFeatures = () => {
    const names = Object.keys(chartFeatureSources()).filter(name => !chartFeature(name));
    if (!names.length) return;
    const load = () => names.forEach(loadChartFeature);
    if (typeof requestIdleCallback === 'function') requestIdleCallback(load, { timeout: 2000 }); else setTimeout(load, 200);
  };
  const motionDuration = root => {
    const duration = Number(root.dataset.cfxMotionDuration ?? 300);
    return Number.isFinite(duration) && duration > 0 && !window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
      && !document.hidden ? duration : 0;
  };
  const captureContextMenuItems = items => {
    if (!Array.isArray(items) || items.some(item => !item || typeof item.id !== 'string' || !item.id.trim()
      || typeof item.label !== 'string' || !item.label.trim() || typeof item.action !== 'function')
      || new Set(items.map(item => item.id)).size !== items.length) throw new TypeError('Context menu: unique id, label and action callback required.');
    return items.map(item => Object.freeze({ id: item.id, label: item.label, action: item.action, disabled: item.disabled === true }));
  };
  // The narrow runtime surface a feature bundle may use. Bundles never reach into chart state directly.
  const chartFeatureKernel = Object.freeze({
    hasFeature, emitHostEvent, downloadBlob, selectedTargets, uiText, listenChart, targetIdentity, seriesLabel,
    tooltipHierarchy, renderedTargetKind, isPassiveTarget, motionDuration, seriesKey, ownChartWork, clearHover, hideTip, hideCrosshair, setHover, showTip,
    toggleSelection, pinTip, refreshKeyboardNavigation, syncResetControl, clearKeyboardFocusPaint,
    hierarchyConnected: root => !!nativePreparationBinding(root, 'hierarchy'),
    contextMenuItems: root => chartLifetimes.get(root)?.contextMenuItems || [],
    annotationsDisconnected: root => !!annotationEditorState(root)?.enabled && !nativePreparationBinding(root, 'annotation'),
    rememberMenuFocus: (root, node) => { if (node) root._cfxContextMenuSourceFocus = node; else delete root._cfxContextMenuSourceFocus; },
    seriesAt: (root, event) => {
      const nearest = acquiredTooltipPoint(root, pointerCandidates(root, event, true));
      return nearest && isDataTarget(nearest.node) ? nearest.node.closest('[data-cfx-role="series"]') || nearest.node : null;
    },
    toggleSeriesFocus: (root, node) => toggleSeriesFocus(root, node, true, true),
    isolatedSeries: root => root.dataset.cfxIsolatedSeries || '',
    restoreSeries: root => {
      const node = root.querySelector('[data-cfx-isolated="true"]') || root.querySelector('[data-cfx-role="series"].cfx-series-isolated-in');
      if (node) toggleSeriesFocus(root, node, true, true); else setSeriesIsolation(root, null, false);
    }
  });
  const exportChartData = (root, format, copy) => loadChartFeature('data')
    .then(feature => feature ? feature.exportData(root, chartFeatureKernel, format, copy === true) : false);
  // Runs once per binding; a lazily fetched bundle binds when it arrives if the chart is still mounted.
  const bindChartFeature = (root, name) => {
    const lifetime = chartLifetimes.get(root), bind = feature => feature?.bind(root, chartFeatureKernel);
    const ready = chartFeature(name);
    if (ready) { bind(ready); return; }
    loadChartFeature(name).then(feature => { if (chartLifetimes.get(root) === lifetime && lifetime && !lifetime.disposed && root.isConnected) bind(feature); });
  };
  const bindChartFeatures = root => {
    if (hasFeature(root, 'LegendToggles') && !nativeLegendConnected(root)) bindChartFeature(root, 'isolate');
  };
  // The entry animation only runs when its bundle is already present: a late bundle never replays a painted chart.
  const enterChart = root => chartFeature('motion')?.enter(root, chartFeatureKernel);
