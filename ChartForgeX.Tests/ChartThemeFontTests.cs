using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Chart, grid, topology, and visual block PNG output resolve theme font stacks to installed families
/// and draw emphasized text with the family's real bold face, measured with the face that draws it.
/// Cases that need a particular installed family return early on hosts without it.
/// </summary>
public sealed class ChartThemeFontTests {
    [Fact]
    public void ChartPngDrawsTheThemeFamilyNotItsCategoryFallback() {
        if (!HasFace("Segoe UI", 400) || !HasFace("Arial", 400)) return;
        var segoe = BarChart("Segoe UI, sans-serif");
        var arial = BarChart("Arial, sans-serif");
        Assert.NotEqual(segoe.ToPng(), arial.ToPng());
        var info = segoe.GetPngFontInfo();
        Assert.Equal(PngFontSource.Automatic, info.Source);
        Assert.Equal(InstalledFontCatalog.Find("Segoe UI", 400, false)!.Path, info.ResolvedPath);
    }

    [Fact]
    public void EmphasizedChartTextUsesTheRealBoldFace() {
        if (!HasFace("Georgia", 400) || !HasFace("Georgia", 700)) return;
        var chart = BarChart("Georgia, serif");
        var bold = TypographyFontResolver.ResolveThemeBoldFont("Georgia, serif")!;
        Assert.NotNull(bold);
        var regular = TypographyFontResolver.ResolveThemeFont("Georgia, serif")!;
        Assert.NotSame(regular, bold);

        // Outside a render the regular face is emboldened; inside one, the paired bold face is used.
        var synthesized = RgbaCanvas.MeasureTextEmphasizedWidth("Readership", 20, regular);
        Assert.True(synthesized > regular.Measure("Readership", 20));
        using (RgbaCanvas.OpenEmphasisScope()) {
            var scoped = TypographyFontResolver.ResolveThemeFont("Georgia, serif");
            Assert.NotSame(regular, scoped);
            Assert.Equal(bold.Measure("Readership", 20), RgbaCanvas.MeasureTextEmphasizedWidth("Readership", 20, scoped), 6);
            var viaEmphasis = new RgbaCanvas(200, 40, 1, scoped, 1, useDefaultOutlineFont: false);
            viaEmphasis.DrawTextEmphasized(4, 4, "Readership", ChartColors.White, 20, scoped, italic: false);
            var direct = new RgbaCanvas(200, 40, 1, bold, 1, useDefaultOutlineFont: false);
            direct.DrawText(4, 4, "Readership", ChartColors.White, 20, bold, italic: false);
            Assert.Equal(direct.Pixels, viaEmphasis.Pixels);
        }

        // The scope closes with the render, and an explicit font file keeps its synthesized emphasis.
        Assert.Equal(synthesized, RgbaCanvas.MeasureTextEmphasizedWidth("Readership", 20, regular), 6);
        Assert.NotEmpty(chart.ToPng());
        Assert.Equal(synthesized, RgbaCanvas.MeasureTextEmphasizedWidth("Readership", 20, regular), 6);
    }

    [Fact]
    public void InstalledFontMeasurementUsesTheFacesTheRenderersDraw() {
        const string family = "Segoe UI, sans-serif";
        var installed = new TextMeasurementContext(family, TextMeasurementMode.InstalledFonts);
        var regular = TypographyFontResolver.ResolveThemeFont(family);
        var bold = TypographyFontResolver.ResolveThemeBoldFont(family);
        if (regular == null) return;
        Assert.Equal(regular.Measure("Domain controller", 14), installed.Measure("Domain controller", 14, bold: false), 6);
        if (bold != null) Assert.Equal(bold.Measure("Domain controller", 14), installed.Measure("Domain controller", 14, bold: true), 6);

        // The portable estimate never looks at host fonts.
        var portable = new TextMeasurementContext(family, TextMeasurementMode.PortableEstimate);
        Assert.Equal("Domain controller".Length * 14 * 0.62, portable.Measure("Domain controller", 14, bold: true), 6);
    }

    [Fact]
    public void GridAndBlockPngsFollowTheThemeFamily() {
        if (!HasFace("Segoe UI", 400) || !HasFace("Georgia", 400)) return;
        var sans = ChartGrid.Create().WithTitle("Operations").WithTheme(ChartTheme.ReportDark().WithFontFamily("Segoe UI")).Add(BarChart("Segoe UI"));
        var serif = ChartGrid.Create().WithTitle("Operations").WithTheme(ChartTheme.ReportDark().WithFontFamily("Georgia")).Add(BarChart("Segoe UI"));
        Assert.NotEqual(sans.ToPng(), serif.ToPng());
    }

    private static Chart BarChart(string family) => Chart.Create()
        .WithTitle("Readership")
        .WithTheme(ChartTheme.ReportDark().WithFontFamily(family))
        .WithSize(360, 220)
        .WithXLabels("A", "B", "C")
        .AddBar("Issues", ChartPoints.FromValues(12, 18, 9));

    private static bool HasFace(string family, int weight) => InstalledFontCatalog.Find(family, weight, false)?.Weight == weight;
}
