using Tools.Api.Modules.Core.AndroidApps.Application.Usecases;
using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.AndroidApps;

public sealed class AndroidAppImportRulesTests
{
    private static readonly byte[] CertA = Enumerable.Repeat((byte)0xAA, 32).ToArray();
    private static readonly byte[] CertB = Enumerable.Repeat((byte)0xBB, 32).ToArray();

    private static ApkInfo Apk(long versionCode, byte[] cert, string? label = "Zen Player") => new(
        "app.zeniptv.mobile", versionCode, "2.1.0", 24, label, 1000, new byte[32], cert, "CN=Test", ["v2"]);

    private static AndroidAppRelease Release(long id, long versionCode, byte[] cert) => new(
        id, 1, versionCode, null, 24, $"tools_core/android_apps/dev/app.zeniptv.mobile/{versionCode}.apk",
        1000, new byte[32], cert, null, DateTimeOffset.UtcNow, 1, null);

    private static AndroidApp App(long? recommendedReleaseId, params AndroidAppRelease[] releases) => new(
        1, "app.zeniptv.mobile", "Zen", 0, true, recommendedReleaseId, null, DateTimeOffset.UtcNow, releases);

    [Fact]
    public void Un_paquet_inconnu_leve_NEW_APP_et_rien_d_autre()
    {
        var warnings = AndroidAppImportRules.Warnings(null, Apk(148, CertA));

        Assert.Equal([AndroidAppImportRules.NewApp], warnings.Select(warning => warning.Code));
    }

    [Fact]
    public void Un_certificat_different_de_la_version_recommandee_leve_CERTIFICATE_CHANGED()
    {
        var app = App(10, Release(10, 140, CertA));

        var warnings = AndroidAppImportRules.Warnings(app, Apk(148, CertB));

        Assert.Equal([AndroidAppImportRules.CertificateChanged], warnings.Select(warning => warning.Code));
    }

    [Fact]
    public void Le_certificat_est_compare_a_la_version_recommandee_pas_a_la_derniere_importee()
    {
        // La nightly 150 (autre clé) n'est pas recommandée : c'est la 140 qui est sur les TV.
        var app = App(10, Release(11, 150, CertB), Release(10, 140, CertA));

        Assert.Empty(AndroidAppImportRules.Warnings(app, Apk(148, CertA)));
    }

    [Fact]
    public void Sans_version_recommandee_aucun_certificat_n_est_compare()
    {
        var app = App(null, Release(10, 140, CertA));

        Assert.Empty(AndroidAppImportRules.Warnings(app, Apk(148, CertB)));
    }

    [Fact]
    public void Une_version_plus_ancienne_que_la_version_recommandee_leve_OLDER_THAN_RECOMMENDED()
    {
        var app = App(11, Release(11, 150, CertA));

        var warnings = AndroidAppImportRules.Warnings(app, Apk(140, CertA));

        Assert.Equal([AndroidAppImportRules.OlderThanRecommended], warnings.Select(warning => warning.Code));
    }

    [Fact]
    public void Un_android_minimum_releve_leve_MIN_SDK_RAISED()
    {
        // La version proposée tourne dès Android 5 (API 21), le nouvel APK exige Android 7 (API 24).
        var recommended = Release(10, 140, CertA) with { MinSdk = 21 };
        var app = App(10, recommended);

        var warnings = AndroidAppImportRules.Warnings(app, Apk(148, CertA));

        Assert.Equal([AndroidAppImportRules.MinSdkRaised], warnings.Select(warning => warning.Code));
    }

    [Fact]
    public void Une_mise_a_jour_ordinaire_ne_leve_aucun_avertissement()
    {
        var app = App(10, Release(10, 140, CertA));

        Assert.Empty(AndroidAppImportRules.Warnings(app, Apk(148, CertA)));
    }

    [Fact]
    public void Un_versionCode_deja_importe_est_refuse()
    {
        var app = App(10, Release(10, 148, CertA));

        var exception = Assert.Throws<AppException>(() => AndroidAppImportRules.EnsureVersionIsNew(app, Apk(148, CertA)));

        Assert.Equal("ANDROID_APP_VERSION_EXISTS", exception.Code);
    }

    [Fact]
    public void Une_version_plus_ancienne_que_les_existantes_est_acceptee()
    {
        // Une stable 140 importée après une nightly 150 : l'ordre d'import est libre.
        var app = App(11, Release(11, 150, CertA));

        AndroidAppImportRules.EnsureVersionIsNew(app, Apk(140, CertA));
    }

    [Fact]
    public void Le_libelle_par_defaut_est_celui_de_l_apk_sinon_le_paquet()
    {
        Assert.Equal("Zen Player", AndroidAppImportRules.DefaultLabel(Apk(1, CertA)));
        Assert.Equal("app.zeniptv.mobile", AndroidAppImportRules.DefaultLabel(Apk(1, CertA, label: null)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void Des_notes_vides_effacent_les_notes(string notes)
    {
        Assert.Null(AndroidApp.NormalizeAdminNotes(notes));
    }

    [Fact]
    public void Les_notes_sont_rognees_et_bornees_a_5000_caracteres()
    {
        Assert.Equal("https://github.com/x/y", AndroidApp.NormalizeAdminNotes("  https://github.com/x/y \n"));

        var exception = Assert.Throws<AppException>(() => AndroidApp.NormalizeAdminNotes(new string('a', 5001)));
        Assert.Equal("ANDROID_APP_ADMIN_NOTES_TOO_LONG", exception.Code);
    }
}
