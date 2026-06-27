  const drawNodeMark = (context, node, selected, compact, root) => {
    context.fillStyle = node.backgroundColor || '#2563eb';
    context.strokeStyle = selected ? '#f59e0b' : node.borderColor || '#eff6ff';
    context.lineWidth = selected ? 5 : compact ? 1.5 : 3;
    if (node.shadow) {
      context.shadowColor = 'rgba(15,23,42,.18)';
      context.shadowBlur = 10;
      context.shadowOffsetY = 5;
    }
    if (node.shape === 'box') {
      const width = node.size * 2.9;
      const height = node.size * 2.1;
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
      const image = graphImage(node.imageUrl, () => drawCanvas(root, graphState(root)));
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
      const image = graphImage(node.imageUrl, () => drawCanvas(root, graphState(root)));
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
    if (node.icon) {
      context.font = 'bold 12px Segoe UI, Arial, sans-serif';
      context.textAlign = 'center'; context.textBaseline = 'middle'; context.fillStyle = '#ffffff';
      context.fillText(node.icon, node.x, node.y + 1);
    }
  };
