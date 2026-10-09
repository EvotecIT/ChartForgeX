using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Exercises publication ownership through the maintained PowerShell entry point.</summary>
public sealed class GallerySyncTests {
    [Fact]
    public void RefreshRetiresCatalogAndPromotedAssetsAndPreservesUnmanagedBytes() {
        var repository = FindRepository();
        var temporary = Directory.CreateTempSubdirectory("cfx-gallery-sync-").FullName;
        try {
            var (source, scenarios, destination, metadata, promoted, assets) = CreateFixture(repository, temporary);
            RunSync(repository, source, scenarios, destination, metadata, promoted);

            var unmanaged = Path.Combine(destination, "unmanaged.txt");
            var originalBytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("User text\r\nΩ\r\n")).ToArray();
            File.WriteAllBytes(unmanaged, originalBytes);
            Directory.CreateDirectory(Path.Combine(destination, "fonts"));
            File.WriteAllText(Path.Combine(destination, "fonts", "retired.ttf"), "old generated font fixture");
            var priorManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(destination, "manifest.json")))!;
            priorManifest["assets"]!.AsArray().Add("fonts/retired.ttf");
            File.WriteAllText(Path.Combine(destination, "manifest.json"), priorManifest.ToJsonString());
            File.WriteAllText(promoted, "{\"cases\":[]}");
            RunSync(repository, source, scenarios, destination, metadata, promoted);

            Assert.Equal(originalBytes, File.ReadAllBytes(unmanaged));
            Assert.False(File.Exists(Path.Combine(destination, "retired-scenario.html")));
            Assert.False(File.Exists(Path.Combine(destination, "fonts", "retired.ttf")));
            var actualMetadata = JsonNode.Parse(File.ReadAllText(metadata))!;
            Assert.Equal(assets.Count, actualMetadata["assets"]!.AsArray().Count);
            foreach (var asset in assets) Assert.True(File.Exists(Path.Combine(destination, asset)), asset);
        } finally {
            Directory.Delete(temporary, recursive: true);
        }
    }

    [Theory]
    [InlineData("catalog.html")]
    [InlineData("retired-scenario.html")]
    [InlineData("fonts")]
    public void RefreshRejectsDestinationLinksBeforeWriting(string linkedAsset) {
        var repository = FindRepository();
        var temporary = Directory.CreateTempSubdirectory("cfx-gallery-links-").FullName;
        var link = Path.Combine(temporary, "published", linkedAsset);
        try {
            var (source, scenarios, destination, metadata, promoted, _) = CreateFixture(repository, temporary);
            Directory.CreateDirectory(destination);
            var outside = Directory.CreateDirectory(Path.Combine(temporary, "outside-output")).FullName;
            var sentinel = Path.Combine(outside, "catalog.html");
            File.WriteAllText(sentinel, "Unrelated content");
            CreateDirectoryLink(link, outside);

            var result = RunSync(repository, source, scenarios, destination, metadata, promoted, expectSuccess: false);
            Assert.Contains("filesystem link", result, StringComparison.Ordinal);
            Assert.Equal("Unrelated content", File.ReadAllText(sentinel));
            Assert.Single(Directory.GetFiles(outside));
            Assert.False(File.Exists(Path.Combine(destination, "manifest.json")));
            Assert.False(File.Exists(metadata));
        } finally {
            if (Directory.Exists(link)) Directory.Delete(link);
            Directory.Delete(temporary, recursive: true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshAllowsDirectoryAliasAboveExplicitOutputBoundary(bool trailingSeparator) {
        var repository = FindRepository();
        var temporary = Directory.CreateTempSubdirectory("cfx-gallery-alias-").FullName;
        var alias = Path.Combine(temporary, "alias");
        try {
            var (source, scenarios, _, metadata, promoted, _) = CreateFixture(repository, temporary);
            var physical = Directory.CreateDirectory(Path.Combine(temporary, "physical")).FullName;
            CreateDirectoryLink(alias, physical);
            var destination = Path.Combine(alias, "published") + (trailingSeparator ? Path.DirectorySeparatorChar : "");
            RunSync(repository, source, scenarios, destination, metadata, promoted);
            Assert.True(File.Exists(Path.Combine(physical, "published", "catalog.html")));
            Assert.True(File.Exists(Path.Combine(physical, "published", "retired-scenario.html")));
        } finally {
            if (Directory.Exists(alias)) Directory.Delete(alias);
            Directory.Delete(temporary, recursive: true);
        }
    }

    [Fact]
    public void RefreshAllowsSameSourceAndDestinationWithTrailingSeparator() {
        var repository = FindRepository();
        var temporary = Directory.CreateTempSubdirectory("cfx-gallery-in-place-").FullName;
        try {
            var (source, scenarios, _, metadata, promoted, assets) = CreateFixture(repository, temporary);
            var directory = source + Path.DirectorySeparatorChar;
            RunSync(repository, directory, scenarios, directory, metadata, promoted);
            foreach (var asset in assets) Assert.True(File.Exists(Path.Combine(source, asset)), asset);
            Assert.True(File.Exists(Path.Combine(source, "retired-scenario.html")));
            Assert.Equal(assets.Count + 1, JsonNode.Parse(File.ReadAllText(metadata))!["assets"]!.AsArray().Count);
        } finally {
            Directory.Delete(temporary, recursive: true);
        }
    }

    private static (string Source, string Scenarios, string Destination, string Metadata, string Promoted, List<string> Assets) CreateFixture(string repository, string temporary) {
        var source = Path.Combine(temporary, "source");
        var scenarios = Path.Combine(temporary, "scenarios");
        var destination = Path.Combine(temporary, "published");
        var metadata = Path.Combine(temporary, "metadata", "gallery.json");
        var promoted = Path.Combine(temporary, "promoted.json");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(scenarios);
        var published = Path.Combine(repository, "Website", "static", "examples", "generated");
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(published, "manifest.json")))!;
        var light = manifest["artifacts"]!.AsArray().First(item => item!["primary"]!.GetValue<bool>() && item["theme"]!.GetValue<string>() == "light")!;
        var darkId = light["id"]!.GetValue<string>().Replace("-light", "-dark", StringComparison.Ordinal);
        var dark = manifest["artifacts"]!.AsArray().Single(item => item!["id"]!.GetValue<string>() == darkId)!;
        var assets = new List<string> { "manifest.json", "catalog.html", "fonts/Carlito-Regular.ttf" };
        foreach (var artifact in new[] { light, dark })
            foreach (var field in new[] { "svg", "png", "html", "source", "thumbnail", "thumbnailPng" })
                assets.Add(artifact[field]!.GetValue<string>());
        foreach (var asset in assets.Where(asset => asset != "manifest.json")) {
            var target = Path.Combine(source, asset);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(published, asset), target);
        }
        manifest["artifacts"] = new JsonArray(light.DeepClone(), dark.DeepClone());
        manifest["assets"] = new JsonArray(assets.Select(asset => (JsonNode?)JsonValue.Create(asset)).ToArray());
        File.WriteAllText(Path.Combine(source, "manifest.json"), manifest.ToJsonString());
        File.WriteAllText(Path.Combine(scenarios, "retired-scenario.html"), "<p>Scenario</p>");
        File.WriteAllText(promoted, "{\"cases\":[{\"artifacts\":{\"html\":\"/examples/generated/retired-scenario.html\"}}]}");
        return (source, scenarios, destination, metadata, promoted, assets);
    }

    private static void CreateDirectoryLink(string link, string target) {
        var kind = OperatingSystem.IsWindows() ? "Junction" : "SymbolicLink";
        var start = new ProcessStartInfo("pwsh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "-NoProfile", "-Command", $"New-Item -ItemType {kind} -Path '{link.Replace("'", "''")}' -Target '{target.Replace("'", "''")}' | Out-Null" })
            start.ArgumentList.Add(argument);
        RunProcess(start, expectSuccess: true);
    }

    private static string RunSync(string repository, string source, string scenarios, string destination, string metadata, string promoted, bool expectSuccess = true) {
        var start = new ProcessStartInfo("pwsh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "-NoProfile", "-File", Path.Combine(repository, "Website", "build", "Sync-GeneratedExamples.ps1"),
            "-SourceRoot", source, "-ScenarioSourceRoot", scenarios, "-DestinationRoot", destination, "-GalleryPath", metadata, "-PromotedCasesPath", promoted })
            start.ArgumentList.Add(argument);
        return RunProcess(start, expectSuccess);
    }

    private static string RunProcess(ProcessStartInfo start, bool expectSuccess) {
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the gallery sync.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(90000)) {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new TimeoutException("The isolated gallery sync did not finish.");
        }
        var result = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
        Assert.True((process.ExitCode == 0) == expectSuccess, result);
        return result;
    }

    private static string FindRepository() {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) return directory.FullName;
        throw new InvalidOperationException("Gallery sync validation requires the source checkout.");
    }
}
