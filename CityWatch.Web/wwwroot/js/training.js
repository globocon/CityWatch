$(function () {
    
});
//p5-Issue3-start
$('#btnCourse').on('click', function (e) {
    e.preventDefault();

    $('#trainingandAssessmentmodal').modal('show');
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var courseDescription = $('#txtHrSettingsDescription').val();
    $('#training_course_Name').html('HR' + ' ' + referenceNumber + ' ' + courseDescription)

    gridCourseDocumentFiles.clear();
    gridCourseDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
    $('#btn_save_trainingassessment_settings').attr('hidden', true);
    $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
    $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
    $('#btn_delete_trainingassessment_testquestions').attr('hidden', true);
    $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', true);
    LoadTQSettings();
    LoadLastTQNumbers();
    GetNumberOfQuestions();
    LoadLastFeedbackQNumbers();
    GetNumberOfFeedbackQuestions();
    gridCourseInstructorDetails.clear();
    gridCourseInstructorDetails.reload({ type: $('#HrSettings_Id').val() });
    gridCertificatesDocumentFiles.clear();
    gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
});

$('#btnTestQuestions').on('click', function (e) {
    e.preventDefault();
    $('#trainingandAssessmentmodal').modal('show');
    $('#trainingCourseTab').removeClass('active');
    $('#trainingTestQuestionstab').addClass('active');
    $('#trainingCertificateTab').removeClass('active');
    $('#TrainingCertificate').removeClass('active');
    $('#TrainingCourse').removeClass('active');
    $('#TrainingTestQuestions').addClass('active');
    $('#TrainingTestQuestionsSettingsTab').addClass('active');
    $('#TrainingTestQuestionsAnswersTab').removeClass('active');
    $('#TrainingTestQuestionsFeedbackTab').removeClass('active');
    $('#TrainingTestQuestionsSettings').addClass('active');
    $('#TrainingTestQuestionsAnswers').removeClass('active');
    $('#TrainingTestQuestionsFeedback').removeClass('active');

    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var courseDescription = $('#txtHrSettingsDescription').val();
    $('#training_course_Name').html('HR' + ' ' + referenceNumber + ' ' + courseDescription)
    gridCourseDocumentFiles.clear();
    gridCourseDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
    $('#btn_save_trainingassessment_settings').attr('hidden', false);
    $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
    $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
    $('#btn_delete_trainingassessment_testquestions').attr('hidden', true);
    $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', true);
    LoadTQSettings();
    LoadLastTQNumbers();
    GetNumberOfQuestions();
    LoadLastFeedbackQNumbers();
    GetNumberOfFeedbackQuestions();
    gridCourseInstructorDetails.clear();
    gridCourseInstructorDetails.reload({ type: $('#HrSettings_Id').val() });
    gridCertificatesDocumentFiles.clear();
    gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
});
$('#btnCourseCertificates').on('click', function (e) {
    e.preventDefault();
    $('#trainingandAssessmentmodal').modal('show');
    $('#trainingCourseTab').removeClass('active');
    $('#trainingCertificateTab').addClass('active');
    $('#TrainingCourse').removeClass('active');
    $('#TrainingCertificate').addClass('active');
    $('#trainingCertificateUploadtab').addClass('active');
    $('#TrainingCertificateUpload').addClass('active');
    $('#trainingCourseInstructorUploadtab').removeClass('active');
    $('#TrainingCourseInstructorUpload').removeClass('active');
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var courseDescription = $('#txtHrSettingsDescription').val();
    $('#training_course_Name').html('HR' + ' ' + referenceNumber + ' ' + courseDescription)
    gridCourseDocumentFiles.clear();
    gridCourseDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
    $('#btn_save_trainingassessment_settings').attr('hidden', true);
    $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
    $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
    $('#btn_delete_trainingassessment_testquestions').attr('hidden', true);
    $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', true);
    LoadTQSettings();
    LoadLastTQNumbers();
    GetNumberOfQuestions();
    LoadLastFeedbackQNumbers();
    GetNumberOfFeedbackQuestions();
    gridCourseInstructorDetails.clear();
    gridCourseInstructorDetails.reload({ type: $('#HrSettings_Id').val() });
    gridCertificatesDocumentFiles.clear();
    gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
});
$('#btnTrainingAssesmentModalClose').on('click', function (e) {
    e.preventDefault();
    $('#trainingCourseTab').addClass('active');
    $('#trainingCertificateTab').removeClass('active');
    $('#TrainingCourse').addClass('active');
    $('#TrainingCertificate').removeClass('active');
    $('#trainingTestQuestionstab').removeClass('active');
    $('#TrainingTestQuestions').removeClass('active');
    $('#trainingandAssessmentmodal').hide();
    GetNumberOfQuestions();

});
var editTrainingCourseDocsButtonRendererSop;
editTrainingCourseDocsButtonRendererSop = function (value, record, $cell, $displayEl, id, $grid) {
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var hrreferenceNumber = 'HR' + referenceNumber;
    var data = $grid.data(),
        $replace = $('<label class="btn btn-success mb-0"><form id="form_file_downloads_course_sop" method="post"><i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_course_file_sop" accept=".pdf, .ppt, .pptx, .mp4" hidden data-doc-id="' + record.id + '" tq-id="' + record.tqNumberId + '">' +
            '</form></label>').attr('data-key', id),
        $downlaod = $('<a href="/TA/' + hrreferenceNumber +'/Course/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>').attr('data-key', id),
        $edit = $('<button class="btn btn-outline-primary ml-2"><i class="gj-icon pencil" style="font-size:15px"></i></button>').attr('data-key', id),
        $delete = $('<button type="button" class="btn btn-outline-danger ml-2 delete_course_file_sop" data-doc-id="' + record.id + '"><i class="fa fa-trash"></i></button>').attr('data-key', id),
        $update = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-check" aria-hidden="true"></i></button>').attr('data-key', id).hide(),
        $cancel = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-close" aria-hidden="true"></i></button>').attr('data-key', id).hide();
    $edit.on('click', function (e) {
        $grid.edit($(this).data('key'));
        $edit.hide();
        $delete.hide();
        $update.show();
        $cancel.show();
        // DisableTQColumn();
        let rowCount = $('#tbl_courseDocumentFiles tbody tr').length;

        let tqColumn = $(this).closest('tr').find('td').eq(2);
        let tqInput = tqColumn.find('select');
        let tqInputNew = tqColumn.find('button');
        if (rowCount === 1) {
            /* tqInput.prop('disabled', true); // disable input*/

            //tqColumn.hide();
            //$('#tbl_courseDocumentFiles thead th:eq(2)').hide();
            tqInput.attr('disabled', 'disabled');
            tqInputNew.attr('disabled', 'disabled');
        }
        //else {
        //    //tqInput.prop('disabled', false); // enable input if more than one row
        //    tqInput.attr('data-mode', 'edit');
        //}
    });
    $delete.on('click', function (e) {
        $grid.removeRow($(this).data('key'));
    });
    $update.on('click', function (e) {
        $grid.update($(this).data('key'));
        $edit.show();
        $delete.show();
        $update.hide();
        $cancel.hide();
    });
    $cancel.on('click', function (e) {
        $grid.cancel($(this).data('key'));
        $edit.show();
        $delete.show();
        $update.hide();
        $cancel.hide();
    });
    $displayEl.empty().append($replace).append($downlaod).append($edit).append($delete).append($update).append($cancel);
}
function DisableTQColumn() {
    let rowCount = $('#tbl_courseDocumentFiles tbody tr').length;
    let tqInput = $(this).closest('tr').find('td').eq(2);
    if (rowCount === 1) {
        /* tqInput.prop('disabled', true); // disable input*/
        tqInput.attr('data-mode', 'display');
    } else {
        //tqInput.prop('disabled', false); // enable input if more than one row
        tqInput.attr('data-mode', 'edit');
    }

}
var editCourseTQNumber;
editCourseTQNumber = function (value, record) {
    // var select = $('<button class="btn btn-primary" id="generate_kvl_docket">Generate Docket</button>');
    var select = $('<select class="form-control mx-1 "  id="CourseTQNumber"></select>');
    $.ajax({
        url: '/Admin/Settings?handler=TQNumbers',
        // data: { id: record },
        //type: 'POST',
        //headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        for (var i = 0; i < result.length; i++) {
            //select.valueField = result[i].name;
            //select.textField = result[i].name;
            var newoption = '<option value= "' + result[i].id + '" > ' + result[i].name + '</option >';
            if (result[i].id == record.tqNumberId) {

                select.append(newoption).attr("selected", "selected");;
            }
            else {
                select.append(newoption)
            }
        }

        select.val(record.tqNumberId);



        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })

    return select;


}

let gridCourseDocumentFiles = $('#tbl_courseDocumentFiles').grid({
    dataSource: '/Admin/Settings?handler=CourseDocsUsingSettingsId',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command', managementColumn: false },
    columns: [
        { field: 'fileName', title: 'File Name', width: 390 },
        { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 140 },
       { width: 75, field: 'tqNumberName', title: 'TQ', align: 'center', type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=TQNumbers', valueField: 'name', textField: 'name' } },
       // { width: 75, field: 'tqNumberId', title: 'TQ', align: 'center', renderer: editCourseTQNumber },
        // { width: 200, renderer: staffDocsButtonRendererCompanySop },

        { width: 270, renderer: editTrainingCourseDocsButtonRendererSop },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        
    }
});


if (gridCourseDocumentFiles) {
    gridCourseDocumentFiles.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: '/Admin/Settings?handler=UpdateDocumentTQNumber',
            data: { id: data.id, name: data.tqNumberName, record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function (result) {

            if (result.success) {
                alert('Updated Successfully');
                gridCourseDocumentFiles.clear();
                gridCourseDocumentFiles.reload();
            } else {

                alert(result.message);
                gridCourseDocumentFiles.clear();
                gridCourseDocumentFiles.reload();
            }
        }).fail(function () {
            console.log('error');
        }).always(function () {

        });
    });

}

$('#tbl_courseDocumentFiles').on('change', 'input[name="upload_course_file_sop"]', function () {
    uploadCourseDocUsingHR($(this), true, 1);
});
$('#tbl_courseDocumentFiles').on('click', '.delete_course_file_sop', function () {
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var hrreferenceNumber = 'HR' + referenceNumber;
    if (confirm('Are you sure want to delete this file?')) {
        $.ajax({
            url: '/Admin/Settings?handler=DeleteCourseDocUsingHR',
            data: {
                id: $(this).attr('data-doc-id'),
                hrreferenceNumber: hrreferenceNumber
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                gridCourseDocumentFiles.clear();
                gridCourseDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
            }
        }).fail(function () {
            console.log('error')
        });
    }
})

function domainRPLSettings(value, record) {

    // Check if the row is a newly added row by ID or another condition
    if (record.id === -1) {
        // Skip rendering the checkbox for new rows
        return '';
    }

    if (record.isRPLEnabled) {

        return '<a href="#" id="btnRPLCertificate"><input type="checkbox" id="chkIsRPL" checked></a><input type="hidden" id="certificateId" value="' + record.id + '">'

    }
    else {
        return '<a href="#" id="btnRPLCertificate"><input type="checkbox" id="chkIsRPL"></a><input type="hidden" id="certificateId" value="' + record.id + '">'
    }

}
var editTrainingCertificatesDocsButtonRendererSop;
editTrainingCertificatesDocsButtonRendererSop = function (value, record, $cell, $displayEl, id, $grid) {
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var hrreferenceNumber = 'HR' + referenceNumber;
    var data = $grid.data(),
        $replace = $('<label class="btn btn-success mb-0"><form id="form_file_downloads_certificate_sop" method="post"><i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_certificate_file_sop" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
            '</form></label>').attr('data-key', id),
        $downlaod = $('<a href="/TA/' + hrreferenceNumber + '/Certificate/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>').attr('data-key', id),
        $edit = $('<button class="btn btn-outline-primary ml-2"><i class="gj-icon pencil" style="font-size:15px"></i></button>').attr('data-key', id),
        $delete = $('<button type="button" class="btn btn-outline-danger ml-2 delete_certificate_file_sop" data-doc-id="' + record.id + '"><i class="fa fa-trash"></i></button>').attr('data-key', id),
        $update = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-check" aria-hidden="true"></i></button>').attr('data-key', id).hide(),
        $cancel = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-close" aria-hidden="true"></i></button>').attr('data-key', id).hide();
    $edit.on('click', function (e) {
        $grid.edit($(this).data('key'));
        $edit.hide();
        $delete.hide();
        $update.show();
        $cancel.show();
    });
    $delete.on('click', function (e) {
        $grid.removeRow($(this).data('key'));
    });
    $update.on('click', function (e) {
        $grid.update($(this).data('key'));
        $edit.show();
        $delete.show();
        $update.hide();
        $cancel.hide();
    });
    $cancel.on('click', function (e) {
        $grid.cancel($(this).data('key'));
        $edit.show();
        $delete.show();
        $update.hide();
        $cancel.hide();
    });
    $displayEl.empty().append($replace).append($downlaod).append($edit).append($delete).append($update).append($cancel);
}
let gridCertificatesDocumentFiles = $('#tbl_certificateDocumentFiles').grid({
    dataSource: '/Admin/Settings?handler=CourseCertificateDocsUsingSettingsId',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command', managementColumn: false },
    columns: [
        { field: 'fileName', title: 'File Name', width: 390 },
        { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 140 },
        { title: 'RPL', width: 50, renderer: domainRPLSettings, cssClass: 'text-center' },
        { width: 270, renderer: editTrainingCertificatesDocsButtonRendererSop },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});

$('#cbIsTrainingTestQuestionsDOE').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsTrainingTestQuestionsDOE').val(isChecked);
    if (isChecked == true) {
        $('#TrainingTestQuestionCertificateExpiryYears').attr('disabled', false)
    }
    else {
        $('#TrainingTestQuestionCertificateExpiryYears').attr('disabled', 'disabled')
    }

});
$('#cbIsTrainingCertificateWithQandADump').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsTrainingCertificateWithQandADump').val(isChecked);

});

