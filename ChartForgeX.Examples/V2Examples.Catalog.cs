using System.Net;
using System.Text;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Themes;

public static partial class V2Examples {
    private sealed record ProofArtifact(string Id, string Family, string Title, string Variant, string Theme,
        int Width, int Height, string[] Diagnostics, int SemanticRegions, string[]? SeriesKinds = null, string[]? DiagnosticMessages = null, bool Compact = false);

    private static string Escape(string value) => WebUtility.HtmlEncode(value);

    private static void WriteCatalog(string output, IReadOnlyList<ProofArtifact> artifacts, bool curated) {
        var primaryIds = PrimaryArtifacts(artifacts).Select(artifact => artifact.Id).ToHashSet(StringComparer.Ordinal);
        var manifest = new {
            schemaVersion = 1,
            matrixScope = curated ? "selected" : "full",
            pipeline = "model-to-prepared-scene-to-svg-or-raster",
            assets = CatalogAssets(artifacts),
            chartKinds = artifacts.SelectMany(ArtifactKinds).Distinct().OrderBy(kind => kind).ToArray(),
            groups = CatalogGroups.Select(group => new { id = group.Id, label = group.Label }),
            fonts = new[] { "fonts/Carlito-Regular.ttf", "fonts/Carlito-Bold.ttf" }.Select(file => new {
                file, sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(output, file))))
            }),
            artifacts = artifacts.Select(artifact => new {
                id = artifact.Id, family = artifact.Family, familyLabel = FamilyLabel(artifact.Family),
                group = GroupFor(artifact.Family), title = artifact.Title, variant = artifact.Variant,
                primary = primaryIds.Contains(artifact.Id), seriesKinds = ArtifactKinds(artifact),
                theme = artifact.Theme, width = artifact.Width, height = artifact.Height,
                naturalAspect = (double)artifact.Width / artifact.Height,
                compact = artifact.Compact, source = artifact.Id + ".csharp.txt",
                svg = artifact.Id + ".svg", png = artifact.Id + ".png", html = artifact.Id + ".html",
                thumbnail = artifact.Id + ".thumbnail.svg", thumbnailPng = artifact.Id + ".thumbnail.png",
                thumbnailWidth = ThumbnailWidth, thumbnailHeight = ThumbnailHeight,
                supportedOutputs = new[] { "svg", "png", "html" },
                diagnostics = artifact.Diagnostics, diagnosticMessages = artifact.DiagnosticMessages ?? Array.Empty<string>(), semanticRegions = artifact.SemanticRegions
            })
        };
        ExampleArtifactWriter.WriteText(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        WriteCatalogAssets(output);
        foreach (var artifact in artifacts) WritePage(output, artifact, artifacts);
        var primary = PrimaryArtifacts(artifacts).Where(artifact => artifact.Theme == "light").ToArray();
        var page = new StringBuilder();
        StartPage(page, "ChartForgeX gallery", "light", "catalog");
        page.Append("<header class=\"gallery-header\"><div class=\"header-top\"><a class=\"brand\" href=\"catalog.html\">ChartForgeX</a>");
        AppendThemeControl(page);
        page.Append("</div><p class=\"eyebrow\">CHARTS AND DIAGRAMS</p><h1>Find the right chart.</h1><p class=\"intro\">Charts and diagrams in one consistent theme. Open an example for SVG, PNG and C# source.</p></header>");
        page.Append("<main id=\"main-content\" class=\"gallery-main\"><section class=\"catalog-tools\" aria-label=\"Explore the gallery\"><label class=\"search-label\" for=\"gallery-search\">Find a chart or diagram<input id=\"gallery-search\" type=\"search\" placeholder=\"Try line, heatmap, schedule…\" autocomplete=\"off\" data-gallery-search></label><nav class=\"family-nav\" aria-label=\"Chart families\"><button type=\"button\" data-gallery-group=\"all\" aria-pressed=\"true\">All examples</button>");
        foreach (var group in CatalogGroups.Where(group => primary.Any(artifact => GroupFor(artifact.Family) == group.Id)))
            page.Append("<button type=\"button\" data-gallery-group=\"").Append(group.Id).Append("\" aria-pressed=\"false\">").Append(Escape(group.Label)).Append("</button>");
        page.Append("</nav><div class=\"catalog-summary\"><p data-gallery-count aria-live=\"polite\">").Append(primary.Length).Append(" examples</p><button type=\"button\" class=\"text-button\" data-gallery-reset>Reset filters</button></div></section><div class=\"grid\" data-gallery-grid>");
        foreach (var artifact in primary.OrderBy(artifact => GroupOrder(artifact.Family)).ThenBy(artifact => FamilyLabel(artifact.Family), StringComparer.Ordinal))
            AppendTile(page, output, artifact, artifacts);
        page.Append("</div><div class=\"empty-results\" data-gallery-empty hidden><h2>No matching examples</h2><p>Try a chart type, a topic such as capacity, or another family.</p></div></main><footer><span>One theme. Every chart and diagram.</span><a href=\"manifest.json\">Export manifest</a></footer>");
        EndPage(page);
        ExampleArtifactWriter.WriteText(Path.Combine(output, "catalog.html"), page.ToString());
    }

    private static IEnumerable<ProofArtifact> PrimaryArtifacts(IReadOnlyList<ProofArtifact> artifacts) =>
        artifacts.GroupBy(artifact => (artifact.Family, artifact.Theme)).Select(group => group
            .OrderBy(artifact => artifact.Id.StartsWith("family-", StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(artifact => artifact.Variant switch { "expanded" => 0, "wide" => 1, "feasibility" => 2, _ => 3 })
            .ThenBy(artifact => artifact.Id, StringComparer.Ordinal).First());

    private static string ExampleKey(ProofArtifact artifact) => artifact.Id.Substring(0, artifact.Id.Length - artifact.Theme.Length - 1);

    private static string[] CatalogAssets(IReadOnlyList<ProofArtifact> artifacts) =>
        new[] { "catalog.html", "manifest.json", "gallery.css", "gallery.js", "fonts/Carlito-Regular.ttf", "fonts/Carlito-Bold.ttf", "fonts/OFL.txt" }
            .Concat(artifacts.SelectMany(artifact => new[] { ".html", ".svg", ".png", ".csharp.txt", ".thumbnail.svg", ".thumbnail.png" }.Select(extension => artifact.Id + extension)))
            .Distinct(StringComparer.Ordinal).OrderBy(file => file, StringComparer.Ordinal).ToArray();

    private static IEnumerable<ProofArtifact> PairedArtifacts(IReadOnlyList<ProofArtifact> artifacts, ProofArtifact selected) =>
        artifacts.Where(artifact => ExampleKey(artifact) == ExampleKey(selected));

    private static string[] ArtifactKinds(ProofArtifact artifact) => artifact.SeriesKinds ?? artifact.Family switch {
        "cartesian" => new[] { nameof(ChartSeriesKind.Line), nameof(ChartSeriesKind.Area), nameof(ChartSeriesKind.Bar) },
        "donut" => new[] { nameof(ChartSeriesKind.Donut) }, "pie" => new[] { nameof(ChartSeriesKind.Pie) }, _ => Array.Empty<string>()
    };
}
