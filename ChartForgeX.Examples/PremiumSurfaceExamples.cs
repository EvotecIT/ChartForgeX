using System.Text.Json;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

internal static class PremiumSurfaceExamples {
    private const string ManifestFileName = "premium-surface-manifest.json";

    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        SaveDesktopWallpaper(output, (int)pngOutputScale);
        SaveCompactEmailSummary(output, (int)pngOutputScale);
        WriteManifest(output);
    }

    private static void SaveDesktopWallpaper(string output, int outputScale) {
        var blue = ChartColor.FromHex("#38BDF8");
        var canvas = VisualCanvas.CreateDesktopWallpaper()
            .WithTitle("Premium endpoint desktop wallpaper")
            .WithBackground(ChartColor.FromHex("#020617"), ChartColor.FromHex("#0B2447"))
            .WithBackdrop(VisualCanvasBackdropStyle.TechHorizon)
            .WithPngOutputScale(outputScale)
            .AddText(70, 58, 1780, "ENDPOINT OVERVIEW", 18, ChartColor.FromHex("#7DD3FC"), emphasized: true)
            .AddHeroTitle(520, 354, 880, 78, new[] {
                new VisualCanvasTextRun("DEV-WKS-", ChartColor.FromHex("#F8FAFC")),
                new VisualCanvasTextRun("01", blue)
            })
            .AddText(520, 456, 880, "Secure, connected, and ready for work", 27, ChartColor.FromHex("#C6D3EA"), TextAlignment.Center)
            .AddInfoTile(70, 146, 390, 118, "OS", "OPERATING SYSTEM", "Windows 11 Enterprise", "24H2 / build 26100", blue)
            .AddInfoTile(70, 286, 390, 118, "IP", "PRIMARY NETWORK", "10.0.0.42", "Corporate LAN", ChartColor.FromHex("#2DD4BF"))
            .AddInfoTile(70, 426, 390, 118, "UP", "UPTIME", "8 days 14 hours", "Last restart: planned", ChartColor.FromHex("#A78BFA"), 0.71)
            .AddInfoTile(1460, 146, 390, 118, "CPU", "PROCESSOR", "Intel Core i7", "Average load 23%", blue, 0.23, miniChartKind: VisualCanvasInfoTileMiniChartKind.Sparkline, miniChartValues: new[] { 18d, 25d, 21d, 30d, 23d })
            .AddInfoTile(1460, 286, 390, 118, "RAM", "MEMORY", "22.7 / 32 GB", "71% in use", ChartColor.FromHex("#FBBF24"), 0.71, miniChartKind: VisualCanvasInfoTileMiniChartKind.Bars, miniChartValues: new[] { 54d, 61d, 65d, 68d, 71d }, miniChartMaximum: 100)
            .AddInfoTile(1460, 426, 390, 118, "SEC", "SECURITY", "Protected", "No action required", ChartColor.FromHex("#34D399"), 1)
            .AddKeyValueBlock(650, 608, 620, new[] {
                new VisualCanvasKeyValueItem("User", "CONTOSO\\jdoe"),
                new VisualCanvasKeyValueItem("Device", "Managed / compliant"),
                new VisualCanvasKeyValueItem("Support", "support.example.net / +1 555 0100")
            }, labelFontSize: 17, valueFontSize: 17, labelWidth: 110, valueWrapWidth: 450)
            .AddFeatureStrip(540, 906, 840, 70, new[] {
                new VisualCanvasFeatureItem("OK", "COMPLIANT"),
                new VisualCanvasFeatureItem("VPN", "CONNECTED"),
                new VisualCanvasFeatureItem("EDR", "HEALTHY"),
                new VisualCanvasFeatureItem("IT", "SUPPORTED")
            });

        Save(canvas, output, "premium-desktop-wallpaper-canvas");
    }

    private static void SaveCompactEmailSummary(string output, int outputScale) {
        var theme = ChartTheme.ReportLight()
            .WithSurfaceColors(ChartColor.FromHex("#F8FAFC"), ChartColor.FromHex("#FFFFFF"), ChartColor.FromHex("#FFFFFF"), ChartColor.FromHex("#EFF4FB"), ChartColor.FromHex("#D8E1EF"))
            .WithTextColors(ChartColor.FromHex("#172033"), ChartColor.FromHex("#647086"))
            .WithPalette("#2563EB", "#14B8A6", "#F59E0B", "#EF4444")
            .WithCornerRadius(12, 8);

        MetricCard Card(string label, string value, string caption, string symbol, VisualStatus status, ChartColor color, params double[] history) => MetricCard.Create()
            .WithSize(260, 142)
            .WithTheme(theme)
            .WithMetric(label, value)
            .WithCaption(caption)
            .WithSymbol(symbol)
            .WithStatus(status)
            .WithMiniSparkline(history, color: color, fillColor: color.WithAlpha(34));

        var cards = new[] {
            Card("Coverage", "98.4%", "assets reporting", "COV", VisualStatus.Positive, ChartColor.FromHex("#14B8A6"), 92, 94, 95, 97, 98.4),
            Card("Critical", "3", "requires action", "CRT", VisualStatus.Negative, ChartColor.FromHex("#EF4444"), 8, 7, 5, 4, 3),
            Card("Warnings", "18", "open findings", "WRN", VisualStatus.Warning, ChartColor.FromHex("#F59E0B"), 28, 26, 23, 20, 18),
            Card("Resolved", "146", "last 30 days", "OK", VisualStatus.Info, ChartColor.FromHex("#2563EB"), 82, 96, 111, 129, 146)
        };

        var grid = VisualGrid.CreateMetricStrip("Monthly Security Summary", cards, columns: 2, panelWidth: 260, panelHeight: 142)
            .WithSubtitle("Compact 584 px visual for email and narrow report columns.")
            .WithTheme(theme)
            .WithPngOutputScale(outputScale);

        grid.SaveSvg(Path.Combine(output, "premium-email-summary-grid.svg"));
        grid.SaveHtml(Path.Combine(output, "premium-email-summary-grid.html"));
        grid.SavePng(Path.Combine(output, "premium-email-summary-grid.png"));
    }

    private static void WriteManifest(string output) {
        var manifest = new {
            version = 1,
            surfaces = new[] {
                new { useCase = "desktop-wallpaper", name = "premium-desktop-wallpaper-canvas", width = 1920, height = 1080, minimumPngScale = 2, requiresTransparency = false },
                new { useCase = "social-preview", name = "powerbginfo-social-preview-canvas", width = 1200, height = 630, minimumPngScale = 2, requiresTransparency = false },
                new { useCase = "compact-email", name = "premium-email-summary-grid", width = 584, height = 424, minimumPngScale = 2, requiresTransparency = false },
                new { useCase = "report-strip", name = "report-summary-metric-strip", width = 980, height = 294, minimumPngScale = 2, requiresTransparency = false },
                new { useCase = "transparent-overlay", name = "transparent-report-summary-metric-strip", width = 980, height = 294, minimumPngScale = 2, requiresTransparency = true }
            }
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(output, ManifestFileName), JsonSerializer.Serialize(manifest, options));
    }

    private static void Save(VisualCanvas canvas, string output, string name) {
        canvas.SaveSvg(Path.Combine(output, name + ".svg"));
        canvas.SaveHtml(Path.Combine(output, name + ".html"));
        canvas.SavePng(Path.Combine(output, name + ".png"));
    }
}
