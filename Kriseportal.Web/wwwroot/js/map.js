// Henter kartelementet og sjekker om kartet kan redigeres eller kun vises.
const mapElement = document.getElementById('map');
const mapMode = mapElement.dataset.mapMode || 'edit';

// Oppretter Leaflet-kartet med Kristiansand som standardvisning.
const map = L.map('map').setView([58.1467, 7.9956], 11);

// Legger til Kartverkets topografiske kartgrunnlag.
L.tileLayer('https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png', {
    attribution: '&copy; Kartverket',
    maxZoom: 18
}).addTo(map);

// Lagrer markøren slik at kun én valgt posisjon vises om gangen.
let marker;

// Henter koordinatfeltene fra skjemaet.
const latitudeInput = document.getElementById('Latitude');
const longitudeInput = document.getElementById('Longitude');

// Viser en eksisterende posisjon dersom koordinatene allerede er registrert.
if (latitudeInput && longitudeInput &&
    latitudeInput.value && longitudeInput.value) {

    const latitude = parseFloat(latitudeInput.value);
    const longitude = parseFloat(longitudeInput.value);

    marker = L.marker([latitude, longitude]).addTo(map);
    map.setView([latitude, longitude], 13);
}

// Tillater valg av posisjon kun når kartet er i redigeringsmodus.
if (mapMode === 'edit') {
    map.on('click', function (e) {
        const latitude = e.latlng.lat;
        const longitude = e.latlng.lng;

        if (latitudeInput && longitudeInput) {
            latitudeInput.value = latitude;
            longitudeInput.value = longitude;
        }

        if (marker) {
            marker.setLatLng([latitude, longitude]);
        } else {
            marker = L.marker([latitude, longitude]).addTo(map);
        }

        console.log("Latitude:", latitude);
        console.log("Longitude:", longitude);
    });
}