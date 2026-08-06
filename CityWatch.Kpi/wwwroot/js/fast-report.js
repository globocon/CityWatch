/*
 * fast-report.js
 * ---------------------------------------------------------------------------
 * Client for the "Download Now" button (#btnScheduleDownloadFast), which is now
 * the default download path on the Run Schedule popup.
 *
 * Still additive: this file registers one click handler on its own button and
 * talks only to /api/FastKpiReport/*. The legacy #btnScheduleDownload button is
 * hidden in the markup but left in place, and its site.js handler is untouched.
 *
 * The overlay is built with plain DOM and inline styles rather than a Bootstrap
 * modal, so it cannot be affected by - or affect - the Bootstrap 4 modal that
 * hosts the schedule popup.
 */
(function () {
    'use strict';

    var API = '/api/FastKpiReport';
    var POLL_INTERVAL_MS = 900;

    var state = {
        jobId: null,
        polling: false,
        timer: null,
        lastPercent: 0,
        request: null
    };

    // -----------------------------------------------------------------------
    // Overlay
    // -----------------------------------------------------------------------

    function buildOverlay() {
        if (document.getElementById('fastReportOverlay')) {
            return document.getElementById('fastReportOverlay');
        }

        var overlay = document.createElement('div');
        overlay.id = 'fastReportOverlay';
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        overlay.setAttribute('aria-labelledby', 'fastReportTitle');
        overlay.style.cssText = [
            'position:fixed', 'inset:0', 'z-index:20000',
            'background:rgba(15,23,42,.55)',
            'display:none', 'align-items:center', 'justify-content:center',
            'padding:16px', 'font-family:inherit'
        ].join(';');

        overlay.innerHTML = [
            '<div style="background:#fff;border-radius:10px;max-width:560px;width:100%;',
            'box-shadow:0 20px 45px rgba(0,0,0,.3);overflow:hidden">',
            '  <div style="padding:18px 22px;border-bottom:1px solid #e5e7eb;display:flex;align-items:center;justify-content:space-between">',
            '    <h5 id="fastReportTitle" style="margin:0;font-size:1.05rem;font-weight:600">Generating Report</h5>',
            '  </div>',
            '  <div style="padding:22px">',
            '    <div id="fastReportStage" style="font-weight:600;margin-bottom:10px">Preparing report</div>',
            '    <div style="background:#e5e7eb;border-radius:999px;height:12px;overflow:hidden">',
            '      <div id="fastReportBar" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow="0"',
            '        style="height:100%;width:0%;background:linear-gradient(90deg,#2563eb,#3b82f6);',
            '        transition:width .35s ease"></div>',
            '    </div>',
            '    <div style="display:flex;justify-content:space-between;margin-top:7px;font-size:.8rem;color:#6b7280">',
            '      <span id="fastReportPercent">0%</span>',
            '      <span id="fastReportSites"></span>',
            '    </div>',
            '    <div style="margin-top:16px;font-size:.85rem;color:#374151;min-height:2.4em" id="fastReportStep">Starting...</div>',
            '    <div style="display:flex;gap:26px;margin-top:14px;font-size:.8rem;color:#6b7280">',
            '      <div><div style="text-transform:uppercase;letter-spacing:.03em;font-size:.68rem">Elapsed</div>',
            '        <div id="fastReportElapsed" style="color:#111827;font-weight:600">0s</div></div>',
            '      <div><div style="text-transform:uppercase;letter-spacing:.03em;font-size:.68rem">Est. remaining</div>',
            '        <div id="fastReportEta" style="color:#111827;font-weight:600">calculating...</div></div>',
            '    </div>',
            '    <div id="fastReportAlert" style="display:none;margin-top:16px;padding:12px 14px;border-radius:7px;font-size:.85rem"></div>',
            '    <pre id="fastReportDetail" style="display:none;margin-top:12px;max-height:190px;overflow:auto;',
            '      background:#0f172a;color:#e2e8f0;padding:12px;border-radius:7px;font-size:.72rem;white-space:pre-wrap"></pre>',
            '  </div>',
            '  <div style="padding:14px 22px;border-top:1px solid #e5e7eb;display:flex;gap:8px;justify-content:flex-end">',
            '    <button type="button" id="fastReportDetailBtn" class="btn btn-link btn-sm" style="display:none">View log</button>',
            '    <button type="button" id="fastReportRetryBtn" class="btn btn-primary btn-sm" style="display:none">Retry</button>',
            '    <button type="button" id="fastReportCancelBtn" class="btn btn-outline-secondary btn-sm">Cancel</button>',
            '    <button type="button" id="fastReportCloseBtn" class="btn btn-secondary btn-sm" style="display:none">Close</button>',
            '  </div>',
            '</div>'
        ].join('');

        document.body.appendChild(overlay);

        document.getElementById('fastReportCancelBtn').addEventListener('click', cancelJob);
        document.getElementById('fastReportCloseBtn').addEventListener('click', hideOverlay);
        document.getElementById('fastReportRetryBtn').addEventListener('click', retryJob);
        document.getElementById('fastReportDetailBtn').addEventListener('click', toggleDetail);

        return overlay;
    }

    function showOverlay() {
        var overlay = buildOverlay();
        overlay.style.display = 'flex';
        state.lastPercent = 0;
        setAlert(null);
        el('fastReportDetail').style.display = 'none';
        el('fastReportDetail').textContent = '';
        el('fastReportDetailBtn').style.display = 'none';
        el('fastReportRetryBtn').style.display = 'none';
        el('fastReportCloseBtn').style.display = 'none';
        el('fastReportCancelBtn').style.display = '';
        setBar(0);
        el('fastReportStage').textContent = 'Preparing report';
        el('fastReportStep').textContent = 'Starting...';
        el('fastReportElapsed').textContent = '0s';
        el('fastReportEta').textContent = 'calculating...';
        el('fastReportSites').textContent = '';
    }

    function hideOverlay() {
        stopPolling();
        var overlay = document.getElementById('fastReportOverlay');
        if (overlay) overlay.style.display = 'none';
        enableButton();
    }

    function el(id) { return document.getElementById(id); }

    function setBar(percent) {
        // Never let the bar move backwards - it reads as a bug even when it isn't.
        var value = Math.max(state.lastPercent, Math.min(100, percent || 0));
        state.lastPercent = value;
        var bar = el('fastReportBar');
        bar.style.width = value + '%';
        bar.setAttribute('aria-valuenow', value);
        el('fastReportPercent').textContent = value + '%';
    }

    function setAlert(kind, message) {
        var box = el('fastReportAlert');
        if (!kind) { box.style.display = 'none'; return; }

        var palette = {
            success: ['#ecfdf5', '#065f46', '#a7f3d0'],
            error: ['#fef2f2', '#991b1b', '#fecaca'],
            info: ['#eff6ff', '#1e40af', '#bfdbfe']
        }[kind] || ['#f3f4f6', '#374151', '#e5e7eb'];

        box.style.display = 'block';
        box.style.background = palette[0];
        box.style.color = palette[1];
        box.style.border = '1px solid ' + palette[2];
        box.innerHTML = message;
    }

    function formatSeconds(seconds) {
        if (seconds === null || typeof seconds === 'undefined') return '--';
        var total = Math.max(0, Math.round(seconds));
        if (total < 60) return total + 's';
        var minutes = Math.floor(total / 60);
        return minutes + 'm ' + (total % 60) + 's';
    }

    function toggleDetail() {
        var pre = el('fastReportDetail');
        pre.style.display = pre.style.display === 'none' ? 'block' : 'none';
    }

    // -----------------------------------------------------------------------
    // Job lifecycle
    // -----------------------------------------------------------------------

    function readRequest() {
        return {
            ScheduleId: $('#sch-id').val(),
            ReportYear: $('#schRunYear').val(),
            ReportMonth: $('#schRunMonth').val(),
            IgnoreRecipients: $('#cbIgnoreRecipients').is(':checked')
        };
    }

    function disableButton() { $('#btnScheduleDownloadFast').prop('disabled', true); }
    function enableButton() { $('#btnScheduleDownloadFast').prop('disabled', false); }

    function startJob() {
        state.request = readRequest();

        if (!state.request.ScheduleId) {
            window.alert('Please select a schedule first.');
            return;
        }

        disableButton();
        showOverlay();

        $.ajax({
            type: 'POST',
            url: API + '/start',
            data: state.request
        }).done(function (response) {
            if (!response || !response.success) {
                fail((response && response.message) || 'The report could not be queued.');
                return;
            }
            state.jobId = response.jobId;
            startPolling();
        }).fail(function (xhr) {
            fail(readError(xhr, 'The report could not be queued.'));
        });
    }

    function retryJob() {
        // The request is preserved on the client, so a retry needs no re-entry.
        startJob();
    }

    function startPolling() {
        state.polling = true;
        poll();
    }

    function stopPolling() {
        state.polling = false;
        if (state.timer) {
            clearTimeout(state.timer);
            state.timer = null;
        }
    }

    function poll() {
        if (!state.polling || !state.jobId) return;

        $.ajax({
            type: 'GET',
            url: API + '/progress/' + encodeURIComponent(state.jobId),
            cache: false
        }).done(function (progress) {
            render(progress);

            if (progress.isTerminal) {
                stopPolling();
                finish(progress);
            } else {
                state.timer = setTimeout(poll, POLL_INTERVAL_MS);
            }
        }).fail(function (xhr) {
            stopPolling();
            fail(readError(xhr, 'Lost contact with the report job.'));
        });
    }

    function render(progress) {
        setBar(progress.percentComplete);
        el('fastReportStage').textContent = progress.stageLabel || progress.stage;
        el('fastReportStep').textContent = progress.currentStep || '';
        el('fastReportElapsed').textContent = formatSeconds(progress.elapsedSeconds);
        el('fastReportEta').textContent = progress.estimatedRemainingSeconds === null
            ? 'calculating...'
            : formatSeconds(progress.estimatedRemainingSeconds);

        el('fastReportSites').textContent = progress.sitesTotal > 0
            ? ('Site ' + progress.sitesCompleted + ' of ' + progress.sitesTotal)
            : '';
    }

    function finish(progress) {
        el('fastReportCancelBtn').style.display = 'none';
        el('fastReportCloseBtn').style.display = '';

        if (progress.status === 'Completed') {
            setBar(100);
            el('fastReportStage').textContent = 'Completed';
            el('fastReportStep').textContent = 'Your download should begin automatically.';
            el('fastReportEta').textContent = '0s';

            var metrics = progress.metrics || {};
            setAlert('success',
                '<strong>Report ready.</strong> Generated in ' + formatSeconds(progress.elapsedSeconds) +
                (metrics.outputPageCount ? ' &middot; ' + metrics.outputPageCount + ' pages' : '') +
                (metrics.cacheHits ? ' &middot; ' + metrics.cacheHits + ' duplicate queries avoided' : ''));

            window.location.href = API + '/download/' + encodeURIComponent(progress.jobId);
            enableButton();
            return;
        }

        if (progress.status === 'Cancelled') {
            setAlert('info', 'Report generation was cancelled.');
            el('fastReportRetryBtn').style.display = '';
            enableButton();
            return;
        }

        fail(progress.errorMessage || 'The report failed to generate.', progress.jobId);
    }

    function fail(message, jobId) {
        el('fastReportCancelBtn').style.display = 'none';
        el('fastReportCloseBtn').style.display = '';
        el('fastReportRetryBtn').style.display = '';
        el('fastReportStage').textContent = 'Failed';
        el('fastReportStep').textContent = '';
        setAlert('error', '<strong>Report failed.</strong> ' + escapeHtml(message));
        enableButton();

        var id = jobId || state.jobId;
        if (!id) return;

        // Pull the server-side log so the user can hand something useful to support.
        $.ajax({ type: 'GET', url: API + '/log/' + encodeURIComponent(id), cache: false })
            .done(function (response) {
                if (!response || !response.success) return;
                var lines = (response.log || []).join('\n');
                if (response.detail) lines += '\n\n' + response.detail;
                el('fastReportDetail').textContent = lines;
                el('fastReportDetailBtn').style.display = '';
            });
    }

    function cancelJob() {
        if (!state.jobId) { hideOverlay(); return; }

        el('fastReportStep').textContent = 'Cancelling...';
        $.ajax({ type: 'POST', url: API + '/cancel/' + encodeURIComponent(state.jobId) })
            .always(function () { /* the poll loop reports the final state */ });
    }

    function readError(xhr, fallback) {
        try {
            var parsed = JSON.parse(xhr.responseText);
            if (parsed && parsed.message) return parsed.message;
        } catch (e) { /* not JSON */ }
        return fallback;
    }

    function escapeHtml(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    // -----------------------------------------------------------------------
    // Wiring
    // -----------------------------------------------------------------------

    $(function () {
        // Delegated so it works regardless of when the schedule popup is rendered.
        $(document).on('click', '#btnScheduleDownloadFast', function (event) {
            event.preventDefault();
            startJob();
        });
    });
})();
