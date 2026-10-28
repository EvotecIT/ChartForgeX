using System;
using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Core;

/// <summary>Materializes complete finite pairs once, using the same Gap/Connect vocabulary as ordinary nullable points.</summary>
internal static partial class ChartPairedSamples {
    internal static T[] FromXY<T>(IEnumerable<double> x, IEnumerable<double?> first, IEnumerable<double?> second,
        ChartMissingDataPolicy policy, Func<double, double, double, bool, T> create, CancellationToken cancellationToken) {
        if (x == null) throw new ArgumentNullException(nameof(x));
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));
        ChartSamplePoints.ValidatePolicy(policy);
        cancellationToken.ThrowIfCancellationRequested();
        var result = new List<T>(); var missing = false;
        using (var positions = x.GetEnumerator())
        using (var a = first.GetEnumerator())
        using (var b = second.GetEnumerator()) {
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                var hasX = positions.MoveNext(); var hasFirst = a.MoveNext(); var hasSecond = b.MoveNext();
                if (hasX != hasFirst || hasX != hasSecond) throw new ArgumentException("X and both boundaries must have the same number of sample slots.");
                if (!hasX) break;
                ChartGuards.Finite(positions.Current, nameof(x));
                if (a.Current.HasValue) ChartGuards.Finite(a.Current.Value, nameof(first));
                if (b.Current.HasValue) ChartGuards.Finite(b.Current.Value, nameof(second));
                if (!a.Current.HasValue || !b.Current.HasValue) { missing = true; continue; }
                result.Add(create(positions.Current, a.Current.Value, b.Current.Value, missing && policy == ChartMissingDataPolicy.Gap && result.Count > 0));
                missing = false;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return result.ToArray();
    }
}
