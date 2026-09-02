var property_key = sessionStorage.getItem('property_choice');
var objector_key = sessionStorage.getItem('objector_choice');
var reviewStatElement = document.getElementById('reviewStat');
var reviewStat = reviewStatElement ? reviewStatElement.value : 'Q';
var cd_o = 'false';
var cd_obj = 'false';
var cd_rep = 'false';
var NewChange = 'false';
var fo_o = 0;

// ============================================================
// SAFE DOM HELPERS
// Prevent form navigation from crashing when a field does not
// exist for the current property/form type.
// ============================================================
function focusIfExists(id) {
    const element = document.getElementById(id);
    if (element && typeof element.focus === 'function') {
        element.focus();
    }
}

function getElementIfExists(id) {
    return document.getElementById(id);
}

var loader = document.getElementById("preloader");

// ── Admin detection: server-side flag (no email regex needed here) ──
var isAdminFlag = document.getElementById('isAdminFlag')?.value === 'true';

// Keep userEmail + regex only for showInput() submit validation
var userEmailElement = document.getElementById('userEmail');
var userEmail = userEmailElement ? userEmailElement.value : '';
var regex = /^(val\.admin(1[0-9]?|[1-9])@joburg\.org\.za)$/i;

const objectorTypeField = document.getElementById('Objector_Type');
if (objectorTypeField) {
    objectorTypeField.value = sessionStorage.getItem('objector_choice');
}
const propertyTypeField = document.getElementById('Property_Type');
if (propertyTypeField) {
    propertyTypeField.value = sessionStorage.getItem('property_choice');
}

var temp_ot = sessionStorage.getItem('objector_choice');
var temp_pt = sessionStorage.getItem('property_choice');

// ── Hero helpers ────────────────────────────────────────────────────
function _heroSetForm(icon, label, subtitle, accentClass) {
    var sub = document.getElementById('obj-hero-sub');
    var badge = document.getElementById('obj-hero-badge');
    var badgeTxt = document.getElementById('obj-hero-badge-text');

    if (sub) sub.textContent = subtitle;
    if (badge) badge.className = 'obj-hero-type-badge ' + (accentClass || '');
    if (badgeTxt) badgeTxt.textContent = label;

    // Update the form-type badge icon
    var iconEl = badge?.querySelector('i');
    if (iconEl) iconEl.className = 'fa-solid ' + icon;
}

function _isReviewMode() {
    return reviewStat === 'R' || /^review$/i.test(reviewStat || '');
}

function _showSection78ReasonPage() {
    $('.div1').hide();
    $('.div781').show();
    $('#form_back').hide();

    var section78 = document.getElementById('section78_1');
    if (section78) section78.focus();
}

function pos_yes() {
    document.getElementById("o_p_1").value = document.getElementById("o_st_1").value;
    document.getElementById("o_p_2").value = document.getElementById("o_st_2").value;
    document.getElementById("o_p_3").value = document.getElementById("o_st_3").value;
    document.getElementById("o_p_4").value = document.getElementById("o_st_4").value;
    document.getElementById("o_p_5").value = document.getElementById("o_st_5").value;
}
function pos_no() {
    document.getElementById("o_p_1").value = "";
    document.getElementById("o_p_2").value = "";
    document.getElementById("o_p_3").value = "";
    document.getElementById("o_p_4").value = "";
    document.getElementById("o_p_5").value = "";
}

if (document.getElementById("AppealStat")) {
    document.getElementById("AppealStat").value = "False";
}
document.getElementById("o_pass").disabled = true;
$("#o_pass").hide();
$("#pass_input_L").hide();

function enable_ID() {
    document.getElementById("o_pass").disabled = true;
    $("#o_pass").hide();
    $("#pass_input_L").hide();
    document.getElementById("o_id").disabled = false;
    $("#o_id").show();
    $("#id_input_L").show();
}
function disable_ID() {
    document.getElementById("o_pass").disabled = false;
    $("#o_pass").show();
    $("#pass_input_L").show();
    document.getElementById("o_id").disabled = true;
    $("#o_id").hide();
    $("#id_input_L").hide();
    document.getElementById("id_status").innerHTML = '';
}
document.getElementById("objector_pass").disabled = true;
$("#objector_pass").hide();
$("#pass_L").hide();
function enable_ID2() {
    document.getElementById("objector_pass").disabled = true;
    $("#objector_pass").hide();
    $("#pass_L").hide();
    document.getElementById("objector_id").disabled = false;
    $("#objector_id").show();
    $("#id_L").show();
}
function disable_ID2() {
    document.getElementById("objector_pass").disabled = false;
    $("#objector_pass").show();
    $("#pass_L").show();
    document.getElementById("objector_id").disabled = true;
    $("#objector_id").hide();
    $("#id_L").hide();
    document.getElementById("obj_id_status").innerHTML = '';
}

var extentElement = document.getElementById('extent');
var mark = extentElement ? extentElement.value : '';
var valu = mark.toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
if (extentElement) extentElement.value = valu;

var date = new Date();
const signDateElement = document.getElementById('signDate');
if (signDateElement) signDateElement.value = date.toISOString().slice(0, 16);

var ext, ext2, fsize, fi;
const input = document.querySelector('#files');

if (input) {
input.addEventListener('change', (e) => {
    const files = input.files;
    if (files.length > 10) {
        input.value = "";
        alert(`Only 10 files are allowed to upload.`);
        return;
    }
    for (let i = 0; i < files.length; i++) {
        ext = input.files.item(i).name;
        ext2 = ext.split(".").pop().toLowerCase();
        switch (ext2) {
            case 'pdf': case 'jpeg': case 'jpg': case 'png': case 'heif':
                fsize = input.files.item(i).size;
                fi = Math.round((fsize / 1024));
                if (ext.length > 100) { alert("File name too long."); input.value = ''; break; }
                if (fi >= 10240) { alert("File too Big, please select a file less than 10.2mb."); input.value = ''; }
                break;
            default:
                alert('File type Not allowed. PDF, JPEG, JPG, PNG, HEIF only.');
                input.value = '';
                break;
        }
    }
});
}

function onlyNumberKey(evt) {
    var ASCIICode = (evt.which) ? evt.which : evt.keyCode;
    if (ASCIICode > 31 && (ASCIICode < 48 || ASCIICode > 57)) return false;
    return true;
}

function RSA() {
    if (sessionStorage.getItem('objector_choice') == "Owner") { $("#o_pass").hide(); $("#o_id").show(); }
    if (objector_key == "Third_Party") { $(".Div1-1").hide(); $(".Div1-3").hide(); }
    if (objector_key == "Representative") { $(".Div1-2").hide(); document.getElementById("owner_head").innerHTML = "1.1 OWNER DETAILS"; }
}
function foreigner() {
    if (sessionStorage.getItem('objector_choice') == "Owner") { $("#o_id").hide(); $("#o_pass").show(); }
}

var cat = ""; var Market_value = ""; var extent = "";
var desc = ""; var owner = ""; var objId = ""; var pin;

let isSubmittingObjectionForm = false;

function showInput() {
    if (isSubmittingObjectionForm) {
        return false;
    }

    // 1. Validate Section 6 before submit
    if (typeof section6ValidateBeforeNext === "function") {
        if (!section6ValidateBeforeNext()) {
            hideSubmitLoader();
            return false;
        }
    }

    // 2. Store property summary in sessionStorage
    const propertyDescEl = document.getElementById("Property_Desc");
    const marketValueEl = document.getElementById("Market_Value");
    const extentEl = document.getElementById("extent");
    const catEl = document.getElementById("cat");
    const ownerEl = document.getElementById("owner");

    desc = propertyDescEl ? propertyDescEl.value : "";
    Market_value = marketValueEl ? marketValueEl.value : "";
    extent = extentEl ? extentEl.value : "";
    Cat = catEl ? catEl.value : "";
    owner = ownerEl ? ownerEl.value : "";

    sessionStorage.setItem("desc", desc);
    sessionStorage.setItem("Market_Value", Market_value);
    sessionStorage.setItem("extent", extent);
    sessionStorage.setItem("cat", Cat);
    sessionStorage.setItem("owner", owner);

    // 3. Validate signature
    const signObj = document.getElementById("sign_obj");

    if (!signObj || signObj.value.trim() === "") {
        alert("Please sign the form before submitting.");
        hideSubmitLoader();
        return false;
    }

    // 4. Validate admin SAP only when admin
    const sapNo = document.getElementById("sapNo");

    if (isAdminFlag === true && (!sapNo || sapNo.value.trim() === "")) {
        alert("Admin SAP number is required before submitting.");
        hideSubmitLoader();
        return false;
    }

    // 5. Push raw money values before submit
    syncMoneyFieldsBeforeSubmit();

    // 6. Submit once only
    isSubmittingObjectionForm = true;

    showSubmitLoader();

    const submitButton = document.getElementById("submitForm");

    if (submitButton) {
        submitButton.disabled = true;
        submitButton.innerHTML = "Please wait...";
    }

    const form = document.getElementById("myForm");

    if (!form) {
        hideSubmitLoader();
        isSubmittingObjectionForm = false;
        alert("Form was not found. Please refresh and try again.");
        return false;
    }

    form.submit();

    return false;
}

function showSubmitLoader() {
    const overlay = document.getElementById("objLoaderOverlay");

    if (overlay) {
        overlay.classList.add("show");
        overlay.style.display = "flex";
    }

    document.body.classList.add("loading");
}

