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

        // ---------- B30: faredata fra NVE ----------

    // NVEs aktsomhetskart hentes som bilder fra NVEs kartserver.
    // lag = id-ene til lagene i tjenesten. minZoom = når NVE begynner å tegne dem.
    const NVE_URL = 'https://kart.nve.no/enterprise/rest/services/';
    const FARELAG = [
        { navn: 'Flomfare (zoom inn)', css: 'lg-flom', tjeneste: 'Flomaktsomhet', lag: '1', minZoom: 12 },
        { navn: 'Jord- og flomskred', css: 'lg-skred', tjeneste: 'JordFlomskredAktsomhet', lag: '0,1', minZoom: 5 },
        { navn: 'Kvikkleireskred (zoom inn)', css: 'lg-kvikkleire', tjeneste: 'KvikkleireskredAktsomhet', lag: '0', minZoom: 13 }
    ];

    // Regner ut hvilket område (i meter) én kartflis dekker.
    // Kartet deles i 2^zoom × 2^zoom fliser, og verden er 2 × 20 037 508 meter bred.
    function flisTilBbox(x, y, z) {
        const halvVerden = 20037508.342789244;
        const flisBredde = (2 * halvVerden) / Math.pow(2, z);
        const xmin = -halvVerden + x * flisBredde;
        const ymax = halvVerden - y * flisBredde;
        return [xmin, ymax - flisBredde, xmin + flisBredde, ymax];
    }

    // Et Leaflet-kartlag som ber NVE om ett bilde for hver kartflis
    const NveLag = L.TileLayer.extend({
        getTileUrl: function (coords) {
            return NVE_URL + this.options.tjeneste + '/MapServer/export' +
                '?bbox=' + flisTilBbox(coords.x, coords.y, coords.z).join(',') +
                '&bboxSR=3857&imageSR=3857&size=256,256' +
                '&format=png32&transparent=true&f=image' +
                '&layers=show:' + this.options.lag;
        }
    });

    // Legger NVE-lagene i en lagvelger nede til høyre, der man slår dem av og på
    function addHazardLayers() {
        const overlays = {};

        FARELAG.forEach(function (fare) {
            // Fargeprikken i lagvelgeren er den samme som i tegnforklaringen
            const label = '<span class="lg lg-fare ' + fare.css + '"></span> ' + fare.navn;

            overlays[label] = new NveLag(NVE_URL, {
                tjeneste: fare.tjeneste,
                lag: fare.lag,
                minZoom: fare.minZoom,
                maxZoom: 18,
                opacity: 0.6,
                attribution: 'Faredata &copy; NVE'
            });
        });

        L.control.layers(null, overlays, { position: 'bottomright', collapsed: true }).addTo(state.map);
    }
    
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
        markers: new Map(),   // punktnøkkel → markør, så lista kan finne riktig markør
        firstLoad: true,
        selectedMarker: null,
        // Filtrene som er valgt nå. Alt vises når siden åpnes.
        filters: {
            showNeeds: true,
            showResources: true,
            priorities: new Set(['Acute', 'High', 'Normal', 'Planned']),
            category: '',
            municipality: ''
        }
    };

    function initMap() {
        state.map = L.map('crisis-map', { zoomControl: false }).setView([58.65, 8.0], 8);

        // Zoom-knappene nede til høyre, der tommelen når på mobil
        L.control.zoom({ position: 'bottomright' }).addTo(state.map);

        L.tileLayer(KARTLAG.graatone, {
            attribution: '&copy; Kartverket',
            maxZoom: 18
        }).addTo(state.map);

        addHazardLayers();
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
            fillFilterOptions();
            applyFilters();

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
        state.markers.clear();
        state.selectedMarker = null;
        const bounds = [];

        points.forEach(function (point) {
            const marker = createMarker(point).addTo(state.pointLayer);
            state.markers.set(pointKey(point), marker);
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
        document.getElementById('panel-close').focus({ preventScroll: true });

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

        // ---------- B29: filter og «Akutt nå»-liste ----------

    // Rekkefølgen behovene sorteres i: det mest akutte først
    const PRIORITET_REKKEFOLGE = ['Acute', 'High', 'Normal', 'Planned'];

    // Unik nøkkel for et punkt, siden et behov og en ressurs kan ha samme id
    function pointKey(point) {
        return point.kind + '-' + point.id;
    }

    // Sjekker om ett punkt passer med filtrene som er valgt nå
    function matchesFilters(point) {
        const f = state.filters;

        if (point.kind === 'need' && !f.showNeeds) {
            return false;
        }
        if (point.kind === 'resource' && !f.showResources) {
            return false;
        }
        // Prioritet gjelder bare behov
        if (point.kind === 'need' && !f.priorities.has(point.priority)) {
            return false;
        }
        if (f.category && point.category !== f.category) {
            return false;
        }
        if (f.municipality && point.municipality !== f.municipality) {
            return false;
        }
        return true;
    }

    // Filtrerer punktene og tegner kart, liste og teller på nytt.
    // Alt skjer i nettleseren, så det går raskt og krever ingen ny henting.
    function applyFilters() {
        const visible = state.points.filter(matchesFilters);

        renderPoints(visible);
        renderNeedList(visible);
        updateFilterSummary(visible.length);
    }

    // Fyller kommune- og kategorilistene med verdiene som finnes i dataene,
    // så man aldri kan velge en kommune uten punkter
    function fillFilterOptions() {
        state.filters.municipality = fillSelect('filter-municipality',
            state.points.map(p => p.municipality), 'Alle kommuner', state.filters.municipality);
        state.filters.category = fillSelect('filter-category',
            state.points.map(p => p.category), 'Alle kategorier', state.filters.category);
    }

    // Lager valgene i en nedtrekksliste, sortert alfabetisk med æ, ø og å riktig
    function fillSelect(id, values, allText, selected) {
        const select = document.getElementById(id);
        const unique = [...new Set(values.filter(Boolean))].sort((a, b) => a.localeCompare(b, 'nb'));

        select.innerHTML = '';
        select.appendChild(new Option(allText, ''));
        unique.forEach(function (value) {
            select.appendChild(new Option(value, value));
        });

        // Beholder valget etter «Oppdater», hvis verdien fortsatt finnes
        select.value = unique.includes(selected) ? selected : '';
        return select.value;
    }

    // Viser hvor mange punkter som vises, og markerer filterknappen
    function updateFilterSummary(visibleCount) {
        document.getElementById('filter-count').textContent =
            'Viser ' + visibleCount + ' av ' + state.points.length + ' punkter';

        document.getElementById('map-filter')
            .classList.toggle('is-active', visibleCount < state.points.length);
    }

    // Lager lista over synlige behov, sortert etter hvor mye de haster
    function renderNeedList(visible) {
        const needs = visible
            .filter(p => p.kind === 'need')
            .sort(function (a, b) {
                return PRIORITET_REKKEFOLGE.indexOf(a.priority) - PRIORITET_REKKEFOLGE.indexOf(b.priority)
                    || a.title.localeCompare(b.title, 'nb');
            });

        const list = document.getElementById('need-list');
        list.innerHTML = '';
        document.getElementById('need-list-count').textContent = needs.length;

        if (needs.length === 0) {
            const empty = document.createElement('li');
            empty.className = 'need-list-empty';
            empty.textContent = 'Ingen behov med disse filtrene.';
            list.appendChild(empty);
            return;
        }

        needs.forEach(function (point) {
            // Ikonet lages av vår egen kode. All tekst fra databasen settes med textContent.
            const icon = document.createElement('span');
            icon.className = 'need-row-icon';
            icon.innerHTML = markerHtml(point);

            const title = document.createElement('span');
            title.className = 'need-row-title';
            title.textContent = point.title;

            const meta = document.createElement('span');
            meta.className = 'need-row-meta';
            meta.textContent = styleFor(point).tekst + ' · ' + point.municipality +
                ' · ' + point.quantity + ' ' + point.unit;

            const text = document.createElement('span');
            text.className = 'need-row-text';
            text.append(title, meta);

            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'need-row';
            button.append(icon, text);
            button.addEventListener('click', function () {
                showPointFromList(point);
            });

            const item = document.createElement('li');
            item.appendChild(button);
            list.appendChild(item);
        });
    }

    // Trykk i lista: zoomer til punktet og åpner detaljene
    function showPointFromList(point) {
        const marker = state.markers.get(pointKey(point));
        if (!marker) {
            return;
        }

        // På mobil lukkes lista, så kartet og detaljene synes
        if (window.innerWidth < 768) {
            closeFilterPanel();
        }

        state.map.setZoom(Math.max(state.map.getZoom(), 13), { animate: false });
        openPanel(point, marker);
    }

    function openFilterPanel() {
        // Detaljpanelet lukkes, så panelene ikke ligger oppå hverandre på mobil
        closePanel();

        const panel = document.getElementById('filter-panel');
        panel.classList.add('is-open');
        panel.setAttribute('aria-hidden', 'false');
        document.getElementById('map-filter').setAttribute('aria-expanded', 'true');
        // preventScroll: ellers ruller nettleseren kartet mens panelet glir inn
        document.getElementById('filter-close').focus({ preventScroll: true });
    }

    function closeFilterPanel() {
        const panel = document.getElementById('filter-panel');
        panel.classList.remove('is-open');
        panel.setAttribute('aria-hidden', 'true');
        document.getElementById('map-filter').setAttribute('aria-expanded', 'false');
    }

    // Leser av/på-knappene og lagrer valgene i state.filters
    function readChips() {
        const isOn = chip => chip.getAttribute('aria-pressed') === 'true';

        state.filters.showNeeds = isOn(document.querySelector('#filter-panel [data-kind="need"]'));
        state.filters.showResources = isOn(document.querySelector('#filter-panel [data-kind="resource"]'));
        state.filters.priorities = new Set(
            [...document.querySelectorAll('#filter-panel [data-priority]')]
                .filter(isOn)
                .map(chip => chip.dataset.priority));
    }

    // Slår av eller på alle knappene som passer med selektoren
    function setChips(selector, on) {
        document.querySelectorAll('#filter-panel ' + selector).forEach(function (chip) {
            chip.setAttribute('aria-pressed', String(on));
        });
    }

    function resetFilters() {
        setChips('.chip', true);
        document.getElementById('filter-municipality').value = '';
        document.getElementById('filter-category').value = '';
        state.filters.municipality = '';
        state.filters.category = '';
        readChips();
        applyFilters();
    }

    // «Akutt nå»: viser bare akutte behov og åpner lista.
    // Velg kommune etterpå, så har man alle akutte behov der med to trykk.
    function showAcuteNow() {
        setChips('[data-priority]', false);
        setChips('[data-priority="Acute"]', true);
        setChips('[data-kind="need"]', true);
        readChips();
        applyFilters();
        openFilterPanel();
    }

    // Kobler knappene og listene i filterpanelet til funksjonene over
    function initFilters() {
        document.getElementById('map-filter').addEventListener('click', function () {
            if (document.getElementById('filter-panel').classList.contains('is-open')) {
                closeFilterPanel();
            } else {
                openFilterPanel();
            }
        });
        document.getElementById('filter-close').addEventListener('click', closeFilterPanel);
        document.getElementById('map-acute').addEventListener('click', showAcuteNow);
        document.getElementById('filter-reset').addEventListener('click', resetFilters);

        document.querySelectorAll('#filter-panel .chip').forEach(function (chip) {
            chip.addEventListener('click', function () {
                const on = chip.getAttribute('aria-pressed') !== 'true';
                chip.setAttribute('aria-pressed', String(on));
                readChips();
                applyFilters();
            });
        });

        document.getElementById('filter-municipality').addEventListener('change', function (event) {
            state.filters.municipality = event.target.value;
            applyFilters();
        });
        document.getElementById('filter-category').addEventListener('change', function (event) {
            state.filters.category = event.target.value;
            applyFilters();
        });

        // Trykk på kartet over panelet lukker det på mobil
        state.map.on('click', function () {
            if (window.innerWidth < 768) {
                closeFilterPanel();
            }
        });
    }
    
    document.addEventListener('DOMContentLoaded', function () {
        // Tegnforklaringen er foldet sammen på små skjermer, så kartet får plass
        if (window.innerWidth < 768) {
            document.getElementById('map-legend').removeAttribute('open');
            document.getElementById('filter-more').removeAttribute('open');
        }

        initMap();
        loadData();

        document.getElementById('map-refresh').addEventListener('click', loadData);
        document.getElementById('panel-close').addEventListener('click', closePanel);
        initFilters();

        // Esc lukker panelet
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closePanel();
                closeFilterPanel();;
            }
        });
    });
})();