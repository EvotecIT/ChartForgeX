'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { fixture } = require('./native-value-fixture');
const { flush } = require('./native-reflow-fixture');
// Synthetic routing/validation only. Actual native subtree membership and ink are owned by C#/browser proof.
function setup() {
  const f = fixture('hierarchy-value');
  const catalog = [{ kind: 'series', seriesKey: 'current' }, { kind: 'hierarchyItem', seriesKey: 'current', nodeId: 'root' }, { kind: 'hierarchyItem', seriesKey: 'current', nodeId: 'value' }];
  function make(rootId = null, hiddenId = null) {
    const root = f.make(20), svg = root.querySelector('svg'), owner = svg.querySelector('[data-cfx-role=hierarchy-series]'), leaf = svg.querySelector('[data-cfx-node=value]');
    root.insertAdjacentHTML('afterbegin', '<button class="cfx-reset" type="button" data-cfx-reset="true" hidden>Reset</button>');
    root.dataset.cfxInteractionFeatures += ',LegendToggles,HierarchyNavigation,Export';
    const parent = leaf.cloneNode(true); parent.dataset.cfxNode = 'root'; parent.dataset.cfxRole = 'treemap-group'; parent.dataset.cfxLeaf = 'false'; parent.dataset.cfxValue = '100'; parent.dataset.cfxAuthoredValue = '100';
    leaf.replaceWith(parent); parent.appendChild(leaf); leaf.dataset.cfxRole = 'treemap-tile'; leaf.dataset.cfxParent = 'root';
    parent.id = 'synthetic-hierarchy-view-node-1'; parent.dataset.cfxSourceId = 'node-root'; leaf.id = 'synthetic-hierarchy-view-node-3'; leaf.dataset.cfxSourceId = 'node-value';
    for (const node of [parent, leaf]) { if (rootId !== null) node.dataset.cfxViewRoot = rootId;
      if (hiddenId === 'root' || hiddenId === node.dataset.cfxNode || rootId === 'value' && node === parent) { node.dataset.cfxVisible = 'false'; node.dataset.cfxPassive = 'true'; node.dataset.cfxGeometryStatus = 'hidden'; } }
    const legend = svg.ownerDocument.createElementNS(svg.namespaceURI, 'g'); Object.assign(legend.dataset, { cfxRole: 'legend-entry', cfxSourceId: 'legend-node-value', cfxSeries: '0', cfxSeriesKey: 'current', cfxLegendTargetKind: 'node', cfxLegendTargetId: 'value', cfxLabel: 'Value' });
    legend.appendChild(leaf.querySelector('rect').cloneNode(true)); if (hiddenId) legend.dataset.cfxTargetHidden = 'true'; if (hiddenId === 'root') legend.dataset.cfxTargetBlocked = 'true'; svg.appendChild(legend);
    const metadata = JSON.parse(root.dataset.cfxPreparedChart); metadata.series[0].kind = 'treemap'; metadata.series[0].indices = [];
    metadata.hierarchy = { kind: 'treemap', seriesKey: 'current', rootId, path: rootId === null ? [] : [{ id: 'root', label: 'Root' }, { id: rootId, label: 'Value' }] };
    metadata.visibility = { layout: 'reflow', sourceKey: 'a'.repeat(64), labelLayering: 'preserveOverlay', hiddenSeries: [], targetCatalog: catalog, hiddenTargets: hiddenId ? [catalog.find(row => row.nodeId === hiddenId)] : [], hierarchyRootId: rootId };
    root.dataset.cfxPreparedChart = JSON.stringify(metadata); return root;
  }
  function capture(fromRoot = null, toRoot = 'value', fromHidden = null, toHidden = null) {
    const source = make(fromRoot, fromHidden), destination = make(toRoot, toHidden);
    const frames = [1, .5, 0, .5, 1].map((opacity, index) => {
      const basis = index < 2 ? 'source' : 'destination', svg = (basis === 'source' ? source : destination).querySelector('svg').cloneNode(true), prefix = 'synthetic-hierarchy-reflow-' + index;
      if (opacity < 1) { const wrapper = svg.ownerDocument.createElementNS(svg.namespaceURI, 'g'); wrapper.dataset.cfxRole = 'native-transition-opacity'; wrapper.setAttribute('opacity', String(opacity)); while (svg.firstChild) wrapper.appendChild(svg.firstChild); svg.appendChild(wrapper); }
      for (const node of svg.querySelectorAll('[id]')) { const match = /-node-(\d+)$/.exec(node.id); node.id = prefix + '-node-' + (Number(match[1]) + (opacity < 1 ? 1 : 0)); }
      return { basis, nativeOpacity: String(opacity), prefix, svg: svg.outerHTML };
    });
    return { kind: 'native-transition', effect: 'legend-visibility', sourceKind: 'native-visibility-reflow', destination: destination.outerHTML,
      visibility: { layout: 'reflow', labelLayering: 'preserveOverlay', targetCatalog: catalog, fromHiddenTargets: fromHidden ? [catalog.find(row => row.nodeId === fromHidden)] : [], toHiddenTargets: toHidden ? [catalog.find(row => row.nodeId === toHidden)] : [], fromHierarchyRootId: fromRoot, toHierarchyRootId: toRoot },
      drawings: { wide: { capturePngText: false, sourcePrefix: 'synthetic-hierarchy-view', destinationPrefix: 'synthetic-hierarchy-view', frames } } };
  }
  f.controller.update(make()); f.changes.length = 0; return { ...f, makeView: make, capture };
}
function save(f, name, payload, error) {
  const output = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!output) return;
  fs.mkdirSync(output, { recursive: true }); fs.writeFileSync(path.join(output, 'hierarchy-reflow-' + name + '.facts.json'), JSON.stringify({ revision: f.controller.sceneRevision, changes: f.changes, payload, error: error?.message }, null, 2));
  fs.writeFileSync(path.join(output, 'hierarchy-reflow-' + name + '.transport.html'), f.root.outerHTML);
}
test('root-only reflow has one accepted destination and source/destination-qualified transient frames', () => {
  const f = setup(); try { const payload = f.capture(); const revision = f.controller.sceneRevision;
    f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); save(f, 'root', payload);
    assert.notEqual(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 1); assert.equal(JSON.parse(f.root.dataset.cfxPreparedChart).visibility.hierarchyRootId, 'value'); assert.equal(f.root.querySelectorAll('.cfx-native-motion-frame').length, 1);
  } finally { f.close(); }
});
const invalid = {
  unknownRoot: payload => { payload.visibility.toHierarchyRootId = 'foreign'; },
  oldRootBasis: payload => { payload.visibility.fromHierarchyRootId = 'value'; },
  nodeCatalog: payload => { payload.visibility.targetCatalog[1] = { kind: 'observation', seriesKey: 'current', observationId: 'root' }; },
  frameNode: payload => { payload.drawings.wide.frames[1].svg = payload.drawings.wide.frames[1].svg.replace('data-cfx-node="value"', 'data-cfx-node="foreign"'); },
  frameRoot: payload => { payload.drawings.wide.frames[3].svg = payload.drawings.wide.frames[3].svg.replaceAll('data-cfx-view-root="value"', 'data-cfx-view-root="root"'); },
  unknownBasis: payload => { payload.drawings.wide.frames[2].basis = 'foreign'; }
};
for (const [name, mutate] of Object.entries(invalid)) test('hierarchy all-frame validation rejects ' + name + ' before commit', () => {
  const f = setup(); try { const payload = f.capture(), original = f.root.outerHTML, revision = f.controller.sceneRevision; mutate(payload); let error;
    try { f.controller.update(payload.destination, { sceneRevision: revision, transition: JSON.stringify(payload), motion: { duration: 1000 } }); } catch (failure) { error = failure; }
    save(f, name, payload, error); assert.ok(error instanceof f.window.TypeError); assert.equal(f.root.outerHTML, original); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
  } finally { f.close(); }
});
test('hierarchy native legend emits nodeId and blocks an ancestor-hidden descendant without a request', async () => {
  const f = setup(); try { const calls = []; f.controller.connectNative((request, signal) => new Promise(resolve => calls.push({ request, signal, resolve })), { basis: 'source', axes: null, hierarchy: true, legend: true });
    const item = () => f.root.querySelector('[data-cfx-role=legend-item]'); item().dispatchEvent(new f.window.MouseEvent('click', { bubbles: true })); await flush(); save(f, 'legend', calls.map(row => row.request));
    assert.deepEqual(JSON.parse(JSON.stringify(calls[0].request.sourceVisibility.hiddenTargets)), [{ kind: 'hierarchyItem', seriesKey: 'current', nodeId: 'value' }]);
    f.controller.connectNative(null); f.controller.update(f.makeView(null, 'root')); f.controller.connectNative((request, signal) => new Promise(resolve => calls.push({ request, signal, resolve })), { basis: 'hidden-root', axes: null, hierarchy: true, legend: true });
    item().dispatchEvent(new f.window.MouseEvent('click', { bubbles: true })); await flush(); save(f, 'ancestor-block', calls.map(row => row.request)); assert.equal(item().getAttribute('aria-disabled'), 'true'); assert.equal(calls.length, 1);
  } finally { f.close(); }
});
test('root request and queued reset retain absolute visibility basis across ignored abort, reconnect and disposal', async () => {
  const f = setup(); try { const calls = [], callback = (request, signal) => new Promise(resolve => calls.push({ request, signal, resolve }));
    f.controller.connectNative(callback, { basis: 'source', axes: null, hierarchy: true, legend: true, preparationBackpressure: 'latest-window' });
    f.controller.setHierarchyView({ rootId: 'value' }); await flush(); f.root.querySelector('[data-cfx-reset]').click(); await flush(); save(f, 'queued', calls.map(row => row.request));
    assert.equal(calls.length, 1); assert.equal(calls[0].request.sourceVisibility.hierarchyRootId, 'value'); assert.equal(calls[0].signal.aborted, true);
    calls[0].resolve({ html: f.makeView('value').outerHTML, basis: 'ignored' }); await flush(); save(f, 'admitted', calls.map(row => row.request));
    assert.equal(calls.length, 2); assert.equal(calls[1].request.kind, 'reset'); assert.equal(calls[1].request.channel, 'hierarchy'); assert.equal(calls[1].request.basis, 'source'); assert.equal(calls[1].request.sourceVisibility.hierarchyRootId, null); assert.equal(calls[1].request.sourceVisibility.hiddenTargets.length, 0);
    f.controller.connectNative(null); f.controller.connectNative(callback, { basis: 'new-source', axes: null, hierarchy: true, legend: true, preparationBackpressure: 'latest-window' });
    const revision = f.controller.sceneRevision; f.controller.dispose(); calls[1].resolve({ html: f.makeView().outerHTML, basis: 'old' }); await flush(); save(f, 'disposed', calls.map(row => row.request)); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
  } finally { f.close(); }
});
