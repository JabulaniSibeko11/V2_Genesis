/*
 * text-flow-fix.js
 * ---------------------------------------------------------------------------
 * Keeps sentences readable across the whole portal.
 *
 * Many notice boxes are flex rows:  <div class="note"> <i/> Text <strong>bold</strong> more text </div>
 * In a flex row every piece (the text, the <strong>, the <a>) becomes its
 * own column, so a sentence is broken into narrow columns
 * ("Please allow up to | 5 working days | for a response").
 *
 * This script finds such boxes and puts the sentence (text + inline tags)
 * into ONE <span class="gs-flow">, next to the icon. The sentence then reads
 * normally; bold words and links stay bold / clickable.
 *
 * Loaded by _Layout, _Legend_Layout and _ClientLayout. Runs on page load and
 * again when content is added later (modals, AJAX partials).
 * ---------------------------------------------------------------------------
 */
(function () {
    'use strict';

    var INLINE = {
        STRONG: 1, B: 1, EM: 1, I: 1, A: 1, SPAN: 1, SMALL: 1, CODE: 1,
        U: 1, MARK: 1, ABBR: 1, TIME: 1, SUP: 1, SUB: 1, BR: 1
    };

    function isIcon(node) {
        if (node.nodeType !== 1) return false;
        var tag = node.tagName;
        if (tag === 'SVG' || tag === 'IMG') return true;
        if (tag === 'I' || tag === 'SPAN') {
            var cls = (node.className && node.className.baseVal !== undefined)
                ? node.className.baseVal : (node.className || '');
            return /\bfa[srlbd]?\b|\bfa-|\bbi\b|\bbi-|icon/.test(cls) && !node.textContent.trim();
        }
        return false;
    }

    function isInlineText(node) {
        if (node.nodeType === 3) return true;
        if (node.nodeType !== 1) return false;
        if (isIcon(node)) return false;
        if (!INLINE[node.tagName]) return false;
        // Inside a flex row every child is shown as a block, so the computed
        // display cannot be used. Buttons / badges / chips are left alone.
        var cls = typeof node.className === 'string' ? node.className : '';
        return !/(^|[\s-])(btn|button|badge|chip|pill|tag|status)([\s-]|$)/i.test(cls);
    }

    function needsFix(el) {
        if (el.dataset.gsFlow === 'done') return false;
        var style = getComputedStyle(el);
        if (style.display !== 'flex' && style.display !== 'inline-flex') return false;
        if (style.flexDirection.indexOf('column') === 0) return false;

        var hasText = false;
        var hasInline = false;
        for (var i = 0; i < el.childNodes.length; i++) {
            var n = el.childNodes[i];
            if (n.nodeType === 3 && n.textContent.trim()) hasText = true;
            else if (n.nodeType === 1 && !isIcon(n) && isInlineText(n) && n.textContent.trim()) hasInline = true;
        }
        // Only a sentence with bold / link parts breaks; plain "icon + text" is fine.
        return hasText && hasInline;
    }

    function fix(el) {
        var nodes = Array.prototype.slice.call(el.childNodes);
        var run = [];

        function flush() {
            var meaningful = run.some(function (n) {
                return n.nodeType === 1 || n.textContent.trim();
            });
            if (run.length && meaningful) {
                var span = document.createElement('span');
                span.className = 'gs-flow';
                el.insertBefore(span, run[0]);
                run.forEach(function (n) { span.appendChild(n); });
            }
            run = [];
        }

        nodes.forEach(function (n) {
            if (isInlineText(n)) run.push(n);
            else flush();
        });
        flush();

        el.dataset.gsFlow = 'done';
    }

    function scan(root) {
        var scope = root && root.querySelectorAll ? root : document;
        var all = scope.querySelectorAll('div, p, li, span, label, small, section, aside, article');
        for (var i = 0; i < all.length; i++) {
            var el = all[i];
            try {
                if (needsFix(el)) fix(el);
            } catch (e) { /* never break the page */ }
        }
    }

    function addStyle() {
        if (document.getElementById('gs-flow-style')) return;
        var css = document.createElement('style');
        css.id = 'gs-flow-style';
        css.textContent =
            '.gs-flow{display:block;flex:1 1 auto;min-width:0;white-space:normal;}' +
            '.gs-flow a,.gs-flow strong,.gs-flow b{display:inline;white-space:normal;}';
        document.head.appendChild(css);
    }

    function start() {
        addStyle();
        scan(document);

        if (window.MutationObserver) {
            var pending = null;
            new MutationObserver(function (mutations) {
                if (pending) return;
                pending = setTimeout(function () {
                    pending = null;
                    mutations.forEach(function (m) {
                        m.addedNodes.forEach(function (n) {
                            if (n.nodeType === 1 && !(n.classList && n.classList.contains('gs-flow'))) scan(n.parentNode || n);
                        });
                    });
                }, 60);
            }).observe(document.body, { childList: true, subtree: true });
        }
    }

    window.GenesisTextFlow = { scan: scan };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
