/**
 * gym-network.js
 * Interactive hover panel for the gym partner section.
 *
 * Blazor interop entry points:
 *   window.initGymPanel() – initialize the hover interactions; safe to call after render
 */
(function () {
    'use strict';

    window.initGymPanel = function initGymPanel() {
        var panel   = document.getElementById('gnPanel');
        var segWrap = document.getElementById('gnSegments');
        if (!panel || !segWrap) return;

        var imgs    = panel.querySelectorAll('.gn-img');
        var segs    = segWrap.querySelectorAll('.gn-seg');
        var current = 1;

        function activate(i) {
            imgs.forEach(function (img, idx) {
                img.classList.toggle('is-active', idx === i);
            });
            segs.forEach(function (seg, idx) {
                seg.classList.toggle('is-active', idx === i);
            });
            current = i;
        }

        segs.forEach(function (seg) {
            seg.addEventListener('mouseenter', function () {
                segWrap.classList.add('has-hover');
                activate(parseInt(seg.dataset.idx, 10));
            });
        });

        segWrap.addEventListener('mouseleave', function () {
            segWrap.classList.remove('has-hover');
        });
    };

}());
