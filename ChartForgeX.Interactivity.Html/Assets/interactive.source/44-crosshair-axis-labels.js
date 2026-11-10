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
  // The value pill sits left of the plot and stays inside the visible card: with too little room it overlaps the plot edge.
  const paintCrosshairAxisLabels = (root, crosshair, node, x, y, bounds, room) => {
    const labels = [crosshairXText(node), tooltipValueText(node)];
    if (root._cfxDataProjection?.horizontal) labels.reverse();
    ['x', 'y'].forEach((axis, index) => {
      const label = crosshairAxisLabel(crosshair, axis, labels[index]);
      if (axis === 'x') {
        label.style.left = clamp(x - label.offsetWidth / 2, 0, Math.max(0, bounds.width - label.offsetWidth)) + 'px';
        label.style.top = bounds.height + 4 + 'px';
      } else {
        label.style.left = Math.max(-label.offsetWidth - 4, 4 - room) + 'px';
        label.style.top = clamp(y - label.offsetHeight / 2, 0, Math.max(0, bounds.height - label.offsetHeight)) + 'px';
      }
    });
  };
