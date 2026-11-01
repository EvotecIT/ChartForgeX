// Chart options for the playground. Reads and edits the first chartforgex chart or timeline fence in a
// markup document, so the options panel changes the source text and the source stays the only state.

const FENCE = /^\s*(`{3,}|~{3,})\s*chartforgex\s+(chart|timeline)\s+v1\b(.*)$/i;
const SIZES = { compact: '640x400', wide: '1200x520' };

// Chart types that read the same table (labels plus one or more series) and can replace each other.
const SERIES_TYPES = [
  ['bar', 'Bar'], ['horizontalBar', 'Horizontal bar'], ['line', 'Line'], ['smoothLine', 'Smooth line'],
  ['stepLine', 'Step line'], ['area', 'Area'], ['smoothArea', 'Smooth area'], ['scatter', 'Scatter'],
  ['lollipop', 'Lollipop'], ['radar', 'Radar']
];
// Part-to-whole types that read one series of labelled values.
const PART_TYPES = [['donut', 'Donut'], ['pie', 'Pie'], ['funnel', 'Funnel'], ['pyramid', 'Pyramid'], ['polarArea', 'Polar area']];
// Area types and their stacked forms.
const STACKS = { area: 'stackedArea', smootharea: 'smoothStackedArea' };
const UNSTACKS = { stackedarea: 'area', smoothstackedarea: 'smoothArea' };
const LABELLED_BY_DEFAULT = new Set(['donut', 'pie']);

const key = (value) => String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase();

/** Finds the first chart or timeline fence: its line range, kind and the line holding its attributes. */
export function findChartFence(source) {
  const lines = source.split('\n');
  for (let index = 0; index < lines.length; index++) {
    const match = FENCE.exec(lines[index]);
    if (!match) continue;
    const marker = match[1];
    let end = lines.length;
    for (let next = index + 1; next < lines.length; next++) {
      const trimmed = lines[next].trim();
      if (trimmed.length >= marker.length && trimmed[0] === marker[0] && /^([`~])\1*$/.test(trimmed)) { end = next; break; }
    }
    return { lines, start: index, end, kind: match[2].toLowerCase(), indent: /^\s*/.exec(lines[index])[0] };
  }
  return null;
}

/** Returns the options panel state for the source, or null when it has no chart or timeline fence. */
export function readChartOptions(source) {
  const fence = findChartFence(source);
  if (!fence) return null;
  const info = fence.lines[fence.start];
  const size = commandValue(fence, ['size']) ?? attribute(info, 'size');
  const sizeName = Object.keys(SIZES).find((name) => SIZES[name] === (size || '').toLowerCase().replace(/\s/g, '')) ?? (size ? 'custom' : '');
  if (fence.kind === 'timeline') return { kind: 'timeline', size: sizeName };

  const type = commandValue(fence, ['type', 'kind']) ?? attribute(info, 'type') ?? 'bar';
  const typeKey = key(type);
  const base = UNSTACKS[typeKey] ?? type;
  const family = familyOf(base);
  const legendOff = commandValue(fence, ['legend', 'showLegend']);
  const legend = legendOff !== undefined && /^(false|no|off|0)$/i.test(legendOff) ? 'off' : (commandValue(fence, ['legendPosition']) ?? '');
  const labels = commandValue(fence, ['dataLabels', 'showDataLabels', 'labelsVisible']);
  return {
    kind: 'chart',
    type: family ? family.find(([value]) => key(value) === key(base))[0] : type,
    types: family,
    stack: key(base) in STACKS ? (typeKey in UNSTACKS ? 'stacked' : 'overlap') : null,
    size: sizeName,
    legend: normaliseLegend(legend),
    labels: labels === undefined ? LABELLED_BY_DEFAULT.has(typeKey) : labels === '' || /^(true|yes|on|1)$/i.test(labels)
  };
}

