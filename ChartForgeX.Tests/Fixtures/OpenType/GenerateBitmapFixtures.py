"""Original EBDT fixtures: five image layouts and four coverage depths.

fontTools is used only for manual fixture regeneration, never shipped or required to build.
"""
from pathlib import Path
import struct
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib.tables.DefaultTable import DefaultTable
from fontTools.ttLib import newTable
from fontTools.ttLib.tables._f_v_a_r import Axis

ROOT = Path(__file__).parent
PATTERN = [[0, 1, 0, 1, 1], [1, 0, 1, 0, 1], [1, 1, 0, 1, 0]]

def base():
    b = FontBuilder(1000, isTTF=True)
    names = ['.notdef', 'A', 'overhang']
    b.setupGlyphOrder(names)
    b.setupCharacterMap({65: 'A', 66: 'overhang'})
    outlines = {}
    for g in names:
        pen = TTGlyphPen(None)
        pen.moveTo((0, 0)); pen.lineTo((300, 0)); pen.lineTo((300, 500)); pen.lineTo((0, 500)); pen.closePath()
        outlines[g] = pen.glyph()
    b.setupGlyf(outlines)
    b.setupHorizontalMetrics({g: (600, 0) for g in names})
    b.setupHorizontalHeader(ascent=800, descent=-200)
    b.setupNameTable(dict(familyName='CFX Embedded Strikes', styleName='Regular', uniqueFontIdentifier='CFXEmbeddedStrikes', fullName='CFX Embedded Strikes', psName='CFXEmbeddedStrikes'))
    b.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200)
    b.setupPost(); b.setupMaxp()
    b.font['head'].created = b.font['head'].modified = 0
    b.font.recalcTimestamp = False
    return b.font

def table(f, tag, data):
    t = DefaultTable(tag); t.data = data; f[tag] = t

def fixture(index, image, depth, bearing=-2, top=7, name=None, variable=False):
    f = base(); packed = bytearray(); bits = []
    mask = (1 << depth) - 1
    # Grayscale fixtures include intermediate coverage, not just opaque bits.
    values = [[(mask if v else 0) if depth == 1 else (x + y) % (mask + 1) for x, v in enumerate(row)] for y, row in enumerate(PATTERN)]
    for row in values:
        for v in row: bits.extend((v >> shift) & 1 for shift in range(depth - 1, -1, -1))
        if image in (1, 6): bits.extend([0] * ((-len(bits)) % 8))
    bits.extend([0] * ((-len(bits)) % 8))
    for at in range(0, len(bits), 8): packed.append(sum(v << (7 - i) for i, v in enumerate(bits[at:at+8])))
    small = struct.pack('>BBbbB', 3, 5, bearing, top, 6)
    big = small + struct.pack('>bbB', 0, 0, 6)
    bitmap = (small if image in (1, 2) else big if image in (6, 7) else b'') + packed
    header = struct.pack('>HHI', index, image, 4)
    if index == 1: sub = header + struct.pack('>II', 0, len(bitmap))
    elif index == 3: sub = header + struct.pack('>HH', 0, len(bitmap))
    elif index == 2: sub = header + struct.pack('>I', len(bitmap)) + big
    elif index == 4: sub = header + struct.pack('>IHHHH', 1, 1, 0, 2, len(bitmap))
    else: sub = header + struct.pack('>I', len(bitmap)) + big + struct.pack('>IH', 1, 1)
    size = struct.pack('>IIII', 56, 8 + len(sub), 1, 0) + b'\0' * 24 + struct.pack('>HHBBBB', 1, 1, 12, 12, depth, 1)
    table(f, 'EBLC', struct.pack('>II', 0x20000, 1) + size + struct.pack('>HHI', 1, 1, 8) + sub)
    table(f, 'EBDT', struct.pack('>I', 0x20000) + bitmap)
    if variable:
        variation = newTable('fvar'); axis = Axis()
        axis.axisTag = 'wght'; axis.minValue = 100; axis.defaultValue = 400; axis.maxValue = 900
        axis.flags = 0; axis.axisNameID = f['name'].addName('Weight')
        variation.axes = [axis]; variation.instances = []; f['fvar'] = variation
    f.save(ROOT / ((name or f'bitmap-{index}-{image}-{depth}') + '.ttf'))

for index, image in ((1, 1), (3, 2), (2, 5), (4, 6), (5, 5), (3, 7)):
    fixture(index, image, 1)
for depth in (2, 4, 8): fixture(3, 7, depth)
fixture(1, 1, 1, bearing=-18, top=22, name='bitmap-overhang')
fixture(1, 1, 1, name='bitmap-variable', variable=True)
