let nIntervId;
let DuressAlarmNotificationPending = false;
const duration = 60 * 4;
var isPaused = false;
var sitebuttonSelectedClientTypeSiteId = -1;
var sitebuttonSelectedClientSiteId = -1;
//p4#48 AudioNotification - Binoy - 12-01-2024 -- Start
let playAlarm = 0;
let audiourl = '/NotificationSound/duressAlarm01.mp3'
const audio = new Audio(audiourl);
audio.loop = true;
//p4#48 AudioNotification - Binoy - 12-01-2024 -- End

// Task p6#73_TimeZone issue -- added by Binoy - Start
var tmzdata = {
    'EventDateTimeLocal': null,
    'EventDateTimeLocalWithOffset': null,
    'EventDateTimeZone': null,
    'EventDateTimeZoneShort': null,
    'EventDateTimeUtcOffsetMinute': null,
};

// 07-05-2026 - MP4 Video Playback Support - Global Definition
window.openVideoPlayer = function (url) {
    var video = document.getElementById('videoPlayer');
    var source = document.getElementById('videoSource');
    if (source && video) {
        source.src = url;
        video.load();
        $('#videoPlayerModal').modal('show');
        video.play();
    }
}

window.closeVideoPlayer = function () {
    var video = document.getElementById('videoPlayer');
    if (video) {
        video.pause();
        video.src = "";
    }
    $('#videoPlayerModal').modal('hide');
}
// Task p6#73_TimeZone issue -- added by Binoy - End
let connection;
window.onload = function () {
    if (document.querySelector('#clockRefresh')) {
        startClockNew();

    }

};

function CheckUnreadMessages() {
    $.get('/RadioCheckV2?handler=CheckUnreadMessages', function (data) {
        if (data && data.length > 0) {
            data.forEach(function (msg) {
                playNotificationSound();
                showNotificationSlider(msg.notes, true, msg.id);
            });
        }
    });
}

//Audio Playback
function playNotificationSound() {
    // Check if audio element exists, if not create it (RadioCheckV2.cshtml has it, but just in case)
    var audio = document.getElementById("audioPlayback");
    if (!audio) {
        // Create if missing
        audio = document.createElement("audio");
        audio.id = "audioPlayback";
        audio.style.display = "none";
        document.body.appendChild(audio);
    }

    if (audio) {
        audio.src = '/NotificationSound/mixkit-bell-notification-933.wav';
        audio.play().catch(function (error) {
            console.log("Autoplay prevented or audio error:", error);
        });
    }
}

