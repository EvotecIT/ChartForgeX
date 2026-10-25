'use strict';
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../mermaid-conformance/node_modules/jsdom');
const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const runtime = ['adapter-native-text-export.js', 'interactive.js'].map(name => fs.readFileSync(path.join(assets, name), 'utf8')).join('\n');

// Native generated HTML and JSDOM's missing SVG measurement are transport boundaries.
// The companion native/browser contracts use real PrepareSection output and paint.
function fixture(policy = 'latest-window', { hierarchy = false, bothAxes = false, capturedMotion = false } = {}) {
  const dom = new JSDOM('<!doctype html><body></body>', { runScripts: 'outside-only', pretendToBeVisual: true, url: 'https://example.test/' });
  const { window } = dom, pending = [], requests = [], changes = [], failures = [], pageErrors = [];
  window.SVGElement.prototype.getBBox = () => ({ x: 40, y: 40, width: 20, height: 20 });
  // Opt-in presentation boundary: the incumbent native-value fixture uses these same finite rendered boxes.
  // Queue callbacks before mounting so elapsed host time cannot advance a captured native frame.
  const motionFrames = new Map(); let motionSequence = 0;
  if (capturedMotion) {
    window.requestAnimationFrame = callback => { const id = ++motionSequence; motionFrames.set(id, callback); return id; };
    window.cancelAnimationFrame = id => motionFrames.delete(id);
    window.HTMLElement.prototype.getBoundingClientRect = () => ({ left: 10, top: 20, width: 360, height: 260 });
    Object.defineProperty(window.HTMLElement.prototype, 'offsetWidth', { configurable: true, get: () => 360 });
    Object.defineProperty(window.HTMLElement.prototype, 'offsetHeight', { configurable: true, get: () => 260 });
    window.SVGElement.prototype.getBoundingClientRect = () => ({ left: 24, top: 34, width: 320, height: 220 });
  }

  window.addEventListener('error', event => pageErrors.push(event.message));
  // Same opaque namespace/encoding as VisualAnnotationIdentity.SourceIdFor; this is generated-host transport, not native geometry.
  const annotationSourceId = 'annotation-id-' + Buffer.from('note', 'utf8').toString('base64url');
  const make = ({ minimum = 0, maximum = 10, hierarchyRootId = 'team', note = { x: 4, y: 40, label: 'Original' } } = {}) => {
    const root = window.document.createElement('section'); root.className = 'cfx-interactive-chart';
    Object.assign(root.dataset, { cfxChartId: 'native-admission', cfxInteractionFeatures: 'Selection,KeyboardNavigation,Zoom,Pan,Export' + (hierarchy ? ',HierarchyNavigation' : '') });
    const axis = (low, high, full) => ({ minimum: low, maximum: high, fullMinimum: 0, fullMaximum: full,
      transformedMinimum: low, transformedMaximum: high, transformedFullMinimum: 0, transformedFullMaximum: full,
      scale: 'linear', threshold: 1, reversed: false, plotBounds: { x: 20, y: 20, width: 300, height: 200 } });
    const x = axis(minimum, maximum, 10), y = axis(0, 100, 100);
    root.dataset.cfxPreparedChart = JSON.stringify({ series: [], xLabels: [], regions: [{ id: annotationSourceId, role: 'annotation', x: 40, y: 40, width: 20, height: 20, label: note.label }],
      annotations: [{ id: 'note', sourceId: annotationSourceId, kind: 'Note' }], xProjection: x, yProjection: y,
      dataProjection: { plotBounds: x.plotBounds, horizontal: false, x, y },
      ...(hierarchy ? { hierarchy: { kind: 'treemap', seriesKey: 'work', rootId: hierarchyRootId, path: [] } } : {}) });
    root.innerHTML = '<div class="cfx-toolbar"><button data-cfx-reset>Reset</button><button data-cfx-zoom="in">Zoom</button>'
      + '<button data-cfx-export="svg">SVG</button><button data-cfx-export="png">PNG</button></div>'
      + '<div class="cfx-stage"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 260">'
      + '<g data-cfx-role="annotation" data-cfx-annotation-id="note" data-cfx-source-id="' + annotationSourceId + '" data-cfx-kind="Note"'
      + ' data-cfx-label="' + note.label + '" data-cfx-source-x="' + note.x + '" data-cfx-source-y="' + note.y
      + '" data-cfx-source-end-x="" data-cfx-source-end-y="" data-cfx-channel-offset="0" data-cfx-y-axis="Primary"/>'
      + (hierarchy ? '<g data-cfx-node="team" data-cfx-target-kind="node" data-cfx-target-id="team"/>' : '')
      + '</svg></div><div class="cfx-tooltip" hidden></div>';
    return root;
  };
  const root = make(); window.document.body.append(root); window.eval(runtime);
  const controller = window.ChartForgeXCharts.get(root);
  for (const channel of ['viewport', 'annotation', 'hierarchy', 'progress']) {
    root.addEventListener('cfx' + channel + 'request', event => requests.push(event.detail));
    root.addEventListener('cfx' + channel + 'change', event => changes.push(event.detail));
    root.addEventListener('cfx' + channel + 'error', event => failures.push(event.detail));
  }
  const callback = (request, signal) => new Promise((resolve, reject) => pending.push({ request, signal, resolve, reject }));
  const connect = (options = {}, prepare = callback) => controller.connectNative(prepare,
    { basis: 'initial', axes: hierarchy && !bothAxes ? null : bothAxes ? ['x', 'y'] : ['x'], ...(hierarchy ? { hierarchy: true } : {}),
      ...(policy === 'omitted' ? {} : { preparationBackpressure: policy }), ...options });
  connect();
  const flush = async () => { for (let turn = 0; turn < 24; turn++) await Promise.resolve(); };
  const set = minimum => controller.setViewport({ x: { minimum, maximum: 10 - minimum } });
  const reply = async (index, basis = 'accepted', override) => {
    const item = pending[index], source = item.request.sourceViewport?.x;
    const result = item.request.channel === 'hierarchy' ? { hierarchyRootId: item.request.kind === 'reset' ? null : item.request.rootId }
      : item.request.kind === 'reset' ? {} : source || {};
    item.resolve({ html: (override || make(result)).outerHTML, basis }); await flush();
  };
  const captureRaw = (name, diagnostics = {}) => {
    const output = process.env.CFX_BROWSER_CAPTURE_DIRECTORY; if (!output) return null;
    fs.mkdirSync(output, { recursive: true });
    const mounted = window.ChartForgeXCharts.get(root);
    const facts = { requests, physicalCalls: pending.map(item => ({ request: item.request, aborted: item.signal.aborted })),
      changes, failures, pageErrors, revision: controller.sceneRevision, rootConnected: root.isConnected,
      mounted: mounted !== null, sameController: mounted === controller, runtimeBound: root.dataset.cfxRuntimeBound,
      prepared: JSON.parse(root.dataset.cfxPreparedChart), diagnostics };
    // Persist raw lifetime/transport before invoking any guarded public accessor.
    fs.writeFileSync(path.join(output, name + '.lifetime.json'), JSON.stringify(facts, null, 2));
    fs.writeFileSync(path.join(output, name + '.transport.html'), root.outerHTML);
    return facts;
  };
  const capture = name => {
    const facts = captureRaw(name); if (!facts) return;
    // Keep this real accessor and its lifetime contract; never remount or hide an invalid facade.
    fs.writeFileSync(path.join(process.env.CFX_BROWSER_CAPTURE_DIRECTORY, name + '.observed.json'),
      JSON.stringify({ ...facts, window: controller.getXWindow() }, null, 2));
  };
  return { window, root, controller, pending, requests, changes, failures, pageErrors, make, connect, callback, flush, set, reply, capture, captureRaw,
    ...(capturedMotion ? {
      advanceMotion(timestamp) { const callbacks = [...motionFrames.values()]; motionFrames.clear(); for (const callback of callbacks) callback(timestamp); },
      motionState() { const svg = root.querySelector('.cfx-stage>svg'); return { queuedFrames: motionFrames.size, rootBounds: root.getBoundingClientRect(), svgBounds: svg?.getBoundingClientRect(), frame: root.querySelector('.cfx-native-motion-frame')?.outerHTML }; }
    } : {}),
    close() { window.ChartForgeXCharts.get(root)?.dispose(); controller.dispose(); dom.window.close(); } };
}
module.exports = { fixture };
