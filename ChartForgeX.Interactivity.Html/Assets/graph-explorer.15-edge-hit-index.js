  // Bounds use the control hull, so curved routes and self loops cannot escape
  // the broad phase. Exact distance checks retain the existing picking tolerance.
  const updateEdgeHitEntry = (entry, byId) => {
    const rendered = visualEdge(entry.edge, byId);
    const control = edgeControl(rendered);
    const endpoints = edgeRenderEndpoints(rendered, control);
    const loop = rendered.source === rendered.target ? selfLoopGeometry(rendered.source) : null;
    const route = !loop && edgeHasRoute(rendered) ? routeRenderPoints(rendered) : null;
    const points = loop ? [loop.start, loop.c1, loop.c2, loop.end]
      : route || (control ? [endpoints.source, control, endpoints.target] : [endpoints.source, endpoints.target]);
    const tolerance = Math.max(8, entry.edge.weight + 6);
    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    points.forEach(point => {
      minX = Math.min(minX, point.x); minY = Math.min(minY, point.y);
      maxX = Math.max(maxX, point.x); maxY = Math.max(maxY, point.y);
    });
    Object.assign(entry, { rendered, control, endpoints, loop, route, tolerance,
      minX: minX - tolerance, minY: minY - tolerance, maxX: maxX + tolerance, maxY: maxY + tolerance });
  };
  const refitEdgeHitTree = (tree) => {
    if (!tree) return;
    const children = tree.entries || tree.children;
    if (!tree.entries) tree.children.forEach(refitEdgeHitTree);
    tree.minX = Infinity; tree.minY = Infinity; tree.maxX = -Infinity; tree.maxY = -Infinity;
    children.forEach(child => {
      tree.minX = Math.min(tree.minX, child.minX); tree.minY = Math.min(tree.minY, child.minY);
      tree.maxX = Math.max(tree.maxX, child.maxX); tree.maxY = Math.max(tree.maxY, child.maxY);
    });
  };
  // Bulk-pack an eight-way tree with spatial tiles. Sorting each level once
  // avoids recursively sorting the same geometry at every binary split.
  const buildEdgeHitTree = (entries) => {
    if (!entries.length) return null;
    let level = entries, leaves = true;
    do {
      const sliceSize = Math.ceil(Math.sqrt(Math.ceil(level.length / 8))) * 8;
      level.sort((a, b) => a.minX + a.maxX - b.minX - b.maxX);
      const parents = [];
      for (let start = 0; start < level.length; start += sliceSize) {
        const slice = level.slice(start, start + sliceSize).sort((a, b) => a.minY + a.maxY - b.minY - b.maxY);
        for (let offset = 0; offset < slice.length; offset += 8) {
          const children = slice.slice(offset, offset + 8);
          const parent = leaves ? { entries: children } : { children };
          parent.minX = Infinity; parent.minY = Infinity; parent.maxX = -Infinity; parent.maxY = -Infinity;
          children.forEach(child => {
            parent.minX = Math.min(parent.minX, child.minX); parent.minY = Math.min(parent.minY, child.minY);
            parent.maxX = Math.max(parent.maxX, child.maxX); parent.maxY = Math.max(parent.maxY, child.maxY);
          });
          parents.push(parent);
        }
      }
      level = parents; leaves = false;
    } while (level.length > 1);
    return level[0];
  };
  const edgeHitCandidates = (root, state, point) => {
    const version = root.__cfxGraphHitVersion || 0;
    let cache = root.__cfxGraphEdgeHitIndex;
    if (!cache || cache.state !== state || cache.version !== version) {
      const started = performance.now();
      const edges = state.edges.filter(edge => visible(edge.el) && edgeHasVisibleEndpoints(edge, state.byId));
      const reuse = cache?.state === state && cache.entries.length === edges.length && cache.entries.every((entry, index) => entry.edge === edges[index]);
      const entries = reuse ? cache.entries : edges.map((edge, order) => ({ edge, order }));
      entries.forEach(entry => updateEdgeHitEntry(entry, state.byId));
      const tree = reuse ? cache.tree : buildEdgeHitTree(entries.slice());
      if (reuse) refitEdgeHitTree(tree);
      cache = { state, version, entries, tree };
      root.__cfxGraphEdgeHitIndex = cache;
      root.dataset.cfxGraphEdgeHitIndex = reuse ? 'refit' : 'built';
      const counter = reuse ? 'cfxGraphEdgeHitRefits' : 'cfxGraphEdgeHitBuilds';
      root.dataset[counter] = String(Number(root.dataset[counter] || 0) + 1);
      root.dataset.cfxGraphEdgeHitIndexMs = String(performance.now() - started);
    }
    const candidates = [], pending = cache.tree ? [cache.tree] : [];
    const contains = item => point.x >= item.minX && point.x <= item.maxX && point.y >= item.minY && point.y <= item.maxY;
    while (pending.length) {
      const tree = pending.pop();
      if (!contains(tree)) continue;
      if (tree.entries) tree.entries.forEach(entry => { if (contains(entry)) candidates.push(entry); });
      else pending.push(...tree.children);
    }
    return candidates;
  };
