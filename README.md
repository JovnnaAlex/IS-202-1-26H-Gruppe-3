# Kriseportal – IS-202 Gruppe 3

Kriseportal er en webapplikasjon utviklet i ASP.NET Core MVC som skal gjøre det enklere å registrere, administrere og få oversikt over tilgjengelige ressurser og behov i en krisesituasjon.

Løsningen skal bidra til å koble offentlige aktører som har behov for ressurser, med privatpersoner, frivillige organisasjoner og bedrifter som kan tilby hjelp.

Applikasjonen bruker MariaDB til lagring av informasjon og ASP.NET Core Identity til brukerregistrering, innlogging og tilgangsstyring. Både webapplikasjonen og databasen kan kjøres i Docker.

## Funksjonalitet

Applikasjonen inneholder følgende funksjonalitet:

- **Brukerregistrering og innlogging:** Brukere kan opprette konto og logge inn. Innlogging og tilgangsstyring håndteres gjennom ASP.NET Core Identity.
- **Registrering av behov:** Offentlige aktører kan registrere behov gjennom `/Need/Create`. Behovene lagres i MariaDB og knyttes til den innloggede brukeren.
- **Registrering av ressurser:** Ressursleverandører kan registrere tilgjengelige ressurser gjennom `/Resource/Create`. Informasjonen lagres i databasen.
- **Geografisk plassering:** Leaflet brukes til å velge geografisk plassering ved registrering av behov og ressurser. Koordinatene sendes sammen med skjemadataene.
- **Situasjonskart:** Registrerte behov og ressurser kan vises geografisk på kartet.
- **Oversikt over behov og ressurser:** Brukerne kan se registrert informasjon gjennom egne oversiktssider.
- **Brukerprofil:** Applikasjonen har egne sider for visning og redigering av brukerprofil.
- **Tilgangsstyring:** Brukerroller brukes til å begrense hvilke funksjoner ulike aktører har tilgang til.

Løsningen er fortsatt under utvikling, og enkelte funksjoner og begrensninger kan endres frem mot ferdigstillelse.

## Systemarkitektur

Kriseportalen er utviklet med ASP.NET Core MVC og følger Model-View-Controller-arkitekturen.

- **Models og ViewModels:** Representerer data og håndterer informasjon mellom brukergrensesnittet og applikasjonen.
- **Controllers:** Behandler forespørsler fra brukeren, validerer data og kommuniserer med databasen.
- **Views:** Razor-sider (`.cshtml`) som viser skjemaer, oversikter og detaljer.
- **Database:** MariaDB brukes til å lagre brukere, behov, ressurser og tilhørende informasjon.
- **Entity Framework Core:** Brukes til kommunikasjon mellom applikasjonen og databasen gjennom `ApplicationDbContext`.
- **ASP.NET Core Identity:** Håndterer brukerregistrering, innlogging og rollebasert tilgangsstyring.
- **Kart:** Leaflet brukes til geografisk plassering og visualisering av behov og ressurser.
- **Docker:** Docker Compose starter webapplikasjonen og MariaDB som separate containere.
- **.NET Aspire:** Brukes som alternativt utviklingsmiljø for å starte og administrere applikasjonen og databasen.

### Prosjektstruktur

- `Kriseportal.Web/Controllers/` – Controllers for applikasjonens funksjonalitet.
- `Kriseportal.Web/Models/` – Datamodeller og entiteter.
- `Kriseportal.Web/Viewmodels/` – ViewModels for skjemaer og visning.
- `Kriseportal.Web/Views/` – Razor Views for brukergrensesnittet.
- `Kriseportal.Web/DataAccess/` – Databasekontekst og datatilgang.
- `Kriseportal.Web/wwwroot/` – CSS, JavaScript, bilder og andre statiske filer.
- `Kriseportal.Web/Dockerfile` – Instruksjoner for bygging av webapplikasjonen i Docker.
- `docker-compose.yml` – Konfigurasjon for oppstart av webapplikasjon og database.
- `Kriseportal.AppHost/` – Oppstart og administrasjon av tjenester gjennom .NET Aspire.

## Drift og kjøreinstruksjoner

Kriseportalen kan kjøres på to måter:
1. Docker Compose – starter både webapplikasjonen og MariaDB.
2. .NET Aspire – brukes som alternativt utviklingsmiljø.

