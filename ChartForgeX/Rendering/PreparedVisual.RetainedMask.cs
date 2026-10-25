using System;
using ChartForgeX.Effects;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Returns a detached complete-root mask presentation in this fixed source viewport.</summary>
    /// <param name="mask">Immutable fixed-density input; native export density must match its grid.</param>
    /// <returns>A retained presentation with unchanged source facts, regions and projections.</returns>
    /// <remarks>NativeFilters keeps vector children behind one materialized alpha mask. Baked uses one canonical
    /// masked object. Complete-root policies join the one ordered portable CapturedEffects owner; model-only import
    /// rejects carried presentation until explicitly cleared on a detached source-facts copy.</remarks>
    public PreparedVisual WithMask(VisualMask mask) {
        if (mask == null) throw new ArgumentNullException(nameof(mask));
        return WithRootEffect(VisualCapturedEffect.From(mask));
    }

    internal bool HasUnsupportedInterchangePresentation => !TryGetRootEffects(out _);

    internal void RequireSupportedInterchangePresentation() {
        if (HasUnsupportedInterchangePresentation)
            throw new NotSupportedException("Captured effect presentation must be a bounded complete-root ordered chain. " +
                "Native rendering/source facts remain available; interchange cannot silently flatten or omit effects.");
    }
}
