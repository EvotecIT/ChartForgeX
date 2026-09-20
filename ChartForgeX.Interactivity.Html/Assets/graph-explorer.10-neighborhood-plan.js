  const neighborhoodOptions = (configuration = {}) => {
    const result = { hops: 1, maximumNodes: 13, maximumEdges: 24, neighborOffset: 0, ...configuration };
    for (const [name, minimum] of [['hops', 0], ['maximumNodes', 2], ['maximumEdges', 0], ['neighborOffset', 0]]) {
      if (!Number.isSafeInteger(result[name]) || result[name] < minimum) throw new RangeError(`Invalid neighborhood ${name}.`);
    }
    return result;
  };
  // Mirrors GraphSceneNeighborhoodPlanner: hidden objects remain traversable,
  // but only eligible objects consume the visible budgets. No geometry changes.
  const planGraphNeighborhood = (nodes, edges, rootNodeId, configuration) => {
    const options = neighborhoodOptions(configuration), byId = new Map(nodes.map(node => [node.id, node]));
    if (!byId.has(rootNodeId) || byId.get(rootNodeId).visible === false) return null;
    const adjacency = new Map(nodes.map(node => [node.id, new Set()]));
    for (const edge of edges) {
      if (!adjacency.has(edge.sourceId) || !adjacency.has(edge.targetId)) continue;
      adjacency.get(edge.sourceId).add(edge.targetId); adjacency.get(edge.targetId).add(edge.sourceId);
    }
    const distances = new Map([[rootNodeId, 0]]), queue = [rootNodeId];
    for (let cursor = 0; cursor < queue.length; cursor++) {
      const id = queue[cursor], depth = distances.get(id);
      if (depth >= options.hops) continue;
      for (const neighbor of adjacency.get(id)) {
        if (distances.has(neighbor)) continue;
        distances.set(neighbor, depth + 1); queue.push(neighbor);
      }
    }
    const ordinal = (a, b) => a < b ? -1 : a > b ? 1 : 0;
    const candidates = queue.filter(id => id !== rootNodeId && byId.get(id).visible !== false)
      .sort((a, b) => distances.get(a) - distances.get(b) || ordinal(a, b));
    const nodeIds = new Set([rootNodeId, ...candidates.slice(options.neighborOffset, options.neighborOffset + options.maximumNodes - 1)]);
    const induced = edges.filter(edge => edge.visible !== false && nodeIds.has(edge.sourceId) && nodeIds.has(edge.targetId));
    const nearest = edge => Math.min(distances.get(edge.sourceId), distances.get(edge.targetId));
    const furthest = edge => Math.max(distances.get(edge.sourceId), distances.get(edge.targetId));
    induced.sort((a, b) => nearest(a) - nearest(b) || furthest(a) - furthest(b) || ordinal(a.id, b.id));
    const edgeIds = new Set(induced.slice(0, options.maximumEdges).map(edge => edge.id));
    return { rootNodeId, ...options, nodeIds, edgeIds, scopeNodeCount: candidates.length + 1,
      hiddenNodeCount: nodes.length - nodeIds.size, hiddenEdgeCount: edges.length - edgeIds.size,
      boundaryEdgeCount: edges.filter(edge => nodeIds.has(edge.sourceId) !== nodeIds.has(edge.targetId)).length,
      hasPrevious: options.neighborOffset > 0, hasNext: options.neighborOffset + options.maximumNodes - 1 < candidates.length };
  };