### Forutsetninger

Før oppstart må følgende være installert:

- Docker Desktop
- Git
- .NET SDK (ved utvikling med Aspire)
- JetBrains Rider, Visual Studio eller en annen egnet kodeeditor (ved utvikling)

**Viktig:** Docker Desktop må være startet før applikasjonen kjøres, også ved bruk av Aspire.

### 1. Kjør hele løsningen med Docker Compose

Dette er den anbefalte metoden for å starte hele løsningen.

**Steg 1: Klon prosjektet**

```bash
git clone https://github.com/JovnnaAlex/IS-202-1-26H-Gruppe-3.git
cd IS-202-1-26H-Gruppe-3
```
**Steg 2: Start Docker Desktop**

Åpne Docker Desktop og vent til Docker Engine kjører.

**Steg 3: Bygg og start applikasjonen**

Kjør følgende kommando fra prosjektets rotmappe:

```bash
docker compose up --build
```

Docker Compose vil:

- Starte MariaDB.
- Bygge og starte webapplikasjonen.
- Vente til databasen er klar før webapplikasjonen starter.
- Kjøre databasemigreringer og legge inn testdata ved oppstart.

**Steg 4: Åpne applikasjonen**

Åpne `http://localhost:8080` i nettleseren.

**Steg 5: Logg inn med en testbruker**

| Bruker | E-post | Passord |
|---|---|---|
| Offentlig aktør | kommune@kriseportal.test | Kriseportal1! |
| Ressursleverandør | ola@kriseportal.test | Kriseportal1! |

Innlogging: `http://localhost:8080/Account/Login`

Registrering: `http://localhost:8080/Account/Register`

**Steg 6: Stopp applikasjonen**

Trykk `Ctrl + C` i terminalen, og kjør:

```bash
docker compose down
```

For å slette databasen og alle lagrede data:

```bash
docker compose down -v
```

**Merk:** `-v` sletter også Docker-volumet som inneholder databasen.

### 2. Kjør løsningen med .NET Aspire

Aspire kan brukes under lokal utvikling.

1. Start Docker Desktop.
2. Åpne prosjektet i Rider eller Visual Studio.
3. Start `Kriseportal.AppHost`.
4. Åpne Aspire-dashboardet og finn adressen til webapplikasjonen.

Portnummeret til webapplikasjonen kan variere mellom utviklingsmiljøer.

### Database

Applikasjonen bruker MariaDB 11.4.

Ved kjøring med Docker Compose:

- Database: `kriseportal`
- Databaseport på vertsmaskinen: `3308`
- Intern databaseport: `3306`
- Databasen lagres i Docker-volumet `kriseportal-docker-data`.

Ved kjøring med Aspire brukes port `3307` for MariaDB i det nåværende oppsettet.

### Kjente begrensninger

- Docker-oppsettet er konfigurert for lokal utvikling og er ikke ment for produksjon.
- Databasepassordet ligger i `docker-compose.yml` og må håndteres sikrere ved eventuell produksjonssetting.
- Nøklene for innloggingscookies lagres inne i webcontaineren. Brukere kan derfor bli logget ut når containeren opprettes på nytt.

## Testing

Løsningen er testet lokalt på macOS med Docker Desktop.

### Gjennomførte tester

| Test | Resultat |
|---|---|
| `docker compose up --build` | Bestått |
| Oppstart av MariaDB | Bestått |
| Database healthcheck | Bestått |
| Oppstart av webapplikasjonen | Bestått |
| Åpning av `http://localhost:8080` | Bestått |
| Innlogging med testbruker | Bestått |
| Registrering og visning av kartkoordinater | Bestått |

### Videre testing

Følgende funksjonalitet bør også kontrolleres:

- Registrering av behov og ressurser.
- Lagring av informasjon i databasen.
- Visning av registrerte behov og ressurser.
- Responsivt design på mobil og PC.


---

# KI-logg


## Edgar

**Verktøy:** ChatGPT

Jeg brukte ChatGPT som støtte i arbeidet med ViewModels. KI ble brukt til å forklare hva en ViewModel er og hvilken rolle den har i MVC, foreslå struktur for `NeedViewModel` og `ResourceViewModel`, forklare datatyper som `string`, `bool` og `double`, og hjelpe med navngivning av properties som `Type`, `Priority`, `Available`, `Latitude` og `Longitude`.

