/* CityWatch.Tracking — control room live layer (feature pack Phase 1, ADD §11 + Command
   Centre plan docs/CONTROL-ROOM-COMMAND-CENTRE-PLAN.md).
   Loaded ONLY when Tracking:Enabled; attaches to the existing map through window.CRM
   ({ map, carLayer, COL }) and never touches controlRoomMap.js internals. If this file
   throws, the existing map keeps working — everything here is additive to carLayer.

   Data path: same-origin authenticated poll of /api/tracking/live every 5 s.
   When a hub URL is configured the SignalR frames take over and the poll drops to a
   30 s consistency sweep.

   Phase 1 invariants:
   - A trail or replay line belongs to ONE session. Session change = new line, always.
   - Stale positions degrade visibly (fresh/soft/hollow/dead) — the map never lies.
   - The map is never covered by marker popups: details live in a docked card
     (bottom sheet on phones), and every control is finger-sized. */

(function () {
    'use strict';

    if (!window.CRM || !window.CRM.map || !window.CRM.carLayer) {
        console.warn('Tracking layer: window.CRM not available; layer disabled.');
        return;
    }

    const POLL_MS = 5000;
    const POLL_WITH_HUB_MS = 30000;
    const map = window.CRM.map;
    const layer = window.CRM.carLayer;      // replay artifacts live here — never toggled off

    /* Phase 4 layers: cars roam free; guards cluster by proximity so sixty people at one
       venue read as one badge, not sixty overlapping avatars. Trails follow their owners
       so the GUARDS toggle removes people and their breadcrumbs together. */
    function guardClusterIcon(cluster) {
        const n = cluster.getChildCount();
        return L.divIcon({
            className: '',
            html: `<div class="trk-gcluster">👮 ${n}</div>`,
            iconSize: [48, 30], iconAnchor: [24, 15]
        });
    }
    const carsGroup = L.layerGroup().addTo(map);
    const carTrailsGroup = L.layerGroup().addTo(map);
    const guardsGroup = (L.markerClusterGroup
        ? L.markerClusterGroup({
            maxClusterRadius: 48, disableClusteringAtZoom: 16,
            spiderfyOnMaxZoom: true, showCoverageOnHover: false,
            iconCreateFunction: guardClusterIcon
        })
        : L.layerGroup()).addTo(map);
    const guardTrailsGroup = L.layerGroup().addTo(map);

    const layerState = { cars: true, guards: true, sites: true, offline: readShowOffline() };
    function readShowOffline() {
        try { return localStorage.getItem('trkShowOffline') === '1'; } catch { return false; }
    }
    function applyLayerState() {
        const pairs = [
            ['cars', [carsGroup, carTrailsGroup]],
            ['guards', [guardsGroup, guardTrailsGroup]],
            ['sites', window.CRM.siteLayer ? [window.CRM.siteLayer] : []]
        ];
        pairs.forEach(([key, groups]) => groups.forEach(g => {
            if (layerState[key] && !map.hasLayer(g)) map.addLayer(g);
            if (!layerState[key] && map.hasLayer(g)) map.removeLayer(g);
        }));
        /* 'offline' is not a layer group — it's a per-unit visibility rule. */
        try { localStorage.setItem('trkShowOffline', layerState.offline ? '1' : '0'); } catch { /* private mode */ }
        refreshOfflineVisibility();
        document.querySelectorAll('[data-trk-layer]').forEach(b =>
            b.classList.toggle('off', !layerState[b.getAttribute('data-trk-layer')]));
    }

    /* Staleness thresholds (ADD §11.3 rule 2): the map must never render an old
       position as current. */
    const FRESH_S = 30, SOFT_S = 120, HOLLOW_S = 300;

    /* Beyond this a unit is yesterday's leftover, not a mid-shift quiet spell: hidden
       from the map by default. The ⚪ Offline chip reveals them, search always lists
       them, and a hidden unit opened from search shows itself while its card is up.
       The server reaper expires truly abandoned sessions; this only tidies the view. */
    const OFFLINE_HIDE_S = 4 * 3600;

    function isHiddenOffline(u) {
        return !layerState.offline && u.unitId !== selectedUnitId && u.ageSeconds > OFFLINE_HIDE_S;
    }
    function applyOfflineVisibility(entry) {
        const hide = isHiddenOffline(entry.data);
        if (hide === !!entry.offlineHidden) return;
        entry.offlineHidden = hide;
        if (hide) {
            entry.markerGroup.removeLayer(entry.marker);
            if (entry.trailCasing) entry.trailGroup.removeLayer(entry.trailCasing);
            entry.trailGroup.removeLayer(entry.trail);
        } else {
            entry.markerGroup.addLayer(entry.marker);
            if (entry.trailCasing) entry.trailGroup.addLayer(entry.trailCasing);   // casing under the line
            entry.trailGroup.addLayer(entry.trail);
        }
    }
    function refreshOfflineVisibility() {
        Object.values(units).forEach(applyOfflineVisibility);
    }

    /* A move this large between consecutive fixes is a data jump (flagged Implausible
       server-side), not driving — snap, never animate the vehicle across the map. */
    const GLIDE_MAX_KM = 3;

    /* Mode → accent. Colour still means urgency: duress uses the existing alarm red. */
    const MODE = {
        1: { label: 'NORMAL', cls: 'trk-normal', color: '#16a34a' },
        2: { label: 'TRANSIT', cls: 'trk-transit', color: '#2563eb' },
        3: { label: 'LIVE', cls: 'trk-live', color: '#2563eb' },
        4: { label: 'DURESS', cls: 'trk-duress', color: '#dc2626' }
    };

    const units = {};        // unitId -> { marker, trail, data, lastSeenMs }
    const idleUnits = {};    // unitId -> idleMinutes (refreshed by the idle poll)

    function esc(s) {
        return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }

    function ageBucket(ageS) {
        if (ageS <= FRESH_S) return 'fresh';
        if (ageS <= SOFT_S) return 'soft';
        if (ageS <= HOLLOW_S) return 'hollow';
        return 'dead';
    }

    /* "301m" reads as metres. Durations are always humanised: 41m, 5h 1m. */
    function fmtMins(min) {
        min = Math.max(0, Math.round(min));
        if (min < 60) return min + 'm';
        return Math.floor(min / 60) + 'h ' + (min % 60) + 'm';
    }
    function fmtAge(s) {
        if (s < 120) return Math.round(s) + 's';
        return fmtMins(s / 60);
    }
    function compass(deg) {
        if (deg == null) return '';
        return ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'][Math.round(((deg % 360) + 360) % 360 / 45) % 8];
    }
    function haversineKm(aLat, aLon, bLat, bLon) {
        const R = 6371, dLa = (bLat - aLat) * Math.PI / 180, dLo = (bLon - aLon) * Math.PI / 180;
        const h = Math.sin(dLa / 2) ** 2 + Math.cos(aLat * Math.PI / 180) * Math.cos(bLat * Math.PI / 180) * Math.sin(dLo / 2) ** 2;
        return 2 * R * Math.asin(Math.sqrt(h));
    }

    /* Identity of a tracked unit — ONE naming system per screen: the radio CALLSIGN the
       officer logged in with ("R3"), because that is the name the control room already
       uses everywhere else. The car from the login Position ("Mobile Patrols (Car) M1"
       -> "M1") is only the fallback when no callsign was entered, then the guard's name.
       Field report 18 Aug: car-name-first put "M1" beside "R1" on the same map and the
       operator could not match markers to the RC screen. The car stays visible in the
       asset card and in search. */
    function shortCar(name) {
        if (!name) return null;
        const m = String(name).match(/\(car\)\s*([A-Z]{0,2}\d+)/i);
        return m ? m[1] : String(name).replace(/mobile patrols\s*/i, '').replace(/[()]/g, '').trim();
    }
    function unitLabel(u) {
        return u.callsign || shortCar(u.patrolCar)
            || (u.kind !== 'guard' ? `PC-${u.unitId}`
                : (u.guardName ? u.guardName.split(' ')[0] : `G-${u.guardId || u.unitId}`));
    }
    function initialsOf(name) {
        return (name || '?').replace(/\[.*?\]/g, '').trim().split(/\s+/)
            .map(w => w[0]).slice(0, 2).join('').toUpperCase() || '?';
    }

    /* Honest status: an idle unit is stopped, whatever the NFC travel state says —
       "in transit + idle 95m" was two truths fighting; stopped-with-context wins.
       Guards get people language (§4.2): moving / stationary, never "in transit". */
    function statusLine(u) {
        const idleMin = idleUnits[u.unitId];
        if (u.kind === 'guard') {
            if (idleMin) return `⏸ Stationary ${fmtMins(idleMin)}${u.currentSite ? ' · ' + esc(u.currentSite) : ''}`;
            if (u.ageSeconds > HOLLOW_S) return `⚪ No recent fix (${fmtAge(u.ageSeconds)} ago)`;
            if (u.speedKph != null && u.speedKph >= 2) return `🚶 Moving${u.currentSite ? ' · ' + esc(u.currentSite) : ''}`;
            return u.currentSite ? `📍 At ${esc(u.currentSite)}` : '🟢 On duty';
        }
        if (idleMin) {
            return `⏸ Stopped ${fmtMins(idleMin)}${u.currentSite ? ' · ' + esc(u.currentSite) : ''}`;
        }
        const mins = u.stateMinutes ? ' ' + fmtMins(u.stateMinutes) : '';
        return u.travelState === 'AtSite' && u.currentSite
            ? `📍 At ${esc(u.currentSite)}${mins}`
            : `🚙 In transit${mins}`;
    }

    /* ================= markers: a car that looks like a car ================= */

    /* Side-view vehicle (field feedback, 12 Aug: "make it look like a car"). One solid
       mode-coloured body, thick white outline, dark glass, two wheels — bold shapes that
       still read as a car at 24 px. Drawn facing RIGHT; westbound headings flip it
       (a side view cannot rotate with heading the way the old top-down sprite did). */
    function carSvg(color) {
        return `<svg viewBox="0 0 48 26" width="44" height="24" aria-hidden="true">
            <path d="M3 16.5 Q3 11.5 8.5 10.5 L13.5 5.8 Q14.8 4.2 17 4.2 L28.5 4.2 Q30.8 4.2 32.4 5.8 L36.8 10.3 Q43.2 11 45 13.4 Q45.7 14.5 45.7 16 L45.7 17 Q45.7 19 43.7 19 L5 19 Q3 19 3 16.5 Z"
                  fill="${color}" stroke="#ffffff" stroke-width="2.2" stroke-linejoin="round"/>
            <path d="M17.2 6 L27.5 6 L27.5 9.8 L15 9.8 Z" fill="#0f172a" opacity=".82"/>
            <path d="M29.5 6 Q30.3 6 30.9 6.6 L34 9.8 L29.5 9.8 Z" fill="#0f172a" opacity=".82"/>
            <circle cx="12.5" cy="19" r="4.6" fill="#1e293b" stroke="#ffffff" stroke-width="1.6"/>
            <circle cx="35.5" cy="19" r="4.6" fill="#1e293b" stroke="#ffffff" stroke-width="1.6"/>
            <circle cx="12.5" cy="19" r="1.7" fill="#cbd5e1"/>
            <circle cx="35.5" cy="19" r="1.7" fill="#cbd5e1"/>
        </svg>`;
    }

    /* The sprite faces right; anything heading west (180–360°) faces left. */
    function facesLeft(headingDeg) {
        if (headingDeg == null) return false;
        return (((headingDeg % 360) + 360) % 360) > 180;
    }

    function unitIcon(u) {
        const mode = MODE[u.mode] || MODE[1];
        const bucket = ageBucket(u.ageSeconds);
        const isCar = u.kind !== 'guard';
        const label = unitLabel(u);
        const idleMin = idleUnits[u.unitId];
        const ageTxt = u.ageSeconds <= FRESH_S ? '' :
            `<span class="trk-age">${fmtAge(u.ageSeconds)}</span>`;
        const idleTxt = idleMin ? `<span class="trk-idle-badge">⏸ ${fmtMins(idleMin)}</span>` : '';
        const sel = selectedUnitId === u.unitId || follow.unitId === u.unitId ? ' trk-sel' : '';

        let body;
        if (isCar) {
            body = `<div class="trk-sprite${facesLeft(u.headingDeg) ? ' trk-flip' : ''}">${carSvg(mode.color)}</div>`;
        } else {
            /* Guards: solid state-coloured circle, white ring, white initials — the
               Google-Maps person-dot idiom. No heading (foot heading is GPS noise). */
            const fill = idleMin ? '#d97706' : mode.color;
            body = `<div class="trk-avatar" style="background:${fill}">${esc(initialsOf(u.guardName))}</div>`;
        }
        return L.divIcon({
            className: 'trk-marker',
            html: `<div class="trk-unit ${isCar ? 'trk-kind-car' : 'trk-kind-guard'} ${mode.cls} trk-${bucket}${sel}">
                     ${body}
                     <span class="trk-id">${esc(label)}</span>${ageTxt}${idleTxt}
                   </div>`,
            iconSize: [56, 68],
            iconAnchor: [28, isCar ? 14 : 26]   /* the sprite's centre — the vehicle, not the label */
        });
    }

    /* Everything that changes the icon's DOM, EXCEPT heading. Heading is applied as a
       class toggle so the sprite flips through its CSS transition; rebuilding innerHTML
       on every poll would snap the flip and churn the DOM for nothing. */
    function iconSig(u) {
        const sel = selectedUnitId === u.unitId || follow.unitId === u.unitId;
        return [u.mode, ageBucket(u.ageSeconds), u.kind, unitLabel(u),
            idleUnits[u.unitId] || 0, sel,
            u.ageSeconds <= FRESH_S ? '' : fmtAge(u.ageSeconds)].join('|');
    }

    function applyIcon(entry, force) {
        const u = entry.data;
        const sig = iconSig(u);
        if (force || entry.iconSig !== sig) {
            entry.marker.setIcon(unitIcon(u));
            entry.iconSig = sig;
        } else if (u.kind !== 'guard' && u.headingDeg != null && entry.marker._icon) {
            const sprite = entry.marker._icon.querySelector('.trk-sprite');
            if (sprite) sprite.classList.toggle('trk-flip', facesLeft(u.headingDeg));
        }
    }

    /* ================= selection, card, follow ================= */

    let selectedUnitId = null;
    let selectedSite = null;       // card shows a SITE (who's here) instead of a unit

    const follow = { unitId: null, suspended: false };

    function followBar() {
        let el = document.getElementById('trkFollowBar');
        if (!el) {
            el = document.createElement('div');
            el.id = 'trkFollowBar';
            el.className = 'trk-follow-bar';
            document.body.appendChild(el);
        }
        return el;
    }

    function renderFollowBar() {
        const el = followBar();
        if (!follow.unitId) { el.style.display = 'none'; return; }
        const u = units[follow.unitId] && units[follow.unitId].data;
        const label = u ? unitLabel(u) : follow.unitId;
        el.style.display = 'flex';
        el.innerHTML = follow.suspended
            ? `<span>⏸ Paused — ${esc(label)}</span>
               <button data-trk-resume="1">RESUME</button>
               <button data-trk-unfollow="1" class="stop">STOP</button>`
            : `<span>◉ FOLLOWING ${esc(label)}</span>
               <button data-trk-unfollow="1" class="stop">STOP</button>`;
    }

    function startFollow(unitId) {
        follow.unitId = unitId;
        follow.suspended = false;
        const entry = units[unitId];
        if (entry) {
            map.flyTo(entry.marker.getLatLng(), Math.max(map.getZoom(), 14), { duration: 1 });
            refreshIcon(entry);
        }
        renderFollowBar();
        renderCard();
    }
    function stopFollow() {
        const prev = follow.unitId;
        follow.unitId = null;
        follow.suspended = false;
        if (prev && units[prev]) refreshIcon(units[prev]);
        renderFollowBar();
        renderCard();
    }

    /* The operator's pan wins: pause following, offer RESUME, never fight the map. */
    map.on('dragstart', () => {
        if (follow.unitId && !follow.suspended) {
            follow.suspended = true;
            renderFollowBar();
        }
    });

    function cardEl() {
        let el = document.getElementById('trkCard');
        if (!el) {
            el = document.createElement('div');
            el.id = 'trkCard';
            el.className = 'trk-card';
            document.body.appendChild(el);
        }
        return el;
    }

    function openCard(unitId) {
        if (selectedUnitId !== unitId) guardIdOpen = false;   // a new card starts folded
        selectedUnitId = unitId;
        selectedSite = null;
        document.body.classList.add('trk-card-open');
        if (units[unitId]) refreshIcon(units[unitId]);
        refreshOfflineVisibility();   // an offline-hidden unit shows itself while its card is up
        renderCard();
    }
    /* The PCAR↔Site↔Guard pivot (§4.7): a site card lists every tracked asset currently
       at that site; tapping one lands on its unit card, one tap from FOLLOW. */
    function openSiteCard(siteName) {
        const prev = selectedUnitId;
        selectedUnitId = null;
        selectedSite = siteName;
        document.body.classList.add('trk-card-open');
        if (prev && units[prev]) refreshIcon(units[prev]);
        renderCard();
    }
    function closeCard() {
        const prev = selectedUnitId;
        selectedUnitId = null;
        selectedSite = null;
        document.body.classList.remove('trk-card-open');
        if (prev && units[prev]) refreshIcon(units[prev]);
        refreshOfflineVisibility();   // card closed: the offline unit hides again
        renderCard();
    }

    /* Street addresses (§2.1): resolved through the server's cached geocoder, cached again
       per ~110 m cell here so an open card costs at most one request per street. The card
       renders without waiting — an address is decoration, never a dependency. */
    const addrCache = {};      // "cellLat:cellLon" -> address string | null (null = known miss)
    const addrPending = {};

    function addrKey(u) {
        return Math.floor(u.lat * 1000) + ':' + Math.floor(u.lon * 1000);
    }

    function addressFor(u) {
        const key = addrKey(u);
        if (key in addrCache) return addrCache[key];
        if (!addrPending[key]) {
            addrPending[key] = true;
            fetch(`/api/tracking/address?lat=${u.lat}&lon=${u.lon}`, { credentials: 'same-origin' })
                .then(r => r.ok ? r.json() : { address: null })
                .then(b => { addrCache[key] = b.address || null; })
                .catch(() => { addrCache[key] = null; })
                .finally(() => {
                    delete addrPending[key];
                    if (selectedUnitId === u.unitId) renderCard();   // repaint once it arrives
                });
        }
        return undefined;      // unknown yet — distinct from a known miss
    }

    /* ================= security intelligence (Phase 5) ================= */

    /* Alert feed: the exceptions surface, so nobody has to stare at the map all day.
       Every rule below runs on data the poll already carries — nothing is invented.
       (Route-DELAY alerts are deliberately absent: planned routes carry no expected
       times, and a made-up threshold would cry wolf. Documented, not faked.) */
    const alerts = [];             // {t, level: 'info'|'warn'|'alarm', msg, unitId}
    let alertsUnseen = 0;
    let alertPanelOpen = false;

    /* Site arrivals are DIFFERENT from the transient alerts above: they come from the
       server's TrackingSiteVisit record, so they survive a refresh, are identical on every
       operator's screen, and include arrivals that happened while no browser was open.
       The transient alerts are this browser's observations; the arrivals are the record. */
    let serverArrivals = [];       // newest first, straight from /api/tracking/arrivals
    let arrivalsUnseen = 0;
    /* Seen-tracking is by EVENT TIME, not row id: one visit produces an "entered" line and
       later a "left" line on the same row — an id watermark would swallow the departure. */
    const ARR_SEEN_KEY = 'trkArrSeenTs';
    function arrSeenTs() { try { return Number(localStorage.getItem(ARR_SEEN_KEY)) || 0; } catch { return 0; } }
    function setArrSeenTs(ts) { try { localStorage.setItem(ARR_SEEN_KEY, String(ts)); } catch { /* private mode */ } }

    /* Server times defensively normalised: datetime2 round-trips without a zone marker,
       and new Date() would silently read that as LOCAL time. */
    function utcDate(v) {
        if (typeof v === 'string' && !/(Z|[+-]\d\d:?\d\d)$/.test(v)) return new Date(v + 'Z');
        return new Date(v);
    }

    function addAlert(level, msg, unitId) {
        const last = alerts[alerts.length - 1];
        if (last && last.msg === msg && Date.now() - last.t < 120000) return;   // burst dedupe
        alerts.push({ t: Date.now(), level, msg, unitId });
        while (alerts.length > 60) alerts.shift();
        alertsUnseen++;
        updateAlertBadge();
        if (alertPanelOpen) renderAlertPanel();
    }

    function updateAlertBadge() {
        const b = document.getElementById('trkBellBadge');
        if (b) {
            const unseen = alertsUnseen + arrivalsUnseen;
            b.textContent = unseen > 9 ? '9+' : String(unseen);
            b.style.display = unseen ? 'flex' : 'none';
        }
    }

    function alertPanelEl() {
        let el = document.getElementById('trkAlerts');
        if (!el) {
            el = document.createElement('div');
            el.id = 'trkAlerts';
            el.className = 'trk-alert-panel';
            document.body.appendChild(el);
        }
        return el;
    }

    function arrivalEvents(a) {
        /* One visit, up to two bell lines: the NFC site scan is "entered the site", the
           in-car dashboard scan is "left the site" — exactly the officer's own actions. */
        const who = esc(a.label || ('Unit ' + a.unitId));
        const guard = a.guardName ? ` · ${esc(a.guardName)}` : '';
        const how = a.source === 'Nfc' ? ' · tagged' : '';
        const events = [{
            t: utcDate(a.confirmedUtc).getTime(),
            level: 'info',
            unitId: a.unitId,
            msg: `📍 <b>${who}</b> entered <b>${esc(a.siteName)}</b>${guard}${how}`
        }];
        if (a.exitedUtc) {
            events.push({
                t: utcDate(a.exitedUtc).getTime(),
                level: 'info',
                unitId: a.unitId,
                msg: `🚗 <b>${who}</b> left <b>${esc(a.siteName)}</b>${guard} · after ${fmtMins(Math.max(1, a.minutesOnSite))}`
            });
        }
        return events;
    }

    function allArrivalEvents() { return serverArrivals.flatMap(arrivalEvents); }

    function toggleAlerts(show) {
        alertPanelOpen = show ?? !alertPanelOpen;
        if (alertPanelOpen) {
            alertsUnseen = 0;
            arrivalsUnseen = 0;
            const evs = allArrivalEvents();
            if (evs.length) setArrSeenTs(Math.max(arrSeenTs(), ...evs.map(e => e.t)));
            updateAlertBadge();
            renderAlertPanel();
        }
        alertPanelEl().classList.toggle('open', alertPanelOpen);
    }

    function renderAlertPanel() {
        const el = alertPanelEl();
        const merged = [...alerts, ...allArrivalEvents()].sort((x, y) => x.t - y.t);
        const rows = merged.reverse().map(a => `
            <div class="trk-alert-row ${a.level}" ${a.unitId && units[a.unitId] ? `data-trk-open="${a.unitId}"` : ''}>
              <span class="t">${new Date(a.t).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
              <span class="m">${a.msg}</span>
            </div>`).join('');
        el.innerHTML = `
            <div class="trk-alert-head"><b>⚠ ATTENTION (${merged.length})</b>
              <button class="trk-card-close" data-trk-alerts-close="1" aria-label="Close">×</button></div>
            <div class="trk-alert-body">${rows || '<div class="trk-alert-empty">Nothing needs attention. 🎉</div>'}</div>`;
    }

    /* The arrivals poll. 30 s is plenty: the dwell window means an arrival is already
       minutes old by the time it is confirmable, and the record is server-side either way. */
    async function pollArrivals() {
        try {
            const res = await fetch('/api/tracking/arrivals', { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            const body = await res.json();
            serverArrivals = body.arrivals || [];
            const seen = arrSeenTs();
            const fresh = allArrivalEvents().filter(e => e.t > seen);
            if (alertPanelOpen) {
                /* Panel is open: the operator is looking at it — new rows appear in place
                   and count as seen. */
                if (fresh.length) setArrSeenTs(Math.max(seen, ...fresh.map(e => e.t)));
                renderAlertPanel();
            } else {
                arrivalsUnseen = fresh.length;
                updateAlertBadge();
            }
        } catch {
            /* keep the last list; the live poll's health pill covers outages */
        } finally {
            setTimeout(pollArrivals, 30000);
        }
    }

    /* Planned vs actual (§5.2): the wand-scan route data the base map already loads,
       refreshed once a minute — no second data model. */
    let pcarPlans = {};            // guardId -> { planned:[{siteId,siteName}], visits:[...], patrolCarName }
    async function pollPlans() {
        try {
            const res = await fetch('/ControlRoomMap?handler=PcarRoutes', { credentials: 'same-origin' });
            if (res.ok) {
                const list = await res.json();
                const m = {};
                (list || []).forEach(r => { if (r && r.guardId != null) m[r.guardId] = r; });
                pcarPlans = m;
            }
        } catch { /* plan panel simply stays empty */ }
        finally { setTimeout(pollPlans, 60000); }
    }
    pollPlans();

    function planHtml(u) {
        const plan = u.kind !== 'guard' && u.guardId ? pcarPlans[u.guardId] : null;
        if (!plan || !plan.planned || !plan.planned.length) return '';
        const visitedIds = new Set((plan.visits || []).map(v => v.siteId));
        const lastVisit = (plan.visits || [])[plan.visits.length - 1];
        const currentIdx = plan.planned.findIndex(p => lastVisit && p.siteId === lastVisit.siteId);
        const rows = plan.planned.map((p, i) => {
            let cls = 'todo', mark = '·';
            if (visitedIds.has(p.siteId)) { cls = 'done'; mark = '✓'; }
            else if (currentIdx >= 0 && i < currentIdx) { cls = 'missed'; mark = '✗'; }
            const cur = lastVisit && p.siteId === lastVisit.siteId ? ' cur' : '';
            return `<div class="trk-plan-row ${cls}${cur}"><span class="mk">${mark}</span>${esc(p.siteName || ('Site ' + p.siteId))}${cls === 'missed' ? '<span class="miss">MISSED</span>' : ''}</div>`;
        });
        const extras = (plan.visits || []).filter(v => !plan.planned.some(p => p.siteId === v.siteId))
            .map(v => `<div class="trk-plan-row extra"><span class="mk">+</span>${esc(v.siteName)}<span class="unexp">UNEXPECTED</span></div>`);
        const done = plan.planned.filter(p => visitedIds.has(p.siteId)).length;
        return `<div class="trk-card-section">PATROL PLAN · ${done}/${plan.planned.length} VISITED</div>
                <div class="trk-plan">${rows.join('')}${extras.join('')}</div>`;
    }

    /* Patrol performance (§5.3): re-read from the existing TrackSegments roll-ups —
       segments close with their sessions, so an in-progress patrol shows what has
       already been rolled up and says so. */
    const perfCache = {};          // unitId -> { t, html }
    function perfFor(u) {
        const hit = perfCache[u.unitId];
        if (hit && Date.now() - hit.t < 60000) return hit.html;
        if (hit && hit.pending) return hit.html || '';
        perfCache[u.unitId] = { t: Date.now(), html: hit ? hit.html : '', pending: true };
        const from = new Date(); from.setHours(0, 0, 0, 0);
        fetch(`/api/tracking/segments?unitId=${u.unitId}&fromUtc=${from.toISOString()}&toUtc=${new Date().toISOString()}`,
            { credentials: 'same-origin' })
            .then(r => r.ok ? r.json() : [])
            .then(segments => {
                let html = '';
                if (segments && segments.length) {
                    const km = segments.reduce((s, x) => s + (x.distanceM || 0), 0) / 1000;
                    const sec = segments.reduce((s, x) => s + (x.durationSec || 0), 0);
                    const scans = segments.reduce((s, x) => s + (x.anchorScanCount || 0), 0);
                    const maxSpeed = Math.max(0, ...segments.map(x => x.maxSpeedKph || 0));
                    html = `<div class="trk-card-section">TODAY (COMPLETED PATROLS)</div>
                        <div class="trk-row">📏 ${km.toFixed(1)} km · ⏱ ${fmtMins(sec / 60)} · 🔝 ${maxSpeed} km/h${scans ? ` · ✓ ${scans} scans` : ''}</div>`;
                }
                perfCache[u.unitId] = { t: Date.now(), html };
                if (selectedUnitId === u.unitId) renderCard();
            })
            .catch(() => { perfCache[u.unitId] = { t: Date.now(), html: '' }; });
        return perfCache[u.unitId].html || '';
    }

    /* Duress intelligence (§5.4): who is closest, right now, with one-tap pivot. */
    function respondersHtml(u) {
        if (u.mode !== 4) return '';
        const nearest = Object.values(units).map(e => e.data)
            .filter(o => o.unitId !== u.unitId && o.ageSeconds <= HOLLOW_S)
            .map(o => ({ o, km: haversineKm(Number(u.lat), Number(u.lon), Number(o.lat), Number(o.lon)) }))
            .sort((a, b) => a.km - b.km)
            .slice(0, 3);
        if (!nearest.length) return '';
        return `<div class="trk-card-section">NEAREST RESPONDERS</div>` + nearest.map(({ o, km }) => `
            <div class="trk-search-row" data-trk-open="${o.unitId}">
              <span class="g">${o.kind !== 'guard' ? '🚓' : '👮'}</span>
              <span class="m"><b>${esc(unitLabel(o))}</b><span>${km < 1 ? Math.round(km * 1000) + ' m' : km.toFixed(1) + ' km'} away</span></span>
              <span class="trk-mode-chip trk-transit">GO ›</span>
            </div>`).join('');
    }

    const liveRequests = {};   // unitId -> epoch ms of an unacknowledged "Track Live" click

    function liveButtonHtml(u) {
        /* Never claim a state the device has not confirmed (§11.3 rule 5):
           requested-but-unacked shows as "requested…", not as LIVE. */
        if (u.mode === 4) return '';                        // duress owns the unit
        if (u.mode === 3)
            return `<button class="trk-btn trk-btn-stop" data-trk-stop="${u.unitId}">⏹ Stop Live</button>`;
        if (liveRequests[u.unitId] && Date.now() - liveRequests[u.unitId] < 90000)
            return `<span class="trk-pending">◉ Live requested…</span>`;
        return `<button class="trk-btn" data-trk-live="${u.unitId}">◉ Track Live</button>`;
    }

    /* The docked asset card: everything the popup used to say, without covering the
       map. Right panel on desktop, bottom sheet on phones (CSS decides). */
    function renderSiteCard() {
        const el = cardEl();
        const here = Object.values(units).map(e => e.data).filter(u => u.currentSite === selectedSite);
        const cars = here.filter(u => u.kind !== 'guard');
        const guards = here.filter(u => u.kind === 'guard');
        const row = u => {
            const mode = MODE[u.mode] || MODE[1];
            return `<div class="trk-search-row" data-trk-open="${u.unitId}">
                      <span class="g">${u.kind !== 'guard' ? '🚓' : `<span class="trk-avatar trk-avatar-sm" style="border-color:${mode.color}">${esc(initialsOf(u.guardName))}</span>`}</span>
                      <span class="m"><b>${esc(unitLabel(u))}</b><span>${statusLine(u)}</span></span>
                      <span class="trk-mode-chip ${mode.cls}">${mode.label}</span>
                    </div>`;
        };
        el.classList.add('open');
        el.innerHTML = `
            <div class="trk-card-head">
              <span class="trk-card-glyph" style="font-size:22px">🏢</span>
              <span class="trk-card-title"><b>${esc(selectedSite)}</b><span>${here.length ? here.length + ' tracked here now' : 'nobody tracked here right now'}</span></span>
              <button class="trk-card-close" data-trk-card-close="1" aria-label="Close">×</button>
            </div>
            <div class="trk-card-body">
              ${cars.length ? `<div class="trk-card-section">PATROL CARS (${cars.length})</div>` + cars.map(row).join('') : ''}
              ${guards.length ? `<div class="trk-card-section">GUARDS (${guards.length})</div>` + guards.map(row).join('') : ''}
              ${!here.length ? '<div class="trk-row dim">Assets appear here while their NFC scans place them at this site.</div>' : ''}
            </div>`;
    }

    /* #153 Part 2: with a hundred Muhammads on the books a truncated first name
       identifies nobody. The card wears the guard's ID the way the guard wears the
       physical one — full name and licence number always in view on a badge strip,
       contact details one tap behind it. (The HR pin is an HR credential, not an
       identity: it never reaches this payload.) */
    let guardIdOpen = false;

    function guardIdHtml(u, isCar) {
        if (!u.guardName && !u.guardLicense) return '';
        const licence = u.guardLicense ? esc(u.guardLicense) : '<span class="dim">no licence on file</span>';
        const state = u.guardState ? `<span class="trk-id-state">${esc(u.guardState)}</span>` : '';
        const tel = u.guardMobile ? String(u.guardMobile).replace(/[^+\d]/g, '') : null;
        const missing = what => `<span class="dim">no ${what} on file</span>`;
        /* A guard card's head already headlines the full name (wrapped, never clipped);
           a car card headlines the callsign, so the badge names its officer. */
        const officer = isCar && u.guardName
            ? `<span class="trk-idbadge-name">${esc(u.guardName)}</span>` : '';
        return `
            <div class="trk-idbadge">
              <div class="trk-idbadge-main">
                ${officer}
                <span class="trk-idbadge-lic">🆔 ${licence} ${state}</span>
              </div>
              <button class="trk-idbadge-more" data-trk-guardid="1" aria-expanded="${guardIdOpen}">${guardIdOpen ? 'Hide ▴' : 'Contact ▾'}</button>
            </div>
            ${guardIdOpen ? `
              <div class="trk-row trk-id-row">📞 ${tel ? `<a class="trk-idlink" href="tel:${esc(tel)}">${esc(u.guardMobile)}</a>` : missing('mobile')}</div>
              <div class="trk-row trk-id-row">✉ ${u.guardEmail ? `<a class="trk-idlink" href="mailto:${esc(u.guardEmail)}">${esc(u.guardEmail)}</a>` : missing('email')}</div>` : ''}`;
    }

    function renderCard() {
        if (selectedSite) { renderSiteCard(); return; }
        const el = cardEl();
        const entry = selectedUnitId && units[selectedUnitId];
        if (!entry) {
            el.classList.remove('open');
            return;
        }
        const u = entry.data;
        const mode = MODE[u.mode] || MODE[1];
        const isCar = u.kind !== 'guard';
        /* Same rule as unitLabel: callsign headlines, the car itself moves to the
           subtitle — the card must answer to the same name as its marker. */
        const title = isCar
            ? (u.callsign || u.patrolCar || `Unit ${u.unitId}`)
            : (u.guardName || `Guard ${u.guardId || u.unitId}`);
        /* Derived speed is approximate and says so; measured speed is plain. */
        const speed = u.speedKph == null ? '—' : `${u.speedDerived ? '~' : ''}${u.speedKph} km/h`;
        const dir = u.headingDeg == null ? '' : ` <span class="dim">${compass(u.headingDeg)} ↗</span>`;
        /* Location line: street address → site name (statusLine) → coordinates. */
        const addr = addressFor(u);
        const locationRow = addr
            ? `<div class="trk-row">📍 ${esc(addr)}</div>`
            : (addr === null && !u.currentSite
                ? `<div class="trk-row dim">📍 ${Number(u.lat).toFixed(5)}, ${Number(u.lon).toFixed(5)}</div>`
                : '');
        const sessionSince = u.sessionStartedUtc
            ? utcDate(u.sessionStartedUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : null;
        const following = follow.unitId === u.unitId;

        el.classList.add('open');
        el.innerHTML = `
            <div class="trk-card-head">
              <span class="trk-card-glyph">${isCar ? carSvg(mode.color) : `<div class="trk-avatar trk-avatar-lg" style="border-color:${mode.color}">${esc(initialsOf(u.guardName))}</div>`}</span>
              <span class="trk-card-title">
                <b${isCar ? '' : ' class="trk-title-wrap"'}>${esc(title)}</b>
                <span>${u.callsign && u.patrolCar ? esc(u.patrolCar) + ' · ' : ''}${isCar ? 'Patrol Car' : 'Guard'}</span>
              </span>
              <span class="trk-mode-chip ${mode.cls}">${mode.label}</span>
              <button class="trk-card-close" data-trk-card-close="1" aria-label="Close">×</button>
            </div>
            <div class="trk-card-body">
              ${guardIdHtml(u, isCar)}
              <div class="trk-row trk-row-state">${statusLine(u)}</div>
              ${locationRow}
              ${u.currentSite ? `<button class="trk-sitelink" data-trk-site="${esc(u.currentSite)}">🏢 ${esc(u.currentSite)} — who's here ›</button>` : ''}
              <div class="trk-row">🚀 ${speed}${dir} &nbsp; ${u.accuracyM == null ? '' : `±${u.accuracyM}m`} ${u.batteryPct == null ? '' : `&nbsp; 🔋${u.batteryPct}%`}</div>
              <div class="trk-row dim">Fix ${fmtAge(u.ageSeconds)} ago${sessionSince ? ` · on shift since ${sessionSince}` : ''}</div>
              ${respondersHtml(u)}
              ${planHtml(u)}
              ${perfFor(u)}
            </div>
            <div class="trk-card-actions">
              <button class="trk-btn ${following ? 'trk-btn-on' : ''}" data-trk-follow="${u.unitId}">${following ? '◉ FOLLOWING' : '◉ FOLLOW'}</button>
              <button class="trk-btn trk-btn-replay" data-trk-replay="${u.unitId}">▶ Replay</button>
              ${liveButtonHtml(u)}
              <button class="trk-btn" data-trk-ping="${u.unitId}" title="Ask the phone for a fresh position right now">📳 Ping</button>
              <button class="trk-btn" data-trk-msg="${u.unitId}" title="Send a text message to this unit's phone">✉ Message</button>
            </div>`;
    }

    /* ================= search: find any tracked asset instantly ================= */

    function searchOverlay() {
        let el = document.getElementById('trkSearch');
        if (!el) {
            el = document.createElement('div');
            el.id = 'trkSearch';
            el.className = 'trk-search';
            el.innerHTML = `
                <div class="trk-search-box">
                  <input id="trkSearchInput" type="search" placeholder="Find patrol car, callsign or guard…"
                         autocomplete="off" enterkeyhint="search">
                  <button class="trk-search-close" data-trk-search-close="1" aria-label="Close">×</button>
                </div>
                <div id="trkSearchResults" class="trk-search-results"></div>`;
            document.body.appendChild(el);
            document.getElementById('trkSearchInput').addEventListener('input', renderSearchResults);
            document.getElementById('trkSearchInput').addEventListener('keydown', e => {
                if (e.key === 'Escape') toggleSearch(false);
                if (e.key === 'Enter') {
                    const first = document.querySelector('#trkSearchResults [data-trk-open]');
                    if (first) first.click();
                }
            });
        }
        return el;
    }

    function toggleSearch(show) {
        const el = searchOverlay();
        el.classList.toggle('open', show);
        if (show) {
            document.getElementById('trkSearchInput').value = '';
            renderSearchResults();
            setTimeout(() => document.getElementById('trkSearchInput').focus(), 50);
        }
    }

    function searchRow(u) {
        const mode = MODE[u.mode] || MODE[1];
        const isCar = u.kind !== 'guard';
        const online = u.ageSeconds <= HOLLOW_S;
        return `<div class="trk-search-row" data-trk-open="${u.unitId}">
                  <span class="g">${isCar ? '🚓' : `<span class="trk-avatar trk-avatar-sm" style="border-color:${mode.color}">${esc(initialsOf(u.guardName))}</span>`}</span>
                  <span class="m"><b>${esc(unitLabel(u))}</b><span>${esc(isCar ? (u.guardName || 'Patrol car') : 'Guard')}${u.currentSite ? ' · ' + esc(u.currentSite) : ''} · ${online ? '' : 'offline '}${fmtAge(u.ageSeconds)} ago</span></span>
                  <span class="trk-mode-chip ${mode.cls}">${mode.label}</span>
                </div>`;
    }

    function renderSearchResults() {
        const q = (document.getElementById('trkSearchInput').value || '').trim().toLowerCase();
        const all = Object.values(units).map(e => e.data);
        const box = document.getElementById('trkSearchResults');

        if (!q) {
            /* Roster mode (the 📱 counter opens here): the WHOLE picture, grouped and
               unabridged — online first (cars before guards, freshest first), then
               whatever is offline. This list is where offline-hidden units stay findable. */
            if (!all.length) {
                box.innerHTML = '<div class="trk-search-empty">No units are tracking right now.</div>';
                return;
            }
            const bySeen = (a, b) => (a.kind !== 'guard' ? 0 : 1) - (b.kind !== 'guard' ? 0 : 1)
                || a.ageSeconds - b.ageSeconds;
            const online = all.filter(u => u.ageSeconds <= HOLLOW_S).sort(bySeen);
            const offline = all.filter(u => u.ageSeconds > HOLLOW_S).sort(bySeen);
            const cars = online.filter(u => u.kind !== 'guard').length;
            box.innerHTML =
                `<div class="trk-search-sect">🟢 ONLINE (${online.length})${online.length ? ` · 🚓 ${cars} · 👮 ${online.length - cars}` : ''}</div>` +
                (online.length ? online.map(searchRow).join('')
                               : '<div class="trk-search-empty">Nobody is online right now.</div>') +
                (offline.length
                    ? `<div class="trk-search-sect">⚪ OFFLINE (${offline.length})</div>` + offline.map(searchRow).join('')
                    : '');
            return;
        }

        const list = all
            .filter(u => [unitLabel(u), u.callsign, u.patrolCar, u.guardName, u.currentSite,
                          initialsOf(u.guardName)]
                .some(v => v && String(v).toLowerCase().includes(q)))
            .sort((a, b) => (b.mode - a.mode) || String(unitLabel(a)).localeCompare(String(unitLabel(b))))
            .slice(0, 12);
        if (!list.length) {
            box.innerHTML = '<div class="trk-search-empty">No tracked asset matches.</div>';
            return;
        }
        box.innerHTML = list.map(searchRow).join('');
    }

    /* ================= compose: operator text to a phone (✉) =================
       Additive layer over the FCM message endpoints. One unit from its asset card, or
       every online unit (all / cars / guards) from the ✉ map control. The counts shown
       are this browser's view; the SERVER re-resolves who is online at send time. */

    const compose = { unitId: null, kind: 'all' };

    function onlineUnitData() {
        return Object.values(units).map(e => e.data).filter(u => u.ageSeconds <= HOLLOW_S);
    }

    function composeOverlay() {
        let el = document.getElementById('trkMsg');
        if (!el) {
            el = document.createElement('div');
            el.id = 'trkMsg';
            el.className = 'trk-msg';
            el.innerHTML = `
                <div class="trk-msg-box">
                  <div class="trk-msg-head">
                    <b id="trkMsgTarget">✉ Message</b>
                    <button class="trk-msg-close" data-trk-msg-close="1" aria-label="Close">×</button>
                  </div>
                  <div id="trkMsgScopes" class="trk-msg-scopes"></div>
                  <textarea id="trkMsgText" maxlength="240" rows="3"
                            placeholder="Type the message the officer's phone should show…"></textarea>
                  <div class="trk-msg-foot">
                    <span id="trkMsgCount" class="trk-msg-count">240</span>
                    <button id="trkMsgSend" class="trk-btn" disabled>✉ Send</button>
                  </div>
                </div>`;
            document.body.appendChild(el);
            const input = document.getElementById('trkMsgText');
            input.addEventListener('input', () => {
                document.getElementById('trkMsgCount').textContent = String(240 - input.value.length);
                document.getElementById('trkMsgSend').disabled = input.value.trim().length === 0;
            });
            input.addEventListener('keydown', e => { if (e.key === 'Escape') closeCompose(); });
        }
        return el;
    }

    function composeScopesHtml() {
        const online = onlineUnitData();
        const cars = online.filter(u => u.kind !== 'guard').length;
        const guards = online.length - cars;
        const btn = (kind, label, n) =>
            `<button class="trk-btn trk-msg-scope${compose.kind === kind ? ' trk-btn-on' : ''}" data-trk-msg-kind="${kind}">${label} (${n})</button>`;
        return btn('all', 'All online', online.length) + btn('car', '🚓 Cars', cars) + btn('guard', '👮 Guards', guards);
    }

    function openCompose(unitId) {
        const el = composeOverlay();
        compose.unitId = unitId ? Number(unitId) : null;
        const target = document.getElementById('trkMsgTarget');
        const scopes = document.getElementById('trkMsgScopes');
        if (compose.unitId) {
            const entry = units[compose.unitId];
            target.textContent = '✉ Message ' + (entry ? unitLabel(entry.data) : 'unit ' + compose.unitId);
            scopes.style.display = 'none';
            scopes.innerHTML = '';
        } else {
            target.textContent = '✉ Message online units';
            scopes.style.display = '';
            scopes.innerHTML = composeScopesHtml();
        }
        const input = document.getElementById('trkMsgText');
        input.value = '';
        document.getElementById('trkMsgCount').textContent = '240';
        document.getElementById('trkMsgSend').disabled = true;
        el.classList.add('open');
        setTimeout(() => input.focus(), 50);
    }

    function closeCompose() {
        const el = document.getElementById('trkMsg');
        if (el) el.classList.remove('open');
    }

    /* ================= zoom / map controls: fingers, not Ctrl+ ================= */

    /* Map modes (§2.4/2.5): Standard for daily work, Satellite for real-world context,
       Tactical Dark for the command-centre wall. Dark re-themes the WHOLE page by
       overriding the base map's CSS variables — one coherent product, not a dark map
       under light widgets. Persisted so a control room keeps its chosen look. */
    const MAP_MODES = ['standard', 'sat', 'dark'];
    const MODE_GLYPH = { standard: '🗺', sat: '🛰', dark: '🌙' };

    function applyMapMode(mode) {
        if (window.CRM.setBase) window.CRM.setBase(mode === 'standard' ? 'light' : mode === 'sat' ? 'sat' : 'dark');
        document.body.classList.toggle('trk-dark', mode === 'dark');
        try { localStorage.setItem('trkMapMode', mode); } catch { /* private mode */ }
        const btn = document.querySelector('[data-trk-ctl="mode"]');
        if (btn) btn.textContent = MODE_GLYPH[mode];
    }

    function currentMapMode() {
        try { return localStorage.getItem('trkMapMode') || 'standard'; } catch { return 'standard'; }
    }

    function buildControls() {
        const el = document.createElement('div');
        el.className = 'trk-controls';
        el.innerHTML = `
            <button data-trk-ctl="alerts" title="Attention feed" aria-label="Attention feed" style="position:relative">🔔<span id="trkBellBadge" class="trk-bellbadge" style="display:none">0</span></button>
            <button data-trk-ctl="search" title="Find a patrol car or guard" aria-label="Search">🔍</button>
            <button data-trk-ctl="msg" title="Message online units" aria-label="Message online units">✉</button>
            <button data-trk-ctl="in" title="Zoom in" aria-label="Zoom in">+</button>
            <button data-trk-ctl="out" title="Zoom out" aria-label="Zoom out">−</button>
            <button data-trk-ctl="fit" title="Fit all tracked units" aria-label="Fit all">⛶</button>
            <button data-trk-ctl="mode" title="Map style: standard / satellite / tactical dark" aria-label="Map style">${MODE_GLYPH[currentMapMode()]}</button>`;
        document.body.appendChild(el);
        el.addEventListener('click', ev => {
            const b = ev.target.closest('[data-trk-ctl]');
            if (!b) return;
            const what = b.getAttribute('data-trk-ctl');
            if (what === 'in') map.zoomIn();
            else if (what === 'out') map.zoomOut();
            else if (what === 'search') toggleSearch(true);
            else if (what === 'msg') openCompose(null);
            else if (what === 'alerts') toggleAlerts();
            else if (what === 'mode') {
                const next = MAP_MODES[(MAP_MODES.indexOf(currentMapMode()) + 1) % MAP_MODES.length];
                applyMapMode(next);
            }
            else if (what === 'fit') {
                const pts = Object.values(units).map(e => e.marker.getLatLng());
                if (pts.length) map.flyToBounds(L.latLngBounds(pts).pad(0.3), { duration: 1 });
            }
        });
        if (currentMapMode() !== 'standard') applyMapMode(currentMapMode());
    }

    /* ================= notices (session takeover etc.) ================= */

    function notice(msg, level) {
        let box = document.getElementById('trkNotices');
        if (!box) {
            box = document.createElement('div');
            box.id = 'trkNotices';
            box.className = 'trk-notices';
            document.body.appendChild(box);
        }
        const n = document.createElement('div');
        n.className = 'trk-notice' + (level === 'alarm' ? ' alarm' : '');
        n.innerHTML = msg;
        box.appendChild(n);
        while (box.children.length > 4) box.removeChild(box.firstChild);
        setTimeout(() => { n.style.opacity = '0'; setTimeout(() => n.remove(), 500); },
            level === 'alarm' ? 15000 : 8000);
    }

    /* ================= replay (Phase 3: professional playback) =================
       Live and replay share the map; replay plays ONE SESSION's audited trail — a window
       holding several sessions offers a picker, and two officers' journeys are never merged
       into one line (the Cochin↔Poonjar rule). The route colours in with TIME as the ghost
       drives it: early = cool blue, late = warm red, un-driven = faint dashes — an
       out-and-back on one road reads unambiguously. */
    const REPLAY_BUCKETS = 14;
    const replay = {
        active: false, playing: false, unitId: null, session: null,
        points: [], stops: [], events: [], idx: 0, speed: 4, timer: null,
        ghost: null, ghostSprite: null, baseLine: null, buckets: [], bucketEnd: [],
        marks: [], truncated: false
    };

    function bucketColor(i) {
        /* hue 210 (blue) → 10 (red) across the session. Colour IS the time axis. */
        return `hsl(${Math.round(210 - 200 * i / Math.max(1, REPLAY_BUCKETS - 1))} 85% 45%)`;
    }
    /* Both through utcDate: p.utc arrives WITHOUT a zone marker, and a raw new Date()
       shows UTC digits on the wall clock — the replay said 02:21 in a room whose clock
       said 12:21 (field report, 18 Aug). Operators read local time, so we render local. */
    function hm(v) { return v ? utcDate(v).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'; }
    function dayOf(v) { return v ? utcDate(v).toLocaleDateString([], { day: '2-digit', month: 'short', year: 'numeric' }) : ''; }

    function replayBarHtml(label, s) {
        const t0 = s.points[0].utc, t1 = s.points[s.points.length - 1].utc;
        return `<div class="trk-replay-bar" id="trkReplayBar">
            <div class="trk-replay-head">
              <b>REPLAY · ${esc(label)} · ${dayOf(t0)}${s.guardName ? ' · ' + esc(s.guardName) : ''}</b>
              ${replay.truncated ? '<span class="trk-replay-trunc">⚠ window truncated at 5000 points — oldest not shown</span>' : ''}
              <button id="trkReplayLive" class="trk-btn">⟳ LIVE</button>
            </div>
            <div class="trk-replay-main">
              <button data-trk-rctl="prev" title="Previous event" aria-label="Previous event">⏮</button>
              <button data-trk-rctl="play" id="trkReplayPlay" title="Play / pause" aria-label="Play or pause">⏸</button>
              <button data-trk-rctl="next" title="Next event" aria-label="Next event">⏭</button>
              <span class="trk-replay-speeds">
                <button data-trk-rspeed="1">1×</button><button data-trk-rspeed="2">2×</button>
                <button data-trk-rspeed="4" class="on">4×</button><button data-trk-rspeed="16">16×</button>
              </span>
              <span class="trk-replay-time" id="trkReplayClock">—</span>
            </div>
            <div class="trk-replay-timeline">
              <span>${hm(t0)}</span>
              <input type="range" id="trkReplayPos" min="0" max="${s.points.length - 1}" value="0" step="1" aria-label="Replay position">
              <span>${hm(t1)}</span>
            </div>
        </div>`;
    }

    function endReplay() {
        if (replay.timer) clearInterval(replay.timer);
        [replay.baseLine, replay.ghost, ...replay.buckets, ...replay.marks]
            .filter(Boolean).forEach(l => layer.removeLayer(l));
        ['trkReplayBar', 'trkSessionPick', 'trkReplayWindow'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.remove();
        });
        Object.assign(replay, {
            active: false, playing: false, unitId: null, session: null,
            points: [], stops: [], events: [], idx: 0, timer: null,
            ghost: null, ghostSprite: null, baseLine: null, buckets: [], bucketEnd: [], marks: [],
            truncated: false
        });
    }

    /* Draw the frame for replay.idx: reveal every bucket the clock has passed, part-fill
       the current one, move+turn the ghost, sync clock and slider. Works for any idx, so
       scrubbing backwards is the same code path as playing forwards. */
    function renderProgress() {
        const pts = replay.points;
        const i = replay.idx;
        for (let b = 0; b < replay.buckets.length; b++) {
            const start = b === 0 ? 0 : replay.bucketEnd[b - 1];
            const end = Math.min(replay.bucketEnd[b], Math.max(start, i));
            replay.buckets[b].setLatLngs(i <= start ? [] : pts.slice(start, end + 1).map(p => [p.lat, p.lon]));
        }
        const p = pts[i];
        if (p) {
            replay.ghost.setLatLng([p.lat, p.lon]);
            if (replay.ghostSprite == null && replay.ghost._icon)
                replay.ghostSprite = replay.ghost._icon.querySelector('.trk-sprite');
            if (replay.ghostSprite && p.headingDeg != null)
                replay.ghostSprite.classList.toggle('trk-flip', facesLeft(p.headingDeg));
        }
        const pos = document.getElementById('trkReplayPos');
        const clock = document.getElementById('trkReplayClock');
        const play = document.getElementById('trkReplayPlay');
        if (pos) pos.value = i;
        if (clock && p) clock.textContent = `${dayOf(p.utc)} ${utcDate(p.utc).toLocaleTimeString()}`;
        if (play) play.textContent = replay.playing ? '⏸' : '▶';
    }

    function stopIcon(min) {
        return L.divIcon({
            className: '',
            html: `<div class="trk-stop-mark">⏸ ${fmtMins(min)}</div>`,
            iconSize: [58, 20], iconAnchor: [29, 10]
        });
    }

    function playSession(unitId, session, truncated) {
        const label = units[unitId] ? unitLabel(units[unitId].data) : ('Unit ' + unitId);
        replay.active = true;
        replay.playing = true;
        replay.unitId = unitId;
        replay.session = session;
        replay.points = session.points;
        replay.stops = session.stops || [];
        replay.truncated = !!truncated;
        replay.idx = 0;

        const pts = session.points;
        const latlngs = pts.map(p => [p.lat, p.lon]);

        /* The whole journey, faint — what remains to be driven. */
        replay.baseLine = L.polyline(latlngs, { weight: 3, opacity: .35, color: '#94a3b8', dashArray: '4 8' }).addTo(layer);

        /* Time buckets: equal spans of the session, blue → red. Filled by renderProgress. */
        replay.buckets = [];
        replay.bucketEnd = [];
        const t0 = utcDate(pts[0].utc).getTime();
        const t1 = utcDate(pts[pts.length - 1].utc).getTime();
        const span = Math.max(1, t1 - t0);
        let cursor = 0;
        for (let b = 0; b < REPLAY_BUCKETS; b++) {
            const limit = t0 + span * (b + 1) / REPLAY_BUCKETS;
            while (cursor < pts.length - 1 && utcDate(pts[cursor].utc).getTime() <= limit) cursor++;
            replay.bucketEnd.push(cursor);
            replay.buckets.push(L.polyline([], { weight: 5, opacity: .9, color: bucketColor(b), lineJoin: 'round' }).addTo(layer));
        }

        /* Marks: start / end flags, verified NFC touches, stops, duress points. */
        replay.marks = [];
        const mark = m => { replay.marks.push(m.addTo(layer)); };
        mark(L.marker(latlngs[0], { icon: L.divIcon({ className: '', html: '<div class="trk-flag trk-flag-start">START</div>', iconSize: [46, 18], iconAnchor: [23, 9] }), zIndexOffset: 1500 }));
        mark(L.marker(latlngs[latlngs.length - 1], { icon: L.divIcon({ className: '', html: '<div class="trk-flag trk-flag-end">END</div>', iconSize: [40, 18], iconAnchor: [20, 9] }), zIndexOffset: 1500 }));
        /* Source values are TrackPointSource: 1 NfcAnchor · 2 Transit · 3 Live · 4 Duress.
           Live (3) is a sampling rate, not an event — painting it red buried a whole
           night's route under phantom "DURESS" dots (field test, 12 Aug). Only 4 is red. */
        pts.forEach(p => {
            if (p.source === 1)
                mark(L.circleMarker([p.lat, p.lon], { radius: 6, color: '#16a34a', fillColor: '#16a34a', fillOpacity: .9 })
                    .bindTooltip('✓ NFC ' + (p.tag || '') + ' · ' + hm(p.utc)));
            if (p.source === 4)
                mark(L.circleMarker([p.lat, p.lon], { radius: 8, color: '#dc2626', fillColor: '#dc2626', fillOpacity: .8 })
                    .bindTooltip('🚨 DURESS · ' + hm(p.utc)));
        });
        replay.stops.forEach(st => {
            mark(L.marker([st.lat, st.lon], { icon: stopIcon(st.durationMinutes), zIndexOffset: 1400 })
                .bindTooltip(`Stopped ${fmtMins(st.durationMinutes)} · ${hm(st.fromUtc)}–${hm(st.toUtc)}`));
        });

        /* Direction arrows on the faint remainder, every ~12th of the trail. */
        const step = Math.max(1, Math.floor(pts.length / 12));
        for (let i = step; i < pts.length; i += step) {
            const a = pts[i - 1], b = pts[i];
            const ang = Math.atan2(
                Math.sin(((b.lon - a.lon)) * Math.PI / 180) * Math.cos(b.lat * Math.PI / 180),
                Math.cos(a.lat * Math.PI / 180) * Math.sin(b.lat * Math.PI / 180)
                - Math.sin(a.lat * Math.PI / 180) * Math.cos(b.lat * Math.PI / 180) * Math.cos((b.lon - a.lon) * Math.PI / 180)
            ) * 180 / Math.PI;
            mark(L.marker([(Number(a.lat) + Number(b.lat)) / 2, (Number(a.lon) + Number(b.lon)) / 2], {
                icon: L.divIcon({ className: '', html: `<div class="trk-dir" style="transform:rotate(${Math.round(ang)}deg)">➤</div>`, iconSize: [14, 14], iconAnchor: [7, 7] }),
                interactive: false, keyboard: false
            }));
        }

        /* Events for ⏮ / ⏭: start, every stop, every NFC touch, every duress, end. */
        const eventIdx = new Set([0, pts.length - 1]);
        pts.forEach((p, i) => { if (p.source === 1 || p.source === 4) eventIdx.add(i); });
        replay.stops.forEach(st => {
            const t = utcDate(st.fromUtc).getTime();
            let best = 0, bestD = Infinity;
            pts.forEach((p, i) => {
                const d = Math.abs(utcDate(p.utc).getTime() - t);
                if (d < bestD) { bestD = d; best = i; }
            });
            eventIdx.add(best);
        });
        replay.events = [...eventIdx].sort((a, b) => a - b);

        /* The ghost wears the unit's own shape: a car replays as a car, a guard as their
           avatar — never a purple car walking through a house. Purple = "this is the
           recording", and the live marker underneath stays untouched. */
        const liveEntry = units[unitId];
        const isGuardGhost = liveEntry && liveEntry.data.kind === 'guard';
        const ghostHtml = isGuardGhost
            ? `<div class="trk-unit trk-replay-ghost"><div class="trk-avatar" style="background:#7c3aed">${esc(initialsOf(liveEntry.data.guardName))}</div></div>`
            : `<div class="trk-unit trk-kind-car trk-replay-ghost"><div class="trk-sprite">${carSvg('#7c3aed')}</div></div>`;
        replay.ghost = L.marker(latlngs[0], {
            icon: L.divIcon({ className: '', html: ghostHtml, iconSize: [56, 68], iconAnchor: [28, isGuardGhost ? 26 : 14] }),
            zIndexOffset: 2000
        }).addTo(layer);
        map.fitBounds(replay.baseLine.getBounds().pad(0.2));

        document.body.insertAdjacentHTML('beforeend', replayBarHtml(label, session));
        renderProgress();

        replay.timer = setInterval(() => {
            if (!replay.active || !replay.playing) return;
            replay.idx = Math.min(replay.idx + replay.speed, replay.points.length - 1);
            renderProgress();
            if (replay.idx >= replay.points.length - 1) { replay.playing = false; renderProgress(); }
        }, 250);
    }

    function sessionPicker(unitId, sessions, truncated) {
        const el = document.createElement('div');
        el.id = 'trkSessionPick';
        el.className = 'trk-session-pick';
        el.innerHTML = `<div class="head"><b>${sessions.length} sessions in this window</b><span>Each officer's journey replays separately</span></div>` +
            sessions.map((s, i) => `
                <div class="row" data-trk-session="${i}">
                  <b>${hm(s.startedUtc || s.points[0].utc)}–${hm(s.endedUtc || s.points[s.points.length - 1].utc)}</b>
                  <span>${esc(s.guardName || 'Unknown officer')}${s.callsign ? ' · ' + esc(s.callsign) : ''}</span>
                  <span class="n">${s.points.length} pts</span>
                </div>`).join('') +
            `<button class="trk-btn cancel" data-trk-session-cancel="1">Cancel</button>`;
        document.body.appendChild(el);
        el.addEventListener('click', ev => {
            if (ev.target.closest('[data-trk-session-cancel]')) { el.remove(); return; }
            const row = ev.target.closest('[data-trk-session]');
            if (!row) return;
            const s = sessions[Number(row.getAttribute('data-trk-session'))];
            el.remove();
            replay.truncated = !!truncated;
            playSession(unitId, s, truncated);
        });
    }

    /* Replay window picker (§3.1): presets for the common asks, custom date + time range
       for the rest. The 26 h server cap is a shift with margin, stated when exceeded. */
    function replayWindowPicker(unitId) {
        endReplay();
        closeCard();
        const label = units[unitId] ? unitLabel(units[unitId].data) : ('Unit ' + unitId);
        const today = new Date().toISOString().slice(0, 10);
        const el = document.createElement('div');
        el.id = 'trkReplayWindow';
        el.className = 'trk-session-pick';
        el.innerHTML = `
            <div class="head"><b>▶ Replay · ${esc(label)}</b><span>Choose what to play back</span></div>
            <div class="row" data-trk-win="8h"><b>Last 8 hours</b><span>the shift so far</span><span class="n"></span></div>
            <div class="row" data-trk-win="today"><b>Today</b><span>midnight → now</span><span class="n"></span></div>
            <div class="row" data-trk-win="yesterday"><b>Yesterday</b><span>full day</span><span class="n"></span></div>
            <div class="trk-win-custom">
              <input type="date" id="trkWinDate" value="${today}" max="${today}" aria-label="Date">
              <input type="time" id="trkWinFrom" value="06:00" aria-label="From">
              <span>→</span>
              <input type="time" id="trkWinTo" value="18:00" aria-label="To">
              <button class="trk-btn" data-trk-win="custom">Load</button>
            </div>
            <button class="trk-btn cancel" data-trk-session-cancel="1">Cancel</button>`;
        document.body.appendChild(el);
        el.addEventListener('click', ev => {
            if (ev.target.closest('[data-trk-session-cancel]')) { el.remove(); return; }
            const row = ev.target.closest('[data-trk-win]');
            if (!row) return;
            const kind = row.getAttribute('data-trk-win');
            const now = new Date();
            let fromUtc, toUtc;
            if (kind === '8h') { toUtc = now; fromUtc = new Date(now.getTime() - 8 * 3600 * 1000); }
            else if (kind === 'today') { toUtc = now; fromUtc = new Date(now); fromUtc.setHours(0, 0, 0, 0); }
            else if (kind === 'yesterday') {
                toUtc = new Date(now); toUtc.setHours(0, 0, 0, 0);
                fromUtc = new Date(toUtc.getTime() - 24 * 3600 * 1000);
            } else {
                const d = document.getElementById('trkWinDate').value;
                const f = document.getElementById('trkWinFrom').value || '00:00';
                const t = document.getElementById('trkWinTo').value || '23:59';
                if (!d) return;
                fromUtc = new Date(`${d}T${f}`);
                toUtc = new Date(`${d}T${t}`);
                if (!(toUtc > fromUtc)) { notice('The end time must be after the start time.', 'alarm'); return; }
                if (toUtc - fromUtc > 26 * 3600 * 1000) { notice('Windows are capped at 26 hours — one shift with margin.', 'alarm'); return; }
            }
            el.remove();
            fetchReplay(unitId, fromUtc, toUtc);
        });
    }

    /* A phone's first fix after login can be a cold-start ghost from a stale A-GPS
       cache, thousands of km from the shift — every 17 Aug replay "started in India"
       and drew a line across the ocean to the real route. A SHORT leading run that
       the next fix abandons at impossible speed is that ghost: drop it before
       anything is drawn, so START, the base line, buckets and events all begin where
       the patrol really began. Jumps deeper inside the route are left alone — there
       they are evidence, and the live-trail rules already refuse to draw across them. */
    const GHOST_RUN_MAX = 3;   // a real route's opening run is longer than this
    function ghostJump(a, b) {
        const km = haversineKm(Number(a.lat), Number(a.lon), Number(b.lat), Number(b.lon));
        if (km > 1000) return true;   // no patrol covers this between two fixes, whatever the gap
        const hours = Math.max(1 / 3600, (utcDate(b.utc) - utcDate(a.utc)) / 3600000);
        return km > GLIDE_MAX_KM && km / hours > 500;   // faster than anything on wheels
    }
    function dropGhostPrefix(points) {
        const pts = points || [];
        let from = 0;
        for (let cluster = 0; cluster < 3; cluster++) {   // tolerate a few ghost fixes, not many
            const limit = Math.min(from + GHOST_RUN_MAX, pts.length - 1);
            let jumpAt = -1;
            for (let i = from; i < limit; i++)
                if (ghostJump(pts[i], pts[i + 1])) { jumpAt = i; break; }
            if (jumpAt < 0) break;
            from = jumpAt + 1;
        }
        return from ? pts.slice(from) : pts;
    }

    async function fetchReplay(unitId, fromUtc, toUtc) {
        let body;
        try {
            const res = await fetch(`/api/tracking/history/${unitId}?fromUtc=${fromUtc.toISOString()}&toUtc=${toUtc.toISOString()}`,
                { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            body = await res.json();
        } catch { notice('Replay unavailable.', 'alarm'); return; }

        const sessions = (body.sessions || [])
            .map(s => ({ ...s, points: dropGhostPrefix(s.points) }))
            .filter(s => s.points && s.points.length >= 2);
        if (!sessions.length) { notice(`No trail recorded between ${hm(fromUtc)} and ${hm(toUtc)}.`); return; }
        if (sessions.length === 1) playSession(unitId, sessions[0], body.truncated);
        else sessionPicker(unitId, sessions, body.truncated);
    }

    function startReplay(unitId) {
        replayWindowPicker(unitId);
    }

    /* ================= delegated clicks ================= */

    document.addEventListener('click', ev => {
        const t = ev.target.closest ? ev.target : null;
        if (!t) return;
        const attr = name => {
            const el = ev.target.closest(`[${name}]`);
            return el ? el.getAttribute(name) : null;
        };
        const rid = attr('data-trk-replay');
        if (rid) { ev.preventDefault(); startReplay(Number(rid)); return; }
        const spd = attr('data-trk-rspeed');
        if (spd && replay.active) {
            replay.speed = Number(spd);
            document.querySelectorAll('[data-trk-rspeed]').forEach(b => b.classList.toggle('on', b.getAttribute('data-trk-rspeed') === spd));
            return;
        }
        const rctl = attr('data-trk-rctl');
        if (rctl && replay.active) {
            if (rctl === 'play') replay.playing = !replay.playing;
            else if (rctl === 'prev') {
                const prev = [...replay.events].reverse().find(i => i < replay.idx);
                replay.idx = prev ?? 0;
            } else if (rctl === 'next') {
                const next = replay.events.find(i => i > replay.idx);
                replay.idx = next ?? replay.points.length - 1;
            }
            renderProgress();
            return;
        }
        if (ev.target.id === 'trkReplayLive') { endReplay(); return; }
        const fid = attr('data-trk-follow');
        if (fid) { follow.unitId === Number(fid) ? stopFollow() : startFollow(Number(fid)); return; }
        if (attr('data-trk-unfollow')) { stopFollow(); return; }
        if (attr('data-trk-resume')) {
            follow.suspended = false;
            const entry = units[follow.unitId];
            if (entry) map.flyTo(entry.marker.getLatLng(), Math.max(map.getZoom(), 14), { duration: 1 });
            renderFollowBar();
            return;
        }
        if (attr('data-trk-guardid')) { guardIdOpen = !guardIdOpen; renderCard(); return; }
        if (attr('data-trk-card-close')) { closeCard(); return; }
        if (attr('data-trk-search-close')) { toggleSearch(false); return; }
        if (attr('data-trk-alerts-close')) { toggleAlerts(false); return; }
        const siteEl = ev.target.closest && ev.target.closest('[data-trk-site]');
        if (siteEl) { openSiteCard(siteEl.getAttribute('data-trk-site')); return; }
        const oid = attr('data-trk-open');
        if (oid) {
            toggleSearch(false);
            toggleAlerts(false);
            const entry = units[Number(oid)];
            if (entry) {
                map.flyTo(entry.marker.getLatLng(), Math.max(map.getZoom(), 14), { duration: 1 });
                openCard(Number(oid));
            }
            return;
        }
    });
    document.addEventListener('input', ev => {
        if (ev.target.id === 'trkReplayPos' && replay.active) {
            /* Scrubbing pauses playback — the timeline must not fight the finger. */
            replay.playing = false;
            replay.idx = Math.max(0, Math.min(Number(ev.target.value) || 0, replay.points.length - 1));
            renderProgress();
        }
    });

    /* Live Mode commands — delegated so card re-renders keep working. */
    document.addEventListener('click', async ev => {
        const pingEl = ev.target.closest && ev.target.closest('[data-trk-ping]');
        if (pingEl) {
            /* 📳 Ping: FCM is the accelerator, ingest is the guarantee — this button only
               ever claims "asked", never "answered". The answer is the marker moving. */
            ev.preventDefault();
            const pingId = pingEl.getAttribute('data-trk-ping');
            pingEl.disabled = true;
            try {
                const res = await fetch(`/api/tracking/ping/${pingId}`, { method: 'POST', credentials: 'same-origin' });
                if (res.status === 202) {
                    notice('📳 Nudge sent — if the phone is reachable, a fresh position arrives within seconds.');
                } else if (res.status === 409 || res.status === 429) {
                    const body = await res.json().catch(() => null);
                    notice((body && body.error) || 'Ping unavailable for this unit.', 'alarm');
                } else {
                    notice('Ping needs an operator sign-in (read-only view cannot send it).', 'alarm');
                }
            } catch {
                notice('Ping failed — network problem.', 'alarm');
            } finally {
                pingEl.disabled = false;
            }
            return;
        }
        const liveEl = ev.target.closest && ev.target.closest('[data-trk-live]');
        const stopEl = ev.target.closest && ev.target.closest('[data-trk-stop]');
        if (!liveEl && !stopEl) return;
        const liveId = liveEl && liveEl.getAttribute('data-trk-live');
        const stopId = stopEl && stopEl.getAttribute('data-trk-stop');
        ev.preventDefault();
        try {
            if (liveId) {
                const res = await fetch('/api/tracking/command', {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ unitId: Number(liveId) })
                });
                if (res.ok) {
                    liveRequests[liveId] = Date.now();
                    notice('◉ Live requested — waiting for the device to confirm…');
                } else if (res.status === 409) {
                    const body = await res.json().catch(() => null);
                    notice((body && body.error) || 'Live tracking unavailable for this unit.', 'alarm');
                } else {
                    /* 403 on the read-only keyed view, 401 after a session timeout — the
                       button must never just do nothing. */
                    notice('Live commands need an operator sign-in (read-only view cannot send them).', 'alarm');
                }
            } else {
                const res = await fetch(`/api/tracking/command/${stopId}`, { method: 'DELETE', credentials: 'same-origin' });
                delete liveRequests[stopId];
                if (res.ok) {
                    notice('⏹ Stop sent — the device returns to normal within a few seconds.');
                } else {
                    notice('Stop could not be sent (operator sign-in required).', 'alarm');
                }
            }
            renderCard();
        } catch {
            notice('Command failed — network problem; the next poll shows the truth.', 'alarm');
        }
    });

    /* ✉ custom messages — a SEPARATE delegated listener so the ping/live handlers above
       stay untouched. 202 means FCM accepted the message, never that anyone read it. */
    document.addEventListener('click', async ev => {
        const msgEl = ev.target.closest && ev.target.closest('[data-trk-msg]');
        if (msgEl) { ev.preventDefault(); openCompose(msgEl.getAttribute('data-trk-msg')); return; }
        if (ev.target.closest && ev.target.closest('[data-trk-msg-close]')) { closeCompose(); return; }
        const scopeEl = ev.target.closest && ev.target.closest('[data-trk-msg-kind]');
        if (scopeEl) {
            compose.kind = scopeEl.getAttribute('data-trk-msg-kind');
            const scopes = document.getElementById('trkMsgScopes');
            if (scopes) scopes.innerHTML = composeScopesHtml();
            return;
        }
        if (ev.target.id !== 'trkMsgSend') return;
        ev.preventDefault();
        const btn = ev.target;
        const message = (document.getElementById('trkMsgText').value || '').trim();
        if (!message) return;
        btn.disabled = true;
        try {
            const res = compose.unitId
                ? await fetch(`/api/tracking/message/${compose.unitId}`, {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ message })
                })
                : await fetch('/api/tracking/message/broadcast', {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ message, kind: compose.kind })
                });
            if (res.status === 202) {
                const body = await res.json().catch(() => null);
                closeCompose();
                if (compose.unitId) {
                    const entry = units[compose.unitId];
                    notice(`✉ Message sent to ${esc(entry ? unitLabel(entry.data) : 'unit ' + compose.unitId)} — it shows on the phone within seconds.`);
                } else {
                    const scopeLabel = compose.kind === 'car' ? 'patrol cars' : compose.kind === 'guard' ? 'guards' : 'online units';
                    const skipped = body
                        ? [body.unitsSkippedNoToken ? body.unitsSkippedNoToken + ' no push' : '',
                           body.unitsSkippedCooldown ? body.unitsSkippedCooldown + ' cooldown' : ''].filter(Boolean).join(', ')
                        : '';
                    notice(`✉ Message sent to ${body ? body.unitsSent : '?'} of ${body ? body.unitsTargeted : '?'} online ${scopeLabel}${skipped ? ' (skipped: ' + esc(skipped) + ')' : ''}.`);
                }
            } else if (res.status === 400 || res.status === 403 || res.status === 409 || res.status === 429) {
                /* Refusals keep the compose open — the typed message must not be lost. */
                const body = await res.json().catch(() => null);
                notice(esc((body && body.error) || 'Message could not be sent.'), 'alarm');
            } else {
                notice('Messaging needs an operator sign-in (read-only view cannot send).', 'alarm');
            }
        } catch {
            notice('Message failed — network problem.', 'alarm');
        } finally {
            btn.disabled = false;
        }
    });

    /* ================= live state ================= */

    function refreshIcon(entry) {
        applyIcon(entry, true);
    }

    /* Glide only when the move is plausible driving; a data jump snaps instantly so the
       operator never watches a car "drive" across a city it never crossed.
       Clustered guard markers must be re-added on a real move — the cluster grid does not
       watch setLatLng — but tiny GPS wobble stays in place so clusters don't flicker. */
    function moveMarker(entry, pos) {
        const from = entry.marker.getLatLng();
        const movedKm = haversineKm(from.lat, from.lng, pos[0], pos[1]);
        const clustered = entry.markerGroup === guardsGroup && guardsGroup.removeLayer && L.MarkerClusterGroup
            && guardsGroup instanceof L.MarkerClusterGroup;
        if (clustered) {
            if (movedKm < 0.005) return;              // <5 m: jitter, leave the cluster alone
            guardsGroup.removeLayer(entry.marker);
            entry.marker.setLatLng(pos);
            guardsGroup.addLayer(entry.marker);
            return;
        }
        const icon = entry.marker._icon;
        if (movedKm > GLIDE_MAX_KM && icon) {
            icon.classList.add('trk-nofx');
            entry.marker.setLatLng(pos);
            setTimeout(() => icon.classList.remove('trk-nofx'), 60);
        } else {
            entry.marker.setLatLng(pos);
        }
    }

    /* Session-trail seeding: the breadcrumb used to begin wherever the unit happened to
       be when THIS browser tab opened, so a car that left Poonjar an hour earlier showed
       a trail born mid-route while replay (server history) showed the true start — two
       different answers to "where has it been". Seed each car's trail once from the
       audited history of its CURRENT session so live and replay tell the same story.
       Guards are not seeded: dozens of clustered guards would fan out dozens of history
       reads for breadcrumbs nobody follows. */
    const TRAIL_MAX = 2000;          // seeded session + live appends; a polyline this size is cheap

    /* A trail is TWO lines: white casing under a solid colour core — the Google-Maps
       idiom that keeps a route legible over a busy street map (field feedback 12 Aug:
       3 px at 55% opacity vanished into the OSM base). Core is the source of truth;
       the casing mirrors it through this helper. */
    function setTrailLine(entry, latlngs) {
        entry.trail.setLatLngs(latlngs);
        if (entry.trailCasing) entry.trailCasing.setLatLngs(latlngs);
    }
    function seedTrail(entry) {
        const u = entry.data;
        if (entry.trailSeeded || u.kind === 'guard') return;
        if (!u.sessionId) return;        // partial hub frame — the next full poll seeds
        entry.trailSeeded = true;
        const from = u.sessionStartedUtc ? utcDate(u.sessionStartedUtc)
            : new Date(Date.now() - 8 * 3600 * 1000);
        fetch(`/api/tracking/history/${u.unitId}?fromUtc=${from.toISOString()}&toUtc=${new Date().toISOString()}`,
            { credentials: 'same-origin' })
            .then(r => r.ok ? r.json() : null)
            .then(body => {
                if (!body || units[u.unitId] !== entry) return;      // unit went off shift meanwhile
                const sid = String(u.sessionId).toLowerCase();
                const s = (body.sessions || []).find(x => String(x.sessionId).toLowerCase() === sid);
                if (!s || !s.points || s.points.length < 2) return;
                /* Thin to a drawable size, keeping the start; the trail is a picture,
                   the audit record stays server-side. */
                let pts = s.points;
                const budget = TRAIL_MAX - 200;
                if (pts.length > budget) {
                    const step = Math.ceil(pts.length / budget);
                    pts = pts.filter((p, i) => i % step === 0 || i === s.points.length - 1);
                }
                let seed = pts.map(p => L.latLng(Number(p.lat), Number(p.lon)));
                /* Jump-break invariant (same rule as the live append path): a >3 km hop is
                   a data jump, not a road — keep only the contiguous tail so the seeded
                   line never crosses ground nobody covered. */
                let start = 0;
                for (let i = 1; i < seed.length; i++) {
                    if (haversineKm(seed[i - 1].lat, seed[i - 1].lng, seed[i].lat, seed[i].lng) > GLIDE_MAX_KM)
                        start = i;
                }
                seed = seed.slice(start);
                const live = entry.trail.getLatLngs();
                /* If the history tail and the live head don't meet (device offline in
                   between), seeding would draw that same phantom line — keep live only. */
                if (live.length && seed.length &&
                    haversineKm(seed[seed.length - 1].lat, seed[seed.length - 1].lng,
                        live[0].lat, live[0].lng) > GLIDE_MAX_KM) return;
                const merged = seed.concat(live).filter((p, i, a) =>
                    i === 0 || p.lat !== a[i - 1].lat || p.lng !== a[i - 1].lng);
                setTrailLine(entry, merged);
            })
            .catch(() => { /* no seed — the trail still grows live from here */ });
    }

    function upsert(u, nowMs) {
        let entry = units[u.unitId];
        /* Hub frames are partial (no labels, no session): merge over what we know so a
           fast-path update never erases identity fields. */
        if (entry) u = Object.assign({}, entry.data, u);
        const pos = [Number(u.lat), Number(u.lon)];

        if (!entry) {
            const isGuard = u.kind === 'guard';
            const markerGroup = isGuard ? guardsGroup : carsGroup;
            const trailGroup = isGuard ? guardTrailsGroup : carTrailsGroup;
            const marker = L.marker(pos, { icon: unitIcon(u), zIndexOffset: 1000 });
            marker.on('click', () => openCard(u.unitId));
            markerGroup.addLayer(marker);
            /* Breadcrumb: session-local, client-side view; full history lives server-side. */
            const trailCasing = L.polyline([pos], { weight: 9, opacity: .9, color: '#ffffff', lineJoin: 'round', lineCap: 'round' }).addTo(trailGroup);
            const trail = L.polyline([pos], { weight: 5, opacity: .95, color: '#2563eb', lineJoin: 'round', lineCap: 'round' }).addTo(trailGroup);
            entry = units[u.unitId] = { marker, trail, trailCasing, markerGroup, trailGroup, data: u, lastSeenMs: nowMs, iconSig: iconSig(u) };
            seedTrail(entry);      // cars: back-fill this session's route so the start shows
            applyOfflineVisibility(entry);   // yesterday's leftovers arrive hidden
        } else {
            /* SESSION BOUNDARY: the unit changed hands. The trail belongs to the previous
               officer — reset it, and tell the operator out loud (§B2/B3). A trail that
               survives a takeover stitches two journeys into one line. */
            if (entry.data.sessionId && u.sessionId && entry.data.sessionId !== u.sessionId) {
                setTrailLine(entry, [pos]);
                entry.trailSeeded = false;      // new session, new seed (covers reconnects too)
                notice(`⚠ <b>${esc(unitLabel(u))}</b> — session taken over${u.guardName ? ' by ' + esc(u.guardName) : ''}`, 'alarm');
                addAlert('alarm', `⚠ <b>${esc(unitLabel(u))}</b> — session taken over${u.guardName ? ' by ' + esc(u.guardName) : ''}`, u.unitId);
            }
            /* Arrival alerts moved server-side (§5.1): /api/tracking/arrivals now records
               them from GPS geofence + NFC, so they survive refreshes and reach every
               operator. Raising a second, transient copy here would double every arrival. */
            /* Offline (§5.1): crossing the hollow threshold is worth one line, once. */
            if (entry.data.ageSeconds <= HOLLOW_S && u.ageSeconds > HOLLOW_S) {
                addAlert('warn', `⚠ <b>${esc(unitLabel(u))}</b> has not reported for ${fmtAge(u.ageSeconds)}`, u.unitId);
            }
            moveMarker(entry, pos);
            entry.data = u;
            applyIcon(entry, false);
            const pts = entry.trail.getLatLngs();
            const last = pts[pts.length - 1];
            if (last && haversineKm(last.lat, last.lng, pos[0], pos[1]) > GLIDE_MAX_KM) {
                /* A jump is not a journey: restart the trail rather than draw a line
                   across ground nobody covered (first coarse fix → real fix, etc.). */
                setTrailLine(entry, [pos]);
            } else if (!last || last.lat !== pos[0] || last.lng !== pos[1]) {
                pts.push(L.latLng(pos[0], pos[1]));
                if (pts.length > TRAIL_MAX) pts.shift();   // bounded, but big enough for a whole shift
                setTrailLine(entry, pts);
            }
            if (!entry.trailSeeded) seedTrail(entry);      // hub-created or post-takeover entries
            entry.lastSeenMs = nowMs;
            applyOfflineVisibility(entry);   // crossing the 4h line hides it; a fresh fix reveals it
        }

        if (u.mode === 4 && !entry.duressAnnounced) {   // duress: centre once, loudly
            entry.duressAnnounced = true;
            addAlert('alarm', `🚨 <b>DURESS — ${esc(unitLabel(u))}</b>${u.guardName ? ' · ' + esc(u.guardName) : ''}${u.currentSite ? ' · ' + esc(u.currentSite) : ''}`, u.unitId);
            map.setView(pos, Math.max(map.getZoom(), 14));
            openCard(u.unitId);
        } else if (u.mode !== 4) {
            entry.duressAnnounced = false;
        }
    }

    /* The base map is locked to Australia. If a tracked unit reports from outside that
       envelope the operator could never pan to it, so release the lock once — an operator
       must always be able to see a unit the system is willing to show. */
    let boundsReleased = false;
    function releaseBoundsIfOutside(list) {
        if (boundsReleased || !list.length) return;
        const b = map.options.maxBounds;
        if (!b) { boundsReleased = true; return; }
        const outside = list.some(u => !b.contains(L.latLng(Number(u.lat), Number(u.lon))));
        if (outside) {
            map.setMaxBounds(null);
            map.setMinZoom(2);
            boundsReleased = true;
            console.info('Tracking: a unit is outside the Australia map bounds; pan lock released.');
        }
    }

    function applySnapshot(list) {
        releaseBoundsIfOutside(list);
        const nowMs = Date.now();
        const seen = {};
        list.forEach(u => {
            seen[u.unitId] = true;
            if (u.mode === 3) delete liveRequests[u.unitId];   // device acked: LIVE is now the truth
            upsert(u, nowMs);
        });
        /* Units gone from the snapshot ended their session: off the map (§13.5). */
        Object.keys(units).forEach(id => {
            if (!seen[id]) {
                addAlert('info', `⚪ <b>${esc(unitLabel(units[id].data))}</b> went off shift`, null);
                if (follow.unitId === Number(id)) {
                    notice(`◉ <b>${esc(unitLabel(units[id].data))}</b> — session ended, follow stopped`);
                    stopFollow();
                }
                if (selectedUnitId === Number(id)) closeCard();
                units[id].markerGroup.removeLayer(units[id].marker);
                units[id].trailGroup.removeLayer(units[id].trail);
                if (units[id].trailCasing) units[id].trailGroup.removeLayer(units[id].trailCasing);
                delete units[id];
            }
        });
        /* Follow: keep the target centred, unless the operator has panned away. */
        if (follow.unitId && !follow.suspended && units[follow.unitId]) {
            map.panTo(units[follow.unitId].marker.getLatLng(), { animate: true, duration: 0.8 });
        }
        renderCard();
        renderFollowBar();
        setStatus(list.length, true);
    }

    /* ---- 📱 online counter (left, above the layer chips): the one-tap answer to
       "who is out there right now?" — tap it for the full clickable roster, which opens
       in the search window (typing there narrows it, so it IS the search too). ---- */
    let onlineFab = null;
    function renderOnlineFab() {
        if (!onlineFab) {
            onlineFab = document.createElement('button');
            onlineFab.className = 'trk-online-fab';
            onlineFab.title = 'Everyone online right now — tap for the full list';
            onlineFab.addEventListener('click', () => toggleSearch(true));
            document.body.appendChild(onlineFab);
        }
        const online = Object.values(units).map(e => e.data).filter(u => u.ageSeconds <= HOLLOW_S);
        const cars = online.filter(u => u.kind !== 'guard').length;
        onlineFab.innerHTML = `📱 <b>${online.length}</b> online · 🚓 ${cars} · 👮 ${online.length - cars}`;
    }

    /* ---- status pill → live-operation strip (§27): the 5-second answer ---- */
    let statusEl = null;
    function setStatus(count, healthy) {
        if (!statusEl) {
            statusEl = document.createElement('div');
            statusEl.className = 'trk-status';
            statusEl.title = 'Open the attention feed';
            statusEl.addEventListener('click', () => toggleAlerts(true));
            document.body.appendChild(statusEl);
        }
        if (!healthy) {
            statusEl.textContent = '🚓 tracking: reconnecting…';
            statusEl.classList.add('trk-status-bad');
            return;
        }
        statusEl.classList.remove('trk-status-bad');
        const list = Object.values(units).map(e => e.data);
        const cars = list.filter(u => u.kind !== 'guard').length;
        const guards = list.length - cars;
        const moving = list.filter(u => (u.speedKph || 0) >= 2 && u.ageSeconds <= SOFT_S).length;
        const stopped = Object.keys(idleUnits).length;
        const stale = list.filter(u => u.ageSeconds > HOLLOW_S).length;
        statusEl.innerHTML =
            `🚓 ${cars} · 👮 ${guards}` +
            (list.length ? ` · ▶ ${moving}` : '') +
            (stopped ? ` · <span class="warn">⏸ ${stopped}</span>` : '') +
            (stale ? ` · <span class="bad">⚠ ${stale} stale</span>` : '');
        renderOnlineFab();
    }

    /* ---- layer toggle chips (§4.6): CARS | GUARDS | SITES ---- */
    function buildLayerChips() {
        const el = document.createElement('div');
        el.className = 'trk-chipbar';
        el.innerHTML = `
            <button data-trk-layer="cars" title="Show/hide patrol cars">🚓 Cars</button>
            <button data-trk-layer="guards" title="Show/hide guards">👮 Guards</button>
            <button data-trk-layer="sites" title="Show/hide site markers">🏢 Sites</button>
            <button data-trk-layer="offline" class="${layerState.offline ? '' : 'off'}"
                    title="Show/hide units that stopped reporting over 4 hours ago">⚪ Offline</button>`;
        document.body.appendChild(el);
        el.addEventListener('click', ev => {
            const b = ev.target.closest('[data-trk-layer]');
            if (!b) return;
            layerState[b.getAttribute('data-trk-layer')] = !layerState[b.getAttribute('data-trk-layer')];
            applyLayerState();
        });
    }

    /* ---- polling (the Phase-1 data path) ---- */
    let pollMs = POLL_MS;
    let failures = 0;
    async function poll() {
        try {
            const res = await fetch('/api/tracking/live', { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            const body = await res.json();
            failures = 0;
            applySnapshot(body.units || []);
        } catch (err) {
            failures++;
            if (failures >= 2) setStatus(0, false);
        } finally {
            setTimeout(poll, pollMs);
        }
    }

    /* ---- idle units panel: who has been sitting in one spot too long ----
       Collapsed by default to its amber header; expands on tap. It owns a fixed layout
       slot (top-right, below the header) and never overlaps the refresh card again. */
    let idlePanel = null;
    let idleCollapsed = true;
    function renderIdlePanel(list) {
        if (!idlePanel) {
            idlePanel = document.createElement('div');
            idlePanel.className = 'trk-idle-panel';
            document.body.appendChild(idlePanel);
            idlePanel.addEventListener('click', ev => {
                if (ev.target.closest('.trk-idle-head')) {
                    idleCollapsed = !idleCollapsed;
                    idlePanel.classList.toggle('collapsed', idleCollapsed);
                    return;
                }
                const row = ev.target.closest('[data-trk-goto]');
                if (!row) return;
                const [lat, lon] = row.getAttribute('data-trk-goto').split(',').map(Number);
                map.setView([lat, lon], Math.max(map.getZoom(), 15));
                const id = Number(row.getAttribute('data-trk-unit'));
                if (units[id]) openCard(id);
            });
        }
        idlePanel.classList.toggle('collapsed', idleCollapsed);
        if (!list.length) {
            idlePanel.style.display = 'none';
            return;
        }
        idlePanel.style.display = 'block';
        idlePanel.innerHTML = `<div class="trk-idle-head">⏸ IDLE UNITS (${list.length}) <span class="tgl">${idleCollapsed ? '▾' : '▴'}</span></div>` +
            list.map(u => {
                const glyph = u.kind === 'car' ? '🚓' : '👮';
                const name = esc(u.callsign || u.guardName || (u.kind === 'car' ? `Unit ${u.unitId}` : `Guard ${u.guardId}`));
                return `<div class="trk-idle-row" data-trk-goto="${u.lat},${u.lon}" data-trk-unit="${u.unitId}">
                          ${glyph} <b>${name}</b><span class="trk-idle-time">${fmtMins(u.idleMinutes)}</span>
                        </div>`;
            }).join('');
    }

    let prevIdleIds = new Set();
    async function pollIdle() {
        try {
            const res = await fetch('/api/tracking/idle', { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            const body = await res.json();
            const list = body.units || [];
            Object.keys(idleUnits).forEach(k => delete idleUnits[k]);
            list.forEach(u => { idleUnits[u.unitId] = u.idleMinutes; });
            /* Stationary alert fires once per idle spell, when the unit first crosses in. */
            list.forEach(u => {
                if (!prevIdleIds.has(u.unitId)) {
                    const label = units[u.unitId] ? unitLabel(units[u.unitId].data)
                        : (u.callsign || u.guardName || ('Unit ' + u.unitId));
                    addAlert('warn', `⏸ <b>${esc(label)}</b> stationary for ${fmtMins(u.idleMinutes)}`, u.unitId);
                }
            });
            prevIdleIds = new Set(list.map(u => u.unitId));
            renderIdlePanel(list);
        } catch {
            /* leave the last known idle picture; the live poll's health pill covers outages */
        } finally {
            setTimeout(pollIdle, 30000);
        }
    }
    pollIdle();

    /* ---- hub fast path (activates when configured; ADD §10) ---- */
    function connectHub(url) {
        if (!window.signalR) return;
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect()
            .build();
        connection.on('Frame', frame => {
            const nowMs = Date.now();
            (frame.u || []).forEach(c => upsert({
                unitId: c.id, lat: c.la, lon: c.lo, speedKph: c.s,
                headingDeg: c.h, mode: c.m, flags: c.f, ageSeconds: c.a
            }, nowMs));
        });
        connection.start()
            .then(() => connection.invoke('JoinControlRoom'))
            .then(() => { pollMs = POLL_WITH_HUB_MS; })      // hub is live; poll becomes a sweep
            .catch(() => { /* poll remains the data path */ });
        connection.onclose(() => { pollMs = POLL_MS; setStatus(0, false); });
    }

    const hubUrl = document.body.dataset.trackingHubUrl;
    if (hubUrl) connectHub(hubUrl);

    buildControls();
    buildLayerChips();
    renderOnlineFab();      // visible from the first paint, even before the first snapshot
    poll();
    pollArrivals();         // the durable bell: last 12 h of confirmed site arrivals
})();
