using System;
using ChartForgeX.Accessibility;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Gets accessibility metadata shared by capable renderers and hosts.</summary>
    public VisualAccessibility Accessibility { get; } = new();

    /// <summary>Configures the chart text alternative and language metadata.</summary>
    public Chart WithAccessibility(Action<VisualAccessibility> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Accessibility);
        return this;
    }

    /// <summary>Marks the chart as decorative for capable renderers.</summary>
    public Chart AsDecorative(bool decorative = true) {
        Accessibility.IsDecorative = decorative;
        return this;
    }
}
