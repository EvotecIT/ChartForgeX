using System;
using System.IO;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlGraphExplorerAssets {
    private static readonly string[] StyleResourceNames = {
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.css",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.chrome.css"
    };
    private static readonly string[] ScriptResourceNames = {
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.00-core.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.01-document.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.02-geometry.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.03-canvas-nodes.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.03-canvas-details.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.04-webgl.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.05-viewport.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.06-theme.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.09-performance.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.10-layout.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.11-state-sync.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.12-overview.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.13-hierarchy.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.15-hit-testing.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.17-physics-profile.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.18-structural-physics.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.19-physics-telemetry.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.20-physics.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.21-physics-runtime.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.25-layout-quality.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.27-pointer-interactions.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.28-svg-export.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.29-selection.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.31-physics-configurator.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.40-api.js",
        "ChartForgeX.Interactivity.Html.Assets.graph-explorer.30-bindings.js"
    };

    private static readonly Lazy<string> StyleResource = new Lazy<string>(() => ReadResources(StyleResourceNames));
    private static readonly Lazy<string> ScriptResource = new Lazy<string>(() => ReadResources(ScriptResourceNames));

    internal static string Style => StyleResource.Value;

    internal static string Script => ScriptResource.Value;

    private static string ReadResources(string[] resourceNames) {
        var parts = new string[resourceNames.Length];
        for (var i = 0; i < resourceNames.Length; i++) parts[i] = ReadResource(resourceNames[i]).TrimEnd('\r', '\n');
        return string.Join("\n", parts) + "\n";
    }

    private static string ReadResource(string resourceName) {
        var assembly = typeof(HtmlGraphExplorerAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new InvalidOperationException("Embedded graph explorer asset was not found: " + resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
