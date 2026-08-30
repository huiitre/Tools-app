using Tools.Api.Modules.Temtem.Creatures.Application.Views;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Creatures.Infrastructure;

// Le résumé d'un Temtem se lit à l'identique depuis le catalogue, la fiche et la composition
// d'une équipe — c'est la vue que la consigne demande de ne pas dupliquer. Son SQL ne l'est pas
// davantage.
//
// Les alias `t`, `t1` et `t2` sont réservés à ce fragment.
public static class TemtemCreatureSql
{
    public const string Columns = """
        t.id AS Id, t.slug AS Slug, t.name AS Name, t.image_url AS ImageUrl,
        t.hp AS Hp, t.stamina AS Stamina, t.speed AS Speed, t.attack AS Attack,
        t.defense AS Defense, t.special_attack AS SpecialAttack, t.special_defense AS SpecialDefense,
        t1.id AS Type1Id, t1.slug AS Type1Slug, t1.name AS Type1Name, t1.image_url AS Type1ImageUrl,
        t2.id AS Type2Id, t2.slug AS Type2Slug, t2.name AS Type2Name, t2.image_url AS Type2ImageUrl
        """;

    // Le second type est joint en externe : un Temtem sur deux n'en a qu'un.
    public const string Joins = """
        JOIN tools_temtem.type t1 ON t1.id = t.type1_id
        LEFT JOIN tools_temtem.type t2 ON t2.id = t.type2_id
        """;

    public const string From = $"""
        FROM tools_temtem.temtem t
        {Joins}
        """;

    public static TemtemSummaryView ToView(Row row) => new(
        row.Id,
        row.Slug,
        row.Name,
        row.ImageUrl,
        new TemtemTypeView(row.Type1Id, row.Type1Slug, row.Type1Name, row.Type1ImageUrl),
        row.Type2Id is { } type2Id
            ? new TemtemTypeView(type2Id, row.Type2Slug!, row.Type2Name!, row.Type2ImageUrl)
            : null,
        new TemtemStatsView(
            row.Hp, row.Stamina, row.Speed, row.Attack,
            row.Defense, row.SpecialAttack, row.SpecialDefense));

    public class Row
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Hp { get; set; }
        public int Stamina { get; set; }
        public int Speed { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Type1Id { get; set; }
        public string Type1Slug { get; set; } = string.Empty;
        public string Type1Name { get; set; } = string.Empty;
        public string? Type1ImageUrl { get; set; }
        public int? Type2Id { get; set; }
        public string? Type2Slug { get; set; }
        public string? Type2Name { get; set; }
        public string? Type2ImageUrl { get; set; }
    }
}
