using Microsoft.Extensions.Logging.Abstractions;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.Security;

public sealed class UseCaseAuthorizerApiKeyTests
{
    private sealed class FixedCaller(CurrentUser? user, CurrentApiKey? apiKey) : ICurrentUserProvider
    {
        public CurrentUser? Current => user;
        public CurrentApiKey? CurrentApiKey => apiKey;
    }

    private static UseCaseAuthorizer Authorizer(CurrentUser? user = null, CurrentApiKey? apiKey = null) =>
        new(new FixedCaller(user, apiKey), NullLogger<UseCaseAuthorizer>.Instance);

    private static CurrentApiKey Key(params ApiKeyScope[] scopes) => new(7, "extracteur", scopes.ToHashSet());

    private static CurrentUser User(RoleCode role) => new(1, role, new Dictionary<ModuleCode, RoleCode>());

    [Fact]
    public void A_key_carrying_the_scope_is_accepted()
    {
        var caller = Authorizer(apiKey: Key(ApiKeyScope.TemtemSync))
            .Authorize(RoleCode.Tech, null, ApiKeyScope.TemtemSync);

        Assert.Equal(7, Assert.IsType<ApiKeyCaller>(caller).ApiKey.Id);
    }

    [Fact]
    public void A_key_without_the_scope_is_forbidden()
    {
        var error = Assert.Throws<AppException>(() =>
            Authorizer(apiKey: Key(ApiKeyScope.CoreMailSend)).Authorize(RoleCode.Tech, null, ApiKeyScope.TemtemSync));

        Assert.Equal("INSUFFICIENT_SCOPE", error.Code);
    }

    [Fact]
    public void A_use_case_without_scope_refuses_every_key()
    {
        var error = Assert.Throws<AppException>(() =>
            Authorizer(apiKey: Key(Enum.GetValues<ApiKeyScope>())).Authorize(RoleCode.ReadOnly, null, null));

        Assert.Equal("API_KEY_NOT_ALLOWED", error.Code);
    }

    // Le scope ne remplace pas le rôle pour un utilisateur : les deux chemins restent séparés.
    [Fact]
    public void A_user_is_still_judged_on_role_when_the_use_case_accepts_keys()
    {
        var error = Assert.Throws<AppException>(() =>
            Authorizer(user: User(RoleCode.User)).Authorize(RoleCode.Tech, null, ApiKeyScope.CoreMailSend));

        Assert.Equal("INSUFFICIENT_ROLE", error.Code);
        Assert.IsType<UserCaller>(
            Authorizer(user: User(RoleCode.Tech)).Authorize(RoleCode.Tech, null, ApiKeyScope.CoreMailSend));
    }

    private sealed class ReadsTheUser(UseCaseAuthorizer authorizer) : SecuredUseCase(authorizer)
    {
        protected override ApiKeyScope? RequiredScope => ApiKeyScope.CoreMailSend;

        public long UserId() => CurrentUser.UserId;
    }

    // Lire l'utilisateur alors qu'une clé appelle est une erreur de programmation, jamais un id vide.
    [Fact]
    public void Reading_the_user_of_a_key_call_fails_loudly()
    {
        var useCase = new ReadsTheUser(Authorizer(apiKey: Key(ApiKeyScope.CoreMailSend)));

        Assert.Throws<InvalidOperationException>(() => useCase.UserId());
    }
}
