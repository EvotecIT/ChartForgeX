using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Typography;

/// <summary>
/// Splits a paragraph into unbreakable runs at the common line-break opportunities: after a run of
/// breaking spaces, after a hyphen between word characters, and around CJK ideographs and kana
/// (never before closing punctuation or small kana, never after opening punctuation). Each run keeps
/// its trailing spaces and whole text elements, so concatenating the runs restores the paragraph.
/// </summary>
internal static class TextLineBreak {
    internal static List<string> Runs(string paragraph) {
        var runs = new List<string>(); var elements = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(paragraph);
        while (enumerator.MoveNext()) elements.Add(enumerator.GetTextElement());
        var start = 0;
        for (var index = 1; index < elements.Count; index++) {
            if (!BreaksBefore(elements, index)) continue;
            runs.Add(string.Concat(elements.GetRange(start, index - start))); start = index;
        }
        if (start < elements.Count) runs.Add(string.Concat(elements.GetRange(start, elements.Count - start)));
        return runs;
    }

    /// <summary>Whether the element is a space that may end a line (no-break spaces glue their neighbours).</summary>
    internal static bool IsBreakingSpace(string element) {
        if (element.Length != 1) return false;
        var c = element[0];
        return c != '\u00A0' && c != '\u2007' && c != '\u202F' && char.IsWhiteSpace(c);
    }

    private static bool BreaksBefore(List<string> elements, int index) {
        string before = elements[index - 1], after = elements[index];
        if (IsBreakingSpace(after)) return false;
        if (IsBreakingSpace(before)) return true;
        if (IsGlue(before) || IsGlue(after)) return false;
        if (IsHyphen(before)) return index > 1 && IsWordCharacter(elements[index - 2]) && IsWordCharacter(after);
        if (!IsCjk(before) && !IsCjk(after)) return false;
        return !ForbidsLineStart(after) && !ForbidsLineEnd(before);
    }

    // No-break spaces, the word joiner and the zero-width no-break space keep both neighbours on one line.
    private static bool IsGlue(string element) => element.Length == 1 && "\u00A0\u2007\u202F\u2060\uFEFF".IndexOf(element[0]) >= 0;

    private static bool IsHyphen(string element) => element == "-" || element == "\u2010" || element == "\u2013";

    private static bool IsWordCharacter(string element) => char.IsLetterOrDigit(element, 0);

    // CJK punctuation and fullwidth forms take part in CJK breaking, so text after "。" or "」" may start a new line.
    private static bool IsCjk(string element) {
        var value = char.ConvertToUtf32(element, 0);
        return IsIdeographic(element) || value is >= 0x3000 and <= 0x303F or >= 0xFE30 and <= 0xFE4F or >= 0xFF01 and <= 0xFF60;
    }

    private static bool IsIdeographic(string element) {
        var value = char.ConvertToUtf32(element, 0);
        return value is >= 0x3040 and <= 0x30FF or >= 0x3400 and <= 0x4DBF or >= 0x4E00 and <= 0x9FFF
            or >= 0xAC00 and <= 0xD7AF or >= 0xF900 and <= 0xFAFF or >= 0x20000 and <= 0x3FFFF;
    }

    // Closing marks, terminal punctuation, the prolonged-sound mark, iteration marks and small kana stay with the
    // text before them, as Japanese and Chinese line breaking expects.
    private static bool ForbidsLineStart(string element) {
        switch (CharUnicodeInfo.GetUnicodeCategory(element, 0)) {
            case UnicodeCategory.ClosePunctuation: case UnicodeCategory.FinalQuotePunctuation: case UnicodeCategory.OtherPunctuation: return true;
        }
        return "\u3005\u303B\u309D\u309E\u30FC\u30FD\u30FEぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヮヵヶ".IndexOf(element[0]) >= 0;
    }

    private static bool ForbidsLineEnd(string element) {
        var category = CharUnicodeInfo.GetUnicodeCategory(element, 0);
        return category is UnicodeCategory.OpenPunctuation or UnicodeCategory.InitialQuotePunctuation;
    }
}
