  // Saved reports re-project these Cartesian families from the scene's data projection: linear or
  // logarithmic axes, an automatic UTC calendar X axis, vertical charts with up to two value axes,
  // and single-axis horizontal charts.
  const standaloneReprojectedKinds = ['line', 'stepline', 'area', 'steparea', 'bar', 'horizontalbar', 'scatter', 'bubble',
    'candlestick', 'ohlc', 'rangeband', 'rangearea', 'boxplot', 'errorbar'];
  const standaloneReprojects = metadata => {
    const projection = metadata.dataProjection, horizontal = metadata.orientation === 'horizontal';
    const axes = ['x', 'y', 'secondaryY'].filter(name => metadata[name + 'Projection'] || projection?.[name]);
    const scale = name => (metadata[name + 'Projection'] || projection[name]).scale;
    return !!projection?.x && !!(projection.y || metadata.yProjection) && !metadata.secondaryXProjection
      && !(horizontal && metadata.secondaryYProjection) && axes.every(name => ['linear', 'logarithmic'].includes(scale(name))
        || scale(name) === 'time' && name === 'x' && !horizontal && projection.x.captions === 'calendar')
      && !!metadata.series?.length && metadata.series.every(row => standaloneReprojectedKinds.includes(row.kind));
  };
  // A saved report zooms only what it can re-project with native-consistent ticks. Matrices, maps,
  // polar, hierarchy, flow and diagram scenes, and Cartesian charts outside the families above
  // (formatted, zoned, session or spaced time axes, symmetric-logarithmic axes, a secondary X axis,
  // stacked, waterfall, lollipop, dumbbell, range-bar and timeline marks) have no window it can
  // redraw faithfully. Scaling their whole
  // drawing would only enlarge text, so zoom and pan decline for them; a connected .NET host
  // re-prepares every family natively instead.
  const standaloneZoomDeclined = root => {
    if (!hasFeature(root, 'Zoom') || nativePreparationBinding(root, 'viewport') || standaloneViewport(root)) return false;
    return !standaloneReprojects(capturedCartesianMetadata(root));
  };
  const syncStandaloneZoomDecline = root => {
    const declined = standaloneZoomDeclined(root), reason = uiText(root, 'ZoomUnavailable', 'Zoom is not available for this chart type');
    if (declined) root.dataset.cfxZoomDeclined = 'true'; else root.removeAttribute('data-cfx-zoom-declined');
    root.querySelectorAll('[data-cfx-zoom],[data-cfx-mode-button="pan"]').forEach(button => {
      if (declined) {
        if (button.dataset.cfxDeclinedTitle === undefined) button.dataset.cfxDeclinedTitle = button.getAttribute('title') || '';
        button.disabled = true; button.title = reason; button.setAttribute('aria-description', reason);
      } else if (button.dataset.cfxDeclinedTitle !== undefined) {
        button.title = button.dataset.cfxDeclinedTitle; button.removeAttribute('aria-description');
        delete button.dataset.cfxDeclinedTitle; button.disabled = false;
      }
    });
    if (declined && root.dataset.cfxMode === 'pan') applyMode(root, '');
    return declined;
  };
