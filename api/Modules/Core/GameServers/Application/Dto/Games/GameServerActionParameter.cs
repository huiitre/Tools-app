namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Champ à saisir avant de déclencher une action. Type pilote le rendu du formulaire côté front :
// « text », « number », ou « player » pour un choix parmi les joueurs connectés.
public sealed record GameServerActionParameter(
    string Name,
    string Label,
    string Type,
    bool Required,
    string? Placeholder);
