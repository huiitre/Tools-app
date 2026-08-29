using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

// AES-256-GCM, compatible octet pour octet avec l'API Java.
//
// **Le point délicat est le tag d'authentification.** En Java, `Cipher.doFinal` le colle à la fin
// du chiffré ; en .NET, AesGcm veut deux tampons séparés. Il faut donc le détacher au
// déchiffrement et le recoller au chiffrement, sinon aucun compte déjà lié n'est relisible.
public sealed class AesGcmValorantTokenCipher : IValorantTokenCipher
{
    private const int IvLengthInBytes = 12;
    private const int TagLengthInBytes = 16;

    private readonly byte[] masterKey;

    public AesGcmValorantTokenCipher(IOptions<RiotOptions> options)
    {
        var configuredKey = options.Value.EncryptionMasterKey;

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException(
                "Riot:EncryptionMasterKey n'est pas configurée : les jetons Valorant ne peuvent être ni lus ni écrits.");
        }

        masterKey = Convert.FromBase64String(configuredKey);
    }

    public string GenerateIv()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(IvLengthInBytes));
    }

    public string Encrypt(string plainText, string iv)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagLengthInBytes];

        using var aes = new AesGcm(masterKey, TagLengthInBytes);
        aes.Encrypt(Convert.FromBase64String(iv), plainBytes, cipherBytes, tag);

        // Chiffré puis tag, dans cet ordre : c'est ce que produit Java.
        return Convert.ToBase64String([.. cipherBytes, .. tag]);
    }

    public string Decrypt(string cipherText, string iv)
    {
        var payload = Convert.FromBase64String(cipherText);

        if (payload.Length < TagLengthInBytes)
        {
            throw new CryptographicException("Le chiffré Valorant est trop court pour porter son tag d'authentification.");
        }

        var cipherBytes = payload.AsSpan(0, payload.Length - TagLengthInBytes);
        var tag = payload.AsSpan(payload.Length - TagLengthInBytes);
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(masterKey, TagLengthInBytes);
        aes.Decrypt(Convert.FromBase64String(iv), cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
