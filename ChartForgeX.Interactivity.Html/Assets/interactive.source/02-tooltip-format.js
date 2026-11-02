  // Adapter-only display policy. Authored native captions remain authoritative.
  const tooltipMetadataCache = new WeakMap();
  const tooltipMetadata = node => {
    const root = node.closest?.('.cfx-interactive-chart'); if (!root) return {};
    const source = root.dataset.cfxPreparedChart || '', previous = tooltipMetadataCache.get(root);
    if (previous?.source === source) return previous.value;
    let value; try { value = JSON.parse(source).tooltip || {}; } catch { value = {}; }
    tooltipMetadataCache.set(root, { source, value }); return value;
  };
  const tooltipAxis = (node, member = 'Value') => {
    const data = node.dataset || {}, axes = tooltipMetadata(node).axes || {};
    const horizontal = node.closest?.('.cfx-interactive-chart')?._cfxDataProjection?.horizontal;
    const name = member === 'X' ? horizontal ? 'y' : data.cfxXAxis === 'secondary' ? 'secondaryX' : 'x'
      : data.cfxYAxis === 'secondary' ? horizontal ? 'secondaryX' : 'secondaryY' : horizontal ? 'x' : 'y';
    const axis = axes[name] || {}, title = axis.title || '';
    const unit = axis.unit ?? title.match(/\(([^()]+)\)\s*$/)?.[1] ?? '';
    return { ...axis, unit, label: title.replace(/\s*\([^()]+\)\s*$/, '') };
  };
  const tooltipNumberText = (node, value, member = 'Value') => {
    if (!tooltipNumber(value)) return value;
    const metadata = tooltipMetadata(node), policy = metadata.series?.[Number(node.dataset?.cfxSeries)] || {};
    const axis = tooltipAxis(node, member), decimals = member === 'X' ? axis.decimals : policy.decimals;
    const root = node.closest?.('.cfx-interactive-chart');
    let result;
    try {
      result = new Intl.NumberFormat(root?.lang || 'en', Number.isInteger(decimals) && decimals >= 0 && decimals <= 12
        ? { minimumFractionDigits: decimals, maximumFractionDigits: decimals } : { maximumFractionDigits: 6 }).format(Number(value));
    } catch { result = String(Number(value)); }
    if (Number(value) !== 0 && Math.abs(Number(value)) < .5 * 10 ** -(Number.isInteger(decimals) ? decimals : 6)) result = String(Number(value));
    const unit = member === 'X' ? axis.unit : policy.unit ?? axis.unit;
    return result + (unit ? (unit === '%' || unit === '‰' || unit === '°' ? '' : ' ') + unit : '');
  };
  const tooltipValueText = (node, member = 'Value') => {
    const data = node.dataset || {}, metadata = tooltipMetadata(node);
    if (member === 'Value' && (data.cfxKind || '').toLowerCase() === 'statetimeline') return data.cfxStateLabel ?? data.cfxLabel ?? '';
    const policy = metadata.series?.[Number(data.cfxSeries)] || {};
    const close = member === 'Value' && ['candlestick', 'ohlc'].includes((data.cfxKind || '').toLowerCase());
    const median = member === 'Value' && (data.cfxKind || '').toLowerCase() === 'boxplot';
    const captured = data['cfxFormatted' + member] ?? (close ? data.cfxFormattedClose : median ? data.cfxFormattedMedian : member === 'Value' ? data.cfxFormattedY : undefined);
    const raw = member === 'Value' ? data.cfxValue ?? (median ? data.cfxMedian : undefined) ?? data.cfxY ?? (close ? data.cfxClose : undefined) : data['cfx' + member];
    // A captured empty caption is intentional too. Only the adapter's explicitly
    // automatic policy may regularize an otherwise plain numeric native caption.
    const axis = tooltipAxis(node, member), unit = policy.unit ?? axis.unit;
    const plainCaption = unit && captured?.endsWith(unit) ? captured.slice(0, -unit.length).trim() : captured;
    const preserve = member === 'X' || policy.preserveFormatted !== false;
    if (captured !== undefined && (preserve || !tooltipNumber(plainCaption) || /[eE]/.test(plainCaption))) return captured;
    return tooltipNumberText(node, raw, member);
  };
  const tooltipSolidColour = (node, fallback) => {
    const colour = fallback || paintColour(node);
    // Opacity belongs to the mark, never to its identifying swatch.
    return (colour || '').replace(/^rgba\(([^,]+),([^,]+),([^,]+),[^)]+\)$/i, 'rgb($1,$2,$3)')
      .replace(/^(#[\da-f]{6})[\da-f]{2}$/i, '$1');
  };
