﻿$(document).ready(function () {
    $(document).on('show.bs.modal', '.modal', function () {
        const zIndex = 1040 + 10 * $('.modal:visible').length;
        $(this).css('z-index', zIndex);
        setTimeout(() => $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack'));
    });

    $(document).on('hidden.bs.modal', '.modal', () => $('.modal:visible').length && $(document.body).addClass('modal-open'));
    $.ajax({
        url: '/Guard/KeyVehicleLog?handler=ANPRDetails',
        type: 'GET',
        data: {
            clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
        }
    }).done(function (response) {
        if (response != null) {
            const singleEntry = response.isSingleLane
            const SeperateEntry = response.isSeperateEntryAndExitLane
            if (singleEntry == true) {
                $("#ANPRDv").show();
                $("#ExitLaneDv").hide();

            }
            else if (SeperateEntry == true) {
                $("#ANPRDv").show();
            }
        }
        
           
    });

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
            '<th colspan="4" style="background-color:#EAF0ED"><center>Trailers Rego or ISO</center></th>' +
            '<th colspan="3" style="background-color:#EAF0ED"><center>Individual</center></th>' +
            '<th rowspan="2" style="background-color:#EAF0ED"><center>Site POC</center></th>' +
            '<th rowspan="2" style="background-color:#EAF0ED"><center>Site Location</center></th>' +
            '<th rowspan="2" style="background-color:#EAF0ED"><center>Purpose Of Entry</center></th>' +
            '<th colspan="3" style="background-color:#EAF0ED"><center>Weight</center></th>' +
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

    function vehicleRegoToUpperCase(e) {
        if (e.which != 35 && e.which == 32 || e.which == 45 && e.which < 48 ||
            (e.which > 57 && e.which < 65) ||
            (e.which > 90 && e.which < 97) ||
            e.which > 122) {
            $(this).val($(this).val().replace(/[^a-z0-9]/gi, ''));
        }
        else {
            let regoToUpper = $(this).val().toUpperCase();
            $(this).val(regoToUpper);
        }

    }

    function vehicleRegoValidateSplChars(e) {
        //  blocking special charactors
        if (e.which != 35 && e.which == 32 || e.which == 45 && e.which < 48 ||
            (e.which > 57 && e.which < 65) ||
            (e.which > 90 && e.which < 97) ||
            e.which > 122) {
            e.preventDefault();
        }

    }

    let keyVehicleLog;
    var displayedrecordsid = [];

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
            columns: [
                { data: 'detail.id', visible: false },
                {
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
                /* for searching site location-start*/
                { data: 'clientSiteLocationName', visible: false },
                /* for searching site location-end*/
                /* for searching site poc-start*/
                { data: 'clientSitePocName', visible: false },
                /* for searching site poc-end*/

                {
                    targets: -1,
                    data: 'detail.id',
                    width: '12%',
                    defaultContent: '',

                    render: function (value, type, data) {
                        return '<button id="btnEditVkl" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
                            '<button id="btnPrintVkl" class="btn btn-outline-primary mr-1 "><i class="fa fa-print"></i></button>' +
                            '<button id="btnDeleteVkl" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash"></i></button>' +
                            '<div class="custom-control custom-switch custom-control-inline ml-2 mt-1"  title="Toggle Print Docket">' +
                            '<input type="checkbox" class="custom-control-input" id="' + value + '" name="toggleDarkMode">' +
                            '<label class="custom-control-label" for="' + value + '"></label>' +
                            '</div>'


                            ;
                        // if (value === null) return 'N/A';
                        // return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardLogBookInfoModal" id="btnLogBookDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + value + '"><input type="hidden" id="GuardId" value="' + value + '">' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" text="' + value + '"><input type="hidden" id="GuardId" text="' + value + '"> ';
                    }
                    //render: function (data, type, row) {
                    //    return '<input type="checkbox" id=' + data.detail.keyNo+'/>';
                    //}
                    //defaultContent: '<button id="btnEditVkl" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
                    //   '<button id="btnPrintVkl" class="btn btn-outline-primary mr-1 "><i class="fa fa-print"></i></button>' +
                    //    '<button id="btnDeleteVkl" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i>Delete</button>',
                }],
            'createdRow': function (row, data, index) {
                if (data.detail.initialCallTime !== null) {
                    $('td', row).eq(1).html(convertDateTimeString(data.detail.initialCallTime));
                    /*to display the color yellow-start*/
                    if (data.detail.entryTime == null && data.detail.sentInTime == null && data.detail.exitTime == null) {
                        $('td', row).eq(1).addClass('initial-call-colour');
                        $('td', row).eq(2).addClass('initial-call-colour');
                        $('td', row).eq(3).addClass('initial-call-colour');
                        $('td', row).eq(4).addClass('initial-call-colour');
                        $('td', row).eq(5).addClass('initial-call-colour');
                        $('td', row).eq(6).addClass('initial-call-colour');
                        $('td', row).eq(7).addClass('initial-call-colour');
                        $('td', row).eq(8).addClass('initial-call-colour');
                        $('td', row).eq(9).addClass('initial-call-colour');
                        $('td', row).eq(10).addClass('initial-call-colour');
                        $('td', row).eq(11).addClass('initial-call-colour');
                    }
                    /*to display the color yellow-end*/
                }
                if (data.detail.entryTime !== null) {
                    $('td', row).eq(2).html(convertDateTimeString(data.detail.entryTime));
                    /*to display the color green for entry time-start*/
                    if (data.detail.sentInTime == null && data.detail.exitTime == null) {
                        $('td', row).eq(1).addClass('entry-time-colour');
                        $('td', row).eq(2).addClass('entry-time-colour');
                        $('td', row).eq(3).addClass('entry-time-colour');
                        $('td', row).eq(4).addClass('entry-time-colour');
                        $('td', row).eq(5).addClass('entry-time-colour');
                        $('td', row).eq(6).addClass('entry-time-colour');
                        $('td', row).eq(7).addClass('entry-time-colour');
                        $('td', row).eq(8).addClass('entry-time-colour');
                        $('td', row).eq(9).addClass('entry-time-colour');
                        $('td', row).eq(10).addClass('entry-time-colour');
                        $('td', row).eq(11).addClass('entry-time-colour');
                    }
                    /*to display the color green for entry time-end*/
                }
                if (data.detail.sentInTime !== null) {
                    $('td', row).eq(3).html(convertDateTimeString(data.detail.sentInTime));
                    /*to display the color green for sent in time-start*/
                    if (data.detail.exitTime == null) {
                        $('td', row).eq(1).addClass('entry-time-colour');
                        $('td', row).eq(2).addClass('entry-time-colour');
                        $('td', row).eq(3).addClass('entry-time-colour');
                        $('td', row).eq(4).addClass('entry-time-colour');
                        $('td', row).eq(5).addClass('entry-time-colour');
                        $('td', row).eq(6).addClass('entry-time-colour');
                        $('td', row).eq(7).addClass('entry-time-colour');
                        $('td', row).eq(8).addClass('entry-time-colour');
                        $('td', row).eq(9).addClass('entry-time-colour');
                        $('td', row).eq(10).addClass('entry-time-colour');
                        $('td', row).eq(11).addClass('entry-time-colour');
                    }
                    /*to display the color green for sent in time-end*/
                }
                if (data.detail.exitTime !== null) {
                    $('td', row).eq(4).html(convertDateTimeString(data.detail.exitTime));
                    /*to display the color green for exit  time-start*/

                    $('td', row).eq(1).addClass('exit-time-colour');
                    $('td', row).eq(2).addClass('exit-time-colour');
                    $('td', row).eq(3).addClass('exit-time-colour');
                    $('td', row).eq(4).addClass('exit-time-colour');
                    $('td', row).eq(5).addClass('exit-time-colour');
                    $('td', row).eq(6).addClass('exit-time-colour');
                    $('td', row).eq(7).addClass('exit-time-colour');
                    $('td', row).eq(8).addClass('exit-time-colour');
                    $('td', row).eq(9).addClass('exit-time-colour');
                    $('td', row).eq(10).addClass('exit-time-colour');
                    $('td', row).eq(11).addClass('exit-time-colour');

                    /*to display the color green for exit  time-end*/
                }
                if (data.detail.exitTime == null) {
                    $('td', row).eq(4).html('<button type="button" class="btn btn-success btn-exit-quick">E</button> ');
                }
            },
            'drawCallback': function (settings) {               
                let api = this.api();
                //let data = api.row().data();
                var cnt = api.rows({ search: 'applied' }).count();
                $('#total_events').html(cnt);
               // alert(data);
               // console.log(data);
                displayedrecordsid = [];
                let dispdata = api.rows({ search: 'applied' }).data();
                dispdata.each(function (row) {                    
                    displayedrecordsid.push(parseInt(row.detail.id));
                });
            }
        });

        var dataTable = $('#vehicle_key_daily_log').DataTable();
        var ids = [];
        dataTable.rows().data().each(function (index, rowData) {

            ids.push(index.detail.id);
        });
    }

    $('#btn_kvl_pdf_generate').on('click', function () {        
        if (displayedrecordsid.length === 0) {
            alert('No records available for download');
            return;
        }

        // Build the URL with URL-encoded parameters
        var params = new URLSearchParams({
            logbookid: parseInt($('#KeyVehicleLog_ClientSiteLogBookId').val()),
            recordids: displayedrecordsid.join(',')
        });
        var url = 'SiteLogPdf?handler=GenerateFilteredDataPdf&' + params.toString();

        
        // Send AJAX request to get the PDF blob
        $.ajax({
            url: url,
            method: 'GET',
            xhrFields: {
                responseType: 'blob' // Important for handling binary data
            },
            beforeSend: function () {
                $('#loader').show();
            }, 
            success: function (blob, textStatus, request) {

                var contentDispositionHeader = request.getResponseHeader('Content-Disposition');
                var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                var matches = filenameRegex.exec(contentDispositionHeader);
                var downloadedFileName = matches !== null && matches[1] ? matches[1].replace(/['"]/g, '') : fileName;

                const URL = window.URL || window.webkitURL;
                const displayNameHash = encodeURIComponent(`#displayName=${downloadedFileName}`);
                const bloburl = URL.createObjectURL(blob);
                const objectUrl = URL.createObjectURL(blob) + displayNameHash;
                const windowUrl = window.location.origin; // + window.location.pathname;
                const viewerUrl = `${windowUrl}/lib/Pdfjs/web/viewer.html?file=`;
                var newTab = window.open(`${viewerUrl}${objectUrl}`);
                if (!newTab) {
                    // If the new tab was blocked, fallback to downloading the file
                    var a = document.createElement('a');
                    a.href = bloburl;
                    a.download = downloadedFileName;
                    a.click();
                }
                URL.revokeObjectURL(bloburl);
                URL.revokeObjectURL(objectUrl);
            },
            error: function (xhr, status, error) {
                if (newTab) {
                    newTab.close(); // Close the tab if there is an error
                } 
                console.error("Failed to generate PDF: ", error);
            },
            complete: function () {
                $('#loader').hide();
            }
        });        

    });

    $('#vehicle_key_daily_log').on('change', '.checkbox', function () {
        // Get the row data associated with the checkbox
        var rowData = table.row($(this).closest('tr')).data();

        // Get the checkbox ID
        var checkboxId = $(this).attr('id');

        // Check if the checkbox is checked or unchecked
        if ($(this).prop('checked')) {
            console.log('Checkbox with ID ' + checkboxId + ' in row ' + rowData[0] + ' checked.');
            // Perform actions when checkbox is checked
        } else {
            console.log('Checkbox with ID ' + checkboxId + ' in row ' + rowData[0] + ' unchecked.');
            // Perform actions when checkbox is unchecked
        }
    });


    $('#search_kvl_log').on('keyup', function () {
        keyVehicleLog.search($(this).val()).draw();
        $("#KeyVehicleLog_ClientSiteLocationId").find('option:selected').prop("selected", false);
        $("#KeyVehicleLog_ClientSitePocId").find('option:selected').prop("selected", false);
    });
    //to search through checkbox-start
    $('#KeyVehicleLog_ClientSiteLocationId').on('change', function () {
        var item = $(this).find('option:selected').text();
        if (item.toLocaleLowerCase() == 'select')
            item = '';
        keyVehicleLog.search(item).draw();
        $("#KeyVehicleLog_ClientSitePocId").find('option:selected').prop("selected", false);
    });
    $('#KeyVehicleLog_ClientSitePocId').on('change', function () {
        var item = $(this).find('option:selected').text();
        if (item.toLocaleLowerCase() == 'select')
            item = '';
        keyVehicleLog.search(item).draw();
        $("#KeyVehicleLog_ClientSiteLocationId").find('option:selected').prop("selected", false);
    });
    //to search through checkbox-end
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
        /*$('#btn_kvl_pdf_download').attr('href', 'SiteLogPdf?id=' + $('#KeyVehicleLog_ClientSiteLogBookId').val() + '&t=vl&f=' + kvlStatusFilter);*/

        keyVehicleLog.ajax.reload();
    });

    $('#vkl-modal').on('shown.bs.modal', function (event) {
        const params = $(event.relatedTarget);
        bindKvlPopupEvents(!params[0].isNewEntry);
        if ($('#VehicleRego').val() != '') {
            GetVehicleImage();
        }
        GetPersonImage();
    });

    $('#add_new_vehicle_and_key_log,#add_new_vehicle_and_key_log_one').on('click', function () {
        loadVklPopup(0, true);
    });

    $('#vehicle_key_daily_log tbody').on('click', '#btnEditVkl', function () {
        var data = keyVehicleLog.row($(this).parents('tr')).data();
        loadVklPopup(data.detail.id,false);

    });

    $('#vehicle_key_daily_log tbody').on('click', '.btn-exit-quick', function () {
        var data = keyVehicleLog.row($(this).parents('tr')).data();
        var tmdata = {
            'EventDateTimeLocal': null,
            'EventDateTimeLocalWithOffset': null,
            'EventDateTimeZone': null,
            'EventDateTimeZoneShort': null,
            'EventDateTimeUtcOffsetMinute': null,
        };
        fillRefreshLocalTimeZoneDetails(tmdata, "", false)
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=KeyVehicleLogQuickExit',
            type: 'POST',
            data: {
                id: data.detail.id,
                tmdata: tmdata
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
    /*for deselecting the common checkbox-start*/
    $('#vehicle_key_daily_log tbody').on('change', 'input[type=checkbox]', function () {
        var isChecked = $(this).is(':checked');

        if (isChecked == false) {
            $('#chkAllBatchDocketSelect').prop('checked', false);
        }
    });
    /*for deselecting the common checkbox-end*/
    let isKeyAllocatedModal;
    let isVehicleOnsiteModal
    let isVehicleInAnotherSiteModal;
    let isKeyAllocatedModalDesc;
    let isPOIOnsiteModal;
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

        isKeyAllocatedModalDesc = new ConfirmationModal('key-alloc', {
            message: 'This key was already allocated and there is no exit time recorded.<br/><br/>Are you sure you want to create a new entry when it appears they are already allocated?',
            onYes: function () { selectKey2() }
        });
        isPOIOnsiteModal = new ConfirmationModal('poi-onsite', {
            message: 'You are about to add a new POI entry to database with no name.This is ok, as C4i System will assign a system generated name. Please confirm you want to continue',
            onYes: function () { GetPOINumber(); }
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
                    trailer1Rego: $('#Trailer1Rego').val(),
                    trailer2Rego: $('#Trailer2Rego').val(),
                    trailer3Rego: $('#Trailer3Rego').val(),
                    trailer4Rego: $('#Trailer4Rego').val(),
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
        var SitePOCIds = $('#ClientSitePocId').val();
        //$('#ClientSitePocId').val(SitePOCIds);
        $('#validation-summary ul').html('');

        const mobileNo = $('#MobileNumber').val();
        if (mobileNo === '+61 (0) ')
            $('#MobileNumber').val('');

        const today = new Date();
        /*To get the text inside the product dropdown*/
        var inputElement = document.querySelector(".es-input");
        // Get the value of the input element
        if (inputElement) { var inputValue = inputElement.value; $('#ProductOther').val(inputValue); }

        if ($('#new_log_initial_call').val() !== '')
            $('#InitialCallTime').val(parseDateInKvlEntryFormat(today, $('#new_log_initial_call').val()));

        if ($('#new_log_entry_time').val() !== '')
            $('#EntryTime').val(parseDateInKvlEntryFormat(today, $('#new_log_entry_time').val()));

        if ($('#new_log_sent_in_time').val() !== '')
            $('#SentInTime').val(parseDateInKvlEntryFormat(today, $('#new_log_sent_in_time').val()));

        if ($('#new_log_exit_time').val() !== '')
            $('#ExitTime').val(parseDateInKvlEntryFormat(today, $('#new_log_exit_time').val()));


        // Task p6#73_TimeZone issue -- added by Binoy -- Start
        var form = document.getElementById('form_new_vehicle_and_key_log');
        var jsformData = new FormData(form);
   
        fillRefreshLocalTimeZoneDetails(jsformData, "", true);
        // Task p6#73_TimeZone issue -- added by Binoy -- End
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=SaveKeyVehicleLog',
            type: 'POST',
            data: jsformData,
            processData: false,
            contentType: false,
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
                    url: '/Guard/KeyVehicleLog?handler=ClientSiteDetails',
                    type: 'GET',
                    data: {
                        keyId: keyId,
                        clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                    }
                }).done(function (response) {
                    const imageHtml = response.imagePath
                        ? '<a href="' + response.imagePath + '" target="_blank"><img src="' + response.imagePath + '" alt="Image" style="width: 24px; height: 24px;" /></a>'
                        : '';
                    const rowHtml = '<tr>' +
                        '<td>' + keyNo + '</td>' +
                        '<td>' + response.description + '</td>' +
                        '<td>' + imageHtml + '</td>' +
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


    function selectKey2() {
        const option = $('#list_clientsite_keystext').find(":selected");
        if (option.val() === '')
            return;

        const keyNo = option.text();
        const keyId = option.val();
        if (keyId !== '') {
            const currentKeyNos = $('#KeyNo').val();

            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=ClientSiteDetails',
                type: 'GET',
                data: {
                    keyId: keyId,
                    clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                }
            }).done(function (response) {
                if (!currentKeyNos.includes(response.keyNo)) {
                    const imageHtml = response.imagePath
                        ? '<a href="' + response.imagePath + '" target="_blank"><img src="' + response.imagePath + '" alt="Image" style="width: 24px; height: 24px;" /></a>'
                        : '';
                    const rowHtml = '<tr>' +
                        '<td>' + response.keyNo + '</td>' +
                        '<td>' + keyNo + '</td>' +
                        '<td>' + imageHtml + '</td>' +
                        '<td><i class="fa fa-trash-o text-danger btn-delete-kvl-Key" title="Delete" style="cursor: pointer;"></i></td>' +
                        '</tr>';
                    $("#kvl-keys-list").append(rowHtml);
                    if (currentKeyNos === '') $('#KeyNo').val(response.keyNo);
                    else $('#KeyNo').val(currentKeyNos + '; ' + response.keyNo);
                }
            });

            $("#list_clientsite_keystext").val('').trigger('change');
        }
    }

    //$('#kv_duress_btn').on('click', function () {
    //    $.ajax({
    //        url: '/Guard/KeyVehicleLog?handler=SaveClientSiteDuress',
    //        data: {
    //            clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
    //            GuardId: $('#KeyVehicleLog_GuardLogin_GuardId').val(),
    //            guardLoginId: $('#KeyVehicleLog_GuardLogin_Id').val(),
    //            logBookId: $('#KeyVehicleLog_ClientSiteLogBookId').val()
    //        },
    //        type: 'POST',
    //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //    }).done(function (result) {
    //        if (result.status) {
    //            $('#kv_duress_btn').removeClass('normal').addClass('active');
    //            $("#kv_duress_status").addClass('font-weight-bold');
    //            $("#kv_duress_status").text("Active");
    //        }
    //    });
    //});

    //var ret = staffDocsButtonRenderer;
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
            //{ data: 'detail.poiImageDisplay' },


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

        GetVehicleImage()


    });
    //ANPR Task 4 start
    $("#iconClick").on("click", function () {
        var item = $("#entryLane").val();
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=ProfileByRego&truckRego=' + item,
            type: 'GET',
            dataType: 'json',
        }).done(function (result) {
            if (result.length > 0) {
                var firstItem = result["0"];
                var detailId = firstItem.detail.id;


                if (detailId) {
                    populateKvlModal(detailId);
                    loadVklPopup(0, true);
                    //GetVehicleImage()

                } else {
                    console.log("No data found in the first row.");
                }
            }
        });
    });
     //ANPT Task 4 stop
    /*to get the vehicle image-start*/


    function GetVehicleImage() {
        const fileForm = new FormData();
        fileForm.append('vehicle_rego', $('#VehicleRego').val());

        $.ajax({
            url: '/Guard/KeyVehiclelog?handler=VehicleImageUpload',
            type: 'GET',
            data: { 'VehicleRego': $('#VehicleRego').val() },

            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {

            if (result.success) {
                $('#img_VehicleId').attr('src', result.fulePath);
                $('#imagevehicledisplay').attr('href', result.fulePath);
                $('#img_VehicleId').prop('hidden', false);
                $('#head_VehicleId').prop('hidden', true);
                $('#btn-delete-VehicleImage').prop('hidden', false)
            } else {
                $('#head_VehicleId').prop('hidden', false);
                $('#img_VehicleId').prop('hidden', true);
                $('#btn-delete-VehicleImage').prop('hidden', true)
            }

        });

    }

    /*to get the vehicle image-end*/

    /*to get the person image-start*/

    function GetPersonImage() {
        const fileForm = new FormData();
        fileForm.append('vehicle_rego', $('#VehicleRego').val());
        //var name = document.createTextNode($('#CompanyName').val()  + $('#PersonType').val()  + $('#PersonName').val());
        ;
        $.ajax({
            url: '/Guard/KeyVehiclelog?handler=PersonImageUpload',
            type: 'GET',
            data: { 'CompanyName': $('#CompanyName').val(), 'PersonType': $('#PersonType').find('option:selected').text(), 'PersonName': $('#PersonName').val() },

            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {

            if (result.success) {

                $('#imagepersondisplay').attr('href', result.fulePath);
                $('#img_PersonId').attr('src', result.fulePath);
                $
                $('#img_PersonId').prop('hidden', false);
                $('#head_PersonId').prop('hidden', true);
                $('#btn-delete-PersonImage').prop('hidden', false)
            } else {
                $('#head_PersonId').prop('hidden', false);
                $('#img_PersonId').prop('hidden', true);
                $('#btn-delete-PersonImage').prop('hidden', true)
            }

        });

    }

    /*to get the person image-end*/

    function populateKvlModal(id) {

        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=ProfileById&id=' + id,
            type: 'GET',
            dataType: 'json',
        }).done(function (result) {
            let personName = result.personName ? result.personName : 'Unknown';


            var check1 = $('#PlateId').val();
            var check = result.keyVehicleLogProfile.plateId;
            if (check1 != 0 && check1 != '') {

            }
            else {

                if (check != 0) {
                    $('#PlateId').val(result.keyVehicleLogProfile.plateId);
                }

            }
           /* if ($('#VehicleRego').val() === '') {*/
                $('#VehicleRego').val(result.keyVehicleLogProfile.vehicleRego);
            /*}*/

           /* if (!$('#kvl_list_plates').val()) {*/
                $('#kvl_list_plates').val(result.keyVehicleLogProfile.plateId);
           /* }*/
           /* if (!$('#TruckConfig').val()) {*/
                $('#TruckConfig').val(result.keyVehicleLogProfile.truckConfig);
           /* }*/
           /* if (!$('#TrailerType').val()) {*/
                $('#TrailerType').val(result.keyVehicleLogProfile.trailerType);
           /* }*/
            /*if (!$('#MaxWeight').val()) {*/
                $('#MaxWeight').val(result.keyVehicleLogProfile.maxWeight);
            /*}*/
            if (!$('#Trailer1Rego').val()) {
                // $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer1Rego);
            }
            if (!$('#Trailer2Rego').val()) {
                // $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer2Rego);
            }
            if (!$('#Trailer3Rego').val()) {
                // $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer3Rego);
            }
            if (!$('#Trailer4Rego').val()) {
                //$('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer4Rego);

            }
           /* if (!$('#CompanyName').val()) {*/
                $('#CompanyName').val(result.companyName);
           /* }*/
            /*if (!$('#PersonName').val() || $('#PersonName').val() ==='Unknown') {*/
                $('#PersonName').val(personName);
            /*}*/
           /* if (!$('#PersonType').val()) {*/
                $('#PersonType').val(result.personType);
            /*}*/
          
            /*if (!$('#MobileNumber').val() || $('#MobileNumber').val() === '+61 (0) ') {*/
                $('#MobileNumber').val(result.keyVehicleLogProfile.mobileNumber);
           /* }*/
          /*  if (!$('#EntryReason').val()) {*/
                $('#EntryReason').val(result.keyVehicleLogProfile.entryReason);
           /* }*/
            /*if (!$('#Product').val()) {*/
                $('#Product').val(result.keyVehicleLogProfile.product);
            /*}*/
            /*if (!$('#Notes').val()) {*/
                $('#Notes').val(result.keyVehicleLogProfile.notes);
           /* }*/
            //=========================================
            $("#list_product").val(result.keyVehicleLogProfile.product);
            $("#list_product").trigger('change');
            $('#Sender').val(result.sender);
            $('#lblIsSender').text(result.isSender ? 'Sender Address' : 'Reciever Address');
            $('#cbIsSender').prop('checked', result.isSender);
            //for checking whether the person is under scam or not(jisha james)
            $('#PersonOfInterest').val(result.personOfInterest)
            if ($('#PersonOfInterest').val() != '') {
                $('#titlePOIWarning').attr('hidden', false);
                $('#imagesiren').attr('hidden', false);


            }
            else {
                $('#titlePOIWarning').attr('hidden', true);
                $('#imagesiren').attr('hidden', true);
            }
            /*to load the common fields to crmtab -start*/
            $('#crm_list_plates').val(result.keyVehicleLogProfile.plateId);
            $('#crmTruckConfig').val(result.keyVehicleLogProfile.truckConfig);
            $('#crmTrailerType').val(result.keyVehicleLogProfile.trailerType);

            $('#IndividualTitle').val(result.individualTitle);
            $('#Gender').val(result.gender);
            $('#crmCompanyABN').val(result.companyABN);
            if (result.companyLandline == null)
                $('#LandLineNumber').val('+61 (0) ');
            else
                $('#LandLineNumber').val(result.companyLandline);
            $('#Email').val(result.email);
            $('#Website').val(result.website);
            

            /*for cheking  the BDM is true-start*/
            let isBDM = $('#IsBDM').val(result.isBDM);
            $('#cbIsBDMOrSales').prop('checked', result.isBDM);

            if (result.isBDM == true) {
                $('#lblIsBDMOrSales').text('BDM/Sales');
                $('#list_BDM').prop('hidden', false);
            }
            else {
                $('#lblIsBDMOrSales').text('Supplier/Partner');
                $('#list_BDM').prop('hidden', false);
            }
            


            /*for cheking  the BDM is true-end*/
            $('#IsCRMId').val(result.bdmList);
            var checkedornot = $('#IsCRMId').val();

            /* to load the selected items in BDM-start*/
            $("#list_BDM  input[type=checkbox]").each(function () {
                crmindivid = $(this).closest('li').find('#IsCRMIndividualId').val();
                if (checkedornot.indexOf(crmindivid) != -1) {
                    $(this).prop('checked', true);
                }
                else {
                    $(this).prop('checked', false);
                }

            });
            /*to load the plate to crmtab -end*/

            const isChecked = $('#chbIsPOIAlert1').is(':checked');
            if (result.keyVehicleLogProfile.vehicleRego != null) {
                loadAuditHistory(result.keyVehicleLogProfile.vehicleRego);

            }
            else {
                loadAuditHistoryWithProfileId(result.keyVehicleLogProfile.id);
            }

            GetPersonImage()


            /* For attachements Start  */
            $("#kvl-attachment-list").empty();
            for (var attachIndex = 0; attachIndex < result.attachments.length; attachIndex++) {
                const file = result.attachments[attachIndex];
                const attachment_id = 'attach_' + attachIndex;
                const li = document.createElement('li');
                li.id = attachment_id;
                li.className = 'list-group-item';
                li.dataset.index = attachIndex;
                let liText = document.createTextNode(file);
                const icon = document.createElement("i");
                icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                icon.title = 'Delete';
                icon.style = 'cursor:pointer';
                li.appendChild(liText);
                li.appendChild(icon);
                const anchorTag = document.createElement("a");
                anchorTag.href = '/KvlUploads/' + $('#VehicleRego').val() + "/" + file;
                anchorTag.target = "_blank";
                const icon2 = document.createElement("i");
                icon2.className = 'fa fa-download ml-2 text-primary';
                icon2.title = 'Download';
                icon2.style = 'cursor:pointer';
                anchorTag.appendChild(icon2);
                li.appendChild(anchorTag);
                document.getElementById('kvl-attachment-list').append(li);



            }

            $('#kvl_attachments_count').html(result.attachments.length);
            /* For attachements end  */
            //traliler changes New change for Add rigo without plate number 21032024 dileep Start
            if (!$('#Trailer1PlateId').val()) {
                // $('#Trailer1PlateId').val(result.keyVehicleLogProfile.trailer1PlateId);
            }
            if (!$('#Trailer1Rego_Vehicle_type').val()) {
                //$('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
            }
            if (!$('#Trailer2PlateId').val()) {
                //$('#Trailer2PlateId').val(result.keyVehicleLogProfile.trailer2PlateId);
            }
            if (!$('#Trailer2Rego_Vehicle_type').val()) {
                // $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
            }
            if (!$('#Trailer3PlateId').val()) {
                //$('#Trailer3PlateId').val(result.keyVehicleLogProfile.trailer3PlateId);
            }
            if (!$('#Trailer3Rego_Vehicle_type').val()) {
                // $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
            }
            if (!$('#Trailer4PlateId').val()) {
                // $('#Trailer4PlateId').val(result.keyVehicleLogProfile.trailer4PlateId);
            }
            if (!$('#Trailer4Rego_Vehicle_type').val()) {
                //$('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
            }
            if ($('#Trailer1Rego').val() !== '') {
                $('#Trailer1Rego_Vehicle_type').attr('disabled', false);
            }
            if ($('#Trailer2Rego').val() !== '') {
                $('#Trailer2Rego_Vehicle_type').attr('disabled', false);
            }
            if ($('#Trailer3Rego').val() !== '') {
                $('#Trailer3Rego_Vehicle_type').attr('disabled', false);
            }
            if ($('#Trailer4Rego').val() !== '') {
                $('#Trailer4Rego_Vehicle_type').attr('disabled', false);
            }
            //traliler changes New change for Add rigo without plate number 21032024 dileep end


        });
        $('#kvl-profiles-modal').modal('hide');

        $('#kvl-trailer_profiles-modal').modal('hide');

    }

        function populateKvlModalANPR(id) {

            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=ProfileById&id=' + id,
                type: 'GET',
                dataType: 'json',
            }).done(function (result) {
                let personName = result.personName ? result.personName : 'Unknown';


                var check1 = $('#PlateId').val();
                var check = result.keyVehicleLogProfile.plateId;
                if (check1 != 0 && check1 != '') {

                }
                else {

                    if (check != 0) {
                        $('#PlateId').val(result.keyVehicleLogProfile.plateId);
                    }

                }
                /* if ($('#VehicleRego').val() === '') {*/
                $('#VehicleRego').val(result.keyVehicleLogProfile.vehicleRego);
                /*}*/

                /* if (!$('#kvl_list_plates').val()) {*/
                $('#kvl_list_plates').val(result.keyVehicleLogProfile.plateId);
                /* }*/
                /* if (!$('#TruckConfig').val()) {*/
                $('#TruckConfig').val(result.keyVehicleLogProfile.truckConfig);
                /* }*/
                /* if (!$('#TrailerType').val()) {*/
                $('#TrailerType').val(result.keyVehicleLogProfile.trailerType);
                /* }*/
                /*if (!$('#MaxWeight').val()) {*/
                $('#MaxWeight').val(result.keyVehicleLogProfile.maxWeight);
                /*}*/
                if (!$('#Trailer1Rego').val()) {
                    // $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                }
                if (!$('#Trailer2Rego').val()) {
                    // $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                }
                if (!$('#Trailer3Rego').val()) {
                    // $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                }
                if (!$('#Trailer4Rego').val()) {
                    //$('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer4Rego);

                }
                /* if (!$('#CompanyName').val()) {*/
                $('#CompanyName').val(result.companyName);
                /* }*/
                /*if (!$('#PersonName').val() || $('#PersonName').val() ==='Unknown') {*/
                $('#PersonName').val(personName);
                /*}*/
                /* if (!$('#PersonType').val()) {*/
                $('#PersonType').val(result.personType);
                /*}*/

                /*if (!$('#MobileNumber').val() || $('#MobileNumber').val() === '+61 (0) ') {*/
                $('#MobileNumber').val(result.keyVehicleLogProfile.mobileNumber);
                /* }*/
                /*  if (!$('#EntryReason').val()) {*/
                $('#EntryReason').val(result.keyVehicleLogProfile.entryReason);
                /* }*/
                /*if (!$('#Product').val()) {*/
                $('#Product').val(result.keyVehicleLogProfile.product);
                /*}*/
                /*if (!$('#Notes').val()) {*/
                $('#Notes').val(result.keyVehicleLogProfile.notes);
                /* }*/
                //=========================================
                $("#list_product").val(result.keyVehicleLogProfile.product);
                $("#list_product").trigger('change');
                $('#Sender').val(result.sender);
                $('#lblIsSender').text(result.isSender ? 'Sender Address' : 'Reciever Address');
                $('#cbIsSender').prop('checked', result.isSender);
                //for checking whether the person is under scam or not(jisha james)
                $('#PersonOfInterest').val(result.personOfInterest)
                if ($('#PersonOfInterest').val() != '') {
                    $('#titlePOIWarning').attr('hidden', false);
                    $('#imagesiren').attr('hidden', false);


                }
                else {
                    $('#titlePOIWarning').attr('hidden', true);
                    $('#imagesiren').attr('hidden', true);
                }
                /*to load the common fields to crmtab -start*/
                $('#crm_list_plates').val(result.keyVehicleLogProfile.plateId);
                $('#crmTruckConfig').val(result.keyVehicleLogProfile.truckConfig);
                $('#crmTrailerType').val(result.keyVehicleLogProfile.trailerType);

                $('#IndividualTitle').val(result.individualTitle);
                $('#Gender').val(result.gender);
                $('#crmCompanyABN').val(result.companyABN);
                if (result.companyLandline == null)
                    $('#LandLineNumber').val('+61 (0) ');
                else
                    $('#LandLineNumber').val(result.companyLandline);
                $('#Email').val(result.email);
                $('#Website').val(result.website);


                /*for cheking  the BDM is true-start*/
                let isBDM = $('#IsBDM').val(result.isBDM);
                $('#cbIsBDMOrSales').prop('checked', result.isBDM);

                if (result.isBDM == true) {
                    $('#lblIsBDMOrSales').text('BDM/Sales');
                    $('#list_BDM').prop('hidden', false);
                }
                else {
                    $('#lblIsBDMOrSales').text('Supplier/Partner');
                    $('#list_BDM').prop('hidden', false);
                }



                /*for cheking  the BDM is true-end*/
                $('#IsCRMId').val(result.bdmList);
                var checkedornot = $('#IsCRMId').val();

                /* to load the selected items in BDM-start*/
                $("#list_BDM  input[type=checkbox]").each(function () {
                    crmindivid = $(this).closest('li').find('#IsCRMIndividualId').val();
                    if (checkedornot.indexOf(crmindivid) != -1) {
                        $(this).prop('checked', true);
                    }
                    else {
                        $(this).prop('checked', false);
                    }

                });
                /*to load the plate to crmtab -end*/

           

                GetPersonImage()


                /* For attachements Start  */
                $("#kvl-attachment-list").empty();
                for (var attachIndex = 0; attachIndex < result.attachments.length; attachIndex++) {
                    const file = result.attachments[attachIndex];
                    const attachment_id = 'attach_' + attachIndex;
                    const li = document.createElement('li');
                    li.id = attachment_id;
                    li.className = 'list-group-item';
                    li.dataset.index = attachIndex;
                    let liText = document.createTextNode(file);
                    const icon = document.createElement("i");
                    icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                    icon.title = 'Delete';
                    icon.style = 'cursor:pointer';
                    li.appendChild(liText);
                    li.appendChild(icon);
                    const anchorTag = document.createElement("a");
                    anchorTag.href = '/KvlUploads/' + $('#VehicleRego').val() + "/" + file;
                    anchorTag.target = "_blank";
                    const icon2 = document.createElement("i");
                    icon2.className = 'fa fa-download ml-2 text-primary';
                    icon2.title = 'Download';
                    icon2.style = 'cursor:pointer';
                    anchorTag.appendChild(icon2);
                    li.appendChild(anchorTag);
                    document.getElementById('kvl-attachment-list').append(li);



                }

                $('#kvl_attachments_count').html(result.attachments.length);
                /* For attachements end  */
                //traliler changes New change for Add rigo without plate number 21032024 dileep Start
                if (!$('#Trailer1PlateId').val()) {
                    // $('#Trailer1PlateId').val(result.keyVehicleLogProfile.trailer1PlateId);
                }
                if (!$('#Trailer1Rego_Vehicle_type').val()) {
                    //$('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                }
                if (!$('#Trailer2PlateId').val()) {
                    //$('#Trailer2PlateId').val(result.keyVehicleLogProfile.trailer2PlateId);
                }
                if (!$('#Trailer2Rego_Vehicle_type').val()) {
                    // $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                }
                if (!$('#Trailer3PlateId').val()) {
                    //$('#Trailer3PlateId').val(result.keyVehicleLogProfile.trailer3PlateId);
                }
                if (!$('#Trailer3Rego_Vehicle_type').val()) {
                    // $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                }
                if (!$('#Trailer4PlateId').val()) {
                    // $('#Trailer4PlateId').val(result.keyVehicleLogProfile.trailer4PlateId);
                }
                if (!$('#Trailer4Rego_Vehicle_type').val()) {
                    //$('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                }
                if ($('#Trailer1Rego').val() !== '') {
                    $('#Trailer1Rego_Vehicle_type').attr('disabled', false);
                }
                if ($('#Trailer2Rego').val() !== '') {
                    $('#Trailer2Rego_Vehicle_type').attr('disabled', false);
                }
                if ($('#Trailer3Rego').val() !== '') {
                    $('#Trailer3Rego_Vehicle_type').attr('disabled', false);
                }
                if ($('#Trailer4Rego').val() !== '') {
                    $('#Trailer4Rego_Vehicle_type').attr('disabled', false);
                }
                //traliler changes New change for Add rigo without plate number 21032024 dileep end


            });
            $('#kvl-profiles-modal').modal('hide');

            $('#kvl-trailer_profiles-modal').modal('hide');

        }
    $('#key_vehicle_log_profiles tbody').on('click', 'tr', function () {
        gridKeyVehicleLogProfile.$('tr.selected').removeClass('selected');
        $(this).addClass('selected');
    });
   
    let gridIncidentReportsVehicleLogProfile = $('#incident_reports_vehicle_log_profiles').DataTable({
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
    
    $('#incident_reports_vehicle_log_profiles tbody').on('click', '#btnSelectProfile', function () {
        var data = gridIncidentReportsVehicleLogProfile.row($(this).parents('tr')).data();
        populateIncidentReportModal(data.detail.id);
    });
    function populateIncidentReportModal(id) {
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=ProfileById&id=' + id,
            type: 'GET',
            dataType: 'json',
        }).done(function (result) {
            let personName = result.personName ? result.personName : 'Unknown';
            $('#PlateId').val(result.keyVehicleLogProfile.plateId);
            $('#kvl_list_plates').val(result.keyVehicleLogProfile.plateId);
            $('#CompanyName').val(result.companyName);
            $.ajax({
                url: '/Incident/Register?handler=PlateLoaded',
                data: { 'TruckConfig': result.keyVehicleLogProfile.truckConfig },
                type: 'GET',
                dataType: 'json',
            }).done(function (result1) {
                $('#TruckConfigure').val(result1.truckConfigText[0].name);
            })

            // (For adding auto entry in IR vechile select. This function is available in Register.cshtml)
            //Added by Binoy for task P7#124 Remove Step - 04-07-2024
            onbtnAddNewPlateLoadedEntryClick(); 

            //new change for trailer rego start 21032024
            if (result.keyVehicleLogProfile.vehicleRego != null) {
                loadAuditHistory(result.keyVehicleLogProfile.vehicleRego);

            }
            else {
                loadAuditHistoryWithProfileId(result.keyVehicleLogProfile.id);
            }
            //new change for trailer rego end 21032024
            //loadAuditHistory(result.keyVehicleLogProfile.vehicleRego);
        });
        $('#incident-report-profiles-modal').modal('hide');
    }



    function loadVklPopup(id, isNewEntry) {
       

        if (isLogbookExpired($('#KeyVehicleLog_ClientSiteLogBook_Date').val())) {
            alert('A new day started and this logbook expired. Please logout and login again');
            return false;
        }

        const vkl_modal_title = isNewEntry ? 'Add a new log book entry' : 'Edit log book entry';
        $('#vkl-modal').find('.modal-title').html(vkl_modal_title);
        const vkl_modal_danger = 'POI Warning'
        $('#vkl-modal').find('.modal-title .btn-danger').html(vkl_modal_danger);

        $.ajax({
            type: 'GET',
            url: '/Guard/KeyVehicleLog?handler=KeyVehicleLog',
            data: { 'id': id },
            beforeSend: function () {
                $('#loader').show();
            }
        }).done(function (response) {
           
            $('#vkl-modal').find(".modal-body").html(response);
            var siteidd = $('#ClientSitePocIdsVehicleLog').val();
            var selectedValues = siteidd.split(',');
            selectedValues.forEach(function (value) {
                
                $('#multiselectVehiclelog option[value="' + value + '"]').prop('selected', true);
                
            });

           
            
            $('#vkl-modal').modal('show', { isNewEntry: isNewEntry });
            $('#kvl_status_pd').hide();

            if (isNewEntry) {
                $('#new_log_initial_call').val(getTimeFromDateTime(new Date()));
                $('#new_log_entry_time').val(getTimeFromDateTime(new Date()));
                $('#new_log_sent_in_time').val(getTimeFromDateTime(new Date()));
                $('#ActiveGuardLoginId').val('');
                $('#titlePOIWarning').attr('hidden', true)
                $('#imagesiren').attr('hidden', true);
                // New code added for solve the product list issue KV 28022024 Start
                $('#list_product').val('');
                $('#Product').val();
                var ulElement = document.querySelector('ul.es-list');

                // Check if the <ul> element exists
                if (ulElement) {
                    // Get all <li> elements within the <ul> element
                    var listItems = ulElement.querySelectorAll('li');

                    // Loop through each <li> element
                    listItems.forEach(function (item) {
                        // Remove the 'style' attribute
                        item.removeAttribute('style');
                        item.classList.add('es-visible');
                    });
                }
                // New code added for solve the product list issue KV 28022024 end
            } else {
                // New code added for solve the product list issue KV 28022024 Start
                $('#list_product').val('');
                var ulElement = document.querySelector('ul.es-list');

                // Check if the <ul> element exists
                if (ulElement) {
                    // Get all <li> elements within the <ul> element
                    var listItems = ulElement.querySelectorAll('li');

                    // Loop through each <li> element
                    listItems.forEach(function (item) {
                        // Remove the 'style' attribute
                        item.removeAttribute('style');
                        item.classList.add('es-visible');
                    });
                }
                // New code added for solve the product list issue KV 28022024 end


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



                //New Custome Product
                var tempElement = document.createElement('div');
                tempElement.innerHTML = response;
                var hiddenFieldElement = tempElement.querySelector('#Product');

                if (hiddenFieldElement) {
                    // Get the value of the hidden field
                    var hiddenFieldValue = hiddenFieldElement.value;
                    var inputElement = $(".es-input");
                    inputElement.val(hiddenFieldValue);
                } else {

                }



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

    function loadAuditHistoryWithProfileId(item) {
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=AuditHistoryUsingProfileId&profileId=' + item,
            type: 'GET',
            dataType: 'json',
        }).done(function (result) {
            $('#key_vehicle_log_audit_history').DataTable().clear().rows.add(result).draw();
        });
    }

    function bindKvlPopupEvents(isEdit) {

        if (!isEdit) {
            /*for manifest options-start*/
            $('#IsTimeSlotNo').val(true);
            $('#cbIsTimeSlotNo').prop('checked', true);
            $('#IsVWI').val(true);
            $('#cbIsVWI').prop('checked', true);
            $('#IsSender').val(true);
            $('#cbIsSender').prop('checked', true);
            $('#IsReels').val(true);
            $('#cbIsReels').prop('checked', true);
            $('#cbIsISOVIN').prop('checked', true);

            GetToggles($('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(), 1);


            //$('#IsTimeSlotNo').val(true);
            //$('#cbIsTimeSlotNo').prop('checked', true);

            GetToggles($('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(), 3);

            //$('#IsSender').val(true);
            //$('#cbIsSender').prop('checked', true);
            GetToggles($('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(), 2);
            GetToggles($('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(), 4);
            GetToggles($('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(), 5);
            /*for manifest options-end*/
            /*for initializing the BDM to true-start*/
            $('#cbIsBDMOrSales').prop('checked', true);
            $('#cbIsBDMOrSalesDisabled').prop('checked', false);
            $('#IsBDM').val(true);
            /*for initializing the BDM to true-end*/
        }
        else {
            let isTimeSlot = $('#IsTimeSlotNo').val().toLowerCase() === 'true';
            $('#lblIsTimeSlotNo').text(isTimeSlot ? 'Time Slot No.' : 'T.No. (Load)');
            $('#cbIsTimeSlotNo').prop('checked', isTimeSlot);

            let isSender = $('#IsSender').val().toLowerCase() === 'true';
            $('#lblIsSender').text(isSender ? 'Sender Address' : 'Reciever Address');
            $('#cbIsSender').prop('checked', isSender);
            /*for manifest options-start*/
            let isReels = $('#IsReels').val().toLowerCase() === 'true';
            $('#lblIsReels').text(isReels ? 'Reels' : 'QTY');
            $('#cbIsReels').prop('checked', isReels);

            let isISOVIN = $('#IsISOVIN').val().toLowerCase() === 'true';
            $('#lblISO_One').text(isISOVIN ? 'ISO/VIN + Seal' : 'Trailer 1 Rego.');
            $('#lblISO_Two').text(isISOVIN ? 'ISO/VIN + Seal' : 'Trailer 2 Rego.');
            $('#lblISO_Three').text(isISOVIN ? 'ISO/VIN + Seal' : 'Trailer 3 Rego.');
            $('#lblISO_Four').text(isISOVIN ? 'ISO/VIN + Seal' : 'Trailer 4 Rego.');
            $('#cbIsISOVIN').prop('checked', isISOVIN);


            let isVWI = $('#IsVWI').val().toLowerCase() === 'true';
            $('#lblIsVWI').text(isVWI ? 'VWI' : 'Manifest');
            $('#cbIsVWI').prop('checked', isVWI);
            /*for manifest options-end*/
            //GetToggles($('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(), 1);
            /*for cheking  the BDM is true-start*/
            let isBDM = $('#IsBDM').val().toLowerCase() === 'true';
            if (isBDM == true) {
                $('#lblIsBDMOrSales').text('BDM/Sales');
                $('#list_BDM').prop('hidden', false);
            }
            else {
                $('#lblIsBDMOrSales').text('Supplier/Partner');
                $('#list_BDM').prop('hidden', false);
            }

            $('#cbIsBDMOrSales').prop('checked', isBDM);
            

            /*for cheking  the BDM is true-end*/


            //for checking whether the person is under scam or not(jisha james)
            if ($('#PersonOfInterest').val() != '') {
                $('#titlePOIWarning').attr('hidden', false);
                $('#imagesiren').attr('hidden', false);



            }
            else {
                $('#titlePOIWarning').attr('hidden', true);
                $('#imagesiren').attr('hidden', true);
            }



            if ($('#VehicleRego').val() !== '') {

                loadAuditHistory($('#VehicleRego').val());
            }
            var checkedornot = $('#IsCRMId').val();

            /* to load the selected items in BDM-start*/
            $("#list_BDM  input[type=checkbox]").each(function () {
                crmindivid = $(this).closest('li').find('#IsCRMIndividualId').val();
                if (checkedornot.indexOf(crmindivid) != -1) {
                    $(this).prop('checked', true);
                    $('#cbIsBDMOrSalesDisabled').prop('checked', true);
                    $('#cbIsBDMOrSales').attr('disabled', false);
                    $(this).attr('disabled', false);
                }

            });
            /* to load the selected items in BDM-end*/
            /*p1 - 115 docket output issues - start*/
            let isDocket = $('#IsDocketNo').val().toLowerCase() === 'true';

            $('#cbIsDocketNo').prop('checked', isDocket);
            if (isDocket == true) {
                $('#txtLoaderName').prop('disabled', false);
                $('#txtDispatchName').prop('disabled', false);
            }
            else {
                $('#txtLoaderName').prop('disabled', 'disabled');
                $('#txtDispatchName').prop('disabled', 'disabled');
            }
            /*p1 - 115 docket output issues - end*/
        }

        $('#cbIsTimeSlotNo').on('change', function () {
            const isChecked = $(this).is(':checked');
            $('#lblIsTimeSlotNo').text(isChecked ? 'Time Slot No.' : 'T.No. (Load)');
            $('#IsTimeSlotNo').val(isChecked);
        });
        /*p7-115 docket output issues-start*/
        $('#cbIsDocketNo').on('change', function () {

            const isChecked = $(this).is(':checked');
            $('#IsDocketNo').val(isChecked);
            if (isChecked == true) {
                $('#addCompliancesDocumentsModal').modal('show');
                $('#txtLoaderName').prop('disabled', false);
                $('#txtDispatchName').prop('disabled', false);
                
                GetComplianceDocumentsAttachmentLists();
            }

        });
        $('#btnkeyComplianceDocumentsclose').on('click', function () {


            $('#addCompliancesDocumentsModal').modal('hide');
            // $('#client-site-key-modal-new').appendTo("body").modal('show');

        });
/*p7 - 115 docket output issues - end*/
        $('#cbIsSender').on('change', function () {
            const isChecked = $(this).is(':checked');
            $('#lblIsSender').text(isChecked ? 'Sender Address' : 'Reciever Address');
            $('#IsSender').val(isChecked);
        });
        /*for manifest options-start*/
        $('#cbIsReels').on('change', function () {
            const isChecked = $(this).is(':checked');
            $('#lblIsReels').text(isChecked ? 'Reels' : 'QTY');
            $('#IsReels').val(isChecked);
        });
        $('#cbIsVWI').on('change', function () {
            const isChecked = $(this).is(':checked');
            $('#lblIsVXI').text(isChecked ? 'VWI' : 'Manifest');
            $('#IsVWI').val(isChecked);
        });

        $('#cbIsISOVIN').on('change', function () {
            const isChecked = $(this).is(':checked');
            $('#lblISO_One').text(isChecked ? 'ISO/VIN + Seal' : 'Trailer 1 Rego.');
            $('#lblISO_Two').text(isChecked ? 'ISO/VIN + Seal' : 'Trailer 2 Rego.');
            $('#lblISO_Three').text(isChecked ? 'ISO/VIN + Seal' : 'Trailer 3 Rego.');
            $('#lblISO_Four').text(isChecked ? 'ISO/VIN + Seal' : 'Trailer 4 Rego.');
            $('#IsISOVIN').val(isChecked);
        });
        /*for manifest options-end*/
        /*for changing the BDM-start*/
        $('#cbIsBDMOrSales').on('change', function () {

            const isChecked = $(this).is(':checked');
            if (isChecked == true) {
                $('#lblIsBDMOrSales').text('BDM/Sales');
                $('#list_BDM').prop('hidden', false);
                $("#list_BDM  input[type=checkbox]:checked").each(function () {
                    var isChecked1 = $(this).is(':checked');
                    if (isChecked1 == true) {
                        $(this).prop('checked', false);
                    }

                });
                if ($('#PersonType').find('option[value=' + 166 + ']').length == 1) {
                    $('#PersonType').val(166);
                }
                else {
                    $('#PersonType').val('');
                }
                
            }
            else {
                $('#lblIsBDMOrSales').text('Supplier/Partner');
                $('#list_BDM').prop('hidden', false);
                //to uncheck the ticked options-start
                $("#list_BDM  input[type=checkbox]:checked").each(function () {
                    var isChecked1 = $(this).is(':checked');
                    if (isChecked1 == true) {
                        $(this).prop('checked', false);
                    }

                });
                $('#IsCRMId').val('');
                //to uncheck the ticked options-end
                if ($('#PersonType').find('option[value=' + 195 + ']').length == 1) {
                    $('#PersonType').val(195);
                }
                else {
                    $('#PersonType').val('');
                }
                
            }
            $('#IsBDM').val(isChecked);
        });

        $('#list_BDM li').on('change', '#cbCRMActivity', function () {


            $('#IsCRMId').val('');

            $("#list_BDM  input[type=checkbox]:checked").each(function () {
                var isChecked1 = $(this).is(':checked');
                if (isChecked1 == true) {
                    crmindivid = $(this).closest('li').find('#IsCRMIndividualId').val();
                    if ($('#IsCRMId').val() == '') {
                        $('#IsCRMId').val(crmindivid);
                    }
                    else {
                        var crmindividnew = $('#IsCRMId').val() + ',' + crmindivid;
                        $('#IsCRMId').val(crmindividnew);
                    }
                }
            });

        });
        /*for changing the BDM-end*/
        /*p7-110  crm issues-start*/
        $('#cbIsBDMOrSalesDisabled').on('change', function () {
            const isChecked = $(this).is(':checked');
            if (isChecked == true) {
                $('#cbIsBDMOrSales').attr('disabled', false);
                $("#list_BDM  input[type=checkbox]:disabled").each(function () {
                    var isChecked1 = $(this).is(':disabled');
                    if (isChecked1 == true) {
                        $(this).attr('disabled', false);
                    }

                });
                if ($('#PersonType').find('option[value=' + 166 + ']').length == 1) {
                    $('#PersonType').val(166);
                }
            }
            else {
                $('#cbIsBDMOrSales').attr('disabled', 'disabled')
                $('#cbIsBDMOrSales').prop('checked', true);
                $('#cbIsBDMOrSales').change();
                $("#list_BDM  input[type=checkbox]:checked").each(function () {
                    var isChecked1 = $(this).is(':checked');
                    if (isChecked1 == true) {
                        $(this).prop('checked', false);
                    }

                });
                $("#list_BDM  input[type=checkbox]").each(function () {
                    var isChecked1 = $(this).is(':disabled');
                    if (isChecked1 == false) {
                        $(this).attr('disabled', 'disabled');
                    }

                });
                $('#PersonType').val('');
            }
        });
        /*p7-110  crm issues-end*/
        //to check whether the person of interest is selected or not 
        $('#PersonOfInterest').on('change', function () {
            const value = $(this).val();
            if (value != '') {
                $('#titlePOIWarning').attr('hidden', false);
                $('#imagesiren').attr('hidden', false);
                $('#kvl_list_plates').val(185);
                $('#kvl_list_plates').change();

            }
            else {
                if ($('#PersonType').find('option:selected').text() != 'POI - Intruder (Tresspass or Arrested)') {
                    $('#titlePOIWarning').attr('hidden', true);
                    $('#imagesiren').attr('hidden', true);
                    $('#kvl_list_plates').val('');
                    //$('#VehicleRego').val('');
                }
            }
            //$('#IsPOIAlert').val(isChecked);
        });
        /*to confirm whether the image is person vehicle or other -start*/
        $('#chbIsPerson').on('change', function () {
            const isChecked = $(this).is(':checked');
            if (isChecked == true) {

                $('#IsVehicle').val(false);
                $('#chbIsVehicle').prop('checked', false)
                $('#IsNone').val(false);
                $('#chbIsNone').prop('checked', false)

            }

            $('#IsPerson').val(isChecked);

        });
        $('#chbIsVehicle').on('change', function () {
            const isChecked = $(this).is(':checked');
            if (isChecked == true) {

                $('#IsPerson').val(false);
                $('#chbIsPerson').prop('checked', false)
                $('#IsNone').val(false);
                $('#chbIsNone').prop('checked', false)

            }
            $('#IsVehicle').val(isChecked);

        });
        $('#chbIsNone').on('change', function () {
            const isChecked = $(this).is(':checked');
            if (isChecked == true) {

                $('#IsPerson').val(false);
                $('#chbIsPerson').prop('checked', false)
                $('#IsVehicle').val(false);
                $('#chbIsVehicle').prop('checked', false)

            }
            $('#IsNone').val(isChecked);

        });
        /*to confirm whether the image is person vehicle or other -end*/
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
            //P7-123 Prevent Crash -start
            if ($('#InWeight').val() > 999) {
                new MessageModal({ message: "Please enter the value in tons. The maximum allowable value is 999" }).showWarning();
                $('#InWeight').val('');
            }
            else if ($('#TareWeight').val() > 999) {
                new MessageModal({ message: "Please enter the value in tons. The maximum allowable value is 999" }).showWarning();
                $('#TareWeight').val('');
            }
            else {
                setCalculatedOutWeight();
            }
            //P7-123 Prevent Crash -end
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
        //P7-123 Prevent Crash -start if manually we want to enter the out weight value and check if it is greater than 999
        $('#OutWeight').on('blur', function () {
            if ($(this).val() > 999) {
                new MessageModal({ message: "Please enter the value in tons. The maximum allowable value is 999" }).showWarning();
                setCalculatedOutWeight();
            }
        });
        //P7-123 Prevent Crash -end 

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
                        $('#list_clientsite_keys').append(new Option(result.keyNo, result.id, false, true));
                        $('#list_clientsite_keystext').append(new Option(result.description, result.id, false, true));

                    };
                });
            }

            if ($('#PlateId').val() !== '') {
                $('#kvl_list_plates').val($('#PlateId').val())
            }

            var check = $('#Trailer1Rego').val();

            if ($('#VehicleRego').val() !== '') {
                $('#kvl_list_plates').attr('disabled', false);
            }
            //traliler changes New change for Add rigo without plate number 21032024 dileep Start
            if ($('#Trailer1Rego').val() !== '') {
                $('#Trailer1Rego_Vehicle_type').attr('disabled', false);
            }
            if ($('#Trailer2Rego').val() !== '') {
                $('#Trailer2Rego_Vehicle_type').attr('disabled', false);
            }
            if ($('#Trailer3Rego').val() !== '') {
                $('#Trailer3Rego_Vehicle_type').attr('disabled', false);
            }
            if ($('#Trailer4Rego').val() !== '') {
                $('#Trailer4Rego_Vehicle_type').attr('disabled', false);
            }
            //traliler changes New change for Add rigo without plate number 21032024 dileep end

        }


        let list_clientsite_keystext = $('#list_clientsite_keystext').select2({
            placeholder: "Select",
            theme: 'bootstrap4',
            allowClear: true,
            dropdownAutoWidth: true,
            width: '100%',
            ajax: {
                url: '/Guard/KeyVehicleLog?handler=ClientSiteKeysDesc',
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return {
                        clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                        searchKeyNo: params.term,
                        searchKeyDesc: params.term
                    }
                },
                processResults: function (data) {
                    return {
                        results: $.map(data, function (item) {
                            return {
                                text: item.description,
                                id: item.id,
                                title: item.description
                            }
                        })
                    };
                },
                cache: true
            }
        });


        $('#list_clientsite_keystext').on('change', function () {
            const option = $(this).find(":selected");
            const test = option.val();
            if (option.val() === '')
                return;
            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=ClientSiteKeyNo',
                type: 'GET',
                data: {
                    keyId: option.val(),
                    clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                }
            }).done(function (response) {
                $.ajax({
                    url: '/Guard/KeyVehicleLog?handler=IsKeyAllocated',
                    type: 'GET',
                    data: {
                        logbookId: $('#KeyVehicleLog_ClientSiteLogBookId').val(),
                        keyNo: response,
                    }
                }).done(function (keyIsAllocated) {
                    if (!keyIsAllocated) selectKey2();
                    else isKeyAllocatedModalDesc.showConfirmation();
                });
            });
        });



        $('#multiselectVehiclelog').multiselect({
            maxHeight: 400,
            buttonWidth: '100%',
            nonSelectedText: 'Select',
            buttonTextAlignment: 'left',
            includeSelectAllOption: true,
        });



        $('#VehicleRego').on('blur', function () {
            const vehicleRegoHasVal = $(this).val() !== '';
            $('#kvl_list_plates').attr('disabled', !vehicleRegoHasVal);
            $('#crm_list_plates').attr('disabled', !vehicleRegoHasVal);
            if (!vehicleRegoHasVal) {
                // TODO: clear previous auto populated profile values
            }
            $('#crmVehicleRego').val($('#VehicleRego').val());

        });

        $('#crmVehicleRego').on('blur', function () {
            const vehicleRegoHasVal = $(this).val() !== '';
            $('#kvl_list_plates').attr('disabled', !vehicleRegoHasVal);
            $('#crm_list_plates').attr('disabled', !vehicleRegoHasVal);
            if (!vehicleRegoHasVal) {
                // TODO: clear previous auto populated profile values
            }
            $('#VehicleRego').val($('#crmVehicleRego').val());

        });
        $('#crmVehicleRego').on('change', function () {
            const vehicleRegoHasVal = $(this).val() !== '';
            $('#VehicleRego').val($('#crmVehicleRego').val());
            GetVehicleImage();

        });

        $('#multiselectVehiclelog').on('change', function () {
          
            var selectedValues = $(this).val();
            $('#ClientSitePocIdsVehicleLog').val(selectedValues);

            
        });

        /*same changes in vehicle rego-start*/

        /* to display the corresponding image on changing the reg no,person type,company name and person name-start*/
        $('#VehicleRego').on('change', function () {
            const vehicleRegoHasVal = $(this).val() !== '';
            $('#crmVehicleRego').val($('#VehicleRego').val());
            GetVehicleImage();
            let regoToUpper = $(this).val().toUpperCase();
            $(this).val(regoToUpper);
        });
        $('#CompanyName').on('change', function () {

            GetPersonImage();
        });
        $('#PersonType').on('change', function () {

            GetPersonImage();
            if ($('#PersonType').find('option:selected').text() == 'CRM (BDM Activity)') {
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=CRMNumber',
                    type: 'GET',
                    data: { 'IndividualType': $('#PersonType').val() },

                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    $('#VehicleRego').val(result);
                    $('#crm_list_plates').attr('disabled', false);
                    $('#kvl_list_plates').attr('disabled', false);

                    if (result.success) {

                        $('#crmVehicleRego').val(result);
                        $('#VehicleRego').val(result);
                        $('#crm_list_plates').attr('disabled', false);
                        $('#kvl_list_plates').attr('disabled', false);
                    } else {
                        $('#crmVehicleRego').val(result);
                    }

                });
            }
            /*to function when type of individual is poi intruder -start*/
            else if ($('#PersonType').find('option:selected').text() == 'POI - Intruder (Tresspass or Arrested)') {
                $('#titlePOIWarning').attr('hidden', false);
                $('#imagesiren').attr('hidden', false);

                $('#PersonOfInterest').val(176);


                $('#kvl_list_plates').val(185);
                $('#kvl_list_plates').change();



            }
            else {
                $('#titlePOIWarning').attr('hidden', true);
                $('#imagesiren').attr('hidden', true);
                //$('#VehicleRego').val('');
                //$('#kvl_list_plates').val('');
                //$('#kvl_list_plates').change();
            }
            /*to function when type of individual is poi intruder - end*/
        });
        $('#PersonName').on('change', function () {

            GetPersonImage();
        });
        /* to display the corresponding image on changing the reg no,person type,company name and person name-end*/


        /* $('#VehicleRego, #Trailer1Rego, #Trailer2Rego, #Trailer3Rego, #Trailer4Rego,#crmVehicleRego').on('keypress', vehicleRegoValidateSplChars);*/
        $('#VehicleRego, #Trailer1Rego, #Trailer2Rego, #Trailer3Rego, #Trailer4Rego,#crmVehicleRego').on('keyup', vehicleRegoToUpperCase);

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
        $('#btnCopyToClipBoard').on('click', function () {
            var time = $('#new_log_initial_call').val();
            navigator.clipboard.writeText(time)
                .then(() => {
                    console.log(`Copied " + $('#new_log_initial_call').val() + " to clipboard`);
                })
                .catch((err) => {
                    console.error('Could not copy text: ', err);
                });

        });
        $('#kvl_list_plates').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {
                $('#PlateId').val(option.val());
                /*to load the plate to crmtab -start*/
                $('#crm_list_plates').val(option.val());
                /*to load the plate to crmtab -end*/
            }
            if ($('#kvl_list_plates').find('option:selected').text() == 'POI-Name') {

                isPOIOnsiteModal.showConfirmation();
            }

        });
        $('#crm_list_plates').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {
                $('#PlateId').val(option.val());
                /*to load the plate to crmtab -start*/
                $('#kvl_list_plates').val(option.val());
                /*to load the plate to crmtab -end*/
            }
        });
        /*to load the plate to crmtab -start*/
        $('#TruckConfig').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {


                $('#crmTruckConfig').val(option.val());

            }
        });

        $('#TrailerType').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {


                $('#crmTrailerType').val(option.val());

            }
        });
        $('#crmTruckConfig').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {


                $('#TruckConfig').val(option.val());

            }
        });       

        $('#crmTrailerType').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {


                $('#TrailerType').val(option.val());

            }
        });
        /*to load the plate to crmtab -end*/
        /*to check whether the fields are restricted to characters -start*/
        //$('#crmCompanyABN').on('keypress', function (e) {
        //    var x = e.which || e.keycode;
        //    if ((x >= 48 && x <= 57))
        //        return true;
        //    else
        //        return false;
        //});

        //$('#LandLineNumber').on('keypress', function (e) {
        //        var x = e.which || e.keycode;
        //        if ((x >= 48 && x <= 57))
        //            return true;
        //        else
        //            return false;
        //});
        /*to load the plate to crmtab -end*/

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
                const vehicleRego = $('#VehicleRego').val()
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=DeleteAttachment',
                    type: 'POST',
                    dataType: 'json',
                    data: {
                        reportReference: $('#ReportReference').val(),
                        fileName: fileName,
                        vehicleRego: vehicleRego
                    },
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result) {
                        target.parentNode.parentNode.removeChild(target.parentNode);
                        adjustKvlAttachmentCount(false);
                        GetPersonImage();
                        GetVehicleImage()
                    }
                });
            }
        });



        //$('#btn-delete-PersonImage').on('click', function (e) {
        //    if (confirm('Are you sure want to remove this Person Image?')) {
        //        e.preventDefault();
        //        const fileName = $('#CompanyName').val() + '-' + $('#PersonType').find('option:selected').text() + '-' + $('#PersonName').val() + '.jpg';
        //        const vehicleRego = $('#VehicleRego').val()
        //        $.ajax({
        //            url: '/Guard/KeyVehiclelog?handler=DeletePersonImage',
        //            type: 'POST',
        //            dataType: 'json',
        //            data: {
        //                reportReference: $('#ReportReference').val(),
        //                fileName: fileName
        //            },
        //            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        //        }).done(function (result) {
        //            if (result) {
        //                $('#img_PersonId').prop('hidden', true)
        //                $('#head_PersonId').prop('hidden', false)
        //                $('#btn-delete-PersonImage').prop('hidden', true)
        //            }
        //        });
        //    }
        //});


        /*to delete person image - start*/

        function DeletePersonImage() {

            /*e.preventDefault();*/
            const fileName = $('#CompanyName').val() + '-' + $('#PersonType').find('option:selected').text() + '-' + $('#PersonName').val() + '.jpg';
            const vehicleRego = $('#VehicleRego').val()
            $.ajax({
                url: '/Guard/KeyVehiclelog?handler=DeletePersonImage',
                type: 'POST',
                dataType: 'json',
                data: {
                    reportReference: $('#ReportReference').val(),
                    fileName: fileName
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result) {
                    $('#img_PersonId').prop('hidden', true)
                    $('#head_PersonId').prop('hidden', false)
                    $('#btn-delete-PersonImage').prop('hidden', true)
                }
            });
        }



        /*  to delete person image - end*/

        //$('#btn-delete-VehicleImage').on('click', function (e) {
        //    if (confirm('Are you sure want to remove this Vehicle Image?')) {
        //        e.preventDefault();
        //        const fileName = $('#VehicleRego').val() + '.jpg';
        //        const vehicleRego = $('#VehicleRego').val()
        //        $.ajax({
        //            url: '/Guard/KeyVehiclelog?handler=DeleteVehicleImage',
        //            type: 'POST',
        //            dataType: 'json',
        //            data: {
        //                reportReference: $('#ReportReference').val(),
        //                fileName: fileName,
        //                vehicleRego: vehicleRego
        //            },
        //            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        //        }).done(function (result) {
        //            if (result) {
        //                $('#img_VehicleId').prop('hidden', true)
        //                $('#head_VehicleId').prop('hidden', false)
        //                $('#btn-delete-VehicleImage').prop('hidden', true)

        //            }
        //        });
        //    }
        //});


        /*todelete the vehicle image - start*/
        function DeleteVehicleImage() {

            //e.preventDefault();
            const fileName = $('#VehicleRego').val() + '.jpg';
            const vehicleRego = $('#VehicleRego').val()
            $.ajax({
                url: '/Guard/KeyVehiclelog?handler=DeleteVehicleImage',
                type: 'POST',
                dataType: 'json',
                data: {
                    reportReference: $('#ReportReference').val(),
                    fileName: fileName,
                    vehicleRego: vehicleRego
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result) {
                    $('#img_VehicleId').prop('hidden', true)
                    $('#head_VehicleId').prop('hidden', false)
                    $('#btn-delete-VehicleImage').prop('hidden', true)

                }
            });

        }

        /*todelete the vehicle image - end*/
        $('#copyButton').on('click', function () {
            /*Copy to clipboard*/
            var textToCopy = "";
            textToCopy = textToCopy + "Initial Call : " + $('#new_log_initial_call').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Entry Time : " + $('#new_log_entry_time').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Sent In Time : " + $('#new_log_sent_in_time').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Exit Time : " + $('#new_log_exit_time').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "ID No. / Car or Truck Rego : " + $('#VehicleRego').val();
            textToCopy = textToCopy + "\r\n";
            var selectedOption = $("#kvl_list_plates option[value='" + $('#kvl_list_plates').val() + "']");
            textToCopy = textToCopy + "ID / Plate(State or AU) : " + selectedOption.text();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Time Slot No. : " + $('#TimeSlotNo').val();
            textToCopy = textToCopy + "\r\n";
            var selectedVehicleConfig = $("#TruckConfig option[value='" + $('#TruckConfig').val() + "']");
            textToCopy = textToCopy + "Vehicle Config : " + selectedVehicleConfig.text();
            textToCopy = textToCopy + "\r\n";
            var selectedTrailerType = $("#TrailerType option[value='" + $('#TrailerType').val() + "']");
            textToCopy = textToCopy + "Trailer Type : " + selectedTrailerType.text();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Max Weight : " + $('#MaxWeight').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Reels : " + $('#Reels').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Trailer 1 Rego.or ISO + Seals : " + $('#Trailer1Rego').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Trailer 2 Rego.or ISO + Seals : " + $('#Trailer2Rego').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Trailer 3 Rego.or ISO + Seals : " + $('#Trailer3Rego').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Trailer 4 Rego.or ISO + Seals : " + $('#Trailer4Rego').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Weight In Gross (t) : " + $('#InWeight').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Weight Out Net(t) : " + $('#OutWeight').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Weight Empty Tare(t) : " + $('#TareWeight').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Contamination Deduction? : " + $('#DeductionPercentage').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Company Name : " + $('#CompanyName').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Individuals Name : " + $('#PersonName').val();
            textToCopy = textToCopy + "\r\n";
            var selectedTypeofIndividual = $("#PersonType option[value='" + $('#PersonType').val() + "']");
            textToCopy = textToCopy + "Type of Individual : " + selectedTypeofIndividual.text();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Mobile Number : " + $('#MobileNumber').val();
            textToCopy = textToCopy + "\r\n";
            var selectedEntryReason = $("#EntryReason option[value='" + $('#EntryReason').val() + "']");
            textToCopy = textToCopy + "Entry Reason : " + selectedEntryReason.text();
            textToCopy = textToCopy + "\r\n";
            var selectedEntryProduct = $("#list_product option[value='" + $('#list_product').val() + "']");
            var hiddenFieldElement = $('#Product').val();
            textToCopy = textToCopy + "Product : " + hiddenFieldElement;
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Customer Ref : " + $('#CustomerRef').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "VWI : " + $('#Vwi').val();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Sender : " + $('#Sender').val();
            textToCopy = textToCopy + "\r\n";
            var selectedSitePocId = $("#ClientSitePocId option[value='" + $('#ClientSitePocId').val() + "']");
            textToCopy = textToCopy + "Site POC : " + selectedSitePocId.text();
            textToCopy = textToCopy + "\r\n";
            var selectedSiteLocation = $("#ClientSiteLocationId option[value='" + $('#ClientSiteLocationId').val() + "']");
            textToCopy = textToCopy + "Site Location : " + selectedSiteLocation.text();
            textToCopy = textToCopy + "\r\n";
            textToCopy = textToCopy + "Notes : " + $('#Notes').val();
            textToCopy = textToCopy + "\r\n";
            navigator.clipboard.writeText(textToCopy)
                .then(() => { alert('Copied to clipboard.') })
                .catch((error) => { alert('Copy failed. Error: ${error}') })
        });



        $('#kvl_attachment_upload').on("change", function (e) {
            var vehicleRego = $('#VehicleRego').val();
            /*to check whether  the vehicle number is entered*/
            if (vehicleRego !== "") {
                const fileUpload = this;
                if (fileUpload.files.length > 0) {

                    let arIndex = [];
                    const attachmentList = document.getElementById('kvl-attachment-list').getElementsByTagName('li');
                    for (let i = 0; i < attachmentList.length; i++)
                        arIndex.push(parseInt(attachmentList[i].getAttribute('data-index')));
                    let attachIndex = arIndex.length > 0 ? Math.max(...arIndex) + 1 : 0;


                    /*Maximum allowed size in bytes*/
                    const maxAllowedSize = 30 * 1024 * 1024;
                    var fileSizeCheck = true;
                    var FileName = "";
                    for (let i = 0; i < fileUpload.files.length; i++, attachIndex++) {
                        const filecheck = fileUpload.files.item(i);
                        if (filecheck.size > maxAllowedSize) {
                            fileSizeCheck = false;
                            FileName = FileName + filecheck.name + ','
                        }
                    }


                    //for (var i = $('#gridIsPersonOrVehicle  tr').length; i > 1; i--) {
                    //    $('#gridIsPersonOrVehicle  tr:last').remove();
                    //}

                    if (fileSizeCheck) {
                        for (let i = 0; i < fileUpload.files.length; i++) {
                            var rowCount = $('#gridIsPersonOrVehicle  tr').length;
                            const fileExtn = fileUpload.files[i].name.split('.').pop();
                            /*if only  one jpg file is uploaded -start*/
                            if (fileUpload.files.length == 1 && (fileExtn == 'jpg' || fileExtn == 'JPG')) {
                                $('#vkl-image-modal').modal('show');
                                $('#chbIsPerson').prop('checked', false)
                                $('#chbIsVehicle').prop('checked', false)
                                $('#chbIsNone').prop('checked', true)
                                /*temporarily commented*/

                                //$('#gridIsPersonOrVehicle  tr:last').after('<tr><td>' + rowCount + '</td>' +
                                //    '<td >' + fileUpload.files[i].name + '</td>' +
                                //    '<td hidden>' + i + '</td>' +

                                //    '<td> <input type="checkbox" id="chbIsPerson" /><label for="chbIsPerson">Is Person</label><input type="hidden" id="IsPerson" />  <input type="checkbox" id="chbIsVehicle" /><label for="chbIsVehicle">Is Vehicle</label><input type="hidden" id="IsVehicle" />  <input type="checkbox" id="chbIsNone" /><label for="chbIsNone">Is None</label><input type="hidden" id="IsNone" /></td>' +
                                //    '</tr > ');
                            }
                            /*if only  one jpg file is uploaded -end*/
                            else {

                                if (fileSizeCheck) {
                                    /* for (let i = 0; i < fileUpload.files.length; i++) {*/


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



                                    fileForm.append('vehicle_rego', $('#VehicleRego').val());

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
                                            //icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                                            //icon.title = 'Delete';
                                            GetAttachmentLists()
                                        } else {
                                            icon.className = 'fa fa-exclamation-triangle ml-2 text-warning';
                                            icon.title = 'Error';
                                        }

                                        adjustKvlAttachmentCount(true);
                                    });
                                    /*}*/

                                }
                            }
                        }

                        /*$('#vkl-image-modal').modal('show');*/
                    }

                    else {
                        alert("Maximum allowed size (30Mb) exceeded for the file '" + FileName + "'")
                    }
                }
            }
            else {
                alert("Please enter the ID/Truck Registration Number");
            }

        });

        /* to get the  attachments automatically-start*/
        function GetAttachmentLists() {
            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=Attachments&truck=' + $('#VehicleRego').val(),
                type: 'GET',
                dataType: 'json',
            }).done(function (result) {
                $("#kvl-attachment-list").empty();
                for (var attachIndex = 0; attachIndex < result.length; attachIndex++) {
                    const file = result[attachIndex];
                    const attachment_id = 'attach_' + attachIndex;
                    const li = document.createElement('li');
                    li.id = attachment_id;
                    li.className = 'list-group-item';
                    li.dataset.index = attachIndex;
                    let liText = document.createTextNode(file);
                    const icon = document.createElement("i");
                    icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                    icon.title = 'Delete';
                    icon.style = 'cursor:pointer';
                    li.appendChild(liText);
                    li.appendChild(icon);
                    const anchorTag = document.createElement("a");
                    anchorTag.href = '/KvlUploads/' + $('#VehicleRego').val() + "/" + file;
                    anchorTag.target = "_blank";
                    const icon2 = document.createElement("i");
                    icon2.className = 'fa fa-download ml-2 text-primary';
                    icon2.title = 'Download';
                    icon2.style = 'cursor:pointer';
                    anchorTag.appendChild(icon2);
                    li.appendChild(anchorTag);
                    document.getElementById('kvl-attachment-list').append(li);



                }

                $('#kvl_attachments_count').html(result.length);
            });
        }
        /* to get the  attachments automatically-end*/

        /*click the button to uplaod image-start*/
        $('#btnIsPersonOrVehicle').on("click", function (e) {



            const isPerson = $('#chbIsPerson').is(':checked');
            if (isPerson == true) {
                if (!ReplacePersonImage()) {
                    PersonImageUpload('cancel');
                }
                else {
                    //if (confirm('Attachment with same name already exists. Do you want to Replace it?')) {
                    DeletePersonImage();
                    $('#vkl-copyorreplace-modal').modal('show');
                    $('#txtPersonOrVehicle').val('Person');
                    /* PersonImageUpload('yes')*/
                    //}
                }
            }
            const isVehicle = $('#chbIsVehicle').is(':checked');
            if (isVehicle == true) {
                if (!ReplaceVehicleImage()) {
                    VehicleImageUpload('cancel');
                }
                else {
                    DeleteVehicleImage();

                    $('#vkl-copyorreplace-modal').modal('show');
                    $('#txtPersonOrVehicle').val('Vehicle');
                    //if (confirm('Attachment with same name already exists. Do you want to Replace it?')) {
                    //    VehicleImageUpload('yes')
                    //}
                }

            }
            const isNone = $('#chbIsNone').is(':checked');
            if (isNone == true) {
                OtherImageUpload()
            }
            $('#vkl-image-modal').modal('hide');
        });
        $('#btnReplace').on("click", function (e) {
            if ($('#txtPersonOrVehicle').val() == 'Person') {
                PersonImageUpload('replace')
            }
            if ($('#txtPersonOrVehicle').val() == 'Vehicle') {
                VehicleImageUpload('replace')
            }

            $('#vkl-copyorreplace-modal').modal('hide');

        });

        $('#btnSkip').on("click", function (e) {


            $('#vkl-copyorreplace-modal').modal('hide');

        });

        $('#btnLetmeDecide').on("click", function (e) {


            if ($('#txtPersonOrVehicle').val() == 'Person') {
                PersonImageUpload('yes')
            }
            if ($('#txtPersonOrVehicle').val() == 'Vehicle') {
                VehicleImageUpload('yes')
            }

            $('#vkl-copyorreplace-modal').modal('hide');

        });
        /*click the button to uplaod image-end*/

        /*to check whether the vehicle image exists-start*/

        function ReplaceVehicleImage() {
            var foundit = 'cancel';
            $("#kvl-attachment-list li").each((id, elem) => {
                if (elem.innerText == $('#VehicleRego').val() + "." + "jpg") {
                    foundit = 'yes';


                }

            });

            if (foundit == 'yes') {
                return true;
            }
            return false;
        }

        /*to check whether the vehicle image exists - end*/

        /*to check whether the person image exists-start*/

        function ReplacePersonImage() {
            var foundit = 'cancel';
            $("#kvl-attachment-list li").each((id, elem) => {
                if (elem.innerText == $('#CompanyName').val() + "-" + $('#PersonType').find('option:selected').text() + "-" + $('#PersonName').val() + "." + "jpg") {
                    foundit = 'yes';


                }

            });

            if (foundit == 'yes') {
                return true;
            }
            return false;
        }

        /*to check whether the vehicle image exists - end*/

        /*to upload the vehicle image-start*/
        function VehicleImageUpload(foundit) {
            const fileUpload = $('#kvl_attachment_upload').prop('files');



            if (fileUpload.length > 0) {

                let arIndex = [];
                const attachmentList = document.getElementById('kvl-attachment-list').getElementsByTagName('li');
                for (let i = 0; i < attachmentList.length; i++)
                    arIndex.push(parseInt(attachmentList[i].getAttribute('data-index')));
                let attachIndex = arIndex.length > 0 ? Math.max(...arIndex) + 1 : 0;

                /*Maximum allowed size in bytes*/
                const maxAllowedSize = 30 * 1024 * 1024;
                var fileSizeCheck = true;
                var FileName = "";
                for (let i = 0; i < fileUpload.length; i++) {
                    const filecheck = fileUpload.item(i);
                    if (filecheck.size > maxAllowedSize) {
                        fileSizeCheck = false;
                        FileName = FileName + filecheck.name + ','
                    }
                }


                if (fileSizeCheck) {


                    /* for (let i = 0; i < fileUpload.length; i++) {*/
                    //const file = fileUpload.item(i);
                    const file = fileUpload.item(0);
                    const attachment_id = 'attach_' + attachIndex;
                    const li = document.createElement('li');
                    li.id = attachment_id;
                    li.className = 'list-group-item';
                    const fileExtn = file.name.split('.').pop();

                    li.dataset.index = attachIndex;

                    let liText = document.createTextNode($('#VehicleRego').val() + "." + fileExtn);

                    const icon = document.createElement("i");
                    icon.className = 'fa fa-circle-o-notch fa-spin ml-2 text-success';
                    icon.title = 'Uploading...';
                    icon.style = 'cursor:pointer';

                    li.appendChild(liText);
                    li.appendChild(icon);
                    document.getElementById('kvl-attachment-list').append(li);

                    // upload file to server
                    const fileForm = new FormData();
                    /* fileForm.append('attachments', fileUpload.item(i))*/
                    fileForm.append('attachments', fileUpload.item(0))
                    fileForm.append('attach_id', attachment_id);
                    fileForm.append('report_reference', $('#ReportReference').val());
                    fileForm.append('vehicle_rego', $('#VehicleRego').val());
                    fileForm.append('foundit', foundit);
                    $.ajax({
                        url: '/Guard/KeyVehiclelog?handler=VehicleImageUpload',
                        type: 'POST',
                        data: fileForm,
                        dataType: 'json',
                        processData: false,
                        contentType: false,
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    }).done(function (result) {
                        const icon = document.getElementById(result.attachmentId).getElementsByTagName('i').item(0);
                        if (result.success) {
                            //icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                            //icon.title = 'Delete';
                            GetAttachmentLists();

                        } else {
                            icon.className = 'fa fa-exclamation-triangle ml-2 text-warning';
                            icon.title = 'Error';
                        }
                        GetVehicleImage();


                    });

                    //    break;

                    //}
                    adjustKvlAttachmentCount(true);
                    //GetAttachmentLists();

                }
                else {
                    alert("Maximum allowed size (30Mb) exceeded for the file '" + FileName + "'")
                }
            }

        }
        /*to upload the vehicle image-end*/

        /*to upload the person image-start*/
        function PersonImageUpload(foundit) {
            const fileUpload = $('#kvl_attachment_upload').prop('files');



            if (fileUpload.length > 0) {

                let arIndex = [];
                const attachmentList = document.getElementById('kvl-attachment-list').getElementsByTagName('li');
                for (let i = 0; i < attachmentList.length; i++)
                    arIndex.push(parseInt(attachmentList[i].getAttribute('data-index')));
                let attachIndex = arIndex.length > 0 ? Math.max(...arIndex) + 1 : 0;

                /*Maximum allowed size in bytes*/
                const maxAllowedSize = 30 * 1024 * 1024;
                var fileSizeCheck = true;
                var FileName = "";
                for (let i = 0; i < fileUpload.length; i++) {
                    const filecheck = fileUpload.item(i);
                    if (filecheck.size > maxAllowedSize) {
                        fileSizeCheck = false;
                        FileName = FileName + filecheck.name + ','
                    }
                }


                if (fileSizeCheck) {
                    //for (let i = 0; i < fileUpload.length; i++) {
                    const file = fileUpload.item(0);
                    const attachment_id = 'attach_' + attachIndex;
                    const li = document.createElement('li');
                    li.id = attachment_id;
                    li.className = 'list-group-item';
                    const fileExtn = file.name.split('.').pop();
                    li.dataset.index = attachIndex;
                    let liText = document.createTextNode($('#CompanyName').val() + "-" + $('#PersonType').find('option:selected').text() + "-" + $('#PersonName').val() + "." + fileExtn);

                    const icon = document.createElement("i");
                    icon.className = 'fa fa-circle-o-notch fa-spin ml-2 text-success';
                    icon.title = 'Uploading...';
                    icon.style = 'cursor:pointer';

                    li.appendChild(liText);
                    li.appendChild(icon);
                    document.getElementById('kvl-attachment-list').append(li);

                    // upload file to server
                    const fileForm = new FormData();
                    fileForm.append('attachments', fileUpload.item(0))
                    fileForm.append('attach_id', attachment_id);
                    fileForm.append('report_reference', $('#ReportReference').val());
                    fileForm.append('vehicle_rego', $('#VehicleRego').val());
                    fileForm.append('company_name', $('#CompanyName').val());
                    fileForm.append('person_type', $('#PersonType').find('option:selected').text());
                    fileForm.append('person_name', $('#PersonName').val());
                    fileForm.append('foundit', foundit);

                    $.ajax({
                        url: '/Guard/KeyVehiclelog?handler=PersonImageUpload',
                        type: 'POST',
                        data: fileForm,
                        dataType: 'json',
                        processData: false,
                        contentType: false,
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    }).done(function (result) {
                        const icon = document.getElementById(result.attachmentId).getElementsByTagName('i').item(0);
                        if (result.success) {
                            //icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                            //icon.title = 'Delete';
                            GetAttachmentLists();
                        } else {
                            icon.className = 'fa fa-exclamation-triangle ml-2 text-warning';
                            icon.title = 'Error';
                        }

                        GetPersonImage();


                    });



                    //}
                    adjustKvlAttachmentCount(true);

                }
                else {
                    alert("Maximum allowed size (30Mb) exceeded for the file '" + FileName + "'")
                }
            }

        }
        /*to upload the person image-end*/
        /*p7-115 Docket Output issues-start*/
        $('#kvl_attachment_compliance_documents_upload').on("change", function (e) {
            var vehicleRego = $('#VehicleRego').val();
            /*to check whether  the vehicle number is entered*/
            if (vehicleRego !== "") {
                const fileUpload = this;
                if (fileUpload.files.length > 0) {

                    let arIndex = [];
                    const attachmentList = document.getElementById('kvl-attachment_compliance_documents-list').getElementsByTagName('li');
                    for (let i = 0; i < attachmentList.length; i++)
                        arIndex.push(parseInt(attachmentList[i].getAttribute('data-index')));
                    let attachIndex = arIndex.length > 0 ? Math.max(...arIndex) + 1 : 0;


                    /*Maximum allowed size in bytes*/
                    const maxAllowedSize = 30 * 1024 * 1024;
                    var fileSizeCheck = true;
                    var FileName = "";
                    for (let i = 0; i < fileUpload.files.length; i++, attachIndex++) {
                        const filecheck = fileUpload.files.item(i);
                        if (filecheck.size > maxAllowedSize) {
                            fileSizeCheck = false;
                            FileName = FileName + filecheck.name + ','
                        }
                    }


                    //for (var i = $('#gridIsPersonOrVehicle  tr').length; i > 1; i--) {
                    //    $('#gridIsPersonOrVehicle  tr:last').remove();
                    //}

                    if (fileSizeCheck) {
                        for (let i = 0; i < fileUpload.files.length; i++) {
                            var rowCount = $('#gridIsPersonOrVehicle  tr').length;
                            const fileExtn = fileUpload.files[i].name.split('.').pop();
                            /*if only  one jpg file is uploaded -start*/


                            if (fileSizeCheck) {
                                /* for (let i = 0; i < fileUpload.files.length; i++) {*/


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
                                document.getElementById('kvl-attachment_compliance_documents-list').append(li);

                                // upload file to server
                                const fileForm = new FormData();
                                fileForm.append('attachments', fileUpload.files.item(i))
                                fileForm.append('attach_id', attachment_id);
                                fileForm.append('report_reference', $('#ReportReference').val());
                                fileForm.append('id', $('#Id').val())


                                fileForm.append('vehicle_rego', $('#VehicleRego').val());

                                $.ajax({
                                    url: '/Guard/KeyVehiclelog?handler=ComplianceDocumensUpload',
                                    type: 'POST',
                                    data: fileForm,
                                    dataType: 'json',
                                    processData: false,
                                    contentType: false,
                                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                                }).done(function (result) {
                                    const icon = document.getElementById(result.attachmentId).getElementsByTagName('i').item(0);
                                    if (result.success) {
                                        //icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                                        //icon.title = 'Delete';
                                        GetComplianceDocumentsAttachmentLists()
                                    } else {
                                        icon.className = 'fa fa-exclamation-triangle ml-2 text-warning';
                                        icon.title = 'Error';
                                    }

                                    adjustKvlAttachmentCount(true);
                                });
                                /*}*/

                            }

                        }

                        /*$('#vkl-image-modal').modal('show');*/
                    }

                    else {
                        alert("Maximum allowed size (30Mb) exceeded for the file '" + FileName + "'")
                    }
                }
            }
            else {
                alert("Please enter the ID/Truck Registration Number");
            }

        }); 
        function GetComplianceDocumentsAttachmentLists() {
            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=ComplianceDocumentsAttachments',
                dataType: 'json',
                data:
                {
                    truck: $('#VehicleRego').val(),
                    id: $('#Id').val()
                },
                type: 'GET',
                dataType: 'json',
            }).done(function (result) {
                $("#kvl-attachment_compliance_documents-list").empty();
                for (var attachIndex = 0; attachIndex < result.length; attachIndex++) {
                    const file = result[attachIndex];
                    const attachment_id = 'attach_' + attachIndex;
                    const li = document.createElement('li');
                    li.id = attachment_id;
                    li.className = 'list-group-item';
                    li.dataset.index = attachIndex;
                    let liText = document.createTextNode(file);
                    const icon = document.createElement("i");
                    icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment_compliance_documents';
                    icon.title = 'Delete';
                    icon.style = 'cursor:pointer';
                    li.appendChild(liText);
                    li.appendChild(icon);
                    const anchorTag = document.createElement("a");
                    var id = $('#Id').val();
                    if (id == 0) {
                        anchorTag.href = '/KvlUploads/' + $('#VehicleRego').val() + "/ComplianceDocuments/" + file;
                    }
                    else {
                        anchorTag.href = '/KvlUploads/' + id + "/ComplianceDocuments/" + file;
                    }
                    anchorTag.target = "_blank";
                    const icon2 = document.createElement("i");
                    icon2.className = 'fa fa-download ml-2 text-primary';
                    icon2.title = 'Download';
                    icon2.style = 'cursor:pointer';
                    anchorTag.appendChild(icon2);
                    li.appendChild(anchorTag);
                    document.getElementById('kvl-attachment_compliance_documents-list').append(li);



                }

                $('#kvl_attachments_count').html(result.length);
            });
        }
        $('#kvl-attachment_compliance_documents-list').on('click', '.btn-delete-kvl-attachment_compliance_documents', function (event) {
            if (confirm('Are you sure want to remove this attachment?')) {
                var target = event.target;
                const fileName = target.parentNode.innerText.trim();
                const vehicleRego = $('#VehicleRego').val()
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=DeleteComplianceDocumentsAttachment',
                    type: 'POST',
                    dataType: 'json',
                    data: {
                        reportReference: $('#ReportReference').val(),
                        fileName: fileName,
                        vehicleRego: vehicleRego,
                        id: $('#Id').val()
                    },
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result) {
                        target.parentNode.parentNode.removeChild(target.parentNode);
                        GetComplianceDocumentsAttachmentLists()
                    }
                });
            }
        });
        $('#btn_save_guard_compliancedocuments').on('click', function (event) {
            $('#addCompliancesDocumentsModal').modal('hide');
        })
        /*p7 - 115 Docket Output issues - end*/
        /*to upload the other image-start*/
        function OtherImageUpload() {
            const fileUpload = $('#kvl_attachment_upload').prop('files');



            if (fileUpload.length > 0) {

                let arIndex = [];
                const attachmentList = document.getElementById('kvl-attachment-list').getElementsByTagName('li');
                for (let i = 0; i < attachmentList.length; i++)
                    arIndex.push(parseInt(attachmentList[i].getAttribute('data-index')));
                let attachIndex = arIndex.length > 0 ? Math.max(...arIndex) + 1 : 0;

                /*Maximum allowed size in bytes*/
                const maxAllowedSize = 30 * 1024 * 1024;
                var fileSizeCheck = true;
                var FileName = "";
                for (let i = 0; i < fileUpload.length; i++) {
                    const filecheck = fileUpload.item(i);
                    if (filecheck.size > maxAllowedSize) {
                        fileSizeCheck = false;
                        FileName = FileName + filecheck.name + ','
                    }
                }


                if (fileSizeCheck) {
                    /*for (let i = 0; i < fileUpload.length; i++) {*/
                    const file = fileUpload.item(0);
                    const attachment_id = 'attach_' + attachIndex;
                    const li = document.createElement('li');
                    li.id = attachment_id;
                    li.className = 'list-group-item';
                    const fileExtn = file.name.split('.').pop();
                    li.dataset.index = attachIndex;
                    let liText = document.createTextNode(file.name);;

                    const icon = document.createElement("i");
                    icon.className = 'fa fa-circle-o-notch fa-spin ml-2 text-success';
                    icon.title = 'Uploading...';
                    icon.style = 'cursor:pointer';

                    li.appendChild(liText);
                    li.appendChild(icon);
                    document.getElementById('kvl-attachment-list').append(li);

                    // upload file to server
                    const fileForm = new FormData();
                    fileForm.append('attachments', fileUpload.item(0))
                    fileForm.append('attach_id', attachment_id);
                    fileForm.append('report_reference', $('#ReportReference').val());
                    fileForm.append('vehicle_rego', $('#VehicleRego').val());


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
                            //icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                            //icon.title = 'Delete';
                            GetAttachmentLists();
                        } else {
                            icon.className = 'fa fa-exclamation-triangle ml-2 text-warning';
                            icon.title = 'Error';
                        }
                        GetPersonImage();

                        adjustKvlAttachmentCount(true);
                    });



                    //}

                }
                else {
                    alert("Maximum allowed size (30Mb) exceeded for the file '" + FileName + "'")
                }
            }

        }



        /*to upload the other image-end*/
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
                    url: '/Guard/KeyVehiclelog?handler=CompanyAndSenderNames',
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

        $('#VehicleRego ,#crmVehicleRego').typeahead({
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



        //trailer changes New change for Add rigo without plate number 21032024 dileep Start*/
        $('#Trailer1Rego').on('blur', function () {
            const vehicleRegoHasVal = $(this).val() !== '';



        });
        $('#Trailer1Rego').on('change', function () {
            const vehicleRegoHasVal = $(this).val() !== '';

            $('#Trailer1Rego_Vehicle_type').attr('disabled', !vehicleRegoHasVal);
            $('#Trailer1Rego_Vehicle_type option:selected').prop('selected', false);

            let regoToUpper = $(this).val().toUpperCase();
            $(this).val(regoToUpper);
        });
        $('#Trailer2Rego').on('change', function () {
            const vehicleRegoHasVal = $(this).val() !== '';
            $('#Trailer2Rego_Vehicle_type').attr('disabled', !vehicleRegoHasVal);
            $('#Trailer2Rego_Vehicle_type option:selected').prop('selected', false);
            let regoToUpper = $(this).val().toUpperCase();
            $(this).val(regoToUpper);
        });
        $('#Trailer3Rego').on('change', function () {
            const vehicleRegoHasVal = $(this).val() !== '';
            $('#Trailer3Rego_Vehicle_type').attr('disabled', !vehicleRegoHasVal);
            $('#Trailer3Rego_Vehicle_type option:selected').prop('selected', false);
            let regoToUpper = $(this).val().toUpperCase();
            $(this).val(regoToUpper);
        });
        $('#Trailer4Rego').on('change', function () {
            const vehicleRegoHasVal = $(this).val() !== '';
            $('#Trailer4Rego_Vehicle_type').attr('disabled', !vehicleRegoHasVal);
            $('#Trailer4Rego_Vehicle_type option:selected').prop('selected', false);
            let regoToUpper = $(this).val().toUpperCase();
            $(this).val(regoToUpper);
        });


        let gridKeyVehicleLogtrailerProfile = $('#key_vehicle_log_trailerprofiles').DataTable({
            retrieve: true,
            paging: false,
            ordering: false,
            info: false,
            searching: false,
            data: [],
            columns: [

                { data: 'id', visible: false },
                { data: 'plate' },
                { data: 'companyName' },
                { data: 'personName' },
                { data: 'personTypeText' },
                {
                    data: 'trailer1Rego',
                    render: function (value, type, data) {
                        if (value !== '' && value !== null)
                            return value + '(' + data.trailer1State + ')';
                        else
                            return ''
                    }
                },

                {
                    data: 'trailer2Rego',
                    render: function (value, type, data) {
                        if (value !== '' && value !== null)
                            return value + '(' + data.trailer2State + ')';
                        else
                            return ''
                    }
                },
                {
                    data: 'trailer3Rego',
                    render: function (value, type, data) {
                        if (value !== '' && value !== null)
                            return value + '(' + data.trailer3State + ')';
                        else
                            return ''
                    }
                },
                {
                    data: 'trailer4Rego',
                    render: function (value, type, data) {
                        if (value !== '' && value !== null)
                            return value + '(' + data.trailer4State + ')';
                        else
                            return ''
                    }
                },

                {
                    data: 'trailerTypeName',
                    render: function (value, type, data) {
                        if (value !== '' && value !== null)
                            return value;
                        else
                            return ''
                    }
                },



                {
                    targets: -1,
                    data: null,
                    className: 'text-center',
                    defaultContent: '<button id="btnSelectTrailerProfile" class="btn btn-outline-primary">Select</button>'
                },
            ],
        });



        $('#Trailer1Rego').typeahead({
            minLength: 3,
            autoSelect: true,
            source: function (request, response) {
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=TrailerRegos',
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
                        url: '/Guard/KeyVehicleLog?handler=TrailerDetails&truckRego=' + item,
                        type: 'GET',
                        dataType: 'json',
                    }).done(function (result) {
                        if (result.length > 0) {
                            gridKeyVehicleLogtrailerProfile.clear().rows.add(result).draw();
                            $('#driver_name').val('Trailer1Rego');
                            $('#duplicate_profile_status').text('');
                            $('#textBoxname').text('Trailer1Rego');

                            $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html(item);
                            $('#kvl-trailer_profiles-modal').modal('show');
                        }
                    });
                }
            }

        });


        $('#Trailer2Rego').typeahead({
            minLength: 3,
            autoSelect: true,
            source: function (request, response) {
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=TrailerRegos',
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
                        url: '/Guard/KeyVehicleLog?handler=TrailerDetails&truckRego=' + item,
                        type: 'GET',
                        dataType: 'json',
                    }).done(function (result) {
                        if (result.length > 0) {
                            gridKeyVehicleLogtrailerProfile.clear().rows.add(result).draw();
                            $('#driver_name').val('Trailer2Rego');
                            $('#duplicate_profile_status').text('');
                            $('#textBoxname').text('Trailer2Rego');
                            $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html(item);
                            $('#kvl-trailer_profiles-modal').modal('show');
                        }
                    });
                }
            }

        });


        $('#Trailer3Rego').typeahead({
            minLength: 3,
            autoSelect: true,
            source: function (request, response) {
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=TrailerRegos',
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
                        url: '/Guard/KeyVehicleLog?handler=TrailerDetails&truckRego=' + item,
                        type: 'GET',
                        dataType: 'json',
                    }).done(function (result) {
                        if (result.length > 0) {
                            gridKeyVehicleLogtrailerProfile.clear().rows.add(result).draw();
                            $('#driver_name').val('Trailer3Rego');
                            $('#duplicate_profile_status').text('');
                            $('#textBoxname').text('Trailer3Rego');
                            $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html(item);
                            $('#kvl-trailer_profiles-modal').modal('show');
                        }
                    });
                }
            }

        });


        $('#Trailer4Rego').typeahead({
            minLength: 3,
            autoSelect: true,
            source: function (request, response) {
                $.ajax({
                    url: '/Guard/KeyVehiclelog?handler=TrailerRegos',
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
                        url: '/Guard/KeyVehicleLog?handler=TrailerDetails&truckRego=' + item,
                        type: 'GET',
                        dataType: 'json',
                    }).done(function (result) {
                        if (result.length > 0) {
                            gridKeyVehicleLogtrailerProfile.clear().rows.add(result).draw();
                            $('#driver_name').val('Trailer4Rego');
                            $('#duplicate_profile_status').text('');
                            $('#textBoxname').text('Trailer4Rego');
                            $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html(item);
                            $('#kvl-trailer_profiles-modal').modal('show');
                        }
                    });
                }
            }

        });

        $('#Trailer1Rego_Vehicle_type').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {
                $('#Trailer1PlateId').val(option.val());
            }

        });
        $('#Trailer2Rego_Vehicle_type').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {
                $('#Trailer2PlateId').val(option.val());
            }

        });
        $('#Trailer3Rego_Vehicle_type').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {
                $('#Trailer3PlateId').val(option.val());
            }

        });
        $('#Trailer4Rego_Vehicle_type').on('change', function () {
            const option = $(this).find(":selected");
            if (option.val() !== '') {
                $('#Trailer4PlateId').val(option.val());
            }

        });


        $('#key_vehicle_log_trailerprofiles tbody').on('click', '#btnSelectTrailerProfile', function () {
            var data = gridKeyVehicleLogtrailerProfile.row($(this).parents('tr')).data();
            var check2 = $('#kvl-trailer_profiles-modal').find('#textBoxname').val();
            const textboxName = $('#driver_name').val();
            populateKvlModalTrailer(data.id, textboxName);
            GetVehicleImage()


        });



        function populateKvlModalTrailer(id, textBoxName) {

            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=ProfileById&id=' + id,
                type: 'GET',
                dataType: 'json',
            }).done(function (result) {
                let personName = result.personName ? result.personName : 'Unknown';


                var check1 = $('#PlateId').val();
                var check = result.keyVehicleLogProfile.plateId;
                if (check1 != 0 && check1 != '') {

                }
                else {

                    if (check != 0) {
                        $('#PlateId').val(result.keyVehicleLogProfile.plateId);
                    }

                }
                if ($('#VehicleRego').val() === '') {
                  //  $('#VehicleRego').val(result.keyVehicleLogProfile.vehicleRego);
                }

                if (!$('#kvl_list_plates').val()) {
                    $('#kvl_list_plates').val(result.keyVehicleLogProfile.plateId);
                }
                if (!$('#TruckConfig').val()) {
                    $('#TruckConfig').val(result.keyVehicleLogProfile.truckConfig);
                }
                if (!$('#TrailerType').val()) {
                    $('#TrailerType').val(result.keyVehicleLogProfile.trailerType);
                }
                if (!$('#MaxWeight').val()) {
                    $('#MaxWeight').val(result.keyVehicleLogProfile.maxWeight);
                }
                if (!$('#Trailer1Rego').val()) {
                    //$('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                }
                if (!$('#Trailer2Rego').val()) {
                   // $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                }
                if (!$('#Trailer3Rego').val()) {
                    //$('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                }
                if (!$('#Trailer4Rego').val()) {
                   // $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer4Rego);

                }
                if (!$('#CompanyName').val()) {
                    $('#CompanyName').val(result.companyName);
                }
                if (!$('#PersonName').val() ) {
                    $('#PersonName').val(personName);
                }
                if (!$('#PersonType').val()) {
                    $('#PersonType').val(result.personType);
                }
                if (!$('#MobileNumber').val() ) {
                    $('#MobileNumber').val(result.keyVehicleLogProfile.mobileNumber);
                }
                if (!$('#EntryReason').val()) {
                    $('#EntryReason').val(result.keyVehicleLogProfile.entryReason);
                }
                if (!$('#Product').val()) {
                    $('#Product').val(result.keyVehicleLogProfile.product);
                }
                if (!$('#Notes').val()) {
                    $('#Notes').val(result.keyVehicleLogProfile.notes);
                }
                //=========================================
                $("#list_product").val(result.keyVehicleLogProfile.product);
                $("#list_product").trigger('change');
                $('#Sender').val(result.sender);
                $('#lblIsSender').text(result.isSender ? 'Sender Address' : 'Reciever Address');
                $('#cbIsSender').prop('checked', result.isSender);
                //for checking whether the person is under scam or not(jisha james)
                $('#PersonOfInterest').val(result.personOfInterest)
                if ($('#PersonOfInterest').val() != '') {
                    $('#titlePOIWarning').attr('hidden', false);
                    $('#imagesiren').attr('hidden', false);


                }
                else {
                    $('#titlePOIWarning').attr('hidden', true);
                    $('#imagesiren').attr('hidden', true);
                }
                /*to load the common fields to crmtab -start*/
                $('#crm_list_plates').val(result.keyVehicleLogProfile.plateId);
                $('#crmTruckConfig').val(result.keyVehicleLogProfile.truckConfig);
                $('#crmTrailerType').val(result.keyVehicleLogProfile.trailerType);

                $('#IndividualTitle').val(result.individualTitle);
                $('#Gender').val(result.gender);
                $('#crmCompanyABN').val(result.companyABN);
                if (result.companyLandline == null)
                    $('#LandLineNumber').val('+61 (0) ');
                else
                    $('#LandLineNumber').val(result.companyLandline);
                $('#Email').val(result.email);
                $('#Website').val(result.website);
                /*for cheking  the BDM is true-start*/
                let isBDM = $('#IsBDM').val(result.isBDM);
                $('#cbIsBDMOrSales').prop('checked', result.isBDM);

                if (result.isBDM == true) {
                    $('#lblIsBDMOrSales').text('BDM/Sales');
                    $('#list_BDM').prop('hidden', false);
                }
                else {
                    $('#lblIsBDMOrSales').text('Supplier/Partner');
                    $('#list_BDM').prop('hidden', false);
                }



                /*for cheking  the BDM is true-end*/
                $('#IsCRMId').val(result.bdmList);
                var checkedornot = $('#IsCRMId').val();

                /* to load the selected items in BDM-start*/
                $("#list_BDM  input[type=checkbox]").each(function () {
                    crmindivid = $(this).closest('li').find('#IsCRMIndividualId').val();
                    if (checkedornot.indexOf(crmindivid) != -1) {
                        $(this).prop('checked', true);
                    }
                    else {
                        $(this).prop('checked', false);
                    }

                });
                /*to load the plate to crmtab -end*/

                const isChecked = $('#chbIsPOIAlert1').is(':checked');
                if (result.keyVehicleLogProfile.vehicleRego != null) {
                    loadAuditHistory(result.keyVehicleLogProfile.vehicleRego);

                }
                else {
                    loadAuditHistoryWithProfileId(result.keyVehicleLogProfile.id);
                }

                GetPersonImage()


                /* For attachements Start  */
                $("#kvl-attachment-list").empty();
                for (var attachIndex = 0; attachIndex < result.attachments.length; attachIndex++) {
                    const file = result.attachments[attachIndex];
                    const attachment_id = 'attach_' + attachIndex;
                    const li = document.createElement('li');
                    li.id = attachment_id;
                    li.className = 'list-group-item';
                    li.dataset.index = attachIndex;
                    let liText = document.createTextNode(file);
                    const icon = document.createElement("i");
                    icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
                    icon.title = 'Delete';
                    icon.style = 'cursor:pointer';
                    li.appendChild(liText);
                    li.appendChild(icon);
                    const anchorTag = document.createElement("a");
                    anchorTag.href = '/KvlUploads/' + $('#VehicleRego').val() + "/" + file;
                    anchorTag.target = "_blank";
                    const icon2 = document.createElement("i");
                    icon2.className = 'fa fa-download ml-2 text-primary';
                    icon2.title = 'Download';
                    icon2.style = 'cursor:pointer';
                    anchorTag.appendChild(icon2);
                    li.appendChild(anchorTag);
                    document.getElementById('kvl-attachment-list').append(li);



                }

                var Trailer1PlateId = $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html();

                $('#kvl_attachments_count').html(result.attachments.length);
                /* For attachements end  */
                //traliler changes New change for Add rigo without plate number 21032024 dileep Start
                if (!$('#Trailer1PlateId').val()) {
                    if (textBoxName === 'Trailer1Rego') {
                        $('#Trailer1PlateId').val(result.keyVehicleLogProfile.trailer1PlateId);
                    }
                }
                if (!$('#Trailer1Rego_Vehicle_type').val()) {
                    if (textBoxName === 'Trailer1Rego') {
                        $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);

                    }
                }
                if (!$('#Trailer2PlateId').val()) {
                    if (textBoxName === 'Trailer2Rego') {
                        $('#Trailer2PlateId').val(result.keyVehicleLogProfile.trailer2PlateId);
                    }
                }
                if (!$('#Trailer2Rego_Vehicle_type').val()) {
                    if (textBoxName === 'Trailer2Rego') {
                        $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                    }
                }
                if (!$('#Trailer3PlateId').val()) {
                    if (textBoxName === 'Trailer3Rego') {
                        $('#Trailer3PlateId').val(result.keyVehicleLogProfile.trailer3PlateId);
                    }
                }
                if (!$('#Trailer3Rego_Vehicle_type').val()) {
                    if (textBoxName === 'Trailer3Rego') {
                        $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                    }
                }
                if (!$('#Trailer4PlateId').val()) {
                    if (textBoxName === 'Trailer4Rego') {
                        $('#Trailer4PlateId').val(result.keyVehicleLogProfile.trailer4PlateId);
                    }
                }
                if (!$('#Trailer4Rego_Vehicle_type').val()) {
                    if (textBoxName === 'Trailer4Rego') {
                        $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                    }
                }
                if ($('#Trailer1Rego').val() !== '') {

                    $('#Trailer1Rego_Vehicle_type').attr('disabled', false);
                }
                if ($('#Trailer2Rego').val() !== '') {
                    $('#Trailer2Rego_Vehicle_type').attr('disabled', false);
                }
                if ($('#Trailer3Rego').val() !== '') {
                    $('#Trailer3Rego_Vehicle_type').attr('disabled', false);
                }
                if ($('#Trailer4Rego').val() !== '') {
                    $('#Trailer4Rego_Vehicle_type').attr('disabled', false);
                }


                if (result.keyVehicleLogProfile.trailer1PlateId === null
                ) {
                    if (textBoxName === 'Trailer1Rego') {

                        if (!$('#VehicleRego').val()) {
                            $('#VehicleRego').val('');
                            $('#kvl_list_plates').val('');
                            $('#kvl_list_plates').attr('disabled', true);
                        }



                        if ($('#Trailer1Rego').val() === result.keyVehicleLogProfile.trailer1Rego) {
                            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                            $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                        }
                        if ($('#Trailer1Rego').val() === result.keyVehicleLogProfile.trailer2Rego) {
                            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                            $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                        }
                        if ($('#Trailer1Rego').val() === result.keyVehicleLogProfile.trailer3Rego) {
                            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                            $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                        }
                        if ($('#Trailer1Rego').val() === result.keyVehicleLogProfile.trailer4Rego) {
                            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer4Rego);
                            $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                        }
                        if (result.keyVehicleLogProfile.vehicleRego !== null) {
                            if (result.keyVehicleLogProfile.vehicleRego == $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html()) {
                                $('#Trailer1Rego').val(result.keyVehicleLogProfile.vehicleRego);
                                $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.plateId);
                            }
                        }
                      
                    }
                }
                else {

                    if (textBoxName === 'Trailer1Rego') {
                        if (!$('#VehicleRego').val()) {
                            $('#VehicleRego').val('');
                            $('#kvl_list_plates').val('');
                            $('#kvl_list_plates').attr('disabled', true);
                        }



                        if ($('#Trailer1Rego').val() === result.keyVehicleLogProfile.trailer1Rego) {
                            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                            $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                        }
                        if ($('#Trailer1Rego').val() === result.keyVehicleLogProfile.trailer2Rego) {
                            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                            $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                        }
                        if ($('#Trailer1Rego').val() === result.keyVehicleLogProfile.trailer3Rego) {
                            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                            $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                        }
                        if ($('#Trailer1Rego').val() === result.keyVehicleLogProfile.trailer4Rego) {
                            $('#Trailer1Rego').val(result.keyVehicleLogProfile.trailer4Rego);
                            $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                        }
                        if (result.keyVehicleLogProfile.vehicleRego !== null)
                        {

                            if (result.keyVehicleLogProfile.vehicleRego == $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html())
                            {
                                $('#Trailer1Rego').val(result.keyVehicleLogProfile.vehicleRego);
                                $('#Trailer1Rego_Vehicle_type').val(result.keyVehicleLogProfile.plateId);
                            }
                        }
                    }


                }
                if (result.keyVehicleLogProfile.trailer2PlateId === null
                ) {
                    if (textBoxName === 'Trailer2Rego') {
                        if (!$('#VehicleRego').val()) {
                            $('#VehicleRego').val('');
                            $('#kvl_list_plates').val('');
                            $('#kvl_list_plates').attr('disabled', true);
                        }

                        if ($('#Trailer2Rego').val() === result.keyVehicleLogProfile.trailer1Rego) {
                            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                            $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                        }
                        if ($('#Trailer2Rego').val() === result.keyVehicleLogProfile.trailer2Rego) {
                            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                            $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                        }
                        if ($('#Trailer2Rego').val() === result.keyVehicleLogProfile.trailer3Rego) {
                            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                            $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                        }
                        if ($('#Trailer2Rego').val() === result.keyVehicleLogProfile.trailer4Rego) {
                            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer4Rego);
                            $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                        }
                        if (result.keyVehicleLogProfile.vehicleRego !== null) {
                            if (result.keyVehicleLogProfile.vehicleRego == $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html()) {
                                $('#Trailer2Rego').val(result.keyVehicleLogProfile.vehicleRego);
                                $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.plateId);
                            }
                        }
                    }
                }
                else {
                    if (textBoxName === 'Trailer2Rego') {
                        if (!$('#VehicleRego').val()) {
                            $('#VehicleRego').val('');
                            $('#kvl_list_plates').val('');
                            $('#kvl_list_plates').attr('disabled', true);
                        }

                        if ($('#Trailer2Rego').val() === result.keyVehicleLogProfile.trailer1Rego) {
                            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                            $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                        }
                        if ($('#Trailer2Rego').val() === result.keyVehicleLogProfile.trailer2Rego) {
                            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                            $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                        }
                        if ($('#Trailer2Rego').val() === result.keyVehicleLogProfile.trailer3Rego) {
                            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                            $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                        }
                        if ($('#Trailer2Rego').val() === result.keyVehicleLogProfile.trailer4Rego) {
                            $('#Trailer2Rego').val(result.keyVehicleLogProfile.trailer4Rego);
                            $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                        }
                        if (result.keyVehicleLogProfile.vehicleRego !== null) {
                            if (result.keyVehicleLogProfile.vehicleRego == $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html()) {
                                $('#Trailer2Rego').val(result.keyVehicleLogProfile.vehicleRego);
                                $('#Trailer2Rego_Vehicle_type').val(result.keyVehicleLogProfile.plateId);
                            }
                        }
                    }
                }
                if (result.keyVehicleLogProfile.trailer3PlateId === null
                ) {
                    if (textBoxName === 'Trailer3Rego') {
                        if (!$('#VehicleRego').val()) {
                            $('#VehicleRego').val('');
                            $('#kvl_list_plates').val('');
                            $('#kvl_list_plates').attr('disabled', true);
                        }

                        if ($('#Trailer3Rego').val() === result.keyVehicleLogProfile.trailer1Rego) {
                            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                            $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                        }
                        if ($('#Trailer3Rego').val() === result.keyVehicleLogProfile.trailer2Rego) {
                            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                            $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                        }
                        if ($('#Trailer3Rego').val() === result.keyVehicleLogProfile.trailer3Rego) {
                            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                            $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                        }
                        if ($('#Trailer3Rego').val() === result.keyVehicleLogProfile.trailer4Rego) {
                            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer4Rego);
                            $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                        }
                        if (result.keyVehicleLogProfile.vehicleRego !== null) {
                            if (result.keyVehicleLogProfile.vehicleRego == $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html()) {
                                $('#Trailer3Rego').val(result.keyVehicleLogProfile.vehicleRego);
                                $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.plateId);
                            }
                        }

                       
                    }
                }
                else {
                    if (textBoxName === 'Trailer3Rego') {
                        if (!$('#VehicleRego').val()) {
                            $('#VehicleRego').val('');
                            $('#kvl_list_plates').val('');
                            $('#kvl_list_plates').attr('disabled', true);
                        }

                        if ($('#Trailer3Rego').val() === result.keyVehicleLogProfile.trailer1Rego) {
                            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                            $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                        }
                        if ($('#Trailer3Rego').val() === result.keyVehicleLogProfile.trailer2Rego) {
                            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                            $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                        }
                        if ($('#Trailer3Rego').val() === result.keyVehicleLogProfile.trailer3Rego) {
                            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                            $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                        }
                        if ($('#Trailer3Rego').val() === result.keyVehicleLogProfile.trailer4Rego) {
                            $('#Trailer3Rego').val(result.keyVehicleLogProfile.trailer4Rego);
                            $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                        }
                        if (result.keyVehicleLogProfile.vehicleRego !== null) {
                            if (result.keyVehicleLogProfile.vehicleRego == $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html()) {
                                $('#Trailer3Rego').val(result.keyVehicleLogProfile.vehicleRego);
                                $('#Trailer3Rego_Vehicle_type').val(result.keyVehicleLogProfile.plateId);
                            }
                        }
                    }
                }
                if (result.keyVehicleLogProfile.trailer4PlateId === null
                ) {
                    if (textBoxName === 'Trailer4Rego') {
                        if (!$('#VehicleRego').val()) {
                            $('#VehicleRego').val('');
                            $('#kvl_list_plates').val('');
                            $('#kvl_list_plates').attr('disabled', true);
                        }

                        if ($('#Trailer4Rego').val() === result.keyVehicleLogProfile.trailer1Rego) {
                            $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                            $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                        }
                        if ($('#Trailer4Rego').val() === result.keyVehicleLogProfile.trailer2Rego) {
                            $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                            $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                        }
                        if ($('#Trailer4Rego').val() === result.keyVehicleLogProfile.trailer3Rego) {
                            $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                            $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                        }
                        if ($('#Trailer4Rego').val() === result.keyVehicleLogProfile.trailer4Rego) {
                            $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer4Rego);
                            $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                        }
                        if (result.keyVehicleLogProfile.vehicleRego !== null) {
                            if (result.keyVehicleLogProfile.vehicleRego == $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html()) {
                                $('#Trailer4Rego').val(result.keyVehicleLogProfile.vehicleRego);
                                $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.plateId);
                            }
                        }
                        
                    }
                    else {
                        if (textBoxName === 'Trailer4Rego') {
                            if (!$('#VehicleRego').val()) {
                                $('#VehicleRego').val('');
                                $('#kvl_list_plates').val('');
                                $('#kvl_list_plates').attr('disabled', true);
                            }

                            if ($('#Trailer4Rego').val() === result.keyVehicleLogProfile.trailer1Rego) {
                                $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer1Rego);
                                $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer1PlateId);
                            }
                            if ($('#Trailer4Rego').val() === result.keyVehicleLogProfile.trailer2Rego) {
                                $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer2Rego);
                                $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer2PlateId);
                            }
                            if ($('#Trailer4Rego').val() === result.keyVehicleLogProfile.trailer3Rego) {
                                $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer3Rego);
                                $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer3PlateId);
                            }
                            if ($('#Trailer4Rego').val() === result.keyVehicleLogProfile.trailer4Rego) {
                                $('#Trailer4Rego').val(result.keyVehicleLogProfile.trailer4Rego);
                                $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.trailer4PlateId);
                            }
                            if (result.keyVehicleLogProfile.vehicleRego !== null) {
                                if (result.keyVehicleLogProfile.vehicleRego == $('#kvl-trailer_profiles-modal').find('#kvl-profile-title-rego').html()) {
                                    $('#Trailer4Rego').val(result.keyVehicleLogProfile.vehicleRego);
                                    $('#Trailer4Rego_Vehicle_type').val(result.keyVehicleLogProfile.plateId);
                                }
                            }
                        }
                    }
                }
                //traliler changes New change for Add rigo without plate number 21032024 dileep end


            });
            $('#kvl-profiles-modal').modal('hide');

            $('#kvl-trailer_profiles-modal').modal('hide');

        }

        //trailer changes New change for Add rigo without plate number 21032024 dileep end*/

        /*same changes in vehicle rego-end*/
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
            '<tr>' +
            '<td colspan="8" rowspan="2">' + convertDbString(d.detail.notes) + '</td>' +
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
                d.poi = $('#kvlProfileIsPOIBDMSupplier').find(':selected').val();
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

            { data: 'detail.poiOrBDM', width: '2%' },
            { data: 'detail.companyName', width: '8%' },
            { data: 'detail.personName', orderable: false, width: '8%' },
            { data: 'clientSite', orderable: false, width: '8%' },
            { data: 'truckConfigText', orderable: false, width: '8%' },
            { data: 'trailerTypeText', orderable: false, width: '8%' },
            {
                orderable: false,
                width: '10%',
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
    $('#kvlProfileIsPOIBDMSupplier').select2({
        theme: 'bootstrap4',
        ajax: {
            url: '/Admin/AuditSiteLog?handler=POIBDMSupplier',
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
        /*To get the text inside the product dropdown*/
        var inputElement = document.querySelector(".es-input");
        // Get the value of the input element
        if (inputElement) { var inputValue = inputElement.value; $('#ProductOther').val(inputValue); }
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

        if (isNewEntry) {
            $('#list_product_profile').val('');
            $('#KeyVehicleLogProfile_Product').val();
            var ulElement = document.querySelector('ul.es-list');

            // Check if the <ul> element exists
            if (ulElement) {
                // Get all <li> elements within the <ul> element
                var listItems = ulElement.querySelectorAll('li');

                // Loop through each <li> element
                listItems.forEach(function (item) {
                    // Remove the 'style' attribute
                    item.removeAttribute('style');
                    item.classList.add('es-visible');
                });
            }

        }
        else {

            $('#list_product_profile').val('');
            var ulElement = document.querySelector('ul.es-list');

            // Check if the <ul> element exists
            if (ulElement) {
                // Get all <li> elements within the <ul> element
                var listItems = ulElement.querySelectorAll('li');

                // Loop through each <li> element
                listItems.forEach(function (item) {
                    // Remove the 'style' attribute
                    item.removeAttribute('style');
                    item.classList.add('es-visible');
                });
            }


        }




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
    /*p1-196 rationalization of menu changes-start*/
    //let gridKvlFields;
    //gridKvlFields = $('#tbl_kvl_fields').grid({
    //    dataSource: '/Admin/GuardSettings?handler=KeyVehcileLogFields',
    //    uiLibrary: 'bootstrap4',
    //    iconsLibrary: 'fontawesome',
    //    primaryKey: 'id',
    //    inlineEditing: { mode: 'command' },
    //    columns: [
    //        { field: 'name', title: 'Name', width: 200, editor: true },
    //    ],
    //    initialized: function (e) {
    //        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    //    }
    //});
  
    //let isKvlFieldAdding = false;
    //$('#add_kvl_fields').on('click', function () {
    //    const selFieldTypeId = $('#kvl_fields_types').val();
    //    if (!selFieldTypeId) {
    //        alert('Please select a field type to update');
    //        return;
    //    }
    //    var rowCount = $('#tbl_kvl_fields tr').length;

    //    if (selFieldTypeId == '9' && rowCount == 8) {
    //        alert('Maximum number of CRM/BDM Activity exeeded');
    //        return;
    //    }

    //    if (isKvlFieldAdding) {
    //        alert('Unsaved changes in the grid. Refresh the page');
    //    } else {
    //        isKvlFieldAdding = true;
    //        gridKvlFields.addRow({
    //            'id': -1,
    //            'typeId': selFieldTypeId,
    //            'name': '',
    //        }).edit(-1);
    //    }
    //});

    //$('#kvl_fields_types').on('change', function () {
    //    const selKvlFieldTypeId = $('#kvl_fields_types').val();
    //    gridKvlFields.clear();
    //    gridKvlFields.reload({ typeId: selKvlFieldTypeId });
    //});

    //if (gridKvlFields) {
    //    gridKvlFields.on('rowDataChanged', function (e, id, record) {
    //        const data = $.extend(true, {}, record);
    //        $.ajax({
    //            url: '/Admin/GuardSettings?handler=SaveKeyVehicleLogField',
    //            data: { record: data },
    //            type: 'POST',
    //            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //        }).done(function (result) {
    //            if (result.success) gridKvlFields.reload({ typeId: $('#kvl_fields_types').val() });
    //            else alert(result.message);
    //        }).fail(function () {
    //            console.log('error');
    //        }).always(function () {
    //            if (isKvlFieldAdding)
    //                isKvlFieldAdding = false;
    //        });
    //    });

    //    gridKvlFields.on('rowRemoving', function (e, id, record) {
    //        if (confirm('Are you sure want to delete this field?')) {
    //            $.ajax({
    //                url: '/Admin/GuardSettings?handler=DeleteKeyVehicleLogField',
    //                data: { id: record },
    //                type: 'POST',
    //                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //            }).done(function (result) {
    //                if (result.success) gridKvlFields.reload({ typeId: $('#kvl_fields_types').val() });
    //                else alert(result.message);
    //            }).fail(function () {
    //                console.log('error');
    //            }).always(function () {
    //                if (isKvlFieldAdding)
    //                    isKvlFieldAdding = false;
    //            });
    //        }
    //    });
    //}
    /* p1 - 196 rationalization of menu changes - end*/

    /****** Print manual docket *******/
    $('#vehicle_key_daily_log tbody').on('click', '#btnPrintVkl', function () {
        var data1 = keyVehicleLog.row($(this).parents('tr')).data();
        
        var clientsiteid = $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val();
        $('#clientsiteIDNew').val(clientsiteid);

        const clientSiteControlKeyvehicle = $('#multiselectVehiclelogDocket');
        clientSiteControlKeyvehicle.html('');
        var siteidd = "";
        var data = keyVehicleLog.row($(this).parents('tr')).data();
        $('#stakeholderEmail').val('');
        //p7-114 Docket Email for poc-start for batch docket
        var uncheckedCount = 0;
        $("#vehicle_key_daily_log  input[type=checkbox]").each(function () {
            var isChecked1 = $(this).is(':checked');
            if (isChecked1 == true) {
                uncheckedCount++;
            }



        });
        //p7-114 Docket Email for poc-end for batch docket
        //To get the Drodown values start
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=POCSDropdown',
            type: 'GET',
            data: { 'clientsiteID': clientsiteid },
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    var option = '<option value="' + site.value + '">' + site.text + '</option>';

                    $.ajax({
                        url: '/Guard/KeyVehicleLog?handler=GetKeyvehicleemails',
                        data: {
                            id: data1.detail.id
                        },
                        type: 'POST',
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    }).done(function (response) {
                        siteidd = response.poc.clientSitePocIdsVehicleLog;
                        //p7-114 Docket Email for poc start -jisha check when poc is selected for the entry
                        if (siteidd != null) {
                            var selectedValues = siteidd.split(',');
                            $('#EmailPopup').val(siteidd);
                            //p7-114 Docket Email for poc-start for batch docket
                            if (uncheckedCount <= 1) { 
                                selectedValues.forEach(function (value) {
                                    if (value === site.value) {
                                        option = '<option value="' + value + '" selected>' + site.text + '</option>';
                                    }
                                });
                            }
                            //p7-114 Docket Email for poc-end for batch docket
                        }
                        //p7-114 Docket Email for poc end -jisha check when poc is selected for the entry
                        // Append the option after the inner AJAX call is done
                        clientSiteControlKeyvehicle.append(option);
                        clientSiteControlKeyvehicle.multiselect('rebuild');
                    });
                });
            }
        });
         //To get the Drodown values stop

       

        var checkedCount = $('.custom-control-input:checked').length;
        if (uncheckedCount > 1) {


            $('#stakeholderEmail').val('');
          
           

        }
        else {
            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=GetKeyvehicleemails',
                data: {

                    id: data.detail.id
                },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (response) {
                var emailCompany = response.poc.emailindividual;
                var emailIndividual = response.poc.emailCompany;
                if (emailCompany) {
                    var concatenatedEmails = emailCompany;

                    if (emailIndividual) {
                        concatenatedEmails += ", " + emailIndividual;
                    }

                    $('#stakeholderEmail').val(concatenatedEmails);
                } else {

                    $('#stakeholderEmail').val(emailIndividual);
                }
                //p7-114 Docket Email for poc-start for single docket
                if (response.poc.clientSitePocIdsVehicleLog != null) {
                    GetEmails(response.poc.clientSitePocIdsVehicleLog);
                    }
                //p7 - 114 Docket Email for poc - end
            });
        }

        $('#printDocketForKvlId').val('');
        $('#generate_kvl_docket_status').hide();
        $('#download_kvl_docket').hide();
        $('.print-docket-reason').prop('checked', false);
        $('#cbxProofOfDelivery').prop('checked', true);
        $('#otherReason').val('');
        $('#otherReason').attr('disabled', true);
        $('#stakeholderEmail').val('');
        $('#generate_kvl_docket').show();
        $('#generate_kvl_AlldocketList').hide();
       

        /* Save the check box values to hidden field to print Start*/
        var data2 = keyVehicleLog.rows().nodes();
        var Ids = '';
        $.each(data2, function (index, value) {
            if ($(this).find('input').prop('checked')) {
                if (Ids === "") {
                    var checkboxId = $(this).find('input').attr('id');
                    Ids = Ids + checkboxId;
                } else {
                    var checkboxId2 = $(this).find('input').attr('id');
                    Ids = Ids + "," + checkboxId2;
                }

            }
        });
        $('#printDocketForKvlId').val(Ids);
        /* Save the check box values to hidden field to print end*/
        var data = keyVehicleLog.row($(this).parents('tr')).data();
        $('#print-manual-docket-modal').modal('show')
        if ($('#printDocketForKvlId').val() === "") {
            $('#printDocketForKvlId').val(data.detail.id);
        }
        if (data.detail.personOfInterest != null) {
            $('#titlePOIWarningPrint').attr('hidden', false);
            $('#imagesirenprint').attr('hidden', false);
        }
        else {
            $('#titlePOIWarningPrint').attr('hidden', true);
            $('#imagesirenprint').attr('hidden', true);
        }
        
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
    function GetEmails(id) {


        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=GetPOCEmailsSelected',
            data: {

                Emails: id
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            if (response.pocEmails != null) {
                var emails = response.pocEmails.filter(function (email) {
                    return email !== null;
                }).join(', ');
                $('#stakeholderEmail').val(function (_, val) {
                    if (val && val.trim() !== '') {
                        return val + ', ' + emails;
                    } else {

                        return emails;
                    }
                });
            }

        });



    }

    $('#multiselectVehiclelogDocket').on('change', function () {
        var IdPrint = $('#printDocketForKvlId').val();

         var Emaillen = $('#multiselectVehiclelogDocket').val().join(',');
       
        $('#stakeholderEmail').val('');
        
            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=GetKeyvehicleemails',
                data: {

                    id: IdPrint
                },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (response) {
                var emailCompany = response.poc.emailindividual;
                var emailIndividual = response.poc.emailCompany;
                if (emailCompany) {
                    var concatenatedEmails = emailCompany;

                    if (emailIndividual) {
                        concatenatedEmails += ", " + emailIndividual;
                    }

                    $('#stakeholderEmail').val(concatenatedEmails);
                } else {

                    $('#stakeholderEmail').val(emailIndividual);
                }
                if (Emaillen != null && Emaillen!="") {
                    GetEmails(Emaillen);
                }
                
            });



    });



    $('#otherReason').attr('placeholder', 'Select Or Edit').editableSelect({
        effects: 'slide'
    }).on('select.editable-select', function (e, li) {
        $('#download_kvl_docket').hide();
    });

    $('#generate_kvl_docket').on('click', function () {
        $('#generate_kvl_docket_status').hide();
      
        var checkboxIdsArray = $('#printDocketForKvlId').val().split(',');
        const checkedReason = $('.print-docket-reason:checkbox:checked');
        if (checkedReason.length === 0) {
            $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger"></i> Please select a reason').show();
            return false;
        }
        $('#generate_kvl_docket_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Manual Docket. Please wait...').show();
        $('#download_kvl_docket').hide();
        $('#generate_kvl_docket').attr('disabled', true);
        /* to check pdf binder is on -start*/
        const pdfbinder = $('#IsPDFBinderOn').val();
        if (pdfbinder == '') {
            $('#IsPDFBinderOn').val(false);
        }
        /* to check pdf binder is on - end*/
        /* Manual docket generation Single Start */
        if (checkboxIdsArray.length != 0) {

            if (checkboxIdsArray.length == 1) {
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
                        var data2 = keyVehicleLog.rows().nodes();
                        var Ids = '';
                        $.each(data2, function (index, value) {
                            if ($(this).find('input').prop('checked')) {
                                $(this).find('input[type="checkbox"]').prop('checked', false);

                            }
                        });
                        $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger mr-2"></i> Error generating report').show();
                    }
                    else {
                        var data2 = keyVehicleLog.rows().nodes();
                        var Ids = '';
                        $.each(data2, function (index, value) {
                            if ($(this).find('input').prop('checked')) {
                                $(this).find('input[type="checkbox"]').prop('checked', false);

                            }
                        });
                        $('#generate_kvl_docket').attr('disabled', false);
                        /*for changing the button name-start*/
                        $('#download_kvl_docket').show();
                        //const isChecked = $('#chb_IsPDFBinder').is(':checked');
                        //if (isChecked == true) {
                        //    $('#download_kvl_docket').text('Download ZIP')
                        //}
                        //else {
                        //    $('#download_kvl_docket').text('Download PDF')
                        //}
                        $('#chkAllBatchDocketSelect').prop('checked', false);

                        /*for changing the button name - end*/
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
            }
            /* Manual docket generation Single end */
            else {
                /* Multiple dockets*/
                $.ajax({
                    url: '/Guard/KeyVehicleLog?handler=GenerateManualDocketBulk',
                    data: {
                        IsGlobal: false,
                        option: $(checkedReason).val(),
                        otherReason: $('#otherReason').val(),
                        stakeholderEmails: $('#stakeholderEmail').val(),
                        clientSiteId: $('#ClientSiteID').val(),
                        blankNoteOnOrOff: $('#IsBlankNoteOn').val(),
                        ids: checkboxIdsArray,
                        pdfBinderOnOrOff: $('#IsPDFBinderOn').val(),
                    },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (response) {

                    if (response.statusCode === -1) {
                        var data2 = keyVehicleLog.rows().nodes();
                        var Ids = '';
                        $.each(data2, function (index, value) {
                            if ($(this).find('input').prop('checked')) {
                                $(this).find('input[type="checkbox"]').prop('checked', false);

                            }
                        });
                        const isChecked = $('#chb_IsPDFBinder').is(':checked');
                        if (isChecked == false) {
                            $('#download_kvl_docket').text('Download ZIP')
                        }
                        else {
                            $('#download_kvl_docket').text('Download PDF')
                        }
                        $('#chkAllBatchDocketSelect').prop('checked', false);
                        $("#generate_kvl_AlldocketList").removeAttr("disabled");
                        $("#generate_kvl_docket").removeAttr("disabled");
                        $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger mr-2"></i> Error generating report').show();
                    }
                    else {


                        var data2 = keyVehicleLog.rows().nodes();
                        var Ids = '';
                        $.each(data2, function (index, value) {
                            if ($(this).find('input').prop('checked')) {
                                $(this).find('input[type="checkbox"]').prop('checked', false);

                            }
                            const isChecked = $('#chb_IsPDFBinder').is(':checked');
                            if (isChecked == false) {
                                $('#download_kvl_docket').text('Download ZIP')
                            }
                            else {
                                $('#download_kvl_docket').text('Download PDF')
                            }
                            $('#chkAllBatchDocketSelect').prop('checked', false);
                        });
                        $("#generate_kvl_docket").removeAttr("disabled");
                        // $('#generate_kvl_AlldocketList').attr('disabled', false);
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


            }
        }


    });


    $("#print-manual-docket-modal").on("hidden.bs.modal", function () {

        var data2 = keyVehicleLog.rows().nodes();
        var Ids = '';
        $.each(data2, function (index, value) {
            if ($(this).find('input').prop('checked')) {
                $(this).find('input[type="checkbox"]').prop('checked', false);

            }
        });
        // put your default event here
    });
    //To Generate All PO List start
    $('#btn_kvl_pdf_POIList').on('click', function () {
        var clientsiteid = $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val();
        $('#clientsiteIDNew').val(clientsiteid);

   
        const clientSiteControlKeyvehicle = $('#multiselectVehiclelogDocket');
        

        clientSiteControlKeyvehicle.html('');
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=POCSDropdown',
            type: 'GET',
            data: { 'clientsiteID': clientsiteid },
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    clientSiteControlKeyvehicle.append('<option value="' + site.value + '">' + site.text + '</option>');
                });
                clientSiteControlKeyvehicle.multiselect('rebuild');
            }
        });


        $('#generate_kvl_docket_status').hide();
        $('#download_kvl_docket').hide();
        $('.print-docket-reason').prop('checked', false);
        $('#cbxProofOfDelivery').prop('checked', false);
        $('#cbxPOIList').prop('checked', true);
        $('#otherReason').val('');
        $('#otherReason').attr('disabled', true);
        $('#stakeholderEmail').val('');

        var data = keyVehicleLog.row($(this).parents('tr')).data();
        $('#generate_kvl_AlldocketList').show();
        $('#generate_kvl_docket').hide();
        $('#print-manual-docket-modal').modal('show')
        //$('#printDocketForKvlId').val(data.detail.id);
        //if (data.detail.personOfInterest != null) {
        //    $('#titlePOIWarningPrint').attr('hidden', false);
        //    $('#imagesirenprint').attr('hidden', false);
        //}
        //else {
        //    $('#titlePOIWarningPrint').attr('hidden', true);
        //    $('#imagesirenprint').attr('hidden', true);
        //}
    });

    
    $('#multiselectVehiclelogDocket').multiselect({
        maxHeight: 400,
        buttonWidth: '350px',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });
    
    $('#generate_kvl_AlldocketList').on('click', function () {
        $('#generate_kvl_docket_status').hide();
        var SitePOCIds = $('#multiselectVehiclelogDocket').val();
        const checkedReason = $('.print-docket-reason:checkbox:checked');
        if (checkedReason.length === 0) {
            $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger"></i> Please select a reason').show();
            return false;
        }
        $('#generate_kvl_docket_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Manual Docket. Please wait...').show();
        $('#download_kvl_docket').hide();
        $('#generate_kvl_AlldocketList').attr('disabled', true);
        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=GenerateManualDocketList',
            data: {
                IsGlobal: false,
                option: $(checkedReason).val(),
                otherReason: $('#otherReason').val(),
                stakeholderEmails: $('#stakeholderEmail').val(),
                clientSiteId: $('#ClientSiteID').val(),
                blankNoteOnOrOff: $('#IsBlankNoteOn').val(),
                ids: ids,
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {

            if (response.statusCode === -1) {
                $("#generate_kvl_AlldocketList").removeAttr("disabled");
                $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger mr-2"></i> Any POI not found for the login site').show();
            }
            else {

                $("#generate_kvl_AlldocketList").removeAttr("disabled");
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







    /* });*/
    //$('#generate_kvl_AlldocketList').on('click', function () {
    //    $('#generate_kvl_docket_status').hide();

    //    const checkedReason = $('.print-docket-reason:checkbox:checked');
    //    if (checkedReason.length === 0) {
    //        $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger"></i> Please select a reason').show();
    //        return false;
    //    }
    //    $('#generate_kvl_docket_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Manual Docket. Please wait...').show();
    //    $('#download_kvl_docket').hide();
    //    $('#generate_kvl_AlldocketList').attr('disabled', true);
    //    var dataTable = $('#vehicle_key_daily_log').DataTable();
    //    var ids = [];
    //    dataTable.rows().data().each(function (index, rowData) {

    //        ids.push(index.detail.id);
    //    });
    //        $.ajax({
    //            url: '/Guard/KeyVehicleLog?handler=GenerateManualDocketList',
    //            data: {
    //                id: ids,
    //                option: $(checkedReason).val(),
    //                otherReason: $('#otherReason').val(),
    //                stakeholderEmails: $('#stakeholderEmail').val(),
    //                clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
    //                blankNoteOnOrOff: $('#IsBlankNoteOn').val(),
    //                ids: ids,
    //            },
    //            type: 'POST',
    //            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //        }).done(function (response) {
    //            if (response.statusCode === -1) {
    //                $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger mr-2"></i> Error generating report').show();
    //            }
    //            else {
    //                $('#generate_kvl_AlldocketList').attr('disabled', false);
    //                $('#download_kvl_docket').show();
    //                $('#download_kvl_docket').attr('href', response.fileName);

    //                let statusClass = 'fa-check-circle-o text-success mr-2';
    //                let statusMessage = 'A copy of the docket is on the Dropbox, and where applicable, has been emailed to relevant stakeholders';
    //                if (response.statusCode !== 0) {
    //                    statusClass = 'fa-exclamation-triangle text-warning mr-2';
    //                    statusMessage = 'Docket created successfully. But sending the email or uploading to Dropbox failed.';
    //                }
    //                $('#generate_kvl_docket_status').html('<i class="fa ' + statusClass + '"></i>' + statusMessage).show();

    //            }

    //        });


    //});

    //To Generate All PO List stop

    //To generate Global POI List start
    $('#btn_VisitorProfile_pdf_POIList').on('click', function (e) {



        $('#btn_VisitorProfile_pdf_POIList').prop('disabled', true);
        $('#schRunStatus').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating PDF. Please wait...');
        var ids = [];
        //$.ajax({
        //    url: '/Admin/AuditSiteLog?handler=KeyVehicleLogProfiles',
        //    data: { truckRego: $('#kvlProfileRegos').find(':selected').val(), poi: 'POI' },
        //    type: 'GET',
        //    dataType: 'json',
        //    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        //}).done(function (result) {
        //    result.forEach(function (item) {
        //        ids.push(item.detail.id);
        //    });


        $.ajax({
            url: '/Guard/KeyVehicleLog?handler=GenerateManualDocketList',
            data: {
                IsGlobal: true,
                option: null,
                otherReason: $('#otherReason').val(),
                stakeholderEmails: $('#stakeholderEmail').val(),
                clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                blankNoteOnOrOff: $('#IsBlankNoteOn').val(),
                ids: ids,
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            if (response.statusCode === -1) {
                $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger mr-2"></i> Error generating report').show();
            } else {


                $('#btnScheduleDownload').prop('disabled', false);
                const messageHtml = '';
                $('#schRunStatus').html(messageHtml);


                var newTab = window.open(response.fileName, '_blank');
                if (!newTab) {
                    // If the new tab was blocked, fallback to downloading the file
                    var a = document.createElement('a');
                    a.href = response.fileName;
                    a.download = "POI_List";
                    a.click();
                }
                /* $('#btn_VisitorProfile_pdf_POIList').attr('href', response.fileName);*/
            }
        });
    });
    /*    });*/
    //To generate Global POI List stop
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
        let selectedRow = gridKeyVehicleLogProfile.rows('.selected').data();

        if (personName === '') {
            $('#duplicate_profile_status').text('Person Name is required.');
            return;
        }

        if (selectedRow.length === 0) {           
            selectedRow = gridKeyVehicleLogProfile.rows(0).data();
            //console.log(selectedRow);
            //$('#duplicate_profile_status').text('Please click one row to copy.');
            //return;
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

    $('#titlePOIWarning').show();
    $('#imagesiren').show();


    $('#Report_VehicleRego').typeahead({

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
                    if (data.length == 0) {
                        $('#Report_VehicleRego').val('');
                    }
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
                        gridIncidentReportsVehicleLogProfile.clear().rows.add(result).draw();

                        $('#driver_name').val('Unknown');
                        $('#duplicate_profile_status').text('');
                        $('#incident-report-profiles-modal').find('#kvl-profile-title-rego').html(item);
                        $('#Report_VehicleRego').val(item);
                        $('#incident-report-profiles-modal').modal('show');
                    }
                });
            }
        }
    });
    $('#Report_DateLocation_ClientSite').on('keyup', function (e) {

        var inputValue = $(this).val();
        if (inputValue.length >= 3 && inputValue.match(/[a-zA-Z]/)) {
            e.preventDefault();

            gridSiteSearch.reload({ typeId: $('#sel_client_type').val(), searchTerm: $(this).val() });
            $('#logbook-modal').modal('show');
            //alert('Letter typed and Enter pressed: ' + inputValue);
        }

    });

    let gridSiteSearch;
    gridSiteSearch = $('#client_site_settingsSearch').grid({
        dataSource: '/Admin/Settings?handler=ClientSites',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'typeId', hidden: true },
            { field: 'clientType', title: 'Client Type', width: 180, renderer: function (value, record) { return value.name; } },
            { field: 'name', title: 'Client Site', width: 180, editor: false },
            { width: 100, renderer: settingsButtonRenderer },


        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    function settingsButtonRenderer(value, record) {

        var ClientTypeName = record.clientType.name;
        var ClientSiteName = record.name;

        return '<button class="btn btn-outline-success mt-2 del-schedule d-block" data-sch-id="' + ClientSiteName + '_' + ClientTypeName + '""><i class="fa fa-check mr-2" aria-hidden="true"></i>Select</button>';
    }
    $('#client_site_settingsSearch').on('click', '.del-schedule', function () {


        const ClientSiteName1 = $(this).attr('data-sch-id');
        const lastUnderscoreIndex = ClientSiteName1.lastIndexOf('_');

       



        if (lastUnderscoreIndex !== -1) {
            const recordName = ClientSiteName1.slice(0, lastUnderscoreIndex);
            const ClientTypeName = ClientSiteName1.slice(lastUnderscoreIndex + 1);

            $('#logbook-modal').modal('hide');
            $('#Report_DateLocation_ClientSite').val(recordName);
            $('#Report_DateLocation_ClientType option').each(function () {

                if ($(this).val() === ClientTypeName) {
                    $(this).prop('selected', true);
                    return false;
                }
            });

            // Use recordName and ClientTypeName here
            console.log('record.name:', recordName);
            console.log('ClientTypeName:', ClientTypeName);


            $.ajax({
                url: '/Incident/Register?handler=ClientSiteByName',
                type: 'GET',
                data: { name: recordName }
            }).done(function (data) {
                if (data.success) {
                    $('#Report_DateLocation_ClientAddress').val(data.clientSite.address);
                    $('#Report_DateLocation_State').val(data.clientSite.state);
                    $('#Report_Officer_Billing').val(data.clientSite.billing);
                    if (data.clientSite.gps) toggleClientGpsLink(true, data.clientSite.gps);
                    else toggleClientGpsLink(false);
                    setSelectedClientStatus(data.clientSite);
                    $('#Report_DateLocation_ShowIncidentLocationAddress').prop('checked', false);
                    $('#clientSiteAddress').val(data.clientSite.address);
                    $('#clientSiteGps').val(data.clientSite.gps);
                    const clientAreaControl = $('#Report_DateLocation_ClientArea');
                    clientAreaControl.html('');
                   // toggleClientGpsLink(false);

                    //const ulClients = $('#Report_DateLocation_ClientArea').siblings('ul.es-list');
                    //ulClients.html('');

                   const option = recordName;
                    if (option == '')
                        return false;

                    $.ajax({
                        url: '/Incident/Register?handler=ClientAreas&Id=' + encodeURIComponent(option),
                        type: 'GET',
                        dataType: 'json',
                        success: function (data) {

                            data.map(function (site) {
                                // ulClients.append('<li class="es-visible" value="' + site.text + '">' + site.text + '</li>');
                                clientAreaControl.append('<option value="' + site.text + '">' + site.text + '</option>');

                            });

                        }
                    });


            ////p1 - 202 site allocation - end
                }
            }).fail(function () {
            });

        } else {
            console.log('Invalid data-sch-id format');
        }
    });

    /*to get POI Number-start*/
    function GetPOINumber() {

        $.ajax({
            url: '/Guard/KeyVehiclelog?handler=POINumber',
            type: 'GET',


            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {


            if (result.success) {

                $('#VehicleRego').val(result);
                $('#kvl_list_plates').attr('disabled', false);
            } else {
                $('#VehicleRego').val(result);
            }

        });
    }
    /*to get POI Number-end*/


    /*to Off Duty start*/
    $('#keyvehicle_offduty').on('click', function (e) {
        e.preventDefault();
        if (confirm('Are you sure you want to end your shift?')) {
            $('#loader').show();
            // Task p6#73_TimeZone issue -- added by Binoy - Start
            var tmdata = {
                'EventDateTimeLocal': null,
                'EventDateTimeLocalWithOffset': null,
                'EventDateTimeZone': null,
                'EventDateTimeZoneShort': null,
                'EventDateTimeUtcOffsetMinute': null,
            };

            fillRefreshLocalTimeZoneDetails(tmdata, "", false)
            // Task p6#73_TimeZone issue -- added by Binoy - End
            $.ajax({
                url: '/Guard/DailyLog?handler=UpdateOffDuty',
                data: { guardLoginId: $('#KeyVehicleLog_GuardLogin_Id').val(), clientSiteLogBookId: $('#KeyVehicleLog_ClientSiteLogBookId').val(), tmdata: tmdata },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    window.location.replace('/Guard/Login?t=vl');
                } else {
                    alert(result.message);
                }
            }).fail(function () {
                alert('error');
            }).always(function () {
                $('#loader').hide();
            });
        }
    });
    /*to Off Duty stop*/


    function showDuressCountdownPopup() {
        if ($("#duress_status").text() === "Active") return;

        countdownValue = 5;
        $('#duress_countdown').text(countdownValue);
        $('#duress_modal_overlay').show();
        $('#duress_confirm_popup').show();
        
        countdownId = setInterval(function () {
            countdownValue--;
            $('#duress_countdown').text(countdownValue);

            if (countdownValue <= 0) {
                clearInterval(countdownId);
                
                $('#duress_modal_overlay').hide();
                $('#duress_confirm_popup').hide();
                GFG_Fun(); // trigger duress activation
            }
        }, 1000);
    }

    function cancelDuressCountdown() {
        clearInterval(countdownId);
        $('#duress_modal_overlay').hide();
        $('#duress_confirm_popup').hide();
        
    }

    $("#kv_duress_btn").on("click", function () {
        showDuressCountdownPopup();
    });

    $("#cancel_duress").on("click", function () {
        cancelDuressCountdown();
    });

    //var tId = 0;
    //$("#kv_duress_btn").mousedown(function () {
    //    tId = setTimeout(GFG_Fun, 2500);
    //    return false;
    //});
    //$("#kv_duress_btn").mouseup(function () {
    //    clearTimeout(tId);
    //});


    ///*for touch devices Start */
    //var touchTimer = 0;
    //$('#kv_duress_btn').on('touchstart', function (e) {
    //    // Prevent the default behavior
    //    e.preventDefault();

    //    if ($("#duress_status").text() !== "Active") {
    //        console.log('click');
    //        /*timer pause while editing*/
    //        isPaused = true;
    //        touchTimer = setTimeout(GFG_Fun, 2500);
    //        console.log(isPaused);
    //        console.log(touchTimer);
    //        gridGuardLog.clear();
    //        gridGuardLog.reload();
    //    }
    //    return false;
    //});

    ////$('#duress_btn').on('touchend', function () {
    //console.log('stoped');
    //// If there is any movement or the touch ends, clear the timer
    ////clearTimeout(touchTimer);
    ////isPaused = false;
    ////});

    //$('#kv_duress_btn').on('pointerup', function (event) {
    //    // Your logic
    //    console.log('stoped2');
    //    clearTimeout(touchTimer);
    //    isPaused = false;
    //});

    /*for touch devices end */



    /* Get Client Site duress Gps Rading Start*/
    function initialize() {
        var geocoder = new google.maps.Geocoder();
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function (position) {

                gpsCoordinatesValues = position.coords.latitude + ',' + position.coords.longitude;
                var latlng = new google.maps.LatLng(position.coords.latitude, position.coords.longitude);
                $("#hid_duressEnabledGpsCoordinates").val(gpsCoordinatesValues);
                reverseGeocode(position.coords.latitude, position.coords.longitude);

            });
        }

    }



    function getDurressLocation() {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function (position) {
                $("#duressEnabledGpsCoordinates").val(position.coords.latitude);
                reverseGeocode(position.coords.latitude, position.coords.longitude);

            });
        }
    }


    function reverseGeocode(latitude, longitude) {
        var latlng = new google.maps.LatLng(latitude, longitude);
        var geocoder = new google.maps.Geocoder();
        geocoder.geocode({ 'latLng': latlng }, function (results, status) {
            if (status === google.maps.GeocoderStatus.OK) {
                if (results[0]) {
                    var address = results[0].formatted_address;
                    $("#hid_duressEnabledAddress").val(address);
                    console.log('Reverse Geocoding Result: ', address);

                } else {
                    console.error('No results found');
                }
            } else {
                console.error('Geocoder failed due to: ' + status);
            }
        });
    }

    function getLocation() {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(showPosition, showError);
        } else {
            alert("Geolocation is not supported by this browser.");
        }
    }

    /* Get Client Site duress Gps Rading end*/


    function GFG_Fun() {
        if ($("#kv_duress_status").text() !== "Active") {
            // Task p6#73_TimeZone issue -- added by Binoy - Start
            var tmdata = {
                'EventDateTimeLocal': null,
                'EventDateTimeLocalWithOffset': null,
                'EventDateTimeZone': null,
                'EventDateTimeZoneShort': null,
                'EventDateTimeUtcOffsetMinute': null,
            };
            fillRefreshLocalTimeZoneDetails(tmdata, "", false)
            // Task p6#73_TimeZone issue -- added by Binoy - End

            $.ajax({
                url: '/Guard/KeyVehicleLog?handler=SaveClientSiteDuress',
                data: {
                    clientSiteId: $('#KeyVehicleLog_ClientSiteLogBook_ClientSiteId').val(),
                    GuardId: $('#KeyVehicleLog_GuardLogin_GuardId').val(),
                    guardLoginId: $('#KeyVehicleLog_GuardLogin_Id').val(),
                    logBookId: $('#KeyVehicleLog_ClientSiteLogBookId').val(),
                    gpsCoordinates: $("#hid_duressEnabledGpsCoordinates").val(),
                    enabledAddress: $("#hid_duressEnabledAddress").val(),
                    tmdata: tmdata
                },
                dataType: 'json',
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $('#kv_duress_btn').removeClass('normal').addClass('active');
                    $("#kv_duress_status").addClass('font-weight-bold');
                    $("#kv_duress_status").text("Active");
                }
            });
        }
    }

    // Task p6#73_TimeZone issue -- added by Binoy - Start
    function fillRefreshLocalTimeZoneDetails(formData, modelname, isform) {
        // for reference https://moment.github.io/luxon/#/
        var DateTime = luxon.DateTime;
        var dt1 = DateTime.local();
        let tz = dt1.zoneName + ' ' + dt1.offsetNameShort;
        let diffTZ = dt1.offset
        //let tzshrtnm = dt1.offsetNameShort;
        let tzshrtnm = 'GMT' + dt1.toFormat('ZZ'); // Modified by binoy on 19-01-2024

        const eventDateTimeLocal = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS');
        const eventDateTimeLocalWithOffset = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS Z');
        if (isform) {
            if (modelname != '') {
                formData.append(modelname + ".EntryCreatedDateTimeLocal", eventDateTimeLocal);
                formData.append(modelname + ".EntryCreatedDateTimeLocalWithOffset", eventDateTimeLocalWithOffset);
                formData.append(modelname + ".EntryCreatedDateTimeZone", tz);
                formData.append(modelname + ".EntryCreatedDateTimeZoneShort", tzshrtnm);
                formData.append(modelname + ".EntryCreatedDateTimeUtcOffsetMinute", diffTZ);
            } else {
                formData.append("EntryCreatedDateTimeLocal", eventDateTimeLocal);
                formData.append("EntryCreatedDateTimeLocalWithOffset", eventDateTimeLocalWithOffset);
                formData.append("EntryCreatedDateTimeZone", tz);
                formData.append("EntryCreatedDateTimeZoneShort", tzshrtnm);
                formData.append("EntryCreatedDateTimeUtcOffsetMinute", diffTZ);
            }
        }
        else {
            formData.EventDateTimeLocal = eventDateTimeLocal;
            formData.EventDateTimeLocalWithOffset = eventDateTimeLocalWithOffset;
            formData.EventDateTimeZone = tz;
            formData.EventDateTimeZoneShort = tzshrtnm;
            formData.EventDateTimeUtcOffsetMinute = diffTZ;
        }
    }

    // Task p6#73_TimeZone issue -- added by Binoy - End

    /*p1-196 rationalization of menu changes-start*/
    /*to add do's and donts -start*/

    //let gridDosAndDontsFields;
    //let isDosandDontsFieldAdding = false;
    //gridDosAndDontsFields = $('#tbl_dosanddonts_fields').grid({
    //    dataSource: '/Admin/GuardSettings?handler=DosandDontsFields',
    //    uiLibrary: 'bootstrap4',
    //    iconsLibrary: 'fontawesome',
    //    primaryKey: 'id',
    //    inlineEditing: { mode: 'command' },
    //    columns: [
    //        { field: 'name', title: 'Name', width: 200, editor: true },
    //    ],
    //    initialized: function (e) {
    //        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    //    }
    //});
    //$('#doanddontfields_types').on('change', function () {
    //    const selKvlFieldTypeId = $('#doanddontfields_types').val();
    //    gridDosAndDontsFields.clear();
    //    gridDosAndDontsFields.reload({ typeId: selKvlFieldTypeId });
    //});
    //$('#add_dosanddonts_fields').on('click', function () {
    //    const selFieldTypeId = $('#doanddontfields_types').val();
    //    if (!selFieldTypeId) {
    //        alert('Please select a field type to update');
    //        return;
    //    }
    //    var rowCount = $('#tbl_dosanddonts_fields tr').length;


    //    if (isDosandDontsFieldAdding) {
    //        alert('Unsaved changes in the grid. Refresh the page');
    //    } else {
    //        isKvlFieldAdding = true;
    //        gridDosAndDontsFields.addRow({
    //            'id': -1,
    //            'typeId': selFieldTypeId,
    //            'name': '',
    //        }).edit(-1);
    //    }
    //});
    //if (gridDosAndDontsFields) {
    //    gridDosAndDontsFields.on('rowDataChanged', function (e, id, record) {
    //        const data = $.extend(true, {}, record);
    //        $.ajax({
    //            url: '/Admin/GuardSettings?handler=SaveDosandDontsField',
    //            data: { record: data },
    //            type: 'POST',
    //            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //        }).done(function (result) {
    //            if (result.success) gridDosAndDontsFields.reload({ typeId: $('#doanddontfields_types').val() });
    //            else alert(result.message);
    //        }).fail(function () {
    //            console.log('error');
    //        }).always(function () {
    //            if (isDosandDontsFieldAdding)
    //                isDosandDontsFieldAdding = false;
    //        });
    //    });

    //    gridDosAndDontsFields.on('rowRemoving', function (e, id, record) {
    //        if (confirm('Are you sure want to delete this field?')) {
    //            $.ajax({
    //                url: '/Admin/GuardSettings?handler=DeleteDosandDontsField',
    //                data: { id: record },
    //                type: 'POST',
    //                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //            }).done(function (result) {
    //                if (result.success) gridDosAndDontsFields.reload({ typeId: $('#doanddontfields_types').val() });
    //                else alert(result.message);
    //            }).fail(function () {
    //                console.log('error');
    //            }).always(function () {
    //                if (isKvlFieldAdding)
    //                    isKvlFieldAdding = false;
    //            });
    //        }
    //    });
    //}
    /*to add do's and donts -end*/
    /*p1-196 rationalization of menu changes-end*/


   



});
/*to add poi binder - start*/
$('#chb_IsPDFBinder').on('change', function () {

    const isChecked = $(this).is(':checked');
    $('#lbl_PDFBinder').text(isChecked ? 'PDF Binder  On' : 'PDF Binder Off (ZIP)'); lbl_PDFBinder
    $('#IsPDFBinderOn').val(isChecked);
    $('#download_kvl_docket').hide();
});
$('#chkAllBatchDocketSelect').on('change', function () {

    const isChecked = $(this).is(':checked');
    if (isChecked == true) {

        $("#vehicle_key_daily_log  input[type=checkbox]").each(function () {
            var isChecked1 = $(this).is(':checked');
            if (isChecked1 == false) {
                $(this).prop('checked', true);
            }

        });
    }
    else {

        //to uncheck the ticked options-start
        $("#vehicle_key_daily_log  input[type=checkbox]:checked").each(function () {
            var isChecked1 = $(this).is(':checked');
            if (isChecked1 == true) {
                $(this).prop('checked', false);
            }

        });

        //to uncheck the ticked options-end
    }

});

