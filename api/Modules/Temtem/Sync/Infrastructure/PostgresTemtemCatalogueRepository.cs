using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Temtem.Sync.Application.Data;
using Tools.Api.Modules.Temtem.Sync.Application.Ports;

namespace Tools.Api.Modules.Temtem.Sync.Infrastructure;

// Adaptateur PostgreSQL/Dapper du catalogue Temtem.
//
// Chaque upsert rend ce qu'il a réellement changé : la clause `WHERE ... IS DISTINCT FROM
// excluded` empêche PostgreSQL de réécrire une ligne identique, si bien que RETURNING ne rend
// rien quand rien n'a bougé. `xmax = 0` distingue alors l'insertion de la mise à jour.
//
// Toutes les méthodes exigent la transaction ouverte par le use case : la synchronisation est
// indivisible, il n'y a pas de mode « une ligne à la fois ».
public sealed class PostgresTemtemCatalogueRepository(PostgresSession session) : ITemtemCatalogueRepository
{
    public Task<TemtemUpsertOutcome> UpsertCategory(TemtemCategoryData data) =>
        Upsert(
            """
            INSERT INTO tools_temtem.category AS t (code, label, image_url)
            VALUES (@Code, @Label, @ImageUrl)
            ON CONFLICT (code) DO UPDATE SET label = excluded.label, image_url = excluded.image_url
            WHERE (t.label, t.image_url) IS DISTINCT FROM (excluded.label, excluded.image_url)
            RETURNING (xmax = 0)
            """,
            data);

    public Task<int> DeleteCategoriesExcept(IReadOnlyCollection<string> codes) =>
        Execute("DELETE FROM tools_temtem.category WHERE code <> ALL(@Keys)", new { Keys = codes.ToArray() });

    public Task<TemtemUpsertOutcome> UpsertPriority(TemtemPriorityData data) =>
        Upsert(
            """
            INSERT INTO tools_temtem.priority AS t (priority_order, label, image_url)
            VALUES (@Order, @Label, @ImageUrl)
            ON CONFLICT (priority_order) DO UPDATE SET label = excluded.label, image_url = excluded.image_url
            WHERE (t.label, t.image_url) IS DISTINCT FROM (excluded.label, excluded.image_url)
            RETURNING (xmax = 0)
            """,
            data);

    public Task<int> DeletePrioritiesExcept(IReadOnlyCollection<int> orders) =>
        Execute("DELETE FROM tools_temtem.priority WHERE priority_order <> ALL(@Keys)", new { Keys = orders.ToArray() });

    public Task<TemtemUpsertOutcome> UpsertType(TemtemTypeData data) =>
        Upsert(
            """
            INSERT INTO tools_temtem.type AS t (id, slug, name, image_url)
            VALUES (@Id, @Slug, @Name, @ImageUrl)
            ON CONFLICT (id) DO UPDATE SET slug = excluded.slug, name = excluded.name, image_url = excluded.image_url
            WHERE (t.slug, t.name, t.image_url) IS DISTINCT FROM (excluded.slug, excluded.name, excluded.image_url)
            RETURNING (xmax = 0)
            """,
            data);

    public Task<int> DeleteTypesExcept(IReadOnlyCollection<int> ids) =>
        Execute("DELETE FROM tools_temtem.type WHERE id <> ALL(@Keys)", new { Keys = ids.ToArray() });

    public Task<TemtemUpsertOutcome> UpsertCreature(TemtemCreatureData data) =>
        Upsert(
            """
            INSERT INTO tools_temtem.temtem AS t
                (id, slug, name, type1_id, type2_id, image_url,
                 hp, stamina, speed, attack, defense, special_attack, special_defense)
            VALUES (@Id, @Slug, @Name, @Type1Id, @Type2Id, @ImageUrl,
                    @Hp, @Stamina, @Speed, @Attack, @Defense, @SpecialAttack, @SpecialDefense)
            ON CONFLICT (id) DO UPDATE SET
                slug = excluded.slug, name = excluded.name,
                type1_id = excluded.type1_id, type2_id = excluded.type2_id, image_url = excluded.image_url,
                hp = excluded.hp, stamina = excluded.stamina, speed = excluded.speed,
                attack = excluded.attack, defense = excluded.defense,
                special_attack = excluded.special_attack, special_defense = excluded.special_defense
            WHERE (t.slug, t.name, t.type1_id, t.type2_id, t.image_url,
                   t.hp, t.stamina, t.speed, t.attack, t.defense, t.special_attack, t.special_defense)
                IS DISTINCT FROM
                  (excluded.slug, excluded.name, excluded.type1_id, excluded.type2_id, excluded.image_url,
                   excluded.hp, excluded.stamina, excluded.speed, excluded.attack, excluded.defense,
                   excluded.special_attack, excluded.special_defense)
            RETURNING (xmax = 0)
            """,
            new
            {
                data.Id, data.Slug, data.Name, data.Type1Id, data.Type2Id, data.ImageUrl,
                data.Stats.Hp, data.Stats.Stamina, data.Stats.Speed, data.Stats.Attack,
                data.Stats.Defense, data.Stats.SpecialAttack, data.Stats.SpecialDefense
            });

