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

    const idleUnits = {};    // unitId -> idleMinutes (refreshed by the idle poll)

    function carIcon(u) {
        const mode = MODE[u.mode] || MODE[1];
        const bucket = ageBucket(u.ageSeconds);
        const heading = (u.headingDeg == null) ? 0 : u.headingDeg;
        const ageTxt = u.ageSeconds <= FRESH_S ? '' :
            `<span class="trk-age">${u.ageSeconds < 120 ? Math.round(u.ageSeconds) + 's' : Math.round(u.ageSeconds / 60) + 'm'}</span>`;
        /* The symbol IS the kind: patrol cars are unmistakable against site markers and
           against guards on foot. */
        const isCar = u.kind !== 'guard';
        const glyph = isCar ? '🚓' : '👮';
        const label = isCar ? `PC-${u.unitId}` : (u.guardName ? u.guardName.split(' ')[0] : `G-${u.guardId || u.unitId}`);
        const idleMin = idleUnits[u.unitId];
        const idleTxt = idleMin ? `<span class="trk-idle-badge">IDLE ${idleMin}m</span>` : '';
        return L.divIcon({
            className: '',
            html: `<div class="trk-car ${isCar ? 'trk-kind-car' : 'trk-kind-guard'} ${mode.cls} trk-${bucket}">
                     <div class="trk-glyph">${glyph}</div>
                     <div class="trk-arrow" style="transform:rotate(${heading}deg)">▲</div>
                     <span class="trk-id">${esc(label)}</span>${ageTxt}${idleTxt}
                   </div>`,
            iconSize: [46, 46],
            iconAnchor: [23, 23]
        });
    }

    function esc(s) {
        return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
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

    function popupHtml(u) {
        const mode = MODE[u.mode] || MODE[1];
        const speed = u.speedKph == null ? '—' : `${u.speedKph} km/h`;
        const battery = u.batteryPct == null ? '' : ` · 🔋${u.batteryPct}%`;
        const acc = u.accuracyM == null ? '' : ` · ±${u.accuracyM}m`;
        const isCar = u.kind !== 'guard';
        const title = isCar ? `🚓 Unit ${u.unitId}` : `👮 ${esc(u.guardName || ('Guard ' + (u.guardId || u.unitId)))}`;
        const who = isCar && u.guardName ? `<small>${esc(u.guardName)}</small><br>` : '';
        const idleMin = idleUnits[u.unitId];
        const idleTxt = idleMin ? ` <span class="trk-idle-chip">⏸ idle ${idleMin}m</span>` : '';
        return `<b>${title}</b> <span class="trk-mode-chip ${mode.cls}">${mode.label}</span>${idleTxt}<br>` +
               who +
               `${speed}${acc}${battery}<br>` +
               `<small>Fix ${u.ageSeconds}s ago</small><br>` + liveButtonHtml(u) +
               ` <button class="trk-btn trk-btn-replay" data-trk-replay="${u.unitId}">▶ Replay</button>`;
    }

    /* ================= replay (M1.9, ADD §11.2) =================
       Live and replay share the map; replay draws the audited history trail and animates a
       ghost marker along it. LIVE returns to the live picture — one surface, two times. */
    const replay = { active: false, points: [], idx: 0, speed: 4, timer: null, line: null, ghost: null, anchors: [] };

    function replayBarHtml(unitId) {
        return `<div class="trk-replay-bar" id="trkReplayBar">
            <b>REPLAY · Unit ${unitId}</b>
            <button data-trk-rspeed="1">1×</button><button data-trk-rspeed="4" class="on">4×</button>
            <button data-trk-rspeed="16">16×</button><button data-trk-rspeed="64">64×</button>
            <input type="range" id="trkReplayPos" min="0" max="100" value="0">
            <span id="trkReplayClock">—</span>
            <button id="trkReplayLive" class="trk-btn">⟳ LIVE</button>
        </div>`;
    }

    function endReplay() {
        if (replay.timer) clearInterval(replay.timer);
        if (replay.line) layer.removeLayer(replay.line);
        if (replay.ghost) layer.removeLayer(replay.ghost);
        replay.anchors.forEach(a => layer.removeLayer(a));
        const bar = document.getElementById('trkReplayBar');
        if (bar) bar.remove();
        Object.assign(replay, { active: false, points: [], idx: 0, line: null, ghost: null, anchors: [] });
    }

    function renderReplayFrame() {
        const p = replay.points[replay.idx];
        if (!p) return;
        replay.ghost.setLatLng([p.lat, p.lon]);
        const pos = document.getElementById('trkReplayPos');
        const clock = document.getElementById('trkReplayClock');
        if (pos) pos.value = Math.round(100 * replay.idx / (replay.points.length - 1));
        if (clock) clock.textContent = new Date(p.utc).toLocaleTimeString();
    }

    async function startReplay(unitId) {
        endReplay();
        const toUtc = new Date();
        const fromUtc = new Date(toUtc.getTime() - 8 * 3600 * 1000);   // the shift so far
        let body;
        try {
            const res = await fetch(`/api/tracking/history/${unitId}?fromUtc=${fromUtc.toISOString()}&toUtc=${toUtc.toISOString()}`,
                { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            body = await res.json();
        } catch { alert('Replay unavailable.'); return; }

        if (!body.points || body.points.length < 2) { alert('No trail recorded for this unit yet.'); return; }

        replay.active = true;
        replay.points = body.points;
        replay.idx = 0;
        const latlngs = body.points.map(p => [p.lat, p.lon]);
        replay.line = L.polyline(latlngs, { weight: 4, opacity: .8, color: '#7c3aed', dashArray: '6 6' }).addTo(layer);
        body.points.filter(p => p.source === 1).forEach(p => {   // NFC anchors: the verified touches
            replay.anchors.push(L.circleMarker([p.lat, p.lon],
                { radius: 6, color: '#16a34a', fillColor: '#16a34a', fillOpacity: .9 })
                .bindTooltip('NFC ' + (p.tag || '')).addTo(layer));
        });
        replay.ghost = L.marker(latlngs[0], {
            icon: L.divIcon({ className: '', html: '<div class="trk-car trk-replay-ghost"><div class="trk-arrow">▲</div></div>', iconSize: [46, 46], iconAnchor: [23, 23] }),
            zIndexOffset: 2000
        }).addTo(layer);
        map.fitBounds(replay.line.getBounds().pad(0.2));

        document.body.insertAdjacentHTML('beforeend', replayBarHtml(unitId));
        if (body.truncated) document.getElementById('trkReplayClock').textContent = '(truncated)';

        replay.timer = setInterval(() => {
            if (!replay.active) return;
            replay.idx = Math.min(replay.idx + replay.speed, replay.points.length - 1);
            renderReplayFrame();
            if (replay.idx >= replay.points.length - 1) clearInterval(replay.timer);
        }, 250);
    }

    document.addEventListener('click', ev => {
        const rid = ev.target.getAttribute && ev.target.getAttribute('data-trk-replay');
        if (rid) { ev.preventDefault(); startReplay(Number(rid)); return; }
        const spd = ev.target.getAttribute && ev.target.getAttribute('data-trk-rspeed');
        if (spd && replay.active) {
            replay.speed = Number(spd);
            document.querySelectorAll('[data-trk-rspeed]').forEach(b => b.classList.toggle('on', b === ev.target));
            return;
        }
        if (ev.target.id === 'trkReplayLive') endReplay();
    });
    document.addEventListener('input', ev => {
        if (ev.target.id === 'trkReplayPos' && replay.active) {
            replay.idx = Math.round((ev.target.value / 100) * (replay.points.length - 1));
            renderReplayFrame();
        }
    });

    /* Live Mode commands — delegated so popup re-renders keep working. */
    document.addEventListener('click', async ev => {
        const liveId = ev.target.getAttribute && ev.target.getAttribute('data-trk-live');
        const stopId = ev.target.getAttribute && ev.target.getAttribute('data-trk-stop');
        if (!liveId && !stopId) return;
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
                } else if (res.status === 409) {
                    const body = await res.json().catch(() => null);
                    alert((body && body.error) || 'Live tracking unavailable for this unit.');
                }
            } else {
                await fetch(`/api/tracking/command/${stopId}`, { method: 'DELETE', credentials: 'same-origin' });
                delete liveRequests[stopId];
            }
            const entry = units[liveId || stopId];
            if (entry) entry.marker.setPopupContent(popupHtml(entry.data));
        } catch { /* next poll re-renders the truth */ }
    });

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
        list.forEach(u => {
            seen[u.unitId] = true;
            if (u.mode === 3) delete liveRequests[u.unitId];   // device acked: LIVE is now the truth
            upsert(u, nowMs);
        });
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

    /* ---- idle units panel: who has been sitting in one spot too long ---- */
    let idlePanel = null;
    function renderIdlePanel(list) {
        if (!idlePanel) {
            idlePanel = document.createElement('div');
            idlePanel.className = 'trk-idle-panel';
            document.body.appendChild(idlePanel);
            idlePanel.addEventListener('click', ev => {
                const row = ev.target.closest('[data-trk-goto]');
                if (!row) return;
                const [lat, lon] = row.getAttribute('data-trk-goto').split(',').map(Number);
                map.setView([lat, lon], Math.max(map.getZoom(), 15));
                const id = Number(row.getAttribute('data-trk-unit'));
                if (units[id]) units[id].marker.openPopup();
            });
        }
        if (!list.length) {
            idlePanel.style.display = 'none';
            return;
        }
        idlePanel.style.display = 'block';
        idlePanel.innerHTML = `<div class="trk-idle-head">⏸ IDLE UNITS (${list.length})</div>` +
            list.map(u => {
                const glyph = u.kind === 'car' ? '🚓' : '👮';
                const name = esc(u.guardName || (u.kind === 'car' ? `Unit ${u.unitId}` : `Guard ${u.guardId}`));
                const since = u.idleMinutes >= 60
                    ? `${Math.floor(u.idleMinutes / 60)}h ${u.idleMinutes % 60}m`
                    : `${u.idleMinutes}m`;
                return `<div class="trk-idle-row" data-trk-goto="${u.lat},${u.lon}" data-trk-unit="${u.unitId}">
                          ${glyph} <b>${name}</b><span class="trk-idle-time">${since}</span>
                        </div>`;
            }).join('');
    }

    async function pollIdle() {
        try {
            const res = await fetch('/api/tracking/idle', { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            const body = await res.json();
            const list = body.units || [];
            Object.keys(idleUnits).forEach(k => delete idleUnits[k]);
            list.forEach(u => { idleUnits[u.unitId] = u.idleMinutes; });
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

    poll();
})();
