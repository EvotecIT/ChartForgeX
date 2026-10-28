using System.Net.Http;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Pixel baselines for every chart family: the freshly generated gallery must match the committed website gallery,
/// which changes only in the PR that intentionally changes the visuals (Website/build/Sync-GeneratedExamples.ps1).
/// </summary>
public sealed class GalleryPixelBaselineBrowserTests {
    private static string BaselineRoot => Path.Combine(TestRepository.Root, "Website", "static", "examples", "generated");

    [Fact]
    public void Compare_IdenticalImages_HasNoDifferences() {
        var png = File.ReadAllBytes(Directory.GetFiles(BaselineRoot, "family-line-wide-light.png").Single());
        var result = PixelBaseline.Compare("same", png, png);
        Assert.Null(result.Failure);
        Assert.Equal(0, result.DifferingPixels);
    }

    [Fact]
    public void Compare_DifferentFamilies_FailsWithAReason() {
        var line = File.ReadAllBytes(Path.Combine(BaselineRoot, "family-line-wide-light.png"));
        var dark = File.ReadAllBytes(Path.Combine(BaselineRoot, "family-line-wide-dark.png"));
        Assert.NotNull(PixelBaseline.Compare("line-vs-dark", line, dark).Failure);
    }

    [BrowserFact(RequiresGallery = true)]
    public async Task GeneratedFamilyImages_MatchTheCommittedGallery() {
        var url = BrowserTestPrerequisites.RequireGalleryUrl();
        var gallery = new Uri(url.EndsWith("/", StringComparison.Ordinal) ? url : url[..(url.LastIndexOf('/') + 1)]);
        var diffs = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } captures ? Path.Combine(captures, "pixel-baselines") : null;
        using var client = new HttpClient();
        var failures = new List<string>(); var compared = 0;
        foreach (var path in Directory.GetFiles(BaselineRoot, "family-*.png").OrderBy(path => path, StringComparer.Ordinal)) {
            var name = Path.GetFileNameWithoutExtension(path);
            using var response = await client.GetAsync(new Uri(gallery, name + ".png"));
            if (!response.IsSuccessStatusCode) { failures.Add(name + ": not generated"); continue; }
            var result = PixelBaseline.Compare(name, await response.Content.ReadAsByteArrayAsync(), File.ReadAllBytes(path), diffs);
            compared++;
            if (result.Failure != null) failures.Add(name + ": " + result.Failure);
        }
        Assert.True(compared >= 200, "Expected the family gallery to provide at least 200 baselines; compared " + compared + ".");
        Assert.True(failures.Count == 0, failures.Count + " family image(s) differ from the committed gallery. Refresh it with Website/build/Sync-GeneratedExamples.ps1 only when the change is intended."
            + Environment.NewLine + string.Join(Environment.NewLine, failures.Take(40)));
    }
}
