let nIntervId;
const duration = 60 * 4;
var isPaused = false;
var playedMessageIds = []; // Track played messages to prevent looping sound

window.onload = function () {
    updateLanguagesDropdown();

    if (document.querySelector('#clockRefresh')) {
        startClock();

    }
    if ($('#GuardLog_GuardLogin_GuardId').val() != null || $('#GuardLog_GuardLogin_GuardId').val() != '') {
        ExpiredDocuments();

    }
    // Only check messages if we are on a Dashboard/Log page
    if (document.querySelector('#clockRefresh')) {
        CheckUnreadMessages();
        setInterval(CheckUnreadMessages, 30000); // Poll every 30 seconds
    }
};

function CheckUnreadMessages() {
    var guardIdVal = $('#GuardLog_GuardLogin_GuardId').val();
    var currentGuardId = guardIdVal ? parseInt(guardIdVal) : 0;
    var clientSiteId = $('#ClientSiteID').val(); // Get current Site ID

    $.get('/Radio/RadioCheckNew?handler=CheckUnreadMessages&clientSiteId=' + clientSiteId, function (data) {
        if (data && data.length > 0) {
            data.forEach(function (msg) {
                // Filter Self-Notification: If the message matches the current Guard ID, ignore it.
                // console.log("MsgGuardId:", msg.guardLogin ? msg.guardLogin.guardId : "null", "CurrentGuardId:", currentGuardId);
                if (currentGuardId > 0 && msg.guardLogin && msg.guardLogin.guardId === currentGuardId) {
                    console.log("Blocking self-notification from GuardId:", currentGuardId);
                    return; // Skip this message (it's my own reply)
                }

                // Only play sound if we haven't played it for this message ID yet
                if (!playedMessageIds.includes(msg.id)) {
                    playNotificationSound();
                    playedMessageIds.push(msg.id);
                }
                showNotificationSlider(msg.notes, true, msg.id);
            });
        }
    });
}
//Audio Playback
function playNotificationSound() {
    var audio = document.getElementById("audioPlayback");
    if (!audio) {
        audio = document.createElement("audio");
        audio.id = "audioPlayback";
        audio.style.display = "none";
        document.body.appendChild(audio);
    }
    if (audio) {
        audio.src = '/NotificationSound/mixkit-bell-notification-933.wav';
        audio.play().catch(function (error) {
            console.log("Autoplay prevented or audio error:", error);
            // Silent Audio Unlock: Browser blocked autoplay. 
            // Attach a one-time global listener to unlock audio on the next user interaction.
            var unlockFunc = function () {
                if (audio) {
                    audio.play().then(() => {
                        // audio.pause(); // Removed to allow delayed playback of blocked notification
                        // audio.currentTime = 0;
                        console.log("Audio unlocked and delayed notification played.");
                    }).catch((e) => console.log("Silent unlock failed:" + e));
                }
                document.removeEventListener('click', unlockFunc);
                document.removeEventListener('keydown', unlockFunc);
            };
            document.addEventListener('click', unlockFunc);
            document.addEventListener('keydown', unlockFunc);
        });
    }
}

//Visual Slider
function showNotificationSlider(message, isFromGuard, id) {
    var container = $('#notification-slider-container');
    if (container.length === 0) {
        $('body').append('<div id="notification-slider-container" style="position: fixed; bottom: 20px; right: 20px; z-index: 9999; display: flex; flex-direction: column-reverse; gap: 10px;"></div>');
        container = $('#notification-slider-container');
    }

    var slider = $('<div class="notification-slider" style="background-color: #f44336; color: white; padding: 15px; border-radius: 5px; box-shadow: 0 4px 8px rgba(0,0,0,0.2); width: 350px; display: none;">' +
        '<div style="display: flex; justify-content: space-between; align-items: start;">' +
        '<span><i class="fa fa-bell"></i> <strong>New Message from Guard</strong></span>' +
        '<button type="button" class="close text-white" aria-label="Close" style="opacity: 1;">' +
        '<span aria-hidden="true">&times;</span>' +
        '</button>' +
        '</div>' +
        '<div style="margin-top: 10px; word-break: break-word;">' + message + '</div>' +
        '</div>');

    container.append(slider);
    slider.slideDown();

    slider.find('.close').on('click', function () {
        slider.slideUp(function () {
            $(this).remove();
            // Ack message on close
            if (id) {
                $.post('/Radio/RadioCheckNew?handler=AckMessage', { id: id, __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val() });
            }
        });
    });
}

function updateLanguagesDropdown() {
    var GuardID = $('#Guard_Id1').val();


    if (!GuardID) {
        console.error("GuardID is missing or invalid.");
        return;
    }

    $.ajax({
        url: '/Admin/GuardSettings?handler=LanguageDetails',
        type: 'GET',
        data: { guardId: GuardID },
        success: function (response) {
            $(".multiselect-option input[type=checkbox]").prop("checked", false);
            var selectedLanguages = response.data.map(function (item) {
                return item.languageID.toString();
            });


            selectedLanguages.forEach(function (value) {

                $(".multiselect-option input[type=checkbox][value='" + value + "']").prop("checked", true);
            });
            $("#LoteDrp").multiselect();
            $("#LoteDrp").val(selectedLanguages);
            $("#LoteDrp").multiselect("refresh");

        },
        error: function (xhr, status, error) {
            console.error("Error occurred during AJAX request:", status, error);
        }
    });
}



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
                    if (window.gridGuardLog) {
                        console.log("Refreshing Guard Log Grid...");
                        window.gridGuardLog.reload();
                        timer = duration;
                    } else {
                        console.warn("gridGuardLog not found, falling back to page reload.");
                        location.reload();
                    }
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
});



