  const tooltipHierarchy = node => {
    const data = node.dataset || {}, root = node.closest?.('.cfx-interactive-chart');
    if (!data.cfxNode || !['treemap', 'sunburst', 'icicle', 'circlepacking'].includes((data.cfxKind || '').toLowerCase())) return null;
    const candidates = Array.from(root?.querySelectorAll('[data-cfx-node]') || [])
      .filter(candidate => candidate.dataset.cfxSeriesKey === data.cfxSeriesKey
        && !candidate.closest('[data-cfx-role="legend-item"],[data-cfx-role="legend-native"],[data-cfx-role="legend-proxy"]'));
    // Drill overlays retain hidden source groups beside their browser clones.
    // Each authored identity contributes once to the source total.
    const byId = new Map(candidates.map(candidate => [candidate.dataset.cfxNode, candidate]));
    const nodes = Array.from(byId.values());
    const path = [], seen = new Set(); let current = node;
    while (current && !seen.has(current.dataset.cfxNode)) {
      seen.add(current.dataset.cfxNode); path.unshift(current.dataset.cfxLabel || current.dataset.cfxNode);
      current = byId.get(current.dataset.cfxParent);
    }
    const value = Number(data.cfxValue), parent = byId.get(data.cfxParent);
    const roots = nodes.filter(candidate => !candidate.dataset.cfxParent);
    const total = roots.reduce((sum, candidate) => sum + (Number(candidate.dataset.cfxValue) || 0), 0);
    const rows = [];
    if (parent && Number(parent.dataset.cfxValue) > 0 && Number.isFinite(value))
      rows.push({ name: '% of parent', value: percentText(value / Number(parent.dataset.cfxValue)) });
    if (total > 0 && Number.isFinite(value)) rows.push({ name: '% of total', value: percentText(value / total) });
    return { path: path.join(' › '), rows };
  };
  const tooltipFlowFacts = node => {
    const data = node.dataset || {}, root = node.closest?.('.cfx-interactive-chart');
    if (!/sankey|chord/.test((data.cfxKind || '').toLowerCase())) return null;
    const sameSeries = candidate => candidate.dataset.cfxSeriesKey === data.cfxSeriesKey
      && !candidate.closest('[data-cfx-role="legend-item"],[data-cfx-role="legend-native"],[data-cfx-role="legend-proxy"]');
    const nodes = new Map(Array.from(root?.querySelectorAll('[data-cfx-node]') || []).filter(sameSeries)
      .map(candidate => [candidate.dataset.cfxNode, candidate]));
    const links = new Map(Array.from(root?.querySelectorAll('[data-cfx-source][data-cfx-target]') || []).filter(sameSeries)
      .map((candidate, index) => [candidate.dataset.cfxTargetId || candidate.dataset.cfxId || candidate.id || index, candidate]));
    const sum = (id, end) => Array.from(links.values()).reduce((total, link) => {
      const value = Number(link.dataset.cfxValue);
      return link.dataset[end] === id && Number.isFinite(value) ? total + value : total;
    }, 0);
    const source = nodes.get(data.cfxSource), target = nodes.get(data.cfxTarget);
    return { sourceName: data.cfxSourceLabel || source?.dataset.cfxLabel,
      targetName: data.cfxTargetLabel || target?.dataset.cfxLabel,
      sourceTotal: tooltipNumber(source?.dataset.cfxOutgoing) ? Number(source.dataset.cfxOutgoing) : sum(data.cfxSource, 'cfxSource'),
      incoming: tooltipNumber(data.cfxIncoming) ? Number(data.cfxIncoming) : sum(data.cfxNode, 'cfxTarget'),
      outgoing: tooltipNumber(data.cfxOutgoing) ? Number(data.cfxOutgoing) : sum(data.cfxNode, 'cfxSource') };
  };
  const tooltipHeading = node => {
    const data = node.dataset || {}, hierarchy = tooltipHierarchy(node);
    if (data.cfxAuthoredLabel !== undefined && !['rangebar', 'boxplot', 'boxplotoutlier'].includes((data.cfxKind || '').toLowerCase()))
      return data.cfxAuthoredLabel && data.cfxAuthoredLabel === targetCategory(node)
        ? categoryReadoutText(node, data.cfxAuthoredLabel) : data.cfxAuthoredLabel;
    if (hierarchy) return hierarchy.path;
    const flow = tooltipFlowFacts(node);
    if (data.cfxSource && data.cfxTarget && flow) return (flow.sourceName || 'Source') + ' → ' + (flow.targetName || 'Destination');
    const kind = (data.cfxKind || '').toLowerCase();
    if (kind === 'calendarheatmap' && data.cfxDate) return data.cfxDate;
    if (['candlestick', 'ohlc'].includes(kind)) {
      const series = seriesLabel(node), category = categoryReadoutText(node, text(node, true));
      return [series, category !== series ? category : undefined].filter(Boolean).join(' · ');
    }
    if (data.cfxDuration !== undefined && data.cfxStart !== undefined) return seriesLabel(node) || data.cfxLabel || '';
    if (['scatter', 'bubble'].includes(kind)) {
      const label = tooltipAxis(node, 'X').label || rowName(node, 'x', 'Observation');
      return label + ' ' + categoryReadoutText(node, data.cfxXLabel ?? tooltipValueText(node, 'X') ?? '');
    }
    return categoryReadoutText(node, text(node, true));
  };
  const tooltipDomainRows = (node, rows) => {
    const data = node.dataset || {}, kind = (data.cfxKind || '').toLowerCase();
    const heading = tooltipHeading(node), category = categoryReadoutText(node, targetCategory(node));
    const hierarchy = tooltipHierarchy(node), result = [], missingValue = capturedText(data, 'cfxFormattedValue', rowName(node, 'no-data', 'No data'));
    if (kind === 'heatmap' && seriesLabel(node)) result.push({ name: tooltipAxis(node).label || rowName(node, 'row', 'Row'), value: seriesLabel(node) });
    if (data.cfxDuration !== undefined && data.cfxStart !== undefined) {
      return [{ name: rowName(node, 'start', 'Start'), value: data.cfxFormattedStart ?? data.cfxXLabel ?? data.cfxStart },
        { name: rowName(node, 'end', 'End'), value: data.cfxOpen === 'true' ? rowName(node, 'ongoing', 'Ongoing') : data.cfxFormattedEnd ?? data.cfxEnd },
        { name: rowName(node, 'duration', 'Duration'), value: data.cfxDuration },
        ...(tooltipNumber(data.cfxProgress) ? [{ name: rowName(node, 'progress', 'Progress'), value: percentText(data.cfxProgress) }] : []), ...metadataRows(node)];
    }
    rows.forEach(row => {
      if (row.authored) { result.push(row); return; }
      if (row.name === 'Series' || row.name === 'Level' || row.name === 'Percent' && hierarchy) return;
      if (row.name === 'Size') row = { ...row, name: tooltipMetadata(node).sizeLabel || rowName(node, 'size', 'Size') };
      if (row.name === 'Status' && /^(empty|none|normal|unknown)$/i.test(row.value)) return;
      if (row.name === 'Target' && (data.cfxTargetLabel || tooltipFlowFacts(node))) return;
      if (row.name === 'X') {
        if (['scatter', 'bubble'].includes(kind) || row.value === heading || row.value === category) return;
        row = { ...row, name: tooltipAxis(node, 'X').label || rowName(node, 'category', 'Category') };
      }
      if (row.name === 'Y' || row.name === rowName(node, 'value', 'Value') && ['scatter', 'bubble'].includes(kind))
        row = { ...row, name: tooltipAxis(node).label || rowName(node, 'value', 'Value'), value: tooltipValueText(node) };
      if (row.name === rowName(node, 'value', 'Value') && (data.cfxValue !== undefined || data.cfxY !== undefined))
        row = { ...row, value: data.cfxEmpty === 'true' || data.cfxStatus === 'empty' ? missingValue : tooltipValueText(node) };
      if (row.value === undefined || row.value === null) return;
      result.push(row);
    });
    if ((data.cfxStatus === 'empty' || data.cfxEmpty === 'true') && !result.some(row => row.value === missingValue))
      result.push({ name: rowName(node, 'value', 'Value'), value: missingValue });
    if (hierarchy) result.push(...hierarchy.rows);
    const flow = tooltipFlowFacts(node);
    if (data.cfxNode && flow) {
      result.push({ name: rowName(node, 'incoming', 'Incoming'), value: tooltipNumberText(node, flow.incoming) });
      result.push({ name: rowName(node, 'outgoing', 'Outgoing'), value: tooltipNumberText(node, flow.outgoing) });
    }
    if (data.cfxSource && tooltipNumber(data.cfxValue) && flow?.sourceTotal > 0) {
      result.push({ name: '% of ' + (flow.sourceName || 'source'), value: percentText(Number(data.cfxValue) / flow.sourceTotal) });
    }
    return result;
  };
  // All families share the same accessible header / label / value structure.
  const renderTooltipContent = (tip, titleText, rows, node) => {
    tip.replaceChildren();
    const title = document.createElement('div'); title.className = 'cfx-tooltip__title'; title.textContent = titleText;
    tip.appendChild(title);
    const series = seriesLabel(node), heading = String(titleText ?? '');
    const named = name => name === series || name.startsWith(series + ' · ') || name.startsWith(series + ': ');
    const secondary = node.dataset?.cfxSecondaryLabel || (series && !named(heading) && !rows.some(row => named(row.name)) ? series : '');
    if (secondary) { const caption = document.createElement('div'); caption.className = 'cfx-tooltip__secondary'; caption.textContent = secondary; tip.appendChild(caption); }
    const list = document.createElement('dl'); list.className = 'cfx-tooltip__meta';
    rows.forEach(row => {
      const term = document.createElement('dt'), value = document.createElement('dd');
      term.textContent = row.name; value.textContent = row.value;
      if (row.point) {
        term.dataset.cfxTooltipSeries = row.index; term.dataset.cfxTooltipSeriesKey = row.key;
        term.dataset.cfxTooltipPoint = row.point.dataset.cfxPoint; term.dataset.cfxTooltipSourcePoint = row.source;
      }
      if (row.dimension) {
        term.dataset.cfxTooltipDimension = row.dimension; term.dataset.cfxTooltipObservation = row.key;
      }
      if (row.active) { term.dataset.cfxActive = 'true'; value.dataset.cfxActive = 'true'; }
      if (row.total) { term.dataset.cfxTotal = ''; value.dataset.cfxTotal = ''; }
      if (row.state === 'quiet' || row.state === 'success') { term.classList.add('cfx-tooltip__quiet'); value.classList.add('cfx-tooltip__quiet'); }
      const colour = row.colour || (!row.secondary && tooltipSolidColour(row.point || node));
      if (colour) {
        const swatch = document.createElement('span'); swatch.className = 'cfx-tooltip__swatch';
        if (/^(line|stepline|trendline|slope)$/.test((row.point || node).dataset?.cfxKind?.toLowerCase() || '')) swatch.classList.add('cfx-tooltip__swatch--line');
        swatch.style.backgroundColor = colour; swatch.setAttribute('aria-hidden', 'true'); term.prepend(swatch);
        term.style.setProperty('--cfx-series-color', colour); value.style.setProperty('--cfx-series-color', colour);
      }
      if (row.direction) { term.dataset.cfxDirection = row.direction; value.dataset.cfxDirection = row.direction; }
      list.append(term, value);
    });
    if (rows.length) tip.appendChild(list);
    const root = node.closest?.('.cfx-interactive-chart');
    const callback = root?.cfxTooltipRenderer;
    if (typeof callback !== 'function' && root?.dataset.cfxTooltipTemplate === undefined) return true;
    const context = { title: titleText, series: seriesLabel(node), category: targetCategory(node), value: tooltipValueText(node),
      target: targetIdentity(node), rows: rows.map(row => ({ name: row.name, value: row.value })) };
    if (typeof callback === 'function') {
      const rendered = callback(context);
      if (rendered?.nodeType && rendered.ownerDocument === tip.ownerDocument) tip.replaceChildren(rendered);
      else if (typeof rendered === 'string') tip.textContent = rendered;
    } else if (root?.dataset.cfxTooltipTemplate !== undefined) {
      tip.textContent = root.dataset.cfxTooltipTemplate.replace(/\{(title|series|category|value)\}/g, (_, key) => context[key] ?? '');
    }
    return true;
  };
