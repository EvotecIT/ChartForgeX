// Visuals the inner pages are built around: navigation thumbnails, the Mermaid family atlas, docs figures, example
// outputs and the gallery composition. Every file is rendered by ChartForgeX and recorded in data/visuals.json.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Themes;

namespace SiteVisuals;

internal static class PageVisuals {
    public static void Write(string website) {
        var output = new VisualOutput(website);
        // Later sets reuse earlier renders where a page shows the same output, so each visual is written once.
        var examples = ExampleVisuals.Write(output);
        var docs = DocVisuals.Write(output, examples);
        var manifest = new SortedDictionary<string, object?>(StringComparer.Ordinal) {
            ["generatedBy"] = "Website/tools/SiteVisuals",
            ["mermaid"] = MermaidAtlas.Write(output),
            ["examples"] = examples,
            ["docs"] = docs,
            ["nav"] = NavVisuals.Write(output, docs),
            ["gallery"] = GalleryVisuals.Write(output),
            ["notFound"] = NotFoundVisuals.Write(output)
        };

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(website, "data", "visuals.json"), json.Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
        Console.WriteLine($"Page visuals: {output.Files} files, {output.Bytes / 1024d:0} KB");
    }
}

/// <summary>Writes generated files under static/site and measures what it wrote.</summary>
internal sealed class VisualOutput {
    private static readonly string[] OwnedFolders = { "atlas", "nav", "docs", "examples", "gallery", "pages" };
    private static readonly Regex RootSize = new(@"<svg\b[^>]*?\bwidth=""(?<w>[\d.]+)""[^>]*?\bheight=""(?<h>[\d.]+)""", RegexOptions.Compiled);
    private static readonly Regex ViewBox = new(@"<svg\b[^>]*?\bviewBox=""[\d.\-]+ [\d.\-]+ (?<w>[\d.]+) (?<h>[\d.]+)""", RegexOptions.Compiled);

    public VisualOutput(string website) {
        Website = website;
        Root = Path.Combine(website, "static", "site");
        Light = VisualDesignTokens.GraphiteLight();
        Dark = VisualDesignTokens.GraphiteDark();
        // The generator owns these folders completely, so a renamed visual never leaves a stale file behind.
        foreach (var folder in OwnedFolders) {
            var path = Path.Combine(Root, folder);
            if (Directory.Exists(path)) {
                foreach (var file in Directory.EnumerateFiles(path)) File.Delete(file);
            }
            Directory.CreateDirectory(path);
        }
    }

    public string Website { get; }
    public string Root { get; }
    public VisualDesignTokens Light { get; }
    public VisualDesignTokens Dark { get; }
    public int Files { get; private set; }
    public long Bytes { get; private set; }

    public IEnumerable<(string Mode, VisualDesignTokens Tokens, bool IsDark)> Modes() {
        yield return ("light", Light, false);
        yield return ("dark", Dark, true);
    }

    public void Text(string relative, string content) {
        var bytes = new UTF8Encoding(false).GetBytes(content.Replace("\r\n", "\n"));
        Binary(relative, bytes);
    }

    public void Binary(string relative, byte[] content) {
        var path = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        Files++;
        Bytes += content.Length;
    }

    /// <summary>Writes a light and a dark SVG named <c>{name}-light.svg</c> and <c>{name}-dark.svg</c> and returns their size.</summary>
    public Dictionary<string, object> Pair(string name, Func<VisualDesignTokens, bool, string> render) {
        var light = render(Light, false);
        var dark = render(Dark, true);
        Text(name + "-light.svg", light);
        Text(name + "-dark.svg", dark);
        var (width, height) = Size(light);
        return new Dictionary<string, object> {
            ["base"] = "/site/" + name,
            ["ext"] = "svg",
            ["w"] = width,
            ["h"] = height,
            ["kb"] = Math.Round((Encoding.UTF8.GetByteCount(light) + Encoding.UTF8.GetByteCount(dark)) / 2048d, 1)
        };
    }

