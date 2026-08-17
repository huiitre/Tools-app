namespace Tools.ApiCore.Modules.Auth.Application.Ports.Google;

// Vérifie un ID token émis par Google et en extrait une identité fiable.
public interface IGoogleIdentityVerifier
{
    Task<GoogleIdentity> VerifyAsync(string idToken);
}
