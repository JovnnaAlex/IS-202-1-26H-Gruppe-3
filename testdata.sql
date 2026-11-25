-- =====================================================================
-- Felles testdata for Kriseportalen (IS-201 Mappeoppgave 2)
-- Kjør hele scriptet på nytt når som helst: det sletter gamle testdata
-- først, slik at alle i gruppa får nøyaktig de samme dataene.
-- NB: Sletter ALLE ressurser, behov, koblinger, forespørsler og varsler.
--     Egne innloggingskontoer blir ikke slettet.
-- =====================================================================

SET NAMES utf8mb4;
START TRANSACTION;

-- 1. Slett gamle data (rekkefølgen følger fremmednøklene)
DELETE FROM Notifications;
DELETE FROM Requests;
DELETE FROM Matches;
DELETE FROM Needs;
DELETE FROM Resources;
DELETE FROM AspNetUsers WHERE Id LIKE 'test-user-%';  -- AspNetUserRoles slettes med CASCADE

-- 2. Kategori-ID-er hentes med navn, siden ID-ene kan være ulike hos hver enkelt
SET @mannskap = (SELECT CategoryId FROM Categories WHERE Name = 'Mannskap');
SET @mat = (SELECT CategoryId FROM Categories WHERE Name = 'Mat');
SET @evakuering = (SELECT CategoryId FROM Categories WHERE Name = 'Evakuering');
SET @varme = (SELECT CategoryId FROM Categories WHERE Name = 'Varme');
SET @samband = (SELECT CategoryId FROM Categories WHERE Name = 'Samband');
SET @maskiner = (SELECT CategoryId FROM Categories WHERE Name = 'Maskiner');
SET @snorydding = (SELECT CategoryId FROM Categories WHERE Name = 'Snørydding');
SET @sand = (SELECT CategoryId FROM Categories WHERE Name = 'Sand og grus');
SET @transport = (SELECT CategoryId FROM Categories WHERE Name = 'Transport');
SET @strom = (SELECT CategoryId FROM Categories WHERE Name = 'Strøm');
SET @vann = (SELECT CategoryId FROM Categories WHERE Name = 'Vann');
SET @lokaler = (SELECT CategoryId FROM Categories WHERE Name = 'Lokaler');
SET @drone = (SELECT CategoryId FROM Categories WHERE Name = 'Drone');
SET @bat = (SELECT CategoryId FROM Categories WHERE Name = 'Båt');

-- 3. Brukere (12): 4 offentlige myndigheter og 8 ressurseiere
-- Agency:       0 Stat, 1 Kommune, 2 Politi, 3 Brann, 4 Helse, 5 Sivilforsvaret, 6 Forsvaret, 7 Heimevernet
-- ProviderType: 0 Privatperson, 1 Bedrift, 2 Bonde, 3 Entreprenør, 4 Frivillig organisasjon, 5 Droneoperatør
-- Testbrukerne har ikke passord og kan ikke brukes til å logge inn.
INSERT INTO AspNetUsers (Id, Name, Agency, ProviderType, UserName, NormalizedUserName, Email, NormalizedEmail,
  EmailConfirmed, SecurityStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsDeactivated) VALUES
