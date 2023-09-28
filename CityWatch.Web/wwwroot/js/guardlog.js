$(function () {
    function isLogbookExpired(logBookDate) {
        if (((new Date()).toLocaleDateString('en-AU') > new Date(logBookDate).toLocaleDateString('en-AU'))) {
            return true;
        }
        return false;
    }

    function isValidDate(date) {
        return date && Object.prototype.toString.call(date) === "[object Date]" && !isNaN(date);
    }

    function parseDateInCsharpFormat(dateValue, timeValue) {
        if (!isValidDate(dateValue)) {
            alert('Login Failed. Unable to parse OnDuty or OffDuty Time');
            return null;
        }

        return getFormattedDate(dateValue, timeValue, '-');
    }

    function getFormattedDate(dateValue, timeValue, seperator) {
        if (!dateValue) return null;

        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        let day = dateValue.getDate();

        if (day < 10) {
            day = '0' + day;
        }

        // format: 15-Mar-2022 or 15 Mar 2022
        let result = day + seperator + months[dateValue.getMonth()] + seperator + dateValue.getFullYear();

        if (timeValue)
            result = result + ' ' + timeValue;

        return result;
    }

    function getDateFromDateTime(dateString) {
        return new Date(dateString.split('T')[0]);
    }

    //*************** Guard Login  *************** //

    function populateClientSites(selectedSiteName) {
        $('#GuardLogin_SmartWandOrPosition').html('<option value="">Select</option>');

        const option = $('#GuardLogin_ClientType').val();
        if (option == '')
            return false;

        const clientSiteControl = $('#GuardLogin_ClientSiteName');
        clientSiteControl.html('');
        $.ajax({
            url: '/Incident/Register?handler=ClientSites&type=' + encodeURIComponent(option),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                clientSiteControl.append('<option value="">Select</option>')
                data.map(function (site) {
                    clientSiteControl.append('<option value="' + site.value + '">' + site.text + '</option>');
                });

                if (selectedSiteName) {
                    $('#GuardLogin_ClientSiteName').val(selectedSiteName);
                } else {
                    $('#GuardLogin_ClientSiteName').val('');
                }
            }
        });
    }

    function getSmartWandOrOfficerPosition(isPosition, clientSiteName, smartWandOrPositionId) {
        const url = isPosition ?
            '/Guard/Login?handler=OfficerPositions' :
            '/Guard/Login?handler=SmartWands&siteName=' + encodeURIComponent(clientSiteName ? clientSiteName : $('#GuardLogin_ClientSiteName').val()) +
            '&guardId=' + $('#GuardLogin_Guard_Id').val();

        const smart_Wand_Or_Position = $('#GuardLogin_SmartWandOrPosition');
        smart_Wand_Or_Position.html('');
        $.ajax({
            url: url,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                smart_Wand_Or_Position.append('<option value="">Select</option>').attr("selected", "selected");
                data.map(function (result) {
                    if (isPosition) {
                        smart_Wand_Or_Position.append('<option value="' + result.value + '">' + result.text + '</option>');
                    }
                    else {
                        smart_Wand_Or_Position.append('<option value="' + result.smartWandId + '">' + result.smartWandId + (result.isInUse ? ' &#xf06a;' : '') + '</option>');
                    }
                });

                if (smartWandOrPositionId) {
                    smart_Wand_Or_Position.val(smartWandOrPositionId);
                } else {
                    smart_Wand_Or_Position.val('');
                }
            }
        });
    }

    function showGuardSearchResult(message, isError) {
        $('#guardSearchResult').removeClass('text-muted').removeClass('text-danger');
        if (isError) $('#guardSearchResult').addClass('text-danger');
        else $('#guardSearchResult').addClass('text-muted');
        $('#guardSearchResult').html(message);
    }

    function resetGuardLoginDetails() {
        $('#divNewGuard').hide();
        $('#guardLoginDetails').hide();

        $('#GuardLogin_Id').val('');
        $('#GuardLogin_Guard_Id').val('-1');
        $('#GuardLogin_Guard_Name').val('');
        $('#GuardLogin_Guard_Initial').val('');
        $('#GuardLogin_Guard_State').val('');
        $('#GuardLogin_ClientType').val('');
        $('#GuardLogin_ClientSiteName').html('<option value="">Select</option>');
        $('#GuardLogin_IsPosition').prop('checked', false);
        $('#GuardLogin_SmartWandOrPosition').html('<option value="">Select</option>');
        $('#GuardLogin_SmartWandOrPosition').prop('disabled', true);
        $('#GuardLogin_OnDuty_Time').val('');
        $('#GuardLogin_OffDuty_Time').val('');

        $('#offDutyIsToday').prop('checked', true);
        $('#onDutyIsToday').prop('checked', true);
        highlightDutyDay('lblOffDutyToday');
        highlightDutyDay('lblOffDutyTomorrow', false);
        $('#guardShiftDayTime').html('N/A');
    }

    function clearGuardValidationSummary(validationControl) {
        $('#' + validationControl).removeClass('validation-summary-errors').addClass('validation-summary-valid');
        $('#' + validationControl).html('');
    }

    function displayGuardValidationSummary(validationControl, errors) {
        $('#' + validationControl).removeClass('validation-summary-valid').addClass('validation-summary-errors');
        $('#' + validationControl).html('');
        $('#' + validationControl).append('<ul></ul>');
        if (!Array.isArray(errors)) {
            $('#' + validationControl + ' ul').append('<li>' + errors + '</li>');
        } else {
            errors.forEach(function (item) {
                if (item.indexOf(',') > 0) {
                    item.split(',').forEach(function (itemInner) {
                        $('#' + validationControl + ' ul').append('<li>' + itemInner + '</li>');
                    });
                } else {
                    $('#' + validationControl + ' ul').append('<li>' + item + '</li>');
                }
            });
        }
    }

    $('#GuardLogin_Guard_SecurityNo').on('blur', function () {
        $('#glValidationSummary').html('');
        const isNewGuard = $('#GuardLogin_IsNewGuard').is(':checked');
        if (isNewGuard)
            return;

        showGuardSearchResult('Enter Security License No and click Search');
        resetGuardLoginDetails();

        if ($(this).val() === '')
            return;

        $('#loader').show();

        $.ajax({
            url: '/Guard/Login?handler=GuardDetails&securityNumber=' + $(this).val(),
            type: 'GET',
            dataType: 'json',
            success: function (result) {
                if (result.guard == null) {
                    showGuardSearchResult('A guard with given security license number not found. If you are a new guard, tick "New Guard?" to register and login.', true);
                }
                else if (!result.guard.isActive) {
                    showGuardSearchResult('A guard with given security license number is disabled. Please contact admin to activate', true);
                }
                else {
                    $('#GuardLogin_IsNewGuard').prop('checked', false);

                    const hasLastLogin = result.lastLogin !== null;
                    showGuardSearchResult('Hello ' + result.guard.name + '. Please ' + (hasLastLogin ? 'verify' : 'fill') + ' your details and click Enter Log Book');

                    $('#GuardLogin_ClientType').prop('disabled', false);
                    $('#GuardLogin_ClientSiteName').prop('disabled', false);
                    $('#GuardLogin_OnDuty_Time').prop('disabled', false);
                    $('#GuardLogin_OnDuty_Time').val(getTimeFromDateTime(new Date()));
                    $('#GuardLogin_OffDuty_Time').prop('disabled', false);

                    $('#GuardLogin_Guard_Name').val(result.guard.name);
                    if (result.guard.email != null) {
                        $("#divGuardEmail").hide();
                    }
                    if (result.guard.mobile != null) {
                        $("#divGuardMobile").hide();
                    }
                    
                    $('#GuardLogin_Guard_Email').val(result.guard.email);
                    $('#GuardLogin_Guard_Mobile').val(result.guard.mobile);
                    $('#GuardLogin_Guard_Initial').val(result.guard.initial);
                    $('#guardLoginDetails').show();
                    $('#GuardLogin_Guard_Id').val(result.guard.id);

                    if (hasLastLogin) {
                        const lastLogin = result.lastLogin;

                        $('#GuardLogin_Id').val(lastLogin.id);
                        $('#GuardLogin_ClientType').val(lastLogin.clientSite.clientType.name);
                        populateClientSites(lastLogin.clientSite.name);
                        const isPosition = lastLogin.smartWandId === null ? true : false;
                        const smartWandOrPositionName = isPosition ? lastLogin.position.name : lastLogin.smartWand.smartWandId;
                        getSmartWandOrOfficerPosition(isPosition, lastLogin.clientSite.name, smartWandOrPositionName);
                        $('#GuardLogin_IsPosition').prop('checked', isPosition);
                        $('#GuardLogin_OnDuty_Time').val(getTimeFromDateTime(new Date(lastLogin.onDuty)));

                        let isOffDutyDateToday = true;
                        if (lastLogin.offDuty) {
                            $('#GuardLogin_OffDuty_Time').val(getTimeFromDateTime(new Date(lastLogin.offDuty)));
                            isOffDutyDateToday = getDateFromDateTime(lastLogin.onDuty) > getDateFromDateTime(lastLogin.offDuty);
                        }

                        $('#GuardLogin_SmartWandOrPosition').prop('disabled', false);
                        onGuardLoginDutyTimeChange(isOffDutyDateToday);
                    }
                }
            },
            complete: function () {
                $('#loader').hide();
            }
        });
    });

    $('#GuardLogin_ClientType').on('change', function () {
        populateClientSites();
    });

    $('#GuardLogin_ClientSiteName').on('change', function () {
        const isPosition = $('#GuardLogin_IsPosition').is(':checked');
        getSmartWandOrOfficerPosition(isPosition);
        $('#GuardLogin_SmartWandOrPosition').prop('disabled', false);
    });

    $('#GuardLogin_IsPosition').on('change', function () {
        const isPosition = $('#GuardLogin_IsPosition').is(':checked');
        if (isPosition)
            new MessageModal({ message: "Only click <b>Position</b> if you do not have a Smart WAND - are you sure you want to continue?" }).showWarning();
        getSmartWandOrOfficerPosition(isPosition);
        $('#GuardLogin_SmartWandOrPosition').prop('disabled', false);
    });

    $('#GuardLogin_IsNewGuard').on('change', function () {
        resetGuardLoginDetails();
        const isChecked = $('#GuardLogin_IsNewGuard').is(':checked');
        if (isChecked) {
            $('#GuardLogin_Guard_SecurityNo').val('');
            showGuardSearchResult('Enter Security License No of New Guard');
            $('#divNewGuard').show();
            $('#GuardLogin_ClientSiteName').prop('disabled', false);
            $('#GuardLogin_ClientType').prop('disabled', false);
            $('#GuardLogin_OnDuty_Time').prop('disabled', false);
            $('#GuardLogin_OnDuty_Time').val(getTimeFromDateTime(new Date()));
            $('#GuardLogin_OffDuty_Time').prop('disabled', false);
            $('#GuardLogin_Guard_Email').val('');
            $("#divGuardEmail").show();
            $('#GuardLogin_Guard_Mobile').val('');
            $('#guardLoginDetails').show();
            $("#divGuardMobile").show();
        }
        else {
            $('#divNewGuard').hide();
            $('#guardLoginDetails').hide();
            showGuardSearchResult('Enter Security License No and click Search');
        }
    });

    function getTargetUrl(logBookType) {
        if (logBookType === 1) return '/Guard/DailyLog';
        if (logBookType === 2) return '/Guard/KeyVehicleLog';
        return '';
    }

    $('#btn_confrim_wand_use').on('click', function () {
        submitGuardLogin();
    });

    $('#btnGuardLogin').on('click', function () {
        const validateSmartWand = $('#GuardLogin_IsPosition').is(':not(:checked)') && $('#GuardLogin_SmartWandOrPosition').val() !== '';

        if (!validateSmartWand) {
            submitGuardLogin();
        } else {
            $('#loader').show();
            $.ajax({
                url: '/Guard/Login?handler=CheckIsWandAvailable',
                type: 'GET',
                data: {
                    clientSiteName: $('#GuardLogin_ClientSiteName').val(),
                    smartWandNo: $('#GuardLogin_SmartWandOrPosition').val(),
                    guardId: $('#GuardLogin_Guard_Id').val()
                }
            }).done(function (result) {
                if (result) $('#alert-wand-in-use-modal').modal('show');
                else submitGuardLogin();
            }).always(function () {
                $('#loader').hide();
            });
        }
    });

    function submitGuardLogin() {
        calculateDutyDateTime();

        $('#loader').show();
        $.ajax({
            url: '/Guard/Login?handler=LoginGuard',
            type: 'POST',
            data: $('#frmGuardLogin').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (result) {
            if (result.success) {
                if (result.initalsChangedMessage !== '')
                    alert(result.initalsChangedMessage);

                let toUrl = getTargetUrl(result.logBookType);
                if (toUrl === '') alert('Invalid logbook type');
                else {
                    window.location.replace(toUrl);
                    $('#btnGuardLogin').prop('disabled', true);
                }
            } else {
                if (result.errors)
                    displayGuardValidationSummary('glValidationSummary', result.errors)
                else
                    alert(result.message)
            }
        }).always(function () {
            $('#loader').hide();
        });
    }

    $('#offDutyIsToday').on('change', function () {
        const offDutyIsToday = $('#offDutyIsToday').is(':checked');
        highlightDutyDay('lblOffDutyToday', offDutyIsToday);
        highlightDutyDay('lblOffDutyTomorrow', !offDutyIsToday);
        var offDutyTime = $('#GuardLogin_OffDuty_Time').val();
        var onDutyTime = $('#GuardLogin_OnDuty_Time').val();

        const onDutyIsToday = !offDutyIsToday || (onDutyTime <= offDutyTime);
        $('#onDutyIsToday').prop('checked', onDutyIsToday);

        showGuardShiftTimeMessage();
    });

    $('#GuardLogin_OnDuty_Time').on('blur', function () {
        onGuardLoginDutyTimeChange();
    });

    $('#GuardLogin_OffDuty_Time').on('blur', function () {
        onGuardLoginDutyTimeChange();
    });

    function onGuardLoginDutyTimeChange(isOffDutyDateToday) {
        const offDutyTime = $('#GuardLogin_OffDuty_Time').val();
        const onDutyTime = $('#GuardLogin_OnDuty_Time').val();

        const offDutyIsToday = isOffDutyDateToday || (offDutyTime >= onDutyTime);
        $('#offDutyIsToday').prop('checked', offDutyIsToday);
        highlightDutyDay('lblOffDutyToday', offDutyIsToday);
        highlightDutyDay('lblOffDutyTomorrow', !offDutyIsToday);

        $('#onDutyIsToday').prop('checked', isOffDutyDateToday ? false : true);

        showGuardShiftTimeMessage();
    }

    function showGuardShiftTimeMessage() {
        const onDutyDayName = $('#onDutyIsToday').is(':checked') ? 'Today' : 'Yesterday';
        const offDutyDayName = $('#offDutyIsToday').is(':checked') ? 'Today' : 'Tomorrow';
        const onDutyTime = $('#GuardLogin_OnDuty_Time').val();
        const offDutyTime = $('#GuardLogin_OffDuty_Time').val();

        $('#guardShiftDayTime').html(onDutyDayName + ' ' + onDutyTime + ' - ' + offDutyDayName + ' ' + offDutyTime);
    }

    function calculateDutyDateTime() {
        let onDutyDate = new Date();
        if ($('#onDutyIsToday').is(':not(:checked)'))
            onDutyDate = new Date(onDutyDate.setDate(onDutyDate.getDate() - 1));
        $('#GuardLogin_OnDuty').val(parseDateInCsharpFormat(onDutyDate, $('#GuardLogin_OnDuty_Time').val()));

        let offDutyDate = new Date();
        if ($('#offDutyIsToday').is(':not(:checked)'))
            offDutyDate = new Date(offDutyDate.setDate(offDutyDate.getDate() + 1));
        $('#GuardLogin_OffDuty').val(parseDateInCsharpFormat(offDutyDate, $('#GuardLogin_OffDuty_Time').val()));
    }

    function highlightDutyDay(ctrlId, selected = true) {
        if (selected) $('#' + ctrlId).removeClass('font-weight-light').addClass('font-weight-bold');
        else $('#' + ctrlId).removeClass('font-weight-bold').addClass('font-weight-light');
    }

    //*************** Daily Guard Log  *************** //

    function getTimeFromDateTime(value) {
        const mins = (value.getMinutes() < 10 ? '0' : '') + value.getMinutes();
        const hours = (value.getHours() < 10 ? '0' : '') + value.getHours();
        return hours + ':' + mins;
    }

    setInterval(() => {
        $('#new_log_time').val(getTimeFromDateTime(new Date()));
        // monitor logbook expiry at midnight
        if ($('#GuardLog_ClientSiteLogBook_Date').length === 1 &&
            isLogbookExpired($('#GuardLog_ClientSiteLogBook_Date').val())) {
            $('#logbook-expiry-modal').modal('show');
        }

        if ($('#KeyVehicleLog_ClientSiteLogBook_Date').length === 1 &&
            isLogbookExpired($('#KeyVehicleLog_ClientSiteLogBook_Date').val())) {
            $('#logbook-kvl-expiry-modal').modal('show');
        }
    }, 1000);

    $('#btn_confirm_logbook_expiry').on('click', function () {
        $('#loader').show();
        $.ajax({
            url: '/Guard/DailyLog?handler=ResetClientSiteLogBook',
            type: 'POST',
            data: {
                clientSiteId: $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val(),
                guardLoginId: $('#GuardLog_GuardLoginId').val()
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (!result.success)
                alert(result.message)
            else {
                $('#logbook-expiry-modal').modal('hide');
                window.location.reload();
            }
        }).always(function () {
            $('#loader').hide();
        });
    });

    $('#collapseOne, #collapseTwo').on('show.bs.collapse', function (e) {
        $(e.target).prev().find(".fa-chevron-down").removeClass("fa-chevron-down").addClass("fa-chevron-up");
    }).on('hide.bs.collapse', function (e) {
        $(e.target).prev().find(".fa-chevron-up").removeClass("fa-chevron-up").addClass("fa-chevron-down");
    });

    function renderDateTime(value, record) {
        if (value !== '') {
            const date = new Date(value);
            const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
            let day = date.getDate();

            if (day < 10) {
                day = '0' + day;
            }

            return day + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
        }
    }

    const renderGuardInitialColumn = function (value, record, $cell, $displayEl) {
        if (record.guardId !== null) {
            return value + '<a href="#" class="ml-2"><i class="fa fa-vcard-o text-info" data-toggle="modal" data-target="#guardInfoModal" data-id="' + record.guardId + '"></i></a>';
        }
        else return value;
    }

    $('#guardInfoModal').on('shown.bs.modal', function (event) {
        $('#lbl_guard_name').html('');
        $('#lbl_guard_security_no').html('');
        $('#lbl_guard_state').html('');
        $('#lbl_guard_provider').html('');

        const button = $(event.relatedTarget);
        const id = button.data('id');

        $.ajax({
            url: '/Admin/AuditSiteLog?handler=GuardData',
            data: { id: id },
            type: 'GET',
        }).done(function (result) {
            if (result) {
                $('#lbl_guard_name').html(result.name);
                $('#lbl_guard_security_no').html(result.securityNo);
                $('#lbl_guard_state').html(result.state);
                $('#lbl_guard_email').html(result.email);
                $('#lbl_guard_mobile').html(result.mobile);
                $('#lbl_guard_provider').html(result.provider);
            }
        });
    });

    function renderTime(value, record) {
        if (value !== '') {
            const date = new Date(value);
            return date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
        }
    }

    let gridGuardLog;

    let gridGuardLogSettings = {
        dataSource: '/Guard/DailyLog?handler=GuardLogs',
        params: { logBookId: $('#GuardLog_ClientSiteLogBookId').val() },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'clientSiteId', hidden: true },
            { field: 'eventDateTime', title: 'Time', width: 100, renderer: function (value, record) { return renderTime(value, record, false); } },
            { field: 'notes', title: 'Event / Notes', width: 450 },
            { field: 'guardInitials', title: 'Guard Initials', width: 100, renderer: function (value, record) { return record.guardLogin.guard.initial; } }
        ]
    };
    $('#card_new_entry').hide();

    if ($('input[name="isEditable"]').val() !== 'false') {
        $('#card_new_entry').show();

        gridGuardLogSettings = {
            dataSource: '/Guard/DailyLog?handler=GuardLogs',
            params: { logBookId: $('#GuardLog_ClientSiteLogBookId').val() },
            uiLibrary: 'bootstrap4',
            iconsLibrary: 'fontawesome',
            primaryKey: 'id',
            inlineEditing: { mode: 'command', managementColumn: false },
            columns: [
                { field: 'clientSiteId', hidden: true },
                { field: 'eventDateTime', title: 'Time', width: 50, renderer: function (value, record) { return renderTime(value, record, false); } },
                { field: 'notes', title: 'Event / Notes', width: 450, editor: logBookNotesEditor, renderer: renderLogBookNotes },
                { field: 'guardInitials', title: 'Guard Initials', width: 50, renderer: function (value, record) { return record.guardLogin ? record.guardLogin.guard.initial : ''; } },
                { width: 75, renderer: renderDailyLogManagement }
            ]
        };
    }

    function renderLogBookNotes(value, record, $cell) {
        $cell.on('keydown', function (e) {
            if (e.which === 13) {
                gridGuardLog.update(record.id);
            }
        });
        return record.notes;
    }

    function logBookNotesEditor($editorContainer, value) {
        var textAreaForNotes = $('<textarea class="form-control" rows="4" maxlength="2048" onpaste="return false" >' + value + '</textarea > ');
        $editorContainer.append(textAreaForNotes);
    }

    function renderDailyLogManagement(value, record, $cell, $displayEl, id) {
        if (record.isSystemEntry || record.guardLogin.guardId != $('#GuardLog_GuardLogin_GuardId').val())
            return;

        var $editBtn = $('<button class="btn btn-outline-primary mr-2" data-id="' + record.id + '"><i class="fa fa-pencil mr-2"></i>Edit</button>'),
            $deleteBtn = $('<button class="btn btn-outline-danger mt-2" data-id="' + record.id + '"><i class="fa fa-trash mr-2"></i>Delete</button>'),
            $updateBtn = $('<button class="btn btn-outline-success mr-2" data-id="' + record.id + '"><i class="fa fa-check-circle mr-2"></i>Update</button>').hide(),
            $cancelBtn = $('<button class="btn btn-outline-primary mt-2" data-id="' + record.id + '"><i class="fa fa-times-circle mr-2"></i>Cancel</button>').hide();

        $editBtn.on('click', function (e) {
            gridGuardLog.edit($(this).data('id'));
            $editBtn.hide();
            $deleteBtn.hide();
            $updateBtn.show();
            $cancelBtn.show();
        });

        $updateBtn.on('click', function (e) {
            gridGuardLog.update($(this).data('id'));
            $editBtn.show();
            $deleteBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $cancelBtn.on('click', function (e) {
            gridGuardLog.cancel($(this).data('id'));
            $editBtn.show();
            $deleteBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $deleteBtn.on('click', function (e) {
            gridGuardLog.removeRow($(this).data('id'));
        });

        $displayEl.append($editBtn)
            .append($deleteBtn)
            .append($updateBtn)
            .append($cancelBtn);
    }

    gridGuardLog = $('#guard_daily_log').grid(gridGuardLogSettings);

    if (gridGuardLog) {
        
        const bg_color_pale_yellow = '#fcf8d1';
        const bg_color_pale_red = '#ffcccc';
        const irEntryTypeIsAlarm = 2;
     
        gridGuardLog.on('rowDataBound', function (e, $row, id, record) {
            if (record.irEntryType) {
                $row.css('background-color', record.irEntryType === irEntryTypeIsAlarm ? bg_color_pale_red : bg_color_pale_yellow);
                /* add for check if dark mode is on start*/
                if ($('#toggleDarkMode').is(':checked')) {
                    $row.css('color', '#333');
                    $row.css('background-color', record.irEntryType === irEntryTypeIsAlarm ? bg_color_pale_red : bg_color_pale_yellow);
                }
                /* add for check if dark mode is on end*/
            }
        });

        gridGuardLog.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $('#loader').show();
            $.ajax({
                url: '/Guard/DailyLog?handler=SaveGuardLog',
                data: { guardlog: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {
                if (result.success) {
                    gridGuardLog.clear();
                    gridGuardLog.reload();
                }
                else {
                    let errorList = [];
                    result.errors.forEach(function (item) {
                        errorList.push(item.errorList[0]);
                    });
                    alert('Update failed. ' + errorList.join(', '));
                }

            }).fail(function () {
                console.log('error');
            }).always(function () {
                $('#loader').hide();
            });
        });

        gridGuardLog.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this entry?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Guard/DailyLog?handler=DeleteGuardLog',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridGuardLog.clear();
                    gridGuardLog.reload();
                }).fail(function () {
                    console.log('error');
                });
            } else {    
                gridGuardLog.clear();
                gridGuardLog.reload();
            }
        });
    }

    let gridGuardLogPreviousDay;

    gridGuardLogPreviousDay = $('#guard_daily_log_previous_day').grid({
        dataSource: '/Guard/DailyLog?handler=GuardLogs',
        params: { logBookId: $('#previousLogBookId').val(), logBookDate: $('#previousLogBookDateString').val() },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command', managementColumn: false },
        columns: [
            { field: 'clientSiteId', hidden: true },
            { field: 'eventDateTime', title: 'Time', width: 50, renderer: function (value, record) { return renderTime(value, record, false); } },
            { field: 'notes', title: 'Event / Notes', width: 450, editor: logBookNotesEditor, renderer: renderLogBookNotes },
            { field: 'guardInitials', title: 'Guard Initials', width: 50, renderer: function (value, record) { return record.guardLogin.guard.initial; } }
        ],
        initialized: function (e) {
            $('#wrapper_previous_day_log').hide();
            if ($('#previousLogBookId').val() !== '') {
                $('#wrapper_previous_day_log').show();
            }
        }
    });

    $('#GuardLog_Notes').on('keydown', function (e) {
        if (e.which === 13) {
            addGuardLog();
        }
    });

    $('#add_new_log').on('click', function () {
        addGuardLog();
    });

    function addGuardLog() {
        const today = new Date();
        $('#GuardLog_EventDateTime').val(today.getDate() + '-' + (today.getMonth() + 1) + '-' + today.getFullYear() + ' ' + $('#new_log_time').val());
        $('#GuardLog_TimePartOnly').val($('#new_log_time').val());
        $('#loader').show();
        $('#validation-summary ul').html('');
        $.ajax({
            url: '/Guard/DailyLog?handler=SaveGuardLog',
            data: $('#form_newlog').serialize(),
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) {
                gridGuardLog.clear();
                gridGuardLog.reload();
                $('#GuardLog_Notes').val('');
            }
            else {
                displayValidationSummary(result.errors);
            }
        }).fail(function () {
            alert('Error saving entry. Please logout and login again');
        }).always(function () {
            $('#loader').hide();
        });
    };

    $('#guard_offduty').on('click', function (e) {
        e.preventDefault();
        if (confirm('Are you sure you want to end your shift?')) {
            $('#loader').show();
            $.ajax({
                url: '/Guard/DailyLog?handler=UpdateOffDuty',
                data: { guardLoginId: $('#GuardLog_GuardLoginId').val(), clientSiteLogBookId: $('#GuardLog_ClientSiteLogBookId').val() },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    window.location.replace('/Guard/Login?t=gl');
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

    function displayValidationSummary(errors) {
        const summaryDiv = document.getElementById('validation-summary');
        summaryDiv.className = "validation-summary-errors";
        summaryDiv.querySelector('ul').innerHTML = '';
        errors.forEach(function (item) {
            const li = document.createElement('li');
            li.appendChild(document.createTextNode(item.errorList[0]));
            summaryDiv.querySelector('ul').appendChild(li);
        });
    }

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

    //*************** Admin Site Log  *************** //

    //Daily Log
    const today = new Date();
    const start = new Date(today.getFullYear(), today.getMonth(), 2);
    $('#dglAudtitFromDate').val(start.toISOString().substr(0, 10));
    $('#dglAudtitToDate').val(new Date().toISOString().substr(0, 10));

    let gridsiteLog;
    gridsiteLog = $('#dgl_site_log').grid({
        dataSource: '/Admin/AuditSiteLog?handler=DailyGuardSiteLogs',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        grouping: { groupBy: 'Date' },
        primaryKey: 'id',
        columns: [
            { field: 'clientSiteId', hidden: true },
            { field: 'eventDateTime', title: 'Time', width: 100, renderer: function (value, record) { return renderDateTime(value, record, false); } },
            { field: 'notes', title: 'Event / Notes', width: 440 },
            { field: 'guardInitials', title: 'Guard Initials', width: 60, renderer: renderGuardInitialColumn }
        ],
        paramNames: { page: 'pageNo' },
        pager: { limit: 100, sizes: [10, 50, 100, 500] }
    });

    $('#expand_dgl_audits').on('click', function () {
        gridsiteLog.expandAll();
    });

    $('#collapse_dgl_audits').on('click', function () {
        gridsiteLog.collapseAll();
    });

    if (gridsiteLog) {
        const bg_color_pale_yellow = '#fcf8d1';
        const bg_color_pale_red = '#ffcccc';
        const bg_color_white = '#ffffff';
        const irEntryTypeIsAlarm = 2;

        gridsiteLog.on('rowDataBound', function (e, $row, id, record) {
            let rowColor = bg_color_white;
            if (record.irEntryType) {
                rowColor = record.irEntryType === irEntryTypeIsAlarm ? bg_color_pale_red : bg_color_pale_yellow;
            }
            $row.css('background-color', rowColor);
        });
    }

    $('#dglClientSiteId').select2({
        placeholder: 'Select',
        theme: 'bootstrap4'
    });

    $('#dglClientType').on('change', function () {
        const clientTypeId = $(this).val();
        const clientSiteControl = $('#dglClientSiteId');
        var selectedOption = $(this).find("option:selected");
        var selectedText = selectedOption.text();
        $("#vklClientType").val(selectedText);
        $("#vklClientType").multiselect("refresh");
        gridsiteLog.clear();

        const clientSiteControlvkl = $('#vklClientSiteId');
        keyVehicleLogReport.clear().draw();
        clientSiteControlvkl.html('');

        clientSiteControl.html('');
        $.ajax({
            url: '/Admin/Settings?handler=ClientSites&typeId=' + clientTypeId,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                $('#dglClientSiteId').append(new Option('Select', '', true, true));
                data.map(function (site) {
                    $('#dglClientSiteId').append(new Option(site.name, site.id, false, false));
                });
                /* vkl multiselect */
                data.map(function (site) {
                    clientSiteControlvkl.append('<option value="' + site.id + '">' + site.name + '</option>');
                });
                clientSiteControlvkl.multiselect('rebuild');
            }
        });


    });


    $('#dglClientSiteId').on('change', function () {
        const clientTypeId = $(this).val();
        $("#vklClientSiteId").val(clientTypeId);
        $("#vklClientSiteId").multiselect("refresh");




    });

    $('#btnGenerateDglAuditReport').on('click', function () {

        if ($('#dglClientSiteId').val() === '') {
            alert('Please select a client site');
            return;
        }

        gridsiteLog.reload({
            clientSiteId: $('#dglClientSiteId').val(),
            logFromDate: $('#dglAudtitFromDate').val(),
            logToDate: $('#dglAudtitToDate').val(),
            excludeSystemLogs: $('#excludeSystemLog').prop("checked")
        });
    });

    $('#auditlog-zip-modal').on('show.bs.modal', function (event) {
        $('#btn-auditlog-zip-download').attr('href', '#');
        $('#btn-auditlog-zip-download').hide();
        $('#auditlog-zip-msg').show();

        if (logBookTypeForAuditZip === 1)
            downloadDailyGuardLogZipFile();
        else
            downloadKeyVehicleLogZipFile();
    });

    function downloadDailyGuardLogZipFile() {
        $.ajax({
            url: '/Admin/AuditSiteLog?handler=DownloadDailyGuardLogZip',
            type: 'POST',
            dataType: 'json',
            data: {
                clientSiteId: $('#dglClientSiteId').val(),
                logFromDate: $('#dglAudtitFromDate').val(),
                logToDate: $('#dglAudtitToDate').val()
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            if (!response.success) {
                $('#auditlog-zip-modal').modal('hide');
                new MessageModal({ message: 'Failed to generate zip file. ' + response.message }).showError();
            } else {
                $('#btn-auditlog-zip-download').attr('href', response.fileName);
                $('#btn-auditlog-zip-download').show();
                $('#auditlog-zip-msg').hide();
            }
        });
    }

    function downloadKeyVehicleLogZipFile() {
        $('#KeyVehicleLogAuditLogRequest_ClientSiteId').val($('#vklClientSiteId').val());
        $('#KeyVehicleLogAuditLogRequest_LogFromDate').val($('#vklAudtitFromDate').val());
        $('#KeyVehicleLogAuditLogRequest_LogToDate').val($('#vklAudtitToDate').val());
        $('#KeyVehicleLogAuditLogRequest_LogBookType').val(2);

        $.ajax({
            url: '/Admin/AuditSiteLog?handler=DownloadKeyVehicleLogZip',
            type: 'POST',
            dataType: 'json',
            data: $('#form_kvl_auditlog_request').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            if (!response.success) {
                $('#auditlog-zip-modal').modal('hide');
                new MessageModal({ message: 'Failed to generate zip file. ' + response.message }).showError();
            } else {
                $('#btn-auditlog-zip-download').attr('href', response.fileName);
                $('#btn-auditlog-zip-download').show();
                $('#auditlog-zip-msg').hide();
            }
        });
    }

    let logBookTypeForAuditZip;
    $('#btnDownloadDglAuditZip').on('click', function () {
        $('#vklClientType').val('');
        $('#vklClientSiteId').val('');
        logBookTypeForAuditZip = 1;
        if ($('#dglClientSiteId').val() === '') {
            alert('Please select a client site');
            return;
        }
        $('#auditlog-zip-modal').modal('show');
    });

    $('#duress_btn').on('click', function () {
        $.ajax({
            url: '/Guard/DailyLog?handler=SaveClientSiteDuress',
            data: {
                clientSiteId: $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val(),
                guardLoginId: $('#GuardLog_GuardLoginId').val(),
                logBookId: $('#GuardLog_ClientSiteLogBookId').val(),
                guardId: $('#GuardLog_GuardLogin_GuardId').val(),
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#duress_btn').removeClass('normal').addClass('active');
                $("#duress_status").addClass('font-weight-bold');
                $("#duress_status").text("Active");
            }           
            gridGuardLog.clear();
            gridGuardLog.reload();
        });
    });

    //Vehicle key Log

    $('#btn_reset_vklfilter').on('click', function () {
        $('#KeyVehicleLogAuditLogRequest_VehicleRego').val('');
        $('#KeyVehicleLogAuditLogRequest_CompanyName').val('');
        $('#KeyVehicleLogAuditLogRequest_PersonName').val('');
        $('#KeyVehicleLogAuditLogRequest_PersonType').val('');
        $('#KeyVehicleLogAuditLogRequest_EntryReason').val('');
        $('#KeyVehicleLogAuditLogRequest_Product').val('');
        $('#KeyVehicleLogAuditLogRequest_TruckConfig').val('');
        $('#KeyVehicleLogAuditLogRequest_TrailerType').val('');
        $('#KeyVehicleLogAuditLogRequest_ClientSitePocId').val('');
        $('#KeyVehicleLogAuditLogRequest_ClientSiteLocationId').val('');
        $('#KeyVehicleLogAuditLogRequest_KeyNo').val('');
        $('#listKeyVehicleLogAuditLogRequestKeyNo').val(null).trigger('change');
    });

    $('#btnDownloadVklAuditZip').on('click', function () {
        $('#dglClientType').val('');
        $('#dglClientSiteId').val('');
        logBookTypeForAuditZip = 2;
        if ($('#vklClientSiteId').val().length === 0) {
            alert('Please select a client site');
            return;
        }
        $('#auditlog-zip-modal').modal('show');
    });

    const todayDate = new Date();
    const startDate = new Date(todayDate.getFullYear(), today.getMonth(), 2);
    $('#vklAudtitFromDate').val(startDate.toISOString().substr(0, 10));
    $('#vklAudtitToDate').val(new Date().toISOString().substr(0, 10));

    // TODO: Duplicate function definition - take out
    function getTimeFromDateTime(value) {
        const mins = (value.getMinutes() < 10 ? '0' : '') + value.getMinutes();
        const hours = (value.getHours() < 10 ? '0' : '') + value.getHours();
        return hours + ':' + mins;
    }

    function convertDateTimeString(value) {
        return (value === null || value === undefined) ? '' : getTimeFromDateTime(new Date(value));
    }

    const groupColumn = 0;
    let keyVehicleLogReport = $('#vkl_site_log').DataTable({
        lengthMenu: [[75, 100, -1], [75, 100, "All"]],
        pageLength: 100,
        paging: true,
        ordering: false,
        order: [[groupColumn, 'asc']],
        info: false,
        searching: true,
        scrollX: true,
        data: [],
        columns: [
            { data: 'groupText', visible: false },
            { data: 'detail.clientSiteLogBook.clientSite.name' },
            { data: 'detail.entryTime', 'render': function (value) { return convertDateTimeString(value); } },
            { data: 'detail.entryTime', 'render': function (value) { return convertDateTimeString(value); } },
            { data: 'detail.exitTime', 'render': function (value) { return convertDateTimeString(value); } },
            { data: 'detail.timeSlotNo' },
            { data: 'detail.vehicleRego' },
            { data: 'plate' },
            { data: 'truckConfigText' },
            { data: 'trailerTypeText' },
            { data: 'detail.trailer1Rego' },
            { data: 'detail.trailer2Rego' },
            { data: 'detail.trailer3Rego' },
            { data: 'detail.trailer4Rego' },
            { data: 'detail.keyNo' },
            { data: 'detail.companyName' },
            { data: 'detail.personName' },
            { data: 'detail.mobileNumber' },
            { data: 'personTypeText' },
            { data: 'clientSitePocName' },
            { data: 'clientSiteLocationName' },
            { data: 'purposeOfEntry' },
            { data: 'detail.inWeight' },
            { data: 'detail.outWeight' },
            { data: 'detail.tareWeight' },
            { data: 'detail.notes' },
        ],
        drawCallback: function () {
            var api = this.api();
            var rows = api.rows({ page: 'current' }).nodes();
            var last = null;

            api.column(groupColumn, { page: 'current' })
                .data()
                .each(function (group, i) {
                    if (last !== group) {
                        $(rows)
                            .eq(i)
                            .before('<tr class="group bg-light text-dark"><td colspan="25">' + group + '</td></tr>');

                        last = group;
                    }
                });
        },
    });

    $('#listKeyVehicleLogAuditLogRequestKeyNo').select2({
        placeholder: "Select",
        theme: 'bootstrap4',
        allowClear: true,
        ajax: {
            url: '/Admin/AuditSiteLog?handler=ClientSiteKeys',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return {
                    clientSiteIds: $('#vklClientSiteId').val().join(';'),
                    searchKeyNo: params.term,
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
    }).on("select2:select", function (e) {
        $('#KeyVehicleLogAuditLogRequest_KeyNo').val(e.params.data.text);
    })
        .on("select2:clear", function (e) {
            $('#KeyVehicleLogAuditLogRequest_KeyNo').val('');
        });

    $('#vklClientSiteId').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });

    $('#vklClientSiteId').on('change', function () {
        if ($('#vklClientSiteId').val().length === 0) {
            alert('Please select a client site');
            return;
        }

        $.ajax({
            url: '/Admin/AuditSiteLog?handler=ClientSiteLocationsAndPocs&clientSiteIds=' + $(this).val().join(';'),
            type: 'GET',
            datatype: 'json',
        }).done(function (data) {
            $('#KeyVehicleLogAuditLogRequest_ClientSitePocId').html('');
            $('#KeyVehicleLogAuditLogRequest_ClientSiteLocationId').html('');
            data.sitePocs.map(function (result) {
                $('#KeyVehicleLogAuditLogRequest_ClientSitePocId').append('<option value="' + result.value + '">' + result.text + '</option>');
            });
            data.siteLocations.map(function (result) {
                $('#KeyVehicleLogAuditLogRequest_ClientSiteLocationId').append('<option value="' + result.value + '">' + result.text + '</option>');
            });
        });
    });

    $('#vklClientType').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });

    $('#vklClientType').on('change', function () {

        const clientType = $(this).val().join(';');
        const clientSiteControl = $('#vklClientSiteId');
        keyVehicleLogReport.clear().draw();

        clientSiteControl.html('');
        $.ajax({
            url: '/Admin/AuditSiteLog?handler=ClientSites&types=' + encodeURIComponent(clientType),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    clientSiteControl.append('<option value="' + site.value + '">' + site.text + '</option>');
                });
                clientSiteControl.multiselect('rebuild');
            }
        });
    });

    $('#btnGenerateVklAuditReport').on('click', function () {
        if ($('#vklClientSiteId').val().length === 0) {
            alert('Please select a client site');
            return;
        }
        $('#KeyVehicleLogAuditLogRequest_ClientSiteId').val($('#vklClientSiteId').val());
        $('#KeyVehicleLogAuditLogRequest_LogFromDate').val($('#vklAudtitFromDate').val());
        $('#KeyVehicleLogAuditLogRequest_LogToDate').val($('#vklAudtitToDate').val());
        $('#KeyVehicleLogAuditLogRequest_LogBookType').val(2);

        $.ajax({
            url: '/Admin/AuditSiteLog?handler=KeyVehicleSiteLogs',
            type: 'POST',
            dataType: 'json',
            data: $('#form_kvl_auditlog_request').serialize(),
        }).done(function (response) {
            keyVehicleLogReport.clear().rows.add(response).draw();
        });
    });

    //*************** Guard Log Settings  *************** //

    $('#btnDisableDataCollection').on('click', function () {
        $.ajax({
            url: '/Admin/GuardSettings?handler=UpdateSiteDataCollection',
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

    $('#ClientSiteCustomField_Name').editableSelect({
        effects: 'slide'
    });

    $('#ClientSiteCustomField_TimeSlot').editableSelect({
        effects: 'slide'
    });

    function settingsButtonRenderer(value, record) {
        return '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#gl-site-settings-modal" ' +
            'data-cs-id="' + record.id + '" data-cs-email="' + record.siteEmail + '" data-cs-landline="' + record.landLine + '" data-cs-duressemail="' + record.duressEmail + '" data-cs-duresssms="' + record.duressSms +
            '" data-cs-guardlog-emailto="' + record.guardLogEmailTo + '" data-cs-dbx-upload="' + record.siteUploadDailyLog +
            '" data-cs-name="' + record.clientSiteName + '"data-cs-datacollection-enabled ="' + record.dataCollectionEnabled + '"><i class="fa fa-pencil mr-2"></i>Edit</button>';
    }

    $('#gl-site-settings-modal').on('shown.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const siteId = button.data('cs-id');
        const siteName = button.data('cs-name');
        const siteEmail = button.data('cs-email');
        const duressEmail = button.data('cs-duressemail');
        const duressSms = button.data('cs-duresssms');
        const landLine = button.data('cs-landline');
        const isDataCollectionEnabled = button.data('cs-datacollection-enabled');

        const guardLogEmailTo = button.data('cs-guardlog-emailto');
        const isUpdateDailyLog = button.data('cs-dbx-upload');
        $('#site-settings-for').html(siteName);
        $('#gl_client_site_id').val(siteId);
        $('#ClientSiteKey_ClientSiteId').val(siteId);
        $('#ClientSiteCustomField_ClientSiteId').val(siteId);
        $('#gs_site_email').val(siteEmail);
        $('#gs_duress_email').val(duressEmail);
        $('#gs_duress_sms').val(duressSms);
        $('#gs_land_line').val(landLine);
        $('#gs_email_recipients').val(guardLogEmailTo);
        $('#enableLogDump').prop('checked', false);
        $('#cbxDisableDataCollection').prop('checked', !isDataCollectionEnabled);
        if (isUpdateDailyLog)
            $('#enableLogDump').prop('checked', true);
        gritdSmartWands.reload({ clientSiteId: $('#gl_client_site_id').val() });
        gridSitePatrolCars.reload({ clientSiteId: $('#gl_client_site_id').val() });
        loadCustomFields();
        gridSiteCustomFields.reload({ clientSiteId: $('#gl_client_site_id').val() });
        gridSiteLocations.reload({ clientSiteId: $('#gl_client_site_id').val() });
        gridSitePocs.reload({ clientSiteId: $('#gl_client_site_id').val() });
        gridClientSiteKeys.ajax.reload();
    });

    $('#gl-site-settings-modal').on('hide.bs.modal', function (event) {
        gridLogSiteSettings.reload({ type: $('#gl_client_type').val(), searchTerm: $('#search_sites_guard_settings').val() });
    });

    $('#btnSearchSitesOnDailyGuard').on('click', function () {
        gridLogSiteSettings.reload({ type: $('#gl_client_type').val(), searchTerm: $('#search_sites_guard_settings').val() });
    });

    $('#search_sites_guard_settings').on('keydown', function (e) {
        if (e.which === 13) {
            gridLogSiteSettings.reload({ type: $('#gl_client_type').val(), searchTerm: $('#search_sites_guard_settings').val() });
        }
    });

    let gridLogSiteSettings;

    gridLogSiteSettings = $('#gl_site_settings').grid({
        dataSource: '/Admin/GuardSettings?handler=ClientSiteWithLogSettings',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { width: 150, field: 'clientTypeName', title: 'Client Type' },
            { width: 250, field: 'clientSiteName', title: 'Client Site' },
            { width: 250, field: 'siteEmail', title: 'Site Email', hidden: true },
            { width: 250, field: 'landLine', title: 'Site Land Line', hidden: true },
            { width: 250, field: 'guardLogEmailTo', title: 'Email Recipients', hidden: true },
            { width: 50, field: 'siteUploadDailyLog', title: 'Daily Log Dump?', renderer: function (value, record) { return value === true ? '<i class="fa fa-check-circle text-success"></i>' : ''; } },
            { width: 50, field: 'hasSettings', title: 'Settings Available?', renderer: function (value, record) { return value === true ? '<i class="fa fa-check-circle text-success"></i>' : ''; } },
            { width: 100, renderer: settingsButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    $('#gl_client_type').on('change', function () {
        gridLogSiteSettings.reload({ type: $(this).val(), searchTerm: $('#search_sites_guard_settings').val() });
    });

    let gridSitePatrolCars;
    gridSitePatrolCars = $('#cs-patrol-cars').grid({
        dataSource: '/Admin/GuardSettings?handler=PatrolCar',
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
                url: '/Admin/GuardSettings?handler=PatrolCar',
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
                    url: '/Admin/GuardSettings?handler=DeletePatrolCar',
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


    let gritdSmartWands;
    gritdSmartWands = $('#cs-smart-wands').grid({
        dataSource: '/Admin/GuardSettings?handler=SmartWandSettings',
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
                url: '/Admin/GuardSettings?handler=SmartWandSettings',
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
                    url: '/Admin/GuardSettings?handler=DeleteSmartWandSettings',
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

    let gridClientSiteKeys = $('#cs_client_site_keys').DataTable({
        paging: true,
        ordering: true,
        order: [[1, "asc"]],
        info: false,
        searching: true,
        autoWidth: false,
        ajax: {
            url: '/Admin/GuardSettings?handler=ClientSiteKeys',
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
                url: '/Admin/GuardSettings?handler=DeleteClientSiteKey',
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

    $('#add_client_site_key').on('click', function () {
        resetClientSiteKeyModal();
        $('#client-site-key-modal').modal('show');
    });

    $('#btn_save_cs_key').on('click', function () {
        $.ajax({
            url: '/Admin/GuardSettings?handler=ClientSiteKey',
            data: $('#frm_add_key').serialize(),
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) {
                $('#client-site-key-modal').modal('hide');
                gridClientSiteKeys.ajax.reload();
            } else {
                displaySiteKeyValidationSummary(result.message);
            }
        });
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
        $('#client-site-key-modal').modal('hide');
    }

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

    $('#btnSaveGuardSiteSettings').on('click', function () {
        var isUpdateDailyLog = false;

        const token = $('input[name="__RequestVerificationToken"]').val();
        if ($('#enableLogDump').is(":checked")) {
            isUpdateDailyLog = true;
        }
        $.ajax({
            url: '/Admin/GuardSettings?handler=SaveSiteEmail',
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

    $('#btnSaveCustomFields').on('click', function () {
        $('#custom-field-validation ul').html('');
        $.ajax({
            url: '/Admin/GuardSettings?handler=CustomFields',
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

    function loadCustomFields() {
        $.ajax({
            url: '/Admin/GuardSettings?handler=CustomFields',
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
        dataSource: '/Admin/GuardSettings?handler=ClientSiteCustomFields',
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
                url: '/Admin/GuardSettings?handler=CustomFields',
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
                    url: '/Admin/GuardSettings?handler=DeleteClientSiteCustomField',
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

    let gridSitePocs;
    gridSitePocs = $('#cs-pocs').grid({
        dataSource: '/Admin/GuardSettings?handler=SitePocs',
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
                url: '/Admin/GuardSettings?handler=SitePoc',
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
                    url: '/Admin/GuardSettings?handler=DeleteSitePoc',
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
        dataSource: '/Admin/GuardSettings?handler=SiteLocations',
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
                url: '/Admin/GuardSettings?handler=SiteLocation',
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
                    url: '/Admin/GuardSettings?handler=DeleteSiteLocation',
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

    /****** Guards *******/

    function renderGuardActiveCell(value, type, data) {
        if (type === 'display') {
            let cellValue = value ? '<i class="fa fa-check-circle text-success"></i>' : '<i class="fa fa-times-circle text-danger"></i>';
            if (data.dateEnrolled) {
                cellValue += '<br/> <span class="small">Enrolled: ' + getFormattedDate(new Date(data.dateEnrolled), null, ' ') + '</span>';
            }
            return cellValue;
        }
        return value;
    }

    function format_guards_child_row(d) {
        var val = d;
        return val;
    }

    var guardSettings = $('#guard_settings').DataTable({
        pageLength: 50,
        autoWidth: false,
        ajax: '/Admin/GuardSettings?handler=Guards',
        columns: [{
            className: 'dt-control',
            orderable: false,
            data: null,
            width: '2%',
            defaultContent: '',
        },
        { data: 'name', width: "10%" },
        { data: 'securityNo', width: "10%" },
        { data: 'initial', orderable: false, width: "5%" },
        { data: 'state', width: "5%" },
        { data: 'provider', width: "10%" },
        { data: 'clientSites', orderable: false, width: "15%" },
        {
            data: 'isActive', className: "text-center", width: "10%", 'render': function (value, type, data) {
                return renderGuardActiveCell(value, type, data);
            }
        },
        {
            targets: -1,
            data: null,
            defaultContent: '<button  class="btn btn-outline-primary mr-2" name="btn_edit_guard"><i class="fa fa-pencil mr-2"></i>Edit</button>',
            orderable: false,
            className: "text-center",
            width: "8%"
        },
        ]
    });

    $('#guard_settings tbody').on('click', 'td.dt-control', function () {
        var tr = $(this).closest('tr');
        var row = guardSettings.row(tr);

        $.ajax({
            type: 'GET',
            url: '/Admin/Guardsettings?handler=GuardLicenseAndCompliance',
            data: { guardId: row.data().id },
        }).done(function (response) {
            if (row.child.isShown()) {
                row.child.hide();
                tr.removeClass('shown');
            } else {
                row.child(format_guards_child_row(response), 'bg-light').show();
                tr.addClass('shown');
            }
        });
    });

    $('#guard_settings tbody').on('click', 'button[name=btn_edit_guard]', function () {
        resetGuardDetailsModal();
        $('.btn-add-guard-addl-details').show();

        var data = guardSettings.row($(this).parents('tr')).data();

        $('#Guard_Name').val(data.name);
        $('#Guard_SecurityNo').val(data.securityNo);
        $('#Guard_Initial').val(data.initial);
        $('#Guard_State').val(data.state);
        $('#Guard_Provider').val(data.provider);
        $('#Guard_Mobile').val(data.mobile)
        $('#Guard_Email').val(data.email)
        $('#Guard_Id').val(data.id);
        $('#cbIsActive').prop('checked', data.isActive);
        $('#cbIsRCAccess').prop('checked', data.isRCAccess);
        $('#cbIsKPIAccess').prop('checked', data.isKPIAccess);
        $('#addGuardModal').modal('show');
        $('#GuardLicense_GuardId').val(data.id);
        $('#GuardCompliance_GuardId').val(data.id);

        gridGuardLicenses.ajax.reload();
        gridGuardCompliances.ajax.reload();
    });

    $('#btn_add_guard_top, #btn_add_guard_bottom').on('click', function () {
        gridGuardLicenses.clear().draw();
        gridGuardCompliances.clear().draw();

        $('.btn-add-guard-addl-details').hide();
        resetGuardDetailsModal();
        $('#addGuardModal').modal('show');
    });

    function resetGuardDetailsModal() {
        $('#Guard_Name').val('');
        $('#Guard_SecurityNo').val('');
        $('#Guard_Initial').val('');
        $('#Guard_State').val('');
        $('#Guard_Provider').val('');
        $('#Guard_Email').val('');
        $('#Guard_Mobile').val('');
        $('#Guard_Mobile').val('+61 4');
        $('#Guard_Id').val('-1');
        $('#cbIsActive').prop('checked', true);
        $('#cbIsRCAccess').prop('checked', false);
        $('#cbIsKPIAccess').prop('checked', false);
        $('#glValidationSummary').html('');
    }

    $('#btn_save_guard').on('click', function () {
        clearGuardValidationSummary('glValidationSummary');
        $('#guard_saved_status').hide();
        $('#Guard_IsActive').val($(cbIsActive).is(':checked'));       
        $('#Guard_IsRCAccess').val($(cbIsRCAccess).is(':checked'));
        $('#Guard_IsKPIAccess').val($(cbIsKPIAccess).is(':checked'));
        $.ajax({
            url: '/Admin/GuardSettings?handler=Guards',
            data: $('#frm_add_guard').serialize(),
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#guardId').val(result.guardId);
                $('#GuardLicense_GuardId').val(result.guardId);
                $('#GuardCompliance_GuardId').val(result.guardId);
                if (result.initalsChangedMessage !== '') {
                    alert(result.initalsChangedMessage);
                    $('#Guard_Initial').val(result.initalsUsed);
                }
                $('#guard_saved_status').show().delay(2000).hide(0);
                $('.btn-add-guard-addl-details').show();
                gridGuardLicenses.ajax.reload();
                guardSettings.ajax.reload(null, false);
            } else {
                displayGuardValidationSummary('glValidationSummary', result.message);
            }
        });
    });

    /****** Daily Guard  Custom Field Logs *******/
    let gridCustomFieldLogs;

    if ($('#custom_field_log').length === 1) {
        $.ajax({
            type: "GET",
            url: '/Guard/DailyLog?handler=CustomFieldConfig&clientSiteId=' + $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val(),
            contentType: "application/json",
            async: false,
            dataType: "json",

            success: function (data) {
                let columnData = [];
                columnData.push({ renderer: renderCustomFieldLogManagement, align: 'center', width: 60 });

                $.each(data, function (i, d) {
                    let colAlign = 'right', colEditor = true;
                    let colWidth = 80;
                    if (d.key === 'timeSlot') {
                        colAlign = 'center';
                        colEditor = false;
                        colWidth = 60;
                    }
                    columnData.push({ field: d.key, title: d.value, align: colAlign, editor: colEditor, width: colWidth });
                });

                gridCustomFieldLogs = $('#custom_field_log').grid({
                    dataSource: '/Guard/DailyLog?handler=CustomFieldLogs' +
                        '&logBookId=' + $('#GuardLog_ClientSiteLogBookId').val() +
                        '&clientSiteId=' + $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val(),
                    uiLibrary: 'bootstrap4',
                    iconsLibrary: 'fontawesome',
                    primaryKey: 'timeSlot',
                    fontSize: '13px',
                    inlineEditing: { mode: 'command', managementColumn: false },
                    columns: columnData,
                    initialized: function (e) {
                        $(e.target).find('thead tr th:first').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
                    }
                });
            },
            error: function (data) {
                alert(data);
            }
        });
    }

    if (gridCustomFieldLogs) {
        gridCustomFieldLogs.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Guard/DailyLog?handler=SaveCustomFieldLog&logBookId=' + $('#GuardLog_ClientSiteLogBookId').val(),
                data: { records: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {
                gridCustomFieldLogs.clear();
                gridCustomFieldLogs.reload();
            }).fail(function () {
                console.log('error');
            });
        });

        gridCustomFieldLogs.on('dataBound', function (e, records, totalRecords) {
            $(this).hide();
            if (totalRecords > 0) $(this).show();
        });
    }

    function renderCustomFieldLogManagement(value, record, $cell, $displayEl, id) {
        var $editBtn = $('<button class="btn btn-outline-primary btn-dgl-edit mt-1" data-id="' + id + '"><i class="fa fa-pencil"></i></button>'),
            $updateBtn = $('<button class="btn btn-outline-success btn-dgl-edit mt-1" data-id="' + id + '"><i class="fa fa-check-circle"></i></button>').hide(),
            $cancelBtn = $('<button class="btn btn-outline-danger btn-dgl-edit mt-1" data-id="' + id + '"><i class="fa fa-times-circle"></i></button>').hide();

        $editBtn.on('click', function (e) {
            gridCustomFieldLogs.edit($(this).data('id'));
            $editBtn.hide();
            $updateBtn.show();
            $cancelBtn.show();
        });

        $updateBtn.on('click', function (e) {
            gridCustomFieldLogs.update($(this).data('id'));
            $editBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $cancelBtn.on('click', function (e) {
            gridCustomFieldLogs.cancel($(this).data('id'));
            $editBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $displayEl.append($editBtn)
            .append($updateBtn)
            .append($cancelBtn);
    }

    /****** Daily Guard  Patrol Car Logs *******/
    function renderPatrolCar(value, record) {
        patrolCar = record.clientSitePatrolCar.model;
        rego = record.clientSitePatrolCar.rego;
        return patrolCar + " - " + rego + " - KM @ 00:01 HRS";
    }

    function renderPatrolCarLogManagement(value, record, $cell, $displayEl, id) {

        var $editBtn = $('<button class="btn btn-outline-primary btn-dgl-edit" data-id="' + record.id + '"><i class="fa fa-pencil"></i></button>'),
            $updateBtn = $('<button class="btn btn-outline-success btn-dgl-edit mr-2" data-id="' + record.id + '"><i class="fa fa-check-circle"></i></button>').hide(),
            $cancelBtn = $('<button class="btn btn-outline-danger btn-dgl-edit" data-id="' + record.id + '"><i class="fa fa-times-circle"></i></button>').hide();

        $editBtn.on('click', function (e) {
            gridSitePatrolCarLogs.edit($(this).data('id'));
            $editBtn.hide();
            $updateBtn.show();
            $cancelBtn.show();
        });

        $updateBtn.on('click', function (e) {
            gridSitePatrolCarLogs.update($(this).data('id'));
            $editBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $cancelBtn.on('click', function (e) {
            gridSitePatrolCarLogs.cancel($(this).data('id'));
            $editBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $displayEl.append($editBtn)
            .append($updateBtn)
            .append($cancelBtn);
    }

    let gridSitePatrolCarLogs;
    gridSitePatrolCarLogs = $('#patrol_cars_log').grid({
        dataSource: '/Guard/DailyLog?handler=PatrolCarLogs&logBookId=' + $('#GuardLog_ClientSiteLogBookId').val() + '&clientSiteId=' + $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val(),
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        fontSize: '13px',
        inlineEditing: { mode: 'command', managementColumn: false },
        columns: [
            { field: 'id', title: 'Id', hidden: true },
            { field: 'patrolCarId', title: 'PatrolCarId', hidden: true, renderer: function (value, record) { return record.PatrolCarId; } },
            { field: 'clientSiteLogBookId', title: 'ClientSiteLogBookId', hidden: true, renderer: function (value, record) { return record.clientSiteLogBookId; } },
            { field: 'patrolCar', title: 'Patrol Cars', editor: false, renderer: function (value, record) { return renderPatrolCar(value, record); }, width: 75 },
            { field: 'mileage', title: 'Kms', align: 'right', editor: true, renderer: function (value, record) { return record.mileageText; }, width: 15 },
            { renderer: renderPatrolCarLogManagement, align: 'center', width: 10 }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridSitePatrolCarLogs) {
        gridSitePatrolCarLogs.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Guard/DailyLog?handler=PatrolCarLog',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {
                gridSitePatrolCarLogs.clear();
                gridSitePatrolCarLogs.reload({
                    logBookId: $('#GuardLog_ClientSiteLogBookId').val(),
                    clientSiteId: $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val()
                });
            }).fail(function () {
                console.log('error');
            });
        });

        gridSitePatrolCarLogs.on('dataBound', function (e, records, totalRecords) {
            $(this).hide();
            if (totalRecords > 0) $(this).show();
        });
    }

    /****** Guard  Licenses *******/
    function resetGuardLicenseAddModal() {
        $('#GuardLicense_Id').val('');
        $('#GuardLicense_LicenseNo').val('');
        $('#GuardLicense_LicenseType').val('');
        $('#GuardLicense_Reminder1').val('');
        $('#GuardLicense_Reminder2').val('');
        $('#GuardLicense_ExpiryDate').val('');
        $('#GuardLicense_FileName').val('');
        $('#guardLicense_fileName').text('None');
        clearGuardValidationSummary('licenseValidationSummary');
    }

    let gridGuardLicenses = $('#tbl_guard_licenses').DataTable({
        autoWidth: false,
        ordering: false,
        searching: false,
        paging: false,
        info: false,
        ajax: {
            url: '/Admin/GuardSettings?handler=GuardLicense',
            data: function (d) {
                d.guardId = $('#GuardLicense_GuardId').val();
            },
            dataSrc: ''
        },
        columns: [
            { data: 'licenseNo', width: "10%" },
            { data: 'licenseTypeText', width: '5%' },
            { data: 'expiryDate', width: '10%', orderable: true },
            { data: 'reminder1', width: "3%" },
            { data: 'reminder2', width: '3%' },
            { data: 'fileName', width: '5%' },
            {
                targets: -1,
                data: null,
                defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_edit_guard_license"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
                    '<button  class="btn btn-outline-danger mr-2" name="btn_delete_guard_licence"><i class="fa fa-trash mr-2"></i>Delete</button>',
                width: '12%'
            }],
        columnDefs: [{
            targets: 5,
            data: 'fileName',
            render: function (data, type, row, meta) {
                if (data)
                    return '<a href="' + row.fileUrl + '" target="_blank">' + data + '</a>';
                return '-';
            }
        }],
        'createdRow': function (row, data, index) {
            if (data.expiryDate !== null) {
                $('td', row).eq(2).html(getFormattedDate(new Date(data.expiryDate), null, ' '));
            }
        },
    });

    $('#btnAddGuardLicense').on('click', function () {
        resetGuardLicenseAddModal();
        $('#addGuardLicenseModal').modal('show');
    });

    $('#tbl_guard_licenses tbody').on('click', 'button[name=btn_edit_guard_license]', function () {
        resetGuardLicenseAddModal();
        var data = gridGuardLicenses.row($(this).parents('tr')).data();
        $('#GuardLicense_LicenseNo').val(data.licenseNo);
        $('#GuardLicense_LicenseType').val(data.licenseType);
        $('#GuardLicense_Reminder1').val(data.reminder1);
        $('#GuardLicense_Reminder2').val(data.reminder2);
        if (data.expiryDate) {
            $('#GuardLicense_ExpiryDate').val(data.expiryDate.split('T')[0]);
        }
        $('#GuardLicense_Id').val(data.id);
        $('#GuardLicense_GuardId').val(data.guardId);
        $('#GuardLicense_FileName').val(data.fileName);
        $('#guardLicense_fileName').text(data.fileName ? data.fileName : 'None');
        $('#addGuardLicenseModal').modal('show');
    });

    $('#tbl_guard_licenses tbody').on('click', 'button[name=btn_delete_guard_licence]', function () {
        var data = gridGuardLicenses.row($(this).parents('tr')).data();
        if (confirm('Are you sure want to delete this Guard License?')) {
            $.ajax({
                type: 'POST',
                url: '/Admin/GuardSettings?handler=DeleteGuardLicense',
                data: { 'id': data.id },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success)
                    gridGuardLicenses.ajax.reload();
            })
        }
    });

    $('#upload_license_file').on('change', function () {
        const file = $(this).get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        if (!fileExtn || 'jpg,jpeg,png,bmp,pdf'.indexOf(fileExtn) < 0) {
            alert('Please select a valid file type');
            return false;
        }

        const formData = new FormData();
        formData.append("file", file);
        formData.append('guardId', $('#GuardLicense_GuardId').val());

        $.ajax({
            type: 'POST',
            url: '/Admin/GuardSettings?handler=UploadGuardAttachment',
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            $('#GuardLicense_FileName').val(data.fileName);
            $('#guardLicense_fileName').text(data.fileName ? data.fileName : 'None');
        }).fail(function () {
        }).always(function () {
            $('#upload_license_file').val('');
        });
    });

    $('#delete_license_file').on('click', function () {
        const guardLicenseId = $('#GuardLicense_Id').val();
        if (!guardLicenseId || parseInt(guardLicenseId) <= 0)
            return false;

        if (confirm('Are you sure want to remove the attachment')) {
            $.ajax({
                url: '/Admin/GuardSettings?handler=DeleteGuardAttachment',
                type: 'POST',
                data: {
                    id: guardLicenseId,
                    type: 'l'
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $('#GuardLicense_FileName').val('');
                    $('#guardLicense_fileName').text('None');
                    gridGuardLicenses.ajax.reload();
                }
                else {
                    displayGuardValidationSummary('licenseValidationSummary', 'Delete failed.');
                }
            });
        }
    });

    $('#btn_save_guard_license').on('click', function () {
        clearGuardValidationSummary('licenseValidationSummary');
        $('#loader').show();
        $.ajax({
            url: '/Admin/GuardSettings?handler=SaveGuardLicense',
            data: $('#frm_add_license').serialize(),
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#addGuardLicenseModal').modal('hide');
                gridGuardLicenses.ajax.reload();
                if (!result.dbxUploaded) {
                    displayGuardValidationSummary('licenseValidationSummary', 'License details saved successfully. However, upload to Dropbox failed.');
                }
            } else {
                displayGuardValidationSummary('licenseValidationSummary', result.message);
            }
        }).always(function () {
            $('#loader').hide();
        });
    });

    /****** Guard  Compliances *******/

    function resetGuardComplianceAddModal() {
        $('#GuardCompliance_Id').val('');
        $('#GuardCompliance_ReferenceNo').val('');
        $('#GuardCompliance_Description').val('');
        $('#GuardCompliance_Reminder1').val('');
        $('#GuardCompliance_Reminder2').val('');
        $('#GuardCompliance_ExpiryDate').val('');
        $('#GuardCompliance_FileName').val('');
        $('#guardCompliance_fileName').text('None');
        $('#GuardCompliance_HrGroup').val('');
        clearGuardValidationSummary('complianceValidationSummary');
    }

    $('#btnAddGuardCompliance').on('click', function () {
        resetGuardComplianceAddModal();
        $('#addGuardCompliancesModal').modal('show');
    })

    let gridGuardCompliances = $('#tbl_guard_compliances').DataTable({
        autoWidth: false,
        ordering: false,
        searching: false,
        paging: false,
        info: false,
        ajax: {
            url: '/Admin/GuardSettings?handler=GuardCompliances',
            data: function (d) {
                d.guardId = $('#GuardCompliance_GuardId').val();
            },
            dataSrc: ''
        },
        columns: [
            { data: 'referenceNo', width: "6%" },
            { data: 'hrGroupText', width: "6%" },
            { data: 'description', width: "7%" },
            { data: 'expiryDate', width: "8%" },
            { data: 'reminder1', width: "3%" },
            { data: 'reminder2', width: "3%" },
            { data: 'fileName', width: "10%" },
            {
                targets: -1,
                data: null,
                defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_edit_guard_compliance"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
                    '<button  class="btn btn-outline-danger mr-2" name="btn_delete_guard_compliance"><i class="fa fa-trash mr-2"></i>Delete</button>',
                orderable: false,
                width: "12%"
            }],
        columnDefs: [{
            targets: 5,
            data: 'fileName',
            render: function (data, type, row, meta) {
                if (data)
                    return '<a href="' + row.fileUrl + '" target="_blank">' + data + '</a>';
                return '-';
            }
        }],
        'createdRow': function (row, data, index) {
            if (data.expiryDate !== null) {
                $('td', row).eq(3).html(getFormattedDate(new Date(data.expiryDate), null, ' '));

            }
        },
    });

    $('#tbl_guard_compliances tbody').on('click', 'button[name=btn_edit_guard_compliance]', function () {
        resetGuardComplianceAddModal();
        var data = gridGuardCompliances.row($(this).parents('tr')).data();
        $('#GuardCompliance_ReferenceNo').val(data.referenceNo);
        $('#GuardCompliance_Description').val(data.description);
        $('#GuardCompliance_Reminder1').val(data.reminder1);
        $('#GuardCompliance_Reminder2').val(data.reminder2);
        if (data.expiryDate) {
            $('#GuardCompliance_ExpiryDate').val(data.expiryDate.split('T')[0]);
        }
        $('#GuardCompliance_Id').val(data.id);
        $('#GuardCompliance_FileName').val(data.fileName);
        $('#guardCompliance_fileName').text(data.fileName ? data.fileName : 'None');
        $('#GuardCompliance_HrGroup').val(data.hrGroup);
        $('#addGuardCompliancesModal').modal('show');
    });

    $('#tbl_guard_compliances tbody').on('click', 'button[name=btn_delete_guard_compliance]', function () {
        var data = gridGuardCompliances.row($(this).parents('tr')).data();
        if (confirm('Are you sure want to delete this Guard Compliance?')) {
            $.ajax({
                type: 'POST',
                url: '/Admin/GuardSettings?handler=DeleteGuardCompliance',
                data: { 'id': data.id },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success)
                    gridGuardCompliances.ajax.reload();
            })
        }
    });

    $('#btn_save_guard_compliance').on('click', function () {
        clearGuardValidationSummary('complianceValidationSummary');
        $('#loader').show();
        $.ajax({
            url: '/Admin/GuardSettings?handler=SaveGuardCompliance',
            data: $('#frm_add_compliance').serialize(),
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                $('#addGuardCompliancesModal').modal('hide');
                gridGuardCompliances.ajax.reload();
                if (!result.dbxUploaded) {
                    displayGuardValidationSummary('complianceValidationSummary', 'Compliance details saved successfully. However, upload to Dropbox failed.');
                }
            } else {
                displayGuardValidationSummary('complianceValidationSummary', result.message);
            }
        }).always(function () {
            $('#loader').hide();
        });
    });

    $('#upload_compliance_file').on('change', function () {
        const file = $(this).get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        if (!fileExtn || 'jpg,jpeg,png,bmp,pdf'.indexOf(fileExtn) < 0) {
            alert('Please select a valid file type');
            return false;
        }

        const formData = new FormData();
        formData.append("file", file);
        formData.append('guardId', $('#GuardCompliance_GuardId').val());

        $.ajax({
            type: 'POST',
            url: '/Admin/GuardSettings?handler=UploadGuardAttachment',
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            $('#GuardCompliance_FileName').val(data.fileName);
            $('#guardCompliance_fileName').text(data.fileName ? data.fileName : 'None');
        }).fail(function () {
        }).always(function () {
            $('#upload_compliance_file').val('');
        });
    });

    $("#LoginConformationBtnRC").on('click', function () {
        $('#txt_securityLicenseNoRC').val('');
        clearGuardValidationSummary('GuardLoginValidationSummaryRC');
        $("#modelGuardLoginConRc").modal("show");
        return false;
    });

    $('#btnGuardLoginRC').on('click', function () {
        $('#Access_permission_RC_status').hide();
        const securityLicenseNo = $('#txt_securityLicenseNoRC').val();
        if (securityLicenseNo === '') {
            displayGuardValidationSummary('GuardLoginValidationSummaryRC', 'Please enter the security license No ');
        }
        else {
            $.ajax({
                url: '/Admin/GuardSettings?handler=GuardDetailsForRCLogin',
                type: 'POST',
                data: {
                    securityLicenseNo: securityLicenseNo,
                    type: 'RC'
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.accessPermission) {
                    $('#txt_securityLicenseNoRC').val('');
                    $('#modelGuardLoginConRC').modal('hide');
                    clearGuardValidationSummary('GuardLoginValidationSummaryRC');
                    $('#Access_permission_RC_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Redirecting to Radio Checklist (RC). Please wait...').show();
                    window.location.href = '/Radio/Check';
                    
                }
                else {
                    $('#txt_securityLicenseNoRC').val('');
                    if (result.successCode === 0) {
                        displayGuardValidationSummary('GuardLoginValidationSummaryRC', result.successMessage);
                    }
                }
            });

        }
    });
    /* Show login conformation popup for KPI */
    $("#LoginConformationBtnKPI").on('click', function () {
        clearGuardValidationSummary('GuardLoginValidationSummary');
        $('#txt_securityLicenseNo').val('');
        $("#modelGuardLoginCon").modal("show");
        return false;
    });
    /* Check if Guard can access the KPI */
    $('#btnGuardLoginKPI').on('click', function () {
        const securityLicenseNo = $('#txt_securityLicenseNo').val();
        if (securityLicenseNo === '') {
            displayGuardValidationSummary('GuardLoginValidationSummary', 'Please enter the security license No ');
        }
        else {
            $.ajax({
                url: '/Admin/GuardSettings?handler=GuardDetailsForRCLogin',
                type: 'POST',
                data: {
                    securityLicenseNo: securityLicenseNo,
                    type: 'KPI'
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.accessPermission) {
                    $('#txt_securityLicenseNo').val('');
                    $('#modelGuardLoginCon').modal('hide');
                    window.location.href = 'https://kpi.cws-ir.com/Dashboard?Sl=' + securityLicenseNo + "&&lud=" + result.loggedInUserId + "&&guid=" + result.guId;/*live server*/
                    /*window.location.href = 'https://localhost:44378/Dashboard?Sl=' + securityLicenseNo + "&&lud=" + result.loggedInUserId + "&&guid=" + result.guId;*//*local testing*/
                    /*window.location.href = 'http://kpi.c4i-system.com/Dashboard?Sl=' + securityLicenseNo + "&&lud=" + result.loggedInUserId + "&&guid=" + result.guId; /*test server*/
                    clearGuardValidationSummary('GuardLoginValidationSummary');
                }
                else {
                    $('#txt_securityLicenseNo').val('');
                    if (result.successCode === 0) {
                        displayGuardValidationSummary('GuardLoginValidationSummary', result.successMessage);
                    }
                }
            });

        }
    });



    $('#delete_compliance_file').on('click', function () {
        const guardComplianceId = $('#GuardCompliance_Id').val();
        if (!guardComplianceId || parseInt(guardComplianceId) <= 0)
            return false;

        if (confirm('Are you sure want to remove the attachment')) {
            $.ajax({
                url: '/Admin/GuardSettings?handler=DeleteGuardAttachment',
                type: 'POST',
                data: {
                    id: guardComplianceId,
                    type: 'c'
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $('#GuardCompliance_FileName').val('');
                    $('#guardCompliance_fileName').text('None');
                    gridGuardCompliances.ajax.reload();
                }
                else {
                    displayGuardValidationSummary('complianceValidationSummary', 'Delete failed.');
                }
            });
        }
    });
});