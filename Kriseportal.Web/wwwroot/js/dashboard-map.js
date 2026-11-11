// Dashbordkartet: et enkelt, levende situasjonsbilde for offentlige aktører.
// Bruker de samme dataene og fargene som situasjonskartet (/Map), men uten verktøyene.
// Oppdaterer seg selv, slik at nye behov dukker opp mens man ser på.
(function () {
    'use strict';

    const DATA_URL = '/Map/Data';
    const SITUASJONSKART_URL = '/Map';
    const OPPDATER_HVERT_MS = 15000;   // henter nye data hvert 15. sekund
    const GRAATONE = 'https://cache.kartverket.no/v1/wmts/1.0.0/topograatone/default/webmercator/{z}/{y}/{x}.png';

    // Samme fargespråk som situasjonskartet: rød = akutt, oransje = haster, gul = planlagt
    const PRIORITET = {
        Acute: { css: 'dm-acute', tekst: 'akutt', size: 20 },
        High: { css: 'dm-high', tekst: 'høy', size: 16 },
        Normal: { css: 'dm-normal', tekst: 'normal', size: 13 },
        Planned: { css: 'dm-planned', tekst: 'planlagt', size: 12 }
    };

    const state = {
        map: null,
        layer: null,
        seen: null,        // punktene vi har sett før, så vi vet hva som er nytt
        firstLoad: true
    };

    // Unik nøkkel, siden et behov og en ressurs kan ha samme id
    function pointKey(point) {
        return point.kind + '-' + point.id;
    }

    // Lager en enkel prikk: rund for behov, firkantet for ressurser
    function createMarker(point, isNew) {
        let css;
        let size;

        if (point.kind === 'need') {
            const p = PRIORITET[point.priority] || PRIORITET.Normal;
            css = 'dm-dot dm-need ' + p.css;
            size = p.size;
        } else {
            const tilgjengelig = { Available: 'dm-available', Reserved: 'dm-reserved' };
            css = 'dm-dot dm-res ' + (tilgjengelig[point.status] || 'dm-unavailable');
            size = 12;
        }

        // Nye punkter får en blå ring som pulserer noen ganger
        if (isNew) {
            css += ' dm-new';
        }

        return L.marker([point.lat, point.lng], {
            icon: L.divIcon({
                className: '',
                html: '<span class="' + css + '"></span>',
                iconSize: [size, size],
                iconAnchor: [size / 2, size / 2]
            }),
            interactive: false,   // trykk på kartet åpner situasjonskartet
            keyboard: false,
            zIndexOffset: point.priority === 'Acute' ? 1000 : 0
        });
    }

    // Tegner alle punktene og returnerer de som er nye siden forrige gang
    function renderPoints(points) {
        const newPoints = [];
        const bounds = [];

        state.layer.clearLayers();

        points.forEach(function (point) {
            const isNew = state.seen !== null && !state.seen.has(pointKey(point));
            if (isNew) {
                newPoints.push(point);
            }
            createMarker(point, isNew).addTo(state.layer);
            bounds.push([point.lat, point.lng]);
        });

        state.seen = new Set(points.map(pointKey));

        // Første gang zoomer kartet slik at alle punktene synes
        if (state.firstLoad && bounds.length > 0) {
            state.map.fitBounds(bounds, { padding: [30, 30], maxZoom: 11 });
            state.firstLoad = false;
        }

        return newPoints;
    }

    // Teller opp fra forrige tall til det nye, så endringer blir synlige
    function animateNumber(id, target) {
        const el = document.getElementById(id);
        if (!el) {
            return;
        }

        const start = parseInt(el.textContent, 10) || 0;
        const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        if (start === target || reduceMotion) {
            el.textContent = target;
            return;
        }

        const startTime = performance.now();
        const duration = 900;

        function step(now) {
            const t = Math.min((now - startTime) / duration, 1);
            const eased = 1 - Math.pow(1 - t, 3);   // starter raskt, bremser mot slutten
            el.textContent = Math.round(start + (target - start) * eased);
            if (t < 1) {
                requestAnimationFrame(step);
            }
        }

        requestAnimationFrame(step);
    }

    // Regner ut tallene på kortene til høyre
    function updateNumbers(points) {
        const needs = points.filter(p => p.kind === 'need');

        animateNumber('dash-needs', needs.length);
        animateNumber('dash-acute', needs.filter(p => p.priority === 'Acute').length);
        animateNumber('dash-resources',
            points.filter(p => p.kind === 'resource' && p.status === 'Available').length);
    }

    // Viser en liten melding oppe til høyre på kartet når noe nytt kommer inn
    function showToast(point) {
        const acute = point.kind === 'need' && point.priority === 'Acute';

        const label = document.createElement('strong');
        if (point.kind === 'need') {
            label.textContent = acute ? 'Nytt akutt behov: ' : 'Nytt behov: ';
        } else {
            label.textContent = 'Ny ressurs: ';
        }

        const toast = document.createElement('div');
        toast.className = 'dm-toast' + (acute ? ' dm-toast-acute' : '');
        // Tekst fra databasen settes som tekst, aldri som HTML
        toast.append(label, document.createTextNode(point.title + ' (' + point.municipality + ')'));

        document.getElementById('dm-toasts').appendChild(toast);

        setTimeout(function () { toast.classList.add('dm-toast-hide'); }, 6000);
        setTimeout(function () { toast.remove(); }, 6600);
    }

    // Viser «Live · oppdatert kl. …», eller at oppdateringen feilet
    function setLiveStatus(ok) {
        const badge = document.getElementById('dm-live');
        const text = document.getElementById('dm-live-text');

        badge.classList.toggle('is-error', !ok);
        text.textContent = ok
            ? 'Live · oppdatert kl. ' + new Date().toLocaleTimeString('nb-NO', { hour: '2-digit', minute: '2-digit' })
            : 'Ikke oppdatert – prøver igjen';
    }

    async function loadData() {
        try {
            const response = await fetch(DATA_URL, { headers: { Accept: 'application/json' }, cache: 'no-store' });
            if (!response.ok) {
                throw new Error('HTTP ' + response.status);
            }
            const data = await response.json();   // feiler hvis innloggingen har gått ut

            const newPoints = renderPoints(data.points);
            updateNumbers(data.points);
            newPoints.slice(0, 3).forEach(showToast);   // maks tre meldinger om gangen
            setLiveStatus(true);
        } catch (error) {
            console.error('Kunne ikke hente kartdata', error);
            setLiveStatus(false);
        }
    }

    // Lager live-merket, meldingsfeltet og «åpne»-hintet oppå kartet
    function addOverlays(section) {
        section.insertAdjacentHTML('beforeend',
            '<div class="dm-live" id="dm-live"><span class="dm-live-dot"></span>' +
            '<span id="dm-live-text">Henter data …</span></div>' +
            '<div class="dm-toasts" id="dm-toasts" aria-live="polite"></div>' +
            '<div class="dm-open">Åpne situasjonskartet &rarr;</div>');
    }

    function init() {
        const mapElement = document.getElementById('dashboardMap');
        if (!mapElement || typeof L === 'undefined') {
            return;
        }

        // Kartet er et overblikk: ingen zoom eller dra, hele kartet er en lenke
        state.map = L.map('dashboardMap', {
            zoomControl: false,
            dragging: false,
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            touchZoom: false
        }).setView([58.65, 8.0], 8);

        L.tileLayer(GRAATONE, { attribution: '&copy; Kartverket', maxZoom: 18 }).addTo(state.map);
        state.layer = L.layerGroup().addTo(state.map);

        addOverlays(mapElement.parentElement);

        // Trykk eller Enter på kartet åpner situasjonskartet
        mapElement.setAttribute('role', 'link');
        mapElement.setAttribute('tabindex', '0');
        mapElement.setAttribute('aria-label', 'Kart over behov og ressurser. Åpne situasjonskartet');
        mapElement.addEventListener('click', function () {
            window.location.href = SITUASJONSKART_URL;
        });
        mapElement.addEventListener('keydown', function (event) {
            if (event.key === 'Enter') {
                window.location.href = SITUASJONSKART_URL;
            }
        });

        loadData();

        // Henter nye data jevnlig, men ikke når fanen er skjult
        setInterval(function () {
            if (!document.hidden) {
                loadData();
            }
        }, OPPDATER_HVERT_MS);

        // Når man kommer tilbake til fanen, oppdateres kartet med en gang
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) {
                loadData();
            }
        });

        // Kartet må vite sin endelige størrelse etter at siden er lastet
        window.addEventListener('load', function () {
            state.map.invalidateSize();
        });
    }

    document.addEventListener('DOMContentLoaded', init);
})();