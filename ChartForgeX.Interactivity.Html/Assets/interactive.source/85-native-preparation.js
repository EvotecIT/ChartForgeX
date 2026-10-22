  // Every native edit shares one scene revision, cancellation and generated-section commit owner.
  let chartSceneSequence = 0, chartPreparationRequestSequence = 0;
  const nextChartSceneRevision = () => 'cfx-scene-' + (++chartSceneSequence);
  const nativePreparationBinding = (root, channel) => chartLifetimes.get(root)?.preparationConnections[channel] || null;
  const cancelNativePreparation = lifetime => {
    const pending = lifetime?.preparationPending;
    if (!pending) return;
    lifetime.preparationPending = null; pending.abort.abort(); pending.cancelled?.();
    if (lifetime.controller) { syncXNavigation(lifetime.controller.root); syncHierarchyNavigation(lifetime.controller.root); }
  };
  const setNativePreparationConnection = (root, channel, connection, settings = {}) => {
    const lifetime = chartLifetimes.get(root);
    if (connection !== null && typeof connection !== 'function') throw new TypeError('Expected a native preparation callback or null to disconnect.');
    cancelNativePreparation(lifetime);
    resetAnnotationEditor(root);
    if (lifetime.preparationSession && lifetime.preparationSession !== settings.session) {
      for (const key of Object.keys(lifetime.preparationConnections))
        if (lifetime.preparationConnections[key]?.session === lifetime.preparationSession) delete lifetime.preparationConnections[key];
      lifetime.preparationSession = null;
      syncXViewportControls(root);
    }
    lifetime.preparationConnections[channel] = connection ? Object.freeze({ ...settings, callback: connection }) : null;
    syncHierarchyNavigation(root);
  };
  const nativePreparationBasis = basis => {
    if (typeof basis !== 'string' || !basis.trim()) throw new TypeError('A managed native section requires its nonempty opaque basis token.');
    return basis;
  };
  const connectNativePreparation = (root, connection, options = {}) => {
    if (connection !== null && typeof connection !== 'function') throw new TypeError('Expected a native preparation callback or null to disconnect.');
    if (options.hierarchy !== undefined && typeof options.hierarchy !== 'boolean') throw new TypeError('The hierarchy capability must be boolean.');
    const axes = options.axes === null ? null : nativeViewportAxes(options.axes);
    const motion = captureChartUpdateMotion(options.motion);
    const session = connection ? { basis: nativePreparationBasis(options.basis) } : null;
    if (connection && axes !== null && axes.some(name => !chartAxisProjection(root, name)))
      throw new TypeError('This chart has no native projection for every requested physical axis.');
    if (connection && options.hierarchy === true && (!hasFeature(root, 'HierarchyNavigation') || !capturedHierarchy(root)))
      throw new TypeError('Native hierarchy preparation requires an enabled Treemap or Sunburst hierarchy.');
    const lifetime = chartLifetimes.get(root);
    cancelNativePreparation(lifetime);
    resetAnnotationEditor(root);
    lifetime.preparationConnections = {}; lifetime.preparationSession = session;
    if (session) {
      setNativePreparationConnection(root, 'annotation', connection, { session, motion });
      if (options.hierarchy === true) setNativePreparationConnection(root, 'hierarchy', connection, { session, motion });
      if (axes !== null) {
        setNativePreparationConnection(root, 'viewport', connection, { session, axes, legacy: false, motion });
        applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
      }
    }
    syncXViewportControls(root); syncHierarchyNavigation(root);
  };
  const replaceChartSection = (root, lifetime, next, nativeKind = '', adaptiveDrawing = '', preserveViewport = false, nativeState = null, motion = null) => {
    // Validate alternates before releasing the committed scene.
    const adaptive = adaptiveDrawing ? lifetime.adaptive : readAdaptiveLayout(next);
    // Commit one active drawing before notifying hosts or exposing fresh controls.
    // Native replies retain their captured layout policy; ordinary updates use the
    // current host width and the new breakpoint. Hidden hosts retain their drawing.
    if (adaptive && !adaptiveDrawing) {
      if (nativeKind && lifetime.adaptive) adaptive.current = lifetime.adaptive.current;
      else {
        const width = root.getBoundingClientRect().width;
        if (width > 0) adaptive.current = width < adaptive.breakpoint ? 'narrow' : 'wide';
        else if (lifetime.adaptive) adaptive.current = lifetime.adaptive.current;
      }
      next = adaptive[adaptive.current].cloneNode(true);
    }
    const state = chartUpdateState(root, !!adaptiveDrawing);
    const active = root.getRootNode().activeElement;
    const navigationFocus = active && root.contains(active) ? active.dataset?.cfxXControl : null;
    const hierarchyFocus = active && root.contains(active) ? active.dataset?.cfxHierarchyControl : null;
    const suspended = nativeKind === 'reset' || !!adaptiveDrawing && pointerReadoutSuspended(root);
    // Retain the request until validation can report failures.
    if (nativeKind) lifetime.preparationPending = null;
    if (nativeKind === 'reset') { state.selected = []; state.muted = []; state.isolated = []; state.focused = null; }
    releaseChartLifetime(root);
    installChartSection(root, next);
    // Retain the root and connection; replace scene-owned work.
    lifetime.disposed = false; lifetime.sceneRevision = nextChartSceneRevision();
    // Publish the accepted native basis before restoring state can notify a host.
    if (nativeState && lifetime.preparationSession === nativeState.session) nativeState.session.basis = nativeState.basis;
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
    if (hierarchyFocus) Array.from(root.querySelectorAll('[data-cfx-hierarchy-control]'))
      .find(control => control.dataset.cfxHierarchyControl === hierarchyFocus && !control.disabled)?.focus({ preventScroll: true });
    syncHierarchyNavigation(root);
    beginChartUpdateMotion(root, motion);
  };
  const requestNativePreparation = (root, channel, payload, hooks) => {
    const lifetime = chartLifetimes.get(root), binding = nativePreparationBinding(root, channel);
    if (!binding || lifetime.disposed || !root.isConnected) return false;
    cancelNativePreparation(lifetime);
    const request = Object.freeze({ ...payload, chartId: root.dataset.cfxChartId,
      requestId: 'cfx-request-' + (++chartPreparationRequestSequence), sceneRevision: lifetime.sceneRevision,
      ...(binding.session ? { channel, basis: binding.session.basis } : {}),
      ...(lifetime.adaptive ? { drawing: lifetime.adaptive.current } : {}) });
    const pending = { request, channel, abort: new AbortController(), cancelled: hooks.cancelled }; lifetime.preparationPending = pending;
    syncXNavigation(root); syncHierarchyNavigation(root); hooks.started?.(request);
    const current = () => root.isConnected && chartLifetimes.get(root) === lifetime && !lifetime.disposed
      && nativePreparationBinding(root, channel) === binding && lifetime.preparationPending === pending
      && lifetime.sceneRevision === request.sceneRevision && !pending.abort.signal.aborted;
    emitHostEvent(root, hooks.eventPrefix + 'request', request);
    Promise.resolve().then(() => current() ? binding.callback(request, pending.abort.signal) : undefined).then(reply => {
      if (!current()) return;
      const nativeState = binding.session ? { session: binding.session, basis: nativePreparationBasis(reply?.basis) } : null;
      const html = binding.session ? reply.html : reply;
      if (typeof html !== 'string') throw new TypeError('A native preparation callback must return a fresh generated section HTML string.');
      const next = readChartSection(html, true).cloneNode(true);
      if (next.dataset.cfxChartId !== request.chartId) throw new TypeError('A native preparation reply must retain the chart identity.');
      hooks.validate(next, request);
      const before = hooks.beforeCommit?.();
      replaceChartSection(root, lifetime, next, hooks.resetView ? 'reset' : 'prepared', '', hooks.preserveViewport === true, nativeState, binding.motion);
      hooks.accepted?.(); hooks.committed?.(request, before);
      emitHostEvent(root, hooks.eventPrefix + 'change', { requestId: request.requestId, sceneRevision: lifetime.sceneRevision, kind: request.kind });
    }).catch(error => {
      if (!current()) return;
      lifetime.preparationPending = null; syncXNavigation(root); syncHierarchyNavigation(root); hooks.failed?.(error, request);
      emitHostEvent(root, hooks.eventPrefix + 'error', { requestId: request.requestId, sceneRevision: request.sceneRevision, kind: request.kind,
        message: error instanceof Error ? error.message : String(error) });
    });
    return true;
  };
