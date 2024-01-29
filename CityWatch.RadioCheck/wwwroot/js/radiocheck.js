let nIntervId;
const duration = 60 * 3;
var isPaused = false;
//p4#48 AudioNotification - Binoy - 12-01-2024 -- Start
let playAlarm = 0; 
let audiourl = '/NotificationSound/duressAlarm01.mp3'
const audio = new Audio(audiourl);
audio.loop = true;
//p4#48 AudioNotification - Binoy - 12-01-2024 -- End

// Task p6#73_TimeZone issue -- added by Binoy - Start
var tmzdata = {
    'EventDateTimeLocal': null,
    'EventDateTimeLocalWithOffset': null,
    'EventDateTimeZone': null,
    'EventDateTimeZoneShort': null,
    'EventDateTimeUtcOffsetMinute': null,
};
// Task p6#73_TimeZone issue -- added by Binoy - End
window.onload = function () {
    if (document.querySelector('#clockRefresh')) {
        startClock();
    }
};
$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();
});
function startClock() {
    let timer = duration, minutes, seconds;
    display = document.querySelector('#clockRefresh');
    if (!nIntervId) {
        nIntervId = setInterval(function () {

            if (!isPaused) {
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
    isPaused = true;
});



/* V2 Changes start 12102023 */

$(window).resize(function () {
    console.log($(window).height());
    //$('.dataTables_scrollBody').css('height', ($(window).height() - 200));
    /* for modifying the size of tables active and inactive guards-start*/
    var count = $('#clientSiteInActiveGuards tbody tr').length;
    if (count > 10) {
        $('#clientSiteInActiveGuards').closest('.dataTables_scrollBody').css('height', ($(window).height() - 300));

    }
    else {


        $('#clientSiteInActiveGuards').closest('.dataTables_scrollBody').css('height', '100%');

    }
    var count2 = $('#clientSiteActiveGuards tbody tr').length;
    if (count2 > 10) {
        $('#clientSiteActiveGuards').closest('.dataTables_scrollBody').css('height', ($(window).height() - 200));

    }
    else {


        $('#clientSiteActiveGuards').closest('.dataTables_scrollBody').css('height', '100%');

    }
   /* for modifying the size of tables active and inactive guards - end*/

});


const groupColumn = 1;
const groupColumn2 = 2;
var scrollPosition2;
var rowIndex2;
var scrollY = ($(window).height() - 300);
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
    autoWidth: true,
    fixedHeader: true,
    "scrollY": scrollY,
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
        { data: 'clientSiteId', visible: false  },
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
                if (data.isEnabled === 1) {
                    return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                        '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>'+
                        '&nbsp;&nbsp;&nbsp; <i class="fa fa-map-marker" aria-hidden="true"></i>';

                }
                else {
                    return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                        '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>';
                }
            }
        },
        {
            data: 'logBook',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardLogBookInfoModal" id="btnLogBookDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" text="' + data.clientSiteId + '"><input type="hidden" id="GuardId" text="' + data.guardId + '"> ';
            }
        },
        {
            data: 'keyVehicle',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardKeyVehicleInfoModal" id="btnKeyVehicleDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
            }
        },
        {
            data: 'incidentReport',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardIncidentReportsInfoModal" id="btnIncidentReportdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '"> ' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
            }

        },
        {
            data: 'smartWands',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardSWInfoModal" id="btnSWdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '"> ' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
            }
        },
        //{
        //    data: 'rcStatus',
        //    width: '5%',
        //    className: "text-center",

        //},
        {
            data: 'rcColorId',
            width: '5%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return '';
                else if (value === 4)
                    return '<i class="fa fa-check-circle text-success"></i>' + ' [' + '<a href="#hoverModal" id="btnGreen1hover" >' + 1 + '</a>' + '] <input type="hidden" id="RCStatusId" value="' + data.rcSatus + '"><input type="hidden" id="RCColortype" value="' + data.rcColor + '"><input type="hidden" id="RCStatus" value="' + data.status + '"><input type="hidden" id="ClientSiteIdHover" value="' + data.clientSiteId + '"><input type="hidden" id="GuardIdHover" value="' + data.guardId + '"><input type="hidden" id="ColorIdHover" value="' + data.rcColorId + '">';
                else if (value === 1)
                     return data.status;
                    //return '<i class="fa fa-check-circle text-danger"></i>' + ' [' + '<a href="#hoverModal" id="btnGreen1hover">' + 1 + '</a>' + '] <input type="hidden" id="RCStatusId" value="' + data.rcSatus + '"><input type="hidden" id="RCColortype" value="' + data.rcColor + '"><input type="hidden" id="RCStatus" value="' + data.status + '">';
                else if (value === 2)
                    return data.status;
                  //  return '<i class="fa fa-check-circle text-danger "></i>' + ' [' + '<a href="#hoverModal" id="btnGreen1hover">' + 2 + '</a>' + '] <input type="hidden" id="RCStatusId" value="' + data.rcSatus + '"><input type="hidden" id="RCColortype" value="' + data.rcColor + '"><input type="hidden" id="RCStatus" value="' + data.status + '">';
                else  
                    return data.status;
                    //return '<i class="fa fa-check-circle text-danger "></i>' + ' [' + '<a href="#hoverModal" id="btnGreen1hover">' + 3 + '</a>' + '] <input type="hidden" id="RCStatusId" value="' + data.rcSatus + '"><input type="hidden" id="RCColortype" value="' + data.rcColor + '"><input type="hidden" id="RCStatus" value="' + data.status + '">';
            }
        },
        {
            targets: -1,
            data: null,
            width: '5%',
            defaultContent: '',
            render: function (value, type, data) {

                return '<button name="btnRadioCheckStatusActive" class="btn btn-outline-primary">Radio Check</button>';

            }
        },


       
       

           
        {
            data: 'siteName',
            visible: false,
            width: '20%',

        },

       

    ],

    preDrawCallback: function (settings) {
        scrollPosition = $(".dataTables_scrollBody").scrollTop();
    },
    drawCallback: function () {
        $(".dataTables_scrollBody").scrollTop(scrollPosition);
        /*for modifying the size of tables active  guards - start*/
        var count = $('#clientSiteActiveGuards tbody tr').length;
        if (count > 10) {
            $('#clientSiteActiveGuards').closest('.dataTables_scrollBody').css('height', ($(window).height() - 300));

        }
        else {


            $('#clientSiteActiveGuards').closest('.dataTables_scrollBody').css('height', '100%');

        }

        /*for modifying the size of tables active  guards - end*/

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



$('#clientSiteActiveGuards tbody').on('click', '#btnUpArrow', function () {
    

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


//$('#clientSiteActiveGuards tbody').on('click', '#btnActiveGuardsMap', function (value, record) {
   
//    var Gps = $(this).closest("tr").find("#txtGPSActiveguards").val();
//    var loc=getGpsAsHyperLink(Gps);
//    window.open('https://www.google.com/maps?q=' + value,'_blank' )
   
//    '<a href="https://www.google.com/maps?q=' + value + '" target="_blank">' + loc + '</a>'
   

//});
function getGpsAsHyperLink(value) {
    const gps = value.split(',');
    let lat = gps[0];
    let lon = gps[1];
    let latDir = (lat >= 0 ? "N" : "S");
    lat = Math.abs(lat);
    let latMinPart = ((lat - Math.trunc(lat) / 1) * 60);
    let latSecPart = ((latMinPart - Math.trunc(latMinPart) / 1) * 60);
    let lonDir = (lon >= 0 ? "E" : "W");
    lon = Math.abs(lon);
    let lonMinPart = ((lon - Math.trunc(lon) / 1) * 60);
    let lonSecPart = ((lonMinPart - Math.trunc(lonMinPart) / 1) * 60);
    let latitude = Math.trunc(lat) + "." + Math.trunc(latMinPart) + "" + Math.trunc(latSecPart) + '\u00B0 ' + latDir;
    let longitude = Math.trunc(lon) + "." + Math.trunc(lonMinPart) + "" + Math.trunc(lonSecPart) + '\u00B0 ' + lonDir;
    let loc = latitude + ' ' + longitude;
    return loc;
    //return '<a href="https://www.google.com/maps?q=' + value + '" target="_blank">' + loc + '</a>';
}
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
var scrollPosition;
var rowIndex;

let clientSiteInActiveGuards = $('#clientSiteInActiveGuards').DataTable({
    dom: 'Bfrtip',
    buttons: [

        {
            extend: 'copy',
            text: '<i class="fa fa-copy"></i>',
            titleAttr: 'Copy',
            className: 'btn btn-md mr-2 btn-copy'
        },
        {
            extend: 'excel',
            text: '<i class="fa fa-file-excel-o"></i>',
            titleAttr: 'Excel',
            className: 'btn btn-md mr-2 btn-excel'
        },
        {
            extend: 'pdf',
            text: '<i class="fa fa-file-pdf-o"></i>',
            titleAttr: 'PDF',
            className: 'btn btn-md mr-2 btn-pdf'
        },
        {
            extend: 'print',
            text: '<i class="fa fa-print"></i>',
            titleAttr: 'Print',
            className: 'btn btn-md mr-2 btn-print'
        },
        
        {
            text: '<img src="/images/man-climbing-stairs.png" alt="Image" height="16" width="16">',
            titleAttr: 'Custom',
            className: 'btn btn-md mr-2 btn-custom',
            action: function () {
               
                $('#ActionListControlRoomModal').modal('show');
                $('#dglClientTypeActionListAll').val('');
                $('#dglClientSiteIdActionListAll').val('');
            }
        }


    ],
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        {
            "visible": false, "targets": 1

        },
        { "visible": false, "targets": 2 } // Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],   
    info: false,
    searching: true,
    autoWidth: true,
    fixedHeader: true,
    
    "scrollY": ($(window).height() - 300),
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
            class: 'dt-control',
            render: function (value, type, data) {
              
                return '<tr class="group group-start "><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
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
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                    //p4#48 AudioNotification - Binoy - 12-01-2024 -- Start
                    if (rowData.playDuressAlarm == 1) {
                        playAlarm = true;
                    }
                    //p4#48 AudioNotification - Binoy - 12-01-2024 -- End
                }
                
            },
            render: function (value, type, data) {

                if (data.notificationType != 1) {

                    if (data.isEnabled != 1) {
                        return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                            '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>';
                    }
                    else {
                        return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                            '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>'+
                            '&nbsp;&nbsp;&nbsp;<a href="https://www.google.com/maps?q='+data.gpsCoordinates+'" target="_blank" data-toggle="tooltip" title="' + data.enabledAddress+'"><i class="fa fa-map-marker" aria-hidden="true"></i></a>';
                    }

                }
                else {
                    return '&nbsp;&nbsp;&nbsp;<i class="fa fa-user" aria-hidden="true" style="color:#FF0000;"></i> ' + data.guardName;
                      
                }

            }
        },

        {
            data: 'guardLoginTime',
            width: '9%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return '<i class="fa fa-clock-o text-success rc-client-status"></i> ' + value;
            }
        },
        {
            data: 'lastEvent',
            width: '8%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return '<i class="fa fa-clock-o text-success rc-client-status"></i> ' + value;
            }
        },
        {
            data: 'loginTimeZone',
            width: '1%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value;
            }
        },
        {
            data: 'twoHrAlert',
            width: '1%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {
                if (value === 'Green') return '<i class="fa fa-circle text-success"></i>';
                return '<i class="fa fa-circle text-danger"></i>';
            }
        },

        {
            data: 'rcStatus',
            width: '4%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
        },
        {
            targets: -1,
            data: null,
            width: '1%',
            defaultContent: '',
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {

                return '<button name="btnRadioCheckStatus" class="btn btn-outline-primary">RC</button>';

            }
        },
        {
            data: 'siteName',
            visible: false,
            width: '20%',
           
        },

    ],

    preDrawCallback: function (settings) {
        scrollPosition = $(".dataTables_scrollBody").scrollTop();
    },
    drawCallback: function () {
        /* Retain the Scroll position*/
        $(".dataTables_scrollBody").scrollTop(scrollPosition);
        /*for modifying the size of tables   inactive guards - start*/
        var count = $('#clientSiteInActiveGuards tbody tr').length;
        if (count > 10) {
            $('#clientSiteInActiveGuards').closest('.dataTables_scrollBody').css('height', ($(window).height() - 200));

        }
        else {


            $('#clientSiteInActiveGuards').closest('.dataTables_scrollBody').css('height', '100%');

        }

        /* for modifying the size of tables   inactive guards - end*/
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
                        .before('<tr class="group bg-info text-white dt-control "><td colspan="25">' + group + '</td></tr>');

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

        PlayDuressAlarm();
    },
    

});


