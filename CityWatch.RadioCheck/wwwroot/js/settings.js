window.onload = function () {

    //To get the Duress Emails in pageload start
    $.ajax({
        url: '/Admin/Settings?handler=DuressEmail',
        type: 'GET',
        dataType: 'json',
    }).done(function (Emails) {
        $('#du_duress_email').val(Emails.emails);
    });

    $.ajax({
        url: '/Admin/Settings?handler=GlobalComplianceAlertEmail',
        type: 'GET',
        dataType: 'json',
    }).done(function (Emails) {
        $('#hr_compliance_email').val(Emails.emails);
    });

    //To get the Duress Emails in pageload stop

};


let gridRadioCheckStatusTypeSettings;
gridRadioCheckStatusTypeSettings = $('#radiocheck_status_type_settings').grid({
    dataSource: '/Admin/Settings?handler=RadioCheckStatusWithOutcome',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },
    columns: [
        { width: 130, field: 'referenceNo', title: 'Reference No', editor: true },
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
        if (isNaN(data.referenceNo)) {
            $.notify('Reference number should only contains numbers. !!!',
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#D44950",
                    blur: 0.4,
                    delay: 0
                }
            );
            gridRadioCheckStatusTypeSettings.edit(id);
            return;
        }
        //data.radioCheckStatusColorId = !Number.isInteger(data.radioCheckStatusColorId) ? data.radioCheckStatusColorId.getValue() : data.radioCheckStatusColorId;
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=RadioCheckStatus',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function (result) {
            //gridRadioCheckStatusTypeSettings.clear();
            //gridRadioCheckStatusTypeSettings.reload();
            if (result.status) {
                //Success
                $.notify(result.message,
                    {
                        align: "center",
                        verticalAlign: "top",
                        color: "#fff",
                        background: "#20D67B",
                        blur: 0.4,
                        delay: 0
                    }
                );
                gridRadioCheckStatusTypeSettings.clear();
                gridRadioCheckStatusTypeSettings.reload();
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
                gridRadioCheckStatusTypeSettings.edit(id);
            }
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
    let isRadionCheckStatusAdding = false;
    $('#add_radiocheck_status').on('click', function () {


        if (isRadionCheckStatusAdding == true) {
            $.notify("Unsaved changes in the grid. Please Refresh the page.",
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#A5881B",
                    blur: 0.4,
                    delay: 0
                }
            );
        } else {
            isRadionCheckStatusAdding = true;
            gridRadioCheckStatusTypeSettings.addRow({
                'id': -1
            }).edit(-1);
        }
    });
}
/*  broadcast live events-start*/
let isBroadCastLiveeventsAdding = false;
$('#add_live_events').on('click', function () {


    if (isBroadCastLiveeventsAdding == true) {
        $.notify("Unsaved changes in the grid. Please Refresh the page.",
            {
                align: "center",
                verticalAlign: "top",
                color: "#fff",
                background: "#A5881B",
                blur: 0.4,
                delay: 0
            }
        );
    } else {
        if ($('#BroadCastBannerLiveEvents tbody tr').find('td').eq(1).text() === '') {
            isBroadCastLiveeventsAdding = true;
            gridBroadCastBannerLiveEvents.addRow({
                'id': -1
            }).edit(-1);
        }
        else {
            $.notify("Only one entry is allowed to be added in live events.",
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
    }
});
let gridBroadCastBannerLiveEvents;
gridBroadCastBannerLiveEvents = $('#BroadCastBannerLiveEvents').grid({
    dataSource: '/Admin/Settings?handler=BroadcastLiveEvents',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },

    columns: [
        { width: 130, field: 'id', title: 'Id', hidden: true },
        { width: 900, field: 'textMessage', title: 'Text Message', editor: true },
        { width: 300, field: 'weblink', title: 'Weblink', editor: true },
        {
            width: 228, field: 'formattedExpiryDate', title: 'Expiry @23:59 hrs',
            type: 'date',
            format: 'dd-mmm-yyyy',
            editor: true,


        },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

if (gridBroadCastBannerLiveEvents) {
    gridBroadCastBannerLiveEvents.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        data.textMessage = data.textMessage.replace(/\s{2,}/g, ' ').trim();
        data.expiryDate = data.formattedExpiryDate;
        //data.radioCheckStatusColorId = !Number.isInteger(data.radioCheckStatusColorId) ? data.radioCheckStatusColorId.getValue() : data.radioCheckStatusColorId;
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=LiveEvents',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            gridBroadCastBannerLiveEvents.clear();
            gridBroadCastBannerLiveEvents.reload();
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isBroadCastLiveeventsAdding)
                isBroadCastLiveeventsAdding = false;
        });
    });

    gridBroadCastBannerLiveEvents.on('rowRemoving', function (e, id, record) {


        if (confirm('Are you sure want to delete this  live events?')) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteLiveEvents',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridBroadCastBannerLiveEvents.reload();
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isBroadCastLiveeventsAdding)
                    isBroadCastLiveeventsAdding = false;
            });
        }
    });
}
/*  broadcast live events-end*/

