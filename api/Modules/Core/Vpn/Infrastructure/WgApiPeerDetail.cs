namespace Tools.Api.Modules.Core.Vpn.Infrastructure;

// Détail d'un peer renvoyé par wg_api : { name, ip, config, qrcodePngBase64 }.
// `config` est le contenu du fichier client, clé privée comprise.
public sealed record WgApiPeerDetail(string? Name, string? Ip, string? Config, string? QrcodePngBase64);
