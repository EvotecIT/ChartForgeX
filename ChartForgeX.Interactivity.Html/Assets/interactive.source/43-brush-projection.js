  const validDataProjection = projection => {
    const plot = projection?.plotBounds;
    const axis = value => !!value && [value.minimum, value.maximum, value.transformedMinimum, value.transformedMaximum, value.threshold].every(Number.isFinite)
      && value.maximum > value.minimum && value.transformedMaximum > value.transformedMinimum && value.threshold > 0
      && typeof value.reversed === 'boolean' && ['linear', 'time', 'logarithmic', 'symmetriclogarithmic'].includes(value.scale);
    return !!plot && [plot.x, plot.y, plot.width, plot.height].every(Number.isFinite) && plot.width > 0 && plot.height > 0
      && typeof projection.horizontal === 'boolean' && axis(projection.x) && (projection.y === undefined || axis(projection.y));
  };
  const brushPointer = (root, stage, event) => {
    const point = stagePointer(stage, event, true), plot = visibleCartesianPlot(root, stage, root._cfxDataProjection?.plotBounds);
    return plot ? { x: clamp(point.x, plot.left, plot.left + plot.width), y: clamp(point.y, plot.top, plot.top + plot.height) } : point;
  };
  const setBrushBox = (root, stage, brush, drag, point) => {
    const plot = root.dataset.cfxBrushAxis === 'x' && visibleCartesianPlot(root, stage, root._cfxDataProjection?.plotBounds);
    brush.style.left = Math.min(drag.left, point.x) + 'px';
    brush.style.top = (plot ? plot.top : Math.min(drag.top, point.y)) + 'px';
    brush.style.width = Math.abs(point.x - drag.left) + 'px';
    brush.style.height = (plot ? plot.height : Math.abs(point.y - drag.top)) + 'px';
  };
  const projectedAxisValue = (axis, ratio) => {
    if (axis.reversed) ratio = 1 - ratio;
    const span = axis.transformedMaximum - axis.transformedMinimum;
    const value = Number.isFinite(span) ? axis.transformedMinimum + ratio * span
      : axis.transformedMinimum * (1 - ratio) + axis.transformedMaximum * ratio;
    const raw = axis.scale === 'logarithmic' ? Math.pow(10, value)
      : axis.scale === 'symmetriclogarithmic' ? Math.sign(value) * axis.threshold * (Math.pow(10, Math.abs(value)) - 1) : value;
    return raw;
  };
  const brushDataSelection = (root, rect) => {
    const projection = root._cfxDataProjection, svg = root.querySelector('.cfx-stage svg');
    const matrix = svg?.getScreenCTM();
    if (!projection || !matrix || matrix.is2D === false || !matrix.a || !matrix.d) return {};
    let inverse;
    try { inverse = matrix.inverse(); } catch { return {}; }
    const points = [new DOMPoint(rect.left, rect.top), new DOMPoint(rect.right, rect.bottom)].map(point => point.matrixTransform(inverse));
    if (!points.every(point => Number.isFinite(point.x) && Number.isFinite(point.y))) return {};
    const plot = projection.plotBounds;
    const x = points.map(point => clamp((point.x - plot.x) / plot.width, 0, 1));
    const y = points.map(point => clamp(1 - (point.y - plot.y) / plot.height, 0, 1));
    const ordered = (axis, ratios) => {
      const values = ratios.map(ratio => projectedAxisValue(axis, ratio)).sort((left, right) => left - right);
      return values.map(value => dataValue(value, axis.scale === 'time' && axis.minimum >= 0 && values.every(endpoint => endpoint >= 0)));
    };
    const range = { x: ordered(projection.x, x) };
    if (root.dataset.cfxBrushAxis !== 'x' && projection.y) range.y = ordered(projection.y, y);
    const categoryRange = projection.horizontal ? range.y : range.x;
    const data = categoryRange ? { range, categoryRange } : { range };
    let metadata;
    try { metadata = JSON.parse(root.dataset.cfxPreparedChart); } catch { return data; }
    const categories = categoryRange && (metadata.xLabels || []).filter(item => item.value >= categoryRange[0] && item.value <= categoryRange[1]).map(item => item.text);
    return categories?.length ? { ...data, categories } : data;
  };
