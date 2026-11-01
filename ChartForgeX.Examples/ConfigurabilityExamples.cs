using System.Globalization;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;

/// <summary>Authorable chart paint and captions, rendered through the same static SVG and native PNG scene.</summary>
internal static class ConfigurabilityExamples {
    internal static void Write(string output, ChartPngOutputScale scale) {
        Directory.CreateDirectory(output);
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            foreach (var compact in new[] { false, true }) {
                var chart = Sales(mode, compact);
                var name = "configurable-sales-" + (compact ? "compact" : "wide") + "-" + mode.ToString().ToLowerInvariant();
                ExampleArtifactWriter.SaveChart(chart, output, name, scale);
            }
    }

    private static Chart Sales(VisualThemeMode mode, bool compact) {
        var blue = ChartColor.FromHex("#3979C6");
        var lightBlue = ChartColor.FromHex("#8DC7F3");
        var navy = ChartColor.FromHex("#214D80");
        var ramp = new[] { new ChartGradientStop(0, lightBlue), new ChartGradientStop(1, blue) };
        var linear = ChartFill.LinearGradient(new ChartPoint(0, 0), new ChartPoint(0, 1), ramp);
        var radial = ChartFill.RadialGradient(new ChartPoint(.3, .2), .9, ramp);
        var tile = new RgbaImage(2, 2, new byte[] {
            57, 121, 198, 255, 141, 199, 243, 255,
            141, 199, 243, 255, 57, 121, 198, 255
        });
        var pattern = ChartFill.Pattern(tile, 8, 8);
        var image = ChartFill.Image(tile);
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
        var readings = new[] { 62d, 78, 71, 91, 84, 97 };
        var orders = ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders",
            readings.Select((value, index) => new ChartPoint(index + 1, value))).WithFill(linear)
            .WithDataLabelFormatter(context => context.Value?.ToString("0", CultureInfo.InvariantCulture))
            .WithTooltipFormatter(context => context.Category + ": " + context.Value?.ToString("0", CultureInfo.InvariantCulture) + " orders")
            .WithItemStyler<ChartPoint>(context => context.Index switch {
                1 => new ChartItemStyle(fill: radial),
                2 => new ChartItemStyle(fill: pattern),
                3 => new ChartItemStyle(fill: image, stroke: new ChartStrokeStyle(navy, 1.5)),
                _ => null
            });
        var forecast = ChartSeries.CreateLine(new ChartSeriesId("plan"), "Plan",
            new[] { 67d, 72, 77, 82, 87, 92 }.Select((value, index) => new ChartPoint(index + 1, value)))
            .WithColor(ChartColor.FromHex("#CC7850"))
            .WithStrokeStyle(new ChartStrokeStyle(width: 2.5, dash: new[] { 7d, 4d }))
            .WithItemStyler<ChartPoint>(context => context.Index == 5
                ? new ChartItemStyle(markers: new ChartMarkerOptions { Shape = ChartMarkerShape.Diamond, Radius = 5, Enabled = true }) : null);
        forecast.ConfigureMarkers(markers => markers.Enabled = false);
        forecast.ShowDataLabels = false;
        var chart = Chart.Create().WithTitle("Monthly orders")
            .WithSubtitle("Orders exceeded the plan in three of six months")
            .WithTheme(VisualTheme.Graphite(), mode).WithSize(compact ? 560 : 1040, compact ? 400 : 520)
            .WithXLabels(months).WithYAxis("Orders").WithYAxisBounds(0, 120).WithDataLabels(true)
            .AddSeries(orders).AddSeries(forecast);
        chart.Options.XAxis.IntegerTicks = true;
        return chart;
    }
}
