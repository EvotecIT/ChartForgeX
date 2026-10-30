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
