
var gridPayRates;
var gridPayRateGroups;

$(document).ready(function () {
    // Initial check (in case page loads with Pay Rates selected)
    initPayRatesGridIfSelected();

    $('#hr_settings_fields_types').on('change', function () {
        initPayRatesGridIfSelected();
    });

    loadGroupFilterDropdown(); // Add this line here for page load

    $('#drpPayRateGroupFilter').on('change', function () {
        if (gridPayRates) {
            gridPayRates.reload({ groupId: $(this).val() });
        }
    });

    $('#add_pay_rates').on('click', function () {
        loadPayRateGroupsDropdown(0);
    loadGroupFilterDropdown(); // Add this line
        $('#payRatesModal').modal('show');
        $('#payRateId').val(0);
        $('#txtPayRateDescription').val('');
        $('#txtSellRate').val('');
        $('#txtComms1').val('');
        $('#txtComms2').val('');
        $('#txtGuardPayRate').val('');
        $('#drpPayRateGroup').val('');

        $('#modalTitlePayRate').text('Add Pay Rate');
    });

    $('#btnManagePayRateGroups').on('click', function () {
        loadPayRateGroupsTable();
        $('#payRateGroupsModal').modal('show');
    });

    $('#btnAddPayRateGroup').on('click', function () {
        var name = $('#txtNewPayRateGroup').val();
        if (name == '') {
            alert('Group name is required.');
            return;
        }

        var id = $('#hdnEditPayRateGroupId').val();
        $.ajax({
            url: '/Admin/Settings?handler=SavePayRateGroup',
            type: 'POST',
            data: { Id: id, Name: name },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (result) {
                if (result.success) {
                    $('#txtNewPayRateGroup').val('');
                    $('#hdnEditPayRateGroupId').val(0);
                    $('#btnAddPayRateGroup').text('Add Group').removeClass('btn-primary').addClass('btn-success');
                    loadPayRateGroupsTable();
                    loadGroupFilterDropdown();
                } else {
                    alert(result.message);
                }
            },
            error: function () {
                alert('An error occurred');
            }
        });
    });

    $('#btnSavePayRate').on('click', function () {
        var id = $('#payRateId').val();
        var groupId = $('#drpPayRateGroup').val();
        var description = $('#txtPayRateDescription').val();
        var sellRate = $('#txtSellRate').val();
        var comms1 = $('#txtComms1').val();
        var comms2 = $('#txtComms2').val();
        var guardPayRate = $('#txtGuardPayRate').val();
        var currency = $('#pay_rates_currency').val();

        if (description == '' || sellRate == '' || comms1 == '' || comms2 == '' || guardPayRate == '' || !groupId || groupId <= 0) {
            alert('All fields are required, including Pay Rate Group.');
            return;
        }

        $.ajax({
            url: '/Admin/Settings?handler=SavePayRate',
            type: 'POST',
            data: {
                Id: id,
                PayRateGroupId: groupId,
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
        if (searchString.length > 0) {
            $('#clearSearch').show();
        } else {
            $('#clearSearch').hide();
        }
        if (typeof gridPayRates !== 'undefined') {
            gridPayRates.reload({ pageNo: 1, searchString: searchString });
        }
    });

    $('#clearSearch').on('click', function () {
        $('#filesSearch').val('');
        $('#clearSearch').hide();
        if (typeof gridPayRates !== 'undefined') {
            gridPayRates.reload({ pageNo: 1, searchString: '' });
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
        dataSource: { 
            url: '/Admin/Settings?handler=PayRatesList',
            data: { groupId: $('#drpPayRateGroupFilter').val() }
        },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'groupName', title: 'Group', width: 120, align: 'left', sortable: true },
            { field: 'description', title: 'Profile / Rate Description', width: 200, align: 'left', sortable: true },
            { field: 'sellRateToClient', title: 'Sell Rate', width: 100, align: 'center', renderer: currencyRenderer },
            { field: 'comms1', title: 'Comms 1', width: 100, align: 'center', renderer: currencyRenderer },
            { field: 'comms2', title: 'Comms 2', width: 100, align: 'center', renderer: currencyRenderer },
            { field: 'guardPayRate', title: 'Guard Pay', width: 100, align: 'center', renderer: currencyRenderer },
            { field: 'currency', title: 'Currency', width: 80, align: 'center' },
            { title: '', field: 'Action', width: 100, align: 'center', renderer: payRatesActionRenderer }
        ],
        paramNames: { page: 'pageNo' },
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
        '<button onclick="openEditPayRate(' + record.id + ', \'' + record.description + '\', ' + record.sellRateToClient + ', ' + record.comms1 + ', ' + record.comms2 + ', ' + record.guardPayRate + ', \'' + (record.currency || '') + '\', ' + (record.payRateGroupId || 0) + ')" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
        '<button onclick="deletePayRate(' + record.id + ')" class="btn btn-outline-danger"><i class="fa fa-trash"></i></button>' +
        '</div>';
}

function openEditPayRate(id, description, sellRate, comms1, comms2, guardPayRate, currency, groupId) {
    loadPayRateGroupsDropdown(groupId);
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
    if (confirm('Are you sure you want to delete this Pay Rate?')) {
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

function loadPayRateGroupsDropdown(selectedId) {
    console.log("Loading Pay Rate Groups...");
    $.ajax({
        url: '/Admin/Settings?handler=PayRateGroupsList',
        type: 'GET',
        success: function (data) {
            console.log("Groups loaded:", data);
            var items = '<option value="">-- Select Group --</option>';
            $.each(data, function (i, item) {
                items += "<option value='" + item.id + "'>" + item.name + "</option>";
            });
            $('#drpPayRateGroup').html(items);
            if (selectedId > 0) {
                $('#drpPayRateGroup').val(selectedId);
            }
        },
        error: function (xhr, status, error) {
            console.error("Failed to load groups:", status, error);
            alert("Failed to load Pay Rate Groups. Check console for details.");
        }
    });
}

function loadPayRateGroupsTable() {
    $.ajax({
        url: '/Admin/Settings?handler=PayRateGroupsList',
        type: 'GET',
        success: function (data) {
            var tbody = $('#tblPayRateGroups tbody');
            tbody.empty();
            $.each(data, function (i, item) {
                var sites = "";
                if (item.assignedSites && item.assignedSites.length > 0) {
                    sites = item.assignedSites.map(s => s.name).join(", ");
                }

                var row = '<tr>' +
                    '<td>' + item.name + '</td>' +
                    '<td style="font-size: small; color: #555;">' + sites + '</td>' +
                    '<td class="text-center">' +
                    '<button onclick="openPayRateGroupAssignment(' + item.id + ')" class="btn btn-sm btn-outline-info mr-2" title="Assign Sites"><i class="fa fa-link"></i></button>' +
                    '<button onclick="editPayRateGroup(' + item.id + ', \'' + item.name + '\')" class="btn btn-sm btn-outline-primary mr-2" title="Edit Group"><i class="fa fa-pencil"></i></button>' +
                    '<button onclick="deletePayRateGroup(' + item.id + ')" class="btn btn-sm btn-outline-danger" title="Delete Group"><i class="fa fa-trash"></i></button>' +
                    '</td>' +
                    '</tr>';
                tbody.append(row);
            });
        }
    });
}

/** Pay Rate Group Site Assignment Logic **/
let prgSiteTree;

function openPayRateGroupAssignment(id) {
    $('#payrate-group-assignment-for-id').val(id);
    if (prgSiteTree === undefined) {
        prgSiteTree = $('#prgSiteTreeView').tree({
            uiLibrary: 'bootstrap4',
            checkboxes: true,
            primaryKey: 'id',
            dataSource: '/Admin/Settings?handler=PayRateGroupAssignments',
            autoLoad: false,
            textField: 'name',
            childrenField: 'clientSites',
            checkedField: 'checked',
            dataBound: function () {
                // Initial highlighting of loaded checked nodes
                const checkedIds = prgSiteTree.getCheckedNodes().map(id => String(id));
                $('#prgSiteTreeView [data-role="node"]').each(function () {
                    var $node = $(this);
                    var nodeId = String($node.attr('data-id'));
                    if (nodeId && checkedIds.indexOf(nodeId) > -1) {
                        $node.find('> [data-role="wrapper"]').addClass('node-selected');
                    }
                });
            },
            checkboxChange: function (e, $node, record, state) {
                if (state === 'checked') {
                    $node.find('> [data-role="wrapper"]').addClass('node-selected');
                } else {
                    $node.find('> [data-role="wrapper"]').removeClass('node-selected');
                }
            }
        });
    }
    prgSiteTree.uncheckAll();
    prgSiteTree.reload({ groupId: id });
    $('#payrate-group-site-assignment-modal').modal('show');
}

$(document).on('click', '#btnSavePrgSiteAssignment', function () {
    const groupId = $('#payrate-group-assignment-for-id').val();
    if (prgSiteTree) {
        let selectedSites = prgSiteTree.getCheckedNodes().filter(function (item) {
            // Only include numeric IDs (Site IDs), filter out parent node labels like "Security" or "Uncategorized"
            return item && !isNaN(parseInt(item));
        });

        if (!groupId || groupId <= 0) {
            alert('Error: No valid group selected for assignment.');
            return;
        }

        $.ajax({
            url: '/Admin/Settings?handler=SavePayRateGroupAssignments',
            data: {
                groupId: groupId,
                selectedSites: selectedSites
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (res) {
            if (res.success) {
                loadPayRateGroupsTable();
                alert('Assignments saved successfully.');
            } else {
                alert('Error: ' + res.message);
            }
        }).fail(function (xhr) {
            let errorMessage = 'Failed to save assignments.';
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMessage = xhr.responseJSON.message;
            } else if (xhr.responseText) {
                errorMessage = xhr.responseText;
            }
            alert('Error: ' + errorMessage);
        });
    }
});

$(document).on('click', '#expandAllPrgAccess', function () {
    if (prgSiteTree) prgSiteTree.expandAll();
});

$(document).on('click', '#collapseAllPrgAccess', function () {
    if (prgSiteTree) prgSiteTree.collapseAll();
});

function deletePayRateGroup(id) {
    if (confirm('Are you sure you want to delete this group?')) {
        $.ajax({
            url: '/Admin/Settings?handler=DeletePayRateGroup',
            type: 'POST',
            data: { id: id },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (result) {
                if (result.success) {
                    loadPayRateGroupsTable();
                }
            },
            error: function () {
                alert('An error occurred');
            }
        });
    }
}

function editPayRateGroup(id, name) {
    $('#hdnEditPayRateGroupId').val(id);
    $('#txtNewPayRateGroup').val(name);
    $('#btnAddPayRateGroup').text('Update Group').removeClass('btn-success').addClass('btn-primary');
    $('#txtNewPayRateGroup').focus();
}

function loadGroupFilterDropdown() {
    $.ajax({
        url: '/Admin/Settings?handler=PayRateGroupsList',
        type: 'GET',
        success: function (data) {
            var items = '<option value="">All Groups (Filter)</option>';
            $.each(data, function (i, item) {
                items += "<option value='" + item.id + "'>" + item.name + "</option>";
            });
            $('#drpPayRateGroupFilter').html(items);
        }
    });
}
