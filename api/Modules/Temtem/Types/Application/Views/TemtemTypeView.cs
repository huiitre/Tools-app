namespace Tools.Api.Modules.Temtem.Types.Application.Views;

// Un type élémentaire. Porté par les cartes du catalogue, les filtres, et les techniques.
public sealed record TemtemTypeView(int Id, string Slug, string Name, string? ImageUrl);
