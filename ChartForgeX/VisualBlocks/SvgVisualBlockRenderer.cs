using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

/// <summary>Renders genuine core diagrams and delegates other blocks to their static producer.</summary>
public sealed partial class SvgVisualBlockRenderer {
    /// <summary>Renders a genuine diagram or delegates static output to its owning producer.</summary>
    public string Render(IVisualBlock block) => Render(block, string.Empty);
    /// <summary>Renders static output with a caller-provided embedding identity.</summary>
    public string Render(IVisualBlock block, string idScope) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        return IsNativeBlock(block) ? RenderNative(block, idScope) : block.RenderSvg(idScope);
    }
    internal string RenderNative(IVisualBlock block, string idScope) {
        VisualBlockRendering.Validate(block);
        return RenderOwned(block, idScope, RenderCore);
    }
    internal static bool IsNativeBlock(IVisualBlock block) => block is PacketLayoutBlock || block is BlockLayoutBlock || block is GitGraphBlock || block is VennDiagramBlock || block is FishboneDiagramBlock || block is WardleyMapBlock;

    /// <summary>Renders a visual block to SVG markup with a caller-provided ID scope.</summary>
    internal static string RenderOwned(IVisualBlock block, string idScope, Func<IVisualBlock, string, string> render) {
        var scope = idScope ?? string.Empty;
        var provisionalId = BuildProvisionalId(block, scope);
        var svg = render(block, provisionalId);
        if (block.Options.Theme.UseGraphiteLayout) {
            var font = new Typography.FontSpec { Family = block.Options.Theme.FontFamily };
            using var measurement = ChartLabelScene.OpenFontScope(font);
            svg = ChartLabelScene.Create(svg, font).ToSvg();
        }
        return BindVisualIdentity(svg, provisionalId, scope);
    }

    internal ChartLabelScene RenderLabelScene(IVisualBlock block) {
        var font = new Typography.FontSpec { Family = block.Options.Theme.FontFamily };
        using var measurement = ChartLabelScene.OpenFontScope(font);
        return ChartLabelScene.Create(RenderCore(block, BuildProvisionalId(block, string.Empty)), font);
    }

    internal static string RenderSurface(IVisualBlock block, string id, Action<SvgMarkupWriter> renderContent) {
        var options = block.Options;
        var theme = options.Theme;
        var accessibility = options.Accessibility;
        var surfaceBackground = VisualBlockRendering.SurfaceBackground(options);
        var writer = new SvgMarkupWriter(4096);
        writer.StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("id", id)
            .Attribute("width", options.Size.Width)
            .Attribute("height", options.Size.Height)
            .Attribute("viewBox", "0 0 " + options.Size.Width.ToString(CultureInfo.InvariantCulture) + " " + options.Size.Height.ToString(CultureInfo.InvariantCulture))
            .Attribute("role", accessibility.IsDecorative ? null : "img")
            .Attribute("aria-hidden", accessibility.IsDecorative ? "true" : null)
            .Attribute("lang", accessibility.Language)
            .Attribute("aria-labelledby", accessibility.IsDecorative ? null : id + "-title " + id + "-desc")
            .Attribute("preserveAspectRatio", "xMidYMid meet")
            .Attribute("shape-rendering", "geometricPrecision")
            .Attribute("text-rendering", "geometricPrecision")
            .Attribute("data-cfx-look", theme.UseGraphiteLayout ? "graphite" : null)
            .Attribute("data-cfx-host-frame", options.HostOwnsFrame ? "true" : null)
            .Attribute("style", "max-width:100%;height:auto;display:block")
            .EndStartElement()
            .Line();
        if (!accessibility.IsDecorative) writer
            .StartElement("title").Attribute("id", id + "-title").Text(block.AccessibleName).EndElement()
            .Line()
            .StartElement("desc").Attribute("id", id + "-desc").Text(accessibility.Description ?? "Static ChartForgeX visual block.").EndElement()
            .Line();

        writer.StartElement("defs").EndStartElement().Line();
        SvgSurfacePolish.WriteScopedStrokeStyle(writer, id);
        if (!theme.FlatMarks) {
            SvgSurfacePolish.WriteSurfaceGradient(writer, id, "visualBackground", surfaceBackground);
            SvgSurfacePolish.WriteSurfaceGradient(writer, id, "visualCard", theme.CardBackground);
            SvgSurfacePolish.WriteSurfaceGradient(writer, id, "visualPlot", theme.PlotBackground);
        }
        writer.StartElement("clipPath").Attribute("id", id + "-visualCardClip").EndStartElement()
            .StartElement("rect").Attribute("x", 0).Attribute("y", 0).Attribute("width", options.Size.Width).Attribute("height", options.Size.Height).Attribute("rx", Math.Max(0, theme.CornerRadius)).EndEmptyElement()
            .EndElement()
            .Line();
        writer.EndElement().Line();

        if (!options.HostOwnsFrame && !options.TransparentBackground && surfaceBackground.A > 0 && !(theme.FlatMarks && options.ShowCard && theme.UseCard)) writer.StartElement("rect").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", theme.FlatMarks ? surfaceBackground.ToCss() : "url(#" + id + "-visualBackground)").EndEmptyElement().Line();
        if (options.ShowCard && theme.UseCard && !options.HostOwnsFrame) {
            writer.StartElement("rect").Attribute("data-cfx-role", "visual-card").Attribute("class", ChartVisualPrimitives.SvgGuideStrokeClass).Attribute("x", 0.5).Attribute("y", 0.5).Attribute("width", Math.Max(0, options.Size.Width - 1)).Attribute("height", Math.Max(0, options.Size.Height - 1)).Attribute("rx", Math.Max(0, theme.CornerRadius - 0.5)).Attribute("fill", theme.FlatMarks ? theme.CardBackground.ToCss() : "url(#" + id + "-visualCard)").Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            if (theme.CardBackground.A > 0 && !theme.FlatMarks) writer.StartElement("rect").Attribute("data-cfx-role", "visual-card-highlight").Attribute("class", ChartVisualPrimitives.SvgGuideStrokeClass).Attribute("x", ChartVisualPrimitives.CardInnerHighlightInset).Attribute("y", ChartVisualPrimitives.CardInnerHighlightInset).Attribute("width", Math.Max(0, options.Size.Width - ChartVisualPrimitives.CardInnerHighlightInset * 2)).Attribute("height", Math.Max(0, options.Size.Height - ChartVisualPrimitives.CardInnerHighlightInset * 2)).Attribute("rx", Math.Max(0, theme.CornerRadius - ChartVisualPrimitives.CardInnerHighlightInset)).Attribute("fill", "none").Attribute("stroke", "#fff").Attribute("stroke-opacity", ChartVisualPrimitives.CardInnerHighlightOpacity).EndEmptyElement().Line();
        }

        renderContent(writer);

        writer.EndElement().Line();
        var markup = writer.Build();
        return theme.UseGraphiteLayout ? markup.Replace("font-weight=\"850\"", "font-weight=\"700\"").Replace("font-weight=\"800\"", "font-weight=\"700\"").Replace("font-weight=\"750\"", "font-weight=\"700\"") : markup;
    }
    private static string RenderCore(IVisualBlock block, string id) => RenderSurface(block, id, writer => {
        if (block is PacketLayoutBlock packet) RenderPacketLayout(writer, packet);
        else if (block is BlockLayoutBlock blockLayout) RenderBlockLayout(writer, blockLayout);
        else if (block is GitGraphBlock gitGraph) RenderGitGraph(writer, gitGraph);
        else if (block is VennDiagramBlock venn) RenderVennDiagram(writer, venn);
        else if (block is FishboneDiagramBlock fishbone) RenderFishboneDiagram(writer, fishbone);
        else if (block is WardleyMapBlock wardleyMap) RenderWardleyMap(writer, wardleyMap);
        else throw new NotSupportedException("Unsupported static block producer: " + block.GetType().FullName);
    });

    internal static void RenderBlockHeading(SvgMarkupWriter writer, IVisualBlock block, ref double y, double contentX, double contentWidth) {
        var theme = block.Options.Theme;
        if (block.Title.Length > 0) {
            writer.StartElement("text").Attribute("data-cfx-role", "visual-title").Attribute("x", contentX).Attribute("y", y + theme.TitleFontSize * 0.75).Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", theme.TitleFontSize).Attribute("font-weight", "800").Text(VisualBlockRendering.FitText(block.Title, theme.TitleFontSize, contentWidth)).EndElement().Line();
            y += theme.TitleFontSize + 7;
        }

        if (block.Subtitle.Length > 0) {
            writer.StartElement("text").Attribute("data-cfx-role", "visual-subtitle").Attribute("x", contentX).Attribute("y", y + theme.SubtitleFontSize * 0.75).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", theme.SubtitleFontSize).Text(VisualBlockRendering.FitText(block.Subtitle, theme.SubtitleFontSize, contentWidth)).EndElement().Line();
            y += theme.SubtitleFontSize + 13;
        } else if (block.Title.Length > 0) {
            y += 8;
        }
    }

    internal static void WriteText(SvgMarkupWriter writer, string text, double x, double y, double width, TextAlignment alignment, ChartColor color, string fontFamily, double fontSize, string weight) {
        var fitted = VisualBlockRendering.FitText(text, fontSize, Math.Max(1, width));
        var anchor = "start";
        var textX = x;
        if (alignment == TextAlignment.Center) { anchor = "middle"; textX = x + width / 2; }
        else if (alignment == TextAlignment.Right) { anchor = "end"; textX = x + width; }
        writer.StartElement("text").Attribute("data-cfx-role", "visual-text").Attribute("x", textX).Attribute("y", y).Attribute("text-anchor", anchor).Attribute("fill", color.ToCss()).Attribute("font-family", fontFamily).Attribute("font-size", fontSize).Attribute("font-weight", weight).Text(fitted).EndElement().Line();
    }

    internal static string FormatPoint(double x, double y) => x.ToString("0.###", CultureInfo.InvariantCulture) + "," + y.ToString("0.###", CultureInfo.InvariantCulture);

    internal static string ArcPath(double cx, double cy, double radius, double start, double end) {
        if (Math.Abs(end - start) >= Math.PI * 2 - 0.000001) {
            var mid = start + Math.PI;
            return new SvgPathDataBuilder()
                .MoveTo(cx + Math.Cos(start) * radius, cy + Math.Sin(start) * radius)
                .ArcTo(radius, radius, 0, false, true, cx + Math.Cos(mid) * radius, cy + Math.Sin(mid) * radius)
                .ArcTo(radius, radius, 0, false, true, cx + Math.Cos(start + Math.PI * 2) * radius, cy + Math.Sin(start + Math.PI * 2) * radius)
                .Build();
        }

        return new SvgPathDataBuilder()
            .MoveTo(cx + Math.Cos(start) * radius, cy + Math.Sin(start) * radius)
            .ArcTo(radius, radius, 0, end - start > Math.PI, true, cx + Math.Cos(end) * radius, cy + Math.Sin(end) * radius)
            .Build();
    }

    internal static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    internal static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
