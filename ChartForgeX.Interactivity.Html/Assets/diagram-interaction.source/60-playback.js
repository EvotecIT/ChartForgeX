    // Sequence playback walks the messages in order, like use case playback on topology pages.
    const toolbar = root.querySelector('.cfx-diagram-toolbar');
    const control = name => toolbar?.querySelector(`[data-cfx-diagram-step="${name}"]`);
    const progress = toolbar?.querySelector('[data-cfx-diagram-progress]');
    const playButton = control('play');
    const updatePlayback = () => {
      if (!toolbar) return;
      const count = graph.messages.length, playing = !!state.timer;
      control('previous').disabled = state.step <= 1;
      control('next').disabled = state.step >= count;
      playButton.setAttribute('aria-pressed', playing ? 'true' : 'false');
      playButton.textContent = playing ? text('Pause', 'Pause') : text('Play', 'Play');
      if (progress) progress.textContent = state.step
        ? text('StepProgress', 'Step {current} / {count}', { current: state.step, count })
        : text('Steps', '{count} steps', { count });
    };
    const stop = () => {
      if (state.timer) lifetime.clearTimeout(state.timer);
      state.timer = 0;
      updatePlayback();
    };
    const setStep = step => {
      state.step = Math.max(0, Math.min(graph.messages.length, step));
      state.played = state.step ? graph.messages[state.step - 1] : null;
      if (state.played) rove(state.played);
      render(); updatePlayback();
    };
    const tick = () => {
      state.timer = 0;
      setStep(state.step + 1);
      if (state.step < graph.messages.length) { state.timer = lifetime.timeout(tick, playbackDelay); updatePlayback(); }
    };
    if (toolbar && graph.messages.length) {
      toolbar.hidden = false;
      listen(control('previous'), 'click', () => { stop(); setStep(state.step - 1); });
      listen(control('next'), 'click', () => { stop(); setStep(state.step + 1); });
      listen(playButton, 'click', () => {
        if (state.timer) { stop(); return; }
        if (state.step >= graph.messages.length) state.step = 0;
        tick();
      });
      // Safari does not focus a clicked button, so Escape would never reach the diagram after Play or Next.
      listen(toolbar, 'click', event => {
        const button = event.target.closest?.('button');
        if (button && document.activeElement !== button) button.focus({ preventScroll: true });
      });
      updatePlayback();
    }
