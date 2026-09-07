using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Auth.Application.Services;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.Auth.Application.Usecases.Google;

// Cas d'usage utilisateur : terminer le callback Google et ouvrir une session Tools.
public sealed class CompleteGoogleOAuthLoginUseCase(
    IGoogleOAuthStateStore stateStore,
    IGoogleOAuthClient googleOAuthClient,
    IGoogleIdentityVerifier googleIdentityVerifier,
    GoogleIdentityAuthenticationService googleIdentityAuthenticationService,
    AuthSessionService authSessionService,
    AdminSignupNotifier adminSignupNotifier)
{
    public async Task<GoogleOAuthLoginResult> Execute(
        string code,
        string state)
    {
        var source = stateStore.Consume(state);
        var idToken = await googleOAuthClient.ExchangeCodeForIdTokenAsync(code);
        var googleIdentity = await googleIdentityVerifier.VerifyAsync(idToken);
        var authentication = await googleIdentityAuthenticationService.AuthenticateAsync(googleIdentity);

        // Google confirme l'adresse lui-même : il n'y a pas d'étape de confirmation à signaler
        // ensuite comme pour l'inscription classique. Le compte n'est pour autant actif d'office
        // que si `auth.adminApprovalRequired` n'est pas posé — un compte tout juste créé et
        // inactif est nécessairement en attente de validation.
        //
        // Notifié **avant** le refus ci-dessous : sans ça, personne n'apprendrait qu'un compte
        // attend, et la file d'attente resterait invisible.
        if (authentication.AccountCreated)
        {
            await adminSignupNotifier.GoogleAccountCreated(
                authentication.User.Email, pendingApproval: !authentication.User.IsActive);
        }

        // Le compte inactif est refusé ici plutôt que dans le service : la session ne doit pas
        // être créée, et le contrôle vaut aussi bien pour une inscription en attente que pour un
        // compte suspendu retrouvé par son provider.
        if (!authentication.User.IsActive)
        {
            throw AppException.Unauthorized("USER_DISABLED", "Utilisateur désactivé.");
        }

        var session = await authSessionService.Create(authentication.User, null);

        return new GoogleOAuthLoginResult(source, session);
    }
}

public sealed record GoogleOAuthLoginResult(string Source, AuthSession Session);
