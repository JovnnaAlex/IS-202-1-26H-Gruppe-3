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
- Sletting av ressurser er foreløpig ikke bekreftet implementert. Ressurser kan deaktiveres og reaktiveres.
- Ressursleverandørens dashboard har foreløpig begrenset visning av registrerte ressurser og statistikk.

## Testing

Løsningen er testet lokalt på macOS med Docker Desktop. Testingen omfatter automatiserte tester og manuelle kontroller av sentrale funksjoner.

### 1. Automatiserte tester

Prosjektet inneholder automatiserte tester i `Kriseportal.Tests/`.

Testene kjøres fra prosjektets rotmappe med:

```bash
dotnet test
```

**Resultat fra siste testkjøring:**

- Totalt: 28 tester
- Bestått: 28
- Feilet: 0

Testprosjektet inneholder blant annet `AuthorizationTests.cs`, som kontrollerer tilgangsstyring.

De automatiserte testene er gjennomført etter at endringene fra `master` ble integrert.

### 2. Systemtesting

Systemtesting er gjennomført manuelt for å kontrollere at applikasjonens komponenter fungerer sammen.

| Testscenario | Forventet resultat | Resultat |
|---|---|---|
| Starte applikasjonen med `docker compose up --build` | Webapplikasjonen og MariaDB starter | Bestått |
| Kontrollere MariaDB | Databasen starter og blir markert som healthy | Bestått |
| Åpne applikasjonen | Forsiden vises på `localhost:8080` | Bestått |
| Logge inn som offentlig aktør | Brukeren får tilgang til offentlig dashboard | Bestått |
| Logge inn som ressursleverandør | Brukeren får tilgang til ressursleverandørens dashboard | Bestått |
| Registrere en ressurs | Registreringsskjemaet kan fylles ut og sendes inn | Bestått |
| Lagre ressurs i databasen | Ressursen lagres og kan hentes frem igjen | Bestått |
| Redigere en ressurs | Endringene lagres | Bestått |
| Deaktivere og reaktivere ressurs | Ressursens status kan endres | Bestått |
| Velge geografisk plassering | Brukeren kan plassere en markør på kartet | Bestått |
| Vise ressursdetaljer | Registrert informasjon og kartpunkt vises | Bestått |

### 3. Sikkerhetstesting

Sikkerhetstesting er gjennomført for å kontrollere at rollebasert tilgangsstyring fungerer.

| Testscenario | Forventet resultat | Resultat |
|---|---|---|
| Offentlig aktør logger inn | Brukeren sendes til riktig dashboard | Bestått |
| Ressursleverandør logger inn | Brukeren sendes til riktig dashboard | Bestått |
| Offentlig aktør forsøker å åpne ressursleverandørens dashboard | Tilgang nektes | Bestått |
| Ressursleverandør forsøker å åpne offentlig dashboard | Tilgang nektes | Bestått |

Applikasjonen benytter ASP.NET Core Identity for autentisering og rollebasert autorisering.

Sikkerhetstiltakene er testet gjennom automatiserte tester og manuelle kontroller. Dette er ikke en fullstendig sikkerhetsrevisjon.

### 4. Brukervennlighetstesting

Det er gjennomført manuelle kontroller av navigasjon og registreringsflyt.

| Testscenario | Forventet resultat | Resultat |
|---|---|---|
| Navigere fra ressursleverandørens sidemeny | Menyvalgene åpner riktige sider | Bestått |
| Åpne registreringsskjemaet | Riktig layout og sidemeny vises | Bestått |
| Gå fra registrering til forhåndsvisning | Informasjonen vises i riktig layout | Bestått |
| Åpne bekreftelsessiden | Bekreftelsen vises med riktig layout | Bestått |
| Trykke «Tilbake til dashboard» | Brukeren sendes til ressursleverandørens dashboard | Bestått |

Testene er gjennomført som manuelle funksjons- og brukervennlighetskontroller under utviklingen. Det er ikke gjennomført en formell brukertest med eksterne testpersoner.

### 5. Videre testing og kjente mangler

Følgende funksjoner bør kontrolleres nærmere:

