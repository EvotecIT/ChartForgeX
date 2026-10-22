  const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
  const getState = (root) => ({
    zoom: Number(root.dataset.cfxZoom || '1'),
    panX: Number(root.dataset.cfxPanX || '0'),
    panY: Number(root.dataset.cfxPanY || '0')
  });
  const applyViewport = (root, state) => {
    state.zoom = clamp(state.zoom, 0.5, 6);
    state.panX = clamp(state.panX, -4000, 4000);
    state.panY = clamp(state.panY, -4000, 4000);
    root.dataset.cfxZoom = state.zoom.toFixed(3);
    root.dataset.cfxPanX = state.panX.toFixed(1);
    root.dataset.cfxPanY = state.panY.toFixed(1);
    syncResetControl(root);
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    hideCrosshair(root, root.querySelector('.cfx-crosshair'));
    stage.style.setProperty('--cfx-zoom', state.zoom);
    stage.style.setProperty('--cfx-pan-x', state.panX + 'px');
    stage.style.setProperty('--cfx-pan-y', state.panY + 'px');
    refreshTooltipPosition(root);
  };

  const viewChanged = (root) => {
    if (hierarchyConnected(root) && hierarchyViewChanged(root)) return true;
    if (connectedXViewport(root) && nativeViewportChanged(root)) return true;
    const state = getState(root);
    if (Math.abs(state.zoom - 1) > 0.0005 || Math.abs(state.panX) > 0.05 || Math.abs(state.panY) > 0.05) return true;
    if (root.dataset.cfxBrush || root.dataset.cfxIsolatedSeries) return true;
    return root.querySelector('.cfx-selected,.cfx-series-muted,[data-cfx-muted="true"]') !== null;
  };
  const syncResetControl = (root) => {
    const reset = root.querySelector('[data-cfx-reset]');
    if (!reset) return;
    const changed = viewChanged(root);
    if (!changed && document.activeElement === reset) {

      if (!root.hasAttribute('tabindex')) root.setAttribute('tabindex', '-1');
      try { root.focus({ preventScroll: true }); } catch { root.focus(); }
    }
    reset.hidden = !changed;
  };
  const sameGroup = (root, peer) => root !== peer && root.dataset.cfxInteractionGroup && root.dataset.cfxInteractionGroup === peer.dataset.cfxInteractionGroup;
  const emitHostEvent = (root, name, detail) => {
    root.dispatchEvent(new CustomEvent(name, { bubbles: true, composed: true, detail: Object.assign({ chartId: root.dataset.cfxChartId || '' }, detail || {}) }));
  };
  const emitSync = (root, payload) => {
    if (!hasFeature(root, 'SynchronizedCharts') || !root.dataset.cfxInteractionGroup) return;

    const sharedTarget = target => target && target.seriesScope !== 'local';
    if (payload.target?.seriesScope === 'local') {
      if (['hover', 'crosshair', 'navigate'].includes(payload.action) && root._cfxSharedHover) emitSync(root, { action: 'hover-clear' });
      return;
    }
    payload = { ...payload };
    if (Array.isArray(payload.previousTargets)) payload.previousTargets = payload.previousTargets.filter(sharedTarget);
    for (const field of ['targets', 'trail']) if (Array.isArray(payload[field])) {
      const shared = payload[field].filter(sharedTarget);
      const replacedSelection = field === 'targets' && (payload.action === 'compare' || payload.action === 'lasso' && Array.isArray(payload.previousTargets));
      if (payload[field].length && !shared.length && !replacedSelection) return;
      payload[field] = shared;
    }
    if (['compare', 'lasso'].includes(payload.action) && Array.isArray(payload.targets)) {

      if (Array.isArray(payload.previousTargets) && !payload.previousTargets.length && !payload.targets.length) return;
      payload.count = payload.targets.length;
    }
    if (payload.action === 'state' && payload.state) {
      const targets = payload.state.selectedTargets || [], shared = targets.filter(sharedTarget);
      if (targets.length && !shared.length) return;
      payload.state = { ...payload.state, selectedTargets: shared };
    }
    if (payload.action === 'hover-clear') {
      if (!root._cfxSharedHover) return;
      delete root._cfxSharedHover;
    } else if (['hover', 'crosshair', 'navigate'].includes(payload.action)) root._cfxSharedHover = true;
    const detail = Object.assign({ chartId: root.dataset.cfxChartId || '', group: root.dataset.cfxInteractionGroup }, payload);
    root.dispatchEvent(new CustomEvent('cfxsync', { bubbles: true, composed: true, detail }));
    document.querySelectorAll('.cfx-interactive-chart').forEach((peer) => {
      if (chartLifetimes.get(peer)?.disposed === false && sameGroup(root, peer) && hasFeature(peer, 'SynchronizedCharts')) applySync(peer, detail);
    });
  };
  const applyMode = (root, mode) => {
    if (mode === 'pan' && !hasFeature(root, 'Pan') || mode === 'brush' && !hasFeature(root, 'Brush')) mode = '';
    root.dataset.cfxMode = mode || '';
    root.querySelectorAll('[data-cfx-mode-button]').forEach((button) => {
      button.setAttribute('aria-pressed', button.dataset.cfxModeButton === root.dataset.cfxMode ? 'true' : 'false');
    });
  };
  const setMode = (root, mode) => applyMode(root, root.dataset.cfxMode === mode ? '' : mode);
  // Cancel draft before pointerup recommits.
  const clearBrush = root => {
    const brush = root.querySelector('.cfx-brush-box');
    const changed = Boolean(root.dataset.cfxBrush || root.dataset.cfxMode === 'brush' || brush && !brush.hidden);
    root._cfxCancelBrushDrag?.();
    root.removeAttribute('data-cfx-brush');
    if (brush) {
      brush.hidden = true;
      for (const name of ['left', 'top', 'width', 'height']) brush.style.removeProperty(name);
    }
    if (root.dataset.cfxMode === 'brush') applyMode(root, '');
    syncResetControl(root);
    return changed;
  };
  const resetViewport = (root) => {
    suspendPointerReadout(root);
    applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
    clearBrush(root);
    applyMode(root, '');
    syncResetControl(root);
  };
  const storeInteractionState = (root, snapshot) => {
    try {
      root.dataset.cfxInteractionSnapshot = JSON.stringify(snapshot);
    } catch {
      root.removeAttribute('data-cfx-interaction-snapshot');
    }
  };
  const captureInteractionState = (root, source) => {
    const snapshot = {
      version: 2,
      chartId: root.dataset.cfxChartId || '',
      source: source || '',
      viewport: getState(root),
      nativeViewport: cartesianViewportConnected(root) ? nativeViewportWindows(root, true)
        : connectedXViewport(root) ? { x: nativeXWindow(root) } : null,
      nativeHierarchy: hierarchyConnected(root) ? hierarchySnapshot(root) : null,
      mode: root.dataset.cfxMode || '',
      brush: root.dataset.cfxBrush || '',
      selectedTargets: selectedTargets(root),
      compareCount: Number(root.dataset.cfxCompareCount || '0'),
      scenario: {
        id: root.dataset.cfxActiveScenario || '',
        stepIndex: root.dataset.cfxActiveScenarioStep || '',
        progress: root.dataset.cfxScenarioProgress || '',
        playback: root.dataset.cfxScenarioPlayback || ''
      }
    };
    storeInteractionState(root, snapshot);
    return snapshot;
  };
  const publishInteractionState = (root, source, sync) => {
    if (!hasFeature(root, 'StateBookmarks')) return null;
    const snapshot = captureInteractionState(root, source || 'host');
    emitHostEvent(root, 'cfxstate', { source: source || 'host', snapshot });
    if (sync !== false) emitSync(root, { action: 'state', state: snapshot });
    return snapshot;
  };
  const applyInteractionState = (root, snapshot, emit, sync, nativeAccepted = false) => {
    if (!snapshot || !hasFeature(root, 'StateBookmarks') || snapshot.xWindow || snapshot.yWindow) return;
    if (snapshot.nativeHierarchy && snapshot.nativeViewport) return;
    if (!nativeAccepted && snapshot.nativeHierarchy) {
      const captured = copyHostValue(snapshot), requested = captured.nativeHierarchy, current = capturedHierarchy(root);
      if (!current || requested.kind !== current.kind || requested.seriesKey !== current.seriesKey) return;
      requestNativeHierarchy(root, requested.rootId === null ? null : { rootId: requested.rootId }, false,
        () => applyInteractionState(root, captured, emit, sync, true));
      return;
    }
    if (!nativeAccepted && snapshot.nativeViewport) {
      const captured = copyHostValue(snapshot);
      const accepted = () => applyInteractionState(root, captured, emit, sync, true);
      if (cartesianViewportConnected(root))
        requestNativeCartesianWindow(root, captured.nativeViewport, false, accepted);
      else if (captured.nativeViewport.x && !cartesianAxisNames.some(name => name !== 'x' && captured.nativeViewport[name]) && connectedXViewport(root))
        requestNativeXWindow(root, captured.nativeViewport.x, false, accepted);
      // Atomic replay; rejected reader state stays intact.
      return;
    }
    const previousSelection = selectedTargets(root).map(targetKey).join('\n');
    if (!nativeAccepted && snapshot.viewport) applyViewport(root, Object.assign({}, snapshot.viewport));
    if (snapshot.mode !== undefined) applyMode(root, snapshot.mode || '');
    if (snapshot.brush !== undefined) root.dataset.cfxBrush = snapshot.brush || '';
    const scenario = snapshot.scenario || {};
    if (scenario.id !== undefined) {
      setScenario(root, scenario.id || '', false, false);
      if (scenario.id && scenario.stepIndex !== undefined && scenario.stepIndex !== '') setScenarioStep(root, scenario.stepIndex, false, false);
      const route = scenarioRoute(root, root.dataset.cfxActiveScenario || '');
      if (scenario.playback === 'playing' && route && route.steps.length) {
        const current = Number(root.dataset.cfxActiveScenarioStep || '-1');
        const next = Number.isFinite(current) ? current + 1 : 0;
        startScenarioPlayback(root, route, next, false, false);
      } else if (scenario.playback) {
        setScenarioPlaybackState(root, scenario.playback, route, false);
      }
    }
    applySelectionSetByTargets(root, snapshot.selectedTargets || [], true);
    renderCompare(root);
    syncResetControl(root);
    storeInteractionState(root, snapshot);
    syncSelectionAppearance(root);
    if (emit !== false) {
      if (previousSelection !== selectedTargets(root).map(targetKey).join('\n')) publishSelectionChange(root, 'command');
      emitHostEvent(root, 'cfxstateapplied', { snapshot });
    }
    if (sync !== false) emitSync(root, { action: 'state', state: { ...snapshot, selectedTargets: selectedTargets(root) } });
  };
  const zoomBy = (root, factor, logicalX, logicalY) => {
    if (!hasFeature(root, 'Zoom')) return;
    if (connectedXViewport(root)) {
      requestXViewportZoom(root, factor, logicalX, logicalY);
      return;
    }
    const state = getState(root);
    state.zoom *= factor;
    applyViewport(root, state);
    emitHostEvent(root, 'cfxviewport', { state: getState(root) });
    emitSync(root, { action: 'viewport', state: getState(root) });
  };
