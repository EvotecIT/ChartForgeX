'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');

// Exercise public controllers and real template/DOM replacement through the shipped asset.
// Only the native host's generated section and JSDOM's missing SVG measurement are boundaries.
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const runtime = ['adapter-native-text-export.js', 'interactive.js'].map(name => fs.readFileSync(path.join(assets, name), 'utf8')).join('\n');
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

test('the shipped runtime preserves public controller keys and native diagnostics', () => {
  const f = fixture();
  try {
    const keys = ['root', 'sceneRevision', 'select', 'clearSelection', 'clearBrush', 'getSelection',
      'resolveTargets', 'highlight', 'clearHighlight', 'unpinTooltip', 'update', 'presentInitial', 'connectNative',
      'connectAnnotations', 'applyAnnotationChange', 'editAnnotations', 'connectXViewport',
      'connectViewport', 'connectPolarViewport', 'getPolarViewport', 'setPolarViewport',
      'setProgressValue', 'getHierarchyView', 'setHierarchyView', 'getViewport', 'setViewport',
      'getYWindow', 'setYWindow', 'getXWindow', 'setXWindow', 'resize', 'dispose'];
    assert.deepEqual(Object.keys(f.controller).sort(), keys.sort());
    assert.equal(Object.isFrozen(f.controller), true);
    const revision = f.controller.sceneRevision;
    assert.throws(() => f.controller.connectNative(1, { basis: 'invalid' }), /Native callback: function\|null required\./);
    assert.throws(() => f.controller.connectNative(() => {}, { basis: '' }), /Native basis: nonempty string\./);
    assert.equal(f.controller.sceneRevision, revision, 'invalid connections must not install a scene');
  } finally { f.close(); }
});
test('native invisible source facts and their descendants never become public interaction targets', () => {
  const dom = new JSDOM('<!doctype html><body></body>', { runScripts: 'outside-only', pretendToBeVisual: true, url: 'https://example.test/' });
  const { window } = dom;
  window.SVGElement.prototype.getBBox = () => ({ x: 40, y: 40, width: 20, height: 20 });
  const root = section(window, {});
  root.dataset.cfxInteractionFeatures = 'Selection,KeyboardNavigation,Tooltips';
  const svg = root.querySelector('svg');
  svg.innerHTML = '<g data-cfx-visible="false"><g data-cfx-target-kind="point" data-cfx-target-id="nested" data-cfx-label="Nested source" /></g>'
    + '<g data-cfx-visible="false" data-cfx-target-kind="point" data-cfx-target-id="hidden" data-cfx-label="Hidden source" />'
    + '<g data-cfx-visible="true" data-cfx-target-kind="point" data-cfx-target-id="visible" data-cfx-label="Visible source" />';
  window.document.body.append(root); window.eval(runtime);
  const controller = window.ChartForgeXCharts.get(root);
  try {
    assert.deepEqual(Array.from(controller.resolveTargets(), target => target.targetId), ['visible']);
    for (const id of ['nested', 'hidden']) {
      const node = svg.querySelector('[data-cfx-target-id="' + id + '"]');
      assert.equal(node.hasAttribute('tabindex'), false);
      assert.notEqual(node.getAttribute('role'), 'button');
      assert.equal(controller.resolveTargets({ targets: [{ targetKind: 'point', targetId: id }] }).length, 0);
    }
    assert.equal(svg.querySelector('[data-cfx-target-id="visible"]').hasAttribute('tabindex'), true);
    assert.equal(svg.querySelectorAll('[data-cfx-visible="false"]').length, 2, 'complete hidden source facts remain in the native drawing');
  } finally { controller.dispose(); dom.window.close(); }
});
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

