$(function () {
});
$('#btnCourse').on('click', function (e) {
    e.preventDefault();
   
    $('#trainingandAssessmentmodal').modal('show');
    var referenceNumber = $('#list_ReferenceNoNumber').find('option:selected').text() + $('#list_ReferenceNoAlphabet').find('option:selected').text();
    var courseDescription = $('#txtHrSettingsDescription').val();
    $('#training_course_Name').val('HR' + ' ' + referenceNumber + ' ' + courseDescription)
    gridCourseDocumentFiles.clear();
    gridCourseDocumentFiles.reload();
});

$('#btnTestQuestions').on('click', function (e) {
    e.preventDefault();
    $('#trainingandAssessmentmodal').modal('show');
    $('#trainingCourseTab').removeClass('active');
    $('#trainingTestQuestionstab').addClass('active');
    $('#TrainingCourse').removeClass('active');
    $('#TrainingTestQuestions').addClass('active');
    gridCourseDocumentFiles.clear();
    gridCourseDocumentFiles.reload();
});
$('#btnCourseCertificates').on('click', function (e) {
    e.preventDefault();
    $('#trainingandAssessmentmodal').modal('show');
    $('#trainingCourseTab').removeClass('active');
    $('#trainingCertificateTab').addClass('active');
    $('#TrainingCourse').removeClass('active');
    $('#TrainingCertificate').addClass('active');
    gridCourseDocumentFiles.clear();
    gridCourseDocumentFiles.reload();
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
    

});
let gridCourseDocumentFiles = $('#tbl_courseDocumentFiles').grid({
    dataSource: '/Admin/Settings?handler=CourseDocsUsingSettingsId&&type=' + $('#HrSettings_Id').val(),
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command', managementColumn: false },
    columns: [
        { field: 'fileName', title: 'File Name', width: 390 },
        { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 140 },
        { width: 75, field: 'tQNumber', title: 'TQ', align: 'center', type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=TQNumbers', valueField: 'id', textField: 'name' } },
        // { width: 200, renderer: staffDocsButtonRendererCompanySop },
       
        { width: 270, renderer: editTrainingCourseDocsButtonRendererSop },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});
var editTrainingCourseDocsButtonRendererSop;
editTrainingCourseDocsButtonRendererSop = function (value, record, $cell, $displayEl, id, $grid) {
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

