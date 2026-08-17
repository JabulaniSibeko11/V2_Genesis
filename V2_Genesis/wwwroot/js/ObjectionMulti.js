var property_key = sessionStorage.getItem('property_choice'); 
var objector_key = sessionStorage.getItem('objector_choice');
var AppealStatus = sessionStorage.getItem('AppealStatus');
var cd_o = 'false';
var cd_obj = 'false';
var cd_rep = 'false';
var NewChange = 'false';
var fo_o = 0;


var loader = document.getElementById("preloader");
window.addEventListener("load", function () {
    loader.style.display = "none";

});

//var userEmailElement = document.getElementById('userEmail');

//if (userEmailElement) {
//    var userEmail = userEmailElement.value;

//    var regex = /^(val\.admin(1[0-9]?|[1-9])@joburg\.org\.za)$/i;

//    if (regex.test(userEmail) || userEmail === 'AdministrationEnquiries@Joburg.org.za') {
//        document.getElementById('capturer').style.display = 'block';
//        document.getElementById('sapNo').setAttribute('required', 'required');
//    } else {
//        document.getElementById('capturer').style.display = 'none';
//        document.getElementById('sapNo').removeAttribute('required');
//    }
//} else {
//    console.warn("Element with ID 'userEmail' not found in the document.");
//}
var isAdminFlag = document.getElementById('isAdminFlag')?.value === 'true';

// keep the regex + userEmail for the showInput() submit validation
var userEmailElement = document.getElementById('userEmail');
var userEmail = userEmailElement ? userEmailElement.value : '';
var regex = /^(val\.admin(1[0-9]?|[1-9])@joburg\.org\.za)$/i;

document.getElementById('Objector_Type').value = sessionStorage.getItem('objector_choice');

document.getElementById('Property_Type').value = sessionStorage.getItem('property_choice');

function pos_yes() {
    var a; var b; var c; var d; var e;

    a = document.getElementById("o_st_1").value;
    document.getElementById("o_p_1").value = a;

    b = document.getElementById("o_st_2").value;
    document.getElementById("o_p_2").value = b;

    c = document.getElementById("o_st_3").value;
    document.getElementById("o_p_3").value = c;

    d = document.getElementById("o_st_4").value;
    document.getElementById("o_p_4").value = d;

    e = document.getElementById("o_st_5").value;
    document.getElementById("o_p_5").value = e;

}
function pos_no() {
    document.getElementById("o_p_1").value = "";

    document.getElementById("o_p_2").value = "";

    document.getElementById("o_p_3").value = "";

    document.getElementById("o_p_4").value = "";

    document.getElementById("o_p_5").value = "";
}
if (document.getElementById("AppealStat").value !== null) {
    document.getElementById("AppealStat").value = sessionStorage.getItem('AppealStatus');
}
document.getElementById("o_pass").disabled = true;

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
var mark = document.getElementById('extent').value;
//var valu = 'R '+'R '+new Intl.NumberFormat().format(mark);
var valu = mark.toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
document.getElementById('extent').value = valu;

var date = new Date();
var currentDate = date.toISOString().slice(0, 16);

document.getElementById('signDate').value = currentDate;

var ext, ext2, fsize, fi;
const input = document.querySelector('#files');

// Listen for files selection
input.addEventListener('change', (e) => {
    // Retrieve all files
    const files = input.files;

    // Check files count
    if (files.length > 10) {
        input.value = "";
        alert(`Only 10 files are allowed to upload.`);
        return;
    }

    for (let i = 0; i < files.length; i++) {
        ext = input.files.item(i).name;
        ext2 = ext.split(".").pop();
        ext2.toLowerCase();
        console.log(ext2);

        switch (ext2) {
            case 'pdf':
            case 'jpeg':
            case 'jpg':
            case 'png':
            case 'heif':
                fsize = input.files.item(i).size;
                fi = Math.round((fsize / 1024));
                // The size of the file.
                if (ext.length > 100) {
                    alert("File name too long.");
                    input.value = '';
                    break;
                }
                if (fi >= 10240) {
                    alert("File too Big, please select a file less than 10,2mb");
                    input.value = '';
                }
                break;
            default:
                alert('File type Not allowed. PDF, JPEG, JPG, PNG,HEIF only.');
                input.value = '';
                break;
        }
    }
});

function onlyNumberKey(evt) {

    // Only ASCII character in that range allowed
    var ASCIICode = (evt.which) ? evt.which : evt.keyCode
    if (ASCIICode > 31 && (ASCIICode < 48 || ASCIICode > 57))
        return false;
    return true;
}

function RSA() {
    if (sessionStorage.getItem('objector_choice') == "Owner") {
        $("#o_pass").hide();
        $("#o_id").show();
    }
    if (objector_key == "Third_Party") {
        $(".Div1-1").hide();
        $(".Div1-3").hide();
    }
    if (objector_key == "Representative") {
        $(".Div1-2").hide();
        document.getElementById("owner_head").innerHTML = "1.1 OWNER DETAILS";

    }
}
function foreigner() {
    if (sessionStorage.getItem('objector_choice') == "Owner") {
        $("#o_id").hide();
        $("#o_pass").show();
    }
    if (objector_key == "Third_Party") {

    }
    if (objector_key == "Representative") {


    }
}

var cat = "";
var market_Value = "";
var extent = "";
var desc = "";
var owner = "";
var objId = "";
var pin

let isSubmittingMultiForm = false;

function showInput() {
    if (isSubmittingMultiForm) {
        return false;
    }

    // Validate Section 6 before final submit
    if (typeof section6ValidateBeforeNext === "function") {
        if (!section6ValidateBeforeNext()) {
            hideSubmitLoader();
            return false;
        }
    }

    // Store summary values
    const propertyDescEl = document.getElementById("Property_Desc");
    const marketValueEl =
        document.getElementById("Market_Value") ||
        document.getElementById("market_Value");

    const extentEl = document.getElementById("extent");
    const catEl = document.getElementById("cat");
    const ownerEl = document.getElementById("owner");

    desc = propertyDescEl ? propertyDescEl.value : "";
    market_Value = marketValueEl ? marketValueEl.value : "";
    extent = extentEl ? extentEl.value : "";
    cat = catEl ? catEl.value : "";
    owner = ownerEl ? ownerEl.value : "";

    sessionStorage.setItem("desc", desc);
    sessionStorage.setItem("market_Value", market_Value);
    sessionStorage.setItem("extent", extent);
    sessionStorage.setItem("cat", cat);
    sessionStorage.setItem("owner", owner);

    // Signature name required
    const signObj = document.getElementById("sign_obj");

    if (!signObj || signObj.value.trim() === "") {
        alert("Please enter the name of the person signing the form.");
        hideSubmitLoader();
        return false;
    }

    // Signature pad accepted required
    const signatureData = document.getElementById("SignatureDataUrl");

    if (!signatureData || signatureData.value.trim() === "") {
        alert("Please accept your signature before submitting.");
        hideSubmitLoader();
        return false;
    }

    // Admin SAP validation
    const sapNo = document.getElementById("sapNo");

    if (isAdminFlag === true && (!sapNo || sapNo.value.trim() === "")) {
        alert("Admin SAP number is required before submitting.");
        hideSubmitLoader();
        return false;
    }

    // Make sure formatted Rand values post as plain numbers
    syncMultiMoneyFieldsBeforeSubmit();

    const form = document.getElementById("myForm");

    if (!form) {
        alert("Form was not found. Please refresh and try again.");
        hideSubmitLoader();
        return false;
    }

    isSubmittingMultiForm = true;

    showSubmitLoader();

    const submitButton = document.getElementById("submitForm");

    if (submitButton) {
        submitButton.disabled = true;
        submitButton.innerHTML = "Please wait...";
    }

    setTimeout(function () {
        form.submit();
    }, 300);

    return false;
}
function showSubmitLoader() {
    const overlay = document.getElementById("objLoaderOverlay");

    if (overlay) {
        overlay.style.display = "flex";
        overlay.classList.add("show");
    }

    document.body.classList.add("loading");
}

function hideSubmitLoader() {
    const overlay = document.getElementById("objLoaderOverlay");

    if (overlay) {
        overlay.style.display = "none";
        overlay.classList.remove("show");
    }

    document.body.classList.remove("loading");

    const submitButton = document.getElementById("submitForm");

    if (submitButton) {
        submitButton.disabled = false;
        submitButton.innerHTML = "Submit";
    }

    isSubmittingMultiForm = false;
}

String.prototype.reverse = function () {
    return this.split("").reverse().join("");
}

function reformatText(input) {
    var x = input.value;
    x = x.replace(/,/g, ""); // Strip out all commas
    x = x.reverse();
    x = x.replace(/.../g, function (e) {
        return e + ",";
    }); // Insert new commas
    x = x.reverse();
    x = x.toString().replace(/^,/g, ""); // Remove leading comma
    //y = x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",")
    input.value = x;	//Re-format as the user type
    document.getElementById('Obj_Compensation_Amount').value = parseFloat(x.replace(/,/g, '')); //Removing commas to be able to save data to the database

}


$(document).ready(function () {
    //Res Full Title
    $("#res_kitchins").hide();
    $("#res_lounges").hide();
    $("#res_dining_room").hide();
    $("#res_laundry").hide();
    $("#res_study").hide();
    $("#res_playroom").hide();
    $("#res_television").hide();
    $("#res_separate_toilets").hide();
    $("#res_lounge_dining_room").hide();

    //Res Sectional Title
    $("#res_st_kitchins").hide();
    $("#res_st_lounges").hide();
    $("#res_st_dining_room").hide();
    $("#res_st_laundry").hide();
    $("#res_st_study").hide();
    $("#res_st_playroom").hide();
    $("#res_st_television").hide();
    $("#res_st_separate_toilets").hide();
    $("#res_st_lounge_dining_room").hide();

    //Agric
    $("#agric_kitchins").hide();
    $("#agric_lounges").hide();
    $("#agric_dining_room").hide();
    $("#agric_laundry").hide();
    $("#agric_study").hide();
    $("#agric_playroom").hide();
    $("#agric_television").hide();
    $("#agric_separate_toilets").hide();
    $("#agric_lounge_dining_room").hide();

});

//****************Res Full Title***********//

//Function to hide and show "Res Number of Kitchens" options
function show_res_kitchins() {
    $("#res_kitchins").show();

    document.getElementById("kitchen_one").innerHTML = 1;
    document.getElementById("kitchen_two").innerHTML = 2;
    document.getElementById("kitchen_three").innerHTML = 3;
    document.getElementById("kitchen_four").innerHTML = 4;
    document.getElementById("kitchen_five").innerHTML = 5;

}

function hide_res_kitchins() {
    $("#res_kitchins").hide();

    document.getElementById("kitchen_one").innerHTML = 0;
    document.getElementById("kitchen_two").innerHTML = 0;
    document.getElementById("kitchen_three").innerHTML = 0;
    document.getElementById("kitchen_four").innerHTML = 0;
    document.getElementById("kitchen_five").innerHTML = 0;
}

//Function to hide and show "Res Number of lounge" options
function show_res_lounge() {
    $("#res_lounges").show();

    document.getElementById("lounge_one").innerHTML = 1;
    document.getElementById("lounge_two").innerHTML = 2;
    document.getElementById("lounge_three").innerHTML = 3;
    document.getElementById("lounge_four").innerHTML = 4;
    document.getElementById("lounge_five").innerHTML = 5;

}

function hide_res_lounge() {
    $("#res_lounges").hide();

    document.getElementById("lounge_one").innerHTML = 0;
    document.getElementById("lounge_two").innerHTML = 0;
    document.getElementById("lounge_three").innerHTML = 0;
    document.getElementById("lounge_four").innerHTML = 0;
    document.getElementById("lounge_five").innerHTML = 0;
}

// //Function to hide and show "Res Number of Dining Rooms" options
function show_res_dining_room() {
    $("#res_dining_room").show();

    document.getElementById("dining_room_one").innerHTML = 1;
    document.getElementById("dining_room_two").innerHTML = 2;
    document.getElementById("dining_room_three").innerHTML = 3;
    document.getElementById("dining_room_four").innerHTML = 4;
    document.getElementById("dining_room_five").innerHTML = 5;

}

