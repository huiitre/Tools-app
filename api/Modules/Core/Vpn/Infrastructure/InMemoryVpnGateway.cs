using Tools.Api.Modules.Core.Vpn.Application.Dto;
using Tools.Api.Modules.Core.Vpn.Application.Ports;

namespace Tools.Api.Modules.Core.Vpn.Infrastructure;

// Jeu de données de dev/QA. Chaque peer couvre un cas d'affichage distinct : les clés, les IP
// et les noms sont fictifs, seule la forme reproduit celle de wg_api.
public sealed class InMemoryVpnGateway : IVpnGateway
{
    public Task<IReadOnlyList<VpnPeerDto>> FindPeersAsync()
    {
        IReadOnlyList<VpnPeerDto> peers = new List<VpnPeerDto>
        {
            // Actif à l'instant : handshake dans le cycle de renégociation (~120 s).
            new(
                "huiitre",
                "10.13.13.2",
                "4hV4j0bQ7OgxGnfAjU4ozAazDUbmZfAkwqRg4qU1/sM=",
                "connected",
                32,
                28_061_540,
                182_179_948,
                true,
                new PeerChecksDto(true, true, true, true)
            ),
            // Actif mais silencieux : tunnel probablement monté, aucun trafic récent.
            new(
                "biscaros",
                "10.13.13.3",
                "janc0nDT11gOsZdwPN2TTeaRBLXYmnU+b5ZBYeIYNzY=",
                "connected",
                174,
                4_312_889,
                9_004_112,
                true,
                new PeerChecksDto(true, true, true, true)
            ),
            // Vu il y a ~27 h.
            new(
                "leytor",
                "10.13.13.6",
                "nuTLawS6q/hUOvnQqQE4PO1q6aagomP/VjPlK99P270=",
                "idle",
                98_585,
                218_902_744,
                1_087_709_576,
                true,
                new PeerChecksDto(true, true, true, true)
            ),
            // Vu il y a 41 jours : borne haute pour le formatage des durées.
            new(
                "marsignac",
                "10.13.13.7",
                "LueT7mUe45y3bl7/C+w8Ahy6WJi3uRyAAU6v9uhtA4g=",
                "idle",
                3_542_400,
                12_884_901_888,
                48_318_382_080,
                true,
                new PeerChecksDto(true, true, true, true)
            ),
            // Jamais vu depuis le démarrage de l'interface : compteurs à zéro, handshake null.
            new(
                "dimitri",
                "10.13.13.4",
                "qQQoisUDts2m4LcFrQdn6V71J4MWwTai+xBN3R0KkOY=",
                "never",
                null,
                0,
                0,
                true,
                new PeerChecksDto(true, true, true, true)
            ),
            // Désactivé : retiré de l'interface, conf conservée. valid=false sans anomalie.
            new(
                "gogo",
                "10.13.13.5",
                "fWuxYmOQGGwYWbEa0qXta/nMJCvx8vdVYJ9zAdpNveA=",
                "never",
                null,
                0,
                0,
                false,
                new PeerChecksDto(true, true, false, true)
            ),
            // Chargé sur l'interface mais absent de wg0.conf : disparaîtra au prochain redémarrage.
            new(
                "randompu",
                "10.13.13.8",
                "LYvatdsHoaczzYEDdEUgr/IZWyLzYlhOQ2B8l1Rke7k=",
                "connected",
                88,
                1_204_338,
                7_781_002,
                false,
                new PeerChecksDto(true, true, true, false)
            ),
            // Clé serveur divergente : la conf du client pointe vers un autre serveur.
            new(
                "yumiie",
                "10.13.13.9",
                "LbJq4jz0AbSbil8KcztfHQXvR1lfej6yKzIE+yllMyc=",
                "never",
                null,
                0,
                0,
                false,
                new PeerChecksDto(true, false, true, true)
            ),
            // Paire de clés absente du disque : conf non régénérable.
            new(
                "ancien-portable",
                "10.13.13.10",
                "WcF/sNZhQpstrQUlkPWEJlH1BrULFS7PdG5WOY7PRBA=",
                "never",
                null,
                0,
                0,
                false,
                new PeerChecksDto(false, true, true, true)
            ),
            // Tout est cassé + nom long : borne pour la troncature et le pire cas d'affichage.
            new(
                "poste-de-test-tres-tres-long",
                "10.13.13.11",
                "/OGV4Q3tQGExjmaLfYT54DAR2zeYAi7SRMiUVMx2GFk=",
                "never",
                null,
                0,
                0,
                false,
                new PeerChecksDto(false, false, false, false)
            ),
        };

        return Task.FromResult(peers);
    }
}
