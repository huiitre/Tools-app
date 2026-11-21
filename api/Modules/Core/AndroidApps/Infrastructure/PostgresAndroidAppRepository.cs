using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Infrastructure;

namespace Tools.Api.Modules.Core.AndroidApps.Infrastructure;

// Lecture sur sa propre connexion, écriture dans la transaction ambiante.
public sealed class PostgresAndroidAppRepository(
    NpgsqlDataSource dataSource,
    PostgresSession session) : IAndroidAppRepository
{
    private const string SelectApps = """
        SELECT a.id, a.package_name AS PackageName, a.label, a.display_order AS DisplayOrder,
               a.is_active AS IsActive, a.recommended_release_id AS RecommendedReleaseId,
               a.admin_notes AS AdminNotes,
               a.created_at AS CreatedAt
        FROM tools_core.android_apps a
        """;

    private const string SelectReleases = """
        SELECT r.id, r.app_id AS AppId, r.version_code AS VersionCode, r.version_name AS VersionName,
               r.min_sdk AS MinSdk, r.file_path AS FilePath, r.file_size AS FileSize,
               r.file_sha256 AS FileSha256, r.cert_sha256 AS CertSha256, r.release_notes AS ReleaseNotes,
               r.uploaded_at AS UploadedAt, r.uploaded_by AS UploadedBy, u.name AS UploadedByName
        FROM tools_core.android_app_releases r
        LEFT JOIN tools_core.users u ON u.id = r.uploaded_by
        """;

    private const string AppOrder = " ORDER BY a.display_order, lower(a.label), a.id";
    private const string ReleaseOrder = " ORDER BY r.version_code DESC";

    public async Task<List<AndroidApp>> FindAll()
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var apps = await connection.QueryAsync<AppRow>(new CommandDefinition(SelectApps + AppOrder));
        var releases = await connection.QueryAsync<ReleaseRow>(new CommandDefinition(SelectReleases + ReleaseOrder));

        return Assemble(apps, releases);
    }

    public Task<AndroidApp?> FindById(long appId) =>
        FindOne(" WHERE a.id = @Value", appId);

    public Task<AndroidApp?> FindByPackageName(string packageName) =>
        FindOne(" WHERE a.package_name = @Value", packageName);

    public async Task<List<AndroidApp>> FindCatalogue()
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var apps = (await connection.QueryAsync<AppRow>(new CommandDefinition(
            SelectApps + " WHERE a.is_active AND a.recommended_release_id IS NOT NULL" + AppOrder))).ToList();

        var releases = await connection.QueryAsync<ReleaseRow>(new CommandDefinition(
            SelectReleases + " WHERE r.id = ANY(@Ids)",
            new { Ids = apps.Select(app => app.RecommendedReleaseId!.Value).ToArray() }));

        return Assemble(apps, releases);
    }

    public async Task<long> CreateApp(string packageName, string label)
    {
        const string sql = """
            INSERT INTO tools_core.android_apps (package_name, label, display_order)
            VALUES (@PackageName, @Label,
                    (SELECT COALESCE(MAX(display_order), -1) + 1 FROM tools_core.android_apps))
            RETURNING id
            """;

        return await Connection().ExecuteScalarAsync<long>(new CommandDefinition(
            sql, new { PackageName = packageName, Label = label }, session.Transaction));
    }

    public async Task<long> CreateRelease(long appId, ApkInfo apk, string filePath, string? releaseNotes, long uploadedBy)
    {
        const string sql = """
            INSERT INTO tools_core.android_app_releases
                (app_id, version_code, version_name, min_sdk, file_path, file_size,
                 file_sha256, cert_sha256, release_notes, uploaded_by)
            VALUES
                (@AppId, @VersionCode, @VersionName, @MinSdk, @FilePath, @FileSize,
                 @FileSha256, @CertSha256, @ReleaseNotes, @UploadedBy)
            RETURNING id
            """;

        return await Connection().ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new
            {
                AppId = appId,
                apk.VersionCode,
                apk.VersionName,
                apk.MinSdk,
                FilePath = filePath,
                apk.FileSize,
                apk.FileSha256,
                apk.CertSha256,
                ReleaseNotes = releaseNotes,
                UploadedBy = uploadedBy
            },
            session.Transaction));
    }

    public async Task UpdateApp(long appId, string label, int displayOrder, bool isActive, long? recommendedReleaseId, string? adminNotes)
    {
        const string sql = """
            UPDATE tools_core.android_apps
            SET label = @Label, display_order = @DisplayOrder, is_active = @IsActive,
                recommended_release_id = @RecommendedReleaseId, admin_notes = @AdminNotes
            WHERE id = @AppId
            """;

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                AppId = appId,
                Label = label,
                DisplayOrder = displayOrder,
                IsActive = isActive,
                RecommendedReleaseId = recommendedReleaseId,
                AdminNotes = adminNotes
            },
            session.Transaction));
    }

    public async Task SetRecommendedRelease(long appId, long releaseId)
    {
        await Connection().ExecuteAsync(new CommandDefinition(
            "UPDATE tools_core.android_apps SET recommended_release_id = @ReleaseId WHERE id = @AppId",
            new { AppId = appId, ReleaseId = releaseId },
            session.Transaction));
    }

    public async Task DeleteRelease(long releaseId)
    {
        await Connection().ExecuteAsync(new CommandDefinition(
            "DELETE FROM tools_core.android_app_releases WHERE id = @Id",
            new { Id = releaseId },
            session.Transaction));
    }

    public async Task DeleteApp(long appId)
    {
        await Connection().ExecuteAsync(new CommandDefinition(
            "DELETE FROM tools_core.android_apps WHERE id = @Id",
            new { Id = appId },
            session.Transaction));
    }

    private async Task<AndroidApp?> FindOne(string where, object value)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var app = await connection.QuerySingleOrDefaultAsync<AppRow>(new CommandDefinition(
            SelectApps + where, new { Value = value }));

        if (app is null)
        {
            return null;
        }

        var releases = await connection.QueryAsync<ReleaseRow>(new CommandDefinition(
            SelectReleases + " WHERE r.app_id = @AppId" + ReleaseOrder, new { AppId = app.Id }));

        return Assemble([app], releases).Single();
    }

    private static List<AndroidApp> Assemble(IEnumerable<AppRow> apps, IEnumerable<ReleaseRow> releases)
    {
        var releasesByApp = releases.Select(ToDomain).ToLookup(release => release.AppId);

        return apps
            .Select(app => new AndroidApp(
                app.Id,
                app.PackageName,
                app.Label,
                app.DisplayOrder,
                app.IsActive,
                app.RecommendedReleaseId,
                app.AdminNotes,
                new DateTimeOffset(DateTime.SpecifyKind(app.CreatedAt, DateTimeKind.Utc)),
                releasesByApp[app.Id].OrderByDescending(release => release.VersionCode).ToList()))
            .ToList();
    }

    private NpgsqlConnection Connection() =>
        session.Connection ?? throw new InvalidOperationException("Écriture hors transaction.");

    private static AndroidAppRelease ToDomain(ReleaseRow row) => new(
        row.Id,
        row.AppId,
        row.VersionCode,
        row.VersionName,
        row.MinSdk,
        row.FilePath,
        row.FileSize,
        row.FileSha256,
        row.CertSha256,
        row.ReleaseNotes,
        new DateTimeOffset(DateTime.SpecifyKind(row.UploadedAt, DateTimeKind.Utc)),
        row.UploadedBy,
        row.UploadedByName);

    private sealed class AppRow
    {
        public long Id { get; init; }
        public string PackageName { get; init; } = "";
        public string Label { get; init; } = "";
        public int DisplayOrder { get; init; }
        public bool IsActive { get; init; }
        public long? RecommendedReleaseId { get; init; }
        public string? AdminNotes { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    private sealed class ReleaseRow
    {
        public long Id { get; init; }
        public long AppId { get; init; }
        public long VersionCode { get; init; }
        public string? VersionName { get; init; }
        public int MinSdk { get; init; }
        public string FilePath { get; init; } = "";
        public long FileSize { get; init; }
        public byte[] FileSha256 { get; init; } = [];
        public byte[] CertSha256 { get; init; } = [];
        public string? ReleaseNotes { get; init; }
        public DateTime UploadedAt { get; init; }
        public long UploadedBy { get; init; }
        public string? UploadedByName { get; init; }
    }
}
