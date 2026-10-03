const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const vm = require('node:vm');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');

function runtime() {
  const images = [], redraws = [], timers = new Set();
  class Image {
    constructor() { this.complete = false; this.naturalWidth = 0; this.listeners = new Map(); images.push(this); }
    addEventListener(type, callback, options) { const entries = this.listeners.get(type) || new Map(); entries.set(callback, options); this.listeners.set(type, entries); }
    removeEventListener(type, callback) { this.listeners.get(type)?.delete(callback); }
    finish(type) {
      this.complete = true; this.naturalWidth = type === 'load' ? 32 : 0;
      [...this.listeners.get(type)].forEach(([callback, options]) => { if (options?.once) this.removeEventListener(type, callback); callback(); });
    }
  }
  const viewport = fs.readFileSync(path.join(assets, 'graph-explorer.05-viewport.js'), 'utf8').split('  const clusterMetrics')[0];
  const nodes = fs.readFileSync(path.join(assets, 'graph-explorer.03-canvas-nodes.js'), 'utf8');
  const bindings = fs.readFileSync(path.join(assets, 'graph-explorer.30-bindings.js'), 'utf8');
  const preload = bindings.slice(bindings.indexOf('  const preloadCanvasImages'), bindings.indexOf('  const exportSvgContent'));
  const host = { Image, drawCanvas: (root, state) => redraws.push({ root, state }), graphState: () => { throw new Error('Reconstructed live image state'); },
    setTimeout: callback => { timers.add(callback); return callback; }, clearTimeout: callback => timers.delete(callback) };
  vm.runInNewContext(viewport + nodes + preload + '\nthis.api = { drawNodeMark, preloadCanvasImages };', host);
  const context = new Proxy({}, { get: (target, key) => target[key] || (() => {}), set: (target, key, value) => (target[key] = value, true) });
  return { api: host.api, images, redraws, timers, context };
}

test('shared loading image redraws each connected graph once and retains its latest live state', () => {
  for (const completion of ['load', 'error']) {
    const { api, images, redraws, context } = runtime();
    const roots = [{ isConnected: true }, { isConnected: true }, { isConnected: false }];
    for (let frame = 0; frame < 25; frame++) for (const root of roots) for (let node = 0; node < 12; node++) {
      api.drawNodeMark(context, { x: node * 40, y: 100, size: 12, shape: node % 2 ? 'image' : 'imageRect', imageUrl: 'https://host/image.png' }, false, true, root, true);
    }
    roots.forEach(root => root.__cfxGraphState = { phase: 'latest' });
    assert.equal(images.length, 1);
    images[0].finish(completion);
    assert.equal(redraws.length, 2);
    roots.slice(0, 2).forEach((root, index) => { assert.equal(redraws[index].root, root); assert.equal(redraws[index].state, root.__cfxGraphState); });
  }
});

test('PNG image preload deduplicates shared URLs and releases listeners on completion or timeout', async () => {
  for (const completion of ['load', 'error', 'timeout']) {
    const { api, images, timers } = runtime();
    const state = { nodes: Array.from({ length: 12 }, (_, i) => ({ shape: i % 2 ? 'image' : 'imageRect', imageUrl: 'https://host/image.png' })) };
    const loaded = api.preloadCanvasImages({}, state);
    assert.equal(images.length, 1); assert.equal(timers.size, 1);
    if (completion === 'timeout') [...timers][0](); else images[0].finish(completion);
    await loaded;
    assert.equal(timers.size, 0);
    // Only the cache's own completion listener may remain; export closures do not.
    assert.ok(images[0].listeners.get('load').size <= 1);
    assert.ok(images[0].listeners.get('error').size <= 1);
  }
});