function hideSubmitLoader() {
    const overlay = document.getElementById("objLoaderOverlay");

    if (overlay) {
        overlay.classList.remove("show");
        overlay.style.display = "none";
    }

    document.body.classList.remove("loading");

    const submitButton = document.getElementById("submitForm");

    if (submitButton) {
        submitButton.disabled = false;
        submitButton.innerHTML = "Submit";
    }
}

String.prototype.reverse = function () { return this.split("").reverse().join(""); };

function reformatText(input) {
    var x = input.value;
    x = x.replace(/,/g, "");
    x = x.reverse();
    x = x.replace(/.../g, function (e) { return e + ","; });
    x = x.reverse();
    x = x.toString().replace(/^,/g, "");
    input.value = x;
    document.getElementById('Obj_Compensation_Amount').value = parseFloat(x.replace(/,/g, ''));
}

$(document).ready(function () {
    //Res Full Title
    $("#res_kitchins").hide(); $("#res_lounges").hide(); $("#res_dining_room").hide();
    $("#res_laundry").hide(); $("#res_study").hide(); $("#res_playroom").hide();
    $("#res_television").hide(); $("#res_separate_toilets").hide(); $("#res_lounge_dining_room").hide();
    //Res Sectional Title
    $("#res_st_kitchins").hide(); $("#res_st_lounges").hide(); $("#res_st_dining_room").hide();
    $("#res_st_laundry").hide(); $("#res_st_study").hide(); $("#res_st_playroom").hide();
    $("#res_st_television").hide(); $("#res_st_separate_toilets").hide(); $("#res_st_lounge_dining_room").hide();
    //Agric
    $("#agric_kitchins").hide(); $("#agric_lounges").hide(); $("#agric_dining_room").hide();
    $("#agric_laundry").hide(); $("#agric_study").hide(); $("#agric_playroom").hide();
    $("#agric_television").hide(); $("#agric_separate_toilets").hide(); $("#agric_lounge_dining_room").hide();
});

// ── Res Full Title ────────────────────────────────────────────────
function show_res_kitchins() { $("#res_kitchins").show(); document.getElementById("kitchen_one").innerHTML = 1; document.getElementById("kitchen_two").innerHTML = 2; document.getElementById("kitchen_three").innerHTML = 3; document.getElementById("kitchen_four").innerHTML = 4; document.getElementById("kitchen_five").innerHTML = 5; }
function hide_res_kitchins() { $("#res_kitchins").hide(); document.getElementById("kitchen_one").innerHTML = 0; document.getElementById("kitchen_two").innerHTML = 0; document.getElementById("kitchen_three").innerHTML = 0; document.getElementById("kitchen_four").innerHTML = 0; document.getElementById("kitchen_five").innerHTML = 0; }
function show_res_lounge() { $("#res_lounges").show(); document.getElementById("lounge_one").innerHTML = 1; document.getElementById("lounge_two").innerHTML = 2; document.getElementById("lounge_three").innerHTML = 3; document.getElementById("lounge_four").innerHTML = 4; document.getElementById("lounge_five").innerHTML = 5; }
function hide_res_lounge() { $("#res_lounges").hide(); document.getElementById("lounge_one").innerHTML = 0; document.getElementById("lounge_two").innerHTML = 0; document.getElementById("lounge_three").innerHTML = 0; document.getElementById("lounge_four").innerHTML = 0; document.getElementById("lounge_five").innerHTML = 0; }
function show_res_dining_room() { $("#res_dining_room").show(); document.getElementById("dining_room_one").innerHTML = 1; document.getElementById("dining_room_two").innerHTML = 2; document.getElementById("dining_room_three").innerHTML = 3; document.getElementById("dining_room_four").innerHTML = 4; document.getElementById("dining_room_five").innerHTML = 5; }
function hide_res_dining_room() { $("#res_dining_room").hide(); document.getElementById("dining_room_one").innerHTML = 0; document.getElementById("dining_room_two").innerHTML = 0; document.getElementById("dining_room_three").innerHTML = 0; document.getElementById("dining_room_four").innerHTML = 0; document.getElementById("dining_room_five").innerHTML = 0; }
function show_res_laundry() { $("#res_laundry").show(); document.getElementById("laundry_one").innerHTML = 1; document.getElementById("laundry_two").innerHTML = 2; document.getElementById("laundry_three").innerHTML = 3; document.getElementById("laundry_four").innerHTML = 4; document.getElementById("laundry_five").innerHTML = 5; }
function hide_res_laundry() { $("#res_laundry").hide(); document.getElementById("laundry_one").innerHTML = 0; document.getElementById("laundry_two").innerHTML = 0; document.getElementById("laundry_three").innerHTML = 0; document.getElementById("laundry_four").innerHTML = 0; document.getElementById("laundry_five").innerHTML = 0; }
function show_res_study() { $("#res_study").show(); document.getElementById("study_one").innerHTML = 1; document.getElementById("study_two").innerHTML = 2; document.getElementById("study_three").innerHTML = 3; document.getElementById("study_four").innerHTML = 4; document.getElementById("study_five").innerHTML = 5; }
function hide_res_study() { $("#res_study").hide(); document.getElementById("study_one").innerHTML = 0; document.getElementById("study_two").innerHTML = 0; document.getElementById("study_three").innerHTML = 0; document.getElementById("study_four").innerHTML = 0; document.getElementById("study_five").innerHTML = 0; }
function show_res_playroom() { $("#res_playroom").show(); document.getElementById("playroom_one").innerHTML = 1; document.getElementById("playroom_two").innerHTML = 2; document.getElementById("playroom_three").innerHTML = 3; document.getElementById("playroom_four").innerHTML = 4; document.getElementById("playroom_five").innerHTML = 5; }
function hide_res_playroom() { $("#res_playroom").hide(); document.getElementById("playroom_one").innerHTML = 0; document.getElementById("playroom_two").innerHTML = 0; document.getElementById("playroom_three").innerHTML = 0; document.getElementById("playroom_four").innerHTML = 0; document.getElementById("playroom_five").innerHTML = 0; }
function show_res_television() { $("#res_television").show(); document.getElementById("television_one").innerHTML = 1; document.getElementById("television_two").innerHTML = 2; document.getElementById("television_three").innerHTML = 3; document.getElementById("television_four").innerHTML = 4; document.getElementById("television_five").innerHTML = 5; }
function hide_res_television() { $("#res_television").hide(); }
function show_res_separate_toilets() { $("#res_separate_toilets").show(); document.getElementById("separate_toilets_one").innerHTML = 1; document.getElementById("separate_toilets_two").innerHTML = 2; document.getElementById("separate_toilets_three").innerHTML = 3; document.getElementById("separate_toilets_four").innerHTML = 4; document.getElementById("separate_toilets_five").innerHTML = 5; }
function hide_res_separate_toilets() { $("#res_separate_toilets").hide(); }
function show_res_lounge_dining_room() { $("#res_lounge_dining_room").show(); document.getElementById("lounge_dining_room_one").innerHTML = 1; document.getElementById("lounge_dining_room_two").innerHTML = 2; document.getElementById("lounge_dining_room_three").innerHTML = 3; document.getElementById("lounge_dining_room_four").innerHTML = 4; document.getElementById("lounge_dining_room_five").innerHTML = 5; }
function hide_res_lounge_dining_room() { $("#res_lounge_dining_room").hide(); }