$('#cbIsTrainingCertificateHold').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsTrainingCertificateHold').val(isChecked);

});

$('#cbIsTrainingAnonymusFeedBack').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsTrainingAnonymusFeedBack').val(isChecked);

});
$('#cbIsOption1').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsOption1').val(isChecked);

    $('#cbIsOption2').prop("checked", false);
    $('#IsOption2').val(false);
    $('#cbIsOption3').prop("checked", false);
    $('#IsOption3').val(false);
    $('#cbIsOption4').prop("checked", false);
    $('#IsOption4').val(false);
    $('#cbIsOption5').prop("checked", false);
    $('#IsOption5').val(false);
    $('#cbIsOption6').prop("checked", false);
    $('#IsOption6').val(false);

});
$('#cbIsOption2').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsOption2').val(isChecked);

    $('#cbIsOption1').prop("checked", false);
    $('#IsOption1').val(false);
    $('#cbIsOption3').prop("checked", false);
    $('#IsOption3').val(false);
    $('#cbIsOption4').prop("checked", false);
    $('#IsOption4').val(false);
    $('#cbIsOption5').prop("checked", false);
    $('#IsOption5').val(false);
    $('#cbIsOption6').prop("checked", false);
    $('#IsOption6').val(false);

});
$('#cbIsOption3').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsOption3').val(isChecked);

    $('#cbIsOption1').prop("checked", false);
    $('#IsOption1').val(false);
    $('#cbIsOption2').prop("checked", false);
    $('#IsOption2').val(false);
    $('#cbIsOption4').prop("checked", false);
    $('#IsOption4').val(false);
    $('#cbIsOption5').prop("checked", false);
    $('#IsOption5').val(false);
    $('#cbIsOption6').prop("checked", false);
    $('#IsOption6').val(false);

});
$('#cbIsOption4').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsOption4').val(isChecked);

    $('#cbIsOption1').prop("checked", false);
    $('#IsOption1').val(false);
    $('#cbIsOption2').prop("checked", false);
    $('#IsOption2').val(false);
    $('#cbIsOption3').prop("checked", false);
    $('#IsOption3').val(false);
    $('#cbIsOption5').prop("checked", false);
    $('#IsOption5').val(false);
    $('#cbIsOption6').prop("checked", false);
    $('#IsOption6').val(false);

});
$('#cbIsOption5').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsOption5').val(isChecked);

    $('#cbIsOption1').prop("checked", false);
    $('#IsOption1').val(false);
    $('#cbIsOption2').prop("checked", false);
    $('#IsOption2').val(false);
    $('#cbIsOption3').prop("checked", false);
    $('#IsOption3').val(false);
    $('#cbIsOption4').prop("checked", false);
    $('#IsOption4').val(false);
    $('#cbIsOption6').prop("checked", false);
    $('#IsOption6').val(false);

});
$('#cbIsOption6').on('change', function () {

    const isChecked = $(this).is(':checked');

    $('#IsOption6').val(isChecked);

    $('#cbIsOption1').prop("checked", false);
    $('#IsOption1').val(false);
    $('#cbIsOption2').prop("checked", false);
    $('#IsOption2').val(false);
    $('#cbIsOption3').prop("checked", false);
    $('#IsOption3').val(false);
    $('#cbIsOption4').prop("checked", false);
    $('#IsOption4').val(false);
    $('#cbIsOption5').prop("checked", false);
    $('#IsOption5').val(false);

});
//p5-Issue3-End


//p5-Issue1-Start
function renderGuardCouseStatusForGuard(value, type, data) {
    if (type === 'display') {
        let cellValue;

        if (value) {
            // Check if data.guardlogins.logindate is present
            if (data.trainingCourseStatus.trainingCourseStatusColorId == 1) {
                cellValue = '<button type="button" class="btn btn-outline-primary mr-2" name="btn_start_guard_TrainingAndAssessment">Start</button>&nbsp;' +
                    '<input type="hidden" id="GuardCourseId" value="' + data.id + '">';
            }  else if (data.trainingCourseStatus.trainingCourseStatusColorId == 2) {
                cellValue = '<button type="button" class="btn btn-outline-primary mr-2" name="btn_InProgress_guard_TrainingAndAssessment" >In Progress</button>&nbsp;' +
                    '<input type="hidden" id="GuardCourseId" value="' + data.id + '">';
            }
         else {
                cellValue = '<button type="button" class="btn btn-outline-primary mr-2" name="btn_InHold_guard_TrainingAndAssessment" disabled >On Hold</button>&nbsp;' +
                '<input type="hidden" id="GuardCourseId" value="' + data.id + '">';
            }
            }


            //cellValue += '<button type="button" class="btn btn-outline-primary mr-2" name="btn_rpl_guard_TrainingAndAssessment">RPL</button>';

        return cellValue;
    }
    return value;
}
let gridGuardTrainingAndAssessment = $('#tbl_guard_trainingAndAssessment').DataTable({
    autoWidth: false,
    ordering: false,
    searching: false,
    paging: false,
    info: false,
    ajax: {
        url: '/Admin/GuardSettings?handler=GuardTrainingAndAssessmentTab',
        data: function (d) {
            d.guardId = $('#GuardLog_GuardLogin_GuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', visible: false },
        { data: 'hrGroupText', width: "12%" },
        { data: 'description', width: "30%" },
        { data: 'newNullColumn', width: '10%' },
        

        {
            targets: -1,
            data: null,
            width: '15%',
            'render': function (value, type, data) {
                return renderGuardCouseStatusForGuard(value, type, data);
            }
            //defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_start_guard_TrainingAndAssessment">Start</button>&nbsp;' +
            //    '<button type="button" class="btn btn-outline-primary mr-2" name="btn_rpl_guard_TrainingAndAssessment">RPL</button>',
            
        }]
    
});
gridGuardTrainingAndAssessment.on('draw.dt', function () {
    var tbody = $('#tbl_guard_trainingAndAssessment tbody');
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
$('#tbl_guard_trainingAndAssessment tbody').on('click', 'button[name=btn_start_guard_TrainingAndAssessment]', function () {
 

    var data = gridGuardTrainingAndAssessment.row($(this).parents('tr')).data();
    const token = $('input[name="__RequestVerificationToken"]').val();
    var courseStatus=2
    $.ajax({
        url: '/Admin/Settings?handler=UpdateCoursesStatus',
        data: {
            'Id': data.id,
            'TrainingCourseStatusId': courseStatus
        },
        // data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success == true) {
            gridGuardTrainingAndAssessmentByAdmin.clear().draw();
            gridGuardTrainingAndAssessmentByAdmin.ajax.reload();
            gridGuardTrainingAndAssessment.clear().draw();
            gridGuardTrainingAndAssessment.ajax.reload();
            GetClassroomLocation(1);
            window.open('/Guard/GuardStartTest?guid=' + $('#GuardLog_GuardLogin_GuardId').val() + '&&guardCourseId=' + data.id, "_blank");

        }
        else {
            alert(result.message);
        }
        $('#loader').hide();


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
});
$('#tbl_guard_trainingAndAssessment tbody').on('click', 'button[name=btn_InProgress_guard_TrainingAndAssessment]', function () {


    var data = gridGuardTrainingAndAssessment.row($(this).parents('tr')).data();
    //const token = $('input[name="__RequestVerificationToken"]').val();
    //var courseStatus = 2
    //$.ajax({
    //    url: '/Admin/Settings?handler=UpdateCoursesStatus',
    //    data: {
    //        'Id': data.id,
    //        'TrainingCourseStatusId': courseStatus
    //    },
    //    // data: { id: record },
    //    type: 'POST',
    //    headers: { 'RequestVerificationToken': token },
    //}).done(function (result) {
    //    if (result.success == true) {
            gridGuardTrainingAndAssessmentByAdmin.clear().draw();
            gridGuardTrainingAndAssessmentByAdmin.ajax.reload();
            gridGuardTrainingAndAssessment.clear().draw();
            gridGuardTrainingAndAssessment.ajax.reload();
            GetClassroomLocation(1);
            window.open('/Guard/GuardStartTest?guid=' + $('#GuardLog_GuardLogin_GuardId').val() + '&&guardCourseId=' + data.id, "_blank");

      /*  }*/
        $('#loader').hide();


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    //}).fail(function () {
    //    console.log('error');
    //})
});
//p5-Issue1-End

//p5-Issue2-Start
function renderGuardCouseStatusForadmin(value, type, data) {
    if (type === 'display') {
        let cellValue;

        if (value) {
            // Check if data.guardlogins.logindate is present
            if (data.trainingCourseStatus.trainingCourseStatusColorId==1) {
                cellValue = '<i class="fa fa-check-circle text-muted mr-3"></i>' +
                    '<input type="hidden" id="CourseId" value="' + data.id + '">';
            } else if (data.trainingCourseStatus.trainingCourseStatusColorId == 2) {
                cellValue = '<i class="fa fa-check-circle text-warning mr-3"></i>' +
                    '<input type="hidden" id="CourseId" value="' + data.id + '">';
            }
        else {
                cellValue = '' +
                    '<a href="#practicalDetailsModal" id="btnPracticalDetailsByGuard"><i class="fa fa-times-circle text-success mr-3"></i></a>' +
                    '<input type="hidden" id="CourseId" value="' + data.id + '">' +
                '<input type="hidden" id="trainingCourseId" value="' + data.trainingCourseId+ '">'                    ;
        }
        }
       
        //cellValue += '<button  class="btn btn-outline-primary  mb-1" name = "btn_admin_RPL" > RPL</button >';
       
        return cellValue;
    }
    return value;
}
let gridGuardTrainingAndAssessmentByAdmin = $('#tbl_guard_trainingAndAssessment_by_Admin').DataTable({
    autoWidth: false,
    ordering: false,
    searching: false,
    paging: false,
    info: false,
    ajax: {
        url: '/Admin/GuardSettings?handler=GuardTrainingAndAssessmentTabByAdmin',
        data: function (d) {
            d.guardId = $('#GuardComplianceandlicense_GuardId').val();
        },
        dataSrc: ''
    },
    columns: [
        { data: 'id', visible: false },
        { data: 'hrGroupText', width: "12%" },
        { data: 'description', width: "30%" },
        { data: 'newNullColumn', width: '10%' },

        {

            targets: -1,
            data: null,
            defaultContent: '',
            width: '5%',
            'render': function (value, type, data) {
                return renderGuardCouseStatusForadmin(value, type, data);
            }
        }
    ],
   
});
$('#tbl_guard_trainingAndAssessment_by_Admin tbody').on('click', '#btnPracticalDetailsByGuard', function () {
    $('#practicalDetailsModal').modal('show');
    var GuardId = $('#Guard_Id').val();
    $('#txtPrcticalGuardId').val(GuardId);
    
    $('#txtPrcticalCourseId').val($(this).closest("td").find('#trainingCourseId').val());

    getPracticalCourseLocation();
    getPracticaInstructorSignOff();


});
gridGuardTrainingAndAssessmentByAdmin.on('draw.dt', function () {
    var tbody = $('#tbl_guard_trainingAndAssessment_by_Admin tbody');
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
//p5-Issue2-End

//p5-Issue3-CourseDocumentUpload-start
$('#add_course_document_files').on('change', function (e) {
    e.preventDefault();
    uploadCourseDocUsingHR($(this), false, 3);
});
function uploadCourseDocUsingHR(uploadCtrl, edit = false) {
    var hrSettingsId = $('#HrSettings_Id').val();
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var hrreferenceNumber = 'HR' + referenceNumber;
    const file = uploadCtrl.get(0).files.item(0);
    const fileExtn = file.name.split('.').pop();
    if (!fileExtn || '.pdf,.ppt,.pptx,.mp4'.indexOf(fileExtn.toLowerCase()) < 0) {
        showModal('Unsupported file type. Please upload a .pdf, .ppt, .pptx or .mp4 file');
        return false;
    }

    const fileForm = new FormData();
    fileForm.append('file', file);
    fileForm.append('hrsettingsid', hrSettingsId);
    fileForm.append('hrreferenceNumber', hrreferenceNumber);


    if (edit) {
        fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));
        fileForm.append('tq-id', uploadCtrl.attr('tq-id'))
    }

    $.ajax({
        url: '/Admin/Settings?handler=UploadCourseDocUsingHR',
        type: 'POST',
        data: fileForm,
        processData: false,
        contentType: false,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
    }).done(function (data) {
        if (data.success) {
            gridCourseDocumentFiles.reload();

            showStatusNotification(data.success, data.message);
        }
        else {
            gridCourseDocumentFiles.reload();

            showStatusNotification(data.success, data.message);
        }
    }).fail(function () {
        showStatusNotification(false, 'Something went wrong');
    });
}
//const showStatusNotification = function (success, message) {
//    if (success) {
//        $('.toast .toast-header strong').removeClass('text-danger').addClass('text-success').html('Success');
//    } else {
//        $('.toast .toast-header strong').removeClass('text-success').addClass('text-danger').html('Error');
//    }
//    $('.toast .toast-body').html(message);
//    $('.toast').toast('show');
//}
$('#form_file_downloads_course_sop').on('change', 'input[name="upload_course_file_sop"]', function () {
    uploadCourseDocUsingHR($(this), true, 1);

});

//p5-Issue3-CourseDocumentUpload-end

$('#TrainingTestQuestionsDetailsTab .nav-item .nav-link').on("click", function (e) {
    var tabId = $(this).attr("href");
    if (tabId == '#TrainingTestQuestionsSettings') {
        $('#btn_save_trainingassessment_settings').attr('hidden', false);
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', true);
    }
    if (tabId == '#TrainingTestQuestionsAnswers') {
        $('#btn_save_trainingassessment_testquestions').attr('hidden', false);
        $('#btn_save_trainingassessment_settings').attr('hidden', true);
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_testquestions').attr('hidden', false);
        $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', true);
       
        if ($('#txt_TestQuestionAnswersId').val() != '') {
            $('#btn_delete_trainingassessment_testquestions').attr('disabled', false);
        }
    }
    if (tabId == '#TrainingTestQuestionsFeedback') {
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', false);
        $('#btn_save_trainingassessment_settings').attr('hidden', true);
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', false);
        
        if ($('#txt_FeedbackQuestionAnswersId').val() != '') {
            $('#btn_delete_trainingassessment_feedbackquestions').attr('disabled', false);
        }

    }
});
$('#trainingAssesmentTab .nav-item .nav-link').on("click", function (e) {
    var tabId = $(this).attr("href");
    if (tabId == '#TrainingCourse') {
        $('#btn_save_trainingassessment_settings').attr('hidden', true);
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#TrainingTestQuestions').removeClass('active');
        $('#TrainingCourse').addClass('active');
        $('#TrainingCertificate').removeClass('active');
    }
    if (tabId == '#TrainingTestQuestions') {
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_save_trainingassessment_settings').attr('hidden', false);
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#TrainingTestQuestions').addClass('active');
        $('#TrainingCourse').removeClass('active');
        $('#TrainingCertificate').removeClass('active');
        $('#TrainingTestQuestionsSettingsTab').addClass('active');
        $('#TrainingTestQuestionsAnswersTab').removeClass('active');
        $('#TrainingTestQuestionsFeedbackTab').removeClass('active');
        $('#TrainingTestQuestionsSettings').addClass('active');
        $('#TrainingTestQuestionsAnswers').removeClass('active');
        $('#TrainingTestQuestionsFeedback').removeClass('active');
    }
    if (tabId == '#TrainingCertificate') {
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#btn_save_trainingassessment_settings').attr('hidden', true);
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_delete_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#TrainingTestQuestions').removeClass('active');
        $('#TrainingCourse').removeClass('active');
        $('#TrainingCertificate').addClass('active');
        $('#trainingCertificateUploadtab').addClass('active');
        $('#trainingCourseInstructorUploadtab').removeClass('active');
        
        $('#TrainingCertificateUpload').addClass('active');
        $('#TrainingCourseInstructorUpload').removeClass('active');

        
    }
});
$('#cbIsTrainingTestQuestionsDOE').on('change', function () {
    var isChecked = $(this).is(':checked');
    if (isChecked == true) {
        $('#TrainingTestQuestionCertificateExpiryYears').attr('disabled', false);
    }
    else {
        $('#TrainingTestQuestionCertificateExpiryYears').attr('disabled', 'disabled');
    }
    $('#IsTrainingTestQuestionsDOE').val(isChecked)
});


$('#cbIsTrainingCertificateWithQandADump').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsTrainingCertificateWithQandADump').val(isChecked)
});



$('#cbIsTrainingCertificateHold').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsTrainingCertificateHold').val(isChecked)
});



