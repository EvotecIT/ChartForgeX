namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the pyramid value encoding, direction and shape settings shared by static exports.</summary>
    public ChartPyramidOptions Pyramid { get; } = new();
}
