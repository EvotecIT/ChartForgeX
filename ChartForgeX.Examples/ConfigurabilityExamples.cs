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
        var hatchPixels = new byte[8 * 8 * 4];
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++) {
                var offset = (y * 8 + x) * 4;
                var hatch = (x + y) % 8 < 2;
                hatchPixels[offset] = hatch ? (byte)86 : (byte)103;
                hatchPixels[offset + 1] = hatch ? (byte)146 : (byte)157;
                hatchPixels[offset + 2] = hatch ? (byte)210 : (byte)216;
                hatchPixels[offset + 3] = 255;
            }
        var tile = new RgbaImage(8, 8, hatchPixels);
        var pattern = ChartFill.Pattern(tile, 8, 8);
        var imagePixels = new byte[64 * 4];
        for (var y = 0; y < 64; y++) {
            var t = y / 63d;
            imagePixels[y * 4] = (byte)Math.Round(141 + (57 - 141) * t);
            imagePixels[y * 4 + 1] = (byte)Math.Round(199 + (121 - 199) * t);
            imagePixels[y * 4 + 2] = (byte)Math.Round(243 + (198 - 243) * t);
            imagePixels[y * 4 + 3] = 255;
        }
        var image = ChartFill.Image(new RgbaImage(1, 64, imagePixels));
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
        var readings = new[] { 62d, 78, 71, 91, 84, 97 };
        var plan = new[] { 67d, 72, 77, 82, 87, 92 };
        var orders = ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders",
            readings.Select((value, index) => new ChartPoint(index + 1, value))).WithFill(linear)
            .WithDataLabelPlacement(ChartDataLabelPlacement.Above)
            .WithDataLabelFormatter(context => context.Value > plan[context.Index]
                ? context.Value?.ToString("0", CultureInfo.InvariantCulture) : string.Empty)
            .WithTooltipFormatter(context => context.Category + ": " + context.Value?.ToString("0", CultureInfo.InvariantCulture) + " orders")
            .WithItemStyler<ChartPoint>(context => context.Index switch {
                2 => new ChartItemStyle(fill: pattern),
                3 => new ChartItemStyle(fill: image),
                5 => new ChartItemStyle(fill: radial, stroke: new ChartStrokeStyle(navy, 1.5)),
                _ => null
            });
        var forecast = ChartSeries.CreateLine(new ChartSeriesId("plan"), "Plan",
            plan.Select((value, index) => new ChartPoint(index + 1, value)))
            .WithColor(ChartColor.FromHex("#CC7850"))
            .WithStrokeStyle(new ChartStrokeStyle(width: 2.5, dash: new[] { 7d, 4d }))
            .WithItemStyler<ChartPoint>(context => context.Index == 5
                ? new ChartItemStyle(markers: new ChartMarkerOptions { Shape = ChartMarkerShape.Diamond, Radius = 5, Enabled = true }) : null);
        forecast.ConfigureMarkers(markers => markers.Enabled = false);
        forecast.ShowDataLabels = false;
        var chart = Chart.Create().WithTitle("Monthly orders")
            .WithSubtitle("Only months above plan are labeled; June led with 97")
            .WithTheme(VisualTheme.Graphite(), mode).WithSize(compact ? 560 : 1040, compact ? 400 : 520)
            .WithXLabels(months).WithYAxis("Orders").WithYAxisBounds(0, 120).WithDataLabels(true)
            .AddSeries(orders).AddSeries(forecast);
        chart.Options.XAxis.IntegerTicks = true;
        chart.Options.YAxis.TickCount = 7;
        return chart;
    }
}
