  // Data and legend are separate roving components. Source identities stay on the actual rendered marks.
  const keyboardTargetAvailable = (node) => {
    if (node.closest('[aria-hidden="true"]') || !paintAncestorsVisible(node, new Map())) return false;
    // Muted data leaves navigation; its legend remains an entry point for restoring the series.
    if (renderedTargetKind(node) !== 'legend' && node.closest('.cfx-series-muted')) return false;
    const style = getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden' || style.visibility === 'collapse') return false;
    if (pointerTargetPaint(node)) return true;
    // Retained authored facts can have no filled geometry, while still belonging to the data component.
    const data = node.dataset;
    if (!data.cfxTargetKind || !data.cfxTargetId || !['zero', 'precision-collapse'].includes(data.cfxGeometryStatus)) return false;
    const svg = node.ownerSVGElement;
    if (!svg) return false;
    // The viewport detects hidden hosts; CSS-hidden inner groups need their own ancestor check.
    for (let parent = node.parentElement; parent && parent !== svg; parent = parent.parentElement) {
      if (getComputedStyle(parent).display === 'none') return false;
    }
    const viewport = svg.getBoundingClientRect();
    return viewport.width > 0 && viewport.height > 0;
  };
  const keyboardTargets = (root) => {
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
  };
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
  const refreshKeyboardNavigation = (root, focused) => {
    const state = root._cfxKeyboardNavigation;
    if (!state || !hasFeature(root, 'KeyboardNavigation')) return null;
    const activeElement = root.getRootNode().activeElement;
    const activeOwned = root.contains(activeElement) && state.owned.has(activeElement);
    const targets = keyboardTargets(root);
    state.legends = targets.filter((node) => renderedTargetKind(node) === 'legend');
    state.groups = keyboardDataGroups(targets.filter((node) => renderedTargetKind(node) !== 'legend'));
    state.data = state.groups.flatMap((group) => group.nodes);
    // A hidden host has no available targets. Retain each component's position until layout returns.
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
      if (replacement) focusKeyboardTarget(root, replacement);
    }
    return state;
  };
  const bindKeyboardNavigationAvailability = (root) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage) return;
    let frame = 0;
    let resizeObserver;
    let paintObserver;
    const queueRefresh = () => {
      if (frame) cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        frame = 0;
        if (!root.isConnected) {
          if (resizeObserver) resizeObserver.disconnect();
          if (paintObserver) paintObserver.disconnect();
          window.removeEventListener('resize', queueRefresh);
          return;
        }
        refreshKeyboardNavigation(root);
      });
    };
    window.addEventListener('resize', queueRefresh);
    if (typeof ResizeObserver !== 'undefined') {
      // Tabs and other initially hidden hosts acquire layout without a window resize.
      resizeObserver = new ResizeObserver(queueRefresh);
      resizeObserver.observe(stage);
    }
    // Native paint and host styles can change without a resize. Tab and arrow availability share this owner.
    paintObserver = new MutationObserver(() => {
      if (!root.isConnected) { queueRefresh(); return; }
      refreshKeyboardNavigation(root);
    });
    paintObserver.observe(root, { subtree: true, attributes: true,
      attributeFilter: ['class', 'style', 'hidden', 'aria-hidden', 'fill', 'stroke', 'stroke-width', 'opacity', 'fill-opacity', 'stroke-opacity', 'clip-path'] });
  };
  const prepareKeyboardNavigation = (root) => {
    if (!hasFeature(root, 'KeyboardNavigation')) return;
    root._cfxKeyboardNavigation = { owned: new Set() };
    // SVG focus listeners can make aggregate groups implicitly tabbable. Only roving leaves enter the tab order.
    interactiveTargets(root).forEach((node) => root._cfxKeyboardNavigation.owned.add(targetFocusNode(node)));
    refreshKeyboardNavigation(root);
    bindKeyboardNavigationAvailability(root);
  };
  const scrollKeyboardTargetIntoView = (root, node) => {
    const stage = root.querySelector('.cfx-stage');
    if (!stage || !stage.contains(node)) return;
    const style = getComputedStyle(stage);
    const stageBox = stage.getBoundingClientRect();
    const box = node.getBoundingClientRect();
    const scrollAxis = (overflow, extent, available, offset, start, end, itemStart, itemEnd) => {
      if (!['auto', 'scroll'].includes(overflow) || extent <= available) return offset;
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
    const left = stageBox.left + stage.clientLeft + 8;
    const top = stageBox.top + stage.clientTop + 8;
    stage.scrollLeft = scrollAxis(style.overflowX, stage.scrollWidth, stage.clientWidth, stage.scrollLeft, left, left + stage.clientWidth - 16, box.left, box.right);
    stage.scrollTop = scrollAxis(style.overflowY, stage.scrollHeight, stage.clientHeight, stage.scrollTop, top, top + stage.clientHeight - 16, box.top, box.bottom);
  };
  const focusKeyboardTarget = (root, node) => {
    const focusNode = targetFocusNode(node);
    scrollKeyboardTargetIntoView(root, focusNode);
    if (focusNode.focus) {
      try { focusNode.focus({ preventScroll: true }); } catch { focusNode.focus(); }
    }
  };
  const focusAdjacentTarget = (root, node, key) => {
    if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(key)) return false;
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
    if (next === node) return true;
    refreshKeyboardNavigation(root, next);
    focusKeyboardTarget(root, next);
    const target = targetIdentity(next);
    const component = legend ? state.legends : state.data;
    emitHostEvent(root, 'cfxnavigate', { label: text(next), target, index: component.indexOf(next), count: component.length, key });
    emitSync(root, { action: 'navigate', label: text(next), target, index: component.indexOf(next), count: component.length, key });
    return true;
  };