/*  broadcast calendar events-start*/
let isBroadCastCalendareventsAdding = false;
$('#add_calendar_events').on('click', function () {
    if (isBroadCastCalendareventsAdding == true) {
        $.notify("Unsaved changes in the grid. Please Refresh the page.",
            {
                align: "center",
                verticalAlign: "top",
                color: "#fff",
                background: "#A5881B",
                blur: 0.4,
                delay: 0
            }
        );
    } else {
        isBroadCastCalendareventsAdding = true;
        gridBroadCastBannerCalendarEvents.addRow({
            'id': -1
        }).edit(-1);
    }
});

let gridBroadCastBannerCalendarEvents;
gridBroadCastBannerCalendarEvents = $('#BroadCastBannerCalendarEvents').grid({
    dataSource: '/Admin/Settings?handler=BroadcastCalendarEvents',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },

    columns: [
        { width: 130, field: 'id', title: 'Id', hidden: true },
        { width: 100, field: 'referenceNo', title: 'Reference No', editor: true },
        { width: 600, field: 'textMessage', title: 'Text Message', editor: true },
        { width: 160, field: 'formattedStartDate', title: 'Start', type: 'date', format: 'dd-mmm-yyyy', editor: true },
        { width: 160, field: 'formattedExpiryDate', title: 'Expiry', type: 'date', format: 'dd-mmm-yyyy', editor: true },
        { width: 100, field: 'repeatYearly', title: 'Repeat', type: 'checkbox', align: 'center', editor: true },
        { width: 100, field: 'isPublicHoliday', title: 'PH', type: 'checkbox', align: 'center', editor: true },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

if (gridBroadCastBannerCalendarEvents) {
    gridBroadCastBannerCalendarEvents.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);

        if (isNaN(data.referenceNo)) {
            $.notify('Reference number should only contains numbers. !!!',
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#D44950",
                    blur: 0.4,
                    delay: 0
                }
            );
            gridBroadCastBannerCalendarEvents.edit(id);
            return;
        }
        if ((data.formattedStartDate == '') || (data.formattedExpiryDate == '') || (data.formattedStartDate == undefined) || (data.formattedExpiryDate == undefined)) {
            $.notify('Please check start date and expiry date. !!!',
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#D44950",
                    blur: 0.4,
                    delay: 0
                }
            );
            gridBroadCastBannerCalendarEvents.edit(id);
            return;
        }
        if ((data.textMessage == '') || (data.textMessage == undefined)) {
            $.notify('Event message cannot be empty. !!!',
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#D44950",
                    blur: 0.4,
                    delay: 0
                }
            );
            gridBroadCastBannerCalendarEvents.edit(id);
            return;
        }
        data.textMessage = data.textMessage.replace(/\s{2,}/g, ' ').trim();
        data.startDate = data.formattedStartDate;
        data.expiryDate = data.formattedExpiryDate;
        //data.radioCheckStatusColorId = !Number.isInteger(data.radioCheckStatusColorId) ? data.radioCheckStatusColorId.getValue() : data.radioCheckStatusColorId;
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=CalendarEvents',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function (result) {
            if (result.status) {
                //Success
                $.notify(result.message,
                    {
                        align: "center",
                        verticalAlign: "top",
                        color: "#fff",
                        background: "#20D67B",
                        blur: 0.4,
                        delay: 0
                    }
                );
                gridBroadCastBannerCalendarEvents.clear();
                gridBroadCastBannerCalendarEvents.reload();
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
                gridBroadCastBannerCalendarEvents.edit(id);
            }
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isBroadCastCalendareventsAdding)
                isBroadCastCalendareventsAdding = false;
        });
    });

    gridBroadCastBannerCalendarEvents.on('rowRemoving', function (e, id, record) {


        if (confirm('Are you sure want to delete this radio calendar events?')) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteCalendarEvents',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridBroadCastBannerCalendarEvents.reload();
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isBroadCastCalendareventsAdding)
                    isBroadCastCalendareventsAdding = false;
            });
        }
    });
}
$('#btn_confrim_wand_usok').on('click', function () {
    $('#alert-wand-in-use-modal').modal('hide')
})

