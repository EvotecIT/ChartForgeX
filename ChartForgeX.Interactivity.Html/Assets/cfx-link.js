/*! ChartForgeX linked views: wires two views with the LinkedView methods and linkchange events (ChartForgeX charts,
    CanopyX grids and trees, or any object of that shape). Applied links never echo. */
(function (w) {
  "use strict";
  if (w.CfxLink) return;
  var clickModes = new WeakMap();
  function selectClicks(api) {
    var state = clickModes.get(api), active = true;
    if (!state) { state = { count: 0, value: api.selectOnClick }; clickModes.set(api, state); }
    state.count++; api.selectOnClick = true;
    return function () {
      if (!active) return;
      active = false;
      if (--state.count === 0) { api.selectOnClick = state.value; clickModes.delete(api); }
    };
  }
  function quiet(promise) { if (promise && promise.catch) promise.catch(function () { }); }
  function iso(ms) { return new Date(ms).toISOString(); }
  function crosshair(api) { return api.timeX && api.svg.hasAttribute("data-cfx-crosshair"); }
  function resolve(view, host) {
    if (view && !view.setHighlight && w.CfxEnhance) view = w.CfxEnhance.get(view) || view;
    var api = view && view.canopyx || view, el = host || (view && view.nodeType === 1 ? view : api.svg || api.host || api.element);
    var chart = !!api.svg;
    return { api: api, el: el, id: api.view || el && el.id || "view", label: api.label || el && el.id || "view",
      change: chart ? "cfx:linkchange" : "canopyx:linkchange", filter: chart ? "cfx:externalfilter" : "canopyx:externalfilter" };
  }
  // A CanopyX host that has not mounted yet is linked on its canopyx:ready.
  function unmounted(view) { return view && view.nodeType === 1 && !view.canopyx && !view.cfx && !(w.CfxEnhance && w.CfxEnhance.get(view)); }
  function canopy(view) { var inner = unmounted(view) && !view.hasAttribute("data-canopyx-host") && view.querySelector("[data-canopyx-host]"); return inner || view; }
  function connect(from, to, options) {
    var o = options || {};
    to = canopy(to);
    if (unmounted(to)) {
      var off = null, wait = function () { to.removeEventListener("canopyx:ready", wait); if (to.canopyx) off = connect(from, to, o); };
      to.addEventListener("canopyx:ready", wait);
      return function disconnect() { to.removeEventListener("canopyx:ready", wait); if (off) off(); };
    }
    var a = resolve(from, o.sourceHost), b = resolve(to, o.host);
    var fields = [].concat(o.field || []), field = fields[0], mode = o.mode || "filter", source = o.source || a.id, label = o.sourceLabel || a.label;
    var targetSource = o.targetSource || o.gridSource || b.id, targetLabel = o.targetLabel || o.gridLabel || b.label;
    if (o.events) { b.change = o.events.change || b.change; b.filter = o.events.filter || b.filter; }
    var releaseClicks = a.api.svg ? selectClicks(a.api) : null;
    if (a.api.svg) {
      a.api.xField = o.xField || a.api.xField || (crosshair(a.api) ? field : null);
    }
    if (b.api.svg && !b.api.xField && crosshair(b.api)) b.api.xField = o.xField || field;
    function text(t, f) { var s = (t.texts || {})[f] || []; return s.slice(0, 3).join(", ") + (s.length > 3 ? " +" + (s.length - 3) : ""); }
    function forward(e) {
      var t = e.detail, r = t.range, filter = null;
      if (mode === "highlight") return quiet(t.action === "clear" ? b.api.clearHighlight() : b.api.setHighlight(t.keys, { source: label }));
      if (t.action !== "clear") {
        if (r && (!field || r.column === field)) filter = [{ field: field || r.column, range: [r.from, typeof r.to === "string" ? iso(Date.parse(r.to) - 1) : r.to], label: t.label }];
        else filter = fields.filter(function (f) { return t.values[f] && t.values[f].length; }).map(function (f) {
          return { field: f, values: t.values[f].slice(0, 1000), label: fields.length > 1 ? text(t, f) : t.label };
        });
      }
      if (filter && filter.length) filter.forEach(function (f) { f.sourceLabel = label; if (!f.label) delete f.label; });
      else filter = null;
      quiet(b.api.setExternalFilter(source, filter && filter.length === 1 ? filter[0] : filter));
    }
    function back(e) {
      var t = e.detail;
      if (t.scope === "select") return quiet(t.action === "clear" ? a.api.clearHighlight() : a.api.setHighlight(t.keys, { source: targetLabel }));
      quiet(a.api.setExternalFilter(targetSource, t.action === "clear" ? null : Object.keys(t.values).map(function (k) {
        return { field: k, values: t.values[k], sourceLabel: targetLabel };
      }).filter(function (f) { return f.values.length; })));
    }
    // A reader who removes this source's chip has cleared the selection that made it.
    function removed(e) {
      var t = e.detail;
      if (t.reason === "user" && !t.filters.some(function (f) { return f.source === source; }) && a.api.clearSelection) a.api.clearSelection({ silent: true });
    }
    a.el.addEventListener(a.change, forward);
    if (b.el) { b.el.addEventListener(b.change, back); b.el.addEventListener(b.filter, removed); }
    if (b.api.startLinkEvents) quiet(b.api.startLinkEvents());
    return function disconnect() {
      if (releaseClicks) releaseClicks();
      a.el.removeEventListener(a.change, forward);
      if (b.el) { b.el.removeEventListener(b.change, back); b.el.removeEventListener(b.filter, removed); }
    };
  }
  w.CfxLink = { connect: connect };
})(window);
