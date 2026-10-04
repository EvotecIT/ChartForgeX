"""Original geometric variable fonts. Test-only fontTools generates OpenType tables;
no font outlines or production font-engine implementation are copied.
Run this file with fontTools installed to regenerate the checked-in fixtures.
"""
from pathlib import Path
from tempfile import TemporaryDirectory
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.misc.psCharStrings import T2CharString
from fontTools.designspaceLib import DesignSpaceDocument, AxisDescriptor, SourceDescriptor
from fontTools.varLib import build
from fontTools.varLib.featureVars import addFeatureVariations
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString
from fontTools.ttLib import newTable
from fontTools.ttLib.tables.TupleVariation import TupleVariation

DEST = Path(__file__).parent
ORDER = ['.notdef', 'space', 'A', 'B', 'H', 'x', 'curve', 'acute', 'composite', 'scaled', 'componentMetrics', 'multiMetrics', 'nestedMetrics']
CMAP = {c: 'A' for c in range(33, 127)}
CMAP.update({32: 'space', 65: 'A', 66: 'B', 72: 'H', 120: 'x', 88: 'curve', 233: 'composite', 0x301: 'acute', 67: 'scaled', 77: 'componentMetrics', 68: 'multiMetrics', 78: 'nestedMetrics'})

def names(builder, family):
    builder.setupNameTable(dict(familyName=family, styleName='Regular', uniqueFontIdentifier=family,
                               fullName=family, psName=family.replace(' ', '')))
    builder.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=900, usWinDescent=200,
                     sxHeight=500, sCapHeight=700)
    builder.setupPost()

def master(weight, width):
    # Changes affect bounds, advances, component placement, marks and pair positioning.
    w = (weight - 400) / 5
    stretch = width / 100
    glyphs = {}
    for name in ORDER:
        pen = TTGlyphPen(None)
        if name not in ['.notdef', 'space', 'composite', 'scaled', 'componentMetrics', 'multiMetrics', 'nestedMetrics']:
            left = 100 + w / 4
            right = (500 + w) * stretch
            top = (500 if name == 'x' else 700) + w / 2
            if name == 'acute': left, right, top = 0, 100 + w / 2, 100
            if name == 'B': right += 100
            pen.moveTo((left, 0)); pen.lineTo((right, 0)); pen.lineTo((right, top))
            if name == 'curve': pen.qCurveTo((300 * stretch, top + 150 + w), (left, top))
            else: pen.lineTo((left, top))
            pen.closePath()
        if name == 'composite':
            pen = TTGlyphPen({n: glyphs[n] for n in glyphs})
            pen.addComponent('A', (1, 0, 0, 1, 0, 0))
            pen.addComponent('acute', (1, 0, 0, 1, 250 + w, 750 + w / 2))
        if name in ['scaled', 'componentMetrics']:
            pen = TTGlyphPen({n: glyphs[n] for n in glyphs})
            pen.addComponent('A', (.5, 0, 0, .75, 100 + w, 200 + w / 2))
        if name == 'multiMetrics':
            pen = TTGlyphPen({n: glyphs[n] for n in glyphs})
            pen.addComponent('acute', (1, 0, 0, 1, 0, 0)); pen.addComponent('H', (1, 0, 0, 1, 150, 0))
        if name == 'nestedMetrics':
            pen = TTGlyphPen({n: glyphs[n] for n in glyphs})
            pen.addComponent('multiMetrics', (.5, 0, 0, 1, 200, 0))
        glyphs[name] = pen.glyph()
        if name == 'scaled': glyphs[name].components[0].flags |= 0x800  # scaled component offset
        if name == 'componentMetrics': glyphs[name].components[0].flags |= 0x200  # component metrics
        if name in ['multiMetrics', 'nestedMetrics']:
            for component in glyphs[name].components: component.flags |= 0x200
    builder = FontBuilder(1000, isTTF=True)
    builder.setupGlyphOrder(ORDER); builder.setupCharacterMap(CMAP); builder.setupGlyf(glyphs)
    builder.setupHorizontalMetrics({name: (0 if name == 'acute' else round((600 + w) * stretch), round(glyphs[name].xMin if hasattr(glyphs[name], 'xMin') else 0)) for name in ORDER})
    builder.font['hmtx'].metrics['componentMetrics'] = (round(1600 + w * 2), 100)
    builder.setupHorizontalHeader(ascent=round(800 + w / 2), descent=-200)
    names(builder, 'CFX Variation Fixture')
    builder.font['OS/2'].sTypoAscender = round(800 + w / 2)
    builder.font['OS/2'].sxHeight = round(500 + w / 2)
    builder.font['OS/2'].sCapHeight = round(700 + w / 2)
    pair = round(-20 + w / 3)
    anchor = round(350 + w)
    addOpenTypeFeaturesFromString(builder.font, f'''languagesystem DFLT dflt;
        markClass acute <anchor 50 0> @TOP;
        feature kern {{ pos A B {pair}; }} kern;
        feature mark {{ pos base A <anchor {anchor} 750> mark @TOP; }} mark;
    ''')
    return builder.font

