  const paintCrosshairAxisLabels = (root, crosshair, node, x, y, bounds) => {
    const data = node.dataset;
    const labels = [categoryReadoutText(node, data.cfxXLabel ?? data.cfxCategoryLabel ?? tooltipValueText(node, 'X')),
      tooltipValueText(node)];
    if (root._cfxDataProjection?.horizontal) labels.reverse();
    ['x', 'y'].forEach((axis, index) => {
      let label = crosshair.querySelector('[data-cfx-axis-value="' + axis + '"]');
      if (!label) {
        label = document.createElement('span'); label.className = 'cfx-crosshair__axis-value';
        label.dataset.cfxAxisValue = axis; crosshair.appendChild(label);
      }
      label.textContent = labels[index] ?? ''; label.hidden = !label.textContent;
      if (axis === 'x') {
        label.style.left = clamp(x - label.offsetWidth / 2, 0, Math.max(0, bounds.width - label.offsetWidth)) + 'px';
        label.style.top = bounds.height + 4 + 'px';
      } else {
        label.style.left = -label.offsetWidth - 4 + 'px';
        label.style.top = clamp(y - label.offsetHeight / 2, 0, Math.max(0, bounds.height - label.offsetHeight)) + 'px';
      }
    });
  };
