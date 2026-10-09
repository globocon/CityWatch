/** 
 *  Fix for issues while opening one BS modal over another
 *  https://stackoverflow.com/questions/19305821/multiple-modals-overlay * 
 **/
let globalClientSiteAddress = '';
window.onload = function () {
    $.ajax({
        url: '/Admin/Settings?handler=KPIScheduleDeafultMailbox',
        type: 'GET',
        dataType: 'json',
    }).done(function (Emails) {
        $('#txt_defaultEmail').val(Emails.emails);
    });

    //To get the Duress Emails in pageload stop

};
$(document).ready(function () {

    $(document).ajaxSend(function (event, jqxhr, settings) {
        if (settings.url && settings.url.indexOf('/Admin/Settings') !== -1) {
            var userId = $('#hid_userId').val();
            if (userId && userId !== '0' && settings.url.indexOf('uId=') === -1) {
                settings.url += (settings.url.indexOf('?') === -1 ? '?' : '&') + 'uId=' + userId;
            }
        }
    });

    //$(document).on('show.bs.modal', '.modal', function () {
    //    const zIndex = 1040 + 10 * $('.modal:visible').length;
    //    $(this).css('z-index', zIndex);
    //    console.log('show.bs.modal');
    //    setTimeout(() => $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack'));
    //});

    //$(document).on('hidden.bs.modal', '.modal', () => $('.modal:visible').length && $(document.body).addClass('modal-open'));

});

