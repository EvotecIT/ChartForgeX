// ChartForgeX playground page: editor, live WebAssembly render, theme preview, zoom, downloads and share links.
import { loadPlaygroundEngine } from '/themes/chartforgex/assets/playground-engine.js';

const MAX_SOURCE = 65536;
const STORAGE_KEY = 'cfx-playground';
const RENDER_DELAY = 220;
const SNIPPETS = {
  mermaid: {
    package: 'dotnet add package ChartForgeX.Mermaid',
    intro: 'MermaidRenderer reads the same source and returns a visual artifact that saves as SVG, PNG or HTML.',
    code: (source) => [
      'using ChartForgeX.Mermaid;',
      '',
      'var source = ' + csharpRawString(source) + ';',
      '',
      'var result = MermaidRenderer.Render(source);',
      'if (!result.HasErrors && result.Artifact != null) {',
      '    result.Artifact.SaveSvg("diagram.svg");',
      '    result.Artifact.SavePng("diagram.png");',
      '}'
    ].join('\n')
  },
  chartforgex: {
    package: 'dotnet add package ChartForgeX.Markup.Mermaid',
    intro: 'MermaidVisualMarkupParser reads the whole Markdown document, chartforgex and mermaid fences alike, and returns one artifact per fence.',
    code: (source) => [
      'using ChartForgeX.Markup.Mermaid;',
      '',
      'var markdown = ' + csharpRawString(source) + ';',
      '',
      'var result = new MermaidVisualMarkupParser().Parse(markdown);',
      'for (var i = 0; i < result.Artifacts.Count; i++) {',
      '    result.Artifacts[i].SaveSvg($"visual-{i + 1}.svg");',
      '}'
    ].join('\n')
  }
};

const root = document.querySelector('[data-cfx-playground]');
if (root) start(root);

