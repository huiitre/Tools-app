using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Temtem.Sync.Application.Data;
using Tools.Api.Modules.Temtem.Sync.Application.Ports;

namespace Tools.Api.Modules.Temtem.Sync.Application.Usecases;

// Recharge le catalogue Temtem depuis les données de l'extracteur.
//
// **Ce n'est pas un SecuredUseCase** : la route est réservée aux appels de service à service et
// authentifiée par secret partagé, aucun utilisateur n'est identifié. Même raison que
// SendInternalMailUseCase.
//
// Tout se joue dans une seule transaction. L'ordre n'est pas négociable : les référentiels avant
// ce qui les référence, les liaisons réécrites avant les suppressions — sinon une ligne de
// liaison désignerait une entité qu'on vient de supprimer.
public sealed class SyncTemtemCatalogueUseCase(
    ITemtemDataProvider dataProvider,
    ITemtemCatalogueRepository catalogueRepository,
    ITransactionManager transactionManager,
    ILogger<SyncTemtemCatalogueUseCase> logger)
{
    public async Task<TemtemCatalogueSyncReport> Execute()
    {
        var categories = Guard(await dataProvider.FetchCategories(), "category.json");
        var priorities = Guard(await dataProvider.FetchPriorities(), "priority.json");
        var types = Guard(await dataProvider.FetchTypes(), "types.json");
        var creatures = Guard(await dataProvider.FetchCreatures(), "temtem.json");
        var techniques = Guard(await dataProvider.FetchTechniques(), "technique.json");
        var traits = Guard(await dataProvider.FetchTraits(), "trait.json");
        var learnings = Guard(await dataProvider.FetchLearnings(), "temtem_technique.json");
        var traitLinks = Guard(await dataProvider.FetchTraitLinks(), "temtem_trait.json");
        var matchups = Guard(await dataProvider.FetchTypeMatchups(), "type_matrix.json");

        await using var transaction = await transactionManager.BeginAsync();

        var categoryReport = await Upsert(categories, catalogueRepository.UpsertCategory);
        var priorityReport = await Upsert(priorities, catalogueRepository.UpsertPriority);
        var typeReport = await Upsert(types, catalogueRepository.UpsertType);
        var creatureReport = await Upsert(creatures, catalogueRepository.UpsertCreature);
        var techniqueReport = await Upsert(techniques, catalogueRepository.UpsertTechnique);
        var traitReport = await Upsert(traits, catalogueRepository.UpsertTrait);

        var links = new TemtemLinkSyncReport(
            await catalogueRepository.ReplaceTechniqueTargets(techniques),
            await catalogueRepository.ReplaceLearnings(learnings),
            await catalogueRepository.ReplaceTraitLinks(traitLinks),
            await catalogueRepository.ReplaceTypeMatchups(matchups));

        // Suppressions en dernier, et des entités vers les référentiels : une technique disparue
        // doit partir avant la catégorie qu'elle était seule à utiliser.
        creatureReport = creatureReport with { Deleted = await catalogueRepository.DeleteCreaturesExcept([.. creatures.Select(x => x.Id)]) };
        techniqueReport = techniqueReport with { Deleted = await catalogueRepository.DeleteTechniquesExcept([.. techniques.Select(x => x.Id)]) };
        traitReport = traitReport with { Deleted = await catalogueRepository.DeleteTraitsExcept([.. traits.Select(x => x.Id)]) };
        typeReport = typeReport with { Deleted = await catalogueRepository.DeleteTypesExcept([.. types.Select(x => x.Id)]) };
        categoryReport = categoryReport with { Deleted = await catalogueRepository.DeleteCategoriesExcept([.. categories.Select(x => x.Code)]) };
        priorityReport = priorityReport with { Deleted = await catalogueRepository.DeletePrioritiesExcept([.. priorities.Select(x => x.Order)]) };

        await transaction.CommitAsync();

        var report = new TemtemCatalogueSyncReport(
            categoryReport, priorityReport, typeReport,
            creatureReport, techniqueReport, traitReport, links);

        logger.LogInformation(
            "Synchronisation Temtem terminée : {Creatures} Temtem, {Techniques} techniques, {Traits} traits, {Links} liaisons",
            creatures.Count, techniques.Count, traits.Count,
            links.TechniqueTargets + links.Learnings + links.TemtemTraits + links.TypeMatchups);

        return report;
    }

    // Un fichier vide viderait la table correspondante, et emporterait le reste par cascade. Une
    // extraction ratée doit interrompre la synchronisation, jamais la laisser tout effacer.
    private static List<T> Guard<T>(List<T> rows, string fileName) =>
        rows.Count > 0
            ? rows
            : throw AppException.Unavailable(
                "TEMTEM_ASSET_EMPTY",
                $"Le fichier {fileName} ne contient aucune entrée : synchronisation interrompue.");

    private static async Task<TemtemSyncReport> Upsert<T>(
        List<T> rows,
        Func<T, Task<TemtemUpsertOutcome>> upsert)
    {
        var created = 0;
        var updated = 0;

        foreach (var row in rows)
        {
            switch (await upsert(row))
            {
                case TemtemUpsertOutcome.Created: created++; break;
                case TemtemUpsertOutcome.Updated: updated++; break;
            }
        }

        return new TemtemSyncReport(created, updated, 0);
    }
}
