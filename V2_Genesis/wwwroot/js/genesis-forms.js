/*
 * genesis-forms.js
 * ---------------------------------------------------------------------------
 * Shared behaviour for the Objection, Appeal, Multipurpose and Section 78
 * Query / Review forms. Load it AFTER the form's own script
 * (ObjectionJS / ObjectionMulti / ObjectionQJS / ObjectionMulti_Section78)
 * and after sa-id-validation.js. Styles: ~/css/Views/genesis-forms.css
 *
 * 1. Yes / No questions - each question is shown as ONE bordered box:
 *
 *        ┌────────────────────────────────────────────────┐
 *        │ KITCHEN   ◉ Yes   ◉ No                         │
 *        └────────────────────────────────────────────────┘
 *
 *      fieldset.yn-q      a question with radio options
 *      fieldset.yn-done   an option has been chosen (box turns green)
 *      span.yn-opts       the options of one question, kept together
 *      label.yn-opt       one option (circle + word)
 *      label.yn-on        the chosen option
 *      .yn-row / .yn-wrap rows that hold several questions side by side
 *
 * 2. Next / Previous - after every step change the page scrolls to the top
 *    of the step that is now showing, so the client never lands in the
 *    middle (or the bottom) of the new section.
 *
 * 3. Compensation amount (Section 2) - the amount is shown with thousand
 *    separators while the plain number is posted in #Obj_Compensation_Amount.
 *
 * 4. "1.1" is never shown twice when a script renames the 1.1 bar.
 * ---------------------------------------------------------------------------
 */
