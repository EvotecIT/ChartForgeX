
  // Pointer work on dense charts reads one model per chart: target lists, keyed lookups and measured centres.
  // A mutation observer drops it after changes that can alter those facts; hover paint and chrome do not.
  const chartModels = new WeakMap();
  const chartModelIgnored = new Set(['class', 'tabindex', 'aria-selected', 'data-cfx-keyboard-component']);
  const chartModelChange = (root, record) => record.target !== root
    && !(record.type === 'attributes' && chartModelIgnored.has(record.attributeName))
    && !record.target.closest?.('.cfx-tooltip,.cfx-crosshair,[data-cfx-reveal-layer]');
  const chartModel = root => {
    if (chartLifetimes.get(root)?.disposed !== false) return null;
    let model = chartModels.get(root);
    const drop = () => { model.observer.disconnect(); model.data = null; };
    if (!model) {
      chartModels.set(root, model = { observer: new MutationObserver(records => records.some(record => chartModelChange(root, record)) && drop()) });
      ownChartWork(root, () => { drop(); chartModels.delete(root); });
    } else if (model.data && model.observer.takeRecords().some(record => chartModelChange(root, record))) drop();
    if (!model.data) {
      model.data = { layers: new Map(), shapes: new Map() };
      model.observer.observe(root, { subtree: true, childList: true, attributes: true });
    }
    return model.data;
  };
  // Without a live model (before binding or after disposal) every read builds its facts afresh.
  const modelled = (root, name, build) => {
    const model = chartModel(root);
    return model ? model[name] ||= build() : build();
  };
  // Chrome lookups outside the drawing would otherwise search every mark of a dense chart on each pointer move.
  // Interactive targets, read once per drawing instead of once per caller: binding alone asks several times.
  const chartTargets = root => modelled(root, 'targets', () => interactiveTargets(root));
  const chartNodes = (root, selector) => modelled(root, selector, () => Array.from(root.querySelectorAll(selector)));
  const modelGroups = (nodes, keys) => {
    const groups = new Map();
    nodes.forEach(node => keys(node).forEach(key => (groups.get(key) || groups.set(key, []).get(key)).push(node)));
    return groups;
  };
  // Hover lookups by target identity, point and label: the only facts besides the series that relate a node to a target.
  const hoverGroups = root => modelled(root, 'hover', () => {
    const nodes = chartTargets(root);
    return {
      identity: modelGroups(nodes, node => { const kind = renderedTargetKind(node); return [kind + '\n' + renderedTargetId(node, kind)]; }),
      point: modelGroups(nodes, node => node.dataset.cfxPoint === undefined ? [] : [node.dataset.cfxPoint]),
      label: modelGroups(nodes, node => [node.dataset.cfxLabel, node.dataset.cfxText, node.getAttribute('aria-label')].filter(Boolean)),
      summary: nodes.filter(node => node.dataset.cfxPoint === undefined)
    };
  });
  // Cartesian observations measured once per drawing size, relative to the drawing so that scrolling keeps them,
  // and sorted along the axis that ranks them (y on horizontal charts).
  const chartPlaces = (root, svg, axis) => {
    const frame = svg?.getBoundingClientRect() || { left: 0, top: 0 }, model = chartModel(root), last = model?.places;
    if (last?.axis === axis && last.width === frame.width && last.height === frame.height) return { ...last, left: frame.left, top: frame.top };
    const places = { axis, width: frame.width, height: frame.height, left: frame.left, top: frame.top, list: [], reach: 0 };
    const local = box => ({ left: box.left - frame.left, right: box.right - frame.left, top: box.top - frame.top, bottom: box.bottom - frame.top });
    const [lo, hi] = axis === 'x' ? ['left', 'right'] : ['top', 'bottom'];
    modelled(root, 'points', () => Array.from(root.querySelectorAll('[data-cfx-point]'))).forEach((node, order) => {
      if (!isInteractiveTarget(node) || !usesCartesianCoordinates(node)) return;
      const box = node.getBoundingClientRect(), area = node.querySelector('[data-cfx-browser-hit-area]')?.getBoundingClientRect();
      if (!box.width && !box.height) return;
      const place = { node, order, x: box.left + box.width / 2 - frame.left, y: box.top + box.height / 2 - frame.top, area: area && local(area), categorical: categoricalPointerKind(node) };
      if (area) places.reach = Math.max(places.reach, Math.abs(place.area[lo] - place[axis]), Math.abs(place.area[hi] - place[axis]));
      places.list.push(place);
    });
    places.list.sort((a, b) => a[axis] - b[axis] || a.order - b.order);
    if (model) model.places = places;
    return places;
  };
  const pointerCandidateOrder = (first, second) => first.rank - second.rank || first.distance - second.distance || first.order - second.order;
  // The nearest painted observation, in the order of a full search: walking outward from the pointer, every rank is
  // at least the distance along the sorted axis, so no farther observation can precede the best one already found.
  const nearestPlace = (root, { list, axis, reach, left, top }, event, enlarged, withinSeries, styles) => {
    const px = event.clientX - left, py = event.clientY - top, at = axis === 'x' ? px : py, auto = !enlarged && root.dataset.cfxTooltipRange === 'auto';
    let high = 0, low = list.length, best = null;
    while (high < low) { const middle = (high + low) >> 1; if (list[middle][axis] < at) high = middle + 1; else low = middle; }
    for (low = high - 1; low >= 0 || high < list.length;) {
      const place = high >= list.length || low >= 0 && at - list[low][axis] <= list[high][axis] - at ? list[low--] : list[high++], gap = Math.abs(place[axis] - at);
      if (enlarged && gap > reach || best && gap > best.rank) break;
      if (withinSeries && !matchesLocalSeriesTarget(place.node, withinSeries)) continue;
      const area = enlarged && place.area, exact = area && px >= area.left && px <= area.right && py >= area.top && py <= area.bottom;
      if (enlarged && !exact) continue;
      const distance = Math.hypot(place.x - px, place.y - py), category = auto && place.categorical;
      const candidate = { node: place.node, x: place.x + left, y: place.y + top, distance, rank: withinSeries || category ? gap : distance, category, order: place.order, exact: !!exact };
      if ((!best || pointerCandidateOrder(candidate, best) < 0) && pointerTargetPaint(place.node, styles)) best = candidate;
    }
    return best;
  };