namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Commands;

public sealed record ImportExpeditionCommand(
    byte[] FileContent,
    string FileName,
    string Source,
    string Name
);