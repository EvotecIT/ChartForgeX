using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the pyramid value encoding, direction and shape settings shared by static exports.</summary>
    public ChartPyramidOptions Pyramid {
        get => _pyramid;
        set => _pyramid = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartPyramidOptions _pyramid = new();
}