Jeg brukte også ChatGPT til å finne og forstå feil i koden, blant annet manglende `public`, feil property-navn og problemer med namespaces.

Forslag fra KI ble kontrollert og tilpasset prosjektets struktur før koden ble lagt inn i GitHub.

**Prompter:**
- «Hva er en ViewModel og hva skal den gjøre?»
- «Hvor skal jeg lage ViewModels-mappen i prosjektet?»
- «Hva betyr feilen NeedViewModel could not be found?»
- «Hvordan legger jeg ViewModel-koden til GitHub?»

---

## Alex

**Verktøy:** ChatGPT
Jeg har brukt ChatGPT som støtte gjennom utviklingen av Kriseportalen i IS-202. KI har hovedsakelig blitt brukt til å forstå programmeringskonsepter, få veiledning under implementering, feilsøking og bruk av Git og GitHub.

**Bruksområder:**
- **ASP.NET Core MVC:** Veiledning i utviklingen av Views/Need/Create.cshtml, inkludert Razor Views, asp-for, ViewModels, validering og skjemafunksjonalitet.
- **Leaflet og kart:** Hjelp til å forstå hvordan kartet kobles til registreringsskjemaet, og hvordan breddegrad og lengdegrad håndteres.
- **Git og GitHub:** Veiledning om opprettelse av branches, merging, oppdatering fra master, håndtering av merge konflikter og feilmeldinger. 
-**Frontend og design:** Diskusjoner om hvordan felles layout og navigasjon kunne utvikles i samarbeid med Figma-designet.
-**Prosjektplanlegging:** Veiledning rundt GitHub Projects, backlog, sprintplanlegging, arbeidsfordeling og avhengigheter mellom oppgaver. 
-**Testing og dokumentasjon:** Hjelp til å forstå hvilke tester som bør gjennomføres for å kontrollere at funksjonaliteten oppfyller prosjektkravene.
  
Eksempler på prompts jeg har brukt:


**Eksempler på prompter:**
-«Hvordan bruker jeg asp-for for å koble inputfeltene i skjemaet til properties i NeedViewModel?»
-«Hvordan lager jeg Priority som en dropdown med Low, Medium og High i Razor View-en?»
-«Hvordan kobler jeg Leaflet og map.js til id="map" i `Create.cshtml`?»
-«Hvordan legger jeg til validering med asp-validation-for?»
-«Hva kan jeg gjøre for min del av prosjektet basert på kravene i IS-202?»

---

## Sara

**Verktøy:** ChatGPT

Jeg brukte ChatGPT som støtte under utviklingen av `NeedController` og `ResourceController`. KI ble brukt til å forstå hvordan Controllers fungerer i ASP.NET Core MVC, hvordan GET brukes til å vise skjemaene, og hvordan POST mottar data gjennom ViewModels og sender dem videre til Details-sidene.

Jeg brukte også ChatGPT til testing og feilsøking da Controller-koden skulle integreres med resten av gruppeprosjektet. Dette inkluderte tilpasning av namespaces og ViewModels, håndtering av merge-konflikter og kontroll av at Controllers, Views og ViewModels fungerte sammen etter merge. KI ble også brukt som veiledning for Git og GitHub.

Forslag fra KI ble tilpasset prosjektets struktur og testet lokalt før koden ble lagt inn i GitHub.

**Prompter:**
- «Hvordan lager jeg NeedController og ResourceController med GET og POST i ASP.NET Core MVC?»
- «Hvordan tester jeg at GET og POST fungerer, og at data fra skjemaet vises på Details-siden?»
- «Hvordan tilpasser jeg Controller-koden til ViewModels og Views i prosjektet?»
- «Hvordan løser jeg merge-konflikter uten å overskrive de andre gruppemedlemmenes arbeid?»
- «Hvordan oppdaterer jeg branchen min med siste versjon av master?»
- «Hvordan tester jeg prosjektet etter at koden er merget?»

---

## Ada

**Verktøy:** ChatGPT

