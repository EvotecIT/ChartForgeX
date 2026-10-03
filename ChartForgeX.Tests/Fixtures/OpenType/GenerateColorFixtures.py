"""Original colour-font fixtures: own rectangles, paint graphs and tiny RGBA strikes.

fontTools is a manual regeneration tool, never a product or normal-build dependency.
"""
from pathlib import Path
from math import pi
import struct, zlib
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.colorLib.builder import buildCOLR, buildCPAL
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString
from fontTools.ttLib import newTable
from fontTools.ttLib.tables.DefaultTable import DefaultTable

ROOT = Path(__file__).parent
NAMES = ['.notdef','mono','layers','left','right','fg','linear','radial','sweep',
         'transform','clip','reference','dupe','zwj','couple','woman','computer','cycle']
NAMES += ['mode'+str(i) for i in range(28)] + ['t'+str(i) for i in range(12,32,2)]
MAP = {ord(c):'mono' for c in 'ChartForgeX 0123456789'}
MAP.update({ord('A'):'layers',0x1f600:'layers',0x1f469:'woman',0x1f4bb:'computer',0x200d:'zwj',
            0x1f601:'linear',0x1f602:'radial',0x1f603:'sweep',0x1f604:'transform',0x1f605:'clip',0x1f606:'reference',0x1f607:'dupe',0x1f608:'cycle'})
MAP.update({0xe000+i:'mode'+str(i) for i in range(28)})
MAP.update({0xe100+i:'t'+str(i) for i in range(12,32,2)})

def font(name):
 b=FontBuilder(1000,isTTF=True);b.setupGlyphOrder(NAMES);b.setupCharacterMap(MAP)
 boxes={'left':(-200,-100,600,700),'right':(300,100,1000,800),'fg':(0,400,100,500)}
 outlines={}
 for g in NAMES:
  p=TTGlyphPen(None)
  if g!='zwj':
   x,y,r,t=boxes.get(g,(20,-100,320,400));p.moveTo((x,y));p.lineTo((r,y));p.lineTo((r,t));p.lineTo((x,t));p.closePath()
  outlines[g]=p.glyph()
 b.setupGlyf(outlines);b.setupHorizontalMetrics({g:(1000 if g!='zwj' else 0,boxes.get(g,(20,))[0] if g!='zwj' else 0) for g in NAMES})
 b.setupHorizontalHeader(ascent=800,descent=-200)
 b.setupNameTable(dict(familyName='CFX Colour '+name,styleName='Regular',uniqueFontIdentifier='CFXColour-'+name,fullName='CFX Colour '+name,psName='CFXColour-'+name))
 b.setupOS2(sTypoAscender=800,sTypoDescender=-200,usWinAscent=800,usWinDescent=200);b.setupPost();b.setupMaxp()
 addOpenTypeFeaturesFromString(b.font,'languagesystem DFLT dflt; feature liga {sub woman zwj computer by couple;} liga;')
 b.font['head'].created=b.font['head'].modified=0;b.font.recalcTimestamp=False
 return b.font

def save(f,name):f.save(ROOT/(name+'.ttf'))
def solid(index,alpha=1):return dict(Format=2,PaletteIndex=index,Alpha=alpha)
def glyph(name,paint):return dict(Format=10,Glyph=name,Paint=paint)
def line(extend=0,minimum=0,maximum=1):return dict(Extend=extend,ColorStop=[dict(StopOffset=minimum,PaletteIndex=0,Alpha=1),dict(StopOffset=maximum,PaletteIndex=1,Alpha=1)])
def layers():return dict(Format=1,Layers=[glyph('left',solid(0)),glyph('right',solid(1,.5)),glyph('fg',solid(65535))])
PALETTE=[(1,0,0,1),(0,0,1,1),(0,1,0,1)]
f=font('COLR0');f['CPAL']=buildCPAL([PALETTE,[(1,1,0,1),(1,0,1,1),(0,1,1,1)]])
f['COLR']=buildCOLR({'layers':[('left',0),('right',1),('fg',65535)],'couple':[('left',0),('right',1)]},version=0,glyphMap=f.getReverseGlyphMap());save(f,'color-colr0')
paints={'layers':layers(),'couple':layers(),
 'linear':glyph('left',dict(Format=4,ColorLine=line(),x0=-200,y0=0,x1=600,y1=0,x2=-200,y2=700)),
 'radial':glyph('left',dict(Format=6,ColorLine=line(),x0=200,y0=300,r0=0,x1=200,y1=300,r1=400)),
 'sweep':glyph('left',dict(Format=8,ColorLine=line(),centerX=200,centerY=300,startAngle=0,endAngle=180)),
 'transform':dict(Format=14,Paint=glyph('left',solid(2)),dx=500,dy=300),
 'clip':solid(0),'reference':dict(Format=14,Paint=dict(Format=11,Glyph='clip'),dx=100,dy=100)}
