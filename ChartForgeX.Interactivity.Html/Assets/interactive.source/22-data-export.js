  const chartDataRows = root => {
    const seen = new Set();
    // Viewport visibility controls hit testing, not the saved report's data.
    const nodes = Array.from(root.querySelectorAll('[data-cfx-target-kind][data-cfx-target-id]'))
      .filter(node => !isPassiveTarget(node) && !node.closest('defs,[data-cfx-role="legend-item"]')
        && renderedTargetKind(node) !== 'legend' && (renderedTargetKind(node) !== 'series' || node.dataset.cfxValue !== undefined));
    return nodes.flatMap(node => {
      const target = targetIdentity(node), key = JSON.stringify([target.targetKind, target.targetId, target.seriesKey]);
      if (seen.has(key)) return []; seen.add(key);
      const data = node.dataset;
      return [{ ...target, seriesIndex: target.series, series: seriesLabel(node), category: target.category ?? target.label, x: target.x,
        nodeId: data.cfxNode, parentId: data.cfxParent, sourceId: data.cfxSource, destinationId: data.cfxTarget,
        hierarchyPath: tooltipHierarchy(node)?.path,
        value: target.numericValue ?? target.value, ...(target.sourcePoint !== undefined ? { sourcePoint: Number(target.sourcePoint) } : {}),
        ...Object.fromEntries(['open', 'high', 'low', 'close', 'size', 'colorValue'].filter(name => tooltipNumber(data['cfx' + name[0].toUpperCase() + name.slice(1)]))
          .map(name => [name, Number(data['cfx' + name[0].toUpperCase() + name.slice(1)])])) }];
    });
  };
  const chartDataText = (root, format) => {
    if (!['csv', 'json'].includes(format)) throw new TypeError('Data export format: csv or json required.');
    const rows = chartDataRows(root);
    if (format === 'json') return JSON.stringify(rows, null, 2);
    const fields = Array.from(new Set(rows.flatMap(row => Object.keys(row))));
    // Spreadsheet readers execute formulas even inside quoted CSV cells. Only
    // text receives the literal prefix; numeric facts and raw JSON stay exact.
    const cell = value => {
      let content = value && typeof value === 'object' ? JSON.stringify(value) : String(value ?? '');
      if (typeof value !== 'number' && /^[\s\u0000-\u001f]*[=+@-]|^[\t\r\n]/.test(content)) content = "'" + content;
      return '"' + content.replace(/"/g, '""') + '"';
    };
    return [fields.map(cell).join(','), ...rows.map(row => fields.map(field => cell(row[field])).join(','))].join('\r\n');
  };
  const copyChartData = async (root, format) => {
    if (!hasFeature(root, 'Export')) return false;
    const value = chartDataText(root, format);
    try {
      if (navigator.clipboard?.writeText) await navigator.clipboard.writeText(value);
      else {
        const active = root.getRootNode().activeElement, input = document.createElement('textarea');
        input.value = value; input.style.cssText = 'position:fixed;left:-10000px;top:0';
        document.body.appendChild(input);
        try { input.select(); if (!document.execCommand('copy')) throw new Error('Clipboard copy is unavailable.'); }
        finally { input.remove(); active?.focus?.({ preventScroll: true }); }
      }
      emitHostEvent(root, 'cfxexport', { format, action: 'copy' }); return true;
    } catch (error) { emitHostEvent(root, 'cfxexporterror', { format, action: 'copy', message: String(error.message || error) }); return false; }
  };
  const exportChartData = (root, format) => {
    if (!hasFeature(root, 'Export')) return;
    downloadBlob(root, new Blob([chartDataText(root, format)], { type: format === 'json' ? 'application/json;charset=utf-8' : 'text/csv;charset=utf-8' }), format, format);
  };
  const captureContextMenuItems = items => {
    if (!Array.isArray(items) || items.some(item => !item || typeof item.id !== 'string' || !item.id.trim()
      || typeof item.label !== 'string' || !item.label.trim() || typeof item.action !== 'function')
      || new Set(items.map(item => item.id)).size !== items.length) throw new TypeError('Context menu: unique id, label and action callback required.');
    return items.map(item => Object.freeze({ id: item.id, label: item.label, action: item.action, disabled: item.disabled === true }));
  };
