/*! ChartForgeX data renderer. MIT. Loads independently of the prepared-markup interaction runtime. */
(function (global) {
  "use strict";
  if (global.ChartForgeXRenderer) return;
  const namespace = "http://www.w3.org/2000/svg";
  const kinds = new Set(["line", "area", "bar", "column", "stacked", "win-loss", "bullet", "donut", "gauge", "state-timeline"]);
  const owners = new WeakMap();
  const surface = __CFX_NATIVE_SURFACE__;

  function text(value, field) {
    if (typeof value !== "string" || !value.trim() || value.length > 2000)
      throw new TypeError("ChartForgeX: " + field + " must be a nonempty string of at most 2000 characters.");
    return value;
  }
  function capture(input) {
    if (!input || !kinds.has(input.type) || !Array.isArray(input.categories) || !Array.isArray(input.series))
      throw new TypeError("ChartForgeX: a supported type, categories and numeric series are required.");
    if (input.categories.length > 10000 || !input.series.length || input.series.length > 32)
      throw new RangeError("ChartForgeX: at most 10000 categories and 1 to 32 series are supported.");
    const categoryIds = new Set(), seriesIds = new Set();
    const categories = Array.from(input.categories, category => {
      const id = text(category?.id, "category id"), label = text(category?.label, "category label");
      if (categoryIds.has(id)) throw new TypeError("ChartForgeX: category ids must be unique.");
      categoryIds.add(id);
      const instant = category.instant == null ? null : Date.parse(text(category.instant, "category instant"));
      if (instant !== null && !Number.isFinite(instant)) throw new TypeError("ChartForgeX: invalid category instant.");
      return Object.freeze({ id, label, instant, source: instant === null ? null : category.instant });
    });
    const temporal = categories.some(category => category.instant !== null);
    if (temporal && categories.some((category, index) => category.instant === null ||
        index > 0 && category.instant <= categories[index - 1].instant))
      throw new TypeError("ChartForgeX: temporal categories require strictly increasing instants.");
    const series = Array.from(input.series, item => {
      const id = text(item?.id, "series id"), name = text(item?.name, "series name");
      if (seriesIds.has(id)) throw new TypeError("ChartForgeX: series ids must be unique.");
      seriesIds.add(id);
      if (!Array.isArray(item.values) || item.values.length !== categories.length ||
          Array.from(item.values).some(value => value !== null && (typeof value !== "number" || !Number.isFinite(value))))
        throw new TypeError("ChartForgeX: each series requires one finite number or null per category.");
      return Object.freeze({ id, name, values: Object.freeze(item.values.slice()) });
    });
    if (input.type === "bullet" && series.length > 2)
      throw new TypeError("ChartForgeX: bullet charts accept a measure series and an optional target series.");
    const captured = { type: input.type, label: text(input.label, "chart label"), temporal,
      categories: Object.freeze(categories), series: Object.freeze(series) };
    if (input.type === "donut" && (series.length !== 1 || series[0].values.some(value => value !== null && value < 0)))
      throw new TypeError("ChartForgeX: donut charts accept one series of nonnegative values, one per slice.");
    if (input.type === "donut" && !Number.isFinite(series[0].values.reduce((sum, value) => sum + (value || 0), 0)))
      throw new RangeError("ChartForgeX: the donut total exceeds the supported finite range.");
    if (input.type === "gauge") {
      if (categories.length !== 1 || series.length > 2 || series.some(item => item.values[0] === null))
        throw new TypeError("ChartForgeX: gauges accept one category, a value series and an optional target series.");
      const minimum = input.minimum ?? 0, maximum = input.maximum ?? Math.max(1, ...series.map(item => item.values[0]));
      if (typeof minimum !== "number" || typeof maximum !== "number" || !Number.isFinite(minimum) || !Number.isFinite(maximum) || maximum <= minimum
          || !Number.isFinite(maximum - minimum))
        throw new RangeError("ChartForgeX: a gauge requires finite minimum < maximum.");
      captured.range = Object.freeze([minimum, maximum]);
    }
    if (input.type === "state-timeline") {
      if (!Array.isArray(input.states) || !input.states.length || input.states.length > 32)
        throw new TypeError("ChartForgeX: state timelines require 1 to 32 states.");
      const stateIds = new Set();
      captured.states = Object.freeze(Array.from(input.states, state => {
        const id = text(state?.id, "state id"), label = text(state?.label, "state label");
        if (stateIds.has(id)) throw new TypeError("ChartForgeX: state ids must be unique.");
        stateIds.add(id);
        // Colours are data, never markup: only hexadecimal colours are accepted.
        if (state.color !== undefined && (typeof state.color !== "string" || !/^#[0-9a-f]{3,8}$/i.test(state.color)))
          throw new TypeError("ChartForgeX: state colours must be hexadecimal.");
        return Object.freeze({ id, label, color: state.color ?? null });
      }));
      if (series.some(item => item.values.some(value => value !== null && (!Number.isInteger(value) || value < 0 || value >= captured.states.length))))
        throw new TypeError("ChartForgeX: state timeline values are state indexes or null.");
    }
    return Object.freeze(captured);
  }
  // The descriptor a captured chart was created from, so live changes can be validated as a complete chart again.
  function descriptor(data) {
    const result = { type: data.type, label: data.label,
      categories: data.categories.map(category => category.source === null ? { id: category.id, label: category.label }
        : { id: category.id, label: category.label, instant: category.source }),
      series: data.series.map(series => ({ id: series.id, name: series.name, values: series.values.slice() })) };
    if (data.range) { result.minimum = data.range[0]; result.maximum = data.range[1]; }
    if (data.states) result.states = data.states.map(state => state.color === null ? { id: state.id, label: state.label } : { ...state });
    return result;
  }
  function element(document, name, attributes, parent, content) {
    const node = document.createElementNS(namespace, name);
    for (const [key, value] of Object.entries(attributes)) node.setAttribute(key, String(value));
    if (content !== undefined) node.textContent = content;
    if (parent) parent.appendChild(node);
    return node;
  }
  function domain(data) {
    if (data.type === "win-loss") return [-1, 1];
    if (data.type === "gauge") return data.range;
    if (data.type === "donut" || data.type === "state-timeline") return [0, 1];
    let low = 0, high = 0;
    if (data.type === "stacked") {
      for (let category = 0; category < data.categories.length; category++) {
        let positive = 0, negative = 0;
        for (const series of data.series) {
          const value = series.values[category];
          if (value > 0) positive += value;
          else if (value < 0) negative += value;
        }
        low = Math.min(low, negative); high = Math.max(high, positive);
      }
    } else {
      for (const series of data.series) for (const value of series.values) {
        if (value === null) continue;
        low = Math.min(low, value); high = Math.max(high, value);
      }
    }
    if (!Number.isFinite(low) || !Number.isFinite(high) || !Number.isFinite(high - low))
      throw new RangeError("ChartForgeX: the numeric domain exceeds the supported finite range.");
    return low === high ? [0, 1] : [low, high];
  }
  function color(index) {
    return "var(--cfx-series-" + (index + 1) + "," + surface.palette[index % surface.palette.length] + ")";
  }
  function stateColor(data, index) {
    return data.states[index].color || "var(--cfx-state-" + (index + 1) + "," + surface.palette[index % surface.palette.length] + ")";
  }
  function sector(cx, cy, outer, inner, start, end) {
    const sweep = Math.min(end - start, Math.PI * 2 - 1e-6), finish = start + sweep, large = sweep > Math.PI ? 1 : 0;
    const point = (radius, angle) => (cx + Math.cos(angle) * radius) + " " + (cy + Math.sin(angle) * radius);
    return "M" + point(outer, start) + "A" + outer + " " + outer + " 0 " + large + " 1 " + point(outer, finish)
      + "L" + point(inner, finish) + "A" + inner + " " + inner + " 0 " + large + " 0 " + point(inner, start) + "Z";
  }
  function drawing(document, data, width, onSelect, selected, compact) {
    const lanes = data.type === "state-timeline", radial = data.type === "donut" || data.type === "gauge";
    const horizontal = data.type === "bar" || data.type === "bullet";
    const height = compact ? 64 : horizontal ? Math.max(300, Math.min(640, data.categories.length * 32 + 100))
      : lanes ? Math.min(640, data.series.length * 28 + 120) : 360;
    const inset = compact ? { left: 4, top: 4, right: 4, bottom: 4 } :
      { left: horizontal || lanes ? Math.min(150, width * .3) : radial ? 24 : 66, top: 68, right: 24, bottom: lanes ? 40 : radial ? 24 : 58 };
    const plot = { x: inset.left, y: inset.top, width: Math.max(1, width - inset.left - inset.right),
      height: height - inset.top - inset.bottom };
    const [low, high] = domain(data), count = data.categories.length, seriesCount = data.series.length;
    let number;
    try { number = new Intl.NumberFormat(document.documentElement.lang || undefined, { maximumSignificantDigits: 6 }); }
    catch { number = new Intl.NumberFormat(undefined, { maximumSignificantDigits: 6 }); }
    const svg = element(document, "svg", { xmlns: namespace, viewBox: "0 0 " + width + " " + height,
      width: "100%", height, role: "group", "aria-label": data.label, "data-cfx-renderer": data.type });
    svg.style.display = "block"; svg.style.fontFamily = "inherit";
    svg.style.color = "var(--cfx-color-text,#334155)";
    element(document, "title", {}, svg, data.label);
    const valueCoordinate = value => horizontal ? plot.x + (value - low) / (high - low) * plot.width :
      plot.y + plot.height - (value - low) / (high - low) * plot.height;
    const band = (horizontal ? plot.height : plot.width) / Math.max(1, count);
    const categoryCoordinate = index => {
      if (horizontal) return plot.y + (index + .5) * band;
      if (data.temporal && count > 1 && (data.type === "line" || data.type === "area"))
        return plot.x + (data.categories[index].instant - data.categories[0].instant) /
          (data.categories[count - 1].instant - data.categories[0].instant) * plot.width;
      return plot.x + (index + .5) * band;
    };
    // Lanes divide time across the plot: temporal categories by instant, otherwise one equal period each.
    const laneStart = index => {
      if (!data.temporal || count < 2) return plot.x + index / Math.max(1, count) * plot.width;
      const first = data.categories[0].instant, last = data.categories[count - 1].instant, pitch = (last - first) / (count - 1);
      return plot.x + (data.categories[index].instant - first) / (last - first + pitch) * plot.width;
    };
    const label = (x, y, value, anchor = "start", extra = {}) => element(document, "text",
      { x, y, fill: "currentColor", "font-size": 12, "text-anchor": anchor, ...extra }, svg, value);
    if (!compact) {
      const titleLength = Math.max(1, Math.floor(plot.width / 9));
      label(plot.x, 23, data.label.length > titleLength ? data.label.slice(0, titleLength) + "…" : data.label,
        "start", { "font-size": 16, "font-weight": 600 });
      const entries = data.type === "donut" ? data.categories.map((category, index) => [category.label, color(index)])
        : lanes ? data.states.map((state, index) => [state.label, stateColor(data, index)])
        : data.type === "gauge" ? [] : data.series.map((series, index) => [series.name, color(index)]);
      const legendWidth = plot.width / Math.max(1, entries.length);
      entries.forEach(([name, fill], index) => {
        const x = (radial ? 24 : plot.x) + index * (radial ? (width - 48) / entries.length : legendWidth);
        element(document, "rect", { x, y: 39, width: 10, height: 10, rx: 2, fill }, svg);
        const shown = Math.max(1, Math.floor(((radial ? (width - 48) / entries.length : legendWidth) - 22) / 7));
        const legend = label(x + 16, 49, name.length > shown ? name.slice(0, shown) + "…" : name);
        element(document, "title", {}, legend, name);
      });
    }
    if (!compact && !radial && !lanes) {
      for (let tick = 0; tick <= 4; tick++) {
        const value = low + (high - low) * (tick / 4), position = valueCoordinate(value);
        element(document, "line", horizontal ? { x1: position, x2: position, y1: plot.y, y2: plot.y + plot.height,
          stroke: "var(--cfx-color-border,#e2e8f0)" } : { x1: plot.x, x2: plot.x + plot.width,
          y1: position, y2: position, stroke: "var(--cfx-color-border,#e2e8f0)" }, svg);
        if (horizontal) label(position, plot.y + plot.height + 23, number.format(value), "middle");
        else label(plot.x - 10, position + 4, number.format(value), "end");
      }
    }
    if (!compact && !radial) {
      const stride = horizontal ? Math.max(1, Math.ceil(count / 16)) : Math.max(1, Math.ceil(count / Math.max(1, plot.width / 90)));
      if (lanes) data.series.forEach((series, index) => {
        const length = Math.max(1, Math.floor((inset.left - 15) / 7));
        const node = label(plot.x - 10, plot.y + (index + .5) * plot.height / seriesCount + 4,
          series.name.length > length ? series.name.slice(0, length) + "…" : series.name, "end");
        element(document, "title", {}, node, series.name);
      });
      data.categories.forEach((category, index) => {
        if (index % stride) return;
        const position = categoryCoordinate(index), room = horizontal ? inset.left - 15 : plot.width / Math.ceil(count / stride);
        const length = Math.max(1, Math.floor(room / 7));
        const shown = category.label.length > length ? category.label.slice(0, length) + "…" : category.label;
        const node = horizontal ? label(plot.x - 10, position + 4, shown, "end") :
          label(lanes ? laneStart(index) : position, plot.y + plot.height + 25, shown, lanes ? "start" : "middle");
        element(document, "title", {}, node, category.label);
      });
    }
    const marks = [], zero = valueCoordinate(0);
    function mark(name, attributes, seriesIndex, categoryIndex) {
      const series = data.series[seriesIndex], category = data.categories[categoryIndex];
      const value = series.values[categoryIndex];
      const node = element(document, "g", { "data-cfx-series": series.id,
        "data-cfx-category": category.id }, svg);
      const { target, ...paint } = attributes;
      const ink = element(document, name, { ...paint, "data-cfx-ink": "" }, node);
      const caption = series.name + ", " + category.label + ": " + number.format(value);
      element(document, "title", {}, node, caption);
      if (onSelect) {
        node.setAttribute("role", "button"); node.setAttribute("aria-label", caption);
        node.setAttribute("aria-pressed", String(selected?.seriesId === series.id && selected?.categoryId === category.id));
        node.setAttribute("tabindex", marks.length ? "-1" : "0");
        node.style.cursor = "pointer";
        // Zero remains zero ink. A separate target keeps it reachable with a pointer.
        if (name === "rect" && (!attributes.width || !attributes.height))
          element(document, "rect", { x: attributes.x - (!attributes.width ? 4 : 0),
            y: attributes.y - (!attributes.height ? 4 : 0), width: Math.max(8, attributes.width),
            height: Math.max(8, attributes.height), fill: "transparent", "pointer-events": "all", "data-cfx-target": "",
            stroke: selected?.seriesId === series.id && selected?.categoryId === category.id ? "currentColor" : "none",
            "stroke-width": 2, "stroke-dasharray": "2 2" }, node);
        // A zero arc has no area either; a small target at its start keeps it reachable.
        if (name === "path" && attributes.target)
          element(document, "circle", { cx: attributes.target[0], cy: attributes.target[1], r: 5, fill: "transparent", "pointer-events": "all", "data-cfx-target": "",
            stroke: selected?.seriesId === series.id && selected?.categoryId === category.id ? "currentColor" : "none", "stroke-width": 2, "stroke-dasharray": "2 2" }, node);
        if (selected?.seriesId === series.id && selected?.categoryId === category.id) {
          ink.setAttribute("stroke", "currentColor"); ink.setAttribute("stroke-width", "2");
        }
        marks.push({ node, detail: Object.freeze({ seriesId: series.id, categoryId: category.id, value }) });
      }
      return node;
    }
    if (data.type === "donut") {
      const series = data.series[0], total = series.values.reduce((sum, value) => sum + (value || 0), 0);
      const cx = width / 2, cy = plot.y + plot.height / 2, outer = Math.max(1, Math.min(plot.width, plot.height) / 2), inner = outer * surface.donutInner;
      let angle = -Math.PI / 2;
      const zeros = [];
      series.values.forEach((value, categoryIndex) => {
        if (value === null || !total) return;
        const sweep = value / total * Math.PI * 2, middle = (outer + inner) / 2;
        const node = mark("path", { d: sector(cx, cy, outer, inner, angle, angle + sweep), fill: color(categoryIndex),
          target: sweep ? undefined : [cx + Math.cos(angle) * middle, cy + Math.sin(angle) * middle] }, 0, categoryIndex);
        if (!sweep) zeros.push(node);
        angle += sweep;
      });
      // A zero slice's target lies on its neighbour's edge; drawing it last keeps it on top.
      for (const node of zeros) svg.appendChild(node);
      if (!compact) label(cx, cy + 6, number.format(total), "middle", { "font-size": 18, "font-weight": 600 });
      return { svg, marks };
    }
    if (data.type === "gauge") {
      const [minimum, maximum] = data.range, start = surface.gaugeStart * Math.PI / 180, sweep = surface.gaugeSweep * Math.PI / 180;
      const cx = width / 2, outer = Math.max(1, Math.min(plot.width / 2, plot.height / (1 + Math.max(0, Math.sin(start))))), cy = plot.y + outer;
      const inner = outer * surface.donutInner, ratio = value => Math.max(0, Math.min(1, (value - minimum) / (maximum - minimum)));
      element(document, "path", { d: sector(cx, cy, outer, inner, start, start + sweep), fill: "var(--cfx-color-border,#e2e8f0)" }, svg);
      const value = data.series[0].values[0];
      mark("path", { d: sector(cx, cy, outer, inner, start, start + sweep * ratio(value)), fill: color(0),
        target: ratio(value) ? undefined : [cx + Math.cos(start) * (outer + inner) / 2, cy + Math.sin(start) * (outer + inner) / 2] }, 0, 0);
      if (data.series[1]) {
        const at = start + sweep * ratio(data.series[1].values[0]);
        mark("line", { x1: cx + Math.cos(at) * (inner - 4), y1: cy + Math.sin(at) * (inner - 4), x2: cx + Math.cos(at) * (outer + 4),
          y2: cy + Math.sin(at) * (outer + 4), stroke: "var(--cfx-color-text,#334155)", "stroke-width": 3 }, 1, 0);
      }
      if (!compact) {
        label(cx, cy + 6, number.format(value), "middle", { "font-size": 22, "font-weight": 600 });
        label(cx, cy + 26, data.categories[0].label, "middle");
        label(cx + Math.cos(start) * (outer + inner) / 2, cy + Math.sin(start) * (outer + inner) / 2 + 18, number.format(minimum), "middle");
        label(cx + Math.cos(start + sweep) * (outer + inner) / 2, cy + Math.sin(start + sweep) * (outer + inner) / 2 + 18, number.format(maximum), "middle");
      }
      return { svg, marks };
    }
    if (lanes) {
      const laneHeight = plot.height / seriesCount, end = index => index + 1 < count ? laneStart(index + 1) : plot.x + plot.width;
      data.series.forEach((series, seriesIndex) => {
        // Consecutive periods in one state form one mark; its identity is the first period.
        for (let first = 0; first < count;) {
          let last = first;
          while (last + 1 < count && series.values[last + 1] === series.values[first]) last++;
          const state = series.values[first];
          if (state !== null) {
            const node = mark("rect", { x: laneStart(first), y: plot.y + seriesIndex * laneHeight + 2, width: Math.max(.5, end(last) - laneStart(first)),
              height: Math.max(1, laneHeight - 4), fill: stateColor(data, state) }, seriesIndex, first);
            const caption = series.name + ", " + data.categories[first].label + (last > first ? " – " + data.categories[last].label : "") + ": " + data.states[state].label;
            node.querySelector("title").textContent = caption;
            if (onSelect) node.setAttribute("aria-label", caption);
          }
          first = last + 1;
        }
      });
      return { svg, marks };
    }
    const positive = Array(count).fill(0), negative = Array(count).fill(0);
    const totals = Array.from({ length: count }, (_, index) => data.series.reduce((sum, item) => {
      const value = item.values[index] || 0;
      sum[value >= 0 ? 1 : 0] += value; return sum;
    }, [0, 0]));
    data.series.forEach((series, seriesIndex) => {
      if (data.type === "line" || data.type === "area") {
        let points = [];
        const flush = () => {
          if (!points.length) return;
          const path = "M" + points.map(point => point.join(" ")).join("L");
          if (data.type === "area") element(document, "path", {
            d: path + "L" + points[points.length - 1][0] + " " + zero + "L" + points[0][0] + " " + zero + "Z",
            fill: color(seriesIndex), "fill-opacity": surface.areaOpacity, stroke: "none" }, svg);
          element(document, "path", { d: path, fill: "none", stroke: color(seriesIndex), "stroke-width": surface.lineWidth }, svg);
          points = [];
        };
        series.values.forEach((value, categoryIndex) => {
          if (value === null) { flush(); return; }
          points.push([categoryCoordinate(categoryIndex), valueCoordinate(value)]);
        });
        flush();
      }
      series.values.forEach((raw, categoryIndex) => {
        if (raw === null) return;
        const value = data.type === "win-loss" ? Math.sign(raw) : raw;
        const middle = categoryCoordinate(categoryIndex), position = valueCoordinate(value);
        if (data.type === "line" || data.type === "area") {
          mark("circle", { cx: middle, cy: position, r: surface.markerRadius, fill: color(seriesIndex) }, seriesIndex, categoryIndex);
        } else if (data.type === "bullet" && seriesIndex === 1) {
          mark("line", { x1: position, x2: position, y1: middle - band * .28, y2: middle + band * .28,
            stroke: "var(--cfx-color-text,#334155)", "stroke-width": 3 }, seriesIndex, categoryIndex);
        } else {
          const slots = data.type === "stacked" || data.type === "bullet" ? 1 : seriesCount;
          const occupied = band * surface.bandFill;
          const gap = slots > 1 ? Math.min(surface.gap, occupied / (slots * 4)) : 0;
          const thickness = Math.max(.1, Math.min(horizontal ? surface.barMaximum : surface.columnMaximum,
            (occupied - gap * (slots - 1)) / slots));
          let start = 0, end = value, offset = (seriesIndex - (slots - 1) / 2) * (thickness + gap);
          if (data.type === "stacked") {
            const sums = value >= 0 ? positive : negative;
            start = sums[categoryIndex]; end = start + value; sums[categoryIndex] = end; offset = 0;
          } else if (data.type === "bullet") {
            offset = 0;
          }
          const a = valueCoordinate(start), b = valueCoordinate(end);
          let top = Math.min(a, b), bottom = Math.max(a, b);
          if (data.type === "stacked") {
            const envelope = totals[categoryIndex].map(valueCoordinate);
            const cutTop = top > Math.min(...envelope) + .001 ? surface.separator / 2 : 0;
            const cutBottom = bottom < Math.max(...envelope) - .001 ? surface.separator / 2 : 0;
            if (bottom - top - cutTop - cutBottom >= surface.minimumSegment) {
              top += cutTop; bottom -= cutBottom;
            }
          }
          mark("rect", horizontal ? { x: Math.min(a, b), y: middle + offset - thickness / 2,
            width: Math.abs(b - a), height: thickness, fill: color(seriesIndex) } :
            { x: middle + offset - thickness / 2, y: top, width: thickness,
              height: bottom - top, fill: color(seriesIndex) }, seriesIndex, categoryIndex);
        }
      });
    });
    return { svg, marks };
  }
  function create(host, input, options = {}) {
    if (!(host instanceof global.Element)) throw new TypeError("ChartForgeX: a host element is required.");
    if (options.onSelect !== undefined && typeof options.onSelect !== "function")
      throw new TypeError("ChartForgeX: onSelect must be a function.");
    if (options.compact !== undefined && typeof options.compact !== "boolean")
      throw new TypeError("ChartForgeX: compact must be a boolean.");
    if (options.signal !== undefined && !(options.signal instanceof global.AbortSignal))
      throw new TypeError("ChartForgeX: signal must be an AbortSignal.");
    if (options.signal?.aborted) throw new DOMException("Chart rendering cancelled.", "AbortError");
    let data = capture(input), selected = null, disposed = false, frame = 0, svg, marks = [];
    const document = host.ownerDocument, listeners = new AbortController();
    function requireActive() { if (disposed) throw new Error("ChartForgeX: renderer is destroyed."); }
    function render(next) {
      const width = Math.max(120, host.clientWidth || 640);
      const result = drawing(document, next, width, options.onSelect, selected, options.compact === true);
      const active = host.getRootNode().activeElement;
      const focused = marks.find(mark => mark.node === active)?.detail;
      const emptyFocused = active === svg;
      if (svg) svg.replaceWith(result.svg); else host.appendChild(result.svg);
      svg = result.svg; marks = result.marks; data = next;
      if (focused || emptyFocused) {
        const target = focused && marks.find(mark => mark.detail.seriesId === focused.seriesId && mark.detail.categoryId === focused.categoryId)
          || marks[0];
        if (target) {
          for (const mark of marks) mark.node.setAttribute("tabindex", mark === target ? "0" : "-1");
          target.node.focus({ preventScroll: true });
        } else {
          svg.setAttribute("tabindex", "0"); svg.focus({ preventScroll: true });
        }
      }
    }
    function select(event) {
      const item = marks.find(mark => mark.node === event.target.closest?.("[data-cfx-category]"));
      if (!item) return;
      selected = item.detail;
      for (const mark of marks) {
        mark.node.setAttribute("aria-pressed", String(mark === item));
        mark.node.setAttribute("tabindex", mark === item ? "0" : "-1");
      }
      render(data);
      options.onSelect(item.detail);
    }
    function keyboard(event) {
      const index = marks.findIndex(mark => mark.node === event.target);
      if (index < 0) return;
      if (event.key === "Enter" || event.key === " ") { event.preventDefault(); select(event); return; }
      if (event.altKey || event.ctrlKey || event.metaKey) return;
      let next = index;
      if (event.key === "Home") next = 0;
      else if (event.key === "End") next = marks.length - 1;
      else if (event.key === "ArrowRight" || event.key === "ArrowDown") next = Math.min(index + 1, marks.length - 1);
      else if (event.key === "ArrowLeft" || event.key === "ArrowUp") next = Math.max(index - 1, 0);
      else return;
      event.preventDefault(); marks[index].node.setAttribute("tabindex", "-1");
      marks[next].node.setAttribute("tabindex", "0"); marks[next].node.focus();
    }
    const observer = new ResizeObserver(() => {
      if (frame || disposed) return;
      frame = global.requestAnimationFrame(() => { frame = 0; if (!disposed) render(data); });
    });
    const controller = Object.freeze({
      update(next) { requireActive(); render(capture(next)); return controller; },
      // Live data: append periods to every series (missing series get null), keeping at most `maximum` periods.
      append(change, settings = {}) {
        requireActive();
        if (!change || !Array.isArray(change.categories) || !change.values || typeof change.values !== "object")
          throw new TypeError("ChartForgeX: append requires categories and values by series id.");
        const next = descriptor(data), added = change.categories.length;
        for (const key of Object.keys(change.values))
          if (!next.series.some(series => series.id === key)) throw new TypeError("ChartForgeX: unknown series id " + key + ".");
        next.categories.push(...change.categories);
        for (const series of next.series) {
          const values = Object.prototype.hasOwnProperty.call(change.values, series.id) ? change.values[series.id] : Array(added).fill(null);
          if (!Array.isArray(values) || values.length !== added) throw new TypeError("ChartForgeX: append requires one value per new category.");
          series.values.push(...values);
        }
        const maximum = settings.maximum;
        if (maximum !== undefined && (!Number.isInteger(maximum) || maximum < 1)) throw new RangeError("ChartForgeX: maximum must be a positive integer.");
        const drop = maximum === undefined ? 0 : Math.max(0, next.categories.length - maximum);
        if (drop) { next.categories.splice(0, drop); for (const series of next.series) series.values.splice(0, drop); }
        render(capture(next)); return controller;
      },
      // Replaces one series' name or values, or adds it when the id is new.
      replaceSeries(series) {
        requireActive();
        const next = descriptor(data), index = next.series.findIndex(item => item.id === series?.id);
        if (index < 0) next.series.push(series); else next.series[index] = series;
        render(capture(next)); return controller;
      },
      destroy() {
        if (disposed) return;
        disposed = true; observer.disconnect(); listeners.abort(); global.cancelAnimationFrame(frame);
        options.signal?.removeEventListener("abort", controller.destroy);
        svg?.remove(); marks = [];
        if (owners.get(host) === controller) owners.delete(host);
      }
    });
    render(data);
    owners.get(host)?.destroy(); owners.set(host, controller);
    host.addEventListener("click", select, { signal: listeners.signal });
    host.addEventListener("keydown", keyboard, { signal: listeners.signal });
    options.signal?.addEventListener("abort", controller.destroy, { once: true });
    observer.observe(host);
    return controller;
  }
  global.ChartForgeXRenderer = Object.freeze({ create });
})(window);
