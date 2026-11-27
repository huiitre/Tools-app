using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.Auth.Application.Usecases.Google;

// Cas d'usage utilisateur : démarrer une connexion Google dans le navigateur ou Electron.
public sealed class GetGoogleAuthorizationUrlUseCase(
    IGoogleOAuthStateStore stateStore,
    IGoogleOAuthClient googleOAuthClient)
{
    public string Execute(string source, int? loopbackPort = null)
    {
        if (source is not ("web" or "electron"))
        {
            throw AppException.Validation("GOOGLE_SOURCE_INVALID", "Source Google invalide.");
        }

        // Port attribué par le système à Electron : jamais un port privilégié, et seulement pour Electron.
        if (loopbackPort is not null && (source != "electron" || loopbackPort is < 1024 or > 65535))
        {
            throw AppException.Validation("GOOGLE_LOOPBACK_PORT_INVALID", "Port de retour invalide.");
        }

        var state = stateStore.Create(new GoogleOAuthOrigin(source, loopbackPort));
        return googleOAuthClient.BuildAuthorizationUrl(state);
    }
}