/** Applies one option change and returns the new source. */
export function applyChartOption(source, option, value) {
  const fence = findChartFence(source);
  if (!fence) return source;
  const current = readChartOptions(source);
  switch (option) {
    case 'type': {
      const stacked = current.stack === 'stacked' && key(value) in STACKS;
      setType(fence, stacked ? STACKS[key(value)] : value);
      break;
    }
    case 'stack':
      setType(fence, value === 'stacked' ? STACKS[key(current.type)] : current.type);
      break;
    case 'size':
      fence.lines[fence.start] = removeAttributes(fence.lines[fence.start], ['size', 'width', 'height']);
      setCommand(fence, ['size'], SIZES[value] ? 'size ' + SIZES[value] : null);
      break;
    case 'legend':
      setCommand(fence, ['legend', 'showLegend'], value === 'off' ? 'legend false' : null);
      setCommand(fence, ['legendPosition'], value && value !== 'off' ? 'legendPosition ' + value : null);
      break;
    case 'labels': {
      const on = value === true || value === 'true';
      const typeKey = key(commandValue(fence, ['type', 'kind']) ?? attribute(fence.lines[fence.start], 'type') ?? 'bar');
      setCommand(fence, ['dataLabels', 'showDataLabels', 'labelsVisible'], on === LABELLED_BY_DEFAULT.has(typeKey) ? null : 'dataLabels ' + on);
      break;
    }
    default:
      return source;
  }
  return fence.lines.join('\n');
}

function familyOf(type) {
  const value = key(type);
  if (SERIES_TYPES.some(([item]) => key(item) === value)) return SERIES_TYPES;
  if (PART_TYPES.some(([item]) => key(item) === value)) return PART_TYPES;
  return null;
}

function normaliseLegend(value) {
  if (value === 'off' || value === '') return value;
  const position = ['Top', 'Bottom', 'Right', 'Left'].find((item) => key(item) === key(value));
  return position ?? value;
}

// Body lines are either "command value" or "option command value"; table rows and other commands are left alone.
function commandPattern(names) {
  return new RegExp('^\\s*(?:option\\s+)?(?:' + names.join('|') + '):?(?:\\s+(.*?))?\\s*$', 'i');
}

function commandValue(fence, names) {
  const pattern = commandPattern(names);
  let value;
  for (let index = fence.start + 1; index < fence.end; index++) {
    const match = pattern.exec(fence.lines[index]);
    if (match) value = unquote(match[1] ?? '');
  }
  return value;
}

function setCommand(fence, names, line) {
  const pattern = commandPattern(names);
  let at = -1;
  for (let index = fence.end - 1; index > fence.start; index--) {
    if (!pattern.test(fence.lines[index])) continue;
    fence.lines.splice(index, 1);
    fence.end--;
    at = index;
  }
  if (line == null) return;
  const position = at > 0 ? at : fence.end;
  fence.lines.splice(position, 0, fence.indent + line);
  fence.end++;
}

function setType(fence, type) {
  const pattern = commandPattern(['type', 'kind']);
  for (let index = fence.start + 1; index < fence.end; index++) {
    if (pattern.test(fence.lines[index])) {
      fence.lines[index] = fence.indent + 'type ' + type;
      return;
    }
  }
  const info = fence.lines[fence.start];
  if (attribute(info, 'type') !== undefined) {
    fence.lines[fence.start] = info.replace(/(\{[^}]*?\btype\s*=\s*)("[^"]*"|'[^']*'|[^\s}]+)/i, '$1"' + type + '"');
  } else if (/\{[^}]*\}\s*$/.test(info)) {
    fence.lines[fence.start] = info.replace(/\s*\}\s*$/, ' type="' + type + '"}');
  } else {
    fence.lines[fence.start] = info.replace(/\s*$/, ' {type="' + type + '"}');
  }
}

function attribute(info, name) {
  const block = /\{([^}]*)\}\s*$/.exec(info);
  if (!block) return undefined;
  const match = new RegExp('(?:^|\\s)' + name + '\\s*=\\s*("[^"]*"|\'[^\']*\'|[^\\s}]+)', 'i').exec(block[1]);
  return match ? unquote(match[1]) : undefined;
}

function removeAttributes(info, names) {
  let result = info;
  for (const name of names) {
    result = result.replace(new RegExp('\\s+' + name + '\\s*=\\s*("[^"]*"|\'[^\']*\'|[^\\s}]+)', 'i'), '');
  }
  return result.replace(/\{\s+/, '{').replace(/\s*\{\s*\}\s*$/, '');
}

function unquote(value) {
  const text = String(value).trim();
  return /^(["']).*\1$/.test(text) ? text.slice(1, -1) : text;
}
