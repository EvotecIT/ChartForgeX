'use strict';
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
// The override permits a scratch-only before run against the exact unchanged maintained fixture/assets.
const fixturePath = require.resolve(process.env.CFX_OWNED_FONT_LIFETIME_FIXTURE || './native-preparation-fixture');
const { fixture } = require(fixturePath);
const sourceRoot = path.resolve(path.dirname(fixturePath), '../..');
const fontBytes = fs.readFileSync(path.join(sourceRoot, 'ChartForgeX.Tests/Fixtures/OpenType/substitution.ttf'));
const rootIdentity = crypto.createHash('sha256').update(fontBytes).digest('hex');

function setup() {
  const f = fixture('immediate');
  f.controller.dispose();
  // Reuse the real fixture/controller asset, but none of its unrelated annotation/SVG measurement boundary.
  delete f.window.SVGElement.prototype.getBBox;
  const { window, root } = f, loads = [], hashes = [], added = [], removed = [], fonts = new Set(), updated = [];
  Object.defineProperty(window.document, 'fonts', { configurable: true, value: {
    add(face) { added.push(face); fonts.add(face); return this; },
    delete(face) { removed.push(face); return fonts.delete(face); },
    get size() { return fonts.size; }
  } });
  Object.defineProperty(window.crypto, 'subtle', { configurable: true, value: {
    digest(algorithm, bytes) {
      assert.equal(algorithm, 'SHA-256'); hashes.push(Buffer.from(bytes));
      // Forward to actual SHA-256; this boundary never fabricates a matching identity.
      return crypto.webcrypto.subtle.digest(algorithm, Buffer.from(bytes));
    }
  } });
  window.FontFace = class {
    constructor(family, bytes, descriptors) {
      assert.ok(bytes instanceof window.Uint8Array); assert.deepEqual(Buffer.from(bytes), fontBytes);
      this.family = family; this.bytes = Buffer.from(bytes); this.descriptors = descriptors; this.status = 'unloaded';
    }
    load() {
      this.status = 'loading';
      return new window.Promise(resolve => loads.push({ face: this, complete: () => { this.status = 'loaded'; resolve(this); } }));
    }
  };
  const section = (name, options = {}) => {
    const node = f.make(options), svg = node.querySelector('.cfx-stage svg');
    node.dataset.cfxInteractionFeatures = '';
    const metadata = JSON.parse(node.dataset.cfxPreparedChart); metadata.annotations = []; metadata.regions = [];
    node.dataset.cfxPreparedChart = JSON.stringify(metadata);
    const alias = 'cfx-owned-' + crypto.createHash('sha256').update(name + ':' + rootIdentity).digest('hex');
    svg.innerHTML = '<g data-owned-fixture="' + name + '"><g data-cfx-owned-font-native="' + alias + '"><path d="M0 0H1V1H0Z"/></g>'
      + '<text data-cfx-owned-font-text="' + alias + '" display="none" font-family="' + alias + '">H</text></g>';
    const resource = window.document.createElement('template');
    resource.setAttribute('data-cfx-font-root', rootIdentity); resource.setAttribute('data-cfx-font-byte-length', String(fontBytes.length));
    resource.content.textContent = fontBytes.toString('base64'); node.append(resource);
    const view = window.document.createElement('template');
    for (const [key, value] of Object.entries({ face: alias, resource: rootIdentity, weight: '400', style: 'normal' }))
      view.setAttribute('data-cfx-font-' + key, value);
    node.append(view); return node;
  };
  const initial = section('initial');
  for (const attribute of [...root.attributes]) root.removeAttribute(attribute.name);
  for (const attribute of initial.attributes) root.setAttribute(attribute.name, attribute.value);
  root.replaceChildren(...[...initial.childNodes].map(node => node.cloneNode(true)));
  const controller = window.ChartForgeXCharts.mount(root);
  root.addEventListener('cfxupdated', event => updated.push(event.detail));
  const settle = async () => { await new Promise(resolve => setImmediate(resolve)); await f.flush(); };
  const until = async (condition, message) => {
    const deadline = Date.now() + 2000;
    while (!condition() && Date.now() < deadline) await settle();
    assert.ok(condition(), message);
  };
  const visible = svg => ({ text: [...svg.querySelectorAll('[data-cfx-owned-font-text]')].filter(node => !node.hasAttribute('display')).length,
    fallback: [...svg.querySelectorAll('[data-cfx-owned-font-native]')].filter(node => node.getAttribute('display') !== 'none').length });
  const capture = (name, extra = {}) => {
    const directory = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!directory) return;
    assert.ok(path.isAbsolute(directory)); fs.mkdirSync(directory, { recursive: true });
    fs.writeFileSync(path.join(directory, name + '.json'), JSON.stringify({ loads: loads.map(item => ({ family: item.face.family, status: item.face.status })),
      validatedInputs: hashes.map(bytes => crypto.createHash('sha256').update(bytes).digest('hex')), added: added.map(face => face.family),
      removed: removed.map(face => face.family), active: [...fonts].map(face => face.family), visible: visible(root.querySelector('svg')),
      revision: controller.sceneRevision, connected: root.isConnected, mounted: window.ChartForgeXCharts.get(root) === controller,
      errors: f.pageErrors, ...extra }, null, 2));
    fs.writeFileSync(path.join(directory, name + '.html'), root.outerHTML);
  };
  const ready = async (index, name) => {
    await until(() => loads.length > index, 'The accepted owned face must reach the browser FontFace.load boundary.');
    assert.deepEqual(hashes[index], fontBytes); loads[index].complete(); await settle(); capture(name);
    assert.ok(fonts.has(loads[index].face)); assert.deepEqual(visible(root.querySelector('svg')), { text: 1, fallback: 0 });
    assert.deepEqual(f.pageErrors, []);
  };
  return { ...f, controller, section, loads, hashes, added, removed, fonts, updated, settle, until, visible, capture, ready,
    close() { window.ChartForgeXCharts.get(root)?.dispose(); f.close(); } };
}

