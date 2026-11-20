using Tools.Api.Modules.Core.AndroidApps.Infrastructure;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.AndroidApps;

// Sorties réelles d'aapt2 2.19 et d'apksigner 0.9 (build-tools 33.0.2) sur l'APK de Zen Player,
// réduites aux lignes utiles.
public sealed class ApkToolOutputParserTests
{
    private const string ZenBadging = """
        package: name='app.zeniptv.mobile' versionCode='148' versionName='2.1.0' platformBuildVersionName='16' platformBuildVersionCode='36' compileSdkVersion='36' compileSdkVersionCodename='16'
        sdkVersion:'24'
        targetSdkVersion:'36'
        application-label:'Zen Player'
        application: label='Zen Player' icon='res/BW.xml' banner='res/Ec.png'
        leanback-launchable-activity: name='app.zeniptv.mobile.MainActivity'  label='' icon='' banner=''
        native-code: 'arm64-v8a' 'armeabi-v7a' 'x86_64'
        """;

    private const string ZenSignature = """
        Verifies
        Verified using v1 scheme (JAR signing): false
        Verified using v2 scheme (APK Signature Scheme v2): true
        Verified using v3 scheme (APK Signature Scheme v3): false
        Verified using v3.1 scheme (APK Signature Scheme v3.1): false
        Verified using v4 scheme (APK Signature Scheme v4): false
        Verified for SourceStamp: false
        Number of signers: 1
        Signer #1 certificate DN: CN=Romain Guyonvarch, OU=BeBest, O=BeBest, L=France, ST=Unknown, C=Unknown
        Signer #1 certificate SHA-256 digest: 009d14c9a62db979846d705575c19d684defc85d8af4e1faa55470488f4a9a04
        Signer #1 certificate SHA-1 digest: 3c5164dcce910c336ae20e6067a776aca7903943
        Signer #1 public key SHA-256 digest: eddd663be29b19b39eb303ef550b59959d9f1f471242764ac7f9c1046c995a12
        """;

    [Fact]
    public void Lit_paquet_versions_minSdk_et_libelle()
    {
        var badging = ApkToolOutputParser.ParseBadging(ZenBadging);

        Assert.NotNull(badging);
        Assert.Equal("app.zeniptv.mobile", badging.PackageName);
        Assert.Equal(148, badging.VersionCode);
        Assert.Equal("2.1.0", badging.VersionName);
        Assert.Equal(24, badging.MinSdk);
        Assert.Equal("Zen Player", badging.Label);
    }

    [Fact]
    public void Ne_confond_pas_name_avec_versionName_ni_platformBuildVersionName()
    {
        // `name=` apparaît aussi à la fin de `versionName=` : seul l'attribut isolé compte.
        var badging = ApkToolOutputParser.ParseBadging(
            "package: versionName='9.9' name='com.exemple.app' versionCode='3'");

        Assert.Equal("com.exemple.app", badging!.PackageName);
        Assert.Equal("9.9", badging.VersionName);
    }

    [Fact]
    public void Sans_sdkVersion_le_minSdk_vaut_1_et_sans_libelle_il_est_absent()
    {
        var badging = ApkToolOutputParser.ParseBadging("package: name='com.exemple.app' versionCode='3' versionName=''");

        Assert.Equal(1, badging!.MinSdk);
        Assert.Null(badging.VersionName);
        Assert.Null(badging.Label);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ERROR: dump failed because no AndroidManifest.xml found")]
    [InlineData("package: name='com.exemple.app' versionName='1.0'")]
    public void Une_sortie_sans_paquet_ou_sans_versionCode_est_refusee(string output)
    {
        Assert.Null(ApkToolOutputParser.ParseBadging(output));
    }

    [Fact]
    public void Lit_le_certificat_et_les_schemas_de_signature()
    {
        var signature = ApkToolOutputParser.ParseSignature(ZenSignature);

        Assert.NotNull(signature);
        Assert.Equal(
            "009d14c9a62db979846d705575c19d684defc85d8af4e1faa55470488f4a9a04",
            Convert.ToHexStringLower(signature.CertSha256));
        Assert.StartsWith("CN=Romain Guyonvarch", signature.CertSubject);
        Assert.Equal(["v2"], signature.Schemes);
    }

    [Fact]
    public void Plusieurs_signataires_sont_refuses()
    {
        var output = ZenSignature.Replace("Number of signers: 1", "Number of signers: 2");

        Assert.Null(ApkToolOutputParser.ParseSignature(output));
    }
}
