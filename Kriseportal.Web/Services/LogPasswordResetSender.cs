namespace Kriseportal.Web.Services;

// Glemt passord: utviklingsversjon som skriver lenken til loggen (terminalen)
// i stedet for å sende e-post. Prosjektet har ingen e-posttjeneste ennå.
// Må byttes ut med en ekte e-posttjeneste før appen settes i drift.
public class LogPasswordResetSender(ILogger<LogPasswordResetSender> logger) : IPasswordResetSender
{
    public Task SendResetLinkAsync(string email, string resetLink)
    {
        logger.LogInformation(
            "Lenke for nytt passord til {Email}: {ResetLink}",
            email,
            resetLink);

        return Task.CompletedTask;
    }
}
