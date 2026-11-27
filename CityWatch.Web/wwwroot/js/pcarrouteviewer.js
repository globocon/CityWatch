/* RoosterHub > PCAR ROUTE: read-only PCAR Routes view for guards (P9 Issue 44 Step 2).
   1. Guard signs in with Security License No + HR PIN; the server allows only PCAR or ADMIN - ROSTER access.
   2. The viewer lists the routes; choosing a PCAR Route Name shows its smart wand and its sites'
      Start / End / Visits per day. Nothing else can be selected or edited.
   Handlers: Pages/Admin/Roster.PcarRoutes.cs. */
$(function () {
    if (!$('#mdlPcarRouteViewer').length) return;

    const URL = '/RoosterHub?handler=';
    const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun', 'Pho'];
    let routes = [];

    function token() {
        return $('input[name="__RequestVerificationToken"]').first().val();
    }

    function escapeHtml(text) {
        return $('<div>').text(text == null ? '' : String(text)).html();
    }

    /* ---------- Sign-in ---------- */

    $('#mdlAuthPcarRouteAccess').on('show.bs.modal', function () {
        $('#PcarRouteAuth_LicenseNo, #PcarRouteAuth_PIN').val('');
        $('#PcarRouteAccessValidationSummary').html('');
    }).on('shown.bs.modal', function () {
        $('#PcarRouteAuth_LicenseNo').trigger('focus');
    });

    $('#frmPcarRouteAccess').on('submit', function () {
        const $btn = $('#btnPcarRouteAccessConfirm').prop('disabled', true);
        $('#PcarRouteAccessValidationSummary').html('');

        $.ajax({
            url: URL + 'VerifyPcarRouteAccess',
            type: 'POST',
            data: { licenseNo: $('#PcarRouteAuth_LicenseNo').val(), pin: $('#PcarRouteAuth_PIN').val() },
            headers: { 'RequestVerificationToken': token() }
        }).done(function (result) {
            if (result.success) {
                $('#mdlAuthPcarRouteAccess').modal('hide');
                openViewer();
            } else {
                $('#PcarRouteAccessValidationSummary').text(result.message || 'Access denied.');
            }
        }).fail(function () {
            $('#PcarRouteAccessValidationSummary').text('Could not verify access. Please try again.');
        }).always(function () {
            $btn.prop('disabled', false);
        });
        return false;
    });

    /* ---------- Viewer ---------- */

    function showMessage(text) {
        $('#pcarViewerSites').html('<p class="text-muted small mb-0">' + escapeHtml(text) + '</p>');
    }

    function openViewer() {
        $('#pcarViewerRoute').html('<option value="">Loading...</option>');
        $('#pcarViewerSmartWand').val('');
        showMessage('Select a PCAR Route Name to see its sites.');
        $('#mdlPcarRouteViewer').modal('show');

        $.ajax({ url: URL + 'PcarRouteViewerRoutes', type: 'GET', dataType: 'json' })
            .done(function (data) {
                routes = data || [];
                const $select = $('#pcarViewerRoute').empty().append($('<option>').val('').text('Select'));
                routes.forEach(r => $select.append($('<option>').val(r.id).text(r.name)));
                if (!routes.length) showMessage('No PCAR routes have been set up.');
            })
            .fail(function () {
                $('#pcarViewerRoute').html('<option value="">Select</option>');
                showMessage('Could not load the PCAR routes. Please close and sign in again.');
            });
    }

    $('#pcarViewerRoute').on('change', function () {
        const routeId = $(this).val();
        const route = routes.find(r => String(r.id) === routeId);
        $('#pcarViewerSmartWand').val(route ? route.smartWand : '');

        if (!routeId) {
            showMessage('Select a PCAR Route Name to see its sites.');
            return;
        }

        showMessage('Loading...');
        $.ajax({ url: URL + 'PcarRouteViewerDetails&id=' + encodeURIComponent(routeId), type: 'GET', dataType: 'json' })
            .done(function (data) {
                if ($('#pcarViewerRoute').val() !== routeId) return; // another route was chosen meanwhile
                const details = (data && data.routeDetails) || [];
                if (!details.length) {
                    showMessage('This route has no sites.');
                    return;
                }
                $('#pcarViewerSites').html(details.map((rd, i) => siteBlock(rd, i + 1)).join(''));
            })
            .fail(function () {
                showMessage('Could not load the route. Please close and sign in again.');
            });
    });

    // Same layout as a site block in Booking > PCAR > Manage PCAR Routes, with every field disabled and no buttons.
    function siteBlock(rd, orderNo) {
        const orderStr = orderNo < 10 ? '0' + orderNo : orderNo;
        const cell = (type, value) =>
            `<td><input type="${type}" class="form-control form-control-sm p-1" value="${escapeHtml(value)}" disabled></td>`;

        return `
            <div class="card mb-3 border-primary">
                <div class="card-header p-2 bg-light border-bottom border-primary">
                    <h6 class="mb-0 text-primary"><strong>List ${orderStr} - ${escapeHtml(rd.clientSiteName)}</strong></h6>
                </div>
                <div class="card-body p-2">
                    <div class="table-responsive">
                        <table class="table table-bordered table-sm text-center mb-0">
                            <thead class="bg-light small">
                                <tr><th></th>${DAYS.map(d => `<th>${d}</th>`).join('')}</tr>
                            </thead>
                            <tbody>
                                <tr><td class="small"><strong>Start</strong></td>${DAYS.map(d => cell('time', rd['start' + d] || '')).join('')}</tr>
                                <tr><td class="small"><strong>End</strong></td>${DAYS.map(d => cell('time', rd['end' + d] || '')).join('')}</tr>
                                <tr><td class="small"><strong>Visits</strong></td>${DAYS.map(d => cell('number', rd['visit' + d] || 0)).join('')}</tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>`;
    }
});