// ################ Global SMS Start ####################
let gridGlobalDuressSmsSettings, editManager;;

editManager = function (value, record, $cell, $displayEl, id, $grid) {
    var data = $grid.data(),
        $delete = $('<button role="delete" class="gj-button-md"><i class="gj-icon delete text-danger"></i> Delete</button>').attr('data-key', id);
    $delete.on('click', function (e) {
        $grid.removeRow($(this).data('key'));
    });
    $displayEl.empty().append($delete);
}

gridGlobalDuressSmsSettings = $('#tbl_GlobalSmsNumbersList').grid({
    dataSource: '/Admin/Settings?handler=GetGlobalSmsNumberList',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command', managementColumn: false },
    columns: [
        { field: 'companyId', title: 'Company ID', hidden: true },
        { field: 'smsNumber', title: 'SMS Number', width: 350 },
        { width: 100, align: 'center', renderer: editManager }
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

if (gridGlobalDuressSmsSettings) {
    gridGlobalDuressSmsSettings.on('rowRemoving', function (e, id, record) {

        if (confirm('Are you sure to delete this sms number ?')) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteGlobalSmsNumber',
                data: { SmsNumberId: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (d) {
                if (d.status == true) {
                    //Success
                    $.notify(d.message,
                        {
                            align: "center",
                            verticalAlign: "top",
                            color: "#fff",
                            background: "#20D67B",
                            blur: 0.4,
                            delay: 0
                        }
                    );
                    gridGlobalDuressSmsSettings.reload();
                } else {
                    //Failed
                    $.notify(d.message,
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
            }).fail(function () {
                console.log('error');
            }).always(function () {

            });
        }
    });

    gridGlobalDuressSmsSettings.on('dataBound', function (e, records, totalRecords) {
        var SmsNumbers = "";
        records.forEach(function (item) {
            SmsNumbers += item.smsNumber + ", ";
        });
        if (SmsNumbers.endsWith(", ")) {
            SmsNumbers = SmsNumbers.substring(0, SmsNumbers.length - 2);
        }
        $('#du_duress_sms').val(SmsNumbers);
    });
}


$('#add_GlobalSms').on('click', function () {
    $('#sms_number').val('');
    $('#sms_country_code option:first-child').attr("selected", "selected");
    $('#sms_country_code')[0].selectedIndex = 0;
    $('#sms_local_code option:first-child').attr("selected", "selected");
    $('#sms_local_code')[0].selectedIndex = 0;
    gridGlobalDuressSmsSettings.reload();
    $('#smsnumber-modal').modal('show');
});

$('#btn_add_smsnumber').on('click', function () {
    const token = $('input[name="__RequestVerificationToken"]').val();
    const countrycode = $('#sms_country_code').val();
    const localcode = $('#sms_local_code').val();
    let sms_number = $('#sms_number').val();
    if (sms_number == '' || sms_number == null || sms_number.length != 9) {
        $.notify("Invalid sms number.",
            {
                align: "center",
                verticalAlign: "top",
                color: "#fff",
                background: "#D44950",
                blur: 0.4,
                delay: 0
            }
        );
        return;
    }
    sms_number = sms_number.substring(0, 3) + ' ' + sms_number.substring(3, 6) + ' ' + sms_number.substring(6, 9);
    var smsnumber = countrycode + ' ' + localcode + ' ' + sms_number;
    var adddata = {
        CompanyId: 1,
        SmsNumber: smsnumber
    };
    $.ajax({
        url: '/Admin/Settings?handler=AddGlobalSmsNumber',
        data: { SmsNumber: adddata },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (d) {
        if (d.status == true) {
            //Success
            $.notify(d.message,
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#20D67B",
                    blur: 0.4,
                    delay: 0
                }
            );
            gridGlobalDuressSmsSettings.reload();
        } else {
            //Failed
            $.notify(d.message,
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
    }).fail(function () {
        console.log('error');
    }).always(function () {

    });
});
// ################ Global SMS End ####################


$('#add_logbook').on('click', function () {

    $('#search_sites_settings').val('');
    $('#cs_client_type option:first-child').attr("selected", "selected");
    $('#cs_client_type')[0].selectedIndex = 0;
    gridClientSiteSettings.reload({ type: $('#cs_client_type').val(), searchTerm: $('#search_sites_settings').val() });
    $('#logbook-modal').modal('show');
});


//To save the Global Email Of Duress Button start
$('#add_GloblEmail').on('click', function () {
    const token = $('input[name="__RequestVerificationToken"]').val();
    var Email = $('#du_duress_email').val();
    var emailsArray = Email.split(',');
    var isValidEmailIds = true;
    for (var i = 0; i < emailsArray.length; i++) {
        var emailAddress = emailsArray[i].trim();
        if (isValidEmail(emailAddress)) {

        }
        else {
            isValidEmailIds = false;
            $.notify("Invalid email address.",
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

    }

    if (isValidEmailIds) {

        $.ajax({
            url: '/Admin/Settings?handler=SaveDuressEmail',
            data: { Email: Email },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            alert("The Duress Email Alert Email was saved successfully");
        })
    }



    function isValidEmail(email) {
        // Regular expression for basic email validation
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailPattern.test(email);
    }
})

$('#add_ComplianceEmail').on('click', function () {
    const token = $('input[name="__RequestVerificationToken"]').val();
    var Email = $('#hr_compliance_email').val();
    var emailsArray = Email.split(',');
    isValidEmailIds = true;
    for (var i = 0; i < emailsArray.length; i++) {
        var emailAddress = emailsArray[i].trim();
        if (isValidEmail(emailAddress)) {

        }
        else {
            isValidEmailIds = false;
            $.notify("Invalid email address." + emailAddress,
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



    }

    if (isValidEmailIds) {
        $.ajax({
            url: '/Admin/Settings?handler=SaveGlobalComplianceAlertEmail',
            data: { Email: Email },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            alert("The Compliance Alert Email was saved successfully");
        })

    }

    function isValidEmail(email) {
        // Regular expression for basic email validation
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailPattern.test(email);
    }
})
//To save the Global Email Of Duress Button stop
/***** Client Site KPI Settings *****/
let gridSiteDetailsforRcLogbook;
gridSiteDetailsforRcLogbook = $('#gridSiteDetailsforRcLogbook').grid({
    dataSource: '/Admin/Settings?handler=ClientSiteForRcLogBook',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    columns: [
        { width: 130, field: 'id', title: 'Id', hidden: true },
        { width: 150, field: 'clientTypeName', title: 'Client Type' },
        { width: 250, field: 'clientSiteName', title: 'Client Site' },




    ]

});



let gridClientSiteSettings;

function settingsButtonRenderer(value, record) {
    return '<button class="btn btn-outline-success mt-2 del-schedule d-block" data-sch-id="' + record.id + '""><i class="fa fa-check mr-2" aria-hidden="true"></i>Select Site</button>';
}

$('#kpi_client_site_settings').on('click', '.del-schedule', function () {
    const idToDelete = $(this).attr('data-sch-id');
    if (confirm('Are you sure want to update the site for logbook assignment ?')) {
        $.ajax({
            url: '/Admin/Settings?handler=SaveSiteIdForRcLogBook',
            type: 'POST',
            data: { id: idToDelete },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            gridSiteDetailsforRcLogbook.reload();
            $('#logbook-modal').modal('hide');
        });
    }

});

gridClientSiteSettings = $('#kpi_client_site_settings').grid({
    dataSource: '/Admin/Settings?handler=ClientSiteWithSettings',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    columns: [
        { width: 150, field: 'clientTypeName', title: 'Client Type' },
        { width: 250, field: 'clientSiteName', title: 'Client Site' },
        { width: 100, renderer: settingsButtonRenderer },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

/*code added for search client start */
$('#search_sites_settings').on('keyup', function (e) {
    var SearchTextbox = $("#search_sites_settings");
    var searchText = SearchTextbox.val();
    if (searchText.length >= 3) {
        gridClientSiteSettings.reload({ type: $('#cs_client_type').val(), searchTerm: $(this).val() });

    }

});
$('#btnSearchSites').on('click', function () {
    gridClientSiteSettings.reload({ type: $('#cs_client_type').val(), searchTerm: $('#search_sites_settings').val() });
});

$('#cs_client_type').on('change', function () {
    var SearchTextbox = $("#search_sites_settings");
    SearchTextbox.val("");
    var searchitem = '';
    gridClientSiteSettings.reload({ type: $(this).val(), searchTerm: searchitem });
});
/*code added for search client stop */


/*  broadcast calendar events-end*/


/*API Calls - start*/
/*SWChannels - start*/
let gridSWChannels;
let isAPICallsAdding = false;

gridSWChannels = $('#tbl_SWChannel').grid({
    dataSource: '/Admin/Settings?handler=SWChannels',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },

    columns: [
        { width: '0%', field: 'id', title: 'Id', hidden: true },
        { width: '70%', field: 'swChannel', title: 'SW Channel', editor: true },
        {
            width: '20%', field: 'keys', title: '<i class="fa fa-key" aria-hidden="true"></i>',
            editor: true,
        },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

if (gridSWChannels) {
    gridSWChannels.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        data.id = data.id.replace(/\s{2,}/g, ' ').trim();
        data.swChannel = data.swChannel;
        data.keys = data.keys;
        //data.radioCheckStatusColorId = !Number.isInteger(data.radioCheckStatusColorId) ? data.radioCheckStatusColorId.getValue() : data.radioCheckStatusColorId;
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=SWChannel',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            gridSWChannels.clear();
            gridSWChannels.reload();
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isAPICallsAdding)
                isAPICallsAdding = false;
        });
    });

    gridSWChannels.on('rowRemoving', function (e, id, record) {


        if (confirm('Are you sure want to delete this  smart wand channel?')) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteSWChannel',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridSWChannels.reload();
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isAPICallsAdding)
                    isAPICallsAdding = false;
            });
        }
    });
}
$('#add_sw_channel').on('click', function () {
    if (isAPICallsAdding == true) {
        $.notify("Unsaved changes in the grid. Please Refresh the page.",
            {
                align: "center",
                verticalAlign: "top",
                color: "#fff",
                background: "#A5881B",
                blur: 0.4,
                delay: 0
            }
        );
    } else {
        if ($('#tbl_SWChannel tbody tr').find('td').eq(1).text() === '') {
            isAPICallsAdding = true;
            gridSWChannels.addRow({
                'id': -1
            }).edit(-1);
        }
        else {
            $.notify("Only one entry is allowed to be added in Smart Wand Channels.",
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
    }
});
/*SWChannels - end*/

/* Sms Channel - Start */

let gridSmsChannels;
let isSmsAPICallsAdding = false;

gridSmsChannels = $('#tbl_SmsChannel').grid({
    dataSource: '/Admin/Settings?handler=SmsChannels',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },

    columns: [
        { width: '0%', field: 'id', title: 'Id', hidden: true },
        { width: '0%', field: 'companyId', title: 'Company Id', hidden: true },
        { width: '15%', field: 'smsProvider', title: 'SMS Provider Name', editor: true },
        { width: '15%', field: 'smsSender', title: 'Sender ID', editor: true },
        { width: '25%', field: 'apiKey', title: 'Api Key', editor: true, align: 'center' },
        { width: '25%', field: 'apiSecret', title: 'Api Password', editor: true, align: 'center' },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs text-success" aria-hidden="true"></i>');
    }
});

if (gridSmsChannels) {
    gridSmsChannels.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        data.id = data.id.replace(/\s{2,}/g, ' ').trim();
        data.companyId = data.companyId.replace(/\s{2,}/g, ' ').trim();
        data.smsProvider = data.smsProvider;
        data.apiKey = data.apiKey;
        data.apiSecret = data.apiSecret;
        data.smsSender = data.smsSender;
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=SmsChannel',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            gridSmsChannels.clear();
            gridSmsChannels.reload();
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isSmsAPICallsAdding)
                isSmsAPICallsAdding = false;
        });
    });

    gridSmsChannels.on('rowRemoving', function (e, id, record) {
        if (confirm('Are you sure want to delete this  SMS channel?')) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteSmsChannel',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridSmsChannels.reload();
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isSmsAPICallsAdding)
                    isSmsAPICallsAdding = false;
            });
        }
    });
}
$('#add_sms_channel').on('click', function () {
    if (isSmsAPICallsAdding == true) {
        $.notify("Unsaved changes in the grid. Please Refresh the page.",
            {
                align: "center",
                verticalAlign: "top",
                color: "#fff",
                background: "#A5881B",
                blur: 0.4,
                delay: 0
            }
        );
    } else {
        if ($('#tbl_SmsChannel tbody tr').find('td').eq(1).text() === '') {
            isSmsAPICallsAdding = true;
            gridSmsChannels.addRow({
                'id': -1,
                'companyId': 1
            }).edit(-1);
        }
        else {
            $.notify("Only one entry is allowed to be added in SMS Channel.",
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
    }
});
/* Sms Channel - End */

/*GeneralFeeds - start*/
let gridGeneralFeeds;
gridGeneralFeeds = $('#tbl_GeneralFeeds').grid({
    dataSource: '/Admin/Settings?handler=GeneralFeeds',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },

    columns: [
        { width: 130, field: 'id', title: 'Id', hidden: true },
        { width: 150, field: 'brand', title: 'Brand', editor: true },
        { width: 350, field: 'apiStrings', title: 'API String', editor: true },
        {
            width: 120, field: 'keys', title: '<i class="fa fa-key" aria-hidden="true"></i>',

            editor: true,


        },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

if (gridGeneralFeeds) {
    gridGeneralFeeds.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        data.id = data.id.replace(/\s{2,}/g, ' ').trim();
        data.brand = data.brand;
        data.apiStrings = data.apiStrings;
        data.keys = data.keys;
        //data.radioCheckStatusColorId = !Number.isInteger(data.radioCheckStatusColorId) ? data.radioCheckStatusColorId.getValue() : data.radioCheckStatusColorId;
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=GeneralFeeds',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            gridGeneralFeeds.clear();
            gridGeneralFeeds.reload();
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isAPICallsAdding)
                isAPICallsAdding = false;
        });
    });

    gridGeneralFeeds.on('rowRemoving', function (e, id, record) {


        if (confirm('Are you sure want to delete this  general feeds?')) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteGeneralFeeds',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridGeneralFeeds.reload();
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isAPICallsAdding)
                    isAPICallsAdding = false;
            });
        }
    });
}


