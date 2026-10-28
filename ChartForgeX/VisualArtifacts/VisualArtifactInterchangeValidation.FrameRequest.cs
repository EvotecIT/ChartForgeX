using System;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateFrameRequest(VisualFrame? frame,PreparedDiagramFont? font,VisualArtifactInterchangeEnvelope envelope) {
        if(frame==null)return;
        if(envelope.Family!=VisualArtifactInterchangeFamily.Topology&&envelope.Family!=VisualArtifactInterchangeFamily.Flow)throw new ArgumentException("Captured frame requests require the native Topology or Flow restoration caller.");
        OptionalText(frame.Title,"frame title request");OptionalText(frame.Subtitle,"frame subtitle request");OptionalText(frame.LegendTitle,"frame legend heading request");
        Style(frame.TitleStyle);Style(frame.SubtitleStyle);Style(frame.LegendStyle);
        void Style(TextStyle? value) {
            if(value==null)return;
            if(font==null&&(value.Font.FilePath!=null||value.Font.CapturedExplicitFile))
                throw new ArgumentException("An owned frame font requires its shared captured context-font owner.");
            Text(value.Font.Family,"frame font family");OptionalText(value.Font.FaceName,"frame requested face");OptionalText(value.OpenTypeLanguageTag,"frame language");
            ValidateDetailTextStyle(new TextStyleOverride{FontFamily=value.Font.Family,FontWeight=value.Font.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Italic=value.Font.Italic,Variations=value.Font.Variations,ColorPaletteIndex=value.Font.ColorPaletteIndex});
            Defined(value.Alignment,"frame text alignment",envelope);PositiveOptional(value.FontSize,"frame font size");
        }
    }
}
