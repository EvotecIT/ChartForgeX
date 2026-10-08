(() => {
    'use strict';
    const root = document.documentElement;
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const compactMedia = window.matchMedia('(max-width: 600px)');
    const themes = ['light', 'dark', 'system'];
    const pageParams = new URLSearchParams(location.search);
    let output = pageParams.get('view') === 'png' ? 'png' : 'svg';
    const readPreference = () => {
        try { return localStorage.getItem('chartforgex-gallery-theme'); } catch { return null; }
    };
    let preference = themes.includes(pageParams.get('theme')) ? pageParams.get('theme') : readPreference();
    if (!themes.includes(preference)) preference = root.dataset.catalogPage === 'example' ? root.dataset.theme : 'system';
    const applyTheme = () => {
        const mode = preference === 'system' ? (media.matches ? 'dark' : 'light') : preference;
        root.dataset.theme = mode;
        for (const button of document.querySelectorAll('[data-set-theme]'))
            button.setAttribute('aria-pressed', String(button.dataset.setTheme === preference));
        for (const link of document.querySelectorAll('[data-theme-link]')) {
            const compact = compactMedia.matches && (mode === 'dark' ? link.dataset.darkCompactHref : link.dataset.lightCompactHref);
            const href = compact || (mode === 'dark' ? link.dataset.darkHref : link.dataset.lightHref);
            const destination = new URL(href, location.href);
            if (href.endsWith('.html')) {
                if (output === 'png') destination.searchParams.set('view', 'png');
                if (pageParams.has('theme')) destination.searchParams.set('theme', preference);
                if (pageParams.has('return')) destination.searchParams.set('return', pageParams.get('return'));
            }
            link.href = destination.href;
        }
        for (const link of document.querySelectorAll('[data-gallery-return]')) {
            const destination = new URL(link.href, location.href);
            destination.searchParams.set('theme', preference);
            link.href = destination.href;
        }
    };
    for (const button of document.querySelectorAll('[data-set-theme]')) {
        button.addEventListener('click', () => {
            preference = button.dataset.setTheme;
            if (pageParams.has('theme')) {
                pageParams.set('theme', preference);
                const next = new URL(location.href);
                next.searchParams.set('theme', preference);
                history.replaceState(null, '', next);
            }
            try { localStorage.setItem('chartforgex-gallery-theme', preference); } catch { /* The gallery works without storage. */ }
            applyTheme();
        });
    }
    media.addEventListener('change', () => { if (preference === 'system') applyTheme(); });
    compactMedia.addEventListener('change', applyTheme);
    applyTheme();

    const applyOutput = () => {
        root.dataset.outputView = output;
        for (const button of document.querySelectorAll('[data-set-output]'))
            button.setAttribute('aria-pressed', String(button.dataset.setOutput === output));
        applyTheme();
    };
    for (const button of document.querySelectorAll('[data-set-output]')) button.addEventListener('click', () => {
        output = button.dataset.setOutput;
        const next = new URL(location.href);
        if (output === 'png') next.searchParams.set('view', 'png'); else next.searchParams.delete('view');
        history.replaceState(null, '', next);
        applyOutput();
    });
    applyOutput();

    const search = document.querySelector('[data-gallery-search]');
    if (search) {
        const params = new URLSearchParams(location.search);
        const buttons = [...document.querySelectorAll('[data-gallery-group]')];
        const tiles = [...document.querySelectorAll('[data-gallery-family]')];
        let group = buttons.some(button => button.dataset.galleryGroup === params.get('family')) ? params.get('family') : 'all';
        search.value = params.get('q') || '';
        const filter = () => {
            const terms = search.value.toLocaleLowerCase().trim().split(/\s+/).filter(Boolean);
            let count = 0;
            for (const tile of tiles) {
                const text = tile.dataset.search.toLocaleLowerCase();
                const visible = (group === 'all' || tile.dataset.group === group) && terms.every(term => text.includes(term));
                tile.hidden = !visible;
                if (visible) count++;
            }
            for (const button of buttons) button.setAttribute('aria-pressed', String(button.dataset.galleryGroup === group));
            document.querySelector('[data-gallery-count]').textContent = count + (count === 1 ? ' example' : ' examples');
            document.querySelector('[data-gallery-empty]').hidden = count !== 0;
            const next = new URL(location.href);
            if (search.value.trim()) next.searchParams.set('q', search.value.trim()); else next.searchParams.delete('q');
            if (group !== 'all') next.searchParams.set('family', group); else next.searchParams.delete('family');
            history.replaceState(null, '', next);
            try { sessionStorage.setItem('chartforgex-gallery-return', next.search); } catch { /* Navigation does not require storage. */ }
        };
        search.addEventListener('input', filter);
        for (const button of buttons) button.addEventListener('click', () => { group = button.dataset.galleryGroup; filter(); });
        document.querySelector('[data-gallery-reset]').addEventListener('click', () => { search.value = ''; group = 'all'; filter(); search.focus(); });
        filter();
    } else {
        let hostReturn = false;
        const returnPath = pageParams.get('return');
        if (returnPath && returnPath.startsWith('/') && !returnPath.startsWith('//')) {
            const destination = new URL(returnPath, location.href);
            if (destination.origin === location.origin && ['http:', 'https:'].includes(destination.protocol)) {
                for (const link of document.querySelectorAll('a[href="index.html"]')) {
                    link.href = destination.href;
                    link.dataset.galleryReturn = '';
                }
                hostReturn = true;
            }
        }
        try {
            const query = sessionStorage.getItem('chartforgex-gallery-return');
            if (!hostReturn && query && /^\?/.test(query)) for (const link of document.querySelectorAll('a[href="index.html"]')) link.href += query;
        } catch { /* A direct example link always has a working gallery link. */ }
        applyTheme();
    }

    const copy = document.querySelector('[data-copy-source]');
    if (copy) copy.addEventListener('click', async () => {
        const source = [...document.querySelectorAll('[data-example-source]')].find(node => node.closest('[data-visual-theme]').dataset.visualTheme === root.dataset.theme);
        const status = document.querySelector('[data-copy-status]');
        try {
            await navigator.clipboard.writeText(source.textContent);
            status.textContent = 'C# copied';
        } catch {
            const selection = window.getSelection();
            const range = document.createRange();
            range.selectNodeContents(source);
            selection.removeAllRanges();
            selection.addRange(range);
            status.textContent = 'Code selected — copy with your keyboard';
        }
    });
})();
