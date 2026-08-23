namespace Tools.Api.Modules.Core.Vpn.Application.Dto;

public sealed record PeerChecksDto(
    bool KeyPair,
    bool ServerKey,
    bool OnInterface,
    bool InConfig
);