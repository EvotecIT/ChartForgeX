/*! ChartForgeX shared interaction parts: one page tooltip (value first, series second, a line key), the polite live region,
    and small DOM helpers. Used by cfx-enhance.js and available to any other ChartForgeX runtime. */
(function (w, d) {
  "use strict";
  if (w.CfxShared) return;
  var NS = "http://www.w3.org/2000/svg", tip, live;
  function make(name, cls, parent, html) {
    var el = html ? d.createElement(name) : d.createElementNS(NS, name);
    if (cls) el.setAttribute("class", cls);
    if (parent) parent.appendChild(el);
    return el;
  }
  function set(el, values) { for (var key in values) el.setAttribute(key, values[key]); return el; }
  function fire(el, type, detail) { el.dispatchEvent(new CustomEvent(type, { bubbles: true, composed: true, detail: detail })); }
  function parse(text, fallback) { try { return text ? JSON.parse(text) : fallback; } catch (e) { return fallback; } }
  function format(text, values) { return String(text).replace(/\{(\w+)\}/g, function (_, key) { return values[key] == null ? "" : values[key]; }); }
  function announce(text) {
    if (!live) live = set(make("div", "cfx-e-live", d.body, 1), { "aria-live": "polite", role: "status" });
    live.textContent = "";
    setTimeout(function () { live.textContent = text; }, 40);
  }
  function slot(root, name, text) {
    var el = root.querySelector('[data-cfx-slot="' + name + '"]');
    if (el && text != null) el.textContent = text;
    return el;
  }
  // content: { head, rows: [{ color, value, name }], hint }. Host templates are cloned and filled with text only.
  function show(content, x, y, template) {
    if (!tip) tip = set(make("div", "cfx-e-tip", d.body, 1), { role: "tooltip", id: "cfx-e-tip" });
    tip.textContent = "";
    var rows = content.rows || [];
    if (template) {
      var copy = template.content.cloneNode(true), row = slot(copy, "row");
      slot(copy, "head", content.head || ""); slot(copy, "hint", content.hint || "");
      rows.forEach(function (r) {
        if (!row) return;
        var line = row.cloneNode(true), key = slot(line, "key");
        slot(line, "value", r.value); slot(line, "name", r.name);
        if (key && r.color) key.style.background = r.color;
        row.parentNode.insertBefore(line, row);
      });
      if (row) row.parentNode.removeChild(row);
      tip.appendChild(copy);
    } else {
      if (content.head) make("div", "cfx-e-tip-head", tip, 1).textContent = content.head;
      rows.forEach(function (r) {
        var line = make("div", "cfx-e-tip-row", tip, 1), key = make("span", "cfx-e-tip-key", line, 1);
        if (r.color) key.style.background = r.color;
        make("strong", "cfx-e-tip-value", line, 1).textContent = r.value;
        make("span", "cfx-e-tip-name", line, 1).textContent = r.name || "";
      });
      if (content.hint) make("div", "cfx-e-tip-hint", tip, 1).textContent = content.hint;
    }
    tip.hidden = false;
    // Prefer above-right of the pointer, flip to the left or below, and always clamp to the viewport.
    var box = tip.getBoundingClientRect(), vw = d.documentElement.clientWidth, vh = d.documentElement.clientHeight;
    var left = x + 14, top = y - box.height - 12;
    if (left + box.width > vw - 8) left = x - box.width - 14;
    if (top < 8) top = y + 16;
    tip.style.left = Math.max(8, Math.min(left, vw - box.width - 8)) + "px";
    tip.style.top = Math.max(8, Math.min(top, vh - box.height - 8)) + "px";
  }
  function hide() { if (tip) tip.hidden = true; }
  d.addEventListener("keydown", function (e) { if (e.key === "Escape") hide(); });
  w.CfxShared = { make: make, set: set, fire: fire, parse: parse, format: format, announce: announce, tip: { id: "cfx-e-tip", show: show, hide: hide } };
})(window, document);

/*! ChartForgeX enhance runtime: upgrades any ChartForgeX SVG in place from its DOM contract (docs/dom-contract.md) and never
    re-renders it. Every family gets states, tooltips, roving keyboard focus, selection, legend toggles and the LinkedView API;
    family gestures are behaviours registered with CfxEnhance.behaviour (see cfx-enhance.behaviours.js). Needs cfx-shared.js. */