$('#add_general_feeds').on('click', function () {
    if (isAPICallsAdding == true) {
        $.notify("Unsaved changes in the grid. Please Refresh the page.",
            {
                align: "center",
                verticalAlign: "top",
                color: "#fff",
                background: "#A5881B",
                blur: 0.4,
                delay: 0
            }
        );
    } else {
        isAPICallsAdding = true;
        gridGeneralFeeds.addRow({
            'id': -1
        }).edit(-1);
    }
});

/*linked duress start dileep 31052024*/
let gridSchedules;

gridSchedules = $('#rc_linked_duress').grid({
    dataSource: '/Admin/Settings?handler=RcLinkedDuress',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    columns: [
        { field: 'groupName', title: 'Group Name', width: 100 },
        { field: 'clientTypes', title: 'Client Types', width: 100 },
        { field: 'clientSites', title: 'Client Sites', width: 180 },
        { width: 75, renderer: schButtonRenderer },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

function schButtonRenderer(value, record) {
    let buttonHtml = '';
    buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-primary mr-2 mt-2 d-block" data-toggle="modal" data-target="#schedule-modal" data-sch-id="' + record.id + '" ';
    buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil mr-2"></i>Edit</button>';
    buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-danger mt-2 del-schedule d-block" data-sch-id="' + record.id + '""><i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
    return buttonHtml;
}



$('#clientTypeName').on('change', function () {
    const option = $(this).val();
    if (option === '') {
        $('#clientSites').html('');
        $('#clientSites').append('<option value="">Select</option>');
    }

    $.ajax({
        url: '/Admin/Settings?handler=ClientSites&type=' + encodeURIComponent(option),
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        $('#clientSites').html('');
        $('#clientSites').append('<option value="">Select</option>');
        data.map(function (site) {
            $('#clientSites').append('<option value="' + site.value + '">' + site.text + '</option>');
        });
    });
});

$('#clientSites').on('change', function () {
    const elem = $(this).find(":selected");
    if (elem.val() !== '') {
        const existing = $('#selectedSites option[value="' + elem.val() + '"]');
        if (existing.length === 0) {
            $('#selectedSites').append('<option value="' + elem.val() + '">' + elem.text() + '</option>');
            updateSelectedSitesCount();
        }
    }
});



$('#removeSelectedSites').on('click', function () {
    $('#selectedSites option:selected').remove();
    updateSelectedSitesCount();
});

function updateSelectedSitesCount() {
    $('#selectedSitesCount').text($('#selectedSites option').length);
}

$('#schedule-modal').on('shown.bs.modal', function (event) {
    clearScheduleModal();
    const button = $(event.relatedTarget);
    const isEdit = button.data('action') !== undefined && button.data('action') === 'editSchedule';
    if (isEdit) {
        schId = button.data('sch-id');
        linkedDuressModalOnEdit(schId);
    } 

  
});

function clearScheduleModal() {
    $('#scheduleId').val('0');
    $('#clientTypeName').val('');
    $('#clientSites').html('<option value="">Select</option>');
    $('#selectedSites').html('');
    updateSelectedSitesCount();
    $('input:hidden[name="clientSiteIds"]').remove();
    $('#clientTypeName option:eq(0)').attr('selected', true);
    $('#GroupName').val('');
    $('#sch-modal-validation').hide();
   
}


function linkedDuressModalOnEdit(scheduleId) {
    $('#loader').show();
    $.ajax({
        url: '/Admin/Settings?handler=RCLinkedDuressbyId&id=' + scheduleId,
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        $('#scheduleId').val(data.id);
        $.each(data.rcLinkedDuressClientSites, function (index, item) {
            $('#selectedSites').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
            updateSelectedSitesCount();
        });
        $('#GroupName').val(data.groupName);
        
    }).always(function () {
        $('#loader').hide();
    });
}

$('#rc_linked_duress').on('click', '.del-schedule', function () {
    const idToDelete = $(this).attr('data-sch-id');
    if (confirm('Are you sure want to delete this linked Duress?')) {
        $.ajax({
            url: '/Admin/Settings?handler=DeleteRCLinkedDuress',
            type: 'POST',
            data: { id: idToDelete },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            gridSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
        });
    }

});

$('#btnSavelinkedDuress').on('click', function () {
    $("input[name=clientSiteIds]").remove();
    var options = $('#selectedSites option');
    options.each(function () {
        const elem = '<input type="hidden" name="clientSiteIds" value="' + $(this).val() + '">';
        $('#frm_kpi_schedule').append(elem);
    });

    $.ajax({
        url: '/Admin/Settings?handler=SaveRCLinkedDuress',
        type: 'POST',
        data: $('#frm_kpi_schedule').serialize(),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        if (data.success) {
            alert('RC LinkedDuress saved successfully');
            $('#schedule-modal').modal('hide');
            gridSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
        } else {
            $('#sch-modal-validation').html('');
            data.message.split(',').map(function (item) { $('#sch-modal-validation').append('<li>' + item + '</li>') });
            $('#sch-modal-validation').show().delay(5000).fadeOut();
        }
    });
});

/*linked duress end*/
/*GeneralFeeds - end*/
/*API Calls -end*/

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
        url: '../RadioCheckV2?handler=DownLoadHelpPDF',
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
