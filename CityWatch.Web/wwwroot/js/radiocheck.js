$(function () {
    // TODO: Avoid duplication
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

    function getFormattedDateFromString(dateString) {
        let dateValue = new Date(dateString.split('T')[0]);
        return dateValue.toLocaleDateString('en-GB', {
            day: '2-digit', month: 'short', year: 'numeric'
        });
    }

    function getTimeFromDateTime(value) {
        const mins = (value.getMinutes() < 10 ? '0' : '') + value.getMinutes();
        const hours = (value.getHours() < 10 ? '0' : '') + value.getHours();
        return hours + ':' + mins;
    }

    function startOfWeek(date) {
        var diff = date.getDate() - date.getDay() + (date.getDay() === 0 ? -6 : 1);

        return new Date(date.setDate(diff));
    }

    function getDayAndCheck(colIndex) {
        let i = 0, j = 1, n = 0;
        let found = false;

        for (; i < 7; i++) {
            for (j = 1; j < 4; j++) {
                n++;
                if (n == colIndex) {
                    found = true;
                    break;
                }
            }
            if (found)
                break;
        }
        return [i, j];
    }

    $('#rcClientType').on('change', function () {
        const clientSiteControl = $('#rcClientSiteId');
        clientSiteControl.html('');

        $.ajax({
            url: '/Radio/Check?handler=ClientSites&type=' + encodeURIComponent($(this).val()),
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

    $('#rcClientSiteId').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
        onDropdownHide: function (event) {
            $('#week-nav-controls').hide();
            if ($('#rcClientSiteId').val().length > 0) {
                $('#week-nav-controls').show();
            }
            weeklyRadioChecklist.ajax.reload(onRadioCheckGridReload);
        }
    });

    let weeklyRadioChecklist = $('#weekly_radio_checklist').DataTable({
        paging: false,
        ordering: false,
        info: false,
        searching: false,
        fixedColumns: {
            left: 1
        },
        scrollCollapse: true,
        scrollX: true,
        autoWidth: false,
        ajax: {
            url: '/Radio/Check?handler=RadioStatus',
            datatype: 'json',
            data: function (d) {
                d.clientSiteIds = $('#rcClientSiteId').val().join(',');
                d.weekStart = $('#selectedWeekStart').val();
            },
            dataSrc: ''
        },
        columns: [
            { data: 'clientSite.name', width: '200px' },
            { data: 'days.Monday.check1' },
            { data: 'days.Monday.check2' },
            { data: 'days.Monday.check3' },
            { data: 'days.Tuesday.check1' },
            { data: 'days.Tuesday.check2' },
            { data: 'days.Tuesday.check3' },
            { data: 'days.Wednesday.check1' },
            { data: 'days.Wednesday.check2' },
            { data: 'days.Wednesday.check3' },
            { data: 'days.Thursday.check1' },
            { data: 'days.Thursday.check2' },
            { data: 'days.Thursday.check3' },
            { data: 'days.Friday.check1' },
            { data: 'days.Friday.check2' },
            { data: 'days.Friday.check3' },
            { data: 'days.Saturday.check1' },
            { data: 'days.Saturday.check2' },
            { data: 'days.Saturday.check3' },
            { data: 'days.Sunday.check1' },
            { data: 'days.Sunday.check2' },
            { data: 'days.Sunday.check3' },
        ],
        columnDefs: [{
            'targets': '_all',
            'createdCell': function (td, cellData, rowData, row, col) {
                $(td).attr('data-site-id', rowData.clientSite.id);
                $(td).attr('data-col-idx', col);
                if (col > 0 && col <= 21 && col <= rowData.lastEditableColumnIndex) {
                    $(td).addClass('cell-radio-status');
                }
            }
        }]
    });

    $('#weekly_radio_checklist').on('dblclick', 'tr td.cell-radio-status', function (event) {
        const siteId = $(this).data('site-id');
        const colIndex = $(this).data('col-idx');
        if (colIndex > 0) {
            $('#clientSiteId').val(siteId);
            $('#colIndex').val(colIndex);   
            $('#selectRadioStatus option:last').remove();
            const timeNow = getTimeFromDateTime(new Date());
            const $option = $("<option/>", { value: timeNow, text: timeNow });
            $('#selectRadioStatus').append($option);
            $('#selectRadioStatus').val('');
            $('#selectRadioCheckStatus').modal('show');
        }
    });

    $('#btnSaveRadioStatus').on('click', function () {
        const newValue = $('#selectRadioStatus').val();
        if (newValue === '') {
            return;
        }

        const [dateOffset, checkNumber] = getDayAndCheck(parseInt($('#colIndex').val()));
        $.ajax({
            url: '/Radio/Check?handler=SaveDayRadioStatus',
            type: 'POST',
            data: {
                clientSiteId: $('#clientSiteId').val(),
                weekStart: $('#selectedWeekStart').val(),
                dateOffset: dateOffset,
                checkNumber: checkNumber,
                newValue: newValue,
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            $('#selectRadioCheckStatus').modal('hide');
            weeklyRadioChecklist.ajax.reload(onRadioCheckGridReload);
        });
    });

    $('#btnPreviousWeekRadioStatus').on('click', function () {
        setSelectedWeekStart(-1);
    });

    $('#btnNextWeekRadioStatus').on('click', function () {
        setSelectedWeekStart(1);
    });

    $('#btnCurrentWeekRadioStatus').on('click', function () {
        $('#selectedWeekStart').val('');
        setSelectedWeekStart(0);
    });

    function setSelectedWeekStart(pos) {
        const weekStartValue = $('#selectedWeekStart').val();
        let weekStart = weekStartValue ? new Date(weekStartValue) : startOfWeek(new Date());
        const previousWeekStart = new Date(weekStart.getFullYear(), weekStart.getMonth(), weekStart.getDate() + (7 * pos));
        $('#selectedWeekStart').val(getFormattedDate(previousWeekStart, null, '-'));
        weeklyRadioChecklist.ajax.reload(onRadioCheckGridReload);
    }

    function onRadioCheckGridReload() {
        var firstRow = weeklyRadioChecklist.rows(0).data()[0];
        if (firstRow) {
            showSelectedWeekInfo(firstRow);
        }
    }

    function showSelectedWeekInfo(firstRow) {
        const startDate = firstRow.days['Monday'].date;
        const endDate = firstRow.days['Sunday'].date;
        const message = 'Week : ' + getFormattedDateFromString(startDate) + ' - ' + getFormattedDateFromString(endDate);
        $('#selectedWeekInfo').html(message);
    }
});