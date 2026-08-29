namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

// Lit le puuid dans un access token Riot fourni par l'appelant. Côté Java c'était une classe
// d'infrastructure appelée directement par un use case ; ici c'est un port, l'application ne
// dépend pas d'un décodeur concret.
public interface IValorantTokenParser
{
    string ExtractPuuid(string accessToken);
}
