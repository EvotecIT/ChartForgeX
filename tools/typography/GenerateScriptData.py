"""Regenerate the font script property table from Unicode 17.0.0 Scripts.txt.

Usage: python tools/typography/GenerateScriptData.py path/to/Scripts.txt
The source is https://www.unicode.org/Public/17.0.0/ucd/Scripts.txt.
Only scripts selected by the current glyph-layout pipeline are retained.
"""
from pathlib import Path
import hashlib
import sys

source = Path(sys.argv[1]).read_bytes()
tags = dict(Latin='latn', Greek='grek', Cyrillic='cyrl', Hebrew='hebr', Arabic='arab',
            Devanagari='deva', Bengali='beng', Gurmukhi='guru', Gujarati='gujr',
            Oriya='orya', Tamil='taml', Telugu='telu', Kannada='knda', Malayalam='mlym',
            Thai='thai', Lao='lao ', Khmer='khmr')
ranges = []
for line in source.decode('utf8').splitlines():
    value = line.split('#')[0].strip()
    if not value:
        continue
    code, script = (part.strip() for part in value.split(';'))
    if script not in tags:
        continue
    bounds = code.split('..')
    ranges.append((int(bounds[0], 16), int(bounds[-1], 16), tags[script]))
merged = []
for first, last, tag in sorted(ranges):
    if merged and merged[-1][1] + 1 == first and merged[-1][2] == tag:
        merged[-1] = (merged[-1][0], last, tag)
    else:
        merged.append((first, last, tag))
output = '''// Generated from Unicode 17.0.0 Scripts.txt; do not edit the ranges manually.
// Copyright Unicode, Inc. See Unicode-LICENSE.txt (Unicode License V3).
// Source SHA256: ''' + hashlib.sha256(source).hexdigest() + '''
namespace ChartForgeX.Typography;

internal static class OpenTypeScriptData {
    internal static string Script(int codePoint) {
        var low = 0; var high = Ranges.Length - 1;
        while (low <= high) {
            var middle = low + (high - low) / 2; var range = Ranges[middle];
            if (codePoint < range.First) high = middle - 1;
            else if (codePoint > range.Last) low = middle + 1;
            else return range.Tag;
        }
        return "DFLT"; // Common/inherited characters follow the surrounding run's script.
    }
    private readonly struct ScriptRange {
        internal ScriptRange(int first, int last, string tag) { First = first; Last = last; Tag = tag; }
        internal readonly int First, Last;
        internal readonly string Tag;
    }
    private static readonly ScriptRange[] Ranges = {
'''
output += ''.join('        new(0x%X, 0x%X, "%s"),\n' % row for row in merged)
output += '    };\n}\n'
target = Path(__file__).resolve().parents[2] / 'ChartForgeX/Typography/OpenTypeScriptData.cs'
target.write_text(output, encoding='utf8', newline='\n')
print('Generated', len(merged), 'script ranges')
