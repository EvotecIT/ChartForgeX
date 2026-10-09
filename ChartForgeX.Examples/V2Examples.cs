using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Deterministic chart and diagram models with shared-context static exports.</summary>
public static partial class V2Examples {
    private const string ProofFont = "CFX Proof Carlito";
    private static readonly string[] Variants = { "wide", "compact", "long-title", "wrapped-legend", "surface", "empty", "zero", "missing", "dense", "explicit-status", "explicit-series" };

    /// <summary>Writes the complete review matrix or the selected fixtures for the public catalog.</summary>
    public static void Write(string output, bool curated = false) {
        Directory.CreateDirectory(output);
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(ProofFont, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(ProofFont, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
        Directory.CreateDirectory(Path.Combine(output, "fonts"));
        foreach (var file in new[] { "Carlito-Regular.ttf", "Carlito-Bold.ttf", "OFL.txt" })
            File.Copy(Path.Combine(fonts, file), Path.Combine(output, "fonts", file), true);
        var artifacts = new List<ProofArtifact>();
        foreach (var family in new[] { "cartesian", "donut" }) {
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                foreach (var variant in curated ? new[] { "wide", "compact", "explicit-status" } : Variants) {
                    var width = variant == "compact" ? 360 : 800;
                    var height = variant == "compact" ? 360 : 440;
                    var title = Title(family, variant);
                    var chart = Create(family, variant, mode);
                    var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)),
                        VisualTheme.Graphite(), mode, new VisualFrame(title, Subtitle(variant), showSurface: variant == "surface"), FontSpec.FromFamily(ProofFont));
                    var prepared = chart.Prepare(context);
                    var id = family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
                    File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                    WriteThumbnail(output, chart, id, title, family, mode, legend: true);
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"), Snippet(family, variant, mode, width, height));
                    artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
                        prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count,
                        DiagnosticMessages: prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray()));
                }
            }
        }
        if (!curated) WriteRadialLabels(output, artifacts);
        WriteDiagrams(output, artifacts);
        WriteFamilies(output, artifacts, curated);
        WriteHistograms(output, artifacts);
        WriteCatalog(output, artifacts, curated);
        ValidateOutput(output);
    }

    /// <summary>Creates the source data; themes and frames are supplied separately to preparation.</summary>
    public static Chart Create(string family, string variant, VisualThemeMode mode = VisualThemeMode.Light) {
        if (family != "cartesian" && family != "donut") throw new ArgumentOutOfRangeException(nameof(family));
        if (!Variants.Contains(variant, StringComparer.Ordinal)) throw new ArgumentOutOfRangeException(nameof(variant));
        var count = variant == "empty" ? 0 : variant == "dense" ? 24 : 7;
        var chart = Chart.Create().WithXLabels(Enumerable.Range(1, count).Select(index => Category(family, variant, index)).ToArray());
        if (family == "donut") {
            chart.AddDonut("Findings", Points(count, variant, 0, false));
        } else {
            chart.AddLine(SeriesName(variant, 0), Points(count, variant, 0));
            chart.AddArea(SeriesName(variant, 1), Points(count, variant, 1));
            chart.AddBar(SeriesName(variant, 2), Points(count, variant, 2, false));
        }
        if (variant == "explicit-status" || variant == "explicit-series") {
            var colors = VisualTheme.Graphite().Resolve(mode);
            var selected = variant == "explicit-status"
                ? new[] { colors.Status.Pass.Fill, colors.Status.Medium.Fill, colors.Status.Critical.Fill }
                : colors.Palette.Take(3).ToArray();
            if (family == "donut") {
                for (var index = 0; index < chart.Series[0].Points.Count; index++)
                    chart.Series[0].WithPointColorRange(index, 1, selected[index % selected.Length]);
            } else {
                for (var index = 0; index < chart.Series.Count; index++) chart.Series[index].WithColor(selected[index]);
            }
        }
        return chart;
    }

    private static IEnumerable<ChartPoint> Points(int count, string variant, int series, bool allowBreaks = true) {
        for (var index = 0; index < count; index++) {
            if (variant == "missing" && (index == 2 || index == 3)) continue;
            var value = variant == "zero" ? 0 : 20 + series * 14 + (index * (series + 3) % 23);
            yield return new ChartPoint(index + 1, value, allowBreaks && variant == "missing" && index == 4);
        }
    }

    private static string Category(string family, string variant, int index) => variant == "wrapped-legend" && family == "donut"
        ? "Assessment category " + index + " requiring follow-up" : "Day " + index;

    private static string SeriesName(string variant, int index) => variant == "wrapped-legend"
        ? new[] { "Observed checks across all locations", "Expected coverage of monitored services", "Capacity available for the next assessment" }[index]
        : new[] { "Observed", "Expected", "Capacity" }[index];

    private static string Title(string family, string variant) => variant == "long-title"
        ? "Assessment results across monitored services and locations over the reporting period"
        : family == "donut" ? "Findings by category" : "Checks over the reporting period";

    private static string Subtitle(string variant) => variant switch {
        "empty" => "No observations received", "zero" => "All observations are zero", "missing" => "Days three and four have no observation",
        "dense" => "Twenty-four categories at a fixed size", _ => "One prepared scene for SVG and native PNG"
    };

    private static string Snippet(string family, string variant, VisualThemeMode mode, int width, int height) {
        var chart = Create(family, variant, mode);
        var lines = new List<string> { "using ChartForgeX.Core;", "using ChartForgeX.Primitives;", "using ChartForgeX.Rendering;", "using ChartForgeX.Themes;", "using ChartForgeX.Typography;", "", "// The generated gallery includes these licensed font fixtures beside the outputs.", "FontRegistry.Register(\"" + ProofFont + "\", \"fonts/Carlito-Regular.ttf\", 400);", "FontRegistry.Register(\"" + ProofFont + "\", \"fonts/Carlito-Bold.ttf\", 700);", "var chart = Chart.Create();" };
        if (chart.Options.XAxisLabels.Count > 0)
            lines.Add("chart.WithXLabels(" + string.Join(", ", chart.Options.XAxisLabels.Select(label => Literal(label.Text))) + ");");
        foreach (var series in chart.Series) {
            var method = series.Kind == ChartSeriesKind.Donut ? "AddDonut" : series.Kind == ChartSeriesKind.Area ? "AddArea" : series.Kind == ChartSeriesKind.Bar ? "AddBar" : "AddLine";
            var points = string.Join(", ", series.Points.Select(point => "new ChartPoint(" + point.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + point.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + (point.BreakBefore ? ", true" : "") + ")"));
            lines.Add("chart." + method + "(" + Literal(series.Name) + ", new ChartPoint[] { " + points + " });");
            var seriesIndex = chart.Series.IndexOf(series);
            if (series.Color.HasValue) lines.Add("chart.Series[" + seriesIndex + "].WithColor(ChartColor.FromHex(" + Literal(series.Color.Value.ToHex()) + "));");
            for (var index = 0; index < series.PointColors.Count; index++)
                if (series.PointColors[index].HasValue) lines.Add("chart.Series[" + seriesIndex + "].WithPointColorRange(" + index + ", 1, ChartColor.FromHex(" + Literal(series.PointColors[index]!.Value.ToHex()) + "));");
        }
        lines.Add("var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(" + width + ", " + height + ")), VisualTheme.Graphite(), VisualThemeMode." + mode + ",");
        lines.Add("    new VisualFrame(" + Literal(Title(family, variant)) + ", " + Literal(Subtitle(variant)) + ", showSurface: " + (variant == "surface" ? "true" : "false") + "), FontSpec.FromFamily(\"" + ProofFont + "\"));");
        lines.Add("var prepared = chart.Prepare(context);");
        lines.Add("System.IO.File.WriteAllText(\"chart.svg\", prepared.ToSvg());");
        lines.Add("System.IO.File.WriteAllBytes(\"chart.png\", prepared.ToPng());");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string Literal(string value) => System.Text.Json.JsonSerializer.Serialize(value);
}
