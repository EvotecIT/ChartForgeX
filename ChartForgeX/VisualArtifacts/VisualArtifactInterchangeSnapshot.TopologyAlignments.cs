namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeSnapshot {
    private static VisualArtifactInterchangeTopologyAlignment CopyAlignment(VisualArtifactInterchangeTopologyAlignment source) {
        var copy = new VisualArtifactInterchangeTopologyAlignment { Id = source.Id, Kind = source.Kind };
        copy.NodeIds.AddRange(source.NodeIds);
        foreach (var fact in source.Extensions) copy.Extensions.Add(fact.Key, fact.Value);
        return copy;
    }
}
