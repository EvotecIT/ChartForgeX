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

    private static void GraphExplorerCanvasConsumersPreferLivePhysicsState() {
        var assetRoot = Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "Assets");
        var bindings = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.30-bindings.js"));
        var pointers = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.27-pointer-interactions.js"));
        const string liveState = "root.__cfxGraphState || graphState(root)";
        var liveStateUses = (bindings + pointers).Split(new[] { liveState }, StringSplitOptions.None).Length - 1;
        Assert(liveStateUses >= 7, "Canvas dragging and SVG, PNG, and JSON exports should consume live physics coordinates before falling back to hidden SVG attributes.");
        Assert(bindings.Contains("indexHitTesting(root, graphState(root))", StringComparison.Ordinal), "Initial graph binding should still build a fresh state before the live cache exists.");
    }

    private static void GraphExplorerRuntimePatchesPreserveVisualAndReferenceContracts() {
        var api = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "Assets", "graph-explorer.40-api.js"));
        Assert(api.Contains("edges.forEach((edge, edgeId)", StringComparison.Ordinal) && api.Contains("references a missing endpoint", StringComparison.Ordinal), "Browser graph patches should validate every surviving edge against the final node set before mutating the document.");
        Assert(api.Contains("graphPatchPolygonPoints", StringComparison.Ordinal) && api.Contains("graphPatchDatabasePath", StringComparison.Ordinal) && api.Contains("shape === 'text'", StringComparison.Ordinal), "Browser graph patches should rebuild rich SVG node shapes instead of reducing them to circles.");
        Assert(api.Contains("setGraphAttribute(element, 'marker-start'", StringComparison.Ordinal) && api.Contains("setGraphAttribute(element, 'marker-end'", StringComparison.Ordinal), "Browser graph patches should restore SVG direction markers for upserted edges.");
        Assert(api.Contains("syncGraphPatchClusterMembership", StringComparison.Ordinal) && api.Contains("data-source-cluster-id", StringComparison.Ordinal) && api.Contains("data-target-cluster-id", StringComparison.Ordinal), "Browser graph patches should synchronize node moves across cluster memberships and edge cluster references.");
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
