using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Typography;

namespace ChartForgeX.Composition;

/// <summary>Thin forwarding of opted-in legacy canvas text to the common prepared typography owner.</summary>
internal static class VisualCanvasStyledText {
    internal static bool Requested(TextStyleOverride? style) {
        if (style == null) return false;
        if (style.LetterSpacing is double spacing && spacing != 0) return true;
        var ordinary = style.Clone(); ordinary.LetterSpacing = null;
        return ordinary.HasOverrides;
    }
    private static TextStyle Style(TextStyleOverride source, ResolvedVisualCanvasStyle theme, double size, int weight, ChartColor color, TextAlignment alignment) =>
        source.Resolve(new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = weight }, FontSize = size,
            Color = color, Alignment = alignment, LineHeight = 1 });

    internal static VisualScene Plain(VisualCanvasTextLayer text, ResolvedVisualCanvasStyle theme) {
        var style = Style(text.TextStyle!, theme, text.FontSize, text.Emphasized ? VisualCanvasFontWeights.Emphasized : VisualCanvasFontWeights.Regular,
            text.ResolveColor(theme), text.Alignment);
        var layout = TextLayoutEngine.Layout(text.Text, Math.Max(4, text.Width), style, TextWrapMode.NoWrap, 1);
        var face = new VisualSceneTextFace(style);
        var builder = new VisualSceneBuilder(new VisualSize(Math.Max(1, text.Width), Math.Max(1, layout.Metrics.Height)), style.Font);
        var prepared = face.PrepareDisplayed(layout.Lines[0].Text, style.EffectiveFontSize);
        builder.PreparedText(prepared, Anchor(text.Width, style.Alignment), prepared.Ascent + VisualSceneTextFace.BaselineOffset(style),
            style.Color, style.Alignment, "visual-canvas-text", includeDecorations: true);
        return builder.Build();
    }

    internal static VisualScene Hero(VisualCanvasHeroTitleLayer hero, ResolvedVisualCanvasStyle theme) {
        var style = Style(hero.TextStyle!, theme, hero.FontSize, VisualCanvasFontWeights.HeroTitle, ChartColor.White, hero.Alignment);
        var runs = new List<TextContentRun>();
        foreach (var run in hero.Runs) runs.Add(new TextContentRun(TextCaseTransformer.Apply(run.Text, style.TextCase, System.Globalization.CultureInfo.InvariantCulture)));
        style.TextCase = TextCaseTransform.None;
        var content = TextContent.FromRuns(runs); var preferred = style.FontSize;
        VisualSceneInlineTextLine Prepare(double size) { var candidate = style.Clone(); candidate.FontSize = size; return new VisualSceneInlineTextContext(candidate).Prepare(content, runs); }
        var selected = Prepare(preferred); var size = preferred;
        for (var attempt = 0; attempt < 40 && selected.Width > hero.Width && size > 1; attempt++) {
            size = Math.Max(1, Math.Min(size * .99, size * hero.Width / selected.Width)); selected = Prepare(size);
        }
        style.FontSize = size;
        var top = (preferred - size) / 2;
        var builder = new VisualSceneBuilder(new VisualSize(Math.Max(1, hero.Width), Math.Max(1, selected.LineHeight + top)), style.Font);
        var left = TextLeft(hero.Width, selected.Width, style.Alignment);
        foreach (var piece in selected.Pieces) {
            var color = hero.TextStyle!.Color ?? hero.Runs[piece.Owner].Color;
            builder.PreparedText(piece.Text, left + piece.Offset, top + selected.Ascent + VisualSceneTextFace.BaselineOffset(style),
                color, TextAlignment.Left, "visual-canvas-hero-title", includeDecorations: true);
        }
        return builder.Build();
    }
    internal static void WriteSvg(SvgMarkupWriter writer, VisualScene scene, double x, double y, string prefix) {
        var svg = VisualSceneSvgRenderer.Render(scene, decorative: true, idPrefix: prefix,
            options: new VisualSvgOptions().WithTextPresentation(VisualSvgTextPresentation.Automatic));
        // This is the existing owner's generated root, not source markup or a second text serializer.
        writer.StartElement("g").Attribute("transform", "translate(" + x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " " + y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ")").EndStartElement();
        writer.Raw("<svg overflow=\"visible\"" + svg.Substring(4)); writer.EndElement();
    }
    private static double Anchor(double width, TextAlignment alignment) => alignment == TextAlignment.Center ? width / 2 : alignment == TextAlignment.Right ? width : 0;
    private static double TextLeft(double width, double measured, TextAlignment alignment) => alignment == TextAlignment.Center ? (width - measured) / 2 : alignment == TextAlignment.Right ? width - measured : 0;
}
