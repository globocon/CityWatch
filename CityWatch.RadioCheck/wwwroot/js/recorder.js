// Control room mic recorder - keeps the microphone in its last known state across page
// reloads (3-min auto refresh, manual F5) and across browser tabs.
//
// How it works:
//  - Audio is captured in TIMESLICE_MS chunks and each chunk is written to IndexedDB straight
//    away, so a reload loses at most the last chunk.
//  - Every MediaRecorder run is one "part" of a recording session. A reload ends the current
//    part; on load the owning tab starts the next part and uploads the finished ones through
//    /Record?handler=SaveAudio (one file per part, same session id).
//  - The recording state lives in localStorage, so every tab shows the same mic buttons.
//    Only one tab (the owner, identified by a sessionStorage tab id which survives reloads)
//    holds the microphone. Stop can be pressed in any tab.
//  - If the owner tab is closed, another tab uploads what was recorded and clears the state.
(function (window, $) {
    'use strict';

    if (!window.MediaRecorder || !navigator.mediaDevices || !window.indexedDB) {
        return;
    }

    const STATE_KEY = 'rc_rec_state';
    const COMMAND_KEY = 'rc_rec_command';
    const HEARTBEAT_KEY = 'rc_rec_heartbeat';
    const TAB_ID_KEY = 'rc_rec_tabId';
    const OWNER_LOCK = 'rc-recorder-owner';
    const UPLOAD_LOCK = 'rc-recorder-upload';
    const DB_NAME = 'rc-recorder';
    const STORE = 'chunks';
    const UPLOAD_URL = '/Record?handler=SaveAudio';

    const TIMESLICE_MS = 2000;          // max audio lost on a hard reload
    const HEARTBEAT_MS = 5000;
    const OWNER_STALE_MS = 60000;       // owner tab considered gone after this long without a heartbeat
    const RECOVERY_CHECK_MS = 30000;
    const AUTO_RESUME_TIMEOUT_MS = 10000;
    const STOP_COMMAND_TIMEOUT_MS = 10000;

    const hasLocks = !!(navigator.locks && navigator.locks.request);

    let tabId = getTabId();
    let isOwner = false;        // this tab owns the recording (recording or interrupted)
    let stream = null;
    let recorder = null;
    let segment = null;         // part being recorded in this tab
    let busy = false;
    let heartbeatTimer = null;
    let wantOwnerLock = false;
    let releaseOwnerLock = null;
    const pendingWrites = new Set();

    // ---------- storage helpers ----------

    function lsGet(key) {
        try { return window.localStorage.getItem(key); } catch (e) { return null; }
    }

    function lsSet(key, value) {
        try { window.localStorage.setItem(key, value); } catch (e) { /* ignore */ }
    }

    function lsRemove(key) {
        try { window.localStorage.removeItem(key); } catch (e) { /* ignore */ }
    }

    function readState() {
        try { return JSON.parse(lsGet(STATE_KEY)); } catch (e) { return null; }
    }

    function writeState(state) {
        state.updatedAt = Date.now();
        lsSet(STATE_KEY, JSON.stringify(state));
    }

    function clearState() {
        lsRemove(STATE_KEY);
    }

    function newId() {
        return Date.now().toString(36) + Math.random().toString(36).substring(2, 8);
    }

    function getTabId() {
        let id = null;
        try { id = window.sessionStorage.getItem(TAB_ID_KEY); } catch (e) { /* ignore */ }
        if (!id) {
            id = newId();
            try { window.sessionStorage.setItem(TAB_ID_KEY, id); } catch (e) { /* ignore */ }
        }
        return id;
    }

    // Guard logged in to this page (0 = none)
    function pageGuardId() {
        return parseInt($('#rcRecorderGuardId').val(), 10) || 0;
    }

    function segmentKey(sessionId, part) {
        return sessionId + '|' + part;
    }

    function pad(n) {
        return n < 10 ? '0' + n : '' + n;
    }

    // Browser local wall-clock time (control room time), not server time
    function localDateTimeString(d) {
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) + ' ' +
            pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
    }

    // ---------- IndexedDB ----------

    let dbPromise = null;

    function openDb() {
        if (!dbPromise) {
            dbPromise = new Promise(function (resolve, reject) {
                const req = window.indexedDB.open(DB_NAME, 1);
                req.onupgradeneeded = function () {
                    const store = req.result.createObjectStore(STORE, { keyPath: 'id', autoIncrement: true });
                    store.createIndex('segment', 'segment', { unique: false });
                };
                req.onsuccess = function () { resolve(req.result); };
                req.onerror = function () { reject(req.error); };
            });
        }
        return dbPromise;
    }

    function txDone(tx) {
        return new Promise(function (resolve, reject) {
            tx.oncomplete = function () { resolve(); };
            tx.onerror = function () { reject(tx.error); };
            tx.onabort = function () { reject(tx.error); };
        });
    }

    async function idbAddChunk(record) {
        const db = await openDb();
        const tx = db.transaction(STORE, 'readwrite');
        tx.objectStore(STORE).add(record);
        return txDone(tx);
    }

    async function idbListSegments() {
        const db = await openDb();
        const tx = db.transaction(STORE, 'readonly');
        const keys = [];
        const req = tx.objectStore(STORE).index('segment').openKeyCursor(null, 'nextunique');
        req.onsuccess = function () {
            const cursor = req.result;
            if (cursor) {
                keys.push(cursor.key);
                cursor.continue();
            }
        };
        await txDone(tx);
        return keys;
    }

    async function idbGetSegment(key) {
        const db = await openDb();
        const tx = db.transaction(STORE, 'readonly');
        const req = tx.objectStore(STORE).index('segment').getAll(key);
        await txDone(tx);
        return (req.result || []).sort(function (a, b) { return a.id - b.id; });
    }

    async function idbDeleteSegment(key) {
        const db = await openDb();
        const tx = db.transaction(STORE, 'readwrite');
        const req = tx.objectStore(STORE).index('segment').openCursor(IDBKeyRange.only(key));
        req.onsuccess = function () {
            const cursor = req.result;
            if (cursor) {
                cursor.delete();
                cursor.continue();
            }
        };
        return txDone(tx);
    }

    // ---------- ownership (heartbeat + Web Lock) ----------

    function beat() {
        lsSet(HEARTBEAT_KEY, String(Date.now()));
    }

    function acquireOwnerLock() {
        if (!hasLocks || wantOwnerLock) return;
        wantOwnerLock = true;
        navigator.locks.request(OWNER_LOCK, function () {
            if (!wantOwnerLock) return undefined; // released before it was granted
            return new Promise(function (release) { releaseOwnerLock = release; });
        }).catch(function () { /* ignore */ });
    }

    function releaseOwnerLockIfHeld() {
        wantOwnerLock = false;
        if (releaseOwnerLock) {
            releaseOwnerLock();
            releaseOwnerLock = null;
        }
    }

    function becomeOwner() {
        isOwner = true;
        beat();
        if (!heartbeatTimer) heartbeatTimer = setInterval(beat, HEARTBEAT_MS);
        acquireOwnerLock();
    }

    function resignOwner() {
        isOwner = false;
        if (heartbeatTimer) {
            clearInterval(heartbeatTimer);
            heartbeatTimer = null;
        }
        releaseOwnerLockIfHeld();
    }

    async function ownerLockHeld() {
        if (!hasLocks || !navigator.locks.query) return false;
        try {
            const q = await navigator.locks.query();
            return (q.held || []).some(function (l) { return l.name === OWNER_LOCK; });
        } catch (e) {
            return false;
        }
    }

    // Owner is alive if it beat recently (covers the gap while it reloads) or still holds
    // the lock (covers background tabs whose timers are throttled).
    async function isOwnerAlive() {
        const hb = parseInt(lsGet(HEARTBEAT_KEY), 10) || 0;
        if (Date.now() - hb < OWNER_STALE_MS) return true;
        return ownerLockHeld();
    }

    // ---------- recording ----------

    function pickMimeType() {
        const types = ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus', 'audio/mp4'];
        for (let i = 0; i < types.length; i++) {
            if (MediaRecorder.isTypeSupported && MediaRecorder.isTypeSupported(types[i])) return types[i];
        }
        return '';
    }

    function extensionFor(mimeType) {
        if (/ogg/i.test(mimeType)) return '.ogg';
        if (/mp4|aac/i.test(mimeType)) return '.m4a';
        return '.webm';
    }

    function releaseMic() {
        if (stream) {
            stream.getTracks().forEach(function (t) { t.stop(); });
            stream = null;
        }
    }

    function getMic(timeoutMs) {
        const request = navigator.mediaDevices.getUserMedia({ audio: true });
        if (!timeoutMs) return request;
        return new Promise(function (resolve, reject) {
            let timedOut = false;
            const timer = setTimeout(function () {
                timedOut = true;
                reject(new Error('Microphone did not start in time'));
            }, timeoutMs);
            request.then(function (s) {
                clearTimeout(timer);
                if (timedOut) {
                    s.getTracks().forEach(function (t) { t.stop(); });
                } else {
                    resolve(s);
                }
            }, function (err) {
                clearTimeout(timer);
                reject(err);
            });
        });
    }

    // guardId: guard who started the recording session - kept for every later part
    async function startRecording(sessionId, part, guardId, micTimeoutMs) {
        stream = await getMic(micTimeoutMs);

        const mimeType = pickMimeType();
        recorder = mimeType ? new MediaRecorder(stream, { mimeType: mimeType }) : new MediaRecorder(stream);

        const seg = {
            key: segmentKey(sessionId, part),
            sessionId: sessionId,
            part: part,
            guardId: guardId,
            mimeType: recorder.mimeType || mimeType || 'audio/webm',
            startedAt: localDateTimeString(new Date())
        };
        segment = seg;

        recorder.ondataavailable = function (e) {
            if (!e.data || e.data.size === 0) return;
            const write = idbAddChunk({
                segment: seg.key,
                sessionId: seg.sessionId,
                part: seg.part,
                guardId: seg.guardId,
                mimeType: seg.mimeType,
                startedAt: seg.startedAt,
                data: e.data
            }).catch(function (err) {
                console.error('Recorder: failed to save audio chunk', err);
            });
            pendingWrites.add(write);
            write.then(function () { pendingWrites.delete(write); });
            beat();
        };

        // Mic unplugged / permission revoked while recording
        stream.getAudioTracks().forEach(function (track) {
            track.onended = function () {
                if (segment === seg) handleInterruption();
            };
        });

        recorder.start(TIMESLICE_MS);

        writeState({
            active: true,
            status: 'recording',
            sessionId: sessionId,
            part: part,
            guardId: guardId,
            ownerTabId: tabId
        });
        becomeOwner();
    }

    // Stops the local recorder and waits until its last chunk is in IndexedDB
    function stopLocal() {
        return new Promise(function (resolve) {
            if (!recorder || recorder.state === 'inactive') {
                resolve();
                return;
            }
            recorder.onstop = function () { resolve(); };
            recorder.stop();
        }).then(function () {
            return Promise.all(Array.from(pendingWrites));
        }).then(function () {
            releaseMic();
            recorder = null;
            segment = null;
        });
    }

    async function stopAsOwner() {
        await stopLocal();
        resignOwner();
        clearState();
        hideBanner();
        applyUi();
        recoverFinishedSegments();
    }

    async function handleInterruption() {
        await stopLocal();
        const s = readState();
        if (s && s.active && s.ownerTabId === tabId) {
            s.status = 'interrupted';
            writeState(s);
        }
        applyUi();
        recoverFinishedSegments();
    }

    // ---------- upload ----------

    function antiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').first().val() || '';
    }

    async function uploadSegment(key) {
        const records = await idbGetSegment(key);
        if (!records.length) return;

        const first = records[0];
        const blob = new Blob(records.map(function (r) { return r.data; }), { type: first.mimeType });

        // A part cut off right at a reload can be empty - nothing to upload
        if (blob.size > 0) {
            const formData = new FormData();
            formData.append('audioFile', blob, 'recording' + extensionFor(first.mimeType));
            formData.append('recordingSessionId', first.sessionId);
            formData.append('partNo', first.part);
            formData.append('guardId', first.guardId || 0);
            formData.append('clientLocalDateTime', first.startedAt);

            const response = await fetch(UPLOAD_URL, {
                method: 'POST',
                body: formData,
                credentials: 'same-origin',
                headers: { 'RequestVerificationToken': antiForgeryToken() }
            });
            // A redirect to the login page also comes back as 200 - only JSON counts as saved
            const result = response.ok ? await response.json().catch(function () { return null; }) : null;
            if (!result) {
                throw new Error('Audio upload failed (HTTP ' + response.status + ')');
            }
        }

        await idbDeleteSegment(key);
    }

    function withUploadLock(fn) {
        if (hasLocks) return navigator.locks.request(UPLOAD_LOCK, fn);
        return fn();
    }

    // Uploads every part except the one still being recorded (here or in another live tab).
    // Failed uploads stay in IndexedDB and are retried on the next check.
    function recoverFinishedSegments() {
        return withUploadLock(async function () {
            let liveKey = null;
            if (segment) {
                liveKey = segment.key;
            } else {
                const s = readState();
                if (s && s.active && s.status === 'recording' && s.ownerTabId !== tabId && await isOwnerAlive()) {
                    liveKey = segmentKey(s.sessionId, s.part);
                }
            }

            const keys = await idbListSegments();
            for (let i = 0; i < keys.length; i++) {
                if (keys[i] === liveKey) continue;
                try {
                    await uploadSegment(keys[i]);
                } catch (err) {
                    console.error('Recorder: upload of ' + keys[i] + ' failed, will retry', err);
                }
            }
        }).catch(function (err) {
            console.error('Recorder: recovery failed', err);
        });
    }

    // ---------- UI ----------

    function eachMicTable(fn) {
        if (!$.fn.dataTable) return;
        $.fn.dataTable.tables().forEach(function (table) {
            try {
                const dt = $(table).DataTable();
                if (dt.buttons && dt.buttons('audioStart:name').count() > 0) fn(dt);
            } catch (e) { /* table without Buttons */ }
        });
    }

    function micButtonsPresent() {
        let found = false;
        eachMicTable(function () { found = true; });
        return found;
    }

    function setMicButtonsState(startEnabled, stopEnabled) {
        eachMicTable(function (dt) {
            dt.buttons('audioStart:name')[startEnabled ? 'enable' : 'disable']();
            dt.buttons('audioStop:name')[stopEnabled ? 'enable' : 'disable']();
        });
    }

    function applyUi() {
        const s = readState();
        const active = !!(s && s.active);
        const interrupted = active && s.status === 'interrupted';
        // Start doubles as "Resume" when the recording was interrupted
        setMicButtonsState(!active || interrupted, active);
        if (interrupted) showBanner(); else hideBanner();
    }

    function showBanner() {
        if (!micButtonsPresent() || $('#rc-recorder-banner').length) return;
        const banner = $(
            '<div id="rc-recorder-banner" role="alert" style="position:fixed;top:10px;left:50%;transform:translateX(-50%);z-index:10000;' +
            'background:#c62828;color:#fff;padding:10px 16px;border-radius:5px;box-shadow:0 4px 8px rgba(0,0,0,.3);display:flex;gap:10px;align-items:center;">' +
            '<span><i class="fa fa-microphone-slash"></i> Voice recording was interrupted. Click Resume to continue recording.</span>' +
            '<button type="button" class="btn btn-sm btn-light" data-rc-action="resume">Resume recording</button>' +
            '<button type="button" class="btn btn-sm btn-outline-light" data-rc-action="stop">Stop</button>' +
            '</div>');
        banner.on('click', '[data-rc-action="resume"]', function () { start(); });
        banner.on('click', '[data-rc-action="stop"]', function () { stop(); });
        $('body').append(banner);
    }

    function hideBanner() {
        $('#rc-recorder-banner').remove();
    }

    // ---------- public actions ----------

    async function start() {
        if (busy || segment) return;
        busy = true;
        try {
            const s = readState();
            let sessionId = newId();
            let part = 1;
            let guardId = pageGuardId();
            if (s && s.active) {
                if (s.status === 'recording' && s.ownerTabId !== tabId && await isOwnerAlive()) {
                    return; // already recording in another tab
                }
                // Resume (or take over) the interrupted session with the next part
                sessionId = s.sessionId;
                part = (s.part || 0) + 1;
                guardId = s.guardId || guardId;
            }
            await startRecording(sessionId, part, guardId);
            hideBanner();
            recoverFinishedSegments();
        } catch (err) {
            console.error('Recorder: unable to start', err);
            releaseMic();
            recorder = null;
            segment = null;
            if (!(readState() || {}).active) {
                alert('Unable to start recording: ' + (err && err.message ? err.message : err));
            }
        } finally {
            busy = false;
            applyUi();
        }
    }

    async function stop() {
        if (busy) return;
        busy = true;
        try {
            const s = readState();
            if (segment || isOwner) {
                await stopAsOwner();
                return;
            }
            if (!s || !s.active) return;

            if (s.status === 'recording' && await isOwnerAlive()) {
                // Recording runs in another tab - ask it to stop and upload
                setMicButtonsState(false, false);
                lsSet(COMMAND_KEY, JSON.stringify({ cmd: 'stop', sessionId: s.sessionId, ts: Date.now() }));
                setTimeout(async function () {
                    const now = readState();
                    if (now && now.active && now.sessionId === s.sessionId && !(await isOwnerAlive())) {
                        clearState();
                        recoverFinishedSegments();
                    }
                    applyUi();
                }, STOP_COMMAND_TIMEOUT_MS);
            } else {
                // Interrupted, or the owner tab is gone - just end the session
                clearState();
                recoverFinishedSegments();
            }
        } finally {
            busy = false;
            applyUi();
        }
    }

    // Saves the chunk in progress; call before a programmatic reload
    function flush(timeoutMs) {
        if (!recorder || recorder.state !== 'recording') return Promise.resolve();
        const rec = recorder;
        return new Promise(function (resolve) {
            const done = function () {
                Promise.all(Array.from(pendingWrites)).then(resolve, resolve);
            };
            const timer = setTimeout(done, timeoutMs || 1500);
            rec.addEventListener('dataavailable', function () {
                clearTimeout(timer);
                setTimeout(done, 0); // let ondataavailable register its write first
            }, { once: true });
            try { rec.requestData(); } catch (e) { clearTimeout(timer); done(); }
        });
    }

    // ---------- startup ----------

    async function resumeAfterReload(s) {
        isOwner = true;
        becomeOwner();
        try {
            // No user click here - only works without a prompt when the site's mic permission is "Allow"
            await startRecording(s.sessionId, (s.part || 0) + 1, s.guardId || pageGuardId(), AUTO_RESUME_TIMEOUT_MS);
        } catch (err) {
            console.warn('Recorder: auto-resume failed', err);
            releaseMic();
            recorder = null;
            segment = null;
            const now = readState() || s;
            now.status = 'interrupted';
            now.ownerTabId = tabId;
            writeState(now);
        }
    }

    async function periodicCheck() {
        const s = readState();
        if (s && s.active && !isOwner && !(await isOwnerAlive())) {
            // Owner tab was closed - what it recorded is uploaded below
            clearState();
        }
        await recoverFinishedSegments();
        applyUi();
    }

    window.addEventListener('storage', function (e) {
        if (e.key === STATE_KEY) {
            const s = readState();
            if (isOwner && (!s || !s.active || s.ownerTabId !== tabId)) {
                // Stopped elsewhere, or another tab resumed the interrupted session
                stopLocal().then(function () {
                    resignOwner();
                    applyUi();
                    recoverFinishedSegments();
                });
                return;
            }
            applyUi();
        } else if (e.key === COMMAND_KEY && isOwner) {
            let cmd = null;
            try { cmd = JSON.parse(e.newValue); } catch (err) { /* ignore */ }
            const s = readState();
            if (cmd && cmd.cmd === 'stop' && s && cmd.sessionId === s.sessionId) {
                stopAsOwner();
            }
        }
    });

    // Hard reload / F5: try to save the chunk in progress (best effort)
    window.addEventListener('pagehide', function () {
        if (recorder && recorder.state === 'recording') {
            try { recorder.requestData(); } catch (e) { /* ignore */ }
        }
    });

    async function init() {
        const s = readState();
        if (s && s.active && s.ownerTabId === tabId) {
            if (await ownerLockHeld()) {
                // Duplicated tab (sessionStorage was copied) - the original tab is still the owner
                tabId = newId();
                try { window.sessionStorage.setItem(TAB_ID_KEY, tabId); } catch (e) { /* ignore */ }
            } else if (micButtonsPresent()) {
                await resumeAfterReload(s);
            }
        }
        applyUi();
        recoverFinishedSegments();
        setInterval(periodicCheck, RECOVERY_CHECK_MS);
    }

    window.RcRecorder = {
        start: start,
        stop: stop,
        flush: flush,
        isRecordingHere: function () { return !!segment; }
    };

    $(function () { init(); });

})(window, jQuery);
