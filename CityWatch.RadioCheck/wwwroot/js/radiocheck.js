let nIntervId;
let DuressAlarmNotificationPending = false;
const duration = 60 * 3;
var isPaused = false;
var sitebuttonSelectedClientTypeSiteId = -1;
var sitebuttonSelectedClientSiteId = -1;
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
let connection;
window.onload = function () {
    if (document.querySelector('#clockRefresh')) {
        startClock();
    }
    if (document.querySelector('#txtSignalRConnectionUrl')) {
        connectToSignalRservice();
    }
};
$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();

    $(document).on('show.bs.modal', '.modal', function () {
        const zIndex = 1040 + 10 * $('.modal:visible').length;
        $(this).css('z-index', zIndex);
        setTimeout(() => $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack'));
    });

    $(document).on('hidden.bs.modal', '.modal', () => $('.modal:visible').length && $(document.body).addClass('modal-open'));

    localStorage.removeItem('activeTab');

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
                    ClearTimerAndReload();
                }
                else if ((timer > 5) && DuressAlarmNotificationPending) {
                    ClearTimerAndReload();
                }
            }
        }, 1000);
    }
}

function connectToSignalRservice() {
    var signalRconnHuburl = $('#txtSignalRConnectionUrl').val() + "/updateHub";
    connection = new signalR.HubConnectionBuilder().withUrl(signalRconnHuburl).configureLogging(signalR.LogLevel.Information).build();
    connection.on("ReceiveDuressAlarmAlert", function () {
        console.log("Duress Alarm Alert Received");
        DuressAlarmNotificationPending = true;
    });

    connection.start().then(function () {
        console.log('SignalR connection established.');
    }).catch(function (err) {
        return console.error(err.toString());
    });
}

$('#btnRefreshActivityStatus').on('click', function () {
    ClearTimerAndReload();
});

function ClearTimerAndReload() {
    clearInterval(nIntervId);
    DuressAlarmNotificationPending = false;
    nIntervId = null;
    location.reload();
}



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
    var nottifcationType = data.notificationType;
    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#nottifcationType').val(nottifcationType);
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
const groupColumnSortAlias = 11; // Task p4#41_A~Z and Z~A sorting issue -- added by Binoy - 31-01-2024
//var clickedrow=0;
var scrollPosition2;
var rowIndex2;
//var scrollY = ($(window).height() - 300);
let clientSiteActiveGuards = $('#clientSiteActiveGuards').DataTable({

    dom: 'Bfrtip',
    buttons: [


        
        {
            text: '<i class="fa fa-microphone" aria-hidden="true"></i>',
            titleAttr: 'Start Recoding',
            className: 'btn btn-md mr-2 btn-custom',
            name: 'audioStart',
            action: function (e, dt, node, config) {
                clickstarAudio();
                clientSiteActiveGuards.buttons('audioStart:name').disable();
                clientSiteActiveGuards.buttons('audioStop:name').enable();
                
                clientSiteInActiveGuards.buttons('audioStart:name').disable();
                clientSiteInActiveGuards.buttons('audioStop:name').enable();
            }
        },
        {
            text: '<i class="fa fa-microphone-slash" aria-hidden="true"></i>',
            titleAttr: 'Stop Recoding',
            className: 'btn btn-md mr-2 btn-custom',
            enabled: false,
            name: 'audioStop',
            action: function () {
                clickStopAudio();
                clientSiteActiveGuards.buttons('audioStop:name').disable();
                clientSiteActiveGuards.buttons('audioStart:name').enable();

                clientSiteInActiveGuards.buttons('audioStop:name').disable();
                clientSiteInActiveGuards.buttons('audioStart:name').enable();
            }
        },
        /*
        {
            text: '<i class="fa fa-play" aria-hidden="true"></i>',
            className: 'btn btn-md mr-2 btn-custom',
            titleAttr: 'play Audio',
            name: 'playaudio',
            action: function (e, dt, node, config) {
                // Get the audio element
                var audioElement = document.getElementById('audioPlayback');
                if (audioElement.getAttribute('src') === null || audioElement.getAttribute('src') === "") {
                    alert('No recording found'); // Alert user if no source is found
                } else {
                    // Check if the audio is already playing
                    if (audioElement.paused) {
                        audioElement.play(); // Play the audio
                        $(node).html('<i class="fa fa-pause" aria-hidden="true"></i>'); // Change button icon to 'Pause'
                    } else {
                        audioElement.pause(); // Pause the audio
                        $(node).html('<i class="fa fa-play" aria-hidden="true"></i>'); // Change button icon to 'Play'
                    }
                }

                // Add an event listener to handle when the audio ends
                audioElement.onended = function () {
                    $(node).html('<i class="fa fa-play" aria-hidden="true"></i>'); // Change button icon back to 'Play' when audio ends
                };
            }
        },*/
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',
            
        },
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
            text: '<i class="fa fa-print"></i>', // Custom button text
            className: 'btn btn-md mr-2 btn-print',
            titleAttr: 'Print',
            customize: function (win) {
                // Customize the print window/document here
                $(win.document.body).css('font-size', '10pt');
                $(win.document.body).find('table')
                    .addClass('display')
                    .css('font-size', 'inherit');

                // Add a landscape orientation
                var css = '@page { size: landscape; }';
                var head = win.document.head || win.document.getElementsByTagName('head')[0];
                var style = win.document.createElement('style');

                style.type = 'text/css';
                style.media = 'print';
                if (style.styleSheet) {
                    style.styleSheet.cssText = css;
                } else {
                    style.appendChild(win.document.createTextNode(css));
                }

                head.appendChild(style);
            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            
            text: '<img src="/images/guards.png" alt="Image" height="16" width="16">',
            titleAttr: '',
            className: 'btn btn-md mr-2 btn-guardlogin',
            action: function () {
                getLoggedGuardDetails();

            }

        },
        {
            extend: 'pdf',
            text: '<i class="fa fa-globe"></i>',
            titleAttr: 'Globe Map',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                // Redirect to the new page with query parameters
                const clientSiteIds = 'test'; // Replace this with dynamic values if needed
                const newPageUrl = '/GlobeMap';
                window.open(newPageUrl, '_blank');
            }
        },
        {
            extend: 'pdf',
            text: '<img src="/images/mail.png" alt="Image" height="16" width="16">',
            titleAttr: 'Webmail Program',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {

                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.webmail;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });



            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            extend: 'pdf',
            text: '<img src="/images/tv.png" alt="Image" height="16" width="16">',
            titleAttr: 'TV News Feed',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.tvNewsFeed;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });
            }
        },
        {
            extend: 'pdf',
            text: '<img src="/images/cloudy.png" alt="Image" height="16" width="16">',
            titleAttr: 'Weather Feed',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.wetherFeed;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });
            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            text: '<img src="/images/man-climbing-stairs.png" alt="Image" height="16" width="16">',
            titleAttr: 'Steps',
            className: 'btn btn-md mr-2 btn-custom',
            action: function () {
                $('#inpCallingFunction').val('STEPBUTTON'); // Setting calling function to step button
                $('#pushNoTificationsControlRoomModal').modal('show');

            }
        }

    ],

   


    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 },// Hide the group column initially
        { "visible": false, "targets": 2 }
    ],
    order: [[11, 'asc']], // Task p4#41_A~Z and Z~A sorting issue -- modified by Binoy - 31-01-2024
    info: false,
    searching: true,
    autoWidth: true,
    fixedHeader: true,
    "scrollY": ($(window).height() - 300),
    "paging": false,
    "footer": true,
    "scrollCollapse": true,
    "scroller": true, // Task p4#19 Screen Jumping day -- added by Binoy -- Start - 01-02-2024
    "stateSave": true,// Task p4#19 Screen Jumping day -- added by Binoy -- End - 01-02-2024
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
            width: '15%',
            orderable: false, // Task p4#41_A~Z and Z~A sorting issue -- added by Binoy - 31-01-2024
            render: function (value, type, data) {
                if (data.isEnabled === 1) {
                    return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                        '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>' +
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
                if (value != 0)
                    return '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardLogBookInfoModal" id="btnLogBookDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
                else
                    return '<button type="button" class="btn" id="btnLogBookHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
            }
        },
        {
            data: 'keyVehicle',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                if (value != 0)
                    return '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardKeyVehicleInfoModal" id="btnKeyVehicleDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
                else
                    return '<button type="button" class="btn" id="btnKvHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
            }
        },
        {
            data: 'incidentReport',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                if (value != 0)
                    return '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardIncidentReportsInfoModal" id="btnIncidentReportdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
                else
                    return '<button type="button" class="btn" id="btnIrHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
            }
        },
        {
            data: 'smartWands',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                if (data.hasmartwand !== 0) {
                    if (value != 0)
                        return '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardSWInfoModal" id="btnSWdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
                    else
                        return '<button type="button" class="btn" id="btnSwHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
                }
                else {
                    /* return '<button type="button" class="btn" id="btnSwHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';*/
                    return '<i class="fa fa-times-circle text-text-muted rc-client-status" style="color:#B8B8B8"></i>';
                }
            }
        },
        {
            data: 'latestDate',
            width: '2%',
            className: "text-center",
            render: function (value, type, data) {

                if (data.rcColorId != 1) {

                    if (value < 80)
                        return '<div class="p-1 mb-1" style="background: #AFE1AF;">' + value + '</div>';
                    if (value >= 80)
                        return '<div class="p-1 mb-1" style="background: #FFD580;">' + value + '</div>';

                }
                else {
                    return '<div class="p-1 mb-1" style="background:  #A9A9A9;">' + '00' + '</div>';
                    //return '<div class="p-1 mb-1" style="background:  #A9A9A9;">' + value + '</div>';
                }
                //return value;
            }

        },
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
        // Task p4#41_A~Z and Z~A sorting issue -- added by Binoy -- Start - 31-01-2024
        {
            data: 'onlySiteName',
            visible: false,
            width: '20%',

        },
        {
            data: 'hR1',
            width: '3%',
            className: "text-center",

            render: function (value, type, data) {
                
                if ($('#txtguardGuardRCAccess').val() == 'False' || $('#txtguardGuardRCAccess').val()=='') {


                    // Include the sort value as a hidden element and render the color circle
                    return '<span style="display:none;">' + value + '</span>' +
                        '<i class="fa fa-circle text-' +
                        (value == 'Green' ? 'success' : value == 'Red' ? 'danger' :
                            value == 'Yellow' ? 'warning' : 'muted') +
                        '"></i>';
                }
                else {
                    return '<i class="fa fa-circle text-muted"></i>';
                }
            }
        },
        {
            data: 'hR2',
            width: '3%',
            className: "text-center",

            render: function (value, type, data) {
               
                if ($('#txtguardGuardRCAccess').val() == 'False' || $('#txtguardGuardRCAccess').val() == '') {

                return '<span style="display:none;">' + value + '</span>' +
                    '<i class="fa fa-circle text-' +
                    (value == 'Green' ? 'success' : value == 'Red' ? 'danger' :
                        value == 'Yellow' ? 'warning' : 'muted') +
                        '"></i>';
                }
                else {
                    return '<i class="fa fa-circle text-muted"></i>';
                }

            }
        },
        {
            data: 'hR3',
            width: '3%',
            className: "text-center",

            render: function (value, type, data) {
              
                 if ($('#txtguardGuardRCAccess').val() == 'False' || $('#txtguardGuardRCAccess').val()=='') {

                return '<span style="display:none;">' + value + '</span>' +
                    '<i class="fa fa-circle text-' +
                    (value == 'Green' ? 'success' : value == 'Red' ? 'danger' :
                        value == 'Yellow' ? 'warning' : 'muted') +
                    '"></i>';
                }
                else {
                    return '<i class="fa fa-circle text-muted"></i>';
                }
            }
        },
       
        // Task p4#41_A~Z and Z~A sorting issue -- added by Binoy -- End - 31-01-2024
    ],

    preDrawCallback: function (settings) {
        clientSiteActiveGuardsscrollPosition = $('#clientSiteActiveGuards').closest('div.dataTables_scrollBody').scrollTop();
    },
    drawCallback: function () {
        $('#clientSiteActiveGuards').closest('div.dataTables_scrollBody').scrollTop(clientSiteActiveGuardsscrollPosition);

        /*for modifying the size of tables active  guards - start*/
        var count = $('#clientSiteActiveGuards tbody tr').length;
        if (count > 10) {
            $('#clientSiteActiveGuards').closest('.dataTables_scrollBody').css('height', ($(window).height() - 300));

        }
        else {

            $('#clientSiteActiveGuards').closest('.dataTables_scrollBody').css('height', '100%');

        }
        $('#clientSiteActiveGuards').closest('div.dataTables_scrollBody').css('overflow-x', 'hidden'); //Remove the x scrollbar

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



// Order by the grouping
// Task p4#41_A~Z and Z~A sorting issue -- added by Binoy -- Start - 31-01-2024
$(clientSiteActiveGuards.table().header()).on('click', 'th', function () {
    // Checkout issue on https://datatables.net/reference/api/table().header()  , https://datatables.net/forums/discussion/43165/click-event-in-column-header-never-fired    
    var index = clientSiteActiveGuards.column(this).index();
    var currentOrder = clientSiteActiveGuards.order()[0];
    if (index === 3) {
        if (currentOrder[1] === 'asc') {
            clientSiteActiveGuards.order([groupColumnSortAlias, 'desc']).draw();
        }

        else {
            clientSiteActiveGuards.order([groupColumnSortAlias, 'asc']).draw();
        }
    }
});
// Task p4#41_A~Z and Z~A sorting issue -- added by Binoy -- End - 31-01-2024

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
var clientSiteActiveGuardsscrollPosition;
var rowIndex;

let clientSiteInActiveGuards = $('#clientSiteInActiveGuards').DataTable({
    dom: 'Bfrtip',
    buttons: [


        {
            text: '<i class="fa fa-microphone" aria-hidden="true"></i>',
            titleAttr: 'Start Recoding',
            className: 'btn btn-md mr-2 btn-custom',
            name: 'audioStart',
            action: function (e, dt, node, config) {
                clickstarAudio();
                clientSiteInActiveGuards.buttons('audioStart:name').disable();
                clientSiteInActiveGuards.buttons('audioStop:name').enable();
                clientSiteActiveGuards.buttons('audioStart:name').disable();
                clientSiteActiveGuards.buttons('audioStop:name').enable();

            }
        },
        {
            text: '<i class="fa fa-microphone-slash" aria-hidden="true"></i>',
            titleAttr: 'Stop Recoding',
            className: 'btn btn-md mr-2 btn-custom',
            enabled: false,
            name: 'audioStop',
            action: function () {
                clickStopAudio();
                clientSiteInActiveGuards.buttons('audioStop:name').disable();
                clientSiteInActiveGuards.buttons('audioStart:name').enable();

                clientSiteActiveGuards.buttons('audioStop:name').disable();
                clientSiteActiveGuards.buttons('audioStart:name').enable();

            }
        },


        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },

       /* {
            text: '<i class="fa fa-play" aria-hidden="true"></i>',
            className: 'btn btn-md mr-2 btn-custom',
            titleAttr: 'play Audio',
            name: 'playaudio',
            action: function (e, dt, node, config) {
                // Get the audio element
                var audioElement = document.getElementById('audioPlayback');
                if (audioElement.getAttribute('src') === null || audioElement.getAttribute('src') === "") {
                    alert('No recording found'); // Alert user if no source is found
                } else {
                    // Check if the audio is already playing
                    if (audioElement.paused) {
                        audioElement.play(); // Play the audio
                        $(node).html('<i class="fa fa-pause" aria-hidden="true"></i>'); // Change button icon to 'Pause'
                    } else {
                        audioElement.pause(); // Pause the audio
                        $(node).html('<i class="fa fa-play" aria-hidden="true"></i>'); // Change button icon to 'Play'
                    }
                }

                // Add an event listener to handle when the audio ends
                audioElement.onended = function () {
                    $(node).html('<i class="fa fa-play" aria-hidden="true"></i>'); // Change button icon back to 'Play' when audio ends
                };
            }
        },*/

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
            text: '<i class="fa fa-print"></i>', // Custom button text
            className: 'btn btn-md mr-2 btn-print',
            titleAttr: 'Print',
            customize: function (win) {
                // Customize the print window/document here
                $(win.document.body).css('font-size', '10pt');
                $(win.document.body).find('table')
                    .addClass('display')
                    .css('font-size', 'inherit');

                // Add a landscape orientation
                var css = '@page { size: landscape; }';
                var head = win.document.head || win.document.getElementsByTagName('head')[0];
                var style = win.document.createElement('style');

                style.type = 'text/css';
                style.media = 'print';
                if (style.styleSheet) {
                    style.styleSheet.cssText = css;
                } else {
                    style.appendChild(win.document.createTextNode(css));
                }

                head.appendChild(style);
            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            
            text: '<img src="/images/guards.png" alt="Image" height="16" width="16">',
            titleAttr: '',
            className: 'btn btn-md mr-2 btn-guardlogin',
            action: function () {
                getLoggedGuardDetails();
               

            }

        },
        {
            extend: 'pdf',
            text: '<i class="fa fa-globe"></i>',
            titleAttr: 'Globe Map',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                // Redirect to the new page with query parameters
                const clientSiteIds = 'test'; // Replace this with dynamic values if needed
                const newPageUrl = '/GlobeMapNoActivity';
                window.open(newPageUrl, '_blank');
            }
        },
        {
            extend: 'pdf',
            text: '<img src="/images/mail.png" alt="Image" height="16" width="16">',
            titleAttr: 'Webmail Program',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
               
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.webmail;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');
                    
                });
                
                
               
            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            extend: 'pdf',
            text: '<img src="/images/tv.png" alt="Image" height="16" width="16">',
            titleAttr: 'TV News Feed',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.tvNewsFeed;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });
            }
        },
        {
            extend: 'pdf',
            text: '<img src="/images/cloudy.png" alt="Image" height="16" width="16">',
            titleAttr: 'Weather Feed',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.wetherFeed;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });
            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            text: '<img src="/images/man-climbing-stairs.png" alt="Image" height="16" width="16">',
            titleAttr: 'Steps',
            className: 'btn btn-md mr-2 btn-custom',
            action: function () {
                $('#inpCallingFunction').val('STEPBUTTON'); // Setting calling function to step button
                $('#pushNoTificationsControlRoomModal').modal('show');

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
    "scroller": true, // Task p4#19 Screen Jumping day -- added by Binoy -- End - 01-02-2024
    "stateSave": true, // Task p4#19 Screen Jumping day -- added by Binoy -- End - 01-02-2024
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
            /*width: '20%',*/
            class: 'dt-control',
            render: function (value, type, data) {

                return '<tr class="group group-start "><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
            }
        },
        {
            data: 'address',
            visible: false,
            render: function (value, type, data) {

                return '<tr class="group group-start sho"><td class="' + (groupColumn2 == '2' ? 'bg-danger' : (groupColumn2 == '0' ? 'bg-danger' : 'bg-danger')) + ' " colspan="5">' + groupColumn2 + '</td></tr>';
            }
        },

        {
            data: 'guardName',

            width: '27%',
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
                            '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>' +
                            '&nbsp;&nbsp;&nbsp;<a href="https://www.google.com/maps?q=' + data.gpsCoordinates + '" target="_blank" data-toggle="tooltip" data-placement="right" title="' + data.enabledAddress + '"><i class="fa fa-map-marker" aria-hidden="true"></i></a>';
                    }

                }
                else {
                    return '&nbsp;&nbsp;&nbsp;<i class="fa fa-user" aria-hidden="true" style="color:#FF0000;"></i> ' + data.guardName;

                }

            }
        },

        {
            data: 'guardLoginTime',
            width: '11%',
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
            width: '3%',
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
            width: '3%',
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
            width: '7%',
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
        },

    ],

    preDrawCallback: function (settings) {
        scrollPosition = $('#clientSiteInActiveGuards').closest("div.dataTables_scrollBody").scrollTop();
    },
    drawCallback: function () {

        /* Retain the Scroll position*/
        $('#clientSiteInActiveGuards').closest("div.dataTables_scrollBody").scrollTop(scrollPosition);
        /*for modifying the size of tables   inactive guards - start*/
        var count = $('#clientSiteInActiveGuards tbody tr').length;
        if (count > 10) {
            $('#clientSiteInActiveGuards').closest('div.dataTables_scrollBody').css('height', ($(window).height() - 200));
        }
        else {
            $('#clientSiteInActiveGuards').closest('div.dataTables_scrollBody').css('height', '100%');
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

        $('#clientSiteInActiveGuards').closest('div.dataTables_scrollBody').css('overflow-x', 'hidden'); //Remove the x scrollbar

        PlayDuressAlarm();
    },


});

function stripHtml(input) {
    const doc = new DOMParser().parseFromString(input, 'text/html');
    return doc.body.textContent || "";
}

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

// To fix the Datatable column header issue when hidden inside tab
$('a[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
    $($.fn.dataTable.tables(true)).DataTable().columns.adjust();
});

// To fix the Datatable column header issue when hidden inside accordion
$('.collapse').on('shown.bs.collapse', function (e) {
    $($.fn.dataTable.tables(true)).DataTable().columns.adjust();
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
    $('#lbl_guard_email').html('');
    $('#lbl_guard_mobile').html('');
    $('#lbl_guard_provider').html('');
    $('#lbl_guard_provider_no').html('');
    $('#lbl_guard_gender').html('');
    $('#lbl_guard_lotes').html('');

    const button = $(event.relatedTarget);
    const id = button.data('id');

    $.ajax({
        url: '/RadioCheckV2?handler=GuardData',
        data: { id: id },
        type: 'GET',
    }).done(function (result) {
        $('#lbl_guard_name').html('&nbsp;&nbsp;&nbsp;' + result.name);
        $('#lbl_guard_security_no').html('&nbsp;&nbsp;&nbsp;' + result.securityNo);
        $('#lbl_guard_state').html('&nbsp;&nbsp;&nbsp;' + result.state);
        $('#lbl_guard_gender').html('&nbsp;&nbsp;&nbsp;' + result.gender);
        $('#lbl_guard_email').html('&nbsp;&nbsp;&nbsp;' + result.email);
        $('#lbl_guard_mobile').html('&nbsp;&nbsp;&nbsp;' + result.mobile);
        var provider = result.provider ? result.provider : '';

        $('#lbl_guard_provider').html('&nbsp;&nbsp;&nbsp;' + provider);
        $('#lbl_guard_provider_no').html('&nbsp;&nbsp;&nbsp;' + result.providerNo);

        if (result.provider !== null && result.provider !== '') {
            var providerHtml = '&nbsp;&nbsp;&nbsp;' + result.provider +
                '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#crmSupplierDetailsModal" data-id="' + result.provider + '"></i>';
            $('#lbl_guard_provider').html(providerHtml);
        }
        /*p4-105 new button-start*/
        //var guardDetailsHtml = '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#specificGuardDetailsModal" data-id="' + result.id + '"></i>';
        var guardDetailsHtml = '<a href = "/GuardDetails?gId=' + result.id +'" target="_blank"><i class="fa fa-file-text fa-lg pl-5"></i></a>';

        $('#lbl_GuardDetailedDetails').html(guardDetailsHtml)
        $('#lbl_guard_lotes').html('&nbsp;&nbsp;&nbsp;' + result.languages);
        

        /*p4-105 new button-end*/
    }).fail(function () {
        alert("An error occurred while fetching guard details.");
    });


});
const renderGuardInitialColumn = function (value, record, $cell, $displayEl) {
    if (record.guardId !== null) {
        return value + '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#guardInfoModal" data-id="' + record.guardId + '"></i>';
    }
    else return value;
}
/* P4#70 to show crm details in guard details-start*/