('test-user-01', 'Kristiansand kommune beredskap', 1, NULL, 'beredskap@kristiansand.test', 'BEREDSKAP@KRISTIANSAND.TEST', 'beredskap@kristiansand.test', 'BEREDSKAP@KRISTIANSAND.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-02', 'Heimevernet Agder distrikt', 7, NULL, 'hv07@forsvaret.test', 'HV07@FORSVARET.TEST', 'hv07@forsvaret.test', 'HV07@FORSVARET.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-03', 'Agder politidistrikt', 2, NULL, 'operasjon@politi.test', 'OPERASJON@POLITI.TEST', 'operasjon@politi.test', 'OPERASJON@POLITI.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-04', 'Sivilforsvaret Agder', 5, NULL, 'agder@sivilforsvaret.test', 'AGDER@SIVILFORSVARET.TEST', 'agder@sivilforsvaret.test', 'AGDER@SIVILFORSVARET.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-05', 'Iveland frivillige beredskapslag', NULL, 4, 'post@ivelandfrivillige.test', 'POST@IVELANDFRIVILLIGE.TEST', 'post@ivelandfrivillige.test', 'POST@IVELANDFRIVILLIGE.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-06', 'Sørlandet Maskin AS', NULL, 3, 'post@sorlandetmaskin.test', 'POST@SORLANDETMASKIN.TEST', 'post@sorlandetmaskin.test', 'POST@SORLANDETMASKIN.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-07', 'Hægeland gårdsbruk', NULL, 2, 'gard@haegeland.test', 'GARD@HAEGELAND.TEST', 'gard@haegeland.test', 'GARD@HAEGELAND.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-08', 'Agder Transport AS', NULL, 1, 'post@agdertransport.test', 'POST@AGDERTRANSPORT.TEST', 'post@agdertransport.test', 'POST@AGDERTRANSPORT.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-09', 'Røde Kors Kristiansand', NULL, 4, 'kristiansand@rodekors.test', 'KRISTIANSAND@RODEKORS.TEST', 'kristiansand@rodekors.test', 'KRISTIANSAND@RODEKORS.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-10', 'Nilsen Droneservice', NULL, 5, 'post@nilsendrone.test', 'POST@NILSENDRONE.TEST', 'post@nilsendrone.test', 'POST@NILSENDRONE.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-11', 'Ole Kristian Berg', NULL, 0, 'ole.berg@epost.test', 'OLE.BERG@EPOST.TEST', 'ole.berg@epost.test', 'OLE.BERG@EPOST.TEST', 1, UUID(), 0, 0, 1, 0, 0),
('test-user-12', 'Setesdal Energi AS', NULL, 1, 'drift@setesdalenergi.test', 'DRIFT@SETESDALENERGI.TEST', 'drift@setesdalenergi.test', 'DRIFT@SETESDALENERGI.TEST', 1, UUID(), 0, 0, 1, 0, 0);

-- Roller (hentes med navn, siden rolle-ID-ene kan være ulike)
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id FROM AspNetUsers u JOIN AspNetRoles r ON r.NormalizedName =
  CASE WHEN u.Agency IS NOT NULL THEN 'PUBLICAUTHORITY' ELSE 'RESOURCEPROVIDER' END
WHERE u.Id LIKE 'test-user-%';

-- 4. Ressurser (75): 2023: 10, 2024: 15, 2025: 30, 2026: 20
-- Fordelingen er valgt slik at spørring 2–7 gir meningsfulle resultater.
INSERT INTO Resources (ResourceId, Name, CategoryId, Quantity, Unit, Availability, Latitude, Longitude,
  Municipality, ContactName, ContactPhone, CreatedAt, ProviderId) VALUES
