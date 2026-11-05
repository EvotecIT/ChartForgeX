// ChartForgeX annotation editor page (/playground/annotations/): the WebAssembly engine is the chart runtime's native host.
// The runtime turns each applied edit into a request; the engine applies it to the chart model and returns the re-prepared section.
import { loadPlaygroundEngine } from '/themes/chartforgex/assets/playground-engine.js';

const page = document.querySelector('[data-cfx-annotation-playground]');
if (page) start(page);

function start(page) {
  const $ = (selector) => page.querySelector(selector);
  const ui = {
    preview: $('.cfx-pg__preview'), canvas: $('[data-pg-canvas]'), boot: $('[data-pg-boot]'),
    bootTitle: $('.cfx-pg__boot-title'), bootText: $('[data-pg-boot-text]'), meter: $('[data-pg-meter]'),
    status: $('[data-pg-status]'), reset: $('[data-pga-reset]'), themes: Array.from(page.querySelectorAll('[data-pg-theme]'))
  };
  const state = { theme: siteTheme(), themeLocked: false, basis: '', chart: null, replies: new Map(), nextId: 0 };
  const engine = loadPlaygroundEngine({ onProgress: showProgress });

  ui.themes.forEach((button) => button.addEventListener('click', () => { state.themeLocked = true; setTheme(button.dataset.pgTheme); }));
  new MutationObserver(() => { if (!state.themeLocked) setTheme(siteTheme()); })
    .observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
  ui.reset.addEventListener('click', () => open('', 'Chart reset; edit history cleared.'));
  paintTheme();

  engine.ready.then(async () => {
    await installRuntime();
    // Show the canvas first so the runtime picks the wide or narrow drawing from its real width.
    ui.boot.hidden = true;
    ui.canvas.hidden = false;
    await open('', 'Engine ready in ' + engine.bootMs + ' ms. Pick a tool above the chart.');
    ui.reset.disabled = false;
  }).catch(showFailure);

  async function installRuntime() {
    // The engine carries the runtime it prepares sections for, so page and host always match.
    const runtime = await engine.annotationRuntime();
    if (!runtime.ok) throw new Error(runtime.error);
    const style = document.createElement('style');
    style.textContent = runtime.css;
    document.head.append(style);
    const script = document.createElement('script');
    script.textContent = runtime.script;
    document.head.append(script);
  }

  // Opens the starting scene (empty basis) or the current scene in the current theme.
  async function open(basis, message) {
    const reply = await engine.openAnnotations(basis, state.theme);
    if (!reply.ok) return showStatus(reply.error, true);
    state.replies.clear();
    if (state.chart) {
      state.chart.update(reply.html, { basis: reply.basis });
    } else {
      ui.canvas.innerHTML = reply.html;
      mount(reply.basis);
    }
    state.basis = reply.basis;
    showStatus(message);
  }

  function mount(basis) {
    const chart = window.ChartForgeXCharts.mount(ui.canvas.querySelector('.cfx-interactive-chart'));
    chart.connectNative(async (request, signal) => {
      const begin = performance.now();
      const reply = await engine.prepareAnnotation(request, state.theme);
      if (signal.aborted) throw new DOMException('Canceled', 'AbortError');
      if (!reply.ok) throw new Error(reply.error);
      state.replies.set(request.requestId, { basis: reply.basis, ms: Math.round(performance.now() - begin) });
      return { html: reply.html, basis: reply.basis };
    }, { basis, axes: null });
    chart.root.addEventListener('cfxannotationchange', (event) => {
      const reply = state.replies.get(event.detail.requestId);
      state.replies.delete(event.detail.requestId);
      if (!reply) return;
      state.basis = reply.basis;
      showStatus('Prepared by the engine in ' + reply.ms + ' ms.');
    });
    chart.root.addEventListener('cfxannotationerror', (event) => showStatus(event.detail.message, true));
    chart.editAnnotations({ createId: (kind) => uniqueId(chart.root, kind) });
    state.chart = chart;
  }

  function uniqueId(root, kind) {
    const prefix = kind === 'Note' ? 'note' : kind === 'LineSegment' ? 'line' : 'channel';
    let id;
    do id = prefix + '-' + (++state.nextId);
    while (Array.from(root.querySelectorAll('[data-cfx-annotation-id]')).some((node) => node.dataset.cfxAnnotationId === id));
    return id;
  }

  function setTheme(theme) {
    const next = theme === 'dark' ? 'dark' : 'light';
    if (next === state.theme) return;
    state.theme = next;
    paintTheme();
    // Re-prepare the current drawings in the new theme. A theme change is a new scene, so edit history starts again.
    if (state.chart) open(state.basis, 'Theme changed; the drawings were kept.').catch(showFailure);
  }

  function paintTheme() {
    ui.preview.dataset.pgPreviewTheme = state.theme;
    ui.themes.forEach((button) => button.setAttribute('aria-pressed', String(button.dataset.pgTheme === state.theme)));
  }

  function showStatus(message, error = false) {
    const span = document.createElement('span');
    if (error) span.className = 'is-error';
    span.textContent = message;
    ui.status.replaceChildren(span);
  }

  function showProgress(progress) {
    if (progress.phase === 'failed') return;
    const ratio = progress.total ? progress.loaded / progress.total : 0;
    const percent = Math.round((progress.phase === 'ready' ? 1 : ratio) * 100);
    ui.meter.style.width = percent + '%';
    ui.meter.parentElement.setAttribute('aria-valuenow', String(percent));
    if (progress.phase === 'downloading' && progress.total) {
      ui.bootText.textContent = 'Downloading ' + progress.loaded + ' of ' + progress.total + ' files. They are cached for your next visit.';
    } else if (progress.phase === 'starting') {
      ui.bootText.textContent = 'Starting the .NET runtime.';
    }
  }

  function showFailure(error) {
    ui.boot.hidden = false;
    ui.canvas.hidden = true;
    ui.boot.classList.add('is-failed');
    ui.bootTitle.textContent = 'The engine could not start';
    ui.bootText.textContent = (error && error.message ? error.message : String(error)) + ' The editor needs WebAssembly. Reload the page to try again.';
    showStatus('The ChartForgeX engine did not start in this browser.', true);
  }
}

function siteTheme() {
  return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light';
}
