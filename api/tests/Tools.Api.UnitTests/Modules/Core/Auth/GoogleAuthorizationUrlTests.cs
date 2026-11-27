using Xunit;
using Tools.Api.Modules.Core.Auth.Application;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;
using Tools.Api.Modules.Core.Auth.Application.Usecases.Google;
using Tools.Api.Modules.Core.Auth.Infrastructure.Google;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.UnitTests.Modules.Core.Auth;

// L'origine conservée dans le state décide où le callback renvoie le token.
public sealed class GoogleAuthorizationUrlTests
{
    private readonly GoogleOAuthStateStore stateStore = new();
    private readonly GetGoogleAuthorizationUrlUseCase useCase;

    public GoogleAuthorizationUrlTests()
    {
        useCase = new GetGoogleAuthorizationUrlUseCase(stateStore, new StateEchoClient());
    }

    [Fact]
    public void Electron_loopback_port_is_kept_until_the_callback()
    {
        var state = useCase.Execute("electron", 45123);

        Assert.Equal(new GoogleOAuthOrigin("electron", 45123), stateStore.Consume(state));
    }

    [Theory]
    [InlineData("web")]
    [InlineData("electron")]
    public void Without_port_the_origin_has_no_loopback(string source)
    {
        var state = useCase.Execute(source);

        Assert.Equal(new GoogleOAuthOrigin(source, null), stateStore.Consume(state));
    }

    [Theory]
    [InlineData("web", 45123)]
    [InlineData("electron", 80)]
    [InlineData("electron", 70000)]
    public void Invalid_loopback_port_is_refused(string source, int port)
    {
        var exception = Assert.Throws<AppException>(() => useCase.Execute(source, port));

        Assert.Equal("GOOGLE_LOOPBACK_PORT_INVALID", exception.Code);
    }

    [Fact]
    public void Unknown_source_is_refused()
    {
        var exception = Assert.Throws<AppException>(() => useCase.Execute("mobile"));

        Assert.Equal("GOOGLE_SOURCE_INVALID", exception.Code);
    }

    // Renvoie le state tel quel : le test le relit dans le store.
    private sealed class StateEchoClient : IGoogleOAuthClient
    {
        public string BuildAuthorizationUrl(string state) => state;

        public Task<string> ExchangeCodeForIdTokenAsync(string code) => throw new NotSupportedException();
    }
}
