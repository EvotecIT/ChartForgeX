  const physicsTick = (root, state, settings, tick) => {
    const started = performanceClock();
    const result = simulatePhysicsStep(state, settings);
    const simulatedAt = performanceClock();
    root.dataset.cfxGraphPhysicsAcceleration = result.acceleration;
    const renderStarted = performanceClock();
    applyLayout(root, state);
    const renderedAt = performanceClock();
    const interval = Math.max(1, num(root, 'data-cfx-performance-telemetry-interval', 30));
    if (tick % interval === 0) publishPerformance(root, { graphId: attr(root, 'data-cfx-graph-id'), mode: 'physics', tick, maxVelocity: result.maxVelocity, acceleration: result.acceleration, overlaps: result.overlaps, communityPushes: result.communityPushes, frameBudget: num(root, 'data-cfx-performance-frame-budget', 16), thread: 'main', sampleMs: simulatedAt - started, sampleTicks: 1 });
    return { maxVelocity: result.maxVelocity, renderMs: renderedAt - renderStarted };
  };