function hide_res_dining_room() {
    $("#res_dining_room").hide();

    document.getElementById("dining_room_one").innerHTML = 0;
    document.getElementById("dining_room_two").innerHTML = 0;
    document.getElementById("dining_room_three").innerHTML = 0;
    document.getElementById("dining_room_four").innerHTML = 0;
    document.getElementById("dining_room_five").innerHTML = 0;
}

function show_res_laundry() {
    $("#res_laundry").show();

    document.getElementById("laundry_one").innerHTML = 1;
    document.getElementById("laundry_two").innerHTML = 2;
    document.getElementById("laundry_three").innerHTML = 3;
    document.getElementById("laundry_four").innerHTML = 4;
    document.getElementById("laundry_five").innerHTML = 5;
}

function hide_res_laundry() {
    $("#res_laundry").hide();

    document.getElementById("laundry_one").innerHTML = 0;
    document.getElementById("laundry_two").innerHTML = 0;
    document.getElementById("laundry_three").innerHTML = 0;
    document.getElementById("laundry_four").innerHTML = 0;
    document.getElementById("laundry_five").innerHTML = 0;
}

//Function to hide and show "Res Number of study rooms" options
function show_res_study() {
    $("#res_study").show();

    document.getElementById("study_one").innerHTML = 1;
    document.getElementById("study_two").innerHTML = 2;
    document.getElementById("study_three").innerHTML = 3;
    document.getElementById("study_four").innerHTML = 4;
    document.getElementById("study_five").innerHTML = 5;

}

function hide_res_study() {
    $("#res_study").hide();

    document.getElementById("study_one").innerHTML = 0;
    document.getElementById("study_two").innerHTML = 0;
    document.getElementById("study_three").innerHTML = 0;
    document.getElementById("study_four").innerHTML = 0;
    document.getElementById("study_five").innerHTML = 0;
}

//Function to hide and show "Res Number of playroom" options
function show_res_playroom() {
    $("#res_playroom").show();

    document.getElementById("playroom_one").innerHTML = 1;
    document.getElementById("playroom_two").innerHTML = 2;
    document.getElementById("playroom_three").innerHTML = 3;
    document.getElementById("playroom_four").innerHTML = 4;
    document.getElementById("playroom_five").innerHTML = 5;

}

function hide_res_playroom() {
    $("#res_playroom").hide();

    document.getElementById("playroom_one").innerHTML = 0;
    document.getElementById("playroom_two").innerHTML = 0;
    document.getElementById("playroom_three").innerHTML = 0;
    document.getElementById("playroom_four").innerHTML = 0;
    document.getElementById("playroom_five").innerHTML = 0;
}

//Function to hide and show "Res Number of television" options
function show_res_television() {
    $("#res_television").show();

    document.getElementById("television_one").innerHTML = 1;
    document.getElementById("television_two").innerHTML = 2;
    document.getElementById("television_three").innerHTML = 3;
    document.getElementById("television_four").innerHTML = 4;
    document.getElementById("television_five").innerHTML = 5;

}

function hide_res_television() {
    $("#res_television").hide();

}

//Function to hide and show "Res Number of separate toilets" options
function show_res_separate_toilets() {
    $("#res_separate_toilets").show();

    document.getElementById("separate_toilets_one").innerHTML = 1;
    document.getElementById("separate_toilets_two").innerHTML = 2;
    document.getElementById("separate_toilets_three").innerHTML = 3;
    document.getElementById("separate_toilets_four").innerHTML = 4;
    document.getElementById("separate_toilets_five").innerHTML = 5;

}

function hide_res_separate_toilets() {
    $("#res_separate_toilets").hide();

}

//Function to hide and show "Res Number of lounge with dining room" options
function show_res_lounge_dining_room() {
    $("#res_lounge_dining_room").show();

    document.getElementById("lounge_dining_room_one").innerHTML = 1;
    document.getElementById("lounge_dining_room_two").innerHTML = 2;
    document.getElementById("lounge_dining_room_three").innerHTML = 3;
    document.getElementById("lounge_dining_room_four").innerHTML = 4;
    document.getElementById("lounge_dining_room_five").innerHTML = 5;

}

function hide_res_lounge_dining_room() {
    $("#res_lounge_dining_room").hide();

}

//******************Res Sectional Title *********************** */

function show_res_st_kitchins() {
    $("#res_st_kitchins").show();

    document.getElementById("kitchen_st_one").innerHTML = 1;
    document.getElementById("kitchen_st_two").innerHTML = 2;
    document.getElementById("kitchen_st_three").innerHTML = 3;
    document.getElementById("kitchen_st_four").innerHTML = 4;
    document.getElementById("kitchen_st_five").innerHTML = 5;

}

function hide_res_st_kitchins() {
    $("#res_st_kitchins").hide();

}

//Function to hide and show "Res Number of lounge" options
function show_res_st_lounge() {
    $("#res_st_lounges").show();

    document.getElementById("lounge_st_one").innerHTML = 1;
    document.getElementById("lounge_st_two").innerHTML = 2;
    document.getElementById("lounge_st_three").innerHTML = 3;
    document.getElementById("lounge_st_four").innerHTML = 4;
    document.getElementById("lounge_st_five").innerHTML = 5;

}

function hide_res_st_lounge() {
    $("#res_st_lounges").hide();

}

// //Function to hide and show "Res Number of Dining Rooms" options
function show_res_st_dining_room() {
    $("#res_st_dining_room").show();

    document.getElementById("dining_room_st_one").innerHTML = 1;
    document.getElementById("dining_room_st_two").innerHTML = 2;
    document.getElementById("dining_room_st_three").innerHTML = 3;
    document.getElementById("dining_room_st_four").innerHTML = 4;
    document.getElementById("dining_room_st_five").innerHTML = 5;

}

function hide_res_st_dining_room() {
    $("#res_st_dining_room").hide();

}

function show_res_st_laundry() {
    $("#res_st_laundry").show();

    document.getElementById("laundry_st_one").innerHTML = 1;
    document.getElementById("laundry_st_two").innerHTML = 2;
    document.getElementById("laundry_st_three").innerHTML = 3;
    document.getElementById("laundry_st_four").innerHTML = 4;
    document.getElementById("laundry_st_five").innerHTML = 5;
}

function hide_res_st_laundry() {
    $("#res_st_laundry").hide();

}

//Function to hide and show "Res Number of study rooms" options
function show_res_st_study() {
    $("#res_st_study").show();

    document.getElementById("study_st_one").innerHTML = 1;
    document.getElementById("study_st_two").innerHTML = 2;
    document.getElementById("study_st_three").innerHTML = 3;
    document.getElementById("study_st_four").innerHTML = 4;
    document.getElementById("study_st_five").innerHTML = 5;

}

function hide_res_st_study() {
    $("#res_st_study").hide();

}

//Function to hide and show "Res Number of playroom" options
function show_res_st_playroom() {
    $("#res_st_playroom").show();

    document.getElementById("playroom_st_one").innerHTML = 1;
    document.getElementById("playroom_st_two").innerHTML = 2;
    document.getElementById("playroom_st_three").innerHTML = 3;
    document.getElementById("playroom_st_four").innerHTML = 4;
    document.getElementById("playroom_st_five").innerHTML = 5;

}

function hide_res_st_playroom() {
    $("#res_st_playroom").hide();

}

//Function to hide and show "Res Number of television" options
function show_res_st_television() {
    $("#res_st_television").show();

    document.getElementById("television_st_one").innerHTML = 1;
    document.getElementById("television_st_two").innerHTML = 2;
    document.getElementById("television_st_three").innerHTML = 3;
    document.getElementById("television_st_four").innerHTML = 4;
    document.getElementById("television_st_five").innerHTML = 5;

}

function hide_res_st_television() {
    $("#res_st_television").hide();

}

//Function to hide and show "Res Number of separate toilets" options
function show_res_st_separate_toilets() {
    $("#res_st_separate_toilets").show();

    document.getElementById("separate_toilets_st_one").innerHTML = 1;
    document.getElementById("separate_toilets_st_two").innerHTML = 2;
    document.getElementById("separate_toilets_st_three").innerHTML = 3;
    document.getElementById("separate_toilets_st_four").innerHTML = 4;
    document.getElementById("separate_toilets_st_five").innerHTML = 5;

}

function hide_res_st_separate_toilets() {
    $("#res_st_separate_toilets").hide();

}

//Function to hide and show "Res Number of lounge with dining room" options
function show_res_st_lounge_dining_room() {
    $("#res_st_lounge_dining_room").show();

    document.getElementById("lounge_dining_room_st_one").innerHTML = 1;
    document.getElementById("lounge_dining_room_st_two").innerHTML = 2;
    document.getElementById("lounge_dining_room_st_three").innerHTML = 3;
    document.getElementById("lounge_dining_room_st_four").innerHTML = 4;
    document.getElementById("lounge_dining_room_st_five").innerHTML = 5;

}

function hide_res_st_lounge_dining_room() {
    $("#res_st_lounge_dining_room").hide();

}

//****************Agric Fill Title***********//

//Function to hide and show "Agric Number of Kitchens" options
function show_agric_kitchins() {
    $("#agric_kitchins").show();

    document.getElementById("agric_kitchen_one").innerHTML = 1;
    document.getElementById("agric_kitchen_two").innerHTML = 2;
    document.getElementById("agric_kitchen_three").innerHTML = 3;
    document.getElementById("agric_kitchen_four").innerHTML = 4;
    document.getElementById("agric_kitchen_five").innerHTML = 5;

}

function hide_agric_kitchins() {
    $("#agric_kitchins").hide();


}

//Function to hide and show "Agric Number of lounge" options
function show_agric_lounge() {
    $("#agric_lounges").show();

    document.getElementById("agric_lounge_one").innerHTML = 1;
    document.getElementById("agric_lounge_two").innerHTML = 2;
    document.getElementById("agric_lounge_three").innerHTML = 3;
    document.getElementById("agric_lounge_four").innerHTML = 4;
    document.getElementById("agric_lounge_five").innerHTML = 5;

}

function hide_agric_lounge() {
    $("#agric_lounges").hide();

}

// //Function to hide and show "Agric Number of Dining Rooms" options
function show_agric_dining_room() {
    $("#agric_dining_room").show();

    document.getElementById("agric_dining_room_one").innerHTML = 1;
    document.getElementById("agric_dining_room_two").innerHTML = 2;
    document.getElementById("agric_dining_room_three").innerHTML = 3;
    document.getElementById("agric_dining_room_four").innerHTML = 4;
    document.getElementById("agric_dining_room_five").innerHTML = 5;

}

function hide_agric_dining_room() {
    $("#agric_dining_room").hide();

}

function show_agric_laundry() {
    $("#agric_laundry").show();

    document.getElementById("agric_laundry_one").innerHTML = 1;
    document.getElementById("agric_laundry_two").innerHTML = 2;
    document.getElementById("agric_laundry_three").innerHTML = 3;
    document.getElementById("agric_laundry_four").innerHTML = 4;
    document.getElementById("agric_laundry_five").innerHTML = 5;
}

function hide_agric_laundry() {
    $("#agric_laundry").hide();

}

//Function to hide and show "Agric Number of study rooms" options
function show_agric_study() {
    $("#agric_study").show();

    document.getElementById("agric_study_one").innerHTML = 1;
    document.getElementById("agric_study_two").innerHTML = 2;
    document.getElementById("agric_study_three").innerHTML = 3;
    document.getElementById("agric_study_four").innerHTML = 4;
    document.getElementById("agric_study_five").innerHTML = 5;

}

function hide_agric_study() {
    $("#agric_study").hide();

}

//Function to hide and show "Agric Number of playroom" options
function show_agric_playroom() {
    $("#agric_playroom").show();

    document.getElementById("agric_playroom_one").innerHTML = 1;
    document.getElementById("agric_playroom_two").innerHTML = 2;
    document.getElementById("agric_playroom_three").innerHTML = 3;
    document.getElementById("agric_playroom_four").innerHTML = 4;
    document.getElementById("agric_playroom_five").innerHTML = 5;

}

function hide_agric_playroom() {
    $("#agric_playroom").hide();

}

