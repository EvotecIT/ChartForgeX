"""Original language-system fixtures; fontTools is validation-only regeneration tooling."""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString

ROOT = Path(__file__).parent
NAMES = [".notdef", "H", "O", "F", "space", "be", "be.srb", "be.bgr", "beh", "beh.urd"]

def build(name, default=True):
    builder = FontBuilder(1000, isTTF=True)
    builder.setupGlyphOrder(NAMES)
    cmap = {ord(ch): "O" for ch in "ChartForgeX 0123456789"}
    cmap.update({ord("H"): "H", ord("O"): "O", ord("F"): "F", 32: "space", 0x431: "be", 0x628: "beh"})
    builder.setupCharacterMap(cmap)
    outlines = {}
    for i, glyph in enumerate(NAMES):
        pen = TTGlyphPen(None)
        if glyph not in (".notdef", "space"):
            pen.moveTo((50, 0)); pen.lineTo((150 + i * 25, 0)); pen.lineTo((150 + i * 25, 450 + i * 25)); pen.lineTo((50, 450 + i * 25)); pen.closePath()
        outlines[glyph] = pen.glyph()
    builder.setupGlyf(outlines)
    builder.setupHorizontalMetrics({g: (300 if g == "space" else 400 + i * 50, 0) for i, g in enumerate(NAMES)})
    builder.setupHorizontalHeader(ascent=850, descent=-200)
    builder.setupNameTable({"familyName": "CFX Language " + name, "styleName": "Regular", "uniqueFontIdentifier": "CFX-Language-" + name, "fullName": "CFX Language " + name, "psName": "CFXLanguage-" + name})
    builder.setupOS2(sTypoAscender=850, sTypoDescender=-200, usWinAscent=850, usWinDescent=200)
    builder.setupPost(); builder.setupMaxp()
    addOpenTypeFeaturesFromString(builder.font, """
languagesystem DFLT dflt;
languagesystem latn dflt;
languagesystem latn TRK;
languagesystem cyrl dflt;
languagesystem cyrl SRB;
languagesystem cyrl BGR;
languagesystem arab dflt;
languagesystem arab URD;
feature ccmp { script latn; language dflt; sub H by O; } ccmp;
feature locl {
 script latn; language TRK exclude_dflt; sub H by F;
 script cyrl; language SRB exclude_dflt; sub be by be.srb;
 language BGR exclude_dflt; sub be by be.bgr;
 script arab; language URD exclude_dflt; sub beh by beh.urd;
} locl;
feature kern {
 script latn; language dflt; pos O O -10;
 language TRK exclude_dflt; pos F F -120;
 script cyrl; language SRB exclude_dflt; pos be.srb be.srb -80;
 language BGR exclude_dflt; pos be.bgr be.bgr -40;
} kern;
""")
    if not default:
        for tag in ("GSUB", "GPOS"):
            for record in builder.font[tag].table.ScriptList.ScriptRecord:
                if record.ScriptTag == "latn": record.Script.DefaultLangSys = None
    builder.font["head"].created = builder.font["head"].modified = 0
    builder.font.recalcTimestamp = False
    builder.font.save(ROOT / (name + ".ttf"))

build("language")
build("language-no-default", False)
