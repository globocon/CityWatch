
var gridPayRates;


$(document).ready(function () {
    // Initial check (in case page loads with Pay Rates selected)
    initPayRatesGridIfSelected();

    $('#hr_settings_fields_types').on('change', function () {
        initPayRatesGridIfSelected();
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
                    $('#pay_rates_currency').val('AUD');
                } else {
                    alert(result.message);
                }
            },
            error: function () {
                alert('An error occurred');
            }
        });
    });


    $('#filesSearch').on('keyup', function () {
        var searchString = $(this).val();
        if (typeof gridPayRates !== 'undefined') {
            gridPayRates.reload({ page: 1, searchString: searchString });
        }
    });

    $('#btnDownloadPayRatesExcel').on('click', function (e) {
        e.preventDefault();
        var searchString = $('#filesSearch').val();
        window.location.href = '/Admin/Settings?handler=PayRatesExport&searchString=' + searchString;
    });
});

function initPayRatesGridIfSelected() {
    var selectedValue = $('#hr_settings_fields_types').val();
    if (selectedValue == '9') { // 9 is PayRates enum value

        if (typeof gridPayRates === 'undefined') {
            initializePayRatesGrid();
        } else {
            gridPayRates.reload();
        }
    }
}

function initializePayRatesGrid() {
    gridPayRates = $('#tbl_pay_rates').grid({
        dataSource: '/Admin/Settings?handler=PayRatesList',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'description', title: 'Description', width: 200, align: 'left' },
            { field: 'sellRateToClient', title: 'Sell Rate to Client', width: 100, align: 'center', renderer: currencyRenderer },
            { field: 'comms1', title: 'Comms 1', width: 100, align: 'center', renderer: currencyRenderer },
            { field: 'comms2', title: 'Comms 2', width: 100, align: 'center', renderer: currencyRenderer },
            { field: 'guardPayRate', title: 'Guard Pay Rate', width: 100, align: 'center', renderer: currencyRenderer },
            { field: 'currency', title: 'Currency', width: 80, align: 'center' },
            { title: '', field: 'Action', width: 100, align: 'center', renderer: payRatesActionRenderer }
        ],
        pager: { limit: 10, sizes: [10, 20, 50, 100] },
        initialized: function (e) {
            $('#tbl_pay_rates thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
}

function currencyRenderer(value, record) {
    if (value === null || value === undefined) return '$0.00';
    return '$' + parseFloat(value).toFixed(2);
}

function payRatesActionRenderer(value, record) {
    return '<div class="text-center">' +
        '<button onclick="openEditPayRate(' + record.id + ', \'' + record.description + '\', ' + record.sellRateToClient + ', ' + record.comms1 + ', ' + record.comms2 + ', ' + record.guardPayRate + ', \'' + (record.currency || '') + '\')" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
        '<button onclick="deletePayRate(' + record.id + ')" class="btn btn-outline-danger"><i class="fa fa-trash"></i></button>' +
        '</div>';
}

function openEditPayRate(id, description, sellRate, comms1, comms2, guardPayRate, currency) {
    $('#payRateId').val(id);
    $('#txtPayRateDescription').val(description);
    $('#txtSellRate').val(sellRate);
    $('#txtComms1').val(comms1);
    $('#txtComms2').val(comms2);
    $('#txtGuardPayRate').val(guardPayRate);
    if (currency) {
        $('#pay_rates_currency').val(currency);
    } else {
        $('#pay_rates_currency').val('AUD');
    }
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
