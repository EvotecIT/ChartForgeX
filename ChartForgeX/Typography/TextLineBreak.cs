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
        return c != ' ' && c != ' ' && c != ' ' && char.IsWhiteSpace(c);
    }

    private static bool BreaksBefore(List<string> elements, int index) {
        string before = elements[index - 1], after = elements[index];
        if (IsBreakingSpace(after)) return false;
        if (IsBreakingSpace(before)) return true;
        if (IsHyphen(before)) return index > 1 && IsWordCharacter(elements[index - 2]) && IsWordCharacter(after);
        if (!IsIdeographic(before) && !IsIdeographic(after)) return false;
        return !ForbidsLineStart(after) && !ForbidsLineEnd(before);
    }

    private static bool IsHyphen(string element) => element == "-" || element == "‐" || element == "–";

    private static bool IsWordCharacter(string element) => char.IsLetterOrDigit(element, 0);

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
        return "々〻ゝゞーヽヾぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヮヵヶ".IndexOf(element[0]) >= 0;
    }

    private static bool ForbidsLineEnd(string element) {
        var category = CharUnicodeInfo.GetUnicodeCategory(element, 0);
        return category is UnicodeCategory.OpenPunctuation or UnicodeCategory.InitialQuotePunctuation;
    }
}
