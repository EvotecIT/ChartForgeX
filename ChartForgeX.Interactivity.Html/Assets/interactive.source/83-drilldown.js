  // Configured category drill-down lives in the drilldown feature bundle. The runtime only recognises a
  // category that opens a child chart and hands it over, so charts without targets pay for one attribute check.
  const drillConfig = root => {
    const source = root.dataset.cfxDrill;
    if (!source) return null;
    if (root._cfxDrillConfig?.source !== source) {
      let value = null;
      try { value = JSON.parse(source); } catch { value = null; }
      root._cfxDrillConfig = { source, value: value && Array.isArray(value.targets) ? value : null };
    }
    return root._cfxDrillConfig.value;
  };
  const drillTargetFor = (root, node) => {
    const config = node?.dataset?.cfxRole === 'point' ? drillConfig(root) : null, x = Number(node?.dataset.cfxX);
    return config && Number.isFinite(x) && config.targets.find(target => target.x === x) || null;
  };
  const activateDrillTarget = (root, node) => {
    const target = drillTargetFor(root, node);
    if (!target) return false;
    const lifetime = chartLifetimes.get(root), open = feature => feature?.open(root, chartFeatureKernel, node, target);
    const ready = chartFeature('drilldown');
    if (ready) open(ready);
    else loadChartFeature('drilldown').then(feature => { if (chartLifetimes.get(root) === lifetime && !lifetime.disposed) open(feature); });
    return true;
  };
  // The standalone reader replaces this with its own mount; the full runtime mounts a child with the host API.
  const mountDrillChild = section => mountChart(section);
  const drillFeatureKernel = {
    drillConfig, drillTargetFor,
    mountChart: section => mountDrillChild(section),
    releaseChart: root => chartLifetimes.get(root)?.controller.dispose(),
    readSection: value => readChartSection(value, true),
    // An adaptive section keeps the embedded children with its wide drawing.
    drillTemplate: (root, id) => [root, chartLifetimes.get(root)?.adaptive?.wide].map(owner => owner && Array.from(owner.children)
      .find(node => node instanceof HTMLTemplateElement && node.dataset.cfxDrillChild === id)).find(Boolean) || null,
    captureView: root => ({ selected: selectedTargets(root), state: getState(root),
      windows: standaloneViewport(root) ? nativeViewportWindows(root, true) : null }),
    restoreView: (root, view) => {
      refreshKeyboardNavigation(root); refreshTooltipPosition(root);
      if (view.windows && standaloneViewport(root)) {
        if (JSON.stringify(nativeViewportWindows(root, true)) !== JSON.stringify(view.windows)) requestNativeCartesianWindow(root, view.windows, false);
      } else applyViewport(root, Object.assign({}, view.state));
      if (selectedTargets(root).map(targetKey).join('\n') !== view.selected.map(targetKey).join('\n')) {
        applySelectionSetByTargets(root, view.selected, true); syncSelectionAppearance(root);
      }
      syncResetControl(root);
    },
    focusTarget: (root, target) => {
      const node = target && interactiveTargets(root).find(candidate => matchesTargetIdentity(candidate, target));
      if (!node) return false;
      refreshKeyboardNavigation(root, node);
      targetFocusNode(node).focus({ preventScroll: true });
      return true;
    }
  };
