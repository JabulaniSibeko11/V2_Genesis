/*
 * Genesis Signature Canvas
 * Attribute-style cursor mapping for Objection, Appeal and Section 78 forms.
 *
 * The older SignaturePad scripts remain loaded for compatibility with the
 * existing form JavaScript. These capture-phase handlers take ownership of
 * drawing so CSS scaling cannot move the ink away from the mouse/finger.
 */
(function () {
    'use strict';

    const canvas = document.getElementById('signature');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const hidden = document.getElementById('SignatureDataUrl');
    const status = document.getElementById('signatureStatus');
    const clearButton = document.getElementById('Clear');
    const submitButton = document.getElementById('submitForm');

    let drawing = false;
    let hasSignature = false;

    ctx.strokeStyle = '#1a1a1a';
    ctx.lineWidth = 2.5;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';

    function getPosition(event) {
        const rect = canvas.getBoundingClientRect();

        if (!rect.width || !rect.height) {
            return { x: 0, y: 0 };
        }

        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;

        const source =
            event.touches?.[0] ||
            event.changedTouches?.[0] ||
            event;

        return {
            x: (source.clientX - rect.left) * scaleX,
            y: (source.clientY - rect.top) * scaleY
        };
    }

    function markSigned() {
        hasSignature = true;

        if (status) {
            status.textContent = '✓ Signature captured';
            status.style.color = '#15803d';
            status.style.fontWeight = '600';
        }

        if (submitButton) {
            submitButton.disabled = false;
        }

        canvas.style.borderColor = '#15803d';
    }

    function saveSignature() {
        if (!hidden || !hasSignature) return;

        hidden.value = canvas.toDataURL('image/png');
        hidden.dispatchEvent(new Event('change', { bubbles: true }));
        hidden.dispatchEvent(new Event('input', { bubbles: true }));
    }

    function startDrawing(event) {
        event.preventDefault();
        event.stopImmediatePropagation();

        drawing = true;
        const point = getPosition(event);

        ctx.beginPath();
        ctx.moveTo(point.x, point.y);
    }

    function moveDrawing(event) {
        if (!drawing) {
            event.stopImmediatePropagation();
            return;
        }

        event.preventDefault();
        event.stopImmediatePropagation();

        const point = getPosition(event);
        ctx.lineTo(point.x, point.y);
        ctx.stroke();
        markSigned();
    }

    function stopDrawing(event) {
        if (event) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }

        if (!drawing) return;

        drawing = false;
        saveSignature();
    }

    // Capture phase is deliberate: it prevents the old SignaturePad listeners
    // from drawing a second, offset line.
    ['mousedown', 'touchstart'].forEach(function (name) {
        canvas.addEventListener(name, startDrawing, { capture: true, passive: false });
    });

    ['mousemove', 'touchmove'].forEach(function (name) {
        canvas.addEventListener(name, moveDrawing, { capture: true, passive: false });
    });

    ['mouseup', 'mouseleave', 'touchend', 'touchcancel'].forEach(function (name) {
        canvas.addEventListener(name, stopDrawing, { capture: true, passive: false });
    });

    if (clearButton) {
        clearButton.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopImmediatePropagation();

            ctx.clearRect(0, 0, canvas.width, canvas.height);
            hasSignature = false;

            if (hidden) {
                hidden.value = '';
                hidden.dispatchEvent(new Event('change', { bubbles: true }));
            }

            if (status) {
                status.textContent = 'No signature drawn';
                status.style.color = '#6b6b6b';
                status.style.fontWeight = '400';
            }

            if (submitButton) {
                submitButton.disabled = true;
            }

            canvas.style.borderColor = '#006570';
        }, true);
    }

    // If the draft-restoration script or server has supplied a signature,
    // restore it and keep the submit button enabled.
    function restoreExistingSignature() {
        if (!hidden?.value) return;

        const image = new Image();
        image.onload = function () {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            ctx.drawImage(image, 0, 0, canvas.width, canvas.height);
            hasSignature = true;
            markSigned();
        };
        image.src = hidden.value;
    }

    restoreExistingSignature();
})();
