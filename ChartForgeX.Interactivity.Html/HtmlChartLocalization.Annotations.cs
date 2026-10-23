using System.Collections.Generic;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlChartLocalization {
    private static Dictionary<HtmlChartText, string> AnnotationDefaults() => new() {
        [HtmlChartText.AnnotationEditor] = "Annotation editor",
        [HtmlChartText.AnnotationChoose] = "Drawing",
        [HtmlChartText.AnnotationNote] = "Add note",
        [HtmlChartText.AnnotationLine] = "Add line",
        [HtmlChartText.AnnotationChannel] = "Add channel",
        [HtmlChartText.AnnotationEdit] = "Edit",
        [HtmlChartText.AnnotationDelete] = "Delete",
        [HtmlChartText.AnnotationCaption] = "Caption",
        [HtmlChartText.AnnotationApply] = "Apply",
        [HtmlChartText.AnnotationCancel] = "Cancel",
        [HtmlChartText.AnnotationUndo] = "Undo",
        [HtmlChartText.AnnotationRedo] = "Redo",
        [HtmlChartText.AnnotationClose] = "Close editor",
        [HtmlChartText.AnnotationStart] = "Start anchor",
        [HtmlChartText.AnnotationEnd] = "End anchor",
        [HtmlChartText.AnnotationOffset] = "Channel offset",
        [HtmlChartText.AnnotationUpdating] = "Updating annotation…",
        [HtmlChartText.AnnotationFailed] = "The annotation could not be updated.",
        [HtmlChartText.AnnotationDraft] = "Draft: move an anchor, then apply or cancel.",
        [HtmlChartText.AnnotationDisconnected] = "Connect native preparation to edit annotations.",
    };
}
