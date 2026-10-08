using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;

public static partial class V2Examples {
    /// <summary>Checks the generated manifest, source/output links and exact SVG/PNG dimensions.</summary>
    public static void ValidateOutput(string output) {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "manifest.json")));
        var artifacts = document.RootElement.GetProperty("artifacts").EnumerateArray().ToArray();
        if (artifacts.Length == 0) throw new InvalidOperationException("The v2 catalog has no artifacts.");
        var assets = document.RootElement.GetProperty("assets").EnumerateArray().Select(asset => asset.GetString()!).ToHashSet(StringComparer.Ordinal);
        var normalizedOutput = Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (var asset in assets) {
            var resolved = Path.GetFullPath(Path.Combine(output, asset));
            if (Path.IsPathRooted(asset) || !resolved.StartsWith(normalizedOutput, pathComparison) || !File.Exists(resolved))
                throw new InvalidOperationException("Missing or invalid declared gallery asset: " + asset);
        }
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) foreach (var theme in new[] { "light", "dark" }) foreach (var variant in new[] { "wide", "compact" })
            if (!artifacts.Any(artifact => artifact.GetProperty("theme").GetString() == theme && artifact.GetProperty("variant").GetString() == variant
                && artifact.GetProperty("seriesKinds").EnumerateArray().Any(value => value.GetString() == kind.ToString())))
                throw new InvalidOperationException("The v2 catalog is missing " + kind + " in " + theme + " at " + variant + " size.");
        var catalog = File.ReadAllText(Path.Combine(output, "index.html"));
        foreach (var asset in new[] { "gallery.css", "gallery.js" })
            if (!File.Exists(Path.Combine(output, asset)) || !catalog.Contains(asset, StringComparison.Ordinal))
                throw new InvalidOperationException("Missing gallery presentation asset: " + asset);
        var families = artifacts.Select(artifact => artifact.GetProperty("family").GetString()).Distinct().ToArray();
        foreach (var family in families) {
            var primary = artifacts.Where(artifact => artifact.GetProperty("family").GetString() == family && artifact.GetProperty("primary").GetBoolean()).ToArray();
            if (primary.Length != 2 || primary.Select(artifact => artifact.GetProperty("theme").GetString()).Distinct().Count() != 2)
                throw new InvalidOperationException("Every gallery family requires one primary example in each theme: " + family);
            if (!catalog.Contains("data-gallery-family=\"" + family + "\"", StringComparison.Ordinal))
                throw new InvalidOperationException("Catalog does not contain the primary family: " + family);
        }
        foreach (var artifact in artifacts) {
            var id = artifact.GetProperty("id").GetString()!;
            if (!identifiers.Add(id)) throw new InvalidOperationException("Duplicate v2 artifact: " + id);
            var example = File.ReadAllText(Path.Combine(output, artifact.GetProperty("html").GetString()!));
            if (artifact.GetProperty("diagnostics").GetArrayLength() > 0 && !example.Contains("data-layout-notes", StringComparison.Ordinal))
                throw new InvalidOperationException("Example page hides its layout limitations: " + id);
            foreach (var kind in new[] { "svg", "png", "html", "source", "thumbnail", "thumbnailPng" }) {
                var name = artifact.GetProperty(kind).GetString()!;
                if (Path.GetFileName(name) != name || !File.Exists(Path.Combine(output, name)))
                    throw new InvalidOperationException("Missing or invalid " + kind + " artifact: " + id);
                if (!assets.Contains(name)) throw new InvalidOperationException("Gallery asset handoff does not include " + name);
                if ((kind is "svg" or "png" or "source") && !example.Contains(name, StringComparison.Ordinal))
                    throw new InvalidOperationException("Example page does not link " + name);
                if (kind == "html" && artifact.GetProperty("primary").GetBoolean() && !catalog.Contains(name, StringComparison.Ordinal))
                    throw new InvalidOperationException("Catalog does not link its primary example: " + name);
            }
            var width = artifact.GetProperty("width").GetInt32();
            var height = artifact.GetProperty("height").GetInt32();
            var thumbnail = XDocument.Load(Path.Combine(output, artifact.GetProperty("thumbnail").GetString()!)).Root!;
            if ((double?)thumbnail.Attribute("width") != artifact.GetProperty("thumbnailWidth").GetInt32() ||
                (double?)thumbnail.Attribute("height") != artifact.GetProperty("thumbnailHeight").GetInt32())
                throw new InvalidOperationException("Thumbnail dimensions differ from the uniform viewport: " + id);
            if ((string?)thumbnail.Attribute("viewBox") != "0 0 " + ThumbnailWidth + " " + ThumbnailHeight)
                throw new InvalidOperationException("Thumbnail geometry was scaled from a larger scene: " + id);
            var accessibleTitle = thumbnail.Elements().FirstOrDefault(element => element.Name.LocalName == "title")?.Value;
            if (accessibleTitle != artifact.GetProperty("title").GetString())
                throw new InvalidOperationException("Thumbnail must name its actual example: " + id);
            var thumbnailPng = File.ReadAllBytes(Path.Combine(output, artifact.GetProperty("thumbnailPng").GetString()!));
            if (thumbnailPng.Length < 24 || PngDimension(thumbnailPng, 16) != ThumbnailWidth || PngDimension(thumbnailPng, 20) != ThumbnailHeight)
                throw new InvalidOperationException("PNG preview dimensions differ from the uniform viewport: " + id);
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
