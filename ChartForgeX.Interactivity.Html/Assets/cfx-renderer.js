/*! ChartForgeX data renderer. MIT. Loads independently of the prepared-markup interaction runtime. */
(function (global) {
  "use strict";
  if (global.ChartForgeXRenderer) return;
  const namespace = "http://www.w3.org/2000/svg";
  const kinds = new Set(["line", "area", "bar", "column", "stacked", "win-loss", "bullet"]);
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
      return Object.freeze({ id, label, instant });
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
    return Object.freeze({ type: input.type, label: text(input.label, "chart label"), temporal,
      categories: Object.freeze(categories), series: Object.freeze(series) });
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
  function drawing(document, data, width, onSelect, selected, compact) {
    const horizontal = data.type === "bar" || data.type === "bullet";
    const height = compact ? 64 : horizontal ? Math.max(300, Math.min(640, data.categories.length * 32 + 100)) : 360;
    const inset = compact ? { left: 4, top: 4, right: 4, bottom: 4 } :
      { left: horizontal ? Math.min(150, width * .3) : 66, top: 68, right: 24, bottom: 58 };
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
    const label = (x, y, value, anchor = "start", extra = {}) => element(document, "text",
      { x, y, fill: "currentColor", "font-size": 12, "text-anchor": anchor, ...extra }, svg, value);
    if (!compact) {
      const titleLength = Math.max(1, Math.floor(plot.width / 9));
      label(plot.x, 23, data.label.length > titleLength ? data.label.slice(0, titleLength) + "…" : data.label,
        "start", { "font-size": 16, "font-weight": 600 });
      const legendWidth = plot.width / seriesCount;
      data.series.forEach((series, index) => {
        const x = plot.x + index * legendWidth;
        element(document, "rect", { x, y: 39, width: 10, height: 10, rx: 2, fill: color(index) }, svg);
        const shown = Math.max(1, Math.floor((legendWidth - 22) / 7));
        const legend = label(x + 16, 49, series.name.length > shown ? series.name.slice(0, shown) + "…" : series.name);
        element(document, "title", {}, legend, series.name);
      });
      for (let tick = 0; tick <= 4; tick++) {
        const value = low + (high - low) * (tick / 4), position = valueCoordinate(value);
        element(document, "line", horizontal ? { x1: position, x2: position, y1: plot.y, y2: plot.y + plot.height,
          stroke: "var(--cfx-color-border,#e2e8f0)" } : { x1: plot.x, x2: plot.x + plot.width,
          y1: position, y2: position, stroke: "var(--cfx-color-border,#e2e8f0)" }, svg);
        if (horizontal) label(position, plot.y + plot.height + 23, number.format(value), "middle");
        else label(plot.x - 10, position + 4, number.format(value), "end");
      }
      const stride = horizontal ? Math.max(1, Math.ceil(count / 16)) : Math.max(1, Math.ceil(count / Math.max(1, plot.width / 90)));
      data.categories.forEach((category, index) => {
        if (index % stride) return;
        const position = categoryCoordinate(index), room = horizontal ? inset.left - 15 : plot.width / Math.ceil(count / stride);
        const length = Math.max(1, Math.floor(room / 7));
        const shown = category.label.length > length ? category.label.slice(0, length) + "…" : category.label;
        const node = horizontal ? label(plot.x - 10, position + 4, shown, "end") :
          label(position, plot.y + plot.height + 25, shown, "middle");
        element(document, "title", {}, node, category.label);
      });
    }
    const marks = [], zero = valueCoordinate(0);
    function mark(name, attributes, seriesIndex, categoryIndex) {
      const series = data.series[seriesIndex], category = data.categories[categoryIndex];
      const value = series.values[categoryIndex];
      const node = element(document, "g", { "data-cfx-series": series.id,
        "data-cfx-category": category.id }, svg);
      const ink = element(document, name, { ...attributes, "data-cfx-ink": "" }, node);
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
        if (selected?.seriesId === series.id && selected?.categoryId === category.id) {
          ink.setAttribute("stroke", "currentColor"); ink.setAttribute("stroke-width", "2");
        }
        marks.push({ node, detail: Object.freeze({ seriesId: series.id, categoryId: category.id, value }) });
      }
      return node;
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
