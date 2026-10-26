using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Svg;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

/// <summary>Content-sized gallery fixtures keep exact baselines across installed font environments.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidGalleryTypographyTests {
    private readonly ITestOutputHelper _output;

    public MermaidGalleryTypographyTests(ITestOutputHelper output) => _output = output;

    [Fact]
    [Trait("Category", "Gallery")]
    public void PublishedSamplesRetainDefaultGeometryAndAuthoredFontsWhenHostFallbackFacesChange() {
        var output = ExampleTestOutput.CreateScratchPath("CFX-Mermaid-gallery-font-");
        Directory.CreateDirectory(output);
        try {
            var first = Generate("Regular", Path.Combine(output, "regular"));
            var second = Generate("Bold", Path.Combine(output, "bold"));
            // The unpinned public renderer proves these really are different font environments.
            Assert.NotEqual(first.UnpinnedWidth, second.UnpinnedWidth);
            Assert.Equal(first.Artifacts.Keys.OrderBy(name => name), second.Artifacts.Keys.OrderBy(name => name));
            Assert.Contains("mermaid-flowchart-nested-directions-dark", first.Artifacts.Keys);
            foreach (var name in first.Artifacts.Keys) {
                var svg = XDocument.Parse(first.Artifacts[name].Svg);
                var html = File.ReadAllText(Path.Combine(output, "regular", name + ".html"));
                if (AuthoredFont(name) is { } authored) {
                    _output.WriteLine("Checking source-font fixture: " + name + " (" + authored + ")");
                    Assert.Contains(svg.Descendants().Attributes("font-family"), attribute => attribute.Value.Contains(authored, StringComparison.Ordinal));
                    Assert.DoesNotContain("CFX Proof Carlito", first.Artifacts[name].Svg, StringComparison.Ordinal);
                    Assert.DoesNotContain("data:font/ttf;base64,", html, StringComparison.Ordinal);
                } else {
                    _output.WriteLine("Comparing default-font fixture: " + name);
                    var expected = CanonicalSvg(first.Artifacts[name].Svg);
                    var actual = CanonicalSvg(second.Artifacts[name].Svg);
                    if (expected != actual || !first.Artifacts[name].Png.SequenceEqual(second.Artifacts[name].Png))
                        CaptureMismatch(name, first.Artifacts[name], second.Artifacts[name]);
                    Assert.Equal(expected, actual);
                    Assert.Equal(first.Artifacts[name].Png, second.Artifacts[name].Png);
                    if (svg.Descendants().Any(element => element.Name.LocalName == "text"))
                        Assert.Contains(svg.Descendants().Attributes("font-family"), attribute => attribute.Value.Split(',')[0].Trim().Trim('\'', '"') == "CFX Proof Carlito");
                    else
                        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-native-text-ink") == "true"
                            && element.Descendants().Any(child => child.Name.LocalName == "use"));
                    Assert.Contains("@font-face", html, StringComparison.Ordinal);
                    Assert.Contains("data:font/ttf;base64,", html, StringComparison.Ordinal);
                }
            }
        } finally {
            FontRegistry.Clear();
            Directory.Delete(output, recursive: true);
        }
    }

    private static string? AuthoredFont(string name) => name switch {
        "mermaid-source-font" or "mermaid-source-font-size" or "mermaid-sequence-presentation" => "Georgia",
        "mermaid-source-font-size-icon-fit" or "mermaid-source-font-size-icon-maximum-fit" or
        "mermaid-source-font-size-c4-subtitle-fit" or "mermaid-source-font-size-requirement-subtitle-fit" or
        "mermaid-source-font-size-group-header-fit" or "mermaid-source-font-size-packet-narrow-fit" or
        "mermaid-source-font-size-axis-rotated-fit" or "mermaid-source-font-size-actor-compact-fit" or
        "mermaid-source-font-size-sankey-fit" or "mermaid-source-font-size-treemap-fit" or
        "mermaid-source-font-size-axis-fit" or "mermaid-source-font-size-actor-fit" or
        "mermaid-source-font-size-tile-fit" or "mermaid-source-font-size-git-cherry-fit" or
        "mermaid-source-font-size-quadrant-captions" or "mermaid-source-font-size-journey-captions" => "Arial",
        _ => null
    };

    private static string CanonicalSvg(string svg) {
        var document = XDocument.Parse(svg);
        var ids = document.Descendants().Attributes("id").Select(attribute => attribute.Value).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(document.Descendants().Attributes("id").Count(), ids.Count);
        foreach (var attribute in document.Descendants().Attributes()) {
            // Anchor navigation may target the containing document. SVG resources
            // such as use/image references still have to resolve inside this artifact.
            if (attribute.Name.LocalName == "href" && attribute.Parent?.Name.LocalName != "a"
                && attribute.Value.StartsWith("#", StringComparison.Ordinal))
                Assert.Contains(attribute.Value.Substring(1), ids);
            if (attribute.Name.LocalName is "aria-labelledby" or "aria-describedby")
                foreach (var id in attribute.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)) Assert.Contains(id, ids);
            foreach (Match match in Regex.Matches(attribute.Value, @"url\(#([^\)]+)\)")) Assert.Contains(match.Groups[1].Value, ids);
        }
        // Scene identity includes native font metadata. Rebind only generated namespaces;
        // every geometry, text, paint and source attribute still compares exactly.
        var prefixes = document.Descendants().Attributes("id").SelectMany(attribute => Regex.Matches(attribute.Value, @"\bcfx-v2-[0-9a-f]{64}").Cast<Match>())
            .Select(match => match.Value).Distinct().ToArray();
        for (var index = 0; index < prefixes.Length; index++)
            svg = SvgRenderedIdentity.RebindGeneratedId(svg, prefixes[index], "cfx-gallery-font-" + index);
        return svg;
    }

    private void CaptureMismatch(string name, (string Svg, byte[] Png) first, (string Svg, byte[] Png) second) {
        _output.WriteLine("SVG/PNG mismatch in " + name);
        var capture = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Assert.True(Path.IsPathRooted(capture));
        Directory.CreateDirectory(capture);
        File.WriteAllText(Path.Combine(capture, name + "-regular.svg"), first.Svg);
        File.WriteAllText(Path.Combine(capture, name + "-bold.svg"), second.Svg);
        File.WriteAllBytes(Path.Combine(capture, name + "-regular.png"), first.Png);
        File.WriteAllBytes(Path.Combine(capture, name + "-bold.png"), second.Png);
    }

    private (double UnpinnedWidth, Dictionary<string, (string Svg, byte[] Png)> Artifacts) Generate(string hostFace, string output) {
        FontRegistry.Clear();
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-" + hostFace + ".ttf");
        foreach (var family in new[] { "Calibri", "Carlito", "Segoe UI", "Arial", "Inter", "system-ui", "sans-serif" })
            foreach (var weight in new[] { 400, 600, 700 }) FontRegistry.Register(family, path, weight);
        const string control = "classDiagram\ndirection RL\nService --> Store : writes\nnote for Service \"Requests: Service --> Store\"\n";
        var rendered = MermaidRenderer.Render(control);
        Assert.False(rendered.HasErrors);
        var unpinnedWidth = double.Parse(XDocument.Parse(rendered.Artifact!.ToSvg()).Root!.Attribute("width")!.Value, CultureInfo.InvariantCulture);

        // Exercise the existing executable entry point, including its real sample routing and exports.
        var entryPoint = typeof(GalleryWriter).Assembly.EntryPoint!;
        try { entryPoint.Invoke(null, new object[] { new[] { "--mermaid-only", "--output", output } }); }
        catch (TargetInvocationException exception) when (exception.InnerException != null) {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        using var comparison = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
        var rows = comparison.RootElement.GetProperty("charts").EnumerateArray().ToDictionary(row => row.GetProperty("name").GetString()!);
        foreach (var row in rows.Values.Where(row => new[] { "svg", "png", "html" }.Any(format => !row.GetProperty(format).GetProperty("healthy").GetBoolean()))) {
            // Retain every failing family before the temporary gallery is removed, so one
            // run diagnoses the complete health batch rather than only its first row.
            _output.WriteLine("Unhealthy gallery artifact: " + row.GetRawText());
            CaptureGalleryArtifact(row, output, hostFace, "health");
        }
        foreach (var row in rows.Values.Where(row => row.GetProperty("name").GetString() is { } name
            && (name.StartsWith("mermaid-usecase-source-depth-", StringComparison.Ordinal)
                || name.StartsWith("mermaid-c4-source-rows-", StringComparison.Ordinal))))
            CaptureGalleryArtifact(row, output, hostFace, "layout");
        var artifacts = new Dictionary<string, (string Svg, byte[] Png)>();
        foreach (var row in rows.Values) {
            var name = row.GetProperty("name").GetString()!;
            foreach (var format in new[] { "svg", "png", "html" }) Assert.True(row.GetProperty(format).GetProperty("healthy").GetBoolean(), name + " " + format);
            artifacts.Add(name, (File.ReadAllText(Path.Combine(output, name + ".svg")), File.ReadAllBytes(Path.Combine(output, name + ".png"))));
        }
        // Source typography probes remain source-owned; the proof face is only the gallery default.
        var authored = XDocument.Load(Path.Combine(output, "mermaid-source-font.svg"));
        Assert.Contains(authored.Descendants().Attributes("font-family"), attribute => attribute.Value.Contains("Georgia", StringComparison.Ordinal));
        var authoredSize = XDocument.Load(Path.Combine(output, "mermaid-source-font-size-icon-fit.svg"));
        Assert.Contains(authoredSize.Descendants().Attributes("font-family"), attribute => attribute.Value.Contains("Arial", StringComparison.Ordinal));
        return (unpinnedWidth, artifacts);
    }

    private static void CaptureGalleryArtifact(JsonElement row, string output, string hostFace, string kind) {
        if (Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT") is not { Length: > 0 } capture) return;
        Assert.True(Path.IsPathRooted(capture));
        Directory.CreateDirectory(capture);
        var name = row.GetProperty("name").GetString()!;
        File.WriteAllText(Path.Combine(capture, name + "-" + hostFace + "." + kind + ".json"), row.GetRawText());
        foreach (var format in new[] { "svg", "png", "html" })
            File.Copy(Path.Combine(output, name + "." + format), Path.Combine(capture, name + "-" + hostFace + "." + format), overwrite: true);
    }
}
