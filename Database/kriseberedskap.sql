
CREATE DATABASE IF NOT EXISTS kriseberedskap
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
 
USE kriseberedskap;
 

DROP TABLE IF EXISTS Requests;
DROP TABLE IF EXISTS Matches;
DROP TABLE IF EXISTS Resources;
DROP TABLE IF EXISTS Needs;
DROP TABLE IF EXISTS Users;

CREATE TABLE Users (
    UserID        INT UNSIGNED  NOT NULL AUTO_INCREMENT,
    Name          VARCHAR(100)  NOT NULL,
    Email         VARCHAR(255)  NOT NULL,
    Phone         VARCHAR(20)   NOT NULL,
    Role          ENUM('PublicAuthority', 'ResourceProvider') NOT NULL,
    Agency        ENUM('Stat', 'Kommune', 'Politi', 'Brann', 'Helse',
                       'Sivilforsvaret', 'Forsvaret', 'Heimevernet') NULL,
    ProviderType  ENUM('Privatperson', 'Bedrift', 'Bonde', 'Entreprenør',
                       'FrivilligOrganisasjon', 'Droneoperatør') NULL,
    CreatedAt     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
 
    PRIMARY KEY (UserID),
    UNIQUE KEY uq_users_email (Email),
    CONSTRAINT chk_users_role CHECK (
        (Role = 'PublicAuthority'  AND Agency IS NOT NULL AND ProviderType IS NULL)
     OR (Role = 'ResourceProvider' AND Agency IS NULL     AND ProviderType IS NOT NULL)
    )
) ENGINE = InnoDB;

CREATE TABLE Needs (
    NeedID             INT UNSIGNED   NOT NULL AUTO_INCREMENT,
    PublicAuthorityID  INT UNSIGNED   NOT NULL,
    Title              VARCHAR(150)   NOT NULL,
    Description        TEXT           NULL,
    Category           ENUM('Transport', 'Drone', 'Strøm', 'Snørydding', 'SandGrus',
                            'Maskiner', 'Evakuering', 'Samband', 'Mannskap',
                            'Lokaler', 'Båt', 'Annet') NOT NULL,
    Priority           ENUM('Akutt', 'Høy', 'Normal', 'Planlagt') NOT NULL DEFAULT 'Normal',
    Quantity           INT UNSIGNED   NOT NULL DEFAULT 1,
    NeededFrom         DATETIME       NOT NULL,
    NeededTo           DATETIME       NULL,
    Latitude           DECIMAL(9,6)   NOT NULL,
    Longitude          DECIMAL(9,6)   NOT NULL,
    Municipality       VARCHAR(100)   NOT NULL,
    Status             ENUM('New', 'UnderReview', 'Assigned', 'Resolved') NOT NULL DEFAULT 'New',
    CreatedAt          DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (NeedID),
    KEY idx_needs_status (Status),
    KEY idx_needs_municipality (Municipality),
    CONSTRAINT fk_needs_user FOREIGN KEY (PublicAuthorityID)
        REFERENCES Users (UserID) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT chk_needs_quantity CHECK (Quantity >= 1),
    CONSTRAINT chk_needs_period   CHECK (NeededTo IS NULL OR NeededTo >= NeededFrom),
    CONSTRAINT chk_needs_lat      CHECK (Latitude  BETWEEN -90  AND 90),
    CONSTRAINT chk_needs_lng      CHECK (Longitude BETWEEN -180 AND 180)
) ENGINE = InnoDB;

CREATE TABLE Resources (
    ResourceID     INT UNSIGNED   NOT NULL AUTO_INCREMENT,
    ProviderID     INT UNSIGNED   NOT NULL,
    Name           VARCHAR(150)   NOT NULL,
    Category       ENUM('Transport', 'Drone', 'Strøm', 'Snørydding', 'SandGrus',
                        'Maskiner', 'Evakuering', 'Samband', 'Mannskap',
                        'Lokaler', 'Båt', 'Annet') NOT NULL,
    Description    TEXT           NULL,
    Availability   ENUM('Available', 'Reserved', 'Unavailable') NOT NULL DEFAULT 'Available',
    AvailableFrom  DATETIME       NULL,
    AvailableTo    DATETIME       NULL,
    Latitude       DECIMAL(9,6)   NOT NULL,
    Longitude      DECIMAL(9,6)   NOT NULL,
    Municipality   VARCHAR(100)   NOT NULL,
    CreatedAt      DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (ResourceID),
    KEY idx_resources_category (Category),
    KEY idx_resources_municipality (Municipality),
    CONSTRAINT fk_resources_user FOREIGN KEY (ProviderID)
        REFERENCES Users (UserID) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT chk_resources_period CHECK (
        AvailableFrom IS NULL OR AvailableTo IS NULL OR AvailableTo >= AvailableFrom),
    CONSTRAINT chk_resources_lat CHECK (Latitude  BETWEEN -90  AND 90),
    CONSTRAINT chk_resources_lng CHECK (Longitude BETWEEN -180 AND 180)
) ENGINE = InnoDB;