/*to add poi binder - end*/

/*for manifest options-start*/
function GetToggles(siteId, toggleId) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/KeyVehicleLog?handler=ClientSiteToggle',
        type: 'GET',
        data: {
            siteId: siteId,
            toggleId: toggleId
        },
        headers: { 'RequestVerificationToken': token }
    }).done(function (response) {
        if (response.length != 0) {
            if (response[0].toggleTypeId == 1) {
                if (response[0].isActive == true) {
                    $('#IsTimeSlotNo').val(true);
                    $('#cbIsTimeSlotNo').prop('checked', true);
                    $('#lblIsTimeSlotNo').text(response[0].isActive ? 'Time Slot No.' : 'T.No. (Load)');
                }
                else {
                    $('#IsTimeSlotNo').val(false);
                    $('#cbIsTimeSlotNo').prop('checked', false);
                    $('#lblIsTimeSlotNo').text(response[0].isActive ? 'Time Slot No.' : 'T.No. (Load)');
                }
            }
            if (response[0].toggleTypeId == 2) {
                if (response[0].isActive == true) {
                    $('#IsVWI').val(true);
                    $('#cbIsVWI').prop('checked', true);
                    $('#lblIsVXI').text(response[0].isActive ? 'VWI' : 'Manifest');
                }
                else {
                    $('#IsVWI').val(false);
                    $('#cbIsVWI').prop('checked', false);
                    $('#lblIsVXI').text(response[0].isActive ? 'VWI' : 'Manifest');
                }
            }
            if (response[0].toggleTypeId == 3) {
                if (response[0].isActive == true) {
                    $('#IsSender').val(true);
                    $('#cbIsSender').prop('checked', true);
                    $('#lblIsSender').text(response[0].isActive ? 'Sender Address' : 'Reciever Address');
                }
                else {
                    $('#IsSender').val(false);
                    $('#cbIsSender').prop('checked', false);
                    $('#lblIsSender').text(response[0].isActive ? 'Sender Address' : 'Reciever Address');
                }
            }
            if (response[0].toggleTypeId == 4) {
                if (response[0].isActive == true) {
                    $('#IsReels').val(true);
                    $('#cbIsReels').prop('checked', true);
                    $('#lblIsReels').text(response[0].isActive ? 'Reels' : 'QTY');
                }
                else {
                    $('#IsReels').val(false);
                    $('#cbIsReels').prop('checked', false);
                    $('#lblIsReels').text(response[0].isActive ? 'Reels' : 'QTY');
                }
            }

            if (response[0].toggleTypeId == 5) {
                if (response[0].isActive == true) {
                    $('#IsISOVIN').val(true);
                    $('#cbIsISOVIN').prop('checked', true);
                    $('#lblISO_One').text(response[0].isActive ? 'ISO/VIN + Seal' : 'Trailer 1 Rego.');
                    $('#lblISO_Two').text(response[0].isActive ? 'ISO/VIN + Seal' : 'Trailer 2 Rego.');
                    $('#lblISO_Three').text(response[0].isActive ? 'ISO/VIN + Seal' : 'Trailer 3 Rego.');
                    $('#lblISO_Four').text(response[0].isActive ? 'ISO/VIN + Seal' : 'Trailer 4 Rego.');
                }
                else {
                    $('#IsISOVIN').val(false);
                    $('#cbIsISOVIN').prop('checked', false);
                    $('#lblISO_One').text(response[0].isActive ? 'ISO/VIN + Seal' : 'Trailer 1 Rego.');
                    $('#lblISO_Two').text(response[0].isActive ? 'ISO/VIN + Seal' : 'Trailer 2 Rego.');
                    $('#lblISO_Three').text(response[0].isActive ? 'ISO/VIN + Seal' : 'Trailer 3 Rego.');
                    $('#lblISO_Four').text(response[0].isActive ? 'ISO/VIN + Seal' : 'Trailer 4 Rego.');
                }
            }
            return response[0].isActive;
        }

    }).fail(function () {
        console.log("error");
    });
}

/*for manifest options-start*/