$('#crmSupplierDetailsModal').on('shown.bs.modal', function (event) {
    isPaused = true;
    $('#lbl_company_name').html('');
    $('#lbl_abn').html('');
    $('#lbl_landline').html('');
    $('#lbl_email').html('');
    $('#lbl_website').html('');


    const button = $(event.relatedTarget);

    const compName = button.data('id');


    $.ajax({
        url: '/GuardDetails?handler=CrmSupplierData',
        data: { companyName: compName },
        type: 'GET',
    }).done(function (result) {
        if (result) {
            $('#lbl_company_name').html('&nbsp;' + result.companyName);
            $('#lbl_abn').html('&nbsp;' + result.companyABN);
            $('#lbl_landline').html('&nbsp;' + result.companyLandline);
            $('#lbl_email').html('&nbsp;' + result.email);
            $('#lbl_website').html('&nbsp;' + result.website);
        }
    });
});
const renderCrmGuardInitialColumn = function (value, record, $cell, $displayEl) {
    if (record.companyName !== null) {
        return value + '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#crmSupplierDetailsModal" data-id="' + record.provider + '"></i>';
    }
    else return value;
}

/* P4#70 to show crm details in guard details-end*/

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
            width: '100%',
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
        {
            data: 'gpsCoordinates', width: '9%',
            className: "text-center", render: function (value, type, data) {
                //return record.guardLogin ? record.guardLogin.guard.initial : '';
                return `${data.gpsCoordinates ? `<a href="https://www.google.com/maps?q=${data.gpsCoordinates}" target="_blank" data-toggle="tooltip" title=""><i class="fa fa-map-marker" aria-hidden="true"></i></a>` : ''}`;
            }
        }

    ],
    drawCallback: function () {
        $('#clientSiteActiveGuardsLogBookDetails').closest('div.dataTables_scrollBody').css('overflow-x', 'hidden'); //Remove the x scrollbar
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
        $('#clientSiteActiveGuardsKeyVehicleDetails').closest('div.dataTables_scrollBody').css('overflow-x', 'hidden'); //Remove the x scrollbar
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
    var GuardId = $(this).closest("tr").find('td').eq(2).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(2).find('#ClientSiteId').val();
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
        $('#clientSiteActiveGuardsIncidentReportsDetails').closest('div.dataTables_scrollBody').css('overflow-x', 'hidden'); //Remove the x scrollbar
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


let clientSiteActiveGuardsLastIncidentReportsDetails = $('#clientSiteActiveGuardsLastIncidentReportsDetails').DataTable({
    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 } // Hide the group column initially
    ],
    order: [[groupColumn, 'asc']],
    info: false,
    searching: false,
    autoWidth: false,
    fixedHeader: true,
    "scrollY": "150px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": false,
    "header":false,
    ajax: {
        url: '/ActiveGuardSinglePage?handler=ClientSiteLastIncidentReportActivityStatus',
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
            data: 'clientSite.name',
            width: '20%',
            render: function (value, type, data) {

                return '<tr class="group group-start"><td class="' + (groupColumn == '1' ? 'bg-danger' : (groupColumn == '0' ? 'bg-danger' : 'bg-danger')) + '" colspan="5">' + groupColumn + '</td></tr>';
            }
        },
        {
            data: 'irId',
            width: '10%',
            visible: false

        },
        {
            data: 'incidentReport.fileName',
            width: '20%'

        },

        {
            data: 'activityDescription',
            width: '9%',
            className: "text-center",

        },
        {
            data: 'lastIRCreatedTime',
            width: '9%',
            className: "text-center",
            render: function (value, type, data) {

                // Convert the date string to a JavaScript Date object
                var date = new Date(data.incidentReport.createdOnDateTimeLocal);

                // Format the date to display only the date part without the time
                var formattedDate = date.toLocaleDateString('en-GB', {
                    //day: '2-digit',
                    //month: '2-digit',
                    //year: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit'
                });
                var additionalData = data.incidentReport.createdOnDateTimeZoneShort;
                if (additionalData != null) {
                    return formattedDate + '(' + additionalData + ')';
                } else {
                    return formattedDate;
                }

            }
        },


    ],
    drawCallback: function () {
        $('#clientSiteActiveGuardsLastIncidentReportsDetails').closest('div.dataTables_scrollBody').css('overflow-x', 'hidden'); //Remove the x scrollbar
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
$("#clientSiteActiveGuardsLastIncidentReportsDetails_wrapper thead").hide();
$('#clientSiteActiveGuardsLastIncidentReportsDetails').closest('.dataTables_scrollBody').css('border-bottom', 'none');
$('#clientSiteActiveGuardsIncidentReportsDetails').closest('.dataTables_scrollBody').css('border-bottom', 'none');

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
            width: '20%',

        },
        {
            data: 'smartWand',
            width: '15%'

        },
        {
            data: 'employeePhone',
            width: '22%',


        },
        {
            data: 'inspectionStartDatetimeLocal',
            width: '25%',


        },

        {
            data: 'locationScan',
            width: '30%',


        },
    ],

    drawCallback: function () {
        $('#clientSiteActiveGuardsSWDetails').closest('div.dataTables_scrollBody').css('overflow-x', 'hidden'); //Remove the x scrollbar
        $('#clientSiteActiveGuardsSWDetails').closest('div.dataTables_scrollBody').css('border-bottom', 0);

        var api = this.api();
        var rows = api.rows({ page: 'current' }).nodes();
        var last = null;

    },

});

$('#clientSiteActiveGuards tbody').on('click', '#btnSWdetails', function (value, record) {
    $('#guardSWInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(4).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(4).find('#ClientSiteId').val();
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
    var GuardId = $(this).closest("tr").find('td').eq(3).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(3).find('#ClientSiteId').val();
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
    clientSiteActiveGuardsLastIncidentReportsDetails.ajax.reload();

});
/*for incident report details of the guard - end*/

/* for logbook history of the guard start*/
function renderLogbookDateTime(value, record) {
    if (record.eventDateTimeLocal != null && record.eventDateTimeLocal != '') {
        const date = new Date(record.eventDateTimeLocal);
        var DateTime = luxon.DateTime;
        var dt1 = DateTime.fromJSDate(date);
        var dt = dt1.toFormat('dd LLL yyyy @ HH:mm') + ' (' + record.eventDateTimeZoneShort + ')';
        return dt;
    }
    else if (value !== '') {
        const date = new Date(value);
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        let day = date.getDate();

        if (day < 10) {
            day = '0' + day;
        }
        return day + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
    }
}


let clientSiteActiveGuardsLogBookHistory = $('#clientSiteActiveGuardsLogBookHistory').DataTable({
    ordering: false,
    info: false,
    searching: false,
    autoWidth: false,
    fixedHeader: true,
    deferLoading: 57,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    "processing": true,
    'language': {
        'loadingRecords': '&nbsp;',
        'processing': 'Loading...Please wait...'
    },
    ajax: {
        url: '/RadioCheckV2?handler=ClientSitelogBookHistory',
        datatype: 'json',
        data: function (d) {
            d.clientSiteId = $('#txtClientSiteId').val();
            d.guardId = $('#txtGuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', title: 'Id', visible: false },
        {
            data: 'clientSiteLogBookId',
            width: '20%',
            title: 'LogBook Id',
            visible: false,
        },
        {
            data: 'notes',
            width: '9%',
            title: 'Notes',
            className: "text-center",
        },
        {
            data: 'eventDateTime',
            width: '9%',
            title: 'Created Time',
            className: "text-center",
            render: function (value, type, data) {
                return renderLogbookDateTime(value, data);
            }
        }
    ]
});

$('#clientSiteActiveGuards tbody').on('dblclick', '#btnLogBookHistoryByGuard', function (value, record) {
    $('#guardLogBookHistoryModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).attr("data-guardid");
    var ClientSiteId = $(this).attr('data-clientsiteid');
    var ClientSiteName = $(this).attr('data-clientsitename');
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    $('#lbl_GuardLogBookHistoryModalHeader').text(GuardName);
    $('#lbl_lb_History_SitenameInfo').text('Last log book log');
    clientSiteActiveGuardsLogBookHistory.clear().draw();
    clientSiteActiveGuardsLogBookHistory.ajax.reload();
});
/* for logbook history of the guard end*/


/* for Key Vehcile history of the guard start*/
function renderKvDateTime(value, record) {
    if (record.entryCreatedDateTimeLocal != null && record.entryCreatedDateTimeLocal != '') {
        const date = new Date(record.entryCreatedDateTimeLocal);
        var DateTime = luxon.DateTime;
        var dt1 = DateTime.fromJSDate(date);
        var dt = dt1.toFormat('dd LLL yyyy @ HH:mm') + ' (' + record.entryCreatedDateTimeZoneShort + ')';
        return dt;
    }
    else if (value !== '') {
        const date = new Date(value);
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        let day = date.getDate();

        if (day < 10) {
            day = '0' + day;
        }
        return day + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
    }
}


let clientSiteActiveGuardsKeyVehicleHistory = $('#clientSiteActiveGuardsKeyVehicleHistory').DataTable({
    ordering: false,
    info: false,
    searching: false,
    autoWidth: false,
    fixedHeader: false,
    deferLoading: 57,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    "processing": true,
    'language': {
        'loadingRecords': '&nbsp;',
        'processing': 'Loading...Please wait...'
    },
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteKeyVehicleHistory',
        datatype: 'json',
        data: function (d) {
            d.clientSiteId = $('#txtClientSiteId').val();
            d.guardId = $('#txtGuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', title: 'Id', visible: false },
        { data: 'id', title: 'KeyVehicleLog Id', visible: false, width: '20%' },
        { data: 'vehicleRego', title: 'License No', width: '20%', visible: true },
        { data: 'personName', width: '20%', title: 'Individual', className: "text-center", visible: true },
        { data: 'companyName', width: '20%', title: 'Company', className: "text-center", visible: true },
        { data: 'individualTitle', width: '20%', title: 'Activity', className: "text-center" },
        {
            data: 'entryCreatedDateTimeLocal', width: '20%', title: 'Created Time', className: "text-center",
            render: function (value, type, data) {
                return renderKvDateTime(value, data);
            }
        },
        { data: 'rubbishDeduction', visible: false }
    ],
    drawCallback() {
        var api = this.api();
        var rowslength = api.rows().data().length;
        if (rowslength > 0) {
            var r = api.rows(0).column(7).data()[0];
            if (r == true) {
                api.columns([2]).visible(true);
                api.columns([3]).visible(true);
                api.columns([4]).visible(true);
            } else {
                api.columns([2]).visible(false);
                api.columns([3]).visible(false);
                api.columns([4]).visible(false);
            }
        } else {
            api.columns([2]).visible(true);
            api.columns([3]).visible(true);
            api.columns([4]).visible(true);
        }
    }
});

$('#clientSiteActiveGuards tbody').on('dblclick', '#btnKvHistoryByGuard', function (value, record) {
    $('#guardKeyVehicleHistoryModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).attr("data-guardid");
    var ClientSiteId = $(this).attr('data-clientsiteid');
    var ClientSiteName = $(this).attr('data-clientsitename');
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    $('#lbl_KvGuardActivityHistoryHeader').text(GuardName);
    $('#lbl_kv_History_SitenameInfo').text('Last key vehicle log');
    clientSiteActiveGuardsKeyVehicleHistory.clear().draw();
    clientSiteActiveGuardsKeyVehicleHistory.ajax.reload();
});
/* for Key Vehcile history of the guard end*/


/* for Incident Reports history of the guard start*/
function renderIrDateTime(value, record) {
    if (record.createdOnDateTimeLocal != null && record.createdOnDateTimeLocal != '') {
        const date = new Date(record.createdOnDateTimeLocal);
        var DateTime = luxon.DateTime;
        var dt1 = DateTime.fromJSDate(date);
        var dt = dt1.toFormat('dd LLL yyyy @ HH:mm') + ' (' + record.createdOnDateTimeZoneShort + ')';
        return dt;
    }
    else if (value !== '') {
        const date = new Date(value);
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        let day = date.getDate();

        if (day < 10) {
            day = '0' + day;
        }
        return day + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
    }
}


let clientSiteActiveGuardsIncidentReportHistory = $('#clientSiteActiveGuardsIncidentReportsHistory').DataTable({
    ordering: false,
    info: false,
    searching: false,
    autoWidth: false,
    fixedHeader: false,
    deferLoading: 57,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    "processing": true,
    'language': {
        'loadingRecords': '&nbsp;',
        'processing': 'Loading...Please wait...'
    },
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteIncidentReportHistory',
        datatype: 'json',
        data: function (d) {
            d.clientSiteId = $('#txtClientSiteId').val();
            d.guardId = $('#txtGuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', title: 'Id', visible: false },
        { data: 'id', title: 'IncidentReport Id', visible: true, width: '20%' },
        {
            data: 'fileName',
            width: '60%',
            title: 'File Name',
            className: "text-center",
        },
        {
            data: 'createdOn',
            width: '20%',
            title: 'Created Time',
            className: "text-center",
            render: function (value, type, data) {
                return renderIrDateTime(value, data);
            }
        }
    ]
});

$('#clientSiteActiveGuards tbody').on('dblclick', '#btnIrHistoryByGuard', function (value, record) {
   
    if (($('#txtguardGuardRCAccess').val() == 'False' && $('#txtguardGuardRCHRAccess').val() == 'False') || ($('#txtguardGuardRCAccess').val() == '' && $('#txtguardGuardRCHRAccess').val() == '')) { 
        $('#guardIncidentReportsHistoryModal').modal('show');
    }
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).attr("data-guardid");
    var ClientSiteId = $(this).attr('data-clientsiteid');
    var ClientSiteName = $(this).attr('data-clientsitename');
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    $('#lbl_IrGuardActivityHistoryHeader').text(GuardName);
    $('#lbl_Ir_History_SitenameInfo').text('Last IR log');
    clientSiteActiveGuardsIncidentReportHistory.clear().draw();
        clientSiteActiveGuardsIncidentReportHistory.ajax.reload();
    
});
/* for Incident Reports history of the guard end*/


/* for SmartWand history of the guard start*/
function renderSwDateTime(value, record) {
    if (value !== '' && value != null) {
        const date = new Date(value);
        var DateTime = luxon.DateTime;
        var dt1 = DateTime.fromJSDate(date);
        var dt = dt1.toFormat('dd LLL yyyy @ HH:mm');
        return dt;
    }
    else {
        return '';
    }
}

let clientSiteActiveGuardsSmartWandHistory = $('#clientSiteActiveGuardsSwHistory').DataTable({
    ordering: false,
    info: false,
    searching: false,
    autoWidth: false,
    fixedHeader: false,
    deferLoading: 57,
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    "processing": true,
    'language': {
        'loadingRecords': '&nbsp;',
        'processing': 'Loading...Please wait...'
    },
    ajax: {
        url: '/RadioCheckV2?handler=ClientSiteSWHistory',
        datatype: 'json',
        data: function (d) {
            d.clientSiteId = $('#txtClientSiteId').val();
            d.guardId = $('#txtGuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', title: 'Id', visible: false },
        { data: 'templateName', width: '20%', title: 'Template Name' },
        { data: 'smartWandId', width: '15%', title: 'Smart Wand' },
        { data: 'employeePhone', width: '22%', title: 'Smart Wand Number' },
        {
            data: 'inspectionStartDatetimeLocal', width: '25%', title: 'Inspection Start', className: "text-center",
            render: function (value, type, data) {
                return renderSwDateTime(value, data);
            }
        },
        { data: 'locationScan', width: '30%', title: 'Location' }
    ]
});

$('#clientSiteActiveGuards tbody').on('dblclick', '#btnSwHistoryByGuard', function (value, record) {
    $('#guardSWHistoryModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).attr("data-guardid");
    var ClientSiteId = $(this).attr('data-clientsiteid');
    var ClientSiteName = $(this).attr('data-clientsitename');
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    $('#lbl_SwGuardActivityHistoryHeader').text(GuardName);
    $('#lbl_Sw_History_SitenameInfo').text('Last smart wand log');
    clientSiteActiveGuardsSmartWandHistory.clear().draw();
    clientSiteActiveGuardsSmartWandHistory.ajax.reload();
});

/* for SmartWand history of the guard end*/


/*For radio check dropdown start*/

$('#clientSiteInActiveGuards').on('click', 'button[name="btnRadioCheckStatus"]', function () {
    var data = clientSiteInActiveGuards.row($(this).parents('tr')).data();
    var rowClientSiteId = data.clientSiteId;
    var rowGuardId = data.guardId;
    var rcSatus = data.rcStatus;
    var nottifcationType = data.notificationType;

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
    $('#nottifcationType').val(nottifcationType);
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
    //clickedrow = clientSiteActiveGuards.row($(this).parents('tr')).index();
    //alert('clickedrow:' + clickedrow);
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
        clientSiteActiveGuards.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy - 01-02-2024
        clientSiteInActiveGuards.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy - 01-02-2024
        clientSiteInActiveGuardsSinglePage.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy - 01-02-2024
        clientSiteActiveGuardsSinglePage.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy - 01-02-2024
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
        clientSiteActiveGuards.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy -- End - 01-02-2024
        clientSiteInActiveGuards.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy -- End - 01-02-2024
        clientSiteInActiveGuardsSinglePage.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy -- End - 01-02-2024
        clientSiteActiveGuardsSinglePage.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy -- End - 01-02-2024

    });
});


/*For radio check dropdown  end - end*/

/*for pushing notifications from the control room - start*/
$('#pushNoTificationsControlRoomModal').on('show.bs.modal', function (event) {
    var inpcallfun = $('#inpCallingFunction').val();
    $('#download_siteRCSOPList').removeAttr('href');
    if (inpcallfun == 'STEPBUTTON') {
        $('#textMessageTab').addClass('d-none').removeClass('active');
        $('#textMessage').addClass('d-none').removeClass('show').removeClass('active');

        $('#globalalertTab').removeClass('active');
        $('#globalalert').removeClass('show').removeClass('active');

        $('#ActionListTab').addClass('active');
        $('#actionlist').addClass('show').addClass('active');
    } else {
        const button = $(event.relatedTarget);
        if (button.hasClass('clickbuilding')) {
            $('#textMessageTab').removeClass('d-none').removeClass('active');
            $('#textMessage').removeClass('d-none').removeClass('show').removeClass('active');

            $('#globalalertTab').removeClass('active');
            $('#globalalert').removeClass('show').removeClass('active');

            $('#ActionListTab').addClass('active');
            $('#actionlist').addClass('show').addClass('active');
        }
        else if (button.hasClass('clickenvelope')) {
            $('#textMessageTab').removeClass('d-none').addClass('active');
            $('#textMessage').removeClass('d-none').addClass('show').addClass('active');

            $('#globalalertTab').removeClass('active');
            $('#globalalert').removeClass('show').removeClass('active');

            $('#ActionListTab').removeClass('active');
            $('#actionlist').removeClass('show').removeClass('active');
        }
    }
});
let gridSiteStaffDocAlarm;
$('#pushNoTificationsControlRoomModal').on('shown.bs.modal', function (event) {

    isPaused = true;
    $('#download_siteRCSOPList').removeAttr('href');
    $('#btnSendPushLotificationMessage').prop('disabled', false);
    $('#btnSendGlabalNotificationMessage').prop('disabled', false);
    $('#btnSendActionList').prop('disabled', false);


    const button = $(event.relatedTarget);
    const id = button.data('id');

   
        $('#StaffDocumentsAlarm').grid('destroy', true, true); // Destroy existing grid
        $('#StaffDocumentsAlarm').empty(); // Clear DOM elements
    

   
    

    $('#txtNotificationsCompanyId').val(id);
    $('#chkLB').prop('checked', true);
    $('#chkSiteEmail').prop('checked', false);
    $('#chkSMSPersonal').prop('checked', false);
    $('#chkSMSSmartWand').prop('checked', false);
    $('#chkNationality').prop('checked', false);
    $('#chkSiteState').prop('checked', false);
    $('#chkSiteState').prop('checked', false);
    $('#chkClientType').prop('checked', false);
    $('#chkSMSPersonalGlobal').prop('checked', false);
    $('#chkSMSSmartWandGlobal').prop('checked', false);
    $('#chkGlobalPersonalEmail').prop('checked', false);
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
    $('#site_Physical_key').val('');
    $('#Action1').val('');
    $('#Action2').val('');
    $('#Action3').val('');
    $('#Action4').val('');
    $('#Action5').val('');
    $('#Site_Combination_Look').val('');
    $('#txtComments').html('');

    $('#search_client_siteSteps').val('');
    $('#searchResults').html('');
    $('#dglClientTypeActionList').val('');
    $('#dglClientTypeActionList2').val('');
    $('#dglClientSiteIdActionList').val('');
    $('#dglClientSiteIdActionList2').val('');
    $('#search_client_site').val('');
    $('#pn_client_site_address').val('');
    $('#pn_sitegpsmap').attr('href', "javascript:void(0)");
    $('#pn_sitegpsmap').addClass("disabled-anchor-link");
    //$('#pn_sitegpsmap').prop('disabled', true);
    $('#btncontractedmanning').prop('disabled', true);


    var inpcallfun = $('#inpCallingFunction').val();
    if (inpcallfun != 'STEPBUTTON') {

        $.ajax({
            url: '/RadioCheckV2?handler=GetClientType',
            type: 'POST',
            data: {
                clientSiteId: clientSiteId
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data != null) {
                sitebuttonSelectedClientTypeSiteId = data;
                sitebuttonSelectedClientSiteId = clientSiteId;
                $('#dglClientTypeActionList').val(sitebuttonSelectedClientTypeSiteId).trigger("change");
            }
        });


    } else {

        const clientSiteControl = $('#dglClientSiteIdActionList');
        clientSiteControl.html('');

    }
    /*p4-79 menu corrections-start*/
    $('#chkPersonalEmail').prop('checked', false);
    /*p4-79 menu corrections-end*/
    gridSiteStaffDocAlarm = $('#StaffDocumentsAlarm').grid({

        //dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=4',
        dataSource: {
            url: '/RadioCheckV2?handler=StaffDocsUsingTypeNew&&type=6&&ClientSiteId=' + id,

        },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [


            {
                field: 'fileName', title: 'File Name', width: 240,
                
            },
            //{ field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 93 },
            { width: 98, renderer: schButtonRendererNew },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    function schButtonRendererNew(value, record) {
        let buttonHtml = '';
        buttonHtml += '<a href="' + record.filePath + record.fileName + '" class="btn btn-outline-primary m-1" target="_blank"><i class="fa fa-download"></i>Download</a>';

        return buttonHtml;

    }
});



function clearGuardValidationSummary(validationControl) {
    $('#' + validationControl).removeClass('validation-summary-errors').addClass('validation-summary-valid');
    $('#' + validationControl).html('');
}



$('#btnSendPushLotificationMessage').on('click', function () {
    $(this).prop('disabled', true);
    const checkedLB = $('#chkLB').is(':checked');
    const checkedSiteEmail = $('#chkSiteEmail').is(':checked');
    const checkedPersonalEmail = $('#chkPersonalEmail').is(':checked');
    const checkedSMSPersonal = $('#chkSMSPersonal').is(':checked');
    const checkedSMSSmartWand = $('#chkSMSSmartWand').is(':checked');
    var clientSiteId = $('#txtNotificationsCompanyId').val();
    var Notifications = $('#txtPushNotificationMessage').val();
    var Subject = $('#txtPushNotificationSubject').val();

    if (Notifications === '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter a Message to send ');
        $(this).prop('disabled', false);
    }
    else if (checkedLB == false && checkedSiteEmail == false && checkedSMSPersonal == false && checkedSMSSmartWand == false && checkedPersonalEmail == false) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select any one of the transfer options ');
        $(this).prop('disabled', false);
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
                tmzdata: tmzdata,
                checkedPersonalEmail: checkedPersonalEmail
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $(this).prop('disabled', false);
                $('#pushNoTificationsControlRoomModal').modal('hide');
                $('#Access_permission_RC_status_new').hide();
            }
            else {
                displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
                $(this).prop('disabled', false);
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
    $('#btnSendPushLotificationMessage').prop('disabled', false);
    $('#btnSendGlabalNotificationMessage').prop('disabled', false);
    $('#btnSendActionList').prop('disabled', false);
    $('#btnSendActionListGlobal').prop('disabled', false);
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
    $(this).prop('disabled', true);
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
    const chkGlobalPersonalEmail = $('#chkGlobalPersonalEmail').is(':checked');
    if (Notifications === '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter a Message to send ');
        $(this).prop('disabled', false);
    }
    else if (checkedState == false && chkClientType == false && chkClientType == false && checkedSMSPersonal == false && checkedSMSSmartWand == false && chkNationality == false && chkGlobalPersonalEmail == false) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select any one of the transfer options ');
        $(this).prop('disabled', false);
    }
    else if (chkClientType == true && ClientType == null) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
        $(this).prop('disabled', false);
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
                chkGlobalPersonalEmail: chkGlobalPersonalEmail,
                clientSiteId: clientSiteId,
                tmzdata: tmzdata
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $(this).prop('disabled', false);
                $('#pushNoTificationsControlRoomModal').modal('hide');
                $('#Access_permission_RC_status').hide();
            }
            else {
                displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
                $(this).prop('disabled', false);
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
    $(this).prop('disabled', true);
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
        $(this).prop('disabled', false);
    }

    else if (chkClientType == true && ClientType == null) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
        $(this).prop('disabled', false);
    }
    else if (ClientType == '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
        $(this).prop('disabled', false);
    }
    else if (ClientSite == '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client site ');
        $(this).prop('disabled', false);
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
                $(this).prop('disabled', false);
                $('#pushNoTificationsControlRoomModal').modal('hide');
                $('#Access_permission_RC_status').hide();
            }
            else {
                displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
                $(this).prop('disabled', false);
            }
            //$('#selectRadioStatus').val('');
            //$('#btnRefreshActivityStatus').trigger('click');
        });
    }
});

