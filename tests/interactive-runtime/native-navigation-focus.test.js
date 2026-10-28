'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const runtime = ['adapter-native-text-export.js', 'interactive.js'].map(name => fs.readFileSync(path.join(assets, name), 'utf8')).join('\n');
const axis = (minimum = 0, maximum = 10) => ({ minimum, maximum, fullMinimum: 0, fullMaximum: 10,
  transformedMinimum: minimum, transformedMaximum: maximum, transformedFullMinimum: 0, transformedFullMaximum: 10,
  scale: 'linear', threshold: 1, reversed: false, plotBounds: { x: 20, y: 20, width: 300, height: 200 } });
function fixture(channel = 'hierarchy', adaptiveScene = false) {
  const dom = new JSDOM('<!doctype html><body><button id="elsewhere">Elsewhere</button><div id="outside">Outside</div></body>',
    { runScripts: 'outside-only', pretendToBeVisual: true, url: 'https://example.test/' });
  const { window } = dom, { document } = window;
  document.body.tabIndex = -1;
  window.SVGElement.prototype.getBBox = () => ({ x: 40, y: 40, width: 20, height: 20 });
  // JSDOM does not perform the focused-button blur caused by disabled=true.
  // Keep the public runtime/DOM path, supplying only that missing browser behavior.
  const disabled = Object.getOwnPropertyDescriptor(window.HTMLButtonElement.prototype, 'disabled');
  Object.defineProperty(window.HTMLButtonElement.prototype, 'disabled', { ...disabled, set(value) {
    disabled.set.call(this, value);
    if (value && this.getRootNode().activeElement === this) document.body.focus();
  } });
  const make = ({ collapsed = [], minimum = 0, maximum = 10, sourceId = 'organization-source' } = {}) => {
    const root = document.createElement('section'); root.className = 'cfx-interactive-chart'; root.dataset.cfxChartId = 'native-navigation-focus';
    const x = axis(minimum, maximum), y = axis();
    const metadata = { series: [], xLabels: [], regions: [], annotations: [] };
    root.innerHTML = '<div class="cfx-stage"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 260"></svg></div><div class="cfx-tooltip" hidden></div>';
    if (channel === 'hierarchy') {
      metadata.hierarchy = { kind: 'topology', sourceId, collapsedNodeIds: collapsed, nodeOrder: [], layoutDirection: null,
        nodes: [{ id: 'lead', hierarchy: true }, { id: 'engineering', hierarchy: true }] };
      root.dataset.cfxInteractionFeatures = 'HierarchyNavigation';
      root.insertAdjacentHTML('beforeend', '<nav data-cfx-hierarchy-navigation><button data-cfx-hierarchy-control="home">Home</button>'
        + '<button data-cfx-hierarchy-control="toggle" data-cfx-hierarchy-node="engineering">Engineering</button><span data-cfx-hierarchy-status></span></nav>');
    } else {
      Object.assign(metadata, { xProjection: x, yProjection: y, dataProjection: { plotBounds: x.plotBounds, horizontal: false, x, y } });
      root.insertAdjacentHTML('beforeend', '<nav data-cfx-x-navigation><input type="range" data-cfx-x-range="start" data-cfx-x-control="start">'
        + '<input type="range" data-cfx-x-range="end" data-cfx-x-control="end"><span data-cfx-x-value="start"></span><span data-cfx-x-value="end"></span>'
        + '<button data-cfx-x-full data-cfx-x-control="full">Full</button><span data-cfx-x-status></span></nav>');
    }
    root.dataset.cfxPreparedChart = JSON.stringify(metadata); return root;
  };
  const adaptive = options => {
    const wide = make(options), narrow = make(options), root = narrow.cloneNode(true);
    root.dataset.cfxAdaptiveDrawing = 'narrow';
    for (const [name, drawing] of [['wide', wide], ['narrow', narrow]]) {
      drawing.dataset.cfxResponsiveLayout = 'adaptive'; drawing.dataset.cfxAdaptiveBreakpoint = '640';
      const template = document.createElement('template'); template.setAttribute('data-cfx-adaptive-' + name, 'true');
      template.content.append(drawing); root.append(template);
    }
    root.dataset.cfxResponsiveLayout = 'adaptive'; root.dataset.cfxAdaptiveBreakpoint = '640'; return root;
  };
  const section = options => adaptiveScene ? adaptive(options) : make(options);
  const root = section(); root.getBoundingClientRect = () => ({ x: 0, y: 0, left: 0, top: 0, right: 390, bottom: 260, width: 390, height: 260 });
  document.body.append(root); window.eval(runtime);
  const controller = window.ChartForgeXCharts.get(root), pending = [], changes = [], errors = [];
  const callback = (request, signal) => new Promise(resolve => pending.push({ request, signal, resolve }));
  controller.connectNative(callback, { basis: 'initial', axes: channel === 'hierarchy' ? null : ['x'], ...(channel === 'hierarchy' ? { hierarchy: true } : {}) });
  root.addEventListener('cfx' + channel + 'change', event => changes.push(event.detail));
  root.addEventListener('cfx' + channel + 'error', event => errors.push(event.detail));
  // Observe actual pending listeners, without asserting their names or number.
  const listenerSets = [];
  for (const target of [document, window]) {
    const listeners = new Set(), ids = new WeakMap(), add = target.addEventListener, remove = target.removeEventListener; let sequence = 0; listenerSets.push(listeners);
    const tracked = type => target === window ? type === 'blur' : ['focusin', 'focusout', 'pointerdown', 'click', 'keydown'].includes(type);
    const key = (type, listener, options) => { if (!ids.has(listener)) ids.set(listener, ++sequence); return type + ':' + ids.get(listener) + ':' + !!(typeof options === 'boolean' ? options : options?.capture); };
    target.addEventListener = function(type, listener, options) { if (tracked(type)) listeners.add(key(type, listener, options)); return add.call(this, type, listener, options); };
    target.removeEventListener = function(type, listener, options) { if (tracked(type)) listeners.delete(key(type, listener, options)); return remove.call(this, type, listener, options); };
  }
  const control = () => root.querySelector(channel === 'hierarchy' ? '[data-cfx-hierarchy-node=engineering]' : '[data-cfx-x-control=full]');
  const flush = async () => { for (let index = 0; index < 12; index++) await Promise.resolve(); };
  const request = async () => {
    control().focus();
    if (channel === 'hierarchy') control().click(); else controller.setXWindow(2, 8);
    await flush(); return pending.at(-1);
  };
  const reply = async (request, options = {}) => {
    request.resolve({ html: section(channel === 'hierarchy' ? { collapsed: ['engineering'], ...options } : { minimum: 2, maximum: 8, ...options }).outerHTML, basis: 'accepted' });
    await flush();
  };
  return { dom, window, document, root, controller, pending, changes, errors, section, control, request, reply, flush,
    listeners: () => listenerSets.reduce((count, listeners) => count + listeners.size, 0),
    close() { controller.dispose(); dom.window.close(); } };
}
for (const adaptiveScene of [false, true]) test(`hierarchy retains the pre-disable control through ${adaptiveScene ? 'adaptive' : 'ordinary'} native commit without aborting its host signal`, async () => {
  const f = fixture('hierarchy', adaptiveScene);
  try {
    const old = f.control(), request = await f.request();
    assert.equal(old.disabled, true); assert.equal(f.document.activeElement, f.document.body);
    await f.reply(request);
    assert.notEqual(f.control(), old); assert.equal(f.document.activeElement, f.control());
    assert.equal(f.control().disabled, false); assert.equal(request.signal.aborted, false);
    assert.equal(f.changes.length, 1); assert.equal(f.errors.length, 0); assert.equal(f.listeners(), 0);
  } finally { f.close(); }
});
for (const disposition of ['accepted', 'rejected']) test(`${disposition} hierarchy reply restores the owned control after deferred disabled-button focusout`, async () => {
  const f = fixture();
  try {
    const old = f.control(), revision = f.controller.sceneRevision, request = await f.request();
    assert.equal(old.disabled, true); assert.equal(f.document.activeElement, f.document.body);
    // Chromium can deliver this automatic focusout in a later turn while the native reply is pending.
    await new Promise(resolve => f.window.setTimeout(() => {
      old.dispatchEvent(new f.window.FocusEvent('focusout', { bubbles: true, composed: true, relatedTarget: null }));
      resolve();
    }, 0));
    await f.reply(request, disposition === 'rejected' ? { collapsed: [] } : {});
    assert.equal(f.document.activeElement, f.control()); assert.equal(f.control().disabled, false);
    assert.equal(request.signal.aborted, false); assert.equal(f.listeners(), 0);
    assert.equal(f.changes.length, disposition === 'accepted' ? 1 : 0);
    assert.equal(f.errors.length, disposition === 'rejected' ? 1 : 0);
    if (disposition === 'rejected') {
      assert.equal(f.control(), old); assert.equal(f.controller.sceneRevision, revision);
    } else assert.notEqual(f.control(), old);
  } finally { f.close(); }
});
for (const channel of ['hierarchy', 'viewport']) for (const intent of ['pointer', 'tab', 'focus'])
  test(`${channel} reply does not reclaim navigation focus after newer ${intent} intent`, async () => {
    const f = fixture(channel);
    try {
      const request = await f.request();
      if (intent === 'pointer') f.document.getElementById('outside').dispatchEvent(new f.window.MouseEvent('pointerdown', { bubbles: true }));
      if (intent === 'tab') f.document.dispatchEvent(new f.window.KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
      if (intent === 'focus') f.document.getElementById('elsewhere').focus();
      await f.reply(request);
      assert.notEqual(f.document.activeElement, f.control());
      if (intent === 'focus') assert.equal(f.document.activeElement.id, 'elsewhere');
      assert.equal(f.changes.length, 1); assert.equal(f.errors.length, 0); assert.equal(request.signal.aborted, false); assert.equal(f.listeners(), 0);
    } finally { f.close(); }
  });
test('X navigation keeps the currently focused control on accepted replacement', async () => {
  const f = fixture('viewport');
  try {
    const request = await f.request(); await f.reply(request);
    assert.equal(f.document.activeElement, f.control()); assert.equal(request.signal.aborted, false); assert.equal(f.listeners(), 0);
  } finally { f.close(); }
});
test('rejected hierarchy reply reenables and restores only the still-owned control, with no basis or revision advance', async () => {
  const f = fixture();
  try {
    const revision = f.controller.sceneRevision, request = await f.request(), old = f.control();
    await f.reply(request, { sourceId: 'foreign' });
    assert.equal(f.control(), old); assert.equal(f.document.activeElement, old); assert.equal(old.disabled, false);
    assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0); assert.equal(f.errors.length, 1); assert.equal(f.listeners(), 0);
    const next = await f.request(); assert.equal(next.request.basis, 'initial'); await f.reply(next);
    assert.equal(f.document.activeElement, f.control()); assert.equal(f.changes.length, 1); assert.equal(f.listeners(), 0);
  } finally { f.close(); }
});
for (const disposition of ['disconnect', 'dispose']) test(`${disposition} detaches pending focus ownership and ignores the late reply`, async () => {
  const f = fixture();
  try {
    const request = await f.request(), revision = f.controller.sceneRevision;
    if (disposition === 'disconnect') f.controller.connectNative(null, { axes: null }); else f.controller.dispose();
    assert.equal(request.signal.aborted, true); assert.equal(f.listeners(), 0);
    f.document.getElementById('elsewhere').focus(); await f.reply(request);
    assert.equal(f.document.activeElement.id, 'elsewhere'); assert.equal(f.controller.sceneRevision, revision); assert.equal(f.changes.length, 0);
  } finally { f.close(); }
});

test('superseding a request releases its focus lease and a stale response cannot override newer outside focus', async () => {
  const f = fixture();
  try {
    const stale = await f.request();
    f.controller.setHierarchyView({ collapsedNodeIds: ['engineering'] }); await f.flush();
    const current = f.pending.at(-1); assert.notEqual(current, stale); assert.equal(stale.signal.aborted, true);
    f.document.getElementById('elsewhere').focus(); await f.reply(stale);
    assert.equal(f.changes.length, 0); assert.equal(f.document.activeElement.id, 'elsewhere');
    assert.equal(current.request.basis, 'initial'); await f.reply(current);
    assert.equal(f.changes.length, 1); assert.equal(f.document.activeElement.id, 'elsewhere'); assert.equal(current.signal.aborted, false); assert.equal(f.listeners(), 0);
  } finally { f.close(); }
});
