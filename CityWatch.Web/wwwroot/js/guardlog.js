
var FileuploadFileChanged = null;
$(function () {

    // To fix the Datatable column header issue when hidden inside tab
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        $($.fn.dataTable.tables(true)).DataTable()
            .columns.adjust();
        //.responsive.recalc();
    });

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
    /* code for AccesGuard Dropdown*/
    $('#Guard_Access').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });

    $("#fileUpload").fileUpload();


    /*P1-203 ADMIN USER PROFILE-START*/
    //$('#Guard_Access').on('change', function () {

    //    var newval = $(this).val();
    //    var newval1 = newval[newval.length - 1];
    //    if (parseInt(newval1) == 6) {
    //        $(".multiselect-option input[type=checkbox]:checked").each(function () {
    //            var isChecked1 = $(this).is(':checked');
    //            if (isChecked1 == true) {
    //                var new1 = $(this).val();
    //                if (parseInt(new1) == 5)

    //                    $(".multiselect-option input[type=checkbox][value='" + 5 + "']").prop("checked", false);
    //                if (parseInt(new1) == 4)
    //                    $(".multiselect-option input[type=checkbox][value='" + 4 + "']").prop("checked", false);
    //            }

    //        });
    //    }
    //    if (parseInt(newval1) == 5) {
    //        $(".multiselect-option input[type=checkbox]:checked").each(function () {
    //            var isChecked1 = $(this).is(':checked');
    //            if (isChecked1 == true) {
    //                var new1 = $(this).val();
    //                if (parseInt(new1) == 6)
    //                    $(".multiselect-option input[type=checkbox][value='" + 6 + "']").prop("checked", false);
    //                if (parseInt(new1) == 4)
    //                    $(".multiselect-option input[type=checkbox][value='" + 4 + "']").prop("checked", false);
    //            }

    //        });
    //    }
    //    //$("#Guard_Access").multiselect();
    //    //$("#Guard_Access").multiselect("refresh");

    //});
    /*P1-203 ADMIN USER PROFILE-END*/
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


                if (isPosition) {
                    /* new Code for select deafult postion if manning deatils exist Start11/09/2024*/
                    $.ajax({
                        url: '/Guard/Login?handler=ManningDeatilsForTheSite&siteName=' + encodeURIComponent(clientSiteName ? clientSiteName : $('#GuardLogin_ClientSiteName').val()),
                        type: 'GET',
                        dataType: 'json',

                    }).done(function (result) {
                        if (result.success) {
                            if (result.positionIdDefault != '') {

                                $('#GuardLogin_IsPosition').prop('checked', true);
                                getSmartWandOrOfficerPositionOnSiteChange(true, clientSiteName, result.positionIdDefault);
                                /*smart_Wand_Or_Position.html('');
                                smart_Wand_Or_Position.append('<option value="">Select</option>').attr("selected", "selected");
                                data.map(function (result) {
            
                                    smart_Wand_Or_Position.append('<option value="' + result.value + '">' + result.text + '</option>');
            
            
            
            
                                });
                                smart_Wand_Or_Position.val(result.positionIdDefault).trigger('change');*/
                            }
                        }

                    }).fail(function () {

                    }).always(function () {

                    });

                    /* new Code for select deafult postion if manning deatils exist end*/

                }



            }


        });
    }



    function getSmartWandOrOfficerPositionOnSiteChange(isPosition, clientSiteName, smartWandOrPositionId) {


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
        /* p1 - 224 RC Bypass for IR - start*/
        $('#GuardLogin_Guard_Gender').val('');
        //p1 - 224 RC Bypass for IR - end
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
                    if (result.guard.mobile == null || result.guard.mobile == '+61 4') {
                        $("#divGuardMobile").show();
                        result.guard.mobile = '+61 4';
                    }
                    else {
                        $("#divGuardMobile").hide();
                    }

                    $('#GuardLogin_Guard_Email').val(result.guard.email);
                    $('#GuardLogin_Guard_Mobile').val(result.guard.mobile);
                    $('#GuardLogin_Guard_Initial').val(result.guard.initial);
                    /* p1 - 224 RC Bypass for IR - start*/
                    $('#GuardLogin_Guard_Gender').val(result.guard.gender);
                    //p1 - 224 RC Bypass for IR - end
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
                //HRList Status
                $('#client_status_0').css('color', result.hR1);
                $('#client_status_1').css('color', result.hR2);
                $('#client_status_2').css('color', result.hR3);


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
        //getSmartWandOrOfficerPositionOnSiteChange(isPosition);
        $('#GuardLogin_SmartWandOrPosition').prop('disabled', false);
        var clientSiteName = $(this).val();
        /* new Code for select deafult postion if manning deatils exist Start11/09/2024*/
        $.ajax({
            url: '/Guard/Login?handler=ManningDeatilsForTheSite&siteName=' + encodeURIComponent(clientSiteName ? clientSiteName : $('#GuardLogin_ClientSiteName').val()),
            type: 'GET',
            dataType: 'json',

        }).done(function (result) {
            if (result.success) {
                if (result.positionIdDefault != '') {

                    $('#GuardLogin_IsPosition').prop('checked', true);
                    getSmartWandOrOfficerPositionOnSiteChange(true, clientSiteName, result.positionIdDefault);
                    /*smart_Wand_Or_Position.html('');
                    smart_Wand_Or_Position.append('<option value="">Select</option>').attr("selected", "selected");
                    data.map(function (result) {

                        smart_Wand_Or_Position.append('<option value="' + result.value + '">' + result.text + '</option>');




                    });
                    smart_Wand_Or_Position.val(result.positionIdDefault).trigger('change');*/
                }
            }

        }).fail(function () {

        }).always(function () {

        });

        /* new Code for select deafult postion if manning deatils exist end*/




        ////To Get the Critical Documents start
        //var ClientSiteName = $('#GuardLogin_ClientSiteName').val();
        //$.ajax({
        //    url: '/Guard/Login?handler=CriticalDocumentsList&ClientSiteName=' + ClientSiteName,
        //    type: 'GET',
        //    dataType: 'json',
        //}).done(function (result) {
        //    var ss = 'kk';
        //    console.log(result);
        //    if (result.length == 0) {
        //        $('#client_status_0').css('color', 'red');
        //        $('#client_status_1').css('color', 'red');
        //    }
        //    else if (result[0].hrSettings) {
        //        var HRGroupID = result[0].hrSettings.hrGroupId;
        //        if (HRGroupID == 1) {
        //            $('#client_status_0').css('color', 'green');
        //            $('#client_status_1').css('color', 'red');
        //            $('#client_status_2').css('color', 'red');
        //        }
        //        else if (HRGroupID == 2) {
        //            $('#client_status_1').css('color', 'green');
        //            $('#client_status_2').css('color', 'red');
        //            $('#client_status_0').css('color', 'red');
        //        }
        //        else {
        //            $('#client_status_2').css('color', 'green');
        //            $('#client_status_0').css('color', 'red');
        //            $('#client_status_1').css('color', 'red');
        //        }
        //    }

        //}).always(function () {
        //    $('#loader').hide();
        //});
        ////To Get the Critical Documents stop
    });




    //P4#70-Disable the guard login if guard didnt login for 120 days -added by manju -start


    function checkGuardLoginExpiry() {
        var guardId = $('#GuardLogin_Guard_SecurityNo').val();
        var res = false;

        $.ajax({
            url: '/Guard/Login?handler=IsGuardLoginActive',
            type: 'GET',
            data: {
                guardLicNo: guardId
            }
        }).done(function (result) {
            if (result.success) {
                alert(result.strResult);
                res = result.success;  // Ensure 'result' is used correctly
            } else {
                alert(result.strResult);
            }
        }).fail(function () {
            alert("An error occurred while checking licenses.");
        }).always(function () {
            return res;
        });
    }



    //P4#70-Disable the guard login if guard didnt login for 120 days -added by manju -end


    $('#GuardLogin_IsPosition').on('change', function () {
        const isPosition = $('#GuardLogin_IsPosition').is(':checked');
        //if (isPosition)
        // new MessageModal({ message: "Only click <b>Position</b> if you do not have a Smart WAND - are you sure you want to continue?" }).showWarning();
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
            $('#GuardLogin_Guard_Mobile').val('+61 4');
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

    //Pr-7-Task-120 Warning-Position Checkbox-Below lines are added-Manju -start-25-04-2024

    /* let isPositionModal;
     isPositionModal = new ConfirmationModal('PositionModal', {
         message: 'Only click <b>Position</b> if you do not have a Smart WAND - are you sure you want to continue?',
         onYes: function () {
             const validateSmartWand = $('#GuardLogin_IsPosition').is(':not(:checked)') && $('#GuardLogin_SmartWandOrPosition').val() !== '';
 
             if (!validateSmartWand) {
                 submitGuardLogin();
             }
         }
     });*/


    function confirmDialog(message, onConfirm) {
        var fClose = function () {
            modal.modal("hide");
        };
        var modal = $("#confirmModal");
        modal.modal("show");
        $("#confirmMessage").empty().append(message);
        $("#confirmOk").unbind().one('click', onConfirm).one('click', fClose);
        $("#confirmCancel").unbind().one("click", fClose);
    }

    $('#btnGuardLogin').on('click', function () {




        const isPosition = $('#GuardLogin_IsPosition').is(':checked');
        if (isPosition) {
            $('#loader').show();
            // New change start bypass the message 
            if ($('#GuardLogin_SmartWandOrPosition').val() !== '') {
                $.ajax({
                    url: '/Guard/Login?handler=CheckIfSmartwandMsgBypass',
                    type: 'GET',
                    data: {
                        clientSiteName: $('#GuardLogin_ClientSiteName').val(),
                        positionName: $('#GuardLogin_SmartWandOrPosition').val(),
                        guardId: $('#GuardLogin_Guard_Id').val()
                    }
                }).done(function (result) {
                    if (result) {
                        $('#loader').show();
                        submitGuardLogin();
                    }
                    else {

                        confirmDialog('Only click <b>Position</b> if you do not have a Smart WAND - are you sure you want to continue?', function () {
                            const validateSmartWand = $('#GuardLogin_IsPosition').is(':not(:checked)') && $('#GuardLogin_SmartWandOrPosition').val() !== '';

                            if (!validateSmartWand) {
                                $('#loader').show();
                                submitGuardLogin();
                            }
                        });
                    }
                }).always(function () {
                    $('#loader').hide();
                });

            }
            else {
                const validateSmartWand = $('#GuardLogin_IsPosition').is(':not(:checked)') && $('#GuardLogin_SmartWandOrPosition').val() !== '';

                if (!validateSmartWand) {
                    submitGuardLogin();
                }

            }



        }
        else {
            const validateSmartWand = $('#GuardLogin_IsPosition').is(':not(:checked)') && $('#GuardLogin_SmartWandOrPosition').val() !== '';
            if (validateSmartWand) {
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
            else if ($('#GuardLogin_IsPosition').is(':not(:checked)') && $('#GuardLogin_SmartWandOrPosition').val() == '') {
                submitGuardLogin();
            }
        }


        /*const validateSmartWand = $('#GuardLogin_IsPosition').is(':not(:checked)') && $('#GuardLogin_SmartWandOrPosition').val() !== '';

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
        }*/
    });




    function submitGuardLogin() {
        $('#loader').show();
        calculateDutyDateTime();
        var mobileno = $('#GuardLogin_Guard_Mobile').val();
        // Task p6#73_TimeZone issue -- added by Binoy -- Start
        var form = document.getElementById('frmGuardLogin');
        var formData = new FormData(form);
        fillRefreshLocalTimeZoneDetails(formData, "GuardLogin", true);
        // Task p6#73_TimeZone issue -- added by Binoy -- End

        if (mobileno == null || mobileno == '+61 4' || mobileno == '') {
            new MessageModal({ message: " <p class=\"font-weight-bold\">The Control Room requires your personal mobile number in case of emergency. It will only be used if we cannot contact you during your shift and you have not responded to a radio check OR call to the allocated site number.</p> <p class=\"font-weight-bold\"> This request occurs only once. Please do not provide false numbers to trick system. It is an OH&S requirement we can contact you in an Emergency </p>" }).showWarning();
            console.log('after msg modal')

        }
        else {

            $('#loader').show();
            // P4#70 checking guard license no and mesaging if guard is not logged in for 120 days + disabling the active status
            var guardId = $('#GuardLogin_Guard_SecurityNo').val();

            $.ajax({
                url: '/Guard/Login?handler=IsGuardLoginActive',
                type: 'GET',
                data: {
                    guardLicNo: guardId
                }
            }).done(function (result) {
                if (result.success) {
                    //alert(result.strResult);
                    new MessageModal({
                        message: result.strResult
                    }).showWarning();
                    return;
                } else {
                    // if guard is active then submit guard login

                    $.ajax({
                        url: '/Guard/Login?handler=LoginGuard',
                        type: 'POST',
                        data: formData,
                        processData: false,
                        contentType: false,
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
            }).fail(function () {
                alert("An error occurred while checking guard license.");

            });
            //m
        }
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
        // Task p6#73_TimeZone issue -- added by Binoy - Start
        var tmdata = {
            'EventDateTimeLocal': null,
            'EventDateTimeLocalWithOffset': null,
            'EventDateTimeZone': null,
            'EventDateTimeZoneShort': null,
            'EventDateTimeUtcOffsetMinute': null,
        };

        fillRefreshLocalTimeZoneDetails(tmdata, "", false)
        $.ajax({
            url: '/Guard/DailyLog?handler=ResetClientSiteLogBook',
            type: 'POST',
            data: {
                clientSiteId: $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val(),
                guardLoginId: $('#GuardLog_GuardLoginId').val(),
                tmdata: tmdata
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
        // p6#73 timezone bug - Modified by binoy 29-01-2024
        if (record.eventDateTimeLocal != null && record.eventDateTimeLocal != '') {
            const date = new Date(record.eventDateTimeLocal);
            var DateTime = luxon.DateTime;
            var dt1 = DateTime.fromJSDate(date);
            var dt = dt1.toFormat('dd LLL yyyy @ HH:mm') + ' Hrs ' + record.eventDateTimeZoneShort;
            return dt;
        }
        else if (value !== '') {
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

            var googleMap = record.gpsCoordinates ? '<a href="https://www.google.com/maps?q=' + record.gpsCoordinates + '" target="_blank" data-toggle="tooltip" title=""><i class="fa fa-map-marker" aria-hidden="true"></i></a>' : '';
            return value + '<a href="#" class="ml-2"><i class="fa fa-vcard-o text-info" data-toggle="modal" data-target="#guardInfoModal" data-id="' + record.guardId + '"></i></a>' + googleMap;
        }
        else return 'Admin'
        /*else return value;*/
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
        if (record.eventDateTimeLocal != null && record.eventDateTimeLocal != '') {
            const date = new Date(record.eventDateTimeLocal);
            var DateTime = luxon.DateTime;
            var dt1 = DateTime.fromJSDate(date);
            var dt = dt1.toFormat('HH:mm') + ' Hrs ' + record.eventDateTimeZoneShort;
            return dt;
        }
        else if (value !== '') {
            const date = new Date(value);
            return date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
        }
    }

    // Project 4 , Task 48, Audio notification, Added By Binoy -- Start
    let audiourl = '/NotificationSound/mixkit-bell-notification-933.wav'
    const audio = new Audio(audiourl);
    let audioplayedlist = [];
    let isControlRoomLogBook = $('#inpHdisControlRoomLogBook').val() == 'false' ? false : true;
    // Project 4 , Task 48, Audio notification, Added By Binoy -- End

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
            {
                field: 'guardInitials', title: 'Guard Initials', width: 80, renderer: function (value, record) {
                    var rtn = '';
                    if (!record.guardLogin.guard.initial) {
                        // str is null, undefined, or an empty string
                        if (record.rcLogbookStamp === true)
                            rtn = 'Admin';
                    }
                    else {
                        rtn = record.guardLogin.guard.initial;
                    }

                    return rtn;
                }
            }
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
                { field: 'eventDateTime', title: 'Time', width: 100, renderer: function (value, record) { return renderTime(value, record, false); } },
                {
                    field: 'notes', title: 'Event / Notes', width: 350, editor: logBookNotesEditor, renderer: renderLogBookNotes,
                    renderer: function (value, record) {
                        if (record.isIRReportTypeEntry == true) {

                            return record.notes + '<a href="https://c4istorage1.blob.core.windows.net/irfiles/' + record.notes.substr(0, 8) + '/' + record.notes + '.pdf" target="_blank">          Click here</a>';
                        }
                        else {
                            return record.notes;
                        }
                    }
                },
                {
                    field: 'guardInitials', title: 'Guard Initials', width: 70, renderer: function (value, record) {
                        //return record.guardLogin ? record.guardLogin.guard.initial : '';
                        var rtn = '';
                        rtn = `${record.guardLogin ? record.guardLogin.guard.initial : ''}&nbsp;&nbsp;${record.gpsCoordinates ? `<a href="https://www.google.com/maps?q=${record.gpsCoordinates}" target="_blank" data-toggle="tooltip" title=""><i class="fa fa-map-marker" aria-hidden="true"></i></a>` : ''}`;
                        /*var rtn = record.guardLogin.guard.initial ? record.guardLogin.guard.initial : record.rcLogbookStamp ? 'Admin' : '';*/
                        if (!rtn || rtn == '&nbsp;&nbsp;') {
                            rtn = record.rcLogbookStamp ? 'Admin' : '';
                        }
                        return rtn;
                    }
                },
                { width: 75, renderer: renderDailyLogManagement, title: '<i class="fa fa-cogs" aria-hidden="true"></i>' },
                { field: 'rcPushMessageId', hidden: true },
                { field: 'playNotificationSound', hidden: true, renderer: playNotificationSound }
            ]
        };
    }

    function renderLogBookNotes(value, $cell) {
        $cell.on('keydown', function (e) {
            /*timer pause while editing*/
            isPaused = true;
            if (e.which === 13) {
                gridGuardLog.update(record.id);
            }
        });
        if (record.isRearfile == true || record.isTwentyfivePercentfile == true) {
            return record.notesNew;
        }
        return record.notes;
    }

    // Project 4 , Task 48, Audio notification, Added By Binoy -- Start
    function playNotificationSound(value, record) {
        if ((record.rcPushMessageId != null) && (record.rcPushMessageId != '') && (record.rcPushMessageId > 0)
            && (value == true) && (!isControlRoomLogBook)) {
            audioplayedlist.push(record.id);
        }
        return;
    }
    // Project 4 , Task 48, Audio notification, Added By Binoy -- End

    function logBookNotesEditor($editorContainer, value, record) {
        if (record.notesNew != '') {
            record.notes = record.notes.replace(record.notesNew, '')
        }
        var yes = $('#IsAssignedLogBook').val();
        if (yes == 0) {
            var textAreaForNotes = $('<textarea class="form-control" rows="4" maxlength="2048" onpaste="return false" >' + record.notes + '</textarea > ');
            $editorContainer.append(textAreaForNotes);
        }
        else {
            var textAreaForNotes = $('<textarea class="form-control" rows="4" maxlength="2048"  >' + record.notes + '</textarea > ');
            $editorContainer.append(textAreaForNotes);
        }

    }

    function renderDailyLogManagement(value, record, $cell, $displayEl, id) {
        //if (record.isSystemEntry || record.guardLogin.guardId != $('#GuardLog_GuardLogin_GuardId').val())
        //    return;
        if (record.isSystemEntry)
            return;
        /*to get the control room notification-start*/
        var $messageBtn = $('<button class="btn btn-outline-primary mt-2"  data-id="' + record.id + '"><i class="fa fa-envelope"></i></button>');
        $messageBtn.on('click', function (e) {
            var id = $(this).data('id');
            var message = $(this).closest('tr').find('td').eq(2).text();
            var rcPushMessageId = $(this).closest('tr').find('td').eq(5).text();
            $('#txtNotificationsControlRoomMessage').val(message);
            $('#rcPushMessageId').val(rcPushMessageId);
            $('#pushNoTificationsGuardModal').modal('show');
        });

        const irEntryTypeIsAlarm = 2;
        if (record.guardLoginId === null || record.guardLogin.guardId != $('#GuardLog_GuardLogin_GuardId').val()) {
            if (record.irEntryType === irEntryTypeIsAlarm) {
                $displayEl.append($messageBtn);
                return;
            }
            if (record.irEntryType === 1) {
                return;
            }
            return;
        }
        else {
            if (record.irEntryType === 1)
                return;
            if (record.irEntryType === irEntryTypeIsAlarm) {
                $displayEl.append($messageBtn);
                return;
            }
        }



        /*to get the control room notification - end*/

        var $editBtn = $('<button class="btn btn-outline-primary mr-1 p-1" data-id="' + record.id + '"><i class="fa fa-pencil mr-2"></i>Edit</button>'),
            //p6-102 Add Photo -start

            $imageBtn = $('<button class="btn btn-outline-primary p-1" data-id="' + record.id + '"><i class="fa fa-camera mr-1"></i><i class="fa fa-plus"></i></button>'),

            //p6-102 Add Photo -end
            $deleteBtn = $('<button class="btn btn-outline-danger mt-2" data-id="' + record.id + '"><i class="fa fa-trash mr-2"></i>Delete</button>'),
            $updateBtn = $('<button class="btn btn-outline-success mr-2 mt-2" data-id="' + record.id + '"><i class="fa fa-check-circle mr-2"></i>Update</button>').hide(),
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

        //p6-102 Add Photo -start
        $imageBtn.on('click', function (e) {
            $('#dgl-image-modal').modal('show');
            $('#chbIsTwentyfivePercentOfPage').prop('checked', true);
            $('#GuardimagedataId').val($(this).data('id'));
            $('#chbIsAttachmentToRear').prop('checked', false);
            loadDlgImagePopup($(this).data('id'), false);
        });
        //p6-102 Add Photo -end
        $displayEl.append($editBtn)
            .append($imageBtn)
            .append($deleteBtn)
            .append($updateBtn)
            .append($cancelBtn);


    }
    //p6-102 Add Photo -start
    function loadDlgImagePopup(id, isNewEntry) {
        $.ajax({
            url: '/Guard/DailyLog?handler=GuardLogsDocumentImages&id=' + id,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            $("#dgl-attachment-list").empty();

            for (var attachIndex = 0; attachIndex < data.length; attachIndex++) {
                const file = data[attachIndex].imagePath;
                const attachment_id = data[attachIndex].id;
                const li = document.createElement('li');
                li.id = attachment_id;
                li.className = 'list-group-item';
                li.dataset.index = attachIndex;
                let liText = document.createTextNode(data[attachIndex].imageFile);
                const icon = document.createElement("i");
                icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-dgl-attachment';
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
                document.getElementById('dgl-attachment-list').append(li);
            }



        }).fail(function () {
            //  showStatusNotification(false, 'Something went wrong');
            //$('#loader').hide();
        }).always(function () {

        });

        //$("#kvl-attachment-list").empty();
        //for (var attachIndex = 0; attachIndex < result.attachments.length; attachIndex++) {
        //    const file = result.attachments[attachIndex];
        //    const attachment_id = 'attach_' + attachIndex;
        //    const li = document.createElement('li');
        //    li.id = attachment_id;
        //    li.className = 'list-group-item';
        //    li.dataset.index = attachIndex;
        //    let liText = document.createTextNode(file);
        //    const icon = document.createElement("i");
        //    icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-kvl-attachment';
        //    icon.title = 'Delete';
        //    icon.style = 'cursor:pointer';
        //    li.appendChild(liText);
        //    li.appendChild(icon);
        //    const anchorTag = document.createElement("a");
        //    anchorTag.href = '/KvlUploads/' + $('#VehicleRego').val() + "/" + file;
        //    anchorTag.target = "_blank";
        //    const icon2 = document.createElement("i");
        //    icon2.className = 'fa fa-download ml-2 text-primary';
        //    icon2.title = 'Download';
        //    icon2.style = 'cursor:pointer';
        //    anchorTag.appendChild(icon2);
        //    li.appendChild(anchorTag);
        //    document.getElementById('kvl-attachment-list').append(li);



        //}

    }
    $('#dgl-attachment-list').on('click', '.btn-delete-dgl-attachment', function (event) {
        if (confirm('Are you sure want to remove this attachment?')) {
            var target = event.target;
            const fileName = target.parentNode.innerText.trim();
            const id = target.parentNode.id;
            const vehicleRego = $('#VehicleRego').val()
            $.ajax({
                url: '/Guard/DailyLog?handler=DeleteAttachment',
                type: 'POST',
                dataType: 'json',
                data: {
                    id: id,

                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result) {
                    target.parentNode.parentNode.removeChild(target.parentNode);
                    gridGuardLog.clear();
                    gridGuardLog.reload();
                }
            });
        }
    });

    $('#chbIsAttachmentToRear').on('change', function () {
        const isChecked = $(this).is(':checked');
        if (isChecked == true) {

            $('#IsTwentyfivePercentOfPage').val(false);
            $('#chbIsTwentyfivePercentOfPage').prop('checked', false)


        }

        $('#IsAttachmentToRear').val(isChecked);

    });
    $('#chbIsTwentyfivePercentOfPage').on('change', function () {
        const isChecked = $(this).is(':checked');
        if (isChecked == true) {

            $('#IsAttachmentToRear').val(false);
            $('#chbIsAttachmentToRear').prop('checked', false)


        }
        $('#IsTwentyfivePercentOfPage').val(isChecked);

    });
    $('#IsTwentyfivePercentOfPage').val(true);
    $('#btnIsRearOrTwentyfivePercent').on('click', function () {


        $('#IsRearOrTwentyfivePercentfileInput').click();




    });
    $('#btnIsRearOrTwentyfivePercentSaveAndClose').on('click', function () {


        $('#dgl-image-modal').modal('hide');




    });
    const showModal = function (message) {

        $('#msg-modal .modal-body p').html(message);
        $('#msg-modal').modal();
    }


    $('#IsRearOrTwentyfivePercentfileInput').on('change', function (e) {
        const files = this.files;
        const logId = $('#GuardimagedataId').val();
        const url = window.location.origin;

        const supportedExtensions = ['jpg', 'jpeg', 'bmp', 'gif', 'heic', 'png', 'JPG', 'JPEG', 'BMP', 'GIF', 'HEIC', 'PNG'];

        function uploadFile(file, uploadUrl, isRearFile) {
            const fileForm = new FormData();
            const fileExtn = file.name.split('.').pop();

            if (!supportedExtensions.includes(fileExtn)) {
                showModal('Unsupported file type. Please upload a .jpg, .jpeg, .bmp, .gif, .heic, .png file');
                return;
            }

            fileForm.append('file', file);
            fileForm.append('logId', logId);
            fileForm.append('url', url);

            $('#loader').show();
            $('#loadinDiv').show();

            $.ajax({
                url: uploadUrl,
                type: 'POST',
                data: fileForm,
                processData: false,
                contentType: false,
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
            }).done(function (data) {
                if (data.success) {
                    if (isRearFile) {
                        $('#IsAttachmentToRear').val(true);
                        $('#IsTwentyfivePercentOfPage').val(false);
                    } else {
                        $('#IsAttachmentToRear').val(false);
                        $('#IsTwentyfivePercentOfPage').val(true);
                    }

                    gridGuardLog.clear();
                    gridGuardLog.reload();
                    loadDlgImagePopup(logId, false);
                }
            }).fail(function () {
                showStatusNotification(false, 'Something went wrong');
            }).always(function () {
                $('#loader').hide();
                $('#loadinDiv').hide();
            });
        }

        if ($('#IsAttachmentToRear').val() === 'true') {
            for (let i = 0; i < files.length; i++) {
                uploadFile(files[i], '/Guard/DailyLog?handler=RearFileUpload', true);
            }
        } else if ($('#IsTwentyfivePercentOfPage').val() === 'true') {
            for (let i = 0; i < files.length; i++) {
                uploadFile(files[i], '/Guard/DailyLog?handler=TwentyfivePercentFileUpload', false);
            }
        }
    });


    //p6-102 Add Photo -end
    /*to display the popup to acknowledge the message-start*/
    $('#guard_daily_log tbody').on('click', '#btnAcknowledgeButton', function (value, record) {
        /*timer pause while editing*/
        isPaused = true;
        var id = $(this).data('id');
        var message = $(this).closest('tr').find('td').eq(2).val();
    });
    /*to display the popup to acknowledge the message-end*/
    gridGuardLog = $('#guard_daily_log').grid(gridGuardLogSettings);

    if (gridGuardLog) {

        const bg_color_pale_yellow = '#fcf8d1';
        const bg_color_pale_red = '#ffcccc';
        const irEntryTypeIsAlarm = 2;
        const noguardonduty_eventType = 1;

        gridGuardLog.on('rowDataBound', function (e, $row, id, record) {
            if (record.irEntryType) {
                if (record.irEntryType === irEntryTypeIsAlarm || record.rcLogbookStamp) {
                    $row.css('background-color', bg_color_pale_red);
                } else {
                    $row.css('background-color', bg_color_pale_yellow);
                }
                /* add for check if dark mode is on start*/
                if ($('#toggleDarkMode').is(':checked')) {
                    $row.css('color', '#333');
                    //$row.css('background-color', record.irEntryType === irEntryTypeIsAlarm ? bg_color_pale_red : bg_color_pale_yellow);
                }
                /* add for check if dark mode is on end*/
            } else if (record.eventType === noguardonduty_eventType) {
                $row.css('background-color', bg_color_pale_red);
                if ($('#toggleDarkMode').is(':checked')) {
                    $row.css('color', '#333');
                    //$row.css('background-color', record.irEntryType === irEntryTypeIsAlarm ? bg_color_pale_red : bg_color_pale_yellow);
                }
            }
        });
        gridGuardLog.on('rowSelect', function (e, $row, id, record) {
            /*timer pause while editing*/
            if (isPaused == true) {
                isPaused = false;

            }
            else {
                isPaused = true;

            }


        });
        gridGuardLog.on('rowUnselect', function (e, $row, id, record) {
            /*timer pause while editing*/
            isPaused = false;


        });
        gridGuardLog.on('rowDataChanged', function (e, id, record) {
            /*timer pause while editing*/
            isPaused = true;
            const data = $.extend(true, {}, record);
            fillRefreshLocalTimeZoneDetails(data, "", false);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $('#loader').show();
            $.ajax({
                url: '/Guard/DailyLog?handler=SaveGuardLog',
                data: { guardlog: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {
                if (result.success) {
                    /*timer pause while editing*/
                    isPaused = false;
                    gridGuardLog.clear();
                    gridGuardLog.reload();
                }
                else {
                    /*timer pause while editing*/
                    isPaused = false;
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
            /*timer pause while editing*/
            isPaused = true;
            if (confirm('Are you sure want to delete this entry?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Guard/DailyLog?handler=DeleteGuardLog',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    /*timer pause while editing*/
                    isPaused = false;
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

        // Project 4 , Task 48, Audio notification, Added By Binoy -- Start
        gridGuardLog.on('dataBound', function (e, records, totalRecords) {
            if (isControlRoomLogBook) {
                // Get Is acknowledge from db
                $.ajax({
                    url: '/Guard/DailyLog?handler=GuardLogsAcknowledgedForControlroom',
                    type: 'GET',
                }).done(function (result) {
                    if (result != null && result.length > 0) {
                        //Can be repeated if needed using loop through id in result
                        audio.play();
                        audioplayedlist = [];
                        result.forEach((item) => {
                            audioplayedlist.push(item);
                        });
                        UpdatePlayedNotification();
                    }
                });
            }
            else if (audioplayedlist.length > 0) {
                // Play notification sound
                audio.play();
                UpdatePlayedNotification();
            }
        });
        // Project 4 , Task 48, Audio notification, Added By Binoy -- End
    }

    // Project 4 , Task 48, Audio notification, Added By Binoy -- Start
    function UpdatePlayedNotification() {
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Guard/DailyLog?handler=GuardLogsUpdateNotificationSoundStatus',
            data: { logBookId: audioplayedlist, isControlRoomLogBook: isControlRoomLogBook },
            dataType: 'json',
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function (result) {
            audioplayedlist = [];
            return;
        });
    }
    // Project 4 , Task 48, Audio notification, Added By Binoy -- End

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
            { field: 'eventDateTime', title: 'Time', width: 100, renderer: function (value, record) { return renderTime(value, record, false); } },
            { field: 'notes', title: 'Event / Notes', width: 400, editor: logBookNotesEditor, renderer: renderLogBookNotes },
            { field: 'guardInitials', title: 'Guard Initials', width: 50, renderer: function (value, record) { return record.guardLogin ? record.guardLogin.guard.initial : ''; } }
        ],
        initialized: function (e) {
            $('#wrapper_previous_day_log').hide();
            if ($('#previousLogBookId').val() !== '') {
                $('#wrapper_previous_day_log').show();
            }
        }
    });

    jQuery("#GuardLog_Notes").blur(function () {
        /*timer pause while editing*/
        isPaused = false;
    });
    $('#GuardLog_Notes').click(function (e) {
        /*timer pause while editing*/
        isPaused = true;
    });
    $('#GuardLog_Notes').on('keydown', function (e) {
        /*timer pause while editing*/
        isPaused = true;
        if (e.which === 13) {
            addGuardLog();
        }
    });

    $('#add_new_log').on('click', function () {
        /*timer pause while editing*/
        isPaused = false;
        addGuardLog();
    });

    function addGuardLog() {
        const today = new Date();
        $('#GuardLog_EventDateTime').val(today.getDate() + '-' + (today.getMonth() + 1) + '-' + today.getFullYear() + ' ' + $('#new_log_time').val());
        $('#GuardLog_TimePartOnly').val($('#new_log_time').val());
        $('#loader').show();
        $('#validation-summary ul').html('');
        // Task p6#73_TimeZone issue -- added by Binoy -- Start
        var form = document.getElementById('form_newlog');
        var formData = new FormData(form);
        fillRefreshLocalTimeZoneDetails(formData, "GuardLog", true);
        // Task p6#73_TimeZone issue -- added by Binoy -- End
        $.ajax({
            url: '/Guard/DailyLog?handler=SaveGuardLog',
            data: formData,
            processData: false,
            contentType: false,
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
                data: { guardLoginId: $('#GuardLog_GuardLoginId').val(), clientSiteLogBookId: $('#GuardLog_ClientSiteLogBookId').val(), tmdata: tmdata },
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

    //function displayCustomFieldsValidationSummary(errors) {
    //    const summaryDiv = document.getElementById('custom-field-validation');
    //    summaryDiv.className = "validation-summary-errors";
    //    summaryDiv.querySelector('ul').innerHTML = '';
    //    errors.forEach(function (item) {
    //        const li = document.createElement('li');
    //        li.appendChild(document.createTextNode(item));
    //        summaryDiv.querySelector('ul').appendChild(li);
    //    });
    //}

    //*************** Admin Site Log  *************** //

    //Daily Log
    const today = new Date();
    const start = new Date(today.getFullYear(), today.getMonth(), 2);
    $('#dglAudtitFromDate').val(start.toISOString().substr(0, 10));
    var systemDate = $('#dglAudtitToDateVal').val();
    //var dateObject = new Date().toISOString().substr(0, 10);
    $('#dglAudtitToDate').val(systemDate);

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
            {
                field: 'notes', title: 'Event / Notes', width: 440,
                renderer: function (value, record) {
                    if (record.isIRReportTypeEntry == true) {

                        return record.notes + '<a href="https://c4istorage1.blob.core.windows.net/irfiles/' + record.notes.substr(0, 8) + '/' + record.notes + '.pdf" target="_blank">          Click here</a>';
                    }
                    else {
                        return record.notes;
                    }
                }
            },
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
        const noguardonduty_eventType = 1;

        gridsiteLog.on('rowDataBound', function (e, $row, id, record) {
            let rowColor = bg_color_white;
            if (record.irEntryType) {
                rowColor = record.irEntryType === irEntryTypeIsAlarm ? bg_color_pale_red : bg_color_pale_yellow;
            } else if (record.eventType === noguardonduty_eventType) {
                rowColor = bg_color_pale_red;
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

    //$('#duress_btn').on('click', function () {
    //    $.ajax({
    //        url: '/Guard/DailyLog?handler=SaveClientSiteDuress',
    //        data: {
    //            clientSiteId: $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val(),
    //            guardLoginId: $('#GuardLog_GuardLoginId').val(),
    //            logBookId: $('#GuardLog_ClientSiteLogBookId').val(),
    //            guardId: $('#GuardLog_GuardLogin_GuardId').val(),
    //        },
    //        type: 'POST',
    //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //    }).done(function (result) {
    //        if (result.status) {
    //            $('#duress_btn').removeClass('normal').addClass('active');
    //            $("#duress_status").addClass('font-weight-bold');
    //            $("#duress_status").text("Active");
    //        }
    //        gridGuardLog.clear();
    //        gridGuardLog.reload();
    //    });
    //});

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
        $('#vklSitePOC').val('');

        $('#vklSiteLoc').val('');
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
    //$('#vklAudtitToDate').val(new Date().toISOString().substr(0, 10));
    $('#vklAudtitToDate').val(systemDate);


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
    //for downloading keyvehicle logs-start
    let keyVehicleLogReportnew = $('#vkl_site_lognew').DataTable({
        lengthMenu: [[75, 100, -1], [75, 100, "All"]],
        pageLength: 100,
        hidden: true,
        paging: false,
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
    //for downloading keyvehicle logs-end
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

    $('#vklClientSiteIdTimesheet').multiselect({
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



        const clientSitePocControl = $('#vklSitePOC');
        const clientSiteLocControl = $('#vklSiteLoc');



        $.ajax({
            url: '/Admin/AuditSiteLog?handler=ClientSiteLocationsAndPocs&clientSiteIds=' + $(this).val().join(';'),
            type: 'GET',
            datatype: 'json',
        }).done(function (data) {
            clientSitePocControl.html('');
            clientSiteLocControl.html('');
            data.sitePocs.map(function (result) {

                clientSitePocControl.append('<option value="' + result.value + '">' + result.text + '</option>');
            });
            data.siteLocations.map(function (result) {
                clientSiteLocControl.append('<option value="' + result.value + '">' + result.text + '</option>');
            });
            clientSitePocControl.multiselect('rebuild');
            clientSiteLocControl.multiselect('rebuild');
        });
    });

    $('#vklClientType').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });
   
    // for selecting more than one POI
    $('#vklPersonOfInterest').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });
    // for selecting more than one Site Poc-start
    $('#vklSitePOC').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });
    // for selecting more than one Site Poc-end
    // for selecting more than one Site Loc-start
    $('#vklSiteLoc').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });
    // for selecting more than one Site Loc-end
    $('#vklClientTypeTimesheet').on('change', function () {
        //gridsitefusionLog.clear();
        const clientTypeId = $(this).val();
        const clientSiteControl = $('#vklClientSiteIdTimesheet');
        clientSiteControl.html('');
        $.ajax({
            url: '/Admin/Settings?handler=ClientSites&typeId=' + clientTypeId,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
                });
                clientSiteControl.multiselect('rebuild');
            }

        });


    });
    $('#vklClientSiteIdTimesheet').on('change', function () {
        var selectedOptions = $(this).val();  // Get selected values
        $('#startDateTimesheetBulk').val('');
        $('#endDateTimesheetBulk').val('');
        $('#frequency').val('');
        if (selectedOptions && selectedOptions.length > 0) {
            var selectedSitesList = $('#selectedSitesList');
            selectedSitesList.empty();  // Clear previous list

            // Add selected options to modal list
            selectedOptions.forEach(function (siteId) {
                var selectedText = $('#vklClientSiteIdTimesheet option[value="' + siteId + '"]').text();
                selectedSitesList.append('<li>' + selectedText + '</li>');
            });

            // Show the modal
            $('#timesheetBulkModal').modal('show');
        }
    });
    $('#vklPersonOfInterest').on('change', function () {
        const personOfInterestId = $(this).val();
        $("#vklPersonOfInterest").val(personOfInterestId);
        $("#vklPersonOfInterest").multiselect("refresh");




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
        //calculate month difference-start
        var date1 = new Date($('#vklAudtitFromDate').val());
        var date2 = new Date($('#vklAudtitToDate').val());
        var monthdiff = monthDiff(date1, date2);
        if (monthdiff > 6) {
            alert('Date Range is  greater than 6 months');
            return false;
        }

        //calculate month difference-end
        $('#KeyVehicleLogAuditLogRequest_ClientSiteId').val($('#vklClientSiteId').val());
        $('#KeyVehicleLogAuditLogRequest_LogFromDate').val($('#vklAudtitFromDate').val());
        $('#KeyVehicleLogAuditLogRequest_LogToDate').val($('#vklAudtitToDate').val());
        $('#KeyVehicleLogAuditLogRequest_LogBookType').val(2);

        $('#KeyVehicleLogAuditLogRequest_PersonOfInterest').val($('#vklPersonOfInterest').val());
        $('#KeyVehicleLogAuditLogRequest_ClientSitePocIdNew').val($('#vklSitePOC').val());
        $('#KeyVehicleLogAuditLogRequest_ClientSiteLocationIdNew').val($('#vklSiteLoc').val());
        $('#loader').show();
        $.ajax({
            url: '/Admin/AuditSiteLog?handler=KeyVehicleSiteLogs',
            type: 'POST',
            dataType: 'json',
            data: $('#form_kvl_auditlog_request').serialize(),
        }).done(function (response) {
            $('#loader').hide();
            keyVehicleLogReport.clear().rows.add(response).draw();
        });
    });
    //code addded  to download Excel start for auditsite key vehicle-start
    $("#btnDownloadVklAuditExcel").click(function () {
        if ($('#vklClientSiteId').val().length === 0) {
            alert('Please select a client site');
            return;
        }
        //calculate month difference-start
        var date1 = new Date($('#vklAudtitFromDate').val());
        var date2 = new Date($('#vklAudtitToDate').val());
        var monthdiff = monthDiff(date1, date2);
        if (monthdiff > 6) {
            alert('Date Range is  greater than 6 months');
            return false;
        }
        //calculate month difference-end
        $('#KeyVehicleLogAuditLogRequest_ClientSiteId').val($('#vklClientSiteId').val());
        $('#KeyVehicleLogAuditLogRequest_LogFromDate').val($('#vklAudtitFromDate').val());
        $('#KeyVehicleLogAuditLogRequest_LogToDate').val($('#vklAudtitToDate').val());
        $('#KeyVehicleLogAuditLogRequest_LogBookType').val(2);

        $('#KeyVehicleLogAuditLogRequest_PersonOfInterest').val($('#vklPersonOfInterest').val());
        $('#KeyVehicleLogAuditLogRequest_ClientSitePocIdNew').val($('#vklSitePOC').val());
        $('#KeyVehicleLogAuditLogRequest_ClientSiteLocationIdNew').val($('#vklSiteLoc').val());
        $('#loader').show();
        $.ajax({
            url: '/Admin/AuditSiteLog?handler=KeyVehicleSiteLogs',
            type: 'POST',
            dataType: 'json',
            data: $('#form_kvl_auditlog_request').serialize(),
        }).done(function (response) {
            $('#loader').hide();
            keyVehicleLogReportnew.clear().rows.add(response).draw();
            /*p1-218 search download select-start*/
            var searchtext = keyVehicleLogReport.search();
            keyVehicleLogReportnew.search(searchtext).draw();
            /*p1-218 search download select-end*/
            var Key = 'Key & Vehicle Logs - ' + $('#vklAudtitFromDate').val() + ' to ' + $('#vklAudtitToDate').val();

            var type = 'xlsx';
            var name = Key + '.';

            var data = document.getElementById('vkl_site_lognew');


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
                //var rows = tableClone.getElementsByTagName('tr');
                //for (var i = 0; i < rows.length; i++) {
                //    var lastCell = rows[i].lastElementChild;
                //    if (lastCell) {
                //        rows[i].removeChild(lastCell);
                //    }
                //}
            }




            var excelFile = XLSX.utils.table_to_book(tableClone, { sheet: "KeyVehicleLogs" });

            // Use XLSX.writeFile to generate and download the Excel file
            XLSX.writeFile(excelFile, name + type);
        });

    });
    //code addded  to download Excel start for auditsite key vehicle-end


    //$('#btnDisableDataCollection').on('click', function () {
    //    $.ajax({
    //        url: '/Admin/GuardSettings?handler=UpdateSiteDataCollection',
    //        type: 'POST',
    //        data: {
    //            clientSiteId: $('#gl_client_site_id').val(),
    //            disabled: $('#cbxDisableDataCollection').is(":checked")
    //        },
    //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
    //    }).done(function () {
    //        alert("Saved successfully");
    //    });
    //});

    //code addded  to download Excel start

    //$("#add_Downloadbtn").click(function () {

    //    var Key = $('#site-settings-for').html();

    //    var type = 'xlsx';
    //    var name = Key + '.';
    //    var data = document.getElementById('cs_client_site_keys');

    //    // Check if all columns are empty
    //    var isEmptyTable = true;
    //    var rows = data.getElementsByTagName('tr');
    //    for (var i = 0; i < rows.length; i++) {
    //        var cells = rows[i].getElementsByTagName('td');
    //        for (var j = 1; j < cells.length; j++) {
    //            if (cells[j].textContent.trim() !== '') {
    //                isEmptyTable = false;
    //                break;
    //            }
    //        }
    //    }

    //    if (isEmptyTable) {
    //        // Create a message row with the desired text
    //        var messageRow = document.createElement('tr');
    //        var messageCell = document.createElement('td');
    //        messageCell.innerText = 'No data available in table';
    //        messageRow.appendChild(messageCell);

    //        // Create a new table with the message
    //        var tableClone = document.createElement('table');
    //        var tbody = document.createElement('tbody');
    //        tbody.appendChild(messageRow);
    //        tableClone.appendChild(tbody);
    //    } else {
    //        // Clone the table and remove the last column
    //        var tableClone = data.cloneNode(true);
    //        var rows = tableClone.getElementsByTagName('tr');
    //        for (var i = 0; i < rows.length; i++) {
    //            var lastCell = rows[i].lastElementChild;
    //            if (lastCell) {
    //                rows[i].removeChild(lastCell);
    //            }
    //        }
    //    }




    //    var excelFile = XLSX.utils.table_to_book(tableClone, { sheet: "Keys" });

    //    // Use XLSX.writeFile to generate and download the Excel file
    //    XLSX.writeFile(excelFile, name + type);
    //});
    //code addded  to download Excel end

    //$('#ClientSiteCustomField_Name').editableSelect({
    //    effects: 'slide'
    //});

    //$('#ClientSiteCustomField_TimeSlot').editableSelect({
    //    effects: 'slide'
    //});

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
        //$('#ClientSiteCustomField_ClientSiteId').val(siteId);
        //$('#gs_site_email').val(siteEmail);
        //$('#gs_duress_email').val(duressEmail);
        //$('#gs_duress_sms').val(duressSms);
        //$('#gs_land_line').val(landLine);
        //$('#gs_email_recipients').val(guardLogEmailTo);
        //$('#enableLogDump').prop('checked', false);
        $('#cbxDisableDataCollection').prop('checked', !isDataCollectionEnabled);
        //if (isUpdateDailyLog)
        //    $('#enableLogDump').prop('checked', true);
        gritdSmartWands.reload({ clientSiteId: $('#gl_client_site_id').val() });
        gridSitePatrolCars.reload({ clientSiteId: $('#gl_client_site_id').val() });
        loadCustomFields();
        gridSiteCustomFields.reload({ clientSiteId: $('#gl_client_site_id').val() });
        gridSiteLocations.reload({ clientSiteId: $('#gl_client_site_id').val() });
        gridSitePocs.reload({ clientSiteId: $('#gl_client_site_id').val() });
        gridClientSiteKeys.ajax.reload();
        /*for manifest options-start*/
        GetClientSiteToggle();
        /*for manifest options - end*/
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

    //let gridSitePatrolCars;
    //gridSitePatrolCars = $('#cs-patrol-cars').grid({
    //    dataSource: '/Admin/GuardSettings?handler=PatrolCar',
    //    uiLibrary: 'bootstrap4',
    //    iconsLibrary: 'fontawesome',
    //    primaryKey: 'id',
    //    inlineEditing: { mode: 'command' },
    //    columns: [
    //        { width: 250, field: 'model', title: 'Model', editor: true },
    //        { width: 250, field: 'rego', title: 'Rego', editor: true },
    //        { width: 250, field: 'id', title: 'Id', hidden: true }
    //    ],
    //    initialized: function (e) {
    //        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    //    }
    //});

    //if (gridSitePatrolCars) {
    //    gridSitePatrolCars.on('rowDataChanged', function (e, id, record) {
    //        const data = $.extend(true, {}, record);
    //        const token = $('input[name="__RequestVerificationToken"]').val();
    //        $.ajax({
    //            url: '/Admin/GuardSettings?handler=PatrolCar',
    //            data: { record: data },
    //            type: 'POST',
    //            headers: { 'RequestVerificationToken': token },
    //        }).done(function () {
    //            gridSitePatrolCars.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //        }).fail(function () {
    //            console.log('error');
    //        }).always(function () {
    //            if (isPatrolCarAdding)
    //                isPatrolCarAdding = false;
    //        });
    //    });

    //    gridSitePatrolCars.on('rowRemoving', function (e, id, record) {
    //        if (confirm('Are you sure want to delete this patrol car details?')) {
    //            const token = $('input[name="__RequestVerificationToken"]').val();
    //            $.ajax({
    //                url: '/Admin/GuardSettings?handler=DeletePatrolCar',
    //                data: { id: record },
    //                type: 'POST',
    //                headers: { 'RequestVerificationToken': token },
    //            }).done(function () {
    //                gridSitePatrolCars.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //            }).fail(function () {
    //                console.log('error');
    //            }).always(function () {
    //                if (isPatrolCarAdding)
    //                    isPatrolCarAdding = false;
    //            });
    //        }
    //    });
    //}

    //let isPatrolCarAdding = false;
    //$('#add_patrol_car').on('click', function () {

    //    if (isPatrolCarAdding) {
    //        alert('Unsaved changes in the grid. Refresh the page');
    //    } else {
    //        isPatrolCarAdding = true;
    //        gridSitePatrolCars.addRow({ 'id': -1, 'model': '', rego: '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
    //    }
    //});


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
    /*  code added to 1000 pages in Excel */
    //let gridClientSiteKeys = $('#cs_client_site_keys').DataTable({
    //    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    //    paging: true,
    //    ordering: true,
    //    order: [[1, "asc"]],
    //    info: false,
    //    searching: true,
    //    autoWidth: false,
    //    ajax: {
    //        url: '/Admin/GuardSettings?handler=ClientSiteKeys',
    //        data: function (d) {
    //            d.clientSiteId = $('#ClientSiteKey_ClientSiteId').val();
    //        },
    //        dataSrc: ''
    //    },
    //    columns: [
    //        { data: 'id', visible: false },
    //        { data: 'keyNo', width: '4%' },
    //        { data: 'description', width: '12%', orderable: false },
    //        {
    //            targets: -1,
    //            orderable: false,
    //            width: '4%',
    //            data: null,
    //            defaultContent: '<button  class="btn btn-outline-primary mr-2" id="btn_edit_cs_key"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
    //                '<button id="btn_delete_cs_key" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i>Delete</button>',
    //            className: "text-center"
    //        },
    //    ],
    //});

    //$('#cs_client_site_keys tbody').on('click', '#btn_delete_cs_key', function () {
    //    var data = gridClientSiteKeys.row($(this).parents('tr')).data();
    //    if (confirm('Are you sure want to delete this key?')) {
    //        $.ajax({
    //            type: 'POST',
    //            url: '/Admin/GuardSettings?handler=DeleteClientSiteKey',
    //            data: { 'id': data.id },
    //            dataType: 'json',
    //            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //        }).done(function () {
    //            gridClientSiteKeys.ajax.reload();
    //        });
    //    }
    //});

    //$('#cs_client_site_keys tbody').on('click', '#btn_edit_cs_key', function () {
    //    var data = gridClientSiteKeys.row($(this).parents('tr')).data();
    //    loadClientSiteKeyModal(data);
    //});

    //$('#add_client_site_key').on('click', function () {
    //    resetClientSiteKeyModal();
    //    $('#client-site-key-modal').modal('show');
    //});

    //$('#btn_save_cs_key').on('click', function () {
    //    $.ajax({
    //        url: '/Admin/GuardSettings?handler=ClientSiteKey',
    //        data: $('#frm_add_key').serialize(),
    //        type: 'POST',
    //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //    }).done(function (result) {
    //        if (result.success) {
    //            $('#client-site-key-modal').modal('hide');
    //            gridClientSiteKeys.ajax.reload();
    //        } else {
    //            displaySiteKeyValidationSummary(result.message);
    //        }
    //    });
    //});

    //function loadClientSiteKeyModal(data) {
    //    $('#ClientSiteKey_Id').val(data.id);
    //    $('#ClientSiteKey_KeyNo').val(data.keyNo);
    //    $('#ClientSiteKey_Description').val(data.description);
    //    $('#csKeyValidationSummary').html('');
    //    $('#client-site-key-modal').modal('show');
    //}

    //function resetClientSiteKeyModal() {
    //    $('#ClientSiteKey_Id').val('');
    //    $('#ClientSiteKey_KeyNo').val('');
    //    $('#ClientSiteKey_Description').val('');
    //    $('#csKeyValidationSummary').html('');
    //    $('#client-site-key-modal').modal('hide');
    //}

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

    //$('#btnSaveGuardSiteSettings').on('click', function () {
    //    var isUpdateDailyLog = false;

    //    const token = $('input[name="__RequestVerificationToken"]').val();
    //    if ($('#enableLogDump').is(":checked")) {
    //        isUpdateDailyLog = true;
    //    }
    //    $.ajax({
    //        url: '/Admin/GuardSettings?handler=SaveSiteEmail',
    //        type: 'POST',
    //        data: {
    //            siteId: $('#gl_client_site_id').val(),
    //            siteEmail: $('#gs_site_email').val(),
    //            enableLogDump: isUpdateDailyLog,
    //            landLine: $('#gs_land_line').val(),
    //            guardEmailTo: $('#gs_email_recipients').val(),
    //            duressEmail: $('#gs_duress_email').val(),
    //            duressSms: $('#gs_duress_sms').val()
    //        },
    //        headers: { 'RequestVerificationToken': token }
    //    }).done(function () {
    //        alert("Saved successfully");
    //    }).fail(function () {
    //        console.log("error");
    //    });
    //});

    //$('#btnSaveCustomFields').on('click', function () {
    //    $('#custom-field-validation ul').html('');
    //    $.ajax({
    //        url: '/Admin/GuardSettings?handler=CustomFields',
    //        type: 'POST',
    //        DataType: 'json',
    //        data: $('#frm_custom_field').serialize(),
    //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //    }).done(function (result) {
    //        if (!result.status)
    //            displayCustomFieldsValidationSummary(result.message[0].split(','));
    //        else {
    //            loadCustomFields();
    //            gridSiteCustomFields.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //        }
    //    }).fail(function () {
    //        console.log("error");
    //    });
    //});

    //function loadCustomFields() {
    //    $.ajax({
    //        url: '/Admin/GuardSettings?handler=CustomFields',
    //        type: 'GET',
    //        dataType: 'json'
    //    }).done(function (data) {
    //        const ulFields = $('#ClientSiteCustomField_Name').siblings('ul.es-list');
    //        $('#ClientSiteCustomField_Name').val('');
    //        ulFields.html('');
    //        data.fieldNames.map(function (result) {
    //            ulFields.append('<li class="es-visible" value="' + result + '">' + result + '</li>');
    //        });

    //        const ulSlots = $('#ClientSiteCustomField_TimeSlot').siblings('ul.es-list');
    //        $('#ClientSiteCustomField_TimeSlot').val('');
    //        ulSlots.html('');
    //        data.slots.map(function (result) {
    //            ulSlots.append('<li class="es-visible" value="' + result + '">' + result + '</li>');
    //        });
    //    });
    //}

    //let gridSiteCustomFields;

    //function renderSiteCustomFieldsManagement(value, record, $cell, $displayEl) {
    //    let $deleteBtn = $('<button class="btn btn-outline-danger mr-2" data-id="' + record.id + '"><i class="fa fa-trash mr-2"></i>Delete</button>');
    //    let $editBtn = $('<button class="btn btn-outline-primary mr-2" data-id="' + record.id + '"><i class="fa fa-pencil mr-2"></i>Edit</button>');
    //    let $updateBtn = $('<button class="btn btn-outline-success mr-2" data-id="' + record.id + '"><i class="fa fa-check-circle mr-2"></i>Update</button>').hide();
    //    let $cancelBtn = $('<button class="btn btn-outline-primary mr-2" data-id="' + record.id + '"><i class="fa fa-times-circle mr-2"></i>Cancel</button>').hide();


    //    $deleteBtn.on('click', function (e) {
    //        gridSiteCustomFields.removeRow($(this).data('id'));
    //    });

    //    $editBtn.on('click', function (e) {
    //        gridSiteCustomFields.edit($(this).data('id'));
    //        $editBtn.hide();
    //        $deleteBtn.hide();
    //        $updateBtn.show();
    //        $cancelBtn.show();
    //    });

    //    $updateBtn.on('click', function (e) {
    //        gridSiteCustomFields.update($(this).data('id'));
    //        $editBtn.show();
    //        $deleteBtn.show();
    //        $updateBtn.hide();
    //        $cancelBtn.hide();
    //    });

    //    $cancelBtn.on('click', function (e) {
    //        gridSiteCustomFields.cancel($(this).data('id'));
    //        $editBtn.show();
    //        $deleteBtn.show();
    //        $updateBtn.hide();
    //        $cancelBtn.hide();
    //    });

    //    $displayEl.empty().append($editBtn)
    //        .append($deleteBtn)
    //        .append($updateBtn)
    //        .append($cancelBtn);
    //}

    //gridSiteCustomFields = $('#cs-custom-fields').grid({
    //    dataSource: '/Admin/GuardSettings?handler=ClientSiteCustomFields',
    //    data: { clientSiteId: $('#gl_client_site_id').val() },
    //    uiLibrary: 'bootstrap4',
    //    iconsLibrary: 'fontawesome',
    //    primaryKey: 'id',
    //    inlineEditing: { mode: 'command', managementColumn: false },
    //    columns: [
    //        { field: 'timeSlot', title: 'Time Slot', editor: true },
    //        { field: 'name', title: 'Field Name', editor: true },
    //        { renderer: renderSiteCustomFieldsManagement }
    //    ],
    //    initialized: function (e) {
    //        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    //    }
    //});

    //if (gridSiteCustomFields) {
    //    gridSiteCustomFields.on('rowDataChanged', function (e, id, record) {
    //        const data = $.extend(true, {}, record);
    //        const token = $('input[name="__RequestVerificationToken"]').val();
    //        $.ajax({
    //            url: '/Admin/GuardSettings?handler=CustomFields',
    //            data: { clientSiteCustomField: record },
    //            type: 'POST',
    //            headers: { 'RequestVerificationToken': token },
    //        }).done(function (result) {
    //            if (result.status) gridSiteCustomFields.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //            else alert(result.message);
    //        }).fail(function () {
    //            console.log('error');
    //        }).always(function () {

    //        });
    //    });

    //    gridSiteCustomFields.on('rowRemoving', function (e, id, record) {
    //        if (confirm('Are you sure want to delete this entry?')) {
    //            const token = $('input[name="__RequestVerificationToken"]').val();
    //            $.ajax({
    //                url: '/Admin/GuardSettings?handler=DeleteClientSiteCustomField',
    //                data: { id: record },
    //                type: 'POST',
    //                headers: { 'RequestVerificationToken': token },
    //            }).done(function (result) {
    //                if (!result.success) alert(result.message);
    //                else {
    //                    loadCustomFields();
    //                    gridSiteCustomFields.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //                }
    //            }).fail(function () {
    //                console.log('error');
    //            });
    //        }
    //    });
    //}

    //let gridSitePocs;
    //gridSitePocs = $('#cs-pocs').grid({
    //    dataSource: '/Admin/GuardSettings?handler=SitePocs',
    //    uiLibrary: 'bootstrap4',
    //    iconsLibrary: 'fontawesome',
    //    primaryKey: 'id',
    //    inlineEditing: { mode: 'command' },
    //    columns: [
    //        { width: 120, field: 'name', title: 'Name', editor: true }
    //    ],
    //    initialized: function (e) {
    //        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    //    }
    //});

    //if (gridSitePocs) {
    //    gridSitePocs.on('rowDataChanged', function (e, id, record) {
    //        const data = $.extend(true, {}, record);
    //        $.ajax({
    //            url: '/Admin/GuardSettings?handler=SitePoc',
    //            data: { record: data },
    //            type: 'POST',
    //            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //        }).done(function (result) {
    //            if (result.success) gridSitePocs.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //            else alert(result.message);
    //        }).fail(function () {
    //            alert('error');
    //        }).always(function () {
    //            if (isSitePocAdding)
    //                isSitePocAdding = false;
    //        });
    //    });

    //    gridSitePocs.on('rowRemoving', function (e, id, record) {
    //        if (confirm('Are you sure want to delete this site POC details?')) {
    //            $.ajax({
    //                url: '/Admin/GuardSettings?handler=DeleteSitePoc',
    //                data: { id: record },
    //                type: 'POST',
    //                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //            }).done(function (result) {
    //                if (result.success) gridSitePocs.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //                else alert(result.message);
    //            }).fail(function () {
    //                aler('error');
    //            }).always(function () {
    //                if (isSitePocAdding)
    //                    isSitePocAdding = false;
    //            });
    //        }
    //    });
    //}

    //let isSitePocAdding = false;
    //$('#add_site_poc').on('click', function () {

    //    if (isSitePocAdding) {
    //        alert('Unsaved changes in the grid. Refresh the page');
    //    } else {
    //        isSitePocAdding = true;
    //        gridSitePocs.addRow({ 'id': -1, 'name': '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
    //    }
    //});

    //let gridSiteLocations;
    //gridSiteLocations = $('#cs-locations').grid({
    //    dataSource: '/Admin/GuardSettings?handler=SiteLocations',
    //    uiLibrary: 'bootstrap4',
    //    iconsLibrary: 'fontawesome',
    //    primaryKey: 'id',
    //    inlineEditing: { mode: 'command' },
    //    columns: [
    //        { width: 120, field: 'name', title: 'Name', editor: true }
    //    ],
    //    initialized: function (e) {
    //        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    //    }
    //});

    //if (gridSiteLocations) {
    //    gridSiteLocations.on('rowDataChanged', function (e, id, record) {
    //        const data = $.extend(true, {}, record);
    //        $.ajax({
    //            url: '/Admin/GuardSettings?handler=SiteLocation',
    //            data: { record: data },
    //            type: 'POST',
    //            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //        }).done(function (result) {
    //            if (result.success) gridSiteLocations.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //            else alert(result.message);
    //        }).fail(function () {
    //            alert('error');
    //        }).always(function () {
    //            if (isSiteLocationAdding)
    //                isSiteLocationAdding = false;
    //        });
    //    });

    //    gridSiteLocations.on('rowRemoving', function (e, id, record) {
    //        if (confirm('Are you sure want to delete this site location details?')) {
    //            $.ajax({
    //                url: '/Admin/GuardSettings?handler=DeleteSiteLocation',
    //                data: { id: record },
    //                type: 'POST',
    //                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //            }).done(function (result) {
    //                if (result.success) gridSiteLocations.reload({ clientSiteId: $('#gl_client_site_id').val() });
    //                else alert(result.message);
    //            }).fail(function () {
    //                aler('error');
    //            }).always(function () {
    //                if (isSiteLocationAdding)
    //                    isSiteLocationAdding = false;
    //            });
    //        }
    //    });
    //}

    //let isSiteLocationAdding = false;
    //$('#add_site_location').on('click', function () {

    //    if (isSiteLocationAdding) {
    //        alert('Unsaved changes in the grid. Refresh the page');
    //    } else {
    //        isSiteLocationAdding = true;
    //        gridSiteLocations.addRow({ 'id': -1, 'name': '', clientSiteId: $('#gl_client_site_id').val() }).edit(-1);
    //    }
    //});

    /****** Guards *******/

    function renderGuardActiveCell(value, type, data) {
        if (type === 'display') {
            let cellValue;

            if (value) {
                // Check if data.guardlogins.logindate is present
                if (data.loginDate) {
                    cellValue = '<i class="fa fa-check-circle text-success"></i>' +
                        '[' +
                        '<a href="#guardLogBookInfoModal" id="btnLogBookDetailsByGuard">1</a>' +
                        ']<input type="hidden" id="GuardId" value="' + data.id + '">';
                } else {
                    cellValue = '<i class="fa fa-check-circle text-success"></i>' +
                        '<input type="hidden" id="GuardId" value="' + data.id + '">';
                }
            } else {
                cellValue = '<i class="fa fa-times-circle text-danger"></i>' +
                    '<input type="hidden" id="GuardId" value="' + data.id + '">';
            }

            // Add enrollment date if available
            if (data.dateEnrolled) {
                cellValue += '<br/> <span class="small">Enrolled: ' + getFormattedDate(new Date(data.dateEnrolled), null, ' ') + '</span>';
            }
            return cellValue;
        }
        return value;
    }


    function renderGuardActiveCellHrValues(value, type, data) {

        if (data.hR1Status == 'green') {
            return '<i class="fa fa-circle text-success mr-2"></i>';
        }
        else if (data.hR1Status == 'red') {
            return '<i class="fa fa-circle text-danger mr-2"></i>';
        }
        else if (data.hR1Status == 'yellow') {
            return '<i class="fa fa-circle text-warning mr-2"></i>';
        }


    }

    function format_guards_child_row(d) {
        var val = d;
        return val;
    }

    var guardSettingsDataLoaded = false;
    var guardSettings = $('#guard_settings').DataTable({
        pageLength: 50,
        autoWidth: false,
        ajax: '/Admin/GuardSettings?handler=Guards',
        processing: true,
        language: {
            'loadingRecords': '&nbsp;',
            'processing': 'Loading data please wait...'
        },
        columns: [{
            className: 'dt-control',
            orderable: false,
            data: null,
            width: '2%',
            defaultContent: '',
        },
        { data: 'name', width: "10%" },
        { data: 'securityNo', width: "10%" },
        { data: 'initial', width: "5%" },
        { data: 'state', width: "5%" },
        { data: 'provider', width: "13%" },
        { data: 'clientSites', orderable: false, width: "15%" },
        { data: 'pin', width: "1%", visible: false },
        { data: 'loginDate', visible: false },
        { data: 'gender', width: "5%" },
        {
            data: 'isActive', name: 'isactive', className: "text-center", width: "4%", 'render': function (value, type, data) {
                return renderGuardActiveCell(value, type, data);
            }
        },


        {
            data: 'hR1Status', name: 'hR1Status', className: "text-center no-padding", width: "2%", 'render': function (value, type, data) {
                return renderGuardActiveCellHrValues(value, type, data);
            }
        },
        {
            data: 'hR2Status', name: 'hR2Status', className: "text-center no-padding", width: "2%", 'render': function (value, type, data) {
                return renderGuardActiveCellHr2Values(value, type, data);
            }
        },

        {
            data: 'hR3Status', name: 'hR3Status', className: "text-center no-padding", width: "2%", 'render': function (value, type, data) {
                return renderGuardActiveCellHr3Values(value, type, data);
            }
        },



        // { data: 'hR1Status', name: 'hR1Status', width: "2%" },
        // { data: 'hR2Status', name: 'hR2Status', width: "2%" },
        //{ data: 'hR3Status', name: 'hR3Status', width: "2%" },


        {
            targets: -1,
            data: null,
            defaultContent: '<button  class="btn btn-outline-primary  mb-1" name="btn_edit_guard"><i class="fa fa-pencil"></i></button>' + '<img src="/images/Timesheet.jpg" style="width: 96%;height: 96%;" alt="Image" class="clickable-image" style="cursor: pointer;" alt="Timesheet" name="btn_timesheet"/>',
            orderable: false,
            className: "text-center",
            width: "0%"
        },
        ],
        initComplete: function (settings, json) {
            $('#chkbxfilterGuardActive').prop("disabled", false);
            $('#chkbxfilterGuardInActive').prop("disabled", false);
            guardSettingsDataLoaded = true;
            $('#chkbxfilterGuardActive').trigger('click');
        }
    }).on('preInit.dt', function (e, settings) {
        $('#chkbxfilterGuardActive').prop("disabled", true);
        $('#chkbxfilterGuardInActive').prop("disabled", true);
        guardSettingsDataLoaded = false;
    });




    function renderGuardActiveCellHrValues(value, type, data) {

        if (data.hR1Status == 'Green') {
            return '<i class="fa fa-circle text-success"></i><span style="color:#f8f9fa;font-size:1px;">green</span>';
        }
        else if (data.hR1Status == 'Red') {
            return '<i class="fa fa-circle text-danger"></i><span style="color:#f8f9fa;font-size:1px;">red</span>';
        }
        else if (data.hR1Status == 'Yellow') {
            return '<i class="fa fa-circle text-warning"></i></i><span style="color:#f8f9fa;font-size:1px;">yellow</span>';
        }
        else {
            return '<i class="fa fa-circle text-secondary"></i><span style="color:#f8f9fa;font-size:1px;">grey</span>';
        }


    }

    function renderGuardActiveCellHr2Values(value, type, data) {

        if (data.hR2Status == 'Green') {
            return '<i class="fa fa-circle text-success"></i><span style="color:#f8f9fa;font-size:1px;">green</span>';
        }
        else if (data.hR2Status == 'Red') {
            return '<i class="fa fa-circle text-danger"></i><span style="color:#f8f9fa;font-size:1px;">red</span>';
        }
        else if (data.hR2Status == 'Yellow') {
            return '<i class="fa fa-circle text-warning"></i><span style="color:#f8f9fa;font-size:1px;">yellow</span>';
        }
        else {
            return '<i class="fa fa-circle text-secondary"></i><span style="color:#f8f9fa;font-size:1px;">grey</span>';
        }


    }

    function renderGuardActiveCellHr3Values(value, type, data) {

        if (data.hR3Status == 'Green') {
            return '<i class="fa fa-circle text-success"></i><span style="color: #f8f9fa;font-size:1px;">green</span>';
        }
        else if (data.hR3Status == 'Red') {
            return '<i class="fa fa-circle text-danger"></i><span style="color: #f8f9fa;font-size:1px;">red</span>';
        }
        else if (data.hR3Status == 'Yellow') {
            return '<i class="fa fa-circle text-warning"></i><span style="color: #f8f9fa;font-size:1px;">yellow</span>';
        }
        else {
            return '<i class="fa fa-circle text-secondary"></i><span style="color:#f8f9fa;font-size:1px;">grey</span>';
        }


    }

    //$('#btn_refresh_guard_top').on('click', function () {
    //    if (guardSettings) {   
    //        guardSettings.clear().draw();
    //        guardSettings.ajax.reload();            
    //    }        
    //});

    $('#chkbxfilterGuardActive').on('click', function () {
        var thisCheck = $(this);
        if (guardSettingsDataLoaded) {
            if (thisCheck.is(':checked')) {
                $('#chkbxfilterGuardInActive').prop("checked", false);
            }
            filterActiveInActiveGuards(guardSettings);
        }
    });

    $('#chkbxfilterGuardInActive').on('click', function () {
        var thisCheck = $(this);
        if (guardSettingsDataLoaded) {
            if (thisCheck.is(':checked')) {
                $('#chkbxfilterGuardActive').prop("checked", false);
            }
            filterActiveInActiveGuards(guardSettings);
        }
    });

    function filterActiveInActiveGuards(table) {
        let filter = '';
        let guardInActive = $('#chkbxfilterGuardInActive').is(':checked');
        let guardActive = $('#chkbxfilterGuardActive').is(':checked');
        let regex = true;
        let smart = true;

        if (guardActive)
            filter = 'true';
        else if (guardInActive)
            filter = 'false';

        //table.search(filter.value, regex, smart).draw();
        table.column('isactive:name').search(filter, regex, smart).draw();
    }

    //$('#guard_settings tbody').on('click', 'td.dt-control', function () {
    //    var tr = $(this).closest('tr');
    //    var row = guardSettings.row(tr);

    //    $.ajax({
    //        type: 'GET',
    //        url: '/Admin/Guardsettings?handler=GuardLicenseAndCompliance',
    //        data: { guardId: row.data().id },
    //    }).done(function (response) {
    //        if (row.child.isShown()) {
    //            row.child.hide();
    //            tr.removeClass('shown');
    //        } else {
    //            row.child(format_guards_child_row(response), 'bg-light').show();
    //            tr.addClass('shown');
    //        }
    //    });
    //});

    $('#guard_settings tbody').on('click', 'button[name=btn_edit_guard]', function () {
        resetGuardDetailsModal();
        $('.btn-add-guard-addl-details').show();

        var data = guardSettings.row($(this).parents('tr')).data();

        $('#Guard_Name').val(data.name);
        $('#Guard_SecurityNo').val(data.securityNo);
        $('#Guard_Initial').val(data.initial);
        $('#Guard_State').val(data.state);
        $('#Guard_Provider').val(data.provider);
        $('#Guard_Pin').val(data.pin);


        $('#Guard_Mobile').val(data.mobile)
        $('#Guard_Email').val(data.email)
        $('#Guard_Id').val(data.id);
        $('#cbIsActive').prop('checked', data.isActive);
        $('#cbIsRCAccess').prop('checked', data.isRCAccess);
        $('#cbIsKPIAccess').prop('checked', data.isKPIAccess);
        $('#addGuardModal').modal('show');
        $('#GuardLicense_GuardId').val(data.id);
        $('#GuardCompliance_GuardId').val(data.id);
        $('#GuardComplianceandlicense_GuardId').val(data.id);
        $('#GuardComplianceandlicense_LicenseNo').val(data.securityNo);
        //p1-224 RC Bypass For HR -start
        $('#Guard_IsRCBypass').val(data.isRCBypass);
        $('#cbIsRCBypass').prop('checked', data.isRCBypass);
        $('#Guard_Gender').val(data.gender);
        //p1-224 RC Bypass For HR -end
        // ;
        var selectedValues = [];
        if (data.isAdminGlobal) {
            selectedValues.push(6);
        }
        if (data.isAdminPowerUser) {
            selectedValues.push(5);
        }
        if (data.isRCAccess) {
            selectedValues.push(4);
        }
        if (data.isKPIAccess) {
            selectedValues.push(3);
        }
        if (data.isLB_KV_IR) {
            selectedValues.push(1);
        }
        if (data.isSTATS) {
            selectedValues.push(2);
        }
        selectedValues.forEach(function (value) {

            $(".multiselect-option input[type=checkbox][value='" + value + "']").prop("checked", true);
        });
        gridGuardLicensesAndLicence.clear().draw();
        gridGuardLicensesAndLicence.ajax.reload();
        $("#Guard_Access").multiselect();
        $("#Guard_Access").val(selectedValues);
        $("#Guard_Access").multiselect("refresh");
    });
    $('#guard_settings tbody').on('click', 'img[name=btn_timesheet]', function () {
        $('#TimesheetGuard_Id').val('-1');
        $('#startDate').val('');
        $('#endDate').val('');
        $('#frequency').val('');
        var data = guardSettings.row($(this).parents('tr')).data();
        $('#TimesheetGuard_Id').val(data.id);
        $('#timesheetModal').modal('show');
    });
    // Download Timesheet start

    //    $('#btnDownloadTimesheet').on('click', function () {
    //    $.ajax({
    //        type: 'GET',
    //        url: '/Admin/Settings?handler=DownloadTimesheet',
    //        data: {
    //            startdate: $('#startDate').val(),
    //            endDate: $('#endDate').val(),
    //            frequency: $('#frequency').val(),
    //            guradid: $('#Guard_Id').val(),
    //        },
    //        xhrFields: {
    //            responseType: 'blob' // For handling binary data
    //        },
    //        success: function (data, textStatus, request) {
    //            var contentDispositionHeader = request.getResponseHeader('Content-Disposition');
    //            var fileName = '';
    //            var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
    //            var matches = filenameRegex.exec(contentDispositionHeader);
    //            var downloadedFileName = matches !== null && matches[1] ? matches[1].replace(/['"]/g, '') : fileName;
    //            // Create a Blob with the PDF data and initiate the download
    //            var blob = new Blob([data], { type: 'application/pdf' });
    //            // // Create a temporary anchor element to trigger the download
    //            //var url = window.URL.createObjectURL(blob);
    //            // // Open the PDF in a new tab
    //            //var newTab = window.open(url, '_blank');

    //            const URL = window.URL || window.webkitURL;
    //            const displayNameHash = encodeURIComponent(`#displayName=${downloadedFileName}`);
    //            const bloburl = URL.createObjectURL(blob);
    //            const objectUrl = URL.createObjectURL(blob) + displayNameHash;
    //            const windowUrl = window.location.origin; // + window.location.pathname;
    //            const viewerUrl = `${windowUrl}/lib/Pdfjs/web/viewer.html?file=`;
    //            var newTab = window.open(`${viewerUrl}${objectUrl}`);
    //            if (!newTab) {
    //                // If the new tab was blocked, fallback to downloading the file
    //                var a = document.createElement('a');
    //                a.href = bloburl;
    //                a.download = downloadedFileName;
    //                a.click();
    //            }

    //            URL.revokeObjectURL(bloburl);
    //            URL.revokeObjectURL(objectUrl);

    //            //if (!newTab) {
    //            //    // If the new tab was blocked, fallback to downloading the file
    //            //    var a = document.createElement('a');
    //            //    a.href = url;
    //            //    a.download = downloadedFileName;
    //            //    a.click();
    //            //}
    //            //window.URL.revokeObjectURL(url);
    //        },
    //        error: function () {
    //            alert('Error while downloading the PDF.');
    //        }
    //    }).done(function (result) {

    //    });
    //});
    $('#btnDownloadTimesheet').on('click', function (e) {
        var startDate = $('#startDate').val();
        var endDate = $('#endDate').val();

        // Check if both startDate and endDate have values
        if (!startDate || !endDate) {
            alert("Please select both start date and end date.");
            return; // Exit the function if validation fails
        }




        $.ajax({
            url: '/Admin/Settings?handler=DownloadTimesheet',
            data: {
                startdate: $('#startDate').val(),
                endDate: $('#endDate').val(),
                frequency: $('#frequency').val(),
                guradid: $('#TimesheetGuard_Id').val(),
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            if (response.statusCode === -1) {

            } else {





                var newTab = window.open(response.fileName, '_blank');
                if (!newTab) {

                    var a = document.createElement('a');
                    a.href = response.fileName;
                    a.download = "TimeSheet_Report";
                    a.click();
                }

            }
        });
    });
    $('#btnDownloadTimesheetFrequency').on('click', function (e) {
        var Frequency = $('#frequency').val();

        if (!Frequency) {
            alert("Please select Instant Timesheet.");
            return; // Exit the function if validation fails
        }




        $.ajax({
            url: '/Admin/Settings?handler=DownloadTimesheetFrequency',
            data: {
                frequency: $('#frequency').val(),
                guradid: $('#TimesheetGuard_Id').val(),
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            if (response.statusCode === -1) {

            } else {





                var newTab = window.open(response.fileName, '_blank');
                if (!newTab) {

                    var a = document.createElement('a');
                    a.href = response.fileName;
                    a.download = "TimeSheet_Report";
                    a.click();
                }

            }
        });
    });

    //Download Timesheet stop
    $('#guard_settings tbody').on('click', '#btnLogBookDetailsByGuard', function () {

        $('#guardLogBookInfoModal').modal('show');
        var GuardId = $(this).closest("td").find('#GuardId').val();
        $('#txtGuardId').val(GuardId);
        //ActiveGuardsLogBookDetails.ajax.reload();
        fetchGuardLogBookDetails(GuardId);
    });

    function fetchGuardLogBookDetails(guardId) {
        $.ajax({
            url: '/Admin/GuardSettings?handler=LastTimeLogin',
            method: 'GET',
            data: { guardId: guardId },
            success: function (data) {
                // Destroy the existing DataTable instance if it exists
                if ($.fn.DataTable.isDataTable('#ActiveGuardsLogBookDetails')) {
                    $('#ActiveGuardsLogBookDetails').DataTable().destroy();
                }

                $('#ActiveGuardsLogBookDetails').DataTable({
                    autoWidth: false,
                    ordering: false,
                    searching: false,
                    paging: false,
                    info: false,
                    data: data, // Use the data fetched from the AJAX request
                    columns: [
                        {
                            data: 'loginDate',
                            width: "4%",
                            render: function (data, type, row) {
                                // Convert the date string to a JavaScript Date object
                                var date = new Date(data);

                                // Format the date to display only the date part without the time
                                var formattedDate = date.toLocaleDateString('en-GB', {
                                    day: '2-digit',
                                    month: '2-digit',
                                    year: 'numeric',
                                    hour: '2-digit',
                                    minute: '2-digit',
                                    second: '2-digit'
                                });
                                var additionalData = row.eventDateTimeZoneShort;
                                if (additionalData != null) {
                                    return formattedDate + ' (' + additionalData + ')';
                                } else {
                                    return formattedDate;
                                }
                            }
                        },
                        {
                            data: 'clientSite', width: "10%",
                            render: function (data, type, row) {

                                var name1 = data.name;

                                return name1;

                            }
                        },
                    ]
                });
            },
            error: function (xhr, status, error) {
                console.error('Error fetching data:', error);
            }
        });
    }

    // P1#231 HR Download Excel for settings -start
    $("#btnDownloadGuardLogExcel").click(async function () {
        const activeChecked = $('#chkbxfilterGuardActive').is(':checked');
        const inactiveChecked = $('#chkbxfilterGuardInActive').is(':checked');
        const currentDateTime = new Date().toISOString().split('T')[0];
        const fileName = `Guards - ${currentDateTime}.xlsx`;



        $('#loader').show(); // Show loader

        var guardIds = [];

        // Use the DataTable API to get the instance of the table
        var table = $('#guard_settings').DataTable();

        // Get the filtered rows only
        table.rows({ filter: 'applied' }).every(function () {
            var rowData = this.data(); // Get the data for each filtered row

            // Assuming GuardId is a property in your row data
            var guardIdValue = rowData.id; // Replace with the actual property if necessary

            // Add the value directly to the array (as a string)
            if (guardIdValue) {
                guardIds.push(guardIdValue.toString()); // Store as a string
            }
        });





        try {
            // Fetch data from the server
            const response = await $.ajax({
                url: '/Admin/GuardSettings?handler=ExportGuardsToExcel',
                type: 'POST',
                dataType: "json",
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                data: {
                    active: activeChecked,
                    inactive: inactiveChecked,
                    guardIdsFilter: guardIds
                }
            });

            $('#loader').hide(); // Hide loader


            //const filteredData = response.data.filter(item => guardIds.includes(item.id));
            // Ensure response contains data
            const rawData = Array.isArray(response.data) ? response.data : [];
            if (rawData.length === 0) {
                console.error("No data available to export.");
                alert("No data available to export.");
                return;
            }

            // Define headers and column widths
            const headers = ['Name', 'Security No', 'Initial', 'State', 'Provider', 'Mobile', 'Email', 'Client Sites', 'Gender', 'Is Active', 'HR1 Status', 'HR2 Status', 'HR3 Status'];
            const columnWidths = [20, 20, 10, 10, 20, 20, 20, 25, 15, 15, 10, 10, 10]; // Example widths



            const ws = XLSX.utils.aoa_to_sheet([[]]);


            //const range = XLSX.utils.decode_range(ws['!ref'] || 'A1:J19'); // Ensure range is defined
            //for (let row = range.s.r; row <= range.e.r; row++) {
            //    for (let col = range.s.c; col <= range.e.c; col++) {
            //        const cellAddress = { c: col, r: row };
            //        const cellRef = XLSX.utils.encode_cell(cellAddress);
            //        if (!ws[cellRef]) ws[cellRef] = {}; // Create the cell if it doesn't exist
            //        ws[cellRef].s = {
            //            fill: {
            //                fgColor: { rgb: "FFFF00" } // Yellow background color
            //            },
            //            font: {
            //                sz: 12, // Font size
            //                bold: false, // Font weight
            //                color: { rgb: "000000" } // Font color
            //            },
            //            alignment: {
            //                horizontal: "center" // Center align text
            //            }
            //        };
            //    }
            //}

            // Create the data rows
            const dataRows = [headers, ...rawData.map(item => [
                item.name,
                item.securityNo,
                item.initial,
                item.state,
                item.provider,
                item.mobile,
                item.email,
                item.clientSites.replace('<br />', ' '),
                item.gender,
                item.isActive ? 'TRUE' : 'FALSE', // Ensure values are strings
                item.hR1Status,
                item.hR2Status,
                item.hR3Status
            ])];

            // Update the worksheet with headers and data
            XLSX.utils.sheet_add_aoa(ws, dataRows);

            // Set column widths
            ws['!cols'] = columnWidths.map(width => ({ wch: width }));

            // Create a new workbook and append the worksheet
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'Guards');

            // Write the file
            XLSX.writeFile(wb, fileName);
        } catch (error) {
            $('#loader').hide(); // Hide loader in case of error
            console.error('Error fetching or processing data:', error); // Log error
            alert("An error occurred while exporting data.");
        }
    });




    $('.dropdownGuardHrFilter > button').prop('disabled', true);
    $('.dropdownGuardHrFilter > .dropdown-menu > .dropdown-item').on('click', function () {
        $(this).closest('.dropdown').find('button').html($(this).html());
        const kvlStatusFilter = $(this).data('val');

        if (kvlStatusFilter !== 0) {
            var filter = 'true'; // or 'false'
            var regex = true;   // No need for regex
            var smart = false;   // Exact match only
            if ($('#chkbxHR1').is(':checked')) {
                $('#chkbxHR2').prop('checked', false);
                $('#chkbxHR3').prop('checked', false);
                // Apply the search filter to the 'isactive' column
                guardSettings.column('hR1Status:name').search(kvlStatusFilter, regex, smart).draw();
                guardSettings.ajax.reload();
            }
            else if ($('#chkbxHR2').is(':checked')) {
                $('#chkbxHR1').prop('checked', false);
                $('#chkbxHR3').prop('checked', false);
                // Apply the search filter to the 'isactive' column
                guardSettings.column('hR2Status:name').search(kvlStatusFilter, regex, smart).draw();
                guardSettings.ajax.reload();
            }
            else if ($('#chkbxHR3').is(':checked')) {
                $('#chkbxHR1').prop('checked', false);
                $('#chkbxHR2').prop('checked', false);
                // Apply the search filter to the 'isactive' column
                guardSettings.column('hR3Status:name').search(kvlStatusFilter, regex, smart).draw();
                guardSettings.ajax.reload();
            }
            else {
                // Clear global search
                guardSettings.search('');

                // Clear individual column searches
                guardSettings.columns().search('');

                // Redraw the table

                // Clear the sorting
                guardSettings.order([]).draw(false);
                guardSettings.draw(false);
                filterActiveInActiveGuards(guardSettings);


            }

        }
        else {

            $('#chkbxHR1').prop('checked', false);
            $('#chkbxHR2').prop('checked', false);
            $('#chkbxHR3').prop('checked', false);
            $('.dropdownGuardHrFilter > button').prop('disabled', true);
            // Clear global search
            guardSettings.search('');

            // Clear individual column searches
            guardSettings.columns().search('');

            // Redraw the table

            // Clear the sorting
            guardSettings.order([]).draw(false);
            guardSettings.draw(false);
            filterActiveInActiveGuards(guardSettings);
            //$('#chkbxfilterGuardActive').prop("disabled", false);
            //$('#chkbxfilterGuardInActive').prop("disabled", false);
            //$('#chkbxfilterGuardActive').trigger('click');

        }

        filterActiveInActiveGuards(guardSettings);


    });

    $('#chkbxHR1').on('click', function () {
        var thisCheck = $(this);

        if (thisCheck.is(':checked')) {
            $('#chkbxHR2').prop('checked', false);
            $('#chkbxHR3').prop('checked', false);
            $('.dropdownGuardHrFilter > button').prop('disabled', false);
        }
        else {
            $('.dropdownGuardHrFilter > button').prop('disabled', true);
        }
        $('.dropdownGuardHrFilter > button').html('<i class="fa fa-circle text-primary mr-2"></i>Show All Entries');
        // Clear global search
        guardSettings.search('');

        // Clear individual column searches
        guardSettings.columns().search('');

        // Redraw the table

        // Clear the sorting
        guardSettings.order([]).draw(false);
        guardSettings.draw(false);
        filterActiveInActiveGuards(guardSettings);
        //$('.dropdownGuardHrFilter > .dropdown-menu > .dropdown-item').first().trigger('click');
    });
    $('#chkbxHR2').on('click', function () {
        var thisCheck = $(this);

        if (thisCheck.is(':checked')) {
            $('#chkbxHR1').prop('checked', false);
            $('#chkbxHR3').prop('checked', false);
            $('.dropdownGuardHrFilter > button').prop('disabled', false);
        }
        else {
            $('.dropdownGuardHrFilter > button').prop('disabled', true);
        }
        $('.dropdownGuardHrFilter > button').html('<i class="fa fa-circle text-primary mr-2"></i>Show All Entries');
        // Clear global search
        guardSettings.search('');

        // Clear individual column searches
        guardSettings.columns().search('');

        // Redraw the table

        // Clear the sorting
        guardSettings.order([]).draw(false);
        guardSettings.draw(false);
        filterActiveInActiveGuards(guardSettings);
        // $('.dropdownGuardHrFilter > .dropdown-menu > .dropdown-item').first().trigger('click');
    });
    $('#chkbxHR3').on('click', function () {
        var thisCheck = $(this);

        if (thisCheck.is(':checked')) {
            $('#chkbxHR1').prop('checked', false);
            $('#chkbxHR2').prop('checked', false);
            $('.dropdownGuardHrFilter > button').prop('disabled', false);
        }
        else {
            $('.dropdownGuardHrFilter > button').prop('disabled', true);
        }
        $('.dropdownGuardHrFilter > button').html('<i class="fa fa-circle text-primary mr-2"></i>Show All Entries');
        // Clear global search
        guardSettings.search('');

        // Clear individual column searches
        guardSettings.columns().search('');

        // Redraw the table

        // Clear the sorting
        guardSettings.order([]).draw(false);
        guardSettings.draw(false);
        filterActiveInActiveGuards(guardSettings);
        //$('.dropdownGuardHrFilter > .dropdown-menu > .dropdown-item').first().trigger('click');
    });


    // P1#231 HR Download Excel for settings -end


    $('#btn_add_guard_top, #btn_add_guard_bottom').on('click', function () {
        gridGuardLicenses.clear().draw();
        gridGuardCompliances.clear().draw();
        gridGuardLicensesAndLicence.clear().draw();

        $('.btn-add-guard-addl-details').hide();
        resetGuardDetailsModal();
        let value = 1;
        $(".multiselect-option input[type=checkbox][value='" + value + "']").prop("checked", true);

        // Initialize the multiselect dropdown
        $("#Guard_Access").multiselect();
        $("#Guard_Access").val(value);
        $("#Guard_Access").multiselect("refresh");
        $('#addGuardModal').modal('show');

    });

    function resetGuardDetailsModal() {
        $('#Guard_Name').val('');
        $('#Guard_SecurityNo').val('');
        $('#Guard_Initial').val('');
        $('#Guard_State').val('');
        $('#Guard_Provider').val('');
        $('#Guard_Pin').val('');
        $('#Guard_Email').val('');
        $('#Guard_Mobile').val('');
        $('#Guard_Mobile').val('+61 4');
        $('#Guard_Id').val('-1');
        $('#cbIsActive').prop('checked', true);
        //p1-224 RC Bypass For HR -start
        $('#cbIsRCAccess').prop('checked', false);
        $('#cbIsKPIAccess').prop('checked', false);
        $('#Guard_IsRCBypass').val(false);
        $('#cbIsRCBypass').prop('checked', false);
        //p1-224 RC Bypass For HR -end
        $('#glValidationSummary').html('');
        //p1-224 RC Bypass For HR -start
        $('#Guard_Gender').val('');
        //p1-224 RC Bypass For HR -end
        $(".multiselect-option input[type=checkbox]").prop("checked", false);
    }

    $('#btn_save_guard').on('click', function () {
        clearGuardValidationSummary('glValidationSummary');
        $('#guard_saved_status').hide();
        $('#Guard_IsActive').val($(cbIsActive).is(':checked'));
        $('#Guard_IsRCBypass').val($(cbIsRCBypass).is(':checked'));
        //$('#Guard_IsRCAccess').val($(cbIsRCAccess).is(':checked'));
        //$('#Guard_IsKPIAccess').val($(cbIsKPIAccess).is(':checked'));
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
                $('#GuardComplianceandlicense_GuardId').val(result.guardId);
                if (result.initalsChangedMessage !== '') {
                    alert(result.initalsChangedMessage);
                    $('#Guard_Initial').val(result.initalsUsed);
                }
                $('#guard_saved_status').show().delay(2000).hide(0);
                $('.btn-add-guard-addl-details').show();
                /*   gridGuardLicenses.ajax.reload();*/
                gridGuardLicensesAndLicence.clear().draw();
                gridGuardLicensesAndLicence.ajax.reload();
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
                    let colAlign = 'center', colEditor = true, colCssClass = 'text-center';
                    let colWidth = 80;
                    console.log(d.key);
                    if (d.key === 'timeSlot') {
                        colAlign = 'center';
                        colEditor = false;
                        colWidth = 60;
                    }
                    else if (d.key === 'Romeo Plate + Initial' || d.key === 'Missed Patrols') {
                        colCssClass = 'text-right';
                    }
                    columnData.push({ field: d.key, title: d.value, align: colAlign, editor: colEditor, width: colWidth, cssClass: colCssClass });
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
            $('#card_custom_field_log').addClass('d-none');
            if (totalRecords > 0) {
                $(this).show();
                $('#card_custom_field_log').removeClass('d-none');
            }
        });
    }

    function renderCustomFieldLogManagement(value, record, $cell, $displayEl, id) {
        var $editBtn = $('<button class="btn btn-outline-primary btn-dgl-edit mt-1" data-id="' + id + '"><i class="fa fa-pencil"></i></button>'),
            $updateBtn = $('<button class="btn btn-outline-success btn-dgl-edit mt-1" data-id="' + id + '"><i class="fa fa-check-circle"></i></button>').hide(),
            $cancelBtn = $('<button class="btn btn-outline-danger btn-dgl-edit mt-1" data-id="' + id + '"><i class="fa fa-times-circle"></i></button>').hide();

        $editBtn.on('click', function (e) {
            isPaused = true;
            gridCustomFieldLogs.edit($(this).data('id'));
            $editBtn.hide();
            $updateBtn.show();
            $cancelBtn.show();
        });

        $updateBtn.on('click', function (e) {
            isPaused = false;
            gridCustomFieldLogs.update($(this).data('id'));
            $editBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
        });

        $cancelBtn.on('click', function (e) {
            isPaused = false;
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
            /*timer pause while editing*/
            isPaused = true;
        });

        $updateBtn.on('click', function (e) {
            gridSitePatrolCarLogs.update($(this).data('id'));
            $editBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
            /*timer pause while editing*/
            isPaused = false;
        });

        $cancelBtn.on('click', function (e) {
            gridSitePatrolCarLogs.cancel($(this).data('id'));
            $editBtn.show();
            $updateBtn.hide();
            $cancelBtn.hide();
            /*timer pause while editing*/
            isPaused = false;
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
            { field: 'patrolCar', title: 'Patrol Cars', align: 'center', cssClass: "text-left", editor: false, renderer: function (value, record) { return renderPatrolCar(value, record); }, width: 75 },
            { field: 'mileage', title: 'Kms', align: 'center', cssClass: "text-right", editor: true, renderer: function (value, record) { return record.mileageText; }, width: 15 },
            { renderer: renderPatrolCarLogManagement, align: 'center', width: 10 }
        ],
        initialized: function (e) {
            var html = '<i class="fa fa-cogs" aria-hidden="true"></i>'
            $(e.target).find('thead tr th:last').html(html);

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
            $('#card_patrol_car').addClass('d-none');
            if (totalRecords > 0) {
                $(this).show();
                $('#card_patrol_car').removeClass('d-none');
            }
        });
    }

    /****** Guard  Licenses *******/
    function resetGuardLicenseAddModal() {
        $('#GuardLicense_Id').val('');
        $('#GuardLicense_LicenseNo').val('');
        $('#GuardLicense_LicenseType').val('');
        $('#GuardLicense_Reminder1').val('45');
        $('#GuardLicense_Reminder2').val('7');
        $('#GuardLicense_ExpiryDate').val('');
        $('#GuardLicense_FileName').val('');
        $('#guardLicense_fileName').text('None');
        clearGuardValidationSummary('licenseValidationSummary');
    }
    function resetGuardLicenseandComplianceAddModal() {
        $('#GuardComplianceandlicense_Id').val('');
        $('#Description').val('');
        $('#LicanseTypeFilter').prop('checked', false);
        $('#ComplianceDate').text('Expiry Date (DOE)');
        $('#IsDateFilterEnabledHidden').val(false)
        $("#GuardComplianceAndLicense_ExpiryDate1").val('');
        $("#GuardComplianceAndLicense_ExpiryDate1").prop('min', function () {
            return new Date().toJSON().split('T')[0];
        });
        $("#GuardComplianceAndLicense_ExpiryDate1").prop('max', '');
        $('#HRGroup').val('');
        $(".es-list").empty();
        $('#guardComplianceandlicense_fileName1').text('None');
        $('#GuardComplianceandlicense_FileName1').val('');
        $('#GuardComplianceandlicense_CurrentDateTime').val('');
        clearGuardValidationSummary('compliancelicanseValidationSummary');
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
                d.guardId = $('#GuardComplianceandlicense_GuardId').val();
            },
            dataSrc: ''
        },
        columns: [

            { data: 'licenseNo', width: "10%" },
            //{ data: 'licenseTypeName', width: "5%" },
            {

                data: 'licenseTypeText',
                width: '5%',
                render: function (data, type, row) {
                    if (type === 'display') {
                        if (data === null) {
                            return row.licenseTypeName;
                        } else {
                            return data;
                        }
                    } else {
                        return data;
                    }
                }
            },
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

    //Gurad License and Compliance Form start
    let gridGuardLicensesAndLicence = $('#tbl_guard_licensesAndCompliance').DataTable({
        autoWidth: false,
        ordering: false,
        searching: false,
        paging: false,
        info: false,
        ajax: {
            url: '/Admin/GuardSettings?handler=GuardLicenseAndComplianceData',
            data: function (d) {
                d.guardId = $('#GuardComplianceandlicense_GuardId').val();
            },
            dataSrc: ''
        },
        columns: [
            { data: 'hrGroupText', width: "12%" },
            { data: 'description', width: "27%" },
            {
                data: 'expiryDate',
                width: '14%',
                orderable: true,

            },
            { data: 'fileName', width: '30%' },
            { data: 'status', width: "4%" },
            {
                targets: -1,
                data: null,
                defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_edit_guard_licenseAndCompliance"><i class="fa fa-pencil mr-2"></i>Edit</button>&nbsp;' +
                    '<button  class="btn btn-outline-danger" name="btn_delete_guard_licenseAndCompliance"><i class="fa fa-trash"></i></button>',
                width: '13%'
            }],
        columnDefs: [{
            targets: 3,
            data: 'fileName',
            render: function (data, type, row, meta) {
                if (data)
                    return '<a href="/Uploads/Guards/License/' + row.licenseNo + '/' + row.fileUrl + '" target="_blank">' + data + '</a>';
                return '-';
            },

        },
        {
            targets: 4,
            data: 'status',
            render: function (data, type, row, meta) {
                var currentDate = new Date();
                var ExpiryDate = new Date(row.expiryDate);
                var timeDifference = ExpiryDate - currentDate;
                var daysDifference = Math.ceil(timeDifference / (1000 * 60 * 60 * 24));
                var statusColor = 'green';


                if (row.dateType == true) {
                    statusColor = 'green';
                }
                else if (row.expiryDate != null) {
                    if (daysDifference <= 45) {
                        statusColor = 'yellow';
                    }

                    if (ExpiryDate < currentDate && row.dateType != true) {
                        statusColor = 'red';
                    }
                }


                return '<div style="display: flex; align-items: center; justify-content: center;"><div style="background-color:' + statusColor + '; width: 10px; height: 10px; border-radius: 50%;"></div></div>';
            }
        }

        ],
        'createdRow': function (row, data, index) {
            if (data.expiryDate !== null) {
                var formattedDate = getFormattedDate(new Date(data.expiryDate), null, ' ');
                if (data.dateType === true) {
                    formattedDate = formattedDate + '  (I)';
                    $('td', row).eq(2).html(formattedDate);
                }
                else {
                    $('td', row).eq(2).html(getFormattedDate(new Date(data.expiryDate), null, ' '));
                }

            }
        },
    });
    gridGuardLicensesAndLicence.on('draw.dt', function () {
        var tbody = $('#tbl_guard_licensesAndCompliance tbody');
        var rows = tbody.find('tr');
        var lastGroupValue = null;

        rows.each(function (index, row) {
            var currentGroupValue = $(row).find('td:eq(0)').text();

            if (currentGroupValue !== lastGroupValue) {
                lastGroupValue = currentGroupValue;

                var headerRow = $('<tr>').addClass('group-header').append($('<th>').attr('colspan', 6));
                headerRow.css('background-color', '#CCCCCC');
                $(row).before(headerRow);
            }
        });
    });
    //To get the data in description dropdown start
    $('#Description').attr('placeholder', 'Select');
    $('#Description').editableSelect({
        //filter: false,
        effects: 'slide'
    }).on('select.editable-select', function (e, li) {
        var check = '';
        $('#GuardComplianceandlicense_FileName1').val('');
        $('#guardComplianceandlicense_fileName1').text('None');
        var sel = $('#Description option:selected');
        $(this).data('cur_val', sel);
        var display = sel.clone();
        display.text($.trim(display.text()));
        sel.replaceWith(display);
        $(this).prop('selectedIndex', display.index());
    }).on(trig, function () {
        if ($(this).prop('selectedIndex') == 0)
            return;
        var sel = $('#Description option:selected');
        sel.replaceWith($('#Description').data('cur_val'));
        $(this).prop('selectedIndex', 0);
    });
    $('#HRGroup').on('change', function () {
        $('#Description').val('');
        $('#GuardComplianceandlicense_FileName1').val('');
        $('#guardComplianceandlicense_fileName1').text('None');
        var Descriptionval = $('#HRGroup').val();
        var GuardID = $('#GuardComplianceandlicense_GuardId').val();
        const token = $('input[name="__RequestVerificationToken"]').val();
        const ulClients = $('#Description').siblings('ul.es-list');
        ulClients.html('');
        $.ajax({
            url: '/Admin/GuardSettings?handler=HRDescription',
            type: 'GET',
            data: {
                HRid: Descriptionval,
                GuardID: GuardID
            },
            headers: { 'RequestVerificationToken': token }
        }).done(function (DescVal) {
            DescVal.forEach(function (DescVals) {
                var mark = ''; // Initialize the mark variable

                if (DescVals.description != null) {
                    if (DescVals.usedDescription == null) {
                        //mark = '❌';
                        mark = '<i class="fa fa-close" style="font-size:24px;color:red"></i>'
                        //ulClients.append('<li class="es-visible" value="' + DescVals.description + '">' + DescVals.referenceNo + '      ' + DescVals.description + ' ' + mark + '</li>');
                        ulClients.append('<li class="es-visible" value="' + DescVals.description + '" style="display: flex; justify-content: space-between; padding: 10px; border-bottom: 1px solid #ddd;">' +
                            '<span class="ref-no" style="flex: 1;">' + DescVals.referenceNo + ' </span>' +
                            '<span class="desc" style="flex: 2; margin-left: 10px;">' + DescVals.description + ' </span>' +
                            mark +
                            '</li>');
                    }
                    else {
                        mark = '<i class="fa fa-check" style="font-size:24px;color:green"></i>'
                        //mark = '<span style="color: green !important;">✔️</span>';
                        // ulClients.append('<li class="es-visible" value="' + DescVals.description + '">' + DescVals.referenceNo + '     ' + DescVals.description + ' ' + mark + '</li>');
                        ulClients.append('<li class="es-visible" value="' + DescVals.description + '" style="display: flex; justify-content: space-between; padding: 10px; border-bottom: 1px solid #ddd;">' +
                            '<span class="ref-no" style="flex: 1;">' + DescVals.referenceNo + ' </span>' +
                            '<span class="desc" style="flex: 2; margin-left: 10px;">' + DescVals.description + ' </span>' +
                            mark +
                            '</li>');
                    }
                }

            });
        })


    });



    var trig = 'mousedown';
    $('#Description').on("change", function () {
        var sel = $('#Description option:selected');
        $(this).data('cur_val', sel);
        var display = sel.clone();
        display.text($.trim(display.text()));
        sel.replaceWith(display);
        $(this).prop('selectedIndex', display.index());
    }).on(trig, function () {
        if ($(this).prop('selectedIndex') == 0)
            return;
        var sel = $('#Description option:selected');
        sel.replaceWith($('#Description').data('cur_val'));
        $(this).prop('selectedIndex', 0);
    });
    //To get the data in description dropdown stop
    //Gurad License and Compliance Form stop

    $('#btnAddGuardLicense').on('click', function () {
        resetGuardLicenseandComplianceAddModal();
        $("#ComplianceHiddenDiv").css({
            "pointer-events": "",
            "opacity": ""
        }).removeAttr("disabled");
        const messageHtml2 = '';
        $('#schRunStatusNew').html(messageHtml2);
        $('#addGuardCompliancesLicenseModal').modal('show');
    });
    /*code added for Licence Type Dropdown Textbox start*/
    $('#GuardLicense_LicenseType').attr('placeholder', 'Select Or Edit').editableSelect({
        effects: 'slide'
    }).on('select.editable-select', function (e, li) {
        $('#GuardLicense_License').val(li.text());
    });

    if ($('#GuardLicense_License').val() !== '') {
        let itemToSelect = $('#GuardLicense_LicenseType').siblings('.es-list').find('li:contains("' + $('#GuardLicense_License').val() + '")')[0];
        if (itemToSelect) {
            $('#GuardLicense_LicenseType').editableSelect('select', $(itemToSelect));

        }
    }
    /*code added for Licence Type Dropdown Textbox stop*/
    $('#tbl_guard_licenses tbody').on('click', 'button[name=btn_edit_guard_license]', function () {
        resetGuardLicenseAddModal();
        var data = gridGuardLicenses.row($(this).parents('tr')).data();
        $('#GuardLicense_LicenseNo').val(data.licenseNo);
        if (data.licenseTypeText == null) {
            $('#GuardLicense_LicenseType').val(data.licenseTypeName);
        }
        else {
            $('#GuardLicense_LicenseType').val(data.licenseTypeText);
        }

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
    //Gurad License and Compliance Form start
    $('#tbl_guard_licensesAndCompliance tbody').on('click', 'button[name=btn_edit_guard_licenseAndCompliance]', function () {
        resetGuardLicenseandComplianceAddModal();
        $("#ComplianceHiddenDiv").css({
            "pointer-events": "none",
            "opacity": "0.5"
        }).attr("disabled", "disabled");
        var data = gridGuardLicensesAndLicence.row($(this).parents('tr')).data();

        if (data.expiryDate) {
            $('#GuardComplianceAndLicense_ExpiryDate1').val(data.expiryDate.split('T')[0]);
        }
        $('#GuardComplianceandlicense_Id').val(data.id);
        $('#GuardComplianceandlicense_GuardId').val(data.GuardId);
        $('#HRGroup').val(data.hrGroup);
        $('#Description').val(data.description);
        $('#GuardComplianceandlicense_GuardId').val(data.guardId);
        $('#GuardComplianceandlicense_FileName1').val(data.fileName);
        $('#guardComplianceandlicense_fileName1').text(data.fileName ? data.fileName : 'None');
        if (data.dateType == true) {
            $('#LicanseTypeFilter').prop('checked', true);
            $('#ComplianceDate').text('Issue Date (DOI)');
            $('#IsDateFilterEnabledHidden').val(true);
        }
        $('#addGuardCompliancesLicenseModal').modal('show');

    });
    $('#tbl_guard_licensesAndCompliance tbody').on('click', 'button[name=btn_delete_guard_licenseAndCompliance]', function () {
        var data = gridGuardLicensesAndLicence.row($(this).parents('tr')).data();
        if (confirm('Are you sure want to delete this Guard License?')) {
            $.ajax({
                type: 'POST',
                url: '/Admin/GuardSettings?handler=DeleteGuardLicense',
                data: { 'id': data.id },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success)
                    gridGuardLicensesAndLicence.ajax.reload();
            })
        }
    });
    //Gurad License and Compliance Form stop


    //$('#upload_license_file').on('change', function () {
    //    const file = $(this).get(0).files.item(0);
    //    const fileExtn = file.name.split('.').pop();
    //    if (!fileExtn || 'jpg,jpeg,png,bmp,pdf'.indexOf(fileExtn) < 0) {
    //        alert('Please select a valid file type');
    //        return false;
    //    }

    //    const formData = new FormData();
    //    formData.append("file", file);
    //    formData.append('guardId', $('#GuardLicense_GuardId').val());

    //    $.ajax({
    //        type: 'POST',
    //        url: '/Admin/GuardSettings?handler=UploadGuardAttachment',
    //        data: formData,
    //        cache: false,
    //        contentType: false,
    //        processData: false,
    //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //    }).done(function (data) {
    //        $('#GuardLicense_FileName').val(data.fileName);
    //        $('#guardLicense_fileName').text(data.fileName ? data.fileName : 'None');
    //    }).fail(function () {
    //    }).always(function () {
    //        $('#upload_license_file').val('');
    //    });
    //});

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
        /*To get the text inside the product dropdown*/
        var inputElement = document.querySelector(".es-input");
        // Get the value of the input element
        if (inputElement) { var inputValue = inputElement.value; $('#LicenseTypeOther').val(inputValue); }
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
        $('#GuardCompliance_Reminder1').val('45');
        $('#GuardCompliance_Reminder2').val('7');
        $('#GuardCompliance_ExpiryDate').val('');
        $('#GuardCompliance_FileName').val('');
        $('#guardCompliance_fileName').text('None');
        $('#GuardCompliance_HrGroup').val('');
        clearGuardValidationSummary('complianceValidationSummary');
    }

    $('#btnAddGuardCompliance').on('click', function () {
        resetGuardLicenseandComplianceAddModal();

        //$('#addGuardCompliancesModal').modal('show');
        $('#addGuardCompliancesLicenseModal').modal('show');
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
                d.guardId = $('#GuardComplianceandlicense_GuardId').val();
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
            {
                data: 'fileName',
                render: function (data, type, row, meta) {
                    if (data)
                        var guardid = row.guardId;
                    return '<a href="/uploads/guards/' + guardid + '/' + data + '" target="_blank">' + data + '</a>';
                    return '-';
                },
                width: "10%"
            },

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
    $('#btn_save_guard_compliancelicense').on('click', function () {

        clearGuardValidationSummary('compliancelicanseValidationSummary');

        var ExpirayDateVal = $('#GuardComplianceAndLicense_ExpiryDate1').val();
        var HrVal = $('#HRGroup').val();
        var DescVal = $('#Description').val();
        var FileVa = $('#guardComplianceandlicense_fileName1').html();
        var hiddenValue = $('#GuardComplianceandlicense_FileName1').val();
        const messageHtml = '';
        $('#schRunStatusNew').html(messageHtml);

        if (HrVal != '' && DescVal != '' && FileVa != 'None') {

            if (ExpirayDateVal == '') {

                alert('Please Enter the Expiry Date or Date of issue');
                //if (confirm('Are you sure you not want to enter expiry Date')) {
                //    $('#schRunStatusNew').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Please wait...');
                //    $('#loader').show();
                //    $.ajax({
                //        url: '/Admin/GuardSettings?handler=SaveGuardComplianceandlicanse',
                //        data: $('#frm_add_complianceandlicense').serialize(),
                //        type: 'POST',
                //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                //    }).done(function (result) {
                //        if (result.status) {
                //            $('#addGuardCompliancesLicenseModal').modal('hide');
                //            const messageHtml1 = '';
                //            $('#schRunStatusNew').html(messageHtml1);
                //            gridGuardLicensesAndLicence.ajax.reload();

                //            if (!result.dbxUploaded) {
                //                displayGuardValidationSummary('compliancelicanseValidationSummary', 'Compliance details saved successfully. However, upload to Dropbox failed.');
                //            }
                //        } else {
                //            const messageHtml1 = '';
                //            $('#schRunStatusNew').html(messageHtml1);
                //            displayGuardValidationSummary('compliancelicanseValidationSummary', result.message);
                //        }
                //    }).always(function () {
                //        $('#loader').hide();
                //    });

                //}
            }
            else {
                $('#schRunStatusNew').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Please wait...');
                $('#loader').show();
                $.ajax({
                    url: '/Admin/GuardSettings?handler=SaveGuardComplianceandlicanse',
                    data: $('#frm_add_complianceandlicense').serialize(),
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.status) {
                        $('#addGuardCompliancesLicenseModal').modal('hide');
                        const messageHtml1 = '';
                        $('#schRunStatusNew').html(messageHtml1);
                        gridGuardLicensesAndLicence.ajax.reload();

                        if (!result.dbxUploaded) {
                            displayGuardValidationSummary('compliancelicanseValidationSummary', 'Compliance details saved successfully. However, upload to Dropbox failed.');
                        }
                    } else {
                        const messageHtml1 = '';
                        $('#schRunStatusNew').html(messageHtml1);
                        displayGuardValidationSummary('compliancelicanseValidationSummary', result.message);
                    }
                }).always(function () {
                    $('#loader').hide();
                });

            }
        }
        else {
            $('#schRunStatusNew').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i>Please wait...');
            $('#loader').show();
            $('#loader').show();
            $.ajax({
                url: '/Admin/GuardSettings?handler=SaveGuardComplianceandlicanse',
                data: $('#frm_add_complianceandlicense').serialize(),
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $('#addGuardCompliancesLicenseModal').modal('hide');
                    const messageHtml1 = '';
                    $('#schRunStatusNew').html(messageHtml1);
                    gridGuardLicensesAndLicence.ajax.reload();

                    if (!result.dbxUploaded) {
                        displayGuardValidationSummary('compliancelicanseValidationSummary', 'Compliance details saved successfully. However, upload to Dropbox failed.');
                    }
                } else {
                    const messageHtml1 = '';
                    $('#schRunStatusNew').html(messageHtml1);
                    displayGuardValidationSummary('compliancelicanseValidationSummary', result.message);
                }
            }).always(function () {
                $('#loader').hide();
            });

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

    //$('#upload_compliance_file').on('change', function () {
    //    const file = $(this).get(0).files.item(0);
    //    const fileExtn = file.name.split('.').pop();
    //    if (!fileExtn || 'jpg,jpeg,png,bmp,pdf'.indexOf(fileExtn) < 0) {
    //        alert('Please select a valid file type');
    //        return false;
    //    }

    //    const formData = new FormData();
    //    formData.append("file", file);
    //    formData.append('guardId', $('#GuardCompliance_GuardId').val());

    //    $.ajax({
    //        type: 'POST',
    //        url: '/Admin/GuardSettings?handler=UploadGuardAttachment',
    //        data: formData,
    //        cache: false,
    //        contentType: false,
    //        processData: false,
    //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //    }).done(function (data) {
    //        $('#GuardCompliance_FileName').val(data.fileName);
    //        $('#guardCompliance_fileName').text(data.fileName ? data.fileName : 'None');
    //    }).fail(function () {
    //    }).always(function () {
    //        $('#upload_compliance_file').val('');
    //    });
    //});

    $('#upload_complianceandlicanse_file').on('change', function () {
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
        var Desc = $('#Description').val();
        var expiryDate = $('#GuardComplianceAndLicense_ExpiryDate1').val();
        Desc = Desc.substring(3);
        var cleanText = Desc.replace(/[✔️❌]/g, '').trim();
        const formData = new FormData();
        formData.append("file", file);
        formData.append('guardId', $('#GuardComplianceandlicense_GuardId').val());
        formData.append('LicenseNo', $('#GuardComplianceandlicense_LicenseNo').val());
        formData.append('Description', cleanText);
        formData.append('HRID', $('#HRGroup').val());
        formData.append('ExpiryDate', $('#GuardComplianceAndLicense_ExpiryDate1').val());
        formData.append('DateType', $('#IsDateFilterEnabledHidden').val());
        if (Desc == '') {
            (confirm('Please select Description and Expiry/Issue Date'))
        }
        if (expiryDate == '') {
            (confirm('Please select the Expiry Date or Issue Date first, and then attach the document'))
        }
        else {
            fileprocess(allfile);

            $.ajax({
                type: 'POST',
                url: '/Admin/GuardSettings?handler=UploadGuardAttachment',
                data: formData,
                cache: false,
                contentType: false,
                processData: false,
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                $('#GuardComplianceandlicense_FileName1').val(data.fileName);
                $('#guardComplianceandlicense_fileName1').text(data.fileName ? data.fileName : 'None');
                $('#GuardComplianceandlicense_CurrentDateTime').val(data.currentDate);
            }).fail(function () {
            }).always(function () {
                $('#upload_complianceandlicanse_file').val('');
            });
        }

    }


    $("#LoginConformationBtnRC").on('click', function () {
        $('#txt_securityLicenseNoRC').val('');
        clearGuardValidationSummary('GuardLoginValidationSummaryRC');
        $("#modelGuardLoginConRc").modal("show");
        return false;
    });
    /*Show login confirmation for IR*/
    $("#LoginConformationBtnIR").on('click', function () {
        $('#txt_securityLicenseNoRC').val('');
        clearGuardValidationSummary('GuardLoginValidationSummaryRC');
        $("#modelGuardLoginConIR").modal("show");
        return false;
    });
    /*Show login confirmation for Patrol-start*/
    $("#LoginConformationBtnPatrols").on('click', function () {
        $('#txt_securityLicenseNoPatrols').val('');
        clearGuardValidationSummary('GuardLoginValidationSummaryRC');
        $("#modelGuardLoginConPatrol").modal("show");
        return false;
    });
    /*Show login confirmation for Patrol-end*/
    /* Check if Guard can access IR*/
    /* Check if Guard can access the IR-start */
    /*used for accessing the SecurityLicenseNumber if a guard is entering the incident report*/
    $('#btnGuardLoginIR').on('click', function () {
        const securityLicenseNo = $('#txt_securityLicenseNoIR').val();
        if (securityLicenseNo === '') {
            displayGuardValidationSummary('GuardLoginValidationSummaryIR', 'Please enter the security license No ');
        }
        else {



            /* $('#txt_securityLicenseNoIR').val('');*/


            $.ajax({
                url: '/Admin/GuardSettings?handler=GuardDetailsForRCLogin',
                type: 'POST',
                data: {
                    securityLicenseNo: securityLicenseNo,
                    type: 'IR'
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {

                if (result.accessPermission) {
                    /* $('#txt_securityLicenseNoIR').val('');*/
                    $('#modelGuardLoginConIR').modal('hide');

                    clearGuardValidationSummary('GuardLoginValidationSummaryIR');
                    window.location.href = '/Incident/Register';
                }
                else {

                    $('#txt_securityLicenseNo').val('');
                    /*$('#txt_securityLicenseNoIR').val('');*/
                    $('#modelGuardLoginConIR').modal('show');
                    if (result.successCode === 0) {
                        if (result.successMessage == 'Mobile is null') {
                            //var msg = '<b>The Control Room requires your personal mobile number in case of Emergency. It will only be used if we cannot contact you during your shift and you have not responded to a radio check OR call to the allocated site number.<p> This request occurs only once. Please donot provide false numbers to trick system. It is an OH&S requirement we can contact you in an emergency </p> </b>';
                            //new MessageModal({ message: "<b>The Control Room requires your personal mobile number in case of Emergency. It will only be used if we cannot contact you during your shift and you have not responded to a radio check OR call to the allocated site number.<p> This request occurs only once. Please donot provide false numbers to trick system. It is an OH&S requirement we can contact you in an emergency </p> </b>" }).showWarning();
                            //alert('<b>The Control Room requires your personal mobile number in case of Emergency. It will only be used if we cannot contact you during your shift and you have not responded to a radio check OR call to the allocated site number.<p> This request occurs only once. Please donot provide false numbers to trick system. It is an OH&S requirement we can contact you in an emergency </p> </b>')
                            /*alert(msg);*/
                            $('#alert-wand-in-use-modal').modal('show');
                        }
                        else {


                            displayGuardValidationSummary('GuardLoginValidationSummaryIR', result.successMessage);
                        }

                    }
                }
            });


            clearGuardValidationSummary('GuardLoginValidationSummaryIR');



        }
    });
    /* Check if Guard can access the IR-END */

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

                    /*window.location.href = '/Radio/Check';*/
                    window.location.href = 'http://rc.cws-ir.com/RadioCheckV2?Sl=' + securityLicenseNo + "&&lud=" + result.loggedInUserId + "&&guid=" + result.guId;
                     //window.location.href = 'https://localhost:7083/RadioCheckV2?Sl=' + securityLicenseNo + "&&lud=" + result.loggedInUserId + "&&guid=" + result.guId;
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
    /* p1-203 Admin User Profile -start */
    $("#LoginConformationBtnC4iSettings").on('click', function () {
        clearGuardValidationSummary('GuardLoginValidationSummary');
        $('#txt_securityLicenseNoC4iSettings').val('');
        $("#modelGuardLoginC4iSettingsPatrol").modal("show");
        return false;
    });
    /* p1-203 Admin User Profile -start */
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
    $('#delete_complianceandlicense_file').on('click', function () {
        const guardComplianceandlicenseId = $('#GuardComplianceandlicense_GuardId').val();
        if (!guardComplianceandlicenseId || parseInt(guardComplianceandlicenseId) <= 0)
            return false;

        if (confirm('Are you sure want to remove the attachment')) {
            $.ajax({
                url: '/Admin/GuardSettings?handler=DeleteGuardAttachment',
                type: 'POST',
                data: {
                    id: guardComplianceandlicenseId,
                    type: 'c'
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $('#GuardComplianceandlicense_fileName1').val('');
                    $('#guardComplianceandlicense_fileName1').text('None');
                    gridGuardCompliances.ajax.reload();
                }
                else {
                    displayGuardValidationSummary('compliancelicanseValidationSummary', 'Delete failed.');
                }
            });
        }
    });

    /* Check if Guard can access the patrol -start */
    /*used for accessing the SecurityLicenseNumber if a guard is entering the incident report*/
    $('#btnGuardLoginPatrols').on('click', function () {
        const securityLicenseNo = $('#txt_securityLicenseNoPatrols').val();
        if (securityLicenseNo === '') {
            displayGuardValidationSummary('GuardLoginValidationSummaryPatrols', 'Please enter the security license No ');
        }
        else {



            /* $('#txt_securityLicenseNoIR').val('');*/


            $.ajax({
                url: '/Admin/GuardSettings?handler=GuardDetailsForRCLogin',
                type: 'POST',
                data: {
                    securityLicenseNo: securityLicenseNo,
                    type: 'Patrols'
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.accessPermission) {
                    /* $('#txt_securityLicenseNoIR').val('');*/
                    $('#modelGuardLoginConPatrol').modal('hide');

                    clearGuardValidationSummary('GuardLoginValidationSummaryPatrols');
                    window.location.href = '/Reports/PatrolData';
                }
                else {

                    // $('#txt_securityLicenseNo').val('');
                    /*$('#txt_securityLicenseNoIR').val('');*/
                    $('#modelGuardLoginConPatrol').modal('show');
                    if (result.successCode === 0) {
                        displayGuardValidationSummary('GuardLoginValidationSummaryPatrols', result.successMessage);
                    }
                }
            });


            clearGuardValidationSummary('GuardLoginValidationSummaryIR');



        }
    });
    /* Check if Guard can access the IR-END */
    /*p1-203 Admin User Profile-start*/
    //to check whether the Guard can enter the settings
    $('#btnGuardLoginC4iSettings').on('click', function () {
        const securityLicenseNo = $('#txt_securityLicenseNoC4iSettings').val();
        if (securityLicenseNo === '') {
            displayGuardValidationSummary('GuardLoginValidationSummaryC4iSettings', 'Please enter the security license No ');
        }
        else {



            /* $('#txt_securityLicenseNoIR').val('');*/


            $.ajax({
                url: '/Admin/GuardSettings?handler=GuardDetailsForRCLogin',
                type: 'POST',
                data: {
                    securityLicenseNo: securityLicenseNo,
                    type: 'Settings'
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.accessPermission) {
                    /* $('#txt_securityLicenseNoIR').val('');*/
                    $('#modelGuardLoginC4iSettingsPatrol').modal('hide');

                    clearGuardValidationSummary('GuardLoginValidationSummaryC4iSettings');
                    window.location.href = '/Admin/Settings?Sl=' + securityLicenseNo + "&lud=" + result.loggedInUserId + "&guid=" + result.guId;
                }
                else {

                    // $('#txt_securityLicenseNo').val('');
                    /*$('#txt_securityLicenseNoIR').val('');*/
                    $('#modelGuardLoginC4iSettingsPatrol').modal('show');
                    if (result.successCode === 0) {
                        displayGuardValidationSummary('GuardLoginValidationSummaryC4iSettings', result.successMessage);
                    }
                }
            });


            clearGuardValidationSummary('GuardLoginValidationSummaryIR');



        }
    });
    /* p1 - 203 Admin User Profile - end*/
    /*for pushing notifications from the control room - start*/
    $('#pushNoTificationsGuardModal').on('shown.bs.modal', function (event) {
        /*timer pause while editing*/
        isPaused = true;

        const button = $(event.relatedTarget);
        const id = button.data('id');

        $('#chkAcknowledgeMessage').prop('checked', true);
        $('#chkMessageBack').prop('checked', false);
        $('#txtPushNotificationGuardReturnMessage').attr('disabled', 'disabled');
        $('#txtPushNotificationGuardReturnMessage').val('');
        clearGuardValidationSummary('PushNotificationsValidationSummary');
        $('#IsAcknowledgeMessage').val($('#chkAcknowledgeMessage').is(':checked'));
        $('#IsMessageBack').val($('#chkMessageBack').is(':checked'));

    });
    $("#pushNoTificationsGuardModal").on("hidden.bs.modal", function () {
        /*timer pause while editing*/
        isPaused = false;
    });
    $('#chkAcknowledgeMessage').on('change', function () {
        const isChecked = $(this).is(':checked');
        $('#IsAcknowledgeMessage').val(isChecked);
        if (isChecked == true) {
            $('#chkMessageBack').prop('checked', false);
            $('#IsMessageBack').val($('#chkMessageBack').is(':checked'));
            $('#txtPushNotificationGuardReturnMessage').attr('disabled', 'disabled');
            $('#txtPushNotificationGuardReturnMessage').val('');
        }
    });
    $('#chkMessageBack').on('change', function () {
        const isChecked = $(this).is(':checked');
        $('#IsMessageBack').val(isChecked);
        if (isChecked == true) {
            $('#chkAcknowledgeMessage').prop('checked', false);
            $('#IsAcknowledgeMessage').val($('#chkAcknowledgeMessage').is(':checked'));
            $('#txtPushNotificationGuardReturnMessage').attr('disabled', false);
        }
    });
    function clearGuardValidationSummary(validationControl) {
        $('#' + validationControl).removeClass('validation-summary-errors').addClass('validation-summary-valid');
        $('#' + validationControl).html('');
    }
    $('#btnSendPushLotificationFromGuardMessage').on('click', function () {
        const IsMessageBack = $('#chkMessageBack').is(':checked');
        const IsAcknowledgeMessage = $('#chkAcknowledgeMessage').is(':checked');

        var controlRoomMessage = $('#txtNotificationsControlRoomMessage').val();
        var Notifications = $('#txtPushNotificationGuardReturnMessage').val();

        if (IsMessageBack == true) {
            if (Notifications === '') {
                displayGuardValidationSummary('PushNotificationsValidationSummary', 'Please enter the Message to send ');
            }
            else {
                Notifications = controlRoomMessage + '</br>' + '    -  ' + Notifications;
            }
        }
        if (IsAcknowledgeMessage == true) {

            Notifications = controlRoomMessage + '</br>' + '     -  ' + 'ACKNOWLEDGED';

        }
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
            url: '/Guard/DailyLog?handler=SavePushNotificationTestMessages',
            type: 'POST',
            data: {
                guardLoginId: $('#GuardLog_GuardLoginId').val(),
                clientSiteLogBookId: $('#GuardLog_ClientSiteLogBookId').val(),
                Notifications: Notifications,
                rcPushMessageId: $('#rcPushMessageId').val(),
                tmdata: tmdata

            },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                $('#pushNoTificationsGuardModal').modal('hide');
                gridGuardLog.clear();
                gridGuardLog.reload();
            }
            else {
                displayGuardValidationSummary('PushNotificationsValidationSummary', data.message);
            }
            //$('#selectRadioStatus').val('');
            //$('#btnRefreshActivityStatus').trigger('click');
        });


    });





    var tId = 0;
    $("#duress_btn").mousedown(function () {
        if ($("#duress_status").text() !== "Active") {
            /*timer pause while editing*/
            isPaused = true;
            tId = setTimeout(GFG_Fun, 2500);

        }
        return false;
    });
    $("#duress_btn").mouseup(function () {
        clearTimeout(tId);
    });




    /*for touch devices Start */
    var touchTimer = 0;
    $('#duress_btn').on('touchstart', function (e) {
        // Prevent the default behavior
        e.preventDefault();

        if ($("#duress_status").text() !== "Active") {
            console.log('click');
            /*timer pause while editing*/
            isPaused = true;
            touchTimer = setTimeout(GFG_Fun, 2500);
            console.log(isPaused);
            console.log(touchTimer);
            gridGuardLog.clear();
            gridGuardLog.reload();
        }
        return false;
    });

    //$('#duress_btn').on('touchend', function () {
    console.log('stoped');
    // If there is any movement or the touch ends, clear the timer
    //clearTimeout(touchTimer);
    //isPaused = false;
    //});

    $('#duress_btn').on('pointerup', function (event) {
        // Your logic
        console.log('stoped2');
        clearTimeout(touchTimer);
        isPaused = false;
    });

    /*for touch devices end */

    /* Get Client Site duress Gps Rading Start*/


    function GFG_Fun() {
        if ($("#duress_status").text() !== "Active") {
            // Task p6#73_TimeZone issue -- added by Binoy - Start
            console.log('function');
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
                url: '/Guard/DailyLog?handler=SaveClientSiteDuress',
                data: {
                    clientSiteId: $('#GuardLog_ClientSiteLogBook_ClientSite_Id').val(),
                    guardLoginId: $('#GuardLog_GuardLoginId').val(),
                    logBookId: $('#GuardLog_ClientSiteLogBookId').val(),
                    guardId: $('#GuardLog_GuardLogin_GuardId').val(),
                    gpsCoordinates: $("#hid_duressEnabledGpsCoordinates").val(),
                    enabledAddress: $("#hid_duressEnabledAddress").val(),
                    tmdata: tmdata
                },
                dataType: 'json',
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $('#duress_btn').removeClass('normal').addClass('active');
                    $("#duress_status").addClass('font-weight-bold');
                    $("#duress_status").text("Active");
                    /*timer pause while editing*/
                    isPaused = false;
                }
                gridGuardLog.clear();
                gridGuardLog.reload();
                console.log(result.message);
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
            formData.append(modelname + ".EventDateTimeLocal", eventDateTimeLocal);
            formData.append(modelname + ".EventDateTimeLocalWithOffset", eventDateTimeLocalWithOffset);
            formData.append(modelname + ".EventDateTimeZone", tz);
            formData.append(modelname + ".EventDateTimeZoneShort", tzshrtnm);
            formData.append(modelname + ".EventDateTimeUtcOffsetMinute", diffTZ);
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


    function initialize() {
        var geocoder = new google.maps.Geocoder();
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function (position) {

                gpsCoordinatesValues = position.coords.latitude + ',' + position.coords.longitude;
                var latlng = new google.maps.LatLng(position.coords.latitude, position.coords.longitude);
                $("#hid_duressEnabledGpsCoordinates").val(gpsCoordinatesValues);
                $("#hid_duressEnabledGpsCoordinatesDailyLog").val(gpsCoordinatesValues);
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


    /*to get the warning - start*/
    $('#btn_confrim_wand_usok').on('click', function () {
        $('#alert-wand-in-use-modal').modal('hide')
    })


    //To Generate All PO List start
    $('#generate_log_AlldocketList').on('click', function () {
        $('#generate_kvl_docket_status').hide();
        $('#download_kvl_docket').hide();
        $('.print-docket-reason').prop('checked', false);
        $('#cbxProofOfDelivery').prop('checked', false);
        $('#cbxPOIList').prop('checked', true);
        $('#otherReason').val('');
        $('#otherReason').attr('disabled', true);
        $('#stakeholderEmail').val('');


        $('#generate_logbook_AlldocketList').show();
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

    $('#generate_logbook_AlldocketList').on('click', function () {
        $('#generate_kvl_docket_status').hide();

        const checkedReason = $('.print-docket-reason:checkbox:checked');
        if (checkedReason.length === 0) {
            $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger"></i> Please select a reason').show();
            return false;
        }
        $('#generate_kvl_docket_status').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Manual Docket. Please wait...').show();
        $('#download_kvl_docket').hide();
        $('#generate_log_AlldocketList').attr('disabled', true);

        var ids = [];
        //$.ajax({
        //    url: '/Admin/AuditSiteLog?handler=KeyVehicleLogProfiles',
        //    data: { truckRego: null, poi: 'POI' },
        //    type: 'GET',
        //    dataType: 'json',
        //    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        //}).done(function (result) { 
        //    var ids = [];
        //    result.forEach(function (item) {
        //        ids.push(item.detail.id);

        //    });
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
                $('#generate_kvl_docket_status').html('<i class="fa fa-times-circle text-danger mr-2"></i> Any POI not found for the login site').show();
            }
            else {

                $('#generate_log_AlldocketList').attr('disabled', false);
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







    /*});*/
    //To Generate All PO List stop

    $('#btncalendarEventModal').on('click', function () {
        $('#calendarEventModal').modal('show');
    });
    let calendarEventsDetails = $('#calendarEventsDetails').grid({
        dataSource: '/Radio/RadioCheckNew?handler=BroadcastCalendarEventsByDate',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { width: 130, field: 'id', title: 'Id', hidden: true },
            { width: 450, field: 'textMessage', title: 'Events' },
            { width: 100, field: 'formattedStartDate', title: 'Start' },
            { width: 100, field: 'formattedExpiryDate', title: 'Expiry' },
        ],

    });
    //let calendarEventsDetails = $('#calendarEventsDetails').DataTable({
    //    lengthMenu: [[10, 25, 50, 100, 1000], [10, 25, 50, 100, 1000]],
    //    ordering: true,
    //    info: false,
    //    searching: true,
    //    autoWidth: false,
    //    fixedHeader: false,
    //    "scrollY": "300px", // Set the desired height for the scrollable area
    //    "paging": false,
    //    "footer": true,
    //    ajax: {
    //        url: '/Radio/RadioCheckNew?handler=BroadcastCalendarEventsByDate',
    //        datatype: 'json',

    //        dataSrc: ''
    //    },
    //    columns: [
    //        { data: 'id', visible: false, title: 'id' },
    //        {
    //            data: 'textMessage',
    //            width: '20%',
    //           // title: 'Events'
    //        },
    //        {
    //            data: 'formattedStartDate',
    //            width: '20%',
    //          //  title: 'Start'

    //        },
    //        {
    //            data: 'formattedExpiryDate',
    //            width: '9%',
    //           // title: 'Expiry',

    //        },




    //    ],

    //});

    //calculate month difference-start

    function monthDiff(d1, d2) {
        var months;
        months = (d2.getFullYear() - d1.getFullYear()) * 12;
        months -= d1.getMonth();
        months += d2.getMonth();
        return months <= 0 ? 0 : months;
    }
    //calculate month difference-end

    /*to view thw audit log report-start*/
    $('#vehicle_key_log_audit_history').DataTable({
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
    $('#btnGenerateVklAuditLogReport').on('click', function () {
        if ($('#vklClientSiteId').val().length === 0) {
            alert('Please select a client site');
            return;
        }
        $('#KeyVehicleLogAuditLogRequest_ClientSiteId').val($('#vklClientSiteId').val());
        var item = $('#KeyVehicleLogAuditLogRequest_VehicleRego').val();
        var item2 = $('#KeyVehicleLogAuditLogRequest_PersonName').val();
        var item3 = $('#KeyVehicleLogAuditLogRequest_KeyNo').val();
        if (((item == null || item == '') && (item2 == null || item2 == '') && (item3 == null || item3 == ''))) {
            new MessageModal({ message: "<b>Please select any one of the 3 options<p></p><p>1. Vehicle Reg</p><p>2. Individual Name</p><p>3. Key No</p> </b>" }).showWarning();
        }
        else if ((item != '') && (item2 != '') && (item3 != '')) {
            new MessageModal({ message: "<b>Please select any one of the 3 options<p></p><p>1. Vehicle Reg</p><p>2. Individual Name</p><p>3. Key No</p> </b>" }).showWarning();
        }
        else if ((item != '') && (item2 != '')) {
            new MessageModal({ message: "<b>Please select any one of the 3 options<p></p><p>1. Vehicle Reg</p><p>2. Individual Name</p><p>3. Key No</p> </b>" }).showWarning();
        }
        else if ((item != '') && (item3 != '')) {
            new MessageModal({ message: "<b>Please select any one of the 3 options<p></p><p>1. Vehicle Reg</p><p>2. Individual Name</p><p>3. Key No</p> </b>" }).showWarning();
        }
        else if ((item2 != '') && (item3 != '')) {
            new MessageModal({ message: "<b>Please select any one of the 3 options<p></p><p>1. Vehicle Reg</p><p>2. Individual Name</p><p>3. Key No</p> </b>" }).showWarning();
        }
        else {
            $('#loader').show();
            $.ajax({
                // url: '/Admin/AuditSiteLog?handler=AuditHistory&vehicleRego=' + item,
                url: '/Admin/AuditSiteLog?handler=AuditHistory',
                type: 'GET',
                dataType: 'json',
                data: $('#form_kvl_auditlog_request').serialize(),
            }).done(function (response) {
                if (item != '') {
                    $('#vkl-auditlog-modal').find('#vkl-profile-title-rego').html('Truck Rego: ' + item);
                }
                if (item2 != '') {
                    $('#vkl-auditlog-modal').find('#vkl-profile-title-rego').html('Individual Name: ' + item2);
                }
                if (item3 != '') {
                    $('#vkl-auditlog-modal').find('#vkl-profile-title-rego').html('Key No: ' + item3);
                }
                $('#vkl-auditlog-modal').modal('show');
                $('#vehicle_key_log_audit_history').DataTable().clear().rows.add(response).draw();
                $('#loader').hide();
            });
        }
    });

    //let ActiveGuardsLogBookDetails = $('#ActiveGuardsLogBookDetails').DataTable({
    //    autoWidth: false,
    //    ordering: false,
    //    searching: false,
    //    paging: false,
    //    info: false,
    //    ajax: {
    //        url: '/Admin/GuardSettings?handler=LastTimeLogin',
    //        data: function (d) {
    //            d.guardId = $('#txtGuardId').val();

    //        },
    //        dataSrc: ''
    //    },
    //    columns: [

    //        {
    //            data: 'eventDateTime',
    //            width: "10%",
    //            render: function (data, type, row) {
    //                // Convert the date string to a JavaScript Date object
    //                var date = new Date(data);

    //                // Format the date to display only the date part without the time
    //                var formattedDate = date.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' });
    //                var additionalData = row.eventDateTimeZoneShort;
    //                if (additionalData != null) {
    //                    return formattedDate + ' (' + additionalData + ')';
    //                }
    //                else {
    //                    return formattedDate
    //                }

    //            }
    //        }

    //    ],


    //});
    /*to view thw audit log report-end*/

    //for toggle areas - start
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
            url: '/Admin/GuardSettings?handler=SaveToggleType',
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
            },
            headers: { 'RequestVerificationToken': token }
        }).done(function () {
            alert("Saved Successfully")
        }).fail(function () {
            console.log("error");
        });
    });

    function GetClientSiteToggle() {
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/GuardSettings?handler=ClientSiteToggle',
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

            }

        }).fail(function () {
            console.log("error");
        });
    }

    $('#LicanseTypeFilter').on('change', function () {
        const isChecked = $(this).is(':checked');

        const filter = isChecked ? 1 : 2;
        if (filter == 1) {
            $('#ComplianceDate').text('Issue Date (DOI)');
            $('#IsDateFilterEnabledHidden').val(true)
            $("#GuardComplianceAndLicense_ExpiryDate1").val('');
            $("#GuardComplianceAndLicense_ExpiryDate1").prop('max', function () {
                return new Date().toJSON().split('T')[0];
            });
            $("#GuardComplianceAndLicense_ExpiryDate1").prop('min', '');
        }
        if (filter == 2) {
            $('#IsDateFilterEnabledHidden').val(false)
            $('#ComplianceDate').text('Expiry Date (DOE)');
            $("#GuardComplianceAndLicense_ExpiryDate1").val('');
            $("#GuardComplianceAndLicense_ExpiryDate1").prop('min', function () {
                return new Date().toJSON().split('T')[0];
            });
            $("#GuardComplianceAndLicense_ExpiryDate1").prop('max', '');
        }

    });
    //for toggle areas - start


    $('#togglePassword').on('click', function () {
        // Get the password field
        var passwordField = $('#Guard_Pin');
        // Get the current type of the password field
        var passwordFieldType = passwordField.attr('type');
        // Toggle the type attribute
        if (passwordFieldType === 'password') {
            passwordField.attr('type', 'text');
            $(this).html('<i class="fa fa-eye-slash" aria-hidden="true"></i>'); // Change icon to a closed eye
        } else {
            passwordField.attr('type', 'password');
            $(this).html('<i class="fa fa-eye" aria-hidden="true"></i>'); // Change icon to an open eye
        }
    });



    //Start fusion report in auditlog08072024
    $('#fusionAudtitFromDate').val(start.toISOString().substr(0, 10));
    // var systemDate = $('#fusionAudtitToDate').val();
    //var dateObject = new Date().toISOString().substr(0, 10);
    $('#fusionAudtitToDate').val(systemDate);

    let gridsitefusionLog;
    gridsitefusionLog = $('#fusion_site_log').grid({
        dataSource: '/Admin/AuditSiteLog?handler=DailyGuardFusionSiteLogs',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        grouping: { groupBy: 'Date' },
        primaryKey: 'id',
        columns: [
            { field: 'clientSiteId', hidden: true },
            { field: 'notificationCreatedTime', title: 'Time', width: 100, renderer: function (value, record) { return renderDateTimefusion(value, record, false); } },
            {
                field: 'notes', title: 'Event / Notes', width: 350,
                renderer: function (value, record) {
                    var activityType = record.activityType == undefined ? '' : record.activityType.trim();
                    if (activityType == 'IR') {

                        return record.notes + '<br>' + '<a href="https://c4istorage1.blob.core.windows.net/irfiles/' + record.notes.substr(0, 8) + '/' + record.notes + '" target="_blank">Click here</a>';
                    }
                    else {
                        return record.notes;
                    }
                }
            },
            { field: 'activityType', title: 'Source', width: 50 },
            { field: 'guardName', title: 'Guard Initials', width: 150, renderer: renderGuardInitialColumn }
        ],
        paramNames: { page: 'pageNo' },
        pager: { limit: 100, sizes: [10, 50, 100, 500] }
    });


    $('#fusionClientSiteId').select2({
        placeholder: 'Select',
        theme: 'bootstrap4'
    });

    $('#fusionClientSiteId').on('change', function () {
        gridsitefusionLog.clear();
    });

    $('#fusionClientType').on('change', function () {
        gridsitefusionLog.clear();
        const clientTypeId = $(this).val();
        const clientSiteControl = $('#fusionClientSiteId');
        clientSiteControl.html('');
        $.ajax({
            url: '/Admin/Settings?handler=ClientSites&typeId=' + clientTypeId,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                $('#fusionClientSiteId').append(new Option('Select', '', true, true));
                data.map(function (site) {
                    $('#fusionClientSiteId').append(new Option(site.name, site.id, false, false));
                });


            }
        });


    });




    $('#expand_fusion_audits').on('click', function () {
        gridsitefusionLog.expandAll();
    });

    $('#collapse_fusion_audits').on('click', function () {
        gridsitefusionLog.collapseAll();
    });



    function renderDateTimefusion(value, record) {
        // p6#73 timezone bug - Modified by binoy 29-01-2024
        if (record.eventDateTime != null && record.eventDateTime != '') {
            const date = new Date(record.eventDateTime);
            var DateTime = luxon.DateTime;
            var dt1 = DateTime.fromJSDate(date);
            var dt = dt1.toFormat('dd LLL yyyy @ HH:mm') + ' Hrs ' + record.eventDateTimeZoneShort;
            return dt;
        }
        else if (value !== '') {
            const date = new Date(value);
            const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
            let day = date.getDate();

            if (day < 10) {
                day = '0' + day;
            }

            return day + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
        }
    }

    if (gridsitefusionLog) {
        const bg_color_pale_yellow = '#fcf8d1';
        const bg_color_pale_red = '#ffcccc';
        const bg_color_white = '#ffffff';
        const irEntryTypeIsAlarm = 2;

        gridsitefusionLog.on('rowDataBound', function (e, $row, id, record) {
            let rowColor = bg_color_white;
            if (record.irEntryType) {
                rowColor = record.irEntryType === irEntryTypeIsAlarm ? bg_color_pale_red : bg_color_pale_yellow;
            }
            $row.css('background-color', rowColor);
        });
    }


    $('#btnGeneratefusionAuditReport').on('click', function () {

        if ($('#fusionClientSiteId').val() === '') {
            alert('Please select a client site');
            return;
        }
        gridsitefusionLog.clear();
        gridsitefusionLog.reload({
            clientSiteId: $('#fusionClientSiteId').val(),
            logFromDate: $('#fusionAudtitFromDate').val(),
            logToDate: $('#fusionAudtitToDate').val(),
            excludeSystemLogs: 0
        });
    });


    let logBookTypeForAuditZipfusion;
    $('#btnDownloadfusionAuditZip').on('click', function () {

        logBookTypeForAuditZipfusion = 1;
        if ($('#fusionClientSiteId').val() === '') {
            alert('Please select a client site');
            return;
        }
        $('#auditfusionlog-zip-modal').modal('show');
    });


    $('#auditfusionlog-zip-modal').on('show.bs.modal', function (event) {
        $('#btn-auditfusionlog-zip-download').attr('href', '#');
        $('#btn-auditfusionlog-zip-download').hide();
        $('#auditfusionlog-zip-msg').show();

        if (logBookTypeForAuditZipfusion === 1)
            downloadDailyGuardfusionLogZipFile();

    });



    function downloadDailyGuardfusionLogZipFile() {
        $.ajax({
            url: '/Admin/AuditSiteLog?handler=DownloadDailyFusionGuardLogZip',
            type: 'POST',
            dataType: 'json',
            data: {
                clientSiteId: $('#fusionClientSiteId').val(),
                logFromDate: $('#fusionAudtitFromDate').val(),
                logToDate: $('#fusionAudtitToDate').val()
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            if (!response.success) {
                $('#auditfusionlog-zip-modal').modal('hide');
                new MessageModal({ message: 'Failed to generate zip file. ' + response.message }).showError();
            } else {
                $('#btn-auditfusionlog-zip-download').attr('href', response.fileName);
                $('#btn-auditfusionlog-zip-download').show();
                $('#auditfusionlog-zip-msg').hide();
            }
        });
    }

    //end fusion report in auditlog08072024

    /* ##### Download Log Audit Start ##### */

    var currdate = new Date();
    $('#dwnlogAuditFromDate').val(currdate.toISOString().substring(0, 10));
    $('#dwnlogAuditToDate').val(currdate.toISOString().substring(0, 10));

    let tbldownloadlogaudit = $('#tbl_downloadlog_audit').DataTable({
        dom: 'lBfrtip',
        buttons: [
            //{
            //    extend: 'copy',
            //    text: '<i class="fa fa-copy"></i>',
            //    titleAttr: 'Copy',
            //    className: 'btn btn-md mr-2 btn-copy'
            //},            
            {
                extend: 'excel',
                text: '<i class="fa fa-file-excel-o"></i>',
                titleAttr: 'Excel',
                className: 'btn btn-md mr-2 btn-excel',
                title: 'Files download/viewed audit logs'
            },
            {
                extend: 'pdf',
                text: '<i class="fa fa-file-pdf-o"></i>',
                titleAttr: 'PDF',
                className: 'btn btn-md mr-2 btn-pdf',
                /*orientation: 'landscape',*/
                pageSize: 'A4',
                /*messageTop: 'Files download/viewed audit logs.',*/
                /*download: 'open',*/
                title: 'Files download/viewed audit logs'
            },
            //{
            //    extend: 'print',
            //    text: '<i class="fa fa-print"></i>',
            //    titleAttr: 'Print',
            //    className: 'btn btn-md mr-2 btn-print'
            //}
        ],
        lengthMenu: [[75, 100, -1], [75, 100, "All"]],
        pageLength: 100,
        hidden: true,
        paging: true,
        ordering: false,
        order: [[groupColumn, 'asc']],
        info: true,
        searching: true,
        autoWidth: false,
        scrollX: true,
        data: [],
        columns: [
            { data: 'id', title: 'Event ID', visible: false },
            { data: 'user.userName', title: 'User', width: "10%" },
            { data: 'guard.name', title: 'Guard Name', width: "15%" },
            { data: 'guard.securityNo', title: 'Security Number', width: "10%" },
            { data: 'ipAddress', title: 'IP Address', width: "10%" },
            { data: 'dwnlCatagory', title: 'Categories', width: "15%" },
            { data: 'dwnlFileName', title: 'File Name', width: "20%" },
            {
                data: 'eventDateTime', title: 'Download/View Time', width: "20%", 'render': function (value) {
                    const date = new Date(value);
                    var DateTime = luxon.DateTime;
                    var dt1 = DateTime.fromJSDate(date);
                    var dt = dt1.toFormat('dd LLL yyyy @ HH:mm') + ' Hrs'; // + record.eventDateTimeZoneShort;
                    return dt;
                }
            },
        ],
        drawCallback: function () {
            var api = this.api();
            var rows = api.rows({ page: 'current' }).nodes();
            var last = null;

            //api.column(groupColumn, { page: 'current' })
            //    .data()
            //    .each(function (group, i) {
            //        if (last !== group) {
            //            $(rows)
            //                .eq(i)
            //                .before('<tr class="group bg-light text-dark"><td colspan="25">' + group + '</td></tr>');

            //            last = group;
            //        }
            //    });
        },
    });

    $("#btnGenerateDwnlog").on('click', function () {
        //calculate month difference-start
        var date1 = new Date($('#dwnlogAuditFromDate').val());
        var date2 = new Date($('#dwnlogAuditToDate').val());
        if (date1 > date2) {
            alert('From date cannot be before To date.')
            return false;
        }
        $('#loader').show();
        $.ajax({
            url: '/Admin/AuditSiteLog?handler=GenerateDownloadFilesLog',
            type: 'POST',
            dataType: 'json',
            data: {
                logFromDate: $('#dwnlogAuditFromDate').val(),
                logToDate: $('#dwnlogAuditToDate').val()
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            $('#loader').hide();
            tbldownloadlogaudit.clear().rows.add(response).draw();
        }).always(function () {
            $('#loader').hide();
        });

    });
    //code added  to download Excel start for Download Log Audit-end


    /* ##### Download Log Audit End ##### */


});

$('#btnTimesheetConfirm').on('click', function () {
    $('#AuthGuardForSopDwnldValidationSummary1').html('');

    var guardLicNo = $('#GuardDownloadSop_SecurityNo').val();


    $.ajax({
        url: '/Admin/Roster?handler=CheckAndCreateDownloadAuditLog1',
        type: 'POST',
        data: {
            guardLicNo: guardLicNo
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.success) {

            $('#mdlAuthGuardForSopDownload').modal('hide');
            $('#TimesheetGuard_Id1').val('-1');
            $('#startDateRoster').val('');
            $('#endDateRoster').val('');
            $('#frequency').val('');
            $('#timesheetModal').modal('show');
            $.ajax({
                url: '/Admin/Roster?handler=GuardID&LicenseNo=' + guardLicNo,
                type: 'GET',
                dataType: 'json',
                success: function (data) {
                    $('#TimesheetGuard_Id1').val(data);

                }
            });
        } else {
            console.log('Error: ', result.message);
            $('#AuthGuardForSopDwnldValidationSummary1').html(result.message);
        }
    }).always(function () {
        $('#loader').hide();
    });
});


$('#btnDownloadTimesheetFrequencyRoster').on('click', function (e) {
    var Frequency = $('#frequency').val();

    if (!Frequency) {
        alert("Please select Instant Timesheet.");
        return; // Exit the function if validation fails
    }




    $.ajax({
        url: '/Admin/Settings?handler=DownloadTimesheetFrequency',
        data: {
            frequency: $('#frequency').val(),
            guradid: $('#TimesheetGuard_Id1').val(),
        },
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        if (response.statusCode === -1) {

        } else {





            var newTab = window.open(response.fileName, '_blank');
            if (!newTab) {

                var a = document.createElement('a');
                a.href = response.fileName;
                a.download = "TimeSheet_Report";
                a.click();
            }

        }
    });
});
$('#btnDownloadTimesheetRoster').on('click', function (e) {
    var startDate1 = $('#startDateRoster').val();
    var endDate1 = $('#endDateRoster').val();
    var ddd = $('#TimesheetGuard_Id1').val();
    // Check if both startDate and endDate have values
    if (!startDate1 || !endDate1) {
        alert("Please select both start date and end date.");
        return; // Exit the function if validation fails
    }




    $.ajax({
        url: '/Admin/Settings?handler=DownloadTimesheet',
        data: {
            startdate: $('#startDateRoster').val(),
            endDate: $('#endDateRoster').val(),
            frequency: $('#frequency').val(),
            guradid: $('#TimesheetGuard_Id1').val(),
        },
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        if (response.statusCode === -1) {

        } else {





            var newTab = window.open(response.fileName, '_blank');
            if (!newTab) {

                var a = document.createElement('a');
                a.href = response.fileName;
                a.download = "TimeSheet_Report";
                a.click();
            }

        }
    });
});

$('#btnDownloadTimesheetFrequencyBulk').on('click', function (e) {
    var Frequency = $('#frequency').val();

    if (!Frequency) {
        alert("Please select Instant Timesheet.");
        return; // Exit the function if validation fails
    }
    $('#auditTimesheetlog-zip-modal').modal('show');
    downloadDailyGuardTimeSheetLogZipFile();

});
$('#btnDownloadTimesheetBulk').on('click', function (e) {
    var startDate1 = $('#startDateTimesheetBulk').val();
    var endDate1 = $('#endDateTimesheetBulk').val();
    var ddd = $('#TimesheetGuard_Id1').val();
    // Check if both startDate and endDate have values
    if (!startDate1 || !endDate1) {
        alert("Please select both start date and end date.");
        return; // Exit the function if validation fails
    }
    $('#auditTimesheetlog-zip-modal').modal('show');
    downloadDailyGuardTimeSheetLogBulkZipFile();
});
function downloadDailyGuardTimeSheetLogZipFile() {
    // Get selected client site IDs from multi-checkbox dropdown
    var selectedClientSiteIds = $('#vklClientSiteIdTimesheet').val(); // This will get the selected IDs as an array
    var ddd = selectedClientSiteIds.join(',');
    if (!selectedClientSiteIds || selectedClientSiteIds.length === 0) {
        new MessageModal({ message: 'Please select at least one client site.' }).showError();
        return;
    }
    
    $.ajax({
        url: '/Admin/AuditSiteLog?handler=DownloadDailyTimesheetLogZip',
        type: 'POST',
        dataType: 'json',
        data: {
            clientSiteId: selectedClientSiteIds.join(','), // Send as a comma-separated string
            frequency: $('#frequency').val(),
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        if (!response.success) {
            $('#auditTimesheetlog-zip-modal').modal('hide');
            new MessageModal({ message: 'Failed to generate zip file. ' + response.message }).showError();
        } else {
            $('#btn-auditTimesheetlog-zip-download').attr('href', response.fileName);
            $('#btn-auditTimesheetlog-zip-download').show();
            $('#auditTimesheetlog-zip-msg').hide();
        }
    });
}
function downloadDailyGuardTimeSheetLogBulkZipFile() {
    // Get selected client site IDs from multi-checkbox dropdown
    var selectedClientSiteIds = $('#vklClientSiteIdTimesheet').val(); // This will get the selected IDs as an array
    var ddd = selectedClientSiteIds.join(',');
    if (!selectedClientSiteIds || selectedClientSiteIds.length === 0) {
        new MessageModal({ message: 'Please select at least one client site.' }).showError();
        return;
    }

    $.ajax({
        url: '/Admin/AuditSiteLog?handler=DownloadTimesheetBulk',
        type: 'POST',
        dataType: 'json',
        data: {
            clientSiteId: selectedClientSiteIds.join(','), // Send as a comma-separated string
            startdate: $('#startDateTimesheetBulk').val(),
            endDate: $('#endDateTimesheetBulk').val(),
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        if (!response.success) {
            $('#auditTimesheetlog-zip-modal').modal('hide');
            new MessageModal({ message: 'Failed to generate zip file. ' + response.message }).showError();
        } else {
            $('#btn-auditTimesheetlog-zip-download').attr('href', response.fileName);
            $('#btn-auditTimesheetlog-zip-download').show();
            $('#auditTimesheetlog-zip-msg').hide();
        }
    });
}