$('#dglClientSiteIdActionList').on('change', function () {
    $('#btncontractedmanning').prop('disabled', true);
    $('#Site_Alarm_Keypad_code').val('');
    $('#Action1').val('');
    $('#site_Physical_key').val('');
    $('#Action2').val('');
    $('#Action3').val('');
    $('#Action4').val('');
    $('#Action5').val('');
    $('#Smart_wand').val('');
    $('#Landline').val('');
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
        if (data != null) {
            $('#Site_Alarm_Keypad_code').val(data.siteAlarmKeypadCode);
            $('#Action1').val(data.action1);
            $('#site_Physical_key').val(data.sitephysicalkey);
            $('#Action2').val(data.action2);
            $('#Action3').val(data.action3);
            $('#Action4').val(data.action4);
            $('#Action5').val(data.action5);
            $('#Landline').val(data.landline);
            $('#Smart_wand').val(data.smartWandID.join(',    '));
            $('#Site_Combination_Look').val(data.siteCombinationLook);
            $('#txtComments').html(data.controlRoomOperator);
            $('#btncontractedmanning').prop('disabled', false);
            //if (data.imagepath != null) {
            //    const myArray = data.imagepath.split(":-:");
            //    $('#download_imageRCList').attr('href', myArray[1]);
            //    $('#download_imageRCList').attr('download', myArray[0]);
            //} else {
            //    $('#download_imageRCList').removeAttr('href');
            //    $('#download_imageRCList').removeAttr('download');
            //}
            if (data.sopAlarmFileNme != null && data.sopAlarmFileNme.length <= 1) {

                // $('#download_siteRCSOPList').attr('href', 'https://localhost:44356/StaffDocs/' + data.sopFileNme );
                $('#download_imageRCList').attr('href', data.sopAlarmFilePath + data.sopAlarmFileNme);

            } else if (data.sopAlarmFileNme != null && data.sopAlarmFileNme.length > 1) {
                $('#download_imageRCList')
                    .attr('data-toggle', 'modal')
                    .attr('data-target', '.bd-example-modal-lgAlarm');
                //$('.bd-example-modal-lg').show();
            }
            else {
                $('#download_imageRCList').removeAttr('href');
            }
            if (data.sopFileNme != null) {
                
                // $('#download_siteRCSOPList').attr('href', 'https://localhost:44356/StaffDocs/' + data.sopFileNme );
                $('#download_siteRCSOPList').attr('href', 'https://cws-ir.com/StaffDocs/' + data.sopFileNme);
            } else {
                $('#download_siteRCSOPList').removeAttr('href');
            }

        }
    });
    if (clientSiteId)
        $('#btncontractedmanning').prop('disabled', false);


    // Get Site Address and map Details
    getSiteAddressAndMapDetails(clientSiteId);
    sitebuttonSelectedClientSiteId = -1;
});

function getSiteAddressAndMapDetails(clientSiteId) {
    $('#pn_client_site_address').val('');
    $('#pn_sitegpsmap').attr('href', "javascript:void(0)");
    $('#pn_sitegpsmap').addClass("disabled-anchor-link");
    $.ajax({
        url: '/RadioCheckV2?handler=GetClientsiteAddressAndMapDetails',
        type: 'POST',
        data: {
            searchclientSiteId: parseInt(clientSiteId)
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        var html = '';
        if (data) {
            $('#pn_client_site_address').val(data.address);
            if (data.gps != null) {
                var gpslink = 'https://www.google.com/maps?q=' + data.gps;
                $('#pn_sitegpsmap').attr('href', gpslink);
                $('#pn_sitegpsmap').removeClass("disabled-anchor-link");
            }
        }
    });
}


$('#btncopytoclipboard').on('click', function () {
    var clientSiteId = $('#dglClientSiteIdActionList').val();
    if (clientSiteId === null) {
        alert('Please select a site.');
        return;
    }

    /*Copy to clipboard*/
    var textToCopy = "";
    var nwl = "\r\n";

    var cl_type = $("#dglClientTypeActionList option[value='" + $('#dglClientTypeActionList').val() + "']").text() + nwl;
    var cl_site = $("#dglClientSiteIdActionList option[value='" + $('#dglClientSiteIdActionList').val() + "']").text() + nwl;
    var cl_address = $('#pn_client_site_address').val() + nwl;
    var cl_gps = ($('#pn_sitegpsmap').prop('href') == "javascript:void(0)" ? "" : $('#pn_sitegpsmap').prop('href')) + nwl + nwl;
    var cl_keypadcode = $('#Site_Alarm_Keypad_code').val() + nwl;
    var cl_phykey = $('#site_Physical_key').val() + nwl;
    var cl_cmblock = $('#Site_Combination_Look').val() + nwl;
    var cl_act1 = $('#Action1').val() + nwl;
    var cl_act2 = $('#Action2').val() + nwl;
    var cl_act3 = $('#Action3').val() + nwl;
    var cl_act4 = $('#Action4').val() + nwl;
    var cl_act5 = $('#Action5').val() + nwl;
    var cl_txtcoment = $('#txtComments').val() + nwl;
    var cl_txtmsgactlist = $('#txtMessageActionList').val() + nwl;

    textToCopy = `Client Type: ${cl_type}`;
    textToCopy += `Client Site: ${cl_site}`;
    textToCopy += `Address: ${cl_address}`;
    textToCopy += `Google Map Link: ${cl_gps}`;
    textToCopy += "Site Access" + nwl;
    textToCopy += "===========" + nwl;
    textToCopy += `Alarm Keypad Code: ${cl_keypadcode}`;
    textToCopy += `Physical key: ${cl_phykey}`;
    textToCopy += `Combination Lock: ${cl_cmblock}${nwl}`;
    textToCopy += "Alarm Response" + nwl;
    textToCopy += "==============" + nwl;
    textToCopy += `Action 1: ${cl_act1}`;
    textToCopy += `Action 2: ${cl_act2}`;
    textToCopy += `Action 3: ${cl_act3}`;
    textToCopy += `Action 4: ${cl_act4}`;
    textToCopy += `Action 5: ${cl_act5}`;
    textToCopy += `Comments For Control Room Operator: ${cl_txtcoment}`;
    textToCopy += `New Outgoing Message: ${cl_txtmsgactlist}`;
    navigator.clipboard.writeText(textToCopy)
        .then(() => { alert('Copied to clipboard.') })
        .catch((error) => { alert(`Copy failed. Error: ${error}`) })
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
        if (data == 'No matching record found') {
            html = '<p style="color:brown">' + data + '</p>';
        }
        else {
            html = '<p style="color:brown"><i class="fa fa-map-marker" aria-hidden="true"></i>' + data + '</p>';
        }

        $('#searchResults').html(html);
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
        $('#chkGlobalPersonalEmail').prop('checked', false);
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
        $('#chkGlobalPersonalEmail').prop('checked', false);
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
            if (sitebuttonSelectedClientSiteId > 0) {
                let siteByid = [];
                siteByid.push(sitebuttonSelectedClientSiteId);
                $('#dglClientSiteIdActionList').val(siteByid).trigger('change');
            }
        }
    });

    sitebuttonSelectedClientTypeSiteId = -1;
    // sitebuttonSelectedClientSiteId = -1;

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
        $('#chkGlobalPersonalEmail').prop('checked', false);
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
            text: '<i class="fa fa-microphone" aria-hidden="true"></i>',
            titleAttr: 'Start Recoding',
            className: 'btn btn-md mr-2 btn-custom',
            name: 'audioStart',
            action: function (e, dt, node, config) {
                clickstarAudio();
                clientSiteInActiveGuardsSinglePage.buttons('audioStart:name').disable();
                clientSiteInActiveGuardsSinglePage.buttons('audioStop:name').enable();
               

            }
        },
        {
            text: '<i class="fa fa-microphone-slash" aria-hidden="true"></i>',
            titleAttr: 'Stop Recoding',
            className: 'btn btn-md mr-2 btn-custom',
            enabled: false,
            name: 'audioStop',
            action: function () {
                clickStopAudio();
                clientSiteInActiveGuardsSinglePage.buttons('audioStop:name').disable();
                clientSiteInActiveGuardsSinglePage.buttons('audioStart:name').enable();

            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },

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
            text: '<i class="fa fa-print"></i>', // Custom button text
            className: 'btn btn-md mr-2 btn-print',
            titleAttr: 'Print',
            customize: function (win) {
                // Customize the print window/document here
                $(win.document.body).css('font-size', '10pt');
                $(win.document.body).find('table')
                    .addClass('display')
                    .css('font-size', 'inherit');

                // Add a landscape orientation
                var css = '@page { size: landscape; }';
                var head = win.document.head || win.document.getElementsByTagName('head')[0];
                var style = win.document.createElement('style');

                style.type = 'text/css';
                style.media = 'print';
                if (style.styleSheet) {
                    style.styleSheet.cssText = css;
                } else {
                    style.appendChild(win.document.createTextNode(css));
                }

                head.appendChild(style);
            }
        },

        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            
            text: '<img src="/images/guards.png" alt="Image" height="16" width="16">',
            titleAttr: '',
            className: 'btn btn-md mr-2 btn-guardlogin',
            action: function () {
                getLoggedGuardDetails();

            }

        },
        {
            extend: 'pdf',
            text: '<i class="fa fa-globe"></i>',
            titleAttr: 'Globe Map',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                // Redirect to the new page with query parameters
                const clientSiteIds = 'test'; // Replace this with dynamic values if needed
                const newPageUrl = '/GlobeMapNoActivity';
                window.open(newPageUrl, '_blank');
            }
        },
        {
            extend: 'pdf',
            text: '<img src="/images/guards.png" alt="Image" height="16" width="16">',
            titleAttr: '',
            className: 'btn btn-md mr-2 btn-pdf',

        },
        {
            extend: 'pdf',
            text: '<img src="/images/mail.png" alt="Image" height="16" width="16">',
            titleAttr: 'Webmail Program',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {

                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.webmail;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });



            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            extend: 'pdf',
            text: '<img src="/images/tv.png" alt="Image" height="16" width="16">',
            titleAttr: 'TV News Feed',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.tvNewsFeed;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });
            }
        },
        {
            extend: 'pdf',
            text: '<img src="/images/cloudy.png" alt="Image" height="16" width="16">',
            titleAttr: 'Weather Feed',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.wetherFeed;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });
            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            text: '<img src="/images/man-climbing-stairs.png" alt="Image" height="16" width="16">',
            titleAttr: 'Steps',
            className: 'btn btn-md mr-2 btn-custom',
            action: function () {
                $('#inpCallingFunction').val('STEPBUTTON'); // Setting calling function to step button
                $('#pushNoTificationsControlRoomModal').modal('show');

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
    "scrollY": ($(window).height() - 100),
    "paging": false,
    "footer": true,
    "scroller": true, // Task p4#19 Screen Jumping day -- added by Binoy -- End - 01-02-2024
    "stateSave": true, // Task p4#19 Screen Jumping day -- added by Binoy -- End - 01-02-2024
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

            width: '27%',
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
        $('.dataTables_scrollBody').css('overflow-x', 'hidden');  //Remove the x scrollbar
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
if ($('#txtguardGuardRCAccess').val() == 'True' || $('#txtguardGuardRCHRAccess').val() == 'True' ) {


    $("#clientSiteActiveGuards_wrapper .dt-buttons .btn-copy").hide();
    $("#clientSiteActiveGuards_wrapper .dt-buttons .btn-excel").hide();
    $("#clientSiteActiveGuards_wrapper .dt-buttons .btn-pdf").hide();
    $("#clientSiteActiveGuards_wrapper .dt-buttons .btn-print").hide();
    $("#clientSiteActiveGuards_wrapper .dt-buttons .btn-guardlogin").hide();

    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-copy").hide();
    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-excel").hide();
    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-pdf").hide();
    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-print").hide();
    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-guardlogin").hide();


    $("#clientSiteActiveGuardsSinglePage_wrapper .dt-buttons .btn-copy").hide();
    $("#clientSiteActiveGuardsSinglePage_wrapper .dt-buttons .btn-excel").hide();
    $("#clientSiteActiveGuardsSinglePage_wrapper .dt-buttons .btn-pdf").hide();
    $("#clientSiteActiveGuardsSinglePage_wrapper .dt-buttons .btn-print").hide();
    $("#clientSiteActiveGuardsSinglePage_wrapper .dt-buttons .btn-guardlogin").hide();

    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-copy").hide();
    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-excel").hide();
    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-pdf").hide();
    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-print").hide();
    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-guardlogin").hide();
    

}
if ($('#txtguardGuardRCLiteAccess').val() == 'True') {


 

    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-copy").hide();
    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-excel").hide();
    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-pdf").hide();
    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-print").hide();
    $("#clientSiteInActiveGuards_wrapper .dt-buttons .btn-guardlogin").hide();



    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-copy").hide();
    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-excel").hide();
    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-pdf").hide();
    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-print").hide();
    $("#clientSiteInActiveGuardsSinglePage_wrapper .dt-buttons .btn-guardlogin").hide();


}

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
    var nottifcationType = data.notificationType;
    $("#selectRadioStatus").val(data.statusId);
    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#nottifcationType').val(nottifcationType);
    $('#selectRadioCheckStatus').modal('show');
    isPaused = true;
});





