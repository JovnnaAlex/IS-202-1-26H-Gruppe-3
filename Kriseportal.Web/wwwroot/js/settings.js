// Innstillinger: kun frontend-demo. Ikke bruker- eller databaselagring.
document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("settings-form");
    const page = document.getElementById("settings-page");
    if (!form || !page) return;

    const key = "kriseportalen.demo.settings.v1";
    const largeText = document.getElementById("settings-large-text");
    const reduceMotion = document.getElementById("settings-reduce-motion");
    const mapView = document.getElementById("settings-map-view");
    const reset = document.getElementById("settings-reset");
    const feedback = document.getElementById("settings-feedback");

    const defaults = { largeText: false, reduceMotion: false, mapView: "both" };
    const validMapViews = ["both", "needs", "resources"];

    function showMessage(message, isError = false) {
        feedback.textContent = message;
        feedback.classList.toggle("is-error", isError);
        feedback.hidden = false;
    }

    function applyPreview() {
        page.classList.toggle("settings-large-text", largeText.checked);
        page.classList.toggle("settings-reduced-motion", reduceMotion.checked);
    }

    function fillForm(value) {
        largeText.checked = value.largeText === true;
        reduceMotion.checked = value.reduceMotion === true;
        mapView.value = validMapViews.includes(value.mapView) ? value.mapView : "both";
        applyPreview();
    }

    function readStoredSettings() {
        try {
            const raw = localStorage.getItem(key);
            return raw ? JSON.parse(raw) : defaults;
        } catch {
            return defaults;
        }
    }

    fillForm(readStoredSettings());
    largeText.addEventListener("change", applyPreview);
    reduceMotion.addEventListener("change", applyPreview);

    form.addEventListener("submit", (event) => {
        event.preventDefault();
        const settings = {
            largeText: largeText.checked,
            reduceMotion: reduceMotion.checked,
            mapView: mapView.value
        };
        try {
            localStorage.setItem(key, JSON.stringify(settings));
            showMessage("Innstillingene er lagret i denne nettleseren (demo).");
        } catch {
            showMessage("Nettleseren tillot ikke lagring av innstillingene.", true);
        }
    });

    reset.addEventListener("click", () => {
        try {
            localStorage.removeItem(key);
            fillForm(defaults);
            showMessage("Innstillingene er tilbakestilt til standardverdiene.");
        } catch {
            showMessage("Kunne ikke tilbakestille lagrede innstillinger.", true);
        }
    });
});