(101, 'Minibuss 16 seter', @transport, 2, 'stk', 'Available', 58.470000, 8.776000, 'Arendal', 'Anne Larsen', '41000008', '2023-01-05 17:17:00', 'test-user-08'),
(102, 'Varm mat fra feltkjøkken', @mat, 150, 'porsjoner', 'Unavailable', 58.158000, 8.003000, 'Kristiansand', 'Lise Moen', '41000009', '2023-02-08 08:17:00', 'test-user-09'),
(103, 'Frivillige mannskap', @mannskap, 12, 'personer', 'Available', 58.161000, 8.007000, 'Kristiansand', 'Kari Iveland', '41000005', '2023-06-16 13:00:00', 'test-user-05'),
(104, 'Gravemaskin 20 tonn', @maskiner, 1, 'stk', 'Available', 58.164000, 8.011000, 'Kristiansand', 'Per Olsen', '41000006', '2023-07-19 14:00:00', 'test-user-06'),
(105, 'Aggregat 20 kVA', @strom, 2, 'stk', 'Available', 58.146000, 7.995000, 'Kristiansand', 'Jon Hægeland', '41000007', '2023-08-22 15:00:00', 'test-user-07'),
(106, 'Lastebil med kran', @transport, 1, 'stk', 'Reserved', 58.149000, 7.999000, 'Kristiansand', 'Anne Larsen', '41000008', '2023-09-25 16:00:00', 'test-user-08'),
(107, 'Søk- og redningsmannskap', @mannskap, 8, 'personer', 'Available', 58.152000, 8.003000, 'Kristiansand', 'Lise Moen', '41000009', '2023-10-01 17:00:00', 'test-user-09'),
(108, 'Feltrasjoner', @mat, 200, 'porsjoner', 'Available', 58.462000, 7.938000, 'Iveland', 'Kari Iveland', '41000005', '2023-10-23 14:17:00', 'test-user-05'),
(109, 'Drone med varmekamera', @drone, 2, 'stk', 'Available', 58.158000, 8.011000, 'Kristiansand', 'Tor Nilsen', '41000010', '2023-11-04 08:00:00', 'test-user-10'),
(110, 'Traktor med snøfres', @snorydding, 1, 'stk', 'Available', 58.161000, 7.995000, 'Kristiansand', 'Per Olsen', '41000006', '2023-11-26 15:17:00', 'test-user-06'),
(111, 'Lastebil med kran', @transport, 1, 'stk', 'Available', 58.479000, 8.776000, 'Arendal', 'Anne Larsen', '41000008', '2024-01-05 17:17:00', 'test-user-08'),
(112, 'Dieselvarmer 15 kW', @varme, 3, 'stk', 'Available', 58.453000, 7.934000, 'Iveland', 'Kari Iveland', '41000005', '2024-02-03 15:34:00', 'test-user-05'),
(113, 'Frivillige mannskap', @mannskap, 12, 'personer', 'Available', 58.149000, 8.007000, 'Kristiansand', 'Lise Moen', '41000009', '2024-02-08 08:17:00', 'test-user-09'),
(114, 'Hjullaster', @maskiner, 1, 'stk', 'Available', 58.152000, 8.011000, 'Kristiansand', 'Per Olsen', '41000006', '2024-03-06 16:34:00', 'test-user-06'),
(115, 'Kartleggingsdrone', @drone, 1, 'stk', 'Reserved', 58.349000, 8.593000, 'Grimstad', 'Tor Nilsen', '41000010', '2024-03-11 09:17:00', 'test-user-10'),
(116, 'Mannskap til sandsekking', @mannskap, 20, 'personer', 'Available', 58.158000, 7.999000, 'Kristiansand', 'Kari Iveland', '41000005', '2024-06-16 13:00:00', 'test-user-05'),
(117, 'Varebil', @transport, 3, 'stk', 'Available', 58.161000, 8.003000, 'Kristiansand', 'Per Olsen', '41000006', '2024-07-19 14:00:00', 'test-user-06'),
(118, 'Minigraver', @maskiner, 2, 'stk', 'Available', 58.164000, 8.007000, 'Kristiansand', 'Jon Hægeland', '41000007', '2024-08-22 15:00:00', 'test-user-07'),
(119, 'Feltsenger', @evakuering, 40, 'stk', 'Available', 58.146000, 8.011000, 'Kristiansand', 'Anne Larsen', '41000008', '2024-09-25 16:00:00', 'test-user-08'),
(120, 'Forsamlingslokale', @lokaler, 1, 'stk', 'Available', 58.149000, 7.995000, 'Kristiansand', 'Lise Moen', '41000009', '2024-10-01 17:00:00', 'test-user-09'),
(121, 'Evakueringstelt', @evakuering, 4, 'stk', 'Available', 58.459000, 7.930000, 'Iveland', 'Kari Iveland', '41000005', '2024-10-23 14:17:00', 'test-user-05'),
(122, 'Nødnett-radioer', @samband, 10, 'stk', 'Available', 58.155000, 8.003000, 'Kristiansand', 'Tor Nilsen', '41000010', '2024-11-04 08:00:00', 'test-user-10'),
(123, 'Strøsand', @sand, 20, 'tonn', 'Reserved', 58.158000, 8.007000, 'Kristiansand', 'Per Olsen', '41000006', '2024-11-26 15:17:00', 'test-user-06'),
(124, 'Aggregat 5 kW', @strom, 4, 'stk', 'Unavailable', 58.283000, 7.989000, 'Vennesla', 'Jon Hægeland', '41000007', '2024-12-02 16:17:00', 'test-user-07'),
(125, 'Båt med påhengsmotor', @bat, 1, 'stk', 'Available', 58.164000, 7.995000, 'Kristiansand', 'Ole Kristian Berg', '41000011', '2024-12-07 09:00:00', 'test-user-11'),
(126, 'Lastebil med kran', @transport, 1, 'stk', 'Available', 58.461000, 8.776000, 'Arendal', 'Anne Larsen', '41000008', '2025-01-05 17:17:00', 'test-user-08'),
(127, 'Aggregat 5 kW', @strom, 4, 'stk', 'Available', 58.149000, 8.003000, 'Kristiansand', 'Siri Bakke', '41000012', '2025-01-10 10:00:00', 'test-user-12'),
(128, 'Mannskap til sandsekking', @mannskap, 20, 'personer', 'Available', 58.459000, 7.938000, 'Iveland', 'Kari Iveland', '41000005', '2025-02-03 15:34:00', 'test-user-05'),
(129, 'Mannskap til sandsekking', @mannskap, 20, 'personer', 'Available', 58.155000, 8.011000, 'Kristiansand', 'Lise Moen', '41000009', '2025-02-08 08:17:00', 'test-user-09'),
(130, 'Nødnett-radioer', @samband, 10, 'stk', 'Available', 58.465000, 7.926000, 'Iveland', 'Kari Iveland', '41000005', '2025-02-18 11:16:00', 'test-user-05'),
(131, 'Evakueringstelt', @evakuering, 4, 'stk', 'Available', 58.468000, 7.930000, 'Iveland', 'Kari Iveland', '41000005', '2025-02-24 08:25:00', 'test-user-05'),
(132, 'Minibuss 16 seter', @transport, 2, 'stk', 'Available', 58.164000, 8.003000, 'Kristiansand', 'Per Olsen', '41000006', '2025-03-06 16:34:00', 'test-user-06'),
(133, 'Satellittelefon', @samband, 2, 'stk', 'Available', 58.340000, 8.605000, 'Grimstad', 'Tor Nilsen', '41000010', '2025-03-11 09:17:00', 'test-user-10'),
(134, 'Minigraver', @maskiner, 2, 'stk', 'Available', 58.149000, 8.011000, 'Kristiansand', 'Per Olsen', '41000006', '2025-03-27 09:25:00', 'test-user-06'),
(135, 'Mobil batteribank', @strom, 1, 'stk', 'Available', 58.274000, 7.973000, 'Vennesla', 'Jon Hægeland', '41000007', '2025-04-09 17:34:00', 'test-user-07'),
(136, 'Feltsenger', @evakuering, 40, 'stk', 'Available', 58.470000, 8.776000, 'Arendal', 'Anne Larsen', '41000008', '2025-05-12 08:34:00', 'test-user-08'),
(137, 'Varmekanon', @varme, 2, 'stk', 'Available', 58.352000, 8.601000, 'Grimstad', 'Kari Iveland', '41000005', '2025-06-04 09:42:00', 'test-user-05'),
(138, 'Feltrasjoner', @mat, 200, 'porsjoner', 'Available', 58.476000, 8.784000, 'Arendal', 'Kari Iveland', '41000005', '2025-06-10 16:51:00', 'test-user-05'),
(139, 'Varm mat fra feltkjøkken', @mat, 150, 'porsjoner', 'Reserved', 58.164000, 8.011000, 'Kristiansand', 'Lise Moen', '41000009', '2025-06-15 09:34:00', 'test-user-09'),
(140, 'Søk- og redningsmannskap', @mannskap, 8, 'personer', 'Available', 58.146000, 7.995000, 'Kristiansand', 'Kari Iveland', '41000005', '2025-06-16 13:00:00', 'test-user-05'),
(141, 'Pukk til veiutbedring', @sand, 40, 'tonn', 'Available', 58.464000, 8.776000, 'Arendal', 'Per Olsen', '41000006', '2025-07-13 17:51:00', 'test-user-06'),
(142, 'Gravemaskin 20 tonn', @maskiner, 1, 'stk', 'Available', 58.152000, 8.003000, 'Kristiansand', 'Per Olsen', '41000006', '2025-07-19 14:00:00', 'test-user-06'),
(143, 'Gravemaskin 20 tonn', @maskiner, 1, 'stk', 'Available', 58.470000, 8.784000, 'Arendal', 'Jon Hægeland', '41000007', '2025-08-16 08:51:00', 'test-user-07'),
(144, 'Vanntank', @vann, 1000, 'liter', 'Available', 58.158000, 8.011000, 'Kristiansand', 'Jon Hægeland', '41000007', '2025-08-22 15:00:00', 'test-user-07'),
(145, 'Minibuss 16 seter', @transport, 2, 'stk', 'Available', 58.476000, 8.772000, 'Arendal', 'Anne Larsen', '41000008', '2025-09-19 09:51:00', 'test-user-08'),
(146, 'Varebil', @transport, 3, 'stk', 'Unavailable', 58.164000, 7.999000, 'Kristiansand', 'Anne Larsen', '41000008', '2025-09-25 16:00:00', 'test-user-08'),
(147, 'Dieselvarmer 15 kW', @varme, 3, 'stk', 'Available', 58.146000, 8.003000, 'Kristiansand', 'Lise Moen', '41000009', '2025-10-01 17:00:00', 'test-user-09'),
(148, 'Søk- og redningsmannskap', @mannskap, 8, 'personer', 'Reserved', 58.456000, 7.938000, 'Iveland', 'Kari Iveland', '41000005', '2025-10-11 10:59:00', 'test-user-05'),
(149, 'Frivillige mannskap', @mannskap, 12, 'personer', 'Available', 58.459000, 7.942000, 'Iveland', 'Kari Iveland', '41000005', '2025-10-17 17:08:00', 'test-user-05'),
(150, 'Satellittelefon', @samband, 2, 'stk', 'Unavailable', 58.462000, 7.926000, 'Iveland', 'Kari Iveland', '41000005', '2025-10-23 14:17:00', 'test-user-05'),
(151, 'Drone med varmekamera', @drone, 2, 'stk', 'Available', 58.158000, 7.999000, 'Kristiansand', 'Tor Nilsen', '41000010', '2025-11-04 08:00:00', 'test-user-10'),
(152, 'Hjullaster', @maskiner, 1, 'stk', 'Available', 58.161000, 8.003000, 'Kristiansand', 'Per Olsen', '41000006', '2025-11-20 08:08:00', 'test-user-06'),
(153, 'Brøytebil', @snorydding, 1, 'stk', 'Unavailable', 58.164000, 8.007000, 'Kristiansand', 'Per Olsen', '41000006', '2025-11-26 15:17:00', 'test-user-06'),
(154, 'Traktor med snøfres', @snorydding, 1, 'stk', 'Reserved', 58.268000, 7.989000, 'Vennesla', 'Jon Hægeland', '41000007', '2025-12-02 16:17:00', 'test-user-07'),
(155, 'Aggregat 20 kVA', @strom, 2, 'stk', 'Unavailable', 58.149000, 7.995000, 'Kristiansand', 'Ole Kristian Berg', '41000011', '2025-12-07 09:00:00', 'test-user-11'),
(156, 'Frivillige mannskap', @mannskap, 12, 'personer', 'Available', 58.152000, 7.999000, 'Kristiansand', 'Kari Iveland', '41000005', '2026-01-16 13:00:00', 'test-user-05'),
(157, 'Feltrasjoner', @mat, 200, 'porsjoner', 'Available', 58.462000, 7.934000, 'Iveland', 'Kari Iveland', '41000005', '2026-01-23 14:17:00', 'test-user-05'),
(158, 'Søk- og redningsmannskap', @mannskap, 8, 'personer', 'Available', 58.465000, 7.938000, 'Iveland', 'Kari Iveland', '41000005', '2026-02-03 15:34:00', 'test-user-05'),
(159, 'Strøsand', @sand, 20, 'tonn', 'Available', 58.161000, 8.011000, 'Kristiansand', 'Per Olsen', '41000006', '2026-02-06 16:34:00', 'test-user-06'),
(160, 'Brøytebil', @snorydding, 1, 'stk', 'Available', 58.164000, 7.995000, 'Kristiansand', 'Per Olsen', '41000006', '2026-02-19 14:00:00', 'test-user-06'),
(161, 'Varebil', @transport, 3, 'stk', 'Reserved', 58.146000, 7.999000, 'Kristiansand', 'Per Olsen', '41000006', '2026-02-26 15:17:00', 'test-user-06'),
(162, 'Flaskevann', @vann, 500, 'liter', 'Unavailable', 58.271000, 7.981000, 'Vennesla', 'Jon Hægeland', '41000007', '2026-03-02 16:17:00', 'test-user-07'),
(163, 'Mobil batteribank', @strom, 1, 'stk', 'Available', 58.152000, 8.007000, 'Kristiansand', 'Jon Hægeland', '41000007', '2026-03-22 15:00:00', 'test-user-07'),
(164, 'Lastebil med kran', @transport, 1, 'stk', 'Available', 58.155000, 8.011000, 'Kristiansand', 'Anne Larsen', '41000008', '2026-04-25 16:00:00', 'test-user-08'),
(165, 'Minibuss 16 seter', @transport, 2, 'stk', 'Available', 58.473000, 8.772000, 'Arendal', 'Anne Larsen', '41000008', '2026-05-05 17:17:00', 'test-user-08'),
(166, 'Evakueringstelt', @evakuering, 4, 'stk', 'Available', 58.476000, 8.776000, 'Arendal', 'Anne Larsen', '41000008', '2026-06-12 08:34:00', 'test-user-08'),
(167, 'Lagerhall', @lokaler, 1, 'stk', 'Available', 58.164000, 8.003000, 'Kristiansand', 'Lise Moen', '41000009', '2026-08-01 17:00:00', 'test-user-09'),
(168, 'Mannskap til sandsekking', @mannskap, 20, 'personer', 'Available', 58.146000, 8.007000, 'Kristiansand', 'Lise Moen', '41000009', '2026-08-08 08:17:00', 'test-user-09'),
(169, 'Varmekanon', @varme, 2, 'stk', 'Reserved', 58.149000, 8.011000, 'Kristiansand', 'Lise Moen', '41000009', '2026-09-15 09:34:00', 'test-user-09'),
(170, 'Kartleggingsdrone', @drone, 1, 'stk', 'Available', 58.152000, 7.995000, 'Kristiansand', 'Tor Nilsen', '41000010', '2026-10-04 08:00:00', 'test-user-10'),
(171, 'Drone med varmekamera', @drone, 2, 'stk', 'Available', 58.349000, 8.597000, 'Grimstad', 'Tor Nilsen', '41000010', '2026-10-11 09:17:00', 'test-user-10'),
(172, 'Forsamlingslokale', @lokaler, 1, 'stk', 'Unavailable', 58.158000, 8.003000, 'Kristiansand', 'Ole Kristian Berg', '41000011', '2026-11-07 09:00:00', 'test-user-11'),
(173, 'Dieselvarmer 15 kW', @varme, 3, 'stk', 'Available', 58.161000, 8.007000, 'Kristiansand', 'Siri Bakke', '41000012', '2026-11-10 10:00:00', 'test-user-12'),
(174, 'Aggregat 20 kVA', @strom, 2, 'stk', 'Available', 58.267000, 8.393000, 'Lillesand', 'Ole Kristian Berg', '41000011', '2026-11-14 10:17:00', 'test-user-11'),
(175, 'Aggregat 5 kW', @strom, 4, 'stk', 'Available', 58.586000, 7.796000, 'Evje og Hornnes', 'Siri Bakke', '41000012', '2026-11-17 11:17:00', 'test-user-12');

