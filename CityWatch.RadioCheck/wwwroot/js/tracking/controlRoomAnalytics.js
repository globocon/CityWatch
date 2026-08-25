/* CityWatch Insights — the isolated analytics drawer (plan A1).
   A SEPARATE module by contract: it reads only /api/analytics/*, renders only its own
   ana-* DOM, and never calls into controlRoomMap.js or controlRoomTracking.js. If this
   file throws, or the analytics API is down, the drawer says "Analytics temporarily
   unavailable." and the map, live tracking, SignalR and replay continue untouched.
   No polling while closed — the drawer costs nothing until an operator opens it.

   The manager's reading order (approved plan §02): what is happening (KPI numbers) →
   is it normal (compare deltas vs the previous equivalent period) → then, in later
   phases, where is the problem (charts, A2) and who is behind it (drill-downs, A3). */
(function () {
    'use strict';

    var REFRESH_MS = 60000;
    var isOpen = false, seq = 0, timer = null;
    var winKind = 'today';

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    /* ---- windows: the chips' meaning, and what "previous" means for each ----
       The compare note is worded per window because "vs yesterday" would be a lie on
       the 7-day view; the server shifts by whole days so 09:00 compares with 09:00. */
    function presetWindow(kind) {
        var now = new Date();
        if (kind === 'today') {
            var f = new Date(now); f.setHours(0, 0, 0, 0);
            return { fromUtc: f, toUtc: now, note: 'vs the same hours yesterday' };
        }
        if (kind === 'yesterday') {
            var t = new Date(now); t.setHours(0, 0, 0, 0);
            return { fromUtc: new Date(t.getTime() - 864e5), toUtc: t, note: 'vs the day before' };
        }
        /* 7d */
        return { fromUtc: new Date(now.getTime() - 7 * 864e5), toUtc: now, note: 'vs the previous 7 days' };
    }

    function customWindow() {
        var d = document.getElementById('anaDate').value;
        var f = document.getElementById('anaFrom').value || '00:00';
        var t = document.getElementById('anaTo').value || '23:59';
        if (!d) return null;
        var fromUtc = new Date(d + 'T' + f);
        var toUtc = new Date(d + 'T' + t);
        if (!(toUtc > fromUtc)) return null;
        return { fromUtc: fromUtc, toUtc: toUtc, note: 'vs the same hours the day before' };
    }

    function currentWindow() {
        return winKind === 'custom' ? customWindow() : presetWindow(winKind);
    }

    /* ---- DOM ---- */

    function drawerEl() {
        var el = document.getElementById('anaDrawer');
        if (el) return el;
        el = document.createElement('aside');
        el.id = 'anaDrawer';
        el.className = 'ana-drawer';
        el.setAttribute('tabindex', '-1');
        el.setAttribute('aria-label', 'Insights');
        var today = new Date().toISOString().slice(0, 10);
        el.innerHTML =
            '<div class="ana-head">' +
              '<b>📊 INSIGHTS</b>' +
              '<button class="ana-x" data-ana-close="1" aria-label="Close">×</button>' +
            '</div>' +
            '<div class="ana-chips">' +
              '<button data-ana-win="today" class="on">Today</button>' +
              '<button data-ana-win="yesterday">Yesterday</button>' +
              '<button data-ana-win="7d">7 days</button>' +
              '<button data-ana-win="custom">Custom</button>' +
            '</div>' +
            '<div class="ana-custom" id="anaCustom" style="display:none">' +
              '<input type="date" id="anaDate" value="' + today + '" max="' + today + '" aria-label="Date">' +
              '<input type="time" id="anaFrom" value="00:00" aria-label="From">' +
              '<span>→</span>' +
              '<input type="time" id="anaTo" value="23:59" aria-label="To">' +
              '<button id="anaLoad">Load</button>' +
            '</div>' +
            '<div class="ana-body" id="anaBody"></div>' +
            '<div class="ana-foot">' +
              '<span class="ana-note" id="anaNote"></span>' +
              '<span class="ana-upd" id="anaUpd"></span>' +
              '<button id="anaRefresh" title="Refresh now" aria-label="Refresh">⟳</button>' +
            '</div>';
        document.body.appendChild(el);
        el.addEventListener('click', onDrawerClick);
        el.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeDrawer(); });
        return el;
    }

    function onDrawerClick(ev) {
        if (ev.target.closest('[data-ana-close]')) { closeDrawer(); return; }
        var chip = ev.target.closest('[data-ana-win]');
        if (chip) {
            winKind = chip.getAttribute('data-ana-win');
            document.querySelectorAll('#anaDrawer [data-ana-win]').forEach(function (b) {
                b.classList.toggle('on', b === chip);
            });
            document.getElementById('anaCustom').style.display = winKind === 'custom' ? '' : 'none';
            if (winKind !== 'custom') load();
            return;
        }
        if (ev.target.id === 'anaLoad' || ev.target.id === 'anaRefresh' || ev.target.id === 'anaRetry') load();
    }

    /* ---- render states ---- */

    function renderLoading() {
        var tiles = '';
        for (var i = 0; i < 6; i++)
            tiles += '<div class="ana-tile ana-skel"><span class="v">—</span><span class="k">loading</span></div>';
        document.getElementById('anaBody').innerHTML = '<div class="ana-grid">' + tiles + '</div>';
    }

    function renderError() {
        document.getElementById('anaBody').innerHTML =
            '<div class="ana-err">' +
              '<b>Analytics temporarily unavailable.</b>' +
              '<span>The map, live tracking and replay are unaffected.</span>' +
              '<button id="anaRetry">Try again</button>' +
            '</div>';
        document.getElementById('anaNote').textContent = '';
        document.getElementById('anaUpd').textContent = '';
    }

    function fmtHours(min) {
        min = Math.max(0, Math.round(min));
        return Math.floor(min / 60) + 'h ' + (min % 60) + 'm';
    }

    /* Deltas always carry the glyph with the colour, so print/colour-blind cases read
       identically. prev=0 → "new" (a percentage against nothing is noise). */
    function deltaHtml(cur, prev) {
        if (prev === 0) return cur > 0
            ? '<span class="ana-delta up">▲ new</span>'
            : '<span class="ana-delta flat">—</span>';
        var pct = Math.round((cur - prev) / prev * 100);
        if (pct === 0) return '<span class="ana-delta flat">＝ 0%</span>';
        return pct > 0
            ? '<span class="ana-delta up">▲ +' + pct + '%</span>'
            : '<span class="ana-delta dn">▼ ' + pct + '%</span>';
    }

    function tile(glyph, value, deltaMarkup, label) {
        return '<div class="ana-tile">' +
                 '<span class="g">' + glyph + '</span>' +
                 '<span class="v">' + esc(value) + '</span>' +
                 deltaMarkup +
                 '<span class="k">' + esc(label) + '</span>' +
               '</div>';
    }

    function renderKpis(data, note) {
        var c = data.current, p = data.previous;
        document.getElementById('anaBody').innerHTML = '<div class="ana-grid">' +
            tile('👮', c.guardsActive, deltaHtml(c.guardsActive, p.guardsActive), 'Guards active') +
            tile('🚓', c.pcarsActive, deltaHtml(c.pcarsActive, p.pcarsActive), 'Patrol cars') +
            tile('🏢', c.sitesActive, deltaHtml(c.sitesActive, p.sitesActive), 'Sites active') +
            tile('📍', c.siteVisits, deltaHtml(c.siteVisits, p.siteVisits), 'Site visits') +
            tile('✓', c.checkIns, deltaHtml(c.checkIns, p.checkIns), 'Check-ins · NFC') +
            tile('⏱', fmtHours(c.activeMinutes), deltaHtml(c.activeMinutes, p.activeMinutes), 'Hours on duty') +
            '</div>';
        document.getElementById('anaNote').textContent = note;
        document.getElementById('anaUpd').textContent =
            'updated ' + new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    /* ---- data ---- */

    async function load() {
        var w = currentWindow();
        if (!w) return;
        var mySeq = ++seq;
        renderLoading();
        try {
            var res = await fetch('/api/analytics/summary?fromUtc=' + w.fromUtc.toISOString() +
                '&toUtc=' + w.toUtc.toISOString(), { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            var body = await res.json();
            if (mySeq !== seq || !isOpen) return;      // superseded, or closed meanwhile
            renderKpis(body, w.note);
        } catch (e) {
            if (mySeq !== seq || !isOpen) return;
            renderError();
        }
    }

    /* ---- open/close ---- */

    function openDrawer() {
        var el = drawerEl();
        isOpen = true;
        el.classList.add('open');
        el.focus({ preventScroll: true });
        load();
        clearInterval(timer);
        timer = setInterval(function () { if (isOpen) load(); }, REFRESH_MS);
    }

    function closeDrawer() {
        isOpen = false;
        clearInterval(timer);
        timer = null;
        var el = document.getElementById('anaDrawer');
        if (el) el.classList.remove('open');
    }

    function toggleDrawer() { isOpen ? closeDrawer() : openDrawer(); }

    /* ---- entry point: one button, injected without touching the tracking layer.
       The tracking rail is preferred (top slot); if tracking is off or its rail is
       missing, a standalone floating button keeps the module self-sufficient. ---- */
    function ensureButton() {
        if (document.getElementById('anaBtn')) return;
        var btn = document.createElement('button');
        btn.id = 'anaBtn';
        btn.title = 'Insights — activity at a glance';
        btn.setAttribute('aria-label', 'Insights');
        btn.textContent = '📊';
        var rail = document.querySelector('.trk-controls');
        if (rail) rail.insertBefore(btn, rail.firstChild);
        else { btn.className = 'ana-fab'; document.body.appendChild(btn); }
        btn.addEventListener('click', toggleDrawer);
    }

    try {
        ensureButton();
    } catch (e) {
        /* the isolation contract: analytics may fail, the room may not */
        console.warn('Insights module failed to initialise; the map is unaffected.', e);
    }
})();
