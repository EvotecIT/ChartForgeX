  // Native links, annotations and scalar series are selectable facts too. A series wrapper
  // has no value of its own; its separately identified children own the observations.
  const isDataTarget = node => isInteractiveTarget(node) && renderedTargetKind(node) !== 'legend'
    && (renderedTargetKind(node) !== 'series' || node.dataset.cfxValue !== undefined);
  const dataTargets = root => interactiveTargets(root).filter(isDataTarget);
  const targetCategory = node => {
    const data = node.dataset || {};
    return data.cfxXLabel ?? data.cfxCategoryLabel ?? data.cfxCategory
      ?? (['pie', 'donut', 'pyramid', 'funnel'].includes((data.cfxKind || '').toLowerCase()) ? data.cfxLabel : undefined);
  };
  const dataValue = (value, time) => {
    if (!tooltipNumber(value)) return undefined;
    const number = Number(value);
    if (!time) return number;
    const instant = new Date((number - 25569) * 86400000);
    return Number.isFinite(instant.valueOf()) ? instant.toISOString() : undefined;
  };
  const timeTarget = node => {
    const projection = node.closest('.cfx-interactive-chart')?._cfxDataProjection;
    return node.dataset.cfxXScale === 'time' || (projection?.horizontal ? projection.y : projection?.x)?.scale === 'time';
  };
  const targetRawX = node => node.dataset.cfxXInstant ?? dataValue(node.dataset.cfxX, timeTarget(node));
  const targetDataRange = node => {
    const data = node.dataset;
    const lower = data.cfxRangeStart;
    const upper = data.cfxRangeEnd;
    if (lower === undefined || upper === undefined) return undefined;
    const time = data.cfxRangeScale === 'time';
    const from = data.cfxRangeStartInstant ?? dataValue(lower, time);
    let to = data.cfxRangeEndInstant ?? dataValue(upper, time);
    if (data.cfxRangeEndExclusive === 'true' && to !== undefined) {
      if (time) {
        const fraction = String(to).match(/\.(\d+)Z$/)?.[1] || '';
        const submillisecond = /[1-9]/.test(fraction.slice(3));
        to = new Date(Date.parse(to) - (submillisecond ? 0 : 1)).toISOString();
      }
    }
    return from === undefined || to === undefined ? undefined : [from, to];
  };
  const contributorObservationIds = node => {
    const sources = sourcePointCollection(node);
    if (!sources) return undefined;
    const ids = node.closest('.cfx-interactive-chart')?._cfxObservationIds?.[Number(node.dataset.cfxSeries)];
    const authored = sources.map(index => ids?.get(index)).filter(id => id !== undefined);
    return authored.length ? authored : undefined;
  };
  const syncSelectionAppearance = root => {
    const nodes = dataTargets(root), selected = nodes.some(node => node.classList.contains('cfx-selected'));
    root.toggleAttribute('data-cfx-has-selection', selected);
    if (selected) root.dataset.cfxHasSelection = 'true';
    nodes.forEach(node => {
      // Selection and external highlights are independent foregrounds. Their combined dim policy
      // keeps either active mark at full strength instead of letting the two layers dim each other.
      const active = node.classList.contains('cfx-selected') || node.classList.contains('cfx-highlighted');
      node.classList.toggle('cfx-selection-dim', selected && root.dataset.cfxDimOthersSelected === 'true' && !active);
      node.classList.toggle('cfx-linked-dim', root._cfxHostHighlight?.dim === true && !active);
    });
  };
  const selectionSignature = root => selectedTargets(root).map(targetKey).join('\n');
  const publishSelectionChange = (root, reason, sync = false, previousTargets) => {
    syncSelectionAppearance(root);
    const targets = selectedTargets(root);
    emitHostEvent(root, 'cfxselectionchange', { reason, targets });
    if (sync) emitSync(root, { action: 'compare', count: targets.length, targets, previousTargets });
    return targets;
  };
  const copyHostValue = value => value == null ? value : JSON.parse(JSON.stringify(value));
  const hostMatch = match => {
    if (!match || typeof match !== 'object' || Array.isArray(match)) throw new TypeError('Expected a chart target match.');
    const candidate = copyHostValue(match);
    for (const key of ['targets', 'observationIds', 'categories', 'seriesKeys', 'sourcePoints', 'ranges'])
      if (candidate[key] !== undefined && !Array.isArray(candidate[key])) throw new TypeError('Chart match ' + key + ' must be an array.');
    return candidate;
  };
  const intervalNumber = value => typeof value === 'number' && Number.isFinite(value) ? value
    : typeof value === 'string' && /(?:Z|[+-]\d\d:\d\d)$/.test(value) ? Date.parse(value) : NaN;
  const overlapsRange = (target, range) => {
    if (!Array.isArray(range) || range.length !== 2) return false;
    const from = intervalNumber(range[0]), to = intervalNumber(range[1]);
    const values = target.range || (target.x !== undefined ? [target.x, target.x] : null);
    if (!values || !Number.isFinite(from) || !Number.isFinite(to)) return false;
    if (typeof range[0] !== typeof values[0] || typeof range[1] !== typeof values[1] || typeof range[0] !== typeof range[1]) return false;
    const lower = intervalNumber(values[0]), upper = intervalNumber(values[1]);
    return Number.isFinite(lower) && Number.isFinite(upper) && Math.min(lower, upper) <= Math.max(from, to) && Math.max(lower, upper) >= Math.min(from, to);
  };
  const matchesHostTarget = (node, match) => {
    const target = targetIdentity(node);
    const scoped = !match.seriesKey || match.seriesKey === target.seriesKey;
    return (match.targets || []).some(item => item && (!item.seriesKey || item.seriesKey === target.seriesKey) && matchesTargetIdentity(node, item))
      || scoped && (match.observationIds || []).some(id => target.observationId === id || target.observationIds?.includes(id))
      || (match.categories || []).includes(target.category)
      || (match.seriesKeys || []).includes(target.seriesKey)
      || scoped && (match.sourcePoints || []).some(item => item && item.seriesKey === target.seriesKey
        && (Number(target.sourcePoint) === item.index || target.sourcePoints?.includes(item.index)))
      || scoped && (match.ranges || []).some(range => overlapsRange(target, range));
  };
  const runSelectionCommand = (root, match, options = {}) => {
    if (!hasFeature(root, 'Selection')) return [];
    const normalized = hostMatch(match), mode = options.mode || 'replace';
    if (!['replace', 'add', 'remove'].includes(mode)) throw new TypeError('Selection mode must be replace, add or remove.');
    const previousTargets = selectedTargets(root);
    const nodes = dataTargets(root), matching = new Set(nodes.filter(node => matchesHostTarget(node, normalized)));
    const single = root.dataset.cfxSelectionMode === 'single', first = matching.values().next().value;
    const changed = [];
    nodes.forEach(node => {
      const before = node.classList.contains('cfx-selected'), matches = matching.has(node);
      const selected = mode === 'replace' ? matches && (!single || node === first)
        : mode === 'add' && single && first ? node === first : matches ? mode === 'add' : before;
      if (selected === before) return;
      setNodeSelected(node, selected); changed.push({ node, selected });
    });
    syncSelectionAppearance(root); renderCompare(root); syncResetControl(root);
    if (changed.length && options.emit === true) {
      changed.forEach(({ node, selected }) => emitHostEvent(root, 'cfxselect', { label: text(node), selected, target: targetIdentity(node) }));
      publishSelectionChange(root, 'command'); publishCompare(root, false);
    }
    const targets = selectedTargets(root);
    if (changed.length && options.sync === true) emitSync(root, { action: 'compare', count: targets.length, targets,
      previousTargets: mode === 'replace' ? undefined : previousTargets });
    return targets;
  };
  const clearSelectionCommand = (root, options = {}) => runSelectionCommand(root, { targets: [] }, { ...options, mode: 'replace' });
  const captureHostHighlight = (root, sameSource = false) => {
    const state = copyHostValue(root._cfxHostHighlight || null);
    if (!state || sameSource) return state;
    const match = state.match, nodes = dataTargets(root);
    const sharedKeys = new Set(JSON.parse(root.dataset.cfxPreparedChart).series.filter(series => !series.local).map(series => series.key));
    let removed = false;
    const retain = (key, predicate) => {
      if (!match[key]) return;
      const original = match[key]; match[key] = original.filter(predicate);
      removed ||= match[key].length !== original.length;
    };
    retain('seriesKeys', key => sharedKeys.has(key));
    if (match.targets) {
      const original = match.targets;
      match.targets = original.flatMap(item => nodes.filter(node => item && (!item.seriesKey || item.seriesKey === seriesKey(node))
        && matchesTargetIdentity(node, item)).map(targetIdentity).filter(durableChartTarget)
        .map(target => ({ targetKind: target.targetKind, targetId: target.targetId, seriesKey: target.seriesKey })));
      removed ||= original.some(item => !nodes.some(node => item && (!item.seriesKey || item.seriesKey === seriesKey(node))
        && matchesTargetIdentity(node, item) && durableChartTarget(targetIdentity(node))));
    }
    // Ordinals describe the previous source order; explicit IDs and value filters describe durable intent.
    if (match.sourcePoints?.length) { match.sourcePoints = []; removed = true; }
    if (match.seriesKey && !sharedKeys.has(match.seriesKey)) {
      for (const key of ['observationIds', 'ranges']) if (match[key]?.length) { match[key] = []; removed = true; }
      delete match.seriesKey;
    }
    return removed && !['targets', 'observationIds', 'categories', 'seriesKeys', 'ranges'].some(key => match[key]?.length) ? null : state;
  };
  const applyHostHighlight = (root, state) => {
    root._cfxHostHighlight = state ? copyHostValue(state) : null;
    const nodes = dataTargets(root), highlighted = state ? nodes.filter(node => matchesHostTarget(node, state.match)) : [];
    const active = new Set(highlighted);
    nodes.forEach(node => {
      const match = active.has(node);
      node.classList.toggle('cfx-highlighted', match);
      node.toggleAttribute('data-cfx-highlighted', match);
      if (match) node.dataset.cfxHighlighted = 'true';
    });
    syncSelectionAppearance(root);
    const region = root.querySelector('[data-cfx-live]');
    if (region) region.textContent = state ? highlighted.length + ' marks highlighted' : 'Highlight cleared';
    return highlighted.map(targetIdentity);
  };
  const runHighlightCommand = (root, match, options = {}) => {
    if (!hasFeature(root, 'Selection')) return [];
    const state = { match: hostMatch(match), dim: options.dim === true, source: options.source || '' };
    const targets = applyHostHighlight(root, state);
    if (options.emit === true) emitHostEvent(root, 'cfxhighlight', { count: targets.length, targets, source: state.source });
    if (options.sync === true) emitSync(root, { action: 'highlight', state });
    return targets;
  };
  const clearHighlightCommand = (root, options = {}) => {
    if (!hasFeature(root, 'Selection')) return [];
    applyHostHighlight(root, null);
    if (options.emit === true) emitHostEvent(root, 'cfxhighlightclear', {});
    if (options.sync === true) emitSync(root, { action: 'highlight', state: null });
    return [];
  };
  const bindHostCommands = root => {
    if (!root.querySelector('[data-cfx-live]')) {
      const region = document.createElement('span');
      region.setAttribute('data-cfx-live', 'true'); region.setAttribute('aria-live', 'polite');
      region.className = 'cfx-visually-hidden'; root.appendChild(region);
    }
    listenChart(root, root, 'cfx-unpin-tooltip', () => unpinTooltip(root));
    if (!hasFeature(root, 'Selection')) return;
    for (const [name, execute] of [['cfx-select', runSelectionCommand], ['cfx-highlight', runHighlightCommand]])
      listenChart(root, root, name, event => { const detail = event.detail || {}; execute(root, detail.match, detail); });
    listenChart(root, root, 'cfx-clear-selection', event => clearSelectionCommand(root, event.detail || {}));
    listenChart(root, root, 'cfx-clear-highlight', event => clearHighlightCommand(root, event.detail || {}));
  };
