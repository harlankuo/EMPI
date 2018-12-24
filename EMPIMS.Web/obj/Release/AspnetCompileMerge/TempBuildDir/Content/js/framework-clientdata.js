var clients = [];
$(function () {
    clients = clientsInit();

})
function clientsInit() {
    var dataJson = {
        country: [],
        gender: [],
        abo_blood_type: [],
        nation: [],
        degress: [],
        address: [],
        marriage: [],
        occupation: [],
        idtype: [],
        rh_blood_type: [],
        relationship:[]
    };
    var init = function () {
        $.ajax({
            url: "/Home/GetClientsDataJson",
            type: "get",
            dataType: "json",
            async: false,
            success: function (data) {

                if (data.length > 0) {
                    var _data = $.parseJSON(data);
                    for (var i = 0; i < data.length; i++) {
                        switch (data[i].type) {
                            case "country":
                                dataJson.country = data[i].data;
                                break;
                            case "gender":
                                dataJson.gender = data[i].data;
                                break;
                            case "abo_blood_type":
                                dataJson.abo_blood_type = data[i].data;
                                break;
                            case "nation":
                                dataJson.nation = data[i].data;
                                break;
                            case "degree":
                                dataJson.degree = data[i].data;
                                break;
                            case "address":
                                dataJson.address = data[i].data;
                                break;
                            case "marriage":
                                dataJson.marriage = data[i].data;
                                break;
                            case "occupation":
                                dataJson.occupation = data[i].data;
                                break;
                            case "idtype":
                                dataJson.idtype = data[i].data;
                                break;                                
                            case "rh_blood_type":
                                dataJson.rh_blood_type = data[i].data;
                                break;
                            case "relationship":
                                dataJson.relationship = data[i].data;
                                break;
                            default:

                        }
                    }
                }
                //dataJson.dataItems = data.dataItems;
                //dataJson.organize = data.organize;
                //dataJson.role = data.role;
                //dataJson.duty = data.duty;
                //dataJson.authorizeMenu = eval(data.authorizeMenu);
                //dataJson.authorizeButton = data.authorizeButton;
            }
        });
    }
    init();
    return dataJson;
}