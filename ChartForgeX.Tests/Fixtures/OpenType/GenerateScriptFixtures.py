"""Original script fixtures: simple rectangles, own mappings and small OpenType rules.

fontTools is only used when maintainers regenerate these checked-in test fonts.
No third-party outlines or shaping code are included or needed by normal builds.
"""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString

ROOT = Path(__file__).parent
NAMES = ['.notdef','ka','ta','ra','halant','i','aa','e','o','nukta','reph',
         'halfKa','belowRa','kaRa','conjunct','longI','nuktaKa','zwj','zwnj',
         'circle','tone','ring','coengRo','belowTa','above','post','eyelash']
WIDTHS = {g:500 for g in NAMES}
WIDTHS.update(halant=0,nukta=0,reph=0,belowRa=0,zwj=0,zwnj=0,tone=0,ring=0,above=0,halfKa=250,kaRa=550,conjunct=600)
INDIC = {
 'deva':('dev2',[0x915,0x924,0x930,0x94d,0x93f,0x93e,0x947,0x94b,0x93c]),
 'beng':('bng2',[0x995,0x9a4,0x9b0,0x9cd,0x9bf,0x9be,0x9c7,0x9cb,0x9bc]),
 'guru':('gur2',[0xa15,0xa24,0xa30,0xa4d,0xa3f,0xa3e,0xa47,0xa4b,0xa3c]),
 'gujr':('gjr2',[0xa95,0xaa4,0xab0,0xacd,0xabf,0xabe,0xac7,0xacb,0xabc]),
 'orya':('ory2',[0xb15,0xb24,0xb30,0xb4d,0xb3f,0xb3e,0xb47,0xb4b,0xb3c]),
 'taml':('tml2',[0xb95,0xba4,0xbb0,0xbcd,0xbbf,0xbbe,0xbc6,0xbca,0x0]),
 'telu':('tel2',[0xc15,0xc24,0xc30,0xc4d,0xc3f,0xc3e,0xc46,0xc4a,0x0]),
 'knda':('knd2',[0xc95,0xca4,0xcb0,0xccd,0xcbf,0xcbe,0xcc6,0xcca,0xcbc]),
 'mlym':('mlm2',[0xd15,0xd24,0xd30,0xd4d,0xd3f,0xd3e,0xd46,0xd4a,0x0])}

def build(name,mapping,scripts,features):
 b=FontBuilder(1000,isTTF=True); b.setupGlyphOrder(NAMES); b.setupCharacterMap(mapping)
 outlines={}
 for i,g in enumerate(NAMES):
  pen=TTGlyphPen(None)
  if g not in ('.notdef','zwj','zwnj'):
   left=20+i*3; top=150 if WIDTHS[g]==0 else 500+i*3
   pen.moveTo((left,0));pen.lineTo((left+90,0));pen.lineTo((left+90,top));pen.lineTo((left,top));pen.closePath()
  outlines[g]=pen.glyph()
 b.setupGlyf(outlines);b.setupHorizontalMetrics({g:(WIDTHS[g],0) for g in NAMES})
 b.setupHorizontalHeader(ascent=800,descent=-200)
 b.setupNameTable(dict(familyName='CFX Script '+name,styleName='Regular',uniqueFontIdentifier='CFXScript-'+name,fullName='CFX Script '+name,psName='CFXScript-'+name))
 b.setupOS2(sTypoAscender=800,sTypoDescender=-200,usWinAscent=800,usWinDescent=200);b.setupPost();b.setupMaxp()
 addOpenTypeFeaturesFromString(b.font,''.join('languagesystem '+s+' dflt;\n' for s in scripts)+features)
 b.font['head'].created=b.font['head'].modified=0;b.font.recalcTimestamp=False;b.font.save(ROOT/(name+'.ttf'))
 return b.font

BASEMAP={0x200d:'zwj',0x200c:'zwnj',0x25cc:'circle',0x951:'tone',ord(' '):'aa'}
BASEMAP.update({ord(c):'ka' for c in 'ChartForgeX0123456789'})
BASEMAP.update({ord('.'):'aa',0xa0:'aa'})
mapping=dict(BASEMAP)
for modern,cps in INDIC.values(): mapping.update({cp:g for cp,g in zip(cps,['ka','ta','ra','halant','i','aa','e','o','nukta']) if cp})
mapping[0x958]='nuktaKa'
FORMS='''
feature nukt { sub ka nukta by nuktaKa; } nukt;
feature akhn { sub ka halant ta by conjunct; sub ra halant zwj by eyelash; } akhn;
feature rphf { sub ra halant by reph; } rphf;
feature half { sub ka halant by halfKa; } half;
feature vatu { sub ka belowRa by kaRa; } vatu;
feature pres { sub i' kaRa by longI; } pres;
markClass reph <anchor 0 0> @above;
feature abvm { pos base ka <anchor 200 700> mark @above; } abvm;
'''
build('indic-modern',mapping,[v[0] for v in INDIC.values()],FORMS+'feature blwf { sub halant ra by belowRa; } blwf;')
build('indic-legacy',mapping,list(INDIC),FORMS+'feature blwf { sub ra halant by belowRa; } blwf;')
mixed=build('indic-mixed-tags',mapping,list(INDIC),FORMS+'feature blwf { sub ra halant by belowRa; } blwf;')
for record in mixed['GPOS'].table.ScriptList.ScriptRecord: record.ScriptTag=INDIC[record.ScriptTag][0]
mixed['GPOS'].table.ScriptList.ScriptRecord.sort(key=lambda r:r.ScriptTag)
mixed.save(ROOT/'indic-mixed-tags.ttf')
thai=dict(BASEMAP)
for offset in (0,0x80): thai.update({0xe01+offset:'ka',0xe33+offset:'o',0xe32+offset:'aa',0xe4d+offset:'ring',0xe48+offset:'tone',0xe34+offset:'i'})
build('thai-script',thai,['thai','lao '],'''
markClass ring <anchor 0 0> @ring;
markClass tone <anchor 0 0> @tone;
feature mark { pos base ka <anchor 250 700> mark @ring <anchor 250 700> mark @tone; pos base circle <anchor 250 700> mark @ring <anchor 250 700> mark @tone; } mark;
feature mkmk { pos mark ring <anchor 0 200> mark @tone; } mkmk;
''')
khmer=dict(BASEMAP);khmer.update({0x1780:'ka',0x178f:'ta',0x179a:'ra',0x17d2:'halant',0x17c1:'e',0x17be:'i',0x17c4:'o',0x17b8:'above',0x17b6:'aa',0x17ca:'nukta'})
build('khmer-script',khmer,['khmr'],'''
feature pref { sub halant ra by coengRo; } pref;
feature blwf { sub halant ta by belowTa; sub nukta' [i above] by ring; } blwf;
feature abvf { sub i by above; } abvf;
feature pstf { sub o by post; } pstf;
''')
fallback=dict(mapping);del fallback[0x924]
build('indic-primary',fallback,['dev2'],FORMS+'feature blwf { sub halant ra by belowRa; } blwf;')
build('indic-growth',mapping,['dev2'],''.join('feature '+f+' { sub ka by ka ka ka ka; } '+f+';\n' for f in ['nukt','akhn','rkrf','cjct']))