//p4#48 AudioNotification - Binoy - 12-01-2024 -- Start
function PlayDuressAlarm() {
    if (playAlarm == 1) { 
        //audio.muted = false; // New browser rule doesn't lets audio play automatically        
        audio.play();
        //setting timer to play for 10 sec
        setTimeout(() => {
            audio.pause();
            audio.currentTime = 0; // Works as audio stop
        }, 10000);
        UpdateDuressAlarmPlayed();
    }    
}

function UpdateDuressAlarmPlayed() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/RadioCheckV2?handler=UpdateDuressAlarmPlayedStatus',
        data: {},
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        playAlarm = 0;
        return;
    });
}

//p4#48 AudioNotification - Binoy - 12-01-2024 -- End

clientSiteInActiveGuards.on('draw', function () {
    $('[data-toggle="tooltip"]').tooltip();
});
$('#clientSiteInActiveGuards tbody').on('click', '#btnUpArrow', function () {



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

$('#guardInfoModal').on('shown.bs.modal', function (event) {
    isPaused = true;
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
        return value + '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + record.guardId + '"></i>';
    }
    else return value;
}


/*to get the guards that are not available-start*/
$('#btnNonActiveList').on('click', function () {
    let newTab = window.open();
    newTab.location.href = "/NonActiveGuards";
   
});
let clientSiteNotAvailableGuards = $('#clientSiteNotAvailableGuards').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 }
        ,
        { "visible": false, "targets": 2 }// Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: true,
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
        { data: 'clientSiteId', visible: false},
        {
            data: 'siteName',
            width: '20%',
            class: 'dt-control',
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
                return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i><i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                    '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>';
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

/*to get the guards that are not available-start*/
$('#clientSiteNotAvailableGuards tbody').on('click', '#btnUpArrow', function () {


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
            visible: false,

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
    isPaused = true;
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
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    if (GuardId.length == 0 ) {
         GuardId = $(this).closest("tr").find('td').eq(2).find('#GuardId').val();
    }
    if (ClientSiteId.length == 0) {
         ClientSiteId = $(this).closest("tr").find('td').eq(2).find('#ClientSiteId').val();
    }
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



/* for SW details of the guard-start*/

let clientSiteActiveGuardsSWDetails = $('#clientSiteActiveGuardsSWDetails').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    order: [[1, 'desc']],
    info: false,
    searching: false,
    autoWidth: false,
    fixedHeader: true,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteSWDetails',
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
            data: 'templateName',
            width: '25%',
           
        },
        {
            data: 'smartWand',
            width: '20%'

        },
        {
            data: 'employeePhone',
            width: '25%',
           

        },
        {
            data: 'inspectionStartDatetimeLocal',
            width: '30%',


        },

       


    ],
   
});

$('#clientSiteActiveGuards tbody').on('click', '#btnSWdetails', function (value, record) {
    $('#guardSWInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    if (GuardId.length == 0) {
        GuardId = $(this).closest("tr").find('td').eq(2).find('#GuardId').val();
    }
    if (ClientSiteId.length == 0) {
        ClientSiteId = $(this).closest("tr").find('td').eq(2).find('#ClientSiteId').val();
    }

    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader4').text(GuardName + '-' + 'Smart Wand Scan Details');
    clientSiteActiveGuardsSWDetails.ajax.reload();

});

/* for SW details of the guard-end*/
$('#clientSiteActiveGuards tbody').on('click', '#btnIncidentReportdetails', function (value, record) {
    $('#guardIncidentReportsInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    if (GuardId.length == 0) {
        GuardId = $(this).closest("tr").find('td').eq(2).find('#GuardId').val();
    }
    if (ClientSiteId.length == 0) {
        ClientSiteId = $(this).closest("tr").find('td').eq(2).find('#ClientSiteId').val();
    }

    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader2').text(GuardName + '-' + 'Incident Report Details');
    clientSiteActiveGuardsIncidentReportsDetails.ajax.reload();

});
/*for incident report details of the guard - end*/

