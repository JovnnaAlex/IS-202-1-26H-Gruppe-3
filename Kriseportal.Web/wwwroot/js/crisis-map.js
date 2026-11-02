// Situasjonskartet: viser behov og ressurser fra databasen på Kartverkets kart.
// Koden er delt i små funksjoner, slik at fargede kommuner og beslutningsstøtte
// senere kan bygges oppå de samme delene.
(function () {
    'use strict';

    const DATA_URL = '/Map/Data';

    // Kartverkets gråtonekart gjør at fargene på kartet synes godt
    const KARTLAG = {
        graatone: 'https://cache.kartverket.no/v1/wmts/1.0.0/topograatone/default/webmercator/{z}/{y}/{x}.png',
        topo: 'https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png'
    };

    // Felles tilstand for kartet
    const state = {
        map: null,
        pointLayer: null,
        points: [],
        firstLoad: true
    };

    function initMap() {
        state.map = L.map('crisis-map', { zoomControl: false }).setView([58.65, 8.0], 8);

        // Zoom-knappene nede til høyre, der tommelen når på mobil
        L.control.zoom({ position: 'bottomright' }).addTo(state.map);

        L.tileLayer(KARTLAG.graatone, {
            attribution: '&copy; Kartverket',
            maxZoom: 18
        }).addTo(state.map);

        // Eget lag for punktene. Fargede kommuner får et eget lag senere.
        state.pointLayer = L.layerGroup().addTo(state.map);
    }

    async function loadData() {
        const updated = document.getElementById('map-updated');

        try {
            const response = await fetch(DATA_URL, { headers: { 'Accept': 'application/json' } });
            if (!response.ok) {
                throw new Error('HTTP ' + response.status);
            }

            const data = await response.json();
            state.points = data.points;
            renderPoints(state.points);

            const time = new Date(data.updatedAt).toLocaleTimeString('nb-NO', { hour: '2-digit', minute: '2-digit' });
            updated.textContent = 'Oppdatert kl. ' + time;
        } catch (error) {
            console.error('Kunne ikke hente kartdata', error);
            updated.textContent = 'Klarte ikke å hente data';
        }
    }

    // Tegner punktene. Beslutningsstøtten kan senere kalle denne med egne punkter.
    function renderPoints(points) {
        state.pointLayer.clearLayers();
        const bounds = [];

        points.forEach(function (point) {
            const marker = L.circleMarker([point.lat, point.lng], {
                radius: 9,
                color: '#ffffff',
                weight: 2,
                fillColor: point.kind === 'need' ? '#C62828' : '#2E7D32',
                fillOpacity: 0.95
            });

            marker.bindPopup(
                '<strong>' + escapeHtml(point.title) + '</strong><br>' +
                escapeHtml(point.category) + ' · ' + escapeHtml(point.municipality)
            );

            marker.addTo(state.pointLayer);
            bounds.push([point.lat, point.lng]);
        });

        // Zoomer til alle punktene første gang, men ikke ved oppdatering
        if (state.firstLoad && bounds.length > 0) {
            state.map.fitBounds(bounds, { padding: [60, 60], maxZoom: 12 });
            state.firstLoad = false;
        }
    }

    // Hindrer at tekst fra brukerne kan kjøre som kode i nettleseren (XSS)
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text ?? '';
        return div.innerHTML;
    }

    document.addEventListener('DOMContentLoaded', function () {
        initMap();
        loadData();
        document.getElementById('map-refresh').addEventListener('click', loadData);
    });
})();