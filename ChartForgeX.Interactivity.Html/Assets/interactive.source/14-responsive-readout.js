  const bindResponsiveReadout = root => {
    const stage = root.querySelector('.cfx-stage'), svg = stage?.querySelector('svg');
    if (typeof svg?.getScreenCTM !== 'function' || typeof svg?.getBBox !== 'function') return;
    if (svg.closest('[data-cfx-responsive-chart]')) return;
    const headings = Array.from(svg.querySelectorAll('[data-cfx-role="frame-heading-source"],[data-cfx-role="frame-subtitle-source"]'));
    const visibility = new Map(headings.map(node => [node, node.style.visibility]));
    const title = svg.querySelector('title')?.textContent || headings[0]?.textContent || '';
    // Native headings can wrap into several sources; lines sharing the first font request belong to the title.
    const caption = node => (Array.from(node.querySelectorAll('[data-cfx-caption]'), line => line.dataset.cfxCaption).join(' ') || node.textContent || '').trim();
    const font = node => node.getAttribute('data-cfx-authored-font-request');
    const subtitleText = Array.from(new Set(headings.filter(node => node.dataset.cfxRole === 'frame-subtitle-source' || font(node) !== font(headings[0]))
      .map(caption).filter(Boolean))).join(' ');
    const heading = document.createElement('div'); heading.className = 'cfx-responsive-heading'; heading.textContent = title;
    if (subtitleText && subtitleText !== title) {
      const subtitle = document.createElement('span'); subtitle.className = 'cfx-responsive-heading__subtitle';
      subtitle.textContent = subtitleText; heading.appendChild(subtitle);
    }
    heading.style.display = 'none';
    const frameNode = stage.parentElement, legend = frameNode.querySelector(':scope > .cfx-responsive-legend');
    (legend || stage).before(heading);
    const printMedia = window.matchMedia?.('print');
    const toolbar = root.querySelector(':scope > .cfx-frame > .cfx-toolbar');
    const toolbarHeight = root.style.getPropertyValue('--cfx-toolbar-height');
    const card = svg.querySelector('[data-cfx-role="frame-card"]');
    const cardTokens = ['fill', 'stroke', 'stroke-width', 'radius', 'inset', 'width'].map(name => '--cfx-card-' + name);
    // A narrow reader replaces the native heading band with wrapping HTML text inside the
    // same card, followed by the flowing legend. The native heading and legend bands are
    // clipped away so no empty strip remains above the plot.
    const placeHeading = shown => {
      heading.style.display = shown ? 'block' : 'none';
      heading.style.removeProperty('padding-inline-end'); heading.style.removeProperty('padding-block-start');
      delete frameNode.dataset.cfxCardHead; cardTokens.forEach(name => frameNode.style.removeProperty(name));
      delete frameNode.dataset.cfxHeadingCrop; frameNode.style.removeProperty('--cfx-heading-crop');
      if (!shown) return;
      // Layout scale, independent of any legacy CSS zoom transform on the drawing.
      const scale = svg.clientWidth / (svg.viewBox.baseVal?.width || svg.clientWidth);
      const bottom = Math.max(...headings.map(node => { const box = node.getBBox(); return box.y + box.height; }),
        ...Array.from(svg.querySelectorAll('[data-cfx-role="legend-native"]'), node => node._cfxNativeRegion ? node._cfxNativeRegion.y + node._cfxNativeRegion.height : -Infinity));
      const plotTop = root._cfxDataProjection?.plotBounds?.y;
      const crop = Math.min(bottom + 4, Number.isFinite(plotTop) ? plotTop - 16 : bottom) - (svg.viewBox.baseVal?.y || 0);
      if (!(scale > 0) || !(crop > 0)) return;
      // The stylesheet clips the drawing's band on a whole pixel; the drawing element itself is never restyled.
      frameNode.dataset.cfxHeadingCrop = 'true'; frameNode.style.setProperty('--cfx-heading-crop', Math.round(crop * scale) + 'px');
      // The title wraps beside the toolbar, or sits below it when the remaining line would be too short.
      if (toolbar) {
        // Reserve room for Reset zoom too, so the toolbar never grows over the title.
        const reset = toolbar.querySelector('[data-cfx-zoom-reset][hidden]') && toolbar.querySelector('.cfx-tool--icon:not([hidden])');
        const width = toolbar.offsetWidth + (reset ? reset.offsetWidth + 2 : 0);
        if (frameNode.clientWidth - width - 32 >= 140) heading.style.paddingInlineEnd = width + 16 + 'px';
        else heading.style.paddingBlockStart = toolbar.offsetHeight + 16 + 'px';
      }
      if (!card) return;
      const paint = getComputedStyle(card), box = card.getBoundingClientRect(), frameBox = frameNode.getBoundingClientRect();
      const stroke = (parseFloat(paint.strokeWidth) || 0) * scale;
      // A readable drawing wider than its host scrolls on its own; the heading keeps the host width.
      if (box.width > frameBox.width + 1) return;
      frameNode.dataset.cfxCardHead = 'true';
      [paint.fill, paint.stroke === 'none' ? 'transparent' : paint.stroke, stroke + 'px', (Number(card.getAttribute('rx')) || 0) * scale + 'px',
        box.left - frameBox.left - stroke / 2 + 'px', box.width + stroke + 'px'].forEach((value, index) => frameNode.style.setProperty(cardTokens[index], value));
    };
    let frame = 0;
    const refresh = () => {
      frame = 0; if (!root.isConnected || chartLifetimes.get(root)?.disposed !== false) return;
      const compact = root.getBoundingClientRect().width < 560;
      const printing = printMedia?.matches === true || root._cfxStandaloneViewport?.printing === true;
      if (toolbar && root.dataset.cfxToolbar === 'compact') {
        const height = toolbar.offsetHeight + 'px';
        if (root.style.getPropertyValue('--cfx-toolbar-height') !== height) root.style.setProperty('--cfx-toolbar-height', height);
      }
      placeHeading(!printing && compact && !!title && headings.length > 0);
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
    ownChartWork(root, () => { if (frame) cancelAnimationFrame(frame); observer?.disconnect(); heading.remove(); delete frameNode.dataset.cfxCardHead; delete frameNode.dataset.cfxHeadingCrop; [...cardTokens, '--cfx-heading-crop'].forEach(name => frameNode.style.removeProperty(name)); headings.forEach(node => node.style.visibility = visibility.get(node)); if (toolbarHeight) root.style.setProperty('--cfx-toolbar-height', toolbarHeight); else root.style.removeProperty('--cfx-toolbar-height'); });
    refresh();
  };
