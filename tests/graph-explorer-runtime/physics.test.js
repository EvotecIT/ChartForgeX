const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');

const adapter = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html');
const manifest = fs.readFileSync(path.join(adapter, 'HtmlGraphExplorerAssets.cs'), 'utf8');
const files = [...manifest.matchAll(/"ChartForgeX\.Interactivity\.Html\.Assets\.(graph-explorer\.[^"]+\.js)"/g)].map(match => match[1]);
const code = files.map(file => fs.readFileSync(path.join(adapter, 'Assets', file), 'utf8')).join('\n');

function runtime() {
  const frames = new Map();
  let sequence = 0;
  const workers = [];
  const context = vm.createContext({
    document: { readyState: 'loading', addEventListener() {} }, window: {},
    Worker: class { constructor() { this.messages = []; workers.push(this); } postMessage(message) { this.messages.push(message); } terminate() { this.stopped = true; } },
    Blob: class {}, URL: { createObjectURL: () => 'blob:test', revokeObjectURL() {} },
    requestAnimationFrame: callback => { frames.set(++sequence, callback); return sequence; },
    cancelAnimationFrame: id => frames.delete(id), performance: { now: () => 0 },
    CustomEvent: class { constructor(type, options) { this.type = type; this.detail = options.detail; } }
  });
  vm.runInContext(code + '\nthis.api = { workerPhysicsSource, profile, startWorkerPhysics, stopWorkerPhysics, updatePhysicsNodes, updateDraggedPhysicsNode, releaseDraggedPhysicsNode, graphVirtualElement, webGlUpload };', context);
  const root = context.api.graphVirtualElement('root', {}, []);
  root.dataset = { cfxGraphPhysicsState: 'running', cfxGraphRendererActive: 'canvas' };
  root.isConnected = true;
  root.querySelector = () => null;
  root.querySelectorAll = () => [];
  root.dispatchEvent = () => true;
  return { api: context.api, root, frames, workers, flush: () => { const work = [...frames.values()]; frames.clear(); work.forEach(callback => callback(16)); } };
}

function solver() {
  const host = runtime();
  const messages = [], timers = [];
  const self = { postMessage: (message, transfer) => {
      const delivered = structuredClone(message, { transfer });
    assert.equal(message.positions.byteLength, 0);
    messages.push({ message: delivered });
  } };
  vm.runInNewContext(host.api.workerPhysicsSource(), { self, setTimeout: fn => { timers.push(fn); return timers.length; }, clearTimeout() {}, Float64Array, Date });
  const settings = { ...host.api.profile(host.root), iterations: 100, minVelocity: -1 };
  self.onmessage({ data: { type: 'start', interval: 1, settings, nodes: [{ id: 'a', x: 1, y: 2, vx: 0, vy: 0, fixed: true, size: 10 }], edges: [] } });
  return { self, timers, messages };
}

test('worker transfers positions and waits for acknowledgement before producing another batch', () => {
  const worker = solver();
  worker.timers.shift()();
  assert.equal(worker.messages.length, 1);
  const { message } = worker.messages[0];
  assert.equal(message.type, 'progress');
  assert.equal(message.nodes, undefined);
  assert.deepEqual([...message.positions], [1, 2, 0, 0, 1]);
  assert.equal(worker.timers.length, 0);
  worker.self.onmessage({ data: { type: 'continue', positions: message.positions } });
  assert.equal(worker.timers.length, 1);
  worker.timers.shift()();
  assert.equal(worker.messages.length, 2);
});

test('drag updates received while a batch is outstanding survive acknowledgement', () => {
  const worker = solver();
  worker.timers.shift()();
  worker.self.onmessage({ data: { type: 'pin', nodeId: 'a', x: 90, y: 80, generation: 1 } });
  assert.equal(worker.timers.length, 0);
  worker.self.onmessage({ data: { type: 'continue', positions: worker.messages[0].message.positions } });
  worker.timers.shift()();
  assert.equal(worker.messages[1].message.generation, 1);
  assert.deepEqual([...worker.messages[1].message.positions].slice(0, 2), [90, 80]);
});

test('stale completion cannot overwrite a dragged node or stop the active simulation', () => {
  const host = runtime();
  const node = { id: 'a', x: 90, y: 80, vx: 0, vy: 0, fixed: true };
  const state = { nodes: [node], edges: [] };
  host.api.startWorkerPhysics(host.root, state, host.api.profile(host.root));
  host.api.updateDraggedPhysicsNode(host.root, node);
  const worker = host.workers[0];
  worker.onmessage({ data: { type: 'done', generation: 0, positions: new Float64Array([1, 2, 0, 0, 1]) } });
  assert.equal(host.frames.size, 1);
  host.flush();
  assert.equal(node.x, 90);
  assert.equal(host.root.dataset.cfxGraphPhysicsState, 'running');
  assert.equal(worker.messages.at(-1).type, 'continue');
  host.api.stopWorkerPhysics(host.root);
  assert.equal(worker.stopped, true);
});

