using System;
using ChartForgeX.Accessibility;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Gets accessibility metadata shared by capable renderers and hosts.</summary>
    public VisualAccessibility Accessibility {
        get => _accessibility;
        set => _accessibility = value ?? throw new ArgumentNullException(nameof(value));
    }
    private VisualAccessibility _accessibility = new();
}
