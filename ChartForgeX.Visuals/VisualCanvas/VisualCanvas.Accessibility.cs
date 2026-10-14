using System;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;

namespace ChartForgeX.Composition;

public sealed partial class VisualCanvas {
    /// <summary>Gets accessibility metadata shared by capable renderers and hosts.</summary>
    public VisualAccessibility Accessibility { get; private set; } = new();

    /// <summary>Installs an independent canvas text alternative after successful configuration.</summary>
    /// <remarks>Callback exceptions preserve the owner. Previous metadata and retained callback values remain independent of the installed result.</remarks>
    public VisualCanvas ConfigureAccessibility(Action<VisualAccessibility> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Accessibility = ChartOptionConfiguration.Configure(Accessibility, configure, options => options.Clone());
        return this;
    }

    /// <summary>Marks the canvas as decorative for capable renderers.</summary>
    public VisualCanvas AsDecorative(bool decorative = true) {
        Accessibility.IsDecorative = decorative;
        return this;
    }
}
