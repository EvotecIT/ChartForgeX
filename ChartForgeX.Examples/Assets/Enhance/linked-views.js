// Four ChartForgeX charts and one CanopyX grid of the same records follow each other: one call per pair.
document.addEventListener("DOMContentLoaded", () => {
  const grid = document.getElementById("records");                    // the element holding the CanopyX grid
  const chart = id => CfxEnhance.get(document.getElementById(id));    // the LinkedView API of the chart inside

  CfxLink.connect(chart("week"), grid, { field: ["day", "kind"], sourceLabel: "Changes this week" });
  CfxLink.connect(chart("trend"), grid, { field: "day", sourceLabel: "Daily trend" });
  CfxLink.connect(chart("heatmap"), grid, { field: ["week", "product"], sourceLabel: "Products by week" });
  CfxLink.connect(chart("support"), grid, { field: "product", sourceLabel: "Support windows" });

  // Chart to chart: brushing the trend highlights the heatmap cells that hold the same records.
  CfxLink.connect(chart("trend"), chart("heatmap"), { mode: "highlight", sourceLabel: "Daily trend" });
});
