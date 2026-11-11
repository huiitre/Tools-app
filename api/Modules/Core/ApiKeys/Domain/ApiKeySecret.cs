using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Tools.Api.Modules.Core.ApiKeys.Domain;

// Clé en clair, connue seulement à la création : seuls son hash et son préfixe sont stockés.
public sealed record ApiKeySecret(string Value, string Prefix, byte[] Hash)
{
    // Repérable dans un log ou un commit par les outils de détection de secrets.
    public const string Marker = "tk_";
    private const int RandomBytes = 32;
    private const int VisiblePrefixLength = 12;

    public static ApiKeySecret Generate()
    {
        var value = Marker + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RandomBytes));
        return new ApiKeySecret(value, value[..VisiblePrefixLength], HashOf(value));
    }

    // Même calcul à la création et à chaque appel : c'est ce hash qu'on cherche en base.
    public static byte[] HashOf(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    // Le ToString généré d'un record afficherait la clé en clair dans un log.
    public override string ToString() => $"{Prefix}…";
}
