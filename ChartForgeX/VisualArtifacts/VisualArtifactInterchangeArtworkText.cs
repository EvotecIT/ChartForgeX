using System;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Admits bounded existing raster data without widening ordinary portable text or URL limits.</summary>
internal static class VisualArtifactInterchangeArtworkText {
    internal static bool ValidateImageHref(string? value) {
        if(value==null||value.Length<=VisualArtifactInterchangeValidation.MaximumTextCharacters)return false;
        if(value.Length>VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters)
            throw new ArgumentException("Artwork image data URI exceeds its 2097152-character limit.");
        var comma=value.IndexOf(',');
        if(comma<0||comma>64)throw Invalid();
        var header=value.Substring(0,comma);string mime;
        if(header.Equals("data:image/png;base64",StringComparison.OrdinalIgnoreCase))mime="image/png";
        else if(header.Equals("data:image/jpeg;base64",StringComparison.OrdinalIgnoreCase))mime="image/jpeg";
        else if(header.Equals("data:image/gif;base64",StringComparison.OrdinalIgnoreCase))mime="image/gif";
        else if(header.Equals("data:image/bmp;base64",StringComparison.OrdinalIgnoreCase))mime="image/bmp";
        else if(header.Equals("data:image/x-portable-pixmap;base64",StringComparison.OrdinalIgnoreCase))mime="image/x-portable-pixmap";
        else if(header.Equals("data:image/tiff;base64",StringComparison.OrdinalIgnoreCase))mime="image/tiff";
        else throw Invalid();
        var start=comma+1;var count=value.Length-start;
        if(count==0||count%4!=0)throw Invalid();
        var padding=value[value.Length-1]=='='?value[value.Length-2]=='='?2:1:0;
        var end=value.Length-padding;
        for(var i=start;i<end;i++)if(Sextet(value[i])<0)throw Invalid();
        for(var i=end;i<value.Length;i++)if(value[i]!='=')throw Invalid();
        if(padding==2&&(Sextet(value[end-1])&15)!=0||padding==1&&(Sextet(value[end-1])&3)!=0)throw Invalid();
        // MIME recognition needs only the first33 encoded bytes. No full payload or pixel buffer is allocated here.
        var prefix=Convert.FromBase64String(value.Substring(start,Math.Min(44,count)));
        if(!string.Equals(RasterImageDecoder.MimeTypeFor(prefix),mime,StringComparison.Ordinal))throw Invalid();
        return true;
    }
    private static int Sextet(char c)=>c>='A'&&c<='Z'?c-'A':c>='a'&&c<='z'?c-'a'+26:c>='0'&&c<='9'?c-'0'+52:c=='+'?62:c=='/'?63:-1;
    private static ArgumentException Invalid()=>new("Artwork image href above 65536 characters requires a bounded recognized raster Base64 data URI with matching MIME signature.");
}
