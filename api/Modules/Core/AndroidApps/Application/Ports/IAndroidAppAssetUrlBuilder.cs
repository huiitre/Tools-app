namespace Tools.Api.Modules.Core.AndroidApps.Application.Ports;

// URL publique d'un APK, servie par le conteneur tools_assets et non par l'API.
public interface IAndroidAppAssetUrlBuilder
{
    string Build(string relativePath);
}