-- 5. Behov (15)
INSERT INTO Needs (NeedId, Title, CategoryId, Priority, Quantity, Unit, NeededFrom, NeededTo, Latitude, Longitude,
  Municipality, ContactName, ContactPhone, Status, CreatedAt, PublicAuthorityId) VALUES
(201, 'Strømbrudd på sykehjem', @strom, 'Acute', 2, 'stk', '2025-01-12 07:00:00', '2025-01-15 18:00:00', 58.156000, 7.985000, 'Kristiansand', 'Marte Svendsen', '38000001', 'Assigned', '2025-01-12 06:40:00', 'test-user-01'),
(202, 'Mannskap til flomvakt ved Otra', @mannskap, 'High', 15, 'personer', '2025-03-04 08:00:00', '2025-03-10 20:00:00', 58.278000, 7.963000, 'Vennesla', 'Kaptein Eirik Lund', '38000002', 'Resolved', '2025-03-03 09:15:00', 'test-user-02'),
(203, 'Brøyting av adkomstvei til legevakt', @snorydding, 'Acute', 1, 'stk', '2025-12-08 06:00:00', NULL, 58.156000, 7.985000, 'Kristiansand', 'Marte Svendsen', '38000001', 'Resolved', '2025-12-08 05:30:00', 'test-user-01'),
(204, 'Transport av evakuerte beboere', @transport, 'High', 2, 'stk', '2026-01-21 16:00:00', '2026-01-22 12:00:00', 58.471000, 8.762000, 'Arendal', 'Geir Aasen', '38000004', 'Assigned', '2026-01-21 14:10:00', 'test-user-04'),
(205, 'Drone til leteaksjon', @drone, 'Acute', 1, 'stk', '2026-02-05 20:00:00', '2026-02-06 08:00:00', 58.340000, 8.226000, 'Birkenes', 'Operasjonsleder Nina Holm', '38000003', 'Assigned', '2026-02-05 19:45:00', 'test-user-03'),
(206, 'Feltsenger til mottakssenter', @evakuering, 'Normal', 30, 'stk', '2026-03-20 08:00:00', '2026-04-01 16:00:00', 58.350000, 8.583000, 'Grimstad', 'Kaptein Eirik Lund', '38000002', 'UnderReview', '2026-03-17 10:00:00', 'test-user-02'),
(207, 'Varm mat til evakuerte', @mat, 'High', 150, 'porsjoner', '2026-04-09 17:00:00', NULL, 58.156000, 7.985000, 'Kristiansand', 'Marte Svendsen', '38000001', 'Assigned', '2026-04-09 11:20:00', 'test-user-01'),
(208, 'Vannforsyning etter rørbrudd', @vann, 'Acute', 1000, 'liter', '2026-06-02 10:00:00', '2026-06-05 10:00:00', 58.278000, 7.963000, 'Vennesla', 'Geir Aasen', '38000004', 'New', '2026-06-02 08:05:00', 'test-user-04'),
(209, 'Sambandsutstyr til øvelse', @samband, 'Planned', 10, 'stk', '2026-09-01 08:00:00', '2026-09-03 16:00:00', 58.596000, 7.786000, 'Evje og Hornnes', 'Kaptein Eirik Lund', '38000002', 'Assigned', '2026-08-14 13:00:00', 'test-user-02'),
(210, 'Gravemaskin etter jordras', @maskiner, 'Acute', 1, 'stk', '2026-09-23 17:00:00', NULL, 58.259000, 8.367000, 'Lillesand', 'Operasjonsleder Nina Holm', '38000003', 'Assigned', '2026-09-23 15:30:00', 'test-user-03'),
(211, 'Strøsand til glatte veier', @sand, 'High', 20, 'tonn', '2026-10-30 07:00:00', NULL, 58.156000, 7.985000, 'Kristiansand', 'Marte Svendsen', '38000001', 'New', '2026-10-30 06:15:00', 'test-user-01'),
(212, 'Aggregat til mottakssenter', @strom, 'High', 1, 'stk', '2026-11-05 08:00:00', '2026-11-12 16:00:00', 58.350000, 8.583000, 'Grimstad', 'Geir Aasen', '38000004', 'UnderReview', '2026-11-04 12:00:00', 'test-user-04'),
(213, 'Lokaler til HV-innsatsstyrke', @lokaler, 'Planned', 1, 'stk', '2026-12-01 08:00:00', '2026-12-05 16:00:00', 58.463000, 7.916000, 'Iveland', 'Kaptein Eirik Lund', '38000002', 'New', '2026-11-10 09:40:00', 'test-user-02'),
(214, 'Varmekilder til isolerte husstander', @varme, 'Acute', 3, 'stk', '2026-11-18 09:00:00', NULL, 58.340000, 8.226000, 'Birkenes', 'Marte Svendsen', '38000001', 'New', '2026-11-18 07:25:00', 'test-user-01'),
(215, 'Mannskap til søk etter savnet person', @mannskap, 'Acute', 8, 'personer', '2026-11-21 22:00:00', '2026-11-22 12:00:00', 58.471000, 8.762000, 'Arendal', 'Operasjonsleder Nina Holm', '38000003', 'New', '2026-11-21 21:10:00', 'test-user-03');

