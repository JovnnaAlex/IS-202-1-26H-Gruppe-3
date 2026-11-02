namespace Kriseportal.Web.Models.Map;

/// <summary>
/// Ett punkt på situasjonskartet. Brukes både for behov og ressurser,
/// og senere for simulerte behov i beslutningsstøtten.
/// </summary>
public record MapPoint(
    int Id,
    string Kind,          // "need" eller "resource"
    string Title,
    string Category,
    string? Priority,     // Bare for behov: Acute, High, Normal, Planned
    string Status,        // Status for behov, tilgjengelighet for ressurser
    int Quantity,
    string Unit,
    double Lat,
    double Lng,
    string Municipality,
    string ContactName,
    string ContactPhone,
    bool IsSimulated = false);