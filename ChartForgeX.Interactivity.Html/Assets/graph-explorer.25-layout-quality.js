  const layoutCommunityKey = (node) => node.cluster ? `cluster:${node.cluster}` : node.groupId ? `group:${node.groupId}` : node.kind ? `kind:${node.kind}` : 'graph';

  const restoreClusterAnchors = (root, state) => {
    const groups = new Map();
    state.nodes.forEach(node => {
      const key = layoutCommunityKey(node);
      const group = groups.get(key) || { key, nodes: [], homeX: 0, homeY: 0, x: 0, y: 0, clustered: !!node.cluster };
      group.nodes.push(node);
      group.homeX += node.homeX;
      group.homeY += node.homeY;
      group.x += node.x;
      group.y += node.y;
      group.clustered = group.clustered || !!node.cluster;
      groups.set(key, group);
    });
    const strength = state.nodes.length >= 500 ? 0.66 : state.nodes.length >= 80 ? 0.72 : 0.42;
    let adjusted = 0;
    let clustered = 0;
    groups.forEach(group => {
      if (group.nodes.length < 2) return;
      const count = group.nodes.length;
      const dx = group.homeX / count - group.x / count;
      const dy = group.homeY / count - group.y / count;
      if (Math.hypot(dx, dy) < 1) return;
      group.nodes.forEach(node => {
        if (node.fixed) return;
        node.x += dx * strength;
        node.y += dy * strength;
      });
      adjusted += 1;
      if (group.clustered) clustered += 1;
    });
    if (adjusted) root.dataset.cfxGraphLayoutCommunityGravity = strength.toFixed(2);
    if (clustered) root.dataset.cfxGraphLayoutClusterGravity = strength.toFixed(2);
  };

  const spreadHubNeighborhoods = (root, state) => {
    if (state.nodes.length < 8 || !state.edges.length) return;
    const neighbors = new Map(state.nodes.map(node => [node.id, new Set()]));
    state.edges.filter(edge => edge.physics !== false).forEach(edge => {
      neighbors.get(edge.source.id)?.add(edge.target.id);
      neighbors.get(edge.target.id)?.add(edge.source.id);
    });
    const hubs = state.nodes
      .map(node => ({ node, degree: neighbors.get(node.id)?.size || 0 }))
      .filter(item => item.degree >= Math.max(4, Math.sqrt(state.nodes.length) * 0.45))
      .sort((a, b) => b.degree - a.degree || a.node.id.localeCompare(b.node.id))
      .slice(0, state.nodes.length >= 1000 ? 12 : 8);
    let moved = 0;
    hubs.forEach((hub, hubIndex) => {
      const neighborNodes = Array.from(neighbors.get(hub.node.id) || [])
        .map(id => state.byId.get(id))
        .filter(node => node && !node.fixed)
        .sort((a, b) => layoutCommunityKey(a).localeCompare(layoutCommunityKey(b)) || a.id.localeCompare(b.id));
      const radius = Math.min(220, Math.max(72, Math.sqrt(neighborNodes.length) * 28 + hub.node.size * 2));
      neighborNodes.forEach((node, index) => {
        const angle = -Math.PI / 2 + Math.PI * 2 * (index + hubIndex / Math.max(1, hubs.length)) / Math.max(1, neighborNodes.length);
        const targetX = hub.node.x + Math.cos(angle) * radius;
        const targetY = hub.node.y + Math.sin(angle) * radius * 0.72;
        const pull = state.nodes.length >= 1000 ? 0.14 : 0.22;
        node.x += (targetX - node.x) * pull;
        node.y += (targetY - node.y) * pull;
        moved += 1;
      });
    });
    if (moved) root.dataset.cfxGraphLayoutHubSpread = String(moved);
  };

  const spreadCommunityAreas = (root, state) => {
    const groups = new Map();
    state.nodes.forEach(node => {
      const key = layoutCommunityKey(node);
      const group = groups.get(key) || { key, nodes: [], x: 0, y: 0, radius: 0 };
      group.nodes.push(node);
      group.x += node.x;
      group.y += node.y;
      groups.set(key, group);
    });
    const ordered = Array.from(groups.values()).filter(group => group.nodes.length > 1);
    if (ordered.length < 2 || ordered.length > 96) return 0;
    ordered.forEach(group => {
      group.x /= group.nodes.length;
      group.y /= group.nodes.length;
      group.radius = Math.max(48, Math.min(220, Math.sqrt(group.nodes.length) * 22));
    });
    let moved = 0;
    for (let i = 0; i < ordered.length; i++) {
      for (let j = i + 1; j < ordered.length; j++) {
        const a = ordered[i];
        const b = ordered[j];
        let dx = b.x - a.x;
        let dy = b.y - a.y;
        let distance = Math.hypot(dx, dy);
        const minDistance = Math.min(380, (a.radius + b.radius) * 0.88);
        if (distance >= minDistance) continue;
        if (distance < 0.001) {
          const seed = (a.key.length * 19 + b.key.length * 29) % 360;
          dx = Math.cos(seed);
          dy = Math.sin(seed);
          distance = 1;
        }
        const push = (minDistance - distance) / distance * 0.24;
        const fx = dx * push;
        const fy = dy * push;
        a.nodes.forEach(node => { if (!node.fixed) { node.x -= fx; node.y -= fy; } });
        b.nodes.forEach(node => { if (!node.fixed) { node.x += fx; node.y += fy; } });
        moved += 1;
      }
    }
    if (moved) root.dataset.cfxGraphLayoutCommunitySpread = String(moved);
    return moved;
  };

  const nodeRadius = (node, includeLabels) => nodeCollisionRadius(node, includeLabels);

  const separateOverlaps = (root, state, passes) => {
    const movable = state.nodes.filter(node => !node.fixed);
    if (movable.length < 2) return 0;
    const includeLabels = state.nodes.length < 500;
    const cellSize = Math.max(28, Math.min(220, movable.reduce((max, node) => Math.max(max, nodeRadius(node, includeLabels) * 2 + 10), 0)));
    const maxPairs = state.nodes.length >= 3000 ? 120000 : state.nodes.length >= 1000 ? 180000 : 220000;
    let totalResolved = 0;
    for (let pass = 0; pass < passes; pass++) {
      const grid = new Map();
      movable.forEach(node => {
        const key = `${Math.floor(node.x / cellSize)},${Math.floor(node.y / cellSize)}`;
        const bucket = grid.get(key) || [];
        bucket.push(node);
        grid.set(key, bucket);
      });
      let pairs = 0;
      let resolved = 0;
      let exhausted = false;
      for (const node of movable) {
        if (exhausted) break;
        const gx = Math.floor(node.x / cellSize);
        const gy = Math.floor(node.y / cellSize);
        for (let ox = -1; ox <= 1; ox++) {
          if (exhausted) break;
          for (let oy = -1; oy <= 1; oy++) {
            if (exhausted) break;
            const bucket = grid.get(`${gx + ox},${gy + oy}`) || [];
            for (const other of bucket) {
              if (other === node || other.id < node.id) continue;
              pairs += 1;
              if (pairs > maxPairs) { exhausted = true; break; }
              const minDistance = nodeRadius(node, includeLabels) + nodeRadius(other, includeLabels);
              let dx = other.x - node.x;
              let dy = other.y - node.y;
              let distance = Math.hypot(dx, dy);
              if (distance >= minDistance) continue;
              if (distance < 0.001) {
                const seed = (node.id.length * 17 + other.id.length * 31) % 360;
                dx = Math.cos(seed);
                dy = Math.sin(seed);
                distance = 1;
              }
              const push = (minDistance - distance) / distance * 0.5;
              const fx = dx * push;
              const fy = dy * push;
              node.x -= fx;
              node.y -= fy;
              other.x += fx;
              other.y += fy;
              resolved += 1;
            }
          }
        }
      }
      totalResolved += resolved;
      if (!resolved) break;
    }
    if (totalResolved) root.dataset.cfxGraphLayoutOverlapResolved = String(totalResolved);
    return totalResolved;
  };

  const countOverlaps = (nodes) => {
    const probe = nodes.slice(0, Math.min(nodes.length, 900));
    const includeLabels = nodes.length < 500;
    let overlaps = 0;
    for (let i = 0; i < probe.length; i++) {
      for (let j = i + 1; j < probe.length; j++) {
        const minDistance = nodeRadius(probe[i], includeLabels) + nodeRadius(probe[j], includeLabels);
        if (Math.hypot(probe[j].x - probe[i].x, probe[j].y - probe[i].y) < minDistance) overlaps += 1;
      }
    }
    return overlaps;
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
    const overlapProbe = countOverlaps(nodes);
    const driftPenalty = Math.min(1, drift / Math.max(1, Math.min(size.width, size.height) * 0.5));
    const overlapTolerance = nodes.length >= 300 ? nodes.length * 24 : nodes.length * 10;
    const score = Math.max(0, 1 - driftPenalty - Math.min(0.35, overlapProbe / Math.max(1, overlapTolerance)));
    root.dataset.cfxGraphLayoutBounds = `${(maxX - minX).toFixed(1)}x${(maxY - minY).toFixed(1)}`;
    root.dataset.cfxGraphLayoutCenterDrift = drift.toFixed(2);
    root.dataset.cfxGraphLayoutOverlapCount = String(overlapProbe);
    root.dataset.cfxGraphLayoutQualityScore = score.toFixed(3);
    root.dataset.cfxGraphLayoutQuality = score >= 0.82 ? 'centered-structured' : 'needs-review';
  };

  const centerLayout = (root, state) => {
    const movable = state.nodes.filter(node => !node.fixed);
    if (movable.length < 2) return;
    const hasFixedAnchors = state.nodes.some(node => node.fixed);
    if (hasFixedAnchors) {
      root.dataset.cfxGraphLayoutRecentering = 'fixed-anchors-skipped';
      return;
    }
    const boundsNodes = movable;
    const size = sceneSize(root);
    const minX = Math.min(...boundsNodes.map(node => node.x));
    const maxX = Math.max(...boundsNodes.map(node => node.x));
    const minY = Math.min(...boundsNodes.map(node => node.y));
    const maxY = Math.max(...boundsNodes.map(node => node.y));
    const dx = size.centerX - (minX + maxX) / 2;
    const dy = size.centerY - (minY + maxY) / 2;
    if (Math.hypot(dx, dy) < 0.5) return;
    movable.forEach(node => {
      node.x += dx;
      node.y += dy;
    });
    root.dataset.cfxGraphLayoutRecentering = `${dx.toFixed(2)},${dy.toFixed(2)}`;
  };

  const overlapPasses = (state, densePasses) => state.nodes.length >= 1200 ? 1 : state.nodes.length >= 500 ? 2 : densePasses;

  const compactStabilizedLayout = (root, state) => {
    const movable = state.nodes.filter(node => !node.fixed);
    if (movable.length < 3) return;
    const size = sceneSize(root);
    const minX = Math.min(...movable.map(node => node.x));
    const maxX = Math.max(...movable.map(node => node.x));
    const minY = Math.min(...movable.map(node => node.y));
    const maxY = Math.max(...movable.map(node => node.y));
    const width = Math.max(1, maxX - minX);
    const height = Math.max(1, maxY - minY);
    const overlaps = countOverlaps(movable);
    if (overlaps > movable.length * 0.45) {
      root.dataset.cfxGraphLayoutCompactionSkipped = String(overlaps);
      return;
    }
    const targetWidth = size.width * 0.86;
    const targetHeight = size.height * 0.8;
    const scale = Math.min(1.08, targetWidth / width, targetHeight / height);
    const centerX = (minX + maxX) / 2;
    const centerY = (minY + maxY) / 2;
    const centerDrift = Math.hypot(centerX - size.centerX, centerY - size.centerY);
    if (scale >= 0.999 && scale <= 1.001 && centerDrift < Math.min(size.width, size.height) * 0.08) return;
    movable.forEach(node => {
      node.x = size.centerX + (node.x - centerX) * scale;
      node.y = size.centerY + (node.y - centerY) * scale;
      node.vx *= scale;
      node.vy *= scale;
    });
    root.dataset.cfxGraphLayoutCompaction = scale.toFixed(3);
  };

  const expandDenseLayout = (root, state) => {
    const movable = state.nodes.filter(node => !node.fixed);
    if (movable.length < 24) return;
    const overlaps = countOverlaps(movable);
    const threshold = movable.length >= 300 ? movable.length * 0.65 : movable.length * 0.35;
    if (overlaps <= threshold) return;
    const size = sceneSize(root);
    const minX = Math.min(...movable.map(node => node.x));
    const maxX = Math.max(...movable.map(node => node.x));
    const minY = Math.min(...movable.map(node => node.y));
    const maxY = Math.max(...movable.map(node => node.y));
    const width = Math.max(1, maxX - minX);
    const height = Math.max(1, maxY - minY);
    const viewportScale = Math.min(size.width * 0.92 / width, size.height * 0.88 / height);
    const spatialHeadroom = movable.length >= 300 ? Math.min(1.62, Math.sqrt(movable.length / 180)) : Math.max(1, viewportScale);
    const factor = Math.min(spatialHeadroom, 1 + Math.min(0.55, overlaps / Math.max(1, movable.length * 8)));
    if (factor <= 1.01) return;
    const centerX = (minX + maxX) / 2;
    const centerY = (minY + maxY) / 2;
    movable.forEach(node => {
      node.x = centerX + (node.x - centerX) * factor;
      node.y = centerY + (node.y - centerY) * factor;
      node.vx *= factor;
      node.vy *= factor;
    });
    root.dataset.cfxGraphLayoutDensityExpansion = factor.toFixed(3);
    root.dataset.cfxGraphLayoutDensityOverlaps = String(overlaps);
  };

  const containLayoutOutliers = (root, state) => {
    const movable = state.nodes.filter(node => !node.fixed);
    if (movable.length < 80) return;
    const xs = movable.map(node => node.x).sort((a, b) => a - b);
    const ys = movable.map(node => node.y).sort((a, b) => a - b);
    const middle = Math.floor(movable.length / 2);
    const centerX = xs[middle];
    const centerY = ys[middle];
    const distances = movable.map(node => ({ node, distance: Math.hypot(node.x - centerX, node.y - centerY) })).sort((a, b) => a.distance - b.distance);
    const percentile = distances[Math.min(distances.length - 1, Math.floor(distances.length * 0.992))].distance;
    const limit = Math.max(Math.min(sceneSize(root).width, sceneSize(root).height) * 0.3, percentile * 1.18);
    let contained = 0;
    distances.forEach(item => {
      if (item.distance <= limit) return;
      const scale = limit / Math.max(1, item.distance);
      item.node.x = centerX + (item.node.x - centerX) * scale;
      item.node.y = centerY + (item.node.y - centerY) * scale;
      item.node.vx *= scale;
      item.node.vy *= scale;
      contained += 1;
    });
    if (contained) root.dataset.cfxGraphLayoutOutliersContained = String(contained);
  };

  const runLayoutQualityPass = (root, state) => {
    spreadHubNeighborhoods(root, state);
    separateOverlaps(root, state, overlapPasses(state, 6));
    balanceLayoutAspect(root, state);
    restoreClusterAnchors(root, state);
    spreadCommunityAreas(root, state);
    compactStabilizedLayout(root, state);
    expandDenseLayout(root, state);
    separateOverlaps(root, state, overlapPasses(state, 14));
    containLayoutOutliers(root, state);
    centerLayout(root, state);
    layoutQualityMetrics(root, state);
  };