test('initial public mount validates and loads the owned binary before replacing its native fallback', async () => {
  const f = setup();
  try {
    assert.deepEqual(f.visible(f.root.querySelector('svg')), { text: 0, fallback: 1 });
    await f.ready(0, 'owned-font-initial-mount-control');
    assert.equal(f.fonts.size, 1); assert.equal(f.added.length, 1);
    f.controller.dispose(); f.capture('owned-font-initial-disposed-control');
    assert.equal(f.fonts.size, 0); assert.ok(f.removed.includes(f.loads[0].face));
  } finally { f.close(); }
});

test('accepted public update keeps the replacement font work in the current chart lifetime', async () => {
  const f = setup(); let external;
  try {
    await f.ready(0, 'owned-font-update-source-control'); const oldSvg = f.root.querySelector('svg'), revision = f.controller.sceneRevision;
    const first = f.loads[0].face;
    // This exported public registration owns the exact initial alias independently of the chart lifetime.
    external = f.window[Symbol.for('ChartForgeX.nativeTextExport')].registerBinary(first.family,
      new f.window.Uint8Array(fontBytes), { weight: '400', style: 'normal' });
    await external.ready;
    assert.equal(f.fonts.size, 1); assert.equal(f.loads.length, 1); assert.equal(f.added.length, 1);
    // The renderer's same alias/root/descriptors take the incumbent known-face reuse branch.
    f.controller.update(f.section('initial').outerHTML); await f.settle(); f.capture('owned-font-update-known-alias-accepted');
    assert.notEqual(f.controller.sceneRevision, revision); assert.equal(oldSvg.isConnected, false); assert.equal(f.updated.length, 1);
    assert.equal(f.fonts.size, 1); assert.ok(f.fonts.has(first)); assert.equal(f.loads.length, 1); assert.equal(f.added.length, 1);
    assert.deepEqual(f.visible(f.root.querySelector('svg')), { text: 1, fallback: 0 });
    assert.deepEqual(f.pageErrors, []);
    f.controller.dispose(); f.capture('owned-font-update-known-alias-chart-disposed');
    assert.equal(f.fonts.size, 1); assert.ok(f.fonts.has(first), 'Only the external holder must retain the face after chart disposal.');
    external.release(); f.capture('owned-font-update-known-alias-external-released');
    assert.equal(f.fonts.size, 0); assert.ok(f.removed.includes(first)); assert.equal(f.loads.length, 1);
    external.release(); assert.equal(f.fonts.size, 0); // Releasing the holder twice cannot change the zero baseline.
  } finally { external?.release(); f.close(); }
});