// ── Res Sectional Title ────────────────────────────────────────────
function show_res_st_kitchins() { $("#res_st_kitchins").show(); document.getElementById("kitchen_st_one").innerHTML = 1; document.getElementById("kitchen_st_two").innerHTML = 2; document.getElementById("kitchen_st_three").innerHTML = 3; document.getElementById("kitchen_st_four").innerHTML = 4; document.getElementById("kitchen_st_five").innerHTML = 5; }
function hide_res_st_kitchins() { $("#res_st_kitchins").hide(); }
function show_res_st_lounge() { $("#res_st_lounges").show(); document.getElementById("lounge_st_one").innerHTML = 1; document.getElementById("lounge_st_two").innerHTML = 2; document.getElementById("lounge_st_three").innerHTML = 3; document.getElementById("lounge_st_four").innerHTML = 4; document.getElementById("lounge_st_five").innerHTML = 5; }
function hide_res_st_lounge() { $("#res_st_lounges").hide(); }
function show_res_st_dining_room() { $("#res_st_dining_room").show(); document.getElementById("dining_room_st_one").innerHTML = 1; document.getElementById("dining_room_st_two").innerHTML = 2; document.getElementById("dining_room_st_three").innerHTML = 3; document.getElementById("dining_room_st_four").innerHTML = 4; document.getElementById("dining_room_st_five").innerHTML = 5; }
function hide_res_st_dining_room() { $("#res_st_dining_room").hide(); }
function show_res_st_laundry() { $("#res_st_laundry").show(); document.getElementById("laundry_st_one").innerHTML = 1; document.getElementById("laundry_st_two").innerHTML = 2; document.getElementById("laundry_st_three").innerHTML = 3; document.getElementById("laundry_st_four").innerHTML = 4; document.getElementById("laundry_st_five").innerHTML = 5; }
function hide_res_st_laundry() { $("#res_st_laundry").hide(); }
function show_res_st_study() { $("#res_st_study").show(); document.getElementById("study_st_one").innerHTML = 1; document.getElementById("study_st_two").innerHTML = 2; document.getElementById("study_st_three").innerHTML = 3; document.getElementById("study_st_four").innerHTML = 4; document.getElementById("study_st_five").innerHTML = 5; }
function hide_res_st_study() { $("#res_st_study").hide(); }
function show_res_st_playroom() { $("#res_st_playroom").show(); document.getElementById("playroom_st_one").innerHTML = 1; document.getElementById("playroom_st_two").innerHTML = 2; document.getElementById("playroom_st_three").innerHTML = 3; document.getElementById("playroom_st_four").innerHTML = 4; document.getElementById("playroom_st_five").innerHTML = 5; }
function hide_res_st_playroom() { $("#res_st_playroom").hide(); }
function show_res_st_television() { $("#res_st_television").show(); document.getElementById("television_st_one").innerHTML = 1; document.getElementById("television_st_two").innerHTML = 2; document.getElementById("television_st_three").innerHTML = 3; document.getElementById("television_st_four").innerHTML = 4; document.getElementById("television_st_five").innerHTML = 5; }
function hide_res_st_television() { $("#res_st_television").hide(); }
function show_res_st_separate_toilets() { $("#res_st_separate_toilets").show(); document.getElementById("separate_toilets_st_one").innerHTML = 1; document.getElementById("separate_toilets_st_two").innerHTML = 2; document.getElementById("separate_toilets_st_three").innerHTML = 3; document.getElementById("separate_toilets_st_four").innerHTML = 4; document.getElementById("separate_toilets_st_five").innerHTML = 5; }
function hide_res_st_separate_toilets() { $("#res_st_separate_toilets").hide(); }
function show_res_st_lounge_dining_room() { $("#res_st_lounge_dining_room").show(); document.getElementById("lounge_dining_room_st_one").innerHTML = 1; document.getElementById("lounge_dining_room_st_two").innerHTML = 2; document.getElementById("lounge_dining_room_st_three").innerHTML = 3; document.getElementById("lounge_dining_room_st_four").innerHTML = 4; document.getElementById("lounge_dining_room_st_five").innerHTML = 5; }
function hide_res_st_lounge_dining_room() { $("#res_st_lounge_dining_room").hide(); }

// ── Agric ─────────────────────────────────────────────────────────
function show_agric_kitchins() { $("#agric_kitchins").show(); document.getElementById("agric_kitchen_one").innerHTML = 1; document.getElementById("agric_kitchen_two").innerHTML = 2; document.getElementById("agric_kitchen_three").innerHTML = 3; document.getElementById("agric_kitchen_four").innerHTML = 4; document.getElementById("agric_kitchen_five").innerHTML = 5; }
function hide_agric_kitchins() { $("#agric_kitchins").hide(); }
function show_agric_lounge() { $("#agric_lounges").show(); document.getElementById("agric_lounge_one").innerHTML = 1; document.getElementById("agric_lounge_two").innerHTML = 2; document.getElementById("agric_lounge_three").innerHTML = 3; document.getElementById("agric_lounge_four").innerHTML = 4; document.getElementById("agric_lounge_five").innerHTML = 5; }
function hide_agric_lounge() { $("#agric_lounges").hide(); }
function show_agric_dining_room() { $("#agric_dining_room").show(); document.getElementById("agric_dining_room_one").innerHTML = 1; document.getElementById("agric_dining_room_two").innerHTML = 2; document.getElementById("agric_dining_room_three").innerHTML = 3; document.getElementById("agric_dining_room_four").innerHTML = 4; document.getElementById("agric_dining_room_five").innerHTML = 5; }
function hide_agric_dining_room() { $("#agric_dining_room").hide(); }
function show_agric_laundry() { $("#agric_laundry").show(); document.getElementById("agric_laundry_one").innerHTML = 1; document.getElementById("agric_laundry_two").innerHTML = 2; document.getElementById("agric_laundry_three").innerHTML = 3; document.getElementById("agric_laundry_four").innerHTML = 4; document.getElementById("agric_laundry_five").innerHTML = 5; }
function hide_agric_laundry() { $("#agric_laundry").hide(); }
function show_agric_study() { $("#agric_study").show(); document.getElementById("agric_study_one").innerHTML = 1; document.getElementById("agric_study_two").innerHTML = 2; document.getElementById("agric_study_three").innerHTML = 3; document.getElementById("agric_study_four").innerHTML = 4; document.getElementById("agric_study_five").innerHTML = 5; }
function hide_agric_study() { $("#agric_study").hide(); }
function show_agric_playroom() { $("#agric_playroom").show(); document.getElementById("agric_playroom_one").innerHTML = 1; document.getElementById("agric_playroom_two").innerHTML = 2; document.getElementById("agric_playroom_three").innerHTML = 3; document.getElementById("agric_playroom_four").innerHTML = 4; document.getElementById("agric_playroom_five").innerHTML = 5; }
function hide_agric_playroom() { $("#agric_playroom").hide(); }
function show_agric_television() { $("#agric_television").show(); document.getElementById("agric_television_one").innerHTML = 1; document.getElementById("agric_television_two").innerHTML = 2; document.getElementById("agric_television_three").innerHTML = 3; document.getElementById("agric_television_four").innerHTML = 4; document.getElementById("agric_television_five").innerHTML = 5; }
function hide_agric_television() { $("#agric_television").hide(); }
function show_agric_separate_toilets() { $("#agric_separate_toilets").show(); document.getElementById("agric_separate_toilets_one").innerHTML = 1; document.getElementById("agric_separate_toilets_two").innerHTML = 2; document.getElementById("agric_separate_toilets_three").innerHTML = 3; document.getElementById("agric_separate_toilets_four").innerHTML = 4; document.getElementById("agric_separate_toilets_five").innerHTML = 5; }
function hide_agric_separate_toilets() { $("#agric_separate_toilets").hide(); }
function show_agric_lounge_dining_room() { $("#agric_lounge_dining_room").show(); document.getElementById("agric_lounge_dining_room_one").innerHTML = 1; document.getElementById("agric_lounge_dining_room_two").innerHTML = 2; document.getElementById("agric_lounge_dining_room_three").innerHTML = 3; document.getElementById("agric_lounge_dining_room_four").innerHTML = 4; document.getElementById("agric_lounge_dining_room_five").innerHTML = 5; }
function hide_agric_lounge_dining_room() { $("#agric_lounge_dining_room").hide(); }

$("textarea").keydown(function (e) { if (e.keyCode == 13) e.preventDefault(); });

function hide_div_af() { $("#affected-land").hide(); }
function show_div_af() { $("#affected-land").show(); }
function hide_div_wr() { $("#water-right").hide(); }
function show_div_wr() { $("#water-right").show(); }

// ══════════════════════════════════════════════════════════════════
//  load() — runs on page ready, sets sections + updates hero
// ══════════════════════════════════════════════════════════════════
function load() {
    var isReview = _isReviewMode();

    // ── Residential ───────────────────────────────────────────────
    if (property_key == "Res") {
        $("#section1").show(); $("#section2").show();
        $("#section3-agric").hide(); $("#section3-bus").hide(); $("#section4-bus").hide();
        $("#section3-res").show(); $("#section4-res").show();
        $(".div3_R").toggle(2000); $(".div4_R").toggle(2000);
        document.getElementById("form_head").innerHTML =
            "FORM A: RESIDENTIAL (FULL TITLE AND SECTIONAL TITLE USED FOR RESIDENTIAL PURPOSES)";

        // Hero update
        _heroSetForm(
            isReview ? "fa-magnifying-glass-chart" : "fa-house",
            isReview ? "Section 78 Residential Review" : "Section 78 Residential Query",
            isReview
                ? "Review of the Section 78 outcome for this residential property"
                : "Query concerning the valuation record for this residential property",
            "obj-hero-badge--res"
        );
    }

    // ── Agricultural ──────────────────────────────────────────────
    if (property_key == "Agric") {
        $("#section1").show(); $("#section2").show();
        $("#section3-agric").show(); $("#section3-bus").hide();
        $("#section4-bus").hide(); $("#section3-res").hide(); $("#section4-res").hide();
        $(".div3_A").toggle(2000);
        document.getElementById("form_head").innerHTML =
            "FORM C: AGRICULTURAL HOLDINGS OR FARMS";

        // Hero update
        _heroSetForm(
            isReview ? "fa-magnifying-glass-chart" : "fa-tractor",
            isReview ? "Section 78 Agricultural Review" : "Section 78 Agricultural Query",
            isReview
                ? "Review of the Section 78 outcome for this agricultural property"
                : "Query concerning the valuation record for this agricultural property",
            "obj-hero-badge--agric"
        );
    }

    // ── Business / Commercial ─────────────────────────────────────
    if (property_key == "Bus") {
        $("#section1").show(); $("#section2").show();
        $("#section3-agric").hide(); $("#section3-res").hide();
        $("#section3-bus").show(); $("#section4-bus").show(); $("#section4-res").hide();
        $(".div3_B").toggle(2000); $(".div4_B").toggle(2000);
        document.getElementById("form_head").innerHTML =
            "FORM B: PROPERTIES OTHER THAN RESIDENTIAL OR AGRICULTURAL (e.g. BUSINESSES, FACTORIES, OFFICES, SCHOOLS)";

        // Hero update
        _heroSetForm(
            isReview ? "fa-magnifying-glass-chart" : "fa-building",
            isReview ? "Section 78 Commercial Review" : "Section 78 Commercial Query",
            isReview
                ? "Review of the Section 78 outcome for this commercial property"
                : "Query concerning the valuation record for this commercial property",
            "obj-hero-badge--bus"
        );
    }

    // ── Objector type ─────────────────────────────────────────────
    if (objector_key == "Owner") {
        $(".Div1-2").hide(); $(".Div1-3").hide();
        document.getElementById("o_name_l").innerHTML = 'REGISTERED OWNER OF PROPERTY';
    }
    if (objector_key == "Third_Party") {
        $(".Div1-1").hide(); $(".Div1-3").hide();
    }
    if (objector_key == "Representative") {
        $(".Div1-2").hide();
        $("#owner_details").hide();
        document.getElementById("o_name_l").innerHTML =
            '<span style="color:red;">*</span>REGISTERED OWNER OF PROPERTY (<span style="color:red;">required</span>)';
        document.getElementById("owner_head").innerHTML = "1.1 OWNER DETAILS";
    }
}

