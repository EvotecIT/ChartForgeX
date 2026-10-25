  const captureProgressCadence = options => {
    const value = options.progressCadenceMs ?? 0;
    if (!Number.isFinite(value) || value < 0 || value > 1000)
      throw new TypeError('progressCadenceMs: finite 0..1000 ms required.');
    if (value && options.progressEditing !== true)
      throw new TypeError('progressCadenceMs: progressEditing required.');
    return value;
  };
  const captureNativePreparationBackpressure = options => {
    const policy = options.preparationBackpressure === undefined ? 'immediate' : options.preparationBackpressure;
    if (!['immediate', 'latest-window'].includes(policy)) throw new TypeError('preparationBackpressure: immediate|latest-window required.');
    if (policy === 'latest-window' && options.progressCadenceMs)
      throw new TypeError('preparationBackpressure: nonzero progressCadenceMs cannot be combined.');
    return policy;
  };
  const latestWindowPreparation = binding => binding?.session?.preparationBackpressure === 'latest-window';
  const nativePreparationFlightKey = binding => latestWindowPreparation(binding) ? binding.session : binding;
  const nativePreparationBackpressureBusy = root => {
    const lifetime = chartLifetimes.get(root), session = lifetime?.preparationSession;
    return !!lifetime && !lifetime.disposed && session?.preparationBackpressure === 'latest-window'
      && (!!lifetime.preparationPending || lifetime.progressFlights?.has(session)
        || lifetime.progressCadence?.binding.session === session && lifetime.progressCadence.queued.size > 0);
  };
  const requireNativePreparationIdle = root => {
    if (nativePreparationBackpressureBusy(root)) throw new TypeError('Native preparation busy: wait for the configured connection callback to settle before an incremental operation.');
  };
  const syncNativePreparationAdmission = root => {
    syncNativeSeriesVisibility(root);
    const lifetime = chartLifetimes.get(root), busy = nativePreparationBackpressureBusy(root);
    const latest = lifetime?.preparationSession?.preparationBackpressure === 'latest-window';
    if (latest && lifetime.preparationAdmissionUi?.busy === busy && lifetime.preparationAdmissionUi.revision === lifetime.sceneRevision) return;
    const exports = [...root.querySelectorAll('[data-cfx-export]')];
    if (!latest && !exports.some(button => Object.hasOwn(button, '_cfxAdmissionDisabled'))) return;
    if (lifetime) lifetime.preparationAdmissionUi = { busy, revision: lifetime.sceneRevision };
    if (busy && !lifetime.preparationAdmissionControls) lifetime.preparationAdmissionControls = ownChartWork(root, () => {
      lifetime.preparationAdmissionControls = null; lifetime.preparationAdmissionUi = null;
      root.querySelectorAll('[data-cfx-export]').forEach(button => {
        if (Object.hasOwn(button, '_cfxAdmissionDisabled')) { button.disabled = button._cfxAdmissionDisabled; delete button._cfxAdmissionDisabled; }
      });
      syncXViewportControls(root); syncHierarchyNavigation(root); refreshAnnotationEditor(root);
    });
    exports.forEach(button => {
      if (busy) {
        if (!Object.hasOwn(button, '_cfxAdmissionDisabled')) button._cfxAdmissionDisabled = button.disabled;
        button.disabled = true;
      } else if (Object.hasOwn(button, '_cfxAdmissionDisabled')) {
        button.disabled = button._cfxAdmissionDisabled; delete button._cfxAdmissionDisabled;
      }
    });
    syncXViewportControls(root); syncHierarchyNavigation(root); refreshAnnotationEditor(root);
  };
  const progressIntentKey = payload => JSON.stringify([payload.seriesKey, payload.observationId]);
  const currentProgressIntent = (root, intent) => {
    const row = editableProgressRows(root).find(node => {
      const identity = targetIdentity(node);
      return identity.seriesKey === intent.payload.seriesKey && identity.observationId === intent.payload.observationId;
    });
    if (!row) return false;
    const domain = progressDomain(root, row);
    return domain.minimum === intent.domain.minimum && domain.maximum === intent.domain.maximum
      && intent.payload.value >= domain.minimum && intent.payload.value <= domain.maximum;
  };
  const cancelProgressCadenceQueued = lifetime => lifetime?.progressCadence?.cancelQueued();
  const releaseProgressCadence = lifetime => lifetime?.progressCadence?.release();
  const takeProgressCadenceQueue = lifetime => {
    const state = lifetime.progressCadence;
    if (!state) return null;
    const queue = { binding: state.binding, intents: [...state.queued.values()] };
    state.queued.clear(); return queue;
  };
  const restoreProgressCadenceQueue = (root, lifetime, queue) => {
    if (!queue || nativePreparationBinding(root, 'progress') !== queue.binding) return;
    for (const intent of queue.intents) {
      if (currentProgressIntent(root, intent)) queueNativeReplacementPreparation(root, lifetime, queue.binding, intent);
      else intent.hooks.cancelled?.();
    }
  };
  const pumpProgressCadence = (lifetime, binding) => {
    const state = lifetime.progressCadence;
    if (state && (state.binding === binding || latestWindowPreparation(binding) && state.binding.session === binding.session)) state.pump();
  };
  const invokeNativePreparationCallback = (lifetime, binding, request, signal) => {
    if (!latestWindowPreparation(binding) && (request.channel !== 'progress' || !binding.progressCadenceMs)) return binding.callback(request, signal);
    // The existing physical-flight owner is keyed by the Progress binding or by
    // the configured latest-window session. Logical abort never releases a slot.
    lifetime.progressFlights ||= new WeakMap(); lifetime.progressDispatchTimes ||= new WeakMap();
    const key = nativePreparationFlightKey(binding), flight = { request }; lifetime.progressFlights.set(key, flight);
    lifetime.progressDispatchTimes.set(key, performance.now());
    const settled = () => { if (lifetime.progressFlights.get(key) === flight) lifetime.progressFlights.delete(key); };
    let result;
    try { result = binding.callback(request, signal); }
    catch (error) { settled(); throw error; }
    return Promise.resolve(result).then(reply => { settled(); return reply; }, error => { settled(); throw error; });
  };
  const queueNativeReplacementPreparation = (root, lifetime, binding, intent) => {
    let state = lifetime.progressCadence;
    if (state?.binding !== binding && !(latestWindowPreparation(binding) && state?.binding.session === binding.session)) {
      const focus = state?.navigationFocus; if (state) state.navigationFocus = null;
      state?.release();
      state = { binding, channel: intent.channel || 'progress', queued: new Map(), timer: 0, scheduled: false, active: true, navigationFocus: focus || null };
      state.cancelQueued = () => {
        if (state.timer) { window.clearTimeout(state.timer); state.timer = 0; }
        const queued = [...state.queued.values()]; state.queued.clear();
        for (const item of queued) item.hooks.cancelled?.();
        state.navigationFocus?.release(); state.navigationFocus = null;
        syncNativePreparationAdmission(root);
      };
      state.release = ownChartWork(root, () => {
        state.active = false; state.cancelQueued();
        if (lifetime.progressCadence === state) lifetime.progressCadence = null;
      });
      state.pump = () => {
        if (!state.active || !state.queued.size || state.timer || state.scheduled) return;
        if (!root.isConnected || lifetime.disposed || chartLifetimes.get(root) !== lifetime
            || nativePreparationBinding(root, state.channel) !== binding) { state.release(); return; }
        if (lifetime.progressFlights?.has(nativePreparationFlightKey(binding))) return;
        const prior = latestWindowPreparation(binding) ? undefined : lifetime.progressDispatchTimes?.get(binding);
        const delay = prior === undefined ? 0 : Math.max(0, binding.progressCadenceMs - (performance.now() - prior));
        if (delay > 0) { state.timer = window.setTimeout(() => { state.timer = 0; state.pump(); }, delay); return; }
        // One admission microtask coalesces same-row producer bursts before the
        // canonical owner creates an ID, emits a request or calls the native host.
        state.scheduled = true;
        Promise.resolve().then(() => {
          state.scheduled = false;
          if (!state.active || !state.queued.size || lifetime.progressFlights?.has(nativePreparationFlightKey(binding))) return;
          if (!root.isConnected || lifetime.disposed || chartLifetimes.get(root) !== lifetime
              || nativePreparationBinding(root, state.channel) !== binding) { state.release(); return; }
          const [key, queued] = state.queued.entries().next().value; state.queued.delete(key);
          const current = queued.channel ? lifetime.sceneRevision === queued.sceneRevision && queued.binding.session.basis === queued.basis
              && nativePreparationBinding(root, queued.channel) === queued.binding
            : currentProgressIntent(root, queued);
          if (!current) {
            queued.hooks.cancelled?.();
            if (!state.queued.size) { state.navigationFocus?.release(); state.navigationFocus = null; }
            state.pump(); syncNativePreparationAdmission(root); return;
          }
          const focus = state.navigationFocus; state.navigationFocus = null;
          performNativePreparation(root, queued.channel || 'progress', queued.payload, queued.hooks, true, focus);
        });
      };
      lifetime.progressCadence = state;
    }
    // One replacement map: Progress retains one entry per admitted native row;
    // latest-window uses one fixed key for the whole configured connection.
    if (intent.navigationFocus) state.navigationFocus = intent.navigationFocus;
    const key = intent.channel ? 'replacement' : progressIntentKey(intent.payload), prior = state.queued.get(key);
    prior?.hooks.cancelled?.(); state.queued.set(key, intent); state.pump(); syncNativePreparationAdmission(root); return true;
  };
  const requestNativePreparation = (root, channel, payload, hooks) => {
    const lifetime = chartLifetimes.get(root), binding = nativePreparationBinding(root, channel);
    if (!binding || lifetime.disposed || !root.isConnected) return false;
    const sourceVisibility = payload.kind === 'reset' ? nativeVisibilityResetDescriptor(root) : null;
    if (sourceVisibility) payload = { ...payload, sourceVisibility };
    if (latestWindowPreparation(binding)) {
      const replacement = channel === 'viewport' && payload.kind === 'window' || payload.kind === 'reset';
      if (!replacement && nativePreparationBackpressureBusy(root)) return false;
      settleChartUpdateMotion(root);
      if (replacement) {
        const pending = lifetime.preparationPending, navigationFocus = pending?.navigationFocus
          || lifetime.progressCadence?.navigationFocus || holdNativeNavigationFocus(root);
        if (pending?.navigationFocus) pending.navigationFocus = null;
        const queued = queueNativeReplacementPreparation(root, lifetime, binding,
          { channel, binding, payload: Object.freeze({ ...payload }), basis: binding.session.basis, sceneRevision: lifetime.sceneRevision, navigationFocus, hooks });
        // Publish the one intent before abort listeners can replace/reconnect it.
        // Never cancel work installed reentrantly by those listeners or focus changes.
        if (lifetime.preparationPending === pending) cancelNativePreparation(lifetime, true);
        return queued;
      }
      return performNativePreparation(root, channel, payload, hooks);
    }
    settleChartUpdateMotion(root);
    if (channel !== 'progress' || payload.kind !== 'replace-value' || !binding.progressCadenceMs)
      return performNativePreparation(root, channel, payload, hooks);
    const row = editableProgressRows(root).find(node => {
      const identity = targetIdentity(node);
      return identity.seriesKey === payload.seriesKey && identity.observationId === payload.observationId;
    });
    if (!row) throw new TypeError('Progress: existing identified row required.');
    const pending = lifetime.preparationPending;
    // Different rows are independent replacement facts. Same-row supersession
    // keeps the existing abort/accept rule; other rows wait in first-admitted order.
    if (pending && (pending.channel !== 'progress' || progressIntentKey(pending.request) === progressIntentKey(payload)))
      cancelNativePreparation(lifetime, true);
    return queueNativeReplacementPreparation(root, lifetime, binding,
      { payload: Object.freeze({ ...payload }), domain: progressDomain(root, row), hooks });
  };
