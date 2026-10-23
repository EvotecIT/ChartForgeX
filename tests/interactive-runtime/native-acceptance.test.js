'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');

// Exercise public controllers, the complete source runtime and real template/DOM replacement.
// Only the native host's generated section and JSDOM's missing SVG measurement are boundaries.
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const runtime = fs.readFileSync(path.join(assets, 'adapter-localization.js'), 'utf8') + '\n'
  + fs.readdirSync(path.join(assets, 'interactive.source')).filter(name => name.endsWith('.js')).sort()
    .map(name => fs.readFileSync(path.join(assets, 'interactive.source', name), 'utf8')).join('\n');
const point = (x, y) => ({ x, y });
const note = (label = 'Note') => ({ kind: 'Note', start: point(4, 40), label, yAxis: 'Primary' });
const axis = (maximum = 10) => ({ minimum: 0, maximum, fullMinimum: 0, fullMaximum: maximum,
  transformedMinimum: 0, transformedMaximum: maximum, transformedFullMinimum: 0,
  transformedFullMaximum: maximum, scale: 'linear', threshold: 1, reversed: false,
  plotBounds: { x: 20, y: 20, width: 300, height: 200 } });
function section(window, annotations, options = {}) {
  const root = window.document.createElement('section'); root.className = 'cfx-interactive-chart';
  root.dataset.cfxChartId = 'native-acceptance';
  const x = axis(), y = axis(100);
  const metadata = { series: [], xLabels: [], regions: [], annotations: [], xProjection: x, yProjection: y,
    dataProjection: { plotBounds: x.plotBounds, horizontal: false, x, y } };
  if (options.missingAxis) delete metadata[options.missingAxis + 'Projection'];
  if (options.hierarchyRoot !== undefined) {
    metadata.hierarchy = { kind: 'treemap', seriesKey: 'work', rootId: options.hierarchyRoot, path: [] };
    root.dataset.cfxInteractionFeatures = 'HierarchyNavigation';
  }
  root.innerHTML = '<div class="cfx-stage"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 260"></svg></div><div class="cfx-tooltip" hidden></div>';
  const svg = root.querySelector('svg');
  for (const [id, drawing] of Object.entries(annotations)) {
    const sourceId = 'annotation-id-' + Buffer.from(id).toString('base64url');
    const node = window.document.createElementNS(svg.namespaceURI, 'g');
    Object.assign(node.dataset, { cfxRole: 'annotation', cfxAnnotationId: id, cfxSourceId: sourceId,
      cfxKind: drawing.kind, cfxLabel: drawing.label ?? '', cfxYAxis: drawing.yAxis ?? 'Primary',
      cfxSourceX: String(drawing.start?.x ?? 0), cfxSourceY: String(drawing.start?.y ?? 30),
      cfxSourceEndX: drawing.end ? String(drawing.end.x) : '', cfxSourceEndY: drawing.end ? String(drawing.end.y) : '',
      cfxChannelOffset: String(drawing.yOffset ?? 0) });
    svg.append(node); metadata.annotations.push({ id, sourceId, kind: drawing.kind });
    metadata.regions.push({ id: sourceId, role: 'annotation', x: 40, y: 40, width: 20, height: 20 });
  }
  if (metadata.hierarchy) {
    const node = window.document.createElementNS(svg.namespaceURI, 'g');
    Object.assign(node.dataset, { cfxNode: 'team', cfxTargetKind: 'node', cfxTargetId: 'team' }); svg.append(node);
  }
  root.dataset.cfxPreparedChart = JSON.stringify(metadata); return root;
}
function adaptive(window, wide, narrow, current = 'wide') {
  const root = (current === 'narrow' ? narrow : wide).cloneNode(true); root.dataset.cfxAdaptiveDrawing = current;
  for (const [name, drawing] of [['wide', wide], ['narrow', narrow]]) {
    drawing.dataset.cfxResponsiveLayout = 'adaptive'; drawing.dataset.cfxAdaptiveBreakpoint = '640';
    const template = window.document.createElement('template'); template.setAttribute('data-cfx-adaptive-' + name, 'true');
    template.content.append(drawing); root.append(template);
  }
  root.dataset.cfxResponsiveLayout = 'adaptive'; root.dataset.cfxAdaptiveBreakpoint = '640'; return root;
}
function fixture({ managed = true, adaptiveScene = false, hierarchy = false, referenceKind = 'HorizontalLine' } = {}) {
  const dom = new JSDOM('<!doctype html><body></body>', { runScripts: 'outside-only', pretendToBeVisual: true, url: 'https://example.test/' });
  const { window } = dom;
  window.SVGElement.prototype.getBBox = () => ({ x: 40, y: 40, width: 20, height: 20 });
  const model = { note: note('Original'), reference: { kind: referenceKind, label: 'Reference' } };
  const make = (annotations = model, options = {}) => section(window, annotations, { ...(hierarchy ? { hierarchyRoot: null } : {}), ...options });
  const root = adaptiveScene ? adaptive(window, make(), make(), 'narrow') : make();
  root.getBoundingClientRect = () => ({ x: 0, y: 0, left: 0, top: 0, right: 390, bottom: 260, width: 390, height: 260 });
  window.document.body.append(root); window.eval(runtime);
  const controller = window.ChartForgeXCharts.get(root), pending = [], changes = [], errors = [], commits = [];
  for (const channel of ['annotation', 'viewport', 'hierarchy']) {
    root.addEventListener('cfx' + channel + 'change', event => changes.push({ channel, detail: event.detail }));
    root.addEventListener('cfx' + channel + 'error', event => errors.push({ channel, detail: event.detail }));
  }
  root.addEventListener('cfxannotationcommit', event => commits.push(event.detail));
  const callback = (request, signal) => new Promise((resolve, reject) => pending.push({ request, signal, resolve, reject }));
  if (managed) controller.connectNative(callback, { basis: 'initial', axes: hierarchy ? null : ['x', 'y'], ...(hierarchy ? { hierarchy: true } : {}) });
  else { controller.connectAnnotations(callback); controller.connectViewport(callback, { axes: ['x', 'y'] }); }
  const editor = hierarchy ? null : controller.editAnnotations();
  const flush = async () => { for (let i = 0; i < 8; i++) await Promise.resolve(); };
  const resolve = async (html, basis = 'accepted') => {
    pending.at(-1).resolve(managed ? { html: typeof html === 'string' ? html : html.outerHTML, basis } : typeof html === 'string' ? html : html.outerHTML);
    await flush();
  };
  const accept = async (change, basis = 'accepted') => {
    controller.applyAnnotationChange(change); await flush();
    if (change.operation === 'remove') delete model[change.id]; else model[change.id] = change.drawing;
    await resolve(adaptiveScene ? adaptive(window, make(), make()) : make(), basis);
  };
  return { dom, window, root, controller, editor, pending, changes, errors, commits, model, make, flush, resolve, accept,
    close() { controller.dispose(); dom.window.close(); } };
}
for (const managed of [true, false]) {
  for (const operation of ['replace', 'remove']) {
    for (const kind of ['HorizontalLine', 'HorizontalBand', 'VerticalLine', 'VerticalBand']) {
      test(`${managed ? 'managed' : 'legacy'} ${operation} of ${kind} preserves native acceptance without inventing editor undo`, async () => {
        const f = fixture({ managed, referenceKind: kind });
        try {
          await f.accept({ operation: 'replace', id: 'note', drawing: note('Edited') }, 'first');
          assert.equal(f.editor.getState().canUndo, true);
          // Keep both a prior undo and redo entry before the nonrepresentable mutation.
          await f.accept({ operation: 'add', id: 'extra', drawing: note('Extra') }, 'second');
          f.editor.undo(); await f.flush(); delete f.model.extra; await f.resolve(f.make(), 'undo');
          assert.equal(f.editor.getState().canUndo, true); assert.equal(f.editor.getState().canRedo, true);
          const commitCount = f.commits.length;
          await f.accept({ operation, id: 'reference', ...(operation === 'replace' ? { drawing: note('Replacement') } : {}) }, 'reference');
          assert.equal(f.errors.length, 0); assert.equal(f.changes.length, 4);
          assert.equal(f.editor.getState().canUndo, false); assert.equal(f.editor.getState().canRedo, false);
          assert.equal(f.commits.length, commitCount, 'editor events must not describe an existing reference as absent');
          assert.equal(f.root.querySelector('[data-cfx-annotation-id=reference]')?.dataset.cfxKind, operation === 'replace' ? 'Note' : undefined);
          assert.throws(() => f.editor.undo(), /No accepted annotation edit/);
          // A subsequent representable add still supports genuine undo/redo.
          await f.accept({ operation: 'add', id: 'later', drawing: note('Later') }, 'later');
          assert.equal(f.editor.getState().canUndo, true); assert.equal(f.commits.at(-1).before, null);
        } finally { f.close(); }
      });
    }
  }
  test(`${managed ? 'managed' : 'legacy'} rejected reference mutation keeps earlier history and redo`, async () => {
    const f = fixture({ managed });
    try {
      await f.accept({ operation: 'replace', id: 'note', drawing: note('Edited') });
      f.editor.undo(); await f.flush(); f.model.note = note('Original'); await f.resolve(f.make());
      const before = JSON.stringify(f.editor.getState()), revision = f.controller.sceneRevision;
      f.controller.applyAnnotationChange({ operation: 'remove', id: 'reference' }); await f.flush();
      await f.resolve(f.make());
      assert.equal(f.errors.length, 1); assert.equal(f.changes.length, 2);
      assert.equal(f.controller.sceneRevision, revision); assert.equal(f.editor.getState().canRedo, true);
      assert.equal(f.editor.getState().canUndo, false); assert.equal(JSON.parse(before).canRedo, true);
    } finally { f.close(); }
  });
  for (const operation of ['add', 'replace', 'remove']) for (const staleDrawing of ['wide', 'narrow']) {
    test(`${managed ? 'managed' : 'legacy'} adaptive annotation ${operation} rejects stale ${staleDrawing} atomically`, async () => {
      const f = fixture({ managed, adaptiveScene: true });
      try {
        const change = { operation, id: operation === 'add' ? 'added' : 'note', ...(operation === 'remove' ? {} : { drawing: note('Requested') }) };
        const initial = f.root.querySelector('.cfx-stage').innerHTML, revision = f.controller.sceneRevision;
        f.controller.applyAnnotationChange(change); await f.flush();
        const updated = { ...f.model }; if (operation === 'remove') delete updated.note; else updated[change.id] = change.drawing;
        const stale = f.make(), valid = f.make(updated);
        await f.resolve(adaptive(f.window, staleDrawing === 'wide' ? stale : valid.cloneNode(true), staleDrawing === 'narrow' ? stale : valid.cloneNode(true)), 'rejected');
        assert.equal(f.errors.length, 1); assert.equal(f.changes.length, 0); assert.equal(f.commits.length, 0);
        assert.equal(f.controller.sceneRevision, revision); assert.equal(f.root.querySelector('.cfx-stage').innerHTML, initial);
        assert.equal(f.editor.getState().canUndo, false);
        f.controller.applyAnnotationChange(change); await f.flush();
        if (managed) assert.equal(f.pending.at(-1).request.basis, 'initial');
        await f.resolve(adaptive(f.window, f.make(updated), f.make(updated)), 'valid');
        assert.equal(f.changes.length, 1); assert.equal(f.editor.getState().canUndo, true);
      } finally { f.close(); }
    });
  }
}
for (const connection of ['managed', 'physical', 'legacyX']) for (const missingDrawing of ['wide', 'narrow']) {
  test(`${connection} viewport rejects missing projection in ${missingDrawing} before accepted basis or scene change`, async () => {
    const f = fixture({ adaptiveScene: true, managed: connection === 'managed' });
    try {
      // Reconnect before establishing history so this test observes only reply acceptance.
      if (connection === 'legacyX') {
        f.controller.connectXViewport((request, signal) => new Promise((resolve, reject) => f.pending.push({ request, signal, resolve, reject })));
      }
      const revision = f.controller.sceneRevision, initial = f.root.querySelector('.cfx-stage').innerHTML;
      const request = () => connection === 'legacyX' ? f.controller.setXWindow(2, 8) : f.controller.setViewport({ y: { minimum: 20, maximum: 80 } });
      request(); await f.flush();
      const missing = f.make(f.model, { missingAxis: connection === 'legacyX' ? 'x' : 'y' }), good = f.make();
      await f.resolve(adaptive(f.window, missingDrawing === 'wide' ? missing : good.cloneNode(true), missingDrawing === 'narrow' ? missing : good.cloneNode(true)), 'rejected');
      assert.equal(f.errors.length, 1); assert.equal(f.changes.length, 0); assert.equal(f.controller.sceneRevision, revision);
      assert.equal(f.root.querySelector('.cfx-stage').innerHTML, initial);
      request(); await f.flush(); if (connection === 'managed') assert.equal(f.pending.at(-1).request.basis, 'initial');
      await f.resolve(adaptive(f.window, f.make(), f.make()), 'valid'); assert.equal(f.changes.length, 1);
    } finally { f.close(); }
  });
}
for (const staleDrawing of ['wide', 'narrow']) test(`hierarchy keeps its existing alternate ${staleDrawing} root validation`, async () => {
  const f = fixture({ adaptiveScene: true, hierarchy: true });
  try {
    f.controller.setHierarchyView({ rootId: 'team' }); await f.flush();
    const good = f.make(f.model, { hierarchyRoot: 'team' }), stale = f.make();
    await f.resolve(adaptive(f.window, staleDrawing === 'wide' ? stale : good.cloneNode(true), staleDrawing === 'narrow' ? stale : good.cloneNode(true)), 'rejected');
    assert.equal(f.errors.length, 1); assert.equal(f.changes.length, 0);
    f.controller.setHierarchyView({ rootId: 'team' }); await f.flush(); assert.equal(f.pending.at(-1).request.basis, 'initial');
    await f.resolve(adaptive(f.window, f.make(f.model, { hierarchyRoot: 'team' }), f.make(f.model, { hierarchyRoot: 'team' })), 'valid');
    assert.equal(f.changes.length, 1); assert.equal(f.controller.getHierarchyView().rootId, 'team');
  } finally { f.close(); }
});

