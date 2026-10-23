namespace ChartForgeX.Topology;

/// <summary>Chooses the source of topology node accent paint at render time.</summary>
public enum TopologyAccentSource {
    /// <summary>Explicit color, icon color, palette, then status (the default).</summary>
    Auto,
    /// <summary>Health status, retaining its theme token and Status paint role.</summary>
    Status,
    /// <summary>Icon color when available, otherwise status.</summary>
    Icon,
    /// <summary>Explicit node color or palette, otherwise status; ignores icon colors.</summary>
    Explicit
}
