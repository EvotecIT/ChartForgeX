using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

/// <summary>Late-bound status accents with reusable catalogue icons.</summary>
public static class TopologyAccentExamples {
    /// <summary>Writes HTML comparisons with native SVG and PNG at desktop and phone drawing widths.</summary>
    public static void Write(string output) {
        Directory.CreateDirectory(output);
        foreach (var width in new[] { 1440, 390 }) foreach (var dark in new[] { false, true }) foreach (var status in new[] { false, true }) {
            var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
            var theme = VisualTheme.Graphite();
            var drawWidth = Math.Min(1120, width - 48);
            var chart = TopologyChart.Create().WithViewport(drawWidth, 260, 12).WithLegend(null)
                .AddNode("service", "Service", 24, 65, width: drawWidth - 48, height: 105, status: TopologyHealthStatus.Critical)
                .WithNodeIcon("service", "common:certificate").WithTheme(theme).WithThemeMode(mode);
            var options = new TopologyRenderOptions { AccentSource = status ? TopologyAccentSource.Status : TopologyAccentSource.Auto,
                IncludeLegend = false, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card };
            var prepared = chart.Prepare(options);
            var colors = theme.Resolve(mode);
            var stage = status ? "after" : "before";
            var html = "<!doctype html><html><head><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'><style>"
                + "body{margin:0;padding:24px;font:14px Arial;background:" + colors.Background.ToCss() + ";color:" + colors.Foreground.ToCss()
                + "}main{max-width:1120px;margin:auto;min-width:0}svg,img{display:block;max-width:100%}h1{font-size:22px}h2{font-size:14px}</style></head><body><main>"
                + "<h1>Node status and catalogue icons</h1><p>" + (status ? "Status accent follows the current theme." : "Default accent follows the catalogue icon.")
                + "</p><h2>Native SVG</h2>" + prepared.ToSvg() + "<h2>Native PNG</h2><img alt='Native PNG comparison' src='data:image/png;base64,"
                + Convert.ToBase64String(prepared.ToPng()) + "'></main></body></html>";
            File.WriteAllText(Path.Combine(output, $"topology-accent-{stage}-{width}-{(dark ? "dark" : "light")}.html"), html);
        }
    }
}
