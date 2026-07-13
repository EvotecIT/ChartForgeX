  const physicsMass = (node, settings) => settings.solver === 'ForceAtlas2' ? Math.max(1, (node.degree || 0) + 1) : 1;
  const physicsDistance = (a, b, rawDistance, settings) => {
    if (!settings.avoidOverlap) return Math.max(1, rawDistance);
    const inset = (physicsNodeRadius(a, false) + physicsNodeRadius(b, false)) * settings.avoidOverlap;
    return Math.max(1, rawDistance - inset);
  };
  const applyPhysicsForce = (a, b, dx, dy, magnitude, settings) => {
    const distance = Math.max(1, Math.hypot(dx, dy));
    const fx = dx / distance * magnitude;
    const fy = dy / distance * magnitude;
    if (!a.fixed) { const mass = physicsMass(a, settings); a.vx -= fx / mass; a.vy -= fy / mass; }
    if (!b.fixed) { const mass = physicsMass(b, settings); b.vx += fx / mass; b.vy += fy / mass; }
  };
  const gravitationalRepulsion = (a, b, settings, i, j) => {
    let dx = b.x - a.x;
    let dy = b.y - a.y;
    let rawDistance = Math.hypot(dx, dy);
    if (rawDistance < 0.001) {
      const angle = ((((i || 0) + 1) * 37 + ((j || 0) + 1) * 17) % 360) * Math.PI / 180;
      dx = Math.cos(angle); dy = Math.sin(angle); rawDistance = 1;
    }
    const distance = physicsDistance(a, b, rawDistance, settings);
    const masses = physicsMass(a, settings) * physicsMass(b, settings);
    const denominator = settings.solver === 'ForceAtlas2' ? distance : distance * distance;
    const magnitude = Math.abs(settings.gravitationalConstant) * masses / Math.max(1, denominator);
    applyPhysicsForce(a, b, dx, dy, magnitude, settings);
  };
  const limitedRepulsion = (a, b, settings, i, j) => {
    let dx = b.x - a.x;
    let dy = b.y - a.y;
    let distance = Math.hypot(dx, dy);
    if (distance < 0.001) {
      const angle = ((((i || 0) + 1) * 31 + ((j || 0) + 1) * 19) % 360) * Math.PI / 180;
      dx = Math.cos(angle); dy = Math.sin(angle); distance = 1;
    }
    if (settings.solver === 'HierarchicalRepulsion' && a.level !== null && b.level !== null && a.level !== b.level) return;
    const reach = settings.nodeDistance * 2;
    if (distance >= reach) return;
    const normalized = distance <= settings.nodeDistance * 0.5 ? 1 : Math.max(0, (reach - distance) / (settings.nodeDistance * 1.5));
    const magnitude = normalized * settings.nodeDistance * 0.08 * settings.repulsionStrength;
    if (settings.solver === 'HierarchicalRepulsion') {
      const vertical = settings.layoutDirection === 'TopToBottom' || settings.layoutDirection === 'BottomToTop';
      if (vertical) dy *= 0.08; else dx *= 0.08;
    }
    applyPhysicsForce(a, b, dx, dy, magnitude, settings);
  };
  const pairwiseRepulsion = (nodes, settings) => {
    for (let i = 0; i < nodes.length; i++) {
      for (let j = i + 1; j < nodes.length; j++) {
        if (settings.solver === 'Repulsion' || settings.solver === 'HierarchicalRepulsion') limitedRepulsion(nodes[i], nodes[j], settings, i, j);
        else gravitationalRepulsion(nodes[i], nodes[j], settings, i, j);
      }
    }
  };
  const gridRepulsion = (nodes, settings) => {
    const cellSize = Math.max(20, settings.nodeDistance * 2);
    const grid = new Map();
    nodes.forEach((node, index) => {
      const key = `${Math.floor(node.x / cellSize)},${Math.floor(node.y / cellSize)}`;
      const bucket = grid.get(key) || [];
      bucket.push({ node, index }); grid.set(key, bucket);
    });
    nodes.forEach((node, index) => {
      const gx = Math.floor(node.x / cellSize), gy = Math.floor(node.y / cellSize);
      for (let ox = -1; ox <= 1; ox++) for (let oy = -1; oy <= 1; oy++) {
        (grid.get(`${gx + ox},${gy + oy}`) || []).forEach(other => { if (other.index > index) limitedRepulsion(node, other.node, settings, index, other.index); });
      }
    });
  };
  const barnesHutTree = (nodes, settings) => {
    const xs = nodes.map(node => node.x), ys = nodes.map(node => node.y);
    const minX = Math.min(...xs, 0), maxX = Math.max(...xs, 960), minY = Math.min(...ys, 0), maxY = Math.max(...ys, 560);
    const size = Math.max(maxX - minX, maxY - minY, 2);
    const root = { x: minX, y: minY, size, mass: 0, cx: 0, cy: 0, node: null, children: null };
    nodes.forEach(node => insertBarnesNode(root, node, settings));
    return root;
  };
  const insertBarnesNode = (quad, node, settings) => {
    const nodeMass = physicsMass(node, settings), nextMass = quad.mass + nodeMass;
    quad.cx = (quad.cx * quad.mass + node.x * nodeMass) / nextMass;
    quad.cy = (quad.cy * quad.mass + node.y * nodeMass) / nextMass;
    quad.mass = nextMass;
    if (!quad.children && !quad.node) { quad.node = node; return; }
    if (!quad.children) {
      if (quad.size <= 2) return;
      const existing = quad.node; quad.node = null; quad.children = splitBarnesQuad(quad);
      if (existing) insertBarnesNode(barnesChild(quad, existing), existing, settings);
    }
    insertBarnesNode(barnesChild(quad, node), node, settings);
  };
  const splitBarnesQuad = (quad) => {
    const half = quad.size / 2;
    const at = (x, y) => ({ x, y, size: half, mass: 0, cx: 0, cy: 0, node: null, children: null });
    return [at(quad.x, quad.y), at(quad.x + half, quad.y), at(quad.x, quad.y + half), at(quad.x + half, quad.y + half)];
  };
  const barnesChild = (quad, node) => quad.children[(node.x >= quad.x + quad.size / 2 ? 1 : 0) + (node.y >= quad.y + quad.size / 2 ? 2 : 0)];
  const barnesHutRepulsion = (nodes, settings) => {
    const tree = barnesHutTree(nodes, settings);
    nodes.forEach(node => applyBarnesForce(node, tree, settings));
  };
  const applyBarnesForce = (node, quad, settings) => {
    if (!quad || quad.mass <= 0 || (!quad.children && quad.node === node)) return;
    const dx = quad.cx - node.x, dy = quad.cy - node.y, distance = Math.max(1, Math.hypot(dx, dy));
    if (!quad.children || quad.size / distance < settings.theta) {
      if (!quad.children && quad.node) { gravitationalRepulsion(node, quad.node, settings, 0, 1); return; }
      const denominator = settings.solver === 'ForceAtlas2' ? distance : distance * distance;
      const magnitude = Math.abs(settings.gravitationalConstant) * physicsMass(node, settings) * quad.mass / Math.max(1, denominator);
      const proxy = { fixed: true, x: quad.cx, y: quad.cy };
      applyPhysicsForce(node, proxy, dx, dy, magnitude, settings);
      return;
    }
    quad.children.forEach(child => applyBarnesForce(node, child, settings));
  };
  const physicsAcceleration = (state, settings) => {
    if (settings.solver === 'BarnesHut' || settings.solver === 'ForceAtlas2') return 'barnes-hut';
    if (state.nodes.length >= 500) return 'grid';
    return 'pairwise';
  };
  const physicsLayout = (state, settings) => settings.layout || { width: 960, height: 560, centerX: 480, centerY: 280, minX: 0, minY: 0, maxX: 960, maxY: 560 };
  const adaptivePhysicsLayout = (root, state) => {
    const size = sceneSize(root);
    const nodeFactor = Math.max(1, Math.sqrt(Math.max(1, state.nodes.length) / 90));
    const edgeFactor = Math.max(1, Math.sqrt(Math.max(1, state.edges.length) / Math.max(1, state.nodes.length * 1.5)));
    const span = Math.min(3.8, Math.max(1, nodeFactor * edgeFactor));
    const width = size.width * span, height = size.height * Math.min(3.2, Math.max(1, nodeFactor * 0.9));
    return { width, height, centerX: size.centerX, centerY: size.centerY, minX: size.centerX - width / 2, minY: size.centerY - height / 2, maxX: size.centerX + width / 2, maxY: size.centerY + height / 2 };
  };
  const balanceLayoutAspect = (root, state) => {
    const movable = state.nodes.filter(node => !node.fixed);
    if (movable.length < 3) return;
    const minX = Math.min(...movable.map(node => node.x)), maxX = Math.max(...movable.map(node => node.x));
    const minY = Math.min(...movable.map(node => node.y)), maxY = Math.max(...movable.map(node => node.y));
    const width = Math.max(1, maxX - minX), height = Math.max(1, maxY - minY), current = width / height;
    const target = sceneSize(root).width / sceneSize(root).height;
    if (current > target * 0.78 && current < target * 1.28) return;
    const factor = Math.min(1.55, Math.sqrt(current < target ? target / current : current / target));
    const scaleX = current < target ? factor : 1 / factor, scaleY = current < target ? 1 / factor : factor;
    const centerX = (minX + maxX) / 2, centerY = (minY + maxY) / 2;
    movable.forEach(node => { node.x = centerX + (node.x - centerX) * scaleX; node.y = centerY + (node.y - centerY) * scaleY; });
    root.dataset.cfxGraphLayoutAspect = `${current.toFixed(3)}>${target.toFixed(3)}`;
  };
  const applyHierarchyAxis = (node, settings) => {
    if (settings.solver !== 'HierarchicalRepulsion' || node.fixed || node.level === null) return;
    const vertical = settings.layoutDirection === 'TopToBottom' || settings.layoutDirection === 'BottomToTop';
    const target = vertical ? node.homeY : node.homeX;
    if (vertical) node.vy += (target - node.y) * 0.018;
    else node.vx += (target - node.x) * 0.018;
  };
  const simulatePhysicsStep = (state, settings) => {
    const layout = physicsLayout(state, settings);
    const acceleration = physicsAcceleration(state, settings);
    if (acceleration === 'barnes-hut') barnesHutRepulsion(state.nodes, settings);
    else if (acceleration === 'grid') gridRepulsion(state.nodes, settings);
    else pairwiseRepulsion(state.nodes, settings);
    const communityPushes = applyStructuralForces(state, settings, layout);
    const overlaps = applyOverlapPressure(state.nodes, settings);
    state.edges.filter(edge => edge.physics !== false).forEach(edge => {
      const dx = edge.target.x - edge.source.x, dy = edge.target.y - edge.source.y;
      const distance = Math.max(1, Math.hypot(dx, dy)), target = edge.length || settings.springLength;
      const force = (distance - target) * settings.springConstant * edge.weight;
      const fx = dx / distance * force, fy = dy / distance * force;
      if (!edge.source.fixed) { const mass = physicsMass(edge.source, settings); edge.source.vx += fx / mass; edge.source.vy += fy / mass; }
      if (!edge.target.fixed) { const mass = physicsMass(edge.target, settings); edge.target.vx -= fx / mass; edge.target.vy -= fy / mass; }
    });
    let maxVelocity = 0;
    const timestep = Number.isFinite(settings.currentTimestep) ? settings.currentTimestep : settings.timestep;
    state.nodes.forEach(node => {
      applyHierarchyAxis(node, settings);
      if (!node.fixed) {
        const dx = layout.centerX - node.x, dy = layout.centerY - node.y, distance = Math.max(1, Math.hypot(dx, dy));
        const gravity = settings.solver === 'ForceAtlas2' ? settings.centerGravity / distance : settings.centerGravity * 0.01;
        node.vx += dx * gravity; node.vy += dy * gravity;
        if (node.x < layout.minX) node.vx += (layout.minX - node.x) * 0.012;
        if (node.x > layout.maxX) node.vx -= (node.x - layout.maxX) * 0.012;
        if (node.y < layout.minY) node.vy += (layout.minY - node.y) * 0.012;
        if (node.y > layout.maxY) node.vy -= (node.y - layout.maxY) * 0.012;
        node.vx *= 1 - settings.damping; node.vy *= 1 - settings.damping;
        const velocity = Math.hypot(node.vx, node.vy);
        if (velocity > settings.maxVelocity) { const scale = settings.maxVelocity / velocity; node.vx *= scale; node.vy *= scale; }
        node.x += node.vx * timestep; node.y += node.vy * timestep;
        maxVelocity = Math.max(maxVelocity, Math.hypot(node.vx, node.vy));
      }
    });
    updateAdaptiveTimestep(settings, maxVelocity);
    return { maxVelocity, acceleration, overlaps, communityPushes };
  };
