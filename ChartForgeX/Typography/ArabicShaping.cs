using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Typography;

/// <summary>The contextual form an Arabic letter takes from its neighbours.</summary>
internal enum ArabicForm : byte {
    Isolated,
    Final,
    Initial,
    Medial
}

/// <summary>
/// Arabic contextual joining without a shaping engine: each letter's form comes from the Unicode
/// joining types of its neighbours (ArabicShaping.txt), and is drawn with the matching Arabic
/// Presentation Forms-B (and, for common Persian and Urdu letters, Forms-A) character when the face
/// has one. Lam followed by alef becomes the lam-alef ligature.
/// </summary>
internal static class ArabicShaping {
    private enum Joining : byte { None, Right, Dual, Causing, Transparent }

    // Presentation forms: isolated, final, initial, medial (the last two only for dual-joining letters).
    private static readonly Dictionary<int, int[]> Forms = BuildForms();

    /// <summary>True for Arabic letters whose neighbours determine their joining form.</summary>
    internal static bool IsJoiningLetter(int cp) => IsRightJoining(cp) || IsDualJoining(cp);

    /// <summary>True when the text may contain Arabic letters that join.</summary>
    internal static bool MayJoin(IReadOnlyList<int> codePoints) {
        foreach (var cp in codePoints) if ((cp >= 0x0620 && cp <= 0x06FF) || (cp >= 0x0750 && cp <= 0x077F) || (cp >= 0x08A0 && cp <= 0x08BD)) return true;
        return false;
    }

    /// <summary>
    /// The form of each letter of <paramref name="letters"/>, the base code points of a run of
    /// clusters in logical order. Letters that do not join are Isolated.
    /// </summary>
    internal static ArabicForm[] ResolveForms(IReadOnlyList<int> letters) {
        var count = letters.Count;
        var joining = new Joining[count];
        for (var i = 0; i < count; i++) joining[i] = JoiningOf(letters[i]);
        var forms = new ArabicForm[count];
        for (var i = 0; i < count; i++) {
            var type = joining[i];
            if (type != Joining.Right && type != Joining.Dual) continue;
            var previous = i - 1;
            while (previous >= 0 && joining[previous] == Joining.Transparent) previous--;
            var next = i + 1;
            while (next < count && joining[next] == Joining.Transparent) next++;
            var joinsPrevious = previous >= 0 && (joining[previous] == Joining.Dual || joining[previous] == Joining.Causing);
            var joinsNext = type == Joining.Dual && next < count && (joining[next] == Joining.Right || joining[next] == Joining.Dual || joining[next] == Joining.Causing);
            forms[i] = joinsPrevious && joinsNext ? ArabicForm.Medial : joinsPrevious ? ArabicForm.Final : joinsNext ? ArabicForm.Initial : ArabicForm.Isolated;
        }

        return forms;
    }

    /// <summary>The presentation form of <paramref name="cp"/>, or <paramref name="cp"/> itself when Unicode has none.</summary>
    internal static int PresentationForm(int cp, ArabicForm form) {
        if (!Forms.TryGetValue(cp, out var forms)) return cp;
        var index = (int)form;
        return index < forms.Length && forms[index] != 0 ? forms[index] : cp;
    }

    /// <summary>The lam-alef ligature for lam followed by <paramref name="alef"/>, or -1 when <paramref name="alef"/> is not an alef.</summary>
    internal static int LamAlef(int alef, bool final) {
        var isolated = alef switch { 0x0622 => 0xFEF5, 0x0623 => 0xFEF7, 0x0625 => 0xFEF9, 0x0627 => 0xFEFB, _ => -1 };
        return isolated < 0 ? -1 : final ? isolated + 1 : isolated;
    }

