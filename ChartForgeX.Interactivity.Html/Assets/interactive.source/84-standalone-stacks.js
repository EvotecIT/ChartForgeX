  // Saved stacks restack from their captured observations when legend toggles hide series. Each stacked
  // observation carries its contribution, baseline, stack group and source total. The page repeats the
  // native stack rule (series order, positive and negative sides apart, 100% shares of the visible
  // sources) and moves each mark's captured value span onto its restacked span before the window
  // projects it. Areas restack only when every member shares one set of X coordinates.
  const standaloneStacksAligned = (metadata, svg) => {
    const sets = new Map();
    svg?.querySelectorAll('[data-cfx-point][data-cfx-stack-end]').forEach(node => {
      if (metadata.series?.[Number(node.dataset.cfxSeries)]?.kind !== 'stackedarea' || node.closest('[data-cfx-role="legend-item"]')) return;
      const key = node.dataset.cfxAxis + '|' + node.dataset.cfxStackGroup, series = (sets.get(key) || sets.set(key, new Map()).get(key));
      (series.get(node.dataset.cfxSeries) || series.set(node.dataset.cfxSeries, []).get(node.dataset.cfxSeries)).push(Number(node.dataset.cfxX));
    });
    return [...sets.values()].every(series => new Set([...series.values()].map(values => values.sort((a, b) => a - b).join())).size === 1);
  };
  const standaloneStackFacts = ['cfxBase', 'cfxStackEnd', 'cfxRenderedY', 'cfxStackTotal', 'cfxStackSourceTotal'];
  const captureStandaloneStacks = view => {
    const members = [], byNode = new Map(), buckets = new Map(), columns = new Map(), plot = view.source.dataProjection.plotBounds;
    for (const point of view.points) {
      const data = point.node.dataset; if (data.cfxStackEnd === undefined) continue;
      const kind = view.source.series[point.series]?.kind, target = data.cfxNormalizedTo === undefined ? null : Number(data.cfxNormalizedTo);
      const sourceTotal = Number(data.cfxStackSourceTotal), rendered = Number(data.cfxRenderedY);
      const member = { point, node: point.node, series: point.series, kind, axis: point.axis, source: point.y, target, group: data.cfxStackGroup,
        raw: target !== null && sourceTotal ? rendered * Math.abs(sourceTotal) / target : rendered, base: Number(data.cfxBase), end: Number(data.cfxStackEnd),
        facts: standaloneStackFacts.map(key => data[key]), now: null };
      member.category = standaloneCoordinate(view.source.dataProjection[view.categoryAxis], point.x, plot, view.horizontal);
      const column = [kind, point.axis, member.group, data.cfxX].join('|'), bucket = column + '|' + (point.y >= 0);
      (buckets.get(bucket) || buckets.set(bucket, []).get(bucket)).push(member);
      (columns.get(column) || columns.set(column, { members: [] }).get(column)).members.push(member);
      member.column = columns.get(column); members.push(member); byNode.set(point.node, member); point.stack = member;
    }
    if (!members.length) return null;
    buckets.forEach(bucket => bucket.sort((a, b) => a.series - b.series));
    const series = new Map(), stacks = { members, buckets, columns: [...columns.values()], active: false };
    for (const member of members) if (member.kind === 'stackedarea') (series.get(member.series) || series.set(member.series, []).get(member.series)).push(member);
    series.forEach(rows => rows.sort((a, b) => a.category - b.category));
    for (const entry of view.geometry) {
      const owner = entry.node.closest('[data-cfx-point]'), member = byNode.get(owner);
      const area = series.get(Number(entry.node.closest('[data-cfx-role="series"]')?.dataset.cfxSeries));
      if (member && member.kind !== 'stackedarea') entry.stack = { member };
      else if (area) entry.stack = { area };
    }
    // A stack's rounded silhouette clips each of its segments; it follows the stack's own ends.
    for (const member of members) member.node.querySelectorAll('[clip-path]').forEach(node => {
      const id = node.getAttribute('clip-path').match(/#([^)]+)/)?.[1], clip = id && view.svg.getElementById?.(id);
      clip?.querySelectorAll('path,rect,polygon').forEach(shape => {
        if (view.geometry.some(entry => entry.node === shape)) return;
        view.geometry.push({ node: shape, attrs: Object.fromEntries(Array.from(shape.attributes).map(attr => [attr.name, attr.value])),
          visibility: standaloneStyle(shape, 'visibility'), marker: null, axis: view.horizontal ? 'y' : member.axis, xAxis: view.horizontal ? member.axis : 'x',
          stack: { column: member.column, member } });
      });
    });
    // Stack totals caption their stack's end; they follow the window like data labels.
    view.svg.querySelectorAll('[data-cfx-role="stack-total-label-source"]').forEach(node => {
      const box = node.getBBox(), total = view.svg.querySelector('[data-cfx-source-id="' + CSS.escape(node.dataset.cfxSourceId.replace(/-label-source$/, '')) + '"]');
      const secondary = total?.dataset.cfxAxis === 'secondary';
      view.labels.push({ node, x: box.x + box.width / 2, y: box.y + box.height / 2, transform: node.getAttribute('transform'), visibility: standaloneStyle(node, 'visibility'),
        axis: !view.horizontal && secondary ? 'secondaryY' : 'y', xAxis: view.horizontal && secondary ? 'secondaryX' : 'x',
        totalOf: total, slack: (view.horizontal ? box.width : box.height) + 8 });
      view.frames.push({ owner: node, clip: node.getAttribute('clip-path') });
    });
    for (const entry of view.labels) {
      const point = entry.totalOf || view.svg.querySelector('[data-cfx-source-id="' + CSS.escape(entry.node.dataset.cfxLabelFor) + '"]');
      const member = byNode.get(point) || null;
      if (member) entry.stack = { member, area: member.kind === 'stackedarea' ? series.get(member.series) : null };
      else if (point?.dataset.cfxRole === 'stack-total') entry.total = columns.get(['bar', 'horizontalbar'].map(kind =>
        [kind, (point.dataset.cfxAxis === 'secondary' ? view.horizontal ? 'secondaryX' : 'secondaryY' : view.horizontal ? 'x' : 'y'),
          point.dataset.cfxStackGroup, point.dataset.cfxX].join('|')).find(key => columns.has(key)));
    }
    return stacks;
  };
  const restackStandaloneView = view => {
    const stacks = view.stacks; if (!stacks) return;
    const hidden = member => !!member.node.closest('.cfx-series-muted,.cfx-series-isolated-out');
    stacks.active = stacks.members.some(hidden);
    for (const bucket of stacks.buckets.values()) {
      const visible = bucket.filter(member => !hidden(member)), target = bucket[0].target;
      const sourceTotal = visible.reduce((sum, member) => sum + member.source, 0), normalized = target !== null && sourceTotal !== 0;
      const total = normalized ? bucket[0].source >= 0 ? target : -target : visible.reduce((sum, member) => sum + member.raw, 0);
      const last = visible.filter(member => member.raw !== 0).pop();
      let baseline = 0;
      for (const member of bucket) {
        const value = hidden(member) ? 0 : normalized ? member.raw / Math.abs(sourceTotal) * target : member.raw;
        const end = normalized && member === last ? total : baseline + value;
        member.now = stacks.active ? { base: baseline, end, total, sourceTotal } : null; baseline = end;
        standaloneStackFacts.forEach((key, index) => {
          const value = member.now ? [member.now.base, end, end - member.now.base, total, sourceTotal][index] : member.facts[index];
          if (value === undefined) delete member.node.dataset[key]; else member.node.dataset[key] = value;
        });
      }
    }
    for (const column of stacks.columns) {
      const edges = rows => rows.reduce(([low, high], row) => [Math.min(low, row.base, row.end), Math.max(high, row.base, row.end)], [0, 0]);
      [column.bottom, column.top] = edges(column.members);
      [column.bottom2, column.top2] = edges(column.members.map(member => member.now || member));
    }
    paintStandaloneStacks(view);
  };
  // Restacked ink stays in the captured drawing's coordinates, so zoom and pan project it like any other mark.
  const paintStandaloneStacks = view => {
    const plot = view.source.dataProjection.plotBounds, vertical = !view.horizontal, identity = { at: value => value, scale: 1 };
    const axisOf = member => standaloneSourceAxis(view, member.axis);
    const value = (member, position) => standaloneAxisValue(axisOf(member), vertical ? 1 - (position - plot.y) / plot.height : (position - plot.x) / plot.width);
    const position = (member, number) => standaloneCoordinate(axisOf(member), number, plot, vertical);
    const shift = (member, number) => {
      const now = member.now || member, span = member.end - member.base;
      return now.base + (number - member.base) * (span ? (now.end - now.base) / span : 1);
    };
    const map = member => ({ at: at => position(member, shift(member, value(member, at))), scale: member.end !== member.base && member.now ? (member.now.end - member.now.base) / (member.end - member.base) : 1 });
    // An area vertex follows the observation at its X whose captured edge it draws (steps draw both
    // neighbours at one X); curve controls between observations interpolate their neighbours' shifts.
    const pair = rows => (at, level) => {
      const edge = row => Math.min(Math.abs(level - position(row, row.base)), Math.abs(level - position(row, row.end)));
      let next = rows.findIndex(row => row.category >= at - .01); if (next < 0) next = rows.length - 1;
      const same = rows.filter(row => Math.abs(row.category - at) <= .01);
      const near = [...same, rows[next - 1], rows[next + same.length]].filter(Boolean).find(row => edge(row) <= .5);
      const move = row => position(row, shift(row, value(row, level)));
      if (near || same.length || !next || rows[next].category < at) return [at, move(near || same[0] || rows[next])];
      const before = rows[next - 1], after = rows[next], ratio = (at - before.category) / (after.category - before.category);
      return [at, move(before) * (1 - ratio) + move(after) * ratio];
    };
    const project = (attrs, along, pairs) => {
      const out = projectStandaloneAttrs(attrs, vertical ? identity : along, vertical ? along : identity, pairs);
      const [start, size] = vertical ? ['y', 'height'] : ['x', 'width'];
      if (along && attrs[start] !== undefined && attrs[size] !== undefined) {
        const first = along.at(Number(attrs[start])), last = along.at(Number(attrs[start]) + Number(attrs[size]));
        out[start] = Math.min(first, last); out[size] = Math.abs(last - first);
      }
      return out;
    };
    const active = view.stacks.active;
    for (const entry of view.geometry) {
      const stack = entry.stack; if (!stack) continue;
      entry.current = entry.currentMarker = null;
      if (!active) continue;
      if (stack.column) {
        const { column, member } = stack;
        // Each silhouette vertex keeps to the nearest of the stack's ends or its zero baseline.
        const ends = [[column.bottom, column.bottom2], [0, 0], [column.top, column.top2]];
        entry.current = project(entry.attrs, { scale: 1, at: at => {
          const number = value(member, at), [from, to] = ends.reduce((best, end) => Math.abs(number - end[0]) < Math.abs(number - best[0]) ? end : best);
          return position(member, number + to - from);
        } });
      } else if (stack.member) entry.current = project(entry.attrs, map(stack.member));
      else if (entry.marker) { const [x, y] = pair(stack.area)(entry.marker.x, entry.marker.y); entry.currentMarker = { x, y }; }
      else if (entry.attrs.d) entry.current = project(entry.attrs, null, pair(stack.area));
      else {
        // Point targets beside an area keep their size and follow their centre.
        const { x = 0, y = 0, width = 0, height = 0, cx, cy } = Object.fromEntries(Object.entries(entry.attrs).map(([key, number]) => [key, Number(number)]));
        const centre = [cx ?? x + width / 2, cy ?? y + height / 2], moved = pair(stack.area)(...centre);
        entry.current = project(entry.attrs, { at: at => at + moved[1] - centre[1], scale: 1 });
      }
    }
    for (const entry of view.labels) {
      entry.current = null; entry.hiddenTotal = active && !!entry.total && (entry.total.top2 !== entry.total.top || entry.total.bottom2 !== entry.total.bottom);
      if (!active || !entry.stack) continue;
      const { member, area } = entry.stack;
      const [x, y] = area ? pair(area)(entry.x, entry.y) : vertical ? [entry.x, map(member).at(entry.y)] : [map(member).at(entry.x), entry.y];
      entry.current = { x, y };
    }
    for (const member of view.stacks.members) {
      const point = member.point, area = member.kind === 'stackedarea' && view.stacks.members.filter(row => row.series === member.series).sort((a, b) => a.category - b.category);
      point.stackRegion = active && point.region ? Object.freeze({ ...point.region, ...project(point.region, map(member)) }) : null;
      point.stackInteriors = active && point.interiors && area ? point.interiors.map(([x, y, radius]) => [...pair(area)(x, y), radius]) : null;
    }
  };