(function () {
    'use strict';

    var form = document.getElementById('myForm');

    // ── Shared helper used by the Next / Previous handlers ──────────
    if (typeof window.focusIfExists !== 'function') {
        window.focusIfExists = function (id) {
            var el = document.getElementById(id);
            if (el && typeof el.focus === 'function') el.focus({ preventScroll: true });
        };
    }

    // ════════════════════════════════════════════════════════════════
    // 1. YES / NO QUESTIONS
    // ════════════════════════════════════════════════════════════════
    var SKIP = '.obj-section-bar, .obj-green-bar';

    function isQuestion(fs) {
        if (!fs.querySelector('input[type="radio"]')) return false;
        if (fs.closest(SKIP)) return false;
        // Only simple questions: radios, their labels and an optional list.
        if (fs.querySelector('input[type="text"], input[type="number"], input[type="email"], input[type="tel"], input[type="date"], textarea')) return false;
        return true;
    }

    function elementChildren(el) {
        return Array.prototype.filter.call(el.children, function (c) {
            return c.tagName !== 'SCRIPT' && c.tagName !== 'BR';
        });
    }

    function refresh(fs) {
        var any = false;
        fs.querySelectorAll('label.yn-opt').forEach(function (label) {
            var radio = label.querySelector('input[type="radio"]');
            var on = !!(radio && radio.checked);
            label.classList.toggle('yn-on', on);
            if (on) any = true;
        });
        fs.classList.toggle('yn-done', any);
    }

    function refreshAll() {
        document.querySelectorAll('#myForm fieldset.yn-q').forEach(refresh);
    }

    function tag() {
        var questions = [];

        document.querySelectorAll('#myForm fieldset').forEach(function (fs) {
            if (!isQuestion(fs) || fs.classList.contains('yn-q')) return;

            fs.classList.add('yn-q');
            questions.push(fs);

            fs.querySelectorAll('input[type="radio"]').forEach(function (radio) {
                var label = radio.closest('label');
                if (label && fs.contains(label)) label.classList.add('yn-opt');
            });

            // Keep the options together (Yes and No never split over two
            // lines): move the option holders into one <span class="yn-opts">.
            var holders = Array.prototype.filter.call(fs.children, function (c) {
                return c.querySelector('input[type="radio"]') ||
                    (c.tagName === 'INPUT' && c.type === 'radio');
            });
            if (holders.length) {
                var group = document.createElement('span');
                group.className = 'yn-opts';
                fs.insertBefore(group, holders[0]);
                holders.forEach(function (h) { group.appendChild(h); });
            }

            refresh(fs);
        });

        questions.forEach(function (fs) {
            var holder = fs.parentElement;
            var row = holder;

            // <div class="d-flex col-md-4"><fieldset/></div> inside a row
            if (holder && holder !== form &&
                elementChildren(holder).length === 1 &&
                holder.parentElement && holder.parentElement !== form &&
                holder.classList.contains('d-flex')) {
                holder.classList.add('yn-wrap');
                row = holder.parentElement;
            }

            if (!row || row === form) return;

            var items = elementChildren(row);
            var allQuestions = items.every(function (c) {
                return c.classList.contains('yn-q') || c.classList.contains('yn-wrap');
            });

            if (allQuestions) row.classList.add('yn-row');
        });
    }

    // Keep the green "answered" look in step with the radios.
    document.addEventListener('change', function (e) {
        var t = e.target;
        if (t && t.type === 'radio' && form && form.contains(t)) refreshAll();
    });

    // Radios set by script (restore / reset) do not fire "change".
    document.addEventListener('click', function (e) {
        if (e.target && e.target.closest && e.target.closest('#myForm')) {
            setTimeout(refreshAll, 0);
        }
    });

    // ════════════════════════════════════════════════════════════════
    // 2. NEXT / PREVIOUS: SHOW THE TOP OF THE NEW STEP
    // ════════════════════════════════════════════════════════════════
    function visibleSteps() {
        return Array.prototype.filter.call(
            document.querySelectorAll('#myForm .obj-form-card'),
            function (card) { return card.offsetParent !== null; });
    }

    document.addEventListener('click', function (e) {
        var btn = e.target && e.target.closest
            ? e.target.closest('#myForm button[class*="btn_n"], #myForm button[class*="btn_p"], #myForm .obj-nav-btn')
            : null;
        if (!btn || btn.type === 'submit' || btn.id === 'submitForm') return;

        var before = visibleSteps();

        // Run after the form script has swapped the steps (and focused a field).
        setTimeout(function () {
            var after = visibleSteps();
            var changed = after.length !== before.length ||
                after.some(function (card, i) { return card !== before[i]; });
            if (!changed) return; // validation kept the client on this step

            var step = after.filter(function (card) { return before.indexOf(card) === -1; })[0] || after[0];
            if (!step) return;

            var nav = document.querySelector('.cl-navbar, .navbar.fixed-top, header.sticky-top');
            var fixedNav = nav && /fixed|sticky/.test(getComputedStyle(nav).position);
            var offset = fixedNav ? nav.getBoundingClientRect().height + 12 : 16;
            var top = step.getBoundingClientRect().top + window.pageYOffset - offset;
            window.scrollTo({ top: Math.max(top, 0), behavior: 'smooth' });
        }, 80);
    });

    // ════════════════════════════════════════════════════════════════
    // 3. COMPENSATION AMOUNT (Section 2)
    // ════════════════════════════════════════════════════════════════
    // Replaces the old reformatText(), which wrote to a field that did not
    // exist and put "12,000" (with commas) in the posted field.
    window.reformatText = function (input) {
        if (!input) return;
        var digits = String(input.value || '').replace(/[^\d]/g, '').replace(/^0+(?=\d)/, '');
        input.value = digits ? digits.replace(/\B(?=(\d{3})+(?!\d))/g, ' ') : '';

        var posted = document.getElementById('Obj_Compensation_Amount');
        if (posted) posted.value = digits;
    };

    // ════════════════════════════════════════════════════════════════
    // 4. NEVER SHOW "1.1 1.1"
    // ════════════════════════════════════════════════════════════════
    // Some scripts set the 1.1 bar text to "1.1 OWNER DETAILS" while the bar
    // already has a "1.1" number badge in front of it.
    function tidyOwnerHead() {
        var head = document.getElementById('owner_head');
        if (!head) return;
        var badge = head.previousElementSibling;
        if (badge && badge.classList.contains('obj-bar-num')) {
            var text = head.textContent;
            var clean = text.replace(/^\s*1\.1\s+/, '');
            if (clean !== text) head.textContent = clean;
        }
    }

    function watchOwnerHead() {
        var head = document.getElementById('owner_head');
        if (!head || !window.MutationObserver) return;
        new MutationObserver(tidyOwnerHead).observe(head, { childList: true, characterData: true, subtree: true });
        tidyOwnerHead();
    }

    // ── Start ────────────────────────────────────────────────────────
    function init() {
        form = form || document.getElementById('myForm');
        tag();
        watchOwnerHead();
    }

    window.GenesisForms = { refresh: function () { tag(); refreshAll(); } };
    window.GenesisYesNo = window.GenesisForms; // older name

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
    window.addEventListener('load', refreshAll);
})();
