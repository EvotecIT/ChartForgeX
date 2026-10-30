using System.Globalization;
using System.Net;
using System.Text;
using CanopyX;
using CanopyX.Html;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>
/// Writes the linked-views example: four enhanced ChartForgeX charts of different families and a real CanopyX grid of the
/// same illustrative records, wired with one CfxLink.connect call per pair. CanopyX is used by this example only.
/// </summary>
public static class EnhanceExamples {
    // The entry point CanopyX chooses for this grid (row selection by pointer needs the reporting entry point).
    private static readonly CanopyRuntime GridRuntime = CanopyHtmlAssets.GetRuntime(Grid(EnhanceRecipe.Records()));

    /// <summary>Writes the page, its assets and its visual story into <c>enhance/</c> under the output folder.</summary>
    public static void Write(string output) {
        var directory = Path.Combine(output, "enhance");
        Directory.CreateDirectory(directory);
        HtmlInteractiveAssetFiles.WriteTo(directory, HtmlEnhanceAssets.Files());
        // The grid entry point and only the parts this grid loads (with the link part), under their content-hashed names.
        ExampleArtifactWriter.WriteText(Path.Combine(directory, CanopyHtmlAssets.GetScriptFileName(CanopyAssetNaming.ContentHash, GridRuntime)), CanopyHtmlAssets.GetScript(GridRuntime));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, CanopyHtmlAssets.GetStyleFileName(CanopyAssetNaming.ContentHash, GridRuntime)), CanopyHtmlAssets.GetStyle(GridRuntime));
        foreach (var part in GridParts()) {
            if (!string.IsNullOrEmpty(part.Script)) ExampleArtifactWriter.WriteText(Path.Combine(directory, part.HashedScriptFileName!), part.Script);
            if (!string.IsNullOrEmpty(part.Style)) ExampleArtifactWriter.WriteText(Path.Combine(directory, part.HashedStyleFileName!), part.Style);
        }
        foreach (var asset in new[] { "enhance-page.css", "enhance-page.js", "linked-views.js" })
            ExampleArtifactWriter.WriteText(Path.Combine(directory, asset), Asset(asset));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "index.html"), Page());
        WriteStory(directory);
    }

    /// <summary>The files the example page needs beside it, relative to the output folder.</summary>
    internal static IEnumerable<string> Files(string output) {
        var directory = Path.Combine(output, "enhance");
        return Directory.EnumerateFiles(directory).Select(file => "enhance/" + Path.GetFileName(file)).OrderBy(file => file, StringComparer.Ordinal);
    }

    internal static string Page() {
        var records = EnhanceRecipe.Records();
        var grid = Grid(records);
        var options = new CanopyHtmlOptions {
            Document = new CanopyHtmlDocument(), AssetMode = CanopyAssetMode.External, AssetBaseUrl = "./",
            Height = "460px", Chrome = CanopyChrome.Embedded, Theme = CanopyTheme.Auto
        };
        options.Parts.Add("link");
        var runtime = "<link rel=\"stylesheet\" href=\"" + CanopyHtmlAssets.GetStyleFileName(CanopyAssetNaming.ContentHash, GridRuntime) + "\">\n  "
            + "<script defer src=\"" + CanopyHtmlAssets.GetScriptFileName(CanopyAssetNaming.ContentHash, GridRuntime) + "\"></script>\n  "
            + HtmlEnhanceAssets.ReferenceMarkup(new HtmlAssetReferences("./")).TrimEnd();
        return Asset("EnhanceExamples.html")
            .Replace("{{TOKENS}}", Tokens(), StringComparison.Ordinal)
            .Replace("{{RUNTIME}}", runtime, StringComparison.Ordinal)
            .Replace("{{WEEK}}", Svg(EnhanceRecipe.Week(records), "week", 320, 260), StringComparison.Ordinal)
            .Replace("{{TREND}}", Svg(EnhanceRecipe.Trend(records), "trend", 320, 260), StringComparison.Ordinal)
            .Replace("{{HEATMAP}}", Svg(EnhanceRecipe.Heatmap(records), "heatmap", 320, 230), StringComparison.Ordinal)
            .Replace("{{SUPPORT}}", Svg(EnhanceRecipe.Support(records), "support", 320, 210, legend: false), StringComparison.Ordinal)
            .Replace("{{COUNT}}", records.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{GRID}}", grid.ToHtmlFragment(options), StringComparison.Ordinal)
            .Replace("{{HOST_CODE}}", WebUtility.HtmlEncode(Asset("linked-views.js").TrimEnd()), StringComparison.Ordinal);
    }

    /// <summary>The CanopyX grid of the records: row ids are the record keys the chart marks carry, columns are the fields they link by.</summary>
    internal static CanopyDataGridView Grid(IReadOnlyList<EnhanceRecipe.Record> records) {
        var grid = new CanopyDataGridView("records", "Change records") {
            Selection = CanopySelectionMode.Multiple, PageSize = 200, Noun = "records",
            // Row check boxes: a flat grid selects rows by pointer only with reporting selection controls.
            Reporting = new CanopyGridReporting { SelectionControls = true }
        };
        grid.Columns.Add(new CanopyColumn("title", "Record") { Sortable = true, Sizing = CanopyColumnSizing.Flex(1, 180, 480, preferredWidth: 240) });
        grid.Columns.Add(new CanopyColumn("day", "Date") { Kind = CanopyColumnKind.DateTime, Sortable = true, Sizing = CanopyColumnSizing.Preferred(110) });
        grid.Columns.Add(new CanopyColumn("kind", "Kind") { Sortable = true, Sizing = CanopyColumnSizing.Preferred(110) });
        grid.Columns.Add(new CanopyColumn("product", "Product") { Sortable = true, Sizing = CanopyColumnSizing.Preferred(170) });
        var severity = new CanopyColumn("severity", "Severity") { Sortable = true, Sizing = CanopyColumnSizing.Preferred(110) };
        severity.ValueTones["Critical"] = CanopyTone.Danger;
        severity.ValueTones["Important"] = CanopyTone.Warning;
        foreach (var name in EnhanceRecipe.Severities) severity.ValueOrder.Add(name);
        grid.Columns.Add(severity);
        grid.Columns.Add(new CanopyColumn("week", "Week") { Kind = CanopyColumnKind.DateTime, Hidden = true });
        grid.Query = new CanopyGridQuery(grid.Id);
        grid.Query.Sort.Add(new CanopyGridSort("day", true));
        foreach (var record in records.OrderByDescending(r => r.Day).ThenBy(r => r.Id, StringComparer.Ordinal)) {
            var row = new CanopyGridRow(record.Id);
            row.Cells["title"] = record.Title;
            row.Cells["day"] = new CanopyCell(record.Day) { Text = record.Day.ToString("ddd d MMM", CultureInfo.InvariantCulture) };
            row.Cells["kind"] = record.Kind;
            row.Cells["product"] = record.Product;
            row.Cells["severity"] = record.Severity;
            row.Cells["week"] = new CanopyCell(record.Week) { Text = record.Week.ToString("d MMM", CultureInfo.InvariantCulture) };
            grid.Rows.Add(row);
        }
        return grid;
    }

    private static IEnumerable<CanopyHtmlPart> GridParts() {
        var parts = new Dictionary<string, CanopyHtmlPart>(StringComparer.Ordinal);
        void Add(CanopyHtmlPart part) {
            if (!parts.TryAdd(part.Name, part)) return;
            foreach (var name in part.Requires) Add(CanopyHtmlAssets.GetPart(name));
        }
        foreach (var part in CanopyHtmlAssets.GetRequiredParts(Grid(EnhanceRecipe.Records()))) Add(part);
        Add(CanopyHtmlAssets.GetPart("link"));
        return parts.Values;
    }

    // The Graphite token colours as the page's --hfx-* roles in both schemes; the SVG names the same roles.
    private static readonly Dictionary<string, string> Roles = new(StringComparer.Ordinal) {
        ["surface.page"] = "--hfx-color-surface-page", ["surface.card"] = "--hfx-color-surface-card", ["surface.line"] = "--hfx-color-surface-line",
        ["text.primary"] = "--hfx-color-text", ["text.secondary"] = "--hfx-color-text-secondary", ["accent.base"] = "--hfx-color-accent"
    };

    private static string? RoleName(string path) {
        if (Roles.TryGetValue(path, out var role)) return role;
        if (path.StartsWith("series.", StringComparison.Ordinal) && !path.EndsWith(".ink", StringComparison.Ordinal)) return "--hfx-color-series-" + path.Substring(7);
        if (path.StartsWith("ramps.sequential.", StringComparison.Ordinal) && !path.EndsWith(".ink", StringComparison.Ordinal)) return "--hfx-ramp-sequential-" + path.Substring(17);
        var name = new StringBuilder("--cfx-");
        foreach (var ch in path) name.Append(ch == '.' ? "-" : char.IsUpper(ch) ? "-" + char.ToLowerInvariant(ch) : ch.ToString());
        return name.ToString();
    }

    private static SvgColorVariables Variables(VisualThemeMode mode) =>
        (mode == VisualThemeMode.Dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight()).ToSvgColorVariables(RoleName);

    private static string Tokens() {
        string Block(VisualThemeMode mode) => string.Join(" ", Variables(mode).Variables.GroupBy(v => v.Name).Select(v => v.Key + ": " + v.First().Color.ToCss() + ";"));
        var light = Block(VisualThemeMode.Light);
        var dark = Block(VisualThemeMode.Dark);
        return ":root { color-scheme: light; " + light + " }\n"
            + "  :root[data-theme=\"dark\"] { color-scheme: dark; " + dark + " }\n"
            + "  @media (prefers-color-scheme: dark) { :root:not([data-theme=\"light\"]) { color-scheme: dark; " + dark + " } }";
    }

    // Fluid charts keep text and tick density from their prepared width, so they are prepared for the narrowest card body
    // (a 390px phone leaves about 330px); wider cards stretch them horizontally.
    private static string Svg(Chart chart, string id, int width, int height, bool legend = true) {
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), 8),
            VisualTheme.Graphite(), VisualThemeMode.Light, new VisualFrame("", "", showLegend: legend, transparentBackground: true), FontSpec.FromFamily("system-ui, sans-serif")));
        var options = new VisualSvgOptions(id, Variables(VisualThemeMode.Light)).WithEnhanceHooks(includeData: true);
        try { return prepared.ToSvg(options.WithFluidWidth()); }
        catch (NotSupportedException) { return prepared.ToSvg(new VisualSvgOptions(id, Variables(VisualThemeMode.Light), responsive: true).WithEnhanceHooks(includeData: true)); }
    }

    private static void WriteStory(string directory) {
        var records = EnhanceRecipe.Records();
        PreparedVisual Prepared(Chart chart) => chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 320), 8),
            VisualTheme.Graphite(), VisualThemeMode.Light, new VisualFrame("", "", showLegend: true), FontSpec.FromFamily("system-ui, sans-serif")));
        var story = VisualStory.Create("Charts and a CanopyX grid that follow each other")
            .WithDescription("Four chart families and one data grid of the same illustrative records, wired with one call per pair.")
            .WithTheme(VisualTheme.Graphite(), VisualThemeMode.Light).WithSize(1200, 720);
        var code = Asset("linked-views.js").TrimEnd();
        Scene("columns", "Click a segment and the grid filters to its day and kind", Prepared(EnhanceRecipe.Week(records)),
            "Records per day by kind for the last seven days of October 2026 (example data).");
        Scene("heatmap", "Select grid rows and every chart marks their records", Prepared(EnhanceRecipe.Heatmap(records)),
            "Records per product and week, October 2026 (example data).");
        story.Outcome("linked", "Each chart and the grid follow each other with one connect call per pair", "output-heatmap");
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "enhance-story.svg"), story.ToSvg("enhance-story"));
        File.WriteAllBytes(Path.Combine(directory, "enhance-story.png"), story.ToPng());
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "enhance-story.html"), story.ToHtmlPage());
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "enhance-story.txt"), story.ToTranscript());

        void Scene(string id, string title, PreparedVisual visual, string description) {
            story.Scene(id, title, 3, VisualStorySceneLayout.Split)
                .Panel("source-" + id, new VisualStorySourceSurface(StorySourceText.Create(code, "javascript"), "Host code"), weight: 1.1)
                .Panel("output-" + id, new VisualStoryMediaSurface(visual.ToPng(), description), "Static chart snapshot", weight: 1);
        }
    }

    private static string Asset(string name) {
        using var stream = typeof(EnhanceExamples).Assembly.GetManifestResourceStream("ChartForgeX.Examples.Enhance." + name)
            ?? throw new InvalidOperationException("Missing enhance example asset: " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
