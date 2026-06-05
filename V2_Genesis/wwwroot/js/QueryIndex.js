var property_type = "";
var objector_type = "";
var query_type = "";
var x, y, z, x1, x2, x3, w1,q1,q2;
var direction = "Normal";

$("#form_key").hide();




//Residential Function
$("#btncheck1").click(function () {
    document.getElementById("obj_type").value = "Res";
    property_type = document.getElementById("obj_type").value;
    sessionStorage.setItem('property_choice', property_type);
    document.getElementById("p_type").innerHTML = "Residential (Full Title and Sectional Title used for Residential Purposes)";
    x = document.getElementById("btn_check1").checked;
    z = document.getElementById("btn_check3").checked;
    w1 = document.getElementById("btncheck4").checked;
    q1 = document.getElementById("btn_checkq1").checked;
    q2 = document.getElementById("btn_checkq3").checked;
    if (w1 == true) {
        direction = "Multi";
        sessionStorage.setItem('direction', direction);
    } else {
        direction = "Normal";
        sessionStorage.setItem('direction', direction);
    }
    if (x == true || y == true || z == true || q1 == true || q2 == true) {
        if (w1 == true) { 
            $("#form_key").show();
            //callback();
        }
        $("#form_key").show();
        //callback();
    } else {
        $("#form_key").hide();
    }
});
//Agricultural Function
$("#btncheck2").click(function () {

    document.getElementById("obj_type").value = "Agric";
    property_type = document.getElementById("obj_type").value;
    sessionStorage.setItem('property_choice', property_type);
    document.getElementById("p_type").innerHTML = "Agricultural Holdings or Farms";
    x = document.getElementById("btn_check1").checked;
    z = document.getElementById("btn_check3").checked;
    w1 = document.getElementById("btncheck4").checked;
    q1 = document.getElementById("btn_checkq1").checked;
    q2 = document.getElementById("btn_checkq3").checked;

    if (w1 == true) {
        direction = "Multi";
        sessionStorage.setItem('direction', direction);
    } else {
        direction = "Normal";
        sessionStorage.setItem('direction', direction);
    }

    if (x == true || y == true || z == true || q1 == true || q2 == true) {
        $("#form_key").show();
        //callback();
    } else {
        $("#form_key").hide();
    }
});
//Business Function 
$("#btncheck3").click(function () {

    document.getElementById("obj_type").value = "Bus";
    property_type = document.getElementById("obj_type").value;
    sessionStorage.setItem('property_choice', property_type);
    document.getElementById("p_type").innerHTML = "Properties other than Residential or Agricultural (e.g. Business, Factories, Offices, Schools)";
    x = document.getElementById("btn_check1").checked;
    z = document.getElementById("btn_check3").checked;
    w1 = document.getElementById("btncheck4").checked;
    q1 = document.getElementById("btn_checkq1").checked;
    q2 = document.getElementById("btn_checkq3").checked;

    if (w1 == true) {
        direction = "Multi";
        sessionStorage.setItem('direction', direction);
    } else {
        direction = "Normal";
        sessionStorage.setItem('direction', direction);
    }

    if (x == true || y == true || z == true || q1 == true || q2 == true) {

        $("#form_key").show();
        //callback();
    } else {
        $("#form_key").hide();
    }
});
//Multiple Purpose Function 
$("#btncheck4").click(function () {

    document.getElementById("obj_type").value = "Multi";
    property_type = document.getElementById("obj_type").value;
    /*sessionStorage.setItem('property_choice', property_type);*/
    sessionStorage.setItem('direction', 'Multi');
    document.getElementById("p_type").innerHTML = "Multiple Purpose (The use of a property for more than one purpose)";
    x = document.getElementById("btn_check1").checked;
    z = document.getElementById("btn_check3").checked;
    w1 = document.getElementById("btncheck4").checked;
    q1 = document.getElementById("btn_checkq1").checked;
    q2 = document.getElementById("btn_checkq3").checked;

    if (w1 == true) {
        direction = "Multi";
        sessionStorage.setItem('direction', direction);
    } else {
        direction = "Normal";
        sessionStorage.setItem('direction', direction);
    }

    if (x == true || y == true || z == true || q1 == true || q2 == true) {

        $("#form_key").show();
        //callback();
    } else {
        $("#form_key").hide();
    }

});

$(function () {
    const dir = sessionStorage.getItem('direction');
    if (dir === "Multi") {
        // force the hidden input that the server reads
        $("#Direct").val("Multi");               // <-- add a hidden input in the view
    }
});

function updateButtonVisibility() {
    const isMulti = sessionStorage.getItem('direction') === "Multi";

    $("#btn_choice, #btn_choice1, #btn_choiceM, #btn_choiceM1")
        .attr("hidden", true);

    if (isMulti) {
        if (submission_type === "Query") $("#btn_choiceM").removeAttr("hidden");
        else $("#btn_choiceM1").removeAttr("hidden");
    } else {
        if (submission_type === "Query") $("#btn_choice").removeAttr("hidden");
        else $("#btn_choice1").removeAttr("hidden");
    }

    // **force the Direct parameter on every visible button**
    const directVal = isMulti ? "Multi" : "";
    $("#form_key a:visible").each(function () {
        const href = new URL(this.href);
        href.searchParams.set("Direct", directVal);
        this.href = href.toString();
    });
}
//Owner Function
$("#btn_check1").click(function () {
    document.getElementById("objector_type").value = "Owner";
    objector_type = document.getElementById("objector_type").value;
    sessionStorage.setItem('objector_choice', objector_type);
    //document.getElementById("o_type").innerHTML = "by Owner";
    document.getElementById("o_type").innerHTML = "Objector is the Owner";
    x1 = document.getElementById("btncheck1").checked;
    y1 = document.getElementById("btncheck2").checked;
    z1 = document.getElementById("btncheck3").checked;
    w1 = document.getElementById("btncheck4").checked;
    q1 = document.getElementById("btn_checkq1").checked;
    q2 = document.getElementById("btn_checkq3").checked;

    if (w1 == true) {
        direction = "Multi";
        sessionStorage.setItem('direction', direction);
    } else {
        direction = "Normal";
        sessionStorage.setItem('direction', direction);
    }

    if (x1 == true || y1 == true || z1 == true || w1 == true || q1 == true || q2 == true) {

        $("#form_key").show();
        //callback();
    } else {
        $("#form_key").hide();
    }
});

