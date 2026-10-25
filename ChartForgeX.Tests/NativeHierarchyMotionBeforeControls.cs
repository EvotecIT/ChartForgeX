using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Existing native source/value APIs only; no proposed hierarchy visibility target/view member is used.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeHierarchyMotionBeforeControls {
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(true, true)]
    public void ExistingNativeHierarchyValueApiCanCaptureCompatibleAuthoredLeafChanges(bool sunburst, bool authoredTotal) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareHierarchySection(Model(sunburst, authoredTotal, 30));
        var to = renderer.PrepareHierarchySection(Model(sunburst, authoredTotal, 40)); var middle = renderer.PrepareHierarchySection(Model(sunburst, authoredTotal, 35));
        var name = "hierarchy-motion-before-" + sunburst + "-" + authoredTotal;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent");
        string? payload = null; Exception? failure = null;
        try { payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = failure?.GetType().Name, message = failure?.Message }));
        Assert.Null(failure); Assert.NotNull(payload); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
    }
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void OrdinaryFullAndNativeSubrootViewsRetainExactSourceAndRepeatableDefaultInk(bool sunburst, bool authoredTotal, bool subroot) {
        var model = Model(sunburst, authoredTotal, 30); var renderer = new HtmlInteractiveChartRenderer(); var view = subroot ? new ChartHierarchyView("branch") : null;
        var prepared = renderer.PrepareHierarchySection(model, view); var repeat = renderer.PrepareHierarchySection(model, view);
        var name = "hierarchy-motion-ordinary-" + sunburst + "-" + authoredTotal + "-" + subroot;
        NativeRadialMotionBeforeControls.Capture(prepared, name);
        NativeRadialMotionBeforeControls.Save(name + ".source.json", JsonSerializer.Serialize(new {
            model.Series[0].Id, model.Options.Sunburst.ParentValuePolicy,
            items = model.Series[0].HierarchyItems.Select(item => new { item.Id, item.ParentId, item.Label, item.Value, item.ColorValue }), root = view?.RootId }));
        Assert.Equal(prepared.Html, repeat.Html); Assert.Equal(prepared.PreparedVisual.ToSvg(), repeat.PreparedVisual.ToSvg()); Assert.Equal(prepared.PreparedVisual.ToPng(), repeat.PreparedVisual.ToPng());
        Assert.Equal(5, model.Series[0].HierarchyItems.Count); Assert.Equal(subroot ? "branch" : null, prepared.PreparedVisual.HierarchyView?.RootId);
    }
    internal static Chart Model(bool sunburst, bool authoredTotal, double reading) {
        var items = new[] { new ChartHierarchyItem("root", "H", value: authoredTotal ? 100 : null),
            new ChartHierarchyItem("branch", "H", "root", authoredTotal ? 60 : null), new ChartHierarchyItem("leaf-a", "H", "branch", reading),
            new ChartHierarchyItem("leaf-b", "H", "branch", 20), new ChartHierarchyItem("leaf-c", "H", "root", 20) };
        var series = sunburst ? ChartSeries.CreateSunburst(new ChartSeriesId("areas"), "H", items) : ChartSeries.CreateTreemap(new ChartSeriesId("areas"), "H", items);
        var chart = Chart.Create().WithSize(760, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithHeader(false).WithLegend(false).WithDataLabels(true).AddSeries(series);
        if (sunburst) chart.ConfigureSunburst(options => options.ParentValuePolicy = authoredTotal ? ChartHierarchyValuePolicy.AuthoredTotal : ChartHierarchyValuePolicy.LeafAggregate);
        return chart;
    }
}
