let nIntervId;
const duration = 60 * 3;

window.onload = function () {
    if (document.querySelector('#clockRefresh')) {
        startClock();
    }
};

function startClock() {
    let timer = duration, minutes, seconds;
    display = document.querySelector('#clockRefresh');
    if (!nIntervId) {
        nIntervId = setInterval(function () {
            minutes = parseInt(timer / 60, 10);
            seconds = parseInt(timer % 60, 10);

            minutes = minutes < 10 ? "0" + minutes : minutes;
            seconds = seconds < 10 ? "0" + seconds : seconds;

            display.textContent = minutes + " min" + " " + seconds + " sec";

            if (--timer < 0) {
                location.reload();
                //$.ajax({
                //    url: '/Radio/Check?handler=UpdateLatestActivityStatus',
                //    type: 'POST',
                //    dataType: 'json',
                //    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                //}).done(function () {
                //    clientSiteActivityStatus.ajax.reload();
                //    timer = duration;
                //});
            }
        }, 1000);
    }
}

$('#btnRefreshActivityStatus').on('click', function () {
    clearInterval(nIntervId);
    nIntervId = null;
    location.reload();
});

let clientSiteActivityStatus = $('#clientSiteActivityStatus').DataTable({
    paging: false,
    ordering: false,
    info: false,
    searching: false,
    fixedColumns: {
        left: 1
    },
    scrollCollapse: true,
    scrollX: true,
    autoWidth: false,
    ajax: {
        url: '/Radio/Check?handler=ClientSiteActivityStatus',
        datatype: 'json',
        data: function (d) {
            d.clientSiteIds = $('#rcClientSiteId').val().join(',');
        },
        dataSrc: ''
    },
    columns: [
        { data: 'activityStatus.clientSite.name' },
        { data: 'guardLicenseNo' },
        {
            data: 'activityStatus.status',
            width: '9%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value === true ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' : '<i class="fa fa-times-circle text-danger rc-client-status"></i>';
            }
        },        
        { data: 'recentActivity' },
        {
            targets: -1,
            data: null,
            width: '12%',
            defaultContent: '',
            render: function (value, type, data) {
                if (data.guardLicenseNo !== '-' && data.activityStatus.status === false) {
                    return '<button name="btnRadioCheckStatus" class="btn btn-outline-primary">Radio Check</button>';
                }
            }
        }
    ]
});


$('#rcClientType').on('change', function () {
    const clientType = $(this).val().join(';');
    const clientSiteControl = $('#rcClientSiteId');
    clientSiteControl.html('');
    $.ajax({
        url: '/Radio/Check?handler=ClientSites&type=' + encodeURIComponent(clientType),
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            data.map(function (site) {
                clientSiteControl.append('<option value="' + site.value + '">' + site.text + '</option>');
            });
            clientSiteControl.multiselect('rebuild');
        }
    });

});

$('#rcFilterClientSites').on('click', function () {
    clientSiteActivityStatus.ajax.reload();
});

$('#clientSiteActivityStatus').on('click', 'button[name="btnRadioCheckStatus"]', function () {
    var data = clientSiteActivityStatus.row($(this).parents('tr')).data();
    var rowClientSiteId = data.activityStatus.clientSiteId;
    var rowGuardId = data.activityStatus.guardId;
    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#selectRadioCheckStatus').modal('show');
});

