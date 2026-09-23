(function () {
    'use strict';

    const PIN_LEN = 4;
    const pinValues = {};

    function initDataTable(table) {
        if (!window.jQuery ||
            !jQuery.fn ||
            !jQuery.fn.DataTable ||
            !table?.id) {
            return;
        }

        const selector =
            '#' + table.id;

        const phoneMode =
            isPhoneWidth();

        // Rebuild when the screen crosses the phone breakpoint so
        // the "+" rows (phone) / full columns (desktop) stay correct.
        if (
            jQuery.fn.DataTable.isDataTable(selector) &&
            table.dataset.dtMode !== (phoneMode ? 'phone' : 'desktop')
        ) {
            jQuery(selector).DataTable().destroy();
            table.classList.remove('dtr-inline', 'collapsed');
            table.querySelectorAll('th, td').forEach(cell =>
                cell.classList.remove('dtr-control', 'all', 'none'));
            table.querySelectorAll('[style*="display: none"]').forEach(cell =>
                cell.style.display = '');
            table.querySelectorAll('tr.child').forEach(row => row.remove());
            table.style.width = '';
        }

        if (jQuery.fn.DataTable.isDataTable(selector)) {
            const api =
                jQuery(selector).DataTable();

            api.columns.adjust();

            if (
                api.responsive &&
                typeof api.responsive.recalc === 'function'
            ) {
                api.responsive.recalc();
            }

            return;
        }

        const useResponsive =
            phoneMode;

        table.dataset.dtMode =
            phoneMode ? 'phone' : 'desktop';

        jQuery(selector).DataTable({
            responsive:
                useResponsive
                    ? {
                        details: {
                            type: 'inline',
                            target: 'td.dtr-control'
                        }
                    }
                    : false,

            autoWidth: false,
            pageLength: 10,

            language: {
                search: 'Search:',
                searchPlaceholder: 'Search this table…',
                emptyTable: 'No records to show yet.',
                zeroRecords: 'No matching records found.'
            },

            lengthMenu: [
                [5, 10, 25, 50, -1],
                [5, 10, 25, 50, 'All']
            ],

            columnDefs:
                useResponsive
                    ? [
                        {
                            targets: 0,
                            className:
                                'dtr-control all',
                            responsivePriority: 1
                        },
                        {
                            // Phone: every other column moves into
                            // the "+" details panel (card style).
                            targets: '_all',
                            className: 'none'
                        },
                        {
                            targets: -1,
                            orderable: false
                        }
                    ]
                    : [
                        {
                            targets: -1,
                            orderable: false
                        }
                    ]
        });
    }

    function isPhoneWidth() {
        return window.matchMedia(
            '(max-width: 767.98px)')
            .matches;
    }

    function initTablesWithin(container) {
        if (!container) {
            return;
        }

        container
            .querySelectorAll('.cd-table')
            .forEach(initDataTable);
    }

    window.toggleWidget =
        function (key) {
            const body =
                document.getElementById(
                    'widget-' + key);

            const chevron =
                document.getElementById(
                    'wchev-' + key);

            if (!body) {
                return;
            }

            const isOpen =
                body.classList.toggle(
                    'open');

            if (chevron) {
                chevron.style.transform =
                    isOpen
                        ? 'rotate(180deg)'
                        : 'rotate(0deg)';
            }

            if (isOpen) {
                initTablesWithin(body);
            }
        };

    window.logAction =
        function (action, roll, reference) {
            console.debug(
                '[Dashboard action]',
                action,
                roll,
                reference);
        };

    // ------------------------------------------------------------
    // Attribute inspection modal helpers
    // ------------------------------------------------------------
    window.closeInspectionResponseModal =
        function (id) {
            const modal =
                document.getElementById(
                    'inspectionModal_' + id);

            if (modal) {
                modal.style.display =
                    'none';

                document.body.style.overflow =
                    '';
            }
        };

    window.closePinModal =
        function (id) {
            const modal =
                document.getElementById(
                    'pinModal_' + id);

            const input =
                document.getElementById(
                    'pinHiddenInput_' + id);

            const error =
                document.getElementById(
                    'pinError_' + id);

            const submit =
                document.getElementById(
                    'pinSubmitBtn_' + id);

            if (modal) {
                modal.classList.remove(
                    'open');

                modal.style.display =
                    'none';
            }

            pinValues[id] = '';

            if (input) {
                input.value = '';
            }

            if (error) {
                error.classList.remove(
                    'show');
            }

            if (submit) {
                submit.disabled = true;
            }

            updatePinDots(id);

            document.body.style.overflow =
                '';
        };

    function updatePinDots(id) {
        const value =
            pinValues[id] || '';

        for (
            let index = 0;
            index < PIN_LEN;
            index++
        ) {
            const dot =
                document.getElementById(
                    `dot_${id}_${index}`);

            if (!dot) {
                continue;
            }

            dot.classList.remove(
                'filled',
                'gold',
                'error');

            if (index < value.length) {
                dot.classList.add(
                    'filled');
            }

            if (
                value.length === PIN_LEN &&
                index === PIN_LEN - 1
            ) {
                dot.classList.add(
                    'gold');
            }
        }
    }

    window.pinKey =
        function (id, digit) {
            let value =
                pinValues[id] || '';

            if (
                value.length >=
                PIN_LEN
            ) {
                return;
            }

            value += digit;

            pinValues[id] =
                value;

            const input =
                document.getElementById(
                    'pinHiddenInput_' + id);

            const error =
                document.getElementById(
                    'pinError_' + id);

            const submit =
                document.getElementById(
                    'pinSubmitBtn_' + id);

            if (input) {
                input.value = value;
            }

            error?.classList.remove(
                'show');

            if (submit) {
                submit.disabled =
                    value.length <
                    PIN_LEN;
            }

            updatePinDots(id);
        };

    window.pinDelete =
        function (id) {
            let value =
                pinValues[id] || '';

            value =
                value.slice(0, -1);

            pinValues[id] =
                value;

            const input =
                document.getElementById(
                    'pinHiddenInput_' + id);

            const error =
                document.getElementById(
                    'pinError_' + id);

            const submit =
                document.getElementById(
                    'pinSubmitBtn_' + id);

            if (input) {
                input.value = value;
            }

            error?.classList.remove(
                'show');

            if (submit) {
                submit.disabled =
                    value.length <
                    PIN_LEN;
            }

            updatePinDots(id);
        };

    window.submitInspectionPin =
        function (id, form) {
            const value =
                (
                    pinValues[id] ||
                    document
                        .getElementById(
                            'pinHiddenInput_' + id)
                        ?.value ||
                    ''
                )
                    .replace(/\D/g, '')
                    .slice(0, PIN_LEN);

            if (
                value.length !==
                PIN_LEN
            ) {
                return false;
            }

            const submit =
                document.getElementById(
                    'pinSubmitBtn_' + id);

            if (submit) {
                submit.disabled = true;
                submit.setAttribute(
                    'aria-busy',
                    'true');
            }

            return true;
        };

    document.addEventListener(
        'input',
        function (event) {
            const target =
                event.target;

            if (
                !target?.id ||
                !target.id.startsWith(
                    'pinHiddenInput_')
            ) {
                return;
            }

            const id =
                target.id.replace(
                    'pinHiddenInput_',
                    '');

            const value =
                target.value
                    .replace(/\D/g, '')
                    .slice(0, PIN_LEN);

            target.value =
                value;

            pinValues[id] =
                value;

            const submit =
                document.getElementById(
                    'pinSubmitBtn_' + id);

            if (submit) {
                submit.disabled =
                    value.length <
                    PIN_LEN;
            }

            updatePinDots(id);
        });

    document.addEventListener(
        'click',
        function (event) {
            const pinBackdrop =
                event.target.closest?.(
                    '.pin-backdrop');

            if (
                pinBackdrop &&
                event.target ===
                pinBackdrop
            ) {
                const id =
                    pinBackdrop.id.replace(
                        'pinModal_',
                        '');

                window.closePinModal(id);

                return;
            }

            if (
                event.target.classList?.contains(
                    'attr-insp-modal-backdrop')
            ) {
                event.target.style.display =
                    'none';

                document.body.style.overflow =
                    '';
            }
        });

    document.addEventListener(
        'keydown',
        function (event) {
            if (
                event.key !==
                'Escape'
            ) {
                return;
            }

            document
                .querySelectorAll(
                    '.pin-backdrop.open')
                .forEach(modal => {
                    const id =
                        modal.id.replace(
                            'pinModal_',
                            '');

                    window.closePinModal(id);
                });

            document
                .querySelectorAll(
                    '.attr-insp-modal-backdrop')
                .forEach(modal => {
                    if (
                        modal.style.display ===
                        'flex'
                    ) {
                        modal.style.display =
                            'none';

                        document.body.style.overflow =
                            '';
                    }
                });
        });

    // Recalculate visible tables when device width changes.
    let resizeTimer = null;

    window.addEventListener(
        'resize',
        function () {
            window.clearTimeout(
                resizeTimer);

            resizeTimer =
                window.setTimeout(
                    function () {
                        document
                            .querySelectorAll(
                                '.cd-widget-body.open .cd-table')
                            .forEach(initDataTable);
                    },
                    150);
        });
})();
