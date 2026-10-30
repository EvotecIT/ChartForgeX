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