/*For radio check dropdown start*/

$('#clientSiteInActiveGuards').on('click', 'button[name="btnRadioCheckStatus"]', function () {
    var data = clientSiteInActiveGuards.row($(this).parents('tr')).data();
    var rowClientSiteId = data.clientSiteId;
    var rowGuardId = data.guardId;
    var rcSatus = data.rcStatus;

    var IsEnabled = data.isEnabled;
    
    if (IsEnabled == 0) {
        // Remove the option based on its value
        $('#selectRadioStatus option:contains("Deactivate")').hide();
    }
    else {
        $('#selectRadioStatus option:contains("Deactivate")').show();
    }
    

    $("#selectRadioStatus").val(data.statusId);

    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#selectRadioCheckStatus').modal('show');
    isPaused = true;
});

$('#clientSiteActiveGuards').on('click', 'button[name="btnRadioCheckStatusActive"]', function () {
    var data = clientSiteActiveGuards.row($(this).parents('tr')).data();
    var rowClientSiteId = data.clientSiteId;
    var rowGuardId = data.guardId;
    var rcSatus = data.rcStatus;
    $("#selectRadioStatusActive").val(rcSatus);
    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#selectRadioStatusActive option:contains("Deactivate")').hide();
    $('#selectRadioCheckStatusActive').modal('show');
    isPaused = true;
});

$('#btnSaveRadioStatus').on('click', function () {
    //const checkedStatus = $('#selectRadioStatus').val();
    var clientSiteId = $('#clientSiteId').val();
    const checkedStatus = $('#selectRadioStatus option:selected').text();
    var statusId = $('#selectRadioStatus').val();
    var guardId = $('#guardId').val();
    if (checkedStatus === '') {
        return;
    }
    // Task p6#73_TimeZone issue -- added by Binoy - Start   
    fillRefreshLocalTimeZoneDetails(tmzdata, "", false);
    // Task p6#73_TimeZone issue -- added by Binoy - End
    $.ajax({
        url: '/RadioCheckV2?handler=SaveRadioStatus',
        type: 'POST',
        data: {
            clientSiteId: clientSiteId,
            guardId: guardId,
            checkedStatus: checkedStatus,
            active: true,
            statusId: statusId,
            tmzdata: tmzdata
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function () {
        $('#selectRadioCheckStatus').modal('hide');
        $('#selectRadioStatus').val('');
        clientSiteActiveGuards.ajax.reload();
        clientSiteInActiveGuards.ajax.reload();
        clientSiteInActiveGuardsSinglePage.ajax.reload();
        clientSiteActiveGuardsSinglePage.ajax.reload();
    });
});
$('#radio_duress_btn').on('click', function () {
    $.ajax({
        url: '/RadioCheckV2?handler=SaveDuress',
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.status) {
            $('#radio_duress_btn').removeClass('normal').addClass('active');
            $("#duress_status").addClass('font-weight-bold');
            $("#duress_status").text("Active");
        }
        //gridGuardLog.clear();
        //gridGuardLog.reload();
    });
});
$('#btnSaveRadioStatusActive').on('click', function () {
    const checkedStatus = $('#selectRadioStatusActive option:selected').text();
    var statusId = $('#selectRadioStatusActive').val();
    var clientSiteId = $('#clientSiteId').val();
    var guardId = $('#guardId').val();
    if (checkedStatus === '') {
        return;
    }
    // Task p6#73_TimeZone issue -- added by Binoy - Start   
    fillRefreshLocalTimeZoneDetails(tmzdata, "", false);
    // Task p6#73_TimeZone issue -- added by Binoy - End
    $.ajax({
        url: '/RadioCheckV2?handler=SaveRadioStatus',
        type: 'POST',
        data: {
            clientSiteId: clientSiteId,
            guardId: guardId,
            checkedStatus: checkedStatus,
            active: true,
            statusId: statusId,
            tmzdata: tmzdata
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function () {
        $('#selectRadioCheckStatusActive').modal('hide');
        $('#selectRadioStatus').val('');      
        clientSiteActiveGuards.ajax.reload();
        clientSiteInActiveGuards.ajax.reload();
        clientSiteInActiveGuardsSinglePage.ajax.reload();
        clientSiteActiveGuardsSinglePage.ajax.reload();

    });
});


/*For radio check dropdown  end - end*/

/*for pushing notifications from the control room - start*/
$('#pushNoTificationsControlRoomModal').on('shown.bs.modal', function (event) {

    isPaused = true;
    
    const button = $(event.relatedTarget);
    const id = button.data('id');
    $('#txtNotificationsCompanyId').val(id);
    $('#chkLB').prop('checked', true);
    $('#chkSiteEmail').prop('checked', true);
    $('#chkSMSPersonal').prop('checked', false);
    $('#chkSMSSmartWand').prop('checked', false); $('#txtPushNotificationMessage').val('');
    $('#chkNationality').prop('checked', false);
    $('#chkSiteState').prop('checked', false);
    $('#chkSiteState').prop('checked', false);
    $('#chkClientType').prop('checked', false);
    $('#chkSMSPersonalGlobal').prop('checked', false);
    $('#chkSMSSmartWandGlobal').prop('checked', false);
    $('#txtGlobalNotificationMessage').val('');
    $('#State1').prop('disabled', 'disabled');
    $('#State1').val('ACT');
    $('#dglClientType').multiselect("disable");
    $('#dglClientSiteId').multiselect("disable");
    $('#dglClientType').val('');
    $('#dglClientSiteId').val('');
    $('#Access_permission_RC_status').hide();
    $('#Access_permission_RC_status_new').hide()
    $('#txtGlobalNotificationMessage').val('');
    $('#txtPushNotificationMessage').val('');
    clearGuardValidationSummary('PushNotificationsValidationSummary');
    var clientSiteId = $('#txtNotificationsCompanyId').val();
    $('#Site_Alarm_Keypad_code').val('');
    $('#Action1').val('');
    $('#site_Physical_key').val('');
    $('#Action2').val('');
    $('#Action3').val('');
    $('#Action4').val('');
    $('#Action5').val('');
    $('#Site_Combination_Look').val('');
    $('#txtComments').html('');

    $('#search_client_site').val('');
    $('#searchResults').html('');
    $('#dglClientTypeActionList').val('');
    $('#dglClientTypeActionList2').val('');
    $('#dglClientSiteIdActionList').val('');
    $('#dglClientSiteIdActionList2').val('');
    
    
   
    //var clientSiteId = $('#dglClientSiteIdActionList').val();

    $.ajax({
        url: '/RadioCheckV2?handler=ActionList',
        type: 'POST',
        data: {
            clientSiteId: clientSiteId
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        if (data != null) {
            $('#Site_Alarm_Keypad_code').val(data.siteAlarmKeypadCode);
            $('#Action1').val(data.action1);
            $('#site_Physical_key').val(data.sitephysicalkey);
            $('#Action2').val(data.action2);
            $('#Action3').val(data.action3);
            $('#Action4').val(data.action4);
            $('#Action5').val(data.action5);
            $('#Site_Combination_Look').val(data.siteCombinationLook);
            $('#txtComments').html(data.controlRoomOperator);
            $('#download_imageRCList').attr('href', '/RCImage/' + data.imagepath + '');
        }
        
    });

  
});


//$('#chkLB').on('change', function () {
//    const isChecked = $(this).is(':checked');
//    $('#IsLB').val(isChecked);
//});
//$('#chkSiteEmail').on('change', function () {
//    const isChecked = $(this).is(':checked');
//    $('#IsSiteEmail').val(isChecked);
//});
//$('#chkSMSPersonal').on('change', function () {
//    const isChecked = $(this).is(':checked');
//    $('#IsSMSPersonal').val(isChecked);
//});
//$('#chkSMSSmartWand').on('change', function () {
//    const isChecked = $(this).is(':checked');
//    $('#IsSMSSmartWand').val(isChecked);
//});
function clearGuardValidationSummary(validationControl) {
    $('#' + validationControl).removeClass('validation-summary-errors').addClass('validation-summary-valid');
    $('#' + validationControl).html('');
}













$('#btnSendPushLotificationMessage').on('click', function () {
    const checkedLB = $('#chkLB').is(':checked');
    const checkedSiteEmail = $('#chkSiteEmail').is(':checked');
    const checkedSMSPersonal = $('#chkSMSPersonal').is(':checked');
    const checkedSMSSmartWand = $('#chkSMSSmartWand').is(':checked');
    var clientSiteId = $('#txtNotificationsCompanyId').val();
    var Notifications = $('#txtPushNotificationMessage').val();
    var Subject = $('#txtPushNotificationSubject').val();
   
    if (Notifications === '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter a Message to send ');
    }
    else if (checkedLB == false && checkedSiteEmail == false && checkedSMSPersonal == false && checkedSMSSmartWand == false) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select any one of the transfer options ');

    }
    else {
        $('#Access_permission_RC_status_new').hide();
        $('#Access_permission_RC_status_new').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Sending Email. Please wait...').show();
        Notifications = Notifications;
        // Task p6#73_TimeZone issue -- added by Binoy - Start   
        fillRefreshLocalTimeZoneDetails(tmzdata, "", false);
        // Task p6#73_TimeZone issue -- added by Binoy - End
        $.ajax({
            url: '/RadioCheckV2?handler=SavePushNotificationTestMessages',
            type: 'POST',
            data: {
                clientSiteId: clientSiteId,
                checkedLB: checkedLB,
                checkedSiteEmail: checkedSiteEmail,
                checkedSMSPersonal: checkedSMSPersonal,
                checkedSMSSmartWand: checkedSMSSmartWand,
                Notifications: Notifications,
                Subject: Subject,
                tmzdata: tmzdata
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $('#pushNoTificationsControlRoomModal').modal('hide');
                $('#Access_permission_RC_status_new').hide();
            }
            else {
                displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
               
            }
            //$('#selectRadioStatus').val('');
            //$('#btnRefreshActivityStatus').trigger('click');
        });
    }
});
function displayGuardValidationSummary(validationControl, errors) {
    $('#' + validationControl).removeClass('validation-summary-valid').addClass('validation-summary-errors');
    $('#' + validationControl).html('');
    $('#' + validationControl).append('<ul></ul>');
    if (!Array.isArray(errors)) {
        $('#' + validationControl + ' ul').append('<li>' + errors + '</li>');
    } else {
        errors.forEach(function (item) {
            if (item.indexOf(',') > 0) {
                item.split(',').forEach(function (itemInner) {
                    $('#' + validationControl + ' ul').append('<li>' + itemInner + '</li>');
                });
            } else {
                $('#' + validationControl + ' ul').append('<li>' + item + '</li>');
            }
        });
    }
}

/*for pushing notifications from the control room - end*/
function clearGuardValidationSummary(validationControl) {
    $('#' + validationControl).removeClass('validation-summary-errors').addClass('validation-summary-valid');
    $('#' + validationControl).html('');
}

$('#openInActiveGuardInNewPage').on('click', function () {
    let newTab = window.open();
    newTab.location.href = "/InActiveGuardSinglePage";
    
});

$('#openActiveGuardInNewPage').on('click', function () {
    let newTab = window.open();
    newTab.location.href = "/ActiveGuardSinglePage";

});
/*code added for Global Messsage start*/
$('#btnSendGlabalNotificationMessage').on('click', function () {
    const checkedState = $('#chkSiteState').is(':checked');
    const checkedSiteEmail = $('#chkSiteEmail').is(':checked');
    const checkedSMSPersonal = $('#chkSMSPersonalGlobal').is(':checked');
    const checkedSMSSmartWand = $('#chkSMSSmartWandGlobal').is(':checked');
    var clientSiteId = $('#dglClientSiteId').val();
    var Notifications = $('#txtGlobalNotificationMessage').val();
    var Subject = $('#txtGlobalNotificationSubject').val();
    var State = $('#State1').val();
    var ClientType = $('#dglClientType').val();
    const chkClientType = $('#chkClientType').is(':checked');
    const chkNationality = $('#chkNationality').is(':checked');

    if (Notifications === '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter a Message to send ');
    }
    else if (checkedState == false && chkClientType == false && chkClientType == false && checkedSMSPersonal == false && checkedSMSSmartWand == false && chkNationality == false) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select any one of the transfer options ');

    }
    else if (chkClientType == true && ClientType == null)
    {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
    }
    else {

        $('#Access_permission_RC_status').hide();
        $('#Access_permission_RC_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Sending Email. Please wait...').show();
        // Task p6#73_TimeZone issue -- added by Binoy - Start   
        fillRefreshLocalTimeZoneDetails(tmzdata, "", false);
        // Task p6#73_TimeZone issue -- added by Binoy - End
        $.ajax({
            url: '/RadioCheckV2?handler=SaveGlobalNotificationTestMessages',
            type: 'POST',
            data: {
                checkedState: checkedState,
                State: State,
                Notifications: Notifications,
                Subject: Subject,
                chkClientType: chkClientType,
                ClientType: ClientType,
                chkNationality: chkNationality,
                checkedSMSPersonal: checkedSMSPersonal,
                checkedSMSSmartWand: checkedSMSSmartWand,
                clientSiteId: clientSiteId,
                tmzdata: tmzdata
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $('#pushNoTificationsControlRoomModal').modal('hide');
                $('#Access_permission_RC_status').hide();
            }
            else {
                displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
            }
            //$('#selectRadioStatus').val('');
            //$('#btnRefreshActivityStatus').trigger('click');
        });
    }
});
/*code added for Global Messsage stop*/
/*code added for client site Dropdown start*/
//$('#dglClientSiteId').select2({
//    placeholder: 'Select',
//    theme: 'bootstrap4'
//});

