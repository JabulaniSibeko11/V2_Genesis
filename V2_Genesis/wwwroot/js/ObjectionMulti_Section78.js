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
    loader.style.display = "none";
});

var userEmailElement = document.getElementById('userEmail');

if (userEmailElement) {
    var userEmail = userEmailElement.value;

    var regex = /^(val\.admin(1[0-9]?|[1-9])@joburg\.org\.za)$/i;

    if (regex.test(userEmail) || userEmail === 'AdministrationEnquiries@Joburg.org.za') {
        document.getElementById('capturer').style.display = 'block';
        document.getElementById('sapNo').setAttribute('required', 'required');
    } else {
        document.getElementById('capturer').style.display = 'none';
        document.getElementById('sapNo').removeAttribute('required');
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
    sessionStorage.setItem("Property_Type", property_type);

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
    // format number 1000000 to 1,234,567
    return n.replace(/\D/g, "").replace(/\B(?=(\d{3})+(?!\d))/g, ",")
}


function formatCurrency(input, blur) {
    // appends $ to value, validates decimal side
    // and puts cursor back in right position.

    // get input value
    var input_val = input.val();

    // don't validate empty input
    if (input_val === "") { return; }

    // original length
    var original_len = input_val.length;

    // initial caret position
    var caret_pos = input.prop("selectionStart");

    // check for decimal
    if (input_val.indexOf(".") >= 0) {

        // get position of first decimal
        // this prevents multiple decimals from
        // being entered
        var decimal_pos = input_val.indexOf(".");

        // split number by decimal point
        var left_side = input_val.substring(0, decimal_pos);
        var right_side = input_val.substring(decimal_pos);

        // add commas to left side of number
        left_side = formatNumber(left_side);

        // validate right side
        right_side = formatNumber(right_side);

        // On blur make sure 2 numbers after decimal
        if (blur === "blur") {
            right_side += "00";
        }

        // Limit decimal to only 2 digits
        right_side = right_side.substring(0, 2);

        // join number by .
        input_val = "R " + left_side + "." + right_side;

    } else {
        // no decimal entered
        // add commas to number
        // remove all non-digits
        input_val = formatNumber(input_val);
        input_val = "R " + input_val;

        // final formatting
        if (blur === "blur") {
            input_val += ".00";
        }
    }

    // send updated string to input
    input.val(input_val);

    // put caret back in the right position
    var updated_len = input_val.length;
    caret_pos = updated_len - original_len + caret_pos;
    input[0].setSelectionRange(caret_pos, caret_pos);
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