$('#cbIsTrainingAnonymusFeedBack').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsTrainingAnonymusFeedBack').val(isChecked)
});
$('#btn_save_trainingassessment_settings').on("click", function (e) {
    e.preventDefault();

    if ($("#txt_TestQuestionSettingsId").val() == '') {
        $("#txt_TestQuestionSettingsId").val(-1);
    }
    var testQuestionSettingsId = parseInt($("#txt_TestQuestionSettingsId").val());
    var certificateExpiry;

    var obj = {
        Id: testQuestionSettingsId,
       CourseDurationId: $("#ddlCourseDurationETA").val(),
        TestDurationId: $("#ddlTestDurationETA").val(),
        PassMarkId: $("#ddlTestPassMark").val(),
        AttemptsId: $("#ddlTestAttempts").val(),
        CertificateExpiryId: $("#TrainingTestQuestionCertificateExpiryYears").val(),
        HRSettingsId: $("#HrSettings_Id").val(),
        IsCertificateExpiry: $('#IsTrainingTestQuestionsDOE').val(),


        IsCertificateWithQAndADump: $("#IsTrainingCertificateWithQandADump").val(),
        IsCertificateHoldUntilPracticalTaken: $("#IsTrainingCertificateHold").val(),
        IsAnonymousFeedback: $("#IsTrainingAnonymusFeedBack").val(),

    }
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=SaveTQSettings',
        data: { 'record': obj },
        // data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success) {
            alert("Saved Successfully");
        }
        LoadTQSettings();



        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
});
function LoadTQSettings() {

    $.ajax({
        url: '/Admin/Settings?handler=TQSettings&hrSettingsid=' + $("#HrSettings_Id").val(),

        type: 'GET',
        //headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.length > 0) {
            result.forEach((item) => {
                $('#txt_TestQuestionSettingsId').val(item.id);
                $('#ddlCourseDurationETA').val(item.courseDurationId);
                $('#ddlTestDurationETA').val(item.testDurationId);
                $('#ddlTestPassMark').val(item.passMarkId);
                $('#ddlTestAttempts').val(item.attemptsId);
                $('#cbIsTrainingTestQuestionsDOE').prop('checked', item.isCertificateExpiry);
                $('#IsTrainingTestQuestionsDOE').val(item.isCertificateExpiry);
                $('#TrainingTestQuestionCertificateExpiryYears').val(item.certificateExpiryId);
                $('#cbIsTrainingCertificateWithQandADump').prop('checked', item.isCertificateWithQAndADump);
                $('#IsTrainingCertificateWithQandADump').val(item.isCertificateWithQAndADump);
                $('#cbIsTrainingCertificateWithQandADump').prop('checked', item.isCertificateWithQAndADump);
                $('#IsTrainingCertificateWithQandADump').val(item.isCertificateWithQAndADump);
                $('#cbIsTrainingCertificateHold').prop('checked', item.isCertificateHoldUntilPracticalTaken);
                $('#IsTrainingCertificateHold').val(item.isCertificateHoldUntilPracticalTaken);
                $('#cbIsTrainingAnonymusFeedBack').prop('checked', item.isAnonymousFeedback);
                $('#IsTrainingAnonymusFeedBack').val(item.isAnonymousFeedback);


            });
        }
        else {
            $('#txt_TestQuestionSettingsId').val('');
            $('#ddlCourseDurationETA').val(3);
            $('#ddlTestDurationETA').val(3);
            $('#ddlTestPassMark').val(1);
            $('#ddlTestAttempts').val(1);
            $('#cbIsTrainingTestQuestionsDOE').val(false);
            $('#IsTrainingTestQuestionsDOE').val('');
            $('#TrainingTestQuestionCertificateExpiryYears').val('');
            $('#cbIsTrainingCertificateWithQandADump').val(false);
            $('#IsTrainingCertificateWithQandADump').val('');
            $('#cbIsTrainingCertificateWithQandADump').val(false);
            $('#IsTrainingCertificateWithQandADump').val('');
            $('#cbIsTrainingCertificateHold').val(false);
            $('#IsTrainingCertificateHold').val();
            $('#cbIsTrainingAnonymusFeedBack').val(false);
            $('#IsTrainingAnonymusFeedBack').val();
        }
        /*$('#onDutyIsToday').prop('checked', true);*/


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
}

$('#cbIsOption1').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsOption1').val(isChecked)
});
$('#cbIsOption2').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsOption2').val(isChecked)
});
$('#cbIsOption3').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsOption3').val(isChecked)
});
$('#cbIsOption4').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsOption4').val(isChecked)
});
$('#cbIsOption5').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsOption5').val(isChecked)
});
$('#cbIsOption6').on('change', function () {
    var isChecked = $(this).is(':checked');

    $('#IsOption6').val(isChecked)
});
$('#ddlTQNo').on('change', function () {
    var valueTQNo = $(this).val();

    LoadNextTQQuestions();
    GetNumberOfQuestions();
});
$('#ddlTestQuestionNo').on('change', function () {
    var valueTQNo = $(this).val();

    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=QuestionWithQuestionNumber',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
            'tqNumberId': $("#ddlTQNo").val(),
            'questionumberId': valueTQNo,
        },
         //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            $('#txtQuestion').val('');
            $('#txt_Option1').val('');
            $('#txt_Option2').val('');
            $('#txt_Option3').val('');
            $('#txt_Option4').val('');
            $('#txt_Option5').val('');
            $('#txt_Option6').val('');
            $('#cbIsOption1').prop('checked', false);
            $('#cbIsOption1').change();
            $('#cbIsOption2').prop('checked', false);
            $('#cbIsOption2').change();
            $('#cbIsOption3').prop('checked', false);
            $('#cbIsOption3').change();
            $('#cbIsOption4').prop('checked', false);
            $('#cbIsOption4').change();
            $('#cbIsOption5').prop('checked', false);
            $('#cbIsOption5').change();
            $('#cbIsOption6').prop('checked', false);
            $('#cbIsOption6').change();
        $("#txt_TestQuestionAnswersId").val(result.id);
        $("#txtQuestion").val(result.question);
        
            GetAnswers();
            GetNumberOfQuestionsNew();
            if ($('#txt_TestQuestionAnswersId').val() != '') {
                $('#btn_delete_trainingassessment_testquestions').attr('disabled', false);
            }
            else {
                $('#btn_delete_trainingassessment_testquestions').attr('disabled', 'disabled');
            }
        }
        else {
            LoadNextTQQuestions();
            GetNumberOfQuestions();
            $('#btn_delete_trainingassessment_testquestions').attr('disabled', 'disabled');
        }



    }).fail(function () {
        console.log('error');
    })
});
function GetAnswers() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=QuestionAndAnswersWithQuestionNumber',
        data: {
            'questionId': $("#txt_TestQuestionAnswersId").val(),
        },
            // data: { id: record },
            type: 'GET',
            headers: { 'RequestVerificationToken': token },
        }).done(function (result) {
            var j = 1;
            
            
            $.each(result, function (i, d) {
                
                var txtoptions = 'txt_Option' + j;
                var options = 'cbIsOption'+j;
                var hiddeninput = 'IsOption'+j;
                j++;
                $('#' + txtoptions).val(d.options);
                $('#' + options).prop('checked', d.isAnswer);
                $('#' + hiddeninput).val(d.isAnswer);

            });
            



        }).fail(function () {
            console.log('error');
        })
}
$('#btn_save_trainingassessment_testquestions').on("click", function (e) {
    e.preventDefault();
    $('#loader').show();
    if ($("#txt_TestQuestionAnswersId").val() == '') {
        $("#txt_TestQuestionAnswersId").val(-1);
    }
    var testQuestionAnswersId = parseInt($("#txt_TestQuestionAnswersId").val());
    var certificateExpiry;

    var obj = {
        Id: testQuestionAnswersId,
        QuestionNoId: $("#ddlTestQuestionNo").val(),
        TQNumberId: $("#ddlTQNo").val(),
        Question: $("#txtQuestion").val(),
        HRSettingsId: $("#HrSettings_Id").val(),

    }
    let objAnswers = [];
    if ($('#txt_Option1').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestQuestionsId: testQuestionAnswersId,
            Options: $('#txt_Option1').val(),
            IsAnswer: $('#IsOption1').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_Option2').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestQuestionsId: testQuestionAnswersId,
            Options: $('#txt_Option2').val(),
            IsAnswer: $('#IsOption2').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_Option3').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestQuestionsId: testQuestionAnswersId,
            Options: $('#txt_Option3').val(),
            IsAnswer: $('#IsOption3').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_Option4').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestQuestionsId: testQuestionAnswersId,
            Options: $('#txt_Option4').val(),
            IsAnswer: $('#IsOption4').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_Option5').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestQuestionsId: testQuestionAnswersId,
            Options: $('#txt_Option5').val(),
            IsAnswer: $('#IsOption5').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_Option6').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestQuestionsId: testQuestionAnswersId,
            Options: $('#txt_Option6').val(),
            IsAnswer: $('#IsOption6').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    const token = $('input[name="__RequestVerificationToken"]').val();

    if (($('#IsOption1').val() == 'false') && ($('#IsOption2').val() == 'false') && ($('#IsOption3').val() == 'false') && ($('#IsOption4').val() == 'false') && ($('#IsOption5').val() == 'false') && ($('#IsOption6').val() == 'false')) {
        alert('Please use toggle to enable correct answer');
        return;
    }
    $.ajax({
        url: '/Admin/Settings?handler=SaveTQAnswers',
        data: {
            'testquestions': obj,
            'testquestionanswers': objAnswers
        },
        // data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success == true) {
            alert('Saved Successfully');
            LoadCurrentTQQuestions();
            //
            GetNumberOfQuestions();
            if ($('#txt_TestQuestionAnswersId').val() != '') {
                $('#btn_delete_trainingassessment_testquestions').attr('disabled', false);
            }
            else {
                $('#btn_delete_trainingassessment_testquestions').attr('disabled', 'disabled');
            }
        }
        $('#loader').hide();


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
});
$('#btn_delete_trainingassessment_testquestions').on("click", function (e) {
    e.preventDefault();
    $('#loader').show();
    
    var testQuestionAnswersId = parseInt($("#txt_TestQuestionAnswersId").val());
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=DeleteTQAnswers',
        data: {
            'Id': testQuestionAnswersId
        },
        // data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success == true) {
            LoadNextTQQuestions();
            //
            GetNumberOfQuestions();
           
                $('#btn_delete_trainingassessment_testquestions').attr('disabled', 'disabled');
            
        }
        $('#loader').hide();


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
});
function LoadCurrentTQQuestions() {
    var valueTQNo = $('#ddlTestQuestionNo').val();

    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=QuestionWithQuestionNumber',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
            'tqNumberId': $("#ddlTQNo").val(),
            'questionumberId': valueTQNo,
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            $("#txt_TestQuestionAnswersId").val(result.id);
            $("#txtQuestion").val(result.question);

            GetAnswers();
            GetNumberOfQuestionsNew();
        }
        else {
            LoadNextTQQuestions();
            GetNumberOfQuestions();
        }



    }).fail(function () {
        console.log('error');
    })
}
function LoadNextTQQuestions() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=NextQuestionWithinSameTQNumber',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
            'tqNumberId': $("#ddlTQNo").val()
        },
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {

        $("#txt_TestQuestionAnswersId").val('');
        $("#ddlTestQuestionNo").val(result);
        $('#txtQuestion').val('');
        $('#txt_Option1').val('');
        $('#txt_Option2').val('');
        $('#txt_Option3').val('');
        $('#txt_Option4').val('');
        $('#txt_Option5').val('');
        $('#txt_Option6').val('');
        $('#cbIsOption1').prop('checked', false);
        $('#cbIsOption1').change();
        $('#cbIsOption2').prop('checked', false);
        $('#cbIsOption2').change();
        $('#cbIsOption3').prop('checked', false);
        $('#cbIsOption3').change();
        $('#cbIsOption4').prop('checked', false);
        $('#cbIsOption4').change();
        $('#cbIsOption5').prop('checked', false);
        $('#cbIsOption5').change();
        $('#cbIsOption6').prop('checked', false);
        $('#cbIsOption6').change();


    }).fail(function () {
        console.log('error');
    })
}
function GetNumberOfQuestions() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=QuestionsCount',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
            'tqNumberId': $("#ddlTQNo").val()
        },
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {


        $('#lblCounterNo').html(result + ' Questions Loaded');



    }).fail(function () {
        console.log('error');
    })
}
function GetNumberOfQuestionsNew() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=QuestionsCount',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
            'tqNumberId': $("#ddlTQNo").val()
        },
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {


        $("#ddlTestQuestionNo").val();
        $('#lblCounterNo').html($("#ddlTestQuestionNo").val() + ' of ' + result);



    }).fail(function () {
        console.log('error');
    })
}
function LoadLastTQNumbers() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=LastTQNumber',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val()
        },
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {

        if (result != 0) 
         {
            $("#ddlTQNo").val(result);
        }
        LoadNextTQQuestions();

    }).fail(function () {
        console.log('error');
    })
}

