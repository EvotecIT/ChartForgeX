using System;

namespace ChartForgeX.Core;

/// <summary>Applies an option callback without exposing either the previous or installed state to the callback.</summary>
internal static class ChartOptionConfiguration {
    internal static T Configure<T>(T current, Action<T> configure, Func<T, T> clone) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var candidate = clone(current);
        configure(candidate);
        return clone(candidate);
    }
}
