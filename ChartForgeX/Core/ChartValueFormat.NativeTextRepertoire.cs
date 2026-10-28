using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartValueFormat {
    /// <summary>Observes only the finite invariant branches of this existing formatter; it never invokes a custom callback.</summary>
    internal bool TryGetInvariantTextRepertoire(out string owner, out IReadOnlyList<string> observations) {
        owner = string.Empty; observations = Array.Empty<string>();
        if (_formatter != null || _notation is not (Notation.Compact or Notation.ExistingValue) || Culture.Name.Length != 0) return false;
        try { _ = PortablePolicy(); } catch (NotSupportedException) { return false; }
        // These values reach the existing decimal/grouping/suffix/automatic-significant branches.
        // The observations are actual Format results, rather than an alternate character formatter.
        var values = InvariantNativeWitnesses();
        observations = Array.AsReadOnly(values.Concat(values.Where(value => value != 0).Select(value => -value))
            .Select(Format).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        owner = "value-format-invariant-" + _notation; return true;
    }
    private static double[] InvariantNativeWitnesses() => new[] { 0d, 1, 2, 3, 4, 5, 6, 7, 8, 9, 12.3, 123.45, 1234.5, 9999.99,
            10000, 12345.6, 1234567.8, 1234567890, 1e-8, 1e-200, double.Epsilon, double.MaxValue };
}
