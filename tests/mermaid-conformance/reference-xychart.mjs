// Test-only projection of the pinned XY database. It does not implement rendering or accept source grammar.
export function xyChartSourceFacts(db) {
  const data = db.getXYChartData();
  return {orientation: db.getChartConfig().chartOrientation, title: data.title,
    xAxis: {type: data.xAxis.type, title: data.xAxis.title, categories: data.xAxis.categories ?? [], min: data.xAxis.min ?? null, max: data.xAxis.max ?? null},
    yAxis: {type: data.yAxis.type, title: data.yAxis.title, min: data.yAxis.min, max: data.yAxis.max},
    series: data.plots.map(plot => ({kind: plot.type, name: plot.title,
      data: plot.data.map(point => [String(point[0]), point[1]]), labels: plot.pointLabels ?? []}))};
}