let clientSiteActiveGuardsSinglePage = $('#clientSiteActiveGuardsSinglePage').DataTable({

    dom: 'Bfrtip',
    buttons: [


        {
            text: '<i class="fa fa-microphone" aria-hidden="true"></i>',
            titleAttr: 'Start Recoding',
            className: 'btn btn-md mr-2 btn-custom',
            name: 'audioStart',
            action: function (e, dt, node, config) {
                clickstarAudio();
                clientSiteActiveGuardsSinglePage.buttons('audioStart:name').disable();
                clientSiteActiveGuardsSinglePage.buttons('audioStop:name').enable();
              

            }
        },
        {
            text: '<i class="fa fa-microphone-slash" aria-hidden="true"></i>',
            titleAttr: 'Stop Recoding',
            className: 'btn btn-md mr-2 btn-custom',
            enabled: false,
            name: 'audioStop',
            action: function () {
                clickStopAudio();
                clientSiteActiveGuardsSinglePage.buttons('audioStop:name').disable();
                clientSiteActiveGuardsSinglePage.buttons('audioStart:name').enable();

            }
        },


        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },

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
            text: '<i class="fa fa-print"></i>', // Custom button text
            className: 'btn btn-md mr-2 btn-print',
            titleAttr: 'Print',
            customize: function (win) {
                // Customize the print window/document here
                $(win.document.body).css('font-size', '10pt');
                $(win.document.body).find('table')
                    .addClass('display')
                    .css('font-size', 'inherit');

                // Add a landscape orientation
                var css = '@page { size: landscape; }';
                var head = win.document.head || win.document.getElementsByTagName('head')[0];
                var style = win.document.createElement('style');

                style.type = 'text/css';
                style.media = 'print';
                if (style.styleSheet) {
                    style.styleSheet.cssText = css;
                } else {
                    style.appendChild(win.document.createTextNode(css));
                }

                head.appendChild(style);
            }
        },


        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            
            text: '<img src="/images/guards.png" alt="Image" height="16" width="16">',
            titleAttr: '',
            className: 'btn btn-md mr-2 btn-guardlogin',
            action: function () {
                getLoggedGuardDetails();

            }

        },
        {
            extend: 'pdf',
            text: '<i class="fa fa-globe"></i>',
            titleAttr: 'Globe Map',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                // Redirect to the new page with query parameters
                const clientSiteIds = 'test'; // Replace this with dynamic values if needed
                const newPageUrl = '/GlobeMapNoActivity';
                window.open(newPageUrl, '_blank');
            }
        },
        {
           
            text: '<img src="/images/guards.png" alt="Image" height="16" width="16">',
            titleAttr: '',
            className: 'btn btn-md mr-2 btn-guardlogin',
            action: function () {
                getLoggedGuardDetails();

            }

        },
        {
            extend: 'pdf',
            text: '<img src="/images/mail.png" alt="Image" height="16" width="16">',
            titleAttr: 'Webmail Program',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {

                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.webmail;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });



            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },
        {
            extend: 'pdf',
            text: '<img src="/images/tv.png" alt="Image" height="16" width="16">',
            titleAttr: 'TV News Feed',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.tvNewsFeed;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });
            }
        },
        {
            extend: 'pdf',
            text: '<img src="/images/cloudy.png" alt="Image" height="16" width="16">',
            titleAttr: 'Weather Feed',
            className: 'btn btn-md mr-2 btn-pdf',
            action: function (e, dt, node, config) {
                $.ajax({
                    url: '/Admin/Settings?handler=HyperLinks',
                    type: 'GET',
                    dataType: 'json',
                }).done(function (result) {
                    var newPageUrl = result.wetherFeed;
                    if (!newPageUrl.startsWith("http://") && !newPageUrl.startsWith("https://")) {
                        newPageUrl = "https://" + newPageUrl; // Prepend "https://" if missing
                    }
                    window.open(newPageUrl, '_blank');

                });
            }
        },
        {
            text: '|',
            titleAttr: 'Space',
            className: 'btn-hidden',
            enabled: false,
            name: 'Space',

        },


        {
            text: '<img src="/images/man-climbing-stairs.png" alt="Image" height="16" width="16">',
            titleAttr: 'Steps',
            className: 'btn btn-md mr-2 btn-custom',
            action: function () {
                $('#inpCallingFunction').val('STEPBUTTON'); // Setting calling function to step button
                $('#pushNoTificationsControlRoomModal').modal('show');

            }
        }


    ],





    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    ordering: true,
    "columnDefs": [
        { "visible": false, "targets": 1 },// Hide the group column initially
        { "visible": false, "targets": 2 }
    ],
    order: [[11, 'asc']], // Task p4#41_A~Z and Z~A sorting issue -- modified by Binoy - 31-01-2024
    info: false,
    searching: true,
    autoWidth: true,
    fixedHeader: true,
    "scrollY": ($(window).height()),
    "paging": false,
    "footer": true,
    "scroller": true, // Task p4#19 Screen Jumping day -- added by Binoy -- Start - 01-02-2024
    "stateSave": true,// Task p4#19 Screen Jumping day -- added by Binoy -- End - 01-02-2024
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
            width: '18%',
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
            orderable: false, // Task p4#41_A~Z and Z~A sorting issue -- added by Binoy - 31-01-2024
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
                if (value != 0)
                    return '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardLogBookInfoModal" id="btnLogBookDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
                else
                    return '<button type="button" class="btn" id="btnLogBookHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
            }
        },
        {
            data: 'keyVehicle',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                if (value != 0)
                    return '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardKeyVehicleInfoModal" id="btnKeyVehicleDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
                else
                    return '<button type="button" class="btn" id="btnKvHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
            }
        },
        {
            data: 'incidentReport',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                if (value != 0)
                    return '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardIncidentReportsInfoModal" id="btnIncidentReportdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
                else
                    return '<button type="button" class="btn" id="btnIrHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
            }

        },
        {
            data: 'smartWands',
            width: '6%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                if (data.hasmartwand !== 0) {
                    if (value != 0)
                        return '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardSWInfoModal" id="btnSWdetails">' + value + '</a>' + ']<input type="hidden" id="ClientSiteId" value="' + data.clientSiteId + '"><input type="hidden" id="GuardId" value="' + data.guardId + '">';
                    else
                        return '<button type="button" class="btn" id="btnSwHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
                }
                else {
                    return '<i class="fa fa-times-circle text-text-muted rc-client-status" style="color:#B8B8B8"></i>'
                    // return '<button type="button" class="btn" id="btnSwHistoryByGuard" data-clientsitename="' + data.onlySiteName + '" data-clientsiteid="' + data.clientSiteId + '" data-guardid="' + data.guardId + '"><i class="fa fa-times-circle text-danger rc-client-status"></i></button>';
                }
            }
        },
        {
            data: 'latestDate',
            width: '2%',
            className: "text-center",
            render: function (value, type, data) {

                if (data.rcColorId != 1) {

                    if (value < 80)
                        return '<div class="p-1 mb-1" style="background: #AFE1AF;">' + value + '</div>';
                    if (value >= 80)
                        return '<div class="p-1 mb-1" style="background: #FFD580;">' + value + '</div>';

                }
                else {
                    return '<div class="p-1 mb-1" style="background:  #A9A9A9;">' + '00' + '</div>';
                }
                //return value;
            }

        },

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

        // Task p4#41_A~Z and Z~A sorting issue -- added by Binoy -- Start - 31-01-2024
        {
            data: 'onlySiteName',
            visible: false,
            width: '20%',

        },
        {
            data: 'hR1',
            width: '3%',
            className: "text-center",

            render: function (value, type, data) {
                if (value == 'Green') return '<i class="fa fa-circle text-success"></i>';
                if (value == 'Red') return '<i class="fa fa-circle text-danger"></i>';
                if (value == 'Yellow') return '<i class="fa fa-circle text-warning"></i>';
                if (value == 'Grey') return '<i class="fa fa-circle text-muted"></i>';
            }
        },
        {
            data: 'hR2',
            width: '3%',
            className: "text-center",

            render: function (value, type, data) {
                if (value == 'Green') return '<i class="fa fa-circle text-success"></i>';
                if (value == 'Red') return '<i class="fa fa-circle text-danger"></i>';
                if (value == 'Yellow') return '<i class="fa fa-circle text-warning"></i>';
                if (value == 'Grey') return '<i class="fa fa-circle text-muted"></i>';
            }
        },
        {
            data: 'hR3',
            width: '3%',
            className: "text-center",

            render: function (value, type, data) {
                if (value == 'Green') return '<i class="fa fa-circle text-success"></i>';
                if (value == 'Red') return '<i class="fa fa-circle text-danger"></i>';
                if (value == 'Yellow') return '<i class="fa fa-circle text-warning"></i>';
                if (value == 'Grey') return '<i class="fa fa-circle text-muted"></i>';
            }
        },
        // Task p4#41_A~Z and Z~A sorting issue -- added by Binoy -- End - 31-01-2024


    ],

    preDrawCallback: function (settings) {
        scrollPosition = $(".dataTables_scrollBody").scrollTop();
    },
    drawCallback: function () {
        $(".dataTables_scrollBody").scrollTop(scrollPosition);
        $('.dataTables_scrollBody').css('overflow-x', 'hidden'); //Remove the x scrollbar
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


// Order by the grouping
// Task p4#41_A~Z and Z~A sorting issue -- added by Binoy -- Start - 31-01-2024
$(clientSiteActiveGuardsSinglePage.table().header()).on('click', 'th', function () {
    // Checkout issue on https://datatables.net/reference/api/table().header()  , https://datatables.net/forums/discussion/43165/click-event-in-column-header-never-fired    
    var index = clientSiteActiveGuardsSinglePage.column(this).index();
    var currentOrder = clientSiteActiveGuardsSinglePage.order()[0];
    if (index === 3) {
        if (currentOrder[1] === 'asc') {
            clientSiteActiveGuardsSinglePage.order([groupColumnSortAlias, 'desc']).draw();
        }

        else {
            clientSiteActiveGuardsSinglePage.order([groupColumnSortAlias, 'asc']).draw();
        }
    }
});
// Task p4#41_A~Z and Z~A sorting issue -- added by Binoy -- End - 31-01-2024


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

$('#clientSiteActiveGuardsSinglePage tbody').on('dblclick', '#btnLogBookHistoryByGuard', function (value, record) {
    $('#guardLogBookHistoryModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).attr("data-guardid");
    var ClientSiteId = $(this).attr('data-clientsiteid');
    var ClientSiteName = $(this).attr('data-clientsitename');
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    $('#lbl_GuardLogBookHistoryModalHeader').text(GuardName);
    $('#lbl_lb_History_SitenameInfo').text('Last log book log');
    clientSiteActiveGuardsLogBookHistory.clear().draw();
    clientSiteActiveGuardsLogBookHistory.ajax.reload();
});

$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnKeyVehicleDetailsByGuard', function (value, record) {
    $('#guardKeyVehicleInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(2).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(2).find('#ClientSiteId').val();
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

$('#clientSiteActiveGuardsSinglePage tbody').on('dblclick', '#btnKvHistoryByGuard', function (value, record) {
    $('#guardKeyVehicleHistoryModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).attr("data-guardid");
    var ClientSiteId = $(this).attr('data-clientsiteid');
    var ClientSiteName = $(this).attr('data-clientsitename');
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    $('#lbl_KvGuardActivityHistoryHeader').text(GuardName);
    $('#lbl_kv_History_SitenameInfo').text('Last key vehicle log');
    clientSiteActiveGuardsKeyVehicleHistory.clear().draw();
    clientSiteActiveGuardsKeyVehicleHistory.ajax.reload();
});


$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnIncidentReportdetails', function (value, record) {
    $('#guardIncidentReportsInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(3).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(3).find('#ClientSiteId').val();
    if (GuardId.length == 0) {
        GuardId = $(this).closest("tr").find('td').eq(1).find('#GuardId').val();
    }
    if (ClientSiteId.length == 0) {
        ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    }

    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader2').text(GuardName + '-' + 'Incident Report Details');
    clientSiteActiveGuardsIncidentReportsDetails.ajax.reload();
    clientSiteActiveGuardsLastIncidentReportsDetails.ajax.reload();

});

$('#clientSiteActiveGuardsSinglePage tbody').on('dblclick', '#btnIrHistoryByGuard', function (value, record) {
    //if (($('#txtguardGuardRCAccess').val() == 'False' && $('#txtguardGuardRCHRAccess').val() == 'False') || ($('#txtguardGuardRCAccess').val() == '' && $('#txtguardGuardRCHRAccess').val() == '')) {
        $('#guardIncidentReportsHistoryModal').modal('show');
    //}
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).attr("data-guardid");
    var ClientSiteId = $(this).attr('data-clientsiteid');
    var ClientSiteName = $(this).attr('data-clientsitename');
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    $('#lbl_IrGuardActivityHistoryHeader').text(GuardName);
    $('#lbl_Ir_History_SitenameInfo').text('Last IR log');
    clientSiteActiveGuardsIncidentReportHistory.clear().draw();
    clientSiteActiveGuardsIncidentReportHistory.ajax.reload();
});

$('#clientSiteActiveGuardsSinglePage tbody').on('click', '#btnSWdetails', function (value, record) {
    $('#guardSWInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(4).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(4).find('#ClientSiteId').val();
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

$('#clientSiteActiveGuardsSinglePage tbody').on('dblclick', '#btnSwHistoryByGuard', function (value, record) {
    $('#guardSWHistoryModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).attr("data-guardid");
    var ClientSiteId = $(this).attr('data-clientsiteid');
    var ClientSiteName = $(this).attr('data-clientsitename');
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    $('#lbl_SwGuardActivityHistoryHeader').text(GuardName);
    $('#lbl_Sw_History_SitenameInfo').text('Last smart wand log');
    clientSiteActiveGuardsSmartWandHistory.clear().draw();
    clientSiteActiveGuardsSmartWandHistory.ajax.reload();
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


    if (isRadionCheckStatusAdding == true) {
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
            d.ColorId = $('#ColorIdHover').val();
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
    $('#inpCallingFunction').val('SITEBUTTON'); // ReSetting call function button to SITEBUTTON.
});
$("#hoverModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#guardInfoModal").on("hidden.bs.modal", function () {
    isPaused = false;
    $('#contactCardIcon').remove();
});
$("#guardLogBookHistoryModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#guardKeyVehicleHistoryModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#guardIncidentReportsHistoryModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#guardSWHistoryModal").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#crmSupplierDetailsModal").on("hidden.bs.modal", function () {
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
//New Grdi
$('#itemList,#itemList2').on('click', '.btn-select-radio-status', function (event) {

    var target = event.target;
    var parentId = target.parentNode.innerText.trim();
    var itemToDelete = target.parentNode.dataset.index;
    var itemToDelete = target.parentNode.value;
    var clientSiteId = $('#clientSiteId').val();
    const checkedStatus = target.parentNode.innerText.trim();
    var statusId = target.parentNode.dataset.index;
    var guardId = $('#guardId').val();
    if (checkedStatus === '') {
        return;
    }
    var notificationType = $('#nottifcationType').val(); 
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
            tmzdata: tmzdata,
            notificationType: notificationType
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function () {
        $('#selectRadioCheckStatusActive').modal('hide');
        $('#selectRadioCheckStatus').modal('hide');
        $('#selectRadioStatus').val('');
        clientSiteActiveGuards.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy - 01-02-2024
        clientSiteInActiveGuards.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy - 01-02-2024
        clientSiteInActiveGuardsSinglePage.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy - 01-02-2024
        clientSiteActiveGuardsSinglePage.ajax.reload(null, false); // Task p4#19 Screen Jumping day -- modified by Binoy - 01-02-2024
    });

});


// ################## Contracted Manning Edit Start ###################
// ---> Need to refer Kpi settings screen also
$('#btncontractedmanning').on('click', function (event) {
    var csnme = $('#dglClientSiteIdActionList option:selected').text();
    var csid = $('#dglClientSiteIdActionList option:selected').val();
    if (csnme == '' || csid == '') {
        console.log('Nothing selected...')
        alert('Please select a site to edit.');
        return;
    }
    localStorage.removeItem('activeTab');
    $('#modelchoice').val('CONTRACTEDMANNING');
    $('#kpi-settings-modal').modal('show');

});

$('#kpi-settings-modal').on('shown.bs.modal', function (event) {
    ShowKpiModelChoice();
});


$('#div_site_settings').on('click', '#save_site_manning_settings', function () {

    localStorage.removeItem('activeTab');
    $.ajax({
        url: '/RadioCheckV2?handler=ClientSiteManningKpiSettings',
        type: 'POST',
        data: $('#frm_site_manning_settings').serialize(),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        if (data.success == 1) {
            alert('Saved site manning details successfully');
            $('#kpi-settings-modal').modal('hide');
            $('#modelchoice').val('CONTRACTEDMANNING');
            $('#kpi-settings-modal').modal('show');
        }
        else if (data.success == 2) {
            $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
            $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");
            $('#ClientSiteManningGuardKpiSettings_1__PositionId').val($('#ClientSiteManningGuardKpiSettings_1__PositionId option:first').val());
            $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').val($('#ClientSiteManningPatrolCarKpiSettings_1__PositionId option:first').val());
            alert('Please add the site settings from site settings tab');
        }
        else if (data.success == 3) {
            alert('Please select position');
        }
        else if (data.success == 4) {
            alert('Error! Please try again');
            $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
            $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");
        }
        else if (data.success == 5) {
            alert('Please enter a valid time for Start and End in the format of HH:mm and in the range of 00:01 - 23:59. These are invalid times ' + data.erorrMessage + ' .');

        }

        else if (data.success == 6) {
            alert('The clocks must be in the range of 00:01-23:59, and ' + data.erorrMessage + ' is an invalid input.');

        }
        else if (data.success == 7) {
            alert('Please make sure you fill out the three boxes (start, end, and workers) for a day or make them blank. Please ensure workers have a value and cannot be blank when a clock is set.');

        }

    }).fail(function () { });
});

if ($('#txtguardGuardRCLiteAccess').val() == 'True') {
    $('#ActiveGuardsDivision').hide();
}
$('#div_site_settings').on('change', '#ClientSite_Status', function () {
    // Get the selected value
    var selectedStatus = $(this).val();
    if (selectedStatus != 0) {
        $('#ClientSite_StatusDate').show();
    } else {
        $('#ClientSite_StatusDate').hide();
    }
    if (selectedStatus == 2) {
        $('#scheduleisActive').prop('checked', false);
    }
    else {
        $('#scheduleisActive').prop('checked', true);
    }



});

$('#div_site_settings').on('click', '#showDivButton', function () {

    $('#divPatrolCar').show();
    $('#divbtn').show();
});

$('#div_site_settings').on('click', '#delete_worker', function () {
    if (confirm('Are you sure want to delete worker ?')) {
        var buttonValue = $(this).val();
        $.ajax({
            url: '/RadioCheckV2?handler=DeleteWorker',
            type: 'POST',
            data: { settingsId: buttonValue },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                if (result.clientSiteId !== 0) {
                    $('#kpi-settings-modal').modal('hide');
                    $('#modelchoice').val('CONTRACTEDMANNING');
                    $('#kpi-settings-modal').modal('show');
                }
            }
            else
                alert(result.message);
        }).fail(function () { });
    }
});

$('#div_site_settings').on('change', '#positionfilterGuard', function () {
    const isChecked = $(this).is(':checked');
    const filter = isChecked ? 1 : 2;

    $.ajax({
        url: '/RadioCheckV2?handler=OfficerPositions&filter=' + filter,
        type: 'GET',
        dataType: 'json'
    }).done(function (data) {
        $('#Report_Officer_Position').html('');
        data.map(function (position) {
            $('#Report_Officer_Position').append('<option value="' + position.value + '">' + position.text + '</option>');
        });
    });
});

$(".patrol-sum").on("input", function () {
    alert('ddd');
    calculateSum();
});
$('#div_site_settings').on('change', '#positionfilterPatrolCar', function () {

    const isChecked = $(this).is(':checked');
    const filter = isChecked ? 1 : 2;

    $("#ClientSiteManningPatrolCarKpiSettings_0__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettings_1__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettings_2__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettings_3__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettings_4__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettings_5__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettings_6__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettings_7__Type").val(filter);


    calculateSumOfTextBoxValues();

    $.ajax({
        url: '/RadioCheckV2?handler=OfficerPositions&filter=' + filter,
        type: 'GET',
        dataType: 'json'
    }).done(function (data) {
        $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').html('');
        data.map(function (position) {
            $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').append('<option value="' + position.value + '">' + position.text + '</option>');
        });
    });

});

$('#div_site_settings').on('input', '#ClientSiteManningPatrolCarKpiSettings_0__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettings_1__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettings_2__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettings_3__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettings_4__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettings_5__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettings_6__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettings_7__NoOfPatrols', function () {

    calculateSumOfTextBoxValues();

});

$('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettings_0__NoOfPatrols,#ClientSiteManningGuardKpiSettings_1__NoOfPatrols, #ClientSiteManningGuardKpiSettings_2__NoOfPatrols, #ClientSiteManningGuardKpiSettings_3__NoOfPatrols, #ClientSiteManningGuardKpiSettings_4__NoOfPatrols, #ClientSiteManningGuardKpiSettings_5__NoOfPatrols,#ClientSiteManningGuardKpiSettings_6__NoOfPatrols,#ClientSiteManningGuardKpiSettings_7__NoOfPatrols', function () {

    calculateSumOfTextBoxValues2();

});

$('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettings_8__NoOfPatrols, #ClientSiteManningGuardKpiSettings_9__NoOfPatrols, #ClientSiteManningGuardKpiSettings_10__NoOfPatrols, #ClientSiteManningGuardKpiSettings_11__NoOfPatrols, #ClientSiteManningGuardKpiSettings_12__NoOfPatrols,#ClientSiteManningGuardKpiSettings_13__NoOfPatrols,#ClientSiteManningGuardKpiSettings_14__NoOfPatrols,#ClientSiteManningGuardKpiSettings_15__NoOfPatrols', function () {

    calculateSumOfTextBoxValues3();

});

$('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettings_16__NoOfPatrols,#ClientSiteManningGuardKpiSettings_17__NoOfPatrols, #ClientSiteManningGuardKpiSettings_18__NoOfPatrols, #ClientSiteManningGuardKpiSettings_19__NoOfPatrols, #ClientSiteManningGuardKpiSettings_20__NoOfPatrols, #ClientSiteManningGuardKpiSettings_21__NoOfPatrols,#ClientSiteManningGuardKpiSettings_22__NoOfPatrols,#ClientSiteManningGuardKpiSettings_23__NoOfPatrols', function () {

    calculateSumOfTextBoxValues4();

});

function calculateSumOfTextBoxValues() {

    if ($("#positionfilterPatrolCar").prop('checked') == true) {


        // Get the values from textbox1 and convert them to numbers
        var value1 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_0__NoOfPatrols').val()) || 0;
        var value2 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_1__NoOfPatrols').val()) || 0;
        var value3 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_2__NoOfPatrols').val()) || 0;
        var value4 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_3__NoOfPatrols').val()) || 0;
        var value5 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_4__NoOfPatrols').val()) || 0;
        var value6 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_5__NoOfPatrols').val()) || 0;
        var value7 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_6__NoOfPatrols').val()) || 0;
        var value8 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_7__NoOfPatrols').val()) || 0;
        // Calculate the sum
        var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
        // Update the value in textbox2
        if (sum !== 0) { $('#monthlyHrsAddNew').val(sum); }
        $('#monthlyHrsTxtAddNew').text('Total Patrols :');
        $('#lbl_ManningPatrolCar_3').text('No of Patrols');
    }
    else {
        $('#monthlyHrsTxtAddNew').text('Monthly Hrs :');
        $('#lbl_ManningPatrolCar_3').text('Workers');
        $('#monthlyHrsAddNew').val('');
    }

}

function calculateSumOfTextBoxValues2() {

    var check = $("#positionfilterGuard_0").prop('checked');
    if ($("#positionfilterGuard_0").prop('checked') == true) {

        // Get the values from textbox1 and convert them to numbers
        var value1 = parseFloat($('#ClientSiteManningGuardKpiSettings_0__NoOfPatrols').val()) || 0;
        var value2 = parseFloat($('#ClientSiteManningGuardKpiSettings_1__NoOfPatrols').val()) || 0;
        var value3 = parseFloat($('#ClientSiteManningGuardKpiSettings_2__NoOfPatrols').val()) || 0;
        var value4 = parseFloat($('#ClientSiteManningGuardKpiSettings_3__NoOfPatrols').val()) || 0;
        var value5 = parseFloat($('#ClientSiteManningGuardKpiSettings_4__NoOfPatrols').val()) || 0;
        var value6 = parseFloat($('#ClientSiteManningGuardKpiSettings_5__NoOfPatrols').val()) || 0;
        var value7 = parseFloat($('#ClientSiteManningGuardKpiSettings_6__NoOfPatrols').val()) || 0;
        var value8 = parseFloat($('#ClientSiteManningGuardKpiSettings_7__NoOfPatrols').val()) || 0;
        // Calculate the sum
        var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
        // Update the value in textbox2
        $('#monthlyHrs_0').val(sum);


    }

}

function calculateSumOfTextBoxValues3() {

    var check = $("#positionfilterGuard_8").prop('checked');
    if ($("#positionfilterGuard_8").prop('checked') == true) {

        // Get the values from textbox1 and convert them to numbers
        var value1 = parseFloat($('#ClientSiteManningGuardKpiSettings_8__NoOfPatrols').val()) || 0;
        var value2 = parseFloat($('#ClientSiteManningGuardKpiSettings_9__NoOfPatrols').val()) || 0;
        var value3 = parseFloat($('#ClientSiteManningGuardKpiSettings_10__NoOfPatrols').val()) || 0;
        var value4 = parseFloat($('#ClientSiteManningGuardKpiSettings_11__NoOfPatrols').val()) || 0;
        var value5 = parseFloat($('#ClientSiteManningGuardKpiSettings_12__NoOfPatrols').val()) || 0;
        var value6 = parseFloat($('#ClientSiteManningGuardKpiSettings_13__NoOfPatrols').val()) || 0;
        var value7 = parseFloat($('#ClientSiteManningGuardKpiSettings_14__NoOfPatrols').val()) || 0;
        var value8 = parseFloat($('#ClientSiteManningGuardKpiSettings_15__NoOfPatrols').val()) || 0;
        // Calculate the sum
        var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
        // Update the value in textbox2
        $('#monthlyHrs_8').val(sum);


    }

}

function calculateSumOfTextBoxValues4() {

    var check = $("#positionfilterGuard_16").prop('checked');
    if ($("#positionfilterGuard_16").prop('checked') == true) {

        // Get the values from textbox1 and convert them to numbers
        var value1 = parseFloat($('#ClientSiteManningGuardKpiSettings_16__NoOfPatrols').val()) || 0;
        var value2 = parseFloat($('#ClientSiteManningGuardKpiSettings_17__NoOfPatrols').val()) || 0;
        var value3 = parseFloat($('#ClientSiteManningGuardKpiSettings_18__NoOfPatrols').val()) || 0;
        var value4 = parseFloat($('#ClientSiteManningGuardKpiSettings_19__NoOfPatrols').val()) || 0;
        var value5 = parseFloat($('#ClientSiteManningGuardKpiSettings_20__NoOfPatrols').val()) || 0;
        var value6 = parseFloat($('#ClientSiteManningGuardKpiSettings_21__NoOfPatrols').val()) || 0;
        var value7 = parseFloat($('#ClientSiteManningGuardKpiSettings_22__NoOfPatrols').val()) || 0;
        var value8 = parseFloat($('#ClientSiteManningGuardKpiSettings_23__NoOfPatrols').val()) || 0;

        // Calculate the sum
        var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
        // Update the value in textbox2
        $('#monthlyHrs_16').val(sum);


    }

}


$('#div_site_settings').on('click', '#save_site_manning_settings_adhoc', function () {

   
    $.ajax({
        url: '/RadioCheckV2?handler=ClientSiteManningKpiSettingsADHOC',
        type: 'POST',
        data: $('#frm_site_manning_settingsAdhoc').serialize(),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        if (data.success == 1) {
            alert('Saved site manning details successfully');
            $('#kpi-settings-modal').modal('hide');
            $('#modelchoice').val('CONTRACTEDMANNING');
            $('#kpi-settings-modal').modal('show');
           // $('#kpi-tab').tab('show');
            $('#contracted-manning-tabadhoc').tab('show');
            // Store the tab ID in local storage
            localStorage.setItem('activeTab', 'contracted-manning-tabadhoc');
        }
        else if (data.success == 2) {
            $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
            $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");
            $('#ClientSiteManningGuardKpiSettings_1__PositionId').val($('#ClientSiteManningGuardKpiSettings_1__PositionId option:first').val());
            $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').val($('#ClientSiteManningPatrolCarKpiSettings_1__PositionId option:first').val());
            alert('Please add the site settings from site settings tab');
        }
        else if (data.success == 3) {
            alert('Please select position');
        }
        else if (data.success == 4) {
            alert('Error! Please try again');
            $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
            $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");
        }
        else if (data.success == 5) {
            alert('Please enter a valid time for Start and End in the format of HH:mm and in the range of 00:01 - 23:59. These are invalid times ' + data.erorrMessage + ' .');

        }

        else if (data.success == 6) {
            alert('The clocks must be in the range of 00:01-23:59, and ' + data.erorrMessage + ' is an invalid input.');

        }
        else if (data.success == 7) {
            alert('Please make sure you fill out the three boxes (start, end, and workers) for a day or make them blank. Please ensure workers have a value and cannot be blank when a clock is set.');

        }

    }).fail(function () { });
});



$('#div_site_settings').on('click', '#showDivButtonAdhoc', function () {

    $('#divPatrolCarAdhoc').show();
    $('#divbtnAdhoc').show();
});


$('#div_site_settings').on('change', '#positionfilterPatrolCarAdhoc', function () {

    const isChecked = $(this).is(':checked');
    const filter = isChecked ? 1 : 2;

    $("#ClientSiteManningPatrolCarKpiSettingsADHOC_0__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettingsADHOC_1__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettingsADHOC_2__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettingsADHOC_3__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettingsADHOC_4__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettingsADHOC_5__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettingsADHOC_6__Type").val(filter);
    $("#ClientSiteManningPatrolCarKpiSettingsADHOC_7__Type").val(filter);


    calculateSumOfTextBoxValuesAdhoc();

    $.ajax({
        url: '/RadioCheckV2?handler=OfficerPositions=' + filter,
        type: 'GET',
        dataType: 'json'
    }).done(function (data) {
        $('#ClientSiteManningPatrolCarKpiSettingsADHOC_1__PositionId').html('');
        data.map(function (position) {
            $('#ClientSiteManningPatrolCarKpiSettingsADHOC_1__PositionId').append('<option value="' + position.value + '">' + position.text + '</option>');
        });
    });

});

$('#div_site_settings').on('click', '#delete_workerAdhoc', function () {
    if (confirm('Are you sure want to delete worker ?')) {
        var buttonValue = $(this).val();
        $.ajax({
            url: '/RadioCheckV2?handler=DeleteWorkerADHOC',
            type: 'POST',
            data: { settingsId: buttonValue },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                if (result.clientSiteId !== 0) {
                    $('#kpi-settings-modal').modal('hide');
                    $('#modelchoice').val('CONTRACTEDMANNING');
                    $('#contracted-manning-tabadhoc').tab('show');
                    $('#kpi-settings-modal').modal('show');
                   
                   
                }
            }
            else
                alert(result.message);
        }).fail(function () { });
    }
});






$('#div_site_settings').on('input', '#ClientSiteManningPatrolCarKpiSettingsADHOC_0__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettingsADHOC_1__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettingsADHOC_2__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettingsADHOC_3__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettingsADHOC_4__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettingsADHOC_5__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettingsADHOC_6__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettingsADHOC_7__NoOfPatrols', function () {

    calculateSumOfTextBoxValuesAdhoc();

});

$('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettingsADHOC_0__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_1__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_2__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_3__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_4__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_5__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_6__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_7__NoOfPatrols', function () {

    calculateSumOfTextBoxValues2Adhoc();

});

$('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettingsADHOC_8__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_9__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_10__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_11__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_12__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_13__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_14__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_15__NoOfPatrols', function () {

    calculateSumOfTextBoxValues3Adhoc();

});

$('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettingsADHOC_16__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_17__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_18__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_19__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_20__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_21__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_22__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_23__NoOfPatrols', function () {

    calculateSumOfTextBoxValues4Adhoc();

});




function calculateSumOfTextBoxValuesAdhoc() {

    if ($("#positionfilterPatrolCarAdhoc").prop('checked') == true) {


        // Get the values from textbox1 and convert them to numbers
        var value1 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_0__NoOfPatrols').val()) || 0;
        var value2 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_1__NoOfPatrols').val()) || 0;
        var value3 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_2__NoOfPatrols').val()) || 0;
        var value4 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_3__NoOfPatrols').val()) || 0;
        var value5 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_4__NoOfPatrols').val()) || 0;
        var value6 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_5__NoOfPatrols').val()) || 0;
        var value7 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_6__NoOfPatrols').val()) || 0;
        var value8 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_7__NoOfPatrols').val()) || 0;
        // Calculate the sum
        var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
        // Update the value in textbox2
        if (sum !== 0) { $('#monthlyHrsAddNewAdhoc').val(sum); }
        $('#monthlyHrsTxtAddNewAdhoc').text('Total Patrols :');
        $('#lbl_ManningPatrolCarAdhoc_3').text('No of Patrols');
    }
    else {
        $('#monthlyHrsTxtAddNewAdhoc').text('Monthly Hrs :');
        $('#lbl_ManningPatrolCarAdhoc_3').text('Workers');
        $('#monthlyHrsAddNewAdhoc').val('');
    }

}

function calculateSumOfTextBoxValues2Adhoc() {

    var check = $("#positionfilterGuardAdhoc_0").prop('checked');
    if ($("#positionfilterGuardAdhoc_0").prop('checked') == true) {

        // Get the values from textbox1 and convert them to numbers
        var value1 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_0__NoOfPatrols').val()) || 0;
        var value2 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_1__NoOfPatrols').val()) || 0;
        var value3 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_2__NoOfPatrols').val()) || 0;
        var value4 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_3__NoOfPatrols').val()) || 0;
        var value5 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_4__NoOfPatrols').val()) || 0;
        var value6 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_5__NoOfPatrols').val()) || 0;
        var value7 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_6__NoOfPatrols').val()) || 0;
        var value8 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_7__NoOfPatrols').val()) || 0;
        // Calculate the sum
        var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
        // Update the value in textbox2
        $('#monthlyHrsAdhoc_0').val(sum);


    }

}

function calculateSumOfTextBoxValues3Adhoc() {

    var check = $("#positionfilterGuardAdhoc_8").prop('checked');
    if ($("#positionfilterGuardAdhoc_8").prop('checked') == true) {

        // Get the values from textbox1 and convert them to numbers
        var value1 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_8__NoOfPatrols').val()) || 0;
        var value2 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_9__NoOfPatrols').val()) || 0;
        var value3 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_10__NoOfPatrols').val()) || 0;
        var value4 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_11__NoOfPatrols').val()) || 0;
        var value5 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_12__NoOfPatrols').val()) || 0;
        var value6 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_13__NoOfPatrols').val()) || 0;
        var value7 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_14__NoOfPatrols').val()) || 0;
        var value8 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_15__NoOfPatrols').val()) || 0;
        // Calculate the sum
        var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
        // Update the value in textbox2
        $('#monthlyHrsAdhoc_8').val(sum);


    }

}

function calculateSumOfTextBoxValues4Adhoc() {

    var check = $("#positionfilterGuardAdhoc_16").prop('checked');
    if ($("#positionfilterGuardAdhoc_16").prop('checked') == true) {

        // Get the values from textbox1 and convert them to numbers
        var value1 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_16__NoOfPatrols').val()) || 0;
        var value2 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_17__NoOfPatrols').val()) || 0;
        var value3 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_18__NoOfPatrols').val()) || 0;
        var value4 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_19__NoOfPatrols').val()) || 0;
        var value5 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_20__NoOfPatrols').val()) || 0;
        var value6 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_21__NoOfPatrols').val()) || 0;
        var value7 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_22__NoOfPatrols').val()) || 0;
        var value8 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_23__NoOfPatrols').val()) || 0;

        // Calculate the sum
        var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
        // Update the value in textbox2
        $('#monthlyHrsAdhoc_16').val(sum);


    }

}

    //Adhoc end




// ################## Contracted Manning Edit End ###################



function ShowKpiModelChoice() {
    $('#div_site_settings').html('');
    var csnme = $('#dglClientSiteIdActionList option:selected').text();
    var csid = $('#dglClientSiteIdActionList option:selected').val();
    $('#client_site_name').text(csnme)
    var choice = $('#modelchoice').val();

    $('#div_site_settings').load('/RadioCheckV2?handler=ClientSiteKpiSettings&siteId=' + csid, function () {
        // This function will be executed after the content is loaded
        // window.sharedVariable = button.data('cs-id');
        // console.log('Load operation completed!');
        // You can add your additional code or actions here
        // console.log(csnme);  
        const activeTab = localStorage.getItem('activeTab');
        if (activeTab == 'contracted-manning-tabadhoc') {
            $('#contracted-manning-tabadhoc').tab('show');
        }
        else {
            $('#contracted-manning-tab').tab('show');
        }
        if (choice == 'RCACTIONLIST')
            $('#div_kpi_rc_contractedmanning').html('');
        else if (choice == 'CONTRACTEDMANNING')
            $('#div_kpi_rc_action_list').html('');
        if ($('#txtguardGuardRCAccess').val() == 'True' || $('#txtguardGuardRCHRAccess').val() == 'True' || $('#txtguardGuardRCLiteAccess').val() == 'True') { 
            $('#div_kpi_rc_contractedmanning #showDivButton').prop('disabled', true);
            $('#div_kpi_rc_contractedmanning #ClientSite_Status').prop('disabled', true);
            $('#div_kpi_rc_contractedmanning #scheduleisActive').prop('disabled', true);
            $('#div_kpi_rc_contractedmanning #delete_worker').prop('disabled', true);
            $('#div_kpi_rc_contractedmanning #save_site_manning_settings').prop('disabled', true);
            
        }
    });
}

// ################## RC Action List Edit Start ###################
// ---> Need to refer Kpi settings screen also


function getFormattedDate(value, withTime) {
    if (value !== '') {
        const date = new Date(value);
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        if (withTime)
            return date.getDate() + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
        else
            return date.getDate() + ' ' + months[date.getMonth()] + ' ' + date.getFullYear();
    }
}

$('#btn_Edit_ActionList').on('click', function (event) {
    var csnme = $('#dglClientSiteIdActionList option:selected').text();
    var csid = $('#dglClientSiteIdActionList option:selected').val();
    if (csnme == '' || csid == '') {
        console.log('Nothing selected...')
        alert('Please select a site to edit.');
        return;
    }

    $('#modelchoice').val('RCACTIONLIST');
    $('#kpi-settings-modal').modal('show');

});

/*Rc Action List Image Upload start*/
$('#div_site_settings').on('change', '#upload_summary_imageRcList', function () {

    const file = $('#upload_summary_imageRcList').prop("files")[0];
    if (file) {
        const scheduleId = $("#scheduleId").val();
        const rcListId = $("#Mdl_Settings_RCList_Id").val();
        var DateTime = luxon.DateTime;
        var dt1 = DateTime.local();
        const updatetime = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS');
        const formData = new FormData();
        formData.append("SummaryImage", file);
        formData.append("ScheduleId", scheduleId);
        formData.append("id", rcListId);
        formData.append("updatetime", updatetime);

        $.ajax({
            type: 'POST',
            url: '/RadioCheckV2?handler=UploadRCImage',
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                setSummaryImageRCList(data);
                $('#dglClientSiteIdActionList').trigger('change');
            }
        }).always(function () {
            $('#upload_summary_imageRcList').val('');
        });
    }
});
function setSummaryImageRCList(summaryImage) {
    if (summaryImage) {
        $('#summary_imageRC').html(summaryImage.fileName);
        var imagePath = $('#summary_imageRC').text().trim();
        $('#Mdl_Settings_RCImagepath').val(imagePath);
        $('#summary_image_updatedRC').html(getFormattedDate(summaryImage.lastUpdated, true));
        var imagePathdate = $('#summary_image_updatedRC').text().trim();
        $('#Mdl_Settings_RCImageDateandTime').val(imagePathdate);

        const myArray = summaryImage.imagepath.split(":-:");
        $('#download_summary_imageRCList').attr('href', myArray[1]);
        $('#download_summary_imageRCList').attr('download', myArray[0]);
        $("#download_summary_imageRCList").attr("target", "_blank");

        $('#download_summary_imageRCList').show();
        $('#delete_summary_image').show();
    }
}
$('#div_site_settings').on('click', '#delete_summary_imageRC', function () {
    if (confirm('Are you sure want to delete this file?')) {
        var check = $('#Mdl_Settings_RCImagepath').val();
        $.ajax({
            url: '/RadioCheckV2?handler=DeleteRCImage&imageName=' + $('#Mdl_Settings_RCImagepath').val(),
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.status) {
                clearSummaryImageRC();
                $('#dglClientSiteIdActionList').trigger('change');
            }
        }).fail(function () {
            console.log('error')
        });
    }
});
function clearSummaryImageRC() {
    $('#Mdl_Settings_RCImagepath').val('');
    $('#Mdl_Settings_RCImageDateandTime').val('');
    $('#summary_imageRC').html('');
    $('#summary_image_updatedRC').html('');
    $("#download_summary_imageRCList").removeAttr("href");
    $('#download_summary_imageRCList').removeAttr('download');
    $('#download_summary_imageRCList').show();
    $('#delete_summary_image').hide();
}
/*Rc Action List Image Upload stop*/

//RC Action List Save start

$('#div_site_settings').on('click', '#save_site_RC', function () {
    var List = $('#frm_Kpi_ActionList').serialize();
    console.log(List);
    $.ajax({
        url: '/RadioCheckV2?handler=ClientSiteRCActionList',
        type: 'POST',
        data: $('#frm_Kpi_ActionList').serialize(),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.status) {
            alert('Saved successfully');
            // refresh calling model
            $('#dglClientSiteIdActionList').trigger('change');
            $('#kpi-settings-modal').modal('hide');
        }
        else {
            alert(result.message);
        }
    }).fail(function () { });
});
//RC Action List Save stop


$('#div_site_settings').on('click', '#delete_site_RCList', function () {
    if (confirm('Are you sure you want to delete?')) {
        $.ajax({
            url: '/RadioCheckV2?handler=DeleteRC',
            type: 'POST',
            data: { RCId: $("#Mdl_Settings_RCList_Id").val() },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#Mdl_Settings_Site_Alarm_Keypad_code').val('');
                $('#Mdl_Settings_site_Physical_key').val('');
                $('#Mdl_Settings_Site_Combination_Look').val('');
                $('#Mdl_Settings_Action1').val('');
                $('#Mdl_Settings_Action2').val('');
                $('#Mdl_Settings_Action3').val('');
                $('#Mdl_Settings_Action4').val('');
                $('#Mdl_Settings_Action5').val('');
                $('#Mdl_Settings_userInput').val('');
                $.ajax({
                    url: '/RadioCheckV2?handler=DeleteRCImage&scheduleId=' + $('#Mdl_Settings_RCImagepath').val(),
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (data) {
                    if (data.status) {
                        clearSummaryImageRC();
                        $('#dglClientSiteIdActionList').trigger('change');
                    }
                }).fail(function () {
                    console.log('error')
                });
            }
            else
                alert(result.message);
        }).fail(function () { });
    }
});


// ################## RC Action List Edit End ###################
/*p4-16 Data dump task1-start*/
var guardSettings = $('#guard_settings_for_control_room').DataTable({
    pageLength: 10,
    autoWidth: false,
    ajax: '/GuardDetails?handler=ActiveGuards',
    columns: [
        //p4-105 new button-start
        {
            className: 'dt-control',
            orderable: false,
            data: null,
            width: '2%',
            defaultContent: '',
        },
        //p4-105 new button-end
        { data: 'name', width: "22%" },
        { data: 'securityNo', width: "12%" },
        { data: 'initial', orderable: false, width: "3%" },
        { data: 'mobile', width: "15%" },
        { data: 'email', width: "11%" },
        {
            data: 'provider',
            width: '23%',
            render: function (data, type, row) {
                var provider = row.provider ? row.provider : '';
                if (provider !== '') {
                    return '&nbsp;&nbsp;&nbsp;' + provider +
                        '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#crmSupplierDetailsModal" data-id="' + provider + '"></i>';
                } else {
                    return '';
                }
            }
        }

    ],
    'createdRow': function (row, data, index) {
        //if (data.detail.initialCallTime !== null) {
        //    $('td', row).eq(1).html(convertDateTimeString(data.detail.initialCallTime));
        //    /*to display the color yellow-start*/
        //    if (data.detail.entryTime == null && data.detail.sentInTime == null && data.detail.exitTime == null) {
        //        $('td', row).eq(1).addClass('initial-call-colour');
        //        $('td', row).eq(2).addClass('initial-call-colour');
        //        $('td', row).eq(3).addClass('initial-call-colour');
        //        $('td', row).eq(4).addClass('initial-call-colour');
        //        $('td', row).eq(5).addClass('initial-call-colour');
        //        $('td', row).eq(6).addClass('initial-call-colour');
        //        $('td', row).eq(7).addClass('initial-call-colour');
        //        $('td', row).eq(8).addClass('initial-call-colour');
        //        $('td', row).eq(9).addClass('initial-call-colour');
        //        $('td', row).eq(10).addClass('initial-call-colour');
        //        $('td', row).eq(11).addClass('initial-call-colour');
        //    }
        //    /*to display the color yellow-end*/
        //}
        //if (data.detail.entryTime !== null) {
        //    $('td', row).eq(2).html(convertDateTimeString(data.detail.entryTime));
        //    /*to display the color green for entry time-start*/
        //    if (data.detail.sentInTime == null && data.detail.exitTime == null) {
        //        $('td', row).eq(1).addClass('entry-time-colour');
        //        $('td', row).eq(2).addClass('entry-time-colour');
        //        $('td', row).eq(3).addClass('entry-time-colour');
        //        $('td', row).eq(4).addClass('entry-time-colour');
        //        $('td', row).eq(5).addClass('entry-time-colour');
        //        $('td', row).eq(6).addClass('entry-time-colour');
        //        $('td', row).eq(7).addClass('entry-time-colour');
        //        $('td', row).eq(8).addClass('entry-time-colour');
        //        $('td', row).eq(9).addClass('entry-time-colour');
        //        $('td', row).eq(10).addClass('entry-time-colour');
        //        $('td', row).eq(11).addClass('entry-time-colour');
        //    }
        //    /*to display the color green for entry time-end*/
        //}
        //if (data.detail.sentInTime !== null) {
        //    $('td', row).eq(3).html(convertDateTimeString(data.detail.sentInTime));
        //    /*to display the color green for sent in time-start*/
        //    if (data.detail.exitTime == null) {
        //        $('td', row).eq(1).addClass('entry-time-colour');
        //        $('td', row).eq(2).addClass('entry-time-colour');
        //        $('td', row).eq(3).addClass('entry-time-colour');
        //        $('td', row).eq(4).addClass('entry-time-colour');
        //        $('td', row).eq(5).addClass('entry-time-colour');
        //        $('td', row).eq(6).addClass('entry-time-colour');
        //        $('td', row).eq(7).addClass('entry-time-colour');
        //        $('td', row).eq(8).addClass('entry-time-colour');
        //        $('td', row).eq(9).addClass('entry-time-colour');
        //        $('td', row).eq(10).addClass('entry-time-colour');
        //        $('td', row).eq(11).addClass('entry-time-colour');
        //    }
        //    /*to display the color green for sent in time-end*/
        //}
        //if (data.detail.exitTime !== null) {
        //    $('td', row).eq(4).html(convertDateTimeString(data.detail.exitTime));
        //    /*to display the color green for exit  time-start*/

        //    $('td', row).eq(1).addClass('exit-time-colour');
        //    $('td', row).eq(2).addClass('exit-time-colour');
        //    $('td', row).eq(3).addClass('exit-time-colour');
        //    $('td', row).eq(4).addClass('exit-time-colour');
        //    $('td', row).eq(5).addClass('exit-time-colour');
        //    $('td', row).eq(6).addClass('exit-time-colour');
        //    $('td', row).eq(7).addClass('exit-time-colour');
        //    $('td', row).eq(8).addClass('exit-time-colour');
        //    $('td', row).eq(9).addClass('exit-time-colour');
        //    $('td', row).eq(10).addClass('exit-time-colour');
        //    $('td', row).eq(11).addClass('exit-time-colour');

        //    /*to display the color green for exit  time-end*/
        //}
        //if (data.detail.exitTime == null) {
        //    $('td', row).eq(4).html('<button type="button" class="btn btn-success btn-exit-quick">E</button> ');
        //}
    }
});
/*p4-16 Data dump task1-end*/

/*P4-105 NEW BUTTON-START*/
$('#guard_settings_for_control_room tbody').on('click', 'td.dt-control', function () {
    var tr = $(this).closest('tr');
    var row = guardSettings.row(tr);

    if (row.child.isShown()) {
        row.child.hide();
        tr.removeClass('shown');
    } else {
        row.child();
        row.child(format_guard_settings_for_control_room_child_row(row.data()), 'bg-light').show();
        tr.addClass('shown');
    }
});
function fetchGuardLogBookDetails(guardId) {
    $.ajax({
        url: '/GuardDetails?handler=LastTimeLogin',
        method: 'GET',
        data: { guardId: guardId },
        success: function (data) {
            // Destroy the existing DataTable instance if it exists
            if ($.fn.DataTable.isDataTable('#ActiveGuardsLogBookDetails')) {
                $('#ActiveGuardsLogBookDetails').DataTable().destroy();
            }

            $('#ActiveGuardsLogBookDetails').DataTable({
                autoWidth: false,
                ordering: false,
                searching: false,
                paging: false,
                info: false,
                data: data, // Use the data fetched from the AJAX request
                columns: [
                    {
                        data: 'loginDate',
                        width: "50%",
                        render: function (data, type, row) {
                            // Convert the date string to a JavaScript Date object
                            var date = new Date(data);

                            // Format the date to display only the date part without the time
                            var formattedDate = date.toLocaleDateString('en-GB', {
                                day: '2-digit',
                                month: '2-digit',
                                year: 'numeric',
                                hour: '2-digit',
                                minute: '2-digit',
                                second: '2-digit'
                            });
                            var additionalData = row.eventDateTimeZoneShort;
                            if (additionalData != null) {
                                return formattedDate + ' (' + additionalData + ')';
                            } else {
                                return formattedDate;
                            }
                        }
                    },
                    {
                        data: 'clientSite', width: "100%",
                        render: function (data, type, row) {

                            var name1 = data.name;

                            return name1;

                        }
                    },
                ]
            });
        },
        error: function (xhr, status, error) {
            console.error('Error fetching data:', error);
        }
    });
}
$('#guard_settings_for_control_room tbody').on('click', '#btnLogBookDetailsByGuard', function () {
    $('#guardLogBookInfoModal').modal('show');
    isPaused = true;
    var GuardName = $(this).closest("tr").find("td").eq(0).text();
    var GuardId = $(this).closest("tr").find('td').eq(3).find('#GuardId').val();
    var ClientSiteId = $(this).closest("tr").find('td').eq(1).find('#ClientSiteId').val();
    $('#txtClientSiteId').val(ClientSiteId);
    $('#txtGuardId').val(GuardId);
    // $('#lbl_GuardActivityHeader').val($(this).closest("tr").find("td").eq(2).text() + 'Log Book Details');
    $('#lbl_GuardActivityHeader').text(GuardName + '-' + 'Log Book Details');
    fetchGuardLogBookDetails(GuardId)
});
function format_guard_settings_for_control_room_child_row(d) {
    if (d.isActive) {
        // Check if data.guardlogins.logindate is present
        if (d.loginDate) {
            cellValue = '<i class="fa fa-check-circle text-success"></i>' +
                '[' +
                '<a href="#guardLogBookInfoModal" id="btnLogBookDetailsByGuard">1</a>' +
                ']<input type="hidden" id="GuardId" value="' + d.id + '">';
        } else {
            cellValue = '<i class="fa fa-check-circle text-success"></i>' +
                '<input type="hidden" id="GuardId" value="' + d.id + '">';
        }
        if (($('#txtguardGuardRCAccess').val() == 'False' && $('#txtguardGuardRCHRAccess').val() == 'False') || ($('#txtguardGuardRCAccess').val() == '' && $('#txtguardGuardRCHRAccess').val() == '')) { 
        if (d.dateEnrolled) {
            cellValue += '<br/> <span class="small">Enrolled: ' + getFormattedDate(new Date(d.dateEnrolled), null, ' ') + '</span>';
            }
        }
    } else {

        cellValue = '<i class="fa fa-times-circle text-danger"></i>';
    }
    return (
        '</td>' +
        '<table cellpadding="7" class="col-md-12" cellspacing="0"  border="0" style="padding-left:50px;">' +
        '<tr>' +
        '<th  style="background-color:#EAF0ED"><center>State</center></th>' +
        '<th  style="background-color:#EAF0ED"><center>Gender</center></th>' +
        '<th  style="background-color:#EAF0ED"><center>Sites</center></th>' +
        '<th  style="background-color:#EAF0ED"><center>Active</center></th>' +

        '</tr>' +
        '<tr>' +
        '<td>' + convertDbString(d.state) + '</td>' +
        '<td>' + convertDbString(d.gender) + '</td>' +
        '<td>' + convertDbString(d.clientSites) + '</td>' +
        '<td>' + cellValue + '</td>' +

        '</tr>' +

        '</table>'
    );

}
function convertDbString(value) { return (value === null || value === undefined) ? '' : value; }
let guardDetailsSettings = $('#specificGuardDetailsTable').DataTable({
    pageLength: 10,
    autoWidth: false,
   
    columns: [
        //p4-105 new button-start
        {
            className: 'dt-control',
            orderable: false,
            data: null,
            width: '2%',
            defaultContent: '',
        },
        //p4-105 new button-end
        { data: 'name', width: "22%" }
        //{ data: 'securityNo', width: "10%" },
        //{ data: 'initial', orderable: false, width: "3%" },
        //{ data: 'mobile', width: "15%" },
        //{ data: 'email', width: "11%" },
        //{
        //    data: 'provider',
        //    width: '23%',
        //    render: function (data, type, row) {
        //        var provider = row.provider ? row.provider : '';
        //        if (provider !== '') {
        //            return '&nbsp;&nbsp;&nbsp;' + provider +
        //                '<i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#crmSupplierDetailsModal" data-id="' + provider + '"></i>';
        //        } else {
        //            return '';
        //        }
        //    }
        //}

    ],
    'createdRow': function (row, data, index) {
        var name = data.name
        //if (data.detail.initialCallTime !== null) {
        //    $('td', row).eq(1).html(convertDateTimeString(data.detail.initialCallTime));
        //    /*to display the color yellow-start*/
        //    if (data.detail.entryTime == null && data.detail.sentInTime == null && data.detail.exitTime == null) {
        //        $('td', row).eq(1).addClass('initial-call-colour');
        //        $('td', row).eq(2).addClass('initial-call-colour');
        //        $('td', row).eq(3).addClass('initial-call-colour');
        //        $('td', row).eq(4).addClass('initial-call-colour');
        //        $('td', row).eq(5).addClass('initial-call-colour');
        //        $('td', row).eq(6).addClass('initial-call-colour');
        //        $('td', row).eq(7).addClass('initial-call-colour');
        //        $('td', row).eq(8).addClass('initial-call-colour');
        //        $('td', row).eq(9).addClass('initial-call-colour');
        //        $('td', row).eq(10).addClass('initial-call-colour');
        //        $('td', row).eq(11).addClass('initial-call-colour');
        //    }
        //    /*to display the color yellow-end*/
        //}
        //if (data.detail.entryTime !== null) {
        //    $('td', row).eq(2).html(convertDateTimeString(data.detail.entryTime));
        //    /*to display the color green for entry time-start*/
        //    if (data.detail.sentInTime == null && data.detail.exitTime == null) {
        //        $('td', row).eq(1).addClass('entry-time-colour');
        //        $('td', row).eq(2).addClass('entry-time-colour');
        //        $('td', row).eq(3).addClass('entry-time-colour');
        //        $('td', row).eq(4).addClass('entry-time-colour');
        //        $('td', row).eq(5).addClass('entry-time-colour');
        //        $('td', row).eq(6).addClass('entry-time-colour');
        //        $('td', row).eq(7).addClass('entry-time-colour');
        //        $('td', row).eq(8).addClass('entry-time-colour');
        //        $('td', row).eq(9).addClass('entry-time-colour');
        //        $('td', row).eq(10).addClass('entry-time-colour');
        //        $('td', row).eq(11).addClass('entry-time-colour');
        //    }
        //    /*to display the color green for entry time-end*/
        //}
        //if (data.detail.sentInTime !== null) {
        //    $('td', row).eq(3).html(convertDateTimeString(data.detail.sentInTime));
        //    /*to display the color green for sent in time-start*/
        //    if (data.detail.exitTime == null) {
        //        $('td', row).eq(1).addClass('entry-time-colour');
        //        $('td', row).eq(2).addClass('entry-time-colour');
        //        $('td', row).eq(3).addClass('entry-time-colour');
        //        $('td', row).eq(4).addClass('entry-time-colour');
        //        $('td', row).eq(5).addClass('entry-time-colour');
        //        $('td', row).eq(6).addClass('entry-time-colour');
        //        $('td', row).eq(7).addClass('entry-time-colour');
        //        $('td', row).eq(8).addClass('entry-time-colour');
        //        $('td', row).eq(9).addClass('entry-time-colour');
        //        $('td', row).eq(10).addClass('entry-time-colour');
        //        $('td', row).eq(11).addClass('entry-time-colour');
        //    }
        //    /*to display the color green for sent in time-end*/
        //}
        //if (data.detail.exitTime !== null) {
        //    $('td', row).eq(4).html(convertDateTimeString(data.detail.exitTime));
        //    /*to display the color green for exit  time-start*/

        //    $('td', row).eq(1).addClass('exit-time-colour');
        //    $('td', row).eq(2).addClass('exit-time-colour');
        //    $('td', row).eq(3).addClass('exit-time-colour');
        //    $('td', row).eq(4).addClass('exit-time-colour');
        //    $('td', row).eq(5).addClass('exit-time-colour');
        //    $('td', row).eq(6).addClass('exit-time-colour');
        //    $('td', row).eq(7).addClass('exit-time-colour');
        //    $('td', row).eq(8).addClass('exit-time-colour');
        //    $('td', row).eq(9).addClass('exit-time-colour');
        //    $('td', row).eq(10).addClass('exit-time-colour');
        //    $('td', row).eq(11).addClass('exit-time-colour');

        //    /*to display the color green for exit  time-end*/
        //}
        //if (data.detail.exitTime == null) {
        //    $('td', row).eq(4).html('<button type="button" class="btn btn-success btn-exit-quick">E</button> ');
        //}
    },
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
                        .before('<tr class="group bg-light text-dark"><td colspan="25">' + group + '</td></tr>');

                    last = group;
                }
            });
    },
});
$('#specificGuardDetailsModal').on('shown.bs.modal', function (event) {
    isPaused = true;



    const button = $(event.relatedTarget);

    $('#txt_SpecificGuardDetailsId').val(button.data('id'));


    //guardDetailsSettings.ajax.reload();
    $.ajax({
        url: '/RadioCheckV2?handler=Guards&id='+ $('#txt_SpecificGuardDetailsId').val(),
        type: 'GET',
        dataType: 'json',
        //data: function (d) { d.id = $('#txt_SpecificGuardDetailsId').val(); }
    })
        .done(function (response) {
            $('#guardcontent').html(response.name);
            guardDetailsSettings.clear().rows.add(response).draw();
        });

});
/*P4 - 105 NEW BUTTON - END*/
$('#search_client_siteSteps').on('keyup', function (e) {

    var inputValue = $(this).val();
    if (inputValue.length >= 3 && inputValue.match(/[a-zA-Z]/)) {
        e.preventDefault();

        gridSiteSearchradio.reload({ typeId: $('#sel_client_type').val(), searchTerm: $(this).val() });
        $('#logbook-modalRadio').modal('show');
        //alert('Letter typed and Enter pressed: ' + inputValue);
    }

});
let gridSiteSearchradio;
gridSiteSearchradio = $('#client_site_RadioSearch').grid({
    dataSource: '/RadioCheckV2?handler=ClientSitesRadioSearch',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    columns: [
        { field: 'typeId', hidden: true },
        { field: 'clientType', title: 'Client Type', width: 180, renderer: function (value, record) { return value ? value.name : ''; } },
        { field: 'name', title: 'Client Site', width: 180, editor: false },
        { width: 100, renderer: settingsButtonRenderer },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});



// search two textbox steps 


$('#search_client_site2').on('keyup', function (e) {

    var inputValue = $(this).val();
    if (inputValue.length >= 3 && inputValue.match(/[a-zA-Z]/)) {
        e.preventDefault();

        gridSiteSearchradio2.reload({ typeId: $('#dglClientTypeActionList2').val(), searchTerm: $(this).val() });
        $('#logbook-modalRadioTwo').modal('show');
        //alert('Letter typed and Enter pressed: ' + inputValue);
    }

});
let gridSiteSearchradio2;
gridSiteSearchradio2 = $('#client_site_RadioSearchTwo').grid({
    dataSource: '/RadioCheckV2?handler=ClientSitesRadioSearch',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    columns: [
        { field: 'typeId', hidden: true },
        { field: 'clientType', title: 'Client Type', width: 180, renderer: function (value, record) { return value ? value.name : ''; } },
        { field: 'name', title: 'Client Site', width: 180, editor: false },
        { width: 100, renderer: settingsButtonRenderer },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

// search two textbox steps  end 


function settingsButtonRenderer(value, record) {

    var ClientTypeName = record.clientType.name;
    var ClientSiteName = record.name;
    var clientSiteId = record.id;
    var typeIdNew = record.typeId;

    return '<button class="btn btn-outline-success mt-2 del-schedule d-block" data-sch-id="' + ClientTypeName + '_' + clientSiteId + '_' + typeIdNew + '""><i class="fa fa-check mr-2" aria-hidden="true"></i>Select</button>';
}
$('#client_site_RadioSearch').on('click', '.del-schedule', function () {


    const ClientSiteName1 = $(this).attr('data-sch-id');
    const lastUnderscoreIndex = ClientSiteName1.lastIndexOf('_');
    var fields = ClientSiteName1.split('_');




    if (lastUnderscoreIndex !== -1) {
        const recordName = fields[0];
        const clientsiteid = fields[1];
        const typeId = fields[2];

        $('#dglClientTypeActionList option').each(function () {

            if ($(this).val() === typeId) {
                $(this).prop('selected', true);
                return false;
            }
        });
        sitebuttonSelectedClientSiteId = clientsiteid;
        $('#dglClientTypeActionList').val(typeId).trigger('change');
        var options = $('#dglClientSiteIdActionList option');
        $('#dglClientSiteIdActionList option').each(function () {

            var check = $(this).val();
            if ($(this).val() === clientsiteid) {
                $(this).prop('selected', true);
                return false;
            }
        });



        $('#logbook-modalRadio').modal('hide');
        $('#search_client_siteSteps').val('');
        $.ajax({
            url: '/RadioCheckV2?handler=ActionList',
            type: 'POST',
            data: {
                clientSiteId: clientsiteid
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data != null) {

                $('#dglClientSiteIdActionList').val(clientsiteid);
                $('#Site_Alarm_Keypad_code').val(data.siteAlarmKeypadCode);
                $('#Action1').val(data.action1);
                $('#site_Physical_key').val(data.sitephysicalkey);
                $('#Action2').val(data.action2);
                $('#Action3').val(data.action3);
                $('#Action4').val(data.action4);
                $('#Action5').val(data.action5);
                $('#Site_Combination_Look').val(data.siteCombinationLook);
                $('#txtComments').html(data.controlRoomOperator);

                if (data.imagepath != null) {
                    const myArray = data.imagepath.split(":-:");
                    $('#download_imageRCList').attr('href', myArray[1]);
                    $('#download_imageRCList').attr('download', myArray[0]);
                } else {
                    $('#download_imageRCList').removeAttr('href');
                    $('#download_imageRCList').removeAttr('download');
                }



            }
        });

    } else {
        console.log('Invalid data-sch-id format');
    }
});


$('#client_site_RadioSearchTwo').on('click', '.del-schedule', function () {
    const ClientSiteName1 = $(this).attr('data-sch-id');
    const lastUnderscoreIndex = ClientSiteName1.lastIndexOf('_');
    var fields = ClientSiteName1.split('_');

    if (lastUnderscoreIndex !== -1) {
        const recordName = fields[0];
        const clientsiteid = fields[1];
        const typeId = fields[2];

        // Select the correct type
        $('#dglClientTypeActionList2').val(typeId).trigger('change');

        const clientSiteControl = $('#dglClientSiteIdActionList2');
        clientSiteControl.html('');
        $.ajax({
            url: '/RadioCheckV2?handler=ClientSitesNew',
            type: 'GET',
            data: {
                typeId: typeId

            },
            dataType: 'json',
            success: function (data) {
                $('#dglClientSiteIdActionList2').append(new Option('Select', '', true, true));
                data.map(function (site) {
                    $('#dglClientSiteIdActionList2').append(new Option(site.name, site.id, false, false));
                });

                $('#dglClientSiteIdActionList2').val(clientsiteid)
            }
        });

        

        // Clear search field
        $('#search_client_site2').val('');

       
            $('#logbook-modalRadioTwo').modal('hide');
       
    } else {
        console.log('Invalid data-sch-id format');
    }
});

// funsion Start
const today = new Date();
const start = new Date(today.getFullYear(), today.getMonth(), 2);
//Start fusion report in auditlog08072024
$('#fusionAudtitFromDate').val(start.toISOString().substr(0, 10));
var systemDate = $('#fusionAudtitToDateVal').val();
//var dateObject = new Date().toISOString().substr(0, 10);
$('#fusionAudtitToDate').val(systemDate);

let gridsitefusionLog;
gridsitefusionLog = $('#fusion_site_log').grid({
    dataSource: '/Fusion?handler=DailyGuardFusionSiteLogs',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    grouping: { groupBy: 'Date' },
    primaryKey: 'id',
    columns: [
        { field: 'clientSiteId', hidden: true },
        { field: 'notificationCreatedTime', title: 'Time', width: 100, renderer: function (value, record) { return renderDateTimefusion(value, record, false); } },
        {
            field: 'notes', title: 'Event / Notes', width: 350,
            renderer: function (value, record) {
                var activityType = record.activityType == undefined ? '' : record.activityType.trim();
                if (activityType == 'IR') {

                    return record.notes + '<br>' + '<a href="https://c4istorage1.blob.core.windows.net/irfiles/' + record.notes.substr(0, 8) + '/' + record.notes + '" target="_blank">Click here</a>';
                }
                else {
                    return record.notes;
                }
            }
        },
        { field: 'activityType', title: 'Source', width: 50 },
        { field: 'siteName', title: 'Client Site', width: 150 },
        { field: 'guardName', title: 'Guard Initials', width: 150, renderer: renderGuardInitialColumn }
    ],
    paramNames: { page: 'pageNo' },
    pager: { limit: 100, sizes: [10, 50, 100, 500] }
});


//$('#fusionClientSiteId').select({
//    placeholder: 'Select',
//    theme: 'bootstrap4'
//});

$('#fusionClientSiteId').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});

$('#fusionClientSiteId').on('change', function () {
    gridsitefusionLog.clear();
});

$('#fusionClientType').on('change', function () {
    gridsitefusionLog.clear();
    const clientTypeId = $(this).val();
    const clientSiteControl = $('#fusionClientSiteId');
    clientSiteControl.html('');
    $.ajax({

        url: '/Admin/Settings?handler=ClientSitesNew&typeId=' + clientTypeId,
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            data.map(function (site) {
                clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
            });
            clientSiteControl.multiselect('rebuild');


        }
    });


});




$('#expand_fusion_audits').on('click', function () {
    gridsitefusionLog.expandAll();
});

$('#collapse_fusion_audits').on('click', function () {
    gridsitefusionLog.collapseAll();
});



function renderDateTimefusion(value, record) {
    // p6#73 timezone bug - Modified by binoy 29-01-2024
    if (record.eventDateTime != null && record.eventDateTime != '') {
        const date = new Date(record.eventDateTime);
        var DateTime = luxon.DateTime;
        var dt1 = DateTime.fromJSDate(date);
        var dt = dt1.toFormat('dd LLL yyyy @ HH:mm') + ' Hrs ' + record.eventDateTimeZoneShort;
        return dt;
    }
    else if (value !== '') {
        const date = new Date(value);
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        let day = date.getDate();

        if (day < 10) {
            day = '0' + day;
        }

        return day + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
    }
}

if (gridsitefusionLog) {
    const bg_color_pale_yellow = '#fcf8d1';
    const bg_color_pale_red = '#ffcccc';
    const bg_color_white = '#ffffff';
    const irEntryTypeIsAlarm = 2;

    gridsitefusionLog.on('rowDataBound', function (e, $row, id, record) {
        let rowColor = bg_color_white;
        if (record.irEntryType) {
            rowColor = record.irEntryType === irEntryTypeIsAlarm ? bg_color_pale_red : bg_color_pale_yellow;
        }
        $row.css('background-color', rowColor);
    });
}


$('#btnGeneratefusionAuditReport').on('click', function () {
    if ($('#fusionClientSiteId').val().length === 0) {
        alert('Please select a client site');
        return;
    }
    //if ($('#fusionClientSiteId').val() === '') {
    //    alert('Please select a client site');
    //    return;
    //}
    gridsitefusionLog.clear();
    gridsitefusionLog.reload({
        clientSiteIds: $('#fusionClientSiteId').val().join(';'),
        logFromDate: $('#fusionAudtitFromDate').val(),
        logToDate: $('#fusionAudtitToDate').val(),
        excludeSystemLogs: 0
    });
     GetRCCharts();
});
$('#btncount_by_numberofduressPerWeek').on('click', function () {
    $('#modelRCDuressEntriesGraphForWeek').modal('show');
});
$('#btncount_by_numberofduressPerMonth').on('click', function () {
    $('#modelRCDuressEntriesGraphForMonth').modal('show');
});
$('#btncount_by_numberofduressPerYear').on('click', function () {
    $('#modelRCDuressEntriesGraphForYear').modal('show');
});

$('#btncount_by_numberoftimesrcpushedbycro').on('click', function () {
    $('#modelRCButtonPushedbyCRO').modal('show');
});
$('#btncount_by_numberofguardswnenttoprealarm').on('click', function () {
    $('#modelRCGuardswenttoprealarm').modal('show');
});
$('#btncount_by_numberofguardswnentfromprealarmorangetored').on('click', function () {
    $('#modelRCGuardswentfromprealarmorangetored').modal('show');
});
function GetRCCharts() {
    $('#loader').show();
    if (window.myChart1 != undefined)
        window.myChart1.destroy();
    if (window.myChart2 != undefined)
        window.myChart2.destroy();
    if (window.myChart3 != undefined)
        window.myChart3.destroy();
    if (window.myChart4 != undefined)
        window.myChart4.destroy();
    if (window.myChart5 != undefined)
        window.myChart5.destroy();
    if (window.myChart6 != undefined)
        window.myChart6.destroy();
    if (window.myChart7 != undefined)
        window.myChart7.destroy();
    if (window.myChart8 != undefined)
        window.myChart8.destroy();
    if (window.myChart9 != undefined)
        window.myChart9.destroy();
    if (window.myChart10 != undefined)
        window.myChart10.destroy();
    if (window.myChart11 != undefined)
        window.myChart11.destroy();
    if (window.myChart12 != undefined)
        window.myChart12.destroy();
    if (window.myChart13 != undefined)
        window.myChart13.destroy();
    if (window.myChart14 != undefined)
        window.myChart14.destroy();
    if (window.myChart15 != undefined)
        window.myChart15.destroy();
    if (window.myChart16 != undefined)
        window.myChart16.destroy();
    if (window.myChart17 != undefined)
        window.myChart17.destroy();
    if (window.myChart18 != undefined)
        window.myChart18.destroy();
    $('#loader-p').show();
    $.ajax({
        url: '/Fusion?handler=GenerateRCGraphs',
        type: 'GET',
        data: {
            clientSiteId: $('#fusionClientSiteId').val(),
            logFromDate: $('#fusionAudtitFromDate').val(),
            logToDate: $('#fusionAudtitToDate').val(),
            excludeSystemLogs: 0
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        
        /* expanding grapph - start*/


        $('#count_by_numberofduressperweek').html(response.rcChartTypesForWeekNewCount);
        if (response.rcChartTypesForWeekNewCount != 0) {
            drawPieChartUsingChartJsChartRCForWeek(response.chartData.rcChartTypesForWeekNew);
        }


        $('#count_by_numberofduresspermonth').html(response.rcChartTypesForMonthNewCount);
        if (response.rcChartTypesForMonthNewCount != 0) {
            drawPieChartUsingChartJsChartRCForMonth(response.chartData.rcChartTypesForMonthNew);
        }

        $('#count_by_numberofduressperyear').html(response.rcChartTypesForYearNewCount);
        if (response.rcChartTypesForYearNewCount != 0) {
            drawPieChartUsingChartJsChartRCForYear(response.chartData.rcChartTypesForYearNew);
        }

        $('#count_by_numberoftimesrcpushedbycro').html(response.rcChartTypesCROCountnew);
        if (response.rcChartTypesCROCountnew != 0) {
            drawPieChartUsingChartJsChartRCButton(response.chartData.rcChartTypesCRONew);
        }

        $('#count_by_numberofguardswnenttoprealarm').html(response.rcChartTypesGuardsPrealarmCountnew);
        if (response.rcChartTypesGuardsPrealarmCountnew != 0) {
            drawPieChartUsingChartJsChartRCForNumberofGuardstoPrealarm(response.chartData.rcChartTypesGuardsPrealarmNew);
        }
        $('#count_by_numberofguardswnentfromprealarmorangetored').html(response.rcChartTypesGuardsFromPrealarmCountnew);
        if (response.rcChartTypesGuardsFromPrealarmCountnew != 0) {
            drawPieChartUsingChartJsChartRCForNumberofGuardsFromPrealarm(response.chartData.rcChartTypesGuardsFromPrealarmNew);
        }


    }).fail(function () {
    }).always(function () {
        $('#loader').hide();
    });
}
function drawPieChartUsingChartJsChartRCForWeek(dataValue) {

    var labels = dataValue.map(function (e) {
        return e.dateRange;
    });
    var data2 = dataValue.map(function (e) {
        return e.recordCountNew;
    });
    // Data for the pie chart
    const data = {
        labels: labels,
        datasets: [{
            data: data2, // Values for each slice

        },
        ],
        datalabels: {
            // display labels for this specific dataset
            display: true
        }
    };


    var canvas = document.getElementById("bar_chart_by_rc_duress_forweek");
    var canvas2 = document.getElementById("bar_chart_by_rc_duress_forweek1");
    //var canvas3 = document.getElementById("pie_chart_ir_by_areaward3");
    if (canvas !== null) {
        const ctx = document.getElementById('bar_chart_by_rc_duress_forweek').getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        window.myChart7 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 2,
                        bottom: 2
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {

                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },

                        outsidePadding: 4,
                        textMargin: 4

                    },

                }

            },


        });


    }



    if (canvas2 !== null) {
        const ctx2 = document.getElementById('bar_chart_by_rc_duress_forweek1').getContext('2d');
        ctx2.clearRect(0, 0, canvas2.width, canvas2.height);
        window.myChart8 = new Chart(ctx2, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0,radius: '80%',
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 20,
                        bottom: 20
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },
                        position: 'outside',
                        outsidePadding: 10,
                        textMargin: 10

                    },

                }

            },


        });
    }


    //if (canvas3 !== null) {
    //    const ctx3 = document.getElementById('pie_chart_ir_by_areaward3').getContext('2d');
    //    window.myChart6 = new Chart(ctx3, {
    //        type: 'pie',
    //        data: {
    //            labels: labels,
    //            datasets: [{
    //                label: '# of Votes',
    //                data: data2,
    //                backgroundColor: getColors(15),
    //                borderColor: [
    //                    'rgba(255, 99, 132, 1)',
    //                    'rgba(54, 162, 235, 1)',
    //                    'rgba(255, 206, 86, 1)',
    //                    'rgba(75, 192, 192, 1)',
    //                    'rgba(153, 102, 255, 1)',
    //                    'rgba(255, 159, 64, 1)'
    //                ],
    //                borderWidth: 0
    //            }]
    //        },
    //        options: {
    //            layout: {
    //                padding: {
    //                    left: 10,
    //                    right: 10,
    //                    top: 20,
    //                    bottom: 20
    //                }
    //            },
    //            maintainAspectRatio: false,
    //            plugins: {
    //                tooltip: {
    //                    enabled: true,
    //                    callbacks: {
    //                        label: function (context) {
    //                            let label = context.label + '(' + context.formattedValue + '%)'
    //                            return label;
    //                        }
    //                    }
    //                },
    //                legend: {
    //                    position: 'right',
    //                    labels: {
    //                        font: {
    //                            family: 'Arial',
    //                            size: 11
    //                        },

    //                        boxWidth: 10,
    //                        boxHeight: 10,
    //                        generateLabels(chart) {
    //                            const data = chart.data;
    //                            if (data.labels.length && data.datasets.length) {
    //                                const { labels: { pointStyle } } = chart.legend.options;

    //                                return data.labels.map((label, i) => {
    //                                    const meta = chart.getDatasetMeta(0);
    //                                    const style = meta.controller.getStyle(i);

    //                                    return {
    //                                        text: `${label} (${data['datasets'][0].data[i]}%)`,
    //                                        fillStyle: style.backgroundColor,
    //                                        strokeStyle: style.borderColor,
    //                                        lineWidth: style.borderWidth,
    //                                        borderWidth: 0,
    //                                        pointStyle: pointStyle,
    //                                        hidden: !chart.getDataVisibility(i),

    //                                        // Extra data used for toggling the correct item
    //                                        index: i
    //                                    };
    //                                });
    //                            }
    //                            return [];
    //                        }
    //                    }
    //                },
    //                labels: {
    //                    /* render:"value",*/
    //                    render: (args) => {

    //                        return args.value + '%';

    //                    },
    //                    position: 'outside',
    //                    outsidePadding: 10,
    //                    textMargin: 10

    //                },

    //            }

    //        },


    //    });
    //}

    function getColors(length) {
        let pallet = ["#4682b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2",
            "#7f7f7f", "#bcbd22", "#17becf",
            "#85144b", "#F012BE", "#3D9970", "#111111", "#AAAAAA"];
        let colors = [];

        for (let i = 0; i < length; i++) {
            colors.push(pallet[i % (pallet.length - 1)]);
        }

        return colors;
    }
    //// Configuration options
    //const options = {
    //    responsive: false,
    //    plugins: {
    //        legend: {
    //            position: 'right',
    //            onClick: function (event, legendItem) {
    //                const index = legendItem.index;
    //                const meta = this.chart.getDatasetMeta(0);
    //                meta.data[index].hidden = !meta.data[index].hidden;
    //                this.chart.update();
    //            }
    //        },
    //        datalabels: {
    //            backgroundColor: function (context) {
    //                return context.dataset.backgroundColor;
    //            },
    //            borderColor: 'white',
    //            borderRadius: 25,
    //            borderWidth: 2,
    //            color: 'white',
    //            display: function (context) {
    //                var dataset = context.dataset;
    //                var count = dataset.data.length;
    //                var value = dataset.data[context.dataIndex];
    //                return value > count * 1.5;
    //            }
    //        }
    //    }

    //};

    //// Create the pie chart
    //var canvas = document.getElementById("myPieChart");
    //if (canvas !== null) {
    //    const ctx = document.getElementById('myPieChart').getContext('2d');
    //    const myPieChart = new Chart(ctx, {
    //        type: 'pie',
    //        data: data,
    //        options: options
    //    });

    //}

}
function drawPieChartUsingChartJsChartRCForMonth(dataValue) {

    var labels = dataValue.map(function (e) {
        return e.dateRange;
    });
    var data2 = dataValue.map(function (e) {
        return e.recordCountNew;
    });
    // Data for the pie chart
    const data = {
        labels: labels,
        datasets: [{
            data: data2, // Values for each slice

        },
        ],
        datalabels: {
            // display labels for this specific dataset
            display: true
        }
    };


    var canvas = document.getElementById("bar_chart_by_rc_duress_formonth");
    var canvas2 = document.getElementById("bar_chart_by_rc_duress_formonth1");
    //var canvas3 = document.getElementById("pie_chart_ir_by_areaward3");
    if (canvas !== null) {
        const ctx = document.getElementById('bar_chart_by_rc_duress_formonth').getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        window.myChart9 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 2,
                        bottom: 2
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {

                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value +'%';

                        },

                        outsidePadding: 4,
                        textMargin: 4

                    },

                }

            },


        });


    }



    if (canvas2 !== null) {
        const ctx2 = document.getElementById('bar_chart_by_rc_duress_formonth1').getContext('2d');
        ctx2.clearRect(0, 0, canvas2.width, canvas2.height);
        window.myChart10 = new Chart(ctx2, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0, radius: '80%',
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 20,
                        bottom: 20
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },
                        position: 'outside',
                        outsidePadding: 10,
                        textMargin: 10

                    },

                }

            },


        });
    }


    //if (canvas3 !== null) {
    //    const ctx3 = document.getElementById('pie_chart_ir_by_areaward3').getContext('2d');
    //    window.myChart6 = new Chart(ctx3, {
    //        type: 'pie',
    //        data: {
    //            labels: labels,
    //            datasets: [{
    //                label: '# of Votes',
    //                data: data2,
    //                backgroundColor: getColors(15),
    //                borderColor: [
    //                    'rgba(255, 99, 132, 1)',
    //                    'rgba(54, 162, 235, 1)',
    //                    'rgba(255, 206, 86, 1)',
    //                    'rgba(75, 192, 192, 1)',
    //                    'rgba(153, 102, 255, 1)',
    //                    'rgba(255, 159, 64, 1)'
    //                ],
    //                borderWidth: 0
    //            }]
    //        },
    //        options: {
    //            layout: {
    //                padding: {
    //                    left: 10,
    //                    right: 10,
    //                    top: 20,
    //                    bottom: 20
    //                }
    //            },
    //            maintainAspectRatio: false,
    //            plugins: {
    //                tooltip: {
    //                    enabled: true,
    //                    callbacks: {
    //                        label: function (context) {
    //                            let label = context.label + '(' + context.formattedValue + '%)'
    //                            return label;
    //                        }
    //                    }
    //                },
    //                legend: {
    //                    position: 'right',
    //                    labels: {
    //                        font: {
    //                            family: 'Arial',
    //                            size: 11
    //                        },

    //                        boxWidth: 10,
    //                        boxHeight: 10,
    //                        generateLabels(chart) {
    //                            const data = chart.data;
    //                            if (data.labels.length && data.datasets.length) {
    //                                const { labels: { pointStyle } } = chart.legend.options;

    //                                return data.labels.map((label, i) => {
    //                                    const meta = chart.getDatasetMeta(0);
    //                                    const style = meta.controller.getStyle(i);

    //                                    return {
    //                                        text: `${label} (${data['datasets'][0].data[i]}%)`,
    //                                        fillStyle: style.backgroundColor,
    //                                        strokeStyle: style.borderColor,
    //                                        lineWidth: style.borderWidth,
    //                                        borderWidth: 0,
    //                                        pointStyle: pointStyle,
    //                                        hidden: !chart.getDataVisibility(i),

    //                                        // Extra data used for toggling the correct item
    //                                        index: i
    //                                    };
    //                                });
    //                            }
    //                            return [];
    //                        }
    //                    }
    //                },
    //                labels: {
    //                    /* render:"value",*/
    //                    render: (args) => {

    //                        return args.value + '%';

    //                    },
    //                    position: 'outside',
    //                    outsidePadding: 10,
    //                    textMargin: 10

    //                },

    //            }

    //        },


    //    });
    //}

    function getColors(length) {
        let pallet = ["#4682b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2",
            "#7f7f7f", "#bcbd22", "#17becf",
            "#85144b", "#F012BE", "#3D9970", "#111111", "#AAAAAA"];
        let colors = [];

        for (let i = 0; i < length; i++) {
            colors.push(pallet[i % (pallet.length - 1)]);
        }

        return colors;
    }
    //// Configuration options
    //const options = {
    //    responsive: false,
    //    plugins: {
    //        legend: {
    //            position: 'right',
    //            onClick: function (event, legendItem) {
    //                const index = legendItem.index;
    //                const meta = this.chart.getDatasetMeta(0);
    //                meta.data[index].hidden = !meta.data[index].hidden;
    //                this.chart.update();
    //            }
    //        },
    //        datalabels: {
    //            backgroundColor: function (context) {
    //                return context.dataset.backgroundColor;
    //            },
    //            borderColor: 'white',
    //            borderRadius: 25,
    //            borderWidth: 2,
    //            color: 'white',
    //            display: function (context) {
    //                var dataset = context.dataset;
    //                var count = dataset.data.length;
    //                var value = dataset.data[context.dataIndex];
    //                return value > count * 1.5;
    //            }
    //        }
    //    }

    //};

    //// Create the pie chart
    //var canvas = document.getElementById("myPieChart");
    //if (canvas !== null) {
    //    const ctx = document.getElementById('myPieChart').getContext('2d');
    //    const myPieChart = new Chart(ctx, {
    //        type: 'pie',
    //        data: data,
    //        options: options
    //    });

    //}

}
function drawPieChartUsingChartJsChartRCForYear(dataValue) {

    var labels = dataValue.map(function (e) {
        return e.dateRange;
    });
    var data2 = dataValue.map(function (e) {
        return e.recordCountNew;
    });
    // Data for the pie chart
    const data = {
        labels: labels,
        datasets: [{
            data: data2, // Values for each slice

        },
        ],
        datalabels: {
            // display labels for this specific dataset
            display: true
        }
    };


    var canvas = document.getElementById("bar_chart_by_rc_duress_foryear");
    var canvas2 = document.getElementById("bar_chart_by_rc_duress_foryear1");
    //var canvas3 = document.getElementById("pie_chart_ir_by_areaward3");
    if (canvas !== null) {
        const ctx = document.getElementById('bar_chart_by_rc_duress_foryear').getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        window.myChart11 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 2,
                        bottom: 2
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {

                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },

                        outsidePadding: 4,
                        textMargin: 4

                    },

                }

            },


        });


    }



    if (canvas2 !== null) {
        const ctx2 = document.getElementById('bar_chart_by_rc_duress_foryear1').getContext('2d');
        ctx2.clearRect(0, 0, canvas2.width, canvas2.height);
        window.myChart12 = new Chart(ctx2, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0,radius: '80%',
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 20,
                        bottom: 20
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },
                        position: 'outside',
                        outsidePadding: 10,
                        textMargin: 10

                    },

                }

            },


        });
    }


    //if (canvas3 !== null) {
    //    const ctx3 = document.getElementById('pie_chart_ir_by_areaward3').getContext('2d');
    //    window.myChart6 = new Chart(ctx3, {
    //        type: 'pie',
    //        data: {
    //            labels: labels,
    //            datasets: [{
    //                label: '# of Votes',
    //                data: data2,
    //                backgroundColor: getColors(15),
    //                borderColor: [
    //                    'rgba(255, 99, 132, 1)',
    //                    'rgba(54, 162, 235, 1)',
    //                    'rgba(255, 206, 86, 1)',
    //                    'rgba(75, 192, 192, 1)',
    //                    'rgba(153, 102, 255, 1)',
    //                    'rgba(255, 159, 64, 1)'
    //                ],
    //                borderWidth: 0
    //            }]
    //        },
    //        options: {
    //            layout: {
    //                padding: {
    //                    left: 10,
    //                    right: 10,
    //                    top: 20,
    //                    bottom: 20
    //                }
    //            },
    //            maintainAspectRatio: false,
    //            plugins: {
    //                tooltip: {
    //                    enabled: true,
    //                    callbacks: {
    //                        label: function (context) {
    //                            let label = context.label + '(' + context.formattedValue + '%)'
    //                            return label;
    //                        }
    //                    }
    //                },
    //                legend: {
    //                    position: 'right',
    //                    labels: {
    //                        font: {
    //                            family: 'Arial',
    //                            size: 11
    //                        },

    //                        boxWidth: 10,
    //                        boxHeight: 10,
    //                        generateLabels(chart) {
    //                            const data = chart.data;
    //                            if (data.labels.length && data.datasets.length) {
    //                                const { labels: { pointStyle } } = chart.legend.options;

    //                                return data.labels.map((label, i) => {
    //                                    const meta = chart.getDatasetMeta(0);
    //                                    const style = meta.controller.getStyle(i);

    //                                    return {
    //                                        text: `${label} (${data['datasets'][0].data[i]}%)`,
    //                                        fillStyle: style.backgroundColor,
    //                                        strokeStyle: style.borderColor,
    //                                        lineWidth: style.borderWidth,
    //                                        borderWidth: 0,
    //                                        pointStyle: pointStyle,
    //                                        hidden: !chart.getDataVisibility(i),

    //                                        // Extra data used for toggling the correct item
    //                                        index: i
    //                                    };
    //                                });
    //                            }
    //                            return [];
    //                        }
    //                    }
    //                },
    //                labels: {
    //                    /* render:"value",*/
    //                    render: (args) => {

    //                        return args.value + '%';

    //                    },
    //                    position: 'outside',
    //                    outsidePadding: 10,
    //                    textMargin: 10

    //                },

    //            }

    //        },


    //    });
    //}

    function getColors(length) {
        let pallet = ["#4682b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2",
            "#7f7f7f", "#bcbd22", "#17becf",
            "#85144b", "#F012BE", "#3D9970", "#111111", "#AAAAAA"];
        let colors = [];

        for (let i = 0; i < length; i++) {
            colors.push(pallet[i % (pallet.length - 1)]);
        }

        return colors;
    }
    //// Configuration options
    //const options = {
    //    responsive: false,
    //    plugins: {
    //        legend: {
    //            position: 'right',
    //            onClick: function (event, legendItem) {
    //                const index = legendItem.index;
    //                const meta = this.chart.getDatasetMeta(0);
    //                meta.data[index].hidden = !meta.data[index].hidden;
    //                this.chart.update();
    //            }
    //        },
    //        datalabels: {
    //            backgroundColor: function (context) {
    //                return context.dataset.backgroundColor;
    //            },
    //            borderColor: 'white',
    //            borderRadius: 25,
    //            borderWidth: 2,
    //            color: 'white',
    //            display: function (context) {
    //                var dataset = context.dataset;
    //                var count = dataset.data.length;
    //                var value = dataset.data[context.dataIndex];
    //                return value > count * 1.5;
    //            }
    //        }
    //    }

    //};

    //// Create the pie chart
    //var canvas = document.getElementById("myPieChart");
    //if (canvas !== null) {
    //    const ctx = document.getElementById('myPieChart').getContext('2d');
    //    const myPieChart = new Chart(ctx, {
    //        type: 'pie',
    //        data: data,
    //        options: options
    //    });

    //}

}


