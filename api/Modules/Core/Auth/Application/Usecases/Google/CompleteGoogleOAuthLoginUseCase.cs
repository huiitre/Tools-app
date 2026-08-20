using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Auth.Application.Services;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;

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
        var session = await authSessionService.Create(authentication.User, null);

        // Google confirme l'adresse lui-même : le compte est actif dès sa création, il n'y a
        // pas d'étape de confirmation à signaler ensuite comme pour l'inscription classique.
        if (authentication.AccountCreated)
        {
            await adminSignupNotifier.GoogleAccountCreated(authentication.User.Email);
        }

        return new GoogleOAuthLoginResult(source, session);
    }
}

public sealed record GoogleOAuthLoginResult(string Source, AuthSession Session);
