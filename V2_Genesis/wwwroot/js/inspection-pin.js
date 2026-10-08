/*
 * inspection-pin.js
 * ---------------------------------------------------------------------------
 * The inspection PIN modal (Views/Shared/Attributes/_InspectionPinModal.cshtml).
 *
 *  • 4 PIN boxes; typing a digit moves to the next box, Backspace goes back,
 *    pasting "1234" fills all four. Only digits are accepted.
 *  • The phone shows its own number keyboard (inputmode="numeric").
 *  • The 4 digits are copied into the hidden posted field
 *    #pinHiddenInput_{key}; the button unlocks when all 4 are filled.
 *
 * API (kept for the existing pages):
 *    GsPin.open(key)  GsPin.close(key)  GsPin.submit(key, form)
 *    window.openPinModal / closePinModal / submitInspectionPin
 * Safe to include more than once.
 * ---------------------------------------------------------------------------
 */
(function () {
    'use strict';

    if (window.GsPin) return;

    var LEN = 4;

    function els(key) {
        return {
            modal: document.getElementById('pinModal_' + key),
            hidden: document.getElementById('pinHiddenInput_' + key),
            submit: document.getElementById('pinSubmitBtn_' + key),
            error: document.getElementById('pinError_' + key),
            errorMsg: document.getElementById('pinErrorMsg_' + key),
            boxes: Array.prototype.slice.call(
                document.querySelectorAll('[data-pin-boxes="' + key + '"] .ipin-box'))
        };
    }

    function keyOf(el) {
        var group = el && el.closest ? el.closest('[data-pin-boxes]') : null;
        return group ? group.getAttribute('data-pin-boxes') : null;
    }

    function sync(key) {
        var e = els(key);
        var value = e.boxes.map(function (b) { return b.value; }).join('');
        if (e.hidden) e.hidden.value = value;
        if (e.submit) e.submit.disabled = !/^\d{4}$/.test(value);
        e.boxes.forEach(function (b) { b.classList.toggle('filled', !!b.value); });
        return value;
    }

    function hideError(key) {
        var e = els(key);
        if (e.error) e.error.classList.remove('show');
    }

    function fill(key, digits, from) {
        var e = els(key);
        var i = from || 0;
        digits.split('').forEach(function (d) {
            if (i < e.boxes.length) e.boxes[i++].value = d;
        });
        sync(key);
        var next = e.boxes[Math.min(i, e.boxes.length - 1)];
        if (next) next.focus();
    }

    function clear(key) {
        var e = els(key);
        e.boxes.forEach(function (b) { b.value = ''; });
        sync(key);
    }

    // ── typing ──────────────────────────────────────────────────────
    document.addEventListener('input', function (ev) {
        var box = ev.target;
        if (!box.classList || !box.classList.contains('ipin-box')) return;
        var key = keyOf(box);
        if (!key) return;

        var digits = (box.value || '').replace(/\D/g, '');
        var e = els(key);
        var index = e.boxes.indexOf(box);

        if (digits.length > 1) {          // autofill / fast typing
            box.value = '';
            fill(key, digits.slice(0, LEN - index), index);
        } else {
            box.value = digits;
            sync(key);
            if (digits && e.boxes[index + 1]) e.boxes[index + 1].focus();
        }
        hideError(key);
    });

    document.addEventListener('keydown', function (ev) {
        var box = ev.target;
        if (!box.classList || !box.classList.contains('ipin-box')) return;
        var key = keyOf(box);
        var e = els(key);
        var index = e.boxes.indexOf(box);

        if (ev.key === 'Backspace' && !box.value && index > 0) {
            ev.preventDefault();
            e.boxes[index - 1].value = '';
            e.boxes[index - 1].focus();
            sync(key);
        } else if (ev.key === 'ArrowLeft' && index > 0) {
            ev.preventDefault();
            e.boxes[index - 1].focus();
        } else if (ev.key === 'ArrowRight' && index < e.boxes.length - 1) {
            ev.preventDefault();
            e.boxes[index + 1].focus();
        } else if (ev.key === 'Enter') {
            if (/^\d{4}$/.test(sync(key))) {
                ev.preventDefault();
                var form = box.form;
                if (form && GsPin.submit(key, form)) form.submit();
            }
        } else if (ev.key.length === 1 && !/\d/.test(ev.key) && !ev.ctrlKey && !ev.metaKey) {
            ev.preventDefault();       // letters / symbols are ignored
        }
    });

    document.addEventListener('paste', function (ev) {
        var box = ev.target;
        if (!box.classList || !box.classList.contains('ipin-box')) return;
        var key = keyOf(box);
        var text = (ev.clipboardData || window.clipboardData).getData('text') || '';
        var digits = text.replace(/\D/g, '').slice(0, LEN);
        ev.preventDefault();
        if (!digits) return;
        clear(key);
        fill(key, digits, 0);
        hideError(key);
    });

    document.addEventListener('focusin', function (ev) {
        var box = ev.target;
        if (box.classList && box.classList.contains('ipin-box')) {
            // Always type into the first empty box.
            var e = els(keyOf(box));
            var firstEmpty = e.boxes.filter(function (b) { return !b.value; })[0];
            if (firstEmpty && e.boxes.indexOf(firstEmpty) < e.boxes.indexOf(box)) firstEmpty.focus();
            else if (box.select) box.select();
        }
    });

    // Click on the dark background closes; Escape closes.
    document.addEventListener('click', function (ev) {
        var bd = ev.target;
        if (bd.classList && bd.classList.contains('ipin-backdrop')) {
            GsPin.close(bd.getAttribute('data-pin-instance'));
        }
    });

    document.addEventListener('keydown', function (ev) {
        if (ev.key !== 'Escape') return;
        var open = document.querySelector('.ipin-backdrop.open');
        if (open) GsPin.close(open.getAttribute('data-pin-instance'));
    });

    // ── API ─────────────────────────────────────────────────────────
    var GsPin = {
        open: function (key) {
            var e = els(key);
            if (!e.modal) return false;
            clear(key);
            hideError(key);
            e.modal.style.display = 'flex';
            e.modal.classList.add('open');
            e.modal.setAttribute('aria-hidden', 'false');
            document.body.classList.add('ipin-lock');
            setTimeout(function () { if (e.boxes[0]) e.boxes[0].focus({ preventScroll: true }); }, 120);
            return true;
        },

        close: function (key) {
            var e = els(key);
            if (!e.modal) return false;
            clear(key);
            e.modal.classList.remove('open');
            e.modal.style.display = 'none';
            e.modal.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('ipin-lock');
            document.body.style.overflow = '';
            return true;
        },

        submit: function (key, form) {
            var e = els(key);
            var value = sync(key);
            if (!/^\d{4}$/.test(value)) {
                if (e.error) e.error.classList.add('show');
                if (e.errorMsg) e.errorMsg.textContent = 'Please enter all 4 digits of the inspection PIN.';
                var empty = e.boxes.filter(function (b) { return !b.value; })[0];
                if (empty) empty.focus();
                return false;
            }
            if (!form || !form.getAttribute('action')) return false;
            if (e.submit) {
                e.submit.disabled = true;
                e.submit.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Checking PIN…';
            }
            return true;
        }
    };

    window.GsPin = GsPin;
    window.openPinModal = GsPin.open;
    window.closePinModal = GsPin.close;
    window.submitInspectionPin = GsPin.submit;

    // A modal rendered open (AutoOpen) gets focus and locks the page scroll.
    function init() {
        // Put the modal directly in <body>, so no page container (scrolling,
        // transforms) can push it below the visible screen.
        document.querySelectorAll('.ipin-backdrop').forEach(function (m) {
            if (m.parentNode !== document.body) document.body.appendChild(m);
        });

        document.querySelectorAll('.ipin-backdrop.open').forEach(function (m) {
            var key = m.getAttribute('data-pin-instance');
            m.style.display = 'flex';
            document.body.classList.add('ipin-lock');
            var e = els(key);
            setTimeout(function () { if (e.boxes[0]) e.boxes[0].focus({ preventScroll: true }); }, 150);
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
