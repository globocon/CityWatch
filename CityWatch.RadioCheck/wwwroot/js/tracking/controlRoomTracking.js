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
    const layer = window.CRM.carLayer;

    /* Staleness thresholds (ADD §11.3 rule 2): the map must never render an old
       position as current. */
    const FRESH_S = 30, SOFT_S = 120, HOLLOW_S = 300;

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

    /* Identity of a tracked unit, most operational first:
       the CAR from the login Position ("Mobile Patrols (Car) M1" -> "M1"), then the
       radio callsign, then the guard's name. Several cars of one fleet roam the same
       sites at once, so the car is what tells them apart. */
    function shortCar(name) {
        if (!name) return null;
        const m = String(name).match(/\(car\)\s*([A-Z]{0,2}\d+)/i);
        return m ? m[1] : String(name).replace(/mobile patrols\s*/i, '').replace(/[()]/g, '').trim();
    }
    function unitLabel(u) {
        return shortCar(u.patrolCar) || u.callsign
            || (u.kind !== 'guard' ? `PC-${u.unitId}`
                : (u.guardName ? u.guardName.split(' ')[0] : `G-${u.guardId || u.unitId}`));
    }
    function initialsOf(name) {
        return (name || '?').replace(/\[.*?\]/g, '').trim().split(/\s+/)
            .map(w => w[0]).slice(0, 2).join('').toUpperCase() || '?';
    }

    /* Honest status: an idle unit is stopped, whatever the NFC travel state says —
       "in transit + idle 95m" was two truths fighting; stopped-with-context wins. */
    function statusLine(u) {
        const idleMin = idleUnits[u.unitId];
        if (idleMin) {
            return `⏸ Stopped ${fmtMins(idleMin)}${u.currentSite ? ' · ' + esc(u.currentSite) : ''}`;
        }
        const mins = u.stateMinutes ? ' ' + fmtMins(u.stateMinutes) : '';
        return u.travelState === 'AtSite' && u.currentSite
            ? `📍 At ${esc(u.currentSite)}${mins}`
            : `🚙 In transit${mins}`;
    }

    /* ================= markers: a car that looks like a car ================= */

    /* Top-down patrol car, drawn pointing north; the sprite div rotates with heading.
       Stroke colour is the mode accent. No <defs>/ids — the SVG repeats per marker. */
    function carSvg(color) {
        return `<svg viewBox="0 0 28 48" width="26" height="45" aria-hidden="true">
            <rect x="1" y="6" width="4" height="9" rx="2" fill="#334155"/>
            <rect x="23" y="6" width="4" height="9" rx="2" fill="#334155"/>
            <rect x="1" y="32" width="4" height="9" rx="2" fill="#334155"/>
            <rect x="23" y="32" width="4" height="9" rx="2" fill="#334155"/>
            <rect x="3" y="1" width="22" height="46" rx="9" fill="#1f2937" stroke="${color}" stroke-width="2.5"/>
            <circle cx="9" cy="4.5" r="1.7" fill="#fde68a"/>
            <circle cx="19" cy="4.5" r="1.7" fill="#fde68a"/>
            <rect x="6.5" y="9" width="15" height="8" rx="2.5" fill="#94a3b8"/>
            <rect x="6.5" y="34" width="15" height="6" rx="2" fill="#94a3b8" opacity=".7"/>
            <rect x="7.5" y="21" width="6" height="4.5" rx="1" fill="#ef4444"/>
            <rect x="14.5" y="21" width="6" height="4.5" rx="1" fill="#3b82f6"/>
        </svg>`;
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
            const heading = (u.headingDeg == null) ? 0 : u.headingDeg;
            body = `<div class="trk-sprite" style="transform:rotate(${heading}deg)">${carSvg(mode.color)}</div>`;
        } else {
            /* Guards: circular initials avatar — identifiable at a glance, no heading
               (foot-patrol heading is GPS noise). State ring colour = mode/idle. */
            const ring = idleMin ? '#d97706' : mode.color;
            body = `<div class="trk-avatar" style="border-color:${ring}">${esc(initialsOf(u.guardName))}</div>`;
        }
        return L.divIcon({
            className: 'trk-marker',
            html: `<div class="trk-unit ${isCar ? 'trk-kind-car' : 'trk-kind-guard'} ${mode.cls} trk-${bucket}${sel}">
                     ${body}
                     <span class="trk-id">${esc(label)}</span>${ageTxt}${idleTxt}
                   </div>`,
            iconSize: [56, 68],
            iconAnchor: [28, 26]      /* the sprite's centre — the vehicle, not the label */
        });
    }

    /* Everything that changes the icon's DOM, EXCEPT heading. Heading is applied as a
       style so the sprite turns through its CSS transition; rebuilding innerHTML on every
       poll would snap the rotation and churn the DOM for nothing. */
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
            if (sprite) sprite.style.transform = `rotate(${u.headingDeg}deg)`;
        }
    }

    /* ================= selection, card, follow ================= */

    let selectedUnitId = null;

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
        selectedUnitId = unitId;
        document.body.classList.add('trk-card-open');
        if (units[unitId]) refreshIcon(units[unitId]);
        renderCard();
    }
    function closeCard() {
        const prev = selectedUnitId;
        selectedUnitId = null;
        document.body.classList.remove('trk-card-open');
        if (prev && units[prev]) refreshIcon(units[prev]);
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
    function renderCard() {
        const el = cardEl();
        const entry = selectedUnitId && units[selectedUnitId];
        if (!entry) {
            el.classList.remove('open');
            return;
        }
        const u = entry.data;
        const mode = MODE[u.mode] || MODE[1];
        const isCar = u.kind !== 'guard';
        const title = isCar
            ? (u.patrolCar || u.callsign || `Unit ${u.unitId}`)
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
            ? new Date(u.sessionStartedUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : null;
        const following = follow.unitId === u.unitId;

        el.classList.add('open');
        el.innerHTML = `
            <div class="trk-card-head">
              <span class="trk-card-glyph">${isCar ? carSvg(mode.color) : `<div class="trk-avatar trk-avatar-lg" style="border-color:${mode.color}">${esc(initialsOf(u.guardName))}</div>`}</span>
              <span class="trk-card-title">
                <b>${esc(title)}</b>
                <span>${u.callsign && u.patrolCar ? esc(u.callsign) + ' · ' : ''}${isCar ? 'Patrol Car' : 'Guard'}</span>
              </span>
              <span class="trk-mode-chip ${mode.cls}">${mode.label}</span>
              <button class="trk-card-close" data-trk-card-close="1" aria-label="Close">×</button>
            </div>
            <div class="trk-card-body">
              ${u.guardName && isCar ? `<div class="trk-row">👤 ${esc(u.guardName)}</div>` : ''}
              <div class="trk-row trk-row-state">${statusLine(u)}</div>
              ${locationRow}
              <div class="trk-row">🚀 ${speed}${dir} &nbsp; ${u.accuracyM == null ? '' : `±${u.accuracyM}m`} ${u.batteryPct == null ? '' : `&nbsp; 🔋${u.batteryPct}%`}</div>
              <div class="trk-row dim">Fix ${fmtAge(u.ageSeconds)} ago${sessionSince ? ` · on shift since ${sessionSince}` : ''}</div>
            </div>
            <div class="trk-card-actions">
              <button class="trk-btn ${following ? 'trk-btn-on' : ''}" data-trk-follow="${u.unitId}">${following ? '◉ FOLLOWING' : '◉ FOLLOW'}</button>
              <button class="trk-btn trk-btn-replay" data-trk-replay="${u.unitId}">▶ Replay</button>
              ${liveButtonHtml(u)}
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

    function renderSearchResults() {
        const q = (document.getElementById('trkSearchInput').value || '').trim().toLowerCase();
        const list = Object.values(units).map(e => e.data)
            .filter(u => {
                if (!q) return true;
                return [unitLabel(u), u.callsign, u.patrolCar, u.guardName, u.currentSite]
                    .some(v => v && String(v).toLowerCase().includes(q));
            })
            .sort((a, b) => (b.mode - a.mode) || String(unitLabel(a)).localeCompare(String(unitLabel(b))))
            .slice(0, 12);
        const box = document.getElementById('trkSearchResults');
        if (!list.length) {
            box.innerHTML = `<div class="trk-search-empty">${Object.keys(units).length ? 'No tracked asset matches.' : 'No units are tracking right now.'}</div>`;
            return;
        }
        box.innerHTML = list.map(u => {
            const mode = MODE[u.mode] || MODE[1];
            const isCar = u.kind !== 'guard';
            return `<div class="trk-search-row" data-trk-open="${u.unitId}">
                      <span class="g">${isCar ? '🚓' : `<span class="trk-avatar trk-avatar-sm" style="border-color:${mode.color}">${esc(initialsOf(u.guardName))}</span>`}</span>
                      <span class="m"><b>${esc(unitLabel(u))}</b><span>${esc(isCar ? (u.guardName || 'Patrol car') : 'Guard')}${u.currentSite ? ' · ' + esc(u.currentSite) : ''}</span></span>
                      <span class="trk-mode-chip ${mode.cls}">${mode.label}</span>
                    </div>`;
        }).join('');
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
            <button data-trk-ctl="search" title="Find a patrol car or guard" aria-label="Search">🔍</button>
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
    function hm(v) { return v ? new Date(v).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'; }
    function dayOf(v) { return v ? new Date(v).toLocaleDateString([], { day: '2-digit', month: 'short', year: 'numeric' }) : ''; }

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
                replay.ghostSprite.style.transform = `rotate(${p.headingDeg}deg)`;
        }
        const pos = document.getElementById('trkReplayPos');
        const clock = document.getElementById('trkReplayClock');
        const play = document.getElementById('trkReplayPlay');
        if (pos) pos.value = i;
        if (clock && p) clock.textContent = `${dayOf(p.utc)} ${new Date(p.utc).toLocaleTimeString()}`;
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
        const t0 = new Date(pts[0].utc).getTime();
        const t1 = new Date(pts[pts.length - 1].utc).getTime();
        const span = Math.max(1, t1 - t0);
        let cursor = 0;
        for (let b = 0; b < REPLAY_BUCKETS; b++) {
            const limit = t0 + span * (b + 1) / REPLAY_BUCKETS;
            while (cursor < pts.length - 1 && new Date(pts[cursor].utc).getTime() <= limit) cursor++;
            replay.bucketEnd.push(cursor);
            replay.buckets.push(L.polyline([], { weight: 5, opacity: .9, color: bucketColor(b), lineJoin: 'round' }).addTo(layer));
        }

        /* Marks: start / end flags, verified NFC touches, stops, duress points. */
        replay.marks = [];
        const mark = m => { replay.marks.push(m.addTo(layer)); };
        mark(L.marker(latlngs[0], { icon: L.divIcon({ className: '', html: '<div class="trk-flag trk-flag-start">START</div>', iconSize: [46, 18], iconAnchor: [23, 9] }), zIndexOffset: 1500 }));
        mark(L.marker(latlngs[latlngs.length - 1], { icon: L.divIcon({ className: '', html: '<div class="trk-flag trk-flag-end">END</div>', iconSize: [40, 18], iconAnchor: [20, 9] }), zIndexOffset: 1500 }));
        pts.forEach(p => {
            if (p.source === 1)
                mark(L.circleMarker([p.lat, p.lon], { radius: 6, color: '#16a34a', fillColor: '#16a34a', fillOpacity: .9 })
                    .bindTooltip('✓ NFC ' + (p.tag || '') + ' · ' + hm(p.utc)));
            if (p.source === 3)
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
        pts.forEach((p, i) => { if (p.source === 1 || p.source === 3) eventIdx.add(i); });
        replay.stops.forEach(st => {
            const t = new Date(st.fromUtc).getTime();
            let best = 0, bestD = Infinity;
            pts.forEach((p, i) => {
                const d = Math.abs(new Date(p.utc).getTime() - t);
                if (d < bestD) { bestD = d; best = i; }
            });
            eventIdx.add(best);
        });
        replay.events = [...eventIdx].sort((a, b) => a - b);

        replay.ghost = L.marker(latlngs[0], {
            icon: L.divIcon({ className: '', html: `<div class="trk-unit trk-kind-car trk-replay-ghost"><div class="trk-sprite">${carSvg('#7c3aed')}</div></div>`, iconSize: [56, 68], iconAnchor: [28, 26] }),
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

    async function fetchReplay(unitId, fromUtc, toUtc) {
        let body;
        try {
            const res = await fetch(`/api/tracking/history/${unitId}?fromUtc=${fromUtc.toISOString()}&toUtc=${toUtc.toISOString()}`,
                { credentials: 'same-origin' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            body = await res.json();
        } catch { notice('Replay unavailable.', 'alarm'); return; }

        const sessions = (body.sessions || []).filter(s => s.points && s.points.length >= 2);
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
        if (attr('data-trk-card-close')) { closeCard(); return; }
        if (attr('data-trk-search-close')) { toggleSearch(false); return; }
        const oid = attr('data-trk-open');
        if (oid) {
            toggleSearch(false);
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
                } else if (res.status === 409) {
                    const body = await res.json().catch(() => null);
                    notice((body && body.error) || 'Live tracking unavailable for this unit.', 'alarm');
                }
            } else {
                await fetch(`/api/tracking/command/${stopId}`, { method: 'DELETE', credentials: 'same-origin' });
                delete liveRequests[stopId];
            }
            renderCard();
        } catch { /* next poll re-renders the truth */ }
    });

    /* ================= live state ================= */

    function refreshIcon(entry) {
        applyIcon(entry, true);
    }

    /* Glide only when the move is plausible driving; a data jump snaps instantly so the
       operator never watches a car "drive" across a city it never crossed. */
    function moveMarker(entry, pos) {
        const from = entry.marker.getLatLng();
        const jump = haversineKm(from.lat, from.lng, pos[0], pos[1]) > GLIDE_MAX_KM;
        const icon = entry.marker._icon;
        if (jump && icon) {
            icon.classList.add('trk-nofx');
            entry.marker.setLatLng(pos);
            setTimeout(() => icon.classList.remove('trk-nofx'), 60);
        } else {
            entry.marker.setLatLng(pos);
        }
    }

    function upsert(u, nowMs) {
        let entry = units[u.unitId];
        /* Hub frames are partial (no labels, no session): merge over what we know so a
           fast-path update never erases identity fields. */
        if (entry) u = Object.assign({}, entry.data, u);
        const pos = [Number(u.lat), Number(u.lon)];

        if (!entry) {
            const marker = L.marker(pos, { icon: unitIcon(u), zIndexOffset: 1000 });
            marker.on('click', () => openCard(u.unitId));
            marker.addTo(layer);
            /* Breadcrumb: session-local, client-side only. Replay/history proper is M1.9. */
            const trail = L.polyline([pos], { weight: 3, opacity: 0.55, color: '#2563eb' }).addTo(layer);
            entry = units[u.unitId] = { marker, trail, data: u, lastSeenMs: nowMs, iconSig: iconSig(u) };
        } else {
            /* SESSION BOUNDARY: the unit changed hands. The trail belongs to the previous
               officer — reset it, and tell the operator out loud (§B2/B3). A trail that
               survives a takeover stitches two journeys into one line. */
            if (entry.data.sessionId && u.sessionId && entry.data.sessionId !== u.sessionId) {
                entry.trail.setLatLngs([pos]);
                notice(`⚠ <b>${esc(unitLabel(u))}</b> — session taken over${u.guardName ? ' by ' + esc(u.guardName) : ''}`, 'alarm');
            }
            moveMarker(entry, pos);
            entry.data = u;
            applyIcon(entry, false);
            const pts = entry.trail.getLatLngs();
            const last = pts[pts.length - 1];
            if (!last || last.lat !== pos[0] || last.lng !== pos[1]) {
                pts.push(L.latLng(pos[0], pos[1]));
                if (pts.length > 500) pts.shift();     // bounded: a shift is thousands of points
                entry.trail.setLatLngs(pts);
            }
            entry.lastSeenMs = nowMs;
        }

        if (u.mode === 4 && !entry.duressAnnounced) {   // duress: centre once, loudly
            entry.duressAnnounced = true;
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
                if (follow.unitId === Number(id)) {
                    notice(`◉ <b>${esc(unitLabel(units[id].data))}</b> — session ended, follow stopped`);
                    stopFollow();
                }
                if (selectedUnitId === Number(id)) closeCard();
                layer.removeLayer(units[id].marker);
                layer.removeLayer(units[id].trail);
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

    buildControls();
    poll();
})();
