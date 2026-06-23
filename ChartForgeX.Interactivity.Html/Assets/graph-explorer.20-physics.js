  const pairwiseRepulsion = (nodes, settings) => {
    for (let i = 0; i < nodes.length; i++) {
      for (let j = i + 1; j < nodes.length; j++) repelPair(nodes[i], nodes[j], settings, i, j);
    }
  };
  const repelPair = (a, b, settings, i, j) => {
    let dx = b.x - a.x;
    let dy = b.y - a.y;
    let distanceSq = dx * dx + dy * dy;
    if (distanceSq < 1) {
      dx = ((i + 1) * 17 % 7) - 3;
      dy = ((j + 1) * 19 % 7) - 3;
      distanceSq = dx * dx + dy * dy + 1;
    }
    const distance = Math.sqrt(distanceSq);
    const force = settings.repulsion / distanceSq;
    const fx = (dx / distance) * force;
    const fy = (dy / distance) * force;
    if (!a.fixed) { a.vx -= fx; a.vy -= fy; }
    if (!b.fixed) { b.vx += fx; b.vy += fy; }
  };
  const barnesHutTree = (nodes) => {
    const xs = nodes.map(node => node.x);
    const ys = nodes.map(node => node.y);
    const minX = Math.min(...xs, 0);
    const maxX = Math.max(...xs, 960);
    const minY = Math.min(...ys, 0);
    const maxY = Math.max(...ys, 560);
    const size = Math.max(maxX - minX, maxY - minY, 2);
    const root = { x: minX, y: minY, size, mass: 0, cx: 0, cy: 0, node: null, children: null };
    nodes.forEach(node => insertBarnesNode(root, node));
    return root;
  };
  const insertBarnesNode = (quad, node) => {
    const nextMass = quad.mass + 1;
    quad.cx = (quad.cx * quad.mass + node.x) / nextMass;
    quad.cy = (quad.cy * quad.mass + node.y) / nextMass;
    quad.mass = nextMass;
    if (!quad.children && !quad.node) {
      quad.node = node;
      return;
    }
    if (!quad.children) {
      if (quad.size <= 2) return;
      const existing = quad.node;
      quad.node = null;
      quad.children = splitBarnesQuad(quad);
      if (existing) insertBarnesNode(barnesChild(quad, existing), existing);
    }
    insertBarnesNode(barnesChild(quad, node), node);
  };
  const splitBarnesQuad = (quad) => {
    const half = quad.size / 2;
    const quadAt = (x, y) => ({ x, y, size: half, mass: 0, cx: 0, cy: 0, node: null, children: null });
    return [quadAt(quad.x, quad.y), quadAt(quad.x + half, quad.y), quadAt(quad.x, quad.y + half), quadAt(quad.x + half, quad.y + half)];
  };
  const barnesChild = (quad, node) => {
    const east = node.x >= quad.x + quad.size / 2 ? 1 : 0;
    const south = node.y >= quad.y + quad.size / 2 ? 2 : 0;
    return quad.children[east + south];
  };
  const barnesHutRepulsion = (nodes, settings) => {
    const tree = barnesHutTree(nodes);
    nodes.forEach(node => applyBarnesForce(node, tree, settings, 0.72));
  };
  const applyBarnesForce = (node, quad, settings, theta) => {
    if (!quad.mass || (quad.mass === 1 && quad.node === node)) return;
    let dx = quad.cx - node.x;
    let dy = quad.cy - node.y;
    let distanceSq = dx * dx + dy * dy;
    if (distanceSq < 1) {
      dx = 1;
      dy = 1;
      distanceSq = 2;
    }
    const distance = Math.sqrt(distanceSq);
    if (!quad.children || quad.size / distance < theta) {
      const force = settings.repulsion * quad.mass / distanceSq;
      if (!node.fixed) { node.vx -= (dx / distance) * force; node.vy -= (dy / distance) * force; }
      return;
    }
    quad.children.forEach(child => applyBarnesForce(node, child, settings, theta));
  };
  const physicsAcceleration = (state, settings) => settings.solver === 'BarnesHut' || state.nodes.length >= 500 ? 'barnes-hut' : 'pairwise';
  const simulatePhysicsStep = (state, settings) => {
    const width = 960;
    const height = 560;
    const centerX = width / 2;
    const centerY = height / 2;
    let maxVelocity = 0;
    const acceleration = physicsAcceleration(state, settings);
    if (acceleration === 'barnes-hut') barnesHutRepulsion(state.nodes, settings);
    else pairwiseRepulsion(state.nodes, settings);
    state.edges.forEach(edge => {
      const dx = edge.target.x - edge.source.x;
      const dy = edge.target.y - edge.source.y;
      const distance = Math.max(1, Math.sqrt(dx * dx + dy * dy));
      const target = edge.length || settings.linkDistance;
      const force = (distance - target) * 0.015 * edge.weight;
      const fx = (dx / distance) * force;
      const fy = (dy / distance) * force;
      if (!edge.source.fixed) { edge.source.vx += fx; edge.source.vy += fy; }
      if (!edge.target.fixed) { edge.target.vx -= fx; edge.target.vy -= fy; }
    });
    state.nodes.forEach((node, index) => {
      if (settings.solver === 'HierarchicalRepulsion' && !node.fixed) {
        const targetY = 120 + (index % 4) * 110;
        node.vy += (targetY - node.y) * 0.0025;
      }
      if (!node.fixed) {
        node.vx += (centerX - node.x) * settings.centerGravity;
        node.vy += (centerY - node.y) * settings.centerGravity;
        node.vx *= (1 - settings.damping);
        node.vy *= (1 - settings.damping);
        const velocity = Math.sqrt(node.vx * node.vx + node.vy * node.vy);
        if (velocity > settings.maxVelocity) {
          const scale = settings.maxVelocity / velocity;
          node.vx *= scale;
          node.vy *= scale;
        }
        node.x = Math.max(24, Math.min(width - 24, node.x + node.vx * settings.timestep));
        node.y = Math.max(24, Math.min(height - 24, node.y + node.vy * settings.timestep));
      }
      maxVelocity = Math.max(maxVelocity, Math.sqrt(node.vx * node.vx + node.vy * node.vy));
    });
    return { maxVelocity, acceleration };
  };
  const physicsTick = (root, state, settings, tick) => {
    const started = Date.now();
    const result = simulatePhysicsStep(state, settings);
    root.dataset.cfxGraphPhysicsAcceleration = result.acceleration;
    applyLayout(root, state);
    const interval = Math.max(1, num(root, 'data-cfx-performance-telemetry-interval', 30));
    if (tick % interval === 0) publishPerformance(root, { graphId: attr(root, 'data-cfx-graph-id'), mode: 'physics', tick, maxVelocity: result.maxVelocity, acceleration: result.acceleration, frameBudget: num(root, 'data-cfx-performance-frame-budget', 16), thread: 'main', sampleMs: Date.now() - started, sampleTicks: 1 });
    return result.maxVelocity;
  };
  const canUseWorkerPhysics = (root, state, settings) =>
    typeof Worker !== 'undefined' && typeof Blob !== 'undefined' && typeof URL !== 'undefined' &&
    state.nodes.length >= 160 && physicsAcceleration(state, settings) === 'barnes-hut' && root.__cfxGraphWorkerFailed !== true;
  const serializePhysicsState = (state) => {
    const nodeIndex = new Map(state.nodes.map((node, index) => [node.id, index]));
    return {
      nodes: state.nodes.map(node => ({ id: node.id, x: node.x, y: node.y, vx: node.vx, vy: node.vy, fixed: node.fixed })),
      edges: state.edges.map(edge => ({ sourceIndex: nodeIndex.get(edge.source.id), targetIndex: nodeIndex.get(edge.target.id), length: edge.length, weight: edge.weight }))
        .filter(edge => Number.isInteger(edge.sourceIndex) && Number.isInteger(edge.targetIndex))
    };
  };
  const updatePhysicsNodes = (state, nodes) => {
    nodes.forEach((node, index) => {
      if (!state.nodes[index]) return;
      state.nodes[index].x = node.x;
      state.nodes[index].y = node.y;
      state.nodes[index].vx = node.vx;
      state.nodes[index].vy = node.vy;
    });
  };
  const workerPhysicsSource = () => `
const pairwiseRepulsion = ${pairwiseRepulsion.toString()};
const repelPair = ${repelPair.toString()};
const barnesHutTree = ${barnesHutTree.toString()};
const insertBarnesNode = ${insertBarnesNode.toString()};
const splitBarnesQuad = ${splitBarnesQuad.toString()};
const barnesChild = ${barnesChild.toString()};
const barnesHutRepulsion = ${barnesHutRepulsion.toString()};
const applyBarnesForce = ${applyBarnesForce.toString()};
const physicsAcceleration = ${physicsAcceleration.toString()};
const simulatePhysicsStep = ${simulatePhysicsStep.toString()};
self.onmessage = event => {
  const data = event.data || {};
  const state = { nodes: data.nodes || [], edges: (data.edges || []).map(edge => ({ ...edge, source: data.nodes[edge.sourceIndex], target: data.nodes[edge.targetIndex] })).filter(edge => edge.source && edge.target) };
  const interval = Math.max(1, data.interval || 30);
  let batchStarted = Date.now();
  let batchTick = 0;
  for (let tick = 1; tick <= data.settings.iterations; tick++) {
    const result = simulatePhysicsStep(state, data.settings);
    const done = tick >= data.settings.iterations || result.maxVelocity <= data.settings.minVelocity;
    if (done || tick % interval === 0) {
      self.postMessage({ type: done ? 'done' : 'progress', tick, maxVelocity: result.maxVelocity, acceleration: result.acceleration, sampleMs: Date.now() - batchStarted, sampleTicks: tick - batchTick, nodes: state.nodes });
      batchStarted = Date.now();
      batchTick = tick;
    }
    if (done) return;
  }
};`;
  const stopWorkerPhysics = (root, preserveThread) => {
    const active = root.__cfxGraphWorker;
    if (!active) return;
    active.worker.terminate();
    URL.revokeObjectURL(active.url);
    root.__cfxGraphWorker = null;
    if (!preserveThread) root.dataset.cfxGraphPhysicsThread = 'stopped';
  };
  const stopMainPhysics = (root, preserveThread) => {
    const active = root.__cfxGraphMainPhysics;
    if (!active) return;
    if (active.frame) cancelAnimationFrame(active.frame);
    root.__cfxGraphMainPhysics = null;
    if (!preserveThread) root.dataset.cfxGraphPhysicsThread = 'stopped';
  };
  const startWorkerPhysics = (root, state, settings) => {
    try {
      const blob = new Blob([workerPhysicsSource()], { type: 'application/javascript' });
      const url = URL.createObjectURL(blob);
      const worker = new Worker(url);
      const active = { worker, url };
      root.__cfxGraphWorker = active;
      root.dataset.cfxGraphPhysicsThread = 'worker';
      root.dataset.cfxGraphPhysicsAcceleration = 'barnes-hut';
      worker.onmessage = event => {
        if (root.__cfxGraphWorker !== active) return;
        if (root.dataset.cfxGraphPhysicsState !== 'running') {
          stopWorkerPhysics(root, true);
          return;
        }
        const message = event.data || {};
        updatePhysicsNodes(state, message.nodes || []);
        applyLayout(root, state);
        publishPerformance(root, { graphId: attr(root, 'data-cfx-graph-id'), mode: 'physics', tick: message.tick, maxVelocity: message.maxVelocity, acceleration: message.acceleration, frameBudget: num(root, 'data-cfx-performance-frame-budget', 16), thread: 'worker', sampleMs: message.sampleMs, sampleTicks: message.sampleTicks });
        if (message.type === 'done') {
          root.dataset.cfxGraphPhysicsState = 'stabilized';
          stopWorkerPhysics(root, true);
          emit(root, 'cfxgraphstabilized', { graphId: attr(root, 'data-cfx-graph-id'), ticks: message.tick, maxVelocity: message.maxVelocity, thread: 'worker' });
        }
      };
      worker.onerror = () => {
        root.__cfxGraphWorkerFailed = true;
        stopWorkerPhysics(root);
        if (root.dataset.cfxGraphPhysicsState === 'running') startMainPhysics(root, state, settings);
      };
      worker.postMessage({ ...serializePhysicsState(state), settings, interval: Math.max(1, num(root, 'data-cfx-performance-telemetry-interval', 30)) });
      return true;
    } catch {
      root.__cfxGraphWorkerFailed = true;
      stopWorkerPhysics(root);
      return false;
    }
  };
  const startMainPhysics = (root, state, settings) => {
    stopMainPhysics(root, true);
    let tick = 0;
    const active = {};
    root.__cfxGraphMainPhysics = active;
    root.dataset.cfxGraphPhysicsThread = 'main';
    const step = () => {
      if (root.__cfxGraphMainPhysics !== active || root.dataset.cfxGraphPhysicsState !== 'running') return;
      tick += 1;
      const velocity = physicsTick(root, state, settings, tick);
      if (tick >= settings.iterations || velocity <= settings.minVelocity) {
        root.dataset.cfxGraphPhysicsState = 'stabilized';
        root.__cfxGraphMainPhysics = null;
        emit(root, 'cfxgraphstabilized', { graphId: attr(root, 'data-cfx-graph-id'), ticks: tick, maxVelocity: velocity });
        return;
      }
      active.frame = requestAnimationFrame(step);
    };
    active.frame = requestAnimationFrame(step);
  };
  const startPhysics = (root) => {
    const settings = profile(root);
    if (!hasFeature(root, 'RuntimePhysics') || settings.solver === 'None' || settings.solver === 'StaticPrepared' || performanceGate(root)) return;
    stopWorkerPhysics(root);
    stopMainPhysics(root);
    const state = graphState(root);
    root.dataset.cfxGraphPhysicsState = 'running';
    if (canUseWorkerPhysics(root, state, settings) && startWorkerPhysics(root, state, settings)) return;
    startMainPhysics(root, state, settings);
  };