//Visual Slider
function showNotificationSlider(message, isFromGuard, id) {
    var container = $('#notification-slider-container');
    if (container.length === 0) {
        $('body').append('<div id="notification-slider-container" style="position: fixed; bottom: 20px; right: 20px; z-index: 9999; display: flex; flex-direction: column-reverse; align-items: flex-end; gap: 10px;"></div>');
        container = $('#notification-slider-container');
    }

    var slider = $('<div class="notification-slider" style="background-color: #f44336; color: white; padding: 15px; border-radius: 5px; box-shadow: 0 4px 8px rgba(0,0,0,0.2); width: 300px; display: none;">' +
        '<div style="display: flex; justify-content: space-between; align-items: start;">' +
        '<span><i class="fa fa-bell"></i> <strong>New Message from Guard</strong></span>' +
        '<button type="button" class="close text-white" aria-label="Close" style="opacity: 1;">' +
        '<span aria-hidden="true">&times;</span>' +
        '</button>' +
        '</div>' +
        '<div style="margin-top: 10px; word-break: break-word;">' + message + '</div>' +
        '</div>');

    container.append(slider);
    slider.slideDown();

    slider.find('.close').on('click', function () {
        slider.slideUp(function () {
            $(this).remove();
            // Ack message on close
            if (id) {
                $.post('/RadioCheckV2?handler=AckMessage', { id: id, __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val() });
            }
        });
    });
}
let map;
$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();

    $(document).on('show.bs.modal', '.modal', function () {
        const zIndex = 1040 + 10 * $('.modal:visible').length;
        $(this).css('z-index', zIndex);
        setTimeout(() => $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack'));
    });

    $(document).on('hidden.bs.modal', '.modal', () => $('.modal:visible').length && $(document.body).addClass('modal-open'));

    localStorage.removeItem('activeTab');
     map = L.map('map').setView([-27.0, 133.0], 5); // Center on Australia
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
    }).addTo(map);
    $('#ClientType').multiselect({
        includeSelectAllOption: true,
        buttonWidth: '153px',
        nonSelectedText: 'Client Type',
        buttonTextAlignment: 'left'
    });
    $('#ClientSiteId').multiselect({
        includeSelectAllOption: true,
        buttonWidth: '153px',
        nonSelectedText: 'Client Site',
        buttonTextAlignment: 'left'
    });
    $('#ClientType').on('change', function () {
        $('#ClientSiteId').empty();
        $('#StateDrp').val('');
        const clientTypeId = $(this).val().join(';')
        const clientSiteControl = $('#ClientSiteId');
        clientSiteControl.html('');
        isPaused = true;

        $.ajax({

            url: '/Admin/Settings?handler=ClientSitesNew&typeId=' + clientTypeId,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
                });
                clientSiteControl.multiselect('rebuild');
                let allClientSiteIds = data.map(site => site.id);
                if (allClientSiteIds) {
                    updateMapSite(allClientSiteIds);
                }
            }
        });


    });
    $('#ClientSiteId').on('change', function () {


        const selectedState = $(this).val(); // Get the selected state
        isPaused = true;
        if (selectedState) {
            updateMapSite(selectedState);
        }
    });
    $('#btnRefresh').on('click', function () {
        location.reload();
    });
    $('#btnRefreshActivityStatusNew').on('click', function () {
        ClearTimerAndReloadForMap();
    });
    $('#txtSearchMap').on('change', function () {

        const searchText = $('#txtSearchMap').val().trim().toLowerCase();

        clearMarkers();
        fetch(`/RadioCheckV2?handler=ClientSiteActivityStatus&clientSiteIds=${clientSiteIds}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' },
        })
            .then(response => response.json())
            .then(data => {
                data.forEach(record => {
                    const gps = record.gps ? record.gps.trim() : '';
                    const address = record.address ? stripHtml(record.address).trim() : '';
                    const GuardName = record.guardName;

                    const siteNameParts = record.siteName.split('&nbsp;');
                    const siteName = siteNameParts[0].trim();
                    const phoneNumber = siteNameParts.slice(1).join('').trim();
                    const siteNameLower = siteName.toLowerCase();   // <-- added

                    // --- FILTER LOGIC ---
                    if (!(siteNameLower.includes(searchText) || GuardName.includes(searchText))) {
                        return;   // skip this marker
                    }
                    const alertColor = 'Green';
                    const markerColor = getColorFromAlert(alertColor);

                    if (gps) {
                        if (record.tourMode != 'PCAR') {
                            const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                            L.marker([lat, lng], { icon: createCustomIcon(markerColor) })
                                .bindPopup(`<strong>SiteName:</strong> ${siteName} <br>
                                            <strong>Phone Number:</strong> ${phoneNumber} <br>
                                            <strong>Address:</strong> ${address} <br>
                                            <strong>GuardName:</strong> ${GuardName}`)
                                .addTo(map);
                        }
                        else {
                            const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                            L.marker([lat, lng], { icon: createPatrolCarIcon(markerColor, false) })
                                .bindPopup('<strong>SiteName:</strong>' + siteName + '<br>' + '<strong>Phone Number:</strong>' + phoneNumber + '<br>' + '<strong>Address:</strong>' + address + '<br>' + '<strong>GuardName:</strong>' + GuardName)
                                .addTo(map);
                        }
                    }
                });
                fetch(`/RadioCheckV2?handler=ClientSiteInActivityStatus&clientSiteIds=${clientSiteIds}`, {
                    method: 'GET',
                    headers: { 'Content-Type': 'application/json' },
                })
                    .then(response => response.json())
                    .then(data => {
                        data.forEach(record => {
                            const gps = record.gps ? record.gps.trim() : ''; // Extract GPS
                            const address = record.address ? stripHtml(record.address).trim() : ''; // Clean up address
                            const GuardName = record.guardName;
                            const siteNameParts = record.siteName.split('&nbsp;');
                            const siteName = siteNameParts[0].trim();
                            const phoneNumber = siteNameParts.slice(1).join('').trim();
                            const siteNameLower = siteName.toLowerCase();   // <-- added

                            // --- FILTER LOGIC ---
                            if (!(siteNameLower.includes(searchText) || GuardName.includes(searchText))) {
                                return;   // skip this marker
                            }
                            const alertColor = record.twoHrAlert ? record.twoHrAlert.trim() : 'grey'; // Default to grey if no color

                            // Map alert colors to actual CSS color values
                            const markerColor = getColorFromAlert(alertColor);

                            if (gps) {
                                if (record.tourMode != 'PCAR') {
                                    const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                                    L.marker([lat, lng], { icon: createCustomIcon(markerColor) })
                                        .bindPopup('<strong>SiteName:</strong>' + siteName + '<br>' + '<strong>Phone Number:</strong>' + phoneNumber + '<br>' + '<strong>Address:</strong>' + address + '<br>' + '<strong>GuardName:</strong>' + GuardName)
                                        .addTo(map);
                                }
                                else {
                                    const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                                    L.marker([lat, lng], { icon: createPatrolCarIcon(markerColor, false) })
                                        .bindPopup('<strong>SiteName:</strong>' + siteName + '<br>' + '<strong>Phone Number:</strong>' + phoneNumber + '<br>' + '<strong>Address:</strong>' + address + '<br>' + '<strong>GuardName:</strong>' + GuardName)
                                        .addTo(map);
                                }

                            }
                        });
                    })
                    .catch(error => console.error('Error:', error));
            })
            .catch(error => console.error('Error:', error));

    });
});
function startClockNew() {
    let timer = duration, minutes, seconds;
    display = document.querySelector('#clockRefresh');
    if (!nIntervId) {
        nIntervId = setInterval(function () {

            if (!isPaused) {
                minutes = parseInt(timer / 60, 10);
                seconds = parseInt(timer % 60, 10);

                minutes = minutes < 10 ? "0" + minutes : minutes;
                seconds = seconds < 10 ? "0" + seconds : seconds;

                display.textContent = minutes + " min" + " " + seconds + " sec";

                if (--timer < 0) {
                    //location.reload();

                }

            }
        }, 1000);
    }
}

function connectToSignalRservice() {
    var signalRconnHuburl = $('#txtSignalRConnectionUrl').val() + "/updateHub";
    connection = new signalR.HubConnectionBuilder().withUrl(signalRconnHuburl).configureLogging(signalR.LogLevel.Information).build();
    connection.on("ReceiveDuressAlarmAlert", function () {
        console.log("Duress Alarm Alert Received");
        DuressAlarmNotificationPending = true;
    });

    connection.start().then(function () {
        console.log('SignalR connection established.');
    }).catch(function (err) {
        return console.error(err.toString());
    });
}



function ClearTimerAndReloadForMap() {
    clearInterval(nIntervId);
    DuressAlarmNotificationPending = false;
    nIntervId = null;
    location.reload();
}


setInterval(() => {
    location.reload();
}, 180000);
const urlParams = new URLSearchParams(window.location.search);
const clientSiteIds = urlParams.get('clientSiteIds');



fetch(`/RadioCheckV2?handler=ClientSiteActivityStatus&clientSiteIds=${clientSiteIds}`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
})
    .then(response => response.json())
    .then(data => {
        data.forEach(record => {
            const gps = record.gps ? record.gps.trim() : '';
            const address = record.address ? stripHtml(record.address).trim() : '';
            const GuardName = record.guardName;
            const siteNameParts = record.siteName.split('&nbsp;');
            const siteName = siteNameParts[0].trim();
            const phoneNumber = siteNameParts.slice(1).join('').trim();
            const alertColor = 'Green';
            const markerColor = getColorFromAlert(alertColor);

            if (gps) {
                const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                L.marker([lat, lng], { icon: createCustomIcon(markerColor) })
                    .bindPopup(`<strong>SiteName:</strong> ${siteName} <br>
                                    <strong>Phone Number:</strong> ${phoneNumber} <br>
                                    <strong>Address:</strong> ${address} <br>
                                    <strong>GuardName:</strong> ${GuardName}`)
                    .addTo(map);
            }
        });
        fetch(`/RadioCheckV2?handler=ClientSiteInActivityStatus&clientSiteIds=${clientSiteIds}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' },
        })
            .then(response => response.json())
            .then(data => {
                data.forEach(record => {
                    const gps = record.gps ? record.gps.trim() : ''; // Extract GPS
                    const address = record.address ? stripHtml(record.address).trim() : ''; // Clean up address
                    const GuardName = record.guardName;
                    const siteNameParts = record.siteName.split('&nbsp;');
                    const siteName = siteNameParts[0].trim();
                    const phoneNumber = siteNameParts.slice(1).join('').trim();

                    const alertColor = record.twoHrAlert ? record.twoHrAlert.trim() : 'grey'; // Default to grey if no color

                    // Map alert colors to actual CSS color values
                    const markerColor = getColorFromAlert(alertColor);

                    if (gps) {
                        const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                        L.marker([lat, lng], { icon: createCustomIcon(markerColor) })
                            .bindPopup('<strong>SiteName:</strong>' + siteName + '<br>' + '<strong>Phone Number:</strong>' + phoneNumber + '<br>' + '<strong>Address:</strong>' + address + '<br>' + '<strong>GuardName:</strong>' + GuardName)
                            .addTo(map);

                    }
                });
            })
            .catch(error => console.error('Error:', error));
    })
    .catch(error => console.error('Error:', error));

