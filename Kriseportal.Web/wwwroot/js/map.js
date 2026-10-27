// Henter kartelementet og avslutter dersom siden ikke har et kart.
const mapElement = document.getElementById('map');

if (mapElement) {
    // Bestemmer om kartet kan redigeres eller bare vises.
    const mapMode = mapElement.dataset.mapMode || 'edit';

    // Oppretter kartet med Kristiansand som standardvisning.
    const map = L.map('map').setView([58.1467, 7.9956], 11);

    // Legger til Kartverkets topografiske kartgrunnlag.
    L.tileLayer(
        'https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png',
        {
            attribution: '&copy; Kartverket',
            maxZoom: 18
        }
    ).addTo(map);

    // Henter skjemafeltene og adressefeltet dersom de finnes.
    const latitudeInput = document.getElementById('Latitude');
    const longitudeInput = document.getElementById('Longitude');
    const addressInput = document.getElementById('addressSearch');
    const suggestions = document.getElementById('addressSuggestions');

    // Lagrer valgt markør.
    let marker;

    // Oppdaterer kartet og koordinatfeltene med valgt plassering.
    function selectLocation(latitude, longitude, zoom = 15) {
        if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
            return;
        }

        if (latitudeInput && longitudeInput) {
            latitudeInput.value = latitude.toFixed(6);
            longitudeInput.value = longitude.toFixed(6);

            // Varsler eventuelle valideringsfunksjoner om endringen.
            latitudeInput.dispatchEvent(new Event('change', { bubbles: true }));
            longitudeInput.dispatchEvent(new Event('change', { bubbles: true }));
        }

        if (marker) {
            marker.setLatLng([latitude, longitude]);
        } else {
            marker = L.marker([latitude, longitude]).addTo(map);
        }

        map.setView([latitude, longitude], zoom);
    }

    // Viser tidligere registrert plassering dersom den finnes.
    if (latitudeInput?.value && longitudeInput?.value) {
        const latitude = Number(latitudeInput.value.replace(',', '.'));
        const longitude = Number(longitudeInput.value.replace(',', '.'));

        if (Number.isFinite(latitude) && Number.isFinite(longitude)) {
            selectLocation(latitude, longitude, 13);
        }
    }

    // Tillater kartklikk bare når kartet er i redigeringsmodus.
    if (mapMode === 'edit') {
        map.on('click', function (event) {
            selectLocation(event.latlng.lat, event.latlng.lng);

            // Fjerner gammel adressetekst når brukeren velger et nytt kartpunkt.
            if (addressInput) {
                addressInput.value = '';
            }

            if (suggestions) {
                suggestions.replaceChildren();
            }
        });
    }

    // Aktiverer adressesøk dersom siden har adressefelt.
    if (mapMode === 'edit' && addressInput && suggestions) {
        let searchTimeout;
        let searchController;

        // Viser en enkel statusmelding under adressefeltet.
        function showMessage(message) {
            suggestions.replaceChildren();

            const text = document.createElement('p');
            text.textContent = message;
            text.style.color = '#66756B';
            text.style.fontSize = '0.9rem';
            text.style.marginTop = '8px';

            suggestions.appendChild(text);
        }

        // Søker etter norske adresser gjennom Kartverkets adresse-API.
        async function searchAddresses(query) {
            if (searchController) {
                searchController.abort();
            }

            searchController = new AbortController();

            const url = new URL('https://api.kartverket.no/adresser/v1/sok');

            url.searchParams.set('sok', query + '*');
            url.searchParams.set('treffPerSide', '8');
            url.searchParams.set('side', '0');
            url.searchParams.set('utkoordsys', '4258');

            showMessage('Søker etter adresser ...');

            try {
                const response = await fetch(url.toString(), {
                    signal: searchController.signal
                });

                if (!response.ok) {
                    throw new Error('Adressesøket kunne ikke gjennomføres.');
                }

                const data = await response.json();
                const addresses = data.adresser || [];

                suggestions.replaceChildren();

                if (addresses.length === 0) {
                    showMessage('Fant ingen adresser. Prøv et annet søk, eller klikk i kartet.');
                    return;
                }

                // Lager klikkbare forslag for adressene som ble funnet.
                addresses.forEach(function (address) {
                    const point = address.representasjonspunkt;

                    if (!point) {
                        return;
                    }

                    const latitude = Number(point.lat);
                    const longitude = Number(point.lon);

                    if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
                        return;
                    }

                    const button = document.createElement('button');
                    button.type = 'button';

                    const addressName = address.adressetekst || address.adressenavn || 'Adresse';
                    const municipality = address.kommunenavn || '';

                    button.textContent = municipality
                        ? `${addressName}, ${municipality}`
                        : addressName;

                    button.style.display = 'block';
                    button.style.width = '100%';
                    button.style.padding = '12px';
                    button.style.textAlign = 'left';
                    button.style.backgroundColor = '#FFFFFF';
                    button.style.color = '#24332B';
                    button.style.border = '1px solid #CBD5CC';
                    button.style.borderRadius = '6px';
                    button.style.marginBottom = '4px';
                    button.style.cursor = 'pointer';

                    // Flytter kartet når brukeren velger en adresse.
                    button.addEventListener('click', function () {
                        addressInput.value = button.textContent;
                        selectLocation(latitude, longitude, 16);
                        suggestions.replaceChildren();
                    });

                    suggestions.appendChild(button);
                });

                if (!suggestions.children.length) {
                    showMessage('Ingen adresser med gyldige koordinater ble funnet.');
                }
            } catch (error) {
                // Avbrutte søk skal ikke vise feilmeldinger.
                if (error.name === 'AbortError') {
                    return;
                }

                console.error('Feil ved adressesøk:', error);
                showMessage('Kunne ikke hente adresser. Du kan fortsatt velge plassering i kartet.');
            }
        }

        // Venter litt etter at brukeren skriver før søket sendes.
        addressInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);

            if (searchController) {
                searchController.abort();
            }

            const query = addressInput.value.trim();

            suggestions.replaceChildren();

            // Fjerner gammel kartplassering når adressen endres.
            if (marker) {
                map.removeLayer(marker);
                marker = null;
            }

            if (latitudeInput && longitudeInput) {
                latitudeInput.value = '';
                longitudeInput.value = '';
            }

            if (query.length < 3) {
                return;
            }

            searchTimeout = setTimeout(function () {
                searchAddresses(query);
            }, 350);
        });
    }
}