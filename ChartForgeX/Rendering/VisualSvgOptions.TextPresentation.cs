using System;

namespace ChartForgeX.Rendering;

// Adapter-selected SVG representation only. Native layout, ordinary SVG and PNG do not change.
internal enum VisualSvgTextPresentation { Browser, Automatic, NativeContours }

public sealed partial class VisualSvgOptions {
    private VisualSvgTextPresentation _textPresentation;
    internal VisualSvgTextPresentation TextPresentation => _textPresentation;
    internal VisualSvgOptions WithTextPresentation(VisualSvgTextPresentation presentation) {
        if (!Enum.IsDefined(typeof(VisualSvgTextPresentation), presentation)) throw new ArgumentOutOfRangeException(nameof(presentation));
        var copy = Copy(); copy._textPresentation = presentation; return copy;
    }
}
