using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

public sealed class PostgresValorantAuthRepository(RiotDatabase database) : IValorantAuthRepository
{
    // COALESCE sur game_name, tag_line et label : la rotation de jeton rappelle cette requête avec
    // ces trois valeurs nulles, elle ne doit pas effacer ce que la liaison avait renseigné.
    public Task<long> Save(
        long userId,
        string puuid,
        string region,
        string? gameName,
        string? tagLine,
        string? label,
        string encryptedRefreshToken,
        string iv,
        DateTime expiresAt) =>
        database.ExecuteScalar<long>(
            """
            INSERT INTO tools_riot.valorant_account
                (user_id, puuid, region, game_name, tag_line, label, encrypted_refresh, encryption_iv, expires_at, updated_at)
            VALUES (@UserId, @Puuid, @Region, @GameName, @TagLine, @Label, @EncryptedRefreshToken, @Iv, @ExpiresAt, now())
            ON CONFLICT (user_id, puuid) DO UPDATE SET
                region = EXCLUDED.region,
                game_name = COALESCE(EXCLUDED.game_name, tools_riot.valorant_account.game_name),
                tag_line = COALESCE(EXCLUDED.tag_line, tools_riot.valorant_account.tag_line),
                label = COALESCE(EXCLUDED.label, tools_riot.valorant_account.label),
                encrypted_refresh = EXCLUDED.encrypted_refresh,
                encryption_iv = EXCLUDED.encryption_iv,
                expires_at = EXCLUDED.expires_at,
                updated_at = now()
            RETURNING id
            """,
            new
            {
                UserId = userId,
                Puuid = puuid,
                Region = region,
                GameName = gameName,
                TagLine = tagLine,
                Label = label,
                EncryptedRefreshToken = encryptedRefreshToken,
                Iv = iv,
                ExpiresAt = expiresAt
            });

    public Task<IValorantAuthRepository.ValorantAuthData?> FindById(long accountId) =>
        database.QueryFirstOrDefault<IValorantAuthRepository.ValorantAuthData>(
            """
            SELECT user_id AS UserId, puuid AS Puuid, region AS Region,
                   encrypted_refresh AS EncryptedRefreshToken, encryption_iv AS Iv, expires_at AS ExpiresAt
            FROM tools_riot.valorant_account
            WHERE id = @AccountId
            """,
            new { AccountId = accountId });

    public Task<List<IValorantAuthRepository.ValorantAccountData>> FindAllByUserId(long userId) =>
        database.Query<IValorantAuthRepository.ValorantAccountData>(
            """
            SELECT id AS Id, puuid AS Puuid, region AS Region,
                   game_name AS GameName, tag_line AS TagLine, label AS Label
            FROM tools_riot.valorant_account
            WHERE user_id = @UserId
            ORDER BY created_at
            """,
            new { UserId = userId });

    public Task<bool> ExistsByIdAndUserId(long accountId, long userId) =>
        database.ExecuteScalar<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM tools_riot.valorant_account WHERE id = @AccountId AND user_id = @UserId
            )
            """,
            new { AccountId = accountId, UserId = userId });

    public Task<bool> ExistsByUserIdAndPuuid(long userId, string puuid) =>
        database.ExecuteScalar<bool>(
            """
            SELECT EXISTS (
                SELECT 1 FROM tools_riot.valorant_account WHERE user_id = @UserId AND puuid = @Puuid
            )
            """,
            new { UserId = userId, Puuid = puuid });

    public Task UpdateLabel(long accountId, string label) =>
        database.Execute(
            "UPDATE tools_riot.valorant_account SET label = @Label, updated_at = now() WHERE id = @AccountId",
            new { AccountId = accountId, Label = label });

    public Task DeleteById(long accountId) =>
        database.Execute(
            "DELETE FROM tools_riot.valorant_account WHERE id = @AccountId",
            new { AccountId = accountId });

    public Task<List<long>> FindAllAccountIds() =>
        database.Query<long>("SELECT id FROM tools_riot.valorant_account");
}
