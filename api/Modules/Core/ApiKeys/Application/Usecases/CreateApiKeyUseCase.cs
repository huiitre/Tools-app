using Tools.Api.Modules.Core.ApiKeys.Application.Commands;
using Tools.Api.Modules.Core.ApiKeys.Application.Ports;
using Tools.Api.Modules.Core.ApiKeys.Application.Views;
using Tools.Api.Modules.Core.ApiKeys.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.ApiKeys.Application.Usecases;

// US-1 : la clé en clair n'existe que dans la réponse ; la base n'en garde que le hash et le préfixe.
public sealed class CreateApiKeyUseCase(
    UseCaseAuthorizer authorizer,
    IApiKeyRepository apiKeyRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<CreatedApiKeyView> Execute(CreateApiKeyCommand command)
    {
        var scopes = ParseScopes(command.Scopes);
        var secret = ApiKeySecret.Generate();

        var apiKey = ApiKey.Create(
            command.Name,
            command.Description,
            secret.Hash,
            secret.Prefix,
            scopes,
            CurrentUser.UserId,
            command.ExpiresAt);

        if (await apiKeyRepository.ActiveNameIsTaken(apiKey.Name))
        {
            throw AppException.Conflict("API_KEY_NAME_TAKEN", "Une clé active porte déjà ce nom.");
        }

        await using var transaction = await transactionManager.BeginAsync();
        var id = await apiKeyRepository.Create(apiKey);
        await transaction.CommitAsync();

        var created = await apiKeyRepository.FindById(id)
            ?? throw AppException.NotFound("API_KEY_NOT_FOUND", "Clé introuvable.");

        return new CreatedApiKeyView(ApiKeyView.From(created), secret.Value);
    }

    // Un code inconnu est refusé : on ne crée jamais une clé avec un droit que personne ne déclare.
    private static List<ApiKeyScope> ParseScopes(IReadOnlyCollection<string>? codes)
    {
        var scopes = new List<ApiKeyScope>();

        foreach (var code in codes ?? [])
        {
            var scope = ApiKeyScopes.Parse(code)
                ?? throw AppException.Validation("API_KEY_SCOPE_UNKNOWN", $"Le scope « {code} » n'existe pas.");

            scopes.Add(scope);
        }

        return scopes;
    }
}
