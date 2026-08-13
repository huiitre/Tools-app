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
        string state,
        CancellationToken cancellationToken)
    {
        var source = stateStore.Consume(state);
        var idToken = await googleOAuthClient.ExchangeCodeForIdTokenAsync(code, cancellationToken);
        var googleIdentity = await googleIdentityVerifier.VerifyAsync(idToken, cancellationToken);
        var user = await googleIdentityAuthenticationService.AuthenticateAsync(googleIdentity, cancellationToken);
        var session = await authSessionService.Create(user, null, cancellationToken);
        return new GoogleOAuthLoginResult(source, session);
    }
}

public sealed record GoogleOAuthLoginResult(string Source, AuthSession Session);
