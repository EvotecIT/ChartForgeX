namespace ChartForgeX.Core;

/// <summary>How series without an explicit colour pick their theme palette colour.</summary>
public enum ChartSeriesColorAssignment {
    /// <summary>Series take distinct categorical colours in series order, extending the base palette with shades when needed.</summary>
    Order,

    /// <summary>
    /// Series with an explicit <see cref="ChartSeriesId"/> take a palette colour derived from that identifier, so the same series keeps its colour
    /// across linked charts unless identifiers collide in the base palette. Collisions resolve in identifier order;
    /// adding or removing a colliding identifier can change its assigned shade. Use explicit colours when that mapping must be fixed.
    /// </summary>
    Identity
}
