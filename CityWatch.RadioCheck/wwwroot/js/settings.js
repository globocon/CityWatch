window.onload = function () {


    
};
let gridRadioCheckStatusTypeSettings;
gridRadioCheckStatusTypeSettings = $('#radiocheck_status_type_settings').grid({
    dataSource: '/Admin/Settings?handler=RadioCheckStatusWithOutcome',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },
    columns: [
        { width: 130, field: 'referenceNo', title: 'Reference No',editor: true },
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
}
/*  broadcast live events-start*/
let isBroadCastLiveeventsAdding = false;
$('#add_live_events').on('click', function () {


    if (isBroadCastLiveeventsAdding == true) {
        alert('Unsaved changes in the grid. Refresh the page');
    } else {
        if ($('#BroadCastBannerLiveEvents tbody tr').find('td').eq(1).text() ==='') {
            isBroadCastLiveeventsAdding = true;
            gridBroadCastBannerLiveEvents.addRow({
                'id': -1
            }).edit(-1);
        }
        else {
            alert('Only one entry is allowed to be added in live events');

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
            { width: 950, field: 'textMessage', title: 'Text Message', editor: true },
            {
                width: 120, field: 'formattedExpiryDate', title: 'Expiry',
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
            alert('Unsaved changes in the grid. Refresh the page');
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
            { width: 700, field: 'textMessage', title: 'Text Message', editor: true },
            { width: 120, field: 'formattedStartDate', title: 'Start', type: 'date', format: 'dd-mmm-yyyy', editor: true },
            { width: 120, field: 'formattedExpiryDate', title: 'Expiry', type: 'date', format: 'dd-mmm-yyyy', editor: true },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
if (gridBroadCastBannerCalendarEvents) {
    gridBroadCastBannerCalendarEvents.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
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
            if (result.message == 'Another Event Exists') {


                $('#alert-wand-in-use-modal').modal('show');
            }
            gridBroadCastBannerCalendarEvents.clear();
            gridBroadCastBannerCalendarEvents.reload();
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
    /*  broadcast calendar events-end*/
