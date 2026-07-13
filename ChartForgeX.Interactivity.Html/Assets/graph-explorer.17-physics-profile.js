  const physicsSolverPrefix = (solver) => {
    if (solver === 'BarnesHut') return 'barnes-hut';
    if (solver === 'ForceAtlas2') return 'force-atlas2';
    if (solver === 'HierarchicalRepulsion') return 'hierarchical-repulsion';
    return 'repulsion';
  };
  const physicsProfileAttribute = (solver, field) => `data-cfx-physics-${physicsSolverPrefix(solver)}-${field}`;
  const physicsProfileValue = (root, solver, field, fallback) => num(root, physicsProfileAttribute(solver, field), fallback);
  const profile = (root) => {
    const solver = attr(root, 'data-cfx-graph-physics');
    const common = {
      solver,
      iterations: Math.max(1, num(root, 'data-cfx-physics-stabilization-iterations', 1000)),
      progressInterval: Math.max(1, num(root, 'data-cfx-physics-stabilization-update-interval', 4)),
      stabilizationFit: attr(root, 'data-cfx-physics-stabilization-fit') !== 'false',
      minVelocity: Math.max(0.001, num(root, 'data-cfx-physics-min-velocity', 0.1)),
      maxVelocity: Math.max(0.01, num(root, 'data-cfx-physics-max-velocity', 50)),
      timestep: Math.max(0.01, num(root, 'data-cfx-physics-timestep', 0.5)),
      adaptiveTimestep: attr(root, 'data-cfx-physics-adaptive-timestep') === 'true'
    };
    const shared = {
      ...common,
      centerGravity: Math.max(0, physicsProfileValue(root, solver, 'central-gravity', 0.01)),
      springLength: Math.max(1, physicsProfileValue(root, solver, 'spring-length', 100)),
      springConstant: Math.max(0.0001, physicsProfileValue(root, solver, 'spring-constant', 0.04)),
      damping: Math.min(1, Math.max(0, physicsProfileValue(root, solver, 'damping', 0.09))),
      avoidOverlap: Math.min(1, Math.max(0, physicsProfileValue(root, solver, 'avoid-overlap', 0))),
      theta: Math.max(0.05, physicsProfileValue(root, solver, 'theta', 0.5)),
      gravitationalConstant: Math.min(-0.01, physicsProfileValue(root, solver, 'gravitational-constant', solver === 'ForceAtlas2' ? -50 : -2000)),
      nodeDistance: Math.max(1, physicsProfileValue(root, solver, 'node-distance', solver === 'HierarchicalRepulsion' ? 120 : 100)),
      repulsionStrength: Math.max(0.01, physicsProfileValue(root, solver, 'strength', 1))
    };
    return { ...shared, linkDistance: shared.springLength, repulsion: Math.abs(shared.gravitationalConstant) };
  };
  const updateAdaptiveTimestep = (settings, maxVelocity) => {
    if (!settings.adaptiveTimestep) return settings.timestep;
    const current = Number.isFinite(settings.currentTimestep) ? settings.currentTimestep : settings.timestep;
    const pressure = maxVelocity / Math.max(settings.minVelocity, settings.maxVelocity);
    const next = pressure > 0.65 ? current * 0.86 : pressure < 0.12 ? current * 1.06 : current;
    settings.currentTimestep = Math.max(settings.timestep, Math.min(settings.timestep * 2.5, next));
    return settings.currentTimestep;
  };
