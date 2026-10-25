  // One scene/revision/cancel/commit owner.
  let chartSceneSequence = 0, chartPreparationRequestSequence = 0;
  const nextChartSceneRevision = () => 'cfx-scene-' + (++chartSceneSequence);
  const nativePreparationBinding = (root, channel) => chartLifetimes.get(root)?.preparationConnections[channel] || null;
  const nativeNavigationFocus = root => {
    const active = root.getRootNode().activeElement;
    return active && root.contains(active) && (active.dataset?.cfxXControl || active.dataset?.cfxHierarchyControl)
      ? { x: active.dataset.cfxXControl, hierarchy: active.dataset.cfxHierarchyControl, node: active.dataset.cfxHierarchyNode } : null;
  };
  const restoreNativeNavigationFocus = (root, focus) => {
    if (!focus) return;
    Array.from(root.querySelectorAll('[data-cfx-x-control],[data-cfx-hierarchy-control]')).find(control => !control.disabled
      && (focus.x ? control.dataset.cfxXControl === focus.x : control.dataset.cfxHierarchyControl === focus.hierarchy && control.dataset.cfxHierarchyNode === focus.node))?.focus({ preventScroll: true });
  };
  const holdNativeNavigationFocus = root => {
    const identity = nativeNavigationFocus(root);
    if (!identity) return null;
    const element = root.getRootNode().activeElement, releases = [];
    let eligible = true, disabling = true;
    const release = () => { for (const cleanup of releases.splice(0)) cleanup(); };
    const moved = event => {
      const target = event.composedPath?.()[0] || event.target;
      // Disabling can blur the owned control in a later browser turn.
      if (event.type === 'focusout' && target === element && element.disabled) return;
      if (event.type === 'focusin' && disabling && target === root.ownerDocument.body) return;
      if (event.type !== 'focusout' && !(event.type === 'keydown' && event.key === 'Tab') && element.contains(target)) return;
      eligible = false; release();
    };
    for (const tree of tooltipRootTrees(root)) for (const name of ['focusin', 'focusout', 'pointerdown', 'click', 'keydown'])
      releases.push(listenChart(root, tree, name, moved, true));
    releases.push(listenChart(root, root.ownerDocument.defaultView, 'blur', () => { eligible = false; release(); }));
    return { element, release, ready: () => { disabling = false; }, take: () => {
      const active = root.getRootNode().activeElement;
      const focus = active !== element && active && active !== root.ownerDocument.body ? nativeNavigationFocus(root)
        : eligible ? identity : null;
      release(); return focus;
    } };
  };
  const cancelNativePreparation = (lifetime, preserveProgressQueue = false) => {
    if (!preserveProgressQueue) cancelProgressCadenceQueued(lifetime);
    const pending = lifetime?.preparationPending;
    if (!pending) return;
    lifetime.preparationPending = null; pending.navigationFocus?.release(); pending.abort.abort(); pending.cancelled?.();
    if (lifetime.controller) { syncXNavigation(lifetime.controller.root); syncHierarchyNavigation(lifetime.controller.root); syncNativePreparationAdmission(lifetime.controller.root); }
  };
  const setNativePreparationConnection = (root, channel, connection, settings = {}) => {
    const lifetime = chartLifetimes.get(root);
    if (connection !== null && typeof connection !== 'function') throw new TypeError('Native callback: function|null required.');
    cancelNativePreparation(lifetime); releaseProgressCadence(lifetime);
    resetAnnotationEditor(root);
    if (lifetime.preparationSession && lifetime.preparationSession !== settings.session) {
      for (const key of Object.keys(lifetime.preparationConnections))
        if (lifetime.preparationConnections[key]?.session === lifetime.preparationSession) delete lifetime.preparationConnections[key];
      lifetime.preparationSession = null;
      syncXViewportControls(root);
    }
    lifetime.preparationConnections[channel] = connection ? Object.freeze({ ...settings, callback: connection }) : null;
    syncHierarchyNavigation(root); syncNativePreparationAdmission(root);
  };
  const nativePreparationBasis = basis => {
    if (typeof basis !== 'string' || !basis.trim()) throw new TypeError('Native basis: nonempty string.');
    return basis;
  };
  const connectNativePreparation = (root, connection, options = {}) => {
    if (connection !== null && typeof connection !== 'function') throw new TypeError('Native callback: function|null required.');
    if (options.progressEditing !== undefined && typeof options.progressEditing !== 'boolean') throw new TypeError('Progress editing must be boolean.');
    if (options.hierarchy !== undefined && typeof options.hierarchy !== 'boolean') throw new TypeError('Hierarchy must be boolean.');
    if (options.legend !== undefined && typeof options.legend !== 'boolean') throw new TypeError('Native legend must be boolean.');
    if (options.coordinateSystem !== undefined && !['cartesian', 'polar'].includes(options.coordinateSystem))
      throw new TypeError('coordinateSystem: cartesian|polar.');
    const topology = capturedHierarchy(root)?.kind === 'topology';
    const polar = options.coordinateSystem === 'polar';
    if (topology && (polar || options.progressEditing === true)) throw new TypeError('Topology hierarchy: no numeric or progress projection.');
    const gestures = nativePolarGestures(options, polar);
    const axes = options.axes === null || topology && options.axes === undefined ? null : polar ? nativePolarAxes(options.axes) : nativeViewportAxes(options.axes);
    if (topology && axes !== null) throw new TypeError('Topology hierarchy: axis projections unavailable.');
    if (gestures && axes === null) throw new TypeError('Polar gestures: axes required.');
    const motion = captureChartUpdateMotion(options.motion);
    const progressCadenceMs = captureProgressCadence(options);
    const preparationBackpressure = captureNativePreparationBackpressure(options);
    if (connection && preparationBackpressure === 'latest-window' && axes === null && options.hierarchy !== true)
      throw new TypeError('preparationBackpressure: native viewport axes or hierarchy reset owner required.');
    const session = connection ? { basis: nativePreparationBasis(options.basis), preparationBackpressure } : null;
    if (connection && axes !== null && axes.some(name => polar ? !polarAxisAvailable(root, name) : !chartAxisProjection(root, name)))
      throw new TypeError('Axis projection unavailable.');
    if (connection && options.hierarchy === true && (!hasFeature(root, 'HierarchyNavigation') || !capturedHierarchy(root)))
      throw new TypeError('Hierarchy: supported native hierarchy and HierarchyNavigation required.');
    if (connection && options.legend === true && (!hasFeature(root, 'LegendToggles') || !capturedNativeVisibility(root)
        || chartLegendStateTargets(root, false).length || chartLegendStateTargets(root, true).length))
      throw new TypeError('Native legend: authored retained Line/Bar visibility and LegendToggles required; reset legacy mute/isolation first.');
    const lifetime = chartLifetimes.get(root);
    cancelNativePreparation(lifetime); releaseProgressCadence(lifetime);
    resetAnnotationEditor(root);
    lifetime.preparationConnections = {}; lifetime.preparationSession = session;
    if (session) {
      setNativePreparationConnection(root, 'annotation', connection, { session, motion });
      if (options.legend === true) setNativePreparationConnection(root, 'legend', connection, { session, motion });
      if (options.hierarchy === true) setNativePreparationConnection(root, 'hierarchy', connection, { session, motion });
      if (options.progressEditing === true) setNativePreparationConnection(root, 'progress', connection, { session, motion, progressCadenceMs });
      if (axes !== null) {
        setNativePreparationConnection(root, 'viewport', connection, { session, axes, legacy: false, motion, ...(polar ? { coordinateSystem: 'polar', gestures } : {}) });
        applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
      }
    }
    syncXViewportControls(root); syncHierarchyNavigation(root); syncProgressEditing(root); syncNativePreparationAdmission(root);
  };
  const replaceChartSection = (root, lifetime, next, nativeKind = '', adaptiveDrawing = '', preserveViewport = false, nativeState = null, motion = null, preparedAdaptive, capturedMotion = null) => {
    // Validate before release.
    const adaptive = adaptiveDrawing ? lifetime.adaptive : preparedAdaptive === undefined ? readAdaptiveLayout(next) : preparedAdaptive;
    // Commit before host events.
    // Reply: keep layout; update: live width; hidden: keep scene.

    if (adaptive && !adaptiveDrawing) {
      if (nativeKind && lifetime.adaptive) adaptive.current = lifetime.adaptive.current;
      else {
        const width = root.getBoundingClientRect().width;
        if (width > 0) adaptive.current = width < adaptive.breakpoint ? 'narrow' : 'wide';
        else if (lifetime.adaptive) adaptive.current = lifetime.adaptive.current;
      }
      next = adaptive[adaptive.current].cloneNode(true);
    }
    const queuedProgress = nativeKind && lifetime.preparationPending?.channel === 'progress' ? takeProgressCadenceQueue(lifetime) : null;
    const state = chartUpdateState(root, !!adaptiveDrawing);
    const active = root.getRootNode().activeElement;
    const heldFocus = nativeKind ? lifetime.preparationPending?.navigationFocus : null;
    const navigationFocus = heldFocus ? heldFocus.take() : nativeNavigationFocus(root);
    if (heldFocus && !navigationFocus && active === heldFocus.element) state.ownedFocus = false;
    const suspended = nativeKind === 'reset' || !!adaptiveDrawing && pointerReadoutSuspended(root);
    // Keep pending through validation.
    if (nativeKind) lifetime.preparationPending = null;
    if (nativeKind === 'reset') { state.selected = []; state.muted = []; state.isolated = []; state.focused = null; }
    releaseChartLifetime(root);
    installChartSection(root, next);
    // Retain root/connections.
    lifetime.disposed = false; lifetime.sceneRevision = nextChartSceneRevision();
    // Publish basis before restore/events.
    if (nativeState && lifetime.preparationSession === nativeState.session) nativeState.session.basis = nativeState.basis;
    lifetime.adaptive = adaptive;
    if (adaptiveDrawing) adaptive.current = adaptiveDrawing;
    chartLifetimes.set(root, lifetime); mountedCharts.add(root);
    rememberNativeDrawingSnapshot(root, lifetime, next);
    // Retain reset dismissal.
    if (suspended) suspendPointerReadout(root);
    prepareAdaptiveRoot(root, lifetime);
    bindChart(root); watchChartLifetimes(); restoreChartUpdateState(root, state, !!nativeKind && !preserveViewport);
    bindAdaptiveLayout(root);
    syncXViewportControls(root);
    restoreNativeNavigationFocus(root, navigationFocus);
    syncHierarchyNavigation(root);
    beginChartUpdateMotion(root, motion, capturedMotion);
    restoreProgressCadenceQueue(root, lifetime, queuedProgress);
    syncNativePreparationAdmission(root);
  };
  const performNativePreparation = (root, channel, payload, hooks, preserveProgressQueue = false, navigationFocus = null) => {
    const lifetime = chartLifetimes.get(root), binding = nativePreparationBinding(root, channel);
    if (!binding || lifetime.disposed || !root.isConnected) { navigationFocus?.release(); return false; }
    cancelNativePreparation(lifetime, preserveProgressQueue);
    const request = Object.freeze({ ...payload, chartId: root.dataset.cfxChartId,
      requestId: 'cfx-request-' + (++chartPreparationRequestSequence), sceneRevision: lifetime.sceneRevision,
      ...(binding.session ? { channel, basis: binding.session.basis } : {}),
      ...(lifetime.adaptive ? { drawing: lifetime.adaptive.current } : {}) });
    const pending = { request, channel, abort: new AbortController(), cancelled: hooks.cancelled, navigationFocus: navigationFocus || holdNativeNavigationFocus(root) }; lifetime.preparationPending = pending;
    syncXNavigation(root); syncHierarchyNavigation(root); syncNativePreparationAdmission(root); pending.navigationFocus?.ready(); hooks.started?.(request);
    const current = () => root.isConnected && chartLifetimes.get(root) === lifetime && !lifetime.disposed
      && nativePreparationBinding(root, channel) === binding && lifetime.preparationPending === pending
      && lifetime.sceneRevision === request.sceneRevision && !pending.abort.signal.aborted;
    emitHostEvent(root, hooks.eventPrefix + 'request', request);
    Promise.resolve().then(() => current() ? invokeNativePreparationCallback(lifetime, binding, request, pending.abort.signal) : undefined).then(reply => {
      if (!current()) return;
      const nativeState = binding.session ? { session: binding.session, basis: nativePreparationBasis(reply?.basis) } : null;
      const html = binding.session ? reply.html : reply;
      if (typeof html !== 'string') throw new TypeError('Native reply: fresh HTML string required.');
      const next = readChartSection(html, true).cloneNode(true);
      if (next.dataset.cfxChartId !== request.chartId) throw new TypeError('Native reply: chart ID changed.');
      // Validate all before basis.
      const adaptive = readAdaptiveLayout(next);
      const sourceChanges = [];
      for (const [name, drawing] of adaptive ? [['wide', adaptive.wide], ['narrow', adaptive.narrow]] : [['wide', next]]) {
        hooks.validate(drawing, request);
        const before = lifetime.nativeDrawingSnapshot?.[name], a = before && nativeRawSource(before), b = nativeRawSource(drawing);
        if (a && b && a.sourceKey !== b.sourceKey) sourceChanges.push([drawing, before]);
        else validateRequestedNativeVisibility(root, drawing, request, before);
      }
      const capturedMotion = captureNativeValueMotion(root, next, binding.session ? reply.transition : undefined, request, adaptive);
      // Only a completely validated SourceReflow can relax the same-value source key. Old/default replies keep their existing validation order.
      for (const [drawing, previous] of sourceChanges) validateRequestedNativeVisibility(root, drawing, request, previous, capturedMotion?.sourceReflow === true);
      const before = hooks.beforeCommit?.();
      replaceChartSection(root, lifetime, next, hooks.resetView ? 'reset' : 'prepared', '', hooks.preserveViewport === true, nativeState, binding.motion, adaptive, capturedMotion);
      hooks.accepted?.(); hooks.committed?.(request, before);
      emitHostEvent(root, hooks.eventPrefix + 'change', { requestId: request.requestId, sceneRevision: lifetime.sceneRevision, kind: request.kind });
    }).catch(error => {
      if (!current()) return;
      const navigationFocus = pending.navigationFocus?.take();
      lifetime.preparationPending = null; syncXNavigation(root); syncHierarchyNavigation(root); syncNativePreparationAdmission(root);
      restoreNativeNavigationFocus(root, navigationFocus); hooks.failed?.(error, request);
      emitHostEvent(root, hooks.eventPrefix + 'error', { requestId: request.requestId, sceneRevision: request.sceneRevision, kind: request.kind,
        message: error instanceof Error ? error.message : String(error) });
    }).finally(() => {
      pumpProgressCadence(lifetime, binding);
      if (chartLifetimes.get(root) === lifetime && !lifetime.disposed) syncNativePreparationAdmission(root);
    });
    return true;
  };
