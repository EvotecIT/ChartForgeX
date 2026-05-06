using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Svg;
using ChartForgeX.Themes;

internal static class WellnessDashboardExamples {
    private static readonly ChartColor Background = ChartColor.FromHex("#F5F6F8");
    private static readonly ChartColor Card = ChartColor.FromHex("#FFFFFF");
    private static readonly ChartColor Text = ChartColor.FromHex("#252936");
    private static readonly ChartColor Muted = ChartColor.FromHex("#8B8E96");
    private static readonly ChartColor Track = ChartColor.FromHex("#F1F2F6");
    private static readonly ChartColor Orange = ChartColor.FromHex("#FF9F4A");
    private static readonly ChartColor Yellow = ChartColor.FromHex("#FFCD62");
    private static readonly ChartColor Green = ChartColor.FromHex("#BDE765");

    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        SaveLayeredRadial(output, pngOutputScale);
        SaveWeightCard(output, (int)pngOutputScale);
        SaveCaloriesCard(output, (int)pngOutputScale);
    }

    private static void SaveLayeredRadial(string output, ChartPngOutputScale pngOutputScale) {
        var chart = Chart.Create()
            .WithTitle("Layered Radial Progress")
            .WithSubtitle("Generic radial layers with independent radius, stroke, color, and value scale.")
            .WithSize(560, 560)
            .WithPadding(48, 48, 48, 48)
            .WithTheme(WellnessTheme())
            .WithPlotBackground(false)
            .WithLegend(false)
            .WithPngOutputScale(pngOutputScale)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " kcal")
            .AddLayeredRadial("Calories left", CaloriesRadialLayers());

        chart.SaveSvg(Path.Combine(output, "wellness-layered-radial-progress.svg"));
        chart.SaveHtml(Path.Combine(output, "wellness-layered-radial-progress.html"));
        chart.SavePng(Path.Combine(output, "wellness-layered-radial-progress.png"));
    }

    private static void SaveWeightCard(string output, int outputScale) {
        const int width = 896;
        const int height = 768;
        var svg = BuildWeightSvg(width, height);
        SaveCustom(output, "wellness-weight-data-gauge", width, height, outputScale, svg, DrawWeightPng);
    }

    private static void SaveCaloriesCard(string output, int outputScale) {
        const int width = 1280;
        const int height = 560;
        var svg = BuildCaloriesSvg(width, height);
        SaveCustom(output, "wellness-calories-intake-dashboard", width, height, outputScale, svg, DrawCaloriesPng);
    }

    private static string BuildWeightSvg(int width, int height) {
        var w = new SvgMarkupWriter(8192);
        StartSvg(w, width, height, "Weight Data");
        w.StartElement("rect").Attribute("width", width).Attribute("height", height).Attribute("fill", Background.ToCss()).EndEmptyElement().Line();
        w.StartElement("rect").Attribute("x", 134).Attribute("y", 26).Attribute("width", 628).Attribute("height", 716).Attribute("rx", 44).Attribute("fill", Card.ToCss()).EndEmptyElement().Line();
        TextNode(w, "Weight Data", 172, 106, 36, "850", Text);
        MenuDots(w, 672, 100, Text);

        WriteLayeredRadialLayers(w, WeightRadialLayers(), 448, 410, 234);
        TextNode(w, "78", 382, 354, 66, "850", Text);
        TextNode(w, "kg", 470, 356, 50, "450", Text);
        TextNode(w, "Current Weight", 358, 424, 28, "500", Muted);
        TextNode(w, "85", 214, 484, 34, "500", Muted);
        TextNode(w, "13 kg left", 393, 482, 28, "500", Muted);
        TextNode(w, "65", 642, 484, 34, "500", Muted);
        w.StartElement("line").Attribute("x1", 170).Attribute("y1", 574).Attribute("x2", 727).Attribute("y2", 574).Attribute("stroke", Green.ToCss()).Attribute("stroke-width", 5).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
        CenterText(w, "Progress is progress, no matter how slow.", 448, 646, 28, "500", ChartColor.FromHex("#555963"));
        CenterText(w, "Keep going, you're getting closer to your goal", 448, 690, 28, "500", ChartColor.FromHex("#555963"));
        CenterText(w, "every day!", 448, 734, 28, "500", ChartColor.FromHex("#555963"));
        return FinishSvg(w);
    }

    private static string BuildCaloriesSvg(int width, int height) {
        var w = new SvgMarkupWriter(12288);
        StartSvg(w, width, height, "Calories Intake");
        w.StartElement("rect").Attribute("width", width).Attribute("height", height).Attribute("fill", Background.ToCss()).EndEmptyElement().Line();
        w.StartElement("rect").Attribute("x", 64).Attribute("y", 16).Attribute("width", 1152).Attribute("height", 528).Attribute("rx", 34).Attribute("fill", Card.ToCss()).EndEmptyElement().Line();
        TextNode(w, "Calories Intake", 100, 78, 34, "850", Text);
        MenuDots(w, 1124, 76, Text);
        WriteLayeredRadialLayers(w, CaloriesRadialLayers(), 340, 334, 174);
        Lightning(w, 340, 262, Muted);
        CenterText(w, "1240 kcal", 340, 346, 46, "760", Text);
        CenterText(w, "Calories left", 340, 404, 22, "500", Muted);
        Metric(w, 640, 158, "fork", "1750", "kcal", "Eaten calories");
        Metric(w, 930, 158, "flame", "510", "kcal", "Burned calories");
        MacroRow(w, 610, 248, "120", "/325gr", "Carbohydrates", "37%", 0.37);
        MacroRow(w, 610, 342, "70", "/75gr", "Proteins", "93%", 0.93);
        MacroRow(w, 610, 436, "20", "/44gr", "Fats", "45%", 0.45);
        return FinishSvg(w);
    }

    private static void DrawWeightPng(RgbaCanvas c) {
        c.Clear(Background);
        c.FillRoundedRect(134, 26, 628, 716, 44, Card);
        c.DrawTextEmphasized(172, 68, "Weight Data", Text, 36);
        DrawMenu(c, 672, 100, Text);
        DrawLayeredRadialLayers(c, WeightRadialLayers(), 448, 410, 234);

        c.DrawTextEmphasized(372, 308, "78", Text, 66);
        c.DrawText(468, 318, "kg", Text, 50);
        c.DrawText(358, 394, "Current Weight", Muted, 28);
        c.DrawText(214, 456, "85", Muted, 34);
        c.DrawText(392, 456, "13 kg left", Muted, 28);
        c.DrawText(642, 456, "65", Muted, 34);
        c.DrawLine(170, 574, 727, 574, Green, 5);
        DrawCentered(c, "Progress is progress, no matter how slow.", 448, 622, 28, ChartColor.FromHex("#555963"));
        DrawCentered(c, "Keep going, you're getting closer to your goal", 448, 666, 28, ChartColor.FromHex("#555963"));
        DrawCentered(c, "every day!", 448, 710, 28, ChartColor.FromHex("#555963"));
    }

    private static void DrawCaloriesPng(RgbaCanvas c) {
        c.Clear(Background);
        c.FillRoundedRect(64, 16, 1152, 528, 34, Card);
        c.DrawTextEmphasized(100, 44, "Calories Intake", Text, 34);
        DrawMenu(c, 1124, 76, Text);
        DrawLayeredRadialLayers(c, CaloriesRadialLayers(), 340, 334, 174);
        DrawLightning(c, 340, 262, Muted);
        DrawCentered(c, "1240 kcal", 340, 318, 46, Text);
        DrawCentered(c, "Calories left", 340, 382, 22, Muted);
        DrawMetric(c, 640, 158, "fork", "1750", "kcal", "Eaten calories");
        DrawMetric(c, 930, 158, "flame", "510", "kcal", "Burned calories");
        DrawMacro(c, 610, 248, "120", "/325gr", "Carbohydrates", "37%", 0.37);
        DrawMacro(c, 610, 342, "70", "/75gr", "Proteins", "93%", 0.93);
        DrawMacro(c, 610, 436, "20", "/44gr", "Fats", "45%", 0.45);
    }

    private static void SaveCustom(string output, string name, int width, int height, int outputScale, string svg, Action<RgbaCanvas> draw) {
        File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
        File.WriteAllText(Path.Combine(output, name + ".html"), "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>" + name + "</title><style>body{margin:0;background:#f5f6f8;display:grid;place-items:center;min-height:100vh}svg{max-width:100%;height:auto}</style></head><body>" + svg + "</body></html>");
        var canvas = new RgbaCanvas(width, height, 2, null, Math.Max(1, outputScale));
        draw(canvas);
        File.WriteAllBytes(Path.Combine(output, name + ".png"), PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels()));
    }

    private static void StartSvg(SvgMarkupWriter w, int width, int height, string title) {
        w.StartElement("svg").Attribute("xmlns", "http://www.w3.org/2000/svg").Attribute("width", width).Attribute("height", height).Attribute("viewBox", "0 0 " + width + " " + height).Attribute("role", "img").EndStartElement().Line();
        w.StartElement("title").Text(title).EndElement().Line();
    }

    private static string FinishSvg(SvgMarkupWriter w) {
        w.EndElement().Line();
        return w.Build();
    }

    private static void TextNode(SvgMarkupWriter w, string text, double x, double y, double size, string weight, ChartColor color) => w.StartElement("text").Attribute("x", x).Attribute("y", y).Attribute("fill", color.ToCss()).Attribute("font-family", "Inter, ui-sans-serif, system-ui, Segoe UI, Arial, sans-serif").Attribute("font-size", size).Attribute("font-weight", weight).Text(text).EndElement().Line();

    private static void CenterText(SvgMarkupWriter w, string text, double x, double y, double size, string weight, ChartColor color) => w.StartElement("text").Attribute("x", x).Attribute("y", y).Attribute("text-anchor", "middle").Attribute("fill", color.ToCss()).Attribute("font-family", "Inter, ui-sans-serif, system-ui, Segoe UI, Arial, sans-serif").Attribute("font-size", size).Attribute("font-weight", weight).Text(text).EndElement().Line();

    private static void MenuDots(SvgMarkupWriter w, double x, double y, ChartColor color) {
        for (var i = 0; i < 3; i++) w.StartElement("circle").Attribute("cx", x + i * 16).Attribute("cy", y).Attribute("r", 3.6).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
    }

    private static void Metric(SvgMarkupWriter w, double x, double y, string icon, string value, string unit, string label) {
        w.StartElement("circle").Attribute("cx", x).Attribute("cy", y).Attribute("r", 38).Attribute("fill", ChartColor.FromHex("#DDF79B").ToCss()).EndEmptyElement().Line();
        if (icon == "fork") ForkIcon(w, x, y, Muted); else FlameIcon(w, x, y, Muted);
        TextNode(w, value, x + 56, y - 6, 36, "850", Text);
        TextNode(w, unit, x + 150, y - 6, 30, "450", Text);
        TextNode(w, label, x + 56, y + 30, 22, "500", Muted);
    }

    private static void MacroRow(SvgMarkupWriter w, double x, double y, string value, string target, string label, string percent, double ratio) {
        w.StartElement("rect").Attribute("x", x).Attribute("y", y).Attribute("width", 550).Attribute("height", 70).Attribute("rx", 22).Attribute("fill", "#F5F5F8").EndEmptyElement().Line();
        w.StartElement("rect").Attribute("x", x).Attribute("y", y).Attribute("width", 176).Attribute("height", 70).Attribute("rx", 22).Attribute("fill", "#FAFAFB").EndEmptyElement().Line();
        TextNode(w, value, x + 25, y + 48, 36, "850", Text);
        TextNode(w, target, x + 98, y + 48, 22, "500", ChartColor.FromHex("#555963"));
        TextNode(w, label, x + 214, y + 32, 22, "500", Muted);
        TextNode(w, percent, x + 472, y + 32, 22, "850", Text);
        w.StartElement("rect").Attribute("x", x + 214).Attribute("y", y + 46).Attribute("width", 310).Attribute("height", 12).Attribute("rx", 6).Attribute("fill", Card.ToCss()).EndEmptyElement();
        w.StartElement("rect").Attribute("x", x + 214).Attribute("y", y + 46).Attribute("width", 310 * ratio).Attribute("height", 12).Attribute("rx", 6).Attribute("fill", Green.ToCss()).EndEmptyElement().Line();
    }

    private static void Lightning(SvgMarkupWriter w, double x, double y, ChartColor color) {
        w.StartElement("path").Attribute("d", "M " + (x - 20) + " " + (y - 18) + " L " + (x + 4) + " " + (y - 56) + " L " + x + " " + (y - 18) + " L " + (x + 23) + " " + (y - 9) + " L " + (x - 8) + " " + (y + 36) + " L " + (x - 2) + " " + y + " Z").Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", 5).Attribute("stroke-linejoin", "round").Attribute("stroke-linecap", "round").EndEmptyElement().Line();
    }

    private static void ForkIcon(SvgMarkupWriter w, double x, double y, ChartColor color) {
        w.StartElement("path").Attribute("d", "M " + (x - 10) + " " + (y - 15) + " V " + (y + 16) + " M " + (x - 20) + " " + (y - 16) + " V " + (y - 2) + " M " + (x - 10) + " " + (y - 16) + " V " + (y - 2) + " M " + x + " " + (y - 16) + " V " + (y - 2) + " M " + (x - 20) + " " + (y - 2) + " Q " + (x - 10) + " " + (y + 8) + " " + x + " " + (y - 2) + " M " + (x + 14) + " " + (y + 16) + " V " + (y - 16) + " Q " + (x + 26) + " " + (y - 7) + " " + (x + 16) + " " + (y + 2)).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", 3).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
    }

    private static void FlameIcon(SvgMarkupWriter w, double x, double y, ChartColor color) {
        w.StartElement("path").Attribute("d", "M " + x + " " + (y + 20) + " C " + (x - 24) + " " + (y + 8) + " " + (x - 13) + " " + (y - 17) + " " + (x - 3) + " " + (y - 28) + " C " + (x + 1) + " " + (y - 10) + " " + (x + 19) + " " + (y - 9) + " " + (x + 15) + " " + (y - 30) + " C " + (x + 35) + " " + (y - 11) + " " + (x + 28) + " " + (y + 15) + " " + x + " " + (y + 20) + " Z").Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", 4).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
    }

    private static string ArcPath(double cx, double cy, double radius, double start, double end) {
        if (Math.Abs(end - start) >= Math.PI * 2 - 0.000001) {
            var mid = start + Math.PI;
            return new SvgPathDataBuilder().MoveTo(cx + Math.Cos(start) * radius, cy + Math.Sin(start) * radius).ArcTo(radius, radius, 0, false, true, cx + Math.Cos(mid) * radius, cy + Math.Sin(mid) * radius).ArcTo(radius, radius, 0, false, true, cx + Math.Cos(start + Math.PI * 2) * radius, cy + Math.Sin(start + Math.PI * 2) * radius).Build();
        }

        return new SvgPathDataBuilder().MoveTo(cx + Math.Cos(start) * radius, cy + Math.Sin(start) * radius).ArcTo(radius, radius, 0, end - start > Math.PI, true, cx + Math.Cos(end) * radius, cy + Math.Sin(end) * radius).Build();
    }

    private static ChartRadialLayer[] CaloriesRadialLayers() => new[] {
        new ChartRadialLayer("Available area", 100, 0, 100, Track)
            .WithGeometry(1.00, 0.18)
            .WithLineCap(ChartRadialLayerCap.Butt),
        new ChartRadialLayer("Target ring", 100, 0, 100, Yellow)
            .WithGeometry(0.93, 0.035)
            .WithLineCap(ChartRadialLayerCap.Butt),
        new ChartRadialLayer("Current", 1240, 0, 2700, Orange)
            .WithGeometry(0.93, 0.14)
            .WithAngles(-90, 360)
    };

    private static ChartRadialLayer[] WeightRadialLayers() => new[] {
        new ChartRadialLayer("Current weight progress", 100, 0, 100, Orange)
            .WithGeometry(1.00, 0.265)
            .WithAngles(184, 74),
        new ChartRadialLayer("Remaining weight range", 100, 0, 100, Yellow)
            .WithGeometry(1.00, 0.265)
            .WithAngles(279, 77)
            .WithSeparators(8, Card, 3, 0)
    };

    private static ChartTheme WellnessTheme() => ChartTheme.Minimal()
        .WithSurfaceColors(Background, Card, Card, Track, Track)
        .WithTextColors(Text, Muted)
        .WithGuideColors(Track, Track)
        .WithPalette(Orange.ToHex(), Yellow.ToHex(), Green.ToHex(), "#6A7FDB")
        .WithTypography(28, 14, 12, 18, 20, 20)
        .WithCornerRadius(30, 14)
        .WithShadowOpacity(0.03);

    private static void WriteLayeredRadialLayers(SvgMarkupWriter w, IEnumerable<ChartRadialLayer> layers, double cx, double cy, double outerRadius) {
        w.StartElement("g").Attribute("data-cfx-role", "layered-radial-composition").EndStartElement().Line();
        foreach (var layer in layers) {
            var ratio = LayerRatio(layer);
            if (ratio <= 0) continue;
            var radius = outerRadius * layer.RadiusRatio;
            var stroke = outerRadius * layer.StrokeRatio;
            var start = DegreesToRadians(layer.StartAngleDegrees);
            var end = start + DegreesToRadians(layer.SweepAngleDegrees) * ratio;
            w.StartElement("path")
                .Attribute("data-cfx-role", "layered-radial-layer")
                .Attribute("data-cfx-label", layer.Name)
                .Attribute("data-cfx-percent", ratio)
                .Attribute("d", ArcPath(cx, cy, radius, start, end))
                .Attribute("fill", "none")
                .Attribute("stroke", (layer.Color ?? Text).ToCss())
                .Attribute("stroke-width", stroke)
                .Attribute("stroke-linecap", layer.LineCap == ChartRadialLayerCap.Butt ? "butt" : "round");
            if (layer.Opacity < 1) w.Attribute("opacity", layer.Opacity);
            w.EndEmptyElement().Line();
            WriteLayerSeparators(w, layer, cx, cy, radius, stroke, start, end);
        }

        w.EndElement().Line();
    }

    private static void WriteLayerSeparators(SvgMarkupWriter w, ChartRadialLayer layer, double cx, double cy, double radius, double stroke, double start, double end) {
        if (layer.SeparatorCount <= 0) return;
        var separator = layer.SeparatorColor ?? Card;
        var inset = Math.Min(stroke / 2 - 0.5, Math.Max(0, stroke * layer.SeparatorInsetRatio));
        var inner = Math.Max(0, radius - stroke / 2 + inset);
        var outer = radius + stroke / 2 - inset;
        for (var i = 1; i <= layer.SeparatorCount; i++) {
            var angle = start + (end - start) * i / (layer.SeparatorCount + 1);
            w.StartElement("line")
                .Attribute("data-cfx-role", "layered-radial-separator")
                .Attribute("x1", cx + Math.Cos(angle) * inner)
                .Attribute("y1", cy + Math.Sin(angle) * inner)
                .Attribute("x2", cx + Math.Cos(angle) * outer)
                .Attribute("y2", cy + Math.Sin(angle) * outer)
                .Attribute("stroke", separator.ToCss())
                .Attribute("stroke-width", layer.SeparatorStrokeWidth)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement().Line();
        }
    }

    private static void DrawLayeredRadialLayers(RgbaCanvas c, IEnumerable<ChartRadialLayer> layers, double cx, double cy, double outerRadius) {
        foreach (var layer in layers) {
            var ratio = LayerRatio(layer);
            if (ratio <= 0) continue;
            var radius = outerRadius * layer.RadiusRatio;
            var stroke = outerRadius * layer.StrokeRatio;
            var start = DegreesToRadians(layer.StartAngleDegrees);
            var end = start + DegreesToRadians(layer.SweepAngleDegrees) * ratio;
            var color = ApplyExampleOpacity(layer.Color ?? Text, layer.Opacity);
            if (layer.LineCap == ChartRadialLayerCap.Butt) {
                c.FillRingSlice(cx, cy, radius + stroke / 2, Math.Max(0, radius - stroke / 2), start, end, color);
            } else {
                c.DrawArc(cx, cy, radius, start, end, color, stroke);
            }

            DrawLayerSeparators(c, layer, cx, cy, radius, stroke, start, end);
        }
    }

    private static void DrawLayerSeparators(RgbaCanvas c, ChartRadialLayer layer, double cx, double cy, double radius, double stroke, double start, double end) {
        if (layer.SeparatorCount <= 0) return;
        var separator = layer.SeparatorColor ?? Card;
        var inset = Math.Min(stroke / 2 - 0.5, Math.Max(0, stroke * layer.SeparatorInsetRatio));
        var inner = Math.Max(0, radius - stroke / 2 + inset);
        var outer = radius + stroke / 2 - inset;
        for (var i = 1; i <= layer.SeparatorCount; i++) {
            var angle = start + (end - start) * i / (layer.SeparatorCount + 1);
            c.DrawLine(cx + Math.Cos(angle) * inner, cy + Math.Sin(angle) * inner, cx + Math.Cos(angle) * outer, cy + Math.Sin(angle) * outer, separator, layer.SeparatorStrokeWidth);
        }
    }

    private static double LayerRatio(ChartRadialLayer layer) => Math.Max(0, Math.Min(1, (layer.Value - layer.Minimum) / (layer.Maximum - layer.Minimum)));

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static ChartColor ApplyExampleOpacity(ChartColor color, double opacity) {
        var alpha = (byte)Math.Max(0, Math.Min(255, Math.Round(color.A * Math.Max(0, Math.Min(1, opacity)))));
        return ChartColor.FromRgba(color.R, color.G, color.B, alpha);
    }

    private static void DrawMenu(RgbaCanvas c, double x, double y, ChartColor color) {
        for (var i = 0; i < 3; i++) c.DrawCircle(x + i * 16, y, 3.6, color);
    }

    private static void DrawCentered(RgbaCanvas c, string text, double x, double y, double size, ChartColor color) {
        c.DrawText(x - RgbaCanvas.MeasureTextWidth(text, size, null) / 2, y - RgbaCanvas.MeasureTextHeight(size, null) * 0.75, text, color, size);
    }

    private static void DrawLightning(RgbaCanvas c, double x, double y, ChartColor color) {
        c.DrawLine(x - 20, y - 18, x + 4, y - 56, color, 5);
        c.DrawLine(x + 4, y - 56, x, y - 18, color, 5);
        c.DrawLine(x, y - 18, x + 23, y - 9, color, 5);
        c.DrawLine(x + 23, y - 9, x - 8, y + 36, color, 5);
        c.DrawLine(x - 8, y + 36, x - 2, y, color, 5);
    }

    private static void DrawMetric(RgbaCanvas c, double x, double y, string icon, string value, string unit, string label) {
        c.DrawCircle(x, y, 38, ChartColor.FromHex("#DDF79B"));
        if (icon == "fork") {
            c.DrawCircleOutline(x, y, 14, Muted, 3);
            c.DrawLine(x - 5, y - 20, x - 5, y - 1, Muted, 2.4);
            c.DrawLine(x + 4, y - 20, x + 4, y - 1, Muted, 2.4);
            c.DrawLine(x + 14, y - 20, x + 14, y + 18, Muted, 2.4);
        } else {
            c.DrawLine(x - 2, y + 18, x - 15, y + 3, Muted, 3.2);
            c.DrawLine(x - 15, y + 3, x - 4, y - 23, Muted, 3.2);
            c.DrawLine(x - 4, y - 23, x + 4, y - 5, Muted, 3.2);
            c.DrawLine(x + 4, y - 5, x + 14, y - 20, Muted, 3.2);
            c.DrawLine(x + 14, y - 20, x + 20, y + 5, Muted, 3.2);
            c.DrawLine(x + 20, y + 5, x - 2, y + 18, Muted, 3.2);
            c.DrawCircleOutline(x, y + 3, 18, Muted, 2.2);
        }
        c.DrawTextEmphasized(x + 56, y - 38, value, Text, 36);
        c.DrawText(x + 150, y - 35, unit, Text, 30);
        c.DrawText(x + 56, y + 4, label, Muted, 22);
    }

    private static void DrawMacro(RgbaCanvas c, double x, double y, string value, string target, string label, string percent, double ratio) {
        c.FillRoundedRect(x, y, 550, 70, 22, ChartColor.FromHex("#F5F5F8"));
        c.FillRoundedRect(x, y, 176, 70, 22, ChartColor.FromHex("#FAFAFB"));
        c.DrawTextEmphasized(x + 25, y + 11, value, Text, 36);
        c.DrawText(x + 98, y + 24, target, ChartColor.FromHex("#555963"), 22);
        c.DrawText(x + 214, y + 12, label, Muted, 22);
        c.DrawTextEmphasized(x + 472, y + 10, percent, Text, 22);
        c.FillRoundedRect(x + 214, y + 46, 310, 12, 6, Card);
        c.FillRoundedRect(x + 214, y + 46, 310 * ratio, 12, 6, Green);
    }
}
