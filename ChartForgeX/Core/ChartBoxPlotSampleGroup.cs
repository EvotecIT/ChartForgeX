using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>One category's immutable measurements in their original input order.</summary>
public sealed class ChartBoxPlotSampleGroup {
    /// <summary>Creates a category from a nonempty sequence of finite measurements.</summary>
    public ChartBoxPlotSampleGroup(double x, IEnumerable<double> samples) {
        ChartGuards.Finite(x, nameof(x));
        if (samples == null) throw new ArgumentNullException(nameof(samples));
        var snapshot = samples.ToArray();
        if (snapshot.Length == 0) throw new ArgumentException("A sample group must contain at least one measurement.", nameof(samples));
        foreach (var sample in snapshot) ChartGuards.Finite(sample, nameof(samples));
        X = x; Samples = Array.AsReadOnly(snapshot);
    }

    /// <summary>Gets the category coordinate.</summary>
    public double X { get; }
    /// <summary>Gets copied, unsorted measurements.</summary>
    public IReadOnlyList<double> Samples { get; }
}
