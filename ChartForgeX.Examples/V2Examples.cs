using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Deterministic chart and diagram models with shared-context static exports.</summary>
public static partial class V2Examples {
    private const string ProofFont = ExampleTypography.FontFamily;
    private static readonly string[] Variants = { "wide", "compact", "long-title", "wrapped-legend", "surface", "empty", "zero", "missing", "dense", "explicit-status", "explicit-series" };

    /// <summary>Writes the complete review matrix or the selected fixtures for the public catalog.</summary>
    public static void Write(string output, bool curated = false) {
        Directory.CreateDirectory(output);
        ExampleTypography.WriteAssets(output);
        TopologyHierarchyExamples.Write(output);
        var artifacts = new List<ProofArtifact>();
        foreach (var family in new[] { "cartesian", "donut" }) {
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                foreach (var variant in curated ? new[] { "wide", "compact", "explicit-status" } : Variants) {
                    var width = variant == "compact" ? 360 : 800;
                    var height = variant == "compact" ? 360 : 440;
                    var title = Title(family, variant);
                    var chart = Create(family, variant, mode);
                    var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)),
                        VisualTheme.Graphite(), mode, new VisualFrame(title, Subtitle(family, variant), showSurface: variant == "surface"), FontSpec.FromFamily(ProofFont));
                    var prepared = chart.Prepare(context);
                    var id = family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), ExampleArtifactWriter.Svg(prepared, id));
                    File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                    WriteThumbnail(output, chart, id, title, family, mode, legend: true);
                    WriteRecipe(output, id, ExampleRecipes.Named(family == "cartesian" ? "comparison" : "findings"), variant, title, Subtitle(family, variant), mode, width, height, null, surface: variant == "surface");
                    var interactive = variant == "wide";
                    if (interactive) WriteInteractive(output, Create(family, variant, mode), id, title, Subtitle(family, variant), mode, width, height, null);
                    artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
                        prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count,
                        DiagnosticMessages: prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray(), Compact: variant == "compact", HtmlAdapter: interactive));
                }
            }
        }
        if (!curated) WriteRadialLabels(output, artifacts);
        WriteDiagrams(output, artifacts);
        WriteFamilies(output, artifacts, curated);
        WriteHistograms(output, artifacts);
        WriteViewports(output, artifacts);
        WriteXZoom(output, artifacts);
        WriteLayeredData(output, artifacts);
        WriteNativeDepth(output, artifacts);
        WriteTableContentDepth(output, artifacts);
        WriteSequenceLayoutTypography(output, artifacts);
        WriteSequenceParticipantViews(output, artifacts);
        WriteSequenceMessagePresentation(output, artifacts);
        WriteSequenceSelfRoutes(output, artifacts);
        WriteSequenceNavigation(output, artifacts);
        WriteSequenceSourceDefaults(output, artifacts);
        WriteSequencePresetAppearance(output,artifacts);
        WriteSequenceSurfaceShadows(output, artifacts);
        WriteOwnedHtmlFonts(output, artifacts);
        ServerRenderedChartExamples.Write(output);
        WriteEmbedding(output, artifacts);
        EnhanceExamples.Write(output);
        WriteCatalog(output, artifacts, curated);
        ValidateOutput(output);
    }

    /// <summary>Creates comparison data from the same compiled recipe included in its standalone download.</summary>
    public static Chart Create(string family, string variant, VisualThemeMode mode = VisualThemeMode.Light) {
        if (family != "cartesian" && family != "donut") throw new ArgumentOutOfRangeException(nameof(family));
        if (!Variants.Contains(variant, StringComparer.Ordinal)) throw new ArgumentOutOfRangeException(nameof(variant));
        return (Chart)ExampleRecipes.Named(family == "cartesian" ? "comparison" : "findings").Create(variant, mode);
    }
    private static string Title(string family, string variant) => variant == "long-title"
        ? "Assessment results across monitored services and locations over the reporting period"
        : family == "donut" ? "Findings by category" : "Checks over the reporting period";

    private static string Subtitle(string family, string variant) => variant switch {
        "empty" => "No observations received", "zero" => "All observations are zero",
        "missing" => family == "donut" ? "Certificates and network findings were not reported" : "Days three and four have no observation",
        "dense" => "Twenty-four categories at a fixed size",
        _ => family == "donut" ? "Open findings from the October assessment" : "Observed checks against the forecast and available capacity"
    };

    private static string Literal(string value) => System.Text.Json.JsonSerializer.Serialize(value);
}
