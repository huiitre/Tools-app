// Adaptateur technique BCrypt du port IPasswordHasher.
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public bool Matches(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
