namespace Tools.Api.Modules.GameServers.Application.Dto;

// Projection privée destinée aux adapters de poll. protocolConfig peut contenir des credentials
// et ne doit donc jamais être exposé par le futur endpoint dashboard.
public sealed record GameServerPollTarget(
    long Id,
    string Slug,
    string ProtocolType,
    string Host,
    int Port,
    string ProtocolConfig);
