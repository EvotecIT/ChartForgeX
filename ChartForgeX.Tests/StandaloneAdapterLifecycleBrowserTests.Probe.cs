namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private const string BreadcrumbProbe = """
        window.breadcrumbListeners=[];
        const addBreadcrumbListener=EventTarget.prototype.addEventListener,removeBreadcrumbListener=EventTarget.prototype.removeEventListener;
        EventTarget.prototype.addEventListener=function(type,listener,options){
            if(type==='click'&&this instanceof HTMLButtonElement&&this.hasAttribute('data-cfx-hierarchy-node'))
                breadcrumbListeners.push({target:this,listener,active:true,connectedAtRegistration:this.isConnected});
            return addBreadcrumbListener.call(this,type,listener,options);
        };
        EventTarget.prototype.removeEventListener=function(type,listener,options){
            if(type==='click')for(const entry of breadcrumbListeners)if(entry.target===this&&entry.listener===listener)entry.active=false;
            return removeBreadcrumbListener.call(this,type,listener,options);
        };
        """;

    // Instrument real browser resource boundaries before the shipped adapter starts. This exposes
    // observable ownership without shipping a counter or relying on garbage-collector timing.
    private static string Instrument(string html) => html.Replace("<head>", "<head><script>" + ResourceProbe + "</script>", StringComparison.Ordinal);

    private const string ResourceProbe = """
        window.lifetimeProbe={globals:[],media:[],timers:new Set(),frames:new Set(),observers:new Set(),externalObservers:new Set(),recordGlobals:true,recordObservers:true};
        for(const [name,target] of [['window',window],['document',document]]){
            const add=target.addEventListener.bind(target),remove=target.removeEventListener.bind(target);
            target.addEventListener=(type,listener,options)=>{
                if(lifetimeProbe.recordGlobals&&['pointerup','pointercancel','blur','fullscreenchange','beforeprint','afterprint'].includes(type))
                    lifetimeProbe.globals.push({target:name,type,listener,active:true});
                return add(type,listener,options);
            };
            target.removeEventListener=(type,listener,options)=>{
                for(const entry of lifetimeProbe.globals)if(entry.target===name&&entry.type===type&&entry.listener===listener)entry.active=false;
                return remove(type,listener,options);
            };
        }
        const matchMedia=window.matchMedia.bind(window);
        window.matchMedia=query=>{
            const media=matchMedia(query),entry={media,listeners:[]};lifetimeProbe.media.push(entry);
            const add=media.addEventListener.bind(media),remove=media.removeEventListener.bind(media);
            media.addEventListener=(type,listener,options)=>{if(type==='change')entry.listeners.push({listener,active:true});return add(type,listener,options);};
            media.removeEventListener=(type,listener,options)=>{for(const value of entry.listeners)if(value.listener===listener)value.active=false;return remove(type,listener,options);};
            return media;
        };
        const timeout=window.setTimeout.bind(window),clearTimer=window.clearTimeout.bind(window);
        window.setTimeout=(callback,delay,...args)=>{
            const id=timeout(()=>{lifetimeProbe.timers.delete(id);callback(...args);},delay);
            lifetimeProbe.timers.add(id);return id;
        };
        window.clearTimeout=id=>{lifetimeProbe.timers.delete(id);return clearTimer(id);};
        const frame=window.requestAnimationFrame.bind(window),cancelFrame=window.cancelAnimationFrame.bind(window);
        window.requestAnimationFrame=callback=>{
            const id=frame(time=>{lifetimeProbe.frames.delete(id);callback(time);});
            lifetimeProbe.frames.add(id);return id;
        };
        window.cancelAnimationFrame=id=>{lifetimeProbe.frames.delete(id);return cancelFrame(id);};
        const Observer=window.MutationObserver;
        window.MutationObserver=class extends Observer {
            constructor(callback){super(callback);this.adapterOwned=lifetimeProbe.recordObservers;}
            observe(...args){(this.adapterOwned?lifetimeProbe.observers:lifetimeProbe.externalObservers).add(this);return super.observe(...args);}
            disconnect(){lifetimeProbe.observers.delete(this);lifetimeProbe.externalObservers.delete(this);return super.disconnect();}
        };
        """;
}
