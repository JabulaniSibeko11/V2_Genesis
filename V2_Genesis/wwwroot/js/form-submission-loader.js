(function () {
    'use strict';

    function elements() {
        return {
            overlay: document.getElementById('genesisSubmissionLoader'),
            title: document.getElementById('submissionLoaderTitle'),
            message: document.getElementById('submissionLoaderMessage')
        };
    }

    function show(form, submitter) {
        const el = elements();
        if (!el.overlay) return;

        const title = form?.dataset?.submissionTitle || 'Submitting your application...';
        const message = form?.dataset?.submissionMessage ||
            'Please wait while your submission is being processed. Do not close or refresh this page.';

        if (el.title) el.title.textContent = title;
        if (el.message) el.message.textContent = message;
        el.overlay.classList.add('show');
        el.overlay.setAttribute('aria-hidden', 'false');
        document.body.classList.add('submission-is-loading');

        if (submitter) {
            submitter.setAttribute('aria-busy', 'true');
            if ('disabled' in submitter) submitter.disabled = true;
        }
    }

    function hide() {
        const el = elements();
        el.overlay?.classList.remove('show');
        el.overlay?.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('submission-is-loading');
    }

    document.addEventListener('submit', function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches('[data-submission-loader]')) return;

        const submitter = event.submitter || form.querySelector('button[type="submit"], input[type="submit"]');

        // Wait for the form's existing validation handlers. This prevents the
        // overlay from opening when one of those handlers cancels submission.
        window.setTimeout(function () {
            if (!event.defaultPrevented && form.checkValidity()) {
                show(form, submitter);
            }
        }, 0);
    });

    window.addEventListener('pageshow', hide);
    window.GenesisSubmissionLoader = { show: show, hide: hide };
})();
