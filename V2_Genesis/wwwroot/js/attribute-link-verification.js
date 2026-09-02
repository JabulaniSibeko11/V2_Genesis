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
        error.hidden = true;
        error.textContent = '';
    }

    function showError(message) {
        error.textContent = message || 'The property could not be verified.';
        error.hidden = false;
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
        backdrop.hidden = true;
        document.body.style.overflow = '';
        pin.value = '';
        clearError();
    }

    document.addEventListener('click', function (event) {
        const trigger = event.target.closest('[data-attr-secure-link]');
        if (trigger) {
            event.preventDefault();
            if (trigger.dataset.unitKey) openModal(trigger);
            return;
        }

        if (event.target === backdrop) closeModal();
    });

    document.getElementById('attrLinkClose')?.addEventListener('click', closeModal);
    document.getElementById('attrLinkCancel')?.addEventListener('click', closeModal);

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && !backdrop.hidden) closeModal();
    });

    pinToggle?.addEventListener('click', function () {
        pin.type = pin.type === 'password' ? 'text' : 'password';
        const icon = pinToggle.querySelector('i');
        if (icon) {
            icon.className = pin.type === 'password'
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

        submit.disabled = true;
        const originalHtml = submit.innerHTML;
        submit.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Verifying...';

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin'
            });

            const payload = await response.json().catch(() => null);

            if (!response.ok || !payload?.success) {
                showError(payload?.message || 'The Account Number or Statement PIN could not be verified.');
                pin.value = '';
                pin.focus();
                return;
            }

            pin.value = '';
            window.location.href = payload.redirectUrl || '/Dashboard?openRoll=attributes';
        }
        catch {
            showError('We could not verify the property at this time. Please try again.');
        }
        finally {
            submit.disabled = false;
            submit.innerHTML = originalHtml;
        }
    });
})();