function hide_div_af() { $("#affected-land").hide(); }
function show_div_af() { $("#affected-land").show(); }
function hide_div_wr() { $("#water-right").hide(); }
function show_div_wr() { $("#water-right").show(); }

function LuhnAlgo() {
    var stat_Value;
    var id;
    if (objector_key == 'Owner' && document.getElementById("o_id").disabled == false) id = document.getElementById('o_id').value;
    if (objector_key == 'Third_Party' && document.getElementById("objector_id").disabled == false) id = document.getElementById('objector_id').value;
    if (objector_key == 'Owner' && document.getElementById("o_id").disabled == true) id = document.getElementById('o_pass').value;
    if (objector_key == 'Third_Party' && document.getElementById("objector_id").disabled == true) id = document.getElementById('objector_pass').value;
    if (!id) return '';

    var id_stat = '';
    var arr = id.split('');
    var sum = 0;
    var n = arr.length;
    for (var i = 0; i < n; i++) arr[i] = parseInt(arr[i]);
    for (var i = 1; i < n; i = i + 2) { var v = arr[n - 1 - i] * 2; arr[n - 1 - i] = (v > 9) ? v - 9 : v; }
    for (var i = 0; i < n; i++) sum += arr[i];

    if (sum % 10 === 0 && id !== '' && id !== '0000000000000' && id.length == 13) {
        id_stat = '';
        if (objector_key == 'Owner') { document.getElementById("id_status").innerHTML = id_stat; stat_Value = id_stat; }
        if (objector_key == 'Third_Party') { document.getElementById("obj_id_status").innerHTML = id_stat; stat_Value = id_stat; }
        return stat_Value;
    } else {
        id_stat = 'Invalid ID Number';
        if (objector_key == 'Owner' && document.getElementById("o_id").disabled == false) { document.getElementById("id_status").innerHTML = id_stat; stat_Value = id_stat; }
        if (objector_key == 'Third_Party' && document.getElementById("objector_id").disabled == false) { document.getElementById("obj_id_status").innerHTML = id_stat; stat_Value = id_stat; }
        if (objector_key == 'Owner' && document.getElementById("o_id").disabled == true) { id_stat = ''; document.getElementById("id_status").innerHTML = id_stat; return id_stat; }
        if (objector_key == 'Third_Party' && document.getElementById("objector_id").disabled == true) { id_stat = ''; document.getElementById("obj_id_status").innerHTML = id_stat; return id_stat; }
        return stat_Value;
    }
}

// ── Phone validation helper ────────────────────────────────────────
function _validatePhone(inputId) {
    var val = document.getElementById(inputId).value;
    if (val === '') return true;
    var err = '';
    if (!val.startsWith('0')) err = "Phone number must start with 0 and be exactly 10 digits.";
    else if (val.length < 10) err = "Phone number must be exactly 10 digits starting with 0.";
    else if (val.length > 10) err = "Phone number cannot be more than 10 digits.";
    else if (!/^\d+$/.test(val)) err = "Phone number must contain only digits.";
    if (err) {
        document.getElementById(inputId).style.border = "2px solid red";
        var span = document.createElement("span");
        span.innerText = err; span.style.color = "red"; span.id = "ph_err_" + inputId;
        document.getElementById(inputId).parentNode.insertBefore(span, document.getElementById(inputId).nextSibling);
        document.getElementById(inputId).focus();
        setTimeout(function () { var s = document.getElementById("ph_err_" + inputId); if (s) s.remove(); }, 5000);
        return false;
    }
    document.getElementById(inputId).style.border = "";
    return true;
}