/*code added for RCActionList start*/
$('#btnSendActionList').on('click', function () {
   
    var clientSiteId = $('#dglClientSiteIdActionList2').val();
    var Notifications = $('#txtMessageActionList').val();
    var Subject = $('#txtGlobalNotificationSubject').val();
  
    var ClientType = $('#dglClientTypeActionList2').val();
    var ClientSite = $('#dglClientSiteIdActionList2').val();
    var AlarmKeypadCode = $('#Site_Alarm_Keypad_code').val();
    var Action1 = $('#Action1').val();
    var Physicalkey = $('#site_Physical_key').val();
    var Action2 = $('#Action2').val();
    var SiteCombinationLook = $('#Site_Combination_Look').val();
    var Action3 = $('#Action3').val();
    var Action4 = $('#Action4').val();
    var Action5 = $('#Action5').val();
    var CommentsForControlRoomOperator = $('#txtComments').val();

    if (Notifications === '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter a Message to send ');
    }
    
    else if (chkClientType == true && ClientType == null) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
    }
    else if (ClientType=='') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
    }
    else if (ClientSite == '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client site ');
    }
    else {

        $('#Access_permission_RC_status').hide();
        $('#Access_permission_RC_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Sending Email. Please wait...').show();
        // Task p6#73_TimeZone issue -- added by Binoy - Start   
        fillRefreshLocalTimeZoneDetails(tmzdata, "", false);
        // Task p6#73_TimeZone issue -- added by Binoy - End
        $.ajax({
            url: '/RadioCheckV2?handler=SaveActionList',
            type: 'POST',
            data: {
                Notifications: Notifications,
                Subject: Subject,
                ClientType: ClientType,
                clientSiteId: clientSiteId,
                AlarmKeypadCode: AlarmKeypadCode,
                Action1: Action1,
                Physicalkey: Physicalkey,
                Action2: Action2,
                SiteCombinationLook: SiteCombinationLook,
                Action3: Action3,
                Action4: Action4,
                Action5: Action5,
                CommentsForControlRoomOperator: CommentsForControlRoomOperator,
                tmzdata: tmzdata
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $('#pushNoTificationsControlRoomModal').modal('hide');
                $('#Access_permission_RC_status').hide();
            }
            else {
                displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
            }
            //$('#selectRadioStatus').val('');
            //$('#btnRefreshActivityStatus').trigger('click');
        });
    }
});

