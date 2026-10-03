  const graphNodeMarkPaint = (node, selected, compact) => ({
    fill: node.backgroundColor || '#2563eb', stroke: selected ? '#f59e0b' : node.borderColor || '#eff6ff',
    width: selected ? 5 : compact ? 1.5 : 3
  });
  let graphNodeMeasureContext = null;
  const graphNodeMarkBounds = (node, compact, moving, margin) => {
    const extents = nodeShapeExtents(node), paint = graphNodeMarkPaint(node, node.el.classList.contains('cfx-graph-selected'), compact);
    // Canvas's default miter limit is 10; angular strokes can extend beyond
    // half the width at star/triangle tips and rectangle corners.
    const angular = ['box', 'imageRect', 'square', 'diamond', 'triangle', 'triangleDown', 'star', 'database'].includes(node.shape);
    const padding = paint.width * (angular ? 5 : .5) + margin + (node.shape === 'image' ? 3 : 0) + (node.shadow && !moving ? 32 : 0);
    const bounds = { node, minX: node.x - extents.x - padding, maxX: node.x + extents.x + padding, minY: node.y - extents.y - padding, maxY: node.y + extents.y + padding };
    const include = (x, y, halfWidth, halfHeight) => {
      bounds.minX = Math.min(bounds.minX, x - halfWidth - margin); bounds.maxX = Math.max(bounds.maxX, x + halfWidth + margin);
      bounds.minY = Math.min(bounds.minY, y - halfHeight - margin); bounds.maxY = Math.max(bounds.maxY, y + halfHeight + margin);
    };
    if (node.icon && !moving) {
      let width = Array.from(node.icon).length * 12;
      if (typeof document?.createElement === 'function') {
        graphNodeMeasureContext ||= document.createElement('canvas').getContext('2d');
        if (graphNodeMeasureContext) { graphNodeMeasureContext.font = 'bold 12px Segoe UI, Arial, sans-serif'; width = graphNodeMeasureContext.measureText(node.icon).width; }
      }
      include(node.card ? node.x - extents.x + 28 : node.x, node.y + 1, width / 2, 12);
    }
    const status = attr(node.el, 'data-cfx-status').toLowerCase();
    if (status && status !== 'unknown') include(node.card ? node.x + extents.x - 15 : node.x - node.size * .8, node.card ? node.y + extents.y - 14 : node.y - node.size * .8, 5.5, 5.5);
    return bounds;
  };
  const drawNodeMark = (context, node, selected, compact, root, moving) => {
    const paint = graphNodeMarkPaint(node, selected, compact);
    context.fillStyle = paint.fill;
    context.strokeStyle = paint.stroke;
    context.lineWidth = paint.width;
    if (node.shadow && !moving) {
      context.shadowColor = 'rgba(15,23,42,.18)';
      context.shadowBlur = 10;
      context.shadowOffsetY = 5;
    }
    if (node.shape === 'box') {
      const width = node.size * 2.9;
      const height = node.card ? Math.min(node.size * 2.1, 72) : node.size * 2.1;
      context.beginPath();
      if (context.roundRect) context.roundRect(node.x - width / 2, node.y - height / 2, width, height, Math.min(8, node.size * .45));
      else context.rect(node.x - width / 2, node.y - height / 2, width, height);
      context.fill();
      context.stroke();
    } else if (node.shape === 'image' && node.imageUrl) {
      context.beginPath();
      context.arc(node.x, node.y, node.size + 3, 0, Math.PI * 2);
      context.fill();
      context.stroke();
      const image = graphImage(node.imageUrl, graphImageRedraw(root));
      if (image && image.complete && image.naturalWidth > 0) {
        try {
          context.save();
          context.beginPath();
          context.arc(node.x, node.y, node.size, 0, Math.PI * 2);
          context.clip();
          context.drawImage(image, node.x - node.size, node.y - node.size, node.size * 2, node.size * 2);
          context.restore();
        } catch {
          context.restore();
          // Keep malformed host-supplied images from breaking Canvas interaction.
        }
      }
    } else if (node.shape === 'imageRect' && node.imageUrl) {
      const width = node.size * 2.6;
      const height = node.size * 1.8;
      context.beginPath();
      if (context.roundRect) context.roundRect(node.x - width / 2, node.y - height / 2, width, height, Math.min(8, node.size * .35));
      else context.rect(node.x - width / 2, node.y - height / 2, width, height);
      context.fill();
      context.stroke();
      const image = graphImage(node.imageUrl, graphImageRedraw(root));
      if (image && image.complete && image.naturalWidth > 0) {
        try {
          context.drawImage(image, node.x - width / 2 + 3, node.y - height / 2 + 3, Math.max(1, width - 6), Math.max(1, height - 6));
        } catch {
          // Keep malformed host-supplied images from breaking Canvas interaction.
        }
      }
    } else drawNodeShapeMark(context, node);
    context.shadowColor = 'transparent';
    context.shadowBlur = 0;
    context.shadowOffsetY = 0;
    if (node.icon && !moving) {
      context.font = 'bold 12px Segoe UI, Arial, sans-serif';
      context.textAlign = 'center'; context.textBaseline = 'middle'; context.fillStyle = '#ffffff';
      context.fillText(node.icon, node.card ? node.x - node.size * 1.45 + 28 : node.x, node.y + 1);
    }
  };