$(document).ready(function () {
    $(".div781").hide(); $(".div2").hide(); $(".div5").hide(); $(".div6").hide(); $(".divU").hide(); $(".div7").hide();

    $(".btn_n1").click(function () {
        if (objector_key == "Owner") {
            var emailVal = document.getElementById("o_cd_5").value;
            var emailPat = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
            if (!emailVal || !emailPat.test(emailVal)) { document.getElementById("o_cd_5").style.border = "2px solid red"; fo_o = 4; return false; }
            else document.getElementById("o_cd_5").style.border = "";

            var fields = ["o_p_1", "o_p_2", "o_p_3", "o_p_4", "o_st_1", "o_st_2", "o_st_3", "o_st_4"];
            fields.forEach(id => { var el = document.getElementById(id); if (!el.value) { el.style.border = "2px solid red"; fo_o = 2; } else el.style.border = ""; });
            if (!document.getElementById("o_p_5").value || document.getElementById("o_p_5").value.length < 4) { document.getElementById("o_p_5").style.border = "2px solid red"; fo_o = 3; } else { document.getElementById("o_p_5").style.border = ""; fo_o = 0; }
            if (!document.getElementById("o_st_5").value || document.getElementById("o_st_5").value.length < 4) { document.getElementById("o_st_5").style.border = "2px solid red"; fo_o = 2; } else { document.getElementById("o_st_5").style.border = ""; fo_o = 0; }

            if (LuhnAlgo() == 'Invalid ID Number') { document.getElementById("o_id").style.border = "2px solid red"; focusIfExists("o_id"); alert("Invalid ID Number"); }
            else document.getElementById("o_id").style.border = "";

            if (!document.getElementById("o_cd_1").value && !document.getElementById("o_cd_2").value && !document.getElementById("o_cd_3").value && !document.getElementById("o_cd_4").value) {
                document.getElementById("o_cd_invalid").innerHTML = "Please fill at least one of the contact details fields."; document.getElementById("o_cd_invalid").style.color = "red"; return false;
            } else { cd_o = 'true'; document.getElementById("o_cd_invalid").innerHTML = ""; }

            if (!_validatePhone("o_cd_2")) return false;
            if (!_validatePhone("o_cd_3")) return false;

            if (fo_o == 0 && LuhnAlgo() !== 'Invalid ID Number' && cd_o == 'true') {
                _showSection78ReasonPage();
            }
        }

        if (objector_key == "Third_Party") {
            if (!document.getElementById("objector_name").value) { document.getElementById("objector_name").style.border = "2px solid red"; focusIfExists("objector_name"); } else document.getElementById("objector_name").style.border = "";
            if (LuhnAlgo() == "Invalid ID Number") { alert("Invalid ID Number"); document.getElementById("objector_id").style.border = "2px solid red"; } else document.getElementById("objector_id").style.border = "";
            var tpFields = ["obj_p_1", "obj_p_2", "obj_p_3", "obj_p_4", "objector_stat"];
            tpFields.forEach(id => { var el = document.getElementById(id); if (!el.value) el.style.border = "2px solid red"; else el.style.border = ""; });
            if (!document.getElementById("obj_p_5").value || document.getElementById("obj_p_5").value.length < 4) document.getElementById("obj_p_5").style.border = "2px solid red"; else document.getElementById("obj_p_5").style.border = "";
            var tpEmail = document.getElementById("obj_cd_5").value;
            if (!tpEmail || !/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(tpEmail)) { document.getElementById("obj_cd_5").style.border = "2px solid red"; return false; } else document.getElementById("obj_cd_5").style.border = "";
            if (!document.getElementById("obj_cd_1").value && !document.getElementById("obj_cd_2").value && !document.getElementById("obj_cd_3").value && !document.getElementById("obj_cd_4").value) {
                document.getElementById("obj_cd_invalid").innerHTML = "Please fill at least one of the contact details fields."; document.getElementById("obj_cd_invalid").style.color = "red"; return false;
            } else { cd_obj = 'true'; document.getElementById("obj_cd_invalid").innerHTML = ""; }
            if (!_validatePhone("obj_cd_2")) return false;
            if (!_validatePhone("obj_cd_3")) return false;
            if (document.getElementById("objector_name").value && document.getElementById("obj_p_1").value && document.getElementById("obj_p_5").value && document.getElementById("objector_stat").value && cd_obj == 'true' && LuhnAlgo() !== 'Invalid ID Number') {
                _showSection78ReasonPage();
            }
        }

        if (objector_key == "Representative") {
            ["rep_name", "o_name", "rep_p_1", "rep_p_2", "rep_p_3", "rep_p_4"].forEach(id => { var el = document.getElementById(id); if (!el.value) el.style.border = "2px solid red"; else el.style.border = ""; });
            if (!document.getElementById("rep_p_5").value || document.getElementById("rep_p_5").value.length < 4) document.getElementById("rep_p_5").style.border = "2px solid red"; else document.getElementById("rep_p_5").style.border = "";
            var ownerEmail = document.getElementById("o_cd_5").value;
            if (!ownerEmail || !/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(ownerEmail)) { document.getElementById("o_cd_5").style.border = "2px solid red"; return false; } else document.getElementById("o_cd_5").style.border = "";
            var repEmail = document.getElementById("rep_cd_5").value;
            if (!repEmail || !/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(repEmail)) { document.getElementById("rep_cd_5").style.border = "2px solid red"; return false; } else document.getElementById("rep_cd_5").style.border = "";
            if (document.getElementById("fileR").files.length == 0) { document.getElementById("fileR").style.border = "2px solid red"; alert("Representative must upload their Authorization Letter to proceed."); } else { if (document.getElementById("fileR").files.item(0).name.length > 100) { alert("File name too long."); document.getElementById("fileR").value = ''; } else document.getElementById("fileR").style.border = ""; }
            if (!document.getElementById("rep_cd_1").value && !document.getElementById("rep_cd_2").value && !document.getElementById("rep_cd_3").value && !document.getElementById("rep_cd_4").value) { document.getElementById("rep_cd_invalid").innerHTML = "Please fill at least one contact field."; document.getElementById("rep_cd_invalid").style.color = "red"; return false; } else { cd_rep = 'true'; document.getElementById("rep_cd_invalid").innerHTML = ""; }
            if (!_validatePhone("rep_cd_2")) return false;
            if (!_validatePhone("rep_cd_3")) return false;
            if (!document.getElementById("o_cd_1").value && !document.getElementById("o_cd_2").value && !document.getElementById("o_cd_3").value && !document.getElementById("o_cd_4").value) { document.getElementById("o_cd_invalid").innerHTML = "Please fill at least one contact field."; document.getElementById("o_cd_invalid").style.color = "red"; return false; } else { cd_o = 'true'; document.getElementById("o_cd_invalid").innerHTML = ""; }
            if (!_validatePhone("o_cd_2")) return false;
            if (!_validatePhone("o_cd_3")) return false;
            if (document.getElementById("rep_name").value && document.getElementById("o_name").value && document.getElementById("rep_p_1").value && document.getElementById("rep_p_5").value && document.getElementById("fileR").files.length !== 0 && cd_rep == 'true' && cd_o == 'true') {
                _showSection78ReasonPage();
            }
        }
    });

    $(".btn_pS78").click(function () {
        $(".div781").hide();
        $(".div1").show();
        $("#form_back").show();
    });

    $(".btn_nS782").click(function () {
        var selectedReason = document.querySelector(
            '#section78_1 input[name^="Option_"]:checked'
        );
        var motivation = document.querySelector(
            '#section78_1 textarea[name="Motivation_for_Supp_Request"]'
        );

        if (!selectedReason) {
            alert('Please select at least one Section 78 reason before continuing.');
            return false;
        }

        if (!motivation || !motivation.value.trim()) {
            if (motivation) {
                motivation.style.border = '2px solid red';
                motivation.focus();
            }
            alert('Please provide the reason or motivation for this Section 78 request.');
            return false;
        }

        motivation.style.border = '';
        $(".div781").hide();
        $(".div2").show();
        var postalCode = document.getElementById("phy_c");
        if (postalCode) postalCode.focus();
        return false;
    });

    $(".btn_p2").click(function () {
        $(".div2").hide();
        $(".div781").show();
    });
    $(".btn_n2").click(function () {
        if (!document.getElementById("phy_c").value) { document.getElementById("phy_c").style.border = "2px solid red"; return; }
        document.getElementById("phy_c").style.border = "";
        $(".div2").hide();
        if (property_key == "Res") { $(".div3_R").show(); focusIfExists("s3r"); }
        if (property_key == "Agric") { $(".div3_A").show(); focusIfExists("s3a"); }
        if (property_key == "Bus") { $(".div3_B").show(); focusIfExists("s3b"); }
    });

    $(".btn_p3").click(function () {
        $(".div2").show();
        if (property_key == "Res") $(".div3_R").hide();
        if (property_key == "Agric") $(".div3_A").hide();
        if (property_key == "Bus") $(".div3_B").hide();
    });
    $(".btn_n3").click(function () {
        if (property_key == "Res") { $(".div3_R").hide(); $(".div4_R").show(); focusIfExists("sch_name"); }
        if (property_key == "Agric") { $(".div3_A").hide(); $(".div5").show(); focusIfExists("s5"); }
        if (property_key == "Bus") { $(".div3_B").hide(); $(".div4_B").show(); focusIfExists("sch_name_b"); }
    });

    $(".btn_p4").click(function () {
        if (property_key == "Res") { $(".div3_R").show(); $(".div4_R").hide(); }
        if (property_key == "Bus") { $(".div3_B").show(); $(".div4_B").hide(); }
    });
    $(".btn_n4").click(function () {
        if (property_key == "Res") { $(".div4_R").hide(); $(".div5").show(); focusIfExists("s5"); }
        if (property_key == "Bus") { $(".div4_B").hide(); $(".div5").show(); focusIfExists("s5"); }
    });

    $(".btn_p5").click(function () {
        if (property_key == "Res") { $(".div4_R").show(); $(".div5").hide(); }
        if (property_key == "Agric") { $(".div3_A").show(); $(".div5").hide(); }
        if (property_key == "Bus") { $(".div4_B").show(); $(".div5").hide(); }
    });
    $(".btn_n5").click(function () { $(".div5").hide(); $(".div6").show(); focusIfExists("NewPropDesc"); });

    $(".btn_p6").click(function () { $(".div5").show(); $(".div6").hide(); });

    //$(".btn_n6").click(function () {
    //    var anyFilled = ['NewCat', 'NewMarketValue', 'NewExtent', 'NewPropDesc', 'NewAddress', 'NewOwner']
    //        .some(id => document.getElementById(id)?.value);
    //    if (!anyFilled) {
    //        document.getElementById("new_change_invalid").innerHTML = "Please fill at least one of the changes you want to make.";
    //        document.getElementById("new_change_invalid").style.color = "red";
    //        ['NewCat', 'NewMarketValue', 'NewExtent', 'NewPropDesc', 'NewAddress', 'NewOwner'].forEach(id => { document.getElementById(id).style.border = "2px solid red"; });
    //    } else {
    //        NewChange = 'true';
    //        document.getElementById("new_change_invalid").innerHTML = "";
    //        ['NewCat', 'NewMarketValue', 'NewExtent', 'NewPropDesc', 'NewAddress', 'NewOwner'].forEach(id => { document.getElementById(id).style.border = ""; });
    //        $(".div6").hide(); $(".divU").show(); focusIfExists("sectionUpload");
    //    }
    //});

    $(".btn_n6").off("click").on("click", function (e) {
        e.preventDefault();
        e.stopImmediatePropagation();

        if (!section6ValidateBeforeNext()) {
            return false;
        }

        NewChange = 'true';

        const invalid = document.getElementById("new_change_invalid");
        if (invalid) {
            invalid.innerHTML = "";
            invalid.style.color = "";
        }

        $(".div6").hide();
        $(".divU").show();

        const upload = document.getElementById("sectionUpload");
        if (upload) upload.focus();

        return false;
    });

    $(".btn_pU").click(function () { $(".div6").show(); $(".divU").hide(); });
    $(".btn_nU").click(function () { $(".divU").hide(); $(".div7").show(); focusIfExists("sign_obj"); });
    $(".btn_p7").click(function () { $(".divU").show(); $(".div7").hide(); });
});

$(function () {
    var canvas = document.querySelector('#signature');
    var pad = new SignaturePad(canvas);
    var data;
    function checkCanvas() {
        var canva = document.getElementById('signature');
        if (isCanvasEmpty(canva)) { document.getElementById("signature").style.border = "2px solid red"; }
        else { data = pad.toDataURL(); pad.off(); $('#savetarget').attr('src', data); $('#SignatureDataUrl').val(data); $('#submitForm').removeAttr('disabled'); document.getElementById("signature").style.border = "2px solid Black"; }
    }
    function isCanvasEmpty(canvas) { const b = document.createElement('canvas'); b.width = canvas.width; b.height = canvas.height; return canvas.toDataURL() === b.toDataURL(); }
    $('#accept').click(function () { checkCanvas(); });
    $('#Clear').click(function () { pad = new SignaturePad(canvas); pad.on(); document.getElementById("submitForm").disabled = true; });
});

$(document).ready(function () {
    $("#affected-land").hide();
    $("#water-right").hide();
});

$("input[data-type='currency']").on({ keyup: function () { formatCurrency($(this)); }, blur: function () { formatCurrency($(this), "blur"); } });

function formatNumber(n) { return n.replace(/\D/g, "").replace(/\B(?=(\d{3})+(?!\d))/g, " "); }
function formatCurrency(input) {
    var text = String(input.val() || "")
        .replace(/[Rr]/g, "")
        .replace(/[\s,\u00a0\u202f]/g, "");
    if (!text) { input.val(""); return; }

    var amount = Number(text);
    if (!Number.isFinite(amount) || amount < 0) { return; }

    var formatted = "R " + Math.round(amount)
        .toLocaleString("en-ZA", { minimumFractionDigits: 0, maximumFractionDigits: 0 })
        .replace(/[,\u00a0\u202f]/g, " ");
    input.val(formatted);

    if (input[0] && input[0].setSelectionRange) {
        input[0].setSelectionRange(formatted.length, formatted.length);
    }
}

