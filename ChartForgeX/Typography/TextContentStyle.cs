using System;

namespace ChartForgeX.Typography;

/// <summary>Resolves safe run emphasis over a detached native text policy.</summary>
internal static class TextContentStyle {
    internal static TextStyle Resolve(TextStyle baseStyle, TextInlineEmphasis emphasis) {
        var style = (baseStyle ?? throw new ArgumentNullException(nameof(baseStyle))).Clone();
        if ((emphasis & TextInlineEmphasis.Bold) != 0) style.Font.Weight = Math.Max(700, style.Font.Weight);
        if ((emphasis & TextInlineEmphasis.Italic) != 0) style.Font.Italic = true;
        return style;
    }
}
