// Dock flutuante (Olondonge): swipe entre secções + ocultar ao fazer scroll para baixo.
// Orden visual no dock (esquerda → direita): Notas · Horário · Início · Histórico · Grelha.
// Swipe left = avança índice; swipe right = recua. Início é o ponto central.

(function () {
    if (window.olondongeDockNav) {
        return;
    }

    const ROUTES = [
        '/grades',
        '/horario',
        '/home',
        '/historico-academico',
        '/curriculo',
    ];

    const HIDDEN_CLASS = 'ol-glass-dock-root--hidden';
    const SCROLL_DELTA_PX = 8;
    const TOP_THRESHOLD_PX = 32;
    const SWIPE_MIN_PX = 48;
    const SWIPE_MAX_MS = 600;
    const SWIPE_RATIO = 1.35; // |dx| > |dy| * RATIO para contar como horizontal

    const SKIP_SWIPE_SELECTOR = [
        'input',
        'textarea',
        'select',
        'button',
        '[contenteditable="true"]',
        '[data-no-swipe]',
        '.ol-glass-dock-root',
        '.ol-fab-refresh',
        '.user-menu',
        '.login-container',
        '.splash-container',
        '[role="dialog"]',
    ].join(',');

    const SCROLL_BOUNDARY_SELECTOR = 'article.content, body';

    let lastScrollY = 0;
    let initialised = false;
    let touch = null;

    function normalisePath(path) {
        if (!path) return '/';
        const cleaned = path.replace(/\/+$/, '');
        return cleaned.length === 0 ? '/' : cleaned.toLowerCase();
    }

    function getCurrentRouteIndex() {
        const path = normalisePath(window.location.pathname);
        for (let i = 0; i < ROUTES.length; i++) {
            const route = ROUTES[i];
            if (path === route || path.startsWith(route + '/')) {
                return i;
            }
        }
        return -1;
    }

    function navigateTo(path) {
        const link = document.querySelector('.ol-glass-dock-root a[href="' + path + '"]');
        if (link) {
            link.click();
        }
    }

    function navigateNext() {
        const i = getCurrentRouteIndex();
        if (i < 0 || i >= ROUTES.length - 1) return;
        navigateTo(ROUTES[i + 1]);
    }

    function navigatePrev() {
        const i = getCurrentRouteIndex();
        if (i <= 0) return;
        navigateTo(ROUTES[i - 1]);
    }

    function setDockHidden(hidden) {
        const dockRoot = document.querySelector('.ol-glass-dock-root');
        const pageRoot = document.getElementById('page-root');
        if (dockRoot) {
            dockRoot.classList.toggle(HIDDEN_CLASS, hidden);
        }
        if (pageRoot && pageRoot.classList.contains('layout-phone-dock')) {
            pageRoot.classList.toggle('dock-bottom-hidden', hidden);
        }
    }

    function handleScroll() {
        const y = window.scrollY || document.documentElement.scrollTop || 0;
        const dockRoot = document.querySelector('.ol-glass-dock-root');
        if (!dockRoot) {
            lastScrollY = y;
            return;
        }
        const dy = y - lastScrollY;
        if (Math.abs(dy) < SCROLL_DELTA_PX) {
            return;
        }
        if (y < TOP_THRESHOLD_PX) {
            setDockHidden(false);
        } else if (dy > 0) {
            setDockHidden(true);
        } else if (dy < 0) {
            setDockHidden(false);
        }
        lastScrollY = y;
    }

    function isSwipeIgnored(target) {
        if (!target || target.nodeType !== 1) return false;
        if (target.closest(SKIP_SWIPE_SELECTOR)) return true;
        // Procurar scroll horizontal ativo até ao limite do conteúdo da página.
        let node = target;
        while (node && node !== document.body) {
            if (node.scrollWidth > node.clientWidth + 4) {
                const overflow = window.getComputedStyle(node).overflowX;
                if (overflow === 'auto' || overflow === 'scroll') {
                    return true;
                }
            }
            if (node.matches && node.matches(SCROLL_BOUNDARY_SELECTOR)) {
                break;
            }
            node = node.parentElement;
        }
        return false;
    }

    function onTouchStart(event) {
        touch = null;
        if (event.touches.length !== 1) return;
        if (getCurrentRouteIndex() < 0) return;
        if (isSwipeIgnored(event.target)) return;
        const t = event.touches[0];
        touch = {
            x: t.clientX,
            y: t.clientY,
            t: Date.now(),
        };
    }

    function onTouchEnd(event) {
        const start = touch;
        touch = null;
        if (!start) return;
        const t = event.changedTouches && event.changedTouches[0];
        if (!t) return;
        const dx = t.clientX - start.x;
        const dy = t.clientY - start.y;
        const dt = Date.now() - start.t;
        if (dt > SWIPE_MAX_MS) return;
        if (Math.abs(dx) < SWIPE_MIN_PX) return;
        if (Math.abs(dx) < Math.abs(dy) * SWIPE_RATIO) return;
        if (dx < 0) {
            navigateNext();
        } else {
            navigatePrev();
        }
    }

    function onTouchCancel() {
        touch = null;
    }

    function init() {
        if (initialised) return;
        initialised = true;
        lastScrollY = window.scrollY || 0;
        window.addEventListener('scroll', handleScroll, { passive: true });
        document.addEventListener('touchstart', onTouchStart, { passive: true });
        document.addEventListener('touchend', onTouchEnd, { passive: true });
        document.addEventListener('touchcancel', onTouchCancel, { passive: true });
        window.addEventListener('popstate', function () {
            lastScrollY = window.scrollY || 0;
            setDockHidden(false);
        });
        document.body.addEventListener(
            'click',
            function (e) {
                const a = e.target.closest && e.target.closest('a[href^="/"]');
                if (!a || a.target === '_blank' || a.hasAttribute('download')) return;
                if (a.getAttribute('href') === '#') return;
                window.setTimeout(function () {
                    lastScrollY = window.scrollY || 0;
                    setDockHidden(false);
                }, 0);
            },
            true
        );
    }

    window.olondongeDockNav = { init: init };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
