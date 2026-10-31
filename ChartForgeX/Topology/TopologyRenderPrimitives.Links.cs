namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static string? SafeHref(string? href) => Core.ChartHyperlink.SafeHref(href, allowTelephone: true, allowWhitespace: true);
}
