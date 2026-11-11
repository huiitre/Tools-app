using Tools.Api.Modules.Core.ApiKeys.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.ApiKeys.Application.Usecases;

// US-2 : définitive, la clé reste en base pour l'historique.
public sealed class RevokeApiKeyUseCase(
    UseCaseAuthorizer authorizer,
    IApiKeyRepository apiKeyRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(long apiKeyId)
    {
        var apiKey = await apiKeyRepository.FindById(apiKeyId)
            ?? throw AppException.NotFound("API_KEY_NOT_FOUND", "Clé introuvable.");

        if (apiKey.RevokedAt is not null)
        {
            throw AppException.Conflict("API_KEY_ALREADY_REVOKED", "Cette clé est déjà révoquée.");
        }

        await using var transaction = await transactionManager.BeginAsync();

        // Faux si une révocation concurrente est passée entre la lecture et l'écriture.
        if (!await apiKeyRepository.Revoke(apiKeyId, DateTimeOffset.UtcNow))
        {
            throw AppException.Conflict("API_KEY_ALREADY_REVOKED", "Cette clé est déjà révoquée.");
        }

        await transaction.CommitAsync();
    }
}
