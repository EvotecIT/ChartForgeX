using System.Text;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Before-product controls use only incumbent public preparation APIs.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeSeriesVisibilityDefaultControls {
    public static IEnumerable<object[]> Cases() {
        foreach (var family in new[] { "line", "bar", "mixed", "mixed-labels" }) {
            yield return new object[] { family, 390, true }; yield return new object[] { family, 760, false };
        }
    }
    [Theory, MemberData(nameof(Cases))]
    public void OrdinaryFullCatalogNativeAndHtmlControlsAreDetachedAndRepeatable(string family, int width, bool dark) {
        var model = Model(family, width, dark); var context = VisualExportRequest.ForChart(model).Context;
        var native = model.Prepare(context); var second = model.Prepare(context);
        var html = new HtmlInteractiveChartRenderer().PrepareSection(model).Html;
        Save("ordinary-" + family + "-" + width + "-" + dark, native, html, new { family, width, dark,
            series = model.Series.Select(series => series.Id.Value).ToArray(), font = BrowserProofFonts.Family });
        Assert.Equal(native.ToSvg(), second.ToSvg()); Assert.Equal(native.ToPng(), second.ToPng());
        Assert.Equal(model.Series.Count, native.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role == "series"));
        Assert.Equal(html, new HtmlInteractiveChartRenderer().PrepareSection(model).Html);
    }
    internal static Chart Model(string family, int width = 760, bool dark = false) {
        var chart = Chart.Create().WithTitle("Native retained visibility").WithSize(width, 400)
            .WithXLabels("Opened", "Reviewed", "Released").WithDataLabels(family.EndsWith("labels", StringComparison.Ordinal));
        chart.Options.Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);
        chart.Options.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        ChartSeries Add(string id, bool bar, double[] values) {
            var points = ChartPoints.FromValues(values);
            return (bar ? ChartSeries.CreateBar(new ChartSeriesId(id), id, points) : ChartSeries.CreateLine(new ChartSeriesId(id), id, points))
                .WithObservationIds(new[] { new ChartObservationId("opened"), new ChartObservationId("reviewed"), new ChartObservationId("released") });
        }
        if (family == "line") chart.AddSeries(Add("observed", false, new[] { 18d, 70, 44 }));
        else if (family == "bar") chart.AddSeries(Add("observed", true, new[] { 18d, 70, 44 }));
        else chart.AddSeries(Add("observed", true, new[] { 18d, 70, 44 })).AddSeries(Add("planned", false, new[] { 40d, 52, 76 }));
        return chart;
    }
    internal static void Save(string name, PreparedVisual native, string? html = null, object? source = null) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } directory) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Native visibility proof requires an absolute capture directory.");
        var prefix = Path.Combine(directory, "native-series-visibility", name); Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        File.WriteAllText(prefix + ".svg", native.ToSvg(), new UTF8Encoding(false));
        File.WriteAllText(prefix + ".fixed.svg", native.ToSvg("native-visibility-control"), new UTF8Encoding(false));
        File.WriteAllBytes(prefix + ".png", native.ToPng());
        if (html != null) File.WriteAllText(prefix + ".html", html, new UTF8Encoding(false));
        if (source != null) File.WriteAllText(prefix + ".source.json", JsonSerializer.Serialize(source, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
    }
}