$('#btnSaveRadioStatus').on('click', function () {
    const checkedStatus = $('#selectRadioStatus').val();
    var clientSiteId = $('#clientSiteId').val();
    var guardId = $('#guardId').val();
    if (checkedStatus === '') {
        return;
    }
    $.ajax({
        url: '/Radio/Check?handler=SaveRadioStatus',
        type: 'POST',
        data: {
            clientSiteId: clientSiteId,
            guardId: guardId,
            checkedStatus: checkedStatus,
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function () {
        $('#selectRadioCheckStatus').modal('hide');
        $('#selectRadioStatus').val('');
        $('#btnRefreshActivityStatus').trigger('click');
    });    
});

/* V2 Changes start 12102023 */
const groupColumn = 1;
const groupColumn2 = 2;
let clientSiteActiveGuards = $('#clientSiteActiveGuards').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 } ,// Hide the group column initially
        { "visible": false, "targets": 2 } 
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: false,
    fixedHeader: true,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteActivityStatus',
        datatype: 'json',
        data: function (d) {
            d.clientSiteIds = 'test,';
        },
        dataSrc: ''
    },
    columns: [
        { data: 'clientSiteId', visible: false },
        {
            data: 'siteName',
            width: '20%',
            class:'dt-control',
            render: function (value, type, data) {
                
                return '<tr class="group group-start"><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
            }
        }, 
        {
            data: 'address',
            width: '20%',
            visible: false,
            render: function (value, type, data) {

                return '<tr class="group group-start sho"><td class="' + (groupColumn2 == '2' ? 'bg-danger' : (groupColumn2 == '0' ? 'bg-danger' : 'bg-danger')) + ' " colspan="5">' + groupColumn2 + '</td></tr>';
            }
        },
        {
            data: 'guardName',
            width: '20%',
            render: function (value, type, data) {
                return '&nbsp;&nbsp;&nbsp;<i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                    '<a href="#" class="ml-2"><i class="fa fa-vcard-o text-info" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i></a>';
            }
        },
        {
            data: 'logBook',
            width: '9%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#"  id="btnLogBookDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" text="' + data.clientSiteId + '"><input type="hidden" id="GuardId" text="' + data.guardId + '"> ';
            }
        }, 
        {
            data: 'keyVehicle',
            width: '9%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#" id="btnKeyVehicleDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
            }
        },   
        {
            data: 'incidentReport',
            width: '9%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#" id="btnIncidentReportdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '"> ' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
            }
        },   
       
       
    ],
    drawCallback: function () {
        var api = this.api();
        var rows = api.rows({ page: 'current' }).nodes();
        var last = null;
        var last2 = null;
        api.column(groupColumn, { page: 'current' })
            .data()
            .each(function (group, i) {
                if (last !== group) {
                    $(rows)
                        .eq(i)
                        .before('<tr class="group bg-info text-white dt-control"><td colspan="25">' + group + '</td></tr>');

                    last = group;
                }
            });
        api.column(groupColumn2, { page: 'current' })
            .data()
            .each(function (group, i) {
                if (last2 !== group) {
                    $(rows)
                        .eq(i)
                        .before('<tr class="group bg-info text-white hide" id="group2" hidden><td colspan="25">' + group + '</td></tr>');

                    last2 = group;
                }
            });
    },
});

$('#clientSiteActiveGuards tbody').on('click', 'tr.dt-control', function () {
    

    if ($(this).closest('tr').next('tr').is(':hidden') == true) {
        $(this).closest('tr').next('tr').prop('hidden', false);
        $(this).closest('tr').find('#btnUpArrow').removeClass('fa-caret-down')
        $(this).closest('tr').find('#btnUpArrow').addClass('fa-caret-up')
        /* $(this).closest('tr').next('tr').toggle();*/
    }
    else {
        $(this).closest('tr').next('tr').prop('hidden', 'hidden');
        $(this).closest('tr').find('#btnUpArrow').removeClass('fa-caret-up')
        $(this).closest('tr').find('#btnUpArrow').addClass('fa-caret-down')

    }
    
    
    //}
});
$('#clientSiteActiveGuards tbody').on('click', '#btnActiveGuardsMap', function (value, record) {
   
    var Gps = $(this).closest("tr").find("#txtGPSActiveguards").val();
    alert(Gps);
   

});
function format_kvl_child_row(d) {
    return (
        '<table cellpadding="7" cellspacing="0"  border="0" style="padding-left:50px;">' +
        
        '<tr>' +
        '<td>' + d.address + '</td>' +
       
        '</tr>' +
        '<tr>' +
       
        '</table>'
    );
}


let clientSiteInActiveGuards = $('#clientSiteInActiveGuards').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 } // Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: false,
    fixedHeader: true,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteInActivityStatus',
        datatype: 'json',
        data: function (d) {
            d.clientSiteIds = 'test,';
        },
        dataSrc: ''
    },
    columns: [
        { data: 'clientSiteId', visible: false },
        {
            data: 'siteName',
            width: '20%',
            render: function (value, type, data) {

                return '<tr class="group group-start"><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
            }
        },
        {
            data: 'guardName',
            width: '20%',
            render: function (value, type, data) {
                return '&nbsp;&nbsp;&nbsp;<i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                    '<a href="#" class="ml-2"><i class="fa fa-vcard-o text-info" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i></a>';
            }
        },

        {
            data: 'guardLoginTime',
            width: '9%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return  '<i class="fa fa-clock-o text-success rc-client-status"></i> ' +  value  ;
            }
        },

    ],
    drawCallback: function () {
        var api = this.api();
        var rows = api.rows({ page: 'current' }).nodes();
        var last = null;

        api.column(groupColumn, { page: 'current' })
            .data()
            .each(function (group, i) {
                if (last !== group) {
                    $(rows)
                        .eq(i)
                        .before('<tr class="group bg-info text-white"><td colspan="25">' + group + '</td></tr>');

                    last = group;
                }
            });
    },
});