for (const managed of [true, false]) test(`${managed ? 'managed' : 'legacy'} accepted reference removal also invalidates a disabled editor's retained history`, async () => {
  const f = fixture({ managed });
  try {
    await f.accept({ operation: 'replace', id: 'note', drawing: note('Edited') }, 'first');
    await f.accept({ operation: 'add', id: 'extra', drawing: note('Extra') }, 'second');
    f.editor.undo(); await f.flush(); delete f.model.extra; await f.resolve(f.make(), 'undo');
    assert.equal(f.editor.getState().canUndo, true); assert.equal(f.editor.getState().canRedo, true);
    f.editor.disable(); const commits = f.commits.length;
    // Supported commands while disabled retain the existing history without recording an editor command.
    await f.accept({ operation: 'add', id: 'disabled-added', drawing: note('Supported while disabled') }, 'disabled-supported');
    assert.equal(f.editor.getState().canUndo, true); assert.equal(f.editor.getState().canRedo, true);
    assert.equal(f.commits.length, commits);
    await f.accept({ operation: 'remove', id: 'reference' }, 'disabled-reference');
    assert.equal(f.changes.length, 5); assert.equal(f.commits.length, commits);
    assert.equal(f.editor.getState().enabled, false);
    assert.equal(f.editor.getState().canUndo, false); assert.equal(f.editor.getState().canRedo, false);
    f.controller.editAnnotations();
    assert.equal(f.editor.getState().canUndo, false); assert.equal(f.editor.getState().canRedo, false);
  } finally { f.close(); }
});