// Public commands run the shipped asset; only browser time and generated-section inputs are boundaries.
function scenarioFixture({ loop = false, autoPlay = false, reducedMotion = false,
  url = 'https://example.test/?keep=host', peers = 1, legendPeers = false, routeSteps = null } = {}) {
  const dom = new JSDOM('<!doctype html><body></body>', { runScripts: 'outside-only', pretendToBeVisual: true, url });
  const { window } = dom, timers = new Map(), events = [], errors = [];
  let now = 0, sequence = 0;
  window.setTimeout = (callback, delay = 0, ...args) => {
    const id = ++sequence; timers.set(id, { at: now + Number(delay), callback: () => callback(...args) }); return id;
  };
  window.clearTimeout = id => timers.delete(id);
  window.matchMedia = query => ({ matches: reducedMotion && query === '(prefers-reduced-motion: reduce)',
    media: query, addEventListener() {}, removeEventListener() {} });
  window.SVGElement.prototype.getBBox = () => ({ x: 40, y: 40, width: 20, height: 20 });
  window.addEventListener('error', event => errors.push(event.message));
  const roots = Array.from({ length: peers }, (_, index) => {
    const root = section(window, {}), keys = index === 2 ? ['unrelated', 'other'] : ['readings', 'reference'];
    root.dataset.cfxChartId = 'plot-' + String.fromCharCode(97 + index);
    root.dataset.cfxInteractionGroup = 'review'; root.dataset.cfxDeepLinkState = 'true';
    root.dataset.cfxInteractionFeatures = 'ReportReview,Scenarios,StepPlayback,StateBookmarks,SynchronizedCharts';
    root.dataset.cfxActiveScenario = index === 1 ? 'plain' : 'review';
    root.getBoundingClientRect = () => ({ x: 0, y: 0, left: 0, top: 0, right: 390, bottom: 260, width: 390, height: 260 });
    const steps = [{ targetKind: 'series', targetId: keys[0], label: 'Reading', durationMilliseconds: 450 },
      { targetKind: 'series', targetId: keys[1], label: 'Reference' }];
    for (const id of ['review', 'plain']) {
      const button = window.document.createElement('button'); button.textContent = id;
      Object.assign(button.dataset, { cfxScenario: id, cfxScenarioLabel: id, cfxScenarioSteps: JSON.stringify(routeSteps || (id === 'plain' ? steps.slice(0, 1) : steps)),
        cfxScenarioPlaybackDelay: '700', cfxScenarioLoop: String(loop), cfxScenarioAutoplay: String(autoPlay && id === 'review'),
        cfxScenarioFocusMode: id === 'review' ? 'spotlight' : 'highlight', ...(id === 'review' ? { cfxScenarioColor: '#2563EB' } : {}) });
      root.append(button);
    }
    root.insertAdjacentHTML('beforeend', '<div data-cfx-scenario-panel><span data-cfx-scenario-panel-title></span>'
      + '<span data-cfx-scenario-panel-meta></span><ol data-cfx-scenario-panel-steps></ol></div>'
      + '<div data-cfx-scenario-progress><span data-cfx-scenario-progress-text></span></div>'
      + '<button data-cfx-scenario-step-control="play">Play</button><button data-cfx-scenario-step-control="link">Link</button>');
    const svg = root.querySelector('svg');
    for (const [ordinal, key] of keys.entries()) {
      const node = window.document.createElementNS(svg.namespaceURI, 'g');
      Object.assign(node.dataset, { cfxRole: 'series', cfxSeries: String(ordinal), cfxSeriesKey: key, cfxLabel: key }); svg.append(node);
      for (const [rendered, original] of [[0, 12], [1, 28]]) {
        const observation = window.document.createElementNS(svg.namespaceURI, 'g');
        Object.assign(observation.dataset, { cfxRole: 'point', cfxSeries: String(ordinal), cfxSeriesKey: key,
          cfxPoint: String(rendered), cfxSourcePoint: String(original), cfxLabel: key + '-' + rendered }); node.append(observation);
      }
      if (legendPeers && index !== 1) {
        const legend = window.document.createElementNS(svg.namespaceURI, 'g');
        Object.assign(legend.dataset, { cfxRole: 'legend-item', cfxSeries: String(ordinal), cfxSeriesKey: key, cfxLabel: key });
        const swatch = window.document.createElementNS(svg.namespaceURI, 'rect');
        for (const [name, value] of Object.entries({ x: 20 + ordinal * 40, y: 20, width: 12, height: 12, fill: '#2864B4' }))
          swatch.setAttribute(name, String(value));
        legend.append(swatch); svg.append(legend);
      }
    }
    const metadata = JSON.parse(root.dataset.cfxPreparedChart);
    metadata.series = keys.map(key => ({ key, name: key, local: false, kind: 'Line', state: 'None', indices: [12, 28] }));
    root.dataset.cfxPreparedChart = JSON.stringify(metadata);
    for (const name of ['cfxscenario', 'cfxscenariostep', 'cfxscenarioplayback', 'cfxscenariolink', 'cfxstate', 'cfxstateapplied', 'cfxsync', 'cfxseries', 'cfxseriesfocus'])
      root.addEventListener(name, event => events.push({ root: index, name, detail: event.detail, bubbles: event.bubbles }));
    window.document.body.append(root); return root;
  });
  window.eval(runtime);
  const controllers = roots.map(root => window.ChartForgeXCharts.get(root));
  const command = (name, detail = {}, index = 0) => roots[index].dispatchEvent(new window.CustomEvent(name, { detail }));
  const advance = milliseconds => {
    const end = now + milliseconds; let calls = 0;
    for (;;) {
      const next = [...timers.entries()].filter(([, timer]) => timer.at <= end).sort((a, b) => a[1].at - b[1].at || a[0] - b[0])[0];
      if (!next) break;
      assert.ok(++calls <= 100, 'a finite clock advance must not spin');
      timers.delete(next[0]); now = next[1].at; next[1].callback();
    }
    now = end;
  };
  return { window, roots, events, errors, command, advance,
    close() { controllers.forEach(controller => controller.dispose()); dom.window.close(); } };
}

