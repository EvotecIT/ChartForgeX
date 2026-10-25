using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Plain native layout records every reached candidate through the same face request owner and replays without a resolver.</summary>
    internal TextLayout LayoutText(string text, double width, TextStyle style, TextWrapMode wrap = TextWrapMode.Word,
        int? maximumLines = null, TextTrimming trimming = TextTrimming.Ellipsis) {
        if (_textCapture == null) return TextLayoutEngine.Layout(text, width, style, wrap, maximumLines, trimming);
        var face = StyleFace(style);
        // Ordinary bitmap/default-font layout keeps its incumbent semantics. Such a face never enters complete captured replay admission.
        if (face.CapturedFace.Font == null && !_textCapture.IsReplay) return TextLayoutEngine.Layout(text, width, style, wrap, maximumLines, trimming);
        return TextLayoutEngine.LayoutCaptured(text, width, style, face.CapturedFace, displayed => {
            var run = face.PrepareDisplayed(displayed, style.EffectiveFontSize);
            if (run.Lines.Count != 1) throw new NotSupportedException("Plain layout candidates must be one native line.");
            return run.Lines[0].Glyphs;
        }, wrap, maximumLines, trimming);
    }
}
