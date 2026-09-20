  const graphPatchIds = (values) => new Set((values || []).map(value => String(value)));
  const graphPatchHas = (value, name) => Object.prototype.hasOwnProperty.call(value || {}, name);
  const graphPatchClusters = (root, patch, nodeIds, removedNodes) => {
    const removedClusters = graphPatchIds(patch.removeClusterIds);
    const clusters = new Map(items(root, '[data-cfx-role="graph-cluster"]').filter(cluster => !removedClusters.has(attr(cluster, 'data-cluster-id'))).map(cluster => [attr(cluster, 'data-cluster-id'), { parentId: attr(cluster, 'data-cluster-parent'), nodeIds: new Set(idList(attr(cluster, 'data-cluster-node-ids'))) }]));
    if (patch.removeIncidentReferences !== false) clusters.forEach(cluster => { cluster.nodeIds = new Set(Array.from(cluster.nodeIds).filter(id => !removedNodes.has(id))); });
    (patch.upsertClusters || []).forEach(cluster => {
      if (!cluster?.id) throw new Error('Graph patch clusters require stable ids.');
      clusters.set(String(cluster.id), { parentId: String(cluster.parentClusterId || ''), nodeIds: new Set((cluster.nodeIds || []).map(String)) });
    });
    (patch.upsertNodes || []).filter(node => graphPatchHas(node, 'clusterId')).forEach(node => {
      const nodeId = String(node.id); clusters.forEach(cluster => cluster.nodeIds.delete(nodeId));
      const clusterId = String(node.clusterId || '');
      if (clusterId) {
        if (!clusters.has(clusterId)) throw new Error(`Graph patch node '${nodeId}' references missing cluster '${clusterId}'.`);
        clusters.get(clusterId).nodeIds.add(nodeId);
      }
    });
    clusters.forEach((cluster, clusterId) => {
      if (cluster.parentId && removedClusters.has(cluster.parentId) && !clusters.has(cluster.parentId)) cluster.parentId = '';
      if (cluster.parentId && !clusters.has(cluster.parentId)) throw new Error(`Graph patch cluster '${clusterId}' references missing parent '${cluster.parentId}'.`);
      cluster.nodeIds.forEach(nodeId => { if (!nodeIds.has(nodeId)) throw new Error(`Graph patch cluster '${clusterId}' references missing node '${nodeId}'.`); });
    });
    return clusters;
  };
  const validateGraphPatch = (root, patch) => {
    const removedNodes = graphPatchIds(patch.removeNodeIds);
    const nodes = new Map(items(root, '[data-cfx-role="graph-node"]').filter(node => !removedNodes.has(attr(node, 'data-node-id'))).map(node => [attr(node, 'data-node-id'), { parentId: attr(node, 'data-node-parent'), clusterId: attr(node, 'data-node-cluster') }]));
    (patch.upsertNodes || []).forEach(node => { if (!node?.id) throw new Error('Graph patch nodes require stable ids.'); nodes.set(String(node.id), { parentId: String(node.parentId || ''), clusterId: String(node.clusterId || '') }); });
    const nodeIds = new Set(nodes.keys());
    nodes.forEach((node, nodeId) => { if (node.parentId && !nodeIds.has(node.parentId)) throw new Error(`Graph patch node '${nodeId}' references missing parent '${node.parentId}'.`); });
    const clusters = graphPatchClusters(root, patch, nodeIds, removedNodes);
    const removedClusters = graphPatchIds(patch.removeClusterIds);
    const declaredMembership = new Map();
    clusters.forEach((cluster, clusterId) => cluster.nodeIds.forEach(nodeId => {
      if (declaredMembership.has(nodeId) && declaredMembership.get(nodeId) !== clusterId) throw new Error(`Graph patch node '${nodeId}' belongs to multiple clusters.`);
      declaredMembership.set(nodeId, clusterId);
    }));
    nodes.forEach((node, nodeId) => {
      if (node.clusterId && removedClusters.has(node.clusterId) && !clusters.has(node.clusterId)) node.clusterId = '';
      if (node.clusterId && !clusters.has(node.clusterId)) throw new Error(`Graph patch node '${nodeId}' references missing cluster '${node.clusterId}'.`);
      if (node.clusterId && declaredMembership.has(nodeId) && declaredMembership.get(nodeId) !== node.clusterId) throw new Error(`Graph patch node '${nodeId}' has conflicting cluster memberships.`);
    });
    const removedEdges = graphPatchIds(patch.removeEdgeIds);
    const edges = new Map();
    items(root, '[data-cfx-role="graph-edge"]').forEach(edge => {
      const id = attr(edge, 'data-edge-id'), source = attr(edge, 'data-source-node-id'), target = attr(edge, 'data-target-node-id');
      if (removedEdges.has(id) || (patch.removeIncidentReferences !== false && (removedNodes.has(source) || removedNodes.has(target)))) return;
      edges.set(id, { source, target });
    });
    (patch.upsertEdges || []).forEach(edge => {
      if (!edge?.id) throw new Error('Graph patch edges require stable ids.');
      const source = String(edge.sourceNodeId || edge.source || ''), target = String(edge.targetNodeId || edge.target || '');
      edges.set(String(edge.id), { source, target });
    });
    edges.forEach((edge, edgeId) => { if (!nodeIds.has(edge.source) || !nodeIds.has(edge.target)) throw new Error(`Graph patch edge '${edgeId}' references a missing endpoint.`); });
  };
  const detachGraphPatchRemovedClusters = (root, removedClusters, upsertClusters) => {
    if (!removedClusters.size) return;
    const restored = new Set((upsertClusters || []).map(cluster => String(cluster.id)));
    items(root, '[data-cfx-role="graph-node"]').forEach(node => {
      const clusterId = attr(node, 'data-node-cluster');
      if (removedClusters.has(clusterId) && !restored.has(clusterId)) setGraphAttribute(node, 'data-node-cluster', null);
    });
    items(root, '[data-cfx-role="graph-cluster"]').forEach(cluster => {
      const parentId = attr(cluster, 'data-cluster-parent');
      if (removedClusters.has(parentId) && !restored.has(parentId)) setGraphAttribute(cluster, 'data-cluster-parent', null);
    });
  };
  const syncGraphPatchClusterMembership = (root, nodes) => {
    const moved = (nodes || []).filter(node => graphPatchHas(node, 'clusterId'));
    const clusters = items(root, '[data-cfx-role="graph-cluster"]');
    if (moved.length) {
      const movedIds = new Set(moved.map(node => String(node.id)));
      const clustersById = new Map(clusters.map(cluster => [attr(cluster, 'data-cluster-id'), cluster]));
      clusters.forEach(cluster => setGraphAttribute(cluster, 'data-cluster-node-ids', idList(attr(cluster, 'data-cluster-node-ids')).filter(id => !movedIds.has(id)).join(',')));
      moved.forEach(node => {
        const clusterId = String(node.clusterId || '');
        if (!clusterId) return;
        const cluster = clustersById.get(clusterId);
        const ids = new Set(idList(attr(cluster, 'data-cluster-node-ids'))); ids.add(String(node.id)); setGraphAttribute(cluster, 'data-cluster-node-ids', Array.from(ids).join(','));
      });
    }
    const membership = new Map();
    clusters.forEach(cluster => idList(attr(cluster, 'data-cluster-node-ids')).forEach(nodeId => membership.set(nodeId, attr(cluster, 'data-cluster-id'))));
    items(root, '[data-cfx-role="graph-node"]').forEach(node => { const clusterId = attr(node, 'data-node-cluster'); if (clusterId) membership.set(attr(node, 'data-node-id'), clusterId); });
    items(root, '[data-cfx-role="graph-edge"]').forEach(edge => {
      setGraphAttribute(edge, 'data-source-cluster-id', membership.get(attr(edge, 'data-source-node-id')) || null);
      setGraphAttribute(edge, 'data-target-cluster-id', membership.get(attr(edge, 'data-target-node-id')) || null);
    });
  };
