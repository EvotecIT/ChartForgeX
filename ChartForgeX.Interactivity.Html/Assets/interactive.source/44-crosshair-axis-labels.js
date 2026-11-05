  const crosshairAxisLabel = (crosshair, axis, value) => {
    let label = crosshair.querySelector('[data-cfx-axis-value="' + axis + '"]');
    if (!label) {
      label = document.createElement('span'); label.className = 'cfx-crosshair__axis-value';
      label.dataset.cfxAxisValue = axis; crosshair.appendChild(label);
    }
    label.textContent = value ?? ''; label.hidden = !label.textContent;
    return label;
  };
  const crosshairXText = node => categoryReadoutText(node, node.dataset.cfxXLabel ?? node.dataset.cfxCategoryLabel ?? tooltipValueText(node, 'X'));
  const paintCrosshairAxisLabels = (root, crosshair, node, x, y, bounds) => {
    const labels = [crosshairXText(node), tooltipValueText(node)];
    if (root._cfxDataProjection?.horizontal) labels.reverse();
    ['x', 'y'].forEach((axis, index) => {
      const label = crosshairAxisLabel(crosshair, axis, labels[index]);
      if (axis === 'x') {
        label.style.left = clamp(x - label.offsetWidth / 2, 0, Math.max(0, bounds.width - label.offsetWidth)) + 'px';
        label.style.top = bounds.height + 4 + 'px';
      } else {
        label.style.left = -label.offsetWidth - 4 + 'px';
        label.style.top = clamp(y - label.offsetHeight / 2, 0, Math.max(0, bounds.height - label.offsetHeight)) + 'px';
      }
    });
  };
