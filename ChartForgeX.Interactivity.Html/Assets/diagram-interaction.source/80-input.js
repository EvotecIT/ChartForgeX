    const itemFor = target => target?.closest?.('[data-cfx-diagram-item]') ? graph.owners.get(target.closest('[data-cfx-diagram-item]')) : null;
    listen(svg, 'pointerover', event => {
      const item = itemFor(event.target);
      if (item && item !== state.hovered) { state.hovered = item; render(); }
    });
    listen(stage, 'pointerleave', () => { if (state.hovered) { state.hovered = null; render(); } });
    listen(svg, 'click', event => {
      const item = itemFor(event.target);
      if (item && document.activeElement === item.focus) { state.focused = item; render(); return; }
      if (item) { item.focus.focus({ preventScroll: true }); return; }
      if (state.focused) state.focused.focus.blur();
      state.focused = null; state.hovered = null; render();
    });
    listen(root, 'focusin', event => {
      const item = itemFor(event.target);
      if (!item || item.focus !== event.target) return;
      rove(item); state.focused = item; render();
    });
    listen(root, 'focusout', event => {
      if (!state.focused || root.contains(event.relatedTarget) && itemFor(event.relatedTarget)) return;
      state.focused = null; render();
    });
    listen(root, 'keydown', event => {
      if (event.key === 'Escape') {
        if (!state.hovered && !state.focused && !state.played && !state.timer) return;
        stop();
        state.hovered = null; state.focused = null; state.played = null; state.step = 0;
        render(); updatePlayback();
        event.preventDefault();
        return;
      }
      const item = itemFor(event.target);
      if (!item || item.focus !== event.target || event.altKey || event.ctrlKey || event.metaKey) return;
      const index = graph.items.indexOf(item), last = graph.items.length - 1;
      const next = { ArrowRight: index + 1, ArrowDown: index + 1, ArrowLeft: index - 1, ArrowUp: index - 1, Home: 0, End: last }[event.key];
      if (next === undefined) return;
      event.preventDefault();
      const target = graph.items[Math.max(0, Math.min(last, next))];
      // Moving onto the already focused item after Escape restores its readout.
      if (target === item) { state.focused = item; render(); return; }
      target.focus.focus({ preventScroll: false });
    });
    listen(window, 'resize', () => { if (!tooltip.hidden) render(); });
    lifetime.own(() => {
      for (const element of root.querySelectorAll('[data-cfx-diagram-item]')) {
        element.removeAttribute('data-cfx-diagram-item'); element.removeAttribute('data-cfx-diagram-state');
      }
      for (const item of graph.items) ['tabindex', 'role', 'aria-label'].forEach(name => item.focus.removeAttribute(name));
      state.ring?.remove();
      root.removeAttribute('data-cfx-diagram-active');
    });
  };

  const mount = scope => {
    const roots = scope?.matches?.(selector) ? [scope] : Array.from((scope || document).querySelectorAll(selector));
    roots.forEach(bind);
  };
  window[key] = Object.freeze({ mount });
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => mount(document), { once: true });
  else mount(document);
})();
