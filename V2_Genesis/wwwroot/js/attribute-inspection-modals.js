(function () {
    'use strict';

    const pinValues = {};
    const PIN_LEN = 4;

    function updatePinDots(id) {
        const value = pinValues[id] || '';

        for (let i = 0; i < PIN_LEN; i++) {
            const dot =
                document.getElementById(`dot_${id}_${i}`);

            if (dot) {
                dot.classList.toggle(
                    'filled',
                    i < value.length);
            }
        }
    }

    function getPinElements(id) {
        return {
            modal:
                document.getElementById(
                    'pinModal_' + id),

            input:
                document.getElementById(
                    'pinHiddenInput_' + id),

            error:
                document.getElementById(
                    'pinError_' + id),

            submit:
                document.getElementById(
                    'pinSubmitBtn_' + id)
        };
    }

    window.openInspectionResponseModal = function (id) {
        const modal =
            document.getElementById(
                'inspectionModal_' + id);

        if (!modal) {
            console.warn(
                'Inspection calendar modal was not found:',
                id);
            return false;
        }

        modal.style.display = 'flex';
        modal.classList.add('open');
        modal.setAttribute(
            'aria-hidden',
            'false');

        document.body.style.overflow =
            'hidden';

        return true;
    };

    window.closeInspectionResponseModal = function (id) {
        const modal =
            document.getElementById(
                'inspectionModal_' + id);

        if (!modal) {
            return false;
        }

        modal.classList.remove('open');
        modal.style.display = 'none';
        modal.setAttribute(
            'aria-hidden',
            'true');

        document.body.style.overflow = '';

        return true;
    };

    window.openPinModal = function (id) {
        const elements =
            getPinElements(id);

        if (!elements.modal ||
            !elements.input ||
            !elements.submit) {

            console.warn(
                'Inspection PIN modal was not fully rendered:',
                id);

            return false;
        }

        pinValues[id] = '';
        elements.input.value = '';

        if (elements.error) {
            elements.error.classList.remove(
                'show');
        }

        elements.submit.disabled = true;

        updatePinDots(id);

        elements.modal.style.display =
            'flex';

        elements.modal.classList.add(
            'open');

        elements.modal.setAttribute(
            'aria-hidden',
            'false');

        document.body.style.overflow =
            'hidden';

        window.setTimeout(function () {
            elements.input.focus({
                preventScroll: true
            });
        }, 100);

        return true;
    };

    window.closePinModal = function (id) {
        const elements =
            getPinElements(id);

        if (elements.modal) {
            elements.modal.classList.remove(
                'open');

            elements.modal.style.display =
                'none';

            elements.modal.setAttribute(
                'aria-hidden',
                'true');
        }

        pinValues[id] = '';

        if (elements.input) {
            elements.input.value = '';
        }

        if (elements.error) {
            elements.error.classList.remove(
                'show');
        }

        if (elements.submit) {
            elements.submit.disabled = true;
            elements.submit.textContent =
                'Confirm PIN';
        }

        updatePinDots(id);

        document.body.style.overflow = '';

        return true;
    };

    window.pinKey = function (id, digit) {
        if (!/^\d$/.test(String(digit))) {
            return;
        }

        let value =
            pinValues[id] || '';

        if (value.length >= PIN_LEN) {
            return;
        }

        value += String(digit);
        pinValues[id] = value;

        const elements =
            getPinElements(id);

        if (elements.input) {
            elements.input.value = value;
        }

        if (elements.error) {
            elements.error.classList.remove(
                'show');
        }

        if (elements.submit) {
            elements.submit.disabled =
                value.length !== PIN_LEN;
        }

        updatePinDots(id);
    };

    window.pinDelete = function (id) {
        let value =
            pinValues[id] || '';

        if (!value.length) {
            return;
        }

        value =
            value.slice(0, -1);

        pinValues[id] = value;

        const elements =
            getPinElements(id);

        if (elements.input) {
            elements.input.value = value;
        }

        if (elements.error) {
            elements.error.classList.remove(
                'show');
        }

        if (elements.submit) {
            elements.submit.disabled =
                value.length !== PIN_LEN;
        }

        updatePinDots(id);
    };

    window.submitInspectionPin =
        function (id, form) {

            const elements =
                getPinElements(id);

            const value =
                (elements.input?.value || '')
                    .trim();

            if (!/^\d{4}$/.test(value)) {
                if (elements.error) {
                    elements.error.classList.add(
                        'show');

                    const msg =
                        elements.error.querySelector(
                            'span');

                    if (msg) {
                        msg.textContent =
                            'Please enter the complete 4-digit inspection PIN.';
                    }
                }

                return false;
            }

            if (!form ||
                !form.action ||
                form.action.trim() === '') {

                console.error(
                    'Inspection PIN form action is missing.');

                return false;
            }

            if (elements.submit) {
                elements.submit.disabled = true;

                elements.submit.innerHTML =
                    '<i class="fa-solid fa-spinner fa-spin"></i> Verifying...';
            }

            return true;
        };

    document.addEventListener(
        'keydown',
        function (event) {

            const openPin =
                document.querySelector(
                    '.pin-backdrop.open[id^="pinModal_"]');

            if (!openPin) {
                return;
            }

            const id =
                openPin.id.replace(
                    'pinModal_',
                    '');

            if (/^\d$/.test(event.key)) {
                event.preventDefault();
                window.pinKey(
                    id,
                    event.key);
            }
            else if (
                event.key === 'Backspace' ||
                event.key === 'Delete') {

                event.preventDefault();
                window.pinDelete(id);
            }
            else if (event.key === 'Escape') {
                event.preventDefault();
                window.closePinModal(id);
            }
        });

    document.addEventListener(
        'DOMContentLoaded',
        function () {

            document
                .querySelectorAll(
                    '.pin-backdrop.open[id^="pinModal_"]')
                .forEach(function (modal) {

                    const id =
                        modal.id.replace(
                            'pinModal_',
                            '');

                    const elements =
                        getPinElements(id);

                    pinValues[id] =
                        elements.input?.value || '';

                    updatePinDots(id);

                    modal.style.display =
                        'flex';

                    modal.setAttribute(
                        'aria-hidden',
                        'false');

                    document.body.style.overflow =
                        'hidden';

                    window.setTimeout(
                        function () {
                            elements.input?.focus({
                                preventScroll: true
                            });
                        },
                        100);
                });

            const openCalendar =
                document.querySelector(
                    '.attr-insp-modal-backdrop[style*="display:flex"],' +
                    '.attr-insp-modal-backdrop[style*="display: flex"],' +
                    '.attr-insp-modal-backdrop.open');

            if (openCalendar) {
                document.body.style.overflow =
                    'hidden';
            }
        });
})();