'use strict';
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const runtime = ['adapter-native-text-export.js', 'interactive.js'].map(name => fs.readFileSync(path.join(assets, name), 'utf8')).join('\n');
// One maintained value transport fixture. No native geometry or pixel proof is claimed here.
function fixture(kind = 'layered-radial-value', managed = false) {
  const dom = new JSDOM('<!doctype html><body></body>', { runScripts: 'outside-only', pretendToBeVisual: true, url: 'https://example.test/' });
  const { window } = dom, frames = new Map(); let sequence = 0;
  window.requestAnimationFrame = callback => { const id = ++sequence; frames.set(id, callback); return id; };
  window.cancelAnimationFrame = id => frames.delete(id);
  window.SVGElement.prototype.getBBox = () => ({ x: 40, y: 40, width: 200, height: 20 });
  window.HTMLElement.prototype.getBoundingClientRect = () => ({ left: 10, top: 20, width: 360, height: 260 });
  Object.defineProperty(window.HTMLElement.prototype, 'offsetWidth', { configurable: true, get: () => 360 });
  Object.defineProperty(window.HTMLElement.prototype, 'offsetHeight', { configurable: true, get: () => 260 });
  window.SVGElement.prototype.getBoundingClientRect = () => ({ left: 24, top: 34, width: 320, height: 220 });
  const roles = { 'progress-value': 'progress-row', 'layered-radial-value': 'layered-radial-point', 'circle-value': 'circle-chart',
    'progress-ring-value': 'progress-ring-point', 'gauge-value': 'gauge', 'bullet-value': 'bullet-row', 'cartesian-value': 'point', 'radial-value': 'radial-point', 'hierarchy-value': 'sunburst-segment' };
  const kinds = { 'progress-value': 'Progress', 'layered-radial-value': 'LayeredRadial', 'circle-value': 'Circle',
    'progress-ring-value': 'ProgressRing', 'gauge-value': 'Gauge', 'bullet-value': 'Bullet', 'cartesian-value': 'Line', 'radial-value': 'Pie', 'hierarchy-value': 'Sunburst' };
  const role = roles[kind], scale = kind === 'progress-value' ? 1 : ['radial-value', 'hierarchy-value'].includes(kind) ? Number.MAX_VALUE : 100;
  const make = value => {
    const root = window.document.createElement('section'); root.className = 'cfx-interactive-chart';
    Object.assign(root.dataset, { cfxChartId: 'value-update', cfxInteractionFeatures: 'Selection,KeyboardNavigation',
      cfxPreparedChart: JSON.stringify({ series: [{ name: 'Current', key: 'current', local: false,
        kind: kinds[kind],
        state: 'None', indices: [0] }], xLabels: [], regions: [] }) });
    root.innerHTML = '<div class="cfx-stage"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 260">'
      + '<g data-cfx-role="' + role + '" data-cfx-series="0" data-cfx-source-point="0" data-cfx-point="0" data-cfx-series-key="current"'
      + ' data-cfx-series-scope="shared" data-cfx-observation-id="value" data-cfx-value="' + value + '" data-cfx-min="0" data-cfx-max="' + scale
      + '"><rect x="40" y="40" width="200" height="20" fill="#2468ac"/></g></svg></div><div class="cfx-tooltip" hidden></div>';
    if (kind === 'radial-value') { const row = root.querySelector('[data-cfx-role=radial-point]'); row.dataset.cfxDerived = 'radial-slice'; row.dataset.cfxSourcePoints = '0'; row.dataset.cfxPercent = '.5'; row.removeAttribute('data-cfx-min'); row.removeAttribute('data-cfx-max'); }
    if (kind === 'hierarchy-value') {
      const row = root.querySelector('[data-cfx-role=sunburst-segment]'), svg = root.querySelector('svg');
      const owner = svg.ownerDocument.createElementNS(svg.namespaceURI, 'g');
      Object.assign(owner.dataset, { cfxRole: 'hierarchy-series', cfxSeries: '0', cfxSeriesKey: 'current' });
      row.replaceWith(owner); owner.appendChild(row); row.dataset.cfxNode = 'value'; row.dataset.cfxAuthoredValue = String(value);
      row.dataset.cfxValue = String(value); row.dataset.cfxLeaf = 'true'; row.dataset.cfxGeometryStatus = value > 0 ? 'visible' : 'zero';
      for (const name of ['data-cfx-point', 'data-cfx-source-point', 'data-cfx-observation-id', 'data-cfx-min', 'data-cfx-max']) row.removeAttribute(name);
      const metadata = JSON.parse(root.dataset.cfxPreparedChart); metadata.hierarchy = { kind: 'sunburst', seriesKey: 'current', rootId: null, path: [] }; root.dataset.cfxPreparedChart = JSON.stringify(metadata);
    }
    if (kind === 'cartesian-value') {
      const metadata = JSON.parse(root.dataset.cfxPreparedChart);
      const projection = maximum => ({ fullMinimum: 0, fullMaximum: maximum, minimum: 0, maximum,
        transformedMinimum: 0, transformedMaximum: maximum, transformedFullMinimum: 0, transformedFullMaximum: maximum,
        scale: 'linear', threshold: 1, reversed: false, plotBounds: { x: 40, y: 40, width: 200, height: 180 } });
      metadata.xProjection = projection(2); metadata.yProjection = projection(100); root.dataset.cfxPreparedChart = JSON.stringify(metadata);
      const row = root.querySelector('[data-cfx-role=point]'); row.dataset.cfxX = '1'; row.dataset.cfxY = String(value); row.dataset.cfxAxis = 'primary';
      row.removeAttribute('data-cfx-value'); row.removeAttribute('data-cfx-min'); row.removeAttribute('data-cfx-max');
    }
    return root;
  };
  const root = make(scale * .2); window.document.body.append(root); window.eval(runtime);
  const controller = window.ChartForgeXCharts.get(root), changes = [];
  root.addEventListener('cfxupdated', event => changes.push(event.detail));
  if (managed) controller.connectNative(() => { throw new Error('No radial source command is configured'); }, { basis: 'initial', axes: null });
  const transition = (before, after) => JSON.stringify({ kind, seriesKey: 'current', ...(kind === 'hierarchy-value' ? { nodeId: 'value' } : { observationId: 'value' }), fromValue: before,
    toValue: after, minimum: 0, maximum: scale, wide: [before, (before + after) / 2, after].map(value => make(value).querySelector('svg').outerHTML) });
  const source = () => { const row = root.querySelector(kind === 'hierarchy-value' ? '.cfx-stage > svg [data-cfx-node=value]' : '.cfx-stage > svg [data-cfx-observation-id=value]'); return Number(kind === 'hierarchy-value' ? row.dataset.cfxAuthoredValue : kind === 'cartesian-value' ? row.dataset.cfxY : row.dataset.cfxValue); };
  return { window, root, controller, changes, make, transition, scale, source,
    close() { controller.dispose(); dom.window.close(); } };
}
module.exports = { fixture };
