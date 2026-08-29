using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Typography;

/// <summary>Defines the line pattern used for underline and strikethrough decorations.</summary>
public enum TextDecorationStyle {
    /// <summary>No decoration line.</summary>
    None,
    /// <summary>One solid line.</summary>
    Single,
    /// <summary>Two parallel solid lines.</summary>
    Double,
    /// <summary>A dotted line.</summary>
    Dotted,
    /// <summary>A dashed line.</summary>
    Dashed,
    /// <summary>A wavy line.</summary>
    Wavy
}

/// <summary>Defines vertical text placement.</summary>
public enum TextBaseline {
    /// <summary>Normal baseline and font size.</summary>
    Normal,
    /// <summary>Raised, reduced superscript text.</summary>
    Superscript,
    /// <summary>Lowered, reduced subscript text.</summary>
    Subscript
}

/// <summary>Defines culture-aware casing applied for display and measurement.</summary>
public enum TextCaseTransform {
    /// <summary>Preserve supplied text.</summary>
    None,
    /// <summary>Convert cased characters to uppercase.</summary>
    Uppercase,
    /// <summary>Convert cased characters to lowercase.</summary>
    Lowercase,
    /// <summary>Capitalize words.</summary>
    TitleCase,
    /// <summary>Lowercase text and capitalize the first cased character of each sentence.</summary>
    SentenceCase,
    /// <summary>Swap uppercase and lowercase characters.</summary>
    ToggleCase
}

internal static class TextDecorationMetrics {
    internal static double OuterExtent(TextDecorationStyle style, double thickness) => style switch {
        TextDecorationStyle.Double => thickness * 1.5,
        TextDecorationStyle.Wavy => thickness * 1.9,
        TextDecorationStyle.None => 0,
        _ => thickness * 0.5
    };
}

/// <summary>Applies the shared ChartForgeX text-case contract.</summary>
public static class TextCaseTransformer {
    /// <summary>Transforms text with the current culture unless another culture is supplied.</summary>
    public static string Apply(string text, TextCaseTransform transform, CultureInfo? culture = null) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var selectedCulture = culture ?? CultureInfo.CurrentCulture;
        switch (transform) {
            case TextCaseTransform.None: return text;
            case TextCaseTransform.Uppercase: return text.ToUpper(selectedCulture);
            case TextCaseTransform.Lowercase: return text.ToLower(selectedCulture);
            case TextCaseTransform.TitleCase: return selectedCulture.TextInfo.ToTitleCase(text.ToLower(selectedCulture));
            case TextCaseTransform.SentenceCase: return SentenceCase(text, selectedCulture);
            case TextCaseTransform.ToggleCase: return ToggleCase(text, selectedCulture);
            default: throw new ArgumentOutOfRangeException(nameof(transform), transform, "Unknown text-case transform.");
        }
    }

    private static string SentenceCase(string text, CultureInfo culture) {
        var normalized = text.ToLower(culture);
        var output = new StringBuilder(normalized.Length);
        var capitalize = true;
        var enumerator = StringInfo.GetTextElementEnumerator(normalized);
        while (enumerator.MoveNext()) {
            var element = enumerator.GetTextElement();
            if (capitalize && IsLetter(element)) {
                output.Append(element.ToUpper(culture));
                capitalize = false;
            } else {
                output.Append(element);
            }
            if (element == "." || element == "!" || element == "?" || element == "\r" || element == "\n") capitalize = true;
        }
        return output.ToString();
    }

    private static string ToggleCase(string text, CultureInfo culture) {
        var output = new StringBuilder(text.Length);
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext()) {
            var element = enumerator.GetTextElement();
            var category = CharUnicodeInfo.GetUnicodeCategory(element, 0);
            if (category == UnicodeCategory.UppercaseLetter) output.Append(element.ToLower(culture));
            else if (category == UnicodeCategory.LowercaseLetter) output.Append(element.ToUpper(culture));
            else output.Append(element);
        }
        return output.ToString();
    }

    private static bool IsLetter(string element) {
        var category = CharUnicodeInfo.GetUnicodeCategory(element, 0);
        return category == UnicodeCategory.UppercaseLetter ||
            category == UnicodeCategory.LowercaseLetter ||
            category == UnicodeCategory.TitlecaseLetter ||
            category == UnicodeCategory.ModifierLetter ||
            category == UnicodeCategory.OtherLetter;
    }
}
