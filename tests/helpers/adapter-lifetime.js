const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const assets = path.resolve(__dirname, '../../ChartForgeX.Interactivity.Html/Assets');
const adapterSource = fs.readFileSync(path.join(assets, 'adapter-lifetime.js'), 'utf8');
const graphSource = fs.readFileSync(path.join(assets, 'graph-explorer.00-lifetime.js'), 'utf8');

// Fragment tests do not have a DOM tree or browser clock. Keep those boundaries
// controllable while using the same listener/timer/frame owner as the shipped adapters.
function adapterLifetimes(options = {}) {
  const observers = new Set(), mounted = new Set(), tree = {};
  class MutationObserver {
    constructor(callback) { this.callback = callback; this.observing = false; observers.add(this); }
    observe() { this.observing = true; }
    disconnect() { this.observing = false; }
  }
  const schedule = options.setTimeout || setTimeout;
  const cancel = options.clearTimeout || clearTimeout;
  const host = {
    window: { setTimeout: schedule, clearTimeout: cancel }, MutationObserver,
    requestAnimationFrame: options.requestAnimationFrame || (callback => schedule(() => callback(0), 0)),
    cancelAnimationFrame: options.cancelAnimationFrame || cancel
  };
  vm.runInNewContext(adapterSource + '\n' + graphSource + '\nthis.api = { graphLifetime, listenGraph };', host);
  const controller = host.window[Symbol.for('ChartForgeX.adapterLifetimes')];
  return {
    window: host.window,
    graphLifetime: host.api.graphLifetime,
    listenGraph: host.api.listenGraph,
    mount(root, owner = 'graph') {
      if (root.isConnected === undefined) root.isConnected = true;
      root.getRootNode ||= () => tree;
      const lifetime = controller.create(root, owner);
      mounted.add(lifetime);
      return lifetime;
    },
    notifyMutations(records = []) { for (const observer of Array.from(observers)) if (observer.observing) observer.callback(records); },
    dispose() { for (const lifetime of mounted) lifetime.dispose(); mounted.clear(); }
  };
}

module.exports = { adapterLifetimes };
