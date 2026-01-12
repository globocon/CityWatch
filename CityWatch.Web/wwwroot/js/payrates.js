
var gridPayRates;

$(document).ready(function () {
    // Initial check (in case page loads with Pay Rates selected, though unlikely with default)
    checkPayRatesVisibility();

    $('#hr_settings_fields_types').on('change', function () {
        checkPayRatesVisibility();
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

        if (description == '' || sellRate == '' || comms1 == '' || comms2 == '' || guardPayRate == '') {
            alert('All fields are required.');
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

function checkPayRatesVisibility() {
    var selectedValue = $('#hr_settings_fields_types').val();
    if (selectedValue == '9') { // 9 is PayRates enum value
        $('#PayRatesDiv').show();

        // Hide other elements that site.js might have shown
        $('#add_hr_settings').hide();
        $('#add_criticalDocuments').hide();
        $('#add_lote').hide();
        $('#add_location').hide();
        $('#SettingsDiv').hide();
        $('#TimesheetDiv').hide();
        $('#ClassroomLocationDiv').hide();

        // Hide other tables by their ID + wrapper
        $('#tbl_hr_settings').closest('.gj-grid-wrapper').hide();
        $('#tbl_license_type').closest('.gj-grid-wrapper').hide();
        $('#tbl_CriticalDocument').closest('.gj-grid-wrapper').hide();
        $('#tbl_language').closest('.gj-grid-wrapper').hide();
        $('#tbl_classroomLocation').closest('.gj-grid-wrapper').hide();
        $('#tbl_hr_settings_with_CourseLibrary').closest('.gj-grid-wrapper').hide();

        if (typeof gridPayRates === 'undefined') {
            initializePayRatesGrid();
        } else {
            gridPayRates.reload();
        }
    } else {
        $('#PayRatesDiv').hide();
    }
}

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
            { title: '', field: 'Action', width: 100, align: 'center', renderer: payRatesActionRenderer }
        ],
        pager: { limit: 10, sizes: [10, 20, 50, 100] }
    });
}

function payRatesActionRenderer(value, record) {
    return '<div class="text-center">' +
        '<button onclick="openEditPayRate(' + record.id + ', \'' + record.description + '\', ' + record.sellRateToClient + ', ' + record.comms1 + ', ' + record.comms2 + ', ' + record.guardPayRate + ')" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
        '<button onclick="deletePayRate(' + record.id + ')" class="btn btn-outline-danger"><i class="fa fa-trash"></i></button>' +
        '</div>';
}

function openEditPayRate(id, description, sellRate, comms1, comms2, guardPayRate) {
    $('#payRateId').val(id);
    $('#txtPayRateDescription').val(description);
    $('#txtSellRate').val(sellRate);
    $('#txtComms1').val(comms1);
    $('#txtComms2').val(comms2);
    $('#txtGuardPayRate').val(guardPayRate);
    $('#payRatesModal').modal('show');
    $('#modalTitlePayRate').text('Edit Pay Rate');
}

function deletePayRate(id) {
    if (confirm('Are you sure you want to delete this Pay Rate?')) { // Action symbol/confirmation
        $.ajax({
            url: '/Admin/Settings?handler=DeletePayRate',
            type: 'POST',
            data: { id: id },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (result) {
                if (result.success) {
                    gridPayRates.reload();
                } else {
                    alert(result.message);
                }
            },
            error: function () {
                alert('An error occurred');
            }
        });
    }
}
