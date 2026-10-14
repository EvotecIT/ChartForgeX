// ChartForgeX site behaviour: theme switch, copy buttons, surface stage, tabs, gallery filter and viewer.
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
    var done = function () {
      var label = button.textContent;
      button.setAttribute('data-copied', '');
      button.textContent = 'Copied';
      setTimeout(function () { button.removeAttribute('data-copied'); button.textContent = label; }, 1600);
    };
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(done, function () {});
    }
  }

  document.querySelectorAll('[data-cfx-copy]').forEach(function (button) {
    button.addEventListener('click', function () { copyText(button.getAttribute('data-cfx-copy'), button); });
  });

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

  // Gallery filter.
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

  // Gallery viewer.
  var viewer = document.querySelector('[data-cfx-viewer]');
  if (viewer && typeof viewer.showModal === 'function') {
    var current = null;
    var sourceText = '';
    var image = viewer.querySelector('[data-cfx-viewer-image]');
    var code = viewer.querySelector('[data-cfx-viewer-code]');

    var render = function () {
      if (!current) return;
      var dark = theme() === 'dark';
      var pick = function (light, darkValue) { return dark && darkValue ? darkValue : light; };
      var imageUrl = pick(current.image, current.darkImage);
      image.src = imageUrl;
      image.alt = current.title + ' (' + current.family + ')';
      viewer.querySelector('[data-cfx-viewer-title]').textContent = current.title;
      viewer.querySelector('[data-cfx-viewer-family]').textContent = current.family;
      viewer.querySelector('[data-cfx-viewer-svg]').href = imageUrl;
      viewer.querySelector('[data-cfx-viewer-html]').href = pick(current.html, current.darkHtml);
      var source = pick(current.source, current.darkSource);
      code.textContent = 'Loading source…';
      fetch(source).then(function (response) { return response.ok ? response.text() : Promise.reject(); })
        .then(function (text) { sourceText = text; code.textContent = text; })
        .catch(function () { sourceText = ''; code.textContent = 'The source file could not be loaded. Open the SVG or HTML output instead.'; });
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
    viewer.querySelector('[data-cfx-viewer-copy]').addEventListener('click', function (event) {
      if (sourceText) copyText(sourceText, event.currentTarget);
    });
    themeListeners.push(function () { if (viewer.open) render(); });
  }
})();