- Sletting av ressurser er ikke bekreftet implementert. Deaktivering er tilgjengelig, men er ikke det samme som sletting.
- Hele flyten for behovsregistrering bør gjennomgås og dokumenteres.
- Ressursoversikten på dashboardet bør kontrolleres mot dataene i databasen.
- Responsivt design bør testes systematisk på flere skjermstørrelser.

---

# KI-logg


## Edgar

**Verktøy:** ChatGPT

Jeg har brukt ChatGPT som støtte under utviklingen av Kriseportalen i IS-202. KI ble brukt til kodeforslag, stegvis veiledning, forklaring av programmeringsbegreper og feilsøking.

**Bruksområder:**

- **ViewModels:** Forklaringer og forslag til struktur for `NeedViewModel`, `ResourceViewModel`, `RegisterViewModel` og `LoginViewModel`. Dette inkluderte datatyper, properties, navngivning og valideringsregler.
- **Skjemaer og validering:** Hjelp med koblingen mellom registreringsskjemaer og ViewModels, kontroll av innsendte opplysninger med `ModelState.IsValid` og visning av feilmeldinger med `asp-validation-for`, blant annet i `Views/Need/Create.cshtml`.
- **Registrering, innlogging og utlogging:** Kodeforslag og veiledning under arbeidet med `AccountController`, `Views/Account/Register.cshtml` og `Views/Account/Login.cshtml`. Funksjonaliteten ble tilpasset prosjektets eksisterende `ApplicationUser` og ASP.NET Core Identity.
- **Kodeforståelse og feilsøking:** Forklaringer av GET og POST og hvordan Controllers, Views og ViewModels samarbeider. KI ble også brukt til hjelp med manglende `public`, feil property-navn og problemer med namespaces.
- **Testing og oppstart:** Veiledning ved testing av validering, brukerregistrering, innlogging, utlogging og feil innlogging, samt feilsøking ved oppstart med Visual Studio, Docker og Aspire.
- **Git og GitHub:** Hjelp med branches, commits, push, pull requests, oppdatering fra master, merging og håndtering av merge-konflikter.

Jeg har brukt konkrete kodeforslag fra KI og fått hjelp til å legge dem inn og tilpasse dem til prosjektets struktur. Registrering, innlogging, utlogging og feil innlogging ble testet lokalt. Jeg har også brukt KI til å forklare koden underveis, og har hatt behov for flere gjennomganger for å forstå logikken bak forslagene.

**Eksempler på spørsmål og temaer fra samtalene:**

- Hva er en ViewModel, og hva skal den gjøre?
- Hvordan kontrollerer vi skjemaopplysninger og viser feilmeldinger?
- Hvordan bygger vi registrering og innlogging på Identity-oppsettet som allerede finnes?
- Hvordan får jeg gruppens endringer inn i min branch?
- Hvordan håndterer jeg merge-konflikter og sender endringene mine til GitHub?

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

Jeg har brukt ChatGPT som støtte under utviklingen av Kriseportalen i IS-202. KI har hovedsakelig blitt brukt til veiledning innen ASP.NET Core MVC, frontendutvikling, design, feilsøking og Git/GitHub.

**Bruksområder:**

- **ASP.NET Core MVC:** Veiledning i utviklingen av Controllers, bruk av GET og POST, ViewModels og hvordan ulike deler av systemet kommuniserer.
- **Innlogging og brukerroller:** Støtte til implementering av registrering og innlogging med ASP.NET Core Identity, samt rollebasert tilgang for ressursleverandører.
- **Frontend og design:** Brukt som støtte til utviklingen av ressursleverandørportalen, inkludert dashboard, navigasjon, layout, fargevalg og ikoner.
- **Logo og visualisering:** Brukt KI til å visualisere egne designideer og skisser basert på krav og ønsker jeg har definert, samt videreutvikle logo og visuelle elementer.
- **Testing og feilsøking:** Veiledning ved integrering av Controllers, Views og ViewModels, samt feilsøking knyttet til Docker, Aspire og databasen.
- **Git og GitHub:** Støtte til branching, merging, håndtering av konflikter og oppdatering av kode i gruppens repository.

