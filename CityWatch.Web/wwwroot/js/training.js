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
    LoadTQSettings();
    LoadLastTQNumbers();
    GetNumberOfQuestions();
    LoadLastFeedbackQNumbers();
    GetNumberOfFeedbackQuestions();
});

$('#btnTestQuestions').on('click', function (e) {
    e.preventDefault();
    $('#trainingandAssessmentmodal').modal('show');
    $('#trainingCourseTab').removeClass('active');
    $('#trainingTestQuestionstab').addClass('active');
    $('#TrainingCourse').removeClass('active');
    $('#TrainingTestQuestions').addClass('active');
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var courseDescription = $('#txtHrSettingsDescription').val();
    $('#training_course_Name').html('HR' + ' ' + referenceNumber + ' ' + courseDescription)
    gridCourseDocumentFiles.clear();
    gridCourseDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
    $('#btn_save_trainingassessment_settings').attr('hidden', false);
    $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
    $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
    LoadTQSettings();
    LoadLastTQNumbers();
    GetNumberOfQuestions();
    LoadLastFeedbackQNumbers();
    GetNumberOfFeedbackQuestions();
});
$('#btnCourseCertificates').on('click', function (e) {
    e.preventDefault();
    $('#trainingandAssessmentmodal').modal('show');
    $('#trainingCourseTab').removeClass('active');
    $('#trainingCertificateTab').addClass('active');
    $('#TrainingCourse').removeClass('active');
    $('#TrainingCertificate').addClass('active');
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var courseDescription = $('#txtHrSettingsDescription').val();
    $('#training_course_Name').html('HR' + ' ' + referenceNumber + ' ' + courseDescription)
    gridCertificatesDocumentFiles.clear();
    gridCourseDocumentFiles.reload({ type: $('#HrSettings_Id').val() });
    $('#btn_save_trainingassessment_settings').attr('hidden', true);
    $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
    $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
    LoadTQSettings();
    LoadLastTQNumbers();
    GetNumberOfQuestions();
    LoadLastFeedbackQNumbers();
    GetNumberOfFeedbackQuestions();
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
            '<input type="file" name="upload_course_file_sop" accept=".pdf, .ppt, .pptx" hidden data-doc-id="' + record.id + '" tq-id="' + record.tqNumberId + '">' +
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
                showStatusNotification(true, 'Updated Successfully');
                gridCourseDocumentFiles.clear();
                gridCourseDocumentFiles.reload();
            } else {

                showStatusNotification(false, 'Please try again');
                gridCourseDocumentFiles.edit(id);
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
        }).done(function () {
            gridCourseDocumentFiles.reload();
        }).fail(function () {
            console.log('error')
        });
    }
})


let gridCertificatesDocumentFiles = $('#tbl_certificateDocumentFiles').grid({
    dataSource: '/Admin/Settings?handler=CourseDocsUsingSettingsId&&type=' + $('#HrSettings_Id').val(),
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command', managementColumn: false },
    columns: [
        { field: 'fileName', title: 'File Name', width: 390 },
        { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 140 },
        // { width: 200, renderer: staffDocsButtonRendererCompanySop },

        { width: 270, renderer: editTrainingCertificatesDocsButtonRendererSop },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});
var editTrainingCertificatesDocsButtonRendererSop;
editTrainingCertificatesDocsButtonRendererSop = function (value, record, $cell, $displayEl, id, $grid) {
    var data = $grid.data(),
        $replace = $('<label class="btn btn-success mb-0"><form id="form_file_downloads_company_sop" method="post"><i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_staff_file_company_sop" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
            '</form></label>').attr('data-key', id),
        $downlaod = $('<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>').attr('data-key', id),
        $edit = $('<button class="btn btn-outline-primary ml-2"><i class="gj-icon pencil" style="font-size:15px"></i></button>').attr('data-key', id),
        $delete = $('<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_company_sop" data-doc-id="' + record.id + '"><i class="fa fa-trash"></i></button>').attr('data-key', id),
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
        { data: 'hrGroupText', width: "12%" },
        { data: 'description', width: "27%" },
        { data: 'newNullColumn', width: '15%' },

        {
            targets: -1,
            data: null,
            defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_start_guard_TrainingAndAssessment">Start</button>&nbsp;' +
                '<button type="button" class="btn btn-outline-primary mr-2" name="btn_rpl_guard_TrainingAndAssessment">RPL</button>',
            width: '15%'
        }],
    
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
//p5-Issue1-End

//p5-Issue2-Start
let gridGuardTrainingAndAssessmentByAdmin = $('#tbl_guard_trainingAndAssessment_by_Admin').DataTable({
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
        { data: 'hrGroupText', width: "12%" },
        { data: 'description', width: "27%" },
        { data: 'newNullColumn', width: '15%' },

        {
            targets: -1,
            data: null,
            defaultContent: '',
            width: '15%'
        }],
   
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
    if (!fileExtn || '.pdf,.ppt,.pptx'.indexOf(fileExtn.toLowerCase()) < 0) {
        showModal('Unsupported file type. Please upload a .pdf, .ppt or .pptx file');
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
const showStatusNotification = function (success, message) {
    if (success) {
        $('.toast .toast-header strong').removeClass('text-danger').addClass('text-success').html('Success');
    } else {
        $('.toast .toast-header strong').removeClass('text-success').addClass('text-danger').html('Error');
    }
    $('.toast .toast-body').html(message);
    $('.toast').toast('show');
}
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
    }
    if (tabId == '#TrainingTestQuestionsAnswers') {
        $('#btn_save_trainingassessment_testquestions').attr('hidden', false);
        $('#btn_save_trainingassessment_settings').attr('hidden', true);
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
    }
    if (tabId == '#TrainingTestQuestionsFeedback') {
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', false);
        $('#btn_save_trainingassessment_settings').attr('hidden', true);
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
    }
});
$('#trainingAssesmentTab .nav-item .nav-link').on("click", function (e) {
    var tabId = $(this).attr("href");
    if (tabId == '#TrainingCourse') {
        $('#btn_save_trainingassessment_settings').attr('hidden', true);
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
    }
    if (tabId == '#TrainingTestQuestions') {
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
        $('#btn_save_trainingassessment_settings').attr('hidden', false);
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
    }
    if (tabId == '#TrainingCertificate') {
        $('#btn_save_trainingassessment_feedbackquestions').attr('hidden', true);
        $('#btn_save_trainingassessment_settings').attr('hidden', true);
        $('#btn_save_trainingassessment_testquestions').attr('hidden', true);
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
        QuestionNoId: $("#ddlCourseDurationETA").val(),
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
            LoadNextTQQuestions();
            GetNumberOfQuestions();
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


        $("#ddlTQNo").val(result);
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
            LoadLastFeedbackQNumbers();
            GetNumberOfFeedbackQuestions();
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