(function (w, d) {
  "use strict";
  if (w.CfxEnhance) return;
  var S = w.CfxShared, make = S.make, set = S.set, fire = S.fire, parse = S.parse, format = S.format, announce = S.announce;
  var LEGEND = '[data-cfx-role="legend-entry"]', DATE = /^-?\d{4}-\d\d-\d\dT/, uid = 0, charts = [], behaviours = [], tooltips = [];
  var labels = {
    hint: "Click to select", hidden: "{name} hidden", shown: "{name} shown", selected: "{label} selected", cleared: "Selection cleared",
    highlighted: "{count} highlighted from {source}", unhighlighted: "Highlight cleared", filtered: "Filtered by {source}", unfiltered: "Linked filters removed"
  };
  function all(root, selector) { return Array.prototype.slice.call(root.querySelectorAll(selector)); }
  function attr(el, name) { return el.getAttribute(name); }
  function iso(ms) { return new Date(ms).toISOString(); }
  function number(v) { return typeof v === "number" ? v : DATE.test(v) ? Date.parse(v) : +v; }

  function enhance(svg, options) {
    if (svg.cfx) return svg.cfx;
    var o = options || {}, L = {}, key, hooks = [];
    for (key in labels) L[key] = (o.labels && o.labels[key]) || labels[key];
    var data = parse((svg.querySelector("metadata[data-cfx-enhance-data]") || {}).textContent, {});
    var size = (attr(svg, "data-cfx-size") || "").split(" ").map(Number), plotBox = (attr(svg, "data-cfx-plot") || "").split(" ").map(Number);
    var title = svg.querySelector("title"), hooked = svg.querySelector("[data-cfx-mark]");
    var MARK = hooked ? "[data-cfx-mark]" : "[data-cfx-series][data-cfx-point]:not(" + LEGEND + ")";
    var api = svg.cfx = {
      svg: svg, view: o.view || svg.id || attr(svg, "data-cfx-view") || "cfx-chart-" + (++uid),
      label: o.label || attr(svg, "aria-label") || (title && title.textContent) || "Chart",
      selectOnClick: !!o.selectOnClick, filterMode: o.filterMode || "dim", xField: o.xField || null,
      timeX: attr(svg, "data-cfx-x-kind") === "time", behaviours: []
    };
    var template = o.template || d.querySelector('template[data-cfx-tooltip="' + api.view + '"]') || d.querySelector('template[data-cfx-tooltip=""]');
    var series = {}, island = {}, bySeries = {}, order = [];
    (data.series || []).forEach(function (s) { series[s.i] = { name: s.name, color: s.color, key: s.key }; });
    (data.points || []).forEach(function (p) { island[p.s + ":" + p.p] = p; });
    if (attr(svg, "role") === "img") svg.setAttribute("role", "group");

    var marks = all(svg, MARK).filter(function (el) { return !el.querySelector(MARK); }).map(function (el, n) {
      var s = +attr(el, "data-cfx-series"), p = +(attr(el, "data-cfx-point") || attr(el, "data-cfx-source-point") || n), point = island[s + ":" + p] || {};
      var instant = attr(el, "data-cfx-x-instant") || point.xi, raw = attr(el, "data-cfx-x"), note = el.querySelector("title"), aria = attr(el, "aria-label");
      if (note && note.parentNode === el) {
        // One tooltip: the authored title becomes the tooltip hint and, without another name, the accessible name.
        if (!aria) el.setAttribute("aria-label", aria = note.textContent);
        el.removeChild(note);
      } else note = null;
      var value = attr(el, "data-cfx-formatted-value") || attr(el, "data-cfx-range-label") || point.v || attr(el, "data-cfx-value") || attr(el, "data-cfx-y") || "";
      var head = attr(el, "data-cfx-display-label") || attr(el, "data-cfx-category-label") || attr(el, "data-cfx-formatted-x")
        || attr(el, "data-cfx-formatted-instant") || attr(el, "data-cfx-label") || "";
      var m = {
        el: el, n: n, s: s, p: p, role: attr(el, "data-cfx-role") || "", raw: raw == null ? NaN : +raw,
        x: instant ? Date.parse(instant) : raw == null ? NaN : +raw, head: value ? head : "", value: value || aria || head,
        keys: parse(attr(el, "data-cfx-keys"), point.k || []), fields: parse(attr(el, "data-cfx-fields"), {}),
        href: attr(el, "href") || attr(el, "xlink:href"), note: note ? note.textContent : ""
      };
      if (value && aria !== value) m.aria = aria;
      m.box = (attr(el, "data-cfx-bounds") || "").split(" ").map(Number);
      m.xv = instant ? iso(m.x) : raw == null ? null : m.raw;
      if (!series[s]) series[s] = { key: attr(el, "data-cfx-series-key") };
      (bySeries[s] = bySeries[s] || []).push(m);
      // A point mark (a lone marker, or a line point without one) takes a ring, like its static selected look.
      m.dot = !!el.querySelector('[data-cfx-role="marker"],[data-cfx-role$="-marker"]');
      el._cfx = m;
      el.setAttribute("tabindex", "-1");
      if (!el.hasAttribute("role")) el.setAttribute("role", "img");
      return m;
    });
    for (key in bySeries) { order.push(+key); bySeries[key].sort(function (a, b) { return (a.raw - b.raw) || (a.p - b.p); }); }
    order.sort(function (a, b) { return a - b; });
    // Legends toggle a series, or one mark when the entry names a point (pie and donut slices).
    var legends = all(svg, LEGEND).map(function (el) {
      var target = attr(el, "data-cfx-legend-for"), s = target == null ? null : +target, p = attr(el, "data-cfx-point");
      if (s == null) for (key in series) if (series[key].key && series[key].key === attr(el, "data-cfx-series-key")) s = +key;
      var only = p == null ? null : (bySeries[s] || []).filter(function (m) { return m.p === +p; })[0];
      return { el: el, s: s, mark: only };
    }).filter(function (l) { return l.s != null && series[l.s] && (l.mark || attr(l.el, "data-cfx-point") == null); });
    order.forEach(function (s) {
      var info = series[s], group = bySeries[s][0].el.closest("[data-cfx-series-name]");
      var legend = legends.filter(function (l) { return l.s === s && !l.mark; })[0];
      var row = all(svg, '[data-cfx-role="schedule-row"]').filter(function (r) { return attr(r, "data-cfx-lane-id") === info.key; })[0];
      if (!info.name) info.name = group ? attr(group, "data-cfx-series-name") : legend ? attr(legend.el, "aria-label") : row ? attr(row, "aria-label") || "" : "";
    });
    var tops = all(svg, "[data-cfx-series]").filter(function (el) { return !el.parentNode.closest("[data-cfx-series]") && !el.matches(LEGEND); })
      .map(function (el) { return { el: el, s: +attr(el, "data-cfx-series") }; });
    // Series ink outside the marks (lines, area washes) dims with them, so a line never stays at full ink.
    var inks = [].concat.apply([], tops.map(function (t) {
      return all(t.el, "path,line,polyline,polygon,rect,circle,ellipse").filter(function (n) { return !n.closest(MARK) && !n.closest("defs,clipPath,mask,pattern"); });
    }));
    var current = marks.filter(function (m) { return attr(m.el, "aria-current") === "true"; })[0] || marks[0];
    if (current) current.el.setAttribute("tabindex", "0");

    var layer = set(make("g", "cfx-e-layer", svg), { "aria-hidden": "true", "pointer-events": "none" });
    var states = make("g", null, layer);
    var geometry = null, hover = null, selection = [], highlight = new Set(), filters = {}, emphasis = null;
    var keyboard = false, pointerType = "", tapped = null;

    // Ask attached behaviours in registration order; the first defined answer wins.
    function ask(name) {
      var args = Array.prototype.slice.call(arguments, 1);
      for (var i = 0; i < hooks.length; i++) if (hooks[i][name]) { var r = hooks[i][name].apply(null, args); if (r !== undefined) return r; }
    }
    function rects() {
      if (!geometry) geometry = marks.map(function (m) {
        var r = m.el.getBoundingClientRect(), b = m.box;
        // Unpainted marks (line points without markers) use their logical anchor from data-cfx-bounds.
        var dot = m.dot && Math.abs(r.width - r.height) <= 1.5;
        if (!r.width && !r.height && b.length === 4) { var a = client(b[0], b[1]), z = client(b[0] + b[2], b[1] + b[3]); r = { left: a.x, top: a.y, right: z.x, bottom: z.y }; dot = true; }
        return { l: r.left, t: r.top, r: r.right, b: r.bottom, x: (r.left + r.right) / 2, y: (r.top + r.bottom) / 2, dot: dot };
      });
      return geometry;
    }
    function user(x, y) {
      var ctm = svg.getScreenCTM(), pt = svg.createSVGPoint();
      pt.x = x; pt.y = y;
      return ctm ? pt.matrixTransform(ctm.inverse()) : pt;
    }
    function box(r) { var a = user(r.l, r.t), b = user(r.r, r.b); return { x: a.x, y: a.y, w: b.x - a.x, h: b.y - a.y }; }
    // Logical scene coordinates to client pixels: fluid roots stretch X only; viewBox roots use the screen matrix.
    function client(x, y) {
      var r = svg.getBoundingClientRect(), pt;
      if (!svg.hasAttribute("viewBox")) return { x: r.left + x * r.width / (size[0] || r.width), y: r.top + y * r.height / (size[1] || r.height) };
      pt = svg.createSVGPoint(); pt.x = x; pt.y = y;
      return pt.matrixTransform(svg.getScreenCTM());
    }
    function plot() {
      var r = svg.getBoundingClientRect();
      if (plotBox.length < 4 || !size[0]) return { l: r.left, t: r.top, r: r.right, b: r.bottom };
      var a = client(plotBox[0], plotBox[1]), b = client(plotBox[0] + plotBox[2], plotBox[1] + plotBox[3]);
      return { l: a.x, t: a.y, r: b.x, b: b.y };
    }
    function inside(p, e, pad) { pad = pad || 0; return e.clientX >= p.l - pad && e.clientX <= p.r + pad && e.clientY >= p.t - pad && e.clientY <= p.b + pad; }
    function visible(m) { return !series[m.s].hidden && !m.off && !(m.out && api.filterMode === "hide"); }
    function color(s) {
      var info = series[s];
      if (info.color == null) {
        // A line's stroke or a mark's solid fill; translucent area fills and gradients are not the series colour.
        var owners = [bySeries[s][0].el].concat(tops.filter(function (t) { return t.s === s; }).map(function (t) { return t.el; }));
        info.color = "";
        owners.some(function (owner) {
          return all(owner, "rect,path,circle,ellipse,polygon").some(function (shape) {
            var style = getComputedStyle(shape), solid = /^(#|rgb\()/.test(style.fill);
            info.color = style.stroke !== "none" && !solid ? style.stroke : solid ? style.fill : "";
            return !!info.color;
          });
        });
      }
      return info.color;
    }
    function markOf(target) {
      var el = target && target.closest && target.closest(MARK);
      while (el && !el._cfx) el = el.parentNode.closest(MARK);
      return el ? el._cfx : null;
    }
    function hit(x, y) {
      // Targets are at least 24px; a pointer inside a painted mark wins over a nearby enlarged one.
      var best = null, distance = Infinity;
      rects().forEach(function (g, i) {
        var m = marks[i];
        if (!visible(m) || Math.abs(x - g.x) > Math.max(12, (g.r - g.l) / 2) || Math.abs(y - g.y) > Math.max(12, (g.b - g.t) / 2)) return;
        var dist = Math.abs(x - g.x) + Math.abs(y - g.y) - (x >= g.l && x <= g.r && y >= g.t && y <= g.b ? 1e6 : 0);
        if (dist < distance) { distance = dist; best = m; }
      });
      return best;
    }
    function content(ms) {
      var c = {
        head: ms[0].head || !series[ms[0].s].name && ms[0].aria || "", rows: ms.map(function (m) { return { color: color(m.s), value: m.value, name: series[m.s].name || "" }; }),
        hint: ms.length === 1 && ms[0].note || (api.selectOnClick || !ms[0].href ? L.hint : "")
      };
      c = ask("tooltip", ms, c) || c;
      tooltips.forEach(function (t) { if (t.match(kit, ms)) c = t.format(ms, c, kit) || c; });
      return o.tooltip ? o.tooltip(ms, c, kit) || c : c;
    }
    function tipFor(ms, x, y) { if (ms.length) S.tip.show(content(ms), x, y, template); else S.tip.hide(); }
    function labelFor(ms) {
      var parts = ms.slice(0, 3).map(function (m) { var name = series[m.s].name; return m.head && name && order.length > 1 ? m.head + " · " + name : m.head || name || m.value; });
      return parts.join(", ") + (ms.length > 3 ? " +" + (ms.length - 3) : "");
    }
    // What a reader calls a field value: a date reads as the mark's own label, a value shared by the whole series as the
    // series name, anything else as itself. Link targets use it for chips; the raw values stay the filter.
    function said(m, name, v) {
      var own = bySeries[m.s], info = series[m.s];
      if (DATE.test(v)) return m.head || String(v);
      return info.name && own.every(function (x) { return String(x.fields[name]) === String(v); }) ? info.name : String(v);
    }
    function detail(ms, range, label) {
      var keys = [], seen = {}, values = {}, texts = {}, extra = ms.length && ask("describe", ms) || {};
      ms.forEach(function (m) {
        m.keys.forEach(function (k) { if (!seen[k]) { seen[k] = 1; keys.push(k); } });
        var fields = {}, name;
        for (name in m.fields) fields[name] = m.fields[name];
        if (api.xField && fields[api.xField] === undefined && m.xv != null) fields[api.xField] = m.xv;
        for (name in fields) {
          var target = values[name] = values[name] || [], text = texts[name] = texts[name] || [], field = name;
          [].concat(fields[name]).forEach(function (v) { if (target.indexOf(v) < 0) { target.push(v); text.push(said(m, field, v)); } });
        }
      });
      var result = { view: api.view, action: ms.length ? "select" : "clear", scope: "select", keys: keys, values: values, texts: texts, label: label || extra.label || labelFor(ms) };
      if (ms.length && (range || extra.range)) result.range = range || extra.range;
      return result;
    }
    function value(m, field) { var v = m.fields[field]; return v === undefined && field === api.xField ? m.xv : v; }
    function passes(m) {
      for (var source in filters) {
        if (!filters[source].every(function (f) {
          var v = value(m, f.field);
          return v == null || [].concat(v).some(function (x) {
            // Dates match by instant: CanopyX reports "…T00:00:00Z" where a chart writes "…T00:00:00.000Z".
            if (f.values) return f.values.some(function (y) { return String(y) === String(x) || DATE.test(x) && DATE.test(y) && Date.parse(x) === Date.parse(y); });
            var n = number(x);
            return (f.range[0] == null || n >= number(f.range[0])) && (f.range[1] == null || n <= number(f.range[1]));
          });
        })) return false;
      }
      return true;
    }

    function paint() {
      var selected = selection.length > 0, linked = highlight.size > 0;
      states.textContent = "";
      // Halos paint under every outline so neighbouring selected marks never cover each other's ink.
      var halos = make("g", null, states), rings = make("g", null, states), active = selected || linked;
      // A brushed range draws its own selection over the series, so the lines it covers stay readable.
      var band = selected && !linked && ask("outline", selection[0]) === false;
      inks.forEach(function (n) { n.classList.toggle("cfx-e-dim", active && !band); });
      tops.forEach(function (t) {
        t.el.classList.toggle("cfx-e-hidden", !!series[t.s].hidden);
        t.el.classList.toggle("cfx-e-faded", emphasis != null && t.s !== emphasis);
      });
      marks.forEach(function (m) {
        var on = selection.indexOf(m) >= 0, link = linked && m.keys.some(function (k) { return highlight.has(k); }), c = m.el.classList;
        m.out = !passes(m);
        c.toggle("cfx-e-selected", on); c.toggle("cfx-e-linked", link);
        // Selected and linked marks share the static look; every other mark dims while either is active.
        c.toggle("cfx-e-dim", active && !on && !link);
        c.toggle("cfx-e-filtered", m.out && api.filterMode !== "hide");
        c.toggle("cfx-e-gone", !!m.off || m.out && api.filterMode === "hide");
        if (!visible(m) || !(on || link) || on && !link && ask("outline", m) === false) return;
        // The static mark-selected-outline: a 2px ink outline over a 4px surface halo, offset 2px around the whole mark,
        // or a ring around a point. Outlines may overflow the plot; the layer is never clipped.
        var g = rects()[m.n], s = outline(g, 2), kind = g.dot ? "circle" : "rect";
        set(make(kind, "cfx-e-halo", halos), s);
        set(make(kind, on ? "cfx-e-sel" : "cfx-e-sel cfx-e-ring", rings), s);
      });
      var focus = d.activeElement;
      if (keyboard && focus && focus !== svg && svg.contains(focus)) {
        var fm = markOf(focus), r = fm ? null : focus.getBoundingClientRect(), fg = fm ? rects()[fm.n] : { l: r.left, t: r.top, r: r.right, b: r.bottom };
        // Focus sits outside the selection halo in the focus-ring colour, so the two never look alike.
        set(make(fg.dot ? "circle" : "rect", "cfx-e-focus", states), outline(fg, 5));
      }
      legends.forEach(function (l) {
        var off = l.mark ? !!l.mark.off : !!series[l.s].hidden;
        l.el.setAttribute("aria-pressed", String(!off));
        l.el.classList.toggle("cfx-e-legend-off", off);
      });
      ask("paint");
    }
    // A rectangle `gap` px outside a mark's client box, or a circle for a point.
    function outline(g, gap) {
      var b = box(g);
      if (g.dot) return { cx: b.x + b.w / 2, cy: b.y + b.h / 2, r: Math.max(b.w, b.h) / 2 + gap };
      return { x: b.x - gap, y: b.y - gap, width: b.w + gap * 2, height: b.h + gap * 2, rx: gap > 2 ? 6 : Math.min(4, (Math.min(b.w, b.h) + 4) / 2) };
    }
    function setHover(m, x, y) {
      if (hover && hover !== m) hover.el.classList.remove("cfx-e-hover");
      var changed = hover !== m;
      hover = m;
      if (!m) { S.tip.hide(); return; }
      m.el.classList.add("cfx-e-hover");
      tipFor([m], x, y);
      if (changed) fire(svg, "cfx:hover", detail([m]));
    }
    function same(a, b) { return a.length === b.length && a.every(function (m) { return b.indexOf(m) >= 0; }); }
    function choose(ms, add, range, label) {
      if (add) ms.forEach(function (m) { var i = selection.indexOf(m); if (i >= 0) selection.splice(i, 1); else selection.push(m); });
      else selection = same(selection, ms) && !range ? [] : ms.slice();
      ask("select", selection);
      paint();
      var result = detail(selection, range, label);
      fire(svg, "cfx:select", result);
      fire(svg, "cfx:linkchange", result);
      announce(selection.length ? format(L.selected, { label: result.label }) : L.cleared);
    }
    function clear() {
      S.tip.hide(); setHover(null); ask("leave");
      if (selection.length) choose([]);
    }
    function seriesMarks(s) { return (bySeries[s] || []).filter(visible); }
    function group(m) { return ask("group", m) || [m]; }
    function focusOn(m) {
      var g = rects()[m.n];
      if (!ask("focus", m, g.r, g.t)) setHover(m, g.r, g.t);
    }
    function go(m) {
      marks.forEach(function (x) { x.el.setAttribute("tabindex", x === m ? "0" : "-1"); });
      keyboard = true;
      m.el.focus();
      focusOn(m);
      var c = content(group(m));
      announce((c.head ? c.head + ", " : "") + c.rows.map(function (r) { return (r.name ? r.name + " " : "") + r.value; }).join(", "));
    }
    function move(m, step) {
      var own = seriesMarks(m.s), i = own.indexOf(m) + step;
      return own[Math.max(0, Math.min(own.length - 1, i))];
    }
    function switchSeries(m, step) {
      var shown = order.filter(function (s) { return seriesMarks(s).length; }), i = shown.indexOf(m.s) + step;
      if (i < 0 || i >= shown.length) return m;
      return seriesMarks(shown[i]).reduce(function (best, x) {
        function gap(y) { return isNaN(m.x) ? Math.abs(y.p - m.p) : Math.abs(y.x - m.x); }
        return gap(x) < gap(best) ? x : best;
      });
    }
    function toggle(l, isolate) {
      var name = l.mark ? l.mark.head || l.mark.value : series[l.s].name;
      if (l.mark) {
        var points = bySeries[l.s], alone = isolate && points.every(function (m) { return m === l.mark ? !m.off : m.off; });
        if (isolate) points.forEach(function (m) { m.off = alone ? false : m !== l.mark; }); else l.mark.off = !l.mark.off;
      } else if (isolate) {
        var only = !series[l.s].hidden && order.every(function (i) { return i === l.s || series[i].hidden; });
        order.forEach(function (i) { series[i].hidden = only ? false : i !== l.s; });
      } else series[l.s].hidden = !series[l.s].hidden;
      geometry = null; paint();
      announce(format((l.mark ? l.mark.off : series[l.s].hidden) ? L.hidden : L.shown, { name: name }));
      fire(svg, "cfx:legend", { view: api.view, hidden: order.filter(function (i) { return series[i].hidden; }) });
    }

    legends.forEach(function (l) {
      set(l.el, { role: "button", tabindex: "0", "aria-pressed": "true" });
      function emphasise(on) { if (l.mark) return; emphasis = on ? l.s : null; paint(); }
      l.el.addEventListener("click", function (e) { e.preventDefault(); toggle(l, e.shiftKey); });
      l.el.addEventListener("keydown", function (e) {
        if (e.key === "Enter" || e.key === " ") { e.preventDefault(); e.stopPropagation(); toggle(l, e.shiftKey); }
      });
      l.el.addEventListener("pointerenter", function () { emphasise(1); });
      l.el.addEventListener("pointerleave", function () { emphasise(0); });
      l.el.addEventListener("focus", function () { emphasise(1); });
      l.el.addEventListener("blur", function () { emphasise(0); });
    });
    function pointAt(e) { if (!ask("pointer", e)) setHover(hit(e.clientX, e.clientY), e.clientX, e.clientY); }
    svg.addEventListener("pointerdown", function (e) {
      keyboard = false; pointerType = e.pointerType;
      if (pointerType === "touch") pointAt(e);
      if (!e.target.closest(LEGEND)) ask("down", e);
    });
    svg.addEventListener("pointermove", function (e) { if (!ask("drag", e) && !e.target.closest(LEGEND)) pointAt(e); });
    svg.addEventListener("pointerup", function (e) { ask("up", e); });
    svg.addEventListener("pointercancel", function (e) { ask("up", e, true); });
    svg.addEventListener("pointerleave", function () { setHover(null); ask("leave"); });
    svg.addEventListener("click", function (e) {
      if (ask("click", e)) { e.preventDefault(); e.stopPropagation(); return; }
      if (e.target.closest(LEGEND)) return;
      var own = markOf(e.target) || hit(e.clientX, e.clientY), ms = ask("targets", e) || (own && visible(own) ? [own] : []);
      var id = ms.map(function (m) { return m.n; }).join();
      if (pointerType === "touch" && ms.length && tapped !== id) { tapped = id; e.preventDefault(); return; }
      tapped = null;
      if (!ms.length) { if (inside(plot(), e)) clear(); return; }
      // Keyboard Enter (detail 0) and modified clicks on a linked mark keep native navigation.
      if (ms.length === 1 && ms[0].href && (!api.selectOnClick || e.detail === 0 || e.ctrlKey || e.metaKey)) return;
      e.preventDefault();
      choose(ms, e.shiftKey);
    }, true);
    svg.addEventListener("keydown", function (e) {
      var m = markOf(e.target), k = e.key, next;
      if (k === "Escape") { clear(); return; }
      if (!m) return;
      next = ask("key", e, m);
      if (next === true) return;
      if (next === undefined) {
        if (k === "ArrowRight" || k === "ArrowLeft") next = move(m, k === "ArrowRight" ? 1 : -1);
        else if (k === "ArrowUp" || k === "ArrowDown") next = switchSeries(m, k === "ArrowUp" ? 1 : -1);
        else if (k === "Home" || k === "End") { var own = seriesMarks(m.s); next = own[k === "Home" ? 0 : own.length - 1]; }
        else if (k === " " || k === "Enter" && !m.href) { e.preventDefault(); choose(group(m), e.shiftKey); return; }
        else return;
      }
      e.preventDefault();
      if (next) go(next);
    });
    svg.addEventListener("focusin", function (e) {
      var m = markOf(e.target), shown = false;
      if (m) {
        marks.forEach(function (x) { x.el.setAttribute("tabindex", x === m ? "0" : "-1"); });
        m.el.setAttribute("aria-describedby", S.tip.id);
      }
      try { shown = e.target.matches(":focus-visible"); } catch (x) { shown = keyboard; }
      if (shown) { keyboard = true; if (m && hover !== m) focusOn(m); }
      paint();
    });
    svg.addEventListener("focusout", function (e) {
      var m = markOf(e.target);
      if (m) m.el.removeAttribute("aria-describedby");
      setTimeout(function () {
        if (!svg.contains(d.activeElement)) { keyboard = false; setHover(null); ask("leave"); }
        paint();
      }, 0);
    });
    function resized() { geometry = null; ask("leave"); paint(); }
    if (w.ResizeObserver) new ResizeObserver(resized).observe(svg);
    w.addEventListener("scroll", function () { geometry = null; }, { capture: true, passive: true });
    w.addEventListener("resize", function () { geometry = null; });

    // LinkedView: the method names and payload shapes CanopyX uses. Applied filters and highlights never emit cfx:linkchange.
    api.setExternalFilter = function (source, filter) {
      if (!/^[\w.:-]{1,128}$/.test(source)) return Promise.reject(new Error("Invalid link source"));
      var next = filter == null ? null : [].concat(filter).map(function (f) {
        if (!f || !f.field || !(f.values ? f.values.length : f.range)) throw new Error("A link filter needs a field and values or a range");
        var copy = { source: source, field: f.field, sourceLabel: f.sourceLabel || source };
        if (f.values) copy.values = f.values.slice(); else copy.range = f.range.slice();
        if (f.label) copy.label = f.label;
        return copy;
      });
      if (next && next.length) filters[source] = next; else delete filters[source];
      paint();
      announce(next ? format(L.filtered, { source: next[0].sourceLabel }) : L.unfiltered);
      fire(svg, "cfx:externalfilter", { filters: api.getExternalFilters(), reason: "api" });
      return Promise.resolve(true);
    };
    api.clearExternalFilters = function (source) {
      if (source) delete filters[source]; else filters = {};
      paint();
      fire(svg, "cfx:externalfilter", { filters: api.getExternalFilters(), reason: "api" });
      return Promise.resolve(true);
    };
    api.getExternalFilters = function () {
      var result = [];
      for (var source in filters) result = result.concat(filters[source].map(function (f) { return JSON.parse(JSON.stringify(f)); }));
      return result;
    };
    api.setHighlight = function (keys, opts) {
      highlight = new Set((keys || []).slice(0, 10000).map(String));
      paint();
      announce(highlight.size ? format(L.highlighted, { count: highlight.size, source: (opts && opts.source) || api.label }) : L.unhighlighted);
      var first = opts && opts.scroll === true && marks.filter(function (m) { return m.el.classList.contains("cfx-e-linked"); })[0];
      if (first) first.el.scrollIntoView({ block: "nearest", inline: "nearest" });
      return Promise.resolve(highlight.size);
    };
    api.clearHighlight = function () { highlight = new Set(); paint(); return Promise.resolve(0); };
    api.getHighlight = function () { return Array.from(highlight); };
    api.startLinkEvents = function () { return Promise.resolve(); };
    api.getSelection = function () { return detail(selection); };
    api.clearSelection = function (opts) {
      if (!selection.length) return;
      if (opts && opts.silent) { selection = []; ask("select", selection); paint(); } else choose([]);
    };
    api.refresh = resized;

    // What a behaviour receives: the chart's model and the same primitives the core uses.
    var kit = {
      api: api, svg: svg, marks: marks, series: series, order: order, bySeries: bySeries, data: data, labels: L, layer: layer,
      rects: rects, user: user, box: box, plot: plot, inside: inside, visible: visible, color: color, hit: hit, markOf: markOf,
      hover: setHover, tip: tipFor, choose: choose, clear: clear, paint: paint, go: go, iso: iso,
      roles: marks.reduce(function (set, m) { set[m.role] = 1; return set; }, {}),
      hovered: function () { return hover; }, selection: function () { return selection; }
    };
    api.attach = function (b) {
      if (api.behaviours.indexOf(b.name) >= 0 || o.behaviours && o.behaviours.indexOf(b.name) < 0) return;
      try {
        if (!b.match(kit)) return;
        hooks.push(b.attach(kit) || {});
        api.behaviours.push(b.name);
        paint();
      } catch (x) { if (w.console) console.error("ChartForgeX behaviour " + b.name + " failed", x); }
    };
    behaviours.forEach(api.attach);
    charts.push(api);
    paint();
    return api;
  }

  function get(el) {
    if (!el) return null;
    if (el.cfx) return el.cfx;
    var svg = el.querySelector && el.querySelector("svg[data-cfx-enhance]");
    return svg ? svg.cfx || enhance(svg) : null;
  }
  // Registers a family gesture or keyboard model: { match(kit) -> boolean, attach(kit) -> hooks }. Charts already
  // enhanced receive it too. Hooks: pointer, down, drag, up, leave, click, targets, key, focus, group, describe, tooltip, select, outline, paint.
  function behaviour(name, definition) {
    var b = { name: name, match: definition.match, attach: definition.attach };
    behaviours = behaviours.filter(function (x) { return x.name !== name; }).concat(b);
    charts.forEach(function (c) { c.attach(b); });
  }
  // Registers a tooltip formatter for marks it matches: match(kit, marks) and format(marks, content, kit) -> content.
  function tooltip(match, formatter) {
    tooltips.push({ match: typeof match === "string" ? function (k, ms) { return ms[0].role === match; } : match, format: formatter });
  }
  function scan(root) { return all(root || d, "svg[data-cfx-enhance]").map(function (svg) { return enhance(svg); }); }
  w.CfxEnhance = { enhance: enhance, scan: scan, get: get, behaviour: behaviour, tooltip: tooltip, labels: labels };
  var script = d.currentScript;
  if (!(script && script.hasAttribute("data-cfx-manual"))) {
    if (d.readyState === "loading") d.addEventListener("DOMContentLoaded", function () { scan(); }); else scan();
  }
})(window, document);

/*! ChartForgeX built-in enhance behaviours, registered through the same public CfxEnhance.behaviour API a host uses. */
(function (w) {
  "use strict";
  var E = w.CfxEnhance, S = w.CfxShared;
  if (!E || !S) return;
  var make = S.make, set = S.set;
  function roles(pattern) { return function (k) { for (var role in k.roles) if (pattern.test(role)) return true; return false; }; }
  // The nearest visible mark in a screen direction; the cross-axis distance counts double so movement stays in its lane.
  function toward(k, m, dx, dy) {
    var g = k.rects(), from = g[m.n], best = null, score = Infinity;
    k.marks.forEach(function (x) {
      if (x === m || !k.visible(x)) return;
      var to = g[x.n], along = (to.x - from.x) * dx + (to.y - from.y) * dy, across = Math.abs((to.x - from.x) * dy) + Math.abs((to.y - from.y) * dx);
      if (along <= 1) return;
      if (along + across * 2 < score) { score = along + across * 2; best = x; }
    });
    return best || m;
  }
  function arrows(k, e, m, vertical, horizontal) {
    var dir = { ArrowUp: [0, -1, 1], ArrowDown: [0, 1, 1], ArrowLeft: [-1, 0, 0], ArrowRight: [1, 0, 0] }[e.key];
    if (dir && (dir[2] ? vertical : horizontal)) return toward(k, m, dir[0], dir[1]);
  }

  // Shared-X crosshair and brush for continuous Cartesian lines and areas.
  E.behaviour("crosshair", {
    match: function (k) { return k.svg.getAttribute("data-cfx-crosshair") === "true" && k.marks.some(function (m) { return !isNaN(m.raw); }); },
    attach: function (k) {
      var byX = {}, columns, column = null, brush = null, range = null, brushing = false, suppress = false, api = k.api;
      var hair = make("g", null, k.layer), band = set(make("rect", "cfx-e-brush", k.layer), { visibility: "hidden", rx: 4 });
      var canBrush = k.svg.getAttribute("data-cfx-brush") === "true";
      k.marks.forEach(function (m) { if (!isNaN(m.raw)) (byX[m.raw] = byX[m.raw] || []).push(m); });
      columns = Object.keys(byX).map(Number).sort(function (a, b) { return a - b; }).map(function (raw, i) {
        var c = { raw: raw, i: i, marks: byX[raw] };
        c.marks.forEach(function (m) { m.col = c; });
        return c;
      });
      function shown(c) { return c.marks.filter(k.visible); }
      function cx(c) { var v = shown(c); return v.length ? k.rects()[v[0].n].x : NaN; }
      function at(x) {
        var best = null, gap = Infinity;
        columns.forEach(function (c) { var d = Math.abs(cx(c) - x); if (d < gap) { gap = d; best = c; } });
        return best;
      }
      function show(c, x, y) {
        hair.textContent = "";
        column = c;
        var v = c ? shown(c) : [];
        if (!v.length) { k.hover(null); return; }
        var p = k.plot(), X = cx(c), top = k.user(X, p.t), bottom = k.user(X, p.b), near = v[0];
        set(make("line", "cfx-e-cross", hair), { x1: top.x, x2: top.x, y1: top.y, y2: bottom.y });
        v.forEach(function (m) {
          var g = k.rects()[m.n], u = k.user(g.x, g.y), dot = set(make("circle", "cfx-e-dot", hair), { cx: u.x, cy: u.y, r: 4.5 });
          dot.style.fill = k.color(m.s);
          if (Math.abs(g.y - y) < Math.abs(k.rects()[near.n].y - y)) near = m;
        });
        k.hover(near, x, y);
        k.tip(v, x, y);
      }
      function span(first, last) {
        var a = first.marks[0], b = last.marks[0], next = columns[last.i + 1], prev = columns[last.i - 1];
        var to = api.timeX ? k.iso(next ? next.marks[0].x : b.x + (prev ? b.x - prev.marks[0].x : 864e5)) : b.raw;
        return { column: api.xField || "x", from: api.timeX ? k.iso(a.x) : a.raw, to: to };
      }
      function draw(a, b) {
        var p = k.plot(), u = k.box({ l: Math.min(a, b) - 3, t: p.t, r: Math.max(a, b) + 3, b: p.b });
        set(band, { x: u.x, y: u.y, width: Math.max(u.w, 1), height: u.h, visibility: "visible" });
      }
      function single(ms) { return ms.length && ms[0].col && ms.every(function (m) { return m.col === ms[0].col; }) ? ms[0].col : null; }
      return {
        pointer: function (e) { show(k.inside(k.plot(), e, 8) ? at(e.clientX) : null, e.clientX, e.clientY); return true; },
        leave: function () { hair.textContent = ""; column = null; },
        targets: function () { return column ? shown(column) : []; },
        group: function (m) { return m.col ? shown(m.col) : undefined; },
        focus: function (m, x, y) { if (!m.col) return; show(m.col, x, y); return true; },
        describe: function (ms) {
          var c = single(ms);
          return c ? { label: ms[0].head, range: api.timeX ? span(c, c) : undefined } : undefined;
        },
        tooltip: function (ms, content) {
          // The data island also lists series whose marks were not drawn at this X.
          var c = single(ms), points = c && k.data.points && k.data.points.filter(function (p) { return p.x === c.raw && k.series[p.s] && !k.series[p.s].hidden; });
          if (points && points.length) content.rows = points.map(function (p) { return { color: k.color(p.s), value: p.v || "", name: k.series[p.s].name || "" }; });
          return content;
        },
        down: function (e) {
          if (canBrush && e.button === 0 && k.inside(k.plot(), e, 8)) brush = { x0: e.clientX, id: e.pointerId, on: false };
        },
        drag: function (e) {
          if (!brush) return;
          if (!brush.on && Math.abs(e.clientX - brush.x0) > 6) {
            brush.on = true; range = null;
            try { k.svg.setPointerCapture(brush.id); } catch (x) { }
          }
          if (!brush.on) return;
          draw(brush.x0, e.clientX); S.tip.hide();
          return true;
        },
        up: function (e, cancelled) {
          var b = brush;
          brush = null;
          if (!b || !b.on) return;
          suppress = true;
          setTimeout(function () { suppress = false; }, 0);
          var a = Math.min(b.x0, e.clientX), z = Math.max(b.x0, e.clientX);
          var covered = cancelled ? [] : columns.filter(function (c) { var x = cx(c); return x >= a && x <= z; });
          if (!covered.length) { k.paint(); return; }
          var first = covered[0], last = covered[covered.length - 1];
          brushing = true;
          k.choose([].concat.apply([], covered.map(shown)), false, span(first, last), first.marks[0].head + " – " + last.marks[0].head);
          brushing = false;
          range = [first, last];
          k.paint();
        },
        click: function () { if (suppress) { suppress = false; return true; } },
        select: function () { if (!brushing) range = null; },
        // A brushed range is drawn by its band; outlining every point inside it would bury the line.
        outline: function () { if (range) return false; },
        paint: function () {
          if (range) draw(cx(range[0]), cx(range[1]));
          else if (!brush || !brush.on) band.setAttribute("visibility", "hidden");
        }
      };
    }
  });

  // Cells, tiles, regions, terms and flow or hierarchy nodes: arrows move to the nearest mark on screen in that direction.
  E.behaviour("grid", {
    match: roles(/(-cell|-tile|region-source|word-cloud-term|pictorial-item|dotted-map-point-source|sankey-node|tree-node|chord-node|treemap-zero-value)$/),
    attach: function (k) { return { key: function (e, m) { return arrows(k, e, m, true, true); } }; }
  });

  // Schedules and stacked rows: Up and Down follow the rows as drawn; Left and Right stay in the row.
  E.behaviour("rows", {
    match: roles(/^(gantt-task|gantt-milestone|gantt-lane-item|state-timeline-segment|timeline-item|bullet-row|progress-row|funnel-stage|pyramid-stage)$/),
    attach: function (k) { return { key: function (e, m) { return arrows(k, e, m, true, false); } }; }
  });

  var radial = roles(/^(radial-point|polar-area-point-source|polar-point-source|progress-ring-point|layered-radial-point|sunburst-segment|radar-point-source|gauge|circle-chart)$/);
  // Radial families: every arrow walks around the ring and wraps; Up and Down change ring when there are several series.
  E.behaviour("radial", {
    match: function (k) { return radial(k) || !!k.roles.point && !k.svg.hasAttribute("data-cfx-plot"); },
    attach: function (k) {
      return {
        key: function (e, m) {
          var step = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[e.key];
          if (!step || (e.key === "ArrowUp" || e.key === "ArrowDown") && k.order.length > 1) return;
          var ring = k.bySeries[m.s].filter(k.visible), i = ring.indexOf(m);
          return ring[(i + step + ring.length) % ring.length];
        }
      };
    }
  });
})(window);

/*! ChartForgeX linked views: wires two views with the LinkedView methods and linkchange events (ChartForgeX charts,
    CanopyX grids and trees, or any object of that shape). Applied links never echo. */
(function (w) {
  "use strict";
  if (w.CfxLink) return;
  function quiet(promise) { if (promise && promise.catch) promise.catch(function () { }); }
  function iso(ms) { return new Date(ms).toISOString(); }
  // A time crosshair chart reports its X as the linked field; other charts link through their marks' fields.
  function crosshair(api) { return api.timeX && api.svg.hasAttribute("data-cfx-crosshair"); }
  // A view: a chart api (or its svg or host), a CanopyX host element, or any LinkedView object.
  function resolve(view, host) {
    if (view && !view.setHighlight && w.CfxEnhance) view = w.CfxEnhance.get(view) || view;
    var api = view && view.canopyx || view, el = host || (view && view.nodeType === 1 ? view : api.svg || api.host || api.element);
    var chart = !!api.svg;
    return { api: api, el: el, id: api.view || el && el.id || "view", label: api.label || el && el.id || "view",
      change: chart ? "cfx:linkchange" : "canopyx:linkchange", filter: chart ? "cfx:externalfilter" : "canopyx:externalfilter" };
  }
  // A CanopyX host that has not mounted yet is linked on its canopyx:ready.
  function unmounted(view) { return view && view.nodeType === 1 && !view.canopyx && !view.cfx && !(w.CfxEnhance && w.CfxEnhance.get(view)); }
  // Like CfxEnhance.get, accept the page element that holds a CanopyX host.
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
    // field: one field, or several that together define a mark (a segment's day and kind).
    var fields = [].concat(o.field || []), field = fields[0], mode = o.mode || "filter", source = o.source || a.id, label = o.sourceLabel || a.label;
    var targetSource = o.targetSource || o.gridSource || b.id, targetLabel = o.targetLabel || o.gridLabel || b.label;
    if (o.events) { b.change = o.events.change || b.change; b.filter = o.events.filter || b.filter; }
    if (a.api.svg) {
      a.api.selectOnClick = true;
      a.api.xField = o.xField || a.api.xField || (crosshair(a.api) ? field : null);
    }
    if (b.api.svg && !b.api.xField && crosshair(b.api)) b.api.xField = o.xField || field;
    // Chip text: one field shows the selection's label, several show each field's texts.
    function text(t, f) { var s = (t.texts || {})[f] || []; return s.slice(0, 3).join(", ") + (s.length > 3 ? " +" + (s.length - 3) : ""); }
    // Source to target: the source's selection filters or highlights the target.
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
    // Target to source: the target's selection highlights the source; its own conditions filter the source.
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
      a.el.removeEventListener(a.change, forward);
      if (b.el) { b.el.removeEventListener(b.change, back); b.el.removeEventListener(b.filter, removed); }
    };
  }
  w.CfxLink = { connect: connect };
})(window);

