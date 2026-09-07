using System.Text.Json.Nodes;
using Tools.Api.Modules.Core.Settings.Application.Ports;
using Tools.Api.Modules.Core.Settings.Domain;

namespace Tools.Api.IntegrationTests.Fakes;

// Remplace l'accès PostgreSQL aux valeurs de paramètres.
//
// Sans lui, le moindre use case qui lit un paramètre — l'inscription, la confirmation d'adresse,
// le login Google — ouvrirait une vraie connexion à la base et ferait échouer le test pour une
// raison sans rapport avec ce qu'il vérifie.
//
// Vide par défaut : aucune valeur posée, donc chaque paramètre vaut son défaut de catalogue.
// C'est exactement l'état d'une installation neuve, et ce que doivent constater les tests qui
// ne s'intéressent pas aux réglages.
public sealed class InMemorySettingValueRepository : ISettingValueRepository
{
    private readonly List<SettingValue> values = [];

    // Pose une valeur globale, la seule accroche dont les tests aient besoin aujourd'hui : les
    // paramètres d'inscription ne déclarent que `Global`.
    public void SetGlobal(SettingDefinition definition, JsonNode value)
    {
        values.RemoveAll(existing => existing.Code == definition.Code);
        values.Add(new SettingValue(definition.Code, SettingScope.Global, null, null, value, false));
    }

    public void Clear() => values.Clear();

    public Task<IReadOnlyList<SettingValue>> FindAsync(
        IReadOnlyCollection<string> codes,
        long? userId,
        IReadOnlyCollection<string> roleCodes)
    {
        // Le filtrage sur les codes suffit : tout ce qui est stocké ici est global, donc
        // concerne tout le monde. `SettingResolution` refait de toute façon le tri exact.
        return Task.FromResult<IReadOnlyList<SettingValue>>(
            [.. values.Where(value => codes.Contains(value.Code))]);
    }
}