function stripHtml(input) {
    const doc = new DOMParser().parseFromString(input, 'text/html');
    return doc.body.textContent || "";
}
function getColorFromAlert(alert) {
    switch (alert) {
        case 'Red': return 'red';
        case 'Green': return 'green';
        case 'Yellow': return 'yellow';
        default: return 'grey';
    }
}
function createCustomIcon(color) {
    return L.divIcon({
        className: 'custom-marker',
        html: `<div style="background-color:${color}; width: 25px; height: 25px; border-radius: 50%;animation: blink 1s infinite;"></div>`,
        iconSize: [25, 25],
    });
}
function createPatrolCarIcon(color, isBlinking = false) {
    return L.divIcon({
        className: 'patrol-car-marker',
        html: `
            <div style="
                width:64px;
                height:64px;
                display:flex;
                align-items:center;
                justify-content:center;
                ${isBlinking ? 'animation: blink 1s infinite;' : ''}
            ">
                <svg width="60" height="60" viewBox="0 0 64 64">
                    <path fill="${color}" d="M53 28h-2.1l-4.2-9.4A5 5 0 0 0 42.2 16H21.8a5 5 0 0 0-4.5 2.6L13.1 28H11a3 3 0 0 0-3 3v11a3 3 0 0 0 3 3h2a6 6 0 0 0 12 0h14a6 6 0 0 0 12 0h2a3 3 0 0 0 3-3V31a3 3 0 0 0-3-3zM21.8 20h20.4a1 1 0 0 1 .9.5l3.4 7.5H17.5l3.4-7.5a1 1 0 0 1 .9-.5zM19 48a2 2 0 1 1 0-4 2 2 0 0 1 0 4zm26 0a2 2 0 1 1 0-4 2 2 0 0 1 0 4z"/>
                </svg>
            </div>
        `,
        iconSize: [64, 64],
        iconAnchor: [32, 32],
        popupAnchor: [0, -32]
    });
}


