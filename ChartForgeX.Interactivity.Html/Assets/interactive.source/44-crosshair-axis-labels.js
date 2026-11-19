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
    coverCrosshairTicks(root, crosshair);
  };
  // A value pill sits on the axis it reads, where tick captions are drawn. Captions it touches would show as clipped
  // fragments around it, so they step aside while the pill names the value. Hidden pills measure empty and cover nothing.
  const crosshairTickSelector = '[data-cfx-role^="axis-"][data-cfx-role$="-label"]:not([data-cfx-role*="title"]),[data-cfx-browser-axis]>text';
  const uncoverCrosshairTicks = root => root.querySelectorAll('.cfx-crosshair-covered').forEach(node => node.classList.remove('cfx-crosshair-covered'));
  const coverCrosshairTicks = (root, crosshair) => {
    const pills = Array.from(crosshair.querySelectorAll('.cfx-crosshair__axis-value'), node => node.getBoundingClientRect()).filter(box => box.width && box.height);
    chartNodes(root, crosshairTickSelector).forEach(node => {
      const box = node.getBoundingClientRect();
      node.classList.toggle('cfx-crosshair-covered', !!(box.width || box.height) && pills.some(pill =>
        box.left < pill.right + 2 && box.right > pill.left - 2 && box.top < pill.bottom + 2 && box.bottom > pill.top - 2));
    });
  };
