using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyHeaderMeasurementPolicyTests : IDisposable {
    private const string Family = "CFX Header Policy Font";

    [Theory]
    [InlineData(TextMeasurementMode.PortableEstimate)]
    [InlineData(TextMeasurementMode.InstalledFonts)]
    public void HeaderLayoutHonorsTheSelectedMeasurementPolicyBeforePaintCapture(TextMeasurementMode mode) {
        // An unresolved family can select a narrow installed fallback on some hosts. Use two
        // controlled faces so the layout-policy distinction does not depend on system fonts.
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(extraGlyphs:
            new Dictionary<int, int> { ['i'] = OpenTypeTestFonts.H }));
        var chart = Fixture();
        var options = Options(mode);
        var wide = chart.Prepare(options);
        Capture(wide, "wide", mode);
        var wideGeometry = Geometry(wide);
        var wideHeader = Assert.Single(wide.Analyze().Groups).HeaderBounds;
        var wideSvg = wide.ToSvg();
        var widePng = wide.ToPng();
        var wideReport = JsonSerializer.Serialize(wide.Analyze());

        Register();
        var registered = chart.Prepare(options);
        Capture(registered, "registered", mode);
        var registeredGeometry = Geometry(registered);
        if (mode == TextMeasurementMode.PortableEstimate) {
            Assert.Equal(wideGeometry, registeredGeometry);
            Assert.Equal(106, registered.Analyze().Nodes.Single(node => node.Id == "member").Bounds.Y, 6);
        } else {
            Assert.NotEqual(wideGeometry, registeredGeometry);
            Assert.Equal(49, registered.Analyze().Nodes.Single(node => node.Id == "member").Bounds.Y, 6);
        }
        // Painted geometry still follows the actual prepared face, independently of portable reservations.
        Assert.NotEqual(wideHeader, Assert.Single(registered.Analyze().Groups).HeaderBounds);
        Assert.NotEqual(widePng, registered.ToPng());
        Assert.Equal(wideSvg, wide.ToSvg());
        Assert.Equal(widePng, wide.ToPng());
        Assert.Equal(wideReport, JsonSerializer.Serialize(wide.Analyze()));
        AssertPaintMatchesBothExports(wide);
        AssertPaintMatchesBothExports(registered);
        Assert.Equal(49, chart.Nodes.Single(node => node.Id == "member").Y);
    }

    [Fact]
    public void UncapturedHeaderReservationsFollowCurrentGeometryAndCaptionOptions() {
        var chart = Fixture();
        var options = Options(TextMeasurementMode.PortableEstimate);
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var group = Assert.Single(prepared.Groups);
        var original = TopologyGroupHeader.PaintedParts(prepared, group, options).ToArray();
        Assert.Equal(20 * prepared.Theme.Typography.DataLabelSize * .62, Assert.Single(original).Width, 6);

        // Normalization can expand a group after its header was first requested.
        group.X += 40; group.Y += 20; group.Width = 100;
        var changed = Assert.Single(TopologyGroupHeader.PaintedParts(prepared, group, options));
        Assert.Equal(group.X + 12, changed.X, 6);
        Assert.Equal(group.Y + 9, changed.Y, 6);
        Assert.InRange(changed.Width, 1, group.Width - 24);
        Assert.NotEqual(original[0], changed);

        group.Label = "First\nSecond";
        options.AllowMultilineNodeLabels = true;
        var firstLine = Assert.Single(TopologyGroupHeader.PaintedParts(prepared, group, options));
        options.AllowMultilineNodeLabels = false;
        var joined = Assert.Single(TopologyGroupHeader.PaintedParts(prepared, group, options));
        Assert.True(joined.Width > firstLine.Width);
        options.IncludeGroupStatusDots = true;
        var status = TopologyGroupHeader.PaintedParts(prepared, group, options);
        Assert.Equal(2, status.Count);
        Assert.Equal(group.X + group.Width - 16, status[1].X, 6);
        options.IncludeGroupLabels = false;
        Assert.Empty(TopologyGroupHeader.PaintedParts(prepared, group, options));
        Assert.Single(original);
        Assert.Equal(44, original[0].X, 6);
    }

    private static TopologyChart Fixture() => TopologyChart.Create().WithViewport(400, 300, 16).WithLegend(null)
        .WithTheme(VisualTheme.Graphite().WithFontFamily(Family))
        .AddGroup("service", new string('i', 20), 32, 40, 300, 180)
        .AddNode("member", "Member", 110, 49, TopologyNodeKind.Generic, groupId: "service", width: 20, height: 20)
        .AddNode("target", "Target", 250, 150, TopologyNodeKind.Generic, width: 20, height: 10)
        .AddEdge("link", "member", "target", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

    private static TopologyRenderOptions Options(TextMeasurementMode mode) => new() {
        IncludeTitle = false, IncludeLegend = false, IncludeGroupStatusDots = false,
        IncludeStatusBadges = false, IncludeNodeLabels = false,
        TextMeasurementMode = mode
    };

    private static string Geometry(PreparedTopology prepared) {
        var report = prepared.Analyze();
        return JsonSerializer.Serialize(new { report.Width, report.Height,
            Groups = report.Groups.Select(group => new { group.Id, group.Bounds }),
            Nodes = report.Nodes.Select(node => new { node.Id, node.Bounds }),
            Routes = report.Edges.Select(edge => new { edge.Id, edge.Points }) });
    }

    private static void Register() {
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(Family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(Family, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
    }

    private static void Capture(PreparedTopology prepared, string font, TextMeasurementMode mode) =>
        ScatterBubbleColorRenderingTests.Capture(prepared.Visual, "header-policy-" + mode.ToString().ToLowerInvariant() + "-" + font);

    private static void AssertPaintMatchesBothExports(PreparedTopology prepared) {
        var report = prepared.Analyze();
        Assert.DoesNotContain(report.RouteCrossings, crossing => crossing.Kind == TopologyLayoutRouteCrossingKind.GroupHeader);
        var route = Assert.Single(report.Edges).Points;
        var renderedRoute = Assert.Single(prepared.ToInterchangeEnvelope().Edges).ResolvedRoute;
        Assert.Equal(renderedRoute.Count, route.Count);
        for (var index = 0; index < route.Count; index++) {
            Assert.Equal(renderedRoute[index].X, route[index].X, 6);
            Assert.Equal(renderedRoute[index].Y, route[index].Y, 6);
        }
        var caption = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-group-label");
        var bounds = new ChartRect(caption.X, caption.Baseline - caption.Text.Ascent, caption.Text.Metrics.Width, caption.Text.Metrics.Height);
        var header = Assert.Single(report.Groups).HeaderBounds!.Value;
        Assert.Equal(bounds, header);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(Assert.Single(caption.Text.Lines).Text,
            Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-group-label").Value);
        var image = PngReader.Decode(prepared.ToPng());
        Assert.Equal((int)Math.Ceiling(prepared.Width), image.Width);
        Assert.Equal((int)Math.Ceiling(prepared.Height), image.Height);
        var hasInk = false;
        for (var y = Math.Max(0, (int)Math.Floor(bounds.Top)); y < Math.Min(image.Height, (int)Math.Ceiling(bounds.Bottom)); y++)
            for (var x = Math.Max(0, (int)Math.Floor(bounds.Left)); x < Math.Min(image.Width, (int)Math.Ceiling(bounds.Right)); x++) {
                var offset = (y * image.Width + x) * 4;
                var color = caption.Color;
                if (Math.Abs(image.Pixels[offset] - color.R) <= 3 && Math.Abs(image.Pixels[offset + 1] - color.G) <= 3
                    && Math.Abs(image.Pixels[offset + 2] - color.B) <= 3 && image.Pixels[offset + 3] > 0) hasInk = true;
            }
        Assert.True(hasInk, "The native raster must paint the measured caption retained by diagnostics and SVG.");
    }

    public void Dispose() => FontRegistry.Clear();
}
