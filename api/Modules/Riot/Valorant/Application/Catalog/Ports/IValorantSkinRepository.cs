using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;

// accountId traverse toutes les lectures du catalogue : il ne sert qu'à renseigner « possédé » et
// « suivi » sur chaque skin, et vaut null quand l'appelant n'a désigné aucun compte Valorant lié
// (les deux drapeaux sont alors faux). Les deux dernières méthodes, elles, listent le contenu d'un
// compte : il y est obligatoire.
public interface IValorantSkinRepository
{
    Task<List<ValorantSkinView>> FindAll(long? accountId);
    Task<ValorantSkinView?> FindById(long id, long? accountId);
    Task<ValorantSkinView?> FindByAssetId(Guid assetId, long? accountId);
    Task<ValorantSkinView?> FindByLevelAssetId(Guid levelAssetId, long? accountId);
    Task<List<ValorantSkinView>> FindAllByWeaponId(long weaponId, long? accountId);
    Task<List<ValorantSkinView>> FindAllByTierUuid(Guid tierUuid, long? accountId);

    Task<List<ValorantSkinView>> FindAllOwnedByAccountId(long accountId);
    Task<List<ValorantSkinView>> FindAllWatchedByAccountId(long accountId);
}
