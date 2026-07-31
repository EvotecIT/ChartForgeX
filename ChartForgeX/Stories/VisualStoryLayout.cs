using System;
using System.Collections.Generic;

namespace ChartForgeX.Stories;

internal readonly struct VisualStoryBounds {
    public VisualStoryBounds(double x, double y, double width, double height) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
}

internal static class VisualStoryLayout {
    internal const double OuterPadding = 34;
    internal const double HeaderHeight = 84;
    internal const double PanelGap = 20;
    internal const double PanelPadding = 20;
    internal const double PanelTitleHeight = 30;
    internal const double MinimumSourceContentLength = 18.5;
    internal const double MinimumTextContentLength = 32;
    internal const double MinimumTerminalContentWidth = 160;
    internal const double MinimumTerminalContentHeight = 90;

    public static IReadOnlyList<VisualStoryBounds> Panels(VisualStory story, VisualStoryScene scene) {
        var available = new VisualStoryBounds(
            OuterPadding,
            HeaderHeight + 18,
            story.Width - OuterPadding * 2,
            story.Height - HeaderHeight - OuterPadding - 18);
        var output = new List<VisualStoryBounds>(scene.Panels.Count);
        if (scene.Layout == VisualStorySceneLayout.Focus) {
            output.Add(available);
            return output;
        }

        var weights = 0d;
        foreach (var panel in scene.Panels) weights += panel.Weight;
        var horizontal = scene.Layout == VisualStorySceneLayout.Split;
        var totalLength = (horizontal ? available.Width : available.Height) - PanelGap * (scene.Panels.Count - 1);
        var cursor = horizontal ? available.X : available.Y;
        for (var index = 0; index < scene.Panels.Count; index++) {
            var length = index == scene.Panels.Count - 1
                ? (horizontal ? available.X + available.Width : available.Y + available.Height) - cursor
                : totalLength * scene.Panels[index].Weight / weights;
            output.Add(horizontal
                ? new VisualStoryBounds(cursor, available.Y, length, available.Height)
                : new VisualStoryBounds(available.X, cursor, available.Width, length));
            cursor += length + PanelGap;
        }
        return output;
    }

    public static VisualStoryBounds PanelContent(VisualStoryPanel panel, VisualStoryBounds bounds) {
        var contentY = bounds.Y + PanelPadding;
        if (panel.Title.Length > 0) contentY += PanelTitleHeight;
        var content = new VisualStoryBounds(
            bounds.X + PanelPadding,
            contentY,
            bounds.Width - PanelPadding * 2,
            bounds.Y + bounds.Height - PanelPadding - contentY);
        if (content.Width <= 0 || content.Height <= 0) {
            throw new InvalidOperationException(
                "Visual-story panel '" + panel.Id +
                "' has no drawable content area. Increase the story size, reduce the panel count, or use a different scene layout.");
        }
        if (panel.Surface.Kind == VisualStorySurfaceKind.Source &&
            (content.Width < MinimumSourceContentLength || content.Height < MinimumSourceContentLength)) {
            throw new InvalidOperationException(
                "Visual-story source panel '" + panel.Id +
                "' is too small to render a source line. Increase the story size, reduce the panel count, rebalance panel weights, or use a different scene layout.");
        }
        if (panel.Surface.Kind == VisualStorySurfaceKind.Text &&
            (content.Width < MinimumTextContentLength || content.Height < MinimumTextContentLength)) {
            throw new InvalidOperationException(
                "Visual-story text panel '" + panel.Id +
                "' is too small to render text without crossing its bounds. Increase the story size, reduce the panel count, rebalance panel weights, or use a different scene layout.");
        }
        if (panel.Surface.Kind == VisualStorySurfaceKind.Terminal &&
            (content.Width < MinimumTerminalContentWidth || content.Height < MinimumTerminalContentHeight)) {
            throw new InvalidOperationException(
                "Visual-story terminal panel '" + panel.Id +
                "' is too small to keep terminal chrome and text readable. Increase the story size, reduce the panel count, rebalance panel weights, or use a different scene layout.");
        }
        return content;
    }
}
