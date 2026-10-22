namespace ChartForgeX.Topology;

// Native paint and the browser graph projection share source precedence; null means semantic status paint.
internal static class TopologyNodeAccent {
    internal static string? Authored(TopologyNode node, TopologyIconDefinition? icon, TopologyAccentSource source) => source switch {
        TopologyAccentSource.Status => null,
        TopologyAccentSource.Icon => icon?.Color,
        TopologyAccentSource.Explicit => node.Color,
        _ => node.Color ?? icon?.Color
    };

    internal static bool UsesPalette(TopologyNode node, TopologyAccentSource source) => node.PaletteIndex.HasValue &&
        source is TopologyAccentSource.Auto or TopologyAccentSource.Explicit;
}
