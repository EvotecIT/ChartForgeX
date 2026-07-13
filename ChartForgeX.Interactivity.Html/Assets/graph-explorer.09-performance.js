  const publishPerformance = (root, detail) => {
    if (!hasFeature(root, 'PerformanceTelemetry')) return;
    const summary = root.__cfxGraphPerformanceSummary || {
      samples: 0,
      budgetMisses: 0,
      budgetMissRate: 0,
      physicsSamples: 0,
      physicsBudgetMisses: 0,
      frameSamples: 0,
      warmupFrameSamples: 0,
      maxSampleMs: 0,
      maxFrameMs: 0,
      maxRenderMs: 0,
      maxWarmupFrameMs: 0,
      maxWarmupRenderMs: 0,
      maxVelocity: 0,
      overlapPressureEvents: 0,
      communityPackingEvents: 0,
      lastTick: 0,
      frameBudget: num(root, 'data-cfx-performance-frame-budget', 16)
    };
    const sampleMs = Number.isFinite(detail.sampleMs) ? detail.sampleMs : 0;
    const sampleTicks = Math.max(1, Number(detail.sampleTicks) || 1);
    const frameSample = detail.mode === 'frame';
    const warmupSample = detail.mode === 'warmup';
    const physicsSample = detail.mode === 'physics';
    const sampleBudgetMs = summary.frameBudget * (frameSample || warmupSample ? 1 : sampleTicks);
    summary.samples += frameSample ? 1 : 0;
    summary.frameSamples += frameSample ? 1 : 0;
    summary.warmupFrameSamples += warmupSample ? 1 : 0;
    summary.physicsSamples += physicsSample ? 1 : 0;
    summary.lastTick = Number.isFinite(detail.tick) ? detail.tick : summary.lastTick;
    summary.maxVelocity = Math.max(summary.maxVelocity, Number.isFinite(detail.maxVelocity) ? detail.maxVelocity : 0);
    summary.overlapPressureEvents += Math.max(0, Number.isFinite(detail.overlaps) ? detail.overlaps : 0);
    summary.communityPackingEvents += Math.max(0, Number.isFinite(detail.communityPushes) ? detail.communityPushes : 0);
    summary.maxSampleMs = Math.max(summary.maxSampleMs, sampleMs);
    summary.maxFrameMs = Math.max(summary.maxFrameMs, frameSample ? sampleMs : 0);
    summary.maxRenderMs = Math.max(summary.maxRenderMs, frameSample && Number.isFinite(detail.renderMs) ? detail.renderMs : 0);
    summary.maxWarmupFrameMs = Math.max(summary.maxWarmupFrameMs, warmupSample ? sampleMs : 0);
    summary.maxWarmupRenderMs = Math.max(summary.maxWarmupRenderMs, warmupSample && Number.isFinite(detail.renderMs) ? detail.renderMs : 0);
    summary.lastSampleMs = sampleMs;
    summary.lastSampleTicks = sampleTicks;
    summary.lastSampleBudgetMs = sampleBudgetMs;
    summary.thread = detail.thread || summary.thread || '';
    summary.acceleration = detail.acceleration || summary.acceleration || '';
    summary.renderer = root.dataset.cfxGraphRendererActive || attr(root, 'data-cfx-graph-renderer');
    summary.budgetMisses += frameSample && sampleMs > sampleBudgetMs ? 1 : 0;
    summary.budgetMissRate = summary.frameSamples > 0 ? summary.budgetMisses / summary.frameSamples : 0;
    summary.physicsBudgetMisses += physicsSample && sampleMs > sampleBudgetMs ? 1 : 0;
    root.__cfxGraphPerformanceSummary = summary;
    root.dataset.cfxGraphPerformanceSamples = String(summary.samples);
    root.dataset.cfxGraphPerformanceLastTick = String(summary.lastTick);
    root.dataset.cfxGraphPerformanceLastSampleMs = sampleMs.toFixed(3);
    root.dataset.cfxGraphPerformanceMaxSampleMs = summary.maxSampleMs.toFixed(3);
    root.dataset.cfxGraphPerformanceFrameSamples = String(summary.frameSamples);
    root.dataset.cfxGraphPerformanceMaxFrameMs = summary.maxFrameMs.toFixed(3);
    root.dataset.cfxGraphPerformanceMaxRenderMs = summary.maxRenderMs.toFixed(3);
    root.dataset.cfxGraphPerformanceWarmupFrameSamples = String(summary.warmupFrameSamples);
    root.dataset.cfxGraphPerformanceMaxWarmupFrameMs = summary.maxWarmupFrameMs.toFixed(3);
    root.dataset.cfxGraphPerformanceMaxWarmupRenderMs = summary.maxWarmupRenderMs.toFixed(3);
    root.dataset.cfxGraphPerformancePhysicsSamples = String(summary.physicsSamples);
    root.dataset.cfxGraphPerformancePhysicsBudgetMisses = String(summary.physicsBudgetMisses);
    root.dataset.cfxGraphPerformanceOverlapPressureEvents = String(summary.overlapPressureEvents);
    root.dataset.cfxGraphPerformanceCommunityPackingEvents = String(summary.communityPackingEvents);
    root.dataset.cfxGraphPerformanceSampleTicks = String(sampleTicks);
    root.dataset.cfxGraphPerformanceSampleBudgetMs = sampleBudgetMs.toFixed(3);
    root.dataset.cfxGraphPerformanceBudgetMisses = String(summary.budgetMisses);
    root.dataset.cfxGraphPerformanceBudgetMissRate = summary.budgetMissRate.toFixed(4);
    root.dataset.cfxGraphPerformanceThread = summary.thread;
    root.dataset.cfxGraphPerformanceAcceleration = summary.acceleration;
    const sustainedOverBudget = summary.budgetMisses >= 2 && summary.budgetMissRate > .2;
    root.dataset.cfxGraphPerformanceBudget = sustainedOverBudget ? 'over-budget' : 'within-budget';
    if (detail.publish !== false) emit(root, 'cfxgraphperformance', { ...detail, summary: { ...summary } });
  };
  const performanceClock = () => typeof performance !== 'undefined' && typeof performance.now === 'function' ? performance.now() : Date.now();
  const recordFramePerformance = (root, timestamp, renderMs, thread) => {
    if (!hasFeature(root, 'PerformanceTelemetry')) return;
    const previous = root.__cfxGraphPerformanceFrameTimestamp;
    root.__cfxGraphPerformanceFrameTimestamp = timestamp;
    if (!Number.isFinite(previous)) return;
    const frameMs = Math.max(0, timestamp - previous);
    const count = (root.__cfxGraphPerformanceFrameCount || 0) + 1;
    root.__cfxGraphPerformanceFrameCount = count;
    const interval = Math.max(1, num(root, 'data-cfx-performance-telemetry-interval', 30));
    const budget = num(root, 'data-cfx-performance-frame-budget', 16);
    const warmupFrames = Math.max(0, num(root, 'data-cfx-performance-warmup-frames', 4));
    if (count <= warmupFrames) {
      publishPerformance(root, { graphId: attr(root, 'data-cfx-graph-id'), mode: 'warmup', renderer: root.dataset.cfxGraphRendererActive, thread, sampleMs: frameMs, sampleTicks: 1, renderMs: Math.max(0, renderMs) });
      return;
    }
    const steadyCount = count - warmupFrames;
    const publish = steadyCount === 1 || steadyCount % interval === 0 || frameMs > budget;
    publishPerformance(root, { graphId: attr(root, 'data-cfx-graph-id'), mode: 'frame', renderer: root.dataset.cfxGraphRendererActive, thread, sampleMs: frameMs, sampleTicks: 1, renderMs: Math.max(0, renderMs), publish });
  };
  const performanceGate = (root) => {
    const totalNodeCount = Number(attr(root, 'data-cfx-graph-node-count'));
    const totalEdgeCount = Number(attr(root, 'data-cfx-graph-edge-count'));
    const visibleNodeCount = attr(root, 'data-cfx-graph-visible-nodes');
    const visibleEdgeCount = attr(root, 'data-cfx-graph-visible-edges');
    const nodeCount = visibleNodeCount === '' ? totalNodeCount : Number(visibleNodeCount);
    const edgeCount = visibleEdgeCount === '' ? totalEdgeCount : Number(visibleEdgeCount);
    const renderer = root.dataset.cfxGraphRendererActive;
    const nodeLimit = renderer === 'webgl' ? num(root, 'data-cfx-performance-max-webgl-nodes', 20000) : renderer === 'canvas' ? num(root, 'data-cfx-performance-max-canvas-nodes', 5000) : num(root, 'data-cfx-performance-max-svg-nodes', 1200);
    const edgeLimit = renderer === 'webgl' ? num(root, 'data-cfx-performance-max-webgl-edges', 50000) : renderer === 'canvas' ? num(root, 'data-cfx-performance-max-canvas-edges', 12000) : num(root, 'data-cfx-performance-max-svg-edges', 3000);
    const gated = nodeCount > nodeLimit || edgeCount > edgeLimit;
    root.classList.toggle('cfx-graph-performance-gated', gated);
    root.dataset.cfxGraphPerformance = gated ? 'gated' : 'interactive';
    if (gated) publishPerformance(root, { graphId: attr(root, 'data-cfx-graph-id'), mode: 'gated', renderer: root.dataset.cfxGraphRendererActive, nodeCount, edgeCount, totalNodeCount, totalEdgeCount, nodeLimit, edgeLimit });
    return gated;
  };
