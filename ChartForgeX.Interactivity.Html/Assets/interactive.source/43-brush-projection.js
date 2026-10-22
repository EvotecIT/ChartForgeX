  const validTimeSpacing = spacing => spacing === undefined || !!spacing && ['ordinal', 'unit'].includes(spacing.kind)
    && Array.isArray(spacing.instants) && spacing.instants.length >= (spacing.kind === 'unit' ? 2 : 1)
    && spacing.instants.length <= (spacing.kind === 'unit' ? 100001 : 100000)
    && spacing.instants.every((value, index, values) => Number.isFinite(value) && value >= 0
      && (index === 0 || value > values[index - 1]));
  const timeSpacingIndex = (instants, value) => {
    let low = 0, high = instants.length - 1;
    while (low <= high) { const middle = low + Math.floor((high - low) / 2); if (instants[middle] <= value) low = middle + 1; else high = middle - 1; }
    return high;
  };
  const validSourceAxisMaximum = (window, axis = window) => {
    if (window.maximumInclusive !== undefined && typeof window.maximumInclusive !== 'boolean') return false;
    if (window.maximumInclusive !== false || axis.timeSpacing?.kind !== 'unit') return true;
    const values = axis.timeSpacing.instants;
    return Array.isArray(values) && values[timeSpacingIndex(values, window.maximum)] === window.maximum;
  };
  const transformedSpacingValue = (spacing, value, bound, maximumInclusive = true) => {
    const values = spacing.instants, last = values.length - 1, found = timeSpacingIndex(values, value);
    if (spacing.kind === 'unit') {
      if (found < 0) return (value - values[0]) / (values[1] - values[0]);
      if (found >= last) return last + (value - values[last]) / (values[last] - values[last - 1]);
      return found + (bound === undefined ? .5 : bound && (maximumInclusive || value !== values[found]) ? 1 : 0);
    }
    if (last === 0) return value - values[0];
    const index = Math.max(0, Math.min(last - 1, found));
    return index + (value - values[index]) / (values[index + 1] - values[index]);
  };
  const projectedSpacingValue = (spacing, coordinate) => {
    const values = spacing.instants, last = values.length - 1;
    if (spacing.kind === 'unit') {
      if (coordinate <= 0) return values[0] + coordinate * (values[1] - values[0]);
      if (coordinate >= last) return values[last] + (coordinate - last) * (values[last] - values[last - 1]);
      return values[Math.floor(coordinate)];
    }
    if (last === 0) return values[0] + coordinate;
    const index = Math.max(0, Math.min(last - 1, Math.floor(coordinate)));
    return values[index] + (coordinate - index) * (values[index + 1] - values[index]);
  };
  const transformedAxisBound = (axis, value, maximum, maximumInclusive = axis.maximumInclusive !== false) => axis.timeSpacing
    ? transformedSpacingValue(axis.timeSpacing, value, maximum, maximumInclusive) : transformedAxisValue(axis, value);
  const validTimeSessions = sessions => sessions === undefined || Array.isArray(sessions) && sessions.length > 0
    && sessions.every((session, index) => session && [session.minimum, session.maximum, session.offset].every(Number.isFinite)
      && session.minimum >= 0 && session.maximum > session.minimum && session.offset >= 0
      && (index === 0 ? session.offset === 0 : session.minimum >= sessions[index - 1].maximum
        && session.offset === sessions[index - 1].offset + (sessions[index - 1].maximum - sessions[index - 1].minimum)));
  const timeSessionIndex = (sessions, value, field) => {
    let low = 0, high = sessions.length - 1;
    while (low <= high) {
      const middle = low + Math.floor((high - low) / 2);
      if (sessions[middle][field] <= value) low = middle + 1; else high = middle - 1;
    }
    return high;
  };
  const projectedSessionValue = (sessions, value) => {
    const last = sessions.at(-1), total = last.offset + (last.maximum - last.minimum);
    if (value < 0) return sessions[0].minimum + value;
    if (value >= total) return last.maximum + (value - total);
    const index = timeSessionIndex(sessions, value, 'offset');
    return sessions[index].minimum + (value - sessions[index].offset);
  };
  const transformedSessionValue = (sessions, value) => {
    const index = timeSessionIndex(sessions, value, 'minimum');
    if (index < 0) return value - sessions[0].minimum;
    const session = sessions[index];
    if (value > session.maximum) return index < sessions.length - 1 ? sessions[index + 1].offset
      : session.offset + (session.maximum - session.minimum) + (value - session.maximum);
    return session.offset + (value - session.minimum);
  };
  const validDataProjection = projection => {
    const plot = projection?.plotBounds;
    const axis = value => !!value && [value.minimum, value.maximum, value.transformedMinimum, value.transformedMaximum, value.threshold].every(Number.isFinite)
      && value.maximum > value.minimum && value.transformedMaximum > value.transformedMinimum && value.threshold > 0
      && validSourceAxisMaximum(value)
      && typeof value.reversed === 'boolean' && ['linear', 'time', 'logarithmic', 'symmetriclogarithmic'].includes(value.scale)
      && Number.isFinite(value.logarithmBase ?? 10) && (value.logarithmBase ?? 10) > 1
      && ['positive', 'negative'].includes(value.logarithmicDomain ?? 'positive')
      && validTimeSessions(value.timeSessions) && validTimeSpacing(value.timeSpacing)
      && (!(value.timeSessions || value.timeSpacing) || value.scale === 'time') && !(value.timeSessions && value.timeSpacing)
      && (value.displayTicks === undefined || Array.isArray(value.displayTicks) && value.displayTicks.every(tick =>
        tick && Number.isFinite(tick.value) && typeof tick.text === 'string' && (tick.instant === undefined || typeof tick.instant === 'string')))
      && (value.displayBounds === undefined || Array.isArray(value.displayBounds) && value.displayBounds.every(tick =>
        tick && Number.isFinite(tick.value) && typeof tick.text === 'string' && (tick.instant === undefined || typeof tick.instant === 'string')));
    return !!plot && [plot.x, plot.y, plot.width, plot.height].every(Number.isFinite) && plot.width > 0 && plot.height > 0
      && typeof projection.horizontal === 'boolean' && (projection.x !== undefined || projection.y !== undefined)
      && (projection.x === undefined || axis(projection.x)) && (projection.y === undefined || axis(projection.y));
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
  const projectedAxisValue = (axis, ratio, bound) => {
    if (axis.reversed) ratio = 1 - ratio;
    const span = axis.transformedMaximum - axis.transformedMinimum;
    const value = Number.isFinite(span) ? axis.transformedMinimum + ratio * span
      : axis.transformedMinimum * (1 - ratio) + axis.transformedMaximum * ratio;
    const base = axis.logarithmBase ?? 10;
    const raw = axis.timeSpacing ? projectedSpacingValue(axis.timeSpacing, axis.timeSpacing.kind === 'unit' && bound !== undefined ? bound ? Math.ceil(value) : Math.floor(value) : value) : axis.timeSessions ? projectedSessionValue(axis.timeSessions, value) : axis.scale === 'logarithmic'
      ? axis.logarithmicDomain === 'negative' ? -Math.pow(base, -value) : Math.pow(base, value)
      : axis.scale === 'symmetriclogarithmic' ? Math.sign(value) * axis.threshold * (Math.pow(base, Math.abs(value)) - 1) : value;
    return clamp(raw, axis.minimum, axis.maximum);
  };
  const axisLogarithm = (axis, value) => (axis.logarithmBase ?? 10) === 10 ? Math.log10(value) : Math.log(value) / Math.log(axis.logarithmBase);
  const transformedAxisValue = (axis, value) => axis.timeSpacing ? transformedSpacingValue(axis.timeSpacing, value) : axis.timeSessions ? transformedSessionValue(axis.timeSessions, value)
    : axis.scale === 'logarithmic' ? axis.logarithmicDomain === 'negative' ? -axisLogarithm(axis, -value) : axisLogarithm(axis, value)
    : axis.scale === 'symmetriclogarithmic' ? Math.sign(value) * axisLogarithm(axis, 1 + Math.abs(value) / axis.threshold) : value;
  const enclosingAxisTicks = (axis, values) => {
    const ticks = [...(axis.displayTicks || []), ...(axis.displayBounds || [])]
      .filter(tick => tick.value >= axis.minimum && tick.value <= axis.maximum);
    const lower = ticks.filter(tick => tick.value <= values[0]).sort((a, b) => b.value - a.value)[0];
    const upper = ticks.filter(tick => tick.value >= values[1]).sort((a, b) => a.value - b.value)[0];
    return lower && upper ? [lower, upper] : undefined;
  };
  const projectedAxisRatio = (axis, value, bound, maximumInclusive) => {
    const transformed = bound === undefined ? transformedAxisValue(axis, value) : transformedAxisBound(axis, value, bound, maximumInclusive);
    const span = axis.transformedMaximum - axis.transformedMinimum;
    const ratio = Number.isFinite(span) ? (transformed - axis.transformedMinimum) / span
      : (transformed / 2 - axis.transformedMinimum / 2) / (axis.transformedMaximum / 2 - axis.transformedMinimum / 2);
    return axis.reversed ? 1 - ratio : ratio;
  };
  const projectBrushSelection = (root, rect) => {
    const projection = root._cfxDataProjection, svg = root.querySelector('.cfx-stage svg');
    const matrix = svgScreenMatrix(svg);
    if (!projection || !matrix || matrix.is2D === false || !matrix.a || !matrix.d) return { data: {} };
    let inverse;
    try { inverse = matrix.inverse(); } catch { return { data: {} }; }
    const points = [new DOMPoint(rect.left, rect.top), new DOMPoint(rect.right, rect.bottom)].map(point => point.matrixTransform(inverse));
    if (!points.every(point => Number.isFinite(point.x) && Number.isFinite(point.y))) return { data: {} };
    const plot = projection.plotBounds;
    const x = points.map(point => clamp((point.x - plot.x) / plot.width, 0, 1));
    const y = points.map(point => clamp(1 - (point.y - plot.y) / plot.height, 0, 1));
    let metadata;
    try { metadata = JSON.parse(root.dataset.cfxPreparedChart); } catch { metadata = {}; }
    const categoryDirection = projection.horizontal ? 'y' : 'x';
    const categoryAxis = projection[categoryDirection];
    const categoryLabels = categoryAxis?.scale === 'linear' && Array.isArray(metadata.categoryLabels) ? metadata.categoryLabels : undefined;
    const selectedAxes = [...(projection.x ? [['x', projection.x, x]] : []),
      ...(root.dataset.cfxBrushAxis !== 'x' && projection.y ? [['y', projection.y, y]] : [])];
    const range = {}, displayRange = {}, ratios = { x, y }, captions = {};
    for (const [direction, axis, endpoints] of selectedAxes) {
      const raw = endpoints.map(ratio => projectedAxisValue(axis, ratio)).sort((a, b) => a - b);
      const ticks = direction === categoryDirection && categoryLabels ? undefined : enclosingAxisTicks(axis, raw);
      const values = ticks ? ticks.map(tick => tick.value) : raw;
      const orderedTime = axis.scale === 'time' && axis.minimum >= 0 && values.every(endpoint => endpoint >= 0);
      range[direction] = values.map((value, index) => orderedTime && ticks?.[index].instant !== undefined
        ? ticks[index].instant : dataValue(value, orderedTime));
      if (ticks) {
        displayRange[direction] = range[direction];
        captions[direction] = ticks.every(tick => tick.text === '') ? ''
          : ticks[0].value === ticks[1].value ? ticks[0].text : ticks.map(tick => tick.text).join(' – ');
        ratios[direction] = values.map(value => projectedAxisRatio(axis, value));
      }
    }
    const categoryRange = range[categoryDirection];
    const data = categoryRange ? { range, categoryRange } : { range };
    if (categoryLabels && categoryRange) {
      const included = categoryLabels.filter(item => item.value >= categoryRange[0] && item.value <= categoryRange[1]).sort((a, b) => a.value - b.value);
      data.categories = included.map(item => item.text);
      captions[categoryDirection] = data.categories.join(', ');
      if (included.length) displayRange[categoryDirection] = [included[0].value, included[included.length - 1].value];
    }
    if (Object.keys(displayRange).length) data.displayRange = displayRange;
    const caption = captions[categoryDirection] ?? captions.x;
    if (caption !== undefined) data.label = data.rangeLabel = caption;
    const xs = ratios.x.map(ratio => plot.x + ratio * plot.width);
    const ys = ratios.y.map(ratio => plot.y + (1 - ratio) * plot.height);
    const bounds = [new DOMPoint(Math.min(...xs), Math.min(...ys)), new DOMPoint(Math.max(...xs), Math.max(...ys))]
      .map(point => point.matrixTransform(matrix));
    return { data, bounds };
  };
  const finishBrushSelection = (root, stage, brush) => {
    const selection = projectBrushSelection(root, brush.getBoundingClientRect());
    if (selection.bounds) {
      const corners = selection.bounds.map(point => stagePoint(stage, point));
      brush.style.left = Math.min(...corners.map(point => point.x)) + 'px';
      brush.style.top = Math.min(...corners.map(point => point.y)) + 'px';
      brush.style.width = Math.abs(corners[1].x - corners[0].x) + 'px';
      brush.style.height = Math.abs(corners[1].y - corners[0].y) + 'px';
    }
    return selection.data;
  };