    /// <summary>Writes a light and a dark PNG pair, for outputs whose SVG animates.</summary>
    public Dictionary<string, object> PngPair(string name, Func<VisualDesignTokens, bool, byte[]> render) {
        var light = render(Light, false);
        var dark = render(Dark, true);
        // Width and height are the big-endian integers of the IHDR chunk.
        static int Read(byte[] png, int offset) => (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
        var width = Read(light, 16);
        var height = Read(light, 20);
        Binary(name + "-light.png", light);
        Binary(name + "-dark.png", dark);
        return new Dictionary<string, object> {
            ["base"] = "/site/" + name,
            ["ext"] = "png",
            ["w"] = width,
            ["h"] = height,
            ["kb"] = Math.Round((light.Length + dark.Length) / 2048d, 1)
        };
    }

    /// <summary>Points a second use at a pair that is already written, so one render serves several pages.</summary>
    public static Dictionary<string, object> Reuse(Dictionary<string, object> entry) => new(entry);

    public static (int Width, int Height) Size(string svg) {
        var match = RootSize.Match(svg);
        if (!match.Success) match = ViewBox.Match(svg);
        if (!match.Success) throw new InvalidOperationException("Generated SVG has no size.");
        return ((int)Math.Round(double.Parse(match.Groups["w"].Value, CultureInfo.InvariantCulture)),
            (int)Math.Round(double.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Crops an SVG to what it draws, then widens the crop to <paramref name="aspect"/> so a card of that shape shows the
    /// content filling it. The SVG is rasterized by ChartForgeX to find the drawn area; the frame edge and its shadow are
    /// ignored and the dominant colour inside is taken as the background. Only the root viewBox and size change.
    /// </summary>
    public static string Fit(string svg, double aspect, int inset = 18, int margin = 20) {
        var (width, height) = Size(svg);
        var image = ChartForgeX.Raster.RasterImageDecoder.Decode(ChartForgeX.SvgRaster.SvgRasterizer.ToPng(svg, width, height));
        int Offset(int x, int y) => (y * image.Width + x) * 4;
        var counts = new Dictionary<int, int>();
        for (var y = inset; y < image.Height - inset; y += 3) {
            for (var x = inset; x < image.Width - inset; x += 3) {
                var o = Offset(x, y);
                var key = (image.Pixels[o] >> 3) << 10 | (image.Pixels[o + 1] >> 3) << 5 | (image.Pixels[o + 2] >> 3);
                counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
            }
        }
        if (counts.Count == 0) return svg;
        var background = counts.OrderByDescending(pair => pair.Value).First().Key;
        int br = ((background >> 10) & 31) << 3, bg = ((background >> 5) & 31) << 3, bb = (background & 31) << 3;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = inset; y < image.Height - inset; y++) {
            for (var x = inset; x < image.Width - inset; x++) {
                var o = Offset(x, y);
                if (image.Pixels[o + 3] < 128) continue;
                var distance = Math.Abs(image.Pixels[o] - br) + Math.Abs(image.Pixels[o + 1] - bg) + Math.Abs(image.Pixels[o + 2] - bb);
                if (distance <= 42) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
        if (maxX < 0) return svg;

        double x0 = Math.Max(0, minX - margin), y0 = Math.Max(0, minY - margin);
        double x1 = Math.Min(width, maxX + margin), y1 = Math.Min(height, maxY + margin);
        if ((x1 - x0) / (y1 - y0) > aspect) {
            var grow = (x1 - x0) / aspect - (y1 - y0);
            y0 -= grow / 2;
            y1 += grow / 2;
        } else {
            var grow = (y1 - y0) * aspect - (x1 - x0);
            x0 -= grow / 2;
            x1 += grow / 2;
        }

        // Keep the crop on the canvas: shift it back inside, and stop widening at the canvas edge.
        (x0, x1) = Clamp(x0, x1, width);
        (y0, y1) = Clamp(y0, y1, height);
        var viewBox = string.Format(CultureInfo.InvariantCulture, "{0:0.#} {1:0.#} {2:0.#} {3:0.#}", x0, y0, x1 - x0, y1 - y0);
        var root = RootTag.Match(svg);
        var tag = root.Value;
        tag = Regex.Replace(tag, @"(?<=\s)viewBox=""[^""]*""", $"viewBox=\"{viewBox}\"");
        tag = Regex.Replace(tag, @"(?<=\s)width=""[\d.]+""", string.Format(CultureInfo.InvariantCulture, "width=\"{0:0}\"", x1 - x0));
        tag = Regex.Replace(tag, @"(?<=\s)height=""[\d.]+""", string.Format(CultureInfo.InvariantCulture, "height=\"{0:0}\"", y1 - y0));
        return svg.Substring(0, root.Index) + tag + svg.Substring(root.Index + root.Length);

        static (double, double) Clamp(double start, double end, double size) {
            if (end - start >= size) return (0, size);
            if (start < 0) return (0, end - start);
            if (end > size) return (size - (end - start), size);
            return (start, end);
        }
    }

    private static readonly Regex RootTag = new(@"<svg\b[^>]*>", RegexOptions.Compiled);
    public static void Report(string label, IEnumerable<string> diagnostics) {
        foreach (var diagnostic in diagnostics) Console.WriteLine($"[{label}] {diagnostic}");
    }

    /// <summary>Renders playground source through the playground engine, so the result matches what the playground shows.</summary>
    public static string Playground(string language, string source, bool dark) {
        using var result = JsonDocument.Parse(ChartForgeX.Playground.PlaygroundEngine.Render(language, source, dark ? "dark" : "light"));
        var root = result.RootElement;
        if (!root.GetProperty("ok").GetBoolean()) {
            throw new InvalidOperationException("Playground render failed: " + root.GetProperty("error").GetString());
        }

        return root.GetProperty("svg").GetString()!;
    }
}
