using System;
using System.IO;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlDiagramAssets {
    private static readonly Lazy<string> ScriptResource = new(() => Read("adapter-lifetime.js") + "\n"
        + Read("adapter-native-text-export.js") + "\n" + Read("diagram-interaction.js"));
    private static readonly Lazy<string> StyleResource = new(() => Read("diagram-interaction.css"));

    internal static string Script => ScriptResource.Value;
    internal static string Style => StyleResource.Value;

    private static string Read(string name) {
        var resourceName = "ChartForgeX.Interactivity.Html.Assets." + name;
        using var stream = typeof(HtmlDiagramAssets).Assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new InvalidOperationException("Embedded interactive diagram asset not found: " + resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
