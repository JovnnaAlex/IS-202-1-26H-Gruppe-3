namespace Kriseportal.Web.Services;

// Glemt passord: sender lenken for å lage nytt passord til brukeren.
// Grensesnittet gjør at utviklingsversjonen (logg) kan byttes ut med ekte e-post
// uten å endre AccountController.
public interface IPasswordResetSender
{
    Task SendResetLinkAsync(string email, string resetLink);
}
