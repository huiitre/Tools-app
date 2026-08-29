namespace Tools.Api.Modules.Riot.Valorant.Application.User.Commands;

// L'ajout est groupé : toute la rotation du jour part en un appel.
//
// Les deux champs sont facultatifs dans le JSON reçu — la liaison ne garantit rien, le use case
// traite l'absence : liste nulle = rien à faire, date nulle = aujourd'hui.
public sealed record AddSkinToStoreHistoryCommand(
    List<long>? SkinIds,
    DateOnly? SeenAt,
    long AccountId
);
