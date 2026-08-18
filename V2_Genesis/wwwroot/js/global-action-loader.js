(function () {
    'use strict';

    const state = {
        trigger: null,
        lastIntent: null,
        hideTimer: null,
        visible: false
    };

    const configurations = {
        search: {
            title: 'Searching properties',
            message: 'Please wait while we check the available valuation records.'
        },
        submit: {
            title: 'Submitting your application',
            message: 'We are saving your information and generating the acknowledgement.'
        },
        evidence: {
            title: 'Uploading evidence',
            message: 'Your supporting documents are being uploaded securely.'
        },
        property: {
            title: 'Processing property request',
            message: 'Please wait while we update or open the property information.'
        },
        document: {
            title: 'Generating document',
            message: 'Your acknowledgement or notice is being prepared for download.'
        },
        account: {
            title: 'Updating your account',
            message: 'Please wait while your account request is processed.'
        }
    };

    function normalise(value) {
        return (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
    }

    function elementText(element) {
        return normalise(
            element?.dataset?.loaderText ||
            element?.innerText ||
            element?.textContent ||
            element?.value ||
            element?.getAttribute?.('aria-label') ||
            element?.getAttribute?.('title'));
    }

    function classify(element) {
        if (!element || element.matches('[data-no-loader], [disabled]')) return null;
        if (element.form?.matches('[data-submission-loader]')) return null;

        const explicit = element.dataset?.loader;
        if (explicit === 'none') return null;
        if (configurations[explicit]) return explicit;

        const text = elementText(element);
        const href = normalise(element.getAttribute?.('href'));
        const action = normalise(element.form?.getAttribute('action'));
        const combined = `${text} ${href} ${action}`;

        if (/logout|sign out/.test(combined)) return null;

        if (/download|acknowledg|section 49|section49|section 51|section51|section 52|section52|section 53|section53|appeal decision|dear johnny|invalid objection|invalid omission|generate.*pdf|notice/.test(combined)) {
            return 'document';
        }

        if (/upload|evidence|supporting document/.test(combined)) {
            return 'evidence';
        }

        if (/search|verify|find property|check reference|check pin/.test(combined)) {
            return 'search';
        }

        if (/link property|unlink|remove property|withdraw|view property|continue to .*form|lodge objection|lodge appeal|lodge query|lodge review/.test(combined)) {
            return 'property';
        }

        if (/sign in|login|register|save changes|change password|update password|reset link|account/.test(combined)) {
            return 'account';
        }

        if (element.matches('button[type="submit"], input[type="submit"]') ||
            /submit|resubmit|send application|submit application|confirm withdrawal/.test(combined)) {
            return 'submit';
        }

        return null;
    }

    function loaderElements() {
        return {
            overlay: document.getElementById('genesisActionLoader'),
            title: document.getElementById('genesisLoaderTitle'),
            message: document.getElementById('genesisLoaderMessage')
        };
    }

    function markTrigger(element) {
        if (!element) return;
        state.trigger = element;
        element.classList.add('genesis-action-busy');
        element.setAttribute('aria-busy', 'true');
        if ('disabled' in element) element.disabled = true;
    }

    function releaseTrigger() {
        const element = state.trigger;
        if (!element) return;
        element.classList.remove('genesis-action-busy');
        element.removeAttribute('aria-busy');
        if ('disabled' in element) element.disabled = false;
        state.trigger = null;
    }

    function show(type, trigger, custom) {
        const pageLoader = document.getElementById('objLoaderOverlay');
        if (pageLoader &&
            (pageLoader.classList.contains('show') || pageLoader.style.display === 'flex')) {
            return;
        }

        const elements = loaderElements();
        if (!elements.overlay) return;

        const config = Object.assign({}, configurations[type] || configurations.submit, custom || {});
        if (elements.title) elements.title.textContent = config.title;
        if (elements.message) elements.message.textContent = config.message;
        elements.overlay.classList.add('is-visible');
        elements.overlay.setAttribute('aria-hidden', 'false');
        document.body.classList.add('genesis-is-loading');
        markTrigger(trigger);
        state.visible = true;

        window.clearTimeout(state.hideTimer);
    }

    function hide() {
        const elements = loaderElements();
        elements.overlay?.classList.remove('is-visible');
        elements.overlay?.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('genesis-is-loading');
        releaseTrigger();
        window.clearTimeout(state.hideTimer);
        state.visible = false;
    }

    function rememberIntent(element, type) {
        state.lastIntent = {
            element: element,
            type: type,
            at: Date.now()
        };
    }

    function recentIntent() {
        const intent = state.lastIntent;
        return intent && Date.now() - intent.at < 1800 ? intent : null;
    }

    document.addEventListener('click', function (event) {
        const element = event.target.closest('button, input[type="submit"], a');
        const type = classify(element);
        if (!type) return;

        rememberIntent(element, type);

        if (!element.matches('a')) {
            window.setTimeout(function () {
                // Some legacy forms call form.submit(), which bypasses the
                // submit event. Those handlers disable the clicked button.
                if (element.disabled && !state.visible) {
                    show(type, element);
                }
            }, 0);
        }

        if (element.matches('a') && !element.matches('[href="#"], [href="javascript:void(0)"]')) {
            show(type, element);

            if (type === 'document') {
                state.hideTimer = window.setTimeout(hide, 15000);
            }
        }
    }, true);

    document.addEventListener('submit', function (event) {
        const form = event.target;
        const submitter = event.submitter || form.querySelector('button[type="submit"], input[type="submit"]');
        const type = classify(submitter);
        if (!type) return;

        rememberIntent(submitter, type);

        window.setTimeout(function () {
            if (!event.defaultPrevented && form.checkValidity()) {
                show(type, submitter);
            }
        }, 0);
    });

    const nativeFetch = window.fetch;
    if (nativeFetch) {
        window.fetch = function () {
            const intent = recentIntent();
            if (intent && !state.visible) show(intent.type, intent.element);

            return nativeFetch.apply(this, arguments)
                .finally(function () {
                    if (intent) hide();
                });
        };
    }

    window.addEventListener('pageshow', hide);
    window.addEventListener('focus', function () {
        if (state.visible && recentIntent()?.type === 'document') {
            window.setTimeout(hide, 900);
        }
    });

    window.GenesisLoader = {
        show: show,
        hide: hide,
        classify: classify
    };
})();
