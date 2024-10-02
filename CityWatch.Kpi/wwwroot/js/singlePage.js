//p2-140 key photos  -start
var FileuploadFileChanged = null;
//p2-140 key photos  -end
$(document).ready(function () {
       
});

$(function () {
    let gritdSmartWands;
    let gridSiteDropboxSettings;

    var clientSiteId = getUrlVars()["clientSiteId"];
    $("#gl_client_site_id").val(window.sharedVariable);
    $("#ClientSiteKey_ClientSiteId").val(window.sharedVariable);
    $('#ClientSiteCustomField_ClientSiteId').val(window.sharedVariable);

    gritdSmartWands = $('#cs-smart-wands').grid({
        dataSource: '/admin/settings?handler=SmartWandSettings&&clientSiteId=' + $('#gl_client_site_id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { width: 250, field: 'smartWandId', title: 'Smart Wand ID', editor: true },
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
        lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
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
                width: '4%',
                data: null,
                defaultContent: '<button  class="btn btn-outline-primary mr-2" id="btn_edit_cs_key"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
                 '<button id="btn_delete_cs_key" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i>Delete</button>',
                
                
                className: "text-center"
            },
        ],
       
    });
    //p2-140 key photos  -start
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
                    id:id,

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

    $('#chk_cs_ISOVIN').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_TrailerRego').prop('checked', false);
        }
        else {
            $('#chk_cs_TrailerRego').prop('checked', true);
        }
        $('#chk_cs_ISOVIN').val(isChecked);
    });

    $('#chk_cs_TrailerRego').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_cs_ISOVIN').prop('checked', false);
        }
        else {
            $('#chk_cs_ISOVIN').prop('checked', true);
        }
        $('#chk_cs_TrailerRego').val(isChecked);
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

        if ($('#chk_cs_ISOVIN').is(":checked")) {
            $('#chk_cs_IsISOVIN').val(true);

        }
        else {
            $('#chk_cs_IsISOVIN').val(false);

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
                isISOVINAcive: $('#chk_cs_IsISOVIN').val(),
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
                    $('#chk_cs_IsISOVIN').val(response[i].isActive);
                    if (response[i].isActive == true) {
                        $('#chk_cs_ISOVIN').prop('checked', true);
                        $('#chk_cs_TrailerRego').prop('checked', false);
                    }
                    else {
                        $('#chk_cs_ISOVIN').prop('checked', false);
                        $('#chk_cs_TrailerRego').prop('checked', true);
                    }

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

                const guardLogEmailTo = result[0].guardLogEmailTo;
                const isUpdateDailyLog = result[0].uploadGuardLog;
                $('#gs_site_email').val(SiteEmail);
                $('#gs_duress_email').val(duressEmail);
                $('#gs_duress_sms').val(duressSms);
                $('#gs_land_line').val(landLine);
                $('#gs_email_recipients').val(guardLogEmailTo);
                $('#enableLogDump').prop('checked', false);
                $('#cbxDisableDataCollection').prop('checked', !isDataCollectionEnabled);
                if (isUpdateDailyLog)
                    $('#enableLogDump').prop('checked', true);
            }
        }).fail(function () { });
    }

    
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
        const dt = {
            Id: $('#Id').val(),
            ClientSiteId: $('#gl_client_site_id').val(),
            DropboxImagesDir: $('#DropboxImagesDir').val(),
            IsThermalCameraSite: $('#IsThermalCameraSite').is(":checked"),
            IsWeekendOnlySite: $('#IsWeekendOnlySite').is(":checked"),
            KpiTelematicsAndStatistics: $('#KpiTelematicsAndStatistics').is(":checked"),
            SmartWandPatrolReports: $('#SmartWandPatrolReports').is(":checked"),
            MonthlyClientReport: $('#MonthlyClientReport').is(":checked")
        };

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
    gridCriticalDocument = $('#tbl_CriticalDocument').grid({
        dataSource: '/Admin/Settings?handler=CriticalDocumentList',
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
});
       
