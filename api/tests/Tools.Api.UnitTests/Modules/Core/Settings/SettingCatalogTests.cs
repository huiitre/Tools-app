using System.Reflection;
using Tools.Api.Modules.Core.Settings.Domain;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.Settings;

public sealed class SettingCatalogTests
{
    [Fact]
    public void Toute_definition_declaree_figure_dans_le_recensement()
    {
        // `All` est tenue à la main : une définition déclarée mais oubliée dans la liste ne
        // serait ni servie au frontend, ni lue en base, ni protégée par le garde-fou — elle
        // existerait dans le code sans exister dans l'application, et rien ne le dirait.
        var declared = typeof(SettingCatalog)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(group => group.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => typeof(SettingDefinition).IsAssignableFrom(field.FieldType))
            .Select(field => (SettingDefinition)field.GetValue(null)!)
            .ToList();

        Assert.NotEmpty(declared);

        var missing = declared
            .Where(definition => !SettingCatalog.All.Contains(definition))
            .Select(definition => definition.Code)
            .ToList();

        Assert.True(missing.Count == 0, $"Définitions absentes de SettingCatalog.All : {string.Join(", ", missing)}");
        Assert.Equal(declared.Count, SettingCatalog.All.Count);
    }

    [Fact]
    public void Chaque_code_est_resolvable_y_compris_les_codes_historiques()
    {
        foreach (var definition in SettingCatalog.All)
        {
            foreach (var code in definition.AllCodes)
            {
                Assert.Same(definition, SettingCatalog.Find(code));
            }
        }
    }
}
