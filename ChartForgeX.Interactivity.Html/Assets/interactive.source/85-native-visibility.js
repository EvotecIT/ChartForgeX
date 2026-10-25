  const validNativeVisibilityMetadata = metadata => {
    const view = metadata?.visibility, series = metadata?.series;
    if (view?.layout !== undefined && view.layout !== 'reflow') return false;
    if (view?.layout === 'reflow') return validNativeReflowMetadata(metadata);
    return !!view && Array.isArray(series) && series.length > 0 && series.every(row => !row.local && ['line', 'bar'].includes(row.kind))
      && ['preserveOverlay', 'groupWithSeries'].includes(view.labelLayering) && Array.isArray(view.hiddenSeries)
      && view.hiddenSeries.every(key => typeof key === 'string' && series.some(row => row.key === key))
      && new Set(view.hiddenSeries).size === view.hiddenSeries.length;
  };
  const nativeTargetToken = target => JSON.stringify([target.kind, target.seriesKey, target.nodeId ?? target.observationId ?? null]);
  const validNativeTarget = target => !!target && ['series', 'observation', 'boxPlotGroup', 'radialOther', 'hierarchyItem'].includes(target.kind)
    && typeof target.seriesKey === 'string' && !!target.seriesKey
    && (target.kind === 'hierarchyItem' ? typeof target.nodeId === 'string' && !!target.nodeId && target.observationId === undefined
      : target.nodeId === undefined && (['series', 'radialOther'].includes(target.kind) ? target.observationId === undefined : typeof target.observationId === 'string' && !!target.observationId))
    && Object.keys(target).every(key => ['kind', 'seriesKey', 'observationId', 'nodeId'].includes(key));
  const sameNativeTargets = (left, right) => {
    if (!Array.isArray(left) || !Array.isArray(right) || left.length !== right.length || !left.every(validNativeTarget) || !right.every(validNativeTarget)) return false;
    const a = new Set(left.map(nativeTargetToken)), b = new Set(right.map(nativeTargetToken));
    return a.size === left.length && b.size === right.length && [...a].every(token => b.has(token));
  };
  const validNativeReflowMetadata = metadata => {
    const view = metadata?.visibility, series = metadata?.series, catalog = view?.targetCatalog;
    const catalogTokens = Array.isArray(catalog) && catalog.every(validNativeTarget) ? new Set(catalog.map(nativeTargetToken)) : new Set();
    if (!view || !Array.isArray(series) || !series.length || series.some(row => row.local || !['line', 'bar', 'boxplot', 'pie', 'donut', 'treemap', 'sunburst'].includes(row.kind))
        || !['preserveOverlay', 'groupWithSeries'].includes(view.labelLayering) || !/^[a-f0-9]{64}$/.test(view.sourceKey)
        || !Array.isArray(catalog) || !catalog.length || !catalog.every(validNativeTarget)
        || new Set(catalog.map(nativeTargetToken)).size !== catalog.length || catalog.some(target => !series.some(row => row.key === target.seriesKey))
        || !Array.isArray(view.hiddenTargets) || !sameNativeTargets(view.hiddenTargets, view.hiddenTargets)
        || view.hiddenTargets.some(target => !catalogTokens.has(nativeTargetToken(target)))) return false;
    const hierarchy = series.length === 1 && ['treemap', 'sunburst'].includes(series[0].kind);
    if (hierarchy ? view.labelLayering !== 'preserveOverlay' || metadata.hierarchy?.kind !== series[0].kind || metadata.hierarchy.seriesKey !== series[0].key
        || !Object.hasOwn(view, 'hierarchyRootId') || view.hierarchyRootId !== metadata.hierarchy.rootId
        || view.hierarchyRootId !== null && !catalog.some(target => target.kind === 'hierarchyItem' && target.nodeId === view.hierarchyRootId)
        || catalog.some(target => !['series', 'hierarchyItem'].includes(target.kind))
      : view.hierarchyRootId !== undefined || catalog.some(target => target.kind === 'hierarchyItem')) return false;
    const seriesTargets = catalog.filter(target => target.kind === 'series');
    return seriesTargets.length === series.length && seriesTargets.every((target, index) => target.seriesKey === series[index].key)
      && sameNativeHiddenSeries(view.hiddenSeries, view.hiddenTargets.filter(target => target.kind === 'series').map(target => target.seriesKey), series.map(row => row.key));
  };
  const validateRequestedNativeReflow = (source, target, expected, sourceReflow = false) => {
    if (source?.layout !== 'reflow' || target?.layout !== 'reflow' || expected?.layout !== 'reflow'
        || !sourceReflow && source.sourceKey !== target.sourceKey || JSON.stringify(source.targetCatalog) !== JSON.stringify(target.targetCatalog)
        || source.labelLayering !== target.labelLayering || expected.labelLayering !== target.labelLayering
        || !sameNativeTargets(expected.hiddenTargets, target.hiddenTargets) || expected.hierarchyRootId !== target.hierarchyRootId)
      throw new TypeError('Native reply: exact detached Reflow source/catalog/layout/absolute targets required.');
  };
  const validateNativeReflowTransition = (facts, first, last, sourceReflow = false) => {
    if (facts?.layout !== 'reflow' || first?.layout !== 'reflow' || last?.layout !== 'reflow' || !sourceReflow && first.sourceKey !== last.sourceKey
        || JSON.stringify(first.targetCatalog) !== JSON.stringify(last.targetCatalog) || JSON.stringify(facts.targetCatalog) !== JSON.stringify(first.targetCatalog)
        || facts.labelLayering !== first.labelLayering || facts.labelLayering !== last.labelLayering
        || !sameNativeTargets(facts.fromHiddenTargets, first.hiddenTargets) || !sameNativeTargets(facts.toHiddenTargets, last.hiddenTargets)
        || facts.fromHierarchyRootId !== first.hierarchyRootId || facts.toHierarchyRootId !== last.hierarchyRootId
        || sameNativeTargets(first.hiddenTargets, last.hiddenTargets) && first.hierarchyRootId === last.hierarchyRootId)
      throw new TypeError('Native transition: exact changed absolute Reflow targets and captured full source required.');
  };
  const nativeReflowTarget = (view, item) => {
    const data = item.dataset || {}, key = seriesKey(item);
    const kind = data.cfxLegendTargetKind === 'node' ? 'hierarchyItem' : data.cfxDerived === 'radial-slice' && data.cfxPoint === '-1' ? 'radialOther' : data.cfxGroupId !== undefined ? 'boxPlotGroup' : data.cfxObservationId !== undefined ? 'observation' : 'series';
    const id = kind === 'hierarchyItem' ? data.cfxLegendTargetId : kind === 'boxPlotGroup' ? data.cfxGroupId : kind === 'observation' ? data.cfxObservationId : undefined;
    return view.targetIndex.get(nativeTargetToken({ kind, seriesKey: key, ...(id === undefined ? {} : kind === 'hierarchyItem' ? { nodeId: id } : { observationId: id }) })) || null;
  };
  const requestNativeReflowVisibility = (root, item, view, binding) => {
    const target = nativeReflowTarget(view, item);
    if (!binding?.session || !target || nativePreparationBackpressureBusy(root)
        || item.dataset.cfxTargetBlocked === 'true' || target.kind !== 'series' && view.hiddenSeries.includes(target.seriesKey)) return false;
    const token = nativeTargetToken(target), hidden = new Set(view.hiddenTargets.map(nativeTargetToken));
    if (hidden.has(token)) hidden.delete(token); else hidden.add(token);
    const hiddenTargets = Object.freeze(view.targetCatalog.filter(row => hidden.has(nativeTargetToken(row))));
    return requestNativeVisibilityView(root, Object.freeze({ layout: 'reflow', hiddenTargets,
      ...(view.hierarchyRootId === undefined ? {} : { hierarchyRootId: view.hierarchyRootId }),
      hiddenSeries: Object.freeze(hiddenTargets.filter(row => row.kind === 'series').map(row => row.seriesKey)), labelLayering: view.labelLayering }), 'visibility');
  };
  const capturedNativeVisibility = root => {
    let metadata; try { metadata = JSON.parse(root.dataset.cfxPreparedChart); } catch { return null; }
    if (!validNativeVisibilityMetadata(metadata)) return null;
    const common = { hiddenSeries: Object.freeze([...metadata.visibility.hiddenSeries]), labelLayering: metadata.visibility.labelLayering,
      seriesKeys: Object.freeze(metadata.series.map(row => row.key)) };
    if (metadata.visibility.layout === 'reflow') Object.assign(common, { layout: 'reflow', sourceKey: metadata.visibility.sourceKey,
      ...(metadata.visibility.hierarchyRootId === undefined ? {} : { hierarchyRootId: metadata.visibility.hierarchyRootId }),
      targetCatalog: Object.freeze(metadata.visibility.targetCatalog.map(row => Object.freeze({ ...row }))),
      hiddenTargets: Object.freeze(metadata.visibility.hiddenTargets.map(row => Object.freeze({ ...row }))),
      targetIndex: new Map(metadata.visibility.targetCatalog.map(row => [nativeTargetToken(row), Object.freeze({ ...row })])),
      hiddenIndex: new Set(metadata.visibility.hiddenTargets.map(nativeTargetToken)) });
    return Object.freeze(common);
  };
  const nativeLegendConnected = root => !!nativePreparationBinding(root, 'legend')?.session;
  const nativeSeriesVisibilityChanged = root => nativeLegendConnected(root) && !!(capturedNativeVisibility(root)?.hiddenTargets?.length || capturedNativeVisibility(root)?.hiddenSeries.length || capturedNativeVisibility(root)?.hierarchyRootId != null);
  const nativeVisibilityResetDescriptor = root => {
    const view = nativeLegendConnected(root) && capturedNativeVisibility(root);
    return view ? Object.freeze({ hiddenSeries: Object.freeze([]), labelLayering: view.labelLayering,
      ...(view.layout === 'reflow' ? { layout: 'reflow', hiddenTargets: Object.freeze([]), ...(view.hierarchyRootId === undefined ? {} : { hierarchyRootId: null }) } : {}) }) : null;
  };
  const sameNativeHiddenSeries = (left, right, catalog) => Array.isArray(left) && Array.isArray(right)
    && left.length === right.length && catalog.every(key => left.includes(key) === right.includes(key));
  const validateRequestedNativeVisibility = (root, drawing, request, before = root, sourceReflow = false) => {
    if (!nativeLegendConnected(root)) return;
    const source = capturedNativeVisibility(before), target = capturedNativeVisibility(drawing), expected = request.sourceVisibility || source;
    if (source?.layout === 'reflow' || target?.layout === 'reflow') {
      validateRequestedNativeReflow(source, target, expected, sourceReflow); return;
    }
    if (!source || !target || JSON.stringify(source.seriesKeys) !== JSON.stringify(target.seriesKeys)
        || expected.labelLayering !== target.labelLayering || !sameNativeHiddenSeries(expected.hiddenSeries, target.hiddenSeries, source.seriesKeys))
      throw new TypeError('Native reply: captured visibility catalog/policy/view changed.');
  };
  const validateNativeVisibilityTransition = (facts, before, after, sourceReflow = false) => {
    const first = capturedNativeVisibility(before), last = capturedNativeVisibility(after);
    if (first?.layout === 'reflow' || last?.layout === 'reflow') { validateNativeReflowTransition(facts, first, last, sourceReflow); return; }
    if (!facts || !first || !last || !Array.isArray(facts.seriesKeys) || !Array.isArray(facts.fromHidden) || !Array.isArray(facts.toHidden)
        || JSON.stringify(first.seriesKeys) !== JSON.stringify(facts.seriesKeys) || JSON.stringify(last.seriesKeys) !== JSON.stringify(facts.seriesKeys)
        || facts.labelLayering !== first.labelLayering || facts.labelLayering !== last.labelLayering
        || !sameNativeHiddenSeries(first.hiddenSeries, facts.fromHidden, first.seriesKeys) || !sameNativeHiddenSeries(last.hiddenSeries, facts.toHidden, first.seriesKeys)
        || facts.fromHidden.some(key => !first.seriesKeys.includes(key)) || facts.toHidden.some(key => !first.seriesKeys.includes(key))
        || new Set(facts.fromHidden).size !== facts.fromHidden.length || new Set(facts.toHidden).size !== facts.toHidden.length)
      throw new TypeError('Native transition: exact absolute series visibility facts required.');
  };
  const syncNativeSeriesVisibility = root => {
    const lifetime = chartLifetimes.get(root);
    // Teardown can reenter through queued admission after the held legend lease released.
    if (!lifetime || lifetime.disposed || !root.isConnected) return;
    const connected = nativeLegendConnected(root), view = connected && capturedNativeVisibility(root);
    if (!view) { lifetime?.nativeLegendRelease?.(); syncLegendOverflow(root); return; }
    if (!lifetime.nativeLegendRelease) {
      const held = new Map(); lifetime.nativeLegendHeld = held;
      const release = ownChartWork(root, () => {
        for (const [node, facts] of held) for (const [name, value] of Object.entries(facts)) {
          if (value === null) node.removeAttribute(name); else node.setAttribute(name, value);
        }
        held.clear(); if (lifetime.nativeLegendRelease === release) { lifetime.nativeLegendRelease = null; lifetime.nativeLegendHeld = null; }
      });
      lifetime.nativeLegendRelease = release;
    }
    for (const node of interactiveTargets(root, '[data-cfx-role="legend-item"]')) {
      const target = view.layout === 'reflow' ? nativeReflowTarget(view, node) : null;
      if (view.layout === 'reflow' ? !target : !view.seriesKeys.includes(seriesKey(node)) || seriesTarget(node).targetKind !== 'series') continue;
      if (!lifetime.nativeLegendHeld.has(node)) lifetime.nativeLegendHeld.set(node,
        Object.fromEntries(['aria-pressed', 'aria-disabled', 'data-cfx-native-legend'].map(name => [name, node.getAttribute(name)])));
      node.setAttribute('aria-pressed', String(!(node.dataset.cfxTargetHidden === 'true' || view.hiddenSeries.includes(seriesKey(node)) || target && view.hiddenIndex.has(nativeTargetToken(target)))));
      node.setAttribute('aria-disabled', String(!!(nativePreparationBackpressureBusy(root) || node.dataset.cfxTargetBlocked === 'true' || target && target.kind !== 'series' && view.hiddenSeries.includes(target.seriesKey)))); node.setAttribute('data-cfx-native-legend', 'true');
    }
    syncLegendOverflow(root);
  };
  const requestNativeSeriesVisibility = (root, item) => {
    const view = capturedNativeVisibility(root), binding = nativePreparationBinding(root, 'legend'), target = seriesTarget(item);
    if (view?.layout === 'reflow') return requestNativeReflowVisibility(root, item, view, binding);
    if (!binding?.session || !view || target.targetKind !== 'series' || !view.seriesKeys.includes(target.seriesKey)
        || nativePreparationBackpressureBusy(root)) return false;
    const hidden = new Set(view.hiddenSeries); if (hidden.has(target.seriesKey)) hidden.delete(target.seriesKey); else hidden.add(target.seriesKey);
    return requestNativeVisibilityView(root, Object.freeze({ hiddenSeries: Object.freeze(view.seriesKeys.filter(key => hidden.has(key))), labelLayering: view.labelLayering }), 'visibility');
  };
  const requestNativeVisibilityView = (root, sourceVisibility, kind) => {
    if (!nativeLegendConnected(root)) return false;
    return requestNativePreparation(root, 'legend', { kind, sourceVisibility }, {
      eventPrefix: 'cfxseries', resetView: kind === 'reset', preserveViewport: true,
      validate: drawing => { if (!capturedNativeVisibility(drawing)) throw new TypeError('Native reply: retained visibility view required.'); },
      beforeCommit: () => kind === 'reset' ? selectionSignature(root) : '',
      committed: (_, previousSelection) => { if (kind === 'reset') { emitHostEvent(root, 'cfxreset', {}); publishResetSelection(root, previousSelection); } }
    });
  };
  const requestNativeVisibilityReset = root => {
    const view = nativeVisibilityResetDescriptor(root); return view ? requestNativeVisibilityView(root, view, 'reset') : false;
  };

  // The shared all-candidate validator binds hierarchy catalogs/roots to actual retained native node metadata, including hidden nodes.
  const validateNativeHierarchyVisibilitySection = (section, metadata) => {
    if (metadata.visibility?.layout !== 'reflow' || metadata.visibility.hierarchyRootId === undefined) return;
    const catalog = metadata.visibility.targetCatalog.filter(target => target.kind === 'hierarchyItem');
    const nodes = [...section.querySelectorAll('.cfx-stage [data-cfx-role="hierarchy-series"] [data-cfx-node]')];
    const ids = new Set(nodes.map(node => node.dataset.cfxNode));
    if (ids.size !== nodes.length || nodes.length !== catalog.length || nodes.some(node => node.dataset.cfxSeriesKey !== metadata.hierarchy.seriesKey)
        || catalog.some(target => !ids.has(target.nodeId))) throw new TypeError('Section: captured hierarchy catalog/node identity mismatch.');
    for (const node of nodes) {
      const root = metadata.visibility.hierarchyRootId;
      if (root === null ? node.dataset.cfxViewRoot !== undefined : node.dataset.cfxViewRoot !== root)
        throw new TypeError('Section: hierarchy root/native metadata mismatch.');
    }
  };
