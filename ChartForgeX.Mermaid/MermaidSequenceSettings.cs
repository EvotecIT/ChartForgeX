using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Maps sequence source paints and numbering through the existing native sequence owner.</summary>
internal sealed partial class MermaidSequenceSettings {
    private static readonly string[] Colors = { "actorBkg", "actorBorder", "actorTextColor", "actorLineColor", "signalColor", "signalTextColor", "noteBkgColor", "noteBorderColor", "noteTextColor", "activationBkgColor", "activationBorderColor", "labelBoxBkgColor", "labelBoxBorderColor", "labelTextColor", "rectBkgColor", "sequenceNumberColor" };
    private readonly Dictionary<string, ChartColor> _colors = new(StringComparer.Ordinal);
    private bool? _showSequenceNumbers;
    internal bool ShowSequenceNumbers => _showSequenceNumbers == true;
    internal ChartColor? RectangleBackground => _colors.TryGetValue("rectBkgColor", out var rectangle) ? rectangle
        : _colors.TryGetValue("actorBkg", out var actor) ? actor : (ChartColor?)null;

    internal static bool IsScalarPath(string path) => IsPresentationPath(path) || path == "sequence.showSequenceNumbers"
        || path.StartsWith("themeVariables.", StringComparison.Ordinal) && Array.IndexOf(Colors, path.Substring("themeVariables.".Length)) >= 0;

    internal bool Handles(string path) {
        if (HandlesPresentation(path)) return true;
        if (path == "sequence.showSequenceNumbers" || path.StartsWith("sequence.showSequenceNumbers.", StringComparison.Ordinal)) return true;
        foreach (var name in Colors) {
            var prefix = "themeVariables." + name;
            if (path == prefix || path.StartsWith(prefix + ".", StringComparison.Ordinal)) return true;
        }
        return false;
    }

    internal static MermaidSequenceSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidSequenceSettings();
        foreach(var name in Colors)if(configuration.Preset?.TryGetAuthored(name,out var color)==true)settings._colors[name]=color;
        var numbering = configuration.Last("sequence.showSequenceNumbers");
        if (numbering != null) {
            if (!numbering.IsString && bool.TryParse(numbering.Value, out var enabled)) settings._showSequenceNumbers = enabled;
            else error(numbering, "Mermaid configuration 'sequence.showSequenceNumbers' must be a boolean.");
        }
        settings.ReadPresentation(configuration,error);
        return settings;
    }

    internal void Apply(SequenceArtifact sequence, bool hasAutonumber, MermaidPresentationOptions? host = null, MermaidThemePreset? preset = null, bool neo = false) {
        ApplyPresentation(sequence,host,neo,hasAutonumber || _showSequenceNumbers==true);
        bool Color(string name,out ChartColor value)=>preset!=null?preset.TryGet(name,out value):_colors.TryGetValue(name,out value);

        sequence.Paint.LifelineStroke=new ChartForgeX.Core.ChartStrokeStyle(width:.5,dash:Array.Empty<double>());
        if (_showSequenceNumbers.HasValue && !hasAutonumber)
            sequence.Metadata["mermaid.autonumber"] = _showSequenceNumbers.Value ? "true" : "false";
        if (Color("actorBkg", out var actorBkg)) sequence.Paint.ParticipantFill = actorBkg;
        if (Color("actorBorder", out var actorBorder)) sequence.Paint.ParticipantBorder = actorBorder;
        if (Color("actorTextColor", out var actorTextColor)) sequence.Paint.ParticipantText = actorTextColor;
        if (Color("actorLineColor", out var actorLineColor)) sequence.Paint.Lifeline = actorLineColor;
        if (Color("signalColor", out var signalColor)) sequence.Paint.MessageLine = signalColor;
        if (Color("signalTextColor", out var signalTextColor)) sequence.Paint.MessageText = signalTextColor;
        if (Color("sequenceNumberColor", out var numberText)) sequence.Paint.NumberText = numberText;
        if (Color("noteBkgColor", out var noteBkgColor)) sequence.Paint.NoteFill = noteBkgColor;
        if (Color("noteBorderColor", out var noteBorderColor)) sequence.Paint.NoteBorder = noteBorderColor;
        if (Color("noteTextColor", out var noteTextColor)) sequence.Paint.NoteText = noteTextColor;
        if (Color("activationBkgColor", out var activationBkgColor)) sequence.Paint.ActivationFill = activationBkgColor;
        if (Color("activationBorderColor", out var activationBorderColor)) sequence.Paint.ActivationBorder = activationBorderColor;
        if (Color("labelBoxBkgColor", out var labelBoxBkgColor)) sequence.Paint.FragmentFill = labelBoxBkgColor;
        if (Color("labelBoxBorderColor", out var labelBoxBorderColor)) sequence.Paint.FragmentBorder = labelBoxBorderColor;
        if (Color("labelTextColor", out var labelTextColor)) sequence.Paint.FragmentText = labelTextColor;
        if(Color("loopTextColor",out var title))sequence.Paint.FragmentTitleText=title;
        if(preset!=null)for(var index=0;index<sequence.Participants.Count;index++){var participant=sequence.Participants[index];var background=preset.AtBackground(index);var border=preset.AtBorder(index);
            if(background.HasValue)participant.Fill=background;
            participant.ActivationFill=preset.AtActivationBackground(index);
            if(border.HasValue){participant.Border=border;participant.ActivationBorder=border;}
        }
    }
}
