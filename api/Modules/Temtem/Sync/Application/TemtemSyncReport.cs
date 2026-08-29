namespace Tools.Api.Modules.Temtem.Sync.Application;

public sealed record TemtemSyncReport(int Created, int Updated, int Deleted);

// Les tables de liaison sont réécrites en bloc plutôt que comparées ligne à ligne : leur contenu
// n'a pas d'identité propre à préserver, le rapport n'a donc qu'un nombre de lignes à donner.
public sealed record TemtemLinkSyncReport(
    int TechniqueTargets,
    int Learnings,
    int TemtemTraits,
    int TypeMatchups);

public sealed record TemtemCatalogueSyncReport(
    TemtemSyncReport Categories,
    TemtemSyncReport Priorities,
    TemtemSyncReport Types,
    TemtemSyncReport Creatures,
    TemtemSyncReport Techniques,
    TemtemSyncReport Traits,
    TemtemLinkSyncReport Links);
