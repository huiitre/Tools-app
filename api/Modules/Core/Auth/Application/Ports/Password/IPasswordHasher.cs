namespace Tools.Api.Modules.Core.Auth.Application.Ports.Password;

// Port de hachage de mot de passe : l'Application ne dépend pas directement de BCrypt.
public interface IPasswordHasher
{
    string Hash(string password);

    bool Matches(string password, string hash);
}