-- 6. Koblinger mellom behov og ressurser (14), alltid i samme kategori
INSERT INTO Matches (MatchId, NeedId, ResourceId, CreatedAt) VALUES
(301, 201, 127, '2025-01-12 07:40:00'),
(302, 201, 105, '2025-01-12 07:40:00'),
(303, 202, 129, '2025-03-03 09:35:00'),
(304, 202, 116, '2025-03-03 09:35:00'),
(305, 203, 154, '2025-12-08 05:50:00'),
(306, 203, 110, '2025-12-08 05:50:00'),
(307, 204, 146, '2026-01-21 14:30:00'),
(308, 204, 132, '2026-01-21 14:30:00'),
(309, 205, 151, '2026-02-05 20:45:00'),
(310, 205, 109, '2026-02-05 20:45:00'),
(311, 206, 136, '2026-03-17 10:20:00'),
(312, 206, 121, '2026-03-17 10:20:00'),
(313, 207, 157, '2026-04-09 11:40:00'),
(314, 207, 138, '2026-04-09 11:40:00');

-- 7. Forespørsler (14). Proposed = ikke besvart ennå, derfor RespondedAt = NULL
INSERT INTO Requests (RequestId, MatchId, Quantity, Status, CreatedAt, RespondedAt, ResponseMessage) VALUES
(401, 301, 2, 'Confirmed', '2025-01-12 08:40:00', '2025-01-12 10:40:00', 'Bekreftet. Vi stiller med ressursen.'),
(402, 302, 2, 'Confirmed', '2025-01-12 08:40:00', '2025-01-12 11:40:00', 'Bekreftet. Vi stiller med ressursen.'),
(403, 303, 15, 'Rejected', '2025-03-03 10:35:00', '2025-03-03 14:35:00', 'Beklager, ressursen er opptatt.'),
(404, 304, 15, 'Confirmed', '2025-03-03 10:35:00', '2025-03-03 15:35:00', 'Bekreftet. Vi stiller med ressursen.'),
(405, 305, 1, 'Proposed', '2025-12-08 06:50:00', NULL, NULL),
(406, 306, 1, 'Confirmed', '2025-12-08 06:50:00', '2025-12-08 08:50:00', 'Bekreftet. Vi stiller med ressursen.'),
(407, 307, 2, 'Confirmed', '2026-01-21 15:30:00', '2026-01-21 18:30:00', 'Bekreftet. Vi stiller med ressursen.'),
(408, 308, 2, 'Proposed', '2026-01-21 15:30:00', NULL, NULL),
(409, 309, 1, 'Confirmed', '2026-02-05 21:45:00', '2026-02-06 02:45:00', 'Bekreftet. Vi stiller med ressursen.'),
(410, 310, 1, 'Rejected', '2026-02-05 21:45:00', '2026-02-06 03:45:00', 'Beklager, ressursen er opptatt.'),
(411, 311, 30, 'Confirmed', '2026-03-17 11:20:00', '2026-03-17 13:20:00', 'Bekreftet. Vi stiller med ressursen.'),
(412, 312, 4, 'Proposed', '2026-03-17 11:20:00', NULL, NULL),
(413, 313, 150, 'Confirmed', '2026-04-09 12:40:00', '2026-04-09 16:40:00', 'Bekreftet. Vi stiller med ressursen.'),
(414, 314, 150, 'Proposed', '2026-04-09 12:40:00', NULL, NULL);

