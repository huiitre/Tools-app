using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Tools.Api.Modules.Core.AppLogs.Application;
using Tools.Api.Modules.Core.AppLogs.Application.Ports;
using Tools.Api.Modules.Core.AppLogs.Application.Services;
using Tools.Api.Modules.Core.AppLogs.Infrastructure;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.AppLogs;

public sealed class AppLogServiceTests
{
    [Fact]
    public async Task Log_normalise_la_classification_et_ajoute_le_contexte_http()
    {
        var repository = new RecordingAppLogRepository();
        var ipAddress = IPAddress.Parse("203.0.113.42");
        var service = new AppLogService(
            repository,
            new FixedAppLogContextProvider(new AppLogContext(ipAddress, "Tools-Test/1.0")));

        var id = await service.Log(new AppLogCommand(
            ModuleId: 7,
            AreaCode: " auth ",
            ActionCode: " login ",
            UserId: 42,
            Metadata: new { authenticationMethod = "PASSWORD" }));

        Assert.Equal(123, id);
        var entry = Assert.IsType<AppLogEntry>(repository.LastEntry);
        Assert.Equal(7, entry.ModuleId);
        Assert.Equal("AUTH", entry.AreaCode);
        Assert.Equal("LOGIN", entry.ActionCode);
        Assert.Equal(42, entry.UserId);
        Assert.Equal(ipAddress, entry.IpAddress);
        Assert.Equal("Tools-Test/1.0", entry.UserAgent);

        using var metadata = JsonDocument.Parse(entry.Metadata);
        Assert.Equal("PASSWORD", metadata.RootElement.GetProperty("authenticationMethod").GetString());
    }

    [Fact]
    public async Task Log_hors_requete_accepte_un_utilisateur_et_un_contexte_absents()
    {
        var repository = new RecordingAppLogRepository();
        var service = new AppLogService(
            repository,
            new FixedAppLogContextProvider(new AppLogContext(null, null)));

        await service.Log(new AppLogCommand(null, "SYSTEM", "SYNC_COMPLETED"));

        var entry = Assert.IsType<AppLogEntry>(repository.LastEntry);
        Assert.Null(entry.ModuleId);
        Assert.Null(entry.UserId);
        Assert.Null(entry.IpAddress);
        Assert.Null(entry.UserAgent);
        Assert.Equal("{}", entry.Metadata);
    }

    [Fact]
    public void Le_contexte_http_lit_lip_et_le_user_agent_de_la_requete()
    {
        var ipAddress = IPAddress.Parse("198.51.100.27");
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = ipAddress;
        httpContext.Request.Headers.UserAgent = "Tools-Browser/1.0";

        var provider = new HttpAppLogContextProvider(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        Assert.Equal(ipAddress, provider.Current.IpAddress);
        Assert.Equal("Tools-Browser/1.0", provider.Current.UserAgent);
    }

    [Theory]
    [InlineData("", "LOGIN")]
    [InlineData("AUTH", "   ")]
    public async Task Log_refuse_une_classification_vide(string areaCode, string actionCode)
    {
        var repository = new RecordingAppLogRepository();
        var service = new AppLogService(
            repository,
            new FixedAppLogContextProvider(new AppLogContext(null, null)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.Log(new AppLogCommand(null, areaCode, actionCode)));

        Assert.Null(repository.LastEntry);
    }

    private sealed class RecordingAppLogRepository : IAppLogRepository
    {
        public AppLogEntry? LastEntry { get; private set; }

        public Task<long> InsertAsync(AppLogEntry entry)
        {
            LastEntry = entry;
            return Task.FromResult(123L);
        }

        public Task<AppLogPageDto> FindForAdminAsync(AppLogListQuery query) =>
            Task.FromResult(new AppLogPageDto([], 0, query.Page, query.PageSize));
    }

    private sealed class FixedAppLogContextProvider(AppLogContext context) : IAppLogContextProvider
    {
        public AppLogContext Current => context;
    }
}
