var property_key = sessionStorage.getItem('property_choice');
var objector_key = sessionStorage.getItem('objector_choice');
var AppealStatus = sessionStorage.getItem('AppealStatus');
var SubType = sessionStorage.getItem('query_choice');
var cd_o = 'false';
var cd_obj = 'false';
var cd_rep = 'false';
var NewChange = 'false';
var fo_o = 0;

var loader = document.getElementById("preloader");
window.addEventListener("load", function () {
    if (loader) {
        loader.style.display = "none";
    }
});

var userEmailElement = document.getElementById('userEmail');

if (userEmailElement) {
    var userEmail = userEmailElement.value;

    var regex = /^(val\.admin(1[0-9]?|[1-9])@joburg\.org\.za)$/i;

    if (regex.test(userEmail) || userEmail === 'AdministrationEnquiries@Joburg.org.za') {
        var capturer = document.getElementById('capturer');
        var sapNo = document.getElementById('sapNo');
        if (capturer) capturer.style.display = 'flex';
        if (sapNo) sapNo.setAttribute('required', 'required');
    } else {
        var hiddenCapturer = document.getElementById('capturer');
        var optionalSapNo = document.getElementById('sapNo');
        if (hiddenCapturer) hiddenCapturer.style.display = 'none';
        if (optionalSapNo) optionalSapNo.removeAttribute('required');
    }
} else {
    console.warn("Element with ID 'userEmail' not found in the document.");
}

document.getElementById('Objector_Type').value = sessionStorage.getItem('objector_choice');

document.getElementById('Property_Type').value = sessionStorage.getItem('property_choice');

var temp_ot = sessionStorage.getItem('objector_choice');
var temp_pt = sessionStorage.getItem('property_choice');

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