//Function to hide and show "Agric Number of television" options
function show_agric_television() {
    $("#agric_television").show();

    document.getElementById("agric_television_one").innerHTML = 1;
    document.getElementById("agric_television_two").innerHTML = 2;
    document.getElementById("agric_television_three").innerHTML = 3;
    document.getElementById("agric_television_four").innerHTML = 4;
    document.getElementById("agric_television_five").innerHTML = 5;

}

function hide_agric_television() {
    $("#agric_television").hide();

}

//Function to hide and show "Agric Number of separate toilets" options
function show_agric_separate_toilets() {
    $("#agric_separate_toilets").show();

    document.getElementById("agric_separate_toilets_one").innerHTML = 1;
    document.getElementById("agric_separate_toilets_two").innerHTML = 2;
    document.getElementById("agric_separate_toilets_three").innerHTML = 3;
    document.getElementById("agric_separate_toilets_four").innerHTML = 4;
    document.getElementById("agric_separate_toilets_five").innerHTML = 5;

}

function hide_agric_separate_toilets() {
    $("#agric_separate_toilets").hide();

}

//Function to hide and show "Agric Number of lounge with dining room" options
function show_agric_lounge_dining_room() {
    $("#agric_lounge_dining_room").show();

    document.getElementById("agric_lounge_dining_room_one").innerHTML = 1;
    document.getElementById("agric_lounge_dining_room_two").innerHTML = 2;
    document.getElementById("agric_lounge_dining_room_three").innerHTML = 3;
    document.getElementById("agric_lounge_dining_room_four").innerHTML = 4;
    document.getElementById("agric_lounge_dining_room_five").innerHTML = 5;

}

function hide_agric_lounge_dining_room() {
    $("#agric_lounge_dining_room").hide();

}
$("textarea").keydown(function (e) {
    if (e.keyCode == 13) {

        e.preventDefault();
    }
});



//const phoneInputField = document.querySelector("#o_cd_3");
//const phoneInput = window.intlTelInput(phoneInputField, {
//    utilsScript:
 //       "https://cdnjs.cloudflare.com/ajax/libs/intl-tel-input/17.0.8/js/utils.js",
//});

//$(document).ready(function () {
//    $(".o_cell_no").inputmask("(999)-999-9999");
    //$(".o_cell_no").inputmask("999,999,999,999");
//});




function load() {
   /* window.alert(property_key);*/
    //Residential focus to hide other sections
    if (property_key == "Multi") {
        $("#section1").show();
        $("#section2").show();
        $("#section3-agric").show();
        $("#section3-bus").show();
        $("#section4-bus").show();
        $("#section3-res").show();
        $("#section4-res").show();
        $(".div3_R").show();
        $(".div4_R").show();
        $(".div3_A").show();
        $(".div3_B").show();
        $(".div4_B").show();
        document.getElementById("form_head").innerHTML = "FORM D: Multiple Purpose (The use of a property for more than one purpose)";
         
    }

    //Owner
    if (objector_key == "Owner") {
        $(".Div1-2").hide();
        $(".Div1-3").hide();
        document.getElementById("o_name_l").innerHTML = 'REGISTERED OWNER OF PROPERTY';
    }
    if (objector_key == "Third_Party") {
        $(".Div1-1").hide();
        $(".Div1-3").hide();

    }
    if (objector_key == "Representative") {
        $(".Div1-2").hide();
        $("#owner_details").hide();
        document.getElementById("o_name_l").innerHTML = '<span style="color: red; ">*</span>REGISTERED OWNER OF PROPERTY (<span style="color: red;">required</span>)';
        document.getElementById("owner_head").innerHTML = "1.1 OWNER DETAILS";

    }
   
    if (objector_key == "Representative" && AppealStatus == "True") {
        $(".Div1-2").hide();
        $("#owner_details").hide();
        document.getElementById("o_name_l").innerHTML = '<span style="color: red; ">*</span>REGISTERED OWNER OF PROPERTY (<span style="color: red;">required</span>)';
        document.getElementById("owner_head").innerHTML = "1.1 OWNER DETAILS";

    }

}

// style=" margin-top: 12px; border-radius: 25px; border: 2px solid #73AD21; padding: 20px; width: 1050px;}"


//Function to hide and show "IS YOUR PROPERTY AFFECTED BY LAND CLAIM"" options
function hide_div_af() {
    $("#affected-land").hide();
}

function show_div_af() {
    $("#affected-land").show();
}

//Function to hide and show "DO YOU HAVE WATER RIGHT?" options
function hide_div_wr() {
    $("#water-right").hide();
}

function show_div_wr() {
    $("#water-right").show();
}
function LuhnAlgo() {
    var stat_Value;
    if (objector_key == 'Owner' &&
        document.getElementById("o_id").disabled == false) {
        var id = document.getElementById('o_id').value;
    }
    if (objector_key == 'Third_Party' &&
        document.getElementById("objector_id").disabled == false) {
        var id = document.getElementById('objector_id').value;
    }
    if (objector_key == 'Owner' &&
        document.getElementById("o_id").disabled == true) {
        var id = document.getElementById('o_pass').value;
    }
    if (objector_key == 'Third_Party' &&
        document.getElementById("objector_id").disabled == true) {
        var id = document.getElementById('objector_pass').value;
    }

    var id_stat = '';
    var arr = id.split(''); //we have converted the string into array
    var sum = 0;    // This variable will consists of sum after step 3
    var n = arr.length;
    for (var i = 0; i < n; i++) {
        arr[i] = parseInt(arr[i]);  // converting from character to int
    }
    for (var i = 1; i < n; i = i + 2) {   // execution of step 1
        var v = arr[n - 1 - i] * 2;
        if (v > 9) { arr[n - 1 - i] = v - 9; }
        else { arr[n - 1 - i] = v; }
    }
    for (var i = 0; i < n; i++) {    //calculating the step
        sum = sum + arr[i];
    }
    if (sum % 10 === 0 && id !== '' && id !== '0000000000000' && id.length == 13) {
        id_stat = ''

        if (objector_key == 'Owner') {
            document.getElementById("id_status").innerHTML = id_stat;
            stat_Value = document.getElementById("id_status").innerHTML;
        }
        if (objector_key == 'Third_Party') {
            document.getElementById("obj_id_status").innerHTML = id_stat;
            stat_Value = document.getElementById("obj_id_status").innerHTML;
        }
        return stat_Value;

    } else {

        id_stat = 'Invalid ID Number';

        if (objector_key == 'Owner' &&
            document.getElementById("o_id").disabled == false) {
            document.getElementById("id_status").innerHTML = id_stat;
            stat_Value = document.getElementById("id_status").innerHTML;
        }
        if (objector_key == 'Third_Party' &&
            document.getElementById("objector_id").disabled == false) {
            document.getElementById("obj_id_status").innerHTML = id_stat;
            stat_Value = document.getElementById("obj_id_status").innerHTML;
        }
        if (objector_key == 'Owner' &&
            document.getElementById("o_id").disabled == true) {
            id_stat = '';
            document.getElementById("id_status").innerHTML = id_stat;
            stat_Value = document.getElementById("id_status").innerHTML;
            return stat_Value;
        }
        if (objector_key == 'Third_Party' &&
            document.getElementById("objector_id").disabled == true) {
            id_stat = '';
            document.getElementById("obj_id_status").innerHTML = id_stat;
            stat_Value = document.getElementById("obj_id_status").innerHTML;
            return stat_Value;
        }
        //alert(stat_Value);
        return stat_Value;
    }
}
// function resetValue()
// {
//     var stat_Value = document.getElementById("id-status").innerHTML;

//     alert(stat_Value);
// }

