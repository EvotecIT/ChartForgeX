using System.Xml.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeSeriesVisibilityTests {
    [Theory, MemberData(nameof(NativeSeriesVisibilityDefaultControls.Cases), MemberType = typeof(NativeSeriesVisibilityDefaultControls))]
    public void NullAndDefaultVisibleViewsRetainOrdinaryNativeBytesAndIdentities(string family, int width, bool dark) {
        var chart = NativeSeriesVisibilityDefaultControls.Model(family, width, dark); var context = VisualExportRequest.ForChart(chart).Context;
        var ordinary = chart.Prepare(context); var nullView = chart.PrepareVisibility(context); var all = chart.PrepareVisibility(context, new ChartVisibilityView());
        NativeSeriesVisibilityDefaultControls.Save("all-visible-" + family + "-" + width + "-" + dark, all);
        Assert.Equal(ordinary.ToSvg(), nullView.ToSvg()); Assert.Equal(ordinary.ToPng(), nullView.ToPng());
        Assert.Equal(ordinary.ToSvg(), all.ToSvg()); Assert.Equal(ordinary.ToPng(), all.ToPng());
        Assert.Null(nullView.VisibilityView); Assert.NotNull(all.VisibilityView);
    }
    [Theory, InlineData(390, true), InlineData(760, false)]
    public void HiddenEndpointsRetainSourceGeometryCatalogDomainsAndExactLabelOwners(int width, bool dark) {
        var chart = NativeSeriesVisibilityDefaultControls.Model("mixed-labels", width, dark); var context = VisualExportRequest.ForChart(chart).Context;
        var ordinary = chart.Prepare(context); var hidden = chart.PrepareVisibility(context, new ChartVisibilityView(new[] { new ChartSeriesId("observed") }));
        NativeSeriesVisibilityDefaultControls.Save("hidden-overlay-" + width + "-" + dark, hidden);
        var before = ordinary.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "series").ToArray();
        var after = hidden.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "series").ToArray();
        Assert.Equal(before.Select(group => group.Id), after.Select(group => group.Id)); Assert.Equal("false", after.Single(group => group.Metadata["data-cfx-series-key"] == "observed").Metadata["data-cfx-visible"]);
        Assert.Equal(ordinary.XProjection!.FullMinimum, hidden.XProjection!.FullMinimum); Assert.Equal(ordinary.XProjection.FullMaximum, hidden.XProjection.FullMaximum);
        Assert.Equal(ordinary.YProjection!.FullMinimum, hidden.YProjection!.FullMinimum); Assert.Equal(ordinary.YProjection.FullMaximum, hidden.YProjection.FullMaximum);
        var sourceIds = NativeGroups(ordinary.ToSvg("visibility"), "point").Where(node => (string?)node.Attribute("data-cfx-series-key") == "observed")
            .Select(node => (string)node.Attribute("data-cfx-source-id")!).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain(hidden.Regions, region => sourceIds.Contains(region.Id));
        Assert.Equal(ordinary.Regions.Count(region => region.Role == "legend"), hidden.Regions.Count(region => region.Role == "legend"));
        Assert.All(NativeGroups(hidden.ToSvg("visibility"), "data-label-source"), node => {
            var owner = (string?)node.Attribute("data-cfx-label-for"); Assert.NotNull(owner);
            Assert.Equal(sourceIds.Contains(owner!) ? "0" : null, (string?)node.Attribute("opacity"));
        });
        Assert.NotEqual(ordinary.ToPng(), hidden.ToPng());
    }
    [Theory, InlineData(false), InlineData(true)]
    public void EnabledLabelsRequireExplicitGroupingAndEveryCaptionMovesWithItsExactNativeOwner(bool dark) {
        var chart = NativeSeriesVisibilityDefaultControls.Model("mixed-labels", 760, dark); var context = VisualExportRequest.ForChart(chart).Context;
        var overlay = chart.PrepareVisibility(context, new ChartVisibilityView());
        var overlayHidden = chart.PrepareVisibility(context, new ChartVisibilityView(new[] { new ChartSeriesId("observed") }));
        Assert.Throws<NotSupportedException>(() => VisualSeriesVisibilityCapture.Frames(overlay, overlayHidden, 3));
        ChartVisibilityView View(bool hide) => new(hide ? new[] { new ChartSeriesId("observed") } : null, ChartVisibilityLabelLayering.GroupWithSeries);
        var from = chart.PrepareVisibility(context, View(false)); var to = chart.PrepareVisibility(context, View(true));
        var middle = VisualSeriesVisibilityCapture.Frames(from, to, 3)[1]; NativeSeriesVisibilityDefaultControls.Save("grouped-midpoint-" + dark, middle);
        var document = XDocument.Parse(middle.ToSvg("grouped"));
        var composites = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "series-visibility").ToArray();
        Assert.Equal(2, composites.Length);
        foreach (var composite in composites) {
            var key = (string)composite.Attribute("data-cfx-visibility-series-key")!;
            var ids = composite.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point")
                .Select(node => (string)node.Attribute("data-cfx-source-id")!).ToHashSet(StringComparer.Ordinal);
            Assert.All(composite.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "data-label-source"),
                label => Assert.Contains((string)label.Attribute("data-cfx-label-for")!, ids));
            Assert.Equal(key == "observed" ? "0.5" : null, (string?)composite.Attribute("opacity"));
            Assert.DoesNotContain(composite.Descendants(), node => node.Attribute("opacity") != null);
        }
        Assert.Equal(NativeGroups(overlay.ToSvg("labels"), "data-label-source").Count(), document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "data-label-source"));
    }
    [Fact]
    public void CapturedPlotAndContainedCaptionOverlapHasAlpha128RatherThanPerPrimitive192() {
        var chart = NativeSeriesVisibilityDefaultControls.Model("bar").WithHostFrame().WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithTransparentBackground().WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Center).WithYAxisBounds(0, 100);
        chart.Series[0].WithColor(ChartColor.FromHex("#2463A1")).WithPointLabel(1, "INK");
        var context = VisualExportRequest.ForChart(chart).Context;
        var from = chart.PrepareVisibility(context, new ChartVisibilityView(labelLayering: ChartVisibilityLabelLayering.GroupWithSeries));
        var to = chart.PrepareVisibility(context, new ChartVisibilityView(new[] { new ChartSeriesId("observed") }, ChartVisibilityLabelLayering.GroupWithSeries));
        var middle = VisualSeriesVisibilityCapture.Frames(from, to, 3)[1]; var full = from.ToRgba(new VisualRenderOptions(supersampling: 1)); var half = middle.ToRgba(new VisualRenderOptions(supersampling: 1));
        NativeSeriesVisibilityDefaultControls.Save("native-label-alpha-overlap", middle);
        var ink = 0; for (var index = 0; index < full.Pixels.Length; index += 4) {
            if (full.Pixels[index + 3] != 255) continue;
            Assert.Equal((byte)128, half.Pixels[index + 3]);
            if (full.Pixels[index] != 0x24 || full.Pixels[index + 1] != 0x63 || full.Pixels[index + 2] != 0xA1) ink++;
        }
        Assert.True(ink > 0, "The real contained native caption must overlap opaque native bar ink before the alpha assertion is meaningful.");
    }
    [Fact]
    public void CaptureRejectsSourcePolicyChangesAndRetainsFontsAfterCallerMutation() {
        var chart = NativeSeriesVisibilityDefaultControls.Model("mixed"); var context = VisualExportRequest.ForChart(chart).Context;
        var from = chart.PrepareVisibility(context, new ChartVisibilityView());
        var to = chart.PrepareVisibility(context, new ChartVisibilityView(new[] { new ChartSeriesId("observed") }));
        var captured = VisualSeriesVisibilityCapture.Frames(from, to, 3); var svg = captured[1].ToSvg(); var png = captured[1].ToPng();
        chart.WithFontFamily("Unavailable later face").WithSize(120, 120).Series[0].WithColor(ChartColor.Black);
        try { FontRegistry.Clear(); var again = VisualSeriesVisibilityCapture.Frames(from, to, 3); Assert.Equal(svg, again[1].ToSvg()); Assert.Equal(png, again[1].ToPng()); }
        finally { _ = BrowserProofFonts.Family; }
        var changed = NativeSeriesVisibilityDefaultControls.Model("mixed"); changed.Series[0].WithColor(ChartColor.Black);
        Assert.Throws<NotSupportedException>(() => VisualSeriesVisibilityCapture.Frames(from, changed.PrepareVisibility(VisualExportRequest.ForChart(changed).Context,
            new ChartVisibilityView(new[] { new ChartSeriesId("observed") })), 3));
        var rejected = NativeSeriesVisibilityDefaultControls.Model("mixed");
        Assert.Throws<ArgumentException>(() => rejected.PrepareVisibility(VisualExportRequest.ForChart(rejected).Context, new ChartVisibilityView(new[] { new ChartSeriesId("unknown") })));
        Assert.Throws<NotSupportedException>(() => Chart.Create().AddLine("Automatic", ChartPoints.FromValues(1, 2, 3)).PrepareVisibility(context, new ChartVisibilityView()));
        Assert.Throws<ArgumentException>(() => new ChartVisibilityView(new[] { new ChartSeriesId("same"), new ChartSeriesId("same") }));
    }
    [Theory, InlineData(390, true), InlineData(760, false)]
    public void ExistingHtmlShellCapturesTheSameAlignedVisibilityAndNullRetainsOrdinaryMarkup(int width, bool dark) {
        var chart = NativeSeriesVisibilityDefaultControls.Model("mixed", width, dark); var renderer = new HtmlInteractiveChartRenderer();
        void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "native-series-visibility"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
        }
        Assert.Equal(renderer.PrepareSection(chart, configure: Configure).Html, renderer.PrepareVisibilitySection(chart, configure: Configure).Html);
        var hidden = renderer.PrepareVisibilitySection(chart, new ChartVisibilityView(new[] { new ChartSeriesId("observed") }), configure: Configure);
        NativeSeriesVisibilityDefaultControls.Save("html-hidden-" + width + "-" + dark, hidden.PreparedVisual, hidden.Html,
            new { width, dark, sourceCatalog = chart.Series.Select(series => series.Id.Value).ToArray() });
        NativeSeriesVisibilityDefaultControls.Save("html-hidden-narrow-" + width + "-" + dark, hidden.NarrowPreparedVisual!);
        using var metadata = JsonDocument.Parse(WebUtility.HtmlDecode(Regex.Match(hidden.Html, "data-cfx-prepared-chart=\"([^\"]+)\"").Groups[1].Value));
        Assert.Equal(2, metadata.RootElement.GetProperty("series").GetArrayLength());
        Assert.Equal("observed", metadata.RootElement.GetProperty("visibility").GetProperty("hiddenSeries")[0].GetString());
        Assert.Equal("preserveOverlay", metadata.RootElement.GetProperty("visibility").GetProperty("labelLayering").GetString());
        Assert.Equal(hidden.PreparedVisual.XProjection!.FullMinimum, hidden.NarrowPreparedVisual!.XProjection!.FullMinimum);
        Assert.Equal(hidden.PreparedVisual.XProjection.FullMaximum, hidden.NarrowPreparedVisual.XProjection.FullMaximum);
        Assert.Equal(hidden.PreparedVisual.VisibilityView!.HiddenSeries, hidden.NarrowPreparedVisual.VisibilityView!.HiddenSeries);
    }
    private static IEnumerable<XElement> NativeGroups(string svg, string role) => XDocument.Parse(svg).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role);
}
