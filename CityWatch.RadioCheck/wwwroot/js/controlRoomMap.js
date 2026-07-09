/* Control Room Map — live site/guard map fed by /RadioCheckV2 handlers.
   Active & inactive guards, per-guard LB/KV/IR/SW/Fq, change toasts, PCAR tracking. */

(function () {
    'use strict';

    const REFRESH_SECONDS = 30;
    const COL = { ok: '#3ddc84', warn: '#ffb020', alarm: '#ff4d5e', off: '#5a6a80', hud: '#4fd8e8' };

    /* ---------------- map ---------------- */
    const map = L.map('map', { zoomControl: true, scrollWheelZoom: true, worldCopyJump: true })
        .setView([-27.0, 133.0], 5);

    const darkLayer = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap contributors &copy; CARTO', maxZoom: 20
    }).addTo(map);
    const streetLayer = L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap contributors &copy; CARTO', maxZoom: 20
    });
    L.control.layers({ 'Night Ops': darkLayer, 'Streets': streetLayer }, null, { position: 'topright' }).addTo(map);

    /* ---------------- state ---------------- */
    let sites = {};            // clientSiteId -> site model
    let markers = {};          // clientSiteId -> { site: L.Marker, cars: { guardId: L.Marker } }
    let prevGuards = null;     // "siteId:guardId" -> snapshot for diffing
    let selectedSiteId = null;
    let searchText = '';

    /* ---------------- helpers ---------------- */
    function stripHtml(input) {
        if (!input) return '';
        const doc = new DOMParser().parseFromString(input, 'text/html');
        return (doc.body.textContent || '').trim();
    }
    function esc(s) {
        return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
    function parseSiteName(raw) {
        const parts = (raw || '').split('&nbsp;');
        return { name: stripHtml(parts[0]), phone: stripHtml(parts.slice(1).join(' ')) };
    }
    function parseGps(gps) {
        if (!gps) return null;
        const p = gps.split(',').map(v => parseFloat(v));
        if (p.length < 2 || isNaN(p[0]) || isNaN(p[1])) return null;
        return p;
    }
    function guardStatus(g) {
        if (g.duress) return 'alarm';
        if (!g.active) return (g.twoHrAlert || '').toLowerCase() === 'red' ? 'alarm'
            : (g.twoHrAlert || '').toLowerCase() === 'yellow' ? 'warn' : 'off';
        return 'ok';
    }
    function siteStatus(s) {
        const st = s.guards.map(guardStatus);
        if (st.includes('alarm')) return 'alarm';
        if (st.includes('warn')) return 'warn';
        if (st.includes('ok')) return 'ok';
        return 'off';
    }
    function initials(name) {
        return (name || '?').split(/\s+/).map(w => w[0]).slice(0, 2).join('').toUpperCase();
    }

    /* ---------------- data loading ---------------- */
    function fetchJson(url) {
        return fetch(url, { headers: { 'Content-Type': 'application/json' } }).then(r => r.json());
    }

    function loadData() {
        return Promise.all([
            fetchJson('/RadioCheckV2?handler=ClientSiteActivityStatus&clientSiteIds='),
            fetchJson('/RadioCheckV2?handler=ClientSiteInActivityStatus&clientSiteIds=')
        ]).then(([active, inactive]) => {
            const model = {};
            const add = (rec, isActive) => {
                const id = rec.clientSiteId;
                if (!model[id]) {
                    const sn = parseSiteName(rec.siteName);
                    model[id] = {
                        id: id, name: sn.name, phone: sn.phone,
                        address: stripHtml(rec.address), gps: parseGps(rec.gps),
                        guards: []
                    };
                }
                const s = model[id];
                if (!s.gps) s.gps = parseGps(rec.gps);
                if (!s.phone) s.phone = parseSiteName(rec.siteName).phone;
                s.guards.push({
                    guardId: rec.guardId,
                    name: stripHtml(rec.guardName),
                    active: isActive,
                    lb: rec.logBook ?? null,
                    kv: rec.keyVehicle ?? null,
                    ir: rec.incidentReport ?? null,
                    sw: rec.smartWands ?? null,
                    fq: rec.completedRounds ?? null,
                    fqUnit: rec.patrolFqForDayOrHour || '',
                    rcStatus: rec.rcStatus ?? null,
                    status: rec.status || rec.lastEvent || '',
                    twoHrAlert: rec.twoHrAlert || '',
                    duress: rec.playDuressAlarm === 1,
                    tourMode: rec.tourMode || '',
                    gps: parseGps(rec.gps)
                });
            };
            (active || []).forEach(r => add(r, true));
            (inactive || []).forEach(r => add(r, false));
            return model;
        });
    }

    /* ---------------- marker icons ---------------- */
    function personSvg(color, size) {
        const s = size || 15;
        return `<svg width="${s}" height="${s * 1.25}" viewBox="0 0 14 18" style="filter:drop-shadow(0 0 3px ${color})">
            <circle cx="7" cy="3.6" r="3" fill="${color}"/>
            <path d="M2.2 17 Q2.2 8.6 7 8.6 Q11.8 8.6 11.8 17 Z" fill="${color}"/></svg>`;
    }
    function buildingSvg(active) {
        const f = active ? '#2c4a76' : '#2a3546';
        return `<svg class="bld" width="30" height="26" viewBox="0 0 30 26">
            <rect x="3" y="9" width="10" height="17" fill="${f}" rx="1"/>
            <rect x="14" y="2" width="13" height="24" fill="${f}" rx="1"/>
            ${[0, 1, 2].map(r => `<rect x="17" y="${5 + r * 7}" width="3" height="3" fill="#ffd68c" opacity=".85"/>
              <rect x="22" y="${5 + r * 7}" width="3" height="3" fill="#ffd68c" opacity=".55"/>`).join('')}
            <rect x="5.5" y="12" width="2.5" height="2.5" fill="#ffd68c" opacity=".5"/>
            <rect x="9.5" y="12" width="2.5" height="2.5" fill="#ffd68c" opacity=".7"/>
        </svg>`;
    }
    function carSvg(color) {
        return `<svg width="42" height="42" viewBox="0 0 64 64" style="filter:drop-shadow(0 0 5px ${color})">
            <path fill="${color}" d="M53 28h-2.1l-4.2-9.4A5 5 0 0 0 42.2 16H21.8a5 5 0 0 0-4.5 2.6L13.1 28H11a3 3 0 0 0-3 3v11a3 3 0 0 0 3 3h2a6 6 0 0 0 12 0h14a6 6 0 0 0 12 0h2a3 3 0 0 0 3-3V31a3 3 0 0 0-3-3zM21.8 20h20.4a1 1 0 0 1 .9.5l3.4 7.5H17.5l3.4-7.5a1 1 0 0 1 .9-.5zM19 48a2 2 0 1 1 0-4 2 2 0 0 1 0 4zm26 0a2 2 0 1 1 0-4 2 2 0 0 1 0 4z"/>
        </svg>`;
    }

    function siteIcon(s) {
        const status = siteStatus(s);
        const anyActive = s.guards.some(g => g.active);
        const staticGuards = s.guards.filter(g => g.tourMode !== 'PCAR');
        const shown = staticGuards.slice(0, 4);
        const extra = staticGuards.length - shown.length;
        const figs = shown.map(g => personSvg(COL[guardStatus(g)])).join('')
            + (extra > 0 ? `<span style="font:700 10px Consolas,monospace;color:#cdd9e6;align-self:flex-end">+${extra}</span>` : '');
        const shortName = s.name.length > 24 ? s.name.slice(0, 22) + '…' : s.name;
        return L.divIcon({
            className: '',
            html: `<div class="crm-site st-${status}${anyActive ? '' : ' inactive'}" data-site="${s.id}">
                     <span class="cnt">${s.guards.length}</span>
                     ${buildingSvg(anyActive)}
                     <div class="figs">${figs}</div>
                     <div class="nm">${esc(shortName)}</div>
                   </div>`,
            iconSize: [110, 66],
            iconAnchor: [55, 46],
            popupAnchor: [0, -40]
        });
    }
    function carIcon(g) {
        const status = guardStatus(g);
        return L.divIcon({
            className: '',
            html: `<div class="crm-site${g.active ? '' : ' inactive'}" data-site="${g.siteId}">
                     ${carSvg(COL[status])}
                     <div class="nm">${esc(g.name)}</div>
                   </div>`,
            iconSize: [90, 58],
            iconAnchor: [45, 34]
        });
    }

    /* ---------------- rendering ---------------- */
    function matchesSearch(s) {
        if (!searchText) return true;
        if (s.name.toLowerCase().includes(searchText)) return true;
        return s.guards.some(g => g.name.toLowerCase().includes(searchText));
    }

    function render() {
        const seenSites = {};
        let nActive = 0, nInactive = 0, nWarn = 0, nAlarm = 0;

        Object.values(sites).forEach(s => {
            s.guards.forEach(g => {
                if (g.active) nActive++; else nInactive++;
                const st = guardStatus(g);
                if (st === 'warn') nWarn++;
                if (st === 'alarm') nAlarm++;
            });

            if (!s.gps || !matchesSearch(s)) return;
            seenSites[s.id] = true;
            let entry = markers[s.id];
            if (!entry) { entry = markers[s.id] = { site: null, cars: {} }; }

            /* site marker (static guards + site itself) */
            if (!entry.site) {
                entry.site = L.marker(s.gps, { icon: siteIcon(s) }).addTo(map)
                    .on('click', () => openPanel(s.id));
            } else {
                entry.site.setIcon(siteIcon(s));
                entry.site.setLatLng(s.gps);
            }

            /* PCAR guards: own gliding car markers at their live GPS */
            const seenCars = {};
            s.guards.filter(g => g.tourMode === 'PCAR' && g.gps).forEach(g => {
                g.siteId = s.id;
                seenCars[g.guardId] = true;
                let car = entry.cars[g.guardId];
                if (!car) {
                    entry.cars[g.guardId] = L.marker(g.gps, { icon: carIcon(g) }).addTo(map)
                        .on('click', () => openPanel(s.id));
                } else {
                    car.setIcon(carIcon(g));
                    car.setLatLng(g.gps);   /* CSS transition makes it glide */
                }
            });
            Object.keys(entry.cars).forEach(gid => {
                if (!seenCars[gid]) { map.removeLayer(entry.cars[gid]); delete entry.cars[gid]; }
            });
        });

        /* drop markers for vanished / filtered-out sites */
        Object.keys(markers).forEach(id => {
            if (!seenSites[id]) {
                const e = markers[id];
                if (e.site) map.removeLayer(e.site);
                Object.values(e.cars).forEach(c => map.removeLayer(c));
                delete markers[id];
            }
        });

        document.getElementById('cntActive').textContent = nActive;
        document.getElementById('cntInactive').textContent = nInactive;
        document.getElementById('cntWarn').textContent = nWarn;
        document.getElementById('cntAlarm').textContent = nAlarm;
    }

    /* ---------------- change detection → blink + toast ---------------- */
    const WATCHED = [
        ['lb', 'LB'], ['kv', 'KV'], ['ir', 'IR'], ['sw', 'SW'],
        ['fq', 'Fq'], ['rcStatus', 'Radio Check'], ['active', 'Duty'], ['duress', 'DURESS']
    ];

    function snapshot(model) {
        const snap = {};
        Object.values(model).forEach(s => s.guards.forEach(g => {
            snap[s.id + ':' + g.guardId] = {
                site: s.name, guard: g.name, siteId: s.id,
                lb: g.lb, kv: g.kv, ir: g.ir, sw: g.sw, fq: g.fq,
                rcStatus: g.rcStatus, active: g.active, duress: g.duress,
                gps: g.gps ? g.gps.join(',') : ''
            };
        }));
        return snap;
    }

    function diffAndNotify(model) {
        const now = snapshot(model);
        if (prevGuards) {
            Object.keys(now).forEach(key => {
                const cur = now[key], old = prevGuards[key];
                if (!old) {
                    notify(cur, ['Logged in — now on site'], cur.duress ? 'alarm' : 'ok');
                    blinkSite(cur.siteId, cur.duress);
                    return;
                }
                const changes = [];
                WATCHED.forEach(([f, label]) => {
                    if (String(old[f]) !== String(cur[f])) {
                        if (f === 'active') changes.push(cur.active ? 'Came ON duty' : 'Went OFF duty');
                        else if (f === 'duress') { if (cur.duress) changes.push('DURESS ALARM'); }
                        else changes.push(label + ': ' + (old[f] ?? '-') + ' → ' + (cur[f] ?? '-'));
                    }
                });
                if (changes.length) {
                    const level = cur.duress ? 'alarm' : (changes.some(c => c.includes('OFF duty')) ? 'warn' : 'ok');
                    notify(cur, changes, level);
                    blinkSite(cur.siteId, cur.duress);
                }
            });
        }
        prevGuards = now;
    }

    function blinkSite(siteId, isAlarm) {
        const entry = markers[siteId];
        if (!entry) return;
        [entry.site, ...Object.values(entry.cars)].forEach(m => {
            if (!m || !m._icon) return;
            const el = m._icon.querySelector('.crm-site');
            if (!el) return;
            const cls = isAlarm ? 'blink-alarm' : 'blink';
            el.classList.remove('blink', 'blink-alarm');
            void el.offsetWidth;              /* restart animation */
            el.classList.add(cls);
        });
    }

    function notify(g, changes, level) {
        const box = document.getElementById('crmToasts');
        const t = document.createElement('div');
        t.className = 'crm-toast ' + (level === 'ok' ? '' : level);
        t.innerHTML = `<div class="t1"><span class="w">&#9679;</span>${esc(g.guard)}</div>
                       <div class="t2">${esc(g.site)}</div>
                       <div class="t3">${changes.map(esc).join('<br>')}</div>`;
        t.addEventListener('click', () => {
            const s = sites[g.siteId];
            if (s && s.gps) { map.flyTo(s.gps, Math.max(map.getZoom(), 15)); openPanel(g.siteId); }
            t.remove();
        });
        box.appendChild(t);
        while (box.children.length > 5) box.removeChild(box.firstChild);
        setTimeout(() => { t.style.opacity = '0'; t.style.transition = 'opacity .5s'; setTimeout(() => t.remove(), 550); },
            level === 'alarm' ? 20000 : 8000);
    }

    /* ---------------- site detail panel ---------------- */
    function chip(label, val) {
        if (val === null || val === undefined) return `<span class="crm-chip">${label} &ndash;</span>`;
        const ok = Number(val) > 0;
        return `<span class="crm-chip ${ok ? 'yes' : 'no'}">${label} ${ok ? '&#10003;' : '&#10007;'}${ok ? ' [' + val + ']' : ''}</span>`;
    }

    function openPanel(siteId) {
        const s = sites[siteId];
        if (!s) return;
        selectedSiteId = siteId;
        const panel = document.getElementById('crmPanel');

        const rows = s.guards.map(g => {
            const st = guardStatus(g);
            const tag = st === 'alarm' ? '<span class="tag alarm">DURESS</span>'
                : st === 'warn' ? '<span class="tag warn">2-HR</span>'
                : g.active ? '<span class="tag on">ON DUTY</span>'
                : '<span class="tag off">OFF</span>';
            const fq = g.fq !== null && g.fq !== undefined
                ? `<span class="crm-chip">Fq ${g.fq}${g.fqUnit ? ' ' + esc(g.fqUnit) : ''}</span>` : '';
            return `<div class="crm-grow">
                      <div class="top">
                        <span class="crm-ava" style="background:${COL[st]}">${esc(initials(g.name))}</span>
                        <span><b>${esc(g.name)}</b>${g.tourMode === 'PCAR' ? ' &#128663;' : ''}<br>
                        <span class="sub">${esc(g.status || (g.active ? 'Active' : 'Inactive'))}</span></span>
                        ${tag}
                      </div>
                      <div class="crm-chips">
                        ${chip('LB', g.lb)}${chip('KV', g.kv)}${chip('IR', g.ir)}${chip('SW', g.sw)}${fq}
                      </div>
                    </div>`;
        }).join('');

        panel.innerHTML = `
            <button type="button" class="close" onclick="document.getElementById('crmPanel').style.display='none'">&times;</button>
            <div class="ph" id="crmPh"><span class="cap">SITE PHOTO &middot; SITE SETTINGS</span></div>
            <div class="bd">
              <h2>${esc(s.name)}</h2>
              <div class="addr">${esc(s.address || '')}</div>
              <div class="phn">${esc(s.phone || '')}</div>
              <h3>GUARDS (${s.guards.length})</h3>
              ${rows}
            </div>`;
        panel.style.display = 'block';

        /* site image from ClientSiteKpiSetting.SiteImage */
        fetchJson('/ControlRoomMap?handler=SiteInfo&clientSiteId=' + siteId).then(info => {
            if (info && info.siteImage) {
                const ph = document.getElementById('crmPh');
                const img = document.createElement('img');
                img.src = info.siteImage;
                img.onerror = () => img.remove();
                ph.insertBefore(img, ph.firstChild);
            }
        }).catch(() => { });
    }

    /* ---------------- refresh loop (no page reload) ---------------- */
    let clock = REFRESH_SECONDS;
    const clockEl = document.getElementById('crmClock');
    setInterval(() => {
        clock--;
        if (clock <= 0) { clock = REFRESH_SECONDS; refresh(); }
        clockEl.textContent = '0:' + String(Math.max(clock, 0)).padStart(2, '0');
    }, 1000);

    let firstLoad = true;
    function refresh() {
        loadData().then(model => {
            sites = model;
            diffAndNotify(model);
            render();
            if (selectedSiteId && document.getElementById('crmPanel').style.display === 'block') {
                openPanel(selectedSiteId);   /* keep panel data live */
            }
            if (firstLoad) {
                firstLoad = false;
                const pts = Object.values(sites).filter(s => s.gps).map(s => s.gps);
                if (pts.length) map.fitBounds(L.latLngBounds(pts).pad(0.15));
            }
        }).catch(err => console.error('ControlRoomMap load error:', err));
    }

    document.getElementById('btnRefreshNow').addEventListener('click', () => { clock = REFRESH_SECONDS; refresh(); });
    document.getElementById('crmSearch').addEventListener('input', function () {
        searchText = this.value.trim().toLowerCase();
        render();
    });

    refresh();
})();
