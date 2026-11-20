using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Huihuistore.Application.Usecases;
using Tools.Api.Modules.Core.Huihuistore.Domain;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.Huihuistore;

public sealed class HuihuistorePublishRulesTests
{
    private const string Package = "fr.huiitre.huihuistore";
    private static readonly byte[] Cert = Enumerable.Repeat((byte)0xAB, 32).ToArray();
    private static readonly string CertHex = Convert.ToHexStringLower(Cert);

    private static ApkInfo Apk(long versionCode, string package = Package, byte[]? cert = null) => new(
        package, versionCode, "1.0.0", 26, "huihuistore", 1000, new byte[32], cert ?? Cert, "CN=huiitre", ["v2"]);

    private static HuihuistoreRelease Current(long versionCode) => new(
        Package, versionCode, "1.0.0", 26, $"{versionCode}.apk", 1000, "", CertHex, null, DateTimeOffset.UtcNow);

    private static string CodeOf(Action action) => Assert.Throws<AppException>(action).Code;

    [Fact]
    public void Une_premiere_publication_conforme_passe()
    {
        HuihuistorePublishRules.Validate(Apk(1), Package, CertHex, null);
    }

    [Fact]
    public void L_empreinte_attendue_se_compare_sans_tenir_compte_de_la_casse()
    {
        HuihuistorePublishRules.Validate(Apk(43), Package, CertHex.ToUpperInvariant(), Current(42));
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData("fr.huiitre.huihuistore", "")]
    public void Sans_paquet_ou_certificat_configure_rien_n_est_publie(string package, string cert)
    {
        Assert.Equal("HUIHUISTORE_NOT_CONFIGURED", CodeOf(() => HuihuistorePublishRules.Validate(Apk(1), package, cert, null)));
    }

    [Fact]
    public void L_apk_d_un_autre_environnement_est_refuse()
    {
        Assert.Equal(
            "HUIHUISTORE_PACKAGE_MISMATCH",
            CodeOf(() => HuihuistorePublishRules.Validate(Apk(1, "fr.huiitre.huihuistore.qa"), Package, CertHex, null)));
    }

    [Fact]
    public void Un_apk_signe_par_une_autre_cle_est_refuse()
    {
        var otherCert = Enumerable.Repeat((byte)0xCD, 32).ToArray();

        Assert.Equal(
            "HUIHUISTORE_CERTIFICATE_MISMATCH",
            CodeOf(() => HuihuistorePublishRules.Validate(Apk(1, cert: otherCert), Package, CertHex, null)));
    }

    [Theory]
    [InlineData(42)]
    [InlineData(41)]
    public void Une_version_qui_n_est_pas_plus_recente_que_la_publiee_est_refusee(long versionCode)
    {
        Assert.Equal(
            "HUIHUISTORE_VERSION_NOT_NEWER",
            CodeOf(() => HuihuistorePublishRules.Validate(Apk(versionCode), Package, CertHex, Current(42))));
    }
}
