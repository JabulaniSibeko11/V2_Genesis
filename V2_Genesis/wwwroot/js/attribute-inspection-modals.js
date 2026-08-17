(function () {
    const pinValues = {};
    const PIN_LEN = 4;

    function updatePinDots(id) {
        const value = pinValues[id] || '';
        for (let i = 0; i < PIN_LEN; i++) {
            const dot = document.getElementById(`dot_${id}_${i}`);
            if (dot) dot.classList.toggle('filled', i < value.length);
        }
    }

    window.openInspectionResponseModal = function (id) {
        const modal = document.getElementById('inspectionModal_' + id);
        if (!modal) return;
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    };

    window.closeInspectionResponseModal = function (id) {
        const modal = document.getElementById('inspectionModal_' + id);
        if (!modal) return;
        modal.style.display = 'none';
        document.body.style.overflow = '';
    };

    window.openPinModal = function (id) {
        pinValues[id] = '';
        const modal = document.getElementById('pinModal_' + id);
        const input = document.getElementById('pinHiddenInput_' + id);
        const error = document.getElementById('pinError_' + id);
        const submit = document.getElementById('pinSubmitBtn_' + id);
        if (!modal || !input || !submit) return;
        input.value = '';
        if (error) error.classList.remove('show');
        submit.disabled = true;
        updatePinDots(id);
        modal.style.display = 'flex';
        modal.classList.add('open');
        document.body.style.overflow = 'hidden';
        setTimeout(() => input.focus(), 100);
    };

    window.closePinModal = function (id) {
        const modal = document.getElementById('pinModal_' + id);
        const input = document.getElementById('pinHiddenInput_' + id);
        const error = document.getElementById('pinError_' + id);
        const submit = document.getElementById('pinSubmitBtn_' + id);
        if (modal) { modal.classList.remove('open'); modal.style.display = 'none'; }
        pinValues[id] = '';
        if (input) input.value = '';
        if (error) error.classList.remove('show');
        if (submit) submit.disabled = true;
        updatePinDots(id);
        document.body.style.overflow = '';
    };

    window.pinKey = function (id, digit) {
        let value = pinValues[id] || '';
        if (value.length >= PIN_LEN) return;
        value += digit;
        pinValues[id] = value;
        const input = document.getElementById('pinHiddenInput_' + id);
        const error = document.getElementById('pinError_' + id);
        const submit = document.getElementById('pinSubmitBtn_' + id);
        if (input) input.value = value;
        if (error) error.classList.remove('show');
        if (submit) submit.disabled = value.length < PIN_LEN;
        updatePinDots(id);
    };

    window.pinDelete = function (id) {
        let value = pinValues[id] || '';
        if (!value.length) return;
        value = value.slice(0, -1);
        pinValues[id] = value;
        const input = document.getElementById('pinHiddenInput_' + id);
        const error = document.getElementById('pinError_' + id);
        const submit = document.getElementById('pinSubmitBtn_' + id);
        if (input) input.value = value;
        if (error) error.classList.remove('show');
        if (submit) submit.disabled = value.length < PIN_LEN;
        updatePinDots(id);
    };

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.pin-backdrop.open[id^="pinModal_"]').forEach(function (modal) {
            const id = modal.id.replace('pinModal_', '');
            pinValues[id] = '';
            updatePinDots(id);
            document.body.style.overflow = 'hidden';
        });
        if (document.querySelector('.attr-insp-modal-backdrop[style*="display:flex"], .attr-insp-modal-backdrop[style*="display: flex"]')) {
            document.body.style.overflow = 'hidden';
        }
    });
})();
