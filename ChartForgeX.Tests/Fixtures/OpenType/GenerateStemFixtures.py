"""Original geometric glyphs for small-text stem fitting. Test-only fontTools;
no external outlines or rasterizer implementation are copied.
"""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.pens.t2CharStringPen import T2CharStringPen

ORDER = ['.notdef', 'space', 'H', 'I', 'O', 'N', 'x', 'L', 'R', 'C']
CONTOURS = {
    'H': [[(100, 0), (180, 0), (180, 310), (400, 310), (400, 0), (480, 0),
           (480, 700), (400, 700), (400, 390), (180, 390), (180, 700), (100, 700)]],
    'I': [[(100, 0), (180, 0), (180, 700), (100, 700)]],
    'O': [[(100, 0), (500, 0), (500, 700), (100, 700)],
          [(180, 80), (180, 620), (420, 620), (420, 80)]],
    'N': [[(100, 0), (180, 0), (500, 700), (420, 700)]],
    'x': [[(100, 0), (180, 0), (180, 500), (100, 500)]],
    'L': [[(-134, 0), (-69, 0), (-69, 700), (-134, 700), (-134, 280), (-290, 280), (-290, 200), (-134, 200)]],
    'R': [[(734, 0), (669, 0), (669, 700), (734, 700), (734, 280), (890, 280), (890, 200), (734, 200)]],
    'C': [[(-634, 0), (-569, 0), (-569, 700), (-634, 700), (-634, 280), (-790, 280), (-790, 200), (-634, 200)]],
}

for compact, variable in [(False, False), (True, False), (False, True)]:
    if variable:
        CONTOURS['I'] = [[(100, 0), (180, 0), (180, 620), (280, 620), (280, 700), (100, 700)]]
    builder = FontBuilder(1000, isTTF=not compact)
    builder.setupGlyphOrder(ORDER)
    builder.setupCharacterMap({**{c: 'H' for c in range(33, 127)}, 32: 'space', 73: 'I', 79: 'O', 78: 'N', 120: 'x', 76: 'L', 82: 'R', 67: 'C'})
    glyphs = {}
    for name in ORDER:
        pen = T2CharStringPen(600, None) if compact else TTGlyphPen(None)
        for contour in CONTOURS.get(name, []):
            pen.moveTo(contour[0])
            for point in contour[1:]: pen.lineTo(point)
            pen.closePath()
        glyphs[name] = pen.getCharString() if compact else pen.glyph()
    if compact:
        builder.setupCFF('CFXStemCompact', {'FullName': 'CFX Stem Compact', 'FamilyName': 'CFX Stem Compact', 'Weight': 'Regular'}, glyphs, {})
    else: builder.setupGlyf(glyphs)
    builder.setupHorizontalMetrics({name: (600, min(p[0] for c in CONTOURS[name] for p in c) if name in CONTOURS else 0) for name in ORDER})
    builder.setupHorizontalHeader(ascent=800, descent=-200)
    builder.setupNameTable(dict(familyName='CFX Stem Compact' if compact else 'CFX Stem TrueType', styleName='Regular', uniqueFontIdentifier='CFX Stem Test', fullName='CFX Stem Test', psName='CFXStemTest'))
    builder.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200, sxHeight=500, sCapHeight=700)
    builder.setupPost()
    if variable:
        from fontTools.ttLib import newTable
        from fontTools.ttLib.tables.TupleVariation import TupleVariation
        builder.setupFvar([('ital', 0, 0, 1, 'Italic'), ('slnt', -12, 0, 0, 'Slant')], [])
        builder.font['gvar'] = newTable('gvar')
        builder.font['gvar'].variations = {name: [] for name in ORDER}
        # An italic I can retain an upright segment while its top serif extends.
        for tag, peak in [('ital', 1), ('slnt', -1)]:
            region = (0, 1, 1) if peak > 0 else (-1, -1, 0)
            builder.font['gvar'].variations['I'].append(TupleVariation({tag: region}, [(0, 0), (0, 0), (0, 0), (80, 0), (80, 0), (0, 0)] + [(0, 0)] * 4))
    builder.font['head'].created = builder.font['head'].modified = 3800000000
    builder.save(Path(__file__).parent / ('stem-variable.ttf' if variable else 'stem-compact.otf' if compact else 'stem-true-type.ttf'))
