using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PromotedDemoManifestStaysAccessibleAndSourceLinked() {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "Website", "static", "examples", "promoted-cases.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifest = document.RootElement;

        Assert(manifest.GetProperty("schemaVersion").GetInt32() == 1, "Promoted demo manifest should use the current schema version.");
        Assert(manifest.GetProperty("projectSlug").GetString() == "chartforgex", "Promoted demo manifest should identify the project slug used by the central gallery.");
        Assert(manifest.GetProperty("$schema").GetString() == "https://evotec.xyz/schemas/project-demos.schema.json", "Promoted demo manifest should reference the reusable Evotec project-demo schema.");
        Assert(manifest.GetProperty("catalogUrl").GetString() == "https://chartforgex.evotec.xyz/gallery/", "Promoted demo manifest should point readers from the curated tour to the complete maintained catalog.");

        var assetRootValue = manifest.GetProperty("assetRoot").GetString();
        Assert(!string.IsNullOrWhiteSpace(assetRootValue), "Promoted demo manifest should declare its source asset root.");
        var assetRoot = Path.GetFullPath(Path.Combine(root, assetRootValue!.Replace('/', Path.DirectorySeparatorChar)));
        Assert(Directory.Exists(assetRoot), "Promoted demo asset root should exist: " + assetRootValue);

        var categoryIds = manifest.GetProperty("categories")
            .EnumerateArray()
            .Select(category => category.GetProperty("id").GetString() ?? string.Empty)
            .ToArray();
        Assert(categoryIds.Length > 0 && categoryIds.All(id => !string.IsNullOrWhiteSpace(id)), "Promoted demo categories should have stable IDs.");
        Assert(categoryIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() == categoryIds.Length, "Promoted demo category IDs should be unique.");
        var knownCategories = categoryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cases = manifest.GetProperty("cases").EnumerateArray().ToArray();
        Assert(cases.Length >= 8, "Promoted demo gallery should retain a representative cross-surface tour.");
        var caseIds = cases.Select(item => item.GetProperty("id").GetString() ?? string.Empty).ToArray();
        Assert(caseIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() == caseIds.Length, "Promoted demo IDs should be unique.");

        foreach (var item in cases) {
            var id = item.GetProperty("id").GetString() ?? string.Empty;
            var title = item.GetProperty("title").GetString() ?? string.Empty;
            var summary = item.GetProperty("summary").GetString() ?? string.Empty;
            var alt = item.GetProperty("alt").GetString() ?? string.Empty;
            var useCase = item.GetProperty("useCase").GetString() ?? string.Empty;
            var category = item.GetProperty("category").GetString() ?? string.Empty;

            Assert(!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(title), "Promoted demos should have stable IDs and reader-facing titles.");
            Assert(knownCategories.Contains(category), "Promoted demo should reference a declared category: " + id);
            Assert(summary.Length >= 24, "Promoted demo summary should explain the visual rather than repeat its title: " + id);
            Assert(alt.Length >= 24 && !alt.Equals(title, StringComparison.OrdinalIgnoreCase), "Promoted demo preview should have meaningful alternative text: " + id);
            Assert(useCase.Length >= 24, "Promoted demo should explain a concrete reader use case: " + id);

            var capabilities = item.GetProperty("capabilities").EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToArray();
            Assert(capabilities.Length > 0 && capabilities.All(value => !string.IsNullOrWhiteSpace(value)), "Promoted demo should name the capabilities it proves: " + id);
            Assert(capabilities.Distinct(StringComparer.OrdinalIgnoreCase).Count() == capabilities.Length, "Promoted demo capabilities should not repeat: " + id);

            var source = item.GetProperty("source");
            var sourcePathValue = source.GetProperty("path").GetString() ?? string.Empty;
            var sourcePath = Path.GetFullPath(Path.Combine(root, sourcePathValue.Replace('/', Path.DirectorySeparatorChar)));
            Assert(File.Exists(sourcePath), "Promoted demo source path should exist: " + sourcePathValue);
            var sourceEntry = source.GetProperty("entry").GetString() ?? string.Empty;
            var entryName = sourceEntry.Split('(')[0].Split('.').LastOrDefault() ?? string.Empty;
            Assert(!string.IsNullOrWhiteSpace(entryName) && File.ReadAllText(sourcePath).Contains(entryName, StringComparison.Ordinal), "Promoted demo source entry should identify code in the declared source file: " + id);
            var sourceUrl = source.GetProperty("url").GetString() ?? string.Empty;
            Assert(sourceUrl.StartsWith("https://github.com/EvotecIT/ChartForgeX/", StringComparison.OrdinalIgnoreCase), "Promoted demo source URL should stay on the public ChartForgeX repository: " + id);
            Assert(sourceUrl.Contains(sourcePathValue.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase), "Promoted demo source URL should identify the same source file as source.path: " + id);

            var artifacts = item.GetProperty("artifacts");
            Assert(artifacts.GetProperty("width").GetInt32() > 0 && artifacts.GetProperty("height").GetInt32() > 0, "Promoted demo preview dimensions should be explicit: " + id);
            foreach (var artifactName in new[] { "preview", "html", "svg", "png", "webp", "code" }) {
                if (!artifacts.TryGetProperty(artifactName, out var artifactValue) || artifactValue.ValueKind != JsonValueKind.String)
                    continue;
                var artifactPath = ResolvePromotedDemoArtifactPath(assetRoot, artifactValue.GetString(), id, artifactName);
                Assert(File.Exists(artifactPath), "Promoted demo artifact should exist: " + id + " -> " + artifactName);
            }
        }

        using var projectManifestDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "WebsiteArtifacts", "project-manifest.json")));
        var demosPath = projectManifestDocument.RootElement.GetProperty("artifacts").GetProperty("demos").GetString();
        Assert(demosPath == "Website/static/examples/promoted-cases.json", "Project manifest should expose the reusable promoted demo artifact explicitly.");
    }

    private static string ResolvePromotedDemoArtifactPath(string assetRoot, string? value, string id, string artifactName) {
        Assert(!string.IsNullOrWhiteSpace(value) && value!.StartsWith("/", StringComparison.Ordinal), "Promoted demo artifact paths should be root-relative: " + id + " -> " + artifactName);
        var relative = value!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var resolved = Path.GetFullPath(Path.Combine(assetRoot, relative));
        var normalizedRoot = assetRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Assert(resolved.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase), "Promoted demo artifact should stay under assetRoot: " + id + " -> " + artifactName);
        return resolved;
    }
}
