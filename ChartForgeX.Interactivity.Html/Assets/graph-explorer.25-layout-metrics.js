  const assessOverlaps = (nodes, includeLabels = nodes.length < 500, maximumComparisons = 250000) => {
    // Cache each radius once. Sweep bounds rather than sampling the first nodes.
    const items = nodes.map(node => ({ x: node.x, y: node.y, radius: nodeRadius(node, includeLabels) }));
    let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
    items.forEach(item => {
      minX = Math.min(minX, item.x); maxX = Math.max(maxX, item.x);
      minY = Math.min(minY, item.y); maxY = Math.max(maxY, item.y);
    });
    // Sweep the wider axis so tall, sparse layouts do not spend their budget on x ties.
    const vertical = maxY - minY > maxX - minX;
    items.forEach(item => {
      const coordinate = vertical ? item.y : item.x;
      item.left = coordinate - item.radius; item.right = coordinate + item.radius;
    });
    items.sort((a, b) => a.left - b.left);
    let overlaps = 0, comparisons = 0;
    for (let i = 0; i < items.length; i++) {
      const a = items[i];
      for (let j = i + 1; j < items.length && items[j].left < a.right; j++) {
        if (comparisons >= maximumComparisons) return { overlaps, comparisons, complete: false, nodeCount: nodes.length, includeLabels };
        comparisons++;
        const b = items[j], radius = a.radius + b.radius;
        const dx = b.x - a.x, dy = b.y - a.y;
        if (dx * dx + dy * dy < radius * radius) overlaps++;
      }
    }
    return { overlaps, comparisons, complete: true, nodeCount: nodes.length, includeLabels };
  };

  const layoutQualityMetrics = (root, state) => {
    const nodes = state.nodes;
    if (!nodes.length) return;
    const size = sceneSize(root);
    const minX = Math.min(...nodes.map(node => node.x));
    const maxX = Math.max(...nodes.map(node => node.x));
    const minY = Math.min(...nodes.map(node => node.y));
    const maxY = Math.max(...nodes.map(node => node.y));
    const centerX = (minX + maxX) / 2;
    const centerY = (minY + maxY) / 2;
    const drift = Math.hypot(centerX - size.centerX, centerY - size.centerY);
    const assessment = assessOverlaps(nodes);
    const overlapProbe = assessment.overlaps;
    const driftPenalty = Math.min(1, drift / Math.max(1, Math.min(size.width, size.height) * 0.5));
    const overlapTolerance = nodes.length >= 300 ? nodes.length * 24 : nodes.length * 10;
    const score = Math.max(0, 1 - driftPenalty - Math.min(0.35, overlapProbe / Math.max(1, overlapTolerance)));
    root.dataset.cfxGraphLayoutBounds = `${(maxX - minX).toFixed(1)}x${(maxY - minY).toFixed(1)}`;
    root.dataset.cfxGraphLayoutCenterDrift = drift.toFixed(2);
    root.dataset.cfxGraphLayoutOverlapCount = String(overlapProbe);
    root.dataset.cfxGraphLayoutOverlapCoverage = assessment.complete ? 'complete' : 'budget-limited';
    root.dataset.cfxGraphLayoutOverlapComparisons = String(assessment.comparisons);
    root.dataset.cfxGraphLayoutOverlapNodeCount = String(assessment.nodeCount);
    root.dataset.cfxGraphLayoutOverlapGeometry = assessment.includeLabels ? 'estimated-label-radius' : 'node-radius';
    root.dataset.cfxGraphLayoutQualityScore = score.toFixed(3);
    root.dataset.cfxGraphLayoutQuality = assessment.complete && overlapProbe === 0 && score >= 0.82 ? 'centered-structured' : 'needs-review';
  };