$('#btnSendActionListGlobal').on('click', function () {

    var clientSiteId = $('#dglClientSiteIdActionList2All').val();
    var Notifications = $('#txtMessageActionList1All').val();
    var Subject = $('#txtGlobalNotificationSubjectAll').val();

    var ClientType = $('#dglClientTypeActionList2All').val();
    var ClientSite = $('#dglClientSiteIdActionList2All').val();
    var AlarmKeypadCode = $('#Site_Alarm_Keypad_codeAll').val();
    var Action1 = $('#Action1All').val();
    var Physicalkey = $('#site_Physical_keyAll').val();
    var Action2 = $('#Action2All').val();
    var SiteCombinationLook = $('#Site_Combination_LookAll').val();
    var Action3 = $('#Action3All').val();
    var Action4 = $('#Action4All').val();
    var Action5 = $('#Action5All').val();
    var CommentsForControlRoomOperator = $('#txtCommentsAll').val();
    // Task p6#73_TimeZone issue -- added by Binoy - Start   
    fillRefreshLocalTimeZoneDetails(tmzdata, "", false);
    // Task p6#73_TimeZone issue -- added by Binoy - End
    if (Notifications === '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter a Message to send ');
    }

    else if (chkClientType == true && ClientType == null) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
    }
    else if (ClientType == '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
    }
    else if (ClientSite == '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client site ');
    }
    else {

        $('#Access_permission_RC_statusAll').hide();
        $('#Access_permission_RC_statusAll').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Sending Email. Please wait...').show();
        $.ajax({
            url: '/RadioCheckV2?handler=SaveActionListGlobal',
            type: 'POST',
            data: {
                Notifications: Notifications,
                Subject: Subject,
                ClientType: ClientType,
                clientSiteId: clientSiteId,
                AlarmKeypadCode: AlarmKeypadCode,
                Action1: Action1,
                Physicalkey: Physicalkey,
                Action2: Action2,
                SiteCombinationLook: SiteCombinationLook,
                Action3: Action3,
                Action4: Action4,
                Action5: Action5,
                CommentsForControlRoomOperator: CommentsForControlRoomOperator,
                tmzdata: tmzdata

            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $('#ActionListControlRoomModal').modal('hide');
                $('#Access_permission_RC_statusAll').hide();
            }
            else {
                displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
            }
            //$('#selectRadioStatus').val('');
            //$('#btnRefreshActivityStatus').trigger('click');
        });
    }
});


