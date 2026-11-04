  // Saved pages have no native preparation connection or mutable host controller.
  const emitSync = () => {};
  const syncResetControl = () => {};
  // Core has no native connection; legend hover must not link host visibility transport.
  const legendEntryShown = (root, node) => node.dataset.cfxMuted !== 'true'
    && (!root.dataset.cfxIsolatedSeries || node.dataset.cfxIsolated === 'true');
  // Hierarchy drilling selects the standalone reader, so core keyboard stops never meet a captured hierarchy.
  const capturedHierarchy = () => null;
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
    const crosshair = root.querySelector('.cfx-crosshair');
    const show = (node, event) => { setHover(root, node, true, false); showTip(root, tip, node, event); };
    const clear = (pointer) => { clearHover(root, true, false); hideCrosshair(root, crosshair); hideTip(root, tip, !pointer); };
    // Saved readers rove like the full runtime: one Tab stop for the plot and one for the legend.
    // The tooltip is the polite status region, so moving focus announces the same readout.
    const keyboard = hasFeature(root, 'KeyboardNavigation') && { owned: new Set() };
    const stops = focused => assignKeyboardStops(root, keyboard, keyboardTargets(root), focused);
    interactiveTargets(root).forEach(node => {
      // Every target leaves the Tab order except the active stop of each component.
      if (keyboard) keyboard.owned.add(targetFocusNode(node));
      [node, ...pointLabelSurfaces(root, node)].forEach(surface => {
        listenChart(root, surface, 'pointerenter', event => { if (pointerTargetPaint(node)) show(node, event); });
        listenChart(root, surface, 'pointermove', event => { if (ownsTooltipPointer(root, event)) moveTip(tip, event, node); });
        listenChart(root, surface, 'pointerleave', event => {
          if (!ownsTooltipPointer(root, event)) return;
          clearHover(root, true, false);
          if (!retainPointerTip(root, event)) hideTip(root, tip, false);
        });
      });
      const focus = targetFocusNode(node);
      listenChart(root, focus, 'focusin', event => {
        // A focused point's focusin also bubbles through its series target; only the focused target reads out.
        if (event.target !== focus) return;
        if (keyboard) stops(node);
        // Focus may read retained geometry that a pointer cannot hit, exactly as the full runtime does.
        if (!tooltipReadoutAvailable(node, event)) { clear(false); return; }
        show(node, event);
        if (focus.matches(':focus-visible')) showFocusCrosshair(root, crosshair, node);
      });
      listenChart(root, focus, 'focusout', () => clear(false));
      if (keyboard) listenChart(root, focus, 'keydown', event => {
        if (event.target !== focus || !keyboardNavigationKeys.includes(event.key)) return;
        event.preventDefault();
        const next = adjacentKeyboardTarget(root, stops(node), node, event.key);
        if (next && next !== node) { stops(next); targetFocusNode(next).focus(); }
      });
    });
    if (keyboard) {
      stops();
      lifetime.cleanups.add(() => keyboard.owned.forEach(node => node.removeAttribute('tabindex')));
    }
    const stage = root.querySelector('.cfx-stage');
    if (stage) {
      listenChart(root, stage, 'pointermove', event => updateNearestPoint(root, crosshair, tip, event));
      listenChart(root, stage, 'pointerleave', () => clear(true));
    }
    listenChart(root, root, 'keydown', event => {
      if (event.key === 'Escape') clear(false);
    });
    watchChartLifetimes();
  };
  document.querySelectorAll('.cfx-interactive-chart').forEach(mountCoreChart);
