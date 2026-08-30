using System.Text.Json;
using Tools.Api.Modules.Temtem.Sync.Application.Data;
using Tools.Api.Modules.Temtem.Sync.Application.Ports;

namespace Tools.Api.Modules.Temtem.Sync.Infrastructure;

// Lit les fichiers publiés par l'extracteur et les traduit en données d'application. C'est ici,
// et nulle part ailleurs, que les particularités de la source sont absorbées : noms de fichiers
// d'images, ciblage publié à part, priorité imbriquée dans un objet.
public sealed class TemtemAssetsDataProvider(
    TemtemAssetsReader assetsReader,
    TemtemAssetUrlBuilder urlBuilder) : ITemtemDataProvider
{
    public async Task<List<TemtemCategoryData>> FetchCategories()
    {
        var data = await assetsReader.Read("category.json");

        return [.. data.EnumerateArray().Select(entry => new TemtemCategoryData(
            TemtemAssetJson.RequiredString(entry, "code"),
            TemtemAssetJson.RequiredString(entry, "label"),
            // « filename » et non « code » : STATUS donne « statut ».
            urlBuilder.Category(TemtemAssetJson.RequiredString(entry, "filename"))))];
    }

    public async Task<List<TemtemPriorityData>> FetchPriorities()
    {
        var data = await assetsReader.Read("priority.json");

        return [.. data.EnumerateArray().Select(entry => new TemtemPriorityData(
            TemtemAssetJson.RequiredInt(entry, "order"),
            TemtemAssetJson.RequiredString(entry, "label"),
            urlBuilder.Priority(TemtemAssetJson.RequiredString(entry, "filename"))))];
    }

    public async Task<List<TemtemTypeData>> FetchTypes()
    {
        var data = await assetsReader.Read("types.json");

        return [.. data.EnumerateArray().Select(entry =>
        {
            var slug = TemtemAssetJson.RequiredString(entry, "slug");

            return new TemtemTypeData(
                TemtemAssetJson.RequiredInt(entry, "id"),
                slug,
                TemtemAssetJson.RequiredString(entry, "name"),
                urlBuilder.Type(slug));
        })];
    }

    public async Task<List<TemtemCreatureData>> FetchCreatures()
    {
        var data = await assetsReader.Read("temtem.json");

        return [.. data.EnumerateArray().Select(entry =>
        {
            var slug = TemtemAssetJson.RequiredString(entry, "slug");
            var stats = TemtemAssetJson.RequiredObject(entry, "stats");

            return new TemtemCreatureData(
                TemtemAssetJson.RequiredInt(entry, "id"),
                slug,
                TemtemAssetJson.RequiredString(entry, "name"),
                TemtemAssetJson.RequiredInt(entry, "type1Id"),
                TemtemAssetJson.OptionalInt(entry, "type2Id"),
                urlBuilder.Creature(slug),
                new TemtemStatsData(
                    TemtemAssetJson.RequiredInt(stats, "hp"),
                    TemtemAssetJson.RequiredInt(stats, "stamina"),
                    TemtemAssetJson.RequiredInt(stats, "speed"),
                    TemtemAssetJson.RequiredInt(stats, "attack"),
                    TemtemAssetJson.RequiredInt(stats, "defense"),
                    TemtemAssetJson.RequiredInt(stats, "specialAttack"),
                    TemtemAssetJson.RequiredInt(stats, "specialDefense")));
        })];
    }

    // Le ciblage vit dans son propre fichier : il est rattaché ici pour que l'application n'ait
    // qu'une seule notion de technique. Le champ « mandatory » de la source est ignoré — il doit
    // disparaître.
    public async Task<List<TemtemTechniqueData>> FetchTechniques()
    {
        var techniques = await assetsReader.Read("technique.json");
        var targets = await assetsReader.Read("technique_target.json");

        var targetsByTechnique = targets.EnumerateArray()
            .GroupBy(entry => TemtemAssetJson.RequiredInt(entry, "techniqueId"))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[.. group.Select(entry => TemtemAssetJson.RequiredString(entry, "target"))]);

        return [.. techniques.EnumerateArray().Select(entry =>
        {
            var id = TemtemAssetJson.RequiredInt(entry, "id");
            var priority = TemtemAssetJson.RequiredObject(entry, "priority");

            return new TemtemTechniqueData(
                id,
                TemtemAssetJson.RequiredString(entry, "slug"),
                TemtemAssetJson.RequiredString(entry, "name"),
                TemtemAssetJson.OptionalString(entry, "effect"),
                TemtemAssetJson.RequiredInt(entry, "typeId"),
                TemtemAssetJson.RequiredString(entry, "category"),
                TemtemAssetJson.RequiredInt(priority, "order"),
                TemtemAssetJson.OptionalInt(entry, "damage"),
                TemtemAssetJson.OptionalInt(entry, "stamina"),
                TemtemAssetJson.OptionalInt(entry, "chargeTurns"),
                targetsByTechnique.GetValueOrDefault(id, []));
        })];
    }

    public async Task<List<TemtemTraitData>> FetchTraits()
    {
        var data = await assetsReader.Read("trait.json");

        return [.. data.EnumerateArray().Select(entry => new TemtemTraitData(
            TemtemAssetJson.RequiredInt(entry, "id"),
            TemtemAssetJson.RequiredString(entry, "slug"),
            TemtemAssetJson.RequiredString(entry, "name"),
            TemtemAssetJson.OptionalString(entry, "effect")))];
    }

    public async Task<List<TemtemLearningData>> FetchLearnings()
    {
        var data = await assetsReader.Read("temtem_technique.json");

        return [.. data.EnumerateArray().Select(entry => new TemtemLearningData(
            TemtemAssetJson.RequiredInt(entry, "temtemId"),
            TemtemAssetJson.RequiredInt(entry, "techniqueId"),
            TemtemAssetJson.RequiredString(entry, "source"),
            // Renseigné uniquement pour un apprentissage par montée de niveau.
            TemtemAssetJson.OptionalInt(entry, "level")))];
    }

    public async Task<List<TemtemTraitLinkData>> FetchTraitLinks()
    {
        var data = await assetsReader.Read("temtem_trait.json");

        return [.. data.EnumerateArray().Select(entry => new TemtemTraitLinkData(
            TemtemAssetJson.RequiredInt(entry, "temtemId"),
            TemtemAssetJson.RequiredInt(entry, "traitId")))];
    }

    public async Task<List<TemtemTypeMatchupData>> FetchTypeMatchups()
    {
        var data = await assetsReader.Read("type_matrix.json");

        return [.. data.EnumerateArray().Select(entry => new TemtemTypeMatchupData(
            TemtemAssetJson.RequiredInt(entry, "attackerTypeId"),
            TemtemAssetJson.RequiredInt(entry, "defenderTypeId"),
            TemtemAssetJson.RequiredDecimal(entry, "multiplier")))];
    }
}
