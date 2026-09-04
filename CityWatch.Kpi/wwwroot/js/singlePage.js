//p2-140 key photos  -start
var FileuploadFileChanged = null;
//p2-140 key photos  -end

$(document).ready(function () {

});

$(function () {
    let gritdSmartWands;
    let gridSiteDropboxSettings;

    let smartWandPatrolCarGroupListForDDL = [];
    $('#smartwandPatrolCarGroupsDDL option').each(function () {
        var ddlvalue = Number($(this).val());// $(this).val();
        var ddltext = $(this).text();

        if (ddlvalue) {
            smartWandPatrolCarGroupListForDDL.push({ value: ddlvalue, text: ddltext });
        }
    });
        
    var clientSiteId = getUrlVars()["clientSiteId"];
    $("#gl_client_site_id").val(window.sharedVariable);
    $("#ClientSiteKey_ClientSiteId").val(window.sharedVariable);
    $("#ANPR_ClientSiteId").val(window.sharedVariable);
    $('#ClientSiteCustomField_ClientSiteId').val(window.sharedVariable);

    gritdSmartWands = $('#cs-smart-wands').grid({
        dataSource: '/admin/settings?handler=SmartWandSettings&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { width: 100, field: 'smartWandId', title: 'Smart Wand ID', editor: true },
            { width: 190, field: 'phoneNumber', title: 'Number', editor: true },
            { width: 110, field: 'simProvider', title: 'SIM Provider', editor: true },
            { width: 170, field: 'imei', title: 'IMEI', editor: true },
            {
                title: 'Registered Device',
                width: 200,
                align: 'left',
                editor: false,
                tmpl: '<span class="action-placeholder"></span>'
            },
            {
                width: 150,
                field: 'patrolCarName',
                title: 'Patrol Car',
                type: 'dropdown',
                editor: { dataSource: smartWandPatrolCarGroupListForDDL, valueField: 'value' },
                editField: 'patrolCarId'
            }
        ],
        rowDataBound: function (e, $row, id, record) {
            const $cell = $row.find('.action-placeholder');
            const html = renderderegisterDevice(record);
            $cell.html(html);
        },

        initialized: function (e) {
            //$(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
            const $grid = $(e.target);
            const $lastTh = $grid.find('thead tr th:last');

            // Add icon
            $lastTh.html('<i class="fa fa-cogs" aria-hidden="true"></i>');

            // Set fixed width (for header)
            $lastTh.css('width', '120px');

            // Also set width for body cells
            $grid.find('tbody tr').each(function () {
                $(this).find('td:last').css('width', '120px');
            });
        }
    });

    if (gritdSmartWands) {
        gritdSmartWands.on('rowDataChanged', function (e, id, record) {

            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
           

            $.ajax({
                url: '/admin/settings?handler=SmartWandPhoneNumber',
                data: { phoneNumber: data.phoneNumber, Id: data.id },
                type: 'GET'
            }).done(function (response) {
                if (response != null) {
                    alert('Number already in use at site ' + response.clientSite.name + ' please deregister this number before trying to allocate it to a different site');
                }
                else {
                    // FIX: Convert -1 back to null
                    var pid = (data.patrolCarId ?? '').toString().toLowerCase();
                    if (pid === '-1') { data.patrolCarId = null; }

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
                }
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

    function renderderegisterDevice(value, record) {
        let buttonHtml = '';
        if (record.deviceId != null && record.deviceId != '') {
            buttonHtml = `<span><strong>Device Name:</strong> ${record.deviceName}</br><strong>Device Type:</strong> ${record.deviceType}</span>`;
            buttonHtml += '</br><button type="button" style="display:inline-block!important;" class="btn btn-outline-primary m-1 de-registerDevice d-block" data-sw-id="' + record.id + '""><i class="fa fa-unlink mr-1" aria-hidden="true"></i>De-Register</button>';
        }
        return buttonHtml;
    }

    function renderderegisterDevice(record) {
        let buttonHtml = '';
        if (record.deviceId != null && record.deviceId != '') {
            buttonHtml = `<span><strong>Device Name:</strong> ${record.deviceName}</br><strong>Device Type:</strong> ${record.deviceType}</span>`;
            buttonHtml += '</br><button type="button" style="display:inline-block!important;" class="btn btn-outline-primary m-1 de-registerDevice d-block" data-sw-id="' + record.id + '""><i class="fa fa-unlink mr-1" aria-hidden="true"></i>De-Register</button>';
        }
        return buttonHtml;
    }


    $('#cs-smart-wands').on('click', '.de-registerDevice', function () {
        const idToDelete = $(this).attr('data-sw-id');
        if (confirm('Are you sure want to de-register this device?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeRegisterDevice',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (response) {
                if (response.status) {
                    //gritdSmartWands.reload({ clientSiteId: $('#gl_client_site_id').val() });
                    gritdSmartWands.reload();
                    alert('Success: ' + response.message);
                } else {
                    alert('Error: ' + response.message);
                }
            });
        }

    });


    let isSmartWandAdding = false;
    $('#add_smart_wand').on('click', function () {

        if (isSmartWandAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isSmartWandAdding = true;
            gritdSmartWands.addRow({ 'id': -1, 'smartWandId': '', phoneNumber: '', patrolCarId: '-1', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
        }
    });

    $('#IsDosDontList').on('change', function () {
        const isChecked = $(this).is(':checked');

        const filter = isChecked ? 1 : 2;
        if (filter == 1) {

            $('#IsDosDontListEnabledHidden').val(true)

        }
        if (filter == 2) {
            $('#IsDosDontListEnabledHidden').val(false)

        }

    });
    $('#btnSaveGuardSiteSettings').on('click', function () {
        //var isUpdateLBDailyLog = false;
        //var isUpdateKVDailyLog = false;
        //var isUpdateSWDailyLog = false;
        //var isUpdateDailyLogFusion = false;
        var test = $('#IsDosDontListEnabledHidden').val();
        const token = $('input[name="__RequestVerificationToken"]').val();
        //if ($('#enableLBLogDump').is(":checked")) {
        //    isUpdateLBDailyLog = true;
        //}
        //if ($('#enableKVLogDump').is(":checked")) {
        //    isUpdateKVDailyLog = true;
        //}
        //if ($('#enableSWLogDump').is(":checked")) {
        //    isUpdateSWDailyLog = true;
        //}
        //if ($('#enableFusionDump').is(":checked")) {
        //    isUpdateDailyLogFusion = true;
        //}
        $.ajax({
            url: '/admin/settings?handler=SaveSiteEmail',
            type: 'POST',
            data: {
                siteId: $('#gl_client_site_id').val(),
                siteEmail: $('#gs_site_email').val(),
                //enableLBLogDump: isUpdateLBDailyLog,
                //enableKVLogDump: isUpdateKVDailyLog,
                //enableSWLogDump: isUpdateSWDailyLog,
                //uploadFusionLog: isUpdateDailyLogFusion,
                landLine: $('#gs_land_line').val(),
                //guardEmailTo: $('#gs_email_recipients').val(),
                duressEmail: $('#gs_duress_email').val(),
                duressSms: $('#gs_duress_sms').val(),
                IsDosDontList: $('#IsDosDontListEnabledHidden').val(),
            },
            headers: { 'RequestVerificationToken': token }
        }).done(function () {
            alert("Saved successfully");
        }).fail(function () {
            console.log("error");
        });
    });
    $('#btnSaveGuardSiteScheduleSettings').on('click', function () {
        var isUpdateLBDailyLog = false;
        var isUpdateKVDailyLog = false;
        var isUpdateSWDailyLog = false;
        var isUpdateDailyLogFusion = false;

        var isUpdateLBWeeklyLog = false;
        var isUpdateKVWeeklyLog = false;
        var isUpdateSWWeeklyLog = false;
        var isUpdateWeeklyLogFusion = false;

        var isUpdateLBMonthlyLog = false;
        var isUpdateKVMonthlyLog = false;
        var isUpdateSWMonthlyLog = false;
        var isUpdateMonthlyLogFusion = false;


        const token = $('input[name="__RequestVerificationToken"]').val();
        if ($('#enableLBLogDump').is(":checked")) {
            isUpdateLBDailyLog = true;
        }
        if ($('#enableKVLogDump').is(":checked")) {
            isUpdateKVDailyLog = true;
        }
        if ($('#enableSWLogDump').is(":checked")) {
            isUpdateSWDailyLog = true;
        }
        if ($('#enableFusionDump').is(":checked")) {
            isUpdateDailyLogFusion = true;
        }


        if ($('#enableLBWeeklyLogDump').is(":checked")) {
            isUpdateLBWeeklyLog = true;
        }
        if ($('#enableKVWeeklyLogDump').is(":checked")) {
            isUpdateKVWeeklyLog = true;
        }
        if ($('#enableSWWeeklyLogDump').is(":checked")) {
            isUpdateSWWeeklyLog = true;
        }
        if ($('#enableFusionWeeklyDump').is(":checked")) {
            isUpdateWeeklyLogFusion = true;
        }

        if ($('#enableLBMonthlyLogDump').is(":checked")) {
            isUpdateLBMonthlyLog = true;
        }
        if ($('#enableKVMonthlyLogDump').is(":checked")) {
            isUpdateKVMonthlyLog = true;
        }
        if ($('#enableSWMonthlyLogDump').is(":checked")) {
            isUpdateSWMonthlyLog = true;
        }
        if ($('#enableFusionMonthlyDump').is(":checked")) {
            isUpdateMonthlyLogFusion = true;
        }
        $.ajax({
            url: '/admin/settings?handler=SaveSiteEmailBasedOnLogs',
            type: 'POST',
            data: {
                siteId: $('#gl_client_site_id').val(),
                enableLBLogDump: isUpdateLBDailyLog,
                enableKVLogDump: isUpdateKVDailyLog,
                enableSWLogDump: isUpdateSWDailyLog,
                uploadFusionLog: isUpdateDailyLogFusion,
                guardEmailTo: $('#gs_email_recipients').val(),
                duressEmail: $('#gs_duress_email').val(),

                enableLBWeeklyLogDump: isUpdateLBWeeklyLog,
                enableKVWeeklyLogDump: isUpdateKVWeeklyLog,
                enableSWWeeklyLogDump: isUpdateSWWeeklyLog,
                uploadFusionWeeklyLog: isUpdateWeeklyLogFusion,
                guardEmailWeeklyLogTo: $('#gs_email_weely_recipients').val(),

                enableLBMonthlyLogDump: isUpdateLBMonthlyLog,
                enableKVMonthlyLogDump: isUpdateKVMonthlyLog,
                enableSWMonthlyLogDump: isUpdateSWMonthlyLog,
                uploadFusionMonthlyLog: isUpdateMonthlyLogFusion,
                guardEmailMonthlyLogTo: $('#gs_email_monthly_recipients').val(),
                
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

    let gridSummaryImage;
    gridSummaryImage = $('#tbl_summaryImage1').grid({

        //dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=4',
        dataSource: {
            url: '/Admin/Settings?handler=StaffDocsUsingTypeNew&&type=6&&ClientSiteId=' + $('#ClientSiteId').val(),

        },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [


            { field: 'fileName', title: 'File Name', width: 240 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 93 },
            { width: 160, renderer: schButtonRenderer },
        ],
        dataBound: function (e, records) {
            if (!records || records.length === 0) {
                // If no records, render the static HTML
                const staticHtml = `
            <thead>
                <tr>
                    <th style="width:370px">File Name</th>
                    <th style="width:359px">Date & Time Uploaded</th>
                    <th class="text-center"><i class="fa fa-cogs" aria-hidden="true"></i></th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td class="align-middle" id="summary_imageRC"></td>
                    <td class="align-middle" id="summary_image_updatedRC"></td>
                    <td class="text-center">
                    <input type="hidden" id="DocumentID"/>
                       <a href="" class="btn btn-outline-primary m-1" id="download_summary_imageRCList" target="_blank"><i class="fa fa-download"></i>Download</a>
                      <label class="btn btn-success mb-0"><form id="form_file_downloads_companyNew" method="post"><i class="fa fa-upload mr-2"></i>Replace <input type="file" id="upload_summary_imageRcList1" accept=".jpg, .jpeg, .png, .bmp, .pdf,.docx" hidden></form></label>
                       <button type="button" style="display:inline-block!important;" class="btn btn-outline-danger m-1 d-block" id="delete_summary_image1"><i class="fa fa-trash" aria-hidden="true"></i>Delete</button>
                    </td>
                </tr>
            </tbody>`;
                $('#tbl_summaryImage1').html(staticHtml);
            }
        },
        initialized: function (e) {

            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });


    function schButtonRenderer(value, record) {
        let buttonHtml = '';
        buttonHtml += '<a href="' + record.filePath + record.fileName + '" class="btn btn-outline-primary m-1" target="_blank"><i class="fa fa-download"></i>Download</a>';
        buttonHtml += '<label class="btn btn-success mb-0"><form id="form_file_downloads_companyNew" method="post"><i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_staff_file_companyNew" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
            '</form></label>'
        //buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-primary m-1 d-block" data-toggle="modal" data-target="#schedule-modal" data-sch-id="' + record.id + '" ';
        //buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil"></i></button>';
        buttonHtml += '<button type="button" style="display:inline-block!important;" class="btn btn-outline-danger m-1 del-scheduleAlarm1 d-block" data-sch-id="' + record.id + '""><i class="fa fa-trash" aria-hidden="true"></i>Delete</button>';
        return buttonHtml;
    }
    $('#tbl_summaryImage1').on('click', '.del-scheduleAlarm1', function () {
        const idToDelete = $(this).attr('data-sch-id');
        if (confirm('Are you sure want to delete this file?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteStaffDoc',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridSummaryImage.reload();
            });
        }

    });
    $('#tbl_summaryImage1').on('change', 'input[name="upload_staff_file_companyNew"]', function () {
        uploadStafDocUsingType1($(this), true, 1);
    });


    function uploadStafDocUsingType1(uploadCtrl, edit = false, type) {

        const ClientSiteID = $('#ClientSiteId').val();
        const file = uploadCtrl.get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        if (!fileExtn || '.pdf,.docx,.xlsx'.indexOf(fileExtn.toLowerCase()) < 0) {
            showModal('Unsupported file type. Please upload a .pdf, .docx or .xlsx file');
            return false;
        }

        const fileForm = new FormData();
        fileForm.append('file', file);
        fileForm.append('type', type);
        fileForm.append('ClientSiteID', ClientSiteID);
        if (edit)
            fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));

        $.ajax({
            url: '/Admin/Settings?handler=UploadStaffDocUsingType',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success) {

                gridSummaryImage.reload();
                //showStatusNotification(data.success, data.message);
            }
        }).fail(function () {
            //showStatusNotification(false, 'Something went wrong');
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
            { width: 120, field: 'name', title: 'Name', editor: true },
            { width: 120, field: 'email', title: 'Email', editor: true }
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
    if ($.fn.DataTable.isDataTable('#cs_client_site_keys')) {
        $('#cs_client_site_keys').DataTable().destroy();
    }
    let gridClientSiteKeys = $('#cs_client_site_keys').DataTable({
        info: true, // Enable the info display
        lengthMenu: [[10, 25, 50, 100, 1000, - 1], [10, 25, 50, 100, 1000, "Show All"]],
        paging: true,
        ordering: true,
        order: [[1, "asc"]],
        info: false,
        searching: true,
        autoWidth: false,
        "bDestroy": true,
        ajax: {
            url: '/Admin/Settings?handler=ClientSiteKeys',
            data: function (d) {
                d.clientSiteId = $('#gl_client_site_id').val();
            },
            dataSrc: ''
        },
        columns: [
            { data: 'id', visible: false },
            { data: 'keyNo', width: '4%' },
            { data: 'description', width: '12%', orderable: false },
            //{ data: 'imagePathNew', width: '4%', orderable: false },
            //p2-140 key photos  -start
            {
                width: '4%', orderable: false, data: 'imagePathNew',
                render: function (value, type, data) {

                    return '<a  href="' + data.imagePath + '"target="_blank" >' + value + '</a>';

                }
            },
            //p2-140 key photos-end
            {
                targets: -1,
                orderable: false,
                width: '5%',
                data: null,
                defaultContent: '<button  class="btn btn-outline-primary mr-2" id="btn_edit_cs_key"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
                    '<button id="btn_delete_cs_key" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i>Delete</button>',


                className: "text-center"
            },
        ],

    });
    //p2-140 key photos  -start
    let gridANPR = $('#cs_ANPR').DataTable({
        lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
        paging: false,
        ordering: true,
        order: [[1, "asc"]],
        info: false,
        searching: false,
        autoWidth: false,
        "bDestroy": true,
        ajax: {
            url: '/Admin/Settings?handler=ANPR',
            data: function (d) {
                d.clientSiteId = $('#gl_client_site_id').val();
            },
            dataSrc: ''
        },
        columns: [
            { data: 'id', visible: false },
            { data: 'profile', width: '4%' },
            { data: 'apicalls', width: '12%', orderable: false },
            { data: 'laneLabel', width: '12%', orderable: false },
            //p2-140 key photos-end
            {
                targets: -1,
                orderable: false,
                width: '4%',
                data: null,
                defaultContent: '<button  class="btn btn-outline-primary mr-2" id="btn_edit_anpr"><i class="fa fa-pencil mr-2"></i></button>' +
                    '<button id="btn_delete_anpr_key" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i></button>',


                className: "text-center"
            },
        ],

    });

    $('#cs_ANPR tbody').on('click', '#btn_edit_anpr', function () {
        var data = gridANPR.row($(this).parents('tr')).data();
        loadANPRModal(data);
    });
    $('#cs_ANPR tbody').on('click', '#btn_delete_anpr_key', function () {
        var data = gridANPR.row($(this).parents('tr')).data();
        if (confirm('Are you sure want to delete this key?')) {
            $.ajax({
                type: 'POST',
                url: '/Admin/Settings?handler=DeleteANPR',
                data: { 'id': data.id },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridANPR.ajax.reload();
            });
        }
    });
    function loadANPRModal(data) {
        console.log(data);
        //$('#AnprKey_Disabled').prop('checked', false);
        //$('#AnprKey_SingleLane').prop('checked', false);
        //$('#AnprKey_SeperateEntryAndExit').prop('checked', false);
        $('#ANPR_Id').val(data.id);
        $('#AnprKey_Profile').val(data.profile);
        $('#AnprKey_ApiCalls').val(data.apicalls);
        $('#AnprKey_LineLabel').val(data.laneLabel);
        $('#AnprKey_Disabled').prop('checked', !!data.isDisabled);  // Ensure it's a boolean
        $('#AnprKey_SingleLane').prop('checked', !!data.isSingleLane);  // Ensure it's a boolean
        $('#AnprKey_SeperateEntryAndExit').prop('checked', !!data.isSeperateEntryAndExitLane);
        $('#csANPRValidationSummary').html('');
        $('#anpr-modal').modal('show');

    }
    $('#btn_save_anpr_key').on('click', function () {
        var formData = $('#frm_add_key1').serializeArray(); // Serialize to array for easier manipulation

        // Filter out the default "on" checkbox values from the serialized array
        formData = formData.filter(function (item) {
            return item.name !== 'ANPR.IsDisabled' && item.name !== 'ANPR.IsSingleLane' && item.name !== 'ANPR.IsSeperateEntryAndExitLane';
        });

        // Manually append the correct checkbox values (true/false)
        formData.push({ name: 'ANPR.IsDisabled', value: $('#AnprKey_Disabled').is(':checked') ? 'true' : 'false' });
        formData.push({ name: 'ANPR.IsSingleLane', value: $('#AnprKey_SingleLane').is(':checked') ? 'true' : 'false' });
        formData.push({ name: 'ANPR.IsSeperateEntryAndExitLane', value: $('#AnprKey_SeperateEntryAndExit').is(':checked') ? 'true' : 'false' });

        // Convert form data array to URL-encoded string
        var formDataString = $.param(formData);


        //console.log(formDataString);
        $.ajax({
            url: '/Admin/Settings?handler=ANPR',
            data: formDataString,
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) {
                $('#anpr-modal').modal('hide');
                gridANPR.ajax.reload();
            } else {
                displayANPRValidationSummary(result.message);
            }
        });
    });
    $("#KeyImagefileUpload").fileUpload();

    $('#upload_KeyImage_file').on('change', function () {
        const file = $(this).get(0).files; //.item(0); 
        FileuploadFileChanged(file);
    });


    FileuploadFileChanged = function (allfile) {
        const file = allfile.item(0); // allfile.get(0).files.item(0);
        const fileExtn = "." + file.name.split('.').pop().toLowerCase();
        console.log('fileExtn: ' + fileExtn);
        if (!fileExtn || allowedfiletypes.includes(fileExtn) == false) {
            alert('Please select a valid file type');
            return false;
        }
        const formData = new FormData();
        var Desc = $('#ClientSiteKey_KeyNo').val();
        formData.append("file", file);
        formData.append('keyNo', $('#ClientSiteKey_KeyNo').val());
        formData.append('clientSiteId', $('#ClientSiteKey_ClientSiteId').val());
        formData.append('url', window.location.origin);
        if (Desc == '') {

            (confirm('Please enter the key no'))
        }
        else {
            fileprocess(allfile);

            $.ajax({
                type: 'POST',
                url: '/Admin/Settings?handler=UploadKeyFileAttachmentAttachment',
                data: formData,
                cache: false,
                contentType: false,
                processData: false,
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (response) {
                if (response.success) {
                    $('#ClientSiteKey_ImagePath').val(response.imagePath);
                    $('#keyImage_fileName1').val(response.imagePathNew);

                    loadKeyImagePopup(response);

                }
            }).fail(function () {
            }).always(function () {
                $('#upload_KeyImage_file').val('');
            });
        }

    }
    function loadKeyImagePopup(response) {
        $("#keyimage-attachment-list").empty();

        var attachIndex = 0;
        const file = response.imagePath;
        const attachment_id = response.id;
        const li = document.createElement('li');
        li.id = attachment_id;
        li.className = 'list-group-item';
        li.dataset.index = attachIndex;
        let liText = document.createTextNode(response.imagePathNew);
        const icon = document.createElement("i");
        icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-keyImage-attachment';
        icon.title = 'Delete';
        icon.style = 'cursor:pointer';
        li.appendChild(liText);
        li.appendChild(icon);
        const anchorTag = document.createElement("a");
        anchorTag.href = file;
        anchorTag.target = "_blank";
        const icon2 = document.createElement("i");
        icon2.className = 'fa fa-download ml-2 text-primary';
        icon2.title = 'Download';
        icon2.style = 'cursor:pointer';
        anchorTag.appendChild(icon2);
        li.appendChild(anchorTag);
        document.getElementById('keyimage-attachment-list').append(li);
        //for (var attachIndex = 0; attachIndex < response.length; attachIndex++) {
        //    const file = response[attachIndex].filePath;
        //    const attachment_id = 1;
        //    const li = document.createElement('li');
        //    li.id = attachment_id;
        //    li.className = 'list-group-item';
        //    li.dataset.index = attachIndex;
        //    let liText = document.createTextNode(response[attachIndex].fileName);
        //    const icon = document.createElement("i");
        //    icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-keyImage-attachment';
        //    icon.title = 'Delete';
        //    icon.style = 'cursor:pointer';
        //    li.appendChild(liText);
        //    li.appendChild(icon);
        //    const anchorTag = document.createElement("a");
        //    anchorTag.href = file;
        //    anchorTag.target = "_blank";
        //    const icon2 = document.createElement("i");
        //    icon2.className = 'fa fa-download ml-2 text-primary';
        //    icon2.title = 'Download';
        //    icon2.style = 'cursor:pointer';
        //    anchorTag.appendChild(icon2);
        //    li.appendChild(anchorTag);
        //    document.getElementById('keyimage-attachment-list').append(li);

        //}
    }

    $('#keyimage-attachment-list').on('click', '.btn-delete-keyImage-attachment', function (event) {
        if (confirm('Are you sure want to remove this attachment?')) {
            var target = event.target;
            const fileName = target.parentNode.innerText.trim();
            const id = target.parentNode.id;
            $.ajax({
                url: '/Admin/Settings?handler=DeleteKeyImageAttachment',
                type: 'POST',
                dataType: 'json',
                data: {
                    clientsiteid: $('#ClientSiteKey_ClientSiteId').val(),
                    name: fileName,
                    id: id,

                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result) {
                    target.parentNode.parentNode.removeChild(target.parentNode);
                    $("#keyimage-attachment-list").empty();
                    //gridClientSiteKeys.clear();
                    gridClientSiteKeys.ajax.reload();
                    //loadKeyImagePopup(result)
                }
            });
        }
    });
    //p2-140 key photos  -end
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

        var Key = $('#client_site_name').html();
        $('#loader').show(); // Show loader
        var guardIds = [];
        // Use the DataTable API to get the instance of the table
        var table = $('#cs_client_site_keys').DataTable();

        // Collect filtered row data
        var rawData = [];
        table.rows({ filter: 'applied' }).every(function () {
            var rowData = this.data(); // Get the data for each filtered row

            // Assuming rowData has properties like id, keyNo, description, imagePathNew
            if (rowData) {
                guardIds.push(rowData.id.toString()); // Collect GuardIds
                rawData.push({
                    keyNo: rowData.keyNo || '',
                    description: rowData.description || '',
                    imagePathNew: rowData.imagePathNew || ''
                });
            }
        });

        try {
            // Define headers and column widths
            const headers = ['Key #', 'Description', 'Image'];
            const columnWidths = [20, 50, 20]; // Example widths

            // Prepare data rows
            const dataRows = [headers, ...rawData.map(item => [
                item.keyNo,
                item.description,
                item.imagePathNew,
            ])];

            // Create a new worksheet and add data
            const ws = XLSX.utils.aoa_to_sheet(dataRows);

            // Set column widths
            ws['!cols'] = columnWidths.map(width => ({ wch: width }));

            // Create a new workbook and append the worksheet
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'Keys');

            // Write the file
            const fileName = 'Keys for ' + Key + '.xlsx';
            XLSX.writeFile(wb, fileName);

            $('#loader').hide(); // Hide loader after successful export
        } catch (error) {
            $('#loader').hide(); // Hide loader in case of error
            console.error('Error fetching or processing data:', error); // Log error
            alert("An error occurred while exporting data.");
        }
    });
    $('#add_client_site_key').on('click', function () {
        resetClientSiteKeyModal();
        $('#client-site-key-modal-new').modal('show');
        //p2-140 key photos  -start
        $("#keyimage-attachment-list").empty();
        //p2-140 key photos  -end
        // $('#client-site-key-modal-new').appendTo("body").modal('show');
    });

    $('#btnkeyclose').on('click', function () {
        $('#client-site-key-modal-new').modal('hide');
    });

    $('#client-site-key-modal-new').on('hidden.bs.modal', function () {
        $('body').addClass('modal-open'); // Add the modal-open class to the body to prevent scrolling
        $('#kpi-settings-modal').focus(); // Refocus on the second modal
    });


    function loadClientSiteKeyModal(data) {
        $('#ClientSiteKey_Id').val(data.id);
        $('#ClientSiteKey_KeyNo').val(data.keyNo);
        $('#ClientSiteKey_Description').val(data.description);
        $('#csKeyValidationSummary').html('');
        $('#client-site-key-modal-new').modal('show');
        //p2-140 key photos  -start
        $("#keyimage-attachment-list").empty();

        if (data.imagePath != '' && data.imagePath != null) {
            loadKeyImagePopup(data);
        }

        //p2-140 key photos  -end
    }

    function resetClientSiteKeyModal() {
        $('#ClientSiteKey_Id').val('');
        $('#ClientSiteKey_KeyNo').val('');
        $('#ClientSiteKey_Description').val('');
        $('#csKeyValidationSummary').html('');
        $('#client-site-key-modal-new').modal('hide');
    }
    $('#btn_save_cs_key').on('click', function () {
        $.ajax({
            url: '/Admin/Settings?handler=ClientSiteKey',
            data: $('#frm_add_key').serialize(),
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) {
                $('#client-site-key-modal-new').modal('hide');
                gridClientSiteKeys.ajax.reload();
            } else {
                displaySiteKeyValidationSummary(result.message);
            }
        });
    });
    function displaySiteKeyValidationSummary(errors) {
        $('#csKeyValidationSummary').removeClass('validation-summary-valid').addClass('validation-summary-errors');
        $('#csKeyValidationSummary').html('');
        $('#csKeyValidationSummary').append('<ul></ul>');
        if (!Array.isArray(errors)) {
            $('#csKeyValidationSummary ul').append('<li>' + errors + '</li>');
        } else {
            errors.forEach(function (item) {
                if (item.indexOf(',') > 0) {
                    item.split(',').forEach(function (itemInner) {
                        $('#csKeyValidationSummary ul').append('<li>' + itemInner + '</li>');
                    });
                } else {
                    $('#csKeyValidationSummary ul').append('<li>' + item + '</li>');
                }
            });
        }
    }

    /*key settings - end*/
    /*toggle settings-start*/
    /*for manifest options-start*/
    GetClientSiteToggle();
    /*for manifest options - end*/
    //for time slot - start 
    $('#chk_cs_time_slot').on('change', function () {


        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_tn_no_load').prop('checked', false);
        }
        else {
            $('#chk_cs_tn_no_load').prop('checked', true);
        }
        $('#chk_cs_Is_Time_Slot').val(isChecked);
    });
    $('#chk_cs_tn_no_load').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_time_slot').prop('checked', false);
        }
        else {
            $('#chk_cs_time_slot').prop('checked', true);
        }
        $('#chk_cs_Is_Time_Slot').val(isChecked);
    });
    //for time slot - end
    //for VWI  - start 
    $('#chk_cs_vwi').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_Manifest').prop('checked', false);
        }
        else {
            $('#chk_cs_Manifest').prop('checked', true);
        }
        $('#chk_cs_Is_VWI').val(isChecked);
    });
    $('#chk_cs_Manifest').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_vwi').prop('checked', false);
        }
        else {
            $('#chk_cs_vwi').prop('checked', true);
        }
        $('#chk_cs_Is_VWI').val(isChecked);
    });
    //for VWI areas - start 
    //for sender  - start 
    $('#chk_cs_Sender').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_Receiver').prop('checked', false);
        }
        else {
            $('#chk_cs_Receiver').prop('checked', true);
        }
        $('#chk_cs_Is_Sender').val(isChecked);
    });
    $('#chk_cs_Receiver').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_Sender').prop('checked', false);
        }
        else {
            $('#chk_cs_Sender').prop('checked', true);
        }
        $('#chk_cs_Is_Sender').val(isChecked);
    });
    //for sender - end
    //for Reels  - start 
    $('#chk_cs_Reels').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_QTY').prop('checked', false);
        }
        else {
            $('#chk_cs_QTY').prop('checked', true);
        }
        $('#chk_cs_Is_Reels').val(isChecked);
    });
    $('#chk_cs_QTY').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_Reels').prop('checked', false);
        }
        else {
            $('#chk_cs_Reels').prop('checked', true);
        }
        $('#chk_cs_Is_Reels').val(isChecked);
    });

    $('#chk_cs_ISO').on('change', function () {
        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_VIN').prop('checked', false);
            $('#chk_cs_TrailerRego').prop('checked', false);
            $('#chk_cs_CarsStock').prop('checked', false);
        }
        $('#chk_cs_ISO').val(isChecked);
        $('#modalIsISO').val(isChecked);
    });

    $('#chk_cs_VIN').on('change', function () {
        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_ISO').prop('checked', false);
            $('#chk_cs_TrailerRego').prop('checked', false);
            $('#chk_cs_CarsStock').prop('checked', false);
        }
        $('#chk_cs_VIN').val(isChecked);
        $('#modalIsVin').val(isChecked);
    });

    $('#chk_cs_TrailerRego').on('change', function () {
        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_VIN').prop('checked', false);
            $('#chk_cs_ISO').prop('checked', false);
            $('#chk_cs_CarsStock').prop('checked', false);        
        }
        $('#chk_cs_TrailerRego').val(isChecked);
        $('#modalIsTrailerRego').val(isChecked);
    });

    $('#chk_cs_CarsStock').on('change', function () {
        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_VIN').prop('checked', false);
            $('#chk_cs_ISO').prop('checked', false);
            $('#chk_cs_TrailerRego').prop('checked', false);
        }
        $('#chk_cs_CarsStock').val(isChecked);
        $('#modalIsCarsStock').val(isChecked);
    });
    //for Reels - start
    $('#btnSaveToggleKeys').on('click', function () {
        var toggleType;
        var IsActive;

        if ($('#chk_cs_time_slot').is(":checked")) {
            $('#chk_cs_Is_Time_Slot').val(true);

        }
        else {
            $('#chk_cs_Is_Time_Slot').val(false);

        }
        if ($('#chk_cs_vwi').is(":checked")) {
            $('#chk_cs_Is_VWI').val(true);

        }
        else {
            $('#chk_cs_Is_VWI').val(false);

        }
        if ($('#chk_cs_Sender').is(":checked")) {
            $('#chk_cs_Is_Sender').val(true);

        }
        else {
            $('#chk_cs_Is_Sender').val(false);

        }
        if ($('#chk_cs_Reels').is(":checked")) {
            $('#chk_cs_Is_Reels').val(true);

        }
        else {
            $('#chk_cs_Is_Reels').val(false);

        }
                
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=SaveToggleType',
            type: 'POST',
            data: {
                siteId: $('#gl_client_site_id').val(),
                timeslottoggleTypeId: 1,
                timeslotIsActive: $('#chk_cs_Is_Time_Slot').val(),
                vwitoggleTypeId: 2,
                vwiIsActive: $('#chk_cs_Is_VWI').val(),
                sendertoggleTypeId: 3,
                senderIsActive: $('#chk_cs_Is_Sender').val(),
                reelstoggleTypeId: 4,
                reelsIsActive: $('#chk_cs_Is_Reels').val(),
                trailerRegoTypeId: 5,
                isISO: $('#chk_cs_ISO').is(":checked"),
                isVIN: $('#chk_cs_VIN').is(":checked"),
                isTrailerRego: $('#chk_cs_TrailerRego').is(":checked"),
                isCarsStock: $('#chk_cs_CarsStock').is(":checked"),
            },
            headers: { 'RequestVerificationToken': token }
        }).done(function () {

            alert("Saved Successfully");
        }).fail(function () {
            console.log("error");
        });
    });

    function GetClientSiteToggle() {
        const token = $('input[name="__RequestVerificationToken"]').val();

        $('#chk_cs_ISO').prop('checked', false);
        $('#chk_cs_VIN').prop('checked', false);
        $('#chk_cs_TrailerRego').prop('checked', false);
        $('#chk_cs_CarsStock').prop('checked', false);


        $.ajax({
            url: '/Admin/Settings?handler=ClientSiteToggle',
            type: 'GET',
            data: {
                siteId: $('#gl_client_site_id').val()
            },
            headers: { 'RequestVerificationToken': token }
        }).done(function (response) {
            for (var i = 0; i < response.length; i++) {

                if (response[i].toggleTypeId == 1) {
                    $('#chk_cs_Is_Time_Slot').val(response[i].isActive);
                    if (response[i].isActive == true) {
                        $('#chk_cs_time_slot').prop('checked', true);
                        $('#chk_cs_tn_no_load').prop('checked', false);
                    }
                    else {
                        $('#chk_cs_time_slot').prop('checked', false);
                        $('#chk_cs_tn_no_load').prop('checked', true);
                    }

                }
                if (response[i].toggleTypeId == 2) {
                    $('#chk_cs_Is_VWI').val(response[i].isActive);
                    if (response[i].isActive == true) {
                        $('#chk_cs_vwi').prop('checked', true);
                        $('#chk_cs_Manifest').prop('checked', false);
                    }
                    else {
                        $('#chk_cs_vwi').prop('checked', false);
                        $('#chk_cs_Manifest').prop('checked', true);
                    }

                }
                if (response[i].toggleTypeId == 3) {
                    $('#chk_cs_Is_Sender').val(response[i].isActive);
                    if (response[i].isActive == true) {
                        $('#chk_cs_Sender').prop('checked', true);
                        $('#chk_cs_Receiver').prop('checked', false);
                    }
                    else {
                        $('#chk_cs_Sender').prop('checked', false);
                        $('#chk_cs_Receiver').prop('checked', true);
                    }

                }
                if (response[i].toggleTypeId == 4) {
                    $('#chk_cs_Is_Reels').val(response[i].isActive);
                    if (response[i].isActive == true) {
                        $('#chk_cs_Reels').prop('checked', true);
                        $('#chk_cs_QTY').prop('checked', false);
                    }
                    else {
                        $('#chk_cs_Reels').prop('checked', false);
                        $('#chk_cs_QTY').prop('checked', true);
                    }

                }

                if (response[i].toggleTypeId == 5) {
                    $('#chk_cs_ISO').prop('checked', response[i].isISO);
                    $('#chk_cs_VIN').prop('checked', response[i].isVin);
                    $('#chk_cs_TrailerRego').prop('checked', response[i].isTrailerRego);
                    $('#chk_cs_CarsStock').prop('checked', response[i].isCarsStock);
                }
            }

        }).fail(function () {
            console.log("error");
        });
    }
    //Ring Fence Settings - Start
    $('#btnDisableDataCollection').on('click', function () {
        $.ajax({
            url: '/Admin/Settings?handler=UpdateSiteDataCollection',
            type: 'POST',
            data: {
                clientSiteId: $('#gl_client_site_id').val(),
                disabled: $('#cbxDisableDataCollection').is(":checked")
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function () {
            alert("Saved successfully");
        });
    });
    //Ring Fence Settings - End

    GetClientSites();
    function GetClientSites() {

        var chek = $('#gl_client_site_id').val();
        $.ajax({
            url: '/Admin/Settings?handler=ClientSiteEmail',
            type: 'GET',
            data: { clientSiteId: $('#gl_client_site_id').val() },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.length != 0) {
                const SiteEmail = result[0].siteEmail;
                const duressEmail = result[0].duressEmail;
                const duressSms = result[0].duressSms;
                const landLine = result[0].landLine;
                const isDataCollectionEnabled = result[0].dataCollectionEnabled;
                const IsDosDontListEnabledHidden = result[0].isDosDontList

                const guardLogEmailTo = result[0].guardLogEmailTo;
                const isUpdateLBDailyLog = result[0].uploadGuardLog;
                const isUpdateDailyFusionLog = result[0].uploadFusionLog;
                const isUpdateDailyKVLog = result[0].uploadKVLog;
                const isUpdateDailySWLog = result[0].uploadSWLog;
                $('#gs_site_email').val(SiteEmail);
                $('#gs_duress_email').val(duressEmail);
                $('#gs_duress_sms').val(duressSms);
                $('#gs_land_line').val(landLine);
                $('#gs_email_recipients').val(guardLogEmailTo);
                $('#enableLBLogDump').prop('checked', false);
                $('#enableKVLogDump').prop('checked', false);
                $('#enableSWLogDump').prop('checked', false);
                $('#enableFusionDump').prop('checked', false);
                $('#IsDosDontListEnabledHidden').val(IsDosDontListEnabledHidden);
                $('#IsDosDontList').prop('checked', IsDosDontListEnabledHidden);

                $('#cbxDisableDataCollection').prop('checked', !isDataCollectionEnabled);
                if (isUpdateLBDailyLog)
                    $('#enableLBLogDump').prop('checked', true);
                if (isUpdateDailyKVLog)
                    $('#enableKVLogDump').prop('checked', true);
                if (isUpdateDailySWLog)
                    $('#enableSWLogDump').prop('checked', true);
                if (isUpdateDailyFusionLog)
                    $('#enableFusionDump').prop('checked', true);

                $('#enableLBWeeklyLogDump').prop('checked', false);
                $('#enableKVWeeklyLogDump').prop('checked', false);
                $('#enableSWWeeklyLogDump').prop('checked', false);
                $('#enableFusionWeeklyDump').prop('checked', false);
                const guardLogEmailWeeklyLogTo = result[0].guardLogEmailWeeklyLogTo;
                const isUpdateLBWeeklyLog = result[0].uploadGuardWeeklyLog;
                const isUploadFusionWeeklyLog = result[0].uploadFusionWeeklyLog;
                const isUploadKVWeeklyLog = result[0].uploadKVWeeklyLog;
                const isUploadSWWeeklyLog = result[0].uploadSWWeeklyLog;
                if (isUpdateLBWeeklyLog)
                    $('#enableLBWeeklyLogDump').prop('checked', true);
                if (isUploadKVWeeklyLog)
                    $('#enableKVWeeklyLogDump').prop('checked', true);
                if (isUploadSWWeeklyLog)
                    $('#enableSWWeeklyLogDump').prop('checked', true);
                if (isUploadFusionWeeklyLog)
                    $('#enableFusionWeeklyDump').prop('checked', true);
                $('#gs_email_weely_recipients').val(guardLogEmailWeeklyLogTo);




                $('#enableLBMonthlyLogDump').prop('checked', false);
                $('#enableKVMonthlyLogDump').prop('checked', false);
                $('#enableSWMonthlyLogDump').prop('checked', false);
                $('#enableFusionMonthlyDump').prop('checked', false);
                const guardLogEmailMonthlyLogTo = result[0].guardLogEmailMonthlyLogTo;
                const isUpdateLBMonthlyLog = result[0].uploadGuardMonthlyLog;
                const isUploadFusionMonthlyLog = result[0].uploadFusionMonthlyLog;
                const isUploadKVMonthlyLog = result[0].uploadKVMonthlyLog;
                const isUploadSWMonthlyLog = result[0].uploadSWMonthlyLog;
                if (isUpdateLBMonthlyLog)
                    $('#enableLBMonthlyLogDump').prop('checked', true);
                if (isUploadKVMonthlyLog)
                    $('#enableKVMonthlyLogDump').prop('checked', true);
                if (isUploadSWMonthlyLog)
                    $('#enableSWMonthlyLogDump').prop('checked', true);
                if (isUploadFusionMonthlyLog)
                    $('#enableFusionMonthlyDump').prop('checked', true);
                $('#gs_email_monthly_recipients').val(guardLogEmailMonthlyLogTo);

            }
        }).fail(function () { });
    }
    /* //$('#enableLBLogDump').on('change', function () {
    //    if (this.checked) {
    //        $('#enableFusionDump').prop('checked', false);  // Uncheck the other box
    //    }
    //});

    // When #enableFusionDump is checked/unchecked
    //$('#enableFusionDump').on('change', function () {
    //    if (this.checked) {
    //        $('#enableLBLogDump').prop('checked', false);  // Uncheck the other box
    //    }
    //}); */

    /*Dropbox settings-start*/
    gridSiteDropboxSettings = $('#grid_Drpbx_Custom').grid({
        dataSource: '/admin/settings?handler=CustomDropboxSettings&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { width: 0, field: 'id', title: 'Id', hidden: true },
            { width: 0, field: 'clientSiteId', title: 'ClientSiteId', hidden: true },
            { width: '250', field: 'dropboxFolderName', title: 'Folder Name', editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridSiteDropboxSettings) {
        gridSiteDropboxSettings.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/admin/settings?handler=CustomDropboxSettings',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridSiteDropboxSettings.reload({ clientSiteId: $('#gl_client_site_id').val() });
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isCustomDropboxSettingsAdding)
                    isCustomDropboxSettingsAdding = false;
            });
        });
        gridSiteDropboxSettings.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure to delete this dropbox folder ?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/admin/settings?handler=DeleteCustomDropboxSettings',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridSiteDropboxSettings.reload({ clientSiteId: $('#gl_client_site_id').val() });
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isCustomDropboxSettingsAdding)
                        isCustomDropboxSettingsAdding = false;
                });
            }
        });
    }

    let isCustomDropboxSettingsAdding = false;

    $('#add_new_custom_dropboxsetting').on('click', function () {

        if (isCustomDropboxSettingsAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isCustomDropboxSettingsAdding = true;
            gridSiteDropboxSettings.addRow({ 'id': -1, 'dropboxFolderName': '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
        }
    });


    $('#save_site_dropboxsettings').on('click', function () {
        const token = $('input[name="__RequestVerificationToken"]').val();
        var _dropboxImagesDir = $('#DropboxImagesDir_DropboxSettings').val();
        const dt = {
            Id: $('#Id').val(),
            ClientSiteId: $('#gl_client_site_id').val(),
            DropboxImagesDir: _dropboxImagesDir, //DropboxImagesDir_DropboxSettings
            IsThermalCameraSite: $('#IsThermalCameraSite').is(":checked"),
            IsWeekendOnlySite: $('#IsWeekendOnlySite').is(":checked"),
            KpiTelematicsAndStatistics: $('#KpiTelematicsAndStatistics').is(":checked"),
            SmartWandPatrolReports: $('#SmartWandPatrolReports').is(":checked"),
            MonthlyClientReport: $('#MonthlyClientReport').is(":checked"),
            DropboxScheduleisActive: $('#DropboxScheduleisActive').is(":checked")
        };

        // Changes made by binoy due to mail on 13 Jul 2025, 21:49 related to Telematics - KV - Dropbox ERROR
        //Refer site.js [[$('#div_site_settings').on('click', '#save_site_settings', function ()]] for DropboxImagesDir issue
        $('#DropboxImagesDir').val(_dropboxImagesDir);

        $.ajax({
            url: '/admin/settings?handler=SaveDropboxSettings',
            data: { record: dt },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function (d) {
            $('#_dropboxStatusDisplay').html(d.message);
            if (d.success) {
                $('#_dropboxStatusDisplay').addClass('text-success').removeClass('text-danger').show().delay(5000).fadeOut('slow');
            } else {
                $('#_dropboxStatusDisplay').addClass('text-danger').removeClass('text-success').show().delay(5000).fadeOut('slow');
            }
        }).fail(function () {
            console.log('error');
        }).always(function () {

        });

    });

    /*Dropbox settings-end*/


    //crtical doc

    let gridCriticalDocument;

    function getClientSiteId() {
        return $('#gl_client_site_id').val(); // Get the client site ID from the hidden field or input
    }
    gridCriticalDocument = $('#tbl_CriticalDocument').grid({
        dataSource: '/Admin/Settings?handler=CriticalDocumentList&&clientSiteId=' + $('#singlepageclientSiteId').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            {
                field: 'groupName', title: 'Group Name', width: 70
            },
            /* { field: 'clientTypes', title: 'Client Types', width: 100 },*/
            { field: 'clientSites', title: 'Client Sites', width: 170 },
            {
                field: 'descriptions', title: 'Mandatory HR Documents', width: 180,
                renderer: function (value, record) {
                    function splitFirstComma(str) {
                        const index = str.indexOf(',');
                        if (index === -1) {
                            return [str, '']; // If there's no comma, return the string and an empty string
                        }
                        return [str.substring(0, index), str.substring(index + 1).trim()];
                    }
                    var descriptions = splitFirstComma(record.descriptions);
                    var referenceNos = splitFirstComma(record.referenceNO);
                    var html = '<table>';
                    html += '<tbody>';
                    for (var i = 0; i < descriptions.length; i++) {
                        var des = descriptions[i];
                        if (des != '') {
                            html += '<tr><td style="width: 58px;">' + record.hrGroupName + '</td><td style="width: 40px;">' + referenceNos[i] + '</td><td>' + descriptions[i] + '</td></tr>';
                        }
                    }
                    html += '</tbody>';
                    html += '</table>';
                    return html;
                }
            },
            { width: 110, renderer: schButtonRendererCrital },

        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }

    });

    function schButtonRendererCrital(value, record) {
        let buttonHtml = '';
        //buttonHtml += '<button class="btn btn-outline-primary mt-2 d-block" data-toggle="modal" data-target="#run-schedule-modal" data-sch-id="' + record.id + '""><i class="fa fa-play mr-2" aria-hidden="true"></i>Run</button>';
        buttonHtml += '<button class="btn btn-outline-primary mr-2 mt-2 d-block" data-toggle="modal" data-target="#Critical-modal" data-sch-id="' + record.id + '" ';
        buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil mr-2"></i>Edit</button>';
        buttonHtml += '<button class="btn btn-outline-danger mt-2 del-Cri d-block" data-sch-id="' + record.id + '""><i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
        return buttonHtml;
    }

    $('#add_criticalDocuments').on('click', function () {
        $('#clientSitesDoc').html('');
        $('#Critical-modal').modal('show');
        clearCriticalModal();
    });





    $('#clientTypeNameDoc').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });
    $('#clientSitesDoc').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });
    $('#clientTypeNameDoc').on('change', function () {
        let clientTypeIds = $(this).val().join(';')
        const clientTypeId = clientTypeIds;
        //$('#clientSitesDoc').multiselect("refresh");
        $('#clientSitesDoc').html('');
        const clientSiteControl = $('#clientSitesDoc');
        var selectedOption = $(this).find("option:selected");
        var selectedText = selectedOption.text();

        $.ajax({
            url: '/admin/settings?handler=ClientSitesNew',
            type: 'GET',
            data: {
                typeId: clientTypeId

            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {

            data.map(function (site) {
                clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
            });
            clientSiteControl.multiselect('rebuild');

        });

    });

    $('#clientSitesDoc').on('change', function () {
        const selectedValues = $(this).val().join(';').split(';');
        selectedValues.forEach(function (value) {
            if (value !== '') {
                const existing = $('#selectedSitesDoc option[value="' + value + '"]');
                if (existing.length === 0) {
                    const text = $('#clientSitesDoc option[value="' + value + '"]').text();
                    $('#selectedSitesDoc').append('<option value="' + value + '">' + text + '</option>');
                }
            }
        });
        updateSelectedSitesCount();
    });
    function updateSelectedSitesCount() {
        $('#selectedSitesCountDoc').text($('#selectedSitesDoc option').length);
        $('#selectedDescCountDoc').text($('#selectedDescDoc option').length);
    }
    $('#HRGroupDoc').on('change', function () {
        const option = $(this).val();
        if (option === '') {
            $('#DescriptionDoc').html('');
            $('#DescriptionDoc').append('<option value="">Select</option>');
        }

        $.ajax({
            url: '/admin/settings?handler=DescriptionList&HRGroupId=' + encodeURIComponent(option),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#DescriptionDoc').html('');
            $('#DescriptionDoc').append('<option value="">Select</option>');
            data.map(function (site) {
                $('#DescriptionDoc').append('<option value="' + site.value + '">' + site.text + '</option>');
            });
        });
    });
    $('#DescriptionDoc').on('change', function () {
        var Clientsite = $('#clientSitesDoc').val();
        if (Clientsite == 'Select') {
            confirm('please select a clientsite')
        }
        else {
            const elem = $(this).find(":selected");
            if (elem.val() !== '') {
                const existing = $('#selectedDescDoc option[value="' + elem.val() + '"]');
                if (existing.length === 0) {
                    $('#selectedDescDoc').append('<option value="' + elem.val() + '">' + elem.text() + '</option>');
                    updateSelectedDescCount();
                }
            }
        }

    });
    function updateSelectedDescCount() {
        $('#selectedDescCountDoc').text($('#selectedDescDoc option').length);
    }

    $('#btnSaveCriticalDoc').on('click', function () {
        $("input[name=clientSiteIds]").remove();
        var options = $('#selectedSitesDoc option');
        options.each(function () {
            const elem = '<input type="hidden" name="clientSiteIds" value="' + $(this).val() + '">';
            $('#frm_CriticalDoc').append(elem);
        });
        $("input[name=DescriptionIds]").remove();
        var optionsNew = $('#selectedDescDoc option');
        optionsNew.each(function () {
            const elem1 = '<input type="hidden" name="DescriptionIds" value="' + $(this).val() + '">';
            $('#frm_CriticalDoc').append(elem1);
        });
        $.ajax({
            url: '/admin/Settings?handler=SaveCriticalDocuments',
            type: 'POST',
            data: $('#frm_CriticalDoc').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                $('#Critical-modal').modal('hide');
                alert('Critical Document saved successfully');
                gridCriticalDocument.reload({ type: $('#sel_schedule').val() });
            } else {
                $('#CriDoc-modal-validation').html('');
                data.message.split(',').map(function (item) { $('#CriDoc-modal-validation').append('<li>' + item + '</li>') });
                $('#CriDoc-modal-validation').show().delay(5000).fadeOut();
            }
        });
    });

    function clearCriticalModal() {
        $('#CriticalDocId').val('0');
        //$('#clientTypeNameDoc').html('');
        $('#clientTypeNameDoc').val('');
        $("#clientTypeNameDoc").multiselect("refresh");
        $('#clientSitesDoc').html('');
        $('#clientSitesDoc').val('');
        $("#clientSitesDoc").multiselect("refresh");
        $('#DescriptionDoc').html('<option value="">Select</option>');
        var valueToSelect = "Select";
        $('#HRGroupDoc').val(valueToSelect);
        $('#clientTypeNameDoc').val('');
        $('#selectedSitesDoc').html('');

        $('#selectedDescDoc').html('');
        $('#GroupName').val('');
        updateSelectedSitesCount();
        $('input:hidden[name="clientSiteIds"]').remove();

        $('#CriDoc-modal-validation').html('');


    }
    $('#Critical-modal').on('shown.bs.modal', function (event) {
        clearCriticalModal();
        const button = $(event.relatedTarget);
        const isEdit = button.data('action') !== undefined && button.data('action') === 'editSchedule';
        if (isEdit) {
            schId = button.data('sch-id');
            CriticalModelOnEdit(schId);
        } else {
            //scheduleModalOnAdd();
        }

        /*showHideSchedulePopupTabs(isEdit);*/
    });
    function CriticalModelOnEdit(CriticalDocId) {
        $('#loader').show();
        $.ajax({
            url: '/admin/Settings?handler=CriticalDocList&id=' + CriticalDocId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#CriticalDocId').val(data.id);
            $('#GroupName').val(data.groupName);
            $.each(data.criticalDocumentsClientSites, function (index, item) {
                $('#selectedSitesDoc').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
                //$('#selectedDescDoc').append('<option value="' + item.hrSettings.id + '">' + item.hrSettings.description + '</option>');
                updateSelectedSitesCount();
            });
            $.each(data.criticalDocumentDescriptions, function (index, item) {
                $('#selectedDescDoc').append('<option value="' + item.hrSettings.id + '">' + item.hrSettings.description + '</option>');
                updateSelectedSitesCount();
            });

        }).always(function () {
            $('#loader').hide();
        });
    }

    $('#removeSelectedSites1').on('click', function () {
        $('#selectedSitesDoc option:selected').remove();
        updateSelectedSitesCount();
    });
    $('#removeSelectedSitesDoc').on('click', function () {
        $('#selectedDescDoc option:selected').remove();
        updateSelectedSitesCount();
    });

    $('#tbl_CriticalDocument').on('click', '.del-Cri', function () {
        const idToDelete = $(this).attr('data-sch-id');
        if (confirm('Are you sure want to delete this Document?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteCriticalDoc',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridCriticalDocument.reload({ type: $('#sel_schedule').val() });
            });
        }

    });
    //wand tags-start
    function tagsUIDEditor($editorContainer, value, record) {
        var textAreaForNotes = $('<input type="text" maxlength="30" class="form-control"/>');
        textAreaForNotes.val(record.uId); // safe way to set value
        $editorContainer.append(textAreaForNotes);
    }
    function tagsLabelEditor($editorContainer, value, record) {
        var textAreaForNotes = $('<input type="text" class="form-control"/>');
        textAreaForNotes.val(record.labelDescription); // safe way to set value
        $editorContainer.append(textAreaForNotes);
    }

    let gritdWandTags;
    //'/Admin/Settings?handler=TagType'

    let smartWandtagsListForDDL = [];
    $('#wandTagTypeDDL option').each(function () {
        var value = $(this).text();
        var text = $(this).text();

        if (value) {
            smartWandtagsListForDDL.push({ value: value, text: text });
        }
    });
    gritdWandTags = $('#cs-wand-tags').grid({
        dataSource: '/admin/settings?handler=WandTagsSettings&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            {
                title: '#',
                width: 50,
                align: 'center',
                sortable: false,
                tmpl: '<span class="gj-sno"></span>'
            },
            { width: '200', field: 'uId', title: 'UID', sortable: true, editor: tagsUIDEditor },
            { width: '100', field: 'tagsType', title: 'Type', align: 'center', type: 'dropdown', editor: { dataSource: smartWandtagsListForDDL, valueField: 'value', textField: 'value' } },

            { width: '100%', field: 'labelDescription', title: 'Label', sortable: true, editor: tagsLabelEditor },
            {
                width: '80',
                field: 'fqBypass',
                title: 'Fq Bypass',
                type: 'checkbox',
                align: 'center',
                editor: true // enables inline checkbox editing
            }
        ],

        dataBound: function (e, records, totalRecords) {

            var $table = $(e.target);

            $table.find('tbody tr').each(function (i) {
                $(this).find('.gj-sno').text(i + 1);
            });
            // Accurately count the number of data records passed to the grid.
            // Only count active tags that are not flagged as fqBypass
            var count = 0;
            if (records) {
                for (var j = 0; j < records.length; j++) {
                    if (records[j].fqBypass !== true) {
                        count++;
                    }
                }
            }
            
            var prevCount = parseInt($('#WandPointsPerPatrol').val());
            $('#WandPointsPerPatrol').val(count);
            $('#WandPointsPerPatrol').trigger('change'); // Updates daily targets
            
            // Silently auto-save KPI settings if the point count has changed
            if (prevCount !== count && !isNaN(prevCount)) {
                var _dropboxImagesDir = $('#DropboxImagesDir_DropboxSettings').val();
                if (_dropboxImagesDir) $('#DropboxImagesDir').val(_dropboxImagesDir);
                
                $.ajax({
                    url: '/admin/settings?handler=ClientSiteKpiSettings',
                    type: 'POST',
                    data: $('#frm_site_settings').serialize(),
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
                });
            }
        },

        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    let isWandTagsAdding = false;
    if (gritdWandTags) {
        gritdWandTags.on('rowDataChanged', function (e, id, record) {

            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();


            $.ajax({
                url: '/admin/settings?handler=SmartWandTagsSettings',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {
                if (result.success) {
                    gritdWandTags.reload({ clientSiteId: $('#gl_client_site_id').val() });
                } else {
                    gritdWandTags.edit(id);
                    alert(result.message);
                }

            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isWandTagsAdding)
                    isWandTagsAdding = false;
            });



        });

        gritdWandTags.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this  wand tag details?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/admin/settings?handler=DeleteSmartWandTagSettings',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gritdWandTags.reload({ clientSiteId: $('#gl_client_site_id').val() });
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isSmartWandAdding)
                        isSmartWandAdding = false;
                });
            }
        });
    }

    $('#add_wand_tag').on('click', function () {

        if (isWandTagsAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isWandTagsAdding = true;
            gritdWandTags.addRow({ 'id': -1, 'uId': '', tagsType: '', labelDescription: '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
        }
    });
    //wand tags-end
    //p2-171-equipments-start
    var editPositionGridRender;
    editPositionGridRender = function (value, record, $cell, $displayEl, id, $grid) {

        var isNewRow = record.id <= 0;

        var $edit = $('<button class="btn btn-outline-primary ml-2">' +
            '<i class="gj-icon pencil" style="font-size:15px"></i></button>')
            .attr('data-key', id);

        var $delete = $('<button type="button" class="btn btn-outline-danger ml-2">' +
            '<i class="fa fa-trash"></i></button>')
            .attr('data-key', id);

        var $update = $('<button class="btn btn-outline-success ml-2">' +
            '<i class="fa fa-check"></i></button>')
            .attr('data-key', id);

        var $cancel = $('<button class="btn btn-outline-secondary ml-2">' +
            '<i class="fa fa-times"></i></button>')
            .attr('data-key', id);

        /* ---------- BUTTON EVENTS ---------- */

        $edit.on('click', function () {
            $grid.edit(id);
            $edit.hide();
            $delete.hide();
            $update.show();
            $cancel.show();
        });

        $delete.on('click', function () {
            $grid.removeRow(id);
        });

        $update.on('click', function () {
            $grid.update(id);
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });

        $cancel.on('click', function () {
            $grid.cancel(id);

            if (isNewRow) {
                $grid.removeRow(id);   // optional: remove new row on cancel
            } else {
                $edit.show();
                $delete.show();
                $update.hide();
                $cancel.hide();
            }
        });

        /* ---------- INITIAL VISIBILITY ---------- */

        if (isNewRow) {
            $edit.hide();
            $delete.hide();
            $update.show();
            $cancel.show();
        } else {
            $update.hide();
            $cancel.hide();
        }

        $displayEl.empty()
            .append($edit)
            .append($delete)
            .append($update)
            .append($cancel);
    };

    //var editPositionGridRender;
    //editPositionGridRender = function (value, record, $cell, $displayEl, id, $grid) {
    //    var data = $grid.data(),
    //        $edit = $('<button class="btn btn-outline-primary ml-2"><i class="gj-icon pencil" style="font-size:15px"></i></button>').attr('data-key', id),
    //        $delete = $('<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_training" data-doc-id="' + record.id + '"><i class="fa fa-trash"></i></button>').attr('data-key', id),
    //        $update = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-check" aria-hidden="true"></i></button>').attr('data-key', id).hide(),
    //        $cancel = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-close" aria-hidden="true"></i></button>').attr('data-key', id).hide();
    //    $edit.on('click', function (e) {
    //        $grid.edit($(this).data('key'));
    //        $edit.hide();
    //        $delete.hide();
    //        $update.show();
    //        $cancel.show();
    //    });
    //    $delete.on('click', function (e) {
    //        $grid.removeRow($(this).data('key'));
    //    });
    //    $update.on('click', function (e) {
    //        $grid.update($(this).data('key'));
    //        $edit.show();
    //        $delete.show();
    //        $update.hide();
    //        $cancel.hide();
    //    });
    //    $cancel.on('click', function (e) {
    //        $grid.cancel($(this).data('key'));
    //        $edit.show();
    //        $delete.show();
    //        $update.hide();
    //        $cancel.hide();
    //    });
    //    $displayEl.empty().append($edit).append($delete).append($update).append($cancel);
    //}
    let gridSiteEquipmentsTags;
    gridSiteEquipmentsTags = $('#cs-siteequipments-tags').grid({
        dataSource: '/admin/settings?handler=SiteEquipmentSettings&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command', managementColumn: false },
        columns: [

            { width: '150', field: 'equipment', title: 'Equipment', align: 'center', type: 'dropdown', editor: { dataSource: '/admin/settings?handler=Equipments', valueField: 'name', textField: 'name' } },

            { width: '500', field: 'brand', title: 'Brand/Description', sortable: true, editor: true },
            {
                width: '200', field: 'serialNo', title: 'Serial No', align: 'center', editor: true
            },
            { renderer: editPositionGridRender, align: 'center' }

        ],

        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    let isSiteEquipmentsAdding = false;


    if (gridSiteEquipmentsTags) {
        gridSiteEquipmentsTags.on('rowDataChanged', function (e, id, record) {

            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();


            $.ajax({
                url: '/admin/settings?handler=SiteEquipmentSettings',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {
                if (result.success) {
                    gridSiteEquipmentsTags.reload({ clientSiteId: $('#gl_client_site_id').val() });
                } else {
                    gridSiteEquipmentsTags.edit(id);
                    alert(result.message);
                }

            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isSiteEquipmentsAdding)
                    isSiteEquipmentsAdding = false;
            });



        });

        gridSiteEquipmentsTags.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this  equipment details?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/admin/settings?handler=DeleteSiteEquipmentSettings',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridSiteEquipmentsTags.reload({ clientSiteId: $('#gl_client_site_id').val() });
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isSiteEquipmentsAdding)
                        isSiteEquipmentsAdding = false;
                });
            }
        });
    }

    $('#add_SiteEquipment_tag').on('click', function () {

        if (isSiteEquipmentsAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isSiteEquipmentsAdding = true;
            gridSiteEquipmentsTags.addRow({ 'id': -1, 'serialNo': '', equipmentId: '', brand: '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
        }
    });
    //p2-171-equipments-end
});