$(document).ready(function () {

    $(".div2").hide();
    $(".div5").hide();
    $(".div6").hide();
    $(".divU").hide();
    $(".div7").hide();


    $(".div3_R").hide();
    $(".div4_R").hide();
    $(".div3_A").hide();
    $(".div3_B").hide();
    $(".div4_B").hide();

    // Navigate between Sections
    //div1
    $(".btn_n1").click(function () {

        if (objector_key == "Owner") {

            const emailValue = document.getElementById("o_cd_5").value;


            if (emailValue == '') {
                document.getElementById("o_cd_5").style.border = "2px solid red";
                fo_o = 4;
            } else {
                // Validate email format using regular expression

                //const emailPattern = /\S+@\S+\.\S+/;
                const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
                if (emailPattern.test(emailValue)) {
                    document.getElementById("o_cd_5").style.border = "";
                    fo_o = 0;
                } else {
                    // Invalid email format
                    document.getElementById("o_cd_5").style.border = "2px solid red";
                    fo_o = 4;
                    return false;
                }
            }
            if ((document.getElementById("o_p_1").value) == '') {
                document.getElementById("o_p_1").style.border = "2px solid red";
                fo_o = 3;
            } else {
                document.getElementById("o_p_1").style.border = "";
            }

            if ((document.getElementById("o_p_2").value) == '') {
                document.getElementById("o_p_2").style.border = "2px solid red";
                fo_o = 3;
            } else {
                document.getElementById("o_p_2").style.border = "";
            }

            if ((document.getElementById("o_p_3").value) == '') {
                document.getElementById("o_p_3").style.border = "2px solid red";
                fo_o = 3;
            } else {
                document.getElementById("o_p_3").style.border = "";
            }
            if ((document.getElementById("o_p_4").value) == '') {
                document.getElementById("o_p_4").style.border = "2px solid red";
                fo_o = 3;
            } else {
                document.getElementById("o_p_4").style.border = "";
            }

            if ((document.getElementById("o_p_5").value) == '' || (document.getElementById("o_p_5").value.length) < 4) {
                document.getElementById("o_p_5").style.border = "2px solid red";
                fo_o = 3;
            } else {
                document.getElementById("o_p_5").style.border = "";
                fo_o = 0;
            }

            if ((document.getElementById("o_st_1").value) == '') {
                document.getElementById("o_st_1").style.border = "2px solid red";
                document.getElementById("o_st_1").focus();
            } else {
                document.getElementById("o_st_1").style.border = "";
            }

            if ((document.getElementById("o_st_2").value) == '') {
                document.getElementById("o_st_2").style.border = "2px solid red";
                fo_o = 2;
            } else {
                document.getElementById("o_st_2").style.border = "";
            }

            if ((document.getElementById("o_st_3").value) == '') {
                document.getElementById("o_st_3").style.border = "2px solid red";
                fo_o = 2;
            } else {
                document.getElementById("o_st_3").style.border = "";
            }
            if ((document.getElementById("o_st_4").value) == '') {
                document.getElementById("o_st_4").style.border = "2px solid red";
                fo_o = 2;
            } else {
                document.getElementById("o_st_4").style.border = "";
            }

            if ((document.getElementById("o_st_5").value) == '' || (document.getElementById("o_st_5").value.length) < 4) {
                document.getElementById("o_st_5").style.border = "2px solid red";
                fo_o = 2;
            } else {
                document.getElementById("o_st_5").style.border = "";
                fo_o = 0;
            }

            if (fo_o == 1) {
                document.getElementById("o_id").focus();
            } if (fo_o == 2) {
                document.getElementById("o_st_1").focus();
            } if (fo_o == 3) {
                document.getElementById("o_p_1").focus();
            } if (fo_o == 4) {
                document.getElementById("o_cd_5").focus();
            }


            if (LuhnAlgo() == 'Invalid ID Number') {
                document.getElementById("o_id").style.border = "2px solid red";
                document.getElementById("o_id").focus();
                alert("Invalid ID Number");

            }
            else {
                document.getElementById("o_id").style.border = "";
            }
            const oCd2Value = document.getElementById("o_cd_2").value;
            const oCd3Value = document.getElementById("o_cd_3").value;
            const phoneNumberPattern = /^0\d{9}$/; // Regular expression for a phone number with 10 digits starting with 0

            if ((document.getElementById("o_cd_1").value) == '' &&
                oCd2Value == '' &&
                oCd3Value == '' &&
                (document.getElementById("o_cd_4").value) == ''
            ) {
                document.getElementById("o_cd_invalid").innerHTML = "Please fill at least one of the contact details fields.";
                document.getElementById("o_cd_invalid").style.color = "red";
                document.getElementById("o_cd_2").style.border = "2px solid red";
                document.getElementById("o_cd_3").style.border = "2px solid red";
                return false;
            } else {
                cd_o = 'true';
                document.getElementById("o_cd_invalid").innerHTML = "";
                document.getElementById("o_cd_1").style.border = "";
                document.getElementById("o_cd_2").style.border = "";
                document.getElementById("o_cd_3").style.border = "";
            }

            // Validate o_cd_2 if not empty
            if (oCd2Value !== '') {
                let errorMessage = "";

                if (!oCd2Value.startsWith('0')) {
                    errorMessage = "Phone number must start with 0 and be exactly 10 digits.";
                } else if (oCd2Value.length < 10) {
                    errorMessage = "Phone number must be exactly 10 digits starting with 0.";
                } else if (oCd2Value.length > 10) {
                    errorMessage = "Phone number cannot be more than 10 digits. Please enter exactly 10 digits starting with 0.";
                } else if (!/^\d+$/.test(oCd2Value)) {
                    errorMessage = "Phone number must contain only digits.";
                }

                if (errorMessage !== "") {
                    document.getElementById("o_cd_2").style.border = "2px solid red";
                    let messageElement = document.createElement("span");
                    messageElement.innerText = errorMessage;
                    messageElement.style.color = "red";
                    messageElement.id = "rep_cd_2_message";
                    document.getElementById("o_cd_2").parentNode.insertBefore(messageElement, document.getElementById("o_cd_2").nextSibling);
                    document.getElementById("o_cd_2").focus();
                    setTimeout(function () {
                        if (messageElement) {
                            messageElement.remove();
                        }
                    }, 5000);
                    return false;
                } else {
                    document.getElementById("o_cd_2").style.border = "";
                }
            }

            // Validate o_cd_3 if not empty
            if (oCd3Value !== '') {
                let errorMessage = "";

                if (!oCd3Value.startsWith('0')) {
                    errorMessage = "Phone number must start with 0 and be exactly 10 digits.";
                } else if (oCd3Value.length < 10) {
                    errorMessage = "Phone number must be exactly 10 digits starting with 0.";
                } else if (oCd3Value.length > 10) {
                    errorMessage = "Phone number cannot be more than 10 digits. Please enter exactly 10 digits starting with 0.";
                } else if (!/^\d+$/.test(oCd3Value)) {
                    errorMessage = "Phone number must contain only digits.";
                }

                if (errorMessage !== "") {
                    document.getElementById("o_cd_3").style.border = "2px solid red";
                    let messageElement = document.createElement("span");
                    messageElement.innerText = errorMessage;
                    messageElement.style.color = "red";
                    messageElement.id = "rep_cd_2_message";
                    document.getElementById("o_cd_3").parentNode.insertBefore(messageElement, document.getElementById("o_cd_3").nextSibling);
                    document.getElementById("o_cd_3").focus();
                    setTimeout(function () {
                        if (messageElement) {
                            messageElement.remove();
                        }
                    }, 5000);
                    return false;
                } else {
                    document.getElementById("o_cd_3").style.border = "";
                }
            }

            if ((document.getElementById("o_st_1").value) !== '' &&
                (document.getElementById("o_st_2").value) !== '' &&
                (document.getElementById("o_st_3").value) !== '' &&
                (document.getElementById("o_st_4").value) !== '' &&
                (document.getElementById("o_st_5").value) !== '' &&
                (document.getElementById("o_p_1").value) !== '' &&
                (document.getElementById("o_p_2").value) !== '' &&
                (document.getElementById("o_p_3").value) !== '' &&
                (document.getElementById("o_p_4").value) !== '' &&
                (document.getElementById("o_p_5").value) !== '' &&
                (document.getElementById("o_cd_5").value) !== '' &&
                (cd_o) == 'true' &&
                LuhnAlgo() !== 'Invalid ID Number'
            ) {

                $(".div1").hide();
                $(".div2").show();
                $("#form_back").hide();
                document.getElementById("phy_c").focus();
            }
        }


        if (objector_key == "Third_Party") {
            if ((document.getElementById("objector_name").value) == '') {
                document.getElementById("objector_name").style.border = "2px solid red";
                document.getElementById("objector_name").focus();
            } else {
                document.getElementById("objector_name").style.border = "";
            }

            if (LuhnAlgo() == "Invalid ID Number") {
                alert("Invalid ID Number");
                document.getElementById("objector_id").style.border = "2px solid red";
            }
            else {
                document.getElementById("objector_id").style.border = "";
            }
            if ((document.getElementById("obj_p_1").value) == '') {
                document.getElementById("obj_p_1").style.border = "2px solid red";
            } else {
                document.getElementById("obj_p_1").style.border = "";
            }
            if ((document.getElementById("obj_p_2").value) == '') {
                document.getElementById("obj_p_2").style.border = "2px solid red";
            } else {
                document.getElementById("obj_p_2").style.border = "";
            }
            if ((document.getElementById("obj_p_3").value) == '') {
                document.getElementById("obj_p_3").style.border = "2px solid red";
            } else {
                document.getElementById("obj_p_3").style.border = "";
            }
            if ((document.getElementById("obj_p_4").value) == '') {
                document.getElementById("obj_p_4").style.border = "2px solid red";
            } else {
                document.getElementById("obj_p_4").style.border = "";
            }
            if ((document.getElementById("obj_p_5").value) == '' || (document.getElementById("obj_p_5").value.length) < 4) {
                document.getElementById("obj_p_5").style.border = "2px solid red";
            } else {
                document.getElementById("obj_p_5").style.border = "";
            }
            if ((document.getElementById("objector_stat").value) == '') {
                document.getElementById("objector_stat").style.border = "2px solid red";
            } else {
                document.getElementById("objector_stat").style.border = "";
            }
            const emailValue = document.getElementById("obj_cd_5").value;


            if (emailValue == '') {
                document.getElementById("obj_cd_5").style.border = "2px solid red";

            } else {
                // Validate email format using regular expression

                //const emailPattern = /\S+@\S+\.\S+/;
                const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
                if (emailPattern.test(emailValue)) {
                    document.getElementById("obj_cd_5").style.border = "";

                } else {
                    // Invalid email format
                    document.getElementById("obj_cd_5").style.border = "2px solid red";
                    return false;
                }
            }
            const oCd2Value = document.getElementById("obj_cd_2").value;
            const oCd3Value = document.getElementById("obj_cd_3").value;
            const phoneNumberPattern = /^0\d{9}$/; // Regular expression for a phone number with 10 digits starting with 0

            if ((document.getElementById("obj_cd_1").value) == '' &&
                oCd2Value == '' &&
                oCd3Value == '' &&
                (document.getElementById("obj_cd_4").value) == ''
            ) {
                document.getElementById("obj_cd_invalid").innerHTML = "Please fill at least one of the contact details fields.";
                document.getElementById("obj_cd_invalid").style.color = "red";
                document.getElementById("obj_cd_2").style.border = "2px solid red";
                document.getElementById("obj_cd_3").style.border = "2px solid red";
                return false;
            } else {
                cd_obj = 'true';
                document.getElementById("obj_cd_invalid").innerHTML = "";
                document.getElementById("obj_cd_1").style.border = "";
                document.getElementById("obj_cd_2").style.border = "";
                document.getElementById("obj_cd_3").style.border = "";
            }

            // Validate o_cd_2 if not empty
            if (oCd2Value !== '') {
                let errorMessage = "";

                if (!oCd2Value.startsWith('0')) {
                    errorMessage = "Phone number must start with 0 and be exactly 10 digits.";
                } else if (oCd2Value.length < 10) {
                    errorMessage = "Phone number must be exactly 10 digits starting with 0.";
                } else if (oCd2Value.length > 10) {
                    errorMessage = "Phone number cannot be more than 10 digits. Please enter exactly 10 digits starting with 0.";
                } else if (!/^\d+$/.test(oCd2Value)) {
                    errorMessage = "Phone number must contain only digits.";
                }

                if (errorMessage !== "") {
                    document.getElementById("obj_cd_2").style.border = "2px solid red";
                    let messageElement = document.createElement("span");
                    messageElement.innerText = errorMessage;
                    messageElement.style.color = "red";
                    messageElement.id = "rep_cd_2_message";
                    document.getElementById("obj_cd_2").parentNode.insertBefore(messageElement, document.getElementById("obj_cd_2").nextSibling);
                    document.getElementById("obj_cd_2").focus();
                    setTimeout(function () {
                        if (messageElement) {
                            messageElement.remove();
                        }
                    }, 5000);
                    return false;
                } else {
                    document.getElementById("obj_cd_2").style.border = "";
                }
            }



            if (oCd3Value !== '') {
                let errorMessage = "";

                if (!oCd3Value.startsWith('0')) {
                    errorMessage = "Phone number must start with 0 and be exactly 10 digits.";
                } else if (oCd3Value.length < 10) {
                    errorMessage = "Phone number must be exactly 10 digits starting with 0.";
                } else if (oCd3Value.length > 10) {
                    errorMessage = "Phone number cannot be more than 10 digits. Please enter exactly 10 digits starting with 0.";
                } else if (!/^\d+$/.test(oCd3Value)) {
                    errorMessage = "Phone number must contain only digits.";
                }

                if (errorMessage !== "") {
                    document.getElementById("obj_cd_3").style.border = "2px solid red";
                    let messageElement = document.createElement("span");
                    messageElement.innerText = errorMessage;
                    messageElement.style.color = "red";
                    messageElement.id = "rep_cd_2_message";
                    document.getElementById("obj_cd_3").parentNode.insertBefore(messageElement, document.getElementById("obj_cd_3").nextSibling);
                    document.getElementById("obj_cd_3").focus();
                    setTimeout(function () {
                        if (messageElement) {
                            messageElement.remove();
                        }
                    }, 5000);
                    return false;
                } else {
                    document.getElementById("obj_cd_3").style.border = "";
                }
            }


            if ((document.getElementById("objector_name").value) !== '' &&
                (document.getElementById("obj_p_1").value) !== '' &&
                (document.getElementById("obj_p_2").value) !== '' &&
                (document.getElementById("obj_p_3").value) !== '' &&
                (document.getElementById("obj_p_4").value) !== '' &&
                (document.getElementById("obj_p_5").value) !== '' &&
                (document.getElementById("obj_cd_5").value) !== '' &&
                (document.getElementById("objector_stat").value) !== '' &&
                (cd_obj) == 'true' &&
                LuhnAlgo() !== 'Invalid ID Number'
            ) {

                $(".div1").hide();
                $(".div2").show();
                $("#form_back").hide();
                document.getElementById("phy_c").focus();
            }
        }

        if (objector_key == "Representative") {
            if ((document.getElementById("rep_name").value) == '') {
                document.getElementById("rep_name").style.border = "2px solid red";
            } else {
                document.getElementById("rep_name").style.border = "";
            }
            if ((document.getElementById("o_name").value) == '') {
                document.getElementById("o_name").style.border = "2px solid red";
            } else {
                document.getElementById("o_name").style.border = "";
            }
            if ((document.getElementById("rep_p_1").value) == '') {
                document.getElementById("rep_p_1").style.border = "2px solid red";
            } else {
                document.getElementById("rep_p_1").style.border = "";
            }
            if ((document.getElementById("rep_p_2").value) == '') {
                document.getElementById("rep_p_2").style.border = "2px solid red";
            } else {
                document.getElementById("rep_p_2").style.border = "";
            }
            if ((document.getElementById("rep_p_3").value) == '') {
                document.getElementById("rep_p_3").style.border = "2px solid red";
            } else {
                document.getElementById("rep_p_3").style.border = "";
            }
            if ((document.getElementById("rep_p_4").value) == '') {
                document.getElementById("rep_p_4").style.border = "2px solid red";
            } else {
                document.getElementById("rep_p_4").style.border = "";
            }
            if ((document.getElementById("rep_p_5").value) == '' || (document.getElementById("rep_p_5").value.length) < 4) {
                document.getElementById("rep_p_5").style.border = "2px solid red";
            } else {
                document.getElementById("rep_p_5").style.border = "";
            }
            const emailValue = document.getElementById("o_cd_5").value;


            if (emailValue == '') {
                document.getElementById("o_cd_5").style.border = "2px solid red";

            } else {
                // Validate email format using regular expression

                //const emailPattern = /\S+@\S+\.\S+/;
                const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
                if (emailPattern.test(emailValue)) {
                    document.getElementById("o_cd_5").style.border = "";

                } else {
                    // Invalid email format
                    document.getElementById("o_cd_5").style.border = "2px solid red";
                    return false;
                }
            }
            const email = document.getElementById("rep_cd_5").value;


            if (email == '') {
                document.getElementById("rep_cd_5").style.border = "2px solid red";
                return false;
            } else {
                // Validate email format using regular expression

                //const emailPattern = /\S+@\S+\.\S+/;
                const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
                if (emailPattern.test(email)) {
                    document.getElementById("rep_cd_5").style.border = "";

                } else {
                    // Invalid email format
                    document.getElementById("rep_cd_5").style.border = "2px solid red";
                    return false;
                }
            }
            if (document.getElementById("fileR").files.length == 0) {
                document.getElementById("fileR").style.border = "2px solid red";
                alert("Representative must upload their Authorization Letter to proceed.");
            } else {
                if (document.getElementById("fileR").files.item(0).name.length > 100) {
                    alert("File name too long.");
                    document.getElementById("fileR").value = '';
                } else {
                    document.getElementById("fileR").style.border = "";
                }
            }

            const repCd2Value = document.getElementById("rep_cd_2").value;
            const repCd3Value = document.getElementById("rep_cd_3").value;
            const repPhoneNumberPattern = /^0\d{9}$/; // Regular expression for a phone number with 10 digits starting with 0

            if ((document.getElementById("rep_cd_1").value) == '' &&
                repCd2Value == '' &&
                repCd3Value == '' &&
                (document.getElementById("rep_cd_4").value) == ''
            ) {
                document.getElementById("rep_cd_invalid").innerHTML = "Please fill at least one of the contact details fields.";
                document.getElementById("rep_cd_invalid").style.color = "red";
                document.getElementById("rep_cd_2").style.border = "2px solid red";
                document.getElementById("rep_cd_3").style.border = "2px solid red";
                return false;
            } else {
                cd_rep = 'true';
                document.getElementById("rep_cd_invalid").innerHTML = "";
                document.getElementById("rep_cd_1").style.border = "";
                document.getElementById("rep_cd_2").style.border = "";
                document.getElementById("rep_cd_3").style.border = "";
            }

            // Validate o_cd_2 if not empty
            if (repCd2Value !== '') {
                let errorMessage = "";

                if (!repCd2Value.startsWith('0')) {
                    errorMessage = "Phone number must start with 0 and be exactly 10 digits.";
                } else if (repCd2Value.length < 10) {
                    errorMessage = "Phone number must be exactly 10 digits starting with 0.";
                } else if (repCd2Value.length > 10) {
                    errorMessage = "Phone number cannot be more than 10 digits. Please enter exactly 10 digits starting with 0.";
                } else if (!/^\d+$/.test(repCd2Value)) {
                    errorMessage = "Phone number must contain only digits.";
                }

                if (errorMessage !== "") {
                    document.getElementById("rep_cd_2").style.border = "2px solid red";
                    let messageElement = document.createElement("span");
                    messageElement.innerText = errorMessage;
                    messageElement.style.color = "red";
                    messageElement.id = "rep_cd_2_message";
                    document.getElementById("rep_cd_2").parentNode.insertBefore(messageElement, document.getElementById("rep_cd_2").nextSibling);
                    document.getElementById("rep_cd_2").focus();
                    setTimeout(function () {
                        if (messageElement) {
                            messageElement.remove();
                        }
                    }, 5000);
                    return false;
                } else {
                    document.getElementById("rep_cd_2").style.border = "";
                }
            }



            // Validate o_cd_3 if not empty
            if (repCd3Value !== '') {
                let errorMessage = "";

                if (!repCd3Value.startsWith('0')) {
                    errorMessage = "Phone number must start with 0 and be exactly 10 digits.";
                } else if (repCd3Value.length < 10) {
                    errorMessage = "Phone number must be exactly 10 digits starting with 0.";
                } else if (repCd3Value.length > 10) {
                    errorMessage = "Phone number cannot be more than 10 digits. Please enter exactly 10 digits starting with 0.";
                } else if (!/^\d+$/.test(repCd3Value)) {
                    errorMessage = "Phone number must contain only digits.";
                }

                if (errorMessage !== "") {
                    document.getElementById("rep_cd_3").style.border = "2px solid red";
                    let messageElement = document.createElement("span");
                    messageElement.innerText = errorMessage;
                    messageElement.style.color = "red";
                    messageElement.id = "rep_cd_2_message";
                    document.getElementById("rep_cd_3").parentNode.insertBefore(messageElement, document.getElementById("rep_cd_3").nextSibling);
                    document.getElementById("rep_cd_3").focus();
                    setTimeout(function () {
                        if (messageElement) {
                            messageElement.remove();
                        }
                    }, 5000);
                    return false;
                } else {
                    document.getElementById("rep_cd_3").style.border = "";
                }
            }
            //Owner Contact

            const oCd2Value = document.getElementById("o_cd_2").value;
            const oCd3Value = document.getElementById("o_cd_3").value;
            const phoneNumberPattern = /^0\d{9}$/; // Regular expression for a phone number with 10 digits starting with 0

            if ((document.getElementById("o_cd_1").value) == '' &&
                oCd2Value == '' &&
                oCd3Value == '' &&
                (document.getElementById("o_cd_4").value) == ''
            ) {
                document.getElementById("o_cd_invalid").innerHTML = "Please fill at least one of the contact details fields.";
                document.getElementById("o_cd_invalid").style.color = "red";
                document.getElementById("o_cd_2").style.border = "2px solid red";
                document.getElementById("o_cd_3").style.border = "2px solid red";
                return false;
            } else {
                cd_o = 'true';
                document.getElementById("o_cd_invalid").innerHTML = "";
                document.getElementById("o_cd_1").style.border = "";
                document.getElementById("o_cd_2").style.border = "";
                document.getElementById("o_cd_3").style.border = "";
            }

            // Validate o_cd_2 if not empty
            if (oCd2Value !== '') {
                let errorMessage = "";

                if (!oCd2Value.startsWith('0')) {
                    errorMessage = "Phone number must start with 0 and be exactly 10 digits.";
                } else if (oCd2Value.length < 10) {
                    errorMessage = "Phone number must be exactly 10 digits starting with 0.";
                } else if (oCd2Value.length > 10) {
                    errorMessage = "Phone number cannot be more than 10 digits. Please enter exactly 10 digits starting with 0.";
                } else if (!/^\d+$/.test(oCd2Value)) {
                    errorMessage = "Phone number must contain only digits.";
                }

                if (errorMessage !== "") {
                    document.getElementById("o_cd_2").style.border = "2px solid red";
                    let messageElement = document.createElement("span");
                    messageElement.innerText = errorMessage;
                    messageElement.style.color = "red";
                    messageElement.id = "rep_cd_2_message";
                    document.getElementById("o_cd_2").parentNode.insertBefore(messageElement, document.getElementById("o_cd_2").nextSibling);
                    document.getElementById("o_cd_2").focus();
                    setTimeout(function () {
                        if (messageElement) {
                            messageElement.remove();
                        }
                    }, 5000);
                    return false;
                } else {
                    document.getElementById("o_cd_2").style.border = "";
                }
            }


            // Validate o_cd_3 if not empty
            if (oCd3Value !== '') {
                let errorMessage = "";

                if (!oCd3Value.startsWith('0')) {
                    errorMessage = "Phone number must start with 0 and be exactly 10 digits.";
                } else if (oCd3Value.length < 10) {
                    errorMessage = "Phone number must be exactly 10 digits starting with 0.";
                } else if (oCd3Value.length > 10) {
                    errorMessage = "Phone number cannot be more than 10 digits. Please enter exactly 10 digits starting with 0.";
                } else if (!/^\d+$/.test(oCd3Value)) {
                    errorMessage = "Phone number must contain only digits.";
                }

                if (errorMessage !== "") {
                    document.getElementById("o_cd_3").style.border = "2px solid red";
                    let messageElement = document.createElement("span");
                    messageElement.innerText = errorMessage;
                    messageElement.style.color = "red";
                    messageElement.id = "rep_cd_2_message";
                    document.getElementById("o_cd_3").parentNode.insertBefore(messageElement, document.getElementById("o_cd_3").nextSibling);
                    document.getElementById("o_cd_3").focus();
                    setTimeout(function () {
                        if (messageElement) {
                            messageElement.remove();
                        }
                    }, 5000);
                    return false;
                } else {
                    document.getElementById("o_cd_3").style.border = "";
                }
            }



            if ((document.getElementById("rep_name").value) !== '' &&
                (document.getElementById("o_name").value) !== '' &&
                (document.getElementById("rep_p_1").value) !== '' &&
                (document.getElementById("rep_p_2").value) !== '' &&
                (document.getElementById("rep_p_3").value) !== '' &&
                (document.getElementById("rep_p_4").value) !== '' &&
                (document.getElementById("rep_p_5").value) !== '' &&
                (document.getElementById("o_cd_5").value) !== '' &&
                (document.getElementById("o_cd_5").value) !== '' &&
                document.getElementById("fileR").files.length !== 0 &&
                document.getElementById("fileR").files.item(0).name.length < 100 &&
                (cd_rep) == 'true' &&
                (cd_o) == 'true'
            ) {
                $(".div1").hide();
                $(".div2").show();
                $("#form_back").hide();
                document.getElementById("phy_c").focus();
            }
        }


    });

        //div2
        $(".btn_p2").click(function () {
            
            $(".div1").show();
            $(".div2").hide();
            $("#form_back").show();
        });

        $(".btn_n2").click(function () {
            
                if ((document.getElementById("phy_c").value) == '') {
                    document.getElementById("phy_c").style.border = "2px solid red";
                } else {
                    document.getElementById("phy_c").style.border = "";
                    $(".div2").hide();
                    $(".div3_R").show();
                    document.getElementById("s3r").focus();
                }                
        });

        //div3
        
        $(".btn_R_p3").click(function () {
                $(".div2").show();
                $(".div3_R").hide();
        });
        $(".btn_A_p3").click(function () {
                $(".div3_R").show();
                $(".div3_A").hide();
        });
        $(".btn_B_p3").click(function () {
                $(".div3_A").show();
                $(".div3_B").hide();
        });

        $(".btn_R_n3").click(function () { 
                $(".div3_R").hide();
                $(".div3_A").show();
                document.getElementById("s3a").focus();
        });
        $(".btn_A_n3").click(function () {
            $(".div3_A").hide();
            $(".div3_B").show();
            document.getElementById("s3b").focus();
        });
        $(".btn_B_n3").click(function () {
            $(".div3_B").hide();
            $(".div4_R").show();
            document.getElementById("sch_name").focus();
        });
    
        //div4

        $(".btn_R_p4").click(function () {
                $(".div3_B").show();
                $(".div4_R").hide();
        });
        $(".btn_B_p4").click(function () {
                $(".div4_R").show();
                $(".div4_B").hide();
        });

        $(".btn_R_n4").click(function () {
                $(".div4_R").hide();
                $(".div4_B").show();
            document.getElementById("sch_name_b").focus();
        });
        $(".btn_B_n4").click(function () {
                $(".div4_B").hide();
                $(".div5").show();
                document.getElementById("s5").focus();
        });

        //div5
        $(".btn_p5").click(function () {
                $(".div4_B").show();
                $(".div5").hide();
        });
        $(".btn_n5").click(function () {
            $(".div5").hide();
            $(".div6").show();
            document.getElementById("NewPropDesc").focus();
        });
        //div6
        $(".btn_p6").click(function () {
            $(".div5").show();
            $(".div6").hide();
        });
        //$(".btn_n6").click(function () {
        //    if (objector_key == "Owner") {
        //        if ((document.getElementById("NewCat").value) == '' &&
        //            (document.getElementById("NewMarketValue").value) == '' &&
        //            (document.getElementById("NewExtent").value) == '' &&
        //            (document.getElementById("NewPropDesc").value) == '' &&
        //            (document.getElementById("NewAddress").value) == '' &&
        //            (document.getElementById("NewOwner").value) == '' &&
        //            (document.getElementById("NewCat1").value) == '' &&
        //            (document.getElementById("NewMarketValue1").value) == '' &&
        //            (document.getElementById("NewExtent1").value) == '' &&
        //            (document.getElementById("NewCat2").value) == '' &&
        //            (document.getElementById("NewMarketValue2").value) == '' &&
        //            (document.getElementById("NewExtent2").value) == '' &&
        //            (document.getElementById("NewCat3").value) == '' &&
        //            (document.getElementById("NewMarketValue3").value) == '' &&
        //            (document.getElementById("NewExtent3").value) == ''

        //        ) {
        //            document.getElementById("new_change_invalid").innerHTML = "Please fill at least one of the change you want to make.";
        //            document.getElementById("new_change_invalid").style.color = "red";
        //            document.getElementById("NewCat").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue").style.border = "2px solid red";
        //            document.getElementById("NewExtent").style.border = "2px solid red";
        //            document.getElementById("NewPropDesc").style.border = "2px solid red";
        //            document.getElementById("NewAddress").style.border = "2px solid red";
        //            document.getElementById("NewOwner").style.border = "2px solid red";
        //            document.getElementById("NewCat1").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue1").style.border = "2px solid red";
        //            document.getElementById("NewExtent1").style.border = "2px solid red";
        //            document.getElementById("NewCat2").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue2").style.border = "2px solid red";
        //            document.getElementById("NewExtent2").style.border = "2px solid red";
        //            document.getElementById("NewCat3").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue3").style.border = "2px solid red";
        //            document.getElementById("NewExtent3").style.border = "2px solid red";
        //        }
        //        else {
        //            NewChange = 'true';
        //            document.getElementById("new_change_invalid").innerHTML = "";
        //            document.getElementById("NewCat").style.border = "";
        //            document.getElementById("NewMarketValue").style.border = "";
        //            document.getElementById("NewExtent").style.border = "";
        //            document.getElementById("NewPropDesc").style.border = "";
        //            document.getElementById("NewAddress").style.border = "";
        //            document.getElementById("NewOwner").style.border = "";
        //            document.getElementById("NewCat1").style.border = "";
        //            document.getElementById("NewMarketValue1").style.border = "";
        //            document.getElementById("NewExtent1").style.border = "";
        //            document.getElementById("NewCat2").style.border = "";
        //            document.getElementById("NewMarketValue2").style.border = "";
        //            document.getElementById("NewExtent2").style.border = "";
        //            document.getElementById("NewCat3").style.border = "";
        //            document.getElementById("NewMarketValue3").style.border = "";
        //            document.getElementById("NewExtent3").style.border = "";

        //            $(".div6").hide();
        //            $(".divU").show();
        //            document.getElementById("sectionUpload").focus();
        //        }
        //    }

        //    if (objector_key == "Third_Party") {
        //        if ((document.getElementById("NewCat").value) == '' &&
        //            (document.getElementById("NewMarketValue").value) == '' &&
        //            (document.getElementById("NewExtent").value) == '' &&
        //            (document.getElementById("NewPropDesc").value) == '' &&
        //            (document.getElementById("NewAddress").value) == '' &&
        //            (document.getElementById("NewOwner").value) == '' &&
        //            (document.getElementById("NewCat1").value) == '' &&
        //            (document.getElementById("NewMarketValue1").value) == '' &&
        //            (document.getElementById("NewExtent1").value) == '' &&
        //            (document.getElementById("NewCat2").value) == '' &&
        //            (document.getElementById("NewMarketValue2").value) == '' &&
        //            (document.getElementById("NewExtent2").value) == '' &&
        //            (document.getElementById("NewCat3").value) == '' &&
        //            (document.getElementById("NewMarketValue3").value) == '' &&
        //            (document.getElementById("NewExtent3").value) == ''
        //        ) {
        //            document.getElementById("new_change_invalid").innerHTML = "Please fill at least one of the change you want to make.";
        //            document.getElementById("new_change_invalid").style.color = "red";
        //            document.getElementById("NewCat").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue").style.border = "2px solid red";
        //            document.getElementById("NewExtent").style.border = "2px solid red";
        //            document.getElementById("NewPropDesc").style.border = "2px solid red";
        //            document.getElementById("NewAddress").style.border = "2px solid red";
        //            document.getElementById("NewOwner").style.border = "2px solid red";
        //            document.getElementById("NewCat1").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue1").style.border = "2px solid red";
        //            document.getElementById("NewExtent1").style.border = "2px solid red";
        //            document.getElementById("NewCat2").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue2").style.border = "2px solid red";
        //            document.getElementById("NewExtent2").style.border = "2px solid red";
        //            document.getElementById("NewCat3").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue3").style.border = "2px solid red";
        //            document.getElementById("NewExtent3").style.border = "2px solid red";
        //        }
        //        else {
        //            NewChange = 'true';
        //            document.getElementById("new_change_invalid").innerHTML = "";
        //            document.getElementById("NewCat").style.border = "";
        //            document.getElementById("NewMarketValue").style.border = "";
        //            document.getElementById("NewExtent").style.border = "";
        //            document.getElementById("NewPropDesc").style.border = "";
        //            document.getElementById("NewAddress").style.border = "";
        //            document.getElementById("NewOwner").style.border = "";
        //            document.getElementById("NewCat1").style.border = "";
        //            document.getElementById("NewMarketValue1").style.border = "";
        //            document.getElementById("NewExtent1").style.border = "";
        //            document.getElementById("NewCat2").style.border = "";
        //            document.getElementById("NewMarketValue2").style.border = "";
        //            document.getElementById("NewExtent2").style.border = "";
        //            document.getElementById("NewCat3").style.border = "";
        //            document.getElementById("NewMarketValue3").style.border = "";
        //            document.getElementById("NewExtent3").style.border = "";

        //            $(".div6").hide();
        //            $(".divU").show();
        //            document.getElementById("sectionUpload").focus();
        //        }
        //    }

        //    if (objector_key == "Representative") {
        //        if ((document.getElementById("NewCat").value) == '' &&
        //            (document.getElementById("NewMarketValue").value) == '' &&
        //            (document.getElementById("NewExtent").value) == '' &&
        //            (document.getElementById("NewPropDesc").value) == '' &&
        //            (document.getElementById("NewAddress").value) == '' &&
        //            (document.getElementById("NewOwner").value) == '' &&
        //            (document.getElementById("NewCat1").value) == '' &&
        //            (document.getElementById("NewMarketValue1").value) == '' &&
        //            (document.getElementById("NewExtent1").value) == '' &&
        //            (document.getElementById("NewCat2").value) == '' &&
        //            (document.getElementById("NewMarketValue2").value) == '' &&
        //            (document.getElementById("NewExtent2").value) == '' &&
        //            (document.getElementById("NewCat3").value) == '' &&
        //            (document.getElementById("NewMarketValue3").value) == '' &&
        //            (document.getElementById("NewExtent3").value) == ''
        //        ) {
        //            document.getElementById("new_change_invalid").innerHTML = "Please fill at least one of the change you want to make.";
        //            document.getElementById("new_change_invalid").style.color = "red";
        //            document.getElementById("NewCat").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue").style.border = "2px solid red";
        //            document.getElementById("NewExtent").style.border = "2px solid red";
        //            document.getElementById("NewPropDesc").style.border = "2px solid red";
        //            document.getElementById("NewAddress").style.border = "2px solid red";
        //            document.getElementById("NewOwner").style.border = "2px solid red";
        //            document.getElementById("NewCat1").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue1").style.border = "2px solid red";
        //            document.getElementById("NewExtent1").style.border = "2px solid red";
        //            document.getElementById("NewCat2").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue2").style.border = "2px solid red";
        //            document.getElementById("NewExtent2").style.border = "2px solid red";
        //            document.getElementById("NewCat3").style.border = "2px solid red";
        //            document.getElementById("NewMarketValue3").style.border = "2px solid red";
        //            document.getElementById("NewExtent3").style.border = "2px solid red";
        //        }
        //        else {
        //            NewChange = 'true';
        //            document.getElementById("new_change_invalid").innerHTML = "";
        //            document.getElementById("NewCat").style.border = "";
        //            document.getElementById("NewMarketValue").style.border = "";
        //            document.getElementById("NewExtent").style.border = "";
        //            document.getElementById("NewPropDesc").style.border = "";
        //            document.getElementById("NewAddress").style.border = "";
        //            document.getElementById("NewOwner").style.border = "";
        //            document.getElementById("NewCat1").style.border = "";
        //            document.getElementById("NewMarketValue1").style.border = "";
        //            document.getElementById("NewExtent1").style.border = "";
        //            document.getElementById("NewCat2").style.border = "";
        //            document.getElementById("NewMarketValue2").style.border = "";
        //            document.getElementById("NewExtent2").style.border = "";
        //            document.getElementById("NewCat3").style.border = "";
        //            document.getElementById("NewMarketValue3").style.border = "";
        //            document.getElementById("NewExtent3").style.border = "";

        //            $(".div6").hide();
        //            $(".divU").show();
        //            document.getElementById("sectionUpload").focus();
        //        }
        //    }

        //    //$(".div6").hide();
        //    //$(".divU").show();
        //});
    $(document).ready(function () {
        $(".btn_n6").off("click").on("click", function (e) {
            e.preventDefault();
            e.stopImmediatePropagation();

            if (!section6ValidateBeforeNext()) {
                return false;
            }

            NewChange = "true";

            const invalid = document.getElementById("new_change_invalid");

            if (invalid) {
                invalid.innerHTML = "";
                invalid.style.color = "";
            }

            $(".div6").hide();
            $(".divU").show();

            const upload = document.getElementById("sectionUpload");

            if (upload) {
                upload.focus();
            }

            return false;
        });
    });


        //divUpload
        $(".btn_pU").click(function () {
            $(".div6").show();
            $(".divU").hide();
        });
        $(".btn_nU").click(function () {
            $(".divU").hide();
            $(".div7").show();
            document.getElementById("sign_obj").focus();
        });
        //div7
        $(".btn_p7").click(function () {
            $(".divU").show();
            $(".div7").hide();
        });

    });

    //function sub() {
    //    alert(" your objection has been submitted");
    //        if (objector_key == "Owner") {
    //            alert($("#owner_name").value + " your objection has been submitted");
    //        }
    //        if (objector_key == "Third_Party") {
    //            alert($("#objector_name").value + " your objection has been submitted");
    //        }
    //        if (objector_key == "Representative") {
    //            alert($("#rep_name").value + " your objection has been submitted");
    //        }
    //}


