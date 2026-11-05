  const svgNamespace = 'http://www.w3.org/2000/svg';

  const unionRect = elements => {
    let left = Infinity, top = Infinity, right = -Infinity, bottom = -Infinity;
    for (const element of elements) {
      const box = element.getBoundingClientRect();
      if (!box.width && !box.height) continue;
      left = Math.min(left, box.left); top = Math.min(top, box.top);
      right = Math.max(right, box.right); bottom = Math.max(bottom, box.bottom);
    }
    return Number.isFinite(left) ? { left, top, right, bottom, width: right - left, height: bottom - top } : null;
  };

  // Steps and participants receive a focus ring in SVG units; lines glow instead because a box around a route reads as clutter.
  const paintRing = (svg, item) => {
    if (item.kind === 'connector' || item.kind === 'message') return null;
    const box = unionRect(item.elements), matrix = svg.getScreenCTM()?.inverse();
    if (!box || !matrix) return null;
    const point = (x, y) => new DOMPoint(x, y).matrixTransform(matrix);
    const start = point(box.left, box.top), end = point(box.right, box.bottom), pad = 4;
    const ring = document.createElementNS(svgNamespace, 'rect');
    ring.setAttribute('data-cfx-diagram-ring', 'true');
    ring.setAttribute('aria-hidden', 'true');
    ring.setAttribute('x', String(start.x - pad)); ring.setAttribute('y', String(start.y - pad));
    ring.setAttribute('width', String(end.x - start.x + pad * 2)); ring.setAttribute('height', String(end.y - start.y + pad * 2));
    ring.setAttribute('rx', '6');
    svg.append(ring);
    return ring;
  };

  const fillReadout = (target, description, strongTitle) => {
    target.replaceChildren();
    const parts = [description.title, description.eyebrow, ...description.lines].filter(Boolean);
    parts.forEach((part, index) => {
      if (index) target.append(document.createTextNode(' · '));
      if (index === 0 && strongTitle) {
        const title = document.createElement('strong'); title.textContent = part; target.append(title);
      } else target.append(document.createTextNode(part));
    });
  };

  const fillTooltip = (tooltip, description) => {
    tooltip.replaceChildren();
    const line = (className, value) => {
      if (!value) return;
      const element = document.createElement('div'); element.className = className; element.textContent = value; tooltip.append(element);
    };
    line('cfx-diagram-tooltip__eyebrow', description.eyebrow);
    line('cfx-diagram-tooltip__title', description.title);
    description.lines.forEach(value => line('cfx-diagram-tooltip__line', value));
  };

  // Above the target when there is room, otherwise below; always inside the visible part of the (possibly scrolled) stage.
  const placeTooltip = (stage, tooltip, item) => {
    const box = unionRect(item.all), host = stage.getBoundingClientRect();
    if (!box) { tooltip.hidden = true; return; }
    tooltip.hidden = false;
    const width = tooltip.offsetWidth, height = tooltip.offsetHeight, gap = 8, scroll = stage.scrollLeft;
    const centre = (box.left + box.right) / 2 - host.left + scroll;
    const left = Math.max(scroll + 4, Math.min(scroll + stage.clientWidth - width - 4, centre - width / 2));
    let top = box.top - host.top - height - gap;
    if (top < 0) top = box.bottom - host.top + gap;
    tooltip.style.left = left + 'px';
    tooltip.style.top = top + 'px';
  };
