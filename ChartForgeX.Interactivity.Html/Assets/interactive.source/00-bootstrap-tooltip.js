  // Captured display text may deliberately be empty; only an absent attribute falls back to raw facts.
  const capturedText = (data, key, fallback) => data[key] !== undefined ? data[key] : fallback;
  const tooltipNumber = (value) => value !== undefined && value !== null && String(value).trim() !== '' && Number.isFinite(Number(value));
  // Source metadata stays exact; fallback captions use the browser's shortest round-trip number.
  const numericDisplayText = (value) => tooltipNumber(value) ? String(Number(value)) : value;
  const numericFactText = (data, member) => capturedText(data, 'cfxFormatted' + member, numericDisplayText(data['cfx' + member]));
  const text = (node, tooltipTitle = false) => {
    const data = node.dataset || {};
    if (tooltipTitle && ['rangebar', 'boxplot', 'boxplotoutlier'].includes((data.cfxKind || '').toLowerCase()))
      return [seriesLabel(node), data.cfxAuthoredLabel ?? targetCategory(node) ?? data.cfxLabel].filter(Boolean).join(': ');
    if (isDataTarget(node)) return data.cfxAuthoredLabel ?? targetCategory(node) ?? data.cfxDisplayLabel ?? (data.cfxLabel || data.cfxText || seriesLabel(node) || data.cfxRole || '');
    const aria = node.getAttribute('aria-label');
    if (aria) return aria;
    const label = data.cfxLabel || data.cfxText || '';
    const parts = [];
    if (label) parts.push(label);
    else if (data.cfxRole) parts.push(data.cfxRole.replace(/-/g, ' '));
    if (data.cfxSeries !== undefined && !label) parts.push(seriesLabel(node));
    if (data.cfxPoint !== undefined && (!tooltipTitle || !label)) parts.push('Point ' + data.cfxPoint);
    const value = capturedText(data, 'cfxFormattedValue', data.cfxValue || data.cfxY || data.cfxEnd || data.cfxTarget || '');
    if (value) parts.push(rowName(node, 'value', 'Value') + ' ' + value);
    return parts.join(' / ');
  };
  const targetIdentity = (node) => {
    const data = node.dataset || {};
    const targetKind = renderedTargetKind(node);
    const state = (data.cfxKind || '').toLowerCase() === 'statetimeline';
    const rawValue = (data.cfxKind || '').toLowerCase() === 'boxplot' ? data.cfxMedian : data.cfxValue ?? data.cfxY ?? data.cfxEnd;
    return {
      targetKind,
      targetId: renderedTargetId(node, targetKind),
      id: data.cfxId || node.id || '',
      role: data.cfxRole || '',
      label: data.cfxAuthoredLabel ?? targetCategory(node) ?? data.cfxDisplayLabel ?? (data.cfxLabel || data.cfxText || node.getAttribute('aria-label') || ''),
      rangeLabel: data.cfxRangeLabel,
      series: data.cfxSeries,
      seriesKey: seriesKey(node),
      seriesScope: data.cfxSeriesScope,
      observationId: data.cfxObservationId,
      observationIds: contributorObservationIds(node),
      category: targetCategory(node),
      x: targetRawX(node),
      numericValue: !state && tooltipNumber(rawValue) ? Number(rawValue) : undefined,
      range: targetDataRange(node),
      point: data.cfxPoint,
      sourcePoint: sourcePointIndex(node),
      sourcePoints: sourcePointCollection(node),
      derived: data.cfxDerived,
      groupIndex: data.cfxGroupIndex === undefined ? undefined : Number(data.cfxGroupIndex),
      groupId: data.cfxGroupId,
      sourceCount: data.cfxSourceCount === undefined ? undefined : Number(data.cfxSourceCount),
      legendTargetKind: data.cfxLegendTargetKind,
      legendTargetId: data.cfxLegendTargetId,
      value: state ? data.cfxStateLabel ?? data.cfxLabel ?? '' : capturedText(data, 'cfxFormattedValue', rawValue ?? ''),
      kind: data.cfxKind || ''
    };
  };
  const metaLabel = (attributeName) => attributeName
    .replace(/^data-cfx-meta-/i, '')
    .split('-')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
  const metadataRows = (node) => Array.from(node.attributes || [])
    .filter((attribute) => attribute.name.toLowerCase().indexOf('data-cfx-meta-') === 0 && attribute.value !== '')
    .map((attribute) => ({ name: metaLabel(attribute.name), value: attribute.value }));
  const rowName = (node, key, fallback) => {
    const owner = node.closest ? node.closest('[data-cfx-label-' + key + ']') : null;
    return (owner && owner.getAttribute('data-cfx-label-' + key)) || fallback;
  };
  const percentFormat = new Intl.NumberFormat('en', { style: 'percent', maximumFractionDigits: 2 });
  const percentText = (value) => value === undefined || value === '' ? value : percentFormat.format(Number(value));
  const colorTooltipRows = (node) => {
    const data = node.dataset || {};
    const value = data.cfxColorMissing === 'true' ? rowName(node, 'no-data', 'No data')
      : capturedText(data, 'cfxFormattedColorValue', data.cfxColorValue);
    return value === undefined ? [] : [{ name: rowName(node, 'color', 'Color'), value }];
  };
  const sizeTooltipRows = (node) => {
    const data = node.dataset || {}, raw = data.cfxSize;
    if (raw === undefined || raw === null || raw === '') return [];
    return [{ name: rowName(node, 'size', 'Size'), value: capturedText(data, 'cfxFormattedSize', raw) }];
  };
  const dimensionTooltipRows = (node) => sizeTooltipRows(node).map((row) => ({ ...row, dimension: 'size' }))
    .concat(colorTooltipRows(node).map((row) => ({ ...row, dimension: 'color' })));
  // A financial Y is the tuple's opening member; all readouts use the complete prices.
  const financialTooltipRows = (node) => {
    const data = node.dataset || {};
    if (!['candlestick', 'ohlc'].includes((data.cfxKind || '').toLowerCase())) return null;
    return ['Open', 'High', 'Low', 'Close'].filter((part) => {
      const value = data['cfx' + part];
      return value !== undefined && value !== null && value !== '';
    }).map((part) => ({ name: rowName(node, part.toLowerCase(), part), value: numericFactText(data, part) }));
  };
  const waterfallTooltipRows = (node) => {
    const data = node.dataset || {};
    if (!data.cfxWaterfallKind) return [];
    const fallback = data.cfxWaterfallKind === 'total' ? 'Total' : data.cfxWaterfallKind === 'subtotal' ? 'Subtotal' : data.cfxWaterfallKind === 'opening-balance' ? 'Opening balance' : 'Change';
    return [{ name: rowName(node, 'waterfall-value', fallback), value: numericFactText(data, 'Value') },
      { name: 'Start', value: numericFactText(data, 'Start') }, { name: 'End', value: numericFactText(data, 'End') }]
      .filter((row) => row.value !== undefined && row.value !== null);
  };
  const boxPlotTooltipRows = (node) => {
    const data = node.dataset || {};
    const kind = (data.cfxKind || '').toLowerCase();
    if (!['boxplot', 'boxplotoutlier'].includes(kind)) return null;
    const rows = [{ name: rowName(node, 'category', 'Category'), value: data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : data.cfxXLabel ?? data.cfxX }];
    if (kind === 'boxplotoutlier') {
      rows.push({ name: rowName(node, 'outlier', 'Outlier'), value: capturedText(data, 'cfxFormattedValue', numericDisplayText(data.cfxValue ?? data.cfxY)) });
      return rows.filter(row => row.value !== undefined && row.value !== null && row.value !== '');
    }
    const iqr = data.cfxWhiskerPolicy === 'Iqr';
    ['minimum', 'q1', 'median', 'q3', 'maximum'].forEach(key => rows.push({
      name: rowName(node, iqr && key === 'minimum' ? 'lower-whisker' : iqr && key === 'maximum' ? 'upper-whisker' : key,
        iqr && key === 'minimum' ? 'Lower whisker' : iqr && key === 'maximum' ? 'Upper whisker' : key === 'q1' ? 'Q1' : key === 'q3' ? 'Q3' : key.charAt(0).toUpperCase() + key.slice(1)),
      value: numericFactText(data, key.charAt(0).toUpperCase() + key.slice(1))
    }));
    if (data.cfxDerived === 'five-number-summary') {
      const add = (key, caption, value) => rows.push({ name: rowName(node, key, caption), value });
      add('sample-count', 'Sample count', data.cfxSourceCount);
      add('whisker-policy', 'Whisker policy', data.cfxWhiskerPolicy);
      add('quartile-estimator', 'Quartile estimator', data.cfxQuartileEstimator);
      if (iqr) add('iqr-multiplier', 'IQR multiplier', data.cfxIqrMultiplier);
      add('source-minimum', 'Source minimum', data.cfxSourceMinimum);
      add('source-maximum', 'Source maximum', data.cfxSourceMaximum);
      add('outlier-count', 'Outlier count', data.cfxOutlierCount);
      if (iqr && (data.cfxInlierMinimum === undefined || Number(data.cfxInlierMinimum) !== Number(data.cfxMinimum)))
        add('inlier-minimum', 'Inlier minimum', data.cfxInlierMinimum ?? rowName(node, 'no-inliers', 'None'));
      if (iqr && (data.cfxInlierMaximum === undefined || Number(data.cfxInlierMaximum) !== Number(data.cfxMaximum)))
        add('inlier-maximum', 'Inlier maximum', data.cfxInlierMaximum ?? rowName(node, 'no-inliers', 'None'));
    }
    return rows.filter(row => row.value !== undefined && row.value !== null && row.value !== '');
  };
  const rangeBarTooltipRows = (node) => {
    const data = node.dataset || {};
    if ((data.cfxKind || '').toLowerCase() !== 'rangebar') return null;
    return [{ name: rowName(node, 'category', ''), value: data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : data.cfxX },
      { name: rowName(node, 'start', ''), value: numericFactText(data, 'Start') },
      { name: rowName(node, 'end', ''), value: numericFactText(data, 'End') }]
      .filter((row) => row.value !== undefined && row.value !== null);
  };
  const differenceTooltipRows = (node) => {
    const data = node.dataset || {};
    if ((data.cfxKind || '').toLowerCase() !== 'differencearea') return null;
    return ['First', 'Second'].map((part) => ({
      name: rowName(node, part.toLowerCase(), part),
      value: numericFactText(data, part)
    }));
  };
  const gaugeTargetTooltipRows = (node) => Array.from({ length: Number((node.dataset || {}).cfxGaugeTargetCount) || 0 }, (_, index) => {
    const prefix = 'data-cfx-gauge-target-' + index + '-';
    return { name: node.getAttribute(prefix + 'label') || rowName(node, 'target', 'Target'),
      value: node.getAttribute(prefix + 'formatted-value') };
  }).filter((row) => row.value !== null);
  const tooltipRows = (node) => {
    const data = node.dataset || {};
    const rows = [];
    const push = (name, value) => {
      if (value !== undefined && value !== null && value !== '') rows.push({ name, value: String(value) });
    };
    const pushX = () => {
      const value = capturedText(data, 'cfxXLabel', capturedText(data, 'cfxFormattedX', numericDisplayText(data.cfxX) || data.cfxCategory || data.cfxDate || capturedText(data, 'cfxFormattedStart', data.cfxStart)));
      if (data.cfxXLabel !== undefined || data.cfxFormattedX !== undefined) rows.push({ name: 'X', value });
      else push('X', value);
    };
    push('Series', seriesLabel(node));
    const boxPlotRows = boxPlotTooltipRows(node);
    if (boxPlotRows) return rows.concat(boxPlotRows, metadataRows(node));
    const rangeBarRows = rangeBarTooltipRows(node);
    if (rangeBarRows) return rows.concat(rangeBarRows, metadataRows(node));
    const differenceRows = differenceTooltipRows(node);
    if (differenceRows) {
      pushX();
      rows.push(...differenceRows);
      metadataRows(node).forEach((row) => push(row.name, row.value));
      return rows;
    }
    if (data.cfxWaterfallKind) push('Category', data.cfxCategoryLabel !== undefined ? data.cfxCategoryLabel : data.cfxXLabel || data.cfxX);
    else pushX();
    const financialRows = financialTooltipRows(node);
    if (financialRows) rows.push(...financialRows);
    else if (data.cfxWaterfallKind) rows.push(...waterfallTooltipRows(node));
    else if (data.cfxEmpty !== 'true') {
      const name = data.cfxValue !== undefined ? rowName(node, 'value', 'Value') : 'Y';
      const value = capturedText(data, 'cfxFormattedValue', numericDisplayText(data.cfxValue !== undefined ? data.cfxValue : data.cfxY));
      if (data.cfxFormattedValue !== undefined) rows.push({ name, value });
      else push(name, value);
      push('End', capturedText(data, 'cfxFormattedEnd', data.cfxEnd));
    }
    rows.push(...sizeTooltipRows(node));
    rows.push(...gaugeTargetTooltipRows(node));
    push(rowName(node, 'target', 'Target'), data.cfxTarget);
    push('Status', data.cfxStatus);
    push(rowName(node, 'level', 'Level'), data.cfxLevel);
    rows.push(...colorTooltipRows(node));
    if (data.cfxLeaf === 'false' && data.cfxAuthoredValue !== undefined && Number(data.cfxAuthoredValue) !== Number(data.cfxValue))
      push(rowName(node, 'authored-value', 'Provided value'), data.cfxAuthoredValue);
    if (Number(data.cfxRemainderValue) > 0)
      push(rowName(node, 'remainder', 'Remainder'), data.cfxRemainderValue);
    push('Percent', percentText(data.cfxPercent));
    if (!data.cfxWaterfallKind) push('Delta', numericFactText(data, 'Delta'));
    if (tooltipNumber(data.cfxLower) && tooltipNumber(data.cfxUpper)) {
      const lower = numericFactText(data, 'Lower'), upper = numericFactText(data, 'Upper');
      rows.push({ name: 'Range', value: lower === '' && upper === '' ? '' : lower + ' - ' + upper });
    }
    metadataRows(node).forEach((row) => push(row.name, row.value));
    return rows;
  };
  const renderTip = (tip, node) => {
    if ((node.dataset || {}).cfxRole === 'legend-item') return renderLegendTip(tip, node);
    const root = node.closest && node.closest('.cfx-interactive-chart');
    if (root && root.dataset.cfxTooltipMode === 'shared-x' && renderSharedXTip(tip, node, root)) return true;
    const label = text(node, true);
    // An explicitly captured empty caption remains a valid readout of the source fields.
    if (label === undefined || label === null) return false;
    tip.replaceChildren();
    const title = document.createElement('div');
    title.className = 'cfx-tooltip__title';
    title.textContent = label;
    const colour = paintColour(node);
    if (colour) {
      const swatch = document.createElement('span');
      swatch.className = 'cfx-tooltip__swatch'; swatch.style.backgroundColor = colour;
      swatch.setAttribute('aria-hidden', 'true'); title.prepend(swatch);
      title.classList.add('cfx-tooltip__title--series');
    }
    tip.appendChild(title);
    const secondary = (node.dataset || {}).cfxSecondaryLabel;
    if (secondary) {
      const caption = document.createElement('div');
      caption.className = 'cfx-tooltip__secondary';
      caption.textContent = secondary;
      tip.appendChild(caption);
    }
    const rows = tooltipRows(node);
    if (rows.length) {
      const list = document.createElement('dl');
      list.className = 'cfx-tooltip__meta';
      rows.forEach((row) => {
        const term = document.createElement('dt');
        term.textContent = row.name;
        const value = document.createElement('dd');
        value.textContent = row.value;
        list.appendChild(term);
        list.appendChild(value);
      });
      tip.appendChild(list);
    }
    return true;
  };
  const showTip = (root, tip, node, event) => {
    if (!hasFeature(root, 'Tooltips') || root.dataset.cfxTooltipPinned === 'true') { cancelTooltipRequest(root); return; }
    if (!tooltipReadoutAvailable(node, event)) { hideTip(root, tip, false); return; }
    if (event instanceof PointerEvent && tooltipDelay(root) > 0) { requestPointerTip(root, tip, node, event); return; }
    cancelTooltipRequest(root);
    if (displayTip(tip, node, event)) rememberImmediateTooltip(root, tip, node, event);
  };
  const displayTip = (tip, node, event) => {
    if (!renderTip(tip, node)) return false;
    tip.hidden = false;
    moveTip(tip, event, node);
    return true;
  };
  const moveTip = (tip, event, node) => {
    updateTooltipPointer(tip, node, event);
    positionTooltip(tip, event, node);
  };
  const targetKey = (target) => target ? [target.targetKind || '', target.targetId || '', target.id || '', target.role || '', target.label || '', target.series ?? '', target.point ?? '', target.value || '', target.kind || ''].join('|') : '';
  const compareItems = (root) => {
    const items = [];
    const seen = new Set();
    dataTargets(root).filter(node => node.classList.contains('cfx-selected')).forEach((node) => {
      const target = targetIdentity(node);
      const key = targetKey(target);
      if (!key || seen.has(key)) return;
      seen.add(key);
      const data = node.dataset || {};
      let label = data.cfxAuthoredLabel ?? targetCategory(node) ?? data.cfxRangeLabel
        ?? (timeTarget(node) ? data.cfxDisplayLabel : undefined) ?? seriesLabel(node);
      const series = seriesLabel(node);
      if (timeTarget(node) && label && series && label !== series) label = series + ' · ' + label;
      items.push({ label: label ?? target.label ?? target.role ?? target.id ?? 'Target', target });
    });
    return items;
  };
  const selectedTargets = (root) => compareItems(root).map((item) => item.target);
  const renderCompare = (root) => {
    (root._cfxCompareCleanup || []).forEach(release => release());
    root._cfxCompareCleanup = [];
    const tray = root.querySelector('[data-cfx-compare-tray]');
    if (!tray || !hasFeature(root, 'CompareMarkers')) return [];
    const items = compareItems(root);
    tray.replaceChildren();
    if (!items.length) {
      tray.hidden = true;
      root.removeAttribute('data-cfx-compare-count');
      return items;
    }
    tray.hidden = false;
    root.dataset.cfxCompareCount = String(items.length);
    const summary = document.createElement('div');
    summary.className = 'cfx-compare-tray__summary';
    summary.textContent = 'Compare ' + items.length;
    tray.appendChild(summary);
    items.slice(0, 6).forEach((item, index) => {
      const chip = document.createElement('button');
      chip.className = 'cfx-compare-chip';
      chip.type = 'button';
      chip.textContent = item.label;
      chip.dataset.cfxCompareIndex = String(index);
      chip.title = item.label;
      root._cfxCompareCleanup.push(listenChart(root, chip, 'click', () => {
        clearHover(root, false, false);
        if (applyHoverByTarget(root, item.target)) {
          recordFocusTrail(root, item.target, true, true);
          emitHostEvent(root, 'cfxhover', { label: item.label, target: item.target });
        }
      }));
      tray.appendChild(chip);
    });
    if (items.length > 6) {
      const more = document.createElement('span');
      more.className = 'cfx-compare-tray__more';
      more.textContent = '+' + (items.length - 6);
      tray.appendChild(more);
    }
    const clear = document.createElement('button');
    clear.className = 'cfx-compare-clear';
    clear.type = 'button';
    clear.textContent = 'Clear';
    clear.setAttribute('data-cfx-compare-clear', 'true');
    root._cfxCompareCleanup.push(listenChart(root, clear, 'click', () => {
      const returnFocus = tray.contains(document.activeElement);
      clearSelections(root);
      publishSelectionChange(root, 'clear');
      publishCompare(root, true);
      if (returnFocus) {
        if (!root.hasAttribute('tabindex')) root.setAttribute('tabindex', '-1');
        try { root.focus({ preventScroll: true }); } catch { root.focus(); }
      }
    }));
    tray.appendChild(clear);
    return items;
  };
  const publishCompare = (root, sync) => {
    const items = renderCompare(root);
    if (!hasFeature(root, 'CompareMarkers')) return items;
    const targets = items.map((item) => item.target);
    emitHostEvent(root, 'cfxcompare', { count: targets.length, targets });
    if (sync !== false) emitSync(root, { action: 'compare', count: targets.length, targets });
    return items;
  };
  const hideTip = (root, tip, force) => {
    // A delayed pointer handoff can hide an active readout; a force-dismissed readout has no request to restore.
    const restoreKeyboardReadout = tip && (!tip.hidden || tooltipRequests.has(root));
    cancelTooltipRequest(root);
    if (!tip || (!force && root.dataset.cfxTooltipPinned === 'true')) return;
    // Pointer exit can follow host reflow while a keyboard target still owns focus, even in a closed tree.
    const active = !force && hasFeature(root, 'Tooltips') && root.getRootNode().activeElement;
    const focused = active && root.contains(active) && interactiveTargets(root).find(node => targetFocusNode(node) === active);
    if (restoreKeyboardReadout && focused && active.matches(':focus-visible') && tooltipReadoutAvailable(focused)) {
      if (root.dataset.cfxHoverKey !== targetKey(targetIdentity(focused))) setHover(root, focused, true, true);
      showTip(root, tip, focused);
      return;
    }
    forgetTooltipPosition(tip);
    tip.hidden = true;
    tip.classList.remove('cfx-tooltip--pinned');
    root.removeAttribute('data-cfx-tooltip-pinned');
    root.removeAttribute('data-cfx-pinned-target');
  };
  const pinTip = (root, tip, node, event) => {
    cancelTooltipRequest(root);
    if (root.dataset.cfxPinOnSelect === 'false') return;
    if (!hasFeature(root, 'Tooltips') || !tooltipReadoutAvailable(node, event) || !renderTip(tip, node)) return;
    const target = targetIdentity(node);
    const key = targetKey(target);
    const pinned = root.dataset.cfxTooltipPinned === 'true' && root.dataset.cfxPinnedTarget === key;
    if (pinned) {
      hideTip(root, tip, true);
      emitHostEvent(root, 'cfxtooltip', { pinned: false, target });
      return;
    }
    tip.hidden = false;
    tip.classList.add('cfx-tooltip--pinned');
    root.dataset.cfxTooltipPinned = 'true';
    root.dataset.cfxPinnedTarget = key;
    moveTip(tip, event, node);
    emitHostEvent(root, 'cfxtooltip', { pinned: true, label: text(node), target });
  };
  const unpinTooltip = root => {
    const key = root.dataset.cfxPinnedTarget;
    const target = key && interactiveTargets(root).find(node => targetKey(targetIdentity(node)) === key);
    hideTip(root, root.querySelector('.cfx-tooltip'), true);
    if (key) emitHostEvent(root, 'cfxtooltip', { pinned: false, ...(target ? { target: targetIdentity(target) } : {}) });
  };
