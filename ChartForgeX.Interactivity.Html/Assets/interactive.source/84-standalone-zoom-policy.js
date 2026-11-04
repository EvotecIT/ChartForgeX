  // Saved reports re-project these Cartesian families from the scene's data projection: linear or
  // logarithmic axes, vertical charts with up to two value axes, and single-axis horizontal charts.
  const standaloneReprojectedKinds = ['line', 'stepline', 'area', 'steparea', 'bar', 'horizontalbar', 'scatter', 'bubble',
    'candlestick', 'ohlc', 'rangeband', 'rangearea', 'boxplot', 'errorbar'];
  const standaloneReprojects = metadata => {
    const projection = metadata.dataProjection, horizontal = metadata.orientation === 'horizontal';
    const axes = ['x', 'y', 'secondaryY'].map(name => metadata[name + 'Projection'] || projection?.[name]).filter(Boolean);
    return !!projection?.x && !!(projection.y || metadata.yProjection) && !metadata.secondaryXProjection
      && !(horizontal && metadata.secondaryYProjection) && axes.every(axis => ['linear', 'logarithmic'].includes(axis.scale))
      && !!metadata.series?.length && metadata.series.every(row => standaloneReprojectedKinds.includes(row.kind));
  };
  // Matrices, maps and every chart without a Cartesian data projection (polar, radial, hierarchy,
  // flow and diagram scenes) have no data window to narrow. Scaling their whole drawing would
  // only enlarge text, so a saved report declines zoom and pan for them instead.
  const standaloneDeclinedKinds = ['heatmap', 'hexbinheatmap', 'calendarheatmap', 'dottedmap', 'tilemap', 'regionmap'];
  const standaloneZoomDeclined = root => {
    if (!hasFeature(root, 'Zoom') || nativePreparationBinding(root, 'viewport') || standaloneViewport(root)) return false;
    const metadata = capturedCartesianMetadata(root);
    return !metadata.dataProjection || !!metadata.polarProjection || metadata.series?.some(row => standaloneDeclinedKinds.includes(row.kind)) === true;
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
