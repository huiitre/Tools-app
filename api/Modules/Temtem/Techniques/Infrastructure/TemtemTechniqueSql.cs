using Tools.Api.Modules.Temtem.Techniques.Application.Views;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Techniques.Infrastructure;

// Une technique se lit de la même façon depuis la fiche d'un Temtem et depuis la composition
// d'une équipe. Colonnes, jointures et projection sont donc définies une seule fois : deux
// copies divergeraient au premier champ ajouté.
//
// Les alias `tec`, `ty`, `c` et `p` sont réservés à ce fragment ; la requête qui l'incorpore
// doit en choisir d'autres.
public static class TemtemTechniqueSql
{
    public const string Columns = """
        tec.id AS Id, tec.slug AS Slug, tec.name AS Name, tec.effect AS Effect,
        ty.id AS TypeId, ty.slug AS TypeSlug, ty.name AS TypeName, ty.image_url AS TypeImageUrl,
        c.code AS CategoryCode, c.label AS CategoryLabel, c.image_url AS CategoryImageUrl,
        p.priority_order AS PriorityOrder, p.label AS PriorityLabel, p.image_url AS PriorityImageUrl,
        tec.damage AS Damage, tec.stamina AS Stamina, tec.charge_turns AS ChargeTurns,
        COALESCE((SELECT array_agg(tt.target::text ORDER BY tt.target)
                  FROM tools_temtem.technique_target tt
                  WHERE tt.technique_id = tec.id), ARRAY[]::text[]) AS Targets
        """;

    // La technique est jointe par l'appelant, qui sait d'où vient `tec.id` ; ces trois-là ne
    // dépendent que d'elle.
    public const string Joins = """
        JOIN tools_temtem.type ty ON ty.id = tec.type_id
        JOIN tools_temtem.category c ON c.code = tec.category_code
        JOIN tools_temtem.priority p ON p.priority_order = tec.priority_order
        """;

    public static TemtemTechniqueView ToView(Row row) => new(
        row.Id,
        row.Slug,
        row.Name,
        row.Effect,
        new TemtemTypeView(row.TypeId, row.TypeSlug, row.TypeName, row.TypeImageUrl),
        new TemtemCategoryView(row.CategoryCode, row.CategoryLabel, row.CategoryImageUrl),
        new TemtemPriorityView(row.PriorityOrder, row.PriorityLabel, row.PriorityImageUrl),
        row.Damage,
        row.Stamina,
        row.ChargeTurns,
        row.Targets);

    // Classe à propriétés, et non record positionnel : Npgsql annonce `text[]` comme
    // `System.Array`, si bien que Dapper ne reconnaît aucun constructeur portant un `string[]`.
    public class Row
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Effect { get; set; }
        public int TypeId { get; set; }
        public string TypeSlug { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string? TypeImageUrl { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryLabel { get; set; } = string.Empty;
        public string? CategoryImageUrl { get; set; }
        public int PriorityOrder { get; set; }
        public string PriorityLabel { get; set; } = string.Empty;
        public string? PriorityImageUrl { get; set; }
        public int? Damage { get; set; }
        public int? Stamina { get; set; }
        public int? ChargeTurns { get; set; }
        public string[] Targets { get; set; } = [];
    }
}