$(function () {


    var canvas = document.querySelector('#signature');
    var pad = new SignaturePad(canvas);
    var data;
    function checkCanvas() {
        var canva = document.getElementById('signature');
        if (isCanvasEmpty(canva)) {
            document.getElementById("signature").style.border = "2px solid red";
        } else {
            data = pad.toDataURL();
            pad.off();
            $('#savetarget').attr('src', data);
            $('#SignatureDataUrl').val(data);
            $('#submitForm').removeAttr('disabled');
            document.getElementById("signature").style.border = "2px solid Black";
        }
    };
    function isCanvasEmpty(canvas) {
        const blankCanvas = document.createElement('canvas');
        blankCanvas.width = canvas.width;
        blankCanvas.height = canvas.height;
        return canvas.toDataURL() === blankCanvas.toDataURL();
    }
    $('#accept').click(function () {


        checkCanvas();


    });


    $('#Clear').click(function () {
        pad = new SignaturePad(canvas);
        pad.on();
        document.getElementById("submitForm").disabled = true;
    });
});


    $(document).ready(function () {
        // Display alert message after toggle paragraphs

        $("#affected-land").hide();
        $("#water-right").hide();
    });


    $(document).ready(function () {
        //Res Full Title
        $("#res_kitchins").hide();
        $("#res_lounges").hide();
        $("#res_dining_room").hide();
        $("#res_laundry").hide();
        $("#res_study").hide();
        $("#res_playroom").hide();
        $("#res_television").hide();
        $("#res_separate_toilets").hide();
        $("#res_lounge_dining_room").hide();

        //Res Sectional Title
        $("#res_st_kitchins").hide();
        $("#res_st_lounges").hide();
        $("#res_st_dining_room").hide();
        $("#res_st_laundry").hide();
        $("#res_st_study").hide();
        $("#res_st_playroom").hide();
        $("#res_st_television").hide();
        $("#res_st_separate_toilets").hide();
        $("#res_st_lounge_dining_room").hide();

        //Agric
        $("#agric_kitchins").hide();
        $("#agric_lounges").hide();
        $("#agric_dining_room").hide();
        $("#agric_laundry").hide();
        $("#agric_study").hide();
        $("#agric_playroom").hide();
        $("#agric_television").hide();
        $("#agric_separate_toilets").hide();
        $("#agric_lounge_dining_room").hide();

    });

    //****************Res Full Title***********//

    //Function to hide and show "Res Number of Kitchens" options
    function show_res_kitchins() {
        $("#res_kitchins").show();

        document.getElementById("kitchen_one").innerHTML = 1;
        document.getElementById("kitchen_two").innerHTML = 2;
        document.getElementById("kitchen_three").innerHTML = 3;
        document.getElementById("kitchen_four").innerHTML = 4;
        document.getElementById("kitchen_five").innerHTML = 5;

    }

    function hide_res_kitchins() {
        $("#res_kitchins").hide();

        document.getElementById("kitchen_one").innerHTML = 0;
        document.getElementById("kitchen_two").innerHTML = 0;
        document.getElementById("kitchen_three").innerHTML = 0;
        document.getElementById("kitchen_four").innerHTML = 0;
        document.getElementById("kitchen_five").innerHTML = 0;
    }

    //Function to hide and show "Res Number of lounge" options
    function show_res_lounge() {
        $("#res_lounges").show();

        document.getElementById("lounge_one").innerHTML = 1;
        document.getElementById("lounge_two").innerHTML = 2;
        document.getElementById("lounge_three").innerHTML = 3;
        document.getElementById("lounge_four").innerHTML = 4;
        document.getElementById("lounge_five").innerHTML = 5;

    }

    function hide_res_lounge() {
        $("#res_lounges").hide();

        document.getElementById("lounge_one").innerHTML = 0;
        document.getElementById("lounge_two").innerHTML = 0;
        document.getElementById("lounge_three").innerHTML = 0;
        document.getElementById("lounge_four").innerHTML = 0;
        document.getElementById("lounge_five").innerHTML = 0;
    }

    // //Function to hide and show "Res Number of Dining Rooms" options
    function show_res_dining_room() {
        $("#res_dining_room").show();

        document.getElementById("dining_room_one").innerHTML = 1;
        document.getElementById("dining_room_two").innerHTML = 2;
        document.getElementById("dining_room_three").innerHTML = 3;
        document.getElementById("dining_room_four").innerHTML = 4;
        document.getElementById("dining_room_five").innerHTML = 5;

    }

    function hide_res_dining_room() {
        $("#res_dining_room").hide();

        document.getElementById("dining_room_one").innerHTML = 0;
        document.getElementById("dining_room_two").innerHTML = 0;
        document.getElementById("dining_room_three").innerHTML = 0;
        document.getElementById("dining_room_four").innerHTML = 0;
        document.getElementById("dining_room_five").innerHTML = 0;
    }

    function show_res_laundry() {
        $("#res_laundry").show();

        document.getElementById("laundry_one").innerHTML = 1;
        document.getElementById("laundry_two").innerHTML = 2;
        document.getElementById("laundry_three").innerHTML = 3;
        document.getElementById("laundry_four").innerHTML = 4;
        document.getElementById("laundry_five").innerHTML = 5;
    }

    function hide_res_laundry() {
        $("#res_laundry").hide();

        document.getElementById("laundry_one").innerHTML = 0;
        document.getElementById("laundry_two").innerHTML = 0;
        document.getElementById("laundry_three").innerHTML = 0;
        document.getElementById("laundry_four").innerHTML = 0;
        document.getElementById("laundry_five").innerHTML = 0;
    }

    //Function to hide and show "Res Number of study rooms" options
    function show_res_study() {
        $("#res_study").show();

        document.getElementById("study_one").innerHTML = 1;
        document.getElementById("study_two").innerHTML = 2;
        document.getElementById("study_three").innerHTML = 3;
        document.getElementById("study_four").innerHTML = 4;
        document.getElementById("study_five").innerHTML = 5;

    }

    function hide_res_study() {
        $("#res_study").hide();

        document.getElementById("study_one").innerHTML = 0;
        document.getElementById("study_two").innerHTML = 0;
        document.getElementById("study_three").innerHTML = 0;
        document.getElementById("study_four").innerHTML = 0;
        document.getElementById("study_five").innerHTML = 0;
    }

    //Function to hide and show "Res Number of playroom" options
    function show_res_playroom() {
        $("#res_playroom").show();

        document.getElementById("playroom_one").innerHTML = 1;
        document.getElementById("playroom_two").innerHTML = 2;
        document.getElementById("playroom_three").innerHTML = 3;
        document.getElementById("playroom_four").innerHTML = 4;
        document.getElementById("playroom_five").innerHTML = 5;

    }

    function hide_res_playroom() {
        $("#res_playroom").hide();

        document.getElementById("playroom_one").innerHTML = 0;
        document.getElementById("playroom_two").innerHTML = 0;
        document.getElementById("playroom_three").innerHTML = 0;
        document.getElementById("playroom_four").innerHTML = 0;
        document.getElementById("playroom_five").innerHTML = 0;
    }

    //Function to hide and show "Res Number of television" options
    function show_res_television() {
        $("#res_television").show();

        document.getElementById("television_one").innerHTML = 1;
        document.getElementById("television_two").innerHTML = 2;
        document.getElementById("television_three").innerHTML = 3;
        document.getElementById("television_four").innerHTML = 4;
        document.getElementById("television_five").innerHTML = 5;

    }

    function hide_res_television() {
        $("#res_television").hide();

    }

    //Function to hide and show "Res Number of separate toilets" options
    function show_res_separate_toilets() {
        $("#res_separate_toilets").show();

        document.getElementById("separate_toilets_one").innerHTML = 1;
        document.getElementById("separate_toilets_two").innerHTML = 2;
        document.getElementById("separate_toilets_three").innerHTML = 3;
        document.getElementById("separate_toilets_four").innerHTML = 4;
        document.getElementById("separate_toilets_five").innerHTML = 5;

    }

    function hide_res_separate_toilets() {
        $("#res_separate_toilets").hide();

    }

    //Function to hide and show "Res Number of lounge with dining room" options
    function show_res_lounge_dining_room() {
        $("#res_lounge_dining_room").show();

        document.getElementById("lounge_dining_room_one").innerHTML = 1;
        document.getElementById("lounge_dining_room_two").innerHTML = 2;
        document.getElementById("lounge_dining_room_three").innerHTML = 3;
        document.getElementById("lounge_dining_room_four").innerHTML = 4;
        document.getElementById("lounge_dining_room_five").innerHTML = 5;

    }

    function hide_res_lounge_dining_room() {
        $("#res_lounge_dining_room").hide();

    }

    //******************Res Sectional Title *********************** */

    function show_res_st_kitchins() {
        $("#res_st_kitchins").show();

        document.getElementById("kitchen_st_one").innerHTML = 1;
        document.getElementById("kitchen_st_two").innerHTML = 2;
        document.getElementById("kitchen_st_three").innerHTML = 3;
        document.getElementById("kitchen_st_four").innerHTML = 4;
        document.getElementById("kitchen_st_five").innerHTML = 5;

    }

    function hide_res_st_kitchins() {
        $("#res_st_kitchins").hide();

    }

    //Function to hide and show "Res Number of lounge" options
    function show_res_st_lounge() {
        $("#res_st_lounges").show();

        document.getElementById("lounge_st_one").innerHTML = 1;
        document.getElementById("lounge_st_two").innerHTML = 2;
        document.getElementById("lounge_st_three").innerHTML = 3;
        document.getElementById("lounge_st_four").innerHTML = 4;
        document.getElementById("lounge_st_five").innerHTML = 5;

    }

    function hide_res_st_lounge() {
        $("#res_st_lounges").hide();

    }

    // //Function to hide and show "Res Number of Dining Rooms" options
    function show_res_st_dining_room() {
        $("#res_st_dining_room").show();

        document.getElementById("dining_room_st_one").innerHTML = 1;
        document.getElementById("dining_room_st_two").innerHTML = 2;
        document.getElementById("dining_room_st_three").innerHTML = 3;
        document.getElementById("dining_room_st_four").innerHTML = 4;
        document.getElementById("dining_room_st_five").innerHTML = 5;

    }

    function hide_res_st_dining_room() {
        $("#res_st_dining_room").hide();

    }

    function show_res_st_laundry() {
        $("#res_st_laundry").show();

        document.getElementById("laundry_st_one").innerHTML = 1;
        document.getElementById("laundry_st_two").innerHTML = 2;
        document.getElementById("laundry_st_three").innerHTML = 3;
        document.getElementById("laundry_st_four").innerHTML = 4;
        document.getElementById("laundry_st_five").innerHTML = 5;
    }

    function hide_res_st_laundry() {
        $("#res_st_laundry").hide();

    }

    //Function to hide and show "Res Number of study rooms" options
    function show_res_st_study() {
        $("#res_st_study").show();

        document.getElementById("study_st_one").innerHTML = 1;
        document.getElementById("study_st_two").innerHTML = 2;
        document.getElementById("study_st_three").innerHTML = 3;
        document.getElementById("study_st_four").innerHTML = 4;
        document.getElementById("study_st_five").innerHTML = 5;

    }

    function hide_res_st_study() {
        $("#res_st_study").hide();

    }

    //Function to hide and show "Res Number of playroom" options
    function show_res_st_playroom() {
        $("#res_st_playroom").show();

        document.getElementById("playroom_st_one").innerHTML = 1;
        document.getElementById("playroom_st_two").innerHTML = 2;
        document.getElementById("playroom_st_three").innerHTML = 3;
        document.getElementById("playroom_st_four").innerHTML = 4;
        document.getElementById("playroom_st_five").innerHTML = 5;

    }

    function hide_res_st_playroom() {
        $("#res_st_playroom").hide();

    }

    //Function to hide and show "Res Number of television" options
    function show_res_st_television() {
        $("#res_st_television").show();

        document.getElementById("television_st_one").innerHTML = 1;
        document.getElementById("television_st_two").innerHTML = 2;
        document.getElementById("television_st_three").innerHTML = 3;
        document.getElementById("television_st_four").innerHTML = 4;
        document.getElementById("television_st_five").innerHTML = 5;

    }

    function hide_res_st_television() {
        $("#res_st_television").hide();

    }

    //Function to hide and show "Res Number of separate toilets" options
    function show_res_st_separate_toilets() {
        $("#res_st_separate_toilets").show();

        document.getElementById("separate_toilets_st_one").innerHTML = 1;
        document.getElementById("separate_toilets_st_two").innerHTML = 2;
        document.getElementById("separate_toilets_st_three").innerHTML = 3;
        document.getElementById("separate_toilets_st_four").innerHTML = 4;
        document.getElementById("separate_toilets_st_five").innerHTML = 5;

    }

    function hide_res_st_separate_toilets() {
        $("#res_st_separate_toilets").hide();

    }

    //Function to hide and show "Res Number of lounge with dining room" options
    function show_res_st_lounge_dining_room() {
        $("#res_st_lounge_dining_room").show();

        document.getElementById("lounge_dining_room_st_one").innerHTML = 1;
        document.getElementById("lounge_dining_room_st_two").innerHTML = 2;
        document.getElementById("lounge_dining_room_st_three").innerHTML = 3;
        document.getElementById("lounge_dining_room_st_four").innerHTML = 4;
        document.getElementById("lounge_dining_room_st_five").innerHTML = 5;

    }

    function hide_res_st_lounge_dining_room() {
        $("#res_st_lounge_dining_room").hide();

    }

    //****************Agric Fill Title***********//

    //Function to hide and show "Agric Number of Kitchens" options
    function show_agric_kitchins() {
        $("#agric_kitchins").show();

        document.getElementById("agric_kitchen_one").innerHTML = 1;
        document.getElementById("agric_kitchen_two").innerHTML = 2;
        document.getElementById("agric_kitchen_three").innerHTML = 3;
        document.getElementById("agric_kitchen_four").innerHTML = 4;
        document.getElementById("agric_kitchen_five").innerHTML = 5;

    }

    function hide_agric_kitchins() {
        $("#agric_kitchins").hide();


    }

    //Function to hide and show "Agric Number of lounge" options
    function show_agric_lounge() {
        $("#agric_lounges").show();

        document.getElementById("agric_lounge_one").innerHTML = 1;
        document.getElementById("agric_lounge_two").innerHTML = 2;
        document.getElementById("agric_lounge_three").innerHTML = 3;
        document.getElementById("agric_lounge_four").innerHTML = 4;
        document.getElementById("agric_lounge_five").innerHTML = 5;

    }

    function hide_agric_lounge() {
        $("#agric_lounges").hide();

    }

    // //Function to hide and show "Agric Number of Dining Rooms" options
    function show_agric_dining_room() {
        $("#agric_dining_room").show();

        document.getElementById("agric_dining_room_one").innerHTML = 1;
        document.getElementById("agric_dining_room_two").innerHTML = 2;
        document.getElementById("agric_dining_room_three").innerHTML = 3;
        document.getElementById("agric_dining_room_four").innerHTML = 4;
        document.getElementById("agric_dining_room_five").innerHTML = 5;

    }

    function hide_agric_dining_room() {
        $("#agric_dining_room").hide();

    }

    function show_agric_laundry() {
        $("#agric_laundry").show();

        document.getElementById("agric_laundry_one").innerHTML = 1;
        document.getElementById("agric_laundry_two").innerHTML = 2;
        document.getElementById("agric_laundry_three").innerHTML = 3;
        document.getElementById("agric_laundry_four").innerHTML = 4;
        document.getElementById("agric_laundry_five").innerHTML = 5;
    }

    function hide_agric_laundry() {
        $("#agric_laundry").hide();

    }

    //Function to hide and show "Agric Number of study rooms" options
    function show_agric_study() {
        $("#agric_study").show();

        document.getElementById("agric_study_one").innerHTML = 1;
        document.getElementById("agric_study_two").innerHTML = 2;
        document.getElementById("agric_study_three").innerHTML = 3;
        document.getElementById("agric_study_four").innerHTML = 4;
        document.getElementById("agric_study_five").innerHTML = 5;

    }

    function hide_agric_study() {
        $("#agric_study").hide();

    }

    //Function to hide and show "Agric Number of playroom" options
    function show_agric_playroom() {
        $("#agric_playroom").show();

        document.getElementById("agric_playroom_one").innerHTML = 1;
        document.getElementById("agric_playroom_two").innerHTML = 2;
        document.getElementById("agric_playroom_three").innerHTML = 3;
        document.getElementById("agric_playroom_four").innerHTML = 4;
        document.getElementById("agric_playroom_five").innerHTML = 5;

    }

    function hide_agric_playroom() {
        $("#agric_playroom").hide();

    }

    //Function to hide and show "Agric Number of television" options
    function show_agric_television() {
        $("#agric_television").show();

        document.getElementById("agric_television_one").innerHTML = 1;
        document.getElementById("agric_television_two").innerHTML = 2;
        document.getElementById("agric_television_three").innerHTML = 3;
        document.getElementById("agric_television_four").innerHTML = 4;
        document.getElementById("agric_television_five").innerHTML = 5;

    }

    function hide_agric_television() {
        $("#agric_television").hide();

    }

    //Function to hide and show "Agric Number of separate toilets" options
    function show_agric_separate_toilets() {
        $("#agric_separate_toilets").show();

        document.getElementById("agric_separate_toilets_one").innerHTML = 1;
        document.getElementById("agric_separate_toilets_two").innerHTML = 2;
        document.getElementById("agric_separate_toilets_three").innerHTML = 3;
        document.getElementById("agric_separate_toilets_four").innerHTML = 4;
        document.getElementById("agric_separate_toilets_five").innerHTML = 5;

    }

    function hide_agric_separate_toilets() {
        $("#agric_separate_toilets").hide();

    }

    //Function to hide and show "Agric Number of lounge with dining room" options
    function show_agric_lounge_dining_room() {
        $("#agric_lounge_dining_room").show();

        document.getElementById("agric_lounge_dining_room_one").innerHTML = 1;
        document.getElementById("agric_lounge_dining_room_two").innerHTML = 2;
        document.getElementById("agric_lounge_dining_room_three").innerHTML = 3;
        document.getElementById("agric_lounge_dining_room_four").innerHTML = 4;
        document.getElementById("agric_lounge_dining_room_five").innerHTML = 5;

    }

    function hide_agric_lounge_dining_room() {
        $("#agric_lounge_dining_room").hide();

    }


