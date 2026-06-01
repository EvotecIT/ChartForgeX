namespace ChartForgeX.Topology;

public sealed partial class TopologyHtmlRenderer
{
    private static string InteractionScript(string cssPrefix)
    {
        var script = TopologyHtmlAssets.InteractionScript
            .Replace(".cfx-topology-wrapper", "." + cssPrefix + "-wrapper")
            .Replace(".cfx-topology-viewport", "." + cssPrefix + "-viewport")
            .Replace(".cfx-topology-controls", "." + cssPrefix + "-controls")
            .Replace(".cfx-topology-scenarios", "." + cssPrefix + "-scenarios")
            .Replace(".cfx-topology-scenario-panel", "." + cssPrefix + "-scenario-panel")
            .Replace(".cfx-topology-selection-panel", "." + cssPrefix + "-selection-panel")
            .Replace(".cfx-topology-force-controls", "." + cssPrefix + "-force-controls")
            .Replace("cfx-topology-html-", cssPrefix + "-html-");
        return "<script>\n" + script + "\n</script>";
    }
}
