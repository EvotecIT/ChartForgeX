const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/topology-interaction.source');
function fixture() {
  const dom = new JSDOM(`<div id="chart" data-chart-id="test"><aside id="controls">
    <input data-cfx-force-search><select data-cfx-force-status><option value=""></option></select>
    <select data-cfx-force-group><option value=""></option><option value="a">A</option><option value="b">B</option></select>
    ${['edges', 'edge-labels', 'groups'].map(name => `<input type="checkbox" data-cfx-force-toggle="${name}" checked>`).join('')}
    <output data-cfx-force-summary></output></aside>
    ${['a', 'b'].map(group => `<div data-cfx-role="topology-group" data-group-id="${group}"></div>` +
      [1, 2].map(n => `<div data-cfx-role="topology-node" data-node-id="${group}${n}" data-group-id="${group}"></div>`).join('') +
      `<div data-cfx-role="topology-edge" data-edge-id="${group}" data-source-node-id="${group}1" data-target-node-id="${group}2"></div>
       <div data-cfx-role="topology-edge-label" data-edge-id="${group}"></div>`).join('')}</div>`);
  const wrapper = dom.window.document.querySelector('#chart'), forceGraphPanel = wrapper.querySelector('#controls');
  let source = fs.readFileSync(path.join(assets, '70-topology-filters.js'), 'utf8');
  source += fs.readFileSync(path.join(assets, '75-filter-summary.js'), 'utf8');
  source += fs.readFileSync(path.join(assets, '80-force-graph-and-bindings.js'), 'utf8').split('    const clearForceFocusLabels =')[0];
  const api = new Function('wrapper', 'forceGraphPanel', 'window', 'CustomEvent', `
    const CSS = window.CSS;
    const forceGraphControls = true, attr = (element, name) => element.getAttribute(name) || '';
    const fitVisibleTopology = () => {}, restoreForceFocusLabels = () => {};
    const isViewportVisible = element => !element.classList.contains('cfx-topology-html-filter-hidden') && !element.classList.contains('cfx-topology-html-force-hidden');
    ${source}\nreturn { applyTopologyFilter, clearTopologyFilter, applyForceGraphFilters };`)(wrapper, forceGraphPanel, dom.window, dom.window.CustomEvent);
  const force = group => { forceGraphPanel.querySelector('[data-cfx-force-group]').value = group; api.applyForceGraphFilters(); };
  return { wrapper, api, force, close: () => dom.window.close() };
}
test('clearing host filters during force filtering does not retain hidden labels or groups', () => {
  const view = fixture();
  try {
    view.force('a'); view.api.clearTopologyFilter(); view.force('');
    for (const item of view.wrapper.querySelectorAll('[data-group-id="b"], [data-edge-id="b"]')) {
      assert.equal(item.classList.contains('cfx-topology-html-filter-hidden'), false);
      assert.equal(item.classList.contains('cfx-topology-html-force-hidden'), false);
    }
  } finally { view.close(); }
});
test('both filter entry points publish effective counts and retain the other filter state', () => {
  const view = fixture(); let latest, forceDetail;
  view.wrapper.addEventListener('cfx-topology-force-filter', event => { forceDetail = event.detail; });
  view.wrapper.addEventListener('cfx-topology-filter', event => { latest = event.detail; });
  try {
    view.api.applyTopologyFilter({ group: 'b' }); view.force('');
    assert.equal(latest.nodes, 2); assert.equal(latest.edges, 1); assert.equal(latest.active, true);
    assert.equal(latest.group, 'b');
    assert.equal(view.wrapper.querySelector('output').textContent, '2 nodes / 1 edges visible');
    view.force('a');
    assert.equal(forceDetail.group, 'a'); assert.equal(forceDetail.filters.topology.group, 'b');
    assert.equal(latest.nodes, 0); assert.equal(latest.edges, 0);
    view.api.clearTopologyFilter();
    assert.equal(latest.nodes, 2); assert.equal(latest.edges, 1); assert.equal(latest.group, 'a');
    view.force(''); assert.equal(latest.nodes, 4); assert.equal(latest.active, false);
  } finally { view.close(); }
});