// Jquery Dependency

$("input[data-type='currency']").on({
    keyup: function () {
        formatCurrency($(this));
    },
    blur: function () {
        formatCurrency($(this), "blur");
    }
});


function formatNumber(n) {
    return n.replace(/\D/g, "").replace(/\B(?=(\d{3})+(?!\d))/g, " ");
}


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


function validateCustomerName() {
    var validatedName = "";
    var restrictedCharactersArray = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "~", "`", "!", "@@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "_",
        "+", "=", "{", "}", "R", "[", "]", ":", ";", "'", "<", ">", ",", ".", "?", "/", "/\/", "|"];
    var customerName = document.getElementById("destinationTextField").value;
    var numberValidation = (/^[a-zA-Z_ ]+$/g).test(customerName);
    if (!numberValidation) {
        validatedName = "";
        var customerNameArray = customerName.split("");
        for (var i = 0; i < restrictedCharactersArray.length; i++) {
            var restrictedCharacter = restrictedCharactersArray[i];
            if (customerNameArray.indexOf(restrictedCharacter) !== -1) {
                for (var j = 0; j < customerNameArray.length; j++) {
                    var customerNameCharacter = customerNameArray[j];
                    if (customerNameCharacter !== restrictedCharacter) {
                        validatedName = validatedName + customerNameCharacter;
                    }
                }
            }
        }
        document.getElementById("destinationTextField").value = validatedName;
    }
}

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

    return "R " + Number(raw)
        .toLocaleString("en-ZA", {
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        })
        .replace(/[,\u00a0\u202f]/g, " ");
}

