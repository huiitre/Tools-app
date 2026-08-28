namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;

// Bilan d'un sync atomique : tous les manifests ont été comparés, puis les absents supprimés.
public sealed record GameServersSyncReport(int Created, int Updated, int Unchanged, int Deleted);