KI-forslagene ble vurdert og tilpasset prosjektets eksisterende struktur, funksjonelle krav og design. Jeg har selv tatt valg knyttet til utforming, funksjonalitet og videre utvikling av løsningen.

**Eksempler på prompter:**

- «Hvordan kan jeg strukturere Controllers og Views for å sikre god sammenheng mellom frontend og backend?»
- «Hvordan kan jeg implementere rollebasert innlogging med ASP.NET Core Identity?»
- «Hvordan kan jeg utvikle et oversiktlig dashboard med god navigasjon og tydelig visuell struktur?»
- «Hvordan kan jeg visualisere og videreutvikle et design basert på egne skisser og krav?»
- «Hvordan kan jeg koble eksisterende funksjonalitet til nye sider uten å påvirke andre deler av systemet?»
- «Hvordan kan jeg feilsøke problemer med Docker og Aspire under lokal utvikling?»
- «Hvordan kan jeg håndtere merge-konflikter og holde min branch oppdatert?»

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

Jeg brukte Claude som støtte under utviklingen av `Views/Resource/Create.cshtml`. KI ble brukt til å forstå Razor og MVC, hvordan `asp-for` kobler skjemafeltene til `ResourceViewModel`, og hvorfor koordinatene sendes gjennom skjulte felt. Under testing ble KI brukt til feilsøking da koordinatene på resultatsiden ble vist som `0,0`.

Senere brukte jeg Claude som støtte i arbeidet med databasen og kartet. KI ble brukt til å forklare EF Core og migrasjoner, foreslå struktur for testdataene i `DataSeeder` og dele opp situasjonskartet med Leaflet i mindre steg. Jeg brukte også KI til feilsøking i Docker Compose og til å forstå en sårbarhet i MySqlConnector.

Jeg ba om forklaringer steg for steg, slik at jeg forsto koden før jeg skrev den inn selv. I felles filer gjorde jeg bare små, målrettede endringer, og jeg sjekket `git diff --stat` før hver commit.

Forslag fra KI ble vurdert, tilpasset prosjektets struktur og testet lokalt før koden ble lagt inn i GitHub.

**Prompter:**
- «Filen min skal være en html, men det finnes også cshtml. Hva er forskjellen?»
- «Markøren på resultatsiden havner midt i havet. Vi tror det har med koordinatene å gjøre. Hva kan være feilen?»
- «Hvordan kobler jeg ASP.NET Core til MariaDB med EF Core og Pomelo?»
- «Jeg vil lage et situasjonskart med Leaflet der behovene vises etter prioritet. Kan du dele det opp i steg jeg kan gjøre selv?»
- «Webappen finner ikke databasen når jeg kjører Docker Compose. Hva kan være feilen?»

---

## Almir

**Verktøy:** Claude

Jeg brukte Claude som støtte under arbeidet med `Views/Need/Details.cshtml`, `Views/Resource/Details.cshtml` og `wwwroot/css/details.css`. Dette er sidene som viser informasjonen som brukeren har sendt inn, kartpunktet for valgt lokasjon og det responsive designet på resultatsidene.

KI ble brukt som støtte under utviklingen og til å forstå hvordan Details-sidene kunne kobles til dataene som ble sendt inn gjennom ViewModels. Jeg skrev selv koden inn i VS Code, satte sammen filene i prosjektet, rettet feil og fulgte opp arbeidet gjennom Git og GitHub.

Forslag fra KI ble vurdert og tilpasset prosjektet før koden ble lagt inn i GitHub.

**Oppgave 2 (backend):**

I oppgave 2 hadde jeg ansvar for B06 (ApplicationUser og roller), B08 (fjerne rester fra Aspire-malen) og B12 (tilgangsstyring). Jeg kartla hva som manglet ut fra casen og koden i master, planla arbeidet, testet med `dotnet build` og `dotnet test`, og håndterte grener, merge og pull requests. Claude ble brukt som støtte til kodeutkast, forklaringer og feilsøking. Jeg gikk gjennom og godkjente alle endringer før de ble pushet.