$('#guardInfoModal').on('shown.bs.modal', function (event) {
    $('#lbl_guard_name').html('');
    $('#lbl_guard_security_no').html('');
    $('#lbl_guard_state').html('');
    $('#lbl_guard_provider').html('');

    const button = $(event.relatedTarget);
    const id = button.data('id');

    $.ajax({
        url: '/RadioCheckV2?handler=GuardData',
        data: { id: id },
        type: 'GET',
    }).done(function (result) {
        if (result) {
            $('#lbl_guard_name').html(result.name);
            $('#lbl_guard_security_no').html(result.securityNo);
            $('#lbl_guard_state').html(result.state);
            $('#lbl_guard_email').html(result.email);
            $('#lbl_guard_mobile').html(result.mobile);
            $('#lbl_guard_provider').html(result.provider);
        }
    });
});
const renderGuardInitialColumn = function (value, record, $cell, $displayEl) {
    if (record.guardId !== null) {
        return value + '<a href="#" class="ml-2"><i class="fa fa-vcard-o text-info" data-toggle="modal" data-target="#guardInfoModal" data-id="' + record.guardId + '"></i></a>';
    }
    else return value;
}


/*to get the guards that are not available-start*/
$('#btnNonActiveList').on('click', function () {
    window.open("../NonActiveGuards")
});
let clientSiteNotAvailableGuards = $('#clientSiteNotAvailableGuards').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 } // Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: false,
    fixedHeader: true,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteNotAvailableStatus',
        datatype: 'json',
        data: function (d) {
            d.clientSiteIds = 'test,';
        },
        dataSrc: ''
    },
    columns: [
        { data: 'clientSiteId', visible: false },
        {
            data: 'siteName',
            width: '20%',
            render: function (value, type, data) {

                return '<tr class="group group-start"><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
            }
        },
        {
            data: 'guardName',
            width: '20%',
            render: function (value, type, data) {
                return '&nbsp;&nbsp;&nbsp;<i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                    '<a href="#" class="ml-2"><i class="fa fa-vcard-o text-info" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i></a>';
            }
        },

        {
            data: 'guardLastLoginDate',
            width: '9%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return '<i class="fa fa-clock-o text-success rc-client-status"></i> ' + value;
            }
        },

    ],
    drawCallback: function () {
        var api = this.api();
        var rows = api.rows({ page: 'current' }).nodes();
        var last = null;

        api.column(groupColumn, { page: 'current' })
            .data()
            .each(function (group, i) {
                if (last !== group) {
                    $(rows)
                        .eq(i)
                        .before('<tr class="group bg-info text-white"><td colspan="25">' + group + '</td></tr>');

                    last = group;
                }
            });
    },
});

/*to get the guards that are not available-start*/

/* for logbook details of the guard-start*/

let clientSiteActiveGuardsLogBookDetails = $('#clientSiteActiveGuardsLogBookDetails').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 } // Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: false,
    fixedHeader: true,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/RadioCheckV2?handler=ClientSitelogBookActivityStatus',
        datatype: 'json',
        data: function (d) {
            d.clientSiteId = $('#txtClientSiteId').val();
            d.guardId = $('#txtGuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', visible: false },
        {
            data: 'siteName',
            width: '20%',
            render: function (value, type, data) {

                return '<tr class="group group-start"><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
            }
        },
        {
            data: 'logBookId',
            width: '20%',
            visible:false,

        },
        {
            data: 'notes',
            width: '9%',
            className: "text-center",

        },

        {
            data: 'activity',
            width: '9%',
            className: "text-center",
           
        },
        {
            data: 'logBookCreatedTime',
            width: '9%',
            className: "text-center",

        },


    ],
    drawCallback: function () {
        var api = this.api();
        var rows = api.rows({ page: 'current' }).nodes();
        var last = null;

        api.column(groupColumn, { page: 'current' })
            .data()
            .each(function (group, i) {
                if (last !== group) {
                    $(rows)
                        .eq(i)
                        .before('<tr class="group bg-info text-white"><td colspan="25">' + group + '</td></tr>');

                    last = group;
                }
            });
    },
});

