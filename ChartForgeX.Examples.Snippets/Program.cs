using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX;

var output = args.Length == 0 ? Path.Combine(AppContext.BaseDirectory, "recipe-output") : Path.GetFullPath(args[0]);
Directory.CreateDirectory(output);
var modes = new[] { VisualThemeMode.Light, VisualThemeMode.Dark };
var results = new List<RecipeResult>();
var visited = new HashSet<string>(StringComparer.Ordinal);
AppearanceSnippetProof.Write(output, results);
RetainedAppearanceSnippetProof.Write(output, results);
foreach (var selection in ExampleRecipes.StandaloneFamilySelections()) RunFamily(selection.Kind, selection.Variant);
foreach (var entry in ExampleRecipes.OtherRecipes) RunNamed(entry.Key, "wide");
RunNamed("aligned-difference", "compact", "negative-log", "compact-negative-log", "sym-log", "compact-sym-log", "connect", "compact-connect");
RunNamed("pictorial-artwork", "compact", "right", "bottom", "top", "image", "contour");
RunNamed("mixed-schedule", "compact", "reversed", "compact-reversed", "time", "compact-time", "time-reversed", "compact-time-reversed");
RunNamed("org-portraits", "compact", "meet", "slice", "collapsed", "horizontal", "reordered");
RunNamed("multi-ring-callouts", "compact", "automatic", "compact-automatic", "vertical", "compact-vertical", "mixed", "compact-mixed");
RunNamed("funnel-presentation", "compact", "cone", "compact-cone", "horizontal", "compact-horizontal", "cone-horizontal", "compact-cone-horizontal");
RunNamed("pyramid-presentation", "compact", "horizontal", "compact-horizontal", "reversed", "compact-reversed");
RunNamed("chord-depth", "compact", "source", "compact-source", "straight", "compact-straight", "center", "compact-center");
RunNamed("combined-gauge", "compact");
RunNamed("quadrant-point-styles", "compact");
RunNamed("usecase-source-depth", "compact");
RunNamed("usecase-advanced-actors", "compact");
RunNamed("topology-path-decorations", "compact", "plain", "compact-plain");
RunNamed("independent-bullet", "compact", "vertical", "compact-vertical");
RunNamed("independent-progress", "compact");
RunNamed("topology-interchange", "compact");
RunNamed("topology-table-content", "compact");
RunNamed("heatmap-hierarchical-axes", "compact", "states-wide", "states-compact");
RunNamed("categorical-radial-area", "compact", "stacked", "compact-stacked", "radius", "compact-radius");
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
foreach (var variant in new[] { "wide", "compact" }) foreach (var mode in modes) RunBlockLayout(variant, mode);
foreach (var mode in modes) foreach (var variant in new[] { "wide", "compact" }) {
    var id = "wardley-depth-" + variant + "-" + mode.ToString().ToLowerInvariant();
    try {
        var block = WardleyDepthRecipe.Create(variant, mode); var svg = block.ToSvg(); var png = block.ToPng();
        if (!svg.Contains("wardley-annotation-box", StringComparison.Ordinal) || !svg.Contains("Prefer a portable service contract", StringComparison.Ordinal))
            throw new InvalidOperationException("Native Wardley recipe lost its complete source annotations.");
        if (ReadInt32(png, 16) != block.Options.Size.Width || ReadInt32(png, 20) != block.Options.Size.Height)
            throw new InvalidOperationException("Native Wardley recipe changed its output dimensions.");
        File.WriteAllText(Path.Combine(output, id + ".svg"), svg); File.WriteAllBytes(Path.Combine(output, id + ".png"), png);
        File.WriteAllText(Path.Combine(output, id + ".html"), block.ToHtmlPage()); File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitWardley(variant, mode));
        results.Add(new(id, nameof(WardleyDepthRecipe), variant, mode.ToString(), block.Options.Size.Width, block.Options.Size.Height, true, 0, Array.Empty<RecipeSeries>(), null));
    } catch (Exception error) { results.Add(new(id, nameof(WardleyDepthRecipe), variant, mode.ToString(), 0, 0, false, 0, Array.Empty<RecipeSeries>(), error.Message)); }
}
RunNamed("sequence-surface-shadows", "compact");
foreach (var mode in modes) foreach (var variant in new[] { "compact", "wide" }) {
    var id = "native-drop-shadow-" + variant + "-" + mode.ToString().ToLowerInvariant();
    try {
        var layer = NativeDropShadowExample.Create(variant, mode); var svg = layer.Visual.ToSvg(); var png = layer.Visual.ToPng();
        if (!svg.Contains("data:image/png;base64,", StringComparison.Ordinal) || !layer.Visual.Diagnostics.Any(item => item.Code == "effects.layer-baked"))
            throw new InvalidOperationException("Captured shadow recipe lost its explicit native image lowering.");
        File.WriteAllText(Path.Combine(output, id + ".svg"), svg); File.WriteAllBytes(Path.Combine(output, id + ".png"), png);
        File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitNativeDropShadow(variant, mode));
        results.Add(new(id, nameof(NativeDropShadowExample), variant, mode.ToString(), ReadInt32(png, 16), ReadInt32(png, 20), true,
            layer.Visual.Regions.Count, Array.Empty<RecipeSeries>(), null));
    } catch (Exception error) { results.Add(new(id, nameof(NativeDropShadowExample), variant, mode.ToString(), 0, 0, false, 0, Array.Empty<RecipeSeries>(), error.Message)); }
}
foreach (var mode in modes) foreach (var variant in new[] { "compact", "wide" }) {
    var id = "native-mask-" + variant + "-" + mode.ToString().ToLowerInvariant();
    try {
        var layer = NativeMaskExample.Create(variant, mode); var svg = layer.Visual.ToSvg(); var png = layer.Visual.ToPng();
        if (!svg.Contains("data:image/png;base64,", StringComparison.Ordinal))
            throw new InvalidOperationException("Native mask lost its explicit baked-image output.");
        File.WriteAllText(Path.Combine(output, id + ".svg"), svg); File.WriteAllBytes(Path.Combine(output, id + ".png"), png);
        File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitNativeMask(variant, mode));
        results.Add(new(id, nameof(NativeMaskExample), variant, mode.ToString(), ReadInt32(png, 16), ReadInt32(png, 20), true,
            layer.Visual.Regions.Count, Array.Empty<RecipeSeries>(), null));
    } catch (Exception error) { results.Add(new(id, nameof(NativeMaskExample), variant, mode.ToString(), 0, 0, false, 0, Array.Empty<RecipeSeries>(), error.Message)); }
}
foreach (var mode in modes) foreach (var variant in new[] { "compact", "wide" }) {
    var id = "native-retained-mask-" + variant + "-" + mode.ToString().ToLowerInvariant();
    var width = variant == "compact" ? 390 : 1100;
    try {
        var source = NativeRetainedMaskExample.PrepareSource(variant, mode);
        var masked = source.WithMask(NativeRetainedMaskExample.CreateMask(source));
        var native = masked.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var png = masked.ToPng();
        if (!native.Contains("<mask", StringComparison.Ordinal) || !native.Contains("<text", StringComparison.Ordinal) ||
            !native.Contains("href=\"#guide\"", StringComparison.Ordinal) || ReadInt32(png, 16) != width || ReadInt32(png, 20) != 620)
            throw new InvalidOperationException("Retained mask recipe lost its typed mask, live captions, link or output dimensions.");
        File.WriteAllText(Path.Combine(output, id + ".svg"), masked.ToSvg());
        File.WriteAllText(Path.Combine(output, id + ".native.svg"), native);
        File.WriteAllBytes(Path.Combine(output, id + ".png"), png);
        File.WriteAllText(Path.Combine(output, id + ".source.json"), source.ToArtifact().ToInterchangeJson());
        var saved = masked.ToArtifact().ToInterchangeJson();
        var restored = NativeRetainedMaskExample.Restore(saved, variant, mode);
        if (!png.SequenceEqual(restored.ToPng()))
            throw new InvalidOperationException("Retained mask source preparation and explicit portable replay changed native output.");
        File.WriteAllText(Path.Combine(output, id + ".json"), saved);
        File.WriteAllText(Path.Combine(output, id + ".restored.svg"), restored.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        File.WriteAllText(Path.Combine(output, id + ".edited.svg"), NativeRetainedMaskExample.Restore(saved, variant, mode, "Approved")
            .ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitRetainedMask(variant, mode));
        results.Add(new(id, nameof(NativeRetainedMaskExample), variant, mode.ToString(), width, 620, true,
            masked.Regions.Count, Array.Empty<RecipeSeries>(), null));
    } catch (Exception error) {
        results.Add(new(id, nameof(NativeRetainedMaskExample), variant, mode.ToString(), width, 620, false,
            0, Array.Empty<RecipeSeries>(), error.Message));
    }
}
foreach (var mode in modes) foreach (var variant in new[] { "compact", "wide" }) {
    var id = "finite-composite-source-" + variant + "-" + mode.ToString().ToLowerInvariant();
    var width = variant == "compact" ? 390 : 760;
    try {
        foreach (var operation in new[] { "over", "in", "out", "atop", "xor" }) {
            var result = NativeCompositeSourceExample.Rasterize(variant, mode, operation);
            if (result.Diagnostics.Count != 0) throw new InvalidOperationException("Finite composite source lost supported lowering.");
            File.WriteAllText(Path.Combine(output, id + "-" + operation + ".svg"), NativeCompositeSourceExample.Source(variant, mode, operation));
            File.WriteAllBytes(Path.Combine(output, id + "-" + operation + ".png"), result.Image.ToPng());
        }
        File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitNativeCompositeSource(variant, mode));
        results.Add(new(id, nameof(NativeCompositeSourceExample), variant, mode.ToString(), width, 280, true,
            0, Array.Empty<RecipeSeries>(), null));
    } catch (Exception error) {
        results.Add(new(id, nameof(NativeCompositeSourceExample), variant, mode.ToString(), width, 280, false,
            0, Array.Empty<RecipeSeries>(), error.Message));
    }
}
RunNamed("sequence-layout-typography","compact");
RunNamed("sequence-participant-views","compact");
RunNamed("sequence-message-presentation","compact");
RunNamed("sequence-self-routes","compact");
RunNamed("sequence-navigation","compact");
RunNamed("sequence-source-defaults","compact");
RunNamed("sequence-preset-appearance","compact");
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
    var width = name is "sequence-layout-typography" or "sequence-participant-views" or "sequence-message-presentation" or "sequence-self-routes" or "sequence-navigation" or "sequence-source-defaults" ? compact ? 390 : 1000 : name == "chart-grid" ? compact ? 520 : 960 : name == "topology" ? compact ? 420 : 960
        : name == "sequence" ? 960 : name == "flow" ? 900 : compact ? 360 : 800;
    var height = name is "sequence-participant-views" or "sequence-message-presentation" or "sequence-self-routes" or "sequence-navigation" or "sequence-source-defaults" ? 820 : name == "sequence-layout-typography" ? 620 : name == "sequence" ? 760 : name == "chart-grid" ? compact ? 540 : 700
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

