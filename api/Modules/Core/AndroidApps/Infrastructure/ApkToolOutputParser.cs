using System.Globalization;
using System.Text.RegularExpressions;

namespace Tools.Api.Modules.Core.AndroidApps.Infrastructure;

// Lecture des sorties texte d'aapt2 et d'apksigner, séparée de leur exécution pour se tester sans
// SDK Android. Null quand une information obligatoire manque : l'appelant décide de l'erreur.
public static partial class ApkToolOutputParser
{
    public sealed record Badging(string PackageName, long VersionCode, string? VersionName, int MinSdk, string? Label);

    public sealed record Signature(byte[] CertSha256, string CertSubject, IReadOnlyList<string> Schemes);

    // `aapt2 dump badging` :
    //   package: name='app.zeniptv.mobile' versionCode='148' versionName='2.1.0' …
    //   sdkVersion:'24'
    //   application-label:'Zen Player'
    public static Badging? ParseBadging(string output)
    {
        var packageLine = Lines(output).FirstOrDefault(line => line.StartsWith("package:", StringComparison.Ordinal));

        if (packageLine is null)
        {
            return null;
        }

        var packageName = Attribute(packageLine, "name");

        if (string.IsNullOrEmpty(packageName)
            || !long.TryParse(Attribute(packageLine, "versionCode"), NumberStyles.None, CultureInfo.InvariantCulture, out var versionCode))
        {
            return null;
        }

        // Sans minSdkVersion déclaré, Android considère 1.
        var minSdk = 1;
        var sdkLine = Lines(output).FirstOrDefault(line => line.StartsWith("sdkVersion:", StringComparison.Ordinal));

        if (sdkLine is not null && !int.TryParse(QuotedValue(sdkLine), NumberStyles.None, CultureInfo.InvariantCulture, out minSdk))
        {
            return null;
        }

        // Le libellé par défaut, sinon celui de la ligne « application: ». Il peut ne pas exister.
        var label = Lines(output)
            .Where(line => line.StartsWith("application-label:", StringComparison.Ordinal))
            .Select(QuotedValue)
            .FirstOrDefault()
            ?? Lines(output)
                .Where(line => line.StartsWith("application:", StringComparison.Ordinal))
                .Select(line => Attribute(line, "label"))
                .FirstOrDefault();

        return new Badging(
            packageName,
            versionCode,
            NullIfEmpty(Attribute(packageLine, "versionName")),
            minSdk,
            NullIfEmpty(label));
    }

    // `apksigner verify --print-certs -v` :
    //   Verified using v2 scheme (APK Signature Scheme v2): true
    //   Number of signers: 1
    //   Signer #1 certificate DN: CN=…
    //   Signer #1 certificate SHA-256 digest: 009d14c9…
    //
    // Un seul signataire est accepté : la TV compare un certificat, pas une liste. Un APK à
    // plusieurs signataires est rarissime et renvoyé comme illisible.
    public static Signature? ParseSignature(string output)
    {
        var lines = Lines(output).ToList();

        var signers = lines
            .Select(line => SignerCountRegex().Match(line))
            .FirstOrDefault(match => match.Success);

        if (signers is null || signers.Groups[1].Value != "1")
        {
            return null;
        }

        var digest = lines
            .Select(line => CertificateDigestRegex().Match(line))
            .FirstOrDefault(match => match.Success);

        if (digest is null)
        {
            return null;
        }

        var subject = lines
            .Select(line => CertificateSubjectRegex().Match(line))
            .FirstOrDefault(match => match.Success)?.Groups[1].Value.Trim() ?? "";

        var schemes = lines
            .Select(line => SchemeRegex().Match(line))
            .Where(match => match.Success && match.Groups[2].Value == "true")
            .Select(match => match.Groups[1].Value)
            .ToList();

        return new Signature(Convert.FromHexString(digest.Groups[1].Value), subject, schemes);
    }

    private static IEnumerable<string> Lines(string output) =>
        output.Split('\n').Select(line => line.TrimEnd('\r'));

    private static string? Attribute(string line, string name)
    {
        var match = Regex.Match(line, $@"(?:^|\s){Regex.Escape(name)}='([^']*)'");

        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? QuotedValue(string line)
    {
        var match = Regex.Match(line, "'([^']*)'");

        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    [GeneratedRegex(@"^Number of signers:\s*(\d+)")]
    private static partial Regex SignerCountRegex();

    [GeneratedRegex(@"^Signer #1 certificate SHA-256 digest:\s*([0-9a-fA-F]{64})\s*$")]
    private static partial Regex CertificateDigestRegex();

    [GeneratedRegex(@"^Signer #1 certificate DN:\s*(.*)$")]
    private static partial Regex CertificateSubjectRegex();

    [GeneratedRegex(@"^Verified using (v[\d.]+) scheme \([^)]*\):\s*(true|false)")]
    private static partial Regex SchemeRegex();
}
