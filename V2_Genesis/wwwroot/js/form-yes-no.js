/*
 * form-yes-no.js
 * ---------------------------------------------------------------------------
 * Marks every Yes/No (and Good/Average/Poor) question on the Objection,
 * Appeal, Multipurpose and Section 78 forms so that ~/css/Views/form-yes-no.css
 * can show each question as ONE bordered box:
 *
 *     ┌────────────────────────────────────────────────┐
 *     │ KITCHEN                     ◉ Yes      ◉ No    │
 *     └────────────────────────────────────────────────┘
 *
 *   fieldset.yn-q      a question with radio options
 *   fieldset.yn-full   the only question on its row (uses the full width)
 *   fieldset.yn-done   an option has been chosen (box turns green)
 *   label.yn-opt       one option (circle + word)
 *   label.yn-on        the chosen option
 *   .yn-row / .yn-wrap the rows / wrappers that hold the questions
 *
 * Questions inside the teal / green section bars (.obj-section-bar,
 * .obj-green-bar) are skipped: they keep their existing look.
 * ---------------------------------------------------------------------------
 */
(function () {
    'use strict';

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

    function tag(root) {
        var questions = [];

        (root || document).querySelectorAll('#myForm fieldset').forEach(function (fs) {
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
            if (holder && holder.id !== 'myForm' &&
                elementChildren(holder).length === 1 &&
                holder.parentElement && holder.parentElement.id !== 'myForm' &&
                holder.classList.contains('d-flex')) {
                holder.classList.add('yn-wrap');
                row = holder.parentElement;
            }

            if (!row || row.id === 'myForm') return;

            var items = elementChildren(row);
            var allQuestions = items.every(function (c) {
                return c.classList.contains('yn-q') || c.classList.contains('yn-wrap');
            });

            if (allQuestions) {
                row.classList.add('yn-row');
                if (items.length === 1) fs.classList.add('yn-full');
            }
        });
    }

    // Keep the green "answered" look in step with the radios.
    document.addEventListener('change', function (e) {
        var t = e.target;
        if (!t || t.type !== 'radio') return;
        var form = document.getElementById('myForm');
        if (!form || !form.contains(t)) return;

        // A radio group can span several boxes (same name) — refresh them all.
        form.querySelectorAll('fieldset.yn-q').forEach(refresh);
    });

    // Radios set by script (restore / reset) do not fire "change".
    document.addEventListener('click', function (e) {
        if (e.target && e.target.closest && e.target.closest('#myForm')) {
            setTimeout(function () {
                document.querySelectorAll('#myForm fieldset.yn-q').forEach(refresh);
            }, 0);
        }
    });

    window.GenesisYesNo = { refresh: function () { tag(); document.querySelectorAll('#myForm fieldset.yn-q').forEach(refresh); } };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { tag(); });
    } else {
        tag();
    }
    window.addEventListener('load', function () {
        document.querySelectorAll('#myForm fieldset.yn-q').forEach(refresh);
    });
})();
