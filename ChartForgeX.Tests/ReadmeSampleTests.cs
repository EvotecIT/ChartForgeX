using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// The README Quick Start pairs <c>WithXLabels</c>, which places labels at x = 1 through N, with
/// points numbered the same way, so every label sits under its point.
/// </summary>
public sealed class ReadmeSampleTests {
    [Fact]
    public void QuickStartLabelsLineUpWithTheirPoints() {
        // The NuGet README Quick Start, as published.
        var chart = Chart.Create()
            .WithTitle("Domain Security Checks")
            .WithSubtitle("Dependency-free SVG, HTML, and PNG chart rendering")
            .WithXAxis("Run")
            .WithYAxis("Checks")
            .WithTheme(ChartTheme.ReportDark())
            .WithSize(1180, 640)
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
            .AddSmoothArea("Passed", ChartPoints.FromValues(820, 940, 980, 1040, 1120, 1180, 1230))
            .AddSmoothLine("Warnings", ChartPoints.FromValues(120, 138, 132, 110, 98, 86, 72), ChartColor.FromRgb(251, 191, 36));

        var labels = chart.Options.XAxisLabels;
        foreach (var series in chart.Series) {
            Assert.Equal(labels.Count, series.Points.Count);
            for (var i = 0; i < labels.Count; i++) Assert.Equal(labels[i].Value, series.Points[i].X);
        }

        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var rendered = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "axis-x-label").ToArray();
        Assert.Equal(new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, rendered.Select(label => Assert.Single(label.Text.Lines).Text));
        var ticks = prepared.Regions.Where(region => region.Role == "axis-x-label").ToArray();
        Assert.Equal(7, ticks.Length);
        for (var index = 0; index < ticks.Length; index++) {
            var point = Assert.Single(prepared.Regions, region => region.Id == "series-1-point-" + index);
            Assert.Equal(point.Bounds.Left + point.Bounds.Width / 2, ticks[index].Bounds.Left, 6);
        }
        Assert.Equal(prepared.ToSvg(), chart.ToSvg());
    }

}
