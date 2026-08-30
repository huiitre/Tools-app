namespace Tools.Api.Modules.Temtem.Sync.Application.Data;

// Miroirs des fichiers de l'extracteur. Les URL d'images sont déjà résolues par le fournisseur :
// l'application ne connaît que des URL publiques, jamais des noms de fichiers.
public sealed record TemtemCategoryData(string Code, string Label, string ImageUrl);

public sealed record TemtemPriorityData(int Order, string Label, string ImageUrl);

public sealed record TemtemTypeData(int Id, string Slug, string Name, string ImageUrl);

public sealed record TemtemStatsData(
    int Hp,
    int Stamina,
    int Speed,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense);

public sealed record TemtemCreatureData(
    int Id,
    string Slug,
    string Name,
    int Type1Id,
    int? Type2Id,
    string ImageUrl,
    TemtemStatsData Stats);

// Targets porte le ciblage, arrivé dans son propre fichier : une technique en a une ou deux.
public sealed record TemtemTechniqueData(
    int Id,
    string Slug,
    string Name,
    string? Effect,
    int TypeId,
    string CategoryCode,
    int PriorityOrder,
    int? Damage,
    int? Stamina,
    int? ChargeTurns,
    IReadOnlyList<string> Targets);

public sealed record TemtemTraitData(int Id, string Slug, string Name, string? Effect);

public sealed record TemtemLearningData(int TemtemId, int TechniqueId, string Source, int? Level);

public sealed record TemtemTraitLinkData(int TemtemId, int TraitId);

public sealed record TemtemTypeMatchupData(int AttackerTypeId, int DefenderTypeId, decimal Multiplier);
