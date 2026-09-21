  const drawCanvasNodeDetails = (context, root, node, compact, moving) => {
    if (!visible(node.el)) return;
    const palette = graphThemePalette(root);
    const dimmed = node.el.classList.contains('cfx-graph-neighborhood-dim');
    const primary = node.el.classList.contains('cfx-graph-neighborhood-primary');
    const related = node.el.classList.contains('cfx-graph-neighborhood-related');
    const selected = node.el.classList.contains('cfx-graph-selected');
    const card = node.card === true;
    const cardHalfWidth = node.size * 1.45;
    const cardHalfHeight = Math.min(node.size * 1.05, 36);
    const nodeColors = graphReadableNodeColors(root, node.el, palette);
    context.save();
    context.globalAlpha = dimmed ? .18 : 1;
    const status = attr(node.el, 'data-cfx-status').toLowerCase();
    if (status && status !== 'unknown') {
      const statusColor = status === 'healthy' ? '#22c55e' : status === 'warning' ? '#f59e0b' : status === 'critical' ? '#ef4444' : '#94a3b8';
      context.beginPath();
      context.arc(card ? node.x + cardHalfWidth - 15 : node.x - node.size * .8, card ? node.y + cardHalfHeight - 14 : node.y - node.size * .8, 4.5, 0, Math.PI * 2);
      context.fillStyle = statusColor;
      context.fill();
      context.strokeStyle = nodeColors.halo;
      context.lineWidth = 2;
      context.stroke();
    }
    if (node.badge && !compact && !moving) {
      context.beginPath();
      context.arc(card ? node.x + cardHalfWidth - 18 : node.x + node.size * .82, card ? node.y - cardHalfHeight + 18 : node.y - node.size * .82, 8, 0, Math.PI * 2);
      context.fillStyle = palette.text;
      context.fill();
      context.strokeStyle = palette.halo;
      context.lineWidth = 2;
      context.stroke();
      context.font = '800 7px Inter, Segoe UI, sans-serif';
      context.textAlign = 'center';
      context.textBaseline = 'middle';
      context.fillStyle = palette.paper;
      context.fillText(node.badge.slice(0, 5), card ? node.x + cardHalfWidth - 18 : node.x + node.size * .82, card ? node.y - cardHalfHeight + 18.5 : node.y - node.size * .82 + .5);
    }
    if (primary) {
      context.beginPath();
      context.arc(node.x, node.y, node.size + 9, 0, Math.PI * 2);
      context.strokeStyle = '#0f766e';
      context.lineWidth = 3;
      context.stroke();
    }
    if ((!compact && !moving) || node.shape === 'text' || selected || primary || related) {
      context.font = card ? '700 12.5px Inter, Segoe UI, Arial, sans-serif' : '12px Inter, Segoe UI, Arial, sans-serif';
      context.textAlign = card ? 'left' : 'center';
      context.textBaseline = node.shape === 'text' ? 'middle' : card ? 'alphabetic' : 'top';
      context.lineWidth = 4;
      context.strokeStyle = nodeColors.halo;
      context.fillStyle = nodeColors.label;
      const fullLabel = node.label || attr(node.el, 'data-node-label');
      const label = card ? graphCardText(fullLabel, node.size) : fullLabel;
      const labelX = card ? node.x - cardHalfWidth + 52 : node.x;
      const labelY = node.shape === 'text' ? node.y : card ? node.y - 5 : node.y + node.size + 8;
      if (node.labelBackgroundColor) {
        if (!card) {
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
      }
      context.strokeText(label, labelX, labelY);
      context.fillText(label, labelX, labelY);
      if (node.secondaryLabel && root.classList.contains('cfx-graph-semantic-detail')) {
        context.font = card ? '10px Inter, Segoe UI, Arial, sans-serif' : '9.5px Inter, Segoe UI, Arial, sans-serif';
        context.fillStyle = nodeColors.secondary;
        context.lineWidth = 3;
        const secondaryLabel = card ? graphCardText(node.secondaryLabel, node.size, true) : node.secondaryLabel;
        context.strokeText(secondaryLabel, labelX, card ? labelY + 19 : labelY + 15);
        context.fillText(secondaryLabel, labelX, card ? labelY + 19 : labelY + 15);
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
