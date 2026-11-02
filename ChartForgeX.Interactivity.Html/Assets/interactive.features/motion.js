// ChartForgeX motion feature: a quiet load animation and eased window transitions.
// Both honour prefers-reduced-motion and the section's data-cfx-motion-duration.
(() => {
  const registry = window[Symbol.for('ChartForgeX.chartFeatures')] ||= {};
  if (registry.motion) return;
  const easing = 'cubic-bezier(0.25, 1, 0.5, 1)';
  const ease = t => 1 - Math.pow(1 - t, 3);
  // Calls step with an eased 0..1 progress on each frame, then done. Returns a cancel function that skips done.
  // A no-op document animation is the clock, so anything awaiting the page's animations
  // (print, export, tests) also awaits the transition.
  const tween = (root, k, step, done) => {
    const duration = k.motionDuration(root);
    if (!duration || typeof root.animate !== 'function') { done(); return () => {}; }
    const clock = root.animate([{ '--cfx-motion-progress': '0' }, { '--cfx-motion-progress': '1' }], { duration, id: 'cfx-window' });
    let frame = 0, active = true;
    const tick = () => {
      if (!active) return;
      const progress = clock.effect?.getComputedTiming().progress;
      if (Number.isFinite(progress) && progress < 1) step(ease(progress));
      frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    clock.finished.then(() => { if (active) { active = false; cancelAnimationFrame(frame); done(); } }, () => {});
    return () => { active = false; cancelAnimationFrame(frame); clock.cancel(); };
  };
  const metadata = root => { try { return JSON.parse(root.dataset.cfxPreparedChart || '{}'); } catch (_) { return {}; } };
  // Bars grow from their baseline; everything else is revealed from the left by a card-coloured
  // curtain over the plot. Effects never hide or clip a mark, so readouts and hit testing work
  // from the first frame, and the drawing's attributes never change.
  const enter = (root, k) => {
    const duration = k.motionDuration(root), stage = root.querySelector('.cfx-stage'), svg = stage?.querySelector('svg');
    if (!duration || !svg || typeof svg.animate !== 'function') return;
    // Replaces the stylesheet's plain stage fade for this chart.
    root.dataset.cfxMotion = 'enter';
    const prepared = metadata(root), projection = prepared.dataProjection, plot = projection?.plotBounds;
    const options = { duration, easing, fill: 'backwards', id: 'cfx-enter' };
    if (!plot || projection.horizontal) { stage.animate([{ opacity: .35 }, { opacity: 1 }], options); return; }
    const y = projection.y, zero = y && y.minimum <= 0 && y.maximum >= 0 ? 0 : y?.minimum;
    const baseline = y && Number.isFinite(zero) ? plot.y + (1 - (zero - y.minimum) / (y.maximum - y.minimum || 1)) * plot.height : plot.y + plot.height;
    let reveal = false;
    svg.querySelectorAll('[data-cfx-role="series"]').forEach(series => {
      const kind = String(prepared.series?.[Number(series.dataset.cfxSeries)]?.kind || '').toLowerCase();
      if (!['bar', 'stackedbar', 'histogram', 'waterfall'].includes(kind)) { reveal = true; return; }
      series.querySelectorAll('[data-cfx-point]').forEach(point => {
        const box = point.getBBox?.(), at = box ? Math.max(0, Math.min(1, (box.x + box.width / 2 - plot.x) / plot.width)) : 0;
        const origin = { transformBox: 'view-box', transformOrigin: '0 ' + baseline + 'px' };
        point.animate([{ ...origin, transform: 'scaleY(.04)' }, { ...origin, transform: 'none' }], { ...options, delay: at * duration * .25 });
      });
    });
    const card = svg.querySelector('[data-cfx-role="frame-card"]'), fill = card && getComputedStyle(card).fill;
    const matrix = svg.getScreenCTM?.();
    if (!reveal || !matrix || !fill || fill === 'none' || /rgba\(.*,\s*0\)$/.test(fill)) return;
    const corner = (x, y) => new DOMPoint(x, y).matrixTransform(matrix), box = stage.getBoundingClientRect();
    const start = corner(plot.x, plot.y), end = corner(plot.x + plot.width, plot.y + plot.height);
    const curtain = document.createElement('div');
    curtain.dataset.cfxMotionCurtain = '';
    Object.assign(curtain.style, { position: 'absolute', zIndex: '1', pointerEvents: 'none', background: fill, transformOrigin: '100% 50%',
      left: start.x - box.left + stage.scrollLeft + 1 + 'px', top: start.y - box.top + stage.scrollTop - 8 + 'px',
      width: Math.max(0, end.x - start.x + 8) + 'px', height: Math.max(0, end.y - start.y + 7) + 'px' });
    stage.appendChild(curtain);
    curtain.animate([{ transform: 'scaleX(1)' }, { transform: 'scaleX(0)' }], { ...options, fill: 'forwards' }).finished
      .catch(() => {}).then(() => curtain.remove());
  };  registry.motion = Object.freeze({ tween, enter });
})();
