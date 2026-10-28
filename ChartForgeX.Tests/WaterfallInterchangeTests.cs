using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WaterfallInterchangeTests {
    [Theory]
    [InlineData(false, false, false, 390)] [InlineData(false, false, true, 900)]
    [InlineData(false, true, true, 390)] [InlineData(false, true, false, 900)]
    [InlineData(true, false, true, 390)] [InlineData(true, false, false, 900)]
    [InlineData(true, true, false, 390)] [InlineData(true, true, true, 900)]
    public void SaveLoadEditRetainsAuthoringBalanceIdentityAndNativePresentation(bool typed, bool horizontal, bool dark, int width) {
        var original = Model(typed, horizontal, dark, width);
        var artifact = original.ToVisualArtifact("portable-waterfall"); artifact.Metadata["host-note"] = "<inert> caller metadata";
        artifact.Accessibility.Name = "Portable balance"; artifact.Accessibility.Language = "en";
        original.Accessibility.Name = artifact.Accessibility.Name; original.Accessibility.Language = "en";
        var bytes = artifact.ToInterchangeUtf8Json(); var envelope = VisualArtifactInterchangeEnvelope.FromUtf8Json(bytes);
        Assert.Equal(VisualArtifactInterchangeFamily.Waterfall, envelope.Family); Assert.Equal(VisualArtifactKind.Chart, envelope.Kind);
        Assert.True(artifact.ExportFormats.HasFlag(VisualArtifactExportFormat.Json)); Assert.Equal(1, envelope.Version);
        Assert.Equal("<inert> caller metadata", envelope.Extensions["host-note"]);
        Assert.Equal(bytes, envelope.ToUtf8Json());
        var source = envelope.Waterfall!; Assert.Equal(typed, source.TypedItems);
        Assert.Equal(typed ? 7 : 0, source.Items.Count); Assert.Equal(typed ? 0 : 3, source.DeltaPoints.Count);
        if (typed) Assert.All(source.Items.Where(item => item.Kind is ChartWaterfallItemKind.Subtotal or ChartWaterfallItemKind.Total), item => Assert.Null(item.Value));
        var imported = envelope.ToWaterfallChart();
        Assert.Equal(original.Options.Theme.ToJson(), imported.Options.Theme.ToJson());
        Assert.Equal(original.Options.Waterfall.Orientation, imported.Options.Waterfall.Orientation);
        Assert.Equal(original.Series[0].Id, imported.Series[0].Id); Assert.False(imported.Series[0].Id.IsAutomatic);
        Assert.Equal(original.Series[0].ObservationIds.OrderBy(pair => pair.Key), imported.Series[0].ObservationIds.OrderBy(pair => pair.Key));
        Assert.Equal(original.Series[0].SourcePointCount, imported.Series[0].SourcePointCount);
        var before = original.ToSvg(); var png = original.ToPng();
        Assert.Equal(before, imported.ToSvg()); Assert.Equal(png, imported.ToPng());
        var captured = imported.Prepare(VisualExportRequest.ForChart(imported).Context);
        var saved = captured.ToSvg(); var capturedPng = captured.ToPng();
        if (typed) imported.Series[0].UpdateWaterfallItem(3, ChartWaterfallItem.Delta(4, -125));
        else imported.Series[0].Points[1] = new ChartPoint(2, -12);
        var edited = imported.ToSvg(); Assert.NotEqual(before, edited);
        var steps = ChartWaterfallSteps.Create(imported.Series[0]);
        Assert.Equal(typed ? 35 : 73, steps[^1].Value); Assert.Equal(typed ? 7 : 4, steps.Count);
        Assert.Equal(!typed, steps[^1].IsAppendedTotal);
        Assert.Equal("authored-change", imported.Series[0].ObservationIds[typed ? 3 : 1].Value);
        var again = VisualArtifactInterchangeEnvelope.FromJson(imported.ToVisualArtifact("edited-waterfall").ToInterchangeJson()).ToWaterfallChart();
        Assert.Equal(edited, again.ToSvg()); Assert.Equal(imported.ToPng(), again.ToPng());
        Assert.Equal(before, original.ToSvg()); Assert.Equal(png, original.ToPng());
        Assert.Equal(saved, captured.ToSvg()); Assert.Equal(capturedPng, captured.ToPng());
        if (typed) Assert.Equal(-130d, envelope.Waterfall!.Items[3].Value!.Value); else Assert.Equal(-15d, envelope.Waterfall!.DeltaPoints[1].Y);
        Save("waterfall-portable-" + (typed ? "typed" : "numeric") + "-" + (horizontal ? "horizontal" : "vertical") + "-" + width + "-" + dark, edited, imported.ToPng());
    }

    [Fact]
    public void PortablePresentationAndNativeReplayKeepCultureUnitAndIdentityColors() {
        var original = Model().WithCulture(CultureInfo.GetCultureInfo("de-DE")).WithValueUnit("ms")
            .WithSeriesColorAssignment(ChartSeriesColorAssignment.Identity);
        var json = original.ToVisualArtifact().ToInterchangeJson();
        var imported = VisualArtifactInterchangeEnvelope.FromJson(json).ToWaterfallChart();
        Assert.Equal("ms", imported.Options.ValueUnit);
        Assert.Equal(ChartSeriesColorAssignment.Identity, imported.Options.SeriesColorAssignment);
        Assert.Equal("de-DE", imported.Options.XAxis.Culture?.Name);
        var legacy = VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"valueUnit\":\"ms\",", "")
            .Replace("\"seriesColorAssignment\":\"Identity\",", "").Replace("\"culture\":\"de-DE\",", "")).ToWaterfallChart();
        Assert.Null(legacy.Options.ValueUnit);
        Assert.Equal(ChartSeriesColorAssignment.Order, legacy.Options.SeriesColorAssignment);
        Assert.Null(legacy.Options.XAxis.Culture);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("de-DE", "bad_culture!")));

        var chart = Chart.Create().WithSize(600, 320).WithXAxisTimeScale(TimeZoneInfo.Utc);
        VisualArtifactInterchangeMapping.RestoreVisibilityPresentation(chart, VisualArtifactInterchangeMapping.CaptureVisibilityPresentation(imported));
        chart.WithXAxisTimeScale(TimeZoneInfo.Utc);
        chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("requests"), "Requests", new[] {
            new ChartPoint(new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc), 50),
            new ChartPoint(new DateTime(2026, 10, 21, 1, 0, 0, DateTimeKind.Utc), 60) }));
        var context = VisualExportRequest.ForChart(chart).Context;
        var source = new ChartVisibilitySource(chart, context);
        var facts = source.CanonicalFacts(context);
        Assert.Contains("\"valueUnit\":\"ms\"", facts, StringComparison.Ordinal);
        Assert.Contains("\"seriesColorAssignment\":\"Identity\"", facts, StringComparison.Ordinal);
        Assert.Contains("\"culture\":\"de-DE\"", facts, StringComparison.Ordinal);
        var view = source.CreateNativeValueView(new[] { 50d, 61d });
        Assert.Equal("de-DE", view.Options.XAxis.Culture?.Name);
        Assert.Equal("ms", view.Options.ValueUnit);
        Assert.Equal(ChartSeriesColorAssignment.Identity, view.Options.SeriesColorAssignment);
        var colors = VisualTheme.Graphite().Resolve(VisualThemeMode.Light);
        Assert.Equal(ChartSeriesColours.Resolve(chart, 0, colors), ChartSeriesColours.Resolve(view, 0, colors));
        Assert.Equal(61d, view.Series[0].Points[1].Y);
        var point = XDocument.Parse(view.ToSvg()).Descendants().First(node => (string?)node.Attribute("data-cfx-role") == "point");
        Assert.Equal("50.00\u00A0ms", (string?)point.Attribute("data-cfx-formatted-value"));
    }

    [Fact]
    public void PortableTimeCultureRejectsCustomPatternsAndRetainsExplicitInvariantCulture() {
        var modified = (CultureInfo)CultureInfo.GetCultureInfo("de-DE").Clone();
        modified.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        var custom = Model().WithCulture(modified);
        Assert.Throws<NotSupportedException>(() => custom.ToVisualArtifact().ToInterchangeJson());
        var invariant = Model(); invariant.Options.XAxis.Culture = CultureInfo.InvariantCulture;
        var restored = VisualArtifactInterchangeEnvelope.FromJson(invariant.ToVisualArtifact().ToInterchangeJson()).ToWaterfallChart();
        Assert.NotNull(restored.Options.XAxis.Culture);
        Assert.Equal(string.Empty, restored.Options.XAxis.Culture!.Name);
    }

    [Fact]
    public void NumericAppendedTotalKeepsDerivedContributorsAndPresentationWithoutBecomingSourceData() {
        var chart = Chart.Create().WithLegend(false).AddWaterfall("Changes", new[] { new ChartPoint(1, 10), new ChartPoint(3, -2) });
        chart.Series[0].WithObservationId(0, new ChartObservationId("first"));
        chart.Series[0].PointColors.AddRange(new ChartColor?[] { null, null, ChartColor.FromHex("#AB1234") });
        chart.Series[0].PointLabels.AddRange(new string?[] { null, null, "Calculated balance" });
        var envelope = chart.ToVisualArtifact().ToInterchangeEnvelope(); Assert.False(envelope.Waterfall!.TypedItems);
        Assert.Equal(2, envelope.Waterfall.DeltaPoints.Count); Assert.Empty(envelope.Waterfall.Items);
        var imported = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToWaterfallChart();
        var steps = ChartWaterfallSteps.Create(imported.Series[0]); Assert.Equal(3, steps.Count); Assert.True(steps[^1].IsAppendedTotal);
        Assert.Equal(new[] { 0, 1 }, steps[^1].SourceIndices); Assert.Equal(5, steps[^1].X);
        Assert.Equal("Calculated balance", imported.Series[0].PointLabels[2]); Assert.Equal(ChartColor.FromHex("#AB1234"), imported.Series[0].PointColors[2]);
        var total = XDocument.Parse(imported.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "waterfall-total");
        Assert.Null(total.Attribute("data-cfx-observation-id")); Assert.Equal("0,1", (string?)total.Attribute("data-cfx-source-points"));
        imported.Series[0].Points.Add(new ChartPoint(6, 5));
        Assert.Equal(13, ChartWaterfallSteps.Create(imported.Series[0])[^1].Value);
        Assert.Equal(4, ChartWaterfallSteps.Create(imported.Series[0]).Count); Assert.Equal(3, imported.Series[0].ObservationCount);
    }

    [Fact]
    public void AutomaticIdsStayChartLocalAndTheirNativeAllocationSequenceContinuesAfterImport() {
        var chart = Chart.Create().AddWaterfall("Old", new[] { new ChartPoint(1, 1) }); chart.ClearSeries();
        chart.AddWaterfall("Current", new[] { new ChartPoint(1, 2) }); var id = chart.Series[0].Id;
        var imported = chart.ToVisualArtifact().ToInterchangeEnvelope().ToWaterfallChart();
        Assert.Equal(id, imported.Series[0].Id); Assert.True(imported.Series[0].Id.IsAutomatic);
        var detached = imported.Series[0]; imported.RemoveSeries(detached);
        Assert.Throws<InvalidOperationException>(() => Chart.Create().AddSeries(detached));
        imported.AddWaterfall("Later", new[] { new ChartPoint(1, 3) });
        Assert.True(imported.Series[0].Id.IsAutomatic); Assert.NotEqual(id, imported.Series[0].Id);
        Assert.Equal("cfx:auto:3", imported.Series[0].Id.Value);
    }

    [Fact]
    public void DefaultAxisPolicyRoundTripsWithoutMakingTheDefaultScaleExplicit() {
        var automatic = Chart.Create().AddWaterfall("Default", new[] { new ChartPoint(1, 3), new ChartPoint(2, -1) });
        var saved = automatic.ToSvg(); var imported = VisualArtifactInterchangeEnvelope.FromJson(automatic.ToVisualArtifact().ToInterchangeJson()).ToWaterfallChart();
        Assert.Equal(saved, imported.ToSvg()); Assert.Equal(automatic.ToPng(), imported.ToPng());
        Assert.False(imported.Options.XAxis.HasExplicitScale);
        imported.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
        Assert.Equal(ChartScaleKind.Time, imported.Options.XAxis.Scale); Assert.Same(ChartTimeSpacing.Ordinal, imported.Options.XAxis.TimeSpacing);
        automatic.Options.XAxis.Scale = ChartScaleKind.Linear;
        var explicitLinear = VisualArtifactInterchangeEnvelope.FromJson(automatic.ToVisualArtifact().ToInterchangeJson()).ToWaterfallChart();
        Assert.True(explicitLinear.Options.XAxis.HasExplicitScale);
        Assert.Equal(ChartScaleKind.Linear, explicitLinear.Options.XAxis.Scale);
        Assert.Null(explicitLinear.Options.XAxis.TimeSpacing);
        // WithTimeSpacing deliberately selects Time, including when an earlier scale was explicit.
        explicitLinear.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
        Assert.Equal(ChartScaleKind.Time, explicitLinear.Options.XAxis.Scale);
        Assert.Same(ChartTimeSpacing.Ordinal, explicitLinear.Options.XAxis.TimeSpacing);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NativePhysicalSecondaryAxesAndCrossingPoliciesRemainAuthoredFacts(bool horizontal) {
        var chart = Model(true, horizontal, false, 800); chart.Series[0].UseSecondaryValueAxis();
        var primary = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        var secondary = horizontal ? chart.Options.SecondaryXAxis : chart.Options.SecondaryYAxis;
        secondary.WithBounds(-80, 160).WithReversal(true); primary.WithBounds(-80, 160);
        var category = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        category.RuleCrossAt = 0; secondary.CrossAt = 3;
        var imported = chart.ToVisualArtifact().ToInterchangeEnvelope().ToWaterfallChart();
        Assert.Equal(ChartAxisSide.Secondary, imported.Series[0].ValueAxis);
        Assert.Equal(0, (horizontal ? imported.Options.YAxis : imported.Options.XAxis).RuleCrossAt);
        Assert.Equal(3, (horizontal ? imported.Options.SecondaryXAxis : imported.Options.SecondaryYAxis).CrossAt);
        Assert.Equal(chart.ToSvg(), imported.ToSvg()); Assert.Equal(chart.ToPng(), imported.ToPng());
    }

    [Fact]
    public void PreparedHandoffAndEmptyAuthoringModesRemainDetachedAndEditable() {
        foreach (var typed in new[] { false, true }) {
            var empty = typed ? Chart.Create().AddWaterfall("Empty", Array.Empty<ChartWaterfallItem>()) : Chart.Create().AddWaterfall("Empty", Array.Empty<ChartPoint>());
            var imported = empty.ToVisualArtifact("empty").ToInterchangeEnvelope().ToWaterfallChart();
            Assert.Equal(typed, imported.Series[0].IsTypedWaterfall); Assert.Empty(ChartWaterfallSteps.Create(imported.Series[0]));
            Assert.NotEmpty(imported.ToSvg()); Assert.NotEmpty(imported.ToPng());
        }
        var chart = Model(true, false, false, 800); var envelope = chart.ToVisualArtifact("prepared").ToInterchangeEnvelope();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var artifact = prepared.ToArtifact("prepared", VisualArtifactKind.Chart, envelope);
        var snapshot = artifact.ToInterchangeJson(); envelope.Waterfall!.Items.Clear(); chart.Series[0].ReplaceWaterfallItems(Array.Empty<ChartWaterfallItem>());
        Assert.Equal(snapshot, artifact.ToInterchangeJson()); Assert.Equal(7, artifact.ToInterchangeEnvelope().ToWaterfallChart().Series[0].WaterfallItems.Count);
    }

    internal static Chart Model(bool typed = true, bool horizontal = false, bool dark = false, int width = 800) {
        var series = typed ? ChartSeries.CreateWaterfall(new ChartSeriesId("movement"), "Movement", new[] {
            ChartWaterfallItem.OpeningBalance(1, 120), ChartWaterfallItem.Delta(2, -30), ChartWaterfallItem.Subtotal(3),
            ChartWaterfallItem.Delta(4, -130), ChartWaterfallItem.Subtotal(5), ChartWaterfallItem.Delta(6, 70), ChartWaterfallItem.Total(7)
        }) : new ChartSeries(new ChartSeriesId("movement"), "Movement", ChartSeriesKind.Waterfall,
            new[] { new ChartPoint(1, 60), new ChartPoint(2, -15), new ChartPoint(4, 25) });
        series.WithObservationId(typed ? 3 : 1, new ChartObservationId("authored-change"));
        if (typed) series.WithObservationId(4, new ChartObservationId("checkpoint"));
        series.WithPointColor(typed ? 4 : 1, new ChartColor(137, 51, 177, 187)).WithPointFillPattern(0, ChartFillPattern.Crosshatch)
            .WithPointLabel(0, "Opening <balance>").ConfigurePointDataLabelStyle(0, style => { style.FontSize = 13; style.Italic = true; })
            .ConfigureDataLabelStyle(style => style.FontWeight = "600");
        var chart = Chart.Create().WithSize(width, 440).WithTitle("Running balance").WithSubtitle("Portable authoring")
            .WithPadding(18, 14, 18, 12).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture)).WithDataLabels().WithLegend(false).AddSeries(series)
            .ConfigureWaterfall(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
        chart.Options.PinStateColorsInForcedColors = true;
        chart.Options.BarStyle = ChartBarStyle.SegmentedCapsule; chart.Options.BarVisualStyle.CapThickness = 5;
        chart.Options.GridLineStyle.Dash = 3; chart.Options.GridLineStyle.Gap = 2;
        chart.ConfigureTitleStyle(style => style.FontSize = 18); chart.ConfigureTickLabelStyle(style => style.FontSize = 11);
        chart.ConfigureLabels(labels => { labels.Change = "Delta"; labels.Subtotal = "Stage sum"; labels.Total = "Final balance"; });
        var axis = horizontal ? chart.Options.XAxis : chart.Options.YAxis; axis.WithBounds(-80, 160).WithReversal(dark);
        chart.Options.XAxis.Labels.AddRange(typed ? new[] { new ChartAxisLabel(1,"Open"), new ChartAxisLabel(2,"Adjust"), new ChartAxisLabel(3,"Stage"),
            new ChartAxisLabel(4,"Costs"), new ChartAxisLabel(5,"Cost sum"), new ChartAxisLabel(6,"Recovery"), new ChartAxisLabel(7,"Balance") }
            : new[] { new ChartAxisLabel(1,"Opening"), new ChartAxisLabel(2,"Costs"), new ChartAxisLabel(4,"Recovery"), new ChartAxisLabel(5,"Balance") });
        return chart;
    }

    private static void Save(string name, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, name); File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png);
    }
}
