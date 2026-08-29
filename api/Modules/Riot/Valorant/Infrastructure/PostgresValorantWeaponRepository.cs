using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

public sealed class PostgresValorantWeaponRepository(RiotDatabase database) : IValorantWeaponRepository
{
    private const string Select = """
        SELECT id AS Id, asset_id AS AssetId, name AS Name, category AS Category,
               default_skin_asset_id AS DefaultSkinAssetId, display_icon_url AS DisplayIconUrl
        FROM tools_riot.valorant_weapons
        """;

    public Task<List<ValorantWeaponView>> FindAll() =>
        database.Query<ValorantWeaponView>($"{Select} ORDER BY name");

    public Task<ValorantWeaponView?> FindById(long id) =>
        database.QueryFirstOrDefault<ValorantWeaponView>($"{Select} WHERE id = @Id", new { Id = id });
}