    private static Joining JoiningOf(int cp) {
        if (cp == 0x0640 || cp == 0x07FA || cp == 0x200D) return Joining.Causing;
        if (cp == 0x200C) return Joining.None;
        var category = BidiCharacterData.Category(cp);
        if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.EnclosingMark || category == UnicodeCategory.Format) return Joining.Transparent;
        if (IsRightJoining(cp)) return Joining.Right;
        if (IsDualJoining(cp)) return Joining.Dual;
        return Joining.None;
    }

    private static bool IsRightJoining(int cp) =>
        (cp >= 0x0622 && cp <= 0x0625) || cp == 0x0627 || cp == 0x0629 || (cp >= 0x062F && cp <= 0x0632) || cp == 0x0648 ||
        (cp >= 0x0671 && cp <= 0x0673) || (cp >= 0x0675 && cp <= 0x0677) || (cp >= 0x0688 && cp <= 0x0699) || cp == 0x06C0 ||
        (cp >= 0x06C3 && cp <= 0x06CB) || cp == 0x06CD || cp == 0x06CF || cp == 0x06D2 || cp == 0x06D3 || cp == 0x06D5 ||
        cp == 0x06EE || cp == 0x06EF || (cp >= 0x0759 && cp <= 0x075B) || cp == 0x076B || cp == 0x076C || cp == 0x0771 ||
        cp == 0x0773 || cp == 0x0774 || cp == 0x0778 || cp == 0x0779 || (cp >= 0x08AA && cp <= 0x08AC) || cp == 0x08AE ||
        cp == 0x08B1 || cp == 0x08B2 || cp == 0x08B9;

    private static bool IsDualJoining(int cp) =>
        cp == 0x0620 || cp == 0x0626 || cp == 0x0628 || (cp >= 0x062A && cp <= 0x062E) || (cp >= 0x0633 && cp <= 0x063F) ||
        (cp >= 0x0641 && cp <= 0x0647) || cp == 0x0649 || cp == 0x064A || cp == 0x066E || cp == 0x066F || (cp >= 0x0678 && cp <= 0x0687) ||
        (cp >= 0x069A && cp <= 0x06BF) || cp == 0x06C1 || cp == 0x06C2 || cp == 0x06CC || cp == 0x06CE || cp == 0x06D0 || cp == 0x06D1 ||
        (cp >= 0x06FA && cp <= 0x06FC) || cp == 0x06FF || (cp >= 0x0750 && cp <= 0x0758) || (cp >= 0x075C && cp <= 0x076A) ||
        (cp >= 0x076D && cp <= 0x0770) || cp == 0x0772 || (cp >= 0x0775 && cp <= 0x0777) || (cp >= 0x077A && cp <= 0x077F) ||
        (cp >= 0x08A0 && cp <= 0x08A9) || cp == 0x08AF || cp == 0x08B0 || (cp >= 0x08B3 && cp <= 0x08B8) || (cp >= 0x08BA && cp <= 0x08BD);

    private static Dictionary<int, int[]> BuildForms() {
        var forms = new Dictionary<int, int[]>();
        // Arabic Presentation Forms-B lists hamza (one form), then each letter from U+0622 to U+064A
        // that has forms, with two forms for right-joining letters and four for dual-joining ones.
        forms[0x0621] = new[] { 0xFE80 };
        var next = 0xFE81;
        for (var cp = 0x0622; cp <= 0x064A; cp++) {
            if (cp >= 0x063B && cp <= 0x0640) continue;
            var count = IsRightJoining(cp) || cp == 0x0649 ? 2 : 4;
            var entry = new int[count];
            for (var i = 0; i < count; i++) entry[i] = next + i;
            forms[cp] = entry;
            next += count;
        }

        // Alef maksura's initial and medial forms are in Forms-A.
        forms[0x0649] = new[] { 0xFEEF, 0xFEF0, 0xFBE8, 0xFBE9 };
        // Common Persian and Urdu letters (Arabic Presentation Forms-A).
        foreach (var (cp, start, count) in new[] {
            (0x0671, 0xFB50, 2), (0x0679, 0xFB66, 4), (0x067E, 0xFB56, 4), (0x0686, 0xFB7A, 4), (0x0688, 0xFB88, 2),
            (0x0691, 0xFB8C, 2), (0x0698, 0xFB8A, 2), (0x06A4, 0xFB6A, 4), (0x06A9, 0xFB8E, 4), (0x06AF, 0xFB92, 4),
            (0x06BA, 0xFB9E, 2), (0x06BE, 0xFBAA, 4), (0x06C1, 0xFBA6, 4), (0x06CC, 0xFBFC, 4), (0x06D2, 0xFBAE, 2), (0x06D3, 0xFBB0, 2)
        }) {
            var entry = new int[count];
            for (var i = 0; i < count; i++) entry[i] = start + i;
            forms[cp] = entry;
        }

        return forms;
    }
}
