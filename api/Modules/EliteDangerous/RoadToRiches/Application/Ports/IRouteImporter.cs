namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;

public interface IRouteImporter
{
    string Source { get; }
    string Parse(byte[] fileContent, string fileName);
}