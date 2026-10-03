  const drawCanvasClusters = (context, root, state, byId, palette, moving, metricsByCluster) => {
    state.clusters.forEach(cluster => {
      if (!visible(cluster.el)) return;
      const metrics = metricsByCluster ? metricsByCluster.get(cluster) : clusterMetrics(cluster, byId);
      if (!metrics) return;
      const label = attr(cluster.el, 'data-cluster-label') || cluster.id;
      const selected = cluster.el.classList.contains('cfx-graph-selected');
      const clusterColors = graphClusterColors(root, cluster, palette);
      context.beginPath();
      context.arc(metrics.x, metrics.y, metrics.radius, 0, Math.PI * 2);
      context.globalAlpha = metrics.expanded ? .1 : .86;
      context.fillStyle = metrics.expanded ? 'rgba(224,242,254,0)' : clusterColors.fill;
      context.strokeStyle = selected ? palette.selected : clusterColors.stroke;
      context.lineWidth = selected ? 4 : metrics.expanded ? 1.2 : 2;
      context.setLineDash([6, 4]);
      if (!metrics.expanded) context.fill();
      context.stroke();
      context.setLineDash([]);
      if ((!moving && !metrics.expanded) || selected) {
        const memberCount = cluster.nodeIds.length;
        const memberLabel = `${memberCount} ${memberCount === 1 ? 'object' : 'objects'}`;
        context.globalAlpha = metrics.expanded ? .55 : 1;
        context.font = '700 12px Segoe UI, Arial, sans-serif';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        context.lineWidth = 4;
        context.strokeStyle = palette.halo;
        context.fillStyle = palette.clusterText;
        context.strokeText(label, metrics.x, metrics.y - 6);
        context.fillText(label, metrics.x, metrics.y - 6);
        context.font = '600 9.5px Segoe UI, Arial, sans-serif';
        context.fillStyle = palette.muted;
        context.strokeText(memberLabel, metrics.x, metrics.y + 10);
        context.fillText(memberLabel, metrics.x, metrics.y + 10);
      }
      context.globalAlpha = 1;
    });
  };
