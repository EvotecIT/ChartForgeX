"""Original size-positioning fixtures; fontTools is optional test-only regeneration tooling."""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString

root=Path(__file__).parent
names=['.notdef','H','I','J','K','L','M','N','space','acute','grave','beh','tah','H_I','O','P']
builder=FontBuilder(1000,isTTF=True)
builder.setupGlyphOrder(names)
builder.setupCharacterMap({**{ord(c):'H' for c in 'ChartForgeX 0123456789'},**{ord(c):c for c in 'HIJKLMNOP'},32:'space',0x301:'acute',0x300:'grave',0x628:'beh',0x62a:'tah'})
outlines={}
for i,name in enumerate(names):
    pen=TTGlyphPen(None)
    if name not in ('.notdef','space'):
        width=120 if name in ('acute','grave') else 400
        height=120 if name in ('acute','grave') else 700
        pen.moveTo((50,0));pen.lineTo((50+width,0));pen.lineTo((50+width,height));pen.lineTo((50,height));pen.closePath()
    outlines[name]=pen.glyph()
builder.setupGlyf(outlines)
builder.setupHorizontalMetrics({n:(0 if n in ('acute','grave') else 1200 if n=='H_I' else 600,50) for n in names})
builder.setupHorizontalHeader(ascent=900,descent=-200)
builder.setupNameTable(dict(familyName='CFX Device Proof',styleName='Regular',uniqueFontIdentifier='CFXDeviceProof',fullName='CFX Device Proof',psName='CFXDeviceProof'))
builder.setupOS2(sTypoAscender=900,sTypoDescender=-200,usWinAscent=900,usWinDescent=200)
builder.setupPost();builder.setupMaxp()
addOpenTypeFeaturesFromString(builder.font, '''
languagesystem DFLT dflt;
languagesystem latn dflt;
languagesystem arab dflt;
markClass acute <anchor 60 100 <device 12 -1, 13 1> <device 12 1, 13 -1>> @TOP;
markClass grave <anchor 60 100 <device 12 1> <device 12 -1>> @TOP;
feature liga { sub H I by H_I; } liga;
feature kern {
 lookup Single { pos H <10 20 30 0 <device 12 1, 13 -2, 14 1, 15 -1, 16 0, 17 1, 18 -1, 19 0, 20 -2, 21 1> <device 12 -1, 13 1> <device 12 2, 13 -2> <device NULL>>; } Single;
 lookup SingleArray { pos O <0 0 40 0 <device NULL> <device NULL> <device 12 5> <device NULL>>; pos P <0 0 -60 0 <device NULL> <device NULL> <device 12 -3> <device NULL>>; } SingleArray;
 lookup Pair { pos I <0 0 -40 0 <device NULL> <device NULL> <device 12 3, 13 -4, 14 7, 15 -8> <device NULL>> J <0 0 0 0 <device 12 -2> <device NULL> <device NULL> <device NULL>>; } Pair;
 lookup Classes { pos [K L] [M N] <0 0 -50 0 <device NULL> <device NULL> <device 12 20, 13 -25, 14 127, 15 -128> <device NULL>>; } Classes;
} kern;
feature mark {
 pos base H <anchor 300 700 <device 12 2, 13 -1> <device 12 3, 13 -2>> mark @TOP;
 pos ligature H_I <anchor 300 700 <device 12 1> <device 12 -1>> mark @TOP ligComponent <anchor 900 700 <device 12 2> <device 12 2>> mark @TOP;
} mark;
feature mkmk { pos mark acute <anchor 60 200 <device 12 -2> <device 12 2>> mark @TOP; } mkmk;
feature curs {
 script arab;
 pos cursive beh <anchor NULL> <anchor 500 300 <device 12 2> <device 12 -2>>;
 pos cursive tah <anchor 50 200 <device 12 -1> <device 12 1>> <anchor NULL>;
} curs;
''')
builder.font['head'].created=builder.font['head'].modified=0
builder.font.recalcTimestamp=False
builder.font.save(root/'device-positioning.ttf')
