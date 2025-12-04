$('#ClientSiteId').on('change', function () {

    const selectedState = $(this).val(); // Get the selected state
    if (selectedState) {
        updateMapSite(selectedState);
    }
});
$('#ClientType').on('change', function () {
    $('#ClientSiteId').empty();
    $('#StateDrp').val('');
    const clientTypeId = $(this).val().join(';')
    const clientSiteControl = $('#ClientSiteId');
    clientSiteControl.html('');

    $.ajax({

        url: '/Admin/Settings?handler=ClientSitesNew&typeId=' + clientTypeId,
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            data.map(function (site) {
                clientSiteControl.append('<option value="' + site.text + '">' + site.text + '</option>');
            });
            clientSiteControl.multiselect('rebuild');


        }
    });


});

