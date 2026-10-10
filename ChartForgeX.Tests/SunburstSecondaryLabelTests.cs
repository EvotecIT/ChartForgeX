using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SunburstSecondaryLabelTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t")]
    public void SuppressedSecondaryTextPreservesDefaultSceneAndNativePixels(string? output) {
        var chart = Gallery(360, false, 6);
        chart.Options.Sunburst.SecondaryLabelFormatter = null;
        var original = Prepare(chart);
        var calls = 0;
        chart.ConfigureSunburst(options => {
            options.SecondaryLabelFormatter = _ => { calls++; return output; };
            options.SecondaryLabelStyle.WithFontSize(20).WithColor("#ff00ff");
            options.SecondaryLabelSpacing = 12;
        });
        var suppressed = Prepare(chart);
        Assert.Equal(chart.Series[0].HierarchyItems.Count, calls);
        Assert.Equal(original.ToSvg(), suppressed.ToSvg());
        Assert.Equal(original.ToPng(new VisualRenderOptions(supersampling: 1)), suppressed.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(original.Regions.Select(region => region.Label), suppressed.Regions.Select(region => region.Label));
    }

    [Theory]
    [InlineData(ChartHierarchyValuePolicy.LeafAggregate, 60)]
    [InlineData(ChartHierarchyValuePolicy.AuthoredTotal, 80)]
    public void ContextAndPreparedTextKeepAuthoredSecondaryFactsSeparateFromResolvedNumericValues(ChartHierarchyValuePolicy policy, double engineeringSize) {
        var items = new[] {
            new ChartHierarchyItem("all", "All teams", value: 140),
            new ChartHierarchyItem("engineering", "Engineering", "all", 80, 12),
            new ChartHierarchyItem("operations", "Operations", "all", colorValue: 0),
            new ChartHierarchyItem("platform", "Support", "engineering", 35, -4),
            new ChartHierarchyItem("services", "Support", "engineering", 25),
            new ChartHierarchyItem("support", "Support", "operations", 40),
            new ChartHierarchyItem("zero", "Reserve", "operations", 0),
            new ChartHierarchyItem("tiny", "Tiny", "operations", 1e-20)
        };
        var chart = Chart.Create().WithSize(500, 400).WithDataLabels(false).WithLegend(false)
            .WithValueFormat(ChartValueFormat.Number("0.0", CultureInfo.GetCultureInfo("pl-PL"))).AddSunburst("Teams", items);
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var contexts = new Dictionary<string, ChartSunburstLabelContext>();
        var descriptions = items.ToDictionary(item => item.Id, item => "Authored <&> " + item.Id);
        chart.ConfigureSunburst(options => {
            options.ParentValuePolicy = policy;
            options.SecondaryLabelFormatter = context => { contexts.Add(context.Item.Id, context); return descriptions[context.Item.Id]; };
        });
        var prepared = Prepare(chart); var groups = Groups(prepared);
        Assert.Equal(items.Length, contexts.Count);
        Assert.Equal(80, contexts["engineering"].Item.Value);
        Assert.Equal(engineeringSize, contexts["engineering"].Value);
        Assert.Equal(engineeringSize.ToString("0.0", CultureInfo.GetCultureInfo("pl-PL")), contexts["engineering"].FormattedValue);
        Assert.All(items, item => {
            var metadata = groups[item.Id].Metadata;
            Assert.Equal(descriptions[item.Id], metadata["data-cfx-secondary-label"]);
            Assert.Equal(contexts[item.Id].FormattedValue, metadata["data-cfx-formatted-value"]);
            Assert.Contains(descriptions[item.Id], prepared.Regions.Single(region => region.Id == groups[item.Id].Id).Label);
            Assert.False(metadata.ContainsKey("data-cfx-point"));
        });
        Assert.Equal("12", groups["engineering"].Metadata["data-cfx-color-value"]);
        Assert.Equal("0", groups["operations"].Metadata["data-cfx-color-value"]);
        Assert.Equal("zero", groups["zero"].Metadata["data-cfx-geometry-status"]);
        Assert.Equal("precision-collapse", groups["tiny"].Metadata["data-cfx-geometry-status"]);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role is "sunburst-label" or "sunburst-secondary-label");
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        descriptions["engineering"] = "Changed";
        chart.ConfigureSunburst(options => {
            options.SecondaryLabelFormatter = _ => throw new InvalidOperationException("Must not re-evaluate a prepared scene.");
            options.SecondaryLabelStyle.WithFontSize(40).WithColor("#ff00ff"); options.SecondaryLabelSpacing = 100;
            options.ParentValuePolicy = policy == ChartHierarchyValuePolicy.AuthoredTotal ? ChartHierarchyValuePolicy.LeafAggregate : ChartHierarchyValuePolicy.AuthoredTotal;
        });
        chart.WithValueFormat(ChartValueFormat.Custom(_ => "Changed"));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(items.Length, contexts.Count);
    }

    [Theory]
    [InlineData(360, false, 0)]
    [InlineData(360, true, 0)]
    [InlineData(800, false, 0)]
    [InlineData(800, true, 0)]
    [InlineData(360, false, 6)]
    [InlineData(360, true, 6)]
    [InlineData(800, false, 6)]
    [InlineData(800, true, 6)]
    public void BothCaptionRunsFitTheirActualSectorAndRetainPrimaryContent(int width, bool dark, double cornerRadius) {
        var chart = Gallery(width, dark, cornerRadius);
        var formatter = chart.Options.Sunburst.SecondaryLabelFormatter;
        chart.Options.Sunburst.SecondaryLabelFormatter = null;
        var primaryOnly = Prepare(chart);
        chart.Options.Sunburst.SecondaryLabelFormatter = formatter;
        var prepared = Prepare(chart);
        Assert.Equal(PrimaryContent(primaryOnly), PrimaryContent(prepared));
        var owners = new Stack<VisualSceneGroup>(); var marks = new Dictionary<string, VisualSceneMark>(); var secondaryCount = 0;
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) { owners.Push(group); continue; }
            if (node is VisualSceneEndGroup) { owners.Pop(); continue; }
            var owner = owners.FirstOrDefault(group => group.Role == "sunburst-segment");
            if (owner == null) continue;
            if (node is VisualSceneSlice or VisualScenePath && node.Role == "sunburst-segment-mark") marks.Add(owner.Id!, (VisualSceneMark)node);
            if (node is not VisualSceneText text || text.Role is not ("sunburst-label" or "sunburst-secondary-label")) continue;
            if (text.Role == "sunburst-secondary-label") secondaryCount++;
            var contours = marks[owner.Id!] is VisualScenePath path ? VisualSceneGeometry.Flatten(path, 8) : VisualSceneGeometry.Flatten((VisualSceneSlice)marks[owner.Id!], 8);
            var rotation = owners.Select(group => group.Rotation).FirstOrDefault(value => value.HasValue);
            if (rotation.HasValue) {
                Assert.InRange(rotation.Value.Degrees, -90, 90);
                var angle = -rotation.Value.Degrees * Math.PI / 180;
                foreach (var contour in contours) for (var i = 0; i < contour.Count; i++) {
                    var point = contour[i]; var dx = point.X - rotation.Value.X; var dy = point.Y - rotation.Value.Y;
                    contour[i] = new ChartPoint(rotation.Value.X + dx * Math.Cos(angle) - dy * Math.Sin(angle), rotation.Value.Y + dx * Math.Sin(angle) + dy * Math.Cos(angle));
                }
            }
            var bounds = new ChartRect(text.X - text.Text.Metrics.Width / 2, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
            Assert.True(new LabelMarkShape(contours, true, 0).Contains(bounds), owner.Metadata["data-cfx-node"] + " " + text.Role);
        }
        Assert.True(secondaryCount >= 2);
        Assert.Equal(primaryOnly.Regions.Count, prepared.Regions.Count);
        Assert.NotEqual(primaryOnly.ToPng(new VisualRenderOptions(supersampling: 1)), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void InheritedTypographyAndSecondaryOverridesAreClonedIntoVisiblePreparedRuns() {
        var chart = Gallery(800, false, 6).ConfigureDataLabelStyle(style => style.WithFontSize(16).WithWeight("bold"));
        chart.Series[0].ConfigurePointDataLabelStyle(0, style => style.WithItalic().WithColor("#034f73"));
        var inherited = Prepare(chart).Scene.Nodes.OfType<VisualSceneText>().Single(text => text.Id == "series-0-node-secondary-label-0");
        Assert.Equal(12.8, inherited.Text.Size, 8); Assert.Equal(700, inherited.Text.Style.Font.Weight);
        Assert.True(inherited.Text.Style.Font.Italic); Assert.Equal(ChartColor.FromHex("#034f73"), inherited.Color);
        chart.ConfigureSunburst(options => options.SecondaryLabelStyle.WithFontSize(10).WithWeight("normal").WithColor("#713c11").WithTextCase(TextCaseTransform.Uppercase));
        var prepared = Prepare(chart); var text = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(text => text.Id == "series-0-node-secondary-label-0");
        Assert.Equal(10, text.Text.Size); Assert.Equal(400, text.Text.Style.Font.Weight); Assert.True(text.Text.Style.Font.Italic);
        Assert.Equal("ALLOCATION", Assert.Single(text.Text.Lines).Text); Assert.Equal(ChartColor.FromHex("#713c11"), text.Color);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        var exported = XDocument.Parse(svg).Descendants().Single(element => (string?)element.Attribute("data-cfx-source-id") == text.Id);
        Assert.Equal(ChartColor.FromHex("#713c11").ToCss(), (string?)Assert.Single(exported.Elements()).Attribute("fill"));
        chart.Options.Sunburst.SecondaryLabelStyle.WithFontSize(25).WithColor("#ff00ff");
        chart.Series[0].ConfigurePointDataLabelStyle(0, style => style.WithColor("#ff00ff"));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void SecondaryOverflowShortensWholeTextElementsOrYieldsToPrimaryWithoutChangingItsPixels() {
        var chart = Gallery(800, false, 6);
        chart.Options.Sunburst.SecondaryLabelFormatter = _ => null;
        var original = Prepare(chart);
        var complete = string.Concat(Enumerable.Repeat("e\u0301", 100));
        chart.Options.Sunburst.SecondaryLabelFormatter = context => context.Item.Id == "all" ? complete : null;
        var shortened = Prepare(chart);
        var caption = Assert.Single(shortened.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "sunburst-secondary-label");
        var shown = string.Join("\n", caption.Text.Lines.Select(line => line.Text));
        Assert.EndsWith("e\u0301…", shown); Assert.Equal(complete, Groups(shortened)["all"].Metadata["data-cfx-secondary-label"]);
        chart.Options.Sunburst.SecondaryLabelSpacing = 10000;
        var omitted = Prepare(chart);
        Assert.DoesNotContain(omitted.Scene.Nodes, node => node.Role == "sunburst-secondary-label");
        Assert.Equal(PrimaryContent(original), PrimaryContent(omitted));
        Assert.Equal(original.ToPng(new VisualRenderOptions(supersampling: 1)), omitted.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Contains(omitted.Diagnostics, diagnostic => diagnostic.Message.Contains("preserve its primary", StringComparison.Ordinal));
        Assert.Equal(complete, Groups(omitted)["all"].Metadata["data-cfx-secondary-label"]);
    }

    [Fact]
    public void SpacingValidationAndActiveFamilyApplicabilityFollowExistingOptionContracts() {
        var options = new ChartSunburstOptions(); Assert.Equal(2, options.SecondaryLabelSpacing); options.SecondaryLabelSpacing = 0;
        foreach (var invalid in new[] { -1, double.NaN, double.NegativeInfinity, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => options.SecondaryLabelSpacing = invalid);
        var bar = Chart.Create().AddBar("Size", new[] { new ChartPoint(1, 5) });
        bar.ConfigureSunburst(sunburst => { sunburst.SecondaryLabelStyle.WithFontSize(10); sunburst.SecondaryLabelSpacing = 0; });
        _ = Prepare(bar);
        bar.Options.Sunburst.SecondaryLabelFormatter = _ => "Caption";
        Assert.Throws<InvalidOperationException>(() => Prepare(bar));
        var sunburst = Gallery(360, false, 0); sunburst.Options.Sunburst.SecondaryLabelFormatter = _ => throw new FormatException("Authored formatter failure");
        Assert.Throws<FormatException>(() => Prepare(sunburst));
    }

    internal static Chart Gallery(int width, bool dark, double radius) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Sunburst, "options", dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(width, width < 500 ? 360 : 440).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithHeader(false).WithLegend(false);
        chart.Options.Sunburst.CornerRadius = radius; RoundedRadialSeriesTests.UseFixtureFont(chart); return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static Dictionary<string, VisualSceneGroup> Groups(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>()
        .Where(group => group.Role == "sunburst-segment").ToDictionary(group => group.Metadata["data-cfx-node"]);
    private static string[] PrimaryContent(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "sunburst-label")
        .OrderBy(text => text.Id).Select(text => text.Id + ":" + string.Join("\n", text.Text.Lines.Select(line => line.Text))).ToArray();
}
