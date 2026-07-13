using System;
using System.IO;

namespace ChartForgeX.Topology;

internal static class TopologyHtmlAssets
{
    private const string StyleResourceName = "ChartForgeX.Topology.Assets.topology.css";

    private static readonly Lazy<string> StyleResource = new Lazy<string>(() => ReadResource(StyleResourceName));

    internal static string Style => StyleResource.Value;

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
