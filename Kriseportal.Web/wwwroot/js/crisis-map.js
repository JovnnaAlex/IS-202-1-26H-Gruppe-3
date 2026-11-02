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

    // Prioritet på behov: CSS-klasse, farge, norsk tekst, størrelse og hvem som legges øverst.
    // Høy og normal har samme farge (haster), men høy er større.
    const PRIORITET = {
        Acute: { css: 'km-p-acute', farge: '#C62828', tekst: 'Akutt', size: 42, z: 400 },
        High: { css: 'km-p-high', farge: '#F57C00', tekst: 'Høy', size: 36, z: 300 },
        Normal: { css: 'km-p-normal', farge: '#F57C00', tekst: 'Normal', size: 28, z: 200 },
        Planned: { css: 'km-p-planned', farge: '#F9A825', tekst: 'Planlagt', size: 28, z: 100 }
    };

    // Tilgjengelighet på ressurser
    const TILGJENGELIGHET = {
        Available: { css: 'km-a-available', farge: '#2E7D32', tekst: 'Tilgjengelig' },
        Reserved: { css: 'km-a-reserved', farge: '#6A1B9A', tekst: 'Reservert' },
        Unavailable: { css: 'km-a-unavailable', farge: '#455A64', tekst: 'Ikke tilgjengelig' }
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
        firstLoad: true,
        selectedMarker: null
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

        // Trykk på selve kartet lukker detaljpanelet
        state.map.on('click', closePanel);
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
        state.selectedMarker = null;
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

    // Finner stilen (farge, størrelse, tekst) for et punkt
    function styleFor(point) {
        return point.kind === 'need'
            ? (PRIORITET[point.priority] || PRIORITET.Normal)
            : (TILGJENGELIGHET[point.status] || TILGJENGELIGHET.Available);
    }

    // Lager HTML for markøren, brukes både på kartet og i panelet
    function markerHtml(point) {
        const isNeed = point.kind === 'need';
        const classes = [
            'km',
            isNeed ? 'km-need' : 'km-res',
            styleFor(point).css,
            point.isSimulated ? 'km-simulated' : ''
        ].join(' ');

        return '<div class="' + classes + '">' + lagIkonHtml(point.category) + '</div>';
    }

    // Lager én markør med riktig form, farge, størrelse og ikon
    function createMarker(point) {
        const isNeed = point.kind === 'need';
        const style = styleFor(point);
        const size = isNeed ? style.size : 30;

        const icon = L.divIcon({
            className: '',
            html: markerHtml(point),
            iconSize: [size, size],
            iconAnchor: [size / 2, size / 2]
        });

        const marker = L.marker([point.lat, point.lng], {
            icon: icon,
            // Behov over ressurser, og de mest akutte øverst
            zIndexOffset: isNeed ? style.z : 0,
            title: point.title,
            alt: point.title,
            keyboard: true
        });

        marker.on('click', function (event) {
            // Hindrer at klikket også lukker panelet via kartet
            L.DomEvent.stopPropagation(event);
            openPanel(point, marker);
        });

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

    // Fyller og åpner detaljpanelet
    function openPanel(point, marker) {
        const isNeed = point.kind === 'need';
        const style = styleFor(point);

        // Marker valgt punkt på kartet
        highlightMarker(marker);

        document.getElementById('panel-icon').innerHTML = markerHtml(point);
        document.getElementById('panel-kind').textContent = isNeed ? 'Behov' : 'Ressurs';
        document.getElementById('panel-title').textContent = point.title;

        const chip = document.getElementById('panel-chip');
        chip.textContent = isNeed
            ? style.tekst + ' · ' + (STATUS[point.status] || point.status)
            : style.tekst;
        chip.style.background = style.farge;

        // Fakta som liste. Teksten settes med textContent, så den ikke kan kjøre som kode.
        const facts = document.getElementById('panel-facts');
        facts.innerHTML = '';
        addFact(facts, 'Kategori', point.category);
        addFact(facts, isNeed ? 'Trenger' : 'Kan bidra med', point.quantity + ' ' + point.unit);
        addFact(facts, 'Kommune', point.municipality);
        addFact(facts, 'Kontakt', point.contactName);
        addFact(facts, 'Telefon', point.contactPhone);

        // Ring-knappen åpner telefonen direkte på mobil
        const phone = (point.contactPhone || '').replace(/\s/g, '');
        const call = document.getElementById('panel-call');
        call.href = 'tel:' + phone;
        call.setAttribute('aria-label', 'Ring ' + point.contactName);

        // Veibeskrivelse åpner kartappen på telefonen med ruten dit
        document.getElementById('panel-route').href =
            'https://www.google.com/maps/dir/?api=1&destination=' + point.lat + ',' + point.lng;

        const panel = document.getElementById('map-panel');
        panel.classList.add('is-open');
        panel.setAttribute('aria-hidden', 'false');
        document.getElementById('panel-close').focus();

        // Flytter kartet litt, så punktet ikke skjules bak panelet
        state.map.panTo([point.lat, point.lng], { animate: true });
    }

    function addFact(list, label, value) {
        const dt = document.createElement('dt');
        dt.textContent = label;
        const dd = document.createElement('dd');
        dd.textContent = value ?? '';
        list.appendChild(dt);
        list.appendChild(dd);
    }

    function closePanel() {
        const panel = document.getElementById('map-panel');
        panel.classList.remove('is-open');
        panel.setAttribute('aria-hidden', 'true');
        highlightMarker(null);
    }

    // Gjør valgt markør større, og fjerner markeringen fra forrige
    function highlightMarker(marker) {
        if (state.selectedMarker) {
            const old = state.selectedMarker.getElement()?.querySelector('.km');
            old?.classList.remove('km-selected');
        }
        state.selectedMarker = marker;
        if (marker) {
            marker.getElement()?.querySelector('.km')?.classList.add('km-selected');
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        // Tegnforklaringen er foldet sammen på små skjermer, så kartet får plass
        if (window.innerWidth < 768) {
            document.getElementById('map-legend').removeAttribute('open');
        }

        initMap();
        loadData();

        document.getElementById('map-refresh').addEventListener('click', loadData);
        document.getElementById('panel-close').addEventListener('click', closePanel);

        // Esc lukker panelet
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closePanel();
            }
        });
    });
})();