//strting of feedback questions


function LoadLastFeedbackQNumbers() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=LastFeedbackQNumber',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val()
        },
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {


        $("#ddlFeedbackQuestionNo").val(result);
        $("#txt_FeedbackQuestionAnswersId").val('');
        $('#txtFeedbackQuestion').val('');
        $('#txt_FeedbackOption1').val('');
        $('#txt_FeedbackOption2').val('');
        $('#txt_FeedbackOption3').val('');
        $('#txt_FeedbackOption4').val('');
        $('#txt_FeedbackOption5').val('');
        $('#txt_FeedbackOption6').val('');

    }).fail(function () {
        console.log('error');
    })
}
function LoadCurrentFeedbackQNumbers() {
    var valueFeedbackQNo = $('#ddlFeedbackQuestionNo').val();

    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=FeedbackQuestionWithQuestionNumber',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
            'questionumberId': valueFeedbackQNo,
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            $("#txt_FeedbackQuestionAnswersId").val(result.id);
            $("#txtFeedbackQuestion").val(result.question);

            GetFeedbackAnswers();
            GetNumberOfFeedbackQuestionsNew();
        }
        else {
            LoadLastFeedbackQNumbers();
            GetNumberOfFeedbackQuestions();
        }



    }).fail(function () {
        console.log('error');
    })
}
function GetNumberOfFeedbackQuestions() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=FeedbackQuestionsCount',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
        },
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {


        $('#lblFeedbackCounterNo').html(result + ' Questions Loaded');



    }).fail(function () {
        console.log('error');
    })
}
function GetNumberOfFeedbackQuestionsNew() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=FeedbackQuestionsCount',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
        },
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {

        $("#ddlFeedbackQuestionNo").val();
        $('#lblFeedbackCounterNo').html($("#ddlFeedbackQuestionNo").val() + ' Of '+ result);



    }).fail(function () {
        console.log('error');
    })
}
$('#ddlFeedbackQuestionNo').on('change', function () {
    var valueFeedbackQNo = $(this).val();

    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=FeedbackQuestionWithQuestionNumber',
        data: {
            'hrSettingsId': $("#HrSettings_Id").val(),
            'questionumberId': valueFeedbackQNo,
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            $("#txt_FeedbackQuestionAnswersId").val(result.id);
            $("#txtFeedbackQuestion").val(result.question);

            GetFeedbackAnswers();
            GetNumberOfFeedbackQuestionsNew();
            if ($('#txt_FeedbackQuestionAnswersId').val() != '') {
                $('#btn_delete_trainingassessment_feedbackquestions').attr('disabled', false);
            }
            else {
                $('#btn_delete_trainingassessment_feedbackquestions').attr('disabled', 'disabled');
            }
        }
        else {
            LoadLastFeedbackQNumbers();
            GetNumberOfFeedbackQuestions();
        }



    }).fail(function () {
        console.log('error');
    })
});
function GetFeedbackAnswers() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=FeedbackQuestionAndAnswersWithQuestionNumber',
        data: {
            'questionId': $("#txt_FeedbackQuestionAnswersId").val(),
        },
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        var j = 1;


        $.each(result, function (i, d) {

            var txtoptions = 'txt_FeedbackOption' + j;
            j++;
            $('#' + txtoptions).val(d.options);

        });




    }).fail(function () {
        console.log('error');
    })
}
$('#btn_save_trainingassessment_feedbackquestions').on("click", function (e) {
    e.preventDefault();
    $('#loader').show();
    if ($("#txt_FeedbackQuestionAnswersId").val() == '') {
        $("#txt_FeedbackQuestionAnswersId").val(-1);
    }
    if ($("#txtFeedbackQuestion").val() == '') {
        var placeholderValue = $('#txtFeedbackQuestion').attr('placeholder');
        $("#txtFeedbackQuestion").val(placeholderValue);
    }
    var feedbackQuestionAnswersId = parseInt($("#txt_FeedbackQuestionAnswersId").val());
    var certificateExpiry;

    var obj = {
        Id: feedbackQuestionAnswersId,
        QuestionNoId: $("#ddlFeedbackQuestionNo").val(),
        Question: $("#txtFeedbackQuestion").val(),
        HRSettingsId: $("#HrSettings_Id").val(),

    }
    let objAnswers = [];
    if ($('#txt_FeedbackOption1').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestFeedbackQuestionsId: feedbackQuestionAnswersId,
            Options: $('#txt_FeedbackOption1').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_FeedbackOption2').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestFeedbackQuestionsId: feedbackQuestionAnswersId,
            Options: $('#txt_FeedbackOption2').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_FeedbackOption3').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestFeedbackQuestionsId: feedbackQuestionAnswersId,
            Options: $('#txt_FeedbackOption3').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_FeedbackOption4').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestFeedbackQuestionsId: feedbackQuestionAnswersId,
            Options: $('#txt_FeedbackOption4').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_FeedbackOption5').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestFeedbackQuestionsId: feedbackQuestionAnswersId,
            Options: $('#txt_FeedbackOption5').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    if ($('#txt_FeedbackOption6').val() != '') {
        let objAnswersnew = {
            Id: 0,
            TrainingTestFeedbackQuestionsId: feedbackQuestionAnswersId,
            Options: $('#txt_FeedbackOption6').val(),
        }
        objAnswers.push(objAnswersnew);
    }
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=SaveFeedbackQAnswers',
        data: {
            'feedbackquestions': obj,
            'feedbackquestionanswers': objAnswers
        },
        // data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success == true) {
            alert("Saved Successfully");
            // LoadLastFeedbackQNumbers();
            LoadCurrentFeedbackQNumbers();
            GetNumberOfFeedbackQuestions();
            if ($('#txt_FeedbackQuestionAnswersId').val() != '') {
                $('#btn_delete_trainingassessment_feedbackquestions').attr('disabled', false);
            }
            else {
                $('#btn_delete_trainingassessment_feedbackquestions').attr('disabled', 'disabled');
            }
        }
        $('#loader').hide();


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
});
$('#btn_delete_trainingassessment_feedbackquestions').on("click", function (e) {
    e.preventDefault();
    $('#loader').show();
   
    
    var feedbackQuestionAnswersId = parseInt($("#txt_FeedbackQuestionAnswersId").val());
 
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=DeleteFeedbackQAnswers',
        data: {
            'Id': $("#txt_FeedbackQuestionAnswersId").val()

        },
        // data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success == true) {
          
            LoadLastFeedbackQNumbers();
            GetNumberOfFeedbackQuestions();

                $('#btn_delete_trainingassessment_feedbackquestions').attr('disabled', 'disabled');
            
        }
        $('#loader').hide();


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
});
$("textarea[id='txtQuestion']").keyup(function () {
    $('#lblRemainingCount').html(getTestQuestionLength($(this).val()));
});

function getTestQuestionLength(note) {
    return 'Remaining characters: ' + getNoteRemainingCount(note, 'test_question');
}

function getNoteRemainingCount(note, type) {
    let max_chars = Number.MAX_SAFE_INTEGER;
    if (type === 'test_question') max_chars = 2048;
    else if (type === 'feedback_question') max_chars = 2048;
    return max_chars - (note ? note.replace(/(\r\n|\n|\r)/g, 'xx').length : 0);
}

$("textarea[id='txtFeedbackQuestion']").keyup(function () {
    $('#lblFeedbackQuestionRemainingCount').html(getFeedbackQuestionLength($(this).val()));
});

function getFeedbackQuestionLength(note) {
    return 'Remaining characters: ' + getNoteRemainingCount(note, 'feedback_question');
}

const courseInstructorDetails =()=> {
    $.ajax({
        url: '/Admin/settings?handler=InstructorAndPosition',
        //data: { record: data },
        type: 'GET',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
       
        result;
        
    });
   
}

function courseInstructorEditor($editorContainer, value, record) {
    if (isCourseInstructorAdding && value === undefined)
        value = 0;
    let selectHtml = $('<select class="form-control dd-course-instructor"></select>');
    $.ajax({
        url: '/Admin/settings?handler=InstructorAndPosition',
        //data: { record: data },
        type: 'GET',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        selectHtml.append('<option value=""></option>')
        result.forEach(function (item) {
            selectHtml.append('<option value="' + item.id + '" >' + item.name + ' (' + item.position + ') </option>')
        })
        if (record.trainingInstructorId != 'undefined') {


            selectHtml.val(record.trainingInstructorId);
            InstructorGlobalId = record.trainingInstructorId;
        }

    });
   
        
   
    $editorContainer.append(selectHtml);
    
}
let isCourseInstructorAdding = false;
let gridCourseInstructorDetails = $('#tbl_Course_Instructor').grid({
    dataSource: '/Admin/Settings?handler=CourseInstructor',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },
    columns: [
        { field: 'id', title: '', hidden: true },
       // { field: 'instructorName', title: 'Instructor Name', width: 390, type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=Instructor', valueField: 'name', textField: 'name' } }, 
        { field: 'instructorName', title: 'Instructor Name', width: 390, type: 'dropdown', editor: courseInstructorEditor  },

        
        { field: 'instructorPosition', title: 'Position', width: 140}
       
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');

    }
});