CREATE TABLE Matches (
    MatchID     INT UNSIGNED NOT NULL AUTO_INCREMENT,
    NeedID      INT UNSIGNED NOT NULL,
    ResourceID  INT UNSIGNED NOT NULL,
    CreatedAt   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (MatchID),
    UNIQUE KEY uq_matches_need_resource (NeedID, ResourceID),
    KEY idx_matches_resource (ResourceID),
    CONSTRAINT fk_matches_need FOREIGN KEY (NeedID)
        REFERENCES Needs (NeedID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_matches_resource FOREIGN KEY (ResourceID)
        REFERENCES Resources (ResourceID) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE = InnoDB;

CREATE TABLE Requests (
    RequestID    INT UNSIGNED NOT NULL AUTO_INCREMENT,
    MatchID      INT UNSIGNED NOT NULL,
    Status       ENUM('Proposed', 'Confirmed', 'Rejected') NOT NULL DEFAULT 'Proposed',
    CreatedAt    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    RespondedAt  DATETIME     NULL,

    PRIMARY KEY (RequestID),
    KEY idx_requests_match_status (MatchID, Status),
    CONSTRAINT fk_requests_match FOREIGN KEY (MatchID)
        REFERENCES Matches (MatchID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_requests_responded CHECK (
        (Status = 'Proposed' AND RespondedAt IS NULL)
     OR (Status <> 'Proposed' AND RespondedAt IS NOT NULL)
    )
) ENGINE = InnoDB;

INSERT INTO Users (Name, Email, Phone, Role, Agency, ProviderType) VALUES
  ('Kristiansand kommune beredskap', 'beredskap@kristiansand.example', '38000000', 'PublicAuthority', 'Kommune', NULL),
  ('HV-07 Agder',                    'hv07@hv.example',                '38000001', 'PublicAuthority', 'Heimevernet', NULL),
  ('Ola Bonde',                      'ola@gard.example',               '90000001', 'ResourceProvider', NULL, 'Bonde'),
  ('Sør Graving AS',                 'post@sorgraving.example',        '90000002', 'ResourceProvider', NULL, 'Entreprenør'),
  ('Kari Drone',                     'kari@drone.example',             '90000003', 'ResourceProvider', NULL, 'Droneoperatør');

INSERT INTO Needs (PublicAuthorityID, Title, Description, Category, Priority, Quantity,
                   NeededFrom, NeededTo, Latitude, Longitude, Municipality, Status) VALUES
  (1, 'Aggregat til sykehjem', 'Strømbrudd, trenger to aggregater', 'Strøm', 'Akutt', 2,
      '2026-11-01 08:00', '2026-11-03 20:00', 58.146700, 7.995600, 'Kristiansand', 'New'),
  (2, 'Droneobservasjon av flomområde', NULL, 'Drone', 'Høy', 1,
      '2026-11-01 10:00', NULL, 58.250000, 7.900000, 'Vennesla', 'UnderReview');

INSERT INTO Resources (ProviderID, Name, Category, Description, Availability,
                       AvailableFrom, AvailableTo, Latitude, Longitude, Municipality) VALUES
  (3, 'Traktoraggregat 40 kVA', 'Strøm', 'Kraftuttaksdrevet aggregat', 'Available', NULL, NULL, 58.180000, 8.050000, 'Kristiansand'),
  (4, 'Dieselaggregat 100 kVA', 'Strøm', NULL, 'Available', '2026-10-01', '2026-12-31', 58.160000, 8.000000, 'Kristiansand'),
  (5, 'DJI Matrice med termisk kamera', 'Drone', 'Operatør med A2-sertifikat', 'Available', NULL, NULL, 58.240000, 7.880000, 'Vennesla');

INSERT INTO Matches (NeedID, ResourceID) VALUES (1, 1), (1, 2), (2, 3);

INSERT INTO Requests (MatchID, Status, RespondedAt) VALUES
  (1, 'Confirmed', '2026-11-01 08:20'),
  (2, 'Proposed',  NULL),
  (3, 'Proposed',  NULL);

