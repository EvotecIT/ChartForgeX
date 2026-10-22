  // Native hosts own scenes; the browser owns gestures and reply lifetime.
  const validXProjection = projection => {
    const box = projection?.plotBounds;
    return !!projection && [projection.fullMinimum, projection.fullMaximum, projection.minimum, projection.maximum,
      projection.transformedMinimum, projection.transformedMaximum, projection.transformedFullMinimum, projection.transformedFullMaximum].every(Number.isFinite)
      && projection.fullMinimum < projection.fullMaximum && projection.minimum < projection.maximum
      && projection.transformedMinimum < projection.transformedMaximum && projection.transformedFullMinimum < projection.transformedFullMaximum
      && ['linear', 'time', 'logarithmic', 'symmetriclogarithmic'].includes(projection.scale)
      && Number.isFinite(projection.threshold) && projection.threshold > 0
      && Number.isFinite(projection.logarithmBase ?? 10) && (projection.logarithmBase ?? 10) > 1
      && ['positive', 'negative'].includes(projection.logarithmicDomain ?? 'positive')
      && (projection.scale !== 'logarithmic' || (projection.logarithmicDomain === 'negative' ? projection.fullMaximum < 0 : projection.fullMinimum > 0))
      && validTimeSessions(projection.timeSessions) && validTimeSpacing(projection.timeSpacing)
      && (!(projection.timeSessions || projection.timeSpacing) || projection.scale === 'time') && !(projection.timeSessions && projection.timeSpacing)
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
  const connectedXViewport = root => !!nativePreparationBinding(root, 'viewport');
  const syncXViewportControls = root => {
    const binding = nativePreparationBinding(root, 'viewport');
    const unsupported = !!binding && (!chartXProjection(root) || !binding.legacy && binding.axes.some(name => !chartAxisProjection(root, name)));
    root.querySelectorAll('[data-cfx-zoom],[data-cfx-mode-button="pan"]').forEach(button => { button.disabled = unsupported; });
    if (unsupported && root.dataset.cfxMode === 'pan') applyMode(root, '');
    syncXNavigation(root);
    syncResetControl(root);
  };
  const requestXViewport = (root, kind, left, right, sync = true, accepted, sourceWindow) => {
    if (!connectedXViewport(root) || !chartXProjection(root)) return false;
    if (kind !== 'reset' && (!Number.isFinite(left) || !Number.isFinite(right) || left >= right)) return false;
    if (kind === 'reset') suspendPointerReadout(root);
    return requestNativePreparation(root, 'viewport', { kind,
      ...(sourceWindow ? { sourceWindow: Object.freeze({ minimum: sourceWindow.minimum, maximum: sourceWindow.maximum }) } : {}),
      ...(kind === 'reset' ? {} : { left, right }) }, {
      eventPrefix: 'cfxviewport', resetView: kind === 'reset', accepted,
      validate: next => { if (!chartXProjection(next)) throw new TypeError('The viewport reply requires a supported X projection.'); },
      beforeCommit: () => kind === 'reset' ? selectionSignature(root) : '',
      committed: (_, previousSelection) => {
        if (kind === 'reset') { emitHostEvent(root, 'cfxreset', {}); publishResetSelection(root, previousSelection); }
        if (sync) emitSync(root, { action: 'viewport', state: getState(root), xWindow: nativeXWindow(root) });
      }
    });
  };
  const requestXViewportZoom = (root, factor, logicalX, logicalY) => {
    if (cartesianViewportConnected(root)) { requestCartesianViewportZoom(root, factor, logicalX, logicalY); return; }
    const plot = chartXProjection(root)?.plotBounds;
    if (!plot || !Number.isFinite(factor) || factor <= 0) return;
    const anchor = Number.isFinite(logicalX) ? clamp(logicalX, plot.x, plot.x + plot.width) : plot.x + plot.width / 2;
    requestXViewport(root, 'zoom', anchor - (anchor - plot.x) / factor, anchor + (plot.x + plot.width - anchor) / factor);
  };
  const nativeXWindow = root => {
    const projection = chartXProjection(root);
    return projection ? { minimum: projection.minimum, maximum: projection.maximum, scale: projection.scale || 'linear' } : null;
  };
  const requestNativeXWindow = (root, window, sync = true, accepted) => {
    if (cartesianViewportConnected(root)) return requestNativeCartesianWindow(root, { x: window }, sync, accepted);
    const projection = chartXProjection(root), axis = root._cfxDataProjection?.x;
    if (!connectedXViewport(root) || !projection || !axis || !window
      || !Number.isFinite(window.minimum) || !Number.isFinite(window.maximum) || window.minimum >= window.maximum
      || window.scale && window.scale !== (projection.scale || 'linear')) return false;
    // Reuse the captured data projection's mapping; the host still owns confinement, ticks and native re-preparation.
    const first = projection.plotBounds.x + projectedAxisRatio(axis, window.minimum) * projection.plotBounds.width;
    const last = projection.plotBounds.x + projectedAxisRatio(axis, window.maximum) * projection.plotBounds.width;
    if (!Number.isFinite(first) || !Number.isFinite(last) || first === last) return false;
    requestXViewport(root, 'window', Math.min(first, last), Math.max(first, last), sync, accepted, window);
    return true;
  };
