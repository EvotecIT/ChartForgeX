namespace ChartForgeX.Interactivity.Html;

public static partial class TopologyReportHtmlExtensions {
    private const string ReportStyles = """
        :root{color-scheme:light;font-family:Inter,Segoe UI,Arial,sans-serif;color:#17243b;background:#f3f6fa}
        *{box-sizing:border-box}
        body{margin:0}
        main{max-width:1480px;margin:auto;padding:36px}
        header{padding:4px 0 20px}
        h1{font-size:32px;letter-spacing:-.6px;margin:8px 0}
        header p{color:#526078}
        .eyebrow{font-size:11px;font-weight:700;letter-spacing:2px;color:#315abb}
        nav,.search{display:flex;align-items:center;gap:12px;flex-wrap:wrap}
        nav{padding:16px;background:white;border:1px solid #dce3ef;border-radius:14px}
        button,select,input{font:inherit}
        button,select,input[type=search]{border:1px solid #bbc9dc;border-radius:8px;padding:10px 14px;background:white;color:#213856}
        button{cursor:pointer}
        button:hover{background:#edf3ff;border-color:#527bcc}
        button:disabled{opacity:.45;cursor:default}
        :focus-visible{outline:3px solid #3d72d8;outline-offset:3px}
        .fit{margin-left:auto}
        .search{margin-top:22px}
        .search label{font-weight:600}
        .search input{min-width:220px}
        .search p{font-size:13px;color:#526078}
        #results{flex-basis:100%;display:flex;gap:8px;flex-wrap:wrap}
        #page-status{font-size:13px;color:#526078}
        .diagram{overflow:auto;background:white;border:1px solid #dce3ef;border-radius:14px;box-shadow:0 5px 24px #17315309}
        .diagram svg{display:block;max-width:none!important}
        .diagram.fitted svg{max-width:100%!important;height:auto}
        .connections{margin-top:22px}
        h2{font-size:17px}
        .links{display:flex;gap:8px;flex-wrap:wrap}
        .links button{text-align:left;font-size:13px}
        @media(max-width:640px){main{padding:16px}
        h1{font-size:25px}
        .fit{margin-left:0}
        nav{gap:8px}
        button,select{padding:9px}
        header{padding-bottom:12px}
        }
        @media print{nav,.search,#page-status{display:none}
        main{padding:0}
        .diagram{border:0;overflow:visible}
        .diagram svg{max-width:100%!important;height:auto}
        .connections button{border:0}
        }
        """;

    private const string ReportScript = """
        (() => {
            'use strict';
            const picker = document.getElementById('page');
            const content = document.getElementById('content');
            const fit = document.getElementById('fit');
            const search = document.getElementById('search');
            const results = document.getElementById('results');
            const status = document.getElementById('search-status');
            const entries = Array.from(document.getElementById('objects').content.querySelectorAll('button'));
            let current = 0;
            function show(value) {
                const page = Number(value);
                const template = document.getElementById('page-' + page);
                if (!Number.isInteger(page) || !template) return;
                current = page;
                picker.value = String(page);
                content.replaceChildren(template.content.cloneNode(true));
                const links = content.querySelector('.links');
                const linkItems = Array.from(links.children);
                if (linkItems.length > 20) {
                    links.replaceChildren();
                    let shown = 0;
                    const more = document.createElement('button');
                    more.type = 'button';
                    const reveal = () => {
                        for (const item of linkItems.slice(shown, shown + 20)) links.append(item);
                        shown = Math.min(linkItems.length, shown + 20);
                        more.textContent = 'Show more (' + (linkItems.length - shown) + ' remaining)';
                        if (shown < linkItems.length) links.append(more); else more.remove();
                    };
                    more.addEventListener('click', reveal);
                    reveal();
                }
                content.querySelector('.diagram').classList.toggle('fitted', fit.checked);
                document.getElementById('previous').disabled = page === 0;
                document.getElementById('next').disabled = page === picker.options.length - 1;
                document.getElementById('page-status').textContent = page === 0
                    ? 'Overview · choose a detail page to read individual objects'
                    : 'Page ' + page + ' of ' + (picker.options.length - 1) + ' · scroll at natural size, or choose Fit page';
            }
            picker.addEventListener('change', () => show(picker.value));
            document.getElementById('previous').addEventListener('click', () => show(current - 1));
            document.getElementById('next').addEventListener('click', () => show(current + 1));
            fit.addEventListener('change', () => content.querySelector('.diagram').classList.toggle('fitted', fit.checked));
            document.addEventListener('click', event => {
                const button = event.target.closest('button[data-page]');
                if (!button) return;
                show(button.dataset.page);
                const diagram = content.querySelector('.diagram');
                diagram.scrollIntoView({ block: 'start' });
                diagram.focus({ preventScroll: true });
            });
            search.addEventListener('input', () => {
                const query = search.value.trim().toLocaleLowerCase();
                results.replaceChildren();
                if (!query) { status.textContent = ''; return; }
                const matches = entries.filter(entry => entry.textContent.toLocaleLowerCase().includes(query));
                for (const entry of matches.slice(0, 20)) results.append(entry.cloneNode(true));
                status.textContent = matches.length + (matches.length === 1 ? ' match' : ' matches')
                    + (matches.length > 20 ? ' · showing first 20; refine your search' : '');
            });
            show(0);
        })();
        """;
}