$('#dglClientSiteIdActionList').on('change', function () {
    $('#Site_Alarm_Keypad_code').val('');
    $('#Action1').val('');
    $('#site_Physical_key').val('');
    $('#Action2').val('');
    $('#Action3').val('');
    $('#Action4').val('');
    $('#Action5').val('');
    $('#Site_Combination_Look').val('');
    $('#txtComments').html('');
    var clientSiteId = $('#dglClientSiteIdActionList').val();
   
        $.ajax({
            url: '/RadioCheckV2?handler=ActionList',
            type: 'POST',
            data: {  
                clientSiteId: clientSiteId
         },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            $('#Site_Alarm_Keypad_code').val(data.siteAlarmKeypadCode);
            $('#Action1').val(data.action1);
            $('#site_Physical_key').val(data.sitephysicalkey);
            $('#Action2').val(data.action2);
            $('#Action3').val(data.action3);
            $('#Action4').val(data.action4);
            $('#Action5').val(data.action5);
            $('#Site_Combination_Look').val(data.siteCombinationLook);
            $('#txtComments').html(data.controlRoomOperator);
        });
    
});
$('#dglClientSiteIdActionListAll').on('change', function () {
    $('#Site_Alarm_Keypad_codeAll').val('');
    $('#Action1All').val('');
    $('#site_Physical_keyAll').val('');
    $('#Action2All').val('');
    $('#Action3All').val('');
    $('#Action4All').val('');
    $('#Action5All').val('');
    $('#Site_Combination_LookAll').val('');
    $('#txtCommentsAll').html('');
    var clientSiteId = $('#dglClientSiteIdActionListAll').val();

    $.ajax({
        url: '/RadioCheckV2?handler=ActionList',
        type: 'POST',
        data: {
            clientSiteId: clientSiteId
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        $('#Site_Alarm_Keypad_codeAll').val(data.siteAlarmKeypadCode);
        $('#Action1All').val(data.action1);
        $('#site_Physical_keyAll').val(data.sitephysicalkey);
        $('#Action2All').val(data.action2);
        $('#Action3All').val(data.action3);
        $('#Action4All').val(data.action4);
        $('#Action5All').val(data.action5);
        $('#Site_Combination_LookAll').val(data.siteCombinationLook);
        $('#txtCommentsAll').html(data.controlRoomOperator);
    });

});
$('#search_client_site').keypress(function (e) {
    var searchInput = $('#search_client_site');
    var minSearchLength = 4;

    searchInput.keypress(function (e) {
        if (searchInput.val().length >= minSearchLength && e.which === 13) {
            performSearch();
        }
    });
});
function performSearch() {
    var searchTerm = $('#search_client_site').val();
    $.ajax({
        url: '/RadioCheckV2?handler=SearchClientsite',
        type: 'POST',
        data: {
            searchTerm: searchTerm
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        var html = '';
        if (data =='No matching record found') {
             html = '<p style="color:brown">' + data + '</p>';
        }
        else {
            html = '<p style="color:brown"><i class="fa fa-map-marker" aria-hidden="true"></i>' + data + '</p>';
        }
         
        $('#searchResults').html(html);
    });

   
}
$('#search_client_siteAll').keypress(function (e) {
    var searchInput = $('#search_client_siteAll');
    var minSearchLength = 1;

    searchInput.keypress(function (e) {
        if (searchInput.val().length >= minSearchLength && e.which === 13) {
            performSearchClientSite();
        }
    });
});
function performSearchClientSite() {
    $('#Site_Alarm_Keypad_codeAll').val('');
    $('#Action1All').val('');
    $('#site_Physical_keyAll').val('');
    $('#Action2All').val('');
    $('#Action3All').val('');
    $('#Action4All').val('');
    $('#Action5All').val('');
    $('#Site_Combination_LookAll').val('');
    $('#txtCommentsAll').html('');
    $('#searchResultsNew').html('');
    var searchTerm = $('#search_client_siteAll').val();
    $.ajax({
        url: '/RadioCheckV2?handler=SearchClientsiteRCList',
        type: 'POST',
        data: {
            searchTerm: searchTerm
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        var html = '';
        if (data == null) {
            html = '<p style="color:brown">No Matching Records</p>';
            $('#searchResultsNew').html(html);
        }
        else {
            $('#Site_Alarm_Keypad_codeAll').val(data.siteAlarmKeypadCode);
            $('#Action1All').val(data.action1);
            $('#site_Physical_keyAll').val(data.sitephysicalkey);
            $('#Action2All').val(data.action2);
            $('#Action3All').val(data.action3);
            $('#Action4All').val(data.action4);
            $('#Action5All').val(data.action5);
            $('#Site_Combination_LookAll').val(data.siteCombinationLook);
            $('#txtCommentsAll').html(data.controlRoomOperator);
        }
        
       
    });


}
/*code added for RCActionList stop*/




$('#dglClientSiteId').on('change', function () {
    const clientTypeId = $(this).val();
    $("#vklClientSiteId").val(clientTypeId);
    $("#vklClientSiteId").multiselect("refresh");




});
$('.GlobalAlert-checkbox').on('change', function () {
    if ($(this).prop('checked')) {

        $('.GlobalAlert-checkbox').not(this).prop('checked', false);
    }
});

/*code added for client site Dropdown stop*/
/*to get the client type and site as multiselect-start*/
$('#dglClientType').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
$('#dglClientSiteId').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
//$('#dglClientTypeActionList').multiselect({
//    maxHeight: 400,
//    buttonWidth: '100%',
//    nonSelectedText: 'Select',
//    buttonTextAlignment: 'left',
//    includeSelectAllOption: true,
//});
//$('#dglClientSiteIdActionList').multiselect({
//    maxHeight: 400,
//    buttonWidth: '100%',
//    nonSelectedText: 'Select',
//    buttonTextAlignment: 'left',
//    includeSelectAllOption: true,
//});
//$('#dglClientTypeActionList2').multiselect({
//    maxHeight: 400,
//    buttonWidth: '100%',
//    nonSelectedText: 'Select',
//    buttonTextAlignment: 'left',
//    includeSelectAllOption: true,
//});
//$('#dglClientSiteIdActionList2').multiselect({
//    maxHeight: 400,
//    buttonWidth: '100%',
//    nonSelectedText: 'Select',
//    buttonTextAlignment: 'left',
//    includeSelectAllOption: true,
//});
$('#chkNationality').on('change', function () {
    const isChecked = $(this).is(':checked');
    $('#IsLB').val(isChecked);
    if (isChecked == true) {
        $('#chkSiteState').prop('checked', false);
        $('#chkSiteState').prop('checked', false);
        $('#chkClientType').prop('checked', false);
        $('#chkSMSPersonalGlobal').prop('checked', false);
        $('#chkSMSSmartWandGlobal').prop('checked', false);
        $('#State1').prop('disabled', 'disabled');
        $('#State1').val('ACT');
        $('#dglClientType').val('');
        $('#dglClientSiteId').val('');
        $('#dglClientType').multiselect("disable");
        $('#dglClientSiteId').multiselect("disable");
    }
});
$('#chkSiteState').change(function () {
    const isChecked = $(this).is(':checked');
    if (isChecked == true) {
        $('#State1').prop('disabled', false);
        $('#chkNationality').prop('checked', false);
        $('#chkClientType').prop('checked', false);
        $('#chkSMSPersonalGlobal').prop('checked', false);
        $('#chkSMSSmartWandGlobal').prop('checked', false);
        $('#State1').val('ACT');
        $('#dglClientType').val('');
        $('#dglClientSiteId').val('');
        $('#dglClientType').multiselect("disable");
        $('#dglClientSiteId').multiselect("disable");
    } else {
        $('#State1').prop('disabled', 'disabled');
        $('#State1').val('ACT');
    }
});

//$('#dglClientTypeActionList').on('change', function () {
//    const clientTypeId = $(this).val().join(';');
//    $('#dglClientSiteIdActionList').multiselect("refresh");
//    $('#dglClientSiteIdActionList').html('');
//    const clientSiteControl = $('#dglClientSiteIdActionList');
//    var selectedOption = $(this).find("option:selected");
//    var selectedText = selectedOption.text();

//    $.ajax({
//        url: '/RadioCheckV2?handler=ClientSitesNew',
//        type: 'GET',
//        data: {
//            typeId: clientTypeId

//        },
//        dataType: 'json',
//        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
//    }).done(function (data) {

//        data.map(function (site) {
//            clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
//        });
//        clientSiteControl.multiselect('rebuild');

//    });


//});

$('#dglClientTypeActionList').on('change', function () {
    const clientTypeId = $(this).val();
    const clientSiteControl = $('#dglClientSiteIdActionList');
    clientSiteControl.html('');
    $.ajax({
        url: '/RadioCheckV2?handler=ClientSitesNew',
        type: 'GET',
        data: {
            typeId: clientTypeId

        },
        dataType: 'json',
        success: function (data) {
            $('#dglClientSiteIdActionList').append(new Option('Select', '', true, true));
            data.map(function (site) {
                $('#dglClientSiteIdActionList').append(new Option(site.name, site.id, false, false));
            });
            
        }
    });


});
$('#dglClientTypeActionList2').on('change', function () {
    const clientTypeId = $(this).val();
    const clientSiteControl = $('#dglClientSiteIdActionList2');
    clientSiteControl.html('');
    $.ajax({
        url: '/RadioCheckV2?handler=ClientSitesNew',
        type: 'GET',
        data: {
            typeId: clientTypeId

        },
        dataType: 'json',
        success: function (data) {
            $('#dglClientSiteIdActionList2').append(new Option('Select', '', true, true));
            data.map(function (site) {
                $('#dglClientSiteIdActionList2').append(new Option(site.name, site.id, false, false));
            });

        }
    });


});
$('#dglClientTypeActionListAll').on('change', function () {
    const clientTypeId = $(this).val();
    const clientSiteControl = $('#dglClientSiteIdActionListAll');
    clientSiteControl.html('');
    $.ajax({
        url: '/RadioCheckV2?handler=ClientSitesNew',
        type: 'GET',
        data: {
            typeId: clientTypeId

        },
        dataType: 'json',
        success: function (data) {
            $('#dglClientSiteIdActionListAll').append(new Option('Select', '', true, true));
            data.map(function (site) {
                $('#dglClientSiteIdActionListAll').append(new Option(site.name, site.id, false, false));
            });

        }
    });


});
$('#dglClientTypeActionList2All').on('change', function () {
    const clientTypeId = $(this).val();
    const clientSiteControl = $('#dglClientSiteIdActionList2All');
    clientSiteControl.html('');
    $.ajax({
        url: '/RadioCheckV2?handler=ClientSitesNew',
        type: 'GET',
        data: {
            typeId: clientTypeId

        },
        dataType: 'json',
        success: function (data) {
            $('#dglClientSiteIdActionList2All').append(new Option('Select', '', true, true));
            data.map(function (site) {
                $('#dglClientSiteIdActionList2All').append(new Option(site.name, site.id, false, false));
            });

        }
    });


});
$('#dglClientSiteIdActionList2All').select({
    placeholder: 'Select',
    theme: 'bootstrap4'
});
$('#dglClientSiteIdActionListAll').select({
    placeholder: 'Select',
    theme: 'bootstrap4'
});
$('#dglClientSiteIdActionList2').select({
    placeholder: 'Select',
    theme: 'bootstrap4'
});
$('#dglClientSiteIdActionList').select({
    placeholder: 'Select',
    theme: 'bootstrap4'
});
$('#chkClientType').change(function () {
    const isChecked = $(this).is(':checked');
    if (isChecked == true) {
        $('#State1').prop('disabled', 'disabled');
        $('#chkNationality').prop('checked', false);
        $('#chkSiteState').prop('checked', false);
        $('#chkSMSPersonalGlobal').prop('checked', false);
        $('#chkSMSSmartWandGlobal').prop('checked', false);
        //$('#dglClientType option').removeAttr('disabled');
        $('#dglClientType').val('');
        $('#dglClientType').multiselect("enable");
        $('#dglClientSiteId').multiselect("enable");
        $('#dglClientSiteId').val('');
        $('#dglClientSiteId').html('');
        
    } else {
        $('#dglClientType').val('').trigger("change");
        
        $('#dglClientType').multiselect("refresh");
        $('#dglClientType').val('');
        $('#dglClientSiteId').val('');
        $('#dglClientType').multiselect("disable");
        $('#dglClientSiteId').multiselect("disable");
        
        $('#dglClientSiteId').html('');
       
        
    }
    $('#dglClientType').on('change', function () {
        const clientTypeId = $(this).val().join(';');
        $('#dglClientSiteId').multiselect("refresh");
        $('#dglClientSiteId').html('');
        const clientSiteControl = $('#dglClientSiteId');
        var selectedOption = $(this).find("option:selected");
        var selectedText = selectedOption.text();
        
        /*$("#vklClientType").multiselect("refresh");*/
        // gridsiteLog.clear();

        //const clientSiteControlvkl = $('#vklClientSiteId');
        //keyVehicleLogReport.clear().draw();
        //clientSiteControlvkl.html('');

        //clientSiteControl.html('');
        $.ajax({
            url: '/RadioCheckV2?handler=ClientSitesNew',
            type: 'GET',
            data: {
                typeId: clientTypeId

            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {

            data.map(function (site) {
                clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
            });
            clientSiteControl.multiselect('rebuild');
            //$('#selectRadioStatus').val('');
            //$('#btnRefreshActivityStatus').trigger('click');
        });


    });
    

});
/*to get the client type and site as multiselect-end*/

/* Single page Grid Start  */


let clientSiteInActiveGuardsSinglePage = $('#clientSiteInActiveGuardsSinglePage').DataTable({
    dom: 'Bfrtip',
    buttons: [

        {
            extend: 'copy',
            text: '<i class="fa fa-copy"></i>',
            titleAttr: 'Copy',
            className: 'btn btn-md mr-2 btn-copy'
        },
        {
            extend: 'excel',
            text: '<i class="fa fa-file-excel-o"></i>',
            titleAttr: 'Excel',
            className: 'btn btn-md mr-2 btn-excel'
        },
        {
            extend: 'pdf',
            text: '<i class="fa fa-file-pdf-o"></i>',
            titleAttr: 'PDF',
            className: 'btn btn-md mr-2 btn-pdf'
        },
        {
            extend: 'print',
            text: '<i class="fa fa-print"></i>',
            titleAttr: 'Print',
            className: 'btn btn-md mr-2 btn-print'
        },
        


    ],
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        {
            "visible": false, "targets": 1

        },
        { "visible": false, "targets": 2 } // Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: true,
    fixedHeader: true,

    "scrollY": ($(window).height() - 100),
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
            class: 'dt-control',
            render: function (value, type, data) {

                return '<tr class="group group-start "><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
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
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }
            },
            render: function (value, type, data) {

                if (data.notificationType != 1) {

                    if (data.isEnabled != 1) {
                        return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                            '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>';
                    }
                    else {
                        return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                            '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>' +
                            '&nbsp;&nbsp;&nbsp;<a href="https://www.google.com/maps?q=' + data.gpsCoordinates + '" target="_blank" data-toggle="tooltip" title="' + data.enabledAddress + '"><i class="fa fa-map-marker" aria-hidden="true"></i></a>';
                    }
                    
                }
                else {
                    return '&nbsp;&nbsp;&nbsp;<i class="fa fa-user" aria-hidden="true" style="color:#FF0000;"></i> ' + data.guardName;

                }

            }
        },

        {
            data: 'guardLoginTime',
            width: '9%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return '<i class="fa fa-clock-o text-success rc-client-status"></i> ' + value;
            }
        },
        {
            data: 'lastEvent',
            width: '8%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return '<i class="fa fa-clock-o text-success rc-client-status"></i> ' + value;
            }
        },
        {
            data: 'loginTimeZone',
            width: '1%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value;
            }
        },
        {
            data: 'twoHrAlert',
            width: '1%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {
                if (value === 'Green') return '<i class="fa fa-circle text-success"></i>';
                return '<i class="fa fa-circle text-danger"></i>';
            }
        },

        {
            data: 'rcStatus',
            width: '4%',
            className: "text-center",
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
        },
        {
            targets: -1,
            data: null,
            width: '1%',
            defaultContent: '',
            createdCell: function (cell, cellData, rowData, rowIndex, colIndex) {
                // Define your conditions to add a class
                if (rowData.isEnabled == 1) {
                    cell.classList.add('bg-danger');
                }

            },
            render: function (value, type, data) {

                return '<button name="btnRadioCheckStatus" class="btn btn-outline-primary">RC</button>';

            }
        },
        {
            data: 'siteName',
            visible: false,
            width: '20%',

        },

    ],

    preDrawCallback: function (settings) {
        scrollPosition = $(".dataTables_scrollBody").scrollTop();
    },
    drawCallback: function () {
        /* Retain the Scroll position*/
        $(".dataTables_scrollBody").scrollTop(scrollPosition);
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
                        .before('<tr class="group bg-info text-white dt-control "><td colspan="25">' + group + '</td></tr>');

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

$('#clientSiteInActiveGuardsSinglePage tbody').on('click', '#btnUpArrow', function () {



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


$('#clientSiteInActiveGuardsSinglePage').on('click', 'button[name="btnRadioCheckStatus"]', function () {
    var data = clientSiteInActiveGuardsSinglePage.row($(this).parents('tr')).data();
    var rowClientSiteId = data.clientSiteId;
    var rowGuardId = data.guardId;
    var rcSatus = data.rcStatus;
    $("#selectRadioStatus").val(data.statusId);    
    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#selectRadioCheckStatus').modal('show');
    isPaused = true;
});





let clientSiteActiveGuardsSinglePage = $('#clientSiteActiveGuardsSinglePage').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 },// Hide the group column initially
        { "visible": false, "targets": 2 }
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: true,
    autoWidth: true,
    fixedHeader: true,
    "scrollY": ($(window).height() ),
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
            class: 'dt-control',
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
                return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                    '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>';
            }
        },
        {
            data: 'logBook',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardLogBookInfoModal" id="btnLogBookDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" text="' + data.clientSiteId + '"><input type="hidden" id="GuardId" text="' + data.guardId + '"> ';
            }
        },
        {
            data: 'keyVehicle',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardKeyVehicleInfoModal" id="btnKeyVehicleDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
            }
        },
        {
            data: 'incidentReport',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardIncidentReportsInfoModal" id="btnIncidentReportdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '"> ' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
            }

        },
        {
            data: 'smartWands',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardSWInfoModal" id="btnSWdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '"> ' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
            }
        },
        //{
        //    data: 'rcStatus',
        //    width: '5%',
        //    className: "text-center",
        //},

        {
            data: 'rcColorId',
            width: '5%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return '';
                else if (value === 4)
                    return '<i class="fa fa-check-circle text-success"></i>' + ' [' + '<a href="#hoverModal" id="btnGreen1hover" >' + 1 + '</a>' + '] <input type="hidden" id="RCStatusId" value="' + data.rcSatus + '"><input type="hidden" id="RCColortype" value="' + data.rcColor + '"><input type="hidden" id="RCStatus" value="' + data.status + '"><input type="hidden" id="ClientSiteIdHover" value="' + data.clientSiteId + '"><input type="hidden" id="GuardIdHover" value="' + data.guardId + '"><input type="hidden" id="ColorIdHover" value="' + data.rcColorId + '">';
                else if (value === 1)
                    return data.status;
                //return '<i class="fa fa-check-circle text-danger"></i>' + ' [' + '<a href="#hoverModal" id="btnGreen1hover">' + 1 + '</a>' + '] <input type="hidden" id="RCStatusId" value="' + data.rcSatus + '"><input type="hidden" id="RCColortype" value="' + data.rcColor + '"><input type="hidden" id="RCStatus" value="' + data.status + '">';
                else if (value === 2)
                    return data.status;
                //  return '<i class="fa fa-check-circle text-danger "></i>' + ' [' + '<a href="#hoverModal" id="btnGreen1hover">' + 2 + '</a>' + '] <input type="hidden" id="RCStatusId" value="' + data.rcSatus + '"><input type="hidden" id="RCColortype" value="' + data.rcColor + '"><input type="hidden" id="RCStatus" value="' + data.status + '">';
                else
                    return data.status;
                //return '<i class="fa fa-check-circle text-danger "></i>' + ' [' + '<a href="#hoverModal" id="btnGreen1hover">' + 3 + '</a>' + '] <input type="hidden" id="RCStatusId" value="' + data.rcSatus + '"><input type="hidden" id="RCColortype" value="' + data.rcColor + '"><input type="hidden" id="RCStatus" value="' + data.status + '">';
            }
        },
        {
            targets: -1,
            data: null,
            width: '5%',
            defaultContent: '',
            render: function (value, type, data) {

                return '<button name="btnRadioCheckStatusActive" class="btn btn-outline-primary">Radio Check</button>';

            }
        },






        {
            data: 'siteName',
            visible: false,
            width: '20%',

        },



    ],

    preDrawCallback: function (settings) {
        scrollPosition = $(".dataTables_scrollBody").scrollTop();
    },
    drawCallback: function () {
        $(".dataTables_scrollBody").scrollTop(scrollPosition);
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



$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnUpArrow', function () {


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

$('#clientSiteActiveGuardsSinglePage').on('click', 'button[name="btnRadioCheckStatusActive"]', function () {
    var data = clientSiteActiveGuardsSinglePage.row($(this).parents('tr')).data();
    var rowClientSiteId = data.clientSiteId;
    var rowGuardId = data.guardId;
    var rcSatus = data.rcStatus;
    $("#selectRadioStatusActive").val(rcSatus);
    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#selectRadioCheckStatusActive').modal('show');
    isPaused = true;
});


$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnLogBookDetailsByGuard', function (value, record) {
    $('#guardLogBookInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader').text(GuardName + '-' + 'Log Book Details');
    clientSiteActiveGuardsLogBookDetails.ajax.reload();

});