function start(root) {
  const $ = (selector) => root.querySelector(selector);
  const $$ = (selector) => Array.from(root.querySelectorAll(selector));
  const ui = {
    source: $('[data-pg-source]'), gutter: $('[data-pg-gutter]'), mark: $('[data-pg-mark]'),
    cursor: $('[data-pg-cursor]'), length: $('[data-pg-length]'), samples: $('[data-pg-samples]'),
    preview: $('.cfx-pg__preview'), stage: $('[data-pg-stage]'), canvas: $('[data-pg-canvas]'),
    boot: $('[data-pg-boot]'), bootTitle: $('.cfx-pg__boot-title'), bootText: $('[data-pg-boot-text]'),
    meter: $('[data-pg-meter]'), status: $('[data-pg-status]'), share: $('[data-pg-share]'),
    snippet: $('[data-pg-snippet]'), snippetIntro: $('[data-pg-snippet-intro]'), packageText: $('[data-pg-package]'),
    copyPackage: $('[data-pg-copy-package]'),
    languages: $$('[data-language]'), themes: $$('[data-pg-theme]'), zooms: $$('[data-pg-zoom]'), downloads: $$('[data-pg-download]')
  };

  const stored = readStorage();
  const state = {
    language: (new URLSearchParams(location.search).get('lang') || stored.language) === 'chartforgex' ? 'chartforgex' : 'mermaid',
    sources: { mermaid: null, chartforgex: null, ...(stored.sources || {}) },
    theme: siteTheme(),
    themeLocked: false,
    zoom: 'fit',
    scale: 1,
    samples: [],
    result: null,
    lastGood: null,
    token: 0,
    timer: 0,
    lineCount: 0,
    errorLine: null,
    leaveOnTab: false
  };
  if (state.sources[state.language]) ui.source.value = state.sources[state.language];
  // The page ships a Mermaid starter; other languages start from their first sample once the engine is ready.
  else if (state.language !== 'mermaid') ui.source.value = '';

  const engine = loadPlaygroundEngine({ onProgress: showProgress });

  // Shared links and linked sample files win over the stored draft.
  readLinkedSource().then((shared) => {
    if (shared) {
      state.language = shared.language;
      ui.source.value = shared.source;
    }
    selectLanguageTab();
    refreshEditor();
    updateSnippet();
    return engine.ready;
  }).then(async () => {
    state.samples = await engine.samples();
    fillSamples();
    if (!ui.source.value.trim()) loadFirstSample(state.language);
    showCanvas();
    renderNow();
  }).catch(showBootFailure);

  // Editor.
  ui.source.addEventListener('input', () => {
    ui.samples.value = '';
    state.sources[state.language] = ui.source.value;
    writeStorage();
    refreshEditor();
    updateSnippet();
    scheduleRender();
  });
  ui.source.addEventListener('scroll', syncScroll);
  ['keyup', 'click', 'select'].forEach((type) => ui.source.addEventListener(type, updateCursor));
  ui.source.addEventListener('keydown', onEditorKey);

  // Toolbar.
  ui.languages.forEach((tab) => {
    tab.addEventListener('click', () => switchLanguage(tab.dataset.language));
    tab.addEventListener('keydown', (event) => {
      if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return;
      const next = ui.languages[(ui.languages.indexOf(tab) + (event.key === 'ArrowRight' ? 1 : ui.languages.length - 1)) % ui.languages.length];
      next.focus();
      switchLanguage(next.dataset.language);
    });
  });
  ui.samples.addEventListener('change', () => {
    const sample = state.samples.find((item) => item.id === ui.samples.value);
    if (!sample) return;
    if (sample.language !== state.language) {
      state.language = sample.language;
      selectLanguageTab();
    }
    setSource(sample.source);
    ui.samples.value = sample.id;
  });
  ui.themes.forEach((button) => button.addEventListener('click', () => {
    state.themeLocked = true;
    setPreviewTheme(button.dataset.pgTheme);
  }));
  new MutationObserver(() => {
    if (!state.themeLocked) setPreviewTheme(siteTheme());
  }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
  setPreviewTheme(state.theme);

  ui.zooms.forEach((button) => button.addEventListener('click', () => setZoom(button.dataset.pgZoom)));
  ui.downloads.forEach((button) => button.addEventListener('click', () => download(button.dataset.pgDownload, button)));
  ui.share.addEventListener('click', share);
  ui.copyPackage.addEventListener('click', () => copy(ui.packageText.textContent, ui.copyPackage));

  function switchLanguage(language) {
    if (language === state.language) return;
    state.sources[state.language] = ui.source.value;
    state.language = language;
    selectLanguageTab();
    if (state.sources[language]) setSource(state.sources[language]);
    else loadFirstSample(language);
    writeStorage();
  }

  function selectLanguageTab() {
    ui.languages.forEach((tab) => {
      const active = tab.dataset.language === state.language;
      tab.setAttribute('aria-selected', String(active));
      tab.tabIndex = active ? 0 : -1;
    });
    updateSnippet();
  }

  function loadFirstSample(language) {
    const sample = state.samples.find((item) => item.language === language);
    if (sample) {
      setSource(sample.source);
      ui.samples.value = sample.id;
    }
  }

  function setSource(text) {
    ui.source.value = text;
    ui.source.scrollTop = 0;
    state.sources[state.language] = text;
    writeStorage();
    refreshEditor();
    updateSnippet();
    renderNow();
  }

  function fillSamples() {
    ui.samples.textContent = '';
    const placeholder = new Option('Choose a sample', '');
    ui.samples.add(placeholder);
    const groups = [['mermaid', 'Mermaid'], ['chartforgex', 'ChartForgeX markup']];
    for (const [language, label] of groups) {
      const group = document.createElement('optgroup');
      group.label = label;
      state.samples.filter((sample) => sample.language === language)
        .forEach((sample) => group.appendChild(new Option(sample.title, sample.id)));
      ui.samples.appendChild(group);
    }
    ui.samples.disabled = false;
  }

  // Line numbers, error band, cursor and length.
  function refreshEditor() {
    const lines = ui.source.value.split('\n').length;
    if (lines !== state.lineCount) {
      state.lineCount = lines;
      const fragment = document.createDocumentFragment();
      for (let index = 1; index <= lines; index++) {
        const number = document.createElement('span');
        number.textContent = String(index);
        fragment.appendChild(number);
      }
      ui.gutter.replaceChildren(fragment);
    }
    markErrorLine(state.errorLine);
    const count = ui.source.value.length;
    ui.length.textContent = count.toLocaleString('en-US') + ' of ' + MAX_SOURCE.toLocaleString('en-US') + ' characters';
    ui.length.classList.toggle('is-over', count > MAX_SOURCE);
    updateCursor();
    syncScroll();
  }

  function markErrorLine(line) {
    state.errorLine = line;
    ui.gutter.querySelectorAll('.is-error').forEach((node) => node.classList.remove('is-error'));
    if (!line || line > state.lineCount) {
      ui.mark.hidden = true;
      return;
    }
    ui.gutter.children[line - 1]?.classList.add('is-error');
    ui.mark.hidden = false;
    syncScroll();
  }

  function syncScroll() {
    ui.gutter.scrollTop = ui.source.scrollTop;
    if (state.errorLine) {
      const lineHeight = parseFloat(getComputedStyle(ui.source).lineHeight) || 22;
      ui.mark.style.transform = 'translateY(' + ((state.errorLine - 1) * lineHeight - ui.source.scrollTop) + 'px)';
    }
  }

  function updateCursor() {
    const before = ui.source.value.slice(0, ui.source.selectionStart);
    const line = before.split('\n').length;
    const column = before.length - before.lastIndexOf('\n');
    ui.cursor.textContent = 'Line ' + line + ', column ' + column;
  }

  function onEditorKey(event) {
    if (event.key === 'Escape') {
      state.leaveOnTab = true;
      return;
    }
    if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
      event.preventDefault();
      renderNow();
      return;
    }
    if (event.key !== 'Tab' || event.altKey || event.ctrlKey || event.metaKey) {
      state.leaveOnTab = false;
      return;
    }
    if (state.leaveOnTab) {
      state.leaveOnTab = false;
      return;
    }
    event.preventDefault();
    indent(event.shiftKey);
  }

  function indent(outdent) {
    const area = ui.source;
    const value = area.value;
    const start = value.lastIndexOf('\n', area.selectionStart - 1) + 1;
    const end = area.selectionEnd;
    if (!outdent && area.selectionStart === end) {
      area.setRangeText('    ', area.selectionStart, end, 'end');
    } else {
      const block = value.slice(start, end);
      const changed = outdent ? block.replace(/^ {1,4}/gm, '') : block.replace(/^/gm, '    ');
      area.setRangeText(changed, start, end, 'select');
    }
    area.dispatchEvent(new Event('input'));
  }

  function jumpTo(line, column) {
    const lines = ui.source.value.split('\n');
    let offset = 0;
    for (let index = 0; index < Math.min(line - 1, lines.length); index++) offset += lines[index].length + 1;
    offset += Math.max(0, (column || 1) - 1);
    ui.source.focus();
    ui.source.setSelectionRange(offset, offset);
    const lineHeight = parseFloat(getComputedStyle(ui.source).lineHeight) || 22;
    ui.source.scrollTop = Math.max(0, (line - 3) * lineHeight);
    updateCursor();
    syncScroll();
  }

  // Rendering.
  function scheduleRender() {
    clearTimeout(state.timer);
    state.timer = setTimeout(renderNow, RENDER_DELAY);
  }

  async function renderNow() {
    clearTimeout(state.timer);
    if (engine.status !== 'ready') return;
    const token = ++state.token;
    const result = await engine.render(state.language, ui.source.value, state.theme);
    if (token !== state.token) return;
    state.result = result;
    if (result.ok && result.svg) {
      state.lastGood = { language: state.language, source: ui.source.value, theme: state.theme };
      ui.canvas.replaceChildren(...sanitizeSvgs(result.svg));
      ui.canvas.style.opacity = '';
      applyZoom();
      markErrorLine(null);
      setDownloads(true);
      showOk(result);
      return;
    }
    markErrorLine(result.line || null);
    setDownloads(false);
    showError(result);
    // Keep the last good render visible, dimmed, in the current preview theme.
    const good = state.lastGood;
    if (good && good.theme !== state.theme) {
      const previous = await engine.render(good.language, good.source, state.theme);
      if (token !== state.token) return;
      if (previous.ok && previous.svg) {
        good.theme = state.theme;
        ui.canvas.replaceChildren(...sanitizeSvgs(previous.svg));
        applyZoom();
      }
    }
    ui.canvas.style.opacity = ui.canvas.childElementCount ? '.35' : '';
  }

  function showOk(result) {
    const warnings = (result.diagnostics || []).filter((item) => item.severity === 'warning');
    const parts = [];
    parts.push(element('span', null, [element('strong', null, 'Rendered'), ' ' + describe(result) + ' in ' + formatMs(result.elapsedMs ?? result.wallMs)]));
    if (warnings.length) {
      const first = warnings[0];
      parts.push(element('span', null, warnings.length + (warnings.length === 1 ? ' warning: ' : ' warnings, first: ') + first.message));
    }
    parts.push(element('span', null, 'ChartForgeX ' + (engine.info?.version || '') + ' in WebAssembly'));
    ui.status.replaceChildren(...parts);
  }

  function showError(result) {
    const parts = [];
    if (result.line) {
      const jump = element('button', null, 'Line ' + result.line + (result.column ? ', column ' + result.column : ''));
      jump.type = 'button';
      jump.addEventListener('click', () => jumpTo(result.line, result.column));
      parts.push(jump);
    }
    parts.push(element('span', 'is-error', result.error || 'The source could not be rendered.'));
    if (ui.canvas.childElementCount) parts.push(element('span', 'is-stale', 'Showing the last successful render.'));
    ui.status.replaceChildren(...parts);
  }

  function describe(result) {
    const type = (result.diagramType || '').split(',').filter(Boolean);
    const count = result.artifactCount || 1;
    if (count > 1) return count + ' visuals';
    return type.length ? type[0] : 'the visual';
  }

  // Preview theme and zoom.
  function setPreviewTheme(theme) {
    const changed = theme !== state.theme;
    state.theme = theme === 'dark' ? 'dark' : 'light';
    ui.preview.dataset.pgPreviewTheme = state.theme;
    ui.themes.forEach((button) => button.setAttribute('aria-pressed', String(button.dataset.pgTheme === state.theme)));
    if (changed) renderNow();
  }

  function setZoom(action) {
    const svgs = Array.from(ui.canvas.querySelectorAll(':scope > svg'));
    const current = state.zoom === 'fit' && svgs[0] ? svgs[0].getBoundingClientRect().width / naturalSize(svgs[0]).width : state.scale;
    if (action === 'fit') state.zoom = 'fit';
    else {
      state.zoom = 'scale';
      state.scale = action === '1' ? 1 : clamp(current * (action === 'in' ? 1.25 : 0.8), 0.2, 4);
    }
    applyZoom();
  }

  function applyZoom() {
    const fit = state.zoom === 'fit';
    ui.canvas.classList.toggle('is-fit', fit);
    ui.canvas.querySelectorAll(':scope > svg').forEach((svg) => {
      svg.style.width = fit ? '' : Math.round(naturalSize(svg).width * state.scale) + 'px';
    });
    ui.zooms.forEach((button) => {
      const pressed = (fit && button.dataset.pgZoom === 'fit') || (!fit && state.scale === 1 && button.dataset.pgZoom === '1');
      button.setAttribute('aria-pressed', String(pressed));
    });
  }

  // Engine boot.
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

  function showCanvas() {
    ui.boot.hidden = true;
    ui.canvas.hidden = false;
  }

  function showBootFailure(error) {
    ui.boot.classList.add('is-failed');
    ui.bootTitle.textContent = 'The engine could not start';
    ui.bootText.textContent = (error && error.message ? error.message : String(error)) +
      ' The playground needs WebAssembly. Reload the page to try again, or render the source from .NET with the snippet below.';
    ui.status.replaceChildren(element('span', 'is-error', 'The ChartForgeX engine did not start in this browser.'));
  }

  // Downloads and sharing.
  function setDownloads(enabled) {
    ui.downloads.forEach((button) => { button.disabled = !enabled; });
  }

  async function download(kind, button) {
    const markup = composedSvg();
    if (!markup) return;
    const name = 'chartforgex-' + slug(describe(state.result || {})) + '-' + state.theme;
    if (kind === 'svg') {
      saveBlob(new Blob([markup.text], { type: 'image/svg+xml' }), name + '.svg');
      flash(button, 'Saved');
      return;
    }
    try {
      const image = new Image();
      image.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(markup.text);
      await image.decode();
      const scale = 2;
      const canvas = document.createElement('canvas');
      canvas.width = Math.ceil(markup.width * scale);
      canvas.height = Math.ceil(markup.height * scale);
      const context = canvas.getContext('2d');
      context.scale(scale, scale);
      context.drawImage(image, 0, 0, markup.width, markup.height);
      const blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/png'));
      if (!blob) throw new Error('The browser could not encode the PNG.');
      saveBlob(blob, name + '.png');
      flash(button, 'Saved');
    } catch (error) {
      ui.status.replaceChildren(element('span', 'is-error', 'PNG export failed: ' + (error.message || error) + ' Download the SVG instead.'));
    }
  }

  function composedSvg() {
    const svgs = Array.from(ui.canvas.querySelectorAll(':scope > svg'));
    if (!svgs.length) return null;
    const serializer = new XMLSerializer();
    if (svgs.length === 1) {
      const size = naturalSize(svgs[0]);
      const copy = svgs[0].cloneNode(true);
      copy.removeAttribute('style');
      copy.setAttribute('width', String(size.width));
      copy.setAttribute('height', String(size.height));
      return { text: serializer.serializeToString(copy), width: size.width, height: size.height };
    }
    // Several fences: stack them in one valid SVG document.
    const gap = 32;
    const sizes = svgs.map(naturalSize);
    const width = Math.max(...sizes.map((size) => size.width));
    const height = sizes.reduce((sum, size) => sum + size.height, 0) + gap * (svgs.length - 1);
    const outer = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    outer.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
    outer.setAttribute('width', String(width));
    outer.setAttribute('height', String(height));
    outer.setAttribute('viewBox', '0 0 ' + width + ' ' + height);
    let y = 0;
    svgs.forEach((svg, index) => {
      const copy = svg.cloneNode(true);
      copy.removeAttribute('style');
      copy.setAttribute('x', String(Math.round((width - sizes[index].width) / 2)));
      copy.setAttribute('y', String(y));
      copy.setAttribute('width', String(sizes[index].width));
      copy.setAttribute('height', String(sizes[index].height));
      outer.appendChild(copy);
      y += sizes[index].height + gap;
    });
    return { text: serializer.serializeToString(outer), width, height };
  }

  async function share() {
    const payload = JSON.stringify({ v: 1, l: state.language, s: ui.source.value });
    let hash;
    if ('CompressionStream' in window) {
      const stream = new Blob([payload]).stream().pipeThrough(new CompressionStream('deflate-raw'));
      hash = '#code=' + toBase64Url(new Uint8Array(await new Response(stream).arrayBuffer()));
    } else {
      hash = '#text=' + toBase64Url(new TextEncoder().encode(payload));
    }
    const url = location.origin + location.pathname + hash;
    history.replaceState(null, '', hash);
    await copy(url, ui.share, 'Link copied');
  }

  async function readLinkedSource() {
    const shared = await readShareLink();
    if (shared) return shared;
    // Site pages link their own samples, for example /playground/?src=/site/mermaid-gantt.mmd.
    const src = new URLSearchParams(location.search).get('src');
    if (!src || !/^\/site\/[\w.-]+\.(mmd|md)$/.test(src)) return null;
    try {
      const response = await fetch(src);
      if (!response.ok) return null;
      const text = await response.text();
      return { language: src.endsWith('.md') ? 'chartforgex' : 'mermaid', source: text.slice(0, MAX_SOURCE) };
    } catch {
      return null;
    }
  }
  async function readShareLink() {
    const match = /^#(code|text)=([A-Za-z0-9_-]+)$/.exec(location.hash);
    if (!match) return null;
    try {
      let bytes = fromBase64Url(match[2]);
      if (match[1] === 'code') {
        const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream('deflate-raw'));
        bytes = new Uint8Array(await new Response(stream).arrayBuffer());
      }
      const data = JSON.parse(new TextDecoder().decode(bytes));
      if (typeof data.s !== 'string') return null;
      return { language: data.l === 'chartforgex' ? 'chartforgex' : 'mermaid', source: data.s.slice(0, MAX_SOURCE) };
    } catch {
      ui.status.replaceChildren(element('span', 'is-error', 'The shared link could not be read, so your last draft is shown.'));
      return null;
    }
  }

  function updateSnippet() {
    const snippet = SNIPPETS[state.language];
    ui.snippetIntro.textContent = snippet.intro;
    ui.packageText.textContent = snippet.package;
    ui.snippet.textContent = snippet.code(ui.source.value);
    if (typeof window.cfxHighlight === 'function') window.cfxHighlight(ui.snippet);
  }

  function writeStorage() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ language: state.language, sources: state.sources }));
    } catch { /* Storage is optional. */ }
  }
}