def true_type():
    with TemporaryDirectory() as folder:
        design = DesignSpaceDocument()
        for tag, minimum, default, maximum in [('wght', 100, 400, 900), ('wdth', 75, 100, 125)]:
            axis = AxisDescriptor(); axis.name = tag; axis.tag = tag
            axis.minimum = minimum; axis.default = default; axis.maximum = maximum; design.addAxis(axis)
        for i, (weight, width) in enumerate([(400, 100), (100, 100), (650, 100), (900, 100), (400, 75), (400, 125)]):
            path = Path(folder) / f'master-{i}.ttf'; master(weight, width).save(path)
            source = SourceDescriptor(); source.path = str(path); source.name = f'master-{i}'
            source.location = dict(wght=weight, wdth=width)
            if i == 0: source.copyInfo = source.copyLib = source.copyFeatures = True
            design.addSource(source)
        font, _, _ = build(design)
        font['avar'] = newTable('avar'); font['avar'].segments = {'wght': {-1: -1, 0: 0, .5: .75, 1: 1}, 'wdth': {-1: -1, 0: 0, 1: 1}}
        addFeatureVariations(font, [([{'wght': (.9, 1)}], {'A': 'B'})])
        font.save(DEST / 'variable-true-type.ttf')
        del font['HVAR']; font.save(DEST / 'variable-phantom-metrics.ttf')
        font['gvar'].variations['H'] = [TupleVariation({'wght': (0, 1, 1)}, [(0, 0)] * 4 + [(50, 0), (150, 0), (0, 0), (0, 0)])]
        font.save(DEST / 'variable-origin-metrics.ttf')
        font['GSUB'].table.FeatureVariations = None
        addFeatureVariations(font, [([{'wght': (-1, 0)}], {'H': 'x'})])
        font.save(DEST / 'variable-default-feature.ttf')

def compact():
    builder = FontBuilder(1000, isTTF=False)
    builder.setupGlyphOrder(ORDER); builder.setupCharacterMap(CMAP)
    builder.setupHorizontalMetrics({name: (600, 100) for name in ORDER})
    builder.setupHorizontalHeader(ascent=800, descent=-200); names(builder, 'CFX Variable Compact')
    builder.setupFvar([('wght', 100, 400, 900, 'Weight')], [])
    program = [100, 200, 1, 'blend', 0, 'rmoveto', 400, 50, 1, 'blend', 'hlineto', 700, 'vlineto', -400, -50, 1, 'blend', 'hlineto']
    builder.setupCFF2({name: T2CharString(program=program if name not in ['.notdef', 'space'] else []) for name in ORDER}, regions=[{'wght': (0, 1, 1)}])
    builder.save(DEST / 'variable-compact.otf')
    malformed = [16384, 16384, 'add', 'dup', 'mul', 'blend']
    static_program = [100, 0, 'rmoveto', 400, 'hlineto', 700, 'vlineto', -400, 'hlineto']
    builder.setupCFF2({name: T2CharString(program=malformed if name == 'H' else static_program if name not in ['.notdef', 'space'] else []) for name in ORDER}, regions=[{'wght': (0, 1, 1)}, {'wght': (-1, -1, 0)}])
    builder.font.recalcBBoxes = False  # Deliberately malformed input must not be evaluated by the generator.
    builder.save(DEST / 'variable-malformed.otf')

def empty_metrics():
    from fontTools.ttLib import TTFont
    from fontTools.ttLib.tables._g_l_y_f import GlyphComponent
    font = TTFont(DEST / 'variable-origin-metrics.ttf')
    # Decode tuples before changing the composite's point count.
    font['gvar'].variations = dict(font['gvar'].variations)
    for variation in font['gvar'].variations['multiMetrics']:
        variation.coordinates.insert(len(variation.coordinates) - 4, (0, 0))
    component = GlyphComponent()
    component.glyphName = 'space'; component.x = component.y = 0
    component.flags = 0x200  # Last USE_MY_METRICS component has no contour points.
    font['glyf']['multiMetrics'].components.append(component)
    font['gvar'].variations['space'] = [TupleVariation({'wght': (0, 1, 1)}, [(50, 0), (150, 0), (0, 0), (0, 0)])]
    font.save(DEST / 'variable-empty-metrics.ttf')

if __name__ == '__main__':
    true_type(); compact(); empty_metrics()
