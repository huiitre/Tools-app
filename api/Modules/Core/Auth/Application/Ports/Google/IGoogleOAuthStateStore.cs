namespace Tools.Api.Modules.Core.Auth.Application.Ports.Google;

// Stocke temporairement l'état OAuth afin de lier un callback à l'ouverture initiale du navigateur.
public interface IGoogleOAuthStateStore
{
    string Create(GoogleOAuthOrigin origin);
    GoogleOAuthOrigin Consume(string state);
}
