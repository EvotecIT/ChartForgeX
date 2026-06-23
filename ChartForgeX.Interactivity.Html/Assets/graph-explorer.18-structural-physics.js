  const physicsCommunityKey = (node) => node.cluster || node.groupId || node.kind || 'graph';
  const physicsCommunityAnchors = (state, layout) => {
    const groups = new Map();
    state.nodes.forEach(node => {
      const key = physicsCommunityKey(node);
      const group = groups.get(key) || { key, count: 0, degree: 0, homeX: 0, homeY: 0 };
      group.count += 1;
      group.degree += node.degree || 0;
      group.homeX += Number.isFinite(node.homeX) ? node.homeX : node.x;
      group.homeY += Number.isFinite(node.homeY) ? node.homeY : node.y;
      groups.set(key, group);
    });
    const ordered = Array.from(groups.values()).sort((a, b) => b.count - a.count || b.degree - a.degree || a.key.localeCompare(b.key));
    const anchors = new Map();
    const spread = Math.min(layout.width, layout.height) * (ordered.length <= 2 ? 0.22 : 0.34);
    ordered.forEach((group, index) => {
      const angle = ordered.length === 1 ? -Math.PI / 2 : -Math.PI / 2 + Math.PI * 2 * index / ordered.length;
      anchors.set(group.key, {
        x: ordered.length === 1 ? layout.centerX : layout.centerX + Math.cos(angle) * spread,
        y: ordered.length === 1 ? layout.centerY : layout.centerY + Math.sin(angle) * spread * 0.72,
        homeX: group.homeX / Math.max(1, group.count),
        homeY: group.homeY / Math.max(1, group.count),
        count: group.count
      });
    });
    return anchors;
  };
  const applyStructuralForces = (state, settings, layout) => {
    const anchors = physicsCommunityAnchors(state, layout);
    const homeGravity = settings.homeGravity;
    const communityGravity = settings.communityGravity;
    const hubSpread = settings.hubSpread;
    state.nodes.forEach(node => {
      if (node.fixed) return;
      const homeX = Number.isFinite(node.homeX) ? node.homeX : node.x;
      const homeY = Number.isFinite(node.homeY) ? node.homeY : node.y;
      const anchor = anchors.get(physicsCommunityKey(node));
      node.vx += (homeX - node.x) * homeGravity;
      node.vy += (homeY - node.y) * homeGravity;
      if (anchor) {
        node.vx += (anchor.x - node.x) * communityGravity;
        node.vy += (anchor.y - node.y) * communityGravity;
      }
      const degree = Math.max(0, node.degree || 0);
      if (degree > 2 && hubSpread > 0) {
        const dx = node.x - layout.centerX;
        const dy = node.y - layout.centerY;
        const distance = Math.max(1, Math.sqrt(dx * dx + dy * dy));
        const target = Math.min(Math.min(layout.width, layout.height) * 0.26, 34 + Math.sqrt(degree) * 22);
        if (distance < target) {
          const force = (target - distance) * hubSpread / distance;
          node.vx += dx * force;
          node.vy += dy * force;
        }
      }
    });
  };
  const applyOverlapPressure = (nodes, settings) => {
    if (!settings.overlapPressure || nodes.length < 2) return 0;
    const cellSize = Math.max(36, Math.min(92, settings.linkDistance * 0.62));
    const grid = new Map();
    nodes.forEach(node => {
      const key = `${Math.floor(node.x / cellSize)},${Math.floor(node.y / cellSize)}`;
      const bucket = grid.get(key) || [];
      bucket.push(node);
      grid.set(key, bucket);
    });
    const maxPairs = nodes.length >= 3000 ? 70000 : nodes.length >= 1000 ? 110000 : 160000;
    let pairs = 0;
    let resolved = 0;
    nodes.forEach((node, index) => {
      const gx = Math.floor(node.x / cellSize);
      const gy = Math.floor(node.y / cellSize);
      for (let ox = -1; ox <= 1; ox++) {
        for (let oy = -1; oy <= 1; oy++) {
          const bucket = grid.get(`${gx + ox},${gy + oy}`) || [];
          for (const other of bucket) {
            if (other === node || other.id < node.id || pairs++ > maxPairs) continue;
            const minDistance = Math.max(14, (node.size || 8) + (other.size || 8) + 12);
            let dx = other.x - node.x;
            let dy = other.y - node.y;
            let distance = Math.sqrt(dx * dx + dy * dy);
            if (distance >= minDistance) continue;
            if (distance < 0.001) {
              const seed = ((index + 1) * 31 + (other.id || '').length * 17) % 360;
              dx = Math.cos(seed);
              dy = Math.sin(seed);
              distance = 1;
            }
            const push = (minDistance - distance) / distance * settings.overlapPressure;
            const fx = dx * push;
            const fy = dy * push;
            if (!node.fixed) { node.vx -= fx; node.vy -= fy; }
            if (!other.fixed) { other.vx += fx; other.vy += fy; }
            resolved += 1;
          }
        }
      }
    });
    return resolved;
  };
