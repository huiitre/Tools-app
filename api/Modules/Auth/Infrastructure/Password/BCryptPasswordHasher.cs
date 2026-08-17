using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Password;

// Adaptateur technique BCrypt du port IPasswordHasher.
// Format compatible avec les hash produits par l'API Java (Spring BCryptPasswordEncoder).
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Matches(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
