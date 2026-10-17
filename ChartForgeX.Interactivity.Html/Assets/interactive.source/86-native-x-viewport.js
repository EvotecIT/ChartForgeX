  // Connected hosts return a C#-prepared scene. JavaScript owns gesture geometry and reply lifetime only.
  let chartSceneSequence = 0, chartViewportRequestSequence = 0;
  const nextChartSceneRevision = () => 'cfx-scene-' + (++chartSceneSequence);
  const validXProjection = projection => {
    const box = projection?.plotBounds;
    return !!projection && [projection.fullMinimum, projection.fullMaximum, projection.minimum, projection.maximum].every(Number.isFinite)
      && projection.fullMinimum < projection.fullMaximum && projection.minimum < projection.maximum
      && projection.minimum >= projection.fullMinimum && projection.maximum <= projection.fullMaximum
      && typeof projection.reversed === 'boolean' && !!box && [box.x, box.y, box.width, box.height].every(Number.isFinite)
      && box.width > 0 && box.height > 0 && Number.isFinite(box.x + box.width) && Number.isFinite(box.y + box.height);
  };
  const chartXProjection = root => {
    try { return JSON.parse(root.dataset.cfxPreparedChart).xProjection || null; } catch { return null; }
  };
  const nativeXWindowChanged = root => {
    const projection = chartXProjection(root);
    return !!projection && (projection.minimum !== projection.fullMinimum || projection.maximum !== projection.fullMaximum);
  };
  const connectedXViewport = root => !!chartLifetimes.get(root)?.xViewportConnection;
  const syncXViewportControls = root => {
    const unsupported = connectedXViewport(root) && !chartXProjection(root);
    root.querySelectorAll('[data-cfx-zoom],[data-cfx-mode-button="pan"]').forEach(button => { button.disabled = unsupported; });
    if (unsupported && root.dataset.cfxMode === 'pan') applyMode(root, '');
    syncResetControl(root);
  };
  const cancelXViewportRequest = lifetime => {
    const pending = lifetime?.xViewportPending;
    if (!pending) return;
    lifetime.xViewportPending = null;
    pending.abort.abort();
  };
  const logicalXPointer = (root, event) => {
    const matrix = root.querySelector('.cfx-stage svg')?.getScreenCTM();
    if (!matrix) return null;
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    return Number.isFinite(point.x) ? point.x : null;
  };
  const replaceChartSection = (root, lifetime, next, nativeKind = '', adaptiveDrawing = '') => {
    // Validate every alternate before releasing the working scene, preserving atomic host updates.
    const adaptive = adaptiveDrawing ? lifetime.adaptive : readAdaptiveLayout(next);
    const state = chartUpdateState(root, !!adaptiveDrawing);
    const suspended = nativeKind === 'reset' || !!adaptiveDrawing && pointerReadoutSuspended(root);
    // Keep the request current through validation so a rejected alternate reports its failure.
    if (nativeKind) lifetime.xViewportPending = null;
    if (nativeKind === 'reset') { state.selected = []; state.muted = []; state.isolated = []; state.focused = null; }
    releaseChartLifetime(root);
    installChartSection(root, next);
    // The application owns the root and connection; each installed scene owns fresh browser work.
    lifetime.disposed = false; lifetime.sceneRevision = nextChartSceneRevision();
    lifetime.adaptive = adaptive;
    if (adaptiveDrawing) adaptive.current = adaptiveDrawing;
    chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    // Same-source redraws retain explicit reset dismissal before any fresh scene can acquire input.
    if (suspended) suspendPointerReadout(root);
    prepareAdaptiveRoot(root, lifetime);
    bindChart(root); watchChartLifetimes(); restoreChartUpdateState(root, state, !!nativeKind);
    bindAdaptiveLayout(root);
    syncXViewportControls(root);
  };
  const requestXViewport = (root, kind, left, right) => {
    const lifetime = chartLifetimes.get(root), connection = lifetime?.xViewportConnection;
    if (!connection || lifetime.disposed || !root.isConnected || !chartXProjection(root)) return;
    if (kind !== 'reset' && (!Number.isFinite(left) || !Number.isFinite(right) || left >= right)) return;
    if (kind === 'reset') suspendPointerReadout(root);
    cancelXViewportRequest(lifetime);
    const request = Object.freeze({ chartId: root.dataset.cfxChartId, requestId: 'cfx-request-' + (++chartViewportRequestSequence),
      sceneRevision: lifetime.sceneRevision, kind, ...(lifetime.adaptive ? { drawing: lifetime.adaptive.current } : {}),
      ...(kind === 'reset' ? {} : { left, right }) });
    const pending = { request, abort: new AbortController() }; lifetime.xViewportPending = pending;
    const current = () => root.isConnected && chartLifetimes.get(root) === lifetime && !lifetime.disposed
      && lifetime.xViewportConnection === connection && lifetime.xViewportPending === pending
      && lifetime.sceneRevision === request.sceneRevision && !pending.abort.signal.aborted;
    emitHostEvent(root, 'cfxviewportrequest', request);
    Promise.resolve().then(() => {
      if (!current()) return;
      return connection(request, pending.abort.signal);
    }).then(html => {
      if (!current()) return;
      if (typeof html !== 'string') throw new TypeError('The viewport connection must return a fresh generated section HTML string.');
      const next = readChartSection(html, true).cloneNode(true);
      if (next.dataset.cfxChartId !== request.chartId || !chartXProjection(next))
        throw new TypeError('The viewport reply must retain the chart identity and a supported X projection.');
      const previousSelection = kind === 'reset' ? selectionSignature(root) : '';
      replaceChartSection(root, lifetime, next, kind);
      if (kind === 'reset') {
        emitHostEvent(root, 'cfxreset', {});
        publishResetSelection(root, previousSelection);
        emitSync(root, { action: 'viewport', state: getState(root) });
      }
      emitHostEvent(root, 'cfxviewportchange', { requestId: request.requestId, sceneRevision: lifetime.sceneRevision, kind });
    }).catch(error => {
      if (!current()) return;
      lifetime.xViewportPending = null;
      emitHostEvent(root, 'cfxviewporterror', { requestId: request.requestId, sceneRevision: request.sceneRevision, kind,
        message: error instanceof Error ? error.message : String(error) });
    });
  };
  const requestXViewportZoom = (root, factor) => {
    const plot = chartXProjection(root)?.plotBounds;
    if (!plot || !Number.isFinite(factor) || factor <= 0) return;
    const middle = plot.x + plot.width / 2, half = plot.width / (2 * factor);
    requestXViewport(root, 'zoom', middle - half, middle + half);
  };
