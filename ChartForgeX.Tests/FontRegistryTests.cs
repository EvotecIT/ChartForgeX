using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Registered font files are named by family in chart themes, SVG text, VisualCanvas themes, and
/// <see cref="FontSpec"/> text. The repository ships no font, so the cases register an installed
/// Windows font file under a family name no host has, and return early where that file is absent.
/// The registry is process-wide, so these cases run alone.
/// </summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class FontRegistryTests : IDisposable {
    private const string Family = "CFX Registered Serif";

    public void Dispose() => FontRegistry.Clear();

    [Fact]
    public void RegisteredFamilyDrivesChartThemesAndTheirBoldFace() {
        if (!TryGetGeorgia(out var regular, out var bold)) return;
        Assert.Null(InstalledFontCatalog.Find(Family, 400, false));
        var before = BarChart(Family + ", sans-serif").ToPng();

        FontRegistry.Register(Family, regular);
        FontRegistry.Register(Family, bold, weight: 700);
        Assert.Contains(Family, FontRegistry.Families);
        var chart = BarChart(Family + ", sans-serif");
        Assert.Equal(Path.GetFullPath(regular), chart.GetPngFontInfo().ResolvedPath);
        Assert.NotEqual(before, chart.ToPng());
        Assert.Same(TrueTypeFont.TryLoadFromPath(Path.GetFullPath(bold)), TypographyFontResolver.ResolveThemeBoldFont(Family));
        Assert.Equal(BarChart("Georgia, serif").ToPng(), chart.ToPng());
    }

    [Fact]
    public void RegisteredFamilyDrivesSvgTextCanvasAndComposition() {
        if (!TryGetGeorgia(out var regular, out var bold)) return;
        FontRegistry.Register(Family, regular);
        FontRegistry.Register(Family, bold, weight: 700);

        Assert.Equal(Svg("Georgia", 700), Svg(Family, 700));
        Assert.Equal(Svg("Georgia", 400), Svg(Family, 400));
        Assert.NotEqual(Svg(Family, 400), Svg(Family, 700));

        var canvas = new PngVisualCanvasRenderer().RenderImage(Canvas(Family));
        Assert.Equal(new PngVisualCanvasRenderer().RenderImage(Canvas("Georgia")).Pixels, canvas.Pixels);

        var style = TextStyle.Create(24, ChartColors.White);
        style.Font = FontSpec.FromFamily(Family);
        style.Font.Weight = 700;
        Assert.Same(TrueTypeFont.TryLoadFromPath(Path.GetFullPath(bold)), TypographyFontResolver.ResolveFace(style.Font).Font);
    }

    [Fact]
    public void RegisteredFacesTakePrecedenceOverInstalledOnesAndGenericNames() {
        if (!TryGetGeorgia(out var regular, out _) || InstalledFontCatalog.Find("Arial", 400, false) == null) return;
        FontRegistry.Register("Arial", regular);
        Assert.Equal(Path.GetFullPath(regular), TypographyFontResolver.ResolveFace("Arial, sans-serif", 400, false).Path);
        // Only the registered faces of a registered family are matched: bold is synthesized on it.
        Assert.True(TypographyFontResolver.ResolveFace("Arial", 700, false).SynthesizeBold);

        FontRegistry.Register("sans-serif", regular);
        Assert.Equal(Path.GetFullPath(regular), TypographyFontResolver.ResolveFace("'No Such Family 4E1D', sans-serif", 400, false).Path);
        Assert.Equal(Svg("Georgia", 400), Render("<text x=\"8\" y=\"40\" font-size=\"28\">Hamburgefonstiv</text>"));

        FontRegistry.Clear();
        Assert.NotEqual(Path.GetFullPath(regular), TypographyFontResolver.ResolveFace("Arial", 400, false).Path);
        Assert.Empty(FontRegistry.Families);
    }

    [Fact]
    public void FilesAndDirectoriesRegisterUnderTheirOwnNames() {
        if (!TryGetGeorgia(out var regular, out var bold)) return;
        var folder = Path.Combine(Path.GetTempPath(), "cfx-font-registry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "nested"));
        try {
            File.Copy(bold, Path.Combine(folder, "nested", "bold.ttf"));
            File.WriteAllText(Path.Combine(folder, "broken.ttf"), "not a font");
            Assert.Equal(1, FontRegistry.RegisterDirectory(folder));
            Assert.Equal(Path.Combine(folder, "nested", "bold.ttf"), TypographyFontResolver.ResolveFace("Georgia", 700, false).Path);
            Assert.Equal(1, FontRegistry.RegisterFile(regular));
            Assert.Equal(Path.GetFullPath(regular), TypographyFontResolver.ResolveFace("Georgia", 400, false).Path);
            FontRegistry.Clear();
        } finally {
            Directory.Delete(folder, true);
        }

        Assert.Throws<FileNotFoundException>(() => FontRegistry.Register(Family, Path.Combine(folder, "missing.ttf")));
        Assert.Throws<ArgumentException>(() => FontRegistry.Register(" ", regular));
        Assert.Throws<ArgumentOutOfRangeException>(() => FontRegistry.Register(Family, regular, weight: 0));
    }

    [Fact]
    public void RegistrationIsSafeWhileTextResolves() {
        if (!TryGetGeorgia(out var regular, out var bold)) return;
        var errors = 0;
        Parallel.For(0, 64, index => {
            try {
                if (index % 4 == 0) FontRegistry.Register(Family, index % 8 == 0 ? regular : bold, index % 8 == 0 ? 400 : 700);
                var face = TypographyFontResolver.ResolveFace(Family + ", sans-serif", 700, false);
                if (face.Font == null) Interlocked.Increment(ref errors);
            } catch (Exception) {
                Interlocked.Increment(ref errors);
            }
        });
        Assert.Equal(0, errors);
        Assert.Same(TrueTypeFont.TryLoadFromPath(Path.GetFullPath(bold)), TypographyFontResolver.ResolveFace(Family, 700, false).Font);
    }

    private static bool TryGetGeorgia(out string regular, out string bold) {
        var fonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        regular = Path.Combine(fonts, "georgia.ttf");
        bold = Path.Combine(fonts, "georgiab.ttf");
        return File.Exists(regular) && File.Exists(bold);
    }

    private static Chart BarChart(string family) => Chart.Create()
        .WithTitle("Readership")
        .WithTheme(ChartTheme.ReportDark().WithFontFamily(family))
        .WithSize(360, 220)
        .WithXLabels("A", "B", "C")
        .AddBar("Issues", ChartPoints.FromValues(12, 18, 9));

    private static VisualCanvas Canvas(string family) =>
        VisualCanvas.Create(400, 80).WithBackdrop(VisualCanvasBackdropStyle.Transparent).WithTheme(new VisualCanvasTheme { FontFamily = family })
            .AddText(10, 10, 380, "Registered canvas text", 24, ChartColors.White, emphasized: true);

    private static byte[] Svg(string family, int weight) =>
        Render("<text x=\"8\" y=\"40\" font-size=\"28\" font-family=\"" + family + "\" font-weight=\"" + weight.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\">Hamburgefonstiv</text>");

    private static byte[] Render(string body) =>
        SvgRasterizer.ToImage("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"320\" height=\"56\">" + body + "</svg>").Pixels;
}

[CollectionDefinition(nameof(FontRegistryCollection), DisableParallelization = true)]
public sealed class FontRegistryCollection {
}
