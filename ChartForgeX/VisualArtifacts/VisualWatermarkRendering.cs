using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualArtifacts;

internal static class VisualWatermarkRendering {
    private const double MaximumRepeatedWatermarkCount = 10000;
    private static readonly Regex SvgRootRegex = new("<svg\\b(?<attributes>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex AttributeRegex = new("(?<name>[A-Za-z_:][-A-Za-z0-9_:.]*)\\s*=\\s*[\"'](?<value>.*?)[\"']", RegexOptions.CultureInvariant);

    public static string ApplyToSvg(string svg, VisualArtifact artifact, IReadOnlyList<VisualWatermark> watermarks) {
        if (watermarks.Count == 0) return svg;
        var size = ResolveSvgSize(svg, artifact);
        var layer = new StringBuilder();
        layer.Append("<g data-cfx-role=\"watermarks\" pointer-events=\"none\">");
        var imageDefinitions = AppendRepeatedSvgImageDefinitions(layer, svg, watermarks);
        for (var i = 0; i < watermarks.Count; i++) {
            imageDefinitions.TryGetValue(i, out var imageDefinition);
            AppendSvgWatermark(layer, watermarks[i], size.Width, size.Height, i, imageDefinition);
        }
        layer.Append("</g>");
        var closing = svg.LastIndexOf("</svg>", StringComparison.OrdinalIgnoreCase);
        if (closing < 0) throw new InvalidOperationException("Rendered artifact did not produce a complete SVG document.");
        return svg.Insert(closing, layer.ToString());
    }

    public static RgbaImage ApplyToImage(RgbaImage source, IReadOnlyList<VisualWatermark> watermarks) {
        if (watermarks.Count == 0) return source;
        var canvas = new RgbaCanvas(source.Width, source.Height, 1, null, 1);
        canvas.DrawImage(0, 0, source.Width, source.Height, source.Pixels);
        for (var i = 0; i < watermarks.Count; i++) DrawRasterWatermark(canvas, watermarks[i]);
        return canvas.ToImage();
    }

    private static Dictionary<int, SvgImageWatermarkDefinition> AppendRepeatedSvgImageDefinitions(StringBuilder output, string svg, IReadOnlyList<VisualWatermark> watermarks) {
        var definitions = new Dictionary<int, SvgImageWatermarkDefinition>();
        for (var index = 0; index < watermarks.Count; index++) {
            var watermark = watermarks[index];
            if (!watermark.Repeat || watermark.Kind != VisualWatermarkKind.Image) continue;
            var image = RasterImageDecoder.Decode(watermark.ImageBytes!);
            definitions[index] = new SvgImageWatermarkDefinition(UniqueSvgImageDefinitionId(svg, index), image);
        }
        if (definitions.Count == 0) return definitions;

        output.Append("<defs>");
        foreach (var pair in definitions) {
            var watermark = watermarks[pair.Key];
            var definition = pair.Value;
            var image = definition.Image.GetValueOrDefault();
            output.Append("<symbol id=\"").Append(definition.Id).Append("\" viewBox=\"0 0 ")
                .Append(image.Width).Append(' ').Append(image.Height)
                .Append("\" preserveAspectRatio=\"xMidYMid meet\"><image width=\"").Append(image.Width)
                .Append("\" height=\"").Append(image.Height)
                .Append("\" preserveAspectRatio=\"xMidYMid meet\" href=\"data:").Append(watermark.ImageMimeType)
                .Append(";base64,").Append(Convert.ToBase64String(watermark.ImageBytes!)).Append("\" /></symbol>");
        }
        output.Append("</defs>");
        return definitions;
    }

    private static string UniqueSvgImageDefinitionId(string svg, int index) {
        var suffix = 0;
        while (true) {
            var id = "cfx-watermark-image-" + index.ToString(CultureInfo.InvariantCulture) + (suffix == 0 ? string.Empty : "-" + suffix.ToString(CultureInfo.InvariantCulture));
            if (!Regex.IsMatch(svg, "\\bid\\s*=\\s*[\\\"']" + Regex.Escape(id) + "[\\\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return id;
            suffix++;
        }
    }

    private static void AppendSvgWatermark(StringBuilder output, VisualWatermark watermark, double width, double height, int index, SvgImageWatermarkDefinition imageDefinition) {
        if (watermark.Repeat) {
            ValidateRepeatDensity(watermark, width, height);
            var row = 0;
            for (var y = watermark.RepeatSpacingY / 2; y < height; y += watermark.RepeatSpacingY) {
                var stagger = row++ % 2 == 0 ? 0 : watermark.RepeatSpacingX / 2;
                for (var x = watermark.RepeatSpacingX / 2 - stagger; x < width; x += watermark.RepeatSpacingX) {
                    AppendSvgWatermarkAt(output, watermark, x, y, index, imageDefinition: imageDefinition);
                }
            }
            return;
        }

        var bounds = ResolveBounds(watermark, width, height);
        AppendSvgWatermarkAt(output, watermark, bounds.CenterX, bounds.CenterY, index, bounds.Width, bounds.Height, imageDefinition);
    }

    private static void AppendSvgWatermarkAt(StringBuilder output, VisualWatermark watermark, double centerX, double centerY, int index, double? explicitWidth = null, double? explicitHeight = null, SvgImageWatermarkDefinition imageDefinition = default) {
        var opacity = watermark.Opacity * watermark.Color.A / 255.0;
        if (watermark.Kind == VisualWatermarkKind.Text) {
            output.Append("<text data-cfx-role=\"watermark\" data-cfx-watermark-index=\"").Append(index).Append("\"")
                .Append(" x=\"").Append(F(centerX)).Append("\" y=\"").Append(F(centerY)).Append("\"")
                .Append(" text-anchor=\"middle\" dominant-baseline=\"middle\"")
                .Append(" fill=\"").Append(watermark.Color.ToHex()).Append("\"")
                .Append(" fill-opacity=\"").Append(F(opacity)).Append("\"")
                .Append(" font-family=\"Segoe UI,Arial,sans-serif\"")
                .Append(" font-size=\"").Append(F(watermark.FontSize * watermark.Scale)).Append("\"")
                .Append(" font-weight=\"").Append(watermark.Emphasized ? "700" : "400").Append("\"");
            if (Math.Abs(watermark.RotationDegrees) > 0.0001) {
                output.Append(" transform=\"rotate(").Append(F(watermark.RotationDegrees)).Append(' ').Append(F(centerX)).Append(' ').Append(F(centerY)).Append(")\"");
            }
            output.Append('>').Append(EscapeXml(watermark.Text!)).Append("</text>");
            return;
        }

        var bytes = watermark.ImageBytes!;
        var image = imageDefinition.Image ?? RasterImageDecoder.Decode(bytes);
        var imageWidth = explicitWidth ?? (watermark.Width ?? image.Width) * watermark.Scale;
        var imageHeight = explicitHeight ?? (watermark.Height ?? image.Height) * watermark.Scale;
        output.Append(imageDefinition.Image == null ? "<image" : "<use")
            .Append(" data-cfx-role=\"watermark\" data-cfx-watermark-index=\"").Append(index).Append("\"")
            .Append(" x=\"").Append(F(centerX - imageWidth / 2)).Append("\" y=\"").Append(F(centerY - imageHeight / 2)).Append("\"")
            .Append(" width=\"").Append(F(imageWidth)).Append("\" height=\"").Append(F(imageHeight)).Append("\"")
            .Append(" opacity=\"").Append(F(watermark.Opacity)).Append("\"")
            .Append(" preserveAspectRatio=\"xMidYMid meet\"");
        if (imageDefinition.Image == null) output.Append(" href=\"data:").Append(watermark.ImageMimeType).Append(";base64,").Append(Convert.ToBase64String(bytes)).Append("\"");
        else output.Append(" href=\"#").Append(imageDefinition.Id).Append("\"");
        if (Math.Abs(watermark.RotationDegrees) > 0.0001) {
            output.Append(" transform=\"rotate(").Append(F(watermark.RotationDegrees)).Append(' ').Append(F(centerX)).Append(' ').Append(F(centerY)).Append(")\"");
        }
        output.Append(" />");
    }

    private static void DrawRasterWatermark(RgbaCanvas canvas, VisualWatermark watermark) {
        RgbaImage? sourceImage = watermark.Kind == VisualWatermarkKind.Image
            ? RasterImageDecoder.Decode(watermark.ImageBytes!)
            : null;
        if (watermark.Repeat) {
            ValidateRepeatDensity(watermark, canvas.Width, canvas.Height);
            RgbaImage? preparedImage = sourceImage.HasValue
                ? PrepareRasterWatermark(watermark, sourceImage.Value)
                : null;
            var row = 0;
            for (var y = watermark.RepeatSpacingY / 2; y < canvas.Height; y += watermark.RepeatSpacingY) {
                var stagger = row++ % 2 == 0 ? 0 : watermark.RepeatSpacingX / 2;
                for (var x = watermark.RepeatSpacingX / 2 - stagger; x < canvas.Width; x += watermark.RepeatSpacingX) DrawRasterWatermarkAt(canvas, watermark, x, y, sourceImage: sourceImage, preparedImage: preparedImage);
            }
            return;
        }

        var bounds = ResolveBounds(watermark, canvas.Width, canvas.Height, sourceImage);
        DrawRasterWatermarkAt(canvas, watermark, bounds.CenterX, bounds.CenterY, bounds.Width, bounds.Height, sourceImage);
    }

    private static void ValidateRepeatDensity(VisualWatermark watermark, double width, double height) {
        var columns = Math.Ceiling(width / watermark.RepeatSpacingX) + 1;
        var rows = Math.Ceiling(height / watermark.RepeatSpacingY) + 1;
        if (columns * rows > MaximumRepeatedWatermarkCount) {
            throw new InvalidOperationException("Repeated watermark spacing would create more than 10,000 marks. Increase RepeatSpacingX or RepeatSpacingY.");
        }
    }

    private static void DrawRasterWatermarkAt(RgbaCanvas canvas, VisualWatermark watermark, double centerX, double centerY, double? explicitWidth = null, double? explicitHeight = null, RgbaImage? sourceImage = null, RgbaImage? preparedImage = null) {
        if (watermark.Kind == VisualWatermarkKind.Text) {
            var fontSize = watermark.FontSize * watermark.Scale;
            var color = watermark.Color.WithOpacity(watermark.Opacity * watermark.Color.A / 255.0);
            var textWidth = watermark.Emphasized
                ? RgbaCanvas.MeasureTextEmphasizedWidth(watermark.Text!, fontSize, null)
                : RgbaCanvas.MeasureTextWidth(watermark.Text!, fontSize, null);
            var textHeight = RgbaCanvas.MeasureTextHeight(fontSize, null);
            if (watermark.Emphasized) canvas.DrawTextRotatedEmphasized(centerX, centerY, watermark.Text!, color, fontSize, watermark.RotationDegrees, textWidth / 2, textHeight / 2);
            else canvas.DrawTextRotated(centerX, centerY, watermark.Text!, color, fontSize, watermark.RotationDegrees, textWidth / 2, textHeight / 2);
            return;
        }

        var source = sourceImage ?? RasterImageDecoder.Decode(watermark.ImageBytes!);
        var prepared = preparedImage ?? PrepareRasterWatermark(watermark, source, explicitWidth, explicitHeight);
        canvas.DrawImage((int)Math.Round(centerX - prepared.Width / 2.0), (int)Math.Round(centerY - prepared.Height / 2.0), prepared.Width, prepared.Height, prepared.Pixels);
    }

    private static RgbaImage PrepareRasterWatermark(VisualWatermark watermark, RgbaImage source, double? explicitWidth = null, double? explicitHeight = null) {
        var targetWidth = Math.Max(1, (int)Math.Round(explicitWidth ?? (watermark.Width ?? source.Width) * watermark.Scale));
        var targetHeight = Math.Max(1, (int)Math.Round(explicitHeight ?? (watermark.Height ?? source.Height) * watermark.Scale));
        return ScaleAndRotate(source, targetWidth, targetHeight, watermark.RotationDegrees, watermark.Opacity);
    }

    private static RgbaImage ScaleAndRotate(RgbaImage source, int width, int height, double degrees, double opacity) {
        var scaledCanvas = new RgbaCanvas(width, height, 1, null, 1);
        scaledCanvas.DrawImageScaled(0, 0, width, height, source.Width, source.Height, source.Pixels);
        var scaled = scaledCanvas.ToImage();
        if (Math.Abs(degrees % 360) < 0.0001) return WithOpacity(scaled, opacity);

        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var rotatedWidth = Math.Max(1, (int)Math.Ceiling(Math.Abs(width * cos) + Math.Abs(height * sin)));
        var rotatedHeight = Math.Max(1, (int)Math.Ceiling(Math.Abs(width * sin) + Math.Abs(height * cos)));
        var pixels = new byte[rotatedWidth * rotatedHeight * 4];
        var sourceCenterX = (width - 1) / 2.0;
        var sourceCenterY = (height - 1) / 2.0;
        var targetCenterX = (rotatedWidth - 1) / 2.0;
        var targetCenterY = (rotatedHeight - 1) / 2.0;
        for (var y = 0; y < rotatedHeight; y++) for (var x = 0; x < rotatedWidth; x++) {
            var dx = x - targetCenterX;
            var dy = y - targetCenterY;
            var sourceX = (int)Math.Round(sourceCenterX + dx * cos + dy * sin);
            var sourceY = (int)Math.Round(sourceCenterY - dx * sin + dy * cos);
            if (sourceX < 0 || sourceY < 0 || sourceX >= width || sourceY >= height) continue;
            var sourceIndex = (sourceY * width + sourceX) * 4;
            var targetIndex = (y * rotatedWidth + x) * 4;
            pixels[targetIndex] = scaled.Pixels[sourceIndex];
            pixels[targetIndex + 1] = scaled.Pixels[sourceIndex + 1];
            pixels[targetIndex + 2] = scaled.Pixels[sourceIndex + 2];
            pixels[targetIndex + 3] = (byte)Math.Round(scaled.Pixels[sourceIndex + 3] * opacity);
        }
        return new RgbaImage(rotatedWidth, rotatedHeight, pixels);
    }

    private static RgbaImage WithOpacity(RgbaImage image, double opacity) {
        if (opacity >= 0.9999) return image;
        var pixels = (byte[])image.Pixels.Clone();
        for (var i = 3; i < pixels.Length; i += 4) pixels[i] = (byte)Math.Round(pixels[i] * opacity);
        return new RgbaImage(image.Width, image.Height, pixels);
    }

    private static WatermarkBounds ResolveBounds(VisualWatermark watermark, double canvasWidth, double canvasHeight, RgbaImage? sourceImage = null) {
        double width;
        double height;
        if (watermark.Kind == VisualWatermarkKind.Text) {
            var fontSize = watermark.FontSize * watermark.Scale;
            width = Math.Max(fontSize, watermark.Text!.Length * fontSize * (watermark.Emphasized ? 0.62 : 0.56));
            height = fontSize * 1.2;
        } else {
            var image = sourceImage ?? RasterImageDecoder.Decode(watermark.ImageBytes!);
            width = (watermark.Width ?? image.Width) * watermark.Scale;
            height = (watermark.Height ?? image.Height) * watermark.Scale;
        }

        var placement = VisualCanvasPlacement.At(
            watermark.Anchor,
            EdgeInsetX(watermark.Anchor, watermark.Padding) + watermark.OffsetX,
            EdgeInsetY(watermark.Anchor, watermark.Padding) + watermark.OffsetY);
        var rect = placement.Resolve(canvasWidth, canvasHeight, width, height);
        return new WatermarkBounds(rect.X + width / 2, rect.Y + height / 2, width, height);
    }

    private static double EdgeInsetX(VisualCanvasAnchor anchor, double padding) {
        return anchor == VisualCanvasAnchor.TopCenter || anchor == VisualCanvasAnchor.Center || anchor == VisualCanvasAnchor.BottomCenter ? 0 : padding;
    }

    private static double EdgeInsetY(VisualCanvasAnchor anchor, double padding) {
        return anchor == VisualCanvasAnchor.MiddleLeft || anchor == VisualCanvasAnchor.Center || anchor == VisualCanvasAnchor.MiddleRight ? 0 : padding;
    }

    private static VisualArtifactSize ResolveSvgSize(string svg, VisualArtifact artifact) {
        if (artifact.PreserveNaturalSize && artifact.NaturalSize.HasValue) return artifact.NaturalSize.Value;
        var root = SvgRootRegex.Match(svg);
        if (!root.Success) throw new InvalidOperationException("Rendered artifact did not produce an SVG root element.");
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AttributeRegex.Matches(root.Groups["attributes"].Value)) attributes[match.Groups["name"].Value] = match.Groups["value"].Value;
        if (attributes.TryGetValue("viewBox", out var viewBox)) {
            var parts = viewBox.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 && TryNumber(parts[2], out var viewWidth) && TryNumber(parts[3], out var viewHeight) && viewWidth > 0 && viewHeight > 0) return new VisualArtifactSize(viewWidth, viewHeight);
        }
        if (attributes.TryGetValue("width", out var width) && attributes.TryGetValue("height", out var height) && TryNumber(TrimPixelSuffix(width), out var parsedWidth) && TryNumber(TrimPixelSuffix(height), out var parsedHeight)) return new VisualArtifactSize(parsedWidth, parsedHeight);
        if (artifact.NaturalSize.HasValue) return artifact.NaturalSize.Value;
        throw new InvalidOperationException("Rendered artifact SVG does not expose a usable viewBox or numeric width and height.");
    }

    private static string TrimPixelSuffix(string value) => value.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? value.Substring(0, value.Length - 2) : value;
    private static bool TryNumber(string value, out double result) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string EscapeXml(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");

    private readonly struct SvgImageWatermarkDefinition {
        public SvgImageWatermarkDefinition(string id, RgbaImage image) { Id = id; Image = image; }
        public string? Id { get; }
        public RgbaImage? Image { get; }
    }

    private readonly struct WatermarkBounds {
        public WatermarkBounds(double centerX, double centerY, double width, double height) {
            CenterX = centerX;
            CenterY = centerY;
            Width = width;
            Height = height;
        }
        public double CenterX { get; }
        public double CenterY { get; }
        public double Width { get; }
        public double Height { get; }
    }
}
