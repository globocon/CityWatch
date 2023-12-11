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
    /*  broadcast live events-start*/
    let gridBroadCastBannerLiveEvents;
    gridBroadCastBannerLiveEvents = $('#BroadCastBannerLiveEvents').grid({
        dataSource: '/Admin/Settings?handler=BroadcastLiveEvents',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },

        columns: [
            { width: 130, field: 'id', title: 'Id', hidden: true },
            { width: 500, field: 'textMessage', title: 'Name', editor: true },
            { width: 100, field: 'expiryDate', title: 'ExpiryDate', type: 'date', format: 'expiryDate', editor: true  },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    /*  broadcast live events-end*/
}