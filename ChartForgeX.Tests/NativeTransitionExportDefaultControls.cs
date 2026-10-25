using System.Text;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Current APIs only: capture exact HTML and both native drawings before the export-facts source change.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeTransitionExportDefaultControls {
    public static IEnumerable<object[]> Cases() {
        foreach (var export in new[] { false, true }) {
            yield return new object[] { "native-export-scope", 760, false, export };
            yield return new object[] { "Wyniki źródła:42", 390, true, export };
        }
    }
    [Theory, MemberData(nameof(Cases))]
    public void ExistingExportModesAndHashedAdaptiveScopesRemainByteIdentical(string scope, int width, bool dark, bool export) {
        var section = Section(scope, width, dark, export); Save(section, width + "-" + dark + "-" + export, new { scope, width, dark, export });
        Assert.Equal(section.Html, Section(scope, width, dark, export).Html); Assert.NotNull(section.NarrowPreparedVisual);
        Assert.DoesNotContain("<script", section.Html, StringComparison.OrdinalIgnoreCase);
    }
    internal static HtmlChartSection Section(string scope, int width, bool dark, bool export) {
        var chart = Chart.Create().WithTitle("Captured native export facts").WithSize(width, 400).WithDataLabels()
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("reviewed"), "Reviewed", ChartPoints.FromValues(18, 70, 44)));
        chart.Options.Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);
        chart.Options.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        return new HtmlInteractiveChartRenderer().PrepareSection(chart, configure: options => {
            options.IdScope = scope; options.Interaction.ChartId = "native-export-facts";
            if (export) options.Interaction.Enable(ChartInteractionFeatures.Export); else options.Interaction.Disable(ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
        });
    }
    internal static void Save(HtmlChartSection section, string name, object source) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Export-facts controls require an absolute capture directory.");
        var prefix = Path.Combine(output, "native-transition-export-facts", name); Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        File.WriteAllText(prefix + ".html", section.Html, new UTF8Encoding(false));
        File.WriteAllText(prefix + ".wide.svg", section.PreparedVisual.ToSvg(), new UTF8Encoding(false));
        File.WriteAllText(prefix + ".wide.fixed.svg", section.PreparedVisual.ToSvg("export-facts-control"), new UTF8Encoding(false));
        File.WriteAllBytes(prefix + ".wide.png", section.PreparedVisual.ToPng());
        File.WriteAllText(prefix + ".narrow.svg", section.NarrowPreparedVisual!.ToSvg(), new UTF8Encoding(false));
        File.WriteAllText(prefix + ".narrow.fixed.svg", section.NarrowPreparedVisual.ToSvg("export-facts-control-narrow"), new UTF8Encoding(false));
        File.WriteAllBytes(prefix + ".narrow.png", section.NarrowPreparedVisual.ToPng());
        File.WriteAllText(prefix + ".source.json", JsonSerializer.Serialize(source), new UTF8Encoding(false));
    }
}
