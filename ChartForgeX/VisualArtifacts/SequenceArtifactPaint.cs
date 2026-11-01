using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Optional explicit paints for sequence roles; null roles resolve from the rendering theme.</summary>
/// <remarks>Explicit role colors remain authored presentation when a host supplies its own theme.</remarks>
public sealed class SequenceArtifactPaint {
    /// <summary>Gets or sets an immutable note contour shadow; note text remains vector content.</summary>
    public ChartForgeX.Effects.VisualDropShadow? NoteShadow { get; set; }
    /// <summary>Gets or sets an immutable fragment keyword-tab contour shadow; kind/title text remains vector content.</summary>
    public ChartForgeX.Effects.VisualDropShadow? FragmentTabShadow { get; set; }
    /// <summary>Gets or sets an immutable rect-block background shadow, independently of later annotations.</summary>
    public ChartForgeX.Effects.VisualDropShadow? BackgroundShadow { get; set; }
    /// <summary>Gets or sets the explicit participant surface color.</summary>
    public ChartColor? ParticipantFill { get; set; }
    /// <summary>Gets or sets the explicit participant outline color.</summary>
    public ChartColor? ParticipantBorder { get; set; }
    /// <summary>Gets or sets the explicit participant caption color.</summary>
    public ChartColor? ParticipantText { get; set; }
    /// <summary>Gets or sets the explicit participant lifeline color.</summary>
    public ChartColor? Lifeline { get; set; }
    /// <summary>Gets or sets optional immutable lifeline stroke overrides; null retains the ordinary native width and dash.</summary>
    public ChartStrokeStyle? LifelineStroke {get;set;}
    /// <summary>Gets or sets the explicit message line and arrowhead color.</summary>
    public ChartColor? MessageLine { get; set; }
    /// <summary>Gets or sets the explicit message caption color.</summary>
    public ChartColor? MessageText { get; set; }
    /// <summary>Gets or sets a pill drawn behind every message caption; null keeps captions without a backplate.</summary>
    public ChartColor? MessageLabelFill { get; set; }
    /// <summary>Gets or sets the border of the message caption pill; null draws the pill without a border.</summary>
    public ChartColor? MessageLabelBorder { get; set; }
    /// <summary>Gets or sets the distinct number text ink; an explicit Number typography color takes precedence.</summary>
    public ChartColor? NumberText { get; set; }
    /// <summary>Gets or sets the number badge fill; null resolves through MessageLine and the native theme.</summary>
    public ChartColor? NumberFill { get; set; }
    /// <summary>Gets or sets the explicit note surface color.</summary>
    public ChartColor? NoteFill { get; set; }
    /// <summary>Gets or sets the explicit note outline color.</summary>
    public ChartColor? NoteBorder { get; set; }
    /// <summary>Gets or sets the explicit note caption color.</summary>
    public ChartColor? NoteText { get; set; }
    /// <summary>Gets or sets the explicit activation surface color.</summary>
    public ChartColor? ActivationFill { get; set; }
    /// <summary>Gets or sets the explicit activation outline color.</summary>
    public ChartColor? ActivationBorder { get; set; }
    /// <summary>Gets or sets the explicit fragment header surface color.</summary>
    public ChartColor? FragmentFill { get; set; }
    /// <summary>Gets or sets the explicit fragment frame and branch separator color.</summary>
    public ChartColor? FragmentBorder { get; set; }
    /// <summary>Gets or sets the explicit fragment and branch caption color.</summary>
    public ChartColor? FragmentText { get; set; }
    /// <summary>Gets or sets an optional fragment title/section caption ink; null retains FragmentText.</summary>
    public ChartColor? FragmentTitleText {get;set;}
}