$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnKeyVehicleDetailsByGuard', function (value, record) {
    $('#guardKeyVehicleInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    if (GuardId.length == 0) {
        GuardId = $(this).closest("tr").find('td').eq(2).find('#GuardId').val();
    }
    if (ClientSiteId.length == 0) {
        ClientSiteId = $(this).closest("tr").find('td').eq(2).find('#ClientSiteId').val();
    }
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader1').text(GuardName + '-' + 'Key Vehicle Log Details');
    clientSiteActiveGuardsKeyVehicleDetails.ajax.reload();

});


$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnIncidentReportdetails', function (value, record) {
    $('#guardIncidentReportsInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    if (GuardId.length == 0) {
        GuardId = $(this).closest("tr").find('td').eq(2).find('#GuardId').val();
    }
    if (ClientSiteId.length == 0) {
        ClientSiteId = $(this).closest("tr").find('td').eq(2).find('#ClientSiteId').val();
    }

    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader2').text(GuardName + '-' + 'Incident Report Details');
    clientSiteActiveGuardsIncidentReportsDetails.ajax.reload();

});

$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnSWdetails', function (value, record) {
    $('#guardSWInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    if (GuardId.length == 0) {
        GuardId = $(this).closest("tr").find('td').eq(2).find('#GuardId').val();
    }
    if (ClientSiteId.length == 0) {
        ClientSiteId = $(this).closest("tr").find('td').eq(2).find('#ClientSiteId').val();
    }

    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader4').text(GuardName + '-' + 'Smart Wand Scan Details');
    clientSiteActiveGuardsSWDetails.ajax.reload();

});

