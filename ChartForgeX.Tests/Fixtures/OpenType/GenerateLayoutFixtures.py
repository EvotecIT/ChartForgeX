"""Rebuild original, tiny layout fixtures with fontTools (validation tooling only).

The outlines, names, mappings and feature definitions below are ChartForgeX test
data. No third-party font outlines or shaping implementation are included.
Run this file from any directory; the tests load its checked-in TTF outputs.
"""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString

ROOT = Path(__file__).parent
NAMES = [".notdef", "H", "O", "x", "e", "acute", "eacute", "F", "box", "grave",
         "beh", "beh.init", "beh.medi", "beh.fina", "beh.isol", "lam", "alef", "lamalef",
         "woman", "laptop", "zwj", "womanlaptop"]
MAP = {ord(c): "box" for c in "ChartForgeX 0123456789"}
MAP.update({ord("H"): "H", ord("O"): "O", ord("x"): "x", ord("e"): "e", ord("F"): "F",
            0x301: "acute", 0x300: "grave", 0xE9: "eacute", 0x628: "beh", 0x644: "lam", 0x627: "alef",
            0xE001: "woman", 0xE002: "laptop", 0x200D: "zwj"})
WIDTHS = {name: 500 for name in NAMES}
WIDTHS.update(H=600, acute=0, grave=0, F=800, woman=1000, laptop=1000, womanlaptop=1200, zwj=0)

def build(name, features):
    builder = FontBuilder(1000, isTTF=True)
    builder.setupGlyphOrder(NAMES)
    builder.setupCharacterMap(MAP)
    outlines = {}
    for i, glyph in enumerate(NAMES):
        pen = TTGlyphPen(None)
        if glyph not in (".notdef", "zwj"):
            left, top = (0, 100) if glyph in ("acute", "grave") else (50 + i * 3, 500 + i * 5)
            pen.moveTo((left, 0)); pen.lineTo((left + 100, 0)); pen.lineTo((left + 100, top)); pen.lineTo((left, top)); pen.closePath()
        outlines[glyph] = pen.glyph()
    builder.setupGlyf(outlines)
    builder.setupHorizontalMetrics({g: (WIDTHS[g], 0) for g in NAMES})
    builder.setupHorizontalHeader(ascent=800, descent=-200)
    builder.setupNameTable({"familyName": "CFX Layout " + name, "styleName": "Regular", "uniqueFontIdentifier": "CFX-Layout-" + name,
                           "fullName": "CFX Layout " + name, "psName": "CFXLayout-" + name})
    builder.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200)
    builder.setupPost()
    builder.setupMaxp()
    addOpenTypeFeaturesFromString(builder.font, "languagesystem DFLT dflt; languagesystem latn dflt; languagesystem arab dflt;\n" + features)
    builder.font["head"].created = builder.font["head"].modified = 0
    builder.font.recalcTimestamp = False
    builder.font.save(ROOT / (name + ".ttf"))
    return builder.font

build("substitution", """
feature ccmp { sub H by O; sub eacute by e acute; } ccmp;
feature liga { sub O x by F; sub woman zwj laptop by womanlaptop; } liga;
feature salt { sub O by box; } salt;
""")
context = build("context", """
lookup Replace { sub x by F; } Replace;
lookup Expand { sub e by O x; } Expand;
feature ccmp {
    sub H x' lookup Replace O;
    sub H e' lookup Expand O;
} ccmp;
""")
for lookup_index, lookup in enumerate(context["GSUB"].table.LookupList.Lookup):
    if lookup.LookupType != 6:
        continue
    nested = next(record for sub in lookup.SubTable for rules in sub.ChainSubRuleSet if rules
                  for rule in rules.ChainSubRule for record in rule.SubstLookupRecord)
    nested.LookupListIndex = lookup_index
    break
