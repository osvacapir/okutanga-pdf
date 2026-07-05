// Gestor de tema (okutangaPDF): light / dark / auto.
// - "auto" não fixa atributo (segue prefers-color-scheme via @media em theme-dark.css).
// - "light"/"dark" fixam <html data-theme="...">.
// Persistência em localStorage (não bloqueante, sem dependências do Blazor).

(function () {
    if (window.okutangaTheme) {
        return;
    }

    const STORAGE_KEY = 'ol_theme';
    const VALID = new Set(['light', 'dark', 'auto']);

    function readStored() {
        try {
            const v = window.localStorage.getItem(STORAGE_KEY);
            return VALID.has(v) ? v : 'auto';
        } catch (_) {
            return 'auto';
        }
    }

    function writeStored(value) {
        try {
            if (value === 'auto') {
                window.localStorage.removeItem(STORAGE_KEY);
            } else {
                window.localStorage.setItem(STORAGE_KEY, value);
            }
        } catch (_) {
            // Storage indisponível (modo privado em algumas WebViews) — ignorar.
        }
    }

    function applyMode(mode) {
        const html = document.documentElement;
        if (mode === 'dark' || mode === 'light') {
            html.setAttribute('data-theme', mode);
        } else {
            html.removeAttribute('data-theme');
        }
    }

    function effectiveMode(mode) {
        if (mode === 'light' || mode === 'dark') return mode;
        try {
            const mql = window.matchMedia('(prefers-color-scheme: dark)');
            return mql.matches ? 'dark' : 'light';
        } catch (_) {
            return 'light';
        }
    }

    function setMode(mode) {
        if (!VALID.has(mode)) {
            mode = 'auto';
        }
        writeStored(mode);
        applyMode(mode);
        return effectiveMode(mode);
    }

    function getMode() {
        return readStored();
    }

    function getEffective() {
        return effectiveMode(readStored());
    }

    function toggle() {
        // light -> dark -> auto -> light
        const current = readStored();
        const next = current === 'light' ? 'dark' : current === 'dark' ? 'auto' : 'light';
        setMode(next);
        return { mode: next, effective: effectiveMode(next) };
    }

    function init() {
        applyMode(readStored());
    }

    window.okutangaTheme = {
        init: init,
        getMode: getMode,
        getEffective: getEffective,
        setMode: setMode,
        toggle: toggle,
    };

    init();
})();