let isActive = true;
$('#heading-example').on('click', function () {

    if (isActive) {
        console.log($(window).height());
        var container = $('#clientSiteActiveGuards').closest('.dataTables_scrollBody');
        /* for modifying the size of tables active  guards - start*/
        var count = $('#clientSiteActiveGuards tbody tr').length;
        if (count > 10) {
            container.css('height', ($(window).height() - 100));

        }
        else {

            container.css('height', '100%');


        }
       /* for modifying the size of tables active  guards - end*/
    }
    else {
        console.log($(window).height());
        var container = $('#clientSiteActiveGuards').closest('.dataTables_scrollBody');
        /*for modifying the size of tables active  guards - start*/
        var count = $('#clientSiteActiveGuards tbody tr').length;
        if (count > 10) {
            container.css('height', ($(window).height() - 300));

        }
        else {

            container.css('height', '100%');


        }
       /* for modifying the size of tables active  guards - end*/

    }
    // Toggle the state
    isActive = !isActive;
});
let isActiveTwo = true;
$('#heading-example2').on('click', function () {

    if (isActiveTwo) {
        console.log($(window).height());
        var container = $('#clientSiteInActiveGuards').closest('.dataTables_scrollBody');
        container.css('height', ($(window).height() - 100));
    }
    else {
        console.log($(window).height());


        var container = $('#clientSiteInActiveGuards').closest('.dataTables_scrollBody');
        /*for modifying the size of tables   inactive guards - start*/
        var count = $('#clientSiteInActiveGuards tbody tr').length;
        if (count > 10) {
            container.css('height', ($(window).height() - 300));

        }
        else {

            container.css('height', '100%');


        }
        /*for modifying the size of tables   inactive guards - end*/


    }
    // Toggle the state
    isActiveTwo = !isActiveTwo;
});



/* Single page Grid end  */

/*functions for settings-start*/
let gridRadioCheckStatusTypeSettings;
function settingsButtonRenderer(value, record) {
    return '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#kpi-settings-modal" ' +
        'data-cs-id="' + record.id + '" data-cs-name="' + record.clientSiteName + '"><i class="fa fa-pencil mr-2"></i>Edit</button>';
}

gridRadioCheckStatusTypeSettings = $('#radiocheck_status_type_settings').grid({
   dataSource: '/Admin/Settings?handler=RadioCheckStatusWithOutcome',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },
    columns: [
        { width: 130, field: 'referenceNo', title: 'Reference No' },
        { width: 500, field: 'name', title: 'Name', editor: true },
        { width: 200, field: 'radioCheckStatusColorName', title: 'Outcome', type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=RadioCheckStatusColorCode', valueField: 'name', textField: 'name' } },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

if (gridRadioCheckStatusTypeSettings) {
    gridRadioCheckStatusTypeSettings.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        //data.radioCheckStatusColorId = !Number.isInteger(data.radioCheckStatusColorId) ? data.radioCheckStatusColorId.getValue() : data.radioCheckStatusColorId;
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=RadioCheckStatus',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            gridRadioCheckStatusTypeSettings.clear();
            gridRadioCheckStatusTypeSettings.reload();
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isRadionCheckStatusAdding)
                isRadionCheckStatusAdding = false;
        });
    });

    gridRadioCheckStatusTypeSettings.on('rowRemoving', function (e, id, record) {
     

        if (confirm('Are you sure want to delete this radio check status?')) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteRadioCheckStatus',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridRadioCheckStatusTypeSettings.reload();
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isRadionCheckStatusAdding)
                    isRadionCheckStatusAdding = false;
            });
        }
    });
}
let isRadionCheckStatusAdding = false;
$('#add_radiocheck_status').on('click', function () {
  

    if (isRadionCheckStatusAdding==true) {
        alert('Unsaved changes in the grid. Refresh the page');
    } else {
        isRadionCheckStatusAdding = true;
        gridRadioCheckStatusTypeSettings.addRow({
            'id': -1
        }).edit(-1);
    }
});
//hover display tooltip-start




let clientSiteRadiostatusDetailsHover = $('#clientSiteRadiostatusDetailsHover').DataTable({
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
        url: '/RadioCheckV2?handler=ClientSiteRadiocheckStatus',
        datatype: 'json',
        data: function (d) {
        d.clientSiteId = $('#ClientSiteIdHover').val();
         d.guardId = $('#GuardIdHover').val();
         d.ColorId=$('#ColorIdHover').val();
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
            visible: false,

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


$('#clientSiteActiveGuards tbody').on('click', '#btnGreen1hover', function (value, record) {
    $($.fn.dataTable.tables(true)).DataTable()
        .columns.adjust();
    var ColorName = $(this).closest("tr").find("td").eq(5).find('#RCColortype').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();

    $('#ClientSiteIdHover').val(ClientSiteId);
    $('#GuardIdHover').val(GuardId);
    //d$('#ColorIdHover').val();

    $('#lblColorType').val(ColorName);


    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    $('#lbl_GuardActivityHeaderHover').text(GuardName + '-' + 'Log Book Details');
    clientSiteRadiostatusDetailsHover.ajax.reload();
    $('#hoverModal').modal('show');
    isPaused = true;
});

$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnGreen1hover', function (value, record) {
    $($.fn.dataTable.tables(true)).DataTable()
        .columns.adjust();
    var ColorName = $(this).closest("tr").find("td").eq(5).find('#RCColortype').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    var GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();

    $('#ClientSiteIdHover').val(ClientSiteId);
    $('#GuardIdHover').val(GuardId);
    //d$('#ColorIdHover').val();

    $('#lblColorType').val(ColorName);


    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    $('#lbl_GuardActivityHeaderHover').text(GuardName + '-' + 'Log Book Details');
    clientSiteRadiostatusDetailsHover.ajax.reload();
    $('#hoverModal').modal('show');
    isPaused = true;
});

$("#guardKeyVehicleInfoModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#guardIncidentReportsInfoModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#guardLogBookInfoModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#selectRadioCheckStatus").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#selectRadioCheckStatusActive").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#pushNoTificationsControlRoomModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#hoverModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#guardInfoModal").on("hidden.bs.modal", function () {
    isPaused = false;
});



//hover display tooltip-end

// Task p6#73_TimeZone issue -- added by Binoy - Start
function fillRefreshLocalTimeZoneDetails(formData, modelname, isform) {
    // for reference https://moment.github.io/luxon/#/
    var DateTime = luxon.DateTime;
    var dt1 = DateTime.local();
    let tz = dt1.zoneName + ' ' + dt1.offsetNameShort;
    let diffTZ = dt1.offset
    //let tzshrtnm = dt1.offsetNameShort;
    let tzshrtnm = 'GMT' + dt1.toFormat('ZZ'); // Modified by binoy on 19-01-2024

    const eventDateTimeLocal = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS');
    const eventDateTimeLocalWithOffset = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS Z');
    if (isform) {
        formData.append(modelname + ".EventDateTimeLocal", eventDateTimeLocal);
        formData.append(modelname + ".EventDateTimeLocalWithOffset", eventDateTimeLocalWithOffset);
        formData.append(modelname + ".EventDateTimeZone", tz);
        formData.append(modelname + ".EventDateTimeZoneShort", tzshrtnm);
        formData.append(modelname + ".EventDateTimeUtcOffsetMinute", diffTZ);
    }
    else {
        formData.EventDateTimeLocal = eventDateTimeLocal;
        formData.EventDateTimeLocalWithOffset = eventDateTimeLocalWithOffset;
        formData.EventDateTimeZone = tz;
        formData.EventDateTimeZoneShort = tzshrtnm;
        formData.EventDateTimeUtcOffsetMinute = diffTZ;
    }
}

    // Task p6#73_TimeZone issue -- added by Binoy - End

