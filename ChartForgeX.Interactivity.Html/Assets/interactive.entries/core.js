  // Saved pages have no native preparation connection or mutable host controller.
  const emitSync = () => {};
  const syncResetControl = () => {};
  // Core has no native connection; legend hover must not link host visibility transport.
  const legendEntryShown = (root, node) => node.dataset.cfxMuted !== 'true'
    && (!root.dataset.cfxIsolatedSeries || node.dataset.cfxIsolated === 'true');
  const readerRuntimeKey = Symbol.for('ChartForgeX.chartRuntime');
  const mountCoreChart = root => {
    if (root.dataset.cfxRuntimeBound === 'true') return;
    const tip = root.querySelector('.cfx-tooltip');
    if (!tip) return;
    const lifetime = { cleanups: new Set(), disposed: false, controller: null };
    lifetime.controller = { dispose: () => {
      if (lifetime.disposed) return;
      cancelTooltipRequest(root);
      hideTip(root, tip, true);
      clearHover(root, false, false);
      root.querySelectorAll('[data-cfx-highlight-paint]').forEach(paint => paint.replaceWith(...paint.childNodes));
      root.querySelectorAll('[data-cfx-highlight-managed]').forEach(node => {
        node.removeAttribute('data-cfx-highlight-managed'); node.removeAttribute('data-cfx-highlight-unavailable');
      });
      delete root._cfxBranchHighlights;
      lifetime.disposed = true;
      for (const cleanup of Array.from(lifetime.cleanups)) cleanup();
      mountedCharts.delete(root); chartLifetimes.delete(root); watchChartLifetimes();
      if (root[readerRuntimeKey]?.dispose === lifetime.controller.dispose) delete root[readerRuntimeKey];
      root.removeAttribute('data-cfx-runtime-bound');
    } };
    chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    root[readerRuntimeKey] = { kind: 'core', dispose: lifetime.controller.dispose };
    root.dataset.cfxRuntimeBound = 'true';
    applyRenderedTargetContract(root);
    prepareRootBranchHighlights(root);
    const hover = (node, event) => {
      if (!pointerTargetPaint(node)) return;
      setHover(root, node, true, false);
      showTip(root, tip, node, event);
    };
    interactiveTargets(root).forEach(node => {
      [node, ...pointLabelSurfaces(root, node)].forEach(surface => {
        listenChart(root, surface, 'pointerenter', event => hover(node, event));
        listenChart(root, surface, 'pointermove', event => { if (ownsTooltipPointer(root, event)) moveTip(tip, event, node); });
        listenChart(root, surface, 'pointerleave', event => {
          if (!ownsTooltipPointer(root, event)) return;
          clearHover(root, true, false);
          if (!retainPointerTip(root, event)) hideTip(root, tip, false);
        });
      });
      const focus = targetFocusNode(node);
      listenChart(root, focus, 'focusin', () => hover(node));
      listenChart(root, focus, 'focusout', () => { clearHover(root, true, false); hideTip(root, tip, true); });
    });
    const stage = root.querySelector('.cfx-stage');
    if (stage) {
      listenChart(root, stage, 'pointermove', event => updateNearestPoint(root, null, tip, event));
      listenChart(root, stage, 'pointerleave', () => { clearHover(root, true, false); hideTip(root, tip, false); });
    }
    listenChart(root, root, 'keydown', event => {
      if (event.key === 'Escape') { clearHover(root, true, false); hideTip(root, tip, true); }
    });
    watchChartLifetimes();
  };
  document.querySelectorAll('.cfx-interactive-chart').forEach(mountCoreChart);
