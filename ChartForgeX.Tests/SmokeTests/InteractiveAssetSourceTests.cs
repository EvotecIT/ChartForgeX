using System;
using System.IO;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void InteractiveJavaScriptAssetsStayGeneratedFromSourceFragments() {
        var root = FindRepositoryRoot();
        var syncScript = Path.Combine(root, "Build", "Sync-InteractiveAssets.ps1");
        Assert(File.Exists(syncScript), "Interactive JS assets should include a dependency-free source sync script.");

        AssertGeneratedAssetMatchesSource(
            root,
            Path.Combine("ChartForgeX.Interactivity.Html", "Assets", "topology-interaction.source"),
            Path.Combine("ChartForgeX.Interactivity.Html", "Assets", "topology-interaction.js"));

        AssertGeneratedAssetMatchesSource(
            root,
            Path.Combine("ChartForgeX.Interactivity.Html", "Assets", "interactive.source"),
            Path.Combine("ChartForgeX.Interactivity.Html", "Assets", "interactive.js"));
    }

    private static void AssertGeneratedAssetMatchesSource(string root, string sourceRelativePath, string targetRelativePath) {
        var sourceDirectory = Path.Combine(root, sourceRelativePath);
        var targetPath = Path.Combine(root, targetRelativePath);
        Assert(Directory.Exists(sourceDirectory), "Interactive JS source fragments should exist: " + sourceRelativePath);
        Assert(File.Exists(targetPath), "Interactive JS generated output should exist: " + targetRelativePath);

        var parts = Directory.EnumerateFiles(sourceDirectory, "*.js", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        Assert(parts.Length >= 3, "Interactive JS assets should be split into maintainable source fragments: " + sourceRelativePath);

        var generated = string.Join("\n", parts.Select(part => NormalizeAsset(File.ReadAllText(part)).TrimEnd('\n'))) + "\n";
        var current = NormalizeAsset(File.ReadAllText(targetPath));
        Assert(current == generated, "Generated interactive JS asset is out of date: " + targetRelativePath + ". Run Build/Sync-InteractiveAssets.ps1.");
    }

    private static string NormalizeAsset(string value) => value.Replace("\r\n", "\n").Replace("\r", "\n");
}
