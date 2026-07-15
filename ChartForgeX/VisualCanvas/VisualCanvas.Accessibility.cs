using System;
using ChartForgeX.Accessibility;

namespace ChartForgeX.Composition;

public sealed partial class VisualCanvas {
    /// <summary>Gets accessibility metadata shared by capable renderers and hosts.</summary>
    public VisualAccessibility Accessibility { get; } = new();

    /// <summary>Configures the canvas text alternative and language metadata.</summary>
    public VisualCanvas WithAccessibility(Action<VisualAccessibility> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Accessibility);
        return this;
    }

    /// <summary>Marks the canvas as decorative for capable renderers.</summary>
    public VisualCanvas AsDecorative(bool decorative = true) {
        Accessibility.IsDecorative = decorative;
        return this;
    }
}
