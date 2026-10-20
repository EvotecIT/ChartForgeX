  // Every native edit shares one scene revision, cancellation and generated-section commit owner.
  let chartSceneSequence = 0, chartPreparationRequestSequence = 0;
  const nextChartSceneRevision = () => 'cfx-scene-' + (++chartSceneSequence);
  const nativePreparationBinding = (root, channel) => chartLifetimes.get(root)?.preparationConnections[channel] || null;
  const cancelNativePreparation = lifetime => {
    const pending = lifetime?.preparationPending;
    if (!pending) return;
    lifetime.preparationPending = null; pending.abort.abort();
    if (lifetime.controller) syncXNavigation(lifetime.controller.root);
  };
  const setNativePreparationConnection = (root, channel, connection, settings = {}) => {
    const lifetime = chartLifetimes.get(root);
    if (connection !== null && typeof connection !== 'function') throw new TypeError('Expected a native preparation callback or null to disconnect.');
    cancelNativePreparation(lifetime);
    lifetime.preparationConnections[channel] = connection ? Object.freeze({ ...settings, callback: connection }) : null;
  };
  const replaceChartSection = (root, lifetime, next, nativeKind = '', adaptiveDrawing = '', preserveViewport = false) => {
    // Validate alternates before releasing the committed scene.
    const adaptive = adaptiveDrawing ? lifetime.adaptive : readAdaptiveLayout(next);
    // A native reply carries fresh wide/narrow scenes. Commit the active drawing
    // directly, rather than briefly installing wide chrome before an observer swaps it.
    if (nativeKind && adaptive && lifetime.adaptive) {
      adaptive.current = lifetime.adaptive.current;
      next = adaptive[adaptive.current].cloneNode(true);
    }
    const state = chartUpdateState(root, !!adaptiveDrawing);
    const active = root.getRootNode().activeElement;
    const navigationFocus = active && root.contains(active) ? active.dataset?.cfxXControl : null;
    const suspended = nativeKind === 'reset' || !!adaptiveDrawing && pointerReadoutSuspended(root);
    // Retain the request until validation can report failures.
    if (nativeKind) lifetime.preparationPending = null;
    if (nativeKind === 'reset') { state.selected = []; state.muted = []; state.isolated = []; state.focused = null; }
    releaseChartLifetime(root);
    installChartSection(root, next);
    // Retain the root and connection; replace scene-owned work.
    lifetime.disposed = false; lifetime.sceneRevision = nextChartSceneRevision();
    lifetime.adaptive = adaptive;
    if (adaptiveDrawing) adaptive.current = adaptiveDrawing;
    chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    // Preserve reset dismissal before binding the fresh scene.
    if (suspended) suspendPointerReadout(root);
    prepareAdaptiveRoot(root, lifetime);
    bindChart(root); watchChartLifetimes(); restoreChartUpdateState(root, state, !!nativeKind && !preserveViewport);
    bindAdaptiveLayout(root);
    syncXViewportControls(root);
    if (navigationFocus) Array.from(root.querySelectorAll('[data-cfx-x-control]'))
      .find(control => control.dataset.cfxXControl === navigationFocus && !control.disabled)?.focus({ preventScroll: true });
  };
  const requestNativePreparation = (root, channel, payload, hooks) => {
    const lifetime = chartLifetimes.get(root), binding = nativePreparationBinding(root, channel);
    if (!binding || lifetime.disposed || !root.isConnected) return false;
    cancelNativePreparation(lifetime);
    const request = Object.freeze({ ...payload, chartId: root.dataset.cfxChartId,
      requestId: 'cfx-request-' + (++chartPreparationRequestSequence), sceneRevision: lifetime.sceneRevision,
      ...(lifetime.adaptive ? { drawing: lifetime.adaptive.current } : {}) });
    const pending = { request, channel, abort: new AbortController() }; lifetime.preparationPending = pending;
    syncXNavigation(root);
    const current = () => root.isConnected && chartLifetimes.get(root) === lifetime && !lifetime.disposed
      && nativePreparationBinding(root, channel) === binding && lifetime.preparationPending === pending
      && lifetime.sceneRevision === request.sceneRevision && !pending.abort.signal.aborted;
    emitHostEvent(root, hooks.eventPrefix + 'request', request);
    Promise.resolve().then(() => current() ? binding.callback(request, pending.abort.signal) : undefined).then(html => {
      if (!current()) return;
      if (typeof html !== 'string') throw new TypeError('A native preparation callback must return a fresh generated section HTML string.');
      const next = readChartSection(html, true).cloneNode(true);
      if (next.dataset.cfxChartId !== request.chartId) throw new TypeError('A native preparation reply must retain the chart identity.');
      hooks.validate(next, request);
      const before = hooks.beforeCommit?.();
      replaceChartSection(root, lifetime, next, hooks.resetView ? 'reset' : 'prepared', '', hooks.preserveViewport === true);
      hooks.accepted?.(); hooks.committed?.(request, before);
      emitHostEvent(root, hooks.eventPrefix + 'change', { requestId: request.requestId, sceneRevision: lifetime.sceneRevision, kind: request.kind });
    }).catch(error => {
      if (!current()) return;
      lifetime.preparationPending = null; syncXNavigation(root);
      emitHostEvent(root, hooks.eventPrefix + 'error', { requestId: request.requestId, sceneRevision: request.sceneRevision, kind: request.kind,
        message: error instanceof Error ? error.message : String(error) });
    });
    return true;
  };
