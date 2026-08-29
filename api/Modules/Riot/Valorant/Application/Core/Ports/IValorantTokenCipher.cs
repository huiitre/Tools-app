namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

// Chiffrement du refresh token stocké en base (AES-GCM côté Java, colonnes `encrypted_refresh` et
// `encryption_iv`). L'API C# n'a pas encore de service de chiffrement transverse : le besoin est
// déclaré ici, et remontera dans le Core le jour où un deuxième module en aura un.
public interface IValorantTokenCipher
{
    string GenerateIv();
    string Encrypt(string plainText, string iv);
    string Decrypt(string cipherText, string iv);
}
