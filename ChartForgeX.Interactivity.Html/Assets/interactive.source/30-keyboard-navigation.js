  // Adapter emphasis preserves roving membership; host/native classes still invalidate availability.
  const keyboardPaintClasses = new Set(['cfx-hovered', 'cfx-hover-related', 'cfx-hover-column', 'cfx-hover-series',
    'cfx-selected', 'cfx-highlighted', 'cfx-linked-dim', 'cfx-selection-dim', 'cfx-linked-series-active',
    'cfx-linked-series-dim', 'cfx-trail']);
  const keyboardNativeClasses = value => (value || '').split(/\s+/).filter(value => value && !keyboardPaintClasses.has(value)).sort().join(' ');
  const keyboardNativeStyle = value => (value || '').replace(/--cfx-highlight-(?:opacity|stroke|stroke-width)\s*:[^;]*(?:;|$)/g, '').trim();
  const keyboardAvailabilityMutation = (record, stage, ancestry) => {
    if (record.type === 'attributes' && record.attributeName === 'class'
      && keyboardNativeClasses(record.oldValue) === keyboardNativeClasses(record.target.getAttribute('class'))) return false;
    if (record.type === 'attributes' && record.target.closest('[data-cfx-highlight-outline],[data-cfx-keyboard-focus-outline]')) return false;
    if (record.type === 'attributes' && record.attributeName === 'style' && record.target.matches('[data-cfx-highlight-paint]')
      && keyboardNativeStyle(record.oldValue) === keyboardNativeStyle(record.target.getAttribute('style'))) return false;
    // Only SVG marks affect navigation; ResizeObserver handles chrome layout.
    const svg = stage.querySelector('svg');
    if (record.type === 'attributes') return record.target === stage || !!svg?.contains(record.target) || ancestry.nodes.includes(record.target);
    const changes = [...record.addedNodes, ...record.removedNodes];
    const adapterPaint = node => node.nodeType === 1 && node.matches('[data-cfx-highlight-outline],[data-cfx-keyboard-focus-outline],.cfx-prepared-point-marker,[data-cfx-browser-hit-area]');
    if (changes.length && changes.every(adapterPaint)) return false;
    return !!svg?.contains(record.target) || changes.some(node => node.contains(stage) || svg && node.contains(svg)
      || ancestry.nodes.some(boundary => node.contains(boundary)));
  };
  // Data and legend rove separately on rendered marks.
  const keyboardTargetAvailable = (node) => {
    if (node.dataset.cfxVisible === 'false') return false;
    if (node.closest('[aria-hidden="true"]') || !paintAncestorsVisible(node, new Map())) return false;
    // Muted data leaves navigation; its legend remains reachable.
    if (renderedTargetKind(node) !== 'legend' && node.closest('.cfx-series-muted')) return false;
    const style = getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden' || style.visibility === 'collapse') return false;
    if (pointerTargetPaint(node)) return true;
    // Retained facts can have no filled geometry.
    const data = node.dataset;
    if (!data.cfxTargetKind || !data.cfxTargetId || !retainedGeometryFact(node)) return false;
    const svg = node.ownerSVGElement;
    if (!svg) return false;
    // Check CSS-hidden ancestors as well as viewport visibility.
    for (let parent = node.parentElement; parent && parent !== svg; parent = parent.parentElement) {
      if (getComputedStyle(parent).display === 'none') return false;
    }
    const viewport = svg.getBoundingClientRect();
    return viewport.width > 0 && viewport.height > 0;
  };
  const keyboardTargets = (root) => withNativeHighlightPaint(root, () => {
    const candidates = interactiveTargets(root).filter(keyboardTargetAvailable);
    const candidateSet = new Set(candidates);
    const containers = new Set();
    candidates.forEach((node) => {
      for (let parent = node.parentElement; parent && parent !== root; parent = parent.parentElement) {
        if (candidateSet.has(parent)) containers.add(parent);
      }
    });
    const seen = new Set();
    return candidates.filter((node) => {
      // A containing series is useful to pointer hover, but it is not an extra keyboard datum.
      if (containers.has(node)) return false;
      const focusNode = targetFocusNode(node);
      if (seen.has(focusNode)) return false;
      seen.add(focusNode);
      return true;
    });
  });
  const keyboardDataGroups = (nodes) => {
    const groups = new Map();
    nodes.forEach((node) => {
      // Cartesian observations and matrix rows share the declared series contract. Other families use target order.
      const owner = node.closest('[data-cfx-role="series"][data-cfx-series]');
      const series = owner ? owner.dataset.cfxSeries : null;
      const key = series === null ? 'targets' : 'series:' + series;
      if (!groups.has(key)) groups.set(key, { series, nodes: [] });
      groups.get(key).nodes.push(node);
    });
    const ordered = Array.from(groups.values()).sort((a, b) => a.series === null ? 1 : b.series === null ? -1 : Number(a.series) - Number(b.series));
    ordered.forEach((group) => {
      if (group.series === null) return;
      group.nodes.sort((a, b) => Number(a.dataset.cfxPoint ?? 0) - Number(b.dataset.cfxPoint ?? 0));
    });
    return ordered;
  };
  const placeKeyboardLegendAfterData = (root, data, legends) => {
    const svg = root.querySelector('.cfx-stage svg');
    if (!svg || !data.length) return;
    const branches = new Set();
    legends.forEach((node) => {
      let branch = node;
      while (branch.parentElement && branch.parentElement !== svg) branch = branch.parentElement;
      // Move the intact legend clip/group, retaining transforms and labels. Never move a branch containing data.
      if (branch.parentElement === svg && !branches.has(branch) && !data.some((mark) => branch.contains(mark))) branches.add(branch);
    });
    branches.forEach((branch) => svg.appendChild(branch));
  };
  const refreshKeyboardNavigation = (root, focused, passive = false) => {
    refreshRootBranchHighlightAvailability(root);
    const state = root._cfxKeyboardNavigation;
    if (!state || !hasFeature(root, 'KeyboardNavigation')) return null;
    const activeElement = root.getRootNode().activeElement;
    const activeOwned = root.contains(activeElement) && state.owned.has(activeElement);
    const targets = keyboardTargets(root);
    state.legends = targets.filter((node) => renderedTargetKind(node) === 'legend');
    state.groups = keyboardDataGroups(targets.filter((node) => renderedTargetKind(node) !== 'legend'));
    state.data = state.groups.flatMap((group) => group.nodes);
    // Keep component positions while the host is hidden.
    if (state.data.length && !state.data.includes(state.activeData)) state.activeData = state.data[0];
    if (state.legends.length && !state.legends.includes(state.activeLegend)) state.activeLegend = state.legends[0];
    if (state.data.includes(focused)) state.activeData = focused;
    if (state.legends.includes(focused)) state.activeLegend = focused;
    if (!state.legendOrderPrepared && state.data.length && state.legends.length) {
      placeKeyboardLegendAfterData(root, state.data, state.legends);
      state.legendOrderPrepared = true;
    }
    state.owned.forEach((node) => node.setAttribute('tabindex', '-1'));
    targets.forEach((node) => {
      const focusNode = targetFocusNode(node);
      state.owned.add(focusNode);
      focusNode.setAttribute('data-cfx-keyboard-component', renderedTargetKind(node) === 'legend' ? 'legend' : 'data');
      focusNode.setAttribute('tabindex', node === state.activeData || node === state.activeLegend ? '0' : '-1');
    });
    if (activeOwned && !targets.some((node) => targetFocusNode(node) === activeElement)) {
      // Local and synchronized state changes must not strand focus on a datum that just left the component.
      const replacement = state.data.length ? state.activeData : state.legends.length ? state.activeLegend : null;
      if (replacement) {
        // Host paint changes recover a usable focus position without opening a new
        // readout in the same update that dismisses the unavailable source or pin.
        if (passive) { clearHover(root, true, true); hideTip(root, root.querySelector('.cfx-tooltip'), true); }
        focusKeyboardTarget(root, replacement, passive);
      }
    }
    refreshKeyboardFocusPaint(root);
    return state;
  };
  const bindKeyboardNavigationAvailability = (root) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    let frame = 0;
    let resizeObserver;
    let paintObserver;
    let ancestry = { nodes: [], trees: [] };
    const unwatchSlots = () => ancestry.trees.forEach(tree => tree.removeEventListener('slotchange', queueRefresh, true));
    const watchAncestry = () => {
      const next = composedAncestry(root);
      if (next.nodes.length === ancestry.nodes.length && next.nodes.every((node, index) => node === ancestry.nodes[index])) return;
      paintObserver.disconnect(); unwatchSlots(); ancestry = next;
      paintObserver.observe(root, { childList: true, subtree: true, attributes: true, attributeOldValue: true, attributeFilter: paintAvailabilityAttributes });
      for (const node of new Set([...ancestry.nodes.slice(1), ...ancestry.trees])) {
        paintObserver.observe(node, { childList: true,
          ...(node instanceof Element ? { attributes: true, attributeOldValue: true, attributeFilter: paintAvailabilityAttributes } : {}) });
      }
      ancestry.trees.forEach(tree => tree.addEventListener('slotchange', queueRefresh, true));
    };
    const queueRefresh = () => {
      if (chartLifetimes.get(root)?.disposed !== false) return;
      if (frame) return;
      frame = requestAnimationFrame(() => {
        frame = 0;
        if (!root.isConnected) {
          if (resizeObserver) resizeObserver.disconnect();
          if (paintObserver) paintObserver.disconnect();
          unwatchSlots();
          window.removeEventListener('resize', queueRefresh);
          return;
        }
        watchAncestry(); refreshKeyboardNavigation(root, undefined, true);
      });
    };
    listenChart(root, window, 'resize', queueRefresh);
    if (typeof ResizeObserver !== 'undefined') {
      // Tabs and other initially hidden hosts acquire layout without a window resize.
      resizeObserver = new ResizeObserver(queueRefresh);
      resizeObserver.observe(stage);
    }
    // Share paint/style availability between Tab and arrow navigation.
    paintObserver = new MutationObserver(records => {
      if (chartLifetimes.get(root)?.disposed !== false) return;
      if (!root.isConnected) { queueRefresh(); return; }
      if (!records.some(record => keyboardAvailabilityMutation(record, stage, ancestry))) return;
      queueRefresh();
    });
    watchAncestry();
    ownChartWork(root, () => {
      if (frame) cancelAnimationFrame(frame);
      resizeObserver?.disconnect(); paintObserver?.disconnect(); unwatchSlots();
    });
  };
  const prepareKeyboardNavigation = (root) => {
    if (!hasFeature(root, 'KeyboardNavigation')) {
      if (root._cfxBranchHighlights) bindKeyboardNavigationAvailability(root);
      return;
    }
    root._cfxKeyboardNavigation = { owned: new Set() };
    const stage = root.querySelector('.cfx-stage');
    // Keep native scroll stages outside the roving tab order.
    if (stage && !stage.hasAttribute('tabindex')) {
      stage.setAttribute('tabindex', '-1');
      ownChartWork(root, () => { if (stage.getAttribute('tabindex') === '-1') stage.removeAttribute('tabindex'); });
    }
    // Only roving leaves, including SVG groups, enter the tab order.
    interactiveTargets(root).forEach((node) => root._cfxKeyboardNavigation.owned.add(targetFocusNode(node)));
    refreshKeyboardNavigation(root);
    bindKeyboardNavigationAvailability(root);
  };
  const scrollKeyboardTargetIntoView = (root, node) => {
    const stage = root.querySelector('.cfx-stage');
    const tray = root.querySelector('[data-cfx-compare-tray]');
    const owner = stage?.contains(node) ? stage : tray?.contains(node) ? tray : null;
    if (!owner) return;
    const scrollAxis = (overflow, extent, available, offset, start, end, itemStart, itemEnd) => {
      if (!['auto', 'scroll', 'hidden'].includes(overflow) || extent <= available) return offset;
      const center = (itemStart + itemEnd) / 2;
      if (itemEnd - itemStart > end - start) {
        // Keep the visible portion of a wide mark: pointer focus must not move it between down and up.
        if (itemEnd > start && itemStart < end) return offset;
        return offset + center - (start + end) / 2;
      }
      if (itemStart < start) return offset + itemStart - start;
      if (itemEnd > end) return offset + itemEnd - end;
      return offset;
    };
    let preserveVisiblePortion = false;
    const inset = 16; // Keep focused paint clear of native overlay scrollbars.
    // Reveal inner clips before outer clips, including shadow hosts.
    const revealClips = () => {
      for (const { node: clip, x, y } of tooltipClippingAncestors(node)) {
        const style = getComputedStyle(clip), box = node.getBoundingClientRect();
        const start = stagePoint(clip, { x: box.left, y: box.top });
        const end = stagePoint(clip, { x: box.right, y: box.bottom });
        const left = clip.scrollLeft + inset, top = clip.scrollTop + inset;
        preserveVisiblePortion ||= x && end.x - start.x > clip.clientWidth - inset * 2 && end.x > left && start.x < left + clip.clientWidth - inset * 2
          || y && end.y - start.y > clip.clientHeight - inset * 2 && end.y > top && start.y < top + clip.clientHeight - inset * 2;
        clip.scrollTo({ behavior: 'instant',
          left: scrollAxis(style.overflowX, clip.scrollWidth, clip.clientWidth, clip.scrollLeft, left, left + clip.clientWidth - inset * 2, start.x, end.x),
          top: scrollAxis(style.overflowY, clip.scrollHeight, clip.clientHeight, clip.scrollTop, top, top + clip.clientHeight - inset * 2, start.y, end.y) });
      }
    };
    revealClips();
    const box = node.getBoundingClientRect(), viewport = owner.getBoundingClientRect();
    // Native reveal reaches opaque slots; preserve partly visible wide marks.
    if (!preserveVisiblePortion && box.width <= viewport.width - inset * 2 && box.height <= viewport.height - inset * 2) {
      // Native SVG scrolling can leave an offscreen mark hidden through an opaque slot.
      // A transient HTML footprint lets the browser traverse that host without inspecting it.
      let nativeTarget = node;
      if (node instanceof SVGElement) {
        const start = stagePoint(owner, { x: box.left, y: box.top });
        const end = stagePoint(owner, { x: box.right, y: box.bottom });
        nativeTarget = document.createElement('span');
        nativeTarget.setAttribute('aria-hidden', 'true');
        nativeTarget.style.cssText = 'all:initial;position:absolute;display:block;pointer-events:none;opacity:0;margin:0;padding:0;border:0;'
          + `scroll-margin:${inset}px;left:${start.x}px;top:${start.y}px;width:${end.x - start.x}px;height:${end.y - start.y}px`;
        owner.appendChild(nativeTarget);
      }
      try { nativeTarget.scrollIntoView({ behavior: 'instant', block: 'nearest', inline: 'nearest' }); }
      finally { if (nativeTarget !== node) nativeTarget.remove(); }
      // Native reveal may reset known clips; remeasure after opaque hosts move.
      revealClips();
    }
  };
  const focusKeyboardTarget = (root, node, passive = false) => {
    const focusNode = targetFocusNode(node);
    // Native links also recover focus when adapter keyboard navigation is disabled.
    const recovering = root._cfxRecoveringFocus === true;
    if (passive) root._cfxRecoveringFocus = true;
    try {
      scrollKeyboardTargetIntoView(root, focusNode);
      if (focusNode.focus) {
        try { focusNode.focus({ preventScroll: true }); } catch { focusNode.focus(); }
      }
    } finally { if (passive && !recovering) delete root._cfxRecoveringFocus; }
  };
  const focusAdjacentTarget = (root, node, key) => {
    if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(key)) return false;
    resumePointerReadout(root);
    const state = refreshKeyboardNavigation(root, node);
    if (!state) return false;
    const legend = state.legends.includes(node);
    const group = legend ? null : state.groups.find((item) => item.nodes.includes(node));
    const targets = legend ? state.legends : group ? group.nodes : [];
    if (!targets.length) return false;
    const current = targets.indexOf(node);
    let next = node;
    if (key === 'Home') next = targets[0];
    else if (key === 'End') next = targets[targets.length - 1];
    else if (!legend && (group.series !== null || state.groups.length > 1) && (key === 'ArrowUp' || key === 'ArrowDown')) {
      // Ungrouped annotations are part of the same data component as the Cartesian series.
      const groups = state.groups;
      const nextGroup = groups[clamp(groups.indexOf(group) + (key === 'ArrowUp' ? -1 : 1), 0, groups.length - 1)];
      if (nextGroup !== group) {
        const x = node.dataset.cfxX;
        const sourcePoint = sourcePointIndex(node);
        next = nextGroup.nodes.find((item) => x !== undefined && item.dataset.cfxX === x)
          || nextGroup.nodes.find((item) => sourcePoint !== undefined && sourcePointIndex(item) === sourcePoint)
          || nextGroup.nodes[Math.min(current, nextGroup.nodes.length - 1)];
      }
    } else {
      const direction = key === 'ArrowLeft' || key === 'ArrowUp' ? -1 : 1;
      next = targets[clamp(current + direction, 0, targets.length - 1)];
    }
    if (next === node) {
      // An explicit boundary arrow still resumes the readout after passive recovery.
      const tip = root.querySelector('.cfx-tooltip');
      if (tip?.hidden && tooltipReadoutAvailable(node)) { setHover(root, node, true, true); showTip(root, tip, node); }
      return true;
    }
    refreshKeyboardNavigation(root, next);
    focusKeyboardTarget(root, next);
    const target = targetIdentity(next);
    const component = legend ? state.legends : state.data;
    emitHostEvent(root, 'cfxnavigate', { label: text(next), target, index: component.indexOf(next), count: component.length, key });
    emitSync(root, { action: 'navigate', label: text(next), target, index: component.indexOf(next), count: component.length, key });
    return true;
  };
