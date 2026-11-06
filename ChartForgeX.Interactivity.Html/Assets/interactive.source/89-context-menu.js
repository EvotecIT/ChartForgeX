
  // The menu itself lives in the data feature bundle; the runtime only routes the gesture.
  const bindChartContextMenu = root => {
    if (root.dataset.cfxContextMenu !== 'true') return;
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    const open = (x, y, event) => {
      const feature = chartFeature('data');
      if (feature) { if (feature.openMenu(root, chartFeatureKernel, x, y)) event.preventDefault(); return; }
      if (!chartFeatureSource('data')) return;
      event.preventDefault();
      loadChartFeature('data').then(loaded => { if (loaded && root.isConnected) loaded.openMenu(root, chartFeatureKernel, x, y); });
    };
    listenChart(root, stage, 'contextmenu', event => open(event.clientX, event.clientY, event));
    listenChart(root, root, 'keydown', event => {
      if (event.key !== 'ContextMenu' && !(event.key === 'F10' && event.shiftKey)) return;
      const active = root.getRootNode().activeElement;
      if (active !== root && !stage.contains(active)) return;
      const box = active.getBoundingClientRect();
      event.stopPropagation();
      open(box.left + box.width / 2, box.top + box.height / 2, event);
    });
    ownChartWork(root, () => chartFeature('data')?.closeMenu(root));
  };
