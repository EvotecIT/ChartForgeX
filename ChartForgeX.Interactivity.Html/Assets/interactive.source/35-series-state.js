  const seriesTarget = (node) => {
    const data = node.dataset || {};
    const reference = legendTarget(node) || (['node', 'link', 'region'].includes(data.cfxTargetKind)
      ? { targetKind: data.cfxTargetKind, targetId: data.cfxTargetId } : null);
    if (boxPlotGroup(node)) return { series: data.cfxSeries, point: data.cfxGroupIndex, seriesKey: seriesKey(node), seriesScope: data.cfxSeriesScope,
      groupIndex: Number(data.cfxGroupIndex), groupId: data.cfxGroupId, derived: 'five-number-summary',
      label: data.cfxLabel || seriesLabel(node), targetKind: 'point', targetId: boxPlotGroupTargetId(node) };
    return { series: data.cfxSeries, point: data.cfxPoint, seriesKey: seriesKey(node), seriesScope: data.cfxSeriesScope,
      observationId: data.cfxObservationId, label: data.cfxLabel || seriesLabel(node),
      targetKind: data.cfxPoint === undefined ? 'series' : 'point',
      targetId: data.cfxPoint === undefined ? seriesKey(node) : pointTargetId(node), ...reference };
  };
  const seriesTargetToken = (target) => !target ? '' : target.targetKind
    ? [target.series ?? '', target.targetKind, target.targetId].join(':') : [target.series ?? '', target.point ?? ''].join(':');
  const matchesLocalSeriesTarget = (node, target) => {
    if (!target) return false;
    const data = node.dataset || {};
    if (data.cfxSeries !== String(target.series)) return false;
    if (target.derived === 'five-number-summary' && target.groupIndex !== undefined)
      return boxPlotGroup(node) && (target.groupId !== undefined ? data.cfxGroupId === target.groupId : data.cfxGroupIndex === String(target.groupIndex));
    if (target.targetKind && target.targetKind !== 'series' && target.targetId) {
      if (target.targetKind === 'point') return data.cfxPoint !== undefined && pointTargetId(node) === target.targetId;
      const reference = legendTarget(node);
      return reference ? reference.targetKind === target.targetKind && reference.targetId === target.targetId
        : data.cfxTargetKind === target.targetKind && data.cfxTargetId === target.targetId;
    }
    return target.point === undefined || data.cfxPoint === String(target.point);
  };
  const resolveSeriesTarget = (root, target) => {
    if (!target || !target.seriesKey) return null;
    const legends = Array.from(root.querySelectorAll('[data-cfx-role="legend-item"][data-cfx-series]'));
    let matchingSeries = legends.filter((item) => seriesKey(item) === target.seriesKey);
    if (!matchingSeries.length) matchingSeries = Array.from(root.querySelectorAll('[data-cfx-series]')).filter((item) => seriesKey(item) === target.seriesKey);
    if (!matchingSeries.length) return null;
    if (['node', 'link', 'region'].includes(target.targetKind) && target.targetId) {
      const exact = matchingSeries.find((node) => {
        const reference = legendTarget(node);
        return reference && reference.targetKind === target.targetKind && reference.targetId === target.targetId;
      }) || referencedTargetNode(root, target);
      return exact ? seriesTarget(exact) : null;
    }
    if (target.point === undefined) {
      const localSeries = (matchingSeries[0].dataset || {}).cfxSeries;
      return localSeries === undefined ? null : { series: localSeries, seriesKey: target.seriesKey, label: target.label };
    }
    if (target.targetId) {
      const exact = matchingSeries.find((item) => (item.dataset || {}).cfxPoint !== undefined && pointTargetId(item) === target.targetId)
        || Array.from(root.querySelectorAll('[data-cfx-series][data-cfx-point]'))
          .find((item) => seriesKey(item) === target.seriesKey && pointTargetId(item) === target.targetId);
      return exact ? seriesTarget(exact) : null;
    }
    const exactLabel = matchingSeries.find((item) => (item.dataset || {}).cfxLabel === target.label);
    const exactPoint = matchingSeries.find((item) => (item.dataset || {}).cfxPoint === String(target.point));
    return seriesTarget(exactLabel || exactPoint || matchingSeries.find((item) => (item.dataset || {}).cfxPoint === undefined) || matchingSeries[0]);
  };
  const setSeriesMuted = (root, target, muted) => {
    root.querySelectorAll('[data-cfx-series]').forEach((node) => {
      if (!matchesLocalSeriesTarget(node, target)) return;
      const role = node.dataset ? node.dataset.cfxRole || '' : '';
      if (role.indexOf('legend') === 0) {
        node.dataset.cfxMuted = muted ? 'true' : 'false';
        return;
      }
      node.classList.toggle('cfx-series-muted', muted);
      pointLabelSurfaces(root, node).forEach(label => label.classList.toggle('cfx-series-muted', muted));
    });
    refreshKeyboardNavigation(root);
    syncSelectionAppearance(root);
    syncLegendOverflow(root);
    syncResetControl(root);
  };
  const setSeriesIsolation = (root, target, isolated) => {
    const item = target && target.targetKind ? referencedTargetNode(root, target) : null;
    root.querySelectorAll('[data-cfx-series]').forEach((node) => {
      const data = node.dataset || {};
      const role = data.cfxRole || '';
      const sameSeries = matchesLocalSeriesTarget(node, target);
      if (role.indexOf('legend') === 0) {
        if (isolated && sameSeries) {
          data.cfxIsolated = 'true';
          node.setAttribute('aria-current', 'true');
        } else {
          delete data.cfxIsolated;
          node.removeAttribute('aria-current');
        }
        return;
      }
      node.classList.toggle('cfx-series-isolated-in', isolated && sameSeries);
      // Preserve ancestor containers for a referenced item or an isolated point.
      const context = item && (node.contains(item) || item.contains(node));
      const pointContainer = target && target.point !== undefined && data.cfxSeries === String(target.series)
        && role === 'series' && data.cfxPoint === undefined;
      node.classList.toggle('cfx-series-isolated-out', isolated && !sameSeries && !context && !pointContainer);
      pointLabelSurfaces(root, node).forEach(label => {
        label.classList.toggle('cfx-series-isolated-in', isolated && sameSeries);
        label.classList.toggle('cfx-series-isolated-out', isolated && !sameSeries && !context && !pointContainer);
      });
    });
    if (isolated) root.dataset.cfxIsolatedSeries = seriesTargetToken(target);
    else root.removeAttribute('data-cfx-isolated-series');
    syncSelectionAppearance(root);
    syncLegendOverflow(root);
    syncResetControl(root);
  };
  const toggleSeriesFocus = (root, item, emit, sync) => {
    if (!hasFeature(root, 'LegendToggles')) return;
    const target = seriesTarget(item);
    if (target.series === undefined) return;
    const isolated = root.dataset.cfxIsolatedSeries !== seriesTargetToken(target);
    setSeriesIsolation(root, target, isolated);
    emitHostEvent(root, 'cfxseriesfocus', { ...target, isolated });
    if (sync !== false) emitSync(root, { action: 'series-focus', target, isolated });
  };
  const toggleSeries = (root, item) => {
    if (!hasFeature(root, 'LegendToggles')) return;
    const target = seriesTarget(item);
    if (target.series === undefined) return;
    if (root.dataset.cfxIsolatedSeries) setSeriesIsolation(root, null, false);
    const muted = item.dataset.cfxMuted !== 'true';
    setSeriesMuted(root, target, muted);
    emitHostEvent(root, 'cfxseries', { ...target, muted });
    emitSync(root, { action: 'series', target, muted });
  };
