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
            var assets = new List<string> { "manifest.json", "catalog.html" };
            foreach (var artifact in new[] { light, dark })
                foreach (var field in new[] { "svg", "png", "html", "source", "thumbnail", "thumbnailPng" })
                    assets.Add(artifact[field]!.GetValue<string>());
            foreach (var asset in assets.Where(asset => asset != "manifest.json"))
                File.Copy(Path.Combine(published, asset), Path.Combine(source, asset));
            manifest["artifacts"] = new JsonArray(light.DeepClone(), dark.DeepClone());
            manifest["assets"] = new JsonArray(assets.Select(asset => (JsonNode?)JsonValue.Create(asset)).ToArray());
            File.WriteAllText(Path.Combine(source, "manifest.json"), manifest.ToJsonString());
            File.WriteAllText(Path.Combine(scenarios, "retired-scenario.html"), "<p>Scenario</p>");
            File.WriteAllText(promoted, "{\"cases\":[{\"artifacts\":{\"html\":\"/examples/generated/retired-scenario.html\"}}]}");
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

    private static void RunSync(string repository, string source, string scenarios, string destination, string metadata, string promoted) {
        var start = new ProcessStartInfo("pwsh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "-NoProfile", "-File", Path.Combine(repository, "Website", "build", "Sync-GeneratedExamples.ps1"),
            "-SourceRoot", source, "-ScenarioSourceRoot", scenarios, "-DestinationRoot", destination, "-GalleryPath", metadata, "-PromotedCasesPath", promoted })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the gallery sync.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(90000)) {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new TimeoutException("The isolated gallery sync did not finish.");
        }
        Assert.True(process.ExitCode == 0, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }

    private static string FindRepository() {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) return directory.FullName;
        throw new InvalidOperationException("Gallery sync validation requires the source checkout.");
    }
}
