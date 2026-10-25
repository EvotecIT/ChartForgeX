namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Sets immutable equal-width automatic Pie/Donut bands. Null restores the existing single/explicit-band contract.</summary>
    /// <param name="allocation">The normalized shared hole and spacing policy, or null.</param>
    /// <returns>The current chart.</returns>
    public Chart WithAutomaticRadialBands(ChartRadialBandAllocation? allocation) {
        Options.RadialBandAllocation = allocation; return this;
    }
}
