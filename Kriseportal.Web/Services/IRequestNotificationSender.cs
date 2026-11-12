namespace Kriseportal.Web.Services;

// Utvidelsespunkt for e-post og mobilpush ved forespørsler og svar.
// Portalvarsler håndteres separat i Notifications-tabellen.
public interface IRequestNotificationSender
{
    Task SendAsync(string userId, string message);
}