function updateMapSite(state) {

    fetch(`/RadioCheckV2?handler=ClientSiteActivityStatusClientSiteForGlobeMap&ClientSiteId=${state}`, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
        .then(response => response.json())
        .then(data => {
            clearMarkers();
            data.forEach(record => {
                const gps = record.gps ? record.gps.trim() : '';
                const address = record.address ? stripHtml(record.address).trim() : '';
                const GuardName = record.guardName;
                const siteNameParts = record.siteName.split('&nbsp;');
                const siteName = siteNameParts[0].trim();
                const phoneNumber = siteNameParts.slice(1).join('').trim();
                const alertColor = 'Green';
                const markerColor = getColorFromAlert(alertColor);

                if (gps) {
                    if (record.tourMode != 'PCAR') {
                        const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                        L.marker([lat, lng], { icon: createCustomIcon(markerColor) })
                            .bindPopup(`<strong>SiteName:</strong> ${siteName} <br>
                                                    <strong>Phone Number:</strong> ${phoneNumber} <br>
                                                    <strong>Address:</strong> ${address} <br>
                                                    <strong>GuardName:</strong> ${GuardName}`)
                            .addTo(map);
                    }
                    else {
                        const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                        L.marker([lat, lng], { icon: createPatrolCarIcon(markerColor, false) })
                            .bindPopup('<strong>SiteName:</strong>' + siteName + '<br>' + '<strong>Phone Number:</strong>' + phoneNumber + '<br>' + '<strong>Address:</strong>' + address + '<br>' + '<strong>GuardName:</strong>' + GuardName)
                            .addTo(map);
                    }
                }
            });
            // let stateIds = state.toString().split(',');

            // let dataIds = data.map(record => record.clientSiteId.toString());
            // let missingIds = stateIds.filter(id => !dataIds.includes(id));
            // state = missingIds.join(',');
            updateMapSiteInactive(state);
        })
        .catch(error => console.error('Error:', error));

}

function clearMarkers() {
    map.eachLayer(function (layer) {
        if (layer instanceof L.Marker) {
            map.removeLayer(layer);
        }
    });
}
function updateMapSiteInactive(state) {
    fetch(`/RadioCheckV2?handler=ClientSiteInActivityStatusClientSite&ClientSites=${state}`, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
        .then(response => response.json())
        .then(data => {
            //clearMarkers();
            data.forEach(record => {
                const gps = record.gps ? record.gps.trim() : ''; // Extract GPS
                const address = record.address ? stripHtml(record.address).trim() : ''; // Clean up address
                const GuardName = record.guardName;
                const siteNameParts = record.siteName.split('&nbsp;');
                const siteName = siteNameParts[0].trim();
                const phoneNumber = siteNameParts.slice(1).join('').trim();

                const alertColor = record.twoHrAlert ? record.twoHrAlert.trim() : 'grey'; // Default to grey if no color

                // Map alert colors to actual CSS color values
                const markerColor = getColorFromAlert(alertColor);

                if (gps) {
                    if (record.tourMode != 'PCAR') {
                        const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                        L.marker([lat, lng], { icon: createCustomIcon(markerColor) })
                            .bindPopup('<strong>SiteName:</strong>' + siteName + '<br>' + '<strong>Phone Number:</strong>' + phoneNumber + '<br>' + '<strong>Address:</strong>' + address + '<br>' + '<strong>GuardName:</strong>' + GuardName)
                            .addTo(map);
                    }
                    else {
                        const [lat, lng] = gps.split(',').map(coord => parseFloat(coord));
                        L.marker([lat, lng], { icon: createPatrolCarIcon(markerColor, false) })
                            .bindPopup('<strong>SiteName:</strong>' + siteName + '<br>' + '<strong>Phone Number:</strong>' + phoneNumber + '<br>' + '<strong>Address:</strong>' + address + '<br>' + '<strong>GuardName:</strong>' + GuardName)
                            .addTo(map);
                    }
                }
            });
        })
        .catch(error => console.error('Error:', error));
}



















