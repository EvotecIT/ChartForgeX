using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;
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

        var svg = XDocument.Parse(chart.ToSvg());
        var rendered = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "x-axis-label").ToArray();
        Assert.Equal(new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, rendered.Select(label => label.Value).ToArray());
        var markers = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "line-marker").ToArray();
        foreach (var label in rendered.Where(label => (string?)label.Attribute("text-anchor") == "middle")) {
            var index = Array.IndexOf(rendered, label);
            var marker = markers.First(candidate => (string?)candidate.Attribute("data-cfx-point") == index.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(Number(marker, "cx"), Number(label, "x"), 1);
        }
    }

    private static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
}
