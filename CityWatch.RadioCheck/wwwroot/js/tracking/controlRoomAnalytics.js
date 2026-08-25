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
    var drill = null, drillSeq = 0;   // A3: { kind: guard|site|pcar|wand, id }

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
    function pad2(n) { return String(n).padStart(2, '0'); }
    /* The operator's calendar date — toISOString() is the UTC date, which in Australia
       is YESTERDAY until mid-morning, exactly the hours a control room reviews. */
    function localDateStr(d) {
        return d.getFullYear() + '-' + pad2(d.getMonth() + 1) + '-' + pad2(d.getDate());
    }

    /* ---- windows: the chips' meaning, and what "previous" means for each ----
       Day arithmetic uses setDate(), never fixed 24h milliseconds: a DST transition
       makes a 23- or 25-hour day, and evidence windows must not drop or double an
       hour of somebody's shift. */
    function presetWindow(kind) {
        var now = new Date();
        if (kind === 'today') {
            var f = new Date(now); f.setHours(0, 0, 0, 0);
            return { fromUtc: f, toUtc: now, note: 'vs the same hours yesterday' };
        }
        if (kind === 'yesterday') {
            var t = new Date(now); t.setHours(0, 0, 0, 0);
            var f2 = new Date(t); f2.setDate(f2.getDate() - 1);
            return { fromUtc: f2, toUtc: t, note: 'vs the day before' };
        }
        var f7 = new Date(now); f7.setDate(f7.getDate() - 7);
        return { fromUtc: f7, toUtc: now, note: 'vs the previous 7 days' };
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
    /* The week runs on its own axis — the last 7 local days — whatever the drawer's
       window chips say: "this week" is a fixed question. */
    function weekWindow() {
        var now = new Date();
        var f = new Date(now); f.setHours(0, 0, 0, 0); f.setDate(f.getDate() - 6);
        return { fromUtc: f, toUtc: now };
    }

    var SECTIONS = {
        guards: { icon: '👮', title: 'GUARDS', path: 'guards', render: renderGuards },
        sites: { icon: '🏢', title: 'SITES', path: 'sites', render: renderSites },
        pcars: { icon: '🚔', title: 'PATROL CARS', path: 'pcars', render: renderPcars },
        wands: { icon: '📟', title: 'SMART WANDS', path: 'wands', render: renderWands },
        week: {
            icon: '📅', title: 'WEEK · PATROL FQ', path: 'weekly', render: renderWeekly,
            window: weekWindow,
            /* Local-day bucketing: the offset is measured AT THE WINDOW START, so a DST
               change inside the week shifts at most the transition day. */
            extraQs: function (w) { return '&tzOffsetMinutes=' + (-w.fromUtc.getTimezoneOffset()); }
        }
    };
    Object.keys(SECTIONS).forEach(function (k) {
        SECTIONS[k].key = k; SECTIONS[k].stamp = -1; SECTIONS[k].seq = 0;
    });

    /* Section open/closed state lives in memory first — localStorage is only the
       persistence layer. In private browsing setItem throws, and state that lives
       ONLY there would make an opened section impossible to collapse. */
    var secOpenState = (function () {
        try { return JSON.parse(localStorage.getItem('anaSecOpen') || '{}'); } catch (e) { return {}; }
    })();
    function openSet() { return secOpenState; }
    function saveOpen(set) {
        secOpenState = set;
        try { localStorage.setItem('anaSecOpen', JSON.stringify(set)); } catch (e) { /* private mode */ }
    }
    function isSecOpen(key) { return !!openSet()[key]; }

    function sectionShell(sec) {
        return '<div class="ana-sec" id="anaSec_' + sec.key + '">' +
                 '<button class="ana-sec-h' + (isSecOpen(sec.key) ? ' open' : '') + '" data-ana-sec="' + sec.key +
                   '" aria-expanded="' + isSecOpen(sec.key) + '">' +
                   '<span class="i">' + sec.icon + '</span><span class="t">' + sec.title + '</span>' +
                   '<span class="n" id="anaSecN_' + sec.key + '"></span>' +
                   '<span class="chev">▾</span>' +
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
        head.classList.toggle('open', !!set[key]);
        body.style.display = set[key] ? '' : 'none';
        if (set[key]) loadSection(sec);
    }

    async function loadSection(sec, force) {
        var body = document.getElementById('anaSecB_' + sec.key);
        if (!body) return;
        if (!force && sec.stamp === winStamp) return;      // already current for this window
        var w = sec.window ? sec.window() : currentWindow();
        if (!w) return;
        var mySeq = ++sec.seq;
        /* Capture the stamp NOW: certifying a response with the stamp read after the
           await would let a stale fetch pass off old-window data as current. */
        var myStamp = winStamp;
        body.innerHTML = '<div class="ana-sec-load">loading…</div>';
        var url = '/api/analytics/' + sec.path + '?' + qs(w) +
            (sec.extraQs ? sec.extraQs(w) : '');
        try {
            var res = await fetch(url, { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            var data = await res.json();
            if (mySeq !== sec.seq || !isOpen || myStamp !== winStamp) return;
            sec.stamp = myStamp;
            sec.data = data;                 // drill-down headers reuse the card's numbers
            sec.render(body, data, w);
        } catch (e) {
            if (mySeq !== sec.seq || !isOpen || myStamp !== winStamp) return;
            sec.stamp = -1;
            body.innerHTML = '<div class="ana-sec-load">unavailable — <button class="ana-linkbtn" data-ana-retry-sec="' + sec.key + '">retry</button></div>';
        }
    }

    function reloadOpenSections(force) {
        Object.keys(SECTIONS).forEach(function (k) {
            if (isSecOpen(k)) loadSection(SECTIONS[k], force);
        });
    }

    function setSecCount(key, text) {
        var n = document.getElementById('anaSecN_' + key);
        if (n) n.textContent = text || '';
    }

    /* ---- shared row: name · relative bar · value; detail lives in the hover tip;
       the row itself is the doorway (A3): click → drill-down ---- */
    function barRows(items) {
        var max = 1;
        items.forEach(function (it) { if (it.value > max) max = it.value; });
        return items.map(function (it, i) {
            return '<div class="ana-row" data-tip="' + esc(it.tip) + '"' +
                     (it.drill ? ' data-ana-drill="' + it.drill + '" role="button" tabindex="0"' : '') + '>' +
                     '<span class="t">' + esc(it.label) + '</span>' +
                     '<span class="bar"><i style="width:' + Math.max(3, Math.round(it.value / max * 100)) +
                       '%;animation-delay:' + (i * 45) + 'ms"></i></span>' +
                     '<span class="v">' + esc(it.valueText != null ? it.valueText : it.value) + '</span>' +
                     '<span class="chev">' + (it.drill ? '›' : '') + '</span>' +
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
                drill: 'guard:' + g.guardId,
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
                drill: 'site:' + s.siteId,
                tip: s.name + ' — ' + s.checkIns + ' check-ins · ' + s.visits + ' visits · ' + s.units + ' unit' + (s.units === 1 ? '' : 's')
            };
        });
        var quietHtml = quiet.length
            ? '<div class="ana-quiet"><b>😴 Quiet — signed in, no evidence:</b> ' +
              quiet.slice(0, 5).map(function (q) {
                  return '<button class="ana-linkbtn" data-ana-drill="site:' + q.siteId + '">' + esc(q.name) + '</button>';
              }).join(', ') +
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
                drill: 'pcar:' + c.unitId,
                tip: c.label + (c.guardName ? ' — ' + c.guardName : '') + ' · ' + c.legs + ' legs · '
                    + c.visits + ' site visits · ' + fmtHours(c.activeMinutes)
            };
        });
        body.innerHTML = '<div class="ana-sec-cap">ranked by distance · hover for detail</div>' +
            barRows(rows) +
            (cars.length > 8 ? '<div class="ana-more">+ ' + (cars.length - 8) + ' more</div>' : '');
    }

    /* Judged against what the baseline EXPECTS for the elapsed part of the window —
       a 10/day wand with 3 scans by 08:00 is on pace, not "falling", and at 00:30
       nothing is "silent" yet. Verdicts need at least one expected scan; "falling"
       needs two, so a slow morning never leads the worst-first list falsely. */
    function wandState(scans, avg, elapsedDays) {
        if (avg === 0) return { cls: 'ok', txt: '✓ new' };
        var expected = avg * elapsedDays;
        if (scans === 0 && expected >= 1) return { cls: 'crit', txt: '✕ silent' };
        if (expected >= 2 && scans < expected * 0.5) return { cls: 'warn', txt: '⚠ falling' };
        return { cls: 'ok', txt: '✓ steady' };
    }

    function renderWands(body, data, w) {
        var wands = data.wands || [];
        setSecCount('wands', wands.length ? '· ' + wands.length : '');
        if (!wands.length) { body.innerHTML = '<div class="ana-sec-load">no wand scans in this window or the 7 days before it</div>'; return; }
        var days = windowDays(w);
        var elapsedDays = Math.max(0.02, (w.toUtc - w.fromUtc) / 864e5);
        var rows = wands.slice(0, 10).map(function (x) {
            var st = wandState(x.scans, x.prevDailyAvg, elapsedDays);
            var rate = days > 1 ? Math.round(x.scans / days) + '/d' : String(x.scans);
            return '<div class="ana-wrow" role="button" tabindex="0" data-ana-drill="wand:' + x.wandId + '" data-tip="' + esc(x.name + (x.siteName ? ' @ ' + x.siteName : '') +
                       ' — ' + x.scans + ' scans in window · 7-day avg ' + x.prevDailyAvg + '/day · last scan ' + dayHm(x.lastScanUtc)) + '">' +
                     '<span class="t">' + esc(x.name) + (x.siteName ? '<span class="s">' + esc(x.siteName) + '</span>' : '') + '</span>' +
                     '<span class="v">' + esc(rate) + ' <span class="dim">vs ' + esc(x.prevDailyAvg) + '/d</span></span>' +
                     '<span class="ana-chip ' + st.cls + '">' + st.txt + '</span>' +
                     '<span class="chev">›</span>' +
                   '</div>';
        }).join('');
        body.innerHTML = '<div class="ana-sec-cap">worst first — a quiet wand leads the list · hover for detail</div>' + rows +
            (wands.length > 10 ? '<div class="ana-more">+ ' + (wands.length - 10) + ' more</div>' : '');
    }

    var WEEK_GLYPH = {
        met: { g: '✓', cls: 'met' }, missed: { g: '✕', cls: 'missed' },
        active: { g: '·', cls: 'act' }, noduty: { g: '–', cls: 'nod' }
    };

    function renderWeekly(body, data) {
        var sites = data.sites || [], days = data.days || [];
        setSecCount('week', sites.length ? '· ' + sites.length : '');
        if (!sites.length) { body.innerHTML = '<div class="ana-sec-load">no targets or activity this week</div>'; return; }
        var head = '<div class="ana-wkrow ana-wkhead"><span class="t"></span>' +
            days.map(function (d) {
                return '<span class="c">' + esc(new Date(d + 'T00:00').toLocaleDateString([], { weekday: 'narrow' })) + '</span>';
            }).join('') + '</div>';
        var rows = sites.slice(0, 30).map(function (s) {
            var cells = s.cells.map(function (c, i) {
                var g = WEEK_GLYPH[c.state] || WEEK_GLYPH.noduty;
                return '<span class="c ' + g.cls + '" data-tip="' +
                    esc(new Date(days[i] + 'T00:00').toLocaleDateString([], { weekday: 'short', day: '2-digit', month: 'short' }) +
                        ' — ' + c.done + (s.target ? '/' + s.target : '') + ' rounds · ' + c.scans + ' scans') + '">' + g.g + '</span>';
            }).join('');
            return '<div class="ana-wkrow" data-ana-drill="site:' + s.siteId + '" role="button" tabindex="0" data-tip="' +
                esc(s.name + (s.target ? ' — target ' + s.target + '/day' : ' — no target set') +
                    ' · met ' + s.met + ' · missed ' + s.missed) + '">' +
                '<span class="t">' + esc(s.name) + '</span>' + cells + '</div>';
        }).join('');
        var t = data.totals || { met: 0, missed: 0 }, p = data.prevTotals || { met: 0, missed: 0 };
        body.innerHTML =
            '<div class="ana-sec-cap">worst first · ✓ met · ✕ missed · – no duty · rounds vs agreed patrol frequency</div>' +
            head + rows +
            (sites.length > 30 ? '<div class="ana-more">+ ' + (sites.length - 30) + ' more in the printed report</div>' : '') +
            '<div class="ana-wktotal">This week: <b class="ok">' + t.met + ' met</b> · <b class="bad">' + t.missed +
                ' missed</b> <span class="dim">(last week ' + p.met + ' · ' + p.missed + ')</span></div>' +
            '<div class="ana-replayrow"><button class="ana-printbtn" data-ana-print="week">📄 Print / PDF — weekly summary</button></div>';
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
            /* The newest bucket wears the accent — "this is now". Bars rise in a wave. */
            var fill = i === b.length - 1 ? '#a78bfa' : 'url(#anaPulseG)';
            bars += '<rect x="' + (i * bw + 1).toFixed(1) + '" y="' + (top + plotH - h) +
                '" width="' + Math.max(1, bw - 2).toFixed(1) + '" height="' + Math.max(1, h) +
                '" rx="1.5" fill="' + fill + '" style="animation-delay:' + (i * 14) + 'ms" data-tip="' +
                esc(label + ' — ' + x.current + ' events · prev ' + x.previous) + '"></rect>';
            var py = top + plotH - Math.round(x.previous / max * plotH);
            dots += '<circle cx="' + (i * bw + bw / 2).toFixed(1) + '" cy="' + py + '" r="1.6" fill="#98a0af"></circle>';
        });
        var first = pulse.bucketHours === 24
            ? utcDate(b[0].utc).toLocaleDateString([], { day: '2-digit', month: 'short' }) : hm(b[0].utc);
        var last = pulse.bucketHours === 24
            ? utcDate(b[b.length - 1].utc).toLocaleDateString([], { day: '2-digit', month: 'short' }) : hm(b[b.length - 1].utc);
        el.innerHTML =
            '<div class="ana-pulse-cap">ACTIVITY · scans + arrivals + sign-ins <span class="dim">· dots = previous</span></div>' +
            '<svg viewBox="0 0 ' + W + ' ' + H + '" role="img" aria-label="Activity per ' +
                (pulse.bucketHours === 24 ? 'day' : 'hour') + ', current window as bars, previous as dots.">' +
              '<defs><linearGradient id="anaPulseG" x1="0" y1="1" x2="0" y2="0">' +
                '<stop offset="0" stop-color="#2a6fd0"></stop><stop offset="1" stop-color="#8b5cf6"></stop>' +
              '</linearGradient></defs>' +
              '<line x1="0" y1="' + (top + plotH) + '" x2="' + W + '" y2="' + (top + plotH) + '" stroke="#262b36"></line>' +
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
    /* One measured layout per hovered element, not per mousemove — this drawer sits
       over a live map, and a forced reflow 60×/second would jank its markers. */
    var lastTipFor = null, tipW = 0, tipH = 0;
    function wireTips(root) {
        root.addEventListener('mousemove', function (ev) {
            var t = ev.target.closest && ev.target.closest('[data-tip]');
            var tip = tipEl();
            if (!t) { tip.style.display = 'none'; lastTipFor = null; return; }
            if (t !== lastTipFor) {
                lastTipFor = t;
                tip.textContent = t.getAttribute('data-tip');
                tip.style.display = 'block';
                tipW = tip.offsetWidth;
                tipH = tip.offsetHeight;
            }
            tip.style.left = Math.min(ev.clientX + 12, window.innerWidth - tipW - 8) + 'px';
            tip.style.top = Math.max(8, ev.clientY - tipH - 10) + 'px';
        });
        root.addEventListener('mouseleave', function () { tipEl().style.display = 'none'; lastTipFor = null; });
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
        var today = localDateStr(new Date());
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
              '<div id="anaMain">' +
                '<div class="ana-body" id="anaBody"></div>' +
                '<div class="ana-pulse" id="anaPulse"></div>' +
                Object.keys(SECTIONS).map(function (k) { return sectionShell(SECTIONS[k]); }).join('') +
              '</div>' +
              '<div id="anaDrill" style="display:none"></div>' +
            '</div>' +
            '<div class="ana-foot">' +
              '<span class="ana-live" title="Live — refreshes every 60 s while open"></span>' +
              '<span class="ana-note" id="anaNote"></span>' +
              '<span class="ana-upd" id="anaUpd"></span>' +
              '<button id="anaRefresh" title="Refresh now" aria-label="Refresh">⟳</button>' +
            '</div>';
        document.body.appendChild(el);
        el.addEventListener('click', onDrawerClick);
        el.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') { if (drill) closeDrill(); else closeDrawer(); return; }
            if ((e.key === 'Enter' || e.key === ' ') && e.target.closest && e.target.closest('[data-ana-drill]')) {
                e.preventDefault();
                var parts = e.target.closest('[data-ana-drill]').getAttribute('data-ana-drill').split(':');
                openDrill(parts[0], Number(parts[1]));
            }
        });
        wireTips(el);
        return el;
    }

    function changeWindow() {
        winStamp++;
        load();
        reloadOpenSections();
        if (drill) loadDrill();
    }

    function onDrawerClick(ev) {
        if (ev.target.closest('[data-ana-close]')) { closeDrawer(); return; }
        var chip = ev.target.closest('[data-ana-win]');
        if (chip) {
            var kind = chip.getAttribute('data-ana-win');
            document.querySelectorAll('#anaDrawer [data-ana-win]').forEach(function (b) {
                b.classList.toggle('on', b === chip);
            });
            document.getElementById('anaCustom').style.display = kind === 'custom' ? '' : 'none';
            /* Custom only ARMS the inputs — the active window must not change until
               Load, or the 60 s timer would silently swap the KPIs to a window the
               operator never chose. */
            if (kind !== 'custom') { winKind = kind; changeWindow(); }
            return;
        }
        var sec = ev.target.closest('[data-ana-sec]');
        if (sec) { toggleSection(sec.getAttribute('data-ana-sec')); return; }
        var retry = ev.target.closest('[data-ana-retry-sec]');
        if (retry) { loadSection(SECTIONS[retry.getAttribute('data-ana-retry-sec')], true); return; }
        var dr = ev.target.closest('[data-ana-drill]');
        if (dr) {
            var parts = dr.getAttribute('data-ana-drill').split(':');
            openDrill(parts[0], Number(parts[1]));
            return;
        }
        if (ev.target.closest('[data-ana-drill-back]')) { closeDrill(); return; }
        if (ev.target.closest('[data-ana-drill-retry]')) { loadDrill(); return; }
        if (ev.target.closest('[data-ana-replay]')) { jumpToReplay(); return; }
        var pr = ev.target.closest('[data-ana-print]');
        if (pr) {
            if (pr.getAttribute('data-ana-print') === 'week') printWeekly(); else printService();
            return;
        }
        if (ev.target.id === 'anaLoad') { winKind = 'custom'; changeWindow(); return; }
        if (ev.target.id === 'anaRefresh') { load(); reloadOpenSections(true); return; }
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
        /* Pure numbers count up on render — the drawer's open sequence. */
        var count = typeof value === 'number' && isFinite(value) && value > 0
            ? ' data-count="' + value + '"' : '';
        return '<div class="ana-tile">' +
                 '<span class="g">' + glyph + '</span>' +
                 '<span class="v"' + count + '>' + esc(value) + '</span>' +
                 deltaMarkup +
                 '<span class="k">' + esc(label) + '</span>' +
               '</div>';
    }

    function runCountUps(root) {
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        root.querySelectorAll('[data-count]').forEach(function (el) {
            var target = Number(el.getAttribute('data-count'));
            if (!isFinite(target) || target <= 0) return;
            var start = performance.now(), dur = 550;
            function step(now) {
                var p = Math.min(1, (now - start) / dur);
                el.textContent = String(Math.round(target * (1 - Math.pow(1 - p, 3))));
                if (p < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        });
    }

    function renderKpis(data, note, quiet) {
        /* Quiet = the 60 s background refresh: no skeleton, no entrance choreography,
           no count-ups — a wall monitor must not flash every minute. */
        var main = document.getElementById('anaMain');
        if (main) main.classList.toggle('ana-instant', !!quiet);
        var c = data.current, p = data.previous;
        document.getElementById('anaBody').innerHTML = '<div class="ana-grid">' +
            tile('👮', c.guardsActive, deltaHtml(c.guardsActive, p.guardsActive), 'Guards active') +
            tile('🚓', c.pcarsActive, deltaHtml(c.pcarsActive, p.pcarsActive), 'Patrol cars') +
            tile('🏢', c.sitesActive, deltaHtml(c.sitesActive, p.sitesActive), 'Sites active') +
            tile('📍', c.siteVisits, deltaHtml(c.siteVisits, p.siteVisits), 'Site visits') +
            tile('✓', c.checkIns, deltaHtml(c.checkIns, p.checkIns), 'Check-ins · NFC') +
            tile('⏱', fmtHours(c.activeMinutes), deltaHtml(c.activeMinutes, p.activeMinutes), 'Hours on duty') +
            '</div>';
        if (!quiet) runCountUps(document.getElementById('anaBody'));
        renderPulse(data.pulse);
        document.getElementById('anaNote').textContent = note;
        document.getElementById('anaUpd').textContent =
            'updated ' + new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    async function load(quiet) {
        var w = currentWindow();
        if (!w) return;
        var mySeq = ++seq;
        if (!quiet) renderLoading();
        try {
            var res = await fetch('/api/analytics/summary?' + qs(w), { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            var body = await res.json();
            if (mySeq !== seq || !isOpen) return;
            renderKpis(body, w.note, quiet);
        } catch (e) {
            if (mySeq !== seq || !isOpen) return;
            renderError();
        }
    }

    /* ================= drill-downs (A3): who/what is behind the number =================
       Clicking any row swaps the drawer to that entity's detail — header stats, the
       merged timeline, the site presence ribbon, the wand's 14-day rhythm — and ends
       with ▶ Open Replay, pre-filled. Analytics finds it; Replay proves it. */

    /* One declaration per drill kind — where its cache lives, which API parameter
       names it, what icon it wears. Adding a kind is one entry here, not four maps. */
    var DRILLS = {
        guard: { sec: 'guards', param: 'guardId', icon: '👮' },
        site: { sec: 'sites', param: 'siteId', icon: '🏢' },
        pcar: { sec: 'pcars', param: 'unitId', icon: '🚔' },
        wand: { sec: 'wands', param: 'wandId', icon: '📟' }
    };

    function drillRow(kind, id) {
        var d = SECTIONS[DRILLS[kind].sec].data;
        if (kind === 'guard') return d && (d.guards || []).find(function (x) { return x.guardId === id; });
        if (kind === 'pcar') return d && (d.cars || []).find(function (x) { return x.unitId === id; });
        if (kind === 'wand') return d && (d.wands || []).find(function (x) { return x.wandId === id; });
        /* A site drill can be entered from the sites card, its quiet list, OR the week
           grid — whichever cache knows the site names it. */
        var row = d && ((d.sites || []).find(function (x) { return x.siteId === id; })
            || (d.quiet || []).find(function (x) { return x.siteId === id; }));
        if (row) return row;
        var wk = SECTIONS.week.data;
        return wk && (wk.sites || []).find(function (x) { return x.siteId === id; });
    }

    /* The cached card row may belong to a previous window — its numbers must not sit
       beside a fresh timeline. Only a cache stamped for the CURRENT window is used. */
    function freshDrillRow() {
        if (drill.kind === 'site') {
            if (SECTIONS.sites.stamp === winStamp || SECTIONS.week.stamp === winStamp)
                return drillRow('site', drill.id);
            return null;
        }
        return SECTIONS[DRILLS[drill.kind].sec].stamp === winStamp
            ? drillRow(drill.kind, drill.id) : null;
    }

    function drillTitle() {
        var r = drillRow(drill.kind, drill.id) || {};
        if (drill.kind === 'guard') return { icon: DRILLS.guard.icon, name: r.name || ('Guard ' + drill.id) };
        if (drill.kind === 'site') return { icon: DRILLS.site.icon, name: r.name || ('Site ' + drill.id) };
        if (drill.kind === 'pcar') return { icon: DRILLS.pcar.icon, name: r.label || ('PC-' + drill.id), sub: r.guardName };
        return { icon: DRILLS.wand.icon, name: r.name || ('Wand ' + drill.id), sub: r.siteName };
    }

    function openDrill(kind, id) {
        drill = { kind: kind, id: id };
        document.getElementById('anaMain').style.display = 'none';
        document.getElementById('anaDrill').style.display = '';
        loadDrill();
    }

    function closeDrill() {
        drill = null;
        var box = document.getElementById('anaDrill');
        if (box) { box.style.display = 'none'; box.innerHTML = ''; }
        var main = document.getElementById('anaMain');
        if (main) main.style.display = '';
    }

    /* Wands read a fixed trend window — the rhythm the plan asks for; everything else
       follows the drawer's window. The server's 15-day gate is sized against this. */
    var WAND_TREND_DAYS = 14;
    function drillWindow() {
        if (drill.kind === 'wand') {
            var now = new Date();
            var f = new Date(now); f.setHours(0, 0, 0, 0); f.setDate(f.getDate() - (WAND_TREND_DAYS - 1));
            return { fromUtc: f, toUtc: now };
        }
        return currentWindow();
    }

    async function loadDrill() {
        if (!drill) return;
        var box = document.getElementById('anaDrill');
        var w = drillWindow();
        if (!box || !w) return;
        var mySeq = ++drillSeq;
        var t = drillTitle();
        box.innerHTML = drillHead(t) + '<div class="ana-sec-load">loading…</div>';
        var param = DRILLS[drill.kind].param;
        try {
            var res = await fetch('/api/analytics/timeline?' + qs(w) + '&' + param + '=' + drill.id,
                { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            var data = await res.json();
            if (mySeq !== drillSeq || !isOpen || !drill) return;
            renderDrill(box, t, data, w);
        } catch (e) {
            if (mySeq !== drillSeq || !isOpen || !drill) return;
            box.innerHTML = drillHead(t) +
                '<div class="ana-sec-load">unavailable — <button class="ana-linkbtn" data-ana-drill-retry="1">retry</button></div>';
        }
    }

    function drillHead(t) {
        return '<div class="ana-crumb"><button class="ana-linkbtn" data-ana-drill-back="1">‹ back to overview</button></div>' +
               '<div class="ana-dhead"><span class="i">' + t.icon + '</span>' +
                 '<span class="t"><b>' + esc(t.name) + '</b>' + (t.sub ? '<span>' + esc(t.sub) + '</span>' : '') + '</span></div>';
    }

    function chipStat(v, k) {
        return '<span class="ana-schip"><b>' + esc(v) + '</b>' + esc(k) + '</span>';
    }

    function drillChips(events) {
        var r = freshDrillRow() || {};
        var scans = 0, arrivals = 0, legs = 0, km = 0;
        events.forEach(function (e) {
            if (e.type === 'scan') scans++;
            else if (e.type === 'arrived') arrivals++;
            else if (e.type === 'leg') { legs++; km += e.km || 0; }
        });
        var last = events.length ? dayHm(events[events.length - 1].utc) : '—';
        var out;
        if (drill.kind === 'guard') {
            out = chipStat(scans, 'check-ins') + chipStat(arrivals, 'visits')
                + (r.activeMinutes != null ? chipStat(fmtHours(r.activeMinutes), 'on duty') : '')
                + (km ? chipStat(km.toFixed(1) + ' km', 'moved') : '')
                + chipStat(last, 'last activity');
        } else if (drill.kind === 'site') {
            var who = {};
            events.forEach(function (e) { if ((e.type === 'arrived' || e.type === 'scan') && e.who) who[e.who] = 1; });
            out = chipStat(arrivals, 'visits') + chipStat(scans, 'check-ins')
                + chipStat(Object.keys(who).length, 'people / cars')
                + chipStat(events.length ? dayHm(events[0].utc) : '—', 'first')
                + chipStat(last, 'last');
        } else if (drill.kind === 'pcar') {
            out = chipStat(km.toFixed(1) + ' km', 'driven') + chipStat(legs, 'legs')
                + chipStat(arrivals, 'site visits')
                + (r.activeMinutes != null ? chipStat(fmtHours(r.activeMinutes), 'on duty') : '')
                + chipStat(last, 'last activity');
        } else {
            out = chipStat(scans, 'scans · 14 d')
                + (r.prevDailyAvg != null ? chipStat(r.prevDailyAvg + '/d', '7-day avg') : '')
                + chipStat(last, 'last scan');
        }
        return '<div class="ana-chiprow">' + out + '</div>';
    }

    var EV = {
        signin: { i: '▶', f: function (e) { return 'Signed in' + (e.siteName ? ' — ' + e.siteName : ''); } },
        signout: { i: '⏹', f: function () { return 'Signed out'; } },
        arrived: {
            i: '📍', f: function (e) {
                return 'Arrived ' + (e.siteName || 'site')
                    + (e.minutes ? ' · stayed ' + fmtHours(e.minutes) : (e.exitedUtc ? '' : ' · still there'));
            }
        },
        scan: { i: '✓', f: function (e) { return 'Scan — ' + (e.siteName || '') + (e.wandName ? ' · ' + e.wandName : ''); } },
        leg: {
            i: '🚗', f: function (e) {
                return 'Drove ' + (e.km || 0) + ' km' + (e.siteName ? ' → ' + e.siteName : '')
                    + (e.minutes ? ' · ' + fmtHours(e.minutes) : '');
            }
        }
    };

    function evRows(events, multiDay) {
        if (!events.length) return '<div class="ana-sec-load">no recorded activity in this window</div>';
        var showWho = drill.kind === 'site' || drill.kind === 'wand';
        return '<div class="ana-evs">' + events.map(function (e) {
            var m = EV[e.type] || { i: '·', f: function () { return e.type; } };
            return '<div class="ana-ev"><span class="t">' + (multiDay ? dayHm(e.utc) : hm(e.utc)) + '</span>' +
                   '<span class="i">' + m.i + '</span>' +
                   '<span class="m">' + (showWho && e.who ? '<b>' + esc(e.who) + '</b> · ' : '') + esc(m.f(e)) + '</span></div>';
        }).join('') + '</div>';
    }

    /* The site presence ribbon: one row per person/car, blocks = confirmed stays,
       ticks = scans, on the drawer window's own time axis. */
    function ribbonSvg(events, w) {
        var rows = {};
        events.forEach(function (e) {
            if (e.type !== 'arrived' || !e.who) return;
            (rows[e.who] = rows[e.who] || []).push(e);
        });
        var names = Object.keys(rows).slice(0, 6);
        if (!names.length) return '';
        var W = 328, left = 80, plotW = W - left - 4, rowH = 22, top = 6;
        var H = top + names.length * rowH + 16;
        var t0 = w.fromUtc.getTime(), span = Math.max(1, w.toUtc.getTime() - t0);
        function x(t) { return left + Math.max(0, Math.min(plotW, (t - t0) / span * plotW)); }
        var colors = ['#3987e5', '#d95926', '#199e70', '#c98500', '#d55181', '#9085e9'];
        var svg = '';
        names.forEach(function (who, i) {
            var y = top + i * rowH;
            svg += '<text x="0" y="' + (y + 12) + '" font-size="10" fill="#e8e9ee">' +
                esc(who.length > 13 ? who.slice(0, 12) + '…' : who) + '</text>';
            rows[who].forEach(function (e) {
                var a = utcDate(e.utc).getTime();
                var b = e.exitedUtc ? utcDate(e.exitedUtc).getTime() : Math.min(Date.now(), w.toUtc.getTime());
                svg += '<rect x="' + x(a).toFixed(1) + '" y="' + (y + 3) + '" width="' +
                    Math.max(2, x(b) - x(a)).toFixed(1) + '" height="11" rx="2.5" fill="' + colors[i % colors.length] +
                    '" data-tip="' + esc(who + ' — ' + hm(e.utc) + (e.exitedUtc ? '–' + hm(e.exitedUtc) : ' → still there')) + '"></rect>';
            });
        });
        events.forEach(function (e) {
            if (e.type !== 'scan' || !e.who) return;
            var i = names.indexOf(e.who);
            if (i < 0) return;
            var xt = x(utcDate(e.utc).getTime()).toFixed(1);
            var y = top + i * rowH;
            svg += '<line x1="' + xt + '" y1="' + (y + 1) + '" x2="' + xt + '" y2="' + (y + 16) +
                '" stroke="#e8e9ee" stroke-width="1.2"></line>';
        });
        var axisY = top + names.length * rowH + 12;
        svg += '<text x="' + left + '" y="' + axisY + '" font-size="9" fill="#9aa0ad">' + hm(w.fromUtc) + '</text>' +
               '<text x="' + W + '" y="' + axisY + '" font-size="9" fill="#9aa0ad" text-anchor="end">' + hm(w.toUtc) + '</text>';
        return '<div class="ana-sec-cap" style="margin-top:12px">PRESENCE · blocks = confirmed stays · ticks = scans</div>' +
            '<svg viewBox="0 0 ' + W + ' ' + H + '" class="ana-ribbon" role="img" aria-label="Who was at the site, and when.">' + svg + '</svg>';
    }

    /* The wand's rhythm: scans per day across 14 days; hover says who carried it. */
    function wandDayBars(events, truncated) {
        var days = {};
        events.forEach(function (e) {
            if (e.type !== 'scan') return;
            var d = utcDate(e.utc);
            var key = d.getFullYear() + '-' + pad2(d.getMonth() + 1) + '-' + pad2(d.getDate());
            var slot = days[key] = days[key] || { n: 0, who: {} };
            slot.n++;
            if (e.who) slot.who[e.who.split(' ')[0]] = 1;
        });
        var list = [];
        for (var i = WAND_TREND_DAYS - 1; i >= 0; i--) {
            var d = new Date(); d.setHours(0, 0, 0, 0); d.setDate(d.getDate() - i);
            var key = d.getFullYear() + '-' + pad2(d.getMonth() + 1) + '-' + pad2(d.getDate());
            list.push({ d: d, n: days[key] ? days[key].n : 0, who: days[key] ? Object.keys(days[key].who) : [] });
        }
        var max = 1;
        list.forEach(function (x) { if (x.n > max) max = x.n; });
        var W = 328, H = 72, plotH = 48, bw = W / WAND_TREND_DAYS;
        var bars = list.map(function (x, i) {
            var h = Math.round(x.n / max * plotH);
            return '<rect x="' + (i * bw + 2).toFixed(1) + '" y="' + (6 + plotH - h) + '" width="' +
                (bw - 4).toFixed(1) + '" height="' + Math.max(2, h) + '" rx="2" fill="' +
                (x.n === 0 ? '#262a35' : '#3987e5') + '" data-tip="' +
                esc(x.d.toLocaleDateString([], { weekday: 'short', day: '2-digit', month: 'short' }) + ' — ' + x.n +
                    ' scans' + (x.who.length ? ' · ' + x.who.join(', ') : '')) + '"></rect>';
        }).join('');
        return '<div class="ana-sec-cap" style="margin-top:12px">SCANS PER DAY · ' + WAND_TREND_DAYS + ' DAYS · hover for who carried it' +
            (truncated ? ' · ⚠ earliest days partial (event cap)' : '') + '</div>' +
            '<svg viewBox="0 0 ' + W + ' ' + H + '" class="ana-ribbon" role="img" aria-label="Scans per day for the last 14 days.">' + bars +
            '<text x="2" y="' + (H - 3) + '" font-size="9" fill="#9aa0ad">' +
                list[0].d.toLocaleDateString([], { day: '2-digit', month: 'short' }) + '</text>' +
            '<text x="' + (W - 2) + '" y="' + (H - 3) + '" font-size="9" fill="#9aa0ad" text-anchor="end">today</text></svg>';
    }

    function replayBtn() {
        if (drill.kind === 'wand') return '';    // a wand is hardware, not a tracked unit
        if (!(window.CRM && typeof window.CRM.openReplay === 'function')) return '';
        return '<div class="ana-replayrow"><button class="ana-replaybtn" data-ana-replay="1">▶ Open Replay — ' +
            esc(drillTitle().name) + '</button></div>';
    }

    function jumpToReplay() {
        if (!drill || !(window.CRM && typeof window.CRM.openReplay === 'function')) return;
        var w = currentWindow();
        var pre = {};
        /* The window is handed over unconditionally — the replay selector owns its own
           cap and falls back to Today itself. One owner per policy. */
        if (w) { pre.fromUtc = w.fromUtc; pre.toUtc = w.toUtc; }
        if (drill.kind === 'guard') {
            pre.type = 'guard';
            /* The guards payload carries the unit key; the constant fallback mirrors
               TrackingUnitKey.GuardOffset for a stale cache and must match the server. */
            var row = drillRow('guard', drill.id);
            pre.unitId = row && row.unitId ? row.unitId : 1000000 + drill.id;
        }
        else if (drill.kind === 'pcar') { pre.type = 'car'; pre.unitId = drill.id; }
        else if (drill.kind === 'site') { pre.type = 'site'; pre.siteId = drill.id; }
        closeDrawer();
        window.CRM.openReplay(pre);
    }

    /* Quiet stretches, declared: ≥2 h with no recorded event between the window's
       edges. "No recorded activity 02:00–05:00" is a fact, and saying it is what
       makes the report credible — which is exactly why a TRUNCATED list must not
       declare a gap before its first retained event: the events were there, the cap
       dropped them, and a false accusation is worse than none. */
    function gapsOf(events, w, truncated) {
        var end = Math.min(Date.now(), w.toUtc.getTime());
        var times = events.map(function (e) { return utcDate(e.utc).getTime(); })
            .sort(function (a, b) { return a - b; });
        var marks = (truncated ? [] : [w.fromUtc.getTime()]).concat(times, [end]);
        var gaps = [];
        for (var i = 1; i < marks.length; i++)
            if (marks[i] - marks[i - 1] >= 2 * 3600 * 1000) gaps.push({ from: marks[i - 1], to: marks[i] });
        return gaps;
    }
    function gapLine(g) {
        return '⚠ no recorded activity ' + hm(new Date(g.from)) + '–' + hm(new Date(g.to)) +
            ' (' + fmtHours((g.to - g.from) / 60000) + ')';
    }

    var drillLast = null;     // { t, data, w } — what the proof-of-service print renders

    function renderDrill(box, t, data, w) {
        var events = data.events || [];
        var multiDay = (w.toUtc - w.fromUtc) > 36 * 3600 * 1000;
        drillLast = { t: t, data: data, w: w };
        var html = drillHead(t) + drillChips(events);
        if (drill.kind === 'site' && !multiDay) {
            html += ribbonSvg(events, w);
            var gaps = gapsOf(events, w, data.truncated);
            if (gaps.length)
                html += '<div class="ana-gaps">' + gaps.slice(0, 4).map(function (g) {
                    return '<div>' + esc(gapLine(g)) + '</div>';
                }).join('') + '</div>';
        }
        if (drill.kind === 'wand') html += wandDayBars(events, data.truncated);
        html += '<div class="ana-sec-cap" style="margin-top:12px">TIMELINE' +
            (drill.kind === 'wand' && events.length > 40 ? ' · most recent 40' : '') +
            (data.truncated ? ' · oldest not shown (capped at 400)' : '') + '</div>';
        html += evRows(drill.kind === 'wand' ? events.slice(-40) : events, drill.kind === 'wand' ? true : multiDay);
        html += replayBtn();
        if (drill.kind === 'site')
            html += '<div class="ana-replayrow"><button class="ana-printbtn" data-ana-print="service">📄 Print / PDF — proof of service</button></div>';
        box.innerHTML = html;
    }

    /* ================= client evidence (A4): print windows =================
       The browser's own print-to-PDF: dependency-free, vector-crisp, and the page is
       plain HTML anyone can audit. Light styling on purpose — this leaves the room. */

    function openPrint(title, bodyHtml) {
        var win = window.open('', '_blank');
        if (!win) {
            window.alert('The browser blocked the report window. Allow pop-ups for this site and try again.');
            return;
        }
        win.document.write('<!doctype html><html><head><meta charset="utf-8"><title>' + esc(title) + '</title>' +
            '<style>' +
            'body{font:13px/1.5 system-ui,-apple-system,"Segoe UI",sans-serif;color:#111;margin:28px;}' +
            'h1{font-size:19px;margin:0 0 2px;} .sub{color:#555;margin:0 0 18px;font-size:12px;}' +
            'table{border-collapse:collapse;width:100%;margin:10px 0;} ' +
            'th{font-size:10px;text-transform:uppercase;letter-spacing:.06em;color:#555;text-align:left;padding:5px 8px;border-bottom:1.5px solid #999;}' +
            'td{padding:5px 8px;border-bottom:1px solid #ddd;vertical-align:top;font-variant-numeric:tabular-nums;}' +
            '.c{text-align:center;} .met{color:#0a7a0a;font-weight:700;} .missed{color:#b00020;font-weight:700;}' +
            '.dim{color:#777;} .gap{color:#b00020;font-weight:600;}' +
            '.note{margin-top:16px;font-size:11px;color:#555;border-top:1px solid #ddd;padding-top:8px;}' +
            '@media print{ .noprint{display:none;} }' +
            '</style></head><body>' + bodyHtml +
            '<script>window.onload=function(){window.print();};<\/script></body></html>');
        win.document.close();
    }

    function printService() {
        if (!drillLast || !drill || drill.kind !== 'site') return;
        var t = drillLast.t, w = drillLast.w, events = drillLast.data.events || [];
        var multiDay = (w.toUtc - w.fromUtc) > 36 * 3600 * 1000;
        var scans = 0, arrivals = 0, who = {};
        events.forEach(function (e) {
            if (e.type === 'scan') scans++;
            if (e.type === 'arrived') arrivals++;
            if ((e.type === 'arrived' || e.type === 'scan') && e.who) who[e.who] = 1;
        });
        var period = w.fromUtc.toLocaleDateString([], { day: '2-digit', month: 'short', year: 'numeric' }) +
            ' ' + hm(w.fromUtc) + ' → ' + (multiDay
                ? w.toUtc.toLocaleDateString([], { day: '2-digit', month: 'short', year: 'numeric' }) + ' ' : '') + hm(w.toUtc);
        var gaps = multiDay ? [] : gapsOf(events, w, drillLast.data.truncated);
        var rows = events.map(function (e) {
            var m = EV[e.type] || { f: function () { return e.type; } };
            return '<tr><td>' + esc(multiDay ? dayHm(e.utc) : hm(e.utc)) + '</td><td>' + esc(e.who || '') +
                '</td><td>' + esc(m.f(e)) + '</td></tr>';
        }).join('');
        openPrint('Proof of Service — ' + t.name,
            '<h1>CityWatch — Proof of Service</h1>' +
            '<p class="sub">' + esc(t.name) + ' · ' + esc(period) + ' · generated ' +
                esc(new Date().toLocaleString()) + '</p>' +
            '<table><tr><th>Confirmed visits</th><th>Check-ins (NFC)</th><th>People / cars attended</th><th>First activity</th><th>Last activity</th></tr>' +
            '<tr><td>' + arrivals + '</td><td>' + scans + '</td><td>' + Object.keys(who).length + '</td><td>' +
                esc(events.length ? dayHm(events[0].utc) : '—') + '</td><td>' +
                esc(events.length ? dayHm(events[events.length - 1].utc) : '—') + '</td></tr></table>' +
            (gaps.length ? '<p class="gap">' + gaps.map(function (g) { return esc(gapLine(g)); }).join('<br>') + '</p>' : '') +
            '<table><tr><th>Time</th><th>Who</th><th>Event</th></tr>' +
            (rows || '<tr><td colspan="3" class="dim">No recorded activity in this period.</td></tr>') + '</table>' +
            '<p class="note">Prepared from CityWatch patrol-tracking records (NFC check-ins, confirmed site arrivals, ' +
            'signed-in sessions). Quiet periods are declared, never hidden' +
            (drillLast.data.truncated ? '; the oldest events were capped at 400 and are retained in the system of record' : '') + '.</p>');
    }

    function printWeekly() {
        var data = SECTIONS.week.data;
        if (!data || !data.sites) return;
        var days = data.days || [];
        var head = days.map(function (d) {
            return '<th class="c">' + esc(new Date(d + 'T00:00').toLocaleDateString([], { weekday: 'short', day: '2-digit' })) + '</th>';
        }).join('');
        var rows = data.sites.map(function (s) {
            return '<tr><td>' + esc(s.name) + '</td><td class="c">' + (s.target || '—') + '</td>' +
                s.cells.map(function (c) {
                    var g = WEEK_GLYPH[c.state] || WEEK_GLYPH.noduty;
                    return '<td class="c ' + g.cls + '">' + g.g + '</td>';
                }).join('') +
                '<td class="c met">' + s.met + '</td><td class="c missed">' + s.missed + '</td></tr>';
        }).join('');
        var t = data.totals || { met: 0, missed: 0 }, p = data.prevTotals || { met: 0, missed: 0 };
        openPrint('Weekly Patrol Frequency',
            '<h1>CityWatch — Weekly Patrol Frequency</h1>' +
            '<p class="sub">' + esc(days[0]) + ' → ' + esc(days[days.length - 1]) + ' · generated ' +
                esc(new Date().toLocaleString()) + ' · worst first</p>' +
            '<table><tr><th>Site</th><th class="c">Target/day</th>' + head +
                '<th class="c">Met</th><th class="c">Missed</th></tr>' + rows + '</table>' +
            '<p><b class="met">' + t.met + ' met</b> · <b class="missed">' + t.missed + ' missed</b> ' +
                '<span class="dim">(previous week: ' + p.met + ' met · ' + p.missed + ' missed)</span></p>' +
            '<p class="note">✓ target met · ✕ target missed with duty recorded · – no duty. Rounds are the better of ' +
            'traditional-wand counts and completed smart-wand inspection rounds — the same conservative rule the ' +
            'control-room board uses — held against the agreed daily patrol frequency. Today’s column is the day so far.</p>');
    }

    /* ================= open/close ================= */

    function openDrawer() {
        var el = drawerEl();
        isOpen = true;
        el.classList.add('open');
        var btn = document.getElementById('anaBtn');
        if (btn) btn.classList.add('on');
        el.focus({ preventScroll: true });
        winStamp++;                     // windows are relative to "now" — reopen means refresh
        load();
        reloadOpenSections();
        if (drill) loadDrill();
        clearInterval(timer);
        timer = setInterval(function () { if (isOpen) load(true); }, REFRESH_MS);
    }

    function closeDrawer() {
        isOpen = false;
        clearInterval(timer);
        timer = null;
        var el = document.getElementById('anaDrawer');
        if (el) el.classList.remove('open');
        var btn = document.getElementById('anaBtn');
        if (btn) btn.classList.remove('on');
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
