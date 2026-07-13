  const canUseWorkerPhysics = (root, state) => typeof Worker !== 'undefined' && typeof Blob !== 'undefined' && typeof URL !== 'undefined' && state.nodes.length >= 160 && root.__cfxGraphWorkerFailed !== true;
  const serializePhysicsState = (state) => {
    const nodeIndex = new Map(state.nodes.map((node, index) => [node.id, index]));
    return {
      nodes: state.nodes.map(node => ({ id: node.id, label: node.label, shape: node.shape, x: node.x, y: node.y, homeX: node.homeX, homeY: node.homeY, vx: node.vx, vy: node.vy, fixed: node.fixed, degree: node.degree, size: node.size, level: node.level, cluster: node.cluster, groupId: node.groupId, kind: node.kind })),
      edges: state.edges.filter(edge => edge.physics !== false).map(edge => ({ sourceIndex: nodeIndex.get(edge.source.id), targetIndex: nodeIndex.get(edge.target.id), length: edge.length, weight: edge.weight })).filter(edge => Number.isInteger(edge.sourceIndex) && Number.isInteger(edge.targetIndex))
    };
  };
  const updatePhysicsNodes = (root, state, nodes) => {
    const byId = new Map(state.nodes.map(node => [node.id, node]));
    nodes.forEach(source => {
      const node = byId.get(source.id);
      if (!node || root.__cfxGraphDragNodeId === node.id) return;
      node.x = source.x; node.y = source.y; node.vx = source.vx; node.vy = source.vy; node.fixed = source.fixed;
    });
  };
  const workerPhysicsSource = () => `
const physicsNodeRadius = ${physicsNodeRadius.toString()};
const physicsMass = ${physicsMass.toString()};
const physicsDistance = ${physicsDistance.toString()};
const applyPhysicsForce = ${applyPhysicsForce.toString()};
const gravitationalRepulsion = ${gravitationalRepulsion.toString()};
const limitedRepulsion = ${limitedRepulsion.toString()};
const pairwiseRepulsion = ${pairwiseRepulsion.toString()};
const gridRepulsion = ${gridRepulsion.toString()};
const barnesHutTree = ${barnesHutTree.toString()};
const insertBarnesNode = ${insertBarnesNode.toString()};
const splitBarnesQuad = ${splitBarnesQuad.toString()};
const barnesChild = ${barnesChild.toString()};
const barnesHutRepulsion = ${barnesHutRepulsion.toString()};
const applyBarnesForce = ${applyBarnesForce.toString()};
const physicsAcceleration = ${physicsAcceleration.toString()};
const physicsLayout = ${physicsLayout.toString()};
const applyHierarchyAxis = ${applyHierarchyAxis.toString()};
const physicsCommunityKey = ${physicsCommunityKey.toString()};
const physicsCommunityAnchors = ${physicsCommunityAnchors.toString()};
const applyCommunityPacking = ${applyCommunityPacking.toString()};
const applyStructuralForces = ${applyStructuralForces.toString()};
const applyOverlapPressure = ${applyOverlapPressure.toString()};
const updateAdaptiveTimestep = ${updateAdaptiveTimestep.toString()};
const simulatePhysicsStep = ${simulatePhysicsStep.toString()};
let runtime = null;
let timer = 0;
const schedule = () => { if (!timer && runtime?.running) timer = setTimeout(runBatch, 0); };
const runBatch = () => {
  timer = 0;
  if (!runtime?.running) return;
  const started = Date.now();
  let result = { maxVelocity: 0, acceleration: 'pairwise', overlaps: 0, communityPushes: 0 };
  let sampleTicks = 0;
  for (let index = 0; index < runtime.interval; index++) {
    result = simulatePhysicsStep(runtime.state, runtime.settings);
    runtime.tick += 1; sampleTicks += 1;
    if (!runtime.dragging && (runtime.tick >= runtime.settings.iterations || result.maxVelocity <= runtime.settings.minVelocity)) break;
  }
  const done = !runtime.dragging && (runtime.tick >= runtime.settings.iterations || result.maxVelocity <= runtime.settings.minVelocity);
  self.postMessage({ type: done ? 'done' : 'progress', tick: runtime.tick, maxVelocity: result.maxVelocity, acceleration: result.acceleration, overlaps: result.overlaps, communityPushes: result.communityPushes, sampleMs: Date.now() - started, sampleTicks, nodes: runtime.state.nodes });
  runtime.running = !done;
  if (runtime.running) schedule();
};
self.onmessage = event => {
  const data = event.data || {};
  if (data.type === 'start') {
    const nodes = data.nodes || [];
    runtime = { state: { nodes, edges: (data.edges || []).map(edge => ({ ...edge, source: nodes[edge.sourceIndex], target: nodes[edge.targetIndex] })).filter(edge => edge.source && edge.target) }, settings: data.settings, interval: Math.max(1, data.interval || 12), tick: 0, dragging: '', running: true };
    schedule(); return;
  }
  if (!runtime) return;
  if (data.type === 'pin') {
    const node = runtime.state.nodes.find(item => item.id === data.nodeId);
    if (node) { node.x = data.x; node.y = data.y; node.vx = 0; node.vy = 0; node.fixed = true; }
    runtime.dragging = data.nodeId || ''; runtime.tick = 0; runtime.running = true; schedule(); return;
  }
  if (data.type === 'release') {
    const node = runtime.state.nodes.find(item => item.id === data.nodeId);
    if (node) { node.x = data.x; node.y = data.y; node.vx = data.vx || 0; node.vy = data.vy || 0; node.fixed = data.fixed === true; }
    runtime.dragging = ''; runtime.tick = 0; runtime.running = true; schedule(); return;
  }
  if (data.type === 'reheat') { runtime.tick = 0; runtime.running = true; schedule(); return; }
  if (data.type === 'pause') { runtime.running = false; if (timer) clearTimeout(timer); timer = 0; }
};`;
  const stopWorkerPhysics = (root, preserveThread) => {
    const active = root.__cfxGraphWorker;
    if (!active) return;
    active.worker.terminate(); URL.revokeObjectURL(active.url); root.__cfxGraphWorker = null;
    if (!preserveThread) root.dataset.cfxGraphPhysicsThread = 'stopped';
  };
  const stopMainPhysics = (root, preserveThread) => {
    const active = root.__cfxGraphMainPhysics;
    if (!active) return;
    if (active.frame) cancelAnimationFrame(active.frame);
    root.__cfxGraphMainPhysics = null;
    if (!preserveThread) root.dataset.cfxGraphPhysicsThread = 'stopped';
  };
  const pausePhysics = (root) => {
    root.dataset.cfxGraphPhysicsState = 'paused';
    root.__cfxGraphWorker?.worker.postMessage({ type: 'pause' });
    stopWorkerPhysics(root, true); stopMainPhysics(root, true);
    if (typeof syncPhysicsControls === 'function') syncPhysicsControls(root);
  };
  const completePhysics = (root, state, ticks, velocity, thread) => {
    root.dataset.cfxGraphPhysicsState = 'stabilized';
    runLayoutQualityPass(root, state); applyLayout(root, state); syncSvgLayout(root, state); drawCanvas(root, state);
    if (root.__cfxGraphAutoFitOnStabilize && root.__cfxGraphViewportTouched !== true) fitViewport(root);
    root.__cfxGraphAutoFitOnStabilize = false;
    if (typeof syncPhysicsControls === 'function') syncPhysicsControls(root);
    if (typeof syncPhysicsConfigurator === 'function') syncPhysicsConfigurator(root);
    emit(root, 'cfxgraphstabilized', { graphId: attr(root, 'data-cfx-graph-id'), ticks, maxVelocity: velocity, thread });
  };
  const startWorkerPhysics = (root, state, settings) => {
    try {
      const blob = new Blob([workerPhysicsSource()], { type: 'application/javascript' });
      const url = URL.createObjectURL(blob), worker = new Worker(url), active = { worker, url, state, settings };
      root.__cfxGraphWorker = active; root.dataset.cfxGraphPhysicsThread = 'worker'; root.dataset.cfxGraphPhysicsAcceleration = physicsAcceleration(state, settings);
      worker.onmessage = event => {
        if (root.__cfxGraphWorker !== active || root.dataset.cfxGraphPhysicsState !== 'running') return;
        const message = event.data || {};
        const present = message.type === 'done' || !graphPrefersReducedMotion(root);
        if (present) updatePhysicsNodes(root, state, message.nodes || []);
        const renderStarted = performanceClock(); if (present) applyLayout(root, state); const renderedAt = performanceClock();
        recordFramePerformance(root, renderedAt, renderedAt - renderStarted, 'worker');
        publishPerformance(root, { graphId: attr(root, 'data-cfx-graph-id'), mode: 'physics', tick: message.tick, maxVelocity: message.maxVelocity, acceleration: message.acceleration, overlaps: message.overlaps, communityPushes: message.communityPushes, frameBudget: num(root, 'data-cfx-performance-frame-budget', 16), thread: 'worker', sampleMs: message.sampleMs, sampleTicks: message.sampleTicks });
        if (message.type === 'done') { stopWorkerPhysics(root, true); completePhysics(root, state, message.tick, message.maxVelocity, 'worker'); }
      };
      worker.onerror = () => { root.__cfxGraphWorkerFailed = true; stopWorkerPhysics(root); if (root.dataset.cfxGraphPhysicsState === 'running') startMainPhysics(root, state, settings); };
      worker.postMessage({ type: 'start', ...serializePhysicsState(state), settings, interval: Math.max(settings.progressInterval, num(root, 'data-cfx-performance-worker-progress-interval', 4)) });
      return true;
    } catch {
      root.__cfxGraphWorkerFailed = true; stopWorkerPhysics(root); return false;
    }
  };
  const startMainPhysics = (root, state, settings) => {
    stopMainPhysics(root, true);
    const active = { tick: 0, frame: 0, state, settings };
    root.__cfxGraphMainPhysics = active; root.dataset.cfxGraphPhysicsThread = 'main'; root.dataset.cfxGraphPhysicsAcceleration = physicsAcceleration(state, settings);
    const step = (timestamp) => {
      if (root.__cfxGraphMainPhysics !== active || root.dataset.cfxGraphPhysicsState !== 'running') return;
      active.tick += 1;
      const frame = physicsTick(root, state, settings, active.tick);
      const renderedAt = performanceClock(); recordFramePerformance(root, Number.isFinite(timestamp) ? timestamp : renderedAt, frame.renderMs, 'main');
      const canFinish = !root.__cfxGraphDragNodeId && (active.tick >= settings.iterations || frame.maxVelocity <= settings.minVelocity);
      if (canFinish) { root.__cfxGraphMainPhysics = null; completePhysics(root, state, active.tick, frame.maxVelocity, 'main'); return; }
      active.frame = requestAnimationFrame(step);
    };
    active.frame = requestAnimationFrame(step);
  };
  const activePhysicsState = (root) => {
    const fullState = root.__cfxGraphState || graphState(root);
    const nodes = fullState.nodes.filter(node => visible(node.el)), byId = new Map(nodes.map(node => [node.id, node]));
    return { ...fullState, nodes, byId, edges: fullState.edges.filter(edge => visible(edge.el) && byId.has(edge.source.id) && byId.has(edge.target.id)), clusters: fullState.clusters.filter(cluster => visible(cluster.el)), fullState };
  };
  const startPhysics = (root, options) => {
    stopWorkerPhysics(root); stopMainPhysics(root);
    const state = activePhysicsState(root), base = profile(root);
    const settings = {
      ...base,
      layout: adaptivePhysicsLayout(root, state),
      layoutDirection: attr(root, 'data-cfx-graph-layout-direction') || 'TopToBottom',
      homeGravity: Math.max(0.0003, Math.min(0.004, base.centerGravity * 0.003 + 0.0006)),
      communityGravity: Math.max(0.0002, Math.min(0.0024, base.centerGravity * 0.0015 + 0.00035)),
      communitySeparation: state.nodes.length >= 3000 ? 0.0022 : state.nodes.length >= 1000 ? 0.0032 : 0.005,
      hubSpread: state.nodes.length >= 1000 ? 0.0008 : 0.0014,
      overlapPressure: Math.max(base.avoidOverlap * 0.08, state.nodes.length >= 3000 ? 0.012 : state.nodes.length >= 1000 ? 0.02 : 0.035)
    };
    if (!hasFeature(root, 'RuntimePhysics') || settings.solver === 'None' || settings.solver === 'StaticPrepared' || performanceGate(root)) return false;
    root.__cfxGraphAutoFitOnStabilize = settings.stabilizationFit && options?.fit !== false && hasFeature(root, 'Viewport') && root.__cfxGraphViewportTouched !== true;
    root.dataset.cfxGraphPhysicsState = 'running'; root.dataset.cfxGraphPhysicsReason = options?.reason || 'manual';
    if (canUseWorkerPhysics(root, state) && startWorkerPhysics(root, state, settings)) return true;
    startMainPhysics(root, state, settings); return true;
  };
  const reheatPhysics = (root, reason, options) => {
    if (!hasFeature(root, 'RuntimePhysics')) return false;
    root.dataset.cfxGraphPhysicsReason = reason || 'reheat';
    if (options?.rebuild) return startPhysics(root, { reason, fit: options.fit });
    if (root.__cfxGraphWorker) {
      root.dataset.cfxGraphPhysicsState = 'running'; root.__cfxGraphWorker.worker.postMessage({ type: 'reheat' });
      if (typeof syncPhysicsControls === 'function') syncPhysicsControls(root); return true;
    }
    if (root.__cfxGraphMainPhysics) {
      root.dataset.cfxGraphPhysicsState = 'running'; root.__cfxGraphMainPhysics.tick = 0;
      if (typeof syncPhysicsControls === 'function') syncPhysicsControls(root); return true;
    }
    return startPhysics(root, { reason, fit: options?.fit });
  };
  const updateDraggedPhysicsNode = (root, node) => {
    root.__cfxGraphWorker?.worker.postMessage({ type: 'pin', nodeId: node.id, x: node.x, y: node.y });
  };
  const releaseDraggedPhysicsNode = (root, node) => {
    root.__cfxGraphWorker?.worker.postMessage({ type: 'release', nodeId: node.id, x: node.x, y: node.y, vx: node.vx, vy: node.vy, fixed: node.fixed });
  };
