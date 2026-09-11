namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

// Un adapter par hébergeur de mods. Le sync ne connaît aucun hébergeur : il confie à chacun les
// URLs qu'il reconnaît. Ne lève jamais : une panne se traduit par des mods sans icône.
public interface IModIconResolver
{
    bool Supports(string modUrl);

    // Clé = URL du mod telle que reçue. Une URL absente du résultat reste sans icône.
    Task<IReadOnlyDictionary<string, string>> ResolveAsync(IReadOnlyCollection<string> modUrls);
}
