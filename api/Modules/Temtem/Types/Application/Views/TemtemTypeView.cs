namespace Tools.Api.Modules.Temtem.Types.Application.Views;

// Un type élémentaire. Porté par les cartes du catalogue, les filtres, et les techniques.
public sealed record TemtemTypeView(int Id, string Slug, string Name, string? ImageUrl);

// Une ligne de la matrice complète, servie une fois au front qui calcule les indications du
// simulateur. Les identifiants suffisent : les libellés et icônes sont déjà dans TemtemTypeView.
public sealed record TemtemTypeEffectivenessView(int AttackerTypeId, int DefenderTypeId, decimal Multiplier);
