  // The physics worker. Pages with asset files load it from its own file; others start it from this source in a blob.
  const installGraphPhysicsWorker = (scope) => {
    let runtime = null;
    let timer = 0;
    const schedule = () => { if (!timer && runtime?.running && !runtime.waiting) timer = setTimeout(runBatch, 0); };
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
      const positions = runtime.positions || new Float64Array(runtime.state.nodes.length * 5);
      runtime.state.nodes.forEach((node, index) => {
        const offset = index * 5;
        positions[offset] = node.x; positions[offset + 1] = node.y;
        positions[offset + 2] = node.vx; positions[offset + 3] = node.vy; positions[offset + 4] = node.fixed ? 1 : 0;
      });
      runtime.waiting = true; runtime.positions = null; runtime.running = !done;
      scope.postMessage({ type: done ? 'done' : 'progress', generation: runtime.generation, tick: runtime.tick, maxVelocity: result.maxVelocity, acceleration: result.acceleration, overlaps: result.overlaps, communityPushes: result.communityPushes, sampleMs: Date.now() - started, sampleTicks, positions }, [positions.buffer]);
    };
    scope.onmessage = event => {
      const data = event.data || {};
      if (data.type === 'start') {
        const nodes = data.nodes || [];
        runtime = { state: { nodes, edges: (data.edges || []).map(edge => ({ ...edge, source: nodes[edge.sourceIndex], target: nodes[edge.targetIndex] })).filter(edge => edge.source && edge.target) }, settings: data.settings, interval: Math.max(1, data.interval || 12), tick: 0, dragging: '', running: true, waiting: false, generation: 0, positions: null };
        schedule(); return;
      }
      if (!runtime) return;
      if (data.type === 'continue') {
        runtime.positions = data.positions; runtime.waiting = false; schedule(); return;
      }
      if (Number.isInteger(data.generation)) runtime.generation = data.generation;
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
    };
  };
