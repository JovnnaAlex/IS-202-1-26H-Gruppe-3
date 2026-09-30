// Gets the map element and determines whether the map is editable or view-only.
const mapElement = document.getElementById('map');
const mapMode = mapElement.dataset.mapMode || 'edit';

// Initializes the Leaflet map and sets the default view to Southern Norway.
const map = L.map('map').setView([58.15, 8.0], 8);

// Adds the OpenStreetMap map layer.
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors'
}).addTo(map);

// Stores the current marker so only one selected location is shown.
let marker;

// Gets the coordinate fields from the form.
const latitudeInput = document.getElementById('Latitude');
const longitudeInput = document.getElementById('Longitude');

// Shows an existing location when coordinates are already available.
if (latitudeInput && longitudeInput &&
    latitudeInput.value && longitudeInput.value) {

    const latitude = parseFloat(latitudeInput.value);
    const longitude = parseFloat(longitudeInput.value);

    marker = L.marker([latitude, longitude]).addTo(map);
    map.setView([latitude, longitude], 13);
}

// Allows location selection only when the map is in edit mode.
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