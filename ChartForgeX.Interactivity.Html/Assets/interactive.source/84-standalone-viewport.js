  // Saved reports reuse the detached projection and original vector geometry. Only
  // data ink moves: the frame, captions, marker sizes and stroke widths stay sharp.
  const standaloneStyle = (node, name) => ({ value: node.style.getPropertyValue(name), priority: node.style.getPropertyPriority(name) });
  const restoreStandaloneStyle = (node, name, style) => {
    // Rewriting an unchanged inline style still reports a paint mutation to the keyboard watch.
    if (node.style.getPropertyValue(name) === style.value && node.style.getPropertyPriority(name) === style.priority) return;
    if (style.value) node.style.setProperty(name, style.value, style.priority);
    else node.style.removeProperty(name);
  };
  const standaloneViewport = root => {
    const lifetime = chartLifetimes.get(root);
    if (!lifetime || lifetime.disposed || Object.values(lifetime.preparationConnections).some(Boolean)) return null;
    if (!hasFeature(root, 'Zoom') && !hasFeature(root, 'LegendToggles')) return null;
    const svg = root.querySelector('.cfx-stage svg');
    if (root._cfxStandaloneViewport?.svg === svg) return root._cfxStandaloneViewport;
    const metadata = capturedCartesianMetadata(root);
    if (!svg || !standaloneReprojects(metadata, svg)) return null;
    const horizontal = metadata.orientation === 'horizontal';
    // Horizontal charts keep categories on the vertical axis and values on X; physical paint maps stay x/y.
    const view = { svg, source: copyHostValue(metadata), windows: {}, geometry: [], labels: [], axes: [], points: [], frames: [],
      horizontal, categoryAxis: horizontal ? 'y' : 'x' };
    const valueAxis = secondary => horizontal ? secondary ? 'secondaryX' : 'x' : secondary ? 'secondaryY' : 'y';
    const seriesAxis = owner => valueAxis(owner?.querySelector('[data-cfx-point][data-cfx-axis]')?.dataset.cfxAxis === 'secondary');
    // Each painted node maps across by xAxis and down by axis.
    const seriesAxes = owner => horizontal ? { axis: 'y', xAxis: seriesAxis(owner) } : { axis: seriesAxis(owner), xAxis: 'x' };
    const plot = metadata.dataProjection.plotBounds;
    const ns = 'http://www.w3.org/2000/svg';
    const clip = document.createElementNS(ns, 'clipPath'), rect = document.createElementNS(ns, 'rect');
    clip.id = (svg.querySelector('[id]')?.id || 'cfx') + '-browser-plot';
    clip.setAttribute('clipPathUnits', 'userSpaceOnUse');
    for (const [key, value] of Object.entries(plot)) rect.setAttribute(key, value);
    clip.appendChild(rect); svg.querySelector('defs')?.appendChild(clip);
    if (!clip.parentNode) { const defs = document.createElementNS(ns, 'defs'); defs.appendChild(clip); svg.prepend(defs); view.defs = defs; }
    view.clip = clip;
    const series = svg.querySelectorAll('[data-cfx-role="series"]'), scatter = metadata.series.some(row => row?.kind === 'scatter');
    series.forEach(owner => {
      // An outer clip remains in plot coordinates while individual marks reproject.
      view.frames.push({ owner, clip: owner.getAttribute('clip-path') });
      // Series-level paths (lines, areas, bands) carry no axis; their observations name it.
      const axes = seriesAxes(owner);
      // The selector engine excludes definitions, text ink and captions and finds marks inside labels: per-mark
      // ancestor walks cost more than the rest of the capture on dense charts.
      const marks = ':is(path,line,rect,circle,ellipse,polygon,polyline,image):not(defs *,[data-cfx-native-text-ink],[data-cfx-native-text-ink] *,[data-cfx-caption],[data-cfx-caption] *)';
      const labelled = new Set(owner.querySelectorAll(marks + ':is([data-cfx-role$="label"],[data-cfx-role$="label"] *)'));
      owner.querySelectorAll(marks).forEach(node => {
        if (labelled.has(node) && node.closest('g')?.querySelector('text')) return;
        const role = node.getAttribute('data-cfx-role') || '';
        // Markers and error-bar caps keep their drawn size; only their position follows the window.
        const marker = !node.hasAttribute('data-cfx-browser-hit-area') && (role.startsWith('marker') || role === 'error-cap'
          || scatter && metadata.series[Number(node.closest('[data-cfx-point]')?.dataset.cfxSeries)]?.kind === 'scatter');
        const box = marker ? node.getBBox() : null;
        view.geometry.push({ node, attrs: Object.fromEntries(Array.from(node.attributes).map(attr => [attr.name, attr.value])),
          visibility: standaloneStyle(node, 'visibility'),
          marker: box ? { x: box.x + box.width / 2, y: box.y + box.height / 2 } : null, ...axes });
      });
    });
    svg.querySelectorAll('[data-cfx-label-for]').forEach(node => {
      const box = node.getBBox(), owner = node.closest('[data-cfx-role="series"]');
      view.labels.push({ node, x: box.x + box.width / 2, y: box.y + box.height / 2, transform: node.getAttribute('transform'), visibility: standaloneStyle(node, 'visibility'),
        ...owner ? seriesAxes(owner) : { axis: 'y', xAxis: 'x' } });
      if (!node.closest('[data-cfx-role="series"]')) {
        view.frames.push({ owner: node, clip: node.getAttribute('clip-path') });
      }
    });
    svg.querySelectorAll('[data-cfx-point][data-cfx-x]').forEach(node => {
      if (node.closest('[data-cfx-role="legend-item"]')) return;
      view.points.push({ node, region: node._cfxNativeRegion, paired: node._cfxPairedRegion, interiors: node._cfxFillInteriors,
        midline: node.dataset.cfxMidlineY, x: Number(node.dataset.cfxX), y: Number(node.dataset.cfxY ?? node.dataset.cfxEnd),
        visibility: standaloneStyle(node, 'visibility'),
        series: Number(node.dataset.cfxSeries), axis: valueAxis(node.dataset.cfxAxis === 'secondary'),
        visible: node.getAttribute('data-cfx-visible') });
    });
    for (const name of standaloneAxisNames) {
      const role = name.replace('secondary', 'secondary-').toLowerCase();
      const nodes = Array.from(svg.querySelectorAll('[data-cfx-role="axis-' + role + '-label"],[data-cfx-role="grid-' + role + '"],[data-cfx-role="grid-' + role + '-minor"]'));
      view.axes.push({ name, nodes, display: new Map(nodes.map(node => [node, standaloneStyle(node, 'display')])), layer: null });
    }
    view.stacks = captureStandaloneStacks(view);
    root._cfxStandaloneViewport = view;
    root.dataset.cfxStandaloneProjection = 'true';
    return view;
  };
  // Logical reads report a running transition's target; painting uses the displayed window.
  const standaloneAxis = (root, name, logical = false) => {
    const view = standaloneViewport(root);
    if (!view) return null;
    // A chart with two value axes emits no single scene Y; each captured axis then carries its own full domain.
    const scene = view.source.dataProjection[name], captured = view.source[name + 'Projection'] || scene;
    if (!captured) return null;
    const full = scene || { minimum: captured.fullMinimum, maximum: captured.fullMaximum,
      transformedMinimum: captured.transformedFullMinimum, transformedMaximum: captured.transformedFullMaximum };
    const source = { ...scene, ...captured, fullMinimum: full.minimum, fullMaximum: full.maximum,
      transformedFullMinimum: full.transformedMinimum, transformedFullMaximum: full.transformedMaximum };
    const window = logical && view.motionTarget ? view.motionTarget[name] : view.windows[name];
    return window ? { ...source, ...window, transformedMinimum: transformedAxisBound(source, window.minimum, false),
      transformedMaximum: transformedAxisBound(source, window.maximum, true, window.maximumInclusive !== false) } : source;
  };
  const standaloneCoordinate = (axis, value, plot, vertical) => vertical
    ? plot.y + (1 - projectedAxisRatio(axis, value)) * plot.height : plot.x + projectedAxisRatio(axis, value) * plot.width;
  // Brush readouts clamp to their current window. Navigation must extrapolate
  // beyond it before clipping the requested window to the captured full domain.
  const standaloneAxisValue = (axis, ratio, bound) => projectedAxisValue({ ...axis, minimum: -Infinity, maximum: Infinity }, ratio, bound);
  const standaloneMap = (source, target, plot, vertical) => {
    // The open source axis is built once per map, not once per coordinate of every mark.
    const first = vertical ? plot.y : plot.x, length = vertical ? plot.height : plot.width, open = { ...source, minimum: -Infinity, maximum: Infinity };
    const map = value => standaloneCoordinate(target, projectedAxisValue(open, vertical ? 1 - (value - first) / length : (value - first) / length), plot, vertical);
    return { at: map, scale: (map(first + length) - map(first)) / length };
  };
  const standalonePath = (path, x, y, pair) => {
    // The native SVG writer emits absolute M/L/Q/C/A/Z commands. Preserve curve
    // control points and arc flags instead of flattening curves into polylines.
    // A pair mapper moves each vertex from both of its coordinates (restacked areas).
    const tokens = path.match(/[a-zA-Z]|[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?/g) || [];
    const arity = { M: 2, L: 2, Q: 4, C: 6, A: 7, H: 1, V: 1, Z: 0 };
    let command = '', position = 0, output = [];
    while (position < tokens.length) {
      if (/^[a-zA-Z]$/.test(tokens[position])) { command = tokens[position++]; output.push(command); if (command === 'Z') continue; }
      const size = arity[command];
      if (!size || position + size > tokens.length) return path;
      const values = tokens.slice(position, position + size).map(Number); position += size;
      if (command === 'A') { values[0] *= Math.abs(x.scale); values[1] *= Math.abs(y.scale); [values[5], values[6]] = pair ? pair(values[5], values[6]) : [x.at(values[5]), y.at(values[6])]; }
      else if (command === 'H') values[0] = x.at(values[0]);
      else if (command === 'V') values[0] = y.at(values[0]);
      else for (let i = 0; i < values.length; i += 2) [values[i], values[i + 1]] = pair ? pair(values[i], values[i + 1]) : [x.at(values[i]), y.at(values[i + 1])];
      output.push(values.map(value => Number(value.toFixed(4))).join(' '));
    }
    return output.join(' ');
  };
  const standaloneRound = value => Number(value.toFixed(4)), standaloneXAttrs = ['x', 'x1', 'x2', 'cx'], standaloneYAttrs = ['y', 'y1', 'y2', 'cy'];
  const projectStandaloneAttrs = (attrs, x, y, pair) => {
    const out = {};
    if (attrs.d) out.d = standalonePath(attrs.d, x, y, pair);
    if (attrs.points) out.points = attrs.points.trim().split(/[\s,]+/).map(Number).map((value, index) => (index % 2 ? y : x).at(value)).join(' ');
    for (const name of standaloneXAttrs) if (attrs[name] !== undefined) out[name] = standaloneRound(x.at(Number(attrs[name])));
    for (const name of standaloneYAttrs) if (attrs[name] !== undefined) out[name] = standaloneRound(y.at(Number(attrs[name])));
    if (attrs.width !== undefined) out.width = standaloneRound(Number(attrs.width) * Math.abs(x.scale));
    if (attrs.height !== undefined) out.height = standaloneRound(Number(attrs.height) * Math.abs(y.scale));
    return out;
  };
  const paintStandaloneGeometry = (entry, x, y) => {
    if (entry.marker) {
      const at = entry.currentMarker || entry.marker, dx = x.at(at.x) - entry.marker.x, dy = y.at(at.y) - entry.marker.y;
      x = { at: value => value + dx, scale: 1 }; y = { at: value => value + dy, scale: 1 };
    }
    // Unchanged values are skipped: rewriting one still restyles the mark and reports a paint mutation.
    const out = projectStandaloneAttrs(entry.current || entry.attrs, x, y);
    for (const name in out) if (entry.node.getAttribute(name) !== String(out[name])) entry.node.setAttribute(name, out[name]);
  };
  let standaloneMeasure = null;
  const standaloneTextWidth = (font, text) => {
    standaloneMeasure ||= document.createElement('canvas').getContext('2d');
    if (!standaloneMeasure) return String(text).length * parseFloat(font) * .6;
    standaloneMeasure.font = font; return standaloneMeasure.measureText(String(text)).width;
  };
  // A glide passes one measure for all its frames: the drawing's size and the caption templates do not change while it eases.
  const paintStandaloneAxes = (root, view, measure = {}) => {
    if (view.printing) return;
    const plot = view.source.dataProjection.plotBounds, ns = view.svg.namespaceURI;
    const compact = measure.compact ??= root.getBoundingClientRect().width < 560;
    for (const axis of view.axes) {
      const projection = standaloneAxis(root, axis.name);
      if (!projection || !axis.nodes.length) continue;
      if (!view.windows[axis.name] && !compact) {
        axis.nodes.forEach(node => restoreStandaloneStyle(node, 'display', axis.display.get(node))); axis.layer?.remove(); axis.grid?.remove(); axis.layer = null; axis.grid = null; continue;
      }
      // Measured only when browser captions are drawn, so restoring native ones forces no layout.
      const scale = measure.scale ??= view.svg.getBoundingClientRect().width / view.svg.viewBox.baseVal.width || 1;
      axis.nodes.forEach(node => node.style.display !== 'none' && (node.style.display = 'none'));
      axis.layer?.remove();
      axis.grid?.remove();
      const layer = document.createElementNS(ns, 'g'); axis.layer = layer;
      layer.dataset.cfxBrowserAxis = axis.name; layer.setAttribute('pointer-events', 'none'); view.svg.appendChild(layer);
      const grid = document.createElementNS(ns, 'g'); axis.grid = grid;
      grid.dataset.cfxBrowserGrid = axis.name; grid.setAttribute('pointer-events', 'none');
      let plotLayer = view.svg.querySelector('[data-cfx-role="series"]');
      while (plotLayer?.parentNode && plotLayer.parentNode !== view.svg) plotLayer = plotLayer.parentNode;
      view.svg.insertBefore(grid, plotLayer || null);
      const vertical = standaloneVertical(axis.name), secondary = axis.name.startsWith('secondary');
      const template = axis.nodes.find(node => node.querySelector('text'))?.querySelector('text');
      const templates = measure.templates ||= new Map(), style = templates.get(axis) || templates.set(axis, template && (({ fill, fontFamily }) => ({ fill, fontFamily }))(getComputedStyle(template))).get(axis);
      const box = (measure.boxes ||= new Map()).get(axis) || measure.boxes.set(axis, template?.getBBox()).get(axis);
      const size = Math.max(Number(template?.getAttribute('font-size')) || 12, compact ? 12 / scale : 0);
      // Measure with the face the axis captions actually paint (the delivered product font), so caption fitting
      // matches native on hosts without an installed copy; the chrome font stack is only the fallback.
      const family = style?.fontFamily || getComputedStyle(root).getPropertyValue('--cfx-tooltip-font') || 'sans-serif';
      // Room is in drawing units: a label's own width plus a 12 CSS pixel gap, or about two lines vertically.
      const room = vertical ? () => size * 2 : label => standaloneTextWidth(size + 'px ' + family, label) + 16 / scale;
      // Linear windows start at the native density for the axis length; others keep the drawing's own label count.
      const ticks = projection.captions === 'calendar' && !vertical && standaloneTimeTicks(projection, plot.width,
        label => standaloneTextWidth(size + 'px ' + family, label), size, standaloneTimeObservations(view), view.calendar ||= standaloneCalendar(projection)) || standaloneTicks(projection, vertical ? plot.height : plot.width, room,
        projection.scale === 'linear' ? standaloneNativeTickCount(projection, vertical ? plot.height : plot.width, vertical, size)
          : axis.nodes.filter(node => node.dataset.cfxRole.endsWith('-label') && axis.display.get(node)?.value !== 'none').length || 5,
        axis.name === view.categoryAxis && view.source.categoryLabels?.length ? view.source.categoryLabels : null, standaloneWholeAxis(view, axis.name, projection));
      for (const tick of ticks) {
        const position = standaloneCoordinate(projection, tick.value, plot, vertical);
        const text = template ? template.cloneNode(false) : document.createElementNS(ns, 'text');
        text.removeAttribute('display'); text.removeAttribute('transform'); text.removeAttribute('data-cfx-owned-font-text'); text.removeAttribute('data-cfx-png-glyphs');
        text.setAttribute('font-size', size); text.setAttribute('fill', style?.fill || 'currentColor');
        text.setAttribute('font-family', family || 'inherit');
        text.setAttribute('x', vertical ? secondary ? plot.x + plot.width + 8 : plot.x - 8 : position);
        // A top axis keeps its drawn caption baseline.
        text.setAttribute('y', vertical ? position : secondary ? template?.getAttribute('y') ?? plot.y - 8 : plot.y + plot.height + 8 + (box?.height || size));
        text.setAttribute('text-anchor', vertical ? secondary ? 'start' : 'end' : 'middle');
        text.setAttribute('dominant-baseline', vertical ? 'middle' : 'auto'); text.textContent = tick.text;
        text.dataset.cfxAxisValue = tick.value; layer.appendChild(text);
        const gridTemplate = axis.nodes.find(node => /^grid-[xy]$/.test(node.dataset.cfxRole));
        if (gridTemplate) grid.appendChild(standaloneGridLine(gridTemplate, position, plot, vertical));
      }
      const minorTemplate = axis.nodes.find(node => node.dataset.cfxRole.endsWith('-minor'));
      if (minorTemplate) for (const value of standaloneMinorTicks(projection, ticks))
        grid.prepend(standaloneGridLine(minorTemplate, standaloneCoordinate(projection, value, plot, vertical), plot, vertical));
    }
  };
  // The changes a window repaint makes itself: mark geometry, caption shifts and visibility, plot clips and native axis captions.
  const standaloneGeometryAttributes = ['d', 'points', 'x', 'x1', 'x2', 'cx', 'y', 'y1', 'y2', 'cy', 'width', 'height'];
  const standaloneMovedAttributes = new Set([...standaloneGeometryAttributes, 'transform']);
  const standaloneMoved = view => {
    if (!view.moves) {
      // Each moved node names the attributes the repaint writes on it.
      const clips = new Set(['clip-path']), captions = new Set([...standaloneMovedAttributes, 'style', 'clip-path']), owners = new Map([
        ...view.geometry.map(entry => [entry.node, standaloneMovedAttributes]), ...view.frames.map(frame => [frame.owner, clips]),
        ...[...view.labels.map(entry => entry.node), ...view.axes.flatMap(axis => axis.nodes)].map(node => [node, captions])]);
      view.moves = record => record.type === 'attributes' && !!owners.get(record.target)?.has(record.attributeName);
    }
    return view.moves;
  };
  const paintStandaloneViewport = root => {
    const view = standaloneViewport(root); if (!view) return false;
    // A glide's closing paint keeps its measure: the drawing's size did not change while it eased.
    const measure = view.glideSettled && view.glideMeasure || {};
    settleStandaloneGlide(view);
    if (view.printing) return true;
    if (!Object.keys(view.windows).length && !view.stacks?.active) {
      // Drawn as captured (at binding or after a restore), the marks and published metadata are already the source's.
      if (view.moved) { restoreStandaloneGeometry(view); root.dataset.cfxPreparedChart = JSON.stringify(view.source); }
      setStandaloneClip(view, false);
      root._cfxDataProjection = view.source.dataProjection;
      paintStandaloneAxes(root, view, measure); syncXNavigation(root); syncResetControl(root); refreshKeyboardNavigation(root, undefined, false, standaloneMoved(view));
      hideCrosshair(root, root.querySelector('.cfx-crosshair')); refreshTooltipPosition(root);
      return true;
    }
    setStandaloneClip(view, true); view.moved = true;
    const plot = view.source.dataProjection.plotBounds, maps = {};
    for (const name of standaloneAxisNames) {
      const source = standaloneSourceAxis(view, name);
      if (source) maps[name] = standaloneMap(source, standaloneAxis(root, name), plot, standaloneVertical(name));
    }
    // A glide that nothing disturbed closes on the marks and observations its opening paint placed for these windows.
    if (!view.glideSettled) {
      // Every mark moves: the chart model is dropped and the keyboard watch paused instead of each recording every write.
      const model = chartModels.get(root), resume = root._cfxPauseAvailability?.();
      if (model?.data) { model.observer.disconnect(); model.data = null; }
      view.geometry.forEach(entry => paintStandaloneGeometry(entry, maps[entry.xAxis] || maps.x, maps[entry.axis] || maps.y));
      view.labels.forEach(entry => {
        const x = maps[entry.xAxis] || maps.x, y = maps[entry.axis] || maps.y, at = entry.current || entry, px = x.at(at.x), py = y.at(at.y);
        entry.node.setAttribute('transform', 'translate(' + (px - entry.x) + ' ' + (py - entry.y) + ') ' + (entry.transform || ''));
        // The clip moves with a translated caption, so a caption whose anchor leaves the plot hides instead.
        // A restacked total keeps its captured caption, so it hides until its stack is whole again.
        const slack = entry.slack || .5, outside = px < plot.x - slack || px > plot.x + plot.width + slack || py < plot.y - slack || py > plot.y + plot.height + slack;
        entry.node.style.visibility = outside || entry.hiddenTotal || entry.node.matches('.cfx-series-muted,.cfx-series-isolated-out') ? 'hidden' : '';
      });
      // Each axis is read once for the pass: reading it builds a fresh projection.
      const axes = {}, axisOf = name => axes[name] ||= standaloneAxis(root, name);
      const region = (box, x, y) => box && Object.freeze({ x: x.at(box.x), y: y.at(box.y), width: box.width * Math.abs(x.scale), height: box.height * Math.abs(y.scale) });
      for (const point of view.points) {
        // A horizontal observation's category runs down the plot and its value across it.
        const y = view.horizontal ? maps.y : maps[point.axis] || maps.y, x = view.horizontal ? maps[point.axis] || maps.x : maps.x;
        const level = point.stack ? (point.stack.now || point.stack).end : point.y;
        const px = view.horizontal ? standaloneCoordinate(axisOf(point.axis), level, plot, false) : standaloneCoordinate(axisOf('x'), point.x, plot, false);
        const py = view.horizontal ? standaloneCoordinate(axisOf('y'), point.x, plot, true) : standaloneCoordinate(axisOf(point.axis), level, plot, true);
        if (point.region) point.node._cfxNativeRegion = region(point.stackRegion || point.region, x, y);
        if (point.paired) point.node._cfxPairedRegion = region(point.paired, x, y);
        if (point.midline !== undefined) point.node.dataset.cfxMidlineY = y.at(Number(point.midline));
        const bounds = point.node._cfxNativeRegion || { x: px, y: py, width: 0, height: 0 };
        point.node.dataset.cfxVisible = String(point.visible !== 'false' && bounds.x + bounds.width >= plot.x - .01 && bounds.x <= plot.x + plot.width + .01
          && bounds.y + bounds.height >= plot.y - .01 && bounds.y <= plot.y + plot.height + .01);
        if (point.interiors) point.node._cfxFillInteriors = Object.freeze((point.stackInteriors || point.interiors).map(([px, py, radius]) =>
          Object.freeze([x.at(px), y.at(py), radius * Math.min(Math.abs(x.scale), Math.abs(y.scale))])));
      }
      resume?.();
    }
    // The published metadata depends only on the windows; a glide's closing paint republishes its opening one.
    const key = JSON.stringify(view.windows);
    if (view.published?.key !== key) {
      const metadata = copyHostValue(view.source);
      for (const name of standaloneAxisNames) {
        const axis = standaloneAxis(root, name); if (!axis) continue;
        metadata[name + 'Projection'] = axis;
        if (metadata.dataProjection[name]) metadata.dataProjection[name] = axis;
      }
      view.published = { key, json: JSON.stringify(metadata), projection: metadata.dataProjection };
    }
    root.dataset.cfxPreparedChart = view.published.json; root._cfxDataProjection = view.published.projection;
    paintStandaloneAxes(root, view, measure); syncXNavigation(root); syncResetControl(root); refreshKeyboardNavigation(root, undefined, false, standaloneMoved(view));
    hideCrosshair(root, root.querySelector('.cfx-crosshair')); refreshTooltipPosition(root);
    return true;
  };
  // Discrete changes (toolbar zoom, brush zoom, reset and legend rescaling) ease from the
  // displayed windows to the new ones when the motion bundle is present. Gestures and API
  // windows repaint at once; the requested windows and events are final immediately.
  const standaloneVertical = name => name === 'y' || name === 'secondaryY';
  // The drawing's own projection of one axis: the scene's X, otherwise each captured axis.
  const standaloneSourceAxis = (view, name) => name === 'x' ? view.source.dataProjection.x || view.source.xProjection
    : view.source[name + 'Projection'] || view.source.dataProjection[name];
  const standaloneFullWindows = (view, windows) => Object.fromEntries(standaloneAxisNames
    .map(name => [name, standaloneSourceAxis(view, name)]).filter(([, axis]) => axis)
    .map(([name, axis]) => [name, { minimum: windows[name]?.minimum ?? axis.minimum, maximum: windows[name]?.maximum ?? axis.maximum }]));
  // Stops a running transition and returns the displayed windows; view.windows becomes its logical target again.
  const beginStandaloneChange = view => {
    const displayed = { ...view.windows };
    view.cancelMotion?.(); view.cancelMotion = null;
    if (view.motionTarget) view.windows = { ...view.motionTarget };
    view.motionTarget = null;
    return displayed;
  };
  const presentStandaloneViewport = (root, view, previous, animate) => {
    const motion = animate && chartFeature('motion'), target = view.windows;
    if (!motion || !motionDuration(root)) { paintStandaloneViewport(root); return; }
    const from = standaloneFullWindows(view, previous), to = standaloneFullWindows(view, target);
    if (Object.keys(to).every(name => from[name] && from[name].minimum === to[name].minimum && from[name].maximum === to[name].maximum)) {
      paintStandaloneViewport(root); return;
    }
    // When only the category window moves, every mark maps from the final window to the displayed one by the same
    // horizontal scale and shift. The marks are drawn once for the final window, with every observation, and only one
    // transform eases them; the closing paint is the same as without motion.
    const glide = !view.horizontal && !view.stacks?.active && Object.keys(to).every(name => name === 'x' || from[name].minimum === to[name].minimum && from[name].maximum === to[name].maximum);
    // Controls and keyboard reach follow the logical window at once: paint it once for its
    // keyboard stops, then show the starting frame before anything renders. Only the drawing eases.
    paintStandaloneViewport(root);
    const frame = progress => {
      view.windows = Object.fromEntries(Object.keys(to).map(name => [name, {
        minimum: from[name].minimum + (to[name].minimum - from[name].minimum) * progress,
        maximum: from[name].maximum + (to[name].maximum - from[name].maximum) * progress }]));
      // A glide eases only the category window; the other axes keep their final windows and native captions.
      if (glide) { view.windows = { ...target, x: view.windows.x }; glideStandaloneViewport(root, view); } else paintStandaloneViewport(root);
    };
    view.motionTarget = target; view.glideTouched = false; frame(0);
    const cancel = motion.tween(root, chartFeatureKernel, frame, () => {
      view.cancelMotion = null; view.motionTarget = null; view.windows = target;
      view.glideSettled = glide && !view.glideTouched; paintStandaloneViewport(root); view.glideSettled = false;
    });
    view.cancelMotion = () => { cancel(); settleStandaloneGlide(view); };
  };
  // Nodes a glide moves or hides; while it runs the keyboard watch ignores those changes, which move no target for good.
  const standaloneGlideNodes = new WeakSet();
  const glideStandaloneViewport = (root, view) => {
    const map = standaloneMap(standaloneAxis(root, 'x', true), standaloneAxis(root, 'x'), view.source.dataProjection.plotBounds, false);
    // Only the drawn marks move, inside the plot clip of their unmoved groups, so no clip shifts and leaves an empty strip.
    // Markers and captions keep their drawn size and move by the shift of their own centre; hit areas wait at their final places.
    if (!view.glide) {
      const target = standaloneMap(standaloneSourceAxis(view, 'x'), standaloneAxis(root, 'x', true), view.source.dataProjection.plotBounds, false).at;
      view.glideMarks = [...view.geometry.filter(entry => !entry.node.matches('[data-cfx-browser-hit-area],.cfx-prepared-point-marker'))
        .map(entry => [entry.node, entry.marker && target((entry.currentMarker || entry.marker).x)]), ...view.labels.map(entry => [entry.node, target((entry.current || entry).x)])]
        .map(([node, x]) => { node.dataset.cfxGlide = ''; standaloneGlideNodes.add(node); return [node, node.getAttribute('transform'), x]; });
      // Browser markers of a hovered or selected observation wait at its final place, so they hide until the closing paint.
      view.glideHidden = [...view.svg.querySelectorAll(':is(.cfx-hovered, .cfx-hover-column, .cfx-selected, .cfx-highlighted) > .cfx-prepared-point-marker')]
        .map(node => { const style = standaloneStyle(node, 'visibility'); standaloneGlideNodes.add(node); node.style.visibility = 'hidden'; return [node, style]; });
    }
    // Marks drawn for the full window carry no plot clip; a glide always eases inside it.
    view.glide = true; view.moved = true; setStandaloneClip(view, true);
    view.glideMarks.forEach(([node, own, x]) => node.setAttribute('transform', (typeof x !== 'number' ? 'matrix(' + map.scale + ' 0 0 1 ' + map.at(0) + ' 0)' : 'translate(' + (map.at(x) - x) + ' 0)') + (own ? ' ' + own : '')));
    paintStandaloneAxes(root, view, view.glideMeasure ||= {});
  };
  const settleStandaloneGlide = view => {
    if (!view.glide) return;
    view.glide = false; view.glideMeasure = null;
    view.glideHidden.forEach(([node, style]) => { standaloneGlideNodes.delete(node); restoreStandaloneStyle(node, 'visibility', style); });
    view.glideMarks.forEach(([node, own]) => { standaloneGlideNodes.delete(node); delete node.dataset.cfxGlide; own === null ? node.removeAttribute('transform') : node.setAttribute('transform', own); });
  };
  const requestStandaloneWindow = (root, windows, sync = true, accepted, emit = true, animate = false) => {
    if (chartLifetimes.get(root)?.preparationPending) return false;
    const view = standaloneViewport(root); if (!view || !windows || typeof windows !== 'object') return false;
    if (windows.valueDomain !== undefined && windows.valueDomain !== (view.source.valueDomain || 'fullData')) return false;
    const names = Object.keys(windows).filter(name => name !== 'valueDomain');
    if (!names.length || names.some(name => !standaloneAxisNames.includes(name) || !validSourceAxisWindow(windows[name], standaloneAxis(root, name)))) return false;
    for (const name of names) {
      const axis = standaloneAxis(root, name), requested = windows[name];
      const fullMin = axis.fullMinimum ?? axis.minimum, fullMax = axis.fullMaximum ?? axis.maximum;
      const minimum = Math.max(fullMin, requested.minimum), maximum = Math.min(fullMax, requested.maximum);
      if (!(minimum < maximum)) return false;
    }
    const previous = beginStandaloneChange(view);
    for (const name of names) {
      const axis = standaloneAxis(root, name), requested = windows[name];
      view.windows[name] = { minimum: Math.max(axis.fullMinimum ?? axis.minimum, requested.minimum), maximum: Math.min(axis.fullMaximum ?? axis.maximum, requested.maximum) };
      const source = standaloneSourceAxis(view, name);
      if (view.windows[name].minimum === source.minimum && view.windows[name].maximum === source.maximum) delete view.windows[name];
    }
    presentStandaloneViewport(root, view, previous, animate); accepted?.();
    if (emit) emitHostEvent(root, 'cfxviewport', { viewport: nativeViewportWindows(root), standalone: true });
    if (sync) emitSync(root, { action: 'viewport', state: getState(root), viewport: nativeViewportWindows(root, true), xWindow: nativeXWindow(root), yWindow: nativeYWindow(root) });
    return true;
  };
  // Gestures name directions: 'x' is the category direction and 'y' the value direction. Each value
  // axis takes its own window from the same screen ratios, so two Y axes re-project independently.
  const standaloneGestureAxes = (view, axes) => [...new Set(axes.flatMap(name => {
    const physical = view.horizontal ? name === 'x' ? 'y' : 'x' : name;
    return physical === 'y' && view.source.secondaryYProjection ? ['y', 'secondaryY']
      : physical === 'x' && view.horizontal && view.source.secondaryXProjection ? ['x', 'secondaryX'] : [physical];
  }))];
  const zoomStandaloneViewport = (root, factor, logicalX, logicalY, axes = ['x'], animate = false) => {
    const view = standaloneViewport(root); if (!view || !Number.isFinite(factor) || factor <= 0) return false;
    // Zoom and pan start from the logical window, so a gesture during a transition continues from its target.
    const plot = view.source.dataProjection.plotBounds, windows = {};
    for (const name of standaloneGestureAxes(view, axes)) {
      const axis = standaloneAxis(root, name, true); if (!axis) continue;
      const vertical = standaloneVertical(name), value = vertical ? logicalY : logicalX;
      const first = vertical ? plot.y : plot.x, size = vertical ? plot.height : plot.width;
      const ratio = Number.isFinite(value) ? clamp((value - first) / size, 0, 1) : .5;
      const a = standaloneAxisValue(axis, vertical ? 1 - ratio - (1 - ratio) / factor : ratio - ratio / factor, false);
      const b = standaloneAxisValue(axis, vertical ? 1 - ratio + ratio / factor : ratio + (1 - ratio) / factor, true);
      windows[name] = { minimum: Math.min(a, b), maximum: Math.max(a, b) };
    }
    return requestStandaloneWindow(root, windows, true, undefined, true, animate);
  };
  const panStandaloneViewport = (root, deltaX, deltaY) => {
    const view = standaloneViewport(root); if (!view) return false;
    const plot = view.source.dataProjection.plotBounds, windows = {};
    for (const [name, delta, size] of [['x', deltaX, plot.width], ['secondaryX', deltaX, plot.width], ['y', -deltaY, plot.height], ['secondaryY', -deltaY, plot.height]]) {
      const axis = standaloneAxis(root, name, true); if (!axis) continue;
      const shift = delta / size * (axis.reversed ? -1 : 1);
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
    const previous = beginStandaloneChange(view);
    view.windows = {}; presentStandaloneViewport(root, view, previous, true); return true;
  };
  const restoreStandaloneGeometry = view => {
    settleStandaloneGlide(view); view.glideTouched = true; view.moved = false;
    for (const entry of view.geometry) {
      for (const name of standaloneGeometryAttributes)
        // Rewriting an unchanged value still restyles the mark, which costs a full pass on dense charts.
        if (entry.attrs[name] !== undefined && entry.node.getAttribute(name) !== entry.attrs[name]) entry.node.setAttribute(name, entry.attrs[name]);
      if (entry.attrs.transform === undefined) entry.node.removeAttribute('transform'); else entry.node.setAttribute('transform', entry.attrs.transform);
      restoreStandaloneStyle(entry.node, 'visibility', entry.visibility);
    }
    for (const entry of view.labels) {
      if (entry.transform === null) entry.node.removeAttribute('transform'); else entry.node.setAttribute('transform', entry.transform);
      restoreStandaloneStyle(entry.node, 'visibility', entry.visibility);
    }
    for (const point of view.points) {
      if (point.visible === null) point.node.removeAttribute('data-cfx-visible'); else point.node.setAttribute('data-cfx-visible', point.visible);
      point.node._cfxNativeRegion = point.region; point.node._cfxFillInteriors = point.interiors;
      if (point.paired) point.node._cfxPairedRegion = point.paired;
      if (point.midline !== undefined) point.node.dataset.cfxMidlineY = point.midline;
      restoreStandaloneStyle(point.node, 'visibility', point.visibility);
    }
    for (const axis of view.axes) {
      axis.layer?.remove(); axis.grid?.remove(); axis.nodes.forEach(node => restoreStandaloneStyle(node, 'display', axis.display.get(node)));
    }
  };
  const setStandaloneClip = (view, active) => {
    view.frames.forEach(({ owner, clip }) => {
      if (active) owner.getAttribute('clip-path') !== 'url(#' + view.clip.id + ')' && owner.setAttribute('clip-path', 'url(#' + view.clip.id + ')');
      else if (clip === null) owner.removeAttribute('clip-path');
      else owner.setAttribute('clip-path', clip);
    });
  };
  const releaseStandaloneViewport = root => {
    const view = root._cfxStandaloneViewport; if (!view) return;
    view.stacks?.members.forEach(member => standaloneStackFacts.forEach((key, index) => {
      if (member.facts[index] === undefined) delete member.node.dataset[key]; else member.node.dataset[key] = member.facts[index];
    }));
    restoreStandaloneGeometry(view);
    setStandaloneClip(view, false); view.clip.remove();
    if (view.defs && !view.defs.hasChildNodes()) view.defs.remove();
    root.dataset.cfxPreparedChart = JSON.stringify(view.source); root._cfxDataProjection = view.source.dataProjection;
    delete root._cfxStandaloneViewport;
    root.removeAttribute('data-cfx-standalone-projection');
  };
  const rescaleStandaloneSeries = root => {
    const view = standaloneViewport(root); if (!view || nativeLegendConnected(root)) return;
    // Only hidden data counts; legend paint can keep a stale isolation class until its own sync.
    const hidden = view.points.some(point => point.node.closest('.cfx-series-muted,.cfx-series-isolated-out'));
    const previous = beginStandaloneChange(view);
    restackStandaloneView(view);
    for (const name of view.horizontal ? ['x', 'secondaryX'] : ['y', 'secondaryY']) {
      const axis = standaloneAxis(root, name); if (!axis) continue;
      if (!hidden) { delete view.windows[name]; continue; }
      // Stacked observations span their restacked baseline and end.
      const values = view.points.filter(point => point.axis === name && !point.node.closest('.cfx-series-muted,.cfx-series-isolated-out'))
        .flatMap(point => point.stack ? [(point.stack.now || point.stack).base, (point.stack.now || point.stack).end]
          : standalonePointValues(point)).filter(Number.isFinite);
      if (!values.length) continue;
      if (view.source.series.some(row => ['area', 'steparea', 'stackedarea', 'bar', 'horizontalbar', 'waterfall', 'lollipop'].includes(row.kind)) && axis.scale !== 'logarithmic') values.push(0);
      let minimum = Infinity, maximum = -Infinity;
      for (const value of values) { minimum = Math.min(minimum, value); maximum = Math.max(maximum, value); }
      if (axis.scale === 'logarithmic') { minimum = Math.max(axis.fullMinimum, minimum); maximum = Math.min(axis.fullMaximum, maximum); }
      if (minimum === maximum) { const pad = Math.abs(minimum) * .05 || 1; minimum -= pad; maximum += pad; }
      else if (!values.includes(0)) { const pad = (maximum - minimum) * .05; minimum -= pad; maximum += pad; }
      view.windows[name] = { minimum, maximum };
    }
    view.moved = true;
    view.points.forEach(point => { point.node.style.visibility = point.node.closest('.cfx-series-muted,.cfx-series-isolated-out') ? 'hidden' : ''; });
    view.geometry.forEach(({ node }) => { node.style.visibility = node.closest('.cfx-series-muted,.cfx-series-isolated-out') ? 'hidden' : ''; });
    presentStandaloneViewport(root, view, previous, true);
  };
  const standaloneBrushWindow = (root, rect) => {
    const view = standaloneViewport(root), matrix = svgScreenMatrix(view?.svg);
    if (!view || !matrix || rect.width < 4 || rect.height < 4) return null;
    const corners = [new DOMPoint(rect.left, rect.top), new DOMPoint(rect.right, rect.bottom)].map(point => point.matrixTransform(matrix.inverse()));
    const plot = view.source.dataProjection.plotBounds, windows = {};
    const across = ['x', ...view.horizontal && view.source.secondaryXProjection ? ['secondaryX'] : []];
    for (const name of root.dataset.cfxBrushAxis === 'x' ? across : [...across, 'y', ...view.source.secondaryYProjection ? ['secondaryY'] : []]) {
      const vertical = standaloneVertical(name), axis = standaloneAxis(root, name);
      const values = corners.map(point => projectedAxisValue(axis, clamp(vertical ? 1 - (point.y - plot.y) / plot.height : (point.x - plot.x) / plot.width, 0, 1)));
      windows[name] = { minimum: Math.min(...values), maximum: Math.max(...values) };
    }
    return windows;
  };
  const bindStandaloneGestures = root => {
    const media = window.matchMedia?.('print');
    let printed = null;
    const print = active => {
      const view = root._cfxStandaloneViewport;
      if (active && view && !view.printing) {
        printed = view; view.printing = true; restoreStandaloneGeometry(view);
        setStandaloneClip(view, false);
      } else if (!active && printed) {
        const previous = printed; printed = null; previous.printing = false;
        if (view === previous) {
          paintStandaloneViewport(root);
        }
      }
    };
    listenChart(root, window, 'beforeprint', () => print(true));
    listenChart(root, window, 'afterprint', () => print(false));
    if (media?.addEventListener) listenChart(root, media, 'change', event => print(event.matches));
    ownChartWork(root, () => { if (printed) printed.printing = false; printed = null; });
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
