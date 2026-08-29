using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Typography;

namespace ChartForgeX.SvgRaster;

internal struct SvgRasterTextTransformer {
    private bool _wordStart;
    private bool _sentenceStart;

    public SvgRasterTextTransformer() {
        _wordStart = true;
        _sentenceStart = true;
    }

    public string Transform(string text, string value) {
        var transform = Resolve(value);
        if (transform == TextCaseTransform.Uppercase || transform == TextCaseTransform.Lowercase || transform == TextCaseTransform.ToggleCase) {
            var transformed = TextCaseTransformer.Apply(text, transform, CultureInfo.InvariantCulture);
            UpdateBoundaries(text);
            return transformed;
        }

        var normalized = transform is TextCaseTransform.TitleCase or TextCaseTransform.SentenceCase
            ? text.ToLower(CultureInfo.InvariantCulture)
            : text;
        var output = new StringBuilder(normalized.Length);
        var enumerator = StringInfo.GetTextElementEnumerator(normalized);
        while (enumerator.MoveNext()) {
            var element = enumerator.GetTextElement();
            var isLetter = IsLetter(element);
            if (isLetter && ((transform == TextCaseTransform.TitleCase && _wordStart) || (transform == TextCaseTransform.SentenceCase && _sentenceStart))) output.Append(element.ToUpper(CultureInfo.InvariantCulture));
            else output.Append(element);
            UpdateBoundary(element, isLetter);
        }
        return output.ToString();
    }

    private static TextCaseTransform Resolve(string value) {
        if (value.IndexOf("uppercase", StringComparison.OrdinalIgnoreCase) >= 0) return TextCaseTransform.Uppercase;
        if (value.IndexOf("lowercase", StringComparison.OrdinalIgnoreCase) >= 0) return TextCaseTransform.Lowercase;
        if (value.IndexOf("capitalize", StringComparison.OrdinalIgnoreCase) >= 0) return TextCaseTransform.TitleCase;
        if (value.IndexOf("sentence-case", StringComparison.OrdinalIgnoreCase) >= 0) return TextCaseTransform.SentenceCase;
        if (value.IndexOf("toggle-case", StringComparison.OrdinalIgnoreCase) >= 0) return TextCaseTransform.ToggleCase;
        return TextCaseTransform.None;
    }

    private void UpdateBoundaries(string text) {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext()) {
            var element = enumerator.GetTextElement();
            UpdateBoundary(element, IsLetter(element));
        }
    }

    private void UpdateBoundary(string element, bool isLetter) {
        if (isLetter || IsNumber(element)) {
            _wordStart = false;
            if (isLetter) _sentenceStart = false;
            return;
        }

        _wordStart = element != "'" && element != "\u2019";
        if (element == "." || element == "!" || element == "?" || element == "\r" || element == "\n") _sentenceStart = true;
    }

    private static bool IsLetter(string element) {
        var category = CharUnicodeInfo.GetUnicodeCategory(element, 0);
        return category == UnicodeCategory.UppercaseLetter || category == UnicodeCategory.LowercaseLetter || category == UnicodeCategory.TitlecaseLetter || category == UnicodeCategory.ModifierLetter || category == UnicodeCategory.OtherLetter;
    }

    private static bool IsNumber(string element) {
        var category = CharUnicodeInfo.GetUnicodeCategory(element, 0);
        return category == UnicodeCategory.DecimalDigitNumber || category == UnicodeCategory.LetterNumber || category == UnicodeCategory.OtherNumber;
    }
}
