/**
 * hero-video.js
 * 5-slot hero video rotation with smooth crossfade.
 *
 * Blazor interop entry points:
 *   window.initHeroVideo()    – (re-)start the rotation; safe to call on every render
 *   window.disposeHeroVideo() – clear all timers; call when navigating away
 *
 * To add videos:
 *   1. Drop hero-N.mp4 into /videos/hero/
 *   2. Add / uncomment the entry in SLOTS below.
 */
(function () {
    'use strict';

    // ── Config ────────────────────────────────────────────────────────────────
    var SLOTS = [
        { src: '/videos/hero/hero-1.mp4', hold: 10000 },   // 10 s
        { src: '/videos/hero/hero-2.mp4', hold:  5000 },   //  5 s
        { src: '/videos/hero/hero-3.mp4', hold:  5000 },   //  5 s
        { src: '/videos/hero/hero-4.mp4', hold:  5000 },   //  5 s
        // { src: '/videos/hero/hero-5.mp4', hold: 6000 },
    ];
    var FADE_MS  = 1500;  // must match CSS transition-duration
    var MIN_HOLD = 2500;  // safety floor

    // ── Dispose ───────────────────────────────────────────────────────────────
    // Clears any running timers from a previous init call.
    window.disposeHeroVideo = function disposeHeroVideo() {
        var t = window.__heroVideoTimers;
        if (!t) return;
        clearTimeout(t.swapTimer);
        clearTimeout(t.preloadTimer);
        window.__heroVideoTimers = null;
    };

    // ── Init ──────────────────────────────────────────────────────────────────
    // Safe to call on every Blazor render. Disposes any prior run first.
    // DOM guard: if heroVid0 is not yet in the document (Blazor hasn't painted
    // the component yet), bail out silently — Blazor will call us again on the
    // next render cycle once the DOM is ready.
    window.initHeroVideo = function initHeroVideo() {

        // Always clean up before re-binding
        window.disposeHeroVideo();

        // Reduced-motion guard
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

        // ── DOM-ready guard ───────────────────────────────────────────────────
        // If Blazor called us before the video elements exist in the DOM, return
        // immediately. The Task.Delay on the C# side plus Blazor's render cycle
        // means the next OnAfterRenderAsync call will succeed.
        var videoEls = [
            document.getElementById('heroVid0'),
            document.getElementById('heroVid1'),
            document.getElementById('heroVid2'),
            document.getElementById('heroVid3'),
            document.getElementById('heroVid4'),
        ].filter(Boolean);

        if (!videoEls.length) return;

        // Hard-reset any inline style that a previous render or browser quirk
        // may have applied, forcing the CSS stacking rules to take full control.
        videoEls.forEach(function (el) {
            el.style.position = 'absolute';
            el.style.top      = '0';
            el.style.left     = '0';
            el.style.width    = '100%';
            el.style.height   = '100%';
            el.style.margin   = '0';
            el.style.padding  = '0';
        });

        // Reset any leftover visual state from a previous run (opacity/z-index)
        videoEls.forEach(function (el) {
            el.pause();
            el.classList.remove('active');
            el.style.zIndex = '1';
        });

        // Build active slot list
        var activeSlots = SLOTS
            .filter(function (s) { return s && s.src; })
            .map(function (cfg, i) {
                var el = videoEls[i];
                if (!el) return null;
                el.src     = cfg.src;
                el.loop    = false;
                el.preload = 'auto';
                return { el: el, hold: Math.max(cfg.hold, MIN_HOLD) };
            })
            .filter(Boolean);

        if (!activeSlots.length) return;

        // ── Single video: loop ────────────────────────────────────────────────
        if (activeSlots.length === 1) {
            activeSlots[0].el.loop = true;
            activeSlots[0].el.style.zIndex = '10';
            activeSlots[0].el.classList.add('active');
            activeSlots[0].el.currentTime = 0.1;
            activeSlots[0].el.play().catch(function () {});
            return;
        }

        // All start stacked at z-index 1
        activeSlots.forEach(function (slot) {
            slot.el.style.zIndex = '1';
        });

        var current = 0;

        // Timer references stored on window so disposeHeroVideo can reach them
        window.__heroVideoTimers = { swapTimer: null, preloadTimer: null };

        function nextIdx(idx) { return (idx + 1) % activeSlots.length; }

        function ensureReady(slot) {
            if (slot.el.readyState >= 3) return;
            slot.el.load();
        }

        function crossfadeTo(inIdx) {
            var outSlot = activeSlots[current];
            var inSlot  = activeSlots[inIdx];

            inSlot.el.style.zIndex = '10';
            inSlot.el.currentTime  = 0.1;

            setTimeout(function () {
                inSlot.el.play().catch(function () {});
                inSlot.el.classList.add('active');

                setTimeout(function () {
                    outSlot.el.classList.remove('active');
                    outSlot.el.style.zIndex = '1';
                }, 100);

                setTimeout(function () {
                    outSlot.el.pause();
                    outSlot.el.currentTime = 0;
                    if (outSlot === activeSlots[0]) {
                        ensureReady(activeSlots[0]);
                    }
                }, FADE_MS + 100);

            }, 50);

            current = inIdx;

            if (inIdx === activeSlots.length - 1) {
                ensureReady(activeSlots[0]);
            }
        }

        function scheduleSwap() {
            var t = window.__heroVideoTimers;
            if (!t) return;  // disposed while waiting

            var slot   = activeSlots[current];
            var holdMs = slot.hold;
            var nIdx   = nextIdx(current);

            var preloadDelay = Math.max(holdMs - FADE_MS - 500, 0);
            t.preloadTimer = setTimeout(function () {
                ensureReady(activeSlots[nIdx]);
            }, preloadDelay);

            var swapDelay = Math.max(holdMs - FADE_MS, MIN_HOLD);
            t.swapTimer = setTimeout(function () {
                crossfadeTo(nIdx);
                scheduleSwap();
            }, swapDelay);
        }

        // ── Fallback: fire crossfade if video ends before timer ───────────────
        activeSlots.forEach(function (slot, idx) {
            slot.el.addEventListener('ended', function () {
                if (idx !== current) return;
                var t = window.__heroVideoTimers;
                if (t) {
                    clearTimeout(t.swapTimer);
                    clearTimeout(t.preloadTimer);
                }
                crossfadeTo(nextIdx(current));
                scheduleSwap();
            });
        });

        // ── Kick off ──────────────────────────────────────────────────────────
        activeSlots[0].el.style.zIndex = '10';
        activeSlots[0].el.classList.add('active');
        activeSlots[0].el.currentTime = 0.1;
        activeSlots[0].el.play().catch(function () {});

        for (var i = 1; i < activeSlots.length; i++) {
            ensureReady(activeSlots[i]);
        }

        scheduleSwap();
    };

}());
