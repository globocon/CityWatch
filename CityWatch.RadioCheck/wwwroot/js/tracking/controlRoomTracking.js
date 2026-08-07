/* CityWatch.Tracking — control room live vehicle layer (feature pack M1.7, ADD §11).
   Loaded ONLY when Tracking:Enabled; attaches to the existing map through window.CRM
   ({ map, carLayer, COL }) and never touches controlRoomMap.js internals. If this file
   throws, the existing map keeps working — everything here is additive to carLayer.

   Data path: same-origin authenticated poll of /api/tracking/live every 5 s.
   When a hub URL is configured (post-Phase-0 JWT world) the SignalR 1 Hz diff frames
   take over and the poll drops to a 30 s consistency sweep. */

(function () {
    'use strict';

    if (!window.CRM || !window.CRM.map || !window.CRM.carLayer) {
        console.warn('Tracking layer: window.CRM not available; layer disabled.');
        return;
    }

    const POLL_MS = 5000;
    const POLL_WITH_HUB_MS = 30000;
    const map = window.CRM.map;
    const layer = window.CRM.carLayer;

    /* Staleness thresholds (ADD §11.3 rule 2): the map must never render an old
       position as current. */
    const FRESH_S = 30, SOFT_S = 120, HOLLOW_S = 300;

    /* Mode → accent. Colour still means urgency: duress uses the existing alarm red. */
    const MODE = {
        1: { label: 'NORMAL', cls: 'trk-normal' },
        2: { label: 'TRANSIT', cls: 'trk-transit' },
        3: { label: 'LIVE', cls: 'trk-live' },
        4: { label: 'DURESS', cls: 'trk-duress' }
    };

    const units = {};        // unitId -> { marker, trail, data, lastSeenMs }

    function ageBucket(ageS) {
        if (ageS <= FRESH_S) return 'fresh';
        if (ageS <= SOFT_S) return 'soft';
        if (ageS <= HOLLOW_S) return 'hollow';
        return 'dead';
    }

    function carIcon(u) {
        const mode = MODE[u.mode] || MODE[1];
        const bucket = ageBucket(u.ageSeconds);
        const heading = (u.headingDeg == null) ? 0 : u.headingDeg;
        const ageTxt = u.ageSeconds <= FRESH_S ? '' :
            `<span class="trk-age">${u.ageSeconds < 120 ? Math.round(u.ageSeconds) + 's' : Math.round(u.ageSeconds / 60) + 'm'}</span>`;
        return L.divIcon({
            className: '',
            html: `<div class="trk-car ${mode.cls} trk-${bucket}">
                     <div class="trk-arrow" style="transform:rotate(${heading}deg)">▲</div>
                     <span class="trk-id">PC-${u.unitId}</span>${ageTxt}
                   </div>`,
            iconSize: [46, 46],
            iconAnchor: [23, 23]
        });
    }

    function popupHtml(u) {
        const mode = MODE[u.mode] || MODE[1];
        const speed = u.speedKph == null ? '—' : `${u.speedKph} km/h`;
        const battery = u.batteryPct == null ? '' : ` · 🔋${u.batteryPct}%`;
        const acc = u.accuracyM == null ? '' : ` · ±${u.accuracyM}m`;
        return `<b>Unit ${u.unitId}</b> <span class="trk-mode-chip ${mode.cls}">${mode.label}</span><br>` +
               `${speed}${acc}${battery}<br>` +
               `<small>Fix ${u.ageSeconds}s ago</small>`;
    }

    function upsert(u, nowMs) {
        let entry = units[u.unitId];
        const pos = [Number(u.lat), Number(u.lon)];

        if (!entry) {
            const marker = L.marker(pos, { icon: carIcon(u), zIndexOffset: 1000 });
            marker.bindPopup(popupHtml(u), { className: 'crm-mini' });
            marker.addTo(layer);
            /* Breadcrumb: session-local, client-side only. Replay/history proper is M1.9. */
            const trail = L.polyline([pos], { weight: 3, opacity: 0.55, color: '#2563eb' }).addTo(layer);
            entry = units[u.unitId] = { marker, trail, data: u, lastSeenMs: nowMs };
        } else {
            /* The CSS transition on .leaflet-marker-icon glides the move (§11.3 rule 3). */
            entry.marker.setLatLng(pos);
            entry.marker.setIcon(carIcon(u));
            if (entry.marker.isPopupOpen()) entry.marker.setPopupContent(popupHtml(u));
            else entry.marker.getPopup() && entry.marker.getPopup().setContent(popupHtml(u));
            const pts = entry.trail.getLatLngs();
            const last = pts[pts.length - 1];
            if (!last || last.lat !== pos[0] || last.lng !== pos[1]) {
                pts.push(L.latLng(pos[0], pos[1]));
                if (pts.length > 500) pts.shift();     // bounded: a shift is thousands of points
                entry.trail.setLatLngs(pts);
            }
            entry.data = u;
            entry.lastSeenMs = nowMs;
        }

        if (u.mode === 4 && !entry.duressAnnounced) {   // duress: centre once, loudly
            entry.duressAnnounced = true;
            map.setView(pos, Math.max(map.getZoom(), 14));
            entry.marker.openPopup();
        } else if (u.mode !== 4) {
            entry.duressAnnounced = false;
        }
    }

    function applySnapshot(list) {
        const nowMs = Date.now();
        const seen = {};
        list.forEach(u => { seen[u.unitId] = true; upsert(u, nowMs); });
        /* Units gone from the snapshot ended their session: off the map (§13.5). */
        Object.keys(units).forEach(id => {
            if (!seen[id]) {
                layer.removeLayer(units[id].marker);
                layer.removeLayer(units[id].trail);
                delete units[id];
            }
        });
        setStatus(list.length, true);
    }

    /* ---- status pill: degrade honestly (§11.3 rule 10) ---- */
    let statusEl = null;
    function setStatus(count, healthy) {
        if (!statusEl) {
            statusEl = document.createElement('div');
            statusEl.className = 'trk-status';
            document.body.appendChild(statusEl);
        }
        statusEl.textContent = healthy ? `🚓 ${count} tracked` : '🚓 tracking: reconnecting…';
        statusEl.classList.toggle('trk-status-bad', !healthy);
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

    poll();
})();
