
/* Sara: Kart på den offentlige forsiden.
   Viser Agder uten private ressursadresser.
   Endrer ikke eksisterende map.js. */

document.addEventListener('DOMContentLoaded', function () {

    const mapElement = document.getElementById('home-map');

    // Sara: Avslutter dersom siden ikke har kartet.
    if (!mapElement || typeof L === 'undefined') {
        return;
    }

    // Sara: Oppretter et kart uten redigeringsfunksjoner.
    const map = L.map('home-map', {
        scrollWheelZoom: false
    });

    // Sara: Kartgrunnlag fra Kartverket.
    L.tileLayer(
        'https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png',
        {
            attribution: '&copy; Kartverket',
            maxZoom: 18
        }
    ).addTo(map);

    // Sara: Viser et større geografisk område rundt Agder.
    map.setView([58.65, 8.0], 7);

});
