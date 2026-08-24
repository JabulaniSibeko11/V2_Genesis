// ═══════════════════════════════════════════════════════════════════════
// Escapes Bootstrap dropdowns from clipping inside scrollable table
// containers (e.g. .table-responsive with overflow-x: auto).
//
// A dropdown-menu positioned with Bootstrap's default `absolute` strategy
// gets visually cut off by any ancestor with overflow set — which every
// data table on this site has, since that's what makes them scroll
// horizontally on smaller screens. Switching the open menu to `fixed`
// positioning, calculated from the toggle button's own screen position,
// makes it render relative to the viewport instead, so it always escapes
// the table regardless of scroll position.
//
// Uses show.bs.dropdown / hide.bs.dropdown (native events Bootstrap 5
// dispatches on the toggle's closest .dropdown), so this works for every
// dropdown on every page — including ones DataTables recreates on redraw —
// without needing to touch any individual view's markup.
// ═══════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    function positionMenu(toggle, menu) {
        var rect = toggle.getBoundingClientRect();
        var menuWidth = menu.offsetWidth || 180;
        var viewportWidth = document.documentElement.clientWidth;
        var viewportHeight = document.documentElement.clientHeight;

        // Prefer opening below the toggle; flip above if there is not
        // enough room beneath it.
        var spaceBelow = viewportHeight - rect.bottom;
        var openUpward = spaceBelow < 220 && rect.top > spaceBelow;

        menu.style.position = 'fixed';
        menu.style.top = openUpward
            ? 'auto'
            : (rect.bottom + 2) + 'px';
        menu.style.bottom = openUpward
            ? (viewportHeight - rect.top + 2) + 'px'
            : 'auto';

        // Align right edge of menu with right edge of toggle if it would
        // otherwise overflow off the right side of the viewport; the same
        // logic Bootstrap uses for dropdown-menu-end.
        var alignEnd = menu.classList.contains('dropdown-menu-end') ||
            (rect.left + menuWidth > viewportWidth);

        if (alignEnd) {
            menu.style.left = 'auto';
            menu.style.right = Math.max(4, viewportWidth - rect.right) + 'px';
        } else {
            menu.style.left = rect.left + 'px';
            menu.style.right = 'auto';
        }

        menu.style.zIndex = 3000;
    }

    function resetMenu(menu) {
        menu.style.position = '';
        menu.style.top = '';
        menu.style.bottom = '';
        menu.style.left = '';
        menu.style.right = '';
        menu.style.zIndex = '';
    }

    document.addEventListener('show.bs.dropdown', function (event) {
        var toggle = event.target;
        var menu = toggle.parentElement
            ? toggle.parentElement.querySelector('.dropdown-menu')
            : null;

        if (!toggle || !menu) return;

        // Position once now, and again on the next frame once Bootstrap
        // has finished its own layout pass, so our fixed coordinates are
        // the ones that stick.
        positionMenu(toggle, menu);
        requestAnimationFrame(function () { positionMenu(toggle, menu); });
    });

    document.addEventListener('hide.bs.dropdown', function (event) {
        var toggle = event.target;
        var menu = toggle.parentElement
            ? toggle.parentElement.querySelector('.dropdown-menu')
            : null;

        if (menu) resetMenu(menu);
    });

    // Keep an open dropdown correctly positioned if the table scrolls
    // horizontally/vertically while the menu is open.
    document.addEventListener('scroll', function (event) {
        var openMenu = document.querySelector('.dropdown-menu.show');
        if (!openMenu) return;

        var container = event.target;
        if (container && typeof container.contains === 'function' &&
            !container.contains(openMenu) && container !== document && container !== window) {
            var toggle = openMenu.parentElement
                ? openMenu.parentElement.querySelector('[data-bs-toggle="dropdown"]')
                : null;
            if (toggle) positionMenu(toggle, openMenu);
        }
    }, true);
})();