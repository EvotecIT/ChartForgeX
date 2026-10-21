(function () {
  "use strict";
  const current = document.currentScript?.parentElement;
  if (window.ChartForgeXResponsive) { window.ChartForgeXResponsive.mount(current); return; }
  async function initialize(host) {
  const compressed = host.querySelector("[data-cfx-layouts-gzip]");
  let payload = host.querySelector("[data-cfx-layouts]")?.textContent;
  if (compressed) {
    if (typeof DecompressionStream !== "function") return;
    const bytes = Uint8Array.from(atob(compressed.textContent), character => character.charCodeAt(0));
    payload = await new Response(new Blob([bytes]).stream().pipeThrough(new DecompressionStream("gzip"))).text();
  }
  if (!host.isConnected) { delete host.cfxResponsiveReady; return; }
  const frames = JSON.parse(payload);
  const number = /[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?/g;
  let active, svg, nodes, pending = false, width = 0;
  function layout() {
    pending = false;
    if (!host.isConnected || !host.clientWidth) return;
    const nextWidth = Math.min(frames[frames.length - 1].width, Math.max(frames[0].width, host.clientWidth));
    if (width === nextWidth) return;
    width = nextWidth;
    let index = 0;
    while (index + 1 < frames.length && frames[index + 1].width <= width) index++;
    let frame = frames[index];
    if (!frame.interpolate && frames[index + 1] && width - frame.width > frames[index + 1].width - width) frame = frames[index + 1];
    if (frame !== active) {
      const parsed = new DOMParser().parseFromString(frame.svg, "image/svg+xml").documentElement;
      host.querySelector(":scope > svg").replaceWith(document.importNode(parsed, true));
      svg = host.querySelector(":scope > svg"); nodes = [...svg.querySelectorAll("*")]; active = frame;
    }
    const delta = width - frame.width;
    for (const [index, name, value, slopes] of frame.interpolate ? frame.coordinates : []) {
      let n = 0;
      nodes[index].setAttribute(name, value.replace(number, token => String(Number(token) + slopes[n++] * delta)));
    }
    svg.setAttribute("width", width); svg.setAttribute("viewBox", `0 0 ${width} ${svg.getAttribute("height")}`);
    svg.style.cssText = `display:block;width:${width}px;height:${svg.getAttribute("height")}px;max-width:none`;
    host.dataset.cfxLayoutWidth = String(width);
  }
  const observer = new ResizeObserver(() => { if (!pending) { pending = true; requestAnimationFrame(layout); } });
  observer.observe(host);
  // Hosts removing a report can explicitly release its observer, without touching another chart.
  host.cfxResponsive = { layout, dispose: () => { observer.disconnect(); delete host.cfxResponsiveReady; delete host.cfxResponsive; } };
  layout();
  }
  function hosts(scope) {
    if (!scope?.querySelectorAll) return [];
    return [...(scope.matches?.('[data-cfx-responsive-chart]') ? [scope] : []), ...scope.querySelectorAll('[data-cfx-responsive-chart]')];
  }
  function mount(scope = document) {
    return Promise.all(hosts(scope).map(host => host.cfxResponsiveReady ||= initialize(host)));
  }
  window.ChartForgeXResponsive = { mount };
  const additions = new MutationObserver(records => {
    for (const record of records) {
      for (const node of record.addedNodes) mount(node);
      for (const node of record.removedNodes) for (const host of hosts(node)) if (!host.isConnected) host.cfxResponsive?.dispose();
    }
  });
  additions.observe(document.documentElement, { childList: true, subtree: true });
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => mount(document), { once: true });
  mount(current || document);
})();
