  // A fresh index belongs to one paint/bounds pass, so filters, dragging and
  // graph patches cannot reuse stale coordinates or hidden member details.
  const graphEdgeLabelObstacles = (root, state) => {
    const byId = state.byId || new Map(state.nodes.map(node => [node.id, node]));
    const nodes = state.nodes.filter(node => visible(node.el));
    state.clusters.forEach(cluster => {
      const proxy = clusterProxyNode(cluster, byId);
      if (proxy) nodes.push(proxy);
    });
    return buildGraphBoundsTree(nodes.flatMap(node => {
      const body = nodeShapeExtents(node), boxes = [];
      const add = (x, y, halfWidth, halfHeight) => boxes.push({ minX: node.x + x - halfWidth, minY: node.y + y - halfHeight,
        maxX: node.x + x + halfWidth, maxY: node.y + y + halfHeight });
      if (attr(node.el, 'data-cfx-role') === 'graph-cluster') { add(0, 0, body.x, body.y); return boxes; }
      if (node.card || ['image', 'imageRect', 'text'].includes(node.shape)) {
        const extents = nodeLayoutExtents(node, false); add(0, 0, extents.x, extents.y); return boxes;
      }
      // Layout/physics use a symmetric envelope. Painted captions are below
      // ordinary marks, so that envelope must not invent obstacles above them
      // or fill the clear horizontal space between neighboring bodies.
      add(0, 0, body.x + 2, body.y + 2);
      const label = node.label || attr(node.el, 'data-node-label');
      const secondary = node.secondaryLabel || attr(node.el, 'data-node-secondary-label');
      if (label) add(0, node.size + 15, Math.max(24, graphEstimatedCardTextWidth(label) / 2 + 6,
        node.labelBackgroundColor ? label.length * 3.8 : 0), 11);
      if (secondary) add(0, node.size + 31, graphEstimatedCardTextWidth(secondary, true) / 2 + 6, 10);
      if (node.badge || attr(node.el, 'data-node-badge')) {
        const badge = graphBadgeGeometry(node, root); add(badge.x, badge.y, badge.width / 2 + 1, 9);
      }
      const status = attr(node.el, 'data-cfx-status');
      if (status && status !== 'unknown') add(-node.size * .8, -node.size * .8, 5.5, 5.5);
      if (node.el.classList.contains('cfx-graph-neighborhood-primary')) {
        const ring = graphNodePrimaryRingPaint(node); add(0, 0, ring.radius + ring.width / 2, ring.radius + ring.width / 2);
      }
      return boxes;
    }));
  };
  const edgeLabelIntersectsObstacles = (point, label, obstacles) => {
    const halfWidth = Math.max(14, graphEstimatedEdgeLabelWidth(label) / 2);
    const query = { minX: point.x - halfWidth - 6, maxX: point.x + halfWidth + 6, minY: point.y - 20, maxY: point.y + 12 };
    return graphBoundsCandidates(obstacles, query).some(obstacle =>
      query.minX < obstacle.maxX && query.maxX > obstacle.minX && query.minY < obstacle.maxY && query.maxY > obstacle.minY);
  };
  const syncGraphEdgeGeometry = (path, label, rendered, control, obstacles) => {
    path.setAttribute('d', edgePathData(rendered, control));
    if (!label) return;
    const point = edgeLabelPoint(rendered, control, obstacles);
    label.setAttribute('x', point.x.toFixed(3));
    label.setAttribute('y', point.y.toFixed(3));
  };
