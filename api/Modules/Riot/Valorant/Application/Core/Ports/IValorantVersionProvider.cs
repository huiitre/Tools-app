namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

public interface IValorantVersionProvider
{
    // TODO: à typer en vue dédiée. Le Java renvoie le bloc `data` de version.json tel quel ; le
    // front n'en lit que `riotClientVersion`. Les assets vivent sur le NAS, le fichier n'a pas pu
    // être lu pour en déduire les champs.
    Task<IReadOnlyDictionary<string, object>> GetVersion();

    // Chaque appel à Riot exige la version du client. Le Java allait la repêcher dans la map à
    // trois endroits (`get("riotClientVersion").toString()`) : ici c'est le fournisseur qui sait
    // où elle est rangée.
    Task<string> GetRiotClientVersion();
}
