"""Original rectangular glyphs for regular and bold cluster fallback. Manual, test-only generation."""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

for role, weight, character in [('primary', 900, 65), ('regular', 400, 66), ('bold', 700, 66)]:
    builder=FontBuilder(1000,isTTF=True); names=['.notdef','mark']
    builder.setupGlyphOrder(names)
    cmap={ord(ch):'mark' for ch in 'ChartForgeX 0123456789'};cmap[character]='mark'
    builder.setupCharacterMap(cmap)
    glyphs={}
    for name in names:
        pen=TTGlyphPen(None);pen.moveTo((0,0));pen.lineTo((200,0));pen.lineTo((200,500));pen.lineTo((0,500));pen.closePath();glyphs[name]=pen.glyph()
    builder.setupGlyf(glyphs);builder.setupHorizontalMetrics({n:(600,0) for n in names})
    builder.setupHorizontalHeader(ascent=800,descent=-200)
    builder.setupNameTable(dict(familyName='CFX Fallback '+role,styleName=role,uniqueFontIdentifier='CFXFallback'+role,fullName='CFX Fallback '+role,psName='CFXFallback'+role))
    builder.setupOS2(usWeightClass=weight,sTypoAscender=800,sTypoDescender=-200,usWinAscent=800,usWinDescent=200)
    builder.setupPost();builder.setupMaxp();builder.font['head'].created=builder.font['head'].modified=0;builder.font.recalcTimestamp=False
    builder.save(Path(__file__).with_name('fallback-weight-'+role+'.ttf'))
