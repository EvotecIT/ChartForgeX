  // Root owns inert alternates.

  const adaptiveTemplate = (root, name) => Array.from(root.children)
    .find(node => node instanceof HTMLTemplateElement && node.hasAttribute('data-cfx-adaptive-' + name));
  const adaptiveSectionCopy = section => {
    const copy = section.cloneNode(true);
    for (const name of ['wide', 'narrow']) adaptiveTemplate(copy, name)?.remove();
    copy.removeAttribute('data-cfx-runtime-bound');
    return copy;
  };
  const readAdaptiveLayout = root => {
    if (root.dataset.cfxResponsiveLayout !== 'adaptive') return null;
    const breakpoint = Number(root.dataset.cfxAdaptiveBreakpoint), current = root.dataset.cfxAdaptiveDrawing || 'wide';
    const narrowTemplate = adaptiveTemplate(root, 'narrow'), wideTemplate = adaptiveTemplate(root, 'wide');
    if (!Number.isFinite(breakpoint) || breakpoint <= 0 || !['wide', 'narrow'].includes(current)
      || !narrowTemplate || narrowTemplate.content.children.length !== 1
      || wideTemplate && wideTemplate.content.children.length !== 1 || current === 'narrow' && !wideTemplate)
      throw new TypeError('Adaptive charts require a valid breakpoint and matching native drawings.');
    const wide = wideTemplate ? adaptiveSectionCopy(readChartSection(wideTemplate.content.firstElementChild, true)) : adaptiveSectionCopy(root);
    const narrow = adaptiveSectionCopy(readChartSection(narrowTemplate.content.firstElementChild, true));
    const source = JSON.stringify(JSON.parse(wide.dataset.cfxPreparedChart).series);
    for (const section of [wide, narrow]) if (section.dataset.cfxChartId !== root.dataset.cfxChartId
      || section.dataset.cfxResponsiveLayout !== 'adaptive'
      || JSON.stringify(JSON.parse(section.dataset.cfxPreparedChart).series) !== source)
      throw new TypeError('Adaptive drawings must retain the same chart and source identities.');
    return { wide, narrow, breakpoint, current };
  };
  const prepareAdaptiveRoot = (root, lifetime) => {
    const adaptive = lifetime.adaptive;
    if (!adaptive) return;
    root.dataset.cfxAdaptiveDrawing = adaptive.current;
    for (const name of ['wide', 'narrow']) {
      adaptiveTemplate(root, name)?.remove();
      const template = root.ownerDocument.createElement('template');
      template.setAttribute('data-cfx-adaptive-' + name, 'true');
      template.content.appendChild(adaptive[name].cloneNode(true));
      root.appendChild(template);
    }
  };
  const bindAdaptiveLayout = root => {
    const lifetime = chartLifetimes.get(root), adaptive = lifetime?.adaptive;
    if (!adaptive) return;
    let frame = 0;
    const refresh = () => {
      frame = 0;
      if (lifetime.disposed || chartLifetimes.get(root) !== lifetime || !root.isConnected) return;
      const width = root.getBoundingClientRect().width;
      if (width <= 0) return;
      const drawing = width < adaptive.breakpoint ? 'narrow' : 'wide';
      if (drawing === adaptive.current) return;
      const previousSceneRevision = lifetime.sceneRevision;
      replaceChartSection(root, lifetime, adaptive[drawing].cloneNode(true), '', drawing);
      emitHostEvent(root, 'cfxlayoutchange', { drawing, previousSceneRevision, sceneRevision: lifetime.sceneRevision });
    };
    const queue = () => { if (!frame) frame = requestAnimationFrame(refresh); };
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(queue);
    observer?.observe(root);
    if (!observer) listenChart(root, window, 'resize', queue);
    ownChartWork(root, () => { observer?.disconnect(); if (frame) cancelAnimationFrame(frame); });
    queue();
  };
