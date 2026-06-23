  const restoreClusterAnchors = (root, state) => {
    const groups = new Map();
    state.nodes.forEach(node => {
      if (!node.cluster) return;
      const group = groups.get(node.cluster) || { nodes: [], homeX: 0, homeY: 0, x: 0, y: 0 };
      group.nodes.push(node);
      group.homeX += node.homeX;
      group.homeY += node.homeY;
      group.x += node.x;
      group.y += node.y;
      groups.set(node.cluster, group);
    });
    const strength = state.nodes.length >= 80 ? 0.72 : 0.42;
    let adjusted = 0;
    groups.forEach(group => {
      if (group.nodes.length < 2) return;
      const count = group.nodes.length;
      const dx = group.homeX / count - group.x / count;
      const dy = group.homeY / count - group.y / count;
      group.nodes.forEach(node => {
        if (node.fixed) return;
        node.x += dx * strength;
        node.y += dy * strength;
      });
      adjusted += 1;
    });
    if (adjusted) root.dataset.cfxGraphLayoutClusterGravity = strength.toFixed(2);
  };

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
    const targetWidth = size.width * 0.84;
    const targetHeight = size.height * 0.78;
    const scale = Math.min(1, targetWidth / width, targetHeight / height);
    const centerX = (minX + maxX) / 2;
    const centerY = (minY + maxY) / 2;
    const centerDrift = Math.hypot(centerX - size.centerX, centerY - size.centerY);
    if (scale >= 0.999 && centerDrift < Math.min(size.width, size.height) * 0.08) return;
    movable.forEach(node => {
      node.x = size.centerX + (node.x - centerX) * scale;
      node.y = size.centerY + (node.y - centerY) * scale;
      node.vx *= scale;
      node.vy *= scale;
    });
    root.dataset.cfxGraphLayoutCompaction = scale.toFixed(3);
  };
