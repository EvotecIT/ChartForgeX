  const comparePageSize = 6;
  const renderCompare = (root, requestedPage) => {
    (root._cfxCompareCleanup || []).forEach(release => release());
    root._cfxCompareCleanup = [];
    const tray = root.querySelector('[data-cfx-compare-tray]');
    if (!tray || !hasFeature(root, 'CompareMarkers')) return [];
    const items = compareItems(root);
    tray.replaceChildren();
    if (!items.length) {
      tray.hidden = true;
      root.removeAttribute('data-cfx-compare-count');
      delete root._cfxComparePage;
      return items;
    }
    tray.hidden = false;
    root.dataset.cfxCompareCount = String(items.length);
    const summary = document.createElement('div');
    summary.className = 'cfx-compare-tray__summary';
    summary.textContent = uiText(root, 'Compare', 'Compare {count}', { count: items.length });
    tray.appendChild(summary);
    const lastPage = Math.floor((items.length - 1) / comparePageSize);
    const page = Math.max(0, Math.min(lastPage, requestedPage ?? root._cfxComparePage ?? 0));
    root._cfxComparePage = page;
    const offset = page * comparePageSize;
    if (lastPage > 0) {
      const range = document.createElement('span');
      range.className = 'cfx-compare-tray__range';
      range.setAttribute('role', 'status');
      range.setAttribute('aria-atomic', 'true');
      range.textContent = uiText(root, 'CompareRange', '{first}–{last} of {count}', {
        first: offset + 1, last: Math.min(offset + comparePageSize, items.length), count: items.length });
      tray.appendChild(range);
      [-1, 1].forEach(direction => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'cfx-compare-page';
        button.dataset.cfxComparePage = direction < 0 ? 'previous' : 'next';
        button.textContent = uiText(root, direction < 0 ? 'Previous' : 'Next', direction < 0 ? 'Prev' : 'Next');
        button.setAttribute('aria-label', uiText(root, direction < 0 ? 'ComparePrevious' : 'CompareNext',
          direction < 0 ? 'Previous selected targets' : 'Next selected targets'));
        button.disabled = direction < 0 ? page === 0 : page === lastPage;
        root._cfxCompareCleanup.push(listenChart(root, button, 'click', () => {
          renderCompare(root, page + direction);
          tray.scrollLeft = 0;
          const chips = tray.querySelectorAll('.cfx-compare-chip');
          const focus = direction < 0 ? chips[chips.length - 1] : chips[0];
          focus?.focus({ preventScroll: true });
          if (focus) scrollKeyboardTargetIntoView(root, focus);
        }));
        tray.appendChild(button);
      });
    }
    items.slice(offset, offset + comparePageSize).forEach((item, index) => {
      const chip = document.createElement('button');
      chip.className = 'cfx-compare-chip';
      chip.type = 'button';
      chip.textContent = item.label;
      chip.dataset.cfxCompareIndex = String(offset + index);
      chip.title = item.label;
      root._cfxCompareCleanup.push(listenChart(root, chip, 'click', () => {
        clearHover(root, false, false);
        if (applyHoverByTarget(root, item.target)) {
          recordFocusTrail(root, item.target, true, true);
          const node = interactiveTargets(root).find(node => matchesTargetIdentity(node, item.target)), tip = root.querySelector('.cfx-tooltip');
          if (node && tip) {
            if (!tooltipSourceInView({ node, tip })) scrollKeyboardTargetIntoView(root, targetFocusNode(node));
            if (root.dataset.cfxPinOnSelect === 'false') showTip(root, tip, node);
            else pinTip(root, tip, node, undefined, false);
          }
          emitHostEvent(root, 'cfxhover', { label: item.label, target: item.target });
        }
      }));
      tray.appendChild(chip);
    });
    const clear = document.createElement('button');
    clear.className = 'cfx-compare-clear';
    clear.type = 'button';
    clear.textContent = uiText(root, 'Clear', 'Clear');
    clear.setAttribute('data-cfx-compare-clear', 'true');
    root._cfxCompareCleanup.push(listenChart(root, clear, 'click', () => {
      const returnFocus = tray.contains(document.activeElement);
      clearSelections(root);
      publishSelectionChange(root, 'clear');
      publishCompare(root, true);
      if (returnFocus) {
        if (!root.hasAttribute('tabindex')) root.setAttribute('tabindex', '-1');
        try { root.focus({ preventScroll: true }); } catch { root.focus(); }
      }
    }));
    tray.appendChild(clear);
    return items;
  };
