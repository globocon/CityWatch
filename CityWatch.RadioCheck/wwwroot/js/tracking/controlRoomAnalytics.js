/* CityWatch Insights — the isolated analytics drawer (plan A1 + A2).
   A SEPARATE module by contract: it reads only /api/analytics/*, renders only its own
   ana-* DOM, and never calls into controlRoomMap.js or controlRoomTracking.js. If this
   file throws, or the analytics API is down, the drawer says "Analytics temporarily
   unavailable." and the map, live tracking, SignalR and replay continue untouched.
   No polling while closed — the drawer costs nothing until an operator opens it.

   The manager's reading order (approved plan §02): what is happening (KPI numbers) →
   is it normal (compare deltas + the pulse) → where is the problem (the activity
   sections, worst first) → who is behind it (drill-downs, phase A3). */
(function () {
    'use strict';

    var REFRESH_MS = 60000;
    var isOpen = false, seq = 0, timer = null;
    var winKind = 'today';
    var winStamp = 0;            // bumped on every window change; sections compare against it

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    /* Server datetimes round-trip without a zone marker; a raw new Date() would read
       them as local. Same defence the tracking layer uses. */
    function utcDate(v) {
        if (typeof v === 'string' && !/(Z|[+-]\d\d:?\d\d)$/.test(v)) return new Date(v + 'Z');
        return new Date(v);
    }
    function hm(v) { return utcDate(v).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); }
    function dayHm(v) {
        var d = utcDate(v);
        return d.toDateString() === new Date().toDateString()
            ? hm(v)
            : d.toLocaleDateString([], { day: '2-digit', month: 'short' }) + ' ' + hm(v);
    }
    function fmtHours(min) {
        min = Math.max(0, Math.round(min));
        return Math.floor(min / 60) + 'h ' + (min % 60) + 'm';
    }

    /* ---- windows: the chips' meaning, and what "previous" means for each ---- */
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
    function windowDays(w) {
        return Math.max(1, Math.round((w.toUtc - w.fromUtc) / 864e5));
    }
    function qs(w) {
        return 'fromUtc=' + w.fromUtc.toISOString() + '&toUtc=' + w.toUtc.toISOString();
    }

    /* ================= sections framework (A2) =================
       Each activity card fetches lazily on first expand and re-fetches only when the
       window has changed since — historical reads are audited server-side, so the
       drawer asks only for what the operator actually opens. */
    var SECTIONS = {
        guards: { icon: '👮', title: 'GUARDS', path: 'guards', render: renderGuards },
        sites: { icon: '🏢', title: 'SITES', path: 'sites', render: renderSites },
        pcars: { icon: '🚔', title: 'PATROL CARS', path: 'pcars', render: renderPcars },
        wands: { icon: '📟', title: 'SMART WANDS', path: 'wands', render: renderWands }
    };
    Object.keys(SECTIONS).forEach(function (k) {
        SECTIONS[k].key = k; SECTIONS[k].stamp = -1; SECTIONS[k].seq = 0;
    });

    function openSet() {
        try { return JSON.parse(localStorage.getItem('anaSecOpen') || '{}'); } catch (e) { return {}; }
    }
    function saveOpen(set) {
        try { localStorage.setItem('anaSecOpen', JSON.stringify(set)); } catch (e) { /* private mode */ }
    }
    function isSecOpen(key) { return !!openSet()[key]; }

    function sectionShell(sec) {
        return '<div class="ana-sec" id="anaSec_' + sec.key + '">' +
                 '<button class="ana-sec-h" data-ana-sec="' + sec.key + '" aria-expanded="' + isSecOpen(sec.key) + '">' +
                   '<span class="i">' + sec.icon + '</span><span class="t">' + sec.title + '</span>' +
                   '<span class="n" id="anaSecN_' + sec.key + '"></span>' +
                   '<span class="chev">' + (isSecOpen(sec.key) ? '▴' : '▾') + '</span>' +
                 '</button>' +
                 '<div class="ana-sec-b" id="anaSecB_' + sec.key + '"' + (isSecOpen(sec.key) ? '' : ' style="display:none"') + '></div>' +
               '</div>';
    }

    function toggleSection(key) {
        var sec = SECTIONS[key];
        var set = openSet();
        set[key] = !set[key];
        saveOpen(set);
        var head = document.querySelector('[data-ana-sec="' + key + '"]');
        var body = document.getElementById('anaSecB_' + key);
        if (!head || !body) return;
        head.setAttribute('aria-expanded', String(!!set[key]));
        head.querySelector('.chev').textContent = set[key] ? '▴' : '▾';
        body.style.display = set[key] ? '' : 'none';
        if (set[key]) loadSection(sec);
    }

    async function loadSection(sec, force) {
        var body = document.getElementById('anaSecB_' + sec.key);
        if (!body) return;
        if (!force && sec.stamp === winStamp) return;      // already current for this window
        var w = currentWindow();
        if (!w) return;
        var mySeq = ++sec.seq;
        body.innerHTML = '<div class="ana-sec-load">loading…</div>';
        try {
            var res = await fetch('/api/analytics/' + sec.path + '?' + qs(w), { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            var data = await res.json();
            if (mySeq !== sec.seq || !isOpen) return;
            sec.stamp = winStamp;
            sec.render(body, data, w);
        } catch (e) {
            if (mySeq !== sec.seq || !isOpen) return;
            sec.stamp = -1;
            body.innerHTML = '<div class="ana-sec-load">unavailable — <button class="ana-linkbtn" data-ana-retry-sec="' + sec.key + '">retry</button></div>';
        }
    }

    function reloadOpenSections() {
        Object.keys(SECTIONS).forEach(function (k) {
            if (isSecOpen(k)) loadSection(SECTIONS[k]);
        });
    }

    function setSecCount(key, text) {
        var n = document.getElementById('anaSecN_' + key);
        if (n) n.textContent = text || '';
    }

    /* ---- shared row: name · relative bar · value; detail lives in the hover tip ---- */
    function barRows(items, opts) {
        var max = 1;
        items.forEach(function (it) { if (it.value > max) max = it.value; });
        return items.map(function (it) {
            return '<div class="ana-row" data-tip="' + esc(it.tip) + '">' +
                     '<span class="t">' + esc(it.label) + '</span>' +
                     '<span class="bar"><i style="width:' + Math.max(3, Math.round(it.value / max * 100)) + '%"></i></span>' +
                     '<span class="v">' + esc(it.valueText != null ? it.valueText : it.value) + '</span>' +
                   '</div>';
        }).join('');
    }

    /* ================= section renderers ================= */

    function renderGuards(body, data) {
        var guards = data.guards || [];
        setSecCount('guards', guards.length ? '· ' + guards.length : '');
        if (!guards.length) { body.innerHTML = '<div class="ana-sec-load">no guard activity in this window</div>'; return; }
        var rows = guards.slice(0, 8).map(function (g) {
            return {
                label: g.name,
                value: g.checkIns + g.visits,
                tip: g.name + ' — ' + g.checkIns + ' check-ins · ' + g.visits + ' visits · '
                    + fmtHours(g.activeMinutes) + ' · ' + g.sessions + ' shift' + (g.sessions === 1 ? '' : 's')
            };
        });
        body.innerHTML = '<div class="ana-sec-cap">ranked by check-ins + visits · hover for detail</div>' +
            barRows(rows) +
            (guards.length > 8 ? '<div class="ana-more">+ ' + (guards.length - 8) + ' more' + (data.truncated ? ' (list capped at 100)' : '') + '</div>' : '');
    }

    function renderSites(body, data) {
        var sites = data.sites || [], quiet = data.quiet || [];
        setSecCount('sites', sites.length ? '· ' + sites.length : '');
        if (!sites.length && !quiet.length) { body.innerHTML = '<div class="ana-sec-load">no site activity in this window</div>'; return; }
        var rows = sites.slice(0, 8).map(function (s) {
            return {
                label: s.name,
                value: s.visits + s.checkIns,
                tip: s.name + ' — ' + s.checkIns + ' check-ins · ' + s.visits + ' visits · ' + s.units + ' unit' + (s.units === 1 ? '' : 's')
            };
        });
        var quietHtml = quiet.length
            ? '<div class="ana-quiet"><b>😴 Quiet — signed in, no evidence:</b> ' +
              quiet.slice(0, 5).map(function (q) { return esc(q.name); }).join(', ') +
              (quiet.length > 5 ? ' +' + (quiet.length - 5) + ' more' : '') + '</div>'
            : '';
        body.innerHTML = '<div class="ana-sec-cap">busiest first · hover for detail</div>' +
            barRows(rows) +
            (sites.length > 8 ? '<div class="ana-more">+ ' + (sites.length - 8) + ' more</div>' : '') +
            quietHtml;
    }

    function renderPcars(body, data) {
        var cars = data.cars || [];
        setSecCount('pcars', cars.length ? '· ' + cars.length : '');
        if (!cars.length) { body.innerHTML = '<div class="ana-sec-load">no patrol car activity in this window</div>'; return; }
        var rows = cars.slice(0, 8).map(function (c) {
            return {
                label: c.label + (c.guardName ? ' · ' + c.guardName.split(' ')[0] : ''),
                value: c.distanceKm,
                valueText: c.distanceKm + ' km',
                tip: c.label + (c.guardName ? ' — ' + c.guardName : '') + ' · ' + c.legs + ' legs · '
                    + c.visits + ' site visits · ' + fmtHours(c.activeMinutes)
            };
        });
        body.innerHTML = '<div class="ana-sec-cap">ranked by distance · hover for detail</div>' +
            barRows(rows) +
            (cars.length > 8 ? '<div class="ana-more">+ ' + (cars.length - 8) + ' more</div>' : '');
    }

    function wandState(perDay, avg) {
        if (avg === 0) return { cls: 'ok', txt: '✓ new' };
        if (perDay === 0) return { cls: 'crit', txt: '✕ silent' };
        if (perDay < avg * 0.5) return { cls: 'warn', txt: '⚠ falling' };
        return { cls: 'ok', txt: '✓ steady' };
    }

    function renderWands(body, data, w) {
        var wands = data.wands || [];
        setSecCount('wands', wands.length ? '· ' + wands.length : '');
        if (!wands.length) { body.innerHTML = '<div class="ana-sec-load">no wand scans in this window or the 7 days before it</div>'; return; }
        var days = windowDays(w);
        var rows = wands.slice(0, 10).map(function (x) {
            var perDay = x.scans / days;
            var st = wandState(perDay, x.prevDailyAvg);
            var rate = days > 1 ? Math.round(perDay) + '/d' : String(x.scans);
            return '<div class="ana-wrow" data-tip="' + esc(x.name + (x.siteName ? ' @ ' + x.siteName : '') +
                       ' — ' + x.scans + ' scans in window · 7-day avg ' + x.prevDailyAvg + '/day · last scan ' + dayHm(x.lastScanUtc)) + '">' +
                     '<span class="t">' + esc(x.name) + (x.siteName ? '<span class="s">' + esc(x.siteName) + '</span>' : '') + '</span>' +
                     '<span class="v">' + esc(rate) + ' <span class="dim">vs ' + esc(x.prevDailyAvg) + '/d</span></span>' +
                     '<span class="ana-chip ' + st.cls + '">' + st.txt + '</span>' +
                   '</div>';
        }).join('');
        body.innerHTML = '<div class="ana-sec-cap">worst first — a quiet wand leads the list · hover for detail</div>' + rows +
            (wands.length > 10 ? '<div class="ana-more">+ ' + (wands.length - 10) + ' more</div>' : '');
    }

    /* ================= the pulse (A2): events per bucket, both windows ================= */

    function renderPulse(pulse) {
        var el = document.getElementById('anaPulse');
        if (!el || !pulse || !pulse.buckets || !pulse.buckets.length) { if (el) el.innerHTML = ''; return; }
        var b = pulse.buckets;
        var W = 328, H = 86, top = 6, bottom = 16;
        var plotH = H - top - bottom;
        var max = 1;
        b.forEach(function (x) { max = Math.max(max, x.current, x.previous); });
        var bw = W / b.length;
        var bars = '', dots = '';
        b.forEach(function (x, i) {
            var h = Math.round(x.current / max * plotH);
            var label = pulse.bucketHours === 24
                ? utcDate(x.utc).toLocaleDateString([], { weekday: 'short' })
                : hm(x.utc);
            bars += '<rect x="' + (i * bw + 1).toFixed(1) + '" y="' + (top + plotH - h) +
                '" width="' + Math.max(1, bw - 2).toFixed(1) + '" height="' + Math.max(1, h) +
                '" rx="1.5" fill="#3987e5" data-tip="' + esc(label + ' — ' + x.current + ' events · prev ' + x.previous) + '"></rect>';
            var py = top + plotH - Math.round(x.previous / max * plotH);
            dots += '<circle cx="' + (i * bw + bw / 2).toFixed(1) + '" cy="' + py + '" r="1.6" fill="#9aa0ad"></circle>';
        });
        var first = pulse.bucketHours === 24
            ? utcDate(b[0].utc).toLocaleDateString([], { day: '2-digit', month: 'short' }) : hm(b[0].utc);
        var last = pulse.bucketHours === 24
            ? utcDate(b[b.length - 1].utc).toLocaleDateString([], { day: '2-digit', month: 'short' }) : hm(b[b.length - 1].utc);
        el.innerHTML =
            '<div class="ana-pulse-cap">ACTIVITY · scans + arrivals + sign-ins <span class="dim">· dots = previous</span></div>' +
            '<svg viewBox="0 0 ' + W + ' ' + H + '" role="img" aria-label="Activity per ' +
                (pulse.bucketHours === 24 ? 'day' : 'hour') + ', current window as bars, previous as dots.">' +
              '<line x1="0" y1="' + (top + plotH) + '" x2="' + W + '" y2="' + (top + plotH) + '" stroke="#2a2d37"></line>' +
              bars + dots +
              '<text x="1" y="' + (H - 3) + '" font-size="9" fill="#9aa0ad">' + esc(first) + '</text>' +
              '<text x="' + (W - 1) + '" y="' + (H - 3) + '" font-size="9" fill="#9aa0ad" text-anchor="end">' + esc(last) + '</text>' +
              '<text x="1" y="' + (top + 8) + '" font-size="9" fill="#9aa0ad">max ' + max + '</text>' +
            '</svg>';
    }

    /* ================= hover tooltips: one element, delegated ================= */

    function tipEl() {
        var el = document.getElementById('anaTip');
        if (!el) {
            el = document.createElement('div');
            el.id = 'anaTip';
            el.className = 'ana-tip';
            document.body.appendChild(el);
        }
        return el;
    }
    function wireTips(root) {
        root.addEventListener('mousemove', function (ev) {
            var t = ev.target.closest && ev.target.closest('[data-tip]');
            var tip = tipEl();
            if (!t) { tip.style.display = 'none'; return; }
            tip.textContent = t.getAttribute('data-tip');
            tip.style.display = 'block';
            var x = Math.min(ev.clientX + 12, window.innerWidth - tip.offsetWidth - 8);
            var y = Math.max(8, ev.clientY - tip.offsetHeight - 10);
            tip.style.left = x + 'px';
            tip.style.top = y + 'px';
        });
        root.addEventListener('mouseleave', function () { tipEl().style.display = 'none'; });
    }

    /* ================= drawer shell ================= */

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
            '<div class="ana-scroll">' +
              '<div class="ana-body" id="anaBody"></div>' +
              '<div class="ana-pulse" id="anaPulse"></div>' +
              Object.keys(SECTIONS).map(function (k) { return sectionShell(SECTIONS[k]); }).join('') +
            '</div>' +
            '<div class="ana-foot">' +
              '<span class="ana-note" id="anaNote"></span>' +
              '<span class="ana-upd" id="anaUpd"></span>' +
              '<button id="anaRefresh" title="Refresh now" aria-label="Refresh">⟳</button>' +
            '</div>';
        document.body.appendChild(el);
        el.addEventListener('click', onDrawerClick);
        el.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeDrawer(); });
        wireTips(el);
        return el;
    }

    function changeWindow() {
        winStamp++;
        load();
        reloadOpenSections();
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
            if (winKind !== 'custom') changeWindow();
            return;
        }
        var sec = ev.target.closest('[data-ana-sec]');
        if (sec) { toggleSection(sec.getAttribute('data-ana-sec')); return; }
        var retry = ev.target.closest('[data-ana-retry-sec]');
        if (retry) { loadSection(SECTIONS[retry.getAttribute('data-ana-retry-sec')], true); return; }
        if (ev.target.id === 'anaLoad') { changeWindow(); return; }
        if (ev.target.id === 'anaRefresh') { load(); Object.keys(SECTIONS).forEach(function (k) { if (isSecOpen(k)) loadSection(SECTIONS[k], true); }); return; }
        if (ev.target.id === 'anaRetry') load();
    }

    /* ================= KPI summary (A1) ================= */

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
        var p = document.getElementById('anaPulse');
        if (p) p.innerHTML = '';
        document.getElementById('anaNote').textContent = '';
        document.getElementById('anaUpd').textContent = '';
    }

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
        renderPulse(data.pulse);
        document.getElementById('anaNote').textContent = note;
        document.getElementById('anaUpd').textContent =
            'updated ' + new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    async function load() {
        var w = currentWindow();
        if (!w) return;
        var mySeq = ++seq;
        renderLoading();
        try {
            var res = await fetch('/api/analytics/summary?' + qs(w), { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            var body = await res.json();
            if (mySeq !== seq || !isOpen) return;
            renderKpis(body, w.note);
        } catch (e) {
            if (mySeq !== seq || !isOpen) return;
            renderError();
        }
    }

    /* ================= open/close ================= */

    function openDrawer() {
        var el = drawerEl();
        isOpen = true;
        el.classList.add('open');
        el.focus({ preventScroll: true });
        winStamp++;                     // windows are relative to "now" — reopen means refresh
        load();
        reloadOpenSections();
        clearInterval(timer);
        timer = setInterval(function () { if (isOpen) load(); }, REFRESH_MS);
    }

    function closeDrawer() {
        isOpen = false;
        clearInterval(timer);
        timer = null;
        var el = document.getElementById('anaDrawer');
        if (el) el.classList.remove('open');
        tipEl().style.display = 'none';
    }

    function toggleDrawer() { isOpen ? closeDrawer() : openDrawer(); }

    /* ---- entry point: one button, injected without touching the tracking layer ---- */
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
