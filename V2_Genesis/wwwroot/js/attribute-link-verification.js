(function () {
    'use strict';

    const backdrop = document.getElementById('attrLinkBackdrop');
    const form = document.getElementById('attrLinkForm');
    const idProperty = document.getElementById('attrLinkIdProperty');
    const propertyFrom = document.getElementById('attrLinkPropertyFrom');
    const account = document.getElementById('attrAccountNumber');
    const pin = document.getElementById('attrStatementPin');
    const error = document.getElementById('attrLinkError');
    const submit = document.getElementById('attrLinkSubmit');
    const pinToggle = document.getElementById('attrPinToggle');

    if (!backdrop || !form) return;

    function clearError() {
        if (!error) return;
        error.hidden = true;
        error.textContent = '';
    }

    function showError(message) {
        if (!error) return;
        error.textContent =
            message || 'The property could not be verified.';
        error.hidden = false;
    }

    function showVerificationLoader() {
        if (!window.GenesisLoader) return;

        window.GenesisLoader.show(
            'search',
            submit,
            {
                title: 'Verifying account details',
                message:
                    'Please wait while we verify the Account Number and Statement PIN.'
            });
    }

    function hideVerificationLoader() {
        window.GenesisLoader?.hide();
    }

    function openModal(button) {
        clearError();

        idProperty.value = button.dataset.unitKey || '';
        propertyFrom.value = 'Attributes';

        account.value = '';
        pin.value = '';
        pin.type = 'password';

        backdrop.hidden = false;
        document.body.style.overflow = 'hidden';

        window.setTimeout(() => account.focus(), 50);
    }

    function closeModal() {
        /*
         * Do not allow the verification dialog to be closed while the
         * verification request is still running.
         */
        if (submit?.disabled) return;

        backdrop.hidden = true;
        document.body.style.overflow = '';
        pin.value = '';
        clearError();
        hideVerificationLoader();
    }

    document.addEventListener('click', function (event) {
        const trigger =
            event.target.closest('[data-attr-secure-link]');

        if (trigger) {
            event.preventDefault();

            if (trigger.dataset.unitKey) {
                openModal(trigger);
            }

            return;
        }

        if (event.target === backdrop) {
            closeModal();
        }
    });

    document
        .getElementById('attrLinkClose')
        ?.addEventListener('click', closeModal);

    document
        .getElementById('attrLinkCancel')
        ?.addEventListener('click', closeModal);

    document.addEventListener('keydown', function (event) {
        if (
            event.key === 'Escape' &&
            !backdrop.hidden
        ) {
            closeModal();
        }
    });

    pinToggle?.addEventListener('click', function () {
        pin.type =
            pin.type === 'password'
                ? 'text'
                : 'password';

        const icon = pinToggle.querySelector('i');

        if (icon) {
            icon.className =
                pin.type === 'password'
                    ? 'fa-solid fa-eye'
                    : 'fa-solid fa-eye-slash';
        }
    });

    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        clearError();

        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const originalHtml = submit.innerHTML;

        submit.disabled = true;
        submit.setAttribute('aria-busy', 'true');
        submit.innerHTML =
            '<i class="fa-solid fa-spinner fa-spin"></i> Verifying...';

        /*
         * Explicitly show the application loader because this form is
         * submitted with fetch() and event.preventDefault().
         */
        showVerificationLoader();

        try {
            const response = await fetch(
                form.action,
                {
                    method: 'POST',
                    body: new FormData(form),
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    credentials: 'same-origin'
                });

            const payload =
                await response.json().catch(() => null);

            if (!response.ok || !payload?.success) {
                showError(
                    payload?.message ||
                    'The Account Number or Statement PIN could not be verified.');

                pin.value = '';
                pin.focus();
                return;
            }

            pin.value = '';

            window.location.href =
                payload.redirectUrl ||
                '/Dashboard?openRoll=attributes';
        }
        catch (err) {
            console.error(
                '[Attributes] Account/PIN verification failed.',
                err);

            showError(
                'We could not verify the property at this time. Please try again.');
        }
        finally {
            /*
             * If navigation succeeds the page will unload. If verification
             * fails, this clears both the full-page loader and button spinner.
             */
            hideVerificationLoader();

            submit.disabled = false;
            submit.removeAttribute('aria-busy');
            submit.innerHTML = originalHtml;
        }
    });
})();
