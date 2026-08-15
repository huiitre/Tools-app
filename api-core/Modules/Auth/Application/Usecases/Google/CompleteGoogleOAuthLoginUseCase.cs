using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Services;
using Tools.ApiCore.Modules.Auth.Application.Ports.Google;

namespace Tools.ApiCore.Modules.Auth.Application.Usecases.Google;

// Cas d'usage utilisateur : terminer le callback Google et ouvrir une session Tools.
public sealed class CompleteGoogleOAuthLoginUseCase(
    IGoogleOAuthStateStore stateStore,
    IGoogleOAuthClient googleOAuthClient,
    IGoogleIdentityVerifier googleIdentityVerifier,
    GoogleIdentityAuthenticationService googleIdentityAuthenticationService,
    AuthSessionService authSessionService)
{
    public async Task<GoogleOAuthLoginResult> Execute(
        string code,
        string state)
    {
        var source = stateStore.Consume(state);
        var idToken = await googleOAuthClient.ExchangeCodeForIdTokenAsync(code);
        var googleIdentity = await googleIdentityVerifier.VerifyAsync(idToken);
        var user = await googleIdentityAuthenticationService.AuthenticateAsync(googleIdentity);
        var session = await authSessionService.Create(user, null);
        return new GoogleOAuthLoginResult(source, session);
    }
}

public sealed record GoogleOAuthLoginResult(string Source, AuthSession Session);
