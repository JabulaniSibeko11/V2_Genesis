


// ════════════════════════════════════════════════════════════════
//  1. DECLARATION — auto-populate date
// ════════════════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', function () {
    var dateEl = document.getElementById('declDate');
    if (dateEl) {
        var now = new Date();
        dateEl.value = now.toLocaleDateString('en-ZA', {
            year: 'numeric', month: 'long', day: 'numeric'
        }) + '  ' + now.toLocaleTimeString('en-ZA', {
            hour: '2-digit', minute: '2-digit'
        });
    }
});


// ════════════════════════════════════════════════════════════════
//  2. CANVAS SIGNATURE
// ════════════════════════════════════════════════════════════════
(function () {
    var canvas  = document.getElementById('sigCanvas');
    if (!canvas) return;

    var ctx     = canvas.getContext('2d');
    var drawing = false;
    var hasSig  = false;
    var status  = document.getElementById('sigStatus');
    var hiddenInput = document.getElementById('signatureData');

    // Set canvas resolution to match display size
    function resizeCanvas() {
        var rect = canvas.getBoundingClientRect();
        var scaleX = canvas.width  / rect.width;
        var scaleY = canvas.height / rect.height;
        return { scaleX, scaleY };
    }

    function getPos(e) {
        var rect = canvas.getBoundingClientRect();
        var scales = resizeCanvas();
        if (e.touches) {
            return {
                x: (e.touches[0].clientX - rect.left) * scales.scaleX,
                y: (e.touches[0].clientY - rect.top)  * scales.scaleY
            };
        }
        return {
            x: (e.clientX - rect.left) * scales.scaleX,
            y: (e.clientY - rect.top)  * scales.scaleY
        };
    }

    ctx.strokeStyle = '#1a1a1a';
    ctx.lineWidth   = 2.5;
    ctx.lineCap     = 'round';
    ctx.lineJoin    = 'round';

    // ── Mouse events ──────────────────────────────────────────
    canvas.addEventListener('mousedown', function (e) {
        drawing = true;
        var p = getPos(e);
        ctx.beginPath();
        ctx.moveTo(p.x, p.y);
    });

    canvas.addEventListener('mousemove', function (e) {
        if (!drawing) return;
        var p = getPos(e);
        ctx.lineTo(p.x, p.y);
        ctx.stroke();
        markSigned();
    });

    canvas.addEventListener('mouseup',    stopDraw);
    canvas.addEventListener('mouseleave', stopDraw);

    // ── Touch events (mobile) ─────────────────────────────────
    canvas.addEventListener('touchstart', function (e) {
        e.preventDefault();
        drawing = true;
        var p = getPos(e);
        ctx.beginPath();
        ctx.moveTo(p.x, p.y);
    }, { passive: false });

    canvas.addEventListener('touchmove', function (e) {
        e.preventDefault();
        if (!drawing) return;
        var p = getPos(e);
        ctx.lineTo(p.x, p.y);
        ctx.stroke();
        markSigned();
    }, { passive: false });

    canvas.addEventListener('touchend',   stopDraw);

    function stopDraw() {
        if (drawing) {
            drawing = false;
            saveSignature();
        }
    }

    function markSigned() {
        if (!hasSig) {
            hasSig = true;
            if (status) {
                status.textContent = '✓ Signature captured';
                status.style.color = '#15803d';
                status.style.fontWeight = '600';
            }
            canvas.style.borderColor = '#15803d';
        }
    }

    function saveSignature() {
        if (hiddenInput && hasSig) {
            hiddenInput.value = canvas.toDataURL('image/png');
        }
    }

    // ── Clear ─────────────────────────────────────────────────
    window.clearSignature = function () {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        hasSig = false;
        if (hiddenInput) hiddenInput.value = '';
        if (status) {
            status.textContent = 'No signature drawn';
            status.style.color = '#6b6b6b';
            status.style.fontWeight = '400';
        }
        canvas.style.borderColor = '#d0d0d0';
    };

    // ── Validate signature on submit ──────────────────────────
    var form = canvas.closest('form');
    if (form) {
        form.addEventListener('submit', function (e) {
            if (!hasSig) {
                e.preventDefault();
                canvas.style.borderColor = '#dc2626';
                canvas.style.boxShadow   = '0 0 0 3px rgba(220,38,38,.2)';
                canvas.scrollIntoView({ behavior: 'smooth', block: 'center' });
                alert('Please draw your signature before submitting.');
                return false;
            }
            saveSignature();
        });
    }
})();


// ════════════════════════════════════════════════════════════════
//  3. DYNAMIC ROWS — row templates + add/delete
// ════════════════════════════════════════════════════════════════

// ── Row counters (start at 2 — we already rendered rows 0 and 1) ──
var rowCounts = {
    'body-bus-buildings'  : 2,
    'body-bus-sections'   : 2,
    'body-drc-buildings'  : 2,
    'body-drc-improvements': 2,
    'body-drc-vacant'     : 2
};

function addRow(bodyId, templateFn) {
    var tbody = document.getElementById(bodyId);
    if (!tbody) return;

    var idx  = rowCounts[bodyId]++;
    var html = templateFn(idx);

    var tr = document.createElement('tr');
    tr.innerHTML = html;
    tbody.appendChild(tr);
}

