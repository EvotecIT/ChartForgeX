  // Generated sections carry the scene and matching semantic metadata as one adapter document.
  const readChartSection = (value, fresh = false) => {
    let section = value;
    if (typeof value === 'string') {
      const template = document.createElement('template'); template.innerHTML = value.trim();
      if (template.content.children.length !== 1) throw new TypeError('Expected one generated chart section without assets.');
      section = template.content.firstElementChild;
    }
    if (!(section instanceof Element) || !section.matches('section.cfx-interactive-chart'))
      throw new TypeError('Expected a generated interactive chart section.');
    if (fresh && section.dataset.cfxRuntimeBound === 'true') throw new TypeError('Update requires a fresh generated section.');
    if (section.querySelector('script') || section.querySelectorAll('.cfx-stage').length !== 1
      || section.querySelectorAll('.cfx-stage svg').length !== 1 || section.querySelectorAll('.cfx-tooltip').length !== 1)
      throw new TypeError('The generated chart section must contain its scene and readout, without scripts.');
    const svg = section.querySelector('.cfx-stage svg');
    const box = svg.getAttribute('viewBox')?.trim().split(/[\s,]+/).map(Number);
    let metadata;
    try { metadata = JSON.parse(section.dataset.cfxPreparedChart); } catch { throw new TypeError('The chart section requires prepared metadata.'); }
    if (!section.dataset.cfxChartId || svg.namespaceURI !== 'http://www.w3.org/2000/svg' || !box || box.length !== 4 || !box.every(Number.isFinite) || box[2] <= 0 || box[3] <= 0
      || !metadata || !Array.isArray(metadata.series) || !Array.isArray(metadata.regions) || !Array.isArray(metadata.xLabels)
      || metadata.series.some(item => !item || typeof item.name !== 'string' || typeof item.key !== 'string' || !item.key
        || typeof item.local !== 'boolean' || typeof item.kind !== 'string' || typeof item.state !== 'string'
        || !Array.isArray(item.indices) || item.indices.some(index => !Number.isSafeInteger(index) || index < 0))
      || new Set(metadata.series.map(item => item.key)).size !== metadata.series.length
      || metadata.xProjection !== undefined && !validXProjection(metadata.xProjection)
      || metadata.dataProjection !== undefined && !validDataProjection(metadata.dataProjection)
      || metadata.xLabels.some(item => !item || !Number.isFinite(item.value) || typeof item.text !== 'string')
      || metadata.categoryLabels !== undefined && (!Array.isArray(metadata.categoryLabels)
        || metadata.categoryLabels.some(item => !item || !Number.isFinite(item.value) || typeof item.text !== 'string'))
      || metadata.regions.some(item => !item || typeof item.id !== 'string' || typeof item.role !== 'string'
        || ![item.x, item.y, item.width, item.height].every(Number.isFinite) || item.width < 0 || item.height < 0
        || item.fillInteriors !== undefined && (!Array.isArray(item.fillInteriors)
          || item.fillInteriors.some(box => !Array.isArray(box) || box.length !== 3 || !box.every(Number.isFinite)))))
      throw new TypeError('The chart section has invalid prepared metadata or scene bounds.');
    if (Array.from(svg.querySelectorAll('[data-cfx-series]')).some(node => {
      const index = Number(node.dataset.cfxSeries);
      return !Number.isInteger(index) || index < 0 || index >= metadata.series.length;
    })) throw new TypeError('The chart scene does not match its prepared series metadata.');
    return section;
  };
  const durableChartTarget = target => !!target && target.seriesScope === 'shared' && !!target.seriesKey
    && (target.targetKind !== 'point' || target.observationId !== undefined || target.derived === 'five-number-summary' && target.groupId !== undefined)
    && (target.targetKind !== 'legend' || target.point === undefined || target.observationId !== undefined
      || target.derived === 'five-number-summary' && target.groupId !== undefined);
  const chartLegendStateTargets = (root, isolated) => {
    // A narrow native legend may omit an entry while its data owner remains painted and muted.
    // Capture that owner's identity too; presentation density must not erase reader state.
    const attribute = isolated ? 'cfxIsolated' : 'cfxMuted';
    const className = isolated ? 'cfx-series-isolated-in' : 'cfx-series-muted';
    const targets = interactiveTargets(root, '[data-cfx-series]')
      .filter(node => node.dataset[attribute] === 'true' || node.classList.contains(className)).map(seriesTarget);
    const wholeSeries = new Set(targets.filter(target => target.targetKind === 'series').map(target => target.seriesKey));
    const captured = new Set();
    return targets.filter(target => {
      if (target.targetKind !== 'series' && wholeSeries.has(target.seriesKey)) return false;
      const key = JSON.stringify([target.seriesKey, target.targetKind, target.targetId]);
      if (captured.has(key)) return false;
      captured.add(key); return true;
    });
  };
  const chartUpdateState = (root, sameSource = false) => {
    const active = root.getRootNode().activeElement;
    const focused = interactiveTargets(root).find(node => targetFocusNode(node) === active);
    return {
      selected: selectedTargets(root).filter(target => sameSource || durableChartTarget(target)),
      muted: chartLegendStateTargets(root, false).filter(target => sameSource || durableChartTarget(target)),
      isolated: chartLegendStateTargets(root, true).filter(target => sameSource || durableChartTarget(target)),
      focused: focused && (sameSource || durableChartTarget(targetIdentity(focused))) ? targetIdentity(focused) : null,
      highlight: captureHostHighlight(root, sameSource),
      ownedFocus: !!active && root.contains(active), viewport: getState(root)
    };
  };
  const installChartSection = (root, next) => {
    // Host attributes and non-adapter CSS properties belong to the embedding application.
    for (const attribute of Array.from(root.attributes)) if (attribute.name.startsWith('data-cfx-')) root.removeAttribute(attribute.name);
    for (const property of Array.from(root.style)) if (property.startsWith('--cfx-')) root.style.removeProperty(property);
    for (const attribute of Array.from(next.attributes)) {
      if (attribute.name.startsWith('data-cfx-') && attribute.name !== 'data-cfx-runtime-bound'
        || attribute.name === 'role' || attribute.name === 'aria-label') root.setAttribute(attribute.name, attribute.value);
    }
    for (const property of Array.from(next.style)) if (property.startsWith('--cfx-'))
      root.style.setProperty(property, next.style.getPropertyValue(property), next.style.getPropertyPriority(property));
    root.replaceChildren(...Array.from(next.childNodes).map(node => node.cloneNode(true)));
  };
  const restoreChartUpdateState = (root, state, nativeCommit = false) => {
    // Explicit identity survives position changes; anonymous ordinal facts never inherit full-replacement state.
    applySelectionSetByTargets(root, state.selected, true);
    for (const target of state.muted) { const resolved = resolveSeriesTarget(root, target); if (resolved) setSeriesMuted(root, resolved, true); }
    for (const target of state.isolated) { const resolved = resolveSeriesTarget(root, target); if (resolved) setSeriesIsolation(root, resolved, true); }
    applyHostHighlight(root, state.highlight);
    syncSelectionAppearance(root);
    applyViewport(root, nativeCommit ? { zoom: 1, panX: 0, panY: 0 } : state.viewport);
    renderCompare(root); refreshKeyboardNavigation(root); syncResetControl(root);
    const focused = state.focused && interactiveTargets(root).find(node => seriesKey(node) === state.focused.seriesKey && matchesTargetIdentity(node, state.focused));
    if (focused && keyboardTargetAvailable(focused)) focusKeyboardTarget(root, focused, pointerReadoutSuspended(root));
    else if (state.ownedFocus) { root.setAttribute('tabindex', '-1'); root.focus({ preventScroll: true }); }
    // Focus recovery must not resurrect a pinned or stale readout from the previous scene.
    hideTip(root, root.querySelector('.cfx-tooltip'), true); clearHover(root, false, false);
  };
