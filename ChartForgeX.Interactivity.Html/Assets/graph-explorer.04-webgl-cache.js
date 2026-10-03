  // Panning and zooming transform retained GPU geometry. Patches replace the
  // canonical arrays; physics, dragging and visibility/selection update the
  // coordinates or classes checked here before reusing a mesh.
  const webGlMeshCurrent = (cache, state, stamp) => {
    if (!cache || cache.nodes !== state.nodes || cache.edges !== state.edges || cache.clusters !== state.clusters || cache.stamp !== stamp) return false;
    for (let index = 0; index < state.nodes.length; index++) {
      const node = state.nodes[index];
      if (cache.coordinates[index * 2] !== node.x || cache.coordinates[index * 2 + 1] !== node.y) return false;
    }
    for (let index = 0; index < cache.items.length; index++) if (cache.classes[index] !== attr(cache.items[index].el, 'class')) return false;
    return true;
  };
  const webGlEdgeMesh = (root, state, byId, palette, dense, moving, scale, compact, pointLimit) => {
    const movingScale = moving ? viewport(root).scale : 0;
    const stamp = JSON.stringify([dense, moving, movingScale, scale, compact, pointLimit, palette.edge, palette.selected, palette.paper]);
    if (webGlMeshCurrent(root.__cfxGraphEdgeMesh, state, stamp)) return root.__cfxGraphEdgeMesh;
    const mesh = { positions: [], colors: [], linePositions: [], lineColors: [], batches: [], fallbackEdges: [], nodes: state.nodes, edges: state.edges,
      clusters: state.clusters, stamp, coordinates: new Float64Array(state.nodes.length * 2),
      items: [...state.nodes, ...state.edges, ...state.clusters] };
    mesh.classes = mesh.items.map(item => attr(item.el, 'class'));
    state.nodes.forEach((node, index) => { mesh.coordinates[index * 2] = node.x; mesh.coordinates[index * 2 + 1] = node.y; });
    mesh.nodePoints = webGlNodePoints(state, palette, compact, moving, scale, pointLimit);
    mesh.clusterMetrics = new Map(state.clusters.map(cluster => [cluster, clusterMetrics(cluster, byId)]));
    const colors = new Map();
    const edgeColor = (paint) => {
      const key = `${paint.color}:${paint.alpha}`;
      if (!colors.has(key)) colors.set(key, webGlColor(paint.color, paint.alpha));
      return colors.get(key);
    };
    const entries = state.edges.filter(edge => visible(edge.el) && edgeHasVisibleEndpoints(edge, byId)).map(edge => {
      const paint = graphEdgePaint(root, edge, byId, palette, dense, moving);
      const points = webGlEdgePoints(paint.rendered, paint.control, scale * 4);
      const paths = webGlStrokeSupported(points) ? edge.dashed ? webGlDashedPaths(points, edge.dashPattern, paint.width) : [points] : null;
      return { edge, paint, paths };
    });
    let fallback = new Set(entries.filter(entry => !entry.paths));
    if (fallback.size) {
      const margin = .5 / Math.max(.001, scale * .2);
      entries.forEach(entry => updateGraphEdgeBounds(entry, byId, entry.paint.width * 5 + 8 + margin));
      fallback = graphBoundsFallback(entries, fallback);
      mesh.fallbackEdges = entries.filter(entry => fallback.has(entry));
    }
    const batch = (mode, start, count) => {
      if (!count) return;
      const previous = mesh.batches[mesh.batches.length - 1];
      if (previous?.mode === mode && previous.start + previous.count === start) previous.count += count;
      else mesh.batches.push({ mode, start, count });
    };
    entries.forEach(entry => {
      if (fallback.has(entry)) return;
      const { edge, paint, paths } = entry;
      const color = edgeColor(paint), coverage = paint.width * scale * movingScale;
      const thin = moving && dense && coverage < 1;
      paths.forEach(path => {
        const lines = thin && path.length === 2, start = (lines ? mesh.linePositions : mesh.positions).length / 2;
        if (lines) webGlThinLine(mesh, path[0], path[1], color, coverage);
        else webGlStrokePath(mesh, path, paint.width, color, !thin);
        batch(lines ? 'LINES' : 'TRIANGLES', start, (lines ? mesh.linePositions : mesh.positions).length / 2 - start);
      });
      if (paint.arrows) {
        const start = mesh.positions.length / 2;
        const arrowColor = webGlColor(paint.color, paint.arrowAlpha);
        for (const side of ['source', 'target']) {
          if (side === 'source' ? !edge.sourceArrow : !edge.targetArrow && !edge.directed) continue;
          const arrow = edgeArrowGeometry(paint.rendered, paint.control, side, 8);
          webGlTriangle(mesh, arrow.tip, arrow.left, arrow.right, arrowColor);
        }
        batch('TRIANGLES', start, mesh.positions.length / 2 - start);
      }
    });
    mesh.triangleVertices = mesh.positions.length / 2;
    mesh.lineVertices = mesh.linePositions.length / 2;
    mesh.batches.forEach(item => { if (item.mode === 'LINES') item.start += mesh.triangleVertices; });
    if (mesh.lineVertices) {
      mesh.positions = mesh.positions.concat(mesh.linePositions);
      mesh.colors = mesh.colors.concat(mesh.lineColors);
    }
    root.__cfxGraphEdgeMesh = mesh;
    return mesh;
  };
