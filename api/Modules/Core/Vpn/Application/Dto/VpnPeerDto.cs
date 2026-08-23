namespace Tools.Api.Modules.Core.Vpn.Application.Dto;

public sealed record VpnPeerDto(
    string Name,
    string Ip,
    string PublicKey,
    string Status,
    long? HandshakeSecondsAgo,
    long RxBytes,
    long TxBytes,
    bool Valid,
    PeerChecksDto Checks
);