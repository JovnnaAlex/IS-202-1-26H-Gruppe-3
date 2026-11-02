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

    // Prioritet på behov: CSS-klasse, norsk tekst, størrelse og hvem som legges øverst.
    // Høy og normal har samme farge (haster), men høy er større.
    const PRIORITET = {
        Acute: { css: 'km-p-acute', tekst: 'Akutt', size: 42, z: 400 },
        High: { css: 'km-p-high', tekst: 'Høy', size: 36, z: 300 },
        Normal: { css: 'km-p-normal', tekst: 'Normal', size: 28, z: 200 },
        Planned: { css: 'km-p-planned', tekst: 'Planlagt', size: 28, z: 100 }
    };

    // Tilgjengelighet på ressurser
    const TILGJENGELIGHET = {
        Available: { css: 'km-a-available', tekst: 'Tilgjengelig' },
        Reserved: { css: 'km-a-reserved', tekst: 'Reservert' },
        Unavailable: { css: 'km-a-unavailable', tekst: 'Ikke tilgjengelig' }
    };

    // Status på behov på norsk
    const STATUS = {
        New: 'Ny',
        UnderReview: 'Vurderes',
        Assigned: 'Tildelt',
        Resolved: 'Løst'
    };

    // Ikon per kategori (Bootstrap Icons), så man ser hva det gjelder uten å klikke
    const KATEGORI_IKON = {
        'Transport': 'truck',
        'Strøm': 'lightning-charge-fill',
        'Snørydding': 'snow',
        'Sand og grus': 'minecart-loaded',
        'Maskiner': 'tools',
        'Evakuering': 'person-walking',
        'Samband': 'broadcast-pin',
        'Mannskap': 'people-fill',
        'Lokaler': 'house-fill',
        'Båt': 'water',
        'Vann': 'droplet-fill',
        'Mat': 'basket-fill',
        'Varme': 'fire'
    };

    // Egne ikoner for kategorier som ikke finnes i Bootstrap Icons.
    // Drone: firerotors drone sett ovenfra.
    const EGNE_IKON = {
        'Drone':
            '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">' +
            '<circle cx="5" cy="5" r="3"/><circle cx="19" cy="5" r="3"/>' +
            '<circle cx="5" cy="19" r="3"/><circle cx="19" cy="19" r="3"/>' +
            '<path d="M7.5 7.5l3 3M16.5 7.5l-3 3M7.5 16.5l3-3M16.5 16.5l-3-3"/>' +
            '<rect x="9.5" y="9.5" width="5" height="5" rx="1" fill="currentColor"/>' +
            '</svg>'
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
            createMarker(point).addTo(state.pointLayer);
            bounds.push([point.lat, point.lng]);
        });

        // Zoomer til alle punktene første gang, men ikke ved oppdatering
        if (state.firstLoad && bounds.length > 0) {
            state.map.fitBounds(bounds, { padding: [60, 60], maxZoom: 12 });
            state.firstLoad = false;
        }
    }

    // Lager én markør med riktig form, farge, størrelse og ikon
    function createMarker(point) {
        const isNeed = point.kind === 'need';
        const style = isNeed
            ? (PRIORITET[point.priority] || PRIORITET.Normal)
            : (TILGJENGELIGHET[point.status] || TILGJENGELIGHET.Available);
        const size = isNeed ? style.size : 30;

        const classes = [
            'km',
            isNeed ? 'km-need' : 'km-res',
            style.css,
            point.isSimulated ? 'km-simulated' : ''
        ].join(' ');

        const icon = L.divIcon({
            className: '',
            html: '<div class="' + classes + '">' + lagIkonHtml(point.category) + '</div>',
            iconSize: [size, size],
            iconAnchor: [size / 2, size / 2],
            popupAnchor: [0, -size / 2]
        });

        const marker = L.marker([point.lat, point.lng], {
            icon: icon,
            // Behov over ressurser, og de mest akutte øverst
            zIndexOffset: isNeed ? style.z : 0,
            title: point.title,
            alt: point.title
        });

        marker.bindPopup(createPopup(point, isNeed));
        return marker;
    }

    // Bruker eget SVG-ikon hvis det finnes, ellers Bootstrap Icons
    function lagIkonHtml(kategori) {
        if (EGNE_IKON[kategori]) {
            return EGNE_IKON[kategori];
        }
        const ikon = KATEGORI_IKON[kategori] || 'three-dots';
        return '<i class="bi bi-' + ikon + '" aria-hidden="true"></i>';
    }

    function createPopup(point, isNeed) {
        const merkelapp = isNeed
            ? (PRIORITET[point.priority]?.tekst || '') + ' · ' + (STATUS[point.status] || point.status)
            : (TILGJENGELIGHET[point.status]?.tekst || point.status);

        return '<strong>' + escapeHtml(point.title) + '</strong><br>' +
            escapeHtml(point.category) + ' · ' + escapeHtml(point.municipality) + '<br>' +
            '<small>' + escapeHtml(merkelapp) + '</small>';
    }

    // Hindrer at tekst fra brukerne kan kjøre som kode i nettleseren (XSS)
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text ?? '';
        return div.innerHTML;
    }

    document.addEventListener('DOMContentLoaded', function () {
        // Tegnforklaringen er foldet sammen på små skjermer, så kartet får plass
        if (window.innerWidth < 768) {
            document.getElementById('map-legend').removeAttribute('open');
        }

        initMap();
        loadData();
        document.getElementById('map-refresh').addEventListener('click', loadData);
    });
})();