function drawPieChartUsingChartJsChartRCButton(dataValue) {

    var labels = dataValue.map(function (e) {
        return e.dateRange;
    });
    var data2 = dataValue.map(function (e) {
        return e.recordCountNew;
    });
    // Data for the pie chart
    const data = {
        labels: labels,
        datasets: [{
            data: data2, // Values for each slice

        },
        ],
        datalabels: {
            // display labels for this specific dataset
            display: true
        }
    };


    var canvas = document.getElementById("bar_chart_by_rc_pushedby_cro");
    var canvas2 = document.getElementById("bar_chart_by_rc_pushedby_cro1");
    //var canvas3 = document.getElementById("pie_chart_ir_by_areaward3");
    if (canvas !== null) {
        const ctx = document.getElementById('bar_chart_by_rc_pushedby_cro').getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        window.myChart13 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 2,
                        bottom: 2
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {

                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value +'%';

                        },

                        outsidePadding: 4,
                        textMargin: 4

                    },

                }

            },


        });


    }



    if (canvas2 !== null) {
        const ctx2 = document.getElementById('bar_chart_by_rc_pushedby_cro1').getContext('2d');
        ctx2.clearRect(0, 0, canvas2.width, canvas2.height);
        window.myChart14 = new Chart(ctx2, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0, radius: '80%'
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 20,
                        bottom: 20
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },
                        position: 'outside',
                        outsidePadding: 10,
                        textMargin: 10

                    },

                }

            },


        });
    }


    //if (canvas3 !== null) {
    //    const ctx3 = document.getElementById('pie_chart_ir_by_areaward3').getContext('2d');
    //    window.myChart6 = new Chart(ctx3, {
    //        type: 'pie',
    //        data: {
    //            labels: labels,
    //            datasets: [{
    //                label: '# of Votes',
    //                data: data2,
    //                backgroundColor: getColors(15),
    //                borderColor: [
    //                    'rgba(255, 99, 132, 1)',
    //                    'rgba(54, 162, 235, 1)',
    //                    'rgba(255, 206, 86, 1)',
    //                    'rgba(75, 192, 192, 1)',
    //                    'rgba(153, 102, 255, 1)',
    //                    'rgba(255, 159, 64, 1)'
    //                ],
    //                borderWidth: 0
    //            }]
    //        },
    //        options: {
    //            layout: {
    //                padding: {
    //                    left: 10,
    //                    right: 10,
    //                    top: 20,
    //                    bottom: 20
    //                }
    //            },
    //            maintainAspectRatio: false,
    //            plugins: {
    //                tooltip: {
    //                    enabled: true,
    //                    callbacks: {
    //                        label: function (context) {
    //                            let label = context.label + '(' + context.formattedValue + '%)'
    //                            return label;
    //                        }
    //                    }
    //                },
    //                legend: {
    //                    position: 'right',
    //                    labels: {
    //                        font: {
    //                            family: 'Arial',
    //                            size: 11
    //                        },

    //                        boxWidth: 10,
    //                        boxHeight: 10,
    //                        generateLabels(chart) {
    //                            const data = chart.data;
    //                            if (data.labels.length && data.datasets.length) {
    //                                const { labels: { pointStyle } } = chart.legend.options;

    //                                return data.labels.map((label, i) => {
    //                                    const meta = chart.getDatasetMeta(0);
    //                                    const style = meta.controller.getStyle(i);

    //                                    return {
    //                                        text: `${label} (${data['datasets'][0].data[i]}%)`,
    //                                        fillStyle: style.backgroundColor,
    //                                        strokeStyle: style.borderColor,
    //                                        lineWidth: style.borderWidth,
    //                                        borderWidth: 0,
    //                                        pointStyle: pointStyle,
    //                                        hidden: !chart.getDataVisibility(i),

    //                                        // Extra data used for toggling the correct item
    //                                        index: i
    //                                    };
    //                                });
    //                            }
    //                            return [];
    //                        }
    //                    }
    //                },
    //                labels: {
    //                    /* render:"value",*/
    //                    render: (args) => {

    //                        return args.value + '%';

    //                    },
    //                    position: 'outside',
    //                    outsidePadding: 10,
    //                    textMargin: 10

    //                },

    //            }

    //        },


    //    });
    //}

    function getColors(length) {
        let pallet = ["#4682b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2",
            "#7f7f7f", "#bcbd22", "#17becf",
            "#85144b", "#F012BE", "#3D9970", "#111111", "#AAAAAA"];
        let colors = [];

        for (let i = 0; i < length; i++) {
            colors.push(pallet[i % (pallet.length - 1)]);
        }

        return colors;
    }
    //// Configuration options
    //const options = {
    //    responsive: false,
    //    plugins: {
    //        legend: {
    //            position: 'right',
    //            onClick: function (event, legendItem) {
    //                const index = legendItem.index;
    //                const meta = this.chart.getDatasetMeta(0);
    //                meta.data[index].hidden = !meta.data[index].hidden;
    //                this.chart.update();
    //            }
    //        },
    //        datalabels: {
    //            backgroundColor: function (context) {
    //                return context.dataset.backgroundColor;
    //            },
    //            borderColor: 'white',
    //            borderRadius: 25,
    //            borderWidth: 2,
    //            color: 'white',
    //            display: function (context) {
    //                var dataset = context.dataset;
    //                var count = dataset.data.length;
    //                var value = dataset.data[context.dataIndex];
    //                return value > count * 1.5;
    //            }
    //        }
    //    }

    //};

    //// Create the pie chart
    //var canvas = document.getElementById("myPieChart");
    //if (canvas !== null) {
    //    const ctx = document.getElementById('myPieChart').getContext('2d');
    //    const myPieChart = new Chart(ctx, {
    //        type: 'pie',
    //        data: data,
    //        options: options
    //    });

    //}

}


