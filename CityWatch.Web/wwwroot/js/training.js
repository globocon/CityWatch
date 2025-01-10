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
var editTrainingCourseDocsButtonRendererSop;
editTrainingCourseDocsButtonRendererSop = function (value, record, $cell, $displayEl, id, $grid) {
    var data = $grid.data(),
        $replace = $('<label class="btn btn-success mb-0"><form id="form_file_downloads_course_sop" method="post"><i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_course_file_sop" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
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
            var newoption = '<option value= " ' + result[i].id + ' " > ' + result[i].name + '</option >';
            select.append(newoption);
        }
        
        
        $('#CourseTQNumber option[value="' + record.tqNumberId + '"]').prop('selected', true);

       
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
       /* { width: 75, field: 'tqNumberName', title: 'TQ', align: 'center', type: 'dropdown', renderer: { dataSource: '/Admin/Settings?handler=TQNumbers', valueField: 'id', textField: 'name' } },*/
        { width: 75, field: 'tqNumberName', title: 'TQ', align: 'center',renderer :editCourseTQNumber},
        // { width: 200, renderer: staffDocsButtonRendererCompanySop },
       
        { width: 270, renderer: editTrainingCourseDocsButtonRendererSop },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});



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
        { data: 'newNullColumn', width: '15%'},
        
        {
            targets: -1,
            data: null,
            defaultContent: '<button type="button" class="btn btn-outline-primary mr-2" name="btn_start_guard_TrainingAndAssessment">Start</button>&nbsp;' +
                '<button type="button" class="btn btn-outline-primary mr-2" name="btn_rpl_guard_TrainingAndAssessment">RPL</button>',
            width: '15%'
        }],
    columnDefs: [{
        targets: 3,
        data: 'fileName',
        render: function (data, type, row, meta) {
            if (data)
                return '<a href="/Uploads/Guards/License/' + row.licenseNo + '/' + row.fileUrl + '" target="_blank">' + data + '</a>';
            return '-';
        }
    }
    ]
});

//p5-Issue1-End
//p5-Issue3-CourseDocumentUpload-start
$('#add_course_document_files').on('change', function () {
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
    
    if (edit)
        fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));

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

