  const physicsCommunityKey = (node) => node.cluster ? `cluster:${node.cluster}` : node.groupId ? `group:${node.groupId}` : node.kind ? `kind:${node.kind}` : 'graph';
  const physicsNodeRadius = (node, includeLabels) => {
    const mark = Math.max(4, Number(node.size) || 8) * (node.shape === 'box' || node.shape === 'database' ? 1.3 : node.shape === 'image' ? 1.18 : 1);
    const label = includeLabels ? Math.min(90, Math.max(0, String(node.label || '').length * 3.2)) : 0;
    return Math.max(mark, label);
  };
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
      const homeX = group.homeX / Math.max(1, group.count);
      const homeY = group.homeY / Math.max(1, group.count);
      const ringX = ordered.length === 1 ? layout.centerX : layout.centerX + Math.cos(angle) * spread;
      const ringY = ordered.length === 1 ? layout.centerY : layout.centerY + Math.sin(angle) * spread * 0.72;
      anchors.set(group.key, {
        x: homeX * 0.62 + ringX * 0.38,
        y: homeY * 0.62 + ringY * 0.38,
        homeX,
        homeY,
        count: group.count
      });
    });
    return anchors;
  };
  const applyCommunityPacking = (state, settings) => {
    if (!settings.communitySeparation || state.nodes.length < 8) return 0;
    const groups = new Map();
    state.nodes.forEach(node => {
      const key = physicsCommunityKey(node);
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
      group.radius = Math.max(44, Math.min(210, Math.sqrt(group.nodes.length) * 20 + group.nodes.reduce((sum, node) => sum + (node.size || 8), 0) / group.nodes.length));
    });
    let pushes = 0;
    for (let i = 0; i < ordered.length; i++) {
      for (let j = i + 1; j < ordered.length; j++) {
        const a = ordered[i];
        const b = ordered[j];
        let dx = b.x - a.x;
        let dy = b.y - a.y;
        let distance = Math.hypot(dx, dy);
        const minDistance = Math.min(360, (a.radius + b.radius) * 0.94);
        if (distance >= minDistance) continue;
        if (distance < 0.001) {
          const seed = (a.key.length * 17 + b.key.length * 31) % 360;
          dx = Math.cos(seed);
          dy = Math.sin(seed);
          distance = 1;
        }
        const force = (minDistance - distance) / distance * settings.communitySeparation;
        const fx = dx * force;
        const fy = dy * force;
        a.nodes.forEach(node => { if (!node.fixed) { node.vx -= fx / Math.sqrt(a.nodes.length); node.vy -= fy / Math.sqrt(a.nodes.length); } });
        b.nodes.forEach(node => { if (!node.fixed) { node.vx += fx / Math.sqrt(b.nodes.length); node.vy += fy / Math.sqrt(b.nodes.length); } });
        pushes += 1;
      }
    }
    return pushes;
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
    return applyCommunityPacking(state, settings);
  };
  const applyOverlapPressure = (nodes, settings) => {
    if ((!settings.overlapPressure && !settings.avoidOverlap) || nodes.length < 2) return 0;
    const includeLabels = nodes.length < 500;
    const cellSize = Math.max(36, Math.min(220, nodes.reduce((maximum, node) => Math.max(maximum, physicsNodeRadius(node, includeLabels) * 2 + 12), settings.linkDistance * 0.62)));
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
    let exhausted = false;
    nodes.forEach((node, index) => {
      if (exhausted) return;
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
            const minDistance = (physicsNodeRadius(node, includeLabels) + physicsNodeRadius(other, includeLabels) + 8) * (0.45 + settings.avoidOverlap * 0.55);
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
            const push = (minDistance - distance) / distance * Math.max(settings.overlapPressure, settings.avoidOverlap * 0.08);
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
