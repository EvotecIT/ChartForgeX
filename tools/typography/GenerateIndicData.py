"""Generate supported-script properties from Unicode 17 Indic category files.

Usage: python GenerateIndicData.py IndicSyllabicCategory.txt IndicPositionalCategory.txt
Sources: https://www.unicode.org/Public/17.0.0/ucd/ (Unicode License V3).
"""
from pathlib import Path
import hashlib
import sys

syllabic = Path(sys.argv[1]).read_bytes()
positional = Path(sys.argv[2]).read_bytes()
classes = {'Consonant': 'Consonant', 'Vowel_Independent': 'Vowel',
           'Vowel_Dependent': 'Matra', 'Virama': 'Halant', 'Nukta': 'Nukta',
           'Bindu': 'Sign', 'Visarga': 'Sign', 'Syllable_Modifier': 'Sign',
           'Cantillation_Mark': 'Accent', 'Tone_Mark': 'Accent',
           'Consonant_Preceding_Repha': 'Repha', 'Consonant_Medial': 'Medial',
           'Consonant_Subjoined': 'Consonant', 'Register_Shifter': 'Shifter',
           'Number': 'Number', 'Invisible_Stacker': 'Halant',
           'Consonant_Dead': 'Consonant', 'Consonant_With_Stacker': 'Consonant',
           'Consonant_Final': 'Sign', 'Pure_Killer': 'Halant',
           'Avagraha': 'Sign', 'Gemination_Mark': 'Sign'}
positions = {'Left': 'Left', 'Visual_Order_Left': 'Left', 'Top': 'Top',
             'Bottom': 'Bottom', 'Right': 'Right', 'Top_And_Right': 'Right',
             'Bottom_And_Right': 'Right', 'Top_And_Bottom': 'Bottom'}
values = {}
for data, mapping, slot in ((syllabic, classes, 0), (positional, positions, 1)):
    for line in data.decode('utf8').splitlines():
        value = line.split('#')[0].strip()
        if not value:
            continue
        code, kind = (part.strip() for part in value.split(';'))
        if kind not in mapping:
            continue
        bounds = code.split('..')
        for cp in range(int(bounds[0], 16), int(bounds[-1], 16) + 1):
            if not (0x900 <= cp <= 0xD7F or 0xE00 <= cp <= 0xEFF or
                    0x1780 <= cp <= 0x17FF or 0x1CD0 <= cp <= 0x1CFF or
                    0xA8E0 <= cp <= 0xA8FF):
                continue
            values.setdefault(cp, ['Other', 'None'])[slot] = mapping[kind]
ranges = []
for cp, value in sorted(values.items()):
    if ranges and ranges[-1][1] + 1 == cp and ranges[-1][2:] == tuple(value):
        ranges[-1] = (ranges[-1][0], cp, *value)
    else:
        ranges.append((cp, cp, *value))
header = '''// Generated from Unicode 17.0.0 IndicSyllabicCategory / IndicPositionalCategory.
// Copyright Unicode, Inc. See Unicode-LICENSE.txt (Unicode License V3).
// Syllabic SHA256: %s
// Positional SHA256: %s
namespace ChartForgeX.Typography;

internal enum IndicCategory : byte { Other, Consonant, Vowel, Matra, Halant, Nukta, Sign, Accent, Repha, Medial, Shifter, Number }
internal enum IndicMatra : byte { None, Left, Top, Bottom, Right }
internal static class IndicCharacterData {
    internal static IndicCategory Category(int cp) => Find(cp).Category;
    internal static IndicMatra Matra(int cp) => Find(cp).Matra;
    private static Range Find(int cp) {
        var low = 0; var high = Ranges.Length - 1;
        while (low <= high) {
            var middle = low + (high - low) / 2; var range = Ranges[middle];
            if (cp < range.First) high = middle - 1;
            else if (cp > range.Last) low = middle + 1;
            else return range;
        }
        return default;
    }
    private readonly struct Range {
        internal Range(int first, int last, IndicCategory category, IndicMatra matra) { First = first; Last = last; Category = category; Matra = matra; }
        internal readonly int First, Last;
        internal readonly IndicCategory Category;
        internal readonly IndicMatra Matra;
    }
    private static readonly Range[] Ranges = {
''' % (hashlib.sha256(syllabic).hexdigest(), hashlib.sha256(positional).hexdigest())
output = header + ''.join('        new(0x%X, 0x%X, IndicCategory.%s, IndicMatra.%s),\n' % row for row in ranges) + '    };\n}\n'
target = Path(__file__).resolve().parents[2] / 'ChartForgeX/Typography/IndicCharacterData.cs'
target.write_text(output, encoding='utf8', newline='\n')
print('Generated', len(ranges), 'Indic category ranges')
