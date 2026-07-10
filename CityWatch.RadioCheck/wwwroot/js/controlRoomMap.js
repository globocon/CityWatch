/* Control Room Map — enterprise live ops dashboard fed by /RadioCheckV2 handlers.
   Light theme, Australia-locked, clustered markers, autocomplete search, guard lists,
   diff-based change animations + toasts, SignalR-assisted refresh. Additive only:
   uses existing APIs (ClientSiteActivityStatus / ClientSiteInActivityStatus / SiteInfo). */

(function () {
    'use strict';

    const REFRESH_SECONDS = 30;
    const AU_BOUNDS = L.latLngBounds([-45.5, 111.0], [-8.8, 156.5]);
    const COL = { ok: '#16a34a', warn: '#d97706', alarm: '#dc2626', off: '#94a3b8', accent: '#2563eb' };

    /* ================= map: Australia only ================= */
    const map = L.map('map', {
        zoomControl: true,
        scrollWheelZoom: true,
        maxBounds: AU_BOUNDS.pad(0.08),
        maxBoundsViscosity: 1.0,
        minZoom: 4,
        maxZoom: 19,
        zoomAnimation: true
    }).fitBounds(AU_BOUNDS);

    const lightLayer = L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap contributors &copy; CARTO', maxZoom: 20
    }).addTo(map);
    const darkLayer = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap contributors &copy; CARTO', maxZoom: 20
    });
    L.control.layers({ 'Streets (light)': lightLayer, 'Night ops (dark)': darkLayer }, null, { position: 'topright' }).addTo(map);

    /* cluster group for site markers; separate glide layer for PCAR cars */
    const clusterGroup = L.markerClusterGroup({
        maxClusterRadius: 58,
        showCoverageOnHover: false,
        spiderfyOnMaxZoom: true,
        animate: true,
        iconCreateFunction: function (cluster) {
            let worst = 'off', guards = 0;
            cluster.getAllChildMarkers().forEach(m => {
                guards += m.options.crmGuards || 0;
                const s = m.options.crmStatus;
                if (s === 'alarm') worst = 'alarm';
                else if (s === 'warn' && worst !== 'alarm') worst = 'warn';
                else if (s === 'ok' && worst !== 'alarm' && worst !== 'warn') worst = 'ok';
            });
            const size = guards > 30 ? 56 : guards > 10 ? 50 : 44;
            return L.divIcon({
                className: '',
                html: `<div class="crm-cluster cl-${worst}" style="width:${size}px;height:${size}px"><b>${guards}</b><span>GUARDS</span></div>`,
                iconSize: [size, size]
            });
        }
    }).addTo(map);
    const carLayer = L.layerGroup().addTo(map);

    /* ================= state ================= */
    let sites = {};                 // clientSiteId -> site model
    let pcarRoutes = {};            // guardId -> { routeName, patrolCarName, nextSite, plannedTotal, visits[] }
    let markers = {};               // clientSiteId -> { site: L.Marker, iconSig, cars: {guardId: {m, iconSig}} }
    let prevGuards = null;          // diff snapshots
    let recentUpdates = {};         // "siteId:guardId" -> epoch ms of last observed change
    let selectedSiteId = null;
    let activeTab = 'active';
    let firstLoad = true;
    const filters = { status: 'all', siteId: null, region: 'all', updated: 'any', alert: 'all', fq: 'all', guardText: '' };

    /* ================= helpers ================= */
    function stripHtml(input) {
        if (!input) return '';
        const doc = new DOMParser().parseFromString(String(input), 'text/html');
        return (doc.body.textContent || '').replace(/ /g, ' ').replace(/\s+/g, ' ').trim();
    }
    function esc(s) {
        return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
    function hilite(text, q) {
        if (!q) return esc(text);
        const i = text.toLowerCase().indexOf(q.toLowerCase());
        if (i < 0) return esc(text);
        return esc(text.slice(0, i)) + '<mark>' + esc(text.slice(i, i + q.length)) + '</mark>' + esc(text.slice(i + q.length));
    }
    function parseSiteName(raw) {
        const parts = (raw || '').split('&nbsp;');
        return { name: stripHtml(parts[0]), phone: stripHtml(parts.slice(1).join(' ')) };
    }
    function parseGps(gps) {
        if (!gps) return null;
        const p = String(gps).split(',').map(v => parseFloat(v));
        if (p.length < 2 || isNaN(p[0]) || isNaN(p[1])) return null;
        return [p[0], p[1]];
    }
    function regionFromAddress(addr) {
        const m = /\b(VIC|NSW|QLD|WA|SA|NT|ACT|TAS)\b/i.exec(addr || '');
        return m ? m[1].toUpperCase() : '';
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
        return (name || '?').replace(/\[.*?\]/g, '').trim().split(/\s+/).map(w => w[0]).slice(0, 2).join('').toUpperCase() || '?';
    }
    function gKey(g) { return g.siteId + ':' + g.guardId; }
    function isRecent(g, mins) {
        const t = recentUpdates[gKey(g)];
        return t && (Date.now() - t) < mins * 60000;
    }
    function timeNow() {
        const d = new Date();
        return String(d.getHours()).padStart(2, '0') + ':' + String(d.getMinutes()).padStart(2, '0') + ':' + String(d.getSeconds()).padStart(2, '0');
    }
    function ago(ms) {
        const m = Math.floor(ms / 60000);
        if (m < 1) return 'just now';
        if (m < 60) return m + ' min';
        const h = Math.floor(m / 60);
        return h + ' hr ' + (m % 60) + ' min';
    }

    /* ---- HR record status (Green=Current, Orange=Pending, Yellow=Due, Red=Expired) ---- */
    const HR_MAP = {
        green: { cls: 'hr-green', dot: '#16a34a', label: 'CURRENT' },
        orange: { cls: 'hr-orange', dot: '#ea580c', label: 'PENDING' },
        yellow: { cls: 'hr-yellow', dot: '#d97706', label: 'DUE' },
        red: { cls: 'hr-red', dot: '#dc2626', label: 'EXPIRED' }
    };
    function hrInfo(val) {
        return HR_MAP[(val || '').toLowerCase()] || { cls: 'hr-grey', dot: '#94a3b8', label: 'N/A' };
    }
    function hrDots(g, title) {
        if (!g.active && !g.hr1 && !g.hr2 && !g.hr3) return '';
        return `<span class="hrdots" title="HR record status">${[g.hr1, g.hr2, g.hr3].map((v, i) =>
            `<span class="hrdot" style="background:${hrInfo(v).dot}" title="HR${i + 1}: ${hrInfo(v).label}"></span>`).join('')}</span>`;
    }
    function hrBadges(g) {
        return `<div class="hrbadges">${[g.hr1, g.hr2, g.hr3].map((v, i) => {
            const h = hrInfo(v);
            return `<span class="hrbadge ${h.cls}"><span class="hrdot" style="background:${h.dot}"></span>HR${i + 1} ${h.label}</span>`;
        }).join('')}</div>`;
    }

    /* ---- site patrol FQ: wands (DailyWandFq) and smartwands (completedRounds) combined ---- */
    function siteFqDone(s) {
        const swRounds = Math.max(0, ...s.guards.map(g => Number(g.fq) || 0));
        return Math.max(Number(s.wandFq) || 0, swRounds);
    }
    function siteFqAchieved(s) {
        return s.fqMin > 0 && siteFqDone(s) >= s.fqMin;
    }
    function fqBadge(s) {
        if (!s.fqMin) return '';
        const done = siteFqDone(s);
        return siteFqAchieved(s)
            ? `<span class="fq-badge done">&#10003; FQ ${done}/${s.fqMin}</span>`
            : `<span class="fq-badge pending">&#9203; FQ ${done}/${s.fqMin}</span>`;
    }

    /* ---- radio check status ---- */
    function rcState(g) {
        const c = (g.rcColor || (!g.active ? g.twoHrAlert : '') || '').toLowerCase();
        if (c === 'green') return { cls: 'on', label: 'ON TIME', dot: '#16a34a' };
        if (c === 'yellow') return { cls: 'warn', label: 'DUE SOON', dot: '#d97706' };
        if (c === 'red') return { cls: 'alarm', label: 'OVERDUE', dot: '#dc2626' };
        if (g.rcText) return { cls: 'warn', label: 'ATTENTION', dot: '#d97706' };
        return { cls: 'off', label: 'NO DATA', dot: '#94a3b8' };
    }

    /* ================= data ================= */
    function fetchJson(url) {
        return fetch(url, { headers: { 'Content-Type': 'application/json' } }).then(r => r.json());
    }

    function loadData() {
        return Promise.all([
            fetchJson('/RadioCheckV2?handler=ClientSiteActivityStatus&clientSiteIds='),
            fetchJson('/RadioCheckV2?handler=ClientSiteInActivityStatus&clientSiteIds='),
            fetchJson('/ControlRoomMap?handler=SiteFq').catch(() => []),
            fetchJson('/ControlRoomMap?handler=PcarRoutes').catch(() => [])
        ]).then(([active, inactive, siteFq, pcar]) => {
            const fqMap = {};
            (siteFq || []).forEach(f => { fqMap[f.clientSiteId] = { min: f.minFq || 0, wand: f.wandFq || 0 }; });

            /* PCAR routes keyed by guardId; visits get parsed GPS */
            const routes = {};
            (pcar || []).forEach(r => {
                r.visits = (r.visits || []).map(v => ({ ...v, pos: parseGps(v.gps) })).filter(v => v.pos);
                if (r.visits.length) routes[r.guardId] = r;
            });
            pcarRoutes = routes;
            const model = {};
            const add = (rec, isActive) => {
                const id = rec.clientSiteId;
                if (!model[id]) {
                    const sn = parseSiteName(rec.siteName);
                    const fq = fqMap[id] || { min: 0, wand: 0 };
                    model[id] = {
                        id: id, name: sn.name, phone: sn.phone,
                        address: stripHtml(rec.address), gps: parseGps(rec.gps),
                        region: rec.state || regionFromAddress(rec.address),
                        fqMin: fq.min, wandFq: fq.wand,
                        guards: []
                    };
                }
                const s = model[id];
                if (!s.gps) s.gps = parseGps(rec.gps);
                if (!s.phone) s.phone = parseSiteName(rec.siteName).phone;
                if (!s.region) s.region = rec.state || regionFromAddress(rec.address);
                s.guards.push({
                    siteId: id,
                    guardId: rec.guardId,
                    name: stripHtml(rec.guardName),
                    active: isActive,
                    lb: rec.logBook ?? null,
                    kv: rec.keyVehicle ?? null,
                    ir: rec.incidentReport ?? null,
                    sw: rec.smartWands ?? null,
                    fq: rec.completedRounds ?? null,
                    fqUnit: stripHtml(rec.patrolFqForDayOrHour),
                    rcStatus: rec.rcStatus ?? null,
                    rcColor: rec.rcColor || '',
                    rcText: stripHtml(isActive ? (rec.status || '') : (rec.rcStatus || '')),
                    hr1: rec.hR1 || '', hr2: rec.hR2 || '', hr3: rec.hR3 || '',
                    status: stripHtml(rec.status || rec.lastEvent || ''),
                    loginTime: stripHtml(rec.guardLoginTime || ''),
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

    /* ================= filtering ================= */
    function guardPasses(g) {
        if (filters.status === 'active' && !g.active) return false;
        if (filters.status === 'inactive' && g.active) return false;
        if (filters.alert !== 'all' && guardStatus(g) !== filters.alert) return false;
        if (filters.updated !== 'any' && !isRecent(g, parseInt(filters.updated, 10))) return false;
        if (filters.guardText && !g.name.toLowerCase().includes(filters.guardText)) return false;
        return true;
    }
    function sitePasses(s) {
        if (filters.siteId && s.id !== filters.siteId) return false;
        if (filters.region !== 'all' && s.region !== filters.region) return false;
        if (filters.fq === 'achieved' && !siteFqAchieved(s)) return false;
        if (filters.fq === 'notachieved' && !(s.fqMin > 0 && !siteFqAchieved(s))) return false;
        return s.guards.some(guardPasses);
    }
    function visibleSites() { return Object.values(sites).filter(sitePasses); }

    /* ================= marker icons ================= */
    function personSvg(color, size) {
        const s = size || 14;
        return `<svg width="${s}" height="${Math.round(s * 1.25)}" viewBox="0 0 14 18">
            <circle cx="7" cy="3.6" r="3" fill="${color}"/>
            <path d="M2.2 17 Q2.2 8.6 7 8.6 Q11.8 8.6 11.8 17 Z" fill="${color}"/></svg>`;
    }
    function buildingSvg(active) {
        const f = active ? '#3b5f8f' : '#8b99ab';
        return `<svg class="bld" width="28" height="24" viewBox="0 0 30 26">
            <rect x="3" y="9" width="10" height="17" fill="${f}" rx="1"/>
            <rect x="14" y="2" width="13" height="24" fill="${f}" rx="1"/>
            ${[0, 1, 2].map(r => `<rect x="17" y="${5 + r * 7}" width="3" height="3" fill="#ffe9b0"/>
              <rect x="22" y="${5 + r * 7}" width="3" height="3" fill="#ffe9b0" opacity=".6"/>`).join('')}
            <rect x="5.5" y="12" width="2.5" height="2.5" fill="#ffe9b0" opacity=".7"/>
        </svg>`;
    }
    function carSvg(color) {
        return `<svg width="40" height="40" viewBox="0 0 64 64" style="filter:drop-shadow(0 0 4px ${color})">
            <path fill="${color}" d="M53 28h-2.1l-4.2-9.4A5 5 0 0 0 42.2 16H21.8a5 5 0 0 0-4.5 2.6L13.1 28H11a3 3 0 0 0-3 3v11a3 3 0 0 0 3 3h2a6 6 0 0 0 12 0h14a6 6 0 0 0 12 0h2a3 3 0 0 0 3-3V31a3 3 0 0 0-3-3zM21.8 20h20.4a1 1 0 0 1 .9.5l3.4 7.5H17.5l3.4-7.5a1 1 0 0 1 .9-.5zM19 48a2 2 0 1 1 0-4 2 2 0 0 1 0 4zm26 0a2 2 0 1 1 0-4 2 2 0 0 1 0 4z"/>
        </svg>`;
    }

    function siteIconSig(s) {
        return siteStatus(s) + '|' + s.guards.map(g => guardStatus(g)).join('') + '|' +
            s.guards.some(g => isRecent(g, 2)) + '|' + s.name;
    }
    function siteIcon(s) {
        const status = siteStatus(s);
        const anyActive = s.guards.some(g => g.active);
        const recentPulse = s.guards.some(g => isRecent(g, 2));
        const staticGuards = s.guards.filter(g => g.tourMode !== 'PCAR');
        const shown = staticGuards.slice(0, 4);
        const extra = staticGuards.length - shown.length;
        const figs = shown.map(g => personSvg(COL[guardStatus(g)])).join('')
            + (extra > 0 ? `<span style="font-size:10px;font-weight:700;color:#475569;align-self:flex-end">+${extra}</span>` : '');
        const shortName = s.name.length > 24 ? s.name.slice(0, 22) + '…' : s.name;
        return L.divIcon({
            className: '',
            html: `<div class="crm-site st-${status}${anyActive ? '' : ' inactive'}${recentPulse ? ' pulse' : ''}" data-site="${s.id}">
                     <span class="cnt">${s.guards.length}</span>
                     ${buildingSvg(anyActive)}
                     <div class="figs">${figs}</div>
                     <div class="nm">${esc(shortName)}</div>
                   </div>`,
            iconSize: [110, 64],
            iconAnchor: [55, 44],
            popupAnchor: [0, -40]
        });
    }
    function carIconSig(g) { return guardStatus(g) + '|' + g.active + '|' + g.name; }
    function carIcon(g) {
        return L.divIcon({
            className: '',
            html: `<div class="crm-site${g.active ? '' : ' inactive'}${isRecent(g, 2) ? ' pulse' : ''}" data-site="${g.siteId}">
                     ${carSvg(COL[guardStatus(g)])}
                     <div class="nm">${esc(g.name)}</div>
                   </div>`,
            iconSize: [90, 56],
            iconAnchor: [45, 34]
        });
    }

    /* ================= rendering (incremental, no clear/redraw) ================= */
    function render() {
        const seenSites = {};

        visibleSites().forEach(s => {
            if (!s.gps || !AU_BOUNDS.contains(s.gps)) return;
            seenSites[s.id] = true;
            let entry = markers[s.id];
            if (!entry) entry = markers[s.id] = { site: null, iconSig: '', cars: {} };

            const sig = siteIconSig(s);
            if (!entry.site) {
                entry.site = L.marker(s.gps, { icon: siteIcon(s), crmStatus: siteStatus(s), crmGuards: s.guards.length })
                    .on('click', () => openPanel(s.id));
                entry.iconSig = sig;
                clusterGroup.addLayer(entry.site);
            } else {
                if (entry.iconSig !== sig) {          /* only touch DOM when something changed */
                    entry.site.setIcon(siteIcon(s));
                    entry.site.options.crmStatus = siteStatus(s);
                    entry.site.options.crmGuards = s.guards.length;
                    entry.iconSig = sig;
                    clusterGroup.refreshClusters(entry.site);
                }
                if (!entry.site.getLatLng().equals(L.latLng(s.gps))) entry.site.setLatLng(s.gps);
            }

            /* PCAR guards glide on their own layer (not clustered).
               Position = site of the most recent wand scan (live route), else feed GPS. */
            const seenCars = {};
            s.guards.filter(g => g.tourMode === 'PCAR' && guardPasses(g)).forEach(g => {
                const pos = pcarCurrentPos(g);
                if (!pos) return;
                seenCars[g.guardId] = true;
                let car = entry.cars[g.guardId];
                const csig = carIconSig(g);
                if (!car) {
                    const m = L.marker(pos, { icon: carIcon(g) }).on('click', () => openGuard(g));
                    carLayer.addLayer(m);
                    entry.cars[g.guardId] = { m: m, iconSig: csig };
                } else {
                    if (car.iconSig !== csig) { car.m.setIcon(carIcon(g)); car.iconSig = csig; }
                    car.m.setLatLng(pos);             /* CSS transition = smooth glide */
                }
            });
            Object.keys(entry.cars).forEach(gid => {
                if (!seenCars[gid]) { carLayer.removeLayer(entry.cars[gid].m); delete entry.cars[gid]; }
            });
        });

        Object.keys(markers).forEach(id => {
            if (!seenSites[id]) {
                const e = markers[id];
                if (e.site) clusterGroup.removeLayer(e.site);
                Object.values(e.cars).forEach(c => carLayer.removeLayer(c.m));
                delete markers[id];
            }
        });

        renderWidgets();
        renderGuardList();
        renderRegionOptions();
    }

    /* ================= KPI widgets ================= */
    let lastKpi = {};
    function setKpi(id, val) {
        const el = document.getElementById(id);
        if (!el) return;
        if (lastKpi[id] !== undefined && lastKpi[id] !== val) {
            el.classList.remove('bump'); void el.offsetWidth; el.classList.add('bump');
        }
        lastKpi[id] = val;
        el.textContent = val;
    }
    function renderWidgets() {
        let total = 0, act = 0, inact = 0, alarm = 0, recent = 0;
        const online = {};
        Object.values(sites).forEach(s => s.guards.forEach(g => {
            total++;
            if (g.active) { act++; online[s.id] = true; } else inact++;
            if (guardStatus(g) === 'alarm') alarm++;
            if (isRecent(g, 5)) recent++;
        }));
        setKpi('kpiTotal', total);
        setKpi('kpiActive', act);
        setKpi('kpiInactive', inact);
        setKpi('kpiSites', Object.keys(online).length);
        setKpi('kpiRecent', recent);
        setKpi('kpiAlarm', alarm);
    }

    /* ================= guard lists (Active / Inactive tabs) ================= */
    function allGuards() {
        const list = [];
        Object.values(sites).forEach(s => s.guards.forEach(g => list.push(g)));
        return list;
    }
    function renderGuardList() {
        const listEl = document.getElementById('crmList');
        const guards = allGuards()
            .filter(g => sitePasses(sites[g.siteId]) && guardPasses(g))
            .filter(g => activeTab === 'active' ? g.active : !g.active)
            .sort((a, b) => a.name.localeCompare(b.name));

        document.getElementById('tabActiveN').textContent =
            allGuards().filter(g => g.active && sitePasses(sites[g.siteId]) && guardPasses(g)).length;
        document.getElementById('tabInactiveN').textContent =
            allGuards().filter(g => !g.active && sitePasses(sites[g.siteId]) && guardPasses(g)).length;

        if (!guards.length) {
            listEl.innerHTML = '<div class="crm-empty">No guards match the current filters</div>';
            return;
        }
        listEl.innerHTML = guards.map(g => {
            const st = guardStatus(g);
            const s = sites[g.siteId];
            return `<div class="crm-g${isRecent(g, 5) ? ' recent' : ''}" data-g="${gKey(g)}">
                      <span class="crm-ava" style="background:${COL[st]}">${esc(initials(g.name))}</span>
                      <span class="gi"><b>${esc(g.name)}</b><span>${esc(s ? s.name : '')}</span></span>
                      ${hrDots(g)}
                      <span class="st" style="background:${COL[st]}"></span>
                    </div>`;
        }).join('');
        listEl.querySelectorAll('.crm-g').forEach(el => {
            el.addEventListener('click', () => {
                const g = findGuard(el.getAttribute('data-g'));
                if (g) openGuard(g, true);
            });
        });
    }
    function findGuard(key) {
        for (const s of Object.values(sites))
            for (const g of s.guards)
                if (gKey(g) === key) return g;
        return null;
    }

    /* ================= region filter options ================= */
    let regionsBuilt = '';
    function renderRegionOptions() {
        const regions = [...new Set(Object.values(sites).map(s => s.region).filter(Boolean))].sort();
        const sig = regions.join(',');
        if (sig === regionsBuilt) return;
        regionsBuilt = sig;
        const sel = document.getElementById('fltRegion');
        const cur = sel.value;
        sel.innerHTML = '<option value="all">All regions</option>' +
            regions.map(r => `<option value="${esc(r)}">${esc(r)}</option>`).join('');
        if ([...sel.options].some(o => o.value === cur)) sel.value = cur;
    }

    /* ================= change detection → animation + popup + toast ================= */
    const WATCHED = [
        ['lb', 'LB'], ['kv', 'KV'], ['ir', 'IR'], ['sw', 'SW'],
        ['fq', 'Fq'], ['rcStatus', 'Radio Check'], ['active', 'Duty'], ['duress', 'DURESS'], ['gps', 'Location']
    ];
    function snapshot(model) {
        const snap = {};
        Object.values(model).forEach(s => s.guards.forEach(g => {
            snap[gKey(g)] = {
                site: s.name, guard: g.name, siteId: s.id, guardId: g.guardId,
                lb: g.lb, kv: g.kv, ir: g.ir, sw: g.sw, fq: g.fq,
                rcStatus: g.rcStatus, active: g.active, duress: g.duress,
                gps: g.gps ? g.gps.map(v => v.toFixed(4)).join(',') : ''
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
                    recentUpdates[key] = Date.now();
                    announce(cur, ['Logged in — now on site'], cur.duress ? 'alarm' : 'ok');
                    return;
                }
                const changes = [];
                WATCHED.forEach(([f, label]) => {
                    if (String(old[f]) !== String(cur[f])) {
                        if (f === 'active') {
                            changes.push(cur.active ? 'Came ON duty' : 'Went OFF duty');
                            if (cur.active) { delete inactiveSince[key]; freshInactive.delete(key); }
                            else { inactiveSince[key] = Date.now(); bannerDismissed.delete(key); freshInactive.add(key); }
                        }
                        else if (f === 'duress') { if (cur.duress) changes.push('DURESS ALARM'); }
                        else if (f === 'gps') changes.push('Moved location');
                        else changes.push(label + ': ' + (old[f] ?? '–') + ' → ' + (cur[f] ?? '–'));
                    }
                });
                if (changes.length) {
                    recentUpdates[key] = Date.now();
                    const level = cur.duress ? 'alarm' : (changes.some(c => c.includes('OFF duty')) ? 'warn' : 'ok');
                    announce(cur, changes, level);
                }
            });
        }
        prevGuards = now;
    }

    function announce(g, changes, level) {
        blinkSite(g.siteId, level === 'alarm');
        toast(g, changes, level);
        miniPopup(g, changes, level);
    }

    function blinkSite(siteId, isAlarm) {
        const entry = markers[siteId];
        if (!entry || !entry.site) return;
        const targets = [entry.site, ...Object.values(entry.cars).map(c => c.m)];
        targets.forEach(m => {
            const el = m._icon && m._icon.querySelector('.crm-site');
            if (!el) return;
            const cls = isAlarm ? 'blink-alarm' : 'blink';
            el.classList.remove('blink', 'blink-alarm');
            void el.offsetWidth;
            el.classList.add(cls);
        });
    }

    /* small popup pinned at the marker, auto-dismisses */
    function miniPopup(g, changes, level) {
        const s = sites[g.siteId];
        if (!s || !s.gps) return;
        const pop = L.popup({
            className: 'crm-mini', closeButton: false, autoClose: true,
            autoPan: false, offset: [0, -38]
        })
            .setLatLng(s.gps)
            .setContent(`<div class="m1" style="color:${COL[level === 'ok' ? 'accent' : level]}">&#9679; ${esc(g.guard)}</div>
                         <div class="m2">${changes.map(esc).join('<br>')}</div>`)
            .openOn(map);
        setTimeout(() => { map.closePopup(pop); }, level === 'alarm' ? 12000 : 5500);
    }

    function toast(g, changes, level) {
        const box = document.getElementById('crmToasts');
        const t = document.createElement('div');
        t.className = 'crm-toast ' + (level === 'ok' ? '' : level);
        t.innerHTML = `<div class="t1"><span class="w">&#9679;</span>${esc(g.guard)}</div>
                       <div class="t2">${esc(g.site)}</div>
                       <div class="t3">${changes.map(esc).join('<br>')}</div>`;
        t.addEventListener('click', () => {
            const s = sites[g.siteId];
            if (s && s.gps) { map.flyTo(s.gps, Math.max(map.getZoom(), 15), { duration: 1.2 }); openPanel(g.siteId); }
            t.remove();
        });
        box.appendChild(t);
        while (box.children.length > 5) box.removeChild(box.firstChild);
        setTimeout(() => { t.style.opacity = '0'; setTimeout(() => t.remove(), 550); },
            level === 'alarm' ? 20000 : 8000);
    }

    /* ================= site panel ================= */
    function chip(label, val) {
        if (val === null || val === undefined) return `<span class="crm-chip">${label} &ndash;</span>`;
        const ok = Number(val) > 0;
        return `<span class="crm-chip ${ok ? 'yes' : 'no'}">${label} ${ok ? '&#10003; [' + val + ']' : '&#10007;'}</span>`;
    }
    function guardRow(g) {
        const st = guardStatus(g);
        const tag = st === 'alarm' ? '<span class="tag alarm">DURESS</span>'
            : st === 'warn' ? '<span class="tag warn">2-HR</span>'
            : g.active ? '<span class="tag on">ON DUTY</span>'
            : '<span class="tag off">OFF</span>';
        const fq = g.fq !== null && g.fq !== undefined
            ? `<span class="crm-chip">Fq ${g.fq}${g.fqUnit ? ' ' + esc(g.fqUnit) : ''}</span>` : '';
        const rc = rcState(g);
        return `<div class="crm-grow" data-g="${gKey(g)}">
                  <div class="top">
                    <span class="crm-ava" style="background:${COL[st]}">${esc(initials(g.name))}</span>
                    <span><b>${esc(g.name)}</b>${g.tourMode === 'PCAR' ? ' &#128663;' : ''} ${hrDots(g)}<br>
                    <span class="sub">${esc(g.status || (g.active ? 'Active' : 'Inactive'))}</span></span>
                    ${tag}
                  </div>
                  <div class="crm-chips">${chip('LB', g.lb)}${chip('KV', g.kv)}${chip('IR', g.ir)}${chip('SW', g.sw)}${fq}
                    <span class="tag ${rc.cls}" title="Radio check">RC ${rc.label}</span></div>
                </div>`;
    }

    function openPanel(siteId) {
        const s = sites[siteId];
        if (!s) return;
        selectedSiteId = siteId;
        const panel = document.getElementById('crmPanel');
        panel.innerHTML = `
            <button type="button" class="crm-close" id="crmPanelClose">&times;</button>
            <div class="ph" id="crmPh"><span class="cap">SITE PHOTO &middot; SITE SETTINGS</span></div>
            <div class="bd">
              <h2>${esc(s.name)}</h2>
              <div class="addr">&#128205; ${esc(s.address || '')}</div>
              <div class="phn">&#9742; ${esc(s.phone || '—')} ${fqBadge(s)}</div>
              <h3>GUARDS (${s.guards.length})</h3>
              ${s.guards.map(guardRow).join('')}
            </div>`;
        panel.style.display = 'block';
        panel.querySelector('#crmPanelClose').addEventListener('click', () => {
            panel.style.display = 'none'; selectedSiteId = null;
        });
        panel.querySelectorAll('.crm-grow').forEach(el => {
            el.addEventListener('click', () => {
                const g = findGuard(el.getAttribute('data-g'));
                if (g) openGuard(g);
            });
        });
        fetchJson('/ControlRoomMap?handler=SiteInfo&clientSiteId=' + siteId).then(info => {
            if (info && info.siteImage) {
                const ph = document.getElementById('crmPh');
                if (!ph) return;
                const img = document.createElement('img');
                img.src = info.siteImage;
                img.onerror = () => img.remove();
                ph.insertBefore(img, ph.firstChild);
            }
        }).catch(() => { });
    }

    /* ================= guard detail card ================= */
    let openGuardKey = null;
    function openGuard(g, fly) {
        const s = sites[g.siteId] || {};
        const st = guardStatus(g);
        const card = document.getElementById('crmGuard');
        const backdrop = document.getElementById('crmBackdrop');
        const upd = recentUpdates[gKey(g)];
        const rc = rcState(g);
        openGuardKey = gKey(g);

        /* PCAR patrol route section */
        const route = g.tourMode === 'PCAR' ? pcarRoutes[g.guardId] : null;
        let routeHtml = '';
        if (g.tourMode === 'PCAR') {
            if (route && route.visits.length) {
                const vs = route.visits;
                const cur = vs[vs.length - 1], prev = vs.length > 1 ? vs[vs.length - 2] : null;
                const first = vs[0];
                const durMs = new Date(cur.at) - new Date(first.at);
                const rows = vs.map((v, i) =>
                    `<div class="pcar-tl-row${i === vs.length - 1 ? ' cur' : ''}">
                       <span class="n" style="background:${i === 0 ? '#16a34a' : i === vs.length - 1 ? '#dc2626' : '#2563eb'}">${i === 0 ? 'S' : i + 1}</span>
                       <span class="t">${esc(visitTime(v))}</span>
                       <span class="s">${esc(v.siteName)}${i === 0 ? ' (Start)' : i === vs.length - 1 ? ' (Current)' : ''}</span>
                       <span class="w" title="Wand scan confirmed">&#10003;</span>
                     </div>`).join('')
                    + (route.nextSite ? `<div class="pcar-tl-row next">
                       <span class="n" style="background:#94a3b8">&#8594;</span>
                       <span class="t">next</span><span class="s">${esc(route.nextSite)} (Expected)</span><span class="w"></span></div>` : '');
                routeHtml = `
              <div class="rcbox pcarbox">
                <div class="rchead">
                  <h4>&#128663; PATROL ROUTE ${route.patrolCarName ? '&middot; ' + esc(route.patrolCarName) : ''}${route.routeName ? ' (' + esc(route.routeName) + ')' : ''}</h4>
                  <span class="tag on">${vs.length}${route.plannedTotal ? '/' + route.plannedTotal : ''} SITES</span>
                </div>
                <dl style="margin:0 0 8px;display:grid;grid-template-columns:104px 1fr;gap:4px 10px;font-size:12px">
                  <dt style="color:var(--text-dim)">Current site</dt><dd style="margin:0;font-weight:600">${esc(cur.siteName)}</dd>
                  <dt style="color:var(--text-dim)">Previous site</dt><dd style="margin:0;font-weight:600">${prev ? esc(prev.siteName) : '—'}</dd>
                  <dt style="color:var(--text-dim)">Next expected</dt><dd style="margin:0;font-weight:600">${route.nextSite ? esc(route.nextSite) : '—'}</dd>
                  <dt style="color:var(--text-dim)">Distance</dt><dd style="margin:0;font-weight:600">~${routeDistanceKm(route).toFixed(1)} km</dd>
                  <dt style="color:var(--text-dim)">Started</dt><dd style="margin:0;font-weight:600">${esc(visitTime(first))}</dd>
                  <dt style="color:var(--text-dim)">Last scan</dt><dd style="margin:0;font-weight:600">${esc(visitTime(cur))}</dd>
                  <dt style="color:var(--text-dim)">Duration</dt><dd style="margin:0;font-weight:600">${ago(durMs)}</dd>
                </dl>
                <div class="pcar-tl">${rows}</div>
                <div class="pcar-pb">
                  <button type="button" id="pbPlay" title="Replay route">&#9654;</button>
                  <button type="button" id="pbSpeed" title="Playback speed">${pb.speed}x</button>
                  <span id="pbInfo">Replay route</span>
                </div>
              </div>`;
            } else {
                routeHtml = `<div class="rcbox pcarbox"><div class="rchead"><h4>&#128663; PATROL ROUTE</h4></div>
                  <div class="rcmsg" style="color:var(--text-dim)">No wand-scan site visits recorded today yet. The route will appear here as sites are scanned.</div></div>`;
            }
        }
        const tag = st === 'alarm' ? '<span class="tag alarm">DURESS</span>'
            : st === 'warn' ? '<span class="tag warn">2-HR ALERT</span>'
            : g.active ? '<span class="tag on">ON DUTY</span>' : '<span class="tag off">OFF DUTY</span>';
        card.innerHTML = `
            <button type="button" class="crm-close" id="crmGuardClose">&times;</button>
            <div class="hd">
              <span class="crm-ava" style="background:${COL[st]}">${esc(initials(g.name))}</span>
              <span><h2>${esc(g.name)} ${g.tourMode === 'PCAR' ? '&#128663;' : ''}</h2>
              <div class="sub">${esc(s.name || '')} &middot; ${tag}</div></span>
            </div>
            <div class="bd">
              <dl>
                <dt>&#127970; Site</dt><dd>${esc(s.name || '—')}</dd>
                <dt>&#128205; Location</dt><dd>${esc(s.address || '—')}</dd>
                <dt>&#128225; Coordinates</dt><dd>${g.gps ? g.gps.map(v => v.toFixed(5)).join(', ') : (s.gps ? s.gps.map(v => v.toFixed(5)).join(', ') : '—')}</dd>
                <dt>&#9742; Site contact</dt><dd>${esc(s.phone || '—')}</dd>
                <dt>&#9201; Last activity</dt><dd>${esc(g.status || '—')}</dd>
                <dt>&#128337; Shift login</dt><dd>${esc(g.loginTime || (g.active ? 'On shift now' : '—'))}</dd>
                <dt>&#128260; Last update</dt><dd>${upd ? new Date(upd).toLocaleTimeString() : 'No change observed'}</dd>
                <dt>&#128663; Tour mode</dt><dd>${esc(g.tourMode || 'Static')}</dd>
              </dl>
              <div class="crm-chips">
                ${chip('LB', g.lb)}${chip('KV', g.kv)}${chip('IR', g.ir)}${chip('SW', g.sw)}
                ${g.fq !== null && g.fq !== undefined ? `<span class="crm-chip">Fq ${g.fq}${g.fqUnit ? ' ' + esc(g.fqUnit) : ''}</span>` : ''}
              </div>
              <div class="rcbox">
                <div class="rchead">
                  <h4>&#128225; RADIO CHECK</h4>
                  <span class="tag ${rc.cls}">${rc.label}</span>
                </div>
                ${g.rcText ? `<div class="rcmsg">${esc(g.rcText)}</div>` : '<div class="rcmsg" style="color:var(--text-dim)">No radio check message for this guard.</div>'}
                <div class="rcmeta">
                  Last change observed: <b>${upd ? new Date(upd).toLocaleTimeString() : '—'}</b>
                  ${g.active ? ' &middot; guard is on an active shift' : (g.loginTime ? ' &middot; expected/login: ' + esc(g.loginTime) : '')}
                </div>
              </div>
              <div class="rcbox">
                <div class="rchead"><h4>&#128100; HR RECORD STATUS</h4></div>
                ${hrBadges(g)}
              </div>
              ${routeHtml}
              <button type="button" class="locbtn" id="crmGuardFly">&#128205; Show on map</button>
            </div>`;
        backdrop.classList.add('open');
        card.classList.add('open');
        const close = () => {
            card.classList.remove('open'); backdrop.classList.remove('open'); openGuardKey = null;
            clearPcarRoute();
        };
        card.querySelector('#crmGuardClose').addEventListener('click', close);
        backdrop.onclick = close;
        card.querySelector('#crmGuardFly').addEventListener('click', () => {
            close();
            const pos = g.gps || s.gps;
            if (pos) { map.flyTo(pos, Math.max(map.getZoom(), 16), { duration: 1.2 }); openPanel(g.siteId); }
        });
        if (fly) {
            const pos = (g.tourMode === 'PCAR' ? pcarCurrentPos(g) : null) || g.gps || s.gps;
            if (pos) map.flyTo(pos, Math.max(map.getZoom(), 14), { duration: 1.2 });
        }

        /* PCAR: draw the live route on the map and wire the replay controls */
        if (g.tourMode === 'PCAR' && route && route.visits.length) {
            const firstDraw = shownRouteGuardId !== g.guardId;
            drawPcarRoute(g.guardId);
            if (firstDraw && route.visits.length > 1) {
                map.flyToBounds(L.latLngBounds(route.visits.map(v => v.pos)).pad(0.25), { duration: 1.2 });
            }
            const pbPlay = card.querySelector('#pbPlay');
            const pbSpeed = card.querySelector('#pbSpeed');
            if (pbPlay) pbPlay.addEventListener('click', () => {
                if (pb.playing) {                       /* pause */
                    pb.playing = false; clearTimeout(pb.timer);
                } else {                                /* play / resume */
                    pb.guardId = g.guardId;
                    pb.playing = true;
                    pbStep();
                }
                refreshPbUi();
            });
            if (pbSpeed) pbSpeed.addEventListener('click', () => {
                pb.speed = PB_SPEEDS[(PB_SPEEDS.indexOf(pb.speed) + 1) % PB_SPEEDS.length];
                refreshPbUi();
            });
        } else if (g.tourMode !== 'PCAR' && shownRouteGuardId !== null) {
            clearPcarRoute();
        }
    }

    /* ================= inactive guard alert banner ================= */
    const inactiveSince = {};        // key -> ms when we observed the guard go inactive
    const bannerDismissed = new Set();
    let bannerHidden = false;
    let bannerSig = '';
    const freshInactive = new Set(); // newly-inactive since last dismiss → flash their chip

    function sinceText(g) {
        const t = inactiveSince[gKey(g)];
        if (t) return ago(Date.now() - t) + ' ago';
        if (g.status) return g.status;               /* last event text from the feed */
        if (g.loginTime) return 'since ' + g.loginTime;
        return 'time unknown';
    }

    function renderBanner() {
        const banner = document.getElementById('crmBanner');
        const list = allGuards().filter(g => !g.active)
            .sort((a, b) => (inactiveSince[gKey(b)] || 0) - (inactiveSince[gKey(a)] || 0));

        if (!list.length) {
            banner.classList.remove('on'); document.body.classList.remove('has-banner');
            bannerSig = ''; return;
        }
        /* reappear automatically when a NEW guard becomes inactive */
        if (bannerHidden) {
            const hasNew = list.some(g => !bannerDismissed.has(gKey(g)));
            if (!hasNew) { banner.classList.remove('on'); document.body.classList.remove('has-banner'); return; }
            bannerHidden = false;
        }

        /* only touch the DOM when content actually changed (minute granularity) */
        const sig = list.map(g => gKey(g) + '|' + sinceText(g)).join(';');
        banner.classList.add('on');
        document.body.classList.add('has-banner');
        if (sig === bannerSig) return;
        bannerSig = sig;

        document.getElementById('crmBannerTitle').textContent =
            list.length + ' INACTIVE GUARD' + (list.length > 1 ? 'S' : '');
        document.getElementById('crmBannerChips').innerHTML = list.map(g => {
            const s = sites[g.siteId] || {};
            return `<span class="bchip${freshInactive.has(gKey(g)) ? ' fresh' : ''}" data-g="${gKey(g)}">
                      <b>${esc(g.name)}</b>
                      <span class="bs">${esc(s.name || '')}</span>
                      <span class="bt">&#9200; ${esc(sinceText(g))}</span>
                      ${s.phone ? `<span class="bp">&#9742; ${esc(s.phone)}</span>` : ''}
                    </span>`;
        }).join('');
        document.querySelectorAll('#crmBannerChips .bchip').forEach(el => {
            el.addEventListener('click', () => {
                const g = findGuard(el.getAttribute('data-g'));
                if (g) openGuard(g, true);
            });
        });
    }

    document.getElementById('crmBannerClose').addEventListener('click', () => {
        bannerHidden = true;
        freshInactive.clear();
        allGuards().filter(g => !g.active).forEach(g => bannerDismissed.add(gKey(g)));
        document.getElementById('crmBanner').classList.remove('on');
        document.body.classList.remove('has-banner');
    });

    /* tick the "time since inactive" labels every 5 seconds without flicker */
    setInterval(renderBanner, 5000);

    /* ================= FQ target achieved banner ================= */
    const fqDismissed = new Set();
    let fqHidden = false;
    let fqSig = '';
    let prevAchieved = new Set();      /* to toast + flash newly-achieved sites */
    const fqFresh = new Set();

    function renderFqBanner() {
        const banner = document.getElementById('crmFqBanner');
        const achieved = Object.values(sites).filter(siteFqAchieved)
            .sort((a, b) => a.name.localeCompare(b.name));

        /* announce sites that just reached their target */
        const nowSet = new Set(achieved.map(s => s.id));
        achieved.forEach(s => {
            if (prevAchieved.size && !prevAchieved.has(s.id)) {
                fqFresh.add(s.id);
                fqDismissed.delete(s.id);          /* new achiever → banner reappears */
                toast({ guard: s.name, site: 'Patrol frequency target reached', siteId: s.id },
                    ['FQ ' + siteFqDone(s) + '/' + s.fqMin + ' — minimum scans achieved'], 'ok');
            }
        });
        prevAchieved = nowSet;

        if (!achieved.length) {
            banner.classList.remove('on'); document.body.classList.remove('has-fqbanner');
            fqSig = ''; return;
        }
        if (fqHidden) {
            const hasNew = achieved.some(s => !fqDismissed.has(s.id));
            if (!hasNew) { banner.classList.remove('on'); document.body.classList.remove('has-fqbanner'); return; }
            fqHidden = false;
        }

        const sig = achieved.map(s => s.id + '|' + siteFqDone(s) + '/' + s.fqMin).join(';');
        banner.classList.add('on');
        document.body.classList.add('has-fqbanner');
        if (sig === fqSig) return;                 /* no flicker */
        fqSig = sig;

        document.getElementById('crmFqBannerTitle').textContent =
            achieved.length + ' SITE' + (achieved.length > 1 ? 'S' : '') + ' REACHED FQ TARGET';
        document.getElementById('crmFqBannerChips').innerHTML = achieved.map(s =>
            `<span class="fqchip${fqFresh.has(s.id) ? ' fresh' : ''}" data-s="${s.id}">
               <b>${esc(s.name)}</b>
               <span class="fqn">&#10003; ${siteFqDone(s)}/${s.fqMin} scans</span>
             </span>`).join('');
        document.querySelectorAll('#crmFqBannerChips .fqchip').forEach(el => {
            el.addEventListener('click', () => {
                const s = sites[parseInt(el.getAttribute('data-s'), 10)];
                if (s) {
                    if (s.gps) map.flyTo(s.gps, Math.max(map.getZoom(), 15), { duration: 1.2 });
                    openPanel(s.id);
                }
            });
        });
    }

    document.getElementById('crmFqBannerClose').addEventListener('click', () => {
        fqHidden = true;
        fqFresh.clear();
        Object.values(sites).filter(siteFqAchieved).forEach(s => fqDismissed.add(s.id));
        document.getElementById('crmFqBanner').classList.remove('on');
        document.body.classList.remove('has-fqbanner');
    });

    /* ================= autocomplete (guards + sites) ================= */
    function attachAutocomplete(inputId, suggId, provider, onPick) {
        const input = document.getElementById(inputId);
        const sugg = document.getElementById(suggId);
        let items = [], sel = -1;

        function close() { sugg.classList.remove('open'); sugg.innerHTML = ''; items = []; sel = -1; }
        function renderSugg() {
            if (!items.length) { close(); return; }
            sugg.innerHTML = items.map((it, i) =>
                `<div class="crm-sug${i === sel ? ' sel' : ''}" data-i="${i}">
                   <span class="dot2" style="background:${it.color}"></span>
                   <span class="main"><b>${it.html}</b><span>${esc(it.sub)}</span></span>
                   <span class="side ${it.side}">${it.sideText}</span>
                 </div>`).join('');
            sugg.classList.add('open');
            sugg.querySelectorAll('.crm-sug').forEach(el => {
                el.addEventListener('mousedown', e => {
                    e.preventDefault();
                    pick(parseInt(el.getAttribute('data-i'), 10));
                });
            });
        }
        function pick(i) {
            const it = items[i];
            if (!it) return;
            input.value = it.label;
            close();
            onPick(it);
        }
        input.addEventListener('input', () => {
            const q = input.value.trim();
            items = q ? provider(q).slice(0, 9) : [];
            sel = -1;
            renderSugg();
        });
        input.addEventListener('keydown', e => {
            if (!items.length) return;
            if (e.key === 'ArrowDown') { e.preventDefault(); sel = (sel + 1) % items.length; renderSugg(); }
            else if (e.key === 'ArrowUp') { e.preventDefault(); sel = (sel - 1 + items.length) % items.length; renderSugg(); }
            else if (e.key === 'Enter') { e.preventDefault(); pick(sel >= 0 ? sel : 0); }
            else if (e.key === 'Escape') close();
        });
        input.addEventListener('blur', () => setTimeout(close, 150));
        return { clear: () => { input.value = ''; close(); } };
    }

    const guardSearch = attachAutocomplete('srchGuard', 'suggGuard',
        q => allGuards()
            .filter(g => g.name.toLowerCase().includes(q.toLowerCase()))
            .sort((a, b) => Number(b.active) - Number(a.active) || a.name.localeCompare(b.name))
            .map(g => ({
                label: g.name,
                html: hilite(g.name, q),
                sub: (sites[g.siteId] || {}).name || '',
                color: COL[guardStatus(g)],
                side: g.active ? 'on' : 'off',
                sideText: g.active ? 'ACTIVE' : 'INACTIVE',
                g: g
            })),
        it => { openGuard(it.g, true); });

    const siteSearch = attachAutocomplete('srchSite', 'suggSite',
        q => Object.values(sites)
            .filter(s => s.name.toLowerCase().includes(q.toLowerCase()))
            .sort((a, b) => a.name.localeCompare(b.name))
            .map(s => ({
                label: s.name,
                html: hilite(s.name, q),
                sub: (s.address || '') + ' · ' + s.guards.length + ' guard' + (s.guards.length > 1 ? 's' : ''),
                color: COL[siteStatus(s)],
                side: 'site',
                sideText: s.guards.some(g => g.active) ? 'ONLINE' : 'OFFLINE',
                s: s
            })),
        it => {
            filters.siteId = it.s.id;           /* selecting a site filters guards to it */
            render();
            if (it.s.gps) map.flyTo(it.s.gps, 15, { duration: 1.2 });
            openPanel(it.s.id);
        });

    /* clearing the site box clears the site filter */
    document.getElementById('srchSite').addEventListener('input', function () {
        if (!this.value.trim() && filters.siteId) { filters.siteId = null; render(); }
    });
    document.getElementById('srchGuard').addEventListener('input', function () {
        filters.guardText = this.value.trim().toLowerCase();
        renderGuardList();
    });

    /* ================= filters wiring ================= */
    document.getElementById('fltStatus').addEventListener('change', function () { filters.status = this.value; render(); });
    document.getElementById('fltRegion').addEventListener('change', function () { filters.region = this.value; render(); });
    document.getElementById('fltUpdated').addEventListener('change', function () { filters.updated = this.value; render(); });
    document.getElementById('fltAlert').addEventListener('change', function () { filters.alert = this.value; render(); });
    document.getElementById('fltFq').addEventListener('change', function () { filters.fq = this.value; render(); });
    document.getElementById('btnResetFilters').addEventListener('click', () => {
        filters.status = 'all'; filters.region = 'all'; filters.updated = 'any';
        filters.alert = 'all'; filters.fq = 'all'; filters.siteId = null; filters.guardText = '';
        document.getElementById('fltStatus').value = 'all';
        document.getElementById('fltRegion').value = 'all';
        document.getElementById('fltUpdated').value = 'any';
        document.getElementById('fltAlert').value = 'all';
        document.getElementById('fltFq').value = 'all';
        guardSearch.clear(); siteSearch.clear();
        render();
    });

    /* tabs */
    document.querySelectorAll('.crm-tab').forEach(t => {
        t.addEventListener('click', () => {
            document.querySelectorAll('.crm-tab').forEach(x => x.classList.remove('on'));
            t.classList.add('on');
            activeTab = t.getAttribute('data-tab');
            renderGuardList();
        });
    });

    /* sidebar toggle */
    document.getElementById('btnSidebar').addEventListener('click', function () {
        document.getElementById('crmSidebar').classList.toggle('hidden');
        this.classList.toggle('closed');
    });

    /* ================= PCAR live route tracking ================= */
    const pcarPathLayer = L.layerGroup().addTo(map);
    let shownRouteGuardId = null;
    let shownRouteSig = '';
    let prevVisitCounts = {};       /* guardId -> visit count, to announce new scans */

    function pcarCurrentPos(g) {
        const r = pcarRoutes[g.guardId];
        if (r && r.visits.length) return r.visits[r.visits.length - 1].pos;
        return g.gps;
    }
    function haversineKm(a, b) {
        const R = 6371, dLa = (b[0] - a[0]) * Math.PI / 180, dLo = (b[1] - a[1]) * Math.PI / 180;
        const h = Math.sin(dLa / 2) ** 2 + Math.cos(a[0] * Math.PI / 180) * Math.cos(b[0] * Math.PI / 180) * Math.sin(dLo / 2) ** 2;
        return 2 * R * Math.asin(Math.sqrt(h));
    }
    function routeDistanceKm(r) {
        let d = 0;
        for (let i = 1; i < r.visits.length; i++) d += haversineKm(r.visits[i - 1].pos, r.visits[i].pos);
        return d;
    }
    function visitTime(v) {
        if (v.timeOn) return v.timeOn;
        const d = new Date(v.at);
        return String(d.getHours()).padStart(2, '0') + ':' + String(d.getMinutes()).padStart(2, '0');
    }
    function bearingDeg(a, b) {
        const y = Math.sin((b[1] - a[1]) * Math.PI / 180) * Math.cos(b[0] * Math.PI / 180);
        const x = Math.cos(a[0] * Math.PI / 180) * Math.sin(b[0] * Math.PI / 180)
            - Math.sin(a[0] * Math.PI / 180) * Math.cos(b[0] * Math.PI / 180) * Math.cos((b[1] - a[1]) * Math.PI / 180);
        return Math.atan2(y, x) * 180 / Math.PI;
    }
    function stopIcon(kind, n) {
        const col = kind === 'start' ? '#16a34a' : kind === 'current' ? '#dc2626' : '#2563eb';
        const label = kind === 'start' ? 'S' : n;
        return L.divIcon({
            className: '',
            html: `<div class="pcar-stop ${kind}" style="background:${col}">${label}</div>`,
            iconSize: [22, 22], iconAnchor: [11, 11]
        });
    }
    function arrowIcon(angle) {
        return L.divIcon({
            className: '',
            html: `<div class="pcar-arrow" style="transform:rotate(${angle}deg)">&#10148;</div>`,
            iconSize: [16, 16], iconAnchor: [8, 8]
        });
    }

    function routeSig(r) {
        return r ? r.visits.length + '|' + (r.visits.length ? r.visits[r.visits.length - 1].at : '') : '';
    }

    function drawPcarRoute(guardId) {
        const r = pcarRoutes[guardId];
        pcarPathLayer.clearLayers();
        shownRouteGuardId = guardId;
        shownRouteSig = routeSig(r);
        if (!r || r.visits.length === 0) return;

        const pts = r.visits.map(v => v.pos);

        /* completed segments: solid blue; active (latest) segment: dashed red */
        if (pts.length > 2)
            pcarPathLayer.addLayer(L.polyline(pts.slice(0, pts.length - 1),
                { color: '#2563eb', weight: 4, opacity: .85, lineJoin: 'round' }));
        if (pts.length > 1)
            pcarPathLayer.addLayer(L.polyline(pts.slice(pts.length - 2),
                { color: '#dc2626', weight: 4, opacity: .9, dashArray: '8 8', lineJoin: 'round' }));

        /* direction arrows at segment midpoints */
        for (let i = 1; i < pts.length; i++) {
            const mid = [(pts[i - 1][0] + pts[i][0]) / 2, (pts[i - 1][1] + pts[i][1]) / 2];
            pcarPathLayer.addLayer(L.marker(mid, {
                icon: arrowIcon(bearingDeg(pts[i - 1], pts[i]) - 90),
                interactive: false, keyboard: false
            }));
        }

        /* stops: green start, blue numbered, red pulsing current */
        r.visits.forEach((v, i) => {
            const kind = i === 0 ? 'start' : (i === r.visits.length - 1 ? 'current' : 'mid');
            pcarPathLayer.addLayer(L.marker(v.pos, { icon: stopIcon(kind, i + 1) })
                .bindTooltip(`${i + 1}. ${v.siteName} — ${visitTime(v)}`, { direction: 'top' }));
        });
    }
    function clearPcarRoute() {
        pcarPathLayer.clearLayers();
        shownRouteGuardId = null;
        shownRouteSig = '';
        stopPlayback();
    }

    /* announce new wand-scan arrivals + live-extend the drawn route */
    function pcarAfterRefresh() {
        Object.keys(pcarRoutes).forEach(gid => {
            const r = pcarRoutes[gid];
            const prev = prevVisitCounts[gid] || 0;
            if (prev && r.visits.length > prev) {
                const v = r.visits[r.visits.length - 1];
                const name = r.patrolCarName || r.routeName || 'Patrol car';
                toast({ guard: name, site: v.siteName, siteId: v.siteId },
                    ['Arrived — wand scan ' + visitTime(v)], 'ok');
                blinkSite(v.siteId, false);
            }
            prevVisitCounts[gid] = r.visits.length;
        });
        if (shownRouteGuardId !== null && routeSig(pcarRoutes[shownRouteGuardId]) !== shownRouteSig) {
            drawPcarRoute(shownRouteGuardId);   /* smooth extension: only redraws when a scan landed */
        }
    }

    /* ---------------- route playback (replay the patrol) ---------------- */
    const pb = { playing: false, idx: -1, speed: 1, timer: null, guardId: null, ghost: null };
    const PB_SPEEDS = [1, 2, 5];

    function stopPlayback() {
        pb.playing = false;
        clearTimeout(pb.timer);
        pb.idx = -1; pb.guardId = null;
        if (pb.ghost) { map.removeLayer(pb.ghost); pb.ghost = null; }
    }
    function pbStep() {
        if (!pb.playing) return;
        const r = pcarRoutes[pb.guardId];
        if (!r) { stopPlayback(); return; }
        pb.idx++;
        if (pb.idx >= r.visits.length) { stopPlayback(); refreshPbUi(); return; }
        const v = r.visits[pb.idx];
        if (!pb.ghost) {
            pb.ghost = L.marker(v.pos, {
                icon: L.divIcon({ className: '', html: '<div class="pcar-ghost">&#128663;</div>', iconSize: [30, 30], iconAnchor: [15, 15] }),
                interactive: false, zIndexOffset: 1000
            }).addTo(map);
        } else pb.ghost.setLatLng(v.pos);
        refreshPbUi();
        pb.timer = setTimeout(pbStep, 1400 / pb.speed);
    }
    function refreshPbUi() {
        const btn = document.getElementById('pbPlay');
        const spd = document.getElementById('pbSpeed');
        const lbl = document.getElementById('pbInfo');
        if (!btn) return;
        btn.innerHTML = pb.playing ? '&#10074;&#10074;' : '&#9654;';
        if (spd) spd.textContent = pb.speed + 'x';
        const r = pcarRoutes[pb.guardId];
        if (lbl) lbl.textContent = pb.playing && r && pb.idx >= 0 && pb.idx < r.visits.length
            ? (pb.idx + 1) + '/' + r.visits.length + ' ' + r.visits[pb.idx].siteName
            : 'Replay route';
        document.querySelectorAll('#crmGuard .pcar-tl-row').forEach((el, i) => {
            el.classList.toggle('playing', pb.playing && i === pb.idx);
        });
    }

    /* ================= auto site tour (play / pause / stop) ================= */
    const TOUR_DWELL_MS = 2000;        /* time spent looking at each site */
    const TOUR_FLY_SECONDS = 1.4;      /* flight time between sites */
    const tour = { ids: [], i: -1, playing: false, timer: null };

    function tourUi() {
        const play = document.getElementById('tourPlay');
        const stop = document.getElementById('tourStop');
        const info = document.getElementById('tourInfo');
        const box = document.getElementById('crmTourBox');
        play.innerHTML = tour.playing ? '&#10074;&#10074;' : '&#9654;';
        play.classList.toggle('playing', tour.playing);
        play.title = tour.playing ? 'Pause tour' : (tour.i >= 0 ? 'Resume tour' : 'Play site tour');
        stop.disabled = tour.i < 0 && !tour.playing;
        box.classList.toggle('touring', tour.playing);
        if (tour.i >= 0 && tour.i < tour.ids.length) {
            const s = sites[tour.ids[tour.i]];
            info.innerHTML = `<b>${tour.i + 1} / ${tour.ids.length}</b> ${esc(s ? (s.name.length > 20 ? s.name.slice(0, 18) + '…' : s.name) : '')}`;
        } else {
            info.textContent = 'Site tour';
        }
    }

    function tourStep() {
        if (!tour.playing) return;
        tour.i++;
        if (tour.i >= tour.ids.length) { tourEnd(true); return; }
        const s = sites[tour.ids[tour.i]];
        if (!s || !s.gps) { tourStep(); return; }     /* site vanished mid-tour → skip */
        map.flyTo(s.gps, 16, { duration: TOUR_FLY_SECONDS });
        openPanel(s.id);                               /* shows the site's guards */
        tourUi();
        tour.timer = setTimeout(tourStep, TOUR_FLY_SECONDS * 1000 + TOUR_DWELL_MS);
    }

    function tourEnd(completed) {
        tour.playing = false;
        clearTimeout(tour.timer);
        tour.i = -1; tour.ids = [];
        const panel = document.getElementById('crmPanel');
        panel.style.display = 'none'; selectedSiteId = null;
        const pts = Object.values(sites).filter(s => s.gps && AU_BOUNDS.contains(s.gps)).map(s => s.gps);
        if (pts.length) map.flyToBounds(L.latLngBounds(pts).pad(0.15), { duration: 1.2 });
        tourUi();
        if (completed) document.getElementById('tourInfo').textContent = 'Tour complete ✓';
    }

    document.getElementById('tourPlay').addEventListener('click', () => {
        if (tour.playing) {                            /* pause: stay on current site */
            tour.playing = false;
            clearTimeout(tour.timer);
        } else {
            if (tour.i < 0) {                          /* fresh start: tour what's on screen (filters respected) */
                tour.ids = visibleSites().filter(s => s.gps).sort((a, b) => a.name.localeCompare(b.name)).map(s => s.id);
                if (!tour.ids.length) return;
                tour.i = -1;
                tour.playing = true;
                tourStep();
                return;
            }
            tour.playing = true;                       /* resume: move on to the next site */
            tour.timer = setTimeout(tourStep, 400);
        }
        tourUi();
    });
    document.getElementById('tourStop').addEventListener('click', () => tourEnd(false));

    /* operator grabs the map → pause the tour instead of fighting them */
    map.on('dragstart', () => {
        if (tour.playing) {
            tour.playing = false;
            clearTimeout(tour.timer);
            tourUi();
        }
    });

    /* ================= SignalR: instant refresh on duress ================= */
    (function connectSignalR() {
        try {
            const url = (document.getElementById('txtSignalRConnectionUrl') || {}).value;
            if (!url || typeof signalR === 'undefined') return;
            const conn = new signalR.HubConnectionBuilder()
                .withUrl(url.replace(/\/$/, '') + '/updateHub')
                .withAutomaticReconnect()
                .build();
            conn.on('ReceiveDuressAlarmAlert', () => { refresh(); });
            conn.start().catch(() => { /* live polling still covers updates */ });
        } catch (e) { /* non-fatal */ }
    })();

    /* ================= refresh loop — background, view preserved ================= */
    let clock = REFRESH_SECONDS;
    const clockEl = document.getElementById('crmClock');
    setInterval(() => {
        clock--;
        if (clock <= 0) { clock = REFRESH_SECONDS; refresh(); }
        clockEl.textContent = '0:' + String(Math.max(clock, 0)).padStart(2, '0');
    }, 1000);

    /* fast change detection: poll a tiny token from the radio check activity table
       every CHANGE_POLL_SECONDS; when a guard inserts a new IR/LB/KV/SW record the
       token changes and the full refresh fires immediately (no hard refresh). */
    const CHANGE_POLL_SECONDS = 5;
    let lastChangeToken = null;
    setInterval(() => {
        fetchJson('/ControlRoomMap?handler=ChangeToken').then(r => {
            if (!r || !r.token) return;
            if (lastChangeToken !== null && r.token !== lastChangeToken) {
                clock = REFRESH_SECONDS;      /* reset the slow timer, we refresh now */
                refresh();
            }
            lastChangeToken = r.token;
        }).catch(() => { /* interval refresh still covers it */ });
    }, CHANGE_POLL_SECONDS * 1000);

    let refreshing = false;
    function refresh() {
        if (refreshing) return;                       /* minimise API calls */
        refreshing = true;
        loadData().then(model => {
            sites = model;
            diffAndNotify(model);
            render();                                  /* incremental: map view, zoom, filters untouched */
            renderBanner();
            renderFqBanner();
            pcarAfterRefresh();                        /* extend drawn routes + announce new scans */
            document.getElementById('kpiLast').textContent = timeNow();
            if (selectedSiteId && sites[selectedSiteId] &&
                document.getElementById('crmPanel').style.display === 'block') {
                openPanel(selectedSiteId);             /* keep open panel data live */
            }
            if (openGuardKey && !pb.playing) {         /* keep open guard card live (not mid-replay) */
                const g = findGuard(openGuardKey);
                if (g) openGuard(g); else { openGuardKey = null; }
            }
            if (firstLoad) {
                firstLoad = false;
                const pts = Object.values(sites).filter(s => s.gps && AU_BOUNDS.contains(s.gps)).map(s => s.gps);
                if (pts.length) map.fitBounds(L.latLngBounds(pts).pad(0.15));
            }
        }).catch(err => {
            console.error('ControlRoomMap load error:', err);
        }).finally(() => { refreshing = false; });
    }

    document.getElementById('btnRefreshNow').addEventListener('click', () => { clock = REFRESH_SECONDS; refresh(); });

    refresh();
})();
