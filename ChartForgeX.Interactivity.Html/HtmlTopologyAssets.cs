using System;
using System.IO;

namespace ChartForgeX.Interactivity.Html;

internal static class HtmlTopologyAssets {
    private const string InteractionScriptResourceName = "ChartForgeX.Interactivity.Html.Assets.topology-interaction.js";
    private const string IconStencilBrowserStyleResourceName = "ChartForgeX.Interactivity.Html.Assets.topology-icon-stencil-browser.css";
    private const string IconStencilBrowserScriptResourceName = "ChartForgeX.Interactivity.Html.Assets.topology-icon-stencil-browser.js";

    private static readonly Lazy<string> InteractionScriptResource = new(() => ReadResource(InteractionScriptResourceName));
    private static readonly Lazy<string> IconStencilBrowserStyleResource = new(() => ReadResource(IconStencilBrowserStyleResourceName));
    private static readonly Lazy<string> IconStencilBrowserScriptResource = new(() => ReadResource(IconStencilBrowserScriptResourceName));

    internal static string InteractionScript => InteractionScriptResource.Value;
    internal static string IconStencilBrowserStyle => IconStencilBrowserStyleResource.Value;
    internal static string IconStencilBrowserScript => IconStencilBrowserScriptResource.Value;

    private static string ReadResource(string resourceName) {
        var assembly = typeof(HtmlTopologyAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new InvalidOperationException("Embedded interactive topology asset not found: " + resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