function syncMultiMoneyFieldsBeforeSubmit() {
    const pairs = [
        ["NewMarketValue", "NewMarketValueRaw"],
        ["NewMarketValue1", "NewMarketValueRaw1"],
        ["NewMarketValue2", "NewMarketValueRaw2"],
        ["NewMarketValue3", "NewMarketValueRaw3"]
    ];

    pairs.forEach(pair => {
        const visible = document.getElementById(pair[0]);
        const raw = document.getElementById(pair[1]);

        if (visible && raw) {
            raw.value = normaliseMoney(visible.value);
        }
    });
}

function initialiseMultiMoneyFormatting() {
    const pairs = [
        ["NewMarketValue", "NewMarketValueRaw"],
        ["NewMarketValue1", "NewMarketValueRaw1"],
        ["NewMarketValue2", "NewMarketValueRaw2"],
        ["NewMarketValue3", "NewMarketValueRaw3"]
    ];

    pairs.forEach(pair => {
        const visible = document.getElementById(pair[0]);
        const raw = document.getElementById(pair[1]);

        if (!visible || !raw) return;

        visible.addEventListener("input", function () {
            const clean = normaliseMoney(visible.value);
            raw.value = clean;
            visible.value = clean ? formatRand(clean) : "";
        });

        visible.addEventListener("blur", function () {
            const clean = normaliseMoney(visible.value);
            raw.value = clean;
            visible.value = clean ? formatRand(clean) : "";
        });
    });
}
function getSubmissionMode() {
    const appealHidden = document.getElementById("AppealStat")?.value;
    const sessionAppeal = sessionStorage.getItem("AppealStatus");

    const isAppeal =
        appealHidden === "True" ||
        appealHidden === "true" ||
        sessionAppeal === "True" ||
        sessionAppeal === "true";

    return isAppeal ? "Appeal" : "Objection";
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

        "multiple purposes": "multipurpose",
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

    return Number.isNaN(num)
        ? cleaned.toLowerCase()
        : num.toString();
}