// Helpers.
function readStorage() {
  try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}') || {}; } catch { return {}; }
}

function siteTheme() {
  return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light';
}

// The engine escapes text, but shared links can carry anyone's source, so keep only inert SVG.
function sanitizeSvgs(markup) {
  const documentNode = new DOMParser().parseFromString('<root xmlns="http://www.w3.org/2000/svg">' + markup + '</root>', 'image/svg+xml');
  if (documentNode.querySelector('parsererror')) return [];
  const svgs = Array.from(documentNode.documentElement.children).filter((node) => node.localName === 'svg');
  for (const svg of svgs) {
    svg.querySelectorAll('script, foreignObject, iframe, object, embed').forEach((node) => node.remove());
    for (const node of [svg, ...svg.querySelectorAll('*')]) {
      for (const attribute of Array.from(node.attributes)) {
        const name = attribute.name.toLowerCase();
        const value = attribute.value.trim().toLowerCase();
        if (name.startsWith('on')) node.removeAttribute(attribute.name);
        else if ((name === 'href' || name === 'xlink:href') && !(value.startsWith('#') || value.startsWith('https:') || value.startsWith('http:'))) node.removeAttribute(attribute.name);
      }
    }
  }
  return svgs.map((svg) => document.importNode(svg, true));
}

function naturalSize(svg) {
  const box = svg.viewBox && svg.viewBox.baseVal;
  const width = parseFloat(svg.getAttribute('width')) || (box && box.width) || 800;
  const height = parseFloat(svg.getAttribute('height')) || (box && box.height) || 600;
  return { width, height };
}

