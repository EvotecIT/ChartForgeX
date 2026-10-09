using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the committed gallery handoff used by the project hub and static previews.</summary>
public sealed class V2GalleryArtifactTests {
    [Fact]
    public void PublishedCatalogKeepsOnePrimaryPerFamilyAndNamedNativePreviews() {
        var repository = FindRepository();
        var output = Path.Combine(repository, "Website", "static", "examples", "generated");
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
            Assert.Contains("/examples/generated/" + asset.GetString(), suppliedAssets);
        Assert.Equal(primary.Length, entries.Length);
        foreach (var artifact in primary) {
            var image = "/examples/generated/" + artifact.GetProperty("svg").GetString();
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

    [Fact]
    public void ExistingCatalogUrlAndReadmeTourUseCurrentDeclaredArtifacts() {
        var repository = FindRepository();
        var publicRoot = Path.Combine(repository, "Website", "static", "examples", "generated");
        var currentRoot = publicRoot;
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(currentRoot, "manifest.json")));
        var artifacts = manifest.RootElement.GetProperty("artifacts").EnumerateArray().ToArray();
        var expectedFamilies = artifacts.Where(artifact => artifact.GetProperty("primary").GetBoolean()
            && artifact.GetProperty("theme").GetString() == "light").Select(artifact => artifact.GetProperty("family").GetString()).ToHashSet();
        var catalog = File.ReadAllText(Path.Combine(publicRoot, "catalog.html"));
        var families = Regex.Matches(catalog, "data-gallery-family=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();
        Assert.Equal(expectedFamilies.Count, families.Length);
        Assert.True(expectedFamilies.SetEquals(families));
        using var gallery = JsonDocument.Parse(File.ReadAllText(Path.Combine(repository, "Website", "data", "gallery.json")));
        var declaredFiles = gallery.RootElement.GetProperty("assets").EnumerateArray()
            .Select(asset => Path.GetFullPath(Path.Combine(repository, "Website", "static", asset.GetString()!.TrimStart('/')))).ToHashSet();
        var publishedFiles = Directory.EnumerateFiles(publicRoot, "*", SearchOption.AllDirectories).Select(Path.GetFullPath).ToHashSet();
        Assert.True(declaredFiles.SetEquals(publishedFiles), "The published directory must contain exactly the declared catalog and promoted demo assets.");

        var readme = File.ReadAllText(Path.Combine(repository, "README.md"));
        var begin = readme.IndexOf("## Visual Tour", StringComparison.Ordinal);
        var end = readme.IndexOf("## Examples", begin, StringComparison.Ordinal);
        var tour = readme.Substring(begin, end - begin);
        var images = Regex.Matches(tour, "<img src=\"Website/static/examples/generated/([^\"]+)\"").ToArray();
        Assert.NotEmpty(images);
        foreach (var image in images) {
            var artifact = Assert.Single(artifacts, artifact => artifact.GetProperty("thumbnailPng").GetString() == image.Groups[1].Value);
            Assert.True(artifact.GetProperty("primary").GetBoolean());
            Assert.True(File.Exists(Path.Combine(currentRoot, image.Groups[1].Value)));
            var darkId = artifact.GetProperty("id").GetString()!.Replace("-light", "-dark", StringComparison.Ordinal);
            var dark = Assert.Single(artifacts, artifact => artifact.GetProperty("id").GetString() == darkId);
            Assert.Contains(Uri.EscapeDataString(artifact.GetProperty("html").GetString()!), tour, StringComparison.Ordinal);
            Assert.Contains(Uri.EscapeDataString(dark.GetProperty("html").GetString()!), tour, StringComparison.Ordinal);
        }
        var packageReadme = File.ReadAllText(Path.Combine(repository, "README.nuget.md"));
        foreach (var image in images)
            Assert.Contains("https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/" + image.Groups[1].Value, packageReadme, StringComparison.Ordinal);
    }

    private static string FindRepository() {
        for (var directory = new DirectoryInfo(TestRepository.Root); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) return directory.FullName;
        throw new InvalidOperationException("The gallery artifact contract requires a source checkout.");
    }
}
