$(function () {
    let gritdSmartWands;
    var clientSiteId = getUrlVars()["clientSiteId"];
    $("#gl_client_site_id").val(window.sharedVariable);
    $('#ClientSiteCustomField_ClientSiteId').val(window.sharedVariable);
   
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

    /*patrolcar settings-start*/
    let gridSitePatrolCars;
    gridSitePatrolCars = $('#cs-patrol-cars').grid({
        dataSource: '/Admin/Settings?handler=PatrolCar&&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { width: 250, field: 'model', title: 'Model', editor: true },
            { width: 250, field: 'rego', title: 'Rego', editor: true },
            { width: 250, field: 'id', title: 'Id', hidden: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridSitePatrolCars) {
        gridSitePatrolCars.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=PatrolCar',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridSitePatrolCars.reload({ clientSiteId: $('#gl_client_site_id').val() });
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isPatrolCarAdding)
                    isPatrolCarAdding = false;
            });
        });

        gridSitePatrolCars.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this patrol car details?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Admin/Settings?handler=DeletePatrolCar',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridSitePatrolCars.reload({ clientSiteId: $('#gl_client_site_id').val() });
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isPatrolCarAdding)
                        isPatrolCarAdding = false;
                });
            }
        });
    }
    let isPatrolCarAdding = false;
    $('#add_patrol_car').on('click', function () {

        if (isPatrolCarAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isPatrolCarAdding = true;
            gridSitePatrolCars.addRow({ 'id': -1, 'model': '', rego: '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
        }
    });
    /*patrolcar settings-end*/
    /*custom fields-start*/
    loadCustomFields();
    
    function loadCustomFields() {
        $.ajax({
            url: '/Admin/Settings?handler=CustomFields',
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            const ulFields = $('#ClientSiteCustomField_Name').siblings('ul.es-list');
            $('#ClientSiteCustomField_Name').val('');
            ulFields.html('');
            data.fieldNames.map(function (result) {
                ulFields.append('<li class="es-visible" value="' + result + '">' + result + '</li>');
            });

            const ulSlots = $('#ClientSiteCustomField_TimeSlot').siblings('ul.es-list');
            $('#ClientSiteCustomField_TimeSlot').val('');
            ulSlots.html('');
            data.slots.map(function (result) {
                ulSlots.append('<li class="es-visible" value="' + result + '">' + result + '</li>');
            });
        });
    }
    let gridSiteCustomFields;
    $('#btnSaveCustomFields').on('click', function () {
        $('#custom-field-validation ul').html('');
        $.ajax({
            url: '/Admin/Settings?handler=CustomFields',
            type: 'POST',
            DataType: 'json',
            data: $('#frm_custom_field').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (!result.status)
                displayCustomFieldsValidationSummary(result.message[0].split(','));
            else {
                loadCustomFields();
                gridSiteCustomFields.reload({ clientSiteId: $('#gl_client_site_id').val() });
            }
        }).fail(function () {
            console.log("error");
        });
    });
    function renderSiteCustomFieldsManagement(value, record, $cell, $displayEl) {
        let $deleteBtn = $('<button class="btn btn-outline-danger mr-2" data-id="' + record.id + '"><i class="fa fa-trash mr-2"></i>Delete</button>');
        let $editBtn = $('<button class="btn btn-outline-primary mr-2" data-id="' + record.id + '"><i class="fa fa-pencil mr-2"></i>Edit</button>');
        let $updateBtn = $('<button class="btn btn-outline-success mr-2" data-id="' + record.id + '"><i class="fa fa-check-circle mr-2"></i>Update</button>').hide();
        let $cancelBtn = $('<button class="btn btn-outline-primary mr-2" data-id="' + record.id + '"><i class="fa fa-times-circle mr-2"></i>Cancel</button>').hide();


        $deleteBtn.on('click', function (e) {
            gridSiteCustomFields.removeRow($(this).data('id'));
        });

        $editBtn.on('click', function (e) {
            gridSiteCustomFields.edit($(this).data('id'));
            $editBtn.hide();
            $deleteBtn.hide();
            $updateBtn.show();
            $cancelBtn.show();
        });

        $updateBtn.on('click', function (e) {
            gridSiteCustomFields.update($(this).data('id'));
            $editBtn.show();
            $deleteBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $cancelBtn.on('click', function (e) {
            gridSiteCustomFields.cancel($(this).data('id'));
            $editBtn.show();
            $deleteBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $displayEl.empty().append($editBtn)
            .append($deleteBtn)
            .append($updateBtn)
            .append($cancelBtn);
    }

    gridSiteCustomFields = $('#cs-custom-fields').grid({
        dataSource: '/Admin/Settings?handler=ClientSiteCustomFields&&clientSiteId=' + $('#gl_client_site_id').val(),
        data: { clientSiteId: $('#gl_client_site_id').val() },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command', managementColumn: false },
        columns: [
            { field: 'timeSlot', title: 'Time Slot', editor: true },
            { field: 'name', title: 'Field Name', editor: true },
            { renderer: renderSiteCustomFieldsManagement }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridSiteCustomFields) {
        gridSiteCustomFields.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=CustomFields',
                data: { clientSiteCustomField: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {
                if (result.status) gridSiteCustomFields.reload({ clientSiteId: $('#gl_client_site_id').val() });
                else alert(result.message);
            }).fail(function () {
                console.log('error');
            }).always(function () {

            });
        });

        gridSiteCustomFields.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this entry?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Admin/Settings?handler=DeleteClientSiteCustomField',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function (result) {
                    if (!result.success) alert(result.message);
                    else {
                        loadCustomFields();
                        gridSiteCustomFields.reload({ clientSiteId: $('#gl_client_site_id').val() });
                    }
                }).fail(function () {
                    console.log('error');
                });
            }
        });
    }
    
    $('#ClientSiteCustomField_Name').editableSelect({
        effects: 'slide'
    });

    $('#ClientSiteCustomField_TimeSlot').editableSelect({
        effects: 'slide'
    });
    function displayCustomFieldsValidationSummary(errors) {
        const summaryDiv = document.getElementById('custom-field-validation');
        summaryDiv.className = "validation-summary-errors";
        summaryDiv.querySelector('ul').innerHTML = '';
        errors.forEach(function (item) {
            const li = document.createElement('li');
            li.appendChild(document.createTextNode(item));
            summaryDiv.querySelector('ul').appendChild(li);
        });
    }
    /*custom fields-end*/

    /*site poc and locations-start*/
    let gridSitePocs;
    gridSitePocs = $('#cs-pocs').grid({
        dataSource: '/Admin/Settings?handler=SitePocs&&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { width: 120, field: 'name', title: 'Name', editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridSitePocs) {
        gridSitePocs.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            $.ajax({
                url: '/Admin/Settings?handler=SitePoc',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) gridSitePocs.reload({ clientSiteId: $('#gl_client_site_id').val() });
                else alert(result.message);
            }).fail(function () {
                alert('error');
            }).always(function () {
                if (isSitePocAdding)
                    isSitePocAdding = false;
            });
        });

        gridSitePocs.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this site POC details?')) {
                $.ajax({
                    url: '/Admin/Settings?handler=DeleteSitePoc',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.success) gridSitePocs.reload({ clientSiteId: $('#gl_client_site_id').val() });
                    else alert(result.message);
                }).fail(function () {
                    aler('error');
                }).always(function () {
                    if (isSitePocAdding)
                        isSitePocAdding = false;
                });
            }
        });
    }

    let isSitePocAdding = false;
    $('#add_site_poc').on('click', function () {

        if (isSitePocAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isSitePocAdding = true;
            gridSitePocs.addRow({ 'id': -1, 'name': '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
        }
    });

    let gridSiteLocations;
    gridSiteLocations = $('#cs-locations').grid({
        dataSource: '/Admin/Settings?handler=SiteLocations&&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { width: 120, field: 'name', title: 'Name', editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridSiteLocations) {
        gridSiteLocations.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            $.ajax({
                url: '/Admin/Settings?handler=SiteLocation',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) gridSiteLocations.reload({ clientSiteId: $('#gl_client_site_id').val() });
                else alert(result.message);
            }).fail(function () {
                alert('error');
            }).always(function () {
                if (isSiteLocationAdding)
                    isSiteLocationAdding = false;
            });
        });

        gridSiteLocations.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this site location details?')) {
                $.ajax({
                    url: '/Admin/Settings?handler=DeleteSiteLocation',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.success) gridSiteLocations.reload({ clientSiteId: $('#gl_client_site_id').val() });
                    else alert(result.message);
                }).fail(function () {
                    aler('error');
                }).always(function () {
                    if (isSiteLocationAdding)
                        isSiteLocationAdding = false;
                });
            }
        });
    }
    let isSiteLocationAdding = false;
    $('#add_site_location').on('click', function () {

        if (isSiteLocationAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isSiteLocationAdding = true;
            gridSiteLocations.addRow({ 'id': -1, 'name': '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
        }
    });
    /* site poc and locations - end*/
    /*key settings-start*/
    let gridClientSiteKeys = $('#cs_client_site_keys').DataTable({
        lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
        paging: true,
        ordering: true,
        order: [[1, "asc"]],
        info: false,
        searching: true,
        autoWidth: false,
        ajax: {
            url: '/Admin/Settings?handler=ClientSiteKeys',
            data: function (d) {
                d.clientSiteId = $('#ClientSiteKey_ClientSiteId').val();
            },
            dataSrc: ''
        },
        columns: [
            { data: 'id', visible: false },
            { data: 'keyNo', width: '4%' },
            { data: 'description', width: '12%', orderable: false },
            {
                targets: -1,
                orderable: false,
                width: '4%',
                data: null,
                defaultContent: '<button  class="btn btn-outline-primary mr-2" id="btn_edit_cs_key"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
                    '<button id="btn_delete_cs_key" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i>Delete</button>',
                className: "text-center"
            },
        ],
    });

    $('#cs_client_site_keys tbody').on('click', '#btn_delete_cs_key', function () {
        var data = gridClientSiteKeys.row($(this).parents('tr')).data();
        if (confirm('Are you sure want to delete this key?')) {
            $.ajax({
                type: 'POST',
                url: '/Admin/Settings?handler=DeleteClientSiteKey',
                data: { 'id': data.id },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridClientSiteKeys.ajax.reload();
            });
        }
    });

    $('#cs_client_site_keys tbody').on('click', '#btn_edit_cs_key', function () {
        var data = gridClientSiteKeys.row($(this).parents('tr')).data();
        loadClientSiteKeyModal(data);
    });
    $("#add_Downloadbtn").click(function () {

        var Key = $('#site-settings-for').html();

        var type = 'xlsx';
        var name = Key + '.';
        var data = document.getElementById('cs_client_site_keys');

        // Check if all columns are empty
        var isEmptyTable = true;
        var rows = data.getElementsByTagName('tr');
        for (var i = 0; i < rows.length; i++) {
            var cells = rows[i].getElementsByTagName('td');
            for (var j = 1; j < cells.length; j++) {
                if (cells[j].textContent.trim() !== '') {
                    isEmptyTable = false;
                    break;
                }
            }
        }

        if (isEmptyTable) {
            // Create a message row with the desired text
            var messageRow = document.createElement('tr');
            var messageCell = document.createElement('td');
            messageCell.innerText = 'No data available in table';
            messageRow.appendChild(messageCell);

            // Create a new table with the message
            var tableClone = document.createElement('table');
            var tbody = document.createElement('tbody');
            tbody.appendChild(messageRow);
            tableClone.appendChild(tbody);
        } else {
            // Clone the table and remove the last column
            var tableClone = data.cloneNode(true);
            var rows = tableClone.getElementsByTagName('tr');
            for (var i = 0; i < rows.length; i++) {
                var lastCell = rows[i].lastElementChild;
                if (lastCell) {
                    rows[i].removeChild(lastCell);
                }
            }
        }




        var excelFile = XLSX.utils.table_to_book(tableClone, { sheet: "Keys" });

        // Use XLSX.writeFile to generate and download the Excel file
        XLSX.writeFile(excelFile, name + type);
    });
    $('#add_client_site_key').on('click', function () {
        resetClientSiteKeyModal();
        $('#client-site-key-modal').removeClass('fade');
       
        $('#client-site-key-modal').modal('show');
        $('#client-site-key-modal').removeClass('modal-backdrop');
    });
    function loadClientSiteKeyModal(data) {
        $('#ClientSiteKey_Id').val(data.id);
        $('#ClientSiteKey_KeyNo').val(data.keyNo);
        $('#ClientSiteKey_Description').val(data.description);
        $('#csKeyValidationSummary').html('');
        $('#client-site-key-modal').modal('show');
    }

    function resetClientSiteKeyModal() {
        $('#ClientSiteKey_Id').val('');
        $('#ClientSiteKey_KeyNo').val('');
        $('#ClientSiteKey_Description').val('');
        $('#csKeyValidationSummary').html('');
       // $('#client-site-key-modal').modal('hide');
    }
    /*key settings - end*/
});

/*to view thw audit log report-end*/



