// Chart options for the playground. Reads and edits the first chartforgex chart or timeline fence in a
// markup document, so the options panel changes the source text and the source stays the only state.

const FENCE = /^\s*(`{3,}|~{3,})\s*chartforgex\s+(chart|timeline)\s+v1\b(.*)$/i;
const SIZES = { compact: '640x400', wide: '1200x520' };
const ATTRIBUTE = /(^|\s+)([\w-]+)\s*=\s*("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|[^\s}]+)/g;

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
  const size = commandValue(fence, ['size']) ?? attribute(info, 'size')
    ?? (attribute(info, 'width') || attribute(info, 'height') ? 'custom' : undefined);
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
      setCommand(fence, ['dataLabels', 'showDataLabels', 'labelsVisible'], 'dataLabels ' + on);
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

// Controls edit effective commands and rows in options sections; unrelated data stays intact.
function commandPattern(names) {
  return new RegExp('^\\s*(?:option\\s+)?(?:' + names.join('|') + '):?(?:\\s+(.*?))?\\s*$', 'i');
}

function commandEntries(fence, names) {
  const pattern = commandPattern(names);
  const entries = [];
  let section = '', headers = null;
  for (let index = fence.start + 1; index < fence.end; index++) {
    const line = stripComment(fence.lines[index]).trim();
    const start = /^(options|data|nodes|flows):?$/i.exec(line);
    if (start || /^end$/i.test(line)) {
      section = start ? start[1].toLowerCase() : '';
      headers = null;
      continue;
    }
    if (section === 'nodes' || section === 'flows') continue;
    if (section === 'options' && line.includes('|') && (line.startsWith('|') || headers)) {
      const cells = line.replace(/^\||\|$/g, '').split(/(?<!\\)\|/).map(cell => cell.trim());
      if (cells.every(cell => /^:?-+:?$/.test(cell))) continue;
      if (!headers) { headers = cells.map(key); continue; }
      const name = cells[headers.findIndex(header => ['option', 'key', 'name'].includes(header))];
      if (names.some(item => key(item) === key(name)))
        entries.push({ index, value: unquote(cells[headers.indexOf('value')] ?? '') });
      continue;
    }
    const match = pattern.exec(line);
    if (match) entries.push({ index, value: unquote(match[1] ?? '') });
  }
  return entries;
}

function commandValue(fence, names) {
  return commandEntries(fence, names).at(-1)?.value;
}

function stripComment(line) {
  let quoted = false;
  for (let index = 0; index < line.length - 1; index++) {
    if (line[index] === '"') quoted = !quoted;
    if (!quoted && line[index] === '/' && line[index + 1] === '/' && (index === 0 || /\s/.test(line[index - 1])))
      return line.slice(0, index);
  }
  return line;
}

function setCommand(fence, names, line) {
  for (const { index } of commandEntries(fence, names).reverse()) {
    fence.lines.splice(index, 1);
    fence.end--;
  }
  if (line == null) return;
  // Before the body is always outside an options/data section, including an unterminated final section.
  fence.lines.splice(fence.start + 1, 0, fence.indent + line);
  fence.end++;
}

function setType(fence, type) {
  setCommand(fence, ['type', 'kind'], null);
  const info = fence.lines[fence.start];
  if (attribute(info, 'type') !== undefined) {
    fence.lines[fence.start] = info.replace(/\{([^}]*)\}\s*$/, (_, attributes) => '{' + attributes.replace(ATTRIBUTE,
      (match, space, name) => key(name) === 'type' ? space + name + '="' + type + '"' : match) + '}');
  } else if (/\{[^}]*\}\s*$/.test(info)) {
    fence.lines[fence.start] = info.replace(/\s*\}\s*$/, ' type="' + type + '"}');
  } else {
    fence.lines[fence.start] = info.replace(/\s*$/, ' {type="' + type + '"}');
  }
}

function attribute(info, name) {
  const block = /\{([^}]*)\}\s*$/.exec(info);
  if (!block) return undefined;
  for (const match of block[1].matchAll(ATTRIBUTE))
    if (key(match[2]) === key(name)) return unquote(match[3]);
  return undefined;
}

function removeAttributes(info, names) {
  return info.replace(/\{([^}]*)\}\s*$/, (_, attributes) => {
    attributes = attributes.replace(ATTRIBUTE,
      (match, space, name) => names.some(item => key(item) === key(name)) ? space : match);
    return attributes.trim() ? '{' + attributes.trim() + '}' : '';
  }).trimEnd();
}

function unquote(value) {
  const text = String(value).trim();
  return /^(["']).*\1$/.test(text) ? text.slice(1, -1) : text;
}
