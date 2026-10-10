using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void WaterfallHonorsAxesVisibility() {
        var compact = WaterfallSample().WithAxes(false);
        var compactScene = compact.Prepare(VisualExportRequest.ForChart(compact).Context).Scene;
        Assert(compactScene.Nodes.Count(node => node.Role == "waterfall-bar") == 5, "Disabling axes must preserve every waterfall step and its derived total.");
        Assert(!compactScene.Nodes.Any(node => node.Role?.StartsWith("axis-", System.StringComparison.Ordinal) == true), "Disabling axes must suppress both value and category axes.");
        Assert(compact.ToPng().Length > 64, "Compact waterfall charts must render native PNG output.");

        var full = WaterfallSample();
        var fullScene = full.Prepare(VisualExportRequest.ForChart(full).Context).Scene;
        Assert(fullScene.Nodes.Any(node => node.Role == "axis-x-label"), "Waterfall categories should render by default.");
        Assert(fullScene.Nodes.Any(node => node.Role == "axis-y-label"), "Waterfall value ticks should render by default.");
        Assert(fullScene.Regions.Any(region => region.Role == "axis-x-label" && region.Label?.StartsWith("Total", System.StringComparison.Ordinal) == true), "The derived total must keep its category label.");

        foreach (var hideX in new[] { false, true }) {
            var chart = WaterfallSample().WithLegend(false).ConfigureTickLabelStyle(style => style.WithColor("#00FFFF"));
            chart.Options.XAxis.Visible = !hideX;
            chart.Options.YAxis.Visible = hideX;
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            Assert(!prepared.Scene.Nodes.Any(node => node.Role == (hideX ? "axis-x-label" : "axis-y-label")), "Each waterfall axis must hide independently.");
            Assert(prepared.Scene.Nodes.Any(node => node.Role == (hideX ? "axis-y-label" : "axis-x-label")), "Hiding one axis must keep the other axis labels.");
            var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
            Assert(CountNearColorInRect(pixels, width, 0, 0, width, height, 0, 255, 255, 80) > 0, "The remaining tick labels must also appear in native PNG output.");
        }

        var independentTicks = WaterfallSample();
        independentTicks.Options.XAxis.TickCount = 2;
        independentTicks.Options.YAxis.TickCount = 10;
        var ticks = independentTicks.Prepare(VisualExportRequest.ForChart(independentTicks).Context).Scene;
        Assert(ticks.Nodes.Count(node => node.Role == "axis-y-label") > 2, "Value ticks must honor their own density independently of categorical ticks.");

        var cramped = Chart.Create().WithSize(420, 220).WithLegend(false).WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Inside)
            .ConfigureDataLabelStyle(style => style.WithColor("#FF00FF").WithFontSize(72))
            .AddWaterfall("Delta", Points(.01, 100, -25));
        cramped.Options.YAxis.Visible = false;
        var withLabels = cramped.Prepare(VisualExportRequest.ForChart(cramped).Context).Scene;
        var withoutLabels = cramped.WithDataLabels(false).Prepare(VisualExportRequest.ForChart(cramped).Context).Scene;
        string[] Categories(VisualScene scene) => scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "axis-x-label")
            .SelectMany(node => node.Text.Lines.Select(line => line.Text)).ToArray();
        Assert(Categories(withLabels).SequenceEqual(Categories(withoutLabels)), "An oversized inside data label must not suppress an independent category label.");
    }

    private static Chart WaterfallSample() => Chart.Create().WithSize(560, 320).WithXAxis("Stage")
        .AddWaterfall("Delta", Points(18, -42, -12, 9));

    private static void WaterfallAutomaticLabelsFollowMappedValueEnds() {
        foreach (var secondary in new[] { false, true })
        foreach (var reversed in new[] { false, true })
        foreach (var sign in new[] { 1, -1 }) {
            var chart = WaterfallLabelSample(640, false).AddWaterfall("Delta", Points(40 * sign, -20 * sign), ChartColor.FromHex("#172554"));
            if (secondary) chart.Series[0].UseSecondaryYAxis();
            (secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis).WithBounds(-100, 100).WithReversal(reversed);
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
            Assert(labels.Length == 3, "Every Waterfall delta and derived total must retain its data label.");
            for (var index = 0; index < labels.Length; index++) {
                var label = labels[index];
                var bounds = WaterfallLabelBounds(label);
                var mark = prepared.Regions.Single(region => region.Id + "-label" == label.Id).Bounds;
                var delta = (index == 1 ? -20 : index == 0 ? 40 : 20) * sign;
                Assert(delta > 0 != reversed ? bounds.Bottom < mark.Top : bounds.Top > mark.Bottom,
                    "Automatic Waterfall labels must prefer the outside of the mapped value end, including negative and total steps on either value axis.");
            }
        }
    }

    private static void WaterfallContainedLabelsUseDrawnFill() {
        foreach (var dark in new[] { false, true })
        foreach (var style in new[] { ChartBarStyle.Flat, ChartBarStyle.SegmentedCapsule })
        foreach (var paint in new[] { "status", "series", "point" }) {
            var chart = WaterfallLabelSample(180, dark).WithBarStyle(style)
                .AddWaterfall("Delta", Points(100), paint == "series" ? ChartColor.FromHex("#172554") : null);
            if (paint == "point") chart.Series[0].WithPointColor(0, ChartColor.FromHex("#172554"));
            chart.Options.YAxis.WithBounds(0, 100).WithReversal();
            var context = VisualExportRequest.ForChart(chart).Context;
            var prepared = chart.Prepare(context);
            var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
            Assert(labels.Length == 2, "Compact Waterfall labels must retain both the source delta and derived total.");
            var pixels = ReadPngRgba(prepared.ToPng(), out var width, out _);
            foreach (var label in labels) {
                var bounds = WaterfallLabelBounds(label);
                var mark = prepared.Regions.Single(region => region.Id + "-label" == label.Id).Bounds;
                var fill = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "waterfall-bar" && node.Bounds.Equals(mark)).Fill!.Value;
                var backdrop = ChartStateMark.Backdrop(chart.Options, context.Theme.Resolve(context.ThemeMode), context.Frame);
                var visibleFill = ChartColorMath.Blend(backdrop, ChartColor.FromRgb(fill.R, fill.G, fill.B), fill.A / 255d);
                var expected = ChartColorMath.AccessibleTextOnBackground(visibleFill);
                Assert(LabelPlacementService.Contains(mark, bounds), "A boundary-clipped Waterfall caption must fit inside its own painted segment.");
                Assert(label.Color.Equals(expected), "Contained Waterfall captions must contrast with the actual status, series, or point fill and body opacity.");
                Assert(CountNearColorInRect(pixels, width, (int)System.Math.Ceiling(bounds.Left), (int)System.Math.Ceiling(bounds.Top),
                    (int)System.Math.Floor(bounds.Width), (int)System.Math.Floor(bounds.Height), expected.R, expected.G, expected.B, 8) > 0,
                    "Native PNG must paint the contrasting contained caption.");
                if (style == ChartBarStyle.Flat) {
                    var role = paint == "series" || paint == "point" && label.Id!.Contains("-point-") ? SvgColorRole.Series : SvgColorRole.Status;
                    var variables = new SvgColorVariables().AddInk("--drawn-ink", fill, expected, role);
                    var svg = System.Xml.Linq.XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: variables)));
                    var text = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-source-id") == label.Id).Descendants()
                        .Single(node => node.Name.LocalName == "text");
                    Assert(text.Attribute("fill")!.Value.Contains("var(--drawn-ink,"), "Contained SVG ink must retain the same status or authored series provenance as its fill.");
                }
            }
        }

        foreach (var level in new[] { "chart", "series", "point" }) {
            var chart = WaterfallLabelSample(180, false).AddWaterfall("Delta", Points(100), ChartColor.FromHex("#172554"));
            chart.Options.YAxis.WithBounds(0, 100).WithReversal();
            if (level == "chart") chart.ConfigureDataLabelStyle(style => style.WithColor("#FFFF00"));
            else if (level == "series") chart.Series[0].ConfigureDataLabelStyle(style => style.WithColor("#FFFF00"));
            else chart.Series[0].ConfigurePointDataLabelStyle(0, style => style.WithColor("#FFFF00"));
            var labels = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
            Assert(labels[0].Color.Equals(ChartColor.FromHex("#FFFF00")), "Chart, series, and point authored label ink must remain authoritative inside Waterfall marks.");
            Assert(labels[1].Color.Equals(level == "point" ? ChartColor.White : ChartColor.FromHex("#FFFF00")), "A source point ink override must not spill into the independently derived total.");
        }
    }

    private static Chart WaterfallLabelSample(int width, bool dark) => Chart.Create().WithSize(width, width == 180 ? 180 : 360)
        .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels().WithBarStyle(ChartBarStyle.Flat)
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());

    private static ChartRect WaterfallLabelBounds(VisualSceneText text) =>
        new(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
}