// ═══════════════════════════════════════════════════════════════
// SECTION 6 VALIDATION — Objection / Appeal / Query / Review
// Prevent same values as Valuation Roll / MVD
// ═══════════════════════════════════════════════════════════════

function getSubmissionMode() {
    return _isReviewMode() ? "Review" : "Query";
}

function normaliseText(value) {
    if (!value) return "";

    return value
        .toString()
        .trim()
        .toLowerCase()
        .replace(/\s+/g, " ")
        .replace(/[.,;:]+$/g, "");
}

function normaliseCategory(value) {
    let v = normaliseText(value);

    const map = {
        "residential": "residential property",
        "residential property": "residential property",

        "business": "business and commercial",
        "commercial": "business and commercial",
        "business and commercial": "business and commercial",

        "agric": "agricultural",
        "agriculture": "agricultural",
        "agricultural": "agricultural",

        "multipurpose": "multipurpose",
        "multipurpose*": "multipurpose",
        "multi purpose": "multipurpose",
        "multi-purpose": "multipurpose",

        "vacant": "vacant land",
        "vacant land": "vacant land"
    };

    return map[v] || v;
}

function normaliseNumber(value) {
    if (!value) return "";

    let cleaned = value
        .toString()
        .replace(/R/gi, "")
        .replace(/\s/g, "")
        .replace(/,/g, "")
        .trim();

    if (cleaned === "") return "";

    const num = Number(cleaned);

    if (Number.isNaN(num)) {
        return cleaned.toLowerCase();
    }

    return num.toString();
}

function normaliseExtent(value) {
    if (!value) return "";

    let cleaned = value
        .toString()
        .replace(/\s/g, "")
        .replace(/,/g, ".")
        .trim();

    const num = Number(cleaned);

    if (Number.isNaN(num)) {
        return cleaned.toLowerCase();
    }

    return num.toString();
}

function section6GetFieldPairs() {
    return [
        {
            label: "Description of the Property/Unit",
            newId: "NewPropDesc",
            oldId: "desc",
            type: "text"
        },
        {
            label: "Category",
            newId: "NewCat",
            oldId: "cat",
            type: "category"
        },
        {
            label: "Physical Address / Door No. / Flat No.",
            newId: "NewAddress",
            oldId: "add",
            type: "text"
        },
        {
            label: "Extent",
            newId: "NewExtent",
            oldId: "extent",
            type: "extent"
        },
        {
            label: "Market Value",
            newId: "NewMarketValue",
            oldId: "Market_Value",
            type: "market"
        },
        {
            label: "Name of Owner",
            newId: "NewOwner",
            oldId: "owner",
            type: "text"
        }
    ];
}

function section6Normalise(value, type) {
    if (type === "category") return normaliseCategory(value);
    if (type === "market") return normaliseNumber(value);
    if (type === "extent") return normaliseExtent(value);

    return normaliseText(value);
}

function section6ClearFieldError(input) {
    if (!input) return;

    input.style.border = "";
    input.style.backgroundColor = "";

    const err = document.getElementById(input.id + "_same_error");
    if (err) err.remove();
}

function section6SetFieldError(input, message) {
    if (!input) return;

    input.style.border = "2px solid #d00000";
    input.style.backgroundColor = "#fff1f1";

    const errorId = input.id + "_same_error";
    let err = document.getElementById(errorId);

    if (!err) {
        err = document.createElement("span");
        err.id = errorId;
        err.style.display = "block";
        err.style.color = "#d00000";
        err.style.fontSize = "12px";
        err.style.fontWeight = "700";
        err.style.marginTop = "-8px";
        err.style.marginBottom = "10px";
        input.parentNode.insertBefore(err, input.nextSibling);
    }

    err.innerText = message;
}

function section6ValidateSameValues() {
    const duplicates = [];

    section6GetFieldPairs().forEach(pair => {
        const newEl = document.getElementById(pair.newId);
        const oldEl = document.getElementById(pair.oldId);

        if (!newEl || !oldEl) return;

        section6ClearFieldError(newEl);

        const newRaw = newEl.value || "";
        const oldRaw = oldEl.value || "";

        if (newRaw.trim() === "") return;

        const newValue = section6Normalise(newRaw, pair.type);
        const oldValue = section6Normalise(oldRaw, pair.type);

        if (newValue !== "" && oldValue !== "" && newValue === oldValue) {
            duplicates.push(pair);

            section6SetFieldError(
                newEl,
                "This value is the same as the value on the Valuation Roll / MVD. Please enter a different value."
            );
        }
    });

    return duplicates;
}

function section6HasAtLeastOneChange() {
    return section6GetFieldPairs().some(pair => {
        const el = document.getElementById(pair.newId);
        return el && el.value.trim() !== "";
    });
}

function section6FormatRandInput(input) {
    if (!input) return;

    let raw = normaliseMoney(input.value);

    if (!raw) {
        input.value = "";
        return;
    }

    const amount = parseInt(raw, 10);

    if (Number.isNaN(amount)) {
        input.value = "";
        return;
    }

    input.value = "R " + amount.toLocaleString("en-ZA").replace(/[,\u00a0\u202f]/g, " ");
}

function section6FormatOriginalMarketValue() {
    const oldMv = document.getElementById("Market_Value");

    if (!oldMv || !oldMv.value) return;

    let raw = normaliseMoney(oldMv.value);

    if (!raw) return;

    oldMv.value = "R " + parseInt(raw, 10).toLocaleString("en-ZA").replace(/[,\u00a0\u202f]/g, " ");
}

