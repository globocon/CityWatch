
var gridPayRates;

$(document).ready(function () {
    $('#hr_settings_fields_types').on('change', function () {
        if ($(this).val() == '9') {
            $('#PayRatesDiv').show();
            if (typeof gridPayRates === 'undefined') {
                initializePayRatesGrid();
            } else {
                gridPayRates.reload();
            }
            
            // Ensure other grids are hidden (if site.js didn't catch them, though it should have)
            // But crucially, we must hide PayRatesDiv if val != 9
        } else {
            $('#PayRatesDiv').hide();
        }
    });

    $('#add_pay_rates').on('click', function () {
        $('#payRatesModal').modal('show');
        $('#payRateId').val(0);
        $('#txtPayRateDescription').val('');
        $('#txtSellRate').val('');
        $('#txtComms1').val('');
        $('#txtComms2').val('');
        $('#txtGuardPayRate').val('');
        $('#modalTitlePayRate').text('Add Pay Rate');
    });

    $('#btnSavePayRate').on('click', function () {
        var id = $('#payRateId').val();
        var description = $('#txtPayRateDescription').val();
        var sellRate = $('#txtSellRate').val();
        var comms1 = $('#txtComms1').val();
        var comms2 = $('#txtComms2').val();
        var guardPayRate = $('#txtGuardPayRate').val();
        var currency = $('#pay_rates_currency').val();

        if (description == '') {
            alert('Description is required');
            return;
        }

        $.ajax({
            url: '/Admin/Settings?handler=SavePayRate',
            type: 'POST',
            data: {
                Id: id,
                Description: description,
                SellRateToClient: sellRate,
                Comms1: comms1,
                Comms2: comms2,
                GuardPayRate: guardPayRate,
                Currency: currency
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (result) {
                if (result.success) {
                    $('#payRatesModal').modal('hide');
                    gridPayRates.reload();
                } else {
                    alert(result.message);
                }
            },
            error: function () {
                alert('An error occurred');
            }
        });
    });
});

function initializePayRatesGrid() {
    gridPayRates = $('#tbl_pay_rates').grid({
        dataSource: '/Admin/Settings?handler=PayRatesList',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'description', title: 'Description', width: 200 },
            { field: 'sellRateToClient', title: 'Sell Rate to Client', width: 100 },
            { field: 'comms1', title: 'Comms 1', width: 100 },
            { field: 'comms2', title: 'Comms 2', width: 100 },
            { field: 'guardPayRate', title: 'Guard Pay Rate', width: 100 },
            { title: '', field: 'Edit', width: 42, type: 'icon', icon: 'fa fa-pencil', tooltip: 'Edit', events: { 'click': editPayRate } },
            { title: '', field: 'Delete', width: 42, type: 'icon', icon: 'fa fa-trash', tooltip: 'Delete', events: { 'click': deletePayRate } }
        ],
         pager: { limit: 10, sizes: [10, 20, 50, 100] }
    });
}

function editPayRate(e) {
    var id = e.data.record.id;
    $('#payRateId').val(id);
    $('#txtPayRateDescription').val(e.data.record.description);
    $('#txtSellRate').val(e.data.record.sellRateToClient);
    $('#txtComms1').val(e.data.record.comms1);
    $('#txtComms2').val(e.data.record.comms2);
    $('#txtGuardPayRate').val(e.data.record.guardPayRate);
    $('#payRatesModal').modal('show');
    $('#modalTitlePayRate').text('Edit Pay Rate');
}

function deletePayRate(e) {
    if (confirm('Are you sure you want to delete this Pay Rate?')) {
        $.ajax({
            url: '/Admin/Settings?handler=DeletePayRate',
            type: 'POST',
            data: { id: e.data.record.id },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (result) {
                gridPayRates.reload();
            },
            error: function () {
                alert('An error occurred');
            }
        });
    }
}
