using System;
using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Svg;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PolarUsesAngleRadiusCoordinates() {
        var chart = Chart.Create()
            .WithSize(520, 360)
            .WithAxes(false)
            .WithGrid(false)
            .AddPolar("Sweep", new[] {
                new ChartPoint(0, 80),
                new ChartPoint(Math.PI / 2, 80),
                new ChartPoint(Math.PI, 80)
            });

        var svg = chart.ToSvg();
        var points = PreparedFamily(chart).Scene.Nodes.OfType<VisualSceneEllipse>()
            .Where(element => element.Role == "polar-point")
            .ToArray();
        Assert(points.Length == 3, "Polar charts should render one marker per angle/radius point.");
        var x0 = points[0].Cx; var y0 = points[0].Cy;
        var x1 = points[1].Cx; var y1 = points[1].Cy;
        var x2 = points[2].Cx; var y2 = points[2].Cy;
        Assert(x0 > x1 && x1 > x2, "Polar angles should control horizontal position instead of becoming evenly spaced radar categories.");
        Assert(y1 < y0 && Math.Abs(y0 - y2) < 0.01, "Polar angles should use standard counter-clockwise radian coordinates.");
        Assert(svg.Contains("data-cfx-role=\"polar-line\"", StringComparison.Ordinal), "Polar charts should render an ordered polar line.");
        Assert(!svg.Contains("data-cfx-role=\"radar-area\"", StringComparison.Ordinal), "Polar charts must not silently render radar geometry.");

        var movedAngles = Chart.Create()
            .WithSize(520, 360)
            .WithAxes(false)
            .WithGrid(false)
            .AddPolar("Sweep", new[] {
                new ChartPoint(0.2, 80),
                new ChartPoint(0.55, 80),
                new ChartPoint(2.8, 80)
            });
        Assert(!chart.ToPng().SequenceEqual(movedAngles.ToPng()), "PNG polar geometry should respond to angle values when radii stay unchanged.");
    }

    private static void PolarHonorsRadialOptionsAndValidation() {
        var chart = PolarSample();
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"polar-ring\"", StringComparison.Ordinal), "Polar charts should render circular value-grid rings.");
        Assert(svg.Contains("data-cfx-role=\"polar-spoke\"", StringComparison.Ordinal), "Polar charts should render angle-grid spokes.");
        Assert(svg.Contains("data-cfx-role=\"polar-angle-label\"", StringComparison.Ordinal), "Polar charts should render angle-axis labels.");
        Assert(svg.Contains("data-cfx-role=\"polar-radius-label\"", StringComparison.Ordinal), "Polar charts should render radius-axis labels.");
        Assert(chart.ToPng().Length > 64, "Polar charts should render valid PNG output.");

        var compact = PolarSample().WithAxes(false).WithGrid(false);
        var compactSvg = compact.ToSvg();
        Assert(compactSvg.Contains("data-cfx-role=\"polar-point\"", StringComparison.Ordinal), "Polar data should remain visible when axes and grid are hidden.");
        Assert(!compactSvg.Contains("data-cfx-role=\"polar-ring\"", StringComparison.Ordinal) && !compactSvg.Contains("data-cfx-role=\"polar-spoke\"", StringComparison.Ordinal), "Polar grid visibility should be independent from the series.");
        Assert(!compactSvg.Contains("data-cfx-role=\"polar-angle-label\"", StringComparison.Ordinal) && !compactSvg.Contains("data-cfx-role=\"polar-radius-label\"", StringComparison.Ordinal), "Polar axes should hide without suppressing the series.");

        var formatted = PolarSample().ConfigureXAxis(axis => axis.LabelFormatter = angle => "a" + angle.ToString("0.0", CultureInfo.InvariantCulture))
            .ConfigureYAxis(axis => axis.LabelFormatter = radius => "r" + radius.ToString("0", CultureInfo.InvariantCulture));
        var formattedSvg = formatted.ToSvg();
        Assert(formattedSvg.Contains(">a0.0</text>", StringComparison.Ordinal), "Polar angle labels should honor x-axis formatting.");
        Assert(formattedSvg.Contains(">r20</text>", StringComparison.Ordinal), "Polar radius labels should honor y-axis formatting.");

        var centered = PolarSample().WithAxes(false).WithGrid(false).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Center);
        var centeredPrepared = PreparedFamily(centered);
        var centeredPoint = centeredPrepared.Scene.Nodes.OfType<VisualSceneEllipse>().First(element => element.Role == "polar-point");
        var centeredLabel = MapTextBounds(FamilyLabels(centeredPrepared, "polar-data-label").First());
        Assert(IsClose(centeredPoint.Cx, centeredLabel.Left + centeredLabel.Width / 2) && IsClose(centeredPoint.Cy, centeredLabel.Top + centeredLabel.Height / 2), "Center polar labels should stay on their marks.");

        var below = PolarSample().WithAxes(false).WithGrid(false).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Center);
        below.Series[0].WithDataLabelPlacement(ChartDataLabelPlacement.Below);
        var belowPrepared = PreparedFamily(below);
        var belowPoint = belowPrepared.Scene.Nodes.OfType<VisualSceneEllipse>().First(element => element.Role == "polar-point");
        var belowLabel = MapTextBounds(FamilyLabels(belowPrepared, "polar-data-label").First());
        Assert(belowLabel.Top > belowPoint.Cy, "Series-level polar label placement should override the chart-level placement.");
        Assert(!centered.ToPng().SequenceEqual(below.ToPng()), "PNG polar labels should honor the same placement override as SVG.");

        AssertThrows<InvalidOperationException>(() => Chart.Create().AddPolar("Negative", new[] { new ChartPoint(0, -1) }).ToSvg(), "Polar charts should reject negative radii instead of folding them across the origin.");
        var zero = PreparedFamily(Chart.Create().AddPolar("Zero", new[] { new ChartPoint(0, 0) }));
        Assert(FamilyGroups(zero, "polar-point-source").Single().Metadata["data-cfx-value"] == "0", "Zero radius remains a truthful observation at the origin.");
        Assert(zero.ToPng().Length > 64, "Zero-radius data remains exportable.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().ConfigureYAxis(axis => axis.Minimum = -10).AddPolar("Bad axis", new[] { new ChartPoint(0, 0), new ChartPoint(1, 10) }).ToSvg(), "Polar charts should reject negative radial-axis minima so zero remains at the origin.");
    }

    private static Chart PolarSample() => Chart.Create()
        .WithSize(520, 360)
        .AddPolar("Observed", new[] {
            new ChartPoint(0, 84),
            new ChartPoint(0.7, 56),
            new ChartPoint(1.9, 92),
            new ChartPoint(3.4, 68),
            new ChartPoint(5.5, 76)
        });

    private static double PolarCoordinate(SvgElement element, string attribute) =>
        double.Parse(element.GetAttribute(attribute)!, CultureInfo.InvariantCulture);
}
