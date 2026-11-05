namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;

// Information libre déclarée par le manifest (version du jeu, du loader, empreinte…), affichée
// telle quelle : ni le libellé ni la valeur ne sont interprétés.
public sealed record GameServerInfoView(string Label, string Value);