Jeg har brukt ChatGPT som en samtalepartner og veileder gjennom utviklingen av Kriseportalen. Jeg kommuniserer med KI omtrent som jeg ville snakket med et menneske. Jeg forklarer hva jeg jobber med, stiller spørsmål, diskuterer forslag og følger opp når jeg ikke forstår noe eller ønsker en annen løsning.

Jeg foretrekker å jobbe steg for steg, slik at jeg forstår hva jeg gjør fremfor å bare kopiere ferdig kode.

**Bruksområder:**
- **Kartfunksjonalitet:** Utvikling og feilsøking med Leaflet, JavaScript og geografiske koordinater.
- **Programmering:** Forståelse av ASP.NET Core MVC, Razor Views og feilmeldinger.
- **Git og GitHub:** Branches, commits, merging, pull requests og håndtering av konflikter.
- **Docker og testing:** Oppstart av applikasjonen og MariaDB, tolking av terminalmeldinger og testing.
- **Dokumentasjon:** Strukturering og oppdatering av README med systemarkitektur, kjøreinstruksjoner og testresultater.

**Eksempler på prompter:**
- «Hvordan lager jeg et Leaflet-kart hvor brukeren kan klikke og plassere en markør?»
- «Hvordan kobler jeg map.js til registreringsskjemaet?»
- «Hva betyr feilen om Scripts-seksjonen i _Layout.cshtml?»
- «Hvordan merger jeg master inn i branchen min?»
- «Hvordan tester jeg at Docker Compose starter både appen og databasen?»
- «Hvordan dokumenterer jeg prosjektets funksjonalitet og testing i README?»

Jeg bruker KI som støtte til å forstå og løse problemer, men gjennomfører arbeidet, vurderer forslagene og tester løsningene selv.

---

## Emma Sofie

**Verktøy:** Claude

Jeg brukte Claude som støtte under utviklingen av `Views/Resource/Create.cshtml`. KI ble brukt til å kontrollere at skjemaet dekket relevante krav i oppgaven, forklare en steg-for-steg-fremgangsmåte for å bygge en MVC-side og gi støtte til forståelsen av Razor og MVC.

Jeg brukte også Claude til å forstå forskjellen mellom HTML og Razor-filer (`.cshtml`), hvordan `asp-for` kobler skjemafeltene til `ResourceViewModel`, hvorfor koordinatene sendes gjennom skjulte felt, og hvordan filstruktur og navngivning brukes av MVC.

Under testing ble KI brukt som støtte til feilsøking da koordinatene på resultatsiden ble vist som `0,0`. Mulige årsaker knyttet til behandling av desimaltall og koordinater ble undersøkt og videreformidlet til gruppen for videre testing.

Forslag fra KI ble vurdert og tilpasset prosjektets struktur før koden ble lagt inn i GitHub.

**Prompter:**
- «Dette er oppgaven vår og gruppas arbeidsfordeling. Jeg er person nummer 4 og har ansvar for Registrer ressurs. Jeg ønsker støtte til koden min og at jeg har forstått oppgaven riktig. Jeg trenger også steg for steg hvordan det er lurt å begynne og hvor jeg kan finne god info om å sette opp oppgaven riktig. Jeg vil dobbeltsjekke med deg som støtte underveis.»
- «Filen min skal være en html, men jeg forstår også at det er noe som heter cshtml, hva er forskjellen?»
- «Markøren på resultatsiden havner midt i havet og der ønsker vi ikke at den skal være. Vi tror det har noe med koordinatene å gjøre, hva kan eventuelt være feilen?»

---

## Almir

**Verktøy:** Claude

Jeg brukte Claude som støtte under arbeidet med `Views/Need/Details.cshtml`, `Views/Resource/Details.cshtml` og `wwwroot/css/details.css`. Dette er sidene som viser informasjonen som brukeren har sendt inn, kartpunktet for valgt lokasjon og det responsive designet på resultatsidene.

KI ble brukt som støtte under utviklingen og til å forstå hvordan Details-sidene kunne kobles til dataene som ble sendt inn gjennom ViewModels. Jeg skrev selv koden inn i VS Code, satte sammen filene i prosjektet, rettet feil og fulgte opp arbeidet gjennom Git og GitHub.

Forslag fra KI ble vurdert og tilpasset prosjektet før koden ble lagt inn i GitHub.
