using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.AndroidApps.Infrastructure;

// Lit un APK avec les outils officiels du SDK Android plutôt qu'un parseur maison : aapt2 décode
// le manifeste binaire, apksigner vérifie toutes les versions de signature (v1 à v4). apksigner
// est un script Java : `java` doit être dans le PATH de l'API.
//
// Les arguments passent par ArgumentList, jamais par une ligne de commande à interpréter : le
// chemin du fichier ne peut rien injecter.
public sealed class AndroidBuildToolsApkInspector(
    IOptions<AndroidAppsOptions> options,
    ILogger<AndroidBuildToolsApkInspector> logger) : IApkInspector
{
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(60);

    public async Task<ApkInfo> Inspect(string apkPath, CancellationToken cancellationToken = default)
    {
        var buildTools = options.Value.BuildToolsPath;

        if (string.IsNullOrWhiteSpace(buildTools) || !Directory.Exists(buildTools))
        {
            throw AppException.Unavailable(
                "ANDROID_BUILD_TOOLS_MISSING",
                "Les outils du SDK Android (aapt2, apksigner) ne sont pas configurés sur ce serveur.");
        }

        var badgingRun = await Run(Path.Combine(buildTools, "aapt2"), ["dump", "badging", apkPath], cancellationToken);
        var badging = badgingRun.ExitCode == 0 ? ApkToolOutputParser.ParseBadging(badgingRun.Output) : null;

        if (badging is null)
        {
            logger.LogInformation("aapt2 refuse le fichier (code {ExitCode}) : {Error}", badgingRun.ExitCode, badgingRun.Error);
            throw Invalid("Le fichier n'est pas un APK lisible.");
        }

        var signatureRun = await Run(
            Path.Combine(buildTools, "apksigner"), ["verify", "--print-certs", "-v", apkPath], cancellationToken);

        if (signatureRun.ExitCode != 0)
        {
            logger.LogInformation("apksigner refuse le fichier (code {ExitCode}) : {Error}", signatureRun.ExitCode, signatureRun.Error);
            throw Invalid("La signature de l'APK ne se vérifie pas.");
        }

        var signature = ApkToolOutputParser.ParseSignature(signatureRun.Output)
            ?? throw Invalid("La signature de l'APK n'a pas pu être lue (un seul signataire est accepté).");

        var file = new FileInfo(apkPath);

        await using var stream = file.OpenRead();
        var fileSha256 = await SHA256.HashDataAsync(stream, cancellationToken);

        return new ApkInfo(
            badging.PackageName,
            badging.VersionCode,
            badging.VersionName,
            badging.MinSdk,
            badging.Label,
            file.Length,
            fileSha256,
            signature.CertSha256,
            signature.CertSubject,
            signature.Schemes);
    }

    private static AppException Invalid(string message) => AppException.Validation("ANDROID_APK_INVALID", message);

    private sealed record ToolRun(int ExitCode, string Output, string Error);

    private static async Task<ToolRun> Run(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw AppException.Unavailable(
                "ANDROID_BUILD_TOOLS_MISSING",
                $"Impossible de lancer {Path.GetFileName(executable)} : {exception.Message}");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ToolTimeout);

        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);

            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw AppException.Unavailable(
                "ANDROID_BUILD_TOOLS_TIMEOUT",
                $"{Path.GetFileName(executable)} n'a pas répondu en {ToolTimeout.TotalSeconds} secondes.");
        }

        return new ToolRun(process.ExitCode, await output, await error);
    }
}
