namespace ChartForgeX.Topology;

public sealed partial class TopologyHtmlRenderer
{
    private static string InteractionScript(string cssPrefix)
    {
        return (InteractionScriptPart1() + "\n" + InteractionScriptPart2())
            .Replace(".cfx-topology-wrapper", "." + cssPrefix + "-wrapper")
            .Replace(".cfx-topology-viewport", "." + cssPrefix + "-viewport")
            .Replace("cfx-topology-html-", cssPrefix + "-html-");
    }
}