test('stopping physics cancels a pending paint and detached roots stop their workers', () => {
  const host = runtime();
  const state = { nodes: [], edges: [] };
  host.api.startWorkerPhysics(host.root, state, host.api.profile(host.root));
  const worker = host.workers[0];
  worker.onmessage({ data: { type: 'progress', generation: 0, positions: new Float64Array() } });
  host.api.stopWorkerPhysics(host.root);
  assert.equal(host.frames.size, 0);
  host.api.startWorkerPhysics(host.root, state, host.api.profile(host.root));
  host.root.isConnected = false;
  host.workers[1].onmessage({ data: { type: 'progress', generation: 0, positions: new Float64Array() } });
  assert.equal(host.workers[1].stopped, true);
  assert.equal(host.frames.size, 0);
});

test('restarting physics begins a fresh frame cadence window after idle', () => {
  const host = runtime();
  host.root.__cfxGraphPerformanceFrameTimestamp = 100;
  host.root.__cfxGraphPerformanceFrameCount = 25;
  host.api.startWorkerPhysics(host.root, { nodes: [], edges: [] }, host.api.profile(host.root));
  assert.equal(host.root.__cfxGraphPerformanceFrameTimestamp, undefined);
  assert.equal(host.root.__cfxGraphPerformanceFrameCount, 0);
  host.api.stopWorkerPhysics(host.root);
});

test('WebGL reuses capacity and uploads only live values when the visible scene shrinks', () => {
  const host = runtime();
  const allocations = [], writes = [];
  let bound;
  const gl = {
    ARRAY_BUFFER: 1, DYNAMIC_DRAW: 2, FLOAT: 3,
    bindBuffer: (_, buffer) => { bound = buffer; },
    bufferData: (_, bytes) => allocations.push([bound, bytes]),
    bufferSubData: (_, offset, data) => writes.push([bound, offset, [...data]]),
    enableVertexAttribArray() {}, vertexAttribPointer() {}, uniform1i() {}
  };
  const renderer = { gl, positionBuffer: 'p', colorBuffer: 'c', sizeBuffer: 's' };
  host.api.webGlUpload(renderer, [1, 2, 3, 4], [1, 0, 0, 1, 0, 1, 0, 1], [5, 6], true);
  assert.equal(allocations.length, 3);
  const staging = renderer.uploads.position.data;
  host.api.webGlUpload(renderer, [7, 8], [0, 0, 1, 1], [9], true);
  assert.equal(allocations.length, 3);
  assert.equal(renderer.uploads.position.data, staging);
  assert.deepEqual(writes.slice(-3), [['p', 0, [7, 8]], ['c', 0, [0, 0, 1, 1]], ['s', 0, [9]]]);
  host.api.webGlUpload(renderer, Array(130).fill(2), Array(260).fill(1), Array(65).fill(3), true);
  assert.equal(allocations.length, 6);
  assert.ok(renderer.uploads.position.data.length >= 130);
});

test('continuous dragging presents neighbors while protecting newly edited and released nodes', () => {
  const host = runtime();
  const dragged = { id: 'a', x: 90, y: 80, vx: 0, vy: 0, fixed: true };
  const neighbor = { id: 'b', x: 0, y: 0, vx: 0, vy: 0, fixed: false };
  const state = { nodes: [dragged, neighbor], edges: [] };
  host.api.startWorkerPhysics(host.root, state, host.api.profile(host.root));
  const worker = host.workers[0];
  for (let generation = 0; generation < 3; generation++) {
    worker.onmessage({ data: { type: 'progress', generation,
      positions: new Float64Array([1, 2, 0, 0, 1, generation + 10, generation + 20, 0, 0, 0]) } });
    host.api.updateDraggedPhysicsNode(host.root, dragged);
    host.flush();
    assert.equal(dragged.x, 90);
    assert.equal(neighbor.x, generation + 10);
  }
  dragged.fixed = false;
  host.api.releaseDraggedPhysicsNode(host.root, dragged);
  worker.onmessage({ data: { type: 'done', generation: 3,
    positions: new Float64Array([1, 2, 0, 0, 1, 40, 50, 0, 0, 0]) } });
  host.flush();
  assert.equal(dragged.x, 90);
  assert.equal(dragged.fixed, false);
  assert.equal(neighbor.x, 40);
  assert.equal(host.root.dataset.cfxGraphPhysicsState, 'running');
  host.api.stopWorkerPhysics(host.root);
});

test('reduced motion hides intermediate worker positions but presents the final state', () => {
  const host = runtime();
  host.root.setAttribute('data-cfx-graph-reduced-motion', 'true');
  const node = { el: host.api.graphVirtualElement('node', {}, []), id: 'a', x: 1, y: 2, homeX: 1, homeY: 2, vx: 0, vy: 0, fixed: true, size: 8 };
  const state = { nodes: [node], edges: [], clusters: [], byId: new Map([['a', node]]) };
  host.api.startWorkerPhysics(host.root, state, host.api.profile(host.root));
  const worker = host.workers[0];
  worker.onmessage({ data: { type: 'progress', generation: 0, positions: new Float64Array([90, 80, 0, 0, 1]) } });
  host.flush();
  assert.equal(node.x, 1);
  assert.equal(worker.messages.at(-1).type, 'continue');
  worker.onmessage({ data: { type: 'done', generation: 0, positions: new Float64Array([100, 200, 0, 0, 1]) } });
  host.flush();
  assert.equal(node.x, 100);
  assert.equal(node.y, 200);
  assert.equal(host.root.dataset.cfxGraphPhysicsState, 'stabilized');
  assert.equal(worker.stopped, true);
});