for i in range(28):paints['mode'+str(i)]=dict(Format=32,SourcePaint=glyph('right',solid(1,.5)),CompositeMode=i,BackdropPaint=glyph('left',solid(0,.75)))
for i in range(12,32,2):
 p=dict(Format=i,Paint=glyph('left',solid(2)))
 if i==12:p['Transform']=dict(xx=1,yx=.25,xy=.5,yy=1,dx=100,dy=200)
 elif i==14:p.update(dx=100,dy=200)
 elif i in (16,18):p.update(scaleX=.75,scaleY=1.25)
 elif i in (20,22):p['scale']=.75
 elif i in (24,26):p['angle']=45
 else:p.update(xSkewAngle=22.5,ySkewAngle=22.5)
 if i in (18,22,26,30):p.update(centerX=200,centerY=300)
 paints['t'+str(i)]=p
f=font('COLR1');f['CPAL']=buildCPAL([PALETTE]);f['COLR']=buildCOLR(paints,version=1,glyphMap=f.getReverseGlyphMap(),clipBoxes={'clip':(-300,-150,900,900)})
save(f,'color-colr1')
f=font('Cycle');f['CPAL']=buildCPAL([PALETTE]);f['COLR']=buildCOLR({'cycle':dict(Format=11,Glyph='cycle')},version=1,glyphMap=f.getReverseGlyphMap());save(f,'color-cycle')

def png(size,color):
 def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
 rows=b''.join(b'\0'+bytes(color)*size for y in range(size))
 return b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',size,size,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(rows))+chunk(b'IEND',b'')
def raw(f,tag,data):t=DefaultTable(tag);t.data=data;f[tag]=t
for draw in (False,True):
 f=font('sbix');strikes=[]
 for ppem,color in ((32,(255,0,0,255)),(64,(0,0,255,255))):
  entries={NAMES.index('layers'):struct.pack('>hh4s',-2,-3,b'png ')+png(8,color),NAMES.index('dupe'):struct.pack('>hh4sH',2,3,b'dupe',NAMES.index('layers'))}
  data=b'';offsets=[];head=4+4*(len(NAMES)+1)
  for gid in range(len(NAMES)):offsets.append(head+len(data));data+=entries.get(gid,b'')
  offsets.append(head+len(data));strikes.append(struct.pack('>HH',ppem,72)+struct.pack('>'+'I'*len(offsets),*offsets)+data)
 offsets=[8+4*len(strikes)];offsets.append(offsets[0]+len(strikes[0]))
 raw(f,'sbix',struct.pack('>HHI',1,3 if draw else 1,len(strikes))+struct.pack('>II',*offsets)+b''.join(strikes))
 save(f,'color-sbix-outlines' if draw else 'color-sbix')

# CBDT fixtures exercise all CBLC index formats and PNG image metric locations.
for index_format,image_format in ((1,17),(3,18),(2,19),(4,17),(5,19)):
 f=font('CBDT');gid=NAMES.index('layers');image=png(8,(0,255,0,255));small=struct.pack('>BBbbB',8,8,-2,6,12);big=small+struct.pack('>bbB',0,0,12)
 f['hmtx'].metrics['layers']=(375,-63)
 bitmap=(small if image_format==17 else big if image_format==18 else b'')+struct.pack('>I',len(image))+image
 header=struct.pack('>HHI',index_format,image_format,4)
 if index_format==1:sub=header+struct.pack('>II',0,len(bitmap))
 elif index_format==3:sub=header+struct.pack('>HH',0,len(bitmap))
 elif index_format==2:sub=header+struct.pack('>I',len(bitmap))+big
 elif index_format==4:sub=header+struct.pack('>IHHHH',1,gid,0,gid+1,len(bitmap))
 else:sub=header+struct.pack('>I',len(bitmap))+big+struct.pack('>IH',1,gid)
 array=struct.pack('>HHI',gid,gid,8)+sub
 size=struct.pack('>IIII',56,len(array),1,0)+bytes(24)+struct.pack('>HHBBBb',gid,gid,32,32,32,1)
 raw(f,'CBDT',struct.pack('>I',0x30000)+bitmap);raw(f,'CBLC',struct.pack('>II',0x30000,1)+size+array)
 if index_format==1:
  del f['glyf'];del f['loca']
  for table in f['cmap'].tables:table.cmap={cp:g for cp,g in table.cmap.items() if cp>=128}
 save(f,'color-cbdt'+str(index_format))