function delRow(btn) {
    var tr = btn.closest('tr');
    if (!tr) return;

    var tbody = tr.closest('tbody');
    if (tbody.rows.length <= 1) {
        alert('At least one row is required.');
        return;
    }

    tr.remove();
    reindexTable(tbody);
}

// Reindex after delete so ASP.NET binding stays sequential
function reindexTable(tbody) {
    var rows = tbody.querySelectorAll('tr');
    rows.forEach(function (row, rowIdx) {
        row.querySelectorAll('input').forEach(function (input) {
            if (input.name) {
                input.name = input.name.replace(/\[\d+\]/, '[' + rowIdx + ']');
            }
        });
    });

    // Sync row counter
    var id = tbody.id;
    if (id && rowCounts.hasOwnProperty(id)) {
        rowCounts[id] = rows.length;
    }
}

// ── Row templates ─────────────────────────────────────────────

function busBuildingTemplate(i) {
    return `
        <td><input name="BusinessBuildings[${i}].BuildingNr"    class="attr-input" /></td>
        <td><input name="BusinessBuildings[${i}].Quality"       class="attr-input" /></td>
        <td><input name="BusinessBuildings[${i}].Condition"     class="attr-input" /></td>
        <td><input name="BusinessBuildings[${i}].YearBuilt"     class="attr-input" type="number" /></td>
        <td><input name="BusinessBuildings[${i}].Storeys"       class="attr-input" type="number" /></td>
        <td><input name="BusinessBuildings[${i}].Depreciation"  class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessBuildings[${i}].GBA"           class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessBuildings[${i}].Cost"          class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessBuildings[${i}].DRC"           class="attr-input" type="number" step="0.01" /></td>
        <td><button type="button" class="dyn-del-btn" onclick="delRow(this)" title="Remove row"><i class="fa-solid fa-xmark"></i></button></td>`;
}

function busSectionTemplate(i) {
    return `
        <td><input name="BusinessSections[${i}].BuildingNr"    class="attr-input" /></td>
        <td><input name="BusinessSections[${i}].Usage"         class="attr-input" /></td>
        <td><input name="BusinessSections[${i}].MarketGroup"   class="attr-input" /></td>
        <td><input name="BusinessSections[${i}].Quality"       class="attr-input" /></td>
        <td><input name="BusinessSections[${i}].GBA"           class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].NLA"           class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].CostRate"      class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Cost"          class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Rental"        class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Vac"           class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Exp"           class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Cap"           class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Gross"         class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Normalised"    class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Nett"          class="attr-input" type="number" step="0.01" /></td>
        <td><input name="BusinessSections[${i}].Value"         class="attr-input" type="number" step="0.01" /></td>
        <td><button type="button" class="dyn-del-btn" onclick="delRow(this)" title="Remove row"><i class="fa-solid fa-xmark"></i></button></td>`;
}

function drcBuildingTemplate(i) {
    return `
        <td><input name="DrcBuildings[${i}].BuildingDescription"    class="attr-input" /></td>
        <td><input name="DrcBuildings[${i}].Quality"                class="attr-input" /></td>
        <td><input name="DrcBuildings[${i}].GrossBuildingArea"      class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcBuildings[${i}].Condition"              class="attr-input" /></td>
        <td><input name="DrcBuildings[${i}].DepreciationPercentage" class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcBuildings[${i}].RatePerSQM"             class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcBuildings[${i}].DepreciatedRate"        class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcBuildings[${i}].ReplacementCost"        class="attr-input" type="number" step="0.01" /></td>
        <td><button type="button" class="dyn-del-btn" onclick="delRow(this)" title="Remove row"><i class="fa-solid fa-xmark"></i></button></td>`;
}

function drcImprovementTemplate(i) {
    return `
        <td><input name="DrcImprovements[${i}].ImprovementDescription" class="attr-input" /></td>
        <td><input name="DrcImprovements[${i}].Quality"                class="attr-input" /></td>
        <td><input name="DrcImprovements[${i}].AreaUnit"               class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcImprovements[${i}].Condition"              class="attr-input" /></td>
        <td><input name="DrcImprovements[${i}].DepreciationPercentage" class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcImprovements[${i}].RatePerSQM"             class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcImprovements[${i}].DepreciatedRate"        class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcImprovements[${i}].ReplacementCost"        class="attr-input" type="number" step="0.01" /></td>
        <td><button type="button" class="dyn-del-btn" onclick="delRow(this)" title="Remove row"><i class="fa-solid fa-xmark"></i></button></td>`;
}

function drcVacantTemplate(i) {
    return `
        <td><input name="DrcVacantLands[${i}].Region"          class="attr-input" /></td>
        <td><input name="DrcVacantLands[${i}].MinRatePerSQM"   class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcVacantLands[${i}].MidRatePerSQM"   class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcVacantLands[${i}].MaxRatePerSQM"   class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcVacantLands[${i}].Area"            class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcVacantLands[${i}].Rate"            class="attr-input" type="number" step="0.01" /></td>
        <td><input name="DrcVacantLands[${i}].VacantLandCost"  class="attr-input" type="number" step="0.01" /></td>
        <td><button type="button" class="dyn-del-btn" onclick="delRow(this)" title="Remove row"><i class="fa-solid fa-xmark"></i></button></td>`;
}


   