function normaliseExtent(value) {
    if (!value) return "";

    let cleaned = value
        .toString()
        .replace(/\s/g, "")
        .replace(/,/g, ".")
        .trim();

    const num = Number(cleaned);

    return Number.isNaN(num)
        ? cleaned.toLowerCase()
        : num.toString();
}

function section6GetFieldPairs() {
    return [
        // General values
        {
            label: "Description of the Property/Unit",
            newId: "NewPropDesc",
            oldId: "desc",
            type: "text"
        },
        {
            label: "Physical Address / Door No. / Flat No.",
            newId: "NewAddress",
            oldId: "add",
            type: "text"
        },
        {
            label: "Name of Owner",
            newId: "NewOwner",
            oldId: "owner",
            type: "text"
        },

        // Purpose 1
        {
            label: "Purpose 1 Category",
            newId: "NewCat",
            oldId: "cat",
            type: "category"
        },
        {
            label: "Purpose 1 Extent",
            newId: "NewExtent",
            oldId: "extent",
            type: "extent"
        },
        {
            label: "Purpose 1 Market Value",
            newId: "NewMarketValue",
            oldId: "Market_Value",
            type: "market"
        },

        // Purpose 2
        {
            label: "Purpose 2 Category",
            newId: "NewCat1",
            oldId: "cat1",
            type: "category"
        },
        {
            label: "Purpose 2 Extent",
            newId: "NewExtent1",
            oldId: "extent1",
            type: "extent"
        },
        {
            label: "Purpose 2 Market Value",
            newId: "NewMarketValue1",
            oldId: "Market_Value1",
            type: "market"
        },

        // Purpose 3
        {
            label: "Purpose 3 Category",
            newId: "NewCat2",
            oldId: "cat2",
            type: "category"
        },
        {
            label: "Purpose 3 Extent",
            newId: "NewExtent2",
            oldId: "extent2",
            type: "extent"
        },
        {
            label: "Purpose 3 Market Value",
            newId: "NewMarketValue2",
            oldId: "Market_Value2",
            type: "market"
        },

        // Purpose 4
        {
            label: "Purpose 4 Category",
            newId: "NewCat3",
            oldId: "cat3",
            type: "category"
        },
        {
            label: "Purpose 4 Extent",
            newId: "NewExtent3",
            oldId: "extent3",
            type: "extent"
        },
        {
            label: "Purpose 4 Market Value",
            newId: "NewMarketValue3",
            oldId: "Market_Value3",
            type: "market"
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

    if (err) {
        err.remove();
    }
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
        err.style.marginTop = "4px";
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

            if (pair.type === "category") {
                resetCategoryDropdown(newEl);
            }
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
                    <h3 id="locusModalTitle">Different Values Required</h3>
                    <p id="locusModalSub">Validation failed</p>
                </div>
            </div>

            <div class="locus-modal-body">
                <p id="locusModalMessage">
                    You cannot continue with the same details that are reflected on the Valuation Roll / MVD.
                </p>

                <div id="locusDuplicateList" class="locus-duplicate-list"></div>
            </div>

            <div class="locus-modal-footer">
                <button type="button"
                        onclick="closeLocusStandModal()"
                        class="locus-modal-btn">
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

    if (modal) {
        modal.style.display = "none";
    }

    document.body.style.overflow = "";
}

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

function section6ValidateBeforeNext() {
    const mode = getSubmissionMode();
    const pairs = section6GetFieldPairs();

    pairs.forEach(pair => {
        const el = document.getElementById(pair.newId);

        if (el) {
            section6ClearFieldError(el);
        }
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
document.addEventListener("DOMContentLoaded", function () {
    ensureLocusStandModalExists();
    initialiseMultiMoneyFormatting();

    section6GetFieldPairs().forEach(pair => {
        const el = document.getElementById(pair.newId);

        if (!el) return;

        const eventName = el.tagName === "SELECT" ? "change" : "input";

        el.addEventListener(eventName, function () {
            if (isResettingCategoryDropdown) return;

            section6ValidateSameValues();
        });

        el.addEventListener("blur", function () {
            if (isResettingCategoryDropdown) return;

            section6ValidateSameValues();
        });
    });
});


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
