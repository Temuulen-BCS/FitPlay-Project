/**
 * gym-network.js
 * Interactive panel for the gym partner section.
 *
 * Blazor interop entry points:
 *   window.initGymPanel()    initialize interactions after render
 *   window.disposeGymPanel() remove listeners/timers on teardown
 */
(function () {
    'use strict';

    window.disposeGymPanel = function disposeGymPanel() {
        var state = window.__gymPanelState;
        if (!state) return;

        if (state.controller) {
            state.controller.abort();
        }

        window.__gymPanelState = null;
    };

    window.initGymPanel = function initGymPanel() {
        // Keep init idempotent if Blazor re-renders this page.
        window.disposeGymPanel();

        var panel = document.getElementById('gnPanel');
        var segWrap = document.getElementById('gnSegments');
        if (!panel || !segWrap) return;

        var imgs = panel.querySelectorAll('.gn-img');
        var segs = segWrap.querySelectorAll('.gn-seg');
        if (!imgs.length || !segs.length) return;

        var activeSeg = segWrap.querySelector('.gn-seg.is-active');
        var current = activeSeg ? Array.prototype.indexOf.call(segs, activeSeg) : 1;
        if (current < 0) current = 0;

        function activate(i) {
            if (i < 0 || i >= segs.length) return;

            imgs.forEach(function (img, idx) {
                img.classList.toggle('is-active', idx === i);
            });

            segs.forEach(function (seg, idx) {
                seg.classList.toggle('is-active', idx === i);
            });

            current = i;
        }

        var controller = typeof AbortController !== 'undefined' ? new AbortController() : null;
        var opts = controller ? { signal: controller.signal } : undefined;

        function parseIdx(seg) {
            var value = parseInt(seg.dataset.idx, 10);
            return Number.isNaN(value) ? current : value;
        }

        segs.forEach(function (seg) {
            seg.addEventListener('mouseenter', function () {
                segWrap.classList.add('has-hover');
                activate(parseIdx(seg));
            }, opts);

            seg.addEventListener('focusin', function () {
                activate(parseIdx(seg));
            }, opts);

            seg.addEventListener('click', function () {
                activate(parseIdx(seg));
            }, opts);
        });

        segWrap.addEventListener('mouseleave', function () {
            segWrap.classList.remove('has-hover');
        }, opts);

        activate(current);
        window.__gymPanelState = { controller: controller };
    };
}());