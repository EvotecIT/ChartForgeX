using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

internal static class WellnessDashboardExamples {
    private static readonly ChartColor Background = ChartColor.FromHex("#F5F6F8");
    private static readonly ChartColor Card = ChartColor.FromHex("#FFFFFF");
    private static readonly ChartColor Text = ChartColor.FromHex("#252936");
    private static readonly ChartColor Muted = ChartColor.FromHex("#8B8E96");
    private static readonly ChartColor Track = ChartColor.FromHex("#F1F2F6");
    private static readonly ChartColor Orange = ChartColor.FromHex("#FF9F4A");
    private static readonly ChartColor Yellow = ChartColor.FromHex("#FFCD62");
    private static readonly ChartColor Green = ChartColor.FromHex("#BDE765");

    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        SaveLayeredRadial(output, pngOutputScale);
        SaveWeightCard(output, (int)pngOutputScale);
        SaveCaloriesCard(output, (int)pngOutputScale);
    }

    private static void SaveLayeredRadial(string output, ChartPngOutputScale pngOutputScale) {
        var chart = Chart.Create()
            .WithTitle("Layered Radial Progress")
            .WithSubtitle("Generic radial layers with independent radius, stroke, color, and value scale.")
            .WithSize(560, 560)
            .WithPadding(48, 48, 48, 48)
            .WithTheme(WellnessTheme())
            .WithPlotBackground(false)
            .WithLegend(false)
            .WithPngOutputScale(pngOutputScale)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " kcal")
            .AddLayeredRadial("Calories left", layers => layers
                .Add("Available area", 100, color: Track, configure: layer => layer
                    .WithGeometry(1.00, 0.18)
                    .WithLineCap(ChartRadialLayerCap.Butt))
                .Add("Target ring", 100, color: Yellow, configure: layer => layer
                    .WithGeometry(0.93, 0.035)
                    .WithLineCap(ChartRadialLayerCap.Butt))
                .Add("Current", 1240, maximum: 2700, color: Orange, configure: layer => layer
                    .WithGeometry(0.93, 0.14)
                    .WithAngles(-90, 360)));

        chart.SaveSvg(Path.Combine(output, "wellness-layered-radial-progress.svg"));
        chart.SaveHtml(Path.Combine(output, "wellness-layered-radial-progress.html"));
        chart.SavePng(Path.Combine(output, "wellness-layered-radial-progress.png"));
    }

    private static void SaveWeightCard(string output, int outputScale) {
        var card = RadialMetricCard.Create()
            .WithTitle("Weight Data")
            .WithSubtitle("Goal progress as reusable radial metric layers.")
            .WithSize(560, 420)
            .WithTheme(WellnessTheme())
            .WithPngOutputScale(outputScale)
            .WithMetric("Current Weight", "78 kg")
            .AddLayer("Current weight progress", 100, color: Orange, configure: layer => layer
                .WithGeometry(1.00, 0.265)
                .WithAngles(184, 74))
            .AddLayer("Remaining weight range", 100, color: Yellow, configure: layer => layer
                .WithGeometry(1.00, 0.265)
                .WithAngles(279, 77)
                .WithSeparators(8, Card, 3, 0));

        card.SaveSvg(Path.Combine(output, "wellness-weight-data-gauge.svg"));
        card.SaveHtml(Path.Combine(output, "wellness-weight-data-gauge.html"));
        card.SavePng(Path.Combine(output, "wellness-weight-data-gauge.png"));
    }

    private static void SaveCaloriesCard(string output, int outputScale) {
        var radial = RadialMetricCard.Create()
            .WithTitle("Calories left")
            .WithSize(360, 438)
            .WithTheme(WellnessTheme())
            .WithIcon(VisualIcon.Lightning)
            .WithMetric("Calories left", "1240 kcal")
            .AddLayer("Available area", 100, color: Track, configure: layer => layer
                .WithGeometry(1.00, 0.18)
                .WithLineCap(ChartRadialLayerCap.Butt))
            .AddLayer("Target ring", 100, color: Yellow, configure: layer => layer
                .WithGeometry(0.93, 0.035)
                .WithLineCap(ChartRadialLayerCap.Butt))
            .AddLayer("Current", 1240, maximum: 2700, color: Orange, configure: layer => layer
                .WithGeometry(0.93, 0.14)
                .WithAngles(-90, 360));

        var eaten = MetricCard.Create()
            .WithSize(360, 210)
            .WithTheme(WellnessTheme())
            .WithIcon(VisualIcon.ForkKnife)
            .WithMetric("Eaten calories", "1750 kcal")
            .WithCaption("Logged intake");

        var burned = MetricCard.Create()
            .WithSize(360, 210)
            .WithTheme(WellnessTheme())
            .WithIcon(VisualIcon.Flame)
            .WithMetric("Burned calories", "510 kcal")
            .WithCaption("Activity estimate");

        var macros = Chart.Create()
            .WithTitle("Macro Balance")
            .WithSize(738, 210)
            .WithPadding(26, 48, 26, 24)
            .WithTheme(WellnessTheme())
            .WithLegend(false)
            .WithProgressValues(true)
            .WithProgressHandles(false)
            .WithProgressBarThickness(0.46)
            .AddProgressBars("Macros", new[] {
                new ChartProgressItem("Carbohydrates", 37, Green),
                new ChartProgressItem("Proteins", 93, Orange),
                new ChartProgressItem("Fats", 45, Yellow)
            });

        var grid = VisualGrid.Create()
            .WithTitle("Calories Intake")
            .WithSubtitle("Built from public visual blocks, icons, radial layers, and progress bars.")
            .WithColumns(3)
            .WithPanelSize(360, 210)
            .WithGap(18)
            .WithPadding(24)
            .WithTheme(WellnessTheme())
            .WithPngOutputScale(outputScale)
            .Add(radial, rowSpan: 2)
            .Add(eaten)
            .Add(burned)
            .Add(macros, columnSpan: 2);

        grid.SaveSvg(Path.Combine(output, "wellness-calories-intake-dashboard.svg"));
        grid.SaveHtml(Path.Combine(output, "wellness-calories-intake-dashboard.html"));
        grid.SavePng(Path.Combine(output, "wellness-calories-intake-dashboard.png"));
    }

    private static ChartTheme WellnessTheme() => ChartTheme.Minimal()
        .WithSurfaceColors(Background, Card, Card, Track, Track)
        .WithTextColors(Text, Muted)
        .WithGuideColors(Track, Track)
        .WithPalette(Orange.ToHex(), Yellow.ToHex(), Green.ToHex(), "#6A7FDB")
        .WithTypography(28, 14, 12, 18, 20, 20)
        .WithCornerRadius(30, 14)
        .WithShadowOpacity(0.03);
}
