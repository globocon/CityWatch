$(function () {
    let gritdSmartWands;
    var clientSiteId = getUrlVars()["clientSiteId"];
    $("#gl_client_site_id").val(window.sharedVariable);
    
   
    gritdSmartWands = $('#cs-smart-wands').grid({
        dataSource: '/admin/settings?handler=SmartWandSettings&&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { width: 250, field: 'smartWandId', title: 'Smart Wand Id', editor: true },
            { width: 250, field: 'phoneNumber', title: 'Number', editor: true },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gritdSmartWands) {
        gritdSmartWands.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/admin/settings?handler=SmartWandSettings',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gritdSmartWands.reload({ clientSiteId: $('#gl_client_site_id').val() });
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isSmartWandAdding)
                    isSmartWandAdding = false;
            });
        });

        gritdSmartWands.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this smart wand details?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/admin/settings?handler=DeleteSmartWandSettings',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gritdSmartWands.reload({ clientSiteId: $('#gl_client_site_id').val() });
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isSmartWandAdding)
                        isSmartWandAdding = false;
                });
            }
        });
    }
    let isSmartWandAdding = false;
    $('#add_smart_wand').on('click', function () {

        if (isSmartWandAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isSmartWandAdding = true;
            gritdSmartWands.addRow({ 'id': -1, 'smartWandId': '', phoneNumber: '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
        }
    });


    $('#btnSaveGuardSiteSettings').on('click', function () {
        var isUpdateDailyLog = false;

        const token = $('input[name="__RequestVerificationToken"]').val();
        if ($('#enableLogDump').is(":checked")) {
            isUpdateDailyLog = true;
        }
        $.ajax({
            url: '/admin/settings?handler=SaveSiteEmail',
            type: 'POST',
            data: {
                siteId: $('#gl_client_site_id').val(),
                siteEmail: $('#gs_site_email').val(),
                enableLogDump: isUpdateDailyLog,
                landLine: $('#gs_land_line').val(),
                guardEmailTo: $('#gs_email_recipients').val(),
                duressEmail: $('#gs_duress_email').val(),
                duressSms: $('#gs_duress_sms').val()
            },
            headers: { 'RequestVerificationToken': token }
        }).done(function () {
            alert("Saved successfully");
        }).fail(function () {
            console.log("error");
        });
    });

    //gritdSmartWands.reload({ clientSiteId: $('#gl_client_site_id').val() });
    
    function getUrlVars() {
        var vars = [], hash;
        var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
        for (var i = 0; i < hashes.length; i++) {
            hash = hashes[i].split('=');
            vars.push(hash[0]);
            vars[hash[0]] = hash[1];
        }
        return vars;
    }

});
/*to view thw audit log report-end*/



