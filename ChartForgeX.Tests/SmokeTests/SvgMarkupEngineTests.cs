using System;
using System.IO;
using System.Linq;
using ChartForgeX.Svg;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgMarkupWriterStreamsEscapedElements() {
        var writer = new SvgMarkupWriter();
        writer.StartElement("svg")
            .Attribute("viewBox", "0 0 10 10")
            .Attribute("data-title", "A < B & \"C\"")
            .EndStartElement();
        writer.StartElement("g")
            .Attribute("data-visible", true)
            .Attribute("data-skip", (string?)null)
            .Attribute("focusable", true)
            .EndStartElement();
        writer.StartElement("text")
            .Attribute("x", 1.23456)
            .Attribute("y", -2)
            .EndStartElement()
            .Text("A < B & C")
            .EndElement();
        writer.StartElement("circle")
            .Attribute("cx", 5)
            .Attribute("cy", 6)
            .Attribute("r", 2.5)
            .EndEmptyElement();
        writer.EndElement();
        writer.EndElement();

        Assert(
            writer.Build() == "<svg viewBox=\"0 0 10 10\" data-title=\"A &lt; B &amp; &quot;C&quot;\"><g data-visible=\"true\" focusable=\"true\"><text x=\"1.235\" y=\"-2\">A &lt; B &amp; C</text><circle cx=\"5\" cy=\"6\" r=\"2.5\"/></g></svg>",
            "SVG markup writer should stream escaped text, escaped attributes, optional attributes, booleans, and invariant numbers.");
    }

    private static void SvgAstBuildsEditableOrderedMarkup() {
        var document = SvgDocument.Create(120, 80);
        document.Root
            .Attribute("role", "img")
            .Element("style", style => style.Raw(".label{font-weight:600}"));
        document.Root
            .Element("g", group => {
                group.Attribute("id", "series-1")
                    .Class("series")
                    .Class("active")
                    .Data("cfx-role", "series")
                    .Style("fill", "none")
                    .Style("stroke", "#2563EB");
                group.Element("rect", rect => rect
                    .Attribute("x", 1.23456)
                    .Attribute("y", 2)
                    .Attribute("width", 30)
                    .Attribute("height", 12)
                    .Attribute("aria-hidden", true));
                group.Element("text", text => text
                    .Attribute("class", "label")
                    .Text("A < B & C"));
            });

        var markup = document.ToMarkup();

        Assert(
            markup == "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"120\" height=\"80\" viewBox=\"0 0 120 80\" role=\"img\"><style>.label{font-weight:600}</style><g id=\"series-1\" class=\"series active\" data-cfx-role=\"series\" style=\"fill:none;stroke:#2563EB\"><rect x=\"1.235\" y=\"2\" width=\"30\" height=\"12\" aria-hidden=\"true\"/><text class=\"label\">A &lt; B &amp; C</text></g></svg>",
            "SVG AST should preserve ordered attributes and children while writing XML-style escaped SVG markup.");
    }

    private static void SvgAstSupportsQueryAndAttributeEditing() {
        var document = SvgDocument.Create(100, 50);
        var group = document.Root.Element("g", element => element
            .Attribute("id", "series")
            .Class("plot")
            .Class("highlight"));
        group.Element("circle", circle => circle
            .Attribute("id", "point-1")
            .Class("point")
            .Attribute("r", 5)
            .Attribute("fill", "#2563EB"));

        var point = document.FindById("point-1");
        Assert(point != null, "SVG AST should find descendants by id.");
        point!.Attribute("r", 7).RemoveAttribute("fill");

        Assert(document.Root.FindByTag("circle").Count() == 1, "SVG AST should query descendants by tag name.");
        Assert(document.Root.FindByClass("highlight").Single() == group, "SVG AST should query class-list tokens.");
        Assert(point.GetAttribute("r") == "7", "SVG AST should update an existing attribute without changing its name.");
        Assert(point.GetAttribute("fill") == null, "SVG AST should remove attributes from an editable node.");
        Assert(
            document.ToMarkup().Contains("<circle id=\"point-1\" class=\"point\" r=\"7\"/>", StringComparison.Ordinal),
            "SVG AST edits should be reflected in saved markup.");
    }

    private static void SvgAstSupportsStructuralEditingAndCloning() {
        var document = SvgDocument.Parse("<svg><defs><g id=\"marker\"><circle r=\"4\"/></g></defs><g id=\"plot\"><text id=\"label\">Label</text></g></svg>");
        var marker = document.FindById("marker");
        var plot = document.FindById("plot");
        var label = document.FindById("label");

        Assert(marker != null && plot != null && label != null, "SVG AST structural test should load source nodes.");

        var clone = marker!.CloneElement()
            .Attribute("id", "marker-copy")
            .Class("copied")
            .RemoveClass("missing");
        plot!.Insert(plot.IndexOf(label!), clone);
        Assert(clone.Parent == plot, "SVG AST insert should attach cloned nodes to the new parent.");
        Assert(marker.Parent != plot, "SVG AST clone should not move the source node.");
        Assert(clone.HasClass("copied"), "SVG AST class helper should recognize class-list tokens.");

        Assert(label!.RemoveFromParent(), "SVG AST nodes should remove themselves from their parent.");
        Assert(label.Parent == null, "SVG AST remove should detach the removed node.");
        Assert(!plot.Remove(label), "SVG AST remove should return false when the node is no longer a child.");

        var saved = document.ToMarkup();
        Assert(saved.Contains("<g id=\"marker\"><circle r=\"4\"/></g>", StringComparison.Ordinal), "SVG AST clone should preserve the original source node.");
        Assert(saved.Contains("<g id=\"marker-copy\" class=\"copied\"><circle r=\"4\"/></g>", StringComparison.Ordinal), "SVG AST clone should save copied descendants.");
        Assert(!saved.Contains("<text id=\"label\">", StringComparison.Ordinal), "SVG AST structural remove should affect saved markup.");
    }

    private static void SvgAstParsesEditsAndSavesSvgMarkup() {
        const string markup = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"120\" height=\"80\"><!--loaded--><defs><linearGradient id=\"fill\"><stop offset=\"0%\" stop-color=\"#fff\"/></linearGradient></defs><text id=\"label\">A &amp; B</text><use xmlns:xlink=\"http://www.w3.org/1999/xlink\" href=\"#shape\" xlink:href=\"#legacy\"/></svg>";

        var document = SvgDocument.Parse(markup);
        var label = document.FindById("label");
        var use = document.Root.FindByTag("use").Single();

        Assert(label != null, "SVG AST parser should load editable descendant elements.");
        Assert(document.Root.FindByTag("linearGradient").Count() == 1, "SVG AST parser should keep nested SVG definitions.");
        Assert(use.GetAttribute("xlink:href") == "#legacy", "SVG AST parser should keep prefixed SVG attributes.");

        label!.SetText("Changed < label");
        use.Attribute("href", "#new-shape");

        var saved = document.ToMarkup();
        Assert(saved.Contains("<!--loaded-->", StringComparison.Ordinal), "SVG AST save should preserve comments loaded inside the root.");
        Assert(saved.Contains("<text id=\"label\">Changed &lt; label</text>", StringComparison.Ordinal), "SVG AST save should escape edited text.");
        Assert(saved.Contains("<use xmlns:xlink=\"http://www.w3.org/1999/xlink\" href=\"#new-shape\" xlink:href=\"#legacy\"/>", StringComparison.Ordinal), "SVG AST save should preserve and update ordered attributes.");

        using var writer = new StringWriter();
        document.Save(writer);
        Assert(writer.ToString() == saved, "SVG AST save should write the same markup as ToMarkup.");
    }

    private static void SvgAstRejectsUnsafeDtdContent() {
        const string markup = "<!DOCTYPE svg [<!ENTITY xxe SYSTEM \"file:///c:/windows/win.ini\">]><svg>&xxe;</svg>";

        AssertThrows<System.Xml.XmlException>(
            () => SvgDocument.Parse(markup),
            "SVG AST parser should reject DTDs when loading arbitrary SVG content.");
    }

    private static void SvgAstRejectsMultipleParents() {
        var child = new SvgElement("circle");
        new SvgElement("g").Add(child);

        AssertThrows<InvalidOperationException>(
            () => new SvgElement("svg").Add(child),
            "SVG AST nodes should not be accidentally shared across parents.");
    }

    private static void SvgMarkupWriterRejectsIncompleteMarkup() {
        var writer = new SvgMarkupWriter();
        writer.StartElement("svg").EndStartElement();

        AssertThrows<InvalidOperationException>(
            () => writer.Build(),
            "SVG markup writer should reject incomplete element stacks before callers publish malformed markup.");
        AssertThrows<ArgumentException>(
            () => new SvgMarkupWriter().StartElement("data role"),
            "SVG markup writer should reject invalid element names.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => new SvgMarkupWriter().StartElement("circle").Attribute("r", double.NaN),
            "SVG markup writer should reject non-finite numeric attributes.");
    }

    private static void SvgPathDataBuilderFormatsDeterministicPaths() {
        var path = new SvgPathDataBuilder()
            .MoveTo(1.23456, 2)
            .LineTo(4, 5.67891)
            .QuadraticTo(6, 7, 8, 9)
            .CubicTo(1, 2, 3, 4, 5, 6)
            .ArcTo(7, 8, 0, largeArc: true, sweep: false, x: 9, y: 10)
            .Close()
            .Build();

        Assert(
            path == "M 1.235 2 L 4 5.679 Q 6 7 8 9 C 1 2 3 4 5 6 A 7 8 0 1 0 9 10 Z",
            "SVG path data builder should format path commands deterministically with invariant numeric values.");
    }

    private static void SvgPathDataParsesAndNormalizesCommands() {
        var path = SvgPathData.Parse("M10-20 30,40 h5v-2 c1 2 3 4 5 6 s7 8 9 10 q11 12 13 14 t15 16 a7 8 45 1 0 17 18z");

        Assert(path.Commands.Count == 10, "SVG path parser should expand repeated move coordinates and keep every command.");
        Assert(path.Commands[0].Command == 'M' && path.Commands[1].Command == 'L', "SVG path parser should treat repeated moveto coordinates as lineto commands.");
        Assert(
            path.ToMarkup() == "M 10 -20 L 30 40 h 5 v -2 c 1 2 3 4 5 6 s 7 8 9 10 q 11 12 13 14 t 15 16 a 7 8 45 1 0 17 18 z",
            "SVG path parser should normalize compact SVG path data without losing command casing.");
    }

    private static void SvgPathDataEditsLoadedPathElements() {
        var document = SvgDocument.Parse("<svg><path id=\"series\" d=\"M0 0L10 10Q12 13 14 15Z\"/></svg>");
        var path = document.FindById("series");
        Assert(path != null, "SVG path data edit test should load a path element.");

        var data = path!.GetPathData();
        Assert(data != null, "SVG path elements should expose parsed path data.");
        data!.Replace(1, new SvgPathCommand('L', new[] { 20.0, 30.0 }))
            .Insert(2, new SvgPathCommand('H', new[] { 25.0 }))
            .RemoveAt(3);
        path.PathData(data);

        Assert(
            document.ToMarkup().Contains("<path id=\"series\" d=\"M 0 0 L 20 30 H 25 Z\"/>", StringComparison.Ordinal),
            "SVG path data edits should save back through the path attribute.");
    }

    private static void SvgPathDataRejectsMalformedCommands() {
        AssertThrows<FormatException>(
            () => SvgPathData.Parse("M 0 0 L 10"),
            "SVG path parser should reject incomplete commands.");
        AssertThrows<FormatException>(
            () => SvgPathData.Parse("M 0 0 R 1 2"),
            "SVG path parser should reject unsupported command letters.");
        AssertThrows<ArgumentException>(
            () => new SvgPathCommand('L', new[] { 1.0 }),
            "SVG path command should reject incorrect parameter counts.");
    }

    private static void SvgTransformListParsesAndNormalizesTransforms() {
        var transform = SvgTransformList.Parse("translate(10,-20) rotate(45 5 6) scale(2) skewX(-10) matrix(1 0 0 1 3 4)");

        Assert(transform.Transforms.Count == 5, "SVG transform parser should keep each transform operation.");
        Assert(transform.Transforms[0].Name == "translate" && transform.Transforms[1].Name == "rotate", "SVG transform parser should preserve transform names.");
        Assert(
            transform.ToMarkup() == "translate(10 -20) rotate(45 5 6) scale(2) skewX(-10) matrix(1 0 0 1 3 4)",
            "SVG transform parser should normalize separators and numbers without collapsing readable transform operations.");
    }

    private static void SvgTransformListEditsLoadedElements() {
        var document = SvgDocument.Parse("<svg><g id=\"plot\" transform=\"translate(1,2) scale(2)\"><path d=\"M0 0L1 1\"/></g></svg>");
        var plot = document.FindById("plot");
        Assert(plot != null, "SVG transform edit test should load a transformed element.");

        var transform = plot!.GetTransform();
        Assert(transform != null, "SVG transformed elements should expose parsed transform data.");
        transform!.Replace(1, new SvgTransform("rotate", new[] { 45.0, 10.0, 20.0 }))
            .Insert(1, new SvgTransform("skewY", new[] { -5.0 }));
        plot.Transform(transform);

        Assert(
            document.ToMarkup().Contains("<g id=\"plot\" transform=\"translate(1 2) skewY(-5) rotate(45 10 20)\">", StringComparison.Ordinal),
            "SVG transform edits should save back through the transform attribute.");
    }

    private static void SvgTransformListRejectsMalformedTransforms() {
        AssertThrows<FormatException>(
            () => SvgTransformList.Parse("translate(1 2"),
            "SVG transform parser should reject missing closing parentheses.");
        AssertThrows<ArgumentException>(
            () => SvgTransformList.Parse("unknown(1)"),
            "SVG transform parser should reject unsupported transform names.");
        AssertThrows<ArgumentException>(
            () => new SvgTransform("matrix", new[] { 1.0, 0.0 }),
            "SVG transform model should reject incorrect parameter counts.");
    }

    private static void SvgStyleDeclarationListParsesAndNormalizesStyles() {
        var style = SvgStyleDeclarationList.Parse(" fill : #2563EB ; stroke-width: 2 ; filter:url(\"data:image/svg+xml;a=b\"); opacity:.5; ");

        Assert(style.Declarations.Count == 4, "SVG style parser should keep each inline declaration.");
        Assert(style.Get("filter") == "url(\"data:image/svg+xml;a=b\")", "SVG style parser should keep semicolons inside quoted function values.");
        Assert(
            style.ToMarkup() == "fill:#2563EB;stroke-width:2;filter:url(\"data:image/svg+xml;a=b\");opacity:.5",
            "SVG style parser should normalize declaration spacing without changing values.");
    }

    private static void SvgStyleDeclarationListEditsLoadedElements() {
        var document = SvgDocument.Parse("<svg><rect id=\"bar\" style=\"fill:#2563EB;stroke:#111;opacity:.8\"/></svg>");
        var rect = document.FindById("bar");
        Assert(rect != null, "SVG style edit test should load a styled element.");

        var style = rect!.GetStyle();
        Assert(style != null, "SVG styled elements should expose parsed style data.");
        style!.Set("fill", "#16A34A")
            .Remove("stroke")
            .Set("stroke-width", "2");
        rect.Style(style);

        Assert(
            document.ToMarkup().Contains("<rect id=\"bar\" style=\"fill:#16A34A;opacity:.8;stroke-width:2\"/>", StringComparison.Ordinal),
            "SVG style edits should save back through the style attribute.");

        rect.Style("opacity", null);
        rect.Style("stroke-width", null);
        rect.Style("fill", null);
        Assert(rect.GetAttribute("style") == null, "SVG style helper should remove the style attribute when no declarations remain.");
    }

    private static void SvgStyleDeclarationListRejectsMalformedStyles() {
        AssertThrows<FormatException>(
            () => SvgStyleDeclarationList.Parse("fill"),
            "SVG style parser should reject declarations without ':'.");
        AssertThrows<ArgumentException>(
            () => SvgStyleDeclarationList.Parse("bad name:red"),
            "SVG style parser should reject invalid property names.");
        AssertThrows<ArgumentException>(
            () => new SvgStyleDeclaration("fill;color", "red"),
            "SVG style declaration should reject property names containing semicolons.");
    }

    private static void SvgViewBoxParsesEditsAndSavesRootViewport() {
        var document = SvgDocument.Parse("<svg viewBox=\"-10,-20 100 80\"><rect width=\"10\" height=\"10\"/></svg>");
        var viewBox = document.Root.GetViewBox();

        Assert(viewBox.HasValue, "SVG root viewBox should parse into a typed value.");
        Assert(viewBox!.Value.MinX == -10 && viewBox.Value.MinY == -20, "SVG viewBox parser should preserve origin.");

        document.Root.ViewBox(viewBox.Value.Expand(5).WithSize(120, 90));

        Assert(
            document.ToMarkup().Contains("<svg viewBox=\"-15 -25 120 90\">", StringComparison.Ordinal),
            "SVG viewBox edits should save back through the viewBox attribute.");
    }

    private static void SvgViewBoxRejectsMalformedValues() {
        AssertThrows<FormatException>(
            () => SvgViewBox.Parse("0 0 100"),
            "SVG viewBox parser should reject missing numbers.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => new SvgViewBox(0, 0, -1, 100),
            "SVG viewBox should reject negative width.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => new SvgViewBox(0, 0, 100, -1),
            "SVG viewBox should reject negative height.");
    }

    private static void SvgPointListParsesEditsAndSavesPolygonPoints() {
        var document = SvgDocument.Parse("<svg><polygon id=\"tile\" points=\"0,0 10,0 10 12 0 12\"/></svg>");
        var polygon = document.FindById("tile");
        Assert(polygon != null, "SVG point list edit test should load a polygon element.");

        var points = polygon!.GetPoints();
        Assert(points != null && points.Points.Count == 4, "SVG point lists should parse polygon point pairs.");
        points!.Replace(2, new SvgPoint(12, 14))
            .Insert(1, new SvgPoint(4.5, -2))
            .RemoveAt(0);
        polygon.Points(points);

        Assert(
            document.ToMarkup().Contains("<polygon id=\"tile\" points=\"4.5 -2 10 0 12 14 0 12\"/>", StringComparison.Ordinal),
            "SVG point list edits should save back through the points attribute.");
    }

    private static void SvgPointListRejectsMalformedValues() {
        AssertThrows<FormatException>(
            () => SvgPointList.Parse("0 0 1"),
            "SVG point lists should reject odd coordinate counts.");
        AssertThrows<FormatException>(
            () => SvgPointList.Parse("0 0 nope"),
            "SVG point lists should reject invalid numeric tokens.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => new SvgPointList().Insert(2, new SvgPoint(0, 0)),
            "SVG point list insert should reject invalid indexes.");
    }

    private static void SvgMarkupEngineStaysDependencyFreeAndStreaming() {
        var root = FindRepositoryRoot();
        var writer = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgMarkupWriter.cs"));
        var pathBuilder = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgPathDataBuilder.cs"));
        var pathData = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgPathData.cs"));
        var transformData = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgTransformList.cs"));
        var styleData = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgStyleDeclarationList.cs"));
        var viewBoxData = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgViewBox.cs"));
        var pointData = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgPointList.cs"));
        var ast = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgDocument.cs")),
            File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgElement.cs")),
            File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgNode.cs")));

        Assert(writer.Contains("StringBuilder", StringComparison.Ordinal), "SVG markup writer should remain a streaming StringBuilder helper.");
        Assert(!writer.Contains("HtmlForgeX", StringComparison.Ordinal) && !pathBuilder.Contains("HtmlForgeX", StringComparison.Ordinal) && !pathData.Contains("HtmlForgeX", StringComparison.Ordinal) && !transformData.Contains("HtmlForgeX", StringComparison.Ordinal) && !styleData.Contains("HtmlForgeX", StringComparison.Ordinal) && !viewBoxData.Contains("HtmlForgeX", StringComparison.Ordinal) && !pointData.Contains("HtmlForgeX", StringComparison.Ordinal) && !ast.Contains("HtmlForgeX", StringComparison.Ordinal), "SVG markup engine should not depend on HtmlForgeX.");
        Assert(!writer.Contains("XmlDocument", StringComparison.Ordinal) && !writer.Contains("XDocument", StringComparison.Ordinal) && !ast.Contains("XmlDocument", StringComparison.Ordinal) && !ast.Contains("XDocument", StringComparison.Ordinal), "SVG markup engine should not switch to DOM-based XML construction.");
    }
}
