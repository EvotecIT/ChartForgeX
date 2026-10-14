  // One owner releases a chart's browser work. Source data and layout remain native prepared facts.
  const chartLifetimes = new WeakMap();
  const mountedCharts = new Set();
  let chartLifetimeObserver = null;
  const watchChartLifetimes = () => {
    if (!mountedCharts.size) {
      chartLifetimeObserver?.disconnect(); chartLifetimeObserver = null;
      return;
    }
    if (!chartLifetimeObserver) chartLifetimeObserver = new MutationObserver(() => {
      for (const root of Array.from(mountedCharts)) if (!root.isConnected) chartLifetimes.get(root)?.controller.dispose();
      if (Array.from(mountedCharts).some(root => {
        const before = chartLifetimes.get(root).trees, after = tooltipRootTrees(root);
        return before.length !== after.length || before.some((tree, index) => tree !== after[index]);
      })) watchChartLifetimes();
    });
    chartLifetimeObserver.disconnect();
    const trees = new Set();
    for (const root of mountedCharts) {
      const ownedTrees = tooltipRootTrees(root); chartLifetimes.get(root).trees = ownedTrees;
      for (const tree of ownedTrees) trees.add(tree);
    }
    for (const tree of trees) chartLifetimeObserver.observe(tree, { childList: true, subtree: true });
  };
  const ownChartWork = (root, release) => {
    const lifetime = chartLifetimes.get(root);
    if (!lifetime || lifetime.disposed) { release(); return () => {}; }
    let active = true;
    const cleanup = () => {
      if (!active) return;
      active = false; lifetime.cleanups.delete(cleanup); release();
    };
    lifetime.cleanups.add(cleanup);
    return cleanup;
  };
  const listenChart = (root, target, name, listener, options) => {
    target.addEventListener(name, listener, options);
    return ownChartWork(root, () => target.removeEventListener(name, listener, options));
  };
  const clearChartBrowserState = (root) => {
    cancelTooltipRequest(root);
    hideTip(root, root.querySelector('.cfx-tooltip'), true);
    stopScenarioPlayback(root, 'paused', false);
    clearHover(root, false, false);
    hideCrosshair(root, root.querySelector('.cfx-crosshair'));
    clearFocusTrail(root); clearReveals(root);
    clearSelections(root); setSeriesIsolation(root, null, false); resetViewport(root);
    root.querySelectorAll('.cfx-series-muted').forEach(node => node.classList.remove('cfx-series-muted'));
    root.querySelectorAll('[data-cfx-muted]').forEach(node => node.removeAttribute('data-cfx-muted'));
    root.querySelectorAll('.cfx-prepared-point-marker,[data-cfx-browser-hit-area],[data-cfx-highlight-outline]').forEach(node => node.remove());
    root.querySelectorAll('[data-cfx-highlight-paint]').forEach(paint => paint.replaceWith(...paint.childNodes));
    root.querySelectorAll('[data-cfx-highlight-managed]').forEach(node => {
      node.removeAttribute('data-cfx-highlight-managed'); node.removeAttribute('data-cfx-highlight-unavailable');
    });
    root.querySelectorAll('[data-cfx-keyboard-component]').forEach(node => {
      node.removeAttribute('data-cfx-keyboard-component'); node.removeAttribute('tabindex');
    });
    root.querySelectorAll('[data-cfx-label-for],[data-cfx-point]').forEach(node => {
      delete node._cfxLabelTarget; delete node._cfxPointLabels;
    });
    for (const key of Object.keys(root)) if (key.startsWith('_cfx')) delete root[key];
    root.removeAttribute('data-cfx-runtime-bound');
  };
  const releaseChartLifetime = (root) => {
    const lifetime = chartLifetimes.get(root);
    if (!lifetime || lifetime.disposed) return;
    lifetime.disposed = true;
    for (const cleanup of Array.from(lifetime.cleanups)) cleanup();
    clearChartBrowserState(root);
    mountedCharts.delete(root); chartLifetimes.delete(root); watchChartLifetimes();
  };
