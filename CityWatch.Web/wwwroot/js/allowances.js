
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
        $('#txtAllowanceAmount').val('');
        $('#modalTitleAllowance').text('Add Allowance');
        $('#allowanceModal').modal('show');
    });

    $('#btnSaveAllowance').on('click', function () {
        var id = $('#allowanceId').val();
        var description = $('#txtAllowanceDescription').val();
        var fq = $('#drpAllowanceFQ').val();
        var amount = $('#txtAllowanceAmount').val();
        var currency = $('#allowance_currency').val();

        if (!description) { alert('Please enter an allowance profile name.'); return; }
        if (!fq) { alert('Please select a frequency.'); return; }
        if (!amount) { alert('Please enter an amount.'); return; }

        var data = {
            id: id,
            description: description,
            fq: fq,
            amount: amount,
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
            { field: 'description', title: 'Allowance Profile', width: 300, sortable: true },
            { field: 'fq', title: 'FQ', width: 150, sortable: true },
            { field: 'amount', title: 'Amount', width: 120, sortable: true, renderer: function (value, record) {
                var symbol = '$';
                if (record.currency === 'GBP') symbol = '£';
                if (record.currency === 'EUR') symbol = '€';
                return symbol + parseFloat(value).toFixed(2);
            }},
            { field: 'currency', title: 'Currency', width: 100, sortable: true },
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

function editAllowance(record) {
    $('#allowanceId').val(record.id);
    $('#txtAllowanceDescription').val(record.description);
    $('#drpAllowanceFQ').val(record.fq);
    $('#txtAllowanceAmount').val(record.amount);
    // Note: Currency is usually managed at section level but we could sync it if needed
    // $('#allowance_currency').val(record.currency); 
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
