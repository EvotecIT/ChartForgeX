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
        .relationship-pages{display:flex;align-items:center;gap:12px;margin-top:16px;flex-wrap:wrap;font-size:13px}
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
            const entries = JSON.parse(document.getElementById('objects').textContent);
            let current = 0;
            function makeButton(record) {
                const button = document.createElement('button');
                button.type = 'button';
                button.dataset.page = record[0];
                button.lang = record[3];
                button.textContent = record[1];
                if (record[2]) {
                    const suffix = document.createElement('span');
                    suffix.lang = 'en';
                    suffix.textContent = record[2];
                    button.append(suffix);
                }
                return button;
            }
            function show(value) {
                const page = Number(value);
                const pageData = document.getElementById('page-' + page);
                if (!Number.isInteger(page) || !pageData) return;
                current = page;
                picker.value = String(page);
                content.innerHTML = JSON.parse(pageData.textContent);
                const links = content.querySelector('.links');
                const linkItems = JSON.parse(document.getElementById('links-' + page).textContent);
                const controls = content.querySelector('.relationship-pages');
                let offset = 0;
                const previous = document.createElement('button');
                const next = document.createElement('button');
                const count = document.createElement('span');
                previous.type = next.type = 'button';
                previous.textContent = page === 0 ? 'Previous detail pages' : 'Previous relationships';
                next.textContent = page === 0 ? 'Next detail pages' : 'Next relationships';
                count.setAttribute('role', 'status');
                function renderLinks() {
                    links.replaceChildren(...linkItems.slice(offset, offset + 20).map(makeButton));
                    previous.disabled = offset === 0;
                    next.disabled = offset + 20 >= linkItems.length;
                    count.textContent = (offset + 1) + '–' + Math.min(offset + 20, linkItems.length) + ' of ' + linkItems.length;
                }
                if (linkItems.length > 20) controls.append(previous, count, next);
                previous.addEventListener('click', () => { offset = Math.max(0, offset - 20); renderLinks(); });
                next.addEventListener('click', () => { offset = Math.min(Math.floor((linkItems.length - 1) / 20) * 20, offset + 20); renderLinks(); });
                renderLinks();
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
            function normalize(value, language) {
                try { return value.toLocaleLowerCase(language || 'en'); }
                catch { return value.toLowerCase(); }
            }
            search.addEventListener('input', () => {
                const query = search.value.trim();
                results.replaceChildren();
                if (!query) { status.textContent = ''; return; }
                const matches = entries.filter(entry => normalize(entry[1], entry[3]).includes(normalize(query, entry[3])));
                for (const entry of matches.slice(0, 20)) results.append(makeButton(entry));
                status.textContent = matches.length + (matches.length === 1 ? ' match' : ' matches')
                    + (matches.length > 20 ? ' · showing first 20; refine your search' : '');
            });
            show(0);
        })();
        """;
}
