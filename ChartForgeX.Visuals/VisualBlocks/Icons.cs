using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>The icon keys that metric and radial metric cards draw. Any other key draws the default mark.</summary>
public static class Icons {
    /// <summary>Food or intake: <c>fork-knife</c>.</summary>
    public static Icon ForkKnife { get; } = Icon.Named("fork-knife");

    /// <summary>Heat, burn or activity: <c>flame</c>.</summary>
    public static Icon Flame { get; } = Icon.Named("flame");

    /// <summary>Energy, activity or alert: <c>lightning</c>.</summary>
    public static Icon Lightning { get; } = Icon.Named("lightning");

    /// <summary>Water, hydration or liquid: <c>droplet</c>.</summary>
    public static Icon Droplet { get; } = Icon.Named("droplet");

    /// <summary>Walking, running or movement: <c>runner</c>.</summary>
    public static Icon Runner { get; } = Icon.Named("runner");

    /// <summary>Cycling: <c>bicycle</c>.</summary>
    public static Icon Bicycle { get; } = Icon.Named("bicycle");

    /// <summary>Person, member or user: <c>person</c>.</summary>
    public static Icon Person { get; } = Icon.Named("person");

    /// <summary>Every key in this list, in declaration order.</summary>
    public static IReadOnlyList<Icon> All { get; } = new[] { ForkKnife, Flame, Lightning, Droplet, Runner, Bicycle, Person };
}
