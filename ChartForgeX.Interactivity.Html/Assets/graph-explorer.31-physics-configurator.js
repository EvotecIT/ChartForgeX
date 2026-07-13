  const physicsCommonFields = {
    minVelocity: 'data-cfx-physics-min-velocity',
    maxVelocity: 'data-cfx-physics-max-velocity',
    timestep: 'data-cfx-physics-timestep',
    iterations: 'data-cfx-physics-stabilization-iterations',
    adaptiveTimestep: 'data-cfx-physics-adaptive-timestep'
  };
  const physicsSolverFields = {
    BarnesHut: new Set(['theta', 'gravitationalConstant', 'centralGravity', 'springLength', 'springConstant', 'damping', 'avoidOverlap']),
    ForceAtlas2: new Set(['theta', 'gravitationalConstant', 'centralGravity', 'springLength', 'springConstant', 'damping', 'avoidOverlap']),
    Repulsion: new Set(['nodeDistance', 'strength', 'centralGravity', 'springLength', 'springConstant', 'damping', 'avoidOverlap']),
    HierarchicalRepulsion: new Set(['nodeDistance', 'strength', 'centralGravity', 'springLength', 'springConstant', 'damping', 'avoidOverlap'])
  };
  const physicsKebab = (value) => String(value).replace(/[A-Z]/g, letter => `-${letter.toLowerCase()}`);
  const physicsFieldAttribute = (root, field) => physicsCommonFields[field] || physicsProfileAttribute(attr(root, 'data-cfx-graph-physics'), physicsKebab(field));
  const physicsFieldSupported = (solver, field) => !!physicsCommonFields[field] || physicsSolverFields[solver]?.has(field) === true;
  const physicsFieldValue = (root, field) => attr(root, physicsFieldAttribute(root, field));
  const syncPhysicsConfigurator = (root) => {
    const panel = root.querySelector('[data-cfx-role="graph-physics-configurator"]');
    if (!panel) return;
    const solver = attr(root, 'data-cfx-graph-physics');
    const solverInput = panel.querySelector('[data-cfx-physics-field="solver"]');
    if (solverInput) solverInput.value = solver;
    items(panel, '[data-cfx-physics-field]').forEach(input => {
      const field = attr(input, 'data-cfx-physics-field');
      if (!field || field === 'solver') return;
      const supported = physicsFieldSupported(solver, field);
      input.disabled = !supported;
      const value = physicsFieldValue(root, field);
      if (input.type === 'checkbox') input.checked = value === 'true';
      else if (supported && value !== '') input.value = value;
    });
    const status = panel.querySelector('[data-cfx-role="graph-physics-status"]');
    if (status) status.textContent = `${solver} · ${root.dataset.cfxGraphPhysicsState || 'ready'} · ${root.dataset.cfxGraphPhysicsThread || 'idle'}`;
  };
  const physicsConfigurationAttributes = (root) => Array.from(root.attributes).filter(attribute => attribute.name === 'data-cfx-graph-physics' || attribute.name.startsWith('data-cfx-physics-')).map(attribute => [attribute.name, attribute.value]);
  const applyPhysicsConfiguration = (root, configuration) => {
    configuration = configuration || {};
    if (configuration.solver && physicsSolverFields[configuration.solver]) root.setAttribute('data-cfx-graph-physics', configuration.solver);
    Object.keys(configuration).forEach(field => {
      if (field === 'solver' || !physicsFieldSupported(attr(root, 'data-cfx-graph-physics'), field)) return;
      const value = configuration[field];
      if (typeof value === 'boolean') root.setAttribute(physicsFieldAttribute(root, field), value ? 'true' : 'false');
      else if (Number.isFinite(Number(value))) root.setAttribute(physicsFieldAttribute(root, field), String(Number(value)));
    });
    reheatPhysics(root, 'configuration', { rebuild: true, fit: false }); syncPhysicsConfigurator(root);
    emit(root, 'cfxgraphphysicschange', { graphId: attr(root, 'data-cfx-graph-id'), solver: attr(root, 'data-cfx-graph-physics'), settings: profile(root) });
    return profile(root);
  };
  const bindPhysicsConfigurator = (root) => {
    const panel = root.querySelector('[data-cfx-role="graph-physics-configurator"]');
    if (!panel || panel.dataset.cfxPhysicsBound === 'true') return;
    panel.dataset.cfxPhysicsBound = 'true';
    root.__cfxGraphPhysicsDefaults = physicsConfigurationAttributes(root);
    panel.addEventListener('change', event => {
      const input = event.target.closest?.('[data-cfx-physics-field]');
      if (!input) return;
      const field = attr(input, 'data-cfx-physics-field');
      applyPhysicsConfiguration(root, field === 'solver' ? { solver: input.value } : { [field]: input.type === 'checkbox' ? input.checked : Number(input.value) });
    });
    panel.addEventListener('click', event => {
      const action = attr(event.target.closest?.('[data-cfx-physics-action]'), 'data-cfx-physics-action');
      if (action === 'reheat') reheatPhysics(root, 'configurator', { rebuild: true, fit: false });
      if (action === 'reset') {
        (root.__cfxGraphPhysicsDefaults || []).forEach(entry => root.setAttribute(entry[0], entry[1]));
        reheatPhysics(root, 'configuration-reset', { rebuild: true, fit: false });
      }
      if (action) syncPhysicsConfigurator(root);
    });
    root.addEventListener('cfxgraphstabilized', () => syncPhysicsConfigurator(root));
    syncPhysicsConfigurator(root);
  };