$('#clientSiteActiveGuards tbody').on('click', '#btnLogBookDetailsByGuard', function (value, record) {
    $('#guardLogBookInfoModal').modal('show');
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader').text(GuardName + '-' + 'Log Book Details');
    clientSiteActiveGuardsLogBookDetails.ajax.reload();
    
});
/*for logbook details of the guard - end*/

/* for key vehicle details of the guard-start*/

let clientSiteActiveGuardsKeyVehicleDetails = $('#clientSiteActiveGuardsKeyVehicleDetails').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 } // Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: false,
    fixedHeader: true,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteKeyVehicleLogActivityStatus',
        datatype: 'json',
        data: function (d) {
            d.clientSiteId = $('#txtClientSiteId').val();
            d.guardId = $('#txtGuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', visible: false },
        {
            data: 'siteName',
            width: '20%',
            render: function (value, type, data) {

                return '<tr class="group group-start"><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
            }
        },
        {
            data: 'keyVehicleId',
            width: '20%',
            visible: false

        },
        {
            data: 'truckNo',
            width: '20%'

        },
        {
            data: 'individual',
            width: '20%'

        },
        {
            data: 'company',
            width: '20%'

        },

        {
            data: 'activity',
            width: '9%',
            className: "text-center",
            
        },
        {
            data: 'keyVehicleLogCreatedTime',
            width: '9%',
            className: "text-center",

        },


    ],
    drawCallback: function () {
        var api = this.api();
        var rows = api.rows({ page: 'current' }).nodes();
        var last = null;

        api.column(groupColumn, { page: 'current' })
            .data()
            .each(function (group, i) {
                if (last !== group) {
                    $(rows)
                        .eq(i)
                        .before('<tr class="group bg-info text-white"><td colspan="25">' + group + '</td></tr>');

                    last = group;
                }
            });
    },
});

$('#clientSiteActiveGuards tbody').on('click', '#btnKeyVehicleDetailsByGuard', function (value, record) {
    $('#guardKeyVehicleInfoModal').modal('show');
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader1').text(GuardName + '-' + 'Key Vehicle Log Details');
    clientSiteActiveGuardsKeyVehicleDetails.ajax.reload();

});
/*for key vehicle details of the guard - end*/

/* for incident report details of the guard-start*/

let clientSiteActiveGuardsIncidentReportsDetails = $('#clientSiteActiveGuardsIncidentReportsDetails').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 } // Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: false,
    fixedHeader: true,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteIncidentReportActivityStatus',
        datatype: 'json',
        data: function (d) {
            d.clientSiteId = $('#txtClientSiteId').val();
            d.guardId = $('#txtGuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', visible: false },
        {
            data: 'siteName',
            width: '20%',
            render: function (value, type, data) {

                return '<tr class="group group-start"><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
            }
        },
        {
            data: 'incidentReportId',
            width: '10%',
            visible: false

        },
        {
            data: 'fileName',
            width: '20%'

        },

        {
            data: 'activity',
            width: '9%',
            className: "text-center",
           
        },
        {
            data: 'incidentReportCreatedTime',
            width: '9%',
            className: "text-center",

        },


    ],
    drawCallback: function () {
        var api = this.api();
        var rows = api.rows({ page: 'current' }).nodes();
        var last = null;

        api.column(groupColumn, { page: 'current' })
            .data()
            .each(function (group, i) {
                if (last !== group) {
                    $(rows)
                        .eq(i)
                        .before('<tr class="group bg-info text-white"><td colspan="25">' + group + '</td></tr>');

                    last = group;
                }
            });
    },
});

$('#clientSiteActiveGuards tbody').on('click', '#btnIncidentReportdetails', function (value, record) {
    $('#guardIncidentReportsInfoModal').modal('show');
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader2').text(GuardName + '-' + 'Incident Report Details');
    clientSiteActiveGuardsIncidentReportsDetails.ajax.reload();

});
/*for incident report details of the guard - end*/
