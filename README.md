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

Jeg brukte ChatGPT som støtte under utviklingen av `Views/Need/Create.cshtml`. KI ble brukt til å forstå Razor Views, koblingen til `NeedViewModel`, bruk av `asp-for`, validering og hvordan kart og koordinater kunne kobles til skjemaet.

Jeg brukte også ChatGPT til feilsøking av `@model` og namespaces, samt veiledning rundt Git og merging.

Forslag fra KI ble kontrollert og tilpasset prosjektets struktur før koden ble lagt inn i GitHub.

**Prompter:**
- «Hvordan kobler jeg inputfeltene til NeedViewModel med asp-for?»
- «Hvordan lager jeg Priority som en dropdown med Low, Medium og High?»
- «Hvordan kobler jeg Leaflet og map.js til id="map" i Create.cshtml?»
- «Hvordan legger jeg til validering med asp-validation-for?»
- «Kan du kontrollere om Create.cshtml er riktig strukturert med skjema, validering, kart og submit-knapp?»

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

Jeg brukte ChatGPT som støtte under utviklingen av kartfunksjonaliteten med Leaflet og `map.js`. KI ble brukt til å forstå hvordan brukeren kan velge en lokasjon ved å klikke på kartet, hvordan en markør kan plasseres og flyttes, og hvordan `Latitude` og `Longitude` kan lagres og sendes videre med registreringsskjemaet.

Jeg brukte også ChatGPT til testing og feilsøking etter at gruppens ulike deler ble merget. Dette inkluderte å forstå feilmeldinger, kontrollere samspillet mellom View, ViewModel og Controller, og finne årsaken til problemet med `Scripts`-seksjonen i `_Layout.cshtml`. KI ble også brukt som veiledning for Git og GitHub, blant annet branches, commit, push, pull, merging og kontroll av prosjektet med `dotnet build`.

Forslag fra KI ble testet lokalt og vurdert før endringer ble lagt inn i GitHub.

**Prompter:**
- «Hvordan lager jeg et Leaflet-kart hvor brukeren kan klikke på kartet og plassere en markør?»
- «Hvordan kan Latitude og Longitude lagres når brukeren klikker på kartet?»
- «Hvordan kobler jeg map.js til registreringsskjemaet?»
- «Hva betyr feilen om at Scripts-seksjonen ikke blir vist i _Layout.cshtml?»
- «Hvordan tester jeg at kart, skjema, Controller og Details-side fungerer sammen?»
- «Hvordan merger jeg master inn i branchen min og kontrollerer at prosjektet fortsatt bygger?»
- «Hvordan committer og pusher jeg bare filene jeg faktisk har endret?»

---

## Emma Sofie Hartvigsen

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