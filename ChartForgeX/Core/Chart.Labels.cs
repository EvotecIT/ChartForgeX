using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Localizes the words ChartForgeX writes into the chart on its own (see <see cref="ChartLabels"/>).</summary>
    /// <param name="configure">Sets the label texts.</param>
    /// <returns>The current chart.</returns>
    public Chart WithLabels(Action<ChartLabels> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.Labels);
        return this;
    }
}
