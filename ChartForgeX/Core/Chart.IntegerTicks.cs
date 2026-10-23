namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Uses whole-number positions for generated linear X-axis ticks, including horizontal bar value axes.</summary>
    /// <param name="enabled">Whether to restrict generated linear ticks to integers.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXAxisIntegerTicks(bool enabled = true) { Options.XAxis.WithIntegerTicks(enabled); return this; }

    /// <summary>Uses whole-number positions for generated linear Y-axis ticks, such as counts in vertical charts.</summary>
    /// <param name="enabled">Whether to restrict generated linear ticks to integers.</param>
    /// <returns>The current chart.</returns>
    public Chart WithYAxisIntegerTicks(bool enabled = true) { Options.YAxis.WithIntegerTicks(enabled); return this; }

    /// <summary>Uses whole-number positions for generated linear secondary Y-axis ticks.</summary>
    /// <param name="enabled">Whether to restrict generated linear ticks to integers.</param>
    /// <returns>The current chart.</returns>
    public Chart WithSecondaryYAxisIntegerTicks(bool enabled = true) { Options.SecondaryYAxis.WithIntegerTicks(enabled); return this; }
}