//Residential Function to hide other sections
if (document.getElementById("AppealStat").value !== null) {
    document.getElementById("AppealStat").value = sessionStorage.getItem('AppealStatus');
}
document.getElementById("AppealStat").value = sessionStorage.getItem('AppealStatus');
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
                if (ext.length > 100) {
                    alert("File name too long.");
                    input.value = '';
                    break;
                }
                if (fi >= 10240) {
                    alert("File too Big, please select a file less than 10,2mb.");
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

// Cumulative Multi-query upload: selections from different folders are retained.
(function initialiseMultiQueryUpload() {
    var evidenceInput = document.getElementById('objectionEvidenceInput');
    var browseButton = document.getElementById('objectionBrowseFiles');
    var dropzone = document.getElementById('objectionDropzone');
    var fileList = document.getElementById('objectionFileList');
    var fileCount = document.getElementById('objectionFileCount');
    var fileFill = document.getElementById('objectionFileFill');

    if (!evidenceInput || !browseButton || !dropzone || !fileList || !fileCount || !fileFill) return;

    var maximumFiles = 10;
    var maximumFileSize = 20 * 1024 * 1024;
    var allowedExtensions = ['pdf', 'doc', 'docx', 'jpg', 'jpeg', 'png', 'xls', 'xlsx'];
    var selectedFiles = new DataTransfer();

    function fileKey(file) {
        return [file.name, file.size, file.lastModified].join('|');
    }

    function safeText(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function renderFiles() {
        evidenceInput.files = selectedFiles.files;
        var total = selectedFiles.files.length;
        fileCount.textContent = total + ' of ' + maximumFiles + ' files added';
        fileFill.style.width = Math.min((total / maximumFiles) * 100, 100) + '%';
        fileFill.classList.toggle('full', total >= maximumFiles);
        dropzone.classList.toggle('full', total >= maximumFiles);
        fileList.innerHTML = '';

        Array.from(selectedFiles.files).forEach(function (file, index) {
            var item = document.createElement('div');
            item.className = 'obj-ev-file';
            item.innerHTML =
                '<i class="fa-solid fa-file"></i>' +
                '<span class="obj-ev-name">' + safeText(file.name) + '</span>' +
                '<span class="obj-ev-size">' + (file.size / 1024 / 1024).toFixed(2) + ' MB</span>' +
                '<button type="button" class="obj-ev-remove" data-remove-query-file="' + index + '" title="Remove file">' +
                '<i class="fa-solid fa-xmark"></i></button>';
            fileList.appendChild(item);
        });
    }

    function addFiles(files) {
        var existing = new Set(Array.from(selectedFiles.files).map(fileKey));
        var rejected = [];

        Array.from(files || []).forEach(function (file) {
            var extension = (file.name.split('.').pop() || '').toLowerCase();
            var key = fileKey(file);

            if (existing.has(key)) return;
            if (!allowedExtensions.includes(extension)) {
                rejected.push(file.name + ' is not a supported file type.');
                return;
            }
            if (file.size > maximumFileSize) {
                rejected.push(file.name + ' is larger than 20 MB.');
                return;
            }
            if (selectedFiles.files.length >= maximumFiles) {
                rejected.push('Only 10 supporting documents can be selected.');
                return;
            }

            selectedFiles.items.add(file);
            existing.add(key);
        });

        if (rejected.length) alert(Array.from(new Set(rejected)).join('\n'));
        renderFiles();
    }

    function openBrowser(event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        evidenceInput.click();
    }

    browseButton.addEventListener('click', openBrowser);
    dropzone.addEventListener('click', openBrowser);
    dropzone.addEventListener('keydown', function (event) {
        if (event.key === 'Enter' || event.key === ' ') openBrowser(event);
    });
    dropzone.addEventListener('dragover', function (event) {
        event.preventDefault();
        dropzone.classList.add('dragover');
    });
    dropzone.addEventListener('dragleave', function () {
        dropzone.classList.remove('dragover');
    });
    dropzone.addEventListener('drop', function (event) {
        event.preventDefault();
        dropzone.classList.remove('dragover');
        addFiles(event.dataTransfer && event.dataTransfer.files);
    });
    evidenceInput.addEventListener('change', function () {
        addFiles(evidenceInput.files);
    });
    fileList.addEventListener('click', function (event) {
        var button = event.target.closest('[data-remove-query-file]');
        if (!button) return;

        var removeIndex = Number(button.dataset.removeQueryFile);
        var replacement = new DataTransfer();
        Array.from(selectedFiles.files).forEach(function (file, index) {
            if (index !== removeIndex) replacement.items.add(file);
        });
        selectedFiles = replacement;
        renderFiles();
    });

    renderFiles();
})();

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
var Market_value = "";
var extent = "";
var desc = "";
var owner = "";
var objId = "";
var pin

function showInput() {

    desc = document.getElementById("Property_Desc").value;
    sessionStorage.setItem("desc", desc);

    desc = document.getElementById("Property_Type").value;
    sessionStorage.setItem("Property_Type", desc);

    Market_value = document.getElementById("Market_Value").value;
    sessionStorage.setItem("Market_Value", Market_value);

    extent = document.getElementById("extent").value;
    sessionStorage.setItem("extent", extent);

    Cat = document.getElementById("cat").value;
    sessionStorage.setItem("cat", Cat);

    owner = document.getElementById("cat").value;
    sessionStorage.setItem("cat", cat);

    owner = document.getElementById("owner").value;
    sessionStorage.setItem("owner", owner);


    if (document.getElementById("sign_obj").value !== null && document.getElementById("sign_obj").value.trim() !== "") {
        const submitButton = document.getElementById("submitForm");
        submitButton.disabled = true;
        submitButton.innerHTML = 'Please wait...';
        document.getElementById("myForm").submit();
    }

    //obj_Id = document.getElementById("pin").value;
    //sessionStorage.setItem("pin", pin);

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
        $(".div3_R").toggle(2000);
        $(".div3_B").toggle(2000);
        $(".div3_A").toggle(2000);
        $(".div4_R").toggle(2000);
        $(".div4_B").toggle(2000);
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
    if (AppealStatus == "True") {
        document.getElementById("obj_head1").innerHTML = "LODGING OF AN APPEAL AGAINST THE DECISION OF THE MINICIPAL VALUER REGARDING MATTERS PERTAINING TO PROPERTY AS REFLECTED IN OR OMITTED FROM THE " +
            "VALUATION ROLL / SUPPLEMENTARY VALUATION ROLL FOR THE PERIOD 1 JULY 2023 TO 30 JUNE 2027";
        document.getElementById("obj_head2").innerHTML = "DESCRIPTION  OF PROPERTY IN RESPECT OF WHICH THE APPEAL IS MADE";
        document.getElementById("obj_head3").innerHTML = "(COMPLETE A SEPARATE FORM FOR EARCH ENTRY APPEALLED TO)";
        document.getElementById("s_head").innerHTML = "SECTION 1: APPELLANT INFORMATION";
        document.getElementById("owner_head").innerHTML = "1.1 APPELLANT IS THE OWNER";
        document.getElementById("T_Party_head").innerHTML = "1.2 APPELLANT IS NOT THE OWNER OR MUNICIPALITY IS THE APPELLANT*";
        document.getElementById("TP_Name").innerHTML = "NAME OF APPELLANT";
        document.getElementById("tp_status").innerHTML = "STATUS OF APPELLANT";
        document.getElementById("r_head").innerHTML = "1.3 AUTHORISED REPRESENTATIVE OF THE APPELLANT";
        document.getElementById("section6_head").innerHTML = "SECTION 6: APPEAL DETAILS";
        document.getElementById("Section6_roll").innerHTML = "PARTICULARS AS REFLECTED IN NEW MVD";
        document.getElementById("Section6_reason").innerHTML = "Appeal";

    }

    if (SubType == "Review") {
        document.getElementById("obj_head1").innerHTML = "LODGING OF A REVIEW AGAINSTS MATTERS PERTAINING TO A GENERAL / SUPPLEMENTARY VALUATION ON THE PROPERTY DESCRIBED BELOW:";
        document.getElementById("obj_head2").innerHTML = "DESCRIPTION OF PROPERTY IN RESPECT OF WHICH THE REVIEW IS MADE";
        document.getElementById("Section6_reason").innerHTML = "REVIEW";
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

    if (objector_key == 'Owner' &&
        document.getElementById("o_id").disabled == true) {
        var id = document.getElementById('o_pass').value;
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
        return stat_Value;

    } else {

        id_stat = 'Invalid ID Number';

        if (objector_key == 'Owner' &&
            document.getElementById("o_id").disabled == false) {
            document.getElementById("id_status").innerHTML = id_stat;
            stat_Value = document.getElementById("id_status").innerHTML;
        }

        if (objector_key == 'Owner' &&
            document.getElementById("o_id").disabled == true) {
            id_stat = '';
            document.getElementById("id_status").innerHTML = id_stat;
            stat_Value = document.getElementById("id_status").innerHTML;
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

    $(".div781").hide();
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

    $(".btn_n1").click(function () {

        if (objector_key == "Owner") {

            if ((document.getElementById("o_cd_5").value) == '') {
                document.getElementById("o_cd_5").style.border = "2px solid red";
                fo_o = 4;
            } else {
                document.getElementById("o_cd_5").style.border = "";
                fo_o = 0;
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


            // if (LuhnAlgo() == 'Invalid ID Number') {
            //     document.getElementById("o_id").style.border = "2px solid red";
            //     document.getElementById("o_id").focus();
            //     alert("Invalid ID Number");

            // }
            // else {
            //     document.getElementById("o_id").style.border = "";
            //}
            if ((document.getElementById("o_cd_1").value) == '' &&
                (document.getElementById("o_cd_2").value) == '' &&
                (document.getElementById("o_cd_3").value) == '' &&
                (document.getElementById("o_cd_4").value) == ''
            ) {
                document.getElementById("o_cd_invalid").innerHTML = "Please fill at least one of the contact details fields.";
                document.getElementById("o_cd_invalid").style.color = "red";
                document.getElementById("o_cd_1").style.border = "2px solid red";
                document.getElementById("o_cd_2").style.border = "2px solid red";
                document.getElementById("o_cd_3").style.border = "2px solid red";
            } else {
                cd_o = 'true';
                document.getElementById("o_cd_invalid").innerHTML = "";
                document.getElementById("o_cd_1").style.border = "";
                document.getElementById("o_cd_2").style.border = "";
                document.getElementById("o_cd_3").style.border = "";
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
                (cd_o) == 'true'
                //&& LuhnAlgo() !== 'Invalid ID Number'
            ) {

                $(".div1").hide();
                $(".div781").show();
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
            if ((document.getElementById("o_cd_5").value) == '') {
                document.getElementById("o_cd_5").style.border = "2px solid red";
            } else {
                document.getElementById("o_cd_5").style.border = "";
            }
            if ((document.getElementById("rep_cd_5").value) == '') {
                document.getElementById("rep_cd_5").style.border = "2px solid red";
            } else {
                document.getElementById("rep_cd_5").style.border = "";
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

            if ((document.getElementById("rep_cd_1").value) == '' &&
                (document.getElementById("rep_cd_2").value) == '' &&
                (document.getElementById("rep_cd_3").value) == '' &&
                (document.getElementById("rep_cd_4").value) == ''
            ) {
                document.getElementById("rep_cd_invalid").innerHTML = "Please fill at least one of the contact details fields.";
                document.getElementById("rep_cd_invalid").style.color = "red";
                document.getElementById("rep_cd_1").style.border = "2px solid red";
                document.getElementById("rep_cd_2").style.border = "2px solid red";
                document.getElementById("rep_cd_3").style.border = "2px solid red";

            } else {
                cd_rep = 'true';
                document.getElementById("rep_cd_invalid").innerHTML = "";
                document.getElementById("rep_cd_1").style.border = "";
                document.getElementById("rep_cd_2").style.border = "";
                document.getElementById("rep_cd_3").style.border = "";
            }
            if ((document.getElementById("o_cd_1").value) == '' &&
                (document.getElementById("o_cd_2").value) == '' &&
                (document.getElementById("o_cd_3").value) == '' &&
                (document.getElementById("o_cd_4").value) == ''
            ) {
                document.getElementById("o_cd_invalid").innerHTML = "Please fill at least one of the contact details fields.";
                document.getElementById("o_cd_invalid").style.color = "red";
                document.getElementById("o_cd_1").style.border = "2px solid red";
                document.getElementById("o_cd_2").style.border = "2px solid red";
                document.getElementById("o_cd_3").style.border = "2px solid red";

            } else {
                cd_o = 'true';
                document.getElementById("o_cd_invalid").innerHTML = "";
                document.getElementById("o_cd_1").style.border = "";
                document.getElementById("o_cd_2").style.border = "";
                document.getElementById("o_cd_3").style.border = "";
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
                $(".div781").show();
                $("#form_back").hide();
                document.getElementById("phy_c").focus();
            }
        }



    });

    //div s78
    $(".btn_pS78").click(function () {

        $(".div1").show();
        $(".div781").hide();
        $("#form_back").show();
    });


    $(".btn_nS782").click(function () {

        $(".div2").show();
        $(".div781").hide();
        $("#form_back").show();
    });

    //div2
    $(".btn_p2").click(function () {

        $(".div781").show();
        $(".div2").hide();
        $("#form_back").show();
    });

    //$(".btn_n2").click(function () {
    //    if (property_key == "Res") {
    //        if ((document.getElementById("phy_c").value) == '') {
    //            document.getElementById("phy_c").style.border = "2px solid red";
    //        } else {
    //            document.getElementById("phy_c").style.border = "";
    //            $(".div2").hide();
    //            $(".div3_R").show();
    //            document.getElementById("s3r").focus();
    //        }

    //    }
    //    if (property_key == "Agric") {
    //        if ((document.getElementById("phy_c").value) == '') {
    //            document.getElementById("phy_c").style.border = "2px solid red";
    //        } else {
    //            document.getElementById("phy_c").style.border = "";
    //            $(".div2").hide();
    //            $(".div3_A").show();
    //            document.getElementById("s3a").focus();
    //        }

    //    }
    //    if (property_key == "Bus") {
    //        if ((document.getElementById("phy_c").value) == '') {
    //            document.getElementById("phy_c").style.border = "2px solid red";
    //        } else {
    //            document.getElementById("phy_c").style.border = "";
    //            $(".div2").hide();
    //            $(".div3_B").show();
    //            document.getElementById("s3b").focus();
    //        }


    //    }
    //    if (property_key == "Multi") {
    //        //Check postal code
    //        if ((document.getElementById("phy_c").value) == '') {
    //            document.getElementById("phy_c").style.border = "2px solid red";
    //        }
    //        else {
    //            document.getElementById("phy_c").style.border = "";
    //            $(".div2").hide();
    //            $(".div3_R").show();
    //            document.getElementById("s3r").focus();
    //        }
    //    }
    //    btn_R_p3
    //});
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

    $(".btn_B_p3").click(function () {
        $(".div3_R").show();
        $(".div3_B").hide();
    });

    $(".btn_A_p3").click(function () {
        $(".div3_B").show();
        $(".div3_A").hide();
    });

    $(".btn_R_n3").click(function () {
        $(".div3_R").hide();
        $(".div3_B").show();
        document.getElementById("s3b").focus();
    });

    $(".btn_B_n3").click(function () {
        $(".div3_B").hide();
        $(".div3_A").show();
        document.getElementById("s3a").focus();
    });

    $(".btn_A_n3").click(function () {
        $(".div3_A").hide();
        $(".div4_R").show();
        document.getElementById("sch_name").focus();
    });

    //$(".btn_p3").click(function () {
    //    if (property_key == "Res") {

    //        $(".div2").show();
    //        $(".div3_R").hide();
    //    }
    //    if (property_key == "Agric") {

    //        $(".div2").show();
    //        $(".div3_A").hide();
    //    }
    //    if (property_key == "Bus") {

    //        $(".div2").show();
    //        $(".div3_B").hide();
    //    }
    //    if (property_key == "Multi") {

    //        $(".btn_R_p3").click(function () {
    //            $(".div2").show();
    //            $(".div3_R").hide();
    //        });

    //        $(".btn_B_p3").click(function () {
    //            $(".div3_R").show();
    //            $(".div3_B").hide();
    //        });
    //        $(".btn_A_p3").click(function () {
    //            $(".div3_B").show();
    //            $(".div3_A").hide();
    //        });
    //    }
    //});

    //$(".btn_n3").click(function () {
    //    if (property_key == "Res") {

    //        $(".div3_R").hide();
    //        $(".div4_R").show();
    //        document.getElementById("sch_name").focus();
    //    }
    //    if (property_key == "Agric") {

    //        $(".div3_A").hide();
    //        $(".div5").show();
    //        document.getElementById("s5").focus();
    //    }
    //    if (property_key == "Bus") {

    //        $(".div3_B").hide();
    //        $(".div4_B").show();
    //        document.getElementById("sch_name_b").focus();
    //    }
    //    if (property_key == "Multi") {

    //        $(".btn_R_n3").click(function () {
    //            $(".div3_R").hide();
    //            $(".div3_B").show();
    //            document.getElementById("s3b").focus();
    //        });
    //        $(".btn_B_n3").click(function () {
    //            $(".div3_B").hide();
    //            $(".div3_A").show();
    //            document.getElementById("s3a").focus();
    //        });
    //        $(".btn_A_n3").click(function () {
    //            $(".div3_A").hide();
    //            $(".div4_R").show();
    //            document.getElementById("sch_name").focus();
    //        });
    //    }
    //});



    //div4

    //$(".btn_p4").click(function () {
    //    if (property_key == "Res") {

    //        $(".div3_R").show();
    //        $(".div4_R").hide();
    //    }
    //    if (property_key == "Bus") {

    //        $(".div3_B").show();
    //        $(".div4_B").hide();
    //    }
    //    if (property_key == "Multi") {

    //        $(".btn_R_p4").click(function () {
    //            $(".div3_A").show();
    //            $(".div4_R").hide();
    //        });
    //        $(".btn_B_p4").click(function () {
    //            $(".div4_R").show();
    //            $(".div4_B").hide();
    //        });
    //    }
    //});

    //$(".btn_n4").click(function () {
    //    if (property_key == "Res") {

    //        $(".div4_R").hide();
    //        $(".div5").show();
    //        document.getElementById("s5").focus();
    //    }
    //    if (property_key == "Bus") {
    //        $(".div4_B").hide();
    //        $(".div5").show();
    //        document.getElementById("s5").focus();
    //    }
    //    if (property_key == "Multi") {

    //        $(".btn_R_n4").click(function () {
    //            $(".div4_R").hide();
    //            $(".div4_B").show();
    //            document.getElementById("sch_name_b").focus();
    //        });
    //        $(".btn_B_n4").click(function () {
    //            $(".div4_B").hide();
    //            $(".div5").show();
    //            document.getElementById("s5").focus();
    //        });
    //    }
    //});

    $(".btn_R_p4").click(function () {
        $(".div3_A").show();
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

        if (property_key == "Res") {
            $(".div4_B").show();
            $(".div5").hide();
        }
        if (property_key == "Agric") {
            $(".div3_A").show();
            $(".div5").hide();
        }
        if (property_key == "Bus") {
            $(".div4_B").show();
            $(".div5").hide();
        }
        if (property_key == "Multi") {

            $(".btn_p5").click(function () {
                $(".div4_B").show();
                $(".div5").hide();
            });
        }

        

    });
    $(".btn_n5").click(function () {
        $(".div5").hide();
        $(".div6").show();
        document.getElementById("desc_in").focus();
    });
    //div6
    $(".btn_p6").click(function () {
        $(".div5").show();
        $(".div6").hide();
    });
    $(".btn_n6").click(function () {
        if (objector_key == "Owner") {
            if ((document.getElementById("NewCat").value) == '' &&
                (document.getElementById("NewMarketValue").value) == '' &&
                (document.getElementById("NewExtent").value) == '' &&
                (document.getElementById("NewPropDesc").value) == '' &&
                (document.getElementById("NewAddress").value) == '' &&
                (document.getElementById("NewOwner").value) == ''
            ) {
                document.getElementById("new_change_invalid").innerHTML = "Please fill at least one of the change you want to make.";
                document.getElementById("new_change_invalid").style.color = "red";
                document.getElementById("NewCat").style.border = "2px solid red";
                document.getElementById("NewMarketValue").style.border = "2px solid red";
                document.getElementById("NewExtent").style.border = "2px solid red";
                document.getElementById("NewPropDesc").style.border = "2px solid red";
                document.getElementById("NewAddress").style.border = "2px solid red";
                document.getElementById("NewOwner").style.border = "2px solid red";
            }
            else {
                NewChange = 'true';
                document.getElementById("new_change_invalid").innerHTML = "";
                document.getElementById("NewCat").style.border = "";
                document.getElementById("NewMarketValue").style.border = "";
                document.getElementById("NewExtent").style.border = "";
                document.getElementById("NewPropDesc").style.border = "";
                document.getElementById("NewAddress").style.border = "";
                document.getElementById("NewOwner").style.border = "";

                $(".div6").hide();
                $(".divU").show();
                document.getElementById("sectionUpload").focus();
            }
        }

        if (objector_key == "Third_Party") {
            if ((document.getElementById("NewCat").value) == '' &&
                (document.getElementById("NewMarketValue").value) == '' &&
                (document.getElementById("NewExtent").value) == '' &&
                (document.getElementById("NewPropDesc").value) == '' &&
                (document.getElementById("NewAddress").value) == '' &&
                (document.getElementById("NewOwner").value) == ''
            ) {
                document.getElementById("new_change_invalid").innerHTML = "Please fill at least one of the change you want to make.";
                document.getElementById("new_change_invalid").style.color = "red";
                document.getElementById("NewCat").style.border = "2px solid red";
                document.getElementById("NewMarketValue").style.border = "2px solid red";
                document.getElementById("NewExtent").style.border = "2px solid red";
                document.getElementById("NewPropDesc").style.border = "2px solid red";
                document.getElementById("NewAddress").style.border = "2px solid red";
                document.getElementById("NewOwner").style.border = "2px solid red";
            }
            else {
                NewChange = 'true';
                document.getElementById("new_change_invalid").innerHTML = "";
                document.getElementById("NewCat").style.border = "";
                document.getElementById("NewMarketValue").style.border = "";
                document.getElementById("NewExtent").style.border = "";
                document.getElementById("NewPropDesc").style.border = "";
                document.getElementById("NewAddress").style.border = "";
                document.getElementById("NewOwner").style.border = "";

                $(".div6").hide();
                $(".divU").show();
                document.getElementById("sectionUpload").focus();
            }
        }

        if (objector_key == "Representative") {
            if ((document.getElementById("NewCat").value) == '' &&
                (document.getElementById("NewMarketValue").value) == '' &&
                (document.getElementById("NewExtent").value) == '' &&
                (document.getElementById("NewPropDesc").value) == '' &&
                (document.getElementById("NewAddress").value) == '' &&
                (document.getElementById("NewOwner").value) == ''
            ) {
                document.getElementById("new_change_invalid").innerHTML = "Please fill at least one of the change you want to make.";
                document.getElementById("new_change_invalid").style.color = "red";
                document.getElementById("NewCat").style.border = "2px solid red";
                document.getElementById("NewMarketValue").style.border = "2px solid red";
                document.getElementById("NewExtent").style.border = "2px solid red";
                document.getElementById("NewPropDesc").style.border = "2px solid red";
                document.getElementById("NewAddress").style.border = "2px solid red";
                document.getElementById("NewOwner").style.border = "2px solid red";
            }
            else {
                NewChange = 'true';
                document.getElementById("new_change_invalid").innerHTML = "";
                document.getElementById("NewCat").style.border = "";
                document.getElementById("NewMarketValue").style.border = "";
                document.getElementById("NewExtent").style.border = "";
                document.getElementById("NewPropDesc").style.border = "";
                document.getElementById("NewAddress").style.border = "";
                document.getElementById("NewOwner").style.border = "";

                $(".div6").hide();
                $(".divU").show();
                document.getElementById("sectionUpload").focus();
            }
        }

        //$(".div6").hide();
        //$(".divU").show();
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
    if (!canvas) return;

    // Match the drawing buffer to the rendered size so ink stays directly
    // under the mouse/finger on desktop and responsive layouts.
    var ratio = Math.max(window.devicePixelRatio || 1, 1);
    var renderedWidth = canvas.getBoundingClientRect().width || 598;
    var renderedHeight = 160;
    canvas.width = Math.round(renderedWidth * ratio);
    canvas.height = Math.round(renderedHeight * ratio);
    canvas.getContext('2d').scale(ratio, ratio);

    var pad = new SignaturePad(canvas);
    var signatureData = document.getElementById('SignatureDataUrl');
    var status = document.getElementById('signatureStatus');
    var submit = document.getElementById('submitForm');

    function captureSignature() {
        window.requestAnimationFrame(function () {
            if (pad.isEmpty()) {
                if (signatureData) signatureData.value = '';
                if (status) {
                    status.textContent = 'No signature drawn';
                    status.style.color = '#6b6b6b';
                }
                if (submit) submit.disabled = true;
                return;
            }

            var data = pad.toDataURL('image/png');
            if (signatureData) {
                signatureData.value = data;
                signatureData.dispatchEvent(new Event('change', { bubbles: true }));
            }
            $('#savetarget').attr('src', data);
            if (status) {
                status.textContent = 'Signature captured';
                status.style.color = '#0f766e';
            }
            if (submit) submit.disabled = false;
        });
    }

    canvas.addEventListener('pointerup', captureSignature);
    canvas.addEventListener('mouseup', captureSignature);
    canvas.addEventListener('touchend', captureSignature);

    // Retained hidden compatibility control from the original Multi view.
    $('#accept').click(function () {
        captureSignature();
    });

    $('#Clear').click(function () {
        pad.clear();
        if (signatureData) signatureData.value = '';
        if (status) {
            status.textContent = 'No signature drawn';
            status.style.color = '#6b6b6b';
        }
        if (submit) submit.disabled = true;
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
async function generateObjectionPDF() {
    const form = document.getElementById('myForm');

    // Clone the form to avoid modifying the original UI
    const clone = form.cloneNode(true);
    clone.style.width = '210mm'; // A4 width
    clone.style.padding = '10mm';
    clone.style.background = 'white';
    clone.style.fontFamily = 'Arial, sans-serif';
    clone.style.fontSize = '10pt';

    // Add footer to each section for multi-page support
    const sections = clone.querySelectorAll('[id^="section"]');
    sections.forEach((section, idx) => {
        const footer = document.createElement('div');
        footer.style.fontSize = '8pt';
        footer.style.textAlign = 'left';
        footer.style.marginTop = '10mm';
        footer.style.pageBreakBefore = 'avoid'; // Avoid breaking mid-section
        footer.innerHTML = `Complete: Erf/Unit No .........    Area/Scheme Name .........................................................Form A Objection<br>Page ${idx + 1} of ${sections.length}`;
        section.appendChild(footer);
    });

    // Add logo from wwwroot
    const logoDiv = document.createElement('div');
    logoDiv.style.textAlign = 'center';
    logoDiv.style.marginBottom = '10mm';
    logoDiv.innerHTML = '<img src="/images/joburg-logo.png" alt="Joburg Logo" style="width:200px; height:auto;">'; // Adjust path if needed
    clone.insertBefore(logoDiv, clone.firstChild);

    // Replace form inputs/selects/textareas with static text (to match filled form)
    clone.querySelectorAll('input, select, textarea').forEach(el => {
        const span = document.createElement('span');
        span.textContent = el.value || '________________'; // Underline for empty fields
        span.style.borderBottom = '1px solid black';
        span.style.minWidth = '100px';
        span.style.display = 'inline-block';
        el.parentNode.replaceChild(span, el);
    });

    // Handle radios/checkboxes: show selected label
    clone.querySelectorAll('fieldset').forEach(fieldset => {
        const selected = fieldset.querySelector('input[type="radio"]:checked, input[type="checkbox"]:checked');
        if (selected) {
            const label = selected.nextElementSibling ? selected.nextElementSibling.textContent.trim() : '';
            const span = document.createElement('span');
            span.textContent = label;
            fieldset.innerHTML = '';
            fieldset.appendChild(span);
        } else {
            fieldset.remove();
        }
    });

    // Remove buttons, scripts, hidden elements
    clone.querySelectorAll('button, script, [hidden]').forEach(el => el.remove());

    // Temporarily append clone to body for rendering
    document.body.appendChild(clone);
    const canvas = await html2canvas(clone, { scale: 2, useCORS: true, logging: false });
    document.body.removeChild(clone);

    const { jsPDF } = window.jspdf;
    const pdf = new jsPDF('p', 'mm', 'a4');
    const imgData = canvas.toDataURL('image/png');
    const imgWidth = 210;
    const imgHeight = (canvas.height * imgWidth) / canvas.width;

    let heightLeft = imgHeight;
    let position = 0;

    pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
    heightLeft -= 297; // A4 height

    while (heightLeft > 0) {
        position = heightLeft - imgHeight;
        pdf.addPage();
        pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
        heightLeft -= 297;
    }

    pdf.save('Objection_Form_A.pdf');
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
