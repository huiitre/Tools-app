using System.Text.Json.Nodes;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Settings.Domain;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.Settings;

// La règle de résolution, éprouvée sans base ni requête HTTP : c'est tout l'intérêt d'en avoir
// fait une fonction pure. Chaque test nomme la règle qu'il protège.
public sealed class SettingResolutionTests
{
    private const long UserId = 7;

    private static readonly ChoiceSetting Theme = new()
    {
        Code = "ui.theme",
        AllowedScopes = SettingScopes.All,
        MinRoleToView = RoleCode.ReadOnly,
        MinRoleToSetOwn = RoleCode.ReadOnly,
        Options = ["light", "dark"],
        Default = "dark"
    };

    [Fact]
    public void Sans_aucune_valeur_le_defaut_du_catalogue_sapplique()
    {
        var resolved = Resolve(Theme, [], Audience(RoleCode.User));

        Assert.Equal("dark", Text(resolved.Value));
        Assert.Null(resolved.Source);
        Assert.False(resolved.CanReset);
    }

    [Fact]
    public void La_portee_la_plus_prioritaire_lemporte()
    {
        var resolved = Resolve(
            Theme,
            [Global(Theme, "light"), ForRole(Theme, RoleCode.User, "dark"), ForUser(Theme, "light")],
            Audience(RoleCode.User));

        Assert.Equal(SettingScope.User, resolved.Source);
        Assert.Equal("light", Text(resolved.Value));
    }

    [Fact]
    public void Sans_valeur_utilisateur_celle_du_role_lemporte_sur_le_global()
    {
        var resolved = Resolve(
            Theme,
            [Global(Theme, "dark"), ForRole(Theme, RoleCode.User, "light")],
            Audience(RoleCode.User));

        Assert.Equal(SettingScope.Role, resolved.Source);
        Assert.Equal("light", Text(resolved.Value));
    }

    [Fact]
    public void Une_valeur_de_role_ne_sapplique_quau_role_vise()
    {
        // Correspondance exacte, pas un seuil : une valeur posée pour les modérateurs ne
        // descend pas — ni ne monte — sur les autres rôles. C'est la différence avec
        // MinRoleToView, qui est une permission.
        var resolved = Resolve(
            Theme,
            [Global(Theme, "dark"), ForRole(Theme, RoleCode.Moderator, "light")],
            Audience(RoleCode.Admin));

        Assert.Equal(SettingScope.Global, resolved.Source);
        Assert.Equal("dark", Text(resolved.Value));
    }

    [Fact]
    public void Un_verrou_global_ecrase_la_valeur_utilisateur_sans_la_supprimer()
    {
        var resolved = Resolve(
            Theme,
            [Global(Theme, "dark", locked: true), ForUser(Theme, "light")],
            Audience(RoleCode.User));

        Assert.Equal(SettingScope.Global, resolved.Source);
        Assert.Equal("dark", Text(resolved.Value));
        Assert.True(resolved.IsLocked);
        Assert.False(resolved.CanSetOwn);
    }

    [Fact]
    public void Entre_deux_verrous_cest_le_plus_bas_qui_gagne()
    {
        // Un verrou interdit tout ce qui est au-dessus de lui : celui posé en Global interdit
        // aussi bien la valeur de rôle que celle de l'utilisateur.
        var resolved = Resolve(
            Theme,
            [Global(Theme, "dark", locked: true), ForRole(Theme, RoleCode.User, "light", locked: true)],
            Audience(RoleCode.User));

        Assert.Equal(SettingScope.Global, resolved.Source);
        Assert.Equal("dark", Text(resolved.Value));
    }

    [Fact]
    public void Une_ligne_dont_la_portee_nest_plus_autorisee_est_ignoree()
    {
        // Le paramètre d'instance n'accepte que Global. Une ligne User posée du temps où elle
        // était permise — ou à la main en base — ne doit pas pouvoir le détourner.
        var maintenance = new BooleanSetting
        {
            Code = "system.maintenanceMode",
            AllowedScopes = SettingScopes.GlobalOnly,
            MinRoleToView = RoleCode.Admin,
            Default = false
        };

        var resolved = Resolve(
            maintenance,
            [new SettingValue(maintenance.Code, SettingScope.User, null, UserId, JsonValue.Create(true), false)],
            Audience(RoleCode.Admin));

        Assert.Null(resolved.Source);
        Assert.False(resolved.Value.GetValue<bool>());
        Assert.False(resolved.CanSetOwn);
    }

    [Fact]
    public void Une_valeur_devenue_invalide_est_ignoree_et_lheritage_reprend()
    {
        // Contrainte resserrée après coup : la borne a bougé, la valeur en base ne passe plus.
        // Elle est écartée, la lecture ne casse pas.
        var pageSize = new IntegerSetting
        {
            Code = "ui.pageSize",
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.ReadOnly,
            Min = 10,
            Max = 50,
            Default = 25
        };

        var resolved = Resolve(
            pageSize,
            [new SettingValue(pageSize.Code, SettingScope.User, null, UserId, JsonValue.Create(500), false)],
            Audience(RoleCode.User));

        Assert.Null(resolved.Source);
        Assert.Equal(25, resolved.Value.GetValue<long>());
    }

