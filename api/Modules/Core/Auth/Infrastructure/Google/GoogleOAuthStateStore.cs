using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;

namespace Tools.Api.Modules.Core.Auth.Infrastructure.Google;

// État OAuth à usage unique, valide dix minutes, stocké en mémoire dans l'instance Core.
public sealed class GoogleOAuthStateStore : IGoogleOAuthStateStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, StateEntry> pendingStates = new();

    public string Create(string source)
    {
        if (source is not ("web" or "electron"))
        {
            throw AppException.Validation("GOOGLE_SOURCE_INVALID", "Source Google invalide.");
        }

        RemoveExpiredStates();
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        pendingStates[state] = new StateEntry(source, DateTimeOffset.UtcNow.Add(Lifetime));
        return state;
    }

    public string Consume(string state)
    {
        if (!pendingStates.TryRemove(state, out var entry) || DateTimeOffset.UtcNow > entry.ExpiresAt)
        {
            throw AppException.Unauthorized("GOOGLE_STATE_INVALID", "Authentification Google invalide.");
        }

        return entry.Source;
    }

    private void RemoveExpiredStates()
    {
        foreach (var pair in pendingStates.Where(pair => DateTimeOffset.UtcNow > pair.Value.ExpiresAt))
        {
            pendingStates.TryRemove(pair.Key, out _);
        }
    }

    private sealed record StateEntry(string Source, DateTimeOffset ExpiresAt);
}
