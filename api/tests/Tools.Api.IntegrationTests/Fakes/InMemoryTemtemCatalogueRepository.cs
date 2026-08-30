using Tools.Api.Modules.Temtem.Creatures.Application.Ports;
using Tools.Api.Modules.Temtem.Creatures.Application.Views;
using Tools.Api.Modules.Temtem.Techniques.Application.Views;
using Tools.Api.Modules.Temtem.Traits.Application.Views;
using Tools.Api.Modules.Temtem.Types.Application.Ports;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.IntegrationTests.Fakes;

// Catalogue Temtem en mémoire : deux Temtem, l'un à type unique et l'autre à double type, pour
// éprouver le câblage HTTP du module sans PostgreSQL. Le SQL lui-même est vérifié contre la
// base réelle, pas ici.
public sealed class InMemoryTemtemCatalogueRepository : ITemtemTypeRepository, ITemtemCreatureRepository
{
    public const string MonoTypeSlug = "mimit";
    public const string DoubleTypeSlug = "platypet";
    public const int MonoTypeId = 1;
    public const int DoubleTypeId = 7;

    // Une technique que le Temtem à double type apprend, et une qu'il n'apprend pas : c'est le
    // couple dont la composition d'équipe a besoin pour être éprouvée.
    public const int LearnableTechniqueId = 53;
    public const int OtherLearnableTechniqueId = 54;
    public const int UnlearnableTechniqueId = 999;

    private static readonly TemtemTypeView Water = new(3, "eau", "Eau", null);
    private static readonly TemtemTypeView Toxic = new(12, "toxique", "Toxique", null);
    private static readonly TemtemTypeView Digital = new(9, "digital", "Numérique", null);

    private static readonly TemtemSummaryView MonoType = new(
        MonoTypeId, MonoTypeSlug, "Mimit", null, Digital, null, new TemtemStatsView(55, 55, 55, 55, 65, 55, 65));

    private static readonly TemtemSummaryView DoubleType = new(
        DoubleTypeId, DoubleTypeSlug, "Platypet", null, Water, Toxic, new TemtemStatsView(55, 39, 65, 45, 31, 67, 56));

    private readonly List<TemtemSummaryView> creatures = [MonoType, DoubleType];

    public Task<List<TemtemTypeView>> FindAll() => Task.FromResult(new List<TemtemTypeView> { Water, Toxic, Digital });

    // Volontairement réduite aux types ci-dessus : un couple absent doit rester une erreur, la
    // matrice réelle étant pleine.
    public Task<Dictionary<(int Attacker, int Defender), decimal>> FindEffectivenessMatrix() =>
        Task.FromResult(new Dictionary<(int, int), decimal>
        {
            [(Water.Id, Water.Id)] = 0.5m,
            [(Water.Id, Toxic.Id)] = 1m,
            [(Water.Id, Digital.Id)] = 1m
        });

    Task<List<TemtemSummaryView>> ITemtemCreatureRepository.FindAll() => Task.FromResult(creatures);

    public Task<bool> Exists(int temtemId) =>
        Task.FromResult(creatures.Any(creature => creature.Id == temtemId));

    public Task<HashSet<int>> FindLearnedTechniqueIds(int temtemId) =>
        Task.FromResult(creatures.Any(creature => creature.Id == temtemId)
            ? new HashSet<int> { LearnableTechniqueId, OtherLearnableTechniqueId }
            : []);

    // Le résumé tel que le catalogue le rend : la composition d'équipe le réutilise au lieu de
    // le recomposer, exactement comme le fait le vrai adaptateur.
    public TemtemSummaryView? SummaryById(int temtemId) =>
        creatures.FirstOrDefault(creature => creature.Id == temtemId);

    public TemtemTechniqueView TechniqueById(int techniqueId) => new(
        techniqueId, $"technique-{techniqueId}", $"Technique {techniqueId}", null,
        Water,
        new TemtemCategoryView("PHYSICAL", "Physique", null),
        new TemtemPriorityView(3, "Rapide", null),
        32, 4, null, ["SINGLE_OPPONENT"]);

    public Task<TemtemDetailView?> FindBySlug(string slug)
    {
        var creature = creatures.FirstOrDefault(temtem => temtem.Slug == slug);

        return Task.FromResult(creature is null
            ? null
            : new TemtemDetailView(
                creature,
                [
                    new TemtemLearnedTechniqueView(
                        new TemtemTechniqueView(
                            LearnableTechniqueId, "claque-nageoire", "Claque-nageoire", "Aucun effet particulier.",
                            Water,
                            new TemtemCategoryView("PHYSICAL", "Physique", null),
                            new TemtemPriorityView(3, "Rapide", null),
                            32, 4, null, ["ALLY", "SINGLE_OPPONENT"]),
                        "LEVEL",
                        1)
                ],
                [new TemtemTraitView(14, "amphibie", "Amphibie", "Donne VIT et ATQ.")]));
    }
}