/* V2 Changes start 12102023 */
const groupColumn = 1;
const groupColumn2 = 2;
var scrollPosition2;
var rowIndex2;
let clientSiteActiveGuards = $('#clientSiteActiveGuards').DataTable({
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
    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/Radio/RadioCheckNew?handler=ClientSiteActivityStatus',
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
                    '<i class="fa fa-vcard-o text-info" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i>';
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
        },
        {
            data: 'rcStatus',
            width: '5%',
            className: "text-center",
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

    "scrollY": "300px", // Set the desired height for the scrollable area
    "paging": false,
    "footer": true,
    ajax: {
        url: '/Radio/RadioCheckNew?handler=ClientSiteInActivityStatus',
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
            render: function (value, type, data) {

                if (data.notificationType != 1) {
                    return '&nbsp;&nbsp;&nbsp;<i class="fa fa-envelope"></i> <i class="fa fa-user" aria-hidden="true"></i> ' + data.guardName +
                        '<a href="#" class="ml-2"><i class="fa fa-vcard-o text-info" data-toggle="modal" data-target="#guardInfoModal" data-id="' + data.guardId + '"></i></a>';
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
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return '<i class="fa fa-clock-o text-success rc-client-status"></i> ' + value;
            }
        },
        {
            data: 'lastEvent',
            width: '7%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === null) return 'N/A';
                return '<i class="fa fa-clock-o text-success rc-client-status"></i> ' + value;
            }
        },

        {
            data: 'twoHrAlert',
            width: '4%',
            className: "text-center",
            render: function (value, type, data) {
                if (value === 'Green') return '<i class="fa fa-circle text-success"></i>';
                return '<i class="fa fa-circle text-danger"></i>';
            }
        },

        {
            data: 'rcStatus',
            width: '4%',
            className: "text-center",
        },
        {
            targets: -1,
            data: null,
            width: '5%',
            defaultContent: '',
            render: function (value, type, data) {

                return '<button name="btnRadioCheckStatus" class="btn btn-outline-primary">Radio Check</button>';

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

    $('#lbl_guard_name').html('');
    $('#lbl_guard_security_no').html('');
    $('#lbl_guard_state').html('');
    $('#lbl_guard_provider').html('');

    const button = $(event.relatedTarget);
    const id = button.data('id');

    $.ajax({
        url: '/Radio/RadioCheckNew?handler=GuardData',
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
        url: '/Radio/RadioCheckNew?handler=ClientSiteNotAvailableStatus',
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
        url: '/Radio/RadioCheckNew?handler=ClientSitelogBookActivityStatus',
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
        url: '/Radio/RadioCheckNew?handler=ClientSiteKeyVehicleLogActivityStatus',
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
        url: '/Radio/RadioCheckNew?handler=ClientSiteIncidentReportActivityStatus',
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
    $("#selectRadioStatus").val(rcSatus);
    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#selectRadioCheckStatus').modal('show');
});

$('#clientSiteActiveGuards').on('click', 'button[name="btnRadioCheckStatusActive"]', function () {
    var data = clientSiteActiveGuards.row($(this).parents('tr')).data();
    var rowClientSiteId = data.clientSiteId;
    var rowGuardId = data.guardId;
    var rcSatus = data.rcStatus;
    $("#selectRadioStatusActive").val(rcSatus);
    $('#clientSiteId').val(rowClientSiteId);
    $('#guardId').val(rowGuardId);
    $('#selectRadioCheckStatusActive').modal('show');
});

$('#btnSaveRadioStatus').on('click', function () {
    const checkedStatus = $('#selectRadioStatus').val();
    var clientSiteId = $('#clientSiteId').val();
    var guardId = $('#guardId').val();
    if (checkedStatus === '') {
        return;
    }
    $.ajax({
        url: '/Radio/RadioCheckNew?handler=SaveRadioStatus',
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
        clientSiteActiveGuards.ajax.reload();
        clientSiteInActiveGuards.ajax.reload();
    });
});
$('#radio_duress_btn').on('click', function () {
    $.ajax({
        url: '/Radio/RadioCheckNew?handler=SaveDuress',
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
    const checkedStatus = $('#selectRadioStatusActive').val();
    var clientSiteId = $('#clientSiteId').val();
    var guardId = $('#guardId').val();
    if (checkedStatus === '') {
        return;
    }
    $.ajax({
        url: '/Radio/RadioCheckNew?handler=SaveRadioStatus',
        type: 'POST',
        data: {
            clientSiteId: clientSiteId,
            guardId: guardId,
            checkedStatus: checkedStatus,
            active: true,
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function () {
        $('#selectRadioCheckStatusActive').modal('hide');
        $('#selectRadioStatus').val('');
        clientSiteActiveGuards.ajax.reload();
        clientSiteInActiveGuards.ajax.reload();
    });
});


/*For radio check dropdown  end - end*/

/*for pushing notifications from the control room - start*/
$('#pushNoTificationsControlRoomModal').on('shown.bs.modal', function (event) {



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
    $('#dglClientType2').multiselect("disable");
    $('#dglClientSiteId2').multiselect("disable");
    $('#dglClientType2').val('');
    $('#dglClientSiteId2').val('');
    clearGuardValidationSummary('PushNotificationsValidationSummary');

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
        Notifications = Notifications;
        $.ajax({
            url: '/Radio/RadioCheckNew?handler=SavePushNotificationTestMessages',
            type: 'POST',
            data: {
                clientSiteId: clientSiteId,
                checkedLB: checkedLB,
                checkedSiteEmail: checkedSiteEmail,
                checkedSMSPersonal: checkedSMSPersonal,
                checkedSMSSmartWand: checkedSMSSmartWand,
                Notifications: Notifications,
                Subject: Subject,
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $('#pushNoTificationsControlRoomModal').modal('hide');
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
    var clientSiteId = $('#dglClientSiteId2').val();
    var Notifications = $('#txtGlobalNotificationMessage').val();
    var Subject = $('#txtGlobalNotificationSubject').val();
    var State = $('#State1').val();
    var ClientType = $('#dglClientType2').val();
    const chkClientType = $('#chkClientType').is(':checked');
    const chkNationality = $('#chkNationality').is(':checked');

    if (Notifications === '') {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter a Message to send ');
    }
    else if (checkedState == false && chkClientType == false && chkClientType == false && checkedSMSPersonal == false && checkedSMSSmartWand == false && chkNationality == false) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select any one of the transfer options ');

    }
    else if (chkClientType == true && ClientType == null) {
        displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please select the client type ');
    }
    else {
        $.ajax({
            url: '/Radio/RadioCheckNew?handler=SaveGlobalNotificationTestMessages',
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
                clientSiteId: clientSiteId

            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $('#pushNoTificationsControlRoomModal').modal('hide');
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
//$('#dglClientSiteId2').select2({
//    placeholder: 'Select',
//    theme: 'bootstrap4'
//});




$('#dglClientSiteId2').on('change', function () {
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
$('#dglClientType2').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
$('#dglClientSiteId2').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
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
        $('#dglClientType2').val('');
        $('#dglClientSiteId2').val('');
        $('#dglClientType2').multiselect("disable");
        $('#dglClientSiteId2').multiselect("disable");
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
        $('#dglClientType2').val('');
        $('#dglClientSiteId2').val('');
        $('#dglClientType2').multiselect("disable");
        $('#dglClientSiteId2').multiselect("disable");
    } else {
        $('#State1').prop('disabled', 'disabled');
        $('#State1').val('ACT');
    }
});
$('#chkClientType').change(function () {
    const isChecked = $(this).is(':checked');
    if (isChecked == true) {
        $('#State1').prop('disabled', 'disabled');
        $('#chkNationality').prop('checked', false);
        $('#chkSiteState').prop('checked', false);
        $('#chkSMSPersonalGlobal').prop('checked', false);
        $('#chkSMSSmartWandGlobal').prop('checked', false);
        //$('#dglClientType2 option').removeAttr('disabled');
        $('#dglClientType2').val('');
        $('#dglClientType2').multiselect("enable");
        $('#dglClientSiteId2').multiselect("enable");
        $('#dglClientSiteId2').val('');
        $('#dglClientSiteId2').html('');

    } else {
        $('#dglClientType2').val('').trigger("change");

        $('#dglClientType2').multiselect("refresh");
        $('#dglClientType2').val('');
        $('#dglClientSiteId2').val('');
        $('#dglClientType2').multiselect("disable");
        $('#dglClientSiteId2').multiselect("disable");

        $('#dglClientSiteId2').html('');


    }
    $('#dglClientType2').on('change', function () {
        const clientTypeId = $(this).val().join(';');
        $('#dglClientSiteId2').multiselect("refresh");
        $('#dglClientSiteId2').html('');
        const clientSiteControl = $('#dglClientSiteId2');
        var selectedOption = $(this).find("option:selected");
        var selectedText = selectedOption.text();

        /*$("#vklClientType").multiselect("refresh");*/
        // gridsiteLog.clear();

        //const clientSiteControlvkl = $('#vklClientSiteId');
        //keyVehicleLogReport.clear().draw();
        //clientSiteControlvkl.html('');

        //clientSiteControl.html('');
        $.ajax({
            url: '/Radio/RadioCheckNew?handler=ClientSitesNew',
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
/*to enable for guard to update their documents-start*/
function getFormattedDate(dateValue, timeValue, seperator) {
    if (!dateValue) return null;

    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    let day = dateValue.getDate();

    if (day < 10) {
        day = '0' + day;
    }

    // format: 15-Mar-2022 or 15 Mar 2022
    let result = day + seperator + months[dateValue.getMonth()] + seperator + dateValue.getFullYear();

    if (timeValue)
        result = result + ' ' + timeValue;

    return result;
}
let gridGuardLicensesLogDaily = $('#tbl_guard_licenses1').DataTable({
    autoWidth: false,
    ordering: false,
    searching: false,
    paging: false,
    info: false,
    ajax: {
        url: '/Admin/GuardSettings?handler=GuardLicense',
        data: function (d) {
            d.guardId = $('#GuardLog_GuardLogin_GuardId').val();
        },
        dataSrc: ''
    },
    columns: [

        { data: 'licenseNo', width: "10%" },
        //{ data: 'licenseTypeName', width: "5%" },
        {

            data: 'licenseTypeText',
            width: '5%',
            render: function (data, type, row) {
                if (type === 'display') {
                    if (data === null) {
                        return row.licenseTypeName;
                    } else {
                        return data;
                    }
                } else {
                    return data;
                }
            }
        },
        { data: 'expiryDate', width: '10%', orderable: true },
        { data: 'reminder1', width: "3%" },
        { data: 'reminder2', width: '3%' },
        { data: 'fileName', width: '5%' },
        {
            targets: -1,
            data: null,
            defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_edit_guard_license1"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
                '<button  class="btn btn-outline-danger mr-2" name="btn_delete_guard_licence1"><i class="fa fa-trash mr-2"></i>Delete</button>',
            width: '5%'
        }],
    columnDefs: [{
        targets: 5,
        data: 'fileName',
        render: function (data, type, row, meta) {
            if (data)
                return '<a href="' + row.fileUrl + '" target="_blank">' + data + '</a>';
            return '-';
        }
    }],
    'createdRow': function (row, data, index) {
        if (data.expiryDate !== null) {
            $('td', row).eq(2).html(getFormattedDate(new Date(data.expiryDate), null, ' '));
        }
    },
});
let gridGuardCompliancesLogDaily = $('#tbl_guard_compliances1').DataTable({
    autoWidth: false,
    ordering: false,
    searching: false,
    paging: false,
    info: false,
    ajax: {
        url: '/Admin/GuardSettings?handler=GuardCompliances',
        data: function (d) {
            d.guardId = $('#GuardLog_GuardLogin_GuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'referenceNo', width: "6%" },
        { data: 'hrGroupText', width: "6%" },
        { data: 'description', width: "7%" },
        { data: 'expiryDate', width: "8%" },
        { data: 'reminder1', width: "3%" },
        { data: 'reminder2', width: "3%" },
        {
            data: 'fileName',
            render: function (data, type, row, meta) {
                if (data)
                    var guardid = row.guardId;
                return '<a href="/uploads/guards/' + guardid + '/' + data + '" target="_blank">' + data + '</a>';
                return '-';
            },
            width: "10%"
        },

        {
            targets: -1,
            data: null,
            defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_edit_guard_compliance1"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
                '<button  class="btn btn-outline-danger mr-2" name="btn_delete_guard_compliance1"><i class="fa fa-trash mr-2"></i>Delete</button>',
            orderable: false,
            width: "12%"
        }],
    columnDefs: [{
        targets: 5,
        data: 'fileName',
        render: function (data, type, row, meta) {
            if (data)
                return '<a href="' + row.fileUrl + '" target="_blank">' + data + '</a>';
            return '-';
        }
    }],
    'createdRow': function (row, data, index) {
        if (data.expiryDate !== null) {
            $('td', row).eq(3).html(getFormattedDate(new Date(data.expiryDate), null, ' '));

        }
    },
});

var guardPinPurpose = 'hr';

$('#btnHRDetails').on('click', function () {
    guardPinPurpose = 'hr';
    $('#txt_guardKey').val('');
    $('#txt_guardKeyNewPIN').val('');
    clearGuardValidationSummary('GuardLoginValidationSummaryHR');
    clearGuardValidationSummary('GuardLoginValidationSummaryHRNewPIN');
    $.ajax({
        url: '/Admin/GuardSettings?handler=CheckIfPINSetForTheGuard',
        type: 'POST',
        data: {
            guardId: $('#GuardLog_GuardLogin_GuardId').val()
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.accessPermission) {
            $('#loginHrNewPasswordSetGuard').modal('show');
            $('#loginHrEditGuard').modal('hide');
        }
        else {
            $('#loginHrNewPasswordSetGuard').modal('hide');
            $('#loginHrEditGuard').modal('show');
        }
    });
});

$('#btnRosterDetails').on('click', function () {
    var fromLogbook = window.location.pathname.toLowerCase().indexOf('/incident/') > -1 || window.location.pathname.toLowerCase().indexOf('/guard/') > -1;
    var guardId = $('#GuardLog_GuardLogin_GuardId').val();

    if (fromLogbook && guardId) {
        // Skip PIN verification in logbook context
        $.ajax({
            url: '/Admin/GuardSettings?handler=GetGuardPermissions',
            type: 'GET',
            data: { guardId: guardId }
        }).done(function (result) {
            openGuardRosterPortal($('#ClientSiteID').val(), result.isAdminRoster, guardId, result.isROEditor, true);
        });
        return;
    }

    guardPinPurpose = 'roster';
    $('#txt_guardKey').val('');
    $('#txt_guardKeyNewPIN').val('');
    clearGuardValidationSummary('GuardLoginValidationSummaryHR');
    clearGuardValidationSummary('GuardLoginValidationSummaryHRNewPIN');
    $.ajax({
        url: '/Admin/GuardSettings?handler=CheckIfPINSetForTheGuard',
        type: 'POST',
        data: {
            guardId: guardId
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.accessPermission) {
            $('#loginHrNewPasswordSetGuard').modal('show');
            $('#loginHrEditGuard').modal('hide');
        }
        else {
            $('#loginHrNewPasswordSetGuard').modal('hide');
            $('#loginHrEditGuard').modal('show');
        }
    });
});


$('#btnHO').on('click', function () {
    $('#Handover-modal').modal('show');
});



$('#btnGuardHrUpdateNewPIN').on('click', function () {
    clearGuardValidationSummary('GuardLoginValidationSummaryHRNewPIN');

    const securityLicenseNo = $('#txt_guardKeyNewPIN').val();

    // Validate the length of securityLicenseNo
    if (securityLicenseNo.length >= 4 && securityLicenseNo.length <= 6) {
        $.ajax({
            url: '/Admin/GuardSettings?handler=SaveNewPINSetForTheGuard',
            type: 'POST',
            data: {
                guardId: $('#GuardLog_GuardLogin_GuardId').val(),
                NewPIN: securityLicenseNo
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.accessPermission) {
                $('#loginHrNewPasswordSetGuard').modal('hide');

                if (guardPinPurpose === 'roster') {
                    var fromLogbook = window.location.pathname.toLowerCase().indexOf('/incident/') > -1 || window.location.pathname.toLowerCase().indexOf('/guard/') > -1;
                    openGuardRosterPortal($('#ClientSiteID').val(), $('#hdnIsAdminLoggedIn1').val() === 'AdminGlobal', $('#GuardLog_GuardLogin_GuardId').val(), result.isROEditor || false, fromLogbook);
                    return;
                }

                $.ajax({
                    type: 'GET',
                    url: '/Admin/Guardsettings?handler=GuardLicenseAndCompliancForGuardse',
                    data: { guardId: $('#GuardLog_GuardLogin_GuardId').val() },
                }).done(function (response) {
                    $('#loginHrEditGuard').modal('hide');
                    $('#addGuardModalnew').modal('show');
                    isPaused = true;
                    $('.btn-add-guard-addl-details').show();
                    $('#addGuardModal1').modal('show');
                    $('#GuardLicense_GuardId1').val(response[0].id);
                    $('#GuardCompliance_GuardId1').val(response[0].id);
                    $('#GuardComplianceandlicense_GuardId').val(response[0].id);
                    $('#GuardComplianceandlicense_LicenseNo').val(response[0].securityNo);

                    var selectedValues = [];
                    if (response[0].isRCAccess) {
                        selectedValues.push(4);
                    }
                    if (response[0].isKPIAccess) {
                        selectedValues.push(3);
                    }
                    if (response[0].isLB_KV_IR) {
                        selectedValues.push(1);
                    }
                    if (response[0].isSTATS) {
                        selectedValues.push(2);
                    }
                    selectedValues.forEach(function (value) {
                        $(".multiselect-option input[type=checkbox][value='" + value + "']").prop("checked", true);
                    });

                    gridGuardLicensesLogDaily.ajax.reload();
                    gridGuardCompliancesLogDaily.ajax.reload();
                    gridGuardTrainingAndAssessment.clear().draw();
                    gridGuardTrainingAndAssessment.ajax.reload();
                    $("#Guard_Access1").multiselect();
                    $("#Guard_Access1").val(selectedValues);
                    $("#Guard_Access1").multiselect("refresh");
                });

            } else {
                displayGuardValidationSummary('GuardLoginValidationSummaryHRNewPIN', result.successMessage);
            }
        }).fail(function () {
            console.error("Error in the first AJAX request.");
        }).always(function () {
            console.log("First AJAX request completed.");
        });
    } else {
        displayGuardValidationSummary('GuardLoginValidationSummaryHRNewPIN', 'Invalid PIN. It must be between 4 and 6 characters.');
    }
});
$('#txt_guardKey').on('keypress', function (e) {
    if (e.which === 13) { // Enter key
        e.preventDefault();
        $('#btnGuardHrUpdate').click();
    }
});


$('#btnGuardHrUpdate').on('click', function () {
    clearGuardValidationSummary('GuardLoginValidationSummaryHR');
    const securityLicenseNo = $('#txt_guardKey').val();
    if (securityLicenseNo === '') {
        displayGuardValidationSummary('GuardLoginValidationSummaryHR', 'Please enter PIN ');
    }
    else {


        $.ajax({
            url: '/Admin/GuardSettings?handler=GuardHrDocLoginConformation',
            type: 'POST',
            data: {
                guardId: $('#GuardLog_GuardLogin_GuardId').val(),
                key: securityLicenseNo
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {

            if (result.accessPermission) {

                $('#modelGuardLoginForHrUpdate').modal('hide');
                $('#loginHrEditGuard').modal('hide');

                if (guardPinPurpose === 'roster') {
                    var fromLogbook = window.location.pathname.toLowerCase().indexOf('/incident/') > -1 || window.location.pathname.toLowerCase().indexOf('/guard/') > -1;
                    openGuardRosterPortal($('#ClientSiteID').val(), $('#hdnIsAdminLoggedIn1').val() === 'AdminGlobal', $('#GuardLog_GuardLogin_GuardId').val(), result.isROEditor || false, fromLogbook);
                    return;
                }

                $.ajax({
                    type: 'GET',
                    url: '/Admin/Guardsettings?handler=GuardLicenseAndCompliancForGuardse',
                    data: { guardId: $('#GuardLog_GuardLogin_GuardId').val() },
                }).done(function (response) {
                    $('#loginHrEditGuard').modal('hide');
                    $('#addGuardModalnew').modal('show');
                    $('#Guard_Id').val(response[0].id);
                    isPaused = true;
                    $('.btn-add-guard-addl-details').show();
                    $('#addGuardModal1').modal('show');
                    $('#GuardLicense_GuardId1').val(response[0].id);
                    $('#GuardCompliance_GuardId1').val(response[0].id);
                    $('#GuardComplianceandlicense_GuardId').val(response[0].id);
                    $('#GuardComplianceandlicense_LicenseNo').val(response[0].securityNo);

                    // ;
                    var selectedValues = [];
                    if (response[0].isRCAccess) {
                        selectedValues.push(4);
                    }
                    if (response[0].isKPIAccess) {
                        selectedValues.push(3);
                    }
                    if (response[0].isLB_KV_IR) {
                        selectedValues.push(1);
                    }
                    if (response[0].isSTATS) {
                        selectedValues.push(2);
                    }
                    //selectedValues.forEach(function (value) {

                    //    $(".multiselect-option input[type=checkbox][value='" + value + "']").prop("checked", true);
                    //});

                    gridGuardLicensesLogDaily.ajax.reload();
                    gridGuardCompliancesLogDaily.ajax.reload();
                    gridGuardTrainingAndAssessment.ajax.reload();
                    $("#Guard_Access1").multiselect();
                    $("#Guard_Access1").val(selectedValues);
                    $("#Guard_Access1").multiselect("refresh");
                });


            }
            else {






                displayGuardValidationSummary('GuardLoginValidationSummaryHR', result.successMessage);



            }
        });






    }
});



function displayGuardValidationSummaryNew(divId, message, isSuccess) {
    const $summaryDiv = $('#' + divId);

    // Set the message
    $summaryDiv.text(message);

    // Apply success or error styling
    if (isSuccess) {
        $summaryDiv.css({
            'color': 'green',
            'font-size': '12px' // Set font size
        });
    } else {
        $summaryDiv.css({
            'color': 'red',
            'font-size': '12px' // Set font size
        });
    }

    // Ensure the div is visible
    $summaryDiv.show();
}


$('#forgotpassword').click(function (e) {
    e.preventDefault(); // Prevent the default anchor behavior
    $('#spinner').removeClass('d-none').addClass('d-inline-block'); // Show spinner
    clearGuardValidationSummary('GuardLoginValidationSummaryHR');
    clearGuardValidationSummary('GuardLoginValidationSummaryHRNewPIN');


    // AJAX request example
    $.ajax({
        url: '/Admin/GuardSettings?handler=ResetGaurdHrPin',
        type: 'POST',
        data: {
            guardId: $('#GuardLog_GuardLogin_GuardId').val(),
            siteName: $('#hidden_guardKey').val()

        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {

        if (result.success) {
            displayGuardValidationSummaryNew('GuardLoginValidationSummaryHR', result.message, true);
        }
        else {
            displayGuardValidationSummaryNew('GuardLoginValidationSummaryHR', result.message, false);
        }
        // Hide spinner after action is complete
        $('#spinner').addClass('d-none').removeClass('d-inline-block');
    });
});






//$('#btnHRDetails').on('click', function () {
//    $.ajax({
//        type: 'GET',
//        url: '/Admin/Guardsettings?handler=GuardLicenseAndCompliancForGuardse',
//        data: { guardId: $('#GuardLog_GuardLogin_GuardId').val() },
//    }).done(function (response) {
//        $('#addGuardModalnew').modal('show');
//        isPaused = true;
//        $('.btn-add-guard-addl-details').show();

//        //var data = guardSettings.row($(this).parents('tr')).data();

//        //$('#Guard_Name1').val(response[0].name);
//        //$('#Guard_SecurityNo1').val(response[0].securityNo);
//        //$('#Guard_Initial1').val(response[0].initial);
//        //$('#Guard_State1').val(response[0].state);
//        //$('#Guard_Provider1').val(response[0].provider);
//        //$('#Guard_Mobile1').val(response[0].mobile)
//        //$('#Guard_Email1').val(response[0].email)
//        //$('#Guard_Id1').val(response[0].id);
//        //$('#cbIsActive1').prop('checked', response[0].isActive);
//        //$('#cbIsRCAccess1').prop('checked', response[0].isRCAccess);
//        //$('#cbIsKPIAccess1').prop('checked', response[0].isKPIAccess);
//        $('#addGuardModal1').modal('show');
//        $('#GuardLicense_GuardId1').val(response[0].id);
//        $('#GuardCompliance_GuardId1').val(response[0].id);
//        $('#GuardComplianceandlicense_GuardId').val(response[0].id);
//        $('#GuardComplianceandlicense_LicenseNo').val(response[0].securityNo);

//        // ;
//        var selectedValues = [];
//        if (response[0].isRCAccess) {
//            selectedValues.push(4);
//        }
//        if (response[0].isKPIAccess) {
//            selectedValues.push(3);
//        }
//        if (response[0].isLB_KV_IR) {
//            selectedValues.push(1);
//        }
//        if (response[0].isSTATS) {
//            selectedValues.push(2);
//        }
//        selectedValues.forEach(function (value) {

//            $(".multiselect-option input[type=checkbox][value='" + value + "']").prop("checked", true);
//        });
//        gridGuardLicensesLogDaily.ajax.reload();
//        gridGuardCompliancesLogDaily.ajax.reload();
//        $("#Guard_Access1").multiselect();
//        $("#Guard_Access1").val(selectedValues);
//        $("#Guard_Access1").multiselect("refresh");
//    });
//});
function clearGuardValidationLogDailySummary(validationControl) {
    $('#' + validationControl).removeClass('validation-summary-errors').addClass('validation-summary-valid');
    $('#' + validationControl).html('');
}
function resetGuardLicenseAddLogDailyModal() {
    $('#GuardLicense_Id1').val('');
    $('#GuardLicense_LicenseNo1').val('');
    $('#GuardLicense_LicenseType1').val('');
    $('#GuardLicense_Reminder11').val('45');
    $('#GuardLicense_Reminder21').val('7');
    $('#GuardLicense_ExpiryDate1').val('');
    $('#GuardLicense_FileName1').val('');
    $('#guardLicense_fileName1').text('None');
    clearGuardValidationLogDailySummary('licenseValidationSummary1');
}
$('#btnAddGuardLicenseLogDaily').on('click', function () {
    resetGuardLicenseAddLogDailyModal();

    /*timer pause while editing*/
    isPaused = true;
    $('#addGuardLicenseLogDailyModal').modal('show');
});
$('#upload_license_file1').on('change', function () {
    const file = $(this).get(0).files.item(0);
    const fileExtn = file.name.split('.').pop();
    if (!fileExtn || 'jpg,jpeg,png,bmp,pdf'.indexOf(fileExtn) < 0) {
        alert('Please select a valid file type');
        return false;
    }

    const formData = new FormData();
    formData.append("file", file);
    formData.append('guardId', $('#GuardLicense_GuardId1').val());

    $.ajax({
        type: 'POST',
        url: '/Admin/GuardSettings?handler=UploadGuardAttachment',
        data: formData,
        cache: false,
        contentType: false,
        processData: false,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        $('#GuardLicense_FileName1').val(data.fileName);
        $('#guardLicense_fileName1').text(data.fileName ? data.fileName : 'None');
    }).fail(function () {
    }).always(function () {
        $('#upload_license_file1').val('');
    });
});

$('#delete_license_file1').on('click', function () {
    const guardLicenseId = $('#GuardLicense_Id1').val();
    if (!guardLicenseId || parseInt(guardLicenseId) <= 0)
        return false;

    if (confirm('Are you sure want to remove the attachment')) {
        $.ajax({
            url: '/Admin/GuardSettings?handler=DeleteGuardAttachment',
            type: 'POST',
            data: {
                id: guardLicenseId,
                type: 'l'
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#GuardLicense_FileName1').val('');
                $('#guardLicense_fileName1').text('None');
                gridGuardLicenses.ajax.reload();
            }
            else {
                displayGuardValidationDailyLogSummary('licenseValidationSummary1', 'Delete failed.');
            }
        });
    }
});
function displayGuardValidationDailyLogSummary(validationControl, errors) {
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
$('#btn_save_guard_license1').on('click', function () {
    clearGuardValidationLogDailySummary('licenseValidationSummary1');
    /*To get the text inside the product dropdown*/
    var inputElement = document.querySelector("#GuardLicense_LicenseType1");
    var dropdown = document.getElementById("GuardLicense_LicenseType1");
    var selText = dropdown.options[dropdown.selectedIndex].text;

    var ser = $('#frm_add_license').serialize();
    // Get the value of the input element
    if (inputElement) { var inputValue = selText; $('#LicenseTypeOther1').val(inputValue); }
    $('#loader').show();
    $.ajax({
        url: '/Admin/GuardSettings?handler=SaveGuardLicense',
        data: $('#frm_add_license').serialize(),
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.status) {
            $('#addGuardLicenseLogDailyModal').modal('hide');
            gridGuardLicensesLogDaily.ajax.reload();
            if (!result.dbxUploaded) {
                displayGuardValidationDailyLogSummary('licenseValidationSummary1', 'License details saved successfully. However, upload to Dropbox failed.');
            }
        } else {
            displayGuardValidationDailyLogSummary('licenseValidationSummary1', result.message);
        }
    }).always(function () {
        $('#loader').hide();
    });
});
function resetGuardLicenseAddModal() {
    $('#GuardLicense_Id1').val('');
    $('#GuardLicense_LicenseNo1').val('');
    $('#GuardLicense_LicenseType1').val('');
    $('#GuardLicense_Reminder11').val('45');
    $('#GuardLicense_Reminder21').val('7');
    $('#GuardLicense_ExpiryDate1').val('');
    $('#GuardLicense_FileName1').val('');
    $('#guardLicense_fileName1').text('None');
    clearGuardValidationLogDailySummary('licenseValidationSummary1');
}
$('#tbl_guard_licenses1 tbody').on('click', 'button[name=btn_edit_guard_license1]', function () {
    resetGuardLicenseAddModal();
    var data = gridGuardLicensesLogDaily
        .row($(this).parents('tr')).data();
    $('#GuardLicense_LicenseNo1').val(data.licenseNo);
    if (data.licenseTypeText == null) {
        $('#GuardLicense_LicenseType1').val(data.licenseTypeName);
    }
    else {
        $('#GuardLicense_LicenseType1').val(data.licenseType);
    }

    $('#GuardLicense_Reminder11').val(data.reminder1);
    $('#GuardLicense_Reminder21').val(data.reminder2);
    if (data.expiryDate) {
        $('#GuardLicense_ExpiryDate1').val(data.expiryDate.split('T')[0]);
    }
    $('#GuardLicense_Id1').val(data.id);
    $('#GuardLicense_GuardId1').val(data.guardId);
    $('#GuardLicense_FileName1').val(data.fileName);
    $('#guardLicense_fileName1').text(data.fileName ? data.fileName : 'None');
    $('#addGuardLicenseLogDailyModal').modal('show');
});

$('#tbl_guard_licenses1 tbody').on('click', 'button[name=btn_delete_guard_licence1]', function () {
    var data = gridGuardLicensesLogDaily.row($(this).parents('tr')).data();
    if (confirm('Are you sure want to delete this Guard License?')) {
        $.ajax({
            type: 'POST',
            url: '/Admin/GuardSettings?handler=DeleteGuardLicense',
            data: { 'id': data.id },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success)
                gridGuardLicensesLogDaily.ajax.reload();
        })
    }
});
function resetGuardComplianceAddModal() {
    $('#GuardCompliance_Id1').val('');
    $('#GuardCompliance_ReferenceNo1').val('');
    $('#GuardCompliance_Description1').val('');
    $('#GuardCompliance_Reminder11').val('45');
    $('#GuardCompliance_Reminder21').val('7');
    $('#GuardCompliance_ExpiryDate1').val('');
    $('#GuardCompliance_FileName1').val('');
    $('#guardCompliance_fileName1').text('None');
    $('#GuardCompliance_HrGroup1').val('');
    clearGuardValidationSummary('complianceValidationSummary1');
}
$('#btnAddGuardCompliance1').on('click', function () {
    resetGuardComplianceAddModal();
    $('#addGuardCompliancesLogDailyModal').modal('show');
})
$('#btn_save_guard_compliance1').on('click', function () {
    clearGuardValidationSummary('complianceValidationSummary1');
    $('#loader').show();
    $.ajax({
        url: '/Admin/GuardSettings?handler=SaveGuardCompliance',
        data: $('#frm_add_compliance').serialize(),
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.status) {
            $('#addGuardCompliancesLogDailyModal').modal('hide');
            gridGuardCompliancesLogDaily.ajax.reload();
            if (!result.dbxUploaded) {
                displayGuardValidationSummary('complianceValidationSummary1', 'Compliance details saved successfully. However, upload to Dropbox failed.');
            }
        } else {
            displayGuardValidationSummary('complianceValidationSummary1', result.message);
        }
    }).always(function () {
        $('#loader').hide();
    });
});
$('#upload_compliance_file1').on('change', function () {
    const file = $(this).get(0).files.item(0);
    const fileExtn = file.name.split('.').pop();
    if (!fileExtn || 'jpg,jpeg,png,bmp,pdf'.indexOf(fileExtn) < 0) {
        alert('Please select a valid file type');
        return false;
    }

    const formData = new FormData();
    formData.append("file", file);
    formData.append('guardId', $('#GuardCompliance_GuardId1').val());

    $.ajax({
        type: 'POST',
        url: '/Admin/GuardSettings?handler=UploadGuardAttachment',
        data: formData,
        cache: false,
        contentType: false,
        processData: false,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        $('#GuardCompliance_FileName1').val(data.fileName);
        $('#guardCompliance_fileName1').text(data.fileName ? data.fileName : 'None');
    }).fail(function () {
    }).always(function () {
        $('#upload_compliance_file1').val('');
    });
});
$('#delete_compliance_file1').on('click', function () {
    const guardComplianceId = $('#GuardCompliance_Id1').val();
    if (!guardComplianceId || parseInt(guardComplianceId) <= 0)
        return false;

    if (confirm('Are you sure want to remove the attachment')) {
        $.ajax({
            url: '/Admin/GuardSettings?handler=DeleteGuardAttachment',
            type: 'POST',
            data: {
                id: guardComplianceId,
                type: 'c'
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#GuardCompliance_FileName1').val('');
                $('#guardCompliance_fileName1').text('None');
                gridGuardCompliancesLogDaily.ajax.reload();
            }
            else {
                displayGuardValidationSummary('complianceValidationSummary1', 'Delete failed.');
            }
        });
    }
});
$('#tbl_guard_compliances1 tbody').on('click', 'button[name=btn_edit_guard_compliance1]', function () {
    resetGuardComplianceAddModal();
    var data = gridGuardCompliancesLogDaily.row($(this).parents('tr')).data();
    $('#GuardCompliance_ReferenceNo1').val(data.referenceNo);
    $('#GuardCompliance_Description1').val(data.description);
    $('#GuardCompliance_Reminder11').val(data.reminder1);
    $('#GuardCompliance_Reminder21').val(data.reminder2);
    if (data.expiryDate) {
        $('#GuardCompliance_ExpiryDate1').val(data.expiryDate.split('T')[0]);
    }
    $('#GuardCompliance_Id1').val(data.id);
    $('#GuardCompliance_FileName1').val(data.fileName);
    $('#guardCompliance_fileName1').text(data.fileName ? data.fileName : 'None');
    $('#GuardCompliance_HrGroup1').val(data.hrGroup);
    $('#addGuardCompliancesLogDailyModal').modal('show');
});

$('#tbl_guard_compliances1 tbody').on('click', 'button[name=btn_delete_guard_compliance1]', function () {
    var data = gridGuardCompliancesLogDaily.row($(this).parents('tr')).data();
    if (confirm('Are you sure want to delete this Guard Compliance?')) {
        $.ajax({
            type: 'POST',
            url: '/Admin/GuardSettings?handler=DeleteGuardCompliance',
            data: { 'id': data.id },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success)
                gridGuardCompliancesLogDaily.ajax.reload();
        })
    }
});
function ExpiredDocuments() {
    isPaused = true;
    $.ajax({
        type: 'GET',
        url: '/Admin/GuardSettings?handler=ExpiredDocuments',
        data: { 'guardId': $('#GuardLog_GuardLogin_GuardId').val() },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.length > 0) {
            var newmsg = "<b> Following Guard Documents are expiring.</br> </b>";
            newmsg = newmsg + "<table  border=\"1px solid black\">";
            newmsg = newmsg + " <thead><th>Document Type</th><th>Document No</th><th>Guard Name</th><th>Expiry Date</th></thead>";
            newmsg = newmsg + "<tbody>";
            $.each(result, function (key, value) {
                newmsg = newmsg + value.value;
            });
            newmsg = newmsg + "</tbody></table>";
            new MessageModal({ message: newmsg, onabort: isPaused = false }).showWarning();
        }
        else {
            isPaused = false;
        }
    })
}
$("#addGuardModalnew").on("hidden.bs.modal", function () {
    isPaused = false;
});
$("#addGuardModalnew").on("shown.bs.modal", function () {
    isPaused = true;
    var userId=0;

    if ($('#ClientSiteName').val() === 'Onboarding') {
        userId = 74;
    }
    //var userId = $('#GuardLogin_UserID').val();
    const token = $('input[name="__RequestVerificationToken"]').val();
    var guardId = $('#GuardLog_GuardLogin_GuardId').val();
    if (userId == 74) {
        $.ajax({
            url: '/Guard/DailyLog?handler=SaveGuardTrainingAndAssessmentFromOnboardingUsers',
            data: {
                'UserId': userId,
                'GuardId': guardId
            },
            type: 'POST',
           headers: { 'RequestVerificationToken': token },
        }).done(function (result) {
            if (result.success) {
                
                gridGuardTrainingAndAssessment.clear().draw();
                gridGuardTrainingAndAssessment.ajax.reload();
            }
            else {
              
                let errorList = [];
                result.errors.forEach(function (item) {
                    errorList.push(item.errorList[0]);
                });
                alert('Update failed. ' + errorList.join(', '));
            }

        }).fail(function () {
            console.log('error');
        }).always(function () {
            $('#loader').hide();
        });
    }
});

var hrDescriptionConfigs = {};

function updateDateVisibility(dt, keepValue) {
    // Immediate update
    applyVisibility(dt, keepValue);

    // Delayed update to ensure it sticks (overriding any UI plugin resets)
    setTimeout(function () {
        applyVisibility(dt, keepValue);
    }, 150);

    function applyVisibility(dt, keepValue) {
        if (dt == 1) { // DOI Only
            $('#LicanseTypeFilter').prop('checked', true).trigger('change', [keepValue]);
            $('#doiToggleContainer').css('visibility', 'hidden');
            $('#doiNoteContainer').css('visibility', 'hidden');
            $('#doePendingToggleContainer').css('visibility', 'hidden');
        } else if (dt == 2) { // DOE Only
            $('#LicanseTypeFilter').prop('checked', false).trigger('change', [keepValue]);
            $('#doiToggleContainer').css('visibility', 'hidden');
            $('#doiNoteContainer').css('visibility', 'hidden');
            $('#doePendingToggleContainer').css('visibility', 'visible');
        } else { // Both (0) or Default
            // We don't automatically check/uncheck the toggle for "Both", 
            // just make it visible so the user can choose.
            $('#doiToggleContainer').css('visibility', 'visible');
            $('#doiNoteContainer').css('visibility', 'visible');
            $('#doePendingToggleContainer').css('visibility', 'hidden');
            // Ensure the current toggle state is reflected in labels/constraints without clearing value
            $('#LicanseTypeFilter').trigger('change', [keepValue]);
        }
    }
}

$('#LicanseTypeFilter').on('change', function (event, keepValue) {
    const isDOI = $(this).prop('checked');
    const $label = $('#ComplianceDate');
    const $dateInput = $('#GuardComplianceAndLicense_ExpiryDate1');

    if (isDOI) {
        $label.text('Issue Date (DOI)');
        $dateInput.prop('min', ''); // No minimum for Issue Date
        $dateInput.prop('max', new Date().toJSON().split('T')[0]); // Cannot be in the future
        $('#doePendingToggleContainer').css('visibility', 'hidden');

    } else {
        $label.text('Expiry Date (DOE)');
        $dateInput.prop('min', new Date().toJSON().split('T')[0]); // Cannot be in the past
        $dateInput.prop('max', ''); // No maximum for Expiry Date
        $('#doePendingToggleContainer').css('visibility', 'visible');
    }

    // Only clear the value if not explicitly told to keep it
    if (keepValue !== true) {
        $dateInput.val('');
    }
});

//To get the Compliance and License data start
$('#btnAddGuardLicenseKey,#btnAddGuardLicenseKey2').on('click', function () {
    resetGuardLicenseandComplianceAddModal();
    const messageHtml2 = '';
    $('#schRunStatusNew').html(messageHtml2);
    $("#ComplianceHiddenDiv").css({
        "pointer-events": "",
        "opacity": ""
    }).removeAttr("disabled");
    $('#complianceValidationDiv').hide();
    $('#addGuardCompliancesLicenseModal').modal('show');
});
function resetGuardLicenseandComplianceAddModal() {
    $('#GuardComplianceandlicense_Id').val('');
    $('#Description').val('');
    $('#LicanseTypeFilter').prop('checked', false).trigger('change');
    $('#doiToggleContainer').css('visibility', 'visible');
    $('#doiNoteContainer').css('visibility', 'visible');
    $('#IsDateFilterEnabledHidden').val(false)
    $("#GuardComplianceAndLicense_ExpiryDate1").val('');
    $("#GuardComplianceAndLicense_ExpiryDate1").prop('min', function () {
        return new Date().toJSON().split('T')[0];
    });
    $("#GuardComplianceAndLicense_ExpiryDate1").prop('max', '');
    $('#HRGroup').val('');
    $(".es-list").empty();
    $('#guardComplianceandlicense_fileName1').text('None');
    $('#GuardComplianceandlicense_FileName1').val('');
    $('#GuardComplianceandlicense_CurrentDateTime').val('');
    $('#complianceValidationDiv').show();
    clearGuardValidationSummary('compliancelicanseValidationSummary');
}
$('#upload_complianceandlicanse_file').on('change', function () {
    const file = $(this).get(0).files; //.item(0); 
    FileuploadFileChanged(file);
});

FileuploadFileChanged = function (allfile) {
    const file = allfile.item(0); // allfile.get(0).files.item(0);
    const fileExtn = "." + file.name.split('.').pop().toLowerCase();
    console.log('fileExtn: ' + fileExtn);
    if (!fileExtn || allowedfiletypes.includes(fileExtn) == false) {
        alert('Please select a valid file type');
        return false;
    }
    var Desc = $('#Description').val();
    var expiryDate = $('#GuardComplianceAndLicense_ExpiryDate1').val();
    Desc = Desc.substring(3);
    var cleanText = Desc.replace(/[✔️❌]/g, '').trim();
    const formData = new FormData();
    formData.append("file", file);
    formData.append('guardId', $('#GuardComplianceandlicense_GuardId').val());
    formData.append('LicenseNo', $('#GuardComplianceandlicense_LicenseNo').val());
    formData.append('Description', cleanText);
    formData.append('HRID', $('#HRGroup').val());
    formData.append('ExpiryDate', $('#GuardComplianceAndLicense_ExpiryDate1').val());
    formData.append('DateType', $('#IsDateFilterEnabledHidden').val());
    if (Desc == '') {
        (confirm('Please select Description and Expiry/Issue Date'))
    }
    if (expiryDate == '') {
        (confirm('Please select the Expiry Date or Issue Date first, and then attach the document'))
    }
    else {
        fileprocess(allfile);

        $.ajax({
            type: 'POST',
            url: '/Admin/GuardSettings?handler=UploadGuardAttachment',
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            $('#GuardComplianceandlicense_FileName1').val(data.fileName);
            $('#guardComplianceandlicense_fileName1').text(data.fileName ? data.fileName : 'None');
            $('#GuardComplianceandlicense_CurrentDateTime').val(data.currentDate);
        }).fail(function () {
        }).always(function () {
            $('#upload_complianceandlicanse_file').val('');
        });
    }

}

let gridGuardLicensesAndLicenceKey = $('#tbl_guard_licensesAndComplianceKey').DataTable({
    autoWidth: false,
    ordering: false,
    searching: false,
    paging: false,
    info: false,
    ajax: {
        url: '/Admin/GuardSettings?handler=GuardLicenseAndComplianceData',
        data: function (d) {
            d.guardId = $('#GuardLog_GuardLogin_GuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'hrGroupText', width: "12%" },
        { data: 'description', width: "27%" },
        { data: 'expiryDate', width: '15%', orderable: true },
        { data: 'fileName', width: '30%' },
        { data: 'status', width: "1%" },
        {
            targets: -1,
            data: null,
            render: function (data, type, row, meta) {
                if (data.isLogin == 'Guard') {
                    if (data.hrBanEdit) {

                        return '<button type="button" class="btn btn-outline-primary mr-2" name="btn_edit_guard_licenseAndCompliance" disabled><i class="fa fa-pencil mr-2"></i>Edit</button>&nbsp;' +
                            '<button class="btn btn-outline-danger" name="btn_delete_guard_licenseAndCompliance" disabled><i class="fa fa-trash"></i></button>';
                    } else {

                        return '<button type="button" class="btn btn-outline-primary mr-2" name="btn_edit_guard_licenseAndCompliance"><i class="fa fa-pencil mr-2"></i>Edit</button>&nbsp;' +
                            '<button class="btn btn-outline-danger" name="btn_delete_guard_licenseAndCompliance"><i class="fa fa-trash"></i></button>';
                    }
                }


            },
            defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_edit_guard_licenseAndCompliance"><i class="fa fa-pencil mr-2"></i>Edit</button>&nbsp;' +
                '<button  class="btn btn-outline-danger mr-2" name="btn_delete_guard_licenseAndCompliance"><i class="fa fa-trash"></i></button>',
            width: '15%'
        }],
    columnDefs: [{
        targets: 3,
        data: 'fileName',
        render: function (data, type, row, meta) {
            if (data)
                return '<a href="/Uploads/Guards/License/' + row.licenseNo + '/' + row.fileUrl + '" target="_blank">' + data + '</a>';
            return '-';
        }
    },
    {
        targets: 4,
        data: 'status',
        render: function (data, type, row, meta) {
            var currentDate = new Date();
            var ExpiryDate = new Date(row.expiryDate);
            var timeDifference = ExpiryDate - currentDate;
            var daysDifference = Math.ceil(timeDifference / (1000 * 60 * 60 * 24));
            var pendingDifference = currentDate - ExpiryDate;
            var pendinddaysDifference = Math.ceil(pendingDifference / (1000 * 60 * 60 * 24));
            /*var statusColor = 'green';*/
            var statusColor = row.statusColor;

            //if (row.dateType == true) {
            //    statusColor = 'green';
            //}
            //else if (row.expiryDate != null) {
            //    if (daysDifference <= 45) {
            //        statusColor = 'yellow';
            //    }

            //    if (ExpiryDate < currentDate && row.dateType != true) {
            //        if (pendinddaysDifference <= 60 && row.isPending == true) {
            //            statusColor = 'red';
            //        }
            //        statusColor = 'red';
            //    }
            //}


            return '<div style="display: flex; align-items: center; justify-content: center;"><div style="background-color:' + statusColor + '; width: 10px; height: 10px; border-radius: 50%;"></div></div>';
        }



    }
    ],
    'createdRow': function (row, data, index) {
        if (data.expiryDate !== null) {
            var formattedDate = getFormattedDate(new Date(data.expiryDate), null, ' ');
            if (data.dateType === true) {
                formattedDate = formattedDate + '  (I)';
                $('td', row).eq(2).html(formattedDate);
            }
            else {
                $('td', row).eq(2).html(getFormattedDate(new Date(data.expiryDate), null, ' '));
            }

        }
    },
});
gridGuardLicensesAndLicenceKey.on('draw.dt', function () {
    var tbody = $('#tbl_guard_licensesAndComplianceKey tbody');
    var rows = tbody.find('tr');
    var lastGroupValue = null;

    rows.each(function (index, row) {
        var currentGroupValue = $(row).find('td:eq(0)').text();

        if (currentGroupValue !== lastGroupValue) {
            lastGroupValue = currentGroupValue;

            var headerRow = $('<tr>').addClass('group-header').append($('<th>').attr('colspan', 6));
            headerRow.css('background-color', '#CCCCCC');
            $(row).before(headerRow);
        }
    });
});
//To get the data in description dropdown start
$('#Description').attr('placeholder', 'Select');
$('#Description').editableSelect({
    //filter: false,
    effects: 'slide'
}).on('select.editable-select', function (e, li) {
    $('#GuardComplianceandlicense_FileName1').val('');
    $('#guardComplianceandlicense_fileName1').text('None');

    const selectedItem = li.val();
    if (hrDescriptionConfigs && hrDescriptionConfigs[selectedItem] !== undefined) {
        updateDateVisibility(hrDescriptionConfigs[selectedItem], true);
    }
});
$('#HRGroup').on('change', function () {
    $('#Description').val('');
    $('#GuardComplianceandlicense_FileName1').val('');
    $('#guardComplianceandlicense_fileName1').text('None');
    var Descriptionval = $('#HRGroup').val();
    var GuardID = $('#GuardComplianceandlicense_GuardId').val();
    const token = $('input[name="__RequestVerificationToken"]').val();
    const ulClients = $('#Description').siblings('ul.es-list');
    ulClients.html('');
    $.ajax({
        url: '/Admin/GuardSettings?handler=HRDescription',
        type: 'GET',
        data: {
            HRid: Descriptionval,
            GuardID: GuardID
        },
        headers: { 'RequestVerificationToken': token }
    }).done(function (DescVal) {
        hrDescriptionConfigs = {}; // Clear old configs
        DescVal.forEach(function (DescVals) {
            var mark = ''; // Initialize the mark variable

            // Store config by ID
            hrDescriptionConfigs[DescVals.id] = DescVals.dateType;

            if (DescVals.description != null) {
                if (DescVals.usedDescription == null) {
                    //mark = '❌';
                    mark = '<i class="fa fa-close" style="font-size:24px;color:red"></i>'
                    //ulClients.append('<li class="es-visible" value="' + DescVals.description + '">' + DescVals.referenceNo + '      ' + DescVals.description + ' ' + mark + '</li>');
                    ulClients.append('<li class="es-visible" value="' + DescVals.id + '" style="display: flex; justify-content: space-between; padding: 10px; border-bottom: 1px solid #ddd;">' +
                        '<span class="ref-no" style="flex: 1;">' + DescVals.referenceNo + ' </span>' +
                        '<span class="desc" style="flex: 2; margin-left: 10px;">' + DescVals.description + ' </span>' +
                        mark +
                        '</li>');
                }
                else {
                    mark = '<i class="fa fa-check" style="font-size:24px;color:green"></i>'
                    //mark = '<span style="color: green !important;">✔️</span>';
                    // ulClients.append('<li class="es-visible" value="' + DescVals.description + '">' + DescVals.referenceNo + '     ' + DescVals.description + ' ' + mark + '</li>');
                    ulClients.append('<li class="es-visible" value="' + DescVals.id + '" style="display: flex; justify-content: space-between; padding: 10px; border-bottom: 1px solid #ddd;">' +
                        '<span class="ref-no" style="flex: 1;">' + DescVals.referenceNo + ' </span>' +
                        '<span class="desc" style="flex: 2; margin-left: 10px;">' + DescVals.description + ' </span>' +
                        mark +
                        '</li>');
                }
            }

        });

        // If we are in edit mode, ensure the description is selected in the editableSelect list
        var currentDesc = $('#Description').val();
        if (currentDesc) {
            var itemToSelect = ulClients.find('li').filter(function () {
                return $(this).find('.desc').text().trim() === currentDesc;
            })[0];
            if (itemToSelect) {
                // We don't use .selectableSelect('select') here as it might trigger recursive calls
                $(itemToSelect).addClass('selected');
            }
        }
    })


});

$('#btn_save_guard_compliancelicenseKey').on('click', function () {
    clearGuardValidationSummary('compliancelicanseValidationSummary1');

    var ExpirayDateVal = $('#GuardComplianceAndLicense_ExpiryDate1').val();
    var HrVal = $('#HRGroup').val();
    var DescVal = $('#Description').val();
    var FileVa = $('#guardComplianceandlicense_fileName1').html();
    const messageHtml = '';
    $('#schRunStatusNew').html(messageHtml);

    const token = $('input[name="__RequestVerificationToken"]').val();
    let LoginVal = $('#hdnIsAdminLoggedIn1').val();
    let selectedItem = $('.es-visible').val();
    var HRBan = false;
    $('#complianceValidationDiv').show();
    if (LoginVal == 'GuardLogin') {
        $.ajax({
            url: '/Admin/GuardSettings?handler=HRDescriptionBanDetails',
            type: 'GET',
            data: {
                DescriptionID: selectedItem
            },
            headers: { 'RequestVerificationToken': token }
        }).done(function (DescVal) {
            if (DescVal?.hrBanEdit === true) {
                HRBan = true;
                displayGuardValidationSummary(
                    'compliancelicanseValidationSummary1',
                    'This description is restricted and cannot be added.'
                );
            } else {
                processFormSubmission();
            }

        });
    }

    function processFormSubmission() {
        if (HrVal != '' && DescVal != '' && FileVa != 'None') {

            if (ExpirayDateVal == '') {

                alert('Please Enter the Expiry Date or Date of issue');
                //if (confirm('Are you sure you not want to enter expiry Date')) {
                //    $('#schRunStatusNew').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Please wait...');
                //    $('#loader').show();
                //    $.ajax({
                //        url: '/Admin/GuardSettings?handler=SaveGuardComplianceandlicanseNew',
                //        data: $('#frm_add_complianceandlicense').serialize(),
                //        type: 'POST',
                //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                //    }).done(function (result) {
                //        if (result.status) {
                //            $('#addGuardCompliancesLicenseModal').modal('hide');
                //            const messageHtml1 = '';
                //            $('#schRunStatusNew').html(messageHtml1);
                //            gridGuardLicensesAndLicenceKey.ajax.reload();

                //            if (!result.dbxUploaded) {
                //                // displayGuardValidationSummary('compliancelicanseValidationSummary1', 'Compliance details saved successfully. However, upload to Dropbox failed.');
                //            }
                //        } else {
                //            const messageHtml = '';
                //            $('#schRunStatusNew').html(messageHtml);
                //            displayGuardValidationSummary('compliancelicanseValidationSummary1', result.message);
                //        }
                //    }).always(function () {
                //        $('#loader').hide();
                //    });
                //}
            }
            else {
                $('#loader').show();
                $('#schRunStatusNew').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Please wait...');
                $.ajax({
                    url: '/Admin/GuardSettings?handler=SaveGuardComplianceandlicanseNew',
                    data: $('#frm_add_complianceandlicense').serialize(),
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.status) {
                        $('#addGuardCompliancesLicenseModal').modal('hide');
                        const messageHtml1 = '';
                        $('#schRunStatusNew').html(messageHtml1);
                        gridGuardLicensesAndLicenceKey.ajax.reload();

                        if (!result.dbxUploaded) {
                            // displayGuardValidationSummary('compliancelicanseValidationSummary1', 'Compliance details saved successfully. However, upload to Dropbox failed.');
                        }
                    } else {
                        const messageHtml = '';
                        $('#schRunStatusNew').html(messageHtml);
                        displayGuardValidationSummary('compliancelicanseValidationSummary1', result.message);
                    }
                }).always(function () {
                    $('#loader').hide();
                });
            }

        }
        else {
            $('#schRunStatusNew').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Please wait...');
            $('#loader').show();
            $.ajax({
                url: '/Admin/GuardSettings?handler=SaveGuardComplianceandlicanseNew',
                data: $('#frm_add_complianceandlicense').serialize(),
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $('#addGuardCompliancesLicenseModal').modal('hide');
                    const messageHtml1 = '';
                    $('#schRunStatusNew').html(messageHtml1);
                    gridGuardLicensesAndLicenceKey.ajax.reload();

                    if (!result.dbxUploaded) {
                        // displayGuardValidationSummary('compliancelicanseValidationSummary1', 'Compliance details saved successfully. However, upload to Dropbox failed.');
                    }
                } else {
                    const messageHtml = '';
                    $('#schRunStatusNew').html(messageHtml);
                    displayGuardValidationSummary('compliancelicanseValidationSummary1', result.message);
                }
            }).always(function () {
                $('#loader').hide();
            });
        }
    }












});

$('#tbl_guard_licensesAndComplianceKey tbody').on('click', 'button[name=btn_edit_guard_licenseAndCompliance]', function () {
    resetGuardLicenseandComplianceAddModal();
    $("#ComplianceHiddenDiv").css({
        "pointer-events": "none",
        "opacity": "0.5"
    }).attr("disabled", "disabled");
    var data = gridGuardLicensesAndLicenceKey.row($(this).parents('tr')).data();

    if (data.expiryDate) {
        $('#GuardComplianceAndLicense_ExpiryDate1').val(data.expiryDate.split('T')[0]);
    }
    $('#GuardComplianceandlicense_Id').val(data.id);
    $('#GuardComplianceandlicense_GuardId').val(data.GuardId);
    $('#HRGroup').val(data.hrGroup).trigger('change');
    $('#Description').val(data.description);
    $('#GuardComplianceandlicense_GuardId').val(data.guardId);
    $('#GuardComplianceandlicense_FileName1').val(data.fileName);
    $('#guardComplianceandlicense_fileName1').text(data.fileName ? data.fileName : 'None');

    // Logic for Edit Mode Visibility
    if (data.masterDateType == 0) {
        // Set toggle state to match record
        $('#LicanseTypeFilter').prop('checked', data.dateType == true);
        // Apply "Both" visibility (shows toggle)
        updateDateVisibility(0, true);
    } else {
        // Apply Master constraint (DOI or DOE)
        updateDateVisibility(data.masterDateType, true);
    }

    $('#addGuardCompliancesLicenseModal').modal('show');
});
$('#tbl_guard_licensesAndComplianceKey tbody').on('click', 'button[name=btn_delete_guard_licenseAndCompliance]', function () {
    var data = gridGuardLicensesAndLicenceKey.row($(this).parents('tr')).data();
    if (confirm('Are you sure want to delete this Guard License?')) {
        $.ajax({
            type: 'POST',
            url: '/Admin/GuardSettings?handler=DeleteGuardLicense',
            data: { 'id': data.id },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success)
                gridGuardLicensesAndLicenceKey.ajax.reload();
        })
    }
});
//To get the Compliance and License data stop
/*to enable for guard to update their documents - end*/

//For personal tab start
$('#LoteDrp').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
$('#btnAddpersonalDetails').on('click', function () {

    $('#addpersonalModal').modal('show');
});
$('#btn_save_Personal').on('click', function () {
    clearGuardValidationSummary('glValidationSummary');

    $.ajax({
        url: '/Admin/GuardSettings?handler=PersonalDetails',
        data: $('#frm_add_personal').serialize(),
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.status) {
            if (result.status) {
                alert("Saved Successfully");

            } else {
                //Failed
                $.notify(result.message,
                    {
                        align: "center",
                        verticalAlign: "top",
                        color: "#fff",
                        background: "#D44950",
                        blur: 0.4,
                        delay: 0
                    }
                );

            }
        } else {
            displayGuardValidationSummary('glValidationSummary', result.message);
        }
    });
});
//For personal tab stop