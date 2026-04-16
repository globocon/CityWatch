$(document).ready(function () {
    let grid_V2;
    let cachedStates = [];

    // Initialize the new grid
    let $grid = $('#BroadCastBannerCalendarEvents_V2');
    if ($grid.length === 0) {
        $grid = $('#BroadCastBannerCalendarEvents_V2_Admin');
    }

    if ($grid.length > 0) {
        grid_V2 = $grid.grid({
            dataSource: '/Admin/Settings?handler=BroadcastCalendarEvents',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'id', title: 'Id', hidden: true },
            { field: 'referenceNo', title: 'Ref No', width: 100 },
            { field: 'textMessage', title: 'Event Message', width: 450 },
            { field: 'formattedStartDate', title: 'Start Date', width: 140 },
            { field: 'formattedExpiryDate', title: 'Expiry Date', width: 140 },
            { 
                field: 'repeatYearly', 
                title: 'Repeat', 
                width: 80, 
                align: 'center',
                renderer: (value) => value ? '<i class="fa fa-check-circle text-success"></i>' : '<i class="fa fa-times-circle text-muted"></i>'
            },
            {
                field: 'isPublicHoliday',
                title: 'PH',
                width: 150,
                renderer: (value, record) => {
                    if (value === true || value === "true" || value === 1 || value === "1") {
                        return `<span class="badge badge-warning p-1" title="${record.states || 'All States'}"><i class="fa fa-flag mr-1"></i>PH</span> <small class="text-muted">${record.states ? record.states.substring(0, 10) + (record.states.length > 10 ? '...' : '') : 'ALL'}</small>`;
                    }
                    return '<span class="badge badge-light border text-muted p-1">Regular</span>';
                }
            },
            {
                width: 100,
                title: '',
                align: 'center',
                renderer: (value, record) => {
                    return `<div class="btn-group">
                                <button class="btn btn-sm btn-outline-primary mr-1" onclick="openCalendarModal(${record.id}, ${JSON.stringify(record).replace(/"/g, '&quot;')})" title="Edit"><i class="fa fa-pencil"></i></button>
                                <button class="btn btn-sm btn-outline-danger" onclick="deleteCalendarEvent(${record.id})" title="Delete"><i class="fa fa-trash"></i></button>
                            </div>`;
                }
            }
        ],
        pager: { limit: 10, sizes: [10, 20, 50] }
    });

    // Handle background coloring for PH rows
    grid_V2.on('rowDataBound', function (e, $row, id, record) {
        if (record.isPublicHoliday === true || record.isPublicHoliday === "true" || record.isPublicHoliday === 1 || record.isPublicHoliday === "1") {
            $row.css('background-color', 'rgba(255, 243, 205, 0.4)'); // Light warning/yellow
        }
    });

    // Fetch states for the multiselect
    $.get("/Admin/Settings?handler=ClientStates", function (data) {
        cachedStates = data;
        let select = $('#modal_states_v2');
        select.empty();
        data.forEach(item => {
            select.append(`<option value="${item.name}">${item.name}</option>`);
        });
        // Initialize bootstrap multiselect if available
        if ($.fn.multiselect) {
            select.multiselect({
                includeSelectAllOption: true,
                buttonWidth: '100%',
                maxHeight: 200
            });
        }
    });

    // Initialize datepickers for the modal
    $('#modal_startDate_v2').datepicker({
        format: 'dd-mmm-yyyy',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome'
    });
    $('#modal_expiryDate_v2').datepicker({
        format: 'dd-mmm-yyyy',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome'
    });

    // Add New Button Click
    $('#btn_add_calendar_event_v2, #btn_add_calendar_event_v2_admin').on('click', function () {
        resetCalendarModal();
        $('#calendarEventModal_V2').modal('show');
    });

    // PH Toggle handler
    $('#modal_isPH_v2').on('change', function () {
        if ($(this).is(':checked')) {
            $('#states_container_v2').slideDown();
            // Auto append -PH to ref no if not present
            let ref = $('#modal_refNo_v2').val();
            if (ref && !ref.endsWith('-PH')) {
                $('#modal_refNo_v2').val(ref + '-PH');
            }
        } else {
            $('#states_container_v2').slideUp();
            // Remove -PH from ref no
            let ref = $('#modal_refNo_v2').val();
            if (ref && ref.endsWith('-PH')) {
                $('#modal_refNo_v2').val(ref.replace(/-PH$/, ''));
            }
        }
    });

    window.openCalendarModal = function (id, record) {
        resetCalendarModal();
        $('#modal_id_v2').val(record.id);
        $('#modal_refNo_v2').val(record.referenceNo);
        $('#modal_message_v2').val(record.textMessage);
        $('#modal_startDate_v2').val(record.formattedStartDate);
        $('#modal_expiryDate_v2').val(record.formattedExpiryDate);
        $('#modal_repeat_v2').prop('checked', record.repeatYearly === true || record.repeatYearly === "true" || record.repeatYearly === 1);
        
        let isPH = record.isPublicHoliday === true || record.isPublicHoliday === "true" || record.isPublicHoliday === 1 || record.isPublicHoliday === "1";
        $('#modal_isPH_v2').prop('checked', isPH).trigger('change');

        if (isPH && record.states) {
            let statesArr = record.states.split(',').map(s => s.trim());
            $('#modal_states_v2').val(statesArr);
            if ($.fn.multiselect) {
                $('#modal_states_v2').multiselect('refresh');
            }
        } else {
            $('#modal_states_v2').val([]);
            if ($.fn.multiselect) {
                $('#modal_states_v2').multiselect('refresh');
            }
        }

        $('#calendarEventModal_V2').modal('show');
    };

    window.saveCalendarEvent = function () {
        let id = $('#modal_id_v2').val() || -1;
        let refNo = $('#modal_refNo_v2').val();
        let message = $('#modal_message_v2').val();
        let start = $('#modal_startDate_v2').val();
        let expiry = $('#modal_expiryDate_v2').val();
        let repeat = $('#modal_repeat_v2').is(':checked');
        let isPH = $('#modal_isPH_v2').is(':checked');
        let states = $('#modal_states_v2').val();

        if (!refNo || !message || !start || !expiry) {
            $.notify("Please fill all required fields", "warning");
            return;
        }

        // Strip -PH for sending if needed (backend might want it striped or preserved, 
        // original logic stripped it in rowDataChanged but kept it in record.referenceNo)
        // Let's keep it as is in the input for now as the user explicitly asked for validations to exist.

        let data = {
            id: id,
            ReferenceNo: refNo,
            TextMessage: message,
            StartDate: start,
            ExpiryDate: expiry,
            RepeatYearly: repeat,
            IsPublicHoliday: isPH,
            States: states ? (Array.isArray(states) ? states.join(',') : states) : ""
        };

        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Admin/Settings?handler=CalendarEvents',
            type: 'POST',
            data: { record: data },
            headers: { 'RequestVerificationToken': token }
        }).done(function (res) {
            if (res.status) {
                $.notify(res.message || "Success", "success");
                $('#calendarEventModal_V2').modal('hide');
                grid_V2.reload();
            } else {
                $.notify(res.message || "Error occurred", "error");
            }
        }).fail(function () {
            $.notify("Failed to save event", "error");
        });
    };

    window.deleteCalendarEvent = function (id) {
        if (confirm("Are you sure you want to delete this calendar event?")) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=DeleteCalendarEvents',
                type: 'POST',
                data: { id: id },
                headers: { 'RequestVerificationToken': token }
            }).done(function (res) {
                if (res.status) {
                    $.notify("Event deleted successfully", "success");
                    grid_V2.reload();
                } else {
                    $.notify(res.message || "Error occurred", "error");
                }
            }).fail(function () {
                $.notify("Failed to delete event", "error");
            });
        }
    };

    function resetCalendarModal() {
        $('#modal_id_v2').val('-1');
        $('#modal_refNo_v2').val('');
        $('#modal_message_v2').val('');
        $('#modal_startDate_v2').val('');
        $('#modal_expiryDate_v2').val('');
        $('#modal_repeat_v2').prop('checked', false);
        $('#modal_isPH_v2').prop('checked', false).trigger('change');
        $('#modal_states_v2').val([]);
        if ($.fn.multiselect) {
            $('#modal_states_v2').multiselect('refresh');
        }
    }
});
