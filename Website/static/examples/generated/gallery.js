(() => {
    'use strict';
    const root = document.documentElement;
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const compactMedia = window.matchMedia('(max-width: 600px)');
    const themes = ['light', 'dark', 'system'];
    const sourceUrl = document.querySelector('meta[name="html-preview-source-url"]')?.content || location.href;
    const pageParams = new URL(sourceUrl).searchParams;
    const canUpdateLocation = ['http:', 'https:'].includes(location.protocol);
    const updateLinkQuery = (link, query) => {
        const wrapper = new URL(link.href, document.baseURI);
        const previewSource = wrapper.searchParams.get('url');
        const destination = new URL(previewSource || wrapper.href);
        destination.search = query;
        if (previewSource) {
            wrapper.searchParams.set('url', destination.href);
            link.href = wrapper.href;
        } else link.href = destination.href;
    };
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
            const currentLink = new URL(link.href, document.baseURI);
            const previewSource = currentLink.searchParams.get('url');
            const destination = new URL(href, previewSource || document.baseURI);
            if (href.endsWith('.html')) {
                if (output === 'png') destination.searchParams.set('view', 'png');
                destination.searchParams.set('theme', preference);
                if (pageParams.has('return')) destination.searchParams.set('return', pageParams.get('return'));
                for (const parameter of ['q', 'family'])
                    if (pageParams.has(parameter)) destination.searchParams.set(parameter, pageParams.get(parameter));
            }
            if (previewSource) {
                currentLink.searchParams.set('url', destination.href);
                link.href = currentLink.href;
            } else {
                link.href = destination.href;
            }
        }
        for (const link of document.querySelectorAll('[data-gallery-return]')) {
            const wrapper = new URL(link.href, document.baseURI);
            const query = new URL(wrapper.searchParams.get('url') || wrapper.href).searchParams;
            query.set('theme', preference);
            updateLinkQuery(link, query.toString());
        }
    };
    for (const button of document.querySelectorAll('[data-set-theme]')) {
        button.addEventListener('click', () => {
            preference = button.dataset.setTheme;
            pageParams.set('theme', preference);
            if (canUpdateLocation) {
                const next = new URL(location.href);
                next.search = pageParams.toString();
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
        if (output === 'png') pageParams.set('view', 'png'); else pageParams.delete('view');
        if (canUpdateLocation) {
            const next = new URL(location.href);
            next.search = pageParams.toString();
            history.replaceState(null, '', next);
        }
        applyOutput();
    });
    applyOutput();

    const search = document.querySelector('[data-gallery-search]');
    if (search) {
        const params = pageParams;
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
            if (search.value.trim()) pageParams.set('q', search.value.trim()); else pageParams.delete('q');
            if (group !== 'all') pageParams.set('family', group); else pageParams.delete('family');
            const next = new URL(sourceUrl);
            next.search = pageParams.toString();
            if (canUpdateLocation) history.replaceState(null, '', next);
            try { sessionStorage.setItem('chartforgex-gallery-return', next.search); } catch { /* Navigation does not require storage. */ }
            applyTheme();
        };
        search.addEventListener('input', filter);
        for (const button of buttons) button.addEventListener('click', () => { group = button.dataset.galleryGroup; filter(); });
        document.querySelector('[data-gallery-reset]').addEventListener('click', () => { search.value = ''; group = 'all'; filter(); search.focus(); });
        filter();
    } else {
        let hostReturn = false;
        const returnPath = pageParams.get('return');
        if (canUpdateLocation && returnPath && returnPath.startsWith('/') && !returnPath.startsWith('//')) {
            const destination = new URL(returnPath, location.href);
            if (destination.origin === location.origin && ['http:', 'https:'].includes(destination.protocol)) {
                for (const link of document.querySelectorAll('a.back-link')) {
                    link.href = destination.href;
                    link.dataset.galleryReturn = '';
                }
                hostReturn = true;
            }
        }
        if (!hostReturn) {
            const query = new URLSearchParams();
            for (const parameter of ['q', 'family', 'theme'])
                if (pageParams.has(parameter)) query.set(parameter, pageParams.get(parameter));
            try {
                const stored = sessionStorage.getItem('chartforgex-gallery-return');
                if (!query.has('q') && !query.has('family') && stored && /^\?/.test(stored)) {
                    const previous = new URLSearchParams(stored);
                    for (const parameter of ['q', 'family']) if (previous.has(parameter)) query.set(parameter, previous.get(parameter));
                }
            } catch { /* Query links work when the preview sandbox blocks storage. */ }
            for (const link of document.querySelectorAll('a.back-link')) {
                updateLinkQuery(link, query.toString());
                link.dataset.galleryReturn = '';
            }
        }
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