$(function () {

    $('#clientType').on('change', function () {
        resetDashboardUi();

        const option = $(this).val();
        if (!option)
            return;
        var guardId = $("#hid_userId").val();

        $.ajax({
            url: '/dashboard?handler=ClientSitesUsingUserId&type=' + encodeURIComponent(option) + "&&guardId=" + encodeURIComponent(guardId),
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
            { data: 'nameOfDay', width: '75px' },
            { data: 'employeeHours' },
            { data: 'actualEmployeeHours', className: 'empHrsActual' },
            { data: 'imageCount' },
            { data: 'imageCountPerHr', width: '75px' },
            { data: 'wandScanCount' },
            { data: 'wandScanCountPerHr', width: '75px' },
            { data: 'wandPatrolsRatio', width: '110px' },
            { data: 'wandScanFq', width: '50px' },  // <-- NEW COLUMN
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
                $('td', row).eq(12).html('-');
            }
            else {
                const val = data.isAcceptableLogFreq ? '< 2hr' : '> 2hr';
                const cellColor = data.isAcceptableLogFreq ? 'cell-avg-above-limit' : 'cell-below-limit';
                $('td', row).eq(12).html(val);
                $('td', row).eq(12).addClass(cellColor);
            }

            if (data.incidentCount) {
                $('td', row).eq(13).addClass('cell-has-ir');
            }

            if (data.hasFireOrAlarm) {
                $('td', row).eq(14).addClass('cell-fire-alarm');
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
                const pdfName = response.fileName !== '' ? '../Pdf/Output/' + response.fileName : '#';
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

        // footer row 1 - averages
        $('#report_footer_1').find('th:eq(6)').text(parseFloat(response.data.wandFqAverage).toFixed(2));

        $('#report_footer_1').find('th:eq(7)').text(parseInt(response.data.notInAcceptableLogFreqCount));
        $('#report_footer_1').find('th:eq(8)').text(parseInt(response.data.irCountTotal));
        $('#report_footer_1').find('th:eq(9)').text(parseInt(response.data.alarmCountTotal));

        //p2-48 adhoc shift expexted vs actual hrs-start
        $('#report_footer_3').find('th:eq(1)').text(parseFloat(response.data.totalExpectedEmployeeHours).toFixed(2));
        $('#report_footer_3').find('th:eq(2)').text(parseFloat(response.data.totalActualEmployeeHours).toFixed(2));
        //$('#report_footer_3').find('th:eq(4)').text(parseFloat(response.data.imageCountAverage).toFixed(2));
        //$('#report_footer_3').find('th:eq(5)').text(parseFloat(response.data.wandPatrolsAverage).toFixed(2));
        //$('#report_footer_3').find('th:eq(7)').text(parseInt(response.data.notInAcceptableLogFreqCount));
        //$('#report_footer_3').find('th:eq(8)').text(parseInt(response.data.irCountTotal));
        //$('#report_footer_3').find('th:eq(9)').text(parseInt(response.data.alarmCountTotal));
        //p2-48 adhoc shift expexted vs actual hrs-end

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


        // footer row 2 - percentage
        $('#report_footer_2').find('th:eq(6)').text();


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
    let gridTimesheetSchedules;
    let gridKVSchedules;

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

    let sel_schedule_report_type = $('#sel_schedule').val();
    $('#sel_schedule').on('change', function () {
        sel_schedule_report_type = $(this).val(); // $('#sel_schedule').val();
        if (sel_schedule_report_type === '2') {
            $('#add_kpi_schedule').addClass('d-none');
            $('#add_customwand_schedule').removeClass('d-none');
        }
        else {
            $('#add_kpi_schedule').removeClass('d-none');
            $('#add_customwand_schedule').addClass('d-none');
        }
        gridSchedules.reload({ handler: getScheduleUrl(), type: sel_schedule_report_type, searchTerm: $('#search_kw_client_site').val() });
    });

    function getScheduleUrl() {

        //let scheduleType = $('#sel_schedule').val();

        if (sel_schedule_report_type === '2') {
            return 'KpiCustomWandSendSchedules';
        }

        return 'KpiSendSchedules';
    }

    gridSchedules = $('#kpi_send_schedules').grid({
        dataSource: {
            url: '/Admin/Settings',
            data: {
                handler: 'KpiSendSchedules'
            }
        },
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
            { width: 150, renderer: schButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    gridTimesheetSchedules = $('#kpi_send_Timesheetschedules').grid({
        dataSource: '/Admin/Settings?handler=KpiTimesheetSchedules',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'projectName', title: 'Project Name', width: 100 },
            { field: 'clientTypes', title: 'Client Types', width: 100 },
            { field: 'clientSites', title: 'Client Sites', width: 180 },
            { title: 'Schedule', renderer: TimesheetscheduleRenderer, width: 120 },
            { field: 'nextRunOn', title: 'Next Run', renderer: function (value, record) { return renderNextRunOn(value, record); }, width: 75 },
            { field: 'emailTo', title: 'Email Recipients', width: 100 },
            { width: 150, renderer: schButtonRendererTimesheet },
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
    function TimesheetscheduleRenderer(value, record) {
        let scheduleHtml = '';
        scheduleHtml += '<span class="my-1 d-block">From: ' + renderScheduleDate(record.startDate, record, false) + ' </span>';
        scheduleHtml += '<span class="my-1 d-block">To: ' + renderScheduleEndDate(record.endDate, record) + ' </span>';
        scheduleHtml += '<span class="my-1 d-block">' + renderScheduleFrequency(record.frequency, record) + ' @ ' + record.time + ' Hrs </span>';
        scheduleHtml += '<span class="my-1 d-block">Status:' + renderIsPaused(record.isPaused);
        return scheduleHtml;
    }
    function schButtonRenderer(value, record) {
        let buttonHtml = '';
        if (sel_schedule_report_type === '2') {
            buttonHtml += '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#run-customWands-schedule-modal" data-sch-id="' + record.id + '""><i class="fa fa-play mr-2" aria-hidden="true"></i>Run</button>';
            buttonHtml += '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#customWands-schedule-modal" data-sch-id="' + record.id + '" ';
            buttonHtml += 'data-action="editScheduleCustomWand"><i class="fa fa-pencil mr-2"></i>Edit</button>';
            buttonHtml += '<button class="btn btn-outline-danger del-customwand-schedule mr-2" data-sch-id="' + record.id + '""><i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
            buttonHtml += '<button class=" btn mr-2" data-toggle="modal" data-target="#customWands-schedule-modal" data-sch-id="' + record.id + '" ';
            buttonHtml += 'data-action="copyCustomWandSchedule"><i class="fa fa-copy fa-2x mr-2"></i></button>';
            return buttonHtml;
        }
        else {
            buttonHtml += '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#run-schedule-modal" data-sch-id="' + record.id + '""><i class="fa fa-play mr-2" aria-hidden="true"></i>Run</button>';
            buttonHtml += '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#schedule-modal" data-sch-id="' + record.id + '" ';
            buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil mr-2"></i>Edit</button>';
            buttonHtml += '<button class="btn btn-outline-danger del-schedule mr-2 mt-2" data-sch-id="' + record.id + '""><i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
            buttonHtml += '<button class=" btn mr-2 p-0" data-toggle="modal" data-target="#schedule-modal" data-sch-id="' + record.id + '" ';
            buttonHtml += 'data-action="copySchedule"><i class="fa fa-copy fa-2x mr-2"></i></button>';
            return buttonHtml;
        }
    }
    function schButtonRendererTimesheet(value, record) {
        let buttonHtml = '';
        buttonHtml += '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#run-schedule-modal1" data-sch-id="' + record.id + '""><i class="fa fa-play mr-2" aria-hidden="true"></i>Run</button>';
        buttonHtml += '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#TimeSheetschedule-modal" data-sch-id="' + record.id + '" ';
        buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil mr-2"></i>Edit</button>';
        buttonHtml += '<button class="btn btn-outline-danger del-schedule1 mr-2 mt-2" data-sch-id="' + record.id + '""><i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
        buttonHtml += '<button class=" btn mr-2 p-0" data-toggle="modal" data-target="#TimeSheetschedule-modal" data-sch-id="' + record.id + '" ';
        buttonHtml += 'data-action="copySchedule1"><i class="fa fa-copy fa-2x mr-2"></i></button>'; return buttonHtml;
    }
    ///kv-schedule-start
    function KVscheduleRenderer(value, record) {
        let scheduleHtml = '';
        scheduleHtml += '<span class="my-1 d-block">From: ' + renderScheduleDate(record.startDate, record, false) + ' </span>';
        scheduleHtml += '<span class="my-1 d-block">To: ' + renderScheduleEndDate(record.endDate, record) + ' </span>';
        scheduleHtml += '<span class="my-1 d-block">' + renderScheduleFrequency(record.frequency, record) + ' @ ' + record.time + ' Hrs </span>';
        scheduleHtml += '<span class="my-1 d-block">Status:' + renderIsPaused(record.isPaused);
        return scheduleHtml;
    }
    function schButtonRendererKV(value, record) {
        let buttonHtml = '';
        buttonHtml += '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#run-schedule-modal2" data-sch-id="' + record.id + '""><i class="fa fa-play mr-2" aria-hidden="true"></i>Run</button>';
        buttonHtml += '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#KVschedule-modal" data-sch-id="' + record.id + '" ';
        buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil mr-2"></i>Edit</button>';
        buttonHtml += '<button class="btn btn-outline-danger del-schedule1 mr-2 mt-2" data-sch-id="' + record.id + '""><i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
        buttonHtml += '<button class=" btn mr-2 p-0" data-toggle="modal" data-target="#KVschedule-modal" data-sch-id="' + record.id + '" ';
        buttonHtml += 'data-action="copySchedule1"><i class="fa fa-copy fa-2x mr-2"></i></button>'; return buttonHtml;
    }
    gridKVSchedules = $('#kpi_send_KVSchedules').grid({
        dataSource: '/Admin/Settings?handler=KpiKVSchedules',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'projectName', title: 'Project Name', width: 150 },
            { title: 'Schedule', renderer: KVscheduleRenderer, width: 120 },
            { field: 'nextRunOn', title: 'Next Run', renderer: function (value, record) { return renderNextRunOn(value, record); }, width: 150 },
            { field: 'emailTo', title: 'Email Recipients', width: 100 },
            { width: 150, renderer: schButtonRendererKV },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    $('#run-schedule-modal1').on('shown.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const schId = button.data('sch-id');
        $('#sch-id').val(schId);
        $('#btnScheduleRunTime').prop('disabled', false);
        $('#schRunStatus').html('');
    });
    $('#run-schedule-modal2').on('shown.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const schId = button.data('sch-id');
        $('#sch-id').val(schId);
        $('#btnScheduleRunKV').prop('disabled', false);
        $('#schRunStatus').html('');
    });
    $('#run-customWands-schedule-modal').on('shown.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const schId = button.data('sch-id');
        $('#sch-id-CustomWands').val(schId);
        $('#btnScheduleRunCustomWands').prop('disabled', false);
        $('#schRunStatusCustomWands').html('');

        const today = new Date();
        // yyyy-MM-dd format for input value and API
        const formattedDate =
            today.getFullYear() + '-' +
            String(today.getMonth() + 1).padStart(2, '0') + '-' +
            String(today.getDate()).padStart(2, '0');

        $('#reportRunDateCustomWand').attr('max', formattedDate).val(formattedDate);
    });

    $('#btnScheduleRunCustomWands').on('click', function () {
        const startDate = $('#reportRunDateCustomWand').val();

        if (!startDate) {
            alert('Please select report run date.');
            return;
        }

        $('#btnScheduleRunCustomWands').prop('disabled', true);
        $('#btnScheduleDownloadCustomWands').prop('disabled', true);
        $('#schRunStatusCustomWands').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Report. Please wait...');
        $.ajax({
            url: '/Admin/Settings?handler=RunScheduleCustomWand',
            type: 'POST',
            data: {
                scheduleId: $('#sch-id-CustomWands').val(),
                reportDate: $('#reportRunDateCustomWand').val(),
                ignoreRecipients: $('#cbIgnoreRecipientsCustomWands').is(':checked'),
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            $('#btnScheduleRunCustomWands').prop('disabled', false);
            $('#btnScheduleDownloadCustomWands').prop('disabled', false);
            const messageHtml = result.success ? '<i class="fa fa-check-circle-o text-success"></i> Done. Report sent via email' :
                '<i class="fa fa-times-circle text-danger"></i> Error. Check log for more details';
            $('#schRunStatusCustomWands').html(messageHtml);
        });
    });

    $('#btnScheduleDownloadCustomWands').on('click', function () {

        const startDate = $('#reportRunDateCustomWand').val();

        if (!startDate) {
            alert('Please select report run date.');
            return;
        }

        $('#btnScheduleDownloadCustomWands').prop('disabled', true);
        $('#btnScheduleRunCustomWands').prop('disabled', true);

        $('#schRunStatusCustomWands').html(
            '<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Excel. Please wait...'
        );

        $.ajax({
            type: 'GET',
            url: '/Admin/Settings?handler=DownloadExcelCustomWands',
            data: {
                scheduleId: $('#sch-id-CustomWands').val(),
                reportDate: $('#reportRunDateCustomWand').val(),
                ignoreRecipients: $('#cbIgnoreRecipientsCustomWands').is(':checked'),
            },
            xhrFields: {
                responseType: 'blob'
            },

            success: function (data, textStatus, request) {

                var contentDispositionHeader =
                    request.getResponseHeader('Content-Disposition');

                var contentType =
                    request.getResponseHeader('Content-Type');

                var filenameRegex =
                    /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;

                var matches =
                    filenameRegex.exec(contentDispositionHeader);

                var downloadedFileName =
                    matches !== null && matches[1]
                        ? matches[1].replace(/['"]/g, '')
                        : 'download';

                // Create blob using server content type
                var blob = new Blob(
                    [data],
                    {
                        type: contentType
                        // Can be excel or zip
                    }
                );

                const URL = window.URL || window.webkitURL;

                const bloburl = URL.createObjectURL(blob);

                // Download file
                var a = document.createElement('a');

                a.href = bloburl;
                a.download = downloadedFileName;

                document.body.appendChild(a);

                a.click();

                document.body.removeChild(a);

                URL.revokeObjectURL(bloburl);
            },

            error: function () {
                alert('Error while downloading the file.');
                $('#schRunStatusCustomWands').html('Error while downloading the file.');
            },

            complete: function () {
                $('#btnScheduleDownloadCustomWands').prop('disabled', false);
                $('#btnScheduleRunCustomWands').prop('disabled', false);

                $('#schRunStatusCustomWands').html('');
            }
        });
    });

    $('#btnScheduleRunKV').on('click', function () {
        $('#btnScheduleRunKV').prop('disabled', true);
        $('#schRunStatusKV').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Report. Please wait...');
        $.ajax({
            url: '/Admin/Settings?handler=RunScheduleKV',
            type: 'POST',
            data: {
                scheduleId: $('#sch-id').val(),
                reportYear: $('#schRunYear').val(),
                reportMonth: $('#schRunMonth2').val(),
                ignoreRecipients: $('#cbIgnoreRecipientsKV').is(':checked'),
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            $('#btnScheduleRunKV').prop('disabled', false);
            const messageHtml = result.success ? '<i class="fa fa-check-circle-o text-success"></i> Done. Report sent via email' :
                '<i class="fa fa-times-circle text-danger"></i> Error. Check log for more details';
            $('#schRunStatusKV').html(messageHtml);
        });
    });


    $('#btnScheduleDownload2').on('click', function () {
        $('#btnScheduleDownload2').prop('disabled', true);
        $('#schRunStatusKV').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating PDF. Please wait...');
        $.ajax({
            type: 'GET',
            url: '/Admin/Settings?handler=DownloadPdfKV',
            data: {
                scheduleId: $('#sch-id').val(),
                reportYear: $('#schRunYear').val(),
                reportMonth: $('#schRunMonth2').val(),
                ignoreRecipients: $('#cbIgnoreRecipientsKV').is(':checked'),
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
                // // Create a temporary anchor element to trigger the download
                //var url = window.URL.createObjectURL(blob);
                // // Open the PDF in a new tab
                //var newTab = window.open(url, '_blank');

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

                //if (!newTab) {
                //    // If the new tab was blocked, fallback to downloading the file
                //    var a = document.createElement('a');
                //    a.href = url;
                //    a.download = downloadedFileName;
                //    a.click();
                //}
                //window.URL.revokeObjectURL(url);                
            },
            error: function () {
                alert('Error while downloading the PDF.');
            }
        }).done(function (result) {
            $('#btnScheduleDownload2').prop('disabled', false);
            const messageHtml = '';
            $('#schRunStatusKV').html(messageHtml);
        });
    });
    $('#kpi_send_KVSchedules').on('click', '.del-schedule1', function () {
        const idToDelete = $(this).attr('data-sch-id');
        if (confirm('Are you sure want to delete this KV schedule?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteKpiSendScheduleKV',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridKVSchedules.reload();
            });
        }

    });
    function clearScheduleModalKV() {

        $('#kvscheduleId').val('0');
        $('#clientTypeNamekv').val('');
        $('#clientSiteskv').html('<option value="">Select</option>');
        $('#selectedSiteskv').html('');
        updateSelectedSitesKVCount()
        $('input:hidden[name="clientSiteIds"]').remove();
        $('#clientTypeNamekv option:eq(0)').attr('selected', true);
        $('#startDatekv').val('');
        $('#startDatekv').removeAttr('min');
        $('#endDatekv').val('');
        $('#endDatekv').removeAttr('min');
        $('#kvtime').val('');
        $('#frequencykv option').removeAttr('selected');
        $('#frequencykv').val('');
        $('#kvnextRunOn').val('');
        $('#sch-modal-validation').html('');
        $('#emailTokv').val('');
        $('#emailBcckv').val('');

        $('#projectNamekv').val('');
        $('#VehicleRegokv').val('');
        $('#CompanyNamekv').val('');
        $('#SiteLockv').val('');
        kvSitesOptionAdded();


    }
    function scheduleModalOnEditKV(scheduleId) {
        $('#loader').show();
        $.ajax({
            url: '/Admin/Settings?handler=KpiKVSchedule&id=' + scheduleId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {

            $('#kvscheduleId').val(data.id);
            $('#startDatekv').val(data.startDate.split('T')[0]);
            if (data.endDate)
                $('#endDatekv').val(data.endDate.split('T')[0]);
            $('#endDatekv').attr('min', new Date().toISOString().split('T')[0]);
            $('#frequencykv').val(data.frequency).change();
            $('#frequencykv option[value="' + data.frequency + '"]').attr('selected', true);
            $('#kvtime').val(data.time);
            $('#kvnextRunOn').val(data.nextRunOn);
            $('#emailTokv').val(data.emailTo);
            $('#emailBcckv').val(data.emailBcc)

            $.each(data.kpiSendKVClientSites, function (index, item) {
                $('#selectedSiteskv').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
                updateSelectedSitesKVCount();
            });
            kvSitesOptionAdded();
            $('#projectNamekv').val(data.projectName);
            $('#VehicleRegokv').val(data.vehicleRego);
            $('#CompanyNamekv').val(data.companyName);
            $('#SiteLockv').val(data.clientSiteLocationId);

        }).always(function () {
            $('#loader').hide();
        });
    }
    function scheduleModalOnCopyKV1(scheduleId) {
        $.ajax({
            url: '/Admin/Settings?handler=KpiKVSchedule&id=' + scheduleId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            const dateToday = new Date().toISOString().split('T')[0];
            $('#startDatekv').val(dateToday);
            $('#startDatekv').attr('min', dateToday);
            const dateEnd = '2100-01-01'
            $('#endDatekv').val(dateEnd.split('T')[0]);

            $('#kvscheduleId').val(data.id);
            // $('#startDateTimesheet').val(data.startDate.split('T')[0]);
            //if (data.endDate)
            //    $('#endDateTimesheet').val(data.endDate.split('T')[0]);
            //$('#endDateTimesheet').attr('min', new Date().toISOString().split('T')[0]);
            $('#frequencykv').val(data.frequency).change();
            $('#frequencykv option[value="' + data.frequency + '"]').attr('selected', true);
            $('#kvtime').val(data.time);
            $('#kvnextRunOn').val(data.nextRunOn);
            $('#emailTokv').val(data.emailTo);
            $('#emailBcckv').val(data.emailBcc)

            $.each(data.kpiSendKVClientSites, function (index, item) {
                $('#selectedSiteskv').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
                updateSelectedSitesKVCount();
            });
            //$('#projectNameTimesheet').val(data.projectName);

            kvSitesOptionAdded();
            $('#projectNamekv').val(data.projectName);
            $('#VehicleRegokv').val(data.vehicleRego);
            $('#CompanyNamekv').val(data.companyName);
            $('#SiteLockv').val(data.clientSiteLocationId);
        }).always(function () {
            $('#loader').hide();
        });
    }
    function scheduleModalOnAddKV() {
        const dateToday = new Date().toISOString().split('T')[0];
        $('#startDatekv').val(dateToday);
        $('#startDatekv').attr('min', dateToday);
        const dateEnd = '2100-01-01'
        $('#endDatekv').val(dateEnd.split('T')[0]);

        $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val('');

    }
    $('#KVschedule-modal').on('shown.bs.modal', function (event) {
        clearScheduleModalKV();
        const button = $(event.relatedTarget);
        const isEdit = button.data('action') !== undefined && button.data('action') === 'editSchedule';
        if (isEdit) {
            schId = button.data('sch-id');
            scheduleModalOnEditKV(schId);
        } else {
            //P2-103 Duplicate Settings-start
            const isCopy = button.data('action') !== undefined && button.data('action') === 'copySchedule1';
            if (isCopy) {
                schId = button.data('sch-id');
                scheduleModalOnCopyKV1(schId);
            } else {
                scheduleModalOnAddTimesheet();
            }
            //P2-103 Duplicate Settings-end
        }

        // showHideSchedulePopupTabs(isEdit);
    });

    $('#btnSaveKVSchedule').on('click', function () {
        $("input[name=clientSiteIds]").remove();
        var options = $('#selectedSiteskv option');
        options.each(function () {
            const elem = '<input type="hidden" name="clientSiteIds" value="' + $(this).val() + '">';
            $('#frm_kpi_kvschedule').append(elem);
        });

        $.ajax({
            url: '/Admin/Settings?handler=SaveKpiKVSchedule',
            type: 'POST',
            data: $('#frm_kpi_kvschedule').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                $('#KVschedule-modal').modal('hide');
                alert('Schedule saved successfully');
                gridKVSchedules.reload();
            } else {
                $('#sch-modal-validation1').html('');
                data.message.split(',').map(function (item) { $('#sch-modal-validation1').append('<li>' + item + '</li>') });
                $('#sch-modal-validation1').show().delay(5000).fadeOut();
            }
        });
    });
    $('#clientTypeNamekv').on('change', function () {
        const option = $(this).val();
        if (option === '') {
            $('#clientSiteskv').html('');
            $('#clientSiteskv').append('<option value="">Select</option>');
        }

        $.ajax({
            url: '/dashboard?handler=ClientSitesUsingUserId&type=' + encodeURIComponent(option) + "&guardId=" + encodeURIComponent($("#hid_userId").val() || $("#hid_userIdSettings").val() || 0),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#clientSiteskv').html('');
            $('#clientSiteskv').append('<option value="">Select</option>');
            data.map(function (site) {
                $('#clientSiteskv').append('<option value="' + site.value + '">' + site.text + '</option>');

            });
        });
    });

    function kvSitesOptionAdded() {
        //const option = $(this).val();
        const option = $('#selectedSiteskv option').map(function () {
            return $(this).val();
        }).get();
        if (option.length == 0) {
            $('#SiteLockv').html('');
            $('#SiteLockv').append('<option value="">Select</option>');
            $('#CompanyNamekv').html('');
            $('#CompanyNamekv').append('<option value="">Select</option>');

            $('#kvKeyNo').html('');
            $('#kvKeyNo').append('<option value="">Select</option>');
            return;
        }

        $.ajax({
            url: '/Admin/Settings?handler=ClientSiteLocationsAndCompanyDetails&clientSiteIds=' + option.join(';'),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#SiteLockv').html('');
            $('#SiteLockv').append('<option value="">Select</option>');

            $('#CompanyNamekv').html('');
            $('#CompanyNamekv').append('<option value="">Select</option>');

            $('#kvKeyNo').html('');
            $('#kvKeyNo').append('<option value="">Select</option>');
            data.siteLocations.map(function (loc) {
                $('#SiteLockv').append('<option value="' + loc.value + '">' + loc.text + '</option>');
            });
            data.companyDetails.map(function (det) {
                $('#CompanyNamekv').append('<option value="' + det + '">' + det + '</option>');
            });
            data.clientSiteKeys.map(function (key) {
                $('#kvKeyNo').append('<option value="' + key.keyNo + '">' + key.keyNo + '</option>');
            });
        });
    }
    $('#clientSiteskv').on('change', function () {
        const elem = $(this).find(":selected");
        if (elem.val() !== '') {
            const existing = $('#selectedSiteskv option[value="' + elem.val() + '"]');
            if (existing.length === 0) {
                $('#selectedSiteskv').append('<option value="' + elem.val() + '">' + elem.text() + '</option>');
                updateSelectedSitesKVCount();
                kvSitesOptionAdded();
            }
        }
    });
    function updateSelectedSitesKVCount() {
        $('#selectedSitesCountkv').text($('#selectedSiteskv option').length);
    }
    $('#editSelectedSitekv').on('click', function () {
        if ($('#editSiteTriggerkv').length === 1) {
            $('#editSiteTriggerkv').remove()
        }

        const selectedOption = $('#selectedSiteskv option:selected');
        if (selectedOption.length == 0) {
            alert('Please select a site to edit');
        } else if (selectedOption.length > 1) {
            alert('Select only one site to edit');
        } else {

            let triggerButton = '<button type="button" id="editSiteTriggerkv" style="display:none" data-toggle="modal" data-target="#kpi-settings-modal" ' +
                //p1-139 change pop up start
                'data-cs-id="' + $(selectedOption).val() + '" data-cs-name="' + $(selectedOption).text() + '" data-type-tab="KV"></button>';
            //p1-139 change pop up end
            $(triggerButton).insertAfter($(this));
            $('#editSiteTriggerkv').click();

        }
    });
    $('#removeSelectedSiteskv').on('click', function () {
        $('#selectedSiteskv option:selected').remove();
        updateSelectedSitesKVCount();
        kvSitesOptionAdded();
    });

    //kv-schedule-end
    //pcar satrt dileep 02122025

    $('#pcar-modal').on('show.bs.modal', function () {
        $(this).find('input, select, textarea').val('');
    });

    function schButtonRendererPCAR(value, record) {
        let buttonHtml = '';

        // Edit button
        buttonHtml += '<button class="btn btn-outline-primary mr-2 edit-pcar-route" ' +
            'data-route-id="' + record.id + '" ' +
            'data-toggle="modal" data-target="#pcar-modal">' +
            '<i class="fa fa-pencil mr-2"></i>Edit</button>';

        // Delete button
        buttonHtml += '<button class="btn btn-outline-danger del-pcar-route mr-2" ' +
            'data-route-id="' + record.id + '">' +
            '<i class="fa fa-trash mr-2"></i>Delete</button>';

        return buttonHtml;
    }



    gridPCARSchedules = $('#kpi_send_pcarrutes').grid({
        dataSource: '/Admin/Settings?handler=PCARProfiles',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            // Route Name
            { field: 'pcarroutename', title: 'Route Name', width: 150 },

            // Smart Wand allocation (SmartWandId + Phone)
            {
                field: 'smartwandallocation',
                title: 'Smart Wand',
                width: 150,
                renderer: function (value, record) {
                    if (record.smartWandId && record.phoneNumber) {
                        return record.smartWandId + " (" + record.phoneNumber + ")";
                    }
                    return value;
                }
            },

            // Sites as badges
            {
                field: 'sites',
                title: 'Sites',
                width: 300,
                renderer: function (value, record) {
                    if (!value) return "";
                    return value
                        .split(',')
                        .map(s => `<span class="badge badge-info mr-1">${s.trim()}</span>`)
                        .join(" ");
                }
            },
            // Action buttons (existing)
            { width: 150, renderer: schButtonRendererPCAR }
        ],

        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    function createPCARSiteBlock(siteId, siteName, scheduleData, orderNo) {
        const orderStr = orderNo < 10 ? '0' + orderNo : orderNo;
        const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun', 'Pho'];

        let html = `
            <div class="card mb-3 pcar-site-block border-primary" data-site-id="${siteId}">
                <div class="card-header p-2 d-flex justify-content-between align-items-center bg-light border-bottom border-primary">
                    <h6 class="mb-0 text-primary"><strong>List <span class="order-no-label">${orderStr}</span> - <span class="site-name-label">${siteName}</span></strong></h6>
                    <div class="d-flex gap-1 align-items-center">
                        <button type="button" class="btn btn-xs btn-outline-secondary move-up-pcar-site mr-1" title="Move Up">
                            <i class="fa fa-arrow-up"></i>
                        </button>
                        <button type="button" class="btn btn-xs btn-outline-secondary move-down-pcar-site mr-1" title="Move Down">
                            <i class="fa fa-arrow-down"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger remove-pcar-site" title="Remove Site">
                            <i class="fa fa-trash-o mr-1"></i>Remove
                        </button>
                    </div>
                </div>
                <div class="card-body p-2">
                    <input type="hidden" class="pcar-site-id" value="${siteId}">
                    <input type="hidden" class="pcar-order-no" value="${orderNo}">
                    
                    <div class="table-responsive">
                        <table class="table table-bordered table-sm text-center mb-0">
                            <thead class="bg-light small">
                                <tr>
                                    <th></th>
                                    ${days.map(d => `<th>${d}</th>`).join('')}
                                </tr>
                            </thead>
                            <tbody>
                                <tr class="start-times">
                                    <td class="small"><strong>Start</strong></td>
                                    ${days.map(d => `<td><input type="time" class="form-control form-control-sm p-1" name="Start${d}" value="${scheduleData['start' + d] || ''}"></td>`).join('')}
                                </tr>
                                <tr class="end-times">
                                    <td class="small"><strong>End</strong></td>
                                    ${days.map(d => `<td><input type="time" class="form-control form-control-sm p-1" name="End${d}" value="${scheduleData['end' + d] || ''}"></td>`).join('')}
                                </tr>
                                <tr class="visit-counts">
                                    <td class="small"><strong>Visits</strong></td>
                                    ${days.map(d => `<td><input type="number" class="form-control form-control-sm p-1" name="Visit${d}" value="${scheduleData['visit' + d] || '0'}" min="0"></td>`).join('')}
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>`;

        return html;
    }

    function reorderPCARSites() {
        $('#pcar-sites-container .pcar-site-block').each(function (index) {
            const orderNo = index + 1;
            const orderStr = orderNo < 10 ? '0' + orderNo : orderNo;
            $(this).find('.order-no-label').text(orderStr);
            $(this).find('.pcar-order-no').val(orderNo);
            $(this).attr('data-order', orderNo);
        });
    }

    $('#btnAddSiteToPcarRoute').on('click', function () {
        const siteId = $('#clientSitesTimesheetPCR').val();
        const siteName = $('#clientSitesTimesheetPCR option:selected').text();

        if (!siteId || siteId === "") {
            alert("Please select a client site first.");
            return;
        }

        const orderNo = $('#pcar-sites-container .pcar-site-block').length + 1;
        const blockHtml = createPCARSiteBlock(siteId, siteName, {}, orderNo);
        $('#pcar-sites-container').append(blockHtml);

        // Scroll to bottom
        const container = document.getElementById('pcar-sites-container');
        container.scrollTop = container.scrollHeight;
    });

    $(document).on('click', '.remove-pcar-site', function () {
        if (confirm("Remove this site package from the route?")) {
            $(this).closest('.pcar-site-block').remove();
            reorderPCARSites();
            // Auto-save after removal
            savePcarRouteDetails(false); // false = suppress success alert
        }
    });

    $(document).on('click', '.move-up-pcar-site', function () {
        const block = $(this).closest('.pcar-site-block');
        block.prev().before(block);
        reorderPCARSites();
        savePcarRouteDetails(false);
    });

    $(document).on('click', '.move-down-pcar-site', function () {
        const block = $(this).closest('.pcar-site-block');
        block.next().after(block);
        reorderPCARSites();
        savePcarRouteDetails(false);
    });

    $('#btnSavePcCarrute').on('click', function () {
        savePcarRouteDetails(true);
    });

    function savePcarRouteDetails(showSuccessAlert) {
        const routeId = $('#TimeSheetscheduleIdpcr').val();
        if (!routeId || routeId === "0") {
            alert("Please create a profile first!");
            return;
        }

        const siteBlocks = $('#pcar-sites-container .pcar-site-block');
        // Allow saving empty routes (deleting all sites)
        // if (siteBlocks.length === 0) ... removed check

        let siteSchedules = [];
        let hasValidationError = false;
        let validationMsg = "";

        siteBlocks.each(function (index) {
            const block = $(this);
            const siteName = block.find('.site-name-label').text();
            const orderNo = index + 1;

            const schedule = {
                ClientSiteId: block.find('.pcar-site-id').val(),
                OrderNo: orderNo
            };

            const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun', 'Pho'];
            let anyDayFilled = false;

            for (let d of days) {
                const start = block.find(`input[name="Start${d}"]`).val();
                const end = block.find(`input[name="End${d}"]`).val();
                const visits = block.find(`input[name="Visit${d}"]`).val();

                schedule[`Start${d}`] = start;
                schedule[`End${d}`] = end;
                schedule[`Visit${d}`] = visits || 0;

                const hasStart = !!start;
                const hasEnd = !!end;
                const hasVisits = visits > 0;

                const fullyFilled = hasStart && hasEnd && hasVisits;
                const partiallyFilled = hasStart || hasEnd || hasVisits;

                if (partiallyFilled && !fullyFilled) {
                    hasValidationError = true;
                    validationMsg = `Site ${siteName}: Start, End, and Visits (>0) must be filled for ${d}.`;
                    return false;
                }

                if (fullyFilled) {
                    anyDayFilled = true;
                    if (start >= end) {
                        hasValidationError = true;
                        validationMsg = `Site ${siteName}: End Time must be greater than Start Time for ${d}.`;
                        return false;
                    }
                }
            }

            if (hasValidationError) return false;

            if (!anyDayFilled) {
                hasValidationError = true;
                validationMsg = `Site ${siteName}: Please fill at least one day completely.`;
                return false;
            }

            siteSchedules.push(schedule);
        });

        if (hasValidationError) {
            alert(validationMsg);
            return;
        }

        const payload = {
            PcarRouteId: routeId,
            SiteSchedules: siteSchedules
        };
        console.log('Saving PCAR Route Details:', payload);

        $.ajax({
            url: '/Admin/Settings?handler=SaveRouteDetails',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                if (showSuccessAlert) {
                    alert('All Schedule Details Saved Successfully');
                }
            } else {
                alert(data.message);
            }
        });
    }



    $('#btnCreatePcarProfile').on('click', function () {
        var name = $('#pcarroutename').val();
        var smartWand = $('#smartwandallocation').val();

        $.ajax({
            url: '/Admin/Settings?handler=CreateProfile',
            type: 'POST',
            data: { routeId: $('#TimeSheetscheduleIdpcr').val(), pcarroutename: name, smartwandallocation: smartWand },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                $('#TimeSheetscheduleIdpcr').val(data.routeId);
                togglePcarBottomSection();
                alert('Profile Created Successfully');
            } else {
                alert(data.message);
            }
        });

    });


    $('#clientTypeNameTimesheetpcr').on('change', function () {
        const option = $(this).val();
        if (option === '') {
            $('#clientSitesTimesheetPCR').html('');
            $('#clientSitesTimesheetPCR').append('<option value="">Select</option>');
        }

        $.ajax({
            url: '/dashboard?handler=ClientSitesUsingUserId&type=' + encodeURIComponent(option) + "&guardId=" + encodeURIComponent($("#hid_userId").val() || $("#hid_userIdSettings").val() || 0),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#clientSitesTimesheetPCR').html('');
            $('#clientSitesTimesheetPCR').append('<option value="">Select</option>');
            data.map(function (site) {
                $('#clientSitesTimesheetPCR').append('<option value="' + site.value + '">' + site.text + '</option>');
            });

            // ⬇⬇⬇ THE IMPORTANT PART (FIX) ⬇⬇⬇
            if (window.firstDetailToSelect) {
                $('#clientSitesTimesheetPCR')
                    .val(window.firstDetailToSelect.clientSiteId)
                    .trigger('change');
            }
            // ⬆⬆⬆ END FIX ⬆⬆⬆
        });
    });


    //$('#btnSavePcCarrute').on('click', function () {
    //    var routeId = $('#TimeSheetscheduleIdpcr').val();

    //    if (!routeId || routeId === "0") {
    //        alert("Please create a profile first!");
    //        return;
    //    }

    //    // Get selected client site ID(s) from the dropdown
    //    var selectedSites = $('#clientSitesTimesheetPCR').val(); // Array of selected site IDs

    //    if (!selectedSites || selectedSites.length === 0) {
    //        alert("Please select at least one client site.");
    //        return;
    //    }

    //    // Collect schedule details
    //    var scheduleData = {
    //        PcarRouteId: routeId,
    //        ClientSiteIds: selectedSites, // only the selected sites
    //        StartMon: $('#startMon').val(),
    //        EndMon: $('#endMon').val(),
    //        VisitMon: $('#visitMon').val(),
    //        StartTue: $('#startTue').val(),
    //        EndTue: $('#endTue').val(),
    //        VisitTue: $('#visitTue').val(),
    //        StartWed: $('#startWed').val(),
    //        EndWed: $('#endWed').val(),
    //        VisitWed: $('#visitWed').val(),
    //        StartThu: $('#startThu').val(),
    //        EndThu: $('#endThu').val(),
    //        VisitThu: $('#visitThu').val(),
    //        StartFri: $('#startFri').val(),
    //        EndFri: $('#endFri').val(),
    //        VisitFri: $('#visitFri').val(),
    //        StartSat: $('#startSat').val(),
    //        EndSat: $('#endSat').val(),
    //        VisitSat: $('#visitSat').val(),
    //        StartSun: $('#startSun').val(),
    //        EndSun: $('#endSun').val(),
    //        VisitSun: $('#visitSun').val(),
    //        StartPho: $('#startPho').val(),
    //        EndPho: $('#endPho').val(),
    //        VisitPho: $('#visitPho').val()
    //    };

    //    $.ajax({
    //        url: '/Admin/Settings?handler=SaveRouteDetails',
    //        type: 'POST',
    //        data: scheduleData,
    //        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    //    }).done(function (data) {
    //        if (data.success) {
    //            alert('Schedule Details Saved Successfully');
    //            // Reload updated route details
    //            const selectedSiteId = $('#clientSitesTimesheetPCR').val(); // get current site
    //            if (selectedSiteId) {
    //                loadRouteDetailsForSite(selectedSiteId);
    //            }
    //        } else {
    //            alert(data.message);
    //        }
    //    });
    //});

    function loadRouteDetailsForSite(siteId) {
        const routeId = $('#TimeSheetscheduleIdpcr').val();
        if (!routeId || !siteId) return;

        $.ajax({
            url: '/Admin/Settings?handler=PCARRouteDetails&id=' + routeId,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            // Store routeDetails globally if needed
            currentRouteDetails = data.routeDetails;

            // Clear previous options
            $('#selectedSitesTimeSheetpcar').empty();
            // Populate dropdown with route details
            $.each(data.routeDetails, function (index, item) {
                $('#selectedSitesTimeSheetpcar').append(
                    '<option value="' + item.clientSiteId + '">' + item.clientSiteName + '</option>'
                );
            });
            updateSelectedSitesCountPCAR();
            // Find details for the selected site
            const rd = currentRouteDetails.find(x => x.clientSiteId == siteId);
            if (rd) {
                fillScheduleInputs(rd); // Fill the time/visit inputs
            } else {
                clearScheduleInputs();
            }

            // Make sure the site is selected in the dropdown
            $('#clientSitesTimesheetPCR').val(siteId).trigger('change');
        });
    }



    $(document).on('click', '.edit-pcar-route', function () {
        var routeId = $(this).data('route-id');
        $('#TimeSheetscheduleIdpcr').val(routeId);
        togglePcarBottomSection();
        loadPCARRoute(routeId); // your AJAX function to fill modal
    });

    $(document).on('click', '.del-pcar-route', function () {
        var routeId = $(this).data('route-id');
        if (!routeId || routeId === 0) {
            alert('Invalid profile ID.');
            return;
        }

        if (!confirm('Are you sure you want to delete this PCAR Profile and Rutes ? This action cannot be undone.')) return;

        $.ajax({
            url: '/Admin/Settings?handler=DeletePCarProfile',
            type: 'POST',
            data: { RouteId: routeId }, // plain form data
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                alert(data.message || 'Profile deleted successfully.');

                // Reload the grid
                if (gridPCARSchedules) {
                    gridPCARSchedules.reload();
                }
            } else {
                alert(data.message || 'Failed to delete profile.');
            }
        });

    });

    function loadPCARRoute(routeId) {
        $('#loader').show();
        $('#pcar-sites-container').empty();

        $.ajax({
            url: '/Admin/Settings?handler=PCARRouteDetails&id=' + routeId,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            currentRouteDetails = data.routeDetails || [];

            $('#pcarroutename').val(data.pcarroutename);
            $('#smartwandallocation').val(data.smartwandallocation);

            // Render site blocks in order
            $.each(currentRouteDetails, function (index, rd) {
                const blockHtml = createPCARSiteBlock(rd.clientSiteId, rd.clientSiteName, rd, index + 1);
                $('#pcar-sites-container').append(blockHtml);
            });

            if (currentRouteDetails.length > 0) {
                const firstDetail = currentRouteDetails[0];
                $('#clientTypeNameTimesheetpcr option').filter(function () {
                    return $(this).text().startsWith(firstDetail.clientTypeName);
                }).prop('selected', true).change();
            }
        }).always(function () {
            $('#loader').hide();
        });
    }


    function selectClientSite(siteId) {
        const interval = setInterval(function () {
            const siteOption = $('#clientSitesTimesheetPCR option[value="' + siteId + '"]');
            if (siteOption.length > 0) {
                $('#clientSitesTimesheetPCR').val(siteId);
                clearInterval(interval);
            }
        }, 100);
    }

    // Bind change event
    $('#clientSitesTimesheetPCR').on('change', function () {
        // No longer auto-loading single schedule inputs
    });

    // Fill schedule inputs from route detail object (DEPRECATED - now handled in createPCARSiteBlock)
    function fillScheduleInputs(rd) { }

    // Clear all schedule inputs (DEPRECATED - containers are emptied)
    function clearScheduleInputs() { }


    $('#editSelectedSiteTimesheetpcr').on('click', function () {
        const selectedOption = $('#selectedSitesTimeSheetpcar option:selected');

        if (selectedOption.length === 0) {
            alert('Please select a site to edit');
            return;
        }

        if (selectedOption.length > 1) {
            alert('Select only one site to edit');
            return;
        }

        const clientSiteId = selectedOption.val();

        // Get the route detail for this site
        const rd = currentRouteDetails.find(x => x.clientSiteId == clientSiteId);

        if (!rd) {
            alert('Route details not found for this site');
            return;
        }

        if (currentRouteDetails.length > 0) {
            // Get first route detail to pre-select Client Type and Site
            const firstDetail = rd;

            $('#clientTypeNameTimesheetpcr option').filter(function () {
                return $(this).text().startsWith(firstDetail.clientTypeName); // match the name portion
            }).prop('selected', true).change();
            // Wait a little or check when sites are loaded


            // Wait for client sites to populate, then select the site
            const waitForSites = setInterval(function () {
                if ($('#clientSitesTimesheetPCR option[value="' + firstDetail.clientSiteId + '"]').length > 0) {
                    clearInterval(waitForSites);

                    // Select site
                    $('#clientSitesTimesheetPCR').val(firstDetail.clientSiteId).trigger('change');
                }
            }, 100);

        }

        // 3️⃣ Optionally, populate the schedule inputs directly
        loadRouteDetailsForSite(clientSiteId);
    });


    //$('#selectedSitesTimeSheetpcar').on('change', function () {
    //    const selectedOption = $(this).find(':selected');

    //    if (selectedOption.length === 0) {
    //        clearScheduleInputs();
    //        return;
    //    }

    //    if (selectedOption.length > 1) {
    //        // If multiple selected, you can choose to clear or ignore
    //        clearScheduleInputs();
    //        return;
    //    }

    //    const clientSiteId = selectedOption.val();

    //    // Get the route detail for this site
    //    const rd = currentRouteDetails.find(x => x.clientSiteId == clientSiteId);

    //    if (!rd) {
    //        alert('Route details not found for this site');
    //        return;
    //    }

    //    if (currentRouteDetails.length > 0) {
    //        // Get first route detail to pre-select Client Type and Site
    //        const firstDetail = rd;

    //        $('#clientTypeNameTimesheetpcr option').filter(function () {
    //            return $(this).text().startsWith(firstDetail.clientTypeName); // match the name portion
    //        }).prop('selected', true).change();
    //        // Wait a little or check when sites are loaded


    //        // Wait for client sites to populate, then select the site
    //        const waitForSites = setInterval(function () {
    //            if ($('#clientSitesTimesheetPCR option[value="' + firstDetail.clientSiteId + '"]').length > 0) {
    //                clearInterval(waitForSites);

    //                // Select site
    //                $('#clientSitesTimesheetPCR').val(firstDetail.clientSiteId).trigger('change');
    //            }
    //        }, 100);

    //    }

    //    // 3️⃣ Optionally, populate the schedule inputs directly
    //    loadRouteDetailsForSite(clientSiteId);
    //    $('#selectedSitesTimeSheetpcar').val(clientSiteId);
    //});

    $('#removeSelectedSitesTimesheetpcr').on('click', function () {
        const selectedOptions = $('#selectedSitesTimeSheetpcar option:selected');

        if (selectedOptions.length === 0) {
            alert('Please select at least one site to remove.');
            return;
        }

        if (!confirm('Are you sure you want to remove the selected site(s)?')) {
            return;
        }

        // Collect IDs of selected sites
        const siteIds = selectedOptions.map(function () {
            return $(this).val();
        }).get();

        // AJAX call to remove from database
        $.ajax({
            url: '/Admin/Settings?handler=RemovePcarRouteSites', // your handler
            type: 'POST',
            data: JSON.stringify({ PcarRouteId: $('#TimeSheetscheduleIdpcr').val(), ClientSiteIds: siteIds }),
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                // Remove from dropdown
                selectedOptions.remove();
                updateSelectedSitesCountPCAR(); // update badge count
                clearScheduleInputs(); // clear schedule inputs if needed
                alert('Selected site(s) removed successfully.');
                // ⬇ Reload the UI
                loadPCARRoute($('#TimeSheetscheduleIdpcr').val());
                if (gridPCARSchedules) {
                    gridPCARSchedules.reload();
                }
            } else {
                alert(data.message || 'Failed to remove site(s).');
            }
        });
    });

    // Function to update badge count
    function updateSelectedSitesCountPCAR() {
        const count = $('#selectedSitesTimeSheetpcar option').length;
        $('#selectedSitesCountTimesheetpcar').text(count);
    }

    // Optional: clear schedule inputs after removing site
    function clearScheduleInputs() {
        $('input[id^="start"], input[id^="end"], input[id^="visit"]').val('');
    }

    $('#add_kpi_pcarrutes').on('click', function () {
        clearScheduleInputs();   // <-- this is clearing everything
        $('#TimeSheetscheduleIdpcr').val(0);
        togglePcarBottomSection();
        $('#selectedSitesTimeSheetpcar').empty();
    });

    $('#pcar-modal').on('hidden.bs.modal', function () {
        if (gridPCARSchedules) {
            gridPCARSchedules.reload();
        }
    });

    function togglePcarBottomSection() {
        var id = parseInt($("#TimeSheetscheduleIdpcr").val());
        var $bottomSection = $("#pcar-bottom-section");
        var $btnProfile = $("#btnCreatePcarProfile");

        if (id > 0) {
            // Enable bottom section
            $bottomSection.css({
                "pointer-events": "auto",
                "opacity": "1"
            });

            // Change button text
            $btnProfile.html('<i class="fa fa-pencil mr-1"></i>Update Profile');
            $btnProfile.removeClass('btn-success').addClass('btn-warning'); // optional style change
        } else {
            // Disable bottom section
            $bottomSection.css({
                "pointer-events": "none",
                "opacity": "0.4"
            });

            // Reset button text
            $btnProfile.html('<i class="fa fa-plus mr-1"></i>Create Profile');
            $btnProfile.removeClass('btn-warning').addClass('btn-success'); // optional style change
        }
    }

    // Call this function on modal load or whenever the ID changes
    $(document).ready(function () {
        togglePcarBottomSection();
    });

    //pcar end 

    $('#btnSaveTimesheetSchedule').on('click', function () {
        $("input[name=clientSiteIds]").remove();
        var options = $('#selectedSitesTimeSheet option');
        options.each(function () {
            const elem = '<input type="hidden" name="clientSiteIds" value="' + $(this).val() + '">';
            $('#frm_kpi_Timesheetschedule').append(elem);
        });

        $.ajax({
            url: '/Admin/Settings?handler=SaveKpiTimesheetSchedule',
            type: 'POST',
            data: $('#frm_kpi_Timesheetschedule').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                $('#TimeSheetschedule-modal').modal('hide');
                alert('Schedule saved successfully');
                gridTimesheetSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
            } else {
                $('#sch-modal-validation1').html('');
                data.message.split(',').map(function (item) { $('#sch-modal-validation1').append('<li>' + item + '</li>') });
                $('#sch-modal-validation1').show().delay(5000).fadeOut();
            }
        });
    });
    $('#kpi_send_Timesheetschedules').on('click', '.del-schedule1', function () {
        const idToDelete = $(this).attr('data-sch-id');
        if (confirm('Are you sure want to delete this Timesheet schedule?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteKpiSendScheduleTimesheet',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridTimesheetSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
            });
        }

    });
    $('#kpi_send_schedules').on('click', '.del-schedule', function () {
        const idToDelete = $(this).attr('data-sch-id');
        if (confirm('Are you sure want to delete this schedule?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteKpiSendSchedule',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridSchedules.reload({ handler: getScheduleUrl(), type: sel_schedule_report_type, searchTerm: $('#search_kw_client_site').val() });
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
            if (data.isCriticalDocumentDownselect == true) {
                $('#cbIsDownselect').prop('checked', true);
                $('#CriticalGroupNameID').prop('disabled', false);
                $('#IsDownselect').val(true)
            }
            else {
                $('#CriticalGroupNameID').prop('disabled', true);
            }
            $('#CriticalGroupNameID').val(data.criticalGroupNameID);
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
            $('#isHrTimerPaused').val(data.isHrTimerPaused);
            $('#hrTimerIsPaused').prop('checked', !data.isHrTimerPaused);
            $('#hrTimerIsPausedStatus').html(data.isHrTimerPaused ? 'Paused' : 'Active');
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



    //P2-103 Duplicate Settings-start
    function scheduleModalOnCopy(scheduleId) {
        $('#loader').show();
        $.ajax({
            url: '/Admin/Settings?handler=KpiSendSchedule&id=' + scheduleId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            const dateToday = new Date().toISOString().split('T')[0];
            $('#startDate').val(dateToday);
            $('#startDate').attr('min', dateToday);

            const dateEnd = '2100-01-01'
            $('#endDate').val(dateEnd.split('T')[0]);
            /*$('#startDate').val(data.startDate.split('T')[0]);*/
            //if (data.endDate)
            //    $('#endDate').val(data.endDate.split('T')[0]);
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
            $('#isHrTimerPaused').val(data.isHrTimerPaused);
            $('#hrTimerIsPaused').prop('checked', !data.isHrTimerPaused);
            $('#hrTimerIsPausedStatus').html(data.isHrTimerPaused ? 'Paused' : 'Active');
            $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val('');
            $.each(data.kpiSendScheduleClientSites, function (index, item) {
                $('#selectedSites').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
                updateSelectedSitesCount();
            });
            //$('#projectName').val(data.projectName);
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

    //P2-103 Duplicate Settings-end
    function clearSummaryImage() {
        $('#summary_image').html('-');
        $('#summary_image_updated').html('-');
        $('#download_summary_image').hide();
        $('#delete_summary_image').hide();
    }

    function setSummaryImage(summaryImage) {
        if (summaryImage) {
            $('#summary_image').html(summaryImage.fileName);
            $('#RCImagepath').val(summaryImage.fileName);
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

    /*Rc Action List Image Upload start*/
    $('#div_site_settings').on('change', '#upload_summary_imageRcList', function () {

        const file = $('#upload_summary_imageRcList').prop("files")[0];
        if (file) {
            const id = $("#RCList_Id").val();
            const formData = new FormData();
            formData.append("SummaryImage", file);
            formData.append("id", id);

            $.ajax({
                type: 'POST',
                url: '/Admin/Settings?handler=UploadRCImage',
                data: formData,
                cache: false,
                contentType: false,
                processData: false,
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.success) {
                    setSummaryImageRCList(data);
                }
            }).always(function () {
                $('#upload_summary_imageRcList').val('');
            });
        }
    });

    $('#div_site_settings').on('change', '#upload_summary_imageRcList1', function () {

        const ClientSiteID = $('#ClientSiteId').val();
        const file = $('#upload_summary_imageRcList1').prop("files")[0];
        const type = 6;
        let DocumentID = $('#DocumentID').val();
        if (DocumentID == '') {
            DocumentID = 0;
        }
        const fileExtn = file.name.split('.').pop();
        if (!fileExtn || '.pdf,.docx,.xlsx'.indexOf(fileExtn.toLowerCase()) < 0) {
            showModal('Unsupported file type. Please upload a .pdf, .docx or .xlsx file');
            return false;
        }

        const fileForm = new FormData();
        fileForm.append('file', file);
        fileForm.append('type', type);
        fileForm.append('ClientSiteID', ClientSiteID);
        fileForm.append('doc-id', DocumentID);


        $.ajax({
            url: '/Admin/Settings?handler=UploadStaffDocUsingType',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success) {

                setSummaryImageRCList(data);
            }
        }).fail(function () {
            //showStatusNotification(false, 'Something went wrong');
        });
    });

    function setSummaryImageRCList(summaryImage) {
        if (summaryImage) {
            var filename = summaryImage.fileName;
            var filebase64 = "";

            if (summaryImage.imagepath) {
                var myArray = summaryImage.imagepath.split(":-:");
                if (myArray.length > 1) {
                    filebase64 = myArray[1];
                    filename = myArray[0];
                }
            }

            $('#summary_imageRC').html(filename);
            $('#RCImagepath').val(filename);
            $('#summary_image_updatedRC').html(summaryImage.lastUpdated);
            $('#RCImageDateandTime').val(summaryImage.lastUpdated);

            if (filebase64 != "") {
                $('#download_summary_imageRCList').attr('href', filebase64);
                $('#download_summary_imageRCList').attr('download', filename);
            } else {
                $('#download_summary_imageRCList').removeAttr('href');
            }

            $('#download_summary_imageRCList').show();
            $('#delete_summary_imageRC').show();
        }
    }

    $('#div_site_settings').on('click', '#delete_summary_image1', function () {
        const idToDelete = $('#DocumentID').val();
        if (confirm('Are you sure want to delete this file?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteStaffDoc',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                clearSummaryImageRC();
            });
        }
    });

    $('#div_site_settings').on('click', '#delete_summary_imageRC', function () {
        if (confirm('Are you sure want to delete this file?')) {
            var check = $('#RCImagepath').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteRCImage&imageName=' + $('#RCImagepath').val(),
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.status) {
                    clearSummaryImageRC();
                }
            }).fail(function () {
                console.log('error')
            });
        }
    });
    function clearSummaryImageRC() {
        $('#RCImagepath').val('');
        $('#RCImageDateandTime').val('');
        $('#summary_imageRC').html('');
        $('#summary_image_updatedRC').html('');
        $("#download_summary_imageRCList").removeAttr("href");
        $('#download_summary_imageRCList').show();
        $('#delete_summary_imageRC').hide();
        $('#DocumentID').val('');

    }
    /*Rc Action List Image Upload stop*/

    //RC Action List Save start



    $('#div_site_settings').on('click', '#save_site_RC', function () {
        var List = $('#frm_ActionList').serialize();
        $.ajax({
            url: '/admin/settings?handler=ClientSiteRCActionList',
            type: 'POST',
            data: $('#frm_ActionList').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status)
                alert('Saved successfully');
            else {
                alert(result.message);
                $("#ClientSiteKpiRC_Id").val(result.id);
                $('#Site_Alarm_Keypad_code').val('');
                $('#site_Physical_key').val('');
                $('#Site_Combination_Look').val('');
                $('#Action1').val('');
                $('#Action2').val('');
                $('#Action3').val('');
                $('#Action4').val('');
                $('#Action5').val('');
                $('#userInput').val('');
            }
            // $('#kpi-settings-modal').modal('hide');
            //gridClientSiteSettings.clear();
            //gridClientSiteSettings.reload({ type: $('#cs_client_type').val() });
        }).fail(function () { });
    });
    //RC Action List Save stop
    //p2 - 135 rc duress - start
    $('#div_site_settings').on('change', '#cbIsRCBypassSite', function () {

        const isChecked = $(this).is(':checked');

        $('#RCList_IsSiteRCBypass').val(isChecked);
    });
    //p2 - 135 rc duress - start
    function scheduleModalOnAdd() {
        const dateToday = new Date().toISOString().split('T')[0];
        $('#startDate').val(dateToday);
        $('#startDate').attr('min', dateToday);
        const dateEnd = '2100-01-01'
        $('#endDate').val(dateEnd.split('T')[0]);
        //$('#endDate').attr('min', dateToday);
        $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val('');
        $('#summaryNoteMonth').val((new Date()).getMonth() + 1);
        $('#summaryNoteYear').val((new Date()).getFullYear());
    }

    function clearScheduleModal() {
        $('#cbIsDownselect').prop('checked', false);
        $('#CriticalGroupNameID').val('');
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
        $('#isHrTimerPaused').val(false);
        $('#hrTimerIsPaused').prop('checked', true);
        $('#hrTimerIsPausedStatus').html('Active');
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

    $('#hrTimerIsPaused').on('change', function () {
        const isChecked = $(this).is(':checked');
        $('#hrTimerIsPausedStatus').html(isChecked ? 'Active' : 'Paused');
        $('#isHrTimerPaused').val(!isChecked);
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
            url: '/dashboard?handler=ClientSitesUsingUserId&type=' + encodeURIComponent(option) + "&guardId=" + encodeURIComponent($("#hid_userId").val() || $("#hid_userIdSettings").val() || 0),
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
                //p1-139 change pop up start
                'data-cs-id="' + $(selectedOption).val() + '" data-cs-name="' + $(selectedOption).text() + '" data-type-tab="KPI"></button>';
            //p1-139 change pop up end
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
            //P2-103 Duplicate Settings-start
            const isCopy = button.data('action') !== undefined && button.data('action') === 'copySchedule';
            if (isCopy) {
                schId = button.data('sch-id');
                scheduleModalOnCopy(schId);
            } else {
                scheduleModalOnAdd();
            }
            //P2-103 Duplicate Settings-end
        }

        showHideSchedulePopupTabs(isEdit);
    });
    $('#cbIsDownselect').on('change', function () {
        const isChecked = $(this).is(':checked');

        const filter = isChecked ? 1 : 2;
        if (filter == 1) {

            $('#IsDownselect').val(true)
            $('#CriticalGroupNameID').prop('disabled', false);

        }
        if (filter == 2) {
            $('#IsDownselect').val(false)
            $('#CriticalGroupNameID').prop('disabled', true);
            $('#CriticalGroupNameID').val('');
        }
    });
    $('#btnSaveSchedule').on('click', function () {
        $("input[name=clientSiteIds]").remove();
        var options = $('#selectedSites option');
        options.each(function () {
            const elem = '<input type="hidden" name="clientSiteIds" value="' + $(this).val() + '">';
            $('#frm_kpi_schedule').append(elem);
        });
        if ($('#cbIsDownselect').is(':checked')) {

            if (!$('#CriticalGroupNameID').val()) {

                alert('Please select a value in  Policy Group.');
                return;
            }
        }
        $.ajax({
            url: '/Admin/Settings?handler=SaveKpiSendSchedule',
            type: 'POST',
            data: $('#frm_kpi_schedule').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                $('#schedule-modal').modal('hide');
                alert('Schedule saved successfully');
                gridSchedules.reload({ handler: getScheduleUrl(), type: sel_schedule_report_type, searchTerm: $('#search_kw_client_site').val() });
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
                // // Create a temporary anchor element to trigger the download
                //var url = window.URL.createObjectURL(blob);
                // // Open the PDF in a new tab
                //var newTab = window.open(url, '_blank');

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

                //if (!newTab) {
                //    // If the new tab was blocked, fallback to downloading the file
                //    var a = document.createElement('a');
                //    a.href = url;
                //    a.download = downloadedFileName;
                //    a.click();
                //}
                //window.URL.revokeObjectURL(url);                
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


    //Code to handle Custom Wands schedule start
    function clearScheduleModalCustomWand() {

        $('#CustomWandscheduleId').val('0');
        $('#clientTypeNameCustomWand').val('');
        $('#clientSitesCustomWand').html('<option value="">Select</option><option value="-1">Select All</option>');
        $('#selectedSitesCustomWand').html('');
        updateSelectedSitesCustomWandCount();
        //$('input:hidden[name="clientSiteIds"]').remove();
        $('#clientTypeNameCustomWand option:eq(0)').attr('selected', true);
        $('#startDateCustomWand').val('');
        $('#startDateCustomWand').removeAttr('min');
        $('#endDateCustomWand').val('');
        $('#endDateCustomWand').removeAttr('min');
        $('#CustomWandtime').val('');
        $('#CustomWandfrequency option').removeAttr('selected');
        $('#CustomWandfrequency').val('');
        $('#nextRunOnCustomWand').val('');
        $('#sch-modal-validation-CustomWand').html('');
        $('#emailToCustomWand').val('');
        $('#emailBccCustomWand').val('');
        $('#projectNameCustomWand').val('');
        $('#chk_CustomWandReportType_Site').prop('checked', true);
        $('#chk_CustomWandReportType_MonSun').prop('checked', false);
        $('#chk_CustomWandReportType_All').prop('checked', false);
    }

    $('#customWands-schedule-modal').on('shown.bs.modal', function (event) {
        clearScheduleModalCustomWand();
        const button = $(event.relatedTarget);
        const isEdit = button.data('action') !== undefined && button.data('action') === 'editScheduleCustomWand';
        if (isEdit) {
            schId = button.data('sch-id');
            scheduleModalOnEditCustomWand(schId);
        }
        else {
            const isCopy = button.data('action') !== undefined && button.data('action') === 'copyCustomWandSchedule';
            if (isCopy) {
                schId = button.data('sch-id');
                scheduleModalOnCopyCustomWand(schId);
            } else {
                scheduleModalOnAddCustomWand();
            }
        }
    });

    $('#clientTypeNameCustomWand').on('change', function () {
        const option = $(this).val();
        if (option === '') {
            $('#clientSitesCustomWand').html('');
            $('#clientSitesCustomWand').append('<option value="">Select</option>');
        }

        $.ajax({
            url: '/dashboard?handler=ClientSitesUsingUserId&type=' + encodeURIComponent(option) + "&guardId=" + encodeURIComponent($("#hid_userId").val() || $("#hid_userIdSettings").val() || 0),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#clientSitesCustomWand').html('');
            $('#clientSitesCustomWand').append('<option value="">Select</option><option value="-1">Select All</option>');
            data.map(function (site) {
                $('#clientSitesCustomWand').append('<option value="' + site.value + '">' + site.text + '</option>');
            });
        });
    });
    $('#clientSitesCustomWand').on('change', function () {
        const elem = $(this).find(":selected");
        if (elem.val() !== '') {
            if (elem.val() == '-1') {
                $('#clientSitesCustomWand option').each(function () {

                    const value = $(this).val();
                    const text = $(this).text();

                    // Skip empty and Select All option
                    if (value === '' || value === '-1') {
                        return;
                    }

                    // Check if already exists
                    const existing = $('#selectedSitesCustomWand option[value="' + value + '"]');

                    if (existing.length === 0) {
                        $('#selectedSitesCustomWand').append(
                            '<option value="' + value + '">' + text + '</option>'
                        );
                    }
                });
                updateSelectedSitesCustomWandCount();
            }
            else {
                const existing = $('#selectedSitesCustomWand option[value="' + elem.val() + '"]');
                if (existing.length === 0) {
                    $('#selectedSitesCustomWand').append(
                        '<option value="' + elem.val() + '">' + elem.text() + '</option>'
                    );
                    updateSelectedSitesCustomWandCount();
                }
            }
        }
    });

    function scheduleModalOnAddCustomWand() {
        const dateToday = new Date().toISOString().split('T')[0];
        $('#startDateCustomWand').val(dateToday);
        $('#startDateCustomWand').attr('min', dateToday);
        const dateEnd = '2100-01-01'
        $('#endDateCustomWand').val(dateEnd.split('T')[0]);

        //$("textarea[id='KpiSendScheduleSummaryNote_Notes']").val('');
    }

    function scheduleModalOnEditCustomWand(scheduleId) {
        $('#loader').show();
        $.ajax({
            url: '/Admin/Settings?handler=KpiCustomWandSchedule&id=' + scheduleId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {

            $('#CustomWandscheduleId').val(data.id);
            $('#startDateCustomWand').val(data.startDate.split('T')[0]);
            if (data.endDate)
                $('#endDateCustomWand').val(data.endDate.split('T')[0]);
            $('#endDateCustomWand').attr('min', new Date().toISOString().split('T')[0]);
            $('#CustomWandfrequency').val(data.frequency).change();
            $('#CustomWandfrequency option[value="' + data.frequency + '"]').attr('selected', true);
            $('#CustomWandtime').val(data.time);
            $('#nextRunOnCustomWand').val(data.nextRunOn);
            $('#emailToCustomWand').val(data.emailTo);
            $('#emailBccCustomWand').val(data.emailBcc)

            $.each(data.kpiSendCustomWandClientSites, function (index, item) {
                $('#selectedSitesCustomWand').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
                updateSelectedSitesCustomWandCount();
            });

            if (data.customWandReportType === 0) {
                $('#chk_CustomWandReportType_Site').prop('checked', true);
                $('#chk_CustomWandReportType_MonSun').prop('checked', false);
                $('#chk_CustomWandReportType_All').prop('checked', false);
            }
            else if (data.customWandReportType === 1) {
                $('#chk_CustomWandReportType_Site').prop('checked', false);
                $('#chk_CustomWandReportType_MonSun').prop('checked', true);
                $('#chk_CustomWandReportType_All').prop('checked', false);
            }
            else if (data.customWandReportType === 2) {
                $('#chk_CustomWandReportType_Site').prop('checked', false);
                $('#chk_CustomWandReportType_MonSun').prop('checked', false);
                $('#chk_CustomWandReportType_All').prop('checked', true);
            }
            $('#CustomWandReportType').val(data.customWandReportType);

            $('#projectNameCustomWand').val(data.projectName);


        }).always(function () {
            $('#loader').hide();
        });
    }
    $('#removeSelectedSitesCustomWand').on('click', function () {
        $('#selectedSitesCustomWand option:selected').remove();
        updateSelectedSitesCustomWandCount();
    });
    function updateSelectedSitesCustomWandCount() {
        $('#selectedSitesCountCustomWand').text($('#selectedSitesCustomWand option').length);
    }

    $('#editSelectedSiteCustomWand').on('click', function () {
        if ($('#editSiteTrigger').length === 1) {
            $('#editSiteTrigger').remove()
        }

        const selectedOption = $('#selectedSitesCustomWand option:selected');
        if (selectedOption.length == 0) {
            alert('Please select a site to edit');
        } else if (selectedOption.length > 1) {
            alert('Select only one site to edit');
        } else {

            let triggerButton = '<button type="button" id="editSiteTrigger" style="display:none" data-toggle="modal" data-target="#kpi-settings-modal" ' +
                //p1-139 change pop up start
                'data-cs-id="' + $(selectedOption).val() + '" data-cs-name="' + $(selectedOption).text() + '" data-type-tab="KPI"></button>';
            //p1-139 change pop up end
            $(triggerButton).insertAfter($(this));
            $('#editSiteTrigger').click();

        }
    });

    function scheduleModalOnCopyCustomWand(scheduleId) {
        $.ajax({
            url: '/Admin/Settings?handler=KpiCustomWandSchedule&id=' + scheduleId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            const dateToday = new Date().toISOString().split('T')[0];
            $('#startDateCustomWand').val(dateToday);
            $('#startDateCustomWand').attr('min', dateToday);
            const dateEnd = '2100-01-01'
            $('#endDateCustomWand').val(dateEnd.split('T')[0]);

            $('#CustomWandscheduleId').val('0');
            // $('#startDateCustomWand').val(data.startDate.split('T')[0]);
            //if (data.endDate)
            //    $('#endDateCustomWand').val(data.endDate.split('T')[0]);
            //$('#endDateCustomWand').attr('min', new Date().toISOString().split('T')[0]);
            $('#CustomWandfrequency').val(data.frequency).change();
            $('#CustomWandfrequency option[value="' + data.frequency + '"]').attr('selected', true);
            $('#CustomWandtime').val(data.time);
            $('#nextRunOnCustomWand').val(data.nextRunOn);
            $('#emailToCustomWand').val(data.emailTo);
            $('#emailBccCustomWand').val(data.emailBcc)

            $.each(data.kpiSendCustomWandClientSites, function (index, item) {
                $('#selectedSitesCustomWand').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
                updateSelectedSitesCustomWandCount();
            });
            if (data.customWandReportType === 0) {
                $('#chk_CustomWandReportType_Site').prop('checked', true);
                $('#chk_CustomWandReportType_MonSun').prop('checked', false);
                $('#chk_CustomWandReportType_All').prop('checked', false);
            }
            else if (data.customWandReportType === 1) {
                $('#chk_CustomWandReportType_Site').prop('checked', false);
                $('#chk_CustomWandReportType_MonSun').prop('checked', true);
                $('#chk_CustomWandReportType_All').prop('checked', false);
            }
            else if (data.customWandReportType === 2) {
                $('#chk_CustomWandReportType_Site').prop('checked', false);
                $('#chk_CustomWandReportType_MonSun').prop('checked', false);
                $('#chk_CustomWandReportType_All').prop('checked', true);
            }
            $('#CustomWandReportType').val(data.customWandReportType);


        }).always(function () {
            $('#loader').hide();
        });
    }


    $('#btnSaveCustomWandSchedule').on('click', function () {
        $("input[name=clientSiteIds]").remove();
        var options = $('#selectedSitesCustomWand option');
        options.each(function () {
            const elem = '<input type="hidden" name="clientSiteIds" value="' + $(this).val() + '">';
            $('#frm_kpi_CustomWandSchedule').append(elem);
        });

        // Validate report type selection
        const isSiteChecked = $('#chk_CustomWandReportType_Site').is(':checked');
        const isMonSunChecked = $('#chk_CustomWandReportType_MonSun').is(':checked');
        const isAllChecked = $('#chk_CustomWandReportType_All').is(':checked');

        // No checkbox selected
        if (!isSiteChecked && !isMonSunChecked && !isAllChecked) {
            alert('Please select Custom Wand Report Type');
            return;
        }

        // Set hidden field value
        if (isSiteChecked) {
            $('#CustomWandReportType').val(0);
        }
        else if (isMonSunChecked) {
            $('#CustomWandReportType').val(1);
        }
        else if (isAllChecked) {
            $('#CustomWandReportType').val(2);
        }

        $.ajax({
            url: '/Admin/Settings?handler=SaveKpiCustomWandSchedule',
            type: 'POST',
            data: $('#frm_kpi_CustomWandSchedule').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success) {
                $('#customWands-schedule-modal').modal('hide');
                alert('Schedule saved successfully');
                gridSchedules.reload({ handler: getScheduleUrl(), type: sel_schedule_report_type, searchTerm: $('#search_kw_client_site').val() });
            } else {
                $('#sch-modal-validation-CustomWand').html('');
                data.message.split(',').map(function (item) { $('#sch-modal-validation-CustomWand').append('<li>' + item + '</li>') });
                $('#sch-modal-validation-CustomWand').show().delay(5000).fadeOut();
            }
        });
    });

    $('#kpi_send_schedules').on('click', '.del-customwand-schedule', function () {
        const idToDelete = $(this).attr('data-sch-id');
        if (confirm('Are you sure want to delete this schedule?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteKpiCustomWandSchedule',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridSchedules.reload({ handler: getScheduleUrl(), type: sel_schedule_report_type, searchTerm: $('#search_kw_client_site').val() });
            });
        }

    });

    $('#chk_CustomWandReportType_Site').on('change', function () {
        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_CustomWandReportType_MonSun').prop('checked', false);
            $('#chk_CustomWandReportType_All').prop('checked', false);
        }
        //else {
        //$('#chk_cs_tn_no_load').prop('checked', true);
        //}
        $('#chk_CustomWandReportType_Site').val(isChecked);
    });
    $('#chk_CustomWandReportType_MonSun').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_CustomWandReportType_Site').prop('checked', false);
            $('#chk_CustomWandReportType_All').prop('checked', false);
        }
        $('#chk_CustomWandReportType_MonSun').val(isChecked);
    });
    $('#chk_CustomWandReportType_All').on('change', function () {

        const isChecked = $(this).is(':checked');
        if (isChecked == true) {
            $('#chk_CustomWandReportType_MonSun').prop('checked', false);
            $('#chk_CustomWandReportType_Site').prop('checked', false);
        }
        $('#chk_CustomWandReportType_All').val(isChecked);
    });

    //Code to handle Custom Wands schedule End



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
            'data-cs-id="' + record.id + '" data-cs-name="' + record.clientSiteName + '" data-cs-email="' + record.siteEmail + '" data-cs-address="' + record.address + '" data-cs-landline="' + record.landLine + '" data-cs-duressemail="' + record.duressEmail + '" data-cs-duresssms="' + record.duressSms +
            '" data-cs-guardlog-emailto="' + record.guardLogEmailTo + '" data-cs-dbx-upload="' + record.siteUploadDailyLog +
            //p1-139 change pop up start
            '"data-cs-datacollection-enabled ="' + record.dataCollectionEnabled + '" data-type-tab="LB"><i class="fa fa-pencil mr-2"></i>Edit</button>';
        //p1-139 change pop up end

    }

    gridClientSiteSettings = $('#kpi_client_site_settings').grid({
        dataSource: '/Admin/Settings?handler=ClientSiteWithSettings',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { width: 150, field: 'clientTypeName', title: 'Client Type' },
            { width: 250, field: 'clientSiteName', title: 'Client Site' },
            { width: 250, field: 'siteEmail', title: 'Site Email', hidden: true },
            { width: 250, field: 'address', title: 'Address', hidden: true },
            { width: 250, field: 'landLine', title: 'Site Land Line', hidden: true },
            { width: 250, field: 'guardLogEmailTo', title: 'Email Recipients', hidden: true },
            { width: 50, field: 'siteUploadDailyLog', title: 'Daily Log Dump?', renderer: function (value, record) { return value === true ? '<i class="fa fa-check-circle text-success"></i>' : '<i class="fa"></i>'; } },
            { width: 100, field: 'hasSettings', title: 'Settings Available?', renderer: function (value, record) { return value === true ? '<i class="fa fa-check-circle text-success"></i>' : '<i class="fa"></i>'; } },
            { width: 100, renderer: settingsButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });


    /*code added for search client start */
    $('#search_sites_settings').on('keyup', function (e) {
        var SearchTextbox = $("#search_sites_settings");
        var searchText = SearchTextbox.val();
        if (searchText.length >= 3) {
            gridClientSiteSettings.reload({ type: $('#cs_client_type').val(), searchTerm: $(this).val(), userId: $('#hid_userIdSettings').val() });

        }

    });
    $('#btnSearchSites').on('click', function () {
        //gridClientSiteSettings.reload({ type: $('#cs_client_type').val(), searchTerm: $('#search_sites_settings').val(), userId: $('#hid_userIdSettings').val() });
        reloadClientSiteSettings();
    });

    $('#cs_client_type').on('change', function () {
        var SearchTextbox = $("#search_sites_settings");
        SearchTextbox.val("");
        var searchitem = '';
        gridClientSiteSettings.reload({ type: $(this).val(), searchTerm: searchitem, userId: $('#hid_userIdSettings').val() });
    });
    if ($('#cs_client_type').val() != '') {
        var searchitem = $("#search_sites_settings").val();
        gridClientSiteSettings.reload({ type: $(this).val(), searchTerm: searchitem, userId: $('#hid_userIdSettings').val() });
    }


    /*code added for search client stop */
    var currentDiv = 1;
    let gritdSmartWands;
    let gridSiteDropboxSettings;

    $('#kpi-settings-modal').on('shown.bs.modal', function (event) {
        currentDiv = 1;
        $("#OtherSettingsNew").load('CriticalDocumentNew');
        $('#div_site_settings').html('');
        const button = $(event.relatedTarget);
        const siteName = button.data('cs-name');
        const siteAddress = button.data('cs-address');
        //p1-139 change pop up start
        var type = button.data('type-tab');
        //p1-139 change pop up end
        $('#client_site_name').text(siteName);
        $('#client_site_address').text(siteAddress);
        globalClientSiteAddress = siteAddress;

        // $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + button.data('cs-id'));

        $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + button.data('cs-id'), function () {
            // This function will be executed after the content is loaded
            window.sharedVariable = button.data('cs-id');
            console.log('Load operation completed!');
            // You can add your additional code or actions here
            console.log(button.data('cs-id'));
            console.log(button.data('cs-address'));
            $('#_dropboxStatusDisplay').html('');

            populateDuressApp(button.data('cs-id'), 1);
            //p1-139 change pop up start
            if (type == 'KPI') {
                $('#kpi-LB-Menu-tab').removeClass('active');
                $('#kpi-tab').addClass('active');
                $('#KpiLB').removeClass('active');
                $('#KPISettings').addClass('active');
            }
            else {
                $('#kpi-LB-Menu-tab').addClass('active');
                $('#kpi-tab').removeClass('active');
                $('#KpiLB').addClass('active');
                $('#KPISettings').removeClass('active');
            }
            //p1-139 change pop up end
        });
    });


    $('#kpi-settings-modal').on('hidden.bs.modal', function (event) {
        reloadClientSiteSettings();
    });

    function reloadClientSiteSettings() {
        let type = $('#cs_client_type').val();
        let searchTerm = $('#search_sites_settings').val();
        let userId = $('#hid_userIdSettings').val();

        gridClientSiteSettings.dataSource = `/Admin/Settings?handler=ClientSiteWithSettings&type=${type}&searchTerm=${searchTerm}&userId=${userId}`;

        gridClientSiteSettings.reload();
    }



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
    $('#div_site_settings').on('click', '#delete_site_RCList', function () {
        if (confirm('Are you sure want to delete?')) {
            $.ajax({
                url: '/admin/settings?handler=DeleteRC',
                type: 'POST',
                data: { RCId: $("#RCList_Id").val() },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    $('#Site_Alarm_Keypad_code').val('');
                    $('#site_Physical_key').val('');
                    $('#Site_Combination_Look').val('');
                    $('#Action1').val('');
                    $('#Action2').val('');
                    $('#Action3').val('');
                    $('#Action4').val('');
                    $('#Action5').val('');
                    $('#userInput').val('');
                    $.ajax({
                        url: '/Admin/Settings?handler=DeleteRCImage&scheduleId=' + $('#RCImagepath').val(),
                        type: 'POST',
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    }).done(function (data) {
                        if (data.status) {
                            clearSummaryImageRC();
                        }
                    }).fail(function () {
                        console.log('error')
                    });
                }
                else
                    alert(result.message);
            }).fail(function () { });
        }
    });


    /* Delete worker 30012024 Start*/
    $('#div_site_settings').on('click', '#delete_worker', function () {
        if (confirm('Are you sure want to delete worker ?')) {
            var buttonValue = $(this).val();
            $.ajax({
                url: '/admin/settings?handler=DeleteWorker',
                type: 'POST',
                data: { settingsId: buttonValue },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    if (result.clientSiteId !== 0) {

                        $('#div_site_settings').html('');
                        //$('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId);  
                        $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + result.clientSiteId, function () {
                            // This function will be executed after the content is loaded
                            console.log('Load operation completed!');
                            // You can add your additional code or actions here
                            $('#kpi-tab').tab('show');
                            $('#contracted-manning-tab').tab('show');

                            window.sharedVariable = result.clientSiteId;
                            console.log('Load operation completed!');
                            // You can add your additional code or actions here
                            console.log(result.clientSiteId);
                            // $("#OtherSettingsNew").load('settingsOther?clientSiteId=53');


                            //alert('Removed the worker successfully');
                        });
                        //$('#kpi-settings-modal').modal('show');
                        //$("#kpi-settings-modal").appendTo("body");
                        currentDiv = 1;
                    }

                }
                else
                    alert(result.message);
            }).fail(function () { });
        }
    });
    /* Delete worker 30012024 end*/
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
    //p2-168-Import--partial-start
    $('#div_site_settings').on('click', '#getPrevoiusMonthKPINotesAndHRrecords', function () {


        $.ajax({
            url: '/admin/settings?handler=PreviousMonthKPINotesAndHRRecords',
            type: 'GET',
            data: {
                clientSiteId: $('#ClientSiteId').val(),
                month: $('#note_month').val(),
                year: $('#note_year').val()
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {

            //$("#ClientSiteKpiNote_Id").val(result.id);
            $("textarea[id='ClientSiteKpiNote_Notes']").val(result.notes);
            $('#lblSiteNoteRemainingCount').html(getSiteNoteLength(result.notes));
            //$("#ClientSiteKpiNote_ForMonth").val(result.forMonth);
            $("#ClientSiteKpiNote_HRRecords").val(result.hrRecords);
        }).fail(function () { });
    });
    //p2-1668-Import-end

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
            $("#ClientSiteKpiNote_HRRecords").val(result.hrRecords);


        }).fail(function () { });
    }

    /*code added for No of Patrols sum start */
    $('#div_site_settings').on('input', function () {
        var sum = 0;
        $(".patrol-sum").each(function () {
            var value = parseFloat($(this).val()) || 0;
            sum += value;
        });
        $("#PatrolSum").val(sum);

    });
    /*code added for No of Patrols sum stop */
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
        // Changes made by binoy due to mail on 13 Jul 2025, 21:49 related to Telematics - KV - Dropbox ERROR
        // Issue: There were one hidden fields with the same name (DropboxImagesDir), which caused issues when trying to save the value.
        //Solution: The name in the drop box settings "DropboxImagesDir" was changed to "DropboxImagesDir_DropboxSettings" to avoid conflicts.
        //and the value is set to the hidden field "DropboxImagesDir" before saving. (Refer singlePage.js function  $('#save_site_dropboxsettings').on('click', function ())
        var _dropboxImagesDir = $('#DropboxImagesDir_DropboxSettings').val();
        $('#DropboxImagesDir').val(_dropboxImagesDir);

        $.ajax({
            url: '/admin/settings?handler=ClientSiteKpiSettings',
            type: 'POST',
            data: $('#frm_site_settings').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == true) {
                alert('Saved successfully');
                $('#kpi-settings-modal').modal('hide');
                gridClientSiteSettings.clear();
                gridClientSiteSettings.reload({ type: $('#cs_client_type').val(), userId: $('#hid_userIdSettings').val() });
                $('#div_site_settings').html('');

                $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId, function () {

                    console.log('Load operation completed!');

                    $('#kpi-tab').tab('show');
                    $('#siteKpiSettings-tab').tab('show');
                    window.sharedVariable = data.clientSiteId;
                    console.log('Load operation completed!');

                    console.log(data.clientSiteId);

                    //  $("#OtherSettingsNew").load('settingsLB?clientSiteId=53');

                });
            }
            else {
                alert('Error');
            }

        }).fail(function () { });
    });


    $('#crmSupplierDetailsModal').on('shown.bs.modal', function (event) {
        $('#lbl_company_name').html('');
        $('#lbl_abn').html('');
        $('#lbl_landline').html('');
        $('#lbl_email').html('');
        $('#lbl_website').html('');


        const button = $(event.relatedTarget);

        const compName = button.data('id');


        $.ajax({
            url: '/admin/settings?handler=CrmSupplierData',
            data: { companyName: compName },
            type: 'GET',
        }).done(function (result) {
            if (result) {
                $('#lbl_company_name').html('&nbsp;' + result.companyName);
                $('#lbl_abn').html('&nbsp;' + result.companyABN);
                $('#lbl_landline').html('&nbsp;' + result.companyLandline);
                $('#lbl_email').html('&nbsp;' + result.email);
                $('#lbl_website').html('&nbsp;' + result.website);
            }
        });
    });

    $('#div_site_settings').on('change', '#ClientSite_Status', function () {
        // Get the selected value
        var selectedStatus = $(this).val();
        if (selectedStatus != 0) {
            $('#ClientSite_StatusDate').show();
        } else {
            $('#ClientSite_StatusDate').hide();
        }
        if (selectedStatus == 2) {
            $('#scheduleisActive').prop('checked', false);
        }
        else {
            $('#scheduleisActive').prop('checked', true);
        }



    });
    $('#div_site_settings').on('click', '#save_site_manning_settings', function () {
        $.ajax({
            url: '/admin/settings?handler=ClientSiteManningKpiSettings',
            type: 'POST',
            data: $('#frm_site_manning_settings').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == 1) {
                alert('Saved site manning details successfully');
                $('#div_site_settings').html('');
                //$('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId);  
                $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId, function () {
                    // This function will be executed after the content is loaded
                    console.log('Load operation completed!');
                    // You can add your additional code or actions here
                    $('#kpi-tab').tab('show');
                    $('#contracted-manning-tab').tab('show');
                    window.sharedVariable = data.clientSiteId;
                    console.log('Load operation completed!');
                    // You can add your additional code or actions here
                    console.log(data.clientSiteId);

                    //  $("#OtherSettingsNew").load('settingsLB?clientSiteId=53');

                });
                //$('#kpi-settings-modal').modal('show');
                //$("#kpi-settings-modal").appendTo("body");
                currentDiv = 1;

            }
            else if (data.success == 2) {
                $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
                $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");
                $('#ClientSiteManningGuardKpiSettings_1__PositionId').val($('#ClientSiteManningGuardKpiSettings_1__PositionId option:first').val());
                $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').val($('#ClientSiteManningPatrolCarKpiSettings_1__PositionId option:first').val());
                alert('Please add the site settings from site settings tab');

            }
            else if (data.success == 3) {
                alert('Please select position');
            }
            else if (data.success == 4) {
                alert('Error! Please try again');
                $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
                $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");

            }
            else if (data.success == 5) {
                alert('Please enter a valid time for Start and End in the format of HH:mm and in the range of 00:01 - 23:59. These are invalid times ' + data.erorrMessage + ' .');

            }

            else if (data.success == 6) {
                alert('The clocks must be in the range of 00:01–23:59, and ' + data.erorrMessage + ' is an invalid input.');

            }
            else if (data.success == 7) {
                alert('Please make sure you fill out the three boxes (start, end, and workers) for a day or make them blank. Please ensure workers have a value and cannot be blank when a clock is set.');

            }

        }).fail(function () { });
    });



    // Save only master settings values without manning hours  start
    $('#div_site_settings').on('click', '#save_site_manning_settings_WithoutValue', function () {


        $.ajax({
            url: '/admin/settings?handler=ClientSiteManningKpiSettingsWithoutValue',
            type: 'POST',
            data: $('#frm_site_manning_settings').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == 1) {
                alert('Saved site manning details successfully');
                $('#div_site_settings').html('');
                //$('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId);  
                $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId, function () {
                    // This function will be executed after the content is loaded
                    console.log('Load operation completed!');
                    // You can add your additional code or actions here
                    $('#kpi-tab').tab('show');
                    $('#contracted-manning-tab').tab('show');
                    window.sharedVariable = data.clientSiteId;
                    console.log('Load operation completed!');
                    // You can add your additional code or actions here
                    console.log(data.clientSiteId);

                    //  $("#OtherSettingsNew").load('settingsLB?clientSiteId=53');

                });
                //$('#kpi-settings-modal').modal('show');
                //$("#kpi-settings-modal").appendTo("body");
                currentDiv = 1;

            }
            else if (data.success == 2) {
                $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
                $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");
                $('#ClientSiteManningGuardKpiSettings_1__PositionId').val($('#ClientSiteManningGuardKpiSettings_1__PositionId option:first').val());
                $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').val($('#ClientSiteManningPatrolCarKpiSettings_1__PositionId option:first').val());
                alert('Please add the site settings from site settings tab');

            }
            else if (data.success == 4) {
                alert('Error! Please try again');
                $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
                $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");

            }


        }).fail(function () { });
    });

    // Save only master settings values without manning hours  end 

    //Adhoc Start 
    $('#div_site_settings').on('click', '#save_site_manning_settings_adhoc', function () {
        $.ajax({
            url: '/admin/settings?handler=ClientSiteManningKpiSettingsADHOC',
            type: 'POST',
            data: $('#frm_site_manning_settingsAdhoc').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success == 1) {
                alert('Saved site manning details successfully');
                $('#div_site_settings').html('');
                //$('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId);  
                $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId, function () {
                    // This function will be executed after the content is loaded
                    console.log('Load operation completed!');
                    // You can add your additional code or actions here
                    $('#kpi-tab').tab('show');
                    $('#contracted-manning-tabadhoc').tab('show');
                    window.sharedVariable = data.clientSiteId;
                    console.log('Load operation completed!');
                    // You can add your additional code or actions here
                    console.log(data.clientSiteId);

                    //  $("#OtherSettingsNew").load('settingsLB?clientSiteId=53');

                });
                //$('#kpi-settings-modal').modal('show');
                //$("#kpi-settings-modal").appendTo("body");
                currentDiv = 1;

            }
            else if (data.success == 2) {
                $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
                $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");
                $('#ClientSiteManningGuardKpiSettings_1__PositionId').val($('#ClientSiteManningGuardKpiSettings_1__PositionId option:first').val());
                $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').val($('#ClientSiteManningPatrolCarKpiSettings_1__PositionId option:first').val());
                alert('Please add the site settings from site settings tab');

            }
            else if (data.success == 3) {
                alert('Please select position');
            }
            else if (data.success == 4) {
                alert('Error! Please try again');
                $("input[name^='ClientSiteManningGuardKpiSettings']").val("");
                $("input[name^='ClientSiteManningPatrolCarKpiSettings']").val("");

            }
            else if (data.success == 5) {
                alert('Please enter a valid time for Start and End in the format of HH:mm and in the range of 00:01 - 23:59. These are invalid times ' + data.erorrMessage + ' .');

            }

            else if (data.success == 6) {
                alert('The clocks must be in the range of 00:01–23:59, and ' + data.erorrMessage + ' is an invalid input.');

            }
            else if (data.success == 7) {
                alert('Please make sure you fill out the three boxes (start, end, and workers) for a day or make them blank. Please ensure workers have a value and cannot be blank when a clock is set.');

            }

        }).fail(function () { });
    });


    $('#div_site_settings').on('click', '#showDivButtonAdhoc', function () {

        $('#divPatrolCarAdhoc').show();
        $('#divbtnAdhoc').show();
    });


    $('#div_site_settings').on('change', '#positionfilterPatrolCarAdhoc', function () {

        const isChecked = $(this).is(':checked');
        const filter = isChecked ? 1 : 2;

        $("#ClientSiteManningPatrolCarKpiSettingsADHOC_0__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettingsADHOC_1__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettingsADHOC_2__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettingsADHOC_3__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettingsADHOC_4__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettingsADHOC_5__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettingsADHOC_6__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettingsADHOC_7__Type").val(filter);


        calculateSumOfTextBoxValuesAdhoc();

        $.ajax({
            url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            $('#ClientSiteManningPatrolCarKpiSettingsADHOC_1__PositionId').html('');
            data.map(function (position) {
                $('#ClientSiteManningPatrolCarKpiSettingsADHOC_1__PositionId').append('<option value="' + position.value + '">' + position.text + '</option>');
            });
        });

    });



    $('#div_site_settings').on('click', '#delete_workerAdhoc', function () {
        if (confirm('Are you sure want to delete worker ?')) {
            var buttonValue = $(this).val();
            $.ajax({
                url: '/admin/settings?handler=DeleteWorkerADHOC',
                type: 'POST',
                data: { settingsId: buttonValue },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.status) {
                    if (result.clientSiteId !== 0) {

                        $('#div_site_settings').html('');
                        //$('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + data.clientSiteId);  
                        $('#div_site_settings').load('/admin/settings?handler=ClientSiteKpiSettings&siteId=' + result.clientSiteId, function () {
                            // This function will be executed after the content is loaded
                            console.log('Load operation completed!');
                            // You can add your additional code or actions here
                            $('#kpi-tab').tab('show');
                            $('#contracted-manning-tabadhoc').tab('show');

                            window.sharedVariable = result.clientSiteId;
                            console.log('Load operation completed!');
                            // You can add your additional code or actions here
                            console.log(result.clientSiteId);
                            // $("#OtherSettingsNew").load('settingsOther?clientSiteId=53');


                            //alert('Removed the worker successfully');
                        });
                        //$('#kpi-settings-modal').modal('show');
                        //$("#kpi-settings-modal").appendTo("body");
                        currentDiv = 1;
                    }

                }
                else
                    alert(result.message);
            }).fail(function () { });
        }
    });




    $('#div_site_settings').on('input', '#ClientSiteManningPatrolCarKpiSettingsADHOC_0__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettingsADHOC_1__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettingsADHOC_2__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettingsADHOC_3__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettingsADHOC_4__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettingsADHOC_5__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettingsADHOC_6__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettingsADHOC_7__NoOfPatrols', function () {

        calculateSumOfTextBoxValuesAdhoc();

    });

    $('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettingsADHOC_0__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_1__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_2__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_3__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_4__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_5__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_6__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_7__NoOfPatrols', function () {

        calculateSumOfTextBoxValues2Adhoc();

    });

    $('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettingsADHOC_8__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_9__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_10__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_11__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_12__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_13__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_14__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_15__NoOfPatrols', function () {

        calculateSumOfTextBoxValues3Adhoc();

    });

    $('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettingsADHOC_16__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_17__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_18__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_19__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_20__NoOfPatrols, #ClientSiteManningGuardKpiSettingsADHOC_21__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_22__NoOfPatrols,#ClientSiteManningGuardKpiSettingsADHOC_23__NoOfPatrols', function () {

        calculateSumOfTextBoxValues4Adhoc();

    });




    function calculateSumOfTextBoxValuesAdhoc() {

        if ($("#positionfilterPatrolCarAdhoc").prop('checked') == true) {


            // Get the values from textbox1 and convert them to numbers
            var value1 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_0__NoOfPatrols').val()) || 0;
            var value2 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_1__NoOfPatrols').val()) || 0;
            var value3 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_2__NoOfPatrols').val()) || 0;
            var value4 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_3__NoOfPatrols').val()) || 0;
            var value5 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_4__NoOfPatrols').val()) || 0;
            var value6 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_5__NoOfPatrols').val()) || 0;
            var value7 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_6__NoOfPatrols').val()) || 0;
            var value8 = parseFloat($('#ClientSiteManningPatrolCarKpiSettingsADHOC_7__NoOfPatrols').val()) || 0;
            // Calculate the sum
            var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
            // Update the value in textbox2
            if (sum !== 0) { $('#monthlyHrsAddNewAdhoc').val(sum); }
            $('#monthlyHrsTxtAddNewAdhoc').text('Total Patrols :');
            $('#lbl_ManningPatrolCarAdhoc_3').text('No of Patrols');
        }
        else {
            $('#monthlyHrsTxtAddNewAdhoc').text('Monthly Hrs :');
            $('#lbl_ManningPatrolCarAdhoc_3').text('Workers');
            $('#monthlyHrsAddNewAdhoc').val('');
        }

    }

    function calculateSumOfTextBoxValues2Adhoc() {

        var check = $("#positionfilterGuardAdhoc_0").prop('checked');
        if ($("#positionfilterGuardAdhoc_0").prop('checked') == true) {

            // Get the values from textbox1 and convert them to numbers
            var value1 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_0__NoOfPatrols').val()) || 0;
            var value2 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_1__NoOfPatrols').val()) || 0;
            var value3 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_2__NoOfPatrols').val()) || 0;
            var value4 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_3__NoOfPatrols').val()) || 0;
            var value5 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_4__NoOfPatrols').val()) || 0;
            var value6 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_5__NoOfPatrols').val()) || 0;
            var value7 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_6__NoOfPatrols').val()) || 0;
            var value8 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_7__NoOfPatrols').val()) || 0;
            // Calculate the sum
            var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
            // Update the value in textbox2
            $('#monthlyHrsAdhoc_0').val(sum);


        }

    }

    function calculateSumOfTextBoxValues3Adhoc() {

        var check = $("#positionfilterGuardAdhoc_8").prop('checked');
        if ($("#positionfilterGuardAdhoc_8").prop('checked') == true) {

            // Get the values from textbox1 and convert them to numbers
            var value1 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_8__NoOfPatrols').val()) || 0;
            var value2 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_9__NoOfPatrols').val()) || 0;
            var value3 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_10__NoOfPatrols').val()) || 0;
            var value4 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_11__NoOfPatrols').val()) || 0;
            var value5 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_12__NoOfPatrols').val()) || 0;
            var value6 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_13__NoOfPatrols').val()) || 0;
            var value7 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_14__NoOfPatrols').val()) || 0;
            var value8 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_15__NoOfPatrols').val()) || 0;
            // Calculate the sum
            var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
            // Update the value in textbox2
            $('#monthlyHrsAdhoc_8').val(sum);


        }

    }

    function calculateSumOfTextBoxValues4Adhoc() {

        var check = $("#positionfilterGuardAdhoc_16").prop('checked');
        if ($("#positionfilterGuardAdhoc_16").prop('checked') == true) {

            // Get the values from textbox1 and convert them to numbers
            var value1 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_16__NoOfPatrols').val()) || 0;
            var value2 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_17__NoOfPatrols').val()) || 0;
            var value3 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_18__NoOfPatrols').val()) || 0;
            var value4 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_19__NoOfPatrols').val()) || 0;
            var value5 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_20__NoOfPatrols').val()) || 0;
            var value6 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_21__NoOfPatrols').val()) || 0;
            var value7 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_22__NoOfPatrols').val()) || 0;
            var value8 = parseFloat($('#ClientSiteManningGuardKpiSettingsAdhoc_23__NoOfPatrols').val()) || 0;

            // Calculate the sum
            var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
            // Update the value in textbox2
            $('#monthlyHrsAdhoc_16').val(sum);


        }

    }

    //Adhoc end 

    $(document).on('click', '#printDivButton', function () {
        $('#printDivButton').prop('disabled', true);
        var currentDate = new Date();
        var formattedDate = formatDate(currentDate);
        var clientSiteId = $("#ClientSiteId").val();
        var clientSiteName = $('#client_site_name').text();
        var clientSiteAddress = globalClientSiteAddress;

        let content = document.getElementById('contractedmanningSettings').innerHTML;

        let element = document.createElement('div');
        element.id = 'temp-pdf-content'; // Assign an ID for easy reference
        element.style.border = '1px solid black'; // Black border with solid style
        element.style.padding = '10px'; // Padding inside the border
        element.style.boxSizing = 'border-box';
        element.style.marginTop = '20px'; // Top margin
        element.style.marginLeft = '175px'; // Left margin
        element.style.width = 'calc(100% - 40px)'; // Adjust width to fit within the page with margins
        element.style.maxWidth = '800px';
        element.style.backgroundColor = '#ffffff';
        element.style.width = '100%';

        let heading = '<div style="text-align: center; color: black;">';
        heading += '<span style="font-size: 1em; display: block;font-weight: bold;">Contracted Manning Schedule</span>';
        heading += '<span style="font-size: 0.875em; display: block;">' + clientSiteName + '</span>';
        heading += '<span style="font-size: 0.875em; display: block;">' + clientSiteAddress + '</span>';
        heading += '</div>';

        element.innerHTML = heading + content;
        document.body.appendChild(element);

        // Hide all buttons with the class 'no-print'
        let noPrintElements = element.getElementsByClassName('no-print');
        for (let el of noPrintElements) {
            el.style.display = 'none';
        }

        let opt = {
            margin: [0, 0, 0, 0],
            filename: formattedDate + 'Contracted Manning.pdf',
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: { scale: 2 },
            jsPDF: { unit: 'in', format: 'a4', orientation: 'landscape' }
        };
        html2pdf().from(element).set(opt).toPdf().output('datauristring').then(function (pdfDataUri) {
            // Create a new window and display the PDF
            let newWindow = window.open('', '_blank');
            if (newWindow) {
                newWindow.document.write('<html><head><title>Contracted Manning Schedule-Print</title></head><body style="margin:0;justify-content: center;"><iframe width="100%" height="100%" src="' + pdfDataUri + '"></iframe></body></html>');
                newWindow.document.close();
            } else {
                alert('Failed to open new window. Please check your browser settings.');
            }

            if (document.body.contains(element)) {
                document.body.removeChild(element);
            }

            $('#printDivButton').prop('disabled', false);
        }).catch(function (error) {
            console.error('Error generating PDF:', error);
            alert('Failed to generate PDF.');
        });
    });


    function formatDate(dateStr) {
        var date = new Date(dateStr);
        if (isNaN(date.getTime())) {
            return null; // Invalid date
        }

        var day = String(date.getDate()).padStart(2, '0');
        var month = String(date.getMonth() + 1).padStart(2, '0'); // Months are zero-based
        var year = date.getFullYear();

        return `${day}/${month}/${year}`;
    }

    $('#div_site_settings').on('click', '#showDivButton', function () {

        $('#divPatrolCar').show();
        $('#divbtn').show();
        $('#save_site_manning_settings_WithoutValue').hide();

    });



    $('#div_site_settings').on('change', '#positionfilterGuard', function () {

        const isChecked = $(this).is(':checked');
        const filter = isChecked ? 1 : 2;

        $.ajax({
            url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            $('#Report_Officer_Position').html('');
            data.map(function (position) {
                $('#Report_Officer_Position').append('<option value="' + position.value + '">' + position.text + '</option>');
            });
        });

    });
    $(".patrol-sum").on("input", function () {
        alert('ddd');
        calculateSum();
    });
    $('#div_site_settings').on('change', '#positionfilterPatrolCar', function () {

        const isChecked = $(this).is(':checked');
        const filter = isChecked ? 1 : 2;

        $("#ClientSiteManningPatrolCarKpiSettings_0__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettings_1__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettings_2__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettings_3__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettings_4__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettings_5__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettings_6__Type").val(filter);
        $("#ClientSiteManningPatrolCarKpiSettings_7__Type").val(filter);


        calculateSumOfTextBoxValues();

        $.ajax({
            url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').html('');
            data.map(function (position) {
                $('#ClientSiteManningPatrolCarKpiSettings_1__PositionId').append('<option value="' + position.value + '">' + position.text + '</option>');
            });
        });

    });

    $('#search_kw_client_site').on('keyup', function (event) {
        // Enter key pressed
        if (event.keyCode === 13) {
            gridSchedules.reload({ handler: getScheduleUrl(), type: $('#sel_schedule').val(), searchTerm: $(this).val() });
        }
    });

    $('#btnSearchClientSite').on('click', function () {
        gridSchedules.reload({ handler: getScheduleUrl(), type: sel_schedule_report_type, searchTerm: $('#search_kw_client_site').val() });
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
    //p2-168-Import-start
    $('#getPrevoiusMonthSummaryNotes').on('click', function () {


        $.ajax({
            url: '/admin/settings?handler=PreviousMonthSummaryNote',
            type: 'GET',
            data: {
                scheduleId: $('#KpiSendScheduleSummaryNote_ScheduleId').val(),
                month: $('#summaryNoteMonth').val(),
                year: $('#summaryNoteYear').val()
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {

            $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val(result)
            $('#lblRemainingCount').html(getNoteLength(result));
        }).fail(function () { });
    });
    //p2-1668-Import-end

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

    $('#div_site_settings').on('input', '#ClientSiteManningPatrolCarKpiSettings_0__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettings_1__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettings_2__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettings_3__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettings_4__NoOfPatrols, #ClientSiteManningPatrolCarKpiSettings_5__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettings_6__NoOfPatrols,#ClientSiteManningPatrolCarKpiSettings_7__NoOfPatrols', function () {

        calculateSumOfTextBoxValues();

    });

    $('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettings_0__NoOfPatrols,#ClientSiteManningGuardKpiSettings_1__NoOfPatrols, #ClientSiteManningGuardKpiSettings_2__NoOfPatrols, #ClientSiteManningGuardKpiSettings_3__NoOfPatrols, #ClientSiteManningGuardKpiSettings_4__NoOfPatrols, #ClientSiteManningGuardKpiSettings_5__NoOfPatrols,#ClientSiteManningGuardKpiSettings_6__NoOfPatrols,#ClientSiteManningGuardKpiSettings_7__NoOfPatrols', function () {

        calculateSumOfTextBoxValues2();

    });

    $('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettings_8__NoOfPatrols, #ClientSiteManningGuardKpiSettings_9__NoOfPatrols, #ClientSiteManningGuardKpiSettings_10__NoOfPatrols, #ClientSiteManningGuardKpiSettings_11__NoOfPatrols, #ClientSiteManningGuardKpiSettings_12__NoOfPatrols,#ClientSiteManningGuardKpiSettings_13__NoOfPatrols,#ClientSiteManningGuardKpiSettings_14__NoOfPatrols,#ClientSiteManningGuardKpiSettings_15__NoOfPatrols', function () {

        calculateSumOfTextBoxValues3();

    });

    $('#div_site_settings').on('input', '#ClientSiteManningGuardKpiSettings_16__NoOfPatrols,#ClientSiteManningGuardKpiSettings_17__NoOfPatrols, #ClientSiteManningGuardKpiSettings_18__NoOfPatrols, #ClientSiteManningGuardKpiSettings_19__NoOfPatrols, #ClientSiteManningGuardKpiSettings_20__NoOfPatrols, #ClientSiteManningGuardKpiSettings_21__NoOfPatrols,#ClientSiteManningGuardKpiSettings_22__NoOfPatrols,#ClientSiteManningGuardKpiSettings_23__NoOfPatrols', function () {

        calculateSumOfTextBoxValues4();

    });




    function calculateSumOfTextBoxValues() {

        if ($("#positionfilterPatrolCar").prop('checked') == true) {


            // Get the values from textbox1 and convert them to numbers
            var value1 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_0__NoOfPatrols').val()) || 0;
            var value2 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_1__NoOfPatrols').val()) || 0;
            var value3 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_2__NoOfPatrols').val()) || 0;
            var value4 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_3__NoOfPatrols').val()) || 0;
            var value5 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_4__NoOfPatrols').val()) || 0;
            var value6 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_5__NoOfPatrols').val()) || 0;
            var value7 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_6__NoOfPatrols').val()) || 0;
            var value8 = parseFloat($('#ClientSiteManningPatrolCarKpiSettings_7__NoOfPatrols').val()) || 0;
            // Calculate the sum
            var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
            // Update the value in textbox2
            if (sum !== 0) { $('#monthlyHrsAddNew').val(sum); }
            $('#monthlyHrsTxtAddNew').text('Total Patrols :');
            $('#lbl_ManningPatrolCar_3').text('No of Patrols');
        }
        else {
            $('#monthlyHrsTxtAddNew').text('Monthly Hrs :');
            $('#lbl_ManningPatrolCar_3').text('Workers');
            $('#monthlyHrsAddNew').val('');
        }

    }

    function calculateSumOfTextBoxValues2() {

        var check = $("#positionfilterGuard_0").prop('checked');
        if ($("#positionfilterGuard_0").prop('checked') == true) {

            // Get the values from textbox1 and convert them to numbers
            var value1 = parseFloat($('#ClientSiteManningGuardKpiSettings_0__NoOfPatrols').val()) || 0;
            var value2 = parseFloat($('#ClientSiteManningGuardKpiSettings_1__NoOfPatrols').val()) || 0;
            var value3 = parseFloat($('#ClientSiteManningGuardKpiSettings_2__NoOfPatrols').val()) || 0;
            var value4 = parseFloat($('#ClientSiteManningGuardKpiSettings_3__NoOfPatrols').val()) || 0;
            var value5 = parseFloat($('#ClientSiteManningGuardKpiSettings_4__NoOfPatrols').val()) || 0;
            var value6 = parseFloat($('#ClientSiteManningGuardKpiSettings_5__NoOfPatrols').val()) || 0;
            var value7 = parseFloat($('#ClientSiteManningGuardKpiSettings_6__NoOfPatrols').val()) || 0;
            var value8 = parseFloat($('#ClientSiteManningGuardKpiSettings_7__NoOfPatrols').val()) || 0;
            // Calculate the sum
            var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
            // Update the value in textbox2
            $('#monthlyHrs_0').val(sum);


        }

    }

    function calculateSumOfTextBoxValues3() {

        var check = $("#positionfilterGuard_8").prop('checked');
        if ($("#positionfilterGuard_8").prop('checked') == true) {

            // Get the values from textbox1 and convert them to numbers
            var value1 = parseFloat($('#ClientSiteManningGuardKpiSettings_8__NoOfPatrols').val()) || 0;
            var value2 = parseFloat($('#ClientSiteManningGuardKpiSettings_9__NoOfPatrols').val()) || 0;
            var value3 = parseFloat($('#ClientSiteManningGuardKpiSettings_10__NoOfPatrols').val()) || 0;
            var value4 = parseFloat($('#ClientSiteManningGuardKpiSettings_11__NoOfPatrols').val()) || 0;
            var value5 = parseFloat($('#ClientSiteManningGuardKpiSettings_12__NoOfPatrols').val()) || 0;
            var value6 = parseFloat($('#ClientSiteManningGuardKpiSettings_13__NoOfPatrols').val()) || 0;
            var value7 = parseFloat($('#ClientSiteManningGuardKpiSettings_14__NoOfPatrols').val()) || 0;
            var value8 = parseFloat($('#ClientSiteManningGuardKpiSettings_15__NoOfPatrols').val()) || 0;
            // Calculate the sum
            var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
            // Update the value in textbox2
            $('#monthlyHrs_8').val(sum);


        }

    }

    function calculateSumOfTextBoxValues4() {

        var check = $("#positionfilterGuard_16").prop('checked');
        if ($("#positionfilterGuard_16").prop('checked') == true) {

            // Get the values from textbox1 and convert them to numbers
            var value1 = parseFloat($('#ClientSiteManningGuardKpiSettings_16__NoOfPatrols').val()) || 0;
            var value2 = parseFloat($('#ClientSiteManningGuardKpiSettings_17__NoOfPatrols').val()) || 0;
            var value3 = parseFloat($('#ClientSiteManningGuardKpiSettings_18__NoOfPatrols').val()) || 0;
            var value4 = parseFloat($('#ClientSiteManningGuardKpiSettings_19__NoOfPatrols').val()) || 0;
            var value5 = parseFloat($('#ClientSiteManningGuardKpiSettings_20__NoOfPatrols').val()) || 0;
            var value6 = parseFloat($('#ClientSiteManningGuardKpiSettings_21__NoOfPatrols').val()) || 0;
            var value7 = parseFloat($('#ClientSiteManningGuardKpiSettings_22__NoOfPatrols').val()) || 0;
            var value8 = parseFloat($('#ClientSiteManningGuardKpiSettings_23__NoOfPatrols').val()) || 0;

            // Calculate the sum
            var sum = value1 + value2 + value3 + value4 + value5 + value6 + value7 + value8;
            // Update the value in textbox2
            $('#monthlyHrs_16').val(sum);


        }

    }


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
















$('#save_default_email').on('click', function () {
    const token = $('input[name="__RequestVerificationToken"]').val();
    var Email = $('#txt_defaultEmail').val();
    var emailsArray = Email.split(',');
    isValidEmailIds = true;
    for (var i = 0; i < emailsArray.length; i++) {
        var emailAddress = emailsArray[i].trim();
        if (isValidEmail(emailAddress)) {

        }
        else {
            isValidEmailIds = false;
            alert("Invalid email address.'" + emailAddress + "'");
        }



    }

    if (isValidEmailIds) {
        $.ajax({
            url: '/Admin/Settings?handler=SaveDeafultMailBox',
            data: { Email: Email },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            alert("The Default Mailbox was saved successfully");
        })

    }

    function isValidEmail(email) {
        // Regular expression for basic email validation
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailPattern.test(email);
    }
})

//Code to handle timesheet schedule start
function clearScheduleModalTimesheet() {

    $('#TimeSheetscheduleId').val('0');
    $('#clientTypeNameTimesheet').val('');
    $('#clientSitesTimesheet').html('<option value="">Select</option>');
    $('#selectedSitesTimeSheet').html('');
    updateSelectedSitesTimesheetCount();
    $('input:hidden[name="clientSiteIds"]').remove();
    $('#clientTypeName option:eq(0)').attr('selected', true);
    $('#startDateTimesheet').val('');
    $('#startDateTimesheet').removeAttr('min');
    $('#endDateTimesheet').val('');
    $('#endDateTimesheet').removeAttr('min');
    $('#Timesheettime').val('');
    $('#frequency option').removeAttr('selected');
    $('#frequency').val('');
    $('#nextRunOn').val('');
    $('#sch-modal-validation').html('');
    $('#emailToTimeSheet').val('');
    $('#emailBccTimeSheet').val('');

    $('#projectNameTimeSheet').val('');

}
$('#TimeSheetschedule-modal').on('shown.bs.modal', function (event) {
    clearScheduleModalTimesheet();
    const button = $(event.relatedTarget);
    const isEdit = button.data('action') !== undefined && button.data('action') === 'editSchedule';
    if (isEdit) {
        schId = button.data('sch-id');
        scheduleModalOnEditTimesheet(schId);
    } else {
        //P2-103 Duplicate Settings-start
        const isCopy = button.data('action') !== undefined && button.data('action') === 'copySchedule1';
        if (isCopy) {
            schId = button.data('sch-id');
            scheduleModalOnCopy1(schId);
        } else {
            scheduleModalOnAddTimesheet();
        }
        //P2-103 Duplicate Settings-end
    }

    // showHideSchedulePopupTabs(isEdit);
});
$('#clientTypeNameTimesheet').on('change', function () {
    const option = $(this).val();
    if (option === '') {
        $('#clientSitesTimesheet').html('');
        $('#clientSitesTimesheet').append('<option value="">Select</option>');
    }

    $.ajax({
        url: '/dashboard?handler=ClientSitesUsingUserId&type=' + encodeURIComponent(option) + "&guardId=" + encodeURIComponent($("#hid_userId").val() || $("#hid_userIdSettings").val() || 0),
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        $('#clientSitesTimesheet').html('');
        $('#clientSitesTimesheet').append('<option value="">Select</option>');
        data.map(function (site) {
            $('#clientSitesTimesheet').append('<option value="' + site.value + '">' + site.text + '</option>');
        });
    });
});
$('#clientSitesTimesheet').on('change', function () {
    const elem = $(this).find(":selected");
    if (elem.val() !== '') {
        const existing = $('#selectedSitesTimeSheet option[value="' + elem.val() + '"]');
        if (existing.length === 0) {
            $('#selectedSitesTimeSheet').append('<option value="' + elem.val() + '">' + elem.text() + '</option>');
            updateSelectedSitesTimesheetCount();
        }
    }
});

function scheduleModalOnAddTimesheet() {
    const dateToday = new Date().toISOString().split('T')[0];
    $('#startDateTimesheet').val(dateToday);
    $('#startDateTimesheet').attr('min', dateToday);
    const dateEnd = '2100-01-01'
    $('#endDateTimesheet').val(dateEnd.split('T')[0]);

    $("textarea[id='KpiSendScheduleSummaryNote_Notes']").val('');

}
function scheduleModalOnEditTimesheet(scheduleId) {
    $('#loader').show();
    $.ajax({
        url: '/Admin/Settings?handler=KpiTimesheetSchedule&id=' + scheduleId,
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {

        $('#TimeSheetscheduleId').val(data.id);
        $('#startDateTimesheet').val(data.startDate.split('T')[0]);
        if (data.endDate)
            $('#endDateTimesheet').val(data.endDate.split('T')[0]);
        $('#endDateTimesheet').attr('min', new Date().toISOString().split('T')[0]);
        $('#frequency').val(data.frequency).change();
        $('#frequency option[value="' + data.frequency + '"]').attr('selected', true);
        $('#Timesheettime').val(data.time);
        $('#nextRunOnTimesheet').val(data.nextRunOn);
        $('#emailToTimeSheet').val(data.emailTo);
        $('#emailBccTimeSheet').val(data.emailBcc)

        $.each(data.kpiSendTimesheetClientSites, function (index, item) {
            $('#selectedSitesTimeSheet').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
            updateSelectedSitesTimesheetCount();
        });
        $('#projectNameTimeSheet').val(data.projectName);


    }).always(function () {
        $('#loader').hide();
    });
}
$('#removeSelectedSitesTimesheet').on('click', function () {
    $('#selectedSitesTimeSheet option:selected').remove();
    updateSelectedSitesTimesheetCount();
});
function updateSelectedSitesTimesheetCount() {
    $('#selectedSitesCountTimesheet').text($('#selectedSitesTimeSheet option').length);
}

$('#editSelectedSiteTimesheet').on('click', function () {
    if ($('#editSiteTrigger').length === 1) {
        $('#editSiteTrigger').remove()
    }

    const selectedOption = $('#selectedSitesTimeSheet option:selected');
    if (selectedOption.length == 0) {
        alert('Please select a site to edit');
    } else if (selectedOption.length > 1) {
        alert('Select only one site to edit');
    } else {

        let triggerButton = '<button type="button" id="editSiteTrigger" style="display:none" data-toggle="modal" data-target="#kpi-settings-modal" ' +
            //p1-139 change pop up start
            'data-cs-id="' + $(selectedOption).val() + '" data-cs-name="' + $(selectedOption).text() + '" data-type-tab="KPI"></button>';
        //p1-139 change pop up end
        $(triggerButton).insertAfter($(this));
        $('#editSiteTrigger').click();

    }
});

function scheduleModalOnCopy1(scheduleId) {
    $.ajax({
        url: '/Admin/Settings?handler=KpiTimesheetSchedule&id=' + scheduleId,
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        const dateToday = new Date().toISOString().split('T')[0];
        $('#startDateTimesheet').val(dateToday);
        $('#startDateTimesheet').attr('min', dateToday);
        const dateEnd = '2100-01-01'
        $('#endDateTimesheet').val(dateEnd.split('T')[0]);

        $('#TimeSheetscheduleId').val(data.id);
        // $('#startDateTimesheet').val(data.startDate.split('T')[0]);
        //if (data.endDate)
        //    $('#endDateTimesheet').val(data.endDate.split('T')[0]);
        //$('#endDateTimesheet').attr('min', new Date().toISOString().split('T')[0]);
        $('#frequency').val(data.frequency).change();
        $('#frequency option[value="' + data.frequency + '"]').attr('selected', true);
        $('#Timesheettime').val(data.time);
        $('#nextRunOnTimesheet').val(data.nextRunOn);
        $('#emailToTimeSheet').val(data.emailTo);
        $('#emailBccTimeSheet').val(data.emailBcc)

        $.each(data.kpiSendTimesheetClientSites, function (index, item) {
            $('#selectedSitesTimeSheet').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
            updateSelectedSitesTimesheetCount();
        });
        //$('#projectNameTimesheet').val(data.projectName);


    }).always(function () {
        $('#loader').hide();
    });
}

$('#run-schedule-modal1').on('shown.bs.modal', function (event) {
    const button = $(event.relatedTarget);
    const schId = button.data('sch-id');
    $('#sch-id').val(schId);
    $('#btnScheduleRunTime').prop('disabled', false);
    $('#schRunStatus').html('');
});

$('#btnScheduleRunTime').on('click', function () {
    $('#btnScheduleRunTime').prop('disabled', true);
    $('#schRunStatus').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating Report. Please wait...');
    $.ajax({
        url: '/Admin/Settings?handler=RunScheduleKV',
        type: 'POST',
        data: {
            scheduleId: $('#sch-id').val(),
            reportYear: $('#schRunYear').val(),
            reportMonth: $('#schRunMonth').val(),
            ignoreRecipients: $('#cbIgnoreRecipients').is(':checked'),
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {
        $('#btnScheduleRunTime').prop('disabled', false);
        const messageHtml = result.success ? '<i class="fa fa-check-circle-o text-success"></i> Done. Report sent via email' :
            '<i class="fa fa-times-circle text-danger"></i> Error. Check log for more details';
        $('#schRunStatusTimesheet').html(messageHtml);
    });
});


$('#btnScheduleDownload1').on('click', function () {
    $('#btnScheduleDownload').prop('disabled', true);
    $('#schRunStatus').html('<i class="fa fa-circle-o-notch fa-spin text-primary"></i> Generating PDF. Please wait...');
    $.ajax({
        type: 'GET',
        url: '/Admin/Settings?handler=DownloadPdfTimesheet',
        data: {
            scheduleId: $('#sch-id').val(),
            reportYear: $('#schRunYear1').val(),
            reportMonth: $('#schRunMonth1').val(),
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
            // // Create a temporary anchor element to trigger the download
            //var url = window.URL.createObjectURL(blob);
            // // Open the PDF in a new tab
            //var newTab = window.open(url, '_blank');

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

            //if (!newTab) {
            //    // If the new tab was blocked, fallback to downloading the file
            //    var a = document.createElement('a');
            //    a.href = url;
            //    a.download = downloadedFileName;
            //    a.click();
            //}
            //window.URL.revokeObjectURL(url);                
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




$('#div_site_settings').on('change', '#dgKPITelamaticsName', function () {
    const NameId = $(this).val();
    $('#KPITelematicsFieldID').val(NameId);

    $.ajax({
        url: '/Admin/Settings?handler=MobileNo&Id=' + NameId,
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            if (data != null) {
                $('#KPITelematicsMobileNo').val(data.mobile);
                $('#emailLink').attr('href', 'mailto:' + data.email);
            } else {
                $('#KPITelematicsMobileNo').val('');
                $('#emailLink').attr('href', 'mailto:');
            }

            $('#KPITelematicsFieldID').val(data.id);
            //$('#KPITelematicsMobileNo').append(new Option('Select', '', true, true));
            data.map(function (site) {
                //$('#KPITelematicsMobileNo').append(new Option(site.name, site.id, false, false));
            });
            /* vkl multiselect */
            data.map(function (site) {
                //clientSiteControlvkl.append('<option value="' + site.id + '">' + site.name + '</option>');
            });
            //clientSiteControlvkl.multiselect('rebuild');
        }
    });

});


$('#div_site_settings').on('change', '#positionfilterPatrolCarDuressApp', function () {

    const isChecked = $(this).is(':checked');
    const filter = isChecked ? 1 : 2;
    $.ajax({
        url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
        type: 'GET',
        dataType: 'json'
    }).done(function (data) {
        $('#positionfilterPatrolCarDuressApp_Pertrol').html('');
        data.map(function (position) {
            $('#positionfilterPatrolCarDuressApp_Pertrol').append('<option value="' + position.value + '">' + position.text + '</option>');
        });
    });

});


$('#div_site_settings').on('click', '#save_DuressApp', function () {
    var positionFilter = $('#positionfilterPatrolCarDuressApp').is(':checked') ? 'Patrol Car' : 'Guard';
    var selectedPosition = $('#positionfilterPatrolCarDuressApp_Pertrol').val();
    var siteDuressNumber = $('#siteDuressNumber').val();
    var clientSiteIdDuress = $('#clientSiteIdDuress').val(); // Get value from hidden input
    var _profileId = $('#selectLogActivityProfile').val();


    // ✅ Clear previous error messages
    $('.error-message').remove();

    // ✅ Validation check
    var isValid = true;

    if (!selectedPosition) {
        $('#positionfilterPatrolCarDuressApp_Pertrol').after('<span class="text-danger error-message">Please select a position.</span>');
        isValid = false;
    }

    if (!siteDuressNumber) {
        $('#siteDuressNumber').after('<span class="text-danger error-message">Please select a site duress number.</span>');
        isValid = false;
    }

    if (!clientSiteIdDuress) {
        $('#clientSiteIdDuress').after('<span class="text-danger error-message">Client site ID is missing.</span>');
        isValid = false;
    }

    if (!_profileId) {
        $('#selectLogActivityProfile').after('<span class="text-danger error-message">Please select a log profile.</span>');
        isValid = false;
    }

    // ❌ Stop AJAX call if validation fails
    if (!isValid) return;
    $.ajax({
        url: '/Admin/Settings?handler=SaveDuressApp',
        type: 'POST',
        data: {
            positionFilter: positionFilter,
            selectedPosition: selectedPosition,
            siteDuressNumber: siteDuressNumber,
            clientSiteIdDuress: clientSiteIdDuress,
            duressAppId: $('#duressAppId').val(),
            logProfileId: _profileId,
        },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (result) {

        if (result.success) {
            var successMessage = '<div class="alert alert-success alert-dismissible fade show" role="alert">' +
                '<strong>Success!</strong> Duress APP settings saved successfully.' +
                '<button type="button" class="close" data-dismiss="alert" aria-label="Close">' +
                '<span aria-hidden="true">&times;</span>' +
                '</button>' +
                '</div>';
            $('#messagesContainer').html(successMessage);  // Assuming you have a div with id 'messagesContainer'
            populateDuressApp(clientSiteIdDuress, siteDuressNumber);
        }
    });


});


$('#div_site_settings').on('change', '#siteDuressNumber', function () {
    var siteDuressNumber = $(this).val();
    var clientSiteIdDuress = $('#clientSiteIdDuress').val(); // Get client site ID
    $('#messagesContainer').html('');
    if (!siteDuressNumber || !clientSiteIdDuress) {
        clearDuressAppForm();
        return;
    }

    $.ajax({
        url: '/admin/settings?handler=GetDuressApp',
        type: 'GET',
        data: {
            clientSiteIdDuress: clientSiteIdDuress,
            siteDuressNumber: siteDuressNumber
        },
        success: function (result) {
            if (result.success === false) {
                /*clearDuressAppForm();*/
                $("#QrCode").attr("src", "");
                $("#QrCode").attr("src", qrImagePath).hide();
                $('#positionfilterPatrolCarDuressApp_Pertrol').val(''); // Clear dropdown
                $('#positionfilterPatrolCarDuressApp').prop('checked', false); // Uncheck the checkbox
                const filter = 2;
                $.ajax({
                    url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
                    type: 'GET',
                    dataType: 'json'
                }).done(function (data) {
                    $('#positionfilterPatrolCarDuressApp_Pertrol').html('');
                    data.map(function (position) {
                        $('#positionfilterPatrolCarDuressApp_Pertrol').append('<option value="' + position.value + '">' + position.text + '</option>');

                    });

                });
                return;
            }


            $('#siteDuressNumber').val(result.data.siteDuressNumber);
            $('#duressAppId').val(result.data.id);
            $('#clientSiteIdDuress').val(clientSiteIdDuress);
            $('#selectLogActivityProfile').val(result.data.logProfileId);
            var qrText = result.data.id; // Change this to any text or URL
            var qrImagePath = 'https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=' + qrText;

            $("#QrCode").attr("src", qrImagePath)
            $("#QrCode").attr("src", qrImagePath).show();

            if (result.data.positionFilter === 'Patrol Car') {
                // If position filter is "Patrol Car", check the checkbox
                $('#positionfilterPatrolCarDuressApp').prop('checked', true);
                const filter = 1;
                $.ajax({
                    url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
                    type: 'GET',
                    dataType: 'json'
                }).done(function (data) {
                    $('#positionfilterPatrolCarDuressApp_Pertrol').html('');
                    data.map(function (position) {
                        $('#positionfilterPatrolCarDuressApp_Pertrol').append('<option value="' + position.value + '">' + position.text + '</option>');
                    });
                    $('#positionfilterPatrolCarDuressApp_Pertrol').val(result.data.selectedPosition);
                });
            } else {
                $('#positionfilterPatrolCarDuressApp').prop('checked', false);
                const filter = 2;
                $.ajax({
                    url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
                    type: 'GET',
                    dataType: 'json'
                }).done(function (data) {
                    $('#positionfilterPatrolCarDuressApp_Pertrol').html('');
                    data.map(function (position) {
                        $('#positionfilterPatrolCarDuressApp_Pertrol').append('<option value="' + position.value + '">' + position.text + '</option>');

                    });
                    $('#positionfilterPatrolCarDuressApp_Pertrol').val(result.data.selectedPosition);
                });
            }

        },
        error: function (xhr, status, error) {
            console.error("AJAX Error: " + status + " " + error);
            clearDuressFields();
        }
    });
});


function populateDuressApp(clientSiteIdDuress, siteDuressNumber) {
    $.ajax({
        url: '/admin/settings?handler=GetDuressApp',
        type: 'GET',
        data: {
            clientSiteIdDuress: clientSiteIdDuress,
            siteDuressNumber: siteDuressNumber
        },
        success: function (result) {
            if (result.success === false) {
                // Handle error or no data (e.g., show a message or log it)
                console.log(result.message); // Log or show the error message
                // Clear the form if no data is found
                clearDuressAppForm();
                return;
            }

            // Populate the form fields with the retrieved data

            $('#siteDuressNumber').val(result.data.siteDuressNumber);
            $('#duressAppId').val(result.data.id);
            $('#clientSiteIdDuress').val(clientSiteIdDuress);
            $('#selectLogActivityProfile').val(result.data.logProfileId);
            var qrText = result.data.id; // Change this to any text or URL
            var qrImagePath = 'https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=' + qrText;
            $("#QrCode").attr("src", qrImagePath).show(); // Hide initially

            if (result.data.positionFilter === 'Patrol Car') {
                // If position filter is "Patrol Car", check the checkbox
                $('#positionfilterPatrolCarDuressApp').prop('checked', true);
                const filter = 1;
                $.ajax({
                    url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
                    type: 'GET',
                    dataType: 'json'
                }).done(function (data) {
                    $('#positionfilterPatrolCarDuressApp_Pertrol').html('');
                    data.map(function (position) {
                        $('#positionfilterPatrolCarDuressApp_Pertrol').append('<option value="' + position.value + '">' + position.text + '</option>');
                    });
                    $('#positionfilterPatrolCarDuressApp_Pertrol').val(result.data.selectedPosition);
                });
            } else {
                $('#positionfilterPatrolCarDuressApp').prop('checked', false);
                const filter = 2;
                $.ajax({
                    url: '/admin/settings?handler=OfficerPositions&filter=' + filter,
                    type: 'GET',
                    dataType: 'json'
                }).done(function (data) {
                    $('#positionfilterPatrolCarDuressApp_Pertrol').html('');
                    data.map(function (position) {
                        $('#positionfilterPatrolCarDuressApp_Pertrol').append('<option value="' + position.value + '">' + position.text + '</option>');

                    });
                    $('#positionfilterPatrolCarDuressApp_Pertrol').val(result.data.selectedPosition);
                });
            }



        },
        error: function (xhr, status, error) {
            // Handle AJAX request failure
            console.error("AJAX Error: " + status + " " + error);
            // Clear the form in case of an error
            clearDuressAppForm();
        }
    });
}


function clearDuressAppForm() {
    $('#messagesContainer').html('');
    $('#positionfilterPatrolCarDuressApp_Pertrol').val(''); // Clear dropdown
    $('#siteDuressNumber').val(''); // Clear dropdown
    $('#positionfilterPatrolCarDuressApp').prop('checked', false); // Uncheck the checkbox
    $("#QrCode").attr("src", "").hide(); // Hide initially
}


$('#div_site_settings').on('click', '#delete_DuressApp', function () {
    var duressAppId = parseInt($('#duressAppId').val(), 10);

    if (!duressAppId || duressAppId === 0) {
        alert("No item found for deletion.");
        return;
    }

    $.ajax({
        url: '/Admin/Settings?handler=DeleteDuressApp',
        type: 'POST',
        data: { duressAppId: duressAppId },  // Using parsed integer value
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
    }).done(function (result) {
        if (result.success) {
            var successMessage = `
                <div class="alert alert-success alert-dismissible fade show" role="alert">
                    <strong>Success!</strong> Duress APP settings removed successfully.
                    <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>`;
            $('#messagesContainer').html(successMessage);
            populateDuressApp($('#clientSiteIdDuress').val(), 1);
        } else {
            alert(result.message || "Failed to delete DuressApp.");
        }
    }).fail(function () {
        alert("Error occurred while deleting DuressApp.");
    });
});

$('#div_site_settings').on('click', '#btnSaveCrowdControlSettings', function () {
    var data = {
        'Id': 0,
        'ClientSiteId': $('#clientSiteMobileAppSettings_ClientSiteId').val(),
        'IsCrowdCountEnabled': $('#clientSiteMobileAppSettings_IsCrowdCountEnabled').is(':checked'),
        'IsDoorEnabled': $('#clientSiteMobileAppSettings_IsDoorEnabled').is(':checked'),
        'IsGateEnabled': $('#clientSiteMobileAppSettings_IsGateEnabled').is(':checked'),
        'IsLevelFloorEnabled': $('#clientSiteMobileAppSettings_IsLevelFloorEnabled').is(':checked'),
        'IsRoomEnabled': $('#clientSiteMobileAppSettings_IsRoomEnabled').is(':checked'),
        'CounterQuantity': $('#clientSiteMobileAppSettings_CounterQuantity option:selected').val(),
    };

    let ccid = $('#clientSiteMobileAppSettings_Id').val();
    if (ccid != null) {
        if (parseInt(ccid) > 0) {
            data.id = parseInt(ccid);
        }
    }
    //console.log("data Id:" + data.Id);
    //console.log("data ClientSiteId:" + data.ClientSiteId);
    //console.log("data IsCrowdCountEnabled:" + data.IsCrowdCountEnabled);
    //console.log("data IsDoorEnabled:" + data.IsDoorEnabled);
    //console.log("data IsGateEnabled:" + data.IsGateEnabled);
    //console.log("data CounterQuantity:" + data.CounterQuantity);

    if ((data.IsCrowdCountEnabled) && (data.IsDoorEnabled == false && data.IsGateEnabled == false && data.IsLevelFloorEnabled == false && data.IsRoomEnabled == false)) {
        alert("Please select atleast one Counter Location.");
        return;
    }
    if (data.IsDoorEnabled || data.IsGateEnabled || data.IsLevelFloorEnabled || data.IsRoomEnabled) {
        ccqty = parseInt(data.CounterQuantity);
        if (ccqty < 1) {
            alert("Counter Quantity must be greater than 0.");
            return;
        }
    }
    if (data.IsCrowdCountEnabled == false) {
        data.IsDoorEnabled = false;
        data.IsGateEnabled = false;
        data.IsLevelFloorEnabled = false;
        data.IsRoomEnabled = false;
        data.CounterQuantity = 0;

        $('#clientSiteMobileAppSettings_IsDoorEnabled').prop('checked', false);
        $('#clientSiteMobileAppSettings_IsGateEnabled').prop('checked', false);
        $('#clientSiteMobileAppSettings_IsLevelFloorEnabled').prop('checked', false);
        $('#clientSiteMobileAppSettings_IsRoomEnabled').prop('checked', false);
        $('#clientSiteMobileAppSettings_CounterQuantity').val('0');
    }

    $('#loader').show();
    $.ajax({
        url: '/Admin/Settings?handler=SaveClientSiteMobileAppCrowdSettings',
        type: 'POST',
        dataType: 'json',
        data: { csmas: data },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        if (response.success) {
            $('#clientSiteMobileAppSettings_Id').val(response.clientSiteMobileAppSettings.id);
        }
        alert(response.message);
    }).fail(function () {
        alert('An error occured while saving the settings. Please try again.');
    }).always(function () { $('#loader').hide(); });
});

$('#div_site_settings').on('click', '#btnSavePatrolTouringModeSettings', function () {
    var data = {
        clientSiteId: parseInt($('#clientSiteIdPatrolTourMode').val()),
        PatrolTourMode: parseInt($('#ClientSite_PatrolTourMode').val())
    };

    $('#loader').show();
    $.ajax({
        url: '/Admin/Settings?handler=SaveClientSiteMobileAppPatrolTouringModeSettings',
        type: 'POST',
        dataType: 'json',
        data: data,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        alert(response.message);
    }).fail(function () {
        alert('An error occured while saving the settings. Please try again.');
    }).always(function () { $('#loader').hide(); });
});


$('#div_site_settings').on('click', '#btnSaveMobileAppIRSettings', function () {
    var data = {
        clientSiteId: parseInt($('#clientSiteIMobileAppIRTab').val()),
        mobAppShowClientTypeandSite: $('#MobAppShowClientTypeandSiteSwitch').is(':checked')
    };

    $('#loader').show();

    $.ajax({
        url: '/Admin/Settings?handler=SavemobAppShowClientTypeandSiteSettings',
        type: 'POST',
        dataType: 'json',
        data: data,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    })
        .done(function (response) {
            alert(response.message);
        })
        .fail(function () {
            alert('An error occurred while saving the settings. Please try again.');
        })
        .always(function () {
            $('#loader').hide();
        });
});


//Code to handle timesheet schedule stop

//$('#div_site_settings').on('click', '#btnSaveGuardSiteSettingsnew', function () {
//    var isUpdateDailyLog = false;
//    console.log('Site.js btnSaveGuardSiteSettings...');
//    const token = $('input[name="__RequestVerificationToken"]').val();
//    if ($('#enableLogDump').is(":checked")) {
//        isUpdateDailyLog = true;
//    }
//    $.ajax({
//        url: '/Admin/Settings?handler=SaveSiteEmail',
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
//menu change 04-03-2024 end


