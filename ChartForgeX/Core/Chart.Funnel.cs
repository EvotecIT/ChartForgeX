using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures proportional stage bars or cone stage lines and their orientation.</summary>
    /// <remarks>The options can be configured before or after adding the funnel series.</remarks>
    public Chart ConfigureFunnel(Action<ChartFunnelOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Options.Funnel = ChartOptionConfiguration.Configure(Options.Funnel, configure, options => options.Clone());
        return this;
    }
}
