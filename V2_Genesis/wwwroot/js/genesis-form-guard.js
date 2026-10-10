/*
 * Genesis Form Guard
 * Shared by Objection, Appeal and Section 78 Query/Review forms.
 *
 * Features:
 *  1. Confirms Section 1 email addresses.
 *  2. Keeps form values through browser refresh using sessionStorage.
 *  3. Keeps selected evidence files through refresh using IndexedDB.
 *
 * IMPORTANT:
 * - Password fields and anti-forgery tokens are never stored.
 * - Evidence drafts expire automatically after 24 hours.
 * - Call window.GenesisClearFormDraft() after a confirmed successful submission.
 */
(function () {
    'use strict';

    const form = document.getElementById('myForm');
    if (!form) return;

    const DRAFT_PREFIX = 'GenesisFormDraft:v2:';

    // Drafts saved by the older version (one draft for every form type of a
    // property, including Section 6 roll values) are removed.
    try {
        Object.keys(sessionStorage)
            .filter(function (k) { return k.indexOf('GenesisFormDraft:v1:') === 0; })
            .forEach(function (k) { sessionStorage.removeItem(k); });
    } catch (_) { }
    const FILE_DB = 'GenesisEvidenceDrafts';
    const FILE_STORE = 'files';
    const FILE_EXPIRY_MS = 24 * 60 * 60 * 1000;

    function valueOf(id) {
        return document.getElementById(id)?.value?.trim() || '';
    }

    function getProcessName() {
        const path = window.location.pathname.toLowerCase();
        const review = valueOf('reviewStat').toUpperCase();
        const appeal = valueOf('AppealStat').toLowerCase();

        if (path.includes('section78')) {
            return review === 'R' ? 'Section78Review' : 'Section78Query';
        }

        return appeal === 'true' ? 'Appeal' : 'Objection';
    }

    // The draft belongs to ONE form: the process, the property, the form
    // type (Residential / Business / Agricultural / Multipurpose) and who
    // is lodging. Without the form type, a client who first opened the
    // Residential form and then went back to choose the Multipurpose form
    // got the Residential values (including Section 6) back.
    function getDraftKey() {
        const process = getProcessName();
        const premise = valueOf('Premise_id') || 'NoPremise';
        const property = valueOf('Property_Desc') || 'NoProperty';
        let formType = '';
        let objector = '';
        try {
            formType = sessionStorage.getItem('property_choice') || '';
            objector = sessionStorage.getItem('objector_choice') || '';
        } catch (_) { }
        formType = formType || valueOf('Property_Type') || 'NoType';
        objector = objector || valueOf('Objector_Type') || 'NoObjector';
        return DRAFT_PREFIX + process + ':' + premise + ':' + property + ':' + formType + ':' + objector;
    }

    function normaliseEmail(value) {
        return (value || '').trim().toLowerCase();
    }

    function isEmail(value) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value || '');
    }

    function setEmailState(primary, confirm, status, message, valid) {
        if (status) {
            status.textContent = message || '';
            status.classList.toggle('text-success', !!valid);
            status.classList.toggle('text-danger', valid === false);
        }

        if (confirm) {
            confirm.classList.toggle('is-valid', !!valid);
            confirm.classList.toggle('is-invalid', valid === false);
            confirm.setCustomValidity(valid === false ? (message || 'Email addresses do not match.') : '');
        }

        if (primary) {
            const primaryValid = !primary.value || isEmail(primary.value);
            primary.classList.toggle('is-invalid', !primaryValid);
            if (!primaryValid) {
                primary.setCustomValidity('Please enter a valid email address.');
            } else {
                primary.setCustomValidity('');
            }
        }
    }

    function validateEmailPair(modelName, showEmptyError) {
        const primary = form.querySelector('[data-email-primary="' + modelName + '"]');
        const confirm = form.querySelector('[data-email-confirm="' + modelName + '"]');
        const status = form.querySelector('[data-email-status="' + modelName + '"]');

        if (!primary || !confirm) return true;

        const email = normaliseEmail(primary.value);
        const confirmation = normaliseEmail(confirm.value);

        if (!email && !confirmation && !showEmptyError) {
            setEmailState(primary, confirm, status, '', null);
            return true;
        }

        if (!email || !isEmail(email)) {
            setEmailState(primary, confirm, status, 'Enter a valid email address first.', false);
            return false;
        }

        if (!confirmation) {
            setEmailState(primary, confirm, status,
                showEmptyError ? 'Please confirm the email address.' : '', false);
            return false;
        }

        if (email !== confirmation) {
            setEmailState(primary, confirm, status,
                'Email addresses do not match. Please correct one of them.', false);
            return false;
        }

        setEmailState(primary, confirm, status, '✓ Email addresses match.', true);
        return true;
    }

    // A Representative must not put their own e-mail in the Owner details:
    // the owner would never receive the acknowledgement and the owner's
    // contact details would be lost. (Server checks the same rule.)
    const OWNER_IS_REP_MESSAGE =
        "The owner's e-mail cannot be the same as the representative's e-mail. " +
        "Please enter the property owner's own e-mail address.";

    function validateOwnerNotRep() {
        const owner = form.querySelector('[data-email-primary="Owner_Email"]');
        const rep = form.querySelector('[data-email-primary="Rep_Email"]');
        if (!owner || !rep) return true;

        const ownerEmail = normaliseEmail(owner.value);
        const repEmail = normaliseEmail(rep.value);
        const clash = !!ownerEmail && !!repEmail && ownerEmail === repEmail;

        const ownerConfirm = form.querySelector('[data-email-confirm="Owner_Email"]');
        const ownerStatus = form.querySelector('[data-email-status="Owner_Email"]');

        if (clash) {
            owner.classList.add('is-invalid');
            owner.classList.remove('is-valid');
            owner.setCustomValidity(OWNER_IS_REP_MESSAGE);
            if (ownerConfirm) {
                ownerConfirm.classList.remove('is-valid');
                ownerConfirm.classList.add('is-invalid');
            }
            if (ownerStatus) {
                ownerStatus.textContent = OWNER_IS_REP_MESSAGE;
                ownerStatus.classList.remove('text-success');
                ownerStatus.classList.add('text-danger');
            }
            return false;
        }

        // No clash: let the normal match check set the owner's state again.
        if (owner.validationMessage === OWNER_IS_REP_MESSAGE) {
            owner.setCustomValidity('');
            validateEmailPair('Owner_Email', false);
        }
        return true;
    }

    function validateAllEmailPairs(showEmptyError) {
        let valid = true;
        form.querySelectorAll('[data-email-primary]').forEach(function (primary) {
            const modelName = primary.getAttribute('data-email-primary');
            if (!validateEmailPair(modelName, showEmptyError)) valid = false;
        });
        if (!validateOwnerNotRep()) valid = false;
        return valid;
    }

    form.querySelectorAll('[data-email-primary], [data-email-confirm]').forEach(function (input) {
        input.addEventListener('input', function () {
            const modelName =
                input.getAttribute('data-email-primary') ||
                input.getAttribute('data-email-confirm');

            validateEmailPair(modelName, false);
            validateOwnerNotRep();
            scheduleSave();
        });

        input.addEventListener('blur', function () {
            const modelName =
                input.getAttribute('data-email-primary') ||
                input.getAttribute('data-email-confirm');

            validateEmailPair(modelName, true);
            validateOwnerNotRep();
        });
    });

    // ─────────────────────────────────────────────────────────────
    // Form draft persistence (refresh-safe)
    // ─────────────────────────────────────────────────────────────
    let saveTimer = null;

    // Values that come from the server (valuation roll values in Section 6,
    // the form type, the property keys ...) are never saved or restored:
    // they must always show what the server sent for THIS form.
    const SERVER_FIELDS = [
        'Property_Type', 'Objector_Type', 'Premise_id', 'Property_Desc',
        'Valuation_Key', 'Unit_Key', 'AppealStat', 'obj_appeal', 'reviewStat',
        'RollSource', 'SourceTable', 'PropertyFrom'
    ];

    function shouldSkipControl(el) {
        if (!el.name) return true;
        if (el.type === 'file' || el.type === 'password') return true;
        if (el.name === '__RequestVerificationToken') return true;
        if (el.disabled || el.readOnly) return true;
        if (el.id !== 'SignatureDataUrl' &&
            (el.type === 'hidden' || el.hasAttribute('hidden'))) return true;
        if (/^Old\d?_/i.test(el.name)) return true;
        if (SERVER_FIELDS.indexOf(el.name) !== -1) return true;
        return false;
    }

    function collectFormValues() {
        const data = {};
        const groups = {};

        form.querySelectorAll('input, textarea, select').forEach(function (el) {
            if (shouldSkipControl(el)) return;

            if (el.type === 'radio') {
                if (el.checked) data[el.name] = el.value;
                return;
            }

            if (el.type === 'checkbox') {
                if (!groups[el.name]) groups[el.name] = [];
                if (el.checked) groups[el.name].push(el.value || 'true');
                return;
            }

            data[el.name] = el.value;
        });

        Object.keys(groups).forEach(function (name) {
            data[name] = groups[name];
        });

        return {
            savedAt: Date.now(),
            values: data
        };
    }

    function saveDraft() {
        try {
            sessionStorage.setItem(getDraftKey(), JSON.stringify(collectFormValues()));
        } catch (err) {
            console.warn('Genesis draft could not be saved.', err);
        }
    }

    function scheduleSave() {
        clearTimeout(saveTimer);
        saveTimer = setTimeout(saveDraft, 250);
    }

    function restoreDraft() {
        let parsed;

        try {
            const raw = sessionStorage.getItem(getDraftKey());
            if (!raw) return;
            parsed = JSON.parse(raw);
        } catch (err) {
            console.warn('Genesis draft could not be restored.', err);
            return;
        }

        const values = parsed?.values || {};

        Object.keys(values).forEach(function (name) {
            const controls = form.querySelectorAll('[name="' + CSS.escape(name) + '"]');
            if (!controls.length) return;

            controls.forEach(function (el) {
                if (shouldSkipControl(el)) return;

                if (el.type === 'radio') {
                    el.checked = el.value === values[name];
                } else if (el.type === 'checkbox') {
                    const selected = Array.isArray(values[name]) ? values[name] : [values[name]];
                    el.checked = selected.includes(el.value || 'true');
                } else {
                    // Do not overwrite a server-supplied value with an empty draft value.
                    if (values[name] !== undefined && values[name] !== null) {
                        el.value = values[name];
                    }
                }

                el.dispatchEvent(new Event('change', { bubbles: true }));
            });
        });

        validateAllEmailPairs(false);

        // Restore saved signature picture into the canvas.
        const signatureData = document.getElementById('SignatureDataUrl');
        const canvas = document.getElementById('signature');
        if (signatureData?.value && canvas) {
            const image = new Image();
            image.onload = function () {
                const ctx = canvas.getContext('2d');
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                ctx.drawImage(image, 0, 0, canvas.width, canvas.height);

                const status = document.getElementById('signatureStatus');
                if (status) {
                    status.textContent = '✓ Signature restored';
                    status.style.color = '#15803d';
                }

                const submit = document.getElementById('submitForm');
                if (submit) submit.disabled = false;
            };
            image.src = signatureData.value;
        }
    }

    form.addEventListener('input', scheduleSave);
    form.addEventListener('change', scheduleSave);

    // ─────────────────────────────────────────────────────────────
    // Evidence draft persistence in IndexedDB
    // ─────────────────────────────────────────────────────────────
    function openFileDb() {
        return new Promise(function (resolve, reject) {
            const request = indexedDB.open(FILE_DB, 1);

            request.onupgradeneeded = function () {
                const db = request.result;
                if (!db.objectStoreNames.contains(FILE_STORE)) {
                    db.createObjectStore(FILE_STORE, { keyPath: 'key' });
                }
            };

            request.onsuccess = function () { resolve(request.result); };
            request.onerror = function () { reject(request.error); };
        });
    }

    async function saveEvidenceInput(input) {
        if (!input?.files) return;

        try {
            const db = await openFileDb();
            const tx = db.transaction(FILE_STORE, 'readwrite');
            const store = tx.objectStore(FILE_STORE);

            const files = Array.from(input.files).map(function (file) {
                return {
                    name: file.name,
                    type: file.type,
                    lastModified: file.lastModified,
                    blob: file
                };
            });

            store.put({
                key: getDraftKey() + ':files:' + input.id,
                expiresAt: Date.now() + FILE_EXPIRY_MS,
                files: files
            });
        } catch (err) {
            console.warn('Evidence draft could not be saved.', err);
        }
    }

    async function restoreEvidenceInput(input) {
        try {
            const db = await openFileDb();
            const tx = db.transaction(FILE_STORE, 'readwrite');
            const store = tx.objectStore(FILE_STORE);
            const key = getDraftKey() + ':files:' + input.id;

            const record = await new Promise(function (resolve, reject) {
                const request = store.get(key);
                request.onsuccess = function () { resolve(request.result); };
                request.onerror = function () { reject(request.error); };
            });

            if (!record) return;

            if (record.expiresAt < Date.now()) {
                store.delete(key);
                return;
            }

            const transfer = new DataTransfer();

            (record.files || []).forEach(function (saved) {
                const file = new File(
                    [saved.blob],
                    saved.name,
                    {
                        type: saved.type || '',
                        lastModified: saved.lastModified || Date.now()
                    }
                );

                transfer.items.add(file);
            });

            input.files = transfer.files;

            // Existing Objection/Query upload UI will rebuild its visible file list.
            input.dispatchEvent(new Event('change', { bubbles: true }));
        } catch (err) {
            console.warn('Evidence draft could not be restored.', err);
        }
    }

    const evidenceInputs = Array.from(
        form.querySelectorAll('input[type="file"][name="files"][multiple]')
    );

    evidenceInputs.forEach(function (input) {
        input.addEventListener('change', function () {
            // Delay until the existing form script has finished rebuilding DataTransfer.
            setTimeout(function () {
                saveEvidenceInput(input);
            }, 0);
        });
    });

    async function clearEvidenceDrafts() {
        try {
            const db = await openFileDb();
            const tx = db.transaction(FILE_STORE, 'readwrite');
            const store = tx.objectStore(FILE_STORE);

            evidenceInputs.forEach(function (input) {
                store.delete(getDraftKey() + ':files:' + input.id);
            });
        } catch (err) {
            console.warn('Evidence draft could not be cleared.', err);
        }
    }

    window.GenesisClearFormDraft = async function () {
        try {
            sessionStorage.removeItem(getDraftKey());
        } catch (_) { }

        await clearEvidenceDrafts();
    };

    // Final safety check. Existing onclick validators still run as normal.
    form.addEventListener('submit', function (event) {
        if (!validateAllEmailPairs(true)) {
            event.preventDefault();
            event.stopImmediatePropagation();

            const invalid = form.querySelector('[data-email-confirm].is-invalid, [data-email-primary].is-invalid');
            invalid?.scrollIntoView({ behavior: 'smooth', block: 'center' });
            invalid?.focus();

            alert(validateOwnerNotRep()
                ? 'Please make sure the email address and confirmation email match before submitting.'
                : OWNER_IS_REP_MESSAGE);
            return false;
        }

        saveDraft();
    }, true);

    document.addEventListener('DOMContentLoaded', function () {
        restoreDraft();

        evidenceInputs.forEach(function (input) {
            restoreEvidenceInput(input);
        });
    });
})();
