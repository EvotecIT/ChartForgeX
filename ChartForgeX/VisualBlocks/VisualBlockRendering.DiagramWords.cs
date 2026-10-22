using System;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    // Match the shared text layout owner's paragraph and space/tab word boundaries.
    internal static double DiagramWordWidth(string text, TextStyle style) {
        var maximum = 0.0;
        foreach (var paragraph in TextLineScanner.Enumerate(text))
            foreach (var word in paragraph.Read(text).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                maximum = Math.Max(maximum, TextLayoutEngine.Measure(word, style).Width);
        return maximum;
    }
}
