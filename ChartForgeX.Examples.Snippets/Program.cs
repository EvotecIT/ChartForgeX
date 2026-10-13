using System.Xml.Linq;

var output = args.Length == 0 ? Path.Combine(AppContext.BaseDirectory, "recipe-output") : Path.GetFullPath(args[0]);
Directory.CreateDirectory(output);
var count = 0;
foreach (var entry in ExampleRecipes.ChartFamilies) {
    Verify(entry.Key.ToString(), entry.Value, "wide", VisualThemeMode.Light);
    Verify(entry.Key.ToString(), entry.Value, "compact", VisualThemeMode.Dark);
}
foreach (var entry in ExampleRecipes.OtherRecipes.Where(entry => !entry.Key.EndsWith("viewport", StringComparison.Ordinal)))
    Verify(entry.Key, entry.Value, "wide", VisualThemeMode.Light);
foreach (var entry in ExampleRecipes.OptionVariants) {
    Verify(entry.Key.Kind + "-option", entry.Value, entry.Key.Variant, VisualThemeMode.Light);
    var compact = "compact-" + entry.Key.Variant;
    Verify(entry.Key.Kind + "-option", ExampleRecipes.ForKind(entry.Key.Kind, compact), compact, VisualThemeMode.Dark);
}
Console.WriteLine($"Verified {count} compiled recipes without a ChartForgeX.Examples assembly reference. Outputs: {output}");

void Verify(string name, ExampleRecipe recipe, string variant, VisualThemeMode mode) {
    var id = name.ToLowerInvariant() + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
    var width = name is "sequence" or "topology" or "chart-grid" ? 960 : name == "flow" ? 900 : variant == "compact" ? 360 : 800;
    var height = name == "sequence" ? 760 : name == "chart-grid" ? 700 : name is "topology" or "flow" ? 600 : variant == "compact" ? 360 : 440;
    var prepared = recipe.Create(variant, mode).Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode, new VisualFrame(name)));
    var svg = prepared.ToSvg();
    var root = XDocument.Parse(svg).Root ?? throw new InvalidOperationException("The recipe produced no SVG root: " + id);
    if ((string?)root.Attribute("width") != width.ToString(CultureInfo.InvariantCulture)
        || (string?)root.Attribute("height") != height.ToString(CultureInfo.InvariantCulture))
        throw new InvalidOperationException("The recipe changed its requested SVG dimensions: " + id);
    var png = prepared.ToPng();
    if (png.Length < 24 || ReadInt32(png, 16) != width || ReadInt32(png, 20) != height)
        throw new InvalidOperationException("The recipe changed its requested PNG dimensions: " + id);
    File.WriteAllText(Path.Combine(output, id + ".svg"), svg);
    File.WriteAllBytes(Path.Combine(output, id + ".png"), png);
    count++;
}

static int ReadInt32(byte[] bytes, int offset) => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