test('accepted native viewport reply keeps its owned font after the validated section replacement', async () => {
  const f = setup(), pending = [], changes = [], failures = [];
  try {
    await f.ready(0, 'owned-font-native-source-control');
    f.root.addEventListener('cfxviewportchange', event => changes.push(event.detail));
    f.root.addEventListener('cfxviewporterror', event => failures.push(event.detail));
    f.controller.connectNative((request, signal) => new Promise(resolve => pending.push({ request, signal, resolve })), { basis: 'owned-initial', axes: ['x'] });
    f.controller.setViewport({ x: { minimum: 1, maximum: 9 } }); await f.until(() => pending.length === 1, 'One native preparation request must be admitted.');
    assert.equal(pending[0].request.basis, 'owned-initial');
    pending[0].resolve({ html: f.section('native-replacement', { minimum: 1, maximum: 9 }).outerHTML, basis: 'owned-accepted' });
    await f.until(() => changes.length === 1 || failures.length !== 0, 'The actual public native reply must settle.');
    f.capture('owned-font-native-reply-before-load', { changes, failures }); assert.deepEqual(failures, []);
    assert.equal(f.controller.getXWindow().minimum, 1); assert.equal(f.controller.getXWindow().maximum, 9);
    await f.ready(1, 'owned-font-native-reply-loaded');
    f.controller.dispose(); assert.equal(f.fonts.size, 0); assert.ok(f.removed.includes(f.loads[1].face));
  } finally { f.close(); }
});

test('disposing a public controller before its pending load completes cannot register or reveal a late face', async () => {
  const f = setup();
  try {
    await f.until(() => f.loads.length === 1, 'Initial binary validation must reach a delayed browser load.');
    const svg = f.root.querySelector('svg'); f.controller.dispose(); f.loads[0].complete(); await f.settle();
    f.capture('owned-font-late-disposal-control');
    assert.equal(f.fonts.size, 0); assert.equal(f.added.length, 0); assert.ok(f.removed.includes(f.loads[0].face));
    assert.deepEqual(f.visible(svg), { text: 0, fallback: 1 }); assert.equal(f.window.ChartForgeXCharts.get(f.root), null);
    assert.deepEqual(f.pageErrors, []);
  } finally { f.close(); }
});

test('a late predecessor load stays detached while the accepted replacement obtains and releases its own face', async () => {
  const f = setup();
  try {
    await f.until(() => f.loads.length === 1, 'The predecessor must actually start a delayed FontFace load.');
    const predecessor = f.root.querySelector('svg'); f.controller.update(f.section('after-pending-source').outerHTML);
    f.loads[0].complete(); await f.settle(); f.capture('owned-font-late-predecessor-before-new-load', { predecessorVisible: f.visible(predecessor) });
    assert.equal(predecessor.isConnected, false); assert.deepEqual(f.visible(predecessor), { text: 0, fallback: 1 });
    assert.equal(f.fonts.has(f.loads[0].face), false); assert.equal(f.added.includes(f.loads[0].face), false);
    await f.ready(1, 'owned-font-late-predecessor-replacement-loaded');
    f.controller.dispose(); assert.equal(f.fonts.size, 0); assert.ok(f.removed.includes(f.loads[1].face));
  } finally { f.close(); }
});
