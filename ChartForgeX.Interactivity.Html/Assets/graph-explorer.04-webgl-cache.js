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
    const stamp = JSON.stringify([dense, moving, scale, compact, pointLimit, palette.edge, palette.selected, palette.paper]);
    if (webGlMeshCurrent(root.__cfxGraphEdgeMesh, state, stamp)) return root.__cfxGraphEdgeMesh;
    const mesh = { positions: [], colors: [], fallbackEdges: [], nodes: state.nodes, edges: state.edges,
      clusters: state.clusters, stamp, coordinates: new Float64Array(state.nodes.length * 2),
      items: [...state.nodes, ...state.edges, ...state.clusters] };
    mesh.classes = mesh.items.map(item => attr(item.el, 'class'));
    state.nodes.forEach((node, index) => { mesh.coordinates[index * 2] = node.x; mesh.coordinates[index * 2 + 1] = node.y; });
    mesh.nodePoints = webGlNodePoints(state, palette, compact, moving, scale, pointLimit);
    mesh.clusterMetrics = new Map(state.clusters.map(cluster => [cluster, clusterMetrics(cluster, byId)]));
    state.edges.forEach(edge => {
      if (!visible(edge.el) || !edgeHasVisibleEndpoints(edge, byId)) return;
      const paint = graphEdgePaint(root, edge, byId, palette, dense, moving);
      const points = webGlEdgePoints(paint.rendered, paint.control, scale * 4);
      const paths = edge.dashed ? webGlDashedPaths(points, edge.dashPattern) : [points];
      if (!paths) { mesh.fallbackEdges.push({ edge, paint }); return; }
      const color = webGlColor(paint.color, paint.alpha);
      paths.forEach(path => webGlStrokePath(mesh, path, paint.width, color));
      if (paint.arrows) {
        const arrowColor = webGlColor(paint.color, paint.arrowAlpha);
        for (const side of ['source', 'target']) {
          if (side === 'source' ? !edge.sourceArrow : !edge.targetArrow && !edge.directed) continue;
          const arrow = edgeArrowGeometry(paint.rendered, paint.control, side, 8);
          webGlTriangle(mesh, arrow.tip, arrow.left, arrow.right, arrowColor);
        }
      }
    });
    root.__cfxGraphEdgeMesh = mesh;
    return mesh;
  };
