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

    let navigatingToDashboard = false;

    function clearError() {
        if (!error) return;

        error.hidden = true;
        error.textContent = '';
    }

    function showError(message) {
        if (!error) return;

        error.textContent =
            message ||
            'The property could not be verified.';

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

    function showDashboardLoader() {
        if (!window.GenesisLoader) return;

        window.GenesisLoader.show(
            'property',
            null,
            {
                title: 'Opening your dashboard',
                message:
                    'Your property has been verified and linked. Please wait while we load your Attributes dashboard.'
            });
    }

    function hideVerificationLoader() {
        window.GenesisLoader?.hide();
    }

    function resetSubmitButton(originalHtml) {
        submit.disabled = false;
        submit.removeAttribute('aria-busy');

        if (originalHtml !== undefined) {
            submit.innerHTML = originalHtml;
        }
    }

    function openModal(button) {
        navigatingToDashboard = false;

        clearError();

        idProperty.value =
            button.dataset.unitKey || '';

        propertyFrom.value =
            'Attributes';

        account.value = '';
        pin.value = '';
        pin.type = 'password';

        backdrop.hidden = false;
        document.body.style.overflow = 'hidden';

        submit.disabled = false;
        submit.removeAttribute('aria-busy');

        window.setTimeout(
            () => account.focus(),
            50);
    }

    function closeModal(force = false) {
        /*
         * While verification is running, the client must not be able
         * to dismiss the modal accidentally.
         *
         * force = true is used only after successful verification.
         */
        if (!force && submit?.disabled) {
            return;
        }

        backdrop.hidden = true;
        document.body.style.overflow = '';

        pin.value = '';

        clearError();

        /*
         * Do not hide the loader while transitioning to the dashboard.
         */
        if (!navigatingToDashboard) {
            hideVerificationLoader();
        }
    }

    document.addEventListener(
        'click',
        function (event) {
            const trigger =
                event.target.closest(
                    '[data-attr-secure-link]');

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

    /*
     * Wrap closeModal() so the click Event object is not passed
     * into the force parameter.
     */
    document
        .getElementById('attrLinkClose')
        ?.addEventListener(
            'click',
            () => closeModal());

    document
        .getElementById('attrLinkCancel')
        ?.addEventListener(
            'click',
            () => closeModal());

    document.addEventListener(
        'keydown',
        function (event) {
            if (
                event.key === 'Escape' &&
                !backdrop.hidden
            ) {
                closeModal();
            }
        });

    pinToggle?.addEventListener(
        'click',
        function () {
            pin.type =
                pin.type === 'password'
                    ? 'text'
                    : 'password';

            const icon =
                pinToggle.querySelector('i');

            if (icon) {
                icon.className =
                    pin.type === 'password'
                        ? 'fa-solid fa-eye'
                        : 'fa-solid fa-eye-slash';
            }
        });

    form.addEventListener(
        'submit',
        async function (event) {
            event.preventDefault();

            clearError();

            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const originalHtml =
                submit.innerHTML;

            navigatingToDashboard = false;

            submit.disabled = true;
            submit.setAttribute(
                'aria-busy',
                'true');

            submit.innerHTML =
                '<i class="fa-solid fa-spinner fa-spin"></i> Verifying...';

            /*
             * Loader 1:
             * Account Number + Statement PIN verification.
             */
            showVerificationLoader();

            try {
                const response =
                    await fetch(
                        form.action,
                        {
                            method: 'POST',

                            body:
                                new FormData(form),

                            headers: {
                                'X-Requested-With':
                                    'XMLHttpRequest'
                            },

                            credentials:
                                'same-origin'
                        });

                const payload =
                    await response
                        .json()
                        .catch(() => null);

                if (
                    !response.ok ||
                    !payload?.success
                ) {
                    showError(
                        payload?.message ||
                        'The Account Number or Statement PIN could not be verified.');

                    pin.value = '';
                    pin.focus();

                    return;
                }

                /*
                 * Verification succeeded.
                 */
                navigatingToDashboard = true;

                pin.value = '';

                /*
                 * Remove the verification modal before navigation.
                 */
                closeModal(true);

                /*
                 * Loader 2:
                 * Keep feedback visible while the Attributes dashboard loads.
                 */
                showDashboardLoader();

                const redirectUrl =
                    payload.redirectUrl ||
                    '/Dashboard?openRoll=attributes';

                /*
                 * Give the browser a paint cycle so the modal disappears
                 * and the second loader becomes visible before navigation.
                 */
                window.requestAnimationFrame(
                    () => {
                        window.requestAnimationFrame(
                            () => {
                                window.location.assign(
                                    redirectUrl);
                            });
                    });
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
                 * Failure:
                 * - hide verification loader
                 * - restore Verify button
                 *
                 * Success:
                 * - keep dashboard loader visible
                 * - navigation replaces the page
                 */
                if (!navigatingToDashboard) {
                    hideVerificationLoader();

                    resetSubmitButton(
                        originalHtml);
                }
            }
        });
})();
