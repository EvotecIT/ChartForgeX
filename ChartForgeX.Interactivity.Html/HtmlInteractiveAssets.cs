using System;
using System.IO;
using System.Reflection;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlInteractiveAssets {
    private const string StyleResourceName = "ChartForgeX.Interactivity.Html.Assets.interactive.css";
    private const string ScriptResourceName = "ChartForgeX.Interactivity.Html.Assets.interactive.js";

    private static readonly Lazy<string> StyleResource = new Lazy<string>(() => ReadResource(StyleResourceName));
    private static readonly Lazy<string> ScriptResource = new Lazy<string>(() => ReadResource(ScriptResourceName));

    internal static string Style => StyleResource.Value;

    internal static string Script => ScriptResource.Value;

    private static string ReadResource(string resourceName) {
        var assembly = typeof(HtmlInteractiveAssets).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) {
            throw new InvalidOperationException("Embedded interactive HTML asset was not found: " + resourceName);
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
