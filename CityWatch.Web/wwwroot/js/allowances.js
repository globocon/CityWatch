
var gridAllowances;

$(document).ready(function () {
    // Initial check (in case page loads with Allowances selected)
    initAllowancesGridIfSelected();

    $('#hr_settings_fields_types').on('change', function () {
        initAllowancesGridIfSelected();
    });

    $('#allowanceSearch').on('keyup', function () {
        if ($(this).val().length > 0) {
            $('#clearAllowanceSearch').show();
        } else {
            $('#clearAllowanceSearch').hide();
        }
        if (gridAllowances) {
            gridAllowances.reload({ searchString: $(this).val() });
        }
    });

    $('#clearAllowanceSearch').on('click', function () {
        $('#allowanceSearch').val('');
        $(this).hide();
        if (gridAllowances) {
            gridAllowances.reload({ searchString: '' });
        }
    });

    $('#add_allowance').on('click', function () {
        $('#allowanceId').val('0');
        $('#txtAllowanceDescription').val('');
        $('#drpAllowanceFQ').val('');
        $('#txtAllowanceSellRate').val('');
        $('#txtAllowanceComms1').val('');
        $('#txtAllowanceComms2').val('');
        $('#txtAllowanceGuardPayRate').val('');
        $('#modalTitleAllowance').text('Add Allowance');
        $('#allowanceModal').modal('show');
    });

    $('#btnSaveAllowance').on('click', function () {
        var id = $('#allowanceId').val();
        var description = $('#txtAllowanceDescription').val();
        var fq = $('#drpAllowanceFQ').val();
        var sellRate = $('#txtAllowanceSellRate').val();
        var comms1 = $('#txtAllowanceComms1').val();
        var comms2 = $('#txtAllowanceComms2').val();
        var guardPayRate = $('#txtAllowanceGuardPayRate').val();
        var currency = $('#allowance_currency').val();

        if (!description) { alert('Please enter an allowance profile name.'); return; }
        if (!fq) { alert('Please select a frequency.'); return; }
        // Rates can be 0 but usually should be provided
        if (sellRate === '' || comms1 === '' || comms2 === '' || guardPayRate === '') { 
            alert('Please enter values for all rate fields (Sell Rate, Comms, and Guard Pay).'); return; 
        }

        var data = {
            id: id,
            description: description,
            fq: fq,
            sellRateToClient: sellRate,
            comms1: comms1,
            comms2: comms2,
            guardPayRate: guardPayRate,
            currency: currency
        };

        $.ajax({
            url: '/Admin/Settings?handler=SaveAllowance',
            type: 'POST',
            data: { allowance: data },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    $('#allowanceModal').modal('hide');
                    gridAllowances.reload();
                } else {
                    alert(response.message);
                }
            }
        });
    });
});

function initAllowancesGridIfSelected() {
    var selectedValue = $('#hr_settings_fields_types').val();
    if (selectedValue == '10') { // 10 is Allowances enum value
        if (typeof gridAllowances === 'undefined') {
            initializeAllowancesGrid();
        } else {
            gridAllowances.reload();
        }
    }
}

function initializeAllowancesGrid() {
    gridAllowances = $('#tbl_allowances').grid({
        dataSource: { 
            url: '/Admin/Settings?handler=AllowancesList',
            data: { searchString: $('#allowanceSearch').val() }
        },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'description', title: 'Allowance Profile', width: 200, sortable: true },
            { field: 'fq', title: 'FQ', width: 100, sortable: true },
            { field: 'sellRateToClient', title: 'Sell Rate', width: 100, sortable: true, renderer: (v, r) => formatCurrency(v, r.currency) },
            { field: 'comms1', title: 'Comms 1', width: 100, sortable: true, renderer: (v, r) => formatCurrency(v, r.currency) },
            { field: 'comms2', title: 'Comms 2', width: 100, sortable: true, renderer: (v, r) => formatCurrency(v, r.currency) },
            { field: 'guardPayRate', title: 'Guard Pay', width: 100, sortable: true, renderer: (v, r) => formatCurrency(v, r.currency) },
            { field: 'currency', title: 'Currency', width: 80, sortable: true },
            {
                width: 100,
                title: '',
                tmpl: '<button class="btn btn-sm btn-outline-primary mr-1" title="Edit"><i class="fa fa-pencil"></i></button><button class="btn btn-sm btn-outline-danger" title="Delete"><i class="fa fa-trash"></i></button>',
                align: 'center',
                events: {
                    'click': function (e) {
                        var id = e.data.record.id;
                        if ($(e.target).hasClass('fa-pencil') || $(e.target).hasClass('btn-outline-primary')) {
                            editAllowance(e.data.record);
                        } else if ($(e.target).hasClass('fa-trash') || $(e.target).hasClass('btn-outline-danger')) {
                            deleteAllowance(id);
                        }
                    }
                }
            }
        ],
        pager: { limit: 10, sizes: [10, 20, 50] }
    });
}

function formatCurrency(value, currency) {
    var symbol = '$';
    if (currency === 'GBP') symbol = '£';
    if (currency === 'EUR') symbol = '€';
    return symbol + parseFloat(value || 0).toFixed(2);
}

function editAllowance(record) {
    $('#allowanceId').val(record.id);
    $('#txtAllowanceDescription').val(record.description);
    $('#drpAllowanceFQ').val(record.fq);
    $('#txtAllowanceSellRate').val(record.sellRateToClient);
    $('#txtAllowanceComms1').val(record.comms1);
    $('#txtAllowanceComms2').val(record.comms2);
    $('#txtAllowanceGuardPayRate').val(record.guardPayRate);
    $('#modalTitleAllowance').text('Edit Allowance');
    $('#allowanceModal').modal('show');
}

function deleteAllowance(id) {
    if (confirm('Are you sure you want to delete this allowance profile?')) {
        $.ajax({
            url: '/Admin/Settings?handler=DeleteAllowance',
            type: 'POST',
            data: { id: id },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    gridAllowances.reload();
                } else {
                    alert(response.message);
                }
            }
        });
    }
}
