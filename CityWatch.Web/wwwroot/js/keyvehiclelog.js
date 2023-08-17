$(document).ready(function () {
    $(document).on('show.bs.modal', '.modal', function () {
        const zIndex = 1040 + 10 * $('.modal:visible').length;
        $(this).css('z-index', zIndex);
        setTimeout(() => $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack'));
    });

    $(document).on('hidden.bs.modal', '.modal', () => $('.modal:visible').length && $(document.body).addClass('modal-open'));
});

$(function () {
    function isLogbookExpired(logBookDate) {
        if (((new Date()).toLocaleDateString('en-AU') > new Date(logBookDate).toLocaleDateString('en-AU'))) {
            return true;
        }
        return false;
    }

    function parseDateInKvlEntryFormat(dateValue, timeValue) {
        let day = dateValue.getDate();
        if (day < 10) day = '0' + day;

        let month = dateValue.getMonth() + 1;
        if (month < 10) month = '0' + month;

        return dateValue.getFullYear() + '-' + month + '-' + day + 'T' + timeValue;
    }

    function getTimeFromDateTime(value) {
        const mins = (value.getMinutes() < 10 ? '0' : '') + value.getMinutes();
        const hours = (value.getHours() < 10 ? '0' : '') + value.getHours();
        return hours + ':' + mins;
    }

    function convertDbString(value) { return (value === null || value === undefined) ? '' : value; }

    function convertDateTimeString(value) { return (value === null || value === undefined) ? '' : getTimeFromDateTime(new Date(value)); }

    function displayValidationSummary(errors) {
        const summaryDiv = document.getElementById('validation-summary');
        summaryDiv.className = "validation-summary-errors";
        summaryDiv.querySelector('ul').innerHTML = '';
        errors.forEach(function (item) {
            const li = document.createElement('li');
            li.appendChild(document.createTextNode(item));
            summaryDiv.querySelector('ul').appendChild(li);
        });
    }

    function displayProfileValidationSummary(errors) {
        const summaryDiv = document.getElementById('kvl-profiles-validation-summary');
        summaryDiv.className = "validation-summary-errors";
        summaryDiv.querySelector('ul').innerHTML = '';
        errors.forEach(function (item) {
            const li = document.createElement('li');
            li.appendChild(document.createTextNode(item));
            summaryDiv.querySelector('ul').appendChild(li);
        });
    }

    function format_kvl_child_row(d) {
        return (
            '<table cellpadding="7" cellspacing="0"  border="0" style="padding-left:50px;">' +
            '<tr>' +
            '<th colspan="4"><center>Trailers Rego or ISO</center></th>' +
            '<th colspan="3"><center>Individual</center></th>' +
            '<th rowspan="2"><center>Site POC</center></th>' +
            '<th rowspan="2"><center>Site Location</center></th>' +
            '<th rowspan="2"><center>Purpose Of Entry</center></th>' +
            '<th colspan="3"><center>Weight</center></th>' +
            '</tr>' +
            '<tr>' +
            '<th>1</th>' +
            '<th>2</th>' +
            '<th>3</th>' +
            '<th>4</th>' +
            '<th>Name</th>' +
            '<th>Mobile No:</th>' +
            '<th>Type</th>' +
            '<th>In Gross (t)</th>' +
            '<th>Out Net (t)</th>' +
            '<th>Tare (t)</th>' +
            '</tr>' +
            '<tr>' +
            '<td>' + convertDbString(d.detail.trailer1Rego) + '</td>' +
            '<td>' + convertDbString(d.detail.trailer2Rego) + '</td>' +
            '<td>' + convertDbString(d.detail.trailer3Rego) + '</td>' +
            '<td>' + convertDbString(d.detail.trailer4Rego) + '</td>' +
            '<td>' + convertDbString(d.detail.personName) + '</td>' +
            '<td>' + convertDbString(d.detail.mobileNumber) + '</td>' +
            '<td>' + convertDbString(d.personTypeText) + '</td>' +
            '<td>' + convertDbString(d.clientSitePocName) + '</td>' +
            '<td>' + convertDbString(d.clientSiteLocationName) + '</td>' +
            '<td>' + convertDbString(d.purposeOfEntry) + '</td>' +
            '<td width="8%">' + convertDbString(d.detail.inWeight) + '</td>' +
            '<td width="8%">' + convertDbString(d.detail.outWeight) + '</td>' +
            '<td width="8%">' + convertDbString(d.detail.tareWeight) + '</td>' +
            '</tr>' +
            '<tr>' +
            '<td colspan="13"><b>Notes:</b> ' + convertDbString(d.detail.notes) + '</td>' +
            '</tr>' +
            '</table>'
        );
    }

    function vehicleRegoToUpperCase() {
        let regoToUpper = $(this).val().toUpperCase();
        $(this).val(regoToUpper);
    }

    function vehicleRegoValidateSplChars(e) {        
        //  blocking special charactors
        if (e.which != 35 && e.which != 32 && e.which < 48 ||
            (e.which > 57 && e.which < 65) ||
            (e.which > 90 && e.which < 97) ||
            e.which > 122) {
            e.preventDefault();
        }
    }

    let keyVehicleLog;

    if ($('#vehicle_key_daily_log').length === 1) {
        keyVehicleLog = new DataTable('#vehicle_key_daily_log', {
            paging: false,
            searching: true,
            ordering: false,
            info: false,
            scrollX: true,
            ajax: {
                url: '/Guard/KeyVehicleLog?handler=KeyVehicleLogs',
                data: function (d) {
                    d.logbookId = $('#KeyVehicleLog_ClientSiteLogBookId').val();
                    d.kvlStatusFilter = $('#kvl_status_filter').val();
                },
                dataSrc: ''
            },
            columns: [{
                className: 'dt-control',
                orderable: false,
                data: null,
                width: '2%',
                defaultContent: '',
            },
            { data: 'detail.initialCallTime', width: '5%' },
            { data: 'detail.entryTime', width: '5%' },
            { data: 'detail.sentInTime', width: '5%' },
            { data: 'detail.exitTime', width: '5%' },
            { data: 'detail.timeSlotNo' },
            { data: 'detail.vehicleRego', width: '5%' },
            { data: 'plate' },
            { data: 'detail.companyName' },
            { data: 'truckConfigText' },
            { data: 'trailerTypeText' },
            { data: 'detail.keyNo', width: '10%' },
            { data: 'detail.mobileNumber', visible: false },
            { data: 'detail.personName', visible: false },
            {
                targets: -1,
                data: null,
                width: '12%',
                defaultContent: '<button id="btnEditVkl" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
                    '<button id="btnPrintVkl" class="btn btn-outline-primary mr-1 "><i class="fa fa-print"></i></button>' +
                    '<button id="btnDeleteVkl" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i>Delete</button>',
            }],
            'createdRow': function (row, data, index) {
                if (data.initialCallTime !== null) {
                    $('td', row).eq(1).html(convertDateTimeString(data.detail.initialCallTime));
                }
                if (data.entryTime !== null) {
                    $('td', row).eq(2).html(convertDateTimeString(data.detail.entryTime));
                }
                if (data.sentInTime !== null) {
                    $('td', row).eq(3).html(convertDateTimeString(data.detail.sentInTime));
                }
                if (data.exitTime !== null) {
                    $('td', row).eq(4).html(convertDateTimeString(data.detail.exitTime));
                }
                if (data.detail.exitTime == null) {
                    $('td', row).eq(4).html('<button type="button" class="btn btn-success btn-exit-quick">E</button> ');
                }
            },
            'drawCallback': function () {
                $('#total_events').html(this.fnSettings().fnRecordsTotal());
            }
        });
    }

    $('#search_kvl_log').on('keyup', function () {
        keyVehicleLog.search($(this).val()).draw();
    });

    $('#vehicle_key_daily_log tbody').on('click', 'td.dt-control', function () {
        var tr = $(this).closest('tr');
        var row = keyVehicleLog.row(tr);

        if (row.child.isShown()) {
            row.child.hide();
            tr.removeClass('shown');
        } else {
            row.child(format_kvl_child_row(row.data()), 'bg-light').show();
            tr.addClass('shown');
        }
    });

    $('.kvl-rec-filter > .dropdown-menu > .dropdown-item').on('click', function () {
        $(this).closest('.dropdown').find('button').html($(this).html());

        const kvlStatusFilter = $(this).data('val');
        $('#kvl_status_filter').val(kvlStatusFilter);
        $('#btn_kvl_pdf_download').attr('href', 'SiteLogPdf?id=' + $('#KeyVehicleLog_ClientSiteLogBookId').val() + '&t=vl&f=' + kvlStatusFilter);

        keyVehicleLog.ajax.reload();
    });

    $('#vkl-modal').on('shown.bs.modal', function (event) {
        const params = $(event.relatedTarget);
        bindKvlPopupEvents(!params[0].isNewEntry);
    });

    $('#add_new_vehicle_and_key_log').on('click', function () {
        loadVklPopup(0, true);
    });

    $('#vehicle_key_daily_log tbody').on('click', '#btnEditVkl', function () {
        var data = keyVehicleLog.row($(this).parents('tr')).data();
        loadVklPopup(data.detail.id);
    });

    $('#vehicle_key_daily_log tbody').on('click', '.btn-exit-quick', function () {
        var data = keyVehicleLog.row($(this).parents('tr')).data();
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=KeyVehicleLogQuickExit',
            type: 'POST',
            data: {
                id: data.detail.id
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            keyVehicleLog.ajax.reload();
        });
    });

    $('#vehicle_key_daily_log tbody').on('click', '#btnDeleteVkl', function () {
        var data = keyVehicleLog.row($(this).parents('tr')).data();
        if (confirm('Are you sure want to delete this vehicle key log entry?')) {
            $.ajax({
                type: 'POST',
                url: '/Guard/KeyVehicleLog?handler=DeleteKeyVehicleLog',
                data: { 'id': data.detail.id },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                beforeSend: function () {
                    $('#loader').show();
                }
            }).done(function () {
                keyVehicleLog.ajax.reload();
            }).always(function () {
                $('#loader').hide();
            });
        }
    });

    let isKeyAllocatedModal;
    let isVehicleOnsiteModal
    let isVehicleInAnotherSiteModal;
    if ($('#vehicle_key_daily_log').length === 1) {
        isVehicleOnsiteModal = new ConfirmationModal('vehicle-onsite', {
            message: 'This truck was already onsite today and there is no exit time recorded.<br/><br/>Are you sure you want to create a new entry when it appears they are already onsite?',
            onYes: function () { saveKeyVehicleLogEntry(); }
        });
        isVehicleInAnotherSiteModal = new ConfirmationModal('vehicle-on-anothersite', {
            message: '',
            onYes: function () { saveKeyVehicleLogEntry(); }
        });

        isKeyAllocatedModal = new ConfirmationModal('key-alloc', {
            message: 'This key was already allocated and there is no exit time recorded.<br/><br/>Are you sure you want to create a new entry when it appears they are already allocated?',
            onYes: function () { selectKey(); }
        });
    }

    $('.save_new_vehicle_and_key_log').on('click', function () {
        if ($('#kvlActionIsEdit').val() === 'true') {
            saveKeyVehicleLogEntry();
        } else {
            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=IsVehicleOnsite',
                type: 'GET',
                data: {
                    logbookId: $('#KeyVehicleLog_ClientSiteLogBookId').val(),
                    vehicleRego: $('#VehicleRego').val(),
                }
            }).done(function (vehicleIsOnsite) {
                if (vehicleIsOnsite.status === 2) {
                    isVehicleInAnotherSiteModal.message = 'This truck is showing as being at ' + vehicleIsOnsite.clientSite + ' today and there is no exit time recorded. <br/><br/> Are you sure ID/ PLATE is correct and you STILL want to enter it?';
                    isVehicleInAnotherSiteModal.showConfirmation();
                }
                else if (vehicleIsOnsite.status === 1) {
                    isVehicleOnsiteModal.showConfirmation();
                }
                else if (vehicleIsOnsite.status === 0)
                    saveKeyVehicleLogEntry();
            });
        }
    });

    function saveKeyVehicleLogEntry() {
        $('#validation-summary ul').html('');

        const mobileNo = $('#MobileNumber').val();
        if (mobileNo === '+61 (0) ')
            $('#MobileNumber').val('');

        const today = new Date();

        if ($('#new_log_initial_call').val() !== '')
            $('#InitialCallTime').val(parseDateInKvlEntryFormat(today, $('#new_log_initial_call').val()));

        if ($('#new_log_entry_time').val() !== '')
            $('#EntryTime').val(parseDateInKvlEntryFormat(today, $('#new_log_entry_time').val()));

        if ($('#new_log_sent_in_time').val() !== '')
            $('#SentInTime').val(parseDateInKvlEntryFormat(today, $('#new_log_sent_in_time').val()));

        if ($('#new_log_exit_time').val() !== '')
            $('#ExitTime').val(parseDateInKvlEntryFormat(today, $('#new_log_exit_time').val()));

        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=SaveKeyVehicleLog',
            type: 'POST',
            data: $('#form_new_vehicle_and_key_log').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            beforeSend: function () {
                $('#loader').show();
            }
        }).done(function (result) {
            if (result.success) {
                keyVehicleLog.ajax.reload();
                $('#vkl-modal').modal('hide');
            } else {
                displayValidationSummary(result.errors);
            }
        }).always(function () {
            $('#loader').hide();
        });
    }

    function selectKey() {
        const option = $('#list_clientsite_keys').find(":selected");
        if (option.val() === '')
            return;

        const keyNo = option.text();
        const keyId = option.val();
        if (keyId !== '') {
            const currentKeyNos = $('#KeyNo').val();
            if (!currentKeyNos.includes(keyNo)) {
                $.ajax({
                    url: '/Guard/KeyVehicleLog?handler=ClientSiteKeyDescription',
                    type: 'GET',
                    data: {
                        keyId: keyId,
                        clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                    }
                }).done(function (response) {
                    const rowHtml = '<tr>' +
                        '<td>' + keyNo + '</td>' +
                        '<td>' + response + '</td>' +
                        '<td><i class="fa fa-trash-o text-danger btn-delete-kvl-Key" title="Delete" style="cursor: pointer;"></i></td>' +
                        '</tr>';
                    $("#kvl-keys-list").append(rowHtml);
                    if (currentKeyNos === '') $('#KeyNo').val(keyNo);
                    else $('#KeyNo').val(currentKeyNos + '; ' + keyNo);
                });
            }
            $("#list_clientsite_keys").val('').trigger('change');
        }
    }

    $('#kv_duress_btn').on('click', function () {
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=SaveClientSiteDuress',
            data: {
                clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                GuardId: $('#KeyVehicleLog_GuardLogin_GuardId').val(),
                guardLoginId: $('#KeyVehicleLog_GuardLogin_Id').val(),
                logBookId: $('#KeyVehicleLog_ClientSiteLogBookId').val()
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#kv_duress_image').attr("src", '/images/DuressButton.jpg');
                $("#kv_duress_status").text("Active");
            }
        });
    });

    
    let gridKeyVehicleLogProfile = $('#key_vehicle_log_profiles').DataTable({
        paging: false,
        ordering: false,
        info: false,
        searching: false,
        data: [],
        columns: [
            { data: 'detail.id', visible: false },
            { data: 'plate' },
            { data: 'detail.companyName' },
            { data: 'detail.personName' },
            { data: 'personTypeText' },
            {
                targets: -1,
                data: null,
                className: 'text-center',
                defaultContent: '<button id="btnSelectProfile" class="btn btn-outline-primary">Select</button>'
            },
        ],
    });

    $('#key_vehicle_log_profiles tbody').on('click', '#btnSelectProfile', function () {
        var data = gridKeyVehicleLogProfile.row($(this).parents('tr')).data();
        populateKvlModal(data.detail.id);        
    });

    function populateKvlModal(id) {        
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=ProfileById&id=' + id,
            type: 'GET',
            dataType: 'json',
        }).done(function (result) {
            let personName = result.personName ? result.personName : 'Unknown';
            $('#PlateId').val(result.keyVehicleLogProfile.plateId);
            $('#kvl_list_plates').val(result.keyVehicleLogProfile.plateId);
            $('#TruckConfig').val(result.keyVehicleLogProfile.truckConfig);
            $('#TrailerType').val(result.keyVehicleLogProfile.trailerType);
            $('#MaxWeight').val(result.keyVehicleLogProfile.maxWeight);
            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer1Rego);
            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer2Rego);
            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer3Rego);
            $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer4Rego);
            $('#CompanyName').val(result.companyName);
            $('#PersonName').val(personName);
            $('#PersonType').val(result.personType);
            $('#MobileNumber').val(result.keyVehicleLogProfile.mobileNumber);
            $('#EntryReason').val(result.keyVehicleLogProfile.entryReason);
            $('#Product').val(result.keyVehicleLogProfile.product);
            $('#Notes').val(result.keyVehicleLogProfile.notes);
            $("#list_product").val(result.keyVehicleLogProfile.product);
            $("#list_product").trigger('change');
            $('#Sender').val(result.sender);
            $('#lblIsSender').text(result.isSender ? 'Sender' : 'Reciever');
            $('#cbIsSender').prop('checked', result.isSender);

            loadAuditHistory(result.keyVehicleLogProfile.vehicleRego);
        });
        $('#kvl-profiles-modal').modal('hide');
    }

    $('#key_vehicle_log_profiles tbody').on('click', 'tr', function () {
        gridKeyVehicleLogProfile.$('tr.selected').removeClass('selected');
        $(this).addClass('selected');
    });

    function loadVklPopup(id, isNewEntry) {
        if (isLogbookExpired($('#KeyVehicleLog_ClientSiteLogBook_Date').val())) {
            alert('A new day started and this logbook expired. Please logout and login again');
            return false;
        }

        const vkl_modal_title = isNewEntry ? 'Add a new log book entry' : 'Edit log book entry';
        $('#vkl-modal').find('.modal-title').html(vkl_modal_title);

        $.ajax({
            type: 'GET',
            url: '/Guard/KeyVehicleLog?handler=KeyVehicleLog',
            data: { 'id': id },
            beforeSend: function () {
                $('#loader').show();
            }
        }).done(function (response) {
            $('#vkl-modal').find(".modal-body").html(response);
            $('#vkl-modal').modal('show', { isNewEntry: isNewEntry });
            $('#kvl_status_pd').hide();

            if (isNewEntry) {
                $('#new_log_initial_call').val(getTimeFromDateTime(new Date()));
                $('#new_log_entry_time').val(getTimeFromDateTime(new Date()));
                $('#new_log_sent_in_time').val(getTimeFromDateTime(new Date()));
                $('#ActiveGuardLoginId').val('');
            } else {
                if ($('#InialCallTime').val() !== '') {
                    $('#new_log_initial_call').val(getTimeFromDateTime(new Date($('#InitialCallTime').val())));
                }
                if ($('#EntryTime').val() !== '') {
                    $('#new_log_entry_time').val(getTimeFromDateTime(new Date($('#EntryTime').val())));
                }

                if ($('#SentInTime').val() !== '') {
                    $('#new_log_sent_in_time').val(getTimeFromDateTime(new Date($('#SentInTime').val())));
                }

                const previousDayEntry = $('#IsPreviousDayEntry').val().toLowerCase() === 'true';
                $('#new_log_initial_call, #new_log_entry_time, #new_log_sent_in_time').prop('readonly', previousDayEntry);
                $('#clear_initialcall_time, #clear_entry_time, #clear_sentin_time').prop('hidden', previousDayEntry);
                if (previousDayEntry) $('#kvl_status_pd').show();

                if ($('#ExitTime').val() !== '')
                    $('#new_log_exit_time').val(getTimeFromDateTime(new Date($('#ExitTime').val())));

                $('#ActiveGuardLoginId').val($('#KeyVehicleLog_GuardLogin_Id').val());
            }
        }).always(function () {
            $('#loader').hide();
        });
    }

    let gridAuditHistory;   
    function loadAuditHistory(item) {
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=AuditHistory&vehicleRego=' + item,
            type: 'GET',
            dataType: 'json',
        }).done(function (result) {
            $('#key_vehicle_log_audit_history').DataTable().clear().rows.add(result).draw();
        });
    }

    function bindKvlPopupEvents(isEdit) {

        if (!isEdit) {
            $('#IsTimeSlotNo').val(true);
            $('#cbIsTimeSlotNo').prop('checked', true);
            $('#IsSender').val(true);
            $('#cbIsSender').prop('checked', true);            
        }
        else {
            let isTimeSlot = $('#IsTimeSlotNo').val().toLowerCase() === 'true';
            $('#lblIsTimeSlotNo').text(isTimeSlot ? 'Time Slot No.' : 'T.No. (Load)');
            $('#cbIsTimeSlotNo').prop('checked', isTimeSlot);

            let isSender = $('#IsSender').val().toLowerCase() === 'true';
            $('#lblIsSender').text(isSender ? 'Sender' : 'Reciever');
            $('#cbIsSender').prop('checked', isSender);
            loadAuditHistory($('#VehicleRego').val());
        }

        $('#cbIsTimeSlotNo').on('change', function () {
            const isChecked = $(this).is(':checked');
            $('#lblIsTimeSlotNo').text(isChecked ? 'Time Slot No.' : 'T.No. (Load)');
            $('#IsTimeSlotNo').val(isChecked);
        });

        $('#cbIsSender').on('change', function () {
            const isChecked = $(this).is(':checked');
            $('#lblIsSender').text(isChecked ? 'Sender' : 'Reciever');
            $('#IsSender').val(isChecked);
        });

        $('#kvlActionIsEdit').val(isEdit);

        $('#list_product').attr('placeholder', 'Select Or Edit').editableSelect({
            effects: 'slide'
        }).on('select.editable-select', function (e, li) {
            $('#Product').val(li.text());
        });

        if ($('#MoistureDeduction').is(':checked') || $('#RubbishDeduction').is(':checked')) {
            $('#DeductionPercentage').attr('disabled', false);
        }

        $('#MoistureDeduction, #RubbishDeduction').on('change', function () {
            const deductionChecked = $('#MoistureDeduction').is(':checked') || $('#RubbishDeduction').is(':checked');
            $('#DeductionPercentage').attr('disabled', !deductionChecked);

            if (!deductionChecked) {
                $('#DeductionPercentage').val('');
                setCalculatedOutWeight();
            }
        });

        $('#DeductionPercentage').on('blur', function () {
            setCalculatedOutWeight();
        });

        if ($('#Product').val() !== '') {
            let itemToSelect = $('#list_product').siblings('.es-list').find('li:contains("' + $('#Product').val() + '")')[0];
            if (itemToSelect) {
                $('#list_product').editableSelect('select', $(itemToSelect));
                $('#vkl-modal').focus();
            }
        }

        $('#InWeight, #OutWeight').on('change', function () {
            const weightInValue = $("#InWeight").val();
            const weightOutValue = $('#OutWeight').val();

            let weightIn = weightInValue !== '' ? parseFloat(weightInValue) : 0;
            let weightOut = (weightOutValue !== '') ? parseFloat(weightOutValue) : 0;

            if (weightOut > weightIn) {
                $('#weight_in_term').html('Net');
                $('#weight_out_term').html('Gross');
            }
            else {
                $('#weight_in_term').html('Gross');
                $('#weight_out_term').html('Net');
            }
        });

        $('#InWeight, #TareWeight').on('blur', function () {
            setCalculatedOutWeight();
        });

        function setCalculatedOutWeight() {
            let outWeight = getOutWeight();
            $('#OutWeight').val(outWeight);
        }

        function getOutWeight() {
            const weightInValue = $("#InWeight").val();
            const weightTareValue = $("#TareWeight").val();
            if (weightTareValue) {
                let weightIn = weightInValue !== '' ? parseFloat(weightInValue) : 0;
                let weightTare = (weightTareValue !== '') ? parseFloat(weightTareValue) : 0;
                let outWeight = Math.abs(weightIn - weightTare).toFixed(2);
                const deductionPercentage = $('#DeductionPercentage').val();
                if (deductionPercentage) {
                    const outWeightPercentage = ((outWeight * deductionPercentage) / 100).toFixed(2);
                    return parseFloat(outWeight - outWeightPercentage).toFixed(2);
                }
                return outWeight;
            }
        }

        $('#OutWeight').on('focus', function () {
            if ($(this).val())
                new MessageModal({ message: "This field is automatic – are you sure you want to input a manual weight?" }).showWarning();
        });

        // HACK: Handle close of generic message popup
        $('#message-modal').on('hide.bs.modal', function () {
            $('#OutWeight').focus();
        });

        let list_clientsite_keys = $('#list_clientsite_keys').select2({
            placeholder: "Select",
            theme: 'bootstrap4',
            allowClear: true,
            ajax: {
                url: '/Guard/KeyVehicleLog?handler=ClientSiteKeys',
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return {
                        clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                        searchKeyNo: params.term,
                        searchKeyDesc: $('#search_key').val()
                    }
                },
                processResults: function (data) {
                    return {
                        results: $.map(data, function (item) {
                            return {
                                text: item.keyNo,
                                id: item.id,
                                title: item.description
                            }
                        })
                    };
                },
                cache: true
            }
        });

        if (isEdit) {

            if ($('#KeyNo').val() !== '') {
                const selectedKeyNo = $('#KeyNo').val();
                $.ajax({
                    url: '/Guard/KeyVehicleLog?handler=ClientSiteKeys',
                    type: 'GET',
                    dataType: 'json',
                    data: {
                        clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                        searchKeyNo: selectedKeyNo
                    }
                }).done(function (response) {
                    const result = response.find(z => z.keyNo === selectedKeyNo);
                    if (result) {
                        $('#list_clientsite_keys').append(new Option(result.keyNo, result.id, false, true))
                    };
                });
            }

            if ($('#PlateId').val() !== '') {
                $('#kvl_list_plates').val($('#PlateId').val())
            }

            if ($('#VehicleRego').val() !== '') {
                $('#kvl_list_plates').attr('disabled', false);
            }
        }

        $('#VehicleRego').on('blur', function () {
            const vehicleRegoHasVal = $(this).val() !== '';
            $('#kvl_list_plates').attr('disabled', !vehicleRegoHasVal);
            if (!vehicleRegoHasVal) {
                // TODO: clear previous auto populated profile values
            }
        });

        $('#VehicleRego, #Trailer1Rego, #Trailer2Rego, #Trailer3Rego, #Trailer4Rego').on('keyup', vehicleRegoToUpperCase);

        $('#VehicleRego, #Trailer1Rego, #Trailer2Rego, #Trailer3Rego, #Trailer4Rego').on('keypress', vehicleRegoValidateSplChars);

        $('#clear_initialcall_time').on('click', function () {
            $('#new_log_initial_call').val('');
            $('#InitialCallTime').val('');
        });

        $('#clear_entry_time').on('click', function () {
            $('#new_log_entry_time').val('');
            $('#EntryTime').val('');
        });

        $('#clear_sentin_time').on('click', function () {
            $('#new_log_sent_in_time').val('');
            $('#SentInTime').val('');
        });

        $('#clear_exit_time').on('click', function () {
            $('#new_log_exit_time').val('');
            $('#ExitTime').val('');
        });

        $('#kvl_list_plates').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {
                $('#PlateId').val(option.val());
            }
        });

        $('#list_clientsite_keys').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() === '')
                return;

            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=IsKeyAllocated',
                type: 'GET',
                data: {
                    logbookId: $('#KeyVehicleLog_ClientSiteLogBookId').val(),
                    keyNo: option.text(),
                }
            }).done(function (keyIsAllocated) {
                if (!keyIsAllocated) selectKey();
                else isKeyAllocatedModal.showConfirmation();
            });
        });

        $('#kvl-keys-list').on('click', '.btn-delete-kvl-Key', function () {
            var removeKeyNo = $(this).closest('tr').find('td:first').text();
            var updatedKeyNos = removeKeyValue($('#KeyNo').val(), removeKeyNo);
            $('#KeyNo').val(updatedKeyNos);
            $(this).parent().parent().remove();
        });

        function removeKeyValue(list, value, separator) {
            separator = separator || "; ";
            var values = list.split(separator);
            for (var i = 0; i < values.length; i++) {
                if (values[i] == value) {
                    values.splice(i, 1);
                    return values.join(separator);
                }
            }
            return list;
        }

        function adjustKvlAttachmentCount(up) {
            let value = parseInt($('#kvl_attachments_count').html());
            value = (up === true) ? value + 1 : value - 1;
            $('#kvl_attachments_count').html(value);
        }

        $('#kvl-attachment-list').on('click', '.btn-delete-kvl-attachment', function (event) {
            if (confirm('Are you sure want to remove this attachment?')) {
                var target = event.target;
                const fileName = target.parentNode.innerText.trim();
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=DeleteAttachment',
                    type: 'POST',
                    dataType: 'json',
                    data: {
                        reportReference: $('#ReportReference').val(),
                        fileName: fileName
                    },
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result) {
                        target.parentNode.parentNode.removeChild(target.parentNode);
                        adjustKvlAttachmentCount(false);
                    }
                });
            }
        });

        $('#kvl_attachment_upload').on("change", function (e) {
            const fileUpload = this;
            if (fileUpload.files.length > 0) {

                let arIndex = [];
                const attachmentList = document.getElementById('kvl-attachment-list').getElementsByTagName('li');
                for (let i = 0; i < attachmentList.length; i++)
                    arIndex.push(parseInt(attachmentList[i].getAttribute('data-index')));
                let attachIndex = arIndex.length > 0 ? Math.max(...arIndex) + 1 : 0;

                for (let i = 0; i < fileUpload.files.length; i++, attachIndex++) {

                    const file = fileUpload.files.item(i);

                    const attachment_id = 'attach_' + attachIndex;
                    const li = document.createElement('li');
                    li.id = attachment_id;
                    li.className = 'list-group-item';
                    li.dataset.index = attachIndex;
                    let liText = document.createTextNode(file.name);

                    const icon = document.createElement("i");
                    icon.className = 'fa fa-circle-o-notch fa-spin ml-2 text-success';
                    icon.title = 'Uploading...';
                    icon.style = 'cursor:pointer';

                    li.appendChild(liText);
                    li.appendChild(icon);
                    document.getElementById('kvl-attachment-list').append(li);

                    // upload file to server
                    const fileForm = new FormData();
                    fileForm.append('attachments', fileUpload.files.item(i))
                    fileForm.append('attach_id', attachment_id);
                    fileForm.append('report_reference', $('#ReportReference').val());
                    $.ajax({
                        url: '/Guard/KeyVehiclelog?handler=Upload',
                        type: 'POST',
                        data: fileForm,
                        dataType: 'json',
                        processData: false,
                        contentType: false,
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    }).done(function (result) {
                        const icon = document.getElementById(result.attachmentId).getElementsByTagName('i').item(0);
                        if (result.success) {
                            icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                            icon.title = 'Delete';
                        } else {
                            icon.className = 'fa fa-exclamation-triangle ml-2 text-warning';
                            icon.title = 'Error';
                        }

                        adjustKvlAttachmentCount(true);
                    });
                }
            }
        });

        $('#CompanyName').typeahead({
            minLength: 3,
            source: function (request, response) {
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=CompanyNames',
                    data: { companyNamePart: request },
                    type: 'GET',
                    dataType: 'json',
                    success: function (data) {
                        items = [];
                        map = {};
                        $.each(data, function (i, item) {
                            items.push(item);
                        });
                        response(items);
                    },
                    error: function (response) {
                        alert(response.responseText);
                    },
                    failure: function (response) {
                        alert(response.responseText);
                    }
                });
            },
        });

        $('#Sender').typeahead({
            minLength: 3,
            source: function (request, response) {
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=CompanyNames',
                    data: { companyNamePart: request },
                    type: 'GET',
                    dataType: 'json',
                    success: function (data) {
                        items = [];
                        map = {};
                        $.each(data, function (i, item) {
                            items.push(item);
                        });
                        response(items);
                    },
                    error: function (response) {
                        alert(response.responseText);
                    },
                    failure: function (response) {
                        alert(response.responseText);
                    }
                });
            },
        });

        $('#VehicleRego').typeahead({
            minLength: 3,
            autoSelect: true,
            source: function (request, response) {
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=VehicleRegos',
                    data: { regoPart: request },
                    type: 'GET',
                    dataType: 'json',
                    success: function (data) {
                        items = [];
                        map = {};
                        $.each(data, function (i, item) {
                            items.push(item);
                        });
                        response(items);
                    },
                    error: function (response) {
                        alert(response.responseText);
                    },
                    failure: function (response) {
                        alert(response.responseText);
                    }
                });
            },
            afterSelect: function (item) {
                if (item) {
                    $.ajax({
                        url: '/Guard/KeyVehicleLog?handler=ProfileByRego&truckRego=' + item,
                        type: 'GET',
                        dataType: 'json',
                    }).done(function (result) {
                        if (result.length > 0) {
                            gridKeyVehicleLogProfile.clear().rows.add(result).draw();
                            $('#driver_name').val('Unknown');
                            $('#duplicate_profile_status').text('');
                            $('#kvl-profiles-modal').find('#kvl-profile-title-rego').html(item);
                            $('#kvl-profiles-modal').modal('show');
                        }
                    });
                }                
            }
        });        
        
        if (!gridAuditHistory) {
            $('#key_vehicle_log_audit_history').DataTable({
                autoWidth: false,
                paging: true,
                ordering: false,
                info: false,
                searching: false,
                pageLength: 10,
                data: [],
                columns: [
                    { data: 'auditTimeString', width: '32%' },
                    { data: 'guardLogin.guard.initial', width: '15%' },
                    { data: 'auditMessage' },
                ],
            });
        }
    }

    /****** Key Vehicle Log Profile in Audit Site Log *******/
    function format(d) {
        return (
            '<table cellpadding="5" cellspacing="0" border="0" style="padding-left:50px; width: 100%">' +
            '<tr>' +
            '<th colspan="4"><center>Trailers Rego or ISO</center></th>' +
            '<th colspan="3"><center>Individual</center></th>' +
            '<th rowspan="2">Purpose Of Entry</th>' +
            '</tr>' +
            '<tr>' +
            '<th>1</th>' +
            '<th>2</th>' +
            '<th>3</th>' +
            '<th>4</th>' +
            '<th>Name</th>' +
            '<th>Mobile No:</th>' +
            '<th>Type</th>' +
            '</tr>' +
            '<tr>' +
            '<td>' + convertDbString(d.detail.trailer1Rego) + '</td>' +
            '<td>' + convertDbString(d.detail.trailer2Rego) + '</td>' +
            '<td>' + convertDbString(d.detail.trailer3Rego) + '</td>' +
            '<td>' + convertDbString(d.detail.trailer4Rego) + '</td>' +
            '<td>' + convertDbString(d.detail.personName) + '</td>' +
            '<td>' + convertDbString(d.detail.mobileNumber) + '</td>' +
            '<td>' + convertDbString(d.personTypeText) + '</td>' +
            '<td>' + convertDbString(d.purposeOfEntry) + '</td>' +
            '</tr>' +
            '<tr>' +
            '<th colspan="8">Notes</th>' +
            '</tr>' +
            '<tr>'+
            '<td colspan="8" rowspan="2">' + convertDbString(d.detail.notes)+'</td>' +
            '</tr>' +
            '</table>'
        );
    }

    let gridKeyVehicleLogProfiles = $('#tbl-audit-kvl-profiles').DataTable({
        paging: true,
        lengthMenu: [[75, 100, -1], [75, 100, "All"]],
        pageLength: 100,
        searching: true,
        order: [[1, "asc"]],
        ordering: true,
        info: false,
        scrollX: false,
        autoWidth: false,
        ajax: {
            url: '/Admin/AuditSiteLog?handler=KeyVehicleLogProfiles',
            data: function (d) {
                d.truckRego = $('#kvlProfileRegos').find(':selected').val();
            },
            dataSrc: ''
        },
        columns: [
            {
                className: 'dt-control',
                orderable: false,
                data: null,
                width: '2%',
                defaultContent: '',
            },
            { data: 'detail.keyVehicleLogProfile.vehicleRego', width: '8%' },
            { data: 'plate', orderable: false, width: '4%' },
            { data: 'detail.companyName', width: '8%' },
            { data: 'detail.personName', orderable: false, width: '8%' },
            { data: 'clientSite', orderable: false, width: '8%' },
            { data: 'truckConfigText', orderable: false, width: '8%' },
            { data: 'trailerTypeText', orderable: false, width: '8%' },
            {
                orderable: false,
                width: '9%',
                targets: -1,
                data: null,
                defaultContent: '<button id="btnEditKvlProfile" type="button" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
                    '<button id="btnDeleteKvlProfile" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i>Delete</button>',
            },
        ],
    });

    $('#tbl-audit-kvl-profiles tbody').on('click', 'td.dt-control', function () {
        var tr = $(this).closest('tr');
        var row = gridKeyVehicleLogProfiles.row(tr);

        if (row.child.isShown()) {
            row.child.hide();
            tr.removeClass('shown');
        } else {
            row.child(format(row.data()), 'bg-light').show();
            tr.addClass('shown');
        }
    });

    $('#tbl-audit-kvl-profiles tbody').on('click', '#btnEditKvlProfile', function () {
        var data = gridKeyVehicleLogProfiles.row($(this).parents('tr')).data();
        loadKvlProfilePopup(data.detail.id);
    });

    $('#tbl-audit-kvl-profiles tbody').on('click', '#btnDeleteKvlProfile', function () {
        if (confirm('Are you sure want to delete this field?')) {
            var data = gridKeyVehicleLogProfiles.row($(this).parents('tr')).data();
            $.ajax({
                url: '/Admin/AuditSiteLog?handler=DeleteKeyVehicleLogProfile',
                data: { 'id': data.detail.id },
                type: 'POST',
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status)
                    gridKeyVehicleLogProfiles.ajax.reload(null, false);
            });
        }
    });

    $('#kvlProfileRegos').select2({
        theme: 'bootstrap4',
        ajax: {
            url: '/Admin/AuditSiteLog?handler=VehicleRegos',
            dataType: 'json',
            delay: 250,
            processResults: function (data) {
                return {
                    results: $.map(data, function (item) {
                        return {
                            id: item.value || '',
                            text: item.text,
                            value: item.value
                        }
                    })
                };
            },
            cache: true
        },
    }).on("select2:select", function (e) {
        gridKeyVehicleLogProfiles.ajax.reload();
    });

    $('#btn_update_kvl_profile').on('click', function () {
        $.ajax({
            url: '/Admin/AuditSiteLog?handler=UpdateKeyVehicleLogProfile',
            data: $('#frm_edit_kvl_profile').serialize(),
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#kvl-profiles-audit-modal').modal('hide');
                gridKeyVehicleLogProfiles.ajax.reload(null, false);
            } else {
                displayProfileValidationSummary(result.errors);
            }
        });
    });

    $('#btn_add_vistor_profile').on('click', function () {
        loadKvlProfilePopup(0);
    });

    function loadKvlProfilePopup(id) {
        $.ajax({
            type: 'GET',
            url: '/Admin/AuditSiteLog?handler=KeyVehicleLogProfile&id=' + id,
        }).done(function (response) {
            $('#kvl-profiles-audit-modal').find(".modal-body").html(response);
            $('#kvl-profiles-audit-modal').modal('show');
        });
    }

    $('#kvl-profiles-audit-modal').on('shown.bs.modal', function (event) {
        $('#list_product_profile').attr('placeholder', 'Select Or Edit').editableSelect({
            effects: 'slide'
        }).on('select.editable-select', function (e, li) {
            $('#KeyVehicleLogProfile_Product').val(li.text());
        });

        const isNewEntry = $('#Id').val() === '0';
        $('#KeyVehicleLogProfile_VehicleRego').prop('disabled', !isNewEntry);

        $('#KeyVehicleLogProfile_VehicleRego, #KeyVehicleLogProfile_Trailer1Rego, #KeyVehicleLogProfile_Trailer2Rego, #KeyVehicleLogProfile_Trailer3Rego, #KeyVehicleLogProfile_Trailer4Rego').on('keyup', vehicleRegoToUpperCase);

        $('#KeyVehicleLogProfile_VehicleRego, #KeyVehicleLogProfile_Trailer1Rego, #KeyVehicleLogProfile_Trailer2Rego, #KeyVehicleLogProfile_Trailer3Rego, #KeyVehicleLogProfile_Trailer4Rego').on('keypress', vehicleRegoValidateSplChars);

        if ($('#KeyVehicleLogProfile_Product').val() !== '') {
            let itemToSelect = $('#list_product_profile').siblings('.es-list').find('li:contains("' + $('#KeyVehicleLogProfile_Product').val() + '")')[0];
            if (itemToSelect) {
                $('#list_product_profile').editableSelect('select', $(itemToSelect));
                $('#kvl-profiles-audit-modal').focus();
            }
        }

        if ($('#KeyVehicleLogProfile_PlateId').val() != '') {
            $('#kvl_profile_list_plates').val($('#KeyVehicleLogProfile_PlateId').val());
        }

        $('#kvl_profile_list_plates').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {
                $('#KeyVehicleLogProfile_PlateId').val(option.val());
            }
        });
    });

    /****** Kvl Fileds Type *******/
    let gridKvlFields;
    gridKvlFields = $('#tbl_kvl_fields').grid({
        dataSource: '/Admin/GuardSettings?handler=KeyVehcileLogFields',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { field: 'name', title: 'Name', width: 200, editor: true },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    let isKvlFieldAdding = false;
    $('#add_kvl_fields').on('click', function () {
        const selFieldTypeId = $('#kvl_fields_types').val();
        if (!selFieldTypeId) {
            alert('Please select a field type to update');
            return;
        }

        if (isKvlFieldAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isKvlFieldAdding = true;
            gridKvlFields.addRow({
                'id': -1,
                'typeId': selFieldTypeId,
                'name': '',
            }).edit(-1);
        }
    });

    $('#kvl_fields_types').on('change', function () {
        const selKvlFieldTypeId = $('#kvl_fields_types').val();
        gridKvlFields.clear();
        gridKvlFields.reload({ typeId: selKvlFieldTypeId });
    });

    if (gridKvlFields) {
        gridKvlFields.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            $.ajax({
                url: '/Admin/GuardSettings?handler=SaveKeyVehicleLogField',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) gridKvlFields.reload({ typeId: $('#kvl_fields_types').val() });
                else alert(result.message);
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isKvlFieldAdding)
                    isKvlFieldAdding = false;
            });
        });

        gridKvlFields.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this field?')) {
                $.ajax({
                    url: '/Admin/GuardSettings?handler=DeleteKeyVehicleLogField',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.success) gridKvlFields.reload({ typeId: $('#kvl_fields_types').val() });
                    else alert(result.message);
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isKvlFieldAdding)
                        isKvlFieldAdding = false;
                });
            }
        });
    }

    /****** Print manual docket *******/
    $('#vehicle_key_daily_log tbody').on('click', '#btnPrintVkl', function () {
        $('#generate_kvl_docket_status').hide();
        $('#download_kvl_docket').hide();
        $('.print-docket-reason').prop('checked', false);
        $('#otherReason').val('');
        $('#otherReason').attr('disabled', true);
        $('#stakeholderEmail').val('');

        var data = keyVehicleLog.row($(this).parents('tr')).data();
        $('#print-manual-docket-modal').modal('show')
        $('#printDocketForKvlId').val(data.detail.id);
    });

    $('.print-docket-reason').on('change', function () {
        $('#otherReason').val('');
        $('#otherReason').attr('disabled', true);
        $('#generate_kvl_docket_status').hide();
        $('#download_kvl_docket').hide();
        $('.print-docket-reason').prop('checked', false);

        $(this).prop('checked', true);
        if ($(this).val() === '0')
            $('#otherReason').attr('disabled', false);
    });

    $('#otherReason').attr('placeholder', 'Select Or Edit').editableSelect({
        effects: 'slide'
    }).on('select.editable-select', function (e, li) {
        $('#download_kvl_docket').hide();
    });

    $('#generate_kvl_docket').on('click', function () {
        $('#generate_kvl_docket_status').hide();

        const checkedReason = $('.print-docket-reason:checkbox:checked');
        if (checkedReason.length === 0) {
            $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger"></i> Please select a reason').show();
            return false;
        }
        $('#generate_kvl_docket_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Manual Docket. Please wait...').show();
        $('#download_kvl_docket').hide();
        $('#generate_kvl_docket').attr('disabled', true);
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=GenerateManualDocket',
            data: {
                id: $('#printDocketForKvlId').val(),
                option: $(checkedReason).val(),
                otherReason: $('#otherReason').val(),
                stakeholderEmails: $('#stakeholderEmail').val(),
                clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                blankNoteOnOrOff: $('#IsBlankNoteOn').val(),
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            if (response.statusCode === -1) {
                $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger mr-2"></i> Error generating report').show();
            }
            else {
                $('#generate_kvl_docket').attr('disabled', false);
                $('#download_kvl_docket').show();
                $('#download_kvl_docket').attr('href', response.fileName);

                let statusClass = 'fa-check-circle-o text-success mr-2';
                let statusMessage = 'A copy of the docket is on the Dropbox, and where applicable, has been emailed to relevant stakeholders';
                if (response.statusCode !== 0) {
                    statusClass = 'fa-exclamation-triangle text-warning mr-2';
                    statusMessage = 'Docket created successfully. But sending the email or uploading to Dropbox failed.';
                }
                $('#generate_kvl_docket_status').html('<i class="fa ' + statusClass + '"></i>' + statusMessage).show();
            }
        });
    });

    $('#btn_confirm_kvl_logbook_expiry').on('click', function () {
        $('#loader').show();
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=ResetClientSiteLogBook',
            type: 'POST',
            data: {
                clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                guardLoginId: $('#KeyVehicleLog_GuardLogin_Id').val()
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (!result.success)
                alert(result.message)
            else {
                $('#logbook-kvl-expiry-modal').modal('hide');
                window.location.reload();
            }
        }).always(function () {
            $('#loader').hide();
        });
    });

    $('#btn_duplicate_profile').on('click', function () {

        const personName = $('#driver_name').val();
        const selectedRow = gridKeyVehicleLogProfile.rows('.selected').data();

        if (personName === '') {
            $('#duplicate_profile_status').text('Person Name is required.');
            return;
        }

        if (selectedRow.length === 0) {
            $('#duplicate_profile_status').text('Please click one row to copy.');
            return;
        }

        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=DuplicateKeyVehicleLogProfile',
            type: 'POST',
            data: {
                id: selectedRow[0].detail.id,
                personName: personName
            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) {
                populateKvlModal(result.kvlProfileId);
            }
            else $('#duplicate_profile_status').text(result.message);
        });
    });

    function reloadGridKeyVehicleLogProfile() {
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=ProfileByRego&truckRego=' + $('#VehicleRego').val(),
            type: 'GET',
            dataType: 'json',
        }).done(function (result) {
            gridKeyVehicleLogProfile.clear().rows.add(result).draw();
            $('#driver_name').val('Unknown');
            $('#duplicate_profile_status').text('');
        });
    }
    $('#chb_IsBlankNote').on('change', function () {       
        const isChecked = $(this).is(':checked');
        $('#lbl_BlankNotes').text(isChecked ? 'Blank Notes On' : 'Blank Notes Off'); lbl_BlankNotes
        $('#IsBlankNoteOn').val(isChecked);
    });
});