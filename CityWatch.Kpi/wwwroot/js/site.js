/** 
 *  Fix for issues while opening one BS modal over another
 *  https://stackoverflow.com/questions/19305821/multiple-modals-overlay * 
 **/
$(document).ready(function () {
    $(document).on('show.bs.modal', '.modal', function () {
        const zIndex = 1040 + 10 * $('.modal:visible').length;
        $(this).css('z-index', zIndex);
        setTimeout(() => $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack'));
    });

    $(document).on('hidden.bs.modal', '.modal', () => $('.modal:visible').length && $(document.body).addClass('modal-open'));
});

$(function () {

    $('#clientType').on('change', function () {
        resetDashboardUi();

        const option = $(this).val();
        if (!option)
            return;

        $.ajax({
            url: '/dashboard?handler=ClientSites&type=' + encodeURIComponent(option),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#ReportRequest_ClientSiteId').html('');
            $('#ReportRequest_ClientSiteId').append('<option value="">Select</option>');
            data.map(function (site) {
                $('#ReportRequest_ClientSiteId').append('<option value="' + site.value + '">' + site.text + '</option>');
            });
        }).fail(function () { });
    });

    $('#ReportRequest_ClientSiteId').on('change', function () {
        resetDashboardUi();

        const option = $(this).val();
        if (!option)
            return;

        const month = $('#month').val();
        const year = $('#year').val();
        $.ajax({
            url: '/dashboard?handler=ClientSiteKpiSettings&siteId=' + encodeURIComponent(option) + '&month=' + month + '&year=' + year,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            if (data.clientSiteKpiSettings === null) {
                $('#client_site_validation').html('<i class="fa fa-exclamation-triangle mr-2 text-warning" aria-hidden="true"></i>Client Site Settings is missing');
            } else {
                $('#header_img_client_site').removeClass('d-none');
                $('#header_img_client_site').attr('src', data.clientSiteKpiSettings.siteImage);
                const thermal_site_val = data.clientSiteKpiSettings.isThermalCameraSite ? 'Yes' : 'No';
                $('#is_thermal_site').val(thermal_site_val);
                showLastImportDateTime(data.lastImportDateTime);
            }
        }).fail(function () { });
    });

    $('#month').on('change', function () {
        populateLastImportDateTime();
    });

    $('#year').on('change', function () {
        populateLastImportDateTime();
    });

    function populateLastImportDateTime() {
        const siteId = $('#ReportRequest_ClientSiteId').val();
        if (!siteId) return;
        const month = $('#month').val();
        const year = $('#year').val();

        $.ajax({
            url: '/dashboard?handler=LastImportDateTime&siteId=' + encodeURIComponent(siteId) + '&month=' + month + '&year=' + year,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            showLastImportDateTime(data.lastImportDateTime);
        });
    }

    function showLastImportDateTime(lastImportDateTime) {
        $('#last_import_message').html('<i class="fa fa-calendar mr-2 text-info" aria-hidden="true"></i>Last import run @ ' + lastImportDateTime);
    }

    function resetDashboardUi() {
        $('#client_site_validation').html('');
        $('#last_import_message').html('');
        $('#header_img_client_site').addClass('d-none');
        $('#is_thermal_site').val('N/A');
    }

    /***** KPI Report *****/

    var gridReport = $('#monthly_kpi_report').DataTable({
        paging: false,
        searching: false,
        ordering: false,
        info: false,
        data: [],
        columns: [
            { data: 'dayOfDate' },
            { data: 'nameOfDay' },
            { data: 'employeeHours' },
            { data: 'actualEmployeeHours', className: 'empHrsActual' },
            { data: 'imageCount' },
            { data: 'imageCountPerHr', width: '75px' },
            { data: 'wandScanCount' },
            { data: 'wandScanCountPerHr', width: '75px' },
            { data: 'wandPatrolsRatio', width: '110px' },
            { data: 'effortCounterImage', width: '50px' },
            { data: 'effortCounterWand', width: '50px' },
            { data: 'isAcceptableLogFreq' },
            { data: 'incidentCount' },
            { data: 'hasFireOrAlarm' },
            { data: 'imagesTarget', visible: false },
            { data: 'wandScansTarget', visible: false },
            { data: 'dailyKpiClientSiteId', visible: false },
        ],
        'createdRow': function (row, data, index) {
            if (data.imageCountPerHr === null) {
                $('td', row).eq(5).html('N/A');
            } else if (data.imageCountPerHr > 0 && data.imageCountPerHr < data.imagesTarget) {
                $('td', row).eq(5).addClass('cell-below-limit');
            }

            if (data.wandScanCountPerHr === null) {
                $('td', row).eq(7).html('N/A');
            } else if (data.wandScanCountPerHr > 0 && data.wandScanCountPerHr < data.wandScansTarget) {
                $('td', row).eq(7).addClass('cell-below-limit');
            }

            if (data.wandPatrolsRatio === null) {
                $('td', row).eq(8).html('N/A');
            } else if (data.wandPatrolsRatio > 0 && data.wandScanCountPerHr < data.wandScansTarget) {
                $('td', row).eq(8).addClass('cell-below-limit');
            } 

            if (data.isAcceptableLogFreq === null) {
                $('td', row).eq(11).html('-');
            }
            else {
                const val = data.isAcceptableLogFreq ? '< 2hr' : '> 2hr';
                const cellColor = data.isAcceptableLogFreq ? 'cell-avg-above-limit' : 'cell-below-limit';
                $('td', row).eq(11).html(val);
                $('td', row).eq(11).addClass(cellColor);
            }

            if (data.incidentCount) {
                $('td', row).eq(12).addClass('cell-has-ir');
            }

            if (data.hasFireOrAlarm) {
                $('td', row).eq(13).addClass('cell-fire-alarm');
            }
        }
    });

    $('#monthly_kpi_report').on('dblclick', 'tr td.empHrsActual', function (event) {
        const rowData = gridReport.row(this).data();
        $('#empHoursActual').val(rowData.actualEmployeeHours);
        $('#dailyKpiClientSiteId').val(rowData.dailyKpiClientSiteId);
        $('#editingRowIndex').val(gridReport.row(this).index());
        $('#dateOfDay').text(getFormattedDate(rowData.date, false));

        $('#updateActualHoursModal').modal('show');
    });

    $('#btnSaveActualEmpHours').on('click', function () {
        $.ajax({
            url: '/Dashboard?handler=UpdateActualEmployeeHours',
            type: 'POST',
            data: {
                id: $('#dailyKpiClientSiteId').val(),
                actualEmpHours: $('#empHoursActual').val()
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            const rowIndex = $('#editingRowIndex').val();
            let rowData = gridReport.row(rowIndex).data();
            rowData['actualEmployeeHours'] = $('#empHoursActual').val();
            gridReport.row(rowIndex).data(rowData).draw();

            $('#updateActualHoursModal').modal('hide');
            $('#alertRegenerateTable').show();
        });
    });

    $('.btn-submit').on('click', function () {
        const siteId = $('#ReportRequest_ClientSiteId option:selected').val();
        const month = $('#month').val();
        const year = $('#year').val();
        if (siteId === '') {
            alert('Please select a client site');
            return false;
        }

        const withImport = $(this).attr('id') === 'btnImportGenerate' ? true : false;
        $('#loader').show();
        $.ajax({
            url: '/dashboard?handler=GenerateReport',
            type: 'POST',
            dataType: 'json',
            data: {
                'siteId': siteId,
                'month': month,
                'year': year,
                'withImport': withImport
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            gridReport.clear().draw();
            $('#alertRegenerateTable').hide();
            clearKpiResults();
            if (response.success) {
                gridReport.rows.add(response.data.dailyKpiResults).draw();
                const pdfName = response.fileName !== '' ? 'Pdf/Output/' + response.fileName : '#';
                $('#btnExportPdf').attr('href', pdfName);
                $('#btnExportPdf').removeClass('btn-outline-secondary').addClass('btn-outline-primary');
                highlightKpiResults(response);
                if (withImport) {
                    var options = { year: "numeric", month: "short", day: "2-digit", hourCycle: 'h23', hour: '2-digit', minute: 'numeric' };
                    showLastImportDateTime(new Date().toLocaleString('en-Au', options));
                }

            }
            $('#loader').hide();
        }).fail(function () { });
    });

    function highlightKpiResults(response) {
        // header
        let kpiTuneBuffer = 0;
        if (response.header.clientSiteSettings.tuneDowngradeBuffer)
            kpiTuneBuffer = (parseFloat(response.header.clientSiteSettings.tuneDowngradeBuffer) - 1) * 100;

        $('#report_header_1').find('th:eq(0) span:eq(0)').text(response.header.date);
        $('#report_header_1').find('th:eq(0) span:eq(1)').text('@ ' + kpiTuneBuffer.toFixed(0) + '%');
        $('#report_header_1').find('th:eq(1) span').text(response.header.clientSiteSettings.imageTargetText);
        $('#report_header_1').find('th:eq(2) span').text(response.header.clientSiteSettings.wandScanTargetText);
        $('#report_header_1').find('th:eq(3) span').text(response.header.clientSiteSettings.patrolsTargetText);

        let hourlyImageUnit = 'p/hr';
        const daySettingsWithPerDay = response.header.clientSiteSettings.clientSiteDayKpiSettings.filter(s => s.patrolFrequency === 1);
        if (daySettingsWithPerDay.length == response.header.clientSiteSettings.clientSiteDayKpiSettings.length)
            hourlyImageUnit = 'p/24 hr'
        const totalImageHeader = response.header.clientSiteSettings.isThermalCameraSite ? 'Day + Ti Total' : 'Day + Total';
        const hourlyImageheader = response.header.clientSiteSettings.isThermalCameraSite ? 'Ti Only' : '';

        $('#report_header_2').find('th:eq(3) span').text(totalImageHeader);
        $('#report_header_2').find('th:eq(4) span').text(hourlyImageheader + ' ' + hourlyImageUnit);

        // footer
        $('#report_footer_1').find('th:eq(2)').text(parseFloat(response.data.imageCountAverage).toFixed(2));
        $('#report_footer_1').find('th:eq(4)').text(parseFloat(response.data.wandScanAverage).toFixed(2));
        $('#report_footer_1').find('th:eq(5)').text(parseFloat(response.data.wandPatrolsAverage).toFixed(2));
        $('#report_footer_1').find('th:eq(7)').text(parseInt(response.data.notInAcceptableLogFreqCount));
        $('#report_footer_1').find('th:eq(8)').text(parseInt(response.data.irCountTotal));
        $('#report_footer_1').find('th:eq(9)').text(parseInt(response.data.alarmCountTotal));

        const imageCountPercentage = response.data.imageCountPercentage ? parseFloat(response.data.imageCountPercentage).toFixed(2) : 0;
        if (imageCountPercentage >= 100)
            $('#report_footer_2').find('th:eq(2)').removeClass().addClass('cell-avg-above-limit');
        else
            $('#report_footer_2').find('th:eq(2)').removeClass().addClass('cell-avg-below-limit');
        $('#report_footer_2').find('th:eq(2)').text(imageCountPercentage + ' %');

        const wandCountPercentage = response.data.wandScanPercentage ? (response.data.wandScanPercentage).toFixed(2) : 0;
        if (wandCountPercentage >= 100)
            $('#report_footer_2').find('th:eq(4)').removeClass().addClass('cell-avg-above-limit');
        else
            $('#report_footer_2').find('th:eq(4)').removeClass().addClass('cell-avg-below-limit');
        $('#report_footer_2').find('th:eq(4)').text(wandCountPercentage + ' %');

        $('#report_footer_2').find('th:eq(5)').text(parseFloat(response.data.wandPatrolsPercentage).toFixed(2) + ' %');

        const siteScorePercentage = response.data.siteScorePercentage ? parseFloat(response.data.siteScorePercentage).toFixed(2) : 0;
        if (siteScorePercentage >= 100)
            $('#report_footer_2').find('th:eq(7)').removeClass().addClass('cell-avg-above-limit');
        else
            $('#report_footer_2').find('th:eq(7)').removeClass().addClass('cell-avg-below-limit');
        $('#report_footer_2').find('th:eq(7)').text(siteScorePercentage + ' %');
    }

    function clearKpiResults() {
        // header
        $('#report_header_1').find('th:eq(0) span:eq(0)').text('');
        $('#report_header_1').find('th:eq(0) span:eq(1)').text('');
        $('#report_header_1').find('th:eq(1) span').text('');
        $('#report_header_1').find('th:eq(2) span').text('');
        $('#report_header_1').find('th:eq(4) span').text('');
        // footer
        $('#report_footer_1').find('th:eq(2)').text('');
        $('#report_footer_1').find('th:eq(4)').text('');
        $('#report_footer_1').find('th:eq(5)').text('');
        $('#report_footer_1').find('th:eq(7)').text('');

        $('#report_footer_2').find('th:eq(2)').removeClass();
        $('#report_footer_2').find('th:eq(2)').text('');
        $('#report_footer_2').find('th:eq(4)').removeClass();
        $('#report_footer_2').find('th:eq(4)').text('');
        $('#report_footer_2').find('th:eq(5)').text('');
        $('#report_footer_2').find('th:eq(7)').removeClass();
        $('#report_footer_2').find('th:eq(7)').text('');
    }

    $('#btnExportPdf').on('click', function () {
        if ($(this).attr('href') === '#')
            return false;
    });

    $('.report-input').on('change', function () {
        $('#btnExportPdf').attr('href', '#');
        $('#btnExportPdf').removeClass('btn-outline-primary').addClass('btn-outline-secondary');
    });

    /***** Settings *****/
    // Schedules
    let gridSchedules;

    function renderNextRunOn(value, record) {
        if (value === '9999-12-31T23:59:59.997') return 'n/a';
        return renderScheduleDate(value, record, true);
    }

    function renderScheduleEndDate(value, record) {
        if (!value) return 'n/a';
        return renderScheduleDate(value, record, false)
    }

    function renderScheduleDate(value, record, withTime) {
        if (value !== '') {
            const date = new Date(value);
            const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
            if (withTime)
                return date.getDate() + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
            else
                return date.getDate() + ' ' + months[date.getMonth()] + ' ' + date.getFullYear();
        }
    }

    function getFormattedDate(value, withTime) {
        if (value !== '') {
            const date = new Date(value);
            const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
            if (withTime)
                return date.getDate() + ' ' + months[date.getMonth()] + ' ' + date.getFullYear() + ' @ ' + date.toLocaleString('en-Au', { hourCycle: 'h23', timeStyle: 'short' }) + ' Hrs';
            else
                return date.getDate() + ' ' + months[date.getMonth()] + ' ' + date.getFullYear();
        }
    }

    function renderScheduleFrequency(value, record) {
        switch (value) {
            case 0:
                return 'Daily';
            case 1:
                return 'Weekly';
            case 2:
                return 'Monthly';
        }
    }

    function renderIsPaused(value, record) {
        return value ?
            '<i class="fa fa-circle text-danger mx-1"></i>Paused' :
            '<i class="fa fa-circle text-success mx-1"></i>Active'
    }


    $('#sel_schedule').on('change', function () {
        gridSchedules.reload({ type: $(this).val(), searchTerm: $('#search_kw_client_site').val() });
    });

    gridSchedules = $('#kpi_send_schedules').grid({
        dataSource: '/Admin/Settings?handler=KpiSendSchedules',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'projectName', title: 'Project Name', width: 100 },
            { field: 'clientTypes', title: 'Client Types', width: 100 },
            { field: 'clientSites', title: 'Client Sites', width: 180 },
            { title: 'Schedule', renderer: scheduleRenderer, width: 120 },
            { field: 'nextRunOn', title: 'Next Run', renderer: function (value, record) { return renderNextRunOn(value, record); }, width: 75 },
            { field: 'emailTo', title: 'Email Recipients', width: 100 },
            { width: 110, renderer: schButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    function scheduleRenderer(value, record) {
        let scheduleHtml = '';
        scheduleHtml += '<span class="my-1 d-block">From: ' + renderScheduleDate(record.startDate, record, false) + ' </span>';
        scheduleHtml += '<span class="my-1 d-block">To: ' + renderScheduleEndDate(record.endDate, record) + ' </span>';
        scheduleHtml += '<span class="my-1 d-block">' + renderScheduleFrequency(record.frequency, record) + ' @ ' + record.time + ' Hrs </span>';
        scheduleHtml += '<span class="my-1 d-block">Status:' + renderIsPaused(record.isPaused);
        return scheduleHtml;
    }

    function schButtonRenderer(value, record) {
        let buttonHtml = '';
        buttonHtml += '<button class="btn btn-outline-primary mt-2 d-block" data-toggle="modal" data-target="#run-schedule-modal" data-sch-id="' + record.id + '""><i class="fa fa-play mr-2" aria-hidden="true"></i>Run</button>';
        buttonHtml += '<button class="btn btn-outline-primary mr-2 mt-2 d-block" data-toggle="modal" data-target="#schedule-modal" data-sch-id="' + record.id + '" ';
        buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil mr-2"></i>Edit</button>';
        buttonHtml += '<button class="btn btn-outline-danger mt-2 del-schedule d-block" data-sch-id="' + record.id + '""><i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
        return buttonHtml;
    }

    $('#kpi_send_schedules').on('click', '.del-schedule', function () {
        const idToDelete = $(this).attr('data-sch-id');
        if (confirm('Are you sure want to delete this schedule?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteKpiSendSchedule',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
            });
        }

    });

    function scheduleModalOnEdit(scheduleId) {
        $('#loader').show();
        $.ajax({
            url: '/Admin/Settings?handler=KpiSendSchedule&id=' + scheduleId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#scheduleId').val(data.id);
            $('#startDate').val(data.startDate.split('T')[0]);
            if (data.endDate)
                $('#endDate').val(data.endDate.split('T')[0]);
            $('#endDate').attr('min', new Date().toISOString().split('T')[0]);
            $('#frequency').val(data.frequency).change();
            $('#frequency option[value="' + data.frequency + '"]').attr('selected', true);
            $('#time').val(data.time);
            $('#nextRunOn').val(data.nextRunOn);
            $('#emailTo').val(data.emailTo);
            $('#emailBcc').val(data.emailBcc)
            $('#coverSheetType').val(data.coverSheetType);
            $('#cbCoverSheetType').prop('checked', data.coverSheetType === 1);
            $('#isPaused').val(data.isPaused);
            $('#cbIsPaused').prop('checked', !data.isPaused);
            $('#cbIsPausedStatus').html(data.isPaused ? 'Paused' : 'Active');
            $.each(data.kpiSendScheduleClientSites, function (index, item) {
                $('#selectedSites').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
                updateSelectedSitesCount();
            });
            $('#projectName').val(data.projectName);
            $('#summaryNote1').val(data.summaryNote1);
            $('#summaryNote2').val(data.summaryNote2);            
            $('#KpiSendScheduleSummaryNote_ScheduleId').val(data.id);            
            $('#KpiSendScheduleSummaryNote_ForMonth').val(data.noteForThisMonth.forMonth);            
            $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val(data.noteForThisMonth.notes);
            $('#KpiSendScheduleSummaryNote_Id').val(data.noteForThisMonth.id);
            $('#lblRemainingCount').html(getNoteLength(data.noteForThisMonth.notes));            
            $('#summaryNoteMonth').val((new Date()).getMonth() + 1);
            $('#summaryNoteYear').val((new Date()).getFullYear());
            setSummaryImage(data.kpiSendScheduleSummaryImage);            
        }).always(function () {
            $('#loader').hide();
        });
    }

    function clearSummaryImage() {
        $('#summary_image').html('-');
        $('#summary_image_updated').html('-');        
        $('#download_summary_image').hide();
        $('#delete_summary_image').hide();
    }

    function setSummaryImage(summaryImage) {
        if (summaryImage) {
            $('#summary_image').html(summaryImage.fileName);
            $('#summary_image_updated').html(getFormattedDate(summaryImage.lastUpdated, true));
            $('#download_summary_image').attr('href', '/SummaryImage/' + summaryImage.fileName);
            $('#download_summary_image').show();
            $('#delete_summary_image').show();
        }
    }

    $('#delete_summary_image').on('click', function () {
        if (confirm('Are you sure want to delete this file?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteSummaryImage&scheduleId=' + $('#scheduleId').val(),
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.status) {
                    clearSummaryImage();
                }
            }).fail(function () {
                console.log('error')
            });
        }
    });

    $('#upload_summary_image').on('change', function () {
        const file = $('#upload_summary_image').prop("files")[0];
        if (file) {
            const scheduleId = $("#scheduleId").val();
            const formData = new FormData();
            formData.append("SummaryImage", file);
            formData.append("ScheduleId", scheduleId);

            $.ajax({
                type: 'POST',
                url: '/Admin/Settings?handler=UploadSummaryImage',
                data: formData,
                cache: false,
                contentType: false,
                processData: false,
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.success) {
                    setSummaryImage(data);
                }
            }).always(function () {
                $('#upload_summary_image').val('');
            });
        }
    });

    function scheduleModalOnAdd() {
        const dateToday = new Date().toISOString().split('T')[0];
        $('#startDate').val(dateToday);
        $('#startDate').attr('min', dateToday);
        $('#endDate').attr('min', dateToday);
        $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val('');
        $('#summaryNoteMonth').val((new Date()).getMonth() + 1);
        $('#summaryNoteYear').val((new Date()).getFullYear());
    }

    function clearScheduleModal() {
        $('#scheduleId').val('0');
        $('#clientTypeName').val('');
        $('#clientSites').html('<option value="">Select</option>');
        $('#selectedSites').html('');
        updateSelectedSitesCount();
        $('input:hidden[name="clientSiteIds"]').remove();
        $('#clientTypeName option:eq(0)').attr('selected', true);
        $('#startDate').val('');
        $('#startDate').removeAttr('min');
        $('#endDate').val('');
        $('#endDate').removeAttr('min');
        $('#time').val('');
        $('#frequency option').removeAttr('selected');
        $('#frequency').val('');
        $('#nextRunOn').val('');
        $('#sch-modal-validation').html('');
        $('#emailTo').val('');
        $('#emailBcc').val('');
        $('#isPaused').val(false);
        $('#cbIsPaused').prop('checked', true);
        $('#cbIsPausedStatus').html('Active');
        $('#coverSheetType').val(0);
        $('#cbCoverSheetType').prop('checked', false);
        $('#projectName').val('');
        $('#summaryNote1').val('');
        $('#summaryNote2').val('');
        $('#KpiSendScheduleSummaryNote_ScheduleId').val('');
        $('#KpiSendScheduleSummaryNote_ForMonth').val('');
        $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val('');
        $('#KpiSendScheduleSummaryNote_Id').val('');
        clearSummaryImage();
    }

    $('#cbIsPaused').on('change', function () {
        const isChecked = $(this).is(':checked');
        $('#cbIsPausedStatus').html(isChecked ? 'Active' : 'Paused');
        $('#isPaused').val(!isChecked);
    });

    $('#cbCoverSheetType').on('change', function () {
        const isChecked = $(this).is(':checked');
        $('#coverSheetType').val(isChecked ? 1 : 0);
    });

    $('#clientTypeName').on('change', function () {
        const option = $(this).val();
        if (option === '') {
            $('#clientSites').html('');
            $('#clientSites').append('<option value="">Select</option>');
        }

        $.ajax({
            url: '/dashboard?handler=ClientSites&type=' + encodeURIComponent(option),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#clientSites').html('');
            $('#clientSites').append('<option value="">Select</option>');
            data.map(function (site) {
                $('#clientSites').append('<option value="' + site.value + '">' + site.text + '</option>');
            });
        });
    });

    $('#clientSites').on('change', function () {
        const elem = $(this).find(":selected");
        if (elem.val() !== '') {
            const existing = $('#selectedSites option[value="' + elem.val() + '"]');
            if (existing.length === 0) {
                $('#selectedSites').append('<option value="' + elem.val() + '">' + elem.text() + '</option>');
                updateSelectedSitesCount();
            }
        }
    });

    $('#editSelectedSite').on('click', function () {
        if ($('#editSiteTrigger').length === 1) {
            $('#editSiteTrigger').remove()
        }

        const selectedOption = $('#selectedSites option:selected');
        if (selectedOption.length == 0) {
            alert('Please select a site to edit');
        } else if (selectedOption.length > 1) {
            alert('Select only one site to edit');
        } else {
            let triggerButton = '<button type="button" id="editSiteTrigger" style="display:none" data-toggle="modal" data-target="#kpi-settings-modal" ' +
                                    'data-cs-id="' + $(selectedOption).val() + '" data-cs-name="' + $(selectedOption).text() + '"></button>';
            $(triggerButton).insertAfter($(this));
            $('#editSiteTrigger').click();
        }
    });

    $('#removeSelectedSites').on('click', function () {
        $('#selectedSites option:selected').remove();
        updateSelectedSitesCount();
    });

    function updateSelectedSitesCount() {
        $('#selectedSitesCount').text($('#selectedSites option').length);
    }

    $('#schedule-modal').on('shown.bs.modal', function (event) {        
        clearScheduleModal();
        const button = $(event.relatedTarget);
        const isEdit = button.data('action') !== undefined && button.data('action') === 'editSchedule';
        if (isEdit) {
            schId = button.data('sch-id');
            scheduleModalOnEdit(schId);
        } else {
            scheduleModalOnAdd();
        }

        showHideSchedulePopupTabs(isEdit);
    });

    $('#btnSaveSchedule').on('click', function () {
        $("input[name=clientSiteIds]").remove();
        var options = $('#selectedSites option');
        options.each(function () {
            const elem = '<input type="hidden" name="clientSiteIds" value="' + $(this).val() + '">';
            $('#frm_kpi_schedule').append(elem);
        });

        $.ajax({
            url: '/Admin/Settings?handler=SaveKpiSendSchedule',
            type: 'POST',
            data: $('#frm_kpi_schedule').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                $('#schedule-modal').modal('hide');
                alert('Schedule saved successfully');
                gridSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
            } else {
                $('#sch-modal-validation').html('');
                data.message.split(',').map(function (item) { $('#sch-modal-validation').append('<li>' + item + '</li>') });
                $('#sch-modal-validation').show().delay(5000).fadeOut();
            }
        });
    });

    $('#run-schedule-modal').on('shown.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const schId = button.data('sch-id');
        $('#sch-id').val(schId);
        $('#btnScheduleRun').prop('disabled', false);
        $('#schRunStatus').html('');
    });

    $('#btnScheduleRun').on('click', function () {
        $('#btnScheduleRun').prop('disabled', true);
        $('#schRunStatus').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Report. Please wait...');
        $.ajax({
            url: '/Admin/Settings?handler=RunSchedule',
            type: 'POST',
            data: {
                scheduleId: $('#sch-id').val(),
                reportYear: $('#schRunYear').val(),
                reportMonth: $('#schRunMonth').val(),
                ignoreRecipients: $('#cbIgnoreRecipients').is(':checked'),
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            $('#btnScheduleRun').prop('disabled', false);
            const messageHtml = result.success ? '<i class="fa fa-check-circle-o text-success"></i> Done. Report sent via email' :
                                                    '<i class="fa fa-times-circle text-danger"></i> Error. Check log for more details';
            $('#schRunStatus').html(messageHtml);
        });
    });


    $('#btnScheduleDownload').on('click', function () {
        $('#btnScheduleDownload').prop('disabled', true);
        $('#schRunStatus').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating PDF. Please wait...');
        $.ajax({
            type: 'GET',
            url: '/Admin/Settings?handler=DownloadPdf',
            data: {
                scheduleId: $('#sch-id').val(),
                reportYear: $('#schRunYear').val(),
                reportMonth: $('#schRunMonth').val(),
                ignoreRecipients: $('#cbIgnoreRecipients').is(':checked'),
            },
            xhrFields: {
                responseType: 'blob' // For handling binary data
            },
            success: function (data, textStatus, request) {
                var contentDispositionHeader = request.getResponseHeader('Content-Disposition');
                var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                var matches = filenameRegex.exec(contentDispositionHeader);
                var downloadedFileName = matches !== null && matches[1] ? matches[1].replace(/['"]/g, '') : fileName;
                // Create a Blob with the PDF data and initiate the download
                var blob = new Blob([data], { type: 'application/pdf' });
                // Create a temporary anchor element to trigger the download
                var url = window.URL.createObjectURL(blob);                
                // Open the PDF in a new tab
                var newTab = window.open(url, '_blank');
                if (!newTab) {
                    // If the new tab was blocked, fallback to downloading the file
                    var a = document.createElement('a');
                    a.href = url;
                    a.download = downloadedFileName;
                    a.click();
                }
            },
            error: function () {
                alert('Error while downloading the PDF.');
            }
        }).done(function (result) {
            $('#btnScheduleDownload').prop('disabled', false);
            const messageHtml = '';
            $('#schRunStatus').html(messageHtml);
        });
    });

    // Import Jobs
    /* TODO: Remove KPI Import Jobs Function
    const gridImportJobs = $('#kpi_import_jobs').DataTable({
        paging: true,
        pageLength: 25,
        searching: false,
        ordering: false,
        info: false,
        ajax: {
            'url': '/admin/settings?handler=KpiDataImportJobs',
            'dataSrc': ''
        },
        columns: [
            { data: 'id' },
            { data: 'name' },
            { data: 'reportDate' },
            { data: 'createdDate' },
            { data: 'completedDate' },
            {
                data: 'success',
                render: function (data, type, row) {
                    if (data === null) return null;
                    const cls = data ? 'fa-check-circle text-success' : 'fa-times-circle text-danger';
                    return '<i class="fa ' + cls + '"></i >';
                }
            }
        ]
    });

    $('#btnRunService').on('click', function () {
        const siteId = $('#ReportRequest_ClientSiteId option:selected').val();
        if (siteId === '') {
            alert('Please select a client site');
            return false;
        }

        $('#loader').show();
        $.ajax({
            url: '/admin/settings',
            type: 'POST',
            data: $('#frmRunService').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            gridImportJobs.ajax.reload();
        }).fail(function () {
        }).always(function () {
            $('#loader').hide();
        });
    });
    */

    /***** Client Site KPI Settings *****/
    let gridClientSiteSettings;

    function settingsButtonRenderer(value, record) {
        return '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#kpi-settings-modal" ' +
            'data-cs-id="' + record.id + '" data-cs-name="' + record.clientSiteName + '"><i class="fa fa-pencil mr-2"></i>Edit</button>';
    }

    gridClientSiteSettings = $('#kpi_client_site_settings').grid({
        dataSource: '/Admin/Settings?handler=ClientSiteWithSettings',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { width: 150, field: 'clientTypeName', title: 'Client Type' },
            { width: 250, field: 'clientSiteName', title: 'Client Site' },
            { width: 100, field: 'hasSettings', title: 'Settings Available?', renderer: function (value, record) { return value === true ? '<i class="fa fa-check-circle text-success"></i>' : ''; } },
            { width: 100, renderer: settingsButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    $('#cs_client_type').on('change', function () {
        gridClientSiteSettings.clear();
        gridClientSiteSettings.reload({ type: $(this).val() });
    });

    $('#kpi-settings-modal').on('shown.bs.modal', function (event) {
        $('#div_site_settings').html('');
        const button = $(event.relatedTarget);
        $('#client_site_name').text(button.data('cs-name'))
        $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + button.data('cs-id'));
    });

    $('#div_site_settings').on('change', '.patrol-frequency', function () {
        $('input[name="ClientSiteDayKpiSettings[' + $(this).attr('data-index') + '].PatrolFrequency"]').val(Number($(this).prop('checked')));
        const text = $(this).prop('checked') ? 'per day' : 'per hr';
        $(this).next().html(text);
    });

    $('#div_site_settings').on('change', '.patrols-count', function () {
        calculateDailyImagesTarget($(this).attr('data-index'));
        calculateDailyWandScansTarget($(this).attr('data-index'));
    });

    $('#div_site_settings').on('change', 'input[name="TuneDowngradeBuffer"]', function () {
        for (var index = 0; index < 7; index++) {
            calculateDailyImagesTarget(index);
            calculateDailyWandScansTarget(index);
        }
    });

    $('#div_site_settings').on('change', '#PhotoPointsPerPatrol', function () {
        for (var index = 0; index < 7; index++) {
            calculateDailyImagesTarget(index);
        }
    });

    $('#div_site_settings').on('change', '.daily-wand-target', function () {
        for (var index = 0; index < 7; index++) {
            calculateDailyWandScansTarget(index);
        }
    });

    function calculateDailyImagesTarget(index) {
        const photoPointsPerHr = $('#PhotoPointsPerPatrol').val();
        const patrolsPerHr = $('#ClientSiteDayKpiSettings_' + index + '__NoOfPatrols').val();
        const tuneDowngradeBuffer = $('input[name="TuneDowngradeBuffer"]').val();

        if (photoPointsPerHr && patrolsPerHr && tuneDowngradeBuffer) {
            if (tuneDowngradeBuffer > 0) {
                const imagesPerHourTarget = (photoPointsPerHr * patrolsPerHr) / tuneDowngradeBuffer;
                $('#ClientSiteDayKpiSettings_' + index + '__ImagesTarget').val(imagesPerHourTarget.toFixed(2));
            }
        }
    }

    function calculateDailyWandScansTarget(index) {
        const wandPointsPerPatrol = $('#WandPointsPerPatrol').val();
        const patrolsPerHr = $('#ClientSiteDayKpiSettings_' + index + '__NoOfPatrols').val();
        const tuneDowngradeBuffer = $('input[name="TuneDowngradeBuffer"]').val();

        if (wandPointsPerPatrol && patrolsPerHr && tuneDowngradeBuffer) {
            if (tuneDowngradeBuffer > 0) {
                const wandScanPerHourTarget = (wandPointsPerPatrol * patrolsPerHr) / tuneDowngradeBuffer;
                $('#ClientSiteDayKpiSettings_' + index + '__WandScansTarget').val(wandScanPerHourTarget.toFixed(2));
            }
        }
    }

    $('#div_site_settings').on('click', '#delete_site_notes', function () {
        if (confirm('Are you sure want to delete?')) {
            $.ajax({
                url: '/admin/settings?handler=DeleteClientSiteKpiNote',
                type: 'POST',
                data: { noteId: $("#ClientSiteKpiNote_Id").val() },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $("textarea[id='ClientSiteKpiNote_Notes']").val('');                    
                    $('#lblSiteNoteRemainingCount').html(getSiteNoteLength(''));
                }
                else
                    alert(result.message);
            }).fail(function () { });
        }
    });

    $('#div_site_settings').on('click', '#save_site_notes', function () {
        const remainingChars = getNoteRemainingCount($('#ClientSiteKpiNote_Notes').val(), 'site_note');
        if (remainingChars < 0) {
            alert('Error: Notes exceeded limit of 512 characters');
            return;
        }

        $.ajax({
            url: '/admin/settings?handler=ClientSiteKpiNote',
            type: 'POST',
            data: $('#frm_site_note').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status)
                alert('Saved successfully');
            else
                alert(result.message);
            $("#ClientSiteKpiNote_Id").val(result.id);
        }).fail(function () { });
    });

    $('#div_site_settings').on('change', '#note_month', function () {
        populateNotesForYearMonth();
    });

    $('#div_site_settings').on('change', '#note_year', function () {
        populateNotesForYearMonth();
    });

    function populateNotesForYearMonth() {
        $.ajax({
            url: '/admin/settings?handler=ClientSiteKpiNote',
            type: 'GET',
            data: {
                clientSiteId: $('#ClientSiteId').val(),
                month: $('#note_month').val(),
                year: $('#note_year').val()
            }
        }).done(function (result) {
            $("#ClientSiteKpiNote_Id").val(result.id);
            $("textarea[id='ClientSiteKpiNote_Notes']").val(result.notes);
            $('#lblSiteNoteRemainingCount').html(getSiteNoteLength(result.notes));
            $("#ClientSiteKpiNote_ForMonth").val(result.forMonth);
        }).fail(function () { });
    }

    $('#div_site_settings').on('keyup', '#ClientSiteKpiNote_Notes', function () {
        $('#lblSiteNoteRemainingCount').html(getSiteNoteLength($(this).val()));
    });

    $('#div_site_settings').on('change', '#upload_site_image', function () {

        const file = $('#upload_site_image').prop("files")[0];
        if (file) {
            const clientSiteId = $("#ClientSiteId").val();
            const formData = new FormData();
            formData.append("UploadSiteImage", file);
            formData.append("PreviousFileName", $('#SiteImage').val())
            formData.append("ClientSiteId", clientSiteId);

            $.ajax({
                type: 'POST',
                url: '/admin/settings?handler=UploadSiteImage',
                data: formData,
                cache: false,
                contentType: false,
                processData: false,
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                const imagePath = data.success ? data.fileName : '//via.placeholder.com/200x112.png?text=No Image';
                $('#img_site_image').attr('src', imagePath);
                $('#SiteImage').val(imagePath);
            }).fail(function () { });
        }
    });

    $('#div_site_settings').on('click', '#save_site_settings', function () {
        $.ajax({
            url: '/admin/settings?handler=ClientSiteKpiSettings',
            type: 'POST',
            data: $('#frm_site_settings').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            alert('Saved successfully');
            $('#kpi-settings-modal').modal('hide');
            gridClientSiteSettings.clear();
            gridClientSiteSettings.reload({ type: $('#cs_client_type').val() });
        }).fail(function () { });
    });


    $('#div_site_settings').on('click', '#save_site_manning_settings', function () {        
        $.ajax({
            url: '/admin/settings?handler=ClientSiteManningKpiSettings',
            type: 'POST',
            data: $('#frm_site_manning_settings').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            alert('Saved successfully');
            $('#kpi-settings-modal').modal('hide');
            gridClientSiteSettings.clear();
            gridClientSiteSettings.reload({ type: $('#cs_client_type').val() });
        }).fail(function () { });
    });

    $('#div_site_settings').on('click', '#showDivButton', function () {
        $("#hiddenDiv").toggle();
    });


    function handleCheckboxChange() {
        // Get the status of chkGuard and chkPatrolCar
        var chkGuard = $("#chkGuard").prop("checked");
        var chkPatrolCar = $("#chkPatrolCar").prop("checked");       
        if (chkGuard) {
            $("#divGuard").show();

        } else {
            $("#divGuard").hide();
        }
        if (chkPatrolCar) {
            $("#divPatrolCar").show();
        } else {
            $("#divPatrolCar").hide();
        }
        if (chkGuard || chkPatrolCar) {
            $("#divbtn").show();
        }
        else {
            $("#divbtn").hide();
        }
    }

    // Attach the handleCheckboxChange function to the change event of both checkboxes
    $('#div_site_settings').on('change', '#chkGuard', function () {
        handleCheckboxChange();
    });
    $('#div_site_settings').on('change', '#chkPatrolCar', function () {
        handleCheckboxChange();
    });

    checkSelectedValue();
    $('#div_site_settings').on('change', '#ClientSiteManningGuardKpiSettings_1__PositionId', function () {        
        checkSelectedValue();
    });
    $('#div_site_settings').on('change', '#ClientSiteManningPatrolCarKpiSettings_1__PositionId', function () {
        checkSelectedValue();
    });

    function checkSelectedValue() {
        var selectedValueGuard = $("#ClientSiteManningGuardKpiSettings_1__PositionId").val();
        var selectedValuePatrolCar = $("#ClientSiteManningPatrolCarKpiSettings_1__PositionId").val();
        

        if (selectedValueGuard === "") {
            $('[id^="ClientSiteManningGuardKpiSettings"]').each(function () {
                $(this).off();

            });
            $('input[name^="ClientSiteManningGuardKpiSettings"]').prop('disabled', true);
        } else {
            $('[id^="ClientSiteManningGuardKpiSettings"]').each(function () {
                $(this).on();

            });
            $('input[name^="ClientSiteManningGuardKpiSettings"]').prop('disabled', false);
        }


        if (selectedValuePatrolCar === "") {
            $('[id^="ClientSiteManningPatrolCarKpiSettings"]').each(function () {
                $(this).off();

            });
            $('input[name^="ClientSiteManningPatrolCarKpiSettings"]').prop('disabled', true);
        } else {
            $('[id^="ClientSiteManningPatrolCarKpiSettings"]').each(function () {
                $(this).on();

            });
            $('input[name^="ClientSiteManningPatrolCarKpiSettings"]').prop('disabled', false);
        }
      
      
    }
  

    $('#search_kw_client_site').on('keyup', function (event) {
        // Enter key pressed
        if (event.keyCode === 13) {
            gridSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $(this).val() });
        }
    });

    $('#btnSearchClientSite').on('click', function () {
        gridSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
    });


    /***** KPI Send Schedule SummaryNotes *****/
    $('#saveScheduleSummaryNotes').on('click', function () {
        const remainingChars = getNoteRemainingCount($('#KpiSendScheduleSummaryNote_Notes').val(), 'summary_note');
        if (remainingChars < 0) {
            alert('Error: Notes exceeded limit of 2048 characters');
            return;
        }

        $.ajax({
            url: '/admin/settings?handler=KpiSendScheduleSummaryNote',
            type: 'POST',
            data: $('#frm_summary_note').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status)
                alert("Saved succesfully");
            else
                alert(result.message);
            $("#KpiSendScheduleSummaryNote_Id").val(result.id);
        });
    });

    function populateSummaryNotesForYearMonth() {
        $.ajax({
            url: '/admin/settings?handler=KpiSendScheduleSummaryNote',
            type: 'GET',
            data: {
                scheduleId: $('#KpiSendScheduleSummaryNote_ScheduleId').val(),
                month: $('#summaryNoteMonth').val(),
                year: $('#summaryNoteYear').val()
            }
        }).done(function (result) {
            $('#KpiSendScheduleSummaryNote_Id').val(result.id);
            $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val(result.notes)
            $('#KpiSendScheduleSummaryNote_ForMonth').val(result.forMonth);
            $('#lblRemainingCount').html(getNoteLength(result.notes));            
        }).fail(function () { });
    }

    $('#summaryNoteMonth').on('change', function () {
        populateSummaryNotesForYearMonth()
    });

    $('#summaryNoteYear').on('change', function () {
        populateSummaryNotesForYearMonth()
    });

    $('#deleteScheduleSummaryNotes').on('click', function () {

        if (confirm('Are you sure want to delete?')) {
            $.ajax({
                url: '/admin/settings?handler=DeleteKpiSendScheduleSummaryNote',
                type: 'POST',
                data: {
                    id: $('#KpiSendScheduleSummaryNote_Id').val(),
                },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val('');                    
                    $('#lblRemainingCount').html(getNoteLength(''));            
                }
                else
                    alert(result.message);
            });
        }
    });

    $("textarea[id='KpiSendScheduleSummaryNote_Notes']").keyup(function () {
        $('#lblRemainingCount').html(getNoteLength($(this).val()));
    });

    function getNoteLength(note) {
        return 'Remaining characters: ' + getNoteRemainingCount(note, 'summary_note');
    }

    function getSiteNoteLength(note) {
        return 'Remaining characters: ' + getNoteRemainingCount(note, 'site_note');
    }

    function getNoteRemainingCount(note, type) {
        let max_chars = Number.MAX_SAFE_INTEGER;
        if (type === 'summary_note') max_chars = 2048;
        else if (type === 'site_note') max_chars = 512;
        return max_chars - (note ? note.replace(/(\r\n|\n|\r)/g, 'xx').length : 0);        
    }

    function showHideSchedulePopupTabs(isEdit) {
        $('#scheduleEditTab li a').removeClass('active');
        $('#siteScheduleSettings.tab-pane').removeClass('active');
        $('#summaryNotes.tab-pane').removeClass('active');
        $('#summaryImage.tab-pane').removeClass('active');
        $('#scheduleEditTab li').each(function () {
            $(this).hide();
        });

        $('#scheduleEditTab li:first a').addClass('active');
        $('#siteScheduleSettings.tab-pane').addClass('active');
        if ($('#sel_schedule').val() === '0' || !isEdit) {
            $('#scheduleEditTab li:first').show();
        } else {
            $('#scheduleEditTab li').each(function () {
                $(this).show();
            });
        }
    }  



});