$('#add_course_Instructor').on('click', function () {
  
    var rowCount = $('#tbl_Course_Instructor tr').length;


    if (isCourseInstructorAdding) {
        alert('Unsaved changes in the grid. Refresh the page');
    } else {
        isCourseInstructorAdding = true;
        gridCourseInstructorDetails.addRow({
            'id': -1,
            'instructorName': '',
            'instructorPosition': '',
        }).edit(-1);
    }
});
var InstructorGlobalId;
$('#tbl_Course_Instructor').on('change', '.dd-course-instructor', function () {

    var control = $(this);
    var id = $(this).val();
    var position;
    
    
    $.ajax({
        url: '/Admin/settings?handler=InstructorAndPositionWithId',
        data: { Id: id },
        type: 'GET',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result != null) {
            position = result.position;
           
        }
        else {
            position = '';
            name = '';
        }
        InstructorGlobalId = id;
    
        control.closest('tr').find('td').eq(2).text(position);
        control.closest('tr').find('td').eq(2).val(position);

    });
   
   
    
});
if (gridCourseInstructorDetails) {
    gridCourseInstructorDetails.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        if (record.instructorName == '') {
            InstructorGlobalId = null;
        }
        InstructorGlobalId;
        $.ajax({
            url: '/Admin/Settings?handler=SaveTrainingCourseInstructor',
            data: { id: data.id, instructorId: InstructorGlobalId, hrsettingsId: $('#HrSettings_Id').val() },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) {

                gridCourseInstructorDetails.clear();
                gridCourseInstructorDetails.reload({ type: $('#HrSettings_Id').val() });;
            }
            else {
                alert(result.message);

                gridCourseInstructorDetails.edit(id);

            }
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isCourseInstructorAdding)
                isCourseInstructorAdding = false;
        });
    });

    gridCourseInstructorDetails.on('rowRemoving', function (e, id, record) {
        if (confirm('Are you sure want to delete this field?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteTrainingCourseInstructor',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) { gridCourseInstructorDetails.clear(); gridCourseInstructorDetails.reload({ type: $('#HrSettings_Id').val() });; }
                else alert(result.message);
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isCourseInstructorAdding)
                    isCourseInstructorAdding = false;
            });
        }
    });
}
//p5-Issue3-CourseDocumentUpload-start
$('#add_certificate_document_files').on('change', function (e) {
    e.preventDefault();
    uploadCourseCertificateDocUsingHR($(this), false, 3);
});
function uploadCourseCertificateDocUsingHR(uploadCtrl, edit = false) {
    var hrSettingsId = $('#HrSettings_Id').val();
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var hrreferenceNumber = 'HR' + referenceNumber;
    const file = uploadCtrl.get(0).files.item(0);
    const fileExtn = file.name.split('.').pop();
    if (!fileExtn || '.pdf,.ppt,.pptx'.indexOf(fileExtn.toLowerCase()) < 0) {
        showModal('Unsupported file type. Please upload a .pdf, .ppt or .pptx file');
        return false;
    }

    const fileForm = new FormData();
    fileForm.append('file', file);
    fileForm.append('hrsettingsid', hrSettingsId);
    fileForm.append('hrreferenceNumber', hrreferenceNumber);
    fileForm.append('filename', $('#txtHrSettingsDescription').val() + '_Certificate.pdf');


    if (edit) {
        fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));
    }

    $.ajax({
        url: '/Admin/Settings?handler=UploadCourseCertificateDocUsingHR',
        type: 'POST',
        data: fileForm,
        processData: false,
        contentType: false,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
    }).done(function (data) {
        if (data.success) {
            
            gridCertificatesDocumentFiles.clear();
            gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });

            showStatusNotification(data.success, data.message);
        }
        else {
            gridCertificatesDocumentFiles.clear();
            gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });

            showStatusNotification(data.success, data.message);
        }
    }).fail(function () {
        showStatusNotification(false, 'Something went wrong');
    });
}

$('#tbl_certificateDocumentFiles').on('change', 'input[name="upload_certificate_file_sop"]', function () {
    uploadCourseCertificateDocUsingHR($(this), true, 1);

});
$('#tbl_certificateDocumentFiles').on('click', '.delete_certificate_file_sop', function () {
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var hrreferenceNumber = 'HR' + referenceNumber;
    if (confirm('Are you sure want to delete this file?')) {
        $.ajax({
            url: '/Admin/Settings?handler=DeleteCourseCertificateDocUsingHR',
            data: {
                id: $(this).attr('data-doc-id'),
                hrreferenceNumber: hrreferenceNumber
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                gridCertificatesDocumentFiles.clear();
                gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
            }
           
        }).fail(function () {
            console.log('error')
        });
    }
})

//p5-Issue3-CourseDocumentUpload-end

//p5-Issue-2-start
$('#btnAddGuardCourse,#btnAddGuardCourse1').on('click', function (e) {
    e.preventDefault();
    //  ReloadHrGroupsforCourseList();
    var guardid = $('#Guard_Id').val();
    $('#trainingguardId').val(guardid);
    //$('#courseList').html('');
    
    $('#selectCoursesForGuardByAdmin').modal('show');
    loadTrainingCourses()
    //$('#courseList').load();
    
})
function loadTrainingCourses() {
    $.ajax({
        url: '/Admin/Settings?handler=TrainingCourses',
        type: 'GET',
        success: function (data) {
            let courseList = $('#courseList');
            courseList.empty(); // Clear existing content

            data.forEach(group => {
                let groupItem = `
                        <li class="list-group-item list-group-item-success" 
                            id="attach_${group.groupId}" 
                            data-index="${group.groupId}" 
                            style="border-left: 0;border-right: 0;font-size:12px;padding:3px">
                        </li>`;

                courseList.append(groupItem);

                group.courses.forEach(course => {
                    let courseItem = course.fileName
                        ? `<li class="list-group-item" 
                                    id="attach_${course.id}" 
                                    data-index="${course.id}" 
                                    style="border-left: 0;border-right: 0;font-size:12px;padding:3px">
                                    ${course.fileName}
                                    <i class="fa fa-check ml-2 text-success btn-select-course-status" 
                                       title="Select" 
                                       style="cursor: pointer;float:right"></i>
                               </li>`
                        : `<li class="list-group-item list-group-item-success" 
                                    id="attach_${course.id}" 
                                    data-index="${course.id}" 
                                    style="border-left: 0;border-right: 0;font-size:12px;padding:3px">
                               </li>`;

                    courseList.append(courseItem);
                });
            });
        },
        error: function () {
            alert('Failed to load training courses.');
        }
    });
}
function ReloadHrGroupsforCourseList() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=HrGroupsforCourseList',
        
        // data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.length > 0) {
            $('#courseList').empty()
            result.forEach((item) => {
                $('#courseList').append('<li class="list-group-item list-group-item-success" id="attach_' + item.value + '" data-index="' + item.value + '" style="border-left: 0;border-right: 0;font-size:12px;padding:3px"></li>');
                ReloadCourseList(item.value);
            });
           
        }


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
}
function ReloadCourseList(id) 
{
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=CourseList',

         data: { groupid: id },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.length > 0) {
            result.qaforEach((item) => {
                $('#courseList').append('<li class="list-group-item" id="attach_' + item.id + '" data-index="' + item.id + '" style="border-left: 0;border-right: 0;font-size:12px;padding:3px">' + item.fileName + '< i class= "fa fa-check ml-2 text-success btn-select-course-status" title = "Select" style = "cursor: pointer;float:right" ></i ></li > ');

            });

        }
       


        //$.each(item1 in result)
        //{
        //    '< option value = "' + item.name + '" >' + item.name +'</option >'
        //}
    }).fail(function () {
        console.log('error');
    })
}
$('#courseList').on('click', '.btn-select-course-status', function (event) {

    var target = event.target;
    var parentId = target.parentNode.innerText.trim();
    
    const courseStatus = 1;
    var courseId = target.parentNode.dataset.index;
    var guardId = $('#trainingguardId').val();
   
    $.ajax({
        url: '/Admin/Settings?handler=SaveGuardTrainingAndAssessmentTab',
        type: 'POST',
        data: {
            TrainingCourseId: courseId,
            GuardId: guardId,
            TrainingCourseStatusId: courseStatus
        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.success) {
           // alert('saved successfully');
            $('#selectCoursesForGuardByAdmin').modal('hide');
            gridGuardTrainingAndAssessmentByAdmin.clear().draw();
            gridGuardTrainingAndAssessmentByAdmin.ajax.reload();
            gridGuardTrainingAndAssessment.clear().draw();
            gridGuardTrainingAndAssessment.ajax.reload();
        }
    });

});
//p5-Issue-2-end

let isLocationAdding = false
let gridClassroomLocation
gridClassroomLocation = $('#tbl_classroomLocation').grid({
    dataSource: '/Admin/Settings?handler=TrainingLocation',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },
    columns: [
        { field: 'id', hidden: true },
        { field: 'location', title: 'Location', width: '100%', editor: true },


    ],

    initialized: function (e) {
        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    },

});
if (gridClassroomLocation) {
    gridClassroomLocation.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        $.ajax({
            url: '/Admin/Settings?handler=SaveTrainingLocation',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) gridClassroomLocation.reload();
            else alert(result.message);
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isLoteAdding)
                isLoteAdding = false;
        });
    });

    gridClassroomLocation.on('rowRemoving', function (e, id, record) {
        if (confirm('Are you sure want to delete this field?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteTrainingLocation',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) gridClassroomLocation.reload();
                else alert(result.message);
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isLoteAdding)
                    isLoteAdding = false;
            });
        }
    });
}
if ($('#hr_settings_fields_types').val() == '') {
  
    gridClassroomLocation.hide();
    gridHrSettingswithCourseLibrary.hide();
}


$('#add_location').on('click', function () {
    if ($('#hr_settings_fields_types').val() == 7) {
        //if (isLocationAdding) {
        //    alert('Unsaved changes in the grid. Refresh the page');
        //} else {
        //    isLocationAdding = true;
        //    gridClassroomLocation.addRow({
        //        'id': -1,
        //        'location': '',
        //    }).edit(-1);
        //}
        const newRecord = {
            id: -1, // Temporary ID for new record
            location: $('#txtClassroomlocation').val()
        };
        gridClassroomLocation.trigger('rowDataChanged', [-1, newRecord]);
        $('#txtClassroomlocation').val('')
        gridClassroomLocation.reload();
    }
});
$('#tbl_certificateDocumentFiles tbody').on('click', '#btnRPLCertificate', function () {
    // ClearModelControls();
    
    var isChecked = $(this).closest("td").find('#chkIsRPL').is(':checked');
    
       
    var id = $(this).closest("td").find('#certificateId').val();
    if (isChecked == true) {
        $('#rplDetailsModal').modal('show');
        getPracticalLocation('');
        getRPLInstructorSignOff('');
        //var userName = $(this).closest("td").find('#userName').val();
        //$('#siteTypeDomainDeatils').find('#userName1').text(typeName)
        //$('#siteTypeDomainDeatils').find('#siteTypeId').val(typeId)
        $('#rplCertificateId').val(id);
        fetchURPLDeatils(id);
    }
    else {
        disableRPLDetails(id)
    }
});
function fetchURPLDeatils(Id) {
    $.ajax({
        url: '/Admin/Settings?handler=RPLDetails',
        method: 'GET',
        data: { id: Id },
        success: function (data) {
        
                //ClearModelControls();
                //$.each(data, function (i,item) {
            if (data != null) {

                    $('#rplDetailsModal').find('#rplCertificateId').val(Id)
                    $('#rplDetailsModal').find('#rplId').val(data.id)
            //$('#rplDetailsModal').find('#ddlPracticalAssessmentLocation').val(data.trainingPracticalLocationId);
            getPracticalLocation(data.trainingPracticalLocationId);
            $('#rplDetailsModal').find('#RPL_DateAssessment_started').val(data.assessmentStartDate.split('T')[0]);
            $('#rplDetailsModal').find('#RPL_DateAssessment_ended').val(data.assessmentEndDate.split('T')[0]);
            // $('#rplDetailsModal').find('#ddlRPLInstructorsignOff').val(data.trainingInstructorId);
                getRPLInstructorSignOff(data.trainingInstructorId)
            }
                //});
               

               

            
        },
        error: function (xhr, status, error) {
            console.error('Error fetching data:', error);
        }
    });
}
function disableRPLDetails(Id) {
    $.ajax({
        url: '/Admin/Settings?handler=DeleteRPLDetails',
        data: { id: Id },
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.success) {
            $('#rplDetailsModal').modal('hide');
            gridCertificatesDocumentFiles.clear();
            gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });


        } else {

            displayGuardValidationSummary('rplValidationSummary', result.message);
        }
    }).always(function () {
        $('#loader').hide();
    });
}
function getPracticalLocation(selectedLocation) {
    const practicalLocationControl = $('#ddlPracticalAssessmentLocation');
    practicalLocationControl.html('');
    $.ajax({
        url: '/Admin/Settings?handler=TrainingLocation',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            practicalLocationControl.append('<option value="" selected>Select</option>')
            data.map(function (site) {
                practicalLocationControl.append('<option value="' + site.id + '">' + site.location + '</option>');
            });

            if (selectedLocation) {
                $('#ddlPracticalAssessmentLocation').val(selectedLocation);
            } else {
                $('#ddlPracticalAssessmentLocation').val('');
            }
        }
    });
}
function getRPLInstructorSignOff(seletedInstructor) {
   
    const practicalInstructorControl = $('#ddlRPLInstructorsignOff');
    practicalInstructorControl.html('');
    $.ajax({
        url: '/Admin/Settings?handler=InstructorAndPosition',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            practicalInstructorControl.append('<option value="" selected>Select</option>')
            data.map(function (site) {
                practicalInstructorControl.append('<option value="' + site.id + '">' + site.name + '</option>');
            });

            if (seletedInstructor) {
                $('#ddlRPLInstructorsignOff').val(seletedInstructor);
            } else {
                $('#ddlRPLInstructorsignOff').val('');
            }
        }
    });
}
$('#btnSaveRPLDetails').on('click', function () {

    clearGuardValidationSummary('rplValidationSummary');
    var Id = $('#rplId').val();
    if (Id == 0) {
        $('#rplId').val(-1);
    }
    var obj = {
        Id: $('#rplId').val(),
        TrainingPracticalLocationId: $('#ddlPracticalAssessmentLocation').val(),
        AssessmentStartDate: $('#RPL_DateAssessment_started').val(),
        AssessmentEndDate: $('#RPL_DateAssessment_ended').val(),
        TrainingCourseCertificateId: $('#rplCertificateId').val(),
        TrainingInstructorId: $('#ddlRPLInstructorsignOff').val(),
        isDeleted: false
        }
   

   
            
            $('#loader').show();
            $.ajax({
                url: '/Admin/Settings?handler=SaveRPLDetails',
                data:{record:obj},
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) {
                    $('#rplDetailsModal').modal('hide');
                    gridCertificatesDocumentFiles.clear();
                    gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });

                    
                } else {
                   
                    displayGuardValidationSummary('rplValidationSummary', result.message);
                }
            }).always(function () {
                $('#loader').hide();
            });

        
    




});
$('#rplDetailsModal').on('hidden.bs.modal', function (e) {
    // Code to run when the modal is closed
    gridCertificatesDocumentFiles.clear();
    gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });


});

