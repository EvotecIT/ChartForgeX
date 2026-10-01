using ChartForgeX.Composition;
using ChartForgeX.Primitives;

/// <summary>Shows the composition shape APIs at a social-preview size.</summary>
internal static class CompositionShapeExamples {
    internal static void Write(string output) {
        var blue = ChartColor.FromHex("#38bdf8");
        var slate = ChartColor.FromHex("#334155");
        var white = ChartColor.FromHex("#f8fafc");
        var image = ImageComposition.Create(960, 420, ChartColor.FromHex("#0f172a"))
            .FillRectangleLinearGradient(24, 24, 912, 372, ChartColor.FromHex("#172554"), ChartColor.FromHex("#0f172a"), angle: 30, radius: 24)
            .FillRectangleRadialGradient(24, 24, 912, 372, 700, 80, 380, ChartColor.FromHex("#164e63"), ChartColor.FromHex("#0f172a"), radius: 24)
            .DrawText(56, 52, 848, "Composition shapes", 30, white)
            .FillCircle(160, 205, 48, blue)
            .StrokeCircle(160, 205, 66, white, 1.5)
            .FillEllipse(365, 205, 72, 38, slate)
            .StrokeEllipse(365, 205, 72, 38, blue, 4)
            .DrawArc(575, 205, 62, -135, 270, blue, 12, ImageLineCap.Square)
            .DrawProgressRing(790, 205, 62, 16, 0.72, slate, blue)
            .DrawText(90, 300, 170, "Circle", 20, white)
            .DrawText(295, 300, 170, "Ellipse", 20, white)
            .DrawText(510, 300, 170, "Square-cap arc", 20, white)
            .DrawText(720, 300, 170, "Progress ring", 20, white);
        image.Save(Path.Combine(output, "composition-shapes.png"));
        File.WriteAllText(Path.Combine(output, "composition-shapes.html"),
            "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Composition shapes</title><body style=\"margin:0;background:#0f172a\"><img src=\"composition-shapes.png\" alt=\"Circles, ellipse, square-cap arc and progress ring over linear and radial gradients\" style=\"display:block;width:100%;max-width:960px;height:auto\"></body></html>");
    }
}
