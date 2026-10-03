"""Original rectangular outlines and script rules for portable Sinhala/Myanmar tests.

fontTools is a regeneration-only tool. No reference font outlines or shaping engine
code are included in these fixtures or the normal build.
"""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString

ROOT = Path(__file__).parent
NAMES = ['.notdef', 'ka', 'ga', 'ra', 'ya', 'halant', 'asat', 'e', 'aa', 'o',
         'i', 'circle', 'zwj', 'zwnj', 'reph', 'rakar', 'stack', 'medialRa',
         'u', 'anusvara', 'wideRa', 'deadKa', 'postYa', 'au', 'space']
WIDTHS = {name: 500 for name in NAMES}
WIDTHS.update(halant=0, asat=0, zwj=0, zwnj=0, reph=0, rakar=240,
              stack=0, medialRa=200, u=0, anusvara=0, wideRa=200, space=250)

def build(name, mapping, scripts, features, circle=True):
    builder = FontBuilder(1000, isTTF=True)
    builder.setupGlyphOrder(NAMES)
    cmap = {**{ord(c):'ka' for c in 'ChartForgeX0123456789'},
        0x25cc:'circle', 0x200d:'zwj', 0x200c:'zwnj', 32:'space', **mapping}
    if not circle:
        del cmap[0x25cc]
    builder.setupCharacterMap(cmap)
    outlines = {}
    for index, glyph in enumerate(NAMES):
        pen = TTGlyphPen(None)
        if glyph not in ('.notdef', 'zwj', 'zwnj', 'space'):
            x = index * 3 + 15
            pen.moveTo((x, 0)); pen.lineTo((x+80, 0)); pen.lineTo((x+80, 400+index*5)); pen.lineTo((x, 400+index*5)); pen.closePath()
        outlines[glyph] = pen.glyph()
    builder.setupGlyf(outlines)
    builder.setupHorizontalMetrics({g:(WIDTHS[g],0) for g in NAMES})
    builder.setupHorizontalHeader(ascent=800, descent=-200)
    builder.setupNameTable(dict(familyName='CFX '+name, styleName='Regular', uniqueFontIdentifier='CFX-'+name, fullName='CFX '+name, psName='CFX-'+name))
    builder.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200)
    builder.setupPost(); builder.setupMaxp()
    addOpenTypeFeaturesFromString(builder.font, ''.join('languagesystem '+tag+' dflt;\n' for tag in scripts)+features)
    builder.font['head'].created = builder.font['head'].modified = 0
    builder.font.recalcTimestamp = False
    builder.font.save(ROOT/(name+'.ttf'))

sinhala = {0xd9a:'ka',0xd9c:'ga',0xdbb:'ra',0xdba:'ya',0xdca:'halant',
           0xdd9:'e',0xdcf:'aa',0xddc:'o',0xdda:'o',0xddd:'o',0xdde:'o',0xddf:'au',0xdd2:'i'}
build('sinhala-script', sinhala, ['sinh'], '''
markClass reph <anchor 0 0> @above;
markClass rakar <anchor 0 0> @below;
markClass i <anchor 0 0> @above;
feature rphf { sub ra halant zwj by reph; } rphf;
feature vatu { sub halant zwj ra by rakar; sub halant zwj ya by postYa; } vatu;
feature psts { sub ka halant by deadKa; } psts;
feature abvm { pos base ka <anchor 250 700> mark @above; pos base circle <anchor 250 700> mark @above; } abvm;
feature blwm { pos base ka <anchor 250 -100> mark @below; } blwm;
''')
myanmar = {0x1000:'ka',0x1001:'ga',0x1004:'ra',0x101b:'ra',0x105a:'ra',
           0x1039:'halant',0x103a:'asat',0x1031:'e',0x1084:'e',0x102c:'aa',
           0x103c:'medialRa',0x102f:'u',0x1030:'u',0x1036:'anusvara',0x1040:'ka',0x1041:'ga'}
rules = '''
markClass reph <anchor 0 0> @above;
markClass anusvara <anchor 0 0> @above;
markClass stack <anchor 0 0> @below;
markClass u <anchor 0 0> @below;
markClass medialRa <anchor 0 0> @medial;
markClass wideRa <anchor 0 0> @medial;
feature rphf { sub ra asat halant by reph; } rphf;
feature pref { sub medialRa' ka by wideRa; } pref;
feature blwf { sub halant ka by stack; } blwf;
feature dist { pos medialRa <0 0 200 0>; pos wideRa <0 0 240 0>; } dist;
feature mark { pos base ka <anchor 250 700> mark @above <anchor 250 -100> mark @below; pos base circle <anchor 250 700> mark @above; } mark;
'''
build('myanmar-script', myanmar, ['mym2'], rules)
build('myanmar-no-circle', myanmar, ['mym2'], rules, circle=False)
build('myanmar-preprocessed', myanmar, ['mym2'], 'feature ccmp { sub ra asat halant by reph; } ccmp;' + rules)
primary = dict(myanmar); del primary[0x1000]
build('myanmar-primary', primary, ['mym2'], rules)

# The shared normalisation owner must not compose vowel pieces across an explicit joiner.
joiner_vowels = {0xc95:'ka',0xcc6:'e',0xcc2:'aa',0xcd5:'i',0xcca:'o',0xccb:'au',
                 0xc15:'ga',0xc46:'u',0xc56:'anusvara',0xc48:'postYa'}
build('indic-joiner-vowels', joiner_vowels, ['knd2','tel2'], '''
feature ccmp { sub ka e aa i by au; sub ga u anusvara by postYa; } ccmp;
''')
