namespace Kriseportal.Web.Services;

public class LogRequestNotificationSender(
    ILogger<LogRequestNotificationSender> logger)
    : IRequestNotificationSender
{
    public Task SendAsync(string userId, string message)
    {
        // TODO: Implementer e-postutsending gjennom en e-posttjeneste.
        // Hent mottakerens e-postadresse fra brukerkontoen.
        //
        // TODO: Implementer mobilpush med brukerens tillatelse
        // og registrerte push-abonnementer.
        //
        // Denne utviklingsversjonen sender ingen e-post eller mobilpush.
        // Ved ekte utsending bør en bakgrunnskø/outbox håndtere levering
        // og nye forsøk uten å blokkere eller gjenta databasehandlingen.

        logger.LogInformation(
            "Eksternt varsel er ikke sendt. Hendelsen er registrert for bruker {UserId}.",
            userId);

        return Task.CompletedTask;
    }
}