void RunBlockLayout(string variant, VisualThemeMode mode) {
    var id = "block-layout-" + variant + "-" + mode.ToString().ToLowerInvariant(); var width = variant == "compact" ? 390 : 1000;
    try {
        var block = BlockLayoutDepthRecipe.Create(variant, mode); var svg = block.ToSvg(); var png = block.ToPng();
        var document = XDocument.Parse(svg);
        if (document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "block-layout-node") != 7
            || document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "block-layout-edge") != 2
            || ReadInt32(png, 16) != width || ReadInt32(png, 20) != 620) throw new InvalidOperationException("Native block recipe lost its source cells, connectors or frame.");
        File.WriteAllText(Path.Combine(output, id + ".svg"), svg); File.WriteAllBytes(Path.Combine(output, id + ".png"), png);
        File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitBlockLayout(variant, mode));
        results.Add(new(id, nameof(BlockLayoutDepthRecipe), variant, mode.ToString(), width, 620, true, 0, Array.Empty<RecipeSeries>(), null));
    } catch (Exception error) { results.Add(new(id, nameof(BlockLayoutDepthRecipe), variant, mode.ToString(), width, 620, false, 0, Array.Empty<RecipeSeries>(), error.Message)); }
}

static int ReadInt32(byte[] bytes, int offset) => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
internal sealed record RecipeSeries(string Id, string Name, string Kind, int SourceObservations);
internal sealed record RecipeResult(string Id, string Factory, string Variant, string Mode, int Width, int Height,
    bool Passed, int SemanticRegions, RecipeSeries[] SourceSeries, string? Error);
