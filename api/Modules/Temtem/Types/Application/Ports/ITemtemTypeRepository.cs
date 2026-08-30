using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Types.Application.Ports;

public interface ITemtemTypeRepository
{
    Task<List<TemtemTypeView>> FindAll();

    // La matrice entière en une fois : 144 lignes, et le simulateur en a besoin de bout en bout.
    // La lire couple par couple ferait une requête par comparaison.
    Task<Dictionary<(int Attacker, int Defender), decimal>> FindEffectivenessMatrix();
}
