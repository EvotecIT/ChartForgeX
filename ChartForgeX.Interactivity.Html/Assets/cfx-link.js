/*! ChartForgeX linked views: wires two views that implement the LinkedView methods (setExternalFilter, clearExternalFilters,
    getExternalFilters, setHighlight, clearHighlight, getHighlight) and report reader changes as linkchange events.
    ChartForgeX charts, CanopyX grids and trees, or any host object with the same shape. Applied links never echo. */
(function (w) {
  "use strict";
  if (w.CfxLink) return;
  function quiet(promise) { if (promise && promise.catch) promise.catch(function () { }); }
  function iso(ms) { return new Date(ms).toISOString(); }
  // A time crosshair chart reports its X as the linked field; other charts link through their marks' fields.
  function crosshair(api) { return api.timeX && api.svg.hasAttribute("data-cfx-crosshair"); }
  // A view is a ChartForgeX chart api (or its svg/host), a CanopyX host element (.canopyx), or any LinkedView object.
  function resolve(view, host) {
    if (view && !view.setHighlight && w.CfxEnhance) view = w.CfxEnhance.get(view) || view;
    var api = view && view.canopyx || view, el = host || (view && view.nodeType === 1 ? view : api.svg || api.host || api.element);
    var chart = !!api.svg;
    return { api: api, el: el, id: api.view || el && el.id || "view", label: api.label || el && el.id || "view",
      change: chart ? "cfx:linkchange" : "canopyx:linkchange", filter: chart ? "cfx:externalfilter" : "canopyx:externalfilter" };
  }
  function connect(from, to, options) {
    var o = options || {}, a = resolve(from, o.sourceHost), b = resolve(to, o.host);
    var field = o.field, mode = o.mode || "filter", source = o.source || a.id, label = o.sourceLabel || a.label;
    var targetSource = o.targetSource || o.gridSource || b.id, targetLabel = o.targetLabel || o.gridLabel || b.label;
    if (o.events) { b.change = o.events.change || b.change; b.filter = o.events.filter || b.filter; }
    if (a.api.svg) {
      a.api.selectOnClick = true;
      a.api.xField = o.xField || a.api.xField || (crosshair(a.api) ? field : null);
    }
    if (b.api.svg && !b.api.xField && crosshair(b.api)) b.api.xField = o.xField || field;
    // Source to target: the source's selection filters or highlights the target.
    function forward(e) {
      var t = e.detail, r = t.range, filter = null;
      if (mode === "highlight") return quiet(t.action === "clear" ? b.api.clearHighlight() : b.api.setHighlight(t.keys, { source: label }));
      if (t.action !== "clear") {
        if (r && (!field || r.column === field)) filter = { field: field || r.column, range: [r.from, typeof r.to === "string" ? iso(Date.parse(r.to) - 1) : r.to] };
        else if (field && t.values[field] && t.values[field].length) filter = { field: field, values: t.values[field].slice(0, 1000) };
      }
      if (filter) { filter.label = t.label; filter.sourceLabel = label; }
      quiet(b.api.setExternalFilter(source, filter));
    }
    // Target to source: the target's selection highlights the source; its own conditions filter the source.
    function back(e) {
      var t = e.detail;
      if (t.scope === "select") return quiet(t.action === "clear" ? a.api.clearHighlight() : a.api.setHighlight(t.keys, { source: targetLabel }));
      quiet(a.api.setExternalFilter(targetSource, t.action === "clear" ? null : Object.keys(t.values).map(function (k) {
        return { field: k, values: t.values[k], sourceLabel: targetLabel };
      }).filter(function (f) { return f.values.length; })));
    }
    // A reader who removes this source's chip in the target has cleared the selection that made it.
    function removed(e) {
      var t = e.detail;
      if (t.reason === "user" && !t.filters.some(function (f) { return f.source === source; }) && a.api.clearSelection) a.api.clearSelection({ silent: true });
    }
    a.el.addEventListener(a.change, forward);
    if (b.el) { b.el.addEventListener(b.change, back); b.el.addEventListener(b.filter, removed); }
    if (b.api.startLinkEvents) quiet(b.api.startLinkEvents());
    return function disconnect() {
      a.el.removeEventListener(a.change, forward);
      if (b.el) { b.el.removeEventListener(b.change, back); b.el.removeEventListener(b.filter, removed); }
    };
  }
  w.CfxLink = { connect: connect };
})(window);
