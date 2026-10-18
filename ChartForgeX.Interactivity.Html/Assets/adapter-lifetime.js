(() => {
  // Standalone adapters share removal ownership without loading the ordinary-chart runtime.
  const key = Symbol.for('ChartForgeX.adapterLifetimes');
  if (window[key]) return;
  const owners = new WeakMap(), mounted = new Set();
  let observer = null;
  const treesFor = root => {
    const trees = [];
    for (let tree = root.getRootNode(); tree; tree = tree.host?.getRootNode()) trees.push(tree);
    return trees;
  };
  const watch = () => {
    observer?.disconnect();
    if (!mounted.size) { observer = null; return; }
    observer ||= new MutationObserver(records => {
      const removals = records.filter(record => record.removedNodes.length);
      let changed = false;
      for (const lifetime of Array.from(mounted)) {
        if (!lifetime.root.isConnected) { lifetime.dispose(); changed = true; continue; }
        if (removals.some(record => lifetime.root.contains(record.target))) lifetime.prune();
        const trees = treesFor(lifetime.root);
        if (trees.length !== lifetime.trees.length || trees.some((tree, index) => tree !== lifetime.trees[index])) changed = true;
      }
      if (changed) watch();
    });
    const trees = new Set();
    for (const lifetime of mounted) {
      lifetime.trees = treesFor(lifetime.root);
      for (const tree of lifetime.trees) trees.add(tree);
    }
    for (const tree of trees) observer.observe(tree, { childList: true, subtree: true });
  };
  const get = (root, owner) => owners.get(root)?.get(owner) || null;
  const create = (root, owner) => {
    const current = get(root, owner);
    if (current) return current;
    const cleanups = new Set(), listeners = new Set(), timers = new Map(), frames = new Map();
    const lifetime = {
      root, trees: [], active: true,
      own(release) {
        if (!lifetime.active) { release(); return () => {}; }
        let active = true;
        const cleanup = () => {
          if (!active) return;
          active = false; cleanups.delete(cleanup); release();
        };
        cleanups.add(cleanup);
        return cleanup;
      },
      listen(target, name, listener, options) {
        if (!lifetime.active || !target?.addEventListener) return () => {};
        const bound = function (...args) {
          if (!lifetime.active) return;
          if (!root.isConnected) { lifetime.dispose(); return; }
          return listener.apply(this, args);
        };
        target.addEventListener(name, bound, options);
        const entry = { target, inTree: !!target.nodeType && !!root.contains?.(target), release: null };
        entry.release = lifetime.own(() => { target.removeEventListener?.(name, bound, options); listeners.delete(entry); });
        listeners.add(entry);
        return entry.release;
      },
      prune() { for (const entry of Array.from(listeners)) if (entry.inTree && !root.contains(entry.target)) entry.release(); },
      timeout(callback, delay) {
        if (!lifetime.active) return 0;
        const timer = window.setTimeout(() => {
          timers.get(timer)?.(); timers.delete(timer);
          if (lifetime.active && root.isConnected) callback();
        }, delay);
        timers.set(timer, lifetime.own(() => { window.clearTimeout(timer); timers.delete(timer); }));
        return timer;
      },
      clearTimeout(timer) { timers.get(timer)?.(); },
      frame(callback) {
        if (!lifetime.active) return 0;
        const frame = requestAnimationFrame(timestamp => {
          frames.get(frame)?.(); frames.delete(frame);
          if (lifetime.active && root.isConnected) callback(timestamp);
        });
        frames.set(frame, lifetime.own(() => { cancelAnimationFrame(frame); frames.delete(frame); }));
        return frame;
      },
      cancelFrame(frame) { frames.get(frame)?.(); },
      observe(target, options, callback) {
        const owned = new MutationObserver(callback);
        owned.observe(target, options);
        lifetime.own(() => owned.disconnect());
        return owned;
      },
      dispose() {
        if (!lifetime.active) return;
        lifetime.active = false;
        for (const cleanup of Array.from(cleanups)) cleanup();
        mounted.delete(lifetime);
        const map = owners.get(root); map?.delete(owner);
        if (!map?.size) owners.delete(root);
        watch();
      }
    };
    const map = owners.get(root) || new Map();
    map.set(owner, lifetime); owners.set(root, map); mounted.add(lifetime); watch();
    return lifetime;
  };
  window[key] = Object.freeze({ create, get });
})();
