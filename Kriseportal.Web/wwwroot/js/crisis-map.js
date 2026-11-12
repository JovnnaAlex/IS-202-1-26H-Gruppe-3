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

        // B31: beredskap per kommune ligger i samme lagvelger som farelagene
        state.capacityLayer = L.layerGroup();
        overlays['<span class="lg lg-cap"></span> Beredskap per kommune'] = state.capacityLayer;

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
        lastFetchedAt: null,
        markers: new Map(),   // punktnøkkel → markør, så lista kan finne riktig markør
        firstLoad: true,
        selectedMarker: null,
        // B20: ressurser i nærheten av valgt behov
        nearbyLayer: null,    // sirkel og linjer på kartet
        selectedNeed: null,   // behovet som er åpent i panelet
        nearbyKm: 10,         // valgt avstand i km
        // B31: lag med beredskap per kommune, slås av og på i lagvelgeren
        capacityLayer: null,
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
        state.nearbyLayer = L.layerGroup().addTo(state.map);

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
            state.lastFetchedAt = new Date();
            fillFilterOptions();
            renderCapacity();
            applyFilters();

            const time = state.lastFetchedAt.toLocaleTimeString('nb-NO', { hour: '2-digit', minute: '2-digit' });
            updated.textContent = 'Oppdatert kl. ' + time;
        } catch (error) {
            console.error('Kunne ikke hente kartdata:', error);
            
            if (state.lastFetchedAt) {
                const time = state.lastFetchedAt.toLocaleTimeString('nb-NO', {
                    hour: '2-digit',
                    minute: '2-digit'
                });
                updated.textContent = 'Viser siste kartdata · Hentet kl. ' + time;
            } else {
                updated.textContent = 'Klarte ikke å hente data';
            }
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

        // B20: behov viser ressurser i nærheten, ressurser gjør det ikke
        if (isNeed) {
            showNearby(point);
        } else {
            clearNearby();
        }

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
        clearNearby();
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

    // ---------- B31: beredskap per kommune ----------

    // Kritiske beredskapsfunksjoner, og hvilke kategorier som teller for hver.
    // Brukes forebyggende: hvor godt rustet er kommunen FØR noe skjer?
    const BEREDSKAP = [
        { navn: 'Strøm', kategorier: ['Strøm'] },
        { navn: 'Vann', kategorier: ['Vann'] },
        { navn: 'Mat', kategorier: ['Mat'] },
        { navn: 'Varme', kategorier: ['Varme'] },
        { navn: 'Transport og evakuering', kategorier: ['Transport', 'Evakuering', 'Båt'] },
        { navn: 'Vei og maskiner', kategorier: ['Maskiner', 'Snørydding', 'Sand og grus'] },
        { navn: 'Samband', kategorier: ['Samband'] },
        { navn: 'Mannskap', kategorier: ['Mannskap'] },
        { navn: 'Lokaler', kategorier: ['Lokaler'] },
        { navn: 'Overvåking', kategorier: ['Drone'] }
    ];

    // Målet for god beredskap: minst én ressurs per 10 000 innbyggere for hver funksjon
    // (og alltid minst én). Forenklet antakelse som kan justeres her.
    const INNBYGGERE_PER_RESSURS = 10000;

    // Kommunene i Agder: navn, omtrentlig kommunesenter og innbyggertall (SSB).
    // Kommuner UTEN registrerte ressurser vises også, og det er dem som trenger mest innsats.
    const AGDER_KOMMUNER = [
        ['Arendal', 58.461, 8.772, 46355], ['Birkenes', 58.335, 8.233, 5480],
        ['Bygland', 58.833, 7.800, 1180], ['Bykle', 59.354, 7.355, 1011],
        ['Evje og Hornnes', 58.586, 7.794, 3967], ['Farsund', 58.095, 6.805, 9876],
        ['Flekkefjord', 58.297, 6.663, 9279], ['Froland', 58.523, 8.640, 6236],
        ['Gjerstad', 58.873, 9.017, 2444], ['Grimstad', 58.341, 8.593, 24969],
        ['Hægebostad', 58.588, 7.198, 1776], ['Iveland', 58.470, 7.940, 1380],
        ['Kristiansand', 58.146, 7.996, 116986], ['Kvinesdal', 58.318, 6.958, 6192],
        ['Lillesand', 58.250, 8.378, 11523], ['Lindesnes', 58.083, 7.300, 23690],
        ['Lyngdal', 58.137, 7.071, 10835], ['Risør', 58.721, 9.234, 6825],
        ['Sirdal', 58.664, 6.718, 1873], ['Tvedestrand', 58.622, 8.932, 6323],
        ['Valle', 59.210, 7.535, 1205], ['Vegårshei', 58.763, 8.800, 2268],
        ['Vennesla', 58.270, 7.973, 15452], ['Åmli', 58.776, 8.497, 1802],
        ['Åseral', 58.617, 7.410, 923]
    ];

    // Regner ut beredskapen for hver kommune, tilpasset hvor mange som bor der
    function beregnBeredskap(points) {
        const kommuner = new Map();

        AGDER_KOMMUNER.forEach(function ([navn, lat, lng, innbyggere]) {
            kommuner.set(navn, { navn: navn, lat: lat, lng: lng, innbyggere: innbyggere, ressurser: [], akutteBehov: 0 });
        });

        points.forEach(function (p) {
            if (!kommuner.has(p.municipality)) {
                // Kommune utenfor Agder: ukjent innbyggertall, plasseres der det første punktet er
                kommuner.set(p.municipality, { navn: p.municipality, lat: p.lat, lng: p.lng, innbyggere: null, ressurser: [], akutteBehov: 0 });
            }
            const k = kommuner.get(p.municipality);

            if (p.kind === 'resource' && p.status !== 'Unavailable') {
                k.ressurser.push(p);   // både ledige og reserverte finnes i kommunen
            } else if (p.kind === 'need' && p.priority === 'Acute') {
                k.akutteBehov += 1;
            }
        });

        return [...kommuner.values()].map(function (k) {
            // Hvor mange ressurser kommunen bør ha per funksjon, f.eks. 12 for Kristiansand
            const mal = Math.max(1, Math.ceil((k.innbyggere || 0) / INNBYGGERE_PER_RESSURS));

            const funksjoner = BEREDSKAP.map(function (f) {
                const antall = k.ressurser.filter(r => f.kategorier.includes(r.category)).length;
                return { navn: f.navn, antall: antall, mal: mal, dekning: Math.min(1, antall / mal) };
            });

            // Kommunens beredskap = gjennomsnittlig dekning av de 10 funksjonene, i prosent
            const prosent = Math.round(100 * funksjoner.reduce((sum, f) => sum + f.dekning, 0) / funksjoner.length);

            return Object.assign(k, { funksjoner: funksjoner, mal: mal, prosent: prosent, nivaa: beredskapsnivaa(prosent) });
        });
    }

    // Grønn: 70 % eller mer, gul: 40–69 %, rød: under 40 %
    function beredskapsnivaa(prosent) {
        if (prosent >= 70) {
            return 'cap-ok';
        }
        if (prosent >= 40) {
            return 'cap-medium';
        }
        return 'cap-missing';
    }

    // Tegner ett merke per kommune, f.eks. «Vennesla · 30 %»
    function renderCapacity() {
        if (!state.capacityLayer) {
            return;
        }
        state.capacityLayer.clearLayers();

        beregnBeredskap(state.points).forEach(function (k) {
            const badge = document.createElement('span');
            badge.className = 'cap-badge ' + k.nivaa;
            const navn = document.createElement('strong');
            navn.textContent = k.navn;
            badge.append(navn, document.createTextNode(' · ' + k.prosent + ' %'));

            const marker = L.marker([k.lat, k.lng], {
                icon: L.divIcon({ className: '', html: badge.outerHTML, iconSize: null, iconAnchor: [0, 0] }),
                zIndexOffset: 2000,   // over punktene, så merket kan trykkes på
                keyboard: true,
                title: k.navn + ': beredskap ' + k.prosent + ' prosent'
            });

            marker.bindPopup(lagBeredskapPopup(k));
            marker.addTo(state.capacityLayer);
        });
    }

    // Innholdet når man trykker på et kommunemerke: én linje per funksjon og hva som trenger innsats
    function lagBeredskapPopup(k) {
        const box = document.createElement('div');
        box.className = 'cap-popup';

        const title = document.createElement('h3');
        title.textContent = k.navn;

        const summary = document.createElement('p');
        summary.className = 'cap-summary';
        summary.textContent = 'Beredskap: ' + k.prosent + ' %' +
            (k.innbyggere ? ' · ' + k.innbyggere.toLocaleString('nb-NO') + ' innbyggere' : '');

        // ✓ = målet er nådd, ◐ = delvis, ✗ = ingen ressurser
        const list = document.createElement('ul');
        k.funksjoner.forEach(function (f) {
            const item = document.createElement('li');
            let tegn = '✗ ';
            item.className = 'cap-row-missing';
            if (f.dekning >= 1) {
                tegn = '✓ ';
                item.className = 'cap-row-ok';
            } else if (f.antall > 0) {
                tegn = '◐ ';
                item.className = 'cap-row-partial';
            }
            item.textContent = tegn + f.navn + ': ' + f.antall + ' av ' + f.mal;
            list.appendChild(item);
        });

        box.append(title, summary, list);

        // De tre funksjonene med lavest dekning er det HV bør se på først
        const svakest = k.funksjoner
            .filter(f => f.dekning < 1)
            .sort((a, b) => a.dekning - b.dekning)
            .map(f => f.navn);
        if (svakest.length > 0) {
            const weak = document.createElement('p');
            weak.className = 'cap-weak';
            weak.textContent = 'Trenger innsats: ' + svakest.slice(0, 3).join(', ') + (svakest.length > 3 ? ' …' : '');
            box.appendChild(weak);
        }

        // Situasjonen nå, i tillegg til den forebyggende oversikten
        if (k.akutteBehov > 0) {
            const now = document.createElement('p');
            now.className = 'cap-now';
            now.textContent = 'Akutte behov nå: ' + k.akutteBehov;
            box.appendChild(now);
        }

        const note = document.createElement('p');
        note.className = 'cap-note';
        note.textContent = 'Mål: minst én ressurs per 10 000 innbyggere for hver funksjon. ' +
            'Teller registrerte ressurser i kommunen (ledige og reserverte).';
        box.appendChild(note);

        return box;
    }

    // ---------- B20: ressurser i nærheten av et behov ----------

    // Avstand i km mellom to punkter på jordkloden (haversine-formelen).
    // Tar hensyn til at jorda er rund, så avstanden blir riktig også over lengre strekninger.
    function avstandKm(a, b) {
        const R = 6371;   // jordas radius i km
        const rad = Math.PI / 180;
        const dLat = (b.lat - a.lat) * rad;
        const dLng = (b.lng - a.lng) * rad;
        const h = Math.sin(dLat / 2) ** 2 +
            Math.cos(a.lat * rad) * Math.cos(b.lat * rad) * Math.sin(dLng / 2) ** 2;
        return 2 * R * Math.asin(Math.sqrt(h));
    }

    // Finner alle ressurser innen valgt avstand.
    // Samme kategori som behovet kommer først, deretter den nærmeste.
    function findNearby(need, km) {
        return state.points
            .filter(p => p.kind === 'resource')
            .map(p => ({ point: p, km: avstandKm(need, p) }))
            .filter(r => r.km <= km)
            .sort(function (a, b) {
                const aSame = a.point.category === need.category ? 0 : 1;
                const bSame = b.point.category === need.category ? 0 : 1;
                return aSame - bSame || a.km - b.km;
            });
    }

    // Tegner søkeområdet og linjer til ressursene, og fyller lista i panelet
    function showNearby(need) {
        state.selectedNeed = need;
        state.nearbyLayer.clearLayers();

        const km = state.nearbyKm;
        const nearby = findNearby(need, km);

        // Stiplet sirkel som viser hvor langt vi leter
        L.circle([need.lat, need.lng], {
            radius: km * 1000,
            color: '#1F6FD1',
            weight: 2,
            dashArray: '6 6',
            fillOpacity: 0.06,
            interactive: false
        }).addTo(state.nearbyLayer);

        // Stiplet linje fra behovet til hver ressurs i nærheten
        nearby.forEach(function (r) {
            L.polyline([[need.lat, need.lng], [r.point.lat, r.point.lng]], {
                color: '#1F6FD1',
                weight: 2,
                opacity: 0.7,
                dashArray: '4 6',
                interactive: false
            }).addTo(state.nearbyLayer);
        });

        renderNearbyList(need, nearby, km);
        document.getElementById('nearby').hidden = false;
    }

    function clearNearby() {
        state.selectedNeed = null;
        if (state.nearbyLayer) {
            state.nearbyLayer.clearLayers();
        }
        document.getElementById('nearby').hidden = true;
    }

    // Lista i panelet: ikon, navn, avstand, tilgjengelighet og en Ring-knapp
    function renderNearbyList(need, nearby, km) {
        const list = document.getElementById('nearby-list');
        list.innerHTML = '';

        if (nearby.length === 0) {
            const empty = document.createElement('li');
            empty.className = 'nearby-empty';
            empty.textContent = 'Ingen ressurser innen ' + km + ' km. Prøv en større avstand.';
            list.appendChild(empty);
            return;
        }

        nearby.forEach(function (r) {
            const p = r.point;

            const icon = document.createElement('span');
            icon.className = 'need-row-icon';
            icon.innerHTML = markerHtml(p);

            const title = document.createElement('span');
            title.className = 'need-row-title';
            title.textContent = p.title;

            // F.eks. «3,2 km · Tilgjengelig · Samme kategori»
            const meta = document.createElement('span');
            meta.className = 'need-row-meta';
            meta.textContent = r.km.toFixed(1).replace('.', ',') + ' km · ' + styleFor(p).tekst +
                (p.category === need.category ? ' · Samme kategori' : '');

            const text = document.createElement('span');
            text.className = 'need-row-text';
            text.append(title, meta);

            const call = document.createElement('a');
            call.className = 'nearby-call';
            call.href = 'tel:' + (p.contactPhone || '').replace(/\s/g, '');
            call.setAttribute('aria-label', 'Ring ' + p.contactName + ' om ' + p.title);
            call.innerHTML = '<i class="bi bi-telephone-fill" aria-hidden="true"></i>';

            const item = document.createElement('li');
            item.className = 'nearby-item';
            item.append(icon, text, call);
            list.appendChild(item);
        });
    }

    // Kobler avstandsknappene (5, 10, 25 km)
    function initNearby() {
        const chips = document.querySelectorAll('#nearby .range-chip');

        chips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                state.nearbyKm = Number(chip.dataset.km);
                chips.forEach(c => c.setAttribute('aria-pressed', String(c === chip)));

                if (state.selectedNeed) {
                    showNearby(state.selectedNeed);
                    // Zoomer slik at hele sirkelen synes
                    const center = L.latLng(state.selectedNeed.lat, state.selectedNeed.lng);
                    state.map.fitBounds(center.toBounds(state.nearbyKm * 2000), { padding: [20, 20] });
                }
            });
        });
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
        initNearby();

        // Esc lukker panelet
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closePanel();
                closeFilterPanel();;
            }
        });
    });
})();