    [Fact]
    public void Un_parametre_de_module_se_juge_sur_le_role_du_module()
    {
        var moduleSetting = new BooleanSetting
        {
            Code = "dofus.autoSync",
            Module = ModuleCode.Dofus,
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.User,
            Default = true
        };

        // Administrateur du site, mais READ_ONLY dans le module : c'est le rôle du module qui
        // décide. Il suffit pour voir, pas pour régler — le rôle global n'y ajoute rien.
        var audience = new SettingAudience(
            UserId,
            RoleCode.Admin,
            new Dictionary<ModuleCode, RoleCode> { [ModuleCode.Dofus] = RoleCode.ReadOnly });

        Assert.True(SettingResolution.CanView(moduleSetting, audience));
        Assert.False(SettingResolution.Resolve(moduleSetting, [], audience).CanSetOwn);
    }

    [Fact]
    public void Un_parametre_de_module_est_invisible_sans_acces_au_module()
    {
        var moduleSetting = new BooleanSetting
        {
            Code = "dofus.autoSync",
            Module = ModuleCode.Dofus,
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.ReadOnly,
            Default = true
        };

        var audience = new SettingAudience(UserId, RoleCode.Owner, new Dictionary<ModuleCode, RoleCode>());

        Assert.False(SettingResolution.CanView(moduleSetting, audience));
    }

    [Fact]
    public void La_visibilite_est_un_seuil_donc_un_admin_voit_un_parametre_moderateur()
    {
        var moderation = new IntegerSetting
        {
            Code = "moderation.autoFlagThreshold",
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.Moderator,
            MinRoleToSetOwn = RoleCode.Moderator,
            Min = 1,
            Max = 100,
            Default = 10
        };

        Assert.True(SettingResolution.CanView(moderation, Audience(RoleCode.Admin)));
        Assert.True(SettingResolution.CanView(moderation, Audience(RoleCode.Moderator)));
        Assert.False(SettingResolution.CanView(moderation, Audience(RoleCode.User)));
    }

    [Fact]
    public void Un_role_insuffisant_ne_peut_pas_poser_sa_propre_valeur()
    {
        var pageSize = new IntegerSetting
        {
            Code = "ui.pageSize",
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.User,
            Min = 10,
            Max = 200,
            Default = 25
        };

        Assert.False(SettingResolution.Resolve(pageSize, [], Audience(RoleCode.ReadOnly)).CanSetOwn);
        Assert.True(SettingResolution.Resolve(pageSize, [], Audience(RoleCode.User)).CanSetOwn);
    }

    [Fact]
    public void Une_ligne_dun_autre_parametre_nest_jamais_retenue()
    {
        // L'appelant charge en un coup les lignes de tous les paramètres : la résolution doit
        // écarter elle-même celles qui ne concernent pas la définition demandée. Deux paramètres
        // de même type aux options qui se recoupent se contamineraient sinon — `Accepts` ne
        // suffit pas à les distinguer.
        var autre = new ChoiceSetting
        {
            Code = "ui.accent",
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.ReadOnly,
            Options = ["light", "dark"],
            Default = "dark"
        };

        var resolved = Resolve(Theme, [ForUser(autre, "light")], Audience(RoleCode.User));

        Assert.Null(resolved.Source);
        Assert.Equal("dark", Text(resolved.Value));
    }

    [Fact]
    public void Une_valeur_ecrite_sous_un_ancien_code_reste_lue()
    {
        var renomme = new ChoiceSetting
        {
            Code = "ui.colorScheme",
            PreviousCodes = ["ui.theme"],
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.ReadOnly,
            Options = ["light", "dark"],
            Default = "dark"
        };

        var ancienneLigne = new SettingValue(
            "ui.theme", SettingScope.User, null, UserId, JsonValue.Create("light"), false);

        var resolved = Resolve(renomme, [ancienneLigne], Audience(RoleCode.User));

        Assert.Equal(SettingScope.User, resolved.Source);
        Assert.Equal("light", Text(resolved.Value));
    }

    [Fact]
    public void Le_catalogue_reel_satisfait_ses_propres_invariants()
    {
        // Touche le catalogue pour déclencher son garde-fou statique. Sans ce test, une
        // définition incohérente n'échouerait qu'au démarrage de l'application.
        Assert.NotEmpty(SettingCatalog.All);
        Assert.NotEmpty(SettingCatalog.AllStoredCodes);
        Assert.NotNull(SettingCatalog.Find("ui.theme"));
        Assert.Null(SettingCatalog.Find("code.inexistant"));
    }

    // ---------- Utilitaires ----------

    private static ResolvedSetting Resolve(
        SettingDefinition definition,
        IEnumerable<SettingValue> candidates,
        SettingAudience audience) =>
        SettingResolution.Resolve(definition, candidates, audience);

    private static SettingAudience Audience(RoleCode role) =>
        new(UserId, role, new Dictionary<ModuleCode, RoleCode>());

    private static SettingValue Global(SettingDefinition definition, string value, bool locked = false) =>
        new(definition.Code, SettingScope.Global, null, null, JsonValue.Create(value), locked);

    private static SettingValue ForRole(
        SettingDefinition definition, RoleCode role, string value, bool locked = false) =>
        new(definition.Code, SettingScope.Role, role, null, JsonValue.Create(value), locked);

    private static SettingValue ForUser(SettingDefinition definition, string value, bool locked = false) =>
        new(definition.Code, SettingScope.User, null, UserId, JsonValue.Create(value), locked);

    private static string? Text(JsonNode node) => node.GetValue<string>();
}