//ANPR Details start
function resetAnprModal() {

    $('#ANPR_Id').val('');
    $('#AnprKey_Profile').val('');
    $('#AnprKey_ApiCalls').val('');
    $('#AnprKey_LineLabel').val('');
    $('#AnprKey_Disabled').prop('checked', false);
    $('#AnprKey_SingleLane').prop('checked', false);
    $('#AnprKey_SeperateEntryAndExit').prop('checked', true);

    $('#csANPRValidationSummary').html('');
    $('#anpr-modal').modal('hide');
}
$('#add_anpr_key').on('click', function () {
    resetAnprModal();

    $('#anpr-modal').modal('show');

});
$('#btnanprclose').on('click', function () {
    $('#anpr-modal').modal('hide');
});

function displayANPRValidationSummary(errors) {
    $('#csANPRValidationSummary').removeClass('validation-summary-valid').addClass('validation-summary-errors');
    $('#csANPRValidationSummary').html('');
    $('#csANPRValidationSummary').append('<ul></ul>');
    if (!Array.isArray(errors)) {
        $('#csANPRValidationSummary ul').append('<li>' + errors + '</li>');
    } else {
        errors.forEach(function (item) {
            if (item.indexOf(',') > 0) {
                item.split(',').forEach(function (itemInner) {
                    $('#csANPRValidationSummary ul').append('<li>' + itemInner + '</li>');
                });
            } else {
                $('#csANPRValidationSummary ul').append('<li>' + item + '</li>');
            }
        });
    }
}
$('#AnprKey_Disabled, #AnprKey_SingleLane, #AnprKey_SeperateEntryAndExit').on('click', function () {
    // Uncheck all checkboxes except the one that was clicked
    $('#AnprKey_Disabled, #AnprKey_SingleLane, #AnprKey_SeperateEntryAndExit').not(this).prop('checked', false);
});


//ANPR Details stop

