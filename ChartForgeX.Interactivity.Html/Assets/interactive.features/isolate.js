// ChartForgeX isolate feature: double-click or double-tap a series to show only that series.
// Repeating the gesture, or using it on empty plot space, restores every series.
(() => {
  const registry = window[Symbol.for('ChartForgeX.chartFeatures')] ||= {};
  if (registry.isolate) return;
  const bind = (root, k) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage || !k.hasFeature(root, 'LegendToggles')) return;
    let lastTap = null, touchIsolated = 0;
    const isolate = event => {
      if (event.target instanceof Element && event.target.closest('[data-cfx-standalone-hierarchy],[data-cfx-role="legend-item"]')) return false;
      const series = k.seriesAt(root, event);
      // The gesture's clicks focus a mark; a pointer gesture never leaves a keyboard focus ring behind.
      if (series) { k.toggleSeriesFocus(root, series); k.clearKeyboardFocusPaint(root); return true; }
      if (!k.isolatedSeries(root)) return false;
      k.restoreSeries(root); return true;
    };
    // A double-click must not select nearby text.
    k.listenChart(root, stage, 'mousedown', event => { if (event.detail > 1) event.preventDefault(); });
    k.listenChart(root, stage, 'dblclick', event => {
      if (performance.now() - touchIsolated < 600) return;
      if (isolate(event)) event.preventDefault();
    });
    // Touch browsers do not reliably deliver dblclick, so two quick taps on one spot also count.
    k.listenChart(root, stage, 'pointerup', event => {
      if (event.pointerType !== 'touch' || root.dataset.cfxMode === 'pan' || root.dataset.cfxMode === 'brush') return;
      const now = performance.now();
      if (lastTap && now - lastTap.time < 320 && Math.hypot(event.clientX - lastTap.x, event.clientY - lastTap.y) < 24) {
        lastTap = null;
        if (isolate(event)) touchIsolated = now;
        return;
      }
      lastTap = { time: now, x: event.clientX, y: event.clientY };
    });
  };
  registry.isolate = Object.freeze({ bind });
})();
