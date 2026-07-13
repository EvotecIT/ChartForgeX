  const drawCanvasNodeDetails = (context, root, node, compact, moving) => {
    if (!visible(node.el)) return;
    const palette = graphThemePalette(root);
    const dimmed = node.el.classList.contains('cfx-graph-neighborhood-dim');
    const primary = node.el.classList.contains('cfx-graph-neighborhood-primary');
    const selected = node.el.classList.contains('cfx-graph-selected');
    context.save();
    context.globalAlpha = dimmed ? .18 : 1;
    const status = attr(node.el, 'data-cfx-status').toLowerCase();
    if (status && status !== 'unknown') {
      const statusColor = status === 'healthy' ? '#22c55e' : status === 'warning' ? '#f59e0b' : status === 'critical' ? '#ef4444' : '#94a3b8';
      context.beginPath();
      context.arc(node.x - node.size * .8, node.y - node.size * .8, 4.5, 0, Math.PI * 2);
      context.fillStyle = statusColor;
      context.fill();
      context.strokeStyle = palette.halo;
      context.lineWidth = 2;
      context.stroke();
    }
    if (node.badge && !compact && !moving) {
      context.beginPath();
      context.arc(node.x + node.size * .82, node.y - node.size * .82, 8, 0, Math.PI * 2);
      context.fillStyle = palette.text;
      context.fill();
      context.strokeStyle = palette.halo;
      context.lineWidth = 2;
      context.stroke();
      context.font = '800 7px Inter, Segoe UI, sans-serif';
      context.textAlign = 'center';
      context.textBaseline = 'middle';
      context.fillStyle = palette.paper;
      context.fillText(node.badge.slice(0, 5), node.x + node.size * .82, node.y - node.size * .82 + .5);
    }
    if (primary) {
      context.beginPath();
      context.arc(node.x, node.y, node.size + 9, 0, Math.PI * 2);
      context.strokeStyle = '#0f766e';
      context.lineWidth = 3;
      context.stroke();
    }
    if ((!compact && !moving) || node.shape === 'text' || selected || primary) {
      context.font = '12px Inter, Segoe UI, Arial, sans-serif';
      context.textAlign = 'center';
      context.textBaseline = node.shape === 'text' ? 'middle' : 'top';
      context.lineWidth = 4;
      context.strokeStyle = palette.halo;
      context.fillStyle = graphAdaptiveTextColor(root, node.labelColor, palette.text);
      const label = node.label || attr(node.el, 'data-node-label');
      const labelY = node.shape === 'text' ? node.y : node.y + node.size + 8;
      if (node.labelBackgroundColor) {
        const metrics = context.measureText(label);
        const width = Math.max(48, metrics.width + 18);
        const x = node.x - width / 2;
        const y = node.shape === 'text' ? node.y - 9 : node.y + node.size + 4;
        context.save();
        context.fillStyle = node.labelBackgroundColor;
        context.beginPath();
        if (context.roundRect) context.roundRect(x, y, width, 18, 5);
        else context.rect(x, y, width, 18);
        context.fill();
        context.restore();
      }
      context.strokeText(label, node.x, labelY);
      context.fillText(label, node.x, labelY);
      if (node.secondaryLabel && root.classList.contains('cfx-graph-semantic-detail')) {
        context.font = '9.5px Inter, Segoe UI, Arial, sans-serif';
        context.fillStyle = palette.muted;
        context.lineWidth = 3;
        context.strokeText(node.secondaryLabel, node.x, labelY + 15);
        context.fillText(node.secondaryLabel, node.x, labelY + 15);
      }
    }
    context.restore();
  };
  const drawCanvasNodes = (context, root, nodes, compact, moving) => {
    nodes.forEach(node => {
      if (!visible(node.el)) return;
      context.save();
      context.globalAlpha = node.el.classList.contains('cfx-graph-neighborhood-dim') ? .18 : 1;
      drawNodeMark(context, node, node.el.classList.contains('cfx-graph-selected'), compact, root, moving);
      context.restore();
    });
    nodes.forEach(node => drawCanvasNodeDetails(context, root, node, compact, moving));
  };