function GetClassroomLocation(selectedLocation) {
    const practicalLocationControl = $('#ddlTestClassroomLocation');
    $('#ddl_TestClassroomLocation').html('');
    $.ajax({
        url: '/Admin/Settings?handler=TrainingLocation',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            
            data.map(function (site) {
                $('#ddl_TestClassroomLocation').append('<option value="' + site.id + '">' + site.location + '</option>');
            });

            if (selectedLocation) {
                $('#ddl_TestClassroomLocation').val(selectedLocation);
            } else {
                $('#ddl_TestClassroomLocation').val('');
            }
        }
    });
}
//p5-Issue6-start
$('#btnStartCourse').on('click', function (e) {
    e.preventDefault();
    StartCourse();
   
});
function StartCourse() {
   
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardStartTest',
        data: {
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val(),
            'guardId': $("#txtguardIdForTest").val(),
            'locationId': $('#ddlTestClassroomLocation').val()
        },
        //data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success) {
            $('#cardFrontPage').hide();
            $('#cardCoursePdf').attr('hidden', false);
            RunCourses();
            isPausedForCourseDuration = false;
            if (valnew != '0 minutes') {
                startCourseDurationClock();
            }
        }
      



    }).fail(function () {
        console.log('error');
    })
}
var pdfUrl;
var pdfDoc = null, pageNum = 1, scale = 1.5, canvas, ctx  ;
function RunCourses() {
    canvas = document.getElementById("canvasCousePdf");
     ctx = canvas.getContext('2d')
    pdfUrl = '/TA/' + $('#txthrreferencenumber').val() + '/Course/' + $('#txtCoursefilename').val()  ;
    pdfjsLib.getDocument(pdfUrl).promise.then(function (pdf) {
        pdfDoc = pdf;
        renderPage(pageNum);
    });
}


function renderPage(num) {
    pdfDoc.getPage(num).then(function (page) {
        var fixedWidth = 1110;  
        var fixedHeight = 600; 
        var viewport = page.getViewport({ scale: scale });
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        page.render({ canvasContext: ctx, viewport: viewport });
    });
}

$("#coursePdfNext").on('click', function (e) {
    if (pageNum < pdfDoc.numPages) {
        if (isPausedForCourseDuration) {
            pageNum = pageNum + 2;
            isPausedForCourseDuration = false;
        }
        else {
            //setTimeout(() => {
                pageNum++;
            //}, 30000);

        }
        renderPage(pageNum);
    }
    else {
        isPausedForCourseDuration = true;
        /* getGuardCourseSpentTimeDetails();*/
        $('#cardFrontPage').hide();
        $('#cardCoursePdf').hide();
        $('#cardTestFrontPage').attr('hidden', false);
    }
});

$("#coursePdfPrev").on('click', function (e) {
    if (pageNum > 1) {
        isPausedForCourseDuration = true;
        pageNum--;
        renderPage(pageNum);
    }
});
function flashArrow(arrow) {
    $(arrow).css('color', 'green');
    //setTimeout(() => {
    //    $(arrow).css('color', 'black');
    //}, 150);
}
$("#coursePdfPrev, #coursePdfNext").on('mouseleave',
    //function () {
    //    $(this).css("color", "red"); // mouse enter
    //},
    function () {
        $(this).css("background-color", "white"); // mouse leave
    }
);
$("#coursePdfPrev, #coursePdfNext").on('mouseenter',
    //function () {
    //    $(this).css("color", "red"); // mouse enter
    //},
    function () {
        $(this).css("background-color", "green"); // mouse leave
    }
);
document.addEventListener("keydown", function (event) {   


    if (event.key === "ArrowLeft") {
        if (pageNum > 1) {
            isPausedForCourseDuration = true;
            pageNum--;
            renderPage(pageNum);
            $('#coursePdfPrev').css("background-color", "green");
            $('#coursePdfNext').css("background-color", "white");
            
        }
    } else if (event.key === "ArrowRight") {
        if (pageNum < pdfDoc.numPages) {
            if (isPausedForCourseDuration) {
                pageNum = pageNum + 2;
                isPausedForCourseDuration = false;
            }
            else {
                //setTimeout(() => {
                    pageNum++;
                //}, 30000);
            }
            renderPage(pageNum);
            $('#coursePdfNext').css("background-color", "green");
            $('#coursePdfPrev').css("background-color", "white");
        }
        else {
            isPausedForCourseDuration = true;
            /* getGuardCourseSpentTimeDetails();*/
            $('#cardFrontPage').hide();
            $('#cardCoursePdf').hide();
            $('#cardTestFrontPage').attr('hidden', false);
        }

    }
    else if (event.key === "PrintScreen") {
    
       // setInterval(detectScreenRecording, 1000);
        alert('Screenshots are disabled!');
    }

    /*Commented its affecting all pages ,not only training */
    /*
    if (event.ctrlKey == true && event.key === 'c') {
        event.preventDefault();  // Disable Ctrl+C
        alert('Copying is disabled!');
    }*/
    
});


//async function detectScreenRecording() {
//    try {
//        const stream = await navigator.mediaDevices.getDisplayMedia({ video: false });
//        stream.getTracks().forEach(track => track.stop()); // Stop the screen capture
//        alert("Screen recording is not allowed!");
//    } catch (err) {
//        console.log("Screen recording not detected.");
//    }
//}

//// Run this check every 5 seconds
//setInterval(detectScreenRecording, 5000);

////$(document).on('contextmenu', function (e) {
////    e.preventDefault();  // Prevent right-click
////});
var isPausedForCourseDuration = false
let nIntervCourseDurationId;
let DuressAlarmNotificationPending = false;
var valnew; 
if ($('#txtCourseTimerStart').val() != undefined) {
    valnew = $('#txtCourseTimerStart').val();
}
else {
    valnew = '0 minutes';
}
var minutesnew =valnew.split("minutes"); 
let courseduration = 60 * parseInt(minutesnew[0],10);
function startCourseDurationClock() {
   
    
    let timer = courseduration, minutes, seconds;
    display = document.querySelector('#courseTimerStart');
    if (!nIntervCourseDurationId) {
        nIntervCourseDurationId = setInterval(function () {

            if (!isPausedForCourseDuration) {
                
                minutes = parseInt(timer / 60, 10);
                seconds = parseInt(timer % 60, 10);

                minutes = minutes < 10 ? "0" + minutes : minutes;
                seconds = seconds < 10 ? "0" + seconds : seconds;

                display.textContent = minutes + " min" + " " + seconds + " sec";

                if (--timer < 0) {
                    ClearTimerAndReload();
                }
                else if ((timer > 5) && DuressAlarmNotificationPending) {
                    ClearTimerAndReload();
                }
            }
        }, 1000);
    }
}
function ClearTimerAndReload() {
    clearInterval(nIntervCourseDurationId);
    DuressAlarmNotificationPending = false;
    nIntervCourseDurationId = null;
    isPausedForCourseDuration = true;
     //deleteGuardAttendedQuestions(2);
    //returnCoursetestStatustostart();
    //location.reload();
    getTimerMessage();
}
//function getGuardCourseSpentTimeDetails() {
//    var obj =
//    {
//        Id: 0,
//        GuardtrainingAndAssementId: $('#txtguardCourseStatusId').val(),
//        TimeSpentOnCourse: $('#courseTimerStart').html()

//    }
//    $('#loader').show();
//    $.ajax({
//        url: '/Admin/Settings?handler=SaveGuardCourseTime',
//        data: { record: obj },
//        type: 'POST',
//        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
//    }).done(function (result) {
//        if (result.success) {
//            $('#rplDetailsModal').modal('hide');
//            gridCertificatesDocumentFiles.clear();
//            gridCertificatesDocumentFiles.reload({ type: $('#HrSettings_Id').val() });


//        } else {

//            displayGuardValidationSummary('rplValidationSummary', result.message);
//        }
//    }).always(function () {
//        $('#loader').hide();
//    });
//}
$('#btnStartTest').on('click', function (e) {
    e.preventDefault();
    $('#cardFrontPage').hide();
    
        $('#cardCoursePdf').hide();
    $('#cardTestFrontPage').hide();
    $('#cardTestPage').attr('hidden', false);
    
    $('#divTestDurationTimer').attr('hidden', false);
    isPausedForTestDuration = false;
    if (valnewfortest != '0 minutes') { 
        
        startTestDurationClock();
    }
    GetQuestionsForGuard();
   
});

var isPausedForTestDuration = true
let nIntervCourseDurationTestId;
let DuressAlarmNotificationPendingForTest = false;
var valnewfortest;
if ($('#txtTestTimerStart').val() != undefined) {
    valnewfortest = $('#txtTestTimerStart').val();
}
else {
    valnewfortest = '0 minutes';
}
var minutesnewfortest = valnewfortest.split("minutes");
let testduration = 60 * parseInt(minutesnewfortest[0], 10);
function startTestDurationClock() {


    let timer = testduration, minutes, seconds;
    display = document.querySelector('#testTimerStart');
    if (!nIntervCourseDurationTestId) {
        nIntervCourseDurationTestId = setInterval(function () {

            if (!isPausedForTestDuration) {

                minutes = parseInt(timer / 60, 10);
                seconds = parseInt(timer % 60, 10);

                minutes = minutes < 10 ? "0" + minutes : minutes;
                seconds = seconds < 10 ? "0" + seconds : seconds;

                display.textContent = minutes + " min" + " " + seconds + " sec";
             
                if (--timer < 0) {
                    ClearTimerAndReloadForTest();
                }
                else if ((timer > 5) && DuressAlarmNotificationPendingForTest) {
                    ClearTimerAndReloadForTest();
                }
            }
        }, 1000);
    }
}
function ClearTimerAndReloadForTest() {
    clearInterval(nIntervCourseDurationTestId);
    DuressAlarmNotificationPendingForTest = false;
    nIntervCourseDurationTestId = null;
    isPausedForTestDuration = true;
     //deleteGuardAttendedQuestions(2);
   // returnCoursetestStatustostart();
    //location.reload();
    getTimerMessage();
    //GetGuardMarks();

}
function getTimerMessage() {
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardMarks',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            $('#cardFrontPage').hide();

            $('#cardCoursePdf').hide();
            $('#cardTestFrontPage').hide();
            $('#cardTestPage').hide();
            $('#cardResultPage').attr('hidden', false);
            $('#resultHeading').html('Time Has Expired');
            $('#ResultIconCross').attr('hidden', false);
            $('#ResultIconTrophy').hide();
            message = 'You have not passed the test.Please press Retry to try again or Exit to try again in a later date.'
            $('#btnContinueTest').hide();
            $('#btnRetryTest').attr('hidden', false);
            $('#btnExitTest').attr('hidden', false);


            $('#ResultMessage').html(message);
            $('#divTestDurationTimer').hide();
            $('#QuestionCounts').html('');
            $('#lblTotalQuestionscount').html(result.totalQuestions);
            $('#lblCorrectQuestionscount').html(result.guardCorrectQuestionsCount);
            $('#lblTotalScore').html(result.guardScore);
            $('#lblAttempts').html(1);
            $('#lblDuration').html('02:33');
        }
    
        }).fail(function () {
            console.log('error');
        })
}
function GetQuestionsForGuard() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardQuestions',
        data: {
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val(),
            'guardId': $('#txtguardIdForTest').val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
         /*   $('#GuardTestQuestions').html(result.question);*/
            $('#txtGuardTestQuestionId').val(result.id);
            GetOptionsForGuard();
            GetQuestionCountForGuard(result.question);

        }
        else {
            //alert('Something Wrong')
            isPausedForTestDuration = true;
            PostGuardMarks();
          
        }



    }).fail(function () {
        console.log('error');
    })
}

