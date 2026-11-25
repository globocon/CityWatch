/* Booking > PCAR tab: patrol car (PCAR) route profiles.
   Moved from Kpi wwwroot/js/site.js ("pcar start" block, Admin/Settings > PCAR Routes) - P9 Issue 44 Step 1A.
   A profile is a route name + smart wand; its details are an ordered list of client sites, each with
   Start / End / Visits per day (Mon-Sun + Pho = public holiday). Handlers: Pages/roster/Booking.Pcar.cs. */
$(function () {
    if (!$('#pcar_routes_grid').length) return; // tab not rendered (ROEditor access)

    const PCAR_URL = '/roster/Booking?handler=';
    const PCAR_DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun', 'Pho'];

    function pcarHeaders() {
        return { 'RequestVerificationToken': $('#antiForgeryForm input[name="__RequestVerificationToken"]').val() };
    }

    function escapeHtml(text) {
        return $('<div>').text(text == null ? '' : String(text)).html();
    }

    /* ---------- Routes grid ---------- */

    function pcarActionsRenderer(value, record) {
        return '<button class="btn btn-outline-primary mr-2 edit-pcar-route" data-route-id="' + record.id + '" ' +
            'data-toggle="modal" data-target="#pcarRouteModal"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
            '<button class="btn btn-outline-danger del-pcar-route mr-2" data-route-id="' + record.id + '">' +
            '<i class="fa fa-trash mr-2"></i>Delete</button>';
    }

    const gridPcarRoutes = $('#pcar_routes_grid').grid({
        dataSource: PCAR_URL + 'PcarProfiles',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'pcarroutename', title: 'Route Name', width: 150 },
            {
                field: 'smartwandallocation', title: 'Smart Wand', width: 150,
                renderer: function (value, record) {
                    if (record.smartWandId && record.phoneNumber)
                        return escapeHtml(record.smartWandId + ' (' + record.phoneNumber + ')');
                    return value;
                }
            },
            {
                field: 'sites', title: 'Sites', width: 300,
                renderer: function (value) {
                    if (!value) return '';
                    return value.split(',')
                        .map(s => '<span class="badge badge-info mr-1">' + escapeHtml(s.trim()) + '</span>')
                        .join(' ');
                }
            },
            { width: 150, renderer: pcarActionsRenderer }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    /* ---------- Modal state ---------- */

    // Route details are only editable once the profile has been created (route id > 0).
    function togglePcarDetailsSection() {
        const id = parseInt($('#pcarRouteId').val()) || 0;
        const $section = $('#pcarRouteDetailsSection');
        const $btnProfile = $('#btnCreatePcarProfile');

        if (id > 0) {
            $section.css({ 'pointer-events': 'auto', 'opacity': '1' });
            $btnProfile.html('<i class="fa fa-pencil mr-1"></i>Update Profile').removeClass('btn-success').addClass('btn-warning');
        } else {
            $section.css({ 'pointer-events': 'none', 'opacity': '0.4' });
            $btnProfile.html('<i class="fa fa-plus mr-1"></i>Create Profile').removeClass('btn-warning').addClass('btn-success');
        }
    }

    // Bootstrap opens the modal before the Add / Edit click handlers below run, so this reset comes first.
    $('#pcarRouteModal').on('show.bs.modal', function (e) {
        if (e.target !== this) return; // ignore the collapse inside the modal
        $(this).find('input, select').val('');
        $('#pcarRouteId').val(0);
        $('#pcarClientSite').html('');
        $('#pcarSitesContainer').empty();
        togglePcarDetailsSection();
    });

    $('#pcarRouteModal').on('hidden.bs.modal', function (e) {
        if (e.target !== this) return;
        gridPcarRoutes.reload();
    });

    $('#btnAddPcarRoute').on('click', function () {
        $('#pcarRouteId').val(0);
        togglePcarDetailsSection();
    });

    $(document).on('click', '.edit-pcar-route', function () {
        const routeId = $(this).data('route-id');
        $('#pcarRouteId').val(routeId);
        togglePcarDetailsSection();
        loadPcarRoute(routeId);
    });

    function loadPcarRoute(routeId) {
        $('#loader').show();
        $('#pcarSitesContainer').empty();

        $.ajax({
            url: PCAR_URL + 'PcarRouteDetails&id=' + encodeURIComponent(routeId),
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            if (!data) return;
            const routeDetails = data.routeDetails || [];

            $('#pcarRouteName').val(data.pcarroutename);
            $('#pcarSmartWandAllocation').val(data.smartwandallocation);

            $.each(routeDetails, function (index, rd) {
                $('#pcarSitesContainer').append(createPcarSiteBlock(rd.clientSiteId, rd.clientSiteName, rd, index + 1));
            });

            // Pre-select the first site's client type, so its sites are ready in the Client Site dropdown.
            if (routeDetails.length > 0) {
                const firstTypeName = routeDetails[0].clientTypeName;
                $('#pcarClientType option').filter(function () {
                    return firstTypeName && $(this).val() === firstTypeName;
                }).prop('selected', true).trigger('change');
            }
        }).always(function () {
            $('#loader').hide();
        });
    }

    /* ---------- Profile (name + smart wand) ---------- */

    $('#btnCreatePcarProfile').on('click', function () {
        $.ajax({
            url: PCAR_URL + 'PcarCreateProfile',
            type: 'POST',
            data: {
                routeId: parseInt($('#pcarRouteId').val()) || 0,
                pcarroutename: $('#pcarRouteName').val(),
                smartwandallocation: parseInt($('#pcarSmartWandAllocation').val()) || 0
            },
            headers: pcarHeaders()
        }).done(function (data) {
            if (data.success) {
                const isNew = !(parseInt($('#pcarRouteId').val()) > 0);
                $('#pcarRouteId').val(data.routeId);
                togglePcarDetailsSection();
                alert(isNew ? 'Profile Created Successfully' : 'Profile Updated Successfully');
            } else {
                alert(data.message);
            }
        });
    });

    $(document).on('click', '.del-pcar-route', function () {
        const routeId = $(this).data('route-id');
        if (!routeId) {
            alert('Invalid profile ID.');
            return;
        }
        if (!confirm('Are you sure you want to delete this PCAR Profile and Routes? This action cannot be undone.')) return;

        $.ajax({
            url: PCAR_URL + 'PcarDeleteProfile',
            type: 'POST',
            data: { routeId: routeId },
            headers: pcarHeaders()
        }).done(function (data) {
            alert(data.message || (data.success ? 'Profile deleted successfully.' : 'Failed to delete profile.'));
            if (data.success) gridPcarRoutes.reload();
        });
    });

    /* ---------- Route details (sites + day schedules) ---------- */

    $('#pcarClientType').on('change', function () {
        const type = $(this).val();
        const $sites = $('#pcarClientSite');
        $sites.html('<option value="">Select</option>');
        if (!type) return;

        $.ajax({
            url: PCAR_URL + 'PcarClientSites&type=' + encodeURIComponent(type),
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            $.each(data || [], function (i, site) {
                $sites.append($('<option>').val(site.value).text(site.text));
            });
        });
    });

    function createPcarSiteBlock(siteId, siteName, scheduleData, orderNo) {
        const orderStr = orderNo < 10 ? '0' + orderNo : orderNo;
        const valueOf = key => escapeHtml(scheduleData[key] || '');

        return `
            <div class="card mb-3 pcar-site-block border-primary" data-site-id="${escapeHtml(siteId)}">
                <div class="card-header p-2 d-flex justify-content-between align-items-center bg-light border-bottom border-primary">
                    <h6 class="mb-0 text-primary"><strong>List <span class="order-no-label">${orderStr}</span> - <span class="site-name-label">${escapeHtml(siteName)}</span></strong></h6>
                    <div class="d-flex align-items-center">
                        <button type="button" class="btn btn-xs btn-outline-secondary move-up-pcar-site mr-1" title="Move Up"><i class="fa fa-arrow-up"></i></button>
                        <button type="button" class="btn btn-xs btn-outline-secondary move-down-pcar-site mr-1" title="Move Down"><i class="fa fa-arrow-down"></i></button>
                        <button type="button" class="btn btn-sm btn-outline-danger remove-pcar-site" title="Remove Site"><i class="fa fa-trash-o mr-1"></i>Remove</button>
                    </div>
                </div>
                <div class="card-body p-2">
                    <input type="hidden" class="pcar-site-id" value="${escapeHtml(siteId)}">
                    <input type="hidden" class="pcar-order-no" value="${orderNo}">
                    <div class="table-responsive">
                        <table class="table table-bordered table-sm text-center mb-0">
                            <thead class="bg-light small">
                                <tr><th></th>${PCAR_DAYS.map(d => `<th>${d}</th>`).join('')}</tr>
                            </thead>
                            <tbody>
                                <tr class="start-times">
                                    <td class="small"><strong>Start</strong></td>
                                    ${PCAR_DAYS.map(d => `<td><input type="time" class="form-control form-control-sm p-1" name="Start${d}" value="${valueOf('start' + d)}"></td>`).join('')}
                                </tr>
                                <tr class="end-times">
                                    <td class="small"><strong>End</strong></td>
                                    ${PCAR_DAYS.map(d => `<td><input type="time" class="form-control form-control-sm p-1" name="End${d}" value="${valueOf('end' + d)}"></td>`).join('')}
                                </tr>
                                <tr class="visit-counts">
                                    <td class="small"><strong>Visits</strong></td>
                                    ${PCAR_DAYS.map(d => `<td><input type="number" class="form-control form-control-sm p-1" name="Visit${d}" value="${escapeHtml(scheduleData['visit' + d] || '0')}" min="0"></td>`).join('')}
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>`;
    }

    function reorderPcarSites() {
        $('#pcarSitesContainer .pcar-site-block').each(function (index) {
            const orderNo = index + 1;
            $(this).find('.order-no-label').text(orderNo < 10 ? '0' + orderNo : orderNo);
            $(this).find('.pcar-order-no').val(orderNo);
            $(this).attr('data-order', orderNo);
        });
    }

    $('#btnAddSiteToPcarRoute').on('click', function () {
        const siteId = $('#pcarClientSite').val();
        const siteName = $('#pcarClientSite option:selected').text();
        if (!siteId) {
            alert('Please select a client site first.');
            return;
        }

        const orderNo = $('#pcarSitesContainer .pcar-site-block').length + 1;
        $('#pcarSitesContainer').append(createPcarSiteBlock(siteId, siteName, {}, orderNo));

        const container = document.getElementById('pcarSitesContainer');
        container.scrollTop = container.scrollHeight;
    });

    $(document).on('click', '.remove-pcar-site', function () {
        if (!confirm('Remove this site package from the route?')) return;
        $(this).closest('.pcar-site-block').remove();
        reorderPcarSites();
        savePcarRouteDetails(false);
    });

    $(document).on('click', '.move-up-pcar-site', function () {
        const block = $(this).closest('.pcar-site-block');
        block.prev().before(block);
        reorderPcarSites();
        savePcarRouteDetails(false);
    });

    $(document).on('click', '.move-down-pcar-site', function () {
        const block = $(this).closest('.pcar-site-block');
        block.next().after(block);
        reorderPcarSites();
        savePcarRouteDetails(false);
    });

    $('#btnSavePcarRoute').on('click', function () {
        savePcarRouteDetails(true);
    });

    // A day is either empty or fully filled (Start, End, Visits > 0, End after Start); each site needs one filled day.
    function savePcarRouteDetails(showSuccessAlert) {
        const routeId = parseInt($('#pcarRouteId').val()) || 0;
        if (routeId <= 0) {
            alert('Please create a profile first!');
            return;
        }

        const siteSchedules = [];
        let validationMsg = '';

        $('#pcarSitesContainer .pcar-site-block').each(function (index) {
            const block = $(this);
            const siteName = block.find('.site-name-label').text();
            const schedule = {
                ClientSiteId: parseInt(block.find('.pcar-site-id').val()),
                OrderNo: index + 1
            };
            let anyDayFilled = false;

            for (const d of PCAR_DAYS) {
                const start = block.find(`input[name="Start${d}"]`).val();
                const end = block.find(`input[name="End${d}"]`).val();
                const visits = parseInt(block.find(`input[name="Visit${d}"]`).val()) || 0;

                schedule[`Start${d}`] = start;
                schedule[`End${d}`] = end;
                schedule[`Visit${d}`] = visits;

                const fullyFilled = !!start && !!end && visits > 0;
                const partiallyFilled = !!start || !!end || visits > 0;

                if (partiallyFilled && !fullyFilled) {
                    validationMsg = `Site ${siteName}: Start, End, and Visits (>0) must be filled for ${d}.`;
                    return false;
                }
                if (fullyFilled) {
                    anyDayFilled = true;
                    if (start >= end) {
                        validationMsg = `Site ${siteName}: End Time must be greater than Start Time for ${d}.`;
                        return false;
                    }
                }
            }

            if (!anyDayFilled) {
                validationMsg = `Site ${siteName}: Please fill at least one day completely.`;
                return false;
            }
            siteSchedules.push(schedule);
        });

        if (validationMsg) {
            alert(validationMsg);
            return;
        }

        $.ajax({
            url: PCAR_URL + 'PcarSaveRouteDetails',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ PcarRouteId: routeId, SiteSchedules: siteSchedules }),
            headers: pcarHeaders()
        }).done(function (data) {
            if (data.success) {
                if (showSuccessAlert) alert('All Schedule Details Saved Successfully');
            } else {
                alert(data.message);
            }
        });
    }
});
