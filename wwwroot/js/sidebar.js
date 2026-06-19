// Sidebar JavaScript (base alinhada ao Ulongisi)

window.initSidebarOverlay = function () {
    const checkbox = document.querySelector('.navbar-toggler');
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebar-overlay');

    if (!checkbox || !overlay) return;

    checkbox.addEventListener('change', function () {
        if (this.checked) {
            if (sidebar) sidebar.classList.add('open');
            overlay.style.display = 'block';
            requestAnimationFrame(() => { overlay.style.opacity = '1'; });
        } else {
            if (sidebar) sidebar.classList.remove('open');
            overlay.style.opacity = '0';
            setTimeout(() => { overlay.style.display = 'none'; }, 250);
        }
    });

    overlay.addEventListener('click', function () {
        checkbox.checked = false;
        checkbox.dispatchEvent(new Event('change'));
    });
};

/** Navegação “voltar”: usa histórico do WebView quando possível. */
window.olondongeTryHistoryBack = function () {
    if (window.history.length > 1) {
        window.history.back();
        return true;
    }
    return false;
};

