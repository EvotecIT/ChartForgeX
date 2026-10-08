using System.Text.Json;
using System.Xml.Linq;
using System.Globalization;
using ChartForgeX.Core;

public static partial class V2Examples {
    /// <summary>Checks the generated manifest, source/output links and exact SVG/PNG dimensions.</summary>
    public static void ValidateOutput(string output) {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "manifest.json")));
        var artifacts = document.RootElement.GetProperty("artifacts").EnumerateArray().ToArray();
        if (artifacts.Length == 0) throw new InvalidOperationException("The v2 catalog has no artifacts.");
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) foreach (var theme in new[] { "light", "dark" })
            if (!artifacts.Any(artifact => artifact.GetProperty("theme").GetString() == theme && artifact.GetProperty("seriesKinds").EnumerateArray().Any(value => value.GetString() == kind.ToString())))
                throw new InvalidOperationException("The v2 catalog is missing " + kind + " in " + theme + ".");
        var catalog = File.ReadAllText(Path.Combine(output, "index.html"));
        foreach (var artifact in artifacts) {
            var id = artifact.GetProperty("id").GetString()!;
            if (!identifiers.Add(id)) throw new InvalidOperationException("Duplicate v2 artifact: " + id);
            foreach (var kind in new[] { "svg", "png", "html", "source", "thumbnail" }) {
                var name = artifact.GetProperty(kind).GetString()!;
                if (Path.GetFileName(name) != name || !File.Exists(Path.Combine(output, name)))
                    throw new InvalidOperationException("Missing or invalid " + kind + " artifact: " + id);
                if (kind != "thumbnail" && !catalog.Contains(name, StringComparison.Ordinal)) throw new InvalidOperationException("Catalog does not link " + name);
            }
            var width = artifact.GetProperty("width").GetInt32();
            var height = artifact.GetProperty("height").GetInt32();
            var thumbnail = XDocument.Load(Path.Combine(output, artifact.GetProperty("thumbnail").GetString()!)).Root!;
            if ((double?)thumbnail.Attribute("width") != artifact.GetProperty("thumbnailWidth").GetInt32() ||
                (double?)thumbnail.Attribute("height") != artifact.GetProperty("thumbnailHeight").GetInt32())
                throw new InvalidOperationException("Thumbnail dimensions differ from the uniform viewport: " + id);
            if (artifact.GetProperty("family").GetString() == "sequence" && artifact.GetProperty("variant").GetString() == "expanded" &&
                ((string?)thumbnail.Attribute("viewBox") != "0 0 " + width.ToString(CultureInfo.InvariantCulture) + " " + height.ToString(CultureInfo.InvariantCulture) ||
                    (string?)thumbnail.Attribute("preserveAspectRatio") != "xMidYMid meet"))
                throw new InvalidOperationException("The sequence thumbnail must retain its full logical viewport: " + id);
            var svg = File.ReadAllText(Path.Combine(output, artifact.GetProperty("svg").GetString()!));
            var root = XDocument.Parse(svg).Root ?? throw new InvalidOperationException("Missing SVG root: " + id);
            if ((double?)root.Attribute("width") != width || (double?)root.Attribute("height") != height)
                throw new InvalidOperationException("SVG dimensions differ from the manifest: " + id);
            if (root.Descendants().Any(element => element.Name.LocalName.Equals("script", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Static proof SVG contains a script: " + id);
            var png = File.ReadAllBytes(Path.Combine(output, artifact.GetProperty("png").GetString()!));
            if (png.Length < 24 || png[0] != 137 || png[1] != 80 || PngDimension(png, 16) != width || PngDimension(png, 20) != height)
                throw new InvalidOperationException("PNG dimensions differ from the manifest: " + id);
        }
    }

    private static int PngDimension(byte[] bytes, int offset) => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
}
