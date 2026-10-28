  const polarAxisNames = Object.freeze(['angle', 'radius', 'secondaryRadius']);
  const nativePolarAxes = axes => nativePreparationAxes(axes, polarAxisNames, ['angle', 'radius']);
  const nativePolarGestures = (options, polar = true) => {
    if (options.gestures !== undefined && (typeof options.gestures !== 'boolean' || !polar))
      throw new TypeError('gestures: boolean on Polar only.');
    return options.gestures === true;
  };
  const capturedPolarProjection = root => {
    const projection = capturedCartesianMetadata(root).polarProjection;
    return validPolarProjection(projection) ? projection : null;
  };
  const validPolarAngle = window => !!window && Number.isFinite(window.minimum) && Number.isFinite(window.maximum)
    && window.minimum < window.maximum && window.maximum - window.minimum <= Math.PI * 2 && (window.maximumInclusive === undefined || typeof window.maximumInclusive === 'boolean');
  const validPolarRadius = (axis, outer) => validXProjection(axis)
    && Number.isFinite(axis.innerRadius)
    && axis.innerRadius >= 0 && axis.innerRadius < axis.outerRadius && axis.outerRadius === outer;
  const validPolarProjection = projection => !!projection && validPolarAngle(projection.angle)
    && [projection.centerX, projection.centerY, projection.outerRadius, projection.rotationRadians].every(Number.isFinite)
    && projection.outerRadius > 0 && typeof projection.angleReversed === 'boolean'
    && (projection.radius || projection.secondaryRadius)
    && (!projection.radius || validPolarRadius(projection.radius, projection.outerRadius))
    && (!projection.secondaryRadius || validPolarRadius(projection.secondaryRadius, projection.outerRadius));
  const polarViewportConnected = root => nativePreparationBinding(root, 'viewport')?.coordinateSystem === 'polar';
  const polarAxisAvailable = (root, name) => {
    const projection = capturedPolarProjection(root);
    return !!projection && (name === 'angle' || !!projection[name]);
  };
  const nativePolarViewport = root => {
    const projection = capturedPolarProjection(root);
    if (!projection) return null;
    return Object.fromEntries(polarAxisNames.filter(name => projection[name]).map(name => [name, nativeProjectionWindow(projection[name])]));
  };
  const nativePolarViewportChanged = root => {
    const projection = capturedPolarProjection(root);
    return !!projection && (projection.angle.maximum - projection.angle.minimum !== Math.PI * 2
      || ['radius', 'secondaryRadius'].some(name => nativeAxisChanged(projection[name])));
  };

  const connectPolarViewport = (root, connection, options = {}) => {
    if (connection !== null && typeof connection !== 'function') throw new TypeError('Native callback: function|null required.');
    const axes = nativePolarAxes(options.axes), gestures = nativePolarGestures(options);
    if (connection && axes.some(name => !polarAxisAvailable(root, name))) throw new TypeError('Polar projection unavailable.');
    setNativePreparationConnection(root, 'viewport', connection, { axes, legacy: false, coordinateSystem: 'polar', gestures });
    if (connection) applyViewport(root, { zoom: 1, panX: 0, panY: 0 });
    syncXViewportControls(root);
  };
  const polarContract = root => {
    const metadata = capturedCartesianMetadata(root), p = metadata.polarProjection;
    return JSON.stringify([metadata.series, p.rotationRadians, p.angleReversed,
      ...['radius', 'secondaryRadius'].map(name => p[name] && ['scale', 'fullMinimum', 'fullMaximum', 'transformedFullMinimum',
        'transformedFullMaximum', 'reversed', 'threshold', 'logarithmBase', 'logarithmicDomain', 'timeSessions', 'timeSpacing'].map(key => p[name][key])),
      Array.from(root.querySelectorAll('[data-cfx-role="polar-point-source"]'), node =>
        ['series', 'point', 'source-id', 'observation-id', 'angle', 'value', 'break-before', 'axis'].map(name => node.getAttribute('data-cfx-' + name)))]);
  };
  const requestPolarViewport = (root, payload, sync = true, accepted) => {
    const original = capturedPolarProjection(root);
    if (!polarViewportConnected(root) || !original || payload.axes.some(name => !original[name])) return false;
    const contract = polarContract(root);
    if (payload.kind === 'reset') suspendPointerReadout(root);
    return requestNativePreparation(root, 'viewport', { ...payload, coordinateSystem: 'polar' }, {
      eventPrefix: 'cfxviewport', resetView: payload.kind === 'reset', accepted,
      validate: next => {
        const projection = capturedPolarProjection(next);
        if (!projection || payload.axes.some(name => !projection[name])
          || polarContract(next) !== contract)
          throw new TypeError('Polar reply: source/domain/projection changed.');
        for (const name of polarAxisNames) {
          const expected = payload.kind === 'window' && payload.sourceViewport[name]
            || !payload.axes.includes(name) && original[name], actual = projection[name];
          if (expected && (expected.minimum !== actual?.minimum || expected.maximum !== actual.maximum
            || (expected.maximumInclusive !== false) !== (actual.maximumInclusive !== false)))
            throw new TypeError('Polar reply: ' + name + ' window mismatch.');
        }
        if (payload.kind === 'reset' && nativePolarViewportChanged(next)) throw new TypeError('Polar reset: full domains.');
      },
      beforeCommit: () => payload.kind === 'reset' ? selectionSignature(root) : '',
      committed: (_, previousSelection) => {
        if (payload.kind === 'reset') { emitHostEvent(root, 'cfxreset', {}); publishResetSelection(root, previousSelection); }
        if (sync) emitSync(root, { action: 'viewport', polarViewport: nativePolarViewport(root) });
      }
    });
  };
  const requestNativePolarWindow = (root, viewport, sync = true, accepted) => {
    const projection = capturedPolarProjection(root);
    if (!polarViewportConnected(root) || !viewport || typeof viewport !== 'object' || Array.isArray(viewport)
      || Object.keys(viewport).some(name => !polarAxisNames.includes(name))) return false;
    const axes = polarAxisNames.filter(name => viewport[name]);
    if (!axes.length || axes.some(name => !polarAxisAvailable(root, name)
      || (name === 'angle' ? !validPolarAngle(viewport[name]) : !validSourceAxisWindow(viewport[name], projection[name])
        || viewport[name].minimum < projection[name].fullMinimum || viewport[name].maximum > projection[name].fullMaximum))) return false;
    const sourceViewport = Object.freeze(Object.fromEntries(axes.map(name => [name, nativeSourceWindow(viewport[name])])));
    return requestPolarViewport(root, { kind: 'window', axes: Object.freeze(axes), sourceViewport }, sync, accepted);
  };
  const requestPolarViewportReset = root => {
    const projection = capturedPolarProjection(root);
    return projection ? requestPolarViewport(root, { kind: 'reset', axes: Object.freeze(polarAxisNames.filter(name => projection[name])) }) : false;
  };