function GetOptionsForGuard() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardOptions',
        data: {
            'questionId': $("#txtGuardTestQuestionId").val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.length > 0) {
            j = 1;
            $('#txtGuardAnswer1').val('');
            $('#txtGuardAnswer2').val('');
            $('#txtGuardAnswer3').val('');
            $('#txtGuardAnswer4').val('');
            $('#txtGuardAnswer5').val('');
            $('#txtGuardAnswer6').val('');
            $('#chkGuardAnswer1').attr('hidden', 'hidden');
            $('#chkGuardAnswer1').prop('checked', false);
            $('#chkGuardAnswer2').attr('hidden', 'hidden');
            $('#chkGuardAnswer2').prop('checked', false);
            $('#chkGuardAnswer3').attr('hidden', 'hidden');
            $('#chkGuardAnswer3').prop('checked', false);
            $('#chkGuardAnswer4').attr('hidden', 'hidden');
            $('#chkGuardAnswer4').prop('checked', false);
            $('#chkGuardAnswer5').attr('hidden', 'hidden');
            $('#chkGuardAnswer5').prop('checked', false);
            $('#chkGuardAnswer6').attr('hidden', 'hidden');
            $('#chkGuardAnswer6').prop('checked', false);
            $('#chkGuardAnswer1').get(0).nextSibling.nodeValue = '';
            $('#chkGuardAnswer2').get(0).nextSibling.nodeValue = '';
            $('#chkGuardAnswer3').get(0).nextSibling.nodeValue = '';
            $('#chkGuardAnswer4').get(0).nextSibling.nodeValue = '';
            $('#chkGuardAnswer5').get(0).nextSibling.nodeValue = '';
            $('#chkGuardAnswer6').get(0).nextSibling.nodeValue = '';
            $('#chkGuardAnswer1').after('');
            $('#chkGuardAnswer2').after('');
            $('#chkGuardAnswer3').after('');
            $('#chkGuardAnswer4').after('');
            $('#chkGuardAnswer5').after('');
            $('#chkGuardAnswer6').after('');
            
            $.each(result, function (i, d) {

                var txtoptions = 'txtGuardAnswer' + j;
                var label = 'lblGuardAnswer' + j
                var chkbox = 'chkGuardAnswer' + j
                j++;
                $('#' + txtoptions).val(d.id);
                //$('#' + label).html(d.options);
                $('#' + label).attr('hidden', false);
                $('#' + chkbox).attr('hidden', false);
                $('#' + chkbox).prop('checked', false);
                $('#' + chkbox).get(0).nextSibling.nodeValue = '';
                $('#' + chkbox).after(d.options);

            });
           
        }
        else {
            alert('Something Wrong')
        }



    }).fail(function () {
        console.log('error');
    })
}
function GetQuestionCountForGuard(question) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardQuestionCount',
        data: {
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val(),
            'guardId': $('#txtguardIdForTest').val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            $('#QuestionCounts').html('Question ' + result.countid + ' of ' + result.totalQuestions );
           // $('#QuestionNo').html('Q.' + result.qno);
            $('#GuardTestQuestions').html('Q.' + result.qno + "  " + question);
        }
        else {
            //alert('Something Wrong')
            isPausedForTestDuration = true;
        }



    }).fail(function () {
        console.log('error');
    })
}
function PostGuardMarks() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardMarks',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val(),
            'duration':''
        },
        //data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success == true) {
            GetGuardMarks();

        }
        else {
            //alert('Something Wrong')
            isPausedForTestDuration = true;
        }



    }).fail(function () {
        console.log('error');
    })
}
function GetGuardMarks() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardMarks',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            $('#cardFrontPage').hide();
            $('#cardWarningPage').hide();
            $('#cardCoursePdf').hide();
            $('#cardTestFrontPage').hide();
            $('#cardTestPage').hide();
            $('#cardResultPage').attr('hidden', false);

            $('#divTestDurationTimer').hide();
            $('#QuestionCounts').html('');
            $('#lblTotalQuestionscount').html(result.totalQuestions);
            $('#lblCorrectQuestionscount').html(result.guardCorrectQuestionsCount);
            $('#lblTotalScore').html(result.guardScore);
            $('#lblAttempts').html(1);
            $('#lblDuration').html('02:33');
            
            var message;
            
            if (result.isPass == true) {
                $('#resultHeading').html('Congratulations !');
                $('#ResultIconTrophy').attr('hidden', false);
                $('#ResultIconCross').hide();
                message = 'You have successfully passed the test.Please press Continue for your results to be submitted.'
                $('#btnContinueTest').attr('hidden', false);
                $('#btnRetryTest').hide();
                $('#btnExitTest').hide();
                /*$('#ResultMessage').html('You have successfully passed the test.Please press Continue for your results to be submitted')*/
            }
            else {
                $('#resultHeading').html('Requirement not met');
                $('#ResultIconCross').attr('hidden', false);
                $('#ResultIconTrophy').hide();
                message = 'You have not passed the test.Please press Retry to try again or Exit to try again in a later date.'
                $('#btnContinueTest').hide();
                $('#btnRetryTest').attr('hidden', false);
                $('#btnExitTest').attr('hidden', false);
            }
            $('#ResultMessage').html(message)

        }
        else {
            //alert('Something Wrong')
            isPausedForTestDuration = true;
        }



    }).fail(function () {
        console.log('error');
    })
}
$('#chkGuardAnswer1').on('change', function () {

    const isChecked = $(this).is(':checked');

    

    $('#chkGuardAnswer2').prop("checked", false);
    $('#chkGuardAnswer3').prop("checked", false);
    $('#chkGuardAnswer4').prop("checked", false);
    $('#chkGuardAnswer5').prop("checked", false);
    $('#chkGuardAnswer6').prop("checked", false);

});
$('#chkGuardAnswer2').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardAnswer1').prop("checked", false);
    $('#chkGuardAnswer3').prop("checked", false);
    $('#chkGuardAnswer4').prop("checked", false);
    $('#chkGuardAnswer5').prop("checked", false);
    $('#chkGuardAnswer6').prop("checked", false);

});
$('#chkGuardAnswer3').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardAnswer2').prop("checked", false);
    $('#chkGuardAnswer1').prop("checked", false);
    $('#chkGuardAnswer4').prop("checked", false);
    $('#chkGuardAnswer5').prop("checked", false);
    $('#chkGuardAnswer6').prop("checked", false);

});
$('#chkGuardAnswer4').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardAnswer2').prop("checked", false);
    $('#chkGuardAnswer3').prop("checked", false);
    $('#chkGuardAnswer1').prop("checked", false);
    $('#chkGuardAnswer5').prop("checked", false);
    $('#chkGuardAnswer6').prop("checked", false);

});
$('#chkGuardAnswer5').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardAnswer2').prop("checked", false);
    $('#chkGuardAnswer3').prop("checked", false);
    $('#chkGuardAnswer4').prop("checked", false);
    $('#chkGuardAnswer1').prop("checked", false);
    $('#chkGuardAnswer6').prop("checked", false);

});
$('#chkGuardAnswer6').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardAnswer2').prop("checked", false);
    $('#chkGuardAnswer3').prop("checked", false);
    $('#chkGuardAnswer4').prop("checked", false);
    $('#chkGuardAnswer5').prop("checked", false);
    $('#chkGuardAnswer1').prop("checked", false);

});
$('#btnSubmitTest').on('click', function (e) {
    e.preventDefault();
    var answerid;
    if ($('#chkGuardAnswer1').is(':checked') == true) {
        answerid = $('#txtGuardAnswer1').val();
    }
    if ($('#chkGuardAnswer2').is(':checked') == true) {
        answerid = $('#txtGuardAnswer2').val();
    }
    if ($('#chkGuardAnswer3').is(':checked') == true) {
        answerid = $('#txtGuardAnswer3').val();
    }
    if ($('#chkGuardAnswer4').is(':checked') == true) {
        answerid = $('#txtGuardAnswer4').val();
    }
    if ($('#chkGuardAnswer5').is(':checked') == true) {
        answerid = $('#txtGuardAnswer5').val();
    }
    if ($('#chkGuardAnswer6').is(':checked') == true) {
        answerid = $('#txtGuardAnswer6').val();
    }
    var obj = {
        Id: 0,
        GuardId: $('#txtguardIdForTest').val(),
        TrainingCourseId: $('#txtguardTestCourseId').val(),
        TrainingTestQuestionsId: $('#txtGuardTestQuestionId').val(),
        TrainingTestQuestionsAnswersId: answerid,
        IsCorrect: false
    }
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=SaveGuardAnswers',
        data: { record: obj },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {

        if (result.success) {
            GetQuestionsForGuard();
        } 
    }).fail(function () {
        console.log('error');
    }).always(function () {

    });
});

$('#btnRetryTest').on('click', function (e) {
    e.preventDefault();
    deleteGuardAttendedQuestions(1);
    
    location.reload();
    
});
$('#btnExitTest').on('click', function (e) {
    e.preventDefault();
    deleteGuardAttendedQuestions(2);


   

});
function deleteGuardAttendedQuestions(buttonmode) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=DeleteGuardAttendedQuestions',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val()
        },
        //data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success) {
            deleteGuardScores(buttonmode);

        }
        else {
            return;
        }



    }).fail(function () {
        console.log('error');
    })
}
function deleteGuardScores(buttonmode) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=DeleteGuardScores',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val()
        },
        //data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success) {
            if (buttonmode == 1) {
                return;
            }
            else {

                returnCoursetestStatustostart(); 
            }

        }
        else {
            return;
        }



    }).fail(function () {
        console.log('error');
    })
}
function returnCoursetestStatustostart() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=ReturnCourseTestStatusTostart',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val()
        },
        //data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success) {
            // Refresh the previous (parent) tab
            if (window.opener && !window.opener.closed) {

                window.opener.location.reload();
                // window.opener.$('#btnHRDetails').trigger('click');
                //window.opener.gridGuardTrainingAndAssessment.clear().draw();
                //window.opener.gridGuardTrainingAndAssessment.ajax.reload();
            }
            window.close();
            return;

        }
        else {
            return;
        }



    }).fail(function () {
        console.log('error');
    })
}

//p5-Issue6-end
//p6-Issue8-start
$('#btnContinueTest').on('click', function (e) {
    e.preventDefault();
    //  GetCertificateAndFeedBackStatus();
    GetGuardAllTestPass();

    //GetCertificateAndFeedBackStatus();
   // GetGuardCertificate();



});
function GetGuardAllTestPass() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardAllTestPass',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result) {
            GetCertificateAndFeedBackStatus();

        }
        else {
            $('#cardFrontPage').hide();

            $('#cardCoursePdf').hide();
            $('#cardTestFrontPage').hide();
            $('#cardTestPage').hide();
            $('#cardResultPage').hide();
            $('#cardFeedbackPage').hide();
            $('#cardThankyouPage').attr('hidden', false);
        }




    }).fail(function () {
        console.log('error');
    })
}
function GetCertificateAndFeedBackStatus() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardCertificateAndfeedBackStatus',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val()
            //,
            //'tqNumberId': $("#txtGuardTQNumberId").val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.getcertificateSatus.isCertificateHoldUntilPracticalTaken == false) {
            GetCertificate()

        }
        else {
            UpdateStatusToHold()
        }
        if (result.getcertificateSatus.isAnonymousFeedback == true) {
            GetFeedbackQuestionsForGuard();
            $('#cardFrontPage').hide();

            $('#cardCoursePdf').hide();
            $('#cardTestFrontPage').hide();
            $('#cardTestPage').hide();
            $('#cardResultPage').hide();
            $('#cardFeedbackPage').attr('hidden', false);

        }
        if ((result.getcertificateSatus.isCertificateHoldUntilPracticalTaken == true || result.getcertificateSatus.isCertificateHoldUntilPracticalTaken == false)  && result.getcertificateSatus.isAnonymousFeedback == false) {
            $('#cardFrontPage').hide();

            $('#cardCoursePdf').hide();
            $('#cardTestFrontPage').hide();
            $('#cardTestPage').hide();
            $('#cardResultPage').hide();
            $('#cardFeedbackPage').hide();
            $('#cardThankyouPage').attr('hidden', false);
        }
        



    }).fail(function () {
        console.log('error');
    })
}
function UpdateStatusToHold() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=UpdateCourseTestStatusToHold',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val()
        },
        //data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
       


    }).fail(function () {
        console.log('error');
    })
}
function GetCertificate() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardCertificate',
        data: {
            'guardId': $('#txtguardIdForTest').val(),
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'tqNumberId': $("#txtGuardTQNumberId").val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success) {
           // deleteGuardScores(buttonmode);

        }
        else {
            return;
        }



    }).fail(function () {
        console.log('error');
    })
}
function GetFeedbackQuestionsForGuard() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardFeedbackQuestions',
        data: {
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'guardId': $('#txtguardIdForTest').val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            /*$('#GuardFeedbackQuestions').html(result.question);*/
            $('#txtGuardFeedbacktQuestionId').val(result.id);
            GetFeedbackOptionsForGuard();
            GetFeedbackQuestionCountForGuard(result.question);

        }
        else {
            $('#cardFrontPage').hide();

            $('#cardCoursePdf').hide();
            $('#cardTestFrontPage').hide();
            $('#cardTestPage').hide();
            $('#cardResultPage').hide();
            $('#cardFeedbackPage').hide();
            $('#cardThankyouPage').attr('hidden', false);
        }



    }).fail(function () {
        console.log('error');
    })
}

