/* A plain table standing in for a CanopyX grid. It implements the LinkedView methods and events (docs/enhance.md), so the
   charts are wired exactly as they would be to a real grid. */
(function () {
  "use strict";
  function parse(v) { return typeof v === "number" ? v : Date.parse(v) || +v; }
  function TableView(host, chips, count) {
    var rows = Array.prototype.slice.call(host.querySelectorAll("tbody tr")), filters = new Map(), highlight = new Set(), selected = new Set();
    function fire(type, detail) { host.dispatchEvent(new CustomEvent(type, { bubbles: true, detail: detail })); }
    function matches(tr, f) {
      var v = tr.dataset[f.field];
      if (v == null) return true;
      if (f.values) return f.values.some(function (x) { return String(x) === v; });
      var n = parse(v);
      return (f.range[0] == null || n >= parse(f.range[0])) && (f.range[1] == null || n <= parse(f.range[1]));
    }
    function list() { var all = []; filters.forEach(function (f) { all = all.concat(f); }); return all; }
    function render() {
      var shown = 0;
      rows.forEach(function (tr) {
        var ok = list().every(function (f) { return matches(tr, f); });
        tr.hidden = !ok; if (ok) shown++;
        if (highlight.has(tr.dataset.key)) tr.setAttribute("data-linked", "true"); else tr.removeAttribute("data-linked");
      });
      chips.textContent = "";
      filters.forEach(function (f, source) {
        var chip = document.createElement("span"), remove = document.createElement("button"), text = "Filtered by " + f[0].sourceLabel + ": " + (f[0].label || f[0].values || f[0].range).toString();
        chip.className = "chip"; chip.textContent = text;
        remove.type = "button"; remove.textContent = "×"; remove.setAttribute("aria-label", "Remove the filter. " + text);
        remove.addEventListener("click", function () { filters.delete(source); render(); fire("canopyx:externalfilter", { filters: list(), reason: "user" }); });
        chip.appendChild(remove); chips.appendChild(chip);
      });
      count.textContent = shown + " of " + rows.length + " records";
    }
    var api = {
      host: host,
      setExternalFilter: function (source, filter) {
        if (filter) filters.set(source, [].concat(filter).map(function (f) { return Object.assign({ source: source, sourceLabel: source }, f); }));
        else filters.delete(source);
        render(); fire("canopyx:externalfilter", { filters: list(), reason: "api" });
        return Promise.resolve(true);
      },
      clearExternalFilters: function () { filters.clear(); render(); return Promise.resolve(true); },
      getExternalFilters: list,
      setHighlight: function (keys) { highlight = new Set(keys); render(); return Promise.resolve(highlight.size); },
      clearHighlight: function () { highlight = new Set(); render(); return Promise.resolve(0); },
      getHighlight: function () { return Array.from(highlight); },
      startLinkEvents: function () { return Promise.resolve(); }
    };
    host.addEventListener("change", function (e) {
      var tr = e.target.closest("tr");
      if (!tr) return;
      if (e.target.checked) selected.add(tr.dataset.key); else selected.delete(tr.dataset.key);
      tr.setAttribute("aria-selected", String(e.target.checked));
      var picked = rows.filter(function (r) { return selected.has(r.dataset.key); }), values = { day: [], kind: [], product: [] };
      picked.forEach(function (r) { for (var k in values) if (values[k].indexOf(r.dataset[k]) < 0) values[k].push(r.dataset[k]); });
      fire("canopyx:linkchange", { view: host.id, action: picked.length ? "select" : "clear", scope: "select", keys: Array.from(selected), values: values,
        label: picked.slice(0, 3).map(function (r) { return r.cells[4].textContent; }).join(", ") });
    });
    host.canopyx = api;
    render();
    return api;
  }
  function start() {
    var host = document.getElementById("records");
    TableView(host, document.getElementById("chips"), document.getElementById("count"));
    [["week", "day", "Changes this week"], ["month", "day", "Records per day"], ["support", "product", "Support windows"]].forEach(function (link) {
      var chart = CfxEnhance.get(document.getElementById(link[0] + "-heading").parentNode);
      if (chart) CfxLink.connect(chart, host, { field: link[1], sourceLabel: link[2], targetLabel: "Records" });
    });
  }
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start); else start();
})();
