// Leitor PDF okutangaPDF — pdf.js 3.x (scroll contínuo, pinch, pesquisa, miniaturas)
(function () {
    if (window.okutangaPdfReader) {
        return;
    }

    const MIN_SCALE = 0.5;
    const MAX_SCALE = 3.0;
    const THUMB_SCALE = 0.2;

    let pdfDoc = null;
    let ready = false;
    let mode = 'continuous';
    let scale = 1.0;
    let dotNetRef = null;
    let observer = null;
    let scrollTimer = null;
    let pinchState = null;
    let viewportEl = null;
    let searchGeneration = 0;
    let findKeyHandler = null;
    let fullscreenChangeHandler = null;

    const renderedPages = new Map();
    const renderingPages = new Set();

    function getLib() {
        if (typeof pdfjsLib === 'undefined') {
            throw new Error('pdf.js não carregado');
        }
        return pdfjsLib;
    }

    async function ensureReady() {
        if (ready) return;
        const lib = getLib();
        lib.GlobalWorkerOptions.workerSrc = 'js/pdfjs/pdf.worker.min.js';
        ready = true;
    }

    function clampScale(value) {
        return Math.min(MAX_SCALE, Math.max(MIN_SCALE, value || 1.0));
    }

    function clearObserver() {
        if (observer) {
            observer.disconnect();
            observer = null;
        }
    }

    function clearContainer(containerId) {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = '';
        }
        renderedPages.clear();
        renderingPages.clear();
    }

    function createPageSlot(pageNumber) {
        const slot = document.createElement('div');
        slot.className = 'ol-pdf-page-slot';
        slot.setAttribute('data-page', String(pageNumber));
        slot.innerHTML = '<canvas aria-label="Página ' + pageNumber + '"></canvas>';
        return slot;
    }

    async function loadPdfData(data) {
        await ensureReady();
        clearObserver();
        renderedPages.clear();
        renderingPages.clear();

        if (pdfDoc) {
            try { await pdfDoc.destroy(); } catch (_) { }
            pdfDoc = null;
        }

        const lib = getLib();
        pdfDoc = await lib.getDocument({ data: data }).promise;
        return { pageCount: pdfDoc.numPages };
    }

    async function loadPdfUrl(url) {
        await ensureReady();
        clearObserver();
        renderedPages.clear();
        renderingPages.clear();

        if (pdfDoc) {
            try { await pdfDoc.destroy(); } catch (_) { }
            pdfDoc = null;
        }

        const lib = getLib();
        pdfDoc = await lib.getDocument({ url: url }).promise;
        return { pageCount: pdfDoc.numPages };
    }

    function appendRemainingPagesAsync(containerId, pageCount, targetPage) {
        const BATCH = 64;
        (async function () {
            const container = document.getElementById(containerId);
            if (!container) return;

            for (let batchStart = 1; batchStart <= pageCount; batchStart += BATCH) {
                const fragment = document.createDocumentFragment();
                let added = false;

                for (let p = batchStart; p < batchStart + BATCH && p <= pageCount; p++) {
                    if (p === targetPage) continue;
                    fragment.appendChild(createPageSlot(p));
                    added = true;
                }

                if (added) {
                    container.appendChild(fragment);
                    setupContinuousObserver(containerId);
                }

                if (batchStart + BATCH <= pageCount) {
                    await new Promise(function (resolve) { setTimeout(resolve, 0); });
                }
            }

            if (targetPage > 1) {
                window.okutangaPdfReader.scrollToPage(containerId, targetPage);
            }
        })();
    }

    async function renderToCanvas(pageNumber, canvas, renderScale) {
        const page = await pdfDoc.getPage(pageNumber);
        const viewport = page.getViewport({ scale: renderScale });
        const ctx = canvas.getContext('2d');
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        canvas.style.width = viewport.width + 'px';
        canvas.style.height = viewport.height + 'px';
        await page.render({ canvasContext: ctx, viewport: viewport }).promise;
        return { width: viewport.width, height: viewport.height };
    }

    async function renderContinuousPage(pageNumber, containerId) {
        if (!pdfDoc || renderingPages.has(pageNumber) || renderedPages.has(pageNumber)) {
            return;
        }

        renderingPages.add(pageNumber);
        try {
            const container = document.getElementById(containerId);
            if (!container) return;

            let slot = container.querySelector('[data-page="' + pageNumber + '"]');
            if (!slot) return;

            const canvas = slot.querySelector('canvas');
            if (!canvas) return;

            await renderToCanvas(pageNumber, canvas, scale);
            slot.classList.add('is-rendered');
            renderedPages.set(pageNumber, true);
        } finally {
            renderingPages.delete(pageNumber);
        }
    }

    function setupContinuousObserver(containerId) {
        clearObserver();
        const container = document.getElementById(containerId);
        if (!container) return;

        observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                const page = parseInt(entry.target.getAttribute('data-page'), 10);
                if (page > 0) {
                    renderContinuousPage(page, containerId);
                }
            });
        }, { root: viewportEl, rootMargin: '200px 0px', threshold: 0.01 });

        container.querySelectorAll('.ol-pdf-page-slot').forEach(function (slot) {
            observer.observe(slot);
        });
    }

    function notifyPageChanged(page) {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnVisiblePageChanged', page);
        }
    }

    function onViewportScroll(containerId) {
        if (mode !== 'continuous') return;
        clearTimeout(scrollTimer);
        scrollTimer = setTimeout(function () {
            const container = document.getElementById(containerId);
            if (!container || !viewportEl) return;

            const slots = container.querySelectorAll('.ol-pdf-page-slot');
            let bestPage = 1;
            let bestVisible = 0;
            const vpRect = viewportEl.getBoundingClientRect();
            const mid = vpRect.top + vpRect.height / 2;

            slots.forEach(function (slot) {
                const rect = slot.getBoundingClientRect();
                const visible = Math.min(rect.bottom, vpRect.bottom) - Math.max(rect.top, vpRect.top);
                if (visible > bestVisible) {
                    bestVisible = visible;
                    bestPage = parseInt(slot.getAttribute('data-page'), 10) || 1;
                }
            });

            notifyPageChanged(bestPage);
        }, 120);
    }

    function ensurePageSlot(containerId, pageNumber) {
        const container = document.getElementById(containerId);
        if (!container || pageNumber < 1) return null;

        let slot = container.querySelector('[data-page="' + pageNumber + '"]');
        if (slot) return slot;

        slot = createPageSlot(pageNumber);
        const slots = container.querySelectorAll('.ol-pdf-page-slot');
        let inserted = false;

        slots.forEach(function (existing) {
            if (inserted) return;
            const p = parseInt(existing.getAttribute('data-page'), 10);
            if (p > pageNumber) {
                container.insertBefore(slot, existing);
                inserted = true;
            }
        });

        if (!inserted) {
            container.appendChild(slot);
        }

        setupContinuousObserver(containerId);
        return slot;
    }

    function snippetAround(text, idx, termLen) {
        const start = Math.max(0, idx - 28);
        const end = Math.min(text.length, idx + termLen + 28);
        let snippet = text.substring(start, end).trim();
        if (start > 0) snippet = '…' + snippet;
        if (end < text.length) snippet = snippet + '…';
        return snippet;
    }

    function setupFindShortcuts() {
        if (findKeyHandler) {
            document.removeEventListener('keydown', findKeyHandler);
        }

        findKeyHandler = function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
                e.preventDefault();
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnFindShortcut');
                }
            }
        };

        document.addEventListener('keydown', findKeyHandler);
    }

    function setupFullscreenSync() {
        if (fullscreenChangeHandler) {
            document.removeEventListener('fullscreenchange', fullscreenChangeHandler);
            document.removeEventListener('webkitfullscreenchange', fullscreenChangeHandler);
        }

        fullscreenChangeHandler = function () {
            const active = !!(document.fullscreenElement || document.webkitFullscreenElement);
            if (!active && dotNetRef) {
                dotNetRef.invokeMethodAsync('OnFullscreenEnded');
            }
        };

        document.addEventListener('fullscreenchange', fullscreenChangeHandler);
        document.addEventListener('webkitfullscreenchange', fullscreenChangeHandler);
    }

    function setReaderLayoutMode(mode) {
        const root = document.getElementById('page-root');
        if (!root) return;
        root.classList.remove('layout-reader-active', 'layout-reader-immersive');
        if (mode === 'reading' || mode === 'immersive') {
            root.classList.add('layout-reader-active');
        }
        if (mode === 'immersive') {
            root.classList.add('layout-reader-immersive');
        }
    }

    const NARROW_READER_MQ = '(max-width: 768px)';
    let readerViewportMq = null;
    let readerViewportHandler = null;
    let readerViewportDotNet = null;

    function notifyReaderViewport(dotNetRef) {
        if (!dotNetRef) return;
        const narrow = window.matchMedia(NARROW_READER_MQ).matches;
        dotNetRef.invokeMethodAsync('OnReaderViewportChanged', narrow);
    }

    function setupPinchZoom(viewportId, containerId, singleCanvasId) {
        viewportEl = document.getElementById(viewportId);
        if (!viewportEl) return;

        viewportEl.addEventListener('touchstart', function (e) {
            if (e.touches.length === 2) {
                const dx = e.touches[0].clientX - e.touches[1].clientX;
                const dy = e.touches[0].clientY - e.touches[1].clientY;
                pinchState = {
                    startDist: Math.hypot(dx, dy),
                    startScale: scale,
                };
            }
        }, { passive: true });

        viewportEl.addEventListener('touchmove', function (e) {
            if (!pinchState || e.touches.length !== 2) return;
            const dx = e.touches[0].clientX - e.touches[1].clientX;
            const dy = e.touches[0].clientY - e.touches[1].clientY;
            const dist = Math.hypot(dx, dy);
            const ratio = dist / pinchState.startDist;
            const newScale = clampScale(pinchState.startScale * ratio);
            if (Math.abs(newScale - scale) > 0.05 && dotNetRef) {
                dotNetRef.invokeMethodAsync('OnPinchZoom', newScale);
            }
        }, { passive: true });

        viewportEl.addEventListener('touchend', function () {
            pinchState = null;
        }, { passive: true });

        viewportEl.addEventListener('scroll', function () {
            onViewportScroll(containerId);
        }, { passive: true });
    }

    window.okutangaPdfReader = {
        prewarm: async function () {
            await ensureReady();
        },

        init: async function (ref, viewportId, containerId, singleCanvasId) {
            await ensureReady();
            dotNetRef = ref;
            setupPinchZoom(viewportId, containerId, singleCanvasId);
            setupFindShortcuts();
            setupFullscreenSync();
            return true;
        },

        loadFromStream: async function (streamRef) {
            const data = await streamRef.arrayBuffer();
            await streamRef.dispose();
            return await loadPdfData(new Uint8Array(data));
        },

        loadFromUrl: async function (url) {
            return await loadPdfUrl(url);
        },

        setMode: function (newMode) {
            mode = newMode === 'single' ? 'single' : 'continuous';
        },

        setScale: function (newScale) {
            scale = clampScale(newScale);
            renderedPages.clear();
            renderingPages.clear();
        },

        buildContinuousLayout: async function (containerId, pageCount, startPage) {
            clearContainer(containerId);
            const container = document.getElementById(containerId);
            if (!container || !pageCount) return;

            const targetPage = Math.max(1, Math.min(startPage || 1, pageCount));

            container.appendChild(createPageSlot(targetPage));
            setupContinuousObserver(containerId);
            await renderContinuousPage(targetPage, containerId);
            notifyPageChanged(targetPage);

            if (pageCount > 1) {
                appendRemainingPagesAsync(containerId, pageCount, targetPage);
            }
        },

        rerenderContinuous: async function (containerId, startPage) {
            if (!pdfDoc) return;
            const page = Math.max(1, Math.min(startPage || 1, pdfDoc.numPages));
            clearContainer(containerId);
            await window.okutangaPdfReader.buildContinuousLayout(containerId, pdfDoc.numPages, page);
        },

        renderSinglePage: async function (pageNumber, canvasId) {
            if (!pdfDoc || pageNumber < 1 || pageNumber > pdfDoc.numPages) return null;
            const canvas = document.getElementById(canvasId);
            if (!canvas) return null;
            return await renderToCanvas(pageNumber, canvas, scale);
        },

        scrollToPage: async function (containerId, pageNumber) {
            if (!pdfDoc || pageNumber < 1 || pageNumber > pdfDoc.numPages) return;
            const slot = ensurePageSlot(containerId, pageNumber);
            if (!slot) return;

            slot.classList.add('ol-pdf-page-slot--highlight');
            setTimeout(function () {
                slot.classList.remove('ol-pdf-page-slot--highlight');
            }, 1200);

            slot.scrollIntoView({ behavior: 'smooth', block: 'start' });
            await renderContinuousPage(pageNumber, containerId);
            notifyPageChanged(pageNumber);
        },

        renderThumbnail: async function (pageNumber, canvasId) {
            if (!pdfDoc) return null;
            const canvas = document.getElementById(canvasId);
            if (!canvas) return null;
            return await renderToCanvas(pageNumber, canvas, THUMB_SCALE);
        },

        cancelSearch: function () {
            searchGeneration++;
        },

        searchAll: async function (query, maxResults) {
            if (!pdfDoc || !query || !query.trim()) return [];
            const gen = ++searchGeneration;
            const term = query.trim().toLowerCase();
            const limit = maxResults || 500;
            const results = [];
            let matchIndex = 0;

            for (let i = 1; i <= pdfDoc.numPages && results.length < limit; i++) {
                if (gen !== searchGeneration) return [];

                const page = await pdfDoc.getPage(i);
                const content = await page.getTextContent();
                const text = content.items.map(function (item) { return item.str; }).join(' ');
                const lower = text.toLowerCase();
                let from = 0;

                while (results.length < limit) {
                    const idx = lower.indexOf(term, from);
                    if (idx < 0) break;

                    results.push({
                        matchIndex: matchIndex,
                        pageNumber: i,
                        snippet: snippetAround(text, idx, term.length),
                    });
                    matchIndex++;
                    from = idx + term.length;
                }

                if (i % 8 === 0) {
                    await new Promise(function (resolve) { setTimeout(resolve, 0); });
                }
            }

            return gen === searchGeneration ? results : [];
        },

        setReaderLayout: function (mode) {
            setReaderLayoutMode(mode || 'normal');
        },

        watchReaderViewport: function (readerId, dotNetRef) {
            readerViewportDotNet = dotNetRef;
            if (readerViewportMq) {
                readerViewportMq.removeEventListener('change', readerViewportHandler);
            }
            readerViewportMq = window.matchMedia(NARROW_READER_MQ);
            readerViewportHandler = function () {
                notifyReaderViewport(readerViewportDotNet);
            };
            readerViewportMq.addEventListener('change', readerViewportHandler);
            notifyReaderViewport(readerViewportDotNet);
        },

        unwatchReaderViewport: function () {
            if (readerViewportMq && readerViewportHandler) {
                readerViewportMq.removeEventListener('change', readerViewportHandler);
            }
            readerViewportMq = null;
            readerViewportHandler = null;
            readerViewportDotNet = null;
        },

        enterFullscreen: async function (elementId) {
            const el = document.getElementById(elementId);
            if (!el) return false;
            const req = el.requestFullscreen || el.webkitRequestFullscreen || el.msRequestFullscreen;
            if (!req) return false;
            try {
                await req.call(el);
                return true;
            } catch (_) {
                return false;
            }
        },

        exitFullscreen: async function () {
            const exit = document.exitFullscreen || document.webkitExitFullscreen || document.msExitFullscreen;
            if (!exit) return false;
            try {
                if (document.fullscreenElement || document.webkitFullscreenElement) {
                    await exit.call(document);
                }
                return true;
            } catch (_) {
                return false;
            }
        },

        dispose: async function () {
            searchGeneration++;
            if (findKeyHandler) {
                document.removeEventListener('keydown', findKeyHandler);
                findKeyHandler = null;
            }
            if (fullscreenChangeHandler) {
                document.removeEventListener('fullscreenchange', fullscreenChangeHandler);
                document.removeEventListener('webkitfullscreenchange', fullscreenChangeHandler);
                fullscreenChangeHandler = null;
            }
            window.okutangaPdfReader.unwatchReaderViewport();
            setReaderLayoutMode('normal');
            clearObserver();
            dotNetRef = null;
            renderedPages.clear();
            renderingPages.clear();
            if (pdfDoc) {
                try { await pdfDoc.destroy(); } catch (_) { }
                pdfDoc = null;
            }
        },
    };
})();