function GetFeedbackOptionsForGuard() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardFeedbackOptions',
        data: {
            'questionId': $("#txtGuardFeedbacktQuestionId").val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.length > 0) {
            j = 1;
            $('#txtGuardFeedbackAnswer1').val('');
            $('#txtGuardFeedbackAnswer2').val('');
            $('#txtGuardFeedbackAnswer3').val('');
            $('#txtGuardFeedbackAnswer4').val('');
            $('#txtGuardFeedbackAnswer5').val('');
            $('#txtGuardFeedbackAnswer6').val('');
            $('#chkGuardFeedbackAnswer1').attr('hidden', 'hidden');
            $('#chkGuardFeedbackAnswer1').prop('checked', false);
            $('#chkGuardFeedbackAnswer2').attr('hidden', 'hidden');
            $('#chkGuardFeedbackAnswer2').prop('checked', false);
            $('#chkGuardFeedbackAnswer3').attr('hidden', 'hidden');
            $('#chkGuardFeedbackAnswer3').prop('checked', false);
            $('#chkGuardFeedbackAnswer4').attr('hidden', 'hidden');
            $('#chkGuardFeedbackAnswer4').prop('checked', false);
            $('#chkGuardFeedbackAnswer5').attr('hidden', 'hidden');
            $('#chkGuardFeedbackAnswer5').prop('checked', false);
            $('#chkGuardFeedbackAnswer6').attr('hidden', 'hidden');
            $('#chkGuardFeedbackAnswer6').prop('checked', false);
            $('#chkGuardFeedbackAnswer1').get(0).nextSibling.nodeValue = '';
            $('#chkGuardFeedbackAnswer2').get(0).nextSibling.nodeValue = '';
            $('#chkGuardFeedbackAnswer3').get(0).nextSibling.nodeValue = '';
            $('#chkGuardFeedbackAnswer4').get(0).nextSibling.nodeValue = '';
            $('#chkGuardFeedbackAnswer5').get(0).nextSibling.nodeValue = '';
            $('#chkGuardFeedbackAnswer6').get(0).nextSibling.nodeValue = '';
            $('#chkGuardFeedbackAnswer1').after('');
            $('#chkGuardFeedbackAnswer2').after('');
            $('#chkGuardFeedbackAnswer3').after('');
            $('#chkGuardFeedbackAnswer4').after('');
            $('#chkGuardFeedbackAnswer5').after('');
            $('#chkGuardFeedbackAnswer6').after('');
            $.each(result, function (i, d) {

                var txtoptions = 'txtGuardFeedbackAnswer' + j;
                var label = 'lblGuardFeedbackAnswer' + j
                var chkbox = 'chkGuardFeedbackAnswer' + j
                j++;
                $('#' + txtoptions).val(d.id);
                //$('#' + label).html(d.options);
                //$('#' + label).attr('hidden', false);
                $('#' + chkbox).attr('hidden', false);
                $('#' + chkbox).prop('checked', false);
                $('#' + chkbox).prev().remove();
                //let textAfter = $('#' + chkbox).get(0).nextSibling.nodeValue.trim();
                $('#' + chkbox).get(0).nextSibling.nodeValue='';
                //alert(textAfter);
                $('#' + chkbox).after(d.options);
                
                

            });

        }
        else {
            alert('Something Wrong')
        }



    }).fail(function () {
        console.log('error');
    })
}
function GetFeedbackQuestionCountForGuard(feedbackquestions) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardFeedbackQuestionCount',
        data: {
            'hrSettingsId': $("#txtGuardHRSettings").val(),
            'guardId': $('#txtguardIdForTest').val()
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result != null) {
            $('#QuestionCounts').html('Question ' + result.countid + ' of ' + result.totalQuestions);
            //$('#FeedbackQuestionNo').html('Q.' + result.qno);
            $('#GuardFeedbackQuestions').html('Q.' + result.qno + ' ' + feedbackquestions);

        }
        else {
            //alert('Something Wrong')
            isPausedForTestDuration = true;
        }



    }).fail(function () {
        console.log('error');
    })
}
$('#chkGuardFeedbackAnswer1').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardFeedbackAnswer2').prop("checked", false);
    $('#chkGuardFeedbackAnswer3').prop("checked", false);
    $('#chkGuardFeedbackAnswer4').prop("checked", false);
    $('#chkGuardFeedbackAnswer5').prop("checked", false);
    $('#chkGuardFeedbackAnswer6').prop("checked", false);

});
$('#chkGuardFeedbackAnswer2').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardFeedbackAnswer1').prop("checked", false);
    $('#chkGuardFeedbackAnswer3').prop("checked", false);
    $('#chkGuardFeedbackAnswer4').prop("checked", false);
    $('#chkGuardFeedbackAnswer5').prop("checked", false);
    $('#chkGuardFeedbackAnswer6').prop("checked", false);

});
$('#chkGuardFeedbackAnswer3').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardFeedbackAnswer2').prop("checked", false);
    $('#chkGuardFeedbackAnswer1').prop("checked", false);
    $('#chkGuardFeedbackAnswer4').prop("checked", false);
    $('#chkGuardFeedbackAnswer5').prop("checked", false);
    $('#chkGuardFeedbackAnswer6').prop("checked", false);

});
$('#chkGuardFeedbackAnswer4').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardFeedbackAnswer2').prop("checked", false);
    $('#chkGuardFeedbackAnswer3').prop("checked", false);
    $('#chkGuardFeedbackAnswer1').prop("checked", false);
    $('#chkGuardFeedbackAnswer5').prop("checked", false);
    $('#chkGuardFeedbackAnswer6').prop("checked", false);

});
$('#chkGuardFeedbackAnswer5').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardFeedbackAnswer2').prop("checked", false);
    $('#chkGuardFeedbackAnswer3').prop("checked", false);
    $('#chkGuardFeedbackAnswer4').prop("checked", false);
    $('#chkGuardFeedbackAnswer1').prop("checked", false);
    $('#chkGuardFeedbackAnswer6').prop("checked", false);

});
$('#chkGuardFeedbackAnswer6').on('change', function () {

    const isChecked = $(this).is(':checked');



    $('#chkGuardFeedbackAnswer2').prop("checked", false);
    $('#chkGuardFeedbackAnswer3').prop("checked", false);
    $('#chkGuardFeedbackAnswer4').prop("checked", false);
    $('#chkGuardFeedbackAnswer5').prop("checked", false);
    $('#chkGuardFeedbackAnswer1').prop("checked", false);

});
$('#btnSubmitFeedback').on('click', function (e) {
    e.preventDefault();
    var answerid;
    if ($('#chkGuardFeedbackAnswer1').is(':checked') == true) {
        answerid = $('#txtGuardFeedbackAnswer1').val();
    }
    if ($('#chkGuardFeedbackAnswer2').is(':checked') == true) {
        answerid = $('#txtGuardFeedbackAnswer2').val();
    }
    if ($('#chkGuardFeedbackAnswer3').is(':checked') == true) {
        answerid = $('#txtGuardFeedbackAnswer3').val();
    }
    if ($('#chkGuardFeedbackAnswer4').is(':checked') == true) {
        answerid = $('#txtGuardFeedbackAnswer4').val();
    }
    if ($('#chkGuardFeedbackAnswer5').is(':checked') == true) {
        answerid = $('#txtGuardFeedbackAnswer5').val();
    }
    if ($('#chkGuardFeedbackAnswer6').is(':checked') == true) {
        answerid = $('#txtGuardFeedbackAnswer6').val();
    }
    var obj = {
        Id: 0,
        GuardId: $('#txtguardIdForTest').val(),
        HrSettingsId: $('#txtGuardHRSettings').val(),
        TrainingTestFeedbackQuestionsId: $('#txtGuardFeedbacktQuestionId').val(),
        TrainingTestFeedbackQuestionsAnswersId: answerid
    }
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=SaveGuardFeedbackAnswers',
        data: { record: obj },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {

        if (result.success) {
            GetFeedbackQuestionsForGuard();
        }
    }).fail(function () {
        console.log('error');
    }).always(function () {

    });
});
$('#btnExitCourse').on('click', function (e) {
    e.preventDefault();

    // Refresh the previous (parent) tab
    if (window.opener && !window.opener.closed) {
       
        window.opener.location.reload();
        // window.opener.$('#btnHRDetails').trigger('click');
        //window.opener.gridGuardTrainingAndAssessment.clear().draw();
        //window.opener.gridGuardTrainingAndAssessment.ajax.reload();
    }
    window.close();
});


//p6-issue8-end
$('#btnSavePracticalDetails').on('click', function () {

    clearGuardValidationSummary('ValidationSummary');
 




    $('#loader').show();
    $.ajax({
        url: '/Admin/Settings?handler=SaveGuardTrainingPracticalDetails',
        data: {
            'guardId': $('#txtPrcticalGuardId').val(),
            'courseId': $('#txtPrcticalCourseId').val(),
            'practicalLocationId': $('#ddlPracticalCourseLocation').val(),
            'instructorId': $('#ddlPracticalCourseInstructorsignOff').val(),
            'practicalDate': $('#Practical_Date_completed').val()
        },
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        if (result.success) {
            GetHoldCertificate(result.hrsettingsId);
            $('#practicalDetailsModal').modal('hide');
            


        } else {

            displayGuardValidationSummary('rplValidationSummary', result.message);
        }
    }).always(function () {
        $('#loader').hide();
    });







});
function GetHoldCertificate(hrSettingsId) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Guard/GuardStartTest?handler=GuardCertificate',
        data: {
            'guardId': $('#txtPrcticalGuardId').val(),
            'hrSettingsId': hrSettingsId,
            'tqNumberId': 0
        },
        //data: { id: record },
        type: 'GET',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.filename) {
            // deleteGuardScores(buttonmode);
            UpdateCourseStatusToComplete(hrSettingsId)

        }
        else {
            return;
        }



    }).fail(function () {
        console.log('error');
    })
}
function UpdateCourseStatusToComplete(hrSettingsId) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Admin/Settings?handler=UpdateCourseStatusToComplete',
        data: {
            'guardId': $('#txtPrcticalGuardId').val(),
            'hrSettingsId': hrSettingsId
        },
        //data: { id: record },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (result) {
        if (result.success) {
            // deleteGuardScores(buttonmode);
           
            gridGuardTrainingAndAssessmentByAdmin.clear().draw();
            gridGuardTrainingAndAssessmentByAdmin.ajax.reload();
            //gridGuardLicensesAndLicence.clear().draw();
            //gridGuardLicensesAndLicence.ajax.reload();
        }
        else {
            return;
        }
    });
}
function getPracticalCourseLocation() {
    const practicalLocationControl = $('#ddlPracticalCourseLocation');
    practicalLocationControl.html('');
    $.ajax({
        url: '/Admin/Settings?handler=TrainingLocation',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            practicalLocationControl.append('<option value="" selected>Select</option>')
            data.map(function (site) {
                practicalLocationControl.append('<option value="' + site.id + '">' + site.location + '</option>');
            });

          
        }
    });
}
function getPracticaInstructorSignOff() {

    const practicalInstructorControl = $('#ddlPracticalCourseInstructorsignOff');
    practicalInstructorControl.html('');
    $.ajax({
        url: '/Admin/Settings?handler=InstructorAndPosition',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            practicalInstructorControl.append('<option value="" selected>Select</option>')
            data.map(function (site) {
                practicalInstructorControl.append('<option value="' + site.id + '">' + site.name + '</option>');
            });

            
        }
    });
}
$('#btnWarningContinueTest').on('click', function (e) {
    e.preventDefault();
    
    $('#cardWarningPage').hide();
    $('#cardFrontPage').hide();

    $('#cardCoursePdf').hide();
    $('#cardTestFrontPage').hide();
    $('#cardTestPage').attr('hidden', false);
  
            GetQuestionsForGuard();
       
});
$('#btnWarningRetryTest').on('click', function (e) {
    e.preventDefault();
    deleteGuardAttendedQuestions(1);

    location.reload();

});