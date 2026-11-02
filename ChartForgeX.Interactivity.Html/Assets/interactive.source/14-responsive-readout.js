  const bindResponsiveReadout = root => {
    const stage = root.querySelector('.cfx-stage'), svg = stage?.querySelector('svg');
    if (typeof svg?.getScreenCTM !== 'function' || typeof svg?.getBBox !== 'function') return;
    if (svg.closest('[data-cfx-responsive-chart]')) return;
    const headings = Array.from(svg.querySelectorAll('[data-cfx-role="frame-heading-source"],[data-cfx-role="frame-subtitle-source"]'));
    const visibility = new Map(headings.map(node => [node, node.style.visibility]));
    const title = svg.querySelector('title')?.textContent || headings[0]?.textContent || '';
    const heading = document.createElement('div'); heading.className = 'cfx-responsive-heading'; heading.textContent = title;
    heading.style.cssText = 'display:none;font:600 14px/20px var(--cfx-tooltip-font);color:var(--cfx-tooltip-text);padding:0 4px 8px';
    stage.before(heading);
    const printMedia = window.matchMedia?.('print');
    const toolbar = root.querySelector(':scope > .cfx-frame > .cfx-toolbar');
    const toolbarHeight = root.style.getPropertyValue('--cfx-toolbar-height');
    let frame = 0;
    const refresh = () => {
      frame = 0; if (!root.isConnected || chartLifetimes.get(root)?.disposed !== false) return;
      const compact = root.getBoundingClientRect().width < 560;
      const printing = printMedia?.matches === true || root._cfxStandaloneViewport?.printing === true;
      if (toolbar && root.dataset.cfxToolbar === 'compact') {
        const height = toolbar.offsetHeight + 'px';
        if (root.style.getPropertyValue('--cfx-toolbar-height') !== height) root.style.setProperty('--cfx-toolbar-height', height);
      }
      heading.style.display = !printing && compact && title && headings.length ? 'block' : 'none';
      headings.forEach(node => node.style.visibility = compact && !printing ? 'hidden' : visibility.get(node));
      const view = (hasFeature(root, 'Zoom') || hasFeature(root, 'LegendToggles')) && standaloneViewport(root);
      if (view) paintStandaloneAxes(root, view);
    };
    const queue = () => { if (!frame) frame = requestAnimationFrame(refresh); };
    // The toolbar matters only for its height; Reset zoom widening it must not repaint the axes.
    let observedToolbarHeight = -1;
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(entries => {
      let changed = false;
      for (const entry of entries) changed = entry.target !== toolbar || observedToolbarHeight !== (observedToolbarHeight = entry.contentRect.height) || changed;
      if (changed) queue();
    });
    observer?.observe(root); listenChart(root, window, 'resize', queue);
    if (toolbar) observer?.observe(toolbar);
    if (printMedia?.addEventListener) listenChart(root, printMedia, 'change', refresh);
    ownChartWork(root, () => { if (frame) cancelAnimationFrame(frame); observer?.disconnect(); heading.remove(); headings.forEach(node => node.style.visibility = visibility.get(node)); if (toolbarHeight) root.style.setProperty('--cfx-toolbar-height', toolbarHeight); else root.style.removeProperty('--cfx-toolbar-height'); });
    refresh();
  };
