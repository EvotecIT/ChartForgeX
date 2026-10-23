using System.Text.Json;
using System.Xml.Linq;

var output = args.Length == 0 ? Path.Combine(AppContext.BaseDirectory, "recipe-output") : Path.GetFullPath(args[0]);
Directory.CreateDirectory(output);
var modes = new[] { VisualThemeMode.Light, VisualThemeMode.Dark };
var results = new List<RecipeResult>();
var visited = new HashSet<string>(StringComparer.Ordinal);
foreach (var selection in ExampleRecipes.StandaloneFamilySelections()) RunFamily(selection.Kind, selection.Variant);
foreach (var entry in ExampleRecipes.OtherRecipes) RunNamed(entry.Key, "wide");
RunNamed("histogram", "compact", "density", "compact-density", "sum", "compact-sum", "mean", "compact-mean");
RunNamed("histogram", "probability", "compact-probability", "probability-density", "compact-probability-density", "weighted-mean", "compact-weighted-mean");
RunNamed("chart-grid", "compact");
RunNamed("topology", "expanded", "compact", "shapes");
RunNamed("flow", "expanded");
RunNamed("sequence", "expanded");
RunNamed("simple-topology", "feasibility");
RunNamed("simple-sequence", "feasibility");
RunNamed("equal-pie", "above-labels", "below-labels");
RunNamed("equal-donut", "above-labels", "below-labels");
RunNamed("mixed-radial", "mixed", "compact-mixed", "mixed-curves", "compact-mixed-curves", "mixed-angular-reversal", "compact-mixed-angular-reversal");
RunNamed("radial-bar-scales", "dual-axes", "compact-dual-axes");
RunNamed("line-viewport", "x-viewport", "compact-x-viewport");
RunNamed("step-line-viewport", "x-viewport", "compact-x-viewport");
foreach (var name in new[] { "comparison", "findings" })
    RunNamed(name, "compact", "long-title", "wrapped-legend", "surface", "empty", "zero", "missing", "dense", "explicit-status", "explicit-series");
foreach (var selection in ExampleRecipes.LegacyCharts)
    File.WriteAllText(Path.Combine(output, selection.Name + ".csharp.txt"),
        ExampleRecipeSource.EmitChart(selection.Recipe, selection.Name, selection.Variant, selection.Mode));
var failures = results.Where(result => !result.Passed).ToArray();
File.WriteAllText(Path.Combine(output, "recipe-results.json"), JsonSerializer.Serialize(new {
    attempted = results.Count, passed = results.Count - failures.Length, failed = failures.Length, results
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Verified {results.Count - failures.Length}/{results.Count} compiled recipe selections without a ChartForgeX.Examples assembly reference. Outputs: {output}");
if (failures.Length > 0) {
    foreach (var failure in failures) Console.Error.WriteLine(failure.Id + ": " + failure.Error);
    throw new InvalidOperationException($"{failures.Length} advertised recipe selections failed; see recipe-results.json.");
}

void RunFamily(ChartSeriesKind kind, string variant) {
    foreach (var mode in modes) Run(kind.ToString(), ExampleRecipes.ForKind(kind, variant), variant, mode, kind);
}

void RunNamed(string name, params string[] variants) {
    foreach (var variant in variants)
        foreach (var mode in modes) Run(name, ExampleRecipes.Named(name), variant, mode);
}

void Run(string name, ExampleRecipe recipe, string variant, VisualThemeMode mode, ChartSeriesKind? expectedKind = null) {
    var id = name.ToLowerInvariant() + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
    // Ordinary and advanced catalogs can advertise the same exact selection; qualify it once.
    if (!visited.Add(id)) return;
    var compact = variant.StartsWith("compact", StringComparison.Ordinal);
    var width = name == "chart-grid" ? compact ? 520 : 960 : name == "topology" ? compact ? 420 : 960
        : name == "sequence" ? 960 : name == "flow" ? 900 : compact ? 360 : 800;
    var height = name == "sequence" ? 760 : name == "chart-grid" ? compact ? 540 : 700
        : name == "topology" ? compact ? 360 : variant == "shapes" ? 720 : 600 : name == "flow" ? 600 : compact ? 360 : 440;
    try {
        var model = recipe.Create(variant, mode);
        if (expectedKind.HasValue && (model is not Chart chart || !chart.Series.Any(series => series.Kind == expectedKind.Value)))
            throw new InvalidOperationException("The recipe did not create its advertised chart family.");
        var sourceSeries = model is Chart source ? source.Series.Select(series =>
            new RecipeSeries(series.Id.Value, series.Name, series.Kind.ToString(), series.SourcePointCount)).ToArray() : Array.Empty<RecipeSeries>();
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode,
            new VisualFrame(name, showSurface: variant == "surface"));
        var prepared = name.EndsWith("viewport", StringComparison.Ordinal) && model is Chart viewportChart
            ? viewportChart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(2, 8))) : model.Prepare(context);
        var svg = prepared.ToSvg();
        var root = XDocument.Parse(svg).Root ?? throw new InvalidOperationException("The recipe produced no SVG root.");
        if ((string?)root.Attribute("width") != width.ToString(CultureInfo.InvariantCulture)
            || (string?)root.Attribute("height") != height.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("The recipe changed its requested SVG dimensions.");
        if (expectedKind.HasValue) {
            var keys = root.DescendantsAndSelf().Attributes("data-cfx-series-key").Select(attribute => attribute.Value).ToHashSet(StringComparer.Ordinal);
            foreach (var series in sourceSeries.Where(series => series.SourceObservations > 0))
                if (!keys.Contains(series.Id)) throw new InvalidOperationException("Export lost authored series identity: " + series.Id);
        }
        var png = prepared.ToPng();
        if (png.Length < 24 || ReadInt32(png, 16) != width || ReadInt32(png, 20) != height)
            throw new InvalidOperationException("The recipe changed its requested PNG dimensions.");
        File.WriteAllText(Path.Combine(output, id + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(output, id + ".png"), png);
        results.Add(new(id, recipe.Factory, variant, mode.ToString(), width, height, true, prepared.Regions.Count, sourceSeries, null));
    } catch (Exception error) {
        results.Add(new(id, recipe.Factory, variant, mode.ToString(), width, height, false, 0, Array.Empty<RecipeSeries>(), error.Message));
    }
}

static int ReadInt32(byte[] bytes, int offset) => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
internal sealed record RecipeSeries(string Id, string Name, string Kind, int SourceObservations);
internal sealed record RecipeResult(string Id, string Factory, string Variant, string Mode, int Width, int Height,
    bool Passed, int SemanticRegions, RecipeSeries[] SourceSeries, string? Error);
