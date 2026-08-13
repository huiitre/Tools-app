// Cas d'usage utilisateur : démarrer une connexion Google dans le navigateur ou Electron.
public sealed class GetGoogleAuthorizationUrlUseCase(
    IGoogleOAuthStateStore stateStore,
    IGoogleOAuthClient googleOAuthClient)
{
    public string Execute(string source)
    {
        var state = stateStore.Create(source);
        return googleOAuthClient.BuildAuthorizationUrl(state);
    }
}
