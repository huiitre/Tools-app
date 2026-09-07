using Tools.Api.Modules.Core.Notifications.Application;
using Tools.Api.Modules.Core.Notifications.Application.Services;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Auth.Application.Services;

// Prévient les administrateurs des arrivées de comptes.
//
// Les trois flux d'inscription — mot de passe, confirmation d'adresse, Google — signalent la
// même chose à la même population ; les réunir ici évite d'écrire trois fois le même message
// et la même protection.
//
// Une notification qui échoue ne doit jamais faire échouer l'inscription : le compte est déjà
// créé et l'email de confirmation parti quand on arrive ici. L'erreur est donc journalisée puis
// absorbée — c'est une information pour les administrateurs, pas une étape du flux.
public sealed class AdminSignupNotifier(
    NotificationService notificationService,
    ILogger<AdminSignupNotifier> logger)
{
    // Ouvre le panneau d'administration des utilisateurs au clic, comme le fait déjà la
    // notification de feedback de l'API Java avec sa propre route.
    private const string AdminUsersRoute = """{"route":"admin-users"}""";

    public Task AccountCreated(string email) => Notify(
        "Nouvelle inscription",
        $"{email} vient de créer un compte. L'adresse n'est pas encore confirmée.");

    // `pendingApproval` n'est pas une nuance de rédaction : quand `auth.adminApprovalRequired`
    // est posé, le compte reste inactif et **quelqu'un doit agir**. Une notification qui
    // annoncerait « le compte est actif » serait fausse, et personne ne saurait qu'il y a une
    // file d'attente à traiter.
    public Task EmailVerified(string email, bool pendingApproval) => pendingApproval
        ? Notify(
            "Inscription à valider",
            $"{email} a confirmé son adresse email. Le compte reste inactif tant qu'un administrateur ne l'a pas activé.")
        : Notify(
            "Inscription confirmée",
            $"{email} a confirmé son adresse email : le compte est actif.");

    public Task GoogleAccountCreated(string email, bool pendingApproval) => pendingApproval
        ? Notify(
            "Inscription Google à valider",
            $"{email} vient de créer un compte avec Google. Le compte reste inactif tant qu'un administrateur ne l'a pas activé.")
        : Notify(
            "Nouvelle inscription via Google",
            $"{email} vient de créer un compte avec Google.");

    private async Task Notify(string title, string body)
    {
        try
        {
            await notificationService.Send(SendNotificationCommand.ForMinRole(
                RoleCode.Admin,
                title,
                body,
                NotificationType.Info,
                AdminUsersRoute));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Notification d'inscription non enregistrée : {Title}", title);
        }
    }
}