-- 8. Varsler (12)
INSERT INTO Notifications (NotificationId, UserId, Message, RequestId, NeedId, IsUrgent, IsRead, CreatedAt) VALUES
(501, 'test-user-01', 'Forespørsel bekreftet: Aggregat 5 kW', 401, 201, 0, 0, '2025-01-12 10:40:00'),
(502, 'test-user-01', 'Forespørsel bekreftet: Aggregat 20 kVA', 402, 201, 0, 1, '2025-01-12 11:40:00'),
(503, 'test-user-02', 'Forespørsel avslått: Mannskap til sandsekking', 403, 202, 0, 1, '2025-03-03 14:35:00'),
(504, 'test-user-02', 'Forespørsel bekreftet: Mannskap til sandsekking', 404, 202, 0, 0, '2025-03-03 15:35:00'),
(505, 'test-user-07', 'Ny forespørsel: Brøyting av adkomstvei til legevakt', 405, 203, 1, 0, '2025-12-08 06:50:00'),
(506, 'test-user-01', 'Forespørsel bekreftet: Traktor med snøfres', 406, 203, 0, 1, '2025-12-08 08:50:00'),
(507, 'test-user-04', 'Forespørsel bekreftet: Varebil', 407, 204, 0, 0, '2026-01-21 18:30:00'),
(508, 'test-user-06', 'Ny forespørsel: Transport av evakuerte beboere', 408, 204, 0, 0, '2026-01-21 15:30:00'),
(509, 'test-user-03', 'Forespørsel bekreftet: Drone med varmekamera', 409, 205, 0, 1, '2026-02-06 02:45:00'),
(510, 'test-user-03', 'Forespørsel avslått: Drone med varmekamera', 410, 205, 0, 0, '2026-02-06 03:45:00'),
(511, 'test-user-02', 'Forespørsel bekreftet: Feltsenger', 411, 206, 0, 1, '2026-03-17 13:20:00'),
(512, 'test-user-05', 'Ny forespørsel: Feltsenger til mottakssenter', 412, 206, 0, 0, '2026-03-17 11:20:00');

COMMIT;

-- 9. Kontroll: antall rader per tabell
SELECT 'AspNetUsers (test)' AS Tabell, COUNT(*) AS Antall FROM AspNetUsers WHERE Id LIKE 'test-user-%'
UNION ALL SELECT 'Resources', COUNT(*) FROM Resources
UNION ALL SELECT 'Needs', COUNT(*) FROM Needs
UNION ALL SELECT 'Matches', COUNT(*) FROM Matches
UNION ALL SELECT 'Requests', COUNT(*) FROM Requests
UNION ALL SELECT 'Notifications', COUNT(*) FROM Notifications
;