context.save(ROOT / "recursive.ttf")
build("extension", """
lookup Extended useExtension { sub H by O; } Extended;
feature ccmp { lookup Extended; } ccmp;
""")
build("reverse", "feature ccmp { rsub H x' O by F; } ccmp;")
build("alternate", "feature rlig { sub H from [O F]; } rlig;")
build("hidden-controls", "feature ccmp { sub zwj by box; } ccmp;")
required = build("required", "feature ccmp { sub H by O; sub O by F; } ccmp;")
for script in required["GSUB"].table.ScriptList.ScriptRecord:
    language = script.Script.DefaultLangSys
    language.ReqFeatureIndex = 0; language.FeatureIndex = []; language.FeatureCount = 0
required.save(ROOT / "required.ttf")
POSITIONING = """
@Left = [H O]; @Right = [x e];
feature kern { pos H O -80; pos @Left @Right -40; } kern;
markClass acute <anchor 0 0> @top;
markClass grave <anchor 0 0> @bottom;
feature mark {
    pos base H <anchor 300 700> mark @top <anchor 300 -100> mark @bottom;
    pos base e <anchor 250 500> mark @top;
    pos ligature F <anchor 150 700> mark @top ligComponent <anchor 600 700> mark @top;
} mark;
feature mkmk { pos mark acute <anchor 0 200> mark @top; } mkmk;
"""
build("positioning", POSITIONING)
build("mark-ligature", POSITIONING + "feature liga { lookupflag IgnoreMarks; sub H O by F; } liga;")
build("cursive", """
feature curs {
    pos cursive H <anchor 100 0> <anchor 500 100>;
    pos cursive O <anchor 100 0> <anchor 400 50>;
    lookupflag RightToLeft;
    pos cursive beh <anchor 100 0> <anchor 400 100>;
    pos cursive lam <anchor 100 0> <anchor 400 50>;
} curs;
""")
build("arabic", """
feature isol { script arab; sub beh by beh.isol; } isol;
feature init { script arab; sub beh by beh.init; } init;
feature medi { script arab; sub beh by beh.medi; } medi;
feature fina { script arab; sub beh by beh.fina; } fina;
feature rlig { script arab; sub lam alef by lamalef; } rlig;
""")

def context_records(font):
    for lookup in font['GSUB'].table.LookupList.Lookup:
        if lookup.LookupType not in (5, 6):
            continue
        for sub in lookup.SubTable:
            if sub.Format == 3:
                yield sub.SubstLookupRecord
            else:
                for rules in getattr(sub, 'ChainSubRuleSet', getattr(sub, 'SubRuleSet', [])):
                    if rules:
                        for rule in getattr(rules, 'ChainSubRule', getattr(rules, 'SubRule', [])):
                            yield rule.SubstLookupRecord

sequence = build('context-sequence', '''
lookup Lig { sub O x by F; } Lig;
lookup Adjust { sub e by box; } Adjust;
feature ccmp { sub H' O' lookup Lig x' e' lookup Adjust; } ccmp;
''')
for records in context_records(sequence):
    records[-1].SequenceIndex = 2  # The fourth input follows the ligature at updated index 2.
sequence.save(ROOT / 'context-sequence.ttf')
build('context-expansion', '''
lookup Expand { sub O by O x; } Expand;
lookup Adjust { sub x by F; } Adjust;
feature ccmp { sub H' O' lookup Expand e' lookup Adjust; } ccmp;
''')
FILTERED_MARKS = '''
markClass acute <anchor 0 0> @top;
markClass grave <anchor 0 0> @bottom;
@TopFilter=[acute];
feature mark { pos base H <anchor 300 700> mark @top <anchor 300 -100> mark @bottom; } mark;
feature mkmk { lookupflag %s @TopFilter; pos mark acute <anchor 0 200> mark @top; } mkmk;
'''
build('mark-filter', FILTERED_MARKS % 'UseMarkFilteringSet')
build('mark-attachment-filter', FILTERED_MARKS % 'MarkAttachmentType')
MAP.update({0x1E6A:'H',0x1EB6:'O',0x1F00:'H',0x1F01:'O',0xA640:'H',0xA641:'O',0x1DF00:'H',0x1DF01:'O'})
build('script-routing', '''
feature kern { script latn; pos H O -80; script grek; pos H O -80; script cyrl; pos H O -80; } kern;
''')
build('greek-only', 'feature kern { script grek; pos H O -80; } kern;')
