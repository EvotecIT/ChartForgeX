
  const captureChartUpdateMotion = value => {
    if (value === undefined || value === null) return Object.freeze({ duration: 200 });
    if (typeof value !== 'object' || !Number.isFinite(value.duration) || value.duration < 0 || value.duration > 1000)
      throw new TypeError('motion.duration: finite 0..1000 ms required.');
    return Object.freeze({ duration: value.duration });
  };
  const settleChartUpdateMotion = root => chartLifetimes.get(root)?.updateMotionRelease?.();
  const settleChartLoadMotion = root => {
    for (const animation of root.querySelector('.cfx-stage')?.getAnimations?.() || [])
      if (animation.animationName === 'cfx-series-enter' && typeof animation.finish === 'function') animation.finish();
  };
  const beginChartUpdateMotion = (root, motion, captured = null) => {
    settleChartUpdateMotion(root);
    settleChartLoadMotion(root);
    if (!motion?.duration || document.hidden) return;
    const lifetime = chartLifetimes.get(root);
    if (!lifetime || lifetime.disposed || !root.isConnected) return;
    const svg = root.querySelector('.cfx-stage svg'), media = window.matchMedia?.('(prefers-reduced-motion: reduce)');
    if (!svg || media?.matches) return;
    if (captured) { beginCapturedValueMotion(root, motion, captured, svg, media); return; }
    const stage = root.querySelector('.cfx-stage');
    if (typeof stage?.animate !== 'function') return;
    const animation = stage.animate([{ opacity: .35 }, { opacity: 1 }], { duration: motion.duration, easing: 'ease-out' });
    svg.dataset.cfxUpdateMotion = 'true';
    const reduced = event => { if (event.matches) release(); };
    const hidden = () => { if (document.hidden) release(); };
    const release = ownChartWork(root, () => {
      animation.cancel(); delete svg.dataset.cfxUpdateMotion;
      media?.removeEventListener('change', reduced); document.removeEventListener('visibilitychange', hidden);
      if (lifetime.updateMotionRelease === release) lifetime.updateMotionRelease = null;
    });
    lifetime.updateMotionRelease = release;
    media?.addEventListener('change', reduced); document.addEventListener('visibilitychange', hidden);
    animation.finished.then(release, release);
  };