    public Task<int> DeleteCreaturesExcept(IReadOnlyCollection<int> ids) =>
        Execute("DELETE FROM tools_temtem.temtem WHERE id <> ALL(@Keys)", new { Keys = ids.ToArray() });

    public Task<TemtemUpsertOutcome> UpsertTechnique(TemtemTechniqueData data) =>
        Upsert(
            """
            INSERT INTO tools_temtem.technique AS t
                (id, slug, name, effect, type_id, category_code, priority_order, damage, stamina, charge_turns)
            VALUES (@Id, @Slug, @Name, @Effect, @TypeId, @CategoryCode, @PriorityOrder, @Damage, @Stamina, @ChargeTurns)
            ON CONFLICT (id) DO UPDATE SET
                slug = excluded.slug, name = excluded.name, effect = excluded.effect,
                type_id = excluded.type_id, category_code = excluded.category_code,
                priority_order = excluded.priority_order, damage = excluded.damage,
                stamina = excluded.stamina, charge_turns = excluded.charge_turns
            WHERE (t.slug, t.name, t.effect, t.type_id, t.category_code,
                   t.priority_order, t.damage, t.stamina, t.charge_turns)
                IS DISTINCT FROM
                  (excluded.slug, excluded.name, excluded.effect, excluded.type_id, excluded.category_code,
                   excluded.priority_order, excluded.damage, excluded.stamina, excluded.charge_turns)
            RETURNING (xmax = 0)
            """,
            new
            {
                data.Id, data.Slug, data.Name, data.Effect, data.TypeId,
                data.CategoryCode, data.PriorityOrder, data.Damage, data.Stamina, data.ChargeTurns
            });

    public Task<int> DeleteTechniquesExcept(IReadOnlyCollection<int> ids) =>
        Execute("DELETE FROM tools_temtem.technique WHERE id <> ALL(@Keys)", new { Keys = ids.ToArray() });

    public Task<TemtemUpsertOutcome> UpsertTrait(TemtemTraitData data) =>
        Upsert(
            """
            INSERT INTO tools_temtem.trait AS t (id, slug, name, effect)
            VALUES (@Id, @Slug, @Name, @Effect)
            ON CONFLICT (id) DO UPDATE SET slug = excluded.slug, name = excluded.name, effect = excluded.effect
            WHERE (t.slug, t.name, t.effect) IS DISTINCT FROM (excluded.slug, excluded.name, excluded.effect)
            RETURNING (xmax = 0)
            """,
            data);

    public Task<int> DeleteTraitsExcept(IReadOnlyCollection<int> ids) =>
        Execute("DELETE FROM tools_temtem.trait WHERE id <> ALL(@Keys)", new { Keys = ids.ToArray() });

    public async Task<int> ReplaceTechniqueTargets(IReadOnlyCollection<TemtemTechniqueData> techniques)
    {
        await Execute("DELETE FROM tools_temtem.technique_target", null);

        var rows = techniques
            .SelectMany(technique => technique.Targets.Select(target => new { TechniqueId = technique.Id, Target = target }))
            .ToList();

        return await Execute(
            """
            INSERT INTO tools_temtem.technique_target (technique_id, target)
            VALUES (@TechniqueId, @Target)
            """,
            rows);
    }

    public async Task<int> ReplaceLearnings(IReadOnlyCollection<TemtemLearningData> learnings)
    {
        await Execute("DELETE FROM tools_temtem.temtem_technique", null);

        return await Execute(
            """
            INSERT INTO tools_temtem.temtem_technique (temtem_id, technique_id, source, level)
            VALUES (@TemtemId, @TechniqueId, @Source, @Level)
            """,
            learnings.ToList());
    }

    public async Task<int> ReplaceTraitLinks(IReadOnlyCollection<TemtemTraitLinkData> links)
    {
        await Execute("DELETE FROM tools_temtem.temtem_trait", null);

        return await Execute(
            """
            INSERT INTO tools_temtem.temtem_trait (temtem_id, trait_id)
            VALUES (@TemtemId, @TraitId)
            """,
            links.ToList());
    }

    public async Task<int> ReplaceTypeMatchups(IReadOnlyCollection<TemtemTypeMatchupData> matchups)
    {
        await Execute("DELETE FROM tools_temtem.type_matrix", null);

        return await Execute(
            """
            INSERT INTO tools_temtem.type_matrix (attacker_type_id, defender_type_id, multiplier)
            VALUES (@AttackerTypeId, @DefenderTypeId, @Multiplier)
            """,
            matchups.ToList());
    }

    private async Task<TemtemUpsertOutcome> Upsert(string sql, object parameters)
    {
        var inserted = await Connection().QuerySingleOrDefaultAsync<bool?>(
            new CommandDefinition(sql, parameters, session.Transaction));

        return inserted switch
        {
            true => TemtemUpsertOutcome.Created,
            false => TemtemUpsertOutcome.Updated,
            null => TemtemUpsertOutcome.Unchanged
        };
    }

    private Task<int> Execute(string sql, object? parameters) =>
        Connection().ExecuteAsync(new CommandDefinition(sql, parameters, session.Transaction));

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
