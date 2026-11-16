const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { installUiText } = require('../helpers/adapter-localization');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets/topology-interaction.source');
const lifetimeSource = fs.readFileSync(path.resolve(assets, '../adapter-lifetime.js'), 'utf8');

function fixture(edges, collapsed = false, clip = null) {
  const dom = new JSDOM(`<div id="chart"><svg>${clip ? `<clipPath id="clip"><rect x="0" y="0" width="400" height="${clip}"/></clipPath>` : ''}<g ${clip ? 'clip-path="url(#clip)"' : ''}>
    <g data-cfx-role="topology-node" data-node-id="card" data-node-label="Domain controllers" data-sibling-count="3"
       data-sibling-expanded="${collapsed ? 'false' : 'true'}" data-sibling-collapsed-height="36" data-sibling-expanded-height="110">
      <rect x="10" y="100" width="210" height="${collapsed ? 36 : 110}" data-cfx-role="topology-node-surface"/>
      <g data-cfx-role="topology-sibling-header" data-node-id="card"><path data-cfx-role="topology-sibling-toggle" d="M 0 0 L 1 1"/></g>
      <g data-cfx-role="topology-sibling-rows" data-node-id="card"${collapsed ? ' opacity="0"' : ''}>
        ${['dc1', 'dc2', 'dc3'].map(id => `<g data-cfx-role="topology-sibling-row" data-node-id="${id}"></g>`).join('')}
      </g>
    </g>
    <g data-cfx-role="topology-node" data-node-id="neighbour"></g>
    ${edges.map((edge, index) => `<g data-cfx-role="topology-edge" data-edge-id="e${index}" data-source-node-id="hub" data-target-node-id="card" data-route-end-y="${edge}"></g>`).join('')}
  </g></svg></div>`, { runScripts: 'outside-only' });
  const wrapper = dom.window.document.querySelector('#chart');
  dom.window.eval(lifetimeSource);
  installUiText(dom.window);
  const lifetime = dom.window[Symbol.for('ChartForgeX.adapterLifetimes')].create(wrapper, 'topology');
  const source = fs.readFileSync(path.join(assets, '68-sibling-cards.js'), 'utf8');
  new Function('wrapper', 'window', 'CustomEvent', 'lifetime', `
    const uiText = (key, fallback, values) => window[Symbol.for('ChartForgeX.uiText')](wrapper, key, fallback, values);
    const attr = (element, name) => element.getAttribute(name) || '';
    const listen = (target, name, listener, options) => lifetime.listen(target, name, listener, options);
    ${source}`)(wrapper, dom.window, dom.window.CustomEvent, lifetime);
  const card = wrapper.querySelector('[data-cfx-role="topology-node"]');
  return {
    window: dom.window, wrapper, card,
    header: card.querySelector('[data-cfx-role="topology-sibling-header"]'),
    surface: card.querySelector('[data-cfx-role="topology-node-surface"]'),
    rows: card.querySelector('[data-cfx-role="topology-sibling-rows"]'),
    close: () => { lifetime.dispose(); dom.window.close(); }
  };
}

test('a sibling card collapses to its header and expands back to its member rows', () => {
  const view = fixture([100]);
  const events = [];
  view.wrapper.addEventListener('cfx-topology-sibling-toggle', event => events.push(event.detail));
  try {
    assert.equal(view.header.getAttribute('role'), 'button');
    assert.equal(view.header.getAttribute('aria-expanded'), 'true');
    view.header.dispatchEvent(new view.window.MouseEvent('click', { bubbles: true }));
    assert.equal(view.card.getAttribute('data-sibling-expanded'), 'false');
    assert.equal(view.header.getAttribute('aria-expanded'), 'false');
    assert.equal(view.header.getAttribute('aria-label'), 'Expand Domain controllers');
    assert.equal(view.rows.style.display, 'none');
    assert.equal(view.surface.getAttribute('height'), '36');
    assert.equal(view.surface.getAttribute('y'), '100');
    view.header.dispatchEvent(new view.window.KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    assert.equal(view.card.getAttribute('data-sibling-expanded'), 'true');
    assert.equal(view.rows.style.display, '');
    assert.equal(view.surface.getAttribute('height'), '110');
    assert.deepEqual(events, [{ id: 'card', expanded: false }, { id: 'card', expanded: true }]);
  } finally { view.close(); }
});

test('a header click toggles without selecting the card', () => {
  const view = fixture([100]);
  let selected = 0;
  view.card.addEventListener('click', () => selected++);
  try {
    view.header.dispatchEvent(new view.window.MouseEvent('click', { bubbles: true }));
    assert.equal(selected, 0);
  } finally { view.close(); }
});

test('a collapsed card keeps every attached link end on its surface', () => {
  const side = fixture([160]);
  try {
    side.header.dispatchEvent(new side.window.MouseEvent('click', { bubbles: true }));
    assert.equal(side.surface.getAttribute('height'), '66');
  } finally { side.close(); }
  const bottom = fixture([210]);
  try {
    bottom.header.dispatchEvent(new bottom.window.MouseEvent('click', { bubbles: true }));
    assert.equal(bottom.surface.getAttribute('height'), '36');
    assert.equal(bottom.surface.getAttribute('y'), '174');
    assert.equal(bottom.header.getAttribute('transform'), 'translate(0 74)');
    bottom.header.dispatchEvent(new bottom.window.MouseEvent('click', { bubbles: true }));
    assert.equal(bottom.surface.getAttribute('y'), '100');
    assert.equal(bottom.surface.getAttribute('height'), '110');
    assert.equal(bottom.header.hasAttribute('transform'), false);
    bottom.header.dispatchEvent(new bottom.window.MouseEvent('click', { bubbles: true }));
    assert.equal(bottom.surface.getAttribute('y'), '174');
  } finally { bottom.close(); }
});

test('a card that starts collapsed opens over its neighbours on demand', () => {
  const view = fixture([100], true);
  try {
    assert.equal(view.header.getAttribute('aria-expanded'), 'false');
    assert.equal(view.rows.style.display, 'none');
    view.header.dispatchEvent(new view.window.MouseEvent('click', { bubbles: true }));
    assert.equal(view.surface.getAttribute('height'), '110');
    assert.equal(view.rows.hasAttribute('opacity'), false);
    assert.equal(view.rows.style.display, '');
    assert.equal(view.card.parentNode.lastElementChild, view.card);
  } finally { view.close(); }
});

test('an opened card near the bottom of the clip is lifted so every row stays visible', () => {
  const view = fixture([100], true, 180);
  try {
    view.header.dispatchEvent(new view.window.MouseEvent('click', { bubbles: true }));
    assert.equal(view.card.getAttribute('transform'), 'translate(0 -30)');
    view.header.dispatchEvent(new view.window.MouseEvent('click', { bubbles: true }));
    assert.equal(view.card.hasAttribute('transform'), false);
  } finally { view.close(); }
});
