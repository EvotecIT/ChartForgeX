// ChartForgeX site behaviour: theme switch, code blocks, tables, the docs outline, the surface stage, tabs,
// and the gallery filter and viewer.
(function () {
  'use strict';

  var root = document.documentElement;
  var theme = function () { return root.dataset.theme === 'dark' ? 'dark' : 'light'; };
  var themeListeners = [];

  document.querySelectorAll('[data-cfx-theme-toggle]').forEach(function (button) {
    button.addEventListener('click', function () {
      var next = theme() === 'dark' ? 'light' : 'dark';
      root.dataset.theme = next;
      try { localStorage.setItem('cfx-theme', next); } catch (e) { /* storage unavailable */ }
      themeListeners.forEach(function (listener) { listener(next); });
    });
  });

  function copyText(text, button) {
    var label = button.getAttribute('data-label') || button.textContent;
    button.setAttribute('data-label', label);
    var done = function () {
      button.setAttribute('data-copied', '');
      setLabel(button, 'Copied');
      setTimeout(function () { button.removeAttribute('data-copied'); setLabel(button, label); }, 1600);
    };
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(done, function () {});
    }
  }

  function setLabel(button, text) {
    var span = button.querySelector('[data-cfx-label]');
    if (span) span.textContent = text; else button.textContent = text;
  }

  document.querySelectorAll('[data-cfx-copy]').forEach(function (button) {
    button.addEventListener('click', function () { copyText(button.getAttribute('data-cfx-copy'), button); });
  });

  // Syntax highlighting. PowerForge injects Prism on pages whose Markdown has code; pages that render code from
  // templates or at runtime load the same self-hosted copy on demand.
  var PRISM_ROOT = '/assets/prism/';
  var prismReady = null;

  function loadScript(src) {
    return new Promise(function (resolve, reject) {
      var script = document.createElement('script');
      script.src = src;
      script.onload = resolve;
      script.onerror = reject;
      document.head.appendChild(script);
    });
  }

  function ensurePrism() {
    var configure = function () {
      var prism = window.Prism;
      if (prism.plugins && prism.plugins.autoloader) prism.plugins.autoloader.languages_path = PRISM_ROOT + 'components/';
      return prism;
    };
    if (window.Prism && window.Prism.highlightElement) return Promise.resolve(configure());
    if (!prismReady) {
      window.Prism = window.Prism || {};
      window.Prism.manual = true;
      prismReady = loadScript(PRISM_ROOT + 'prism-core.js')
        .then(function () { return loadScript(PRISM_ROOT + 'prism-autoloader.js'); })
        .then(configure);
    }
    return prismReady;
  }

  function highlight(code) {
    if (!code || !/\blanguage-/.test(code.className)) return;
    ensurePrism().then(function (prism) { prism.highlightElement(code); }, function () { /* plain text stays readable */ });
  }
  window.cfxHighlight = highlight;

  // One code block component: language key, optional file name and a copy action in a caption bar.
  var LANGUAGES = {
    csharp: 'C#', cs: 'C#', powershell: 'PowerShell', ps1: 'PowerShell', bash: 'Shell', shell: 'Shell', sh: 'Shell',
    json: 'JSON', xml: 'XML', markup: 'HTML', html: 'HTML', markdown: 'Markdown', md: 'Markdown', mermaid: 'Mermaid',
    yaml: 'YAML', css: 'CSS', javascript: 'JavaScript', js: 'JavaScript', typescript: 'TypeScript', text: 'Text'
  };
  var COPY_ICON = '<svg viewBox="0 0 16 16" width="13" height="13" aria-hidden="true"><rect x="5.5" y="5.5" width="8" height="8" rx="1.5" fill="none" stroke="currentColor" stroke-width="1.4"/><path d="M10.5 3.5v-.5A1.5 1.5 0 0 0 9 1.5H4A1.5 1.5 0 0 0 2.5 3v5A1.5 1.5 0 0 0 4 9.5h.5" fill="none" stroke="currentColor" stroke-width="1.4"/></svg>';

  function languageOf(code) {
    var match = /\blanguage-([\w-]+)/.exec(code ? code.className : '');
    return match ? match[1].toLowerCase() : 'text';
  }

  function copyButton(getText) {
    var button = document.createElement('button');
    button.type = 'button';
    button.className = 'cfx-codeblock__btn';
    button.setAttribute('data-cfx-code-copy', '');
    button.innerHTML = COPY_ICON + '<span data-cfx-label>Copy</span>';
    button.addEventListener('click', function () { copyText(getText(), button); });
    return button;
  }

  function wireCodeBlock(block) {
    var pre = block.querySelector('pre');
    var code = pre && pre.querySelector('code');
    if (!code) return;
    var actions = block.querySelector('.cfx-codeblock__actions');
    if (actions && !actions.querySelector('[data-cfx-code-copy]')) {
      actions.appendChild(copyButton(function () { return block.cfxSource || code.textContent; }));
    }
    highlight(code);
  }

  function enhancePre(pre) {
    if (pre.closest('.cfx-codeblock') || pre.closest('[data-cfx-plain-code]')) return;
    var code = pre.querySelector('code');
    if (!code) return;
    var language = languageOf(code);
    var block = document.createElement('figure');
    block.className = 'cfx-codeblock';
    block.setAttribute('data-lang', language);
    var bar = document.createElement('figcaption');
    bar.className = 'cfx-codeblock__bar';
    bar.innerHTML = '<span class="cfx-codeblock__lang"></span><span class="cfx-codeblock__actions"></span>';
    bar.firstChild.textContent = LANGUAGES[language] || language;
    pre.parentNode.insertBefore(block, pre);
    block.appendChild(bar);
    block.appendChild(pre);
    wireCodeBlock(block);
  }

  document.querySelectorAll('.cfx-codeblock').forEach(wireCodeBlock);
  document.querySelectorAll('.cfx-prose pre').forEach(enhancePre);

  // Wide tables scroll inside their own frame instead of widening the page.
  document.querySelectorAll('.cfx-prose table').forEach(function (table) {
    if (table.parentElement.classList.contains('cfx-tablewrap')) return;
    var wrap = document.createElement('div');
    wrap.className = 'cfx-tablewrap';
    wrap.tabIndex = 0;
    wrap.setAttribute('role', 'region');
    wrap.setAttribute('aria-label', 'Table');
    table.parentNode.insertBefore(wrap, table);
    wrap.appendChild(table);
  });

  // The docs contents start folded on narrow screens so the article comes first.
  if (window.matchMedia && window.matchMedia('(max-width: 820px)').matches) {
    document.querySelectorAll('.cfx-docs__menu[open]').forEach(function (menu) { menu.open = false; });
  }

  // Docs outline: the heading in view is marked on the outline's axis.
  var outline = document.querySelector('[data-cfx-outline]');
  if (outline && 'IntersectionObserver' in window) {
    var links = Array.prototype.slice.call(outline.querySelectorAll('a[href^="#"]'));
    var targets = links.map(function (link) { return document.getElementById(decodeURIComponent(link.getAttribute('href').slice(1))); });
    var visible = new Map();
    var mark = function () {
      var active = null;
      targets.forEach(function (target, index) { if (!active && target && visible.get(target)) active = links[index]; });
      if (!active) return;
      links.forEach(function (link) { if (link === active) link.setAttribute('aria-current', 'location'); else link.removeAttribute('aria-current'); });
    };
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) { visible.set(entry.target, entry.isIntersecting); });
      mark();
    }, { rootMargin: '-80px 0px -60% 0px' });
    targets.forEach(function (target) { if (target) observer.observe(target); });
  }

  // Accessible tabs: arrow keys move between tabs, Home/End jump to the ends.
  function wireTabs(tabs, onSelect) {
    tabs.forEach(function (tab, index) {
      tab.addEventListener('click', function () { onSelect(tab); });
      tab.addEventListener('keydown', function (event) {
        var next = null;
        if (event.key === 'ArrowRight') next = tabs[(index + 1) % tabs.length];
        if (event.key === 'ArrowLeft') next = tabs[(index - 1 + tabs.length) % tabs.length];
        if (event.key === 'Home') next = tabs[0];
        if (event.key === 'End') next = tabs[tabs.length - 1];
        if (next) { event.preventDefault(); next.focus(); onSelect(next); }
      });
    });
  }

  function selectTab(tabs, active) {
    tabs.forEach(function (tab) {
      var selected = tab === active;
      tab.setAttribute('aria-selected', selected ? 'true' : 'false');
      tab.tabIndex = selected ? 0 : -1;
      var panel = document.getElementById(tab.getAttribute('aria-controls'));
      if (panel) panel.hidden = !selected;
    });
  }

  // The hero stage: one chart, every surface.
  var stage = document.querySelector('[data-cfx-stage]');
  if (stage) {
    var stageTabs = Array.prototype.slice.call(stage.querySelectorAll('[role="tab"]'));
    var frame = stage.querySelector('[data-cfx-html-frame]');
    var url = stage.querySelector('[data-cfx-html-url]');
    var htmlMode = 'static';

    var loadFrame = function () {
      if (!frame) return;
      var file = 'hero-' + theme() + (htmlMode === 'interactive' ? '.interactive' : '') + '.html';
      if (frame.getAttribute('data-file') !== file) {
        frame.setAttribute('data-file', file);
        frame.src = '/site/' + file;
      }
      if (url) url.textContent = htmlMode === 'interactive' ? 'sign-ins.interactive.html' : 'sign-ins.html';
    };

    wireTabs(stageTabs, function (tab) {
      selectTab(stageTabs, tab);
      var id = tab.getAttribute('data-cfx-surface');
      stage.querySelectorAll('[data-cfx-caption]').forEach(function (caption) {
        caption.hidden = caption.getAttribute('data-cfx-caption') !== id;
      });
      if (id === 'html') loadFrame();
    });

    stage.querySelectorAll('[data-cfx-html-mode]').forEach(function (button) {
      button.addEventListener('click', function () {
        htmlMode = button.getAttribute('data-cfx-html-mode');
        stage.querySelectorAll('[data-cfx-html-mode]').forEach(function (other) {
          other.setAttribute('aria-pressed', other === button ? 'true' : 'false');
        });
        loadFrame();
      });
    });

    themeListeners.push(function () {
      var htmlPanel = document.getElementById('cfx-panel-html');
      if (htmlPanel && !htmlPanel.hidden) loadFrame();
      else if (frame) frame.removeAttribute('data-file');
    });
  }

  // Generic tab groups (Mermaid samples).
  document.querySelectorAll('[data-cfx-tabs]').forEach(function (group) {
    var tabs = Array.prototype.slice.call(group.querySelectorAll('[role="tab"]'));
    wireTabs(tabs, function (tab) { selectTab(tabs, tab); });
  });

  // Gallery filter: the category legend.
  document.querySelectorAll('[data-cfx-filters]').forEach(function (filters) {
    var grid = document.querySelector('[data-cfx-gallery]');
    filters.querySelectorAll('button').forEach(function (button) {
      button.addEventListener('click', function () {
        var value = button.getAttribute('data-filter');
        filters.querySelectorAll('button').forEach(function (other) {
          other.setAttribute('aria-pressed', other === button ? 'true' : 'false');
        });
        grid.querySelectorAll('.cfx-card').forEach(function (card) {
          card.hidden = value !== 'all' && card.getAttribute('data-category') !== value;
        });
      });
    });
  });

  // Gallery viewer. Generated programs keep their gallery-only setup (licensed font fixtures) in a "Setup" region so
  // the download reproduces the published files; the viewer shows and copies the program without it.
  function withoutSetup(text) {
    return text.replace(/^#region Setup\b[^\n]*\n[\s\S]*?^#endregion[^\n]*\n?/gm, '').replace(/\n{3,}/g, '\n\n');
  }

  var viewer = document.querySelector('[data-cfx-viewer]');
  if (viewer && typeof viewer.showModal === 'function') {
    var current = null;
    var request = 0;
    var image = viewer.querySelector('[data-cfx-viewer-image]');
    var code = viewer.querySelector('[data-cfx-viewer-code]');
    var block = code.closest('.cfx-codeblock');

    var render = function () {
      if (!current) return;
      var dark = theme() === 'dark';
      var pick = function (light, darkValue) { return dark && darkValue ? darkValue : light; };
      var imageUrl = pick(current.image, current.darkImage);
      var source = pick(current.source, current.darkSource);
      var ticket = ++request;
      image.src = imageUrl;
      image.alt = current.title + ', ' + current.family + ' rendered by ChartForgeX';
      viewer.querySelector('[data-cfx-viewer-title]').textContent = current.title;
      viewer.querySelector('[data-cfx-viewer-family]').textContent = current.family;
      viewer.querySelector('[data-cfx-viewer-svg]').href = imageUrl;
      viewer.querySelector('[data-cfx-viewer-html]').href = pick(current.html, current.darkHtml);
      var download = viewer.querySelector('[data-cfx-viewer-download]');
      download.href = source;
      download.setAttribute('download', source.split('/').pop().replace(/\.csharp\.txt$/, '.cs'));
      viewer.querySelector('[data-cfx-viewer-file]').textContent = source.split('/').pop().replace(/\.csharp\.txt$/, '.cs');
      block.cfxSource = '';
      code.textContent = 'Loading source…';
      fetch(source).then(function (response) { return response.ok ? response.text() : Promise.reject(); })
        .then(function (text) {
          if (ticket !== request) return;
          block.cfxSource = withoutSetup(text);
          code.textContent = block.cfxSource;
          highlight(code);
        })
        .catch(function () {
          if (ticket !== request) return;
          code.textContent = 'The source file could not be loaded. Open the SVG or HTML output instead.';
        });
    };

    document.querySelectorAll('[data-cfx-gallery-open]').forEach(function (button) {
      button.addEventListener('click', function () {
        var d = button.dataset;
        current = {
          title: d.title, family: d.family, image: d.image, darkImage: d.darkImage,
          source: d.source, darkSource: d.darkSource, html: d.html, darkHtml: d.darkHtml
        };
        render();
        viewer.showModal();
      });
    });

    viewer.querySelector('[data-cfx-viewer-close]').addEventListener('click', function () { viewer.close(); });
    viewer.addEventListener('click', function (event) { if (event.target === viewer) viewer.close(); });
    themeListeners.push(function () { if (viewer.open) render(); });
  }
})();
