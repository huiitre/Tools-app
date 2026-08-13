// Port de vérification de mot de passe : l'Application ne dépend pas directement de BCrypt.
public interface IPasswordHasher
{
    bool Matches(string password, string hash);
}
