
/*
    MIN PROFIL - FRONTEND DEMO

    Bruker sessionStorage for å bevare
    testdata i samme nettleserfane.

    Ingen database eller backend-lagring.
*/

document.addEventListener("DOMContentLoaded", function () {

    const storageKey = "kriseportalen.demo.profile";
    const messageKey = "kriseportalen.demo.profile.message";

    // Henter lagret testinformasjon
    function getProfile() {
        try {
            const savedData = sessionStorage.getItem(storageKey);

            if (!savedData) {
                return { email: "", phone: "" };
            }

            const profile = JSON.parse(savedData);

            return {
                email: typeof profile.email === "string"
                    ? profile.email
                    : "",

                phone: typeof profile.phone === "string"
                    ? profile.phone
                    : ""
            };

        } catch (error) {
            console.error("Kunne ikke lese profildata.", error);

            return { email: "", phone: "" };
        }
    }

    // Lagrer testinformasjon i nettleserfanen
    function saveProfile(email, phone) {
        try {
            const profile = {
                email: email,
                phone: phone
            };

            sessionStorage.setItem(
                storageKey,
                JSON.stringify(profile)
            );

            sessionStorage.setItem(
                messageKey,
                "Kontaktinformasjonen er oppdatert i demoen."
            );

            return true;

        } catch (error) {
            console.error("Kunne ikke lagre profildata.", error);
            return false;
        }
    }

    // =====================================
    // MIN PROFIL - INDEX.CSHTML
    // =====================================

    const emailDisplay = document.getElementById(
        "profile-email-display"
    );

    const phoneDisplay = document.getElementById(
        "profile-phone-display"
    );

    // Kjører bare på profilsiden
    if (emailDisplay && phoneDisplay) {

        const profile = getProfile();

        // Oppdaterer teksten på skjermen
        emailDisplay.textContent =
            profile.email || "Ikke oppgitt";

        phoneDisplay.textContent =
            profile.phone || "Ikke oppgitt";

        // Viser melding etter lagring
        const messageBox = document.getElementById(
            "profile-message"
        );

        if (messageBox) {
            try {
                const message = sessionStorage.getItem(messageKey);

                if (message) {
                    messageBox.textContent = message;
                    messageBox.hidden = false;

                    // Meldingen vises bare én gang.
                    sessionStorage.removeItem(messageKey);
                }

            } catch (error) {
                console.error(
                    "Kunne ikke hente bekreftelsesmeldingen.",
                    error
                );
            }
        }
    }

    // =====================================
    // REDIGER PROFIL - EDIT.CSHTML
    // =====================================

    const form = document.getElementById(
        "profile-edit-form"
    );

    // Kjører bare på redigeringssiden
    if (!form) {
        return;
    }

    const emailInput = document.getElementById(
        "profile-email"
    );

    const phoneInput = document.getElementById(
        "profile-phone"
    );

    const emailError = document.getElementById(
        "profile-email-error"
    );

    const phoneError = document.getElementById(
        "profile-phone-error"
    );

    const formError = document.getElementById(
        "profile-form-error"
    );

    // Fyller inn tidligere lagrede verdier
    const profile = getProfile();

    emailInput.value = profile.email;
    phoneInput.value = profile.phone;

    // Viser feilmelding for ett inputfelt
    function showFieldError(input, errorElement, message) {
        errorElement.textContent = message;
        input.setAttribute("aria-invalid", "true");
    }

    // Fjerner feilmelding
    function clearFieldError(input, errorElement) {
        errorElement.textContent = "";
        input.removeAttribute("aria-invalid");
    }

    // Fjerner feil mens brukeren retter opp
    emailInput.addEventListener("input", function () {
        clearFieldError(emailInput, emailError);
        formError.hidden = true;
    });

    phoneInput.addEventListener("input", function () {
        clearFieldError(phoneInput, phoneError);
        formError.hidden = true;
    });

    // Når brukeren klikker Lagre endring
    form.addEventListener("submit", function (event) {

        // Hindrer vanlig innsending til serveren
        event.preventDefault();

        const email = emailInput.value.trim();
        const phone = phoneInput.value.trim();

        let valid = true;
        let firstInvalidInput = null;

        // Fjern gamle feil
        clearFieldError(emailInput, emailError);
        clearFieldError(phoneInput, phoneError);

        formError.textContent = "";
        formError.hidden = true;

        // Valider e-postadresse hvis den er oppgitt
        if (email && !emailInput.checkValidity()) {
            showFieldError(
                emailInput,
                emailError,
                "Skriv inn en gyldig e-postadresse."
            );

            valid = false;
            firstInvalidInput = emailInput;
        }

        // Valider telefonnummer hvis det er oppgitt
        if (phone) {

            const allowedCharacters = /^[+0-9 ()-]+$/;
            const digits = phone.replace(/\D/g, "");

            if (
                !allowedCharacters.test(phone) ||
                digits.length < 7 ||
                digits.length > 15
            ) {
                showFieldError(
                    phoneInput,
                    phoneError,
                    "Skriv inn et gyldig telefonnummer."
                );

                valid = false;

                if (!firstInvalidInput) {
                    firstInvalidInput = phoneInput;
                }
            }
        }

        // Minst én kontaktmåte må være oppgitt
        if (!email && !phone) {

            formError.textContent =
                "Du må oppgi e-postadresse eller telefonnummer.";

            formError.hidden = false;

            valid = false;
            firstInvalidInput = emailInput;
        }

        // Stopp hvis informasjonen er ugyldig
        if (!valid) {
            if (firstInvalidInput) {
                firstInvalidInput.focus();
            }

            return;
        }

        // Lagre verdiene
        const saved = saveProfile(email, phone);

        if (!saved) {
            formError.textContent =
                "Kunne ikke lagre testdata i nettleseren.";

            formError.hidden = false;
            return;
        }

        // Går tilbake til Min profil
        const returnUrl = form.dataset.returnUrl;

        if (returnUrl) {
            window.location.assign(returnUrl);
        }
    });
});
