/*
 * sa-id-validation.js
 * ---------------------------------------------------------------------------
 * South African ID number validation for the Objection, Appeal, Multipurpose
 * and Section 78 forms. Load it AFTER the form's own script
 * (ObjectionJS / ObjectionQJS / ObjectionMulti / ObjectionMulti_Section78):
 * it replaces the old checksum-only LuhnAlgo() with a full check.
 *
 * A South African ID number (13 digits):
 *   1-6   date of birth, YYMMDD
 *   7-10  gender: 0000-4999 female, 5000-9999 male
 *   11    citizenship: 0 citizen, 1 permanent resident, 2 refugee
 *   12    population register index (usually 8)
 *   13    checksum digit (Luhn)
 *
 * Owner (1.1) and Third-Party objector (1.2): when a COMPANY / C.C.
 * REGISTRATION NO. is entered, the ID number and passport number are not
 * requested (hidden and not validated).
 *
 * The form scripts compare LuhnAlgo() with 'Invalid ID Number'; that return
 * value is kept. The detailed reason is shown under the field and placed in
 * window.GenesisIdError for the alert.
 * ---------------------------------------------------------------------------
 */
(function () {
    'use strict';

    var INVALID = 'Invalid ID Number';

    function $(id) { return document.getElementById(id); }

    function luhnOk(id) {
        var sum = 0;
        for (var i = 0; i < id.length; i++) {
            var d = parseInt(id.charAt(id.length - 1 - i), 10);
            if (i % 2 === 1) {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
        }
        return sum % 10 === 0;
    }

    /** Full SA ID check. Returns { valid, message, info }. */
    function validate(raw) {
        var id = (raw || '').trim();

        if (!/^\d{13}$/.test(id)) {
            return { valid: false, message: 'The ID number must be exactly 13 digits (numbers only).' };
        }

        if (/^0{13}$/.test(id)) {
            return { valid: false, message: 'Please enter a real ID number.' };
        }

        // 1-6: date of birth (YYMMDD)
        var yy = parseInt(id.substr(0, 2), 10);
        var mm = parseInt(id.substr(2, 2), 10);
        var dd = parseInt(id.substr(4, 2), 10);
        var now = new Date();
        var century = yy <= (now.getFullYear() % 100) ? 2000 : 1900;
        var year = century + yy;
        var dob = new Date(year, mm - 1, dd);

        if (mm < 1 || mm > 12 ||
            dob.getFullYear() !== year ||
            dob.getMonth() !== mm - 1 ||
            dob.getDate() !== dd ||
            dob > now) {
            return {
                valid: false,
                message: 'The first 6 digits must be a valid date of birth (YYMMDD).'
            };
        }

        // 11: citizenship
        var citizenship = id.charAt(10);
        if (citizenship !== '0' && citizenship !== '1' && citizenship !== '2') {
            return {
                valid: false,
                message: 'Digit 11 must be 0 (SA citizen), 1 (permanent resident) or 2 (refugee).'
            };
        }

        // 13: checksum
        if (!luhnOk(id)) {
            return {
                valid: false,
                message: 'This ID number is not valid. Please check the digits and try again.'
            };
        }

        var gender = parseInt(id.substr(6, 4), 10) < 5000 ? 'Female' : 'Male';
        var status = citizenship === '0'
            ? 'SA citizen'
            : citizenship === '1' ? 'Permanent resident' : 'Refugee';
        var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

        return {
            valid: true,
            message: '',
            info: 'Valid ID · born ' + dd + ' ' + months[mm - 1] + ' ' + year + ' · ' + gender + ' · ' + status
        };
    }

    function validatePassport(raw) {
        var p = (raw || '').trim();
        if (!p) return { valid: false, message: 'Please enter the passport number.' };
        if (!/^[A-Za-z0-9]{6,20}$/.test(p)) {
            return { valid: false, message: 'The passport number must be 6 to 20 letters or numbers (no spaces).' };
        }
        return { valid: true, message: '' };
    }

    function setStatus(spanId, text, ok) {
        var span = $(spanId);
        if (!span) return;
        span.textContent = text || '';
        span.style.color = ok ? '#15803d' : '#b91c1c';
        span.style.fontWeight = '600';
        span.style.display = text ? 'block' : '';
        span.style.marginTop = text ? '4px' : '';
        span.style.fontSize = '13px';
    }

    function markField(input, ok) {
        if (!input) return;
        input.style.border = ok === null ? '' : ok ? '2px solid #16a34a' : '2px solid #dc2626';
    }

    // ── Company registration: ID / passport not requested ─────────────
    // Applies to the OWNER (1.1) and to the THIRD-PARTY objector (1.2).
    // When a company / C.C. registration number is typed, the ID number
    // and passport number are hidden, cleared and not validated.
    var PARTIES = {
        Owner: { idId: 'o_id', passId: 'o_pass', statusId: 'id_status', company: 'Owner_Company', noteId: 'ownerCompanyIdNote' },
        Third_Party: { idId: 'objector_id', passId: 'objector_pass', statusId: 'obj_id_status', company: 'Objector_Company', noteId: 'objectorCompanyIdNote' }
    };

    function companyInputFor(party) {
        return document.querySelector('input[name="' + party.company + '"]');
    }

    function hasCompany(party) {
        var c = companyInputFor(party);
        return !!(c && !c.disabled && c.value && c.value.trim());
    }

    function ownerHasCompany() { return hasCompany(PARTIES.Owner); }

    function idBlockFor(party) {
        var id = $(party.idId);
        return id ? id.closest('.col-sm-8') : null;
    }

    function idModeIsId(party) {
        // First radio in the group = ID Number
        var block = idBlockFor(party);
        var radio = block ? block.querySelector('input[type="radio"]') : null;
        return !radio || radio.checked;
    }

    function applyCompanyRuleFor(party) {
        var block = idBlockFor(party);
        if (!block) return;

        var note = $(party.noteId);
        if (!note) {
            note = document.createElement('div');
            note.id = party.noteId;
            note.style.cssText =
                'display:none;margin:6px 0 10px;padding:10px 12px;border-radius:8px;' +
                'background:#ecfdf3;border:1px solid #16a34a;color:#14532d;font-size:13px;font-weight:600;';
            note.innerHTML =
                '<i class="fa-solid fa-building" style="margin-right:6px;"></i>' +
                'A company / C.C. registration number was entered, so an ID or passport number is not required.';
            block.insertBefore(note, block.firstChild);
        }

        var idInput = $(party.idId);
        var passInput = $(party.passId);
        var parts = Array.prototype.filter.call(block.children, function (c) { return c !== note; });

        if (hasCompany(party)) {
            // !important: the page CSS forces the ID / Passport radio row to
            // display:flex !important, so a plain display:none did not hide it.
            parts.forEach(function (c) { c.style.setProperty('display', 'none', 'important'); });
            note.style.display = 'block';
            if (idInput) { idInput.value = ''; idInput.disabled = true; markField(idInput, null); }
            if (passInput) { passInput.value = ''; passInput.disabled = true; markField(passInput, null); }
            setStatus(party.statusId, '', true);
            window.GenesisIdError = '';
        } else {
            parts.forEach(function (c) { c.style.removeProperty('display'); });
            note.style.display = 'none';
            var idMode = idModeIsId(party);
            if (idInput) idInput.disabled = !idMode;
            if (passInput) passInput.disabled = idMode;
        }
    }

    function applyCompanyRule() {
        applyCompanyRuleFor(PARTIES.Owner);
        applyCompanyRuleFor(PARTIES.Third_Party);
    }

    // ── Live checks on the ID inputs ──────────────────────────────────
    function wireIdInput(inputId, statusId) {
        var input = $(inputId);
        if (!input) return;

        input.setAttribute('maxlength', '13');
        input.setAttribute('inputmode', 'numeric');
        input.setAttribute('pattern', '[0-9]{13}');
        input.setAttribute('autocomplete', 'off');
        input.setAttribute('placeholder', '13-digit ID number');

        input.addEventListener('input', function () {
            var digits = input.value.replace(/\D/g, '').slice(0, 13);
            if (digits !== input.value) input.value = digits;

            if (!digits) { setStatus(statusId, '', true); markField(input, null); return; }

            if (digits.length < 13) {
                setStatus(statusId, (13 - digits.length) + ' more digit' + (13 - digits.length === 1 ? '' : 's') + ' needed.', false);
                markField(input, null);
                return;
            }

            var r = validate(digits);
            setStatus(statusId, r.valid ? r.info : r.message, r.valid);
            markField(input, r.valid);
        });

        // Allow paste of numbers only (the old markup blocks paste on the label only).
        input.addEventListener('paste', function (e) {
            var text = (e.clipboardData || window.clipboardData).getData('text') || '';
            e.preventDefault();
            input.value = text.replace(/\D/g, '').slice(0, 13);
            input.dispatchEvent(new Event('input'));
        });
    }

    // ── Replacement for the form scripts' LuhnAlgo() ─────────────────
    function currentObjectorKey() {
        if (typeof window.objector_key !== 'undefined' && window.objector_key) return window.objector_key;
        return sessionStorage.getItem('objector_choice');
    }

    function fail(statusId, input, message) {
        window.GenesisIdError = message;
        setStatus(statusId, message, false);
        markField(input, false);
        return INVALID;
    }

    function pass(statusId, input, info) {
        window.GenesisIdError = '';
        setStatus(statusId, info || '', true);
        markField(input, info ? true : null);
        return '';
    }

    function checkPerson(idInputId, passInputId, statusId, party) {
        var allowCompany = !!party;
        if (party && hasCompany(party)) return pass(statusId, null, '');

        var idInput = $(idInputId);
        var passInput = $(passInputId);
        if (!idInput && !passInput) return '';

        var usingId = idInput && !idInput.disabled;

        if (usingId) {
            var value = idInput.value.trim();
            if (!value) {
                return fail(statusId, idInput, allowCompany
                    ? 'Please enter the 13-digit ID number, choose Passport Number, or enter the company / C.C. registration number.'
                    : 'Please enter the 13-digit ID number, or choose Passport Number.');
            }
            var r = validate(value);
            return r.valid ? pass(statusId, idInput, r.info) : fail(statusId, idInput, r.message);
        }

        var p = validatePassport(passInput ? passInput.value : '');
        return p.valid ? pass(statusId, passInput, '') : fail(statusId, passInput, p.message);
    }

    window.GenesisSaId = { validate: validate, validatePassport: validatePassport, applyCompanyRule: applyCompanyRule };

    window.LuhnAlgo = function () {
        var key = currentObjectorKey();
        if (key === 'Owner') return checkPerson('o_id', 'o_pass', 'id_status', PARTIES.Owner);
        if (key === 'Third_Party') return checkPerson('objector_id', 'objector_pass', 'obj_id_status', PARTIES.Third_Party);
        window.GenesisIdError = '';
        return '';
    };

    function init() {
        wireIdInput('o_id', 'id_status');
        wireIdInput('objector_id', 'obj_id_status');

        [PARTIES.Owner, PARTIES.Third_Party].forEach(function (party) {
            var company = companyInputFor(party);
            if (company) {
                company.addEventListener('input', function () { applyCompanyRuleFor(party); });
                company.addEventListener('change', function () { applyCompanyRuleFor(party); });
            }

            // Re-apply after the ID / Passport radios are clicked.
            var block = idBlockFor(party);
            if (block) {
                block.querySelectorAll('input[type="radio"]').forEach(function (r) {
                    r.addEventListener('change', function () {
                        setStatus(party.statusId, '', true);
                        markField($(party.idId), null);
                        markField($(party.passId), null);
                    });
                });
            }
        });

        // Drafts restored by genesis-form-guard.js fill the company box
        // without an input event: check again once the page has loaded.
        window.addEventListener('load', function () { setTimeout(applyCompanyRule, 50); });

        applyCompanyRule();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
