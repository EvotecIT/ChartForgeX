using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    // Only a freshly reconstructed private native view receives this same immutable root/membership request.
    internal ChartVisibilityView? NativeHierarchyVisibility { get; set; }
}
