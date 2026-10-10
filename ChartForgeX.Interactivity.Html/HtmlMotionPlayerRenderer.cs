using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Optional browser controls for a self-contained sampled ChartForgeX motion SVG, without depending on its producer package.</summary>
public sealed class HtmlMotionPlayerRenderer {
    /// <summary>Renders a complete player page with seeking, chapters, playback rate and a text transcript.</summary>
    /// <remarks>The producer owns frame timing. This adapter displays that schedule and replaces SVG autoplay with user-controlled playback.</remarks>
    public string RenderPage(string animatedSvg, string title = "Story player") {
        if (animatedSvg == null) throw new ArgumentNullException(nameof(animatedSvg));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A page title is required.", nameof(title));
        using var text = new StringReader(animatedSvg);
        using var reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 64L * 1024 * 1024 });
        var root = XDocument.Load(reader).Root ?? throw new ArgumentException("A motion SVG is required.", nameof(animatedSvg));
        // XML nodes are emitted into text/html. Only elements and ordinary escaped text have the same inert meaning there.
        if (root.DescendantNodes().Any(node => node is not XElement && node.GetType() != typeof(XText)))
            throw new ArgumentException("Motion SVG must not contain processing instructions, comments or CDATA.", nameof(animatedSvg));
        var ns = XNamespace.Get("http://www.w3.org/2000/svg");
        if (root.Name != ns + "svg" || (string?)root.Attribute("data-cfx-story") != "visual" ||
            !double.TryParse((string?)root.Attribute("data-cfx-motion-duration"), NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) ||
            double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0 || duration > 1810) throw new ArgumentException("A bounded sampled story SVG is required.", nameof(animatedSvg));
        if (!int.TryParse((string?)root.Attribute("data-cfx-motion-plays"), NumberStyles.None, CultureInfo.InvariantCulture, out var plays) || plays < 0 || plays > 65536)
            throw new ArgumentException("Motion SVG requires a valid play count.", nameof(animatedSvg));
        foreach (var element in root.DescendantsAndSelf().ToArray()) {
            if (element.Name == ns + "style") { element.Remove(); continue; }
            if (element.Name.Namespace != ns || element.Name.LocalName != "svg" && element.Name.LocalName != "g" && element.Name.LocalName != "image" &&
                element.Name.LocalName != "title" && element.Name.LocalName != "desc") throw new ArgumentException("The motion player accepts only sampled SVG image frames.", nameof(animatedSvg));
            foreach (var attribute in element.Attributes().ToArray()) {
                if (attribute.IsNamespaceDeclaration && attribute.Value != "http://www.w3.org/2000/svg" && attribute.Value != "http://www.w3.org/1999/xlink")
                    throw new ArgumentException("Motion SVG contains unsupported namespace declarations.", nameof(animatedSvg));
                if (!attribute.IsNamespaceDeclaration && attribute.Name.NamespaceName.Length > 0 &&
                    !(attribute.Name == XName.Get("href", "http://www.w3.org/1999/xlink")))
                    throw new ArgumentException("Motion SVG contains unsupported namespaced attributes.", nameof(animatedSvg));
                if (attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Motion SVG must not contain event handlers.", nameof(animatedSvg));
                if (attribute.Name.LocalName == "style") attribute.Remove();
                else if (!attribute.IsNamespaceDeclaration && !attribute.Name.LocalName.StartsWith("data-", StringComparison.Ordinal) &&
                    attribute.Name.LocalName != "id" && attribute.Name.LocalName != "class" && attribute.Name.LocalName != "width" &&
                    attribute.Name.LocalName != "height" && attribute.Name.LocalName != "viewBox" && attribute.Name.LocalName != "role" &&
                    attribute.Name.LocalName != "aria-labelledby" && attribute.Name.LocalName != "preserveAspectRatio" &&
                    attribute.Name.LocalName != "href" && attribute.Name.LocalName != "x" && attribute.Name.LocalName != "y")
                    throw new ArgumentException("Motion frames contain unsupported attributes.", nameof(animatedSvg));
                if (attribute.Name.LocalName == "href" && !attribute.Value.StartsWith("data:image/svg+xml;base64,", StringComparison.Ordinal) &&
                    !attribute.Value.StartsWith("data:image/png;base64,", StringComparison.Ordinal)) throw new ArgumentException("Motion frames must be embedded images.", nameof(animatedSvg));
            }
        }
        var frames = root.Elements(ns + "g").Where(element => element.Attribute("data-cfx-frame-time") != null).ToArray();
        if (frames.Length < 1 || frames.Length > 3600) throw new ArgumentException("The player requires one to 3600 sampled frames.", nameof(animatedSvg));
        var previous = -1d;
        foreach (var frame in frames) {
            if (!double.TryParse((string?)frame.Attribute("data-cfx-frame-time"), NumberStyles.Float, CultureInfo.InvariantCulture, out var start) ||
                double.IsNaN(start) || double.IsInfinity(start) || start < 0 || start >= duration || start <= previous)
                throw new ArgumentException("Motion frame starts must be strictly increasing within the play duration.", nameof(animatedSvg));
            previous = start;
            if (!double.TryParse((string?)frame.Attribute("data-cfx-chapter-start"), NumberStyles.Float, CultureInfo.InvariantCulture, out var chapterStart) ||
                double.IsNaN(chapterStart) || double.IsInfinity(chapterStart) || chapterStart < 0 || chapterStart >= duration ||
                frame.Attribute("data-cfx-scene") == null) throw new ArgumentException("Motion frames require valid chapter metadata.", nameof(animatedSvg));
        }
        if ((string?)frames[0].Attribute("data-cfx-frame-time") != "0") throw new ArgumentException("The first frame must begin at zero.", nameof(animatedSvg));
        var chapterNodes = root.Elements(ns + "g").Where(element => element.Attribute("data-cfx-chapter-id") != null).ToArray();
        if (chapterNodes.Length < 1 || chapterNodes.Length > 24) throw new ArgumentException("The player requires one to 24 declared chapters.", nameof(animatedSvg));
        var chapterIds = new HashSet<string>(StringComparer.Ordinal);
        var frameStarts = new HashSet<double>(frames.Select(frame => double.Parse((string)frame.Attribute("data-cfx-frame-time")!, CultureInfo.InvariantCulture)));
        previous = -1;
        var previousSample = -1d;
        foreach (var chapter in chapterNodes) {
            var id = (string?)chapter.Attribute("data-cfx-chapter-id");
            if (id == null || string.IsNullOrWhiteSpace(id) || !chapterIds.Add(id) ||
                !double.TryParse((string?)chapter.Attribute("data-cfx-chapter-start"), NumberStyles.Float, CultureInfo.InvariantCulture, out var start) ||
                double.IsNaN(start) || double.IsInfinity(start) || start < 0 || start >= duration || start <= previous ||
                !double.TryParse((string?)chapter.Attribute("data-cfx-chapter-frame-time"), NumberStyles.Float, CultureInfo.InvariantCulture, out var sample) ||
                !frameStarts.Contains(sample) || sample < previousSample)
                throw new ArgumentException("Chapters require unique identifiers, ordered boundaries and readable frame destinations.", nameof(animatedSvg));
            previous = start;
            previousSample = sample;
        }
        if ((string?)chapterNodes[0].Attribute("data-cfx-chapter-start") != "0" || (string?)chapterNodes[0].Attribute("data-cfx-chapter-frame-time") != "0")
            throw new ArgumentException("The first chapter must begin with the first frame.", nameof(animatedSvg));
        var transcript = root.Element(ns + "desc")?.Value ?? title;
        return "<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + WebUtility.HtmlEncode(title) +
            "</title><style>" + Styles + "</style></head><body><main class=\"cfx-motion-player\"><div class=\"cfx-motion-stage\">" + root.ToString(SaveOptions.DisableFormatting) +
            "</div><div class=\"cfx-motion-controls\"><button type=\"button\" data-action=\"play\" aria-label=\"Play story\">Play</button>" +
            "<button type=\"button\" data-action=\"restart\" aria-label=\"Restart story\">Restart</button><output aria-live=\"off\">0:00</output>" +
            "<input type=\"range\" min=\"0\" max=\"" + duration.ToString("0.#########", CultureInfo.InvariantCulture) + "\" step=\"0.01\" value=\"0\" aria-label=\"Story position\">" +
            "<label>Speed <select data-action=\"speed\"><option value=\"0.5\">0.5×</option><option value=\"1\" selected>1×</option><option value=\"1.5\">1.5×</option><option value=\"2\">2×</option></select></label>" +
            "</div><nav class=\"cfx-motion-chapters\" aria-label=\"Story chapters\"></nav><details><summary>Read the transcript</summary><pre>" + WebUtility.HtmlEncode(transcript) +
            "</pre></details></main><script>" + Script + "</script></body></html>";
    }

    private const string Styles = "html{color-scheme:dark}body{-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision;margin:0;padding:20px;background:#0c1016;color:#e8edf3;font:14px system-ui,sans-serif;box-sizing:border-box}.cfx-motion-player{max-width:1280px;margin:auto}.cfx-motion-stage{overflow:visible;display:grid;place-items:center}.cfx-motion-stage>svg{display:block;max-width:100%;max-height:75vh;height:auto}.cfx-story-frame{display:none}.cfx-story-frame-last{display:inline}.cfx-motion-controls{display:flex;align-items:center;gap:12px;flex-wrap:wrap;padding:16px 0}.cfx-motion-controls input{flex:1;min-width:140px;accent-color:#6bc8ef}.cfx-motion-controls output{font-variant-numeric:tabular-nums;min-width:100px}button,select{font:inherit;color:inherit;background:#1b2635;border:1px solid #405066;border-radius:8px;padding:8px 12px;cursor:pointer}button:focus-visible,select:focus-visible,input:focus-visible,summary:focus-visible{outline:2px solid #79d6fa;outline-offset:3px}.cfx-motion-chapters{display:flex;flex-wrap:wrap;gap:8px;margin:0 0 18px}.cfx-motion-chapters button[aria-current=true]{border-color:#79d6fa;background:#193d52}details{border-top:1px solid #354255;padding:16px 0}summary{cursor:pointer}pre{white-space:pre-wrap;overflow-wrap:anywhere;line-height:1.55;font:13px ui-monospace,monospace}@media(max-width:480px){body{padding:8px}.cfx-motion-controls{gap:8px}.cfx-motion-controls input{order:5;flex-basis:100%}.cfx-motion-stage>svg{max-height:70vh}}@media print{body{background:transparent}.cfx-motion-controls,.cfx-motion-chapters,script{display:none}.cfx-story-frame{display:none!important}.cfx-story-frame-last{display:inline!important}}";

    private const string Script = @"(()=>{'use strict';const player=document.querySelector('.cfx-motion-player'),svg=player.querySelector('svg'),frames=[...svg.children].filter(f=>f.localName==='g'&&f.hasAttribute('data-cfx-frame-time')),starts=frames.map(f=>Number(f.dataset.cfxFrameTime)),duration=Number(svg.dataset.cfxMotionDuration),plays=Number(svg.dataset.cfxMotionPlays),seek=player.querySelector('input'),play=player.querySelector('[data-action=play]'),out=player.querySelector('output'),nav=player.querySelector('nav');let time=0,rate=1,playing=false,last=0,request=0,visible=-1,completed=0,selectedChapter=null;const chapters=[];for(const f of [...svg.children].filter(f=>f.localName==='g'&&f.hasAttribute('data-cfx-chapter-id'))){const c={id:f.dataset.cfxChapterId,start:Number(f.dataset.cfxChapterStart),seek:Number(f.dataset.cfxChapterFrameTime),title:f.dataset.cfxChapterTitle};const b=document.createElement('button');b.type='button';b.textContent=c.title||c.id;b.addEventListener('click',()=>{time=c.seek;selectedChapter=c;last=performance.now();show();});c.button=b;chapters.push(c);nav.append(b);}const clock=t=>Math.floor(t/60)+':'+String(Math.floor(t%60)).padStart(2,'0');function show(){let index=0;for(let i=1;i<starts.length&&starts[i]<=time;i++)index=i;if(index!==visible){if(visible>=0)frames[visible].style.display='none';for(let i=0;visible<0&&i<frames.length;i++)frames[i].style.display='none';frames[index].style.display='inline';visible=index;}seek.value=String(time);out.textContent=clock(time)+' / '+clock(duration);let chapter=chapters[0];for(const c of chapters)if(c.start<=time)chapter=c;if(selectedChapter&&selectedChapter.seek===starts[index])chapter=selectedChapter;else selectedChapter=null;for(const c of chapters)c.button.setAttribute('aria-current',String(c===chapter));}function setPlaying(value){playing=value;play.textContent=value?'Pause':'Play';play.setAttribute('aria-label',value?'Pause story':'Play story');cancelAnimationFrame(request);if(value){if(time>=duration){time=0;completed=0;}last=performance.now();request=requestAnimationFrame(tick);}show();}function tick(now){time+=(now-last)*rate/1000;last=now;if(time>=duration){const crossed=Math.floor(time/duration);completed+=crossed;if(plays===0||completed<plays)time%=duration;else{time=duration;setPlaying(false);return;}}show();request=requestAnimationFrame(tick);}play.addEventListener('click',()=>setPlaying(!playing));player.querySelector('[data-action=restart]').addEventListener('click',()=>{time=0;completed=0;selectedChapter=null;last=performance.now();show();});seek.addEventListener('input',()=>{time=Number(seek.value);selectedChapter=null;last=performance.now();show();});player.querySelector('[data-action=speed]').addEventListener('change',event=>{rate=Number(event.target.value);last=performance.now();});document.addEventListener('visibilitychange',()=>{if(document.hidden)setPlaying(false);});window.addEventListener('pagehide',()=>cancelAnimationFrame(request));show();})();";
}
