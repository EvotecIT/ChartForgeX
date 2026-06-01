using System;
using System.IO;

namespace ChartForgeX.Topology;

internal static class TopologyHtmlAssets
{
    private const string StyleResourceName = "ChartForgeX.Topology.Assets.topology.css";
    private const string InteractionScriptResourceName = "ChartForgeX.Topology.Assets.topology-interaction.js";
    private const string IconStencilBrowserStyleResourceName = "ChartForgeX.Topology.Assets.topology-icon-stencil-browser.css";
    private const string IconStencilBrowserScriptResourceName = "ChartForgeX.Topology.Assets.topology-icon-stencil-browser.js";

    private static readonly Lazy<string> StyleResource = new Lazy<string>(() => ReadResource(StyleResourceName));
    private static readonly Lazy<string> InteractionScriptResource = new Lazy<string>(() => ReadResource(InteractionScriptResourceName));
    private static readonly Lazy<string> IconStencilBrowserStyleResource = new Lazy<string>(() => ReadResource(IconStencilBrowserStyleResourceName));
    private static readonly Lazy<string> IconStencilBrowserScriptResource = new Lazy<string>(() => ReadResource(IconStencilBrowserScriptResourceName));

    internal static string Style => StyleResource.Value;
    internal static string InteractionScript => InteractionScriptResource.Value;
    internal static string IconStencilBrowserStyle => IconStencilBrowserStyleResource.Value;
    internal static string IconStencilBrowserScript => IconStencilBrowserScriptResource.Value;

    private static string ReadResource(string resourceName)
    {
        var assembly = typeof(TopologyHtmlAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException("Embedded topology HTML asset not found: " + resourceName);
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
