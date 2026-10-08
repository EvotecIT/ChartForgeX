using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.Themes;

/// <summary>Composes desktop-sized examples from generic canvas layers and native topology output.</summary>
public static class WallpaperOwnerExamples {
    /// <summary>Creates one example layout; the layout names describe consumer policy, not engine presets.</summary>
    public static VisualCanvas Create(string layout, int width, int height) {
        var tokens = VisualDesignTokens.GraphiteDark();
        var canvas = VisualCanvas.Create(width, height).WithTitle("System overview").WithDesignTokens(tokens)
            .WithBackdrop(VisualCanvasBackdropStyle.Plain).WithBackground(tokens.Background);
        var accent = tokens.Accent;
        if (layout == "CenterRight") {
            canvas.AddInfoTile(VisualCanvasPlacement.At(VisualCanvasAnchor.MiddleRight, 34, -140), 560, 132,
                "PC", "HOST", "DEV-WKS-01", detail: "Windows workstation", accent: accent);
            canvas.AddInfoTile(VisualCanvasPlacement.At(VisualCanvasAnchor.MiddleRight, 34, 12), 560, 132,
                "CPU", "PROCESSOR", "42%", accent: accent, progress: .42,
                miniChartKind: VisualCanvasInfoTileMiniChartKind.Sparkline, miniChartValues: new[] { 18d, 28d, 24d, 42d }, miniChartMaximum: 100);
        } else if (layout == "ContrastBox") {
            var panel = ImageComposition.Create(620, 320, ChartColor.Transparent)
                .FillRoundedRectangle(0, 0, 620, 320, 18, ChartColor.FromRgba(0, 0, 0, 220)).ToImage();
            var bounds = canvas.ResolvePlacement(VisualCanvasPlacement.At(VisualCanvasAnchor.MiddleRight, 34, -60), 620, 320);
            canvas.AddRasterImage(bounds.X, bounds.Y, 620, 320, panel);
            canvas.AddInfoTile(bounds.X + 24, bounds.Y + 24, 572, 120, "IP", "NETWORK", "10.0.0.42", "192.168.1.42", accent,
                surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Outline);
            canvas.AddInfoTile(bounds.X + 24, bounds.Y + 176, 572, 120, "RAM", "MEMORY", "32 GB", accent: accent,
                miniChartKind: VisualCanvasInfoTileMiniChartKind.Area, miniChartValues: new[] { 18d, 24d, 22d, 28d }, miniChartMaximum: 100);
        } else if (layout == "RaisedSections") {
            canvas.AddInfoTile(VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 34, 34), 620, 140,
                "PC", "SYSTEM", "DEV-WKS-01", "Windows workstation", accent, surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised);
            canvas.AddInfoTile(VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 34, 198), 620, 140,
                "CPU", "PROCESSOR", "42%", accent: accent, progress: .42, surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised,
                miniChartKind: VisualCanvasInfoTileMiniChartKind.Bars, miniChartValues: new[] { 18d, 28d, 24d, 42d }, miniChartMaximum: 100);
            canvas.AddInfoTile(VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 34, 362), 620, 140,
                "RAM", "MEMORY", "32 GB", accent: accent, surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised);
        } else throw new ArgumentException("Unknown example layout.", nameof(layout));
        return canvas.AddTopology(VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 34, 34), 560, 310,
            CreateTopology(), new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false, FitContentToViewport = true, PngSupersamplingScale = 1 }, VisualCanvasImageFit.Contain);
    }

    /// <summary>Creates a transparent fixed-viewport topology suitable for a wallpaper overlay.</summary>
    public static TopologyChart CreateTopology() => TopologyChart.Create().WithId("wallpaper-topology").WithViewport(560, 310, 20)
        .WithLegend(null).WithTheme(OverlayTheme())
        .AddNode("workstation", "Workstation", 34, 110, width: 140, height: 70)
        .AddNode("gateway", "Gateway", 330, 110, TopologyNodeKind.Gateway, width: 140, height: 70)
        .AddEdge("network", "workstation", "gateway", "Network", routing: TopologyEdgeRouting.Straight);

    private static TopologyTheme OverlayTheme() {
        var theme = VisualDesignTokens.GraphiteDark().ApplyTo(new TopologyTheme());
        theme.Background = "#00000000";
        return theme;
    }

    internal static void Write(string output) {
        foreach (var size in new[] { (Width: 2560, Height: 1080), (Width: 3840, Height: 2160) }) {
            foreach (var layout in new[] { "CenterRight", "ContrastBox", "RaisedSections" }) {
                var canvas = Create(layout, size.Width, size.Height);
                var name = "wallpaper-" + layout.ToLowerInvariant() + "-" + size.Width + "x" + size.Height;
                canvas.SaveSvg(Path.Combine(output, name + ".svg"));
                canvas.SavePng(Path.Combine(output, name + ".png"));
                canvas.SaveHtml(Path.Combine(output, name + ".html"));
            }
        }
    }
}
