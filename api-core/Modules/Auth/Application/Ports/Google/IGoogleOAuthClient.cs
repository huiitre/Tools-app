namespace Tools.ApiCore.Modules.Auth.Application.Ports.Google;

// Client technique de l'autorisation OAuth Google et de l'échange code -> ID token.
public interface IGoogleOAuthClient
{
    string BuildAuthorizationUrl(string state);
    Task<string> ExchangeCodeForIdTokenAsync(string code);
}
