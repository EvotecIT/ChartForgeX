using System;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

using static ChartForgeX.VisualBlocks.SvgVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgFactBlockRenderer {
    private static void WriteIcon(SvgMarkupWriter writer, VisualIcon icon, double x, double y, double size, ChartColor color) {
        var stroke = Math.Max(1.6, size * 0.16);
        if (icon == VisualIcon.ForkKnife) {
            writer.StartElement("path")
                .Attribute("data-cfx-role", "visual-icon")
                .Attribute("data-cfx-icon", "fork-knife")
                .Attribute("d", VisualIconGeometry.ForkKnife(x, y, size))
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", stroke)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement().Line();
            return;
        }

        if (icon == VisualIcon.Flame) {
            writer.StartElement("path")
                .Attribute("data-cfx-role", "visual-icon")
                .Attribute("data-cfx-icon", "flame")
                .Attribute("d", VisualIconGeometry.Flame(x, y, size))
                .Attribute("fill", color.ToCss())
                .EndEmptyElement().Line();
            return;
        }

        if (icon == VisualIcon.Droplet) {
            writer.StartElement("path")
                .Attribute("data-cfx-role", "visual-icon")
                .Attribute("data-cfx-icon", "droplet")
                .Attribute("d", VisualIconGeometry.Droplet(x, y, size))
                .Attribute("fill", color.ToCss())
                .EndEmptyElement().Line();
            return;
        }

        if (icon == VisualIcon.Runner) {
            writer.StartElement("g").Attribute("data-cfx-role", "visual-icon").Attribute("data-cfx-icon", "runner").EndStartElement()
                .StartElement("circle").Attribute("cx", x + size * 0.12).Attribute("cy", y - size * 0.70).Attribute("r", size * 0.16).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", stroke).EndEmptyElement()
                .StartElement("path").Attribute("d", "M " + F(x + size * 0.02) + " " + F(y - size * 0.42) + " L " + F(x - size * 0.18) + " " + F(y - size * 0.02) + " L " + F(x + size * 0.10) + " " + F(y + size * 0.16) + " M " + F(x) + " " + F(y - size * 0.34) + " L " + F(x + size * 0.40) + " " + F(y - size * 0.18) + " M " + F(x - size * 0.18) + " " + F(y - size * 0.02) + " L " + F(x - size * 0.50) + " " + F(y + size * 0.36) + " M " + F(x + size * 0.10) + " " + F(y + size * 0.16) + " L " + F(x + size * 0.48) + " " + F(y + size * 0.50)).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", stroke).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement()
                .EndElement().Line();
            return;
        }

        if (icon == VisualIcon.Bicycle) {
            writer.StartElement("g").Attribute("data-cfx-role", "visual-icon").Attribute("data-cfx-icon", "bicycle").EndStartElement()
                .StartElement("circle").Attribute("cx", x - size * 0.50).Attribute("cy", y + size * 0.34).Attribute("r", size * 0.28).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", stroke).EndEmptyElement()
                .StartElement("circle").Attribute("cx", x + size * 0.50).Attribute("cy", y + size * 0.34).Attribute("r", size * 0.28).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", stroke).EndEmptyElement()
                .StartElement("path").Attribute("d", "M " + F(x - size * 0.50) + " " + F(y + size * 0.34) + " L " + F(x - size * 0.12) + " " + F(y - size * 0.12) + " L " + F(x + size * 0.18) + " " + F(y + size * 0.34) + " L " + F(x - size * 0.50) + " " + F(y + size * 0.34) + " M " + F(x - size * 0.12) + " " + F(y - size * 0.12) + " L " + F(x + size * 0.46) + " " + F(y - size * 0.12) + " L " + F(x + size * 0.50) + " " + F(y + size * 0.34) + " M " + F(x - size * 0.02) + " " + F(y - size * 0.28) + " L " + F(x - size * 0.24) + " " + F(y - size * 0.28)).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", stroke).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement()
                .EndElement().Line();
            return;
        }

        if (icon == VisualIcon.Person) {
            writer.StartElement("g").Attribute("data-cfx-role", "visual-icon").Attribute("data-cfx-icon", "person").EndStartElement()
                .StartElement("circle").Attribute("cx", x).Attribute("cy", y - size * 0.36).Attribute("r", size * 0.28).Attribute("fill", color.ToCss()).EndEmptyElement()
                .StartElement("path").Attribute("d", "M " + F(x - size * 0.60) + " " + F(y + size * 0.62) + " C " + F(x - size * 0.46) + " " + F(y + size * 0.12) + " " + F(x + size * 0.46) + " " + F(y + size * 0.12) + " " + F(x + size * 0.60) + " " + F(y + size * 0.62) + " Z").Attribute("fill", color.ToCss()).EndEmptyElement()
                .EndElement().Line();
            return;
        }

        writer.StartElement("path")
            .Attribute("data-cfx-role", "visual-icon")
            .Attribute("data-cfx-icon", "lightning")
            .Attribute("d", "M " + F(x - size * 0.52) + " " + F(y - size * 0.32) + " L " + F(x + size * 0.10) + " " + F(y - size * 0.92) + " L " + F(x) + " " + F(y - size * 0.26) + " L " + F(x + size * 0.58) + " " + F(y - size * 0.08) + " L " + F(x - size * 0.20) + " " + F(y + size * 0.82) + " L " + F(x - size * 0.04) + " " + F(y + size * 0.08) + " Z")
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", stroke)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .EndEmptyElement().Line();
    }
}