function csharpRawString(text) {
  let quotes = '"""';
  while (text.includes(quotes)) quotes += '"';
  return quotes + '\n' + text.replace(/\s+$/, '') + '\n' + quotes;
}

function element(tag, className, content) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  for (const part of [].concat(content)) node.append(part);
  return node;
}

function formatMs(value) {
  if (value == null) return '';
  return (value < 10 ? Math.round(value * 10) / 10 : Math.round(value)) + ' ms';
}

function slug(text) {
  return String(text || 'visual').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'visual';
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function saveBlob(blob, name) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = name;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

async function copy(text, button, done = 'Copied') {
  try {
    await navigator.clipboard.writeText(text);
    flash(button, done);
  } catch {
    flash(button, 'Copy failed');
  }
}

function flash(button, label) {
  const original = button.dataset.label || button.textContent;
  button.dataset.label = original;
  button.textContent = label;
  button.setAttribute('data-done', '');
  setTimeout(() => {
    button.textContent = original;
    button.removeAttribute('data-done');
  }, 1600);
}

function toBase64Url(bytes) {
  let binary = '';
  for (let index = 0; index < bytes.length; index += 0x8000) binary += String.fromCharCode(...bytes.subarray(index, index + 0x8000));
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fromBase64Url(text) {
  const binary = atob(text.replace(/-/g, '+').replace(/_/g, '/'));
  return Uint8Array.from(binary, (character) => character.charCodeAt(0));
}
