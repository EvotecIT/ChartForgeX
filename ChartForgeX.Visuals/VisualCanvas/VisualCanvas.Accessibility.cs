using System;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;

namespace ChartForgeX.Composition;

public sealed partial class VisualCanvas {
    /// <summary>Gets accessibility metadata shared by capable renderers and hosts.</summary>
    public VisualAccessibility Accessibility {
        get => _accessibility;
        set => _accessibility = value ?? throw new ArgumentNullException(nameof(value));
    }
    private VisualAccessibility _accessibility = new();
}
