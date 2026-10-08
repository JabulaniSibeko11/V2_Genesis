/*
 * attribute-inspection-modals.js
 * ---------------------------------------------------------------------------
 * Inspection date modal helpers (Views/Shared/Attributes/_InspectionDateModal).
 * The PIN modal has its own script: ~/js/inspection-pin.js (4 PIN boxes, no
 * on-screen keypad). openPinModal / closePinModal are provided there.
 * ---------------------------------------------------------------------------
 */
(function () {
    'use strict';

    window.openInspectionResponseModal = function (id) {
        var modal = document.getElementById('inspectionModal_' + id);
        if (!modal) {
            console.warn('Inspection calendar modal was not found:', id);
            return false;
        }

        modal.style.display = 'flex';
        modal.classList.add('open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
        return true;
    };

    window.closeInspectionResponseModal = function (id) {
        var modal = document.getElementById('inspectionModal_' + id);
        if (!modal) return false;

        modal.classList.remove('open');
        modal.style.display = 'none';
        modal.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
        return true;
    };

    document.addEventListener('DOMContentLoaded', function () {
        var openCalendar = document.querySelector(
            '.attr-insp-modal-backdrop[style*="display:flex"],' +
            '.attr-insp-modal-backdrop[style*="display: flex"],' +
            '.attr-insp-modal-backdrop.open');

        if (openCalendar) document.body.style.overflow = 'hidden';
    });
})();