function ensureLocusStandModalExists() {
    if (document.getElementById("locusStandModal")) return;

    const modal = document.createElement("div");
    modal.id = "locusStandModal";
    modal.className = "locus-modal-backdrop";
    modal.style.display = "none";

    modal.innerHTML = `
        <div class="locus-modal">
            <div class="locus-modal-header">
                <div class="locus-modal-icon">
                    <i class="fa-solid fa-triangle-exclamation"></i>
                </div>
                <div>
                    <h3 id="locusModalTitle">Locus Standi Required</h3>
                    <p id="locusModalSub">Appeal validation failed</p>
                </div>
            </div>

            <div class="locus-modal-body">
                <p id="locusModalMessage">
                    You cannot continue with the same details that are reflected on the Valuation Roll / MVD.
                </p>

                <div id="locusDuplicateList" class="locus-duplicate-list"></div>
            </div>

            <div class="locus-modal-footer">
                <button type="button" onclick="closeLocusStandModal()" class="locus-modal-btn">
                    Okay, I will update the values
                </button>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
}

function showLocusStandModal(mode, duplicates) {
    ensureLocusStandModalExists();

    const modal = document.getElementById("locusStandModal");
    const title = document.getElementById("locusModalTitle");
    const sub = document.getElementById("locusModalSub");
    const msg = document.getElementById("locusModalMessage");
    const list = document.getElementById("locusDuplicateList");

    if (mode === "Appeal") {
        title.innerText = "Locus Standi Required";
        sub.innerText = "Appeal cannot continue with unchanged MVD details";
        msg.innerText =
            "You cannot appeal with the same details that are reflected on the Municipal Valuation Roll / MVD. Please enter different values before continuing.";
    } else if (mode === "Review") {
        title.innerText = "Review Details Required";
        sub.innerText = "Review cannot continue with unchanged MVD details";
        msg.innerText =
            "You cannot submit a review using the same details that are reflected on the Municipal Valuation Roll / MVD. Please enter different values before continuing.";
    } else if (mode === "Query") {
        title.innerText = "Query Details Required";
        sub.innerText = "Query cannot continue with unchanged MVD details";
        msg.innerText =
            "You cannot submit a query using the same details that are reflected on the Municipal Valuation Roll / MVD. Please enter different values before continuing.";
    } else {
        title.innerText = "Different Values Required";
        sub.innerText = "Objection cannot continue with unchanged roll details";
        msg.innerText =
            "You cannot lodge an objection using the same details that are reflected on the Valuation Roll. Please enter different values before continuing.";
    }

    if (list) {
        if (duplicates && duplicates.length > 0) {
            list.style.display = "block";
            list.innerHTML =
                "<strong>Same values found:</strong><br/>" +
                duplicates.map(x => "• " + x.label).join("<br/>");
        } else {
            list.style.display = "none";
            list.innerHTML = "";
        }
    }

    modal.style.display = "flex";
    document.body.style.overflow = "hidden";
}

function closeLocusStandModal() {
    const modal = document.getElementById("locusStandModal");
    if (modal) modal.style.display = "none";

    document.body.style.overflow = "";
}

function section6ValidateBeforeNext() {
    const mode = getSubmissionMode();
    const pairs = section6GetFieldPairs();

    pairs.forEach(pair => {
        const el = document.getElementById(pair.newId);
        if (el) section6ClearFieldError(el);
    });

    if (!section6HasAtLeastOneChange()) {
        const msg = document.getElementById("new_change_invalid");

        if (msg) {
            msg.innerHTML = "Please fill at least one of the changes you want to make.";
            msg.style.color = "red";
        }

        pairs.forEach(pair => {
            const el = document.getElementById(pair.newId);
            if (el) {
                el.style.border = "2px solid #d00000";
                el.style.backgroundColor = "#fff1f1";
            }
        });

        showLocusStandModal(mode, []);

        return false;
    }

    const duplicates = section6ValidateSameValues();

    if (duplicates.length > 0) {
        showLocusStandModal(mode, duplicates);

        const first = document.getElementById(duplicates[0].newId);
        if (first) {
            setTimeout(() => first.focus(), 250);
        }

        return false;
    }

    const msg = document.getElementById("new_change_invalid");

    if (msg) {
        msg.innerHTML = "";
        msg.style.color = "";
    }

    return true;
}

function initialiseSection6MvdValidation() {
    ensureLocusStandModalExists();
    section6FormatOriginalMarketValue();

    const newMarketValue = document.getElementById("NewMarketValue");

    if (newMarketValue) {
        newMarketValue.addEventListener("input", function () {
            section6FormatRandInput(this);
            section6ValidateSameValues();
        });

        newMarketValue.addEventListener("blur", function () {
            section6FormatRandInput(this);
            section6ValidateSameValues();
        });
    }

    section6GetFieldPairs().forEach(pair => {
        const el = document.getElementById(pair.newId);

        if (!el || pair.newId === "NewMarketValue") return;

        const eventName = el.tagName === "SELECT" ? "change" : "input";

        el.addEventListener(eventName, function () {
            section6ValidateSameValues();
        });

        el.addEventListener("blur", function () {
            section6ValidateSameValues();
        });
    });
}
const style = document.createElement('style');
style.textContent = `
    body.loading { overflow: hidden; }
    #preloader.fade-out { opacity: 0; transition: opacity 0.6s ease-out; }
    .btn:active { transform: scale(0.95); }
    input:focus, select:focus, textarea:focus { box-shadow: 0 0 0 3px rgba(0, 101, 112, 0.1); }

    .locus-modal-backdrop {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, .65);
        z-index: 25000;
        display: none;
        align-items: center;
        justify-content: center;
        padding: 20px;
    }

    .locus-modal {
        width: min(620px, 94vw);
        background: #ffffff;
        border-radius: 16px;
        overflow: hidden;
        box-shadow: 0 25px 80px rgba(0, 0, 0, .35);
        border: 2px solid #e6b000;
        font-family: 'Poppins', Arial, sans-serif;
    }

    .locus-modal-header {
        display: flex;
        align-items: center;
        gap: 14px;
        padding: 20px 24px;
        background: linear-gradient(135deg, #1a2e35, #006572);
        color: #ffffff;
        border-bottom: 4px solid #e6b000;
    }

    .locus-modal-icon {
        width: 46px;
        height: 46px;
        border-radius: 12px;
        background: rgba(230, 176, 0, .18);
        border: 1.5px solid rgba(230, 176, 0, .6);
        color: #e6b000;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 20px;
    }

    .locus-modal-header h3 {
        margin: 0;
        font-size: 20px;
        font-weight: 800;
    }

    .locus-modal-header p {
        margin: 3px 0 0;
        color: rgba(255, 255, 255, .7);
        font-size: 13px;
    }

    .locus-modal-body {
        padding: 24px;
        color: #333;
        font-size: 14px;
    }

    .locus-duplicate-list {
        margin-top: 14px;
        padding: 12px 14px;
        background: #fff5f5;
        border: 1px solid #f5b5b5;
        border-radius: 10px;
        color: #9f1d1d;
        font-weight: 600;
        display: none;
    }

    .locus-modal-footer {
        padding: 16px 24px 22px;
        text-align: right;
    }

    .locus-modal-btn {
        border: none;
        background: #006572;
        color: #ffffff;
        font-weight: 800;
        padding: 11px 18px;
        border-radius: 9px;
        cursor: pointer;
    }

    .locus-modal-btn:hover {
        background: #004f59;
    }
`;

document.head.appendChild(style);
function normaliseMoney(value) {
    if (!value) return "";
    const text = value.toString()
        .replace(/[Rr]/g, "")
        .replace(/[\s,\u00a0\u202f]/g, "");
    const match = text.match(/^\d+(?:\.\d+)?/);
    if (!match) return "";
    const amount = Number(match[0]);
    return Number.isFinite(amount) && amount >= 0 ? String(Math.round(amount)) : "";
}

function formatRand(value) {
    const raw = normaliseMoney(value);

    if (!raw) return "";

    return "R " + Number(raw).toLocaleString("en-ZA", {
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).replace(/[,\u00a0\u202f]/g, " ");
}

document.addEventListener("DOMContentLoaded", function () {
    const newMv = document.getElementById("NewMarketValue");
    const newMvRaw = document.getElementById("NewMarketValueRaw");

    if (newMv && newMvRaw) {
        newMv.addEventListener("input", function () {
            const raw = normaliseMoney(newMv.value);
            newMvRaw.value = raw;
            newMv.value = raw ? formatRand(raw) : "";
        });

        const form = document.getElementById("myForm");

        if (form) {
            form.addEventListener("submit", function () {
                newMvRaw.value = normaliseMoney(newMv.value);
            });
        }
    }
});



let isResettingCategoryDropdown = false;

function resetCategoryDropdown(selectElement) {
    if (!selectElement) return;

    isResettingCategoryDropdown = true;

    selectElement.value = "";

    setTimeout(function () {
        isResettingCategoryDropdown = false;
        selectElement.focus();
    }, 100);
}

function validateCategoryChange(selectId, oldCategoryId) {
    const ddl = document.getElementById(selectId);
    const oldInput = document.getElementById(oldCategoryId);

    if (!ddl || !oldInput) return;

    ddl.addEventListener("change", function () {
        if (isResettingCategoryDropdown) {
            return;
        }

        const selectedCategory = normaliseCategory(ddl.value);
        const oldCategory = normaliseCategory(oldInput.value);

        if (!selectedCategory || !oldCategory) return;

        if (selectedCategory === oldCategory) {
            showCategorySameModal(ddl);
        }
    });
}

function validateCategoryChange(selectId, oldCategoryId) {
    const ddl = document.getElementById(selectId);
    const oldInput = document.getElementById(oldCategoryId);

    if (!ddl || !oldInput) return;

    ddl.addEventListener("change", function () {
        const selectedCategory = normaliseCategory(ddl.value);
        const oldCategory = normaliseCategory(oldInput.value);

        if (!selectedCategory || !oldCategory) return;

        if (selectedCategory === oldCategory) {
            alert("You cannot select the same category that is already on the current valuation roll. Please select a different category.");

            resetCategoryDropdown(ddl);

            setTimeout(function () {
                ddl.focus();
            }, 100);
        }
    });
}

document.addEventListener("DOMContentLoaded", function () {
    // Single-purpose category dropdown
    validateCategoryChange("NewCategory", "OldCategoryValue");

    // Multipurpose category dropdowns
    validateCategoryChange("NewCategory1", "OldCategoryValue1");
    validateCategoryChange("NewCategory2", "OldCategoryValue2");
    validateCategoryChange("NewCategory3", "OldCategoryValue3");
});

function syncMoneyFieldsBeforeSubmit() {
    const newMv = document.getElementById("NewMarketValue");
    const newMvRaw = document.getElementById("NewMarketValueRaw");

    if (newMv && newMvRaw) {
        newMvRaw.value = normaliseMoney(newMv.value);
    }

    const multiMoneyPairs = [
        ["NewMarketValue1", "NewMarketValueRaw1"],
        ["NewMarketValue2", "NewMarketValueRaw2"],
        ["NewMarketValue3", "NewMarketValueRaw3"]
    ];

    multiMoneyPairs.forEach(pair => {
        const visible = document.getElementById(pair[0]);
        const raw = document.getElementById(pair[1]);

        if (visible && raw) {
            raw.value = normaliseMoney(visible.value);
        }
    });
}
function showCategorySameModal(dropdown) {
    section6SetFieldError(
        dropdown,
        "You cannot select the same category that is already on the current valuation roll. Please select a different category."
    );

    showLocusStandModal(getSubmissionMode(), [
        {
            label: "Category"
        }
    ]);

    resetCategoryDropdown(dropdown);
}


// ═══════════════════════════════════════════════════════════════════
// GENESIS FRIENDLY VALIDATION + CONSISTENT RAND DISPLAY
// Shared behaviour for Objection, Multi, Query and Query Multi forms.
// Existing IDs, classes and navigation handlers remain unchanged.
// ═══════════════════════════════════════════════════════════════════
(function () {
    "use strict";

    if (window.__genesisFriendlyValidationInitialised) return;
    window.__genesisFriendlyValidationInitialised = true;

    var form = document.getElementById("myForm");
    if (!form) return;

    var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
    var telephonePattern = /^0\d{9}$/;
    var moneyIds = [
        "Market_Value", "Market_Value1", "Market_Value2", "Market_Value3",
        "market_Value",
        "NewMarketValue", "NewMarketValue1", "NewMarketValue2", "NewMarketValue3"
    ];

    function element(id) {
        return document.getElementById(id);
    }

    function cleanMoney(value) {
        var text = String(value == null ? "" : value)
            .replace(/[Rr]/g, "")
            .replace(/[\s,\u00a0\u202f]/g, "")
            .trim();

        if (!text) return "";

        var match = text.match(/^-?\d+(?:\.\d+)?/);
        if (!match) return "";

        var amount = Number(match[0]);
        if (!Number.isFinite(amount) || amount < 0) return "";

        return String(Math.round(amount));
    }

    function formatRand(value) {
        var raw = cleanMoney(value);
        if (!raw) return "";

        return "R " + Number(raw)
            .toLocaleString("en-ZA", {
                minimumFractionDigits: 0,
                maximumFractionDigits: 0
            })
            .replace(/[,\u00a0\u202f]/g, " ");
    }

    function syncRawMoney(visible) {
        if (!visible || !visible.id) return;

        var suffix = visible.id.replace("NewMarketValue", "");
        var raw = element("NewMarketValueRaw" + suffix);

        if (raw) raw.value = cleanMoney(visible.value);
    }

    function formatMoneyElement(target) {
        if (!target) return;

        var currentValue =
            target.matches && target.matches("input, textarea")
                ? target.value
                : target.textContent;

        var formatted = formatRand(currentValue);
        if (!formatted) return;

        if (target.matches && target.matches("input, textarea")) {
            if ((target.type || "").toLowerCase() !== "number") {
                target.value = formatted;
            }
        } else {
            target.textContent = formatted;
        }

        syncRawMoney(target);
    }

    function initialiseMoneyFormatting() {
        var seen = new Set();

        moneyIds.forEach(function (id) {
            var target = element(id);
            if (target) seen.add(target);
        });

        form.querySelectorAll(
            "input[data-type='currency'], [data-city-market-value], .city-market-value"
        ).forEach(function (target) {
            seen.add(target);
        });

        seen.forEach(function (target) {
            formatMoneyElement(target);

            if (target.matches && target.matches("input:not([readonly]):not([disabled])")) {
                target.addEventListener("input", function () {
                    formatMoneyElement(target);
                });

                target.addEventListener("blur", function () {
                    formatMoneyElement(target);
                });
            }
        });
    }

    function errorId(input) {
        return input.id + "_friendly_error";
    }

    function clearFieldError(input) {
        if (!input) return;

        input.removeAttribute("aria-invalid");
        input.style.borderColor = "";
        input.style.backgroundColor = "";

        var error = element(errorId(input));
        if (error) error.remove();
    }

    function setFieldError(input, message) {
        if (!input) return;

        input.setAttribute("aria-invalid", "true");
        input.style.borderColor = "#dc2626";
        input.style.backgroundColor = "#fff7f7";

        var id = errorId(input);
        var error = element(id);

        if (!error) {
            error = document.createElement("span");
            error.id = id;
            error.setAttribute("role", "alert");
            error.style.display = "block";
            error.style.marginTop = "5px";
            error.style.color = "#b91c1c";
            error.style.fontSize = "12px";
            error.style.fontWeight = "600";
            input.insertAdjacentElement("afterend", error);
        }

        error.textContent = message;
        input.setAttribute("aria-describedby", id);
    }

    function getValidationSummary() {
        var summary = element("genesisFriendlyValidationSummary");

        if (!summary) {
            summary = document.createElement("div");
            summary.id = "genesisFriendlyValidationSummary";
            summary.setAttribute("role", "alert");
            summary.setAttribute("aria-live", "assertive");
            summary.style.display = "none";
            summary.style.margin = "15px auto";
            summary.style.maxWidth = "1500px";
            summary.style.padding = "14px 18px";
            summary.style.border = "1px solid #dc2626";
            summary.style.borderLeft = "5px solid #dc2626";
            summary.style.borderRadius = "8px";
            summary.style.background = "#fff7f7";
            summary.style.color = "#991b1b";

            var modelSummary = form.querySelector("[asp-validation-summary], .validation-summary-errors");
            if (modelSummary && modelSummary.parentNode) {
                modelSummary.insertAdjacentElement("afterend", summary);
            } else {
                form.insertAdjacentElement("afterbegin", summary);
            }
        }

        return summary;
    }

    function showValidationSummary(errors) {
        var summary = getValidationSummary();

        if (!errors.length) {
            summary.style.display = "none";
            summary.innerHTML = "";
            return;
        }

        var uniqueMessages = [];
        errors.forEach(function (error) {
            if (!uniqueMessages.includes(error.message)) {
                uniqueMessages.push(error.message);
            }
        });

        summary.innerHTML =
            "<strong>Please correct the following before continuing:</strong><ul style='margin:8px 0 0 20px;'>" +
            uniqueMessages.map(function (message) {
                return "<li>" + message.replace(/</g, "&lt;").replace(/>/g, "&gt;") + "</li>";
            }).join("") +
            "</ul>";
        summary.style.display = "block";
    }

    function addError(errors, input, message) {
        if (!input) return;
        setFieldError(input, message);
        errors.push({ input: input, message: message });
    }

    function validateRequiredName(id, label, errors) {
        var input = element(id);
        if (!input || input.disabled) return;

        clearFieldError(input);

        if (!input.value.trim()) {
            addError(errors, input, label + " is required.");
        }
    }

    function validateContactGroup(prefix, label, errors) {
        var email = element(prefix + "_cd_5");
        var phones = [
            element(prefix + "_cd_1"),
            element(prefix + "_cd_2"),
            element(prefix + "_cd_3")
        ].filter(function (input) {
            return input && !input.disabled;
        });

        if ((!email || email.disabled) && !phones.length) return;

        if (email && !email.disabled) {
            clearFieldError(email);

            if (!email.value.trim()) {
                addError(errors, email, label + " email address is required.");
            } else if (!emailPattern.test(email.value.trim())) {
                addError(errors, email, "Enter a valid " + label.toLowerCase() + " email address.");
            }
        }

        phones.forEach(clearFieldError);

        var enteredPhones = phones.filter(function (input) {
            return input.value.trim() !== "";
        });

        if (!enteredPhones.length && phones.length) {
            addError(
                errors,
                phones[0],
                "Enter at least one " + label.toLowerCase() +
                " telephone number: Home, Work or Cell."
            );
        }

        enteredPhones.forEach(function (input) {
            var digits = input.value.replace(/\D/g, "");

            if (!telephonePattern.test(digits)) {
                addError(
                    errors,
                    input,
                    "Telephone numbers must contain 10 digits and start with 0."
                );
            }
        });

        var groupMessage = element(prefix + "_cd_invalid");
        if (groupMessage) {
            groupMessage.textContent = enteredPhones.length
                ? ""
                : "Enter at least one telephone number: Home, Work or Cell.";
            groupMessage.style.color = enteredPhones.length ? "" : "#b91c1c";
        }
    }

    function objectorType() {
        var hidden = element("Objector_Type");
        return (
            (hidden && hidden.value) ||
            sessionStorage.getItem("objector_choice") ||
            "Owner"
        ).trim();
    }

    function validateSectionOne() {
        var errors = [];
        var type = objectorType();

        if (type === "Third_Party") {
            validateRequiredName("objector_name", "Third-party name", errors);
            validateContactGroup("obj", "Third-party", errors);
        } else if (type === "Representative") {
            validateRequiredName("o_name", "Registered owner name", errors);
            validateRequiredName("rep_name", "Representative name", errors);
            validateContactGroup("o", "Owner", errors);
            validateContactGroup("rep", "Representative", errors);
        } else {
            validateRequiredName("o_name", "Registered owner name", errors);
            validateContactGroup("o", "Owner", errors);
        }

        showValidationSummary(errors);
        return errors;
    }

    function validateDeclaration() {
        var errors = [];
        var name = element("sign_obj");
        var signature = element("SignatureDataUrl") || element("signatureData");
        var canvas = element("signature") || element("sigCanvas");

        if (name) {
            clearFieldError(name);
            if (!name.value.trim()) {
                addError(errors, name, "Enter the full name of the person signing the form.");
            }
        }

        if (signature && !signature.value.trim()) {
            if (canvas) {
                canvas.setAttribute("aria-invalid", "true");
                canvas.style.borderColor = "#dc2626";
            }

            errors.push({
                input: canvas || signature,
                message: "Draw your signature before submitting."
            });
        }

        return errors;
    }

    function focusFirstError(errors) {
        if (!errors.length) return;

        var target = errors[0].input;
        if (!target) return;

        if (typeof target.scrollIntoView === "function") {
            target.scrollIntoView({ behavior: "smooth", block: "center" });
        }

        setTimeout(function () {
            if (typeof target.focus === "function") target.focus();
        }, 350);
    }

    function stopAction(event, errors) {
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        showValidationSummary(errors);
        focusFirstError(errors);
    }

    document.addEventListener("click", function (event) {
        var sectionOneNext = event.target.closest(".btn_n1");
        if (sectionOneNext) {
            var sectionErrors = validateSectionOne();
            if (sectionErrors.length) stopAction(event, sectionErrors);
            return;
        }

        var submitButton = event.target.closest("#submitForm");
        if (!submitButton) return;

        var errors = validateSectionOne().concat(validateDeclaration());
        if (errors.length) stopAction(event, errors);
    }, true);

    form.addEventListener("submit", function (event) {
        var errors = validateSectionOne().concat(validateDeclaration());

        if (errors.length) {
            event.preventDefault();
            event.stopImmediatePropagation();
            showValidationSummary(errors);
            focusFirstError(errors);
            return false;
        }

        moneyIds.forEach(function (id) {
            var target = element(id);
            if (target) syncRawMoney(target);
        });
    }, true);

    form.addEventListener("input", function (event) {
        var target = event.target;
        if (target && target.id) clearFieldError(target);
    });

    initialiseMoneyFormatting();

    window.GenesisFriendlyValidation = {
        validateSectionOne: validateSectionOne,
        validateDeclaration: validateDeclaration,
        cleanMoney: cleanMoney,
        formatRand: formatRand,
        initialiseMoneyFormatting: initialiseMoneyFormatting
    };
})();