//Representative Function 
$("#btn_check3").click(function () {

    document.getElementById("objector_type").value = "Representative";
    objector_type = document.getElementById("objector_type").value;
    sessionStorage.setItem('objector_choice', objector_type);
    //document.getElementById("o_type").innerHTML = "by Representative";
    document.getElementById("o_type").innerHTML = "Authorised Representative of the Objector";
    x1 = document.getElementById("btncheck1").checked;
    y1 = document.getElementById("btncheck2").checked;
    z1 = document.getElementById("btncheck3").checked;
    w1 = document.getElementById("btncheck4").checked;
    q1 = document.getElementById("btn_checkq1").checked;
    q2 = document.getElementById("btn_checkq3").checked;

    if (w1 == true) {
        direction = "Multi";
        sessionStorage.setItem('direction', direction);
    } else {
        direction = "Normal";
        sessionStorage.setItem('direction', direction);
    }

    if (x1 == true || y1 == true || z1 == true || w1 == true || q1 == true || q2 == true) {

        $("#form_key").show();
        //callback();
    } else {
        $("#form_key").hide();
    }
});


//Query
$("#btn_checkq1").click(function () {
    document.getElementById("sub_type").value = "Query";
    query_type = document.getElementById("sub_type").value;
    sessionStorage.setItem('query_choice', query_type);
    //document.getElementById("o_type").innerHTML = "by Owner";
    document.getElementById("q_type").innerHTML = "Lodging A Query";
    x1 = document.getElementById("btncheck1").checked;
    y1 = document.getElementById("btncheck2").checked;
    z1 = document.getElementById("btncheck3").checked;
    w1 = document.getElementById("btncheck4").checked;
    q1 = document.getElementById("btn_checkq1").checked;

    if (w1 == true) {
        direction = "Multi";
        sessionStorage.setItem('direction', direction);
    } else {
        direction = "Normal";
        sessionStorage.setItem('direction', direction);
    }

    if (x1 == true || y1 == true || z1 == true || w1 == true || q1 == true) {

        $("#form_key").show();
        document.getElementById("btn_choice1").hidden = true;
        document.getElementById("btn_choice").hidden = false;
        //callback();
    } else {
        $("#form_key").hide();
        document.getElementById("btn_choice").hidden = true;
        document.getElementById("btn_choice1").hidden = false;
    }
});

//Review
$("#btn_checkq3").click(function () {

    document.getElementById("sub_type").value = "Review";
    query_type = document.getElementById("sub_type").value;
    sessionStorage.setItem('query_choice', query_type);
    //document.getElementById("o_type").innerHTML = "by Representative";
    document.getElementById("q_type").innerHTML = "Lodging A Review";
    x1 = document.getElementById("btncheck1").checked;
    y1 = document.getElementById("btncheck2").checked;
    z1 = document.getElementById("btncheck3").checked;
    w1 = document.getElementById("btncheck4").checked;
    q2 = document.getElementById("btn_checkq3").checked;

    if (w1 == true) {
        direction = "Multi";
        sessionStorage.setItem('direction', direction);
    } else {
        direction = "Normal";
        sessionStorage.setItem('direction', direction);
    }

    if (x1 == true || y1 == true || z1 == true || w1 == true || q2 == true) {

        $("#form_key").show();
        document.getElementById("btn_choice").hidden = true;
        document.getElementById("btn_choice1").hidden = false;
        //callback();
    } else {
        $("#form_key").hide();
        document.getElementById("btn_choice1").hidden = true;
        document.getElementById("btn_choice").hidden = false;
        document.getElementById("btn_choice").hidden = true;
        document.getElementById("btn_choice1").hidden = false;
    }
});


//document.getElementById("btn_choice").disabled = true;
document.getElementById("disclaimer").style.display = "";

function disclaimer() {
    document.getElementById("disclaimer").style.display = "none";
    document.getElementById("main_index").style.display = "";
    document.getElementById("main_index2").style.display = "";

}
function agree() {

    $("#Agree").hide();
    $("#btn_close").removeAttr("hidden");

}

var drive = ""
function callback() {

    if (sessionStorage.getItem('direction') == "Multi") {

        
            const submitButton = document.getElementById("btn_choice1");
            //submitButton.removeAttribute("disabled");
            submitButton.removeAttribute("hidden");
            document.getElementById("btn_choice").hidden = true;

    }
    if (sessionStorage.getItem('direction') == "Normal") {
        
            const submitButton = document.getElementById("btn_choice");
            //submitButton.removeAttribute("disabled");
            submitButton.removeAttribute("hidden");
            document.getElementById("btn_choice1").hidden = true;
        
    }

}
