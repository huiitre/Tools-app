namespace Tools.Api.Modules.Core.AppLogs.Application.Ports;

// Fournit les informations de la requête sans faire dépendre l'Application d'ASP.NET.
public interface IAppLogContextProvider
{
    AppLogContext Current { get; }
}