function drawPieChartUsingChartJsChartRCForNumberofGuardstoPrealarm(dataValue) {

    var labels = dataValue.map(function (e) {
        return e.dateRange;
    });
    var data2 = dataValue.map(function (e) {
        return e.recordCountNew;
    });
    // Data for the pie chart
    const data = {
        labels: labels,
        datasets: [{
            data: data2, // Values for each slice

        },
        ],
        datalabels: {
            // display labels for this specific dataset
            display: true
        }
    };


    var canvas = document.getElementById("bar_chart_by_rc_guards_wenttoprealarm");
    var canvas2 = document.getElementById("bar_chart_by_rc_guards_wenttoprealarm1");
    //var canvas3 = document.getElementById("pie_chart_ir_by_areaward3");
    if (canvas !== null) {
        const ctx = document.getElementById('bar_chart_by_rc_guards_wenttoprealarm').getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        window.myChart15 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 2,
                        bottom: 2
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {

                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value +'%';

                        },

                        outsidePadding: 4,
                        textMargin: 4

                    },

                }

            },


        });


    }



    if (canvas2 !== null) {
        const ctx2 = document.getElementById('bar_chart_by_rc_guards_wenttoprealarm1').getContext('2d');
        ctx2.clearRect(0, 0, canvas2.width, canvas2.height);
        window.myChart16 = new Chart(ctx2, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0,radius: '80%',
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 20,
                        bottom: 20
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },
                        position: 'outside',
                        outsidePadding: 10,
                        textMargin: 10

                    },

                }

            },


        });
    }


    //if (canvas3 !== null) {
    //    const ctx3 = document.getElementById('pie_chart_ir_by_areaward3').getContext('2d');
    //    window.myChart6 = new Chart(ctx3, {
    //        type: 'pie',
    //        data: {
    //            labels: labels,
    //            datasets: [{
    //                label: '# of Votes',
    //                data: data2,
    //                backgroundColor: getColors(15),
    //                borderColor: [
    //                    'rgba(255, 99, 132, 1)',
    //                    'rgba(54, 162, 235, 1)',
    //                    'rgba(255, 206, 86, 1)',
    //                    'rgba(75, 192, 192, 1)',
    //                    'rgba(153, 102, 255, 1)',
    //                    'rgba(255, 159, 64, 1)'
    //                ],
    //                borderWidth: 0
    //            }]
    //        },
    //        options: {
    //            layout: {
    //                padding: {
    //                    left: 10,
    //                    right: 10,
    //                    top: 20,
    //                    bottom: 20
    //                }
    //            },
    //            maintainAspectRatio: false,
    //            plugins: {
    //                tooltip: {
    //                    enabled: true,
    //                    callbacks: {
    //                        label: function (context) {
    //                            let label = context.label + '(' + context.formattedValue + '%)'
    //                            return label;
    //                        }
    //                    }
    //                },
    //                legend: {
    //                    position: 'right',
    //                    labels: {
    //                        font: {
    //                            family: 'Arial',
    //                            size: 11
    //                        },

    //                        boxWidth: 10,
    //                        boxHeight: 10,
    //                        generateLabels(chart) {
    //                            const data = chart.data;
    //                            if (data.labels.length && data.datasets.length) {
    //                                const { labels: { pointStyle } } = chart.legend.options;

    //                                return data.labels.map((label, i) => {
    //                                    const meta = chart.getDatasetMeta(0);
    //                                    const style = meta.controller.getStyle(i);

    //                                    return {
    //                                        text: `${label} (${data['datasets'][0].data[i]}%)`,
    //                                        fillStyle: style.backgroundColor,
    //                                        strokeStyle: style.borderColor,
    //                                        lineWidth: style.borderWidth,
    //                                        borderWidth: 0,
    //                                        pointStyle: pointStyle,
    //                                        hidden: !chart.getDataVisibility(i),

    //                                        // Extra data used for toggling the correct item
    //                                        index: i
    //                                    };
    //                                });
    //                            }
    //                            return [];
    //                        }
    //                    }
    //                },
    //                labels: {
    //                    /* render:"value",*/
    //                    render: (args) => {

    //                        return args.value + '%';

    //                    },
    //                    position: 'outside',
    //                    outsidePadding: 10,
    //                    textMargin: 10

    //                },

    //            }

    //        },


    //    });
    //}

    function getColors(length) {
        let pallet = ["#4682b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2",
            "#7f7f7f", "#bcbd22", "#17becf",
            "#85144b", "#F012BE", "#3D9970", "#111111", "#AAAAAA"];
        let colors = [];

        for (let i = 0; i < length; i++) {
            colors.push(pallet[i % (pallet.length - 1)]);
        }

        return colors;
    }
    //// Configuration options
    //const options = {
    //    responsive: false,
    //    plugins: {
    //        legend: {
    //            position: 'right',
    //            onClick: function (event, legendItem) {
    //                const index = legendItem.index;
    //                const meta = this.chart.getDatasetMeta(0);
    //                meta.data[index].hidden = !meta.data[index].hidden;
    //                this.chart.update();
    //            }
    //        },
    //        datalabels: {
    //            backgroundColor: function (context) {
    //                return context.dataset.backgroundColor;
    //            },
    //            borderColor: 'white',
    //            borderRadius: 25,
    //            borderWidth: 2,
    //            color: 'white',
    //            display: function (context) {
    //                var dataset = context.dataset;
    //                var count = dataset.data.length;
    //                var value = dataset.data[context.dataIndex];
    //                return value > count * 1.5;
    //            }
    //        }
    //    }

    //};

    //// Create the pie chart
    //var canvas = document.getElementById("myPieChart");
    //if (canvas !== null) {
    //    const ctx = document.getElementById('myPieChart').getContext('2d');
    //    const myPieChart = new Chart(ctx, {
    //        type: 'pie',
    //        data: data,
    //        options: options
    //    });

    //}

}
function drawPieChartUsingChartJsChartRCForNumberofGuardsFromPrealarm(dataValue) {

    var labels = dataValue.map(function (e) {
        return e.dateRange;
    });
    var data2 = dataValue.map(function (e) {
        return e.recordCountNew;
    });
    // Data for the pie chart
    const data = {
        labels: labels,
        datasets: [{
            data: data2, // Values for each slice

        },
        ],
        datalabels: {
            // display labels for this specific dataset
            display: true
        }
    };


    var canvas = document.getElementById("bar_chart_by_rc_guards_wentfromprealarmorangetored");
    var canvas2 = document.getElementById("bar_chart_by_rc_guards_wentfromprealarmorangetored1");
    //var canvas3 = document.getElementById("pie_chart_ir_by_areaward3");
    if (canvas !== null) {
        const ctx = document.getElementById('bar_chart_by_rc_guards_wentfromprealarmorangetored').getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        window.myChart17 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0, radius: '80%',
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 2,
                        bottom: 2
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {

                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value +'%';

                        },

                        outsidePadding: 4,
                        textMargin: 4

                    },

                }

            },


        });


    }



    if (canvas2 !== null) {
        const ctx2 = document.getElementById('bar_chart_by_rc_guards_wentfromprealarmorangetored1').getContext('2d');
        ctx2.clearRect(0, 0, canvas2.width, canvas2.height);
        window.myChart18 = new Chart(ctx2, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0, radius: '80%'
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 20,
                        bottom: 20
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]})`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },
                        position: 'outside',
                        outsidePadding: 10,
                        textMargin: 10

                    },

                }

            },


        });
    }


    //if (canvas3 !== null) {
    //    const ctx3 = document.getElementById('pie_chart_ir_by_areaward3').getContext('2d');
    //    window.myChart6 = new Chart(ctx3, {
    //        type: 'pie',
    //        data: {
    //            labels: labels,
    //            datasets: [{
    //                label: '# of Votes',
    //                data: data2,
    //                backgroundColor: getColors(15),
    //                borderColor: [
    //                    'rgba(255, 99, 132, 1)',
    //                    'rgba(54, 162, 235, 1)',
    //                    'rgba(255, 206, 86, 1)',
    //                    'rgba(75, 192, 192, 1)',
    //                    'rgba(153, 102, 255, 1)',
    //                    'rgba(255, 159, 64, 1)'
    //                ],
    //                borderWidth: 0
    //            }]
    //        },
    //        options: {
    //            layout: {
    //                padding: {
    //                    left: 10,
    //                    right: 10,
    //                    top: 20,
    //                    bottom: 20
    //                }
    //            },
    //            maintainAspectRatio: false,
    //            plugins: {
    //                tooltip: {
    //                    enabled: true,
    //                    callbacks: {
    //                        label: function (context) {
    //                            let label = context.label + '(' + context.formattedValue + '%)'
    //                            return label;
    //                        }
    //                    }
    //                },
    //                legend: {
    //                    position: 'right',
    //                    labels: {
    //                        font: {
    //                            family: 'Arial',
    //                            size: 11
    //                        },

    //                        boxWidth: 10,
    //                        boxHeight: 10,
    //                        generateLabels(chart) {
    //                            const data = chart.data;
    //                            if (data.labels.length && data.datasets.length) {
    //                                const { labels: { pointStyle } } = chart.legend.options;

    //                                return data.labels.map((label, i) => {
    //                                    const meta = chart.getDatasetMeta(0);
    //                                    const style = meta.controller.getStyle(i);

    //                                    return {
    //                                        text: `${label} (${data['datasets'][0].data[i]}%)`,
    //                                        fillStyle: style.backgroundColor,
    //                                        strokeStyle: style.borderColor,
    //                                        lineWidth: style.borderWidth,
    //                                        borderWidth: 0,
    //                                        pointStyle: pointStyle,
    //                                        hidden: !chart.getDataVisibility(i),

    //                                        // Extra data used for toggling the correct item
    //                                        index: i
    //                                    };
    //                                });
    //                            }
    //                            return [];
    //                        }
    //                    }
    //                },
    //                labels: {
    //                    /* render:"value",*/
    //                    render: (args) => {

    //                        return args.value + '%';

    //                    },
    //                    position: 'outside',
    //                    outsidePadding: 10,
    //                    textMargin: 10

    //                },

    //            }

    //        },


    //    });
    //}

    function getColors(length) {
        let pallet = ["#4682b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2",
            "#7f7f7f", "#bcbd22", "#17becf",
            "#85144b", "#F012BE", "#3D9970", "#111111", "#AAAAAA"];
        let colors = [];

        for (let i = 0; i < length; i++) {
            colors.push(pallet[i % (pallet.length - 1)]);
        }

        return colors;
    }
    //// Configuration options
    //const options = {
    //    responsive: false,
    //    plugins: {
    //        legend: {
    //            position: 'right',
    //            onClick: function (event, legendItem) {
    //                const index = legendItem.index;
    //                const meta = this.chart.getDatasetMeta(0);
    //                meta.data[index].hidden = !meta.data[index].hidden;
    //                this.chart.update();
    //            }
    //        },
    //        datalabels: {
    //            backgroundColor: function (context) {
    //                return context.dataset.backgroundColor;
    //            },
    //            borderColor: 'white',
    //            borderRadius: 25,
    //            borderWidth: 2,
    //            color: 'white',
    //            display: function (context) {
    //                var dataset = context.dataset;
    //                var count = dataset.data.length;
    //                var value = dataset.data[context.dataIndex];
    //                return value > count * 1.5;
    //            }
    //        }
    //    }

    //};

    //// Create the pie chart
    //var canvas = document.getElementById("myPieChart");
    //if (canvas !== null) {
    //    const ctx = document.getElementById('myPieChart').getContext('2d');
    //    const myPieChart = new Chart(ctx, {
    //        type: 'pie',
    //        data: data,
    //        options: options
    //    });

    //}

}
let logBookTypeForAuditZipfusion;
$('#btnDownloadfusionAuditZip').on('click', function () {

    logBookTypeForAuditZipfusion = 1;
    if ($('#fusionClientSiteId').val() === '') {
        alert('Please select a client site');
        return;
    }
    $('#auditfusionlog-zip-modal').modal('show');
});


$('#auditfusionlog-zip-modal').on('show.bs.modal', function (event) {
    $('#btn-auditfusionlog-zip-download').attr('href', '#');
    $('#btn-auditfusionlog-zip-download').hide();
    $('#auditfusionlog-zip-msg').show();

    if (logBookTypeForAuditZipfusion === 1)
        downloadDailyGuardfusionLogZipFile();

});



function downloadDailyGuardfusionLogZipFile() {
    $.ajax({
        url: '/Fusion?handler=DownloadDailyFusionGuardLogZip',
        type: 'POST',
        dataType: 'json',
        data: {
            clientSiteId: $('#fusionClientSiteId').val().join(';'), 
            logFromDate: $('#fusionAudtitFromDate').val(),
            logToDate: $('#fusionAudtitToDate').val()
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        if (!response.success) {
            $('#auditfusionlog-zip-modal').modal('hide');
            new MessageModal({ message: 'Failed to generate zip file. ' + response.message }).showError();
        } else {
            $('#btn-auditfusionlog-zip-download').attr('href', response.fileName);
            $('#btn-auditfusionlog-zip-download').show();
            $('#auditfusionlog-zip-msg').hide();
        }
    });
}

     //end fusion report in auditlog08072024


//Audio file save 
let mediaRecorder;
let audioChunks = [];
$('#startBtn').click(async function () {
    // Clear previous recording data
    audioChunks = [];
    mediaRecorder = null;
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    mediaRecorder = new MediaRecorder(stream);

    mediaRecorder.ondataavailable = function (event) {
        audioChunks.push(event.data);
    };

    mediaRecorder.onstop = function () {
        const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
        const audioUrl = URL.createObjectURL(audioBlob);
        $('#audioPlayback').attr('src', audioUrl);
        uploadAudio(audioBlob);
    };

    mediaRecorder.start();
    $(this).prop('disabled', true);
    $('#stopBtn').prop('disabled', false);
});

async function clickstarAudio() {

    // Clear previous recording data
    audioChunks = [];
    mediaRecorder = null;
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    mediaRecorder = new MediaRecorder(stream);

    mediaRecorder.ondataavailable = function (event) {
        audioChunks.push(event.data);
    };

    mediaRecorder.onstop = function () {
        const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
        const audioUrl = URL.createObjectURL(audioBlob);
        $('#audioPlayback').attr('src', audioUrl);
        uploadAudio(audioBlob);
    };

    mediaRecorder.start();
    //$(this).prop('disabled', true);
    //$('#stopBtn').prop('disabled', false);
}

function clickStopAudio() {

    mediaRecorder.stop();
    //$(this).prop('disabled', true);
    //$('#startBtn').prop('disabled', false);
}

$('#stopBtn').click(function () {
    mediaRecorder.stop();
    $(this).prop('disabled', true);
    $('#startBtn').prop('disabled', false);
});

function uploadAudio(blob) {





    let formData = new FormData();
    formData.append('audioFile', blob, 'recording.wav');

    $.ajax({
        url: '/Record?handler=SaveAudio',
        type: 'POST',
        dataType: 'json',
        data: formData,
        processData: false,
        contentType: false, // Add this line
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        console.log('Upload success:', result);
        formData.delete('audioFile'); // Removes the file from FormData
        blob = null; // Clear the blob reference to allow garbage collection
    });



}


$('.btn-delete-dgl-attachment1').on('click', function (event) {
    // $('#dgl-attachment-list1').on('click', '.btn-delete-dgl-attachment1', function (event) {
    var pageNameValue = $('#pageName').val();
    var tmdata = {
        'EventDateTimeLocal': null,
        'EventDateTimeLocalWithOffset': null,
        'EventDateTimeZone': null,
        'EventDateTimeZoneShort': null,
        'EventDateTimeUtcOffsetMinute': null,
    };

    /*fillRefreshLocalTimeZoneDetails(tmdata, "", false)*/

    var DateTime = luxon.DateTime;
    var dt1 = DateTime.local();
    let tz = dt1.zoneName + ' ' + dt1.offsetNameShort;
    let diffTZ = dt1.offset
    let tzshrtnm = 'GMT' + dt1.toFormat('ZZ');
    const eventDateTimeLocal = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS');
    const eventDateTimeLocalWithOffset = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS Z');
    tmdata.EventDateTimeLocal = eventDateTimeLocal;
    tmdata.EventDateTimeLocalWithOffset = eventDateTimeLocalWithOffset;
    tmdata.EventDateTimeZone = tz;
    tmdata.EventDateTimeZoneShort = tzshrtnm;
    tmdata.EventDateTimeUtcOffsetMinute = diffTZ;

    var target = event.target;
    var filename = target.id;
    var guardId = $("#GuardLog_GuardLogin_GuardId").val();
    var loginUserId = $("#loginUserId").val();
    $.ajax({
        url: 'RadioCheckV2?handler=DownLoadHelpPDF',
        type: 'POST',
        dataType: 'json',
        data: {
            filename: filename,
            loginGuardId: guardId,
            tmdata: tmdata,
            pageName: pageNameValue,
            loginUserId: loginUserId,
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result) {
            // Step 3: Dynamically create an anchor tag and trigger the download
            var link = document.createElement('a');
            link.href = 'https://www.cws-ir.com/StaffDocs/' + filename;  // Construct the file URL
            link.target = "_blank";                // Open the file in a new window or tab
            document.body.appendChild(link);       // Append to the body
            link.click();                          // Simulate a click event to open the file
            document.body.removeChild(link);    // Remove the link from the document
        }
    });

});

$('#StateDrp').on('change', function () {
    const selectedState = $(this).val(); // Get the selected state
    if (selectedState) {
        updateMap(selectedState);
    }
});
function updateMap(state) {
    fetch(`/RadioCheckV2?handler=ClientSiteActivityStatusState&state=${state}`, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
        .then(response => response.json())
        .then(data => {
            clearMarkers();
            data.forEach(record => {
                const gps = record.gps ? record.gps.trim() : '';
                const address = record.address ? stripHtml(record.address).trim() : '';
                const GuardName = record.guardName;
                const siteNameParts = record.siteName.split('&nbsp;');
                const siteName = siteNameParts[0].trim();
                const phoneNumber = siteNameParts.slice(1).join('').trim();
                const alertColor = 'Green';
                const markerColor = getColorFromAlert(alertColor);

                if (gps) {
                    const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                    L.marker([lat, lng], { icon: createCustomIcon(markerColor) })
                        .bindPopup(`<strong>SiteName:</strong> ${siteName} <br>
                                    <strong>Phone Number:</strong> ${phoneNumber} <br>
                                    <strong>Address:</strong> ${address} <br>
                                    <strong>GuardName:</strong> ${GuardName}`)
                        .addTo(map);
                }
            });
        })
        .catch(error => console.error('Error:', error));
}

function clearMarkers() {
    map.eachLayer(function (layer) {
        if (layer instanceof L.Marker) {
            map.removeLayer(layer);
        }
    });
}
function stripHtml(input) {
    const doc = new DOMParser().parseFromString(input, 'text/html');
    return doc.body.textContent || "";
}

function getColorFromAlert(alert) {
    switch (alert) {
        case 'Red': return 'red';
        case 'Green': return 'green';
        case 'Yellow': return 'yellow';
        default: return 'grey';
    }
}

function createCustomIcon(color) {
    return L.divIcon({
        className: 'custom-marker',
        html: `<div style="background-color:${color}; width: 25px; height: 25px; border-radius: 50%; animation: blink 1s infinite;"></div>`,
        iconSize: [25, 25],
    });
}
//p4-117-start

const columns = [];
//let gridGuardRCLogin = $('#loggedGuardsDetails').DataTable({
//    paging: false,
//    ordering: false,
//    info: false,
//    searching: false,
//    fixedColumns: {
//        left: 1
//    },
//    scrollCollapse: true,
//    scrollX: true,
//    autoWidth: false,
//   data: null,
//    columns: columns
//});
function getLoggedGuardDetails() {
    $('#loggedGuardsInfoModal').modal('show');
    const subHeaders = ['Time', 'IP'];
    const now = new Date();
    const start = new Date(now.getTime() - (72 * 60 * 60 * 1000)); // 72 hours ago
    const dates = [];
    isPaused = true;
    // Generate dates
    for (let d = new Date(start); d <= now; d.setDate(d.getDate() + 1)) {
        dates.push(d.toLocaleDateString('en-GB', {
            weekday: 'short', day: '2-digit', month: 'short', year: 'numeric'
        }));
    }

    // Row 1: Main headers (dates)
    let topRow = '<tr><th rowspan="2">CRO</th><th rowspan="2">Licence</th>';
    dates.forEach(date => {
        topRow += `<th colspan="${subHeaders.length}">${date}</th>`;
    });
    topRow += '</tr>';

    // Row 2: Sub-headers (Time, IP)
    let secondRow = '<tr>';
    dates.forEach(() => {
        subHeaders.forEach(sub => {
            secondRow += `<th>${sub}</th>`;
        });
    });
    secondRow += '</tr>';
    var headerrow = '<thead>' + topRow + secondRow + '</thead>';
    GetGuardRCLoginDetails(headerrow, dates);
   /* $('#loggedGuardsDetails').html('<thead>' + topRow + secondRow + '</thead>' + databody);*/
    
}
function GetGuardRCLoginDetails(headerrow, dates) {
    $.ajax({
        url: '/RadioCheckV2?handler=GuardRCLoginDetails',
        type: 'GET',
        dataType: 'json',
        //data: function (d) { d.id = $('#txt_SpecificGuardDetailsId').val(); }
    })
        .done(function (response) {
            let datarow = '<tbody>';
            response.forEach(item => {
                datarow += `<tr>`;
                datarow += `<td> ${item.guardName}</td> `;
                datarow += `<td> ${item.license}</td> `;
                dates.forEach(date => {
                    const log = item.logins.find(log => log.formattedLastLoginDate === date);
                    if (log) {
                        datarow += `<td>${log.formattedLastLoginTime}</td>`;
                        datarow += `<td>${log.ipAddress}</td>`;
                    } else {
                        datarow += `<td></td><td></td>`;
                    }
                });
                //item.logins.forEach(itemnew => {
                //    if (dates[0] == itemnew.formattedLastLoginDate) {
                //        datarow += `<td> ${itemnew.formattedLastLoginTime}</td> `;
                //        datarow += `<td> ${itemnew.ipAddress}</td> `;

                //    }
                //    else {
                //        datarow += `<td> </td> `;
                //        datarow += `<td></td> `;
                //    }
                //    if (dates[1] == itemnew.formattedLastLoginDate) {
                        
                //        datarow += `<td> ${itemnew.formattedLastLoginTime}</td> `;
                //        datarow += `<td> ${itemnew.ipAddress}</td> `;
                        
                        
                //    }
                //    else {
                //        datarow += `<td> </td> `;
                //        datarow += `<td></td> `;
                //    }
                //    if (dates[2] == itemnew.formattedLastLoginDate) {

                //        datarow += `<td> ${itemnew.formattedLastLoginTime}</td> `;
                //        datarow += `<td> ${itemnew.ipAddress}</td> `;

                //    }
                //    else {
                //        datarow += `<td> </td> `;
                //        datarow += `<td></td> `;
                //    }
                //    if (dates[3] == itemnew.formattedLastLoginDate) {
                       
                //        datarow += `<td> ${itemnew.formattedLastLoginTime}</td> `;
                //        datarow += `<td> ${itemnew.ipAddress}</td> `;

                //    }
                //    else {
                //        datarow += `<td> </td> `;
                //        datarow += `<td></td> `;
                //    }
                //    //dates.forEach(date => {
                //    //    if (date == itemnew.formattedLastLoginDate) {
                //    //        datarow += `<td> ${itemnew.formattedLastLoginTime}</td> `;
                //    //        datarow += `<td> ${itemnew.ipAddress}</td> `;
                //    //    }
                //    //    else {
                //    //        datarow += `<td> </td> `;
                //    //        datarow += `<td></td> `;
                //    //    }
                //    //   // topRow += `<th colspan="${subHeaders.length}">${date}</th>`;
                //    //});
                //    //datarow += `<td> ${itemnew.formattedLastLoginTime}</td> `;
                //    //datarow += `<td> ${itemnew.ipAddress}</td> `;
                   
                //});
                //if (item.logins.length == 0) {
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //}
                //else if (item.logins.length == 1) {
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;

                //}
                //else if (item.logins.length == 2) {
                    
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;

                //}
                //else if (item.logins.length == 3) {

                    
                //    datarow += `<td> </td> `;
                //    datarow += `<td> </td> `;

                //}
                //else if (item.logins.length == 4) {


                //    datarow += ` `;

                //}


                datarow += `</tr>`;
                
            });
            datarow += `</tbody>`;
            $('#loggedGuardsDetails').html(headerrow + datarow);
            //gridGuardRCLogin.ajax.reload(null, false)
            //gridGuardRCLogin.refresh();
            //gridGuardRCLogin.ajax.reload();
        });
}
//p4-117-end
$('#btnSendActionListLater').on('click', function () {
    $('#MessageType').val('ActionList');
    $('#MessageSendTimeInfoModal').modal('show');
});
function fillRefreshLocalTimeZoneDetailswithMessageTime(formData, modelname, isform) {
    // for reference https://moment.github.io/luxon/#/

    const { DateTime } = luxon;

    let inputVal = $('#txtSendMessageDate').val(); // e.g. "2025-05-16T14:30"
    var DateTimenew = DateTime.fromISO(inputVal);
    //var DateTime = luxon.DateTime;
    var dt1 = DateTimenew.toLocal();
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
$('#btnSaveMessageTime').on('click', function () {
    if ($('#MessageType').val() == 'ActionList') {


        $(this).prop('disabled', true);
        var messagetime = $('#txtSendMessageDate').val()
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
            $(this).prop('disabled', false);
        }

        else if (chkClientType == true && ClientType == null) {
            displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
            $(this).prop('disabled', false);
        }
        else if (ClientType == '') {
            displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
            $(this).prop('disabled', false);
        }
        else if (ClientSite == '') {
            displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client site ');
            $(this).prop('disabled', false);
        }
        else {

            // Task p6#73_TimeZone issue -- added by Binoy - Start   
            fillRefreshLocalTimeZoneDetailswithMessageTime(tmzdata, "", false);
            // Task p6#73_TimeZone issue -- added by Binoy - End
            var objforMessage = {
                Id: 0,
                Notifications: Notifications,
                Subject: Subject,
                AlarmKeypadCode: AlarmKeypadCode,
                Action1: Action1,
                Physicalkey: Physicalkey,
                Action2: Action2,
                SiteCombinationLook: SiteCombinationLook,
                Action3: Action3,
                Action4: Action4,
                Action5: Action5,
                CommentsForControlRoomOperator: CommentsForControlRoomOperator,

                messagetime: messagetime,
                IsDeleted: false
            }
            var objGuardLogs = {
                Id: 0,
                GuardId: 0,
                RCActionListMessagesId: 0,
                EventDateTimeLocal: tmzdata.EventDateTimeLocal,
                EventDateTime: tmzdata.EventDateTime,
                EventDateTimeLocalWithOffset: tmzdata.EventDateTimeLocalWithOffset,
                EventDateTimeZone: tmzdata.EventDateTimeZone,
                EventDateTimeZoneShort: tmzdata.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute: tmzdata.EventDateTimeUtcOffsetMinute
            }

            $.ajax({
                url: '/RadioCheckV2?handler=SaveActionListLater',
                type: 'POST',
                data: {
                    objforMessage: objforMessage,
                    ClientType: ClientType,
                    clientSiteId: clientSiteId,

                    objGuardLogs: objGuardLogs
                },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.success == true) {
                    $('#MessageSendTimeInfoModal').modal('hide');
                    $('#pushNoTificationsControlRoomModal').modal('hide');
                    alert(data.message)
                    $(this).prop('disabled', false);

                }
                else {
                    displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
                    $(this).prop('disabled', false);
                }
                //$('#selectRadioStatus').val('');
                //$('#btnRefreshActivityStatus').trigger('click');
            });
        }
    }
    else {
        $(this).prop('disabled', true);
        var messagetime = $('#txtSendMessageDate').val()
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
        const chkGlobalPersonalEmail = $('#chkGlobalPersonalEmail').is(':checked');
        if (Notifications === '') {
            displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter a Message to send ');
            $(this).prop('disabled', false);
        }
        else if (checkedState == false && chkClientType == false && chkClientType == false && checkedSMSPersonal == false && checkedSMSSmartWand == false && chkNationality == false && chkGlobalPersonalEmail == false) {
            displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select any one of the transfer options ');
            $(this).prop('disabled', false);
        }
        else if (chkClientType == true && ClientType == null) {
            displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
            $(this).prop('disabled', false);
        }
        else {

            $('#Access_permission_RC_status').hide();
            $('#Access_permission_RC_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Sending Email. Please wait...').show();
            // Task p6#73_TimeZone issue -- added by Binoy - Start   
            fillRefreshLocalTimeZoneDetailswithMessageTime(tmzdata, "", false);
            // Task p6#73_TimeZone issue -- added by Binoy - End
            var objGuardLogs = {
                Id: 0,
                GuardId: 0,
                RCActionListMessagesId: 0,
                EventDateTimeLocal: tmzdata.EventDateTimeLocal,
                EventDateTime: tmzdata.EventDateTime,
                EventDateTimeLocalWithOffset: tmzdata.EventDateTimeLocalWithOffset,
                EventDateTimeZone: tmzdata.EventDateTimeZone,
                EventDateTimeZoneShort: tmzdata.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute: tmzdata.EventDateTimeUtcOffsetMinute
            }
            $.ajax({
                url: '/RadioCheckV2?handler=SaveGlobalNotificationTestMessagesLater',
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
                    chkGlobalPersonalEmail: chkGlobalPersonalEmail,
                    clientSiteId: clientSiteId,
                    objGuardLogs: objGuardLogs,
                    messagetime: messagetime
                },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.success == true) {
                    $('#MessageSendTimeInfoModal').modal('hide');
                    $('#pushNoTificationsControlRoomModal').modal('hide');
                    $(this).prop('disabled', false);
                    $('#Access_permission_RC_status').hide();
                }
                else {
                    displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
                    $(this).prop('disabled', false);
                }
                //$('#selectRadioStatus').val('');
                //$('#btnRefreshActivityStatus').trigger('click');
            });
        }
    }
});
$('#btnSendGlabalNotificationMessageLater').on('click', function () {
    $('#MessageType').val('GlobalList');
    $('#MessageSendTimeInfoModal').modal('show');
});
$('#MessageSendTimeInfoModal').on('shown.bs.modal', function (event) {
    $('#btnSaveMessageTime').prop('disabled', false);
    $('#txtSendMessageDate').val('');
    });