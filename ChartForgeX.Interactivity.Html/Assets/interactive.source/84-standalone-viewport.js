  // Saved reports reuse the detached projection and original vector geometry. Only
  // data ink moves: the frame, captions, marker sizes and stroke widths stay sharp.
  const standaloneStyle = (node, name) => ({ value: node.style.getPropertyValue(name), priority: node.style.getPropertyPriority(name) });
  const restoreStandaloneStyle = (node, name, style) => {
    if (style.value) node.style.setProperty(name, style.value, style.priority);
    else node.style.removeProperty(name);
  };
  const standaloneViewport = root => {
    const lifetime = chartLifetimes.get(root);
    if (!lifetime || lifetime.disposed || nativePreparationBinding(root, 'viewport')) return null;
    const svg = root.querySelector('.cfx-stage svg'), metadata = capturedCartesianMetadata(root);
    if (!svg || !metadata.dataProjection?.x || !metadata.dataProjection?.y || metadata.orientation === 'horizontal'
      || !metadata.series?.length || metadata.series.some(row => !['line', 'stepline', 'area', 'steparea', 'bar', 'scatter', 'candlestick', 'ohlc'].includes(row.kind))) return null;
    if (root._cfxStandaloneViewport?.svg === svg) return root._cfxStandaloneViewport;
    const view = { svg, source: copyHostValue(metadata), windows: {}, geometry: [], labels: [], axes: [], points: [], frames: [] };
    const plot = metadata.dataProjection.plotBounds;
    const ns = 'http://www.w3.org/2000/svg';
    const clip = document.createElementNS(ns, 'clipPath'), rect = document.createElementNS(ns, 'rect');
    clip.id = (svg.querySelector('[id]')?.id || 'cfx') + '-browser-plot';
    for (const [key, value] of Object.entries(plot)) rect.setAttribute(key, value);
    clip.appendChild(rect); svg.querySelector('defs')?.appendChild(clip);
    if (!clip.parentNode) { const defs = document.createElementNS(ns, 'defs'); defs.appendChild(clip); svg.prepend(defs); view.defs = defs; }
    view.clip = clip;
    const series = svg.querySelectorAll('[data-cfx-role="series"]');
    series.forEach(owner => {
      // An outer clip remains in plot coordinates while individual marks reproject.
      const frame = document.createElementNS(ns, 'g');
      frame.setAttribute('clip-path', 'url(#' + clip.id + ')');
      owner.before(frame); frame.appendChild(owner);
      view.frames.push(frame);
      owner.querySelectorAll('path,line,rect,circle,ellipse,polygon,polyline,image').forEach(node => {
        if (node.closest('defs,[data-cfx-native-text-ink]') || node.closest('[data-cfx-caption]') || node.closest('g')?.querySelector('text') && node.closest('[data-cfx-role$="label"]')) return;
        const point = node.closest('[data-cfx-point]');
        const marker = !node.hasAttribute('data-cfx-browser-hit-area') && ((node.dataset.cfxRole || '').startsWith('marker')
          || point && metadata.series[Number(point.dataset.cfxSeries)]?.kind === 'scatter');
        const box = marker ? node.getBBox() : null;
        view.geometry.push({ node, attrs: Object.fromEntries(Array.from(node.attributes).map(attr => [attr.name, attr.value])),
          visibility: standaloneStyle(node, 'visibility'),
          marker: box ? { x: box.x + box.width / 2, y: box.y + box.height / 2 } : null,
          axis: node.closest('[data-cfx-axis]')?.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y' });
      });
    });
    svg.querySelectorAll('[data-cfx-label-for]').forEach(node => {
      const box = node.getBBox();
      view.labels.push({ node, x: box.x + box.width / 2, y: box.y + box.height / 2, transform: node.getAttribute('transform'), visibility: standaloneStyle(node, 'visibility') });
      if (!node.closest('[data-cfx-role="series"]')) {
        const frame = document.createElementNS(ns, 'g'); frame.setAttribute('clip-path', 'url(#' + clip.id + ')');
        node.before(frame); frame.appendChild(node);
        view.frames.push(frame);
      }
    });
    svg.querySelectorAll('[data-cfx-point][data-cfx-x]').forEach(node => {
      if (node.closest('[data-cfx-role="legend-item"]')) return;
      view.points.push({ node, region: node._cfxNativeRegion, interiors: node._cfxFillInteriors, x: Number(node.dataset.cfxX), y: Number(node.dataset.cfxY),
        visibility: standaloneStyle(node, 'visibility'),
        series: Number(node.dataset.cfxSeries), axis: node.dataset.cfxAxis === 'secondary' ? 'secondaryY' : 'y',
        visible: node.getAttribute('data-cfx-visible') });
    });
    for (const name of ['x', 'y', 'secondaryY']) {
      const role = name === 'secondaryY' ? 'secondary-y' : name;
      const nodes = Array.from(svg.querySelectorAll('[data-cfx-role="axis-' + role + '-label"],[data-cfx-role="grid-' + role + '"]'));
      view.axes.push({ name, nodes, display: new Map(nodes.map(node => [node, standaloneStyle(node, 'display')])), layer: null });
    }
    root._cfxStandaloneViewport = view;
    return view;
  };
  const standaloneAxis = (root, name) => {
    const view = standaloneViewport(root);
    if (!view) return null;
    const captured = view.source[name + 'Projection'] || view.source.dataProjection[name];
    const source = captured && { ...view.source.dataProjection[name], ...captured };
    if (!source) return null;
    const window = view.windows[name];
    return window ? { ...source, ...window, transformedMinimum: transformedAxisBound(source, window.minimum, false),
      transformedMaximum: transformedAxisBound(source, window.maximum, true, window.maximumInclusive !== false) } : source;
  };
  const standaloneCoordinate = (axis, value, plot, vertical) => vertical
    ? plot.y + (1 - projectedAxisRatio(axis, value)) * plot.height : plot.x + projectedAxisRatio(axis, value) * plot.width;
  // Brush readouts clamp to their current window. Navigation must extrapolate
  // beyond it before clipping the requested window to the captured full domain.
  const standaloneAxisValue = (axis, ratio, bound) => projectedAxisValue({ ...axis, minimum: -Infinity, maximum: Infinity }, ratio, bound);
  const standaloneMap = (source, target, plot, vertical) => {
    const first = vertical ? plot.y : plot.x, length = vertical ? plot.height : plot.width;
    const map = value => standaloneCoordinate(target, standaloneAxisValue(source, vertical ? 1 - (value - first) / length : (value - first) / length), plot, vertical);
    return { at: map, scale: (map(first + length) - map(first)) / length };
  };
  const standalonePath = (path, x, y) => {
    // The native SVG writer emits absolute M/L/Q/C/A/Z commands. Preserve curve
    // control points and arc flags instead of flattening curves into polylines.
    const tokens = path.match(/[a-zA-Z]|[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?/g) || [];
    const arity = { M: 2, L: 2, Q: 4, C: 6, A: 7, H: 1, V: 1, Z: 0 };
    let command = '', position = 0, output = [];
    while (position < tokens.length) {
      if (/^[a-zA-Z]$/.test(tokens[position])) { command = tokens[position++]; output.push(command); if (command === 'Z') continue; }
      const size = arity[command];
      if (!size || position + size > tokens.length) return path;
      const values = tokens.slice(position, position + size).map(Number); position += size;
      if (command === 'A') { values[0] *= Math.abs(x.scale); values[1] *= Math.abs(y.scale); values[5] = x.at(values[5]); values[6] = y.at(values[6]); }
      else if (command === 'H') values[0] = x.at(values[0]);
      else if (command === 'V') values[0] = y.at(values[0]);
      else for (let i = 0; i < values.length; i++) values[i] = (i % 2 ? y : x).at(values[i]);
      output.push(values.map(value => Number(value.toFixed(4))).join(' '));
    }
    return output.join(' ');
  };
  const paintStandaloneGeometry = (entry, x, y) => {
    const { node, attrs } = entry;
    if (entry.marker) {
      const dx = x.at(entry.marker.x) - entry.marker.x, dy = y.at(entry.marker.y) - entry.marker.y;
      x = { at: value => value + dx, scale: 1 }; y = { at: value => value + dy, scale: 1 };
    }
    const set = (name, value) => node.setAttribute(name, Number(value.toFixed(4)));
    if (attrs.d) node.setAttribute('d', standalonePath(attrs.d, x, y));
    if (attrs.points) {
      const points = attrs.points.trim().split(/[\s,]+/).map(Number);
      node.setAttribute('points', points.map((value, index) => (index % 2 ? y : x).at(value)).join(' '));
    }
    for (const name of ['x', 'x1', 'x2', 'cx']) if (attrs[name] !== undefined) set(name, x.at(Number(attrs[name])));
    for (const name of ['y', 'y1', 'y2', 'cy']) if (attrs[name] !== undefined) set(name, y.at(Number(attrs[name])));
    for (const [name, map] of [['width', x], ['height', y]]) if (attrs[name] !== undefined) set(name, Number(attrs[name]) * Math.abs(map.scale));
  };
  const standaloneTicks = (axis, count, categories) => {
    if (categories) {
      const values = categories.filter(tick => tick.value >= axis.minimum && tick.value <= axis.maximum);
      const stride = Math.max(1, Math.ceil(values.length / count));
      return values.filter((_, index) => index % stride === 0);
    }
    const span = axis.transformedMaximum - axis.transformedMinimum, rough = span / Math.max(2, count);
    const magnitude = Math.pow(10, Math.floor(Math.log10(rough))), normal = rough / magnitude;
    const step = (normal <= 1 ? 1 : normal <= 2 ? 2 : normal <= 5 ? 5 : 10) * magnitude;
    const values = [];
    for (let index = Math.ceil(axis.transformedMinimum / step); index * step <= axis.transformedMaximum + step * 1e-8 && values.length < 40; index++) {
      const value = projectedAxisValue({ ...axis, reversed: false }, (index * step - axis.transformedMinimum) / span);
      const captured = [...(axis.displayTicks || []), ...(axis.displayBounds || [])].find(tick => Math.abs(tick.value - value) < Math.abs(value) * 1e-12);
      const text = captured?.text ?? (axis.scale === 'time' ? String(dataValue(value, value >= 0))
        : new Intl.NumberFormat(undefined, { maximumSignificantDigits: 7 }).format(Math.abs(value) < step * 1e-10 ? 0 : value));
      values.push({ value, text });
    }
    return values;
  };
  const paintStandaloneAxes = (root, view) => {
    const plot = view.source.dataProjection.plotBounds, ns = view.svg.namespaceURI;
    for (const axis of view.axes) {
      const projection = standaloneAxis(root, axis.name);
      if (!projection || !axis.nodes.length) continue;
      if (!view.windows[axis.name]) {
        axis.nodes.forEach(node => restoreStandaloneStyle(node, 'display', axis.display.get(node))); axis.layer?.remove(); axis.layer = null; continue;
      }
      axis.nodes.forEach(node => node.style.display = 'none');
      axis.layer?.remove();
      const layer = document.createElementNS(ns, 'g'); axis.layer = layer;
      layer.dataset.cfxBrowserAxis = axis.name; layer.setAttribute('pointer-events', 'none'); view.svg.appendChild(layer);
      const vertical = axis.name !== 'x', secondary = axis.name === 'secondaryY';
      const template = axis.nodes.find(node => node.querySelector('text'))?.querySelector('text');
      const box = template?.getBBox(), style = template && getComputedStyle(template);
      const size = Number(template?.getAttribute('font-size')) || 12;
      const ticks = standaloneTicks(projection, Math.max(2, Math.floor((vertical ? plot.height : plot.width) / (vertical ? size * 3 : 90))),
        !vertical && view.source.categoryLabels?.length ? view.source.categoryLabels : null);
      for (const tick of ticks) {
        const position = standaloneCoordinate(projection, tick.value, plot, vertical);
        const text = template ? template.cloneNode(false) : document.createElementNS(ns, 'text');
        text.removeAttribute('display'); text.removeAttribute('transform'); text.removeAttribute('data-cfx-owned-font-text'); text.removeAttribute('data-cfx-png-glyphs');
        text.setAttribute('font-size', size); text.setAttribute('fill', style?.fill || 'currentColor');
        text.setAttribute('x', vertical ? secondary ? plot.x + plot.width + 8 : plot.x - 8 : position);
        text.setAttribute('y', vertical ? position : plot.y + plot.height + 8 + (box?.height || size));
        text.setAttribute('text-anchor', vertical ? secondary ? 'start' : 'end' : 'middle');
        text.setAttribute('dominant-baseline', vertical ? 'middle' : 'auto'); text.textContent = tick.text;
        text.dataset.cfxAxisValue = tick.value; layer.appendChild(text);
        const gridTemplate = axis.nodes.find(node => node.dataset.cfxRole?.startsWith('grid-'));
        if (gridTemplate) {
          const line = gridTemplate.cloneNode(false);
          line.style.removeProperty('display'); line.removeAttribute('id');
          for (const [key, value] of Object.entries(vertical ? { x1: plot.x, x2: plot.x + plot.width, y1: position, y2: position }
            : { x1: position, x2: position, y1: plot.y, y2: plot.y + plot.height })) line.setAttribute(key, value);
          layer.prepend(line);
        }
      }
    }
  };
  const paintStandaloneViewport = root => {
    const view = standaloneViewport(root); if (!view) return false;
    const plot = view.source.dataProjection.plotBounds;
    const x = standaloneMap(view.source.dataProjection.x, standaloneAxis(root, 'x'), plot, false);
    const maps = {};
    for (const name of ['y', 'secondaryY']) {
      const source = view.source[name + 'Projection'] || view.source.dataProjection[name];
      if (source) maps[name] = standaloneMap(source, standaloneAxis(root, name), plot, true);
    }
    view.geometry.forEach(entry => paintStandaloneGeometry(entry, x, maps[entry.axis] || maps.y));
    view.labels.forEach(entry => {
      entry.node.setAttribute('transform', 'translate(' + (x.at(entry.x) - entry.x) + ' ' + (maps.y.at(entry.y) - entry.y) + ') ' + (entry.transform || ''));
      entry.node.style.visibility = entry.node.matches('.cfx-series-muted,.cfx-series-isolated-out') ? 'hidden' : '';
    });
    for (const point of view.points) {
      const axis = standaloneAxis(root, point.axis), y = maps[point.axis] || maps.y;
      const px = standaloneCoordinate(standaloneAxis(root, 'x'), point.x, plot, false), py = standaloneCoordinate(axis, point.y, plot, true);
      if (point.region) point.node._cfxNativeRegion = Object.freeze({ x: x.at(point.region.x), y: y.at(point.region.y),
        width: point.region.width * Math.abs(x.scale), height: point.region.height * Math.abs(y.scale) });
      const bounds = point.node._cfxNativeRegion || { x: px, y: py, width: 0, height: 0 };
      point.node.dataset.cfxVisible = String(point.visible !== 'false' && bounds.x + bounds.width >= plot.x - .01 && bounds.x <= plot.x + plot.width + .01
        && bounds.y + bounds.height >= plot.y - .01 && bounds.y <= plot.y + plot.height + .01);
      if (point.interiors) point.node._cfxFillInteriors = Object.freeze(point.interiors.map(([px, py, radius]) =>
        Object.freeze([x.at(px), y.at(py), radius * Math.min(Math.abs(x.scale), Math.abs(y.scale))])));
    }
    const metadata = copyHostValue(view.source);
    for (const name of ['x', 'y', 'secondaryY']) {
      const axis = standaloneAxis(root, name); if (!axis) continue;
      metadata[name + 'Projection'] = axis;
      if (metadata.dataProjection[name]) metadata.dataProjection[name] = axis;
    }
    root.dataset.cfxPreparedChart = JSON.stringify(metadata); root._cfxDataProjection = metadata.dataProjection;
    paintStandaloneAxes(root, view); syncXNavigation(root); syncResetControl(root); refreshKeyboardNavigation(root);
    hideCrosshair(root, root.querySelector('.cfx-crosshair')); refreshTooltipPosition(root);
    return true;
  };
  const requestStandaloneWindow = (root, windows, sync = true, accepted) => {
    const view = standaloneViewport(root); if (!view || !windows || typeof windows !== 'object') return false;
    const names = Object.keys(windows);
    if (!names.length || names.some(name => !['x', 'y', 'secondaryY'].includes(name) || !validSourceAxisWindow(windows[name], standaloneAxis(root, name)))) return false;
    for (const name of names) {
      const axis = standaloneAxis(root, name), requested = windows[name];
      const fullMin = axis.fullMinimum ?? axis.minimum, fullMax = axis.fullMaximum ?? axis.maximum;
      const minimum = Math.max(fullMin, requested.minimum), maximum = Math.min(fullMax, requested.maximum);
      if (!(minimum < maximum)) return false;
    }
    for (const name of names) {
      const axis = standaloneAxis(root, name), requested = windows[name];
      view.windows[name] = { minimum: Math.max(axis.fullMinimum ?? axis.minimum, requested.minimum), maximum: Math.min(axis.fullMaximum ?? axis.maximum, requested.maximum) };
      const source = view.source[name + 'Projection'] || view.source.dataProjection[name];
      if (view.windows[name].minimum === source.minimum && view.windows[name].maximum === source.maximum) delete view.windows[name];
    }
    paintStandaloneViewport(root); accepted?.();
    emitHostEvent(root, 'cfxviewport', { viewport: nativeViewportWindows(root), standalone: true });
    if (sync) emitSync(root, { action: 'viewport', state: getState(root), viewport: nativeViewportWindows(root, true), xWindow: nativeXWindow(root), yWindow: nativeYWindow(root) });
    return true;
  };
  const zoomStandaloneViewport = (root, factor, logicalX, logicalY, axes = ['x']) => {
    const view = standaloneViewport(root); if (!view || !Number.isFinite(factor) || factor <= 0) return false;
    const plot = view.source.dataProjection.plotBounds, windows = {};
    for (const name of axes) {
      const axis = standaloneAxis(root, name); if (!axis) continue;
      const vertical = name !== 'x', value = vertical ? logicalY : logicalX;
      const first = vertical ? plot.y : plot.x, size = vertical ? plot.height : plot.width;
      const ratio = Number.isFinite(value) ? clamp((value - first) / size, 0, 1) : .5;
      const a = standaloneAxisValue(axis, vertical ? 1 - ratio - (1 - ratio) / factor : ratio - ratio / factor, false);
      const b = standaloneAxisValue(axis, vertical ? 1 - ratio + ratio / factor : ratio + (1 - ratio) / factor, true);
      windows[name] = { minimum: Math.min(a, b), maximum: Math.max(a, b) };
    }
    return requestStandaloneWindow(root, windows);
  };
  const panStandaloneViewport = (root, deltaX, deltaY) => {
    const view = standaloneViewport(root); if (!view) return false;
    const plot = view.source.dataProjection.plotBounds, windows = {};
    for (const [name, delta, size] of [['x', deltaX, plot.width], ['y', -deltaY, plot.height]]) {
      const axis = standaloneAxis(root, name), shift = delta / size * (axis.reversed ? -1 : 1);
      const lo = transformedAxisBound(axis, axis.fullMinimum ?? axis.minimum, false), hi = transformedAxisBound(axis, axis.fullMaximum ?? axis.maximum, true);
      const span = axis.transformedMaximum - axis.transformedMinimum;
      const start = clamp(axis.transformedMinimum - shift * span, lo, hi - span);
      const full = { ...axis, transformedMinimum: lo, transformedMaximum: hi, reversed: false };
      windows[name] = { minimum: standaloneAxisValue(full, (start - lo) / (hi - lo), false), maximum: standaloneAxisValue(full, (start + span - lo) / (hi - lo), true) };
    }
    return requestStandaloneWindow(root, windows);
  };
  const resetStandaloneViewport = root => {
    if (nativePreparationBinding(root, 'viewport')) return false;
    const view = root._cfxStandaloneViewport; if (!view) return false;
    view.windows = {}; paintStandaloneViewport(root); return true;
  };
  const releaseStandaloneViewport = root => {
    const view = root._cfxStandaloneViewport; if (!view) return;
    for (const entry of view.geometry) {
      for (const name of ['d', 'points', 'x', 'x1', 'x2', 'cx', 'y', 'y1', 'y2', 'cy', 'width', 'height'])
        if (entry.attrs[name] !== undefined) entry.node.setAttribute(name, entry.attrs[name]);
      restoreStandaloneStyle(entry.node, 'visibility', entry.visibility);
    }
    for (const entry of view.labels) {
      if (entry.transform === null) entry.node.removeAttribute('transform'); else entry.node.setAttribute('transform', entry.transform);
      restoreStandaloneStyle(entry.node, 'visibility', entry.visibility);
    }
    for (const point of view.points) {
      if (point.visible === null) point.node.removeAttribute('data-cfx-visible'); else point.node.setAttribute('data-cfx-visible', point.visible);
      point.node._cfxNativeRegion = point.region; point.node._cfxFillInteriors = point.interiors;
      restoreStandaloneStyle(point.node, 'visibility', point.visibility);
    }
    for (const axis of view.axes) {
      axis.layer?.remove(); axis.nodes.forEach(node => restoreStandaloneStyle(node, 'display', axis.display.get(node)));
    }
    view.frames.forEach(frame => frame.replaceWith(...frame.childNodes)); view.clip.remove();
    if (view.defs && !view.defs.hasChildNodes()) view.defs.remove();
    root.dataset.cfxPreparedChart = JSON.stringify(view.source); root._cfxDataProjection = view.source.dataProjection;
    delete root._cfxStandaloneViewport;
  };
  const rescaleStandaloneSeries = root => {
    const view = standaloneViewport(root); if (!view || nativeLegendConnected(root)) return;
    const hidden = !!root.querySelector('.cfx-series-muted,.cfx-series-isolated-out');
    for (const name of ['y', 'secondaryY']) {
      const axis = view.source[name + 'Projection'] || view.source.dataProjection[name]; if (!axis) continue;
      if (!hidden) { delete view.windows[name]; continue; }
      const values = view.points.filter(point => point.axis === name && !point.node.closest('.cfx-series-muted,.cfx-series-isolated-out'))
        .flatMap(point => [point.y, ...['cfxLow', 'cfxHigh'].filter(key => point.node.dataset[key] !== undefined).map(key => Number(point.node.dataset[key]))]).filter(Number.isFinite);
      if (!values.length) continue;
      if (view.source.series.some(row => ['area', 'steparea', 'bar'].includes(row.kind)) && axis.scale !== 'logarithmic') values.push(0);
      let minimum = Infinity, maximum = -Infinity;
      for (const value of values) { minimum = Math.min(minimum, value); maximum = Math.max(maximum, value); }
      if (axis.scale === 'logarithmic') { minimum = Math.max(axis.fullMinimum, minimum); maximum = Math.min(axis.fullMaximum, maximum); }
      if (minimum === maximum) { const pad = Math.abs(minimum) * .05 || 1; minimum -= pad; maximum += pad; }
      else if (!values.includes(0)) { const pad = (maximum - minimum) * .05; minimum -= pad; maximum += pad; }
      view.windows[name] = { minimum, maximum };
    }
    view.points.forEach(point => { point.node.style.visibility = point.node.closest('.cfx-series-muted,.cfx-series-isolated-out') ? 'hidden' : ''; });
    view.geometry.forEach(({ node }) => { node.style.visibility = node.closest('.cfx-series-muted,.cfx-series-isolated-out') ? 'hidden' : ''; });
    paintStandaloneViewport(root);
  };
  const standaloneBrushWindow = (root, rect) => {
    const view = standaloneViewport(root), matrix = svgScreenMatrix(view?.svg);
    if (!view || !matrix || rect.width < 4 || rect.height < 4) return null;
    const corners = [new DOMPoint(rect.left, rect.top), new DOMPoint(rect.right, rect.bottom)].map(point => point.matrixTransform(matrix.inverse()));
    const plot = view.source.dataProjection.plotBounds, windows = {};
    for (const name of root.dataset.cfxBrushAxis === 'x' ? ['x'] : ['x', 'y']) {
      const vertical = name === 'y', axis = standaloneAxis(root, name);
      const values = corners.map(point => projectedAxisValue(axis, clamp(vertical ? 1 - (point.y - plot.y) / plot.height : (point.x - plot.x) / plot.width, 0, 1)));
      windows[name] = { minimum: Math.min(...values), maximum: Math.max(...values) };
    }
    return windows;
  };
  const bindStandaloneGestures = root => {
    if (!hasFeature(root, 'Zoom') || !standaloneViewport(root)) return;
    const stage = root.querySelector('.cfx-stage'), pointers = new Map();
    const touchAction = stage.style.touchAction; stage.style.touchAction = 'none';
    let previous = null;
    const gesture = () => {
      const values = Array.from(pointers.values());
      return values.length === 2 ? { distance: Math.hypot(values[1].x - values[0].x, values[1].y - values[0].y),
        x: (values[0].x + values[1].x) / 2, y: (values[0].y + values[1].y) / 2 } : null;
    };
    listenChart(root, stage, 'pointerdown', event => {
      if (event.pointerType !== 'touch') return;
      const point = logicalChartPointer(root, event), plot = root._cfxDataProjection.plotBounds;
      if (!point || point.x < plot.x || point.x > plot.x + plot.width || point.y < plot.y || point.y > plot.y + plot.height) return;
      pointers.set(event.pointerId, point);
      if (pointers.size === 2) {
        root._cfxCancelStandaloneDrag?.(); stage.setPointerCapture(event.pointerId);
        previous = gesture(); event.preventDefault(); event.stopImmediatePropagation();
      }
    }, true);
    listenChart(root, stage, 'pointermove', event => {
      if (!pointers.has(event.pointerId)) return;
      const point = logicalChartPointer(root, event); if (!point) return;
      pointers.set(event.pointerId, point);
      const next = gesture();
      if (previous && next && previous.distance > 0 && next.distance > 0) {
        zoomStandaloneViewport(root, next.distance / previous.distance, previous.x, previous.y, ['x', 'y']);
        panStandaloneViewport(root, next.x - previous.x, next.y - previous.y);
        event.preventDefault(); event.stopImmediatePropagation();
      }
      previous = next;
    }, true);
    for (const name of ['pointerup', 'pointercancel', 'lostpointercapture']) listenChart(root, stage, name, event => {
      const pinching = !!previous; pointers.delete(event.pointerId); previous = gesture();
      if (pinching) { event.stopImmediatePropagation(); if (stage.hasPointerCapture(event.pointerId)) stage.releasePointerCapture(event.pointerId); }
    }, true);
    ownChartWork(root, () => { pointers.clear(); previous = null; stage.style.touchAction = touchAction; });
  };
