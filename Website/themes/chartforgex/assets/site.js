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

  // Header navigation. Each group button discloses its panel; one panel is open at a time on wide screens, and
  // Escape, a click outside or tabbing out of the group closes it. The phone sheet lists every group open.
  var header = document.querySelector('[data-cfx-header]');
  var compactNav = window.matchMedia('(max-width: 960px)');
  var navToggle = document.getElementById('cfx-nav-toggle');

  function wireNav() {
    var groups = Array.prototype.slice.call(header.querySelectorAll('[data-cfx-navgroup]'));
    var triggerOf = function (group) { return group.querySelector('.cfx-navgroup__trigger'); };
    var setOpen = function (group, open) { triggerOf(group).setAttribute('aria-expanded', open ? 'true' : 'false'); };
    var closeAll = function (except) { groups.forEach(function (group) { if (group !== except) setOpen(group, false); }); };

    groups.forEach(function (group) {
      var trigger = triggerOf(group);
      var items = Array.prototype.slice.call(group.querySelectorAll('.cfx-navitem__link'));
      trigger.addEventListener('click', function () {
        var open = trigger.getAttribute('aria-expanded') !== 'true';
        if (!compactNav.matches) closeAll(group);
        setOpen(group, open);
      });
      trigger.addEventListener('keydown', function (event) {
        if (event.key !== 'ArrowDown' || compactNav.matches || !items.length) return;
        event.preventDefault();
        event.stopPropagation();
        closeAll(group);
        setOpen(group, true);
        items[0].focus();
      });
      group.addEventListener('keydown', function (event) {
        var index = items.indexOf(document.activeElement);
        if (event.key === 'Escape' && !compactNav.matches && trigger.getAttribute('aria-expanded') === 'true') {
          setOpen(group, false);
          trigger.focus();
        } else if (index >= 0 && !compactNav.matches && /^Arrow(Left|Right|Up|Down)$/.test(event.key)) {
          event.preventDefault();
          var step = event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? -1 : 1;
          items[(index + step + items.length) % items.length].focus();
        }
      });
      group.addEventListener('focusout', function (event) {
        if (!compactNav.matches && event.relatedTarget && !group.contains(event.relatedTarget)) setOpen(group, false);
      });
    });

    document.addEventListener('click', function (event) {
      if (!compactNav.matches && !header.contains(event.target)) closeAll(null);
    });
    var reset = function () { if (compactNav.matches) groups.forEach(function (group) { setOpen(group, true); }); else closeAll(null); };
    if (compactNav.addEventListener) compactNav.addEventListener('change', reset);
    reset();
  }

  if (navToggle) {
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && navToggle.checked && compactNav.matches) { navToggle.checked = false; navToggle.focus(); }
    });
  }

  if (header && header.querySelector('[data-cfx-navgroup]')) wireNav();

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
      // Native authoring fences use Prism's existing plain-text grammar, including inside Markdown examples.
      prism.languages.chartforgex = prism.languages.plain;
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
    yaml: 'YAML', css: 'CSS', javascript: 'JavaScript', js: 'JavaScript', typescript: 'TypeScript', chartforgex: 'ChartForgeX markup', text: 'Text'
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

  // Help text indents example code to its own margin and keeps only the first line flush, so the common indent of
  // the remaining lines is removed (or of every line when the first is indented too).
  function dedent(text) {
    var lines = text.replace(/\s+$/, '').replace(/^\s*\n/, '').split('\n');
    var indent = function (line) { return /^[ \t]*/.exec(line)[0].length; };
    var from = lines.length > 1 && indent(lines[0]) === 0 ? 1 : 0;
    var min = Infinity;
    lines.slice(from).forEach(function (line) { if (line.trim()) min = Math.min(min, indent(line)); });
    if (!isFinite(min) || min === 0) return lines.join('\n');
    return lines.map(function (line, index) { return index < from ? line : line.slice(Math.min(min, indent(line))); }).join('\n');
  }

  document.querySelectorAll('.cfx-codeblock').forEach(wireCodeBlock);
  document.querySelectorAll('.cfx-prose pre').forEach(enhancePre);

  // API and command reference examples join the same code block. The bare "PS>" line before each one is dropped;
  // the block's language label says what it is.
  document.querySelectorAll('.pf-api-docs .type-examples pre').forEach(function (pre) {
    var code = pre.querySelector('code');
    if (!code) return;
    code.textContent = dedent(code.textContent);
    var prompt = pre.previousElementSibling;
    if (prompt && prompt.tagName === 'P' && /^PS>$/.test(prompt.textContent.trim())) prompt.remove();
    enhancePre(pre);
  });

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

  // Viewer dialog for gallery cards (gallery-card.html). A card names its output in each theme, the full-size file,
  // a page link and its source file; gallery sources are standalone .csharp.txt programs. A card may also carry tabs,
  // line ranges of that one source (PowerBGInfo shows a script's presentation and data blocks). A card without
  // data-image shows its own image for the page theme at the viewer's size, so the viewer uses whatever responsive
  // variants the build wrote for it.

  var viewer = document.querySelector('[data-cfx-viewer]');
  if (viewer && typeof viewer.showModal === 'function') {
    var current = null;
    var opener = null;
    var request = 0;
    var tabIndex = 0;
    var sources = {};
    var image = viewer.querySelector('[data-cfx-viewer-image]');
    var code = viewer.querySelector('[data-cfx-viewer-code]');
    var block = code.closest('.cfx-codeblock');
    var tablist = viewer.querySelector('[data-cfx-viewer-tabs]');
    var openLink = viewer.querySelector('[data-cfx-viewer-open]');
    var pageLink = viewer.querySelector('[data-cfx-viewer-page]');
    var download = viewer.querySelector('[data-cfx-viewer-download]');
    var pick = function (light, darkValue) { return theme() === 'dark' && darkValue ? darkValue : light; };
    var fileName = function (url) { return url.split('/').pop().replace(/\.csharp\.txt$/, '.cs').replace(/\.txt$/, ''); };

    var loadSource = function (url) {
      if (!sources[url]) {
        sources[url] = fetch(url).then(function (response) { return response.ok ? response.text() : Promise.reject(); });
        sources[url].catch(function () { delete sources[url]; });
      }
      return sources[url];
    };

    var showSource = function () {
      var source = pick(current.source, current.darkSource);
      var range = current.tabs ? current.tabs[tabIndex] : null;
      var ticket = ++request;
      download.href = source;
      download.setAttribute('download', fileName(source));
      viewer.querySelector('[data-cfx-viewer-file]').textContent = fileName(source);
      block.cfxSource = '';
      code.textContent = 'Loading source…';
      loadSource(source)
        .then(function (text) {
          if (ticket !== request) return;
          if (range && range.from) text = text.split('\n').slice(range.from - 1, range.to).join('\n') + '\n';
          block.cfxSource = text;
          code.textContent = text;
          highlight(code);
        })
        .catch(function () {
          if (ticket !== request) return;
          code.textContent = 'The source file could not be loaded. Use the links above instead.';
        });
    };

    var buildTabs = function () {
      tablist.textContent = '';
      tablist.hidden = !current.tabs;
      tabIndex = 0;
      if (!current.tabs) {
        block.removeAttribute('role');
        block.removeAttribute('aria-labelledby');
        return;
      }
      block.setAttribute('role', 'tabpanel');
      var tabs = current.tabs.map(function (entry, index) {
        var tab = document.createElement('button');
        tab.type = 'button';
        tab.id = 'cfx-viewer-tab-' + index;
        tab.setAttribute('role', 'tab');
        tab.setAttribute('aria-controls', block.id);
        tab.innerHTML = '<span class="cfx-key' + (index ? ' cfx-key--hollow' : '') + '" aria-hidden="true"></span>';
        tab.appendChild(document.createTextNode(entry.label));
        tablist.appendChild(tab);
        return tab;
      });
      var choose = function (active, load) {
        tabIndex = tabs.indexOf(active);
        tabs.forEach(function (tab) {
          tab.setAttribute('aria-selected', tab === active ? 'true' : 'false');
          tab.tabIndex = tab === active ? 0 : -1;
        });
        block.setAttribute('aria-labelledby', active.id);
        if (load !== false) showSource();
      };
      wireTabs(tabs, choose);
      choose(tabs[0], false);
    };

    var render = function () {
      if (!current) return;
      var own = current.image ? null : opener.querySelector(theme() === 'dark' ? 'img.cfx-only-dark' : 'img.cfx-only-light');
      if (own) {
        image.sizes = viewer.getAttribute('data-sizes') || '100vw';
        image.srcset = own.getAttribute('srcset') || '';
        image.src = own.getAttribute('src');
        image.width = +own.getAttribute('width');
        image.height = +own.getAttribute('height');
      } else {
        ['srcset', 'sizes', 'width', 'height'].forEach(function (name) { image.removeAttribute(name); });
        image.src = pick(current.image, current.darkImage);
      }
      image.alt = current.alt || current.title + ', ' + current.family + ' rendered by ChartForgeX';
      viewer.querySelector('[data-cfx-viewer-title]').textContent = current.title;
      viewer.querySelector('[data-cfx-viewer-family]').textContent = [current.family, current.note].filter(Boolean).join(' · ');
      openLink.href = pick(current.full, current.darkFull) || image.getAttribute('src');
      var page = pick(current.page, current.darkPage);
      pageLink.hidden = !page;
      if (page) pageLink.href = page;
      showSource();
    };

    document.querySelectorAll('[data-cfx-gallery-open]').forEach(function (button) {
      button.addEventListener('click', function () {
        var d = button.dataset;
        var tabs = null;
        try { tabs = d.tabs ? JSON.parse(d.tabs) : null; } catch (e) { tabs = null; }
        current = {
          title: d.title, family: d.family, note: d.note, alt: d.alt, image: d.image, darkImage: d.darkImage, full: d.full,
          darkFull: d.darkFull, source: d.source, darkSource: d.darkSource, page: d.page, darkPage: d.darkPage, tabs: tabs
        };
        opener = button;
        var card = button.closest('.cfx-card');
        var key = card ? card.style.getPropertyValue('--key') : '';
        if (key) viewer.style.setProperty('--key', key); else viewer.style.removeProperty('--key');
        buildTabs();
        render();
        viewer.showModal();
      });
    });

    viewer.querySelector('[data-cfx-viewer-close]').addEventListener('click', function () { viewer.close(); });
    viewer.addEventListener('click', function (event) { if (event.target === viewer) viewer.close(); });
    // Escape and the close button both end here: focus goes back to the card that opened the viewer.
    viewer.addEventListener('close', function () { if (opener && opener.isConnected) opener.focus(); });
    themeListeners.push(function () { if (viewer.open) render(); });
  }
})();
