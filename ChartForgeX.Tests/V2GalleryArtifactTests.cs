using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the committed gallery handoff used by the project hub and static previews.</summary>
public sealed class V2GalleryArtifactTests {
    [Fact]
    public void PublishedCatalogKeepsOnePrimaryPerFamilyAndNamedNativePreviews() {
        var repository = FindRepository();
        var output = Path.Combine(repository, "Website", "static", "examples", "generated-v2");
        V2Examples.ValidateOutput(output);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "manifest.json")));
        var artifacts = document.RootElement.GetProperty("artifacts").EnumerateArray().ToArray();
        var primary = artifacts.Where(artifact => artifact.GetProperty("primary").GetBoolean() && artifact.GetProperty("theme").GetString() == "light").ToArray();
        Assert.Equal(primary.Length, primary.Select(artifact => artifact.GetProperty("family").GetString()).Distinct().Count());
        var represented = primary.SelectMany(artifact => artifact.GetProperty("seriesKinds").EnumerateArray().Select(kind => kind.GetString())).ToHashSet(StringComparer.Ordinal);
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) Assert.Contains(kind.ToString(), represented);

        using var galleryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(repository, "Website", "data", "gallery.json")));
        var entries = galleryDocument.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var suppliedAssets = galleryDocument.RootElement.GetProperty("assets").EnumerateArray().Select(asset => asset.GetString()!).ToHashSet(StringComparer.Ordinal);
        foreach (var asset in document.RootElement.GetProperty("assets").EnumerateArray())
            Assert.Contains("/examples/generated-v2/" + asset.GetString(), suppliedAssets);
        Assert.Equal(primary.Length, entries.Length);
        foreach (var artifact in primary) {
            var image = "/examples/generated-v2/" + artifact.GetProperty("svg").GetString();
            var entry = Assert.Single(entries, entry => entry.GetProperty("image").GetString() == image);
            Assert.Equal(artifact.GetProperty("title").GetString(), entry.GetProperty("title").GetString());
            Assert.Equal(artifact.GetProperty("group").GetString(), entry.GetProperty("category").GetString());
            Assert.EndsWith(".thumbnail.png", entry.GetProperty("thumbnail").GetString(), StringComparison.Ordinal);
            Assert.EndsWith(".thumbnail.png", entry.GetProperty("darkThumbnail").GetString(), StringComparison.Ordinal);
            if (artifacts.Any(candidate => candidate.GetProperty("family").GetString() == artifact.GetProperty("family").GetString()
                && candidate.GetProperty("variant").GetString() == "compact")) {
                foreach (var field in new[] { "compactHtml", "darkCompactHtml" }) {
                    var compactHtml = entry.GetProperty(field).GetString()!;
                    Assert.Contains("-compact-", compactHtml, StringComparison.Ordinal);
                    Assert.True(File.Exists(Path.Combine(repository, "Website", "static", compactHtml.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
                }
            }
            var darkImage = entry.GetProperty("darkImage").GetString()!;
            Assert.True(File.Exists(Path.Combine(repository, "Website", "static", darkImage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
            var thumbnail = XDocument.Load(Path.Combine(output, artifact.GetProperty("thumbnail").GetString()!)).Root!;
            Assert.Equal("0 0 400 280", (string?)thumbnail.Attribute("viewBox"));
            Assert.Equal(artifact.GetProperty("title").GetString(), thumbnail.Elements().Single(element => element.Name.LocalName == "title").Value);
        }
    }

    private static string FindRepository() {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) return directory.FullName;
        throw new InvalidOperationException("The gallery artifact contract requires a source checkout.");
    }
}
