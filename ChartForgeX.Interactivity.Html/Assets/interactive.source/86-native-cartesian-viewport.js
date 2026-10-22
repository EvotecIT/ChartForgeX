  // Every physical axis uses one captured transform and the existing native preparation lifetime.
  const cartesianAxisNames = Object.freeze(['x', 'y', 'secondaryX', 'secondaryY']);
  const nativeViewportAxes = (axes, defaults = ['x']) => {
    const selected = axes === undefined ? defaults : axes;
    if (!Array.isArray(selected) || !selected.length || selected.length > 4
      || selected.some(axis => !cartesianAxisNames.includes(axis)) || new Set(selected).size !== selected.length)
      throw new TypeError('Native viewport axes require a nonempty distinct array of x, y, secondaryX or secondaryY.');
    return Object.freeze([...selected]);
  };
  const capturedCartesianMetadata = root => {
    try { return JSON.parse(root.dataset.cfxPreparedChart); } catch { return {}; }
  };
  const chartAxisProjection = (root, name) => capturedCartesianMetadata(root)[name + 'Projection'] || null;
  const chartYProjection = root => chartAxisProjection(root, 'y');
  const nativeAxisWindow = (root, name) => {
    const projection = chartAxisProjection(root, name);
    return projection ? { minimum: projection.minimum, maximum: projection.maximum, scale: projection.scale } : null;
  };
  const nativeYWindow = root => nativeAxisWindow(root, 'y');
  const nativeYWindowChanged = root => {
    const projection = chartYProjection(root);
    return !!projection && (projection.minimum !== projection.fullMinimum || projection.maximum !== projection.fullMaximum);
  };
  const nativeViewportChanged = root => cartesianAxisNames.some(name => {
    const projection = chartAxisProjection(root, name);
    return projection && (projection.minimum !== projection.fullMinimum || projection.maximum !== projection.fullMaximum);
  });
  const cartesianViewportConnected = root => nativePreparationBinding(root, 'viewport')?.legacy === false;
  const nativeViewportWindows = (root, connectedOnly = false) => ({ ...Object.fromEntries(
    (connectedOnly ? cartesianAxisNames.filter(name => chartAxisProjection(root, name)) : cartesianAxisNames).map(name => [name, nativeAxisWindow(root, name)])),
    ...(capturedCartesianMetadata(root).valueDomain ? { valueDomain: capturedCartesianMetadata(root).valueDomain } : {}) });
  const logicalChartPointer = (root, event) => {
    const matrix = svgScreenMatrix(root.querySelector('.cfx-stage svg'));
    if (!matrix) return null;
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    return Number.isFinite(point.x) && Number.isFinite(point.y) ? { x: point.x, y: point.y } : null;
  };
  const validSourceAxisWindow = (window, projection, axis = projection) => {
    if (!window || !projection || !axis || !Number.isFinite(window.minimum) || !Number.isFinite(window.maximum)
      || window.minimum >= window.maximum || window.scale && window.scale !== projection.scale
      || axis.scale === 'logarithmic' && !(axis.logarithmicDomain === 'negative' ? window.maximum < 0 : window.minimum > 0)) return false;
    const first = transformedAxisBound(axis, window.minimum, false), last = transformedAxisBound(axis, window.maximum, true);
    return Number.isFinite(first) && Number.isFinite(last) && first !== last;
  };
  const viewportValuePolicy = (root, value) => {
    const policy = value ?? capturedCartesianMetadata(root).valueDomain ?? 'fullData';
    return ['fullData', 'visibleCategories'].includes(policy) ? policy : null;
  };
  const requestCartesianViewport = (root, payload, sync = true, accepted) => {
    const binding = nativePreparationBinding(root, 'viewport');
    if (!cartesianViewportConnected(root) || !Array.isArray(payload.axes) || !payload.axes.length
      || payload.axes.some(name => !chartAxisProjection(root, name))) return false;
    const valueDomain = viewportValuePolicy(root, payload.valueDomain);
    if (!valueDomain) return false;
    if (payload.kind === 'reset') suspendPointerReadout(root);
    return requestNativePreparation(root, 'viewport', { ...payload, ...(valueDomain === 'visibleCategories' ? { valueDomain } : {}) }, {
      eventPrefix: 'cfxviewport', resetView: payload.kind === 'reset', accepted,
      validate: next => {
        if (payload.axes.some(name => !chartAxisProjection(next, name)))
          throw new TypeError('The viewport reply must retain every requested physical axis projection.');
      },
      beforeCommit: () => payload.kind === 'reset' ? selectionSignature(root) : '',
      committed: (_, previousSelection) => {
        if (payload.kind === 'reset') { emitHostEvent(root, 'cfxreset', {}); publishResetSelection(root, previousSelection); }
        if (sync) emitSync(root, { action: 'viewport', state: getState(root), xWindow: nativeXWindow(root), yWindow: nativeYWindow(root), viewport: nativeViewportWindows(root, true) });
      }
    });
  };
  const requestNativeCartesianWindow = (root, viewport, sync = true, accepted) => {
    if (!cartesianViewportConnected(root) || !viewport || typeof viewport !== 'object'
      || Object.keys(viewport).some(name => name !== 'valueDomain' && !cartesianAxisNames.includes(name))) return false;
    const axes = cartesianAxisNames.filter(name => viewport[name]);
    if (!axes.length || axes.some(name => !validSourceAxisWindow(viewport[name], chartAxisProjection(root, name)))) return false;
    const valueDomain = viewportValuePolicy(root, viewport.valueDomain);
    if (!valueDomain) return false;
    const sourceViewport = Object.freeze(Object.fromEntries(axes.map(name => [name,
      Object.freeze({ minimum: viewport[name].minimum, maximum: viewport[name].maximum })])));
    return requestCartesianViewport(root, { kind: 'window', axes: Object.freeze(axes), sourceViewport, valueDomain }, sync, accepted);
  };
  const requestViewportReset = root => {
    if (!cartesianViewportConnected(root)) return requestXViewport(root, 'reset');
    const axes = cartesianAxisNames.filter(name => chartAxisProjection(root, name));
    return axes.length ? requestCartesianViewport(root, { kind: 'reset', axes: Object.freeze(axes), valueDomain: 'fullData' }) : false;
  };
  const requestCartesianViewportZoom = (root, factor, logicalX, logicalY) => {
    const binding = nativePreparationBinding(root, 'viewport'), plot = chartXProjection(root)?.plotBounds;
    if (!plot || !binding || !Number.isFinite(factor) || factor <= 0) return;
    const x = Number.isFinite(logicalX) ? clamp(logicalX, plot.x, plot.x + plot.width) : plot.x + plot.width / 2;
    const y = Number.isFinite(logicalY) ? clamp(logicalY, plot.y, plot.y + plot.height) : plot.y + plot.height / 2;
    const axes = binding.axes;
    requestCartesianViewport(root, { kind: 'zoom', axes,
      ...(axes.some(name => name === 'x' || name === 'secondaryX') ? { left: x - (x - plot.x) / factor, right: x + (plot.x + plot.width - x) / factor } : {}),
      ...(axes.some(name => name === 'y' || name === 'secondaryY') ? { top: y - (y - plot.y) / factor, bottom: y + (plot.y + plot.height - y) / factor } : {})
    });
  };
  const requestNativeViewportPan = (root, deltaX, deltaY) => {
    const plot = chartXProjection(root)?.plotBounds, binding = nativePreparationBinding(root, 'viewport');
    if (!plot || !binding) return;
    if (binding.legacy) { if (Math.abs(deltaX) > .5) requestXViewport(root, 'pan', plot.x - deltaX, plot.x + plot.width - deltaX); return; }
    const axes = binding.axes;
    const horizontal = axes.some(name => name === 'x' || name === 'secondaryX');
    const vertical = axes.some(name => name === 'y' || name === 'secondaryY');
    if (!(horizontal && Math.abs(deltaX) > .5 || vertical && Math.abs(deltaY) > .5)) return;
    requestCartesianViewport(root, { kind: 'pan', axes,
      ...(horizontal ? { left: plot.x - deltaX, right: plot.x + plot.width - deltaX } : {}),
      ...(vertical ? { top: plot.y - deltaY, bottom: plot.y + plot.height - deltaY } : {})
    });
  };
