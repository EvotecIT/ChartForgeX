using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

/// <summary>
/// Renders visual canvases to dependency-free PNG images.
/// </summary>
public sealed class PngVisualCanvasRenderer {
    /// <summary>Renders a visual canvas to PNG bytes.</summary>
    public byte[] Render(VisualCanvas canvas) => PngWriter.WriteRgba(RenderImage(canvas));

    internal RgbaImage RenderImage(VisualCanvas canvas) {
        var source = RenderCanvas(canvas).ToImage();
        if (!canvas.IsResponsive) return source;

        var scale = canvas.PngOutputScale;
        var background = canvas.BackdropStyle == VisualCanvasBackdropStyle.Transparent ? ChartColor.Transparent : canvas.BackgroundBottom;
        return ImageComposition.Create(canvas.Width * scale, canvas.Height * scale, background)
            .DrawImage(source, 0, 0, canvas.Width * scale, canvas.Height * scale, canvas.ResponsiveFit)
            .ToImage();
    }

    internal RgbaCanvas RenderCanvas(VisualCanvas canvas) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        VisualCanvas.ValidateEnum(canvas.BackdropStyle, nameof(canvas.BackdropStyle));
        var width = canvas.DesignWidth;
        var height = canvas.DesignHeight;
        var output = new RgbaCanvas(width, height, 2, null, canvas.PngOutputScale);
        output.Clear(ChartColor.Transparent);
        if (canvas.BackdropStyle != VisualCanvasBackdropStyle.Transparent) {
            output.FillRoundedRectVerticalGradient(0, 0, width, height, 0, canvas.BackgroundTop, canvas.BackgroundBottom);
        }
        var theme = canvas.Theme ?? new VisualCanvasTheme();
        if (canvas.BackdropStyle == VisualCanvasBackdropStyle.TechHorizon) RenderTechBackdrop(output, width, height, theme);
        foreach (var layer in canvas.Layers) RenderLayer(output, layer, theme);
        return output;
    }

    private static void RenderLayer(RgbaCanvas canvas, VisualCanvasLayer layer, VisualCanvasTheme theme) {
        if (layer is VisualCanvasTextLayer text) {
            DrawText(canvas, text.X, text.Y, text.Width, text.Text, text.FontSize, text.Color, text.Alignment, text.Emphasized);
        } else if (layer is VisualCanvasHeroTitleLayer hero) {
            DrawHeroTitle(canvas, hero);
        } else if (layer is VisualCanvasKeyValueBlockLayer keyValue) {
            DrawKeyValueBlock(canvas, keyValue, theme);
        } else if (layer is VisualCanvasInfoTileLayer tile) {
            DrawInfoTile(canvas, tile, theme);
        } else if (layer is VisualCanvasHeroBadgeLayer badge) {
            DrawHeroBadge(canvas, badge, theme);
        } else if (layer is VisualCanvasImageLayer image) {
            DrawImage(canvas, image, theme);
        } else if (layer is VisualCanvasFeatureStripLayer strip) {
            DrawFeatureStrip(canvas, strip, theme);
        } else {
            throw new NotSupportedException("Unsupported visual canvas layer: " + layer.GetType().FullName);
        }
    }

    private static void RenderTechBackdrop(RgbaCanvas canvas, double width, double height, VisualCanvasTheme theme) {
        var accent = theme.SecondaryAccent;
        for (var i = 0; i < 56; i++) {
            var x = (width * ((i * 37) % 101)) / 100.0;
            var y = height * (0.05 + (((i * 19) % 67) / 100.0) * 0.44);
            var opacity = 0.14 + ((i % 5) * 0.035);
            canvas.DrawCircle(x, y, i % 11 == 0 ? 4.2 : 1.8, accent.WithOpacity(opacity));
        }

        for (var i = 0; i < 10; i++) {
            var y = height * (0.18 + i * 0.045);
            DrawCurve(canvas, width * 0.08, y, width * 0.28, y - 80, width * 0.48, y + 120, width * 0.68, y - 10, accent.WithOpacity(0.11), 1.1);
        }

        var horizon = new[] {
            new ChartPoint(0, height),
            new ChartPoint(0, height * 0.78),
            new ChartPoint(width * 0.20, height * 0.72),
            new ChartPoint(width * 0.38, height * 0.82),
            new ChartPoint(width * 0.55, height * 0.76),
            new ChartPoint(width * 0.74, height * 0.70),
            new ChartPoint(width * 0.84, height * 0.85),
            new ChartPoint(width, height * 0.73),
            new ChartPoint(width, height)
        };
        canvas.FillPolygon(horizon, theme.TechHorizonFill);
    }

    private static void DrawText(RgbaCanvas canvas, double x, double y, double width, string text, double fontSize, ChartColor color, TextAlignment alignment, bool emphasized) {
        var fitted = FitText(text, fontSize, Math.Max(4, width), emphasized);
        var textWidth = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(fitted, fontSize, null) : RgbaCanvas.MeasureTextWidth(fitted, fontSize, null);
        var drawX = AlignedX(x, width, textWidth, alignment);
        if (emphasized) canvas.DrawTextEmphasized(drawX, y, fitted, color, fontSize);
        else canvas.DrawText(drawX, y, fitted, color, fontSize);
    }

    private static void DrawHeroTitle(RgbaCanvas canvas, VisualCanvasHeroTitleLayer hero) {
        var totalWidth = 0.0;
        foreach (var run in hero.Runs) totalWidth += RgbaCanvas.MeasureTextEmphasizedWidth(run.Text, hero.FontSize, null);
        var x = AlignedX(hero.X, hero.Width, totalWidth, hero.Alignment);
        foreach (var run in hero.Runs) {
            canvas.DrawTextEmphasized(x, hero.Y, run.Text, run.Color, hero.FontSize);
            x += RgbaCanvas.MeasureTextEmphasizedWidth(run.Text, hero.FontSize, null);
        }
    }

    private static void DrawKeyValueBlock(RgbaCanvas canvas, VisualCanvasKeyValueBlockLayer block, VisualCanvasTheme theme) {
        var layout = VisualCanvasKeyValueBlockLayout.Build(block);
        var defaultLabel = block.LabelColorOverride ?? theme.TileLabelColor;
        var defaultValue = block.ValueColorOverride ?? theme.TileValueColor;
        foreach (var row in layout.Rows) {
            var labelColor = row.Item.LabelColor ?? defaultLabel;
            if (block.LabelEmphasized) canvas.DrawTextEmphasized(row.LabelX, row.Y, row.LabelText, labelColor, block.LabelFontSize);
            else canvas.DrawText(row.LabelX, row.Y, row.LabelText, labelColor, block.LabelFontSize);
            if (row.LabelOnly) continue;

            var valueColor = row.Item.ValueColor ?? defaultValue;
            for (var i = 0; i < row.ValueLines.Count; i++) {
                var lineY = row.Y + row.ValueLineHeight * i;
                if (block.ValueEmphasized) canvas.DrawTextEmphasized(row.ValueX, lineY, row.ValueLines[i], valueColor, block.ValueFontSize);
                else canvas.DrawText(row.ValueX, lineY, row.ValueLines[i], valueColor, block.ValueFontSize);
            }
        }
    }

    private static void DrawInfoTile(RgbaCanvas canvas, VisualCanvasInfoTileLayer tile, VisualCanvasTheme theme) {
        VisualCanvas.ValidateEnum(tile.SurfaceStyle, nameof(tile.SurfaceStyle));
        VisualCanvas.ValidateEnum(tile.IconKind, nameof(tile.IconKind));
        VisualCanvas.ValidateEnum(tile.MiniChartKind, nameof(tile.MiniChartKind));
        VisualCanvas.ValidateEnum(tile.TextFitPolicy, nameof(tile.TextFitPolicy));
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile);
        var x = metrics.X;
        var y = metrics.Y;
        var width = metrics.Width;
        var height = metrics.Height;
        var radius = Math.Min(16, height * 0.18);
        var isRaised = tile.SurfaceStyle == VisualCanvasInfoTileSurfaceStyle.Raised;
        var isFilled = tile.SurfaceStyle == VisualCanvasInfoTileSurfaceStyle.Glass || isRaised;
        var accent = tile.AccentOverride ?? theme.Accent;
        if (isRaised) {
            canvas.FillRoundedRect(x + 10, y + 13, width, height, radius + 2, ChartColor.Black.WithOpacity(0.42));
            canvas.FillRoundedRect(x + 4, y + 6, width, height, radius + 1, ChartColor.Black.WithOpacity(0.24));
            canvas.FillRoundedRect(x - 5, y - 5, width + 10, height + 10, radius + 5, accent.WithOpacity(0.11));
            canvas.FillRoundedRect(x - 2, y - 2, width + 4, height + 4, radius + 2, accent.WithOpacity(0.13));
        }
        if (isFilled) {
            canvas.FillRoundedRectVerticalGradient(x, y, width, height, radius, theme.TileGlassTop, theme.TileGlassBottom);
        }
        if (isRaised) {
            canvas.FillRoundedRectVerticalGradient(x + 2, y + 2, Math.Max(1, width - 4), Math.Max(1, height * 0.52), Math.Max(1, radius - 2), ChartColor.White.WithOpacity(0.20), ChartColor.White.WithOpacity(0.03));
            canvas.StrokeRoundedRect(x - 2, y - 2, width + 4, height + 4, radius + 2, accent.WithOpacity(0.42), 3.2);
            canvas.DrawLine(x + radius, y + 3, x + width - radius, y + 3, ChartColor.White.WithOpacity(0.28), 1.4);
            canvas.DrawLine(x + radius, y + height - 2, x + width - radius, y + height - 2, accent.WithOpacity(0.48), 1.6);
        }
        canvas.StrokeRoundedRect(x + 0.5, y + 0.5, Math.Max(1, width - 1), Math.Max(1, height - 1), radius, accent.WithOpacity(isRaised ? 0.92 : 0.72), isRaised ? 2.2 : 1.4);
        if (isFilled) {
            canvas.StrokeRoundedRect(x + 2.5, y + 2.5, Math.Max(1, width - 5), Math.Max(1, height - 5), Math.Max(1, radius - 2), theme.TileInnerStroke, 1);
        }
        var padX = metrics.PadX;
        var iconBox = metrics.IconBox;
        var iconX = metrics.IconX;
        var iconY = metrics.IconY;
        var iconRadius = Math.Min(13, iconBox * 0.25);
        if (isFilled) {
            canvas.FillRoundedRect(iconX, iconY, iconBox, iconBox, iconRadius, accent.WithOpacity(isRaised ? 0.25 : 0.18));
            if (isRaised) canvas.StrokeRoundedRect(iconX + 0.5, iconY + 0.5, iconBox - 1, iconBox - 1, iconRadius, accent.WithOpacity(0.32), 1);
        } else {
            canvas.StrokeRoundedRect(iconX, iconY, iconBox, iconBox, iconRadius, accent.WithOpacity(0.38), 1);
        }
        DrawTileIcon(canvas, tile.IconKind, tile.Icon, iconX, iconY, iconBox, accent);
        var textX = metrics.TextX;
        var chartW = metrics.ChartWidth;
        var chartX = metrics.ChartX;
        foreach (var line in VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height, metrics.TextX, metrics.TextMax).Lines) {
            var color = TileTextColor(line.Role, theme);
            if (line.Emphasized) canvas.DrawTextEmphasized(line.X, line.Y, line.Text, color, line.FontSize);
            else canvas.DrawText(line.X, line.Y, line.Text, color, line.FontSize);
        }
        if (tile.Progress.HasValue) {
            var railX = textX;
            var railY = y + height - 16;
            var railW = metrics.HasMiniChart ? Math.Max(24, chartX - railX - 16) : Math.Max(24, width - (railX - x) - padX);
            canvas.FillRoundedRect(railX, railY, railW, 8, 4, theme.TileProgressTrackColor);
            canvas.FillRoundedRect(railX, railY, railW * tile.Progress.Value, 8, 4, accent);
        }
        if (metrics.HasMiniChart) {
            DrawTileMiniChart(canvas, tile, theme, accent, chartX, metrics.ChartY, chartW, metrics.ChartHeight);
        }
    }

    private static ChartColor TileTextColor(VisualCanvasInfoTileTextRole role, VisualCanvasTheme theme) {
        switch (role) {
            case VisualCanvasInfoTileTextRole.Label: return theme.TileLabelColor;
            case VisualCanvasInfoTileTextRole.Detail: return theme.TileDetailColor;
            default: return theme.TileValueColor;
        }
    }

    private static void DrawTileMiniChart(RgbaCanvas canvas, VisualCanvasInfoTileLayer tile, VisualCanvasTheme theme, ChartColor accent, double x, double y, double width, double height) {
        canvas.FillRoundedRect(x, y, width, height, Math.Min(8, height * 0.24), theme.TileMiniChartTrackColor.WithOpacity(0.20));
        canvas.DrawLine(x + 4, y + height * 0.72, x + width - 4, y + height * 0.72, theme.TileMiniChartTrackColor, 1);
        canvas.DrawLine(x + 4, y + height * 0.38, x + width - 4, y + height * 0.38, theme.TileMiniChartTrackColor.WithOpacity(0.62), 1);

        var values = tile.MiniChartValues;
        if (values.Count == 0) return;

        var min = 0.0;
        var max = tile.MiniChartMaximum ?? 0.0;
        for (var i = 0; i < values.Count; i++) {
            if (values[i] < min) min = values[i];
            if (!tile.MiniChartMaximum.HasValue && values[i] > max) max = values[i];
        }
        if (max <= min) max = min + 1;

        var plotX = x + 7;
        var plotY = y + 6;
        var plotW = Math.Max(1, width - 14);
        var plotH = Math.Max(1, height - 12);
        var baseY = plotY + plotH;
        if (tile.MiniChartKind == VisualCanvasInfoTileMiniChartKind.Bars) {
            var gap = values.Count > 1 ? Math.Min(Math.Max(1, plotW * 0.035), plotW / (values.Count * 3.0)) : 0;
            var barW = Math.Max(0.5, (plotW - gap * (values.Count - 1)) / values.Count);
            for (var i = 0; i < values.Count; i++) {
                var ratio = Math.Max(0, Math.Min(1, (values[i] - min) / (max - min)));
                var barH = Math.Max(2, plotH * ratio);
                canvas.FillRoundedRect(plotX + i * (barW + gap), baseY - barH, barW, barH, Math.Min(4, barW * 0.42), accent.WithOpacity(0.82));
            }
            return;
        }

        var points = new ChartPoint[values.Count];
        for (var i = 0; i < values.Count; i++) {
            var px = values.Count == 1 ? plotX + plotW / 2 : plotX + plotW * i / (values.Count - 1);
            var ratio = Math.Max(0, Math.Min(1, (values[i] - min) / (max - min)));
            var py = plotY + plotH - plotH * ratio;
            points[i] = new ChartPoint(px, py);
        }

        if (tile.MiniChartKind == VisualCanvasInfoTileMiniChartKind.Area && points.Length > 1) {
            var polygon = new ChartPoint[points.Length + 2];
            polygon[0] = new ChartPoint(points[0].X, baseY);
            for (var i = 0; i < points.Length; i++) polygon[i + 1] = points[i];
            polygon[polygon.Length - 1] = new ChartPoint(points[points.Length - 1].X, baseY);
            canvas.FillPolygon(polygon, theme.TileMiniChartFillColor);
        }

        for (var i = 1; i < points.Length; i++) {
            canvas.DrawLine(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, accent, 2.2);
        }
    }

    private static void DrawTileIcon(RgbaCanvas canvas, VisualCanvasInfoTileIconKind kind, string text, double x, double y, double size, ChartColor color) {
        if (kind == VisualCanvasInfoTileIconKind.Text) {
            var iconFont = Math.Min(25, size * (text.Length > 3 ? 0.34 : 0.42));
            DrawText(canvas, x, y + (size - iconFont) / 2 - 1, size, text, iconFont, color, TextAlignment.Center, true);
            return;
        }

        var cx = x + size / 2;
        var cy = y + size / 2;
        var left = x + size * 0.24;
        var right = x + size * 0.76;
        var top = y + size * 0.24;
        var bottom = y + size * 0.76;
        var thick = Math.Max(1.6, size * 0.045);
        switch (kind) {
            case VisualCanvasInfoTileIconKind.Computer:
            case VisualCanvasInfoTileIconKind.OperatingSystem:
                canvas.StrokeRoundedRect(left, top, size * 0.52, size * 0.36, 3, color, thick);
                canvas.DrawLine(cx, top + size * 0.36, cx, bottom - size * 0.08, color, thick);
                canvas.DrawLine(cx - size * 0.16, bottom - size * 0.08, cx + size * 0.16, bottom - size * 0.08, color, thick);
                if (kind == VisualCanvasInfoTileIconKind.OperatingSystem) {
                    canvas.DrawLine(cx, top, cx, top + size * 0.36, color.WithOpacity(0.75), 1);
                    canvas.DrawLine(left, top + size * 0.18, right, top + size * 0.18, color.WithOpacity(0.75), 1);
                }
                break;
            case VisualCanvasInfoTileIconKind.Network:
                canvas.DrawCircleOutline(cx, cy, size * 0.27, color, thick);
                canvas.DrawLine(cx - size * 0.27, cy, cx + size * 0.27, cy, color, thick);
                canvas.DrawLine(cx, cy - size * 0.27, cx, cy + size * 0.27, color, thick);
                canvas.DrawCircleOutline(cx, cy, size * 0.12, color.WithOpacity(0.65), 1);
                break;
            case VisualCanvasInfoTileIconKind.Cpu:
                canvas.StrokeRoundedRect(x + size * 0.31, y + size * 0.31, size * 0.38, size * 0.38, 4, color, thick);
                for (var i = 0; i < 4; i++) {
                    var p = y + size * (0.24 + i * 0.17);
                    canvas.DrawLine(x + size * 0.22, p, x + size * 0.31, p, color, thick);
                    canvas.DrawLine(x + size * 0.69, p, x + size * 0.78, p, color, thick);
                    var q = x + size * (0.24 + i * 0.17);
                    canvas.DrawLine(q, y + size * 0.22, q, y + size * 0.31, color, thick);
                    canvas.DrawLine(q, y + size * 0.69, q, y + size * 0.78, color, thick);
                }
                break;
            case VisualCanvasInfoTileIconKind.Memory:
                canvas.StrokeRoundedRect(left, y + size * 0.35, size * 0.52, size * 0.30, 3, color, thick);
                for (var i = 0; i < 4; i++) canvas.DrawLine(x + size * (0.31 + i * 0.10), y + size * 0.43, x + size * (0.31 + i * 0.10), y + size * 0.57, color.WithOpacity(0.75), 1.2);
                for (var i = 0; i < 5; i++) canvas.DrawLine(x + size * (0.28 + i * 0.10), y + size * 0.65, x + size * (0.28 + i * 0.10), y + size * 0.72, color, thick);
                break;
            case VisualCanvasInfoTileIconKind.User:
                canvas.DrawCircleOutline(cx, y + size * 0.38, size * 0.11, color, thick);
                canvas.DrawLine(cx - size * 0.22, bottom, cx - size * 0.12, y + size * 0.58, color, thick);
                canvas.DrawLine(cx + size * 0.22, bottom, cx + size * 0.12, y + size * 0.58, color, thick);
                canvas.DrawLine(cx - size * 0.12, y + size * 0.58, cx + size * 0.12, y + size * 0.58, color, thick);
                break;
            case VisualCanvasInfoTileIconKind.Domain:
                canvas.StrokeRoundedRect(x + size * 0.32, top, size * 0.28, size * 0.52, 2, color, thick);
                canvas.DrawLine(x + size * 0.22, bottom, x + size * 0.78, bottom, color, thick);
                canvas.DrawLine(x + size * 0.60, y + size * 0.44, x + size * 0.74, y + size * 0.44, color, thick);
                canvas.DrawLine(x + size * 0.74, y + size * 0.44, x + size * 0.74, bottom, color, thick);
                for (var i = 0; i < 3; i++) canvas.DrawLine(x + size * 0.39, y + size * (0.34 + i * 0.13), x + size * 0.51, y + size * (0.34 + i * 0.13), color.WithOpacity(0.75), 1.2);
                break;
            case VisualCanvasInfoTileIconKind.Terminal:
                canvas.DrawLine(x + size * 0.30, y + size * 0.36, x + size * 0.44, cy, color, thick);
                canvas.DrawLine(x + size * 0.44, cy, x + size * 0.30, y + size * 0.64, color, thick);
                canvas.DrawLine(x + size * 0.52, y + size * 0.64, x + size * 0.72, y + size * 0.64, color, thick);
                break;
            case VisualCanvasInfoTileIconKind.Storage:
                canvas.DrawCircleOutline(cx, y + size * 0.34, size * 0.22, color, thick);
                canvas.DrawLine(cx - size * 0.22, y + size * 0.34, cx - size * 0.22, y + size * 0.66, color, thick);
                canvas.DrawLine(cx + size * 0.22, y + size * 0.34, cx + size * 0.22, y + size * 0.66, color, thick);
                canvas.DrawCircleOutline(cx, y + size * 0.66, size * 0.22, color, thick);
                break;
            case VisualCanvasInfoTileIconKind.Shield:
                canvas.FillPolygon(new[] {
                    new ChartPoint(cx, top),
                    new ChartPoint(right, y + size * 0.36),
                    new ChartPoint(x + size * 0.68, bottom),
                    new ChartPoint(cx, y + size * 0.82),
                    new ChartPoint(x + size * 0.32, bottom),
                    new ChartPoint(left, y + size * 0.36)
                }, color.WithOpacity(0.24));
                canvas.DrawLine(cx, top, right, y + size * 0.36, color, thick);
                canvas.DrawLine(right, y + size * 0.36, x + size * 0.68, bottom, color, thick);
                canvas.DrawLine(x + size * 0.68, bottom, cx, y + size * 0.82, color, thick);
                canvas.DrawLine(cx, y + size * 0.82, x + size * 0.32, bottom, color, thick);
                canvas.DrawLine(x + size * 0.32, bottom, left, y + size * 0.36, color, thick);
                canvas.DrawLine(left, y + size * 0.36, cx, top, color, thick);
                break;
            default:
                DrawTileIcon(canvas, VisualCanvasInfoTileIconKind.Text, text, x, y, size, color);
                break;
        }
    }

    private static void DrawHeroBadge(RgbaCanvas canvas, VisualCanvasHeroBadgeLayer badge, VisualCanvasTheme theme) {
        VisualCanvas.ValidateEnum(badge.ImageFit, nameof(badge.ImageFit));
        var radius = Math.Min(22, badge.Height * 0.20);
        canvas.FillRoundedRect(badge.X - 6, badge.Y - 6, badge.Width + 12, badge.Height + 12, radius + 4, theme.HeroBadgeGlowColor);
        canvas.FillRoundedRectVerticalGradient(badge.X, badge.Y, badge.Width, badge.Height, radius, theme.HeroBadgeTop, theme.HeroBadgeBottom);
        var accent = badge.AccentOverride ?? theme.SecondaryAccent;
        canvas.StrokeRoundedRect(badge.X, badge.Y, badge.Width, badge.Height, radius, accent, 2.4);
        if (badge.ImageRgba != null && badge.ImageSourceWidth > 0 && badge.ImageSourceHeight > 0) {
            var padding = Math.Max(0, Math.Min(Math.Min(badge.ImagePadding, badge.Width / 2 - 1), badge.Height / 2 - 1));
            var image = new VisualCanvasImageLayer(badge.X + padding, badge.Y + padding, Math.Max(1, badge.Width - padding * 2), Math.Max(1, badge.Height - padding * 2)) {
                Rgba = badge.ImageRgba,
                SourceWidth = badge.ImageSourceWidth,
                SourceHeight = badge.ImageSourceHeight,
                Fit = badge.ImageFit,
                Opacity = badge.ImageOpacity
            };
            var rgba = image.Opacity >= 0.999 ? image.Rgba : ApplyOpacity(image.Rgba, image.Opacity);
            DrawFittedImage(canvas, image, rgba);
            return;
        }

        var fontSize = Math.Max(24, badge.Height * 0.42);
        DrawText(canvas, badge.X, badge.Y + badge.Height / 2 - fontSize * 0.40, badge.Width, badge.Symbol, fontSize, theme.HeroBadgeTextColor, TextAlignment.Center, true);
    }

    private static void DrawImage(RgbaCanvas canvas, VisualCanvasImageLayer image, VisualCanvasTheme theme) {
        VisualCanvas.ValidateEnum(image.Fit, nameof(image.Fit));
        if (image.Rgba != null && (image.SourceWidth <= 0 || image.SourceHeight <= 0)) {
            throw new ArgumentOutOfRangeException(nameof(image.SourceWidth), "RGBA image layers require positive source dimensions.");
        }
        if (image.Rgba != null && image.SourceWidth > 0 && image.SourceHeight > 0) {
            var rgba = image.Opacity >= 0.999 ? image.Rgba : ApplyOpacity(image.Rgba, image.Opacity);
            DrawFittedImage(canvas, image, rgba);
        } else {
            canvas.FillRoundedRect(image.X, image.Y, image.Width, image.Height, 12, theme.ImagePlaceholderFill);
            canvas.StrokeRoundedRect(image.X, image.Y, image.Width, image.Height, 12, theme.ImagePlaceholderStroke, 1);
        }
    }

    private static void DrawFittedImage(RgbaCanvas canvas, VisualCanvasImageLayer image, byte[] rgba) {
        var destinationWidth = Math.Max(1, (int)Math.Round(image.Width));
        var destinationHeight = Math.Max(1, (int)Math.Round(image.Height));
        var destinationX = (int)Math.Round(image.X);
        var destinationY = (int)Math.Round(image.Y);
        var sourceWidth = image.SourceWidth;
        var sourceHeight = image.SourceHeight;

        switch (image.Fit) {
            case VisualCanvasImageFit.Contain:
                {
                    var scale = Math.Min(destinationWidth / (double)sourceWidth, destinationHeight / (double)sourceHeight);
                    var width = Math.Max(1, (int)Math.Round(sourceWidth * scale));
                    var height = Math.Max(1, (int)Math.Round(sourceHeight * scale));
                    canvas.DrawImageScaled(destinationX + (destinationWidth - width) / 2, destinationY + (destinationHeight - height) / 2, width, height, sourceWidth, sourceHeight, rgba);
                    return;
                }
            case VisualCanvasImageFit.Cover:
                {
                    var scale = Math.Max(destinationWidth / (double)sourceWidth, destinationHeight / (double)sourceHeight);
                    var cropWidth = Math.Min(sourceWidth, destinationWidth / scale);
                    var cropHeight = Math.Min(sourceHeight, destinationHeight / scale);
                    canvas.DrawImageScaled(destinationX, destinationY, destinationWidth, destinationHeight, sourceWidth, sourceHeight, rgba, (sourceWidth - cropWidth) / 2, (sourceHeight - cropHeight) / 2, cropWidth, cropHeight);
                    return;
                }
            case VisualCanvasImageFit.Center:
                DrawCenteredImage(canvas, destinationX, destinationY, destinationWidth, destinationHeight, sourceWidth, sourceHeight, rgba);
                return;
            case VisualCanvasImageFit.Tile:
                DrawTiledImage(canvas, destinationX, destinationY, destinationWidth, destinationHeight, sourceWidth, sourceHeight, rgba);
                return;
            default:
                canvas.DrawImageScaled(destinationX, destinationY, destinationWidth, destinationHeight, sourceWidth, sourceHeight, rgba);
                return;
        }
    }

    private static void DrawCenteredImage(RgbaCanvas canvas, int destinationX, int destinationY, int destinationWidth, int destinationHeight, int sourceWidth, int sourceHeight, byte[] rgba) {
        var centeredX = destinationX + (destinationWidth - sourceWidth) / 2;
        var centeredY = destinationY + (destinationHeight - sourceHeight) / 2;
        var drawX = Math.Max(destinationX, centeredX);
        var drawY = Math.Max(destinationY, centeredY);
        var drawRight = Math.Min(destinationX + destinationWidth, centeredX + sourceWidth);
        var drawBottom = Math.Min(destinationY + destinationHeight, centeredY + sourceHeight);
        var drawWidth = drawRight - drawX;
        var drawHeight = drawBottom - drawY;
        if (drawWidth <= 0 || drawHeight <= 0) return;
        canvas.DrawImageScaled(drawX, drawY, drawWidth, drawHeight, sourceWidth, sourceHeight, rgba, drawX - centeredX, drawY - centeredY, drawWidth, drawHeight);
    }

    private static void DrawTiledImage(RgbaCanvas canvas, int destinationX, int destinationY, int destinationWidth, int destinationHeight, int sourceWidth, int sourceHeight, byte[] rgba) {
        for (var y = destinationY; y < destinationY + destinationHeight; y += sourceHeight) {
            var drawHeight = Math.Min(sourceHeight, destinationY + destinationHeight - y);
            for (var x = destinationX; x < destinationX + destinationWidth; x += sourceWidth) {
                var drawWidth = Math.Min(sourceWidth, destinationX + destinationWidth - x);
                canvas.DrawImageScaled(x, y, drawWidth, drawHeight, sourceWidth, sourceHeight, rgba, 0, 0, drawWidth, drawHeight);
            }
        }
    }

    private static void DrawFeatureStrip(RgbaCanvas canvas, VisualCanvasFeatureStripLayer strip, VisualCanvasTheme theme) {
        var slot = strip.Width / strip.Items.Count;
        for (var i = 0; i < strip.Items.Count; i++) {
            var item = strip.Items[i];
            var slotX = strip.X + slot * i;
            if (i > 0) canvas.DrawLine(slotX, strip.Y + 4, slotX, strip.Y + strip.Height - 4, theme.FeatureDividerColor, 1);
            DrawText(canvas, slotX, strip.Y + 2, slot, item.Icon, 22, strip.Accent, TextAlignment.Center, true);
            DrawText(canvas, slotX + 6, strip.Y + 38, slot - 12, item.Label, 15, theme.FeatureLabelColor, TextAlignment.Center, true);
        }
    }

    private static void DrawCurve(RgbaCanvas canvas, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3, ChartColor color, double thickness) {
        var previousX = x0;
        var previousY = y0;
        const int steps = 40;
        for (var i = 1; i <= steps; i++) {
            var t = i / (double)steps;
            var inv = 1 - t;
            var x = inv * inv * inv * x0 + 3 * inv * inv * t * x1 + 3 * inv * t * t * x2 + t * t * t * x3;
            var y = inv * inv * inv * y0 + 3 * inv * inv * t * y1 + 3 * inv * t * t * y2 + t * t * t * y3;
            canvas.DrawLine(previousX, previousY, x, y, color, thickness);
            previousX = x;
            previousY = y;
        }
    }

    private static string FitText(string value, double fontSize, double maxWidth, bool emphasized) {
        if (string.IsNullOrEmpty(value) || Measure(value, fontSize, emphasized) <= maxWidth) return value;
        const string suffix = "...";
        if (Measure(suffix, fontSize, emphasized) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (Measure(value.Substring(0, mid) + suffix, fontSize, emphasized) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }

    private static double Measure(string value, double fontSize, bool emphasized) =>
        emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(value, fontSize, null) : RgbaCanvas.MeasureTextWidth(value, fontSize, null);

    private static double AlignedX(double x, double width, double textWidth, TextAlignment alignment) {
        VisualCanvas.ValidateEnum(alignment, nameof(alignment));
        switch (alignment) {
            case TextAlignment.Center: return x + (width - textWidth) / 2;
            case TextAlignment.Right: return x + width - textWidth;
            default: return x;
        }
    }

    private static byte[] ApplyOpacity(byte[] rgba, double opacity) {
        var copy = new byte[rgba.Length];
        Buffer.BlockCopy(rgba, 0, copy, 0, rgba.Length);
        for (var i = 3; i < copy.Length; i += 4) copy[i] = (byte)Math.Round(copy[i] * opacity);
        return copy;
    }
}
