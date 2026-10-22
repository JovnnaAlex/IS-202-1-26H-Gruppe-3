-- Spørring 1: Ressursliste for en kommune (utskrift)
SELECT r.Name AS Ressurs,
       r.Category AS Kategori,
       r.Availability AS Status,
       u.Name AS Leverandør,
       u.Phone AS Telefon
FROM Resources r
JOIN Users u ON u.UserID = r.ProviderID
WHERE r.Municipality = 'Kristiansand'
ORDER BY r.Category, r.Name;

-- Spørring 2: Hvilke ressurser er matchet mot hvert behov
SELECT n.Title AS Behov,
       res.Name AS Ressurs,
       req.Status AS Forespørsel,
       req.RespondedAt AS Svart
FROM Needs n
JOIN Matches m    ON m.NeedID = n.NeedID
JOIN Resources res ON res.ResourceID = m.ResourceID
JOIN Requests req ON req.MatchID = m.MatchID
ORDER BY n.NeedID;

-- Spørring 3: Hvilke behov er dekket (grunnlag for status Assigned, vedtak 7)
SELECT n.Title, n.Quantity AS Trenger, COUNT(req.RequestID) AS Bekreftet
FROM Needs n
JOIN Matches m    ON m.NeedID = n.NeedID
JOIN Requests req ON req.MatchID = m.MatchID AND req.Status = 'Confirmed'
GROUP BY n.NeedID, n.Title, n.Quantity;D

UPDATE Requests SET Status = 'Confirmed', RespondedAt = NOW() WHERE MatchID = 2;