test('shipped scenario commands honor per-step duration and explicit looping without real-time sleeps', () => {
  const f = scenarioFixture({ loop: true }), root = f.roots[0];
  try {
    f.command('cfx-play-scenario');
    assert.equal(root.dataset.cfxActiveScenarioStep, '0'); assert.equal(root.dataset.cfxScenarioPlayback, 'playing');
    assert.equal(root.querySelector('[data-cfx-scenario-step-control=play]').textContent, 'Pause');
    f.advance(449); assert.equal(root.dataset.cfxActiveScenarioStep, '0');
    f.advance(1); assert.equal(root.dataset.cfxActiveScenarioStep, '1'); assert.equal(root.dataset.cfxScenarioProgress, '2/2');
    f.advance(699); assert.equal(root.dataset.cfxActiveScenarioStep, '1');
    f.advance(1); assert.equal(root.dataset.cfxActiveScenarioStep, '0');
    assert.deepEqual(f.events.filter(event => event.name === 'cfxscenariostep').map(event => event.detail.index), [0, 1, 0]);
    assert.deepEqual(f.errors, []);
  } finally { f.close(); }
});
for (const loop of [false, true]) test(`shipped playing bookmark holds its captured final step before ${loop ? 'looping' : 'finishing'}`, () => {
  const f = scenarioFixture({ loop }), root = f.roots[0];
  try {
    f.command('cfx-play-scenario'); f.advance(450);
    f.command('cfx-capture-state', { source: 'review-bookmark', sync: false });
    const snapshot = f.events.find(event => event.name === 'cfxstate').detail.snapshot;
    assert.equal(snapshot.scenario.stepIndex, '1'); assert.equal(snapshot.scenario.playback, 'playing');
    f.command('cfx-pause-scenario'); f.command('cfx-set-scenario-step', { index: 0 });
    const before = f.events.filter(event => event.name === 'cfxscenariostep').length;
    f.command('cfx-apply-state', { snapshot, sync: false });
    assert.equal(root.dataset.cfxActiveScenarioStep, '1'); assert.equal(root.dataset.cfxScenarioPlayback, 'playing');
    assert.equal(f.events.filter(event => event.name === 'cfxscenariostep').length, before, 'restore must not immediately advance playback');
    assert.equal(f.events.filter(event => event.name === 'cfxstateapplied').length, 1);
    f.advance(699); assert.equal(root.dataset.cfxActiveScenarioStep, '1');
    f.advance(1);
    assert.equal(root.dataset.cfxActiveScenarioStep, loop ? '0' : '1');
    assert.equal(root.dataset.cfxScenarioPlayback, loop ? 'playing' : 'finished');
    assert.deepEqual(f.errors, []);
  } finally { f.close(); }
});
for (const reducedMotion of [false, true]) test(`shipped autoplay ${reducedMotion ? 'respects reduced motion' : 'starts the first step when allowed'}`, () => {
  const f = scenarioFixture({ autoPlay: true, reducedMotion }), root = f.roots[0];
  try {
    assert.equal(root.dataset.cfxScenarioPlayback, reducedMotion ? 'idle' : 'playing');
    assert.equal(root.dataset.cfxActiveScenarioStep, reducedMotion ? undefined : '0');
    f.advance(450); assert.equal(root.dataset.cfxActiveScenarioStep, reducedMotion ? undefined : '1');
    assert.deepEqual(f.errors, []);
  } finally { f.close(); }
});
for (const scoped of [false, true]) test(`shipped ${scoped ? 'chart-scoped' : 'legacy'} scenario links restore the captured step before URL normalization`, () => {
  const query = scoped ? 'cfx-plot-a-scenario=review&cfx-plot-a-scenarioStep=0&scenario=plain&scenarioStep=1' : 'scenario=review&scenarioStep=1';
  const f = scenarioFixture({ autoPlay: true, url: 'https://example.test/?keep=host&cfx-other-scenario=untouched&' + query }), root = f.roots[0];
  try {
    const step = scoped ? '0' : '1', url = new f.window.URL(f.window.location.href);
    assert.equal(root.dataset.cfxActiveScenario, 'review'); assert.equal(root.dataset.cfxActiveScenarioStep, step);
    assert.equal(root.dataset.cfxScenarioPlayback, 'idle', 'an explicit restored step must not autoplay');
    assert.equal(url.searchParams.get('cfx-plot-a-scenario'), 'review'); assert.equal(url.searchParams.get('cfx-plot-a-scenarioStep'), step);
    assert.equal(url.searchParams.has('scenario'), false); assert.equal(url.searchParams.has('scenarioStep'), false);
    assert.equal(url.searchParams.get('keep'), 'host'); assert.equal(url.searchParams.get('cfx-other-scenario'), 'untouched');
    root.querySelector('[data-cfx-scenario-step-control=link]').click();
    assert.equal(f.events.filter(event => event.name === 'cfxscenariolink').at(-1).detail.url, f.window.location.href);
    assert.deepEqual(f.errors, []);
  } finally { f.close(); }
});
test('shipped scenario switching stops playback, publishes idle and clears previous route color', () => {
  const f = scenarioFixture(), root = f.roots[0];
  try {
    assert.equal(root.style.getPropertyValue('--cfx-scenario-color'), '#2563EB');
    f.command('cfx-play-scenario'); f.command('cfx-set-scenario', { scenarioId: 'plain' });
    assert.equal(root.dataset.cfxScenarioPlayback, 'idle'); assert.equal(root.dataset.cfxActiveScenario, 'plain');
    assert.equal(root.style.getPropertyValue('--cfx-scenario-color'), '');
    assert.equal(root.querySelector('[data-cfx-scenario-panel]').style.getPropertyValue('--cfx-scenario-color'), '');
    assert.equal(root.querySelector('[data-cfx-scenario-step-control=play]').textContent, 'Play');
    assert.equal(root.querySelectorAll('.cfx-scenario-muted,.cfx-scenario-step-muted').length, 0, 'highlight routes must not inherit spotlight muting');
    assert.ok(f.events.some(event => event.name === 'cfxscenarioplayback' && event.detail.state === 'idle'));
    f.advance(5000); assert.equal(root.dataset.cfxActiveScenarioStep, undefined);
    assert.deepEqual(f.errors, []);
  } finally { f.close(); }
});
test('shipped dashboard scenario-step sync resolves its route before the step and never echoes from the peer', () => {
  const f = scenarioFixture({ peers: 2 }), [source, peer] = f.roots;
  try {
    assert.equal(source.dataset.cfxActiveScenario, 'review'); assert.equal(peer.dataset.cfxActiveScenario, 'plain');
    f.command('cfx-set-scenario-step', { index: 1 });
    assert.equal(peer.dataset.cfxActiveScenario, 'review'); assert.equal(peer.dataset.cfxActiveScenarioStep, '1');
    assert.equal(peer.dataset.cfxScenarioProgress, '2/2');
    assert.ok(peer.querySelector('[data-cfx-series-key=reference]').classList.contains('cfx-scenario-step-active'));
    assert.equal(f.events.filter(event => event.name === 'cfxsync' && event.root === 0 && event.detail.action === 'scenario-step').length, 1);
    assert.equal(f.events.filter(event => event.name === 'cfxsync' && event.root === 1).length, 0);
    assert.equal(f.events.filter(event => event.name === 'cfxscenariostep' && event.root === 1).length, 0);
    f.command('cfx-set-scenario', { scenarioId: 'plain' });
    assert.equal(peer.dataset.cfxActiveScenario, 'plain'); assert.equal(peer.dataset.cfxActiveScenarioStep, undefined);
    assert.equal(f.events.filter(event => event.name === 'cfxsync' && event.root === 1).length, 0);
    assert.deepEqual(f.errors, []);
  } finally { f.close(); }
});
test('shipped ReportReview legends synchronize with legend-free peers without clearing unrelated isolation', () => {
  const f = scenarioFixture({ peers: 3, legendPeers: true }), [source, peer, unrelated] = f.roots;
  const key = (root, name, value) => root.querySelector('[data-cfx-role=legend-item]').dispatchEvent(new f.window.KeyboardEvent(name, { key: value, bubbles: true }));
  try {
    assert.equal(peer.querySelectorAll('[data-cfx-role=legend-item]').length, 0);
    key(unrelated, 'keydown', 'i');
    const retained = unrelated.dataset.cfxIsolatedSeries;
    assert.ok(retained);
    key(source, 'keydown', ' ');
    assert.ok(peer.querySelector('[data-cfx-series-key=readings]').classList.contains('cfx-series-muted'));
    assert.equal(peer.querySelector('[data-cfx-series-key=reference]').classList.contains('cfx-series-muted'), false);
    key(source, 'keydown', 'i');
    assert.ok(peer.querySelector('[data-cfx-series-key=readings]').classList.contains('cfx-series-isolated-in'));
    assert.ok(peer.querySelector('[data-cfx-series-key=reference]').classList.contains('cfx-series-isolated-out'));
    assert.equal(unrelated.dataset.cfxIsolatedSeries, retained);
    assert.ok(unrelated.querySelector('[data-cfx-role=series][data-cfx-series-key=unrelated]').classList.contains('cfx-series-isolated-in'));
    assert.equal(f.events.filter(event => event.name === 'cfxsync' && event.root === 1).length, 0);
    assert.ok(f.events.some(event => event.name === 'cfxseries' && event.root === 0 && event.bubbles));
    assert.ok(f.events.some(event => event.name === 'cfxseriesfocus' && event.root === 0 && event.bubbles));
    assert.deepEqual(f.errors, []);
  } finally { f.close(); }
});

test('shipped scenario targets prefer typed source identity while retaining documented local point ordinals', () => {
  const f = scenarioFixture({ routeSteps: [{ targetKind: 'point', targetId: '1', label: 'Legacy local ordinal' },
    { targetKind: 'point', targetId: 'readings:12', label: 'Typed source identity' }] }), root = f.roots[0];
  try {
    f.command('cfx-set-scenario-step', { index: 0 });
    const legacy = [...root.querySelectorAll('.cfx-scenario-step-active')];
    assert.equal(legacy.length, 2); assert.ok(legacy.every(node => node.dataset.cfxPoint === '1' && node.dataset.cfxSourcePoint === '28'));
    f.command('cfx-set-scenario-step', { index: 1 });
    const typed = [...root.querySelectorAll('.cfx-scenario-step-active')];
    assert.equal(typed.length, 1); assert.equal(typed[0].dataset.cfxSeriesKey, 'readings');
    assert.equal(typed[0].dataset.cfxPoint, '0'); assert.equal(typed[0].dataset.cfxSourcePoint, '12');
    assert.deepEqual(f.errors, []);
  } finally { f.close(); }
});
